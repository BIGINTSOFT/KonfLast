namespace ERP.Core.Application.Common;

/// <summary>
/// Eşzamanlılık kontrolü için rowversion'ın istemciyle paylaşılan hali. İstemci kaydı okurken aldığı
/// değeri değişiklik isteğinde geri gönderir; arada kayıt değiştiyse istek reddedilir.
/// </summary>
public static class RowVersionToken
{
    private const string Field = "rowVersion";

    public static string From(byte[] rowVersion) => Convert.ToBase64String(rowVersion);

    /// <summary>İstemcinin gördüğü sürüm güncelse null; değilse <paramref name="conflict"/> hatasını döner.</summary>
    public static Error? Check(byte[] current, string? supplied, Error conflict)
    {
        if (string.IsNullOrWhiteSpace(supplied))
        {
            return ReferenceValidation.InvalidReference(Field, "Kayıt sürümü (rowVersion) zorunludur.");
        }

        var bytes = new byte[current.Length];
        if (!Convert.TryFromBase64String(supplied, bytes, out var written) || written != current.Length)
        {
            return ReferenceValidation.InvalidReference(Field, "Kayıt sürümü (rowVersion) geçersiz.");
        }

        return bytes.AsSpan().SequenceEqual(current) ? null : conflict;
    }
}
