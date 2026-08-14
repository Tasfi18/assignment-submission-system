using System.Security.Cryptography;
using AssignmentSystem.Application.Common.Interfaces;

namespace AssignmentSystem.Infrastructure.Security;

/// <summary>
/// PBKDF2-HMAC-SHA256 password hashing using only the .NET base class library.
///
/// Format: PBKDF2$sha256$&lt;iterations&gt;$&lt;base64 salt&gt;$&lt;base64 hash&gt;
///
/// A dedicated algorithm identifier and iteration count are stored with every hash so the work factor
/// can be raised later without invalidating existing passwords. Verification is done with a
/// fixed-time comparison to avoid leaking information through timing.
/// </summary>
public class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const string Prefix = "PBKDF2";
    private const string Algorithm = "sha256";
    private const int DefaultIterations = 100_000;
    private const int SaltSize = 16;
    private const int KeySize = 32;

    public string Hash(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            throw new ArgumentException("Password must not be empty.", nameof(password));
        }

        var salt = RandomNumberGenerator.GetBytes(SaltSize);

        var key = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            DefaultIterations,
            HashAlgorithmName.SHA256,
            KeySize);

        return string.Join('$', Prefix, Algorithm, DefaultIterations.ToString(), Convert.ToBase64String(salt), Convert.ToBase64String(key));
    }

    public bool Verify(string password, string passwordHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(passwordHash))
        {
            return false;
        }

        var parts = passwordHash.Split('$');

        if (parts.Length != 5 || parts[0] != Prefix || parts[1] != Algorithm)
        {
            return false;
        }

        if (!int.TryParse(parts[2], out var iterations) || iterations <= 0)
        {
            return false;
        }

        byte[] salt;
        byte[] expected;

        try
        {
            salt = Convert.FromBase64String(parts[3]);
            expected = Convert.FromBase64String(parts[4]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actual = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            expected.Length);

        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
