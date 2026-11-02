using ERP.Core.Domain.Common;
using ERP.Dokuma.Domain.Enums;

namespace ERP.Dokuma.Domain.Entities;

/// <summary>Çeki listesi: bir siparişin sevk edilecek topları. Liste ya irsaliyeyle müşteriye ya da Boyahane'ye gider.</summary>
public class DokumaPackingList : AuditableEntity
{
    public string PackingNo { get; set; } = null!;

    public int OrderId { get; set; }
    public DokumaOrder Order { get; set; } = null!;

    public DateOnly PackingDate { get; set; }
    public string? Notes { get; set; }
    public PackingListStatus Status { get; set; } = PackingListStatus.Draft;

    /// <summary>Stoğa üretim siparişinde malın satıldığı müşteri; sipariş müşterili ise boş.</summary>
    public int? SaleCustomerId { get; set; }
    public DokumaCustomer? SaleCustomer { get; set; }

    /// <summary>Sevk edildiyse irsaliye; irsaliyedeki liste taslağa alınamaz.</summary>
    public int? DespatchId { get; set; }
    public DokumaDespatch? Despatch { get; set; }

    /// <summary>Boyahane'ye gönderildiyse oluşan kumaş kabul belgesinin numarası; gönderilen liste geri alınmaz.</summary>
    public string? BoyahaneReceiptNo { get; set; }
    public DateOnly? BoyahaneSentOn { get; set; }

    public ICollection<DokumaPackingItem> Items { get; } = [];
}

/// <summary>Listedeki top. Metre kontrolde ölçülendir, kalite listeye girdiği andaki karardır; ikisi sabitlenir.</summary>
public class DokumaPackingItem : AuditableEntity
{
    public int PackingListId { get; set; }
    public DokumaPackingList PackingList { get; set; } = null!;

    public int RollId { get; set; }
    public DokumaFabricRoll Roll { get; set; } = null!;

    public decimal Meters { get; set; }
    public decimal? Kilograms { get; set; }
    public FabricRollStatus Grade { get; set; }

    /// <summary>Liste iptal edilince kapanır; bir top aynı anda tek listede olur.</summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>İrsaliye: aynı müşterinin onaylı çeki listeleriyle yapılan sevkiyat.</summary>
public class DokumaDespatch : AuditableEntity
{
    public string DespatchNo { get; set; } = null!;

    public int CustomerId { get; set; }
    public DokumaCustomer Customer { get; set; } = null!;

    public DateOnly DespatchDate { get; set; }
    public string? VehiclePlate { get; set; }
    public string? DriverName { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? Notes { get; set; }
    public DespatchStatus Status { get; set; } = DespatchStatus.Draft;

    /// <summary>Faturalandıysa fatura; bir irsaliye tek faturaya girer.</summary>
    public int? InvoiceId { get; set; }
    public DokumaInvoice? Invoice { get; set; }

    public ICollection<DokumaPackingList> PackingLists { get; } = [];
}

/// <summary>Fatura: aynı müşterinin onaylı irsaliyelerinden, sipariş satırı ve kalite bazında metre.</summary>
public class DokumaInvoice : AuditableEntity
{
    public string InvoiceNo { get; set; } = null!;

    public int CustomerId { get; set; }
    public DokumaCustomer Customer { get; set; } = null!;

    public DateOnly InvoiceDate { get; set; }
    public DateOnly? DueDate { get; set; }
    public string Currency { get; set; } = "TRY";

    /// <summary>KDV oranı (%); onay için zorunludur, sıfır olabilir.</summary>
    public decimal? VatRate { get; set; }

    /// <summary>KDV'nin tevkif edilen kısmı (%), örneğin 5/10 için 50; yoksa boş.</summary>
    public decimal? WithholdingPercent { get; set; }

    public string? Notes { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    /// <summary>Sisteme geçişteki açılış borcu; doluysa kayıt fatura değildir, satırı olmaz, ödenecek tutar budur.</summary>
    public decimal? OpeningAmount { get; set; }

    public ICollection<DokumaInvoiceLine> Lines { get; } = [];
    public ICollection<DokumaDespatch> Despatches { get; } = [];
}

public class DokumaInvoiceLine : AuditableEntity
{
    public int InvoiceId { get; set; }
    public DokumaInvoice Invoice { get; set; } = null!;

    public int OrderLineId { get; set; }
    public DokumaOrderLine OrderLine { get; set; } = null!;

    public FabricRollStatus Grade { get; set; }
    public string Description { get; set; } = null!;

    /// <summary>Metre.</summary>
    public decimal Quantity { get; set; }

    /// <summary>Metre fiyatı; siparişten gelir, taslakta değiştirilebilir.</summary>
    public decimal? UnitPrice { get; set; }

    /// <summary>İndirim (%); 2. kalitede müşteri kartından gelir, taslakta değiştirilebilir.</summary>
    public decimal? DiscountPercent { get; set; }
}

/// <summary>Müşteriden tahsilat. Düzenlenmez; yanlış kayıt gerekçeyle iptal edilip yeniden girilir.</summary>
public class DokumaPayment : AuditableEntity
{
    public string PaymentNo { get; set; } = null!;

    public int CustomerId { get; set; }
    public DokumaCustomer Customer { get; set; } = null!;

    public DateOnly PaymentDate { get; set; }
    public PaymentMethod Method { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";

    /// <summary>Çek no ya da dekont no.</summary>
    public string? ReferenceNo { get; set; }

    /// <summary>Çekin vadesi.</summary>
    public DateOnly? DueDate { get; set; }

    public string? Notes { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Active;
    public string? CancelReason { get; set; }

    public ICollection<DokumaPaymentAllocation> Allocations { get; } = [];
}

/// <summary>Tahsilatın bir faturaya dağıtılan kısmı. Dağıtılmayan kısım müşterinin avansıdır.</summary>
public class DokumaPaymentAllocation : AuditableEntity
{
    public int PaymentId { get; set; }
    public DokumaPayment Payment { get; set; } = null!;

    public int InvoiceId { get; set; }
    public DokumaInvoice Invoice { get; set; } = null!;

    public decimal Amount { get; set; }
}

/// <summary>Üretimden artan iplik: çözgü emrine ya da tezgâhtaki levente verilenden düşer, lota döner.</summary>
public class DokumaYarnTakeBack : AuditableEntity
{
    /// <summary>Çözgüden artan; doluysa <see cref="BeamId"/> boştur.</summary>
    public int? WarpOrderId { get; set; }
    public DokumaWarpOrder? WarpOrder { get; set; }

    /// <summary>Atkıdan artan; doluysa <see cref="WarpOrderId"/> boştur.</summary>
    public int? BeamId { get; set; }
    public DokumaBeam? Beam { get; set; }

    public int YarnLotId { get; set; }
    public DokumaYarnLot YarnLot { get; set; } = null!;

    public DateOnly TakenOn { get; set; }
    public decimal Kilograms { get; set; }
    public string? Notes { get; set; }
}

/// <summary>Fasonda müşteriye geri verilen iplik: lottan kesin düşülür.</summary>
public class DokumaYarnReturn : AuditableEntity
{
    public string ReturnNo { get; set; } = null!;

    public int YarnLotId { get; set; }
    public DokumaYarnLot YarnLot { get; set; } = null!;

    public DateOnly ReturnDate { get; set; }
    public decimal Kilograms { get; set; }
    public string? Notes { get; set; }
}
