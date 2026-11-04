using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Konfeksiyon.Application.Features.Bundles;
using ERP.Konfeksiyon.Application.Features.Shifts;
using ERP.Konfeksiyon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using static ERP.Konfeksiyon.Application.Features.Reports.ReportColumnType;

namespace ERP.Konfeksiyon.Application.Features.Reports;

/// <summary>Gün gün üretim: bildirilen adetler ve banttan çıkan (son operasyondan geçen) adet.</summary>
public sealed class DailyProductionReport(IKonfeksiyonDbContext db, IFactoryClock clock) : IReport
{
    public ReportDefinition Definition { get; } = new(
        "daily-production", "Günlük üretim",
        "Gün gün üretilen ve hatalı adet. \"Biten\", demetin son operasyon(lar)ından çıkan parça adedidir; her parça bir kez sayılır.",
        MaxRangeDays: 92, Group: ReportGroups.Production);

    public async Task<Result<ReportData>> RunAsync(ReportParameters parameters, CancellationToken cancellationToken)
    {
        var rows = new List<IReadOnlyList<object?>>();
        int totalFinished = 0, totalProduced = 0, totalRejected = 0;
        var finishedByDay = await FinishedByDayAsync(parameters, cancellationToken);

        // Günler fabrikanın yerel saatine göre ayrılır; her gün tek bir toplama sorgusudur.
        foreach (var (day, start, end) in ReportDays.Split(clock, parameters.From, parameters.To))
        {
            var totals = await db.BundleOperationEvents
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(e => e.OccurredAt >= start && e.OccurredAt < end
                    && (e.EventType == BundleOperationEventType.QuantityReported
                        || e.EventType == BundleOperationEventType.RejectionReported))
                .Select(e => new { e.ProducedQuantity, e.RejectedQuantity, e.BundleId, e.OperatorId, e.MachineId })
                .GroupBy(e => 1)
                .Select(g => new
                {
                    Produced = g.Sum(e => e.ProducedQuantity),
                    Rejected = g.Sum(e => e.RejectedQuantity),
                    Bundles = g.Select(e => e.BundleId).Distinct().Count(),
                    Operators = g.Select(e => e.OperatorId).Distinct().Count(),
                    Machines = g.Select(e => e.MachineId).Distinct().Count(),
                })
                .SingleOrDefaultAsync(cancellationToken);

            var produced = totals?.Produced ?? 0;
            var rejected = totals?.Rejected ?? 0;
            var finished = finishedByDay.GetValueOrDefault(day);
            totalFinished += finished;
            totalProduced += produced;
            totalRejected += rejected;

            rows.Add(
            [
                day, finished, produced, rejected, ReportMath.Rate(rejected, produced + rejected),
                totals?.Bundles ?? 0, totals?.Operators ?? 0, totals?.Machines ?? 0,
            ]);
        }

        return new ReportData(
            [
                new("day", "Tarih", Date),
                new("finished", "Biten (son operasyon)", Integer),
                new("produced", "Üretilen (tüm operasyonlar)", Integer),
                new("rejected", "Hatalı", Integer),
                new("rejectRate", "Hata oranı", Percent),
                new("bundles", "Demet", Integer),
                new("operators", "Operatör", Integer),
                new("machines", "Makine", Integer),
            ],
            rows,
            ["Toplam", totalFinished, totalProduced, totalRejected, ReportMath.Rate(totalRejected, totalProduced + totalRejected), null, null, null]);
    }

