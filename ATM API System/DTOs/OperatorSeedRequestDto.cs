using System.Collections.Generic;

namespace ATM_API_System.Dtos
{
    public class OperatorSeedRequestDto
    {
        public string CustomerName { get; set; } = "";
        public List<AccountSeedDto> Accounts { get; set; } = new();
        public string? CardNumber { get; set; } // optional
        public string? Pin { get; set; } // optional (plain PIN, will be hashed)
        public decimal? CardDailyLimit { get; set; }
    }

    public class AccountSeedDto
    {
        public string? Type { get; set; } = "Checking";
        public decimal InitialBalance { get; set; } = 0m;
    }
}