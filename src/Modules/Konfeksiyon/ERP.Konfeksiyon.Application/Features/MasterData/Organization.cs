using ERP.Konfeksiyon.Application.Abstractions;
using System.Linq.Expressions;
using ERP.Core.Application.Common;
using ERP.Konfeksiyon.Domain.Entities;
using ERP.Konfeksiyon.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Features.MasterData;

namespace ERP.Konfeksiyon.Application.Features.MasterData;

/// <param name="AreaCount">Fabrikadaki üretim alanı sayısı.</param>
/// <param name="LineCount">Fabrikadaki bant sayısı.</param>
/// <param name="MachineCount">Fabrikanın bantlarına şu an yerleşik makine sayısı.</param>
public sealed record FactoryDto(
    int Id,
    string Code,
    string Name,
    bool IsActive,
    string? AddressLine,
    string? District,
    string? City,
    string? PostalCode,
    string CountryCode,
    string? Phone,
    string? Email,
    string? ManagerName,
    string? SgkWorkplaceNo,
    int? IndoorArea,
    int AreaCount,
    int LineCount,
    int MachineCount);

/// <param name="IndoorArea">Kapalı alan (m²).</param>
public sealed record FactoryRequest(
    string Code,
    string Name,
    bool IsActive = true,
    string? AddressLine = null,
    string? District = null,
    string? City = null,
    string? PostalCode = null,
    string? CountryCode = null,
    string? Phone = null,
    string? Email = null,
    string? ManagerName = null,
    string? SgkWorkplaceNo = null,
    int? IndoorArea = null);

public sealed class FactoryRequestValidator : AbstractValidator<FactoryRequest>
{
    public FactoryRequestValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName(150);
        RuleFor(x => x.AddressLine).OptionalText(500);
        RuleFor(x => x.District).OptionalText(100);
        RuleFor(x => x.City).OptionalText(100);
        RuleFor(x => x.Phone).OptionalText(30);
        RuleFor(x => x.ManagerName).OptionalText(100);
        RuleFor(x => x.SgkWorkplaceNo).OptionalText(40);
        RuleFor(x => x.CountryCode)
            .Matches(@"^\s*[A-Za-z]{2}\s*$").WithMessage("Ülke kodu iki harf olmalıdır (örn. TR).")
            .When(x => !string.IsNullOrWhiteSpace(x.CountryCode));
        RuleFor(x => x.PostalCode)
            .Must((request, value) => IsTurkey(request.CountryCode) ? value!.Trim().Length == 5 && value.Trim().All(char.IsAsciiDigit) : value!.Trim().Length <= 10)
            .WithMessage(request => IsTurkey(request.CountryCode) ? "Posta kodu 5 haneli olmalıdır." : "Posta kodu en fazla 10 karakter olabilir.")
            .When(x => !string.IsNullOrWhiteSpace(x.PostalCode));
        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi girin.")
            .MaximumLength(256)
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.IndoorArea)
            .GreaterThan(0).WithMessage("Kapalı alan sıfırdan büyük olmalıdır.")
            .When(x => x.IndoorArea is not null);
    }

    private static bool IsTurkey(string? countryCode) =>
        string.IsNullOrWhiteSpace(countryCode) || countryCode.Trim().Equals("TR", StringComparison.OrdinalIgnoreCase);
}

public sealed class FactoryService(IKonfeksiyonDbContext db) : KonfeksiyonMasterDataService<Factory, FactoryDto, FactoryRequest>(db)
{
    protected override DbSet<Factory> Set => Db.Factories;
    protected override string Label => "Fabrika";
    protected override string CodeField => "code";

    protected override string CodeOf(FactoryRequest request) => request.Code;
    protected override Expression<Func<Factory, bool>> WithCode(string code) => e => e.Code == code;

    protected override Expression<Func<Factory, bool>> Matches(string term) =>
        e => e.Code.Contains(term) || e.Name.Contains(term);

