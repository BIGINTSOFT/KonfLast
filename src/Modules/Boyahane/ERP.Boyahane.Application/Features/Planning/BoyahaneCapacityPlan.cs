using ERP.Boyahane.Application.Features.Batches;
using ERP.Boyahane.Application.Features.Quotations;
using ERP.Boyahane.Domain.Authorization;
using ERP.Boyahane.Domain.Entities;
using ERP.Boyahane.Domain.Enums;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Boyahane.Application.Features.Planning;

public sealed class BoyahaneCapacityPlanQuery
{
    /// <summary>Zaman çizelgesinde gösterilen gün sayısı.</summary>
    public int Days { get; set; } = 14;

    /// <summary>Makinenin günde çalıştığı saat; boşsa vardiyalardan, vardiya yoksa varsayılandan (22,5 saat) alınır.</summary>
    public decimal? HoursPerDay { get; set; }
}

public sealed class BoyahaneCapacityPlanQueryValidator : AbstractValidator<BoyahaneCapacityPlanQuery>
{
    public BoyahaneCapacityPlanQueryValidator()
    {
        RuleFor(x => x.Days).InclusiveBetween(7, 90).WithMessage("Gün sayısı 7 ile 90 arasında olmalıdır.");
        RuleFor(x => x.HoursPerDay).InclusiveBetween(1, 24).WithMessage("Günlük saat 1 ile 24 arasında olmalıdır.").When(x => x.HoursPerDay is not null);
    }
}

/// <summary>Makine kuyruğundaki parti ve tahmini zamanları. Saatler UTC'dir; ofsetler bugünün başından itibaren gün cinsindendir.</summary>
/// <param name="Started">Parti makineye alınmış (işlemde); kuyruğun başındadır, taşınamaz.</param>
/// <param name="Running">Şu anda bu makinede süren adımı var.</param>
/// <param name="Sequence">Planlı partinin kuyruktaki sırası; işlemdeki partide boş.</param>
/// <param name="DurationMinutes">Partinin bu makinedeki toplam süresi (dakika).</param>
/// <param name="RemainingMinutes">Kalan süre; planlı partide toplam süreye eşittir.</param>
/// <param name="DurationSource">Rota (operasyonların standart süreleri) ya da işlem türünün varsayılanı.</param>
/// <param name="Overrun">İşlemdeki parti tahmini süreyi aştı; kalan süre sıfır sayılır.</param>
/// <param name="OverCapacity">Parti kilosu makine kapasitesinden fazla.</param>
/// <param name="DueDate">Partideki siparişlerin en yakın termini.</param>
/// <param name="LateRisk">Tahmini bitiş siparişin termininden sonra.</param>
public sealed record BoyahaneCapacityItemDto(
    int BatchId,
    string BatchNo,
    bool Started,
    bool Running,
    int? Sequence,
    OrderProcessType ProcessType,
    string ItemCode,
    string? ColorName,
    string? CustomerNames,
    string OrderNos,
    decimal TotalKilograms,
    bool OverCapacity,
    string? CapacityOverrideBy,
    string? CurrentStep,
    decimal DurationMinutes,
    decimal RemainingMinutes,
    ProcessMinutesSource DurationSource,
    bool Overrun,
    DateTime EstimatedStart,
    DateTime EstimatedFinish,
    DateOnly EstimatedFinishDate,
    double StartOffsetDays,
    double EndOffsetDays,
    DateOnly? PlannedDate,
    DateOnly? DueDate,
    bool LateRisk,
    int? LateDays);

/// <param name="QueueMinutes">Kuyruktaki işlerin kalan toplam süresi.</param>
/// <param name="QueueEndsAt">Kuyruğun tahmini bitişi (UTC); kuyruk boşsa boş.</param>
/// <param name="Load7Percent">Önümüzdeki 7 günün dolu payı.</param>
public sealed record BoyahaneCapacityMachineDto(
    int MachineId,
    string MachineCode,
    string MachineName,
    DyeMachineType MachineType,
    decimal CapacityKg,
    DyeMachineStatus Status,
    bool IsActive,
    decimal QueueMinutes,
    DateTime? QueueEndsAt,
    double QueueDays,
    decimal Load7Percent,
    decimal Load14Percent,
    decimal Load30Percent,
    int LateRiskCount,
    int OverCapacityCount,
    IReadOnlyList<BoyahaneCapacityItemDto> Items);

