using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Orme.Domain.Entities;
using ERP.Orme.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Orme.Application.Features.Reports;

public sealed class OrmeMachineReportQuery
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }

    /// <summary>Makinenin günde çalıştığı saat (vardiya düzeni).</summary>
    public int DailyHours { get; set; } = 24;
}

public sealed class OrmeMachineReportQueryValidator : AbstractValidator<OrmeMachineReportQuery>
{
    public OrmeMachineReportQueryValidator()
    {
        RuleFor(x => x.From).NotEmpty().WithMessage("Başlangıç tarihi zorunludur.");
        RuleFor(x => x.To).NotEmpty().WithMessage("Bitiş tarihi zorunludur.").GreaterThanOrEqualTo(x => x.From).WithMessage("Bitiş başlangıçtan önce olamaz.");
        RuleFor(x => x.DailyHours).InclusiveBetween(1, 24).WithMessage("Günlük saat 1 ile 24 arasında olmalıdır.");
    }
}

/// <param name="LoadedDays">Dönemde makinede emir bulunan gün.</param>
/// <param name="StopMinutes">Emirli günlerdeki üretim duruşu; emirsiz günlerin duruşu sayılmaz (arıza ve planlı duruş da böyle).</param>
/// <param name="AvailableMinutes">Emirli gün × günlük saat, planlı duruş düşülmüş.</param>
/// <param name="RunningPercent">Kullanılabilir sürenin üretim duruşu ve arıza dışında kalan payı.</param>
/// <param name="EfficiencyPercent">Örülen kilonun kumaş kartındaki beklenen kg/saate göre gerektirdiği süre / kullanılabilir süre.</param>
/// <param name="MissingExpectedRate">Örülen kumaşlardan birinin kartında beklenen kg/saat yok; bu yüzden verim hesaplanmadı.</param>
public sealed record OrmeMachineReportRowDto(
    int MachineId,
    string MachineCode,
    string MachineName,
    int RollCount,
    decimal KnittedKilograms,
    int LoadedDays,
    int StopMinutes,
    int BreakdownMinutes,
    int PlannedMinutes,
    int AvailableMinutes,
    decimal? RunningPercent,
    decimal? EfficiencyPercent,
    bool MissingExpectedRate);

public sealed class OrmeOrderReportQuery : PageRequest
{
    /// <summary>Sipariş no, müşteri ya da kumaşta aranır.</summary>
    public string? Search { get; set; }

    /// <summary>Yalnızca sevki tamamlanmamış satırlar.</summary>
    public bool OpenOnly { get; set; }
}

/// <param name="PlannedKilograms">İptal edilmemiş örme emirlerinin planı.</param>
/// <param name="ShippedKilograms">Onaylı irsaliyeyle ya da Boyahane'ye gönderilen.</param>
public sealed record OrmeOrderReportRowDto(
    int OrderLineId,
    int OrderId,
    string OrderNo,
    string? CustomerName,
    string FabricCode,
    string FabricName,
    OrderOwnership Ownership,
    DateOnly? DueDate,
    decimal OrderedKilograms,
    decimal PlannedKilograms,
    decimal KnittedKilograms,
    decimal FirstQualityKilograms,
    decimal SecondQualityKilograms,
    decimal RejectedKilograms,
    decimal PackedKilograms,
    decimal ShippedKilograms,
    decimal InvoicedKilograms);

public sealed class OrmeQualityReportQuery
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
}

public sealed class OrmeQualityReportQueryValidator : AbstractValidator<OrmeQualityReportQuery>
{
    public OrmeQualityReportQueryValidator()
    {
        RuleFor(x => x.From).NotEmpty().WithMessage("Başlangıç tarihi zorunludur.");
        RuleFor(x => x.To).NotEmpty().WithMessage("Bitiş tarihi zorunludur.").GreaterThanOrEqualTo(x => x.From).WithMessage("Bitiş başlangıçtan önce olamaz.");
    }
}

/// <param name="AveragePointsPer100M2">Kontrol metresiyle ağırlıklı ortalama.</param>
/// <param name="EstimatedMeterRollCount">Puanı tahmini metreyle hesaplanan top.</param>
/// <param name="DeviationRollCount">En ya da gramajı kumaş kartından sapan top.</param>
public sealed record OrmeQualityGroupDto(
    string Code,
    string Name,
    int RollCount,
    decimal Kilograms,
    decimal FirstQualityKilograms,
    decimal SecondQualityKilograms,
    decimal RejectedKilograms,
    decimal? AveragePointsPer100M2,
    int EstimatedMeterRollCount,
    int DeviationRollCount);

