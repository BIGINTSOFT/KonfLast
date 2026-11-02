using System.Linq.Expressions;
using ERP.Boyahane.Domain.Entities;
using ERP.Boyahane.Domain.Enums;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Features.MasterData;
using ERP.Core.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Boyahane.Application.Features.Materials;

/// <param name="KilogramsPer1000Meters">Ham en ve gramajdan önerilen karşılık: 1.000 metre kaç kilogramdır.</param>
public sealed record BoyahaneFabricDto(
    int Id,
    string FabricCode,
    string Name,
    FabricType FabricType,
    string? Structure,
    string? Composition,
    decimal RawWidthCm,
    int RawWeightGsm,
    decimal? FinishedWidthCm,
    int? FinishedWeightGsm,
    decimal? ShrinkageTolerancePercent,
    StockUnit DefaultUnit,
    decimal KilogramsPer1000Meters,
    bool IsActive);

public sealed record BoyahaneFabricRequest(
    string FabricCode,
    string Name,
    decimal RawWidthCm,
    int RawWeightGsm,
    FabricType FabricType = FabricType.Knit,
    string? Structure = null,
    string? Composition = null,
    decimal? FinishedWidthCm = null,
    int? FinishedWeightGsm = null,
    decimal? ShrinkageTolerancePercent = null,
    StockUnit DefaultUnit = StockUnit.Meter,
    bool IsActive = true);

public sealed class BoyahaneFabricRequestValidator : AbstractValidator<BoyahaneFabricRequest>
{
    public BoyahaneFabricRequestValidator()
    {
        RuleFor(x => x.FabricCode).ValidCode();
        RuleFor(x => x.Name).ValidName(200);
        RuleFor(x => x.FabricType).IsInEnum().WithMessage("Geçerli bir kumaş türü seçin.");
        RuleFor(x => x.Structure).OptionalText(100);
        RuleFor(x => x.Composition).OptionalText(200);

        RuleFor(x => x.RawWidthCm)
            .InclusiveBetween(1, 1000).WithMessage("Ham en 1 ile 1000 cm arasında olmalıdır.")
            .PrecisionScale(6, 2, true).WithMessage("Ham en en fazla iki ondalık olabilir.");
        RuleFor(x => x.RawWeightGsm).InclusiveBetween(1, 5000).WithMessage("Ham gramaj 1 ile 5000 g/m² arasında olmalıdır.");

        RuleFor(x => x.FinishedWidthCm)
            .InclusiveBetween(1, 1000).WithMessage("Mamul en 1 ile 1000 cm arasında olmalıdır.")
            .PrecisionScale(6, 2, true).WithMessage("Mamul en en fazla iki ondalık olabilir.")
            .When(x => x.FinishedWidthCm is not null);
        RuleFor(x => x.FinishedWeightGsm)
            .InclusiveBetween(1, 5000).WithMessage("Mamul gramaj 1 ile 5000 g/m² arasında olmalıdır.")
            .When(x => x.FinishedWeightGsm is not null);
        RuleFor(x => x.ShrinkageTolerancePercent)
            .InclusiveBetween(0, 100).WithMessage("Çekme toleransı 0 ile 100 arasında olmalıdır.")
            .PrecisionScale(5, 2, true).WithMessage("Çekme toleransı en fazla iki ondalık olabilir.")
            .When(x => x.ShrinkageTolerancePercent is not null);

        RuleFor(x => x.DefaultUnit)
            .Must(unit => unit is StockUnit.Meter or StockUnit.Kilogram)
            .WithMessage("Varsayılan birim metre ya da kilogram olmalıdır.");
    }
}

public sealed class BoyahaneFabricService(IAppDbContext db)
    : MasterDataService<BoyahaneFabric, BoyahaneFabricDto, BoyahaneFabricRequest>(db)
{
    protected override DbSet<BoyahaneFabric> Set => Db.Set<BoyahaneFabric>();
    protected override string Label => "Kumaş kartı";
    protected override string CodeField => "fabricCode";

    protected override string CodeOf(BoyahaneFabricRequest request) => request.FabricCode;
    protected override Expression<Func<BoyahaneFabric, bool>> WithCode(string code) => e => e.FabricCode == code;

    protected override Expression<Func<BoyahaneFabric, bool>> Matches(string term) =>
        e => e.FabricCode.Contains(term) || e.Name.Contains(term) || (e.Structure != null && e.Structure.Contains(term));

    protected override IQueryable<BoyahaneFabric> Order(IQueryable<BoyahaneFabric> query) => query.OrderBy(e => e.FabricCode);

    // kg = metre × en (m) × gramaj (g/m²) ÷ 1000; 1.000 metrede en (cm) × gramaj ÷ 100'e iner.
    protected override Expression<Func<BoyahaneFabric, BoyahaneFabricDto>> ToDto => e => new BoyahaneFabricDto(
        e.Id, e.FabricCode, e.Name, e.FabricType, e.Structure, e.Composition, e.RawWidthCm, e.RawWeightGsm,
        e.FinishedWidthCm, e.FinishedWeightGsm, e.ShrinkageTolerancePercent, e.DefaultUnit,
        Math.Round(e.RawWidthCm * e.RawWeightGsm / 100m, 2),
        e.IsActive);

    protected override Expression<Func<BoyahaneFabric, LookupDto>> ToLookup =>
        e => new LookupDto(e.Id, e.FabricCode, e.Name);

    protected override async Task<string?> FindUsageAsync(int id, CancellationToken cancellationToken)
    {
        if (await Db.Set<BoyahaneOrderLine>().AnyAsync(l => l.FabricId == id, cancellationToken))
        {
            return "siparişler";
        }

        return await Db.Set<BoyahaneFabricLot>().AnyAsync(l => l.FabricId == id, cancellationToken) ? "kumaş kabul belgeleri" : null;
    }

    protected override void Apply(BoyahaneFabricRequest request, BoyahaneFabric entity, string code)
    {
        entity.FabricCode = code;
        entity.Name = request.Name.Trim();
        entity.FabricType = request.FabricType;
        entity.Structure = NullIfBlank(request.Structure);
        entity.Composition = NullIfBlank(request.Composition);
        entity.RawWidthCm = request.RawWidthCm;
        entity.RawWeightGsm = request.RawWeightGsm;
        entity.FinishedWidthCm = request.FinishedWidthCm;
        entity.FinishedWeightGsm = request.FinishedWeightGsm;
        entity.ShrinkageTolerancePercent = request.ShrinkageTolerancePercent;
        entity.DefaultUnit = request.DefaultUnit;
        entity.IsActive = request.IsActive;
    }
}

