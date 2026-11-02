using ERP.Boyahane.Application.Features.Warehousing;
using ERP.Boyahane.Domain.Entities;
using ERP.Boyahane.Domain.Enums;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Boyahane.Application.Features.Stock;

public sealed class BoyahaneChemicalStockQuery : PageRequest
{
    /// <summary>Lot no, tedarikçi ya da boya / kimyasal.</summary>
    public string? Search { get; set; }
}

public sealed record BoyahaneChemicalStockRowDto(
    int DyeChemicalId,
    string Code,
    string Name,
    DyeChemicalKind Kind,
    decimal UsableKilograms,
    decimal ExpiredKilograms,
    int LotCount,
    DateOnly? NearestExpiry);

public sealed record BoyahaneChemicalLotDto(
    int Id,
    string LotNo,
    int DyeChemicalId,
    string ChemicalCode,
    string ChemicalName,
    string? SupplierLotNo,
    int? SupplierId,
    string? SupplierName,
    string? PurchaseOrderNo,
    DateOnly ReceiptDate,
    DateOnly? ExpiryDate,
    decimal ReceivedKilograms,
    decimal RemainingKilograms,
    decimal? UnitCost,
    string Currency,
    string? Notes,
    bool IsUsed,
    string? LocationCode);

public sealed record BoyahaneChemicalMovementDto(
    long Id,
    string LotNo,
    string ChemicalCode,
    string ChemicalName,
    ChemicalMovementType Type,
    decimal Kilograms,
    string? Reference,
    DateTime CreatedAt,
    string? CreatedBy);

public sealed record BoyahaneChemicalLotRequest(
    int DyeChemicalId,
    DateOnly ReceiptDate,
    decimal Kilograms,
    string? SupplierLotNo = null,
    int? SupplierId = null,
    DateOnly? ExpiryDate = null,
    decimal? UnitCost = null,
    string? Currency = null,
    string? Notes = null,
    int? LocationId = null);

public sealed class BoyahaneChemicalLotRequestValidator : AbstractValidator<BoyahaneChemicalLotRequest>
{
    public BoyahaneChemicalLotRequestValidator()
    {
        RuleFor(x => x.DyeChemicalId).GreaterThan(0).WithMessage("Boya ya da kimyasal seçilmelidir.");
        RuleFor(x => x.ReceiptDate).NotEmpty().WithMessage("Giriş tarihi zorunludur.");
        RuleFor(x => x.Kilograms).InclusiveBetween(0.001m, 9_999_999).WithMessage("Miktar sıfırdan büyük olmalıdır.");
        RuleFor(x => x.SupplierLotNo).OptionalText(50);
        RuleFor(x => x.ExpiryDate).Must((request, expiry) => expiry >= request.ReceiptDate).WithMessage("Son kullanma tarihi giriş tarihinden önce olamaz.").When(x => x.ExpiryDate is not null);
        RuleFor(x => x.UnitCost).InclusiveBetween(0, 99_999_999).WithMessage("Birim maliyet geçersiz.").When(x => x.UnitCost is not null);
        RuleFor(x => x.Currency).Matches(@"^\s*[A-Za-z]{3}\s*$").WithMessage("Para birimi üç harfli ISO kodu olmalıdır.").When(x => !string.IsNullOrWhiteSpace(x.Currency));
        RuleFor(x => x.Notes).OptionalText(500);
    }
}

/// <param name="Note">Gerekçe zorunludur; hareket kaydına yazılır.</param>
public sealed record AdjustChemicalLotRequest(decimal CountedKilograms, string Note);

public sealed class AdjustChemicalLotRequestValidator : AbstractValidator<AdjustChemicalLotRequest>
{
    public AdjustChemicalLotRequestValidator()
    {
        RuleFor(x => x.CountedKilograms).InclusiveBetween(0, 9_999_999).WithMessage("Sayılan miktar geçersiz.");
        RuleFor(x => x.Note).NotEmpty().WithMessage("Düzeltme gerekçesi zorunludur.").MaximumLength(200).WithMessage("Gerekçe en fazla 200 karakter olabilir.");
    }
}

