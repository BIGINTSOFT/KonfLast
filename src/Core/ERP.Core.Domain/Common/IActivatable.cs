namespace ERP.Core.Domain.Common;

/// <summary>Pasif kayıtlar korunur ama yeni işlemlerde seçilemez.</summary>
public interface IActivatable
{
    bool IsActive { get; set; }
}
