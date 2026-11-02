using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Dokuma.Application.Features.Receipts;
using ERP.Dokuma.Application.Features.Stock;
using ERP.Dokuma.Domain.Entities;
using ERP.Dokuma.Domain.Enums;
using ERP.Dokuma.Domain.Rules;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Dokuma.Application.Features.Warping;

public sealed class DokumaWarpOrderQuery : PageRequest
{
    /// <summary>Çözgü no, sipariş no, artikel ya da müşteri adında aranır.</summary>
    public string? Search { get; set; }
}

public sealed class DokumaBeamQuery : PageRequest
{
    /// <summary>Levent no, çözgü no, sipariş no ya da artikelde aranır.</summary>
    public string? Search { get; set; }
}

public sealed record DokumaWarpOrderListItemDto(
    int Id,
    string WarpNo,
    DateOnly PlannedDate,
    int OrderId,
    string OrderNo,
    string? CustomerName,
    string ArticleCode,
    string ArticleName,
    int PlannedBeamCount,
    decimal MetersPerBeam,
    int Ends,
    int BeamCount,
    decimal IssuedKilograms,
    WarpOrderStatus Status);

public sealed record DokumaWarpIssueDto(int Id, int YarnLotId, string LotNo, string? ProducerLotNo, string YarnCode, string YarnName, DateOnly IssuedOn, decimal Kilograms);

/// <param name="RequiredKilograms">Planlanan levent metresi için bu ipliğin çözgü ihtiyacı (fire dahil).</param>
public sealed record DokumaWarpYarnDto(int YarnId, string YarnCode, string YarnName, string? YarnColor, int Ends, decimal RequiredKilograms, decimal IssuedKilograms);

public sealed record DokumaBeamDto(
    int Id,
    string BeamNo,
    int WarpOrderId,
    string WarpNo,
    string OrderNo,
    string? CustomerName,
    string ArticleCode,
    string ArticleName,
    decimal Meters,
    int Ends,
    SizingMode SizingMode,
    bool IsSized,
    int? SizingSupplierId,
    string? SizingSupplierName,
    DateOnly? SentToSizingOn,
    DateOnly? SizedOn,
    DrawingMethod? DrawingMethod,
    DateOnly? ReadyOn,
    BeamStatus Status,
    string? Notes,
    string? LoomCode,
    int? SizingRecipeId,
    string? SizingRecipeCode,
    decimal? SizingPickUpPercent,
    decimal? SizingViscositySeconds);

/// <param name="TakeBacks">Çözgüden artıp lota geri alınan iplik.</param>
/// <param name="MixedProducerLotYarns">Aynı ipliğin birden çok üretici lotundan verildiği iplikler (renk farkı riski).</param>
public sealed record DokumaWarpOrderDto(
    int Id,
    string WarpNo,
    DateOnly PlannedDate,
    int OrderId,
    string OrderNo,
    OrderOwnership Ownership,
    string? CustomerName,
    int OrderLineId,
    decimal OrderLineMeters,
    string ArticleCode,
    string ArticleName,
    int PlannedBeamCount,
    decimal MetersPerBeam,
    int Ends,
    WarpOrderStatus Status,
    DateOnly? CompletedOn,
    string? Notes,
    IReadOnlyList<DokumaWarpYarnDto> Yarns,
    IReadOnlyList<DokumaWarpIssueDto> Issues,
    IReadOnlyList<DokumaBeamDto> Beams,
    IReadOnlyList<string> MixedProducerLotYarns,
    IReadOnlyList<DokumaYarnTakeBackDto> TakeBacks);

public sealed record DokumaIssuableLotDto(int Id, string LotNo, DateOnly ReceiptDate, int YarnId, string YarnCode, string YarnName, string? ProducerLotNo, decimal RemainingKilograms);

public sealed record CreateDokumaWarpOrderRequest(int OrderLineId, DateOnly PlannedDate, int PlannedBeamCount, decimal MetersPerBeam, string? Notes = null);

public sealed record UpdateDokumaWarpOrderRequest(DateOnly PlannedDate, int PlannedBeamCount, decimal MetersPerBeam, string? Notes = null);

public sealed record DokumaWarpIssueRequest(int YarnLotId, decimal Kilograms, DateOnly IssuedOn);

/// <param name="Ends">Boşsa çözgü emrinin tel sayısı.</param>
public sealed record DokumaBeamRequest(decimal Meters, SizingMode SizingMode, int? Ends = null, string? Notes = null);

/// <param name="SizingRecipeId">Haşılcıya gönderilen belgeye basılan reçete (bilgi amaçlı, isteğe bağlı).</param>
public sealed record SendDokumaBeamToSizingRequest(int SupplierId, DateOnly Date, int? SizingRecipeId = null);

public sealed record DokumaBeamDateRequest(DateOnly Date);

/// <param name="PickUpPercent">Haşılcının bildirdiği alım (%), isteğe bağlı.</param>
/// <param name="ViscositySeconds">Haşılcının bildirdiği viskozite (saniye), isteğe bağlı.</param>
public sealed record ReceiveDokumaBeamFromSizingRequest(DateOnly Date, decimal? PickUpPercent = null, decimal? ViscositySeconds = null);

/// <param name="Quantity">Kullanılan kimyasal, malzemenin biriminde; lotlardan düşülür.</param>
public sealed record DokumaSizingChemicalRequest(int MaterialId, decimal Quantity);

/// <param name="SizingRecipeId">Uygulanan reçete; artikelin reçetelerinden biri olmalıdır.</param>
/// <param name="PickUpPercent">Gerçekleşen alım (%).</param>
/// <param name="ViscositySeconds">Gerçekleşen viskozite (saniye).</param>
/// <param name="Chemicals">Gerçekte kullanılan kimyasallar; malzeme stoğundan en eski lottan başlayarak düşülür.</param>
public sealed record MarkDokumaBeamSizedRequest(
    DateOnly Date,
    int? SizingRecipeId = null,
    decimal? PickUpPercent = null,
    decimal? ViscositySeconds = null,
    IReadOnlyList<DokumaSizingChemicalRequest>? Chemicals = null);

/// <param name="EstimatedQuantity">Leventin tahmini ipliği × hedef alım × banyo oranı; hedef alım ya da iplik bilgisi yoksa boş.</param>
/// <param name="AvailableQuantity">Malzeme stoğundaki kalan.</param>
public sealed record DokumaSizingPlanLineDto(int MaterialId, string MaterialCode, string MaterialName, MaterialUnit Unit, decimal BathPercent, decimal? EstimatedQuantity, decimal AvailableQuantity);

public sealed record DokumaSizingPlanRecipeDto(
    int Id,
    string Code,
    string Name,
    bool IsDefault,
    decimal? ConcentrationPercent,
    decimal? BathTemperatureC,
    decimal? ViscositySeconds,
    decimal? MachineSpeedMetersPerMinute,
    decimal? TargetPickUpPercent,
    string? Notes,
    IReadOnlyList<DokumaSizingPlanLineDto> Lines);

