using ERP.Core.Application.Common;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Infrastructure.Web.Authorization;
using ERP.Dokuma.Application.Features.Purchasing;
using ERP.Dokuma.Domain.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Dokuma.Infrastructure.Api.Controllers;

/// <summary>
/// İplik ve malzeme satın alma: taslak, onay, kısmi teslim alma (iplik ve malzeme lotu girişi), kapatma ve tedarik önerileri.
/// </summary>
[Route("api/v1/dokuma-purchase-orders")]
public sealed class DokumaPurchaseOrdersController(IDokumaPurchasingService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(DokumaPermissions.PurchaseOrder.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<DokumaPurchaseOrderListItemDto>>>> GetList(
        [FromQuery] DokumaPurchaseOrderQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetListAsync(query, cancellationToken), HttpContext));

    /// <summary>Kritik stok altındaki malzemeler ve kendi malı siparişlerin iplik eksikleri.</summary>
    [HttpGet("suggestions")]
    [HasPermission(DokumaPermissions.PurchaseOrder.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DokumaSupplySuggestionDto>>>> Suggestions(CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetSuggestionsAsync(cancellationToken), HttpContext));

    /// <summary>Seçilen önerilerden tedarikçi başına bir taslak sipariş açar.</summary>
    [HttpPost("from-suggestions")]
    [HasPermission(DokumaPermissions.PurchaseOrder.Manage)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DokumaPurchaseDraftDto>>>> FromSuggestions(
        CreateDokumaPurchaseDraftsRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.CreateDraftsAsync(request, cancellationToken));

    [HttpGet("{id:int}")]
    [HasPermission(DokumaPermissions.PurchaseOrder.View)]
    public async Task<ActionResult<ApiResponse<DokumaPurchaseOrderDto>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetAsync(id, cancellationToken));

    [HttpPost]
    [HasPermission(DokumaPermissions.PurchaseOrder.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaPurchaseOrderDto>>> Create(DokumaPurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        var result = await service.SaveAsync(null, request, cancellationToken);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, ApiResponse.Success(result.Value, HttpContext))
            : FromResult(result);
    }

    [HttpPut("{id:int}")]
    [HasPermission(DokumaPermissions.PurchaseOrder.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaPurchaseOrderDto>>> Update(int id, DokumaPurchaseOrderRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SaveAsync(id, request, cancellationToken));

    /// <summary>Taslak siparişi siler; onaylanmış sipariş iptal edilir.</summary>
    [HttpDelete("{id:int}")]
    [HasPermission(DokumaPermissions.PurchaseOrder.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(int id, CancellationToken cancellationToken) =>
        FromResult(await service.DeleteAsync(id, cancellationToken));

    [HttpPost("{id:int}/approve")]
    [HasPermission(DokumaPermissions.PurchaseOrder.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaPurchaseOrderDto>>> Approve(int id, CancellationToken cancellationToken) =>
        FromResult(await service.ApproveAsync(id, cancellationToken));

    [HttpPost("{id:int}/reopen")]
    [HasPermission(DokumaPermissions.PurchaseOrder.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaPurchaseOrderDto>>> Reopen(int id, CancellationToken cancellationToken) =>
        FromResult(await service.ReopenAsync(id, cancellationToken));

    [HttpPost("{id:int}/cancel")]
    [HasPermission(DokumaPermissions.PurchaseOrder.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaPurchaseOrderDto>>> Cancel(int id, CancellationToken cancellationToken) =>
        FromResult(await service.CancelAsync(id, cancellationToken));

    /// <summary>Kısmi teslim alınmış siparişin kalanını kapatır; kalan miktar artık beklenmez.</summary>
    [HttpPost("{id:int}/close")]
    [HasPermission(DokumaPermissions.PurchaseOrder.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaPurchaseOrderDto>>> Close(int id, CancellationToken cancellationToken) =>
        FromResult(await service.CloseAsync(id, cancellationToken));

    /// <summary>Teslim alır: iplik satırı iplik lotu, malzeme satırı malzeme lotu olarak girer.</summary>
    [HttpPost("{id:int}/receive")]
    [HasPermission(DokumaPermissions.PurchaseOrder.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaPurchaseOrderDto>>> Receive(int id, DokumaPurchaseReceiptRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.ReceiveAsync(id, request, cancellationToken));
}
