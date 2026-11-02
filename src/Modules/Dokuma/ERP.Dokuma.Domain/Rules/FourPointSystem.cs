using ERP.Dokuma.Domain.Enums;

namespace ERP.Dokuma.Domain.Rules;

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
}
