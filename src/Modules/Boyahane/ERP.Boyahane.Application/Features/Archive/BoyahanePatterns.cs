using System.Linq.Expressions;
using ERP.Boyahane.Domain.Entities;
using ERP.Boyahane.Domain.Enums;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Boyahane.Application.Features.Archive;

public sealed record BoyahanePatternDto(
    int Id,
    string Code,
    string Name,
    int? CustomerId,
    string? CustomerName,
    decimal? RepeatWidthCm,
    decimal? RepeatHeightCm,
    PrintType PrintType,
    int? ScreenCount,
    string? Notes,
    bool HasImage,
    int ColorwayCount,
    bool IsActive);

public sealed record BoyahanePatternRequest(
    string Code,
    string Name,
    int? CustomerId = null,
    decimal? RepeatWidthCm = null,
    decimal? RepeatHeightCm = null,
    PrintType PrintType = PrintType.Pigment,
    int? ScreenCount = null,
    string? Notes = null,
    bool IsActive = true);

public sealed class BoyahanePatternRequestValidator : AbstractValidator<BoyahanePatternRequest>
{
    public BoyahanePatternRequestValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName(100);
        RuleFor(x => x.PrintType).IsInEnum().WithMessage("Geçerli bir baskı türü seçin.");
        RuleFor(x => x.Notes).OptionalText(500);
        RuleFor(x => x.RepeatWidthCm).InclusiveBetween(0.01m, 10_000).WithMessage("Rapor eni sıfırdan büyük olmalıdır.").When(x => x.RepeatWidthCm is not null);
        RuleFor(x => x.RepeatHeightCm).InclusiveBetween(0.01m, 10_000).WithMessage("Rapor boyu sıfırdan büyük olmalıdır.").When(x => x.RepeatHeightCm is not null);
        RuleFor(x => x.ScreenCount).InclusiveBetween(1, 50).WithMessage("Şablon / silindir sayısı 1 ile 50 arasında olmalıdır.").When(x => x.ScreenCount is not null);
    }
}

/// <summary>Desen kartları (desen arşivi). Renk yolları ve baskı reçeteleri desen ekranından yönetilir.</summary>
public sealed class BoyahanePatternService(IAppDbContext db)
    : MasterDataService<BoyahanePattern, BoyahanePatternDto, BoyahanePatternRequest>(db)
{
    protected override DbSet<BoyahanePattern> Set => Db.Set<BoyahanePattern>();
    protected override string Label => "Desen kartı";
    protected override string CodeField => "code";

    protected override string CodeOf(BoyahanePatternRequest request) => request.Code;
    protected override Expression<Func<BoyahanePattern, bool>> WithCode(string code) => e => e.Code == code;

    protected override Expression<Func<BoyahanePattern, bool>> Matches(string term) => e =>
        e.Code.Contains(term) || e.Name.Contains(term) || (e.Customer != null && e.Customer.Name.Contains(term));

    protected override IQueryable<BoyahanePattern> Order(IQueryable<BoyahanePattern> query) => query.OrderBy(e => e.Code);

    protected override Expression<Func<BoyahanePattern, BoyahanePatternDto>> ToDto => e => new BoyahanePatternDto(
        e.Id,
        e.Code,
        e.Name,
        e.CustomerId,
        e.Customer != null ? e.Customer.Name : null,
        e.RepeatWidthCm,
        e.RepeatHeightCm,
        e.PrintType,
        e.ScreenCount,
        e.Notes,
        Db.Set<BoyahanePatternImage>().Any(i => i.PatternId == e.Id),
        e.Colorways.Count,
        e.IsActive);

    protected override Expression<Func<BoyahanePattern, LookupDto>> ToLookup =>
        e => new LookupDto(e.Id, e.Code, e.Name) { Detail = e.Customer != null ? e.Customer.Name : null };

    /// <summary>Müşteri verilirse genel desenler ve o müşterinin desenleri listelenir.</summary>
    protected override IQueryable<BoyahanePattern> FilterLookup(IQueryable<BoyahanePattern> items, LookupQuery query) =>
        query.CustomerId is { } customerId ? items.Where(e => e.CustomerId == null || e.CustomerId == customerId) : items;

    protected override void Apply(BoyahanePatternRequest request, BoyahanePattern entity, string code)
    {
        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.CustomerId = request.CustomerId;
        entity.RepeatWidthCm = request.RepeatWidthCm;
        entity.RepeatHeightCm = request.RepeatHeightCm;
        entity.PrintType = request.PrintType;
        entity.ScreenCount = request.ScreenCount;
        entity.Notes = NullIfBlank(request.Notes);
        entity.IsActive = request.IsActive;
    }

    protected override Task<Error?> ValidateAsync(BoyahanePatternRequest request, BoyahanePattern? existing, CancellationToken cancellationToken) =>
        RequireSelectableAsync(Db.Set<BoyahaneCustomer>(), request.CustomerId, existing?.CustomerId, "customerId", "Müşteri", cancellationToken);

    protected override async Task<string?> FindUsageAsync(int id, CancellationToken cancellationToken) =>
        await Db.Set<BoyahaneOrderLine>().AnyAsync(l => l.PatternId == id, cancellationToken) ? "sipariş satırları" : null;
}

