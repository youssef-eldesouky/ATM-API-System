using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ATM_API_System.Dtos;
using ATM_API_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATM_API_System.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Operator")]
    public class OperatorController : ControllerBase
    {
        private readonly IOperatorService _svc;

        public OperatorController(IOperatorService svc) => _svc = svc;

        private int GetOperatorIdFromClaims()
        {
            var idClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "cardId")?.Value;
            if (int.TryParse(idClaim, out var id)) return id;
            throw new InvalidOperationException("Operator id missing in token");
        }

        [HttpPost("cards/{cardId}/lock")]
        public async Task<IActionResult> LockCard(int cardId, [FromBody] string? reason = null)
        {
            var opId = GetOperatorIdFromClaims();
            await _svc.LockCardAsync(cardId, opId, reason);
            return Ok(new { Message = "Card locked" });
        }

        [HttpPost("cards/{cardId}/unlock")]
        public async Task<IActionResult> UnlockCard(int cardId, [FromBody] string? reason = null)
        {
            var opId = GetOperatorIdFromClaims();
            await _svc.UnlockCardAsync(cardId, opId, reason);
            return Ok(new { Message = "Card unlocked" });
        }

        [HttpPost("cards/{cardId}/reset-pin-retries")]
        public async Task<IActionResult> ResetPinRetries(int cardId)
        {
            var opId = GetOperatorIdFromClaims();
            await _svc.ResetPinRetriesAsync(cardId, opId);
            return Ok(new { Message = "Pin retries reset" });
        }

        [HttpPost("atm/reconcile")]
        public async Task<IActionResult> ReconcileAtm([FromBody] ReconcileRequestDto req)
        {
            var opId = GetOperatorIdFromClaims();
            var rec = await _svc.ReconcileAtmAsync(req, opId);
            return Ok(new
            {
                rec.Id,
                rec.AtmId,
                rec.CountedCash,
                rec.SystemCashBefore,
                rec.Difference,
                rec.CreatedAt
            });
        }

        [HttpGet("cash-outs")]
        public async Task<IActionResult> GetCashOutEvents([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        {
            var events = await _svc.GetCashOutEventsAsync(from, to);
            return Ok(events);
        }

        // ---- New operator-only read endpoints ----

        // GET /api/operator/transactions?from=&to=&accountId=&type=&limit=
        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactions([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null,
            [FromQuery] int? accountId = null, [FromQuery] string? type = null, [FromQuery] int limit = 100)
        {
            var list = await _svc.GetTransactionsAsync(from, to, accountId, type, limit);
            return Ok(list);
        }

        // GET /api/operator/security-logs?from=&to=&cardId=&limit=
        [HttpGet("security-logs")]
        public async Task<IActionResult> GetSecurityLogs([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null,
            [FromQuery] int? cardId = null, [FromQuery] int limit = 100)
        {
            var list = await _svc.GetSecurityLogsAsync(from, to, cardId, limit);
            return Ok(list);
        }

        // POST /api/operator/seed
        [HttpPost("seed")]
        public async Task<IActionResult> SeedCustomerAccount([FromBody] OperatorSeedRequestDto req)
        {
            var opId = GetOperatorIdFromClaims();
            var res = await _svc.SeedCustomerAccountAsync(req, opId);
            return Ok(res);
        }

        // GET /api/operator/export/transactions?from=&to=&accountId=&type=&limit=
        [HttpGet("export/transactions")]
        public async Task<IActionResult> ExportTransactions([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null,
            [FromQuery] int? accountId = null, [FromQuery] string? type = null, [FromQuery] int limit = 1000)
        {
            var opId = GetOperatorIdFromClaims(); // ensure operator auth
            var bytes = await _svc.ExportTransactionsCsvAsync(from, to, accountId, type, limit);
            var filename = $"transactions_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
            return File(bytes, "text/csv; charset=utf-8", filename);
        }

        // POST /api/operator/export/transactions (body contains filters)
        [HttpPost("export/transactions")]
        public async Task<IActionResult> ExportTransactionsPost([FromBody] ExportRequestDto req)
        {
            var opId = GetOperatorIdFromClaims();
            var bytes = await _svc.ExportTransactionsCsvAsync(req.From, req.To, req.AccountId, req.Type, req.Limit ?? 1000);
            var filename = $"transactions_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
            return File(bytes, "text/csv; charset=utf-8", filename);
        }

        // GET /api/operator/export/security-logs?from=&to=&cardId=&limit=
        [HttpGet("export/security-logs")]
        public async Task<IActionResult> ExportSecurityLogs([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null,
            [FromQuery] int? cardId = null, [FromQuery] int limit = 1000)
        {
            var opId = GetOperatorIdFromClaims();
            var bytes = await _svc.ExportSecurityLogsCsvAsync(from, to, cardId, limit);
            var filename = $"security_logs_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
            return File(bytes, "text/csv; charset=utf-8", filename);
        }

        // POST /api/operator/export/security-logs
        [HttpPost("export/security-logs")]
        public async Task<IActionResult> ExportSecurityLogsPost([FromBody] ExportRequestDto req)
        {
            var opId = GetOperatorIdFromClaims();
            var bytes = await _svc.ExportSecurityLogsCsvAsync(req.From, req.To, req.AccountId, req.Limit ?? 1000);
            var filename = $"security_logs_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
            return File(bytes, "text/csv; charset=utf-8", filename);
        }

    }
}
