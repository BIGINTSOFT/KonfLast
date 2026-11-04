namespace ERP.Core.Domain.Common;

/// <summary>
/// RFID kart UID'sinin sistemdeki tek biçimi: en fazla 10 karakterlik kart numarası (harf ve rakam).
/// Okuyucunun yazdığı sayısal numaralarda ("0012629368") baştaki sıfırlar elle girişte atlanabilir
/// ("12629368"); yalnız rakamdan oluşan numaralar 10 haneye tamamlanarak saklanır ve aranır.
/// Harfler büyük harfe çevrilir.
/// </summary>
public static class RfidUid
{
    public const int Digits = 10;

    /// <summary>UID sütununun genişliği (kart numarasından geniştir; sütun değiştirilmedi).</summary>
    public const int MaxLength = 47;

    /// <summary>Okuyucunun kart çekilince yazdığı satır; kart numarası değildir.</summary>
    public const string NoCard = "NOCARD";

    /// <summary>Geçerli bir kart numarası (1-10 harf ya da rakam) değilse null döner.</summary>
    public static string? Normalize(string? raw)
    {
        var code = raw?.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(code) || code == NoCard || code.Length > Digits || !code.All(char.IsAsciiLetterOrDigit))
        {
            return null;
        }

        return code.All(char.IsAsciiDigit) ? code.PadLeft(Digits, '0') : code;
    }
}
