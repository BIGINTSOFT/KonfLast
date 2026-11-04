using ERP.Konfeksiyon.Domain.Authorization;
using ERP.Core.Infrastructure.Web.Authorization;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Application.Common;
using ERP.Konfeksiyon.Application.Features.ProductionTracking;
using ERP.Konfeksiyon.Application.Features.Rfid;
using ERP.Konfeksiyon.Application.Features.WorkSessions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Konfeksiyon.Infrastructure.Api.Controllers;

/// <summary>Tabletteki operatör oturumu (tablet token'ı gerekir). Makine, tabletin bağlı olduğu makinedir.</summary>
[Route("api/v1/production/session")]
[Authorize(Policy = AuthorizationPolicies.TabletDevice)]
public sealed class TabletSessionController(
    IWorkSessionService service, IOperatorPhotoService photos, ITabletPerformanceService performance) : ApiControllerBase
{
    /// <summary>
    /// Oturumu açık operatörün seçilen dönemdeki (bugün / bu hafta) verimi, kalitesi, kıyasları ve saatlik ya da
    /// günlük adet dökümü; oturum yoksa <c>data: null</c>.
    /// </summary>
    [HttpGet("performance")]
    public async Task<ActionResult<ApiResponse<TabletPerformanceDto?>>> GetPerformance(
        [FromQuery] TabletPerformancePeriod period, CancellationToken cancellationToken) =>
        FromResult(await performance.GetForStationAsync(period, cancellationToken));

    /// <summary>Makinede oturumu açık olan operatörün fotoğrafı; oturum ya da fotoğraf yoksa <c>data: null</c>.</summary>
    [HttpGet("photo")]
    public async Task<ActionResult<ApiResponse<OperatorPhotoDto?>>> GetPhoto(CancellationToken cancellationToken) =>
        FromResult(await photos.GetForStationAsync(cancellationToken));

    /// <summary>Makinedeki açık oturum; yoksa <c>data: null</c>.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<TabletSessionDto?>>> GetCurrent(CancellationToken cancellationToken) =>
        FromResult(await service.GetCurrentAsync(cancellationToken));

    /// <summary>
    /// Okutulan personel kartıyla oturum açar. Makinede ya da operatörde süren iş yoksa önceki açık oturumlar
    /// kapatılır. Hatalar: kart hataları (RFID_*), OPERATOR_INACTIVE, OPERATOR_NOT_QUALIFIED, WORK_IN_PROGRESS,
    /// OPERATOR_BUSY, MACHINE_INACTIVE, TABLET_NOT_ASSIGNED.
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<TabletSessionDto>>> Login(
        RfidReadRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.LoginAsync(request, cancellationToken));

    /// <summary>
    /// Planlı molaya çıkma (BREAK), ara verme (PAUSE, neden zorunlu) ya da demet bekleme (WAITING_FOR_BUNDLE).
    /// Hatalar: NO_ACTIVE_SESSION, NO_BREAK_DUE, BREAK_AFTER_WORK, WORK_IN_PROGRESS, SESSION_INTERRUPTED.
    /// </summary>
    [HttpPost("pause")]
    public async Task<ActionResult<ApiResponse<TabletSessionDto>>> Pause(
        PauseSessionRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.PauseAsync(request, cancellationToken));

    /// <summary>Duraklamadan çalışmaya döner. Operasyon başlatmak / adet bildirmek de duraklamayı sonlandırır.</summary>
    [HttpPost("resume")]
    public async Task<ActionResult<ApiResponse<TabletSessionDto>>> Resume(
        ResumeSessionRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.ResumeAsync(request, cancellationToken));

    /// <summary>Makinedeki oturumu kapatır. Operatörün süren işi varsa WORK_IN_PROGRESS döner.</summary>
    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse<object?>>> Logout(CancellationToken cancellationToken) =>
        FromResult(await service.LogoutAsync(cancellationToken));
}

