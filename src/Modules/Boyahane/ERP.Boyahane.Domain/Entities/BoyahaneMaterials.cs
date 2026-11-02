using ERP.Boyahane.Domain.Enums;
using ERP.Core.Domain.Common;
using ERP.Core.Domain.Enums;

namespace ERP.Boyahane.Domain.Entities;

/// <summary>
/// Kumaş kartı: kalitenin değişmez tanımı, bütün müşteriler için ortaktır. Malın sahibi, gerçek miktarı ve ölçümü
/// karta değil girişe (lota) aittir; renk de sipariş ve reçetede tutulur.
/// </summary>
public class BoyahaneFabric : SoftDeletableEntity, IActivatable
{
    public string FabricCode { get; set; } = null!;
    public string Name { get; set; } = null!;
    public FabricType FabricType { get; set; } = FabricType.Knit;

    /// <summary>Kumaş yapısı (süprem, ribana, gabardin ...).</summary>
    public string? Structure { get; set; }

    /// <summary>Örn. %95 pamuk %5 elastan.</summary>
    public string? Composition { get; set; }

    /// <summary>Ham en (cm); metre / kilo karşılığı önerisinde kullanılır.</summary>
    public decimal RawWidthCm { get; set; }

    /// <summary>Ham gramaj (g/m²); metre / kilo karşılığı önerisinde kullanılır.</summary>
    public int RawWeightGsm { get; set; }

    /// <summary>Hedeflenen mamul en (cm).</summary>
    public decimal? FinishedWidthCm { get; set; }

    /// <summary>Hedeflenen mamul gramaj (g/m²).</summary>
    public int? FinishedWeightGsm { get; set; }

    /// <summary>Kabul edilen çekme oranı (%).</summary>
    public decimal? ShrinkageTolerancePercent { get; set; }

    /// <summary>Takipte varsayılan birim: metre ya da kilogram.</summary>
    public StockUnit DefaultUnit { get; set; } = StockUnit.Meter;

    public bool IsActive { get; set; } = true;
}

/// <summary>İplik kartı: bütün müşteriler için ortak tanım. İplik kilogram ile takip edilir.</summary>
public class BoyahaneYarn : SoftDeletableEntity, IActivatable
{
    public string YarnCode { get; set; } = null!;
    public string Name { get; set; } = null!;

    /// <summary>İplik numarası (örn. 30); hangi sisteme göre olduğu <see cref="CountSystem"/> ile belirtilir.</summary>
    public decimal CountValue { get; set; }
    public YarnCountSystem CountSystem { get; set; } = YarnCountSystem.Ne;

    /// <summary>Kat sayısı (örn. 30/2 ipliğinde 2).</summary>
    public int Plies { get; set; } = 1;

    public string? Composition { get; set; }

    /// <summary>Büküm bilgisi (örn. Z 650 tur/m).</summary>
    public string? Twist { get; set; }

    public bool IsActive { get; set; } = true;
}
