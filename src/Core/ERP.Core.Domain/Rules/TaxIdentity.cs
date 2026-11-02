namespace ERP.Core.Domain.Rules;

/// <summary>Türk vergi kimliklerinin kontrol hanesi doğrulaması (GİB / NVİ algoritmaları).</summary>
public static class TaxIdentity
{
    /// <summary>Vergi Kimlik Numarası: 10 hane; son hane ilk dokuz haneden hesaplanır.</summary>
    public static bool IsValidVkn(string? value)
    {
        if (value is not { Length: 10 } || !value.All(char.IsAsciiDigit))
        {
            return false;
        }

        var sum = 0;
        for (var i = 0; i < 9; i++)
        {
            var digit = (value[i] - '0' + 9 - i) % 10;
            // 9 sonucu 9 olarak kalır; diğerleri 2'nin kuvvetiyle çarpılıp 9'a göre indirgenir.
            sum += digit == 9 ? 9 : digit * (1 << (9 - i)) % 9;
        }

        return (10 - sum % 10) % 10 == value[9] - '0';
    }

    /// <summary>T.C. Kimlik Numarası: 11 hane, sıfırla başlamaz; son iki hane kontrol hanesidir.</summary>
    public static bool IsValidTckn(string? value)
    {
        if (value is not { Length: 11 } || !value.All(char.IsAsciiDigit) || value[0] == '0')
        {
            return false;
        }

        var d = value.Select(c => c - '0').ToArray();
        var odd = d[0] + d[2] + d[4] + d[6] + d[8];
        var even = d[1] + d[3] + d[5] + d[7];
        var tenth = ((odd * 7 - even) % 10 + 10) % 10;
        var eleventh = d.Take(10).Sum() % 10;
        return d[9] == tenth && d[10] == eleventh;
    }

    /// <summary>Yalnızca rakamlardan oluşan numara Türk vergi kimliği sayılır; diğerleri yurt dışı vergi numarasıdır.</summary>
    public static bool IsDomestic(string? value) => value is { Length: > 0 } && value.All(char.IsAsciiDigit);
}