    protected override IQueryable<Factory> Order(IQueryable<Factory> query) => query.OrderBy(e => e.Code);

    protected override Expression<Func<Factory, FactoryDto>> ToDto => e => new FactoryDto(
        e.Id, e.Code, e.Name, e.IsActive, e.AddressLine, e.District, e.City, e.PostalCode, e.CountryCode, e.Phone,
        e.Email, e.ManagerName, e.SgkWorkplaceNo, e.IndoorArea,
        Db.ProductionAreas.Count(a => a.FactoryId == e.Id),
        Db.ProductionLines.Count(l => l.ProductionArea.FactoryId == e.Id),
        Db.LineMachineAssignments.Count(a => a.UnassignedAt == null && a.ProductionLine.ProductionArea.FactoryId == e.Id));

    protected override Expression<Func<Factory, LookupDto>> ToLookup => e => new LookupDto(e.Id, e.Code, e.Name);

    protected override void Apply(FactoryRequest request, Factory entity, string code)
    {
        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.AddressLine = MasterDataRules.NullIfBlank(request.AddressLine);
        entity.District = MasterDataRules.NullIfBlank(request.District);
        entity.City = MasterDataRules.NullIfBlank(request.City);
        entity.PostalCode = MasterDataRules.NullIfBlank(request.PostalCode);
        entity.CountryCode = MasterDataRules.NullIfBlank(request.CountryCode)?.ToUpperInvariant() ?? "TR";
        entity.Phone = MasterDataRules.NullIfBlank(request.Phone);
        entity.Email = MasterDataRules.NullIfBlank(request.Email);
        entity.ManagerName = MasterDataRules.NullIfBlank(request.ManagerName);
        entity.SgkWorkplaceNo = MasterDataRules.NullIfBlank(request.SgkWorkplaceNo);
        entity.IndoorArea = request.IndoorArea;
        entity.IsActive = request.IsActive;
    }

    protected override async Task<string?> FindUsageAsync(int id, CancellationToken cancellationToken) =>
        await Db.ProductionAreas.AnyAsync(a => a.FactoryId == id, cancellationToken) ? "üretim alanları" : null;
}

public sealed record ProductionAreaDto(
    int Id, string Code, string Name, int FactoryId, string FactoryName, bool IsActive);

public sealed record ProductionAreaRequest(string Code, string Name, int FactoryId, bool IsActive = true);

public sealed class ProductionAreaRequestValidator : AbstractValidator<ProductionAreaRequest>
{
    public ProductionAreaRequestValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName(150);
        RuleFor(x => x.FactoryId).GreaterThan(0).WithMessage("Fabrika seçilmelidir.");
    }
}

public sealed class ProductionAreaService(IKonfeksiyonDbContext db)
    : KonfeksiyonMasterDataService<ProductionArea, ProductionAreaDto, ProductionAreaRequest>(db)
{
    protected override DbSet<ProductionArea> Set => Db.ProductionAreas;
    protected override string Label => "Üretim alanı";
    protected override string CodeField => "code";

    protected override string CodeOf(ProductionAreaRequest request) => request.Code;
    protected override Expression<Func<ProductionArea, bool>> WithCode(string code) => e => e.Code == code;

    protected override Expression<Func<ProductionArea, bool>> Matches(string term) =>
        e => e.Code.Contains(term) || e.Name.Contains(term);

    protected override IQueryable<ProductionArea> Order(IQueryable<ProductionArea> query) =>
        query.OrderBy(e => e.Code);

    protected override Expression<Func<ProductionArea, ProductionAreaDto>> ToDto =>
        e => new ProductionAreaDto(e.Id, e.Code, e.Name, e.FactoryId, e.Factory.Name, e.IsActive);

    protected override Expression<Func<ProductionArea, LookupDto>> ToLookup =>
        e => new LookupDto(e.Id, e.Code, e.Name);

    protected override void Apply(ProductionAreaRequest request, ProductionArea entity, string code)
    {
        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.FactoryId = request.FactoryId;
        entity.IsActive = request.IsActive;
    }

    protected override Task<Error?> ValidateAsync(
        ProductionAreaRequest request, ProductionArea? existing, CancellationToken cancellationToken) =>
        RequireSelectableAsync(
            Db.Factories, request.FactoryId, existing?.FactoryId, "factoryId", "Fabrika", cancellationToken);

    protected override async Task<string?> FindUsageAsync(int id, CancellationToken cancellationToken) =>
        await Db.ProductionLines.AnyAsync(l => l.ProductionAreaId == id, cancellationToken) ? "bantlar" : null;
}

