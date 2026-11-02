using System.Linq.Expressions;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Orme.Application.Features.Knitting;
using ERP.Orme.Domain.Entities;
using ERP.Orme.Domain.Enums;
using ERP.Orme.Domain.Rules;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Orme.Application.Features.Inspection;

public sealed record OrmeDefectTypeDto(int Id, string Code, string Name, bool AlwaysFourPoints, bool IsActive);

public sealed record OrmeDefectTypeRequest(string Code, string Name, bool AlwaysFourPoints = false, bool IsActive = true);

public sealed class OrmeDefectTypeRequestValidator : AbstractValidator<OrmeDefectTypeRequest>
{
    public OrmeDefectTypeRequestValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName(100);
    }
}

public sealed class OrmeDefectTypeService(IAppDbContext db) : MasterDataService<OrmeDefectType, OrmeDefectTypeDto, OrmeDefectTypeRequest>(db)
{
    protected override DbSet<OrmeDefectType> Set => Db.Set<OrmeDefectType>();
    protected override string Label => "Hata türü";
    protected override string CodeField => "code";

    protected override string CodeOf(OrmeDefectTypeRequest request) => request.Code;
    protected override Expression<Func<OrmeDefectType, bool>> WithCode(string code) => e => e.Code == code;
    protected override Expression<Func<OrmeDefectType, bool>> Matches(string term) => e => e.Code.Contains(term) || e.Name.Contains(term);
    protected override IQueryable<OrmeDefectType> Order(IQueryable<OrmeDefectType> query) => query.OrderBy(e => e.Code);
    protected override Expression<Func<OrmeDefectType, OrmeDefectTypeDto>> ToDto => e => new OrmeDefectTypeDto(e.Id, e.Code, e.Name, e.AlwaysFourPoints, e.IsActive);
    protected override Expression<Func<OrmeDefectType, LookupDto>> ToLookup => e => new LookupDto(e.Id, e.Code, e.Name);

    protected override void Apply(OrmeDefectTypeRequest request, OrmeDefectType entity, string code)
    {
        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.AlwaysFourPoints = request.AlwaysFourPoints;
        entity.IsActive = request.IsActive;
    }

    protected override async Task<string?> FindUsageAsync(int id, CancellationToken cancellationToken) =>
        await Db.Set<OrmeRollDefect>().AnyAsync(d => d.DefectTypeId == id, cancellationToken) ? "top kontrolleri" : null;
}

public sealed record OrmeRollDefectDto(int Id, int DefectTypeId, string DefectCode, string DefectName, decimal AtMeter, decimal LengthCm, int Points);

/// <param name="CardWidthCm">Kumaş kartındaki ham en; ölçümle karşılaştırılır.</param>
/// <param name="CardWeightGsm">Kumaş kartındaki ham gramaj; ölçümle karşılaştırılır.</param>
/// <param name="FirstQualityMaxPoints">Bu topa uygulanan 1. kalite üst sınırı (müşteriye özel ya da genel).</param>
/// <param name="SecondQualityMaxPoints">Bu topa uygulanan 2. kalite üst sınırı.</param>
public sealed record OrmeRollInspectionDto(
    OrmeFabricRollDto Roll,
    decimal? CardWidthCm,
    int CardWeightGsm,
    decimal FirstQualityMaxPoints,
    decimal SecondQualityMaxPoints,
    int? TotalPoints,
    FabricRollStatus? CalculatedStatus,
    string? InspectionNotes,
    IReadOnlyList<OrmeRollDefectDto> Defects);

public sealed record OrmeRollDefectRequest(int DefectTypeId, decimal AtMeter, decimal LengthCm);

/// <param name="Meters">Ölçüldüyse metre; boşsa makinede girilen metre, o da yoksa tartılan kilo, gramaj ve enden tahmin kullanılır.</param>
/// <param name="WidthCm">Ölçülen ham en; zorunlu.</param>
/// <param name="WeightGsm">Ölçülen ham gramaj; zorunlu.</param>
public sealed record SaveOrmeRollInspectionRequest(
    DateOnly InspectedOn,
    decimal WidthCm,
    decimal WeightGsm,
    IReadOnlyList<OrmeRollDefectRequest> Defects,
    decimal? Meters = null,
    string? Notes = null);

