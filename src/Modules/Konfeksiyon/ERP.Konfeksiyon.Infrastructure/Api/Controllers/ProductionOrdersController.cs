using ERP.Konfeksiyon.Domain.Authorization;
using ERP.Core.Infrastructure.Web.Authorization;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Konfeksiyon.Application.Features.ProductionOrders;
using ERP.Konfeksiyon.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Konfeksiyon.Infrastructure.Api.Controllers;

[Route("api/v1/production-orders")]
public sealed class ProductionOrdersController(IProductionOrderService service) : ApiControllerBase
{
    /// <summary>Sipariş listesi, en yeniden eskiye (sayfalı; arama, durum, müşteri ve termin filtresi).</summary>
    [HttpGet]
    [HasPermission(KonfeksiyonPermissions.ProductionOrder.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<ProductionOrderListItemDto>>>> GetList(
        [FromQuery] ProductionOrderListQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetListAsync(query, cancellationToken), HttpContext));

    /// <summary>Banta planlanabilecek (onaylı / üretimdeki) siparişler; seçim kutuları için.</summary>
    [HttpGet("lookup")]
    [HasPermission(KonfeksiyonPermissions.ProductionOrder.View, KonfeksiyonPermissions.ProductionLine.Update)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LookupDto>>>> Lookup(
        [FromQuery] LookupQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.LookupPlannableAsync(query, cancellationToken), HttpContext));

    /// <summary>Sipariş ayrıntısı: başlık, renk-beden kalemleri ve yapılabilecek durum geçişleri.</summary>
    [HttpGet("{id:int}")]
    [HasPermission(KonfeksiyonPermissions.ProductionOrder.View)]
    public async Task<ActionResult<ApiResponse<ProductionOrderDto>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetAsync(id, cancellationToken));

    /// <summary>Taslak sipariş oluşturur; sipariş numarası sistem tarafından verilir.</summary>
    [HttpPost]
    [HasPermission(KonfeksiyonPermissions.ProductionOrder.Create)]
    public async Task<ActionResult<ApiResponse<ProductionOrderDto>>> Create(
        ProductionOrderRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { id = result.Value.Id }, ApiResponse.Success(result.Value, HttpContext))
            : FromResult(result);
    }

    /// <summary>Taslak siparişin başlığını ve kalemlerini günceller (rowVersion zorunlu).</summary>
    [HttpPut("{id:int}")]
    [HasPermission(KonfeksiyonPermissions.ProductionOrder.Update)]
    public async Task<ActionResult<ApiResponse<ProductionOrderDto>>> Update(
        int id, ProductionOrderRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.UpdateAsync(id, request, cancellationToken));

    /// <summary>Taslağı onaylar ve üretime açar; içerik kilitlenir. Ayrı bir yetki ister (ProductionOrder.Approve).</summary>
    [HttpPost("{id:int}/release")]
    [HasPermission(KonfeksiyonPermissions.ProductionOrder.Approve)]
    public Task<ActionResult<ApiResponse<ProductionOrderDto>>> Release(
        int id, ProductionOrderTransitionRequest request, CancellationToken cancellationToken) =>
        TransitionAsync(id, ProductionOrderStatus.Released, request, cancellationToken);

    /// <summary>Onaylı siparişi düzenlenebilmesi için taslağa geri alır.</summary>
    [HttpPost("{id:int}/revert-to-draft")]
    [HasPermission(KonfeksiyonPermissions.ProductionOrder.Update)]
    public Task<ActionResult<ApiResponse<ProductionOrderDto>>> RevertToDraft(
        int id, ProductionOrderTransitionRequest request, CancellationToken cancellationToken) =>
        TransitionAsync(id, ProductionOrderStatus.Draft, request, cancellationToken);

    /// <summary>Üretimdeki siparişi tamamlandı olarak kapatır.</summary>
    [HttpPost("{id:int}/complete")]
    [HasPermission(KonfeksiyonPermissions.ProductionOrder.Complete)]
    public Task<ActionResult<ApiResponse<ProductionOrderDto>>> Complete(
        int id, ProductionOrderTransitionRequest request, CancellationToken cancellationToken) =>
        TransitionAsync(id, ProductionOrderStatus.Completed, request, cancellationToken);

    /// <summary>
    /// Demet operasyonlarının sırasını değiştirir (SEQUENTIAL: sıra no'ya göre, FREE: sırasız). İlk demet
    /// açıldıktan sonra değiştirilemez. Hata: ORDER_NOT_EDITABLE.
    /// </summary>
    [HttpPut("{id:int}/operation-sequence")]
    [HasPermission(KonfeksiyonPermissions.ProductionOrder.Update)]
    public async Task<ActionResult<ApiResponse<ProductionOrderDto>>> ChangeOperationSequence(
        int id, OperationSequenceRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.ChangeOperationSequenceAsync(id, request, cancellationToken));

    /// <summary>Siparişi iptal eder.</summary>
    [HttpPost("{id:int}/cancel")]
    [HasPermission(KonfeksiyonPermissions.ProductionOrder.Cancel)]
    public Task<ActionResult<ApiResponse<ProductionOrderDto>>> Cancel(
        int id, ProductionOrderTransitionRequest request, CancellationToken cancellationToken) =>
        TransitionAsync(id, ProductionOrderStatus.Cancelled, request, cancellationToken);

    /// <summary>Taslak siparişi siler (soft delete); numarası yeniden kullanılmaz.</summary>
    [HttpDelete("{id:int}")]
    [HasPermission(KonfeksiyonPermissions.ProductionOrder.Update)]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(int id, CancellationToken cancellationToken) =>
        FromResult(await service.DeleteAsync(id, cancellationToken));

    private async Task<ActionResult<ApiResponse<ProductionOrderDto>>> TransitionAsync(
        int id,
        ProductionOrderStatus target,
        ProductionOrderTransitionRequest request,
        CancellationToken cancellationToken) =>
        FromResult(await service.TransitionAsync(id, target, request.RowVersion, cancellationToken));
}
