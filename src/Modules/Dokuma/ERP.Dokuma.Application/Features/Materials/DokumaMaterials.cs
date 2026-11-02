using System.Linq.Expressions;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Dokuma.Domain.Entities;
using ERP.Dokuma.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Dokuma.Application.Features.Materials;

public sealed record DokumaSupplierDto(int Id, string Code, string Name, string? TaxNumber, string? Phone, string? Email, string Currency, int? LeadTimeDays, bool IsActive);

public sealed record DokumaSupplierRequest(
    string Code,
    string Name,
    string? TaxNumber = null,
    string? Phone = null,
    string? Email = null,
    string? Currency = null,
    int? LeadTimeDays = null,
    bool IsActive = true);

public sealed class DokumaSupplierRequestValidator : AbstractValidator<DokumaSupplierRequest>
{
    public DokumaSupplierRequestValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName(200);
        RuleFor(x => x.TaxNumber).OptionalText(20);
        RuleFor(x => x.Phone).OptionalText(30);
        RuleFor(x => x.Email).EmailAddress().WithMessage("Geçerli bir e-posta girin.").MaximumLength(150).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Currency).Matches(@"^\s*[A-Za-z]{3}\s*$").WithMessage("Para birimi üç harfli ISO kodu olmalıdır.").When(x => !string.IsNullOrWhiteSpace(x.Currency));
        RuleFor(x => x.LeadTimeDays).InclusiveBetween(0, 3650).WithMessage("Teslim süresi 0 ile 3650 gün arasında olmalıdır.").When(x => x.LeadTimeDays is not null);
    }
}

public sealed class DokumaSupplierService(IAppDbContext db) : MasterDataService<DokumaSupplier, DokumaSupplierDto, DokumaSupplierRequest>(db)
{
    protected override DbSet<DokumaSupplier> Set => Db.Set<DokumaSupplier>();
    protected override string Label => "Tedarikçi";
    protected override string CodeField => "code";

    protected override string CodeOf(DokumaSupplierRequest request) => request.Code;
    protected override Expression<Func<DokumaSupplier, bool>> WithCode(string code) => e => e.Code == code;
    protected override Expression<Func<DokumaSupplier, bool>> Matches(string term) => e => e.Code.Contains(term) || e.Name.Contains(term);
    protected override IQueryable<DokumaSupplier> Order(IQueryable<DokumaSupplier> query) => query.OrderBy(e => e.Code);
    protected override Expression<Func<DokumaSupplier, DokumaSupplierDto>> ToDto => e => new DokumaSupplierDto(e.Id, e.Code, e.Name, e.TaxNumber, e.Phone, e.Email, e.Currency, e.LeadTimeDays, e.IsActive);
    protected override Expression<Func<DokumaSupplier, LookupDto>> ToLookup => e => new LookupDto(e.Id, e.Code, e.Name);

    protected override void Apply(DokumaSupplierRequest request, DokumaSupplier entity, string code)
    {
        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.TaxNumber = NullIfBlank(request.TaxNumber);
        entity.Phone = NullIfBlank(request.Phone);
        entity.Email = NullIfBlank(request.Email);
        entity.Currency = string.IsNullOrWhiteSpace(request.Currency) ? "TRY" : request.Currency.Trim().ToUpperInvariant();
        entity.LeadTimeDays = request.LeadTimeDays;
        entity.IsActive = request.IsActive;
    }

    protected override Task<Error?> ValidateAsync(DokumaSupplierRequest request, DokumaSupplier? existing, CancellationToken cancellationToken) =>
        CurrencyRules.RequireActiveAsync(Db, request.Currency, existing?.Currency, "currency", cancellationToken);

    protected override async Task<string?> FindUsageAsync(int id, CancellationToken cancellationToken)
    {
        if (await Db.Set<DokumaYarnLot>().AnyAsync(l => l.SupplierId == id, cancellationToken))
        {
            return "iplik girişleri";
        }

        if (await Db.Set<DokumaPurchaseOrder>().AnyAsync(o => o.SupplierId == id, cancellationToken))
        {
            return "satın alma siparişleri";
        }

        if (await Db.Set<DokumaMaterialLot>().AnyAsync(l => l.SupplierId == id, cancellationToken))
        {
            return "malzeme girişleri";
        }

        return await Db.Set<DokumaBeam>().AnyAsync(b => b.SizingSupplierId == id, cancellationToken) ? "haşıla gönderilen leventler" : null;
    }
}

