using ERP.Boyahane.Domain.Entities;
using ERP.Boyahane.Domain.Enums;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ERP.Boyahane.Application.Features.Quotations;

/// <summary>Makinenin saat maliyeti; <paramref name="HourlyCost"/> boşsa varsayılan saat maliyeti kullanılır.</summary>
public sealed record BoyahaneMachineCostRateDto(int MachineId, string Code, string Name, DyeMachineType MachineType, decimal CapacityKg, bool IsActive, decimal? HourlyCost);

/// <summary>Ön maliyet ve planlama parametreleri. Tutarlar <paramref name="Currency"/> para birimindedir.</summary>
/// <param name="IsDefault">Parametreler henüz kaydedilmedi; gösterilen değerler varsayılandır.</param>
public sealed record BoyahaneCostSettingsDto(
    string Currency,
    decimal DefaultMachineHourlyCost,
    decimal LaborCostPerMinute,
    decimal DefaultLiquorRatio,
    decimal DefaultBatchKilograms,
    decimal OverheadPercent,
    decimal FirePercent,
    decimal MarginPercent,
    ChemicalCostMethod ChemicalCostMethod,
    decimal FabricDyeingMinutes,
    decimal YarnDyeingMinutes,
    decimal FinishingMinutes,
    decimal PrintingMinutes,
    bool IsDefault,
    DateTime? UpdatedAt,
    string? UpdatedBy,
    IReadOnlyList<BoyahaneMachineCostRateDto> Machines);

/// <param name="HourlyCost">Boşsa makinenin özel maliyeti kaldırılır, varsayılan kullanılır.</param>
public sealed record BoyahaneMachineCostRateRequest(int MachineId, decimal? HourlyCost);

public sealed record SaveBoyahaneCostSettingsRequest(
    string Currency,
    decimal DefaultMachineHourlyCost,
    decimal LaborCostPerMinute,
    decimal DefaultLiquorRatio,
    decimal DefaultBatchKilograms,
    decimal OverheadPercent,
    decimal FirePercent,
    decimal MarginPercent,
    ChemicalCostMethod ChemicalCostMethod,
    decimal FabricDyeingMinutes,
    decimal YarnDyeingMinutes,
    decimal FinishingMinutes,
    decimal PrintingMinutes,
    IReadOnlyList<BoyahaneMachineCostRateRequest>? MachineRates = null);

public sealed class SaveBoyahaneCostSettingsRequestValidator : AbstractValidator<SaveBoyahaneCostSettingsRequest>
{
    public SaveBoyahaneCostSettingsRequestValidator()
    {
        RuleFor(x => x.Currency).NotEmpty().Matches(@"^\s*[A-Za-z]{3}\s*$").WithMessage("Para birimi üç harfli ISO kodu olmalıdır (örn. TRY, EUR).");
        RuleFor(x => x.DefaultMachineHourlyCost).InclusiveBetween(0, 10_000_000).WithMessage("Makine saat maliyeti negatif olamaz.");
        RuleFor(x => x.LaborCostPerMinute).InclusiveBetween(0, 1_000_000).WithMessage("Dakika işçilik maliyeti negatif olamaz.");
        RuleFor(x => x.DefaultLiquorRatio).InclusiveBetween(0.1m, 100).WithMessage("Flotte oranı 0,1 ile 100 arasında olmalıdır.");
        RuleFor(x => x.DefaultBatchKilograms).InclusiveBetween(1, 100_000).WithMessage("Varsayılan parti büyüklüğü 1 ile 100.000 kg arasında olmalıdır.");
        RuleFor(x => x.OverheadPercent).InclusiveBetween(0, 100).WithMessage("Genel gider oranı 0 ile 100 arasında olmalıdır.");
        RuleFor(x => x.FirePercent).InclusiveBetween(0, 50).WithMessage("Fire oranı 0 ile 50 arasında olmalıdır.");
        RuleFor(x => x.MarginPercent).InclusiveBetween(-100, 1000).WithMessage("Kâr payı -100 ile 1000 arasında olmalıdır.");
        RuleFor(x => x.ChemicalCostMethod).IsInEnum().WithMessage("Geçerli bir maliyet yöntemi seçin.");
        RuleFor(x => x.FabricDyeingMinutes).InclusiveBetween(1, 10_000).WithMessage("Kumaş boyama süresi 1 ile 10.000 dakika arasında olmalıdır.");
        RuleFor(x => x.YarnDyeingMinutes).InclusiveBetween(1, 10_000).WithMessage("İplik boyama süresi 1 ile 10.000 dakika arasında olmalıdır.");
        RuleFor(x => x.FinishingMinutes).InclusiveBetween(1, 10_000).WithMessage("Terbiye süresi 1 ile 10.000 dakika arasında olmalıdır.");
        RuleFor(x => x.PrintingMinutes).InclusiveBetween(1, 10_000).WithMessage("Baskı süresi 1 ile 10.000 dakika arasında olmalıdır.");
        RuleFor(x => x.MachineRates)
            .Must(rates => rates is null || rates.Select(r => r.MachineId).Distinct().Count() == rates.Count).WithMessage("Bir makine için tek saat maliyeti girilir.")
            .Must(rates => rates is null || rates.All(r => r.HourlyCost is null or (>= 0 and <= 10_000_000))).WithMessage("Makine saat maliyeti negatif olamaz.");
    }
}

public interface IBoyahaneCostSettingsService
{
    Task<BoyahaneCostSettingsDto> GetAsync(CancellationToken cancellationToken);
    Task<Result<BoyahaneCostSettingsDto>> SaveAsync(SaveBoyahaneCostSettingsRequest request, CancellationToken cancellationToken);
}

