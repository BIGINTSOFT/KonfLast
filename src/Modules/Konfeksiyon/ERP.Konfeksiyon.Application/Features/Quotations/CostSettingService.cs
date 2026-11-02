using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Konfeksiyon.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP.Konfeksiyon.Application.Features.Quotations;

public interface ICostSettingService
{
    Task<CostSettingDto> GetAsync(CancellationToken cancellationToken);
    Task<Result<CostSettingDto>> UpdateAsync(CostSettingRequest request, CancellationToken cancellationToken);
}

/// <summary>Ön maliyet parametreleri (tek satır). Kayıt yoksa sıfır değerlerle çalışılır; ilk kayıtta oluşturulur.</summary>
public sealed class CostSettingService(IKonfeksiyonDbContext db) : ICostSettingService
{
    public async Task<CostSettingDto> GetAsync(CancellationToken cancellationToken)
    {
        var setting = await LoadAsync(db, cancellationToken);
        return new CostSettingDto(
            setting.LaborCostPerMinute,
            setting.EnergyCostPerMinute,
            setting.Currency,
            setting.OverheadPercent,
            setting.DefaultFirePercent,
            setting.DefaultMarginPercent,
            setting.UpdatedAt ?? (setting.CreatedAt == default ? (DateTime?)null : setting.CreatedAt),
            setting.UpdatedBy ?? setting.CreatedBy);
    }

    public async Task<Result<CostSettingDto>> UpdateAsync(CostSettingRequest request, CancellationToken cancellationToken)
    {
        var setting = await db.CostSettings.SingleOrDefaultAsync(s => s.Id == CostSetting.SingletonId, cancellationToken);
        if (await CurrencyRules.RequireActiveAsync(db, request.Currency, setting?.Currency, "currency", cancellationToken)
            is { } currencyError)
        {
            return currencyError;
        }

        if (setting is null)
        {
            setting = new CostSetting { Id = CostSetting.SingletonId };
            db.CostSettings.Add(setting);
        }

        setting.LaborCostPerMinute = request.LaborCostPerMinute;
        setting.EnergyCostPerMinute = request.EnergyCostPerMinute;
        setting.Currency = string.IsNullOrWhiteSpace(request.Currency)
            ? ERP.Core.Domain.Entities.Currency.LocalCode
            : request.Currency.Trim().ToUpperInvariant();
        setting.OverheadPercent = request.OverheadPercent;
        setting.DefaultFirePercent = request.DefaultFirePercent;
        setting.DefaultMarginPercent = request.DefaultMarginPercent;
        await db.SaveChangesAsync(cancellationToken);

        return await GetAsync(cancellationToken);
    }

    /// <summary>Ayar satırı; yoksa kaydedilmemiş varsayılan (sıfır) ayar.</summary>
    public static async Task<CostSetting> LoadAsync(IKonfeksiyonDbContext db, CancellationToken cancellationToken) =>
        await db.CostSettings.AsNoTracking().SingleOrDefaultAsync(s => s.Id == CostSetting.SingletonId, cancellationToken)
        ?? new CostSetting { Id = CostSetting.SingletonId };
}
