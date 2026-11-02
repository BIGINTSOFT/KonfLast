using ERP.Boyahane.Application.Features.Warehousing;
using ERP.Boyahane.Domain.Entities;
using ERP.Boyahane.Domain.Enums;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Boyahane.Application.Features.Receipts;

public sealed class BoyahaneYarnLotQuery : PageRequest
{
    /// <summary>Lot no, irsaliye no, müşteri lot no ya da müşteri adında aranır.</summary>
    public string? Search { get; set; }
}

/// <param name="CustomerName">Malın sahibi; boşsa mal işletmenindir.</param>
/// <param name="OrderNo">Bağlı olduğu sipariş; boşsa boştadır.</param>
public sealed record BoyahaneYarnLotDto(
    int Id,
    string LotNo,
    DateOnly ReceiptDate,
    int? CustomerId,
    string? CustomerName,
    int YarnId,
    string YarnCode,
    string YarnName,
    string? DespatchNo,
    string? CustomerLotNo,
    YarnPackageType PackageType,
    int PackageCount,
    decimal Kilograms,
    string? Notes,
    string? OrderNo,
    YarnLotStatus Status,
    string? RejectionNote,
    string? LocationCode);

public sealed record BoyahaneYarnLotRequest(
    DateOnly ReceiptDate,
    int YarnId,
    decimal Kilograms,
    int PackageCount,
    YarnPackageType PackageType = YarnPackageType.Bobbin,
    int? CustomerId = null,
    string? DespatchNo = null,
    string? CustomerLotNo = null,
    string? Notes = null,
    int? LocationId = null);

public sealed record RejectBoyahaneYarnLotRequest(string Note);

public sealed class RejectBoyahaneYarnLotRequestValidator : AbstractValidator<RejectBoyahaneYarnLotRequest>
{
    public RejectBoyahaneYarnLotRequestValidator()
    {
        RuleFor(x => x.Note).NotEmpty().WithMessage("Red gerekçesi zorunludur.").MaximumLength(500).WithMessage("Red gerekçesi en fazla 500 karakter olabilir.");
    }
}

public sealed class BoyahaneYarnLotRequestValidator : AbstractValidator<BoyahaneYarnLotRequest>
{
    public BoyahaneYarnLotRequestValidator()
    {
        RuleFor(x => x.ReceiptDate).NotEmpty().WithMessage("Kabul tarihi zorunludur.");
        RuleFor(x => x.YarnId).GreaterThan(0).WithMessage("İplik kartı seçilmelidir.");
        RuleFor(x => x.Kilograms).GreaterThan(0).WithMessage("Kilo sıfırdan büyük olmalıdır.").LessThanOrEqualTo(9_999_999).WithMessage("Kilo sıfırdan büyük olmalıdır.");
        RuleFor(x => x.PackageCount).InclusiveBetween(1, 100_000).WithMessage("Bobin / çile sayısı en az 1 olmalıdır.");
        RuleFor(x => x.PackageType).IsInEnum().WithMessage("Geçerli bir ambalaj türü seçin.");
        RuleFor(x => x.DespatchNo).OptionalText(50);
        RuleFor(x => x.CustomerLotNo).OptionalText(50);
        RuleFor(x => x.Notes).OptionalText(1000);
    }
}

public interface IBoyahaneYarnLotService
{
    Task<PagedResult<BoyahaneYarnLotDto>> GetListAsync(BoyahaneYarnLotQuery query, CancellationToken cancellationToken);
    Task<Result<BoyahaneYarnLotDto>> GetAsync(int id, CancellationToken cancellationToken);
    Task<Result<BoyahaneYarnLotDto>> CreateAsync(BoyahaneYarnLotRequest request, CancellationToken cancellationToken);
    Task<Result<BoyahaneYarnLotDto>> UpdateAsync(int id, BoyahaneYarnLotRequest request, CancellationToken cancellationToken);
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken);
    Task<Result<BoyahaneYarnLotDto>> RejectAsync(int id, RejectBoyahaneYarnLotRequest request, CancellationToken cancellationToken);
    Task<Result<BoyahaneYarnLotDto>> ApproveAsync(int id, CancellationToken cancellationToken);
}

public sealed class BoyahaneYarnLotService(IAppDbContext db, IDocumentNumberGenerator numbers) : IBoyahaneYarnLotService
{
    private const string Series = "BoyahaneYarnLot";

    private static readonly Error NotFound = new(ErrorCodes.NotFound, "İplik lotu bulunamadı.", ErrorType.NotFound);

    private IQueryable<BoyahaneYarnLotDto> Project(IQueryable<BoyahaneYarnLot> lots) => lots.Select(l => new BoyahaneYarnLotDto(
        l.Id,
        l.LotNo,
        l.ReceiptDate,
        l.CustomerId,
        l.Customer != null ? l.Customer.Name : null,
        l.YarnId,
        l.Yarn.YarnCode,
        l.Yarn.Name,
        l.DespatchNo,
        l.CustomerLotNo,
        l.PackageType,
        l.PackageCount,
        l.Kilograms,
        l.Notes,
        l.OrderLine != null ? l.OrderLine.Order.OrderNo : null,
        l.Status,
        l.RejectionNote,
        l.Location != null ? l.Location.Code : null));

