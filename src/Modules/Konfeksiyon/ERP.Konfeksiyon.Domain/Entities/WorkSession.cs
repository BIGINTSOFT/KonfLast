using ERP.Core.Domain.Common;
using ERP.Konfeksiyon.Domain.Enums;

namespace ERP.Konfeksiyon.Domain.Entities;

/// <summary>
/// Operatörün bir makine başındaki çalışma oturumu: kartını okutmasıyla açılır, çıkış yapmasıyla (ya da
/// makineyi başka operatörün devralmasıyla) kapanır. Kim, hangi makinede, ne zaman çalıştı sorusunun
/// kaynağıdır; kayıtlar silinmez. Bir operatörün ve bir makinenin aynı anda tek açık oturumu olur.
/// </summary>
[NotAudited]
public class WorkSession
{
    public long Id { get; set; }

    public int OperatorId { get; set; }
    public Operator Operator { get; set; } = null!;

    public int MachineId { get; set; }
    public Machine Machine { get; set; } = null!;

    /// <summary>Oturumun açıldığı tablet.</summary>
    public int TabletId { get; set; }
    public Tablet Tablet { get; set; } = null!;

    /// <summary>Sunucu saati; cihaz saati kullanılmaz.</summary>
    public DateTime LoginAt { get; set; }

    /// <summary>Boşsa oturum açıktır.</summary>
    public DateTime? LogoutAt { get; private set; }

    public WorkSessionStatus Status { get; private set; } = WorkSessionStatus.Active;

    public WorkSessionEndReason? EndReason { get; private set; }

    /// <summary>Oturumu kapatan kullanıcı ya da tablet.</summary>
    public string? ClosedBy { get; private set; }

    public byte[] RowVersion { get; set; } = null!;

    public bool IsOpen => LogoutAt is null;

    /// <summary>Oturum açık ama operatör şu an çalışmıyor (mola, ara, arıza ya da demet bekleme).</summary>
    public bool IsInterrupted => IsOpen && Status != WorkSessionStatus.Active;

    /// <summary>Duraklama türünün karşılık geldiği oturum durumu.</summary>
    public static WorkSessionStatus StatusFor(WorkSessionPauseType type) => type switch
    {
        WorkSessionPauseType.Break => WorkSessionStatus.Break,
        WorkSessionPauseType.Pause => WorkSessionStatus.Paused,
        WorkSessionPauseType.MachineBreakdown => WorkSessionStatus.MachineBreakdown,
        _ => WorkSessionStatus.WaitingForBundle,
    };

    /// <summary>Oturumu duraklatır; duraklama kaydını çağıran oluşturur.</summary>
    public void Interrupt(WorkSessionPauseType type)
    {
        if (!IsOpen)
        {
            throw new InvalidOperationException("Kapalı oturum duraklatılamaz.");
        }

        Status = StatusFor(type);
    }

    /// <summary>Duraklamadan çalışmaya dönüş.</summary>
    public void Resume()
    {
        if (!IsOpen)
        {
            throw new InvalidOperationException("Kapalı oturum sürdürülemez.");
        }

        Status = WorkSessionStatus.Active;
    }

    public void Close(WorkSessionEndReason reason, DateTime utcNow, string? closedBy)
    {
        if (!IsOpen)
        {
            throw new InvalidOperationException("Oturum zaten kapatılmış.");
        }

        LogoutAt = utcNow;
        Status = WorkSessionStatus.LoggedOut;
        EndReason = reason;
        ClosedBy = closedBy;
    }
}

/// <summary>Operatörün yapabildiği operasyon (yetkinlik).</summary>
public class OperatorOperationQualification
{
    public int OperatorId { get; set; }
    public Operator Operator { get; set; } = null!;

    public int OperationId { get; set; }
    public Operation Operation { get; set; } = null!;
}

/// <summary>
/// Operatörün çalışabileceği makine. Bir operatör için hiç kayıt yoksa makine kısıtı yoktur
/// (yetkin olduğu operasyonu yapan her makinede çalışabilir); kayıt varsa yalnızca o makinelerde çalışır.
/// </summary>
public class OperatorMachine
{
    public int OperatorId { get; set; }
    public Operator Operator { get; set; } = null!;

    public int MachineId { get; set; }
    public Machine Machine { get; set; } = null!;
}
