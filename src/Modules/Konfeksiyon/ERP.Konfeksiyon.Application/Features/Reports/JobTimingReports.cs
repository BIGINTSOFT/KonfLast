using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Konfeksiyon.Application.Features.Shifts;
using ERP.Konfeksiyon.Application.Features.WorkSessions;
using ERP.Konfeksiyon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using static ERP.Konfeksiyon.Application.Features.Reports.ReportColumnType;

namespace ERP.Konfeksiyon.Application.Features.Reports;

/// <summary>
/// İş süreleri: tabletten bildirilen her işin (başlat → bildir) hedef ve gerçekleşen süresi. Verimi olağan dışı
/// yüksek işler işaretlenir: operatör işi bitirmeden "Tamamla" demiş olabilir.
/// </summary>
public sealed class JobDurationsReport(IKonfeksiyonDbContext db, IOptions<ProductionOptions> options) : IReport
{
    private readonly int _suspiciousPercent = options.Value.SuspiciousEfficiencyPercent;

    public ReportDefinition Definition { get; } = new(
        "job-durations", "İş süreleri ve verim",
        "Tabletten bildirilen her işin hedef ve gerçekleşen süresi (mola, ara ve arıza düşülmüş). Verimi olağan dışı " +
        "yüksek işler 'Olağan dışı hızlı' olarak işaretlenir: süre erken kesilmiş olabilir. Resmi performans ölçüsü " +
        "vardiya verimidir (Performans raporları).",
        SearchLabel: "Demet no veya sipariş no", Group: ReportGroups.Personnel,
        Permission: ERP.Konfeksiyon.Domain.Authorization.KonfeksiyonPermissions.Report.Personnel);

    public async Task<Result<ReportData>> RunAsync(ReportParameters parameters, CancellationToken cancellationToken)
    {
        // Her bildirim tek bir kapanış olayıyla biter (tamamlandı ya da yarım bırakıldı); süre o olaya yazılır.
        var events = db.BundleOperationEvents
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(e => e.OccurredAt >= parameters.From && e.OccurredAt < parameters.To
                && e.RunSeconds != null
                && (e.EventType == BundleOperationEventType.OperationCompleted
                    || e.EventType == BundleOperationEventType.OperationPaused));

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
                e.Machine.MachineCode,
                Operator = e.Operator.FirstName + " " + e.Operator.LastName,
                e.BundleOperation.Sam,
                e.RunSeconds,
                e.EfficiencyPercent,
                // Aynı bildirimin adet olayları.
                Produced = db.BundleOperationEvents
                    .Where(x => x.RequestId == e.RequestId && x.BundleOperationId == e.BundleOperationId)
                    .Sum(x => x.ProducedQuantity),
                Rejected = db.BundleOperationEvents
                    .Where(x => x.RequestId == e.RequestId && x.BundleOperationId == e.BundleOperationId)
                    .Sum(x => x.RejectedQuantity),
            })
            .ToListAsync(cancellationToken);

        return new ReportData(
            [
                new("occurredAt", "Bildirim zamanı", ReportColumnType.DateTime),
                new("bundleNo", "Demet"),
                new("orderNo", "İş emri"),
                new("sequenceNo", "Sıra", Integer),
                new("operation", "Operasyon"),
                new("machine", "Makine"),
                new("operator", "Operatör"),
                new("produced", "Üretilen", Integer),
                new("rejected", "Hatalı", Integer),
                new("targetMinutes", "Hedef süre", Minutes),
                new("actualMinutes", "Gerçekleşen süre", Minutes),
                new("efficiency", "Verim", Percent),
                new("warning", "Uyarı"),
            ],
            items
                .Take(ReportLimits.MaxRows)
                .Select(e => (IReadOnlyList<object?>)
                [
                    e.OccurredAt, e.BundleNo, e.OrderNo, e.SequenceNo, e.OperationName, e.MachineCode, e.Operator,
                    e.Produced, e.Rejected,
                    ReportMath.Round((double)(e.Produced * e.Sam)),
                    ReportMath.Round(e.RunSeconds!.Value / 60.0),
                    e.EfficiencyPercent,
                    e.EfficiencyPercent >= _suspiciousPercent ? "Olağan dışı hızlı" : null,
                ])
                .ToList(),
            Truncated: items.Count > ReportLimits.MaxRows);
    }
}

/// <summary>
/// Operasyonlar arası bekleme: bir demetin önceki adımı tamamlandıktan sonra sıradaki adımın başlamasına kadar
/// geçen süre. Hangi makinenin önünde iş biriktiğini ve "tamamlandı" denip teslim edilmeyen demetleri gösterir.
/// </summary>
public sealed class HandoverWaitsReport(IKonfeksiyonDbContext db, IFactoryClock clock, IOptions<ProductionOptions> options) : IReport
{
    private readonly int _warningMinutes = options.Value.HandoverWarningMinutes;

