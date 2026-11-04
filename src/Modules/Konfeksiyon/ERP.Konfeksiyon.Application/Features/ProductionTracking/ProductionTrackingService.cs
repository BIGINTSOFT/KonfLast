using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Konfeksiyon.Application.Features.Bundles;
using ERP.Konfeksiyon.Domain.Entities;
using ERP.Konfeksiyon.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERP.Konfeksiyon.Application.Features.ProductionTracking;

public interface IProductionTrackingService
{
    /// <summary>Siparişin üretim ilerlemesi: toplamlar, operasyon ve renk-beden bazında döküm.</summary>
    Task<Result<OrderProgressDto>> GetOrderProgressAsync(int orderId, CancellationToken cancellationToken);

    /// <summary>Operasyon bazında ara stok: bekleyen, işlemdeki ve yarım bırakılmış demetler.</summary>
    Task<IReadOnlyList<WipOperationDto>> GetWipAsync(WipQuery query, CancellationToken cancellationToken);

    /// <summary>Bir operasyonun önündeki / üzerindeki demetler.</summary>
    Task<PagedResult<WipBundleDto>> GetWipBundlesAsync(WipBundleQuery query, CancellationToken cancellationToken);

    /// <summary>Aktif makinelerin şu anki durumu, operatörü ve süren işi.</summary>
    Task<IReadOnlyList<MachineActivityDto>> GetMachinesAsync(CancellationToken cancellationToken);

    /// <summary>Tarih aralığında operatör / makine / operasyon bazında üretilen ve hatalı adet.</summary>
    Task<Result<OutputReportDto>> GetOutputAsync(OutputQuery query, CancellationToken cancellationToken);
}

/// <summary>
/// Üretim takibi sorguları. Veri kopyası tutulmaz: ilerleme demet operasyonlarından, çıktı üretim
/// olaylarından hesaplanır. Sorgular geçmişi eksiksiz göstermek için sonradan silinen ana verileri
/// (operasyon, makine, operatör) de içerir.
/// </summary>
public sealed class ProductionTrackingService(IKonfeksiyonDbContext db) : IProductionTrackingService
{
    public const int MaxOutputRangeDays = 366;
    private const int MaxMachines = 500;

    /// <summary>Sahadaki demetler: kartı atanmış ve henüz bitmemiş.</summary>
    private static readonly BundleStatus[] OnFloor =
    [
        BundleStatus.Waiting, BundleStatus.InProduction, BundleStatus.PartiallyCompleted, BundleStatus.Rework,
    ];

    /// <summary>Artık üzerinde iş yapılmayacak demetler.</summary>
    private static readonly BundleStatus[] Finished = [BundleStatus.Completed, BundleStatus.Closed];

    private static readonly BundleOperationStatus[] WipStatuses =
    [
        BundleOperationStatus.Ready, BundleOperationStatus.InProgress, BundleOperationStatus.Paused,
    ];

