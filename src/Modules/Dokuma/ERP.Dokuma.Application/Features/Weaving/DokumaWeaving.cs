using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Domain.Enums;
using ERP.Dokuma.Application.Features.Floor;
using ERP.Dokuma.Application.Features.Receipts;
using ERP.Dokuma.Application.Features.Warping;
using ERP.Dokuma.Domain.Entities;
using ERP.Dokuma.Domain.Enums;
using ERP.Dokuma.Domain.Rules;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Dokuma.Application.Features.Weaving;

public sealed class DokumaFabricRollQuery : PageRequest
{
    /// <summary>Top no, levent no, sipariş no, artikel ya da tezgâhta aranır.</summary>
    public string? Search { get; set; }
}

/// <param name="ExpectedFabricMeters">Leventten beklenen kumaş: levent metresi / (1 + çözgü çekmesi).</param>
/// <param name="OpenStopCategory">Tezgâh duruyorsa duruşun türü.</param>
public sealed record DokumaLoomBoardItemDto(
    int LoomId,
    string LoomCode,
    string LoomName,
    LoomType LoomType,
    SheddingType SheddingType,
    decimal? MaxReedWidthCm,
    int? BeamId,
    string? BeamNo,
    string? OrderNo,
    string? CustomerName,
    string? ArticleCode,
    string? ArticleName,
    DateOnly? MountedOn,
    decimal? BeamMeters,
    decimal? ExpectedFabricMeters,
    decimal? WovenMeters,
    int RollCount,
    DokumaStopCategory? OpenStopCategory,
    string? OpenStopReason,
    DateTime? OpenStopSince);

public sealed record DokumaWeftYarnDto(int YarnId, string YarnCode, string YarnName, string? YarnColor, decimal RequiredKilograms, decimal IssuedKilograms);

public sealed record DokumaWeftIssueDto(int Id, int YarnLotId, string LotNo, string? ProducerLotNo, string YarnCode, string YarnName, DateOnly IssuedOn, decimal Kilograms);

/// <param name="CustomerOrderNo">Müşteri siparişinde müşterinin kendi sipariş numarası; etikete basılır.</param>
/// <param name="CustomerArticleCode">Müşterinin artikel için kullandığı kendi kodu; etikete basılır.</param>
/// <param name="DocumentLanguage">Etiketin dili: müşterili siparişte müşterinin belge dili.</param>
public sealed record DokumaFabricRollDto(
    int Id,
    string RollNo,
    int BeamId,
    string BeamNo,
    string LoomCode,
    string OrderNo,
    string? CustomerName,
    string ArticleCode,
    string ArticleName,
    DateOnly CutOn,
    decimal Meters,
    decimal? Kilograms,
    FabricRollStatus Status,
    string? Notes,
    DateOnly? InspectedOn,
    decimal? InspectedMeters,
    decimal? InspectedWidthCm,
    decimal? PointsPer100M2,
    string? OverrideNote,
    string? CutByName,
    string? CustomerOrderNo,
    string? CustomerArticleCode,
    DocumentLanguage DocumentLanguage);

/// <param name="Beam">Leventin çözgü ve haşıl bilgisi.</param>
/// <param name="ExpectedFabricMeters">Leventten beklenen kumaş: levent metresi / (1 + çözgü çekmesi).</param>
/// <param name="WeftYarns">Beklenen kumaş için atkı iplikleri (fire dahil) ve verilen.</param>
public sealed record DokumaBeamWeavingDto(
    DokumaBeamDto Beam,
    int? LoomId,
    string? LoomCode,
    string? LoomName,
    DateOnly? MountedOn,
    DateOnly? FinishedOn,
    decimal? LeftoverMeters,
    OrderOwnership Ownership,
    decimal ExpectedFabricMeters,
    decimal WovenMeters,
    IReadOnlyList<DokumaWeftYarnDto> WeftYarns,
    IReadOnlyList<DokumaWeftIssueDto> WeftIssues,
    IReadOnlyList<DokumaFabricRollDto> Rolls,
    IReadOnlyList<DokumaYarnTakeBackDto> TakeBacks);

public sealed record MountDokumaBeamRequest(int LoomId, DateOnly Date);

public sealed record FinishDokumaBeamRequest(DateOnly Date, decimal LeftoverMeters);

public sealed record DokumaWeftIssueRequest(int YarnLotId, decimal Kilograms, DateOnly IssuedOn);

public sealed record DokumaFabricRollRequest(DateOnly CutOn, decimal Meters, decimal? Kilograms = null, string? Notes = null);

