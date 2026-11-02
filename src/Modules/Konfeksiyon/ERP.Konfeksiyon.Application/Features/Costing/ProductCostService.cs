using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Konfeksiyon.Application.Features.Payroll;
using ERP.Konfeksiyon.Domain.Entities;
using ERP.Konfeksiyon.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERP.Konfeksiyon.Application.Features.Costing;

/// <param name="RouteVersion">Modelin etkin rotasının sürümü; etkin rota yoksa boş.</param>
/// <param name="TotalSam">Rotadaki adımların standart süreleri toplamı (dakika).</param>
/// <param name="LaborCost">Parça başı işçilik: operasyonun birim ücreti, yoksa SAM × dakika ücreti.</param>
/// <param name="LaborUnpriced">Ücreti hesaplanamayan (birim ücreti ve dakika ücreti olmayan) adım var.</param>
/// <param name="MaterialCost">
/// Renk-bedene özel satırı olmayan bir parçanın malzeme maliyeti, fire dahil: reçetenin genel satırları, malzeme başına
/// bir satır (sipariş maliyetindeki seçimle aynı). Böyle bir renk-bedenin sipariş maliyetiyle aynı çıkar.
/// </param>
/// <param name="MaterialUncosted">Genel satırlarda birim maliyeti olmayan malzeme var.</param>
/// <param name="HasVariantMaterials">
/// Renk / bedene özel reçete satırı var: o renk-bedenlerde aynı malzemenin genel satırı yerine özel satır geçerlidir,
/// maliyetleri bu özetten farklıdır (sipariş maliyetinde renk-beden bazında hesaplanır).
/// </param>
public sealed record ProductCostSummaryDto(
    int ProductId,
    string Currency,
    int? RouteVersion,
    int OperationCount,
    decimal TotalSam,
    decimal? LaborCost,
    bool LaborUnpriced,
    int MaterialLineCount,
    decimal? MaterialCost,
    bool MaterialUncosted,
    bool HasVariantMaterials,
    decimal? TotalCost,
    decimal? ListPrice,
    string ListPriceCurrency);

public interface IProductCostService
{
    /// <summary>Ürünün etkin rotası ve reçetesinden parça başı standart maliyeti.</summary>
    Task<Result<ProductCostSummaryDto>> GetSummaryAsync(int productId, CancellationToken cancellationToken);
}

public sealed class ProductCostService(IKonfeksiyonDbContext db, IPayrollService payroll) : IProductCostService
{
    public async Task<Result<ProductCostSummaryDto>> GetSummaryAsync(int productId, CancellationToken cancellationToken)
    {
        var product = await db.Products.AsNoTracking()
            .Where(p => p.Id == productId)
            .Select(p => new { p.ModelId, p.ListPrice, p.Currency })
            .SingleOrDefaultAsync(cancellationToken);
        if (product is null)
        {
            return new Error(ErrorCodes.NotFound, "Ürün bulunamadı.", ErrorType.NotFound);
        }

        var setting = await payroll.LoadSettingAsync(cancellationToken);

        var steps = await db.ProductionRouteOperations.AsNoTracking()
            .Where(o => o.ProductionRoute.ModelId == product.ModelId && o.ProductionRoute.Status == RouteStatus.Active)
            .Select(o => new { o.Sam, o.OperationId, o.Operation.PieceRate, o.ProductionRoute.Version })
            .ToListAsync(cancellationToken);
        var rates = steps.Select(s => setting.UnitRateFor(s.PieceRate, s.Sam)).ToList();
        decimal? laborCost = steps.Count == 0 ? null : decimal.Round(rates.Sum(r => r ?? 0), 4);

        // Sipariş maliyetiyle aynı kural: ürün düzeyindeki maliyet, renk-bedene özel satırı olmayan bir parçanın
        // maliyetidir (MaterialRequirement.GeneralLines = o parça için LinesFor). Özel satırı olan renk-bedenlerde
        // aynı malzemenin genel satırı geçersizdir; bu özet onları kapsamaz (HasVariantMaterials ile belirtilir).
        var lines = await db.ProductMaterials.AsNoTracking()
            .Where(m => m.ProductId == productId)
            .ToListAsync(cancellationToken);
        var general = MaterialRequirement.GeneralLines(lines);
        var generalItemIds = general.Select(l => l.StockItemId).Distinct().ToList();
        // Sonradan silinen malzeme kartının maliyeti de okunur (sipariş maliyetindeki gibi).
        var unitCosts = await db.StockItems.AsNoTracking().IgnoreQueryFilters()
            .Where(i => generalItemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, i => i.UnitCost, cancellationToken);
        decimal? materialCost = general.Count == 0
            ? null
            : decimal.Round(general.Sum(l => l.GrossQuantityPerPiece * (unitCosts.GetValueOrDefault(l.StockItemId) ?? 0)), 4);

        return new ProductCostSummaryDto(
            productId,
            setting.Currency,
            steps.FirstOrDefault()?.Version,
            steps.Count,
            steps.Sum(s => s.Sam),
            laborCost,
            rates.Any(r => r is null),
            lines.Count,
            materialCost,
            general.Any(l => unitCosts.GetValueOrDefault(l.StockItemId) is null),
            lines.Any(l => l.Specificity > 0),
            laborCost is null && materialCost is null ? null : (laborCost ?? 0) + (materialCost ?? 0),
            product.ListPrice,
            product.Currency);
    }
}