public sealed record DokumaYarnDto(
    int Id,
    string YarnCode,
    string Name,
    decimal CountValue,
    YarnCountSystem CountSystem,
    int Plies,
    string? Composition,
    string? Twist,
    string? Color,
    bool IsActive);

public sealed record DokumaYarnRequest(
    string YarnCode,
    string Name,
    decimal CountValue,
    YarnCountSystem CountSystem = YarnCountSystem.Ne,
    int Plies = 1,
    string? Composition = null,
    string? Twist = null,
    string? Color = null,
    bool IsActive = true);

public sealed class DokumaYarnRequestValidator : AbstractValidator<DokumaYarnRequest>
{
    public DokumaYarnRequestValidator()
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
        RuleFor(x => x.Color).OptionalText(100);
    }
}

public sealed class DokumaYarnService(IAppDbContext db)
    : MasterDataService<DokumaYarn, DokumaYarnDto, DokumaYarnRequest>(db)
{
    protected override DbSet<DokumaYarn> Set => Db.Set<DokumaYarn>();
    protected override string Label => "İplik kartı";
    protected override string CodeField => "yarnCode";

    protected override string CodeOf(DokumaYarnRequest request) => request.YarnCode;
    protected override Expression<Func<DokumaYarn, bool>> WithCode(string code) => e => e.YarnCode == code;

    protected override Expression<Func<DokumaYarn, bool>> Matches(string term) =>
        e => e.YarnCode.Contains(term) || e.Name.Contains(term) || (e.Color != null && e.Color.Contains(term));

    protected override IQueryable<DokumaYarn> Order(IQueryable<DokumaYarn> query) => query.OrderBy(e => e.YarnCode);

    protected override Expression<Func<DokumaYarn, DokumaYarnDto>> ToDto => e => new DokumaYarnDto(
        e.Id, e.YarnCode, e.Name, e.CountValue, e.CountSystem, e.Plies, e.Composition, e.Twist, e.Color, e.IsActive);

    protected override Expression<Func<DokumaYarn, LookupDto>> ToLookup =>
        e => new LookupDto(e.Id, e.YarnCode, e.Name);

    protected override async Task<string?> FindUsageAsync(int id, CancellationToken cancellationToken)
    {
        if (await Db.Set<DokumaArticleYarn>().AnyAsync(y => y.YarnId == id, cancellationToken))
        {
            return "artikel kartları";
        }

        if (await Db.Set<DokumaPurchaseOrderLine>().AnyAsync(l => l.YarnId == id, cancellationToken))
        {
            return "satın alma siparişleri";
        }

        return await Db.Set<DokumaYarnLot>().AnyAsync(l => l.YarnId == id, cancellationToken) ? "iplik girişleri" : null;
    }

    protected override void Apply(DokumaYarnRequest request, DokumaYarn entity, string code)
    {
        entity.YarnCode = code;
        entity.Name = request.Name.Trim();
        entity.CountValue = request.CountValue;
        entity.CountSystem = request.CountSystem;
        entity.Plies = request.Plies;
        entity.Composition = NullIfBlank(request.Composition);
        entity.Twist = NullIfBlank(request.Twist);
        entity.Color = NullIfBlank(request.Color);
        entity.IsActive = request.IsActive;
    }
}

public sealed record DokumaLoomDto(
    int Id,
    string Code,
    string Name,
    LoomType LoomType,
    SheddingType SheddingType,
    decimal? MaxReedWidthCm,
    int? NominalSpeedRpm,
    string? Brand,
    bool IsActive);

public sealed record DokumaLoomRequest(
    string Code,
    string Name,
    LoomType LoomType = LoomType.Rapier,
    SheddingType SheddingType = SheddingType.Dobby,
    decimal? MaxReedWidthCm = null,
    int? NominalSpeedRpm = null,
    string? Brand = null,
    bool IsActive = true);

