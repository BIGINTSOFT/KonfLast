using ERP.Core.Domain.Common;
using ERP.Konfeksiyon.Domain.Enums;

namespace ERP.Konfeksiyon.Domain.Entities;

/// <summary>
/// Kesim planı (pastal): bir üretim siparişinin tek renginden, pastal boyu ve beden oranıyla kaç kat serilip
/// kesileceği. Pastalın her katından her bedenden oran kadar parça çıkar; kumaş ihtiyacı
/// (pastal boyu + kat başı pay) × kat sayısıdır. Bir pastalda tek ton kumaş kullanılır.
/// </summary>
public class CuttingPlan : AuditableEntity
{
    /// <summary>Sistem tarafından üretilir, örn. KP-2026-00001; değiştirilemez ve yeniden kullanılmaz.</summary>
    public string PlanNo { get; set; } = null!;

    public int ProductionOrderId { get; set; }
    public ProductionOrder ProductionOrder { get; set; } = null!;

    public int ColorId { get; set; }
    public Color Color { get; set; } = null!;

    /// <summary>Pastal (marker) boyu, metre.</summary>
    public decimal MarkerLength { get; set; }

    /// <summary>Kat başına uç payı / fire, metre (varsayılan 0,03).</summary>
    public decimal EndAllowance { get; set; } = DefaultEndAllowance;

    /// <summary>Planlanan kat (pastal katı) sayısı.</summary>
    public int PlannedPlies { get; set; }

    /// <summary>Kesimde gerçekten serilen kat sayısı; kesim kaydedilince dolar.</summary>
    public int? ActualPlies { get; private set; }

    public CuttingPlanStatus Status { get; private set; } = CuttingPlanStatus.Draft;

    public string? Notes { get; set; }

    /// <summary>Kesimin kaydedildiği an (sunucu saati) ve kaydeden kullanıcı.</summary>
    public DateTime? CutAt { get; private set; }
    public string? CutBy { get; private set; }

    /// <summary>Aynı planın iki kullanıcı tarafından eşzamanlı değiştirilmesini engeller.</summary>
    public byte[] RowVersion { get; set; } = null!;

    public ICollection<CuttingPlanSize> Sizes { get; } = new List<CuttingPlanSize>();
    public ICollection<CuttingPlanLot> Lots { get; } = new List<CuttingPlanLot>();

    public const decimal DefaultEndAllowance = 0.03m;

    /// <summary>Pastal, oran ve toplar yalnızca taslakken değiştirilebilir.</summary>
    public bool IsEditable => Status == CuttingPlanStatus.Draft;

    /// <summary>Bir katta çıkan parça (oranların toplamı).</summary>
    public int PiecesPerPly => Sizes.Sum(s => s.Ratio);

    /// <summary>Planlanan kumaş ihtiyacı, metre.</summary>
    public decimal PlannedFabric => FabricFor(MarkerLength, EndAllowance, PlannedPlies);

    /// <summary>(Pastal boyu + kat başı pay) × kat; metre, 3 ondalık.</summary>
    public static decimal FabricFor(decimal markerLength, decimal endAllowance, int plies) =>
        decimal.Round((markerLength + endAllowance) * plies, 3);

    /// <summary>
    /// Kalan adetleri karşılayan en küçük kat sayısı: her beden için kalan ÷ oran yukarı yuvarlanır, en büyüğü alınır.
    /// Kalanı olmayan (ya da oranı sıfır olan) beden hesaba girmez; hiçbir bedende kalan yoksa sıfırdır.
    /// </summary>
    public static int SuggestPlies(IEnumerable<(int Ratio, int Remaining)> sizes) =>
        sizes
            .Where(s => s.Ratio > 0 && s.Remaining > 0)
            .Select(s => (s.Remaining + s.Ratio - 1) / s.Ratio)
            .DefaultIfEmpty(0)
            .Max();

    /// <summary>Kesimi kaydeder: gerçek kat sayısı yazılır, plan kesildi durumuna geçer.</summary>
    public void MarkCut(int actualPlies, DateTime cutAt, string? cutBy)
    {
        if (!IsEditable)
        {
            throw new InvalidOperationException($"{Status} durumundaki kesim planı kesilemez.");
        }

        ActualPlies = actualPlies;
        CutAt = cutAt;
        CutBy = cutBy;
        Status = CuttingPlanStatus.Cut;
    }

    public void Cancel()
    {
        if (!IsEditable)
        {
            throw new InvalidOperationException($"{Status} durumundaki kesim planı iptal edilemez.");
        }

        Status = CuttingPlanStatus.Cancelled;
    }
}

/// <summary>Pastaldaki bir beden: bir katta bu bedenden kaç parça çıktığı (oran) ve kesimde çıkan adet.</summary>
public class CuttingPlanSize : BaseEntity
{
    public int CuttingPlanId { get; set; }
    public CuttingPlan CuttingPlan { get; set; } = null!;

    public int SizeId { get; set; }
    public Size Size { get; set; } = null!;

    /// <summary>Bir kattaki parça adedi (pastal oranı).</summary>
    public int Ratio { get; set; }

    /// <summary>Kesimde gerçekten çıkan adet; kesim kaydedilince dolar (varsayılan oran × gerçek kat).</summary>
    public int? CutQuantity { get; set; }
}

/// <summary>Pastalda kullanılacak kumaş topu; kesimde toptan kullanılan miktar (kartın biriminde) yazılır.</summary>
public class CuttingPlanLot : BaseEntity
{
    public int CuttingPlanId { get; set; }
    public CuttingPlan CuttingPlan { get; set; } = null!;

    public int StockLotId { get; set; }
    public StockLot StockLot { get; set; } = null!;

    /// <summary>Serim sırası (kesim föyünde ve kumaş düşümünde toplar bu sırayla kullanılır).</summary>
    public int SortOrder { get; set; }

    /// <summary>Kesimde bu toptan düşülen miktar; kesim kaydedilince dolar.</summary>
    public decimal? UsedQuantity { get; set; }
}
