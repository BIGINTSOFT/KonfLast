using System.Linq.Expressions;
using ERP.Boyahane.Domain.Entities;
using ERP.Boyahane.Domain.Enums;
using ERP.Boyahane.Domain.Rules;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Boyahane.Application.Features.Inspection;

public sealed record BoyahaneDefectTypeDto(int Id, string Code, string Name, DefectCategory Category, bool IsActive);

public sealed record BoyahaneDefectTypeRequest(string Code, string Name, DefectCategory Category = DefectCategory.Appearance, bool IsActive = true);

public sealed class BoyahaneDefectTypeRequestValidator : AbstractValidator<BoyahaneDefectTypeRequest>
{
    public BoyahaneDefectTypeRequestValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName(100);
        RuleFor(x => x.Category).IsInEnum().WithMessage("Geçerli bir kategori seçin.");
    }
}

public sealed class BoyahaneDefectTypeService(IAppDbContext db)
    : MasterDataService<BoyahaneDefectType, BoyahaneDefectTypeDto, BoyahaneDefectTypeRequest>(db)
{
    protected override DbSet<BoyahaneDefectType> Set => Db.Set<BoyahaneDefectType>();
    protected override string Label => "Hata türü";
    protected override string CodeField => "code";

    protected override string CodeOf(BoyahaneDefectTypeRequest request) => request.Code;
    protected override Expression<Func<BoyahaneDefectType, bool>> WithCode(string code) => e => e.Code == code;
    protected override Expression<Func<BoyahaneDefectType, bool>> Matches(string term) => e => e.Code.Contains(term) || e.Name.Contains(term);
    protected override IQueryable<BoyahaneDefectType> Order(IQueryable<BoyahaneDefectType> query) => query.OrderBy(e => e.Category).ThenBy(e => e.Name);

    protected override Expression<Func<BoyahaneDefectType, BoyahaneDefectTypeDto>> ToDto =>
        e => new BoyahaneDefectTypeDto(e.Id, e.Code, e.Name, e.Category, e.IsActive);

    protected override Expression<Func<BoyahaneDefectType, LookupDto>> ToLookup => e => new LookupDto(e.Id, e.Code, e.Name);

    protected override void Apply(BoyahaneDefectTypeRequest request, BoyahaneDefectType entity, string code)
    {
        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.Category = request.Category;
        entity.IsActive = request.IsActive;
    }

    protected override async Task<string?> FindUsageAsync(int id, CancellationToken cancellationToken) =>
        await Db.Set<BoyahaneFabricRollDefect>().AnyAsync(d => d.DefectTypeId == id, cancellationToken) ? "top hata kayıtları" : null;
}

public sealed class BoyahaneFabricRollQuery : PageRequest
{
    /// <summary>Top no, lot no, müşteri top no ya da müşteri adında aranır.</summary>
    public string? Search { get; set; }
}

/// <param name="Meters">Geçerli metre: kontrolde ölçüldüyse o, değilse kabul değeri.</param>
/// <param name="Kilograms">Geçerli kilo: kontrolde tartıldıysa o, değilse kabul değeri.</param>
public sealed record BoyahaneFabricRollListItemDto(
    int Id,
    string RollNo,
    string? CustomerRollNo,
    string LotNo,
    int ReceiptId,
    DateOnly ReceiptDate,
    string? CustomerName,
    string FabricCode,
    string FabricName,
    decimal Meters,
    decimal Kilograms,
    int DefectCount,
    bool IsConditionallyApproved,
    FabricRollStatus Status);

public sealed record BoyahaneFabricRollDefectDto(int Id, int DefectTypeId, string DefectCode, string DefectName, int Quantity, decimal? AtMeter, string? Note);

