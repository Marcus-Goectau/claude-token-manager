using System.Security.Cryptography;
using System.Text;

namespace ClaudeTokenManager.Services;

public static class TokenUtil
{
    public static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public static string Hint(string token) =>
        token.Length <= 8 ? new string('*', token.Length) : $"{token[..3]}...{token[^4..]}";
}
