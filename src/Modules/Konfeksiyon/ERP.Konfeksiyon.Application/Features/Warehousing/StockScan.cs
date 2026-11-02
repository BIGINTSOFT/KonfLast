using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Konfeksiyon.Application.Features.Costing;
using ERP.Konfeksiyon.Domain.Entities;
using ERP.Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERP.Konfeksiyon.Application.Features.Warehousing;

/// <summary>Okutulan barkodun neye ait olduğu.</summary>
public enum StockScanKind
{
    Lot,
    Location,
    Item,
}

public sealed record ScanLocationDto(
    int Id, int WarehouseId, string WarehouseCode, string WarehouseName, string Code, string? Description, bool IsActive);

public sealed record ScanItemDto(int Id, string Code, string Name, StockUnit Unit, bool IsLotTracked);

public sealed record ScanLotDto(
    int Id, string LotNo, string? BatchNo, string? Shade, decimal? Width, string? SupplierRollNo, decimal InitialQuantity);

/// <summary>Okutulan şeyle ilgili stok: bir kartın (takipli kartta bir topun) bir adresteki miktarı.</summary>
public sealed record ScanBalanceDto(
    int WarehouseId,
    string WarehouseCode,
    string WarehouseName,
    int LocationId,
    string LocationCode,
    int StockItemId,
    string ItemCode,
    string ItemName,
    StockUnit Unit,
    bool IsLotTracked,
    int? LotId,
    string? LotNo,
    string? BatchNo,
    decimal Quantity);

/// <param name="Location">Adres okutulduysa dolu.</param>
/// <param name="Item">Kart ya da top okutulduysa kart.</param>
/// <param name="Lot">Top okutulduysa dolu.</param>
/// <param name="Balances">Topun / kartın bulunduğu yerler ya da adresin içindekiler (stoğu olan satırlar).</param>
public sealed record StockScanDto(
    StockScanKind Kind, string Code, ScanLocationDto? Location, ScanItemDto? Item, ScanLotDto? Lot,
    IReadOnlyList<ScanBalanceDto> Balances)
{
    public const int MaxBalances = 500;
}

public interface IStockScanService
{
    /// <summary>
    /// Okutulan kodu çözer: sırasıyla top numarası, adres barkodu, adres kodu ve stok kartı kodu denenir. Adres
    /// kodu birden fazla depoda varsa depo verilmelidir (ya da adres etiketi okutulmalıdır).
    /// </summary>
    Task<Result<StockScanDto>> ResolveAsync(string? code, int? warehouseId, CancellationToken cancellationToken);

    /// <summary>Sipariş numarasıyla toplama listesi (el terminali): hangi adresten, hangi toptan ne kadar alınacağı.</summary>
    Task<Result<PickListDto>> PickListAsync(string? orderNo, int? warehouseId, CancellationToken cancellationToken);
}

/// <summary>El terminali ekranları için barkod çözme. Yalnızca okur; hareketler stok servisinden geçer.</summary>
public sealed class StockScanService(IKonfeksiyonDbContext db, ICostingService costing) : IStockScanService
{
    public async Task<Result<PickListDto>> PickListAsync(string? orderNo, int? warehouseId, CancellationToken cancellationToken)
    {
        var text = orderNo?.Trim() ?? string.Empty;
        var orderId = text.Length is 0 or > 30
            ? null
            : await db.ProductionOrders.AsNoTracking().Where(o => o.OrderNo == text).Select(o => (int?)o.Id).SingleOrDefaultAsync(cancellationToken);
        if (orderId is null)
        {
            return new Error(ErrorCodes.NotFound, $"{text}: bu numarayla bir üretim siparişi bulunamadı.", ErrorType.NotFound);
        }

        return await costing.GetPickListAsync(orderId.Value, warehouseId, cancellationToken);
    }