public sealed class MountDokumaBeamRequestValidator : AbstractValidator<MountDokumaBeamRequest>
{
    public MountDokumaBeamRequestValidator()
    {
        RuleFor(x => x.LoomId).GreaterThan(0).WithMessage("Tezgâh seçilmelidir.");
        RuleFor(x => x.Date).NotEmpty().WithMessage("Tarih zorunludur.");
    }
}

public sealed class FinishDokumaBeamRequestValidator : AbstractValidator<FinishDokumaBeamRequest>
{
    public FinishDokumaBeamRequestValidator()
    {
        RuleFor(x => x.Date).NotEmpty().WithMessage("Tarih zorunludur.");
        RuleFor(x => x.LeftoverMeters)
            .GreaterThanOrEqualTo(0).WithMessage("Artık metre negatif olamaz.")
            .PrecisionScale(12, 2, true).WithMessage("Artık metre en fazla iki ondalık olabilir.");
    }
}

public sealed class DokumaWeftIssueRequestValidator : AbstractValidator<DokumaWeftIssueRequest>
{
    public DokumaWeftIssueRequestValidator()
    {
        RuleFor(x => x.YarnLotId).GreaterThan(0).WithMessage("İplik lotu seçilmelidir.");
        RuleFor(x => x.IssuedOn).NotEmpty().WithMessage("Çıkış tarihi zorunludur.");
        RuleFor(x => x.Kilograms)
            .GreaterThan(0).WithMessage("Kilo sıfırdan büyük olmalıdır.")
            .PrecisionScale(12, 2, true).WithMessage("Kilo en fazla iki ondalık olabilir.");
    }
}

public sealed class DokumaFabricRollRequestValidator : AbstractValidator<DokumaFabricRollRequest>
{
    public DokumaFabricRollRequestValidator()
    {
        RuleFor(x => x.CutOn).NotEmpty().WithMessage("Kesim tarihi zorunludur.");
        RuleFor(x => x.Meters)
            .GreaterThan(0).WithMessage("Metre sıfırdan büyük olmalıdır.")
            .LessThanOrEqualTo(100_000).WithMessage("Metre çok büyük.")
            .PrecisionScale(12, 2, true).WithMessage("Metre en fazla iki ondalık olabilir.");
        RuleFor(x => x.Kilograms)
            .GreaterThan(0).WithMessage("Kilo sıfırdan büyük olmalıdır.")
            .PrecisionScale(12, 2, true).WithMessage("Kilo en fazla iki ondalık olabilir.")
            .When(x => x.Kilograms is not null);
        RuleFor(x => x.Notes).OptionalText(500);
    }
}

public interface IDokumaWeavingService
{
    Task<IReadOnlyList<DokumaLoomBoardItemDto>> GetBoardAsync(CancellationToken cancellationToken);
    Task<Result<DokumaBeamWeavingDto>> GetBeamAsync(int beamId, CancellationToken cancellationToken);
    Task<Result<DokumaBeamWeavingDto>> MountAsync(int beamId, MountDokumaBeamRequest request, CancellationToken cancellationToken);
    Task<Result<DokumaBeamWeavingDto>> UnmountAsync(int beamId, CancellationToken cancellationToken);
    Task<Result<DokumaBeamWeavingDto>> FinishAsync(int beamId, FinishDokumaBeamRequest request, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<DokumaIssuableLotDto>>> GetIssuableWeftLotsAsync(int beamId, CancellationToken cancellationToken);
    Task<Result<DokumaBeamWeavingDto>> AddWeftIssueAsync(int beamId, DokumaWeftIssueRequest request, CancellationToken cancellationToken);
    Task<Result<DokumaBeamWeavingDto>> RemoveWeftIssueAsync(int beamId, int issueId, CancellationToken cancellationToken);
    Task<Result<DokumaBeamWeavingDto>> AddRollAsync(int beamId, DokumaFabricRollRequest request, CancellationToken cancellationToken);
    Task<Result<DokumaTabletRollDto>> CutRollFromTabletAsync(int beamId, DokumaTabletRollRequest request, int operatorId, CancellationToken cancellationToken);
    Task<Result<DokumaBeamWeavingDto>> RemoveRollAsync(int beamId, int rollId, CancellationToken cancellationToken);
    Task<PagedResult<DokumaFabricRollDto>> GetRollsAsync(DokumaFabricRollQuery query, CancellationToken cancellationToken);
}

/// <summary>
/// Dokuma: hazır levent tezgâha bağlanır (tezgâhta tek levent), atkı ipliği lottan verilir, kumaş top top kesilir, levent
/// bitince artık metresi yazılır ve tezgâh boşalır.
/// </summary>
public sealed class DokumaWeavingService(IAppDbContext db, IDocumentNumberGenerator numbers, IAppLock appLock) : IDokumaWeavingService
{
    private const string RollSeries = "DokumaFabricRoll";
    private const string InvalidState = "DOKUMA_WEAVING_INVALID_STATE";
    private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(10);

