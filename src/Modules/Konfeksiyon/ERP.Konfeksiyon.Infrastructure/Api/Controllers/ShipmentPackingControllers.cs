using ERP.Konfeksiyon.Domain.Authorization;
using ERP.Core.Infrastructure.Web.Authorization;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Application.Common;
using ERP.Konfeksiyon.Application.Features.Shipping;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Konfeksiyon.Infrastructure.Api.Controllers;

/// <summary>
/// Sevkiyatın kolileri (çeki listesi), basılı belgeleri ve el terminalinden kontrollü yükleme. Koliler yalnızca
/// taslak sevkiyatta değiştirilir; her değişiklik sevkiyatın rowVersion'ını ilerletir.
/// </summary>
[Route("api/v1/shipments")]
public sealed class ShipmentPackingController(IShipmentPackingService service) : ApiControllerBase
{
    /// <summary>Kalemler, kolilere girmiş adetler ve koliler.</summary>
    [HttpGet("{id:int}/cartons")]
    [HasPermission(KonfeksiyonPermissions.Shipment.View)]
    public async Task<ActionResult<ApiResponse<ShipmentPackingDto>>> GetCartons(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetAsync(id, cancellationToken));

    /// <summary>
    /// Kolileri asorti şablonuyla yeniden dağıtır: mevcut koliler silinir; her ürün-renk için şablon oranlarıyla dolu
    /// koliler, kalan adetlerle renk başına bir karışık koli oluşturulur.
    /// </summary>
    [HttpPost("{id:int}/cartons/distribute")]
    [HasPermission(KonfeksiyonPermissions.Shipment.Manage)]
    public async Task<ActionResult<ApiResponse<ShipmentPackingDto>>> Distribute(
        int id, CartonDistributionRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.DistributeAsync(id, request, cancellationToken));

    /// <summary>Sona yeni koli ekler.</summary>
    [HttpPost("{id:int}/cartons")]
    [HasPermission(KonfeksiyonPermissions.Shipment.Manage)]
    public async Task<ActionResult<ApiResponse<ShipmentPackingDto>>> AddCarton(
        int id, ShipmentCartonRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SaveCartonAsync(id, null, request, cancellationToken));

    /// <summary>Kolinin içeriğini ve ağırlığını değiştirir; gönderilmeyen satırlar silinir.</summary>
    [HttpPut("{id:int}/cartons/{cartonId:int}")]
    [HasPermission(KonfeksiyonPermissions.Shipment.Manage)]
    public async Task<ActionResult<ApiResponse<ShipmentPackingDto>>> UpdateCarton(
        int id, int cartonId, ShipmentCartonRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SaveCartonAsync(id, cartonId, request, cancellationToken));

    /// <summary>Koliyi siler; sonraki kolilerin numarası bir azalır (rowVersion sorgu dizgisinde).</summary>
    [HttpDelete("{id:int}/cartons/{cartonId:int}")]
    [HasPermission(KonfeksiyonPermissions.Shipment.Manage)]
    public async Task<ActionResult<ApiResponse<ShipmentPackingDto>>> DeleteCarton(
        int id, int cartonId, [FromQuery] string rowVersion, CancellationToken cancellationToken) =>
        FromResult(await service.DeleteCartonAsync(id, cartonId, rowVersion, cancellationToken));

    /// <summary>Bütün kolileri siler.</summary>
    [HttpPost("{id:int}/cartons/clear")]
    [HasPermission(KonfeksiyonPermissions.Shipment.Manage)]
    public async Task<ActionResult<ApiResponse<ShipmentPackingDto>>> ClearCartons(
        int id, ShipmentPackingVersionRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.ClearAsync(id, request.RowVersion, cancellationToken));

    /// <summary>Seçilen kolilere aynı net / brüt ağırlığı yazar.</summary>
    [HttpPost("{id:int}/cartons/weights")]
    [HasPermission(KonfeksiyonPermissions.Shipment.Manage)]
    public async Task<ActionResult<ApiResponse<ShipmentPackingDto>>> SetWeights(
        int id, ShipmentCartonWeightsRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.SetWeightsAsync(id, request, cancellationToken));

    /// <summary>Çeki listesi, koli etiketi ve irsaliye çıktısının verisi (müşterinin belge diliyle).</summary>
    [HttpGet("{id:int}/document")]
    [HasPermission(KonfeksiyonPermissions.Shipment.View)]
    public async Task<ActionResult<ApiResponse<ShipmentDocumentDto>>> GetDocument(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetDocumentAsync(id, cancellationToken));

