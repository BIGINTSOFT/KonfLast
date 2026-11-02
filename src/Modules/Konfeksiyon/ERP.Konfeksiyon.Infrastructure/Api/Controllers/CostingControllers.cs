using ERP.Konfeksiyon.Domain.Authorization;
using ERP.Core.Infrastructure.Web.Authorization;
using ERP.Core.Infrastructure.Web;
using ERP.Konfeksiyon.Application.Features.Costing;
using ERP.Core.Domain.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Konfeksiyon.Infrastructure.Api.Controllers;

/// <summary>Ürün reçetesi: bir parça için gereken malzemeler.</summary>
[Route("api/v1/products/{productId:int}/materials")]
public sealed class ProductMaterialsController(IProductMaterialService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.MasterData.View)]
    public async Task<ActionResult<ApiResponse<ProductMaterialsDto>>> Get(int productId, CancellationToken cancellationToken) =>
        FromResult(await service.GetAsync(productId, cancellationToken));

    /// <summary>
    /// Reçeteyi verilen satırlarla değiştirir. Renk / bedeni belirtilen satır yalnızca o parçalar için geçerlidir ve
    /// aynı malzemenin genel satırının önüne geçer. Hatalar: INVALID_REFERENCE, MATERIAL_NOT_ALLOWED.
    /// </summary>
    [HttpPut]
    [HasPermission(Permissions.MasterData.Manage)]
    public async Task<ActionResult<ApiResponse<ProductMaterialsDto>>> Save(
        int productId, SaveProductMaterialsRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SaveAsync(productId, request, cancellationToken));
}

/// <summary>Reçete formunun malzeme listesi: mamul olmayan stok kartları (kod, ad, birim, birim maliyet).</summary>
[Route("api/v1/product-material-options")]
public sealed class ProductMaterialOptionsController(IProductMaterialService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.MasterData.Manage)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MaterialOptionDto>>>> Get(CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetOptionsAsync(cancellationToken), HttpContext));
}

/// <summary>Ürünün parça başı standart maliyeti (etkin rota ve reçeteden).</summary>
[Route("api/v1/products/{productId:int}/cost-summary")]
public sealed class ProductCostController(IProductCostService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(KonfeksiyonPermissions.Costing.View)]
    public async Task<ActionResult<ApiResponse<ProductCostSummaryDto>>> Get(int productId, CancellationToken cancellationToken) =>
        FromResult(await service.GetSummaryAsync(productId, cancellationToken));
}

[Route("api/v1/production-orders/{orderId:int}")]
public sealed class OrderCostingController(ICostingService service) : ApiControllerBase
{
    /// <summary>
    /// Siparişin reçeteye göre malzeme ihtiyacı: toplam ihtiyaç, demetler için düşülen, kalan ihtiyaç, depodaki
    /// stok ve eksik. Depo verilmezse tek hammadde deposu kullanılır.
    /// </summary>
    [HttpGet("materials")]
    [HasPermission(KonfeksiyonPermissions.ProductionOrder.View)]
    public async Task<ActionResult<ApiResponse<OrderMaterialsDto>>> GetMaterials(
        int orderId, [FromQuery] int? warehouseId, CancellationToken cancellationToken) =>
        FromResult(await service.GetOrderMaterialsAsync(orderId, warehouseId, cancellationToken));

    /// <summary>
    /// Siparişin kalan malzeme ihtiyacını depoda ayırır (stok yetmeyen kalemde yettiği kadar); yeniden çağrılırsa
    /// rezervasyon güncel ihtiyaca göre yenilenir. Ayrılan stok başka siparişlerce ve elle çıkışla kullanılamaz.
    /// </summary>
    [HttpPost("materials/reserve")]
    [HasPermission(KonfeksiyonPermissions.Stock.Reserve)]
    public async Task<ActionResult<ApiResponse<OrderMaterialsDto>>> ReserveMaterials(
        int orderId, [FromQuery] int? warehouseId, CancellationToken cancellationToken) =>
        FromResult(await service.ReserveMaterialsAsync(orderId, warehouseId, cancellationToken));

    /// <summary>Siparişin tüm malzeme rezervasyonlarını bırakır.</summary>
    [HttpPost("materials/release")]
    [HasPermission(KonfeksiyonPermissions.Stock.Reserve)]
    public async Task<ActionResult<ApiResponse<OrderMaterialsDto>>> ReleaseMaterials(
        int orderId, [FromQuery] int? warehouseId, CancellationToken cancellationToken) =>
        FromResult(await service.ReleaseReservationsAsync(orderId, warehouseId, cancellationToken));

    /// <summary>
    /// Toplama listesi: kalan ihtiyaç için hangi adresten, hangi toptan ne kadar alınacağı (demet açılırken yapılan
    /// otomatik çıkışla aynı sırada).
    /// </summary>
    [HttpGet("pick-list")]
    [HasPermission(KonfeksiyonPermissions.ProductionOrder.View)]
    public async Task<ActionResult<ApiResponse<PickListDto>>> GetPickList(
        int orderId, [FromQuery] int? warehouseId, CancellationToken cancellationToken) =>
        FromResult(await service.GetPickListAsync(orderId, warehouseId, cancellationToken));

    /// <summary>Siparişin gerçekleşen malzeme ve işçilik maliyeti ile standart birim maliyeti.</summary>
    [HttpGet("cost")]
    [HasPermission(KonfeksiyonPermissions.Costing.View)]
    public async Task<ActionResult<ApiResponse<OrderCostDto>>> GetCost(int orderId, CancellationToken cancellationToken) =>
        FromResult(await service.GetOrderCostAsync(orderId, cancellationToken));
}
