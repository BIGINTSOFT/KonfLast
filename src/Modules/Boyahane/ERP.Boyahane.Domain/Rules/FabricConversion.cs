namespace ERP.Boyahane.Domain.Rules;

/// <summary>Kumaşın metre / kilo karşılığı: kilo = metre × en (m) × gramaj (g/m²) ÷ 1000.</summary>
public static class FabricConversion
{
    public static decimal KilogramsFromMeters(decimal meters, decimal widthCm, int weightGsm) =>
        Math.Round(meters * widthCm * weightGsm / 100_000m, 2);

    public static decimal MetersFromKilograms(decimal kilograms, decimal widthCm, int weightGsm) =>
        Math.Round(kilograms * 100_000m / (widthCm * weightGsm), 2);
}
