using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.Integrations;
using ERP.Konfeksiyon.Application.Features.Integrations;
using ERP.Core.Application.Features.MasterData;
using ERP.Konfeksiyon.Application.Features.Sales;
using ERP.Konfeksiyon.Application.Features.Warehousing;
using ERP.Core.Domain.Entities;
using ERP.Konfeksiyon.Domain.Entities;
using ERP.Konfeksiyon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Konfeksiyon.Application.Features.Shipping;

public interface IShipmentService
{
    Task<PagedResult<ShipmentListItemDto>> GetListAsync(ShipmentListQuery query, CancellationToken cancellationToken);
    Task<Result<ShipmentDto>> GetAsync(int id, CancellationToken cancellationToken);
    Task<Result<ShipmentDto>> CreateAsync(ShipmentRequest request, CancellationToken cancellationToken);
    Task<Result<ShipmentDto>> UpdateAsync(int id, ShipmentRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Toplanmış sevkiyatın irsaliyesini keser: kontrol adetleri toplananla tutmalıdır; kalemler toplandıkları
    /// adreslerden düşer ve sevkiyat kesinleşir.
    /// </summary>
    Task<Result<ShipmentDto>> DispatchAsync(int id, ShipmentDispatchRequest request, CancellationToken cancellationToken);

    Task<Result<ShipmentDto>> CancelAsync(int id, string rowVersion, CancellationToken cancellationToken);
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken);

    /// <summary>Sevkiyata bağlanabilecek (taslak ya da iptal olmayan) üretim siparişleri; seçim kutuları için.</summary>
    Task<IReadOnlyList<LookupDto>> LookupOrdersAsync(LookupQuery query, CancellationToken cancellationToken);

    /// <summary>Sevkiyata bağlanabilecek (onaylı) müşteri siparişleri; seçim kutuları için.</summary>
    Task<IReadOnlyList<LookupDto>> LookupSalesOrdersAsync(LookupQuery query, CancellationToken cancellationToken);
}

