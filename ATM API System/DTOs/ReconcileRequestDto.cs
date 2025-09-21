namespace ATM_API_System.Dtos
{
    public class ReconcileRequestDto
    {
        public int AtmId { get; set; } = 1;
        public decimal CountedCash { get; set; }
        public string Notes { get; set; } = "";
    }
}