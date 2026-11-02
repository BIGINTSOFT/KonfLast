using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Orme.Application.Features.Knitting;
using ERP.Orme.Domain.Entities;
using ERP.Orme.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ERP.Orme.Application.Features.Planning;

public sealed class OrmeCapacityPlanQuery
{
    /// <summary>Zaman çizelgesinde gösterilen gün sayısı.</summary>
    public int Days { get; set; } = 30;

    /// <summary>Makinenin günde çalıştığı saat; boşsa vardiyalardan, vardiya yoksa varsayılandan (22,5 saat) alınır.</summary>
    public decimal? HoursPerDay { get; set; }
}

public sealed class OrmeCapacityPlanQueryValidator : AbstractValidator<OrmeCapacityPlanQuery>
{
    public OrmeCapacityPlanQueryValidator()
    {
        RuleFor(x => x.Days).InclusiveBetween(7, 90).WithMessage("Gün sayısı 7 ile 90 arasında olmalıdır.");
        RuleFor(x => x.HoursPerDay).InclusiveBetween(1, 24).WithMessage("Günlük saat 1 ile 24 arasında olmalıdır.").When(x => x.HoursPerDay is not null);
    }
}

/// <summary>Makine kuyruğundaki emir ve tahmini zamanları. Saatler UTC'dir; ofsetler bugünün başından itibaren gün cinsindendir.</summary>
/// <param name="Running">Emir şu anda bu makinede çalışıyor (kuyruğun başı).</param>
/// <param name="Sequence">Kuyruktaki sıra; çalışan emirde boş.</param>
/// <param name="RunningMachineCount">Emrin aynı anda çalıştığı makine sayısı; kalan kilo makinelere eşit bölünür.</param>
/// <param name="RemainingKilograms">Emrin plandan kalanı (bütün makineler).</param>
/// <param name="MachineKilograms">Bu makinede örülmesi beklenen: kalan ÷ çalıştığı makine sayısı.</param>
/// <param name="KilogramsPerHour">Kumaş kartındaki beklenen üretim; boşsa süre hesaplanamaz.</param>
/// <param name="DurationHours">Makine kilosu ÷ beklenen kg/saat (üretim saati).</param>
/// <param name="EstimatedFinishDate">Tahmini bitişin yerel günü.</param>
/// <param name="StartOffsetDays">Başlangıcın bugünün 00:00'ından uzaklığı (gün).</param>
/// <param name="EndOffsetDays">Bitişin bugünün 00:00'ından uzaklığı (gün).</param>
/// <param name="PlannedDate">Emrin plan tarihi; emir bu günden önce başlamaz.</param>
/// <param name="DueDate">Siparişin termini.</param>
/// <param name="LateRisk">Tahmini bitiş siparişin termininden sonra.</param>
/// <param name="FitWarning">Çap ya da fine kumaş kartıyla uyuşmuyorsa uyarı.</param>
public sealed record OrmeCapacityItemDto(
    int KnitOrderId,
    string KnitOrderNo,
    int OrderId,
    string OrderNo,
    string? CustomerName,
    string FabricCode,
    string FabricName,
    bool Running,
    int? Sequence,
    int RunningMachineCount,
    decimal PlannedKilograms,
    decimal KnittedKilograms,
    decimal RemainingKilograms,
    decimal MachineKilograms,
    decimal? KilogramsPerHour,
    decimal? DurationHours,
    DateTime EstimatedStart,
    DateTime? EstimatedFinish,
    DateOnly? EstimatedFinishDate,
    double StartOffsetDays,
    double? EndOffsetDays,
    DateOnly PlannedDate,
    DateOnly? DueDate,
    bool LateRisk,
    int? LateDays,
    string? FitWarning);

