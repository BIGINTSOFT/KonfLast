using ERP.Konfeksiyon.Domain.Authorization;
using ERP.Core.Infrastructure.Web.Authorization;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Application.Features.MasterData;
using ERP.Konfeksiyon.Application.Features.Rfid;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Konfeksiyon.Infrastructure.Api.Controllers;

[Route("api/v1/rfid-cards")]
[ResourcePermissions(KonfeksiyonPermissions.RfidCard.View, KonfeksiyonPermissions.RfidCard.Manage)]
public sealed class RfidCardsController(IMasterDataService<RfidCardDto, RfidCardRequest> service)
    : MasterDataController<RfidCardDto, RfidCardRequest>(service);

/// <summary>Kartların demetlere ve operatörlere atanması.</summary>
[Route("api/v1")]
public sealed class RfidAssignmentsController(IRfidAssignmentService service) : ApiControllerBase
{
    /// <summary>Demet kartını demete atar; demet üretime hazır (Bekliyor) olur.</summary>
    [HttpPost("bundles/{bundleId:int}/rfid-card")]
    [HasPermission(KonfeksiyonPermissions.Bundle.Create)]
    public async Task<ActionResult<ApiResponse<object?>>> AssignToBundle(
        int bundleId, AssignCardRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.AssignToBundleAsync(bundleId, request.Uid, cancellationToken));

    /// <summary>Demetin kartını bırakır; kart başka bir demete atanabilir.</summary>
    [HttpDelete("bundles/{bundleId:int}/rfid-card")]
    [HasPermission(KonfeksiyonPermissions.Bundle.ReleaseCard)]
    public async Task<ActionResult<ApiResponse<object?>>> ReleaseFromBundle(
        int bundleId, CancellationToken cancellationToken) =>
        FromResult(await service.ReleaseFromBundleAsync(bundleId, cancellationToken));

    /// <summary>Okutulan demet kartını ve bağlı olduğu demeti döner. Hatalar: RFID_NOT_REGISTERED,
    /// RFID_CARD_TYPE_MISMATCH.</summary>
    [HttpGet("bundle-cards/{uid}")]
    [HasPermission(KonfeksiyonPermissions.Bundle.View)]
    public async Task<ActionResult<ApiResponse<BundleCardDto>>> GetBundleCard(
        string uid, CancellationToken cancellationToken) =>
        FromResult(await service.GetBundleCardAsync(uid, cancellationToken));

    /// <summary>Kart boşaltma: okutulan kartın demetle bağını kaldırır; demet yarım kalmış olabilir.
    /// Hatalar: RFID_NOT_REGISTERED, RFID_CARD_TYPE_MISMATCH, RFID_CARD_NOT_ASSIGNED.</summary>
    [HttpPost("bundle-cards/{uid}/release")]
    [HasPermission(KonfeksiyonPermissions.Bundle.ReleaseCard)]
    public async Task<ActionResult<ApiResponse<BundleCardReleaseDto>>> ReleaseBundleCard(
        string uid, CancellationToken cancellationToken) =>
        FromResult(await service.ReleaseBundleCardAsync(uid, cancellationToken));

    /// <summary>Personel kartını operatöre atar.</summary>
    [HttpPost("operators/{operatorId:int}/rfid-card")]
    [HasPermission(KonfeksiyonPermissions.Operator.AssignCard)]
    public async Task<ActionResult<ApiResponse<object?>>> AssignToOperator(
        int operatorId, AssignCardRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.AssignToOperatorAsync(operatorId, request.Uid, cancellationToken));

    /// <summary>Operatörün personel kartını geri alır.</summary>
    [HttpDelete("operators/{operatorId:int}/rfid-card")]
    [HasPermission(KonfeksiyonPermissions.Operator.AssignCard)]
    public async Task<ActionResult<ApiResponse<object?>>> ReleaseFromOperator(
        int operatorId, CancellationToken cancellationToken) =>
        FromResult(await service.ReleaseFromOperatorAsync(operatorId, cancellationToken));
}

/// <summary>
/// Tabletten gelen kart okutmaları (tablet token'ı gerekir). Her okutma bir kez işlenir: aynı
/// <c>readId</c> ya da kısa süre içinde aynı kartın tekrarı önceki sonucu <c>duplicate: true</c> ile döner.
/// </summary>
[Route("api/v1/rfid")]
[Authorize(Policy = AuthorizationPolicies.TabletDevice)]
public sealed class RfidReadsController(IRfidReadService service) : ApiControllerBase
{
    /// <summary>Personel kartını operatöre çözer. Hatalar: RFID_NOT_REGISTERED, RFID_CARD_INACTIVE,
    /// RFID_CARD_TYPE_MISMATCH, RFID_CARD_NOT_ASSIGNED, OPERATOR_INACTIVE.</summary>
    [HttpPost("operator/read")]
    public async Task<ActionResult<ApiResponse<OperatorReadDto>>> ReadOperator(
        RfidReadRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.ReadOperatorAsync(request, cancellationToken));

    /// <summary>Demet kartını demete çözer ve bu makinenin operasyonuna ait adımı döner. Hatalar:
    /// RFID_NOT_REGISTERED, RFID_CARD_INACTIVE, RFID_CARD_TYPE_MISMATCH, RFID_CARD_NOT_ASSIGNED, BUNDLE_NOT_ACTIVE.</summary>
    [HttpPost("bundle/read")]
    public async Task<ActionResult<ApiResponse<BundleReadDto>>> ReadBundle(
        RfidReadRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.ReadBundleAsync(request, cancellationToken));
}
