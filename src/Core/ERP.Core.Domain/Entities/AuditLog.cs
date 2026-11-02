using ERP.Core.Domain.Common;
using ERP.Core.Domain.Enums;

namespace ERP.Core.Domain.Entities;

/// <summary>
/// Kim, neyi, ne zaman, hangi cihazdan değiştirdi; eski ve yeni değer neydi.
/// Kayıtlar yalnızca eklenir; güncellenmez ve silinmez.
/// </summary>
[NotAudited]
public class AuditLog
{
    public long Id { get; set; }

    public string EntityName { get; set; } = null!;

    /// <summary>Birincil anahtar; bileşik anahtarlarda virgülle ayrılır.</summary>
    public string EntityId { get; set; } = null!;

    public AuditAction Action { get; set; }

    /// <summary>Değişen alanların önceki değerleri (JSON).</summary>
    public string? OldValues { get; set; }

    /// <summary>Değişen alanların yeni değerleri (JSON).</summary>
    public string? NewValues { get; set; }

    public int? UserId { get; set; }
    public string UserName { get; set; } = null!;
    public string? IpAddress { get; set; }
    public string? DeviceInfo { get; set; }
    public string? CorrelationId { get; set; }
    public DateTime CreatedAt { get; set; }
}