/// <param name="GeneratedAt">Planın hesaplandığı an (UTC); tahminler bu andan başlar.</param>
/// <param name="HoursPerDay">Hesapta kullanılan günlük çalışma saati.</param>
/// <param name="HoursSource">Günlük saatin nereden geldiği.</param>
public sealed record BoyahaneCapacityPlanDto(
    DateTime GeneratedAt,
    DateOnly Today,
    int Days,
    decimal HoursPerDay,
    string HoursSource,
    IReadOnlyList<BoyahaneCapacityMachineDto> Machines);

/// <summary>Partinin taşınabileceği makine: etkin ve partinin asıl operasyonunu yapabilen.</summary>
/// <param name="Current">Parti şu anda bu makinede planlı.</param>
/// <param name="CapacityOk">Parti kilosu makine kapasitesine sığıyor.</param>
/// <param name="QueueCount">Bu parti dışında kuyruktaki parti sayısı (işlemdekiler dahil).</param>
/// <param name="QueueEndsAt">Kuyruğun tahmini bitişi (UTC).</param>
public sealed record BoyahaneCapacityMachineOptionDto(
    int MachineId,
    string Code,
    string Name,
    DyeMachineType MachineType,
    decimal CapacityKg,
    DyeMachineStatus Status,
    bool Current,
    bool CapacityOk,
    int QueueCount,
    DateTime? QueueEndsAt);

/// <param name="TotalKilograms">Partinin kilosu.</param>
/// <param name="MainOperation">Makinenin yapabilmesi gereken operasyon; yoksa boş.</param>
public sealed record BoyahaneCapacityOptionsDto(
    int BatchId,
    string BatchNo,
    decimal TotalKilograms,
    string? MainOperation,
    IReadOnlyList<BoyahaneCapacityMachineOptionDto> Machines);

/// <param name="MachineId">Partinin planlanacağı makine.</param>
/// <param name="PlannedDate">Plan tarihi (en erken başlangıç); boşsa tarih kaldırılır.</param>
/// <param name="Position">Kuyruktaki yeri (1'den); boşsa sona eklenir.</param>
/// <param name="CapacityOverrideNote">Parti makine kapasitesini aşıyorsa gerekçe; ayrı yetki ister.</param>
public sealed record BoyahanePlanBatchRequest(int MachineId, DateOnly? PlannedDate = null, int? Position = null, string? CapacityOverrideNote = null);

/// <param name="BatchIds">Kuyruğun yeni sırası; listede olmayan planlı partiler eski sırasıyla sona eklenir.</param>
public sealed record BoyahaneMachineQueueRequest(IReadOnlyList<int> BatchIds);

public sealed class BoyahanePlanBatchRequestValidator : AbstractValidator<BoyahanePlanBatchRequest>
{
    public BoyahanePlanBatchRequestValidator()
    {
        RuleFor(x => x.MachineId).GreaterThan(0).WithMessage("Makine seçilmelidir.");
        RuleFor(x => x.Position).InclusiveBetween(1, 10_000).WithMessage("Sıra 1'den başlar.").When(x => x.Position is not null);
        RuleFor(x => x.CapacityOverrideNote).OptionalText(500);
    }
}

public sealed class BoyahaneMachineQueueRequestValidator : AbstractValidator<BoyahaneMachineQueueRequest>
{
    public BoyahaneMachineQueueRequestValidator()
    {
        RuleFor(x => x.BatchIds)
            .NotEmpty().WithMessage("Sıralanacak parti gönderilmedi.")
            .Must(ids => ids is null || ids.Count <= 1000).WithMessage("Bir kuyrukta en fazla 1000 parti sıralanır.");
    }
}