public sealed class DokumaLoomRequestValidator : AbstractValidator<DokumaLoomRequest>
{
    public DokumaLoomRequestValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName(200);
        RuleFor(x => x.LoomType).IsInEnum().WithMessage("Geçerli bir tezgâh türü seçin.");
        RuleFor(x => x.SheddingType).IsInEnum().WithMessage("Geçerli bir ağızlık sistemi seçin.");
        RuleFor(x => x.MaxReedWidthCm)
            .InclusiveBetween(1, 1000).WithMessage("Tarak eni 1 ile 1000 cm arasında olmalıdır.")
            .PrecisionScale(6, 2, true).WithMessage("Tarak eni en fazla iki ondalık olabilir.")
            .When(x => x.MaxReedWidthCm is not null);
        RuleFor(x => x.NominalSpeedRpm)
            .InclusiveBetween(1, 3000).WithMessage("Devir 1 ile 3000 arasında olmalıdır.")
            .When(x => x.NominalSpeedRpm is not null);
        RuleFor(x => x.Brand).OptionalText(100);
    }
}

public sealed class DokumaLoomService(IAppDbContext db)
    : MasterDataService<DokumaLoom, DokumaLoomDto, DokumaLoomRequest>(db)
{
    protected override DbSet<DokumaLoom> Set => Db.Set<DokumaLoom>();
    protected override string Label => "Tezgâh";
    protected override string CodeField => "code";

    protected override string CodeOf(DokumaLoomRequest request) => request.Code;
    protected override Expression<Func<DokumaLoom, bool>> WithCode(string code) => e => e.Code == code;
    protected override Expression<Func<DokumaLoom, bool>> Matches(string term) =>
        e => e.Code.Contains(term) || e.Name.Contains(term) || (e.Brand != null && e.Brand.Contains(term));
    protected override IQueryable<DokumaLoom> Order(IQueryable<DokumaLoom> query) => query.OrderBy(e => e.Code);

    protected override Expression<Func<DokumaLoom, DokumaLoomDto>> ToDto => e => new DokumaLoomDto(
        e.Id, e.Code, e.Name, e.LoomType, e.SheddingType, e.MaxReedWidthCm, e.NominalSpeedRpm, e.Brand, e.IsActive);

    protected override Expression<Func<DokumaLoom, LookupDto>> ToLookup => e => new LookupDto(e.Id, e.Code, e.Name);

    protected override void Apply(DokumaLoomRequest request, DokumaLoom entity, string code)
    {
        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.LoomType = request.LoomType;
        entity.SheddingType = request.SheddingType;
        entity.MaxReedWidthCm = request.MaxReedWidthCm;
        entity.NominalSpeedRpm = request.NominalSpeedRpm;
        entity.Brand = NullIfBlank(request.Brand);
        entity.IsActive = request.IsActive;
    }

    protected override async Task<string?> FindUsageAsync(int id, CancellationToken cancellationToken) =>
        await Db.Set<DokumaBeam>().AnyAsync(b => b.LoomId == id, cancellationToken) || await Db.Set<DokumaFabricRoll>().AnyAsync(r => r.LoomId == id, cancellationToken)
            || await Db.Set<DokumaTablet>().AnyAsync(t => t.LoomId == id, cancellationToken) || await Db.Set<DokumaOperatorLoom>().AnyAsync(l => l.LoomId == id, cancellationToken) || await Db.Set<DokumaLoomStop>().AnyAsync(s => s.LoomId == id, cancellationToken)
            ? "leventler, toplar, tabletler ve duruşlar"
            : null;
}

/// <param name="TotalEnds">Toplam tel sayısı: çözgü satırlarının toplamı.</param>
/// <param name="WarpYarnCount">Çözgüdeki iplik satırı sayısı.</param>
/// <param name="WeftYarnCount">Atkıdaki iplik satırı sayısı.</param>
public sealed record DokumaArticleDto(
    int Id,
    string ArticleCode,
    string Name,
    string? WeaveName,
    SheddingType SheddingType,
    string? Composition,
    decimal? ReedNumber,
    int? EndsPerDent,
    decimal ReedWidthCm,
    decimal WeftDensityPerCm,
    decimal RawWidthCm,
    int RawWeightGsm,
    decimal? WarpCrimpPercent,
    decimal? WeftCrimpPercent,
    decimal? WarpWastePercent,
    decimal? WeftWastePercent,
    int TotalEnds,
    int WarpYarnCount,
    int WeftYarnCount,
    bool IsActive);

