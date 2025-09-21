using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ATM_API_System.Data;
using ATM_API_System.Dtos;
using ATM_API_System.Models;
using ATM_API_System.Utilities;
using Microsoft.EntityFrameworkCore;

namespace ATM_API_System.Services
{
    public class OperatorService : IOperatorService
    {
        private readonly AppDbContext _db;

        public OperatorService(AppDbContext db) => _db = db;

        // ---------------- Existing operator actions ----------------

        public async Task LockCardAsync(int cardId, int operatorId, string? reason = null)
        {
            var card = await _db.Cards.FindAsync(cardId);
            if (card == null) throw new InvalidOperationException("Card not found");
            card.Status = "Locked";
            await _db.AuditLogs.AddAsync(new AuditLog { ActorType = "Operator", ActorId = operatorId, Action = $"Lock card {cardId}. Reason: {reason}", CreatedAt = DateTime.UtcNow });
            await _db.SaveChangesAsync();
        }

        public async Task UnlockCardAsync(int cardId, int operatorId, string? reason = null)
        {
            var card = await _db.Cards.FindAsync(cardId);
            if (card == null) throw new InvalidOperationException("Card not found");
            card.Status = "Active";
            await _db.AuditLogs.AddAsync(new AuditLog { ActorType = "Operator", ActorId = operatorId, Action = $"Unlock card {cardId}. Reason: {reason}", CreatedAt = DateTime.UtcNow });
            await _db.SaveChangesAsync();
        }

        public async Task ResetPinRetriesAsync(int cardId, int operatorId)
        {
            var card = await _db.Cards.FindAsync(cardId);
            if (card == null) throw new InvalidOperationException("Card not found");
            card.PinRetryCount = 0;
            await _db.AuditLogs.AddAsync(new AuditLog { ActorType = "Operator", ActorId = operatorId, Action = $"Reset PIN retries for card {cardId}", CreatedAt = DateTime.UtcNow });
            await _db.SaveChangesAsync();
        }

        public async Task<AtmReconciliation> ReconcileAtmAsync(ReconcileRequestDto req, int operatorId)
        {
            var atm = await _db.AtmInventories.FirstOrDefaultAsync();
            if (atm == null) throw new InvalidOperationException("ATM inventory not found");

            var systemBefore = atm.CashAvailable;
            var difference = req.CountedCash - systemBefore;

            atm.CashAvailable = req.CountedCash;
            atm.UpdatedAt = DateTime.UtcNow;

            var reconciliation = new AtmReconciliation
            {
                AtmId = req.AtmId,
                CountedCash = req.CountedCash,
                SystemCashBefore = systemBefore,
                Difference = difference,
                Notes = req.Notes ?? "",
                OperatorId = operatorId,
                CreatedAt = DateTime.UtcNow
            };

            await _db.AtmReconciliations.AddAsync(reconciliation);
            await _db.AuditLogs.AddAsync(new AuditLog { ActorType = "Operator", ActorId = operatorId, Action = $"Reconcile ATM {req.AtmId}. Counted: {req.CountedCash}, Diff: {difference}", CreatedAt = DateTime.UtcNow });
            await _db.SaveChangesAsync();
            return reconciliation;
        }

        public async Task<IEnumerable<CashOutEventDto>> GetCashOutEventsAsync(DateTime? from, DateTime? to)
        {
            var q = _db.Transactions.AsNoTracking().Where(t => t.Type == "Withdrawal");

            if (from.HasValue) q = q.Where(t => t.CreatedAt >= from.Value);
            if (to.HasValue) q = q.Where(t => t.CreatedAt <= to.Value);

            var list = await q.OrderByDescending(t => t.CreatedAt).Take(1000).ToListAsync();

            return list.Select(t => new CashOutEventDto
            {
                TransactionId = t.Id,
                AccountId = t.AccountId,
                Amount = t.Amount,
                CreatedAt = t.CreatedAt,
                Reference = t.Reference
            }).ToList();
        }

        // -------------------- Reporting / query methods --------------------

        public async Task<IEnumerable<TransactionDto>> GetTransactionsAsync(DateTime? from, DateTime? to, int? accountId, string? type, int limit = 100)
        {
            var q = _db.Transactions.AsNoTracking().AsQueryable();

            if (accountId.HasValue) q = q.Where(t => t.AccountId == accountId.Value);
            if (!string.IsNullOrEmpty(type)) q = q.Where(t => t.Type == type);
            if (from.HasValue) q = q.Where(t => t.CreatedAt >= from.Value);
            if (to.HasValue) q = q.Where(t => t.CreatedAt <= to.Value);

            var txs = await q.OrderByDescending(t => t.CreatedAt).Take(limit).ToListAsync();

            return txs.Select(t => new TransactionDto
            {
                TransactionId = t.Id,
                AccountId = t.AccountId,
                Type = t.Type,
                Amount = t.Amount,
                BalanceAfter = t.BalanceAfter,
                Reference = t.Reference,
                CreatedAt = t.CreatedAt
            }).ToList();
        }

