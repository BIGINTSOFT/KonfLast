using ERP.Core.Application.Common;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Infrastructure.Web.Authorization;
using ERP.Dokuma.Application.Features.Planning;
using ERP.Dokuma.Application.Features.Quotations;
using ERP.Dokuma.Domain.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Dokuma.Infrastructure.Api.Controllers;

/// <summary>Teklifler: ön maliyet dökümüyle metre fiyatı; taslak → gönderildi → kabul (taslak sipariş açılır) / ret.</summary>
[Route("api/v1/dokuma-quotations")]
public sealed class DokumaQuotationsController(IDokumaQuotationService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(DokumaPermissions.Quotation.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<DokumaQuotationListItemDto>>>> GetList(
        [FromQuery] DokumaQuotationQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetListAsync(query, cancellationToken), HttpContext));

    [HttpGet("{id:int}")]
    [HasPermission(DokumaPermissions.Quotation.View)]
    public async Task<ActionResult<ApiResponse<DokumaQuotationDto>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetAsync(id, cancellationToken));

    /// <summary>Müşteriye basılan teklif, belge dilinde; maliyet dökümü içermez.</summary>
    [HttpGet("{id:int}/print")]
    [HasPermission(DokumaPermissions.Quotation.View)]
    public async Task<ActionResult<ApiResponse<DokumaQuotationPrintDto>>> GetPrint(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetPrintAsync(id, cancellationToken));

    /// <summary>Kaydetmeden ön maliyet hesabı: formdaki değerlerle satır dökümü.</summary>
    [HttpPost("calculate")]
    [HasPermission(DokumaPermissions.Quotation.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaQuotationCalculationDto>>> Calculate(DokumaQuotationRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.CalculateAsync(request, cancellationToken));

    [HttpPost]
    [HasPermission(DokumaPermissions.Quotation.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaQuotationDto>>> Create(DokumaQuotationRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, ApiResponse.Success(result.Value, HttpContext))
            : FromResult(result);
    }

    [HttpPut("{id:int}")]
    [HasPermission(DokumaPermissions.Quotation.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaQuotationDto>>> Update(int id, DokumaQuotationRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:int}")]
    [HasPermission(DokumaPermissions.Quotation.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(int id, CancellationToken cancellationToken) =>
        FromResult(await service.DeleteAsync(id, cancellationToken));

    [HttpPost("{id:int}/send")]
    [HasPermission(DokumaPermissions.Quotation.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaQuotationDto>>> Send(int id, CancellationToken cancellationToken) =>
        FromResult(await service.SendAsync(id, cancellationToken));

    /// <summary>Gönderilmiş, reddedilmiş ya da süresi dolmuş teklifi düzeltmek için taslağa alır.</summary>
    [HttpPost("{id:int}/reopen")]
    [HasPermission(DokumaPermissions.Quotation.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaQuotationDto>>> Reopen(int id, CancellationToken cancellationToken) =>
        FromResult(await service.ReopenAsync(id, cancellationToken));

    [HttpPost("{id:int}/reject")]
    [HasPermission(DokumaPermissions.Quotation.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaQuotationDto>>> Reject(int id, RejectDokumaQuotationRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.RejectAsync(id, request, cancellationToken));

    /// <summary>Kabul: teklifin satırları ve fiyatlarıyla taslak sipariş açar.</summary>
    [HttpPost("{id:int}/accept")]
    [HasPermission(DokumaPermissions.Quotation.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaQuotationDto>>> Accept(int id, AcceptDokumaQuotationRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.AcceptAsync(id, request, cancellationToken));
}

/// <summary>Ön maliyet parametreleri ve tezgâha özel saat maliyeti / verim.</summary>
[Route("api/v1/dokuma-cost-settings")]
public sealed class DokumaCostSettingsController(IDokumaCostSettingsService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(DokumaPermissions.Quotation.View)]
    public async Task<ActionResult<ApiResponse<DokumaCostSettingsDto>>> Get(CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetAsync(cancellationToken), HttpContext));

    [HttpPut]
    [HasPermission(DokumaPermissions.Quotation.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaCostSettingsDto>>> Save(DokumaCostSettingsRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SaveAsync(request, cancellationToken));
}

/// <summary>Tezgâh kapasite planı: levent kuyrukları, tahmini bitişler, doluluk ve termin riski.</summary>
[Route("api/v1/dokuma-capacity-plan")]
public sealed class DokumaCapacityPlanController(IDokumaCapacityPlanService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(DokumaPermissions.Planning.View)]
    public async Task<ActionResult<ApiResponse<DokumaCapacityPlanDto>>> Get([FromQuery] DokumaCapacityPlanQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetAsync(query, cancellationToken), HttpContext));

    /// <summary>Leventin taşınabileceği tezgâhlar, uygunluk uyarısı ve kuyruklarının bitişi.</summary>
    [HttpGet("beams/{beamId:int}/options")]
    [HasPermission(DokumaPermissions.Planning.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DokumaCapacityLoomOptionDto>>>> GetOptions(int beamId, CancellationToken cancellationToken) =>
        FromResult(await service.GetOptionsAsync(beamId, cancellationToken));

    /// <summary>Leventi tezgâh kuyruğuna alır, başka tezgâha taşır ya da plandan çıkarır.</summary>
    [HttpPut("beams/{beamId:int}")]
    [HasPermission(DokumaPermissions.Planning.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> Plan(int beamId, DokumaPlanBeamRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.PlanAsync(beamId, request, cancellationToken));

    /// <summary>Tezgâh kuyruğunun sırasını değiştirir.</summary>
    [HttpPut("looms/{loomId:int}/queue")]
    [HasPermission(DokumaPermissions.Planning.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> Reorder(int loomId, DokumaLoomQueueRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.ReorderAsync(loomId, request, cancellationToken));
}
