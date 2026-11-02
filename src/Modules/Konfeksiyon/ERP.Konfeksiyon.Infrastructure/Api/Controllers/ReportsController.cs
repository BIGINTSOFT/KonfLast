using ERP.Konfeksiyon.Domain.Authorization;
using ERP.Core.Infrastructure.Web.Authorization;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Application.Common;
using ERP.Konfeksiyon.Application.Features.Reports;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Konfeksiyon.Infrastructure.Api.Controllers;

[Route("api/v1/reports")]
[HasPermission(KonfeksiyonPermissions.Report.View, KonfeksiyonPermissions.Report.Warehouse)]
public sealed class ReportsController(IReportService service, IEnumerable<IReportExporter> exporters) : ApiControllerBase
{
    /// <summary>
    /// Kullanıcının çalıştırabileceği raporlar ve istedikleri parametreler. Yalnızca depo raporu yetkisi
    /// (Report.Warehouse) olan kullanıcı "Depo" grubundaki raporları görür ve çalıştırabilir.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ReportDefinition>>>> GetCatalog(CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await service.GetCatalogAsync(cancellationToken), HttpContext));

    /// <summary>
    /// Raporu çalıştırır: sütun tanımları, satırlar (sütun sırasıyla hücre değerleri) ve varsa toplam satırı.
    /// Tarih aralığı UTC'dir (from dahil, to hariç); süreler dakika, yüzdeler 0-100 ölçeğindedir.
    /// </summary>
    [HttpGet("{key}")]
    public async Task<ActionResult<ApiResponse<ReportTable>>> Run(
        string key, [FromQuery] ReportQuery query, CancellationToken cancellationToken) =>
        FromResult(await service.RunAsync(key, query, cancellationToken));

    /// <summary>Raporu dosya olarak indirir. format: csv (Excel'in Türkçe ayarlarıyla açılır).</summary>
    [HttpGet("{key}/export")]
    [HasPermission(KonfeksiyonPermissions.Report.Export)]
    [Produces("text/csv", "application/json")]
    public async Task<IActionResult> Export(
        string key, [FromQuery] ReportQuery query, [FromQuery] string format = "csv",
        CancellationToken cancellationToken = default)
    {
        var exporter = exporters.FirstOrDefault(e => string.Equals(e.Format, format, StringComparison.OrdinalIgnoreCase));
        if (exporter is null)
        {
            var message = $"Desteklenmeyen biçim. Kullanılabilir: {string.Join(", ", exporters.Select(e => e.Format))}.";
            return BadRequest(ApiResponse.Failure(HttpContext, new Error(
                ErrorCodes.ValidationError, message, ErrorType.Validation,
                new Dictionary<string, string[]> { ["format"] = [message] })));
        }

        var result = await service.RunAsync(key, query, cancellationToken);
        if (!result.IsSuccess)
        {
            return StatusCode(result.Error!.Type.ToStatusCode(), ApiResponse.Failure(HttpContext, result.Error));
        }

        return File(exporter.Export(result.Value), exporter.ContentType, $"{result.Value.Key}.{exporter.FileExtension}");
    }
}
