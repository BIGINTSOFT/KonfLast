using ERP.Core.Domain.Common;
using ERP.Dokuma.Domain.Enums;

namespace ERP.Dokuma.Domain.Entities;

/// <summary>Çözgü emri: onaylı sipariş satırı için kaç levent, levent başına kaç metre çözgü hazırlanacağı.</summary>
public class DokumaWarpOrder : AuditableEntity
{
    public string WarpNo { get; set; } = null!;

    public int OrderLineId { get; set; }
    public DokumaOrderLine OrderLine { get; set; } = null!;

    public DateOnly PlannedDate { get; set; }
    public int PlannedBeamCount { get; set; }
    public decimal MetersPerBeam { get; set; }

    /// <summary>Toplam tel sayısı; emir açılırken artikelden alınır.</summary>
    public int Ends { get; set; }

    public WarpOrderStatus Status { get; set; } = WarpOrderStatus.Open;
    public DateOnly? CompletedOn { get; set; }
    public string? Notes { get; set; }

    public ICollection<DokumaWarpIssue> Issues { get; } = [];
    public ICollection<DokumaBeam> Beams { get; } = [];
    public ICollection<DokumaYarnTakeBack> TakeBacks { get; } = [];
}

/// <summary>Çözgüye verilen iplik: lottan kesin düşülür.</summary>
public class DokumaWarpIssue : AuditableEntity
{
    public int WarpOrderId { get; set; }
    public DokumaWarpOrder WarpOrder { get; set; } = null!;

    public int YarnLotId { get; set; }
    public DokumaYarnLot YarnLot { get; set; } = null!;

    public DateOnly IssuedOn { get; set; }
    public decimal Kilograms { get; set; }
}

/// <summary>Levent: çözgüden çıkan, haşıl ve tahar / düğümden geçip tezgâha bağlanan çözgü.</summary>
public class DokumaBeam : AuditableEntity
{
    public string BeamNo { get; set; } = null!;

    public int WarpOrderId { get; set; }
    public DokumaWarpOrder WarpOrder { get; set; } = null!;

    public decimal Meters { get; set; }
    public int Ends { get; set; }

    public SizingMode SizingMode { get; set; } = SizingMode.None;

    /// <summary>Haşıl tamamlandı (içeride yapıldı ya da fasondan geri alındı).</summary>
    public bool IsSized { get; set; }

    /// <summary>Fason haşılda haşılcı.</summary>
    public int? SizingSupplierId { get; set; }
    public DokumaSupplier? SizingSupplier { get; set; }

    public DateOnly? SentToSizingOn { get; set; }

    /// <summary>İçeride haşıl tarihi ya da fasondan geri alım tarihi.</summary>
    public DateOnly? SizedOn { get; set; }

    /// <summary>Uygulanan haşıl reçetesi; fasonda haşılcıya gönderilen belgeye basılır (bilgi amaçlı).</summary>
    public int? SizingRecipeId { get; set; }
    public DokumaSizingRecipe? SizingRecipe { get; set; }

    /// <summary>Gerçekleşen alım (%): içeride haşılda girilir, fasonda geri alımda isteğe bağlı.</summary>
    public decimal? SizingPickUpPercent { get; set; }

    /// <summary>Gerçekleşen banyo viskozitesi (saniye).</summary>
    public decimal? SizingViscositySeconds { get; set; }

    public DrawingMethod? DrawingMethod { get; set; }
    public DateOnly? ReadyOn { get; set; }

    public BeamStatus Status { get; set; } = BeamStatus.Warped;
    public string? Notes { get; set; }

    /// <summary>Leventin bağlı olduğu (ya da en son bağlandığı) tezgâh.</summary>
    public int? LoomId { get; set; }
    public DokumaLoom? Loom { get; set; }

    public DateOnly? MountedOn { get; set; }
    public DateOnly? FinishedOn { get; set; }

    /// <summary>Levent bitince üzerinde kalan çözgü (artık) metresi.</summary>
    public decimal? LeftoverMeters { get; set; }

    /// <summary>Kapasite planında leventin bağlanacağı tezgâh; tezgâha bağlanınca temizlenir. Fiili bağlama <see cref="LoomId"/>'dir.</summary>
    public int? PlannedLoomId { get; set; }
    public DokumaLoom? PlannedLoom { get; set; }

    /// <summary>Planlanan tezgâhın kuyruğundaki sıra (1'den); boşsa kuyruğun sonunda.</summary>
    public int? PlanSequence { get; set; }

