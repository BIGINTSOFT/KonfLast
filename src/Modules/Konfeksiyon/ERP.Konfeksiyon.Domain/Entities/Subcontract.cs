using ERP.Core.Domain.Common;
using ERP.Konfeksiyon.Domain.Enums;

namespace ERP.Konfeksiyon.Domain.Entities;

/// <summary>
/// Fason fiyatı: bir fasoncunun bir operasyon için adet başı birim fiyatı, başlangıç tarihinden itibaren geçerli.
/// Ürün verilirse yalnızca o ürün için geçerlidir ve genel (ürünsüz) fiyatın önüne geçer. Bir gün için geçerli fiyat,
/// başlangıcı o güne kadar olan en yeni kayıttır.
/// </summary>
public class SubcontractPrice : AuditableEntity
{
    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;

    public int OperationId { get; set; }
    public Operation Operation { get; set; } = null!;

    /// <summary>Boşsa operasyonun bütün ürünleri için.</summary>
    public int? ProductId { get; set; }
    public Product? Product { get; set; }

    /// <summary>Adet başı fiyat.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>ISO 4217 para birimi kodu.</summary>
    public string Currency { get; set; } = ERP.Core.Domain.Entities.Currency.LocalCode;

    public DateOnly ValidFrom { get; set; }

    public string? Notes { get; set; }
}

/// <summary>
/// Fason gönderim fişi: bir fasoncuya bir operasyon için gönderilen demetler (ya da ürün / renk / beden adetleri).
/// Basılı irsaliye olarak yazdırılır. Demet satırlarındaki demetler dönüş işlenene kadar fasoncuda sayılır.
/// </summary>
public class SubcontractSend : AuditableEntity
{
    /// <summary>Sistem tarafından üretilir, örn. FG-2026-00001.</summary>
    public string SendNo { get; set; } = null!;

    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;

    public DateOnly SendDate { get; set; }

    /// <summary>Fasonda yapılacak operasyon.</summary>
    public int OperationId { get; set; }
    public Operation Operation { get; set; } = null!;

    public SubcontractSendStatus Status { get; private set; } = SubcontractSendStatus.Open;

    /// <summary>Taşıyıcı / araç plakası (irsaliyede yazılır).</summary>
    public string? Carrier { get; set; }

    public string? Notes { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public ICollection<SubcontractSendLine> Lines { get; } = new List<SubcontractSendLine>();

    public bool IsOpen => Status is SubcontractSendStatus.Open or SubcontractSendStatus.PartiallyReturned;

    /// <summary>Hiç dönüş işlenmemiş fiş iptal edilebilir.</summary>
    public bool CanCancel => Status == SubcontractSendStatus.Open && Lines.All(l => l.ReturnedTotal == 0);

    public int TotalQuantity => Lines.Sum(l => l.Quantity);
    public int OutstandingQuantity => Lines.Sum(l => l.Outstanding);

    /// <summary>Durumu satırların dönüş adetlerinden yeniden belirler.</summary>
    public void RefreshStatus()
    {
        if (Status == SubcontractSendStatus.Cancelled)
        {
            return;
        }

        Status = Lines.All(l => l.Outstanding == 0)
            ? SubcontractSendStatus.Closed
            : Lines.Any(l => l.ReturnedTotal > 0) ? SubcontractSendStatus.PartiallyReturned : SubcontractSendStatus.Open;
    }

    public void Cancel()
    {
        if (!CanCancel)
        {
            throw new InvalidOperationException("Dönüşü işlenmiş fason gönderimi iptal edilemez.");
        }

        Status = SubcontractSendStatus.Cancelled;
    }
}

/// <summary>
/// Gönderim satırı: bir demet (demetin o operasyonu) ya da demetsiz bir ürün / renk / beden adedi. Dönen adetler
/// dönüş fişleriyle birikir; gönderilen = sağlam + hatalı + eksik + hâlâ fasoncuda olan.
/// </summary>
public class SubcontractSendLine : BaseEntity
{
    public int SubcontractSendId { get; set; }
    public SubcontractSend SubcontractSend { get; set; } = null!;

    public int? BundleId { get; set; }
    public Bundle? Bundle { get; set; }

