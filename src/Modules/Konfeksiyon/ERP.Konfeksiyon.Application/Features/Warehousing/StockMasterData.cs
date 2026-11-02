using ERP.Konfeksiyon.Domain.Authorization;
using ERP.Konfeksiyon.Application.Abstractions;
using System.Linq.Expressions;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Konfeksiyon.Application.Features.Costing;
using ERP.Core.Application.Features.MasterData;
using ERP.Konfeksiyon.Application.Features.MasterData;
using ERP.Konfeksiyon.Domain.Entities;
using ERP.Konfeksiyon.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERP.Konfeksiyon.Application.Features.Warehousing;

public sealed class WarehouseService(IKonfeksiyonDbContext db)
    : KonfeksiyonMasterDataService<ERP.Konfeksiyon.Domain.Entities.Warehouse, WarehouseDto, WarehouseRequest>(db)
{
    protected override DbSet<ERP.Konfeksiyon.Domain.Entities.Warehouse> Set => Db.Warehouses;
    protected override string Label => "Depo";
    protected override string CodeField => "code";

    protected override string CodeOf(WarehouseRequest request) => request.Code;
    protected override Expression<Func<ERP.Konfeksiyon.Domain.Entities.Warehouse, bool>> WithCode(string code) => e => e.Code == code;

    protected override Expression<Func<ERP.Konfeksiyon.Domain.Entities.Warehouse, bool>> Matches(string term) =>
        e => e.Code.Contains(term) || e.Name.Contains(term);

    protected override IQueryable<ERP.Konfeksiyon.Domain.Entities.Warehouse> Order(IQueryable<ERP.Konfeksiyon.Domain.Entities.Warehouse> query) =>
        query.OrderBy(e => e.Code);

    protected override Expression<Func<ERP.Konfeksiyon.Domain.Entities.Warehouse, WarehouseDto>> ToDto =>
        e => new WarehouseDto(e.Id, e.Code, e.Name, e.Type, e.IsActive);

    protected override Expression<Func<ERP.Konfeksiyon.Domain.Entities.Warehouse, LookupDto>> ToLookup =>
        e => new LookupDto(e.Id, e.Code, e.Name);

    protected override void Apply(WarehouseRequest request, ERP.Konfeksiyon.Domain.Entities.Warehouse entity, string code)
    {
        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.Type = request.Type;
        entity.IsActive = request.IsActive;

        // Her depo genel adresiyle açılır: adres belirtilmeyen hareketler oraya yazılır.
        if (entity.Id == 0 && entity.Locations.Count == 0)
        {
            entity.Locations.Add(WarehouseLocation.CreateDefault());
        }
    }

    // Hareket görmüş depo silinemez: hareket geçmişi depoya bağlıdır.
    protected override async Task<string?> FindUsageAsync(int id, CancellationToken cancellationToken) =>
        await Db.StockTransactions.AnyAsync(t => t.WarehouseId == id || t.CounterWarehouseId == id, cancellationToken)
            ? "stok hareketleri"
            : await Db.Shipments.AnyAsync(s => s.WarehouseId == id, cancellationToken) ? "sevkiyatlar" : null;
}

