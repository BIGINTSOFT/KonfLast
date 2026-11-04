using ERP.Core.Application.Common;
using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Konfeksiyon.Application.Features.Production;
using ERP.Konfeksiyon.Application.Features.Shifts;
using ERP.Konfeksiyon.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERP.Konfeksiyon.Application.Features.ProductionTracking;

/// <summary>Tablet performans sayfasının dönemi (fabrika saatiyle).</summary>
public enum TabletPerformancePeriod
{
    /// <summary>Bugün gece yarısından şu ana.</summary>
    Today,

    /// <summary>Bu haftanın pazartesisinden şu ana.</summary>
    Week,
}

/// <summary>Kıyas için aynı hesap başka bir dönemde (ör. dün, son 7 gün, geçen hafta).</summary>
public sealed record TabletPerformanceComparisonDto(string Label, double? EfficiencyPercent, double? QualityPercent);

/// <summary>Dönemin bir dilimi (bugün için saat, hafta için gün) ve o dilimde bildirilen adet.</summary>
/// <param name="Label">Ekranda yazacak kısa ad (ör. "09", "Pzt").</param>
/// <param name="Current">Şu an içinde bulunulan dilim mi.</param>
public sealed record TabletPerformanceBucketDto(string Label, int Produced, int Rejected, bool Current);

/// <summary>
/// Tablette oturumu açık operatörün performansı. Hesap web'deki performans raporuyla aynıdır
/// (<see cref="PerformanceCalculator"/>).
/// </summary>
/// <param name="EfficiencyPercent">Kazanılan dakika ÷ çalışılabilir dakika; %100'ü aşabilir.</param>
/// <param name="QualityPercent">Sağlam adet ÷ (sağlam + hatalı).</param>
/// <param name="TargetQuantity">%100 verimde aynı sürede üretilmesi gereken adet.</param>
/// <param name="EarnedMinutes">Üretilen adet × birim süre.</param>
/// <param name="AvailableMinutes">Net çalışma − demet bekleme (verimin paydası).</param>
/// <param name="TargetPerHour">%100 verimde saatte üretilmesi gereken adet (grafikteki hedef çizgisi).</param>
public sealed record TabletPerformanceDto(
    int OperatorId,
    string FullName,
    TabletPerformancePeriod Period,
    DateTime From,
    DateTime To,
    double? EfficiencyPercent,
    double? QualityPercent,
    int Produced,
    int Rejected,
    int? TargetQuantity,
    double? ProducedPerHour,
    double? TargetPerHour,
    double NetWorkingMinutes,
    double WaitingMinutes,
    double EarnedMinutes,
    double AvailableMinutes,
    IReadOnlyList<TabletPerformanceComparisonDto> Comparisons,
    IReadOnlyList<TabletPerformanceBucketDto> Buckets);

public interface ITabletPerformanceService
{
    /// <summary>Makinede oturumu açık operatörün seçilen dönemdeki performansı; oturum yoksa <c>null</c>.</summary>
    Task<Result<TabletPerformanceDto?>> GetForStationAsync(TabletPerformancePeriod period, CancellationToken cancellationToken);
}

