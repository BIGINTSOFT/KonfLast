using ERP.Core.Domain.Common;
using ERP.Konfeksiyon.Domain.Enums;

namespace ERP.Konfeksiyon.Domain.Entities;

public class Machine : SoftDeletableEntity, IActivatable
{
    public string MachineCode { get; set; } = null!;
    public string MachineName { get; set; } = null!;
    public int MachineTypeId { get; set; }
    public MachineType MachineType { get; set; } = null!;

    /// <summary>
    /// Makinenin ana operasyonu (planlama ve bant dengeleme bunu kullanır). Makine ayrıca
    /// <see cref="MachineOperation"/> ile tanımlı ek operasyonları da yapabilir.
    /// </summary>
    public int? OperationId { get; set; }
    public Operation? Operation { get; set; }

    public MachineStatus Status { get; set; } = MachineStatus.Offline;

    // Teknik bilgiler
    public string? Brand { get; set; }
    public string? ModelNo { get; set; }
    public string? SerialNo { get; set; }
    public int? ManufactureYear { get; set; }

    /// <summary>Muhasebedeki demirbaş numarası.</summary>
    public string? AssetNo { get; set; }

    // Satın alma
    public DateOnly? PurchaseDate { get; set; }
    public int? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public DateOnly? WarrantyEndDate { get; set; }

    // Bakım
    /// <summary>Periyodik bakım aralığı (gün); sonraki bakım son bakım tarihinden hesaplanır.</summary>
    public int? MaintenanceIntervalDays { get; set; }
    public DateOnly? LastMaintenanceDate { get; set; }
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Kullanıcının (ya da tabletin) elle geçirebileceği durumlar. Çalışıyor / Boşta durumlarını
    /// yalnızca üretim akışı belirler.
    /// </summary>
    private static readonly MachineStatus[] ManualStatuses =
    [
        MachineStatus.Available, MachineStatus.Breakdown, MachineStatus.Maintenance, MachineStatus.Offline,
    ];

    public IReadOnlyList<MachineStatus> ManualStatusTargets => ManualStatuses.Where(s => s != Status).ToList();

    public bool CanSetStatusManually(MachineStatus target) => ManualStatuses.Contains(target) && target != Status;

    public void SetStatusManually(MachineStatus target)
    {
        if (!CanSetStatusManually(target))
        {
            throw new InvalidOperationException($"Makine {Status} durumundan {target} durumuna elle geçirilemez.");
        }

        Status = target;
    }
}

/// <summary>Makinenin ana operasyonuna ek olarak yapabildiği operasyon.</summary>
public class MachineOperation
{
    public int MachineId { get; set; }
    public Machine Machine { get; set; } = null!;

    public int OperationId { get; set; }
    public Operation Operation { get; set; } = null!;
}
