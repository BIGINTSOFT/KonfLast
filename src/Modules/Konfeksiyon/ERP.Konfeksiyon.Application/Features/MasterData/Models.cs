using ERP.Core.Application.Features.MasterData;
using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Konfeksiyon.Domain.Entities;
using System.Linq.Expressions;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Konfeksiyon.Application.Features.MasterData;

public sealed record ModelDto(
    int Id, string ModelCode, string Name, string? Description, int? CustomerId, string? CustomerName, bool IsActive);

public sealed record ModelRequest(
    string ModelCode, string Name, string? Description, int? CustomerId, bool IsActive = true);

public sealed class ModelRequestValidator : AbstractValidator<ModelRequest>
{
    public ModelRequestValidator()
    {
        RuleFor(x => x.ModelCode).ValidCode();
        RuleFor(x => x.Name).ValidName(200);
        RuleFor(x => x.Description).OptionalText(500);
    }
}

public sealed class ModelService(IKonfeksiyonDbContext db, IReferenceUsage usage) : KonfeksiyonMasterDataService<Model, ModelDto, ModelRequest>(db)
{
    protected override DbSet<Model> Set => Db.Models;
    protected override string Label => "Model";
    protected override string CodeField => "modelCode";

    protected override string CodeOf(ModelRequest request) => request.ModelCode;
    protected override Expression<Func<Model, bool>> WithCode(string code) => e => e.ModelCode == code;

    protected override Expression<Func<Model, bool>> Matches(string term) =>
        e => e.ModelCode.Contains(term) || e.Name.Contains(term)
            || (e.Customer != null && (e.Customer.CustomerCode.Contains(term) || e.Customer.Name.Contains(term)));

    protected override IQueryable<Model> Order(IQueryable<Model> query) => query.OrderBy(e => e.ModelCode);

    protected override Expression<Func<Model, ModelDto>> ToDto => e => new ModelDto(
        e.Id, e.ModelCode, e.Name, e.Description, e.CustomerId, e.Customer!.Name, e.IsActive);

    protected override Expression<Func<Model, LookupDto>> ToLookup =>
        e => new LookupDto(e.Id, e.ModelCode, e.Name) { Detail = e.Customer != null ? e.Customer.Name : null };

    protected override void Apply(ModelRequest request, Model entity, string code)
    {
        entity.ModelCode = code;
        entity.Name = request.Name.Trim();
        entity.Description = NullIfBlank(request.Description);
        entity.CustomerId = request.CustomerId;
        entity.IsActive = request.IsActive;
    }

    protected override Task<Error?> ValidateAsync(
        ModelRequest request, Model? existing, CancellationToken cancellationToken) =>
        RequireSelectableAsync(
            Db.Customers, request.CustomerId, existing?.CustomerId, "customerId", "Müşteri", cancellationToken);

    protected override async Task<string?> FindUsageAsync(int id, CancellationToken cancellationToken)
    {
        if (await Db.Products.AnyAsync(p => p.ModelId == id, cancellationToken))
        {
            return "ürünler";
        }

        return await usage.FindAsync<Model>(id, cancellationToken);
    }
}
