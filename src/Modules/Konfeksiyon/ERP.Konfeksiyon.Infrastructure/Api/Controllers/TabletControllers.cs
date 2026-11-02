using ERP.Konfeksiyon.Domain.Authorization;
using ERP.Core.Infrastructure.Web.Authorization;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Konfeksiyon.Application.Features.Tablets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ERP.Konfeksiyon.Infrastructure.Api.Controllers;

[Route("api/v1/tablets")]
[ResourcePermissions(KonfeksiyonPermissions.Tablet.View, KonfeksiyonPermissions.Tablet.Manage)]
public sealed class TabletsController(IMasterDataService<TabletDto, TabletRequest> service)
    : MasterDataController<TabletDto, TabletRequest>(service);

/// <summary>Tablet cihaz eşleştirmesinin yönetici tarafı.</summary>
[Route("api/v1/tablets/{id:int}")]
public sealed class TabletDeviceAdminController(ITabletDeviceService service) : ApiControllerBase
{
    /// <summary>
    /// Eşleştirilmiş cihazın bağını koparır (kayıp / değişen tablet); cihaz artık token alamaz ve tablet
    /// yeni bir cihazla eşleştirilebilir.
    /// </summary>
    [HttpPost("reset-device")]
    [HasPermission(KonfeksiyonPermissions.Tablet.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> ResetDevice(int id, CancellationToken cancellationToken) =>
        FromResult(await service.ResetDeviceAsync(id, cancellationToken));
}

/// <summary>Tablet cihazının kendini tanıtması. Kullanıcı oturumu gerektirmez; cihaz kendi kimliğiyle gelir.</summary>
[Route("api/v1/tablet-auth")]
[AllowAnonymous]
[EnableRateLimiting(RateLimitPolicies.Auth)]
public sealed class TabletAuthController(ITabletDeviceService service) : ApiControllerBase
{
    /// <summary>Tablet ayarlarında seçilebilecek makineler; tablet ayarları parolası gerekir.</summary>
    [HttpPost("pairing/stations")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TabletPairingStationDto>>>> PairingStations(
        TabletPairingListRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.GetPairingStationsAsync(request, cancellationToken));

    /// <summary>Cihazı seçilen makineye bağlar ve cihaz anahtarını döner (yalnızca bir kez).</summary>
    [HttpPost("pairing/pair")]
    public async Task<ActionResult<ApiResponse<PairTabletResponse>>> Pair(
        PairTabletRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.PairAsync(request, cancellationToken));

    /// <summary>Cihaz anahtarı ile kısa ömürlü tablet token'ı alır; süresi dolunca yeniden çağrılır.</summary>
    [HttpPost("token")]
    public async Task<ActionResult<ApiResponse<TabletTokenResponse>>> Token(
        TabletTokenRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.IssueTokenAsync(request, cancellationToken));
}

/// <summary>Tabletin kendi adına çağırdığı uçlar (tablet token'ı gerekir; kullanıcı token'ı kabul edilmez).</summary>
[Route("api/v1/tablet")]
[Authorize(Policy = AuthorizationPolicies.TabletDevice)]
public sealed class TabletDeviceController(ITabletDeviceService deviceService, IMachineStatusService machineStatus)
    : ApiControllerBase
{
    /// <summary>Tabletin bağlamı: makine, operasyon, bant, bantın şu anki planı ve sunucu saati.</summary>
    [HttpGet("context")]
    public async Task<ActionResult<ApiResponse<TabletContextDto>>> GetContext(CancellationToken cancellationToken) =>
        FromResult(await deviceService.GetContextAsync(cancellationToken));

    /// <summary>Tabletin çevrimiçi olduğunu bildirir (periyodik çağrılır).</summary>
    [HttpPost("heartbeat")]
    public async Task<ActionResult<ApiResponse<object?>>> Heartbeat(CancellationToken cancellationToken) =>
        FromResult(await deviceService.HeartbeatAsync(cancellationToken));

    /// <summary>Tabletin bağlı olduğu makine için arıza bildirir.</summary>
    [HttpPost("machine/breakdown")]
    public async Task<ActionResult<ApiResponse<MachineStatusDto>>> ReportBreakdown(
        ReportBreakdownRequest request, CancellationToken cancellationToken) =>
        FromResult(await machineStatus.ReportBreakdownFromTabletAsync(request, cancellationToken));

    /// <summary>
    /// Makinenin arızadan çıktığını bildirir (yalnızca arıza durumundayken). Bakım operatörünün ya da bant
    /// şefinin kartı okutulmalıdır: AUTHORIZATION_CARD_REQUIRED, NOT_AUTHORIZED_FOR_ACTION.
    /// </summary>
    [HttpPost("machine/recover")]
    public async Task<ActionResult<ApiResponse<MachineStatusDto>>> ReportRecovery(
        ReportRecoveryRequest request, CancellationToken cancellationToken) =>
        FromResult(await machineStatus.ReportRecoveryFromTabletAsync(request, cancellationToken));
}

/// <summary>Makine durumu (arıza, bakım, müsait, çevrimdışı) ve durum geçmişi.</summary>
[Route("api/v1/machines/{machineId:int}/status")]
public sealed class MachineStatusController(IMachineStatusService service) : ApiControllerBase
{
    /// <summary>Makinenin durumu, geçirilebileceği durumlar ve son durum kayıtları (süreleriyle).</summary>
    [HttpGet]
    [HasPermission(KonfeksiyonPermissions.Machine.View)]
    public async Task<ActionResult<ApiResponse<MachineStatusDto>>> Get(
        int machineId, CancellationToken cancellationToken) =>
        FromResult(await service.GetAsync(machineId, cancellationToken));

    /// <summary>Makine durumunu değiştirir. Çalışıyor / Boşta durumlarını yalnızca üretim akışı belirler.</summary>
    [HttpPost]
    [HasPermission(KonfeksiyonPermissions.Machine.Update)]
    public async Task<ActionResult<ApiResponse<MachineStatusDto>>> Change(
        int machineId, ChangeMachineStatusRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.ChangeAsync(machineId, request, cancellationToken));
}
