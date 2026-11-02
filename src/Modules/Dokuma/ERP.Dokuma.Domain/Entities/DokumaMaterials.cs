using ERP.Dokuma.Domain.Enums;
using ERP.Core.Domain.Common;

namespace ERP.Dokuma.Domain.Entities;

/// <summary>İplik, haşıl ve diğer hizmetlerin tedarikçisi. Diğer projelerin tedarikçilerinden ayrıdır.</summary>
public class DokumaSupplier : SoftDeletableEntity, IActivatable
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
public class DokumaYarn : SoftDeletableEntity, IActivatable
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

/// <summary>Dokuma tezgâhı.</summary>
public class DokumaLoom : SoftDeletableEntity, IActivatable
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public LoomType LoomType { get; set; } = LoomType.Rapier;
    public SheddingType SheddingType { get; set; } = SheddingType.Dobby;

    /// <summary>Tezgâhın alabileceği en geniş tarak eni (cm).</summary>
    public decimal? MaxReedWidthCm { get; set; }

    /// <summary>Anma hızı (devir/dk).</summary>
    public int? NominalSpeedRpm { get; set; }

    /// <summary>Marka ve model.</summary>
    public string? Brand { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Artikel (kumaş kalitesi) kartı: dokumanın teknik tanımı. Çözgü ve atkı iplikleri <see cref="Yarns"/> satırlarındadır;
/// toplam tel sayısı çözgü satırlarının toplamıdır.
/// </summary>
public class DokumaArticle : SoftDeletableEntity, IActivatable
{
    public string ArticleCode { get; set; } = null!;
    public string Name { get; set; } = null!;

    /// <summary>Örgü (bezayağı, dimi 2/1, saten ...).</summary>
    public string? WeaveName { get; set; }

    /// <summary>Gereken ağızlık sistemi; tezgâh seçiminde karşılaştırılır.</summary>
    public SheddingType SheddingType { get; set; } = SheddingType.Dobby;

    /// <summary>Örn. %100 pamuk.</summary>
    public string? Composition { get; set; }

    /// <summary>Tarak numarası (işletmenin kullandığı birimle).</summary>
    public decimal? ReedNumber { get; set; }

    /// <summary>Bir dişten geçen tel sayısı.</summary>
    public int? EndsPerDent { get; set; }

    /// <summary>Tarak eni (cm); atkı ipliği ihtiyacında kullanılır.</summary>
    public decimal ReedWidthCm { get; set; }

    /// <summary>Atkı sıklığı (atkı/cm).</summary>
    public decimal WeftDensityPerCm { get; set; }

    /// <summary>Ham en (cm).</summary>
    public decimal RawWidthCm { get; set; }

    /// <summary>Ham gramaj (g/m²).</summary>
    public int RawWeightGsm { get; set; }

    /// <summary>Çözgü çekmesi (%): bir metre kumaş için gereken fazla çözgü ipliği.</summary>
    public decimal? WarpCrimpPercent { get; set; }

    /// <summary>Atkı çekmesi (%): tarak eninin üzerine gereken fazla atkı ipliği.</summary>
    public decimal? WeftCrimpPercent { get; set; }

    /// <summary>Çözgü firesi (%): ihtiyaç hesabında net çözgü ipliğinin üzerine eklenir.</summary>
    public decimal? WarpWastePercent { get; set; }

    /// <summary>Atkı firesi (%): ihtiyaç hesabında net atkı ipliğinin üzerine eklenir.</summary>
    public decimal? WeftWastePercent { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<DokumaArticleYarn> Yarns { get; } = [];
}

/// <summary>
/// Artikelin çözgü ya da atkı ipliği. Çözgüde <see cref="Ends"/> bu iplikten kaç tel olduğunu, atkıda desen raporundaki
/// atkı sayısını (oranı) verir.
/// </summary>
public class DokumaArticleYarn : AuditableEntity
{
    public int ArticleId { get; set; }
    public DokumaArticle Article { get; set; } = null!;

    public ArticleYarnRole Role { get; set; }

    /// <summary>Aynı yer içindeki sıra (1'den başlar).</summary>
    public int Sequence { get; set; }

    public int YarnId { get; set; }
    public DokumaYarn Yarn { get; set; } = null!;

    public int Ends { get; set; }
}