/// <param name="ReceivedMeters">Kabulde girilen metre; kontrol ölçümü bunun üzerine yazılmaz.</param>
/// <param name="ReceivedKilograms">Kabulde girilen kilo.</param>
/// <param name="PendingRollsInLot">Lotta kontrol bekleyen top sayısı (toplu karar için).</param>
/// <param name="CanInspect">Müşteri kararı girildikten sonra kontrol değiştirilemez.</param>
public sealed record BoyahaneFabricRollDetailDto(
    int Id,
    string RollNo,
    string? CustomerRollNo,
    int LotId,
    string LotNo,
    int ReceiptId,
    string? CustomerName,
    string FabricCode,
    string FabricName,
    decimal RawWidthCm,
    int RawWeightGsm,
    decimal ReceivedMeters,
    decimal ReceivedKilograms,
    decimal? InspectedMeters,
    decimal? InspectedKilograms,
    bool MetersMeasured,
    bool KilogramsMeasured,
    decimal? InspectedWidthCm,
    int? InspectedWeightGsm,
    DateTime? InspectedAt,
    string? InspectedBy,
    string? InspectionNote,
    FabricRollStatus Status,
    bool IsConditionallyApproved,
    bool? CustomerDecisionAccepted,
    string? CustomerDecisionBy,
    CustomerDecisionChannel? CustomerDecisionChannel,
    DateOnly? CustomerDecisionOn,
    string? CustomerDecisionNote,
    string? CustomerDecisionRecordedBy,
    int PendingRollsInLot,
    bool CanInspect,
    IReadOnlyList<BoyahaneFabricRollDefectDto> Defects);

public sealed record BoyahaneFabricRollDefectRequest(int DefectTypeId, int Quantity = 1, decimal? AtMeter = null, string? Note = null);

/// <param name="Decision">Uygun, şartlı ya da red.</param>
public sealed record InspectBoyahaneFabricRollRequest(
    FabricRollStatus Decision,
    IReadOnlyList<BoyahaneFabricRollDefectRequest>? Defects = null,
    decimal? InspectedMeters = null,
    decimal? InspectedKilograms = null,
    decimal? InspectedWidthCm = null,
    int? InspectedWeightGsm = null,
    string? Note = null);

public sealed class InspectBoyahaneFabricRollRequestValidator : AbstractValidator<InspectBoyahaneFabricRollRequest>
{
    public const int MaxDefects = 100;

    public InspectBoyahaneFabricRollRequestValidator()
    {
        RuleFor(x => x.Decision)
            .Must(d => d is FabricRollStatus.Approved or FabricRollStatus.Conditional or FabricRollStatus.Rejected)
            .WithMessage("Karar uygun, şartlı ya da red olmalıdır.");
        RuleFor(x => x.InspectedMeters).GreaterThan(0).WithMessage("Metre sıfırdan büyük olmalıdır.").LessThanOrEqualTo(999_999).WithMessage("Metre sıfırdan büyük olmalıdır.").When(x => x.InspectedMeters is not null);
        RuleFor(x => x.InspectedKilograms).GreaterThan(0).WithMessage("Kilo sıfırdan büyük olmalıdır.").LessThanOrEqualTo(999_999).WithMessage("Kilo sıfırdan büyük olmalıdır.").When(x => x.InspectedKilograms is not null);
        RuleFor(x => x.InspectedWidthCm).InclusiveBetween(1, 1000).WithMessage("En 1 ile 1000 cm arasında olmalıdır.").When(x => x.InspectedWidthCm is not null);
        RuleFor(x => x.InspectedWeightGsm).InclusiveBetween(1, 5000).WithMessage("Gramaj 1 ile 5000 g/m² arasında olmalıdır.").When(x => x.InspectedWeightGsm is not null);
        RuleFor(x => x.Note).OptionalText(1000);

        // Şartlı ve red kararının dayanağı kayıtta görünmelidir.
        RuleFor(x => x.Defects)
            .Must(list => list is { Count: > 0 })
            .WithMessage("Şartlı karar için en az bir hata girilmelidir.")
            .When(x => x.Decision == FabricRollStatus.Conditional);
        RuleFor(x => x.Note)
            .Must((request, note) => request.Defects is { Count: > 0 } || !string.IsNullOrWhiteSpace(note))
            .WithMessage("Red kararı için hata ya da not girilmelidir.")
            .When(x => x.Decision == FabricRollStatus.Rejected);

        RuleFor(x => x.Defects)
            .Must(list => list!.Count <= MaxDefects).WithMessage($"En fazla {MaxDefects} hata girilebilir.")
            .Must(list => list!.All(d => d.DefectTypeId > 0)).WithMessage("Hata türü seçilmelidir.")
            .Must(list => list!.All(d => d.Quantity is >= 1 and <= 9999)).WithMessage("Hata adedi 1 ile 9999 arasında olmalıdır.")
            .Must(list => list!.All(d => d.AtMeter is null or >= 0)).WithMessage("Hatanın metresi negatif olamaz.")
            .Must(list => list!.All(d => d.Note is null || d.Note.Trim().Length <= 200)).WithMessage("Hata notu en fazla 200 karakter olabilir.")
            .When(x => x.Defects is not null);
    }
}

public sealed record ApproveRemainingRollsRequest(int LotId);