public sealed record BoyahanePrintRecipeLineDto(int Id, int DyeChemicalId, string Code, string Name, DyeChemicalKind Kind, decimal GramsPerKilogram);

/// <param name="PasteKilogramsPerMeter">Metre başına tahmini pasta (kg).</param>
public sealed record BoyahaneColorwayColorDto(
    int Id,
    int Position,
    int? ColorCardId,
    string? ColorCardCode,
    string Name,
    decimal? PasteKilogramsPerMeter,
    IReadOnlyList<BoyahanePrintRecipeLineDto> Lines);

/// <param name="OrderLineCount">Bu renk yolunu seçen sipariş satırı sayısı.</param>
public sealed record BoyahaneColorwayDto(
    int Id,
    int PatternId,
    string Code,
    string Name,
    bool IsActive,
    int OrderLineCount,
    IReadOnlyList<BoyahaneColorwayColorDto> Colors);

/// <param name="GramsPerKilogram">Kg pasta başına gram.</param>
public sealed record BoyahanePrintRecipeLineRequest(int DyeChemicalId, decimal GramsPerKilogram);

/// <param name="Position">Şablon / silindir sırası.</param>
/// <param name="Name">Renk kartı seçilmediyse zorunlu; seçildiyse boşsa kartın adı kullanılır.</param>
public sealed record BoyahaneColorwayColorRequest(
    int Position,
    int? ColorCardId = null,
    string? Name = null,
    decimal? PasteKilogramsPerMeter = null,
    IReadOnlyList<BoyahanePrintRecipeLineRequest>? Lines = null);

public sealed record BoyahaneColorwayRequest(string Code, string Name, IReadOnlyList<BoyahaneColorwayColorRequest> Colors, bool IsActive = true);

public sealed class BoyahaneColorwayRequestValidator : AbstractValidator<BoyahaneColorwayRequest>
{
    public const int MaxColors = 30;
    public const int MaxLines = 30;

    public BoyahaneColorwayRequestValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName(100);
        RuleFor(x => x.Colors)
            .NotEmpty().WithMessage("Renk yolunda en az bir renk olmalıdır.")
            .Must(colors => colors.Count <= MaxColors).WithMessage($"Bir renk yolunda en fazla {MaxColors} renk olabilir.")
            .Must(colors => colors.Select(c => c.Position).Distinct().Count() == colors.Count).WithMessage("Aynı sıra numarası iki kez girilemez.")
            .Custom((colors, context) =>
            {
                if (colors is null)
                {
                    return;
                }

                foreach (var color in colors)
                {
                    foreach (var problem in Problems(color))
                    {
                        context.AddFailure($"{color.Position}. renk: {problem}");
                    }
                }
            });
    }

    private static IEnumerable<string> Problems(BoyahaneColorwayColorRequest color)
    {
        if (color.Position is < 1 or > 99)
        {
            yield return "Sıra 1 ile 99 arasında olmalıdır.";
        }

        if (color.ColorCardId is null && string.IsNullOrWhiteSpace(color.Name))
        {
            yield return "Renk kartı seçin ya da renk adı yazın.";
        }

        if (color.Name?.Trim().Length > 100)
        {
            yield return "Renk adı en fazla 100 karakter olabilir.";
        }

        if (color.PasteKilogramsPerMeter is <= 0 or > 100)
        {
            yield return "Metre başına pasta sıfırdan büyük olmalıdır.";
        }

        var lines = color.Lines ?? [];
        if (lines.Count > MaxLines)
        {
            yield return $"En fazla {MaxLines} reçete satırı girilebilir.";
        }

        if (lines.Any(l => l.DyeChemicalId <= 0))
        {
            yield return "Reçete satırında boya / kimyasal seçilmelidir.";
        }

        if (lines.Any(l => l.GramsPerKilogram is <= 0 or > 1000))
        {
            yield return "Reçete miktarı 0 ile 1000 g/kg arasında olmalıdır.";
        }

        if (lines.Select(l => l.DyeChemicalId).Distinct().Count() != lines.Count)
        {
            yield return "Aynı boya / kimyasal iki kez girilemez.";
        }
    }
}