public sealed record DokumaArticleRequest(
    string ArticleCode,
    string Name,
    decimal ReedWidthCm,
    decimal WeftDensityPerCm,
    decimal RawWidthCm,
    int RawWeightGsm,
    string? WeaveName = null,
    SheddingType SheddingType = SheddingType.Dobby,
    string? Composition = null,
    decimal? ReedNumber = null,
    int? EndsPerDent = null,
    decimal? WarpCrimpPercent = null,
    decimal? WeftCrimpPercent = null,
    decimal? WarpWastePercent = null,
    decimal? WeftWastePercent = null,
    bool IsActive = true);

public sealed class DokumaArticleRequestValidator : AbstractValidator<DokumaArticleRequest>
{
    public DokumaArticleRequestValidator()
    {
        RuleFor(x => x.ArticleCode).ValidCode();
        RuleFor(x => x.Name).ValidName(200);
        RuleFor(x => x.WeaveName).OptionalText(100);
        RuleFor(x => x.SheddingType).IsInEnum().WithMessage("Geçerli bir ağızlık sistemi seçin.");
        RuleFor(x => x.Composition).OptionalText(200);

        RuleFor(x => x.ReedWidthCm)
            .InclusiveBetween(1, 1000).WithMessage("Tarak eni 1 ile 1000 cm arasında olmalıdır.")
            .PrecisionScale(6, 2, true).WithMessage("Tarak eni en fazla iki ondalık olabilir.");
        RuleFor(x => x.WeftDensityPerCm)
            .InclusiveBetween(1, 500).WithMessage("Atkı sıklığı 1 ile 500 atkı/cm arasında olmalıdır.")
            .PrecisionScale(6, 2, true).WithMessage("Atkı sıklığı en fazla iki ondalık olabilir.");
        RuleFor(x => x.RawWidthCm)
            .InclusiveBetween(1, 1000).WithMessage("Ham en 1 ile 1000 cm arasında olmalıdır.")
            .PrecisionScale(6, 2, true).WithMessage("Ham en en fazla iki ondalık olabilir.");
        RuleFor(x => x.RawWeightGsm).InclusiveBetween(1, 5000).WithMessage("Ham gramaj 1 ile 5000 g/m² arasında olmalıdır.");

        RuleFor(x => x.ReedNumber)
            .GreaterThan(0).WithMessage("Tarak numarası sıfırdan büyük olmalıdır.")
            .PrecisionScale(8, 2, true).WithMessage("Tarak numarası en fazla iki ondalık olabilir.")
            .When(x => x.ReedNumber is not null);
        RuleFor(x => x.EndsPerDent).InclusiveBetween(1, 20).WithMessage("Diş başına tel 1 ile 20 arasında olmalıdır.").When(x => x.EndsPerDent is not null);
        RuleFor(x => x.WarpCrimpPercent)
            .InclusiveBetween(0, 100).WithMessage("Çözgü çekmesi 0 ile 100 arasında olmalıdır.")
            .PrecisionScale(5, 2, true).WithMessage("Çözgü çekmesi en fazla iki ondalık olabilir.")
            .When(x => x.WarpCrimpPercent is not null);
        RuleFor(x => x.WeftCrimpPercent)
            .InclusiveBetween(0, 100).WithMessage("Atkı çekmesi 0 ile 100 arasında olmalıdır.")
            .PrecisionScale(5, 2, true).WithMessage("Atkı çekmesi en fazla iki ondalık olabilir.")
            .When(x => x.WeftCrimpPercent is not null);
        RuleFor(x => x.WarpWastePercent)
            .InclusiveBetween(0, 100).WithMessage("Çözgü firesi 0 ile 100 arasında olmalıdır.")
            .PrecisionScale(5, 2, true).WithMessage("Çözgü firesi en fazla iki ondalık olabilir.")
            .When(x => x.WarpWastePercent is not null);
        RuleFor(x => x.WeftWastePercent)
            .InclusiveBetween(0, 100).WithMessage("Atkı firesi 0 ile 100 arasında olmalıdır.")
            .PrecisionScale(5, 2, true).WithMessage("Atkı firesi en fazla iki ondalık olabilir.")
            .When(x => x.WeftWastePercent is not null);

        // Ham en, tarak eninden büyük olamaz: kumaş tezgâhtan çıkınca daralır.
        RuleFor(x => x.RawWidthCm)
            .LessThanOrEqualTo(x => x.ReedWidthCm).WithMessage("Ham en tarak eninden büyük olamaz.");
    }
}

