using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Konfeksiyon.Domain.Entities;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Core.Domain.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Konfeksiyon.Application.Features.Pricing;

/// <param name="CustomerName">Boşsa genel liste.</param>
/// <param name="IsCurrent">Bugün geçerli (aktif ve tarih aralığında).</param>
public sealed record PriceListListItemDto(
    int Id,
    string Code,
    string Name,
    int? CustomerId,
    string? CustomerName,
    string Currency,
    DateOnly ValidFrom,
    DateOnly? ValidTo,
    bool IsActive,
    int ItemCount,
    bool IsCurrent);

public sealed record PriceListItemDto(
    int Id,
    int ProductId,
    string ProductCode,
    string ProductName,
    int? ColorId,
    string? ColorName,
    int? SizeId,
    string? SizeName,
    decimal UnitPrice);

public sealed record PriceListDto(
    int Id,
    string Code,
    string Name,
    int? CustomerId,
    string? CustomerCode,
    string? CustomerName,
    string Currency,
    DateOnly ValidFrom,
    DateOnly? ValidTo,
    string? Notes,
    bool IsActive,
    DateTime CreatedAt,
    string? CreatedBy,
    DateTime? UpdatedAt,
    string? UpdatedBy,
    IReadOnlyList<PriceListItemDto> Items);

public sealed class PriceListQuery : PageRequest
{
    /// <summary>Kod, ad ya da müşteri adı içinde aranır.</summary>
    public string? Search { get; set; }
    public int? CustomerId { get; set; }

    /// <summary>Yalnızca bugün geçerli listeler.</summary>
    public bool? CurrentOnly { get; set; }
}

/// <param name="ColorId">Boşsa ürünün tüm renkleri.</param>
/// <param name="SizeId">Boşsa ürünün tüm bedenleri.</param>
public sealed record PriceListItemRequest(int ProductId, int? ColorId, int? SizeId, decimal UnitPrice);

public sealed record PriceListRequest(
    string Code,
    string Name,
    int? CustomerId,
    string? Currency,
    DateOnly ValidFrom,
    DateOnly? ValidTo,
    string? Notes,
    IReadOnlyList<PriceListItemRequest> Items,
    bool IsActive = true);

public sealed class PriceListRequestValidator : AbstractValidator<PriceListRequest>
{
    public const int MaxItems = 5000;
    public const decimal MaxUnitPrice = 100_000_000m;

    public PriceListRequestValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName(200);
        RuleFor(x => x.Notes).OptionalText(500);
        RuleFor(x => x.ValidFrom).NotEqual(default(DateOnly)).WithMessage("Başlangıç tarihi zorunludur.");
        RuleFor(x => x.ValidTo)
            .GreaterThanOrEqualTo(x => x.ValidFrom).WithMessage("Bitiş tarihi başlangıçtan önce olamaz.")
            .When(x => x.ValidTo is not null);
        RuleFor(x => x.Currency)
            .Matches(@"^\s*[A-Za-z]{3}\s*$").WithMessage("Para birimi üç harfli kod olmalıdır.")
            .When(x => !string.IsNullOrWhiteSpace(x.Currency));

        RuleFor(x => x.Items).NotNull().WithMessage("Kalem listesi zorunludur.");
        When(x => x.Items is not null, () =>
        {
            RuleFor(x => x.Items)
                .Must(items => items.Count <= MaxItems).WithMessage($"Bir listede en fazla {MaxItems} kalem olabilir.")
                .Must(items => items.All(i => i.ProductId > 0)).WithMessage("Her kalemde ürün seçilmelidir.")
                .Must(items => items.All(i => i.UnitPrice >= 0 && i.UnitPrice <= MaxUnitPrice && decimal.Round(i.UnitPrice, 4) == i.UnitPrice))
                .WithMessage("Fiyat negatif olamaz ve en fazla 4 ondalık basamak içerebilir.")
                .Must(items => items.Select(i => (i.ProductId, i.ColorId, i.SizeId)).Distinct().Count() == items.Count)
                .WithMessage("Aynı ürün, renk ve beden birleşimi listede birden fazla kez yer alamaz.");
        });
    }
}