    public Task<PagedResult<BoyahaneYarnLotDto>> GetListAsync(BoyahaneYarnLotQuery query, CancellationToken cancellationToken)
    {
        var lots = db.Set<BoyahaneYarnLot>().AsNoTracking();
        if (NullIfBlank(query.Search) is { } term)
        {
            lots = lots.Where(l => l.LotNo.Contains(term)
                || (l.DespatchNo != null && l.DespatchNo.Contains(term))
                || (l.CustomerLotNo != null && l.CustomerLotNo.Contains(term))
                || (l.Customer != null && l.Customer.Name.Contains(term)));
        }

        return Project(lots.OrderByDescending(l => l.ReceiptDate).ThenByDescending(l => l.Id)).ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<Result<BoyahaneYarnLotDto>> GetAsync(int id, CancellationToken cancellationToken)
    {
        var lot = await Project(db.Set<BoyahaneYarnLot>().AsNoTracking().Where(l => l.Id == id)).SingleOrDefaultAsync(cancellationToken);
        return lot is null ? NotFound : lot;
    }

    public async Task<Result<BoyahaneYarnLotDto>> CreateAsync(BoyahaneYarnLotRequest request, CancellationToken cancellationToken)
    {
        if (await ValidateReferencesAsync(request, null, cancellationToken) is { } error)
        {
            return error;
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var year = request.ReceiptDate.Year;
        var sequence = await numbers.NextAsync(Series, year, cancellationToken);
        var lot = new BoyahaneYarnLot
        {
            LotNo = $"BI-{year}-{sequence:D5}",
            LocationId = request.LocationId ?? await BoyahaneWarehouseRules.DefaultLocationIdAsync(db, BoyahaneWarehouseType.RawFabric, cancellationToken),
        };
        Apply(lot, request);
        db.Set<BoyahaneYarnLot>().Add(lot);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(lot.Id, cancellationToken);
    }

    public async Task<Result<BoyahaneYarnLotDto>> UpdateAsync(int id, BoyahaneYarnLotRequest request, CancellationToken cancellationToken)
    {
        var lot = await db.Set<BoyahaneYarnLot>().SingleOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (lot is null)
        {
            return NotFound;
        }

        if (lot.OrderLineId is not null)
        {
            return new Error(ErrorCodes.RecordInUse, "Siparişe bağlı lot düzenlenemez; önce siparişten çıkarın.", ErrorType.Conflict);
        }

        if (await ValidateReferencesAsync(request, lot, cancellationToken) is { } error)
        {
            return error;
        }

        Apply(lot, request);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var lot = await db.Set<BoyahaneYarnLot>().SingleOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (lot is null)
        {
            return NotFound;
        }

        if (lot.OrderLineId is not null)
        {
            return new Error(ErrorCodes.RecordInUse, "Siparişe bağlı lot silinemez.", ErrorType.Conflict);
        }

        db.Set<BoyahaneYarnLot>().Remove(lot);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    /// <summary>Lotu reddeder; siparişe bağlıysa siparişten çıkar. Partiye girmiş lot reddedilemez.</summary>
    public async Task<Result<BoyahaneYarnLotDto>> RejectAsync(int id, RejectBoyahaneYarnLotRequest request, CancellationToken cancellationToken)
    {
        var lot = await db.Set<BoyahaneYarnLot>().SingleOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (lot is null)
        {
            return NotFound;
        }

        if (await db.Set<BoyahaneBatchItem>().AnyAsync(i => i.IsActive && i.YarnLotId == id, cancellationToken))
        {
            return new Error(ErrorCodes.RecordInUse, "Partiye girmiş lot reddedilemez.", ErrorType.Conflict);
        }

        lot.Status = YarnLotStatus.Rejected;
        lot.RejectionNote = request.Note.Trim();
        lot.OrderLineId = null;
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    /// <summary>Reddedilen lotu yeniden uygun yapar; siparişe bağlamak ayrı işlemdir.</summary>
    public async Task<Result<BoyahaneYarnLotDto>> ApproveAsync(int id, CancellationToken cancellationToken)
    {
        var lot = await db.Set<BoyahaneYarnLot>().SingleOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (lot is null)
        {
            return NotFound;
        }

        if (await db.Set<BoyahaneGoodsReturnItem>().AnyAsync(i => i.YarnLotId == id && i.Return.Status != GoodsReturnStatus.Cancelled, cancellationToken))
        {
            return new Error(ErrorCodes.RecordInUse, "İade ya da hurda belgesindeki lot yeniden uygun yapılamaz; önce belgeden çıkarın.", ErrorType.Conflict);
        }

        lot.Status = YarnLotStatus.Approved;
        lot.RejectionNote = null;
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    private async Task<Error?> ValidateReferencesAsync(BoyahaneYarnLotRequest request, BoyahaneYarnLot? existing, CancellationToken cancellationToken) =>
        await ReferenceValidation.RequireSelectableAsync(db.Set<BoyahaneCustomer>(), request.CustomerId, existing?.CustomerId, "customerId", "Müşteri", cancellationToken)
        ?? await ReferenceValidation.RequireSelectableAsync(db.Set<BoyahaneYarn>(), request.YarnId, existing?.YarnId, "yarnId", "İplik kartı", cancellationToken)
        ?? (existing is null ? await BoyahaneWarehouseRules.ValidateLocationAsync(db, request.LocationId, cancellationToken) : null);

    private static void Apply(BoyahaneYarnLot lot, BoyahaneYarnLotRequest request)
    {
        lot.ReceiptDate = request.ReceiptDate;
        lot.CustomerId = request.CustomerId;
        lot.YarnId = request.YarnId;
        lot.DespatchNo = NullIfBlank(request.DespatchNo);
        lot.CustomerLotNo = NullIfBlank(request.CustomerLotNo);
        lot.PackageType = request.PackageType;
        lot.PackageCount = request.PackageCount;
        lot.Kilograms = request.Kilograms;
        lot.Notes = NullIfBlank(request.Notes);
    }
}
