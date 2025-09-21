using System.Security.Cryptography;
using System.Text;

namespace ATM_API_System.Utilities
{
    public static class PinHasher
    {
        public static string HashPin(string pin)
        {
            if (string.IsNullOrEmpty(pin)) return string.Empty;
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(pin);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToHexString(hash);
        }

        public static bool Verify(string pin, string storedHash)
        {
            if (string.IsNullOrEmpty(pin) || string.IsNullOrEmpty(storedHash)) return false;
            var computed = HashPin(pin);
            return string.Equals(computed, storedHash, StringComparison.OrdinalIgnoreCase);
        }
    }
}