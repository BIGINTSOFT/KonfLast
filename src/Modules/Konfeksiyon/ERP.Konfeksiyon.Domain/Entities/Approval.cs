using ERP.Core.Domain.Common;
using ERP.Core.Domain.Entities.Identity;
using ERP.Konfeksiyon.Domain.Enums;

namespace ERP.Konfeksiyon.Domain.Entities;

/// <summary>
/// Bir belge türünün onay akışı: sıralı adımlar. Belge onaya gönderildiğinde akışın o anki hâli belgeye
/// kopyalanır; akış sonradan değişse de süren onaylar etkilenmez.
/// </summary>
public class ApprovalFlow : AuditableEntity
{
    public ApprovalDocumentType DocumentType { get; set; }
    public string Name { get; set; } = null!;

    /// <summary>Kapalıysa belge akışsız onaylanır (onay yetkisi olan herkes).</summary>
    public bool IsActive { get; set; } = true;

    public ICollection<ApprovalFlowStep> Steps { get; } = new List<ApprovalFlowStep>();
}

/// <summary>
/// Onay adımı. Rol atanmışsa roldeki herkes, kişiler atanmışsa bunlardan biri onaylayınca adım tamamlanır.
/// Tutar eşiği verilmişse adım yalnızca belge tutarı (yerel para birimine çevrilmiş) eşiğe ulaşınca uygulanır.
/// </summary>
public class ApprovalFlowStep : BaseEntity
{
    public int ApprovalFlowId { get; set; }
    public ApprovalFlow ApprovalFlow { get; set; } = null!;

    public int Sequence { get; set; }
    public string Name { get; set; } = null!;
    public ApproverType ApproverType { get; set; }

    public int? RoleId { get; set; }
    public Role? Role { get; set; }

    /// <summary>Yerel para birimi (TRY) cinsinden eşik; boşsa adım her belgede uygulanır.</summary>
    public decimal? MinAmount { get; set; }

    public ICollection<ApprovalFlowStepUser> Users { get; } = new List<ApprovalFlowStepUser>();
}

public class ApprovalFlowStepUser
{
    public int ApprovalFlowStepId { get; set; }
    public ApprovalFlowStep ApprovalFlowStep { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
}

/// <summary>
/// Müşteri siparişinin bir onaya gönderiminde (tur) uygulanan adım. Onaylayabilecekler gönderim anında kopyalanır.
/// </summary>
public class SalesOrderApprovalStep : BaseEntity
{
    public int SalesOrderId { get; set; }
    public SalesOrder SalesOrder { get; set; } = null!;

    /// <summary>Kaçıncı onaya gönderim (revizyon / ret sonrası yeniden gönderimde artar).</summary>
    public int Round { get; set; }

    public int Sequence { get; set; }
    public string Name { get; set; } = null!;
    public ApproverType ApproverType { get; set; }
    public int? RoleId { get; set; }

    /// <summary>Kişilere atanmış adımda onaylayabilecek kullanıcı Id'leri, ",3,7," biçiminde.</summary>
    public string? ApproverUserIds { get; set; }

    /// <summary>Ekranda gösterilen onaylayıcılar (rol adı ya da kişi adları).</summary>
    public string ApproverNames { get; set; } = null!;

    public ApprovalStepStatus Status { get; set; }
    public DateTime? DecidedAt { get; set; }
    public string? DecidedBy { get; set; }
    public string? Note { get; set; }

    public bool CanBeDecidedBy(int userId, IReadOnlyCollection<int> roleIds) => ApproverType switch
    {
        ApproverType.Role => RoleId is { } roleId && roleIds.Contains(roleId),
        ApproverType.Users => ApproverUserIds?.Contains($",{userId},", StringComparison.Ordinal) == true,
        _ => false,
    };
}

/// <summary>Sipariş onay geçmişi: kim, hangi aşamada, ne yaptı ve ne not yazdı.</summary>
public class SalesOrderApprovalEvent : BaseEntity
{
    public int SalesOrderId { get; set; }
    public SalesOrder SalesOrder { get; set; } = null!;

    public int Round { get; set; }
    public ApprovalEventType EventType { get; set; }

    /// <summary>İşlemin yapıldığı onay adımı (gönderim ve revizyonda boş).</summary>
    public string? StepName { get; set; }

    public string UserName { get; set; } = null!;
    public string? FullName { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Note { get; set; }
}
