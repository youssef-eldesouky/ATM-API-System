using System;

namespace ATM_API_System.Dtos
{
    public class SecurityLogDto
    {
        public int Id { get; set; }
        public string ActorType { get; set; } = "";
        public int ActorId { get; set; }
        public string Action { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }
}