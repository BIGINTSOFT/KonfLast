using ERP.Konfeksiyon.Application.Abstractions;
using System.Globalization;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Konfeksiyon.Application.Features.Costing;
using ERP.Konfeksiyon.Application.Features.Bundles;
using ERP.Konfeksiyon.Domain.Entities;
using ERP.Core.Domain.Enums;
using ERP.Konfeksiyon.Domain.Enums;
using ERP.Konfeksiyon.Domain.Routing;
using Microsoft.EntityFrameworkCore;

namespace ERP.Konfeksiyon.Application.Features.Warehousing;

public interface IStockService
{
    /// <summary>Depo ve kart bazında bakiyeler (deponun tüm adreslerinin toplamı).</summary>
    Task<PagedResult<StockBalanceDto>> GetBalancesAsync(StockBalanceQuery query, CancellationToken cancellationToken);

    /// <summary>Adres ve kart bazında bakiyeler.</summary>
    Task<PagedResult<StockLocationBalanceDto>> GetLocationBalancesAsync(
        StockLocationBalanceQuery query, CancellationToken cancellationToken);

    /// <summary>Top ve adres bazında bakiyeler (takipli kartlar), topun özellikleriyle.</summary>
    Task<PagedResult<StockLotBalanceDto>> GetLotBalancesAsync(StockLotBalanceQuery query, CancellationToken cancellationToken);

    /// <summary>Topun özelliklerini (parti, ton, en, tedarikçi top no) düzeltir.</summary>
    Task<Result> UpdateLotAsync(int lotId, StockLotUpdateRequest request, CancellationToken cancellationToken);

    /// <summary>Deponun adresleri (hareket formlarındaki seçim için): önce genel adres, sonra kod sırasıyla.</summary>
    Task<IReadOnlyList<WarehouseLocationOptionDto>> GetLocationOptionsAsync(int warehouseId, CancellationToken cancellationToken);

    /// <summary>Stok hareketleri, en yeniden eskiye.</summary>
    Task<PagedResult<StockTransactionDto>> GetTransactionsAsync(StockTransactionQuery query, CancellationToken cancellationToken);

    /// <summary>Tamamlanmış, henüz depoya teslim alınmamış demetler (en eski önce).</summary>
    Task<PagedResult<ReceivableBundleDto>> GetReceivableBundlesAsync(
        ReceivableBundleQuery query, CancellationToken cancellationToken);

    Task<Result<StockMovementDto>> ReceiveAsync(StockReceiptRequest request, CancellationToken cancellationToken);
    Task<Result<StockMovementDto>> IssueAsync(StockIssueRequest request, CancellationToken cancellationToken);
    Task<Result<StockMovementDto>> TransferAsync(StockTransferRequest request, CancellationToken cancellationToken);

    /// <summary>Sayım düzeltmesi: bakiyeyi sayılan miktara getirir.</summary>
    Task<Result<StockMovementDto>> AdjustAsync(StockAdjustmentRequest request, CancellationToken cancellationToken);

    /// <summary>Tamamlanmış demetin sağlam adedini mamul olarak depoya alır ve demeti kapatır.</summary>
    Task<Result<StockMovementDto>> ReceiveFromProductionAsync(
        ProductionReceiptRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Ürün / renk / beden için mamul kartını açar (varsa onu döner). Mamul kartlarını normalde üretimden teslim
    /// alma açar; bu, sistemde üretilmemiş mamulün (açılış stoğu, iade) stoğa girilebilmesi içindir.
    /// </summary>
    Task<Result<FinishedGoodCardDto>> OpenFinishedGoodCardAsync(FinishedGoodCardRequest request, CancellationToken cancellationToken);
}

/// <summary>Mal kabulde gelen bir top: miktarı ve özellikleri.</summary>
public sealed record ReceivedLot(decimal Quantity, StockLotRequest Attributes);

/// <param name="UnitCost">Sistem para biriminde alış fiyatı; verilirse kartın ortalama maliyeti güncellenir.</param>
/// <param name="Lots">Takipli kartta gelen toplar; verilmezse kalemin tamamı tek top olarak açılır.</param>
public sealed record PurchaseReceiptLine(
    int StockItemId, decimal Quantity, decimal? UnitCost, IReadOnlyList<ReceivedLot>? Lots = null);

/// <summary>Başka modüllerin (satın alma) kendi işlemlerinin parçası olarak stok yazması için.</summary>
public interface IStockLedger
{
    /// <summary>
    /// Satın alma siparişinden teslim alınan kalemleri deponun genel adresine girer: bakiyeyi artırır ve
    /// hareketleri ekler; takipli kartta her top için ayrı top kaydı ve hareket yazılır. Çağıranın açık
    /// transaction'ı içinde çalışır ve kaydetmez; kaydı ve commit'i çağıran yapar.
    /// </summary>
    Task<Error?> PostPurchaseReceiptAsync(
        int warehouseId,
        int purchaseOrderId,
        IReadOnlyList<PurchaseReceiptLine> lines,
        string? documentNo,
        string? note,
        CancellationToken cancellationToken);

    /// <summary>
    /// Yeni açılan demetlerin reçete malzemesini depodan düşer; her demet için ayrı hareket yazılır (iptalde
    /// geri alınabilsin). Malzeme önce verilen depodan, yetmeyen kısmı öteki aktif hammadde depolarından alınır;
    /// her depoda adreslerden sırayla (önce genel adres), takipli kartta her adreste en eski toptan başlanarak.
    /// Herhangi bir malzeme
    /// yetmezse hiçbir şey yazılmaz ve eksikleri sayan hata döner. Çağıranın açık transaction'ı içinde çalışır
    /// ve kaydetmez.
    /// </summary>
    Task<Error?> PostProductionIssueAsync(
        int warehouseId,
        int productionOrderId,
        string documentNo,
        IReadOnlyList<(Bundle Bundle, int StockItemId, decimal Quantity)> lines,
        CancellationToken cancellationToken);

    /// <summary>
    /// İptal edilen demetler için düşülmüş malzemeyi çıkıldığı adrese (ve topa) geri alır. Çağıranın açık transaction'ı
    /// içinde çalışır ve kaydetmez.
    /// </summary>
    Task<Error?> PostProductionReturnAsync(IReadOnlyCollection<int> bundleIds, CancellationToken cancellationToken);

    /// <summary>
    /// Kesimde kullanılan kumaşı belirtilen toplardan üretime çıkış olarak düşer: top hangi depoda / adreste duruyorsa
    /// oradan (depo kimliği, sonra adres sırasıyla). Hareketler demete değil siparişe ve belge numarasına (kesim planı no)
    /// bağlanır: kesilmiş kumaş depoya dönmez, demet iptalinde iade edilmez. Başka siparişlere ayrılmış stok kullanılmaz;
    /// bu siparişin rezervi çıkılan kadar azalır. Herhangi bir top yetmezse hiçbir şey yazılmaz ve eksikleri sayan hata
    /// döner. Çağıranın açık transaction'ı içinde çalışır ve kaydetmez.
    /// </summary>
    Task<Error?> PostLotIssueAsync(
        int productionOrderId,
        string documentNo,
        string? note,
        IReadOnlyList<(int LotId, decimal Quantity)> lines,
        CancellationToken cancellationToken);

    /// <summary>
    /// Sevk edilen kalemleri deponun adreslerinden sırayla (önce genel adres) düşer ve hareketleri ekler; stok
    /// yetmezse hata döner ve hiçbir şey yazılmamış sayılır (çağıran transaction'ı geri alır). Çağıranın açık
    /// transaction'ı içinde çalışır.
    /// </summary>
    Task<Error?> PostShipmentAsync(
        int warehouseId,
        int shipmentId,
        int? productionOrderId,
        int? salesOrderId,
        IReadOnlyList<(int StockItemId, string ItemCode, decimal Quantity)> lines,
        string? documentNo,
        CancellationToken cancellationToken);

    /// <summary>
    /// Müşteri siparişine hazır mamul ayırır: her kalemin ayırması istenen adede, stok yetmiyorsa kullanılabilir stok
    /// kadarına getirilir (başka siparişlere ayrılmış stok kullanılmaz). Çağıranın açık transaction'ı içinde çalışır,
    /// kaydetmez.
    /// </summary>
    Task<Error?> ReserveForSalesOrderAsync(
        int salesOrderId, IReadOnlyList<(int LineId, int StockItemId, decimal Target)> targets, CancellationToken cancellationToken);

    /// <summary>Müşteri siparişinin bütün ayırmalarını bırakır (kaydetmez).</summary>
    Task ReleaseSalesOrderReservationsAsync(int salesOrderId, CancellationToken cancellationToken);

    /// <summary>
    /// Siparişe depoda malzeme ayırır: her kartın rezervasyonu istenen miktara, stok yetmiyorsa kullanılabilir
    /// stok kadarına getirilir (başka siparişlere ayrılmış stok kullanılabilir sayılmaz). Çağıranın açık
    /// transaction'ı içinde çalışır ve kaydetmez.
    /// </summary>
    Task<Error?> ReserveAsync(
        int warehouseId, int productionOrderId, IReadOnlyList<(int StockItemId, decimal Quantity)> targets,
        CancellationToken cancellationToken);

    /// <summary>Siparişin tüm rezervasyonlarını bırakır (stok yeniden kullanılabilir olur). Kaydetmez.</summary>
    Task ReleaseReservationsAsync(int productionOrderId, CancellationToken cancellationToken);

