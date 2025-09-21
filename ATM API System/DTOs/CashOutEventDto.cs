using System;

namespace ATM_API_System.Dtos
{
    public class CashOutEventDto
    {
        public int TransactionId { get; set; }
        public int AccountId { get; set; }
        public decimal Amount { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Reference { get; set; }
    }
}