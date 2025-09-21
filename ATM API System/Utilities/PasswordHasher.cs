using System;
using System.Security.Cryptography;

namespace ATM_API_System.Utilities
{
    public static class PasswordHasher
    {
        private const int SaltSize = 16; // 128 bit
        private const int KeySize = 32;  // 256 bit
        private const int Iterations = 100_000;

        public static (byte[] Hash, byte[] Salt) HashPassword(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            using var derive = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            var key = derive.GetBytes(KeySize);
            return (key, salt);
        }

        public static bool Verify(string password, byte[] storedHash, byte[] storedSalt)
        {
            using var derive = new Rfc2898DeriveBytes(password, storedSalt, Iterations, HashAlgorithmName.SHA256);
            var key = derive.GetBytes(KeySize);
            return CryptographicOperations.FixedTimeEquals(key, storedHash);
        }
    }
}