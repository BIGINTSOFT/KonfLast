using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Konfeksiyon.Application.Features.Shifts;
using ERP.Konfeksiyon.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERP.Konfeksiyon.Application.Features.ProductionTracking;

/// <summary>Fabrikanın şu anki özeti. Üretim / hedef / verim fabrikanın bugünkü (yerel) günü içindir.</summary>
/// <param name="OperatorsPresent">Açık oturumu olan operatör sayısı.</param>
/// <param name="TargetQuantity">%100 verimde bugün şu ana kadar üretilmesi gereken adet.</param>
/// <param name="PendingReworkTasks">Tamir ya da tekrar kontrol bekleyen tamir işleri.</param>
public sealed record LiveSummaryDto(
    int Machines,
    int RunningMachines,
    int BreakdownMachines,
    int OperatorsPresent,
    int Producing,
    int WaitingForBundle,
    int OnBreak,
    int Produced,
    int Rejected,
    int? TargetQuantity,
    double? EfficiencyPercent,
    int PendingReworkTasks);

/// <summary>Bantın şu an yürüyen planı.</summary>
/// <param name="ProducedQuantity">
/// Bu siparişten, bantın makinelerinde rotanın son operasyonundan geçmiş sağlam adet (banttan çıkan).
/// </param>
public sealed record LiveLinePlanDto(
    int PlanId,
    int ProductionOrderId,
    string OrderNo,
    string ModelCode,
    int PlannedQuantity,
    int ProducedQuantity,
    decimal CompletionPercent,
    DateTime PlannedEnd);

/// <summary>Bir bantın canlı görünümü. Adet ve süreler bugüne aittir; süreler dakikadır.</summary>
/// <param name="LineId">Banta atanmamış makineler grubunda null.</param>
/// <param name="Produced">Bugün bantın makinelerinde bildirilen sağlam adet (tüm operasyonlar).</param>
public sealed record LiveLineDto(
    int? LineId,
    string LineCode,
    string Name,
    LiveLinePlanDto? Plan,
    int Produced,
    int Rejected,
    int? TargetQuantity,
    double? EfficiencyPercent,
    double? ProducedPerHour,
    double? TargetPerHour,
    double BreakdownMinutes,
    double WaitingMinutes,
    double BreakMinutes,
    IReadOnlyList<MachineActivityDto> Machines);

/// <param name="DayStart">Bugünkü rakamların başladığı an (fabrikanın yerel gece yarısı, UTC).</param>
/// <param name="BundleStatuses">Sahadaki (bitmemiş) demetlerin duruma göre sayısı.</param>
public sealed record LiveDashboardDto(
    DateTime GeneratedAt,
    DateTime DayStart,
    LiveSummaryDto Summary,
    IReadOnlyList<LiveLineDto> Lines,
    IReadOnlyList<BundleStatusCountDto> BundleStatuses,
    IReadOnlyList<WipOperationDto> Wip);

