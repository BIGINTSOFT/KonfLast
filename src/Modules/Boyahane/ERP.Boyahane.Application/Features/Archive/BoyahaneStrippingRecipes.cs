using ERP.Boyahane.Domain.Entities;
using ERP.Boyahane.Domain.Enums;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Boyahane.Application.Features.Archive;

public sealed record BoyahaneStrippingRecipeLineDto(
    int Id,
    int Sequence,
    int DyeChemicalId,
    string Code,
    string Name,
    decimal Amount,
    RecipeAmountUnit Unit,
    int? TemperatureCelsius,
    int? DurationMinutes);

/// <param name="UsageCount">Bu reçeteyle eklenmiş söküm adımı sayısı.</param>
public sealed record BoyahaneStrippingRecipeDto(
    int Id,
    string Code,
    string Name,
    string? Notes,
    bool IsActive,
    int UsageCount,
    IReadOnlyList<BoyahaneStrippingRecipeLineDto> Lines);

public sealed record BoyahaneStrippingRecipeLineRequest(
    int DyeChemicalId,
    decimal Amount,
    RecipeAmountUnit Unit = RecipeAmountUnit.GramsPerLiter,
    int? TemperatureCelsius = null,
    int? DurationMinutes = null);

/// <param name="Lines">Satırlar, uygulama sırasıyla.</param>
public sealed record BoyahaneStrippingRecipeRequest(
    string Code,
    string Name,
    IReadOnlyList<BoyahaneStrippingRecipeLineRequest> Lines,
    string? Notes = null,
    bool IsActive = true);

public sealed class BoyahaneStrippingRecipeRequestValidator : AbstractValidator<BoyahaneStrippingRecipeRequest>
{
    public const int MaxLines = 30;

    public BoyahaneStrippingRecipeRequestValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName(100);
        RuleFor(x => x.Notes).OptionalText(500);
        RuleFor(x => x.Lines)
            .NotEmpty().WithMessage("En az bir kimyasal girilmelidir.")
            .Must(lines => lines.Count <= MaxLines).WithMessage($"En fazla {MaxLines} satır girilebilir.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.DyeChemicalId).GreaterThan(0).WithMessage("Kimyasal seçilmelidir.");
            line.RuleFor(l => l.Amount).InclusiveBetween(0.0001m, 100_000).WithMessage("Miktar sıfırdan büyük olmalıdır.");
            line.RuleFor(l => l.Unit).IsInEnum().WithMessage("Geçerli bir ölçü seçin.");
            line.RuleFor(l => l.TemperatureCelsius).InclusiveBetween(0, 150).WithMessage("Sıcaklık 0 ile 150 °C arasında olmalıdır.").When(l => l.TemperatureCelsius is not null);
            line.RuleFor(l => l.DurationMinutes).InclusiveBetween(1, 1440).WithMessage("Süre 1 ile 1440 dakika arasında olmalıdır.").When(l => l.DurationMinutes is not null);
        });
    }
}

public interface IBoyahaneStrippingRecipeService
{
    Task<PagedResult<BoyahaneStrippingRecipeDto>> GetListAsync(MasterDataQuery query, CancellationToken cancellationToken);

    /// <summary>Seçim kutuları için etkin reçeteler; renk kontrolü formu da kullanır.</summary>
    Task<IReadOnlyList<LookupDto>> LookupAsync(LookupQuery query, CancellationToken cancellationToken);

    Task<Result<BoyahaneStrippingRecipeDto>> GetAsync(int id, CancellationToken cancellationToken);
    Task<Result<BoyahaneStrippingRecipeDto>> CreateAsync(BoyahaneStrippingRecipeRequest request, CancellationToken cancellationToken);
    Task<Result<BoyahaneStrippingRecipeDto>> UpdateAsync(int id, BoyahaneStrippingRecipeRequest request, CancellationToken cancellationToken);
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken);
}

