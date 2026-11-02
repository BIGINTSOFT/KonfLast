namespace ERP.Dokuma.Domain.Rules;

/// <summary>
/// Tezgâhın saatte dokuduğu kumaş: devir/dk × 60 atkı atar, bir metre kumaş atkı/cm × 100 atkıdır; verimle çarpılır.
/// Ön maliyette ve kapasite planında aynı formül kullanılır.
/// </summary>
public static class WeavingRate
{
    /// <summary>Verim tanımlı değilse kullanılan varsayılan (%).</summary>
    public const decimal DefaultEfficiencyPercent = 85m;

    /// <summary>Saatte dokunan kumaş (metre); hız, sıklık ya da verim sıfırsa boş.</summary>
    public static decimal? MetersPerHour(decimal? speedRpm, decimal picksPerCm, decimal efficiencyPercent)
    {
        if (speedRpm is not > 0 || picksPerCm <= 0 || efficiencyPercent <= 0)
        {
            return null;
        }

        return speedRpm.Value * 60m / (picksPerCm * 100m) * efficiencyPercent / 100m;
    }

    /// <summary>Bir metre kumaşın dokuma süresi (saat) = 1 ÷ saatte dokunan metre.</summary>
    public static decimal? HoursPerMeter(decimal? speedRpm, decimal picksPerCm, decimal efficiencyPercent) =>
        MetersPerHour(speedRpm, picksPerCm, efficiencyPercent) is { } perHour ? 1m / perHour : null;

    /// <summary>Leventten beklenen kumaş metresi: çözgü çekmesi kadar kısalır.</summary>
    public static decimal ExpectedFabricMeters(decimal beamMeters, decimal? warpCrimpPercent) =>
        Math.Round(beamMeters / (1 + (warpCrimpPercent ?? 0) / 100m), 2);
}
