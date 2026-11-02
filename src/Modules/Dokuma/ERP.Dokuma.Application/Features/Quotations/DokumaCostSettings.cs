using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Dokuma.Domain.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ERP.Dokuma.Application.Features.Quotations;

/// <param name="NominalSpeedRpm">Tezgâh kartındaki anma hızı; ön maliyette ve kapasite planında kullanılır.</param>
/// <param name="HourlyCost">Tezgâha özel saat maliyeti; boşsa genel değer.</param>
/// <param name="EfficiencyPercent">Tezgâha özel verim; boşsa genel değer.</param>
public sealed record DokumaLoomCostRateDto(
    int LoomId,
    string LoomCode,
    string LoomName,
    bool IsActive,
    int? NominalSpeedRpm,
    decimal? HourlyCost,
    decimal? EfficiencyPercent);

/// <summary>Ön maliyet parametreleri ve tezgâha özel değerler.</summary>
/// <param name="Currency">Tutarların para birimi.</param>
/// <param name="UseSizingRecipe">Haşıl maliyeti artikelin varsayılan reçetesindeki kimyasallardan hesaplanır.</param>
public sealed record DokumaCostSettingsDto(
    string Currency,
    decimal LoomHourlyCost,
    decimal LaborHourlyCost,
    decimal WarpingCostPerMeter,
    decimal SizingCostPerMeter,
    bool UseSizingRecipe,
    decimal OverheadPercent,
    decimal DefaultWastePercent,
    decimal DefaultMarginPercent,
    decimal DefaultEfficiencyPercent,
    int QuotationValidityDays,
    DateTime? UpdatedAt,
    string? UpdatedBy,
    IReadOnlyList<DokumaLoomCostRateDto> Looms);

public sealed record DokumaLoomCostRateRequest(int LoomId, decimal? HourlyCost = null, decimal? EfficiencyPercent = null);

public sealed record DokumaCostSettingsRequest(
    string Currency,
    decimal LoomHourlyCost,
    decimal LaborHourlyCost,
    decimal WarpingCostPerMeter,
    decimal SizingCostPerMeter,
    bool UseSizingRecipe,
    decimal OverheadPercent,
    decimal DefaultWastePercent,
    decimal DefaultMarginPercent,
    decimal DefaultEfficiencyPercent,
    int QuotationValidityDays,
    IReadOnlyList<DokumaLoomCostRateRequest> Looms);

public sealed class DokumaCostSettingsRequestValidator : AbstractValidator<DokumaCostSettingsRequest>
{
    public DokumaCostSettingsRequestValidator()
    {
        RuleFor(x => x.Currency).NotEmpty().Matches(@"^\s*[A-Za-z]{3}\s*$").WithMessage("Para birimi üç harfli ISO kodu olmalıdır.");
        RuleFor(x => x.LoomHourlyCost).InclusiveBetween(0, 1_000_000).WithMessage("Tezgâh saat maliyeti 0 ile 1.000.000 arasında olmalıdır.");
        RuleFor(x => x.LaborHourlyCost).InclusiveBetween(0, 1_000_000).WithMessage("İşçilik saat maliyeti 0 ile 1.000.000 arasında olmalıdır.");
        RuleFor(x => x.WarpingCostPerMeter).InclusiveBetween(0, 100_000).WithMessage("Çözgü maliyeti 0 ile 100.000 arasında olmalıdır.");
        RuleFor(x => x.SizingCostPerMeter).InclusiveBetween(0, 100_000).WithMessage("Haşıl maliyeti 0 ile 100.000 arasında olmalıdır.");
        RuleFor(x => x.OverheadPercent).InclusiveBetween(0, 500).WithMessage("Genel gider 0 ile 500 arasında olmalıdır.");
        RuleFor(x => x.DefaultWastePercent).InclusiveBetween(0, 100).WithMessage("Fire 0 ile 100 arasında olmalıdır.");
        RuleFor(x => x.DefaultMarginPercent).InclusiveBetween(0, 500).WithMessage("Kâr marjı 0 ile 500 arasında olmalıdır.");
        RuleFor(x => x.DefaultEfficiencyPercent).GreaterThan(0).WithMessage("Verim 0'dan büyük, en fazla 100 olmalıdır.").LessThanOrEqualTo(100).WithMessage("Verim 0'dan büyük, en fazla 100 olmalıdır.");
        RuleFor(x => x.QuotationValidityDays).InclusiveBetween(1, 365).WithMessage("Teklif geçerlilik süresi 1 ile 365 gün arasında olmalıdır.");
        RuleFor(x => x.Looms).NotNull().WithMessage("Tezgâh listesi zorunludur.");
        RuleForEach(x => x.Looms).ChildRules(loom =>
        {
            loom.RuleFor(l => l.LoomId).GreaterThan(0).WithMessage("Tezgâh geçersiz.");
            loom.RuleFor(l => l.HourlyCost).InclusiveBetween(0, 1_000_000).WithMessage("Tezgâh saat maliyeti 0 ile 1.000.000 arasında olmalıdır.").When(l => l.HourlyCost is not null);
            loom.RuleFor(l => l.EfficiencyPercent).GreaterThan(0).WithMessage("Tezgâh verimi 0'dan büyük, en fazla 100 olmalıdır.").LessThanOrEqualTo(100).WithMessage("Tezgâh verimi 0'dan büyük, en fazla 100 olmalıdır.").When(l => l.EfficiencyPercent is not null);
        });
    }
}

public interface IDokumaCostSettingsService
{
    Task<DokumaCostSettingsDto> GetAsync(CancellationToken cancellationToken);
    Task<Result<DokumaCostSettingsDto>> SaveAsync(DokumaCostSettingsRequest request, CancellationToken cancellationToken);
}

