using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Domain.Enums;
using ERP.Konfeksiyon.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ERP.Konfeksiyon.Application.Features.Costing;

/// <param name="ColorId">Satır yalnızca bu renkteki parçalar için geçerliyse renk; hepsi için boş.</param>
/// <param name="SizeId">Satır yalnızca bu bedendeki parçalar için geçerliyse beden; hepsi için boş.</param>
/// <param name="QuantityPerPiece">Bir parça için gereken miktar (stok kartının biriminde), fire hariç.</param>
/// <param name="UnitCost">Malzemenin güncel ortalama birim maliyeti; bilinmiyorsa boş.</param>
/// <param name="CostPerPiece">Fire dahil, bir parçaya düşen malzeme maliyeti.</param>
public sealed record ProductMaterialDto(
    int Id,
    int StockItemId,
    string ItemCode,
    string ItemName,
    StockItemType ItemType,
    StockUnit Unit,
    int? ColorId,
    string? ColorName,
    int? SizeId,
    string? SizeName,
    decimal QuantityPerPiece,
    decimal WastePercent,
    decimal? UnitCost,
    decimal? CostPerPiece);

public sealed record ProductMaterialsDto(
    int ProductId, string ProductCode, string ProductName, string Currency, IReadOnlyList<ProductMaterialDto> Lines);

public sealed record ProductMaterialRequest(
    int StockItemId, int? ColorId, int? SizeId, decimal QuantityPerPiece, decimal WastePercent = 0);

/// <summary>Ürünün reçetesinin tamamı; gönderilmeyen satırlar reçeteden çıkar.</summary>
public sealed record SaveProductMaterialsRequest(IReadOnlyList<ProductMaterialRequest> Lines);

/// <param name="Required">Siparişin tamamı için gereken miktar (fire dahil).</param>
/// <param name="Issued">Açılan demetler için depodan düşülmüş net miktar (iptaller geri alınmış).</param>
/// <param name="RemainingNeed">Henüz demetlenmemiş adet için gereken miktar.</param>
/// <param name="Available">Depodaki mevcut stok.</param>
/// <param name="Shortage">Kalan ihtiyacın stokla karşılanamayan kısmı: depo seçiliyse o depo ve öteki aktif hammadde depoları
/// birlikte sayılır (demet açılırken de öyle düşülür); başka siparişlere ayrılmış stok sayılmaz.</param>
/// <param name="Reserved">Depoda bu siparişe ayrılmış miktar.</param>
/// <param name="ReservedByOthers">Depoda başka siparişlere ayrılmış miktar.</param>
/// <param name="Stocks">Malzemenin bütün depolardaki stoğu (stoğu olan depolar).</param>
/// <param name="OnOrder">Bu sipariş için açılmış satın alma siparişlerinde teslim alınmayı bekleyen miktar.</param>
public sealed record OrderMaterialLineDto(
    int StockItemId,
    string ItemCode,
    string ItemName,
    StockUnit Unit,
    decimal Required,
    decimal Issued,
    decimal RemainingNeed,
    decimal Available,
    decimal Shortage,
    decimal Reserved = 0,
    decimal ReservedByOthers = 0,
    decimal OnOrder = 0,
    IReadOnlyList<OrderMaterialPurchaseDto>? Purchases = null,
    IReadOnlyList<OrderMaterialStockDto>? Stocks = null);

/// <summary>Malzemenin bir depodaki stoğu (siparişin deposundan bağımsız, bütün depolar).</summary>
/// <param name="Reserved">Bu depoda siparişlere ayrılmış miktar.</param>
public sealed record OrderMaterialStockDto(int WarehouseId, string WarehouseCode, string WarehouseName, decimal OnHand, decimal Reserved);

/// <summary>Malzeme için bu üretim siparişine bağlı açılmış, henüz tamamı teslim alınmamış satın alma kalemi.</summary>
/// <param name="Remaining">Teslim alınmayı bekleyen miktar.</param>
public sealed record OrderMaterialPurchaseDto(
    int PurchaseOrderId, string OrderNo, PurchaseOrderStatus Status, decimal Quantity, decimal Remaining, DateOnly? ExpectedDate);

/// <param name="LotNo">Takipli kartta alınacak top.</param>
/// <param name="Quantity">Bu adresten (toptan) alınacak miktar.</param>
/// <param name="OnHand">Adresteki (toptaki) mevcut miktar.</param>
/// <param name="WarehouseId">Malzemenin alınacağı depo: seçilen depoda yetmeyen kısım öteki hammadde depolarından gelir.</param>
public sealed record PickListLineDto(
    string LocationCode, string? LotNo, string? BatchNo, string? Shade, decimal Quantity, decimal OnHand,
    int WarehouseId = 0, string WarehouseCode = "", string WarehouseName = "");

/// <param name="Need">Siparişin bu malzemede kalan ihtiyacı.</param>
/// <param name="Shortage">İhtiyacın depodan toplanamayan kısmı.</param>
public sealed record PickListItemDto(
    int StockItemId, string ItemCode, string ItemName, StockUnit Unit, decimal Need, decimal Shortage,
    IReadOnlyList<PickListLineDto> Lines);

public sealed record PickListDto(
    int ProductionOrderId, string OrderNo, string ProductCode, string ProductName, int WarehouseId, string WarehouseCode,
    string WarehouseName, IReadOnlyList<PickListItemDto> Items);