public sealed record ApproveRemainingRollsResultDto(int ApprovedCount);

/// <param name="Accepted">Müşteri (işletme malında yetkili) şartlı topu kabul etti mi.</param>
/// <param name="DecidedBy">Kararı veren kişi.</param>
/// <param name="DecidedOn">Kararın verildiği tarih.</param>
public sealed record BoyahaneRollCustomerDecisionRequest(
    bool Accepted,
    string DecidedBy,
    CustomerDecisionChannel Channel,
    DateOnly DecidedOn,
    string? Note = null);

public sealed class BoyahaneRollCustomerDecisionRequestValidator : AbstractValidator<BoyahaneRollCustomerDecisionRequest>
{
    public BoyahaneRollCustomerDecisionRequestValidator()
    {
        RuleFor(x => x.DecidedBy).NotEmpty().WithMessage("Kararı veren kişi zorunludur.").MaximumLength(100).WithMessage("En fazla 100 karakter olabilir.");
        RuleFor(x => x.Channel).IsInEnum().WithMessage("Geçerli bir iletişim yolu seçin.");
        RuleFor(x => x.DecidedOn).NotEmpty().WithMessage("Karar tarihi zorunludur.");
        RuleFor(x => x.Note).OptionalText(500);
    }
}

public interface IBoyahaneInspectionService
{
    Task<PagedResult<BoyahaneFabricRollListItemDto>> GetRollsAsync(BoyahaneFabricRollQuery query, CancellationToken cancellationToken);
    Task<Result<BoyahaneFabricRollDetailDto>> GetRollAsync(int id, CancellationToken cancellationToken);

    /// <summary>Okutulan barkoddaki top numarasıyla topu bulur.</summary>
    Task<Result<BoyahaneFabricRollDetailDto>> GetRollByNoAsync(string rollNo, CancellationToken cancellationToken);
    Task<Result<BoyahaneFabricRollDetailDto>> InspectAsync(int id, InspectBoyahaneFabricRollRequest request, CancellationToken cancellationToken);
    Task<Result<ApproveRemainingRollsResultDto>> ApproveRemainingAsync(ApproveRemainingRollsRequest request, CancellationToken cancellationToken);
    Task<Result<BoyahaneFabricRollDetailDto>> RecordCustomerDecisionAsync(int id, BoyahaneRollCustomerDecisionRequest request, CancellationToken cancellationToken);
}

public sealed class BoyahaneInspectionService(IAppDbContext db, ICurrentUser currentUser, TimeProvider timeProvider) : IBoyahaneInspectionService
{
    private const string InvalidState = "FABRIC_ROLL_INVALID_STATE";

    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Top bulunamadı.", ErrorType.NotFound);

    public Task<PagedResult<BoyahaneFabricRollListItemDto>> GetRollsAsync(BoyahaneFabricRollQuery query, CancellationToken cancellationToken)
    {
        var rolls = db.Set<BoyahaneFabricRoll>().AsNoTracking();
        if (NullIfBlank(query.Search) is { } term)
        {
            rolls = rolls.Where(t => t.RollNo.Contains(term)
                || t.Lot.LotNo.Contains(term)
                || (t.CustomerRollNo != null && t.CustomerRollNo.Contains(term))
                || (t.Lot.Receipt.Customer != null && t.Lot.Receipt.Customer.Name.Contains(term)));
        }

        return rolls
            .OrderBy(t => t.RollNo)
            .Select(t => new BoyahaneFabricRollListItemDto(
                t.Id,
                t.RollNo,
                t.CustomerRollNo,
                t.Lot.LotNo,
                t.Lot.ReceiptId,
                t.Lot.Receipt.ReceiptDate,
                t.Lot.Receipt.Customer != null ? t.Lot.Receipt.Customer.Name : null,
                t.Lot.Fabric.FabricCode,
                t.Lot.Fabric.Name,
                t.InspectedMeters ?? t.Meters,
                t.InspectedKilograms ?? t.Kilograms,
                t.Defects.Count,
                t.IsConditionallyApproved,
                t.Status))
            .ToPagedResultAsync(query, cancellationToken);
    }

    public Task<Result<BoyahaneFabricRollDetailDto>> GetRollAsync(int id, CancellationToken cancellationToken) =>
        DetailAsync(db.Set<BoyahaneFabricRoll>().Where(t => t.Id == id), cancellationToken);

