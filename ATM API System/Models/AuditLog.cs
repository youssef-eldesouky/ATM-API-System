namespace ATM_API_System.Models
{
    public class AuditLog
    {
        public int Id { get; set; }
        public string ActorType { get; set; } // Cardholder, Operator
        public int ActorId { get; set; } // ID of Card or Operator
        public string Action { get; set; } // e.g., "Withdraw $100"
        public DateTime CreatedAt { get; set; }
    }

}