public interface ILiveDashboardService
{
    /// <summary>Canlı pano: bantlar, makineler, bugünkü üretim / hedef, demet durumları ve ara stok.</summary>
    Task<LiveDashboardDto> GetAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Canlı panonun tek çağrılık verisi. Yeni bir hesap içermez: makine durumu üretim takibinden, hedef ve
/// verim performans hesabından gelir; böylece pano ile raporlar aynı rakamı gösterir.
/// </summary>
public sealed class LiveDashboardService(
    IKonfeksiyonDbContext db,
    IProductionTrackingService tracking,
    IPerformanceService performance,
    IFactoryClock clock) : ILiveDashboardService
{
    private const string NoLineCode = "—";
    private const string NoLineName = "Banta atanmamış makineler";

    /// <summary>Performans raporunda banta atanmamış makinelerin satır kimliği.</summary>
    private const int NoLineRowId = 0;

    /// <summary>Sahadaki demetler: kartı atanmış ve henüz bitmemiş.</summary>
    private static readonly BundleStatus[] OnFloor =
    [
        BundleStatus.Waiting, BundleStatus.InProduction, BundleStatus.PartiallyCompleted, BundleStatus.QualityHold,
        BundleStatus.Rework,
    ];

    public async Task<LiveDashboardDto> GetAsync(CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var dayStart = clock.ToUtc(clock.ToLocal(now).Date);

        var machines = await tracking.GetMachinesAsync(cancellationToken);
        var wip = await tracking.GetWipAsync(new WipQuery(), cancellationToken);
        var today = (await performance.GetAsync(
            new PerformanceQuery { From = dayStart, To = dayStart.AddDays(1), GroupBy = PerformanceGroupBy.Line },
            cancellationToken)).Value;
        var todayByLine = today.Rows.ToDictionary(r => r.Id);

        var lines = await db.ProductionLines
            .AsNoTracking()
            .Where(l => l.IsActive)
            .OrderBy(l => l.LineCode)
            .Select(l => new { l.Id, l.LineCode, l.Name })
            .ToListAsync(cancellationToken);
        var lineIds = lines.Select(l => l.Id).ToList();

        var lineOfMachine = await db.LineMachineAssignments
            .AsNoTracking()
            .Where(a => a.UnassignedAt == null && lineIds.Contains(a.ProductionLineId))
            .Select(a => new { a.MachineId, a.ProductionLineId, a.Position })
            .ToDictionaryAsync(a => a.MachineId, cancellationToken);

        var plans = await LoadCurrentPlansAsync(lineIds, now, lineOfMachine.ToDictionary(a => a.Key, a => a.Value.ProductionLineId), cancellationToken);

        var lineRows = lines
            .Select(line =>
            {
                var own = machines
                    .Where(m => lineOfMachine.TryGetValue(m.MachineId, out var a) && a.ProductionLineId == line.Id)
                    .OrderBy(m => lineOfMachine[m.MachineId].Position)
                    .ToList();
                return ToLine(line.Id, line.LineCode, line.Name, plans.GetValueOrDefault(line.Id), todayByLine.GetValueOrDefault(line.Id), own);
            })
            .ToList();

        var unassigned = machines.Where(m => !lineOfMachine.ContainsKey(m.MachineId)).ToList();
        if (unassigned.Count > 0)
        {
            lineRows.Add(ToLine(null, NoLineCode, NoLineName, null, todayByLine.GetValueOrDefault(NoLineRowId), unassigned));
        }

        var bundleStatuses = await db.Bundles
            .AsNoTracking()
            .Where(b => OnFloor.Contains(b.Status))
            .GroupBy(b => b.Status)
            .Select(g => new BundleStatusCountDto(g.Key, g.Count(), g.Sum(b => b.Quantity)))
            .ToListAsync(cancellationToken);

        var pendingRework = await db.ReworkTasks
            .CountAsync(t => t.Status == ReworkStatus.Open || t.Status == ReworkStatus.Done, cancellationToken);

        var summary = new LiveSummaryDto(
            machines.Count,
            machines.Count(m => m.Status == MachineStatus.Running),
            machines.Count(m => m.Status == MachineStatus.Breakdown),
            machines.Count(m => m.OperatorState != OperatorState.Offline),
            machines.Count(m => m.OperatorState == OperatorState.Producing),
            machines.Count(m => m.OperatorState == OperatorState.WaitingForBundle),
            machines.Count(m => m.OperatorState == OperatorState.Break),
            today.Total.Produced,
            today.Total.Rejected,
            today.Total.TargetQuantity,
            today.Total.EfficiencyPercent,
            pendingRework);

        return new LiveDashboardDto(
            now, dayStart, summary, lineRows, bundleStatuses.OrderBy(b => b.Status).ToList(), wip);
    }

    /// <summary>Bant başına şu an yürüyen plan ve o siparişten banttan çıkan adet.</summary>
    private async Task<Dictionary<int, LiveLinePlanDto>> LoadCurrentPlansAsync(
        List<int> lineIds, DateTime now, Dictionary<int, int> lineOfMachine, CancellationToken cancellationToken)
    {
        // Sonradan silinen ürün / model de planın adında görünür.
        var plans = await db.LineProductionPlans
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(p => lineIds.Contains(p.ProductionLineId)
                && p.Status == LinePlanStatus.Planned && p.PlannedStart <= now && p.PlannedEnd > now
                && !p.ProductionOrder.IsDeleted)
            .OrderBy(p => p.PlannedStart)
            .Select(p => new
            {
                p.Id,
                p.ProductionLineId,
                p.ProductionOrderId,
                p.ProductionOrder.OrderNo,
                p.ProductionOrder.Product.Model.ModelCode,
                p.PlannedQuantity,
                p.PlannedEnd,
            })
            .ToListAsync(cancellationToken);
        if (plans.Count == 0)
        {
            return [];
        }

        // Rotanın son operasyonundan (ardılı olmayan) geçen sağlam adet, sipariş ve makine bazında.
        var orderIds = plans.Select(p => p.ProductionOrderId).Distinct().ToList();
        var finished = await db.BundleOperations
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(o => orderIds.Contains(o.Bundle.ProductionOrderId)
                && o.Bundle.Status != BundleStatus.Cancelled
                && o.MachineId != null
                && !db.OperationDependencies.Any(d => d.PredecessorId == o.RouteOperationId))
            .GroupBy(o => new { o.Bundle.ProductionOrderId, MachineId = o.MachineId!.Value })
            .Select(g => new { g.Key.ProductionOrderId, g.Key.MachineId, Quantity = g.Sum(o => o.ProducedQuantity) })
            .ToListAsync(cancellationToken);

        return plans
            .GroupBy(p => p.ProductionLineId)
            .ToDictionary(g => g.Key, g =>
            {
                var plan = g.First();
                var produced = finished
                    .Where(f => f.ProductionOrderId == plan.ProductionOrderId
                        && lineOfMachine.GetValueOrDefault(f.MachineId) == plan.ProductionLineId)
                    .Sum(f => f.Quantity);
                return new LiveLinePlanDto(
                    plan.Id, plan.ProductionOrderId, plan.OrderNo, plan.ModelCode, plan.PlannedQuantity, produced,
                    Math.Min(100, Math.Round(produced * 100m / plan.PlannedQuantity, 1)), plan.PlannedEnd);
            });
    }

    private static LiveLineDto ToLine(
        int? id, string code, string name, LiveLinePlanDto? plan, PerformanceRowDto? today,
        IReadOnlyList<MachineActivityDto> machines) => new(
        id, code, name, plan,
        today?.Produced ?? 0,
        today?.Rejected ?? 0,
        today?.TargetQuantity,
        today?.EfficiencyPercent,
        today?.ProducedPerHour,
        today?.TargetPerHour,
        today?.BreakdownMinutes ?? 0,
        today?.WaitingMinutes ?? 0,
        today?.BreakMinutes ?? 0,
        machines);
}