public sealed record OrmeDefectSummaryDto(string Code, string Name, int Count, int Points);

public sealed record OrmeQualityReportDto(
    OrmeQualityGroupDto Total,
    IReadOnlyList<OrmeQualityGroupDto> ByFabric,
    IReadOnlyList<OrmeQualityGroupDto> ByMachine,
    IReadOnlyList<OrmeDefectSummaryDto> Defects);

public sealed class OrmeShipmentReportQuery
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
}

public sealed class OrmeShipmentReportQueryValidator : AbstractValidator<OrmeShipmentReportQuery>
{
    public const int MaxDays = 366;

    public OrmeShipmentReportQueryValidator()
    {
        RuleFor(x => x.From).NotEmpty().WithMessage("Başlangıç tarihi zorunludur.");
        RuleFor(x => x.To).NotEmpty().WithMessage("Bitiş tarihi zorunludur.").GreaterThanOrEqualTo(x => x.From).WithMessage("Bitiş başlangıçtan önce olamaz.");
        RuleFor(x => x.To)
            .Must((query, to) => to.DayNumber - query.From.DayNumber < MaxDays).WithMessage($"Dönem en fazla {MaxDays} gün olabilir.")
            .When(x => x.To >= x.From);
    }
}

/// <summary>Günün bir müşteriye (ya da Boyahane'ye) yaptığı sevk.</summary>
/// <param name="CustomerId">Boyahane satırında boş.</param>
/// <param name="ToBoyahane">Boyahane'ye gönderilen çeki listeleri; bunlar irsaliyesizdir.</param>
/// <param name="DespatchCount">Onaylı irsaliye sayısı; Boyahane satırında gönderilen çeki listesi sayısı.</param>
public sealed record OrmeDailyShipmentRowDto(int? CustomerId, string CustomerName, bool ToBoyahane, int DespatchCount, int RollCount, decimal Kilograms);

public sealed record OrmeDailyShipmentDayDto(DateOnly Date, IReadOnlyList<OrmeDailyShipmentRowDto> Rows, int DespatchCount, int RollCount, decimal Kilograms);

public sealed record OrmeDailyShipmentReportDto(IReadOnlyList<OrmeDailyShipmentDayDto> Days, int DespatchCount, int RollCount, decimal Kilograms);

public sealed class OrmeIdleStockQuery
{
    /// <summary>Bu kadar gündür hareketi olmayan stok listelenir.</summary>
    public int Days { get; set; } = 90;
}

public sealed class OrmeIdleStockQueryValidator : AbstractValidator<OrmeIdleStockQuery>
{
    public OrmeIdleStockQueryValidator()
    {
        RuleFor(x => x.Days).InclusiveBetween(1, 3650).WithMessage("Gün 1 ile 3650 arasında olmalıdır.");
    }
}

/// <param name="CustomerName">Malın sahibi; boşsa işletme malı.</param>
/// <param name="LastMovementOn">Son verme, geri alma ya da iade; hiç hareket yoksa giriş tarihi.</param>
public sealed record OrmeIdleYarnLotDto(
    int Id, string LotNo, string YarnCode, string YarnName, string? YarnColor, string? CustomerName, decimal RemainingKilograms, DateOnly LastMovementOn, int IdleDays);

/// <param name="LastChangeOn">Kontrol tarihi; kontrol edilmediyse kesim tarihi.</param>
public sealed record OrmeIdleRollDto(
    int Id, string RollNo, int KnitOrderId, string KnitOrderNo, string OrderNo, string? CustomerName, string FabricCode, string FabricName,
    decimal Kilograms, FabricRollStatus Status, DateOnly LastChangeOn, int IdleDays);

