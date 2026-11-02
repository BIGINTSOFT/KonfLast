using ERP.Boyahane.Domain.Enums;
using ERP.Core.Domain.Common;

namespace ERP.Boyahane.Domain.Entities;

/// <summary>Boyahane operasyonu. Rotalar operasyonlardan kurulur; davranış adımın tablette ne istediğini belirler.</summary>
public class BoyahaneOperation : SoftDeletableEntity, IActivatable
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    public OperationBehavior Behavior { get; set; } = OperationBehavior.Standard;

    /// <summary>Makinede yapılır mı? Değilse kontrol noktasından da yürütülür.</summary>
    public bool NeedsMachine { get; set; } = true;

    /// <summary>Standart süre (dakika); rapordaki karşılaştırma için.</summary>
    public decimal? StandardMinutes { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>Makinenin yapabildiği operasyon.</summary>
[NotAudited]
public class BoyahaneMachineOperation
{
    public int MachineId { get; set; }
    public BoyahaneMachine Machine { get; set; } = null!;

    public int OperationId { get; set; }
    public BoyahaneOperation Operation { get; set; } = null!;
}

/// <summary>
/// Operasyon rotası: bir işlem türünün adımları. İşlem türünün varsayılan rotası partiye kendiliğinden gelir, parti açılırken
/// başka rota seçilebilir. Adımlar partiye kopyalanır; rota sonradan değişse açılmış parti etkilenmez.
/// </summary>
public class BoyahaneRoute : SoftDeletableEntity, IActivatable
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public OrderProcessType ProcessType { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<BoyahaneRouteStep> Steps { get; } = [];
}

public class BoyahaneRouteStep : AuditableEntity
{
    public int RouteId { get; set; }
    public BoyahaneRoute Route { get; set; } = null!;

    public int Sequence { get; set; }

    public int OperationId { get; set; }
    public BoyahaneOperation Operation { get; set; } = null!;
}
