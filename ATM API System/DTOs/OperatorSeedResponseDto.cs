using System.Collections.Generic;

namespace ATM_API_System.Dtos
{
    public class OperatorSeedResponseDto
    {
        public int CustomerId { get; set; }
        public List<AccountSummaryDto> Accounts { get; set; } = new();
        public int? CardId { get; set; }
    }

    public class AccountSummaryDto
    {
        public int AccountId { get; set; }
        public string Type { get; set; } = "";
        public decimal Balance { get; set; }
    }
}