public interface IBoyahanePatternDetailService
{
    Task<Result<BoyahaneArchiveImageDto>> GetImageAsync(int id, CancellationToken cancellationToken);

    /// <summary>Desenin görüntüsünü yükler; varsa eskisinin yerine geçer.</summary>
    Task<Result> SetImageAsync(int id, BoyahaneArchiveImageRequest request, CancellationToken cancellationToken);

    Task<Result> RemoveImageAsync(int id, CancellationToken cancellationToken);

    /// <summary>Desenin renk yolları, renkleri ve baskı reçeteleri.</summary>
    Task<Result<IReadOnlyList<BoyahaneColorwayDto>>> GetColorwaysAsync(int id, CancellationToken cancellationToken);

    /// <summary>Sipariş satırı için etkin renk yolları; seçili olan pasif olsa da listede kalır.</summary>
    Task<Result<IReadOnlyList<LookupDto>>> LookupColorwaysAsync(int id, LookupQuery query, CancellationToken cancellationToken);

    Task<Result<BoyahaneColorwayDto>> CreateColorwayAsync(int id, BoyahaneColorwayRequest request, CancellationToken cancellationToken);
    Task<Result<BoyahaneColorwayDto>> UpdateColorwayAsync(int id, int colorwayId, BoyahaneColorwayRequest request, CancellationToken cancellationToken);
    Task<Result> DeleteColorwayAsync(int id, int colorwayId, CancellationToken cancellationToken);
}

/// <summary>
/// Desenin görüntüsü ve renk yolları. Renk yolunun her rengi bir şablon / silindirdir; baskı reçetesi (g/kg pasta) ve
/// metre başına pasta tahmini rengin üzerindedir. Baskı partisinde tüketim buradan hesaplanır.
/// </summary>
public sealed class BoyahanePatternDetailService(IAppDbContext db, TimeProvider timeProvider) : IBoyahanePatternDetailService
{
    private const string InvalidState = "BOYAHANE_PATTERN_INVALID_STATE";

    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Desen kartı bulunamadı.", ErrorType.NotFound);
    private static readonly Error ColorwayNotFound = new(ErrorCodes.NotFound, "Renk yolu bulunamadı.", ErrorType.NotFound);
    private static readonly Error ImageNotFound = new(ErrorCodes.NotFound, "Desenin görüntüsü yok.", ErrorType.NotFound);