    private static readonly Error BeamNotFound = new(ErrorCodes.NotFound, "Levent bulunamadı.", ErrorType.NotFound);
    private static readonly Error Busy = new("DOKUMA_BUSY", "Aynı anda başka bir işlem yapılıyor; tekrar deneyin.", ErrorType.Conflict);
    private static readonly Error NotOnLoom = new(InvalidState, "Levent tezgâhta değil.", ErrorType.Conflict);

    public async Task<IReadOnlyList<DokumaLoomBoardItemDto>> GetBoardAsync(CancellationToken cancellationToken)
    {
        var looms = await db.Set<DokumaLoom>().AsNoTracking().Where(l => l.IsActive).OrderBy(l => l.Code).ToListAsync(cancellationToken);
        var beams = await db.Set<DokumaBeam>().AsNoTracking()
            .Where(b => b.Status == BeamStatus.OnLoom)
            .Select(b => new
            {
                b.Id,
                b.LoomId,
                b.BeamNo,
                b.WarpOrder.OrderLine.Order.OrderNo,
                CustomerName = b.WarpOrder.OrderLine.Order.Customer != null ? b.WarpOrder.OrderLine.Order.Customer.Name : null,
                b.WarpOrder.OrderLine.Article.ArticleCode,
                ArticleName = b.WarpOrder.OrderLine.Article.Name,
                b.WarpOrder.OrderLine.Article.WarpCrimpPercent,
                b.MountedOn,
                b.Meters,
                Woven = b.Rolls.Sum(r => (decimal?)r.Meters) ?? 0,
                RollCount = b.Rolls.Count,
            })
            .ToListAsync(cancellationToken);
        var byLoom = beams.Where(b => b.LoomId is not null).ToDictionary(b => b.LoomId!.Value);
        var stops = await db.Set<DokumaLoomStop>().AsNoTracking()
            .Where(s => s.EndedAt == null)
            .Select(s => new { s.LoomId, s.Category, ReasonName = s.Reason.Name, s.StartedAt })
            .ToDictionaryAsync(s => s.LoomId, cancellationToken);

        return looms.Select(l =>
        {
            var beam = byLoom.GetValueOrDefault(l.Id);
            var stop = stops.GetValueOrDefault(l.Id);
            return new DokumaLoomBoardItemDto(
                l.Id, l.Code, l.Name, l.LoomType, l.SheddingType, l.MaxReedWidthCm,
                beam?.Id, beam?.BeamNo, beam?.OrderNo, beam?.CustomerName, beam?.ArticleCode, beam?.ArticleName, beam?.MountedOn,
                beam?.Meters, beam is null ? null : ExpectedFabric(beam.Meters, beam.WarpCrimpPercent), beam?.Woven, beam?.RollCount ?? 0,
                stop?.Category, stop?.ReasonName, stop?.StartedAt);
        }).ToList();
    }

