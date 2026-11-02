using System.Linq.Expressions;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Dokuma.Application.Features.Weaving;
using ERP.Dokuma.Domain.Entities;
using ERP.Dokuma.Domain.Enums;
using ERP.Dokuma.Domain.Rules;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Dokuma.Application.Features.Inspection;

public sealed record DokumaDefectTypeDto(int Id, string Code, string Name, bool AlwaysFourPoints, bool IsActive);

public sealed record DokumaDefectTypeRequest(string Code, string Name, bool AlwaysFourPoints = false, bool IsActive = true);

public sealed class DokumaDefectTypeRequestValidator : AbstractValidator<DokumaDefectTypeRequest>
{
    public DokumaDefectTypeRequestValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName(100);
    }
}

public sealed class DokumaDefectTypeService(IAppDbContext db) : MasterDataService<DokumaDefectType, DokumaDefectTypeDto, DokumaDefectTypeRequest>(db)
{
    protected override DbSet<DokumaDefectType> Set => Db.Set<DokumaDefectType>();
    protected override string Label => "Hata türü";
    protected override string CodeField => "code";

    protected override string CodeOf(DokumaDefectTypeRequest request) => request.Code;
    protected override Expression<Func<DokumaDefectType, bool>> WithCode(string code) => e => e.Code == code;
    protected override Expression<Func<DokumaDefectType, bool>> Matches(string term) => e => e.Code.Contains(term) || e.Name.Contains(term);
    protected override IQueryable<DokumaDefectType> Order(IQueryable<DokumaDefectType> query) => query.OrderBy(e => e.Code);
    protected override Expression<Func<DokumaDefectType, DokumaDefectTypeDto>> ToDto => e => new DokumaDefectTypeDto(e.Id, e.Code, e.Name, e.AlwaysFourPoints, e.IsActive);
    protected override Expression<Func<DokumaDefectType, LookupDto>> ToLookup => e => new LookupDto(e.Id, e.Code, e.Name);

    protected override void Apply(DokumaDefectTypeRequest request, DokumaDefectType entity, string code)
    {
        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.AlwaysFourPoints = request.AlwaysFourPoints;
        entity.IsActive = request.IsActive;
    }

    protected override async Task<string?> FindUsageAsync(int id, CancellationToken cancellationToken) =>
        await Db.Set<DokumaRollDefect>().AnyAsync(d => d.DefectTypeId == id, cancellationToken) ? "top kontrolleri" : null;
}

public sealed record DokumaRollDefectDto(int Id, int DefectTypeId, string DefectCode, string DefectName, decimal AtMeter, decimal LengthCm, int Points);

/// <param name="RawWidthCm">Artikelin ham eni; kontrol formuna önerilir.</param>
/// <param name="FirstQualityMaxPoints">Bu topa uygulanan 1. kalite üst sınırı (müşteriye özel ya da genel).</param>
/// <param name="SecondQualityMaxPoints">Bu topa uygulanan 2. kalite üst sınırı.</param>
public sealed record DokumaRollInspectionDto(
    DokumaFabricRollDto Roll,
    decimal RawWidthCm,
    decimal FirstQualityMaxPoints,
    decimal SecondQualityMaxPoints,
    int? TotalPoints,
    FabricRollStatus? CalculatedStatus,
    string? OverrideNote,
    string? InspectionNotes,
    IReadOnlyList<DokumaRollDefectDto> Defects);

public sealed record DokumaRollDefectRequest(int DefectTypeId, decimal AtMeter, decimal LengthCm);

public sealed record SaveDokumaRollInspectionRequest(DateOnly InspectedOn, decimal Meters, decimal WidthCm, IReadOnlyList<DokumaRollDefectRequest> Defects, string? Notes = null);

/// <param name="Status">1. kalite, 2. kalite ya da red.</param>
/// <param name="Note">Hesaplanan kaliteden farklı kararda zorunlu gerekçe.</param>
public sealed record OverrideDokumaRollGradeRequest(FabricRollStatus Status, string? Note = null);

public sealed record DokumaQualitySettingsDto(decimal FirstQualityMaxPoints, decimal SecondQualityMaxPoints);

public sealed class SaveDokumaRollInspectionRequestValidator : AbstractValidator<SaveDokumaRollInspectionRequest>
{
    public const int MaxDefects = 500;

