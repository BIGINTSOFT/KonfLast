using ERP.Boyahane.Application.Features.Warehousing;
using ERP.Boyahane.Domain.Entities;
using ERP.Boyahane.Domain.Enums;
using ERP.Boyahane.Domain.Rules;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Boyahane.Application.Features.Insights;

public sealed record BoyahaneDashboardMachineDto(
    int Id,
    string Code,
    string Name,
    DyeMachineStatus Status,
    int? BatchId,
    string? BatchNo,
    string? OperationName,
    string? OperatorName,
    DateTime? StartedAt);

public sealed record BoyahaneDashboardBatchDto(int Id, string BatchNo, string MachineCode, DateOnly? PlannedDate, decimal Kilograms, string? NextOperation);

public sealed record BoyahaneDashboardDto(
    int Planned,
    int InProgress,
    int CompletedToday,
    int MachinesDown,
    IReadOnlyList<BoyahaneDashboardMachineDto> Machines,
    IReadOnlyList<BoyahaneDashboardBatchDto> Waiting);

/// <summary>Kurulum rehberindeki tanım sayıları.</summary>
public sealed record BoyahaneSetupDto(
    int Customers,
    int Fabrics,
    int Yarns,
    int DyeChemicals,
    int DefectTypes,
    int Operations,
    int Routes,
    int Machines,
    int MachinesWithoutOperation,
    int Tablets,
    int RfidCards,
    int Operators,
    int StopReasons);

public sealed class BoyahaneTrackingQuery : PageRequest
{
    /// <summary>Parti no, makine ya da personel.</summary>
    public string? Search { get; set; }
}

public sealed record BoyahaneTrackingRowDto(
    int Id,
    int BatchId,
    string BatchNo,
    string OperationName,
    BatchStepStatus Status,
    string? MachineCode,
    string? StartedBy,
    string? CompletedBy,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    ColorCheckResult? ColorResult);

public sealed record BoyahaneGoodsStatusDto(
    string? OwnerName,
    string CardCode,
    string CardName,
    bool IsYarn,
    decimal PendingInspection,
    decimal Ready,
    decimal Assigned,
    decimal InBatch,
    decimal Done,
    decimal Rejected);

public sealed class BoyahaneOrderTrackingQuery : PageRequest
{
    /// <summary>Sipariş no, müşteri sipariş no ya da müşteri.</summary>
    public string? Search { get; set; }
}

/// <param name="ShippedMeters">Onaylı irsaliyedeki paketlerin metresi.</param>
/// <param name="HasUnmeasuredShipment">Sevk edilen paketlerden birinin metresi yok (iplik ya da ölçülmemiş); metre kalanı hesaplanamaz.</param>
public sealed record BoyahaneOrderTrackingDto(
    int Id,
    int OrderId,
    string OrderNo,
    string? CustomerName,
    DateOnly? DueDate,
    OrderProcessType ProcessType,
    string CardCode,
    string? ColorName,
    decimal Quantity,
    StockUnit Unit,
    decimal AssignedKilograms,
    decimal InBatchKilograms,
    decimal DoneKilograms,
    decimal DoneMeters,
    decimal PackedKilograms,
    decimal ShippedKilograms,
    decimal InvoicedQuantity,
    BillingUnit? LineBillingUnit,
    BillingUnit? CustomerBillingUnit,
    decimal ShippedMeters,
    bool HasUnmeasuredShipment,
    string? CustomerOrderNo)
{
    /// <summary>Sipariş biriminde sevk edilmeyi bekleyen miktar; metre satırında sevk metresi bilinmiyorsa boş.</summary>
    public decimal? Remaining => Unit == StockUnit.Kilogram ? Quantity - ShippedKilograms
        : HasUnmeasuredShipment ? null
        : Quantity - ShippedMeters;

    /// <summary>Faturanın ölçüsü: satır, müşteri, sipariş birimi sırasıyla.</summary>
    public StockUnit BillableUnit => Billing.Resolve(LineBillingUnit, CustomerBillingUnit, Unit, ProcessType == OrderProcessType.YarnDyeing);

    /// <summary>Uygun malın çıkış ölçüsünden faturalanacak miktar.</summary>
    public decimal BillableQuantity => BillableUnit == StockUnit.Meter ? DoneMeters : DoneKilograms;
}

public sealed class BoyahaneReportQuery
{
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
}

public sealed record ConsumptionReportRow(string Code, string Name, decimal PlannedKilograms, decimal ActualKilograms, decimal WithoutLotKilograms);

public sealed record OperationReportRow(string Name, int Count, decimal AverageMinutes, decimal? StandardMinutes);

public sealed record OperatorReportRow(string Name, int Steps, decimal Minutes);

public sealed record DowntimeReportRow(string MachineCode, DyeMachineStatus Status, int Count, decimal Minutes);

