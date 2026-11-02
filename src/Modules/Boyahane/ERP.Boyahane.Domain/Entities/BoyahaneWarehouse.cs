using ERP.Boyahane.Domain.Enums;
using ERP.Core.Domain.Common;

namespace ERP.Boyahane.Domain.Entities;

public class BoyahaneWarehouse : SoftDeletableEntity, IActivatable
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public BoyahaneWarehouseType Type { get; set; } = BoyahaneWarehouseType.RawFabric;
    public bool IsActive { get; set; } = true;

    public ICollection<BoyahaneWarehouseLocation> Locations { get; } = [];
}

/// <summary>Depo adresi (bölge / raf / göz). Her deponun bir genel adresi vardır; adres seçilmeyen mal oraya yazılır.</summary>
public class BoyahaneWarehouseLocation : SoftDeletableEntity, IActivatable
{
    public const string DefaultCode = "GENEL";

    public int WarehouseId { get; set; }
    public BoyahaneWarehouse Warehouse { get; set; } = null!;

    /// <summary>Dolu seviyelerin tire ile birleşimi (örn. A-01-03); depo içinde benzersizdir.</summary>
    public string Code { get; private set; } = null!;

    public string Zone { get; private set; } = null!;
    public string? Rack { get; private set; }
    public string? Bin { get; private set; }
    public string? Description { get; set; }

    /// <summary>Deponun genel adresi: silinemez, pasif yapılamaz.</summary>
    public bool IsDefault { get; private set; }

    public bool IsActive { get; set; } = true;

    public static BoyahaneWarehouseLocation CreateDefault()
    {
        var location = new BoyahaneWarehouseLocation { IsDefault = true, Description = "Adres seçilmeyen mal" };
        location.SetAddress(DefaultCode, null, null);
        return location;
    }

    public static string ComposeCode(string zone, string? rack, string? bin) =>
        string.Join('-', new[] { zone, rack, bin }.Where(part => !string.IsNullOrEmpty(part)));

    public void SetAddress(string zone, string? rack, string? bin)
    {
        Zone = zone;
        Rack = string.IsNullOrEmpty(rack) ? null : rack;
        Bin = string.IsNullOrEmpty(bin) ? null : bin;
        Code = ComposeCode(zone, Rack, Bin);
    }
}

/// <summary>Malın adres değişimi ya da depolar arası transferi. Kayıtlar silinmez.</summary>
[NotAudited]
public class BoyahaneLocationMovement
{
    public long Id { get; set; }

    public WarehouseGoodsType GoodsType { get; set; }
    public int GoodsId { get; set; }
    public string GoodsNo { get; set; } = null!;
    public LocationMovementType Type { get; set; }

    public int? FromLocationId { get; set; }
    public BoyahaneWarehouseLocation? FromLocation { get; set; }

    public int ToLocationId { get; set; }
    public BoyahaneWarehouseLocation ToLocation { get; set; } = null!;

    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
}
