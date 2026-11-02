using ERP.Konfeksiyon.Domain.Authorization;
using ERP.Core.Infrastructure.Web.Authorization;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Konfeksiyon.Application.Features.Purchasing;
using ERP.Konfeksiyon.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Konfeksiyon.Infrastructure.Api.Controllers;

[Route("api/v1/suppliers")]
[ResourcePermissions(KonfeksiyonPermissions.Purchase.View, KonfeksiyonPermissions.Purchase.Manage)]
public sealed class SuppliersController(IMasterDataService<SupplierDto, SupplierRequest> service)
    : MasterDataController<SupplierDto, SupplierRequest>(service);

[Route("api/v1/supply-suggestions")]
public sealed class SupplySuggestionsController(ISupplyPlanningService service) : ApiControllerBase
{
    /// <summary>
    /// Tedarik önerisi: açık üretim siparişlerinin ihtiyacı ve asgari stok için eksik kalan malzemeler
    /// (eksik = ihtiyaç + asgari stok − stok − yoldaki ve taslaktaki satın alma).
    /// </summary>
    [HttpGet]
    [HasPermission(KonfeksiyonPermissions.Purchase.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SupplySuggestionDto>>>> GetList(
        [FromQuery] SupplySuggestionQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetSuggestionsAsync(query, cancellationToken), HttpContext));

    /// <summary>
    /// Seçilen satırlardan, tedarikçi başına bir taslak satın alma siparişi açar. Hatalar:
    /// SUPPLY_SUPPLIER_REQUIRED, STOCK_QUANTITY_INVALID, MATERIAL_NOT_ALLOWED.
    /// </summary>
    [HttpPost("orders")]
    [HasPermission(KonfeksiyonPermissions.Purchase.Manage)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PurchaseOrderDto>>>> CreateOrders(
        CreateSupplyOrdersRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.CreateOrdersAsync(request, cancellationToken));
}

[Route("api/v1/purchase-orders")]
public sealed class PurchaseOrdersController(IPurchaseOrderService service) : ApiControllerBase
{
    /// <summary>Satın alma siparişleri, en yeniden eskiye (sayfalı; arama, durum, tedarikçi, mal kabul bekleyenler).</summary>
    [HttpGet]
    [HasPermission(KonfeksiyonPermissions.Purchase.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<PurchaseOrderListItemDto>>>> GetList(
        [FromQuery] PurchaseOrderListQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetListAsync(query, cancellationToken), HttpContext));

    /// <summary>Sipariş ayrıntısı: kalemler, teslim alınan / kalan miktarlar ve yapılabilecek durum geçişleri.</summary>
    [HttpGet("{id:int}")]
    [HasPermission(KonfeksiyonPermissions.Purchase.View)]
    public async Task<ActionResult<ApiResponse<PurchaseOrderDto>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetAsync(id, cancellationToken));

    /// <summary>Taslak sipariş oluşturur; sipariş numarası sistem tarafından verilir.</summary>
    [HttpPost]
    [HasPermission(KonfeksiyonPermissions.Purchase.Manage)]
    public async Task<ActionResult<ApiResponse<PurchaseOrderDto>>> Create(
        PurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { id = result.Value.Id }, ApiResponse.Success(result.Value, HttpContext))
            : FromResult(result);
    }

    /// <summary>Taslak siparişin başlığını ve kalemlerini günceller (rowVersion zorunlu).</summary>
    [HttpPut("{id:int}")]
    [HasPermission(KonfeksiyonPermissions.Purchase.Manage)]
    public async Task<ActionResult<ApiResponse<PurchaseOrderDto>>> Update(
        int id, PurchaseOrderRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.UpdateAsync(id, request, cancellationToken));

    /// <summary>Taslağı onaylar; içerik kilitlenir ve mal kabul yapılabilir. Ayrı bir yetki ister (Purchase.Approve).</summary>
    [HttpPost("{id:int}/approve")]
    [HasPermission(KonfeksiyonPermissions.Purchase.Approve)]
    public Task<ActionResult<ApiResponse<PurchaseOrderDto>>> Approve(
        int id, PurchaseOrderTransitionRequest request, CancellationToken cancellationToken) =>
        TransitionAsync(id, PurchaseOrderStatus.Approved, request, cancellationToken);

    /// <summary>Henüz mal kabul yapılmamış onaylı siparişi düzenlenebilmesi için taslağa alır.</summary>
    [HttpPost("{id:int}/revert-to-draft")]
    [HasPermission(KonfeksiyonPermissions.Purchase.Manage)]
    public Task<ActionResult<ApiResponse<PurchaseOrderDto>>> RevertToDraft(
        int id, PurchaseOrderTransitionRequest request, CancellationToken cancellationToken) =>
        TransitionAsync(id, PurchaseOrderStatus.Draft, request, cancellationToken);

    /// <summary>Kısmen teslim alınmış siparişi, kalan miktarı beklemeden kapatır.</summary>
    [HttpPost("{id:int}/close")]
    [HasPermission(KonfeksiyonPermissions.Purchase.Close)]
    public Task<ActionResult<ApiResponse<PurchaseOrderDto>>> Close(
        int id, PurchaseOrderTransitionRequest request, CancellationToken cancellationToken) =>
        TransitionAsync(id, PurchaseOrderStatus.Closed, request, cancellationToken);

    /// <summary>Henüz mal kabul yapılmamış siparişi iptal eder.</summary>
    [HttpPost("{id:int}/cancel")]
    [HasPermission(KonfeksiyonPermissions.Purchase.Cancel)]
    public Task<ActionResult<ApiResponse<PurchaseOrderDto>>> Cancel(
        int id, PurchaseOrderTransitionRequest request, CancellationToken cancellationToken) =>
        TransitionAsync(id, PurchaseOrderStatus.Cancelled, request, cancellationToken);

    /// <summary>Taslak siparişi siler (soft delete); numarası yeniden kullanılmaz.</summary>
    [HttpDelete("{id:int}")]
    [HasPermission(KonfeksiyonPermissions.Purchase.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(int id, CancellationToken cancellationToken) =>
        FromResult(await service.DeleteAsync(id, cancellationToken));

    /// <summary>Siparişin mal kabulleri, eskiden yeniye.</summary>
    [HttpGet("{id:int}/receipts")]
    [HasPermission(KonfeksiyonPermissions.Purchase.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<GoodsReceiptDto>>>> GetReceipts(
        int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetReceiptsAsync(id, cancellationToken));

    /// <summary>
    /// Mal kabul: siparişin kalemlerini (kısmen de olabilir) bir depoya teslim alır. Stok girişi ve hareketi aynı
    /// işlemde yazılır. Aynı requestId ile yinelenen istek stoğu ikinci kez artırmaz. Hatalar:
    /// PURCHASE_ORDER_NOT_RECEIVABLE, RECEIPT_EXCEEDS_ORDERED, STOCK_QUANTITY_INVALID.
    /// </summary>
    [HttpPost("{id:int}/receipts")]
    [HasPermission(KonfeksiyonPermissions.Stock.Manage)]
    public async Task<ActionResult<ApiResponse<GoodsReceiptResultDto>>> Receive(
        int id, GoodsReceiptRequest request, CancellationToken cancellationToken)
    {
        var result = await service.ReceiveAsync(id, request, cancellationToken);
        return result.IsSuccess && !result.Value.Duplicate
            ? StatusCode(StatusCodes.Status201Created, ApiResponse.Success(result.Value, HttpContext))
            : FromResult(result);
    }

    private async Task<ActionResult<ApiResponse<PurchaseOrderDto>>> TransitionAsync(
        int id, PurchaseOrderStatus target, PurchaseOrderTransitionRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.TransitionAsync(id, target, request.RowVersion, cancellationToken));
}
