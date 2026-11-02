using ERP.Boyahane.Domain.Enums;
using ERP.Core.Domain.Common;

namespace ERP.Boyahane.Domain.Entities;

/// <summary>Reddedilen malın çıkış belgesi: fason müşteriye iade irsaliyesi ya da hurda. Onaylanınca mal depodan düşer.</summary>
public class BoyahaneGoodsReturn : AuditableEntity
{
    public string ReturnNo { get; set; } = null!;
    public GoodsReturnKind Kind { get; set; } = GoodsReturnKind.CustomerReturn;

    /// <summary>İadede malın sahibi; hurdada boş.</summary>
    public int? CustomerId { get; set; }
    public BoyahaneCustomer? Customer { get; set; }

    public DateOnly ReturnDate { get; set; }
    public string? VehiclePlate { get; set; }
    public string? DriverName { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? Notes { get; set; }
    public GoodsReturnStatus Status { get; set; } = GoodsReturnStatus.Draft;

    public ICollection<BoyahaneGoodsReturnItem> Items { get; } = [];
}

/// <summary>Belgedeki top ya da iplik lotu. Numara, ölçü ve gerekçe belge yazılırken sabitlenir.</summary>
public class BoyahaneGoodsReturnItem : AuditableEntity
{
    public int ReturnId { get; set; }
    public BoyahaneGoodsReturn Return { get; set; } = null!;

    public int? RollId { get; set; }
    public BoyahaneFabricRoll? Roll { get; set; }

    public int? YarnLotId { get; set; }
    public BoyahaneYarnLot? YarnLot { get; set; }

    public string ItemNo { get; set; } = null!;
    public string CardName { get; set; } = null!;
    public decimal Kilograms { get; set; }
    public decimal? Meters { get; set; }
    public string? Reason { get; set; }
}
