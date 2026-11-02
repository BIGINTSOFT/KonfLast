using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace ERP.Core.Application.Common;

/// <summary>Cihaz anahtarı üretimi / doğrulaması.</summary>
public static class Secrets
{
    /// <summary>Uzun, rastgele cihaz anahtarı (256 bit).</summary>
    public static string NewDeviceKey() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));

    public static string Hash(string secret) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));

    /// <summary>Sabit süreli karşılaştırma: yanıt süresinden bilgi sızmaz.</summary>
    public static bool Matches(string? expectedHash, string? suppliedSecret) =>
        expectedHash is not null
        && suppliedSecret is not null
        && CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(expectedHash), Encoding.ASCII.GetBytes(Hash(suppliedSecret)));
}