    public async Task<Result<OrderProgressDto>> GetOrderProgressAsync(int orderId, CancellationToken cancellationToken)
    {
        var order = await db.ProductionOrders
            .AsNoTracking()
            .Where(o => o.Id == orderId)
            .Select(o => new { o.Id, o.OrderNo, o.Status, o.SalesOrderId, o.ProductId })
            .SingleOrDefaultAsync(cancellationToken);
        if (order is null)
        {
            return new Error(ErrorCodes.NotFound, "İş emri bulunamadı.", ErrorType.NotFound);
        }

        var items = await db.ProductionOrderItems
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(i => i.ProductionOrderId == orderId)
            .OrderBy(i => i.Color.ColorCode).ThenBy(i => i.Size.SortOrder).ThenBy(i => i.Size.SizeCode)
            .Select(i => new { i.ColorId, ColorName = i.Color.Name, i.SizeId, SizeName = i.Size.Name, i.Quantity })
            .ToListAsync(cancellationToken);

        var bundles = await db.Bundles
            .AsNoTracking()
            .Where(b => b.ProductionOrderId == orderId && b.Status != BundleStatus.Cancelled)
            .Select(b => new { b.Id, b.ColorId, b.SizeId, b.Quantity, b.Status })
            .ToListAsync(cancellationToken);

        var operations = await OrderOperations(orderId)
            .GroupBy(o => new { o.SequenceNo, o.OperationId, o.Operation.OperationCode, o.Operation.Name })
            .Select(g => new
            {
                g.Key.SequenceNo,
                g.Key.OperationId,
                g.Key.OperationCode,
                g.Key.Name,
                Planned = g.Sum(o => o.Status == BundleOperationStatus.Cancelled ? 0 : o.PlannedQuantity),
                Produced = g.Sum(o => o.ProducedQuantity),
                Rejected = g.Sum(o => o.RejectedQuantity),
                Waiting = g.Count(o => o.Status == BundleOperationStatus.Ready || o.Status == BundleOperationStatus.Paused),
                InProgress = g.Count(o => o.Status == BundleOperationStatus.InProgress),
                Completed = g.Count(o => o.Status == BundleOperationStatus.Completed),
                Total = g.Count(),
            })
            .OrderBy(o => o.SequenceNo)
            .ToListAsync(cancellationToken);

        // Demetten çıkan sağlam adet: demetin kendi sıra moduna göre son adım(lar)ında üretilen en küçük adet
        // (Bundle.FinishedQuantity ile aynı kural; sırasızda bütün adımlar sondur, her parça bir kez sayılır).
        var finalSteps = await db.LoadFinalStepsAsync(
            db.Bundles.Where(b => b.ProductionOrderId == orderId && Finished.Contains(b.Status)).Select(b => b.Id),
            cancellationToken);
        var finishedByBundle = (await OrderOperations(orderId)
                .Where(o => Finished.Contains(o.Bundle.Status))
                .Select(o => new { o.BundleId, o.RouteOperationId, o.Status, o.ProducedQuantity })
                .ToListAsync(cancellationToken))
            .Where(o => finalSteps.TryGetValue(o.BundleId, out var final) && final.Contains(o.RouteOperationId))
            .GroupBy(o => o.BundleId)
            .ToDictionary(
                g => g.Key,
                g => g.Min(o => o.Status == BundleOperationStatus.Completed ? o.ProducedQuantity : 0));

        var rejectedByBundle = await OrderOperations(orderId)
            .GroupBy(o => o.BundleId)
            .Select(g => new { BundleId = g.Key, Quantity = g.Sum(o => o.RejectedQuantity) })
            .ToDictionaryAsync(g => g.BundleId, g => g.Quantity, cancellationToken);

        var itemRows = items
            .Select(i =>
            {
                var own = bundles.Where(b => b.ColorId == i.ColorId && b.SizeId == i.SizeId).ToList();
                return new OrderItemProgressDto(
                    i.ColorId, i.ColorName, i.SizeId, i.SizeName, i.Quantity,
                    own.Sum(b => b.Quantity),
                    own.Sum(b => finishedByBundle.GetValueOrDefault(b.Id)),
                    own.Sum(b => rejectedByBundle.GetValueOrDefault(b.Id)));
            })
            .ToList();

        var shipped = await ShippedAsync(
            orderId, order.SalesOrderId, order.ProductId, items.Select(i => (i.ColorId, i.SizeId, i.Quantity)).ToList(),
            cancellationToken);

        var ordered = items.Sum(i => i.Quantity);
        var finished = bundles.Sum(b => finishedByBundle.GetValueOrDefault(b.Id));
        var open = bundles.Where(b => !Finished.Contains(b.Status)).ToList();

        return new OrderProgressDto(
            order.Id,
            order.OrderNo,
            order.Status,
            ordered,
            bundles.Sum(b => b.Quantity),
            finished,
            bundles.Sum(b => rejectedByBundle.GetValueOrDefault(b.Id)),
            open.Sum(b => b.Quantity),
            Math.Max(ordered - finished, 0),
            ordered == 0 ? 0 : Math.Min(100, Math.Round(finished * 100m / ordered, 1)),
            open.Count,
            order.Status == ProductionOrderStatus.InProduction && open.Count == 0,
            bundles
                .GroupBy(b => b.Status)
                .OrderBy(g => g.Key)
                .Select(g => new BundleStatusCountDto(g.Key, g.Count(), g.Sum(b => b.Quantity)))
                .ToList(),
            operations
                .Select(o => new OrderOperationProgressDto(
                    o.SequenceNo, o.OperationId, o.OperationCode, o.Name, o.Planned, o.Produced, o.Rejected,
                    Math.Max(o.Planned - o.Produced - o.Rejected, 0), o.Waiting, o.InProgress, o.Completed, o.Total))
                .ToList(),
            itemRows,
            (int)shipped);
    }

