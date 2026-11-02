using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Dokuma.Application.Features.Quotations;
using ERP.Dokuma.Application.Features.Weaving;
using ERP.Dokuma.Domain.Entities;
using ERP.Dokuma.Domain.Enums;
using ERP.Dokuma.Domain.Rules;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ERP.Dokuma.Application.Features.Planning;

public sealed class DokumaCapacityPlanQuery
{
    /// <summary>Zaman çizelgesinde gösterilen gün sayısı.</summary>
    public int Days { get; set; } = 30;

    /// <summary>Tezgâhın günde çalıştığı saat; boşsa vardiyalardan, vardiya yoksa varsayılandan (22,5 saat) alınır.</summary>
    public decimal? HoursPerDay { get; set; }
}

public sealed class DokumaCapacityPlanQueryValidator : AbstractValidator<DokumaCapacityPlanQuery>
{
    public DokumaCapacityPlanQueryValidator()
    {
        RuleFor(x => x.Days).InclusiveBetween(7, 90).WithMessage("Gün sayısı 7 ile 90 arasında olmalıdır.");
        RuleFor(x => x.HoursPerDay).InclusiveBetween(1, 24).WithMessage("Günlük saat 1 ile 24 arasında olmalıdır.").When(x => x.HoursPerDay is not null);
    }
}

/// <summary>Tezgâh kuyruğundaki levent ve tahmini zamanları. Saatler UTC'dir; ofsetler bugünün başından itibaren gün cinsindendir.</summary>
/// <param name="Running">Levent şu anda bu tezgâhta bağlı (kuyruğun başı).</param>
/// <param name="Sequence">Kuyruktaki sıra; bağlı levente boş.</param>
/// <param name="ExpectedFabricMeters">Leventten beklenen kumaş: levent metresi ÷ (1 + çözgü çekmesi).</param>
/// <param name="WovenMeters">Leventten kesilen topların toplamı.</param>
/// <param name="RemainingMeters">Dokunacak kumaş: beklenen − dokunan (sıfırın altına inmez).</param>
/// <param name="MetersPerHour">devir/dk × 60 ÷ (atkı/cm × 100) × verim; tezgâh hızı yoksa boş.</param>
/// <param name="DurationHours">Kalan metre ÷ saatte dokunan metre (tezgâh çalışma saati).</param>
/// <param name="StartOffsetDays">Başlangıcın bugünün 00:00'ından uzaklığı (gün).</param>
/// <param name="EndOffsetDays">Bitişin bugünün 00:00'ından uzaklığı (gün).</param>
/// <param name="NotReady">Levent henüz tezgâha hazır değil (çözgüden çıktı ya da haşılda).</param>
/// <param name="LateRisk">Tahmini bitiş siparişin termininden sonra.</param>
/// <param name="FitWarning">Tezgâhın ağızlık sistemi ya da tarak eni artikele uymuyorsa uyarı.</param>
public sealed record DokumaCapacityItemDto(
    int BeamId,
    string BeamNo,
    BeamStatus BeamStatus,
    int WarpOrderId,
    string WarpNo,
    int OrderId,
    string OrderNo,
    string? CustomerName,
    string ArticleCode,
    string ArticleName,
    bool Running,
    int? Sequence,
    decimal BeamMeters,
    decimal ExpectedFabricMeters,
    decimal WovenMeters,
    decimal RemainingMeters,
    decimal PicksPerCm,
    decimal? MetersPerHour,
    decimal? DurationHours,
    DateTime EstimatedStart,
    DateTime? EstimatedFinish,
    DateOnly? EstimatedFinishDate,
    double StartOffsetDays,
    double? EndOffsetDays,
    DateOnly? DueDate,
    bool NotReady,
    bool LateRisk,
    int? LateDays,
    string? FitWarning);