/// <param name="Status">1. kalite, 2. kalite ya da red.</param>
/// <param name="Note">Hesaplanan kaliteden farklı kararda zorunlu gerekçe.</param>
public sealed record OverrideOrmeRollGradeRequest(FabricRollStatus Status, string? Note = null);

/// <param name="WidthTolerancePercent">Ölçülen enin karttan sapma sınırı; aşılırsa uyarı çıkar.</param>
/// <param name="WeightTolerancePercent">Ölçülen gramajın karttan sapma sınırı; aşılırsa uyarı çıkar.</param>
public sealed record OrmeQualitySettingsDto(decimal FirstQualityMaxPoints, decimal SecondQualityMaxPoints, decimal WidthTolerancePercent, decimal WeightTolerancePercent);

public sealed class SaveOrmeRollInspectionRequestValidator : AbstractValidator<SaveOrmeRollInspectionRequest>
{
    public const int MaxDefects = 500;

    public SaveOrmeRollInspectionRequestValidator()
    {
        RuleFor(x => x.InspectedOn).NotEmpty().WithMessage("Kontrol tarihi zorunludur.");
        RuleFor(x => x.Meters)
            .GreaterThan(0).WithMessage("Ölçülen metre sıfırdan büyük olmalıdır.")
            .LessThanOrEqualTo(100_000).WithMessage("Ölçülen metre çok büyük.")
            .PrecisionScale(12, 2, true).WithMessage("Metre en fazla iki ondalık olabilir.")
            .When(x => x.Meters is not null);
        RuleFor(x => x.WidthCm)
            .InclusiveBetween(1, 1000).WithMessage("En 1 ile 1000 cm arasında olmalıdır.")
            .PrecisionScale(6, 2, true).WithMessage("En en fazla iki ondalık olabilir.");
        RuleFor(x => x.WeightGsm)
            .InclusiveBetween(1, 2000).WithMessage("Gramaj 1 ile 2000 g/m² arasında olmalıdır.")
            .PrecisionScale(7, 2, true).WithMessage("Gramaj en fazla iki ondalık olabilir.");
        RuleFor(x => x.Notes).OptionalText(500);
        RuleFor(x => x.Defects)
            .NotNull().WithMessage("Hata listesi zorunludur.")
            .Must(list => list.Count <= MaxDefects).WithMessage($"En fazla {MaxDefects} hata girilebilir.")
            .Custom((list, context) =>
            {
                if (list is null)
                {
                    return;
                }

                for (var i = 0; i < list.Count; i++)
                {
                    var defect = list[i];
                    if (defect.DefectTypeId <= 0)
                    {
                        context.AddFailure($"{i + 1}. hata: hata türü seçin.");
                    }

                    if (defect.AtMeter is < 0 or > 100_000)
                    {
                        context.AddFailure($"{i + 1}. hata: yeri sıfır ya da büyük olmalıdır.");
                    }

                    if (defect.LengthCm is <= 0 or > 100_000)
                    {
                        context.AddFailure($"{i + 1}. hata: uzunluk sıfırdan büyük olmalıdır.");
                    }
                }
            });
    }
}

public sealed class OverrideOrmeRollGradeRequestValidator : AbstractValidator<OverrideOrmeRollGradeRequest>
{
    public OverrideOrmeRollGradeRequestValidator()
    {
        RuleFor(x => x.Status)
            .Must(s => s is FabricRollStatus.FirstQuality or FabricRollStatus.SecondQuality or FabricRollStatus.Rejected)
            .WithMessage("Karar 1. kalite, 2. kalite ya da red olmalıdır.");
        RuleFor(x => x.Note).OptionalText(500);
    }
}

