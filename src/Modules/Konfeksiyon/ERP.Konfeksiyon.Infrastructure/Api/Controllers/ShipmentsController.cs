using ERP.Konfeksiyon.Domain.Authorization;
using ERP.Core.Infrastructure.Web.Authorization;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Konfeksiyon.Application.Features.Shipping;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Konfeksiyon.Infrastructure.Api.Controllers;

[Route("api/v1/shipments")]
public sealed class ShipmentsController(IShipmentService service) : ApiControllerBase
{
    /// <summary>Sevkiyatlar, en yeniden eskiye (sayfalı; arama, durum, müşteri ve üretim siparişi filtresi).</summary>
    [HttpGet]
    [HasPermission(KonfeksiyonPermissions.Shipment.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<ShipmentListItemDto>>>> GetList(
        [FromQuery] ShipmentListQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetListAsync(query, cancellationToken), HttpContext));

    /// <summary>Sevkiyata bağlanabilecek üretim siparişleri (taslak / iptal hariç); seçim kutuları için.</summary>
    [HttpGet("orders/lookup")]
    [HasPermission(KonfeksiyonPermissions.Shipment.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LookupDto>>>> LookupOrders(
        [FromQuery] LookupQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.LookupOrdersAsync(query, cancellationToken), HttpContext));

    /// <summary>Sevkiyata bağlanabilecek onaylı müşteri siparişleri; seçim kutuları için.</summary>
    [HttpGet("sales-orders/lookup")]
    [HasPermission(KonfeksiyonPermissions.Shipment.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LookupDto>>>> LookupSalesOrders(
        [FromQuery] LookupQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.LookupSalesOrdersAsync(query, cancellationToken), HttpContext));

    /// <summary>Sevkiyat ayrıntısı: kalemler ve her kalemin depodaki şu anki stoğu.</summary>
    [HttpGet("{id:int}")]
    [HasPermission(KonfeksiyonPermissions.Shipment.View)]
    public async Task<ActionResult<ApiResponse<ShipmentDto>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetAsync(id, cancellationToken));

    /// <summary>
    /// Taslak sevkiyat oluşturur; numara sistem tarafından verilir. Yalnızca mamul kartları sevk edilebilir;
    /// üretim siparişi verilirse müşteri ve ürün siparişle uyuşmalıdır. Taslak stoğu etkilemez.
    /// </summary>
    [HttpPost]
    [HasPermission(KonfeksiyonPermissions.Shipment.Manage)]
    public async Task<ActionResult<ApiResponse<ShipmentDto>>> Create(ShipmentRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { id = result.Value.Id }, ApiResponse.Success(result.Value, HttpContext))
            : FromResult(result);
    }

    /// <summary>Taslak sevkiyatın başlığını ve kalemlerini günceller (rowVersion zorunlu).</summary>
    [HttpPut("{id:int}")]
    [HasPermission(KonfeksiyonPermissions.Shipment.Manage)]
    public async Task<ActionResult<ApiResponse<ShipmentDto>>> Update(
        int id, ShipmentRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.UpdateAsync(id, request, cancellationToken));

    /// <summary>
    /// İrsaliye keser: toplanmış sevkiyatın her kalemi için son kontrolde sayılan adet gönderilir; toplananla birebir
    /// tutmazsa (DESPATCH_CHECK_MISMATCH) hiçbir şey değişmez. Tutarsa irsaliye numarası (boşsa sistem verir) ve ETTN
    /// atanır, kalemler toplandıkları adreslerden düşer ve sevkiyat kesinleşir; geri alınamaz.
    /// </summary>
    [HttpPost("{id:int}/dispatch")]
    [HasPermission(KonfeksiyonPermissions.Shipment.Ship)]
    public async Task<ActionResult<ApiResponse<ShipmentDto>>> Dispatch(
        int id, ShipmentDispatchRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.DispatchAsync(id, request, cancellationToken));

    /// <summary>Stok düşmemiş (taslak, toplamada, toplanmış) sevkiyatı iptal eder.</summary>
    [HttpPost("{id:int}/cancel")]
    [HasPermission(KonfeksiyonPermissions.Shipment.Manage)]
    public async Task<ActionResult<ApiResponse<ShipmentDto>>> Cancel(
        int id, ShipmentTransitionRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.CancelAsync(id, request.RowVersion, cancellationToken));

    /// <summary>Taslak sevkiyatı siler (soft delete); numarası yeniden kullanılmaz.</summary>
    [HttpDelete("{id:int}")]
    [HasPermission(KonfeksiyonPermissions.Shipment.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(int id, CancellationToken cancellationToken) =>
        FromResult(await service.DeleteAsync(id, cancellationToken));
}
