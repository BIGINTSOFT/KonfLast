using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Orme.Application.Features.Purchasing;
using ERP.Orme.Domain.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Orme.Application.Features.Receipts;

public sealed class OrmeYarnLotQuery : PageRequest
{
    /// <summary>Lot no, irsaliye no, üretici lot no, iplik ya da müşteri adında aranır.</summary>
    public string? Search { get; set; }
}

/// <param name="CustomerName">Malın sahibi; boşsa mal işletmenindir.</param>
/// <param name="PurchaseOrderNo">Lot bir satın alma siparişinden teslim alındıysa siparişin numarası.</param>
public sealed record OrmeYarnLotDto(
    int Id,
    string LotNo,
    DateOnly ReceiptDate,
    int? CustomerId,
    string? CustomerName,
    int? SupplierId,
    string? SupplierName,
    int YarnId,
    string YarnCode,
    string YarnName,
    string? YarnColor,
    string? DespatchNo,
    string? ProducerLotNo,
    int PackageCount,
    decimal Kilograms,
    decimal RemainingKilograms,
    string? Notes,
    int? PurchaseOrderId = null,
    string? PurchaseOrderNo = null);

/// <param name="CustomerId">Fason işte ipliğin sahibi olan müşteri; boşsa işletme malıdır.</param>
/// <param name="PackageCount">Bobin sayısı.</param>
public sealed record OrmeYarnLotRequest(
    DateOnly ReceiptDate,
    int YarnId,
    decimal Kilograms,
    int PackageCount,
    int? CustomerId = null,
    int? SupplierId = null,
    string? DespatchNo = null,
    string? ProducerLotNo = null,
    string? Notes = null);

public sealed class OrmeYarnLotRequestValidator : AbstractValidator<OrmeYarnLotRequest>
{
    public OrmeYarnLotRequestValidator()
    {
        RuleFor(x => x.ReceiptDate).NotEmpty().WithMessage("Giriş tarihi zorunludur.");
        RuleFor(x => x.YarnId).GreaterThan(0).WithMessage("İplik kartı seçilmelidir.");
        RuleFor(x => x.Kilograms)
            .GreaterThan(0).WithMessage("Kilo sıfırdan büyük olmalıdır.")
            .LessThanOrEqualTo(9_999_999).WithMessage("Kilo çok büyük.")
            .PrecisionScale(12, 2, true).WithMessage("Kilo en fazla iki ondalık olabilir.");
        RuleFor(x => x.PackageCount).InclusiveBetween(1, 100_000).WithMessage("Bobin sayısı en az 1 olmalıdır.");
        RuleFor(x => x.DespatchNo).OptionalText(50);
        RuleFor(x => x.ProducerLotNo).OptionalText(50);
        RuleFor(x => x.Notes).OptionalText(1000);
    }
}

public interface IOrmeYarnLotService
{
    Task<PagedResult<OrmeYarnLotDto>> GetListAsync(OrmeYarnLotQuery query, CancellationToken cancellationToken);
    Task<Result<OrmeYarnLotDto>> GetAsync(int id, CancellationToken cancellationToken);
    Task<Result<OrmeYarnLotDto>> CreateAsync(OrmeYarnLotRequest request, CancellationToken cancellationToken);
    Task<Result<OrmeYarnLotDto>> UpdateAsync(int id, OrmeYarnLotRequest request, CancellationToken cancellationToken);
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken);
}

/// <summary>İplik girişleri: kilo ve lot bazında, bobin sayısıyla. Üretime verilmiş lotun ipliği ve sahibi değişmez, lot silinmez.</summary>
public sealed class OrmeYarnLotService(IAppDbContext db, IDocumentNumberGenerator numbers, IAppLock appLock) : IOrmeYarnLotService
{
    /// <summary>Lot numarası serisi; satın alma teslimi de aynı seriden numara alır.</summary>
    public const string Series = "OrmeYarnLot";

    /// <summary>Lot numarası biçimi (OI-yıl-sıra).</summary>
    public static string LotNo(int year, int sequence) => $"OI-{year}-{sequence:D5}";

    private const string PurchasedLotMessage = "Satın alma siparişinden gelen lotun ipliği, sahibi ve tedarikçisi değiştirilemez.";
    private const string InvalidState = "ORME_YARN_LOT_INVALID_STATE";
    private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(10);
    private static readonly Error NotFound = new(ErrorCodes.NotFound, "İplik girişi bulunamadı.", ErrorType.NotFound);
    private static readonly Error Busy = new("ORME_BUSY", "Aynı anda başka bir iplik işlemi yapılıyor; tekrar deneyin.", ErrorType.Conflict);

    private static IQueryable<OrmeYarnLotDto> Project(IQueryable<OrmeYarnLot> lots) => lots.Select(l => new OrmeYarnLotDto(
        l.Id,
        l.LotNo,
        l.ReceiptDate,
        l.CustomerId,
        l.Customer != null ? l.Customer.Name : null,
        l.SupplierId,
        l.Supplier != null ? l.Supplier.Name : null,
        l.YarnId,
        l.Yarn.YarnCode,
        l.Yarn.Name,
        l.Yarn.Color,
        l.DespatchNo,
        l.ProducerLotNo,
        l.PackageCount,
        l.Kilograms,
        l.Kilograms - (l.Issues.Sum(i => (decimal?)i.Kilograms) ?? 0) + (l.TakeBacks.Sum(t => (decimal?)t.Kilograms) ?? 0) - (l.Returns.Sum(r => (decimal?)r.Kilograms) ?? 0),
        l.Notes,
        l.PurchaseOrderLine != null ? (int?)l.PurchaseOrderLine.OrderId : null,
        l.PurchaseOrderLine != null ? l.PurchaseOrderLine.Order.OrderNo : null));

