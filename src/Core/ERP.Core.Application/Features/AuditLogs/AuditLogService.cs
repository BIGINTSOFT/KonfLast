using System.Text.Json;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERP.Core.Application.Features.AuditLogs;

public sealed record AuditLogDto(
    long Id,
    string EntityName,
    string EntityId,
    AuditAction Action,
    JsonElement? OldValues,
    JsonElement? NewValues,
    int? UserId,
    string UserName,
    string? IpAddress,
    string? DeviceInfo,
    string? CorrelationId,
    DateTime CreatedAt);

public sealed class AuditLogQuery : PageRequest
{
    public string? EntityName { get; set; }
    public string? EntityId { get; set; }
    public string? UserName { get; set; }
    public AuditAction? Action { get; set; }

    /// <summary>Başlangıç zamanı (UTC, dahil).</summary>
    public DateTime? From { get; set; }

    /// <summary>Bitiş zamanı (UTC, hariç).</summary>
    public DateTime? To { get; set; }
}

public interface IAuditLogService
{
    Task<PagedResult<AuditLogDto>> GetAsync(AuditLogQuery query, CancellationToken cancellationToken);
}

public sealed class AuditLogService(IAppDbContext db) : IAuditLogService
{
    public async Task<PagedResult<AuditLogDto>> GetAsync(AuditLogQuery query, CancellationToken cancellationToken)
    {
        var logs = db.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.EntityName))
        {
            logs = logs.Where(l => l.EntityName == query.EntityName);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityId))
        {
            logs = logs.Where(l => l.EntityId == query.EntityId);
        }

        if (!string.IsNullOrWhiteSpace(query.UserName))
        {
            logs = logs.Where(l => l.UserName == query.UserName);
        }

        if (query.Action is { } action)
        {
            logs = logs.Where(l => l.Action == action);
        }

        if (query.From is { } from)
        {
            var fromUtc = from.ToUniversalTime();
            logs = logs.Where(l => l.CreatedAt >= fromUtc);
        }

        if (query.To is { } to)
        {
            var toUtc = to.ToUniversalTime();
            logs = logs.Where(l => l.CreatedAt < toUtc);
        }

        // Id artan sırada üretildiği için en yeni kayıt en büyük Id'dir (clustered index'i kullanır).
        var page = await logs.OrderByDescending(l => l.Id).ToPagedResultAsync(query, cancellationToken);

        var items = page.Items
            .Select(l => new AuditLogDto(
                l.Id, l.EntityName, l.EntityId, l.Action, Parse(l.OldValues), Parse(l.NewValues),
                l.UserId, l.UserName, l.IpAddress, l.DeviceInfo, l.CorrelationId, l.CreatedAt))
            .ToList();

        return new PagedResult<AuditLogDto>(items, page.Page, page.PageSize, page.TotalCount);
    }

    private static JsonElement? Parse(string? json) =>
        json is null ? null : JsonSerializer.Deserialize<JsonElement>(json);
}