    public async Task<Result<DokumaBeamWeavingDto>> GetBeamAsync(int beamId, CancellationToken cancellationToken)
    {
        var beam = await db.Set<DokumaBeam>().AsNoTracking()
            .Include(b => b.Loom)
            .Include(b => b.WarpOrder).ThenInclude(w => w.OrderLine).ThenInclude(l => l.Order)
            .Include(b => b.WarpOrder).ThenInclude(w => w.OrderLine).ThenInclude(l => l.Article).ThenInclude(a => a.Yarns).ThenInclude(y => y.Yarn)
            .Include(b => b.WeftIssues).ThenInclude(i => i.YarnLot).ThenInclude(l => l.Yarn)
            .Include(b => b.TakeBacks).ThenInclude(t => t.YarnLot).ThenInclude(l => l.Yarn)
            .SingleOrDefaultAsync(b => b.Id == beamId, cancellationToken);
        if (beam is null)
        {
            return BeamNotFound;
        }

        var article = beam.WarpOrder.OrderLine.Article;
        var expected = ExpectedFabric(beam.Meters, article.WarpCrimpPercent);
        var issuedByYarn = beam.WeftIssues.GroupBy(i => i.YarnLot.YarnId).ToDictionary(g => g.Key, g => g.Sum(i => i.Kilograms));
        foreach (var taken in beam.TakeBacks)
        {
            issuedByYarn[taken.YarnLot.YarnId] = issuedByYarn.GetValueOrDefault(taken.YarnLot.YarnId) - taken.Kilograms;
        }

        var weft = article.Yarns.Where(y => y.Role == ArticleYarnRole.Weft).ToList();
        var weftTotal = weft.Sum(y => y.Ends);
        var weftYarns = weft
            .GroupBy(y => y.YarnId)
            .Select(g =>
            {
                var yarn = g.First().Yarn;
                var tex = YarnRequirement.Tex(yarn.CountValue, yarn.CountSystem, yarn.Plies);
                var share = weftTotal == 0 ? 0 : (decimal)g.Sum(y => y.Ends) / weftTotal;
                var kilograms = YarnRequirement.WeftKilogramsPerMeter(article.WeftDensityPerCm, article.ReedWidthCm, article.WeftCrimpPercent, tex, share)
                    * expected * (1 + (article.WeftWastePercent ?? 0) / 100m);
                return new DokumaWeftYarnDto(yarn.Id, yarn.YarnCode, yarn.Name, yarn.Color, Math.Round(kilograms, 2), issuedByYarn.GetValueOrDefault(yarn.Id));
            })
            .ToList();

        var beamDto = await ProjectBeam(beamId).SingleAsync(cancellationToken);
        var rolls = await ProjectRolls(db.Set<DokumaFabricRoll>().AsNoTracking().Where(r => r.BeamId == beamId).OrderBy(r => r.Id)).ToListAsync(cancellationToken);

        return new DokumaBeamWeavingDto(
            beamDto,
            beam.LoomId,
            beam.Loom?.Code,
            beam.Loom?.Name,
            beam.MountedOn,
            beam.FinishedOn,
            beam.LeftoverMeters,
            beam.WarpOrder.OrderLine.Order.Ownership,
            expected,
            rolls.Sum(r => r.Meters),
            weftYarns,
            beam.WeftIssues.OrderBy(i => i.Id).Select(i => new DokumaWeftIssueDto(
                i.Id, i.YarnLotId, i.YarnLot.LotNo, i.YarnLot.ProducerLotNo, i.YarnLot.Yarn.YarnCode, i.YarnLot.Yarn.Name, i.IssuedOn, i.Kilograms)).ToList(),
            rolls,
            beam.TakeBacks.OrderBy(t => t.Id).Select(t => new DokumaYarnTakeBackDto(t.Id, t.YarnLotId, t.YarnLot.LotNo, t.YarnLot.Yarn.YarnCode, t.YarnLot.Yarn.Name, t.TakenOn, t.Kilograms, t.Notes)).ToList());
    }

    /// <summary>
    /// Hazır leventi boş tezgâha bağlar. Ağızlık: jakar artikel yalnız jakar tezgâhta, armür artikel armür ya da jakar
    /// tezgâhta, eksantrik artikel her tezgâhta dokunur. Artikelin tarak eni tezgâhın en geniş tarak enini aşamaz.
    /// </summary>
    public async Task<Result<DokumaBeamWeavingDto>> MountAsync(int beamId, MountDokumaBeamRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        // Levent kilidi aynı leventin iki tezgâha birden bağlanmasını, tezgâh kilidi bir tezgâha iki levent bağlanmasını
        // önler. Sıra hep levent → tezgâhtır (tezgâh kilidini alan diğer işlemler levent kilidi almaz); kilitlenme olmaz.
        if (!await appLock.TryAcquireAsync($"DokumaBeam:{beamId}", LockTimeout, cancellationToken)
            || !await appLock.TryAcquireAsync($"DokumaLoom:{request.LoomId}", LockTimeout, cancellationToken))
        {
            return Busy;
        }

        // Levent durumu kilit alındıktan sonra okunur; aynı anda bağlanan levent burada artık hazır görünmez.
        var beam = await db.Set<DokumaBeam>()
            .Include(b => b.WarpOrder).ThenInclude(w => w.OrderLine).ThenInclude(l => l.Article)
            .SingleOrDefaultAsync(b => b.Id == beamId, cancellationToken);
        if (beam is null)
        {
            return BeamNotFound;
        }

        if (beam.Status != BeamStatus.Ready)
        {
            return new Error(InvalidState, "Yalnızca tezgâha hazır (tahar ya da düğümü yapılmış) levent bağlanır.", ErrorType.Conflict);
        }

        if (await db.Set<DokumaLoomStop>().AnyAsync(s => s.LoomId == request.LoomId && s.EndedAt == null && s.Category == DokumaStopCategory.Breakdown, cancellationToken))
        {
            return new Error(InvalidState, "Tezgâh arızalı; arıza kapanmadan levent bağlanamaz.", ErrorType.Conflict);
        }

        if (await ReferenceValidation.RequireSelectableAsync(db.Set<DokumaLoom>(), request.LoomId, null, "loomId", "Tezgâh", cancellationToken) is { } loomError)
        {
            return loomError;
        }

        var loom = await db.Set<DokumaLoom>().SingleAsync(l => l.Id == request.LoomId, cancellationToken);
        var article = beam.WarpOrder.OrderLine.Article;
        if (!CanWeave(loom.SheddingType, article.SheddingType))
        {
            return new Error(InvalidState, $"{loom.Code} tezgâhının ağızlık sistemi bu artikeli dokuyamaz.", ErrorType.Conflict);
        }

        if (loom.MaxReedWidthCm is { } maxWidth && article.ReedWidthCm > maxWidth)
        {
            return new Error(InvalidState, $"Artikelin tarak eni ({article.ReedWidthCm:0.##} cm) tezgâhın en geniş tarak enini ({maxWidth:0.##} cm) aşıyor.", ErrorType.Conflict);
        }

        if (await db.Set<DokumaBeam>().AnyAsync(b => b.LoomId == loom.Id && b.Status == BeamStatus.OnLoom, cancellationToken))
        {
            return new Error(InvalidState, $"{loom.Code} tezgâhında bağlı levent var; önce onu bitirin.", ErrorType.Conflict);
        }

        if (request.Date < beam.ReadyOn)
        {
            return ReferenceValidation.InvalidReference("date", "Bağlama tarihi hazırlık tarihinden önce olamaz.");
        }

        beam.LoomId = loom.Id;
        beam.MountedOn = request.Date;
        beam.Status = BeamStatus.OnLoom;

        // Bağlanan levent kapasite planındaki kuyruktan çıkar; artık tezgâhın bağlı leventi olarak planlanır.
        beam.PlannedLoomId = null;
        beam.PlanSequence = null;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetBeamAsync(beamId, cancellationToken);
    }

