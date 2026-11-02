using ERP.Boyahane.Domain.Enums;
using ERP.Core.Domain.Common;

namespace ERP.Boyahane.Domain.Entities;

/// <summary>Boya ya da kimyasal kartı. Reçetede boya kumaş ağırlığına göre %, kimyasal g/L ile yazılır.</summary>
public class BoyahaneDyeChemical : SoftDeletableEntity, IActivatable
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public DyeChemicalKind Kind { get; set; } = DyeChemicalKind.Dye;

    /// <summary>Kritik stok (kg); kullanılabilir ve yoldaki toplamı altına düşünce tedarik önerisine gelir. Boşsa izlenmez.</summary>
    public decimal? MinStockKilograms { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>Sipariş satırındaki renk için laboratuvar talebi. Satırda en fazla bir etkin talep olur.</summary>
public class BoyahaneLabRequest : AuditableEntity
{
    public string RequestNo { get; set; } = null!;

    public int OrderLineId { get; set; }
    public BoyahaneOrderLine OrderLine { get; set; } = null!;

    /// <summary>Hedef renk açıklaması (müşteri numunesi, ışık kaynağı vb.).</summary>
    public string? TargetNote { get; set; }

    public LabRequestStatus Status { get; set; } = LabRequestStatus.Open;

    public ICollection<BoyahaneLabTrial> Trials { get; } = [];
}

/// <summary>Lab dip denemesi. Müşterinin kabul ettiği deneme sipariş satırının reçetesi olur.</summary>
public class BoyahaneLabTrial : AuditableEntity
{
    public int RequestId { get; set; }
    public BoyahaneLabRequest Request { get; set; } = null!;

    /// <summary>Talep içinde sıra: A, B, C ...</summary>
    public string TrialCode { get; set; } = null!;
    public string? Notes { get; set; }

    public LabTrialDecision Decision { get; set; } = LabTrialDecision.Pending;
    public string? DecisionBy { get; set; }
    public CustomerDecisionChannel? DecisionChannel { get; set; }
    public DateOnly? DecisionOn { get; set; }
    public string? DecisionNote { get; set; }

    /// <summary>Kararı sisteme giren kullanıcı.</summary>
    public string? DecisionRecordedBy { get; set; }

    /// <summary>Laboratuvar atlanıp önceki reçete kullanıldıysa kaynak deneme.</summary>
    public int? ReusedFromTrialId { get; set; }
    public BoyahaneLabTrial? ReusedFromTrial { get; set; }

    public ICollection<BoyahaneLabTrialItem> Items { get; } = [];
}

/// <summary>Denemedeki boya ya da kimyasal ve miktarı (boyada %, kimyasalda g/L).</summary>
public class BoyahaneLabTrialItem : AuditableEntity
{
    public int TrialId { get; set; }
    public BoyahaneLabTrial Trial { get; set; } = null!;

    public int DyeChemicalId { get; set; }
    public BoyahaneDyeChemical DyeChemical { get; set; } = null!;

    public decimal Amount { get; set; }
}