public sealed class OrmeQualitySettingsDtoValidator : AbstractValidator<OrmeQualitySettingsDto>
{
    public OrmeQualitySettingsDtoValidator()
    {
        RuleFor(x => x.FirstQualityMaxPoints)
            .InclusiveBetween(0, 100_000).WithMessage("1. kalite sınırı sıfır ya da büyük olmalıdır.")
            .PrecisionScale(8, 2, true).WithMessage("Sınır en fazla iki ondalık olabilir.");
        RuleFor(x => x.SecondQualityMaxPoints)
            .GreaterThanOrEqualTo(x => x.FirstQualityMaxPoints).WithMessage("2. kalite sınırı 1. kalite sınırından küçük olamaz.")
            .LessThanOrEqualTo(100_000).WithMessage("2. kalite sınırı çok büyük.")
            .PrecisionScale(8, 2, true).WithMessage("Sınır en fazla iki ondalık olabilir.");
        RuleFor(x => x.WidthTolerancePercent)
            .InclusiveBetween(0, 100).WithMessage("En toleransı 0 ile 100 arasında olmalıdır.")
            .PrecisionScale(5, 2, true).WithMessage("Tolerans en fazla iki ondalık olabilir.");
        RuleFor(x => x.WeightTolerancePercent)
            .InclusiveBetween(0, 100).WithMessage("Gramaj toleransı 0 ile 100 arasında olmalıdır.")
            .PrecisionScale(5, 2, true).WithMessage("Tolerans en fazla iki ondalık olabilir.");
    }
}

public interface IOrmeInspectionService
{
    Task<Result<OrmeRollInspectionDto>> GetAsync(int rollId, CancellationToken cancellationToken);
    Task<Result<OrmeRollInspectionDto>> SaveAsync(int rollId, SaveOrmeRollInspectionRequest request, CancellationToken cancellationToken);
    Task<Result<OrmeRollInspectionDto>> OverrideAsync(int rollId, OverrideOrmeRollGradeRequest request, CancellationToken cancellationToken);
    Task<OrmeQualitySettingsDto> GetSettingsAsync(CancellationToken cancellationToken);
    Task<OrmeQualitySettingsDto> SaveSettingsAsync(OrmeQualitySettingsDto request, CancellationToken cancellationToken);
}

/// <summary>
/// Ham kontrol (4 puan): hatalar kaydedilir, puan ve kalite sunucuda hesaplanır. Metre ölçülmediyse tahmin kullanılır ve işaretlenir.
/// En ve gramajın kumaş kartından sapması uyarıdır, kaliteyi değiştirmez; tartılan kilo değişmez. Yeniden kontrol önceki hataları ve
/// yetkili kararını siler.
/// </summary>
public sealed class OrmeInspectionService(IAppDbContext db) : IOrmeInspectionService
{
    private const string InvalidState = "ORME_INSPECTION_INVALID_STATE";
    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Top bulunamadı.", ErrorType.NotFound);
    private static readonly Error InPackingList = new(InvalidState, "Çeki listesindeki topun kontrolü ve kalitesi değiştirilemez; önce listeden çıkarın.", ErrorType.Conflict);

    private Task<bool> InPackingListAsync(int rollId, CancellationToken cancellationToken) =>
        db.Set<OrmePackingItem>().AnyAsync(i => i.RollId == rollId && i.IsActive, cancellationToken);

    public async Task<Result<OrmeRollInspectionDto>> GetAsync(int rollId, CancellationToken cancellationToken)
    {
        var roll = await db.Set<OrmeFabricRoll>().AsNoTracking()
            .Include(r => r.KnitOrder).ThenInclude(k => k.OrderLine).ThenInclude(l => l.Fabric)
            .Include(r => r.KnitOrder).ThenInclude(k => k.OrderLine).ThenInclude(l => l.Order).ThenInclude(o => o.Customer)
            .Include(r => r.Defects).ThenInclude(d => d.DefectType)
            .SingleOrDefaultAsync(r => r.Id == rollId, cancellationToken);
        if (roll is null)
        {
            return NotFound;
        }

        var settings = await GetSettingsAsync(cancellationToken);
        var customer = roll.KnitOrder.OrderLine.Order.Customer;
        var fabric = roll.KnitOrder.OrderLine.Fabric;
        var dto = await OrmeKnitOrderService.ProjectRolls(db.Set<OrmeFabricRoll>().AsNoTracking().Where(r => r.Id == rollId)).SingleAsync(cancellationToken);
        var limits = QualityLimits(customer, settings);

        return new OrmeRollInspectionDto(
            dto,
            fabric.RawWidthCm,
            fabric.RawWeightGsm,
            limits.First,
            limits.Second,
            roll.TotalPoints,
            roll.CalculatedStatus,
            roll.InspectionNotes,
            roll.Defects.OrderBy(d => d.AtMeter).ThenBy(d => d.Id)
                .Select(d => new OrmeRollDefectDto(d.Id, d.DefectTypeId, d.DefectType.Code, d.DefectType.Name, d.AtMeter, d.LengthCm, d.Points))
                .ToList());
    }