/// <param name="QueueHours">Kuyruktaki işlerin toplam üretim saati (hızı bilinenler).</param>
/// <param name="QueueEndsAt">Kuyruğun tahmini bitişi (UTC); kuyruk boşsa boş.</param>
/// <param name="QueueDays">Şimdiden kuyruğun bitişine kadar geçen gün.</param>
/// <param name="Load7Percent">Önümüzdeki 7 günün dolu payı.</param>
/// <param name="MissingRateCount">Kumaş kartında beklenen kg/saat olmadığı için süresi hesaplanamayan emir.</param>
public sealed record OrmeCapacityMachineDto(
    int MachineId,
    string MachineCode,
    string MachineName,
    KnitBedType BedType,
    int DiameterInch,
    int Gauge,
    bool IsActive,
    OrmeStopCategory? OpenStopCategory,
    string? OpenStopReason,
    decimal QueueHours,
    DateTime? QueueEndsAt,
    double QueueDays,
    decimal Load7Percent,
    decimal Load14Percent,
    decimal Load30Percent,
    int LateRiskCount,
    int MissingRateCount,
    IReadOnlyList<OrmeCapacityItemDto> Items);

/// <summary>Makinede çalışmayan ve kuyruğa alınmamış açık emir.</summary>
/// <param name="Note">Planlanan makine pasife alındıysa açıklama.</param>
public sealed record OrmeUnplannedKnitOrderDto(
    int KnitOrderId,
    string KnitOrderNo,
    int OrderId,
    string OrderNo,
    string? CustomerName,
    string FabricCode,
    string FabricName,
    KnitBedType BedType,
    int? DiameterInch,
    int? Gauge,
    DateOnly PlannedDate,
    DateOnly? DueDate,
    decimal RemainingKilograms,
    decimal? KilogramsPerHour,
    decimal? DurationHours,
    string? Note);

/// <param name="GeneratedAt">Planın hesaplandığı an (UTC); tahminler bu andan başlar.</param>
/// <param name="HoursPerDay">Hesapta kullanılan günlük çalışma saati.</param>
/// <param name="HoursSource">Günlük saatin nereden geldiği.</param>
public sealed record OrmeCapacityPlanDto(
    DateTime GeneratedAt,
    DateOnly Today,
    int Days,
    decimal HoursPerDay,
    string HoursSource,
    IReadOnlyList<OrmeCapacityMachineDto> Machines,
    IReadOnlyList<OrmeUnplannedKnitOrderDto> Unplanned);

/// <summary>Emri taşımak için uygun makine: aynı plaka, etkin.</summary>
/// <param name="Current">Emir şu anda bu makinenin kuyruğunda.</param>
/// <param name="QueueCount">Bu emir dışında kuyruktaki emir sayısı (çalışan dahil).</param>
/// <param name="QueueEndsAt">Kuyruğun tahmini bitişi (UTC).</param>
public sealed record OrmeCapacityMachineOptionDto(
    int MachineId,
    string Code,
    string Name,
    int DiameterInch,
    int Gauge,
    string? FitWarning,
    bool Current,
    int QueueCount,
    DateTime? QueueEndsAt,
    OrmeStopCategory? OpenStopCategory);

/// <param name="MachineId">Kuyruğuna alınacak makine; boşsa emir plandan çıkarılır.</param>
/// <param name="PlannedDate">Verilirse emrin plan tarihi (en erken başlangıç) değişir.</param>
/// <param name="Position">Kuyruktaki yeri (1'den); boşsa sona eklenir.</param>
public sealed record OrmePlanKnitOrderRequest(int? MachineId, DateOnly? PlannedDate = null, int? Position = null);

/// <param name="KnitOrderIds">Kuyruğun yeni sırası; listede olmayan emirler eski sırasıyla sona eklenir.</param>
public sealed record OrmeMachineQueueRequest(IReadOnlyList<int> KnitOrderIds);

public sealed class OrmePlanKnitOrderRequestValidator : AbstractValidator<OrmePlanKnitOrderRequest>
{
    public OrmePlanKnitOrderRequestValidator()
    {
        RuleFor(x => x.MachineId).GreaterThan(0).WithMessage("Makine geçersiz.").When(x => x.MachineId is not null);
        RuleFor(x => x.Position).InclusiveBetween(1, 10_000).WithMessage("Sıra 1'den başlar.").When(x => x.Position is not null);
    }
}

