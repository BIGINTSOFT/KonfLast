namespace ERP.Core.Infrastructure.Licensing;

/// <summary>
/// Yayıncının lisans imza anahtarının AÇIK kısmı. Özel anahtar yalnızca yayıncıda durur
/// (tools\ERP.LicenseTool keygen ile üretilir; depoya ve müşteriye konmaz). Anahtar değişirse
/// eski anahtarla verilmiş lisansların geçerli kalması için eskisi de listede tutulur.
/// </summary>
public static class LicensePublicKeys
{
    public const string CurrentKeyId = "tekstilerp-2026";

    public const string Current =
        "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEYo9zOCKHhZoHwhuZ8Oh4++BMiH5dBKs3+0KVtgwb57YgvCSAKZmtCty90sIW7+4iPvxQ+h+SzODUx3SYLJMTSQ==";
}
