using System.Security.Cryptography; 

namespace KoperasiTentera.Service.Services.Registration
{
    /// <summary>
    /// Salted PBKDF2 hashing for the user's 6-digit PIN. The plain PIN is
    /// never persisted or logged - only the hash produced here is stored.
    /// </summary>
    public static class PinHasher
    {
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int Iterations = 100_000;

        public static string Hash(string pin)
        {
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var hash = Rfc2898DeriveBytes.Pbkdf2(pin, salt, Iterations, HashAlgorithmName.SHA256, HashSize);

            return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }

        public static bool Verify(string pin, string hashed)
        {
            var parts = hashed.Split('.', 3);
            if (parts.Length != 3)
                return false;

            var iterations = int.Parse(parts[0]);
            var salt = Convert.FromBase64String(parts[1]);
            var expectedHash = Convert.FromBase64String(parts[2]);

            var actualHash = Rfc2898DeriveBytes.Pbkdf2(pin, salt, iterations, HashAlgorithmName.SHA256, expectedHash.Length);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
    }
}