/// <summary>Leventin haşılı için seçilebilecek reçeteler ve tahmini kimyasal miktarları.</summary>
/// <param name="EstimatedYarnKilograms">Leventteki çözgü ipliğinin tahmini ağırlığı: tel × metre × tex (artikelin çözgü iplik oranlarıyla).</param>
/// <param name="CurrentRecipeId">Levente daha önce seçilmiş reçete; yoksa artikelin varsayılan reçetesi.</param>
public sealed record DokumaBeamSizingPlanDto(
    int BeamId,
    string BeamNo,
    int ArticleId,
    string ArticleCode,
    string ArticleName,
    decimal Meters,
    int Ends,
    decimal? EstimatedYarnKilograms,
    int? CurrentRecipeId,
    IReadOnlyList<DokumaSizingPlanRecipeDto> Recipes);

/// <summary>Leventte kullanılan haşıl kimyasalı (malzeme hareketlerinden).</summary>
public sealed record DokumaBeamSizingChemicalDto(string MaterialCode, string MaterialName, MaterialUnit Unit, decimal BathPercent, decimal Quantity);

public sealed record DokumaSizingDocumentYarnDto(string YarnCode, string YarnName, string? YarnColor, int Ends);

/// <summary>Haşılcıya gönderme belgesi: levent, artikel, iplik ve reçete bilgisi.</summary>
public sealed record DokumaBeamSizingDocumentDto(
    int BeamId,
    string BeamNo,
    string WarpNo,
    string OrderNo,
    string? CustomerName,
    string ArticleCode,
    string ArticleName,
    string? Composition,
    decimal Meters,
    int Ends,
    decimal? EstimatedYarnKilograms,
    SizingMode SizingMode,
    BeamStatus Status,
    string? SupplierCode,
    string? SupplierName,
    string? SupplierTaxNumber,
    string? SupplierPhone,
    DateOnly? SentToSizingOn,
    DateOnly? SizedOn,
    decimal? PickUpPercent,
    decimal? ViscositySeconds,
    string? Notes,
    IReadOnlyList<DokumaSizingDocumentYarnDto> Yarns,
    DokumaSizingPlanRecipeDto? Recipe,
    IReadOnlyList<DokumaBeamSizingChemicalDto> Chemicals);

public sealed record DokumaBeamReadyRequest(DrawingMethod DrawingMethod, DateOnly Date);

public sealed class CreateDokumaWarpOrderRequestValidator : AbstractValidator<CreateDokumaWarpOrderRequest>
{
    public CreateDokumaWarpOrderRequestValidator()
    {
        RuleFor(x => x.OrderLineId).GreaterThan(0).WithMessage("Sipariş satırı seçilmelidir.");
        RuleFor(x => x.PlannedDate).NotEmpty().WithMessage("Plan tarihi zorunludur.");
        RuleFor(x => x.PlannedBeamCount).InclusiveBetween(1, 200).WithMessage("Levent sayısı 1 ile 200 arasında olmalıdır.");
        RuleFor(x => x.MetersPerBeam)
            .GreaterThan(0).WithMessage("Levent metresi sıfırdan büyük olmalıdır.")
            .LessThanOrEqualTo(100_000).WithMessage("Levent metresi çok büyük.")
            .PrecisionScale(12, 2, true).WithMessage("Levent metresi en fazla iki ondalık olabilir.");
        RuleFor(x => x.Notes).OptionalText(1000);
    }
}

public sealed class UpdateDokumaWarpOrderRequestValidator : AbstractValidator<UpdateDokumaWarpOrderRequest>
{
    public UpdateDokumaWarpOrderRequestValidator()
    {
        RuleFor(x => x.PlannedDate).NotEmpty().WithMessage("Plan tarihi zorunludur.");
        RuleFor(x => x.PlannedBeamCount).InclusiveBetween(1, 200).WithMessage("Levent sayısı 1 ile 200 arasında olmalıdır.");
        RuleFor(x => x.MetersPerBeam)
            .GreaterThan(0).WithMessage("Levent metresi sıfırdan büyük olmalıdır.")
            .LessThanOrEqualTo(100_000).WithMessage("Levent metresi çok büyük.")
            .PrecisionScale(12, 2, true).WithMessage("Levent metresi en fazla iki ondalık olabilir.");
        RuleFor(x => x.Notes).OptionalText(1000);
    }
}

public sealed class DokumaWarpIssueRequestValidator : AbstractValidator<DokumaWarpIssueRequest>
{
    public DokumaWarpIssueRequestValidator()
    {
        RuleFor(x => x.YarnLotId).GreaterThan(0).WithMessage("İplik lotu seçilmelidir.");
        RuleFor(x => x.IssuedOn).NotEmpty().WithMessage("Çıkış tarihi zorunludur.");
        RuleFor(x => x.Kilograms)
            .GreaterThan(0).WithMessage("Kilo sıfırdan büyük olmalıdır.")
            .PrecisionScale(12, 2, true).WithMessage("Kilo en fazla iki ondalık olabilir.");
    }
}

public sealed class DokumaBeamRequestValidator : AbstractValidator<DokumaBeamRequest>
{
    public DokumaBeamRequestValidator()
    {
        RuleFor(x => x.Meters)
            .GreaterThan(0).WithMessage("Levent metresi sıfırdan büyük olmalıdır.")
            .LessThanOrEqualTo(100_000).WithMessage("Levent metresi çok büyük.")
            .PrecisionScale(12, 2, true).WithMessage("Levent metresi en fazla iki ondalık olabilir.");
        RuleFor(x => x.SizingMode).IsInEnum().WithMessage("Geçerli bir haşıl şekli seçin.");
        RuleFor(x => x.Ends).InclusiveBetween(1, 100_000).WithMessage("Tel sayısı 1 ile 100.000 arasında olmalıdır.").When(x => x.Ends is not null);
        RuleFor(x => x.Notes).OptionalText(500);
    }
}

public sealed class SendDokumaBeamToSizingRequestValidator : AbstractValidator<SendDokumaBeamToSizingRequest>
{
    public SendDokumaBeamToSizingRequestValidator()
    {
        RuleFor(x => x.SupplierId).GreaterThan(0).WithMessage("Haşılcı seçilmelidir.");
        RuleFor(x => x.Date).NotEmpty().WithMessage("Gönderim tarihi zorunludur.");
    }
}

public sealed class ReceiveDokumaBeamFromSizingRequestValidator : AbstractValidator<ReceiveDokumaBeamFromSizingRequest>
{
    public ReceiveDokumaBeamFromSizingRequestValidator()
    {
        RuleFor(x => x.Date).NotEmpty().WithMessage("Geri alım tarihi zorunludur.");
        RuleFor(x => x.PickUpPercent)
            .InclusiveBetween(0, 500).WithMessage("Alım 0 ile 500 arasında olmalıdır.")
            .PrecisionScale(5, 2, true).WithMessage("Alım en fazla iki ondalık olabilir.")
            .When(x => x.PickUpPercent is not null);
        RuleFor(x => x.ViscositySeconds)
            .GreaterThan(0).WithMessage("Viskozite sıfırdan büyük olmalıdır.")
            .LessThanOrEqualTo(10_000).WithMessage("Viskozite çok büyük.")
            .PrecisionScale(6, 1, true).WithMessage("Viskozite en fazla bir ondalık olabilir.")
            .When(x => x.ViscositySeconds is not null);
    }
}