public sealed record FireReportRow(
    string? OwnerName,
    string CardCode,
    int Items,
    decimal InputKilograms,
    decimal FinalKilograms,
    decimal? FirePercent,
    decimal? TolerancePercent,
    bool OverTolerance);

public sealed record BoyahaneReportDto(
    DateOnly From,
    DateOnly To,
    int ColorCheckedBatches,
    int ColorFirstPassBatches,
    int Additions,
    int Redos,
    int ItemsApproved,
    int ItemsRejected,
    int ItemsReworked,
    IReadOnlyList<ConsumptionReportRow> Consumption,
    IReadOnlyList<FireReportRow> Fire,
    IReadOnlyList<OperationReportRow> Operations,
    IReadOnlyList<OperatorReportRow> Operators,
    IReadOnlyList<DowntimeReportRow> Downtime,
    int Strippings = 0,
    decimal? StrippingRatePercent = null);

/// <summary>Sevk toplamı: irsaliye, paket, kilo ve metre.</summary>
public sealed record ShipmentTotals(int DespatchCount, int PackageCount, decimal Kilograms, decimal Meters);

public sealed record DailyShipmentCustomerRow(int CustomerId, string CustomerName, ShipmentTotals Totals);

public sealed record DailyShipmentDayDto(DateOnly Day, IReadOnlyList<DailyShipmentCustomerRow> Customers, ShipmentTotals Total);

/// <summary>Günlük sevk: onaylı irsaliyeler, gün ve müşteri bazında; gün ara toplamı ve genel toplam.</summary>
public sealed record BoyahaneShipmentReportDto(DateOnly From, DateOnly To, IReadOnlyList<DailyShipmentDayDto> Days, ShipmentTotals Total);

public sealed class BoyahaneIdleStockQuery
{
    /// <summary>Bu kadar gündür hareket görmeyen stok listelenir.</summary>
    public int Days { get; set; } = 90;
}

/// <summary>Adreste bekleyen mal (top, iplik lotu, paket); son hareket adres hareketi ya da giriş zamanıdır.</summary>
public sealed record IdleGoodsRow(
    WarehouseGoodsType GoodsType,
    string GoodsNo,
    string CardCode,
    string CardName,
    string? OwnerName,
    string? WarehouseName,
    string? LocationCode,
    decimal Kilograms,
    decimal? Meters,
    DateTime LastMovementAt,
    int IdleDays);

/// <summary>Kalanı olan boya / kimyasal lotu; son hareket lot hareketi ya da giriş zamanıdır.</summary>
public sealed record IdleChemicalRow(
    string LotNo,
    string Code,
    string Name,
    string? SupplierName,
    decimal RemainingKilograms,
    string? WarehouseName,
    string? LocationCode,
    DateTime LastMovementAt,
    int IdleDays);

/// <param name="Truncated">Satır sınırı aşıldı; en uzun bekleyenler listelenir.</param>
public sealed record BoyahaneIdleStockDto(int Days, IReadOnlyList<IdleGoodsRow> Goods, IReadOnlyList<IdleChemicalRow> Chemicals, bool Truncated);

public interface IBoyahaneInsightService
{
    Task<BoyahaneDashboardDto> GetDashboardAsync(CancellationToken cancellationToken);
    Task<BoyahaneSetupDto> GetSetupAsync(CancellationToken cancellationToken);
    Task<PagedResult<BoyahaneTrackingRowDto>> GetTrackingAsync(BoyahaneTrackingQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyList<BoyahaneGoodsStatusDto>> GetGoodsStatusAsync(CancellationToken cancellationToken);
    Task<PagedResult<BoyahaneOrderTrackingDto>> GetOrderTrackingAsync(BoyahaneOrderTrackingQuery query, CancellationToken cancellationToken);
    Task<BoyahaneReportDto> GetReportAsync(BoyahaneReportQuery query, CancellationToken cancellationToken);
    Task<BoyahaneShipmentReportDto> GetShipmentReportAsync(BoyahaneReportQuery query, CancellationToken cancellationToken);
    Task<BoyahaneIdleStockDto> GetIdleStockAsync(BoyahaneIdleStockQuery query, CancellationToken cancellationToken);
}

/// <summary>Salt okunur ekranlar: pano, kurulum rehberi, üretim ve sipariş takibi, mal durumu, raporlar.</summary>
public sealed class BoyahaneInsightService(IAppDbContext db, TimeProvider timeProvider) : IBoyahaneInsightService
{
    private const int WaitingSize = 20;
    private const int DefaultReportDays = 30;
    private const int MaxIdleRows = 1000;
    private const int MaxIdleDays = 3650;

