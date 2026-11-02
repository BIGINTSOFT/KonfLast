namespace ERP.Konfeksiyon.Domain.Enums;

public enum BundleStatus
{
    /// <summary>Oluşturuldu; henüz RFID kartı atanmadı / sahaya verilmedi.</summary>
    Created,

    /// <summary>Üretime hazır, ilk operasyonunu bekliyor.</summary>
    Waiting,

    InProduction,
    PartiallyCompleted,
    Completed,
    QualityHold,
    Rework,
    Cancelled,
    Closed,
}

public enum BundleOperationStatus
{
    /// <summary>Öncül operasyonların tamamlanmasını bekliyor.</summary>
    Waiting,

    /// <summary>Öncülleri tamam; başlatılabilir.</summary>
    Ready,

    InProgress,
    Paused,
    Completed,
    QualityHold,
    Cancelled,
}
