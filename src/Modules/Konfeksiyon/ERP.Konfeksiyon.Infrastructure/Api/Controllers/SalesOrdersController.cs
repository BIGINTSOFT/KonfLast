using ERP.Konfeksiyon.Domain.Authorization;
using ERP.Core.Infrastructure.Web.Authorization;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Application.Common;
using ERP.Konfeksiyon.Application.Features.Sales;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Konfeksiyon.Infrastructure.Api.Controllers;

/// <summary>
/// Müşteri siparişleri: taslak → onaya gönder → onayla / reddet → üretime aktar → kapat. Onay ve ret ayrı bir
/// yetkiyle (SalesOrder.Approve), üretime aktarma üretim siparişi açma yetkisiyle yapılır.
/// </summary>
[Route("api/v1/sales-orders")]
public sealed class SalesOrdersController(ISalesOrderService service, ISalesOrderReservationService reservations, ISalesOrderProductionChangesService productionChanges) : ApiControllerBase
{
    /// <summary>Sipariş listesi, en yeniden eskiye (sayfalı; arama, durum ve müşteri filtresi).</summary>
    [HttpGet]
    [HasPermission(KonfeksiyonPermissions.SalesOrder.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<SalesOrderListItemDto>>>> GetList(
        [FromQuery] SalesOrderListQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetListAsync(query, cancellationToken), HttpContext));

    /// <summary>Sipariş ayrıntısı: kalemler, onay bilgisi, açılmış üretim siparişleri ve yapılabilecek geçişler.</summary>
    [HttpGet("{id:int}")]
    [HasPermission(KonfeksiyonPermissions.SalesOrder.View)]
    public async Task<ActionResult<ApiResponse<SalesOrderDto>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetAsync(id, cancellationToken));

    /// <summary>
    /// Sipariş girişinde müşteri seçilince forma gelen bilgiler: varsayılan para birimi, teslim adresleri, aktif
    /// buyer'lar ve son siparişin bedenleri.
    /// </summary>
    [HttpGet("customer-defaults/{customerId:int}")]
    [HasPermission(KonfeksiyonPermissions.SalesOrder.Manage)]
    public async Task<ActionResult<ApiResponse<SalesOrderCustomerDefaultsDto>>> GetCustomerDefaults(
        int customerId, CancellationToken cancellationToken) =>
        FromResult(await service.GetCustomerDefaultsAsync(customerId, cancellationToken));

    /// <summary>Proforma (müşteriye giden sipariş teyidi) için sipariş, satıcı ve müşteri bilgileri.</summary>
    [HttpGet("{id:int}/proforma")]
    [HasPermission(KonfeksiyonPermissions.SalesOrder.View)]
    public async Task<ActionResult<ApiResponse<SalesOrderProformaDto>>> GetProforma(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetProformaAsync(id, cancellationToken));

