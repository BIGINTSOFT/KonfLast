using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Domain.Entities;
using ERP.Orme.Domain.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ERP.Orme.Application.Features.Quotations;

/// <param name="HourlyCost">Makineye özel saat maliyeti; boşsa genel makine saat maliyeti kullanılır.</param>
public sealed record OrmeMachineCostRateDto(int MachineId, string MachineCode, string MachineName, bool IsActive, decimal? HourlyCost);

/// <summary>Ön maliyet parametreleri; saatlik tutarlar <paramref name="Currency"/> (yerel para) cinsindendir.</summary>
/// <param name="Machines">Bütün makineler; özel saat maliyeti olmayanlarda boş.</param>
public sealed record OrmeCostSettingsDto(
    decimal MachineHourlyCost,
    decimal LaborHourlyCost,
    decimal OverheadPercent,
    decimal DefaultWastePercent,
    decimal DefaultMarginPercent,
    string Currency,
    IReadOnlyList<OrmeMachineCostRateDto> Machines);

public sealed record OrmeMachineCostRateRequest(int MachineId, decimal HourlyCost);

/// <param name="Machines">Makineye özel saat maliyetlerinin tamamı; listede olmayan makinenin özel maliyeti silinir.</param>
public sealed record OrmeCostSettingsRequest(
    decimal MachineHourlyCost,
    decimal LaborHourlyCost,
    decimal OverheadPercent,
    decimal DefaultWastePercent,
    decimal DefaultMarginPercent,
    IReadOnlyList<OrmeMachineCostRateRequest> Machines);

public sealed class OrmeCostSettingsRequestValidator : AbstractValidator<OrmeCostSettingsRequest>
{
    public OrmeCostSettingsRequestValidator()
    {
        RuleFor(x => x.MachineHourlyCost)
            .InclusiveBetween(0, 99_999_999).WithMessage("Makine saat maliyeti sıfır ya da büyük olmalıdır.")
            .PrecisionScale(18, 4, true).WithMessage("Makine saat maliyeti en fazla dört ondalık olabilir.");
        RuleFor(x => x.LaborHourlyCost)
            .InclusiveBetween(0, 99_999_999).WithMessage("İşçilik saat maliyeti sıfır ya da büyük olmalıdır.")
            .PrecisionScale(18, 4, true).WithMessage("İşçilik saat maliyeti en fazla dört ondalık olabilir.");
        RuleFor(x => x.OverheadPercent)
            .InclusiveBetween(0, 1000).WithMessage("Genel gider 0 ile 1000 arasında olmalıdır.")
            .PrecisionScale(7, 2, true).WithMessage("Genel gider en fazla iki ondalık olabilir.");
        RuleFor(x => x.DefaultWastePercent)
            .InclusiveBetween(0, 100).WithMessage("Fire 0 ile 100 arasında olmalıdır.")
            .PrecisionScale(5, 2, true).WithMessage("Fire en fazla iki ondalık olabilir.");
        RuleFor(x => x.DefaultMarginPercent)
            .InclusiveBetween(0, 1000).WithMessage("Kâr 0 ile 1000 arasında olmalıdır.")
            .PrecisionScale(7, 2, true).WithMessage("Kâr en fazla iki ondalık olabilir.");
        RuleFor(x => x.Machines)
            .NotNull().WithMessage("Makine listesi zorunludur.")
            .Must(list => list is null || list.Select(m => m.MachineId).Distinct().Count() == list.Count).WithMessage("Bir makine için tek saat maliyeti girilebilir.");
        RuleForEach(x => x.Machines).ChildRules(machine =>
        {
            machine.RuleFor(m => m.MachineId).GreaterThan(0).WithMessage("Makine seçilmelidir.");
            machine.RuleFor(m => m.HourlyCost)
                .InclusiveBetween(0, 99_999_999).WithMessage("Makine saat maliyeti sıfır ya da büyük olmalıdır.")
                .PrecisionScale(18, 4, true).WithMessage("Makine saat maliyeti en fazla dört ondalık olabilir.");
        });
    }
}

