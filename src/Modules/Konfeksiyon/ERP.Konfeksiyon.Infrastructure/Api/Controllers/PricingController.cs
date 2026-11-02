using ERP.Core.Infrastructure.Web.Authorization;
using ERP.Core.Infrastructure.Web;
using ERP.Konfeksiyon.Application.Features.Pricing;
using Microsoft.AspNetCore.Mvc;
using ERP.Konfeksiyon.Domain.Authorization;

namespace ERP.Konfeksiyon.Infrastructure.Api.Controllers;

/// <summary>Sipariş girişinde önerilen fiyatlar (fiyat listesi, kur çevrimi ve müşteri iskontosu uygulanmış).</summary>
[Route("api/v1/pricing")]
public sealed class PricingController(IPricingService service) : ApiControllerBase
{
    [HttpPost("quote")]
    [HasPermission(KonfeksiyonPermissions.SalesOrder.Manage)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PriceQuoteDto>>>> Quote(
        PriceQuoteRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.QuoteAsync(request, cancellationToken), HttpContext));
}
