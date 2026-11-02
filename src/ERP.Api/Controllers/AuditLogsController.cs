using ERP.Core.Infrastructure.Web.Authorization;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.AuditLogs;
using ERP.Core.Domain.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Api.Controllers;

[Route("api/v1/audit-logs")]
public sealed class AuditLogsController(IAuditLogService auditLogService) : ApiControllerBase
{
    /// <summary>İşlem kayıtları, en yeniden eskiye (sayfalı; kayıt, kullanıcı, işlem ve tarih filtresi).</summary>
    [HttpGet]
    [HasPermission(Permissions.AuditLog.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<AuditLogDto>>>> GetAuditLogs(
        [FromQuery] AuditLogQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse.Success(await auditLogService.GetAsync(query, cancellationToken), HttpContext));
}