    /// <summary>Yanlış tezgâha bağlanan leventi, top kesilmeden ve atkı verilmeden önce geri alır.</summary>
    public async Task<Result<DokumaBeamWeavingDto>> UnmountAsync(int beamId, CancellationToken cancellationToken)
    {
        var beam = await db.Set<DokumaBeam>().Include(b => b.Rolls).Include(b => b.WeftIssues).SingleOrDefaultAsync(b => b.Id == beamId, cancellationToken);
        if (beam is null)
        {
            return BeamNotFound;
        }

        if (beam.Status != BeamStatus.OnLoom)
        {
            return NotOnLoom;
        }

        if (beam.Rolls.Count > 0 || beam.WeftIssues.Count > 0)
        {
            return new Error(InvalidState, "Top kesilmiş ya da atkı verilmiş levent tezgâhtan geri alınamaz; leventi bitirin.", ErrorType.Conflict);
        }

        beam.LoomId = null;
        beam.MountedOn = null;
        beam.Status = BeamStatus.Ready;
        await db.SaveChangesAsync(cancellationToken);
        return await GetBeamAsync(beamId, cancellationToken);
    }

    public async Task<Result<DokumaBeamWeavingDto>> FinishAsync(int beamId, FinishDokumaBeamRequest request, CancellationToken cancellationToken)
    {
        var beam = await db.Set<DokumaBeam>().SingleOrDefaultAsync(b => b.Id == beamId, cancellationToken);
        if (beam is null)
        {
            return BeamNotFound;
        }

        if (beam.Status != BeamStatus.OnLoom)
        {
            return NotOnLoom;
        }

        if (request.Date < beam.MountedOn)
        {
            return ReferenceValidation.InvalidReference("date", "Bitiş tarihi bağlama tarihinden önce olamaz.");
        }

        if (request.LeftoverMeters > beam.Meters)
        {
            return ReferenceValidation.InvalidReference("leftoverMeters", "Artık metre levent metresinden büyük olamaz.");
        }

        beam.FinishedOn = request.Date;
        beam.LeftoverMeters = request.LeftoverMeters;
        beam.Status = BeamStatus.Finished;
        await db.SaveChangesAsync(cancellationToken);
        return await GetBeamAsync(beamId, cancellationToken);
    }