    /// <summary>Taslak sipariş oluşturur; sipariş numarası sistem tarafından verilir.</summary>
    [HttpPost]
    [HasPermission(KonfeksiyonPermissions.SalesOrder.Manage)]
    public async Task<ActionResult<ApiResponse<SalesOrderDto>>> Create(SalesOrderRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { id = result.Value.Id }, ApiResponse.Success(result.Value, HttpContext))
            : FromResult(result);
    }

    /// <summary>Taslak siparişin başlığını ve kalemlerini günceller (rowVersion zorunlu).</summary>
    [HttpPut("{id:int}")]
    [HasPermission(KonfeksiyonPermissions.SalesOrder.Manage)]
    public async Task<ActionResult<ApiResponse<SalesOrderDto>>> Update(
        int id, SalesOrderRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.UpdateAsync(id, request, cancellationToken));

    /// <summary>Taslak siparişi siler (soft delete); numarası yeniden kullanılmaz.</summary>
    [HttpDelete("{id:int}")]
    [HasPermission(KonfeksiyonPermissions.SalesOrder.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(int id, CancellationToken cancellationToken) =>
        FromResult(await service.DeleteAsync(id, cancellationToken));

    /// <summary>Taslağı onaya gönderir; içerik kilitlenir.</summary>
    [HttpPost("{id:int}/submit")]
    [HasPermission(KonfeksiyonPermissions.SalesOrder.Manage)]
    public async Task<ActionResult<ApiResponse<SalesOrderDto>>> Submit(
        int id, SalesOrderActionRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SubmitAsync(id, request, cancellationToken));

    /// <summary>
    /// Kararı bekleyen onay adımını onaylar; son adımsa sipariş onaylanır. Onay akışı tanımlıysa karar yetkisi adımın
    /// rolüne / kişilerine göre, akışsız gönderimde müşteri siparişi onaylama yetkisine göre denetlenir (NOT_YOUR_APPROVAL).
    /// </summary>
    [HttpPost("{id:int}/approve")]
    [HasPermission(KonfeksiyonPermissions.SalesOrder.View)]
    public async Task<ActionResult<ApiResponse<SalesOrderDto>>> Approve(
        int id, SalesOrderActionRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.ApproveAsync(id, request, cancellationToken));

    /// <summary>Onay bekleyen siparişi, düzeltilmek üzere (notuyla) taslağa geri çevirir.</summary>
    [HttpPost("{id:int}/reject")]
    [HasPermission(KonfeksiyonPermissions.SalesOrder.View)]
    public async Task<ActionResult<ApiResponse<SalesOrderDto>>> Reject(
        int id, SalesOrderActionRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.RejectAsync(id, request, cancellationToken));

    /// <summary>
    /// Onaylı siparişi revizyon için taslağa alır (müşteri değişikliği); düzeltilip yeniden onaya gönderilir.
    /// Geçerli üretim siparişi ya da sevkiyatı varsa reddedilir (SALES_ORDER_HAS_PRODUCTION / SALES_ORDER_HAS_SHIPMENTS).
    /// </summary>
    [HttpPost("{id:int}/revise")]
    [HasPermission(KonfeksiyonPermissions.SalesOrder.Approve)]
    public async Task<ActionResult<ApiResponse<SalesOrderDto>>> Revise(
        int id, SalesOrderActionRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.ReviseAsync(id, request, cancellationToken));

    /// <summary>Siparişi iptal eder. Açılmış üretim siparişi varsa önce onlar iptal edilmelidir (SALES_ORDER_HAS_PRODUCTION).</summary>
    [HttpPost("{id:int}/cancel")]
    [HasPermission(KonfeksiyonPermissions.SalesOrder.Cancel)]
    public async Task<ActionResult<ApiResponse<SalesOrderDto>>> Cancel(
        int id, SalesOrderActionRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.CancelAsync(id, request, cancellationToken));

    /// <summary>
    /// Onaylı siparişi elle kapatır (kalan adet sevk edilmeyecekse). Tamamı sevk edilen sipariş kendiliğinden kapanır;
    /// bu işlem yalnızca SalesOrder.Close yetkisiyle yapılır. Üretimi süren üretim siparişi varken
    /// (SALES_ORDER_HAS_PRODUCTION) ya da hiç tamamlanmış üretimi ve sevkiyatı yokken (SALES_ORDER_NOTHING_DELIVERED) kapatılamaz.
    /// </summary>
    [HttpPost("{id:int}/close")]
    [HasPermission(KonfeksiyonPermissions.SalesOrder.Close)]
    public async Task<ActionResult<ApiResponse<SalesOrderDto>>> Close(
        int id, SalesOrderActionRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.CloseAsync(id, request, cancellationToken));

    /// <summary>
    /// Kalemlerin karşılanma durumu: sevk edilen, depoda ayrılan hazır mamul, üretimdeki ve planlamayı bekleyen adet,
    /// serbest stok.
    /// </summary>
    [HttpGet("{id:int}/reservations")]
    [HasPermission(KonfeksiyonPermissions.SalesOrder.View)]
    public async Task<ActionResult<ApiResponse<SalesOrderReservationsDto>>> GetReservations(int id, CancellationToken cancellationToken) =>
        FromResult(await reservations.GetAsync(id, cancellationToken));

    /// <summary>
    /// Onaylı siparişin stok ayırmasını elle değiştirir (kalem başına adet). Stok yetmezse kullanılabilir kadar ayrılır;
    /// ayrılmayan kısım planlamaya düşer.
    /// </summary>
    [HttpPut("{id:int}/reservations")]
    [HasPermission(KonfeksiyonPermissions.Stock.Reserve)]
    public async Task<ActionResult<ApiResponse<SalesOrderReservationsDto>>> SetReservations(
        int id, SetSalesOrderReservationsRequest request, CancellationToken cancellationToken) =>
        FromResult(await reservations.SetAsync(id, request, cancellationToken));

    /// <summary>
    /// Siparişten açılmış üretim siparişlerinin renk / beden bazında sipariş adedinden farkı ve üretim kalemlerinde kimin,
    /// ne zaman, neyi değiştirdiği.
    /// </summary>
    [HttpGet("{id:int}/production-changes")]
    [HasPermission(KonfeksiyonPermissions.SalesOrder.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ProductionOrderDifferenceDto>>>> GetProductionChanges(
        int id, CancellationToken cancellationToken) =>
        FromResult(await productionChanges.GetAsync(id, cancellationToken));
}
