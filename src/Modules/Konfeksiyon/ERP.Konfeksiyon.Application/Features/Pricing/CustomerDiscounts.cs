using ERP.Core.Application.Features.MasterData;
using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Konfeksiyon.Domain.Entities;
using ERP.Core.Application.Common;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Konfeksiyon.Application.Features.Pricing;

/// <param name="ProductId">Boşsa müşterinin genel iskontosu.</param>
/// <param name="IsCurrent">Bugün geçerli.</param>
public sealed record CustomerDiscountDto(
    int Id,
    int CustomerId,
    string CustomerCode,
    string CustomerName,
    int? ProductId,
    string? ProductCode,
    string? ProductName,
    decimal DiscountPercent,
    DateOnly ValidFrom,
    DateOnly? ValidTo,
    string? Notes,
    bool IsCurrent,
    DateTime? UpdatedAt,
    string? UpdatedBy);

public sealed class CustomerDiscountQuery : PageRequest
{
    /// <summary>Müşteri ya da ürün kodu / adı içinde aranır.</summary>
    public string? Search { get; set; }
    public int? CustomerId { get; set; }
    public bool? CurrentOnly { get; set; }
}

public sealed record CustomerDiscountRequest(
    int CustomerId, int? ProductId, decimal DiscountPercent, DateOnly ValidFrom, DateOnly? ValidTo, string? Notes = null);

public sealed class CustomerDiscountRequestValidator : AbstractValidator<CustomerDiscountRequest>
{
    public CustomerDiscountRequestValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0).WithMessage("Müşteri seçilmelidir.");
        RuleFor(x => x.DiscountPercent)
            .GreaterThan(0).WithMessage("İskonto sıfırdan büyük olmalıdır.")
            .LessThan(100).WithMessage("İskonto %100'den küçük olmalıdır.")
            .Must(value => decimal.Round(value, 2) == value).WithMessage("İskonto en fazla 2 ondalık basamak içerebilir.");
        RuleFor(x => x.ValidFrom).NotEqual(default(DateOnly)).WithMessage("Başlangıç tarihi zorunludur.");
        RuleFor(x => x.ValidTo)
            .GreaterThanOrEqualTo(x => x.ValidFrom).WithMessage("Bitiş tarihi başlangıçtan önce olamaz.")
            .When(x => x.ValidTo is not null);
        RuleFor(x => x.Notes).OptionalText(500);
    }
}

public interface ICustomerDiscountService
{
    Task<PagedResult<CustomerDiscountDto>> GetListAsync(CustomerDiscountQuery query, CancellationToken cancellationToken);
    Task<Result<CustomerDiscountDto>> GetAsync(int id, CancellationToken cancellationToken);
    Task<Result<CustomerDiscountDto>> CreateAsync(CustomerDiscountRequest request, CancellationToken cancellationToken);
    Task<Result<CustomerDiscountDto>> UpdateAsync(int id, CustomerDiscountRequest request, CancellationToken cancellationToken);
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken);
}

/// <summary>
/// Müşteri iskontoları. Aynı müşteri ve ürün (ya da genel iskonto) için geçerlilik aralıkları çakışamaz; böylece bir
/// gün için hangi iskontonun uygulanacağı her zaman bellidir.
/// </summary>
public sealed class CustomerDiscountService(IKonfeksiyonDbContext db, TimeProvider timeProvider) : ICustomerDiscountService
{
    private static readonly Error NotFound = new(ErrorCodes.NotFound, "İskonto bulunamadı.", ErrorType.NotFound);

    public Task<PagedResult<CustomerDiscountDto>> GetListAsync(CustomerDiscountQuery query, CancellationToken cancellationToken)
    {
        var today = Today();
        var discounts = db.CustomerDiscounts.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            discounts = discounts.Where(d => d.Customer.CustomerCode.Contains(search) || d.Customer.Name.Contains(search)
                || (d.Product != null && (d.Product.ProductCode.Contains(search) || d.Product.Name.Contains(search))));
        }

        if (query.CustomerId is { } customerId)
        {
            discounts = discounts.Where(d => d.CustomerId == customerId);
        }

        if (query.CurrentOnly == true)
        {
            discounts = discounts.Where(d => d.ValidFrom <= today && (d.ValidTo == null || d.ValidTo >= today));
        }