/// <summary>Ön maliyet ve planlama parametreleri: tek kayıt ve makineye özel saat maliyetleri. Kayıt yoksa varsayılanlar geçerlidir.</summary>
public sealed class BoyahaneCostSettingsService(IAppDbContext db, ICurrentUser currentUser, TimeProvider timeProvider) : IBoyahaneCostSettingsService
{
    /// <summary>Parametreler; kayıt yoksa varsayılanlarla yeni (kaydedilmemiş) nesne.</summary>
    public static async Task<BoyahaneCostSettings> LoadAsync(IAppDbContext db, CancellationToken cancellationToken) =>
        await db.Set<BoyahaneCostSettings>().AsNoTracking().OrderBy(s => s.Id).FirstOrDefaultAsync(cancellationToken) ?? new BoyahaneCostSettings();

    public async Task<BoyahaneCostSettingsDto> GetAsync(CancellationToken cancellationToken)
    {
        var settings = await db.Set<BoyahaneCostSettings>().AsNoTracking().OrderBy(s => s.Id).FirstOrDefaultAsync(cancellationToken);
        var rates = await db.Set<BoyahaneMachineCostRate>().AsNoTracking().ToDictionaryAsync(r => r.MachineId, r => r.HourlyCost, cancellationToken);
        var rateMachineIds = rates.Keys.ToList();
        var machines = await db.Set<BoyahaneMachine>().AsNoTracking()
            .Where(m => m.IsActive || rateMachineIds.Contains(m.Id))
            .OrderBy(m => m.Code)
            .Select(m => new { m.Id, m.Code, m.Name, m.MachineType, m.CapacityKg, m.IsActive })
            .ToListAsync(cancellationToken);

        var value = settings ?? new BoyahaneCostSettings();
        return new BoyahaneCostSettingsDto(
            value.Currency,
            value.DefaultMachineHourlyCost,
            value.LaborCostPerMinute,
            value.DefaultLiquorRatio,
            value.DefaultBatchKilograms,
            value.OverheadPercent,
            value.FirePercent,
            value.MarginPercent,
            value.ChemicalCostMethod,
            value.FabricDyeingMinutes,
            value.YarnDyeingMinutes,
            value.FinishingMinutes,
            value.PrintingMinutes,
            settings is null,
            settings?.UpdatedAt ?? settings?.CreatedAt,
            settings?.UpdatedBy ?? settings?.CreatedBy,
            machines.Select(m => new BoyahaneMachineCostRateDto(
                m.Id, m.Code, m.Name, m.MachineType, m.CapacityKg, m.IsActive, rates.TryGetValue(m.Id, out var cost) ? (decimal?)cost : null)).ToList());
    }

    public async Task<Result<BoyahaneCostSettingsDto>> SaveAsync(SaveBoyahaneCostSettingsRequest request, CancellationToken cancellationToken)
    {
        var settings = await db.Set<BoyahaneCostSettings>().OrderBy(s => s.Id).FirstOrDefaultAsync(cancellationToken);
        if (await CurrencyRules.RequireActiveAsync(db, request.Currency, settings?.Currency, "currency", cancellationToken) is { } currencyError)
        {
            return currencyError;
        }

        var machineIds = (request.MachineRates ?? []).Select(r => r.MachineId).ToList();
        var knownMachines = await db.Set<BoyahaneMachine>().Where(m => machineIds.Contains(m.Id)).Select(m => m.Id).ToListAsync(cancellationToken);
        if (knownMachines.Count != machineIds.Count)
        {
            return ReferenceValidation.InvalidReference("machineRates", "Listede bulunamayan bir makine var.");
        }

        if (settings is null)
        {
            settings = new BoyahaneCostSettings();
            db.Set<BoyahaneCostSettings>().Add(settings);
        }

        settings.Currency = request.Currency.Trim().ToUpperInvariant();
        settings.DefaultMachineHourlyCost = request.DefaultMachineHourlyCost;
        settings.LaborCostPerMinute = request.LaborCostPerMinute;
        settings.DefaultLiquorRatio = request.DefaultLiquorRatio;
        settings.DefaultBatchKilograms = request.DefaultBatchKilograms;
        settings.OverheadPercent = request.OverheadPercent;
        settings.FirePercent = request.FirePercent;
        settings.MarginPercent = request.MarginPercent;
        settings.ChemicalCostMethod = request.ChemicalCostMethod;
        settings.FabricDyeingMinutes = request.FabricDyeingMinutes;
        settings.YarnDyeingMinutes = request.YarnDyeingMinutes;
        settings.FinishingMinutes = request.FinishingMinutes;
        settings.PrintingMinutes = request.PrintingMinutes;

        // Makine listesi verildiyse yalnızca listedeki makinelerin maliyeti değişir; listede olmayanlar korunur.
        if (request.MachineRates is { } machineRates)
        {
            var existing = await db.Set<BoyahaneMachineCostRate>().Where(r => machineIds.Contains(r.MachineId)).ToDictionaryAsync(r => r.MachineId, cancellationToken);
            var now = timeProvider.GetUtcNow().UtcDateTime;
            foreach (var rate in machineRates)
            {
                existing.TryGetValue(rate.MachineId, out var row);
                if (rate.HourlyCost is not { } cost)
                {
                    if (row is not null)
                    {
                        db.Set<BoyahaneMachineCostRate>().Remove(row);
                    }

                    continue;
                }

                if (row is null)
                {
                    row = new BoyahaneMachineCostRate { MachineId = rate.MachineId };
                    db.Set<BoyahaneMachineCostRate>().Add(row);
                }
                else if (row.HourlyCost == cost)
                {
                    continue;
                }

                row.HourlyCost = cost;
                row.UpdatedAt = now;
                row.UpdatedBy = currentUser.UserName;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(cancellationToken);
    }
}
