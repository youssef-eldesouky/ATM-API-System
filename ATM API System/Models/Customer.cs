using System.Security.Principal;

namespace ATM_API_System.Models
{
    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Status { get; set; } // Active, Inactive

        // Navigation properties
        public ICollection<Card> Cards { get; set; }
        public ICollection<Account> Accounts { get; set; }
    }


}
