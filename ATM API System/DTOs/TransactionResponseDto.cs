using System;

namespace ATM_API_System.Dtos
{
    public class TransactionResponseDto
    {
        public int TransactionId { get; set; }
        public string Reference { get; set; } = "";
        public string Type { get; set; } = "";
        public decimal Amount { get; set; }
        public decimal BalanceAfter { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}