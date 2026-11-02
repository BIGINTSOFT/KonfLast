namespace ERP.Konfeksiyon.Domain.Enums;

/// <summary>Veritabanında ve API'de BÜYÜK_SNAKE_CASE saklanır (AVAILABLE, BREAKDOWN ...).</summary>
public enum MachineStatus
{
    Available,
    Running,
    Idle,
    Breakdown,
    Maintenance,
    Offline,
}