public interface IBoyahaneCapacityPlanService
{
    Task<BoyahaneCapacityPlanDto> GetAsync(BoyahaneCapacityPlanQuery query, CancellationToken cancellationToken);
    Task<Result<BoyahaneCapacityOptionsDto>> GetOptionsAsync(int batchId, CancellationToken cancellationToken);
    Task<Result> PlanAsync(int batchId, BoyahanePlanBatchRequest request, CancellationToken cancellationToken);
    Task<Result> ReorderAsync(int machineId, BoyahaneMachineQueueRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// İleriye dönük makine kapasite planı. Her makinede önce işlemdeki partiler, sonra planlı partiler kuyruk sırasıyla dizilir.
/// Parti süresi, partinin rota adımlarından bu makinede yapılanların standart sürelerinin toplamıdır; standart süre yoksa işlem
/// türünün varsayılan süresi (Maliyet Parametreleri). İşlemdeki partide kalan süre = süre − partinin başlangıcından geçen süre.
/// Süre, günlük çalışma saatiyle takvime yayılır: günlük saat aktif vardiyaların kapsadığı süreden bütün vardiyanın birlikte çıktığı
/// (grupsuz) molalar düşülerek bulunur; vardiya yoksa 22,5 saat. Makine değişikliği parti açmadaki kurallarla denetlenir.
/// </summary>
public sealed class BoyahaneCapacityPlanService(
    IAppDbContext db,
    IAppLock appLock,
    ICurrentUser currentUser,
    ICurrentPermissions permissions,
    TimeProvider timeProvider) : IBoyahaneCapacityPlanService
{
    /// <summary>Vardiya tanımı yoksa kullanılan günlük çalışma saati (3 × 8 saat − 1,5 saat mola).</summary>
    public const decimal DefaultHoursPerDay = 22.5m;

    private const string PlanLock = "BoyahaneCapacityPlan";
    private const string InvalidState = "BOYAHANE_PLAN_INVALID_STATE";
    private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(10);

    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Parti bulunamadı.", ErrorType.NotFound);
    private static readonly Error Busy = new("BOYAHANE_PLAN_BUSY", "Aynı anda başka bir plan işlemi yapılıyor; tekrar deneyin.", ErrorType.Conflict);

    public async Task<BoyahaneCapacityPlanDto> GetAsync(BoyahaneCapacityPlanQuery query, CancellationToken cancellationToken)
    {
        var (hoursPerDay, hoursSource) = await DailyHoursAsync(query.HoursPerDay, cancellationToken);
        var settings = await BoyahaneCostSettingsService.LoadAsync(db, cancellationToken);
        var zone = timeProvider.LocalTimeZone;
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var now = DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(nowUtc, zone), DateTimeKind.Unspecified);
        var today = DateOnly.FromDateTime(now);
        var dayStart = today.ToDateTime(TimeOnly.MinValue);

        var batches = await LoadBatchesAsync(null, cancellationToken);
        var machineOperations = await MachineOperationsAsync(cancellationToken);
        var busyMachineIds = batches.Select(b => b.MachineId).Distinct().ToList();
        var machines = await db.Set<BoyahaneMachine>().AsNoTracking()
            .Where(m => m.IsActive || busyMachineIds.Contains(m.Id))
            .OrderBy(m => m.Code)
            .ToListAsync(cancellationToken);

        var result = new List<BoyahaneCapacityMachineDto>();
        foreach (var machine in machines)
        {
            var cursor = now;
            var items = new List<BoyahaneCapacityItemDto>();
            var operations = machineOperations.GetValueOrDefault(machine.Id);

            void Schedule(BatchRow batch, int? sequence)
            {
                var estimate = EstimateOf(batch, operations, settings, nowUtc);
                if (estimate is null)
                {
                    return;
                }

                var notBefore = batch.PlannedDate?.ToDateTime(TimeOnly.MinValue);
                var start = batch.Status == BatchStatus.OnMachine || notBefore is null || notBefore <= cursor ? cursor : notBefore.Value;
                var finish = start.AddDays((double)(estimate.Remaining / 60 / hoursPerDay));
                cursor = finish;

                var finishDate = DateOnly.FromDateTime(finish);
                var late = batch.DueDate is { } due && finishDate > due;
                items.Add(new BoyahaneCapacityItemDto(
                    batch.Id,
                    batch.BatchNo,
                    batch.Status == BatchStatus.OnMachine,
                    batch.Steps.Any(s => s.Status == BatchStepStatus.InProgress && s.MachineId == machine.Id),
                    sequence,
                    batch.ProcessType,
                    batch.ItemCode,
                    batch.ColorName,
                    batch.Customers.Count == 0 ? null : string.Join(", ", batch.Customers.Distinct().Order()),
                    string.Join(", ", batch.OrderNos.Distinct().Order()),
                    batch.Kilograms,
                    batch.Kilograms > machine.CapacityKg,
                    batch.CapacityOverrideBy,
                    batch.Steps.Where(s => s.Status is BatchStepStatus.InProgress or BatchStepStatus.Pending).OrderBy(s => s.Sequence).Select(s => s.OperationName).FirstOrDefault(),
                    estimate.Total,
                    estimate.Remaining,
                    estimate.Source,
                    estimate.Overrun,
                    ToUtc(start, zone),
                    ToUtc(finish, zone),
                    finishDate,
                    (start - dayStart).TotalDays,
                    (finish - dayStart).TotalDays,
                    batch.PlannedDate,
                    batch.DueDate,
                    late,
                    late ? finishDate.DayNumber - batch.DueDate!.Value.DayNumber : null));
            }

            foreach (var batch in batches.Where(b => b.MachineId == machine.Id && b.Status == BatchStatus.OnMachine).OrderBy(b => b.StartedAt))
            {
                Schedule(batch, null);
            }

            var position = 0;
            foreach (var batch in QueueOrder(batches.Where(b => b.MachineId == machine.Id && b.Status == BatchStatus.Planned)))
            {
                Schedule(batch, ++position);
            }

            result.Add(new BoyahaneCapacityMachineDto(
                machine.Id,
                machine.Code,
                machine.Name,
                machine.MachineType,
                machine.CapacityKg,
                machine.Status,
                machine.IsActive,
                items.Sum(i => i.RemainingMinutes),
                items.Count == 0 ? null : ToUtc(cursor, zone),
                Math.Max(0, (cursor - now).TotalDays),
                Load(items, now, dayStart, 7),
                Load(items, now, dayStart, 14),
                Load(items, now, dayStart, 30),
                items.Count(i => i.LateRisk),
                items.Count(i => i.OverCapacity),
                items));
        }

        return new BoyahaneCapacityPlanDto(nowUtc, today, query.Days, hoursPerDay, hoursSource, result);
    }

    /// <summary>Planlı partinin taşınabileceği makineler: etkin ve partinin asıl operasyonunu (boyamada boyama) yapabilen.</summary>
    public async Task<Result<BoyahaneCapacityOptionsDto>> GetOptionsAsync(int batchId, CancellationToken cancellationToken)
    {
        var batch = await db.Set<BoyahaneBatch>().AsNoTracking()
            .Include(b => b.Steps).ThenInclude(s => s.Operation)
            .Include(b => b.Items)
            .SingleOrDefaultAsync(b => b.Id == batchId, cancellationToken);
        if (batch is null)
        {
            return NotFound;
        }

        var mainOperation = BoyahaneMachineFit.MainOperation(batch.Steps.Select(s => (s.Sequence, s.Operation)), batch.RecipeTrialId is not null);
        var capableIds = mainOperation is null
            ? null
            : await db.Set<BoyahaneMachineOperation>().Where(o => o.OperationId == mainOperation.Id).Select(o => o.MachineId).ToListAsync(cancellationToken);
        var machines = await db.Set<BoyahaneMachine>().AsNoTracking()
            .Where(m => m.IsActive || m.Id == batch.MachineId)
            .OrderBy(m => m.Code)
            .ToListAsync(cancellationToken);

        var plan = await GetAsync(new BoyahaneCapacityPlanQuery(), cancellationToken);
        var total = batch.Items.Sum(i => i.Kilograms);
        return new BoyahaneCapacityOptionsDto(
            batch.Id,
            batch.BatchNo,
            total,
            mainOperation?.Name,
            machines
                .Where(m => capableIds is null || capableIds.Contains(m.Id) || m.Id == batch.MachineId)
                .Select(m =>
                {
                    var row = plan.Machines.FirstOrDefault(x => x.MachineId == m.Id);
                    var others = row?.Items.Where(i => i.BatchId != batchId).ToList() ?? [];
                    return new BoyahaneCapacityMachineOptionDto(
                        m.Id,
                        m.Code,
                        m.Name,
                        m.MachineType,
                        m.CapacityKg,
                        m.Status,
                        m.Id == batch.MachineId,
                        total <= m.CapacityKg,
                        others.Count,
                        others.Count == 0 ? null : others.Max(i => i.EstimatedFinish));
                })
                .ToList());
    }

    /// <summary>
    /// Planlı partiyi bir makinenin kuyruğuna alır ya da kuyrukta yerini ve plan tarihini değiştirir. Makine değişirse parti açmadaki
    /// kurallar geçerlidir: makine etkin olmalı, partinin asıl operasyonunu yapabilmeli; kapasite aşılıyorsa ayrı yetki ve gerekçe
    /// gerekir. İşlemdeki parti taşınamaz.
    /// </summary>
    public async Task<Result> PlanAsync(int batchId, BoyahanePlanBatchRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await appLock.TryAcquireAsync(PlanLock, LockTimeout, cancellationToken))
        {
            return Busy;
        }

        var batch = await db.Set<BoyahaneBatch>()
            .Include(b => b.Items)
            .Include(b => b.Steps).ThenInclude(s => s.Operation)
            .SingleOrDefaultAsync(b => b.Id == batchId, cancellationToken);
        if (batch is null)
        {
            return NotFound;
        }

        if (batch.Status != BatchStatus.Planned)
        {
            return new Error(InvalidState, "Yalnızca planlı parti planda taşınır; işlemdeki parti makinesinde biter.", ErrorType.Conflict);
        }

        var previous = batch.MachineId;
        if (request.MachineId != previous)
        {
            if (await ReferenceValidation.RequireSelectableAsync(
                    db.Set<BoyahaneMachine>(), request.MachineId, null, "machineId", "Makine", cancellationToken) is { } machineError)
            {
                return machineError;
            }

            var mainOperation = BoyahaneMachineFit.MainOperation(batch.Steps.Select(s => (s.Sequence, s.Operation)), batch.RecipeTrialId is not null);
            if (mainOperation is not null
                && !await db.Set<BoyahaneMachineOperation>().AnyAsync(o => o.MachineId == request.MachineId && o.OperationId == mainOperation.Id, cancellationToken))
            {
                var code = await db.Set<BoyahaneMachine>().Where(m => m.Id == request.MachineId).Select(m => m.Code).SingleAsync(cancellationToken);
                return new Error("BATCH_INVALID_ITEM", $"{code} makinesinde {mainOperation.Name} yapılmaz; makine kartını kontrol edin.", ErrorType.Conflict);
            }
        }

        // Kapasite: parti açmadaki kural. Aşım yetkili gerekçesiyle geçer; makine değişmediyse kayıtlı onay korunur.
        var machine = await db.Set<BoyahaneMachine>().AsNoTracking().SingleAsync(m => m.Id == request.MachineId, cancellationToken);
        var total = batch.Items.Sum(i => i.Kilograms);
        var note = NullIfBlank(request.CapacityOverrideNote);
        if (total > machine.CapacityKg)
        {
            var approved = request.MachineId == previous && batch.CapacityOverrideBy is not null;
            if (!approved || note is not null)
            {
                var canOverride = await permissions.HasAsync(BoyahanePermissions.Batch.CapacityOverride, cancellationToken);
                if (!canOverride || note is null)
                {
                    return new Error(BoyahaneBatchService.OverCapacity, BoyahaneMachineFit.OverCapacityMessage(total, machine.CapacityKg, canOverride), ErrorType.Conflict);
                }

                batch.CapacityOverrideBy = currentUser.UserName;
                batch.CapacityOverrideNote = note;
            }
        }
        else
        {
            batch.CapacityOverrideBy = null;
            batch.CapacityOverrideNote = null;
        }

        batch.MachineId = request.MachineId;
        batch.PlannedDate = request.PlannedDate;
        batch.PlanSequence = null;

        var queue = await QueueAsync(request.MachineId, batchId, cancellationToken);
        var position = Math.Clamp(request.Position ?? queue.Count + 1, 1, queue.Count + 1);
        queue.Insert(position - 1, batch);
        Renumber(queue);

        if (previous != request.MachineId)
        {
            Renumber(await QueueAsync(previous, batchId, cancellationToken));
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Result.Success();
    }

    /// <summary>Makinenin planlı parti kuyruğunu yeniden sıralar. İşlemdeki partiler sıraya girmez; her zaman kuyruğun başındadır.</summary>
    public async Task<Result> ReorderAsync(int machineId, BoyahaneMachineQueueRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await appLock.TryAcquireAsync(PlanLock, LockTimeout, cancellationToken))
        {
            return Busy;
        }

        var queue = await QueueAsync(machineId, null, cancellationToken);
        var ids = request.BatchIds.Distinct().ToList();
        if (ids.Any(id => queue.All(b => b.Id != id)))
        {
            return new Error(InvalidState, "Sıralanan partilerden biri bu makinenin kuyruğunda değil; planı yenileyip tekrar deneyin.", ErrorType.Conflict);
        }

        var ordered = ids.Select(id => queue.Single(b => b.Id == id)).Concat(queue.Where(b => !ids.Contains(b.Id))).ToList();
        Renumber(ordered);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Result.Success();
    }

