using ATM_API_System.Dtos;
using ATM_API_System.DTOs;
using ATM_API_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATM_API_System.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _auth;
        public AuthController(IAuthService auth) => _auth = auth;

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            var token = await _auth.AuthenticateAsync(request);
            if (token == null) return Unauthorized(new { Message = "Invalid credentials or card locked." });
            return Ok(token);
        }

        [Authorize]
        [HttpPost("change-pin")]
        public async Task<IActionResult> ChangePin([FromBody] ChangePinRequestDto req)
        {
            var cardIdClaim = User.Claims.FirstOrDefault(c => c.Type == "cardId")?.Value;
            if (!int.TryParse(cardIdClaim, out var cardId)) return Forbid();

            var ok = await _auth.ChangePinAsync(cardId, req.CurrentPin, req.NewPin);
            if (!ok) return BadRequest(new { Message = "Pin change failed (invalid current pin or policy violation)." });
            return Ok(new { Message = "PIN changed successfully." });
        }

        [HttpPost("operator-login")]
        public async Task<IActionResult> OperatorLogin([FromBody] OperatorLoginDto req)
        {
            var token = await _auth.AuthenticateOperatorAsync(req);
            if (token == null) return Unauthorized(new { Message = "Invalid operator credentials." });
            return Ok(token);
        }

    }
}