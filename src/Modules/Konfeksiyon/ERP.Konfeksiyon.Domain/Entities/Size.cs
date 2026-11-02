using ERP.Core.Domain.Common;

namespace ERP.Konfeksiyon.Domain.Entities;

public class Size : SoftDeletableEntity, IActivatable
{
    public string SizeCode { get; set; } = null!;
    public string Name { get; set; } = null!;

    /// <summary>Beden sıralaması (S, M, L ... alfabetik sıralanamaz).</summary>
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
