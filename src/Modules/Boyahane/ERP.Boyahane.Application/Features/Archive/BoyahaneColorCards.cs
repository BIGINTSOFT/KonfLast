using System.Linq.Expressions;
using ERP.Boyahane.Application.Features.Lab;
using ERP.Boyahane.Domain.Entities;
using ERP.Boyahane.Domain.Enums;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Boyahane.Application.Features.Archive;

/// <param name="RecipeCount">Bu renk kartına bağlı onaylı lab reçetesi sayısı.</param>
public sealed record BoyahaneColorCardDto(
    int Id,
    string Code,
    string Name,
    int? CustomerId,
    string? CustomerName,
    string? CustomerColorCode,
    string? Pantone,
    decimal? LabL,
    decimal? LabA,
    decimal? LabB,
    string? Notes,
    bool HasImage,
    int RecipeCount,
    bool IsActive);

public sealed record BoyahaneColorCardRequest(
    string Code,
    string Name,
    int? CustomerId = null,
    string? CustomerColorCode = null,
    string? Pantone = null,
    decimal? LabL = null,
    decimal? LabA = null,
    decimal? LabB = null,
    string? Notes = null,
    bool IsActive = true);

public sealed class BoyahaneColorCardRequestValidator : AbstractValidator<BoyahaneColorCardRequest>
{
    public BoyahaneColorCardRequestValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName(100);
        RuleFor(x => x.CustomerColorCode).OptionalText(50);
        RuleFor(x => x.Pantone).OptionalText(50);
        RuleFor(x => x.Notes).OptionalText(500);
        RuleFor(x => x.LabL).InclusiveBetween(0, 100).WithMessage("L değeri 0 ile 100 arasında olmalıdır.").When(x => x.LabL is not null);
        RuleFor(x => x.LabA).InclusiveBetween(-128, 128).WithMessage("a değeri -128 ile 128 arasında olmalıdır.").When(x => x.LabA is not null);
        RuleFor(x => x.LabB).InclusiveBetween(-128, 128).WithMessage("b değeri -128 ile 128 arasında olmalıdır.").When(x => x.LabB is not null);
        RuleFor(x => x)
            .Must(x => (x.LabL is null && x.LabA is null && x.LabB is null) || (x.LabL is not null && x.LabA is not null && x.LabB is not null))
            .WithName("labL").WithMessage("Lab değerleri birlikte girilir: L, a ve b.");
    }
}

