using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Infrastructure.Web.Authorization;
using ERP.Dokuma.Application.Features.Dashboard;
using ERP.Dokuma.Application.Features.Floor;
using ERP.Dokuma.Application.Features.Reports;
using ERP.Dokuma.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ERP.Dokuma.Infrastructure.Api.Controllers;

[Route("api/v1/dokuma-operators")]
[ResourcePermissions(DokumaPermissions.Floor.View, DokumaPermissions.Floor.Manage)]
public sealed class DokumaOperatorsController(IMasterDataService<DokumaOperatorDto, DokumaOperatorRequest> service)
    : MasterDataController<DokumaOperatorDto, DokumaOperatorRequest>(service);

/// <summary>Operatör kartının kayıt dışı bölümleri: fotoğraf ve çalışabileceği tezgâhlar.</summary>
[Route("api/v1/dokuma-operators/{operatorId:int}")]
public sealed class DokumaOperatorCardController(IDokumaOperatorCardService service) : ApiControllerBase
{
    /// <summary>Operatörün fotoğrafı (base64). Fotoğraf yoksa NOT_FOUND döner.</summary>
    [HttpGet("photo")]
    [HasPermission(DokumaPermissions.Floor.View)]
    public async Task<ActionResult<ApiResponse<DokumaOperatorPhotoDto>>> GetPhoto(int operatorId, CancellationToken cancellationToken) =>
        FromResult(await service.GetPhotoAsync(operatorId, cancellationToken));

