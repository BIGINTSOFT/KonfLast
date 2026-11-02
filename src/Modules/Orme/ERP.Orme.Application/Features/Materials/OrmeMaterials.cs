using System.Linq.Expressions;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Orme.Domain.Entities;
using ERP.Orme.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Orme.Application.Features.Materials;

public sealed record OrmeSupplierDto(int Id, string Code, string Name, string? TaxNumber, string? Phone, string? Email, string Currency, int? LeadTimeDays, bool IsActive);

public sealed record OrmeSupplierRequest(
    string Code,
    string Name,
    string? TaxNumber = null,
    string? Phone = null,
    string? Email = null,
    string? Currency = null,
    int? LeadTimeDays = null,
    bool IsActive = true);

public sealed class OrmeSupplierRequestValidator : AbstractValidator<OrmeSupplierRequest>
{
    public OrmeSupplierRequestValidator()
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

public sealed class OrmeSupplierService(IAppDbContext db) : MasterDataService<OrmeSupplier, OrmeSupplierDto, OrmeSupplierRequest>(db)
{
    protected override DbSet<OrmeSupplier> Set => Db.Set<OrmeSupplier>();
    protected override string Label => "Tedarikçi";
    protected override string CodeField => "code";

    protected override string CodeOf(OrmeSupplierRequest request) => request.Code;
    protected override Expression<Func<OrmeSupplier, bool>> WithCode(string code) => e => e.Code == code;
    protected override Expression<Func<OrmeSupplier, bool>> Matches(string term) => e => e.Code.Contains(term) || e.Name.Contains(term);
    protected override IQueryable<OrmeSupplier> Order(IQueryable<OrmeSupplier> query) => query.OrderBy(e => e.Code);
    protected override Expression<Func<OrmeSupplier, OrmeSupplierDto>> ToDto => e => new OrmeSupplierDto(e.Id, e.Code, e.Name, e.TaxNumber, e.Phone, e.Email, e.Currency, e.LeadTimeDays, e.IsActive);
    protected override Expression<Func<OrmeSupplier, LookupDto>> ToLookup => e => new LookupDto(e.Id, e.Code, e.Name);

    protected override void Apply(OrmeSupplierRequest request, OrmeSupplier entity, string code)
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

    protected override Task<Error?> ValidateAsync(OrmeSupplierRequest request, OrmeSupplier? existing, CancellationToken cancellationToken) =>
        CurrencyRules.RequireActiveAsync(Db, request.Currency, existing?.Currency, "currency", cancellationToken);