/// <param name="EfficiencyPercent">Hesapta kullanılan verim (tezgâha özel ya da genel).</param>
/// <param name="QueueHours">Kuyruktaki leventlerin toplam dokuma saati (hızı bilinenler).</param>
/// <param name="QueueEndsAt">Kuyruğun tahmini bitişi (UTC); kuyruk boşsa boş.</param>
/// <param name="QueueDays">Şimdiden kuyruğun bitişine kadar geçen gün.</param>
/// <param name="Load7Percent">Önümüzdeki 7 günün dolu payı.</param>
/// <param name="MissingRateCount">Tezgâh hızı ya da atkı sıklığı olmadığı için süresi hesaplanamayan levent.</param>
public sealed record DokumaCapacityLoomDto(
    int LoomId,
    string LoomCode,
    string LoomName,
    LoomType LoomType,
    SheddingType SheddingType,
    decimal? MaxReedWidthCm,
    int? NominalSpeedRpm,
    decimal EfficiencyPercent,
    bool IsActive,
    DokumaStopCategory? OpenStopCategory,
    string? OpenStopReason,
    decimal QueueHours,
    DateTime? QueueEndsAt,
    double QueueDays,
    decimal Load7Percent,
    decimal Load14Percent,
    decimal Load30Percent,
    int LateRiskCount,
    int MissingRateCount,
    IReadOnlyList<DokumaCapacityItemDto> Items);

/// <summary>Tezgâhta olmayan ve kuyruğa alınmamış levent.</summary>
/// <param name="Note">Planlandığı tezgâh pasife alındıysa açıklama.</param>
public sealed record DokumaUnplannedBeamDto(
    int BeamId,
    string BeamNo,
    BeamStatus BeamStatus,
    int WarpOrderId,
    string WarpNo,
    int OrderId,
    string OrderNo,
    string? CustomerName,
    string ArticleCode,
    string ArticleName,
    SheddingType SheddingType,
    decimal ReedWidthCm,
    decimal PicksPerCm,
    DateOnly? ReadyOn,
    DateOnly? DueDate,
    decimal ExpectedFabricMeters,
    string? Note);

/// <param name="GeneratedAt">Planın hesaplandığı an (UTC); tahminler bu andan başlar.</param>
/// <param name="HoursPerDay">Hesapta kullanılan günlük çalışma saati.</param>
/// <param name="HoursSource">Günlük saatin nereden geldiği.</param>
/// <param name="DefaultEfficiencyPercent">Tezgâha özel verim yoksa kullanılan genel verim.</param>
public sealed record DokumaCapacityPlanDto(
    DateTime GeneratedAt,
    DateOnly Today,
    int Days,
    decimal HoursPerDay,
    string HoursSource,
    decimal DefaultEfficiencyPercent,
    IReadOnlyList<DokumaCapacityLoomDto> Looms,
    IReadOnlyList<DokumaUnplannedBeamDto> Unplanned);

/// <summary>Leventin taşınabileceği tezgâh.</summary>
/// <param name="FitWarning">Ağızlık sistemi ya da tarak eni uymuyorsa açıklama; bu tezgâha planlanamaz.</param>
/// <param name="Current">Levent şu anda bu tezgâhın kuyruğunda.</param>
/// <param name="QueueCount">Bu levent dışında kuyruktaki levent sayısı (bağlı olan dahil).</param>
/// <param name="QueueEndsAt">Kuyruğun tahmini bitişi (UTC).</param>
/// <param name="DurationHours">Bu leventin bu tezgâhta tahmini dokuma süresi.</param>
public sealed record DokumaCapacityLoomOptionDto(
    int LoomId,
    string Code,
    string Name,
    SheddingType SheddingType,
    decimal? MaxReedWidthCm,
    int? NominalSpeedRpm,
    string? FitWarning,
    bool Current,
    int QueueCount,
    DateTime? QueueEndsAt,
    decimal? DurationHours,
    DokumaStopCategory? OpenStopCategory);

/// <param name="LoomId">Kuyruğuna alınacak tezgâh; boşsa levent plandan çıkarılır.</param>
/// <param name="Position">Kuyruktaki yeri (1'den); boşsa sona eklenir.</param>
public sealed record DokumaPlanBeamRequest(int? LoomId, int? Position = null);

/// <param name="BeamIds">Kuyruğun yeni sırası; listede olmayan leventler eski sırasıyla sona eklenir.</param>
public sealed record DokumaLoomQueueRequest(IReadOnlyList<int> BeamIds);

