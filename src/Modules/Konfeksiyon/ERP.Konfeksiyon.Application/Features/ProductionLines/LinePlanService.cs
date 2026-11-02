using ERP.Konfeksiyon.Application.Abstractions;
using System.Linq.Expressions;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Konfeksiyon.Domain.Entities;
using ERP.Konfeksiyon.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERP.Konfeksiyon.Application.Features.ProductionLines;

public interface ILinePlanService
{
    Task<PagedResult<LinePlanDto>> GetListAsync(LinePlanListQuery query, CancellationToken cancellationToken);
    Task<Result<LinePlanDto>> GetAsync(int id, CancellationToken cancellationToken);

    /// <summary>Bantın şu anki planı ve henüz başlamamış sıradaki planları.</summary>
    Task<(LinePlanDto? Current, IReadOnlyList<LinePlanDto> Upcoming)> GetCurrentAndUpcomingAsync(
        int lineId, int upcomingCount, CancellationToken cancellationToken);

    Task<Result<LinePlanDto>> CreateAsync(LinePlanRequest request, CancellationToken cancellationToken);
    Task<Result<LinePlanDto>> UpdateAsync(int id, LinePlanRequest request, CancellationToken cancellationToken);
    Task<Result<LinePlanDto>> CancelAsync(int id, string rowVersion, CancellationToken cancellationToken);
}

public sealed class LinePlanService(IKonfeksiyonDbContext db, IAppLock appLock, TimeProvider timeProvider) : ILinePlanService
{
    private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(10);

    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Bant planı bulunamadı.", ErrorType.NotFound);

    private static readonly Error ConcurrencyConflict = new(
        ErrorCodes.ConcurrencyConflict,
        "Plan siz açtıktan sonra başka bir kullanıcı tarafından değiştirildi. Sayfayı yenileyip tekrar deneyin.",
        ErrorType.Conflict);

    private static readonly Error Busy = new(
        ErrorCodes.ResourceBusy, "Bu bant için şu an başka bir planlama işlemi sürüyor. Lütfen tekrar deneyin.",
        ErrorType.Conflict);

    private DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;

    public async Task<PagedResult<LinePlanDto>> GetListAsync(
        LinePlanListQuery query, CancellationToken cancellationToken)
    {
        var plans = db.LineProductionPlans.AsNoTracking();

        if (query.ProductionLineId is { } lineId)
        {
            plans = plans.Where(p => p.ProductionLineId == lineId);
        }

        if (query.ProductionOrderId is { } orderId)
        {
            plans = plans.Where(p => p.ProductionOrderId == orderId);
        }

        if (query.Status is { } status)
        {
            plans = plans.Where(p => p.Status == status);
        }

        if (query.From is { } from)
        {
            var fromUtc = from.ToUniversalTime();
            plans = plans.Where(p => p.PlannedEnd > fromUtc);
        }

        if (query.To is { } to)
        {
            var toUtc = to.ToUniversalTime();
            plans = plans.Where(p => p.PlannedStart < toUtc);
        }

        var page = await plans
            .OrderBy(p => p.PlannedStart).ThenBy(p => p.Id)
            .Select(Projection)
            .ToPagedResultAsync(query, cancellationToken);

        var now = UtcNow;
        return new PagedResult<LinePlanDto>(
            page.Items.Select(row => ToDto(row, now)).ToList(), page.Page, page.PageSize, page.TotalCount);
    }

    public async Task<Result<LinePlanDto>> GetAsync(int id, CancellationToken cancellationToken)
    {
        var rows = await db.LineProductionPlans
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(Projection)
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return NotFound;
        }

