using System;
using System.Security.Cryptography;

namespace Cinema.Authentication;

internal static class AuthUtils
{
    public static string HashPassword(string password)
    {
        using var rng = RandomNumberGenerator.Create();
        var salt = new byte[16];
        rng.GetBytes(salt);

        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(32);

        var result = new byte[1 + salt.Length + hash.Length];
        result[0] = 1; // версия
        Buffer.BlockCopy(salt, 0, result, 1, salt.Length);
        Buffer.BlockCopy(hash, 0, result, 1 + salt.Length, hash.Length);

        return Convert.ToBase64String(result);
    }

    public static bool VerifyPassword(string password, string storedHash)
    {
        byte[] data;
        try
        {
            data = Convert.FromBase64String(storedHash);
        }
        catch
        {
            return false;
        }

        if (data.Length < 1 + 16 + 32)
            return false;

        var version = data[0];
        if (version != 1)
            return false;

        var salt = new byte[16];
        Buffer.BlockCopy(data, 1, salt, 0, salt.Length);

        var hash = new byte[32];
        Buffer.BlockCopy(data, 1 + salt.Length, hash, 0, hash.Length);

        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
        var computed = pbkdf2.GetBytes(32);

        return CryptographicOperations.FixedTimeEquals(hash, computed);
    }
}