    /// <summary>Atkıya verilebilecek lotlar: artikelin atkı iplikleri, malın sahibi siparişle aynı, kalanı olan.</summary>
    public async Task<Result<IReadOnlyList<DokumaIssuableLotDto>>> GetIssuableWeftLotsAsync(int beamId, CancellationToken cancellationToken)
    {
        var beam = await db.Set<DokumaBeam>().AsNoTracking()
            .Include(b => b.WarpOrder).ThenInclude(w => w.OrderLine).ThenInclude(l => l.Order)
            .Include(b => b.WarpOrder).ThenInclude(w => w.OrderLine).ThenInclude(l => l.Article).ThenInclude(a => a.Yarns)
            .SingleOrDefaultAsync(b => b.Id == beamId, cancellationToken);
        if (beam is null)
        {
            return BeamNotFound;
        }

        var owner = OwnerOf(beam.WarpOrder.OrderLine.Order);
        var yarnIds = beam.WarpOrder.OrderLine.Article.Yarns.Where(y => y.Role == ArticleYarnRole.Weft).Select(y => y.YarnId).Distinct().ToList();
        var lots = await db.Set<DokumaYarnLot>().AsNoTracking()
            .Where(l => yarnIds.Contains(l.YarnId) && l.CustomerId == owner)
            .Select(l => new
            {
                l.Id,
                l.LotNo,
                l.ReceiptDate,
                l.YarnId,
                l.Yarn.YarnCode,
                l.Yarn.Name,
                l.ProducerLotNo,
                Remaining = l.Kilograms - (l.Issues.Sum(i => (decimal?)i.Kilograms) ?? 0) - (l.WeftIssues.Sum(i => (decimal?)i.Kilograms) ?? 0) - (l.Returns.Sum(r => (decimal?)r.Kilograms) ?? 0) + (l.TakeBacks.Sum(t => (decimal?)t.Kilograms) ?? 0),
            })
            .Where(l => l.Remaining > 0)
            .OrderBy(l => l.YarnCode).ThenBy(l => l.ReceiptDate).ThenBy(l => l.Id)
            .ToListAsync(cancellationToken);

        return lots.Select(l => new DokumaIssuableLotDto(l.Id, l.LotNo, l.ReceiptDate, l.YarnId, l.YarnCode, l.Name, l.ProducerLotNo, l.Remaining)).ToList();
    }

    public async Task<Result<DokumaBeamWeavingDto>> AddWeftIssueAsync(int beamId, DokumaWeftIssueRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await appLock.TryAcquireAsync(DokumaWarpService.YarnLotLock, LockTimeout, cancellationToken))
        {
            return Busy;
        }

        var beam = await db.Set<DokumaBeam>()
            .Include(b => b.WarpOrder).ThenInclude(w => w.OrderLine).ThenInclude(l => l.Order)
            .Include(b => b.WarpOrder).ThenInclude(w => w.OrderLine).ThenInclude(l => l.Article).ThenInclude(a => a.Yarns)
            .SingleOrDefaultAsync(b => b.Id == beamId, cancellationToken);
        if (beam is null)
        {
            return BeamNotFound;
        }

        if (beam.Status != BeamStatus.OnLoom)
        {
            return NotOnLoom;
        }

        var lot = await db.Set<DokumaYarnLot>().SingleOrDefaultAsync(l => l.Id == request.YarnLotId, cancellationToken);
        if (lot is null)
        {
            return ReferenceValidation.InvalidReference("yarnLotId", "İplik lotu bulunamadı.");
        }

        if (lot.CustomerId != OwnerOf(beam.WarpOrder.OrderLine.Order))
        {
            return ReferenceValidation.InvalidReference("yarnLotId", "Lotun sahibi siparişle aynı değil: fasonda müşterinin, kendi malında işletmenin ipliği verilir.");
        }

        if (beam.WarpOrder.OrderLine.Article.Yarns.All(y => y.Role != ArticleYarnRole.Weft || y.YarnId != lot.YarnId))
        {
            return ReferenceValidation.InvalidReference("yarnLotId", "Bu iplik artikelin atkısında yok.");
        }

        var remaining = lot.Kilograms - await DokumaYarnStock.IssuedFromLotAsync(db, lot.Id, cancellationToken);
        if (request.Kilograms > remaining)
        {
            return new Error(InvalidState, $"{lot.LotNo} lotunda {remaining:0.##} kg kaldı; daha fazlası verilemez.", ErrorType.Conflict);
        }

        db.Set<DokumaWeftIssue>().Add(new DokumaWeftIssue { BeamId = beamId, YarnLotId = lot.Id, IssuedOn = request.IssuedOn, Kilograms = request.Kilograms });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetBeamAsync(beamId, cancellationToken);
    }