/// <summary>Ön maliyet parametreleri (tek kayıt) ve tezgâha özel saat maliyeti / verim.</summary>
public sealed class DokumaCostSettingsService(IAppDbContext db) : IDokumaCostSettingsService
{
    /// <summary>Kayıtlı parametreler; hiç kaydedilmemişse varsayılanlar (izlenmeyen nesne).</summary>
    public static async Task<DokumaCostSettings> LoadAsync(IAppDbContext db, CancellationToken cancellationToken) =>
        await db.Set<DokumaCostSettings>().AsNoTracking().OrderBy(s => s.Id).FirstOrDefaultAsync(cancellationToken) ?? new DokumaCostSettings();

    /// <summary>Tezgâha özel değerler, tezgâh Id'sine göre.</summary>
    public static async Task<Dictionary<int, DokumaLoomCostRate>> LoomRatesAsync(IAppDbContext db, CancellationToken cancellationToken) =>
        await db.Set<DokumaLoomCostRate>().AsNoTracking().ToDictionaryAsync(r => r.LoomId, cancellationToken);

    public async Task<DokumaCostSettingsDto> GetAsync(CancellationToken cancellationToken)
    {
        var settings = await LoadAsync(db, cancellationToken);
        var rates = await LoomRatesAsync(db, cancellationToken);
        var rateLoomIds = rates.Keys.ToList();

        // Etkin tezgâhlar ve özel değeri olan pasif tezgâhlar.
        var looms = await db.Set<DokumaLoom>().AsNoTracking()
            .Where(l => l.IsActive || rateLoomIds.Contains(l.Id))
            .OrderBy(l => l.Code)
            .Select(l => new { l.Id, l.Code, l.Name, l.IsActive, l.NominalSpeedRpm })
            .ToListAsync(cancellationToken);

        return new DokumaCostSettingsDto(
            settings.Currency,
            settings.LoomHourlyCost,
            settings.LaborHourlyCost,
            settings.WarpingCostPerMeter,
            settings.SizingCostPerMeter,
            settings.UseSizingRecipe,
            settings.OverheadPercent,
            settings.DefaultWastePercent,
            settings.DefaultMarginPercent,
            settings.DefaultEfficiencyPercent,
            settings.QuotationValidityDays,
            settings.Id == 0 ? null : settings.UpdatedAt ?? settings.CreatedAt,
            settings.UpdatedBy ?? settings.CreatedBy,
            looms.Select(l =>
            {
                var rate = rates.GetValueOrDefault(l.Id);
                return new DokumaLoomCostRateDto(l.Id, l.Code, l.Name, l.IsActive, l.NominalSpeedRpm, rate?.HourlyCost, rate?.EfficiencyPercent);
            }).ToList());
    }

    public async Task<Result<DokumaCostSettingsDto>> SaveAsync(DokumaCostSettingsRequest request, CancellationToken cancellationToken)
    {
        var settings = await db.Set<DokumaCostSettings>().OrderBy(s => s.Id).FirstOrDefaultAsync(cancellationToken);
        if (await CurrencyRules.RequireActiveAsync(db, request.Currency, settings?.Currency, "currency", cancellationToken) is { } currencyError)
        {
            return currencyError;
        }

        var loomIds = request.Looms.Select(l => l.LoomId).Distinct().ToList();
        if (loomIds.Count != request.Looms.Count)
        {
            return ReferenceValidation.InvalidReference("looms", "Bir tezgâh listede iki kez olamaz.");
        }

        var existingLooms = await db.Set<DokumaLoom>().Where(l => loomIds.Contains(l.Id)).CountAsync(cancellationToken);
        if (existingLooms != loomIds.Count)
        {
            return ReferenceValidation.InvalidReference("looms", "Tezgâhlardan biri bulunamadı.");
        }

        if (settings is null)
        {
            settings = new DokumaCostSettings();
            db.Set<DokumaCostSettings>().Add(settings);
        }

        settings.Currency = request.Currency.Trim().ToUpperInvariant();
        settings.LoomHourlyCost = request.LoomHourlyCost;
        settings.LaborHourlyCost = request.LaborHourlyCost;
        settings.WarpingCostPerMeter = request.WarpingCostPerMeter;
        settings.SizingCostPerMeter = request.SizingCostPerMeter;
        settings.UseSizingRecipe = request.UseSizingRecipe;
        settings.OverheadPercent = request.OverheadPercent;
        settings.DefaultWastePercent = request.DefaultWastePercent;
        settings.DefaultMarginPercent = request.DefaultMarginPercent;
        settings.DefaultEfficiencyPercent = request.DefaultEfficiencyPercent;
        settings.QuotationValidityDays = request.QuotationValidityDays;

        // Gönderilen tezgâhlar güncellenir; iki değeri de boş olan tezgâhın özel kaydı silinir. Listede olmayan tezgâhlar değişmez.
        var rates = await db.Set<DokumaLoomCostRate>().Where(r => loomIds.Contains(r.LoomId)).ToListAsync(cancellationToken);
        foreach (var item in request.Looms)
        {
            var rate = rates.SingleOrDefault(r => r.LoomId == item.LoomId);
            if (item.HourlyCost is null && item.EfficiencyPercent is null)
            {
                if (rate is not null)
                {
                    db.Set<DokumaLoomCostRate>().Remove(rate);
                }

                continue;
            }

            if (rate is null)
            {
                rate = new DokumaLoomCostRate { LoomId = item.LoomId };
                db.Set<DokumaLoomCostRate>().Add(rate);
            }

            rate.HourlyCost = item.HourlyCost;
            rate.EfficiencyPercent = item.EfficiencyPercent;
        }

        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(cancellationToken);
    }
}
