using System.Security.Cryptography;
using System.Text;

namespace ProjectBuilder.Application.Modeling;

internal static class ModelRequestFingerprint
{
    internal static string Create(params string?[] values)
    {
        var canonical = string.Join('\n', values.Select(value => value?.Trim() ?? string.Empty));
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }
}
