using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ATM_API_System.Data;
using ATM_API_System.Dtos;
using ATM_API_System.DTOs;

namespace ATM_API_System.Services
{
    public interface IOperatorService
    {
        // existing operator functions we added earlier
        Task LockCardAsync(int cardId, int operatorId, string? reason = null);
        Task UnlockCardAsync(int cardId, int operatorId, string? reason = null);
        Task ResetPinRetriesAsync(int cardId, int operatorId);
        Task<AtmReconciliation> ReconcileAtmAsync(ReconcileRequestDto req, int operatorId);
        Task<IEnumerable<CashOutEventDto>> GetCashOutEventsAsync(DateTime? from, DateTime? to);

        // new features:
        Task<IEnumerable<TransactionDto>> GetTransactionsAsync(DateTime? from, DateTime? to, int? accountId, string? type, int limit = 100);
        Task<IEnumerable<SecurityLogDto>> GetSecurityLogsAsync(DateTime? from, DateTime? to, int? cardId, int limit = 100);
        Task<OperatorSeedResponseDto> SeedCustomerAccountAsync(OperatorSeedRequestDto req, int operatorId);
        Task<byte[]> ExportTransactionsCsvAsync(DateTime? from, DateTime? to, int? accountId, string? type, int limit = 1000);
        Task<byte[]> ExportSecurityLogsCsvAsync(DateTime? from, DateTime? to, int? cardId, int limit = 1000);

    }
}