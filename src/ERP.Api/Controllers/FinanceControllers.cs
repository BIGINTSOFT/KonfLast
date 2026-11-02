using ERP.Core.Infrastructure.Web.Authorization;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Application.Features.Finance;
using ERP.Core.Application.Features.MasterData;
using ERP.Core.Domain.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Api.Controllers;

/// <summary>Para birimleri (ISO 4217). Yerel para birimi (TRY) silinemez ve pasif yapılamaz.</summary>
[Route("api/v1/currencies")]
[ResourcePermissions(Permissions.Pricing.View, Permissions.Pricing.Manage)]
public sealed class CurrenciesController(IMasterDataService<CurrencyDto, CurrencyRequest> service)
    : MasterDataController<CurrencyDto, CurrencyRequest>(service);

/// <summary>
/// Döviz kurları (1 birim döviz = X TRY). TCMB'den çekilir ya da elle girilir; bir gün için kur yoksa önceki son
/// kur kullanılır.
/// </summary>
[Route("api/v1/exchange-rates")]
public sealed class ExchangeRatesController(IExchangeRateService service, TimeProvider timeProvider) : ApiControllerBase
{
    /// <summary>Aktif dövizlerin verilen gündeki (boşsa bugünkü) kurları.</summary>
    [HttpGet]
    [HasPermission(Permissions.Pricing.View)]
    public async Task<ActionResult<ApiResponse<ExchangeRateDayDto>>> GetDay([FromQuery] DateOnly? date, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(
            await service.GetDayAsync(date ?? DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime), cancellationToken),
            HttpContext));

    /// <summary>Bir dövizin tarih aralığındaki kurları (en fazla bir yıl), yeniden eskiye.</summary>
    [HttpGet("history")]
    [HasPermission(Permissions.Pricing.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ExchangeRateHistoryItemDto>>>> GetHistory(
        [FromQuery] int currencyId, [FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken cancellationToken) =>
        FromResult(await service.GetHistoryAsync(currencyId, from, to, cancellationToken));

    /// <summary>Bir günün kurlarını elle girer ya da düzeltir; değişen kurlar "elle" olarak işaretlenir.</summary>
    [HttpPut]
    [HasPermission(Permissions.ExchangeRate.Manage)]
    public async Task<ActionResult<ApiResponse<ExchangeRateDayDto>>> Save(
        SaveExchangeRatesRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SaveAsync(request, cancellationToken));

    /// <summary>
    /// TCMB'den bir günün (boşsa en son) bültenini çeker. Elle girilmiş kurların üzerine, istenmedikçe yazılmaz.
    /// O gün bülten yoksa (hafta sonu, tatil) hiçbir kur eklenmez.
    /// </summary>
    [HttpPost("fetch")]
    [HasPermission(Permissions.ExchangeRate.Manage)]
    public async Task<ActionResult<ApiResponse<FetchExchangeRatesResultDto>>> Fetch(
        FetchExchangeRatesRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.FetchAsync(request, cancellationToken));

    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.ExchangeRate.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(int id, CancellationToken cancellationToken) =>
        FromResult(await service.DeleteAsync(id, cancellationToken));
}
