namespace ATM_API_System.Models
{
    public class CardAccount
    {
        public int CardId { get; set; }
        public Card Card { get; set; }

        public int AccountId { get; set; }
        public Account Account { get; set; }
    }

}