/// <summary>
/// Sevkiyatlar. Taslak, stoğu etkilemez; sevk etme, sevkiyat kilidi altında durumu değiştirir ve stok çıkışını
/// aynı transaction'da yazar: stok yetmezse hiçbir şey değişmez.
/// </summary>
public sealed class ShipmentService(
    IKonfeksiyonDbContext db,
    IAppLock appLock,
    IStockLedger stockLedger,
    IIntegrationOutbox outbox,
    IDocumentNumberGenerator numberGenerator,
    ICurrentUser currentUser,
    ISalesOrderFulfillment fulfillment,
    TimeProvider timeProvider,
    IOptions<ShippingOptions> options) : IShipmentService
{
    private const string SystemUser = "system";
    private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(10);

    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Sevkiyat bulunamadı.", ErrorType.NotFound);

    private static readonly Error ConcurrencyConflict = new(
        ErrorCodes.ConcurrencyConflict,
        "Sevkiyat siz açtıktan sonra başka bir kullanıcı tarafından değiştirildi. Sayfayı yenileyip tekrar deneyin.",
        ErrorType.Conflict);

    private static readonly IReadOnlyDictionary<ShipmentStatus, string> StatusLabels = new Dictionary<ShipmentStatus, string>
    {
        [ShipmentStatus.Draft] = "Taslak",
        [ShipmentStatus.Picking] = "Toplamada",
        [ShipmentStatus.Picked] = "Toplandı",
        [ShipmentStatus.Shipped] = "Sevk edildi",
        [ShipmentStatus.Cancelled] = "İptal",
    };

    private readonly ShippingOptions _options = options.Value;

    public Task<PagedResult<ShipmentListItemDto>> GetListAsync(ShipmentListQuery query, CancellationToken cancellationToken)
    {
        // Silinmiş müşteri / depo da adıyla görünür; sevkiyat kendisi silinmemiş olmalıdır.
        var shipments = db.Shipments.AsNoTracking().IgnoreQueryFilters().Where(s => !s.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            shipments = shipments.Where(s =>
                s.ShipmentNo.Contains(search)
                || (s.DocumentNo != null && s.DocumentNo.Contains(search))
                || s.Customer.Name.Contains(search)
                || (s.ProductionOrder != null && s.ProductionOrder.OrderNo.Contains(search))
                || (s.SalesOrder != null
                    && (s.SalesOrder.OrderNo.Contains(search)
                        || (s.SalesOrder.CustomerOrderNo != null && s.SalesOrder.CustomerOrderNo.Contains(search)))));
        }

        if (query.Status is { } status)
        {
            shipments = shipments.Where(s => s.Status == status);
        }

        if (query.CustomerId is { } customerId)
        {
            shipments = shipments.Where(s => s.CustomerId == customerId);
        }

        if (query.ProductionOrderId is { } orderId)
        {
            shipments = shipments.Where(s => s.ProductionOrderId == orderId);
        }

        if (query.SalesOrderId is { } salesOrderId)
        {
            shipments = shipments.Where(s => s.SalesOrderId == salesOrderId);
        }

        if (query.From is { } from)
        {
            shipments = shipments.Where(s => s.ShipDate >= from);
        }

        if (query.To is { } to)
        {
            shipments = shipments.Where(s => s.ShipDate <= to);
        }

        return shipments
            .OrderByDescending(s => s.Id)
            .Select(s => new ShipmentListItemDto(
                s.Id,
                s.ShipmentNo,
                s.CustomerId,
                s.Customer.Name,
                s.Warehouse.Code,
                s.ProductionOrder != null ? s.ProductionOrder.OrderNo : s.SalesOrder != null ? s.SalesOrder.OrderNo : null,
                s.ShipDate,
                s.Status,
                s.DocumentNo,
                s.Items.Count,
                s.Items.Sum(i => (decimal?)i.Quantity) ?? 0,
                s.SalesOrder != null ? s.SalesOrder.CustomerOrderNo : null,
                s.Cartons.Count,
                s.Items.Sum(i => (decimal?)i.PickedQuantity) ?? 0,
                s.PickingBy))
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<Result<ShipmentDto>> GetAsync(int id, CancellationToken cancellationToken)
    {
        var shipment = await db.Shipments
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(s => s.Customer)
            .Include(s => s.Warehouse)
            .Include(s => s.ProductionOrder)
            .Include(s => s.SalesOrder)
            .Include(s => s.Items).ThenInclude(i => i.StockItem)
            .SingleOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken);
        if (shipment is null)
        {
            return NotFound;
        }

        var stockItemIds = shipment.Items.Select(i => i.StockItemId).ToList();
        var available = await db.Stocks
            .AsNoTracking()
            .Where(s => s.WarehouseId == shipment.WarehouseId && stockItemIds.Contains(s.StockItemId))
            .GroupBy(s => s.StockItemId)
            .Select(g => new { StockItemId = g.Key, Quantity = g.Sum(s => s.Quantity) })
            .ToDictionaryAsync(s => s.StockItemId, s => s.Quantity, cancellationToken);

        var cartons = await CartonMatching.SummaryAsync(db, shipment, cancellationToken);

        return new ShipmentDto(
            shipment.Id,
            shipment.ShipmentNo,
            shipment.CustomerId,
            shipment.Customer.CustomerCode,
            shipment.Customer.Name,
            shipment.WarehouseId,
            shipment.Warehouse.Code,
            shipment.Warehouse.Name,
            shipment.ProductionOrderId,
            shipment.ProductionOrder?.OrderNo,
            shipment.ShipDate,
            shipment.DocumentNo,
            shipment.Carrier,
            shipment.Notes,
            shipment.Status,
            shipment.ShippedAt,
            shipment.ShippedBy,
            shipment.Items.Sum(i => i.Quantity),
            shipment.IsEditable,
            shipment.CanShip,
            shipment.CanCancel,
            RowVersionToken.From(shipment.RowVersion),
            shipment.CreatedAt,
            shipment.CreatedBy,
            shipment.Items
                .OrderBy(i => i.StockItem.Code, StringComparer.Ordinal)
                .Select(i => new ShipmentItemDto(
                    i.Id, i.StockItemId, i.StockItem.Code, i.StockItem.Name, i.StockItem.Unit, i.Quantity,
                    available.GetValueOrDefault(i.StockItemId), i.PickedQuantity, i.RequestedQuantity, i.CheckedQuantity))
                .ToList(),
            shipment.SalesOrderId,
            shipment.SalesOrder?.OrderNo,
            shipment.SalesOrder?.CustomerOrderNo,
            cartons,
            new ShipmentWorkflowDto(
                shipment.CanSendToPicking, shipment.CanReturnToDraft, shipment.CanCompletePicking, shipment.CanReopenPicking,
                shipment.PickingBy, shipment.PickingStartedAt, shipment.PickedAt, shipment.PickedBy, shipment.VehiclePlate,
                shipment.DriverName, shipment.DriverIdentityNo, shipment.DespatchUuid));
    }

    public async Task<Result<ShipmentDto>> CreateAsync(ShipmentRequest request, CancellationToken cancellationToken)
    {
        if (await ValidateAsync(request, null, cancellationToken) is { } error)
        {
            return error;
        }

        var shipment = new Shipment();
        Apply(request, shipment);
        shipment.SalesOrderId = await ResolveSalesOrderIdAsync(request, cancellationToken);
        foreach (var item in request.Items)
        {
            shipment.Items.Add(new ShipmentItem { StockItemId = item.StockItemId, Quantity = item.Quantity });
        }

        // Numara ve sevkiyat aynı transaction'da: kayıt başarısız olursa numara da geri alınır. Çağıranın açık
        // transaction'ı varsa (siparişten aktarma) onun parçası olur; commit'i çağıran yapar.
        await using var transaction = db.Database.CurrentTransaction is null
            ? await db.Database.BeginTransactionAsync(cancellationToken)
            : null;
        var year = timeProvider.GetLocalNow().Year;
        var sequence = await numberGenerator.NextAsync(DocumentSeries.Shipment, year, cancellationToken);
        shipment.ShipmentNo = $"{_options.NumberPrefix}-{year}-{sequence.ToString().PadLeft(_options.NumberDigits, '0')}";

        db.Shipments.Add(shipment);
        await db.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return await GetAsync(shipment.Id, cancellationToken);
    }

    public async Task<Result<ShipmentDto>> UpdateAsync(int id, ShipmentRequest request, CancellationToken cancellationToken)
    {
        var shipment = await db.Shipments.Include(s => s.Items).SingleOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (shipment is null)
        {
            return NotFound;
        }

        if (RowVersionToken.Check(shipment.RowVersion, request.RowVersion, ConcurrencyConflict) is { } versionError)
        {
            return versionError;
        }

        if (!shipment.IsEditable)
        {
            return NotEditable(shipment, "değiştirilemez");
        }

        if (await ValidateAsync(request, shipment, cancellationToken) is { } error)
        {
            return error;
        }

        Apply(request, shipment);
        shipment.SalesOrderId = await ResolveSalesOrderIdAsync(request, cancellationToken);
        SyncItems(shipment, request.Items);

        return await SaveAndReloadAsync(shipment, cancellationToken);
    }

    public async Task<Result<ShipmentDto>> DispatchAsync(int id, ShipmentDispatchRequest request, CancellationToken cancellationToken)
    {
        var rowVersion = request.RowVersion;
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // Aynı sevkiyatın iki kez sevk edilmesi (çift tıklama, iki kullanıcı) sıraya girer; ikincisi durumu görür.
        if (!await appLock.TryAcquireAsync($"shipment:{id}", LockTimeout, cancellationToken))
        {
            return new Error(
                ErrorCodes.ResourceBusy, "Bu sevkiyat şu an başka bir işlemle değiştiriliyor. Lütfen tekrar deneyin.",
                ErrorType.Conflict);
        }

        var shipment = await db.Shipments.Include(s => s.Items).Include(s => s.Picks)
            .SingleOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (shipment is null)
        {
            return NotFound;
        }

        if (RowVersionToken.Check(shipment.RowVersion, rowVersion, ConcurrencyConflict) is { } versionError)
        {
            return versionError;
        }

        if (shipment.Status != ShipmentStatus.Picked)
        {
            return new Error(
                ErrorCodes.InvalidStatusTransition,
                shipment.Status is ShipmentStatus.Draft or ShipmentStatus.Picking
                    ? "İrsaliye yalnızca toplaması bitmiş sevkiyata kesilir; önce depoda toplanmalıdır."
                    : $"{StatusLabels[shipment.Status]} durumundaki sevkiyata irsaliye kesilemez.");
        }

        var itemIds = shipment.Items.Select(i => i.StockItemId).ToList();
        var codes = await db.StockItems.AsNoTracking().IgnoreQueryFilters().Where(i => itemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, i => i.Code, cancellationToken);

        // Son kontrol: her kalem sayılmış olmalı ve sayılan adet toplananla (sevk edilecekle) birebir tutmalıdır.
        var checks = request.Checks.GroupBy(c => c.ShipmentItemId).ToDictionary(g => g.Key, g => g.Last().Quantity);
        var checkProblems = shipment.Items
            .Select(item => (item, counted: checks.TryGetValue(item.Id, out var counted) ? counted : (decimal?)null))
            .Where(x => x.counted != x.item.Quantity)
            .Select(x => x.counted is null
                ? $"{codes[x.item.StockItemId]}: kontrol adedi girilmemiş."
                : $"{codes[x.item.StockItemId]}: toplanan {x.item.Quantity:0.###}, sayılan {x.counted:0.###}.")
            .ToList();
        if (checkProblems.Count > 0)
        {
            return new Error(
                ErrorCodes.DespatchCheckMismatch,
                "Kontrol adetleri toplananla tutmuyor; farkı giderin ya da sevkiyatı yeniden toplamaya açın.",
                ErrorType.BusinessRule,
                new Dictionary<string, string[]> { ["checks"] = [.. checkProblems] });
        }

        if (!shipment.CanShip)
        {
            return new Error(ErrorCodes.ShipmentHasNoItems, "Kalemi olmayan sevkiyat sevk edilemez.");
        }

        if (await CheckLinkedOrdersAsync(shipment, cancellationToken) is { } linkError)
        {
            return linkError;
        }

        // Çeki listesi hazırlanmışsa kolilerdeki adetler kalemlerle birebir tutmalıdır; yüklenmemiş koli engel değildir.
        if (await CartonMatching.CheckAsync(db, shipment, cancellationToken) is { } cartonError)
        {
            return cartonError;
        }

        var stockItemIds = shipment.Items.Select(i => i.StockItemId).ToList();
        var stockItems = await db.StockItems
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(i => stockItemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, cancellationToken);

        if (await CheckOrderedQuantityAsync(shipment, stockItems, cancellationToken) is { } orderError)
        {
            return orderError;
        }

        shipment.DocumentNo = NullIfBlank(request.DocumentNo) ?? shipment.DocumentNo ?? await NextDespatchNoAsync(cancellationToken);
        shipment.Carrier = NullIfBlank(request.Carrier) ?? shipment.Carrier;
        shipment.VehiclePlate = NullIfBlank(request.VehiclePlate)?.ToUpperInvariant();
        shipment.DriverName = NullIfBlank(request.DriverName);
        shipment.DriverIdentityNo = NullIfBlank(request.DriverIdentityNo);
        if (await db.Shipments.AnyAsync(s => s.Id != shipment.Id && s.DocumentNo == shipment.DocumentNo, cancellationToken))
        {
            var message = $"{shipment.DocumentNo} numaralı irsaliye başka bir sevkiyatta kullanılmış.";
            return new Error(ErrorCodes.DespatchNoInUse, message, ErrorType.Conflict, new Dictionary<string, string[]> { ["documentNo"] = [message] });
        }

        // Kalemlerin toplandığı adresler: stok önce oradan düşer.
        var picked = shipment.Picks
            .GroupBy(p => shipment.Items.Single(i => i.Id == p.ShipmentItemId).StockItemId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<(int LocationId, decimal Quantity)>)g.GroupBy(p => p.LocationId)
                    .Select(l => (l.Key, l.Sum(p => p.Quantity))).ToList());

        var stockError = await stockLedger.PostShipmentAsync(
            shipment.WarehouseId,
            shipment.Id,
            shipment.ProductionOrderId,
            shipment.SalesOrderId,
            shipment.Items.Select(i => (i.StockItemId, stockItems[i.StockItemId].Code, i.Quantity)).ToList(),
            shipment.DocumentNo,
            cancellationToken,
            picked);
        if (stockError is not null)
        {
            return stockError;
        }

        foreach (var item in shipment.Items)
        {
            item.CheckedQuantity = checks[item.Id];
        }

        shipment.Ship(timeProvider.GetUtcNow().UtcDateTime, currentUser.UserName ?? SystemUser, Guid.NewGuid());
        await EnqueueShippedAsync(shipment, stockItems, cancellationToken);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ConcurrencyConflict;
        }

        // Siparişin tamamı sevk edildiyse sipariş de kendiliğinden kapanır.
        if (shipment.SalesOrderId is { } salesOrderId)
        {
            await fulfillment.CloseIfFullyShippedAsync(salesOrderId, shipment.ShipmentNo, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<Result<ShipmentDto>> CancelAsync(int id, string rowVersion, CancellationToken cancellationToken)
    {
        var shipment = await db.Shipments.Include(s => s.Items).SingleOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (shipment is null)
        {
            return NotFound;
        }

        if (RowVersionToken.Check(shipment.RowVersion, rowVersion, ConcurrencyConflict) is { } versionError)
        {
            return versionError;
        }

        if (!shipment.CanCancel)
        {
            return new Error(
                ErrorCodes.InvalidStatusTransition,
                shipment.Status == ShipmentStatus.Shipped
                    ? "Sevk edilmiş sevkiyat iptal edilemez; iade, stok girişiyle kaydedilir."
                    : "Sevkiyat zaten iptal edilmiş.");
        }

        shipment.Cancel();
        return await SaveAndReloadAsync(shipment, cancellationToken);
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var shipment = await db.Shipments.SingleOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (shipment is null)
        {
            return NotFound;
        }

        if (!shipment.IsEditable)
        {
            return NotEditable(shipment, "silinemez");
        }

        // Soft delete: kayıt ve numarası korunur, numara yeniden kullanılmaz.
        db.Shipments.Remove(shipment);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<IReadOnlyList<LookupDto>> LookupOrdersAsync(LookupQuery query, CancellationToken cancellationToken)
    {
        var orders = db.ProductionOrders
            .AsNoTracking()
            .Where(o => o.Status != ProductionOrderStatus.Draft && o.Status != ProductionOrderStatus.Cancelled);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            orders = orders.Where(o =>
                o.OrderNo.Contains(search) || o.Product.ProductCode.Contains(search) || o.Customer.Name.Contains(search));
        }

        var result = await orders
            .OrderByDescending(o => o.Id)
            .Take(query.NormalizedTake)
            .Select(o => new LookupDto(o.Id, o.OrderNo, o.Customer.Name + " · " + o.Product.ProductCode))
            .ToListAsync(cancellationToken);

        // Formda seçili olan sipariş, liste sınırının dışında kalsa da listede yer alır.
        if (query.IncludeId is { } includeId && result.All(item => item.Id != includeId))
        {
            result.InsertRange(
                0,
                await db.ProductionOrders.AsNoTracking().Where(o => o.Id == includeId)
                    .Select(o => new LookupDto(o.Id, o.OrderNo, o.Customer.Name + " · " + o.Product.ProductCode))
                    .ToListAsync(cancellationToken));
        }

        return result;
    }

    public async Task<IReadOnlyList<LookupDto>> LookupSalesOrdersAsync(LookupQuery query, CancellationToken cancellationToken)
    {
        var orders = db.SalesOrders.AsNoTracking().Where(o => o.Status == SalesOrderStatus.Approved);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            orders = orders.Where(o =>
                o.OrderNo.Contains(search) || o.CustomerOrderNo!.Contains(search) || o.Customer.Name.Contains(search));
        }

        var result = await orders
            .OrderByDescending(o => o.Id)
            .Take(query.NormalizedTake)
            .Select(o => new LookupDto(o.Id, o.OrderNo, o.Customer.Name))
            .ToListAsync(cancellationToken);

        // Formda seçili olan sipariş, liste sınırının dışında kalsa da (ya da artık onaylı olmasa da) listede yer alır.
        if (query.IncludeId is { } includeId && result.All(item => item.Id != includeId))
        {
            result.InsertRange(
                0,
                await db.SalesOrders.AsNoTracking().Where(o => o.Id == includeId)
                    .Select(o => new LookupDto(o.Id, o.OrderNo, o.Customer.Name))
                    .ToListAsync(cancellationToken));
        }

        return result;
    }

    /// <summary>
    /// Sevkiyatın müşteri siparişi: açıkça seçilen sipariş, yoksa bağlı üretim siparişinin müşteri siparişi.
    /// </summary>
    private async Task<int?> ResolveSalesOrderIdAsync(ShipmentRequest request, CancellationToken cancellationToken) =>
        request.SalesOrderId ?? (request.ProductionOrderId is { } orderId
            ? await db.ProductionOrders.Where(o => o.Id == orderId).Select(o => o.SalesOrderId).SingleOrDefaultAsync(cancellationToken)
            : null);

    /// <summary>
    /// Siparişe bağlı sevkiyat, siparişte olmayan bir renk-bedeni ya da sipariş adedinden (tolerans dahil) fazlasını
    /// sevk edemez. Müşteri siparişine bağlıysa onun kalemleri, yalnızca üretim siparişine bağlıysa o siparişin
    /// kalemleri esas alınır; daha önce sevk edilenler düşülür.
    /// </summary>
    private async Task<Error?> CheckOrderedQuantityAsync(
        Shipment shipment, IReadOnlyDictionary<int, StockItem> stockItems, CancellationToken cancellationToken)
    {
        Dictionary<(int ProductId, int ColorId, int SizeId), int> ordered;
        IQueryable<ShipmentItem> earlier;
        string orderNo;
        if (shipment.SalesOrderId is { } salesOrderId)
        {
            // Aynı siparişin iki sevkiyatı aynı anda sevk edilirse toplam, sırayla denetlenir.
            if (!await appLock.TryAcquireAsync($"sales-order-shipping:{salesOrderId}", LockTimeout, cancellationToken))
            {
                return OrderBusy;
            }

            orderNo = await db.SalesOrders.IgnoreQueryFilters().Where(o => o.Id == salesOrderId).Select(o => o.OrderNo).SingleAsync(cancellationToken);
            ordered = (await db.SalesOrderLines
                    .Where(l => l.SalesOrderId == salesOrderId)
                    .Select(l => new { l.ProductId, l.ColorId, l.SizeId, l.Quantity })
                    .ToListAsync(cancellationToken))
                .ToDictionary(l => (l.ProductId, l.ColorId, l.SizeId), l => l.Quantity);
            earlier = db.ShipmentItems.Where(i => i.Shipment.SalesOrderId == salesOrderId);
        }
        else if (shipment.ProductionOrderId is { } orderId)
        {
            if (!await appLock.TryAcquireAsync($"production-order-shipping:{orderId}", LockTimeout, cancellationToken))
            {
                return OrderBusy;
            }

            var order = await db.ProductionOrders
                .IgnoreQueryFilters()
                .Where(o => o.Id == orderId)
                .Select(o => new { o.OrderNo, o.ProductId, Items = o.Items.Select(i => new { i.ColorId, i.SizeId, i.Quantity }).ToList() })
                .SingleAsync(cancellationToken);
            orderNo = order.OrderNo;
            ordered = order.Items.ToDictionary(i => (order.ProductId, i.ColorId, i.SizeId), i => i.Quantity);
            earlier = db.ShipmentItems.Where(i => i.Shipment.ProductionOrderId == orderId);
        }
        else
        {
            return null;
        }

        var shipped = (await earlier
                .Where(i => i.Shipment.Status == ShipmentStatus.Shipped && i.ShipmentId != shipment.Id)
                .GroupBy(i => new { i.StockItem.ProductId, i.StockItem.ColorId, i.StockItem.SizeId })
                .Select(g => new { g.Key.ProductId, g.Key.ColorId, g.Key.SizeId, Quantity = g.Sum(i => i.Quantity) })
                .ToListAsync(cancellationToken))
            .Where(g => g.ProductId is not null && g.ColorId is not null && g.SizeId is not null)
            .ToDictionary(g => (g.ProductId!.Value, g.ColorId!.Value, g.SizeId!.Value), g => g.Quantity);

        var problems = new List<string>();
        foreach (var item in shipment.Items)
        {
            var card = stockItems[item.StockItemId];
            if (card.ProductId is not { } productId || card.ColorId is not { } colorId || card.SizeId is not { } sizeId
                || !ordered.TryGetValue((productId, colorId, sizeId), out var quantity))
            {
                problems.Add($"{card.Code}: {orderNo} siparişinde bu ürün / renk / beden yok.");
                continue;
            }

            var allowed = Math.Floor(quantity * (1 + _options.OverShipTolerancePercent / 100m));
            var before = shipped.GetValueOrDefault((productId, colorId, sizeId));
            if (before + item.Quantity > allowed)
            {
                problems.Add(
                    $"{card.Code}: sipariş {quantity:0.###}, daha önce sevk edilen {before:0.###}; en fazla {Math.Max(allowed - before, 0):0.###} sevk edilebilir.");
            }
        }

        return problems.Count == 0
            ? null
            : new Error(
                ErrorCodes.ShipmentExceedsOrder, $"Sevkiyat {orderNo} siparişini aşıyor.", ErrorType.BusinessRule,
                new Dictionary<string, string[]> { ["items"] = [.. problems] });
    }

    /// <summary>
    /// Taslak kaydedildikten sonra bağlı siparişler değişmiş olabilir: sevk anında müşteri siparişi hâlâ onaylı,
    /// üretim siparişi taslak ya da iptal değil olmalıdır (bağ kurulurken uygulanan kuralların aynısı).
    /// </summary>
    private async Task<Error?> CheckLinkedOrdersAsync(Shipment shipment, CancellationToken cancellationToken)
    {
        if (shipment.SalesOrderId is { } salesOrderId)
        {
            var salesOrder = await db.SalesOrders
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(o => o.Id == salesOrderId)
                .Select(o => new { o.OrderNo, o.Status, o.IsDeleted })
                .SingleOrDefaultAsync(cancellationToken);
            if (salesOrder is null || salesOrder.IsDeleted)
            {
                return LinkError("Sevkiyatın bağlı olduğu müşteri siparişi silinmiş; sevkiyat sevk edilemez.");
            }

            if (salesOrder.Status != SalesOrderStatus.Approved)
            {
                var state = salesOrder.Status switch
                {
                    SalesOrderStatus.Draft => "taslak",
                    SalesOrderStatus.Submitted => "onay bekliyor",
                    SalesOrderStatus.Closed => "kapatılmış",
                    _ => "iptal edilmiş",
                };
                return LinkError(
                    $"{salesOrder.OrderNo} müşteri siparişi {state} durumda; yalnızca onaylı siparişe sevkiyat yapılabilir.");
            }
        }

        if (shipment.ProductionOrderId is { } orderId)
        {
            var order = await db.ProductionOrders
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(o => o.Id == orderId)
                .Select(o => new { o.OrderNo, o.Status, o.IsDeleted })
                .SingleOrDefaultAsync(cancellationToken);
            if (order is null || order.IsDeleted)
            {
                return LinkError("Sevkiyatın bağlı olduğu iş emri silinmiş; sevkiyat sevk edilemez.");
            }

            if (order.Status is ProductionOrderStatus.Draft or ProductionOrderStatus.Cancelled)
            {
                return LinkError(
                    $"{order.OrderNo} iş emri {(order.Status == ProductionOrderStatus.Draft ? "taslak" : "iptal edilmiş")} durumda; sevkiyat sevk edilemez.");
            }
        }

        return null;

        static Error LinkError(string message) => new(ErrorCodes.ShipmentOrderMismatch, message, ErrorType.Conflict);
    }

    private static readonly Error OrderBusy = new(
        ErrorCodes.ResourceBusy, "Siparişin başka bir sevkiyatı şu an işleniyor. Lütfen tekrar deneyin.", ErrorType.Conflict);

    private async Task<Error?> ValidateAsync(ShipmentRequest request, Shipment? existing, CancellationToken cancellationToken)
    {
        var stockItemIds = request.Items.Select(i => i.StockItemId).Distinct().ToList();
        var referenceError =
            await ReferenceValidation.RequireSelectableAsync(
                db.Customers, request.CustomerId, existing?.CustomerId, "customerId", "Müşteri", cancellationToken)
            ?? await ReferenceValidation.RequireSelectableAsync(
                db.Warehouses, request.WarehouseId, existing?.WarehouseId, "warehouseId", "Depo", cancellationToken)
            ?? await ReferenceValidation.RequireSelectableAsync(
                db.StockItems, stockItemIds, existing?.Items.Select(i => i.StockItemId).ToHashSet() ?? [], "items",
                "Stok kartı", cancellationToken);
        if (referenceError is not null)
        {
            return referenceError;
        }

        var stockItems = await db.StockItems
            .AsNoTracking()
            .Where(i => stockItemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, cancellationToken);

        if (stockItems.Values.FirstOrDefault(i => !i.IsFinishedGood) is { } material)
        {
            return ItemError(
                ErrorCodes.ShipmentItemNotFinishedGood,
                $"{material.Code} bir mamul kartı değil; yalnızca mamuller sevk edilebilir.");
        }

        if (request.Items.FirstOrDefault(i => !stockItems[i.StockItemId].Accepts(i.Quantity)) is { } fractional)
        {
            return ItemError(
                ErrorCodes.StockQuantityInvalid,
                $"{stockItems[fractional.StockItemId].Code}: adetle izlenen stok kartında miktar tam sayı olmalıdır.",
                ErrorType.Validation);
        }

        if (request.SalesOrderId is { } salesOrderId)
        {
            var salesOrder = await db.SalesOrders
                .AsNoTracking()
                .Where(o => o.Id == salesOrderId)
                .Select(o => new { o.OrderNo, o.CustomerId, o.Status, ProductIds = o.Lines.Select(l => l.ProductId).Distinct().ToList() })
                .SingleOrDefaultAsync(cancellationToken);
            if (salesOrder is null)
            {
                return ReferenceValidation.InvalidReference("salesOrderId", "Müşteri siparişi bulunamadı.");
            }

            // Kayıtlı sevkiyatın siparişi sonradan kapanmış olabilir; yeni bağ yalnızca onaylı siparişe kurulur.
            if (salesOrder.Status != SalesOrderStatus.Approved && existing?.SalesOrderId != salesOrderId)
            {
                return SalesOrderError("Sevkiyat yalnızca onaylı müşteri siparişine bağlanabilir.");
            }

            if (salesOrder.CustomerId != request.CustomerId)
            {
                return SalesOrderError($"{salesOrder.OrderNo} siparişi başka bir müşteriye ait.");
            }

            if (stockItems.Values.FirstOrDefault(i => i.ProductId is null || !salesOrder.ProductIds.Contains(i.ProductId.Value)) is { } stranger)
            {
                return ItemError(ErrorCodes.ShipmentOrderMismatch, $"{stranger.Code}, {salesOrder.OrderNo} siparişinin ürünü değil.");
            }
        }

        if (request.ProductionOrderId is not { } orderId)
        {
            return null;
        }

        var order = await db.ProductionOrders
            .AsNoTracking()
            .Where(o => o.Id == orderId)
            .Select(o => new { o.OrderNo, o.CustomerId, o.ProductId, o.Status, o.SalesOrderId })
            .SingleOrDefaultAsync(cancellationToken);
        if (order is null)
        {
            return ReferenceValidation.InvalidReference("productionOrderId", "İş emri bulunamadı.");
        }

        if (order.Status is ProductionOrderStatus.Draft or ProductionOrderStatus.Cancelled)
        {
            return OrderError("Taslak ya da iptal edilmiş siparişe sevkiyat bağlanamaz.");
        }

        if (order.CustomerId != request.CustomerId)
        {
            return OrderError($"{order.OrderNo} siparişi başka bir müşteriye ait.");
        }

        if (request.SalesOrderId is not null && order.SalesOrderId != request.SalesOrderId)
        {
            return OrderError($"{order.OrderNo} iş emri seçilen müşteri siparişine ait değil.");
        }

        return stockItems.Values.FirstOrDefault(i => i.ProductId != order.ProductId) is { } foreign
            ? ItemError(ErrorCodes.ShipmentOrderMismatch, $"{foreign.Code}, {order.OrderNo} siparişinin ürünü değil.")
            : null;

        static Error OrderError(string message) => new(
            ErrorCodes.ShipmentOrderMismatch, message, ErrorType.BusinessRule,
            new Dictionary<string, string[]> { ["productionOrderId"] = [message] });

        static Error SalesOrderError(string message) => new(
            ErrorCodes.ShipmentOrderMismatch, message, ErrorType.BusinessRule,
            new Dictionary<string, string[]> { ["salesOrderId"] = [message] });
    }

    private static void Apply(ShipmentRequest request, Shipment shipment)
    {
        shipment.CustomerId = request.CustomerId;
        shipment.WarehouseId = request.WarehouseId;
        shipment.ProductionOrderId = request.ProductionOrderId;
        shipment.ShipDate = request.ShipDate;
        shipment.DocumentNo = NullIfBlank(request.DocumentNo);
        shipment.Carrier = NullIfBlank(request.Carrier);
        shipment.Notes = NullIfBlank(request.Notes);
    }

    /// <summary>Kalemleri istekle eşitler: kalan kalem güncellenir, eksilen silinir, yeni gelen eklenir.</summary>
    private void SyncItems(Shipment shipment, IReadOnlyList<ShipmentItemRequest> requested)
    {
        var wanted = requested.ToDictionary(i => i.StockItemId, i => i.Quantity);

        foreach (var item in shipment.Items.ToList())
        {
            if (wanted.Remove(item.StockItemId, out var quantity))
            {
                item.Quantity = quantity;
            }
            else
            {
                db.ShipmentItems.Remove(item);
            }
        }

        foreach (var (stockItemId, quantity) in wanted)
        {
            db.ShipmentItems.Add(new ShipmentItem { ShipmentId = shipment.Id, StockItemId = stockItemId, Quantity = quantity });
        }
    }

    private async Task<Result<ShipmentDto>> SaveAndReloadAsync(Shipment shipment, CancellationToken cancellationToken)
    {
        // Yalnızca kalemler değişse bile başlık güncellenir: rowversion ilerler.
        db.Entry(shipment).State = EntityState.Modified;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ConcurrencyConflict;
        }

        return await GetAsync(shipment.Id, cancellationToken);
    }

    /// <summary>Sevk olayını, sevkiyatla aynı kayıtta dış sistemlere duyurulmak üzere yazar.</summary>
    private async Task EnqueueShippedAsync(
        Shipment shipment, IReadOnlyDictionary<int, StockItem> stockItems, CancellationToken cancellationToken)
    {
        var header = await db.Shipments
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(s => s.Id == shipment.Id)
            .Select(s => new
            {
                s.Customer.CustomerCode,
                CustomerName = s.Customer.Name,
                s.Customer.TaxNumber,
                WarehouseCode = s.Warehouse.Code,
                OrderNo = s.ProductionOrder == null ? null : s.ProductionOrder.OrderNo,
            })
            .SingleAsync(cancellationToken);

        outbox.Enqueue(
            IntegrationEventTypes.ShipmentShipped,
            nameof(Shipment),
            shipment.Id,
            shipment.ShipmentNo,
            new ShipmentShippedPayload(
                shipment.ShipmentNo,
                shipment.ShipDate,
                shipment.ShippedAt!.Value,
                shipment.ShippedBy!,
                shipment.DocumentNo,
                shipment.Carrier,
                shipment.Notes,
                new PartyPayload(header.CustomerCode, header.CustomerName, header.TaxNumber),
                header.WarehouseCode,
                header.OrderNo,
                shipment.Items
                    .Select(i =>
                    {
                        var item = stockItems[i.StockItemId];
                        return new StockLinePayload(item.Code, item.Name, item.Unit, i.Quantity);
                    })
                    .OrderBy(l => l.ItemCode, StringComparer.Ordinal)
                    .ToList()));
    }

    /// <summary>Sıradaki irsaliye numarası: {önek}{yıl}{9 haneli sıra} (e-İrsaliye belge numarası biçimi).</summary>
    private async Task<string> NextDespatchNoAsync(CancellationToken cancellationToken)
    {
        var year = timeProvider.GetLocalNow().Year;
        var sequence = await numberGenerator.NextAsync(DespatchSeries, year, cancellationToken);
        return $"{_options.DespatchPrefix}{year}{sequence.ToString().PadLeft(9, '0')}";
    }

    private const string DespatchSeries = "KonfeksiyonDespatch";

    private static Error NotEditable(Shipment shipment, string verb) => new(
        ErrorCodes.ShipmentNotEditable, $"{StatusLabels[shipment.Status]} durumundaki sevkiyat {verb}.");

    private static Error ItemError(string code, string message, ErrorType type = ErrorType.BusinessRule) => new(
        code, message, type, new Dictionary<string, string[]> { ["items"] = [message] });
}