    public ICollection<DokumaWeftIssue> WeftIssues { get; } = [];
    public ICollection<DokumaFabricRoll> Rolls { get; } = [];
    public ICollection<DokumaYarnTakeBack> TakeBacks { get; } = [];
}

/// <summary>Tezgâhtaki levente verilen atkı ipliği: lottan kesin düşülür.</summary>
public class DokumaWeftIssue : AuditableEntity
{
    public int BeamId { get; set; }
    public DokumaBeam Beam { get; set; } = null!;

    public int YarnLotId { get; set; }
    public DokumaYarnLot YarnLot { get; set; } = null!;

    public DateOnly IssuedOn { get; set; }
    public decimal Kilograms { get; set; }
}

/// <summary>Tezgâhtan kesilen ham kumaş topu; 4 puan kontrolüne girer.</summary>
public class DokumaFabricRoll : AuditableEntity
{
    public string RollNo { get; set; } = null!;

    public int BeamId { get; set; }
    public DokumaBeam Beam { get; set; } = null!;

    /// <summary>Topun dokunduğu tezgâh (levent sonradan başka tezgâha geçse de değişmez).</summary>
    public int LoomId { get; set; }
    public DokumaLoom Loom { get; set; } = null!;

    public DateOnly CutOn { get; set; }
    public decimal Meters { get; set; }

    /// <summary>Tartıldıysa ağırlık (kg).</summary>
    public decimal? Kilograms { get; set; }

    public FabricRollStatus Status { get; set; } = FabricRollStatus.PendingInspection;
    public string? Notes { get; set; }

    public DateOnly? InspectedOn { get; set; }

    /// <summary>Kontrolde ölçülen metre; kesimde girilenin üzerine yazılmaz.</summary>
    public decimal? InspectedMeters { get; set; }

    /// <summary>Kontrolde ölçülen en (cm).</summary>
    public decimal? InspectedWidthCm { get; set; }

    /// <summary>Metre başına en fazla 4 puan sınırı uygulanmış toplam puan.</summary>
    public int? TotalPoints { get; set; }

    /// <summary>100 m² başına puan: toplam puan × 100 / (metre × en).</summary>
    public decimal? PointsPer100M2 { get; set; }

    /// <summary>Puan sınırlarına göre bulunan kalite; yetkili kararı farklıysa <see cref="Status"/> ondan ayrılır.</summary>
    public FabricRollStatus? CalculatedStatus { get; set; }

    /// <summary>Yetkilinin kalite kararını değiştirme gerekçesi; boşsa karar hesaplanan kalitedir.</summary>
    public string? OverrideNote { get; set; }

    public string? InspectionNotes { get; set; }

    /// <summary>Tabletten gelen kesimin işlem kimliği: yeniden gönderilen kesim ikinci top açmaz.</summary>
    public Guid? ClientRequestId { get; set; }

    /// <summary>Topu tabletten kesen personel; ofisten girilen topta boş.</summary>
    public int? CutByOperatorId { get; set; }
    public DokumaOperator? CutByOperator { get; set; }

    public ICollection<DokumaRollDefect> Defects { get; } = [];
}

/// <summary>Kontrolde topta bulunan hata: yeri, uzunluğu ve 4 puan sistemine göre puanı.</summary>
public class DokumaRollDefect : AuditableEntity
{
    public int RollId { get; set; }
    public DokumaFabricRoll Roll { get; set; } = null!;

    public int DefectTypeId { get; set; }
    public DokumaDefectType DefectType { get; set; } = null!;

    /// <summary>Topun başından itibaren kaçıncı metrede.</summary>
    public decimal AtMeter { get; set; }

    public decimal LengthCm { get; set; }
    public int Points { get; set; }
}

/// <summary>Dokuma hata türü (atkı kaçığı, çözgü kopuğu, delik ...).</summary>
public class DokumaDefectType : SoftDeletableEntity, IActivatable
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;

    /// <summary>Uzunluğundan bağımsız her zaman 4 puan (örn. delik).</summary>
    public bool AlwaysFourPoints { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>Dokuma kalite sınırları (tek kayıt): 100 m² başına puan; müşteri kartında müşteriye özel sınır verilebilir.</summary>
public class DokumaQualitySettings : AuditableEntity
{
    public decimal FirstQualityMaxPoints { get; set; } = 20;
    public decimal SecondQualityMaxPoints { get; set; } = 40;
}