public sealed class MarkDokumaBeamSizedRequestValidator : AbstractValidator<MarkDokumaBeamSizedRequest>
{
    public MarkDokumaBeamSizedRequestValidator()
    {
        RuleFor(x => x.Date).NotEmpty().WithMessage("Haşıl tarihi zorunludur.");
        RuleFor(x => x.PickUpPercent)
            .InclusiveBetween(0, 500).WithMessage("Alım 0 ile 500 arasında olmalıdır.")
            .PrecisionScale(5, 2, true).WithMessage("Alım en fazla iki ondalık olabilir.")
            .When(x => x.PickUpPercent is not null);
        RuleFor(x => x.ViscositySeconds)
            .GreaterThan(0).WithMessage("Viskozite sıfırdan büyük olmalıdır.")
            .LessThanOrEqualTo(10_000).WithMessage("Viskozite çok büyük.")
            .PrecisionScale(6, 1, true).WithMessage("Viskozite en fazla bir ondalık olabilir.")
            .When(x => x.ViscositySeconds is not null);
        RuleFor(x => x.Chemicals)
            .Must(list => list!.Count <= 30).WithMessage("En fazla 30 kimyasal girilebilir.")
            .Must(list => list!.Select(c => c.MaterialId).Distinct().Count() == list!.Count).WithMessage("Aynı kimyasal iki satırda olamaz.")
            .When(x => x.Chemicals is not null);
        RuleForEach(x => x.Chemicals).ChildRules(chemical =>
        {
            chemical.RuleFor(c => c.MaterialId).GreaterThan(0).WithMessage("Kimyasal seçilmelidir.");
            chemical.RuleFor(c => c.Quantity)
                .GreaterThan(0).WithMessage("Kullanılan miktar sıfırdan büyük olmalıdır.")
                .LessThanOrEqualTo(999_999).WithMessage("Kullanılan miktar çok büyük.")
                .PrecisionScale(14, 3, true).WithMessage("Miktar en fazla üç ondalık olabilir.");
        });
    }
}

public sealed class DokumaBeamReadyRequestValidator : AbstractValidator<DokumaBeamReadyRequest>
{
    public DokumaBeamReadyRequestValidator()
    {
        RuleFor(x => x.DrawingMethod).IsInEnum().WithMessage("Tahar ya da düğüm seçilmelidir.");
        RuleFor(x => x.Date).NotEmpty().WithMessage("Tarih zorunludur.");
    }
}

public interface IDokumaWarpService
{
    Task<PagedResult<DokumaWarpOrderListItemDto>> GetListAsync(DokumaWarpOrderQuery query, CancellationToken cancellationToken);
    Task<Result<DokumaWarpOrderDto>> GetAsync(int id, CancellationToken cancellationToken);
    Task<Result<DokumaWarpOrderDto>> CreateAsync(CreateDokumaWarpOrderRequest request, CancellationToken cancellationToken);
    Task<Result<DokumaWarpOrderDto>> UpdateAsync(int id, UpdateDokumaWarpOrderRequest request, CancellationToken cancellationToken);
    Task<Result<DokumaWarpOrderDto>> CompleteAsync(int id, CancellationToken cancellationToken);
    Task<Result<DokumaWarpOrderDto>> CancelAsync(int id, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<DokumaIssuableLotDto>>> GetIssuableLotsAsync(int id, CancellationToken cancellationToken);
    Task<Result<DokumaWarpOrderDto>> AddIssueAsync(int id, DokumaWarpIssueRequest request, CancellationToken cancellationToken);
    Task<Result<DokumaWarpOrderDto>> RemoveIssueAsync(int id, int issueId, CancellationToken cancellationToken);
    Task<Result<DokumaWarpOrderDto>> AddBeamAsync(int id, DokumaBeamRequest request, CancellationToken cancellationToken);
    Task<Result<DokumaWarpOrderDto>> UpdateBeamAsync(int id, int beamId, DokumaBeamRequest request, CancellationToken cancellationToken);
    Task<Result<DokumaWarpOrderDto>> RemoveBeamAsync(int id, int beamId, CancellationToken cancellationToken);
    Task<PagedResult<DokumaBeamDto>> GetBeamsAsync(DokumaBeamQuery query, CancellationToken cancellationToken);
    Task<Result<DokumaBeamDto>> SendToSizingAsync(int beamId, SendDokumaBeamToSizingRequest request, CancellationToken cancellationToken);
    Task<Result<DokumaBeamDto>> ReceiveFromSizingAsync(int beamId, ReceiveDokumaBeamFromSizingRequest request, CancellationToken cancellationToken);
    Task<Result<DokumaBeamDto>> MarkSizedAsync(int beamId, MarkDokumaBeamSizedRequest request, CancellationToken cancellationToken);
    Task<Result<DokumaBeamSizingPlanDto>> GetSizingPlanAsync(int beamId, CancellationToken cancellationToken);
    Task<Result<DokumaBeamSizingDocumentDto>> GetSizingDocumentAsync(int beamId, CancellationToken cancellationToken);
    Task<Result<DokumaBeamDto>> MarkReadyAsync(int beamId, DokumaBeamReadyRequest request, CancellationToken cancellationToken);
    Task<Result<DokumaBeamDto>> UndoReadyAsync(int beamId, CancellationToken cancellationToken);
}

/// <summary>
/// Çözgü: emir onaylı sipariş satırından açılır, iplik lotlardan kesin düşülür, leventler kaydedilir. Levent haşıl (içeride
/// ya da fason) ve tahar / düğümden geçip tezgâha hazır olur.
/// </summary>
public sealed class DokumaWarpService(IAppDbContext db, IDocumentNumberGenerator numbers, IAppLock appLock) : IDokumaWarpService
{
    /// <summary>İplik lotlarından düşüm aynı anda iki istekle yapılırsa kalan miktar aşılmasın diye alınan kilit.</summary>
    public const string YarnLotLock = "DokumaYarnLots";

    private const string WarpSeries = "DokumaWarpOrder";
    private const string BeamSeries = "DokumaBeam";
    private const string InvalidState = "DOKUMA_WARP_INVALID_STATE";
    private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(10);

    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Çözgü emri bulunamadı.", ErrorType.NotFound);
    private static readonly Error BeamNotFound = new(ErrorCodes.NotFound, "Levent bulunamadı.", ErrorType.NotFound);
    private static readonly Error Busy = new("DOKUMA_BUSY", "Aynı anda başka bir iplik işlemi yapılıyor; tekrar deneyin.", ErrorType.Conflict);
    private static readonly Error NotOpen = new(InvalidState, "Yalnızca süren çözgü emrinde değişiklik yapılabilir.", ErrorType.Conflict);

