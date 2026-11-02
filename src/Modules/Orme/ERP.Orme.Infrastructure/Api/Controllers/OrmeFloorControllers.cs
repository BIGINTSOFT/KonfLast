using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Infrastructure.Web.Authorization;
using ERP.Orme.Application.Features.Dashboard;
using ERP.Orme.Application.Features.Floor;
using ERP.Orme.Application.Features.Reports;
using ERP.Orme.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ERP.Orme.Infrastructure.Api.Controllers;

[Route("api/v1/orme-operators")]
[ResourcePermissions(OrmePermissions.Floor.View, OrmePermissions.Floor.Manage)]
public sealed class OrmeOperatorsController(IMasterDataService<OrmeOperatorDto, OrmeOperatorRequest> service)
    : MasterDataController<OrmeOperatorDto, OrmeOperatorRequest>(service);

/// <summary>Operatör kartının kayıt dışı bölümleri: fotoğraf ve çalışabileceği makineler.</summary>
[Route("api/v1/orme-operators/{operatorId:int}")]
public sealed class OrmeOperatorCardController(IOrmeOperatorCardService service) : ApiControllerBase
{
    /// <summary>Operatörün fotoğrafı (base64). Fotoğraf yoksa NOT_FOUND döner.</summary>
    [HttpGet("photo")]
    [HasPermission(OrmePermissions.Floor.View)]
    public async Task<ActionResult<ApiResponse<OrmeOperatorPhotoDto>>> GetPhoto(int operatorId, CancellationToken cancellationToken) =>
        FromResult(await service.GetPhotoAsync(operatorId, cancellationToken));