/// <summary>Depo adresleri (bölge / raf / göz). Adres kodu seviyelerden oluşur ve deposunda benzersizdir.</summary>
public sealed class WarehouseLocationService(IKonfeksiyonDbContext db)
    : KonfeksiyonMasterDataService<WarehouseLocation, WarehouseLocationDto, WarehouseLocationRequest>(db)
{
    protected override DbSet<WarehouseLocation> Set => Db.WarehouseLocations;
    protected override string Label => "Depo adresi";
    protected override string CodeField => "zone";

    protected override string CodeOf(WarehouseLocationRequest request) =>
        WarehouseLocation.ComposeCode(Segment(request.Zone)!, Segment(request.Rack), Segment(request.Bin));

    protected override Expression<Func<WarehouseLocation, bool>> WithCode(string code) => e => e.Code == code;

    protected override Expression<Func<WarehouseLocation, bool>> WithCode(string code, WarehouseLocationRequest request) =>
        e => e.Code == code && e.WarehouseId == request.WarehouseId;

    protected override Expression<Func<WarehouseLocation, bool>> Matches(string term) =>
        e => e.Code.Contains(term) || (e.Description != null && e.Description.Contains(term))
            || e.Warehouse.Code.Contains(term) || e.Warehouse.Name.Contains(term);

    protected override IQueryable<WarehouseLocation> Order(IQueryable<WarehouseLocation> query) =>
        query.OrderBy(e => e.Warehouse.Code).ThenByDescending(e => e.IsDefault).ThenBy(e => e.Code);

    protected override Expression<Func<WarehouseLocation, WarehouseLocationDto>> ToDto => e => new WarehouseLocationDto(
        e.Id, e.WarehouseId, e.Warehouse.Code, e.Warehouse.Name, e.Code, e.Zone, e.Rack, e.Bin, e.Description, e.IsDefault,
        Db.Stocks.Count(s => s.LocationId == e.Id && s.Quantity > 0), e.IsActive);

    protected override Expression<Func<WarehouseLocation, LookupDto>> ToLookup =>
        e => new LookupDto(e.Id, e.Code, e.Warehouse.Name);

    protected override void Apply(WarehouseLocationRequest request, WarehouseLocation entity, string code)
    {
        entity.WarehouseId = request.WarehouseId;
        entity.SetAddress(Segment(request.Zone)!, Segment(request.Rack), Segment(request.Bin));
        entity.Description = MasterDataRules.NullIfBlank(request.Description);
        entity.IsActive = request.IsActive;
    }

    protected override async Task<Error?> ValidateAsync(
        WarehouseLocationRequest request, WarehouseLocation? existing, CancellationToken cancellationToken)
    {
        if (existing is null)
        {
            return await RequireSelectableAsync(
                Db.Warehouses, request.WarehouseId, null, "warehouseId", "Depo", cancellationToken);
        }

        // Adresin stoğu ve hareketleri deposuyla birlikte kayıtlıdır.
        if (existing.WarehouseId != request.WarehouseId)
        {
            const string message = "Adresin deposu değiştirilemez; diğer depoda yeni bir adres açın.";
            return new Error(
                ErrorCodes.WarehouseLocationLocked, message, ErrorType.BusinessRule,
                new Dictionary<string, string[]> { ["warehouseId"] = [message] });
        }

        if (existing.IsDefault && !request.IsActive)
        {
            return new Error(
                ErrorCodes.WarehouseLocationLocked,
                "Deponun genel adresi pasif yapılamaz: adres belirtilmeyen hareketler buraya yazılır.");
        }

        return null;
    }

    // Stok görmüş adres silinemez: bakiye ve hareket geçmişi adrese bağlıdır.
    protected override async Task<string?> FindUsageAsync(int id, CancellationToken cancellationToken) =>
        await Db.WarehouseLocations.AnyAsync(l => l.Id == id && l.IsDefault, cancellationToken) ? "deponun genel adresi"
        : await Db.Stocks.AnyAsync(s => s.LocationId == id, cancellationToken) ? "stok kayıtları"
        : null;

    private static string? Segment(string? value) => MasterDataRules.NullIfBlank(value)?.ToUpperInvariant();
}

