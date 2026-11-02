using ERP.Core.Domain.Common;
using ERP.Konfeksiyon.Domain.Enums;

namespace ERP.Konfeksiyon.Domain.Entities;

/// <summary>
/// Makine başındaki Android tablet. Tablet bir makineye bağlıdır; makinenin operasyonu ve bantı
/// üzerinden "üretim nerede yapılıyor" sorusunu yanıtlar.
///
/// Cihaz eşleştirme: tablet ayarlarından makine seçilir ve cihaza özel bir anahtar alınır; tablet
/// kaydına yalnızca bir cihaz bağlanabilir. Anahtarın yalnızca hash'i saklanır.
/// </summary>
public class Tablet : SoftDeletableEntity, IActivatable
{
    public string TabletCode { get; set; } = null!;
    public string? Name { get; set; }

    public int? MachineId { get; set; }
    public Machine? Machine { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Eşleştirilen fiziksel cihazın kimliği (Android ID vb.).</summary>
    public string? DeviceIdentifier { get; private set; }

    [AuditMasked] public string? DeviceKeyHash { get; private set; }

    /// <summary>Tabletten gelen son istek zamanı (kalp atışı).</summary>
    [AuditIgnore] public DateTime? LastSeenAt { get; set; }

    public bool IsEnrolled => DeviceKeyHash is not null;

    /// <summary>Cihazı tablete bağlar. Zaten bir cihaz bağlıysa önce <see cref="ResetDevice"/> gerekir.</summary>
    public void Enroll(string deviceIdentifier, string deviceKeyHash)
    {
        if (IsEnrolled)
        {
            throw new InvalidOperationException("Tablet zaten bir cihazla eşleştirilmiş.");
        }

        DeviceIdentifier = deviceIdentifier;
        DeviceKeyHash = deviceKeyHash;
    }

    /// <summary>Cihaz bağını koparır (kayıp / değişen tablet); yeniden eşleştirme gerekir.</summary>
    public void ResetDevice()
    {
        DeviceIdentifier = null;
        DeviceKeyHash = null;
    }
}

/// <summary>
/// Makinenin bir durumda geçirdiği süre. Durum her değiştiğinde açık kayıt kapatılır ve yenisi açılır;
/// böylece arıza ve bakım süreleri hesaplanabilir. Kayıtlar silinmez.
/// </summary>
[NotAudited]
public class MachineStatusLog
{
    public long Id { get; set; }

    public int MachineId { get; set; }
    public Machine Machine { get; set; } = null!;

    public MachineStatus Status { get; set; }
    public DateTime StartedAt { get; set; }

    /// <summary>Boşsa makine hâlâ bu durumdadır.</summary>
    public DateTime? EndedAt { get; set; }

    public string? Reason { get; set; }

    /// <summary>Durumu değiştiren kullanıcı adı ya da tablet kodu.</summary>
    public string ChangedBy { get; set; } = null!;

    /// <summary>Değişiklik sahadan (tabletten) bildirildiyse o tablet.</summary>
    public int? TabletId { get; set; }
}