public interface IOrmeCostSettingsService
{
    Task<OrmeCostSettingsDto> GetAsync(CancellationToken cancellationToken);
    Task<Result<OrmeCostSettingsDto>> SaveAsync(OrmeCostSettingsRequest request, CancellationToken cancellationToken);
}

/// <summary>Ön maliyet parametreleri (tek kayıt) ve makineye özel saat maliyetleri. Değişiklik yalnızca yeni teklif satırlarına uygulanır.</summary>
public sealed class OrmeCostSettingsService(IAppDbContext db) : IOrmeCostSettingsService
{
    /// <summary>Kayıt yoksa varsayılan değerler.</summary>
    public static async Task<OrmeCostSettings> LoadAsync(IAppDbContext db, CancellationToken cancellationToken) =>
        await db.Set<OrmeCostSettings>().AsNoTracking().OrderBy(s => s.Id).FirstOrDefaultAsync(cancellationToken) ?? new OrmeCostSettings();

    public async Task<OrmeCostSettingsDto> GetAsync(CancellationToken cancellationToken)
    {
        var settings = await LoadAsync(db, cancellationToken);
        var rates = await db.Set<OrmeMachineCostRate>().AsNoTracking()
            .ToDictionaryAsync(r => r.MachineId, r => r.HourlyCost, cancellationToken);
        var machines = await db.Set<OrmeMachine>().AsNoTracking()
            .OrderBy(m => m.Code)
            .Select(m => new { m.Id, m.Code, m.Name, m.IsActive })
            .ToListAsync(cancellationToken);

        return new OrmeCostSettingsDto(
            settings.MachineHourlyCost,
            settings.LaborHourlyCost,
            settings.OverheadPercent,
            settings.DefaultWastePercent,
            settings.DefaultMarginPercent,
            Currency.LocalCode,
            machines.Select(m => new OrmeMachineCostRateDto(m.Id, m.Code, m.Name, m.IsActive, rates.TryGetValue(m.Id, out var cost) ? cost : null)).ToList());
    }

    public async Task<Result<OrmeCostSettingsDto>> SaveAsync(OrmeCostSettingsRequest request, CancellationToken cancellationToken)
    {
        var machineIds = request.Machines.Select(m => m.MachineId).Distinct().ToList();
        var known = await db.Set<OrmeMachine>().CountAsync(m => machineIds.Contains(m.Id), cancellationToken);
        if (known != machineIds.Count)
        {
            return ReferenceValidation.InvalidReference("machines", "Makine bulunamadı.");
        }

        var settings = await db.Set<OrmeCostSettings>().OrderBy(s => s.Id).FirstOrDefaultAsync(cancellationToken);
        if (settings is null)
        {
            settings = new OrmeCostSettings();
            db.Set<OrmeCostSettings>().Add(settings);
        }

        settings.MachineHourlyCost = request.MachineHourlyCost;
        settings.LaborHourlyCost = request.LaborHourlyCost;
        settings.OverheadPercent = request.OverheadPercent;
        settings.DefaultWastePercent = request.DefaultWastePercent;
        settings.DefaultMarginPercent = request.DefaultMarginPercent;

        var existing = await db.Set<OrmeMachineCostRate>().ToListAsync(cancellationToken);
        db.Set<OrmeMachineCostRate>().RemoveRange(existing.Where(r => machineIds.All(id => id != r.MachineId)));
        foreach (var item in request.Machines)
        {
            var rate = existing.SingleOrDefault(r => r.MachineId == item.MachineId);
            if (rate is null)
            {
                db.Set<OrmeMachineCostRate>().Add(new OrmeMachineCostRate { MachineId = item.MachineId, HourlyCost = item.HourlyCost });
            }
            else
            {
                rate.HourlyCost = item.HourlyCost;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(cancellationToken);
    }
}