    protected override async Task<string?> FindUsageAsync(int id, CancellationToken cancellationToken) =>
        await Db.Set<OrmeYarnLot>().AnyAsync(l => l.SupplierId == id, cancellationToken) ? "iplik girişleri"
        : await Db.Set<OrmePurchaseOrder>().AnyAsync(o => o.SupplierId == id, cancellationToken) ? "satın alma siparişleri"
        : null;
}

public sealed record OrmeYarnDto(
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

public sealed record OrmeYarnRequest(
    string YarnCode,
    string Name,
    decimal CountValue,
    YarnCountSystem CountSystem = YarnCountSystem.Ne,
    int Plies = 1,
    string? Composition = null,
    string? Twist = null,
    string? Color = null,
    bool IsActive = true);

public sealed class OrmeYarnRequestValidator : AbstractValidator<OrmeYarnRequest>
{
    public OrmeYarnRequestValidator()
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

public sealed class OrmeYarnService(IAppDbContext db)
    : MasterDataService<OrmeYarn, OrmeYarnDto, OrmeYarnRequest>(db)
{
    protected override DbSet<OrmeYarn> Set => Db.Set<OrmeYarn>();
    protected override string Label => "İplik kartı";
    protected override string CodeField => "yarnCode";

    protected override string CodeOf(OrmeYarnRequest request) => request.YarnCode;
    protected override Expression<Func<OrmeYarn, bool>> WithCode(string code) => e => e.YarnCode == code;

    protected override Expression<Func<OrmeYarn, bool>> Matches(string term) =>
        e => e.YarnCode.Contains(term) || e.Name.Contains(term) || (e.Color != null && e.Color.Contains(term));

    protected override IQueryable<OrmeYarn> Order(IQueryable<OrmeYarn> query) => query.OrderBy(e => e.YarnCode);

    protected override Expression<Func<OrmeYarn, OrmeYarnDto>> ToDto => e => new OrmeYarnDto(
        e.Id, e.YarnCode, e.Name, e.CountValue, e.CountSystem, e.Plies, e.Composition, e.Twist, e.Color, e.IsActive);

    protected override Expression<Func<OrmeYarn, LookupDto>> ToLookup =>
        e => new LookupDto(e.Id, e.YarnCode, e.Name);

    protected override async Task<string?> FindUsageAsync(int id, CancellationToken cancellationToken) =>
        await Db.Set<OrmeFabricYarn>().AnyAsync(y => y.YarnId == id, cancellationToken) || await Db.Set<OrmeYarnLot>().AnyAsync(l => l.YarnId == id, cancellationToken)
            ? "kumaş kartları ve iplik girişleri"
            : await Db.Set<OrmePurchaseOrderLine>().AnyAsync(l => l.YarnId == id, cancellationToken) ? "satın alma siparişleri"
            : await Db.Set<OrmeQuotationLineYarn>().AnyAsync(l => l.YarnId == id, cancellationToken) ? "teklifler"
            : null;

    protected override void Apply(OrmeYarnRequest request, OrmeYarn entity, string code)
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

public sealed record OrmeMachineDto(
    int Id,
    string Code,
    string Name,
    KnitBedType BedType,
    int DiameterInch,
    int Gauge,
    int? NeedleCount,
    int? FeederCount,
    int? NominalSpeedRpm,
    string? Brand,
    bool IsActive);

public sealed record OrmeMachineRequest(
    string Code,
    string Name,
    int DiameterInch,
    int Gauge,
    KnitBedType BedType = KnitBedType.Single,
    int? NeedleCount = null,
    int? FeederCount = null,
    int? NominalSpeedRpm = null,
    string? Brand = null,
    bool IsActive = true);

public sealed class OrmeMachineRequestValidator : AbstractValidator<OrmeMachineRequest>
{
    public OrmeMachineRequestValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName(200);
        RuleFor(x => x.BedType).IsInEnum().WithMessage("Geçerli bir plaka yapısı seçin.");
        RuleFor(x => x.DiameterInch).InclusiveBetween(1, 100).WithMessage("Çap 1 ile 100 pus arasında olmalıdır.");
        RuleFor(x => x.Gauge).InclusiveBetween(1, 100).WithMessage("Fine 1 ile 100 arasında olmalıdır.");
        RuleFor(x => x.NeedleCount).InclusiveBetween(1, 20000).WithMessage("İğne sayısı 1 ile 20.000 arasında olmalıdır.").When(x => x.NeedleCount is not null);
        RuleFor(x => x.FeederCount).InclusiveBetween(1, 500).WithMessage("Sistem sayısı 1 ile 500 arasında olmalıdır.").When(x => x.FeederCount is not null);
        RuleFor(x => x.NominalSpeedRpm).InclusiveBetween(1, 200).WithMessage("Devir 1 ile 200 arasında olmalıdır.").When(x => x.NominalSpeedRpm is not null);
        RuleFor(x => x.Brand).OptionalText(100);
    }
}

public sealed class OrmeMachineService(IAppDbContext db) : MasterDataService<OrmeMachine, OrmeMachineDto, OrmeMachineRequest>(db)
{
    protected override DbSet<OrmeMachine> Set => Db.Set<OrmeMachine>();
    protected override string Label => "Örme makinesi";
    protected override string CodeField => "code";

    protected override string CodeOf(OrmeMachineRequest request) => request.Code;
    protected override Expression<Func<OrmeMachine, bool>> WithCode(string code) => e => e.Code == code;
    protected override Expression<Func<OrmeMachine, bool>> Matches(string term) =>
        e => e.Code.Contains(term) || e.Name.Contains(term) || (e.Brand != null && e.Brand.Contains(term));
    protected override IQueryable<OrmeMachine> Order(IQueryable<OrmeMachine> query) => query.OrderBy(e => e.Code);

    protected override Expression<Func<OrmeMachine, OrmeMachineDto>> ToDto => e => new OrmeMachineDto(
        e.Id, e.Code, e.Name, e.BedType, e.DiameterInch, e.Gauge, e.NeedleCount, e.FeederCount, e.NominalSpeedRpm, e.Brand, e.IsActive);

    protected override Expression<Func<OrmeMachine, LookupDto>> ToLookup => e => new LookupDto(e.Id, e.Code, e.Name);

    protected override async Task<string?> FindUsageAsync(int id, CancellationToken cancellationToken) =>
        await Db.Set<OrmeKnitOrderMachine>().AnyAsync(m => m.MachineId == id, cancellationToken) || await Db.Set<OrmeFabricRoll>().AnyAsync(r => r.MachineId == id, cancellationToken)
            || await Db.Set<OrmeKnitOrder>().AnyAsync(k => k.PlannedMachineId == id, cancellationToken)
            ? "örme emirleri ya da kapasite planı"
            : await Db.Set<OrmeTablet>().AnyAsync(t => t.MachineId == id, cancellationToken) || await Db.Set<OrmeOperatorMachine>().AnyAsync(m => m.MachineId == id, cancellationToken) || await Db.Set<OrmeMachineStop>().AnyAsync(s => s.MachineId == id, cancellationToken)
                ? "tabletler, operatörler ya da makine duruşları"
                : null;

    protected override void Apply(OrmeMachineRequest request, OrmeMachine entity, string code)
    {
        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.BedType = request.BedType;
        entity.DiameterInch = request.DiameterInch;
        entity.Gauge = request.Gauge;
        entity.NeedleCount = request.NeedleCount;
        entity.FeederCount = request.FeederCount;
        entity.NominalSpeedRpm = request.NominalSpeedRpm;
        entity.Brand = NullIfBlank(request.Brand);
        entity.IsActive = request.IsActive;
    }
}

/// <param name="YarnCount">İplik satırı sayısı.</param>
/// <param name="TotalPercent">İplik oranlarının toplamı; satır girildiyse 100'dür.</param>
public sealed record OrmeFabricDto(
    int Id,
    string FabricCode,
    string Name,
    string? KnitName,
    KnitBedType BedType,
    int? DiameterInch,
    int? Gauge,
    string? Composition,
    decimal? RawWidthCm,
    int RawWeightGsm,
    decimal? WastePercent,
    decimal? ExpectedKilogramsPerHour,
    int YarnCount,
    decimal TotalPercent,
    bool IsActive);

public sealed record OrmeFabricRequest(
    string FabricCode,
    string Name,
    int RawWeightGsm,
    string? KnitName = null,
    KnitBedType BedType = KnitBedType.Single,
    int? DiameterInch = null,
    int? Gauge = null,
    string? Composition = null,
    decimal? RawWidthCm = null,
    decimal? WastePercent = null,
    decimal? ExpectedKilogramsPerHour = null,
    bool IsActive = true);

public sealed class OrmeFabricRequestValidator : AbstractValidator<OrmeFabricRequest>
{
    public OrmeFabricRequestValidator()
    {
        RuleFor(x => x.FabricCode).ValidCode();
        RuleFor(x => x.Name).ValidName(200);
        RuleFor(x => x.KnitName).OptionalText(100);
        RuleFor(x => x.BedType).IsInEnum().WithMessage("Geçerli bir plaka yapısı seçin.");
        RuleFor(x => x.Composition).OptionalText(200);
        RuleFor(x => x.RawWeightGsm).InclusiveBetween(1, 5000).WithMessage("Ham gramaj 1 ile 5000 g/m² arasında olmalıdır.");
        RuleFor(x => x.DiameterInch).InclusiveBetween(1, 100).WithMessage("Çap 1 ile 100 pus arasında olmalıdır.").When(x => x.DiameterInch is not null);
        RuleFor(x => x.Gauge).InclusiveBetween(1, 100).WithMessage("Fine 1 ile 100 arasında olmalıdır.").When(x => x.Gauge is not null);
        RuleFor(x => x.RawWidthCm)
            .InclusiveBetween(1, 1000).WithMessage("Ham en 1 ile 1000 cm arasında olmalıdır.")
            .PrecisionScale(6, 2, true).WithMessage("Ham en en fazla iki ondalık olabilir.")
            .When(x => x.RawWidthCm is not null);
        RuleFor(x => x.WastePercent)
            .InclusiveBetween(0, 100).WithMessage("Fire 0 ile 100 arasında olmalıdır.")
            .PrecisionScale(5, 2, true).WithMessage("Fire en fazla iki ondalık olabilir.")
            .When(x => x.WastePercent is not null);
        RuleFor(x => x.ExpectedKilogramsPerHour)
            .GreaterThan(0).WithMessage("Beklenen üretim sıfırdan büyük olmalıdır.")
            .PrecisionScale(8, 2, true).WithMessage("Beklenen üretim en fazla iki ondalık olabilir.")
            .When(x => x.ExpectedKilogramsPerHour is not null);
    }
}

public sealed class OrmeFabricService(IAppDbContext db) : MasterDataService<OrmeFabric, OrmeFabricDto, OrmeFabricRequest>(db)
{
    protected override DbSet<OrmeFabric> Set => Db.Set<OrmeFabric>();
    protected override string Label => "Kumaş kartı";
    protected override string CodeField => "fabricCode";

    protected override string CodeOf(OrmeFabricRequest request) => request.FabricCode;
    protected override Expression<Func<OrmeFabric, bool>> WithCode(string code) => e => e.FabricCode == code;
    protected override Expression<Func<OrmeFabric, bool>> Matches(string term) =>
        e => e.FabricCode.Contains(term) || e.Name.Contains(term) || (e.KnitName != null && e.KnitName.Contains(term));
    protected override IQueryable<OrmeFabric> Order(IQueryable<OrmeFabric> query) => query.OrderBy(e => e.FabricCode);

    protected override Expression<Func<OrmeFabric, OrmeFabricDto>> ToDto => e => new OrmeFabricDto(
        e.Id, e.FabricCode, e.Name, e.KnitName, e.BedType, e.DiameterInch, e.Gauge, e.Composition, e.RawWidthCm, e.RawWeightGsm,
        e.WastePercent, e.ExpectedKilogramsPerHour, e.Yarns.Count, e.Yarns.Sum(y => (decimal?)y.Percent) ?? 0, e.IsActive);

    protected override Expression<Func<OrmeFabric, LookupDto>> ToLookup => e => new LookupDto(e.Id, e.FabricCode, e.Name);

    protected override void Apply(OrmeFabricRequest request, OrmeFabric entity, string code)
    {
        entity.FabricCode = code;
        entity.Name = request.Name.Trim();
        entity.KnitName = NullIfBlank(request.KnitName);
        entity.BedType = request.BedType;
        entity.DiameterInch = request.DiameterInch;
        entity.Gauge = request.Gauge;
        entity.Composition = NullIfBlank(request.Composition);
        entity.RawWidthCm = request.RawWidthCm;
        entity.RawWeightGsm = request.RawWeightGsm;
        entity.WastePercent = request.WastePercent;
        entity.ExpectedKilogramsPerHour = request.ExpectedKilogramsPerHour;
        entity.IsActive = request.IsActive;
    }

    protected override async Task<string?> FindUsageAsync(int id, CancellationToken cancellationToken) =>
        await Db.Set<OrmeOrderLine>().AnyAsync(l => l.FabricId == id, cancellationToken) ? "siparişler"
        : await Db.Set<OrmeQuotationLine>().AnyAsync(l => l.FabricId == id, cancellationToken) ? "teklifler"
        : null;
}

public sealed record OrmeFabricYarnDto(int Id, int Sequence, int YarnId, string YarnCode, string YarnName, string? YarnColor, decimal Percent, decimal? StitchLengthMm);

public sealed record OrmeFabricYarnsDto(int FabricId, IReadOnlyList<OrmeFabricYarnDto> Yarns);

public sealed record OrmeFabricYarnRequest(int YarnId, decimal Percent, decimal? StitchLengthMm = null);

/// <summary>Kumaşın iplik satırlarının tamamı, sırasıyla; gönderilmeyen satırlar silinir.</summary>
public sealed record SaveOrmeFabricYarnsRequest(IReadOnlyList<OrmeFabricYarnRequest> Yarns);

public sealed class SaveOrmeFabricYarnsRequestValidator : AbstractValidator<SaveOrmeFabricYarnsRequest>
{
    public const int MaxLines = 20;

    public SaveOrmeFabricYarnsRequestValidator()
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
                    if (line.YarnId <= 0)
                    {
                        context.AddFailure($"{i + 1}. satır: iplik seçin.");
                    }

                    if (line.Percent is <= 0 or > 100)
                    {
                        context.AddFailure($"{i + 1}. satır: oran 0'dan büyük, en fazla 100 olmalıdır.");
                    }

                    if (decimal.Round(line.Percent, 2) != line.Percent)
                    {
                        context.AddFailure($"{i + 1}. satır: oran en fazla iki ondalık olabilir.");
                    }

                    if (line.StitchLengthMm is <= 0 or > 100)
                    {
                        context.AddFailure($"{i + 1}. satır: ilmek iplik uzunluğu 0'dan büyük, en fazla 100 mm olmalıdır.");
                    }
                }

                if (list.Select(l => l.YarnId).Distinct().Count() != list.Count)
                {
                    context.AddFailure("Aynı iplik iki kez girilemez.");
                }

                // İhtiyaç hesabı oranlarla yapılır: toplam tam 100 olmalıdır.
                if (list.Count > 0 && list.Sum(l => l.Percent) != 100)
                {
                    context.AddFailure($"İplik oranlarının toplamı 100 olmalıdır; şu an {list.Sum(l => l.Percent):0.##}.");
                }
            });
    }
}