/// <param name="Since">Bu tarihten sonra hareketi olan stok listeye girmez.</param>
/// <param name="RollsTruncated">Top listesi en eski <see cref="OrmeReportService.MaxIdleRolls"/> topla sınırlandı; sayı ve kilo bütün topları kapsar.</param>
public sealed record OrmeIdleStockReportDto(
    int Days,
    DateOnly Since,
    IReadOnlyList<OrmeIdleYarnLotDto> YarnLots,
    decimal YarnKilograms,
    IReadOnlyList<OrmeIdleRollDto> Rolls,
    int RollCount,
    decimal RollKilograms,
    bool RollsTruncated);

public interface IOrmeReportService
{
    Task<IReadOnlyList<OrmeMachineReportRowDto>> GetMachinesAsync(OrmeMachineReportQuery query, CancellationToken cancellationToken);
    Task<PagedResult<OrmeOrderReportRowDto>> GetOrdersAsync(OrmeOrderReportQuery query, CancellationToken cancellationToken);
    Task<OrmeQualityReportDto> GetQualityAsync(OrmeQualityReportQuery query, CancellationToken cancellationToken);
    Task<OrmeDailyShipmentReportDto> GetDailyShipmentsAsync(OrmeShipmentReportQuery query, CancellationToken cancellationToken);
    Task<OrmeIdleStockReportDto> GetIdleStockAsync(OrmeIdleStockQuery query, CancellationToken cancellationToken);
}

public sealed class OrmeReportService(IAppDbContext db, TimeProvider timeProvider) : IOrmeReportService
{
    /// <summary>Hareketsiz top listesinde gösterilen en fazla satır (en eskiler).</summary>
    public const int MaxIdleRolls = 2000;

    public async Task<IReadOnlyList<OrmeMachineReportRowDto>> GetMachinesAsync(OrmeMachineReportQuery query, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var zone = timeProvider.LocalTimeZone;
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(now, zone));
        var periodStart = LocalDayStartUtc(query.From, zone);
        var periodEnd = LocalDayStartUtc(query.To.AddDays(1), zone);

        var machines = await db.Set<OrmeMachine>().AsNoTracking().OrderBy(m => m.Code).Select(m => new { m.Id, m.Code, m.Name, m.IsActive }).ToListAsync(cancellationToken);
        var rolls = await db.Set<OrmeFabricRoll>().AsNoTracking()
            .Where(r => r.CutOn >= query.From && r.CutOn <= query.To)
            .Select(r => new { r.MachineId, r.Kilograms, r.KnitOrder.OrderLine.Fabric.ExpectedKilogramsPerHour })
            .ToListAsync(cancellationToken);
        var assignments = await db.Set<OrmeKnitOrderMachine>().AsNoTracking()
            .Where(a => a.AssignedOn <= query.To && (a.ReleasedOn == null || a.ReleasedOn >= query.From))
            .Select(a => new { a.MachineId, a.AssignedOn, a.ReleasedOn })
            .ToListAsync(cancellationToken);
        var stops = await db.Set<OrmeMachineStop>().AsNoTracking()
            .Where(s => s.StartedAt < periodEnd && (s.EndedAt == null || s.EndedAt > periodStart))
            .Select(s => new { s.MachineId, s.Category, s.StartedAt, s.EndedAt })
            .ToListAsync(cancellationToken);

