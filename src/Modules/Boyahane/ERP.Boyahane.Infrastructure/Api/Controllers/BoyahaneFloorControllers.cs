using ERP.Boyahane.Application.Features.Batches;
using ERP.Boyahane.Application.Features.Floor;
using ERP.Boyahane.Domain.Authorization;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Infrastructure.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ERP.Boyahane.Infrastructure.Api.Controllers;

[Route("api/v1/boyahane-operators")]
[ResourcePermissions(BoyahanePermissions.Floor.View, BoyahanePermissions.Floor.Manage)]
public sealed class BoyahaneOperatorsController(IMasterDataService<BoyahaneOperatorDto, BoyahaneOperatorRequest> service)
    : MasterDataController<BoyahaneOperatorDto, BoyahaneOperatorRequest>(service);

[Route("api/v1/boyahane-operators/{operatorId:int}")]
public sealed class BoyahaneOperatorCardController(IBoyahaneOperatorCardService service) : ApiControllerBase
{
    /// <summary>Operatörün fotoğrafı (base64). Fotoğraf yoksa NOT_FOUND döner.</summary>
    [HttpGet("photo")]
    [HasPermission(BoyahanePermissions.Floor.View)]
    public async Task<ActionResult<ApiResponse<BoyahaneOperatorPhotoDto>>> GetPhoto(int operatorId, CancellationToken cancellationToken) =>
        FromResult(await service.GetPhotoAsync(operatorId, cancellationToken));