public interface IPriceListService
{
    Task<PagedResult<PriceListListItemDto>> GetListAsync(PriceListQuery query, CancellationToken cancellationToken);
    Task<Result<PriceListDto>> GetAsync(int id, CancellationToken cancellationToken);
    Task<Result<PriceListDto>> CreateAsync(PriceListRequest request, CancellationToken cancellationToken);
    Task<Result<PriceListDto>> UpdateAsync(int id, PriceListRequest request, CancellationToken cancellationToken);
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken);
}

/// <summary>Satış fiyat listeleri ve kalemleri.</summary>
public sealed class PriceListService(IKonfeksiyonDbContext db, TimeProvider timeProvider) : IPriceListService
{
    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Fiyat listesi bulunamadı.", ErrorType.NotFound);

    public Task<PagedResult<PriceListListItemDto>> GetListAsync(PriceListQuery query, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
        var lists = db.PriceLists.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            lists = lists.Where(p => p.Code.Contains(search) || p.Name.Contains(search)
                || (p.Customer != null && p.Customer.Name.Contains(search)));
        }

        if (query.CustomerId is { } customerId)
        {
            lists = lists.Where(p => p.CustomerId == customerId);
        }

        if (query.CurrentOnly == true)
        {
            lists = lists.Where(p => p.IsActive && p.ValidFrom <= today && (p.ValidTo == null || p.ValidTo >= today));
        }

        return lists
            .OrderByDescending(p => p.ValidFrom).ThenBy(p => p.Code)
            .Select(p => new PriceListListItemDto(
                p.Id, p.Code, p.Name, p.CustomerId, p.Customer != null ? p.Customer.Name : null, p.Currency,
                p.ValidFrom, p.ValidTo, p.IsActive, p.Items.Count,
                p.IsActive && p.ValidFrom <= today && (p.ValidTo == null || p.ValidTo >= today)))
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<Result<PriceListDto>> GetAsync(int id, CancellationToken cancellationToken)
    {
        var list = await db.PriceLists.AsNoTracking()
            .Include(p => p.Customer)
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (list is null)
        {
            return NotFound;
        }

        // Sonradan silinen ürün / renk / beden de adıyla görünür.
        var items = await db.PriceListItems.AsNoTracking().IgnoreQueryFilters()
            .Where(i => i.PriceListId == id)
            .OrderBy(i => i.Product.ProductCode)
            .ThenBy(i => i.Color == null ? string.Empty : i.Color.ColorCode)
            .ThenBy(i => i.Size == null ? -1 : i.Size.SortOrder)
            .Select(i => new PriceListItemDto(
                i.Id, i.ProductId, i.Product.ProductCode, i.Product.Name,
                i.ColorId, i.Color != null ? i.Color.Name : null,
                i.SizeId, i.Size != null ? i.Size.Name : null,
                i.UnitPrice))
            .ToListAsync(cancellationToken);

        return new PriceListDto(
            list.Id, list.Code, list.Name, list.CustomerId, list.Customer?.CustomerCode, list.Customer?.Name, list.Currency,
            list.ValidFrom, list.ValidTo, list.Notes, list.IsActive, list.CreatedAt, list.CreatedBy, list.UpdatedAt,
            list.UpdatedBy, items);
    }

    public async Task<Result<PriceListDto>> CreateAsync(PriceListRequest request, CancellationToken cancellationToken)
    {
        if (await ValidateAsync(request, null, cancellationToken) is { } error)
        {
            return error;
        }

        var list = new PriceList();
        Apply(request, list);
        foreach (var item in request.Items)
        {
            list.Items.Add(new PriceListItem
            {
                ProductId = item.ProductId, ColorId = item.ColorId, SizeId = item.SizeId, UnitPrice = item.UnitPrice,
            });
        }

        db.PriceLists.Add(list);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(list.Id, cancellationToken);
    }

