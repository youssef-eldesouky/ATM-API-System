namespace ATM_API_System.DTOs
{
    public class ChangePinRequestDto
    {
        public string CurrentPin { get; set; }
        public string NewPin { get; set; }
    }
}