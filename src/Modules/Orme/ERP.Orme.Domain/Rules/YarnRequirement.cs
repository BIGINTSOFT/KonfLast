namespace ERP.Orme.Domain.Rules;

/// <summary>Örme iplik ihtiyacı: kumaş kilosunun ipliğe düşen payı. Metre tahmini gramaj ve enden çıkar.</summary>
public static class YarnRequirement
{
    /// <summary>Net iplik (kg): kumaş kilosu × ipliğin oranı.</summary>
    public static decimal NetKilograms(decimal fabricKilograms, decimal percent) => fabricKilograms * percent / 100m;

    /// <summary>Fireli iplik (kg).</summary>
    public static decimal WithWaste(decimal netKilograms, decimal? wastePercent) => netKilograms * (1 + (wastePercent ?? 0) / 100m);

    /// <summary>Tahmini metre: kilo × 1000 / (gramaj × en); en bilinmiyorsa boş.</summary>
    public static decimal? EstimatedMeters(decimal kilograms, int weightGsm, decimal? widthCm) =>
        widthCm is > 0 && weightGsm > 0 ? Math.Round(kilograms * 1000m / (weightGsm * widthCm.Value / 100m), 2, MidpointRounding.AwayFromZero) : null;
}