        var rows = new List<OrmeMachineReportRowDto>();
        foreach (var machine in machines)
        {
            var machineRolls = rolls.Where(r => r.MachineId == machine.Id).ToList();
            var loadedDays = new HashSet<DateOnly>();
            foreach (var assignment in assignments.Where(a => a.MachineId == machine.Id))
            {
                var first = assignment.AssignedOn > query.From ? assignment.AssignedOn : query.From;
                var last = Earliest(assignment.ReleasedOn ?? today, query.To, today);
                for (var day = first; day <= last; day = day.AddDays(1))
                {
                    loadedDays.Add(day);
                }
            }

            // Duruşlar yalnızca emirli günlerde sayılır, o günlere kırpılır: kullanılabilir süreyle aynı tabanda kalır.
            var loadedWindows = loadedDays
                .Select(day => (Start: LocalDayStartUtc(day, zone), End: LocalDayStartUtc(day.AddDays(1), zone)))
                .ToList();
            var minutes = new Dictionary<OrmeStopCategory, double>();
            foreach (var stop in stops.Where(s => s.MachineId == machine.Id))
            {
                var stopEnd = stop.EndedAt ?? now;
                foreach (var window in loadedWindows)
                {
                    var start = stop.StartedAt > window.Start ? stop.StartedAt : window.Start;
                    var end = stopEnd < window.End ? stopEnd : window.End;
                    if (end > start)
                    {
                        minutes[stop.Category] = minutes.GetValueOrDefault(stop.Category) + (end - start).TotalMinutes;
                    }
                }
            }

            var stopMinutes = Whole(minutes.GetValueOrDefault(OrmeStopCategory.Stop));
            var breakdownMinutes = Whole(minutes.GetValueOrDefault(OrmeStopCategory.Breakdown));
            var plannedMinutes = Whole(minutes.GetValueOrDefault(OrmeStopCategory.Planned));
            if (!machine.IsActive && machineRolls.Count == 0 && loadedDays.Count == 0 && minutes.Count == 0)
            {
                continue;
            }

            // Süre olmadan oran anlamsızdır; saat planı dışındaki duruşlar yüzünden oran eksiye düşmesin.
            var available = Math.Max(0, loadedDays.Count * query.DailyHours * 60 - plannedMinutes);
            decimal? running = available == 0 ? null : Percent(Math.Max(0, available - stopMinutes - breakdownMinutes), available);

            // Beklenen hızı bilinmeyen kumaş örüldüyse oran eksik sayılır, yanıltıcı bir düşük verim gösterilmez.
            var missingRate = machineRolls.Any(r => r.ExpectedKilogramsPerHour is not > 0);
            decimal? efficiency = available == 0 || missingRate
                ? null
                : Percent(machineRolls.Sum(r => r.Kilograms / r.ExpectedKilogramsPerHour!.Value * 60m), available);

            rows.Add(new OrmeMachineReportRowDto(
                machine.Id, machine.Code, machine.Name, machineRolls.Count, machineRolls.Sum(r => r.Kilograms), loadedDays.Count,
                stopMinutes, breakdownMinutes, plannedMinutes, available, running, efficiency, missingRate));
        }

