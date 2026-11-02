namespace ERP.Core.Application.Common;

/// <summary>
/// Saha oturumu ayarları; Konfeksiyon ile aynı <c>Production</c> bölümünden okunur, böylece fabrikada tek yerden yönetilir.
/// </summary>
public sealed class FloorSessionOptions
{
    public const string SectionName = "Production";

    /// <summary>Fabrikanın saat dilimi; vardiya ve mola saatleri bu yerel saate göredir.</summary>
    public string TimeZone { get; set; } = "Europe/Istanbul";

    /// <summary>Planlı mola, bitiş saatinden en fazla bu kadar dakika sonrasına kadar başlatılabilir.</summary>
    public int BreakLateStartMinutes { get; set; } = 30;

    /// <summary>Vardiyası biten açık oturumların kendiliğinden kapatılması.</summary>
    public bool SessionAutoCloseEnabled { get; set; } = true;

    /// <summary>Oturum, vardiyanın bitişinden bu kadar dakika sonra kapatılır.</summary>
    public int AutoCloseAfterShiftMinutes { get; set; } = 60;

    /// <summary>Hiçbir vardiyaya denk gelmeyen oturumun açık kalabileceği en uzun süre (saat).</summary>
    public int MaxSessionHours { get; set; } = 16;

    /// <summary>Oturum açık, iş yoksa tablet bu kadar dakika sonra operatöre sorar. 0: sorulmaz.</summary>
    public int IdlePromptMinutes { get; set; } = 5;

    public DateTime ToLocal(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), TimeZoneInfo.FindSystemTimeZoneById(TimeZone));
}

/// <summary>Vardiyası biten açık oturumları kapatan servis; arka plan görevi her modül için bunu çağırır.</summary>
public interface IFloorSessionExpiry
{
    /// <summary>Kapatılan oturum sayısını döner.</summary>
    Task<int> CloseExpiredAsync(CancellationToken cancellationToken);
}

/// <summary>Vardiyanın saatleri (fabrika yerel saati). Bitiş başlangıçtan küçükse vardiya gece yarısını aşar.</summary>
public readonly record struct ShiftWindow(TimeOnly Start, TimeOnly End)
{
    private const int MinutesPerDay = 24 * 60;

    /// <summary>Vardiyanın uzunluğu (dakika).</summary>
    public int LengthMinutes => OffsetOf(End) is var end and > 0 ? end : MinutesPerDay;

    /// <summary>Verilen saatin vardiya başlangıcından kaç dakika sonra olduğu (0 - 1439).</summary>
    public int OffsetOf(TimeOnly time) => (((int)(time - Start).TotalMinutes % MinutesPerDay) + MinutesPerDay) % MinutesPerDay;

    /// <summary>Verilen yerel saat vardiyanın içinde mi (başlangıç dahil, bitiş hariç).</summary>
    public bool Contains(TimeOnly time) => OffsetOf(time) < LengthMinutes;

    /// <summary>Mola vardiyanın içinde ve başlangıcı bitişinden önce mi? Gece vardiyasında saatler vardiya başlangıcına göre değerlendirilir.</summary>
    public bool Fits(TimeOnly start, TimeOnly end)
    {
        var from = OffsetOf(start);
        var to = OffsetOf(end);

        // Mola tam vardiya bitiminde sona erebilir: bitişin ofseti vardiya uzunluğuna eşit sayılır.
        if (to == 0 || (end == End && LengthMinutes < MinutesPerDay))
        {
            to = LengthMinutes;
        }

        return from < to && to <= LengthMinutes;
    }

    /// <summary>İki saat arasındaki süre; bitiş başlangıçtan küçük ya da eşitse ertesi güne sarar.</summary>
    public static int MinutesBetween(TimeOnly start, TimeOnly end)
    {
        var minutes = (int)(end - start).TotalMinutes;
        return minutes > 0 ? minutes : minutes + MinutesPerDay;
    }
}

public static class FloorSessionTimes
{
    /// <summary>
    /// Oturumun kendiliğinden kapanacağı an: giriş saatinin denk geldiği vardiyanın bitişi + bekleme süresi; hiçbir vardiyaya
    /// denk gelmiyorsa girişten itibaren en uzun oturum süresi.
    /// </summary>
    public static DateTime ExpiresAt(DateTime loginAt, IReadOnlyList<ShiftWindow> shifts, FloorSessionOptions options) =>
        ShiftEndOf(loginAt, shifts, options) is { } shiftEnd
            ? shiftEnd.AddMinutes(options.AutoCloseAfterShiftMinutes)
            : loginAt.AddHours(options.MaxSessionHours);

    /// <summary>Giriş saatinin denk geldiği vardiyanın bitiş anı; hiçbir vardiyaya denk gelmiyorsa boş.</summary>
    public static DateTime? ShiftEndOf(DateTime loginAt, IReadOnlyList<ShiftWindow> shifts, FloorSessionOptions options)
    {
        var loginTime = TimeOnly.FromDateTime(options.ToLocal(loginAt));
        foreach (var shift in shifts)
        {
            if (shift.Contains(loginTime))
            {
                return loginAt.AddMinutes(shift.LengthMinutes - shift.OffsetOf(loginTime));
            }
        }

        return null;
    }
}
