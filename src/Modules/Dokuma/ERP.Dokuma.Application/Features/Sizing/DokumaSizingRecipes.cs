using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Dokuma.Domain.Entities;
using ERP.Dokuma.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Dokuma.Application.Features.Sizing;

public sealed class DokumaSizingRecipeQuery : PageRequest
{
    /// <summary>Reçete kodu, adı ya da artikelde aranır.</summary>
    public string? Search { get; set; }

    public int? ArticleId { get; set; }
    public bool? IsActive { get; set; }
}

public sealed record DokumaSizingRecipeListItemDto(
    int Id,
    string Code,
    string Name,
    int ArticleId,
    string ArticleCode,
    string ArticleName,
    bool IsDefault,
    decimal? ConcentrationPercent,
    decimal? TargetPickUpPercent,
    int LineCount,
    bool IsActive);

public sealed record DokumaSizingRecipeLineDto(int Id, int Sequence, int MaterialId, string MaterialCode, string MaterialName, MaterialUnit Unit, decimal BathPercent);

public sealed record DokumaSizingRecipeDto(
    int Id,
    string Code,
    string Name,
    int ArticleId,
    string ArticleCode,
    string ArticleName,
    bool IsDefault,
    decimal? ConcentrationPercent,
    decimal? BathTemperatureC,
    decimal? ViscositySeconds,
    decimal? MachineSpeedMetersPerMinute,
    decimal? TargetPickUpPercent,
    string? Notes,
    bool IsActive,
    IReadOnlyList<DokumaSizingRecipeLineDto> Lines);

/// <param name="BathPercent">Kimyasalın banyodaki oranı (%).</param>
public sealed record DokumaSizingRecipeLineRequest(int MaterialId, decimal BathPercent);

/// <param name="IsDefault">Artikelin varsayılan reçetesi; işaretlenince artikelin diğer reçeteleri varsayılan olmaktan çıkar.</param>
public sealed record DokumaSizingRecipeRequest(
    string Code,
    string Name,
    int ArticleId,
    IReadOnlyList<DokumaSizingRecipeLineRequest> Lines,
    bool IsDefault = false,
    decimal? ConcentrationPercent = null,
    decimal? BathTemperatureC = null,
    decimal? ViscositySeconds = null,
    decimal? MachineSpeedMetersPerMinute = null,
    decimal? TargetPickUpPercent = null,
    string? Notes = null,
    bool IsActive = true);

public sealed class DokumaSizingRecipeRequestValidator : AbstractValidator<DokumaSizingRecipeRequest>
{
    public const int MaxLines = 30;

    public DokumaSizingRecipeRequestValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName(200);
        RuleFor(x => x.ArticleId).GreaterThan(0).WithMessage("Artikel seçilmelidir.");
        RuleFor(x => x.ConcentrationPercent)
            .InclusiveBetween(0, 100).WithMessage("Konsantrasyon 0 ile 100 arasında olmalıdır.")
            .PrecisionScale(5, 2, true).WithMessage("Konsantrasyon en fazla iki ondalık olabilir.")
            .When(x => x.ConcentrationPercent is not null);
        RuleFor(x => x.BathTemperatureC)
            .InclusiveBetween(0, 200).WithMessage("Banyo sıcaklığı 0 ile 200 °C arasında olmalıdır.")
            .PrecisionScale(5, 1, true).WithMessage("Sıcaklık en fazla bir ondalık olabilir.")
            .When(x => x.BathTemperatureC is not null);
        RuleFor(x => x.ViscositySeconds)
            .GreaterThan(0).WithMessage("Viskozite sıfırdan büyük olmalıdır.")
            .LessThanOrEqualTo(10_000).WithMessage("Viskozite çok büyük.")
            .PrecisionScale(6, 1, true).WithMessage("Viskozite en fazla bir ondalık olabilir.")
            .When(x => x.ViscositySeconds is not null);
        RuleFor(x => x.MachineSpeedMetersPerMinute)
            .GreaterThan(0).WithMessage("Makine hızı sıfırdan büyük olmalıdır.")
            .LessThanOrEqualTo(10_000).WithMessage("Makine hızı çok büyük.")
            .PrecisionScale(6, 1, true).WithMessage("Makine hızı en fazla bir ondalık olabilir.")
            .When(x => x.MachineSpeedMetersPerMinute is not null);
        RuleFor(x => x.TargetPickUpPercent)
            .InclusiveBetween(0, 500).WithMessage("Hedef alım 0 ile 500 arasında olmalıdır.")
            .PrecisionScale(5, 2, true).WithMessage("Hedef alım en fazla iki ondalık olabilir.")
            .When(x => x.TargetPickUpPercent is not null);
        RuleFor(x => x.Notes).OptionalText(500);
        RuleFor(x => x.Lines)
            .NotNull().WithMessage("Kimyasal listesi zorunludur.")
            .Must(lines => lines.Count <= MaxLines).WithMessage($"En fazla {MaxLines} kimyasal girilebilir.")
            .Custom((lines, context) =>
            {
                if (lines is null)
                {
                    return;
                }

                for (var i = 0; i < lines.Count; i++)
                {
                    var line = lines[i];
                    if (line.MaterialId <= 0)
                    {
                        context.AddFailure($"{i + 1}. satır: kimyasal seçin.");
                    }

                    if (line.BathPercent is <= 0 or > 100 || decimal.Round(line.BathPercent, 3) != line.BathPercent)
                    {
                        context.AddFailure($"{i + 1}. satır: banyo oranı 0 ile 100 arasında, en fazla üç ondalık olmalıdır.");
                    }
                }

                if (lines.Select(l => l.MaterialId).Distinct().Count() != lines.Count)
                {
                    context.AddFailure("Aynı kimyasal iki satırda olamaz.");
                }

                if (lines.Sum(l => l.BathPercent) > 100)
                {
                    context.AddFailure("Kimyasalların banyo oranları toplamı %100'ü geçemez.");
                }
            });
    }
}

