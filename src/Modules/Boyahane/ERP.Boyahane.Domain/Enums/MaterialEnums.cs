namespace ERP.Boyahane.Domain.Enums;

public enum FabricType
{
    /// <summary>Örme.</summary>
    Knit,

    /// <summary>Dokuma.</summary>
    Woven,
}

/// <summary>İplik numara sistemi.</summary>
public enum YarnCountSystem
{
    Ne,
    Nm,
    Dtex,
}

/// <summary>Topun ham kontrol durumu; yalnızca uygun top partiye alınabilir.</summary>
public enum FabricRollStatus
{
    PendingInspection,
    Approved,

    /// <summary>Hata not edildi, müşteri onayı bekleniyor.</summary>
    Conditional,

    /// <summary>İşleme girmez, iade edilir.</summary>
    Rejected,
}

public enum DefectCategory
{
    /// <summary>Leke, delik, çizgi gibi görünüm hataları.</summary>
    Appearance,

    /// <summary>İplik kopuğu, kaçık gibi yapı hataları.</summary>
    Structure,

    /// <summary>En ve gramaj farkı gibi ölçü hataları.</summary>
    Measure,
}

/// <summary>Şartlı top kararının alındığı yol.</summary>
public enum CustomerDecisionChannel
{
    Email,
    Phone,
    Written,
    InPerson,
}

public enum OrderOwnership
{
    /// <summary>Fason: kumaş müşterinin.</summary>
    Commission,

    /// <summary>Kendi malı: kumaş işletmenin, boyanıp satılır.</summary>
    OwnGoods,
}

public enum OrderStatus
{
    Draft,
    Confirmed,
    Cancelled,

    /// <summary>İşi biten onaylı sipariş elle kapatılır; salt okunurdur, yeni parti, talep ve çeki açılmaz.</summary>
    Closed,
}

public enum OrderProcessType
{
    FabricDyeing,
    YarnDyeing,
    Finishing,
    Printing,
}

public enum YarnPackageType
{
    Bobbin,
    Hank,
}

public enum DyeChemicalKind
{
    /// <summary>Boya: reçetede kumaş ağırlığına göre %.</summary>
    Dye,

    /// <summary>Kimyasal: reçetede g/L.</summary>
    Chemical,
}

public enum LabRequestStatus
{
    Open,
    Approved,
    Cancelled,
}

public enum LabTrialDecision
{
    Pending,
    Accepted,
    Rejected,
}

public enum DyeMachineType
{
    Jet,
    Overflow,
    Jigger,

    /// <summary>Ram.</summary>
    Stenter,

    /// <summary>İplik boyama kazanı.</summary>
    YarnVessel,
}

public enum BatchStatus
{
    Planned,
    OnMachine,
    Completed,
    Cancelled,
}

public enum YarnLotStatus
{
    Approved,
    Rejected,
}

/// <summary>Operasyonun adımda istediği kayıt.</summary>
public enum OperationBehavior
{
    /// <summary>Başlat ve bitir.</summary>
    Standard,

    /// <summary>Reçete uygulanır, tüketim girilir.</summary>
    Dyeing,

    /// <summary>Parti için uygun, ilave ya da yeniden işlem.</summary>
    ColorCheck,

    /// <summary>Top ya da lot bazında sonuç.</summary>
    FinalQuality,

    /// <summary>Söküm: renk kontrolünden eklenir, seçilen söküm reçetesi uygulanır, tüketim girilir.</summary>
    Stripping,

    /// <summary>Baskı: desen renk yolunun baskı reçetesi uygulanır, pasta tüketimi girilir.</summary>
    Printing,
}

public enum BatchStepStatus
{
    Pending,
    InProgress,
    Completed,
    Skipped,
}

public enum ColorCheckResult
{
    Approved,

    /// <summary>İlave: eksik renk için ek boya ve kimyasal.</summary>
    Addition,

    /// <summary>Yeniden işlem: reçete baştan uygulanır.</summary>
    Redo,

    /// <summary>Söküm + yeniden boya: önce söküm reçetesi, ardından reçete baştan uygulanır.</summary>
    Stripping,
}

public enum FinalQualityResult
{
    Approved,
    Rejected,

    /// <summary>Mal boşa çıkar, yeni partiye girebilir.</summary>
    Rework,
}

public enum BoyahaneCardType
{
    Operator,
    Batch,
}

/// <summary>Yetki gerektiren saha işlemleri.</summary>
public enum BoyahaneFloorAction
{
    Pause,
    ReportBreakdown,
    ResolveBreakdown,
}

public enum BoyahaneOperatorRole
{
    Operator,

    /// <summary>Vardiya amiri: arızayı kapatabilir.</summary>
    Supervisor,

    /// <summary>Bakım: arızayı kapatabilir.</summary>
    Maintenance,
}

