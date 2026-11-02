using ERP.Core.Domain.Common;
using ERP.Konfeksiyon.Domain.Enums;

namespace ERP.Konfeksiyon.Domain.Entities;

/// <summary>
/// Vardiya. Saatler fabrikanın yerel saatidir. Bitiş başlangıçtan küçükse vardiya gece yarısını aşar
/// (örn. 22:00 - 06:00).
/// </summary>
public class Shift : SoftDeletableEntity, IActivatable
{
    public const int MinutesPerDay = 24 * 60;

    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>Vardiyanın uzunluğu (dakika). Başlangıç ve bitiş aynıysa 24 saattir.</summary>
    public int LengthMinutes => OffsetOf(EndTime) is var end and > 0 ? end : MinutesPerDay;

    /// <summary>Verilen saatin vardiya başlangıcından kaç dakika sonra olduğu (0 - 1439).</summary>
    public int OffsetOf(TimeOnly time) =>
        (((int)(time - StartTime).TotalMinutes % MinutesPerDay) + MinutesPerDay) % MinutesPerDay;

    /// <summary>Verilen yerel saat vardiyanın içinde mi (başlangıç dahil, bitiş hariç).</summary>
    public bool Contains(TimeOnly time) => OffsetOf(time) < LengthMinutes;
}

/// <summary>
/// Mola grubu: molaya birlikte çıkan operatörler. Bant tamamen durmasın diye gruplar farklı saatlerde
/// molaya çıkar.
/// </summary>
public class BreakGroup : SoftDeletableEntity, IActivatable
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; } = true;
}

/// <summary>Vardiyanın planlı molası (çay, yemek). Grup boşsa vardiyadaki herkes içindir.</summary>
public class BreakSchedule : SoftDeletableEntity, IActivatable
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;

    public int ShiftId { get; set; }
    public Shift Shift { get; set; } = null!;

    public int? BreakGroupId { get; set; }
    public BreakGroup? BreakGroup { get; set; }

    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    /// <summary>Mola saati geldiğinde elinde işi olan operatörün ne yapacağı.</summary>
    public BreakPolicy Policy { get; set; } = BreakPolicy.AfterBundle;

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Mola, vardiyanın içinde ve başlangıcı bitişinden önce mi? Gece vardiyasında saatler vardiya
    /// başlangıcına göre değerlendirilir (22:00 vardiyasında 02:00 molası geçerlidir).
    /// </summary>
    public static bool FitsIn(Shift shift, TimeOnly start, TimeOnly end)
    {
        var from = shift.OffsetOf(start);
        var to = shift.OffsetOf(end);
        // Mola tam vardiya bitiminde sona erebilir: bitişin ofseti vardiya uzunluğuna eşit sayılır.
        if (to == 0 || (end == shift.EndTime && shift.LengthMinutes < Shift.MinutesPerDay))
        {
            to = shift.LengthMinutes;
        }

        return from < to && to <= shift.LengthMinutes;
    }
}

/// <summary>
/// Oturum içindeki bir duraklama: planlı mola, operatörün kendi arası, makine arızası ya da demet
/// bekleme. Türler birbirine karıştırılmaz; performans hesabında her biri ayrı değerlendirilir.
/// Kayıtlar silinmez.
/// </summary>
[NotAudited]
public class WorkSessionPause
{
    public long Id { get; set; }

    public long WorkSessionId { get; set; }
    public WorkSession WorkSession { get; set; } = null!;

    public WorkSessionPauseType Type { get; set; }

    /// <summary>Yalnızca operatörün kendi arasında (<see cref="WorkSessionPauseType.Pause"/>) dolu: ara nedeni.</summary>
    public int? StopReasonId { get; set; }
    public StopReason? StopReason { get; set; }

    /// <summary>Yalnızca planlı molada dolu: hangi mola planı.</summary>
    public int? BreakScheduleId { get; set; }
    public BreakSchedule? BreakSchedule { get; set; }

    public string? Note { get; set; }

    /// <summary>Sunucu saati.</summary>
    public DateTime StartedAt { get; set; }

    /// <summary>Boşsa duraklama sürüyor.</summary>
    public DateTime? EndedAt { get; private set; }

    public bool IsOpen => EndedAt is null;

    public void End(DateTime utcNow)
    {
        if (!IsOpen)
        {
            throw new InvalidOperationException("Duraklama zaten sonlandırılmış.");
        }

        EndedAt = utcNow;
    }
}

/// <summary>
/// Duruş nedeni (ana veri): tablette "Ara ver" ve "Arıza bildir" seçenekleri buradan gelir. Geçmiş
/// kayıtlarda kullanılan neden silinse de adıyla görünmeye devam eder (soft delete).
/// </summary>
public class StopReason : SoftDeletableEntity, IActivatable
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public StopReasonCategory Category { get; set; }

    /// <summary>Tabletteki sıra; küçük olan önce gösterilir.</summary>
    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
