using System.Security.Cryptography;

namespace GeoURPWebApi.Services;

public sealed class PasswordService
{
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
    }

    public string GenerateTemporaryPassword(int length = 12)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$%";
        var passwordChars = new char[length];

        for (var i = 0; i < length; i++)
        {
            passwordChars[i] = chars[RandomNumberGenerator.GetInt32(chars.Length)];
        }

        return new string(passwordChars);
    }
}
