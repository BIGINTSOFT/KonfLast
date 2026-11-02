using ERP.Boyahane.Domain.Enums;
using ERP.Core.Domain.Common;

namespace ERP.Boyahane.Domain.Entities;

/// <summary>Boya ya da kimyasal lotu. Renk sapmasında hangi lotun kullanıldığı buradan izlenir.</summary>
public class BoyahaneChemicalLot : AuditableEntity
{
    public string LotNo { get; set; } = null!;

    public int DyeChemicalId { get; set; }
    public BoyahaneDyeChemical DyeChemical { get; set; } = null!;

    /// <summary>Tedarikçinin lot numarası.</summary>
    public string? SupplierLotNo { get; set; }

    public int? SupplierId { get; set; }
    public BoyahaneSupplier? Supplier { get; set; }

    /// <summary>Tedarikçi kartının adı; kart yokken girilmiş lotlarda serbest metindir.</summary>
    public string? SupplierName { get; set; }

    /// <summary>Satın alma siparişinden geldiyse sipariş satırı.</summary>
    public int? PurchaseOrderLineId { get; set; }
    public BoyahanePurchaseOrderLine? PurchaseOrderLine { get; set; }

    public DateOnly ReceiptDate { get; set; }

    /// <summary>Son kullanma tarihi; geçmiş lottan otomatik düşülmez.</summary>
    public DateOnly? ExpiryDate { get; set; }

    public decimal ReceivedKilograms { get; set; }
    public decimal RemainingKilograms { get; set; }

    /// <summary>Kilogram maliyeti; girilmişse parti maliyetine yansır.</summary>
    public decimal? UnitCost { get; set; }
    public string Currency { get; set; } = "TRY";
    public string? Notes { get; set; }

    /// <summary>Lotun depodaki yeri; lot tek adreste durur.</summary>
    public int? LocationId { get; set; }
    public BoyahaneWarehouseLocation? Location { get; set; }
}

/// <summary>Lot hareketi: giriş, tüketim ya da sayım düzeltmesi. Kayıtlar silinmez.</summary>
[NotAudited]
public class BoyahaneChemicalMovement
{
    public long Id { get; set; }

    public int LotId { get; set; }
    public BoyahaneChemicalLot Lot { get; set; } = null!;

    public ChemicalMovementType Type { get; set; }

    /// <summary>Girişte artı, tüketimde eksi.</summary>
    public decimal Kilograms { get; set; }

    /// <summary>Parti no ya da düzeltme gerekçesi.</summary>
    public string? Reference { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
}

/// <summary>Boyama tüketiminin karşılandığı lot.</summary>
[NotAudited]
public class BoyahaneConsumptionLot
{
    public long Id { get; set; }

    public int ConsumptionId { get; set; }
    public BoyahaneBatchConsumption Consumption { get; set; } = null!;

    public int LotId { get; set; }
    public BoyahaneChemicalLot Lot { get; set; } = null!;

    public decimal Kilograms { get; set; }
}
