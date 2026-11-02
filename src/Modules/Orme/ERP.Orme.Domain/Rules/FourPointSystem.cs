using ERP.Orme.Domain.Enums;

namespace ERP.Orme.Domain.Rules;

/// <summary>
/// 4 puan sistemi: hata uzunluğuna göre 1-4 puan (7,5 / 15 / 23 cm sınırları; delik gibi türler her zaman 4), bir metrede
/// en fazla 4 puan sayılır. Sonuç 100 m² başına puandır.
/// </summary>
public static class FourPointSystem
{
    public const int MaxPointsPerMeter = 4;

    public static int PointsFor(decimal lengthCm, bool alwaysFourPoints) =>
        alwaysFourPoints ? 4
        : lengthCm <= 7.5m ? 1
        : lengthCm <= 15m ? 2
        : lengthCm <= 23m ? 3
        : 4;

    /// <summary>Hataların toplam puanı; aynı metredeki (0-1, 1-2 ...) hataların toplamı 4'ü geçmez.</summary>
    public static int TotalPoints(IEnumerable<(decimal AtMeter, int Points)> defects) =>
        defects
            .GroupBy(d => (int)Math.Floor(d.AtMeter))
            .Sum(g => Math.Min(MaxPointsPerMeter, g.Sum(d => d.Points)));

    /// <summary>100 m² başına puan: toplam puan × 100 / (metre × en (m)).</summary>
    public static decimal PointsPer100SquareMeters(int totalPoints, decimal meters, decimal widthCm) =>
        Math.Round(totalPoints * 100m / (meters * widthCm / 100m), 2, MidpointRounding.AwayFromZero);

    public static FabricRollStatus Grade(decimal pointsPer100M2, decimal firstQualityMax, decimal secondQualityMax) =>
        pointsPer100M2 <= firstQualityMax ? FabricRollStatus.FirstQuality
        : pointsPer100M2 <= secondQualityMax ? FabricRollStatus.SecondQuality
        : FabricRollStatus.Rejected;

    /// <summary>Metre ölçülmediyse tartılan kilo, ölçülen gramaj ve enden tahmin: kilo × 1000 / (gramaj × en (m)).</summary>
    public static decimal EstimatedMeters(decimal kilograms, decimal weightGsm, decimal widthCm) =>
        Math.Max(0.01m, Math.Round(kilograms * 1000m / (weightGsm * widthCm / 100m), 2, MidpointRounding.AwayFromZero));

    /// <summary>Ölçülen en ve gramajın kumaş kartından toleransı aşan sapması; yoksa boş. Kaliteyi değiştirmez, uyarıdır.</summary>
    public static string? SpecDeviation(decimal widthCm, decimal? cardWidthCm, decimal weightGsm, int cardWeightGsm, decimal widthTolerancePercent, decimal weightTolerancePercent)
    {
        var differences = new List<string>();
        if (cardWidthCm is > 0 && DeviationPercent(widthCm, cardWidthCm.Value) > widthTolerancePercent)
        {
            differences.Add($"en {widthCm:0.##} cm, kartta {cardWidthCm:0.##} cm");
        }

        if (cardWeightGsm > 0 && DeviationPercent(weightGsm, cardWeightGsm) > weightTolerancePercent)
        {
            differences.Add($"gramaj {weightGsm:0.##} g/m², kartta {cardWeightGsm} g/m²");
        }

        return differences.Count == 0 ? null : string.Join("; ", differences);
    }

    private static decimal DeviationPercent(decimal measured, decimal card) => Math.Abs(measured - card) / card * 100m;
}