    /// <summary>Operatörün fotoğrafını yükler (JPEG / PNG, en fazla 256 KB); varsa eskisinin yerine geçer.</summary>
    [HttpPut("photo")]
    [HasPermission(BoyahanePermissions.Floor.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> SetPhoto(int operatorId, BoyahaneOperatorPhotoRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SetPhotoAsync(operatorId, request, cancellationToken));

    [HttpDelete("photo")]
    [HasPermission(BoyahanePermissions.Floor.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> RemovePhoto(int operatorId, CancellationToken cancellationToken) =>
        FromResult(await service.RemovePhotoAsync(operatorId, cancellationToken));

    /// <summary>Operatörün yapabildiği operasyonlar ve (varsa) çalışabileceği makineler.</summary>
    [HttpGet("qualifications")]
    [HasPermission(BoyahanePermissions.Floor.View)]
    public async Task<ActionResult<ApiResponse<BoyahaneOperatorQualificationsDto>>> GetQualifications(int operatorId, CancellationToken cancellationToken) =>
        FromResult(await service.GetQualificationsAsync(operatorId, cancellationToken));

    /// <summary>Operatörün yetkinliklerini verilen listelerle değiştirir.</summary>
    [HttpPut("qualifications")]
    [HasPermission(BoyahanePermissions.Floor.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneOperatorQualificationsDto>>> ReplaceQualifications(int operatorId, BoyahaneOperatorQualificationsRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.ReplaceQualificationsAsync(operatorId, request, cancellationToken));
}

[Route("api/v1/boyahane-tablets")]
[ResourcePermissions(BoyahanePermissions.Floor.View, BoyahanePermissions.Floor.Manage)]
public sealed class BoyahaneTabletsController(IMasterDataService<BoyahaneTabletDto, BoyahaneTabletRequest> service)
    : MasterDataController<BoyahaneTabletDto, BoyahaneTabletRequest>(service);

[Route("api/v1/boyahane-tablets/{id:int}")]
public sealed class BoyahaneTabletAdminController(IBoyahaneTabletDeviceService service) : ApiControllerBase
{
    /// <summary>Eşleştirilmiş cihazın bağını koparır; tablet yeniden eşleştirilir.</summary>
    [HttpPost("reset-device")]
    [HasPermission(BoyahanePermissions.Floor.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> ResetDevice(int id, CancellationToken cancellationToken) =>
        FromResult(await service.ResetDeviceAsync(id, cancellationToken));
}

/// <summary>Boyahane tabletinin kendini tanıtması; verilen token yalnızca Boyahane tablet uçlarında geçerlidir.</summary>
[Route("api/v1/boyahane/tablet-auth")]
[AllowAnonymous]
[EnableRateLimiting(RateLimitPolicies.Auth)]
public sealed class BoyahaneTabletAuthController(IBoyahaneTabletDeviceService service) : ApiControllerBase
{
    /// <summary>Tablet ayarlarında seçilebilecek makineler ve kontrol noktaları; tablet ayarları parolası gerekir.</summary>
    [HttpPost("pairing/stations")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TabletPairingStationDto>>>> PairingStations(TabletPairingListRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.GetPairingStationsAsync(request, cancellationToken));

    /// <summary>Cihazı seçilen istasyona bağlar ve cihaz anahtarını döner (yalnızca bir kez).</summary>
    [HttpPost("pairing/pair")]
    public async Task<ActionResult<ApiResponse<PairTabletResponse>>> Pair(PairTabletRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.PairAsync(request, cancellationToken));

    [HttpPost("token")]
    public async Task<ActionResult<ApiResponse<BoyahaneTabletTokenResponse>>> Token(BoyahaneTabletTokenRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.IssueTokenAsync(request, cancellationToken));
}

/// <summary>Tablet istasyonu: personel oturumu, parti kartı okuma, adım başlatma ve bitirme.</summary>
[Route("api/v1/boyahane/tablet")]
[Authorize(Policy = AuthorizationPolicies.BoyahaneTablet)]
public sealed class BoyahaneTabletController(IBoyahaneTabletDeviceService service, IBoyahaneOperatorCardService operatorCards) : ApiControllerBase
{
    /// <summary>Tablette oturumu açık operatörün fotoğrafı; oturum ya da fotoğraf yoksa boş.</summary>
    [HttpGet("session/photo")]
    public async Task<ActionResult<ApiResponse<BoyahaneOperatorPhotoDto?>>> SessionPhoto(CancellationToken cancellationToken) =>
        FromResult(await operatorCards.GetSessionPhotoAsync(cancellationToken));


    [HttpGet("context")]
    public async Task<ActionResult<ApiResponse<BoyahaneTabletContextDto>>> GetContext(CancellationToken cancellationToken) =>
        FromResult(await service.GetContextAsync(cancellationToken));

    [HttpPost("heartbeat")]
    public async Task<ActionResult<ApiResponse<object?>>> Heartbeat(CancellationToken cancellationToken) =>
        FromResult(await service.HeartbeatAsync(cancellationToken));

    [HttpPost("session/login")]
    public async Task<ActionResult<ApiResponse<BoyahaneTabletContextDto>>> Login(BoyahaneTabletLoginRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.LoginAsync(request, cancellationToken));

    [HttpPost("session/logout")]
    public async Task<ActionResult<ApiResponse<BoyahaneTabletContextDto>>> Logout(CancellationToken cancellationToken) =>
        FromResult(await service.LogoutAsync(cancellationToken));

    [HttpPost("session/pause")]
    public async Task<ActionResult<ApiResponse<BoyahaneTabletContextDto>>> Pause(BoyahaneTabletPauseRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.PauseAsync(request, cancellationToken));

    [HttpPost("session/resume")]
    public async Task<ActionResult<ApiResponse<BoyahaneTabletContextDto>>> Resume(BoyahaneTabletResumeRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.ResumeAsync(request, cancellationToken));

    [HttpPost("machine/breakdown")]
    public async Task<ActionResult<ApiResponse<BoyahaneTabletContextDto>>> Breakdown(BoyahaneTabletBreakdownRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.ReportBreakdownAsync(request, cancellationToken));

    /// <summary>Arızayı kapatır: vardiya amirinin ya da bakımcının kartı okutulur.</summary>
    [HttpPost("machine/recover")]
    public async Task<ActionResult<ApiResponse<BoyahaneTabletContextDto>>> Recover(BoyahaneTabletRecoveryRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.ReportRecoveryAsync(request, cancellationToken));

    /// <summary>Okutulan RFID kart numarası ya da kağıt karttaki barkod.</summary>
    [HttpPost("batches/read")]
    public async Task<ActionResult<ApiResponse<BoyahaneBatchDto>>> Read(BoyahaneTabletReadRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.ReadBatchAsync(request, cancellationToken));

    [HttpGet("batches/{id:int}")]
    public async Task<ActionResult<ApiResponse<BoyahaneBatchDto>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetBatchAsync(id, cancellationToken));

    [HttpPost("batches/{id:int}/steps/{stepId:int}/start")]
    public async Task<ActionResult<ApiResponse<BoyahaneBatchDto>>> Start(int id, int stepId, BoyahaneTabletStartRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.StartStepAsync(id, stepId, request, cancellationToken));

    [HttpPost("batches/{id:int}/steps/{stepId:int}/complete")]
    public async Task<ActionResult<ApiResponse<BoyahaneBatchDto>>> Complete(int id, int stepId, CompleteBatchStepRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.CompleteStepAsync(id, stepId, request, cancellationToken));
}

[Route("api/v1/boyahane-rfid-cards")]
[ResourcePermissions(BoyahanePermissions.Floor.View, BoyahanePermissions.Floor.Manage)]
public sealed class BoyahaneRfidCardsController(IMasterDataService<BoyahaneRfidCardDto, BoyahaneRfidCardRequest> service)
    : MasterDataController<BoyahaneRfidCardDto, BoyahaneRfidCardRequest>(service);

[Route("api/v1/boyahane-stop-reasons")]
[ResourcePermissions(BoyahanePermissions.Floor.View, BoyahanePermissions.Floor.Manage)]
public sealed class BoyahaneStopReasonsController(IMasterDataService<BoyahaneStopReasonDto, BoyahaneStopReasonRequest> service)
    : MasterDataController<BoyahaneStopReasonDto, BoyahaneStopReasonRequest>(service);

/// <summary>Tablet oturumları: ofisten izleme ve kapatma.</summary>
[Route("api/v1/boyahane-work-sessions")]
public sealed class BoyahaneWorkSessionsController(IBoyahaneWorkSessionService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(BoyahanePermissions.Floor.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<BoyahaneWorkSessionDto>>>> GetList([FromQuery] BoyahaneWorkSessionQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetListAsync(query, cancellationToken), HttpContext));

    /// <summary>Oturumu kapatır; süren adım işlemde kalır, başka personel bitirebilir.</summary>
    [HttpPost("{id:long}/close")]
    [HasPermission(BoyahanePermissions.Floor.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneWorkSessionDto>>> Close(long id, CancellationToken cancellationToken) =>
        FromResult(await service.CloseAsync(id, cancellationToken));
}

/// <summary>Makine durumu: arıza, bakım ve geçmişi.</summary>
[Route("api/v1/boyahane-machines/{id:int}/status")]
public sealed class BoyahaneMachineStatusController(IBoyahaneMachineStatusService service, ICurrentPermissions permissions) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(BoyahanePermissions.Batch.View)]
    public async Task<ActionResult<ApiResponse<BoyahaneMachineStatusDto>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetAsync(id, cancellationToken));

    [HttpPost]
    [HasPermission(BoyahanePermissions.Batch.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneMachineStatusDto>>> Change(int id, ChangeBoyahaneMachineStatusRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.ChangeFromOfficeAsync(id, request, await permissions.HasAsync(BoyahanePermissions.Batch.ResolveBreakdown, cancellationToken), cancellationToken));
}

[Route("api/v1/boyahane-shifts")]
[ResourcePermissions(BoyahanePermissions.Floor.View, BoyahanePermissions.Floor.Manage)]
public sealed class BoyahaneShiftsController(IMasterDataService<BoyahaneShiftDto, BoyahaneShiftRequest> service)
    : MasterDataController<BoyahaneShiftDto, BoyahaneShiftRequest>(service);

[Route("api/v1/boyahane-break-groups")]
[ResourcePermissions(BoyahanePermissions.Floor.View, BoyahanePermissions.Floor.Manage)]
public sealed class BoyahaneBreakGroupsController(IMasterDataService<BoyahaneBreakGroupDto, BoyahaneBreakGroupRequest> service)
    : MasterDataController<BoyahaneBreakGroupDto, BoyahaneBreakGroupRequest>(service);

[Route("api/v1/boyahane-break-schedules")]
[ResourcePermissions(BoyahanePermissions.Floor.View, BoyahanePermissions.Floor.Manage)]
public sealed class BoyahaneBreakSchedulesController(IMasterDataService<BoyahaneBreakScheduleDto, BoyahaneBreakScheduleRequest> service)
    : MasterDataController<BoyahaneBreakScheduleDto, BoyahaneBreakScheduleRequest>(service);