/// <param name="HasRecipe">Ürünün reçetesi var mı; yoksa demet açılırken stok düşülmez.</param>
/// <param name="WarehouseId">Stokların okunduğu depo; belirlenemediyse (birden fazla hammadde deposu) boş ve stok tüm depoların toplamıdır.</param>
public sealed record OrderMaterialsDto(
    int ProductionOrderId, bool HasRecipe, int? WarehouseId, string? WarehouseName, IReadOnlyList<OrderMaterialLineDto> Lines);

/// <param name="Amount">Miktar × çıkış anındaki birim maliyet; maliyeti bilinmeyen malzemede boş.</param>
public sealed record OrderCostMaterialDto(
    int StockItemId, string ItemCode, string ItemName, StockUnit Unit, decimal Quantity, decimal? Amount);

/// <summary>Siparişin gerçekleşen ve standart maliyeti. Tutarlar sistem para birimindedir; prim dahil değildir.</summary>
/// <param name="FinishedQuantity">Depoya mamul olarak teslim alınan adet.</param>
/// <param name="MaterialCost">Demetler için düşülen malzemenin maliyeti (iptaller hariç).</param>
/// <param name="LaborCost">Üretilen sağlam adet × demette sabitlenen birim ücret.</param>
/// <param name="CostPerFinishedPiece">Toplam maliyet ÷ depoya giren adet; henüz teslim alınan yoksa boş.</param>
/// <param name="StandardMaterialCostPerPiece">Reçete ve güncel ortalama maliyetlerden, parça başına malzeme maliyeti.</param>
/// <param name="StandardLaborCostPerPiece">Etkin rotadaki operasyonların güncel birim ücretlerinin toplamı.</param>
/// <param name="HasUncostedMaterials">Maliyeti bilinmeyen malzeme var; malzeme maliyeti eksik hesaplanmıştır.</param>
/// <param name="HasUnpricedLabor">Ücreti tanımlı olmayan operasyon var; işçilik maliyeti eksik hesaplanmıştır.</param>
public sealed record OrderCostDto(
    int ProductionOrderId,
    string Currency,
    int OrderedQuantity,
    int BundledQuantity,
    int FinishedQuantity,
    decimal MaterialCost,
    decimal LaborCost,
    decimal TotalCost,
    decimal? CostPerFinishedPiece,
    decimal? StandardMaterialCostPerPiece,
    decimal? StandardLaborCostPerPiece,
    decimal? StandardCostPerPiece,
    bool HasUncostedMaterials,
    bool HasUnpricedLabor,
    IReadOnlyList<OrderCostMaterialDto> Materials);

public sealed class SaveProductMaterialsRequestValidator : AbstractValidator<SaveProductMaterialsRequest>
{
    public const int MaxLines = 200;

    public SaveProductMaterialsRequestValidator()
    {
        RuleFor(x => x.Lines)
            .NotNull().WithMessage("Reçete satırları gönderilmelidir.")
            .Must(lines => lines is null || lines.Count <= MaxLines)
            .WithMessage($"Bir reçetede en fazla {MaxLines} satır olabilir.")
            .Must(lines => lines is null || lines.All(l => l.StockItemId > 0))
            .WithMessage("Her satırda malzeme seçilmelidir.")
            .Must(lines => lines is null || lines.All(l => l.QuantityPerPiece is > 0 and <= 100_000))
            .WithMessage("Parça başı miktar sıfırdan büyük olmalıdır.")
            .Must(lines => lines is null || lines.All(l => decimal.Round(l.QuantityPerPiece, 4) == l.QuantityPerPiece))
            .WithMessage("Parça başı miktar en fazla 4 ondalık basamak içerebilir.")
            .Must(lines => lines is null || lines.All(l => l.WastePercent is >= 0 and <= 100))
            .WithMessage("Fire payı 0 ile 100 arasında olmalıdır.")
            .Must(lines => lines is null || lines.All(l => decimal.Round(l.WastePercent, 2) == l.WastePercent))
            .WithMessage("Fire payı en fazla 2 ondalık basamak içerebilir.")
            .Must(lines => lines is null
                || lines.Select(l => (l.StockItemId, l.ColorId, l.SizeId)).Distinct().Count() == lines.Count)
            .WithMessage("Aynı malzeme, aynı renk ve beden için reçetede yalnızca bir kez yer alabilir.");
    }
}

/// <summary>Reçete malzemesinin düşüleceği deponun belirlenmesi.</summary>
public static class MaterialWarehouse
{
    /// <summary>
    /// İstekte depo verilmişse o; verilmemişse siparişin malzeme deposu; o da yoksa tek aktif hammadde deposu.
    /// Birden fazla (ya da hiç) hammadde deposu varsa depo seçilmesi istenir.
    /// </summary>
    public static async Task<Result<int>> ResolveAsync(
        IKonfeksiyonDbContext db, int? requestedId, int? orderWarehouseId, CancellationToken cancellationToken)
    {
        if ((requestedId ?? orderWarehouseId) is { } id)
        {
            return id;
        }

        var candidates = await db.Warehouses
            .AsNoTracking()
            .Where(w => w.IsActive && w.Type == WarehouseType.RawMaterial)
            .Select(w => w.Id)
            .Take(2)
            .ToListAsync(cancellationToken);
        if (candidates.Count == 1)
        {
            return candidates[0];
        }

        const string message = "Reçete malzemesinin düşüleceği depoyu seçin.";
        return new Error(
            ErrorCodes.MaterialWarehouseRequired, message, ErrorType.Validation,
            new Dictionary<string, string[]> { ["warehouseId"] = [message] });
    }
}

/// <summary>Reçeteye eklenebilecek bir malzeme kartı.</summary>
public sealed record MaterialOptionDto(int Id, string Code, string Name, StockUnit Unit, decimal? UnitCost, bool IsActive);