    /// <summary>Fasonda yapılan demet operasyonu; demetsiz satırda boş.</summary>
    public long? BundleOperationId { get; set; }
    public BundleOperation? BundleOperation { get; set; }

    public int? ProductionOrderId { get; set; }
    public ProductionOrder? ProductionOrder { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int ColorId { get; set; }
    public Color Color { get; set; } = null!;

    public int SizeId { get; set; }
    public Size Size { get; set; } = null!;

    public int Quantity { get; set; }

    public int ReturnedGood { get; set; }
    public int ReturnedDefective { get; set; }
    public int ReturnedMissing { get; set; }

    /// <summary>Gönderim tarihindeki fason fiyatı (adet başı); fiyat tablosunda yoksa boş.</summary>
    public decimal? UnitPrice { get; set; }
    public string Currency { get; set; } = ERP.Core.Domain.Entities.Currency.LocalCode;

    public int ReturnedTotal => ReturnedGood + ReturnedDefective + ReturnedMissing;

    /// <summary>Hâlâ fasoncuda olan adet.</summary>
    public int Outstanding => Quantity - ReturnedTotal;
}

/// <summary>
/// Fason dönüş fişi: bir gönderime karşı dönen sağlam, hatalı ve eksik adetler. Sağlam adet demet operasyonuna
/// üretilmiş olarak işlenir; fişin tutarı (sağlam × fiyat) fasoncunun hakedişidir ve "ödendi" olarak işaretlenir.
/// </summary>
public class SubcontractReturn : AuditableEntity
{
    /// <summary>Sistem tarafından üretilir, örn. FD-2026-00001.</summary>
    public string ReturnNo { get; set; } = null!;

    public int SubcontractSendId { get; set; }
    public SubcontractSend SubcontractSend { get; set; } = null!;

    /// <summary>Gönderimin fasoncusu (hakediş listesi bununla süzülür).</summary>
    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;

    public DateOnly ReturnDate { get; set; }

    /// <summary>Fasoncunun irsaliye numarası.</summary>
    public string? SupplierDocumentNo { get; set; }

    public string? Notes { get; set; }

    /// <summary>Hakediş ödendiyse ödeme günü; boşsa ödenmedi.</summary>
    public DateOnly? PaidDate { get; private set; }
    public string? PaidBy { get; private set; }
    public string? PaymentNote { get; private set; }

    public byte[] RowVersion { get; set; } = null!;

    public ICollection<SubcontractReturnLine> Lines { get; } = new List<SubcontractReturnLine>();

    public bool IsPaid => PaidDate is not null;

    public void MarkPaid(DateOnly paidDate, string user, string? note)
    {
        PaidDate = paidDate;
        PaidBy = user;
        PaymentNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
    }

    public void MarkUnpaid()
    {
        PaidDate = null;
        PaidBy = null;
        PaymentNote = null;
    }
}

/// <summary>Dönüş satırı: gönderim satırına karşı dönen adetler ve hakediş fiyatı.</summary>
public class SubcontractReturnLine : BaseEntity
{
    public int SubcontractReturnId { get; set; }
    public SubcontractReturn SubcontractReturn { get; set; } = null!;

    public int SubcontractSendLineId { get; set; }
    public SubcontractSendLine SubcontractSendLine { get; set; } = null!;

    public int GoodQuantity { get; set; }

    /// <summary>Hatalı dönen adet; demet operasyonuna hatalı (ıskarta) olarak işlenir.</summary>
    public int DefectiveQuantity { get; set; }

    /// <summary>Fasoncunun teslim edemediği adet; demet operasyonuna hatalı olarak işlenir.</summary>
    public int MissingQuantity { get; set; }

    /// <summary>Hatalıların türü (varsa).</summary>
    public int? DefectTypeId { get; set; }
    public DefectType? DefectType { get; set; }

    /// <summary>Hakediş birim fiyatı (adet başı); gönderim satırının fiyatından gelir, değiştirilebilir.</summary>
    public decimal? UnitPrice { get; set; }
    public string Currency { get; set; } = ERP.Core.Domain.Entities.Currency.LocalCode;

    /// <summary>Hakediş tutarı: sağlam adet × birim fiyat.</summary>
    public decimal Amount => decimal.Round(GoodQuantity * (UnitPrice ?? 0), 2);
}
