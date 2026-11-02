using ERP.Core.Domain.Common;

namespace ERP.Konfeksiyon.Domain.Entities;

public class Operation : SoftDeletableEntity, IActivatable
{
    public string OperationCode { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    /// <summary>Standart süre (dakika / adet).</summary>
    public decimal Sam { get; set; }

    /// <summary>Parça başı birim ücret (adet başına); boşsa ücret SAM × dakika ücretinden hesaplanır.</summary>
    public decimal? PieceRate { get; set; }

    /// <summary>Operasyonun gerektirdiği makine tipi (varsa).</summary>
    public int? MachineTypeId { get; set; }
    public MachineType? MachineType { get; set; }
    public bool IsActive { get; set; } = true;
}