[Route("api/v1/work-sessions")]
public sealed class WorkSessionsController(IWorkSessionService service) : ApiControllerBase
{
    /// <summary>Çalışma oturumları, en yeniden eskiye (sayfalı; operatör / makine / açık-kapalı / tarih filtresi).</summary>
    [HttpGet]
    [HasPermission(KonfeksiyonPermissions.Operator.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<WorkSessionDto>>>> GetList(
        [FromQuery] WorkSessionListQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetListAsync(query, cancellationToken), HttpContext));

    /// <summary>Oturumun duraklamaları: planlı mola, ara, makine arızası, demet bekleme (süreleriyle).</summary>
    [HttpGet("{id:long}/pauses")]
    [HasPermission(KonfeksiyonPermissions.Operator.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WorkSessionPauseDto>>>> GetPauses(
        long id, CancellationToken cancellationToken) =>
        FromResult(await service.GetPausesAsync(id, cancellationToken));

    /// <summary>
    /// Açık kalmış oturumu kapatır. Operatörün süren işi varsa WORK_IN_PROGRESS döner; <c>releaseWork=true</c> ile
    /// süren iş adet bildirilmeden bırakılıp (kalan adet sonra sürdürülür) oturum kapatılır.
    /// </summary>
    [HttpPost("{id:long}/close")]
    [HasPermission(KonfeksiyonPermissions.WorkSession.Close)]
    public async Task<ActionResult<ApiResponse<WorkSessionDto>>> Close(
        long id, [FromQuery] bool releaseWork, CancellationToken cancellationToken) =>
        FromResult(await service.CloseAsync(id, releaseWork, cancellationToken));
}

[Route("api/v1/operators/{operatorId:int}/photo")]
public sealed class OperatorPhotosController(IOperatorPhotoService service) : ApiControllerBase
{
    /// <summary>Operatörün fotoğrafı (base64). Fotoğraf yoksa NOT_FOUND döner.</summary>
    [HttpGet]
    [HasPermission(KonfeksiyonPermissions.Operator.ViewPersonal)]
    public async Task<ActionResult<ApiResponse<OperatorPhotoDto>>> Get(int operatorId, CancellationToken cancellationToken) =>
        FromResult(await service.GetAsync(operatorId, cancellationToken));

    /// <summary>Operatörün fotoğrafını yükler (JPEG / PNG, en fazla 256 KB); varsa eskisinin yerine geçer.</summary>
    [HttpPut]
    [HasPermission(KonfeksiyonPermissions.Operator.Update)]
    public async Task<ActionResult<ApiResponse<object?>>> Set(
        int operatorId, OperatorPhotoRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SetAsync(operatorId, request, cancellationToken));

    [HttpDelete]
    [HasPermission(KonfeksiyonPermissions.Operator.Update)]
    public async Task<ActionResult<ApiResponse<object?>>> Remove(int operatorId, CancellationToken cancellationToken) =>
        FromResult(await service.RemoveAsync(operatorId, cancellationToken));
}

[Route("api/v1/operators/{operatorId:int}/qualifications")]
public sealed class OperatorQualificationsController(IOperatorQualificationService service) : ApiControllerBase
{
    /// <summary>Operatörün yapabildiği operasyonlar ve (varsa) çalışabileceği makineler.</summary>
    [HttpGet]
    [HasPermission(KonfeksiyonPermissions.Operator.View)]
    public async Task<ActionResult<ApiResponse<OperatorQualificationsDto>>> Get(
        int operatorId, CancellationToken cancellationToken) =>
        FromResult(await service.GetAsync(operatorId, cancellationToken));

    /// <summary>Operatörün yetkinliklerini verilen listelerle değiştirir.</summary>
    [HttpPut]
    [HasPermission(KonfeksiyonPermissions.Operator.Update)]
    public async Task<ActionResult<ApiResponse<OperatorQualificationsDto>>> Replace(
        int operatorId, OperatorQualificationsRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.ReplaceAsync(operatorId, request, cancellationToken));
}
