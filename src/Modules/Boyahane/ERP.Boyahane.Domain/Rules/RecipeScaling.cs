using ERP.Boyahane.Domain.Enums;

namespace ERP.Boyahane.Domain.Rules;

/// <summary>Reçetenin parti ölçeği: boya kumaş ağırlığına göre %, kimyasal flotte suyuna göre g/L.</summary>
public static class RecipeScaling
{
    /// <summary>Flotte suyu (litre) = parti kilosu × flotte oranı.</summary>
    public static decimal LiquorLiters(decimal batchKilograms, decimal liquorRatio) =>
        Math.Round(batchKilograms * liquorRatio, 2);

    /// <summary>Gereken miktar (kg). Boya: kilo × % ÷ 100. Kimyasal: litre × g/L ÷ 1000.</summary>
    public static decimal RequiredKilograms(DyeChemicalKind kind, decimal amount, decimal batchKilograms, decimal liquorRatio) =>
        kind == DyeChemicalKind.Dye
            ? Math.Round(batchKilograms * amount / 100m, 3)
            : Math.Round(batchKilograms * liquorRatio * amount / 1000m, 3);

    /// <summary>Söküm reçetesi satırı (kg). %: kilo × % ÷ 100. g/L: litre × g/L ÷ 1000.</summary>
    public static decimal RequiredKilograms(RecipeAmountUnit unit, decimal amount, decimal batchKilograms, decimal liquorRatio) =>
        unit == RecipeAmountUnit.Percent
            ? Math.Round(batchKilograms * amount / 100m, 3)
            : Math.Round(batchKilograms * liquorRatio * amount / 1000m, 3);

    /// <summary>Baskıda bir rengin pastası (kg) = basılan metre × metre başına pasta.</summary>
    public static decimal PasteKilograms(decimal meters, decimal pasteKilogramsPerMeter) =>
        Math.Round(meters * pasteKilogramsPerMeter, 3);

    /// <summary>Pasta reçetesi satırı (kg) = pasta kilosu × g/kg ÷ 1000.</summary>
    public static decimal PasteChemicalKilograms(decimal pasteKilograms, decimal gramsPerKilogram) =>
        Math.Round(pasteKilograms * gramsPerKilogram / 1000m, 3);
}