        public async Task<IEnumerable<SecurityLogDto>> GetSecurityLogsAsync(DateTime? from, DateTime? to, int? cardId, int limit = 100)
        {
            var q = _db.AuditLogs.AsNoTracking().AsQueryable();

            if (from.HasValue) q = q.Where(a => a.CreatedAt >= from.Value);
            if (to.HasValue) q = q.Where(a => a.CreatedAt <= to.Value);

            if (cardId.HasValue)
            {
                q = q.Where(a => a.ActorId == cardId.Value || a.Action.Contains($"CardId={cardId.Value}") || a.Action.Contains($"card {cardId.Value}"));
            }
            else
            {
                q = q.Where(a => a.Action.Contains("PIN") || a.Action.ToLower().Contains("lock") || a.Action.ToLower().Contains("unlock") || a.Action.ToLower().Contains("failed"));
            }

            var logs = await q.OrderByDescending(a => a.CreatedAt).Take(limit).ToListAsync();

            return logs.Select(l => new SecurityLogDto
            {
                Id = l.Id,
                ActorType = l.ActorType,
                ActorId = l.ActorId,
                Action = l.Action,
                CreatedAt = l.CreatedAt
            }).ToList();
        }

        // -------------------- Seeding --------------------

        public async Task<OperatorSeedResponseDto> SeedCustomerAccountAsync(OperatorSeedRequestDto req, int operatorId)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (string.IsNullOrWhiteSpace(req.CustomerName)) throw new ArgumentException("CustomerName is required");

            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var customer = new Customer
                {
                    Name = req.CustomerName,
                    Status = "Active"
                };
                await _db.Customers.AddAsync(customer);
                await _db.SaveChangesAsync();

                var createdAccounts = new List<AccountSummaryDto>();
                if (req.Accounts != null && req.Accounts.Any())
                {
                    foreach (var a in req.Accounts)
                    {
                        var acc = new Account
                        {
                            CustomerId = customer.Id,
                            Type = a.Type ?? "Checking",
                            Balance = a.InitialBalance
                        };
                        await _db.Accounts.AddAsync(acc);
                        await _db.SaveChangesAsync();
                        createdAccounts.Add(new AccountSummaryDto { AccountId = acc.Id, Type = acc.Type, Balance = acc.Balance });
                    }
                }

                Card? card = null;
                if (!string.IsNullOrWhiteSpace(req.CardNumber) && !string.IsNullOrWhiteSpace(req.Pin))
                {
                    card = new Card
                    {
                        CardNumber = req.CardNumber,
                        PinHash = PinHasher.HashPin(req.Pin),
                        Status = "Active",
                        PinRetryCount = 0,
                        DailyWithdrawalLimit = req.CardDailyLimit ?? 1000m,
                        DailyWithdrawalUsed = 0m,
                        CustomerId = customer.Id
                    };
                    await _db.Cards.AddAsync(card);
                    await _db.SaveChangesAsync();

                    try
                    {
                        if (_db.Model.FindEntityType(typeof(CardAccount)) != null)
                        {
                            var firstAcc = createdAccounts.FirstOrDefault();
                            if (firstAcc != null)
                            {
                                var ca = new CardAccount { CardId = card.Id, AccountId = firstAcc.AccountId };
                                await _db.AddAsync(ca);
                                await _db.SaveChangesAsync();
                            }
                        }
                    }
                    catch
                    {
                        // ignore if CardAccount not present
                    }
                }

                await _db.AuditLogs.AddAsync(new AuditLog
                {
                    ActorType = "Operator",
                    ActorId = operatorId,
                    Action = $"Seeded customer '{customer.Name}' id={customer.Id} accounts={createdAccounts.Count} cardId={(card?.Id.ToString() ?? "none")}",
                    CreatedAt = DateTime.UtcNow
                });

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return new OperatorSeedResponseDto
                {
                    CustomerId = customer.Id,
                    Accounts = createdAccounts,
                    CardId = card?.Id
                };
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        // -------------------- CSV Export helpers --------------------

        public async Task<byte[]> ExportTransactionsCsvAsync(DateTime? from, DateTime? to, int? accountId, string? type, int limit = 1000)
        {
            var transactions = (await GetTransactionsAsync(from, to, accountId, type, limit)).ToList();

            var sb = new StringBuilder();
            sb.AppendLine("TransactionId,AccountId,Type,Amount,BalanceAfter,Reference,CreatedAt");

            foreach (var t in transactions)
            {
                string Escape(string s)
                {
                    if (s == null) return "";
                    if (s.Contains('"')) s = s.Replace("\"", "\"\"");
                    if (s.Contains(',') || s.Contains('"') || s.Contains('\n') || s.Contains('\r')) return $"\"{s}\"";
                    return s;
                }

                var line = string.Join(",",
                    t.TransactionId.ToString(),
                    t.AccountId.ToString(),
                    Escape(t.Type),
                    t.Amount.ToString(CultureInfo.InvariantCulture),
                    t.BalanceAfter.ToString(CultureInfo.InvariantCulture),
                    Escape(t.Reference),
                    t.CreatedAt.ToString("o", CultureInfo.InvariantCulture)
                );
                sb.AppendLine(line);
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        public async Task<byte[]> ExportSecurityLogsCsvAsync(DateTime? from, DateTime? to, int? cardId, int limit = 1000)
        {
            var logs = (await GetSecurityLogsAsync(from, to, cardId, limit)).ToList();

            var sb = new StringBuilder();
            sb.AppendLine("Id,ActorType,ActorId,Action,CreatedAt");

            foreach (var l in logs)
            {
                string Escape(string s)
                {
                    if (s == null) return "";
                    if (s.Contains('"')) s = s.Replace("\"", "\"\"");
                    if (s.Contains(',') || s.Contains('"') || s.Contains('\n') || s.Contains('\r')) return $"\"{s}\"";
                    return s;
                }

                var line = string.Join(",",
                    l.Id.ToString(),
                    Escape(l.ActorType),
                    l.ActorId.ToString(),
                    Escape(l.Action),
                    l.CreatedAt.ToString("o", CultureInfo.InvariantCulture)
                );
                sb.AppendLine(line);
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }
    }
}
