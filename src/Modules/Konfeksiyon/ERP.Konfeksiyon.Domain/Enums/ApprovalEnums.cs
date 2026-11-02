namespace ERP.Konfeksiyon.Domain.Enums;

/// <summary>Onay akışı tanımlanabilen belge türleri.</summary>
public enum ApprovalDocumentType
{
    SalesOrder,
}

public enum ApproverType
{
    /// <summary>Roldeki herhangi bir kullanıcı onaylar.</summary>
    Role,

    /// <summary>Seçili kişilerden herhangi biri onaylar.</summary>
    Users,
}

public enum ApprovalStepStatus
{
    /// <summary>Önceki adımın onayını bekliyor.</summary>
    Waiting,

    /// <summary>Kararı bekleniyor (şu anki adım).</summary>
    Pending,

    Approved,
    Rejected,

    /// <summary>Tur kapandı (ret ya da revizyon) ve adıma gelinmedi.</summary>
    Cancelled,
}

public enum ApprovalEventType
{
    Submitted,
    Approved,
    Rejected,
    Revised,
    Cancelled,
}
