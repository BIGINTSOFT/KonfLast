using ERP.Core.Application.Features.MasterData;
using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Konfeksiyon.Domain.Entities;
using System.Linq.Expressions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ERP.Konfeksiyon.Application.Features.MasterData;

/// <param name="ProductCount">Bu gruptaki ürün sayısı.</param>
public sealed record ProductGroupDto(int Id, string Code, string Name, bool IsActive, int ProductCount);

public sealed record ProductGroupRequest(string Code, string Name, bool IsActive = true);

public sealed class ProductGroupRequestValidator : AbstractValidator<ProductGroupRequest>
{
    public ProductGroupRequestValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName(100);
    }
}

public sealed class ProductGroupService(IKonfeksiyonDbContext db)
    : KonfeksiyonMasterDataService<ProductGroup, ProductGroupDto, ProductGroupRequest>(db)
{
    protected override DbSet<ProductGroup> Set => Db.ProductGroups;
    protected override string Label => "Ürün grubu";
    protected override string CodeField => "code";

    protected override string CodeOf(ProductGroupRequest request) => request.Code;
    protected override Expression<Func<ProductGroup, bool>> WithCode(string code) => e => e.Code == code;

    protected override Expression<Func<ProductGroup, bool>> Matches(string term) =>
        e => e.Code.Contains(term) || e.Name.Contains(term);

    protected override IQueryable<ProductGroup> Order(IQueryable<ProductGroup> query) => query.OrderBy(e => e.Name);

    protected override Expression<Func<ProductGroup, ProductGroupDto>> ToDto => e => new ProductGroupDto(
        e.Id, e.Code, e.Name, e.IsActive, Db.Products.Count(p => p.ProductGroupId == e.Id));

    protected override Expression<Func<ProductGroup, LookupDto>> ToLookup => e => new LookupDto(e.Id, e.Code, e.Name);

    protected override void Apply(ProductGroupRequest request, ProductGroup entity, string code)
    {
        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.IsActive = request.IsActive;
    }

    protected override async Task<string?> FindUsageAsync(int id, CancellationToken cancellationToken) =>
        await Db.Products.AnyAsync(p => p.ProductGroupId == id, cancellationToken) ? "ürünler" : null;
}
