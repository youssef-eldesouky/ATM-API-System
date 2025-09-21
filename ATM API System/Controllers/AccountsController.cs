using System.Linq;
using ATM_API_System.Dtos;
using ATM_API_System.DTOs;
using ATM_API_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATM_API_System.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountsController : ControllerBase
    {
        private readonly IAccountService _svc;

        public AccountsController(IAccountService svc) => _svc = svc;

        // GET /api/accounts/{accountId}/balance
        [Authorize]
        [HttpGet("{accountId}/balance")]
        public async Task<IActionResult> GetBalance(int accountId)
        {
            var custClaim = User.Claims.FirstOrDefault(c => c.Type == "customerId")?.Value;
            if (!int.TryParse(custClaim, out var customerId)) return Forbid();

            var bal = await _svc.GetBalanceAsync(accountId, customerId);
            if (bal == null) return NotFound();
            return Ok(bal);
        }

        // POST /api/accounts/{accountId}/withdraw
        [Authorize]
        [HttpPost("{accountId}/withdraw")]
        public async Task<IActionResult> Withdraw(int accountId, [FromBody] WithdrawRequestDto req)
        {
            var cardClaim = User.Claims.FirstOrDefault(c => c.Type == "cardId")?.Value;
            var custClaim = User.Claims.FirstOrDefault(c => c.Type == "customerId")?.Value;
            if (!int.TryParse(cardClaim, out var cardId) || !int.TryParse(custClaim, out var customerId)) return Forbid();

            try
            {
                var trx = await _svc.WithdrawAsync(accountId, req.Amount, cardId, customerId);
                return Ok(trx);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // POST /api/accounts/{accountId}/deposit
        [Authorize]
        [HttpPost("{accountId}/deposit")]
        public async Task<IActionResult> Deposit(int accountId, [FromBody] DepositRequestDto req)
        {
            var cardClaim = User.Claims.FirstOrDefault(c => c.Type == "cardId")?.Value;
            var custClaim = User.Claims.FirstOrDefault(c => c.Type == "customerId")?.Value;
            if (!int.TryParse(cardClaim, out var cardId) || !int.TryParse(custClaim, out var customerId)) return Forbid();

            try
            {
                var trx = await _svc.DepositAsync(accountId, req.Amount, cardId, customerId);
                return Ok(trx);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // POST /api/accounts/transfer
        [Authorize]
        [HttpPost("transfer")]
        public async Task<IActionResult> Transfer([FromBody] TransferRequestDto req)
        {
            var cardClaim = User.Claims.FirstOrDefault(c => c.Type == "cardId")?.Value;
            var custClaim = User.Claims.FirstOrDefault(c => c.Type == "customerId")?.Value;
            if (!int.TryParse(cardClaim, out var cardId) || !int.TryParse(custClaim, out var customerId)) return Forbid();

            try
            {
                var trx = await _svc.TransferAsync(req.FromAccountId, req.ToAccountId, req.Amount, cardId, customerId);
                return Ok(trx);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // GET /api/accounts/{accountId}/statement?count=10
        [Authorize]
        [HttpGet("{accountId}/statement")]
        public async Task<IActionResult> MiniStatement(int accountId, [FromQuery] int count = 10)
        {
            var custClaim = User.Claims.FirstOrDefault(c => c.Type == "customerId")?.Value;
            if (!int.TryParse(custClaim, out var customerId)) return Forbid();

            // Optionally enforce ownership: check that account belongs to customer
            var statement = await _svc.GetMiniStatementAsync(accountId, count);
            return Ok(statement);
        }
    }
}