    /// <summary>Operatörün fotoğrafını yükler (JPEG / PNG, en fazla 256 KB); varsa eskisinin yerine geçer.</summary>
    [HttpPut("photo")]
    [HasPermission(DokumaPermissions.Floor.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> SetPhoto(int operatorId, DokumaOperatorPhotoRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SetPhotoAsync(operatorId, request, cancellationToken));

    [HttpDelete("photo")]
    [HasPermission(DokumaPermissions.Floor.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> RemovePhoto(int operatorId, CancellationToken cancellationToken) =>
        FromResult(await service.RemovePhotoAsync(operatorId, cancellationToken));

    [HttpGet("looms")]
    [HasPermission(DokumaPermissions.Floor.View)]
    public async Task<ActionResult<ApiResponse<DokumaOperatorLoomsDto>>> GetLooms(int operatorId, CancellationToken cancellationToken) =>
        FromResult(await service.GetLoomsAsync(operatorId, cancellationToken));

    [HttpPut("looms")]
    [HasPermission(DokumaPermissions.Floor.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaOperatorLoomsDto>>> ReplaceLooms(int operatorId, DokumaOperatorLoomsRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.ReplaceLoomsAsync(operatorId, request, cancellationToken));
}

[Route("api/v1/dokuma-rfid-cards")]
[ResourcePermissions(DokumaPermissions.Floor.View, DokumaPermissions.Floor.Manage)]
public sealed class DokumaRfidCardsController(IMasterDataService<DokumaRfidCardDto, DokumaRfidCardRequest> service)
    : MasterDataController<DokumaRfidCardDto, DokumaRfidCardRequest>(service);

[Route("api/v1/dokuma-stop-reasons")]
[ResourcePermissions(DokumaPermissions.Floor.View, DokumaPermissions.Floor.Manage)]
public sealed class DokumaStopReasonsController(IMasterDataService<DokumaStopReasonDto, DokumaStopReasonRequest> service)
    : MasterDataController<DokumaStopReasonDto, DokumaStopReasonRequest>(service);

[Route("api/v1/dokuma-tablets")]
[ResourcePermissions(DokumaPermissions.Floor.View, DokumaPermissions.Floor.Manage)]
public sealed class DokumaTabletsController(IMasterDataService<DokumaTabletDto, DokumaTabletRequest> service)
    : MasterDataController<DokumaTabletDto, DokumaTabletRequest>(service);

[Route("api/v1/dokuma-tablets/{id:int}")]
public sealed class DokumaTabletAdminController(IDokumaTabletDeviceService service) : ApiControllerBase
{
    /// <summary>Eşleştirilmiş cihazın bağını koparır; tablet yeniden eşleştirilir.</summary>
    [HttpPost("reset-device")]
    [HasPermission(DokumaPermissions.Floor.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> ResetDevice(int id, CancellationToken cancellationToken) =>
        FromResult(await service.ResetDeviceAsync(id, cancellationToken));
}

/// <summary>Dokuma tabletinin kendini tanıtması; verilen token yalnızca Dokuma tablet uçlarında geçerlidir.</summary>
[Route("api/v1/dokuma/tablet-auth")]
[AllowAnonymous]
[EnableRateLimiting(RateLimitPolicies.Auth)]
public sealed class DokumaTabletAuthController(IDokumaTabletDeviceService service) : ApiControllerBase
{
    /// <summary>Tablet ayarlarında seçilebilecek tezgâhlar ve salon tabletleri; tablet ayarları parolası gerekir.</summary>
    [HttpPost("pairing/stations")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TabletPairingStationDto>>>> PairingStations(TabletPairingListRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.GetPairingStationsAsync(request, cancellationToken));

    /// <summary>Cihazı seçilen istasyona bağlar ve cihaz anahtarını döner (yalnızca bir kez).</summary>
    [HttpPost("pairing/pair")]
    public async Task<ActionResult<ApiResponse<PairTabletResponse>>> Pair(PairTabletRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.PairAsync(request, cancellationToken));

    [HttpPost("token")]
    public async Task<ActionResult<ApiResponse<DokumaTabletTokenResponse>>> Token(DokumaTabletTokenRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.IssueTokenAsync(request, cancellationToken));
}

/// <summary>Tablet istasyonu: personel oturumu, top kesimi ve tezgâh duruşu.</summary>
[Route("api/v1/dokuma/tablet")]
[Authorize(Policy = AuthorizationPolicies.DokumaTablet)]
public sealed class DokumaTabletController(IDokumaTabletDeviceService service, IDokumaOperatorCardService operatorCards) : ApiControllerBase
{
    [HttpGet("context")]
    public async Task<ActionResult<ApiResponse<DokumaTabletContextDto>>> GetContext(CancellationToken cancellationToken) =>
        FromResult(await service.GetContextAsync(cancellationToken));

    [HttpPost("heartbeat")]
    public async Task<ActionResult<ApiResponse<object?>>> Heartbeat(CancellationToken cancellationToken) =>
        FromResult(await service.HeartbeatAsync(cancellationToken));

    [HttpPost("session/login")]
    public async Task<ActionResult<ApiResponse<DokumaTabletContextDto>>> Login(DokumaTabletLoginRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.LoginAsync(request, cancellationToken));

    [HttpPost("session/logout")]
    public async Task<ActionResult<ApiResponse<DokumaTabletContextDto>>> Logout(CancellationToken cancellationToken) =>
        FromResult(await service.LogoutAsync(cancellationToken));

    /// <summary>Tablette oturumu açık operatörün fotoğrafı; oturum ya da fotoğraf yoksa boş.</summary>
    [HttpGet("session/photo")]
    public async Task<ActionResult<ApiResponse<DokumaOperatorPhotoDto?>>> SessionPhoto(CancellationToken cancellationToken) =>
        FromResult(await operatorCards.GetSessionPhotoAsync(cancellationToken));

    [HttpPost("session/pause")]
    public async Task<ActionResult<ApiResponse<DokumaTabletContextDto>>> Pause(DokumaTabletPauseRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.PauseAsync(request, cancellationToken));

    [HttpPost("session/resume")]
    public async Task<ActionResult<ApiResponse<DokumaTabletContextDto>>> Resume(DokumaTabletResumeRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.ResumeAsync(request, cancellationToken));

    [HttpPost("beams/{beamId:int}/rolls")]
    public async Task<ActionResult<ApiResponse<DokumaTabletRollDto>>> CutRoll(int beamId, DokumaTabletRollRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.CutRollAsync(beamId, request, cancellationToken));

    [HttpPost("looms/{loomId:int}/stop")]
    public async Task<ActionResult<ApiResponse<DokumaTabletContextDto>>> StartStop(int loomId, DokumaTabletStopRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.StartStopAsync(loomId, request, cancellationToken));

    [HttpPost("looms/{loomId:int}/resume")]
    public async Task<ActionResult<ApiResponse<DokumaTabletContextDto>>> EndStop(int loomId, DokumaTabletEndStopRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.EndStopAsync(loomId, request, cancellationToken));
}

/// <summary>Tablet oturumları: ofisten izleme ve kapatma.</summary>
[Route("api/v1/dokuma-work-sessions")]
public sealed class DokumaWorkSessionsController(IDokumaWorkSessionService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(DokumaPermissions.Floor.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<DokumaWorkSessionDto>>>> GetList([FromQuery] DokumaWorkSessionQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetListAsync(query, cancellationToken), HttpContext));

    [HttpPost("{id:long}/close")]
    [HasPermission(DokumaPermissions.Floor.Manage)]
    public async Task<ActionResult<ApiResponse<DokumaWorkSessionDto>>> Close(long id, CancellationToken cancellationToken) =>
        FromResult(await service.CloseAsync(id, cancellationToken));
}

/// <summary>Tezgâh duruşları: geçmiş ve ofisten açma / kapatma.</summary>
[Route("api/v1/dokuma-loom-stops")]
public sealed class DokumaLoomStopsController(IDokumaLoomStopService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(DokumaPermissions.Weaving.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<DokumaLoomStopDto>>>> GetList([FromQuery] DokumaLoomStopQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetListAsync(query, cancellationToken), HttpContext));

    [HttpPost("looms/{loomId:int}/start")]
    [HasPermission(DokumaPermissions.Floor.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> Start(int loomId, StartDokumaLoomStopRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.StartAsync(loomId, request, null, cancellationToken));

    [HttpPost("looms/{loomId:int}/end")]
    [HasPermission(DokumaPermissions.Floor.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> End(int loomId, CancellationToken cancellationToken) =>
        FromResult(await service.EndAsync(loomId, null, cancellationToken));
}

/// <summary>Canlı pano; modülü gören herkes özet rakamları görür, ayrıntı ekranları kendi yetkisini ister.</summary>
[Route("api/v1/dokuma-dashboard")]
public sealed class DokumaDashboardController(IDokumaDashboardService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(DokumaPermissions.Module.View)]
    public async Task<ActionResult<ApiResponse<DokumaDashboardDto>>> Get(CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetAsync(cancellationToken), HttpContext));
}

[Route("api/v1/dokuma-reports")]
public sealed class DokumaReportsController(IDokumaReportService service) : ApiControllerBase
{
    /// <summary>Tezgâh verimi: dokunan metre, duruş süreleri, çalışma oranı ve anma hızına göre verim.</summary>
    [HttpGet("looms")]
    [HasPermission(DokumaPermissions.Report.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DokumaLoomReportRowDto>>>> GetLooms([FromQuery] DokumaLoomReportQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetLoomsAsync(query, cancellationToken), HttpContext));

    /// <summary>Sipariş takibi: çözgüden faturaya satır başına metre.</summary>
    [HttpGet("orders")]
    [HasPermission(DokumaPermissions.Report.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<DokumaOrderReportRowDto>>>> GetOrders([FromQuery] DokumaOrderReportQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetOrdersAsync(query, cancellationToken), HttpContext));

    /// <summary>Kalite özeti: kontrol tarihine göre artikel, tezgâh ve hata türü kırılımı.</summary>
    [HttpGet("quality")]
    [HasPermission(DokumaPermissions.Report.View)]
    public async Task<ActionResult<ApiResponse<DokumaQualityReportDto>>> GetQuality([FromQuery] DokumaQualityReportQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetQualityAsync(query, cancellationToken), HttpContext));

    /// <summary>Günlük sevk: gün ve müşteri bazında onaylı irsaliyeler, Boyahane'ye gönderilenler ayrı satırda.</summary>
    [HttpGet("shipments")]
    [HasPermission(DokumaPermissions.Report.View)]
    public async Task<ActionResult<ApiResponse<DokumaShipmentReportDto>>> GetShipments([FromQuery] DokumaShipmentReportQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetShipmentsAsync(query, cancellationToken), HttpContext));

    /// <summary>Hareketsiz stok: belirtilen gündür hareket görmeyen iplik lotları, ham toplar ve leventler.</summary>
    [HttpGet("idle-stock")]
    [HasPermission(DokumaPermissions.Report.View)]
    public async Task<ActionResult<ApiResponse<DokumaIdleStockDto>>> GetIdleStock([FromQuery] DokumaIdleStockQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetIdleStockAsync(query, cancellationToken), HttpContext));
}

[Route("api/v1/dokuma-shifts")]
[ResourcePermissions(DokumaPermissions.Floor.View, DokumaPermissions.Floor.Manage)]
public sealed class DokumaShiftsController(IMasterDataService<DokumaShiftDto, DokumaShiftRequest> service)
    : MasterDataController<DokumaShiftDto, DokumaShiftRequest>(service);

[Route("api/v1/dokuma-break-groups")]
[ResourcePermissions(DokumaPermissions.Floor.View, DokumaPermissions.Floor.Manage)]
public sealed class DokumaBreakGroupsController(IMasterDataService<DokumaBreakGroupDto, DokumaBreakGroupRequest> service)
    : MasterDataController<DokumaBreakGroupDto, DokumaBreakGroupRequest>(service);

[Route("api/v1/dokuma-break-schedules")]
[ResourcePermissions(DokumaPermissions.Floor.View, DokumaPermissions.Floor.Manage)]
public sealed class DokumaBreakSchedulesController(IMasterDataService<DokumaBreakScheduleDto, DokumaBreakScheduleRequest> service)
    : MasterDataController<DokumaBreakScheduleDto, DokumaBreakScheduleRequest>(service);
