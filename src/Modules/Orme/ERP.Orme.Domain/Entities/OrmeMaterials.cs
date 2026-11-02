using ERP.Core.Domain.Common;
using ERP.Orme.Domain.Enums;

namespace ERP.Orme.Domain.Entities;

/// <summary>İplik ve diğer hizmetlerin tedarikçisi. Diğer projelerin tedarikçilerinden ayrıdır.</summary>
public class OrmeSupplier : SoftDeletableEntity, IActivatable
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? TaxNumber { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string Currency { get; set; } = "TRY";

    /// <summary>Ortalama teslim süresi (gün); tahmini tarih için.</summary>
    public int? LeadTimeDays { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>İplik kartı: bütün müşteriler için ortak tanım. İplik kilogram ile takip edilir; sahibi ve lotu girişte tutulur.</summary>
public class OrmeYarn : SoftDeletableEntity, IActivatable
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

    /// <summary>Boyalı iplikte renk; ham iplikte boş.</summary>
    public string? Color { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>Yuvarlak örme makinesi.</summary>
public class OrmeMachine : SoftDeletableEntity, IActivatable
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public KnitBedType BedType { get; set; } = KnitBedType.Single;

    /// <summary>Çap (pus).</summary>
    public int DiameterInch { get; set; }

    /// <summary>İnceliği (fine: bir pustaki iğne sayısı).</summary>
    public int Gauge { get; set; }

    public int? NeedleCount { get; set; }

    /// <summary>Sistem (besleyici) sayısı.</summary>
    public int? FeederCount { get; set; }

    /// <summary>Anma hızı (devir/dk).</summary>
    public int? NominalSpeedRpm { get; set; }

    /// <summary>Marka ve model.</summary>
    public string? Brand { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>Kumaş kartı: örgünün teknik tanımı. İplikler ve oranları <see cref="Yarns"/> satırlarındadır.</summary>
public class OrmeFabric : SoftDeletableEntity, IActivatable
{
    public string FabricCode { get; set; } = null!;
    public string Name { get; set; } = null!;

    /// <summary>Örgü (süprem, ribana, interlok, iki iplik ...).</summary>
    public string? KnitName { get; set; }

    /// <summary>Gereken plaka yapısı; makine seçiminde karşılaştırılır.</summary>
    public KnitBedType BedType { get; set; } = KnitBedType.Single;

    /// <summary>Önerilen çap (pus); boşsa kısıt yoktur.</summary>
    public int? DiameterInch { get; set; }

    /// <summary>Önerilen incelik (fine); boşsa kısıt yoktur.</summary>
    public int? Gauge { get; set; }

    /// <summary>Örn. %95 pamuk %5 likra.</summary>
    public string? Composition { get; set; }

    /// <summary>Ham en (cm).</summary>
    public decimal? RawWidthCm { get; set; }

    /// <summary>Ham gramaj (g/m²); metre karşılığı bununla hesaplanır.</summary>
    public int RawWeightGsm { get; set; }

    /// <summary>Fire (%): iplik ihtiyacında net kilonun üzerine eklenir.</summary>
    public decimal? WastePercent { get; set; }

    /// <summary>Beklenen üretim (kg/saat); verim raporunda karşılaştırılır.</summary>
    public decimal? ExpectedKilogramsPerHour { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<OrmeFabricYarn> Yarns { get; } = [];
}

/// <summary>Kumaşın ipliği: kumaş ağırlığındaki oranı (%) ve ilmek iplik uzunluğu.</summary>
public class OrmeFabricYarn : AuditableEntity
{
    public int FabricId { get; set; }
    public OrmeFabric Fabric { get; set; } = null!;

    /// <summary>Sıra (1'den başlar).</summary>
    public int Sequence { get; set; }

    public int YarnId { get; set; }
    public OrmeYarn Yarn { get; set; } = null!;

    /// <summary>Kumaş ağırlığındaki oran (%); satırların toplamı 100 olur.</summary>
    public decimal Percent { get; set; }

    /// <summary>İlmek iplik uzunluğu (mm); biliniyorsa.</summary>
    public decimal? StitchLengthMm { get; set; }
}