    public async Task<BoyahaneDashboardDto> GetDashboardAsync(CancellationToken cancellationToken)
    {
        // "Bugün" fabrikanın yerel günüdür; zamanlar UTC tutulduğu için yerel gün başı UTC'ye çevrilir.
        var today = LocalDayStartUtc(DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime), timeProvider.LocalTimeZone);
        var batches = db.Set<BoyahaneBatch>().AsNoTracking();
        var planned = await batches.CountAsync(b => b.Status == BatchStatus.Planned, cancellationToken);
        var inProgress = await batches.CountAsync(b => b.Status == BatchStatus.OnMachine, cancellationToken);
        var completedToday = await batches.CountAsync(b => b.Status == BatchStatus.Completed && b.CompletedAt >= today, cancellationToken);

        var machines = await db.Set<BoyahaneMachine>().AsNoTracking()
            .Where(m => m.IsActive)
            .OrderBy(m => m.Code)
            .Select(m => new { m.Id, m.Code, m.Name, m.Status })
            .ToListAsync(cancellationToken);
        var running = await db.Set<BoyahaneBatchStep>().AsNoTracking()
            .Where(s => s.Status == BatchStepStatus.InProgress && s.MachineId != null)
            .Select(s => new
            {
                MachineId = s.MachineId!.Value,
                s.BatchId,
                s.Batch.BatchNo,
                OperationName = s.Operation.Name,
                OperatorName = s.StartedByOperator != null ? s.StartedByOperator.Name : null,
                s.StartedAt,
            })
            .ToListAsync(cancellationToken);

        var waiting = await batches
            .Where(b => b.Status == BatchStatus.Planned || (b.Status == BatchStatus.OnMachine && !b.Steps.Any(s => s.Status == BatchStepStatus.InProgress)))
            .OrderBy(b => b.PlannedDate == null).ThenBy(b => b.PlannedDate).ThenBy(b => b.Id)
            .Take(WaitingSize)
            .Select(b => new BoyahaneDashboardBatchDto(
                b.Id,
                b.BatchNo,
                b.Machine.Code,
                b.PlannedDate,
                b.Items.Where(i => i.IsActive).Sum(i => i.Kilograms),
                b.Steps.Where(s => s.Status == BatchStepStatus.Pending).OrderBy(s => s.Sequence).Select(s => s.Operation.Name).FirstOrDefault()))
            .ToListAsync(cancellationToken);

        return new BoyahaneDashboardDto(
            planned,
            inProgress,
            completedToday,
            machines.Count(m => m.Status == DyeMachineStatus.Breakdown),
            machines.Select(m =>
            {
                var step = running.FirstOrDefault(r => r.MachineId == m.Id);
                return new BoyahaneDashboardMachineDto(m.Id, m.Code, m.Name, m.Status, step?.BatchId, step?.BatchNo, step?.OperationName, step?.OperatorName, step?.StartedAt);
            }).ToList(),
            waiting);
    }

    public async Task<BoyahaneSetupDto> GetSetupAsync(CancellationToken cancellationToken) => new(
        await db.Set<BoyahaneCustomer>().CountAsync(cancellationToken),
        await db.Set<BoyahaneFabric>().CountAsync(cancellationToken),
        await db.Set<BoyahaneYarn>().CountAsync(cancellationToken),
        await db.Set<BoyahaneDyeChemical>().CountAsync(cancellationToken),
        await db.Set<BoyahaneDefectType>().CountAsync(cancellationToken),
        await db.Set<BoyahaneOperation>().CountAsync(cancellationToken),
        await db.Set<BoyahaneRoute>().CountAsync(cancellationToken),
        await db.Set<BoyahaneMachine>().CountAsync(cancellationToken),
        await db.Set<BoyahaneMachine>().CountAsync(m => !db.Set<BoyahaneMachineOperation>().Any(o => o.MachineId == m.Id), cancellationToken),
        await db.Set<BoyahaneTablet>().CountAsync(cancellationToken),
        await db.Set<BoyahaneRfidCard>().CountAsync(cancellationToken),
        await db.Set<BoyahaneOperator>().CountAsync(cancellationToken),
        await db.Set<BoyahaneStopReason>().CountAsync(cancellationToken));