    /// <summary>Makinenin planlı partileri, mevcut kuyruk sırasıyla.</summary>
    private async Task<List<BoyahaneBatch>> QueueAsync(int machineId, int? exceptBatchId, CancellationToken cancellationToken) =>
        QueueOrder(await db.Set<BoyahaneBatch>()
                .Where(b => b.MachineId == machineId && b.Status == BatchStatus.Planned && (exceptBatchId == null || b.Id != exceptBatchId))
                .ToListAsync(cancellationToken))
            .ToList();

    /// <summary>Kuyruk sırası: sıralanmışlar sırasıyla, sonra sırasızlar plan tarihine göre (tarihsizler en sonda).</summary>
    private static IEnumerable<BoyahaneBatch> QueueOrder(IEnumerable<BoyahaneBatch> batches) =>
        batches.OrderBy(b => b.PlanSequence is null).ThenBy(b => b.PlanSequence).ThenBy(b => b.PlannedDate is null).ThenBy(b => b.PlannedDate).ThenBy(b => b.Id);

    private static IEnumerable<BatchRow> QueueOrder(IEnumerable<BatchRow> batches) =>
        batches.OrderBy(b => b.PlanSequence is null).ThenBy(b => b.PlanSequence).ThenBy(b => b.PlannedDate is null).ThenBy(b => b.PlannedDate).ThenBy(b => b.Id);

