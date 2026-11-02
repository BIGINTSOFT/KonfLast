namespace ERP.Konfeksiyon.Domain.Enums;

public enum BundleOperationEventType
{
    OperationStarted,

    /// <summary>Kısmi üretimden sonra kalan adet için yeniden başlatma.</summary>
    OperationResumed,

    /// <summary>Kalan adet varken operasyonun bırakılması.</summary>
    OperationPaused,
    OperationCompleted,
    QuantityReported,
    RejectionReported,
}

/// <summary>Kalitenin bir operasyondan parça geri alamamasının (ret / tamir) nedeni.</summary>
public enum QualityTakeBackBlock
{
    None,

    /// <summary>Operasyon şu an makinede işleniyor.</summary>
    OperationInProgress,

    /// <summary>İstenen adet operasyonun ürettiğinden fazla.</summary>
    ExceedsProduced,

    /// <summary>Parçalar sonraki operasyonda işlenmiş; kontrol o operasyon üzerinden yapılmalı.</summary>
    ConsumedDownstream,
}

/// <summary>Bir demet operasyonunun neden başlatılamadığı.</summary>
public enum OperationStartBlock
{
    None,

    /// <summary>Demete henüz kart atanmamış.</summary>
    BundleNotReleased,

    /// <summary>Demet tamamlanmış, kapatılmış ya da iptal edilmiş.</summary>
    BundleClosed,
    BundleOnQualityHold,
    OperationCompleted,
    OperationCancelled,
    OperationOnQualityHold,
    PredecessorsIncomplete,

    /// <summary>Bu operasyon zaten işlemde.</summary>
    AlreadyInProgress,

    /// <summary>Demetin başka bir operasyonu işlemde: demet aynı anda tek yerde işlenir.</summary>
    AnotherOperationInProgress,

    /// <summary>
    /// Demetin başlanıp yarım bırakılmış başka bir operasyonu var: o tamamlanmadan başka operasyona geçilmez
    /// (kalite tamiri için yeniden açılan adet bu kurala girmez).
    /// </summary>
    AnotherOperationUnfinished,

    /// <summary>Demet fasoncuda (fason gönderim fişiyle gönderilmiş); dönüş fişi işlenene kadar sahada işlenemez.</summary>
    BundleAtSubcontractor,
}

/// <summary>Demet operasyonlarının hangi sırayla yapılacağı; üretim siparişinde seçilir, demete kopyalanır.</summary>
public enum OperationSequenceMode
{
    /// <summary>Rotadaki sıra numarasına göre: 1 → 2 → 3 → 4; önceki tamamlanmadan sonraki başlatılamaz.</summary>
    Sequential,

    /// <summary>Sırasız (bağımsız): operasyonlar istenen sırayla yapılır.</summary>
    Free,

    /// <summary>
    /// Rotadaki öncül tanımlarına göre (paralel adımlar kendi aralarında serbest). Bu özellikten önce açılmış
    /// demetler için korunur; yeni siparişte seçilmez.
    /// </summary>
    Route,
}