/// <summary>
/// Söküm reçeteleri. Renk kontrolünde "söküm + yeniden boya" seçilince reçete söküm adımına bağlanır; adım bitince satırlar
/// parti kilosu ve flottesine göre hesaplanıp lotlardan düşülür. Reçete değişikliği yalnızca sonraki adımları etkiler.
/// </summary>
public sealed class BoyahaneStrippingRecipeService(IAppDbContext db) : IBoyahaneStrippingRecipeService
{
    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Söküm reçetesi bulunamadı.", ErrorType.NotFound);

    public async Task<PagedResult<BoyahaneStrippingRecipeDto>> GetListAsync(MasterDataQuery query, CancellationToken cancellationToken)
    {
        var recipes = db.Set<BoyahaneStrippingRecipe>().AsNoTracking();
        if (NullIfBlank(query.Search) is { } term)
        {
            recipes = recipes.Where(r => r.Code.Contains(term) || r.Name.Contains(term));
        }

        if (query.IsActive is { } isActive)
        {
            recipes = recipes.Where(r => r.IsActive == isActive);
        }

        // Satırlar ayrı yüklenir: önce sayfadaki reçetelerin kimlikleri, sonra reçeteler satırlarıyla.
        var page = query.NormalizedPage;
        var pageSize = query.NormalizedPageSize;
        var total = await recipes.CountAsync(cancellationToken);
        var ids = await recipes.OrderBy(r => r.Code).Skip((page - 1) * pageSize).Take(pageSize).Select(r => r.Id).ToListAsync(cancellationToken);
        var loaded = (await LoadAsync(ids, cancellationToken)).ToDictionary(r => r.Id);
        return new PagedResult<BoyahaneStrippingRecipeDto>(ids.Select(id => loaded[id]).ToList(), page, pageSize, total);
    }

    public async Task<IReadOnlyList<LookupDto>> LookupAsync(LookupQuery query, CancellationToken cancellationToken)
    {
        var recipes = db.Set<BoyahaneStrippingRecipe>().AsNoTracking().Where(r => r.IsActive || r.Id == query.IncludeId);
        if (NullIfBlank(query.Search) is { } term)
        {
            recipes = recipes.Where(r => r.Code.Contains(term) || r.Name.Contains(term) || r.Id == query.IncludeId);
        }

        return await recipes.OrderBy(r => r.Code).Take(query.NormalizedTake).Select(r => new LookupDto(r.Id, r.Code, r.Name)).ToListAsync(cancellationToken);
    }

    public async Task<Result<BoyahaneStrippingRecipeDto>> GetAsync(int id, CancellationToken cancellationToken)
    {
        var recipes = await LoadAsync([id], cancellationToken);
        return recipes.Count == 0 ? NotFound : recipes[0];
    }

    public async Task<Result<BoyahaneStrippingRecipeDto>> CreateAsync(BoyahaneStrippingRecipeRequest request, CancellationToken cancellationToken)
    {
        var code = NormalizeCode(request.Code);
        if (await db.Set<BoyahaneStrippingRecipe>().AnyAsync(r => r.Code == code, cancellationToken))
        {
            return CodeExists(code);
        }

        if (await ValidateChemicalsAsync(request, [], cancellationToken) is { } error)
        {
            return error;
        }

        var recipe = new BoyahaneStrippingRecipe();
        Apply(recipe, code, request);
        db.Set<BoyahaneStrippingRecipe>().Add(recipe);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(recipe.Id, cancellationToken);
    }

