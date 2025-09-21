using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using ATM_API_System.Data;
using ATM_API_System.Dtos;
using ATM_API_System.DTOs;
using ATM_API_System.Models;
using ATM_API_System.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ATM_API_System.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        private const int MaxPinRetries = 3;

        public AuthService(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        public async Task<AuthResponseDto?> AuthenticateAsync(LoginRequestDto request)
        {
            if (request == null) return null;

            var card = await _db.Cards
                .Include(c => c.Customer)
                .FirstOrDefaultAsync(c => c.CardNumber == request.CardNumber);

            if (card == null) return null;
            if (string.Equals(card.Status, "Locked", StringComparison.OrdinalIgnoreCase))
            {
                // Log attempted access to locked card
                await _db.AuditLogs.AddAsync(new AuditLog
                {
                    ActorType = "Card",
                    ActorId = 0,
                    Action = $"Login attempt to locked card {request.CardNumber}",
                    CreatedAt = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();
                return null;
            }

            var pinOk = PinHasher.Verify(request.Pin, card.PinHash);
            if (!pinOk)
            {
                card.PinRetryCount += 1;

                // Log failed PIN attempt
                await _db.AuditLogs.AddAsync(new AuditLog
                {
                    ActorType = "Card",
                    ActorId = card.Id,
                    Action = $"Failed PIN attempt for CardId={card.Id}, RetryCount={card.PinRetryCount}",
                    CreatedAt = DateTime.UtcNow
                });

                if (card.PinRetryCount >= MaxPinRetries)
                {
                    card.Status = "Locked";
                    // Log lock event
                    await _db.AuditLogs.AddAsync(new AuditLog
                    {
                        ActorType = "System",
                        ActorId = 0,
                        Action = $"Card locked due to max PIN retries. CardId={card.Id}",
                        CreatedAt = DateTime.UtcNow
                    });
                }

                await _db.SaveChangesAsync();
                return null;
            }

            // Successful login -> reset retry count if needed and log success
            if (card.PinRetryCount != 0)
            {
                card.PinRetryCount = 0;
            }

            await _db.AuditLogs.AddAsync(new AuditLog
            {
                ActorType = "Card",
                ActorId = card.Id,
                Action = $"Successful login for CardId={card.Id}",
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            var token = GenerateJwtToken(card.Id.ToString(), card.CustomerId.ToString(), "Cardholder", card.CardNumber);

            return new AuthResponseDto
            {
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(int.Parse(_config["Jwt:ExpiresMinutes"] ?? "15"))
            };
        }

        public async Task<bool> ChangePinAsync(int cardId, string currentPin, string newPin)
        {
            var card = await _db.Cards.FindAsync(cardId);
            if (card == null) return false;
            if (!PinHasher.Verify(currentPin, card.PinHash))
            {
                // Log failed change attempt
                await _db.AuditLogs.AddAsync(new AuditLog
                {
                    ActorType = "Card",
                    ActorId = cardId,
                    Action = $"Failed PIN change attempt for CardId={cardId}",
                    CreatedAt = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();
                return false;
            }
            if (!IsValidPin(newPin)) return false;

            card.PinHash = PinHasher.HashPin(newPin);
            card.PinRetryCount = 0;
            await _db.AuditLogs.AddAsync(new AuditLog
            {
                ActorType = "Card",
                ActorId = cardId,
                Action = $"PIN changed for CardId={cardId}",
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            return true;
        }

        // Operator authentication handled elsewhere (AuthenticateOperatorAsync exists in AuthService if previously implemented)

        private string GenerateJwtToken(string idClaimValue, string customerIdValue, string role, string name)
        {
            var jwt = _config.GetSection("Jwt");
            var keyString = jwt["Key"] ?? throw new InvalidOperationException("Jwt:Key missing");
            var keyBytes = Encoding.UTF8.GetBytes(keyString);
            var creds = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim("cardId", idClaimValue),
                new Claim("customerId", customerIdValue),
                new Claim(ClaimTypes.NameIdentifier, idClaimValue),
                new Claim(ClaimTypes.Name, name),
                new Claim(ClaimTypes.Role, role)
            };

            var token = new JwtSecurityToken(
                issuer: jwt["Issuer"],
                audience: jwt["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(int.Parse(jwt["ExpiresMinutes"] ?? "15")),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static bool IsValidPin(string pin)
        {
            if (string.IsNullOrWhiteSpace(pin)) return false;
            if (pin.Length < 4 || pin.Length > 6) return false;
            if (!pin.All(char.IsDigit)) return false;

            var seqInc = "0123456789012345";
            var seqDec = "5432109876543210";
            if (seqInc.Contains(pin) || seqDec.Contains(pin)) return false;
            if (pin.Distinct().Count() == 1) return false;

            return true;
        }

        public Task<AuthResponseDto?> AuthenticateOperatorAsync(OperatorLoginDto request)
        {
            throw new NotImplementedException();
        }
    }
}