    /// <summary>
    /// Gün gün demetten çıkan (bitmiş) parça adedi. Son adımlar demetin kendi sıra moduna göredir
    /// (<see cref="Domain.Entities.Bundle.FinishedQuantity"/> ile aynı kural): bitmiş adet, son adım(lar)a bildirilen
    /// birikimli adetlerin en küçüğüdür; bir güne, o gün bu en küçük adette oluşan artış yazılır. Böylece sırasızda
    /// (bütün adımlar son adımdır) ya da birden çok son adımda her parça yalnızca bir kez sayılır.
    /// </summary>
    private async Task<Dictionary<DateOnly, int>> FinishedByDayAsync(
        ReportParameters parameters, CancellationToken cancellationToken)
    {
        var reports = db.BundleOperationEvents
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(e => e.EventType == BundleOperationEventType.QuantityReported && e.ProducedQuantity > 0);

        // Aralıkta adet bildirilen demetler; birikimli adet için bu demetlerin aralık öncesi bildirimleri de okunur.
        var bundleIds = reports
            .Where(e => e.OccurredAt >= parameters.From && e.OccurredAt < parameters.To)
            .Select(e => e.BundleId)
            .Distinct();

        var finalSteps = await db.LoadFinalStepsAsync(bundleIds, cancellationToken);
        var history = await reports
            .Where(e => bundleIds.Contains(e.BundleId) && e.OccurredAt < parameters.To)
            .Select(e => new { e.Id, e.BundleId, e.BundleOperation.RouteOperationId, e.ProducedQuantity, e.OccurredAt })
            .ToListAsync(cancellationToken);

        var result = new Dictionary<DateOnly, int>();
        foreach (var bundle in history.GroupBy(e => e.BundleId))
        {
            if (!finalSteps.TryGetValue(bundle.Key, out var final) || final.Count == 0)
            {
                continue;
            }

            var cumulative = final.ToDictionary(id => id, _ => 0);
            var finished = 0;
            foreach (var report in bundle.OrderBy(e => e.OccurredAt).ThenBy(e => e.Id))
            {
                if (!cumulative.ContainsKey(report.RouteOperationId))
                {
                    continue;
                }

                cumulative[report.RouteOperationId] += report.ProducedQuantity;
                var now = cumulative.Values.Min();
                if (now > finished && report.OccurredAt >= parameters.From)
                {
                    var day = DateOnly.FromDateTime(clock.ToLocal(report.OccurredAt));
                    result[day] = result.GetValueOrDefault(day) + now - finished;
                }

                finished = now;
            }
        }

        return result;
    }
}

/// <summary>Üretim olayları: hangi demet, ne zaman, hangi makinede, kim tarafından işlendi.</summary>
public sealed class BundleMovementsReport(IKonfeksiyonDbContext db) : IReport
{
    private static readonly Dictionary<BundleOperationEventType, string> EventLabels = new()
    {
        [BundleOperationEventType.OperationStarted] = "Başladı",
        [BundleOperationEventType.OperationResumed] = "Devam etti",
        [BundleOperationEventType.OperationPaused] = "Yarım bırakıldı",
        [BundleOperationEventType.OperationCompleted] = "Tamamlandı",
        [BundleOperationEventType.QuantityReported] = "Adet bildirildi",
        [BundleOperationEventType.RejectionReported] = "Hatalı bildirildi",
    };

    public ReportDefinition Definition { get; } = new(
        "bundle-movements", "Demet hareketleri",
        "Demetlerin üretim olayları, eskiden yeniye. En fazla 5.000 satır; daraltmak için demet ya da sipariş no ile arayın.",
        SearchLabel: "Demet no veya sipariş no", Group: ReportGroups.Production);

    public async Task<Result<ReportData>> RunAsync(ReportParameters parameters, CancellationToken cancellationToken)
    {
        // Geçmiş, sonradan silinen makine / operatör / operasyon için de eksiksiz görünür.
        var events = db.BundleOperationEvents
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(e => e.OccurredAt >= parameters.From && e.OccurredAt < parameters.To);

        if (parameters.Search is { } search)
        {
            events = events.Where(e => e.Bundle.BundleNo.Contains(search) || e.Bundle.ProductionOrder.OrderNo.Contains(search));
        }

        var items = await events
            .OrderBy(e => e.Id)
            .Take(ReportLimits.MaxRows + 1)
            .Select(e => new
            {
                e.OccurredAt,
                e.Bundle.BundleNo,
                e.Bundle.ProductionOrder.OrderNo,
                e.BundleOperation.SequenceNo,
                OperationName = e.BundleOperation.Operation.Name,
                e.EventType,
                e.Machine.MachineCode,
                Operator = e.Operator == null ? Production.ProductionService.NoOperatorName : e.Operator.FirstName + " " + e.Operator.LastName,
                e.ProducedQuantity,
                e.RejectedQuantity,
            })
            .ToListAsync(cancellationToken);

        return new ReportData(
            [
                new("occurredAt", "Zaman", ReportColumnType.DateTime),
                new("bundleNo", "Demet"),
                new("orderNo", "İş emri"),
                new("sequenceNo", "Sıra", Integer),
                new("operation", "Operasyon"),
                new("event", "Olay"),
                new("machine", "Makine"),
                new("operator", "Operatör"),
                new("produced", "Üretilen", Integer),
                new("rejected", "Hatalı", Integer),
            ],
            items
                .Take(ReportLimits.MaxRows)
                .Select(e => (IReadOnlyList<object?>)
                [
                    e.OccurredAt, e.BundleNo, e.OrderNo, e.SequenceNo, e.OperationName, EventLabels[e.EventType], e.MachineCode,
                    e.Operator, e.ProducedQuantity, e.RejectedQuantity,
                ])
                .ToList(),
            Truncated: items.Count > ReportLimits.MaxRows);
    }
}

