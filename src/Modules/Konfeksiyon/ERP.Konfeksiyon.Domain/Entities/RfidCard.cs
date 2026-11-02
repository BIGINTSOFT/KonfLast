using ERP.Core.Domain.Common;
using ERP.Konfeksiyon.Domain.Enums;

namespace ERP.Konfeksiyon.Domain.Entities;

/// <summary>
/// RFID kartı. Kartta yalnızca benzersiz UID bulunur; kartın neyi temsil ettiği (hangi demet, hangi
/// operatör) atama kayıtlarından okunur. Okuyucu markası / protokolü bu katmana ulaşmaz.
/// </summary>
public class RfidCard : SoftDeletableEntity, IActivatable
{
    /// <summary>Normalize edilmiş UID (10 haneli kart numarası), örn. 0012629368.</summary>
    public string Uid { get; set; } = null!;

    public RfidCardType CardType { get; set; }

    /// <summary>Kartın üzerinde yazan numara / etiket (varsa).</summary>
    public string? Label { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Demet kartının bir demete atanması. Demet kartları yeniden kullanılır: demet tamamlanınca ya da
/// kart bırakılınca kayıt kapatılır ve kart başka bir demete atanabilir.
/// </summary>
public class BundleCardAssignment : BaseEntity
{
    public int RfidCardId { get; set; }
    public RfidCard RfidCard { get; set; } = null!;

    public int BundleId { get; set; }
    public Bundle Bundle { get; set; } = null!;

    public DateTime AssignedAt { get; set; }

    /// <summary>Boşsa kart hâlâ bu demettedir.</summary>
    public DateTime? ReleasedAt { get; private set; }

    public bool IsCurrent => ReleasedAt is null;

    public void Release(DateTime utcNow)
    {
        if (!IsCurrent)
        {
            throw new InvalidOperationException("Kart ataması zaten sonlandırılmış.");
        }

        ReleasedAt = utcNow;
    }
}

/// <summary>Operatörün personel kartı. Bir operatörün aynı anda tek geçerli kartı olur.</summary>
public class OperatorCard : BaseEntity
{
    public int OperatorId { get; set; }
    public Operator Operator { get; set; } = null!;

    public int RfidCardId { get; set; }
    public RfidCard RfidCard { get; set; } = null!;

    public DateTime AssignedAt { get; set; }

    /// <summary>Boşsa kart hâlâ bu operatöre aittir.</summary>
    public DateTime? RevokedAt { get; private set; }

    public bool IsCurrent => RevokedAt is null;

    public void Revoke(DateTime utcNow)
    {
        if (!IsCurrent)
        {
            throw new InvalidOperationException("Kart ataması zaten sonlandırılmış.");
        }

        RevokedAt = utcNow;
    }
}

/// <summary>
/// Tabletten gelen her kart okutması (başarısızlar dahil). Çift okuma kontrolü bu kayıtlar üzerinden
/// yapılır; kayıtlar silinmez.
/// </summary>
[NotAudited]
public class RfidReadLog
{
    public long Id { get; set; }

    /// <summary>İstemcinin okuma için ürettiği kimlik; aynı okuma tekrar gönderilirse yeniden işlenmez.</summary>
    public Guid ReadId { get; set; }

    public int TabletId { get; set; }
    public string Uid { get; set; } = null!;
    public RfidReadKind Kind { get; set; }

    /// <summary>"OK" ya da reddedilme nedeni olan iş hata kodu (örn. RFID_NOT_REGISTERED).</summary>
    public string ResultCode { get; set; } = null!;

    public int? OperatorId { get; set; }
    public int? BundleId { get; set; }

    /// <summary>Sunucu saati; cihaz saati kullanılmaz.</summary>
    public DateTime ReadAt { get; set; }

    public const string Ok = "OK";
}