public sealed record BoyahaneYarnDto(
    int Id,
    string YarnCode,
    string Name,
    decimal CountValue,
    YarnCountSystem CountSystem,
    int Plies,
    string? Composition,
    string? Twist,
    bool IsActive);

public sealed record BoyahaneYarnRequest(
    string YarnCode,
    string Name,
    decimal CountValue,
    YarnCountSystem CountSystem = YarnCountSystem.Ne,
    int Plies = 1,
    string? Composition = null,
    string? Twist = null,
    bool IsActive = true);

public sealed class BoyahaneYarnRequestValidator : AbstractValidator<BoyahaneYarnRequest>
{
    public BoyahaneYarnRequestValidator()
    {
        RuleFor(x => x.YarnCode).ValidCode();
        RuleFor(x => x.Name).ValidName(200);
        RuleFor(x => x.CountValue)
            .GreaterThan(0).WithMessage("İplik numarası sıfırdan büyük olmalıdır.")
            .PrecisionScale(8, 2, true).WithMessage("İplik numarası en fazla iki ondalık olabilir.");
        RuleFor(x => x.CountSystem).IsInEnum().WithMessage("Geçerli bir numara sistemi seçin.");
        RuleFor(x => x.Plies).InclusiveBetween(1, 20).WithMessage("Kat sayısı 1 ile 20 arasında olmalıdır.");
        RuleFor(x => x.Composition).OptionalText(200);
        RuleFor(x => x.Twist).OptionalText(50);
    }
}

public sealed class BoyahaneYarnService(IAppDbContext db)
    : MasterDataService<BoyahaneYarn, BoyahaneYarnDto, BoyahaneYarnRequest>(db)
{
    protected override DbSet<BoyahaneYarn> Set => Db.Set<BoyahaneYarn>();
    protected override string Label => "İplik kartı";
    protected override string CodeField => "yarnCode";

    protected override string CodeOf(BoyahaneYarnRequest request) => request.YarnCode;
    protected override Expression<Func<BoyahaneYarn, bool>> WithCode(string code) => e => e.YarnCode == code;

    protected override Expression<Func<BoyahaneYarn, bool>> Matches(string term) =>
        e => e.YarnCode.Contains(term) || e.Name.Contains(term);

    protected override IQueryable<BoyahaneYarn> Order(IQueryable<BoyahaneYarn> query) => query.OrderBy(e => e.YarnCode);

    protected override Expression<Func<BoyahaneYarn, BoyahaneYarnDto>> ToDto => e => new BoyahaneYarnDto(
        e.Id, e.YarnCode, e.Name, e.CountValue, e.CountSystem, e.Plies, e.Composition, e.Twist, e.IsActive);

    protected override Expression<Func<BoyahaneYarn, LookupDto>> ToLookup =>
        e => new LookupDto(e.Id, e.YarnCode, e.Name);

    protected override async Task<string?> FindUsageAsync(int id, CancellationToken cancellationToken)
    {
        if (await Db.Set<BoyahaneOrderLine>().AnyAsync(l => l.YarnId == id, cancellationToken))
        {
            return "siparişler";
        }

        return await Db.Set<BoyahaneYarnLot>().AnyAsync(l => l.YarnId == id, cancellationToken) ? "iplik lotları" : null;
    }

    protected override void Apply(BoyahaneYarnRequest request, BoyahaneYarn entity, string code)
    {
        entity.YarnCode = code;
        entity.Name = request.Name.Trim();
        entity.CountValue = request.CountValue;
        entity.CountSystem = request.CountSystem;
        entity.Plies = request.Plies;
        entity.Composition = NullIfBlank(request.Composition);
        entity.Twist = NullIfBlank(request.Twist);
        entity.IsActive = request.IsActive;
    }
}