        return ToDto(rows[0], UtcNow);
    }

    public async Task<(LinePlanDto? Current, IReadOnlyList<LinePlanDto> Upcoming)> GetCurrentAndUpcomingAsync(
        int lineId, int upcomingCount, CancellationToken cancellationToken)
    {
        var now = UtcNow;
        var rows = await db.LineProductionPlans
            .AsNoTracking()
            .Where(p => p.ProductionLineId == lineId && p.Status == LinePlanStatus.Planned && p.PlannedEnd > now)
            .OrderBy(p => p.PlannedStart)
            .Select(Projection)
            .Take(upcomingCount + 1)
            .ToListAsync(cancellationToken);

        var plans = rows.Select(row => ToDto(row, now)).ToList();
        var current = plans.FirstOrDefault(p => p.IsCurrent);
        return (current, plans.Where(p => !p.IsCurrent).Take(upcomingCount).ToList());
    }

    public async Task<Result<LinePlanDto>> CreateAsync(LinePlanRequest request, CancellationToken cancellationToken)
    {
        var lineError = await ReferenceValidation.RequireSelectableAsync(
            db.ProductionLines, request.ProductionLineId, null, "productionLineId", "Bant", cancellationToken);
        if (lineError is not null)
        {
            return lineError;
        }

        var plan = new LineProductionPlan
        {
            ProductionLineId = request.ProductionLineId, ProductionOrderId = request.ProductionOrderId,
        };
        Apply(request, plan);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (await CheckRulesAsync(plan, cancellationToken) is { } error)
        {
            return error;
        }

        db.LineProductionPlans.Add(plan);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await GetAsync(plan.Id, cancellationToken);
    }

    public async Task<Result<LinePlanDto>> UpdateAsync(
        int id, LinePlanRequest request, CancellationToken cancellationToken)
    {
        var plan = await db.LineProductionPlans.SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (plan is null)
        {
            return NotFound;
        }

        if (RowVersionToken.Check(plan.RowVersion, request.RowVersion, ConcurrencyConflict) is { } versionError)
        {
            return versionError;
        }

        if (!plan.IsEditable)
        {
            return NotEditable(plan);
        }

        Apply(request, plan);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (await CheckRulesAsync(plan, cancellationToken) is { } error)
        {
            return error;
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ConcurrencyConflict;
        }

        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<Result<LinePlanDto>> CancelAsync(int id, string rowVersion, CancellationToken cancellationToken)
    {
        var plan = await db.LineProductionPlans.SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (plan is null)
        {
            return NotFound;
        }

        if (RowVersionToken.Check(plan.RowVersion, rowVersion, ConcurrencyConflict) is { } versionError)
        {
            return versionError;
        }

        if (!plan.IsEditable)
        {
            return NotEditable(plan);
        }

        plan.Cancel();

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ConcurrencyConflict;
        }

        return await GetAsync(id, cancellationToken);
    }

    /// <summary>
    /// Sipariş durumu, bantta zaman çakışması ve sipariş adedi kurallarını denetler. Kontrol ile kayıt
    /// arasına başka bir istek giremesin diye önce bant, sonra sipariş için kilit alınır (her zaman bu
    /// sırayla; böylece iki istek birbirini kilitleyemez). Açık bir transaction içinde çağrılmalıdır.
    /// </summary>
    private async Task<Error?> CheckRulesAsync(LineProductionPlan plan, CancellationToken cancellationToken)
    {
        if (!await appLock.TryAcquireAsync($"line-plan:line:{plan.ProductionLineId}", LockTimeout, cancellationToken)
            || !await appLock.TryAcquireAsync($"line-plan:order:{plan.ProductionOrderId}", LockTimeout, cancellationToken))
        {
            return Busy;
        }

        var order = await db.ProductionOrders
            .Where(o => o.Id == plan.ProductionOrderId)
            .Select(o => new { o.OrderNo, o.Status, Total = o.Items.Sum(i => (int?)i.Quantity) ?? 0 })
            .SingleOrDefaultAsync(cancellationToken);
        if (order is null)
        {
            return ReferenceValidation.InvalidReference("productionOrderId", "Sipariş bulunamadı.");
        }

        if (order.Status is not (ProductionOrderStatus.Released or ProductionOrderStatus.InProduction))
        {
            const string message = "Yalnızca onaylı veya üretimdeki siparişler banta planlanabilir.";
            return new Error(
                ErrorCodes.OrderNotPlannable,
                message,
                ErrorType.BusinessRule,
                new Dictionary<string, string[]> { ["productionOrderId"] = [message] });
        }

        var overlapping = await db.LineProductionPlans
            .Where(p => p.ProductionLineId == plan.ProductionLineId
                && p.Id != plan.Id
                && p.Status != LinePlanStatus.Cancelled
                && p.PlannedStart < plan.PlannedEnd
                && plan.PlannedStart < p.PlannedEnd)
            .OrderBy(p => p.PlannedStart)
            .Select(p => p.ProductionOrder.OrderNo)
            .FirstOrDefaultAsync(cancellationToken);
        if (overlapping is not null)
        {
            var message = $"Bant bu zaman aralığında başka bir plana ayrılmış ({overlapping}).";
            return new Error(
                ErrorCodes.LinePlanOverlap,
                message,
                ErrorType.Conflict,
                new Dictionary<string, string[]> { ["plannedStart"] = [message] });
        }

        var plannedElsewhere = await db.LineProductionPlans
            .Where(p => p.ProductionOrderId == plan.ProductionOrderId
                && p.Id != plan.Id
                && p.Status != LinePlanStatus.Cancelled)
            .SumAsync(p => (int?)p.PlannedQuantity, cancellationToken) ?? 0;
        var remaining = order.Total - plannedElsewhere;
        if (plan.PlannedQuantity > remaining)
        {
            var message =
                $"{order.OrderNo} siparişinin toplamı {order.Total} adet; planlanabilecek kalan {Math.Max(remaining, 0)} adet.";
            return new Error(
                ErrorCodes.PlanExceedsOrderQuantity,
                message,
                ErrorType.BusinessRule,
                new Dictionary<string, string[]> { ["plannedQuantity"] = [message] });
        }

        return null;
    }

    private static void Apply(LinePlanRequest request, LineProductionPlan plan)
    {
        plan.PlannedStart = request.PlannedStart.ToUniversalTime();
        plan.PlannedEnd = request.PlannedEnd.ToUniversalTime();
        plan.PlannedQuantity = request.PlannedQuantity;
        plan.Notes = MasterDataRules.NullIfBlank(request.Notes);
    }

    private static Error NotEditable(LineProductionPlan plan) => new(
        ErrorCodes.LinePlanNotEditable,
        plan.Status == LinePlanStatus.Cancelled ? "İptal edilmiş plan değiştirilemez." : "Tamamlanmış plan değiştirilemez.");

    private sealed record Row(
        int Id, int LineId, string LineCode, string LineName, int OrderId, string OrderNo, string CustomerName,
        string ProductCode, string ProductName, string ModelCode, DateTime PlannedStart, DateTime PlannedEnd,
        int PlannedQuantity, LinePlanStatus Status, string? Notes, byte[] RowVersion);

    private static readonly Expression<Func<LineProductionPlan, Row>> Projection = p => new Row(
        p.Id,
        p.ProductionLineId,
        p.ProductionLine.LineCode,
        p.ProductionLine.Name,
        p.ProductionOrderId,
        p.ProductionOrder.OrderNo,
        p.ProductionOrder.Customer.Name,
        p.ProductionOrder.Product.ProductCode,
        p.ProductionOrder.Product.Name,
        p.ProductionOrder.Product.Model.ModelCode,
        p.PlannedStart,
        p.PlannedEnd,
        p.PlannedQuantity,
        p.Status,
        p.Notes,
        p.RowVersion);

    private static LinePlanDto ToDto(Row row, DateTime utcNow) => new(
        row.Id,
        row.LineId,
        row.LineCode,
        row.LineName,
        row.OrderId,
        row.OrderNo,
        row.CustomerName,
        row.ProductCode,
        row.ProductName,
        row.ModelCode,
        row.PlannedStart,
        row.PlannedEnd,
        row.PlannedQuantity,
        row.Status,
        row.Status == LinePlanStatus.Planned,
        row.Status == LinePlanStatus.Planned && row.PlannedStart <= utcNow && utcNow < row.PlannedEnd,
        row.Notes,
        RowVersionToken.From(row.RowVersion));
}