public interface IOrmeFabricYarnService
{
    Task<Result<OrmeFabricYarnsDto>> GetAsync(int fabricId, CancellationToken cancellationToken);
    Task<Result<OrmeFabricYarnsDto>> SaveAsync(int fabricId, SaveOrmeFabricYarnsRequest request, CancellationToken cancellationToken);
}

/// <summary>Kumaşın iplikleri ve oranları: liste bütün olarak kaydedilir, sıra gönderilen sıradır.</summary>
public sealed class OrmeFabricYarnService(IAppDbContext db) : IOrmeFabricYarnService
{
    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Kumaş kartı bulunamadı.", ErrorType.NotFound);

    public async Task<Result<OrmeFabricYarnsDto>> GetAsync(int fabricId, CancellationToken cancellationToken)
    {
        if (!await db.Set<OrmeFabric>().AnyAsync(f => f.Id == fabricId, cancellationToken))
        {
            return NotFound;
        }

        var yarns = await db.Set<OrmeFabricYarn>().AsNoTracking()
            .Where(y => y.FabricId == fabricId)
            .OrderBy(y => y.Sequence)
            .Select(y => new OrmeFabricYarnDto(y.Id, y.Sequence, y.YarnId, y.Yarn.YarnCode, y.Yarn.Name, y.Yarn.Color, y.Percent, y.StitchLengthMm))
            .ToListAsync(cancellationToken);

        return new OrmeFabricYarnsDto(fabricId, yarns);
    }

