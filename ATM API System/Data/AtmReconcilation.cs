using System;

namespace ATM_API_System.Data
{
    public class AtmReconciliation
    {
        public int Id { get; set; }
        public int AtmId { get; set; } = 1;
        public decimal CountedCash { get; set; }
        public decimal SystemCashBefore { get; set; }
        public decimal Difference { get; set; }
        public string Notes { get; set; } = "";
        public int OperatorId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}