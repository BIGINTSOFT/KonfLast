using ERP.Boyahane.Domain.Enums;
using ERP.Core.Domain.Common;

namespace ERP.Boyahane.Domain.Entities;

/// <summary>Ham kontrolde ve sonraki kalite adımlarında kullanılan hata türü.</summary>
public class BoyahaneDefectType : SoftDeletableEntity, IActivatable
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public DefectCategory Category { get; set; } = DefectCategory.Appearance;
    public bool IsActive { get; set; } = true;
}

/// <summary>Topta bulunan hata. Müşteriyle anlaşmazlıkta kanıttır; müşteri kararı girildikten sonra değiştirilmez.</summary>
public class BoyahaneFabricRollDefect : AuditableEntity
{
    public int RollId { get; set; }
    public BoyahaneFabricRoll Roll { get; set; } = null!;

    public int DefectTypeId { get; set; }
    public BoyahaneDefectType DefectType { get; set; } = null!;

    public int Quantity { get; set; } = 1;

    /// <summary>Hatanın topun kaçıncı metresinde olduğu; biliniyorsa.</summary>
    public decimal? AtMeter { get; set; }

    public string? Note { get; set; }
}
