using ERP.Boyahane.Domain.Enums;
using ERP.Core.Domain.Common;

namespace ERP.Boyahane.Domain.Entities;

/// <summary>Çeki listesi: bir siparişin paketlenen malı. Sevkiyat buna göre ayrılır; farklı müşterilerin malı aynı listeye girmez.</summary>
public class BoyahanePackingList : AuditableEntity
{
    public string PackingNo { get; set; } = null!;

    public int OrderId { get; set; }
    public BoyahaneOrder Order { get; set; } = null!;

    public DateOnly PackingDate { get; set; }
    public string? Notes { get; set; }
    public PackingListStatus Status { get; set; } = PackingListStatus.Draft;

    /// <summary>Sevk edildiyse irsaliye; irsaliyedeki liste taslağa alınamaz.</summary>
    public int? DespatchId { get; set; }
    public BoyahaneDespatch? Despatch { get; set; }

    /// <summary>Stoğa üretim siparişinde malın satıldığı müşteri; sipariş müşterili ise boş.</summary>
    public int? SaleCustomerId { get; set; }
    public BoyahaneCustomer? SaleCustomer { get; set; }

    public ICollection<BoyahanePackingItem> Items { get; } = [];
}

/// <summary>Bir paket: uygun top ya da iplik lotu. Miktar final kalitede ölçülen çıkış ölçüsüdür, yazıldığı anda sabitlenir.</summary>
public class BoyahanePackingItem : AuditableEntity
{
    public int PackingListId { get; set; }
    public BoyahanePackingList PackingList { get; set; } = null!;

    /// <summary>Etiketteki barkod.</summary>
    public string PackageNo { get; set; } = null!;

    public int BatchItemId { get; set; }
    public BoyahaneBatchItem BatchItem { get; set; } = null!;

    public decimal Kilograms { get; set; }
    public decimal? Meters { get; set; }

    /// <summary>Koli adedi; top tek paket sayılır, iplikte lot birden çok koliye bölünebilir.</summary>
    public int BoxCount { get; set; } = 1;

    /// <summary>Liste iptal edilince kapanır; bir mal aynı anda tek listede olur.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Paketin mamul depodaki yeri; liste onaylanınca verilir.</summary>
    public int? LocationId { get; set; }
    public BoyahaneWarehouseLocation? Location { get; set; }
}
