using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Konfeksiyon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Konfeksiyon.Application.Features.Subcontracting;

public interface ISubcontractPriceService
{
    Task<PagedResult<SubcontractPriceDto>> GetListAsync(SubcontractPriceQuery query, CancellationToken cancellationToken);
    Task<Result<SubcontractPriceDto>> GetAsync(int id, CancellationToken cancellationToken);
    Task<Result<SubcontractPriceDto>> CreateAsync(SubcontractPriceRequest request, CancellationToken cancellationToken);
    Task<Result<SubcontractPriceDto>> UpdateAsync(int id, SubcontractPriceRequest request, CancellationToken cancellationToken);
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken);

    /// <summary>Fasoncunun operasyon (ve ürün) için verilen gündeki fiyatı; ürüne özel fiyat genel fiyatın önündedir.</summary>
    Task<ResolvedSubcontractPriceDto> ResolveAsync(
        int supplierId, int operationId, int? productId, DateOnly date, CancellationToken cancellationToken);
}

/// <summary>Bir fason fiyatı bulma isteği (toplu çözümleme için).</summary>
public readonly record struct SubcontractPriceKey(int SupplierId, int OperationId, int? ProductId);

/// <summary>
/// Fason fiyatları. Bir gün için geçerli fiyat, başlangıcı o güne kadar olan en yeni kayıttır (bitiş tarihi yoktur;
/// yeni fiyat eskisinin yerine geçer). Ürüne özel fiyat, aynı fasoncu ve operasyonun genel fiyatının önüne geçer.
/// </summary>
public sealed class SubcontractPriceService(IKonfeksiyonDbContext db, TimeProvider timeProvider) : ISubcontractPriceService
{
    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Fason fiyatı bulunamadı.", ErrorType.NotFound);

    public Task<PagedResult<SubcontractPriceDto>> GetListAsync(SubcontractPriceQuery query, CancellationToken cancellationToken)
    {
        var today = Today();
        var prices = db.SubcontractPrices.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            prices = prices.Where(p => p.Supplier.SupplierCode.Contains(search) || p.Supplier.Name.Contains(search)
                || p.Operation.OperationCode.Contains(search) || p.Operation.Name.Contains(search)
                || (p.Product != null && (p.Product.ProductCode.Contains(search) || p.Product.Name.Contains(search))));
        }

        if (query.SupplierId is { } supplierId)
        {
            prices = prices.Where(p => p.SupplierId == supplierId);
        }

        if (query.OperationId is { } operationId)
        {
            prices = prices.Where(p => p.OperationId == operationId);
        }

        if (query.CurrentOnly == true)
        {
            prices = prices.Where(p => p.ValidFrom <= today && !db.SubcontractPrices.Any(other =>
                other.SupplierId == p.SupplierId && other.OperationId == p.OperationId && other.ProductId == p.ProductId
                && other.ValidFrom <= today && other.ValidFrom > p.ValidFrom));
        }

        return Project(
                prices
                    .OrderBy(p => p.Supplier.Name).ThenBy(p => p.Operation.OperationCode).ThenBy(p => p.ProductId != null)
                    .ThenByDescending(p => p.ValidFrom),
                today)
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<Result<SubcontractPriceDto>> GetAsync(int id, CancellationToken cancellationToken)
    {
        var price = await Project(db.SubcontractPrices.AsNoTracking().IgnoreQueryFilters().Where(p => p.Id == id), Today())
            .SingleOrDefaultAsync(cancellationToken);
        return price is null ? NotFound : price;
    }

    public async Task<Result<SubcontractPriceDto>> CreateAsync(SubcontractPriceRequest request, CancellationToken cancellationToken)
    {
        if (await ValidateAsync(request, null, cancellationToken) is { } error)
        {
            return error;
        }

        var price = new SubcontractPrice();
        Apply(request, price);
        db.SubcontractPrices.Add(price);
        if (await SaveAsync(cancellationToken) is { } saveError)
        {
            return saveError;
        }

        return await GetAsync(price.Id, cancellationToken);
    }

