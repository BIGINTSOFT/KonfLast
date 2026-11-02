using ERP.Core.Application.Common;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Infrastructure.Web.Authorization;
using ERP.Orme.Application.Features.Quotations;
using ERP.Orme.Domain.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Orme.Infrastructure.Api.Controllers;

/// <summary>Satış teklifleri ve ön maliyet. Taslak düzenlenir; gönderilen teklif kabul (taslak sipariş açılır) ya da ret edilir.</summary>
[Route("api/v1/orme-quotations")]
public sealed class OrmeQuotationsController(IOrmeQuotationService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(OrmePermissions.Quotation.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<OrmeQuotationListItemDto>>>> GetList(
        [FromQuery] OrmeQuotationQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetListAsync(query, cancellationToken), HttpContext));

    [HttpGet("{id:int}")]
    [HasPermission(OrmePermissions.Quotation.View)]
    public async Task<ActionResult<ApiResponse<OrmeQuotationDto>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetAsync(id, cancellationToken));

    /// <summary>Satırın varsayılan maliyet girdileri: kumaş kartı, maliyet parametreleri ve son alış fiyatları.</summary>
    [HttpGet("cost-defaults")]
    [HasPermission(OrmePermissions.Quotation.View)]
    public async Task<ActionResult<ApiResponse<OrmeQuotationCostDefaultsDto>>> GetCostDefaults(
        [FromQuery] OrmeQuotationCostDefaultsQuery query, CancellationToken cancellationToken) =>
        FromResult(await service.GetCostDefaultsAsync(query, cancellationToken));

    /// <summary>Müşteriye basılan teklif; müşterinin (ya da teklifte seçilen) belge dilinde.</summary>
    [HttpGet("{id:int}/print")]
    [HasPermission(OrmePermissions.Quotation.View)]
    public async Task<ActionResult<ApiResponse<OrmeQuotationPrintDto>>> GetPrint(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetPrintAsync(id, cancellationToken));

    [HttpPost]
    [HasPermission(OrmePermissions.Quotation.Manage)]
    public async Task<ActionResult<ApiResponse<OrmeQuotationDto>>> Create(OrmeQuotationRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, ApiResponse.Success(result.Value, HttpContext))
            : FromResult(result);
    }

    [HttpPut("{id:int}")]
    [HasPermission(OrmePermissions.Quotation.Manage)]
    public async Task<ActionResult<ApiResponse<OrmeQuotationDto>>> Update(int id, OrmeQuotationRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:int}")]
    [HasPermission(OrmePermissions.Quotation.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(int id, CancellationToken cancellationToken) =>
        FromResult(await service.DeleteAsync(id, cancellationToken));

    [HttpPost("{id:int}/lines")]
    [HasPermission(OrmePermissions.Quotation.Manage)]
    public async Task<ActionResult<ApiResponse<OrmeQuotationDto>>> AddLine(int id, OrmeQuotationLineRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.AddLineAsync(id, request, cancellationToken));

    [HttpPut("{id:int}/lines/{lineId:int}")]
    [HasPermission(OrmePermissions.Quotation.Manage)]
    public async Task<ActionResult<ApiResponse<OrmeQuotationDto>>> UpdateLine(int id, int lineId, OrmeQuotationLineRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.UpdateLineAsync(id, lineId, request, cancellationToken));

    [HttpDelete("{id:int}/lines/{lineId:int}")]
    [HasPermission(OrmePermissions.Quotation.Manage)]
    public async Task<ActionResult<ApiResponse<OrmeQuotationDto>>> DeleteLine(int id, int lineId, CancellationToken cancellationToken) =>
        FromResult(await service.DeleteLineAsync(id, lineId, cancellationToken));

    [HttpPost("{id:int}/send")]
    [HasPermission(OrmePermissions.Quotation.Manage)]
    public async Task<ActionResult<ApiResponse<OrmeQuotationDto>>> Send(int id, CancellationToken cancellationToken) =>
        FromResult(await service.SendAsync(id, cancellationToken));

    /// <summary>Gönderilmiş, reddedilmiş ya da süresi dolmuş teklifi düzeltmek için taslağa döndürür.</summary>
    [HttpPost("{id:int}/reopen")]
    [HasPermission(OrmePermissions.Quotation.Manage)]
    public async Task<ActionResult<ApiResponse<OrmeQuotationDto>>> Reopen(int id, CancellationToken cancellationToken) =>
        FromResult(await service.ReopenAsync(id, cancellationToken));

    [HttpPost("{id:int}/reject")]
    [HasPermission(OrmePermissions.Quotation.Manage)]
    public async Task<ActionResult<ApiResponse<OrmeQuotationDto>>> Reject(int id, CancellationToken cancellationToken) =>
        FromResult(await service.RejectAsync(id, cancellationToken));

    /// <summary>Kabul: satırlardan taslak örme siparişi açılır (sipariş yetkisi de gerekir).</summary>
    [HttpPost("{id:int}/accept")]
    [HasPermission(OrmePermissions.Quotation.Manage)]
    [HasPermission(OrmePermissions.Order.Manage)]
    public async Task<ActionResult<ApiResponse<OrmeQuotationDto>>> Accept(int id, OrmeQuotationAcceptRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.AcceptAsync(id, request, cancellationToken));
}

/// <summary>Ön maliyet parametreleri: makine ve işçilik saat maliyeti, genel gider, varsayılan fire ve kâr, makineye özel saat maliyeti.</summary>
[Route("api/v1/orme-cost-settings")]
public sealed class OrmeCostSettingsController(IOrmeCostSettingsService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(OrmePermissions.Quotation.View)]
    public async Task<ActionResult<ApiResponse<OrmeCostSettingsDto>>> Get(CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetAsync(cancellationToken), HttpContext));

    [HttpPut]
    [HasPermission(OrmePermissions.Quotation.Manage)]
    public async Task<ActionResult<ApiResponse<OrmeCostSettingsDto>>> Save(OrmeCostSettingsRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SaveAsync(request, cancellationToken));
}
