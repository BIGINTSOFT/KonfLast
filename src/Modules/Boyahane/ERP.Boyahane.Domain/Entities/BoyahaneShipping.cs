using ERP.Boyahane.Domain.Enums;
using ERP.Core.Domain.Common;
using ERP.Core.Domain.Enums;

namespace ERP.Boyahane.Domain.Entities;

/// <summary>İrsaliye: aynı müşterinin onaylı çeki listeleriyle yapılan sevkiyat. Bir araçla birkaç sipariş gidebilir.</summary>
public class BoyahaneDespatch : AuditableEntity
{
    public string DespatchNo { get; set; } = null!;

    public int CustomerId { get; set; }
    public BoyahaneCustomer Customer { get; set; } = null!;

    public DateOnly DespatchDate { get; set; }
    public string? VehiclePlate { get; set; }
    public string? DriverName { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? Notes { get; set; }
    public DespatchStatus Status { get; set; } = DespatchStatus.Draft;

    /// <summary>Faturalandıysa fatura; bir irsaliye tek faturaya girer.</summary>
    public int? InvoiceId { get; set; }
    public BoyahaneInvoice? Invoice { get; set; }

    public ICollection<BoyahanePackingList> PackingLists { get; } = [];
}

/// <summary>Fatura: aynı müşterinin onaylı irsaliyelerinden, sipariş satırı bazında. Miktar fatura ölçüsüne göre paketlerden gelir.</summary>
public class BoyahaneInvoice : AuditableEntity
{
    public string InvoiceNo { get; set; } = null!;

    public int CustomerId { get; set; }
    public BoyahaneCustomer Customer { get; set; } = null!;

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

    public ICollection<BoyahaneInvoiceLine> Lines { get; } = [];
    public ICollection<BoyahaneDespatch> Despatches { get; } = [];
}

public class BoyahaneInvoiceLine : AuditableEntity
{
    public int InvoiceId { get; set; }
    public BoyahaneInvoice Invoice { get; set; } = null!;

    public int OrderLineId { get; set; }
    public BoyahaneOrderLine OrderLine { get; set; } = null!;

    public string Description { get; set; } = null!;
    public decimal Quantity { get; set; }
    public StockUnit Unit { get; set; }

    /// <summary>Fatura ölçüsünün birim fiyatı; siparişten gelir, taslakta değiştirilebilir.</summary>
    public decimal? UnitPrice { get; set; }
}
