using ERP.Dokuma.Domain.Enums;

namespace ERP.Dokuma.Domain.Rules;

/// <summary>
/// Artikelden bir metre kumaş için gereken iplik (fire hariç). İplik inceliği tex'e (g / 1000 m) çevrilir; katlı iplikte
/// tek kat numarası kat sayısıyla çarpılır.
/// </summary>
public static class YarnRequirement
{
    /// <summary>İpliğin tex karşılığı.</summary>
    public static decimal Tex(decimal countValue, YarnCountSystem system, int plies)
    {
        var single = system switch
        {
            YarnCountSystem.Ne => 590.54m / countValue,
            YarnCountSystem.Nm => 1000m / countValue,
            YarnCountSystem.Dtex => countValue / 10m,
            YarnCountSystem.Denier => countValue / 9m,
            _ => throw new ArgumentOutOfRangeException(nameof(system)),
        };
        return single * Math.Max(plies, 1);
    }

    /// <summary>Bir metre kumaş için çözgü ipliği (kg): tel sayısı × (1 + çekme) × tex.</summary>
    public static decimal WarpKilogramsPerMeter(int ends, decimal? crimpPercent, decimal tex) =>
        ends * (1 + (crimpPercent ?? 0) / 100m) * tex / 1_000_000m;

    /// <summary>
    /// Bir metre kumaş için atkı ipliği (kg): atkı/cm × 100 × tarak eni (m) × (1 + çekme) × tex, ipliğin rapordaki payıyla.
    /// </summary>
    public static decimal WeftKilogramsPerMeter(decimal picksPerCm, decimal reedWidthCm, decimal? crimpPercent, decimal tex, decimal share) =>
        picksPerCm * 100m * (reedWidthCm / 100m) * (1 + (crimpPercent ?? 0) / 100m) * tex / 1_000_000m * share;
}
