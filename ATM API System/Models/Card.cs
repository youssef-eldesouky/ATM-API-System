namespace ATM_API_System.Models
{
    public class Card
    {
        public int Id { get; set; }
        public string CardNumber { get; set; }
        public string PinHash { get; set; }
        public string Status { get; set; } // Active, Locked
        public int PinRetryCount { get; set; }
        public decimal DailyWithdrawalLimit { get; set; } = 10000;
        public decimal DailyWithdrawalUsed { get; set; } = 0;

        public int CustomerId { get; set; }
        public Customer Customer { get; set; }

        // Many-to-Many relation
        public ICollection<CardAccount> CardAccounts { get; set; }
    }



}
