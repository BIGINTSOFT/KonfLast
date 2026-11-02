using ERP.Core.Application.Features.MasterData;
using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Konfeksiyon.Domain.Entities;
using ERP.Konfeksiyon.Domain.Enums;
using System.Linq.Expressions;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Domain.Authorization;
using ERP.Core.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Konfeksiyon.Application.Features.MasterData;

public sealed record ProductDto(
    int Id,
    string ProductCode,
    string Name,
    string? Description,
    int ModelId,
    string ModelCode,
    string ModelName,
    bool IsActive,
    int? ProductGroupId,
    string? ProductGroupName,
    TargetGroup? TargetGroup,
    string? Season,
    string? CustomerArticleCode,
    string? Composition,
    int? FabricWeight,
    string? CareInstructions,
    string OriginCountryCode,
    string? GtipCode,
    decimal VatRate,
    StockUnit SalesUnit,
    decimal? ListPrice,
    string Currency,
    string? CustomerName);

/// <param name="GtipCode">12 haneli GTİP; nokta ve boşluklarla yazılabilir, rakamları saklanır.</param>
/// <param name="VatRate">KDV oranı (%); verilmezse %10.</param>
/// <param name="Currency">Liste fiyatının para birimi (ISO 4217); boşsa TRY.</param>
public sealed record ProductRequest(
    string ProductCode,
    string Name,
    string? Description,
    int ModelId,
    bool IsActive = true,
    int? ProductGroupId = null,
    TargetGroup? TargetGroup = null,
    string? Season = null,
    string? CustomerArticleCode = null,
    string? Composition = null,
    int? FabricWeight = null,
    string? CareInstructions = null,
    string? OriginCountryCode = null,
    string? GtipCode = null,
    decimal? VatRate = null,
    StockUnit SalesUnit = StockUnit.Piece,
    decimal? ListPrice = null,
    string? Currency = null);

