using ERP.Core.Domain.Common;
using ERP.Core.Domain.Enums;

namespace ERP.Konfeksiyon.Domain.Entities;

/// <summary>
/// Reçete satırı: ürünün bir parçası için gereken malzeme. Renk ve / veya beden verilirse satır yalnızca o
/// renk-bedendeki parçalar için geçerlidir (örn. beyaz tişört için beyaz kumaş); boşsa hepsi için.
/// </summary>
public class ProductMaterial : AuditableEntity
{
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int StockItemId { get; set; }
    public StockItem StockItem { get; set; } = null!;

    public int? ColorId { get; set; }
    public Color? Color { get; set; }

    public int? SizeId { get; set; }
    public Size? Size { get; set; }

    /// <summary>Bir parça için gereken miktar (stok kartının biriminde), fire hariç.</summary>
    public decimal QuantityPerPiece { get; set; }

    /// <summary>Fire payı (%); ihtiyaç bu oranda artırılır.</summary>
    public decimal WastePercent { get; set; }

    /// <summary>Satırın ne kadar özel olduğu: renk ve bedeni belirtilmiş satır, genel satırın önüne geçer.</summary>
    public int Specificity => (ColorId is null ? 0 : 1) + (SizeId is null ? 0 : 1);

    /// <summary>Fire dahil, bir parça için gereken miktar.</summary>
    public decimal GrossQuantityPerPiece => QuantityPerPiece * (1 + WastePercent / 100m);

    /// <summary>Satır bu renk ve bedendeki parça için geçerli mi.</summary>
    public bool AppliesTo(int colorId, int sizeId) =>
        (ColorId is null || ColorId == colorId) && (SizeId is null || SizeId == sizeId);
}

/// <summary>Reçeteden malzeme ihtiyacı hesabı.</summary>
public static class MaterialRequirement
{
    /// <summary>
    /// Verilen renk-bedendeki parçalar için geçerli reçete satırları. Aynı malzeme için hem genel hem özel
    /// satır varsa en özel olan kullanılır (genel satır o renk-beden için geçersiz kalır).
    /// </summary>
    public static IReadOnlyList<ProductMaterial> LinesFor(IEnumerable<ProductMaterial> lines, int colorId, int sizeId) =>
        lines
            .Where(l => l.AppliesTo(colorId, sizeId))
            .GroupBy(l => l.StockItemId)
            .Select(g => g.OrderByDescending(l => l.Specificity).First())
            .ToList();

    /// <summary>
    /// Renk-bedene özel satırı bulunmayan bir parça için geçerli reçete satırları: yalnız genel satırlar, malzeme başına
    /// bir tane (<see cref="LinesFor"/> ile aynı seçim). Ürün düzeyindeki standart maliyet bunlardan hesaplanır; özel
    /// satırı olan renk-bedenlerde o malzemenin genel satırı geçersizdir, maliyetleri sipariş maliyetinde ayrıca hesaplanır.
    /// </summary>
    public static IReadOnlyList<ProductMaterial> GeneralLines(IEnumerable<ProductMaterial> lines) =>
        lines
            .Where(l => l.Specificity == 0)
            .GroupBy(l => l.StockItemId)
            .Select(g => g.First())
            .ToList();

    /// <summary>
    /// <paramref name="pieces"/> parça için gereken miktar (fire dahil). Yukarı yuvarlanır: stok 3 ondalık
    /// basamak tutar, adetle izlenen kartta kesirli miktar olmaz.
    /// </summary>
    public static decimal QuantityFor(ProductMaterial line, StockUnit unit, int pieces)
    {
        var quantity = pieces * line.GrossQuantityPerPiece;
        return unit == StockUnit.Piece ? decimal.Ceiling(quantity) : decimal.Ceiling(quantity * 1000m) / 1000m;
    }

    /// <summary>
    /// Aynı sipariş kaleminden <paramref name="alreadyCovered"/> parça karşılandıktan sonraki
    /// <paramref name="pieces"/> parça için gereken ek miktar. Yuvarlama kalemin toplamı üzerinden yapılır:
    /// demet demet düşülen miktarların toplamı, siparişte gösterilen ihtiyaçla aynı çıkar (her demeti ayrı ayrı
    /// yukarı yuvarlamak, ihtiyaç "yeterli" görünürken son demette stok yetmemesine yol açardı).
    /// </summary>
    public static decimal QuantityForNext(ProductMaterial line, StockUnit unit, int alreadyCovered, int pieces) =>
        QuantityFor(line, unit, alreadyCovered + pieces) - QuantityFor(line, unit, alreadyCovered);
}
