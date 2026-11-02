using ERP.Konfeksiyon.Domain.Authorization;
using ERP.Core.Infrastructure.Web.Authorization;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Konfeksiyon.Application.Features.Warehousing;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Konfeksiyon.Infrastructure.Api.Controllers;

[Route("api/v1/warehouses")]
[ResourcePermissions(KonfeksiyonPermissions.Stock.View, KonfeksiyonPermissions.Warehouse.Manage)]
public sealed class WarehousesController(IMasterDataService<WarehouseDto, WarehouseRequest> service)
    : MasterDataController<WarehouseDto, WarehouseRequest>(service);

/// <summary>
/// Depo adresleri (bölge / raf / göz). Adres kodu seviyelerden oluşur (A-01-03) ve deposunda benzersizdir. Her
/// deponun genel adresi depoyla birlikte açılır; silinemez ve pasif yapılamaz.
/// </summary>
[Route("api/v1/warehouse-locations")]
[ResourcePermissions(KonfeksiyonPermissions.Stock.View, KonfeksiyonPermissions.Warehouse.Manage)]
public sealed class WarehouseLocationsController(IMasterDataService<WarehouseLocationDto, WarehouseLocationRequest> service)
    : MasterDataController<WarehouseLocationDto, WarehouseLocationRequest>(service);

[Route("api/v1/warehouse-locations")]
public sealed class WarehouseLocationBulkController(IWarehouseLocationBulkService service) : ApiControllerBase
{
    /// <summary>
    /// Toplu adres açar: bir bölgede raf (ve isteğe bağlı göz) aralığı, örn. A-01-01 … A-10-04. Depoda zaten var
    /// olan adresler atlanır; yanıtta açılan ve atlanan adres sayısı döner. Tek seferde en fazla 2.000 adres.
    /// </summary>
    [HttpPost("bulk")]
    [HasPermission(KonfeksiyonPermissions.Warehouse.Manage)]
    public async Task<ActionResult<ApiResponse<BulkLocationResultDto>>> Create(
        BulkLocationRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.CreateAsync(request, cancellationToken));
}

/// <summary>Stok kartları: malzemeler kullanıcı tarafından tanımlanır, mamul kartlarını sistem açar.</summary>
[Route("api/v1/stock-items")]
[ResourcePermissions(KonfeksiyonPermissions.Stock.View, KonfeksiyonPermissions.Stock.Manage)]
public sealed class StockItemsController(IMasterDataService<StockItemDto, StockItemRequest> service)
    : MasterDataController<StockItemDto, StockItemRequest>(service);

[Route("api/v1/stock")]
public sealed class StockController(IStockService service, IStockScanService scanner) : ApiControllerBase
{
    /// <summary>
    /// Okutulan barkodu çözer (el terminali): top numarası (T00000123), adres etiketi (ADR00000042), adres kodu
    /// ya da stok kartı kodu. Yanıtta neyin okutulduğu ve ilgili stok satırları vardır. Hatalar: NOT_FOUND,
    /// SCAN_AMBIGUOUS (adres kodu birden fazla depoda; warehouseId verin ya da adres etiketini okutun).
    /// </summary>
    [HttpGet("scan")]
    [HasPermission(KonfeksiyonPermissions.Stock.View)]
    public async Task<ActionResult<ApiResponse<StockScanDto>>> Scan(
        [FromQuery] string? code, [FromQuery] int? warehouseId, CancellationToken cancellationToken) =>
        FromResult(await scanner.ResolveAsync(code, warehouseId, cancellationToken));

    /// <summary>
    /// Sipariş numarasıyla toplama listesi (el terminali): siparişin kalan ihtiyacı için hangi adresten, hangi
    /// toptan ne kadar alınacağı. Hatalar: NOT_FOUND, MATERIAL_WAREHOUSE_REQUIRED (depo belirsiz; warehouseId verin).
    /// </summary>
    [HttpGet("pick-list")]
    [HasPermission(KonfeksiyonPermissions.Stock.View)]
    public async Task<ActionResult<ApiResponse<ERP.Konfeksiyon.Application.Features.Costing.PickListDto>>> PickList(
        [FromQuery] string? orderNo, [FromQuery] int? warehouseId, CancellationToken cancellationToken) =>
        FromResult(await scanner.PickListAsync(orderNo, warehouseId, cancellationToken));

