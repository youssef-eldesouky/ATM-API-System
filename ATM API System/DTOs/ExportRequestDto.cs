using System;

namespace ATM_API_System.Dtos
{
    public class ExportRequestDto
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public int? AccountId { get; set; } // for transactions export; reused for security logs (card id)
        public string? Type { get; set; } // transaction type (Withdrawal, Deposit, etc.)
        public int? Limit { get; set; } = 1000;
    }
}