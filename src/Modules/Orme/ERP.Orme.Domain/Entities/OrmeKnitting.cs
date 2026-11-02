using ERP.Core.Domain.Common;
using ERP.Orme.Domain.Enums;

namespace ERP.Orme.Domain.Entities;

/// <summary>Örme emri: onaylı sipariş satırından açılır, bir ya da birkaç makinede çalışır.</summary>
public class OrmeKnitOrder : AuditableEntity
{
    public string KnitOrderNo { get; set; } = null!;

    public int OrderLineId { get; set; }
    public OrmeOrderLine OrderLine { get; set; } = null!;

    /// <summary>Plan tarihi; kapasite planında emrin bu günden önce başlamayacağı kabul edilir.</summary>
    public DateOnly PlannedDate { get; set; }
    public decimal PlannedKilograms { get; set; }

    /// <summary>Kapasite planında emrin sıraya alındığı makine; makineye fiilen atama <see cref="Machines"/> kayıtlarıdır.</summary>
    public int? PlannedMachineId { get; set; }
    public OrmeMachine? PlannedMachine { get; set; }

    /// <summary>Planlanan makinenin kuyruğundaki sıra (1'den başlar).</summary>
    public int? PlanSequence { get; set; }

    public DateOnly? CompletedOn { get; set; }
    public string? Notes { get; set; }
    public KnitOrderStatus Status { get; set; } = KnitOrderStatus.Open;

    public ICollection<OrmeKnitOrderMachine> Machines { get; } = [];
    public ICollection<OrmeYarnIssue> Issues { get; } = [];
    public ICollection<OrmeYarnTakeBack> TakeBacks { get; } = [];
    public ICollection<OrmeFabricRoll> Rolls { get; } = [];
}

/// <summary>Emrin makinesi. Makineden alınınca kayıt geçmiş olarak kalır; bir makinede aynı anda tek emir çalışır.</summary>
public class OrmeKnitOrderMachine : AuditableEntity
{
    public int KnitOrderId { get; set; }
    public OrmeKnitOrder KnitOrder { get; set; } = null!;

    public int MachineId { get; set; }
    public OrmeMachine Machine { get; set; } = null!;

    public DateOnly AssignedOn { get; set; }

    /// <summary>Boşsa emir bu makinede çalışıyordur.</summary>
    public DateOnly? ReleasedOn { get; set; }
}

/// <summary>Lottan örme emrine verilen iplik.</summary>
public class OrmeYarnIssue : AuditableEntity
{
    public int KnitOrderId { get; set; }
    public OrmeKnitOrder KnitOrder { get; set; } = null!;

    public int YarnLotId { get; set; }
    public OrmeYarnLot YarnLot { get; set; } = null!;

    public DateOnly IssuedOn { get; set; }
    public decimal Kilograms { get; set; }
}

/// <summary>Emirden artıp lota geri alınan iplik: verilenden düşer, fire buna göre çıkar.</summary>
public class OrmeYarnTakeBack : AuditableEntity
{
    public int KnitOrderId { get; set; }
    public OrmeKnitOrder KnitOrder { get; set; } = null!;

    public int YarnLotId { get; set; }
    public OrmeYarnLot YarnLot { get; set; } = null!;

    public DateOnly TakenOn { get; set; }
    public decimal Kilograms { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Makineden çıkan ham top. Kilo tartılır, metre isteğe bağlıdır. Top emre bağlıdır; emre birden çok lot verilebildiği için
/// lot bağlantısı emrin kullandığı lotlar üzerinden izlenir.
/// </summary>
public class OrmeFabricRoll : AuditableEntity
{
    public string RollNo { get; set; } = null!;

    public int KnitOrderId { get; set; }
    public OrmeKnitOrder KnitOrder { get; set; } = null!;

    public int MachineId { get; set; }
    public OrmeMachine Machine { get; set; } = null!;

    public DateOnly CutOn { get; set; }
    public decimal Kilograms { get; set; }
    public decimal? Meters { get; set; }
    public string? Notes { get; set; }
    public FabricRollStatus Status { get; set; } = FabricRollStatus.PendingInspection;

    /// <summary>Tabletten gelen kaydın kimliği; yeniden gönderim ikinci top açmaz.</summary>
    public Guid? ClientRequestId { get; set; }

    /// <summary>Topu tabletten kaydeden personel; ofiste girilen topta boş.</summary>
    public int? CutByOperatorId { get; set; }
    public OrmeOperator? CutByOperator { get; set; }

    public DateOnly? InspectedOn { get; set; }

    /// <summary>Puanda kullanılan metre: ölçülen, makinede girilen ya da tahmin.</summary>
    public decimal? InspectedMeters { get; set; }

    /// <summary>Metre ölçülmedi, tartılan kilo, gramaj ve enden tahmin edildi.</summary>
    public bool InspectedMetersEstimated { get; set; }

    public decimal? InspectedWidthCm { get; set; }
    public decimal? InspectedWeightGsm { get; set; }

    /// <summary>Ölçülen en ve gramajın kontrol anındaki karttan toleransı aşan sapması; kaliteyi değiştirmez.</summary>
    public string? SpecDeviation { get; set; }

    public int? TotalPoints { get; set; }
    public decimal? PointsPer100M2 { get; set; }

    /// <summary>Puandan hesaplanan kalite; Status yetkili kararıyla bundan farklı olabilir.</summary>
    public FabricRollStatus? CalculatedStatus { get; set; }

    /// <summary>Hesaplanandan farklı kararın gerekçesi.</summary>
    public string? OverrideNote { get; set; }

    public string? InspectionNotes { get; set; }

    public ICollection<OrmeRollDefect> Defects { get; } = [];
}

/// <summary>Topta işaretlenen hata; puan kayıtta sabitlenir.</summary>
public class OrmeRollDefect : AuditableEntity
{
    public int RollId { get; set; }
    public OrmeFabricRoll Roll { get; set; } = null!;

    public int DefectTypeId { get; set; }
    public OrmeDefectType DefectType { get; set; } = null!;

    /// <summary>Topun başından itibaren kaçıncı metrede.</summary>
    public decimal AtMeter { get; set; }

    public decimal LengthCm { get; set; }
    public int Points { get; set; }
}

/// <summary>Örme hata türü (iğne çizgisi, ilmek kaçığı, delik ...).</summary>
public class OrmeDefectType : SoftDeletableEntity, IActivatable
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;

    /// <summary>Uzunluğundan bağımsız her zaman 4 puan (örn. delik).</summary>
    public bool AlwaysFourPoints { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>Örme kalite sınırları (tek kayıt): 100 m² başına puan, en ve gramaj sapma toleransı. Müşteri kartında müşteriye özel puan sınırı verilebilir.</summary>
public class OrmeQualitySettings : AuditableEntity
{
    public decimal FirstQualityMaxPoints { get; set; } = 20;
    public decimal SecondQualityMaxPoints { get; set; } = 40;
    public decimal WidthTolerancePercent { get; set; } = 3;
    public decimal WeightTolerancePercent { get; set; } = 5;
}