public sealed class TabletPerformanceService(
    IKonfeksiyonDbContext db, IStationResolver stations, IPerformanceService performance, IFactoryClock clock)
    : ITabletPerformanceService
{
    private static readonly string[] DayNames = ["Pzt", "Sal", "Çar", "Per", "Cum", "Cmt", "Paz"];

    public async Task<Result<TabletPerformanceDto?>> GetForStationAsync(
        TabletPerformancePeriod period, CancellationToken cancellationToken)
    {
        var resolved = await stations.ResolveAsync(cancellationToken);
        if (!resolved.IsSuccess)
        {
            return resolved.Error!;
        }

        var session = await db.WorkSessions
            .AsNoTracking()
            .Where(s => s.MachineId == resolved.Value.MachineId && s.LogoutAt == null)
            .Select(s => new { s.OperatorId, FullName = s.Operator.FirstName + " " + s.Operator.LastName })
            .SingleOrDefaultAsync(cancellationToken);
        if (session is null)
        {
            return Result<TabletPerformanceDto?>.Success(null);
        }

        var now = clock.UtcNow;
        var today = clock.ToLocal(now).Date;
        // Pazartesi haftanın ilk günü.
        var monday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        var fromLocal = period == TabletPerformancePeriod.Today ? today : monday;
        var from = clock.ToUtc(fromLocal);

        var current = await RowAsync(session.OperatorId, from, now, cancellationToken);
        if (!current.IsSuccess)
        {
            return current.Error!;
        }

        // Kıyas: bugün için dün ve önceki 7 gün; hafta için geçen hafta.
        var comparisonRanges = period == TabletPerformancePeriod.Today
            ? new[] { ("Dün", today.AddDays(-1), today), ("Son 7 gün", today.AddDays(-7), today) }
            : new[] { ("Geçen hafta", monday.AddDays(-7), monday) };
        var comparisons = new List<TabletPerformanceComparisonDto>();
        foreach (var (label, start, end) in comparisonRanges)
        {
            var row = await RowAsync(session.OperatorId, clock.ToUtc(start), clock.ToUtc(end), cancellationToken);
            if (!row.IsSuccess)
            {
                return row.Error!;
            }

            // O dönemde bildirim yoksa kıyas yoktur ("%0" yanıltır).
            var reported = row.Value is { } r && r.Produced + r.Rejected > 0;
            comparisons.Add(new TabletPerformanceComparisonDto(
                label, reported ? row.Value!.EfficiencyPercent : null, reported ? Quality(row.Value) : null));
        }

        var buckets = await BucketsAsync(session.OperatorId, period, fromLocal, from, now, cancellationToken);
        var figures = current.Value;
        return Result<TabletPerformanceDto?>.Success(new TabletPerformanceDto(
            session.OperatorId,
            session.FullName,
            period,
            from,
            now,
            figures?.EfficiencyPercent,
            Quality(figures),
            figures?.Produced ?? 0,
            figures?.Rejected ?? 0,
            figures?.TargetQuantity,
            figures?.ProducedPerHour,
            figures?.TargetPerHour,
            figures?.NetWorkingMinutes ?? 0,
            figures?.WaitingMinutes ?? 0,
            figures?.EarnedMinutes ?? 0,
            figures is null ? 0 : Math.Max(0, figures.NetWorkingMinutes - figures.WaitingMinutes),
            comparisons,
            buckets));
    }

    /// <summary>Operatörün aralıktaki performans satırı; o aralıkta kaydı yoksa <c>null</c>.</summary>
    private async Task<Result<PerformanceRowDto?>> RowAsync(
        int operatorId, DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        var report = await performance.GetAsync(
            new PerformanceQuery { From = from, To = to, GroupBy = PerformanceGroupBy.Operator }, cancellationToken);
        return report.IsSuccess
            ? Result<PerformanceRowDto?>.Success(report.Value.Rows.FirstOrDefault(r => r.Id == operatorId))
            : report.Error!;
    }

    private static double? Quality(PerformanceRowDto? row)
    {
        var total = (row?.Produced ?? 0) + (row?.Rejected ?? 0);
        return total > 0 ? Math.Round(100.0 * row!.Produced / total, 1) : null;
    }

    /// <summary>Bugün için saat saat (ilk bildirimden şu ana), hafta için gün gün bildirilen adet.</summary>
    private async Task<IReadOnlyList<TabletPerformanceBucketDto>> BucketsAsync(
        int operatorId, TabletPerformancePeriod period, DateTime fromLocal, DateTime from, DateTime now,
        CancellationToken cancellationToken)
    {
        var events = await db.BundleOperationEvents
            .AsNoTracking()
            .Where(e => e.OperatorId == operatorId && e.CountsForPerformance && e.OccurredAt >= from && e.OccurredAt < now
                && (e.EventType == BundleOperationEventType.QuantityReported
                    || e.EventType == BundleOperationEventType.RejectionReported))
            .Select(e => new { e.OccurredAt, e.ProducedQuantity, e.RejectedQuantity })
            .ToListAsync(cancellationToken);

        var nowLocal = clock.ToLocal(now);
        if (period == TabletPerformancePeriod.Week)
        {
            return Enumerable.Range(0, 7)
                .Select(day =>
                {
                    var date = fromLocal.AddDays(day);
                    var ofDay = events.Where(e => clock.ToLocal(e.OccurredAt).Date == date).ToList();
                    return new TabletPerformanceBucketDto(
                        DayNames[day], ofDay.Sum(e => e.ProducedQuantity), ofDay.Sum(e => e.RejectedQuantity), date == nowLocal.Date);
                })
                .ToList();
        }

        // Gün, ilk bildirimin saatinden (yoksa şimdiki saatten) başlar: gece saatleri boş sütun olarak çizilmez.
        var hours = events.Select(e => clock.ToLocal(e.OccurredAt).Hour).ToList();
        var firstHour = hours.Count > 0 ? Math.Min(hours.Min(), nowLocal.Hour) : nowLocal.Hour;
        return Enumerable.Range(firstHour, nowLocal.Hour - firstHour + 1)
            .Select(hour =>
            {
                var ofHour = events.Where(e => clock.ToLocal(e.OccurredAt).Hour == hour).ToList();
                return new TabletPerformanceBucketDto(
                    hour.ToString("00"), ofHour.Sum(e => e.ProducedQuantity), ofHour.Sum(e => e.RejectedQuantity), hour == nowLocal.Hour);
            })
            .ToList();
    }
}
