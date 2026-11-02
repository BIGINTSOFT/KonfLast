using ERP.Boyahane.Domain.Enums;
using ERP.Core.Domain.Common;

namespace ERP.Boyahane.Domain.Entities;

/// <summary>İplik girişi: kilo ve lot bazında takip edilir, top yerine bobin ya da çile sayısı tutulur.</summary>
public class BoyahaneYarnLot : AuditableEntity
{
    public string LotNo { get; set; } = null!;
    public DateOnly ReceiptDate { get; set; }

    /// <summary>Malın sahibi olan müşteri (fason); boşsa mal işletmenindir.</summary>
    public int? CustomerId { get; set; }
    public BoyahaneCustomer? Customer { get; set; }

    public int YarnId { get; set; }
    public BoyahaneYarn Yarn { get; set; } = null!;

    public string? DespatchNo { get; set; }

    /// <summary>Müşterinin kendi lot numarası.</summary>
    public string? CustomerLotNo { get; set; }

    public YarnPackageType PackageType { get; set; } = YarnPackageType.Bobbin;
    public int PackageCount { get; set; }
    public decimal Kilograms { get; set; }
    public string? Notes { get; set; }

    /// <summary>Lot kabulde uygun sayılır; hatalı çıkarsa gerekçeyle reddedilir, reddedilen lot siparişe ve partiye giremez.</summary>
    public YarnLotStatus Status { get; set; } = YarnLotStatus.Approved;
    public string? RejectionNote { get; set; }

    /// <summary>Lotun bağlı olduğu sipariş satırı; lot bölünmeden tek satıra bağlanır.</summary>
    public int? OrderLineId { get; set; }
    public BoyahaneOrderLine? OrderLine { get; set; }

    /// <summary>Lotun depodaki yeri; makineye girince son adres olarak kalır.</summary>
    public int? LocationId { get; set; }
    public BoyahaneWarehouseLocation? Location { get; set; }
}
