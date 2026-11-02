using ERP.Core.Domain.Common;
using ERP.Konfeksiyon.Domain.Enums;

namespace ERP.Konfeksiyon.Domain.Entities;

/// <summary>Hata türü (dikiş hatası, leke, ölçü hatası ...).</summary>
public class DefectType : SoftDeletableEntity, IActivatable
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Kalite kontrolü: bir demet operasyonunun ürettiği parçaların kontrol sonucu. Kayıtlar değiştirilmez;
/// düzeltme yeni bir kontrol kaydıyla yapılır.
/// </summary>
[NotAudited]
public class QualityInspection
{
    public long Id { get; set; }

    public int BundleId { get; set; }
    public Bundle Bundle { get; set; } = null!;

    /// <summary>Çıktısı kontrol edilen operasyon.</summary>
    public long BundleOperationId { get; set; }
    public BundleOperation BundleOperation { get; set; } = null!;

    public QualityResult Result { get; set; }

    public int InspectedQuantity { get; set; }

    /// <summary>Iskartaya ayrılan adet.</summary>
    public int RejectedQuantity { get; set; }

    /// <summary>Tamire gönderilen adet.</summary>
    public int ReworkQuantity { get; set; }

    public int PassedQuantity => InspectedQuantity - RejectedQuantity - ReworkQuantity;

    /// <summary>Bu kontrol demeti kalite bekletmesine aldı mı.</summary>
    public bool PlacedOnHold { get; set; }

    /// <summary>Tamir sonrası tekrar kontrolse, kapattığı tamir işi.</summary>
    public long? ReworkTaskId { get; set; }

    public string? Note { get; set; }

    /// <summary>Sunucu saati.</summary>
    public DateTime InspectedAt { get; set; }
    public string InspectedBy { get; set; } = null!;

    public ICollection<QualityRecord> Records { get; } = new List<QualityRecord>();

    /// <summary>Sonuç adetlerden türetilir: bekletme &gt; tamir &gt; ret &gt; geçti.</summary>
    public static QualityResult ResultOf(int rejectedQuantity, int reworkQuantity, bool hold)
    {
        if (hold)
        {
            return QualityResult.Hold;
        }

        if (reworkQuantity > 0)
        {
            return QualityResult.Rework;
        }

        return rejectedQuantity > 0 ? QualityResult.Rejected : QualityResult.Passed;
    }
}

/// <summary>Kontrolde bulunan bir hata: türü ve adedi.</summary>
[NotAudited]
public class QualityRecord
{
    public long Id { get; set; }

    public long QualityInspectionId { get; set; }
    public QualityInspection QualityInspection { get; set; } = null!;

    public int DefectTypeId { get; set; }
    public DefectType DefectType { get; set; } = null!;

    public int Quantity { get; set; }
}

/// <summary>
/// Tamir işi: kalitenin bir operasyona geri gönderdiği parçalar. Operasyon yeniden işlenince "yapıldı",
/// tekrar kontrolden sonra "onaylandı" ya da "reddedildi" olur. Tamir hareketleri üretimden ayrı izlenir.
/// </summary>
[NotAudited]
public class ReworkTask
{
    public long Id { get; set; }

    /// <summary>Tamiri isteyen kontrol.</summary>
    public long QualityInspectionId { get; set; }
    public QualityInspection QualityInspection { get; set; } = null!;

    public int BundleId { get; set; }
    public Bundle Bundle { get; set; } = null!;

    public long BundleOperationId { get; set; }
    public BundleOperation BundleOperation { get; set; } = null!;

    public int Quantity { get; set; }

    public ReworkStatus Status { get; private set; } = ReworkStatus.Open;

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;

    /// <summary>Parçaların makinede yeniden işlenip bildirildiği an.</summary>
    public DateTime? DoneAt { get; private set; }

    /// <summary>Tekrar kontrolle kapatıldığı an.</summary>
    public DateTime? ClosedAt { get; private set; }
    public string? ClosedBy { get; private set; }

    /// <summary>Parçalar yeniden işlendi; tekrar kontrol bekliyor.</summary>
    public void MarkDone(DateTime utcNow)
    {
        if (Status != ReworkStatus.Open)
        {
            throw new InvalidOperationException($"{Status} durumundaki tamir işi 'yapıldı' olarak işaretlenemez.");
        }

        Status = ReworkStatus.Done;
        DoneAt = utcNow;
    }

    /// <summary>Tekrar kontrolün sonucuyla kapatır.</summary>
    public void Close(bool approved, DateTime utcNow, string closedBy)
    {
        if (Status != ReworkStatus.Done)
        {
            throw new InvalidOperationException("Yalnızca yeniden işlenmiş tamir işi tekrar kontrolle kapatılabilir.");
        }

        Status = approved ? ReworkStatus.Approved : ReworkStatus.Rejected;
        ClosedAt = utcNow;
        ClosedBy = closedBy;
    }
}
