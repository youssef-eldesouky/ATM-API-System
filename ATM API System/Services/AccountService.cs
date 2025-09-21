using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ATM_API_System.Data;
using ATM_API_System.Dtos;
using ATM_API_System.DTOs;
using ATM_API_System.Models;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace ATM_API_System.Services
{
    public class AccountService : IAccountService
    {
        private readonly AppDbContext _db;
        private readonly IMapper _mapper;

        public AccountService(AppDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<BalanceResponseDto?> GetBalanceAsync(int accountId, int requestingCustomerId)
        {
            var account = await _db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == accountId);
            if (account == null || account.CustomerId != requestingCustomerId) return null;
            return new BalanceResponseDto { AccountId = account.Id, Balance = account.Balance };
        }

        public async Task<TransactionResponseDto> WithdrawAsync(int accountId, decimal amount, int cardId, int customerId)
        {
            if (amount <= 0) throw new ArgumentException("Amount must be > 0");

            using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var account = await _db.Accounts.FirstOrDefaultAsync(a => a.Id == accountId);
                if (account == null) throw new InvalidOperationException("Account not found");
                if (account.CustomerId != customerId) throw new UnauthorizedAccessException("Not allowed");
                if (account.Balance < amount) throw new InvalidOperationException("Insufficient funds");

                var atm = await _db.AtmInventories.FirstOrDefaultAsync();
                if (atm == null || atm.CashAvailable < amount) throw new InvalidOperationException("ATM has insufficient cash");

                account.Balance -= amount;
                atm.CashAvailable -= amount;

                var trx = new Transaction
                {
                    AccountId = account.Id,
                    Type = "Withdrawal",
                    Amount = amount,
                    BalanceAfter = account.Balance,
                    Reference = GenerateTxRef(),
                    CreatedAt = DateTime.UtcNow
                };
                await _db.Transactions.AddAsync(trx);

                await _db.AuditLogs.AddAsync(new AuditLog
                {
                    ActorType = "Card",
                    ActorId = cardId,
                    Action = $"Withdraw {amount} from Account {accountId}",
                    CreatedAt = DateTime.UtcNow
                });

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return _mapper.Map<TransactionResponseDto>(trx);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<TransactionResponseDto> DepositAsync(int accountId, decimal amount, int cardId, int customerId)
        {
            if (amount <= 0) throw new ArgumentException("Amount must be > 0");

            using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var account = await _db.Accounts.FirstOrDefaultAsync(a => a.Id == accountId);
                if (account == null) throw new InvalidOperationException("Account not found");
                if (account.CustomerId != customerId) throw new UnauthorizedAccessException("Not allowed");

                account.Balance += amount;

                var trx = new Transaction
                {
                    AccountId = account.Id,
                    Type = "Deposit",
                    Amount = amount,
                    BalanceAfter = account.Balance,
                    Reference = GenerateTxRef(),
                    CreatedAt = DateTime.UtcNow
                };
                await _db.Transactions.AddAsync(trx);

                await _db.AuditLogs.AddAsync(new AuditLog
                {
                    ActorType = "Card",
                    ActorId = cardId,
                    Action = $"Deposit {amount} to Account {accountId}",
                    CreatedAt = DateTime.UtcNow
                });

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return _mapper.Map<TransactionResponseDto>(trx);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<TransactionResponseDto> TransferAsync(int fromAccountId, int toAccountId, decimal amount, int cardId, int customerId)
        {
            if (amount <= 0) throw new ArgumentException("Amount must be > 0");
            if (fromAccountId == toAccountId) throw new ArgumentException("Cannot transfer to same account");

            using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var from = await _db.Accounts.FirstOrDefaultAsync(a => a.Id == fromAccountId);
                var to = await _db.Accounts.FirstOrDefaultAsync(a => a.Id == toAccountId);

                if (from == null || to == null) throw new InvalidOperationException("Account not found");
                if (from.CustomerId != customerId || to.CustomerId != customerId) throw new UnauthorizedAccessException("Accounts must belong to same customer");
                if (from.Balance < amount) throw new InvalidOperationException("Insufficient funds");

                from.Balance -= amount;
                to.Balance += amount;

                var reference = GenerateTxRef();

                var trxOut = new Transaction
                {
                    AccountId = from.Id,
                    Type = "Transfer-Out",
                    Amount = amount,
                    BalanceAfter = from.Balance,
                    Reference = reference,
                    CreatedAt = DateTime.UtcNow
                };
                await _db.Transactions.AddAsync(trxOut);

                var trxIn = new Transaction
                {
                    AccountId = to.Id,
                    Type = "Transfer-In",
                    Amount = amount,
                    BalanceAfter = to.Balance,
                    Reference = reference,
                    CreatedAt = DateTime.UtcNow
                };
                await _db.Transactions.AddAsync(trxIn);

                await _db.AuditLogs.AddAsync(new AuditLog
                {
                    ActorType = "Card",
                    ActorId = cardId,
                    Action = $"Transfer {amount} from {fromAccountId} to {toAccountId}",
                    CreatedAt = DateTime.UtcNow
                });

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return _mapper.Map<TransactionResponseDto>(trxOut);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<IEnumerable<TransactionResponseDto>> GetMiniStatementAsync(int accountId, int count = 10)
        {
            if (count <= 0) count = 10;
            if (count > 100) count = 100;

            var txs = await _db.Transactions
                .AsNoTracking()
                .Where(t => t.AccountId == accountId)
                .OrderByDescending(t => t.CreatedAt)
                .Take(count)
                .ToListAsync();

            return _mapper.Map<IEnumerable<TransactionResponseDto>>(txs);
        }

        private static string GenerateTxRef()
        {
            return $"TRX-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N").Substring(0, 6)}";
        }
    }
}