/// <summary>Renk kartları (renk arşivi). Müşteriye özel kart yalnızca o müşterinin siparişinde seçilir; seçim listesi buna göre süzülür.</summary>
public sealed class BoyahaneColorCardService(IAppDbContext db)
    : MasterDataService<BoyahaneColorCard, BoyahaneColorCardDto, BoyahaneColorCardRequest>(db)
{
    protected override DbSet<BoyahaneColorCard> Set => Db.Set<BoyahaneColorCard>();
    protected override string Label => "Renk kartı";
    protected override string CodeField => "code";

    protected override string CodeOf(BoyahaneColorCardRequest request) => request.Code;
    protected override Expression<Func<BoyahaneColorCard, bool>> WithCode(string code) => e => e.Code == code;

    protected override Expression<Func<BoyahaneColorCard, bool>> Matches(string term) => e =>
        e.Code.Contains(term) || e.Name.Contains(term)
        || (e.CustomerColorCode != null && e.CustomerColorCode.Contains(term))
        || (e.Pantone != null && e.Pantone.Contains(term))
        || (e.Customer != null && e.Customer.Name.Contains(term));

    protected override IQueryable<BoyahaneColorCard> Order(IQueryable<BoyahaneColorCard> query) => query.OrderBy(e => e.Code);

    protected override Expression<Func<BoyahaneColorCard, BoyahaneColorCardDto>> ToDto => e => new BoyahaneColorCardDto(
        e.Id,
        e.Code,
        e.Name,
        e.CustomerId,
        e.Customer != null ? e.Customer.Name : null,
        e.CustomerColorCode,
        e.Pantone,
        e.LabL,
        e.LabA,
        e.LabB,
        e.Notes,
        Db.Set<BoyahaneColorCardImage>().Any(i => i.ColorCardId == e.Id),
        Db.Set<BoyahaneLabTrial>().Count(t => t.Decision == LabTrialDecision.Accepted && t.Request.Status == LabRequestStatus.Approved && t.Request.OrderLine.ColorCardId == e.Id),
        e.IsActive);

    protected override Expression<Func<BoyahaneColorCard, LookupDto>> ToLookup =>
        e => new LookupDto(e.Id, e.Code, e.Name) { Detail = e.CustomerColorCode ?? e.Pantone };

    /// <summary>Müşteri verilirse genel kartlar ve o müşterinin kartları listelenir.</summary>
    protected override IQueryable<BoyahaneColorCard> FilterLookup(IQueryable<BoyahaneColorCard> items, LookupQuery query) =>
        query.CustomerId is { } customerId ? items.Where(e => e.CustomerId == null || e.CustomerId == customerId) : items;

    protected override void Apply(BoyahaneColorCardRequest request, BoyahaneColorCard entity, string code)
    {
        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.CustomerId = request.CustomerId;
        entity.CustomerColorCode = NullIfBlank(request.CustomerColorCode);
        entity.Pantone = NullIfBlank(request.Pantone);
        entity.LabL = request.LabL;
        entity.LabA = request.LabA;
        entity.LabB = request.LabB;
        entity.Notes = NullIfBlank(request.Notes);
        entity.IsActive = request.IsActive;
    }

    protected override Task<Error?> ValidateAsync(BoyahaneColorCardRequest request, BoyahaneColorCard? existing, CancellationToken cancellationToken) =>
        RequireSelectableAsync(Db.Set<BoyahaneCustomer>(), request.CustomerId, existing?.CustomerId, "customerId", "Müşteri", cancellationToken);

    protected override async Task<string?> FindUsageAsync(int id, CancellationToken cancellationToken) =>
        await Db.Set<BoyahaneOrderLine>().AnyAsync(l => l.ColorCardId == id, cancellationToken) ? "sipariş satırları"
        : await Db.Set<BoyahaneColorwayColor>().AnyAsync(c => c.ColorCardId == id, cancellationToken) ? "desen renk yolları"
        : null;
}

/// <summary>Renk kartına bağlı onaylı lab reçetesi: hangi müşteri, hangi kumaş / iplik, hangi talep ve ne zaman.</summary>
public sealed record BoyahaneColorCardRecipeDto(
    int TrialId,
    int RequestId,
    string RequestNo,
    string TrialCode,
    int OrderId,
    string OrderNo,
    string? CustomerName,
    string? ItemCode,
    string? ItemName,
    DateOnly? DecisionOn,
    IReadOnlyList<BoyahaneRecipeItemDto> Items);

public interface IBoyahaneColorCardDetailService
{
    Task<Result<BoyahaneArchiveImageDto>> GetImageAsync(int id, CancellationToken cancellationToken);

    /// <summary>Kartın görüntüsünü yükler; varsa eskisinin yerine geçer.</summary>
    Task<Result> SetImageAsync(int id, BoyahaneArchiveImageRequest request, CancellationToken cancellationToken);

    Task<Result> RemoveImageAsync(int id, CancellationToken cancellationToken);

    /// <summary>Kartın onaylı lab reçeteleri, en yeniden eskiye.</summary>
    Task<Result<IReadOnlyList<BoyahaneColorCardRecipeDto>>> GetRecipesAsync(int id, CancellationToken cancellationToken);
}

public sealed class BoyahaneColorCardDetailService(IAppDbContext db, TimeProvider timeProvider) : IBoyahaneColorCardDetailService
{
    private const int MaxRecipes = 200;

    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Renk kartı bulunamadı.", ErrorType.NotFound);
    private static readonly Error ImageNotFound = new(ErrorCodes.NotFound, "Renk kartının görüntüsü yok.", ErrorType.NotFound);

