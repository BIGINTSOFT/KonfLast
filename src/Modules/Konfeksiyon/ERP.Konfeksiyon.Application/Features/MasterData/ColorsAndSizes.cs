using ERP.Core.Application.Features.MasterData;
using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Konfeksiyon.Domain.Entities;
using System.Linq.Expressions;
using ERP.Core.Application.Abstractions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Konfeksiyon.Application.Features.MasterData;

public sealed record ColorDto(int Id, string ColorCode, string Name, string? HexCode, bool IsActive);

public sealed record ColorRequest(string ColorCode, string Name, string? HexCode, bool IsActive = true);

public sealed class ColorRequestValidator : AbstractValidator<ColorRequest>
{
    public ColorRequestValidator()
    {
        RuleFor(x => x.ColorCode).ValidCode();
        RuleFor(x => x.Name).ValidName(100);
        RuleFor(x => x.HexCode)
            .Matches("^#[0-9A-Fa-f]{6}$").WithMessage("Renk kodu #RRGGBB biçiminde olmalıdır.")
            .When(x => !string.IsNullOrWhiteSpace(x.HexCode));
    }
}

public sealed class ColorService(IKonfeksiyonDbContext db, IReferenceUsage usage) : KonfeksiyonMasterDataService<Color, ColorDto, ColorRequest>(db)
{
    protected override DbSet<Color> Set => Db.Colors;
    protected override string Label => "Renk";
    protected override string CodeField => "colorCode";

    protected override string CodeOf(ColorRequest request) => request.ColorCode;
    protected override Expression<Func<Color, bool>> WithCode(string code) => e => e.ColorCode == code;

    protected override Expression<Func<Color, bool>> Matches(string term) =>
        e => e.ColorCode.Contains(term) || e.Name.Contains(term);

    protected override IQueryable<Color> Order(IQueryable<Color> query) => query.OrderBy(e => e.ColorCode);

    protected override Expression<Func<Color, ColorDto>> ToDto =>
        e => new ColorDto(e.Id, e.ColorCode, e.Name, e.HexCode, e.IsActive);

    protected override Expression<Func<Color, LookupDto>> ToLookup => e => new LookupDto(e.Id, e.ColorCode, e.Name);

    protected override void Apply(ColorRequest request, Color entity, string code)
    {
        entity.ColorCode = code;
        entity.Name = request.Name.Trim();
        entity.HexCode = NullIfBlank(request.HexCode)?.ToUpperInvariant();
        entity.IsActive = request.IsActive;
    }

    protected override Task<string?> FindUsageAsync(int id, CancellationToken cancellationToken) =>
        usage.FindAsync<Color>(id, cancellationToken);
}

public sealed record SizeDto(int Id, string SizeCode, string Name, int SortOrder, bool IsActive);

public sealed record SizeRequest(string SizeCode, string Name, int SortOrder, bool IsActive = true);

public sealed class SizeRequestValidator : AbstractValidator<SizeRequest>
{
    public SizeRequestValidator()
    {
        RuleFor(x => x.SizeCode).ValidCode();
        RuleFor(x => x.Name).ValidName(100);
        RuleFor(x => x.SortOrder).InclusiveBetween(0, 9999).WithMessage("Sıra 0 ile 9999 arasında olmalıdır.");
    }
}

public sealed class SizeService(IKonfeksiyonDbContext db, IReferenceUsage usage) : KonfeksiyonMasterDataService<Size, SizeDto, SizeRequest>(db)
{
    protected override DbSet<Size> Set => Db.Sizes;
    protected override string Label => "Beden";
    protected override string CodeField => "sizeCode";

    protected override string CodeOf(SizeRequest request) => request.SizeCode;
    protected override Expression<Func<Size, bool>> WithCode(string code) => e => e.SizeCode == code;

    protected override Expression<Func<Size, bool>> Matches(string term) =>
        e => e.SizeCode.Contains(term) || e.Name.Contains(term);

    // Bedenler alfabetik değil, tanımlanan sıraya göre listelenir (S, M, L, XL ...).
    protected override IQueryable<Size> Order(IQueryable<Size> query) =>
        query.OrderBy(e => e.SortOrder).ThenBy(e => e.SizeCode);

    protected override Expression<Func<Size, SizeDto>> ToDto =>
        e => new SizeDto(e.Id, e.SizeCode, e.Name, e.SortOrder, e.IsActive);

    protected override Expression<Func<Size, LookupDto>> ToLookup => e => new LookupDto(e.Id, e.SizeCode, e.Name);

    protected override void Apply(SizeRequest request, Size entity, string code)
    {
        entity.SizeCode = code;
        entity.Name = request.Name.Trim();
        entity.SortOrder = request.SortOrder;
        entity.IsActive = request.IsActive;
    }

    protected override Task<string?> FindUsageAsync(int id, CancellationToken cancellationToken) =>
        usage.FindAsync<Size>(id, cancellationToken);
}
