using ERP.Core.Domain.Common;
using ERP.Boyahane.Domain.Enums;

namespace ERP.Boyahane.Domain.Entities;

/// <summary>Vardiya. Saatler fabrikanın yerel saatidir. Bitiş başlangıçtan küçükse vardiya gece yarısını aşar.</summary>
public class BoyahaneShift : SoftDeletableEntity, IActivatable
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Mola grubu: molaya birlikte çıkan operatörler. Üretim tamamen durmasın diye gruplar farklı saatlerde çıkar.</summary>
public class BoyahaneBreakGroup : SoftDeletableEntity, IActivatable
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; } = true;
}

/// <summary>Vardiyanın planlı molası (çay, yemek). Grup boşsa vardiyadaki herkes içindir.</summary>
public class BoyahaneBreakSchedule : SoftDeletableEntity, IActivatable
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;

    public int ShiftId { get; set; }
    public BoyahaneShift Shift { get; set; } = null!;

    public int? BreakGroupId { get; set; }
    public BoyahaneBreakGroup? BreakGroup { get; set; }

    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    /// <summary>Mola saati geldiğinde süren adımı olan operatörün ne yapacağı.</summary>
    public BoyahaneBreakPolicy Policy { get; set; } = BoyahaneBreakPolicy.AfterStep;

    public bool IsActive { get; set; } = true;
}
