using ERP.Konfeksiyon.Domain.Enums;
using ERP.Core.Domain.Common;
using ERP.Core.Domain.Enums;

namespace ERP.Konfeksiyon.Domain.Entities;

/// <summary>Bir modelden üretilen, siparişe ve demete konu olan ürün.</summary>
public class Product : SoftDeletableEntity, IActivatable
{
    public string ProductCode { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int ModelId { get; set; }
    public Model Model { get; set; } = null!;

    // Sınıflandırma
    public int? ProductGroupId { get; set; }
    public ProductGroup? ProductGroup { get; set; }
    public TargetGroup? TargetGroup { get; set; }

    /// <summary>Sezon kodu (örn. SS27, AW26).</summary>
    public string? Season { get; set; }

    /// <summary>Müşterinin bu ürün için kullandığı kendi ürün / artikel kodu.</summary>
    public string? CustomerArticleCode { get; set; }

    // Kumaş ve etiket
    /// <summary>Kumaş içeriği (örn. %95 Pamuk %5 Elastan); bakım etiketinde yer alır.</summary>
    public string? Composition { get; set; }

    /// <summary>Ana kumaş gramajı (g/m²).</summary>
    public int? FabricWeight { get; set; }
    public string? CareInstructions { get; set; }

    /// <summary>Menşe ülkesi (ISO 3166-1 alfa-2).</summary>
    public string OriginCountryCode { get; set; } = "TR";

    // Ticari ve e-belge
    /// <summary>Gümrük tarife istatistik pozisyonu (12 hane); ihracat faturasında zorunludur.</summary>
    public string? GtipCode { get; set; }

    /// <summary>KDV oranı (%); hazır giyimde %10.</summary>
    public decimal VatRate { get; set; } = 10;
    public StockUnit SalesUnit { get; set; } = StockUnit.Piece;
    public decimal? ListPrice { get; set; }
    public string Currency { get; set; } = "TRY";

    public bool IsActive { get; set; } = true;
}
