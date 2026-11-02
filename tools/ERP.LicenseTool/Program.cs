using System.Security.Cryptography;
using System.Text;
using ERP.Core.Infrastructure.Licensing;

// Tekstil ERP lisans aracı (yalnızca yayıncıda kullanılır).
//
//   keygen --out <ozel-anahtar.pem>
//       Yeni imza anahtarı üretir. Özel anahtarı dosyaya yazar, uygulamaya gömülecek açık anahtarı ekrana basar.
//
//   issue --key <ozel-anahtar.pem> --key-id <kimlik> --customer "<Firma>" --modules Konfeksiyon,Boyahane
//         [--expires 2027-12-31] [--machine SUNUCU-ADI] --out <license.lic>
//       Müşteriye lisans üretir.
//
//   show <license.lic>
//       Lisansın içeriğini ve bu makinede geçerli olup olmadığını gösterir.

Console.OutputEncoding = Encoding.UTF8;

try
{
    return args.FirstOrDefault() switch
    {
        "keygen" => KeyGen(Options(args)),
        "issue" => Issue(Options(args)),
        "show" when args.Length > 1 => Show(args[1]),
        _ => Usage(),
    };
}
catch (ToolException exception)
{
    Console.Error.WriteLine($"Hata: {exception.Message}");
    return 1;
}

static int KeyGen(Dictionary<string, string> options)
{
    var output = Required(options, "out");
    if (File.Exists(output))
    {
        throw new ToolException($"{output} zaten var; mevcut anahtarın üzerine yazılmaz.");
    }

    using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
    File.WriteAllText(output, key.ExportPkcs8PrivateKeyPem());

    Console.WriteLine($"Özel anahtar yazıldı: {Path.GetFullPath(output)}");
    Console.WriteLine("Bu dosyayı yedekleyin ve kimseyle paylaşmayın; kaybolursa yeni lisans üretilemez.");
    Console.WriteLine();
    Console.WriteLine("Uygulamaya gömülecek açık anahtar (LicensePublicKeys.Current):");
    Console.WriteLine(Convert.ToBase64String(key.ExportSubjectPublicKeyInfo()));
    return 0;
}

static int Issue(Dictionary<string, string> options)
{
    using var key = ECDsa.Create();
    key.ImportFromPem(File.ReadAllText(Required(options, "key")));

    var modules = Required(options, "modules")
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    if (modules.Length == 0)
    {
        throw new ToolException("En az bir modül verilmeli (--modules Konfeksiyon,Boyahane).");
    }

    DateOnly? expires = null;
    if (options.TryGetValue("expires", out var expiresText))
    {
        expires = DateOnly.TryParseExact(expiresText, "yyyy-MM-dd", out var parsed)
            ? parsed
            : throw new ToolException("--expires yyyy-MM-dd biçiminde olmalı.");
    }

    var license = new ErpLicense(
        LicenseId: Guid.NewGuid().ToString("N")[..12].ToUpperInvariant(),
        Customer: Required(options, "customer"),
        Modules: modules,
        IssuedAt: DateOnly.FromDateTime(DateTime.UtcNow),
        ExpiresAt: expires,
        Machine: options.GetValueOrDefault("machine"));

    var output = Required(options, "out");
    File.WriteAllText(output, LicenseValidator.Sign(license, Required(options, "key-id"), key));
    Console.WriteLine($"Lisans yazıldı: {Path.GetFullPath(output)}");
    Print(license);
    return 0;
}

static int Show(string path)
{
    var check = LicenseValidator.Validate(File.ReadAllText(path), DateOnly.FromDateTime(DateTime.UtcNow), Environment.MachineName);
    if (check.License is not null)
    {
        Print(check.License);
    }

    Console.WriteLine(check.IsValid ? "Durum: geçerli (bu makinede)" : $"Durum: GEÇERSİZ — {check.Error}");
    return check.IsValid ? 0 : 2;
}

static void Print(ErpLicense license)
{
    Console.WriteLine($"  Lisans no : {license.LicenseId}");
    Console.WriteLine($"  Firma     : {license.Customer}");
    Console.WriteLine($"  Modüller  : {string.Join(", ", license.Modules)}");
    Console.WriteLine($"  Veriliş   : {license.IssuedAt:dd.MM.yyyy}");
    Console.WriteLine($"  Bitiş     : {(license.ExpiresAt is { } e ? e.ToString("dd.MM.yyyy") : "süresiz")}");
    Console.WriteLine($"  Sunucu    : {license.Machine ?? "bağlı değil"}");
}

static Dictionary<string, string> Options(string[] args)
{
    var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    for (var i = 1; i < args.Length - 1; i += 2)
    {
        if (!args[i].StartsWith("--", StringComparison.Ordinal))
        {
            throw new ToolException($"Beklenmeyen argüman: {args[i]}");
        }

        options[args[i][2..]] = args[i + 1];
    }

    return options;
}

static string Required(Dictionary<string, string> options, string name) =>
    options.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value)
        ? value
        : throw new ToolException($"--{name} verilmeli.");

static int Usage()
{
    Console.WriteLine("""
        Tekstil ERP lisans aracı

          keygen --out <ozel-anahtar.pem>
          issue  --key <ozel-anahtar.pem> --key-id <kimlik> --customer "<Firma>" --modules Konfeksiyon,Boyahane
                 [--expires yyyy-MM-dd] [--machine SUNUCU-ADI] --out <license.lic>
          show   <license.lic>
        """);
    return 1;
}

internal sealed class ToolException(string message) : Exception(message);