public sealed class DokumaArticleService(IAppDbContext db)
    : MasterDataService<DokumaArticle, DokumaArticleDto, DokumaArticleRequest>(db)
{
    protected override DbSet<DokumaArticle> Set => Db.Set<DokumaArticle>();
    protected override string Label => "Artikel";
    protected override string CodeField => "articleCode";

    protected override string CodeOf(DokumaArticleRequest request) => request.ArticleCode;
    protected override Expression<Func<DokumaArticle, bool>> WithCode(string code) => e => e.ArticleCode == code;
    protected override Expression<Func<DokumaArticle, bool>> Matches(string term) =>
        e => e.ArticleCode.Contains(term) || e.Name.Contains(term) || (e.WeaveName != null && e.WeaveName.Contains(term));
    protected override IQueryable<DokumaArticle> Order(IQueryable<DokumaArticle> query) => query.OrderBy(e => e.ArticleCode);

    protected override Expression<Func<DokumaArticle, DokumaArticleDto>> ToDto => e => new DokumaArticleDto(
        e.Id, e.ArticleCode, e.Name, e.WeaveName, e.SheddingType, e.Composition, e.ReedNumber, e.EndsPerDent, e.ReedWidthCm,
        e.WeftDensityPerCm, e.RawWidthCm, e.RawWeightGsm, e.WarpCrimpPercent, e.WeftCrimpPercent, e.WarpWastePercent, e.WeftWastePercent,
        e.Yarns.Where(y => y.Role == ArticleYarnRole.Warp).Sum(y => (int?)y.Ends) ?? 0,
        e.Yarns.Count(y => y.Role == ArticleYarnRole.Warp),
        e.Yarns.Count(y => y.Role == ArticleYarnRole.Weft),
        e.IsActive);

    protected override Expression<Func<DokumaArticle, LookupDto>> ToLookup => e => new LookupDto(e.Id, e.ArticleCode, e.Name);

    protected override void Apply(DokumaArticleRequest request, DokumaArticle entity, string code)
    {
        entity.ArticleCode = code;
        entity.Name = request.Name.Trim();
        entity.WeaveName = NullIfBlank(request.WeaveName);
        entity.SheddingType = request.SheddingType;
        entity.Composition = NullIfBlank(request.Composition);
        entity.ReedNumber = request.ReedNumber;
        entity.EndsPerDent = request.EndsPerDent;
        entity.ReedWidthCm = request.ReedWidthCm;
        entity.WeftDensityPerCm = request.WeftDensityPerCm;
        entity.RawWidthCm = request.RawWidthCm;
        entity.RawWeightGsm = request.RawWeightGsm;
        entity.WarpCrimpPercent = request.WarpCrimpPercent;
        entity.WeftCrimpPercent = request.WeftCrimpPercent;
        entity.WarpWastePercent = request.WarpWastePercent;
        entity.WeftWastePercent = request.WeftWastePercent;
        entity.IsActive = request.IsActive;
    }

    protected override async Task<string?> FindUsageAsync(int id, CancellationToken cancellationToken) =>
        await Db.Set<DokumaOrderLine>().AnyAsync(l => l.ArticleId == id, cancellationToken) ? "siparişler"
        : await Db.Set<DokumaSizingRecipe>().AnyAsync(r => r.ArticleId == id, cancellationToken) ? "haşıl reçeteleri"
        : null;
}

public sealed record DokumaArticleYarnDto(int Id, ArticleYarnRole Role, int Sequence, int YarnId, string YarnCode, string YarnName, string? YarnColor, int Ends);

public sealed record DokumaArticleYarnsDto(int ArticleId, IReadOnlyList<DokumaArticleYarnDto> Yarns);

/// <param name="Ends">Çözgüde bu iplikten tel sayısı; atkıda desen raporundaki atkı sayısı.</param>
public sealed record DokumaArticleYarnRequest(ArticleYarnRole Role, int YarnId, int Ends);

/// <summary>Artikelin iplik satırlarının tamamı, sırasıyla; gönderilmeyen satırlar silinir.</summary>
public sealed record SaveDokumaArticleYarnsRequest(IReadOnlyList<DokumaArticleYarnRequest> Yarns);

public sealed class SaveDokumaArticleYarnsRequestValidator : AbstractValidator<SaveDokumaArticleYarnsRequest>
{
    public const int MaxLines = 60;

