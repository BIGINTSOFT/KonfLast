using ERP.Konfeksiyon.Domain.Enums;

namespace ERP.Konfeksiyon.Application.Features.Planning;

public sealed class PlanningOptions
{
    public const string SectionName = "Planning";

    /// <summary>Çalışılan günler; boşsa pazartesi-cumartesi.</summary>
    public DayOfWeek[] WorkingDays { get; set; } = [];

    /// <summary>Planlamada varsayılan beklenen verim (%100 = tam standart süre).</summary>
    public decimal DefaultEfficiencyPercent { get; set; } = 100;
}

/// <summary>Siparişin termin riski; en ciddi olan önce gelir.</summary>
public enum PlanningRisk
{
    /// <summary>Termin geçmiş, sipariş bitmemiş.</summary>
    Late,

    /// <summary>Siparişin planı terminden sonra bitiyor.</summary>
    PlanAfterDueDate,

    /// <summary>Kalan adedin bir kısmı henüz hiçbir banta planlanmamış.</summary>
    Unplanned,
    OnTrack,
}

/// <summary>Açık bir siparişin planlama durumu.</summary>
/// <param name="DaysToDue">Termine kalan gün (geçmişse eksi).</param>
/// <param name="Finished">Tamamlanan demetlerden çıkan sağlam adet.</param>
/// <param name="Planned">İptal edilmemiş bant planlarındaki toplam adet.</param>
/// <param name="Unplanned">Henüz banta planlanabilecek adet (sipariş − planlanan).</param>
/// <param name="RemainingWorkHours">Kalan adet × rotanın toplam SAM'i (iş gücü saati); rota yoksa null.</param>
public sealed record PlanningOrderDto(
    int Id,
    string OrderNo,
    string CustomerName,
    string ModelCode,
    string ProductName,
    ProductionOrderStatus Status,
    DateOnly DueDate,
    int DaysToDue,
    int Ordered,
    int Finished,
    int Remaining,
    int Planned,
    int Unplanned,
    bool HasRoute,
    double? RemainingWorkHours,
    DateTime? LastPlannedEnd,
    PlanningRisk Risk);

public enum PlanLoadStatus
{
    /// <summary>Plan, penceresindeki çalışma süresine rahatça sığıyor.</summary>
    Ok,

    /// <summary>Sığıyor ama pay %10'un altında.</summary>
    Tight,

    /// <summary>Planlanan adet, penceredeki çalışma süresinde üretilemez.</summary>
    Overloaded,

    /// <summary>Bantta rotanın bir operasyonunu yapacak çalışır makine yok.</summary>
    NoCapacity,

    /// <summary>Modelin aktif rotası yok; süre hesaplanamaz.</summary>
    NoRoute,
}

/// <param name="RatePerHour">Bantın bu modeli saatte çıkarabileceği adet (darboğaz operasyonu belirler).</param>
/// <param name="RequiredHours">Planlanan adet için gereken çalışma süresi.</param>
/// <param name="AvailableHours">Planın penceresindeki çalışma süresi (takvime göre).</param>
/// <param name="LoadPercent">Gereken ÷ mevcut süre.</param>
public sealed record PlanLoadDto(
    int PlanId,
    int ProductionOrderId,
    string OrderNo,
    string ModelCode,
    int PlannedQuantity,
    DateTime PlannedStart,
    DateTime PlannedEnd,
    double? RatePerHour,
    string? BottleneckOperationName,
    double? RequiredHours,
    double AvailableHours,
    double? LoadPercent,
    PlanLoadStatus Status,
    DateOnly DueDate,
    bool EndsAfterDueDate);

/// <param name="WorkingHours">Aralıktaki çalışma süresi (takvime göre).</param>
/// <param name="BookedHours">Bunun planlara ayrılmış kısmı.</param>
public sealed record LineLoadDto(
    int LineId,
    string LineCode,
    string Name,
    int Machines,
    double WorkingHours,
    double BookedHours,
    double FreeHours,
    double? UtilizationPercent,
    IReadOnlyList<PlanLoadDto> Plans);

/// <param name="CalendarAssumed">Vardiya tanımlı olmadığı için gün boyu çalışma varsayıldı.</param>
public sealed record LineLoadReportDto(
    DateTime From, DateTime To, decimal EfficiencyPercent, bool CalendarAssumed, IReadOnlyList<LineLoadDto> Lines);

public sealed class LineLoadQuery
{
    /// <summary>Aralığın başı (UTC).</summary>
    public DateTime From { get; set; }

    /// <summary>Aralığın sonu (UTC).</summary>
    public DateTime To { get; set; }

    /// <summary>Beklenen verim; verilmezse ayarlardaki varsayılan.</summary>
    public decimal? EfficiencyPercent { get; set; }
}

public sealed class PlanSuggestionQuery
{
    public int ProductionOrderId { get; set; }

    /// <summary>Planlanacak adet; verilmezse siparişin planlanmamış adedinin tamamı.</summary>
    public int? Quantity { get; set; }

    public decimal? EfficiencyPercent { get; set; }
}

/// <summary>Siparişin bir banta yerleştirilmesi önerisi.</summary>
/// <param name="Feasible">Bant bu siparişi üretebilir ve takvimde yer bulundu.</param>
/// <param name="Reason">Uygun değilse nedeni.</param>
/// <param name="SuggestedStart">Bantın mevcut planlarıyla çakışmayan en erken başlangıç.</param>
/// <param name="SuggestedEnd">Çalışma takvimine göre hesaplanan bitiş.</param>
public sealed record LineSuggestionDto(
    int LineId,
    string LineCode,
    string Name,
    bool Feasible,
    string? Reason,
    double? RatePerHour,
    string? BottleneckOperationName,
    double? RequiredHours,
    DateTime? SuggestedStart,
    DateTime? SuggestedEnd,
    bool? MeetsDueDate);

public sealed record PlanSuggestionDto(
    int ProductionOrderId,
    string OrderNo,
    string ModelCode,
    DateOnly DueDate,
    int Quantity,
    int UnplannedQuantity,
    decimal EfficiencyPercent,
    bool CalendarAssumed,
    IReadOnlyList<LineSuggestionDto> Lines);
