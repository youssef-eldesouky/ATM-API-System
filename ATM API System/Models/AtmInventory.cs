namespace ATM_API_System.Models
{
    public class AtmInventory
    {
        public int Id { get; set; }
        public int AtmId { get; set; }
        public decimal CashAvailable { get; set; } // Total available cash
        public DateTime UpdatedAt { get; set; }
    }

}