public sealed class StockItemService(IKonfeksiyonDbContext db, ICurrentPermissions permissions)
    : KonfeksiyonMasterDataService<StockItem, StockItemDto, StockItemRequest>(db)
{
    /// <summary>Maliyet değiştirme yetkisi yoksa mevcut birim maliyet korunur (gizli alan boş gelir).</summary>
    private bool _keepUnitCost;

    // Birim maliyet sipariş maliyetini belirler: görmek maliyet görüntüleme (ya da yönetme), değiştirmek maliyet yönetimi ister.
    protected override async Task<Func<StockItemDto, StockItemDto>?> PresenterAsync(CancellationToken cancellationToken) =>
        await CostRules.CanSeeCostAsync(permissions, cancellationToken) ? null : dto => dto with { UnitCost = null };

    protected override DbSet<StockItem> Set => Db.StockItems;
    protected override string Label => "Stok kartı";
    protected override string CodeField => "code";

    protected override string CodeOf(StockItemRequest request) => request.Code;
    protected override Expression<Func<StockItem, bool>> WithCode(string code) => e => e.Code == code;

    protected override Expression<Func<StockItem, bool>> Matches(string term) =>
        e => e.Code.Contains(term) || e.Name.Contains(term);

    protected override IQueryable<StockItem> Order(IQueryable<StockItem> query) => query.OrderBy(e => e.Code);

    protected override Expression<Func<StockItem, StockItemDto>> ToDto => e => new StockItemDto(
        e.Id, e.Code, e.Name, e.ItemType, e.Unit, e.ItemType == StockItemType.FinishedGood, e.IsActive, e.UnitCost,
        e.IsLotTracked, e.MinimumStock, e.ReorderQuantity, e.SupplierId, e.Supplier!.Name);

    protected override Expression<Func<StockItem, LookupDto>> ToLookup => e => new LookupDto(e.Id, e.Code, e.Name);

    protected override void Apply(StockItemRequest request, StockItem entity, string code)
    {
        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.ItemType = request.ItemType;
        entity.Unit = request.Unit;
        if (!_keepUnitCost)
        {
            entity.UnitCost = request.UnitCost;
        }
        entity.IsLotTracked = StockItem.LotTrackingFor(request.ItemType, request.IsLotTracked);
        entity.MinimumStock = request.MinimumStock;
        entity.ReorderQuantity = request.ReorderQuantity;
        entity.SupplierId = request.SupplierId;
        entity.IsActive = request.IsActive;
    }

    protected override async Task<Error?> ValidateAsync(
        StockItemRequest request, StockItem? existing, CancellationToken cancellationToken)
    {
        _keepUnitCost = !await permissions.HasAsync(KonfeksiyonPermissions.Costing.Manage, cancellationToken);
        if (_keepUnitCost && request.UnitCost is not null && request.UnitCost != existing?.UnitCost)
        {
            const string message = "Birim maliyeti değiştirme yetkiniz yok (Stok kartı birim maliyetini değiştirme).";
            return new Error(
                ErrorCodes.Forbidden, message, ErrorType.Forbidden,
                new Dictionary<string, string[]> { ["unitCost"] = [message] });
        }

        if (await RequireSelectableAsync(
                Db.Suppliers, request.SupplierId, existing?.SupplierId, "supplierId", "Tedarikçi", cancellationToken)
            is { } supplierError)
        {
            return supplierError;
        }

        if (existing is null)
        {
            return null;
        }

        if (existing.IsFinishedGood)
        {
            return new Error(
                ErrorCodes.StockItemSystemManaged,
                "Mamul kartları ürün, renk ve bedenden oluşturulur; elle düzenlenemez.");
        }

        // Birim değişirse geçmiş hareketlerin miktarları anlamını yitirir.
        if (existing.Unit != request.Unit
            && await Db.StockTransactions.AnyAsync(t => t.StockItemId == existing.Id, cancellationToken))
        {
            const string message = "Hareket görmüş stok kartının birimi değiştirilemez.";
            return new Error(
                ErrorCodes.StockItemUnitLocked, message, ErrorType.BusinessRule,
                new Dictionary<string, string[]> { ["unit"] = [message] });
        }

        // Takip biçimi değişirse eski hareketlerin topu olmaz ya da toplar sahipsiz kalır.
        if (existing.IsLotTracked != StockItem.LotTrackingFor(request.ItemType, request.IsLotTracked)
            && await Db.StockTransactions.AnyAsync(t => t.StockItemId == existing.Id, cancellationToken))
        {
            const string message = "Hareket görmüş stok kartının top / parti takibi (ve kumaş türü) değiştirilemez.";
            return new Error(
                ErrorCodes.StockItemLotTrackingLocked, message, ErrorType.BusinessRule,
                new Dictionary<string, string[]> { ["isLotTracked"] = [message] });
        }

        return null;
    }

    protected override async Task<string?> FindUsageAsync(int id, CancellationToken cancellationToken) =>
        await Db.StockTransactions.AnyAsync(t => t.StockItemId == id, cancellationToken) ? "stok hareketleri"
        : await Db.PurchaseOrderItems.AnyAsync(i => i.StockItemId == id, cancellationToken) ? "satın alma siparişleri"
        : await Db.ShipmentItems.AnyAsync(i => i.StockItemId == id, cancellationToken) ? "sevkiyatlar"
        : await Db.ProductMaterials.AnyAsync(m => m.StockItemId == id, cancellationToken) ? "ürün reçeteleri"
        : null;
}
