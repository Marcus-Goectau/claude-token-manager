using System.Security.Cryptography;
using System.Text;

namespace ClaudeTokenManager.Services;

public static class TokenUtil
{
    public const int MaxLength = 4096;

    public static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public static string Hint(string token) =>
        token.Length <= 12 ? new string('*', token.Length) : $"{token[..3]}...{token[^4..]}";

    // Returns a reason the token is unusable, or null if it looks fine.
    public static string? Validate(string token)
    {
        if (token.Length == 0) return "The token is empty.";
        if (token.Length > MaxLength) return $"The token is longer than {MaxLength} characters.";
        if (token.Any(c => char.IsWhiteSpace(c) || char.IsControl(c)))
            return "The token contains spaces, line breaks or other control characters. Check that it was pasted correctly.";
        return null;
    }
}