    public async Task<Result<OrmeRollInspectionDto>> SaveAsync(int rollId, SaveOrmeRollInspectionRequest request, CancellationToken cancellationToken)
    {
        var roll = await db.Set<OrmeFabricRoll>()
            .Include(r => r.Defects)
            .Include(r => r.KnitOrder).ThenInclude(k => k.OrderLine).ThenInclude(l => l.Fabric)
            .Include(r => r.KnitOrder).ThenInclude(k => k.OrderLine).ThenInclude(l => l.Order).ThenInclude(o => o.Customer)
            .SingleOrDefaultAsync(r => r.Id == rollId, cancellationToken);
        if (roll is null)
        {
            return NotFound;
        }

        if (await InPackingListAsync(rollId, cancellationToken))
        {
            return InPackingList;
        }

        if (request.InspectedOn < roll.CutOn)
        {
            return ReferenceValidation.InvalidReference("inspectedOn", "Kontrol tarihi top tarihinden önce olamaz.");
        }

        // Ölçülen metre, yoksa makinede girilen metre, o da yoksa tahmin. Tahminde hatanın yeri metreyle sınırlanmaz.
        var knownMeters = request.Meters ?? roll.Meters;
        var meters = knownMeters ?? FourPointSystem.EstimatedMeters(roll.Kilograms, request.WeightGsm, request.WidthCm);
        if (knownMeters is { } limit && request.Defects.FirstOrDefault(d => d.AtMeter > limit) is { } outside)
        {
            return ReferenceValidation.InvalidReference("defects", $"{outside.AtMeter:0.##}. metredeki hata topun metresini ({limit:0.##}) aşıyor.");
        }

        var typeIds = request.Defects.Select(d => d.DefectTypeId).Distinct().ToList();
        if (await ReferenceValidation.RequireSelectableAsync(
                db.Set<OrmeDefectType>(), typeIds, roll.Defects.Select(d => d.DefectTypeId).ToHashSet(), "defects", "Hata türü", cancellationToken) is { } error)
        {
            return error;
        }

        var fourPointTypes = await db.Set<OrmeDefectType>().Where(t => typeIds.Contains(t.Id) && t.AlwaysFourPoints).Select(t => t.Id).ToListAsync(cancellationToken);
        var defects = request.Defects.Select(d => new OrmeRollDefect
        {
            RollId = rollId,
            DefectTypeId = d.DefectTypeId,
            AtMeter = d.AtMeter,
            LengthCm = d.LengthCm,
            Points = FourPointSystem.PointsFor(d.LengthCm, fourPointTypes.Contains(d.DefectTypeId)),
        }).ToList();

        var settings = await GetSettingsAsync(cancellationToken);
        var customer = roll.KnitOrder.OrderLine.Order.Customer;
        var fabric = roll.KnitOrder.OrderLine.Fabric;
        var total = FourPointSystem.TotalPoints(defects.Select(d => (d.AtMeter, d.Points)));
        var score = FourPointSystem.PointsPer100SquareMeters(total, meters, request.WidthCm);
        var limits = QualityLimits(customer, settings);
        var grade = FourPointSystem.Grade(score, limits.First, limits.Second);

        db.Set<OrmeRollDefect>().RemoveRange(roll.Defects);
        db.Set<OrmeRollDefect>().AddRange(defects);
        roll.InspectedOn = request.InspectedOn;
        roll.InspectedMeters = meters;
        roll.InspectedMetersEstimated = knownMeters is null;
        roll.InspectedWidthCm = request.WidthCm;
        roll.InspectedWeightGsm = request.WeightGsm;
        roll.SpecDeviation = FourPointSystem.SpecDeviation(
            request.WidthCm, fabric.RawWidthCm, request.WeightGsm, fabric.RawWeightGsm, settings.WidthTolerancePercent, settings.WeightTolerancePercent);
        roll.TotalPoints = total;
        roll.PointsPer100M2 = score;
        roll.CalculatedStatus = grade;
        roll.Status = grade;
        roll.OverrideNote = null;
        roll.InspectionNotes = NullIfBlank(request.Notes);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(rollId, cancellationToken);
    }