    public async Task<Result<BoyahaneStrippingRecipeDto>> UpdateAsync(int id, BoyahaneStrippingRecipeRequest request, CancellationToken cancellationToken)
    {
        var recipe = await db.Set<BoyahaneStrippingRecipe>().Include(r => r.Lines).SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (recipe is null)
        {
            return NotFound;
        }

        var code = NormalizeCode(request.Code);
        if (await db.Set<BoyahaneStrippingRecipe>().AnyAsync(r => r.Code == code && r.Id != id, cancellationToken))
        {
            return CodeExists(code);
        }

        if (await ValidateChemicalsAsync(request, recipe.Lines.Select(l => l.DyeChemicalId).ToHashSet(), cancellationToken) is { } error)
        {
            return error;
        }

        // Sıra numarası benzersiz olduğu için eski satırlar yenilerden önce silinir; iki adım tek işlemdir.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.Set<BoyahaneStrippingRecipeLine>().RemoveRange(recipe.Lines);
        await db.SaveChangesAsync(cancellationToken);
        recipe.Lines.Clear();
        Apply(recipe, code, request);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var recipe = await db.Set<BoyahaneStrippingRecipe>().SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (recipe is null)
        {
            return NotFound;
        }

        if (await db.Set<BoyahaneBatchStep>().AnyAsync(s => s.StrippingRecipeId == id, cancellationToken))
        {
            return new Error(ErrorCodes.RecordInUse, "Söküm reçetesi kullanımda olduğu için silinemez (parti adımları). Bunun yerine pasif yapabilirsiniz.", ErrorType.Conflict);
        }

        // Fiziksel silme değildir: kayıt IsDeleted olarak işaretlenir, satırları geçmişte kalır.
        db.Set<BoyahaneStrippingRecipe>().Remove(recipe);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static void Apply(BoyahaneStrippingRecipe recipe, string code, BoyahaneStrippingRecipeRequest request)
    {
        recipe.Code = code;
        recipe.Name = request.Name.Trim();
        recipe.Notes = NullIfBlank(request.Notes);
        recipe.IsActive = request.IsActive;
        var sequence = 0;
        foreach (var line in request.Lines)
        {
            recipe.Lines.Add(new BoyahaneStrippingRecipeLine
            {
                Sequence = ++sequence,
                DyeChemicalId = line.DyeChemicalId,
                Amount = line.Amount,
                Unit = line.Unit,
                TemperatureCelsius = line.TemperatureCelsius,
                DurationMinutes = line.DurationMinutes,
            });
        }
    }

    private async Task<Error?> ValidateChemicalsAsync(BoyahaneStrippingRecipeRequest request, HashSet<int> current, CancellationToken cancellationToken)
    {
        var ids = request.Lines.Select(l => l.DyeChemicalId).Distinct().ToList();
        return await ReferenceValidation.RequireSelectableAsync(db.Set<BoyahaneDyeChemical>(), ids, current, "lines", "Boya / kimyasal", cancellationToken);
    }

    private async Task<List<BoyahaneStrippingRecipeDto>> LoadAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken)
    {
        var recipes = await db.Set<BoyahaneStrippingRecipe>().AsNoTracking()
            .Include(r => r.Lines).ThenInclude(l => l.DyeChemical)
            .Where(r => ids.Contains(r.Id))
            .ToListAsync(cancellationToken);
        var usage = await db.Set<BoyahaneBatchStep>()
            .Where(s => s.StrippingRecipeId != null && ids.Contains(s.StrippingRecipeId.Value))
            .GroupBy(s => s.StrippingRecipeId!.Value)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Key, g => g.Count, cancellationToken);

        return recipes.Select(r => new BoyahaneStrippingRecipeDto(
            r.Id,
            r.Code,
            r.Name,
            r.Notes,
            r.IsActive,
            usage.GetValueOrDefault(r.Id),
            r.Lines.OrderBy(l => l.Sequence).Select(l => new BoyahaneStrippingRecipeLineDto(
                l.Id, l.Sequence, l.DyeChemicalId, l.DyeChemical.Code, l.DyeChemical.Name, l.Amount, l.Unit, l.TemperatureCelsius, l.DurationMinutes)).ToList())).ToList();
    }

    private static Error CodeExists(string code)
    {
        var message = $"'{code}' kodu zaten kullanılıyor.";
        return new Error(ErrorCodes.CodeAlreadyExists, message, ErrorType.Conflict, new Dictionary<string, string[]> { ["code"] = [message] });
    }
}
