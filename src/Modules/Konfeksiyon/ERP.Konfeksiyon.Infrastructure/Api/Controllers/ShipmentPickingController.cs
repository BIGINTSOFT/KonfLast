using ERP.Konfeksiyon.Domain.Authorization;
using ERP.Core.Infrastructure.Web.Authorization;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Application.Common;
using ERP.Konfeksiyon.Application.Features.Shipping;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Konfeksiyon.Infrastructure.Api.Controllers;

/// <summary>
/// Sevkiyat hazırlığı: taslağı toplamaya verme, depoda (el terminalinde) adres sırasıyla toplama ve toplamayı bitirme.
/// Toplama stok hareketi yazmaz; stok, irsaliye kesilince düşer.
/// </summary>
[Route("api/v1/shipments")]
public sealed class ShipmentPickingController(IShipmentPickingService service) : ApiControllerBase
{
    /// <summary>Toplama kuyruğu: toplamadaki (ya da durum verilirse o durumdaki) sevkiyatlar, en eski sevk günü önce.</summary>
    [HttpGet("picking-queue")]
    [HasPermission(KonfeksiyonPermissions.Shipment.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PickingQueueItemDto>>>> GetQueue(
        [FromQuery] PickingQueueQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetQueueAsync(query, cancellationToken), HttpContext));

    /// <summary>Toplama listesi: kalemler, adres sırasıyla toplama satırları ve yapılan toplamalar.</summary>
    [HttpGet("{id:int}/picking")]
    [HasPermission(KonfeksiyonPermissions.Shipment.View)]
    public async Task<ActionResult<ApiResponse<ShipmentPickingDto>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetAsync(id, cancellationToken));

    /// <summary>Taslak sevkiyatı depoya toplamaya verir (kalemler kilitlenir).</summary>
    [HttpPost("{id:int}/send-to-picking")]
    [HasPermission(KonfeksiyonPermissions.Shipment.Manage)]
    public async Task<ActionResult<ApiResponse<ShipmentPickingDto>>> SendToPicking(
        int id, ShipmentTransitionRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SendToPickingAsync(id, request.RowVersion, cancellationToken));

    /// <summary>Hiçbir şey toplanmamış sevkiyatı taslağa döndürür.</summary>
    [HttpPost("{id:int}/return-to-draft")]
    [HasPermission(KonfeksiyonPermissions.Shipment.Manage)]
    public async Task<ActionResult<ApiResponse<ShipmentPickingDto>>> ReturnToDraft(
        int id, ShipmentTransitionRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.ReturnToDraftAsync(id, request.RowVersion, cancellationToken));

    /// <summary>Toplanmış sevkiyatı yeniden toplamaya açar (kontrolde fark çıktığında).</summary>
    [HttpPost("{id:int}/reopen-picking")]
    [HasPermission(KonfeksiyonPermissions.Shipment.Manage)]
    public async Task<ActionResult<ApiResponse<ShipmentPickingDto>>> Reopen(
        int id, ShipmentTransitionRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.ReopenAsync(id, request.RowVersion, cancellationToken));

    /// <summary>
    /// Toplamayı üstlenir. Başka bir kullanıcı topluyorsa PICKING_CLAIMED döner; takeOver ile devralınır.
    /// </summary>
    [HttpPost("{id:int}/picking/start")]
    [HasPermission(KonfeksiyonPermissions.Shipment.Manage)]
    public async Task<ActionResult<ApiResponse<ShipmentPickingDto>>> Start(
        int id, StartPickingRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.StartAsync(id, request, cancellationToken));

    /// <summary>
    /// Bir adresten alınan adedi kaydeder. Adreste o kadar mal olmalı ve kalemin adedi aşılmamalıdır; aynı istek kimliği
    /// yeniden gelirse ikinci kez yazılmaz.
    /// </summary>
    [HttpPost("{id:int}/picks")]
    [HasPermission(KonfeksiyonPermissions.Shipment.Manage)]
    public async Task<ActionResult<ApiResponse<ShipmentPickingDto>>> Pick(
        int id, PickRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.PickAsync(id, request, cancellationToken));

    /// <summary>Bir toplama kaydını geri alır (toplama sürerken).</summary>
    [HttpDelete("{id:int}/picks/{pickId:long}")]
    [HasPermission(KonfeksiyonPermissions.Shipment.Manage)]
    public async Task<ActionResult<ApiResponse<ShipmentPickingDto>>> UndoPick(int id, long pickId, CancellationToken cancellationToken) =>
        FromResult(await service.UndoPickAsync(id, pickId, cancellationToken));

    /// <summary>
    /// Toplamayı bitirir: eksik toplanan kalem toplanan adede iner, hiç toplanmayan kalem düşer; sevkiyat irsaliye bekler.
    /// </summary>
    [HttpPost("{id:int}/picking/complete")]
    [HasPermission(KonfeksiyonPermissions.Shipment.Manage)]
    public async Task<ActionResult<ApiResponse<ShipmentPickingDto>>> Complete(int id, CancellationToken cancellationToken) =>
        FromResult(await service.CompleteAsync(id, cancellationToken));
}

/// <summary>Sevke hazır müşteri siparişleri: depoda ayrılmış mal ve sevkiyata aktarma.</summary>
[Route("api/v1/shipping/ready-orders")]
public sealed class ReadyOrdersController(IShipmentPickingService service) : ApiControllerBase
{
    /// <summary>Ayrılmış ve henüz bir sevkiyata aktarılmamış malı olan onaylı siparişler, en yakın termin önce.</summary>
    [HttpGet]
    [HasPermission(KonfeksiyonPermissions.Shipment.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ReadyOrderDto>>>> GetList(
        [FromQuery] string? search, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetReadyOrdersAsync(search, cancellationToken), HttpContext));

    /// <summary>Siparişin depo + mamul bazında ayrılan, açık sevkiyattaki ve aktarılabilecek adetleri.</summary>
    [HttpGet("{salesOrderId:int}")]
    [HasPermission(KonfeksiyonPermissions.Shipment.View)]
    public async Task<ActionResult<ApiResponse<ReadyOrderDetailDto>>> Get(int salesOrderId, CancellationToken cancellationToken) =>
        FromResult(await service.GetReadyOrderAsync(salesOrderId, cancellationToken));

    /// <summary>Siparişin bir depodaki ayrılmış malını yeni sevkiyata aktarır ve toplamaya verir; sevkiyat kimliğini döndürür.</summary>
    [HttpPost("{salesOrderId:int}/transfer")]
    [HasPermission(KonfeksiyonPermissions.Shipment.Manage)]
    public async Task<ActionResult<ApiResponse<int>>> Transfer(
        int salesOrderId, ReadyOrderTransferRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.TransferAsync(salesOrderId, request, cancellationToken));
}