public sealed class OrmeMachineQueueRequestValidator : AbstractValidator<OrmeMachineQueueRequest>
{
    public OrmeMachineQueueRequestValidator()
    {
        RuleFor(x => x.KnitOrderIds)
            .NotEmpty().WithMessage("Sıralanacak emir gönderilmedi.")
            .Must(ids => ids is null || ids.Count <= 1000).WithMessage("Bir kuyrukta en fazla 1000 emir sıralanır.");
    }
}

public interface IOrmeCapacityPlanService
{
    Task<OrmeCapacityPlanDto> GetAsync(OrmeCapacityPlanQuery query, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<OrmeCapacityMachineOptionDto>>> GetOptionsAsync(int knitOrderId, CancellationToken cancellationToken);
    Task<Result> PlanAsync(int knitOrderId, OrmePlanKnitOrderRequest request, CancellationToken cancellationToken);
    Task<Result> ReorderAsync(int machineId, OrmeMachineQueueRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// İleriye dönük makine kapasite planı. Her makinede önce çalışan emir, sonra kuyruğa alınmış açık emirler sırasıyla dizilir.
/// Süre = makinede örülecek kilo ÷ kumaş kartındaki beklenen kg/saat; birden çok makinede çalışan emrin kalanı makinelere eşit bölünür.
/// Üretim saati, günlük çalışma saatiyle takvime yayılır: günlük saat aktif vardiyaların kapsadığı süreden, bütün vardiyanın çıktığı
/// (grupsuz) molalar düşülerek hesaplanır; gruplu molalarda makine dönüşümlü çalıştığı için düşülmez. Vardiya tanımı yoksa 22,5 saat.
/// </summary>
public sealed class OrmeCapacityPlanService(IAppDbContext db, IAppLock appLock, TimeProvider timeProvider) : IOrmeCapacityPlanService
{
    /// <summary>Vardiya tanımı yoksa kullanılan günlük çalışma saati (3 × 8 saat − 1,5 saat mola).</summary>
    public const decimal DefaultHoursPerDay = 22.5m;

    private const string PlanLock = "OrmeCapacityPlan";
    private const string InvalidState = "ORME_PLAN_INVALID_STATE";
    private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(10);

    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Örme emri bulunamadı.", ErrorType.NotFound);
    private static readonly Error Busy = new("ORME_BUSY", "Aynı anda başka bir plan işlemi yapılıyor; tekrar deneyin.", ErrorType.Conflict);

    public async Task<OrmeCapacityPlanDto> GetAsync(OrmeCapacityPlanQuery query, CancellationToken cancellationToken)
    {
        var (hoursPerDay, hoursSource) = await DailyHoursAsync(query.HoursPerDay, cancellationToken);
        var zone = timeProvider.LocalTimeZone;
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var now = DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(nowUtc, zone), DateTimeKind.Unspecified);
        var today = DateOnly.FromDateTime(now);
        var dayStart = today.ToDateTime(TimeOnly.MinValue);

        var knits = await db.Set<OrmeKnitOrder>().AsNoTracking()
            .Where(k => k.Status == KnitOrderStatus.Open)
            .Select(k => new KnitRow(
                k.Id,
                k.KnitOrderNo,
                k.OrderLine.OrderId,
                k.OrderLine.Order.OrderNo,
                k.OrderLine.Order.Customer != null ? k.OrderLine.Order.Customer.Name : null,
                k.OrderLine.Fabric.FabricCode,
                k.OrderLine.Fabric.Name,
                k.OrderLine.Fabric.BedType,
                k.OrderLine.Fabric.DiameterInch,
                k.OrderLine.Fabric.Gauge,
                k.OrderLine.Fabric.ExpectedKilogramsPerHour,
                k.PlannedDate,
                k.OrderLine.Order.DueDate,
                k.PlannedKilograms,
                k.Rolls.Sum(r => (decimal?)r.Kilograms) ?? 0,
                k.PlannedMachineId,
                k.PlanSequence))
            .ToListAsync(cancellationToken);
        var knitById = knits.ToDictionary(k => k.Id);

        var running = await db.Set<OrmeKnitOrderMachine>().AsNoTracking()
            .Where(m => m.ReleasedOn == null && m.KnitOrder.Status == KnitOrderStatus.Open)
            .Select(m => new { m.MachineId, m.KnitOrderId })
            .ToListAsync(cancellationToken);
        var runningByMachine = running.GroupBy(r => r.MachineId).ToDictionary(g => g.Key, g => g.First().KnitOrderId);
        var runningCount = running.GroupBy(r => r.KnitOrderId).ToDictionary(g => g.Key, g => g.Count());
        var runningMachineIds = runningByMachine.Keys.ToList();

        // Pasife alınmış makine üzerinde emir çalışıyorsa planda görünmeye devam eder.
        var machines = await db.Set<OrmeMachine>().AsNoTracking()
            .Where(m => m.IsActive || runningMachineIds.Contains(m.Id))
            .OrderBy(m => m.Code)
            .ToListAsync(cancellationToken);
        var stops = (await db.Set<OrmeMachineStop>().AsNoTracking()
                .Where(s => s.EndedAt == null)
                .Select(s => new { s.MachineId, s.Category, ReasonName = s.Reason.Name, s.StartedAt })
                .ToListAsync(cancellationToken))
            .GroupBy(s => s.MachineId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(s => s.StartedAt).First());

        var activeMachineIds = machines.Where(m => m.IsActive).Select(m => m.Id).ToHashSet();
        var result = new List<OrmeCapacityMachineDto>();
        foreach (var machine in machines)
        {
            var cursor = now;
            var items = new List<OrmeCapacityItemDto>();

            OrmeCapacityItemDto Schedule(KnitRow knit, bool isRunning, int? sequence, int machineCount)
            {
                var remaining = Math.Max(0, knit.PlannedKilograms - knit.KnittedKilograms);
                var share = Round(remaining / machineCount);
                var notBefore = knit.PlannedDate.ToDateTime(TimeOnly.MinValue);
                var start = isRunning || notBefore <= cursor ? cursor : notBefore;
                var rate = knit.KilogramsPerHour is > 0 ? knit.KilogramsPerHour : null;
                decimal? hours = rate is { } perHour ? Round(share / perHour) : null;
                DateTime? finish = hours is { } h ? start.AddDays((double)(h / hoursPerDay)) : null;
                cursor = finish ?? start;

                var finishDate = finish is { } f ? DateOnly.FromDateTime(f) : (DateOnly?)null;
                var late = finishDate is { } fd && knit.DueDate is { } due && fd > due;
                return new OrmeCapacityItemDto(
                    knit.Id,
                    knit.KnitOrderNo,
                    knit.OrderId,
                    knit.OrderNo,
                    knit.CustomerName,
                    knit.FabricCode,
                    knit.FabricName,
                    isRunning,
                    sequence,
                    machineCount,
                    knit.PlannedKilograms,
                    knit.KnittedKilograms,
                    remaining,
                    share,
                    rate,
                    hours,
                    ToUtc(start, zone),
                    finish is { } end ? ToUtc(end, zone) : null,
                    finishDate,
                    (start - dayStart).TotalDays,
                    finish is { } endAt ? (endAt - dayStart).TotalDays : null,
                    knit.PlannedDate,
                    knit.DueDate,
                    late,
                    late ? finishDate!.Value.DayNumber - knit.DueDate!.Value.DayNumber : null,
                    OrmeKnitOrderService.FitWarning(knit.DiameterInch, knit.Gauge, machine));
            }

            if (runningByMachine.TryGetValue(machine.Id, out var runningId) && knitById.TryGetValue(runningId, out var current))
            {
                items.Add(Schedule(current, true, null, runningCount.GetValueOrDefault(runningId, 1)));
            }

            // Kuyruk: bu makineye planlanmış, hiçbir makinede çalışmayan açık emirler. Sırasızlar sona, plan tarihine göre.
            var queue = knits
                .Where(k => k.PlannedMachineId == machine.Id && !runningCount.ContainsKey(k.Id))
                .OrderBy(k => k.PlanSequence is null).ThenBy(k => k.PlanSequence).ThenBy(k => k.PlannedDate).ThenBy(k => k.Id);
            var position = 0;
            foreach (var knit in queue)
            {
                items.Add(Schedule(knit, false, ++position, 1));
            }

            var openStop = stops.GetValueOrDefault(machine.Id);
            result.Add(new OrmeCapacityMachineDto(
                machine.Id,
                machine.Code,
                machine.Name,
                machine.BedType,
                machine.DiameterInch,
                machine.Gauge,
                machine.IsActive,
                openStop?.Category,
                openStop?.ReasonName,
                items.Sum(i => i.DurationHours ?? 0),
                items.Count == 0 ? null : ToUtc(cursor, zone),
                Math.Max(0, (cursor - now).TotalDays),
                Load(items, now, dayStart, 7),
                Load(items, now, dayStart, 14),
                Load(items, now, dayStart, 30),
                items.Count(i => i.LateRisk),
                items.Count(i => i.DurationHours is null),
                items));
        }

        var unplanned = knits
            .Where(k => !runningCount.ContainsKey(k.Id) && (k.PlannedMachineId is null || !activeMachineIds.Contains(k.PlannedMachineId.Value)))
            .OrderBy(k => k.DueDate is null).ThenBy(k => k.DueDate).ThenBy(k => k.PlannedDate).ThenBy(k => k.KnitOrderNo)
            .Select(k =>
            {
                var remaining = Math.Max(0, k.PlannedKilograms - k.KnittedKilograms);
                var rate = k.KilogramsPerHour is > 0 ? k.KilogramsPerHour : null;
                return new OrmeUnplannedKnitOrderDto(
                    k.Id, k.KnitOrderNo, k.OrderId, k.OrderNo, k.CustomerName, k.FabricCode, k.FabricName, k.BedType, k.DiameterInch, k.Gauge,
                    k.PlannedDate, k.DueDate, remaining, rate, rate is { } perHour ? Round(remaining / perHour) : null,
                    k.PlannedMachineId is null ? null : "Planlandığı makine pasife alınmış ya da silinmiş; yeniden planlayın.");
            })
            .ToList();

        return new OrmeCapacityPlanDto(nowUtc, today, query.Days, hoursPerDay, hoursSource, result, unplanned);
    }

    /// <summary>Emrin taşınabileceği makineler: etkin ve kumaşla aynı plakada. Çap ya da fine farkı uyarı olarak gösterilir.</summary>
    public async Task<Result<IReadOnlyList<OrmeCapacityMachineOptionDto>>> GetOptionsAsync(int knitOrderId, CancellationToken cancellationToken)
    {
        var knit = await db.Set<OrmeKnitOrder>().AsNoTracking()
            .Where(k => k.Id == knitOrderId)
            .Select(k => new { k.PlannedMachineId, k.OrderLine.Fabric.BedType, k.OrderLine.Fabric.DiameterInch, k.OrderLine.Fabric.Gauge })
            .SingleOrDefaultAsync(cancellationToken);
        if (knit is null)
        {
            return NotFound;
        }

        var plan = await GetAsync(new OrmeCapacityPlanQuery(), cancellationToken);
        var machines = await db.Set<OrmeMachine>().AsNoTracking()
            .Where(m => m.IsActive && m.BedType == knit.BedType)
            .OrderBy(m => m.Code)
            .ToListAsync(cancellationToken);

        return machines.Select(m =>
        {
            var row = plan.Machines.FirstOrDefault(x => x.MachineId == m.Id);
            var others = row?.Items.Where(i => i.KnitOrderId != knitOrderId).ToList() ?? [];
            return new OrmeCapacityMachineOptionDto(
                m.Id,
                m.Code,
                m.Name,
                m.DiameterInch,
                m.Gauge,
                OrmeKnitOrderService.FitWarning(knit.DiameterInch, knit.Gauge, m),
                m.Id == knit.PlannedMachineId,
                others.Count,
                others.Count == 0 ? null : others.Max(i => i.EstimatedFinish ?? i.EstimatedStart),
                row?.OpenStopCategory);
        }).ToList();
    }

    /// <summary>
    /// Emri bir makinenin kuyruğuna alır, başka makineye taşır ya da plandan çıkarır. Plaka yapısı kumaşla aynı olmalıdır; çap ve fine
    /// farkı engel değildir. Makinede çalışan emir planda taşınmaz; önce makineden alınır.
    /// </summary>
    public async Task<Result> PlanAsync(int knitOrderId, OrmePlanKnitOrderRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await appLock.TryAcquireAsync(PlanLock, LockTimeout, cancellationToken))
        {
            return Busy;
        }

        var knit = await db.Set<OrmeKnitOrder>().Include(k => k.OrderLine).ThenInclude(l => l.Fabric).SingleOrDefaultAsync(k => k.Id == knitOrderId, cancellationToken);
        if (knit is null)
        {
            return NotFound;
        }

        if (knit.Status != KnitOrderStatus.Open)
        {
            return new Error(InvalidState, "Yalnızca açık emir planlanır.", ErrorType.Conflict);
        }

        if (await db.Set<OrmeKnitOrderMachine>().AnyAsync(m => m.KnitOrderId == knitOrderId && m.ReleasedOn == null, cancellationToken))
        {
            return new Error(InvalidState, "Makinede çalışan emir planda taşınamaz; önce emri makineden alın.", ErrorType.Conflict);
        }

        if (request.MachineId is { } machineId)
        {
            var machine = await db.Set<OrmeMachine>().AsNoTracking().SingleOrDefaultAsync(m => m.Id == machineId, cancellationToken);
            if (machine is null || !machine.IsActive)
            {
                return ReferenceValidation.InvalidReference("machineId", "Makine bulunamadı ya da pasif.");
            }

            if (machine.BedType != knit.OrderLine.Fabric.BedType)
            {
                return ReferenceValidation.InvalidReference("machineId", "Makinenin plaka yapısı kumaşla aynı değil.");
            }
        }

        var previous = knit.PlannedMachineId;
        if (request.PlannedDate is { } plannedDate)
        {
            knit.PlannedDate = plannedDate;
        }

        knit.PlannedMachineId = request.MachineId;
        knit.PlanSequence = null;
        if (request.MachineId is { } target)
        {
            var queue = await QueueAsync(target, knitOrderId, cancellationToken);
            var position = Math.Clamp(request.Position ?? queue.Count + 1, 1, queue.Count + 1);
            queue.Insert(position - 1, knit);
            Renumber(queue);
        }

        if (previous is { } old && old != request.MachineId)
        {
            Renumber(await QueueAsync(old, knitOrderId, cancellationToken));
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Result.Success();
    }

    /// <summary>Makinenin kuyruğunu yeniden sıralar. Çalışan emir sıraya girmez; her zaman kuyruğun başındadır.</summary>
    public async Task<Result> ReorderAsync(int machineId, OrmeMachineQueueRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await appLock.TryAcquireAsync(PlanLock, LockTimeout, cancellationToken))
        {
            return Busy;
        }

        var queue = await QueueAsync(machineId, null, cancellationToken);
        var ids = request.KnitOrderIds.Distinct().ToList();
        if (ids.Any(id => queue.All(k => k.Id != id)))
        {
            return new Error(InvalidState, "Sıralanan emirlerden biri bu makinenin kuyruğunda değil; planı yenileyip tekrar deneyin.", ErrorType.Conflict);
        }

        var ordered = ids.Select(id => queue.Single(k => k.Id == id)).Concat(queue.Where(k => !ids.Contains(k.Id))).ToList();
        Renumber(ordered);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Result.Success();
    }

    /// <summary>Makinenin kuyruğu: bu makineye planlanmış, açık ve hiçbir makinede çalışmayan emirler, mevcut sırasıyla.</summary>
    private Task<List<OrmeKnitOrder>> QueueAsync(int machineId, int? exceptKnitOrderId, CancellationToken cancellationToken) =>
        db.Set<OrmeKnitOrder>()
            .Where(k => k.PlannedMachineId == machineId
                && k.Status == KnitOrderStatus.Open
                && (exceptKnitOrderId == null || k.Id != exceptKnitOrderId)
                && !k.Machines.Any(m => m.ReleasedOn == null))
            .OrderBy(k => k.PlanSequence == null).ThenBy(k => k.PlanSequence).ThenBy(k => k.PlannedDate).ThenBy(k => k.Id)
            .ToListAsync(cancellationToken);

    private static void Renumber(IReadOnlyList<OrmeKnitOrder> queue)
    {
        for (var i = 0; i < queue.Count; i++)
        {
            queue[i].PlanSequence = i + 1;
        }
    }

    /// <summary>
    /// Günlük çalışma saati: ekranda verilmişse o; yoksa aktif vardiyaların gün içinde kapsadığı dakikalar (çakışmalar bir kez sayılır),
    /// bütün vardiyanın birlikte çıktığı (grupsuz) planlı molalar düşülür. Vardiya yoksa <see cref="DefaultHoursPerDay"/>.
    /// </summary>
    private async Task<(decimal Hours, string Source)> DailyHoursAsync(decimal? requested, CancellationToken cancellationToken)
    {
        if (requested is { } given)
        {
            return (given, "Ekranda girilen");
        }

        var shifts = await db.Set<OrmeShift>().AsNoTracking()
            .Where(s => s.IsActive)
            .Select(s => new { s.Id, s.StartTime, s.EndTime })
            .ToListAsync(cancellationToken);
        if (shifts.Count == 0)
        {
            return (DefaultHoursPerDay, "Varsayılan (vardiya tanımlı değil)");
        }

        var shiftIds = shifts.Select(s => s.Id).ToList();
        var breaks = await db.Set<OrmeBreakSchedule>().AsNoTracking()
            .Where(b => b.IsActive && b.BreakGroupId == null && shiftIds.Contains(b.ShiftId))
            .Select(b => new { b.StartTime, b.EndTime })
            .ToListAsync(cancellationToken);

        var minutes = new bool[MinutesPerDay];
        foreach (var shift in shifts)
        {
            Mark(minutes, shift.StartTime, shift.EndTime, true);
        }

        foreach (var pause in breaks.Where(b => b.StartTime != b.EndTime))
        {
            Mark(minutes, pause.StartTime, pause.EndTime, false);
        }

        var total = minutes.Count(m => m);
        return total == 0
            ? (DefaultHoursPerDay, "Varsayılan (vardiyalarda çalışma süresi yok)")
            : (Math.Round(total / 60m, 2, MidpointRounding.AwayFromZero), "Vardiyalardan (grupsuz molalar düşüldü)");
    }

    private const int MinutesPerDay = 24 * 60;

    /// <summary>Gün içindeki dakikaları işaretler; bitiş başlangıçtan küçükse gece yarısını aşar, eşitse bütün gündür.</summary>
    private static void Mark(bool[] minutes, TimeOnly start, TimeOnly end, bool value)
    {
        var from = start.Hour * 60 + start.Minute;
        var to = end.Hour * 60 + end.Minute;
        var length = to > from ? to - from : MinutesPerDay - from + to;
        for (var i = 0; i < length; i++)
        {
            minutes[(from + i) % MinutesPerDay] = value;
        }
    }

    /// <summary>Önümüzdeki <paramref name="days"/> günün (şimdiden itibaren) emirlerle dolu payı.</summary>
    private static decimal Load(IEnumerable<OrmeCapacityItemDto> items, DateTime now, DateTime dayStart, int days)
    {
        var windowStart = (now - dayStart).TotalDays;
        var windowEnd = windowStart + days;
        var busy = items
            .Where(i => i.EndOffsetDays is not null)
            .Sum(i => Math.Max(0, Math.Min(i.EndOffsetDays!.Value, windowEnd) - Math.Max(i.StartOffsetDays, windowStart)));
        return Math.Round((decimal)(busy / days * 100), 1, MidpointRounding.AwayFromZero);
    }

    private static DateTime ToUtc(DateTime local, TimeZoneInfo zone) =>
        new DateTimeOffset(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), zone.GetUtcOffset(local)).UtcDateTime;

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private sealed record KnitRow(
        int Id,
        string KnitOrderNo,
        int OrderId,
        string OrderNo,
        string? CustomerName,
        string FabricCode,
        string FabricName,
        KnitBedType BedType,
        int? DiameterInch,
        int? Gauge,
        decimal? KilogramsPerHour,
        DateOnly PlannedDate,
        DateOnly? DueDate,
        decimal PlannedKilograms,
        decimal KnittedKilograms,
        int? PlannedMachineId,
        int? PlanSequence);
}