    /// <summary>El terminalinde yüklenecek sevkiyatlar: kolisi olan taslak ya da sevk edilmiş sevkiyatlar.</summary>
    [HttpGet("loading/lookup")]
    [HasPermission(KonfeksiyonPermissions.Shipment.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LoadableShipmentDto>>>> LookupLoadable(
        [FromQuery] string? search, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetLoadableAsync(search, cancellationToken), HttpContext));

    /// <summary>Sevkiyat numarası ya da koli kodu okutularak sevkiyatın yükleme durumu.</summary>
    [HttpGet("loading/find")]
    [HasPermission(KonfeksiyonPermissions.Shipment.View)]
    public async Task<ActionResult<ApiResponse<ShipmentLoadingDto>>> FindLoading(
        [FromQuery] string code, CancellationToken cancellationToken) =>
        FromResult(await service.FindLoadingAsync(code, cancellationToken));

    /// <summary>Yükleme durumu: kaç koli okutuldu, hangileri eksik.</summary>
    [HttpGet("{id:int}/loading")]
    [HasPermission(KonfeksiyonPermissions.Shipment.View)]
    public async Task<ActionResult<ApiResponse<ShipmentLoadingDto>>> GetLoading(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetLoadingAsync(id, cancellationToken));

    /// <summary>
    /// Okutulan koliyi yüklendi olarak işaretler. Aynı koli yeniden okutulursa hata değildir (alreadyLoaded);
    /// başka sevkiyatın kolisi ya da tanınmayan kod reddedilir. Sevkiyatın rowVersion'ı değişmez.
    /// </summary>
    [HttpPost("{id:int}/loading/scan")]
    [HasPermission(KonfeksiyonPermissions.Shipment.Manage, KonfeksiyonPermissions.Shipment.Ship)]
    public async Task<ActionResult<ApiResponse<CartonLoadResultDto>>> Scan(
        int id, CartonLoadRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.LoadAsync(id, request.Code, cancellationToken));
}

/// <summary>Asorti şablonları: kolinin beden dağılımı; müşteriye özel ya da genel.</summary>
[Route("api/v1/assortment-templates")]
public sealed class AssortmentTemplatesController(IAssortmentTemplateService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(KonfeksiyonPermissions.Shipment.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<AssortmentTemplateListItemDto>>>> GetList(
        [FromQuery] AssortmentTemplateQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetListAsync(query, cancellationToken), HttpContext));

    /// <summary>Koli dağıtımında seçilebilecek aktif şablonlar (müşterininkiler ve genel şablonlar).</summary>
    [HttpGet("options")]
    [HasPermission(KonfeksiyonPermissions.Shipment.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AssortmentTemplateOptionDto>>>> GetOptions(
        [FromQuery] int? customerId, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetOptionsAsync(customerId, cancellationToken), HttpContext));

    [HttpGet("{id:int}")]
    [HasPermission(KonfeksiyonPermissions.Shipment.View)]
    public async Task<ActionResult<ApiResponse<AssortmentTemplateDto>>> Get(int id, CancellationToken cancellationToken) =>
        FromResult(await service.GetAsync(id, cancellationToken));

    [HttpPost]
    [HasPermission(KonfeksiyonPermissions.Shipment.Manage)]
    public async Task<ActionResult<ApiResponse<AssortmentTemplateDto>>> Create(
        AssortmentTemplateRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { id = result.Value.Id }, ApiResponse.Success(result.Value, HttpContext))
            : FromResult(result);
    }

    /// <summary>Başlığı ve bedenleri günceller; gönderilmeyen bedenler silinir. Dağıtılmış koliler değişmez.</summary>
    [HttpPut("{id:int}")]
    [HasPermission(KonfeksiyonPermissions.Shipment.Manage)]
    public async Task<ActionResult<ApiResponse<AssortmentTemplateDto>>> Update(
        int id, AssortmentTemplateRequest request, CancellationToken cancellationToken) =>
        FromResult(await service.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:int}")]
    [HasPermission(KonfeksiyonPermissions.Shipment.Manage)]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(int id, CancellationToken cancellationToken) =>
        FromResult(await service.DeleteAsync(id, cancellationToken));
}
