using ERP.Boyahane.Application.Features.Batches;
using ERP.Boyahane.Domain.Authorization;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Infrastructure.Web.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Boyahane.Infrastructure.Api.Controllers;

[Route("api/v1/boyahane-machines")]
[ResourcePermissions(BoyahanePermissions.Batch.View, BoyahanePermissions.Batch.Manage)]
public sealed class BoyahaneMachinesController(IMasterDataService<BoyahaneMachineDto, BoyahaneMachineRequest> service)
    : MasterDataController<BoyahaneMachineDto, BoyahaneMachineRequest>(service);

/// <summary>Boya partileri: uygun toplardan, onaylı reçeteyle. Reçete parti kilosu ve flotteye göre ölçeklenir.</summary>
[Route("api/v1/boyahane-batches")]
public sealed class BoyahaneBatchesController(IBoyahaneBatchService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(BoyahanePermissions.Batch.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<BoyahaneBatchListItemDto>>>> GetList(
        [FromQuery] BoyahaneBatchQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetListAsync(query, cancellationToken), HttpContext));

    /// <summary>Makine planı: planlanan bütün partiler, sayfasız.</summary>
    [HttpGet("plan")]
    [HasPermission(BoyahanePermissions.Batch.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BoyahaneBatchListItemDto>>>> GetPlan(CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetPlanAsync(cancellationToken), HttpContext));

    /// <summary>Partiye alınabilecek mal; aynı grup anahtarını taşıyanlar birlikte boyanabilir. Düzenlemede partinin kendi malı da gelir.</summary>
    [HttpGet("candidates")]
    [HasPermission(BoyahanePermissions.Batch.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BoyahaneBatchCandidateDto>>>> GetCandidates(int? batchId, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetCandidatesAsync(batchId, cancellationToken), HttpContext));

    [HttpGet("{id:int}")]
    [HasPermission(BoyahanePermissions.Batch.View)]
    public async Task<ActionResult<ApiResponse<BoyahaneBatchDto>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetAsync(id, cancellationToken));

    /// <summary>Kapasite aşılıyorsa BATCH_OVER_CAPACITY döner; yetkili gerekçe yazarak devam eder.</summary>
    [HttpPost]
    [HasPermission(BoyahanePermissions.Batch.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneBatchDto>>> Create(BoyahaneBatchRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, ApiResponse.Success(result.Value, HttpContext))
            : FromResult(result);
    }

    [HttpPut("{id:int}")]
    [HasPermission(BoyahanePermissions.Batch.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneBatchDto>>> Update(int id, BoyahaneBatchRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.UpdateAsync(id, request, cancellationToken));

    /// <summary>Okutulan kartın açık partisi (kart boşaltma ekranı).</summary>
    [HttpGet("by-card/{uid}")]
    [HasPermission(BoyahanePermissions.Batch.View)]
    public async Task<ActionResult<ApiResponse<BoyahaneBatchDto>>> GetByCard(string uid, CancellationToken cancellationToken) =>
        FromResult(await service.FindByCardAsync(uid, cancellationToken));

    /// <summary>RFID kartı atar; boş numara kartı çıkarır.</summary>
    [HttpPut("{id:int}/card")]
    [HasPermission(BoyahanePermissions.Batch.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneBatchDto>>> SetCard(int id, SetBatchCardRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SetCardAsync(id, request, cancellationToken));

    /// <summary>Ofisten elle bildirim: tablet arızalıysa ya da tablet kullanılmıyorsa adımı başlatır.</summary>
    // Yetki adıma göre serviste denetlenir: final kalite ayrı yetkiyle, diğerleri parti yönetimiyle.
    [HttpPost("{id:int}/steps/{stepId:int}/start")]
    [HasPermission(BoyahanePermissions.Batch.View)]
    public async Task<ActionResult<ApiResponse<BoyahaneBatchDto>>> StartStep(int id, int stepId, StartBatchStepRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.StartStepAsync(id, stepId, new BatchStepActor(null, null, request.MachineId), null, cancellationToken));

    // Yetki adıma göre serviste denetlenir: final kalite ayrı yetkiyle, diğerleri parti yönetimiyle.
    [HttpPost("{id:int}/steps/{stepId:int}/complete")]
    [HasPermission(BoyahanePermissions.Batch.View)]
    public async Task<ActionResult<ApiResponse<BoyahaneBatchDto>>> CompleteStep(int id, int stepId, CompleteBatchStepRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.CompleteStepAsync(id, stepId, request, new BatchStepActor(null, null, null), cancellationToken));

    /// <summary>Parti başlamadan adımı kaldırır ya da geri alır.</summary>
    [HttpPost("{id:int}/steps/{stepId:int}/skip")]
    [HasPermission(BoyahanePermissions.Batch.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneBatchDto>>> SkipStep(int id, int stepId, CancellationToken cancellationToken) =>
        FromResult(await service.SkipStepAsync(id, stepId, cancellationToken));

    /// <summary>Planlanan partiyi iptal eder; mal boşa çıkar.</summary>
    [HttpPost("{id:int}/cancel")]
    [HasPermission(BoyahanePermissions.Batch.Manage)]
    public async Task<ActionResult<ApiResponse<BoyahaneBatchDto>>> Cancel(int id, CancellationToken cancellationToken) =>
        FromResult(await service.CancelAsync(id, cancellationToken));
}
