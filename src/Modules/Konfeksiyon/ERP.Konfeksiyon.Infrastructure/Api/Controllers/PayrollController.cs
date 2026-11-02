using ERP.Konfeksiyon.Domain.Authorization;
using ERP.Core.Infrastructure.Web.Authorization;
using ERP.Core.Infrastructure.Web;
using ERP.Konfeksiyon.Application.Features.Payroll;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Konfeksiyon.Infrastructure.Api.Controllers;

/// <summary>Parça başı ücret ve prim: ayarlar ve operatör kazançları.</summary>
[Route("api/v1/payroll")]
public sealed class PayrollController(IPayrollService service) : ApiControllerBase
{
    /// <summary>Dakika ücreti, para birimi ve prim kademeleri.</summary>
    [HttpGet("settings")]
    [HasPermission(KonfeksiyonPermissions.Payroll.View)]
    public async Task<ActionResult<ApiResponse<PayrollSettingsDto>>> GetSettings(CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetSettingsAsync(cancellationToken), HttpContext));

    /// <summary>Ayarları günceller. Yeni ücretler yalnızca bundan sonra açılan demetlere uygulanır.</summary>
    [HttpPut("settings")]
    [HasPermission(KonfeksiyonPermissions.Payroll.Manage)]
    public async Task<ActionResult<ApiResponse<PayrollSettingsDto>>> UpdateSettings(
        UpdatePayrollSettingsRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.UpdateSettingsAsync(request, cancellationToken));

    /// <summary>Aralıkta üretim bildiren operatörlerin parça başı kazancı ve primi (en fazla 31 gün).</summary>
    [HttpGet("earnings")]
    [HasPermission(KonfeksiyonPermissions.Payroll.View)]
    public async Task<ActionResult<ApiResponse<PayrollReportDto>>> GetEarnings(
        [FromQuery] EarningsQuery query, CancellationToken cancellationToken) =>
        FromResult(await service.GetEarningsAsync(query, cancellationToken));

    /// <summary>Bir operatörün kazancının gün ve operasyon dökümü.</summary>
    [HttpGet("earnings/{operatorId:int}")]
    [HasPermission(KonfeksiyonPermissions.Payroll.View)]
    public async Task<ActionResult<ApiResponse<OperatorEarningsDetailDto>>> GetOperatorEarnings(
        int operatorId, [FromQuery] EarningsQuery query, CancellationToken cancellationToken) =>
        FromResult(await service.GetOperatorEarningsAsync(operatorId, query, cancellationToken));
}