    public async Task<Result<PriceListDto>> UpdateAsync(int id, PriceListRequest request, CancellationToken cancellationToken)
    {
        var list = await db.PriceLists.Include(p => p.Items).SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (list is null)
        {
            return NotFound;
        }

        if (await ValidateAsync(request, list, cancellationToken) is { } error)
        {
            return error;
        }

        Apply(request, list);

        // Kalemler istekle eşitlenir: kalan güncellenir, eksilen silinir, yeni gelen eklenir.
        var wanted = request.Items.ToDictionary(i => (i.ProductId, i.ColorId, i.SizeId));
        foreach (var item in list.Items.ToList())
        {
            if (wanted.Remove((item.ProductId, item.ColorId, item.SizeId), out var match))
            {
                item.UnitPrice = match.UnitPrice;
            }
            else
            {
                db.PriceListItems.Remove(item);
            }
        }

        foreach (var item in wanted.Values)
        {
            db.PriceListItems.Add(new PriceListItem
            {
                PriceListId = list.Id, ProductId = item.ProductId, ColorId = item.ColorId, SizeId = item.SizeId,
                UnitPrice = item.UnitPrice,
            });
        }

        // Yalnızca kalemler değişse de listenin güncellenme bilgisi ilerler.
        db.Entry(list).State = EntityState.Modified;
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var list = await db.PriceLists.SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (list is null)
        {
            return NotFound;
        }

        // Soft delete: siparişlerdeki fiyatlar sipariş kalemine yazıldığı için liste silinse de değişmez.
        db.PriceLists.Remove(list);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<Error?> ValidateAsync(PriceListRequest request, PriceList? existing, CancellationToken cancellationToken)
    {
        var code = NormalizeCode(request.Code);
        if (await db.PriceLists.AnyAsync(p => p.Code == code && p.Id != (existing == null ? 0 : existing.Id), cancellationToken))
        {
            var message = $"'{code}' kodlu bir fiyat listesi zaten var.";
            return new Error(
                ErrorCodes.CodeAlreadyExists, message, ErrorType.Conflict, new Dictionary<string, string[]> { ["code"] = [message] });
        }

        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var colorIds = request.Items.Where(i => i.ColorId is not null).Select(i => i.ColorId!.Value).Distinct().ToList();
        var sizeIds = request.Items.Where(i => i.SizeId is not null).Select(i => i.SizeId!.Value).Distinct().ToList();
        var referenceError =
            await ReferenceValidation.RequireSelectableAsync(
                db.Customers, request.CustomerId, existing?.CustomerId, "customerId", "Müşteri", cancellationToken)
            ?? await CurrencyRules.RequireActiveAsync(db, request.Currency ?? Currency.LocalCode, existing?.Currency, "currency", cancellationToken)
            ?? await ReferenceValidation.RequireSelectableAsync(
                db.Products, productIds, existing?.Items.Select(i => i.ProductId).ToHashSet() ?? [], "items", "Ürün", cancellationToken)
            ?? await ReferenceValidation.RequireSelectableAsync(
                db.Colors, colorIds, existing?.Items.Where(i => i.ColorId != null).Select(i => i.ColorId!.Value).ToHashSet() ?? [],
                "items", "Renk", cancellationToken)
            ?? await ReferenceValidation.RequireSelectableAsync(
                db.Sizes, sizeIds, existing?.Items.Where(i => i.SizeId != null).Select(i => i.SizeId!.Value).ToHashSet() ?? [],
                "items", "Beden", cancellationToken);
        if (referenceError is not null)
        {
            return referenceError;
        }

        // Müşteriye özel listede başka müşterinin modeline ait ürün yer alamaz.
        if (request.CustomerId is { } customerId)
        {
            var foreign = await db.Products.AsNoTracking().IgnoreQueryFilters()
                .Where(p => productIds.Contains(p.Id) && p.Model.CustomerId != null && p.Model.CustomerId != customerId)
                .Select(p => p.ProductCode)
                .FirstOrDefaultAsync(cancellationToken);
            if (foreign is not null)
            {
                return ReferenceValidation.InvalidReference("items", $"{foreign}: ürünün modeli başka bir müşteriye ait.");
            }
        }

        return null;
    }

    private static void Apply(PriceListRequest request, PriceList list)
    {
        list.Code = NormalizeCode(request.Code);
        list.Name = request.Name.Trim();
        list.CustomerId = request.CustomerId;
        list.Currency = NullIfBlank(request.Currency)?.ToUpperInvariant() ?? Currency.LocalCode;
        list.ValidFrom = request.ValidFrom;
        list.ValidTo = request.ValidTo;
        list.Notes = NullIfBlank(request.Notes);
        list.IsActive = request.IsActive;
    }
}