    public Task<PagedResult<DokumaWarpOrderListItemDto>> GetListAsync(DokumaWarpOrderQuery query, CancellationToken cancellationToken)
    {
        var warps = db.Set<DokumaWarpOrder>().AsNoTracking();
        if (NullIfBlank(query.Search) is { } term)
        {
            warps = warps.Where(w => w.WarpNo.Contains(term)
                || w.OrderLine.Order.OrderNo.Contains(term)
                || w.OrderLine.Article.ArticleCode.Contains(term)
                || w.OrderLine.Article.Name.Contains(term)
                || (w.OrderLine.Order.Customer != null && w.OrderLine.Order.Customer.Name.Contains(term)));
        }

        return warps
            .OrderByDescending(w => w.PlannedDate).ThenByDescending(w => w.Id)
            .Select(w => new DokumaWarpOrderListItemDto(
                w.Id,
                w.WarpNo,
                w.PlannedDate,
                w.OrderLine.OrderId,
                w.OrderLine.Order.OrderNo,
                w.OrderLine.Order.Customer != null ? w.OrderLine.Order.Customer.Name : null,
                w.OrderLine.Article.ArticleCode,
                w.OrderLine.Article.Name,
                w.PlannedBeamCount,
                w.MetersPerBeam,
                w.Ends,
                w.Beams.Count,
                w.Issues.Sum(i => (decimal?)i.Kilograms) ?? 0,
                w.Status))
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<Result<DokumaWarpOrderDto>> GetAsync(int id, CancellationToken cancellationToken)
    {
        var warp = await db.Set<DokumaWarpOrder>().AsNoTracking()
            .Include(w => w.OrderLine).ThenInclude(l => l.Order).ThenInclude(o => o.Customer)
            .Include(w => w.OrderLine).ThenInclude(l => l.Article).ThenInclude(a => a.Yarns).ThenInclude(y => y.Yarn)
            .Include(w => w.Issues).ThenInclude(i => i.YarnLot).ThenInclude(l => l.Yarn)
            .Include(w => w.TakeBacks).ThenInclude(t => t.YarnLot).ThenInclude(l => l.Yarn)
            .SingleOrDefaultAsync(w => w.Id == id, cancellationToken);
        if (warp is null)
        {
            return NotFound;
        }

        var article = warp.OrderLine.Article;
        var totalMeters = warp.PlannedBeamCount * warp.MetersPerBeam;
        var issuedByYarn = warp.Issues.GroupBy(i => i.YarnLot.YarnId).ToDictionary(g => g.Key, g => g.Sum(i => i.Kilograms));
        foreach (var taken in warp.TakeBacks)
        {
            issuedByYarn[taken.YarnLot.YarnId] = issuedByYarn.GetValueOrDefault(taken.YarnLot.YarnId) - taken.Kilograms;
        }


        // Levent metresi iplik boyudur: çekme eklenmez, artikeldeki çözgü firesi eklenir.
        var yarns = article.Yarns
            .Where(y => y.Role == ArticleYarnRole.Warp)
            .OrderBy(y => y.Sequence)
            .GroupBy(y => y.YarnId)
            .Select(g =>
            {
                var yarn = g.First().Yarn;
                var ends = g.Sum(y => y.Ends);
                var tex = YarnRequirement.Tex(yarn.CountValue, yarn.CountSystem, yarn.Plies);
                var kilograms = ends * totalMeters * tex / 1_000_000m * (1 + (article.WarpWastePercent ?? 0) / 100m);
                return new DokumaWarpYarnDto(yarn.Id, yarn.YarnCode, yarn.Name, yarn.Color, ends, Math.Round(kilograms, 2), issuedByYarn.GetValueOrDefault(yarn.Id));
            })
            .ToList();

        var mixed = warp.Issues
            .GroupBy(i => i.YarnLot.Yarn.YarnCode)
            .Where(g => g.Select(i => NullIfBlank(i.YarnLot.ProducerLotNo) ?? i.YarnLot.LotNo).Distinct().Count() > 1)
            .Select(g => g.Key)
            .OrderBy(code => code)
            .ToList();

        var beams = await ProjectBeams(db.Set<DokumaBeam>().AsNoTracking().Where(b => b.WarpOrderId == id).OrderBy(b => b.Id)).ToListAsync(cancellationToken);

        return new DokumaWarpOrderDto(
            warp.Id,
            warp.WarpNo,
            warp.PlannedDate,
            warp.OrderLine.OrderId,
            warp.OrderLine.Order.OrderNo,
            warp.OrderLine.Order.Ownership,
            warp.OrderLine.Order.Customer?.Name,
            warp.OrderLineId,
            warp.OrderLine.Meters,
            article.ArticleCode,
            article.Name,
            warp.PlannedBeamCount,
            warp.MetersPerBeam,
            warp.Ends,
            warp.Status,
            warp.CompletedOn,
            warp.Notes,
            yarns,
            warp.Issues.OrderBy(i => i.Id).Select(i => new DokumaWarpIssueDto(
                i.Id, i.YarnLotId, i.YarnLot.LotNo, i.YarnLot.ProducerLotNo, i.YarnLot.Yarn.YarnCode, i.YarnLot.Yarn.Name, i.IssuedOn, i.Kilograms)).ToList(),
            beams,
            mixed,
            warp.TakeBacks.OrderBy(t => t.Id).Select(t => new DokumaYarnTakeBackDto(t.Id, t.YarnLotId, t.YarnLot.LotNo, t.YarnLot.Yarn.YarnCode, t.YarnLot.Yarn.Name, t.TakenOn, t.Kilograms, t.Notes)).ToList());
    }

    public async Task<Result<DokumaWarpOrderDto>> CreateAsync(CreateDokumaWarpOrderRequest request, CancellationToken cancellationToken)
    {
        var line = await db.Set<DokumaOrderLine>()
            .Include(l => l.Order)
            .Include(l => l.Article).ThenInclude(a => a.Yarns)
            .SingleOrDefaultAsync(l => l.Id == request.OrderLineId, cancellationToken);
        if (line is null)
        {
            return ReferenceValidation.InvalidReference("orderLineId", "Sipariş satırı bulunamadı.");
        }

        if (line.Order.Status != OrderStatus.Confirmed)
        {
            return new Error(InvalidState, "Çözgü emri yalnızca onaylı siparişe açılır.", ErrorType.Conflict);
        }

        var ends = line.Article.Yarns.Where(y => y.Role == ArticleYarnRole.Warp).Sum(y => y.Ends);
        if (ends == 0)
        {
            return new Error(InvalidState, "Artikelde çözgü ipliği tanımlı değil.", ErrorType.Conflict);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var year = request.PlannedDate.Year;
        var sequence = await numbers.NextAsync(WarpSeries, year, cancellationToken);
        var warp = new DokumaWarpOrder
        {
            WarpNo = $"CE-{year}-{sequence:D5}",
            OrderLineId = line.Id,
            PlannedDate = request.PlannedDate,
            PlannedBeamCount = request.PlannedBeamCount,
            MetersPerBeam = request.MetersPerBeam,
            Ends = ends,
            Notes = NullIfBlank(request.Notes),
        };
        db.Set<DokumaWarpOrder>().Add(warp);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(warp.Id, cancellationToken);
    }

    public async Task<Result<DokumaWarpOrderDto>> UpdateAsync(int id, UpdateDokumaWarpOrderRequest request, CancellationToken cancellationToken)
    {
        var warp = await db.Set<DokumaWarpOrder>().SingleOrDefaultAsync(w => w.Id == id, cancellationToken);
        if (warp is null)
        {
            return NotFound;
        }

        if (warp.Status != WarpOrderStatus.Open)
        {
            return NotOpen;
        }

        // Planlanan levent sayısı kaydedilmiş leventlerin altına indirilemez.
        var recorded = await db.Set<DokumaBeam>().CountAsync(b => b.WarpOrderId == id, cancellationToken);
        if (request.PlannedBeamCount < recorded)
        {
            return new Error(
                InvalidState,
                $"Bu çözgü emrinde {recorded} levent kaydedilmiş; planlanan levent sayısı kaydedilen levent sayısının altına indirilemez.",
                ErrorType.Conflict);
        }

        warp.PlannedDate = request.PlannedDate;
        warp.PlannedBeamCount = request.PlannedBeamCount;
        warp.MetersPerBeam = request.MetersPerBeam;
        warp.Notes = NullIfBlank(request.Notes);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<Result<DokumaWarpOrderDto>> CompleteAsync(int id, CancellationToken cancellationToken)
    {
        var warp = await db.Set<DokumaWarpOrder>().Include(w => w.Beams).SingleOrDefaultAsync(w => w.Id == id, cancellationToken);
        if (warp is null)
        {
            return NotFound;
        }

        if (warp.Status != WarpOrderStatus.Open)
        {
            return NotOpen;
        }

        if (warp.Beams.Count == 0)
        {
            return new Error(InvalidState, "Çözgü bitirilmeden önce en az bir levent kaydedilmelidir.", ErrorType.Conflict);
        }

        warp.Status = WarpOrderStatus.Completed;
        warp.CompletedOn = DateOnly.FromDateTime(DateTime.Today);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    /// <summary>Levent kaydedilmemiş çözgü emrini iptal eder; verilen iplik lotlara geri döner.</summary>
    public async Task<Result<DokumaWarpOrderDto>> CancelAsync(int id, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await appLock.TryAcquireAsync(YarnLotLock, LockTimeout, cancellationToken))
        {
            return Busy;
        }

        var warp = await db.Set<DokumaWarpOrder>().Include(w => w.Issues).Include(w => w.Beams).SingleOrDefaultAsync(w => w.Id == id, cancellationToken);
        if (warp is null)
        {
            return NotFound;
        }

        if (warp.Status != WarpOrderStatus.Open)
        {
            return NotOpen;
        }

        if (warp.Beams.Count > 0)
        {
            return new Error(InvalidState, "Levent kaydedilmiş çözgü emri iptal edilemez; önce leventleri silin.", ErrorType.Conflict);
        }

        db.Set<DokumaWarpIssue>().RemoveRange(warp.Issues);
        warp.Status = WarpOrderStatus.Cancelled;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    /// <summary>Çözgüye verilebilecek lotlar: artikelin çözgü iplikleri, malın sahibi siparişle aynı, kalan miktarı olan.</summary>
    public async Task<Result<IReadOnlyList<DokumaIssuableLotDto>>> GetIssuableLotsAsync(int id, CancellationToken cancellationToken)
    {
        var warp = await db.Set<DokumaWarpOrder>().AsNoTracking()
            .Include(w => w.OrderLine).ThenInclude(l => l.Order)
            .Include(w => w.OrderLine).ThenInclude(l => l.Article).ThenInclude(a => a.Yarns)
            .SingleOrDefaultAsync(w => w.Id == id, cancellationToken);
        if (warp is null)
        {
            return NotFound;
        }

        var owner = OwnerOf(warp.OrderLine.Order);
        var yarnIds = warp.OrderLine.Article.Yarns.Where(y => y.Role == ArticleYarnRole.Warp).Select(y => y.YarnId).Distinct().ToList();
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

    public async Task<Result<DokumaWarpOrderDto>> AddIssueAsync(int id, DokumaWarpIssueRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await appLock.TryAcquireAsync(YarnLotLock, LockTimeout, cancellationToken))
        {
            return Busy;
        }

        var warp = await db.Set<DokumaWarpOrder>()
            .Include(w => w.OrderLine).ThenInclude(l => l.Order)
            .Include(w => w.OrderLine).ThenInclude(l => l.Article).ThenInclude(a => a.Yarns)
            .SingleOrDefaultAsync(w => w.Id == id, cancellationToken);
        if (warp is null)
        {
            return NotFound;
        }

        if (warp.Status != WarpOrderStatus.Open)
        {
            return NotOpen;
        }

        var lot = await db.Set<DokumaYarnLot>().SingleOrDefaultAsync(l => l.Id == request.YarnLotId, cancellationToken);
        if (lot is null)
        {
            return ReferenceValidation.InvalidReference("yarnLotId", "İplik lotu bulunamadı.");
        }

        if (lot.CustomerId != OwnerOf(warp.OrderLine.Order))
        {
            return ReferenceValidation.InvalidReference("yarnLotId", "Lotun sahibi siparişle aynı değil: fasonda müşterinin, kendi malında işletmenin ipliği verilir.");
        }

        if (warp.OrderLine.Article.Yarns.All(y => y.Role != ArticleYarnRole.Warp || y.YarnId != lot.YarnId))
        {
            return ReferenceValidation.InvalidReference("yarnLotId", "Bu iplik artikelin çözgüsünde yok.");
        }

        var issued = await DokumaYarnStock.IssuedFromLotAsync(db, lot.Id, cancellationToken);
        var remaining = lot.Kilograms - issued;
        if (request.Kilograms > remaining)
        {
            return new Error(InvalidState, $"{lot.LotNo} lotunda {remaining:0.##} kg kaldı; daha fazlası verilemez.", ErrorType.Conflict);
        }

        db.Set<DokumaWarpIssue>().Add(new DokumaWarpIssue
        {
            WarpOrderId = id,
            YarnLotId = lot.Id,
            IssuedOn = request.IssuedOn,
            Kilograms = request.Kilograms,
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    /// <summary>Yanlış girilen çıkışı geri alır; iplik lota döner. Yalnızca süren çözgüde.</summary>
    public async Task<Result<DokumaWarpOrderDto>> RemoveIssueAsync(int id, int issueId, CancellationToken cancellationToken)
    {
        var issue = await db.Set<DokumaWarpIssue>().Include(i => i.WarpOrder).SingleOrDefaultAsync(i => i.Id == issueId && i.WarpOrderId == id, cancellationToken);
        if (issue is null)
        {
            return new Error(ErrorCodes.NotFound, "İplik çıkışı bulunamadı.", ErrorType.NotFound);
        }

        if (issue.WarpOrder.Status != WarpOrderStatus.Open)
        {
            return NotOpen;
        }

        db.Set<DokumaWarpIssue>().Remove(issue);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<Result<DokumaWarpOrderDto>> AddBeamAsync(int id, DokumaBeamRequest request, CancellationToken cancellationToken)
    {
        var warp = await db.Set<DokumaWarpOrder>().SingleOrDefaultAsync(w => w.Id == id, cancellationToken);
        if (warp is null)
        {
            return NotFound;
        }

        if (warp.Status != WarpOrderStatus.Open)
        {
            return NotOpen;
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var year = DateTime.Today.Year;
        var sequence = await numbers.NextAsync(BeamSeries, year, cancellationToken);
        db.Set<DokumaBeam>().Add(new DokumaBeam
        {
            BeamNo = $"LV-{year}-{sequence:D5}",
            WarpOrderId = id,
            Meters = request.Meters,
            Ends = request.Ends ?? warp.Ends,
            SizingMode = request.SizingMode,
            Notes = NullIfBlank(request.Notes),
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    /// <summary>Haşıl ya da tahar / düğüm başlamamış leventin ölçüsü ve haşıl şekli düzeltilir.</summary>
    public async Task<Result<DokumaWarpOrderDto>> UpdateBeamAsync(int id, int beamId, DokumaBeamRequest request, CancellationToken cancellationToken)
    {
        var beam = await db.Set<DokumaBeam>().Include(b => b.WarpOrder).SingleOrDefaultAsync(b => b.Id == beamId && b.WarpOrderId == id, cancellationToken);
        if (beam is null)
        {
            return BeamNotFound;
        }

        if (beam.Status != BeamStatus.Warped || beam.IsSized)
        {
            return new Error(InvalidState, "Haşıla girmiş ya da tezgâha hazırlanmış levent düzeltilemez.", ErrorType.Conflict);
        }

        beam.Meters = request.Meters;
        beam.Ends = request.Ends ?? beam.WarpOrder.Ends;
        beam.SizingMode = request.SizingMode;
        beam.Notes = NullIfBlank(request.Notes);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<Result<DokumaWarpOrderDto>> RemoveBeamAsync(int id, int beamId, CancellationToken cancellationToken)
    {
        var beam = await db.Set<DokumaBeam>().Include(b => b.WarpOrder).SingleOrDefaultAsync(b => b.Id == beamId && b.WarpOrderId == id, cancellationToken);
        if (beam is null)
        {
            return BeamNotFound;
        }

        if (beam.WarpOrder.Status != WarpOrderStatus.Open)
        {
            return NotOpen;
        }

        if (beam.Status != BeamStatus.Warped || beam.IsSized)
        {
            return new Error(InvalidState, "Haşıla girmiş ya da tezgâha hazırlanmış levent silinemez.", ErrorType.Conflict);
        }

        db.Set<DokumaBeam>().Remove(beam);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public Task<PagedResult<DokumaBeamDto>> GetBeamsAsync(DokumaBeamQuery query, CancellationToken cancellationToken)
    {
        var beams = db.Set<DokumaBeam>().AsNoTracking();
        if (NullIfBlank(query.Search) is { } term)
        {
            beams = beams.Where(b => b.BeamNo.Contains(term)
                || b.WarpOrder.WarpNo.Contains(term)
                || b.WarpOrder.OrderLine.Order.OrderNo.Contains(term)
                || b.WarpOrder.OrderLine.Article.ArticleCode.Contains(term));
        }

        return ProjectBeams(beams.OrderByDescending(b => b.Id)).ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<Result<DokumaBeamDto>> SendToSizingAsync(int beamId, SendDokumaBeamToSizingRequest request, CancellationToken cancellationToken)
    {
        var beam = await db.Set<DokumaBeam>().SingleOrDefaultAsync(b => b.Id == beamId, cancellationToken);
        if (beam is null)
        {
            return BeamNotFound;
        }

        if (beam.SizingMode != SizingMode.Commission || beam.Status != BeamStatus.Warped || beam.IsSized)
        {
            return new Error(InvalidState, "Yalnızca fason haşıllı, henüz haşıllanmamış levent haşılcıya gönderilir.", ErrorType.Conflict);
        }

        if (await ReferenceValidation.RequireSelectableAsync(db.Set<DokumaSupplier>(), request.SupplierId, null, "supplierId", "Haşılcı", cancellationToken) is { } error)
        {
            return error;
        }

        if (await ValidateRecipeAsync(beam, request.SizingRecipeId, cancellationToken) is { } recipeError)
        {
            return recipeError;
        }

        beam.SizingSupplierId = request.SupplierId;
        beam.SentToSizingOn = request.Date;
        beam.SizingRecipeId = request.SizingRecipeId;
        beam.Status = BeamStatus.AtSizing;
        await db.SaveChangesAsync(cancellationToken);
        return await GetBeamAsync(beamId, cancellationToken);
    }

    public async Task<Result<DokumaBeamDto>> ReceiveFromSizingAsync(int beamId, ReceiveDokumaBeamFromSizingRequest request, CancellationToken cancellationToken)
    {
        var beam = await db.Set<DokumaBeam>().SingleOrDefaultAsync(b => b.Id == beamId, cancellationToken);
        if (beam is null)
        {
            return BeamNotFound;
        }

        if (beam.Status != BeamStatus.AtSizing)
        {
            return new Error(InvalidState, "Levent haşılcıda değil.", ErrorType.Conflict);
        }

        if (request.Date < beam.SentToSizingOn)
        {
            return ReferenceValidation.InvalidReference("date", "Geri alım tarihi gönderim tarihinden önce olamaz.");
        }

        beam.IsSized = true;
        beam.SizedOn = request.Date;
        beam.SizingPickUpPercent = request.PickUpPercent;
        beam.SizingViscositySeconds = request.ViscositySeconds;
        beam.Status = BeamStatus.Warped;
        await db.SaveChangesAsync(cancellationToken);
        return await GetBeamAsync(beamId, cancellationToken);
    }

    /// <summary>
    /// İçeride haşıl: reçete, gerçekleşen alım ve viskozite kaydedilir; kullanılan kimyasallar malzeme stoğundan en eski
    /// lottan başlayarak düşülür. Stok yetmezse levent haşıllanmış sayılmaz.
    /// </summary>
    public async Task<Result<DokumaBeamDto>> MarkSizedAsync(int beamId, MarkDokumaBeamSizedRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var chemicals = request.Chemicals ?? [];
        if (chemicals.Count > 0
            && !await appLock.TryAcquireAsync(DokumaMaterialStockRules.MaterialLock, DokumaMaterialStockRules.LockTimeout, cancellationToken))
        {
            return DokumaMaterialStockRules.Busy;
        }

        var beam = await db.Set<DokumaBeam>().Include(b => b.WarpOrder).ThenInclude(w => w.OrderLine).SingleOrDefaultAsync(b => b.Id == beamId, cancellationToken);
        if (beam is null)
        {
            return BeamNotFound;
        }

        if (beam.SizingMode != SizingMode.InHouse || beam.Status != BeamStatus.Warped || beam.IsSized)
        {
            return new Error(InvalidState, "Yalnızca içeride haşıllanacak, henüz haşıllanmamış levent işaretlenir.", ErrorType.Conflict);
        }

        if (await ValidateRecipeAsync(beam, request.SizingRecipeId, cancellationToken) is { } recipeError)
        {
            return recipeError;
        }

        var materialIds = chemicals.Select(c => c.MaterialId).Distinct().ToList();
        if (materialIds.Count > 0
            && await db.Set<DokumaMaterial>().CountAsync(m => materialIds.Contains(m.Id), cancellationToken) != materialIds.Count)
        {
            return ReferenceValidation.InvalidReference("chemicals", "Kimyasallardan biri bulunamadı.");
        }

        var reason = request.SizingRecipeId is { } recipeId
            ? await db.Set<DokumaSizingRecipe>().Where(r => r.Id == recipeId).Select(r => "Haşıl reçetesi " + r.Code).SingleAsync(cancellationToken)
            : "Haşıl";
        foreach (var chemical in chemicals)
        {
            var error = await DokumaMaterialStockRules.AllocateAsync(db, chemical.MaterialId, chemical.Quantity, () => new DokumaMaterialMovement
            {
                Type = MaterialMovementType.SizingConsumption,
                MovementDate = request.Date,
                BeamId = beam.Id,
                Reference = beam.BeamNo,
                Reason = reason,
            }, cancellationToken);
            if (error is not null)
            {
                return error;
            }
        }

        beam.IsSized = true;
        beam.SizedOn = request.Date;
        beam.SizingRecipeId = request.SizingRecipeId;
        beam.SizingPickUpPercent = request.PickUpPercent;
        beam.SizingViscositySeconds = request.ViscositySeconds;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetBeamAsync(beamId, cancellationToken);
    }

    /// <summary>Haşıl için artikelin etkin reçeteleri, leventin tahmini ipliği ve kimyasal stoğu.</summary>
    public async Task<Result<DokumaBeamSizingPlanDto>> GetSizingPlanAsync(int beamId, CancellationToken cancellationToken)
    {
        var beam = await LoadBeamForSizingAsync(beamId, cancellationToken);
        if (beam is null)
        {
            return BeamNotFound;
        }

        var article = beam.WarpOrder.OrderLine.Article;
        var yarnKilograms = EstimateYarnKilograms(beam, article);
        var recipes = await db.Set<DokumaSizingRecipe>().AsNoTracking()
            .Include(r => r.Lines).ThenInclude(l => l.Material)
            .Where(r => r.ArticleId == article.Id && (r.IsActive || r.Id == beam.SizingRecipeId))
            .OrderByDescending(r => r.IsDefault).ThenBy(r => r.Code)
            .ToListAsync(cancellationToken);
        var stock = await DokumaMaterialStockRules.OnHandAsync(db, recipes.SelectMany(r => r.Lines).Select(l => l.MaterialId).Distinct().ToList(), cancellationToken);

        return new DokumaBeamSizingPlanDto(
            beam.Id,
            beam.BeamNo,
            article.Id,
            article.ArticleCode,
            article.Name,
            beam.Meters,
            beam.Ends,
            yarnKilograms,
            beam.SizingRecipeId ?? recipes.FirstOrDefault(r => r.IsDefault && r.IsActive)?.Id,
            recipes.Select(r => ToPlanRecipe(r, yarnKilograms, stock)).ToList());
    }

    /// <summary>Haşılcıya gönderme belgesi; fasonda reçete bilgi amaçlıdır. İçeride haşılda kullanılan kimyasalları da gösterir.</summary>
    public async Task<Result<DokumaBeamSizingDocumentDto>> GetSizingDocumentAsync(int beamId, CancellationToken cancellationToken)
    {
        var beam = await LoadBeamForSizingAsync(beamId, cancellationToken);
        if (beam is null)
        {
            return BeamNotFound;
        }

        if (beam.SizingMode == SizingMode.None)
        {
            return new Error(InvalidState, "Haşılsız leventin haşıl belgesi yoktur.", ErrorType.Conflict);
        }

        var article = beam.WarpOrder.OrderLine.Article;
        var yarnKilograms = EstimateYarnKilograms(beam, article);
        DokumaSizingPlanRecipeDto? recipe = null;
        var bathPercents = new Dictionary<int, decimal>();
        if (beam.SizingRecipeId is { } recipeId)
        {
            var entity = await db.Set<DokumaSizingRecipe>().AsNoTracking().IgnoreQueryFilters()
                .Include(r => r.Lines).ThenInclude(l => l.Material)
                .SingleAsync(r => r.Id == recipeId, cancellationToken);
            recipe = ToPlanRecipe(entity, yarnKilograms, new Dictionary<int, decimal>());
            bathPercents = entity.Lines.GroupBy(l => l.MaterialId).ToDictionary(g => g.Key, g => g.First().BathPercent);
        }

        var used = await db.Set<DokumaMaterialMovement>().AsNoTracking()
            .Where(m => m.BeamId == beamId && m.Type == MaterialMovementType.SizingConsumption)
            .GroupBy(m => new { m.MaterialId, m.Material.Code, m.Material.Name, m.Material.Unit })
            .Select(g => new { g.Key.MaterialId, g.Key.Code, g.Key.Name, g.Key.Unit, Quantity = -g.Sum(m => m.Quantity) })
            .ToListAsync(cancellationToken);

        var supplier = beam.SizingSupplier;
        return new DokumaBeamSizingDocumentDto(
            beam.Id,
            beam.BeamNo,
            beam.WarpOrder.WarpNo,
            beam.WarpOrder.OrderLine.Order.OrderNo,
            beam.WarpOrder.OrderLine.Order.Customer?.Name,
            article.ArticleCode,
            article.Name,
            article.Composition,
            beam.Meters,
            beam.Ends,
            yarnKilograms,
            beam.SizingMode,
            beam.Status,
            supplier?.Code,
            supplier?.Name,
            supplier?.TaxNumber,
            supplier?.Phone,
            beam.SentToSizingOn,
            beam.SizedOn,
            beam.SizingPickUpPercent,
            beam.SizingViscositySeconds,
            beam.Notes,
            article.Yarns.Where(y => y.Role == ArticleYarnRole.Warp).OrderBy(y => y.Sequence)
                .Select(y => new DokumaSizingDocumentYarnDto(y.Yarn.YarnCode, y.Yarn.Name, y.Yarn.Color, y.Ends)).ToList(),
            recipe,
            used.OrderBy(u => u.Code).Select(u => new DokumaBeamSizingChemicalDto(u.Code, u.Name, u.Unit, bathPercents.GetValueOrDefault(u.MaterialId), u.Quantity)).ToList());
    }

    private Task<DokumaBeam?> LoadBeamForSizingAsync(int beamId, CancellationToken cancellationToken) =>
        db.Set<DokumaBeam>().AsNoTracking()
            .Include(b => b.SizingSupplier)
            .Include(b => b.WarpOrder).ThenInclude(w => w.OrderLine).ThenInclude(l => l.Order).ThenInclude(o => o.Customer)
            .Include(b => b.WarpOrder).ThenInclude(w => w.OrderLine).ThenInclude(l => l.Article).ThenInclude(a => a.Yarns).ThenInclude(y => y.Yarn)
            .SingleOrDefaultAsync(b => b.Id == beamId, cancellationToken);

    /// <summary>Reçete leventin artikeline ait olmalı; yeni seçilen reçete etkin olmalıdır.</summary>
    private async Task<Error?> ValidateRecipeAsync(DokumaBeam beam, int? recipeId, CancellationToken cancellationToken)
    {
        if (recipeId is null)
        {
            return null;
        }

        var articleId = await db.Set<DokumaWarpOrder>().Where(w => w.Id == beam.WarpOrderId).Select(w => w.OrderLine.ArticleId).SingleAsync(cancellationToken);
        var recipe = await db.Set<DokumaSizingRecipe>().AsNoTracking().Where(r => r.Id == recipeId).Select(r => new { r.ArticleId, r.IsActive }).SingleOrDefaultAsync(cancellationToken);
        if (recipe is null)
        {
            return ReferenceValidation.InvalidReference("sizingRecipeId", "Haşıl reçetesi bulunamadı.");
        }

        if (recipe.ArticleId != articleId)
        {
            return ReferenceValidation.InvalidReference("sizingRecipeId", "Haşıl reçetesi leventin artikeline ait değil.");
        }

        return recipe.IsActive || recipeId == beam.SizingRecipeId ? null : ReferenceValidation.InvalidReference("sizingRecipeId", "Haşıl reçetesi pasif durumda.");
    }

    /// <summary>Leventteki çözgü ipliğinin tahmini ağırlığı (kg); artikelin çözgü ipliği yoksa boş.</summary>
    private static decimal? EstimateYarnKilograms(DokumaBeam beam, DokumaArticle article)
    {
        var warp = article.Yarns.Where(y => y.Role == ArticleYarnRole.Warp).ToList();
        var totalEnds = warp.Sum(y => y.Ends);
        if (totalEnds == 0)
        {
            return null;
        }

        var kilograms = warp.Sum(y =>
            (decimal)y.Ends / totalEnds * beam.Ends * beam.Meters * YarnRequirement.Tex(y.Yarn.CountValue, y.Yarn.CountSystem, y.Yarn.Plies) / 1_000_000m);
        return Math.Round(kilograms, 2);
    }

    private static DokumaSizingPlanRecipeDto ToPlanRecipe(DokumaSizingRecipe recipe, decimal? yarnKilograms, IReadOnlyDictionary<int, decimal> stock) => new(
        recipe.Id,
        recipe.Code,
        recipe.Name,
        recipe.IsDefault,
        recipe.ConcentrationPercent,
        recipe.BathTemperatureC,
        recipe.ViscositySeconds,
        recipe.MachineSpeedMetersPerMinute,
        recipe.TargetPickUpPercent,
        recipe.Notes,
        recipe.Lines.OrderBy(l => l.Sequence).Select(l => new DokumaSizingPlanLineDto(
            l.MaterialId,
            l.Material.Code,
            l.Material.Name,
            l.Material.Unit,
            l.BathPercent,
            yarnKilograms is { } kg && recipe.TargetPickUpPercent is { } pickUp ? Math.Round(kg * pickUp / 100m * l.BathPercent / 100m, 3) : null,
            stock.GetValueOrDefault(l.MaterialId))).ToList());

    public async Task<Result<DokumaBeamDto>> MarkReadyAsync(int beamId, DokumaBeamReadyRequest request, CancellationToken cancellationToken)
    {
        var beam = await db.Set<DokumaBeam>().SingleOrDefaultAsync(b => b.Id == beamId, cancellationToken);
        if (beam is null)
        {
            return BeamNotFound;
        }

        if (beam.Status != BeamStatus.Warped)
        {
            return new Error(InvalidState, "Yalnızca çözgüden çıkmış, haşılda olmayan levent tezgâha hazırlanır.", ErrorType.Conflict);
        }

        if (beam.SizingMode != SizingMode.None && !beam.IsSized)
        {
            return new Error(InvalidState, "Levent haşıllanmadan tezgâha hazırlanamaz.", ErrorType.Conflict);
        }

        beam.DrawingMethod = request.DrawingMethod;
        beam.ReadyOn = request.Date;
        beam.Status = BeamStatus.Ready;
        await db.SaveChangesAsync(cancellationToken);
        return await GetBeamAsync(beamId, cancellationToken);
    }

    /// <summary>Yanlışlıkla hazır işaretlenen leventi geri alır; tezgâha bağlanmışsa olmaz.</summary>
    public async Task<Result<DokumaBeamDto>> UndoReadyAsync(int beamId, CancellationToken cancellationToken)
    {
        var beam = await db.Set<DokumaBeam>().SingleOrDefaultAsync(b => b.Id == beamId, cancellationToken);
        if (beam is null)
        {
            return BeamNotFound;
        }

        if (beam.Status != BeamStatus.Ready)
        {
            return new Error(InvalidState, "Levent tezgâha hazır durumda değil.", ErrorType.Conflict);
        }

        beam.DrawingMethod = null;
        beam.ReadyOn = null;
        beam.Status = BeamStatus.Warped;
        await db.SaveChangesAsync(cancellationToken);
        return await GetBeamAsync(beamId, cancellationToken);
    }

    private async Task<Result<DokumaBeamDto>> GetBeamAsync(int beamId, CancellationToken cancellationToken) =>
        await ProjectBeams(db.Set<DokumaBeam>().AsNoTracking().Where(b => b.Id == beamId)).SingleAsync(cancellationToken);

    private static IQueryable<DokumaBeamDto> ProjectBeams(IQueryable<DokumaBeam> beams) => beams.Select(b => new DokumaBeamDto(
        b.Id,
        b.BeamNo,
        b.WarpOrderId,
        b.WarpOrder.WarpNo,
        b.WarpOrder.OrderLine.Order.OrderNo,
        b.WarpOrder.OrderLine.Order.Customer != null ? b.WarpOrder.OrderLine.Order.Customer.Name : null,
        b.WarpOrder.OrderLine.Article.ArticleCode,
        b.WarpOrder.OrderLine.Article.Name,
        b.Meters,
        b.Ends,
        b.SizingMode,
        b.IsSized,
        b.SizingSupplierId,
        b.SizingSupplier != null ? b.SizingSupplier.Name : null,
        b.SentToSizingOn,
        b.SizedOn,
        b.DrawingMethod,
        b.ReadyOn,
        b.Status,
        b.Notes,
        b.Loom != null ? b.Loom.Code : null,
        b.SizingRecipeId,
        b.SizingRecipe != null ? b.SizingRecipe.Code : null,
        b.SizingPickUpPercent,
        b.SizingViscositySeconds));

    /// <summary>Malın sahibi: fasonda siparişin müşterisi, kendi malında işletme (boş).</summary>
    private static int? OwnerOf(DokumaOrder order) => order.Ownership == OrderOwnership.Commission ? order.CustomerId : null;
}