public interface IBoyahaneChemicalStockService
{
    Task<IReadOnlyList<BoyahaneChemicalStockRowDto>> GetStockAsync(CancellationToken cancellationToken);
    Task<PagedResult<BoyahaneChemicalLotDto>> GetLotsAsync(BoyahaneChemicalStockQuery query, CancellationToken cancellationToken);
    Task<PagedResult<BoyahaneChemicalMovementDto>> GetMovementsAsync(BoyahaneChemicalStockQuery query, CancellationToken cancellationToken);
    Task<Result<BoyahaneChemicalLotDto>> CreateLotAsync(BoyahaneChemicalLotRequest request, CancellationToken cancellationToken);
    Task<Result<BoyahaneChemicalLotDto>> UpdateLotAsync(int id, BoyahaneChemicalLotRequest request, CancellationToken cancellationToken);
    Task<Result> DeleteLotAsync(int id, CancellationToken cancellationToken);
    Task<Result<BoyahaneChemicalLotDto>> AdjustLotAsync(int id, AdjustChemicalLotRequest request, CancellationToken cancellationToken);

    /// <summary>Kullanılabilir stok: son kullanma tarihi geçmemiş lotlar.</summary>
    Task<IReadOnlyDictionary<int, decimal>> GetUsableAsync(IReadOnlyCollection<int> chemicalIds, CancellationToken cancellationToken);

    /// <summary>
    /// Tüketimi son kullanma tarihi en yakın lottan başlayarak düşer. Stok yetmezse kalan lotsuz kalır; üretim durdurulmaz.
    /// Çağıran açık bir işlem (transaction) içinde olmalı ve değişiklikleri kaydetmelidir.
    /// </summary>
    Task<bool> AllocateAsync(BoyahaneBatchConsumption consumption, int chemicalId, decimal kilograms, string reference, CancellationToken cancellationToken);
}

