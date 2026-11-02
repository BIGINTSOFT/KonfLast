using ERP.Core.Domain.Common;

namespace ERP.Konfeksiyon.Domain.Entities;

/// <summary>
/// Parça başı ücret ayarları (tek satır). Operasyona birim ücret girilmişse o kullanılır; girilmemişse ücret
/// operasyonun standart süresi (SAM) ile dakika ücretinin çarpımıdır. Günlük verimi bir kademeye ulaşan
/// operatör o günün parça başı kazancı üzerinden prim alır.
/// </summary>
public class PayrollSetting : AuditableEntity
{
    /// <summary>Ayarlar tek satırdır; kimliği sabittir.</summary>
    public const int SingletonId = 1;

    /// <summary>Bir standart dakikanın ücreti. 0: SAM üzerinden ücret hesaplanmaz.</summary>
    public decimal MinuteRate { get; set; }

    /// <summary>Ücretlerin para birimi (ISO 4217, örn. TRY).</summary>
    public string Currency { get; set; } = "TRY";

    public ICollection<PayrollBonusTier> BonusTiers { get; } = new List<PayrollBonusTier>();

    /// <summary>Bir operasyon adımının parça başı birim ücreti; ücret tanımlı değilse null.</summary>
    public decimal? UnitRateFor(decimal? pieceRate, decimal sam) =>
        pieceRate ?? (MinuteRate > 0 && sam > 0 ? decimal.Round(sam * MinuteRate, 4) : null);

    /// <summary>Verimin ulaştığı en yüksek kademenin prim oranı (%); hiçbir kademeye ulaşmıyorsa 0.</summary>
    public decimal BonusPercentFor(double? efficiencyPercent)
    {
        if (efficiencyPercent is not { } efficiency)
        {
            return 0;
        }

        return BonusTiers
            .Where(t => (double)t.MinEfficiencyPercent <= efficiency)
            .OrderByDescending(t => t.MinEfficiencyPercent)
            .Select(t => t.BonusPercent)
            .FirstOrDefault();
    }
}

/// <summary>Prim kademesi: günlük verim bu eşiğe ulaşırsa parça başı kazanca bu oranda prim eklenir.</summary>
public class PayrollBonusTier : BaseEntity
{
    public int PayrollSettingId { get; set; }
    public PayrollSetting PayrollSetting { get; set; } = null!;

    /// <summary>Kademenin başladığı günlük verim (%), örn. 85.</summary>
    public decimal MinEfficiencyPercent { get; set; }

    /// <summary>Parça başı kazanca eklenecek prim oranı (%), örn. 10.</summary>
    public decimal BonusPercent { get; set; }
}
