using ERP.Konfeksiyon.Domain.Enums;

namespace ERP.Konfeksiyon.Application.Features.ProductionTracking;

/// <summary>Bir çalışma oturumu (bitişi yoksa "şu an").</summary>
public sealed record SessionFact(long Id, int OperatorId, int MachineId, DateTime Start, DateTime End);

/// <summary>Oturum içindeki bir duraklama.</summary>
public sealed record PauseFact(long SessionId, WorkSessionPauseType Type, DateTime Start, DateTime End);

/// <summary>Bir demet operasyonunun fiilen işlemde olduğu aralık (başlat → bildir).</summary>
public sealed record WorkFact(long? SessionId, int OperatorId, int MachineId, int OperationId, DateTime Start, DateTime End);

/// <summary>Bir adet bildirimi. <paramref name="Sam"/> o demet operasyonunun standart süresidir (dk / adet).</summary>
public sealed record OutputFact(
    long? SessionId, int OperatorId, int MachineId, int OperationId, DateTime At, int Produced, int Rejected, decimal Sam);

/// <summary>Hesaplanan performans; süreler dakikadır.</summary>
public sealed record PerformanceFigures(
    double PresenceMinutes,
    double BreakMinutes,
    double PauseMinutes,
    double BreakdownMinutes,
    double WaitingMinutes,
    double NetWorkingMinutes,
    double ActiveMinutes,
    int Produced,
    int Rejected,
    double EarnedMinutes,
    int? TargetQuantity,
    double? EfficiencyPercent,
    double? UtilizationPercent,
    double? RejectRatePercent,
    double? ProducedPerHour,
    double? TargetPerHour);

/// <summary>
/// Performans hesabı. Yalnızca üretilen adede bakılmaz:
///
///   net çalışma     = mevcudiyet − planlı mola − ara − makine arızası
///   kullanılabilir  = net çalışma − demet bekleme      (operatörün elinde olmayan bekleme aleyhine sayılmaz)
///   kazanılan       = Σ üretilen adet × SAM
///   verim           = kazanılan ÷ kullanılabilir
///   hedef adet      = %100 verimde aynı sürede üretilmesi gereken adet
///   kullanım        = aktif üretim ÷ net çalışma
///
/// Operasyon boyutunda oturum süresi yoktur (oturum makineye aittir): zaman tabanı, operasyonun fiilen
/// işlemde olduğu süredir; böylece operasyonlar darboğaz analizi için kendi aralarında karşılaştırılabilir.
/// </summary>
public static class PerformanceCalculator
{
    /// <summary>Verilen anahtara göre gruplayarak hesaplar. Anahtarı null olan kayıtlar atlanır.</summary>
    public static IReadOnlyDictionary<int, PerformanceFigures> Calculate(
        DateTime from,
        DateTime to,
        IReadOnlyCollection<SessionFact> sessions,
        IReadOnlyCollection<PauseFact> pauses,
        IReadOnlyCollection<WorkFact> work,
        IReadOnlyCollection<OutputFact> outputs,
        Func<SessionFact, int?> sessionKey,
        Func<WorkFact, int?> workKey,
        Func<OutputFact, int?> outputKey,
        bool timeBaseIsActiveWork = false)
    {
        var pausesBySession = pauses.ToLookup(p => p.SessionId);
        var keys = new HashSet<int>();

        var sessionsByKey = Group(sessions, sessionKey, keys);
        var workByKey = Group(work, workKey, keys);
        var outputsByKey = Group(outputs.Where(o => o.At >= from && o.At < to), outputKey, keys);

        return keys.ToDictionary(key => key, key =>
        {
            var ownSessions = sessionsByKey.GetValueOrDefault(key) ?? [];
            var ownPauses = ownSessions.SelectMany(s => pausesBySession[s.Id]).ToList();

            double PauseMinutes(WorkSessionPauseType type) =>
                ownPauses.Where(p => p.Type == type).Sum(p => Overlap(p.Start, p.End, from, to));

            var presence = ownSessions.Sum(s => Overlap(s.Start, s.End, from, to));
            var planned = PauseMinutes(WorkSessionPauseType.Break);
            var manual = PauseMinutes(WorkSessionPauseType.Pause);
            var breakdown = PauseMinutes(WorkSessionPauseType.MachineBreakdown);
            var waiting = PauseMinutes(WorkSessionPauseType.WaitingForBundle);

            // Aktif üretim: operasyon işlemdeyken geçen süre; o sırada verilen mola / ara / arıza düşülür.
            var active = (workByKey.GetValueOrDefault(key) ?? []).Sum(w =>
            {
                var start = Max(w.Start, from);
                var end = Min(w.End, to);
                if (end <= start)
                {
                    return 0;
                }

                var interrupted = w.SessionId is { } sessionId
                    ? pausesBySession[sessionId]
                        .Where(p => p.Type != WorkSessionPauseType.WaitingForBundle)
                        .Sum(p => Overlap(p.Start, p.End, start, end))
                    : 0;
                return Math.Max((end - start).TotalMinutes - interrupted, 0);
            });

            var net = timeBaseIsActiveWork ? active : Math.Max(presence - planned - manual - breakdown, 0);
            var available = timeBaseIsActiveWork ? active : Math.Max(net - waiting, 0);

            var ownOutputs = outputsByKey.GetValueOrDefault(key) ?? [];
            var produced = ownOutputs.Sum(o => o.Produced);
            var rejected = ownOutputs.Sum(o => o.Rejected);
            var earned = (double)ownOutputs.Sum(o => o.Produced * o.Sam);

            int? target = earned > 0 && available > 0 ? (int)Math.Round(produced * available / earned) : null;
            var hours = available / 60;

            return new PerformanceFigures(
                Round(presence),
                Round(planned),
                Round(manual),
                Round(breakdown),
                Round(waiting),
                Round(net),
                Round(active),
                produced,
                rejected,
                Round(earned),
                target,
                available > 0 ? Round(earned / available * 100) : null,
                net > 0 ? Round(Math.Min(active / net, 1) * 100) : null,
                produced + rejected > 0 ? Round(rejected * 100.0 / (produced + rejected)) : null,
                hours > 0 ? Round(produced / hours) : null,
                hours > 0 && target is not null ? Round(target.Value / hours) : null);
        });
    }

    /// <summary>İki aralığın kesişimi (dakika); kesişmiyorlarsa 0.</summary>
    public static double Overlap(DateTime start, DateTime end, DateTime from, DateTime to)
    {
        var overlapStart = Max(start, from);
        var overlapEnd = Min(end, to);
        return overlapEnd > overlapStart ? (overlapEnd - overlapStart).TotalMinutes : 0;
    }

    private static Dictionary<int, List<T>> Group<T>(IEnumerable<T> items, Func<T, int?> keyOf, HashSet<int> keys)
    {
        var groups = new Dictionary<int, List<T>>();
        foreach (var item in items)
        {
            if (keyOf(item) is not { } key)
            {
                continue;
            }

            keys.Add(key);
            if (!groups.TryGetValue(key, out var list))
            {
                groups[key] = list = [];
            }

            list.Add(item);
        }

        return groups;
    }

    private static DateTime Max(DateTime a, DateTime b) => a > b ? a : b;
    private static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;
    private static double Round(double value) => Math.Round(value, 1);
}
