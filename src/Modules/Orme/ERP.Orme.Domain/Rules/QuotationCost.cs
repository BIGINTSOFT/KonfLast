namespace ERP.Orme.Domain.Rules;

/// <summary>Kilo başına maliyet kırılımı (teklifin para biriminde).</summary>
/// <param name="YarnCost">Fireli iplik maliyeti; fasonda sıfır.</param>
/// <param name="KnittingCost">(Makine + işçilik) saat maliyeti / saatte örülen kilo.</param>
/// <param name="Overhead">Doğrudan maliyet (iplik + örme) × genel gider oranı.</param>
/// <param name="TotalCost">İplik + örme + genel gider.</param>
/// <param name="CalculatedPrice">Toplam maliyet × (1 + kâr oranı).</param>
public sealed record QuotationCostBreakdown(decimal YarnCost, decimal KnittingCost, decimal Overhead, decimal TotalCost, decimal CalculatedPrice);

/// <summary>
/// Örme ön maliyeti. İplik: Σ (oran × kilo fiyatı) × (1 + fire). Örme: (makine + işçilik saat maliyeti) / saatte örülen kilo.
/// Genel gider doğrudan maliyetin üzerine, kâr toplam maliyetin üzerine eklenir. Web ekranındaki anlık hesap aynı formülü kullanır.
/// </summary>
public static class QuotationCost
{
    public const decimal MinMarginPercent = -100m;
    public const decimal MaxMarginPercent = 9_999_999m;

    public static decimal YarnCost(IEnumerable<(decimal Percent, decimal? UnitPrice)> yarns, decimal wastePercent) =>
        yarns.Sum(y => y.Percent / 100m * (y.UnitPrice ?? 0)) * (1 + wastePercent / 100m);

    /// <summary>Saatte örülen kilo bilinmiyorsa sıfır.</summary>
    public static decimal KnittingCost(decimal? kilogramsPerHour, decimal machineHourlyCost, decimal laborHourlyCost) =>
        kilogramsPerHour is > 0 ? (machineHourlyCost + laborHourlyCost) / kilogramsPerHour.Value : 0;

    public static QuotationCostBreakdown Of(
        bool isCommission,
        IEnumerable<(decimal Percent, decimal? UnitPrice)> yarns,
        decimal wastePercent,
        decimal? kilogramsPerHour,
        decimal machineHourlyCost,
        decimal laborHourlyCost,
        decimal overheadPercent,
        decimal marginPercent)
    {
        var yarn = isCommission ? 0 : YarnCost(yarns, wastePercent);
        var knitting = KnittingCost(kilogramsPerHour, machineHourlyCost, laborHourlyCost);
        var overhead = (yarn + knitting) * overheadPercent / 100m;
        var total = yarn + knitting + overhead;
        return new QuotationCostBreakdown(Round4(yarn), Round4(knitting), Round4(overhead), Round4(total), PriceFor(total, marginPercent));
    }

    /// <summary>Kâr oranından kilo fiyatı.</summary>
    public static decimal PriceFor(decimal totalCost, decimal marginPercent) => Math.Max(0, Round4(totalCost * (1 + marginPercent / 100m)));

    /// <summary>Elle girilen fiyattan kâr oranı; maliyet sıfırsa hesaplanamaz (boş).</summary>
    public static decimal? MarginFor(decimal totalCost, decimal unitPrice) =>
        totalCost > 0
            ? Math.Clamp(Math.Round((unitPrice / totalCost - 1) * 100m, 2, MidpointRounding.AwayFromZero), MinMarginPercent, MaxMarginPercent)
            : null;

    private static decimal Round4(decimal value) => Math.Round(value, 4, MidpointRounding.AwayFromZero);
}