public interface IDokumaSizingRecipeService
{
    Task<PagedResult<DokumaSizingRecipeListItemDto>> GetListAsync(DokumaSizingRecipeQuery query, CancellationToken cancellationToken);
    Task<Result<DokumaSizingRecipeDto>> GetAsync(int id, CancellationToken cancellationToken);
    Task<Result<DokumaSizingRecipeDto>> CreateAsync(DokumaSizingRecipeRequest request, CancellationToken cancellationToken);
    Task<Result<DokumaSizingRecipeDto>> UpdateAsync(int id, DokumaSizingRecipeRequest request, CancellationToken cancellationToken);
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken);
}

/// <summary>
/// Haşıl reçeteleri: artikel başına bir ya da birkaç reçete, biri varsayılan. Kimyasal satırları bütün olarak kaydedilir;
/// gönderilmeyen satırlar silinir. Artikelin ilk etkin reçetesi kendiliğinden varsayılan olur.
/// </summary>
public sealed class DokumaSizingRecipeService(IAppDbContext db) : IDokumaSizingRecipeService
{
    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Haşıl reçetesi bulunamadı.", ErrorType.NotFound);

    public Task<PagedResult<DokumaSizingRecipeListItemDto>> GetListAsync(DokumaSizingRecipeQuery query, CancellationToken cancellationToken)
    {
        var recipes = db.Set<DokumaSizingRecipe>().AsNoTracking();
        if (NullIfBlank(query.Search) is { } term)
        {
            recipes = recipes.Where(r => r.Code.Contains(term) || r.Name.Contains(term) || r.Article.ArticleCode.Contains(term) || r.Article.Name.Contains(term));
        }

        if (query.ArticleId is { } articleId)
        {
            recipes = recipes.Where(r => r.ArticleId == articleId);
        }

        if (query.IsActive is { } isActive)
        {
            recipes = recipes.Where(r => r.IsActive == isActive);
        }

        return recipes
            .OrderBy(r => r.Article.ArticleCode).ThenByDescending(r => r.IsDefault).ThenBy(r => r.Code)
            .Select(r => new DokumaSizingRecipeListItemDto(
                r.Id, r.Code, r.Name, r.ArticleId, r.Article.ArticleCode, r.Article.Name, r.IsDefault, r.ConcentrationPercent, r.TargetPickUpPercent, r.Lines.Count, r.IsActive))
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<Result<DokumaSizingRecipeDto>> GetAsync(int id, CancellationToken cancellationToken)
    {
        var recipe = await db.Set<DokumaSizingRecipe>().AsNoTracking()
            .Include(r => r.Article)
            .Include(r => r.Lines).ThenInclude(l => l.Material)
            .SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        return recipe is null ? NotFound : ToDto(recipe);
    }

    public Task<Result<DokumaSizingRecipeDto>> CreateAsync(DokumaSizingRecipeRequest request, CancellationToken cancellationToken) =>
        SaveAsync(null, request, cancellationToken);

    public Task<Result<DokumaSizingRecipeDto>> UpdateAsync(int id, DokumaSizingRecipeRequest request, CancellationToken cancellationToken) =>
        SaveAsync(id, request, cancellationToken);

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var recipe = await db.Set<DokumaSizingRecipe>().SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (recipe is null)
        {
            return NotFound;
        }

        if (await db.Set<DokumaBeam>().AnyAsync(b => b.SizingRecipeId == id, cancellationToken))
        {
            return new Error(ErrorCodes.RecordInUse, "Haşıl reçetesi leventlerde kullanıldığı için silinemez. Bunun yerine pasif yapabilirsiniz.", ErrorType.Conflict);
        }

        // Silinen reçete varsayılan kalmaz (soft delete).
        recipe.IsDefault = false;
        await db.SaveChangesAsync(cancellationToken);
        db.Set<DokumaSizingRecipe>().Remove(recipe);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<Result<DokumaSizingRecipeDto>> SaveAsync(int? id, DokumaSizingRecipeRequest request, CancellationToken cancellationToken)
    {
        DokumaSizingRecipe? recipe = null;
        if (id is not null)
        {
            recipe = await db.Set<DokumaSizingRecipe>().Include(r => r.Lines).SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
            if (recipe is null)
            {
                return NotFound;
            }
        }

        var code = NormalizeCode(request.Code);
        if (await db.Set<DokumaSizingRecipe>().AnyAsync(r => r.Code == code && r.Id != (id ?? 0), cancellationToken))
        {
            var message = $"'{code}' kodu zaten kullanılıyor.";
            return new Error(ErrorCodes.CodeAlreadyExists, message, ErrorType.Conflict, new Dictionary<string, string[]> { ["code"] = [message] });
        }

        if (await ReferenceValidation.RequireSelectableAsync(db.Set<DokumaArticle>(), request.ArticleId, recipe?.ArticleId, "articleId", "Artikel", cancellationToken) is { } articleError)
        {
            return articleError;
        }

        if (recipe is not null && recipe.ArticleId != request.ArticleId && await db.Set<DokumaBeam>().AnyAsync(b => b.SizingRecipeId == recipe.Id, cancellationToken))
        {
            return ReferenceValidation.InvalidReference("articleId", "Leventlerde kullanılmış reçetenin artikeli değiştirilemez.");
        }

        if (await ReferenceValidation.RequireSelectableAsync(
                db.Set<DokumaMaterial>(),
                request.Lines.Select(l => l.MaterialId).Distinct().ToList(),
                recipe?.Lines.Select(l => l.MaterialId).ToHashSet() ?? [],
                "lines",
                "Kimyasal",
                cancellationToken) is { } materialError)
        {
            return materialError;
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // Varsayılan tektir: işaretlenirse artikelin diğer reçeteleri önce varsayılandan çıkar. Artikelin başka etkin
        // varsayılan reçetesi yoksa etkin reçete kendiliğinden varsayılan olur.
        var others = await db.Set<DokumaSizingRecipe>().Where(r => r.ArticleId == request.ArticleId && r.Id != (id ?? 0)).ToListAsync(cancellationToken);
        var isDefault = request.IsActive && (request.IsDefault || !others.Any(r => r.IsDefault && r.IsActive));
        if (isDefault && others.Any(r => r.IsDefault))
        {
            foreach (var other in others.Where(r => r.IsDefault))
            {
                other.IsDefault = false;
            }

            await db.SaveChangesAsync(cancellationToken);
        }
        else if (!isDefault && recipe is { IsDefault: true } && others.Where(r => r.IsActive).OrderBy(r => r.Code).FirstOrDefault() is { } next)
        {
            // Varsayılan reçete pasife alınınca ya da varsayılandan çıkarılınca artikel varsayılansız kalmaz: sıradaki etkin reçete devralır.
            recipe.IsDefault = false;
            await db.SaveChangesAsync(cancellationToken);
            next.IsDefault = true;
            await db.SaveChangesAsync(cancellationToken);
        }

        if (recipe is null)
        {
            recipe = new DokumaSizingRecipe();
            db.Set<DokumaSizingRecipe>().Add(recipe);
        }
        else
        {
            // Sıra dizini benzersiz olduğu için eski satırlar önce silinir.
            db.Set<DokumaSizingRecipeLine>().RemoveRange(recipe.Lines);
            await db.SaveChangesAsync(cancellationToken);
        }

        recipe.Code = code;
        recipe.Name = request.Name.Trim();
        recipe.ArticleId = request.ArticleId;
        recipe.IsDefault = isDefault;
        recipe.ConcentrationPercent = request.ConcentrationPercent;
        recipe.BathTemperatureC = request.BathTemperatureC;
        recipe.ViscositySeconds = request.ViscositySeconds;
        recipe.MachineSpeedMetersPerMinute = request.MachineSpeedMetersPerMinute;
        recipe.TargetPickUpPercent = request.TargetPickUpPercent;
        recipe.Notes = NullIfBlank(request.Notes);
        recipe.IsActive = request.IsActive;

        var sequence = 0;
        foreach (var line in request.Lines)
        {
            recipe.Lines.Add(new DokumaSizingRecipeLine { Sequence = ++sequence, MaterialId = line.MaterialId, BathPercent = line.BathPercent });
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(recipe.Id, cancellationToken);
    }

    private static DokumaSizingRecipeDto ToDto(DokumaSizingRecipe recipe) => new(
        recipe.Id,
        recipe.Code,
        recipe.Name,
        recipe.ArticleId,
        recipe.Article.ArticleCode,
        recipe.Article.Name,
        recipe.IsDefault,
        recipe.ConcentrationPercent,
        recipe.BathTemperatureC,
        recipe.ViscositySeconds,
        recipe.MachineSpeedMetersPerMinute,
        recipe.TargetPickUpPercent,
        recipe.Notes,
        recipe.IsActive,
        recipe.Lines.OrderBy(l => l.Sequence)
            .Select(l => new DokumaSizingRecipeLineDto(l.Id, l.Sequence, l.MaterialId, l.Material.Code, l.Material.Name, l.Material.Unit, l.BathPercent))
            .ToList());
}
