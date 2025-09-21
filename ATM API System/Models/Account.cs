using System.Transactions;

namespace ATM_API_System.Models
{
    public class Account
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string Type { get; set; } // e.g., Checking, Savings
        public decimal Balance { get; set; }

        public Customer Customer { get; set; }
        public ICollection<Transaction> Transactions { get; set; }

        // Many-to-Many relation
        public ICollection<CardAccount> CardAccounts { get; set; }
    }


}