    public async Task<Result<SubcontractPriceDto>> UpdateAsync(int id, SubcontractPriceRequest request, CancellationToken cancellationToken)
    {
        var price = await db.SubcontractPrices.SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (price is null)
        {
            return NotFound;
        }

        if (await ValidateAsync(request, price, cancellationToken) is { } error)
        {
            return error;
        }

        Apply(request, price);
        if (await SaveAsync(cancellationToken) is { } saveError)
        {
            return saveError;
        }

        return await GetAsync(id, cancellationToken);
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var price = await db.SubcontractPrices.SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (price is null)
        {
            return NotFound;
        }

        // Gönderim ve dönüş satırları fiyatı kopyaladığı için silme geçmiş fişleri etkilemez.
        db.SubcontractPrices.Remove(price);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<ResolvedSubcontractPriceDto> ResolveAsync(
        int supplierId, int operationId, int? productId, DateOnly date, CancellationToken cancellationToken)
    {
        var resolved = await ResolveManyAsync(db, [new SubcontractPriceKey(supplierId, operationId, productId)], date, cancellationToken);
        var price = resolved.GetValueOrDefault(new SubcontractPriceKey(supplierId, operationId, productId));
        return new ResolvedSubcontractPriceDto(
            supplierId, operationId, productId, date, price?.Id, price?.UnitPrice, price?.Currency, price?.ValidFrom);
    }

    /// <summary>
    /// Birden fazla fasoncu / operasyon / ürün için verilen gündeki fiyatlar. Bulunamayan anahtar sözlükte yer almaz.
    /// </summary>
    public static async Task<Dictionary<SubcontractPriceKey, SubcontractPrice>> ResolveManyAsync(
        IKonfeksiyonDbContext db, IReadOnlyCollection<SubcontractPriceKey> keys, DateOnly date, CancellationToken cancellationToken)
    {
        if (keys.Count == 0)
        {
            return [];
        }

        var supplierIds = keys.Select(k => k.SupplierId).Distinct().ToList();
        var operationIds = keys.Select(k => k.OperationId).Distinct().ToList();
        var candidates = await db.SubcontractPrices
            .AsNoTracking()
            .Where(p => supplierIds.Contains(p.SupplierId) && operationIds.Contains(p.OperationId) && p.ValidFrom <= date)
            .ToListAsync(cancellationToken);

        var result = new Dictionary<SubcontractPriceKey, SubcontractPrice>();
        foreach (var key in keys.Distinct())
        {
            var match = candidates
                .Where(p => p.SupplierId == key.SupplierId && p.OperationId == key.OperationId
                    && (p.ProductId == null || p.ProductId == key.ProductId))
                .OrderByDescending(p => p.ProductId != null)
                .ThenByDescending(p => p.ValidFrom)
                .FirstOrDefault();
            if (match is not null)
            {
                result[key] = match;
            }
        }

        return result;
    }

    private DateOnly Today() => DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);

    private IQueryable<SubcontractPriceDto> Project(IQueryable<SubcontractPrice> prices, DateOnly today) =>
        prices.Select(p => new SubcontractPriceDto(
            p.Id,
            p.SupplierId,
            p.Supplier.SupplierCode,
            p.Supplier.Name,
            p.OperationId,
            p.Operation.OperationCode,
            p.Operation.Name,
            p.ProductId,
            p.Product != null ? p.Product.ProductCode : null,
            p.Product != null ? p.Product.Name : null,
            p.UnitPrice,
            p.Currency,
            p.ValidFrom,
            p.Notes,
            p.ValidFrom <= today && !db.SubcontractPrices.Any(other =>
                other.SupplierId == p.SupplierId && other.OperationId == p.OperationId && other.ProductId == p.ProductId
                && other.ValidFrom <= today && other.ValidFrom > p.ValidFrom),
            p.UpdatedAt ?? p.CreatedAt,
            p.UpdatedBy ?? p.CreatedBy));

    private async Task<Error?> ValidateAsync(
        SubcontractPriceRequest request, SubcontractPrice? existing, CancellationToken cancellationToken)
    {
        var referenceError =
            await ReferenceValidation.RequireSelectableAsync(
                db.Suppliers, request.SupplierId, existing?.SupplierId, "supplierId", "Fasoncu", cancellationToken)
            ?? await ReferenceValidation.RequireSelectableAsync(
                db.Operations, request.OperationId, existing?.OperationId, "operationId", "Operasyon", cancellationToken)
            ?? await ReferenceValidation.RequireSelectableAsync(
                db.Products, request.ProductId, existing?.ProductId, "productId", "Ürün", cancellationToken)
            ?? await CurrencyRules.RequireActiveAsync(db, request.Currency, existing?.Currency, "currency", cancellationToken);
        if (referenceError is not null)
        {
            return referenceError;
        }

        var duplicate = await db.SubcontractPrices.AnyAsync(
            p => p.SupplierId == request.SupplierId && p.OperationId == request.OperationId && p.ProductId == request.ProductId
                && p.ValidFrom == request.ValidFrom && p.Id != (existing == null ? 0 : existing.Id),
            cancellationToken);
        if (!duplicate)
        {
            return null;
        }

        var message = "Bu fasoncu, operasyon ve ürün için aynı başlangıç tarihli bir fiyat zaten var; onu düzenleyin.";
        return new Error(
            SubcontractErrors.PriceOverlap, message, ErrorType.Conflict, new Dictionary<string, string[]> { ["validFrom"] = [message] });
    }

    private static void Apply(SubcontractPriceRequest request, SubcontractPrice price)
    {
        price.SupplierId = request.SupplierId;
        price.OperationId = request.OperationId;
        price.ProductId = request.ProductId;
        price.UnitPrice = request.UnitPrice;
        price.Currency = string.IsNullOrWhiteSpace(request.Currency)
            ? ERP.Core.Domain.Entities.Currency.LocalCode
            : request.Currency.Trim().ToUpperInvariant();
        price.ValidFrom = request.ValidFrom;
        price.Notes = NullIfBlank(request.Notes);
    }

    private async Task<Error?> SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return null;
        }
        catch (DbUpdateException)
        {
            const string message = "Bu fasoncu, operasyon ve ürün için aynı başlangıç tarihli bir fiyat aynı anda kaydedildi.";
            return new Error(
                SubcontractErrors.PriceOverlap, message, ErrorType.Conflict,
                new Dictionary<string, string[]> { ["validFrom"] = [message] });
        }
    }
}
