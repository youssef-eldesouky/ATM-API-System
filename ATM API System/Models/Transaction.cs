namespace ATM_API_System.Models
{
    public class Transaction
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public string Type { get; set; } // Withdrawal, Deposit, Transfer
        public decimal Amount { get; set; }
        public decimal BalanceAfter { get; set; }
        public string Reference { get; set; } // Unique transaction reference
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public Account Account { get; set; }
    }

}