    /// <summary>Yetkilinin kalite kararı: hesaplanandan farklıysa gerekçe zorunlu; hesaplanana dönüşte gerekçe silinir.</summary>
    public async Task<Result<OrmeRollInspectionDto>> OverrideAsync(int rollId, OverrideOrmeRollGradeRequest request, CancellationToken cancellationToken)
    {
        var roll = await db.Set<OrmeFabricRoll>().SingleOrDefaultAsync(r => r.Id == rollId, cancellationToken);
        if (roll is null)
        {
            return NotFound;
        }

        if (await InPackingListAsync(rollId, cancellationToken))
        {
            return InPackingList;
        }

        if (roll.CalculatedStatus is null)
        {
            return new Error(InvalidState, "Kontrol edilmemiş topun kalitesi değiştirilemez.", ErrorType.Conflict);
        }

        var differs = request.Status != roll.CalculatedStatus;
        if (differs && NullIfBlank(request.Note) is null)
        {
            return ReferenceValidation.InvalidReference("note", "Hesaplanan kaliteden farklı kararda gerekçe zorunludur.");
        }

        roll.Status = request.Status;
        roll.OverrideNote = differs ? NullIfBlank(request.Note) : null;
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(rollId, cancellationToken);
    }

    public async Task<OrmeQualitySettingsDto> GetSettingsAsync(CancellationToken cancellationToken)
    {
        var settings = await db.Set<OrmeQualitySettings>().AsNoTracking().OrderBy(s => s.Id).FirstOrDefaultAsync(cancellationToken) ?? new OrmeQualitySettings();
        return new OrmeQualitySettingsDto(settings.FirstQualityMaxPoints, settings.SecondQualityMaxPoints, settings.WidthTolerancePercent, settings.WeightTolerancePercent);
    }

    public async Task<OrmeQualitySettingsDto> SaveSettingsAsync(OrmeQualitySettingsDto request, CancellationToken cancellationToken)
    {
        var settings = await db.Set<OrmeQualitySettings>().OrderBy(s => s.Id).FirstOrDefaultAsync(cancellationToken);
        if (settings is null)
        {
            settings = new OrmeQualitySettings();
            db.Set<OrmeQualitySettings>().Add(settings);
        }

        settings.FirstQualityMaxPoints = request.FirstQualityMaxPoints;
        settings.SecondQualityMaxPoints = request.SecondQualityMaxPoints;
        settings.WidthTolerancePercent = request.WidthTolerancePercent;
        settings.WeightTolerancePercent = request.WeightTolerancePercent;
        await db.SaveChangesAsync(cancellationToken);
        return request;
    }

    /// <summary>
    /// Topa uygulanan puan sınırları: müşteri kartında iki sınır da girilmişse müşterinin, yoksa genel sınırlar.
    /// Sınırlar tek tek karıştırılmaz; yalnız biri girilmiş eski müşteri kaydı genel sınırlarla değerlendirilir.
    /// </summary>
    private static (decimal First, decimal Second) QualityLimits(OrmeCustomer? customer, OrmeQualitySettingsDto settings) =>
        customer is { FirstQualityMaxPoints: { } first, SecondQualityMaxPoints: { } second }
            ? (first, second)
            : (settings.FirstQualityMaxPoints, settings.SecondQualityMaxPoints);
}
