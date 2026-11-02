namespace ERP.Core.Domain.Common;

/// <summary>
/// RFID kart UID'sinin sistemdeki tek biçimi: kart okuyucunun yazdığı 10 haneli kart numarası
/// ("0012629368"). Baştaki sıfırlar elle girişte atlanabilir ("12629368"); hepsi 10 haneye tamamlanarak
/// saklanır ve aranır.
/// </summary>
public static class RfidUid
{
    public const int Digits = 10;

    /// <summary>UID sütununun genişliği (kart numarasından geniştir; sütun değiştirilmedi).</summary>
    public const int MaxLength = 47;

    /// <summary>Geçerli bir kart numarası (1-10 rakam) değilse null döner.</summary>
    public static string? Normalize(string? raw)
    {
        var digits = raw?.Trim();
        if (string.IsNullOrEmpty(digits) || digits.Length > Digits || !digits.All(char.IsAsciiDigit))
        {
            return null;
        }

        return digits.PadLeft(Digits, '0');
    }
}