    public async Task<Result<StockScanDto>> ResolveAsync(string? code, int? warehouseId, CancellationToken cancellationToken)
    {
        var text = code?.Trim().ToUpperInvariant() ?? string.Empty;
        if (text.Length is 0 or > 100)
        {
            const string message = "Okutulan kod boş ya da çok uzun.";
            return new Error(
                ErrorCodes.ValidationError, message, ErrorType.Validation,
                new Dictionary<string, string[]> { ["code"] = [message] });
        }

        var lot = await db.StockLots
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(l => l.LotNo == text)
            .Select(l => new
            {
                Lot = new ScanLotDto(l.Id, l.LotNo, l.BatchNo, l.Shade, l.Width, l.SupplierRollNo, l.InitialQuantity),
                Item = new ScanItemDto(l.StockItem.Id, l.StockItem.Code, l.StockItem.Name, l.StockItem.Unit, l.StockItem.IsLotTracked),
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (lot is not null)
        {
            return new StockScanDto(
                StockScanKind.Lot, text, null, lot.Item, lot.Lot,
                await BalancesAsync(db.Stocks.Where(s => s.LotId == lot.Lot.Id), cancellationToken));
        }

        var locations = db.WarehouseLocations.AsNoTracking();
        locations = WarehouseLocation.TryParseBarcode(text, out var locationId)
            ? locations.Where(l => l.Id == locationId)
            : locations.Where(l => l.Code == text && (warehouseId == null || l.WarehouseId == warehouseId));
        var matches = await locations
            .OrderBy(l => l.Id)
            .Take(2)
            .Select(l => new ScanLocationDto(
                l.Id, l.WarehouseId, l.Warehouse.Code, l.Warehouse.Name, l.Code, l.Description, l.IsActive))
            .ToListAsync(cancellationToken);
        if (matches.Count > 1)
        {
            return new Error(
                ErrorCodes.ScanAmbiguous,
                $"{text} adres kodu birden fazla depoda var. Adres etiketindeki barkodu okutun.", ErrorType.Conflict);
        }

        if (matches.Count == 1)
        {
            var location = matches[0];
            return new StockScanDto(
                StockScanKind.Location, text, location, null, null,
                await BalancesAsync(db.Stocks.Where(s => s.LocationId == location.Id), cancellationToken));
        }

        var item = await db.StockItems
            .AsNoTracking()
            .Where(i => i.Code == text)
            .Select(i => new ScanItemDto(i.Id, i.Code, i.Name, i.Unit, i.IsLotTracked))
            .SingleOrDefaultAsync(cancellationToken);
        if (item is not null)
        {
            return new StockScanDto(
                StockScanKind.Item, text, null, item, null,
                await BalancesAsync(db.Stocks.Where(s => s.StockItemId == item.Id), cancellationToken));
        }

        return new Error(
            ErrorCodes.NotFound, $"{text}: bu kodla bir top, adres ya da stok kartı bulunamadı.", ErrorType.NotFound);
    }

    // Sıra, otomatik çıkıştaki sırayla aynıdır: önce genel adres, sonra adres kodu; her adreste en eski top.
    private static async Task<IReadOnlyList<ScanBalanceDto>> BalancesAsync(
        IQueryable<Stock> stocks, CancellationToken cancellationToken) =>
        await stocks
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(s => s.Quantity > 0)
            .OrderBy(s => s.Warehouse.Code).ThenByDescending(s => s.Location.IsDefault).ThenBy(s => s.Location.Code)
            .ThenBy(s => s.StockItem.Code).ThenBy(s => s.LotId)
            .Take(StockScanDto.MaxBalances)
            .Select(s => new ScanBalanceDto(
                s.WarehouseId, s.Warehouse.Code, s.Warehouse.Name, s.LocationId, s.Location.Code, s.StockItemId,
                s.StockItem.Code, s.StockItem.Name, s.StockItem.Unit, s.StockItem.IsLotTracked, s.LotId,
                s.Lot == null ? null : s.Lot.LotNo, s.Lot == null ? null : s.Lot.BatchNo, s.Quantity))
            .ToListAsync(cancellationToken);
}