    /// <summary>Operatörün fotoğrafını yükler (JPEG / PNG, en fazla 256 KB); varsa eskisinin yerine geçer.</summary>
    [HttpPut("photo")]
    [HasPermission(OrmePermissions.Floor.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> SetPhoto(int operatorId, OrmeOperatorPhotoRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SetPhotoAsync(operatorId, request, cancellationToken));

    [HttpDelete("photo")]
    [HasPermission(OrmePermissions.Floor.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> RemovePhoto(int operatorId, CancellationToken cancellationToken) =>
        FromResult(await service.RemovePhotoAsync(operatorId, cancellationToken));

    [HttpGet("machines")]
    [HasPermission(OrmePermissions.Floor.View)]
    public async Task<ActionResult<ApiResponse<OrmeOperatorMachinesDto>>> GetMachines(int operatorId, CancellationToken cancellationToken) =>
        FromResult(await service.GetMachinesAsync(operatorId, cancellationToken));

    [HttpPut("machines")]
    [HasPermission(OrmePermissions.Floor.Manage)]
    public async Task<ActionResult<ApiResponse<OrmeOperatorMachinesDto>>> ReplaceMachines(int operatorId, OrmeOperatorMachinesRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.ReplaceMachinesAsync(operatorId, request, cancellationToken));
}

[Route("api/v1/orme-rfid-cards")]
[ResourcePermissions(OrmePermissions.Floor.View, OrmePermissions.Floor.Manage)]
public sealed class OrmeRfidCardsController(IMasterDataService<OrmeRfidCardDto, OrmeRfidCardRequest> service)
    : MasterDataController<OrmeRfidCardDto, OrmeRfidCardRequest>(service);

[Route("api/v1/orme-stop-reasons")]
[ResourcePermissions(OrmePermissions.Floor.View, OrmePermissions.Floor.Manage)]
public sealed class OrmeStopReasonsController(IMasterDataService<OrmeStopReasonDto, OrmeStopReasonRequest> service)
    : MasterDataController<OrmeStopReasonDto, OrmeStopReasonRequest>(service);

[Route("api/v1/orme-tablets")]
[ResourcePermissions(OrmePermissions.Floor.View, OrmePermissions.Floor.Manage)]
public sealed class OrmeTabletsController(IMasterDataService<OrmeTabletDto, OrmeTabletRequest> service)
    : MasterDataController<OrmeTabletDto, OrmeTabletRequest>(service);

[Route("api/v1/orme-tablets/{id:int}")]
public sealed class OrmeTabletAdminController(IOrmeTabletDeviceService service) : ApiControllerBase
{
    /// <summary>Eşleştirilmiş cihazın bağını koparır; tablet yeniden eşleştirilir.</summary>
    [HttpPost("reset-device")]
    [HasPermission(OrmePermissions.Floor.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> ResetDevice(int id, CancellationToken cancellationToken) =>
        FromResult(await service.ResetDeviceAsync(id, cancellationToken));
}

/// <summary>Örme tabletinin kendini tanıtması; verilen token yalnızca Örme tablet uçlarında geçerlidir.</summary>
[Route("api/v1/orme/tablet-auth")]
[AllowAnonymous]
[EnableRateLimiting(RateLimitPolicies.Auth)]
public sealed class OrmeTabletAuthController(IOrmeTabletDeviceService service) : ApiControllerBase
{
    /// <summary>Tablet ayarlarında seçilebilecek makineler ve salon tabletleri; tablet ayarları parolası gerekir.</summary>
    [HttpPost("pairing/stations")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TabletPairingStationDto>>>> PairingStations(TabletPairingListRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.GetPairingStationsAsync(request, cancellationToken));

    /// <summary>Cihazı seçilen istasyona bağlar ve cihaz anahtarını döner (yalnızca bir kez).</summary>
    [HttpPost("pairing/pair")]
    public async Task<ActionResult<ApiResponse<PairTabletResponse>>> Pair(PairTabletRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.PairAsync(request, cancellationToken));

    [HttpPost("token")]
    public async Task<ActionResult<ApiResponse<OrmeTabletTokenResponse>>> Token(OrmeTabletTokenRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.IssueTokenAsync(request, cancellationToken));
}

/// <summary>Tablet istasyonu: personel oturumu, top kaydı ve makine duruşu.</summary>
[Route("api/v1/orme/tablet")]
[Authorize(Policy = AuthorizationPolicies.OrmeTablet)]
public sealed class OrmeTabletController(IOrmeTabletDeviceService service, IOrmeOperatorCardService operatorCards) : ApiControllerBase
{
    [HttpGet("context")]
    public async Task<ActionResult<ApiResponse<OrmeTabletContextDto>>> GetContext(CancellationToken cancellationToken) =>
        FromResult(await service.GetContextAsync(cancellationToken));

    [HttpPost("heartbeat")]
    public async Task<ActionResult<ApiResponse<object?>>> Heartbeat(CancellationToken cancellationToken) =>
        FromResult(await service.HeartbeatAsync(cancellationToken));

    [HttpPost("session/login")]
    public async Task<ActionResult<ApiResponse<OrmeTabletContextDto>>> Login(OrmeTabletLoginRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.LoginAsync(request, cancellationToken));

    [HttpPost("session/logout")]
    public async Task<ActionResult<ApiResponse<OrmeTabletContextDto>>> Logout(CancellationToken cancellationToken) =>
        FromResult(await service.LogoutAsync(cancellationToken));

    /// <summary>Tablette oturumu açık operatörün fotoğrafı; oturum ya da fotoğraf yoksa boş.</summary>
    [HttpGet("session/photo")]
    public async Task<ActionResult<ApiResponse<OrmeOperatorPhotoDto?>>> SessionPhoto(CancellationToken cancellationToken) =>
        FromResult(await operatorCards.GetSessionPhotoAsync(cancellationToken));

    [HttpPost("session/pause")]
    public async Task<ActionResult<ApiResponse<OrmeTabletContextDto>>> Pause(OrmeTabletPauseRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.PauseAsync(request, cancellationToken));

    [HttpPost("session/resume")]
    public async Task<ActionResult<ApiResponse<OrmeTabletContextDto>>> Resume(OrmeTabletResumeRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.ResumeAsync(request, cancellationToken));

    [HttpPost("knit-orders/{knitOrderId:int}/rolls")]
    public async Task<ActionResult<ApiResponse<OrmeTabletRollDto>>> CutRoll(int knitOrderId, OrmeTabletRollRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.CutRollAsync(knitOrderId, request, cancellationToken));

    [HttpPost("machines/{machineId:int}/stop")]
    public async Task<ActionResult<ApiResponse<OrmeTabletContextDto>>> StartStop(int machineId, OrmeTabletStopRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.StartStopAsync(machineId, request, cancellationToken));

    [HttpPost("machines/{machineId:int}/resume")]
    public async Task<ActionResult<ApiResponse<OrmeTabletContextDto>>> EndStop(int machineId, OrmeTabletEndStopRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.EndStopAsync(machineId, request, cancellationToken));
}

/// <summary>Tablet oturumları: ofisten izleme ve kapatma.</summary>
[Route("api/v1/orme-work-sessions")]
public sealed class OrmeWorkSessionsController(IOrmeWorkSessionService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(OrmePermissions.Floor.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<OrmeWorkSessionDto>>>> GetList([FromQuery] OrmeWorkSessionQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetListAsync(query, cancellationToken), HttpContext));

    [HttpPost("{id:long}/close")]
    [HasPermission(OrmePermissions.Floor.Manage)]
    public async Task<ActionResult<ApiResponse<OrmeWorkSessionDto>>> Close(long id, CancellationToken cancellationToken) =>
        FromResult(await service.CloseAsync(id, cancellationToken));
}

/// <summary>Makine duruşları: geçmiş ve ofisten açma / kapatma.</summary>
[Route("api/v1/orme-machine-stops")]
public sealed class OrmeMachineStopsController(IOrmeMachineStopService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(OrmePermissions.Knitting.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<OrmeMachineStopDto>>>> GetList([FromQuery] OrmeMachineStopQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetListAsync(query, cancellationToken), HttpContext));

    [HttpPost("machines/{machineId:int}/start")]
    [HasPermission(OrmePermissions.Floor.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> Start(int machineId, StartOrmeMachineStopRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.StartAsync(machineId, request, null, cancellationToken));

    [HttpPost("machines/{machineId:int}/end")]
    [HasPermission(OrmePermissions.Floor.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> End(int machineId, CancellationToken cancellationToken) =>
        FromResult(await service.EndAsync(machineId, null, cancellationToken));
}

/// <summary>Canlı pano; modülü gören herkes özet rakamları görür, ayrıntı ekranları kendi yetkisini ister.</summary>
[Route("api/v1/orme-dashboard")]
public sealed class OrmeDashboardController(IOrmeDashboardService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(OrmePermissions.Module.View)]
    public async Task<ActionResult<ApiResponse<OrmeDashboardDto>>> Get(CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetAsync(cancellationToken), HttpContext));
}

[Route("api/v1/orme-reports")]
public sealed class OrmeReportsController(IOrmeReportService service) : ApiControllerBase
{
    /// <summary>Makine verimi: örülen kilo, duruş süreleri, çalışma oranı ve beklenen kg/saate göre verim.</summary>
    [HttpGet("machines")]
    [HasPermission(OrmePermissions.Report.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<OrmeMachineReportRowDto>>>> GetMachines([FromQuery] OrmeMachineReportQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetMachinesAsync(query, cancellationToken), HttpContext));

    /// <summary>Sipariş takibi: emirden faturaya satır başına kilo.</summary>
    [HttpGet("orders")]
    [HasPermission(OrmePermissions.Report.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<OrmeOrderReportRowDto>>>> GetOrders([FromQuery] OrmeOrderReportQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetOrdersAsync(query, cancellationToken), HttpContext));

    /// <summary>Kalite özeti: kontrol tarihine göre kumaş, makine ve hata türü kırılımı.</summary>
    [HttpGet("quality")]
    [HasPermission(OrmePermissions.Report.View)]
    public async Task<ActionResult<ApiResponse<OrmeQualityReportDto>>> GetQuality([FromQuery] OrmeQualityReportQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetQualityAsync(query, cancellationToken), HttpContext));

    /// <summary>Günlük sevk: gün ve müşteri bazında onaylı irsaliyeler, Boyahane'ye gönderilen çeki listeleri ayrı satırda.</summary>
    [HttpGet("daily-shipments")]
    [HasPermission(OrmePermissions.Report.View)]
    public async Task<ActionResult<ApiResponse<OrmeDailyShipmentReportDto>>> GetDailyShipments([FromQuery] OrmeShipmentReportQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetDailyShipmentsAsync(query, cancellationToken), HttpContext));

    /// <summary>Hareketsiz stok: belirtilen gündür hareketi olmayan iplik lotları ve sevk edilmemiş ham toplar.</summary>
    [HttpGet("idle-stock")]
    [HasPermission(OrmePermissions.Report.View)]
    public async Task<ActionResult<ApiResponse<OrmeIdleStockReportDto>>> GetIdleStock([FromQuery] OrmeIdleStockQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetIdleStockAsync(query, cancellationToken), HttpContext));
}

[Route("api/v1/orme-shifts")]
[ResourcePermissions(OrmePermissions.Floor.View, OrmePermissions.Floor.Manage)]
public sealed class OrmeShiftsController(IMasterDataService<OrmeShiftDto, OrmeShiftRequest> service)
    : MasterDataController<OrmeShiftDto, OrmeShiftRequest>(service);

[Route("api/v1/orme-break-groups")]
[ResourcePermissions(OrmePermissions.Floor.View, OrmePermissions.Floor.Manage)]
public sealed class OrmeBreakGroupsController(IMasterDataService<OrmeBreakGroupDto, OrmeBreakGroupRequest> service)
    : MasterDataController<OrmeBreakGroupDto, OrmeBreakGroupRequest>(service);

[Route("api/v1/orme-break-schedules")]
[ResourcePermissions(OrmePermissions.Floor.View, OrmePermissions.Floor.Manage)]
public sealed class OrmeBreakSchedulesController(IMasterDataService<OrmeBreakScheduleDto, OrmeBreakScheduleRequest> service)
    : MasterDataController<OrmeBreakScheduleDto, OrmeBreakScheduleRequest>(service);
