using ERP.Boyahane.Domain.Enums;
using ERP.Core.Domain.Common;

namespace ERP.Boyahane.Domain.Entities;

/// <summary>
/// Renk kartı (renk arşivi). Sipariş satırında seçilirse renk adı ve kodu karttan gelir; onaylanan lab reçeteleri sipariş
/// satırı üzerinden karta bağlanır.
/// </summary>
public class BoyahaneColorCard : SoftDeletableEntity, IActivatable
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;

    /// <summary>Müşteriye özel renkse müşteri; boşsa genel renktir.</summary>
    public int? CustomerId { get; set; }
    public BoyahaneCustomer? Customer { get; set; }

    /// <summary>Müşterinin kendi renk kodu.</summary>
    public string? CustomerColorCode { get; set; }

    /// <summary>Pantone referansı (serbest metin, örn. 19-4052 TCX).</summary>
    public string? Pantone { get; set; }

    /// <summary>CIELab ölçümü; ölçülmediyse boş.</summary>
    public decimal? LabL { get; set; }
    public decimal? LabA { get; set; }
    public decimal? LabB { get; set; }

    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Renk kartının numune görüntüsü; listelerde taşınmasın diye ayrı tutulur.</summary>
[NotAudited]
public class BoyahaneColorCardImage
{
    public int ColorCardId { get; set; }
    public BoyahaneColorCard ColorCard { get; set; } = null!;

    public byte[] Content { get; set; } = null!;

    /// <summary>image/jpeg ya da image/png.</summary>
    public string ContentType { get; set; } = null!;

    public DateTime UpdatedAt { get; set; }
}

/// <summary>Desen kartı (desen arşivi). Renk yolları ve her renk yolunun baskı reçetesi desene bağlıdır.</summary>
public class BoyahanePattern : SoftDeletableEntity, IActivatable
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;

    /// <summary>Müşteriye özel desense müşteri; boşsa genel desendir.</summary>
    public int? CustomerId { get; set; }
    public BoyahaneCustomer? Customer { get; set; }

    /// <summary>Rapor (tekrar) ölçüsü, cm.</summary>
    public decimal? RepeatWidthCm { get; set; }
    public decimal? RepeatHeightCm { get; set; }

    public PrintType PrintType { get; set; } = PrintType.Pigment;

    /// <summary>Şablon ya da silindir sayısı.</summary>
    public int? ScreenCount { get; set; }

    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<BoyahanePatternColorway> Colorways { get; } = [];
}

/// <summary>Desenin görüntüsü; listelerde taşınmasın diye ayrı tutulur.</summary>
[NotAudited]
public class BoyahanePatternImage
{
    public int PatternId { get; set; }
    public BoyahanePattern Pattern { get; set; } = null!;

    public byte[] Content { get; set; } = null!;

    /// <summary>image/jpeg ya da image/png.</summary>
    public string ContentType { get; set; } = null!;

    public DateTime UpdatedAt { get; set; }
}

/// <summary>Desenin renk yolu (renk varyantı). Baskı siparişi satırı desen ve renk yolunu seçer.</summary>
public class BoyahanePatternColorway : AuditableEntity
{
    public int PatternId { get; set; }
    public BoyahanePattern Pattern { get; set; } = null!;

    /// <summary>Desen içinde benzersiz kod.</summary>
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;

    public bool IsActive { get; set; } = true;

    public ICollection<BoyahaneColorwayColor> Colors { get; } = [];
}

/// <summary>Renk yolundaki bir renk (şablon / silindir sırası) ve baskı reçetesi: pasta reçetesi ve metre başına pasta.</summary>
public class BoyahaneColorwayColor : AuditableEntity
{
    public int ColorwayId { get; set; }
    public BoyahanePatternColorway Colorway { get; set; } = null!;

    /// <summary>Şablon / silindir sırası (1, 2, 3 ...).</summary>
    public int Position { get; set; }

    /// <summary>Arşivdeki renk; boşsa yalnızca ad yazılır.</summary>
    public int? ColorCardId { get; set; }
    public BoyahaneColorCard? ColorCard { get; set; }

    /// <summary>Rengin adı; renk kartı seçildiyse karttan gelir.</summary>
    public string Name { get; set; } = null!;

    /// <summary>Metre kumaş başına tahmini pasta (kg); boşsa bu renk için tüketim hesaplanmaz.</summary>
    public decimal? PasteKilogramsPerMeter { get; set; }

    public ICollection<BoyahanePrintRecipeLine> Lines { get; } = [];
}

/// <summary>Baskı pastası reçetesi satırı: boya / pigment ya da kimyasal, kg pasta başına gram.</summary>
public class BoyahanePrintRecipeLine : AuditableEntity
{
    public int ColorwayColorId { get; set; }
    public BoyahaneColorwayColor ColorwayColor { get; set; } = null!;

    public int DyeChemicalId { get; set; }
    public BoyahaneDyeChemical DyeChemical { get; set; } = null!;

    /// <summary>g/kg pasta.</summary>
    public decimal GramsPerKilogram { get; set; }
}

/// <summary>Söküm reçetesi: renk kontrolünde "söküm + yeniden boya" seçilince eklenen söküm adımında uygulanır.</summary>
public class BoyahaneStrippingRecipe : SoftDeletableEntity, IActivatable
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<BoyahaneStrippingRecipeLine> Lines { get; } = [];
}

/// <summary>Söküm reçetesi satırı: kimyasal, miktar (g/L ya da %), sıcaklık ve süre.</summary>
public class BoyahaneStrippingRecipeLine : AuditableEntity
{
    public int RecipeId { get; set; }
    public BoyahaneStrippingRecipe Recipe { get; set; } = null!;

    /// <summary>Satır sırası; banyo aşamaları bu sırayla uygulanır.</summary>
    public int Sequence { get; set; }

    public int DyeChemicalId { get; set; }
    public BoyahaneDyeChemical DyeChemical { get; set; } = null!;

    public decimal Amount { get; set; }
    public RecipeAmountUnit Unit { get; set; } = RecipeAmountUnit.GramsPerLiter;

    /// <summary>Banyo sıcaklığı (°C).</summary>
    public int? TemperatureCelsius { get; set; }

    /// <summary>Süre (dakika).</summary>
    public int? DurationMinutes { get; set; }
}