    public ReportDefinition Definition { get; } = new(
        "handover-waits", "Operasyonlar arası bekleme",
        "Önceki operasyonun tamamlanmasından sıradaki operasyonun başlamasına kadar geçen süre; henüz başlamamış " +
        "olanlar şu ana kadar sayılır. En uzun bekleyen üsttedir. Uzun beklemeler 'Geç devir' olarak işaretlenir.",
        SearchLabel: "Demet no veya sipariş no", Group: ReportGroups.Production);

    public async Task<Result<ReportData>> RunAsync(ReportParameters parameters, CancellationToken cancellationToken)
    {
        // Aralıkta bir adımı tamamlanan demetler.
        var bundles = db.Bundles
            .AsNoTracking()
            .Where(b => b.Operations.Any(o => o.CompletedAt >= parameters.From && o.CompletedAt < parameters.To));
        if (parameters.Search is { } search)
        {
            bundles = bundles.Where(b => b.BundleNo.Contains(search) || b.ProductionOrder.OrderNo.Contains(search));
        }

        var operations = await bundles
            .SelectMany(b => b.Operations.Select(o => new
            {
                o.BundleId,
                b.BundleNo,
                b.ProductionOrder.OrderNo,
                o.RouteOperationId,
                o.SequenceNo,
                OperationName = o.Operation.Name,
                o.Status,
                o.StartedAt,
                o.CompletedAt,
                Operator = o.Operator == null ? null : o.Operator.FirstName + " " + o.Operator.LastName,
            }))
            .ToListAsync(cancellationToken);

        var routeOperationIds = operations.Select(o => o.RouteOperationId).Distinct().ToList();
        var predecessors = (await db.OperationDependencies
                .AsNoTracking()
                .Where(d => routeOperationIds.Contains(d.RouteOperationId))
                .Select(d => new { d.RouteOperationId, d.PredecessorId })
                .ToListAsync(cancellationToken))
            .ToLookup(d => d.RouteOperationId, d => d.PredecessorId);

        var now = clock.UtcNow;
        var rows = operations
            .GroupBy(o => o.BundleId)
            .SelectMany(bundle => bundle
                .Where(step => step.Status != BundleOperationStatus.Cancelled && predecessors[step.RouteOperationId].Any())
                .Select(step =>
                {
                    var before = bundle.Where(o => predecessors[step.RouteOperationId].Contains(o.RouteOperationId)).ToList();
                    // Adım, öncüllerinin hepsi bitince başlatılabilir hale gelir.
                    if (before.Count == 0 || before.Any(o => o.CompletedAt is null))
                    {
                        return null;
                    }

                    var last = before.OrderByDescending(o => o.CompletedAt).First();
                    var readyAt = last.CompletedAt!.Value;
                    if (readyAt < parameters.From || readyAt >= parameters.To)
                    {
                        return null;
                    }

                    // Tamir nedeniyle yeniden açılan adımda başlama, hazır olmadan önce kalabilir: bekleme sıfır sayılır.
                    var until = step.StartedAt ?? now;
                    var wait = Math.Max((until - readyAt).TotalMinutes, 0);
                    return new
                    {
                        step.BundleNo,
                        step.OrderNo,
                        Previous = $"{last.SequenceNo} · {last.OperationName}",
                        PreviousOperator = last.Operator,
                        ReadyAt = readyAt,
                        Next = $"{step.SequenceNo} · {step.OperationName}",
                        step.StartedAt,
                        Wait = wait,
                        State = step.StartedAt is null ? "Bekliyor" : "Başladı",
                        Warning = wait >= _warningMinutes ? "Geç devir" : null,
                    };
                }))
            .Where(row => row is not null)
            .Select(row => row!)
            .OrderByDescending(row => row.Wait)
            .ToList();

        return new ReportData(
            [
                new("bundleNo", "Demet"),
                new("orderNo", "İş emri"),
                new("previous", "Önceki operasyon"),
                new("previousOperator", "Tamamlayan"),
                new("readyAt", "Tamamlanma", ReportColumnType.DateTime),
                new("next", "Sıradaki operasyon"),
                new("startedAt", "Başlama", ReportColumnType.DateTime),
                new("waitMinutes", "Bekleme", Minutes),
                new("state", "Durum"),
                new("warning", "Uyarı"),
            ],
            rows
                .Take(ReportLimits.MaxRows)
                .Select(r => (IReadOnlyList<object?>)
                [
                    r.BundleNo, r.OrderNo, r.Previous, r.PreviousOperator, r.ReadyAt, r.Next, r.StartedAt,
                    ReportMath.Round(r.Wait), r.State, r.Warning,
                ])
                .ToList(),
            Truncated: rows.Count > ReportLimits.MaxRows);
    }
}
