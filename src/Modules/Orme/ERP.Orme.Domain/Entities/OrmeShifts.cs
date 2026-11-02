using ERP.Core.Domain.Common;

namespace ERP.Orme.Domain.Entities;

/// <summary>Vardiya. Saatler fabrikanın yerel saatidir. Bitiş başlangıçtan küçükse vardiya gece yarısını aşar.</summary>
public class OrmeShift : SoftDeletableEntity, IActivatable
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Mola grubu: molaya birlikte çıkan operatörler. Üretim tamamen durmasın diye gruplar farklı saatlerde çıkar.</summary>
public class OrmeBreakGroup : SoftDeletableEntity, IActivatable
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; } = true;
}

/// <summary>Vardiyanın planlı molası (çay, yemek). Grup boşsa vardiyadaki herkes içindir.</summary>
public class OrmeBreakSchedule : SoftDeletableEntity, IActivatable
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;

    public int ShiftId { get; set; }
    public OrmeShift Shift { get; set; } = null!;

    public int? BreakGroupId { get; set; }
    public OrmeBreakGroup? BreakGroup { get; set; }

    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    public bool IsActive { get; set; } = true;
}