    public SaveDokumaArticleYarnsRequestValidator()
    {
        // Hatalar tek alanda ("yarns") toplanır; ileti hangi satırın hatalı olduğunu söyler.
        RuleFor(x => x.Yarns)
            .NotNull().WithMessage("İplik listesi zorunludur.")
            .Must(list => list.Count <= MaxLines).WithMessage($"En fazla {MaxLines} iplik satırı girilebilir.")
            .Custom((list, context) =>
            {
                if (list is null)
                {
                    return;
                }

                for (var i = 0; i < list.Count; i++)
                {
                    var line = list[i];
                    if (!Enum.IsDefined(line.Role))
                    {
                        context.AddFailure($"{i + 1}. satır: geçerli bir yer (çözgü / atkı) seçin.");
                    }

                    if (line.YarnId <= 0)
                    {
                        context.AddFailure($"{i + 1}. satır: iplik seçin.");
                    }

                    if (line.Ends is < 1 or > 100_000)
                    {
                        context.AddFailure($"{i + 1}. satır: adet 1 ile 100.000 arasında olmalıdır.");
                    }
                }

                // Satır girildiyse artikelin hem çözgüsü hem atkısı olmalıdır.
                if (list.Count > 0 && (list.All(l => l.Role != ArticleYarnRole.Warp) || list.All(l => l.Role != ArticleYarnRole.Weft)))
                {
                    context.AddFailure("Artikelde en az bir çözgü ve bir atkı ipliği olmalıdır.");
                }
            });
    }
}

public interface IDokumaArticleYarnService
{
    Task<Result<DokumaArticleYarnsDto>> GetAsync(int articleId, CancellationToken cancellationToken);
    Task<Result<DokumaArticleYarnsDto>> SaveAsync(int articleId, SaveDokumaArticleYarnsRequest request, CancellationToken cancellationToken);
}

/// <summary>Artikelin çözgü ve atkı iplikleri: liste bütün olarak kaydedilir, sıra gönderilen sıradır.</summary>
public sealed class DokumaArticleYarnService(IAppDbContext db) : IDokumaArticleYarnService
{
    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Artikel bulunamadı.", ErrorType.NotFound);

    public async Task<Result<DokumaArticleYarnsDto>> GetAsync(int articleId, CancellationToken cancellationToken)
    {
        if (!await db.Set<DokumaArticle>().AnyAsync(a => a.Id == articleId, cancellationToken))
        {
            return NotFound;
        }

        var yarns = await db.Set<DokumaArticleYarn>().AsNoTracking()
            .Where(y => y.ArticleId == articleId)
            .OrderBy(y => y.Role).ThenBy(y => y.Sequence)
            .Select(y => new DokumaArticleYarnDto(y.Id, y.Role, y.Sequence, y.YarnId, y.Yarn.YarnCode, y.Yarn.Name, y.Yarn.Color, y.Ends))
            .ToListAsync(cancellationToken);

        return new DokumaArticleYarnsDto(articleId, yarns);
    }

    public async Task<Result<DokumaArticleYarnsDto>> SaveAsync(int articleId, SaveDokumaArticleYarnsRequest request, CancellationToken cancellationToken)
    {
        if (!await db.Set<DokumaArticle>().AnyAsync(a => a.Id == articleId, cancellationToken))
        {
            return NotFound;
        }

        var set = db.Set<DokumaArticleYarn>();
        var existing = await set.Where(y => y.ArticleId == articleId).ToListAsync(cancellationToken);

        // Kartta zaten bulunan pasif iplik kalabilir; yeni eklenen iplik etkin olmalıdır.
        var reference = await ReferenceValidation.RequireSelectableAsync(
            db.Set<DokumaYarn>(),
            request.Yarns.Select(y => y.YarnId).Distinct().ToList(),
            existing.Select(y => y.YarnId).ToHashSet(),
            "yarns",
            "İplik",
            cancellationToken);
        if (reference is not null)
        {
            return reference;
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // Sıra dizini benzersiz olduğu için eski satırlar önce silinir.
        set.RemoveRange(existing);
        await db.SaveChangesAsync(cancellationToken);

        foreach (var group in request.Yarns.GroupBy(y => y.Role))
        {
            var sequence = 0;
            foreach (var line in group)
            {
                set.Add(new DokumaArticleYarn { ArticleId = articleId, Role = line.Role, Sequence = ++sequence, YarnId = line.YarnId, Ends = line.Ends });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(articleId, cancellationToken);
    }
}