public sealed class DokumaPlanBeamRequestValidator : AbstractValidator<DokumaPlanBeamRequest>
{
    public DokumaPlanBeamRequestValidator()
    {
        RuleFor(x => x.LoomId).GreaterThan(0).WithMessage("Tezgâh geçersiz.").When(x => x.LoomId is not null);
        RuleFor(x => x.Position).InclusiveBetween(1, 10_000).WithMessage("Sıra 1'den başlar.").When(x => x.Position is not null);
    }
}

public sealed class DokumaLoomQueueRequestValidator : AbstractValidator<DokumaLoomQueueRequest>
{
    public DokumaLoomQueueRequestValidator()
    {
        RuleFor(x => x.BeamIds)
            .NotEmpty().WithMessage("Sıralanacak levent gönderilmedi.")
            .Must(ids => ids is null || ids.Count <= 1000).WithMessage("Bir kuyrukta en fazla 1000 levent sıralanır.");
    }
}

public interface IDokumaCapacityPlanService
{
    Task<DokumaCapacityPlanDto> GetAsync(DokumaCapacityPlanQuery query, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<DokumaCapacityLoomOptionDto>>> GetOptionsAsync(int beamId, CancellationToken cancellationToken);
    Task<Result> PlanAsync(int beamId, DokumaPlanBeamRequest request, CancellationToken cancellationToken);
    Task<Result> ReorderAsync(int loomId, DokumaLoomQueueRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// İleriye dönük tezgâh kapasite planı. Her tezgâhta önce bağlı levent (kalan = beklenen kumaş − dokunan), sonra o tezgâha planlanmış
/// henüz bağlanmamış leventler sırasıyla dizilir. Süre = kalan metre ÷ saatte dokunan metre (devir/dk × 60 ÷ (atkı/cm × 100) × verim);
/// verim tezgâha özel değerden ya da maliyet parametrelerindeki genel verimden gelir. Dokuma saati günlük çalışma saatiyle takvime
/// yayılır: aktif vardiyaların kapsadığı süreden bütün vardiyanın çıktığı (grupsuz) molalar düşülür; vardiya yoksa 22,5 saat.
/// </summary>
public sealed class DokumaCapacityPlanService(IAppDbContext db, IAppLock appLock, TimeProvider timeProvider) : IDokumaCapacityPlanService
{
    /// <summary>Vardiya tanımı yoksa kullanılan günlük çalışma saati (3 × 8 saat − 1,5 saat mola).</summary>
    public const decimal DefaultHoursPerDay = 22.5m;

    private const string PlanLock = "DokumaCapacityPlan";
    private const string InvalidState = "DOKUMA_PLAN_INVALID_STATE";
    private const int MinutesPerDay = 24 * 60;
    private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(10);

    private static readonly BeamStatus[] WaitingStatuses = [BeamStatus.Warped, BeamStatus.AtSizing, BeamStatus.Ready];

    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Levent bulunamadı.", ErrorType.NotFound);
    private static readonly Error Busy = new("DOKUMA_BUSY", "Aynı anda başka bir plan işlemi yapılıyor; tekrar deneyin.", ErrorType.Conflict);

    public async Task<DokumaCapacityPlanDto> GetAsync(DokumaCapacityPlanQuery query, CancellationToken cancellationToken)
    {
        var (hoursPerDay, hoursSource) = await DailyHoursAsync(query.HoursPerDay, cancellationToken);
        var settings = await DokumaCostSettingsService.LoadAsync(db, cancellationToken);
        var rates = await DokumaCostSettingsService.LoomRatesAsync(db, cancellationToken);
        var zone = timeProvider.LocalTimeZone;
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var now = DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(nowUtc, zone), DateTimeKind.Unspecified);
        var today = DateOnly.FromDateTime(now);
        var dayStart = today.ToDateTime(TimeOnly.MinValue);

        var beams = await BeamRows(cancellationToken);
        var runningLoomIds = beams.Where(b => b.Status == BeamStatus.OnLoom && b.LoomId is not null).Select(b => b.LoomId!.Value).Distinct().ToList();

        // Pasife alınmış tezgâhta levent bağlıysa planda görünmeye devam eder.
        var looms = await db.Set<DokumaLoom>().AsNoTracking()
            .Where(l => l.IsActive || runningLoomIds.Contains(l.Id))
            .OrderBy(l => l.Code)
            .ToListAsync(cancellationToken);
        var stops = await OpenStopsAsync(cancellationToken);
        var activeLoomIds = looms.Where(l => l.IsActive).Select(l => l.Id).ToHashSet();

        var result = new List<DokumaCapacityLoomDto>();
        foreach (var loom in looms)
        {
            var efficiency = rates.GetValueOrDefault(loom.Id)?.EfficiencyPercent ?? settings.DefaultEfficiencyPercent;
            var cursor = now;
            var items = new List<DokumaCapacityItemDto>();

            DokumaCapacityItemDto Schedule(BeamRow beam, bool isRunning, int? sequence)
            {
                var expected = WeavingRate.ExpectedFabricMeters(beam.Meters, beam.WarpCrimpPercent);
                var remaining = Math.Max(0, expected - beam.WovenMeters);
                var start = cursor;
                var perHour = WeavingRate.MetersPerHour(loom.NominalSpeedRpm, beam.PicksPerCm, efficiency);
                decimal? hours = perHour is { } rate ? Round(remaining / rate) : null;
                DateTime? finish = hours is { } h ? start.AddDays((double)(h / hoursPerDay)) : null;
                cursor = finish ?? start;

                var finishDate = finish is { } f ? DateOnly.FromDateTime(f) : (DateOnly?)null;
                var late = finishDate is { } fd && beam.DueDate is { } due && fd > due;
                return new DokumaCapacityItemDto(
                    beam.Id,
                    beam.BeamNo,
                    beam.Status,
                    beam.WarpOrderId,
                    beam.WarpNo,
                    beam.OrderId,
                    beam.OrderNo,
                    beam.CustomerName,
                    beam.ArticleCode,
                    beam.ArticleName,
                    isRunning,
                    sequence,
                    beam.Meters,
                    expected,
                    beam.WovenMeters,
                    remaining,
                    beam.PicksPerCm,
                    perHour is { } mph ? Round(mph) : null,
                    hours,
                    ToUtc(start, zone),
                    finish is { } end ? ToUtc(end, zone) : null,
                    finishDate,
                    (start - dayStart).TotalDays,
                    finish is { } endAt ? (endAt - dayStart).TotalDays : null,
                    beam.DueDate,
                    !isRunning && beam.Status != BeamStatus.Ready,
                    late,
                    late ? finishDate!.Value.DayNumber - beam.DueDate!.Value.DayNumber : null,
                    isRunning ? null : FitWarning(loom, beam.SheddingType, beam.ReedWidthCm));
            }

            foreach (var running in beams.Where(b => b.Status == BeamStatus.OnLoom && b.LoomId == loom.Id))
            {
                items.Add(Schedule(running, true, null));
            }

            // Kuyruk: bu tezgâha planlanmış, henüz bağlanmamış leventler. Sırasızlar sona, hazır olma tarihine göre.
            var queue = beams
                .Where(b => b.PlannedLoomId == loom.Id && b.Status != BeamStatus.OnLoom)
                .OrderBy(b => b.PlanSequence is null).ThenBy(b => b.PlanSequence).ThenBy(b => b.ReadyOn is null).ThenBy(b => b.ReadyOn).ThenBy(b => b.Id);
            var position = 0;
            foreach (var beam in queue)
            {
                items.Add(Schedule(beam, false, ++position));
            }

            var openStop = stops.GetValueOrDefault(loom.Id);
            result.Add(new DokumaCapacityLoomDto(
                loom.Id,
                loom.Code,
                loom.Name,
                loom.LoomType,
                loom.SheddingType,
                loom.MaxReedWidthCm,
                loom.NominalSpeedRpm,
                efficiency,
                loom.IsActive,
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

        var unplanned = beams
            .Where(b => b.Status != BeamStatus.OnLoom && (b.PlannedLoomId is null || !activeLoomIds.Contains(b.PlannedLoomId.Value)))
            .OrderBy(b => b.Status != BeamStatus.Ready)
            .ThenBy(b => b.DueDate is null).ThenBy(b => b.DueDate)
            .ThenBy(b => b.ReadyOn is null).ThenBy(b => b.ReadyOn)
            .ThenBy(b => b.BeamNo)
            .Select(b => new DokumaUnplannedBeamDto(
                b.Id, b.BeamNo, b.Status, b.WarpOrderId, b.WarpNo, b.OrderId, b.OrderNo, b.CustomerName, b.ArticleCode, b.ArticleName,
                b.SheddingType, b.ReedWidthCm, b.PicksPerCm, b.ReadyOn, b.DueDate,
                WeavingRate.ExpectedFabricMeters(b.Meters, b.WarpCrimpPercent),
                b.PlannedLoomId is null ? null : "Planlandığı tezgâh pasife alınmış ya da silinmiş; yeniden planlayın."))
            .ToList();

        return new DokumaCapacityPlanDto(nowUtc, today, query.Days, hoursPerDay, hoursSource, settings.DefaultEfficiencyPercent, result, unplanned);
    }

    /// <summary>Leventin taşınabileceği tezgâhlar: bütün etkin tezgâhlar; artikele uymayanlar uyarıyla gösterilir, planlanamaz.</summary>
    public async Task<Result<IReadOnlyList<DokumaCapacityLoomOptionDto>>> GetOptionsAsync(int beamId, CancellationToken cancellationToken)
    {
        var beam = await db.Set<DokumaBeam>().AsNoTracking()
            .Where(b => b.Id == beamId)
            .Select(b => new
            {
                b.PlannedLoomId,
                b.Meters,
                b.WarpOrder.OrderLine.Article.SheddingType,
                b.WarpOrder.OrderLine.Article.ReedWidthCm,
                b.WarpOrder.OrderLine.Article.WeftDensityPerCm,
                b.WarpOrder.OrderLine.Article.WarpCrimpPercent,
                Woven = b.Rolls.Sum(r => (decimal?)r.Meters) ?? 0,
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (beam is null)
        {
            return NotFound;
        }

        var plan = await GetAsync(new DokumaCapacityPlanQuery(), cancellationToken);
        var looms = await db.Set<DokumaLoom>().AsNoTracking().Where(l => l.IsActive).OrderBy(l => l.Code).ToListAsync(cancellationToken);
        var remaining = Math.Max(0, WeavingRate.ExpectedFabricMeters(beam.Meters, beam.WarpCrimpPercent) - beam.Woven);

        return looms.Select(l =>
        {
            var row = plan.Looms.FirstOrDefault(x => x.LoomId == l.Id);
            var others = row?.Items.Where(i => i.BeamId != beamId).ToList() ?? [];
            var perHour = WeavingRate.MetersPerHour(l.NominalSpeedRpm, beam.WeftDensityPerCm, row?.EfficiencyPercent ?? plan.DefaultEfficiencyPercent);
            return new DokumaCapacityLoomOptionDto(
                l.Id,
                l.Code,
                l.Name,
                l.SheddingType,
                l.MaxReedWidthCm,
                l.NominalSpeedRpm,
                FitWarning(l, beam.SheddingType, beam.ReedWidthCm),
                l.Id == beam.PlannedLoomId,
                others.Count,
                others.Count == 0 ? null : others.Max(i => i.EstimatedFinish ?? i.EstimatedStart),
                perHour is { } rate ? Round(remaining / rate) : null,
                row?.OpenStopCategory);
        }).ToList();
    }

    /// <summary>
    /// Leventi bir tezgâhın kuyruğuna alır, başka tezgâha taşır ya da plandan çıkarır. Tezgâha bağlı ya da bitmiş levent planlanmaz.
    /// Tezgâh artikeli dokuyabilmelidir (ağızlık sistemi ve tarak eni; levent bağlamadaki kuralla aynı).
    /// </summary>
    public async Task<Result> PlanAsync(int beamId, DokumaPlanBeamRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await appLock.TryAcquireAsync(PlanLock, LockTimeout, cancellationToken))
        {
            return Busy;
        }

        var beam = await db.Set<DokumaBeam>()
            .Include(b => b.WarpOrder).ThenInclude(w => w.OrderLine).ThenInclude(l => l.Article)
            .SingleOrDefaultAsync(b => b.Id == beamId, cancellationToken);
        if (beam is null)
        {
            return NotFound;
        }

        if (!WaitingStatuses.Contains(beam.Status))
        {
            return new Error(InvalidState, "Yalnızca tezgâha bağlanmamış levent planlanır.", ErrorType.Conflict);
        }

        if (beam.WarpOrder.Status == WarpOrderStatus.Cancelled)
        {
            return new Error(InvalidState, "İptal edilmiş çözgü emrinin leventi planlanmaz.", ErrorType.Conflict);
        }

        if (request.LoomId is { } loomId)
        {
            var loom = await db.Set<DokumaLoom>().AsNoTracking().SingleOrDefaultAsync(l => l.Id == loomId, cancellationToken);
            if (loom is null || !loom.IsActive)
            {
                return ReferenceValidation.InvalidReference("loomId", "Tezgâh bulunamadı ya da pasif.");
            }

            var article = beam.WarpOrder.OrderLine.Article;
            if (FitWarning(loom, article.SheddingType, article.ReedWidthCm) is { } warning)
            {
                return ReferenceValidation.InvalidReference("loomId", $"{loom.Code}: {warning}");
            }
        }

        var previous = beam.PlannedLoomId;
        beam.PlannedLoomId = request.LoomId;
        beam.PlanSequence = null;
        if (request.LoomId is { } target)
        {
            var queue = await QueueAsync(target, beamId, cancellationToken);
            var position = Math.Clamp(request.Position ?? queue.Count + 1, 1, queue.Count + 1);
            queue.Insert(position - 1, beam);
            Renumber(queue);
        }

        if (previous is { } old && old != request.LoomId)
        {
            Renumber(await QueueAsync(old, beamId, cancellationToken));
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Result.Success();
    }

    /// <summary>Tezgâhın kuyruğunu yeniden sıralar. Bağlı levent sıraya girmez; her zaman kuyruğun başındadır.</summary>
    public async Task<Result> ReorderAsync(int loomId, DokumaLoomQueueRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await appLock.TryAcquireAsync(PlanLock, LockTimeout, cancellationToken))
        {
            return Busy;
        }

        var queue = await QueueAsync(loomId, null, cancellationToken);
        var ids = request.BeamIds.Distinct().ToList();
        if (ids.Any(id => queue.All(b => b.Id != id)))
        {
            return new Error(InvalidState, "Sıralanan leventlerden biri bu tezgâhın kuyruğunda değil; planı yenileyip tekrar deneyin.", ErrorType.Conflict);
        }

        var ordered = ids.Select(id => queue.Single(b => b.Id == id)).Concat(queue.Where(b => !ids.Contains(b.Id))).ToList();
        Renumber(ordered);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Result.Success();
    }

    /// <summary>Tezgâhın kuyruğu: bu tezgâha planlanmış, bağlanmamış leventler, mevcut sırasıyla.</summary>
    private Task<List<DokumaBeam>> QueueAsync(int loomId, int? exceptBeamId, CancellationToken cancellationToken) =>
        db.Set<DokumaBeam>()
            .Where(b => b.PlannedLoomId == loomId
                && WaitingStatuses.Contains(b.Status)
                && (exceptBeamId == null || b.Id != exceptBeamId))
            .OrderBy(b => b.PlanSequence == null).ThenBy(b => b.PlanSequence).ThenBy(b => b.ReadyOn == null).ThenBy(b => b.ReadyOn).ThenBy(b => b.Id)
            .ToListAsync(cancellationToken);

    private static void Renumber(IReadOnlyList<DokumaBeam> queue)
    {
        for (var i = 0; i < queue.Count; i++)
        {
            queue[i].PlanSequence = i + 1;
        }
    }

    /// <summary>Tezgâh artikele uymuyorsa nedeni; uyuyorsa boş.</summary>
    private static string? FitWarning(DokumaLoom loom, SheddingType articleShedding, decimal reedWidthCm)
    {
        var problems = new List<string>();
        if (!DokumaWeavingService.CanWeave(loom.SheddingType, articleShedding))
        {
            problems.Add("ağızlık sistemi artikeli dokuyamaz");
        }

        if (loom.MaxReedWidthCm is { } max && reedWidthCm > max)
        {
            problems.Add($"tarak eni {reedWidthCm:0.##} cm, tezgâh en fazla {max:0.##} cm");
        }

        return problems.Count == 0 ? null : string.Join("; ", problems);
    }

    /// <summary>Plandaki leventler: tezgâhta bağlı olanlar ile iptal edilmemiş çözgü emirlerinin bağlanmamış leventleri.</summary>
    private Task<List<BeamRow>> BeamRows(CancellationToken cancellationToken) =>
        db.Set<DokumaBeam>().AsNoTracking()
            .Where(b => b.Status == BeamStatus.OnLoom
                || (WaitingStatuses.Contains(b.Status) && b.WarpOrder.Status != WarpOrderStatus.Cancelled))
            .Select(b => new BeamRow(
                b.Id,
                b.BeamNo,
                b.Status,
                b.WarpOrderId,
                b.WarpOrder.WarpNo,
                b.WarpOrder.OrderLine.OrderId,
                b.WarpOrder.OrderLine.Order.OrderNo,
                b.WarpOrder.OrderLine.Order.Customer != null ? b.WarpOrder.OrderLine.Order.Customer.Name : null,
                b.WarpOrder.OrderLine.Article.ArticleCode,
                b.WarpOrder.OrderLine.Article.Name,
                b.WarpOrder.OrderLine.Article.SheddingType,
                b.WarpOrder.OrderLine.Article.ReedWidthCm,
                b.WarpOrder.OrderLine.Article.WeftDensityPerCm,
                b.WarpOrder.OrderLine.Article.WarpCrimpPercent,
                b.Meters,
                b.Rolls.Sum(r => (decimal?)r.Meters) ?? 0,
                b.ReadyOn,
                b.WarpOrder.OrderLine.Order.DueDate,
                b.LoomId,
                b.PlannedLoomId,
                b.PlanSequence))
            .ToListAsync(cancellationToken);

    private async Task<Dictionary<int, OpenStop>> OpenStopsAsync(CancellationToken cancellationToken) =>
        (await db.Set<DokumaLoomStop>().AsNoTracking()
            .Where(s => s.EndedAt == null)
            .Select(s => new OpenStop(s.LoomId, s.Category, s.Reason.Name, s.StartedAt))
            .ToListAsync(cancellationToken))
        .GroupBy(s => s.LoomId)
        .ToDictionary(g => g.Key, g => g.OrderByDescending(s => s.StartedAt).First());

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

        var shifts = await db.Set<DokumaShift>().AsNoTracking()
            .Where(s => s.IsActive)
            .Select(s => new { s.Id, s.StartTime, s.EndTime })
            .ToListAsync(cancellationToken);
        if (shifts.Count == 0)
        {
            return (DefaultHoursPerDay, "Varsayılan (vardiya tanımlı değil)");
        }

        var shiftIds = shifts.Select(s => s.Id).ToList();
        var breaks = await db.Set<DokumaBreakSchedule>().AsNoTracking()
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

    /// <summary>Önümüzdeki <paramref name="days"/> günün (şimdiden itibaren) leventlerle dolu payı.</summary>
    private static decimal Load(IEnumerable<DokumaCapacityItemDto> items, DateTime now, DateTime dayStart, int days)
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

    private sealed record OpenStop(int LoomId, DokumaStopCategory Category, string ReasonName, DateTime StartedAt);

    private sealed record BeamRow(
        int Id,
        string BeamNo,
        BeamStatus Status,
        int WarpOrderId,
        string WarpNo,
        int OrderId,
        string OrderNo,
        string? CustomerName,
        string ArticleCode,
        string ArticleName,
        SheddingType SheddingType,
        decimal ReedWidthCm,
        decimal PicksPerCm,
        decimal? WarpCrimpPercent,
        decimal Meters,
        decimal WovenMeters,
        DateOnly? ReadyOn,
        DateOnly? DueDate,
        int? LoomId,
        int? PlannedLoomId,
        int? PlanSequence);
}