    /// <summary>
    /// Onaylanan sayım fişinin satırlarını stoğa işler: her satırın bakiyesi sayılan miktara getirilir, fark
    /// (kilit altında okunan güncel bakiyeye göre) sayım düzeltmesi olarak yazılır. Çağıranın açık transaction'ı
    /// içinde çalışır ve kaydetmez.
    /// </summary>
    Task<Error?> PostCountAsync(
        int warehouseId, string countNo, IReadOnlyList<(int LocationId, int StockItemId, int? LotId, decimal Counted)> lines,
        CancellationToken cancellationToken);
}

/// <summary>
/// Stok hareketleri. Bakiye yalnızca burada, bir hareket kaydıyla birlikte değişir. Bakiye adres bazında
/// tutulur; deponun bakiyesi adreslerinin toplamıdır.
///
/// Eşzamanlılık: her hareket (depo, kart) kilidi altında tek transaction'da yürür; kilit deponun tüm
/// adreslerini kapsar (depo içi adres değişimi de aynı kilitle yürür). Kilitler her zaman depo
/// kimliği sırasıyla alınır (iki yönlü transferler kilitlenmez). Bakiyenin rowversion'ı ve hareketlerin tekil
/// istek kimliği son güvencedir. Aynı istek kimliğiyle yinelenen istek hareketi ikinci kez yazmaz.
/// </summary>
public sealed class StockService(
    IKonfeksiyonDbContext db, IAppLock appLock, ICurrentUser currentUser, ICurrentPermissions permissions, TimeProvider timeProvider)
    : IStockService, IStockLedger
{
    private const string SystemUser = "system";
    private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(10);
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    private static readonly Error Busy = new(
        ErrorCodes.ResourceBusy, "Stok şu an başka bir işlemle değiştiriliyor. Lütfen tekrar deneyin.", ErrorType.Conflict);

    private static readonly Error RequestIdReused = new(
        ErrorCodes.RequestIdReused, "Bu istek kimliği başka bir işlem için kullanılmış.", ErrorType.Conflict);

    /// <summary>Hareketin bir ucu: depo ve adres (adres verilmezse topun bulunduğu adres, o da yoksa deponun genel adresi).</summary>
    /// <param name="Inbound">Uca stok giriyor: depo ve adres aktif olmalıdır.</param>
    private sealed record Place(int WarehouseId, int? LocationId, string WarehouseField, string LocationField, bool Inbound);

    /// <summary>Çözülmüş adres.</summary>
    private sealed record LocationRef(int Id, int WarehouseId, string Code, bool IsActive);

    /// <summary>Bir uçtaki bakiye değişikliği; uçlar <see cref="Place"/> dizisindeki sıralarıyla anılır.</summary>
    private sealed record Movement(StockTransactionType Type, int Place, decimal Delta, int? CounterPlace = null);

    private sealed record Reference(int? ProductionOrderId = null, string? DocumentNo = null, string? Note = null);

    /// <summary>Hareketin topu: var olan bir top (çıkış, transfer, sayım) ya da girişte açılacak yeni top.</summary>
    private sealed record LotChoice(int? LotId = null, StockLotRequest? NewLot = null, bool CreatesLot = false);

    private DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;
    private string Actor => currentUser.UserName ?? SystemUser;

    public async Task<PagedResult<StockBalanceDto>> GetBalancesAsync(StockBalanceQuery query, CancellationToken cancellationToken)
    {
        // Sonradan pasif yapılan / silinen depo ve kartların bakiyesi de görünür.
        var stocks = db.Stocks.AsNoTracking().IgnoreQueryFilters();

        if (!query.IncludeZero)
        {
            stocks = stocks.Where(s => s.Quantity > 0);
        }

        if (query.WarehouseId is { } warehouseId)
        {
            stocks = stocks.Where(s => s.WarehouseId == warehouseId);
        }

        if (query.StockItemId is { } stockItemId)
        {
            stocks = stocks.Where(s => s.StockItemId == stockItemId);
        }

        if (query.ItemType is { } itemType)
        {
            stocks = stocks.Where(s => s.StockItem.ItemType == itemType);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            stocks = stocks.Where(s => s.StockItem.Code.Contains(search) || s.StockItem.Name.Contains(search));
        }

        var page = await stocks
            .GroupBy(s => new
            {
                s.WarehouseId,
                WarehouseCode = s.Warehouse.Code,
                WarehouseName = s.Warehouse.Name,
                s.StockItemId,
                ItemCode = s.StockItem.Code,
                ItemName = s.StockItem.Name,
                s.StockItem.ItemType,
                s.StockItem.Unit,
                s.StockItem.IsLotTracked,
            })
            .OrderBy(g => g.Key.ItemCode).ThenBy(g => g.Key.WarehouseCode)
            .Select(g => new StockBalanceDto(
                g.Key.WarehouseId, g.Key.WarehouseCode, g.Key.WarehouseName, g.Key.StockItemId, g.Key.ItemCode,
                g.Key.ItemName, g.Key.ItemType, g.Key.Unit, g.Sum(s => s.Quantity), g.Key.IsLotTracked))
            .ToPagedResultAsync(query, cancellationToken);

        // Sayfadaki kartların siparişlere ayrılmış miktarı.
        var itemIds = page.Items.Select(b => b.StockItemId).Distinct().ToList();
        var reserved = (await db.StockReservations
                .AsNoTracking()
                .Where(r => itemIds.Contains(r.StockItemId))
                .Select(r => new { r.WarehouseId, r.StockItemId, r.Quantity })
                .Concat(db.SalesOrderReservations.AsNoTracking()
                    .Where(r => itemIds.Contains(r.StockItemId))
                    .Select(r => new { r.WarehouseId, r.StockItemId, r.Quantity }))
                .ToListAsync(cancellationToken))
            .GroupBy(r => (r.WarehouseId, r.StockItemId))
            .ToDictionary(g => g.Key, g => g.Sum(r => r.Quantity));

        return page with
        {
            Items = page.Items
                .Select(b => b with { Reserved = reserved.GetValueOrDefault((b.WarehouseId, b.StockItemId)) })
                .ToList(),
        };
    }

    public Task<PagedResult<StockLocationBalanceDto>> GetLocationBalancesAsync(
        StockLocationBalanceQuery query, CancellationToken cancellationToken)
    {
        var stocks = db.Stocks.AsNoTracking().IgnoreQueryFilters();

        if (!query.IncludeZero)
        {
            stocks = stocks.Where(s => s.Quantity > 0);
        }

        if (query.WarehouseId is { } warehouseId)
        {
            stocks = stocks.Where(s => s.WarehouseId == warehouseId);
        }

        if (query.LocationId is { } locationId)
        {
            stocks = stocks.Where(s => s.LocationId == locationId);
        }

        if (query.StockItemId is { } stockItemId)
        {
            stocks = stocks.Where(s => s.StockItemId == stockItemId);
        }

        if (query.ItemType is { } itemType)
        {
            stocks = stocks.Where(s => s.StockItem.ItemType == itemType);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            stocks = stocks.Where(s =>
                s.StockItem.Code.Contains(search) || s.StockItem.Name.Contains(search) || s.Location.Code.Contains(search));
        }

        // Takipli kartta adresin bakiyesi, o adresteki topların toplamıdır.
        return stocks
            .GroupBy(s => new
            {
                s.WarehouseId,
                WarehouseCode = s.Warehouse.Code,
                WarehouseName = s.Warehouse.Name,
                s.LocationId,
                LocationCode = s.Location.Code,
                LocationIsDefault = s.Location.IsDefault,
                s.StockItemId,
                ItemCode = s.StockItem.Code,
                ItemName = s.StockItem.Name,
                s.StockItem.ItemType,
                s.StockItem.Unit,
                s.StockItem.IsLotTracked,
            })
            .OrderBy(g => g.Key.ItemCode).ThenBy(g => g.Key.WarehouseCode)
            .ThenByDescending(g => g.Key.LocationIsDefault).ThenBy(g => g.Key.LocationCode)
            .Select(g => new StockLocationBalanceDto(
                g.Key.WarehouseId, g.Key.WarehouseCode, g.Key.WarehouseName, g.Key.LocationId, g.Key.LocationCode,
                g.Key.LocationIsDefault, g.Key.StockItemId, g.Key.ItemCode, g.Key.ItemName, g.Key.ItemType, g.Key.Unit,
                g.Sum(s => s.Quantity), g.Key.IsLotTracked))
            .ToPagedResultAsync(query, cancellationToken);
    }

    public Task<PagedResult<StockLotBalanceDto>> GetLotBalancesAsync(
        StockLotBalanceQuery query, CancellationToken cancellationToken)
    {
        var stocks = db.Stocks.AsNoTracking().IgnoreQueryFilters().Where(s => s.LotId != null);

        if (!query.IncludeZero)
        {
            stocks = stocks.Where(s => s.Quantity > 0);
        }

        if (query.WarehouseId is { } warehouseId)
        {
            stocks = stocks.Where(s => s.WarehouseId == warehouseId);
        }

        if (query.LocationId is { } locationId)
        {
            stocks = stocks.Where(s => s.LocationId == locationId);
        }

        if (query.StockItemId is { } stockItemId)
        {
            stocks = stocks.Where(s => s.StockItemId == stockItemId);
        }

        if (query.LotId is { } lotId)
        {
            stocks = stocks.Where(s => s.LotId == lotId);
        }

        if (query.PurchaseOrderId is { } purchaseOrderId)
        {
            stocks = stocks.Where(s => s.Lot!.PurchaseOrderId == purchaseOrderId);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            stocks = stocks.Where(s =>
                s.Lot!.LotNo.Contains(search)
                || (s.Lot.BatchNo != null && s.Lot.BatchNo.Contains(search))
                || (s.Lot.SupplierRollNo != null && s.Lot.SupplierRollNo.Contains(search))
                || s.StockItem.Code.Contains(search) || s.StockItem.Name.Contains(search));
        }

        return stocks
            .OrderBy(s => s.StockItem.Code).ThenBy(s => s.LotId).ThenBy(s => s.Warehouse.Code).ThenBy(s => s.Location.Code)
            .Select(s => new StockLotBalanceDto(
                s.Lot!.Id, s.Lot.LotNo, s.Lot.BatchNo, s.Lot.Shade, s.Lot.Width, s.Lot.SupplierRollNo, s.Lot.InitialQuantity,
                s.Lot.ReceivedAt, s.Lot.PurchaseOrderId, s.Lot.PurchaseOrder == null ? null : s.Lot.PurchaseOrder.OrderNo,
                s.WarehouseId, s.Warehouse.Code, s.Warehouse.Name, s.LocationId, s.Location.Code, s.StockItemId,
                s.StockItem.Code, s.StockItem.Name, s.StockItem.ItemType, s.StockItem.Unit, s.Quantity))
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<Result> UpdateLotAsync(int lotId, StockLotUpdateRequest request, CancellationToken cancellationToken)
    {
        var lot = await db.StockLots.SingleOrDefaultAsync(l => l.Id == lotId, cancellationToken);
        if (lot is null)
        {
            return new Error(ErrorCodes.NotFound, "Top bulunamadı.", ErrorType.NotFound);
        }

        ApplyAttributes(lot, new StockLotRequest(request.BatchNo, request.Shade, request.Width, request.SupplierRollNo));
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<IReadOnlyList<WarehouseLocationOptionDto>> GetLocationOptionsAsync(
        int warehouseId, CancellationToken cancellationToken) =>
        await db.WarehouseLocations
            .AsNoTracking()
            .Where(l => l.WarehouseId == warehouseId)
            .OrderByDescending(l => l.IsDefault).ThenBy(l => l.Code)
            .Take(WarehouseLocationOptionDto.MaxOptions)
            .Select(l => new WarehouseLocationOptionDto(l.Id, l.Code, l.Description, l.IsDefault, l.IsActive))
            .ToListAsync(cancellationToken);

    public async Task<PagedResult<StockTransactionDto>> GetTransactionsAsync(
        StockTransactionQuery query, CancellationToken cancellationToken)
    {
        var transactions = db.StockTransactions.AsNoTracking().IgnoreQueryFilters();

        if (query.WarehouseId is { } warehouseId)
        {
            transactions = transactions.Where(t => t.WarehouseId == warehouseId);
        }

        if (query.LocationId is { } locationId)
        {
            transactions = transactions.Where(t => t.LocationId == locationId);
        }

        if (query.StockItemId is { } stockItemId)
        {
            transactions = transactions.Where(t => t.StockItemId == stockItemId);
        }

        if (query.LotId is { } lotId)
        {
            transactions = transactions.Where(t => t.LotId == lotId);
        }

        if (query.Type is { } type)
        {
            transactions = transactions.Where(t => t.Type == type);
        }

        if (query.ProductionOrderId is { } orderId)
        {
            transactions = transactions.Where(t => t.ProductionOrderId == orderId);
        }

        if (query.From is { } from)
        {
            transactions = transactions.Where(t => t.OccurredAt >= from);
        }

        if (query.To is { } to)
        {
            transactions = transactions.Where(t => t.OccurredAt < to);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            transactions = transactions.Where(t =>
                t.StockItem.Code.Contains(search) || t.StockItem.Name.Contains(search)
                || (t.DocumentNo != null && t.DocumentNo.Contains(search))
                || (t.Bundle != null && t.Bundle.BundleNo.Contains(search))
                || (t.Lot != null && t.Lot.LotNo.Contains(search)));
        }

        var page = await Project(transactions.OrderByDescending(t => t.Id)).ToPagedResultAsync(query, cancellationToken);
        // Hareketin birim maliyeti yalnızca maliyet görme yetkisiyle görünür.
        return await CostRules.CanSeeCostAsync(permissions, cancellationToken)
            ? page
            : page with { Items = page.Items.Select(t => t with { UnitCost = null }).ToList() };
    }

    public Task<PagedResult<ReceivableBundleDto>> GetReceivableBundlesAsync(
        ReceivableBundleQuery query, CancellationToken cancellationToken)
    {
        var bundles = db.Bundles.AsNoTracking().IgnoreQueryFilters().Where(b => b.Status == BundleStatus.Completed);

        if (query.ProductionOrderId is { } orderId)
        {
            bundles = bundles.Where(b => b.ProductionOrderId == orderId);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            bundles = bundles.Where(b => b.BundleNo.Contains(search) || b.ProductionOrder.OrderNo.Contains(search));
        }

        // Sağlam adet: rotanın son (ardılı olmayan) operasyonlarından geçen en küçük adet.
        return bundles
            .OrderBy(b => b.Id)
            .Select(b => new ReceivableBundleDto(
                b.Id,
                b.BundleNo,
                b.ProductionOrderId,
                b.ProductionOrder.OrderNo,
                b.ProductionOrder.Product.ProductCode,
                b.ProductionOrder.Product.Name,
                b.Color.Name,
                b.Size.Name,
                b.Quantity,
                b.Operations
                    .Where(o => !db.OperationDependencies.Any(d => d.PredecessorId == o.RouteOperationId))
                    .Min(o => (int?)o.ProducedQuantity) ?? 0,
                b.Operations.Max(o => o.CompletedAt)))
            .ToPagedResultAsync(query, cancellationToken);
    }

    public Task<Result<StockMovementDto>> ReceiveAsync(StockReceiptRequest request, CancellationToken cancellationToken) =>
        MoveAsync(
            request.RequestId, request.StockItemId, request.Quantity, "quantity",
            [new Place(request.WarehouseId, request.LocationId, "warehouseId", "locationId", Inbound: true)],
            itemMustBeActive: true,
            _ => Planned(new Movement(StockTransactionType.Receipt, 0, request.Quantity)),
            new Reference(null, request.DocumentNo, request.Note),
            new LotChoice(NewLot: request.Lot, CreatesLot: true), cancellationToken);

    /// <summary>Malzeme siparişin reçetesinde ve henüz demetlenmemiş adet var: demet açılınca reçeteden düşecek.</summary>
    private async Task<bool> RecipeIssuePendingAsync(int orderId, int stockItemId, CancellationToken cancellationToken)
    {
        var order = await db.ProductionOrders.AsNoTracking()
            .Where(o => o.Id == orderId)
            .Select(o => new
            {
                o.ProductId,
                Ordered = o.Items.Sum(i => (int?)i.Quantity) ?? 0,
                Bundled = db.Bundles.Where(b => b.ProductionOrderId == o.Id && b.Status != BundleStatus.Cancelled).Sum(b => (int?)b.Quantity) ?? 0,
            })
            .SingleAsync(cancellationToken);
        return order.Bundled < order.Ordered
            && await db.ProductMaterials.AnyAsync(m => m.ProductId == order.ProductId && m.StockItemId == stockItemId, cancellationToken);
    }

    public async Task<Result<StockMovementDto>> IssueAsync(StockIssueRequest request, CancellationToken cancellationToken)
    {
        if (request.ProductionOrderId is { } orderId
            && !await db.ProductionOrders.AnyAsync(o => o.Id == orderId, cancellationToken))
        {
            return ReferenceValidation.InvalidReference("productionOrderId", "Üretim siparişi bulunamadı.");
        }

        if (request.ProductionOrderId is { } productionOrderId && await RecipeIssuePendingAsync(productionOrderId, request.StockItemId, cancellationToken))
        {
            return new Error(
                "MATERIAL_ISSUED_BY_BUNDLE",
                "Bu malzeme reçetede; demet açılınca depodan kendiliğinden düşer. Fazladan çıkış için önce bütün demetleri açın.");
        }

        // Pasif depodaki / adresteki stok da çıkılabilir (boşaltılabilsin); yeni giriş yapılamaz.
        return await MoveAsync(
            request.RequestId, request.StockItemId, request.Quantity, "quantity",
            [new Place(request.WarehouseId, request.LocationId, "warehouseId", "locationId", Inbound: false)],
            itemMustBeActive: false,
            // Siparişe yazılan çıkış üretim tüketimidir: ihtiyaçtan düşer ve maliyete girer.
            _ => Planned(new Movement(request.ProductionOrderId is null ? StockTransactionType.Issue : StockTransactionType.ProductionIssue, 0, -request.Quantity)),
            new Reference(request.ProductionOrderId, request.DocumentNo, request.Note), new LotChoice(request.LotId),
            cancellationToken);
    }

    public Task<Result<StockMovementDto>> TransferAsync(StockTransferRequest request, CancellationToken cancellationToken)
    {
        // Aynı depo içinde taşıma adres değişimidir: deponun bakiyesi değişmez.
        var (outType, inType) = request.FromWarehouseId == request.ToWarehouseId
            ? (StockTransactionType.RelocationOut, StockTransactionType.RelocationIn)
            : (StockTransactionType.TransferOut, StockTransactionType.TransferIn);

        return MoveAsync(
            request.RequestId, request.StockItemId, request.Quantity, "quantity",
            [
                new Place(request.FromWarehouseId, request.FromLocationId, "fromWarehouseId", "fromLocationId", Inbound: false),
                new Place(request.ToWarehouseId, request.ToLocationId, "toWarehouseId", "toLocationId", Inbound: true),
            ],
            itemMustBeActive: false,
            _ => Planned(new Movement(outType, 0, -request.Quantity, 1), new Movement(inType, 1, request.Quantity, 0)),
            new Reference(Note: request.Note), new LotChoice(request.LotId), cancellationToken);
    }

    public Task<Result<StockMovementDto>> AdjustAsync(StockAdjustmentRequest request, CancellationToken cancellationToken) =>
        MoveAsync(
            request.RequestId, request.StockItemId, request.CountedQuantity, "countedQuantity",
            [new Place(request.WarehouseId, request.LocationId, "warehouseId", "locationId", Inbound: false)],
            itemMustBeActive: false,
            balances =>
            {
                // Fark, kilit altında okunan güncel adres bakiyesine göre hesaplanır.
                var delta = request.CountedQuantity - balances[0];
                return delta == 0
                    ? new Error(ErrorCodes.StockNoChange, "Sayılan miktar mevcut stokla aynı; düzeltme gerekmiyor.")
                    : Planned(new Movement(StockTransactionType.Adjustment, 0, delta));
            },
            new Reference(Note: request.Note), new LotChoice(request.LotId), cancellationToken);

    public async Task<Result<StockMovementDto>> ReceiveFromProductionAsync(
        ProductionReceiptRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // Üretim ve kalite işlemleriyle aynı kilit: teslim alma, o anda girilen bir kalite reddiyle yarışmaz.
        if (!await appLock.TryAcquireAsync($"bundle:{request.BundleId}", LockTimeout, cancellationToken))
        {
            return Busy;
        }

        var replay = await db.StockTransactions
            .Where(t => t.RequestId == request.RequestId)
            .Select(t => new { t.Type, t.BundleId })
            .ToListAsync(cancellationToken);
        if (replay.Count > 0)
        {
            return replay.All(t => t.Type == StockTransactionType.ProductionReceipt && t.BundleId == request.BundleId)
                ? await LoadMovementAsync(request.RequestId, duplicate: true, cancellationToken)
                : RequestIdReused;
        }

        var bundle = await db.Bundles.Include(b => b.Operations).SingleOrDefaultAsync(b => b.Id == request.BundleId, cancellationToken);
        if (bundle is null)
        {
            return new Error(ErrorCodes.NotFound, "Demet bulunamadı.", ErrorType.NotFound);
        }

        if (bundle.Status == BundleStatus.Closed)
        {
            return new Error(
                ErrorCodes.BundleAlreadyReceived, $"{bundle.BundleNo} numaralı demet daha önce depoya teslim alınmış.",
                ErrorType.Conflict);
        }

        if (!bundle.CanClose)
        {
            return new Error(
                ErrorCodes.BundleNotCompleted,
                bundle.Status == BundleStatus.QualityHold
                    ? "Demet kalite bekletmesinde; kalite serbest bırakmadan depoya alınamaz."
                    : "Yalnızca tüm operasyonları tamamlanmış demet depoya teslim alınabilir.");
        }

        // Tamiri yapılmış ama kalite tekrar kontrol etmemiş demet kapanırsa tamir işi bir daha kapatılamaz.
        if (await db.ReworkTasks.AnyAsync(t => t.BundleId == bundle.Id && (t.Status == ReworkStatus.Open || t.Status == ReworkStatus.Done), cancellationToken))
        {
            return new Error(
                ErrorCodes.ReworkNotDone,
                $"{bundle.BundleNo} numaralı demetin tekrar kontrol bekleyen tamiri var; kalite kontrolü tamamlanmadan depoya alınamaz.");
        }

        if (await ReferenceValidation.RequireSelectableAsync(
                db.Warehouses, request.WarehouseId, null, "warehouseId", "Depo", cancellationToken) is { } warehouseError)
        {
            return warehouseError;
        }

        var quantity = bundle.FinishedQuantity(await LoadFlowAsync(bundle, cancellationToken));
        if (quantity <= 0)
        {
            return new Error(ErrorCodes.NothingToReceive, "Demetten sağlam parça çıkmamış; depoya alınacak adet yok.");
        }

        var (item, isNew) = await GetOrCreateFinishedGoodAsync(bundle, cancellationToken);
        if (item is null)
        {
            return Busy;
        }

        // Yeni açılan kart henüz başka bir işlem tarafından görülemez; kilide gerek yoktur.
        if (!isNew && !await appLock.TryAcquireAsync(StockLock(request.WarehouseId, item.Id), LockTimeout, cancellationToken))
        {
            return Busy;
        }

        // Mamul deponun genel adresine girer; rafa yerleştirme ayrı bir adres değişimidir.
        var locationId = await DefaultLocationIdAsync(request.WarehouseId, cancellationToken);
        if (await CountLockAsync(request.WarehouseId, [locationId], cancellationToken) is { } countLock)
        {
            return countLock;
        }

        var stocks = isNew ? [] : await LoadStocksAsync(request.WarehouseId, item.Id, cancellationToken);
        var stock = stocks.FirstOrDefault(s => s.LocationId == locationId && s.LotId == null);
        if (stock is null)
        {
            stock = new Stock { WarehouseId = request.WarehouseId, LocationId = locationId, StockItem = item };
            stocks.Add(stock);
            db.Stocks.Add(stock);
        }

        var locationBalance = stock.Apply(quantity);
        db.StockTransactions.Add(new StockTransaction
        {
            RequestId = request.RequestId,
            Type = StockTransactionType.ProductionReceipt,
            WarehouseId = request.WarehouseId,
            LocationId = locationId,
            StockItem = item,
            Quantity = quantity,
            BalanceAfter = stocks.Sum(s => s.Quantity),
            LocationBalanceAfter = locationBalance,
            BundleId = bundle.Id,
            ProductionOrderId = bundle.ProductionOrderId,
            OccurredAt = UtcNow,
            CreatedBy = Actor,
        });
        bundle.Close();

        if (await SaveAsync(cancellationToken) is { } saveError)
        {
            return saveError;
        }

        await transaction.CommitAsync(cancellationToken);
        return await LoadMovementAsync(request.RequestId, duplicate: false, cancellationToken);
    }

    public async Task<Error?> PostPurchaseReceiptAsync(
        int warehouseId,
        int purchaseOrderId,
        IReadOnlyList<PurchaseReceiptLine> lines,
        string? documentNo,
        string? note,
        CancellationToken cancellationToken)
    {
        if (db.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("Mal kabulün stok girişi, mal kabulle aynı transaction içinde yazılmalıdır.");
        }

        if (await ReferenceValidation.RequireSelectableAsync(
                db.Warehouses, warehouseId, null, "warehouseId", "Depo", cancellationToken) is { } warehouseError)
        {
            return warehouseError;
        }

        var now = UtcNow;
        var locationId = await DefaultLocationIdAsync(warehouseId, cancellationToken);
        if (await CountLockAsync(warehouseId, [locationId], cancellationToken) is { } countLock)
        {
            return countLock;
        }

        var itemIds = lines.Select(l => l.StockItemId).Distinct().ToList();
        var tracked = await db.StockItems
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(i => itemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, i => new { i.Code, i.IsLotTracked }, cancellationToken);

        // Kilitler her zaman kart kimliği sırasıyla alınır: iki mal kabul birbirini kilitleyemez.
        foreach (var (stockItemId, quantity, unitCost, lots) in lines.OrderBy(l => l.StockItemId))
        {
            if (!tracked[stockItemId].IsLotTracked && lots is { Count: > 0 })
            {
                var message = $"{tracked[stockItemId].Code}: kart top / parti takipli değil; top bilgisi girilemez.";
                return new Error(
                    ErrorCodes.StockLotNotAllowed, message, ErrorType.Validation,
                    new Dictionary<string, string[]> { ["lines"] = [message] });
            }

            if (!await appLock.TryAcquireAsync(StockLock(warehouseId, stockItemId), LockTimeout, cancellationToken))
            {
                return Busy;
            }

            var stocks = await LoadStocksAsync(warehouseId, stockItemId, cancellationToken);

            if (unitCost is { } price)
            {
                // Ortalama maliyet tüm depolardaki stok üzerinden hesaplanır; eşzamanlı iki mal kabul birbirini ezmesin.
                if (!await appLock.TryAcquireAsync($"stock-item-cost:{stockItemId}", LockTimeout, cancellationToken))
                {
                    return Busy;
                }

                var item = await db.StockItems.IgnoreQueryFilters().SingleAsync(i => i.Id == stockItemId, cancellationToken);
                var onHand = await db.Stocks
                    .Where(s => s.StockItemId == stockItemId)
                    .SumAsync(s => (decimal?)s.Quantity, cancellationToken) ?? 0;
                item.UnitCost = item.UnitCost is { } current && onHand > 0
                    ? decimal.Round((onHand * current + quantity * price) / (onHand + quantity), 4)
                    : price;
            }

            // Takipli kartta her top ayrı bir giriştir; top bilgisi verilmediyse kalemin tamamı tek toptur.
            IReadOnlyList<ReceivedLot?> parts = !tracked[stockItemId].IsLotTracked
                ? [null]
                : lots is { Count: > 0 } ? (IReadOnlyList<ReceivedLot?>)lots : [new ReceivedLot(quantity, new StockLotRequest())];
            foreach (var part in parts)
            {
                var lot = part is null ? null : NewLot(stockItemId, part.Quantity, part.Attributes, now, purchaseOrderId);
                var stock = StockAt(stocks, warehouseId, locationId, stockItemId, lot);
                var partQuantity = part?.Quantity ?? quantity;
                var rowBalance = stock.Apply(partQuantity);
                db.StockTransactions.Add(new StockTransaction
                {
                    // Tekrar koruması mal kabul kaydındadır; hareketin kimliği yalnızca benzersiz olmalıdır.
                    RequestId = Guid.NewGuid(),
                    Type = StockTransactionType.PurchaseReceipt,
                    WarehouseId = warehouseId,
                    LocationId = locationId,
                    StockItemId = stockItemId,
                    Lot = lot,
                    Quantity = partQuantity,
                    BalanceAfter = stocks.Sum(s => s.Quantity),
                    LocationBalanceAfter = stocks.Where(s => s.LocationId == locationId).Sum(s => s.Quantity),
                    LotBalanceAfter = lot is null ? null : rowBalance,
                    UnitCost = unitCost,
                    PurchaseOrderId = purchaseOrderId,
                    DocumentNo = string.IsNullOrWhiteSpace(documentNo) ? null : documentNo.Trim(),
                    Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
                    OccurredAt = now,
                    CreatedBy = Actor,
                });
            }
        }

        return null;
    }

    public async Task<Error?> PostShipmentAsync(
        int warehouseId,
        int shipmentId,
        int? productionOrderId,
        int? salesOrderId,
        IReadOnlyList<(int StockItemId, string ItemCode, decimal Quantity)> lines,
        string? documentNo,
        CancellationToken cancellationToken)
    {
        if (db.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("Sevkiyatın stok çıkışı, sevkiyatla aynı transaction içinde yazılmalıdır.");
        }

        var now = UtcNow;
        // Kilitler her zaman kart kimliği sırasıyla alınır: iki sevkiyat birbirini kilitleyemez.
        foreach (var (stockItemId, itemCode, quantity) in lines.OrderBy(l => l.StockItemId))
        {
            if (!await appLock.TryAcquireAsync(StockLock(warehouseId, stockItemId), LockTimeout, cancellationToken))
            {
                return Busy;
            }

            var stocks = await LoadStocksAsync(warehouseId, stockItemId, cancellationToken);
            var onHand = stocks.Sum(s => s.Quantity);
            // Siparişin kendi ayırması bu sevkiyatta kullanılabilir.
            var reserved = await ReservedByOthersAsync(warehouseId, stockItemId, null, cancellationToken, salesOrderId);
            if (onHand - reserved < quantity)
            {
                var message =
                    $"{itemCode}: depoda yeterli stok yok (mevcut {Format(onHand)}{ReservedNote(reserved)}, sevk edilecek {Format(quantity)}).";
                return new Error(
                    ErrorCodes.InsufficientStock, message, ErrorType.BusinessRule,
                    new Dictionary<string, string[]> { ["items"] = [message] });
            }

            var allocations = Allocate(stocks, quantity);
            if (await CountLockAsync(warehouseId, allocations.Select(a => a.Stock.LocationId), cancellationToken) is { } countLock)
            {
                return countLock;
            }

            foreach (var (stock, taken) in allocations)
            {
                var rowBalance = stock.Apply(-taken);
                db.StockTransactions.Add(new StockTransaction
                {
                    // Tekrar koruması sevkiyatın durumundadır; hareketin kimliği yalnızca benzersiz olmalıdır.
                    RequestId = Guid.NewGuid(),
                    Type = StockTransactionType.Shipment,
                    WarehouseId = warehouseId,
                    LocationId = stock.LocationId,
                    StockItemId = stockItemId,
                    LotId = stock.LotId,
                    Quantity = -taken,
                    BalanceAfter = stocks.Sum(s => s.Quantity),
                    LocationBalanceAfter = stocks.Where(s => s.LocationId == stock.LocationId).Sum(s => s.Quantity),
                    LotBalanceAfter = stock.LotId is null ? null : rowBalance,
                    ShipmentId = shipmentId,
                    ProductionOrderId = productionOrderId,
                    DocumentNo = string.IsNullOrWhiteSpace(documentNo) ? null : documentNo.Trim(),
                    OccurredAt = now,
                    CreatedBy = Actor,
                });
            }

            if (salesOrderId is { } orderId)
            {
                await ConsumeSalesReservationAsync(orderId, warehouseId, stockItemId, quantity, cancellationToken);
            }
        }

        return null;
    }

    public async Task<Error?> PostProductionIssueAsync(
        int warehouseId,
        int productionOrderId,
        string documentNo,
        IReadOnlyList<(Bundle Bundle, int StockItemId, decimal Quantity)> lines,
        CancellationToken cancellationToken)
    {
        if (db.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("Üretime malzeme çıkışı, demetlerle aynı transaction içinde yazılmalıdır.");
        }

        // Pasif depodaki stok da kullanılabilir; depo var olmalıdır.
        if (!await db.Warehouses.AnyAsync(w => w.Id == warehouseId, cancellationToken))
        {
            return ReferenceValidation.InvalidReference("warehouseId", "Depo bulunamadı.");
        }

        var itemIds = lines.Select(l => l.StockItemId).Distinct().Order().ToList();
        var items = await db.StockItems
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(i => itemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, i => new { i.Code, i.Name, i.UnitCost }, cancellationToken);

        // Malzeme önce verilen depodan, yetmeyen kısmı öteki aktif hammadde depolarından düşülür: kumaşla aksesuarın
        // ayrı depolarda durması olağandır.
        var warehouseIds = await db.Warehouses
            .AsNoTracking()
            .Where(w => w.Id != warehouseId && w.IsActive && w.Type == WarehouseType.RawMaterial)
            .OrderBy(w => w.Id)
            .Select(w => w.Id)
            .ToListAsync(cancellationToken);
        warehouseIds.Insert(0, warehouseId);

        // Kilitler her zaman (depo, kart) kimliği sırasıyla alınır: iki demetleme birbirini kilitleyemez.
        foreach (var (lockWarehouseId, itemId) in warehouseIds.Order().SelectMany(w => itemIds.Select(i => (w, i))))
        {
            if (!await appLock.TryAcquireAsync(StockLock(lockWarehouseId, itemId), LockTimeout, cancellationToken))
            {
                return Busy;
            }
        }

        var sources = new Dictionary<int, List<IssueSource>>();
        var shortages = new List<string>();
        foreach (var itemId in itemIds)
        {
            var needed = lines.Where(l => l.StockItemId == itemId).Sum(l => l.Quantity);
            var remaining = needed;
            decimal onHand = 0, reserved = 0;
            var plan = new List<IssueSource>();
            foreach (var sourceWarehouseId in warehouseIds)
            {
                var warehouseStocks = await LoadStocksAsync(sourceWarehouseId, itemId, cancellationToken);
                var warehouseOnHand = warehouseStocks.Sum(s => s.Quantity);
                // Başka siparişlere ayrılmış stok bu sipariş için kullanılamaz.
                var warehouseReserved = await ReservedByOthersAsync(sourceWarehouseId, itemId, productionOrderId, cancellationToken);
                onHand += warehouseOnHand;
                reserved += warehouseReserved;
                var take = Math.Min(Math.Max(warehouseOnHand - warehouseReserved, 0), remaining);
                if (take > 0)
                {
                    plan.Add(new IssueSource(sourceWarehouseId, warehouseStocks, take));
                    remaining -= take;
                }
            }

            if (remaining > 0)
            {
                shortages.Add(
                    $"{items[itemId].Code} ({items[itemId].Name}): gereken {Format(needed)}, hammadde depolarında {Format(onHand)}{ReservedNote(reserved)}");
                continue;
            }

            foreach (var source in plan)
            {
                await ConsumeReservationAsync(source.WarehouseId, itemId, productionOrderId, source.Remaining, cancellationToken);
            }

            sources[itemId] = plan;
        }

        if (shortages.Count > 0)
        {
            return new Error(
                ErrorCodes.InsufficientStock,
                $"Reçetedeki malzemeler için depolarda yeterli stok yok. {string.Join("; ", shortages)}.");
        }

        var now = UtcNow;
        foreach (var (bundle, itemId, quantity) in lines)
        {
            var left = quantity;
            foreach (var source in sources[itemId].Where(s => s.Remaining > 0))
            {
                var portion = Math.Min(source.Remaining, left);
                source.Remaining -= portion;
                left -= portion;
                var allocations = Allocate(source.Stocks, portion);
                if (await CountLockAsync(source.WarehouseId, allocations.Select(a => a.Stock.LocationId), cancellationToken) is { } countLock)
                {
                    return countLock;
                }

                foreach (var (stock, taken) in allocations)
                {
                    var rowBalance = stock.Apply(-taken);
                    db.StockTransactions.Add(new StockTransaction
                    {
                        // Tekrar koruması demetlemenin kendisindedir; hareketin kimliği yalnızca benzersiz olmalıdır.
                        RequestId = Guid.NewGuid(),
                        Type = StockTransactionType.ProductionIssue,
                        WarehouseId = source.WarehouseId,
                        LocationId = stock.LocationId,
                        StockItemId = itemId,
                        LotId = stock.LotId,
                        Quantity = -taken,
                        BalanceAfter = source.Stocks.Sum(s => s.Quantity),
                        LocationBalanceAfter = source.Stocks.Where(s => s.LocationId == stock.LocationId).Sum(s => s.Quantity),
                        LotBalanceAfter = stock.LotId is null ? null : rowBalance,
                        UnitCost = items[itemId].UnitCost,
                        Bundle = bundle,
                        ProductionOrderId = productionOrderId,
                        DocumentNo = documentNo,
                        OccurredAt = now,
                        CreatedBy = Actor,
                    });
                }

                if (left == 0)
                {
                    break;
                }
            }
        }

        return null;
    }

    /// <summary>Bir malzemenin üretime çıkışında kullanılacak depo ve oradan alınacak (kalan) miktar.</summary>
    private sealed class IssueSource(int warehouseId, List<Stock> stocks, decimal quantity)
    {
        public int WarehouseId { get; } = warehouseId;
        public List<Stock> Stocks { get; } = stocks;
        public decimal Remaining { get; set; } = quantity;
    }

    public async Task<Error?> PostLotIssueAsync(
        int productionOrderId,
        string documentNo,
        string? note,
        IReadOnlyList<(int LotId, decimal Quantity)> lines,
        CancellationToken cancellationToken)
    {
        if (db.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("Toptan üretime çıkış, kesim kaydıyla aynı transaction içinde yazılmalıdır.");
        }

        var wanted = lines
            .Where(l => l.Quantity > 0)
            .GroupBy(l => l.LotId)
            .Select(g => (LotId: g.Key, Quantity: g.Sum(l => l.Quantity)))
            .ToList();
        if (wanted.Count == 0)
        {
            return null;
        }

        var lotIds = wanted.Select(w => w.LotId).ToList();
        var lots = await db.StockLots
            .AsNoTracking()
            .Where(l => lotIds.Contains(l.Id))
            .Select(l => new { l.Id, l.LotNo, l.StockItemId, l.StockItem.Code, l.StockItem.UnitCost, l.StockItem.Unit })
            .ToDictionaryAsync(l => l.Id, cancellationToken);
        if (lots.Count != lotIds.Count)
        {
            return ReferenceValidation.InvalidReference("lots", "Top bulunamadı.");
        }

        // Topların durduğu (depo, kart) çiftleri; kilitler her zaman bu sırayla alınır.
        var keys = (await db.Stocks
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(s => s.LotId != null && lotIds.Contains(s.LotId.Value) && s.Quantity > 0)
                .Select(s => new { s.WarehouseId, s.StockItemId })
                .Distinct()
                .ToListAsync(cancellationToken))
            .Select(k => (k.WarehouseId, k.StockItemId))
            .OrderBy(k => k.WarehouseId).ThenBy(k => k.StockItemId)
            .ToList();

        var stocks = new Dictionary<(int WarehouseId, int StockItemId), List<Stock>>();
        foreach (var key in keys)
        {
            if (!await appLock.TryAcquireAsync(StockLock(key.WarehouseId, key.StockItemId), LockTimeout, cancellationToken))
            {
                return Busy;
            }

            stocks[key] = await LoadStocksAsync(key.WarehouseId, key.StockItemId, cancellationToken);
        }

        // Önce her topun nereden ne kadar alınacağı planlanır; eksik varsa hiçbir şey yazılmaz.
        var plan = new List<(int WarehouseId, int StockItemId, Stock Stock, decimal Quantity)>();
        var shortages = new List<string>();
        foreach (var (lotId, quantity) in wanted)
        {
            var lot = lots[lotId];
            var rows = keys
                .Where(k => k.StockItemId == lot.StockItemId)
                .SelectMany(k => stocks[k].Where(s => s.LotId == lotId && s.Quantity > 0).Select(s => (Key: k, Stock: s)))
                .ToList();
            var onHand = rows.Sum(r => r.Stock.Quantity);
            if (onHand < quantity)
            {
                shortages.Add($"{lot.LotNo} ({lot.Code}): gereken {Format(quantity)}, topta {Format(onHand)}");
                continue;
            }

            var left = quantity;
            foreach (var (key, stock) in rows)
            {
                var taken = Math.Min(stock.Quantity, left);
                if (taken > 0)
                {
                    plan.Add((key.WarehouseId, key.StockItemId, stock, taken));
                    left -= taken;
                }

                if (left == 0)
                {
                    break;
                }
            }
        }

        // Başka siparişlere ayrılmış miktar, bu siparişin kesiminde kullanılamaz.
        foreach (var group in plan.GroupBy(p => (p.WarehouseId, p.StockItemId)))
        {
            var issued = group.Sum(p => p.Quantity);
            var onHand = stocks[group.Key].Sum(s => s.Quantity);
            var reserved = await ReservedByOthersAsync(group.Key.WarehouseId, group.Key.StockItemId, productionOrderId, cancellationToken);
            if (onHand - reserved < issued)
            {
                var code = lots.Values.First(l => l.StockItemId == group.Key.StockItemId).Code;
                shortages.Add($"{code}: çıkılacak {Format(issued)}, depoda {Format(onHand)}{ReservedNote(reserved)}");
            }
        }

        if (shortages.Count > 0)
        {
            return new Error(
                ErrorCodes.InsufficientStock,
                $"Kesim planının toplarında yeterli kumaş yok. {string.Join("; ", shortages)}.");
        }

        foreach (var warehouseRows in plan.GroupBy(p => p.WarehouseId))
        {
            if (await CountLockAsync(warehouseRows.Key, warehouseRows.Select(p => p.Stock.LocationId), cancellationToken) is { } countLock)
            {
                return countLock;
            }
        }

        var now = UtcNow;
        foreach (var (warehouseId, stockItemId, stock, taken) in plan)
        {
            var warehouseStocks = stocks[(warehouseId, stockItemId)];
            var rowBalance = stock.Apply(-taken);
            db.StockTransactions.Add(new StockTransaction
            {
                RequestId = Guid.NewGuid(),
                Type = StockTransactionType.ProductionIssue,
                WarehouseId = warehouseId,
                LocationId = stock.LocationId,
                StockItemId = stockItemId,
                LotId = stock.LotId,
                Quantity = -taken,
                BalanceAfter = warehouseStocks.Sum(s => s.Quantity),
                LocationBalanceAfter = warehouseStocks.Where(s => s.LocationId == stock.LocationId).Sum(s => s.Quantity),
                LotBalanceAfter = rowBalance,
                UnitCost = lots.Values.First(l => l.StockItemId == stockItemId).UnitCost,
                ProductionOrderId = productionOrderId,
                DocumentNo = documentNo,
                Note = note,
                OccurredAt = now,
                CreatedBy = Actor,
            });
        }

        foreach (var group in plan.GroupBy(p => (p.WarehouseId, p.StockItemId)))
        {
            await ConsumeReservationAsync(group.Key.WarehouseId, group.Key.StockItemId, productionOrderId, group.Sum(p => p.Quantity), cancellationToken);
        }

        return null;
    }

    public async Task<Error?> PostProductionReturnAsync(IReadOnlyCollection<int> bundleIds, CancellationToken cancellationToken)
    {
        if (db.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("Malzeme iadesi, demet iptaliyle aynı transaction içinde yazılmalıdır.");
        }

        var movements = await db.StockTransactions
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(t => t.BundleId != null && bundleIds.Contains(t.BundleId.Value)
                && (t.Type == StockTransactionType.ProductionIssue || t.Type == StockTransactionType.ProductionReturn))
            .OrderBy(t => t.Id)
            .ToListAsync(cancellationToken);

        // Malzemesi daha önce iade edilmiş demet ikinci kez iade edilmez.
        var returned = movements.Where(t => t.Type == StockTransactionType.ProductionReturn).Select(t => t.BundleId).ToHashSet();
        var issues = movements
            .Where(t => t.Type == StockTransactionType.ProductionIssue && !returned.Contains(t.BundleId))
            .ToList();
        if (issues.Count == 0)
        {
            return null;
        }

        var stocks = new Dictionary<(int WarehouseId, int StockItemId), List<Stock>>();
        foreach (var key in issues.Select(t => (t.WarehouseId, t.StockItemId)).Distinct().OrderBy(k => k.WarehouseId).ThenBy(k => k.StockItemId))
        {
            if (!await appLock.TryAcquireAsync(StockLock(key.WarehouseId, key.StockItemId), LockTimeout, cancellationToken))
            {
                return Busy;
            }

            stocks[key] = await LoadStocksAsync(key.WarehouseId, key.StockItemId, cancellationToken);
        }

        foreach (var warehouseIssues in issues.GroupBy(t => t.WarehouseId))
        {
            if (await CountLockAsync(warehouseIssues.Key, warehouseIssues.Select(t => t.LocationId), cancellationToken) is { } countLock)
            {
                return countLock;
            }
        }

        var now = UtcNow;
        foreach (var issue in issues)
        {
            // Malzeme, alındığı adrese ve topa döner.
            var warehouseStocks = stocks[(issue.WarehouseId, issue.StockItemId)];
            // Top takibi başlamadan önce çıkılmış malzemenin topu yoktur: adresteki ilk topa döner.
            var row = warehouseStocks.FirstOrDefault(s => s.LocationId == issue.LocationId && s.LotId == issue.LotId)
                ?? warehouseStocks.First(s => s.LocationId == issue.LocationId);
            var rowBalance = row.Apply(-issue.Quantity);
            db.StockTransactions.Add(new StockTransaction
            {
                RequestId = Guid.NewGuid(),
                Type = StockTransactionType.ProductionReturn,
                WarehouseId = issue.WarehouseId,
                LocationId = issue.LocationId,
                StockItemId = issue.StockItemId,
                LotId = row.LotId,
                Quantity = -issue.Quantity,
                BalanceAfter = warehouseStocks.Sum(s => s.Quantity),
                LocationBalanceAfter = warehouseStocks.Where(s => s.LocationId == issue.LocationId).Sum(s => s.Quantity),
                LotBalanceAfter = row.LotId is null ? null : rowBalance,
                UnitCost = issue.UnitCost,
                BundleId = issue.BundleId,
                ProductionOrderId = issue.ProductionOrderId,
                DocumentNo = issue.DocumentNo,
                Note = "Demet iptali",
                OccurredAt = now,
                CreatedBy = Actor,
            });
        }

        return null;
    }

    public async Task<Error?> ReserveAsync(
        int warehouseId, int productionOrderId, IReadOnlyList<(int StockItemId, decimal Quantity)> targets,
        CancellationToken cancellationToken)
    {
        if (db.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("Rezervasyon, kart kilitleriyle aynı transaction içinde yazılmalıdır.");
        }

        if (!await db.Warehouses.AnyAsync(w => w.Id == warehouseId, cancellationToken))
        {
            return ReferenceValidation.InvalidReference("warehouseId", "Depo bulunamadı.");
        }

        var existing = await db.StockReservations
            .Where(r => r.ProductionOrderId == productionOrderId && r.WarehouseId == warehouseId)
            .ToListAsync(cancellationToken);

        // Artık ihtiyaç duyulmayan (reçeteden çıkmış) kartların rezervi bırakılır.
        db.StockReservations.RemoveRange(existing.Where(r => targets.All(t => t.StockItemId != r.StockItemId)));

        // Kilitler her zaman kart kimliği sırasıyla alınır.
        foreach (var (stockItemId, target) in targets.OrderBy(t => t.StockItemId))
        {
            if (!await appLock.TryAcquireAsync(StockLock(warehouseId, stockItemId), LockTimeout, cancellationToken))
            {
                return Busy;
            }

            var onHand = await db.Stocks
                .Where(s => s.WarehouseId == warehouseId && s.StockItemId == stockItemId)
                .SumAsync(s => (decimal?)s.Quantity, cancellationToken) ?? 0;
            var reservedByOthers = await ReservedByOthersAsync(warehouseId, stockItemId, productionOrderId, cancellationToken);
            var quantity = Math.Min(target, Math.Max(onHand - reservedByOthers, 0));

            var reservation = existing.FirstOrDefault(r => r.StockItemId == stockItemId);
            if (quantity <= 0)
            {
                if (reservation is not null)
                {
                    db.StockReservations.Remove(reservation);
                }

                continue;
            }

            if (reservation is null)
            {
                reservation = new StockReservation
                {
                    ProductionOrderId = productionOrderId, WarehouseId = warehouseId, StockItemId = stockItemId,
                };
                db.StockReservations.Add(reservation);
            }

            reservation.SetQuantity(quantity);
            reservation.UpdatedAt = UtcNow;
            reservation.UpdatedBy = Actor;
        }

        return null;
    }

    public async Task ReleaseReservationsAsync(int productionOrderId, CancellationToken cancellationToken) =>
        db.StockReservations.RemoveRange(
            await db.StockReservations.Where(r => r.ProductionOrderId == productionOrderId).ToListAsync(cancellationToken));

    public async Task<Error?> PostCountAsync(
        int warehouseId, string countNo, IReadOnlyList<(int LocationId, int StockItemId, int? LotId, decimal Counted)> lines,
        CancellationToken cancellationToken)
    {
        if (db.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("Sayım farkları, fişin onayıyla aynı transaction içinde yazılmalıdır.");
        }

        var now = UtcNow;
        // Kilitler her zaman kart kimliği sırasıyla alınır.
        foreach (var itemLines in lines.GroupBy(l => l.StockItemId).OrderBy(g => g.Key))
        {
            if (!await appLock.TryAcquireAsync(StockLock(warehouseId, itemLines.Key), LockTimeout, cancellationToken))
            {
                return Busy;
            }

            var stocks = await LoadStocksAsync(warehouseId, itemLines.Key, cancellationToken);
            foreach (var (locationId, stockItemId, lotId, counted) in itemLines)
            {
                var stock = stocks.FirstOrDefault(s => s.LocationId == locationId && s.LotId == lotId);
                if (stock is null)
                {
                    stock = new Stock { WarehouseId = warehouseId, LocationId = locationId, StockItemId = stockItemId, LotId = lotId };
                    stocks.Add(stock);
                    db.Stocks.Add(stock);
                }

                var delta = counted - stock.Quantity;
                if (delta == 0)
                {
                    continue;
                }

                var rowBalance = stock.Apply(delta);
                db.StockTransactions.Add(new StockTransaction
                {
                    // Tekrar koruması fişin durumundadır; hareketin kimliği yalnızca benzersiz olmalıdır.
                    RequestId = Guid.NewGuid(),
                    Type = StockTransactionType.Adjustment,
                    WarehouseId = warehouseId,
                    LocationId = locationId,
                    StockItemId = stockItemId,
                    LotId = lotId,
                    Quantity = delta,
                    BalanceAfter = stocks.Sum(s => s.Quantity),
                    LocationBalanceAfter = stocks.Where(s => s.LocationId == locationId).Sum(s => s.Quantity),
                    LotBalanceAfter = lotId is null ? null : rowBalance,
                    DocumentNo = countNo,
                    Note = "Sayım farkı",
                    OccurredAt = now,
                    CreatedBy = Actor,
                });
            }
        }

        return null;
    }

    /// <summary>
    /// Adreslerden biri açık bir sayımın kapsamındaysa hareketi reddeden hata; sayım sürerken stok değişirse
    /// sayılan miktar ile kayıtlar aynı ana ait olmaz.
    /// </summary>
    private async Task<Error?> CountLockAsync(int warehouseId, IEnumerable<int> locationIds, CancellationToken cancellationToken)
    {
        var ids = locationIds.Distinct().ToList();
        var countNo = await db.StockCounts
            .Where(c => c.WarehouseId == warehouseId && c.Status == StockCountStatus.Open
                && (c.LocationId == null || ids.Contains(c.LocationId.Value)))
            .Select(c => c.CountNo)
            .FirstOrDefaultAsync(cancellationToken);

        return countNo is null
            ? null
            : new Error(
                ErrorCodes.StockCountInProgress,
                $"Bu adreste {countNo} numaralı sayım sürüyor; fiş onaylanana ya da iptal edilene dek stok hareketi yapılamaz.",
                ErrorType.Conflict);
    }

    /// <summary>
    /// Kartın bu depoda başka siparişlere ayrılmış miktarı; <paramref name="productionOrderId"/> boşsa tüm
    /// rezervasyonlar. (Depo, kart) kilidi altında çağrılır.
    /// </summary>
    private async Task<decimal> ReservedByOthersAsync(
        int warehouseId, int stockItemId, int? productionOrderId, CancellationToken cancellationToken, int? salesOrderId = null) =>
        (await db.StockReservations
            .Where(r => r.WarehouseId == warehouseId && r.StockItemId == stockItemId
                && (productionOrderId == null || r.ProductionOrderId != productionOrderId))
            .SumAsync(r => (decimal?)r.Quantity, cancellationToken) ?? 0)
        // Müşteri siparişlerine ayrılmış hazır mamul de başkasına ayrılmış sayılır.
        + (await db.SalesOrderReservations
            .Where(r => r.WarehouseId == warehouseId && r.StockItemId == stockItemId
                && (salesOrderId == null || r.SalesOrderId != salesOrderId))
            .SumAsync(r => (decimal?)r.Quantity, cancellationToken) ?? 0);

    /// <summary>
    /// Siparişin sevkiyatı, siparişe ayrılmış stoğu tüketir: ayırma sevk edilen kadar azalır (biten silinir), ayırmadan
    /// karşılanan adet kaleme "ayırmadan sevk edilen" olarak yazılır.
    /// </summary>
    private async Task ConsumeSalesReservationAsync(
        int salesOrderId, int warehouseId, int stockItemId, decimal shipped, CancellationToken cancellationToken)
    {
        var reservation = await db.SalesOrderReservations
            .Include(r => r.SalesOrderLine)
            .SingleOrDefaultAsync(r => r.SalesOrderId == salesOrderId && r.WarehouseId == warehouseId && r.StockItemId == stockItemId,
                cancellationToken);
        if (reservation is null)
        {
            return;
        }

        var consumed = Math.Min(reservation.Quantity, shipped);
        reservation.SalesOrderLine.ShippedFromReservation += consumed;
        reservation.Quantity -= consumed;
        if (reservation.Quantity <= 0)
        {
            db.SalesOrderReservations.Remove(reservation);
        }
        else
        {
            reservation.UpdatedAt = UtcNow;
            reservation.UpdatedBy = Actor;
        }
    }

    public async Task<Error?> ReserveForSalesOrderAsync(
        int salesOrderId, IReadOnlyList<(int LineId, int StockItemId, decimal Target)> targets, CancellationToken cancellationToken)
    {
        if (db.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("Sipariş ayırması, kart kilitleriyle aynı transaction içinde yazılmalıdır.");
        }

        var lineIds = targets.Select(t => t.LineId).ToList();
        var existing = await db.SalesOrderReservations
            .Where(r => r.SalesOrderId == salesOrderId && lineIds.Contains(r.SalesOrderLineId))
            .ToListAsync(cancellationToken);
        var itemIds = targets.Select(t => t.StockItemId).Distinct().ToList();
        // Kartın stoğu olan depolar; ayırma depo kimliği sırasıyla doldurulur.
        var stocked = await db.Stocks
            .Where(s => itemIds.Contains(s.StockItemId) && s.Quantity > 0)
            .GroupBy(s => new { s.StockItemId, s.WarehouseId })
            .Select(g => new { g.Key.StockItemId, g.Key.WarehouseId, OnHand = g.Sum(s => s.Quantity) })
            .ToListAsync(cancellationToken);

        foreach (var (lineId, stockItemId, target) in targets.OrderBy(t => t.StockItemId))
        {
            var remaining = Math.Max(target, 0);
            var current = existing.Where(r => r.SalesOrderLineId == lineId).ToList();
            foreach (var place in stocked.Where(s => s.StockItemId == stockItemId).OrderBy(s => s.WarehouseId))
            {
                if (!await appLock.TryAcquireAsync(StockLock(place.WarehouseId, stockItemId), LockTimeout, cancellationToken))
                {
                    return Busy;
                }

                var reservedByOthers = await ReservedByOthersAsync(place.WarehouseId, stockItemId, null, cancellationToken, salesOrderId);
                var quantity = Math.Min(remaining, Math.Max(place.OnHand - reservedByOthers, 0));
                var reservation = current.FirstOrDefault(r => r.WarehouseId == place.WarehouseId);
                if (quantity <= 0)
                {
                    continue;
                }

                if (reservation is null)
                {
                    reservation = new SalesOrderReservation
                    {
                        SalesOrderId = salesOrderId, SalesOrderLineId = lineId, WarehouseId = place.WarehouseId, StockItemId = stockItemId,
                    };
                    db.SalesOrderReservations.Add(reservation);
                }

                current.Remove(reservation);
                reservation.Quantity = quantity;
                reservation.UpdatedAt = UtcNow;
                reservation.UpdatedBy = Actor;
                remaining -= quantity;
            }

            // Artık gerekmeyen (ya da stoğu kalmamış depodaki) ayırmalar bırakılır.
            db.SalesOrderReservations.RemoveRange(current);
        }

        return null;
    }

    public async Task ReleaseSalesOrderReservationsAsync(int salesOrderId, CancellationToken cancellationToken) =>
        db.SalesOrderReservations.RemoveRange(
            await db.SalesOrderReservations.Where(r => r.SalesOrderId == salesOrderId).ToListAsync(cancellationToken));

    /// <summary>Sipariş için çıkılan miktarı siparişin rezervinden düşer; biten rezervasyon silinir.</summary>
    private async Task ConsumeReservationAsync(
        int warehouseId, int stockItemId, int productionOrderId, decimal issued, CancellationToken cancellationToken)
    {
        var reservation = await db.StockReservations.SingleOrDefaultAsync(
            r => r.ProductionOrderId == productionOrderId && r.WarehouseId == warehouseId && r.StockItemId == stockItemId,
            cancellationToken);
        if (reservation is null)
        {
            return;
        }

        if (reservation.Consume(issued) == 0)
        {
            db.StockReservations.Remove(reservation);
        }
        else
        {
            reservation.UpdatedAt = UtcNow;
            reservation.UpdatedBy = Actor;
        }
    }

    private static string ReservedNote(decimal reserved) =>
        reserved > 0 ? $", bunun {Format(reserved)} kadarı başka siparişlere ayrılmış" : "";

    private static Result<IReadOnlyList<Movement>> Planned(params Movement[] movements) =>
        Result<IReadOnlyList<Movement>>.Success(movements);

    private static string StockLock(int warehouseId, int stockItemId) => $"stock:{warehouseId}:{stockItemId}";

    /// <summary>Deponun genel adresi; adres belirtilmeyen girişler buraya yazılır.</summary>
    private Task<int> DefaultLocationIdAsync(int warehouseId, CancellationToken cancellationToken) =>
        db.WarehouseLocations
            .Where(l => l.WarehouseId == warehouseId && l.IsDefault)
            .Select(l => l.Id)
            .SingleAsync(cancellationToken);

    /// <summary>
    /// Kartın depodaki bakiye satırları, çıkışta kullanılacakları sırayla: önce genel adres, sonra adres kodu;
    /// takipli kartta her adreste en eski top önce. (Depo, kart) kilidi altında çağrılır.
    /// </summary>
    private Task<List<Stock>> LoadStocksAsync(int warehouseId, int stockItemId, CancellationToken cancellationToken) =>
        db.Stocks
            .IgnoreQueryFilters()
            .Where(s => s.WarehouseId == warehouseId && s.StockItemId == stockItemId)
            .OrderByDescending(s => s.Location.IsDefault).ThenBy(s => s.Location.Code).ThenBy(s => s.LotId)
            .ToListAsync(cancellationToken);

    /// <summary>Kartın (takipli kartta: topun) adresteki bakiye satırı; yoksa açılır.</summary>
    private Stock StockAt(List<Stock> stocks, int warehouseId, int locationId, int stockItemId, StockLot? lot)
    {
        // Yeni açılan topun kimliği kayıttan önce yoktur; satır topun kendisiyle eşleştirilir.
        var stock = stocks.FirstOrDefault(s =>
            s.LocationId == locationId && (lot is null ? s.LotId == null && s.Lot == null : s.Lot == lot || (lot.Id != 0 && s.LotId == lot.Id)));
        if (stock is null)
        {
            stock = new Stock { WarehouseId = warehouseId, LocationId = locationId, StockItemId = stockItemId, Lot = lot };
            stocks.Add(stock);
            db.Stocks.Add(stock);
        }

        return stock;
    }

    private StockLot NewLot(int stockItemId, decimal quantity, StockLotRequest? attributes, DateTime receivedAt, int? purchaseOrderId)
    {
        var lot = new StockLot
        {
            StockItemId = stockItemId,
            InitialQuantity = quantity,
            ReceivedAt = receivedAt,
            PurchaseOrderId = purchaseOrderId,
        };
        ApplyAttributes(lot, attributes ?? new StockLotRequest());
        db.StockLots.Add(lot);
        return lot;
    }

    private static void ApplyAttributes(StockLot lot, StockLotRequest attributes)
    {
        lot.BatchNo = string.IsNullOrWhiteSpace(attributes.BatchNo) ? null : attributes.BatchNo.Trim();
        lot.Shade = string.IsNullOrWhiteSpace(attributes.Shade) ? null : attributes.Shade.Trim();
        lot.Width = attributes.Width;
        lot.SupplierRollNo = string.IsNullOrWhiteSpace(attributes.SupplierRollNo) ? null : attributes.SupplierRollNo.Trim();
    }

    /// <summary>
    /// Çıkışı adreslere liste sırasıyla dağıtır. Toplam bakiyenin yettiğini çağıran önceden denetler.
    /// </summary>
    private static List<(Stock Stock, decimal Quantity)> Allocate(IReadOnlyList<Stock> stocks, decimal quantity)
    {
        var allocations = new List<(Stock, decimal)>();
        var remaining = quantity;
        foreach (var stock in stocks)
        {
            var taken = Math.Min(stock.Quantity, remaining);
            if (taken > 0)
            {
                allocations.Add((stock, taken));
                remaining -= taken;
            }
        }

        return remaining == 0
            ? allocations
            : throw new InvalidOperationException("Çıkış miktarı adres bakiyelerinin toplamını aşıyor.");
    }

    private static string Format(decimal quantity) => quantity.ToString("#,0.###", Turkish);

    /// <summary>
    /// Bir kartın bir ya da iki adresteki bakiyesini tek transaction'da değiştirir.
    /// </summary>
    /// <param name="quantity">İstekteki miktar; kartın birimine uygunluğu denetlenir.</param>
    /// <param name="places">Etkilenen uçlar (depo ve adres).</param>
    /// <param name="plan">Kilit altında okunan güncel adres bakiyelerine (uç sırasıyla) göre yapılacak hareketler.</param>
    private async Task<Result<StockMovementDto>> MoveAsync(
        Guid requestId,
        int stockItemId,
        decimal quantity,
        string quantityField,
        Place[] places,
        bool itemMustBeActive,
        Func<IReadOnlyList<decimal>, Result<IReadOnlyList<Movement>>> plan,
        Reference reference,
        LotChoice lotChoice,
        CancellationToken cancellationToken)
    {
        var item = await db.StockItems.AsNoTracking().SingleOrDefaultAsync(i => i.Id == stockItemId, cancellationToken);
        if (item is null)
        {
            return ReferenceValidation.InvalidReference("stockItemId", "Stok kartı bulunamadı.");
        }

        if (itemMustBeActive && !item.IsActive)
        {
            return ReferenceValidation.InvalidReference("stockItemId", "Stok kartı pasif durumda.");
        }

        if (!item.Accepts(quantity))
        {
            const string message = "Adetle izlenen stok kartında miktar tam sayı olmalıdır.";
            return new Error(
                ErrorCodes.StockQuantityInvalid, message, ErrorType.Validation,
                new Dictionary<string, string[]> { [quantityField] = [message] });
        }

        var warehouseIds = places.Select(p => p.WarehouseId).Distinct().ToArray();
        var warehouses = await db.Warehouses
            .AsNoTracking()
            .Where(w => warehouseIds.Contains(w.Id))
            .ToDictionaryAsync(w => w.Id, w => w.IsActive, cancellationToken);
        foreach (var place in places)
        {
            if (!warehouses.TryGetValue(place.WarehouseId, out var isActive))
            {
                return ReferenceValidation.InvalidReference(place.WarehouseField, "Depo bulunamadı.");
            }

            if (!isActive && place.Inbound)
            {
                return ReferenceValidation.InvalidReference(place.WarehouseField, "Depo pasif durumda; stok girişi yapılamaz.");
            }
        }

        // Takipli kartta hareket bir topa aittir: girişte yeni top açılır, diğerlerinde var olan top seçilir.
        if (!item.IsLotTracked && (lotChoice.LotId is not null || lotChoice.NewLot is not null))
        {
            const string message = "Stok kartı top / parti takipli değil; top bilgisi gönderilemez.";
            return new Error(
                ErrorCodes.StockLotNotAllowed, message, ErrorType.Validation,
                new Dictionary<string, string[]> { [lotChoice.CreatesLot ? "lot" : "lotId"] = [message] });
        }

        StockLot? lot = null;
        if (item.IsLotTracked && !lotChoice.CreatesLot)
        {
            if (lotChoice.LotId is null)
            {
                const string message = "Top / parti takipli stok kartında işlem yapılacak top seçilmelidir.";
                return new Error(
                    ErrorCodes.StockLotRequired, message, ErrorType.Validation,
                    new Dictionary<string, string[]> { ["lotId"] = [message] });
            }

            lot = await db.StockLots.SingleOrDefaultAsync(
                l => l.Id == lotChoice.LotId && l.StockItemId == stockItemId, cancellationToken);
            if (lot is null)
            {
                return ReferenceValidation.InvalidReference("lotId", "Top bulunamadı ya da bu stok kartına ait değil.");
            }

            // Adres verilmediyse top, çıkış deposunda bulunduğu (tek) adresten işlenir.
            if (places[0].LocationId is null)
            {
                var lotLocations = await db.Stocks
                    .Where(s => s.LotId == lot.Id && s.WarehouseId == places[0].WarehouseId && s.Quantity > 0)
                    .Select(s => s.LocationId)
                    .ToListAsync(cancellationToken);
                if (lotLocations.Count == 1)
                {
                    places = [places[0] with { LocationId = lotLocations[0] }, .. places[1..]];
                }
            }
        }

        var resolved = await ResolveLocationsAsync(places, warehouseIds, cancellationToken);
        if (!resolved.IsSuccess)
        {
            return resolved.Error!;
        }

        var locations = resolved.Value;
        foreach (var warehouseLocations in locations.GroupBy(l => l.WarehouseId))
        {
            if (await CountLockAsync(warehouseLocations.Key, warehouseLocations.Select(l => l.Id), cancellationToken) is { } countLock)
            {
                return countLock;
            }
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        foreach (var warehouseId in warehouseIds.Order())
        {
            if (!await appLock.TryAcquireAsync(StockLock(warehouseId, stockItemId), LockTimeout, cancellationToken))
            {
                return Busy;
            }
        }

        // Deponun tüm adresleri okunur: depo bakiyesi adreslerin toplamıdır.
        var stocks = await db.Stocks
            .Where(s => s.StockItemId == stockItemId && warehouseIds.Contains(s.WarehouseId))
            .ToListAsync(cancellationToken);

        var planned = plan(locations
            .Select(l => stocks.FirstOrDefault(s => s.LocationId == l.Id && s.LotId == lot?.Id)?.Quantity ?? 0m)
            .ToList());

        // Yinelenen istek, işin yeniden hesaplanmasından önce yakalanır (ör. sayım farkı artık sıfırdır).
        var replay = await db.StockTransactions
            .Where(t => t.RequestId == requestId)
            .Select(t => new { t.Type, t.WarehouseId, t.StockItemId })
            .ToListAsync(cancellationToken);
        if (replay.Count > 0)
        {
            var expectedTypes = planned.IsSuccess ? planned.Value.Select(m => m.Type).ToHashSet() : null;
            var same = replay.All(t => t.StockItemId == stockItemId && warehouseIds.Contains(t.WarehouseId)
                && (expectedTypes is null || expectedTypes.Contains(t.Type)));
            return same ? await LoadMovementAsync(requestId, duplicate: true, cancellationToken) : RequestIdReused;
        }

        if (!planned.IsSuccess)
        {
            return planned.Error!;
        }

        var now = UtcNow;
        if (item.IsLotTracked && lotChoice.CreatesLot)
        {
            lot = NewLot(stockItemId, quantity, lotChoice.NewLot, now, purchaseOrderId: null);
        }

        foreach (var movement in planned.Value)
        {
            var location = locations[movement.Place];
            var stock = StockAt(stocks, location.WarehouseId, location.Id, stockItemId, lot);
            decimal LocationBalance() => stocks.Where(s => s.LocationId == location.Id).Sum(s => s.Quantity);
            decimal WarehouseBalance() => stocks.Where(s => s.WarehouseId == location.WarehouseId).Sum(s => s.Quantity);

            if (!stock.CanApply(movement.Delta))
            {
                // Stok deponun başka adreslerinde ya da toplarındaysa kullanıcı neye baktığını bilmelidir.
                var message = lot is not null
                    ? $"{lot.LotNo} numaralı topun {location.Code} adresinde yeterli miktarı yok: mevcut {Format(stock.Quantity)}, " +
                      $"istenen {Format(-movement.Delta)}."
                    : WarehouseBalance() == stock.Quantity
                        ? $"Depoda yeterli stok yok: mevcut {Format(stock.Quantity)}, istenen {Format(-movement.Delta)}."
                        : $"{location.Code} adresinde yeterli stok yok: mevcut {Format(stock.Quantity)}, " +
                          $"istenen {Format(-movement.Delta)} (deponun toplamı {Format(WarehouseBalance())}).";
                return new Error(
                    ErrorCodes.InsufficientStock, message, ErrorType.BusinessRule,
                    new Dictionary<string, string[]> { [quantityField] = [message] });
            }

            // Siparişlere ayrılmış stok elle çıkılamaz, başka depoya aktarılamaz; siparişin kendi çıkışı rezervinden düşer.
            if (movement.Type is StockTransactionType.Issue or StockTransactionType.TransferOut)
            {
                var reserved = await ReservedByOthersAsync(
                    location.WarehouseId, stockItemId, reference.ProductionOrderId, cancellationToken);
                if (WarehouseBalance() - reserved < -movement.Delta)
                {
                    var message =
                        $"Depodaki stoğun {Format(reserved)} kadarı üretim siparişlerine ayrılmış: kullanılabilir " +
                        $"{Format(Math.Max(WarehouseBalance() - reserved, 0))}, istenen {Format(-movement.Delta)}.";
                    return new Error(
                        ErrorCodes.InsufficientStock, message, ErrorType.BusinessRule,
                        new Dictionary<string, string[]> { [quantityField] = [message] });
                }

                if (movement.Type == StockTransactionType.Issue && reference.ProductionOrderId is { } orderId)
                {
                    await ConsumeReservationAsync(location.WarehouseId, stockItemId, orderId, -movement.Delta, cancellationToken);
                }
            }

            var rowBalance = stock.Apply(movement.Delta);
            var counter = movement.CounterPlace is { } counterPlace ? locations[counterPlace] : null;
            var relocation = counter?.WarehouseId == location.WarehouseId;

            db.StockTransactions.Add(new StockTransaction
            {
                RequestId = requestId,
                Type = movement.Type,
                WarehouseId = location.WarehouseId,
                LocationId = location.Id,
                StockItemId = stockItemId,
                Lot = lot,
                Quantity = movement.Delta,
                // Adres değişiminde çıkış satırı yazılırken giriş henüz uygulanmamıştır; depo bakiyesi değişmez.
                BalanceAfter = WarehouseBalance() - (relocation && movement.Delta < 0 ? movement.Delta : 0),
                LocationBalanceAfter = LocationBalance(),
                LotBalanceAfter = lot is null ? null : rowBalance,
                CounterWarehouseId = relocation ? null : counter?.WarehouseId,
                CounterLocationId = counter?.Id,
                ProductionOrderId = reference.ProductionOrderId,
                DocumentNo = string.IsNullOrWhiteSpace(reference.DocumentNo) ? null : reference.DocumentNo.Trim(),
                Note = string.IsNullOrWhiteSpace(reference.Note) ? null : reference.Note.Trim(),
                OccurredAt = now,
                CreatedBy = Actor,
            });
        }

        if (await SaveAsync(cancellationToken) is { } saveError)
        {
            return saveError;
        }

        await transaction.CommitAsync(cancellationToken);
        return await LoadMovementAsync(requestId, duplicate: false, cancellationToken);
    }

    /// <summary>
    /// Uçların adreslerini çözer: adres verilmeyen uçta deponun genel adresi kullanılır; verilen adres uçtaki
    /// depoya ait olmalı, stok giren uçta aktif olmalıdır. İki uç aynı adres olamaz.
    /// </summary>
    private async Task<Result<IReadOnlyList<LocationRef>>> ResolveLocationsAsync(
        Place[] places, int[] warehouseIds, CancellationToken cancellationToken)
    {
        var requestedIds = places.Where(p => p.LocationId is not null).Select(p => p.LocationId!.Value).ToList();
        var known = await db.WarehouseLocations
            .AsNoTracking()
            .Where(l => warehouseIds.Contains(l.WarehouseId) && (l.IsDefault || requestedIds.Contains(l.Id)))
            .Select(l => new { l.Id, l.WarehouseId, l.Code, l.IsDefault, l.IsActive })
            .ToListAsync(cancellationToken);

        var locations = new List<LocationRef>();
        foreach (var place in places)
        {
            var location = place.LocationId is { } id
                ? known.FirstOrDefault(l => l.Id == id && l.WarehouseId == place.WarehouseId)
                : known.Single(l => l.WarehouseId == place.WarehouseId && l.IsDefault);
            if (location is null)
            {
                return ReferenceValidation.InvalidReference(
                    place.LocationField, "Adres bulunamadı ya da seçilen depoya ait değil.");
            }

            if (!location.IsActive && place.Inbound)
            {
                return ReferenceValidation.InvalidReference(
                    place.LocationField, "Adres pasif durumda; stok girişi yapılamaz.");
            }

            locations.Add(new LocationRef(location.Id, location.WarehouseId, location.Code, location.IsActive));
        }

        if (locations.Select(l => l.Id).Distinct().Count() != locations.Count)
        {
            const string message = "Çıkış ve giriş adresi aynı olamaz.";
            return new Error(
                ErrorCodes.ValidationError, message, ErrorType.Validation,
                new Dictionary<string, string[]> { [places[^1].LocationField] = [message] });
        }

        return locations;
    }

    /// <summary>
    /// Demetin ürün + renk + bedeni için mamul kartını bulur, yoksa açar. Kart açma, aynı mamulün eşzamanlı
    /// ilk girişleri iki kart oluşturmasın diye kilit altındadır. Kilit alınamazsa kart null döner.
    /// </summary>
    public async Task<Result<FinishedGoodCardDto>> OpenFinishedGoodCardAsync(
        FinishedGoodCardRequest request, CancellationToken cancellationToken)
    {
        var referenceError =
            await ReferenceValidation.RequireSelectableAsync(db.Products, request.ProductId, null, "productId", "Ürün", cancellationToken)
            ?? await ReferenceValidation.RequireSelectableAsync(db.Colors, request.ColorId, null, "colorId", "Renk", cancellationToken)
            ?? await ReferenceValidation.RequireSelectableAsync(db.Sizes, request.SizeId, null, "sizeId", "Beden", cancellationToken);
        if (referenceError is not null)
        {
            return referenceError;
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var (item, isNew) = await GetOrCreateFinishedGoodAsync(request.ProductId, request.ColorId, request.SizeId, cancellationToken);
        if (item is null)
        {
            return Busy;
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new FinishedGoodCardDto(item.Id, item.Code, item.Name, isNew);
    }

    private async Task<(StockItem? Item, bool IsNew)> GetOrCreateFinishedGoodAsync(
        Bundle bundle, CancellationToken cancellationToken)
    {
        var productId = await db.ProductionOrders
            .IgnoreQueryFilters()
            .Where(o => o.Id == bundle.ProductionOrderId)
            .Select(o => o.ProductId)
            .SingleAsync(cancellationToken);
        return await GetOrCreateFinishedGoodAsync(productId, bundle.ColorId, bundle.SizeId, cancellationToken);
    }

    private async Task<(StockItem? Item, bool IsNew)> GetOrCreateFinishedGoodAsync(
        int productId, int colorId, int sizeId, CancellationToken cancellationToken)
    {
        if (!await appLock.TryAcquireAsync(
                $"stock-item:fg:{productId}:{colorId}:{sizeId}", LockTimeout, cancellationToken))
        {
            return (null, false);
        }

        var existing = await db.StockItems.SingleOrDefaultAsync(
            i => i.ProductId == productId && i.ColorId == colorId && i.SizeId == sizeId, cancellationToken);
        if (existing is not null)
        {
            return (existing, false);
        }

        // Sonradan silinmiş ana veriyle de kart açılabilir: üretilmiş mal depoya alınabilmelidir.
        var product = await db.Products.IgnoreQueryFilters()
            .Where(p => p.Id == productId).Select(p => new { p.ProductCode, p.Name }).SingleAsync(cancellationToken);
        var color = await db.Colors.IgnoreQueryFilters()
            .Where(c => c.Id == colorId).Select(c => new { c.ColorCode, c.Name }).SingleAsync(cancellationToken);
        var size = await db.Sizes.IgnoreQueryFilters()
            .Where(s => s.Id == sizeId).Select(s => new { s.SizeCode, s.Name }).SingleAsync(cancellationToken);

        var code = $"{product.ProductCode}-{color.ColorCode}-{size.SizeCode}";
        if (await db.StockItems.AnyAsync(i => i.Code == code, cancellationToken))
        {
            // Aynı kodla elle açılmış bir malzeme kartı var: çakışmayan bir kod kullanılır.
            code = $"MAMUL-{productId}-{colorId}-{sizeId}";
        }

        var item = new StockItem
        {
            Code = code,
            Name = $"{product.Name} / {color.Name} / {size.Name}",
            ItemType = StockItemType.FinishedGood,
            Unit = StockUnit.Piece,
            ProductId = productId,
            ColorId = colorId,
            SizeId = sizeId,
        };
        db.StockItems.Add(item);
        return (item, true);
    }

    private Task<OperationFlow> LoadFlowAsync(Bundle bundle, CancellationToken cancellationToken) =>
        db.LoadFlowAsync(bundle, cancellationToken);

    private async Task<Error?> SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return null;
        }
        catch (DbUpdateConcurrencyException)
        {
            return new Error(
                ErrorCodes.ConcurrencyConflict, "Stok aynı anda başka bir işlemle değişti. Lütfen tekrar deneyin.",
                ErrorType.Conflict);
        }
        catch (DbUpdateException)
        {
            // Tekil index'ler: aynı istek kimliği ya da aynı demet eşzamanlı başka bir istekle işlendi.
            return new Error(
                ErrorCodes.ResourceBusy, "İşlem aynı anda başka bir istekle yapıldı. Lütfen listeyi yenileyip kontrol edin.",
                ErrorType.Conflict);
        }
    }

    private async Task<StockMovementDto> LoadMovementAsync(Guid requestId, bool duplicate, CancellationToken cancellationToken) =>
        new(
            duplicate,
            await Project(db.StockTransactions.Where(t => t.RequestId == requestId).OrderBy(t => t.Id))
                .ToListAsync(cancellationToken));

    // Geçmiş hareketler, sonradan silinen depo / kart için de eksiksiz görünür.
    private static IQueryable<StockTransactionDto> Project(IQueryable<StockTransaction> transactions) =>
        transactions.AsNoTracking().IgnoreQueryFilters().Select(t => new StockTransactionDto(
            t.Id,
            t.Type,
            t.WarehouseId,
            t.Warehouse.Code,
            t.Warehouse.Name,
            t.StockItemId,
            t.StockItem.Code,
            t.StockItem.Name,
            t.StockItem.Unit,
            t.Quantity,
            t.BalanceAfter,
            t.CounterWarehouse == null ? null : t.CounterWarehouse.Name,
            t.BundleId,
            t.Bundle == null ? null : t.Bundle.BundleNo,
            t.ProductionOrderId,
            t.ProductionOrder == null ? null : t.ProductionOrder.OrderNo,
            t.DocumentNo,
            t.Note,
            t.OccurredAt,
            t.CreatedBy,
            t.PurchaseOrderId,
            t.PurchaseOrder == null ? null : t.PurchaseOrder.OrderNo,
            t.ShipmentId,
            t.Shipment == null ? null : t.Shipment.ShipmentNo,
            t.UnitCost,
            t.LocationId,
            t.Location.Code,
            t.LocationBalanceAfter,
            t.CounterLocation == null ? null : t.CounterLocation.Code,
            t.LotId,
            t.Lot == null ? null : t.Lot.LotNo,
            t.LotBalanceAfter));
}
