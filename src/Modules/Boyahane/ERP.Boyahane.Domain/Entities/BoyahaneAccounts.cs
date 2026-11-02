using ERP.Boyahane.Domain.Enums;
using ERP.Core.Domain.Common;

namespace ERP.Boyahane.Domain.Entities;

/// <summary>Müşteriden tahsilat. Düzenlenmez; yanlış kayıt gerekçeyle iptal edilip yeniden girilir.</summary>
public class BoyahanePayment : AuditableEntity
{
    public string PaymentNo { get; set; } = null!;

    public int CustomerId { get; set; }
    public BoyahaneCustomer Customer { get; set; } = null!;

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

    public ICollection<BoyahanePaymentAllocation> Allocations { get; } = [];
}

/// <summary>Tahsilatın bir faturaya dağıtılan kısmı. Dağıtılmayan kısım müşterinin avansıdır.</summary>
public class BoyahanePaymentAllocation : AuditableEntity
{
    public int PaymentId { get; set; }
    public BoyahanePayment Payment { get; set; } = null!;

    public int InvoiceId { get; set; }
    public BoyahaneInvoice Invoice { get; set; } = null!;

    public decimal Amount { get; set; }
}