        return Project(discounts.OrderBy(d => d.Customer.CustomerCode).ThenBy(d => d.ProductId != null).ThenByDescending(d => d.ValidFrom), today)
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<Result<CustomerDiscountDto>> GetAsync(int id, CancellationToken cancellationToken)
    {
        var discount = await Project(db.CustomerDiscounts.AsNoTracking().IgnoreQueryFilters().Where(d => d.Id == id), Today())
            .SingleOrDefaultAsync(cancellationToken);
        return discount is null ? NotFound : discount;
    }

    public async Task<Result<CustomerDiscountDto>> CreateAsync(CustomerDiscountRequest request, CancellationToken cancellationToken)
    {
        if (await ValidateAsync(request, null, cancellationToken) is { } error)
        {
            return error;
        }

        var discount = new CustomerDiscount();
        Apply(request, discount);
        db.CustomerDiscounts.Add(discount);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(discount.Id, cancellationToken);
    }

    public async Task<Result<CustomerDiscountDto>> UpdateAsync(int id, CustomerDiscountRequest request, CancellationToken cancellationToken)
    {
        var discount = await db.CustomerDiscounts.SingleOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (discount is null)
        {
            return NotFound;
        }

        if (await ValidateAsync(request, discount, cancellationToken) is { } error)
        {
            return error;
        }

        Apply(request, discount);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var discount = await db.CustomerDiscounts.SingleOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (discount is null)
        {
            return NotFound;
        }

        db.CustomerDiscounts.Remove(discount);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private DateOnly Today() => DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);

    private static IQueryable<CustomerDiscountDto> Project(IQueryable<CustomerDiscount> discounts, DateOnly today) =>
        discounts.Select(d => new CustomerDiscountDto(
            d.Id, d.CustomerId, d.Customer.CustomerCode, d.Customer.Name, d.ProductId,
            d.Product != null ? d.Product.ProductCode : null, d.Product != null ? d.Product.Name : null,
            d.DiscountPercent, d.ValidFrom, d.ValidTo, d.Notes,
            d.ValidFrom <= today && (d.ValidTo == null || d.ValidTo >= today),
            d.UpdatedAt ?? d.CreatedAt, d.UpdatedBy ?? d.CreatedBy));

    private async Task<Error?> ValidateAsync(
        CustomerDiscountRequest request, CustomerDiscount? existing, CancellationToken cancellationToken)
    {
        var referenceError =
            await ReferenceValidation.RequireSelectableAsync(
                db.Customers, request.CustomerId, existing?.CustomerId, "customerId", "Müşteri", cancellationToken)
            ?? await ReferenceValidation.RequireSelectableAsync(
                db.Products, request.ProductId, existing?.ProductId, "productId", "Ürün", cancellationToken);
        if (referenceError is not null)
        {
            return referenceError;
        }

        // Aynı müşteri ve ürün için iki iskonto aynı günde geçerli olamaz.
        var end = request.ValidTo ?? DateOnly.MaxValue;
        var overlap = await db.CustomerDiscounts.AsNoTracking()
            .Where(d => d.CustomerId == request.CustomerId && d.ProductId == request.ProductId
                && d.Id != (existing == null ? 0 : existing.Id)
                && d.ValidFrom <= end && (d.ValidTo == null || d.ValidTo >= request.ValidFrom))
            .Select(d => new { d.ValidFrom, d.ValidTo, d.DiscountPercent })
            .FirstOrDefaultAsync(cancellationToken);
        if (overlap is null)
        {
            return null;
        }

        var range = overlap.ValidTo is null ? $"{overlap.ValidFrom:dd.MM.yyyy} ve sonrası" : $"{overlap.ValidFrom:dd.MM.yyyy}–{overlap.ValidTo:dd.MM.yyyy}";
        var message = $"Bu tarihlerde geçerli başka bir iskonto var (%{overlap.DiscountPercent:0.##}, {range}). Önce onun bitiş tarihini düzeltin.";
        return new Error(
            ErrorCodes.DiscountOverlap, message, ErrorType.Conflict, new Dictionary<string, string[]> { ["validFrom"] = [message] });
    }

    private static void Apply(CustomerDiscountRequest request, CustomerDiscount discount)
    {
        discount.CustomerId = request.CustomerId;
        discount.ProductId = request.ProductId;
        discount.DiscountPercent = request.DiscountPercent;
        discount.ValidFrom = request.ValidFrom;
        discount.ValidTo = request.ValidTo;
        discount.Notes = NullIfBlank(request.Notes);
    }
}
