using ERP.Boyahane.Domain.Enums;
using ERP.Core.Domain.Common;

namespace ERP.Boyahane.Domain.Entities;

/// <summary>Boyahane makinesi. Kapasite parti açılırken denetlenir; flotte oranı partiye varsayılan olarak gelir.</summary>
public class BoyahaneMachine : SoftDeletableEntity, IActivatable
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public DyeMachineType MachineType { get; set; } = DyeMachineType.Jet;

    /// <summary>Kapasite (kg).</summary>
    public decimal CapacityKg { get; set; }

    /// <summary>Varsayılan flotte oranı: 1 kg mal için litre su (1:8 için 8). Su kullanmayan makinede boş.</summary>
    public decimal? DefaultLiquorRatio { get; set; }

    public DyeMachineStatus Status { get; set; } = DyeMachineStatus.Available;

    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Parti: aynı kart ve aynı işlemle birlikte işlenen mal. Boyamada işlem reçetedir; baskıda desen, terbiyede yalnızca kart
/// belirler, reçete ve flotte tutulmaz. Farklı müşterilerin malı aynı partide olabilir.
/// </summary>
public class BoyahaneBatch : AuditableEntity
{
    public string BatchNo { get; set; } = null!;

    public int MachineId { get; set; }
    public BoyahaneMachine Machine { get; set; } = null!;

    /// <summary>Partinin reçetesi: bağlı sipariş satırının onaylanmış denemesi. Baskı ve terbiyede boştur.</summary>
    public int? RecipeTrialId { get; set; }
    public BoyahaneLabTrial? RecipeTrial { get; set; }

    /// <summary>Flotte oranı: 1 kg mal için litre su. Baskı ve terbiyede boştur.</summary>
    public decimal? LiquorRatio { get; set; }

    public DateOnly? PlannedDate { get; set; }

    /// <summary>Makine kuyruğundaki planlı sıra (1'den); boşsa sıralanmamıştır, kuyrukta plan tarihine göre sona dizilir.</summary>
    public int? PlanSequence { get; set; }

    public string? Notes { get; set; }
    public BatchStatus Status { get; set; } = BatchStatus.Planned;

    /// <summary>Kapasite aşımını onaylayan kullanıcı ve gerekçesi; aşım yoksa boş.</summary>
    public string? CapacityOverrideBy { get; set; }
    public string? CapacityOverrideNote { get; set; }

    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    /// <summary>Adımların alındığı rota; adımlar kopyalandığı için rota sonradan değişse parti etkilenmez.</summary>
    public int? RouteId { get; set; }
    public BoyahaneRoute? Route { get; set; }

    /// <summary>Partinin RFID kartı; parti kapanınca boşalır, kart başka partiye verilir.</summary>
    public string? CardUid { get; set; }

    /// <summary>Final kaliteden yeniden işleme ayrılan malla açıldıysa önceki parti.</summary>
    public int? ReworkOfBatchId { get; set; }
    public BoyahaneBatch? ReworkOfBatch { get; set; }

    public ICollection<BoyahaneBatchItem> Items { get; } = [];
    public ICollection<BoyahaneBatchStep> Steps { get; } = [];
}

/// <summary>Partideki top ya da iplik lotu. Miktar parti açılırken yazılır; iptal edilen partinin malı boşa çıkar.</summary>
public class BoyahaneBatchItem : AuditableEntity
{
    public int BatchId { get; set; }
    public BoyahaneBatch Batch { get; set; } = null!;

    public int OrderLineId { get; set; }
    public BoyahaneOrderLine OrderLine { get; set; } = null!;

    public int? RollId { get; set; }
    public BoyahaneFabricRoll? Roll { get; set; }

    public int? YarnLotId { get; set; }
    public BoyahaneYarnLot? YarnLot { get; set; }

    public decimal Kilograms { get; set; }

    /// <summary>İplik lotunda boştur.</summary>
    public decimal? Meters { get; set; }

    /// <summary>Parti iptal edilince ya da mal yeniden işleme ayrılınca kapanır; bir mal aynı anda tek etkin partide olur.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Final kalite sonucu; top ya da lot bazındadır.</summary>
    public FinalQualityResult? FinalResult { get; set; }

    /// <summary>Final kalitede ölçülen miktar; fire ve fatura buna göre hesaplanır.</summary>
    public decimal? FinalKilograms { get; set; }
    public decimal? FinalMeters { get; set; }
    public string? FinalNote { get; set; }

    /// <summary>Çıkış ölçüsü sınırı aşıldıysa onaylayan kullanıcı ve gerekçesi.</summary>
    public string? FinalOverrideBy { get; set; }
    public string? FinalOverrideNote { get; set; }
}