/// <param name="MachineCount">Şu an bantta bulunan makine sayısı.</param>
public sealed record ProductionLineDto(
    int Id, string LineCode, string Name, int ProductionAreaId, string ProductionAreaName, string FactoryName,
    int MachineCount, bool IsActive);

public sealed record ProductionLineRequest(string LineCode, string Name, int ProductionAreaId, bool IsActive = true);

public sealed class ProductionLineRequestValidator : AbstractValidator<ProductionLineRequest>
{
    public ProductionLineRequestValidator()
    {
        RuleFor(x => x.LineCode).ValidCode();
        RuleFor(x => x.Name).ValidName(150);
        RuleFor(x => x.ProductionAreaId).GreaterThan(0).WithMessage("Üretim alanı seçilmelidir.");
    }
}

public sealed class ProductionLineService(IKonfeksiyonDbContext db)
    : KonfeksiyonMasterDataService<ProductionLine, ProductionLineDto, ProductionLineRequest>(db)
{
    protected override DbSet<ProductionLine> Set => Db.ProductionLines;
    protected override string Label => "Bant";
    protected override string CodeField => "lineCode";

    protected override string CodeOf(ProductionLineRequest request) => request.LineCode;
    protected override Expression<Func<ProductionLine, bool>> WithCode(string code) => e => e.LineCode == code;

    protected override Expression<Func<ProductionLine, bool>> Matches(string term) =>
        e => e.LineCode.Contains(term) || e.Name.Contains(term);

    protected override IQueryable<ProductionLine> Order(IQueryable<ProductionLine> query) =>
        query.OrderBy(e => e.LineCode);

    protected override Expression<Func<ProductionLine, ProductionLineDto>> ToDto => e => new ProductionLineDto(
        e.Id,
        e.LineCode,
        e.Name,
        e.ProductionAreaId,
        e.ProductionArea.Name,
        e.ProductionArea.Factory.Name,
        Db.LineMachineAssignments.Count(a => a.ProductionLineId == e.Id && a.UnassignedAt == null),
        e.IsActive);

    protected override Expression<Func<ProductionLine, LookupDto>> ToLookup =>
        e => new LookupDto(e.Id, e.LineCode, e.Name);

    protected override void Apply(ProductionLineRequest request, ProductionLine entity, string code)
    {
        entity.LineCode = code;
        entity.Name = request.Name.Trim();
        entity.ProductionAreaId = request.ProductionAreaId;
        entity.IsActive = request.IsActive;
    }

    protected override Task<Error?> ValidateAsync(
        ProductionLineRequest request, ProductionLine? existing, CancellationToken cancellationToken) =>
        RequireSelectableAsync(
            Db.ProductionAreas, request.ProductionAreaId, existing?.ProductionAreaId, "productionAreaId",
            "Üretim alanı", cancellationToken);

    protected override async Task<string?> FindUsageAsync(int id, CancellationToken cancellationToken)
    {
        if (await Db.LineMachineAssignments.AnyAsync(
                a => a.ProductionLineId == id && a.UnassignedAt == null, cancellationToken))
        {
            return "bantta makineler var";
        }

        return await Db.LineProductionPlans.AnyAsync(
            p => p.ProductionLineId == id && p.Status != LinePlanStatus.Cancelled, cancellationToken)
            ? "üretim planları"
            : null;
    }
}
