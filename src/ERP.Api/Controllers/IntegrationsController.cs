using ERP.Core.Infrastructure.Web.Authorization;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.Integrations;
using ERP.Core.Domain.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Api.Controllers;

[Route("api/v1/integrations")]
public sealed class IntegrationsController(IIntegrationService service) : ApiControllerBase
{
    /// <summary>Tanımlı kanallar (webhook vb.), kanal başına teslim sayıları ve yayınlanan olay türleri.</summary>
    [HttpGet("overview")]
    [HasPermission(Permissions.Integration.View)]
    public async Task<ActionResult<ApiResponse<IntegrationOverviewDto>>> GetOverview(CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetOverviewAsync(cancellationToken), HttpContext));

    /// <summary>
    /// Dış sistemlere duyurulan iş olayları (sayfalı; numara, olay türü, kanal ve teslim durumu filtresi).
    /// <c>afterId</c> verilirse o sıra numarasından sonraki olaylar eskiden yeniye döner: webhook kullanmayan
    /// bir dış sistem, olayları bu uçtan kaldığı yerden çekebilir.
    /// </summary>
    [HttpGet("messages")]
    [HasPermission(Permissions.Integration.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<IntegrationMessageListItemDto>>>> GetMessages(
        [FromQuery] IntegrationMessageQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetMessagesAsync(query, cancellationToken), HttpContext));

    /// <summary>Olayın verisi ve kanallara teslim durumu (deneme sayısı, son hata, sıradaki deneme).</summary>
    [HttpGet("messages/{id:long}")]
    [HasPermission(Permissions.Integration.View)]
    public async Task<ActionResult<ApiResponse<IntegrationMessageDto>>> GetMessage(long id, CancellationToken cancellationToken) =>
        FromResult(await service.GetMessageAsync(id, cancellationToken));

    /// <summary>
    /// Bitmiş bir teslimi (deneme hakkı tükenmiş ya da başarılı) yeniden kuyruğa alır; olay aynı kimlikle
    /// yeniden gönderilir. Zaten kuyruktaki teslim için INTEGRATION_DELIVERY_PENDING döner.
    /// </summary>
    [HttpPost("deliveries/{id:long}/retry")]
    [HasPermission(Permissions.Integration.Manage)]
    public async Task<ActionResult<ApiResponse<IntegrationMessageDto>>> RetryDelivery(long id, CancellationToken cancellationToken) =>
        FromResult(await service.RetryDeliveryAsync(id, cancellationToken));
}
