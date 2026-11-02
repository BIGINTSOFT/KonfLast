using ERP.Boyahane.Application.Features.Purchasing;
using ERP.Boyahane.Domain.Authorization;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Infrastructure.Web.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Boyahane.Infrastructure.Api.Controllers;

[Route("api/v1/boyahane-suppliers")]
[ResourcePermissions(BoyahanePermissions.Purchase.View, BoyahanePermissions.Purchase.Manage)]
public sealed class BoyahaneSuppliersController(IMasterDataService<BoyahaneSupplierDto, BoyahaneSupplierRequest> service)
    : MasterDataController<BoyahaneSupplierDto, BoyahaneSupplierRequest>(service);

/// <summary>Boya ve kimyasal satın alma: sipariş, kısmi teslim alma, teslimde otomatik lot girişi, kritik stok önerisi.</summary>
[Route("api/v1/boyahane-purchase-orders")]
public sealed class BoyahanePurchaseOrdersController(IBoyahanePurchasingService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(BoyahanePermissions.Purchase.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<BoyahanePurchaseOrderListItemDto>>>> GetList([FromQuery] BoyahanePurchaseOrderQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetListAsync(query, cancellationToken), HttpContext));

    [HttpGet("suggestions")]
    [HasPermission(BoyahanePermissions.Purchase.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BoyahaneSupplySuggestionDto>>>> Suggestions(CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetSuggestionsAsync(cancellationToken), HttpContext));

    [HttpGet("{id:int}")]
    [HasPermission(BoyahanePermissions.Purchase.View)]
    public async Task<ActionResult<ApiResponse<BoyahanePurchaseOrderDto>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetAsync(id, cancellationToken));

    [HttpPost]
    [HasPermission(BoyahanePermissions.Purchase.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahanePurchaseOrderDto>>> Create(BoyahanePurchaseOrderRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SaveAsync(null, request, cancellationToken));

    [HttpPut("{id:int}")]
    [HasPermission(BoyahanePermissions.Purchase.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahanePurchaseOrderDto>>> Update(int id, BoyahanePurchaseOrderRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SaveAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/confirm")]
    [HasPermission(BoyahanePermissions.Purchase.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahanePurchaseOrderDto>>> Confirm(int id, CancellationToken cancellationToken) =>
        FromResult(await service.ConfirmAsync(id, cancellationToken));

    [HttpPost("{id:int}/reopen")]
    [HasPermission(BoyahanePermissions.Purchase.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahanePurchaseOrderDto>>> Reopen(int id, CancellationToken cancellationToken) =>
        FromResult(await service.ReopenAsync(id, cancellationToken));

    [HttpPost("{id:int}/cancel")]
    [HasPermission(BoyahanePermissions.Purchase.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahanePurchaseOrderDto>>> Cancel(int id, CancellationToken cancellationToken) =>
        FromResult(await service.CancelAsync(id, cancellationToken));

    /// <summary>Kısmi teslim alınmış siparişin kalanını kapatır; kalan miktar artık beklenmez.</summary>
    [HttpPost("{id:int}/close")]
    [HasPermission(BoyahanePermissions.Purchase.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahanePurchaseOrderDto>>> Close(int id, CancellationToken cancellationToken) =>
        FromResult(await service.CloseAsync(id, cancellationToken));

    /// <summary>Teslim alır: her satır depoya bir lot olarak girer, birim maliyet sipariş fiyatından gelir.</summary>
    [HttpPost("{id:int}/receive")]
    [HasPermission(BoyahanePermissions.Purchase.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahanePurchaseOrderDto>>> Receive(int id, BoyahanePurchaseReceiptRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.ReceiveAsync(id, request, cancellationToken));
}
