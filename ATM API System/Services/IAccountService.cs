using ATM_API_System.Dtos;
using System.Collections.Generic;
using System.Threading.Tasks;
using ATM_API_System.DTOs;

namespace ATM_API_System.Services
{
    public interface IAccountService
    {
        Task<BalanceResponseDto?> GetBalanceAsync(int accountId, int requestingCustomerId);
        Task<TransactionResponseDto> WithdrawAsync(int accountId, decimal amount, int cardId, int customerId);
        Task<TransactionResponseDto> DepositAsync(int accountId, decimal amount, int cardId, int customerId);
        Task<TransactionResponseDto> TransferAsync(int fromAccountId, int toAccountId, decimal amount, int cardId, int customerId);
        Task<IEnumerable<TransactionResponseDto>> GetMiniStatementAsync(int accountId, int count = 10);
    }
}