    public async Task<IReadOnlyList<WipOperationDto>> GetWipAsync(WipQuery query, CancellationToken cancellationToken)
    {
        var rows = await WipOperations(query.ProductionOrderId)
            .GroupBy(o => new { o.OperationId, o.Operation.OperationCode, o.Operation.Name })
            .Select(g => new WipOperationDto(
                g.Key.OperationId,
                g.Key.OperationCode,
                g.Key.Name,
                g.Count(o => o.Status == BundleOperationStatus.Ready),
                g.Sum(o => o.Status == BundleOperationStatus.Ready
                    ? o.PlannedQuantity - o.ProducedQuantity - o.RejectedQuantity : 0),
                g.Count(o => o.Status == BundleOperationStatus.InProgress),
                g.Sum(o => o.Status == BundleOperationStatus.InProgress
                    ? o.PlannedQuantity - o.ProducedQuantity - o.RejectedQuantity : 0),
                g.Count(o => o.Status == BundleOperationStatus.Paused),
                g.Sum(o => o.Status == BundleOperationStatus.Paused
                    ? o.PlannedQuantity - o.ProducedQuantity - o.RejectedQuantity : 0)))
            .ToListAsync(cancellationToken);

        return rows.OrderBy(r => r.OperationCode, StringComparer.Ordinal).ToList();
    }

