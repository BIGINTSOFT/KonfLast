using ERP.Boyahane.Domain.Enums;
using ERP.Core.Domain.Common;

namespace ERP.Boyahane.Domain.Entities;

/// <summary>Kumaş kabul belgesi: bir irsaliye ya da araçla gelen mal.</summary>
public class BoyahaneFabricReceipt : AuditableEntity
{
    public string ReceiptNo { get; set; } = null!;
    public DateOnly ReceiptDate { get; set; }

    /// <summary>Malın sahibi olan müşteri (fason); boşsa mal işletmenindir.</summary>
    public int? CustomerId { get; set; }
    public BoyahaneCustomer? Customer { get; set; }

    public string? DespatchNo { get; set; }
    public string? Notes { get; set; }

    /// <summary>Sisteme geçişte girilen başlangıç stoğu: toplar ham kontrole girmez.</summary>
    public bool IsOpening { get; set; }

    public ICollection<BoyahaneFabricLot> Lots { get; } = [];
}

/// <summary>Belgedeki aynı kumaş kartından gelen mal. İrsaliyede yazan değerler, sayılan toplarla karşılaştırılmak için saklanır.</summary>
public class BoyahaneFabricLot : AuditableEntity
{
    public int ReceiptId { get; set; }
    public BoyahaneFabricReceipt Receipt { get; set; } = null!;

    public string LotNo { get; set; } = null!;

    public int FabricId { get; set; }
    public BoyahaneFabric Fabric { get; set; } = null!;

    /// <summary>Müşterinin irsaliyesinde geçen kendi kumaş kodu ya da adı.</summary>
    public string? CustomerFabricCode { get; set; }

    public int? DeclaredRollCount { get; set; }
    public decimal? DeclaredMeters { get; set; }
    public decimal? DeclaredKilograms { get; set; }

    /// <summary>Kabul anında ham kontrol gerekli miydi (müşteri kartındaki seçimden gelir).</summary>
    public bool InspectionRequired { get; set; }

    public ICollection<BoyahaneFabricRoll> Rolls { get; } = [];
}

/// <summary>Tek top. Metre ve kilodan ölçülmemiş olan, kumaş kartının ham en ve gramajından hesaplanmıştır.</summary>
public class BoyahaneFabricRoll : AuditableEntity
{
    public int LotId { get; set; }
    public BoyahaneFabricLot Lot { get; set; } = null!;

    /// <summary>Sistemin verdiği top numarası; barkod bunu taşır.</summary>
    public string RollNo { get; set; } = null!;

    /// <summary>Müşterinin kendi top numarası.</summary>
    public string? CustomerRollNo { get; set; }

    public decimal Meters { get; set; }
    public decimal Kilograms { get; set; }

    /// <summary>Metre ölçüldü mü; değilse kilodan hesaplanmıştır.</summary>
    public bool MetersMeasured { get; set; }

    /// <summary>Kilo tartıldı mı; değilse metreden hesaplanmıştır.</summary>
    public bool KilogramsMeasured { get; set; }

    public FabricRollStatus Status { get; set; } = FabricRollStatus.PendingInspection;

    // Ham kontrol: kabul değerlerinin üzerine yazılmaz, yanına kaydedilir; ölçülen varsa geçerli değer odur.
    public decimal? InspectedMeters { get; set; }
    public decimal? InspectedKilograms { get; set; }
    public decimal? InspectedWidthCm { get; set; }
    public int? InspectedWeightGsm { get; set; }
    public DateTime? InspectedAt { get; set; }
    public string? InspectedBy { get; set; }
    public string? InspectionNote { get; set; }

    /// <summary>Şartlı top müşteri (işletme malında yetkili) tarafından kabul edildi; hata kaydı kanıt olarak kalır.</summary>
    public bool IsConditionallyApproved { get; set; }

    /// <summary>Şartlı top için verilen karar; boşsa karar girilmemiştir.</summary>
    public bool? CustomerDecisionAccepted { get; set; }
    public string? CustomerDecisionBy { get; set; }
    public CustomerDecisionChannel? CustomerDecisionChannel { get; set; }
    public DateOnly? CustomerDecisionOn { get; set; }
    public string? CustomerDecisionNote { get; set; }

    /// <summary>Kararı sisteme giren kullanıcı.</summary>
    public string? CustomerDecisionRecordedBy { get; set; }

    public ICollection<BoyahaneFabricRollDefect> Defects { get; } = [];

    /// <summary>Topun bağlı olduğu sipariş satırı; ham kumaş siparişten önce gelebildiği için boş olabilir.</summary>
    public int? OrderLineId { get; set; }
    public BoyahaneOrderLine? OrderLine { get; set; }

    /// <summary>Topun depodaki yeri; makineye girince son adres olarak kalır.</summary>
    public int? LocationId { get; set; }
    public BoyahaneWarehouseLocation? Location { get; set; }
}