    public SaveDokumaRollInspectionRequestValidator()
    {
        RuleFor(x => x.InspectedOn).NotEmpty().WithMessage("Kontrol tarihi zorunludur.");
        RuleFor(x => x.Meters)
            .GreaterThan(0).WithMessage("Ölçülen metre sıfırdan büyük olmalıdır.")
            .LessThanOrEqualTo(100_000).WithMessage("Ölçülen metre çok büyük.")
            .PrecisionScale(12, 2, true).WithMessage("Metre en fazla iki ondalık olabilir.");
        RuleFor(x => x.WidthCm)
            .InclusiveBetween(1, 1000).WithMessage("En 1 ile 1000 cm arasında olmalıdır.")
            .PrecisionScale(6, 2, true).WithMessage("En en fazla iki ondalık olabilir.");
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

                var meters = context.InstanceToValidate.Meters;
                for (var i = 0; i < list.Count; i++)
                {
                    var defect = list[i];
                    if (defect.DefectTypeId <= 0)
                    {
                        context.AddFailure($"{i + 1}. hata: hata türü seçin.");
                    }

                    if (defect.AtMeter < 0 || defect.AtMeter > meters)
                    {
                        context.AddFailure($"{i + 1}. hata: yeri 0 ile ölçülen metre arasında olmalıdır.");
                    }

                    if (defect.LengthCm is <= 0 or > 100_000)
                    {
                        context.AddFailure($"{i + 1}. hata: uzunluk sıfırdan büyük olmalıdır.");
                    }
                }
            });
    }
}

public sealed class OverrideDokumaRollGradeRequestValidator : AbstractValidator<OverrideDokumaRollGradeRequest>
{
    public OverrideDokumaRollGradeRequestValidator()
    {
        RuleFor(x => x.Status)
            .Must(s => s is FabricRollStatus.FirstQuality or FabricRollStatus.SecondQuality or FabricRollStatus.Rejected)
            .WithMessage("Karar 1. kalite, 2. kalite ya da red olmalıdır.");
        RuleFor(x => x.Note).OptionalText(500);
    }
}

public sealed class DokumaQualitySettingsDtoValidator : AbstractValidator<DokumaQualitySettingsDto>
{
    public DokumaQualitySettingsDtoValidator()
    {
        RuleFor(x => x.FirstQualityMaxPoints)
            .InclusiveBetween(0, 100_000).WithMessage("1. kalite sınırı sıfır ya da büyük olmalıdır.")
            .PrecisionScale(8, 2, true).WithMessage("Sınır en fazla iki ondalık olabilir.");
        RuleFor(x => x.SecondQualityMaxPoints)
            .GreaterThanOrEqualTo(x => x.FirstQualityMaxPoints).WithMessage("2. kalite sınırı 1. kalite sınırından küçük olamaz.")
            .LessThanOrEqualTo(100_000).WithMessage("2. kalite sınırı çok büyük.")
            .PrecisionScale(8, 2, true).WithMessage("Sınır en fazla iki ondalık olabilir.");
    }
}

public interface IDokumaInspectionService
{
    Task<Result<DokumaRollInspectionDto>> GetAsync(int rollId, CancellationToken cancellationToken);
    Task<Result<DokumaRollInspectionDto>> SaveAsync(int rollId, SaveDokumaRollInspectionRequest request, CancellationToken cancellationToken);
    Task<Result<DokumaRollInspectionDto>> OverrideAsync(int rollId, OverrideDokumaRollGradeRequest request, CancellationToken cancellationToken);
    Task<DokumaQualitySettingsDto> GetSettingsAsync(CancellationToken cancellationToken);
    Task<DokumaQualitySettingsDto> SaveSettingsAsync(DokumaQualitySettingsDto request, CancellationToken cancellationToken);
}

/// <summary>
/// Ham kontrol (4 puan): hatalar kaydedilir, puan ve kalite sunucuda hesaplanır. Kontrol yeniden yapılabilir; yeniden
/// kontrol önceki hataları ve yetkili kararını siler.
/// </summary>
public sealed class DokumaInspectionService(IAppDbContext db) : IDokumaInspectionService
{
    private const string InvalidState = "DOKUMA_INSPECTION_INVALID_STATE";
    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Top bulunamadı.", ErrorType.NotFound);
    private static readonly Error InPackingList = new(InvalidState, "Çeki listesindeki topun kontrolü ve kalitesi değiştirilemez; önce listeden çıkarın.", ErrorType.Conflict);

    private Task<bool> InPackingListAsync(int rollId, CancellationToken cancellationToken) =>
        db.Set<DokumaPackingItem>().AnyAsync(i => i.RollId == rollId && i.IsActive, cancellationToken);

    public async Task<Result<DokumaRollInspectionDto>> GetAsync(int rollId, CancellationToken cancellationToken)
    {
        var roll = await db.Set<DokumaFabricRoll>().AsNoTracking()
            .Include(r => r.Beam).ThenInclude(b => b.WarpOrder).ThenInclude(w => w.OrderLine).ThenInclude(l => l.Article)
            .Include(r => r.Beam).ThenInclude(b => b.WarpOrder).ThenInclude(w => w.OrderLine).ThenInclude(l => l.Order).ThenInclude(o => o.Customer)
            .Include(r => r.Defects).ThenInclude(d => d.DefectType)
            .SingleOrDefaultAsync(r => r.Id == rollId, cancellationToken);
        if (roll is null)
        {
            return NotFound;
        }

        var (first, second) = await LimitsAsync(roll.Beam.WarpOrder.OrderLine.Order.Customer, cancellationToken);
        var dto = await DokumaWeavingService.ProjectRolls(db.Set<DokumaFabricRoll>().AsNoTracking().Where(r => r.Id == rollId)).SingleAsync(cancellationToken);

        return new DokumaRollInspectionDto(
            dto,
            roll.Beam.WarpOrder.OrderLine.Article.RawWidthCm,
            first,
            second,
            roll.TotalPoints,
            roll.CalculatedStatus,
            roll.OverrideNote,
            roll.InspectionNotes,
            roll.Defects.OrderBy(d => d.AtMeter).ThenBy(d => d.Id)
                .Select(d => new DokumaRollDefectDto(d.Id, d.DefectTypeId, d.DefectType.Code, d.DefectType.Name, d.AtMeter, d.LengthCm, d.Points))
                .ToList());
    }