    /// <summary>Yanlış girilen atkı çıkışını geri alır; iplik lota döner. Yalnızca levent tezgâhtayken.</summary>
    public async Task<Result<DokumaBeamWeavingDto>> RemoveWeftIssueAsync(int beamId, int issueId, CancellationToken cancellationToken)
    {
        var issue = await db.Set<DokumaWeftIssue>().Include(i => i.Beam).SingleOrDefaultAsync(i => i.Id == issueId && i.BeamId == beamId, cancellationToken);
        if (issue is null)
        {
            return new Error(ErrorCodes.NotFound, "Atkı çıkışı bulunamadı.", ErrorType.NotFound);
        }

        if (issue.Beam.Status != BeamStatus.OnLoom)
        {
            return NotOnLoom;
        }

        var issuedFromLot = await db.Set<DokumaWeftIssue>().Where(i => i.BeamId == beamId && i.YarnLotId == issue.YarnLotId).SumAsync(i => i.Kilograms, cancellationToken);
        var takenFromLot = await db.Set<DokumaYarnTakeBack>().Where(t => t.BeamId == beamId && t.YarnLotId == issue.YarnLotId).SumAsync(t => (decimal?)t.Kilograms, cancellationToken) ?? 0;
        if (issuedFromLot - issue.Kilograms < takenFromLot)
        {
            return new Error(InvalidState, "Bu çıkıştan geri alım yapılmış; önce geri alımı silin.", ErrorType.Conflict);
        }

        db.Set<DokumaWeftIssue>().Remove(issue);
        await db.SaveChangesAsync(cancellationToken);
        return await GetBeamAsync(beamId, cancellationToken);
    }

    public async Task<Result<DokumaBeamWeavingDto>> AddRollAsync(int beamId, DokumaFabricRollRequest request, CancellationToken cancellationToken)
    {
        var added = await AddRollAsync(beamId, request.CutOn, request.Meters, request.Kilograms, request.Notes, null, null, cancellationToken);
        return added.IsSuccess ? await GetBeamAsync(beamId, cancellationToken) : added.Error!;
    }

    public async Task<Result<DokumaTabletRollDto>> CutRollFromTabletAsync(int beamId, DokumaTabletRollRequest request, int operatorId, CancellationToken cancellationToken)
    {
        var added = await AddRollAsync(beamId, request.CutOn, request.Meters, request.Kilograms, request.Notes, request.RequestId, operatorId, cancellationToken);
        return added.IsSuccess ? new DokumaTabletRollDto(added.Value!.RollNo, added.Value.Meters) : added.Error!;
    }