    public Task<PagedResult<OrmeYarnLotDto>> GetListAsync(OrmeYarnLotQuery query, CancellationToken cancellationToken)
    {
        var lots = db.Set<OrmeYarnLot>().AsNoTracking();
        if (NullIfBlank(query.Search) is { } term)
        {
            lots = lots.Where(l => l.LotNo.Contains(term)
                || (l.DespatchNo != null && l.DespatchNo.Contains(term))
                || (l.ProducerLotNo != null && l.ProducerLotNo.Contains(term))
                || l.Yarn.YarnCode.Contains(term)
                || l.Yarn.Name.Contains(term)
                || (l.Customer != null && l.Customer.Name.Contains(term)));
        }

        return Project(lots.OrderByDescending(l => l.ReceiptDate).ThenByDescending(l => l.Id)).ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<Result<OrmeYarnLotDto>> GetAsync(int id, CancellationToken cancellationToken)
    {
        var lot = await Project(db.Set<OrmeYarnLot>().AsNoTracking().Where(l => l.Id == id)).SingleOrDefaultAsync(cancellationToken);
        return lot is null ? NotFound : lot;
    }

    public async Task<Result<OrmeYarnLotDto>> CreateAsync(OrmeYarnLotRequest request, CancellationToken cancellationToken)
    {
        if (await ValidateReferencesAsync(request, null, cancellationToken) is { } error)
        {
            return error;
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var year = request.ReceiptDate.Year;
        var sequence = await numbers.NextAsync(Series, year, cancellationToken);
        var lot = new OrmeYarnLot { LotNo = LotNo(year, sequence) };
        Apply(lot, request);
        db.Set<OrmeYarnLot>().Add(lot);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(lot.Id, cancellationToken);
    }

    public async Task<Result<OrmeYarnLotDto>> UpdateAsync(int id, OrmeYarnLotRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await appLock.TryAcquireAsync(OrmeYarnStock.YarnLotLock, LockTimeout, cancellationToken))
        {
            return Busy;
        }

        var lot = await db.Set<OrmeYarnLot>().SingleOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (lot is null)
        {
            return NotFound;
        }

        if (lot.PurchaseOrderLineId is not null
            && (lot.YarnId != request.YarnId || request.CustomerId is not null || lot.SupplierId != request.SupplierId))
        {
            return new Error(InvalidState, PurchasedLotMessage, ErrorType.Conflict);
        }

        if (await OrmeYarnStock.HasMovementsAsync(db, id, cancellationToken))
        {
            if (lot.YarnId != request.YarnId || lot.CustomerId != request.CustomerId)
            {
                return new Error(InvalidState, "Üretime verilmiş ya da iade edilmiş lotun ipliği ve sahibi değiştirilemez.", ErrorType.Conflict);
            }

            var issued = await OrmeYarnStock.IssuedFromLotAsync(db, id, cancellationToken);
            if (request.Kilograms < issued)
            {
                return new Error(InvalidState, $"Lottan {issued:0.##} kg çıktı; kilo bunun altına indirilemez.", ErrorType.Conflict);
            }
        }

        if (await ValidateReferencesAsync(request, lot, cancellationToken) is { } error)
        {
            return error;
        }

        Apply(lot, request);
        await db.SaveChangesAsync(cancellationToken);
        if (lot.PurchaseOrderLineId is { } purchaseLineId)
        {
            // Kilo düzeltilirse siparişin teslim durumu değişebilir.
            await OrmePurchaseReceipts.RefreshStatusByLineAsync(db, purchaseLineId, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var lot = await db.Set<OrmeYarnLot>().SingleOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (lot is null)
        {
            return NotFound;
        }

        if (await OrmeYarnStock.HasMovementsAsync(db, id, cancellationToken))
        {
            return new Error(InvalidState, "Üretime verilmiş ya da iade edilmiş lot silinemez.", ErrorType.Conflict);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.Set<OrmeYarnLot>().Remove(lot);
        await db.SaveChangesAsync(cancellationToken);
        if (lot.PurchaseOrderLineId is { } purchaseLineId)
        {
            // Yanlış teslim silinince sipariş yeniden teslim bekler.
            await OrmePurchaseReceipts.RefreshStatusByLineAsync(db, purchaseLineId, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<Error?> ValidateReferencesAsync(OrmeYarnLotRequest request, OrmeYarnLot? existing, CancellationToken cancellationToken) =>
        await ReferenceValidation.RequireSelectableAsync(db.Set<OrmeYarn>(), request.YarnId, existing?.YarnId, "yarnId", "İplik kartı", cancellationToken)
        ?? await ReferenceValidation.RequireSelectableAsync(db.Set<OrmeCustomer>(), request.CustomerId, existing?.CustomerId, "customerId", "Müşteri", cancellationToken)
        ?? await ReferenceValidation.RequireSelectableAsync(db.Set<OrmeSupplier>(), request.SupplierId, existing?.SupplierId, "supplierId", "Tedarikçi", cancellationToken);

    private static void Apply(OrmeYarnLot lot, OrmeYarnLotRequest request)
    {
        lot.ReceiptDate = request.ReceiptDate;
        lot.YarnId = request.YarnId;
        lot.CustomerId = request.CustomerId;
        lot.SupplierId = request.SupplierId;
        lot.DespatchNo = NullIfBlank(request.DespatchNo);
        lot.ProducerLotNo = NullIfBlank(request.ProducerLotNo);
        lot.PackageCount = request.PackageCount;
        lot.Kilograms = request.Kilograms;
        lot.Notes = NullIfBlank(request.Notes);
    }
}