    public Task<Result<BoyahaneFabricRollDetailDto>> GetRollByNoAsync(string rollNo, CancellationToken cancellationToken)
    {
        var number = rollNo.Trim().ToUpperInvariant();
        return DetailAsync(db.Set<BoyahaneFabricRoll>().Where(t => t.RollNo == number), cancellationToken);
    }

    private async Task<Result<BoyahaneFabricRollDetailDto>> DetailAsync(IQueryable<BoyahaneFabricRoll> query, CancellationToken cancellationToken)
    {
        var roll = await query.AsNoTracking()
            .Include(t => t.Lot).ThenInclude(l => l.Fabric)
            .Include(t => t.Lot).ThenInclude(l => l.Receipt).ThenInclude(r => r.Customer)
            .Include(t => t.Defects).ThenInclude(d => d.DefectType)
            .SingleOrDefaultAsync(cancellationToken);
        if (roll is null)
        {
            return NotFound;
        }

        var pending = await db.Set<BoyahaneFabricRoll>()
            .CountAsync(t => t.LotId == roll.LotId && t.Status == FabricRollStatus.PendingInspection, cancellationToken);

        return new BoyahaneFabricRollDetailDto(
            roll.Id,
            roll.RollNo,
            roll.CustomerRollNo,
            roll.LotId,
            roll.Lot.LotNo,
            roll.Lot.ReceiptId,
            roll.Lot.Receipt.Customer?.Name,
            roll.Lot.Fabric.FabricCode,
            roll.Lot.Fabric.Name,
            roll.Lot.Fabric.RawWidthCm,
            roll.Lot.Fabric.RawWeightGsm,
            roll.Meters,
            roll.Kilograms,
            roll.InspectedMeters,
            roll.InspectedKilograms,
            roll.MetersMeasured,
            roll.KilogramsMeasured,
            roll.InspectedWidthCm,
            roll.InspectedWeightGsm,
            roll.InspectedAt,
            roll.InspectedBy,
            roll.InspectionNote,
            roll.Status,
            roll.IsConditionallyApproved,
            roll.CustomerDecisionAccepted,
            roll.CustomerDecisionBy,
            roll.CustomerDecisionChannel,
            roll.CustomerDecisionOn,
            roll.CustomerDecisionNote,
            roll.CustomerDecisionRecordedBy,
            pending,
            roll.CustomerDecisionAccepted is null,
            roll.Defects.OrderBy(d => d.Id).Select(d => new BoyahaneFabricRollDefectDto(
                d.Id, d.DefectTypeId, d.DefectType.Code, d.DefectType.Name, d.Quantity, d.AtMeter, d.Note)).ToList());
    }