    private async Task<Result<DokumaFabricRoll>> AddRollAsync(
        int beamId, DateOnly cutOn, decimal meters, decimal? kilograms, string? notes, Guid? requestId, int? operatorId, CancellationToken cancellationToken)
    {
        var beam = await db.Set<DokumaBeam>().SingleOrDefaultAsync(b => b.Id == beamId, cancellationToken);
        if (beam is null)
        {
            return BeamNotFound;
        }

        if (beam.Status != BeamStatus.OnLoom || beam.LoomId is null)
        {
            return NotOnLoom;
        }

        if (cutOn < beam.MountedOn)
        {
            return ReferenceValidation.InvalidReference("cutOn", "Kesim tarihi bağlama tarihinden önce olamaz.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var year = cutOn.Year;
        var sequence = await numbers.NextAsync(RollSeries, year, cancellationToken);
        var roll = new DokumaFabricRoll
        {
            RollNo = $"DT-{year}-{sequence:D6}",
            BeamId = beamId,
            LoomId = beam.LoomId.Value,
            CutOn = cutOn,
            Meters = meters,
            Kilograms = kilograms,
            Notes = NullIfBlank(notes),
            ClientRequestId = requestId,
            CutByOperatorId = operatorId,
        };
        db.Set<DokumaFabricRoll>().Add(roll);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return roll;
    }

    /// <summary>Yanlış kesilen topu siler; levent tezgâhtayken ve top kontrole girmeden.</summary>
    public async Task<Result<DokumaBeamWeavingDto>> RemoveRollAsync(int beamId, int rollId, CancellationToken cancellationToken)
    {
        var roll = await db.Set<DokumaFabricRoll>().Include(r => r.Beam).SingleOrDefaultAsync(r => r.Id == rollId && r.BeamId == beamId, cancellationToken);
        if (roll is null)
        {
            return new Error(ErrorCodes.NotFound, "Top bulunamadı.", ErrorType.NotFound);
        }

        if (roll.Beam.Status != BeamStatus.OnLoom)
        {
            return NotOnLoom;
        }

        if (roll.Status != FabricRollStatus.PendingInspection)
        {
            return new Error(InvalidState, "Kontrol edilmiş top silinemez.", ErrorType.Conflict);
        }

        db.Set<DokumaFabricRoll>().Remove(roll);
        await db.SaveChangesAsync(cancellationToken);
        return await GetBeamAsync(beamId, cancellationToken);
    }

    public Task<PagedResult<DokumaFabricRollDto>> GetRollsAsync(DokumaFabricRollQuery query, CancellationToken cancellationToken)
    {
        var rolls = db.Set<DokumaFabricRoll>().AsNoTracking();
        if (NullIfBlank(query.Search) is { } term)
        {
            rolls = rolls.Where(r => r.RollNo.Contains(term)
                || r.Beam.BeamNo.Contains(term)
                || r.Loom.Code.Contains(term)
                || r.Beam.WarpOrder.OrderLine.Order.OrderNo.Contains(term)
                || r.Beam.WarpOrder.OrderLine.Article.ArticleCode.Contains(term));
        }

        return ProjectRolls(rolls.OrderByDescending(r => r.Id)).ToPagedResultAsync(query, cancellationToken);
    }

    /// <summary>Tezgâhın ağızlık sistemi artikelinkini karşılıyor mu: jakar hepsini, armür armür ve eksantriği dokur.</summary>
    public static bool CanWeave(SheddingType loom, SheddingType article) => loom switch
    {
        SheddingType.Jacquard => true,
        SheddingType.Dobby => article is SheddingType.Dobby or SheddingType.Cam,
        _ => article == SheddingType.Cam,
    };

    /// <summary>Leventten beklenen kumaş metresi: çözgü çekmesi kadar kısalır.</summary>
    private static decimal ExpectedFabric(decimal beamMeters, decimal? warpCrimpPercent) =>
        Math.Round(beamMeters / (1 + (warpCrimpPercent ?? 0) / 100m), 2);

    private static int? OwnerOf(DokumaOrder order) => order.Ownership == OrderOwnership.Commission ? order.CustomerId : null;

    private IQueryable<DokumaBeamDto> ProjectBeam(int beamId) => db.Set<DokumaBeam>().AsNoTracking().Where(b => b.Id == beamId).Select(b => new DokumaBeamDto(
        b.Id, b.BeamNo, b.WarpOrderId, b.WarpOrder.WarpNo, b.WarpOrder.OrderLine.Order.OrderNo,
        b.WarpOrder.OrderLine.Order.Customer != null ? b.WarpOrder.OrderLine.Order.Customer.Name : null,
        b.WarpOrder.OrderLine.Article.ArticleCode, b.WarpOrder.OrderLine.Article.Name, b.Meters, b.Ends, b.SizingMode, b.IsSized,
        b.SizingSupplierId, b.SizingSupplier != null ? b.SizingSupplier.Name : null, b.SentToSizingOn, b.SizedOn, b.DrawingMethod, b.ReadyOn,
        b.Status, b.Notes, b.Loom != null ? b.Loom.Code : null,
        b.SizingRecipeId, b.SizingRecipe != null ? b.SizingRecipe.Code : null, b.SizingPickUpPercent, b.SizingViscositySeconds));

    public static IQueryable<DokumaFabricRollDto> ProjectRolls(IQueryable<DokumaFabricRoll> rolls) => rolls.Select(r => new DokumaFabricRollDto(
        r.Id,
        r.RollNo,
        r.BeamId,
        r.Beam.BeamNo,
        r.Loom.Code,
        r.Beam.WarpOrder.OrderLine.Order.OrderNo,
        r.Beam.WarpOrder.OrderLine.Order.Customer != null ? r.Beam.WarpOrder.OrderLine.Order.Customer.Name : null,
        r.Beam.WarpOrder.OrderLine.Article.ArticleCode,
        r.Beam.WarpOrder.OrderLine.Article.Name,
        r.CutOn,
        r.Meters,
        r.Kilograms,
        r.Status,
        r.Notes,
        r.InspectedOn,
        r.InspectedMeters,
        r.InspectedWidthCm,
        r.PointsPer100M2,
        r.OverrideNote,
        r.CutByOperator != null ? r.CutByOperator.Name : null,
        // Müşteri kodları yalnızca müşterili siparişte etikete basılır.
        r.Beam.WarpOrder.OrderLine.Order.CustomerId != null ? r.Beam.WarpOrder.OrderLine.Order.CustomerOrderNo : null,
        r.Beam.WarpOrder.OrderLine.Order.CustomerId != null ? r.Beam.WarpOrder.OrderLine.CustomerArticleCode : null,
        r.Beam.WarpOrder.OrderLine.Order.Customer != null ? r.Beam.WarpOrder.OrderLine.Order.Customer.DocumentLanguage : DocumentLanguage.Turkish));
}