public sealed class BoyahaneChemicalStockService(
    IAppDbContext db,
    IDocumentNumberGenerator numbers,
    IAppLock appLock,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IBoyahaneChemicalStockService
{
    private const string Series = "BoyahaneChemicalLot";
    private const string InvalidState = "CHEMICAL_LOT_INVALID_STATE";
    private const string StockLock = "boyahane-chemical-stock";

    private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(10);
    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Lot bulunamadı.", ErrorType.NotFound);

    private DateOnly Today => DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

    public async Task<IReadOnlyList<BoyahaneChemicalStockRowDto>> GetStockAsync(CancellationToken cancellationToken)
    {
        var today = Today;
        var lots = await db.Set<BoyahaneChemicalLot>().AsNoTracking()
            .Where(l => l.RemainingKilograms > 0)
            .Select(l => new { l.DyeChemicalId, l.RemainingKilograms, l.ExpiryDate })
            .ToListAsync(cancellationToken);
        var chemicals = await db.Set<BoyahaneDyeChemical>().AsNoTracking()
            .OrderBy(c => c.Code)
            .Select(c => new { c.Id, c.Code, c.Name, c.Kind })
            .ToListAsync(cancellationToken);

        return chemicals.Select(c =>
        {
            var own = lots.Where(l => l.DyeChemicalId == c.Id).ToList();
            var usable = own.Where(l => l.ExpiryDate is null || l.ExpiryDate >= today).ToList();
            return new BoyahaneChemicalStockRowDto(
                c.Id,
                c.Code,
                c.Name,
                c.Kind,
                usable.Sum(l => l.RemainingKilograms),
                own.Except(usable).Sum(l => l.RemainingKilograms),
                own.Count,
                usable.Where(l => l.ExpiryDate is not null).Min(l => l.ExpiryDate));
        }).ToList();
    }

    public Task<PagedResult<BoyahaneChemicalLotDto>> GetLotsAsync(BoyahaneChemicalStockQuery query, CancellationToken cancellationToken)
    {
        var lots = db.Set<BoyahaneChemicalLot>().AsNoTracking();
        if (NullIfBlank(query.Search) is { } term)
        {
            lots = lots.Where(l => l.LotNo.Contains(term) || l.DyeChemical.Code.Contains(term) || l.DyeChemical.Name.Contains(term)
                || (l.SupplierLotNo != null && l.SupplierLotNo.Contains(term)) || (l.SupplierName != null && l.SupplierName.Contains(term)));
        }

        return Project(lots.OrderByDescending(l => l.Id)).ToPagedResultAsync(query, cancellationToken);
    }

    public Task<PagedResult<BoyahaneChemicalMovementDto>> GetMovementsAsync(BoyahaneChemicalStockQuery query, CancellationToken cancellationToken)
    {
        var movements = db.Set<BoyahaneChemicalMovement>().AsNoTracking();
        if (NullIfBlank(query.Search) is { } term)
        {
            movements = movements.Where(m => m.Lot.LotNo.Contains(term) || m.Lot.DyeChemical.Code.Contains(term) || m.Lot.DyeChemical.Name.Contains(term)
                || (m.Reference != null && m.Reference.Contains(term)));
        }

        return movements
            .OrderByDescending(m => m.Id)
            .Select(m => new BoyahaneChemicalMovementDto(m.Id, m.Lot.LotNo, m.Lot.DyeChemical.Code, m.Lot.DyeChemical.Name, m.Type, m.Kilograms, m.Reference, m.CreatedAt, m.CreatedBy))
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<Result<BoyahaneChemicalLotDto>> CreateLotAsync(BoyahaneChemicalLotRequest request, CancellationToken cancellationToken)
    {
        if (await ReferenceValidation.RequireSelectableAsync(db.Set<BoyahaneDyeChemical>(), request.DyeChemicalId, null, "dyeChemicalId", "Boya / kimyasal", cancellationToken) is { } error)
        {
            return error;
        }

        if (await BoyahaneWarehouseRules.ValidateLocationAsync(db, request.LocationId, cancellationToken) is { } locationError)
        {
            return locationError;
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var year = request.ReceiptDate.Year;
        var sequence = await numbers.NextAsync(Series, year, cancellationToken);
        var lot = new BoyahaneChemicalLot
        {
            LotNo = $"KL-{year}-{sequence:D5}",
            RemainingKilograms = request.Kilograms,
            LocationId = request.LocationId ?? await BoyahaneWarehouseRules.DefaultLocationIdAsync(db, BoyahaneWarehouseType.Chemical, cancellationToken),
        };
        Apply(lot, request);
        await ApplySupplierAsync(lot, request, cancellationToken);
        db.Set<BoyahaneChemicalLot>().Add(lot);
        await db.SaveChangesAsync(cancellationToken);
        db.Set<BoyahaneChemicalMovement>().Add(Movement(lot.Id, ChemicalMovementType.Receipt, request.Kilograms, null));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetLotAsync(lot.Id, cancellationToken);
    }

    public async Task<Result<BoyahaneChemicalLotDto>> UpdateLotAsync(int id, BoyahaneChemicalLotRequest request, CancellationToken cancellationToken)
    {
        var lot = await db.Set<BoyahaneChemicalLot>().SingleOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (lot is null)
        {
            return NotFound;
        }

        var used = await IsUsedAsync(id, cancellationToken);
        if (used && (lot.DyeChemicalId != request.DyeChemicalId || lot.ReceivedKilograms != request.Kilograms))
        {
            return new Error(InvalidState, "Hareket görmüş lotun boya kartı ve miktarı değiştirilemez; sayım düzeltmesi yapın.", ErrorType.Conflict);
        }

        if (await ReferenceValidation.RequireSelectableAsync(db.Set<BoyahaneDyeChemical>(), request.DyeChemicalId, lot.DyeChemicalId, "dyeChemicalId", "Boya / kimyasal", cancellationToken) is { } error)
        {
            return error;
        }

        if (lot.PurchaseOrderLineId is not null && lot.DyeChemicalId != request.DyeChemicalId)
        {
            return new Error(InvalidState, "Siparişten gelen lotun boya kartı değiştirilemez.", ErrorType.Conflict);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!used)
        {
            lot.RemainingKilograms = request.Kilograms;
            var receipt = await db.Set<BoyahaneChemicalMovement>().SingleAsync(m => m.LotId == id && m.Type == ChemicalMovementType.Receipt, cancellationToken);
            receipt.Kilograms = request.Kilograms;
        }

        Apply(lot, request);
        await ApplySupplierAsync(lot, request, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await RefreshPurchaseAsync(lot.PurchaseOrderLineId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetLotAsync(id, cancellationToken);
    }

    public async Task<Result> DeleteLotAsync(int id, CancellationToken cancellationToken)
    {
        var lot = await db.Set<BoyahaneChemicalLot>().SingleOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (lot is null)
        {
            return NotFound;
        }

        if (await IsUsedAsync(id, cancellationToken))
        {
            return new Error(ErrorCodes.RecordInUse, "Hareket görmüş lot silinemez.", ErrorType.Conflict);
        }

        db.Set<BoyahaneChemicalMovement>().RemoveRange(await db.Set<BoyahaneChemicalMovement>().Where(m => m.LotId == id).ToListAsync(cancellationToken));
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var purchaseLine = lot.PurchaseOrderLineId;
        db.Set<BoyahaneChemicalLot>().Remove(lot);
        await db.SaveChangesAsync(cancellationToken);
        await RefreshPurchaseAsync(purchaseLine, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<BoyahaneChemicalLotDto>> AdjustLotAsync(int id, AdjustChemicalLotRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await appLock.TryAcquireAsync(StockLock, LockTimeout, cancellationToken))
        {
            return new Error(ErrorCodes.ResourceBusy, "Stok şu an başka bir işlemle değişiyor. Lütfen tekrar deneyin.", ErrorType.Conflict);
        }

        var lot = await db.Set<BoyahaneChemicalLot>().SingleOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (lot is null)
        {
            return NotFound;
        }

        var difference = request.CountedKilograms - lot.RemainingKilograms;
        if (difference != 0)
        {
            lot.RemainingKilograms = request.CountedKilograms;
            db.Set<BoyahaneChemicalMovement>().Add(Movement(id, ChemicalMovementType.Adjustment, difference, request.Note.Trim()));
            await db.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return await GetLotAsync(id, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<int, decimal>> GetUsableAsync(IReadOnlyCollection<int> chemicalIds, CancellationToken cancellationToken)
    {
        var today = Today;
        var rows = await db.Set<BoyahaneChemicalLot>().AsNoTracking()
            .Where(l => chemicalIds.Contains(l.DyeChemicalId) && l.RemainingKilograms > 0 && (l.ExpiryDate == null || l.ExpiryDate >= today))
            .GroupBy(l => l.DyeChemicalId)
            .Select(g => new { g.Key, Total = g.Sum(l => l.RemainingKilograms) })
            .ToListAsync(cancellationToken);
        return rows.ToDictionary(r => r.Key, r => r.Total);
    }

    public async Task<bool> AllocateAsync(BoyahaneBatchConsumption consumption, int chemicalId, decimal kilograms, string reference, CancellationToken cancellationToken)
    {
        if (!await appLock.TryAcquireAsync(StockLock, LockTimeout, cancellationToken))
        {
            return false;
        }

        var today = Today;
        var lots = await db.Set<BoyahaneChemicalLot>()
            .Where(l => l.DyeChemicalId == chemicalId && l.RemainingKilograms > 0 && (l.ExpiryDate == null || l.ExpiryDate >= today))
            .OrderBy(l => l.ExpiryDate == null).ThenBy(l => l.ExpiryDate).ThenBy(l => l.ReceiptDate).ThenBy(l => l.Id)
            .ToListAsync(cancellationToken);
        var open = kilograms;
        foreach (var lot in lots)
        {
            if (open <= 0)
            {
                break;
            }

            var taken = Math.Min(open, lot.RemainingKilograms);
            lot.RemainingKilograms -= taken;
            open -= taken;
            consumption.Lots.Add(new BoyahaneConsumptionLot { LotId = lot.Id, Kilograms = taken });
            db.Set<BoyahaneChemicalMovement>().Add(Movement(lot.Id, ChemicalMovementType.Consumption, -taken, reference));
        }

        return true;
    }

    /// <summary>Tedarikçi kartı seçildiyse adı lota yazılır; kart seçilmemiş eski lotun serbest metni korunur.</summary>
    private async Task ApplySupplierAsync(BoyahaneChemicalLot lot, BoyahaneChemicalLotRequest request, CancellationToken cancellationToken)
    {
        if (request.SupplierId is { } supplierId)
        {
            lot.SupplierId = supplierId;
            lot.SupplierName = await db.Set<BoyahaneSupplier>().Where(s => s.Id == supplierId).Select(s => s.Name).FirstOrDefaultAsync(cancellationToken);
        }
        else if (lot.SupplierId is not null)
        {
            lot.SupplierId = null;
            lot.SupplierName = null;
        }
    }

    /// <summary>Siparişten gelen lot değişince teslim miktarı ve sipariş durumu yeniden hesaplanır.</summary>
    private async Task RefreshPurchaseAsync(int? purchaseLineId, CancellationToken cancellationToken)
    {
        if (purchaseLineId is null)
        {
            return;
        }

        var order = await db.Set<BoyahanePurchaseOrder>().Include(o => o.Lines).SingleAsync(o => o.Lines.Any(l => l.Id == purchaseLineId), cancellationToken);
        var line = order.Lines.Single(l => l.Id == purchaseLineId);
        line.ReceivedKilograms = await db.Set<BoyahaneChemicalLot>().Where(l => l.PurchaseOrderLineId == purchaseLineId).SumAsync(l => l.ReceivedKilograms, cancellationToken);
        order.RefreshReceiptStatus();
        await db.SaveChangesAsync(cancellationToken);
    }

    private BoyahaneChemicalMovement Movement(int lotId, ChemicalMovementType type, decimal kilograms, string? reference) => new()
    {
        LotId = lotId,
        Type = type,
        Kilograms = kilograms,
        Reference = reference,
        CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
        CreatedBy = currentUser.UserName,
    };

    private Task<bool> IsUsedAsync(int id, CancellationToken cancellationToken) =>
        db.Set<BoyahaneChemicalMovement>().AnyAsync(m => m.LotId == id && m.Type != ChemicalMovementType.Receipt, cancellationToken);

    private async Task<Result<BoyahaneChemicalLotDto>> GetLotAsync(int id, CancellationToken cancellationToken) =>
        await Project(db.Set<BoyahaneChemicalLot>().AsNoTracking().Where(l => l.Id == id)).SingleAsync(cancellationToken);

    private IQueryable<BoyahaneChemicalLotDto> Project(IQueryable<BoyahaneChemicalLot> lots) => lots.Select(l => new BoyahaneChemicalLotDto(
        l.Id,
        l.LotNo,
        l.DyeChemicalId,
        l.DyeChemical.Code,
        l.DyeChemical.Name,
        l.SupplierLotNo,
        l.SupplierId,
        l.SupplierName,
        l.PurchaseOrderLine != null ? l.PurchaseOrderLine.Order.OrderNo : null,
        l.ReceiptDate,
        l.ExpiryDate,
        l.ReceivedKilograms,
        l.RemainingKilograms,
        l.UnitCost,
        l.Currency,
        l.Notes,
        db.Set<BoyahaneChemicalMovement>().Any(m => m.LotId == l.Id && m.Type != ChemicalMovementType.Receipt),
        l.Location != null ? l.Location.Code : null));

    private static void Apply(BoyahaneChemicalLot lot, BoyahaneChemicalLotRequest request)
    {
        lot.DyeChemicalId = request.DyeChemicalId;
        lot.ReceiptDate = request.ReceiptDate;
        lot.ReceivedKilograms = request.Kilograms;
        lot.SupplierLotNo = NullIfBlank(request.SupplierLotNo);
        lot.ExpiryDate = request.ExpiryDate;
        lot.UnitCost = request.UnitCost;
        lot.Currency = string.IsNullOrWhiteSpace(request.Currency) ? "TRY" : request.Currency.Trim().ToUpperInvariant();
        lot.Notes = NullIfBlank(request.Notes);
    }
}