    public async Task<Result<BoyahaneFabricRollDetailDto>> InspectAsync(
        int id, InspectBoyahaneFabricRollRequest request, CancellationToken cancellationToken)
    {
        var roll = await db.Set<BoyahaneFabricRoll>()
            .Include(t => t.Lot).ThenInclude(l => l.Fabric)
            .Include(t => t.Defects)
            .SingleOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (roll is null)
        {
            return NotFound;
        }

        if (roll.CustomerDecisionAccepted is not null)
        {
            return new Error(InvalidState, "Müşteri kararı girilmiş topun kontrolü değiştirilemez.", ErrorType.Conflict);
        }

        if (await db.Set<BoyahaneBatchItem>().AnyAsync(i => i.IsActive && i.RollId == id, cancellationToken))
        {
            return new Error(InvalidState, "Partiye girmiş topun kontrolü değiştirilemez.", ErrorType.Conflict);
        }

        if (await db.Set<BoyahaneGoodsReturnItem>().AnyAsync(i => i.RollId == id && i.Return.Status != GoodsReturnStatus.Cancelled, cancellationToken))
        {
            return new Error(InvalidState, "İade ya da hurda belgesindeki topun kontrolü değiştirilemez; önce belgeden çıkarın.", ErrorType.Conflict);
        }

        // Kaliteci kendi bulduğu hatayı müşteri adına onaylayamaz: şartlı top ancak müşteri kararıyla uygun olur.
        if (roll.Status == FabricRollStatus.Conditional && request.Decision == FabricRollStatus.Approved)
        {
            return new Error(InvalidState, "Şartlı top yalnızca müşteri kararıyla uygun yapılabilir.", ErrorType.Conflict);
        }

        var defects = request.Defects ?? [];
        foreach (var defectTypeId in defects.Select(d => d.DefectTypeId).Distinct())
        {
            var currentlyUsed = roll.Defects.Any(d => d.DefectTypeId == defectTypeId) ? defectTypeId : (int?)null;
            if (await ReferenceValidation.RequireSelectableAsync(
                    db.Set<BoyahaneDefectType>(), defectTypeId, currentlyUsed, "defects", "Hata türü", cancellationToken) is { } error)
            {
                return error;
            }
        }

        var fabric = roll.Lot.Fabric;
        var width = request.InspectedWidthCm ?? fabric.RawWidthCm;
        var weight = request.InspectedWeightGsm ?? fabric.RawWeightGsm;

        // Ölçülmüş değer korunur; yalnızca kabulde hesaplanmış karşılık, ölçülen değerden yeniden hesaplanır.
        roll.InspectedMeters = request.InspectedMeters
            ?? (request.InspectedKilograms is { } kilograms && !roll.MetersMeasured
                ? FabricConversion.MetersFromKilograms(kilograms, width, weight)
                : null);
        roll.InspectedKilograms = request.InspectedKilograms
            ?? (request.InspectedMeters is { } meters && !roll.KilogramsMeasured
                ? FabricConversion.KilogramsFromMeters(meters, width, weight)
                : null);
        roll.MetersMeasured |= request.InspectedMeters is not null;
        roll.KilogramsMeasured |= request.InspectedKilograms is not null;
        roll.InspectedWidthCm = request.InspectedWidthCm;
        roll.InspectedWeightGsm = request.InspectedWeightGsm;
        roll.InspectionNote = NullIfBlank(request.Note);
        roll.Status = request.Decision;
        ReleaseIfRejected(roll);
        MarkInspected(roll);

        db.Set<BoyahaneFabricRollDefect>().RemoveRange(roll.Defects);
        foreach (var item in defects)
        {
            db.Set<BoyahaneFabricRollDefect>().Add(new BoyahaneFabricRollDefect
            {
                RollId = roll.Id,
                DefectTypeId = item.DefectTypeId,
                Quantity = item.Quantity,
                AtMeter = item.AtMeter,
                Note = NullIfBlank(item.Note),
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        return await GetRollAsync(id, cancellationToken);
    }

    public async Task<Result<ApproveRemainingRollsResultDto>> ApproveRemainingAsync(
        ApproveRemainingRollsRequest request, CancellationToken cancellationToken)
    {
        var rolls = await db.Set<BoyahaneFabricRoll>()
            .Where(t => t.LotId == request.LotId && t.Status == FabricRollStatus.PendingInspection)
            .ToListAsync(cancellationToken);

        foreach (var roll in rolls)
        {
            roll.Status = FabricRollStatus.Approved;
            MarkInspected(roll);
        }

        await db.SaveChangesAsync(cancellationToken);
        return new ApproveRemainingRollsResultDto(rolls.Count);
    }

    public async Task<Result<BoyahaneFabricRollDetailDto>> RecordCustomerDecisionAsync(
        int id, BoyahaneRollCustomerDecisionRequest request, CancellationToken cancellationToken)
    {
        var roll = await db.Set<BoyahaneFabricRoll>().SingleOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (roll is null)
        {
            return NotFound;
        }

        if (roll.Status != FabricRollStatus.Conditional)
        {
            return new Error(InvalidState, "Müşteri kararı yalnızca şartlı top için girilir.", ErrorType.Conflict);
        }

        roll.Status = request.Accepted ? FabricRollStatus.Approved : FabricRollStatus.Rejected;
        ReleaseIfRejected(roll);
        roll.IsConditionallyApproved = request.Accepted;
        roll.CustomerDecisionAccepted = request.Accepted;
        roll.CustomerDecisionBy = request.DecidedBy.Trim();
        roll.CustomerDecisionChannel = request.Channel;
        roll.CustomerDecisionOn = request.DecidedOn;
        roll.CustomerDecisionNote = NullIfBlank(request.Note);
        roll.CustomerDecisionRecordedBy = currentUser.UserName;

        await db.SaveChangesAsync(cancellationToken);
        return await GetRollAsync(id, cancellationToken);
    }

    /// <summary>Reddedilen top işleme girmez; bağlı olduğu sipariş satırından çıkar.</summary>
    private static void ReleaseIfRejected(BoyahaneFabricRoll roll)
    {
        if (roll.Status == FabricRollStatus.Rejected)
        {
            roll.OrderLineId = null;
        }
    }

    private void MarkInspected(BoyahaneFabricRoll roll)
    {
        roll.InspectedAt = timeProvider.GetUtcNow().UtcDateTime;
        roll.InspectedBy = currentUser.UserName;
    }
}