        return rows;
    }

    public Task<PagedResult<OrmeOrderReportRowDto>> GetOrdersAsync(OrmeOrderReportQuery query, CancellationToken cancellationToken)
    {
        var lines = db.Set<OrmeOrderLine>().AsNoTracking().Where(l => l.Order.Status == OrderStatus.Confirmed);
        if (NullIfBlank(query.Search) is { } term)
        {
            lines = lines.Where(l => l.Order.OrderNo.Contains(term) || l.Fabric.FabricCode.Contains(term) || l.Fabric.Name.Contains(term)
                || (l.Order.Customer != null && l.Order.Customer.Name.Contains(term)));
        }

        var rolls = db.Set<OrmeFabricRoll>();
        var packed = db.Set<OrmePackingItem>().Where(i => i.IsActive && i.PackingList.Status == PackingListStatus.Confirmed);
        if (query.OpenOnly)
        {
            lines = lines.Where(l => (packed.Where(i => i.Roll.KnitOrder.OrderLineId == l.Id
                && (i.PackingList.BoyahaneReceiptNo != null || (i.PackingList.Despatch != null && i.PackingList.Despatch.Status == DespatchStatus.Confirmed)))
                .Sum(i => (decimal?)i.Kilograms) ?? 0) < l.Kilograms);
        }

        return lines
            .OrderByDescending(l => l.OrderId).ThenBy(l => l.Id)
            .Select(l => new OrmeOrderReportRowDto(
                l.Id,
                l.OrderId,
                l.Order.OrderNo,
                l.Order.Customer != null ? l.Order.Customer.Name : null,
                l.Fabric.FabricCode,
                l.Fabric.Name,
                l.Order.Ownership,
                l.Order.DueDate,
                l.Kilograms,
                db.Set<OrmeKnitOrder>().Where(k => k.OrderLineId == l.Id && k.Status != KnitOrderStatus.Cancelled).Sum(k => (decimal?)k.PlannedKilograms) ?? 0,
                rolls.Where(r => r.KnitOrder.OrderLineId == l.Id).Sum(r => (decimal?)r.Kilograms) ?? 0,
                rolls.Where(r => r.KnitOrder.OrderLineId == l.Id && r.Status == FabricRollStatus.FirstQuality).Sum(r => (decimal?)r.Kilograms) ?? 0,
                rolls.Where(r => r.KnitOrder.OrderLineId == l.Id && r.Status == FabricRollStatus.SecondQuality).Sum(r => (decimal?)r.Kilograms) ?? 0,
                rolls.Where(r => r.KnitOrder.OrderLineId == l.Id && r.Status == FabricRollStatus.Rejected).Sum(r => (decimal?)r.Kilograms) ?? 0,
                packed.Where(i => i.Roll.KnitOrder.OrderLineId == l.Id).Sum(i => (decimal?)i.Kilograms) ?? 0,
                packed.Where(i => i.Roll.KnitOrder.OrderLineId == l.Id
                    && (i.PackingList.BoyahaneReceiptNo != null || (i.PackingList.Despatch != null && i.PackingList.Despatch.Status == DespatchStatus.Confirmed)))
                    .Sum(i => (decimal?)i.Kilograms) ?? 0,
                db.Set<OrmeInvoiceLine>().Where(i => i.OrderLineId == l.Id && i.Invoice.Status == InvoiceStatus.Confirmed).Sum(i => (decimal?)i.Quantity) ?? 0))
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<OrmeQualityReportDto> GetQualityAsync(OrmeQualityReportQuery query, CancellationToken cancellationToken)
    {
        var inRange = db.Set<OrmeFabricRoll>().AsNoTracking().Where(r => r.InspectedOn >= query.From && r.InspectedOn <= query.To && r.Status != FabricRollStatus.PendingInspection);
        var rolls = await inRange
            .Select(r => new RollRow(
                r.Status,
                r.Kilograms,
                r.InspectedMeters ?? 0,
                r.InspectedMetersEstimated,
                r.SpecDeviation != null,
                r.PointsPer100M2,
                r.KnitOrder.OrderLine.Fabric.FabricCode,
                r.KnitOrder.OrderLine.Fabric.Name,
                r.Machine.Code,
                r.Machine.Name))
            .ToListAsync(cancellationToken);
        var defects = await db.Set<OrmeRollDefect>().AsNoTracking()
            .Where(d => d.Roll.InspectedOn >= query.From && d.Roll.InspectedOn <= query.To && d.Roll.Status != FabricRollStatus.PendingInspection)
            .GroupBy(d => new { d.DefectType.Code, d.DefectType.Name })
            .Select(g => new { g.Key.Code, g.Key.Name, Count = g.Count(), Points = g.Sum(d => d.Points) })
            .ToListAsync(cancellationToken);

        return new OrmeQualityReportDto(
            Group(string.Empty, string.Empty, rolls),
            rolls.GroupBy(r => (r.FabricCode, r.FabricName)).OrderByDescending(g => g.Sum(r => r.Kilograms)).Select(g => Group(g.Key.FabricCode, g.Key.FabricName, g.ToList())).ToList(),
            rolls.GroupBy(r => (r.MachineCode, r.MachineName)).OrderBy(g => g.Key.MachineCode).Select(g => Group(g.Key.MachineCode, g.Key.MachineName, g.ToList())).ToList(),
            defects.OrderByDescending(d => d.Points).ThenBy(d => d.Code).Select(d => new OrmeDefectSummaryDto(d.Code, d.Name, d.Count, d.Points)).ToList());
    }

    /// <summary>
    /// Günlük sevk: onaylı irsaliyeler irsaliye tarihine göre müşteri müşteri; Boyahane'ye gönderilen çeki listeleri
    /// gönderim tarihine göre ayrı bir "Boyahane'ye" satırında.
    /// </summary>
    public async Task<OrmeDailyShipmentReportDto> GetDailyShipmentsAsync(OrmeShipmentReportQuery query, CancellationToken cancellationToken)
    {
        // Silinmiş müşteriye yapılan sevk de sayılır.
        var despatches = await db.Set<OrmeDespatch>().AsNoTracking().IgnoreQueryFilters()
            .Where(d => d.Status == DespatchStatus.Confirmed && d.DespatchDate >= query.From && d.DespatchDate <= query.To)
            .Select(d => new
            {
                Date = d.DespatchDate,
                d.CustomerId,
                CustomerName = d.Customer.Name,
                Rolls = d.PackingLists.SelectMany(l => l.Items).Count(i => i.IsActive),
                Kilograms = d.PackingLists.SelectMany(l => l.Items).Where(i => i.IsActive).Sum(i => (decimal?)i.Kilograms) ?? 0,
            })
            .ToListAsync(cancellationToken);
        var toBoyahane = await db.Set<OrmePackingList>().AsNoTracking()
            .Where(l => l.BoyahaneSentOn != null && l.BoyahaneSentOn >= query.From && l.BoyahaneSentOn <= query.To)
            .Select(l => new
            {
                Date = l.BoyahaneSentOn!.Value,
                Rolls = l.Items.Count(i => i.IsActive),
                Kilograms = l.Items.Where(i => i.IsActive).Sum(i => (decimal?)i.Kilograms) ?? 0,
            })
            .ToListAsync(cancellationToken);

        var rows = despatches
            .GroupBy(d => (d.Date, d.CustomerId, d.CustomerName))
            .Select(g => (g.Key.Date, Row: new OrmeDailyShipmentRowDto(g.Key.CustomerId, g.Key.CustomerName, false, g.Count(), g.Sum(d => d.Rolls), g.Sum(d => d.Kilograms))))
            .Concat(toBoyahane
                .GroupBy(l => l.Date)
                .Select(g => (Date: g.Key, Row: new OrmeDailyShipmentRowDto(null, "Boyahane'ye", true, g.Count(), g.Sum(l => l.Rolls), g.Sum(l => l.Kilograms)))))
            .ToList();

        var days = rows
            .GroupBy(r => r.Date)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                // Müşteriler adına göre, Boyahane satırı en sonda.
                var dayRows = g.Select(r => r.Row).OrderBy(r => r.ToBoyahane).ThenBy(r => r.CustomerName).ToList();
                return new OrmeDailyShipmentDayDto(g.Key, dayRows, dayRows.Sum(r => r.DespatchCount), dayRows.Sum(r => r.RollCount), dayRows.Sum(r => r.Kilograms));
            })
            .ToList();

        return new OrmeDailyShipmentReportDto(days, days.Sum(d => d.DespatchCount), days.Sum(d => d.RollCount), days.Sum(d => d.Kilograms));
    }

    /// <summary>
    /// Hareketsiz stok. İplik: kalanı olan, son <c>Days</c> gündür verme, geri alma ya da iade hareketi olmayan lotlar (yeni girmiş lot
    /// giriş tarihinden sayılır). Ham top: hiçbir etkin çeki listesinde olmayan (sevk edilmemiş), kontrol ya da kesim tarihi eski toplar.
    /// </summary>
    public async Task<OrmeIdleStockReportDto> GetIdleStockAsync(OrmeIdleStockQuery query, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(timeProvider.GetUtcNow().UtcDateTime, timeProvider.LocalTimeZone));
        var since = today.AddDays(-query.Days);

        // Kartı silinmiş iplik ya da müşterinin lotu da depoda durur; filtre uygulanmaz.
        var lots = await db.Set<OrmeYarnLot>().AsNoTracking().IgnoreQueryFilters()
            .Select(l => new
            {
                l.Id,
                l.LotNo,
                l.Yarn.YarnCode,
                YarnName = l.Yarn.Name,
                l.Yarn.Color,
                CustomerName = l.Customer != null ? l.Customer.Name : null,
                l.ReceiptDate,
                Remaining = l.Kilograms - (l.Issues.Sum(i => (decimal?)i.Kilograms) ?? 0) + (l.TakeBacks.Sum(t => (decimal?)t.Kilograms) ?? 0) - (l.Returns.Sum(r => (decimal?)r.Kilograms) ?? 0),
                LastIssue = l.Issues.Max(i => (DateOnly?)i.IssuedOn),
                LastTakeBack = l.TakeBacks.Max(t => (DateOnly?)t.TakenOn),
                LastReturn = l.Returns.Max(r => (DateOnly?)r.ReturnDate),
            })
            .Where(l => l.Remaining > 0
                && l.ReceiptDate <= since
                && (l.LastIssue == null || l.LastIssue <= since)
                && (l.LastTakeBack == null || l.LastTakeBack <= since)
                && (l.LastReturn == null || l.LastReturn <= since))
            .ToListAsync(cancellationToken);

        var yarnLots = lots
            .Select(l =>
            {
                var last = new[] { l.ReceiptDate, l.LastIssue ?? DateOnly.MinValue, l.LastTakeBack ?? DateOnly.MinValue, l.LastReturn ?? DateOnly.MinValue }.Max();
                return new OrmeIdleYarnLotDto(l.Id, l.LotNo, l.YarnCode, l.YarnName, l.Color, l.CustomerName, l.Remaining, last, today.DayNumber - last.DayNumber);
            })
            .OrderByDescending(l => l.IdleDays).ThenBy(l => l.LotNo)
            .ToList();

        var idleRolls = db.Set<OrmeFabricRoll>().AsNoTracking().IgnoreQueryFilters()
            .Where(r => (r.InspectedOn ?? r.CutOn) <= since
                && !db.Set<OrmePackingItem>().Any(i => i.RollId == r.Id && i.IsActive));
        var rollCount = await idleRolls.CountAsync(cancellationToken);
        var rollKilograms = await idleRolls.SumAsync(r => (decimal?)r.Kilograms, cancellationToken) ?? 0;
        var rolls = await idleRolls
            .OrderBy(r => r.InspectedOn ?? r.CutOn).ThenBy(r => r.RollNo)
            .Take(MaxIdleRolls)
            .Select(r => new
            {
                r.Id,
                r.RollNo,
                r.KnitOrderId,
                r.KnitOrder.KnitOrderNo,
                r.KnitOrder.OrderLine.Order.OrderNo,
                CustomerName = r.KnitOrder.OrderLine.Order.Customer != null ? r.KnitOrder.OrderLine.Order.Customer.Name : null,
                r.KnitOrder.OrderLine.Fabric.FabricCode,
                FabricName = r.KnitOrder.OrderLine.Fabric.Name,
                r.Kilograms,
                r.Status,
                LastChangeOn = r.InspectedOn ?? r.CutOn,
            })
            .ToListAsync(cancellationToken);

        return new OrmeIdleStockReportDto(
            query.Days,
            since,
            yarnLots,
            yarnLots.Sum(l => l.RemainingKilograms),
            rolls.Select(r => new OrmeIdleRollDto(
                r.Id, r.RollNo, r.KnitOrderId, r.KnitOrderNo, r.OrderNo, r.CustomerName, r.FabricCode, r.FabricName, r.Kilograms, r.Status,
                r.LastChangeOn, today.DayNumber - r.LastChangeOn.DayNumber)).ToList(),
            rollCount,
            rollKilograms,
            rollCount > rolls.Count);
    }

    private sealed record RollRow(
        FabricRollStatus Status, decimal Kilograms, decimal Meters, bool MetersEstimated, bool HasDeviation, decimal? Points, string FabricCode, string FabricName, string MachineCode, string MachineName);

    private static OrmeQualityGroupDto Group(string code, string name, IReadOnlyCollection<RollRow> rolls)
    {
        var scored = rolls.Where(r => r.Points is not null).ToList();
        var scoredMeters = scored.Sum(r => r.Meters);
        return new OrmeQualityGroupDto(
            code,
            name,
            rolls.Count,
            rolls.Sum(r => r.Kilograms),
            rolls.Where(r => r.Status == FabricRollStatus.FirstQuality).Sum(r => r.Kilograms),
            rolls.Where(r => r.Status == FabricRollStatus.SecondQuality).Sum(r => r.Kilograms),
            rolls.Where(r => r.Status == FabricRollStatus.Rejected).Sum(r => r.Kilograms),
            scoredMeters == 0 ? null : Math.Round(scored.Sum(r => r.Points!.Value * r.Meters) / scoredMeters, 2, MidpointRounding.AwayFromZero),
            rolls.Count(r => r.MetersEstimated),
            rolls.Count(r => r.HasDeviation));
    }

    private static DateTime LocalDayStartUtc(DateOnly day, TimeZoneInfo zone) => TimeZoneInfo.ConvertTimeToUtc(day.ToDateTime(TimeOnly.MinValue), zone);

    private static DateOnly Earliest(DateOnly a, DateOnly b, DateOnly c)
    {
        var first = a < b ? a : b;
        return first < c ? first : c;
    }

    private static int Whole(double minutes) => (int)Math.Round(minutes, MidpointRounding.AwayFromZero);

    private static decimal Percent(decimal part, decimal whole) => Math.Round(part * 100 / whole, 1, MidpointRounding.AwayFromZero);
}