    private static void Renumber(IReadOnlyList<BoyahaneBatch> queue)
    {
        for (var i = 0; i < queue.Count; i++)
        {
            queue[i].PlanSequence = i + 1;
        }
    }

    private sealed record StepRow(int Sequence, int OperationId, string OperationName, bool NeedsMachine, decimal? StandardMinutes, BatchStepStatus Status, int? MachineId);

    private sealed record BatchRow(
        int Id,
        string BatchNo,
        int MachineId,
        BatchStatus Status,
        DateOnly? PlannedDate,
        int? PlanSequence,
        DateTime? StartedAt,
        string? CapacityOverrideBy,
        OrderProcessType ProcessType,
        string ItemCode,
        string? ColorName,
        decimal Kilograms,
        DateOnly? DueDate,
        List<string> OrderNos,
        List<string> Customers,
        List<StepRow> Steps);

    private sealed record Estimate(decimal Total, decimal Remaining, ProcessMinutesSource Source, bool Overrun);

    private async Task<List<BatchRow>> LoadBatchesAsync(int? batchId, CancellationToken cancellationToken) =>
        await db.Set<BoyahaneBatch>().AsNoTracking()
            .Where(b => (b.Status == BatchStatus.Planned || b.Status == BatchStatus.OnMachine) && (batchId == null || b.Id == batchId))
            .Select(b => new BatchRow(
                b.Id,
                b.BatchNo,
                b.MachineId,
                b.Status,
                b.PlannedDate,
                b.PlanSequence,
                b.StartedAt,
                b.CapacityOverrideBy,
                b.Items.OrderBy(i => i.Id).Select(i => i.OrderLine.ProcessType).FirstOrDefault(),
                b.Items.OrderBy(i => i.Id)
                    .Select(i => i.OrderLine.Fabric != null ? i.OrderLine.Fabric.FabricCode : i.OrderLine.Yarn != null ? i.OrderLine.Yarn.YarnCode : "")
                    .FirstOrDefault() ?? "",
                b.Items.OrderBy(i => i.Id).Select(i => i.OrderLine.ColorName).FirstOrDefault(),
                b.Items.Sum(i => i.Kilograms),
                b.Items.Min(i => i.OrderLine.Order.DueDate),
                b.Items.Select(i => i.OrderLine.Order.OrderNo).ToList(),
                b.Items.Where(i => i.OrderLine.Order.Customer != null).Select(i => i.OrderLine.Order.Customer!.Name).ToList(),
                b.Steps.Select(s => new StepRow(s.Sequence, s.OperationId, s.Operation.Name, s.Operation.NeedsMachine, s.Operation.StandardMinutes, s.Status, s.MachineId)).ToList()))
            .ToListAsync(cancellationToken);