public enum DyeMachineStatus
{
    Available,
    Breakdown,
    Maintenance,
}

public enum BoyahaneStopReasonCategory
{
    Pause,
    Breakdown,
}

public enum BoyahaneBreakPolicy
{
    /// <summary>Önce süren adımı bitirir.</summary>
    AfterStep,

    /// <summary>İşi bırakıp hemen çıkar.</summary>
    Immediate,
}

public enum BoyahanePauseKind
{
    /// <summary>Parti bekliyor: bekleme süresi çalışılmış sayılmaz, adım başlayınca kendiliğinden biter.</summary>
    WaitingForBatch,

    /// <summary>Mola.</summary>
    Break,

    /// <summary>Kısa ara; neden zorunludur.</summary>
    Pause,

    /// <summary>Makine arızası: yetkili kartıyla kapanır.</summary>
    MachineBreakdown,
}

public enum BoyahaneSessionEndReason
{
    Logout,

    /// <summary>Vardiya bitti; oturum kendiliğinden kapandı.</summary>
    ShiftEnded,

    /// <summary>Tablette başka personel oturum açtı.</summary>
    TakenOver,

    /// <summary>Personel başka tablette oturum açtı.</summary>
    MovedAway,

    /// <summary>Ofisten kapatıldı.</summary>
    ClosedByOffice,
}

/// <summary>Fatura ölçüsü: müşteri kartında varsayılan, sipariş satırında istenirse farklı.</summary>
public enum BillingUnit
{
    OrderUnit,
    Meter,
    Kilogram,
}

public enum PackingListStatus
{
    Draft,
    Confirmed,
    Cancelled,
}

public enum DespatchStatus
{
    Draft,
    Confirmed,
    Cancelled,
}

public enum InvoiceStatus
{
    Draft,
    Confirmed,
    Cancelled,
}

public enum ChemicalMovementType
{
    Receipt,
    Consumption,
    Adjustment,
}

public enum PurchaseOrderStatus
{
    Draft,
    Confirmed,
    PartiallyReceived,
    Received,

    /// <summary>Kısmi teslimden sonra kalan artık beklenmiyor.</summary>
    Closed,
    Cancelled,
}

public enum PaymentMethod
{
    Cash,
    BankTransfer,
    Check,

    /// <summary>Sisteme geçişte müşterinin alacak bakiyesi; gerçek tahsilat değildir.</summary>
    Opening,
}

public enum PaymentStatus
{
    Active,
    Cancelled,
}

public enum BoyahaneWarehouseType
{
    /// <summary>Ham kumaş ve iplik.</summary>
    RawFabric,
    Chemical,
    FinishedGoods,
    Quarantine,
}

public enum WarehouseGoodsType
{
    FabricRoll,
    YarnLot,
    ChemicalLot,
    Package,
}

public enum LocationMovementType
{
    AddressChange,
    Transfer,
}

public enum GoodsReturnKind
{
    CustomerReturn,
    Scrap,
}

public enum GoodsReturnStatus
{
    Draft,
    Confirmed,
    Cancelled,
}

/// <summary>Desenin baskı tekniği.</summary>
public enum PrintType
{
    Pigment,
    Reactive,

    /// <summary>Aşındırma baskı.</summary>
    Discharge,
    Other,
}

/// <summary>Fiyat teklifinin durumu.</summary>
public enum QuotationStatus
{
    Draft,

    /// <summary>Müşteriye gönderildi; yanıt bekleniyor.</summary>
    Sent,

    /// <summary>Kabul edildi; taslak sipariş açıldı.</summary>
    Accepted,
    Rejected,

    /// <summary>Geçerlilik süresi yanıtsız doldu.</summary>
    Expired,
}

/// <summary>Teklif satırındaki parti süresinin kaynağı.</summary>
public enum ProcessMinutesSource
{
    /// <summary>Satırda elle girildi.</summary>
    Manual,

    /// <summary>İşlem türünün varsayılan rotasındaki operasyonların standart süreleri.</summary>
    Route,

    /// <summary>Parametrelerdeki işlem türü varsayılanı.</summary>
    Default,
}

/// <summary>Boya ve kimyasalın ön maliyette kullanılan kilogram maliyeti.</summary>
public enum ChemicalCostMethod
{
    /// <summary>Maliyeti girilmiş en son lot.</summary>
    LastLot,

    /// <summary>Eldeki (kalanı olan) maliyetli lotların ağırlıklı ortalaması; elde lot yoksa son lot.</summary>
    WeightedAverage,
}

/// <summary>Söküm reçetesindeki miktarın ölçüsü.</summary>
public enum RecipeAmountUnit
{
    /// <summary>Mal ağırlığına göre %.</summary>
    Percent,

    /// <summary>Flotte suyuna göre g/L.</summary>
    GramsPerLiter,
}