    public async Task<Result<BoyahaneArchiveImageDto>> GetImageAsync(int id, CancellationToken cancellationToken)
    {
        if (!await ExistsAsync(id, cancellationToken))
        {
            return NotFound;
        }

        var image = await db.Set<BoyahaneColorCardImage>().AsNoTracking().SingleOrDefaultAsync(i => i.ColorCardId == id, cancellationToken);
        return image is null ? ImageNotFound : new BoyahaneArchiveImageDto(image.ContentType, Convert.ToBase64String(image.Content), image.UpdatedAt);
    }

    public async Task<Result> SetImageAsync(int id, BoyahaneArchiveImageRequest request, CancellationToken cancellationToken)
    {
        if (!await ExistsAsync(id, cancellationToken))
        {
            return NotFound;
        }

        var content = BoyahaneArchiveImages.Decode(request);
        if (content is null)
        {
            return ReferenceValidation.InvalidReference("dataBase64", "Dosya geçerli bir JPEG ya da PNG görüntüsü değil ya da 2 MB'ı aşıyor.");
        }

        var image = await db.Set<BoyahaneColorCardImage>().SingleOrDefaultAsync(i => i.ColorCardId == id, cancellationToken);
        if (image is null)
        {
            image = new BoyahaneColorCardImage { ColorCardId = id };
            db.Set<BoyahaneColorCardImage>().Add(image);
        }

        image.Content = content;
        image.ContentType = request.ContentType;
        image.UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> RemoveImageAsync(int id, CancellationToken cancellationToken)
    {
        if (!await ExistsAsync(id, cancellationToken))
        {
            return NotFound;
        }

        await db.Set<BoyahaneColorCardImage>().Where(i => i.ColorCardId == id).ExecuteDeleteAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<BoyahaneColorCardRecipeDto>>> GetRecipesAsync(int id, CancellationToken cancellationToken)
    {
        if (!await ExistsAsync(id, cancellationToken))
        {
            return NotFound;
        }

        return await db.Set<BoyahaneLabTrial>().AsNoTracking()
            .Where(t => t.Decision == LabTrialDecision.Accepted && t.Request.Status == LabRequestStatus.Approved && t.Request.OrderLine.ColorCardId == id)
            .OrderByDescending(t => t.DecisionOn).ThenByDescending(t => t.Id)
            .Take(MaxRecipes)
            .Select(t => new BoyahaneColorCardRecipeDto(
                t.Id,
                t.RequestId,
                t.Request.RequestNo,
                t.TrialCode,
                t.Request.OrderLine.OrderId,
                t.Request.OrderLine.Order.OrderNo,
                t.Request.OrderLine.Order.Customer != null ? t.Request.OrderLine.Order.Customer.Name : null,
                t.Request.OrderLine.Fabric != null ? t.Request.OrderLine.Fabric.FabricCode : t.Request.OrderLine.Yarn != null ? t.Request.OrderLine.Yarn.YarnCode : null,
                t.Request.OrderLine.Fabric != null ? t.Request.OrderLine.Fabric.Name : t.Request.OrderLine.Yarn != null ? t.Request.OrderLine.Yarn.Name : null,
                t.DecisionOn,
                t.Items.OrderBy(i => i.DyeChemical.Kind).ThenBy(i => i.Id)
                    .Select(i => new BoyahaneRecipeItemDto(i.DyeChemical.Code, i.DyeChemical.Name, i.DyeChemical.Kind, i.Amount))
                    .ToList()))
            .ToListAsync(cancellationToken);
    }

    private Task<bool> ExistsAsync(int id, CancellationToken cancellationToken) =>
        db.Set<BoyahaneColorCard>().AnyAsync(c => c.Id == id, cancellationToken);
}
