using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

namespace ERP.Core.Infrastructure.Licensing;

/// <summary>Lisansın içeriği: kime verildiği, hangi modülleri açtığı ve (varsa) süresi ile bağlı olduğu sunucu.</summary>
public sealed record ErpLicense(
    string LicenseId,
    string Customer,
    IReadOnlyList<string> Modules,
    DateOnly IssuedAt,
    DateOnly? ExpiresAt = null,
    string? Machine = null);

/// <summary>Diskteki lisans dosyası: imzalanan içerik (base64) ve imzası.</summary>
public sealed record LicenseFile(string KeyId, string Payload, string Signature);

public sealed record LicenseCheck(ErpLicense? License, string? Error, string? Path)
{
    public bool IsValid => License is not null && Error is null;

    public bool Allows(string module) =>
        IsValid && License!.Modules.Contains(module, StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Lisansı okur ve doğrular. Lisans, yalnızca yayıncıda duran özel anahtarla imzalanır; uygulamada açık anahtar
/// vardır, bu yüzden dosyadaki modül listesi ya da süre değiştirilirse imza tutmaz. Doğrulanmayan lisansta
/// hiçbir modül devreye alınmaz (çekirdek çalışmaya devam eder).
/// </summary>
public static class LicenseValidator
{
    public const string FileName = "license.lic";
    public const string PathSetting = "License:Path";

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>Yayıncının lisans imza anahtarları (anahtar kimliği → SubjectPublicKeyInfo, base64).</summary>
    private static readonly Dictionary<string, string> TrustedKeys = new(StringComparer.Ordinal)
    {
        [LicensePublicKeys.CurrentKeyId] = LicensePublicKeys.Current,
    };

    /// <summary>
    /// Lisans dosyasının yeri: <c>License:Path</c> ayarı, yoksa uygulama klasöründeki <c>license.lic</c>,
    /// yoksa (geliştirme makinesi) <c>%APPDATA%\TekstilERP\license.lic</c>.
    /// </summary>
    public static string? Locate(IConfiguration configuration, string? baseDirectory = null)
    {
        var candidates = new[]
        {
            configuration[PathSetting],
            Path.Combine(baseDirectory ?? AppContext.BaseDirectory, FileName),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TekstilERP", FileName),
        };
        return candidates.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p) && File.Exists(p));
    }

    public static LicenseCheck Check(IConfiguration configuration, string? baseDirectory = null)
    {
        var path = Locate(configuration, baseDirectory);
        if (path is null)
        {
            return new LicenseCheck(null, "Lisans dosyası bulunamadı (license.lic).", null);
        }

        try
        {
            return Validate(File.ReadAllText(path), DateOnly.FromDateTime(DateTime.UtcNow), Environment.MachineName) with { Path = path };
        }
        catch (IOException exception)
        {
            return new LicenseCheck(null, $"Lisans dosyası okunamadı: {exception.Message}", path);
        }
    }

    public static LicenseCheck Validate(string fileContent, DateOnly today, string machineName)
    {
        LicenseFile? file;
        try
        {
            file = JsonSerializer.Deserialize<LicenseFile>(fileContent, Json);
        }
        catch (JsonException)
        {
            return Invalid("Lisans dosyası bozuk.");
        }

        if (file is null || string.IsNullOrEmpty(file.Payload) || string.IsNullOrEmpty(file.Signature))
        {
            return Invalid("Lisans dosyası bozuk.");
        }

        if (!TrustedKeys.TryGetValue(file.KeyId ?? string.Empty, out var publicKey))
        {
            return Invalid("Lisans tanınmayan bir anahtarla imzalanmış.");
        }

        byte[] payload, signature;
        try
        {
            payload = Convert.FromBase64String(file.Payload);
            signature = Convert.FromBase64String(file.Signature);
        }
        catch (FormatException)
        {
            return Invalid("Lisans dosyası bozuk.");
        }

        using (var key = ECDsa.Create())
        {
            key.ImportSubjectPublicKeyInfo(Convert.FromBase64String(publicKey), out _);
            if (!key.VerifyData(payload, signature, HashAlgorithmName.SHA256))
            {
                return Invalid("Lisans imzası geçersiz: dosya değiştirilmiş.");
            }
        }

        var license = JsonSerializer.Deserialize<ErpLicense>(Encoding.UTF8.GetString(payload), Json);
        if (license is null)
        {
            return Invalid("Lisans dosyası bozuk.");
        }

        if (license.ExpiresAt is { } expires && today > expires)
        {
            return new LicenseCheck(license, $"Lisansın süresi {expires:dd.MM.yyyy} tarihinde doldu.", null);
        }

        if (!string.IsNullOrWhiteSpace(license.Machine)
            && !string.Equals(license.Machine, machineName, StringComparison.OrdinalIgnoreCase))
        {
            return new LicenseCheck(license, $"Lisans başka bir sunucu için verilmiş ({license.Machine}).", null);
        }

        return new LicenseCheck(license, null, null);

        static LicenseCheck Invalid(string error) => new(null, error, null);
    }

    /// <summary>İçeriği imzalar (yalnızca yayıncının lisans aracı kullanır; özel anahtar müşteriye gitmez).</summary>
    public static string Sign(ErpLicense license, string keyId, ECDsa privateKey)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(license, Json);
        var signature = privateKey.SignData(payload, HashAlgorithmName.SHA256);
        return JsonSerializer.Serialize(
            new LicenseFile(keyId, Convert.ToBase64String(payload), Convert.ToBase64String(signature)),
            new JsonSerializerOptions(Json) { WriteIndented = true });
    }
}
