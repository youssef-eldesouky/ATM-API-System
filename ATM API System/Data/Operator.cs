using System;

namespace ATM_API_System.Data
{
    public class Operator
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public byte[] PasswordHash { get; set; } = Array.Empty<byte>();
        public byte[] PasswordSalt { get; set; } = Array.Empty<byte>();
        public string Role { get; set; } = "Operator";
        public string Status { get; set; } = "Active"; // Active, Disabled
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}