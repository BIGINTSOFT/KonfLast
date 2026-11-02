using ERP.Core.Domain.Common;
using ERP.Konfeksiyon.Domain.Enums;

namespace ERP.Konfeksiyon.Domain.Entities;

public class Factory : SoftDeletableEntity, IActivatable
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;

    // Adres
    public string? AddressLine { get; set; }
    public string? District { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string CountryCode { get; set; } = "TR";

    // İletişim ve yönetim
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? ManagerName { get; set; }

    /// <summary>SGK işyeri sicil numarası.</summary>
    public string? SgkWorkplaceNo { get; set; }

    /// <summary>Kapalı alan (m²).</summary>
    public int? IndoorArea { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>Fabrika içindeki üretim alanı (kat, salon, bölüm).</summary>
public class ProductionArea : SoftDeletableEntity, IActivatable
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public int FactoryId { get; set; }
    public Factory Factory { get; set; } = null!;
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Bant / hat. Hangi makinelerin bantta olduğu <see cref="LineMachineAssignment"/>, bantın ne zaman
/// hangi siparişi ürettiği <see cref="LineProductionPlan"/> ile tutulur; ikisi de zamanla değişir.
/// </summary>
public class ProductionLine : SoftDeletableEntity, IActivatable
{
    public string LineCode { get; set; } = null!;
    public string Name { get; set; } = null!;
    public int ProductionAreaId { get; set; }
    public ProductionArea ProductionArea { get; set; } = null!;
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Makinenin bir banta atanması. Kayıt silinmez: makine banttan çıkarıldığında
/// <see cref="UnassignedAt"/> doldurulur; böylece makinenin hangi tarihte hangi bantta olduğu bilinir.
/// </summary>
public class LineMachineAssignment : BaseEntity
{
    public int ProductionLineId { get; set; }
    public ProductionLine ProductionLine { get; set; } = null!;

    public int MachineId { get; set; }
    public Machine Machine { get; set; } = null!;

    /// <summary>Makinenin bant üzerindeki sırası (1'den başlar).</summary>
    public int Position { get; set; }

    public DateTime AssignedAt { get; set; }

    /// <summary>Boşsa makine hâlâ bu banttadır.</summary>
    public DateTime? UnassignedAt { get; private set; }

    public bool IsCurrent => UnassignedAt is null;

    public void End(DateTime utcNow)
    {
        if (!IsCurrent)
        {
            throw new InvalidOperationException("Atama zaten sonlandırılmış.");
        }

        UnassignedAt = utcNow;
    }
}

/// <summary>Bantın belirli bir zaman aralığında bir siparişten üreteceği adet.</summary>
public class LineProductionPlan : AuditableEntity
{
    public int ProductionLineId { get; set; }
    public ProductionLine ProductionLine { get; set; } = null!;

    public int ProductionOrderId { get; set; }
    public ProductionOrder ProductionOrder { get; set; } = null!;

    public DateTime PlannedStart { get; set; }
    public DateTime PlannedEnd { get; set; }
    public int PlannedQuantity { get; set; }
    public LinePlanStatus Status { get; private set; } = LinePlanStatus.Planned;
    public string? Notes { get; set; }

    /// <summary>Aynı planın iki kullanıcı tarafından eşzamanlı değiştirilmesini engeller.</summary>
    public byte[] RowVersion { get; set; } = null!;

    public bool IsEditable => Status == LinePlanStatus.Planned;

    public void Cancel()
    {
        if (!IsEditable)
        {
            throw new InvalidOperationException($"{Status} durumundaki plan iptal edilemez.");
        }

        Status = LinePlanStatus.Cancelled;
    }
}
