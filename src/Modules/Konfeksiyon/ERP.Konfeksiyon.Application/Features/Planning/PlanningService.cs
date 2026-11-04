using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Konfeksiyon.Application.Features.Shifts;
using ERP.Konfeksiyon.Domain.Balancing;
using ERP.Konfeksiyon.Domain.Enums;
using ERP.Konfeksiyon.Domain.Planning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ERP.Konfeksiyon.Application.Features.Planning;

public interface IPlanningService
{
    /// <summary>Açık siparişlerin planlama durumu ve termin riski; en riskli ve termini en yakın olan önce.</summary>
    Task<IReadOnlyList<PlanningOrderDto>> GetOrdersAsync(CancellationToken cancellationToken);

    /// <summary>Aralıkta bantların çalışma süresi, planlara ayrılan kısmı ve her planın penceresine sığıp sığmadığı.</summary>
    Task<Result<LineLoadReportDto>> GetLineLoadAsync(LineLoadQuery query, CancellationToken cancellationToken);

    /// <summary>Siparişin her bant için en erken yerleştirilebileceği zaman ve terme yetişip yetişmediği.</summary>
    Task<Result<PlanSuggestionDto>> SuggestAsync(PlanSuggestionQuery query, CancellationToken cancellationToken);
}

/// <summary>
/// Planlama yardımcısı. Hiçbir şey kaydetmez: mevcut bant planlarını çalışma takvimi ve bant hızıyla
/// değerlendirir, yeni plan için öneri üretir. Plan, mevcut bant planı ucuyla kaydedilir.
///
/// Bant hızı = bantın modeli saatte çıkarabileceği adet; dengeleme hesabındaki (<see cref="LineBalancer"/>)
/// darboğaz kapasitesidir. Süreler çalışma takvimine (<see cref="WorkCalendar"/>) göre hesaplanır.
/// </summary>
public sealed class PlanningService(IKonfeksiyonDbContext db, IFactoryClock clock, IOptions<PlanningOptions> options)
    : IPlanningService
{
    public const int MaxHorizonDays = 92;
    public const int MaxOrders = 500;
    private const double TightLoadPercent = 90;

    private static readonly ProductionOrderStatus[] OpenStatuses =
        [ProductionOrderStatus.Released, ProductionOrderStatus.InProduction];

    private static readonly BundleStatus[] FinishedBundles = [BundleStatus.Completed, BundleStatus.Closed];
    private static readonly MachineStatus[] OutOfService = [MachineStatus.Breakdown, MachineStatus.Maintenance];
    private static readonly DayOfWeek[] DefaultWorkingDays =
    [
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday,
    ];

    private sealed record RouteStep(int Id, int OperationId, string OperationName, decimal Sam);

    /// <summary>Bantın bir modeli üretme hızı; rota yoksa <see cref="HasRoute"/> false.</summary>
    private sealed record LineRate(bool HasRoute, double? PerHour, string? BottleneckOperationName);

    public async Task<IReadOnlyList<PlanningOrderDto>> GetOrdersAsync(CancellationToken cancellationToken)
    {
        // Silinmiş müşteri / ürün de adıyla görünür; sipariş kendisi silinmemiş olmalıdır.
        var orders = await db.ProductionOrders
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(o => !o.IsDeleted && OpenStatuses.Contains(o.Status))
            .OrderBy(o => o.DueDate).ThenBy(o => o.Id)
            .Take(MaxOrders)
            .Select(o => new
            {
                o.Id,
                o.OrderNo,
                CustomerName = o.Customer.Name,
                o.Product.ModelId,
                o.Product.Model.ModelCode,
                ProductName = o.Product.Name,
                o.Status,
                o.DueDate,
                Ordered = o.Items.Sum(i => (int?)i.Quantity) ?? 0,
            })
            .ToListAsync(cancellationToken);
        var orderIds = orders.Select(o => o.Id).ToList();

        var plans = (await db.LineProductionPlans
                .AsNoTracking()
                .Where(p => orderIds.Contains(p.ProductionOrderId) && p.Status != LinePlanStatus.Cancelled)
                .GroupBy(p => p.ProductionOrderId)
                .Select(g => new { OrderId = g.Key, Planned = g.Sum(p => p.PlannedQuantity), LastEnd = g.Max(p => p.PlannedEnd) })
                .ToListAsync(cancellationToken))
            .ToDictionary(p => p.OrderId);

        // Biten adet: tamamlanan demetlerin son operasyon(lar)ından geçen en küçük adet.
        var finished = (await db.BundleOperations
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(o => orderIds.Contains(o.Bundle.ProductionOrderId)
                    && FinishedBundles.Contains(o.Bundle.Status)
                    && !db.OperationDependencies.Any(d => d.PredecessorId == o.RouteOperationId))
                .GroupBy(o => new { o.BundleId, o.Bundle.ProductionOrderId })
                .Select(g => new { g.Key.ProductionOrderId, Quantity = g.Min(o => o.ProducedQuantity) })
                .ToListAsync(cancellationToken))
            .GroupBy(b => b.ProductionOrderId)
            .ToDictionary(g => g.Key, g => g.Sum(b => b.Quantity));

        var routes = await LoadRoutesAsync(orders.Select(o => o.ModelId), cancellationToken);
        var today = DateOnly.FromDateTime(clock.ToLocal(clock.UtcNow));

        return orders
            .Select(o =>
            {
                var plan = plans.GetValueOrDefault(o.Id);
                var done = Math.Min(finished.GetValueOrDefault(o.Id), o.Ordered);
                var remaining = o.Ordered - done;
                var unplanned = Math.Max(o.Ordered - (plan?.Planned ?? 0), 0);
                var route = routes.GetValueOrDefault(o.ModelId);

                var risk = remaining <= 0
                    ? PlanningRisk.OnTrack
                    : o.DueDate < today
                        ? PlanningRisk.Late
                        : plan is not null && plan.LastEnd > DueDeadline(o.DueDate)
                            ? PlanningRisk.PlanAfterDueDate
                            : unplanned > 0 ? PlanningRisk.Unplanned : PlanningRisk.OnTrack;

                return new PlanningOrderDto(
                    o.Id, o.OrderNo, o.CustomerName, o.ModelCode, o.ProductName, o.Status, o.DueDate,
                    o.DueDate.DayNumber - today.DayNumber, o.Ordered, done, remaining, plan?.Planned ?? 0, unplanned,
                    route is not null,
                    route is null ? null : Round((double)(remaining * route.Sum(s => s.Sam)) / 60),
                    plan?.LastEnd, risk);
            })
            .OrderBy(o => o.Risk).ThenBy(o => o.DueDate).ThenBy(o => o.OrderNo, StringComparer.Ordinal)
            .ToList();
    }

    public async Task<Result<LineLoadReportDto>> GetLineLoadAsync(LineLoadQuery query, CancellationToken cancellationToken)
    {
        if (query.From == default || query.To == default || query.To <= query.From)
        {
            return Invalid("from", "Geçerli bir tarih aralığı verin: bitiş, başlangıçtan sonra olmalıdır.");
        }

        if ((query.To - query.From).TotalDays > MaxHorizonDays)
        {
            return Invalid("from", $"Tarih aralığı en fazla {MaxHorizonDays} gün olabilir.");
        }

        if (ResolveEfficiency(query.EfficiencyPercent) is not { } efficiency)
        {
            return InvalidEfficiency();
        }

        var from = DateTime.SpecifyKind(query.From.ToUniversalTime(), DateTimeKind.Utc);
        var to = DateTime.SpecifyKind(query.To.ToUniversalTime(), DateTimeKind.Utc);
        var (calendar, assumed) = await LoadCalendarAsync(cancellationToken);

        var lines = await db.ProductionLines
            .AsNoTracking()
            .Where(l => l.IsActive)
            .OrderBy(l => l.LineCode)
            .Select(l => new { l.Id, l.LineCode, l.Name })
            .ToListAsync(cancellationToken);
        var lineIds = lines.Select(l => l.Id).ToList();
        var machines = await LoadMachinesAsync(lineIds, cancellationToken);

        var plans = await db.LineProductionPlans
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(p => lineIds.Contains(p.ProductionLineId) && p.Status == LinePlanStatus.Planned
                && p.PlannedStart < to && p.PlannedEnd > from && !p.ProductionOrder.IsDeleted)
            .OrderBy(p => p.PlannedStart)
            .Select(p => new
            {
                p.Id,
                p.ProductionLineId,
                p.ProductionOrderId,
                p.ProductionOrder.OrderNo,
                p.ProductionOrder.Product.ModelId,
                p.ProductionOrder.Product.Model.ModelCode,
                p.ProductionOrder.DueDate,
                p.PlannedQuantity,
                p.PlannedStart,
                p.PlannedEnd,
            })
            .ToListAsync(cancellationToken);
        var routes = await LoadRoutesAsync(plans.Select(p => p.ModelId), cancellationToken);

        var report = lines
            .Select(line =>
            {
                var lineMachines = machines[line.Id].ToList();
                var working = WorkingHours(calendar, from, to);
                var booked = 0.0;

                var lineplans = plans
                    .Where(p => p.ProductionLineId == line.Id)
                    .Select(p =>
                    {
                        // Yük, planın kendi penceresinin tamamına göre; bantın doluluğu aralığa düşen kısmına göre.
                        var available = WorkingHours(calendar, p.PlannedStart, p.PlannedEnd);
                        booked += WorkingHours(calendar, p.PlannedStart < from ? from : p.PlannedStart, p.PlannedEnd > to ? to : p.PlannedEnd);

                        var rate = RateOf(lineMachines, routes.GetValueOrDefault(p.ModelId), efficiency);
                        double? required = rate.PerHour > 0 ? Round(p.PlannedQuantity / rate.PerHour.Value) : null;
                        double? load = required is not null && available > 0 ? Round(required.Value / available * 100) : null;

                        var status = !rate.HasRoute
                            ? PlanLoadStatus.NoRoute
                            : required is null
                                ? PlanLoadStatus.NoCapacity
                                : required > available
                                    ? PlanLoadStatus.Overloaded
                                    : load >= TightLoadPercent ? PlanLoadStatus.Tight : PlanLoadStatus.Ok;

                        return new PlanLoadDto(
                            p.Id, p.ProductionOrderId, p.OrderNo, p.ModelCode, p.PlannedQuantity, p.PlannedStart, p.PlannedEnd,
                            rate.PerHour, rate.BottleneckOperationName, required, available, load, status, p.DueDate,
                            p.PlannedEnd > DueDeadline(p.DueDate));
                    })
                    .ToList();

                booked = Round(booked);
                return new LineLoadDto(
                    line.Id, line.LineCode, line.Name, lineMachines.Count, working, booked, Round(Math.Max(working - booked, 0)),
                    working > 0 ? Round(booked / working * 100) : null, lineplans);
            })
            .ToList();

        return new LineLoadReportDto(from, to, efficiency, assumed, report);
    }

    public async Task<Result<PlanSuggestionDto>> SuggestAsync(PlanSuggestionQuery query, CancellationToken cancellationToken)
    {
        if (ResolveEfficiency(query.EfficiencyPercent) is not { } efficiency)
        {
            return InvalidEfficiency();
        }

        var order = await db.ProductionOrders
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(o => o.Id == query.ProductionOrderId && !o.IsDeleted)
            .Select(o => new
            {
                o.Id,
                o.OrderNo,
                o.Status,
                o.DueDate,
                o.Product.ModelId,
                o.Product.Model.ModelCode,
                Ordered = o.Items.Sum(i => (int?)i.Quantity) ?? 0,
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (order is null)
        {
            return new Error(ErrorCodes.NotFound, "İş emri bulunamadı.", ErrorType.NotFound);
        }

        if (!OpenStatuses.Contains(order.Status))
        {
            return new Error(ErrorCodes.OrderNotPlannable, "Yalnızca onaylı veya üretimdeki siparişler banta planlanabilir.");
        }

        var planned = await db.LineProductionPlans
            .Where(p => p.ProductionOrderId == order.Id && p.Status != LinePlanStatus.Cancelled)
            .SumAsync(p => (int?)p.PlannedQuantity, cancellationToken) ?? 0;
        var unplanned = Math.Max(order.Ordered - planned, 0);
        var quantity = query.Quantity ?? unplanned;

        if (quantity <= 0 || quantity > unplanned)
        {
            var message = unplanned == 0
                ? $"{order.OrderNo} siparişinin tamamı zaten planlanmış."
                : $"Adet 1 ile planlanabilecek kalan adet ({unplanned}) arasında olmalıdır.";
            return new Error(
                ErrorCodes.PlanExceedsOrderQuantity, message, ErrorType.BusinessRule,
                new Dictionary<string, string[]> { ["quantity"] = [message] });
        }

        var route = (await LoadRoutesAsync([order.ModelId], cancellationToken)).GetValueOrDefault(order.ModelId);
        if (route is null)
        {
            return new Error(
                ErrorCodes.RouteRequired, $"{order.ModelCode} modelinin aktif bir operasyon rotası yok; süre hesaplanamaz.");
        }

        var (calendar, assumed) = await LoadCalendarAsync(cancellationToken);
        var lines = await db.ProductionLines
            .AsNoTracking()
            .Where(l => l.IsActive)
            .OrderBy(l => l.LineCode)
            .Select(l => new { l.Id, l.LineCode, l.Name })
            .ToListAsync(cancellationToken);
        var lineIds = lines.Select(l => l.Id).ToList();
        var machines = await LoadMachinesAsync(lineIds, cancellationToken);

        var now = clock.UtcNow;
        var busy = (await db.LineProductionPlans
                .AsNoTracking()
                .Where(p => lineIds.Contains(p.ProductionLineId) && p.Status != LinePlanStatus.Cancelled && p.PlannedEnd > now)
                .OrderBy(p => p.PlannedStart)
                .Select(p => new { p.ProductionLineId, p.PlannedStart, p.PlannedEnd })
                .ToListAsync(cancellationToken))
            .ToLookup(p => p.ProductionLineId, p => (p.PlannedStart, p.PlannedEnd));

        var deadline = DueDeadline(order.DueDate);
        var suggestions = lines
            .Select(line =>
            {
                var rate = RateOf(machines[line.Id].ToList(), route, efficiency);
                if (rate.PerHour is not > 0)
                {
                    return Infeasible(
                        rate.BottleneckOperationName is null
                            ? "Rotanın operasyon süreleri tanımlı değil."
                            : $"Bantta '{rate.BottleneckOperationName}' operasyonunu yapacak çalışır makine yok.");
                }

                var requiredMinutes = quantity / rate.PerHour.Value * 60;
                var slot = FindSlot(calendar, now, requiredMinutes, busy[line.Id].ToList());
                return slot is null
                    ? Infeasible("Çalışma takviminde bu iş için yeterli süre bulunamadı.") with
                    {
                        RatePerHour = rate.PerHour, BottleneckOperationName = rate.BottleneckOperationName,
                    }
                    : new LineSuggestionDto(
                        line.Id, line.LineCode, line.Name, true, null, rate.PerHour, rate.BottleneckOperationName,
                        Round(requiredMinutes / 60), slot.Value.Start, slot.Value.End, slot.Value.End <= deadline);

                LineSuggestionDto Infeasible(string reason) => new(
                    line.Id, line.LineCode, line.Name, false, reason, null, rate.BottleneckOperationName, null, null, null, null);
            })
            // Uygun olanlar önce; aralarında en erken biten.
            .OrderByDescending(s => s.Feasible)
            .ThenBy(s => s.SuggestedEnd ?? DateTime.MaxValue)
            .ThenBy(s => s.LineCode, StringComparer.Ordinal)
            .ToList();

        return new PlanSuggestionDto(
            order.Id, order.OrderNo, order.ModelCode, order.DueDate, quantity, unplanned, efficiency, assumed, suggestions);
    }

    /// <summary>
    /// Bantın mevcut planlarıyla çakışmayan en erken yer: iş, başladığı andan çalışma takvimiyle sürer ve
    /// bir sonraki plan başlamadan bitmelidir; bitmiyorsa o planın sonrasına bakılır.
    /// </summary>
    private (DateTime Start, DateTime End)? FindSlot(
        WorkCalendar calendar, DateTime notBefore, double requiredMinutes,
        IReadOnlyList<(DateTime Start, DateTime End)> busy)
    {
        var candidate = notBefore;
        foreach (var plan in busy.Append((Start: DateTime.MaxValue, End: DateTime.MaxValue)))
        {
            if (calendar.NextWorkingInstant(clock.ToLocal(candidate)) is not { } localStart
                || calendar.AddWorkingMinutes(localStart, requiredMinutes) is not { } localEnd)
            {
                return null;
            }

            // Dakikaya yuvarlanır: plan saatleri dakika hassasiyetinde girilir.
            var start = CeilToMinute(clock.ToUtc(localStart));
            var end = CeilToMinute(clock.ToUtc(localEnd));
            if (end <= plan.Start)
            {
                return (start, end);
            }

            if (plan.End > candidate)
            {
                candidate = plan.End;
            }
        }

        return null;
    }

    private static LineRate RateOf(
        IReadOnlyList<(int? OperationId, MachineStatus Status)> lineMachines, IReadOnlyList<RouteStep>? route, decimal efficiency)
    {
        if (route is null)
        {
            return new LineRate(false, null, null);
        }

        // Hedef, ulaşılabilir kapasiteyi etkilemez; hesap yalnızca darboğazı bulmak için çağrılır.
        var balance = LineBalancer.Calculate(
            route
                .Select(s => new BalanceInput(
                    s.Id, s.Sam, lineMachines.Count(m => m.OperationId == s.OperationId && !OutOfService.Contains(m.Status))))
                .ToList(),
            1,
            efficiency);

        return new LineRate(
            true, balance.AchievablePerHour, route.FirstOrDefault(s => s.Id == balance.BottleneckKey)?.OperationName);
    }

    private async Task<Dictionary<int, List<RouteStep>>> LoadRoutesAsync(
        IEnumerable<int> modelIds, CancellationToken cancellationToken)
    {
        var ids = modelIds.Distinct().ToList();
        var steps = await db.ProductionRouteOperations
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(o => ids.Contains(o.ProductionRoute.ModelId) && o.ProductionRoute.Status == RouteStatus.Active
                && !o.ProductionRoute.IsDeleted)
            .OrderBy(o => o.SequenceNo)
            .Select(o => new { o.ProductionRoute.ModelId, o.Id, o.OperationId, o.Operation.Name, o.Sam })
            .ToListAsync(cancellationToken);

        return steps
            .GroupBy(s => s.ModelId)
            .ToDictionary(g => g.Key, g => g.Select(s => new RouteStep(s.Id, s.OperationId, s.Name, s.Sam)).ToList());
    }

    /// <summary>Bantların şu anki aktif makineleri (operasyonu ve durumuyla).</summary>
    private async Task<ILookup<int, (int? OperationId, MachineStatus Status)>> LoadMachinesAsync(
        List<int> lineIds, CancellationToken cancellationToken) =>
        (await db.LineMachineAssignments
            .AsNoTracking()
            .Where(a => lineIds.Contains(a.ProductionLineId) && a.UnassignedAt == null && a.Machine.IsActive)
            .Select(a => new { a.ProductionLineId, a.Machine.OperationId, a.Machine.Status })
            .ToListAsync(cancellationToken))
        .ToLookup(m => m.ProductionLineId, m => (m.OperationId, m.Status));

    /// <summary>
    /// Çalışma takvimi: aktif vardiyalar ve vardiyadaki herkesin çıktığı molalar. Vardiya tanımlı değilse
    /// gün boyu çalışma varsayılır ve bu, sonuçta belirtilir.
    /// </summary>
    private async Task<(WorkCalendar Calendar, bool Assumed)> LoadCalendarAsync(CancellationToken cancellationToken)
    {
        var workingDays = (options.Value.WorkingDays.Length > 0 ? options.Value.WorkingDays : DefaultWorkingDays).ToHashSet();

        var shifts = await db.Shifts.AsNoTracking().Where(s => s.IsActive).ToListAsync(cancellationToken);
        if (shifts.Count == 0)
        {
            return (WorkCalendar.AroundTheClock(workingDays), true);
        }

        var shiftIds = shifts.Select(s => s.Id).ToList();
        var breaks = (await db.BreakSchedules
                .AsNoTracking()
                .Where(b => b.IsActive && b.BreakGroupId == null && shiftIds.Contains(b.ShiftId))
                .Select(b => new { b.ShiftId, b.StartTime, b.EndTime })
                .ToListAsync(cancellationToken))
            .ToLookup(b => b.ShiftId);

        var periods = shifts
            .Select(shift => new WorkPeriod(
                shift.StartTime,
                shift.LengthMinutes,
                breaks[shift.Id]
                    .Select(b =>
                    {
                        var start = shift.OffsetOf(b.StartTime);
                        // Vardiya bitiminde sona eren molanın bitişi vardiya uzunluğudur.
                        var end = shift.OffsetOf(b.EndTime) is var offset and > 0 ? offset : shift.LengthMinutes;
                        return (start, end - start);
                    })
                    .ToList()))
            .ToList();

        return (new WorkCalendar(periods, workingDays), false);
    }

    private double WorkingHours(WorkCalendar calendar, DateTime fromUtc, DateTime toUtc) =>
        toUtc <= fromUtc ? 0 : Round(calendar.WorkingMinutes(clock.ToLocal(fromUtc), clock.ToLocal(toUtc)) / 60);

    /// <summary>Termin gününün sonu (fabrikanın yerel saatiyle), UTC.</summary>
    private DateTime DueDeadline(DateOnly dueDate) => clock.ToUtc(dueDate.AddDays(1).ToDateTime(TimeOnly.MinValue));

    private decimal? ResolveEfficiency(decimal? requested)
    {
        var efficiency = requested ?? options.Value.DefaultEfficiencyPercent;
        return efficiency is >= 1 and <= 200 ? efficiency : null;
    }

    private static DateTime CeilToMinute(DateTime value)
    {
        var floor = new DateTime(value.Ticks - value.Ticks % TimeSpan.TicksPerMinute, value.Kind);
        return floor == value ? value : floor.AddMinutes(1);
    }

    private static double Round(double value) => Math.Round(value, 1, MidpointRounding.AwayFromZero);

    private static Error InvalidEfficiency() => Invalid("efficiencyPercent", "Verim %1 ile %200 arasında olmalıdır.");

    private static Error Invalid(string field, string message) => new(
        ErrorCodes.ValidationError, message, ErrorType.Validation, new Dictionary<string, string[]> { [field] = [message] });
}
