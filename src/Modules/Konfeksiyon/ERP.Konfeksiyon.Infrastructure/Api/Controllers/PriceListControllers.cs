using ERP.Konfeksiyon.Application.Features.Pricing;
using ERP.Core.Infrastructure.Web.Authorization;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Application.Common;
using ERP.Core.Domain.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Konfeksiyon.Infrastructure.Api.Controllers;

/// <summary>
/// Satış fiyat listeleri. Müşteriye özel liste genel listeden önceliklidir; kalem renk ve / veya bedene özel
/// fiyat taşıyabilir.
/// </summary>
[Route("api/v1/price-lists")]
public sealed class PriceListsController(IPriceListService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.Pricing.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<PriceListListItemDto>>>> GetList(
        [FromQuery] PriceListQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetListAsync(query, cancellationToken), HttpContext));

    [HttpGet("{id:int}")]
    [HasPermission(Permissions.Pricing.View)]
    public async Task<ActionResult<ApiResponse<PriceListDto>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetAsync(id, cancellationToken));

    [HttpPost]
    [HasPermission(Permissions.Pricing.Manage)]
    public async Task<ActionResult<ApiResponse<PriceListDto>>> Create(PriceListRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { id = result.Value.Id }, ApiResponse.Success(result.Value, HttpContext))
            : FromResult(result);
    }

    /// <summary>Başlığı ve kalemleri günceller; gönderilmeyen kalemler silinir.</summary>
    [HttpPut("{id:int}")]
    [HasPermission(Permissions.Pricing.Manage)]
    public async Task<ActionResult<ApiResponse<PriceListDto>>> Update(
        int id, PriceListRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.Pricing.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(int id, CancellationToken cancellationToken) =>
        FromResult(await service.DeleteAsync(id, cancellationToken));
}

/// <summary>Müşteri iskontoları: ürün boşsa genel iskonto, doluysa ürüne özel iskonto.</summary>
[Route("api/v1/customer-discounts")]
public sealed class CustomerDiscountsController(ICustomerDiscountService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.Pricing.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<CustomerDiscountDto>>>> GetList(
        [FromQuery] CustomerDiscountQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetListAsync(query, cancellationToken), HttpContext));

    [HttpGet("{id:int}")]
    [HasPermission(Permissions.Pricing.View)]
    public async Task<ActionResult<ApiResponse<CustomerDiscountDto>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetAsync(id, cancellationToken));

    [HttpPost]
    [HasPermission(Permissions.Pricing.Manage)]
    public async Task<ActionResult<ApiResponse<CustomerDiscountDto>>> Create(
        CustomerDiscountRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { id = result.Value.Id }, ApiResponse.Success(result.Value, HttpContext))
            : FromResult(result);
    }

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.Pricing.Manage)]
    public async Task<ActionResult<ApiResponse<CustomerDiscountDto>>> Update(
        int id, CustomerDiscountRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.Pricing.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(int id, CancellationToken cancellationToken) =>
        FromResult(await service.DeleteAsync(id, cancellationToken));
}