    public Task<PagedResult<WipBundleDto>> GetWipBundlesAsync(WipBundleQuery query, CancellationToken cancellationToken)
    {
        var operations = WipOperations(query.ProductionOrderId).Where(o => o.OperationId == query.OperationId);
        if (query.Status is { } status)
        {
            operations = operations.Where(o => o.Status == status);
        }

        // İşlemdekiler üstte; bekleyenlerde en eski demet önce (ilk giren ilk çıkar).
        return operations
            .OrderByDescending(o => o.Status == BundleOperationStatus.InProgress)
            .ThenBy(o => o.BundleId)
            .Select(o => new WipBundleDto(
                o.Id,
                o.BundleId,
                o.Bundle.BundleNo,
                o.Bundle.ProductionOrderId,
                o.Bundle.ProductionOrder.OrderNo,
                o.Bundle.ProductionOrder.Product.Model.ModelCode,
                o.Bundle.Color.Name,
                o.Bundle.Size.Name,
                o.Status,
                o.PlannedQuantity,
                o.ProducedQuantity,
                o.RejectedQuantity,
                o.PlannedQuantity - o.ProducedQuantity - o.RejectedQuantity,
                o.Machine!.MachineCode,
                o.Operator == null ? null : o.Operator.FirstName + " " + o.Operator.LastName,
                o.StartedAt))
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<IReadOnlyList<MachineActivityDto>> GetMachinesAsync(CancellationToken cancellationToken)
    {
        var machines = await db.Machines
            .AsNoTracking()
            .Where(m => m.IsActive)
            .OrderBy(m => m.MachineCode)
            .Take(MaxMachines)
            .Select(m => new { m.Id, m.MachineCode, m.MachineName, m.Status, OperationName = m.Operation!.Name })
            .ToListAsync(cancellationToken);
        var machineIds = machines.Select(m => m.Id).ToList();

        var lines = await db.LineMachineAssignments
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(a => machineIds.Contains(a.MachineId) && a.UnassignedAt == null)
            .Select(a => new { a.MachineId, a.ProductionLine.LineCode })
            .ToDictionaryAsync(a => a.MachineId, a => a.LineCode, cancellationToken);

        var sessions = await db.WorkSessions
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(s => machineIds.Contains(s.MachineId) && s.LogoutAt == null)
            .Select(s => new { s.MachineId, Name = s.Operator.FirstName + " " + s.Operator.LastName, s.LoginAt, s.Status })
            .ToDictionaryAsync(s => s.MachineId, cancellationToken);

        var jobs = await db.BundleOperations
            .AsNoTracking()
            .Where(o => o.MachineId != null && machineIds.Contains(o.MachineId.Value) && o.Status == BundleOperationStatus.InProgress)
            .Select(o => new
            {
                MachineId = o.MachineId!.Value,
                o.BundleId,
                o.Bundle.BundleNo,
                o.StartedAt,
                Remaining = o.PlannedQuantity - o.ProducedQuantity - o.RejectedQuantity,
            })
            .ToDictionaryAsync(o => o.MachineId, cancellationToken);

        return machines
            .Select(m =>
            {
                var session = sessions.GetValueOrDefault(m.Id);
                var job = jobs.GetValueOrDefault(m.Id);
                return new MachineActivityDto(
                    m.Id, m.MachineCode, m.MachineName, m.Status, m.OperationName, lines.GetValueOrDefault(m.Id),
                    session?.Name, StateOf(session?.Status, job is not null), session?.LoginAt, job?.BundleId,
                    job?.BundleNo, job?.StartedAt, job?.Remaining);
            })
            .ToList();
    }

    public async Task<Result<OutputReportDto>> GetOutputAsync(OutputQuery query, CancellationToken cancellationToken)
    {
        if (query.From == default || query.To == default || query.To <= query.From)
        {
            return RangeError("Geçerli bir tarih aralığı verin: bitiş, başlangıçtan sonra olmalıdır.");
        }

        if ((query.To - query.From).TotalDays > MaxOutputRangeDays)
        {
            return RangeError($"Tarih aralığı en fazla {MaxOutputRangeDays} gün olabilir.");
        }

        var from = DateTime.SpecifyKind(query.From.ToUniversalTime(), DateTimeKind.Utc);
        var to = DateTime.SpecifyKind(query.To.ToUniversalTime(), DateTimeKind.Utc);

        var events = db.BundleOperationEvents
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(e => e.OccurredAt >= from && e.OccurredAt < to
                && (e.EventType == BundleOperationEventType.QuantityReported
                    || e.EventType == BundleOperationEventType.RejectionReported));

        // Gruplama anahtarı seçilen boyuta göre değişir; toplama hepsi için aynıdır.
        var facts = query.GroupBy switch
        {
            OutputGroupBy.Machine => events.Select(e => new
            {
                Id = e.MachineId, Code = e.Machine.MachineCode, Name = e.Machine.MachineName,
                e.ProducedQuantity, e.RejectedQuantity, e.BundleId,
            }),
            OutputGroupBy.Operation => events.Select(e => new
            {
                Id = e.BundleOperation.OperationId, Code = e.BundleOperation.Operation.OperationCode,
                Name = e.BundleOperation.Operation.Name, e.ProducedQuantity, e.RejectedQuantity, e.BundleId,
            }),
            // Operatörsüz elle kapatılan adet (ofis kapatması) ayrı satırda toplanır.
            _ => events.Select(e => new
            {
                Id = e.OperatorId ?? 0,
                Code = e.Operator == null ? "—" : e.Operator.OperatorCode,
                Name = e.Operator == null ? "Ofis kapatması (operatörsüz)" : e.Operator.FirstName + " " + e.Operator.LastName,
                e.ProducedQuantity, e.RejectedQuantity, e.BundleId,
            }),
        };

        var rows = await facts
            .GroupBy(f => new { f.Id, f.Code, f.Name })
            .Select(g => new OutputRowDto(
                g.Key.Id,
                g.Key.Code,
                g.Key.Name,
                g.Sum(f => f.ProducedQuantity),
                g.Sum(f => f.RejectedQuantity),
                g.Select(f => f.BundleId).Distinct().Count()))
            .ToListAsync(cancellationToken);

        var ordered = rows.OrderByDescending(r => r.Produced).ThenBy(r => r.Code, StringComparer.Ordinal).ToList();
        return new OutputReportDto(
            from, to, query.GroupBy, ordered.Sum(r => r.Produced), ordered.Sum(r => r.Rejected), ordered);
    }

    private static OperatorState StateOf(WorkSessionStatus? session, bool hasJob) => session switch
    {
        null or WorkSessionStatus.LoggedOut => OperatorState.Offline,
        WorkSessionStatus.Break => OperatorState.Break,
        WorkSessionStatus.Paused => OperatorState.Paused,
        WorkSessionStatus.MachineBreakdown => OperatorState.MachineBreakdown,
        WorkSessionStatus.WaitingForBundle => OperatorState.WaitingForBundle,
        _ => hasJob ? OperatorState.Producing : OperatorState.Working,
    };

    private static Error RangeError(string message) => new(
        ErrorCodes.ValidationError, message, ErrorType.Validation,
        new Dictionary<string, string[]> { ["from"] = [message] });

    /// <summary>
    /// Üretim siparişinden sevk edilen adet: bu siparişe bağlı sevk edilmiş sevkiyatlar, artı yalnızca müşteri
    /// siparişine bağlı (üretim siparişi seçilmemiş) sevkiyatlarda bu siparişin ürününden, siparişin renk-bedenlerinde
    /// sevk edilenler. İkincisi aynı müşteri siparişinin başka üretim siparişleriyle paylaşılabileceğinden renk-beden
    /// başına bu siparişin açık kalan adediyle (sipariş − doğrudan bağlı sevk) sınırlanır; her sevkiyat bir kez sayılır.
    /// </summary>
    private async Task<decimal> ShippedAsync(
        int orderId, int? salesOrderId, int productId, IReadOnlyList<(int ColorId, int SizeId, int Quantity)> items,
        CancellationToken cancellationToken)
    {
        var linked = await db.ShipmentItems
            .AsNoTracking()
            .Where(i => i.Shipment.ProductionOrderId == orderId && i.Shipment.Status == ShipmentStatus.Shipped)
            .GroupBy(i => new { i.StockItem.ColorId, i.StockItem.SizeId })
            .Select(g => new { g.Key.ColorId, g.Key.SizeId, Quantity = g.Sum(i => i.Quantity) })
            .ToListAsync(cancellationToken);
        var total = linked.Sum(g => g.Quantity);

        if (salesOrderId is not { } salesId)
        {
            return total;
        }

        var unlinked = await db.ShipmentItems
            .AsNoTracking()
            .Where(i => i.Shipment.ProductionOrderId == null && i.Shipment.SalesOrderId == salesId
                && i.Shipment.Status == ShipmentStatus.Shipped && i.StockItem.ProductId == productId)
            .GroupBy(i => new { i.StockItem.ColorId, i.StockItem.SizeId })
            .Select(g => new { g.Key.ColorId, g.Key.SizeId, Quantity = g.Sum(i => i.Quantity) })
            .ToListAsync(cancellationToken);
        if (unlinked.Count == 0)
        {
            return total;
        }

        // Renk-beden kalemi olmayan siparişte ürünün bütün sevki sayılır.
        if (items.Count == 0)
        {
            return total + unlinked.Sum(g => g.Quantity);
        }

        var ordered = items
            .GroupBy(i => (i.ColorId, i.SizeId))
            .ToDictionary(g => ((int?)g.Key.ColorId, (int?)g.Key.SizeId), g => (decimal)g.Sum(i => i.Quantity));
        var linkedByVariant = linked
            .GroupBy(g => (g.ColorId, g.SizeId))
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

        return total + unlinked
            .Where(g => ordered.ContainsKey((g.ColorId, g.SizeId)))
            .Sum(g => Math.Min(
                g.Quantity,
                Math.Max(ordered[(g.ColorId, g.SizeId)] - linkedByVariant.GetValueOrDefault((g.ColorId, g.SizeId)), 0)));
    }

    /// <summary>Siparişin iptal edilmemiş demetlerinin operasyonları.</summary>
    private IQueryable<BundleOperation> OrderOperations(int orderId) =>
        db.BundleOperations
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(o => o.Bundle.ProductionOrderId == orderId && o.Bundle.Status != BundleStatus.Cancelled);

    /// <summary>
    /// Sahadaki demetlerin yapılmayı bekleyen ya da süren operasyonları. Fasoncudaki demetler sahada olmadığından
    /// ara stoğa girmez (fason gönderimlerinde izlenir).
    /// </summary>
    private IQueryable<BundleOperation> WipOperations(int? orderId)
    {
        var operations = db.BundleOperations
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(o => WipStatuses.Contains(o.Status) && OnFloor.Contains(o.Bundle.Status) && !o.Bundle.AtSubcontractor);

        return orderId is { } id ? operations.Where(o => o.Bundle.ProductionOrderId == id) : operations;
    }
}