    public Task<PagedResult<BoyahaneTrackingRowDto>> GetTrackingAsync(BoyahaneTrackingQuery query, CancellationToken cancellationToken)
    {
        var steps = db.Set<BoyahaneBatchStep>().AsNoTracking().Where(s => s.StartedAt != null);
        if (NullIfBlank(query.Search) is { } term)
        {
            steps = steps.Where(s => s.Batch.BatchNo.Contains(term)
                || (s.Machine != null && s.Machine.Code.Contains(term))
                || (s.StartedByOperator != null && s.StartedByOperator.Name.Contains(term)));
        }

        return steps
            .OrderByDescending(s => s.StartedAt)
            .Select(s => new BoyahaneTrackingRowDto(
                s.Id,
                s.BatchId,
                s.Batch.BatchNo,
                s.Operation.Name,
                s.Status,
                s.Machine != null ? s.Machine.Code : null,
                s.StartedByOperator != null ? s.StartedByOperator.Name : null,
                s.CompletedByOperator != null ? s.CompletedByOperator.Name : null,
                s.StartedAt,
                s.CompletedAt,
                s.ColorResult))
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<IReadOnlyList<BoyahaneGoodsStatusDto>> GetGoodsStatusAsync(CancellationToken cancellationToken)
    {
        var rolls = await db.Set<BoyahaneFabricRoll>().AsNoTracking()
            .Select(t => new
            {
                OwnerName = t.Lot.Receipt.Customer != null ? t.Lot.Receipt.Customer.Name : null,
                CardCode = t.Lot.Fabric.FabricCode,
                CardName = t.Lot.Fabric.Name,
                t.Status,
                Kilograms = t.InspectedKilograms ?? t.Kilograms,
                HasOrder = t.OrderLineId != null,
                Latest = db.Set<BoyahaneBatchItem>().Where(i => i.RollId == t.Id).OrderByDescending(i => i.Id)
                    .Select(i => new { i.IsActive, i.FinalResult, BatchStatus = i.Batch.Status }).FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);
        var lots = await db.Set<BoyahaneYarnLot>().AsNoTracking()
            .Select(l => new
            {
                OwnerName = l.Customer != null ? l.Customer.Name : null,
                CardCode = l.Yarn.YarnCode,
                CardName = l.Yarn.Name,
                l.Status,
                l.Kilograms,
                HasOrder = l.OrderLineId != null,
                Latest = db.Set<BoyahaneBatchItem>().Where(i => i.YarnLotId == l.Id).OrderByDescending(i => i.Id)
                    .Select(i => new { i.IsActive, i.FinalResult, BatchStatus = i.Batch.Status }).FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);

        var rows = rolls.Select(t => new
        {
            t.OwnerName,
            t.CardCode,
            t.CardName,
            IsYarn = false,
            t.Kilograms,
            Stage = t.Status == FabricRollStatus.Rejected ? Stage.Rejected
                : t.Status != FabricRollStatus.Approved ? Stage.PendingInspection
                : StageOf(t.HasOrder, t.Latest?.IsActive, t.Latest?.FinalResult, t.Latest?.BatchStatus),
        }).Concat(lots.Select(l => new
        {
            l.OwnerName,
            l.CardCode,
            l.CardName,
            IsYarn = true,
            l.Kilograms,
            Stage = l.Status == YarnLotStatus.Rejected ? Stage.Rejected : StageOf(l.HasOrder, l.Latest?.IsActive, l.Latest?.FinalResult, l.Latest?.BatchStatus),
        }));

        return rows
            .GroupBy(r => new { r.OwnerName, r.CardCode, r.CardName, r.IsYarn })
            .OrderBy(g => g.Key.OwnerName).ThenBy(g => g.Key.CardCode)
            .Select(g => new BoyahaneGoodsStatusDto(
                g.Key.OwnerName,
                g.Key.CardCode,
                g.Key.CardName,
                g.Key.IsYarn,
                g.Where(r => r.Stage == Stage.PendingInspection).Sum(r => r.Kilograms),
                g.Where(r => r.Stage == Stage.Ready).Sum(r => r.Kilograms),
                g.Where(r => r.Stage == Stage.Assigned).Sum(r => r.Kilograms),
                g.Where(r => r.Stage == Stage.InBatch).Sum(r => r.Kilograms),
                g.Where(r => r.Stage == Stage.Done).Sum(r => r.Kilograms),
                g.Where(r => r.Stage == Stage.Rejected).Sum(r => r.Kilograms)))
            .ToList();
    }

    public Task<PagedResult<BoyahaneOrderTrackingDto>> GetOrderTrackingAsync(BoyahaneOrderTrackingQuery query, CancellationToken cancellationToken)
    {
        var lines = db.Set<BoyahaneOrderLine>().AsNoTracking().Where(l => l.Order.Status == OrderStatus.Confirmed);
        if (NullIfBlank(query.Search) is { } term)
        {
            lines = lines.Where(l => l.Order.OrderNo.Contains(term)
                || (l.Order.CustomerOrderNo != null && l.Order.CustomerOrderNo.Contains(term))
                || (l.Order.Customer != null && l.Order.Customer.Name.Contains(term)));
        }

        return lines
            .OrderBy(l => l.Order.DueDate == null).ThenBy(l => l.Order.DueDate).ThenBy(l => l.Id)
            .Select(l => new BoyahaneOrderTrackingDto(
                l.Id,
                l.OrderId,
                l.Order.OrderNo,
                l.Order.Customer != null ? l.Order.Customer.Name : null,
                l.Order.DueDate,
                l.ProcessType,
                l.Fabric != null ? l.Fabric.FabricCode : l.Yarn != null ? l.Yarn.YarnCode : "",
                l.ColorName,
                l.Quantity,
                l.Unit,
                l.Rolls.Sum(t => t.InspectedKilograms ?? t.Kilograms) + l.YarnLots.Sum(y => y.Kilograms),
                db.Set<BoyahaneBatchItem>().Where(i => i.OrderLineId == l.Id && i.IsActive && (i.Batch.Status == BatchStatus.Planned || i.Batch.Status == BatchStatus.OnMachine)).Sum(i => i.Kilograms),
                db.Set<BoyahaneBatchItem>().Where(i => i.OrderLineId == l.Id && i.FinalResult == FinalQualityResult.Approved).Sum(i => i.FinalKilograms ?? i.Kilograms),
                db.Set<BoyahaneBatchItem>().Where(i => i.OrderLineId == l.Id && i.FinalResult == FinalQualityResult.Approved).Sum(i => i.FinalMeters ?? i.Meters ?? 0),
                db.Set<BoyahanePackingItem>().Where(i => i.IsActive && i.BatchItem.OrderLineId == l.Id).Sum(i => i.Kilograms),
                db.Set<BoyahanePackingItem>().Where(i => i.IsActive && i.BatchItem.OrderLineId == l.Id && i.PackingList.Despatch != null && i.PackingList.Despatch.Status == DespatchStatus.Confirmed).Sum(i => i.Kilograms),
                db.Set<BoyahaneInvoiceLine>().Where(i => i.OrderLineId == l.Id && i.Invoice.Status == InvoiceStatus.Confirmed).Sum(i => i.Quantity),
                l.BillingUnit,
                l.Order.Customer != null ? l.Order.Customer.BillingUnit : null,
                db.Set<BoyahanePackingItem>().Where(i => i.IsActive && i.BatchItem.OrderLineId == l.Id && i.PackingList.Despatch != null && i.PackingList.Despatch.Status == DespatchStatus.Confirmed).Sum(i => i.Meters ?? 0),
                db.Set<BoyahanePackingItem>().Any(i => i.IsActive && i.BatchItem.OrderLineId == l.Id && i.Meters == null && i.PackingList.Despatch != null && i.PackingList.Despatch.Status == DespatchStatus.Confirmed),
                l.Order.CustomerOrderNo))
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<BoyahaneReportDto> GetReportAsync(BoyahaneReportQuery query, CancellationToken cancellationToken)
    {
        // Tarihler yerel gündür; zamanlar UTC tutulduğu için aralık UTC'ye çevrilir.
        var zone = timeProvider.LocalTimeZone;
        var to = query.To ?? DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
        var from = query.From ?? to.AddDays(-DefaultReportDays);
        var start = LocalDayStartUtc(from, zone);
        var end = LocalDayStartUtc(to.AddDays(1), zone);

        var steps = await db.Set<BoyahaneBatchStep>().AsNoTracking()
            .Where(s => s.CompletedAt >= start && s.CompletedAt < end)
            .Select(s => new
            {
                s.ReworkKind,
                s.Operation.Behavior,
                s.ColorResult,
                OperationName = s.Operation.Name,
                s.Operation.StandardMinutes,
                OperatorName = s.CompletedByOperator != null ? s.CompletedByOperator.Name : null,
                s.StartedAt,
                s.CompletedAt,
            })
            .ToListAsync(cancellationToken);
        decimal Minutes(DateTime? a, DateTime? b) => a is { } x && b is { } y ? (decimal)(y - x).TotalMinutes : 0;

        // İlk seferde tutma partinin ilk renk kontrolüne bakar: ilk kontrolü dönemde biten parti sayılır, dönem öncesi
        // kontrolü olan partinin sonraki kontrolleri sayılmaz.
        var firstChecks = await db.Set<BoyahaneBatchStep>().AsNoTracking()
            .Where(s => s.Operation.Behavior == OperationBehavior.ColorCheck && s.CompletedAt >= start && s.CompletedAt < end
                && !db.Set<BoyahaneBatchStep>().Any(o => o.BatchId == s.BatchId && o.Operation.Behavior == OperationBehavior.ColorCheck
                    && o.CompletedAt != null && o.Sequence < s.Sequence))
            .Select(s => s.ColorResult)
            .ToListAsync(cancellationToken);
        var consumptionRows = await db.Set<BoyahaneBatchConsumption>().AsNoTracking()
            .Where(c => c.Step.CompletedAt >= start && c.Step.CompletedAt < end)
            .Select(c => new { c.DyeChemical.Code, c.DyeChemical.Name, c.PlannedKilograms, c.ActualKilograms, Allocated = c.Lots.Sum(l => l.Kilograms) })
            .ToListAsync(cancellationToken);
        var consumption = consumptionRows
            .GroupBy(c => new { c.Code, c.Name })
            .OrderBy(g => g.Key.Code)
            .Select(g => new ConsumptionReportRow(g.Key.Code, g.Key.Name, g.Sum(c => c.PlannedKilograms), g.Sum(c => c.ActualKilograms), g.Sum(c => c.ActualKilograms - c.Allocated)))
            .ToList();
        // Kalite ve fire kararın verildiği final kalite adımının bitişine göre dönemlenir; malın sonraki güncellemeleri sayılmaz.
        var decided = db.Set<BoyahaneBatchItem>().AsNoTracking()
            .Where(i => i.Batch.Steps.Any(s => s.Operation.Behavior == OperationBehavior.FinalQuality && s.CompletedAt >= start && s.CompletedAt < end));
        var results = await decided
            .Where(i => i.FinalResult != null)
            .GroupBy(i => i.FinalResult)
            .Select(g => new { Result = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var measured = await decided
            .Where(i => i.FinalResult == FinalQualityResult.Approved && i.FinalKilograms != null)
            .Select(i => new
            {
                OwnerName = i.OrderLine.Order.Customer != null ? i.OrderLine.Order.Customer.Name : null,
                Tolerance = i.OrderLine.Order.Customer != null ? i.OrderLine.Order.Customer.FireTolerancePercent : null,
                CardCode = i.OrderLine.Fabric != null ? i.OrderLine.Fabric.FabricCode : i.OrderLine.Yarn != null ? i.OrderLine.Yarn.YarnCode : "",
                i.Kilograms,
                Final = i.FinalKilograms!.Value,
            })
            .ToListAsync(cancellationToken);
        var logs = await db.Set<BoyahaneMachineStatusLog>().AsNoTracking()
            .Where(l => l.StartedAt < end && (l.EndedAt == null || l.EndedAt > start))
            .Select(l => new { l.Machine.Code, l.Status, l.StartedAt, l.EndedAt })
            .ToListAsync(cancellationToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        // Duruş dönem sınırlarına (açık kayıtta şimdiye) kırpılır; dönemi taşan kaydın tamamı sayılmaz.
        decimal ClippedMinutes(DateTime startedAt, DateTime? endedAt)
        {
            var clippedStart = startedAt > start ? startedAt : start;
            var stop = endedAt ?? now;
            var clippedEnd = stop < end ? stop : end;
            return clippedEnd > clippedStart ? (decimal)(clippedEnd - clippedStart).TotalMinutes : 0;
        }

        return new BoyahaneReportDto(
            from,
            to,
            firstChecks.Count,
            firstChecks.Count(r => r == ColorCheckResult.Approved),
            steps.Count(s => s.ReworkKind == ColorCheckResult.Addition),
            steps.Count(s => s.ReworkKind == ColorCheckResult.Redo),
            results.Where(r => r.Result == FinalQualityResult.Approved).Sum(r => r.Count),
            results.Where(r => r.Result == FinalQualityResult.Rejected).Sum(r => r.Count),
            results.Where(r => r.Result == FinalQualityResult.Rework).Sum(r => r.Count),
            consumption,
            measured.GroupBy(i => new { i.OwnerName, i.CardCode, i.Tolerance }).OrderBy(g => g.Key.OwnerName).ThenBy(g => g.Key.CardCode)
                .Select(g =>
                {
                    var fire = Billing.LossPercent(g.Sum(i => i.Kilograms), g.Sum(i => i.Final));
                    return new FireReportRow(g.Key.OwnerName, g.Key.CardCode, g.Count(), g.Sum(i => i.Kilograms), g.Sum(i => i.Final), fire, g.Key.Tolerance, fire > g.Key.Tolerance);
                }).ToList(),
            steps.GroupBy(s => new { s.OperationName, s.StandardMinutes }).OrderBy(g => g.Key.OperationName)
                .Select(g => new OperationReportRow(g.Key.OperationName, g.Count(), Math.Round(g.Average(s => Minutes(s.StartedAt, s.CompletedAt)), 1), g.Key.StandardMinutes)).ToList(),
            steps.Where(s => s.OperatorName != null).GroupBy(s => s.OperatorName!).OrderBy(g => g.Key)
                .Select(g => new OperatorReportRow(g.Key, g.Count(), Math.Round(g.Sum(s => Minutes(s.StartedAt, s.CompletedAt)), 1))).ToList(),
            logs.GroupBy(l => new { l.Code, l.Status }).OrderBy(g => g.Key.Code)
                .Select(g => new DowntimeReportRow(g.Key.Code, g.Key.Status, g.Count(), Math.Round(g.Sum(l => ClippedMinutes(l.StartedAt, l.EndedAt)), 1))).ToList(),
            steps.Count(s => s.Behavior == OperationBehavior.Stripping),
            StrippingRate(steps.Where(s => s.Behavior == OperationBehavior.ColorCheck).Select(s => s.ColorResult).ToList()));
    }

    /// <summary>Söküm oranı: dönemde biten renk kontrollerinden "söküm + yeniden boya" sonucunun yüzdesi; kontrol yoksa boş.</summary>
    private static decimal? StrippingRate(IReadOnlyList<ColorCheckResult?> results) =>
        results.Count == 0 ? null : Math.Round(results.Count(r => r == ColorCheckResult.Stripping) * 100m / results.Count, 1);

    public async Task<BoyahaneShipmentReportDto> GetShipmentReportAsync(BoyahaneReportQuery query, CancellationToken cancellationToken)
    {
        // İrsaliye tarihi zaten yerel gündür; zaman dönüşümü gerekmez.
        var to = query.To ?? DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
        var from = query.From ?? to.AddDays(-DefaultReportDays);
        var despatches = await db.Set<BoyahaneDespatch>().AsNoTracking()
            .Where(d => d.Status == DespatchStatus.Confirmed && d.DespatchDate >= from && d.DespatchDate <= to)
            .Select(d => new DespatchTotal(
                d.DespatchDate,
                d.CustomerId,
                d.Customer.Name,
                d.PackingLists.SelectMany(l => l.Items).Count(),
                d.PackingLists.SelectMany(l => l.Items).Sum(i => i.Kilograms),
                d.PackingLists.SelectMany(l => l.Items).Sum(i => i.Meters ?? 0)))
            .ToListAsync(cancellationToken);

        var days = despatches
            .GroupBy(d => d.Day)
            .OrderBy(g => g.Key)
            .Select(day => new DailyShipmentDayDto(
                day.Key,
                day.GroupBy(d => new { d.CustomerId, d.CustomerName })
                    .OrderBy(g => g.Key.CustomerName)
                    .Select(g => new DailyShipmentCustomerRow(g.Key.CustomerId, g.Key.CustomerName, TotalsOf(g)))
                    .ToList(),
                TotalsOf(day)))
            .ToList();
        return new BoyahaneShipmentReportDto(from, to, days, TotalsOf(despatches));
    }

    public async Task<BoyahaneIdleStockDto> GetIdleStockAsync(BoyahaneIdleStockQuery query, CancellationToken cancellationToken)
    {
        var days = Math.Clamp(query.Days, 1, MaxIdleDays);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var cutoff = now.AddDays(-days);

        // Son hareket adres değişimi ya da transferdir; hiç taşınmamış mal giriş zamanından sayılır.
        var rolls = await BoyahaneWarehouseRules.RollsInWarehouse(db).AsNoTracking()
            .Where(r => r.LocationId != null)
            .Select(r => new
            {
                No = r.RollNo,
                Code = r.Lot.Fabric.FabricCode,
                CardName = r.Lot.Fabric.Name,
                Owner = r.Lot.Receipt.Customer != null ? r.Lot.Receipt.Customer.Name : null,
                Warehouse = r.Location!.Warehouse.Name,
                Location = r.Location!.Code,
                Kilograms = r.InspectedKilograms ?? r.Kilograms,
                Meters = (decimal?)(r.InspectedMeters ?? r.Meters),
                Last = db.Set<BoyahaneLocationMovement>().Where(m => m.GoodsType == WarehouseGoodsType.FabricRoll && m.GoodsId == r.Id).Max(m => (DateTime?)m.CreatedAt) ?? r.CreatedAt,
            })
            .Where(x => x.Last < cutoff)
            .OrderBy(x => x.Last)
            .Take(MaxIdleRows + 1)
            .ToListAsync(cancellationToken);
        var yarns = await BoyahaneWarehouseRules.YarnLotsInWarehouse(db).AsNoTracking()
            .Where(y => y.LocationId != null)
            .Select(y => new
            {
                No = y.LotNo,
                Code = y.Yarn.YarnCode,
                CardName = y.Yarn.Name,
                Owner = y.Customer != null ? y.Customer.Name : null,
                Warehouse = y.Location!.Warehouse.Name,
                Location = y.Location!.Code,
                y.Kilograms,
                Meters = (decimal?)null,
                Last = db.Set<BoyahaneLocationMovement>().Where(m => m.GoodsType == WarehouseGoodsType.YarnLot && m.GoodsId == y.Id).Max(m => (DateTime?)m.CreatedAt) ?? y.CreatedAt,
            })
            .Where(x => x.Last < cutoff)
            .OrderBy(x => x.Last)
            .Take(MaxIdleRows + 1)
            .ToListAsync(cancellationToken);
        var packages = await BoyahaneWarehouseRules.PackagesInWarehouse(db).AsNoTracking()
            .Where(p => p.LocationId != null)
            .Select(p => new
            {
                No = p.PackageNo,
                Code = p.BatchItem.OrderLine.Fabric != null ? p.BatchItem.OrderLine.Fabric.FabricCode : p.BatchItem.OrderLine.Yarn != null ? p.BatchItem.OrderLine.Yarn.YarnCode : "",
                CardName = p.BatchItem.OrderLine.Fabric != null ? p.BatchItem.OrderLine.Fabric.Name : p.BatchItem.OrderLine.Yarn != null ? p.BatchItem.OrderLine.Yarn.Name : "",
                Owner = p.PackingList.Order.Customer != null ? p.PackingList.Order.Customer.Name : p.PackingList.SaleCustomer != null ? p.PackingList.SaleCustomer.Name : null,
                Warehouse = p.Location!.Warehouse.Name,
                Location = p.Location!.Code,
                p.Kilograms,
                p.Meters,
                Last = db.Set<BoyahaneLocationMovement>().Where(m => m.GoodsType == WarehouseGoodsType.Package && m.GoodsId == p.Id).Max(m => (DateTime?)m.CreatedAt) ?? p.CreatedAt,
            })
            .Where(x => x.Last < cutoff)
            .OrderBy(x => x.Last)
            .Take(MaxIdleRows + 1)
            .ToListAsync(cancellationToken);
        var chemicals = await db.Set<BoyahaneChemicalLot>().AsNoTracking()
            .Where(l => l.RemainingKilograms > 0)
            .Select(l => new
            {
                l.LotNo,
                l.DyeChemical.Code,
                l.DyeChemical.Name,
                l.SupplierName,
                l.RemainingKilograms,
                Warehouse = l.Location != null ? l.Location.Warehouse.Name : null,
                Location = l.Location != null ? l.Location.Code : null,
                Last = db.Set<BoyahaneChemicalMovement>().Where(m => m.LotId == l.Id).Max(m => (DateTime?)m.CreatedAt) ?? l.CreatedAt,
            })
            .Where(x => x.Last < cutoff)
            .OrderBy(x => x.Last)
            .Take(MaxIdleRows + 1)
            .ToListAsync(cancellationToken);

        int IdleDays(DateTime last) => (int)(now - last).TotalDays;
        var goods = rolls.Take(MaxIdleRows).Select(x => new IdleGoodsRow(WarehouseGoodsType.FabricRoll, x.No, x.Code, x.CardName, x.Owner, x.Warehouse, x.Location, x.Kilograms, x.Meters, x.Last, IdleDays(x.Last)))
            .Concat(yarns.Take(MaxIdleRows).Select(x => new IdleGoodsRow(WarehouseGoodsType.YarnLot, x.No, x.Code, x.CardName, x.Owner, x.Warehouse, x.Location, x.Kilograms, x.Meters, x.Last, IdleDays(x.Last))))
            .Concat(packages.Take(MaxIdleRows).Select(x => new IdleGoodsRow(WarehouseGoodsType.Package, x.No, x.Code, x.CardName, x.Owner, x.Warehouse, x.Location, x.Kilograms, x.Meters, x.Last, IdleDays(x.Last))))
            .OrderBy(x => x.LastMovementAt)
            .ToList();
        return new BoyahaneIdleStockDto(
            days,
            goods,
            chemicals.Take(MaxIdleRows).Select(x => new IdleChemicalRow(x.LotNo, x.Code, x.Name, x.SupplierName, x.RemainingKilograms, x.Warehouse, x.Location, x.Last, IdleDays(x.Last))).ToList(),
            rolls.Count > MaxIdleRows || yarns.Count > MaxIdleRows || packages.Count > MaxIdleRows || chemicals.Count > MaxIdleRows);
    }

    private sealed record DespatchTotal(DateOnly Day, int CustomerId, string CustomerName, int Packages, decimal Kilograms, decimal Meters);

    private static ShipmentTotals TotalsOf(IEnumerable<DespatchTotal> rows)
    {
        var list = rows as IReadOnlyCollection<DespatchTotal> ?? rows.ToList();
        return new ShipmentTotals(list.Count, list.Sum(r => r.Packages), list.Sum(r => r.Kilograms), list.Sum(r => r.Meters));
    }

    private static DateTime LocalDayStartUtc(DateOnly day, TimeZoneInfo zone) => TimeZoneInfo.ConvertTimeToUtc(day.ToDateTime(TimeOnly.MinValue), zone);

    private enum Stage
    {
        PendingInspection,
        Ready,
        Assigned,
        InBatch,
        Done,
        Rejected,
    }

    /// <summary>Malın bulunduğu aşama: son parti kaydı ve sipariş bağından çıkar.</summary>
    private static Stage StageOf(bool hasOrder, bool? isActive, FinalQualityResult? finalResult, BatchStatus? batchStatus)
    {
        if (finalResult == FinalQualityResult.Rejected)
        {
            return Stage.Rejected;
        }

        if (isActive == true && batchStatus is BatchStatus.Planned or BatchStatus.OnMachine)
        {
            return Stage.InBatch;
        }

        if (isActive == true && batchStatus == BatchStatus.Completed)
        {
            return Stage.Done;
        }

        return hasOrder ? Stage.Assigned : Stage.Ready;
    }
}
