using ERP.Core.Domain.Common;
using ERP.Dokuma.Domain.Enums;

namespace ERP.Dokuma.Domain.Entities;

/// <summary>Malzeme kartı: haşıl kimyasalı, yedek parça ve sarf malzemesi. Stok lot bazında tutulur.</summary>
public class DokumaMaterial : SoftDeletableEntity, IActivatable
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public MaterialType Type { get; set; } = MaterialType.SizingChemical;
    public MaterialUnit Unit { get; set; } = MaterialUnit.Kilogram;

    /// <summary>Kritik stok: stok bunun altına inerse kritik listesine ve tedarik önerisine girer.</summary>
    public decimal? MinimumStock { get; set; }

    /// <summary>Tedarik önerisinde siparişin açılacağı tedarikçi.</summary>
    public int? PreferredSupplierId { get; set; }
    public DokumaSupplier? PreferredSupplier { get; set; }

    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Malzeme lotu: bir girişin miktarı ve kalanı. Çıkışlar en eski lottan başlayarak düşülür.</summary>
public class DokumaMaterialLot : AuditableEntity
{
    public string LotNo { get; set; } = null!;

    public int MaterialId { get; set; }
    public DokumaMaterial Material { get; set; } = null!;

    public DateOnly ReceiptDate { get; set; }

    public int? SupplierId { get; set; }
    public DokumaSupplier? Supplier { get; set; }

    /// <summary>Tedarikçinin lot / parti numarası.</summary>
    public string? SupplierLotNo { get; set; }

    public string? DespatchNo { get; set; }

    /// <summary>Giriş miktarı (malzemenin biriminde).</summary>
    public decimal Quantity { get; set; }

    /// <summary>Lotta kalan; hareketlerle birlikte güncellenir.</summary>
    public decimal RemainingQuantity { get; set; }

    /// <summary>Birim fiyat; satın alma tesliminde sipariş fiyatından gelir.</summary>
    public decimal? UnitPrice { get; set; }
    public string Currency { get; set; } = "TRY";

    /// <summary>Satın alma siparişinden geldiyse sipariş satırı.</summary>
    public int? PurchaseOrderLineId { get; set; }
    public DokumaPurchaseOrderLine? PurchaseOrderLine { get; set; }

    public string? Notes { get; set; }

    public ICollection<DokumaMaterialMovement> Movements { get; } = [];
}

/// <summary>Malzeme stok hareketi: giriş, çıkış, haşıl tüketimi, düzeltme ya da sayım. Kayıtlar silinmez (giriş hariç).</summary>
[NotAudited]
public class DokumaMaterialMovement : AuditableEntity
{
    public int MaterialId { get; set; }
    public DokumaMaterial Material { get; set; } = null!;

    public int LotId { get; set; }
    public DokumaMaterialLot Lot { get; set; } = null!;

    public MaterialMovementType Type { get; set; }
    public DateOnly MovementDate { get; set; }

    /// <summary>Girişte artı, çıkışta eksi.</summary>
    public decimal Quantity { get; set; }

    /// <summary>Yedek parça çıkışında parçanın takıldığı tezgâh (isteğe bağlı).</summary>
    public int? LoomId { get; set; }
    public DokumaLoom? Loom { get; set; }

    /// <summary>Çıkışın yapıldığı bölüm (örn. hazırlık, bakım).</summary>
    public string? Department { get; set; }

    /// <summary>Çıkış nedeni ya da düzeltme gerekçesi.</summary>
    public string? Reason { get; set; }

    /// <summary>Haşıl tüketiminde levent.</summary>
    public int? BeamId { get; set; }
    public DokumaBeam? Beam { get; set; }

    /// <summary>Satın alma sipariş no, levent no gibi belge referansı.</summary>
    public string? Reference { get; set; }
}

/// <summary>
/// Haşıl reçetesi: artikelin çözgüsüne uygulanan haşıl banyosu. Bir artikelin birden çok reçetesi olabilir, biri
/// varsayılandır. Kimyasallar banyo yüzdesi olarak girilir; levent için kimyasal miktarı ipliğin ağırlığı × alım ×
/// banyo yüzdesi ile tahmin edilir.
/// </summary>
public class DokumaSizingRecipe : SoftDeletableEntity, IActivatable
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;

    public int ArticleId { get; set; }
    public DokumaArticle Article { get; set; } = null!;

    /// <summary>Artikelin varsayılan reçetesi; levent haşıla girerken önce bu seçilir.</summary>
    public bool IsDefault { get; set; }

    /// <summary>Banyo konsantrasyonu (% katı madde).</summary>
    public decimal? ConcentrationPercent { get; set; }

    /// <summary>Banyo sıcaklığı (°C).</summary>
    public decimal? BathTemperatureC { get; set; }

    /// <summary>Viskozite (saniye, akış kabı ile).</summary>
    public decimal? ViscositySeconds { get; set; }

    /// <summary>Haşıl makinesi hızı (m/dk).</summary>
    public decimal? MachineSpeedMetersPerMinute { get; set; }

    /// <summary>Hedef alım (%): ipliğin ağırlığına göre aldığı banyo.</summary>
    public decimal? TargetPickUpPercent { get; set; }

    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<DokumaSizingRecipeLine> Lines { get; } = [];
}

/// <summary>Haşıl reçetesindeki kimyasal: banyodaki yüzdesi.</summary>
public class DokumaSizingRecipeLine : AuditableEntity
{
    public int RecipeId { get; set; }
    public DokumaSizingRecipe Recipe { get; set; } = null!;

    /// <summary>Reçete içindeki sıra (1'den başlar).</summary>
    public int Sequence { get; set; }

    public int MaterialId { get; set; }
    public DokumaMaterial Material { get; set; } = null!;

    /// <summary>Banyodaki oranı (%).</summary>
    public decimal BathPercent { get; set; }
}