    public async Task<Result<OrmeFabricYarnsDto>> SaveAsync(int fabricId, SaveOrmeFabricYarnsRequest request, CancellationToken cancellationToken)
    {
        if (!await db.Set<OrmeFabric>().AnyAsync(f => f.Id == fabricId, cancellationToken))
        {
            return NotFound;
        }

        var set = db.Set<OrmeFabricYarn>();
        var existing = await set.Where(y => y.FabricId == fabricId).ToListAsync(cancellationToken);

        // Kullanılan kumaşın reçetesi değişirse geçmiş ihtiyaç ve fire hesapları bozulur: değişiklik için yeni kumaş kartı açılır.
        // Yalnızca taslak siparişte geçen kumaş serbestçe düzenlenir; reçetesi boş kartın doldurulması değişiklik sayılmaz.
        if (await IsInUseAsync(fabricId, cancellationToken))
        {
            if (request.Yarns.Count == 0)
            {
                return InUse("Kullanımdaki kumaşın en az bir iplik satırı olmalıdır.");
            }

            if (existing.Count > 0 && RecipeChanged(existing, request.Yarns))
            {
                return InUse("Bu kumaş onaylı siparişte ya da açık örme emrinde kullanılıyor; iplik, oran ve ilmek iplik uzunluğu değiştirilemez. "
                    + "Farklı reçete için yeni bir kumaş kartı açın.");
            }
        }

        // Kartta zaten bulunan pasif iplik kalabilir; yeni eklenen iplik etkin olmalıdır.
        var reference = await ReferenceValidation.RequireSelectableAsync(
            db.Set<OrmeYarn>(),
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

        var sequence = 0;
        foreach (var line in request.Yarns)
        {
            set.Add(new OrmeFabricYarn { FabricId = fabricId, Sequence = ++sequence, YarnId = line.YarnId, Percent = line.Percent, StitchLengthMm = line.StitchLengthMm });
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(fabricId, cancellationToken);
    }

    /// <summary>Kumaş taslak ve iptal dışındaki bir siparişte ya da tamamlanmamış, iptal edilmemiş bir örme emrinde geçiyor mu.</summary>
    private async Task<bool> IsInUseAsync(int fabricId, CancellationToken cancellationToken) =>
        await db.Set<OrmeOrderLine>().AnyAsync(
            l => l.FabricId == fabricId && l.Order.Status != OrderStatus.Draft && l.Order.Status != OrderStatus.Cancelled,
            cancellationToken)
        || await db.Set<OrmeKnitOrder>().AnyAsync(
            k => k.OrderLine.FabricId == fabricId && k.Status != KnitOrderStatus.Completed && k.Status != KnitOrderStatus.Cancelled,
            cancellationToken);

    /// <summary>İplik, oran ya da ilmek iplik uzunluğu değişti mi; yalnızca sıra değişikliği değişiklik sayılmaz.</summary>
    private static bool RecipeChanged(IReadOnlyCollection<OrmeFabricYarn> existing, IReadOnlyList<OrmeFabricYarnRequest> requested)
    {
        if (existing.Count != requested.Count || existing.Select(y => y.YarnId).Distinct().Count() != existing.Count)
        {
            return true;
        }

        // Uzunluk iki ondalıkla saklanır; aynı değerin fazla ondalıkla gelmesi değişiklik sayılmaz.
        var current = existing.ToDictionary(y => y.YarnId);
        return requested.Any(line =>
            !current.TryGetValue(line.YarnId, out var old)
            || old.Percent != line.Percent
            || old.StitchLengthMm != (line.StitchLengthMm is { } length ? (decimal?)decimal.Round(length, 2) : null));
    }

    private static Error InUse(string message) =>
        new(ErrorCodes.RecordInUse, message, ErrorType.Conflict, new Dictionary<string, string[]> { ["yarns"] = [message] });
}
