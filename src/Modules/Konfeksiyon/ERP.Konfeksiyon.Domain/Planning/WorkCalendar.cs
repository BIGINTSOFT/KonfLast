namespace ERP.Konfeksiyon.Domain.Planning;

/// <summary>Bir çalışma dönemi (vardiya): her çalışma gününde aynı saatte başlar; gece yarısını aşabilir.</summary>
/// <param name="Start">Başlangıç saati (yerel).</param>
/// <param name="LengthMinutes">Dönemin uzunluğu.</param>
/// <param name="Breaks">Dönem içindeki molalar: başlangıçtan uzaklık ve süre (dakika).</param>
public sealed record WorkPeriod(TimeOnly Start, int LengthMinutes, IReadOnlyList<(int OffsetMinutes, int LengthMinutes)> Breaks);

/// <summary>
/// Fabrikanın çalışma takvimi: hangi günlerde, hangi saatlerde çalışılır. Tüm zamanlar fabrikanın yerel
/// saatidir. Planlama, süreleri takvim saatiyle değil bu takvimdeki çalışma süresiyle hesaplar.
/// </summary>
public sealed class WorkCalendar
{
    private readonly IReadOnlyList<WorkPeriod> _periods;
    private readonly IReadOnlySet<DayOfWeek> _workingDays;

    public WorkCalendar(IReadOnlyList<WorkPeriod> periods, IReadOnlySet<DayOfWeek> workingDays)
    {
        _periods = periods;
        _workingDays = workingDays;
    }

    /// <summary>Hiç çalışma süresi içermeyen takvim (dönem ya da çalışma günü yok).</summary>
    public bool IsEmpty => _workingDays.Count == 0 || _periods.All(p => p.LengthMinutes <= 0);

    /// <summary>Gün boyu, haftanın verilen günlerinde çalışılan takvim (vardiya tanımlı değilken varsayım).</summary>
    public static WorkCalendar AroundTheClock(IReadOnlySet<DayOfWeek> workingDays) =>
        new([new WorkPeriod(TimeOnly.MinValue, 24 * 60, [])], workingDays);

    /// <summary>
    /// Aralıktaki çalışma dilimleri: sıralı, birbirine değmeyen ve aralığa kırpılmış. Vardiya, başladığı
    /// günün çalışma günü olmasına göre sayılır (cumartesi gecesi başlayan vardiya pazara taşsa da çalışılır).
    /// </summary>
    public IReadOnlyList<(DateTime Start, DateTime End)> Intervals(DateTime from, DateTime to)
    {
        var segments = new List<(DateTime Start, DateTime End)>();
        if (to <= from || IsEmpty)
        {
            return segments;
        }

        // Bir önceki günde başlayıp bu güne taşan vardiya da hesaba girer.
        for (var day = from.Date.AddDays(-1); day <= to.Date; day = day.AddDays(1))
        {
            if (!_workingDays.Contains(day.DayOfWeek))
            {
                continue;
            }

            foreach (var period in _periods)
            {
                var start = day + period.Start.ToTimeSpan();
                var cursor = 0;
                foreach (var (offset, length) in period.Breaks.Where(b => b.LengthMinutes > 0).OrderBy(b => b.OffsetMinutes))
                {
                    Add(start.AddMinutes(cursor), start.AddMinutes(Math.Min(offset, period.LengthMinutes)));
                    cursor = Math.Max(cursor, Math.Min(offset + length, period.LengthMinutes));
                }

                Add(start.AddMinutes(cursor), start.AddMinutes(period.LengthMinutes));
            }
        }

        // Üst üste binen vardiyalar tek dilim sayılır (aynı süre iki kez sayılmaz).
        var merged = new List<(DateTime Start, DateTime End)>();
        foreach (var segment in segments.OrderBy(s => s.Start))
        {
            if (merged.Count > 0 && segment.Start <= merged[^1].End)
            {
                merged[^1] = (merged[^1].Start, segment.End > merged[^1].End ? segment.End : merged[^1].End);
            }
            else
            {
                merged.Add(segment);
            }
        }

        return merged;

        void Add(DateTime start, DateTime end)
        {
            start = start < from ? from : start;
            end = end > to ? to : end;
            if (end > start)
            {
                segments.Add((start, end));
            }
        }
    }

    /// <summary>Aralıktaki toplam çalışma süresi (dakika).</summary>
    public double WorkingMinutes(DateTime from, DateTime to) => Intervals(from, to).Sum(i => (i.End - i.Start).TotalMinutes);

    /// <summary>Verilen andan itibaren çalışılan ilk an (an zaten çalışma süresindeyse kendisi).</summary>
    public DateTime? NextWorkingInstant(DateTime from, int maxDays = 400)
    {
        for (var offset = 0; offset < maxDays; offset += 7)
        {
            var intervals = Intervals(from.AddDays(offset), from.AddDays(offset + 7));
            if (intervals.Count > 0)
            {
                return intervals[0].Start;
            }
        }

        return null;
    }

    /// <summary>
    /// Başlangıçtan itibaren verilen çalışma süresi dolduğunda varılan an. Takvimde yeterli çalışma süresi
    /// yoksa (ya da <paramref name="maxDays"/> içinde dolmuyorsa) null.
    /// </summary>
    public DateTime? AddWorkingMinutes(DateTime start, double minutes, int maxDays = 400)
    {
        if (minutes <= 0)
        {
            return start;
        }

        var remaining = minutes;
        for (var offset = 0; offset < maxDays; offset += 7)
        {
            foreach (var (from, to) in Intervals(start.AddDays(offset), start.AddDays(offset + 7)))
            {
                var length = (to - from).TotalMinutes;
                if (length >= remaining)
                {
                    return from.AddMinutes(remaining);
                }

                remaining -= length;
            }
        }

        return null;
    }
}