/// <summary>Makine duruşları: arıza, bakım ve boşta geçen süreler (makine durum kayıtlarından).</summary>
public sealed class MachineDowntimeReport(IKonfeksiyonDbContext db, IFactoryClock clock) : IReport
{
    public ReportDefinition Definition { get; } = new(
        "machine-downtime", "Makine duruşları",
        "Makinelerin arızada, bakımda, kapalı ve boşta geçirdiği süreler. En uzun süre arızada kalan makine üsttedir.",
        MaxRangeDays: 92, Group: ReportGroups.Machine);

    public async Task<Result<ReportData>> RunAsync(ReportParameters parameters, CancellationToken cancellationToken)
    {
        var (from, to) = (parameters.From, parameters.To);
        // Süren durum "şu an"a kadar sayılır; gelecek sayılmaz.
        var until = to < clock.UtcNow ? to : clock.UtcNow;

        var logs = await db.MachineStatusLogs
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(l => l.StartedAt < to && (l.EndedAt == null || l.EndedAt > from))
            .Select(l => new { l.MachineId, l.Machine.MachineCode, l.Machine.MachineName, l.Status, l.StartedAt, l.EndedAt })
            .ToListAsync(cancellationToken);

        var machines = logs
            .GroupBy(l => new { l.MachineId, l.MachineCode, l.MachineName })
            .Select(g =>
            {
                double Minutes(MachineStatus status) => g
                    .Where(l => l.Status == status)
                    .Sum(l => ReportMath.Overlap(l.StartedAt, l.EndedAt ?? until, from, until));

                return new
                {
                    g.Key.MachineCode,
                    g.Key.MachineName,
                    // Aralıktan önce başlamış arıza bu aralığın arızası sayılmaz, süresi sayılır.
                    Breakdowns = g.Count(l => l.Status == MachineStatus.Breakdown && l.StartedAt >= from),
                    Breakdown = Minutes(MachineStatus.Breakdown),
                    Maintenance = Minutes(MachineStatus.Maintenance),
                    Offline = Minutes(MachineStatus.Offline),
                    Idle = Minutes(MachineStatus.Idle) + Minutes(MachineStatus.Available),
                    Running = Minutes(MachineStatus.Running),
                };
            })
            .OrderByDescending(m => m.Breakdown)
            .ThenByDescending(m => m.Maintenance)
            .ThenBy(m => m.MachineCode, StringComparer.Ordinal)
            .ToList();

        return new ReportData(
            [
                new("machineCode", "Makine"),
                new("machineName", "Ad"),
                new("breakdowns", "Arıza sayısı", Integer),
                new("breakdown", "Arıza", Minutes),
                new("maintenance", "Bakım", Minutes),
                new("offline", "Kapalı", Minutes),
                new("idle", "Boşta", Minutes),
                new("running", "Çalışma", Minutes),
            ],
            machines
                .Select(m => (IReadOnlyList<object?>)
                [
                    m.MachineCode, m.MachineName, m.Breakdowns, ReportMath.Round(m.Breakdown), ReportMath.Round(m.Maintenance),
                    ReportMath.Round(m.Offline), ReportMath.Round(m.Idle), ReportMath.Round(m.Running),
                ])
                .ToList(),
            [
                "Toplam", null, machines.Sum(m => m.Breakdowns), ReportMath.Round(machines.Sum(m => m.Breakdown)),
                ReportMath.Round(machines.Sum(m => m.Maintenance)), ReportMath.Round(machines.Sum(m => m.Offline)),
                ReportMath.Round(machines.Sum(m => m.Idle)), ReportMath.Round(machines.Sum(m => m.Running)),
            ]);
    }
}

public static class ReportMath
{
    /// <summary>Parçanın bütüne yüzdesi (bir ondalık); bütün sıfırsa null.</summary>
    public static double? Rate(double part, double whole) => whole > 0 ? Math.Round(part * 100.0 / whole, 1) : null;

    public static double Round(double value) => Math.Round(value, 1);

    /// <summary>İki aralığın kesişimi (dakika); kesişmiyorlarsa 0.</summary>
    public static double Overlap(DateTime start, DateTime end, DateTime from, DateTime to)
    {
        var overlapStart = start > from ? start : from;
        var overlapEnd = end < to ? end : to;
        return overlapEnd > overlapStart ? (overlapEnd - overlapStart).TotalMinutes : 0;
    }
}