    public async Task<Result<BoyahaneArchiveImageDto>> GetImageAsync(int id, CancellationToken cancellationToken)
    {
        if (!await ExistsAsync(id, cancellationToken))
        {
            return NotFound;
        }

        var image = await db.Set<BoyahanePatternImage>().AsNoTracking().SingleOrDefaultAsync(i => i.PatternId == id, cancellationToken);
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

        var image = await db.Set<BoyahanePatternImage>().SingleOrDefaultAsync(i => i.PatternId == id, cancellationToken);
        if (image is null)
        {
            image = new BoyahanePatternImage { PatternId = id };
            db.Set<BoyahanePatternImage>().Add(image);
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

        await db.Set<BoyahanePatternImage>().Where(i => i.PatternId == id).ExecuteDeleteAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<BoyahaneColorwayDto>>> GetColorwaysAsync(int id, CancellationToken cancellationToken)
    {
        if (!await ExistsAsync(id, cancellationToken))
        {
            return NotFound;
        }

        var colorways = await Load(db.Set<BoyahanePatternColorway>().AsNoTracking().Where(c => c.PatternId == id))
            .OrderBy(c => c.Code)
            .ToListAsync(cancellationToken);
        var usage = await UsageAsync(colorways.Select(c => c.Id).ToList(), cancellationToken);
        return colorways.Select(c => ToDto(c, usage.GetValueOrDefault(c.Id))).ToList();
    }

    public async Task<Result<IReadOnlyList<LookupDto>>> LookupColorwaysAsync(int id, LookupQuery query, CancellationToken cancellationToken)
    {
        if (!await ExistsAsync(id, cancellationToken))
        {
            return NotFound;
        }

        var items = db.Set<BoyahanePatternColorway>().AsNoTracking().Where(c => c.PatternId == id && (c.IsActive || c.Id == query.IncludeId));
        if (NullIfBlank(query.Search) is { } term)
        {
            items = items.Where(c => c.Code.Contains(term) || c.Name.Contains(term) || c.Id == query.IncludeId);
        }

        return await items
            .OrderBy(c => c.Code)
            .Take(query.NormalizedTake)
            .Select(c => new LookupDto(c.Id, c.Code, c.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<Result<BoyahaneColorwayDto>> CreateColorwayAsync(int id, BoyahaneColorwayRequest request, CancellationToken cancellationToken)
    {
        var pattern = await db.Set<BoyahanePattern>().AsNoTracking().SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (pattern is null)
        {
            return NotFound;
        }

        var code = NormalizeCode(request.Code);
        if (await db.Set<BoyahanePatternColorway>().AnyAsync(c => c.PatternId == id && c.Code == code, cancellationToken))
        {
            return CodeExists(code);
        }

        if (await ValidateColorsAsync(pattern, request, null, cancellationToken) is { } error)
        {
            return error;
        }

        var colorway = new BoyahanePatternColorway { PatternId = id };
        await ApplyAsync(colorway, code, request, cancellationToken);
        db.Set<BoyahanePatternColorway>().Add(colorway);
        await db.SaveChangesAsync(cancellationToken);
        return await GetColorwayAsync(colorway.Id, cancellationToken);
    }

    public async Task<Result<BoyahaneColorwayDto>> UpdateColorwayAsync(int id, int colorwayId, BoyahaneColorwayRequest request, CancellationToken cancellationToken)
    {
        var pattern = await db.Set<BoyahanePattern>().AsNoTracking().SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        var colorway = await db.Set<BoyahanePatternColorway>()
            .Include(c => c.Colors).ThenInclude(c => c.Lines)
            .SingleOrDefaultAsync(c => c.Id == colorwayId && c.PatternId == id, cancellationToken);
        if (pattern is null || colorway is null)
        {
            return ColorwayNotFound;
        }

        var code = NormalizeCode(request.Code);
        if (await db.Set<BoyahanePatternColorway>().AnyAsync(c => c.PatternId == id && c.Code == code && c.Id != colorwayId, cancellationToken))
        {
            return CodeExists(code);
        }

        if (await ValidateColorsAsync(pattern, request, colorway, cancellationToken) is { } error)
        {
            return error;
        }

        // Sıra ve kalem benzersiz olduğu için eski renkler yenilerden önce silinir; iki adım tek işlemdir.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.Set<BoyahanePrintRecipeLine>().RemoveRange(colorway.Colors.SelectMany(c => c.Lines));
        db.Set<BoyahaneColorwayColor>().RemoveRange(colorway.Colors);
        await db.SaveChangesAsync(cancellationToken);
        colorway.Colors.Clear();
        await ApplyAsync(colorway, code, request, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetColorwayAsync(colorwayId, cancellationToken);
    }

    public async Task<Result> DeleteColorwayAsync(int id, int colorwayId, CancellationToken cancellationToken)
    {
        var colorway = await db.Set<BoyahanePatternColorway>()
            .Include(c => c.Colors).ThenInclude(c => c.Lines)
            .SingleOrDefaultAsync(c => c.Id == colorwayId && c.PatternId == id, cancellationToken);
        if (colorway is null)
        {
            return ColorwayNotFound;
        }

        if (await db.Set<BoyahaneOrderLine>().AnyAsync(l => l.ColorwayId == colorwayId, cancellationToken))
        {
            return new Error(ErrorCodes.RecordInUse, "Renk yolu sipariş satırlarında kullanıldığı için silinemez. Bunun yerine pasif yapabilirsiniz.", ErrorType.Conflict);
        }

        db.Set<BoyahanePrintRecipeLine>().RemoveRange(colorway.Colors.SelectMany(c => c.Lines));
        db.Set<BoyahaneColorwayColor>().RemoveRange(colorway.Colors);
        db.Set<BoyahanePatternColorway>().Remove(colorway);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<Error?> ValidateColorsAsync(BoyahanePattern pattern, BoyahaneColorwayRequest request, BoyahanePatternColorway? existing, CancellationToken cancellationToken)
    {
        if (pattern.ScreenCount is { } screens && request.Colors.Any(c => c.Position > screens))
        {
            return ReferenceValidation.InvalidReference("colors", $"Desende {screens} şablon / silindir var; renk sırası bunu aşamaz.");
        }

        var currentCards = existing?.Colors.Where(c => c.ColorCardId is not null).Select(c => c.ColorCardId!.Value).ToHashSet() ?? [];
        var cardIds = request.Colors.Where(c => c.ColorCardId is not null).Select(c => c.ColorCardId!.Value).Distinct().ToList();
        if (cardIds.Count > 0
            && await ReferenceValidation.RequireSelectableAsync(db.Set<BoyahaneColorCard>(), cardIds, currentCards, "colors", "Renk kartı", cancellationToken) is { } cardError)
        {
            return cardError;
        }

        var currentChemicals = existing?.Colors.SelectMany(c => c.Lines).Select(l => l.DyeChemicalId).ToHashSet() ?? [];
        var chemicalIds = request.Colors.SelectMany(c => c.Lines ?? []).Select(l => l.DyeChemicalId).Distinct().ToList();
        return chemicalIds.Count > 0
            ? await ReferenceValidation.RequireSelectableAsync(db.Set<BoyahaneDyeChemical>(), chemicalIds, currentChemicals, "colors", "Boya / kimyasal", cancellationToken)
            : null;
    }

    private async Task ApplyAsync(BoyahanePatternColorway colorway, string code, BoyahaneColorwayRequest request, CancellationToken cancellationToken)
    {
        var cardIds = request.Colors.Where(c => c.ColorCardId is not null).Select(c => c.ColorCardId!.Value).Distinct().ToList();
        var cardNames = await db.Set<BoyahaneColorCard>().Where(c => cardIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

        colorway.Code = code;
        colorway.Name = request.Name.Trim();
        colorway.IsActive = request.IsActive;
        foreach (var item in request.Colors.OrderBy(c => c.Position))
        {
            var color = new BoyahaneColorwayColor
            {
                Position = item.Position,
                ColorCardId = item.ColorCardId,
                Name = NullIfBlank(item.Name) ?? (item.ColorCardId is { } cardId ? cardNames.GetValueOrDefault(cardId) ?? "" : ""),
                PasteKilogramsPerMeter = item.PasteKilogramsPerMeter,
            };
            foreach (var line in item.Lines ?? [])
            {
                color.Lines.Add(new BoyahanePrintRecipeLine { DyeChemicalId = line.DyeChemicalId, GramsPerKilogram = line.GramsPerKilogram });
            }

            colorway.Colors.Add(color);
        }
    }

    private async Task<Result<BoyahaneColorwayDto>> GetColorwayAsync(int colorwayId, CancellationToken cancellationToken)
    {
        var colorway = await Load(db.Set<BoyahanePatternColorway>().AsNoTracking()).SingleAsync(c => c.Id == colorwayId, cancellationToken);
        var usage = await UsageAsync([colorwayId], cancellationToken);
        return ToDto(colorway, usage.GetValueOrDefault(colorwayId));
    }

    private static IQueryable<BoyahanePatternColorway> Load(IQueryable<BoyahanePatternColorway> colorways) => colorways
        .Include(c => c.Colors).ThenInclude(c => c.ColorCard)
        .Include(c => c.Colors).ThenInclude(c => c.Lines).ThenInclude(l => l.DyeChemical);

    private async Task<Dictionary<int, int>> UsageAsync(IReadOnlyList<int> colorwayIds, CancellationToken cancellationToken) =>
        await db.Set<BoyahaneOrderLine>()
            .Where(l => l.ColorwayId != null && colorwayIds.Contains(l.ColorwayId.Value))
            .GroupBy(l => l.ColorwayId!.Value)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Key, g => g.Count, cancellationToken);

    private static BoyahaneColorwayDto ToDto(BoyahanePatternColorway colorway, int orderLineCount) => new(
        colorway.Id,
        colorway.PatternId,
        colorway.Code,
        colorway.Name,
        colorway.IsActive,
        orderLineCount,
        colorway.Colors.OrderBy(c => c.Position).Select(c => new BoyahaneColorwayColorDto(
            c.Id,
            c.Position,
            c.ColorCardId,
            c.ColorCard?.Code,
            c.Name,
            c.PasteKilogramsPerMeter,
            c.Lines.OrderBy(l => l.DyeChemical.Kind).ThenBy(l => l.Id).Select(l => new BoyahanePrintRecipeLineDto(
                l.Id, l.DyeChemicalId, l.DyeChemical.Code, l.DyeChemical.Name, l.DyeChemical.Kind, l.GramsPerKilogram)).ToList())).ToList());

    private static Error CodeExists(string code)
    {
        var message = $"'{code}' kodlu renk yolu bu desende zaten var.";
        return new Error(ErrorCodes.CodeAlreadyExists, message, ErrorType.Conflict, new Dictionary<string, string[]> { ["code"] = [message] });
    }

    private Task<bool> ExistsAsync(int id, CancellationToken cancellationToken) =>
        db.Set<BoyahanePattern>().AnyAsync(p => p.Id == id, cancellationToken);
}