    public async Task<Result<DokumaRollInspectionDto>> SaveAsync(int rollId, SaveDokumaRollInspectionRequest request, CancellationToken cancellationToken)
    {
        var roll = await db.Set<DokumaFabricRoll>()
            .Include(r => r.Defects)
            .Include(r => r.Beam).ThenInclude(b => b.WarpOrder).ThenInclude(w => w.OrderLine).ThenInclude(l => l.Order).ThenInclude(o => o.Customer)
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
            return ReferenceValidation.InvalidReference("inspectedOn", "Kontrol tarihi kesim tarihinden önce olamaz.");
        }

        var typeIds = request.Defects.Select(d => d.DefectTypeId).Distinct().ToList();
        if (await ReferenceValidation.RequireSelectableAsync(
                db.Set<DokumaDefectType>(), typeIds, roll.Defects.Select(d => d.DefectTypeId).ToHashSet(), "defects", "Hata türü", cancellationToken) is { } error)
        {
            return error;
        }

        var fourPointTypes = await db.Set<DokumaDefectType>().Where(t => typeIds.Contains(t.Id) && t.AlwaysFourPoints).Select(t => t.Id).ToListAsync(cancellationToken);
        var defects = request.Defects.Select(d => new DokumaRollDefect
        {
            RollId = rollId,
            DefectTypeId = d.DefectTypeId,
            AtMeter = d.AtMeter,
            LengthCm = d.LengthCm,
            Points = FourPointSystem.PointsFor(d.LengthCm, fourPointTypes.Contains(d.DefectTypeId)),
        }).ToList();

        var total = FourPointSystem.TotalPoints(defects.Select(d => (d.AtMeter, d.Points)));
        var score = FourPointSystem.PointsPer100SquareMeters(total, request.Meters, request.WidthCm);
        var (first, second) = await LimitsAsync(roll.Beam.WarpOrder.OrderLine.Order.Customer, cancellationToken);
        var grade = FourPointSystem.Grade(score, first, second);

        db.Set<DokumaRollDefect>().RemoveRange(roll.Defects);
        db.Set<DokumaRollDefect>().AddRange(defects);
        roll.InspectedOn = request.InspectedOn;
        roll.InspectedMeters = request.Meters;
        roll.InspectedWidthCm = request.WidthCm;
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
    public async Task<Result<DokumaRollInspectionDto>> OverrideAsync(int rollId, OverrideDokumaRollGradeRequest request, CancellationToken cancellationToken)
    {
        var roll = await db.Set<DokumaFabricRoll>().SingleOrDefaultAsync(r => r.Id == rollId, cancellationToken);
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

    public async Task<DokumaQualitySettingsDto> GetSettingsAsync(CancellationToken cancellationToken)
    {
        var settings = await db.Set<DokumaQualitySettings>().AsNoTracking().OrderBy(s => s.Id).FirstOrDefaultAsync(cancellationToken) ?? new DokumaQualitySettings();
        return new DokumaQualitySettingsDto(settings.FirstQualityMaxPoints, settings.SecondQualityMaxPoints);
    }

    public async Task<DokumaQualitySettingsDto> SaveSettingsAsync(DokumaQualitySettingsDto request, CancellationToken cancellationToken)
    {
        var settings = await db.Set<DokumaQualitySettings>().OrderBy(s => s.Id).FirstOrDefaultAsync(cancellationToken);
        if (settings is null)
        {
            settings = new DokumaQualitySettings();
            db.Set<DokumaQualitySettings>().Add(settings);
        }

        settings.FirstQualityMaxPoints = request.FirstQualityMaxPoints;
        settings.SecondQualityMaxPoints = request.SecondQualityMaxPoints;
        await db.SaveChangesAsync(cancellationToken);
        return request;
    }

    /// <summary>
    /// Sınırlar bir çift olarak değerlendirilir: müşteride iki sınır da girilmişse müşterinin çifti, değilse genel çift.
    /// Tek tek karıştırmak 1. sınırın 2. sınırdan büyük olduğu tutarsız bir çift doğurabilir.
    /// </summary>
    private async Task<(decimal First, decimal Second)> LimitsAsync(DokumaCustomer? customer, CancellationToken cancellationToken)
    {
        if (customer is { FirstQualityMaxPoints: { } first, SecondQualityMaxPoints: { } second })
        {
            return (first, second);
        }

        var settings = await GetSettingsAsync(cancellationToken);
        return (settings.FirstQualityMaxPoints, settings.SecondQualityMaxPoints);
    }
}
