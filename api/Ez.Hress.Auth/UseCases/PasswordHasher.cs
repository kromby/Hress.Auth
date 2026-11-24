using System.Security.Cryptography;
using System.Text;

namespace Ez.Hress.Auth.UseCases;

/// <summary>
/// Password hashing compatible with Hress.Org's existing hashes:
/// PBKDF2-HMACSHA512, 10,000 iterations, 32 bytes, salt = UTF-32 bytes of the global salt, Base64 output.
/// Equivalent to Microsoft.AspNetCore.Cryptography.KeyDerivation.Pbkdf2 as Hress.Org used it.
/// </summary>
public static class PasswordHasher
{
    private const int Iterations = 10000;
    private const int OutputBytes = 256 / 8;

    public static string Hash(string password, string salt) =>
        Convert.ToBase64String(Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            Encoding.UTF32.GetBytes(salt),
            Iterations,
            HashAlgorithmName.SHA512,
            OutputBytes));

    /// <summary>Constant-time comparison of two Base64 hashes.</summary>
    public static bool Matches(string computedHash, string? storedHash) =>
        storedHash != null &&
        CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(computedHash), Encoding.ASCII.GetBytes(storedHash));
}