    /// <summary>Depo ve kart bazında bakiyeler (sayfalı; depo, kart türü, arama; includeZero ile sıfır bakiyeler).</summary>
    [HttpGet]
    [HasPermission(KonfeksiyonPermissions.Stock.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<StockBalanceDto>>>> GetBalances(
        [FromQuery] StockBalanceQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetBalancesAsync(query, cancellationToken), HttpContext));

    /// <summary>Adres ve kart bazında bakiyeler (sayfalı; depo, adres, kart, tür, arama; includeZero ile sıfır bakiyeler).</summary>
    [HttpGet("by-location")]
    [HasPermission(KonfeksiyonPermissions.Stock.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<StockLocationBalanceDto>>>> GetLocationBalances(
        [FromQuery] StockLocationBalanceQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetLocationBalancesAsync(query, cancellationToken), HttpContext));

    /// <summary>
    /// Top / parti takipli kartların top ve adres bazında bakiyeleri, topun özellikleriyle (sayfalı; depo, adres,
    /// kart, top, satın alma siparişi, arama; includeZero ile tükenmiş toplar).
    /// </summary>
    [HttpGet("lots")]
    [HasPermission(KonfeksiyonPermissions.Stock.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<StockLotBalanceDto>>>> GetLotBalances(
        [FromQuery] StockLotBalanceQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetLotBalancesAsync(query, cancellationToken), HttpContext));

    /// <summary>Topun özelliklerini (parti no, ton, en, tedarikçi top no) düzeltir; miktar ve yer değişmez.</summary>
    [HttpPut("lots/{lotId:int}")]
    [HasPermission(KonfeksiyonPermissions.Stock.EditLot)]
    public async Task<ActionResult<ApiResponse<object?>>> UpdateLot(
        int lotId, StockLotUpdateRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.UpdateLotAsync(lotId, request, cancellationToken));

    /// <summary>Deponun adresleri (hareket formlarındaki seçim için; en fazla 1.000): önce genel adres.</summary>
    [HttpGet("locations")]
    [HasPermission(KonfeksiyonPermissions.Stock.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WarehouseLocationOptionDto>>>> GetLocationOptions(
        [FromQuery] int warehouseId, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetLocationOptionsAsync(warehouseId, cancellationToken), HttpContext));

    /// <summary>Stok hareketleri, en yeniden eskiye (sayfalı; depo, adres, kart, tür, sipariş, tarih, arama).</summary>
    [HttpGet("transactions")]
    [HasPermission(KonfeksiyonPermissions.Stock.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<StockTransactionDto>>>> GetTransactions(
        [FromQuery] StockTransactionQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetTransactionsAsync(query, cancellationToken), HttpContext));

    /// <summary>Tamamlanmış, henüz depoya teslim alınmamış demetler (sayfalı; en eski önce).</summary>
    [HttpGet("receivable-bundles")]
    [HasPermission(KonfeksiyonPermissions.Stock.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<ReceivableBundleDto>>>> GetReceivableBundles(
        [FromQuery] ReceivableBundleQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetReceivableBundlesAsync(query, cancellationToken), HttpContext));

    /// <summary>
    /// Depoya giriş. Aynı requestId ile yinelenen istek hareketi ikinci kez yazmaz (duplicate: true). locationId
    /// verilmezse giriş deponun genel adresine yapılır (çıkış ve sayımda da böyledir).
    /// </summary>
    [HttpPost("receipts")]
    [HasPermission(KonfeksiyonPermissions.Stock.Manage)]
    public async Task<ActionResult<ApiResponse<StockMovementDto>>> Receive(
        StockReceiptRequest request, CancellationToken cancellationToken) =>
        Moved(await service.ReceiveAsync(request, cancellationToken));

    /// <summary>Depodan çıkış. Stok eksiye düşemez (INSUFFICIENT_STOCK).</summary>
    [HttpPost("issues")]
    [HasPermission(KonfeksiyonPermissions.Stock.Manage)]
    public async Task<ActionResult<ApiResponse<StockMovementDto>>> Issue(
        StockIssueRequest request, CancellationToken cancellationToken) =>
        Moved(await service.IssueAsync(request, cancellationToken));

    /// <summary>
    /// Depolar arası transfer ya da (iki depo aynıysa) depo içinde adres değişimi: çıkış ve giriş tek işlemde yazılır.
    /// </summary>
    [HttpPost("transfers")]
    [HasPermission(KonfeksiyonPermissions.Stock.Manage)]
    public async Task<ActionResult<ApiResponse<StockMovementDto>>> Transfer(
        StockTransferRequest request, CancellationToken cancellationToken) =>
        Moved(await service.TransferAsync(request, cancellationToken));

    /// <summary>Sayım düzeltmesi: adresin bakiyesi sayılan miktara getirilir; fark hareket olarak yazılır. Not zorunludur.</summary>
    [HttpPost("adjustments")]
    [HasPermission(KonfeksiyonPermissions.Stock.Adjust)]
    public async Task<ActionResult<ApiResponse<StockMovementDto>>> Adjust(
        StockAdjustmentRequest request, CancellationToken cancellationToken) =>
        Moved(await service.AdjustAsync(request, cancellationToken));

    /// <summary>
    /// Tamamlanmış demeti depoya teslim alır: sağlam adedi (sunucu hesaplar) mamul stoğuna girer, demet kapanır
    /// (CLOSED) ve artık tamire dönemez. Hatalar: BUNDLE_NOT_COMPLETED, BUNDLE_ALREADY_RECEIVED, NOTHING_TO_RECEIVE.
    /// </summary>
    [HttpPost("production-receipts")]
    [HasPermission(KonfeksiyonPermissions.Stock.Manage)]
    public async Task<ActionResult<ApiResponse<StockMovementDto>>> ReceiveFromProduction(
        ProductionReceiptRequest request, CancellationToken cancellationToken) =>
        Moved(await service.ReceiveFromProductionAsync(request, cancellationToken));

    /// <summary>
    /// Ürün / renk / beden için mamul kartını açar (zaten varsa onu döner). Sistemde üretilmemiş mamulün (açılış
    /// stoğu, iade) stoğa girilebilmesi içindir; miktar ardından olağan stok girişiyle girilir.
    /// </summary>
    [HttpPost("finished-good-cards")]
    [HasPermission(KonfeksiyonPermissions.Stock.Manage)]
    public async Task<ActionResult<ApiResponse<FinishedGoodCardDto>>> OpenFinishedGoodCard(
        FinishedGoodCardRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.OpenFinishedGoodCardAsync(request, cancellationToken));

    private ActionResult<ApiResponse<StockMovementDto>> Moved(Result<StockMovementDto> result) =>
        result.IsSuccess && !result.Value.Duplicate
            ? StatusCode(StatusCodes.Status201Created, ApiResponse.Success(result.Value, HttpContext))
            : FromResult(result);
}