    private async Task<Dictionary<int, IReadOnlySet<int>>> MachineOperationsAsync(CancellationToken cancellationToken) =>
        (await db.Set<BoyahaneMachineOperation>().AsNoTracking().Select(o => new { o.MachineId, o.OperationId }).ToListAsync(cancellationToken))
            .GroupBy(o => o.MachineId)
            .ToDictionary(g => g.Key, g => (IReadOnlySet<int>)g.Select(o => o.OperationId).ToHashSet());

    /// <summary>
    /// Partinin bu makinedeki süresi. Makinenin operasyonları tanımlıysa yalnızca makinede yapılan adımlar sayılır. İşlemdeki partide
    /// makinenin yapacağı adım kalmadıysa parti makineyi tutmaz (boş döner); kalan süre başlangıçtan geçen süre düşülerek bulunur.
    /// </summary>
    private static Estimate? EstimateOf(BatchRow batch, IReadOnlySet<int>? machineOperations, BoyahaneCostSettings settings, DateTime nowUtc)
    {
        var operations = machineOperations is { Count: > 0 } ? machineOperations : null;
        var steps = batch.Steps.Where(s => s.Status != BatchStepStatus.Skipped).ToList();
        var standard = BoyahaneMachineFit.StandardMinutes(steps.Select(s => (s.OperationId, s.NeedsMachine, s.StandardMinutes)), operations);
        var total = standard ?? settings.DefaultMinutes(batch.ProcessType);
        var source = standard is null ? ProcessMinutesSource.Default : ProcessMinutesSource.Route;
        if (batch.Status != BatchStatus.OnMachine)
        {
            return new Estimate(total, total, source, false);
        }

        var machineWorkLeft = steps.Any(s => s.NeedsMachine
            && (operations is null || operations.Contains(s.OperationId))
            && s.Status is BatchStepStatus.Pending or BatchStepStatus.InProgress);
        if (!machineWorkLeft)
        {
            return null;
        }

        var elapsed = batch.StartedAt is { } started ? (decimal)Math.Max(0, (nowUtc - started).TotalMinutes) : 0;
        var remaining = Math.Max(0, total - elapsed);
        return new Estimate(total, Math.Round(remaining, 1), source, remaining == 0);
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

        var shifts = await db.Set<BoyahaneShift>().AsNoTracking()
            .Where(s => s.IsActive)
            .Select(s => new { s.Id, s.StartTime, s.EndTime })
            .ToListAsync(cancellationToken);
        if (shifts.Count == 0)
        {
            return (DefaultHoursPerDay, "Varsayılan (vardiya tanımlı değil)");
        }

        var shiftIds = shifts.Select(s => s.Id).ToList();
        var breaks = await db.Set<BoyahaneBreakSchedule>().AsNoTracking()
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

    /// <summary>Önümüzdeki <paramref name="days"/> günün (şimdiden itibaren) partilerle dolu payı.</summary>
    private static decimal Load(IEnumerable<BoyahaneCapacityItemDto> items, DateTime now, DateTime dayStart, int days)
    {
        var windowStart = (now - dayStart).TotalDays;
        var windowEnd = windowStart + days;
        var busy = items.Sum(i => Math.Max(0, Math.Min(i.EndOffsetDays, windowEnd) - Math.Max(i.StartOffsetDays, windowStart)));
        return Math.Round((decimal)(busy / days * 100), 1, MidpointRounding.AwayFromZero);
    }

    private static DateTime ToUtc(DateTime local, TimeZoneInfo zone) =>
        new DateTimeOffset(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), zone.GetUtcOffset(local)).UtcDateTime;
}