public sealed class ProductRequestValidator : AbstractValidator<ProductRequest>
{
    public ProductRequestValidator()
    {
        RuleFor(x => x.ProductCode).ValidCode();
        RuleFor(x => x.Name).ValidName(200);
        RuleFor(x => x.Description).OptionalText(500);
        RuleFor(x => x.ModelId).GreaterThan(0).WithMessage("Model seçilmelidir.");
        RuleFor(x => x.TargetGroup).IsInEnum().WithMessage("Geçerli bir hedef grup seçin.");
        RuleFor(x => x.Season).OptionalText(20);
        RuleFor(x => x.CustomerArticleCode).OptionalText(50);
        RuleFor(x => x.Composition).OptionalText(200);
        RuleFor(x => x.CareInstructions).OptionalText(500);
        RuleFor(x => x.FabricWeight)
            .InclusiveBetween(1, 2000).WithMessage("Gramaj 1–2000 g/m² arasında olmalıdır.")
            .When(x => x.FabricWeight is not null);
        RuleFor(x => x.OriginCountryCode)
            .Matches(@"^\s*[A-Za-z]{2}\s*$").WithMessage("Menşe ülke kodu iki harf olmalıdır (örn. TR).")
            .When(x => !string.IsNullOrWhiteSpace(x.OriginCountryCode));
        RuleFor(x => x.GtipCode)
            .Must(value => DigitsOf(value).Length == 12).WithMessage("GTİP kodu 12 haneli olmalıdır (örn. 6109.10.00.10.00).")
            .Must(value => value!.All(c => char.IsAsciiDigit(c) || c is '.' or ' '))
            .WithMessage("GTİP kodu yalnızca rakam ve nokta içerebilir.")
            .When(x => !string.IsNullOrWhiteSpace(x.GtipCode));
        RuleFor(x => x.VatRate)
            .InclusiveBetween(0, 100).WithMessage("KDV oranı 0 ile 100 arasında olmalıdır.")
            .When(x => x.VatRate is not null);
        RuleFor(x => x.SalesUnit).IsInEnum().WithMessage("Geçerli bir satış birimi seçin.");
        RuleFor(x => x.ListPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Liste fiyatı negatif olamaz.")
            .When(x => x.ListPrice is not null);
        RuleFor(x => x.Currency)
            .Matches(@"^\s*[A-Za-z]{3}\s*$").WithMessage("Para birimi üç harfli kod olmalıdır (örn. TRY, USD, EUR).")
            .When(x => !string.IsNullOrWhiteSpace(x.Currency));
    }

    internal static string DigitsOf(string? value) => new((value ?? string.Empty).Where(char.IsAsciiDigit).ToArray());
}

public sealed class ProductService(IKonfeksiyonDbContext db, ICurrentPermissions permissions, IReferenceUsage usage)
    : KonfeksiyonMasterDataService<Product, ProductDto, ProductRequest>(db)
{
    /// <summary>Fiyat yönetme yetkisi yoksa mevcut liste fiyatı korunur (gizli alan boş gelir).</summary>
    private bool _keepListPrice;

    // Liste fiyatı fiyat listeleriyle aynı yetkiye bağlıdır: görmek fiyat görüntüleme, değiştirmek fiyat yönetimi ister.
    protected override async Task<Func<ProductDto, ProductDto>?> PresenterAsync(CancellationToken cancellationToken) =>
        await permissions.HasAsync(Permissions.Pricing.View, cancellationToken) ? null : dto => dto with { ListPrice = null };

    protected override DbSet<Product> Set => Db.Products;
    protected override string Label => "Ürün";
    protected override string CodeField => "productCode";

    protected override string CodeOf(ProductRequest request) => request.ProductCode;
    protected override Expression<Func<Product, bool>> WithCode(string code) => e => e.ProductCode == code;

    protected override Expression<Func<Product, bool>> Matches(string term) =>
        e => e.ProductCode.Contains(term) || e.Name.Contains(term) || e.Model.ModelCode.Contains(term)
            || (e.CustomerArticleCode != null && e.CustomerArticleCode.Contains(term));

    protected override IQueryable<Product> Order(IQueryable<Product> query) => query.OrderBy(e => e.ProductCode);

    // Siparişte müşteri seçiliyse yalnızca ona satılabilen ürünler: modeli müşteriye özel olmayan ya da bu müşterinin modeli.
    protected override IQueryable<Product> FilterLookup(IQueryable<Product> items, LookupQuery query) =>
        query.CustomerId is { } customerId
            ? items.Where(e => e.Model.CustomerId == null || e.Model.CustomerId == customerId)
            : items;

    protected override Expression<Func<Product, ProductDto>> ToDto => e => new ProductDto(
        e.Id, e.ProductCode, e.Name, e.Description, e.ModelId, e.Model.ModelCode, e.Model.Name, e.IsActive,
        e.ProductGroupId, e.ProductGroup!.Name, e.TargetGroup, e.Season, e.CustomerArticleCode, e.Composition,
        e.FabricWeight, e.CareInstructions, e.OriginCountryCode, e.GtipCode, e.VatRate, e.SalesUnit, e.ListPrice,
        e.Currency, e.Model.Customer!.Name);

    protected override Expression<Func<Product, LookupDto>> ToLookup =>
        e => new LookupDto(e.Id, e.ProductCode, e.Name) { Detail = e.CustomerArticleCode };

    protected override void Apply(ProductRequest request, Product entity, string code)
    {
        entity.ProductCode = code;
        entity.Name = request.Name.Trim();
        entity.Description = NullIfBlank(request.Description);
        entity.ModelId = request.ModelId;
        entity.ProductGroupId = request.ProductGroupId;
        entity.TargetGroup = request.TargetGroup;
        entity.Season = NullIfBlank(request.Season)?.ToUpperInvariant();
        entity.CustomerArticleCode = NullIfBlank(request.CustomerArticleCode);
        entity.Composition = NullIfBlank(request.Composition);
        entity.FabricWeight = request.FabricWeight;
        entity.CareInstructions = NullIfBlank(request.CareInstructions);
        entity.OriginCountryCode = NullIfBlank(request.OriginCountryCode)?.ToUpperInvariant() ?? "TR";
        entity.GtipCode = string.IsNullOrWhiteSpace(request.GtipCode) ? null : ProductRequestValidator.DigitsOf(request.GtipCode);
        entity.VatRate = request.VatRate ?? 10;
        entity.SalesUnit = request.SalesUnit;
        if (!_keepListPrice)
        {
            entity.ListPrice = request.ListPrice;
        }
        entity.Currency = NullIfBlank(request.Currency)?.ToUpperInvariant() ?? "TRY";
        entity.IsActive = request.IsActive;
    }

    protected override async Task<Error?> ValidateAsync(
        ProductRequest request, Product? existing, CancellationToken cancellationToken)
    {
        _keepListPrice = !await permissions.HasAsync(Permissions.Pricing.Manage, cancellationToken);
        if (_keepListPrice && request.ListPrice is not null && request.ListPrice != existing?.ListPrice)
        {
            const string message = "Liste fiyatını değiştirme yetkiniz yok (Fiyat listesi düzenleme).";
            return new Error(
                ErrorCodes.Forbidden, message, ErrorType.Forbidden,
                new Dictionary<string, string[]> { ["listPrice"] = [message] });
        }

        return await ValidateReferencesAsync(request, existing, cancellationToken);
    }

    private async Task<Error?> ValidateReferencesAsync(
        ProductRequest request, Product? existing, CancellationToken cancellationToken) =>
        await RequireSelectableAsync(Db.Models, request.ModelId, existing?.ModelId, "modelId", "Model", cancellationToken)
        ?? await RequireSelectableAsync(
            Db.ProductGroups, request.ProductGroupId, existing?.ProductGroupId, "productGroupId", "Ürün grubu",
            cancellationToken)
        ?? await CurrencyRules.RequireActiveAsync(Db, request.Currency, existing?.Currency, "currency", cancellationToken);

    protected override async Task<string?> FindUsageAsync(int id, CancellationToken cancellationToken)
    {
        if (await usage.FindAsync<Product>(id, cancellationToken) is { } usedIn)
        {
            return usedIn;
        }

        return await Db.PriceListItems.AnyAsync(i => i.ProductId == id && !i.PriceList.IsDeleted, cancellationToken) ? "fiyat listeleri" : null;
    }
}
