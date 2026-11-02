using ERP.Core.Application.Common;
using ERP.Konfeksiyon.Domain.Enums;

namespace ERP.Konfeksiyon.Application.Features.ProductionTracking;

public sealed record BundleStatusCountDto(BundleStatus Status, int BundleCount, int Quantity);

/// <summary>Siparişin bir operasyondaki durumu (siparişin tüm demetleri üzerinden).</summary>
/// <param name="Planned">Bu operasyondan geçecek adet (önceki adımlarda hatalı çıkanlar düşülmüş).</param>
/// <param name="WaitingBundles">Bu operasyonun önünde bekleyen (hazır ya da yarım bırakılmış) demet sayısı.</param>
public sealed record OrderOperationProgressDto(
    int SequenceNo,
    int OperationId,
    string OperationCode,
    string OperationName,
    int Planned,
    int Produced,
    int Rejected,
    int Remaining,
    int WaitingBundles,
    int InProgressBundles,
    int CompletedBundles,
    int TotalBundles);

/// <param name="Finished">Tüm operasyonları biten demetlerden çıkan sağlam adet.</param>
/// <param name="Rejected">Herhangi bir operasyonda hatalı çıkan adet.</param>
public sealed record OrderItemProgressDto(
    int ColorId, string ColorName, int SizeId, string SizeName, int Ordered, int Bundled, int Finished, int Rejected);

/// <param name="Bundled">İptal edilmemiş demetlerdeki toplam adet.</param>
/// <param name="Finished">Tüm operasyonları tamamlanan demetlerden çıkan sağlam adet.</param>
/// <param name="InProcess">Henüz tamamlanmamış (açık) demetlerdeki adet.</param>
/// <param name="Shortage">Sipariş adedine ulaşmak için eksik kalan sağlam adet.</param>
/// <param name="CompletionPercent">Biten sağlam adedin sipariş adedine oranı (0-100).</param>
/// <param name="CanComplete">Sipariş şu an tamamlanabilir mi (üretimde ve açık demeti yok).</param>
/// <param name="Shipped">
/// Bu siparişten sevk edilen adet: siparişe bağlı sevkiyatlar ile yalnızca müşteri siparişine bağlı sevkiyatlarda bu
/// siparişin ürünü ve renk-bedenlerinden sevk edilenler.
/// </param>
public sealed record OrderProgressDto(
    int ProductionOrderId,
    string OrderNo,
    ProductionOrderStatus Status,
    int Ordered,
    int Bundled,
    int Finished,
    int Rejected,
    int InProcess,
    int Shortage,
    decimal CompletionPercent,
    int OpenBundleCount,
    bool CanComplete,
    IReadOnlyList<BundleStatusCountDto> BundleStatuses,
    IReadOnlyList<OrderOperationProgressDto> Operations,
    IReadOnlyList<OrderItemProgressDto> Items,
    int Shipped = 0);

/// <summary>Bir operasyonun önündeki ara stok: sahadaki (kartı atanmış) demetler.</summary>
/// <param name="ReadyQuantity">Başlatılmayı bekleyen demetlerdeki adet.</param>
/// <param name="PausedQuantity">Yarım bırakılmış demetlerde kalan adet.</param>
public sealed record WipOperationDto(
    int OperationId,
    string OperationCode,
    string OperationName,
    int ReadyBundles,
    int ReadyQuantity,
    int InProgressBundles,
    int InProgressQuantity,
    int PausedBundles,
    int PausedQuantity);

public sealed class WipQuery
{
    /// <summary>Yalnızca bu siparişin demetleri.</summary>
    public int? ProductionOrderId { get; set; }
}

public sealed class WipBundleQuery : PageRequest
{
    public int OperationId { get; set; }
    public int? ProductionOrderId { get; set; }

    /// <summary>READY, IN_PROGRESS ya da PAUSED; boşsa üçü de.</summary>
    public BundleOperationStatus? Status { get; set; }
}

public sealed record WipBundleDto(
    long BundleOperationId,
    int BundleId,
    string BundleNo,
    int ProductionOrderId,
    string OrderNo,
    string ModelCode,
    string ColorName,
    string SizeName,
    BundleOperationStatus Status,
    int PlannedQuantity,
    int ProducedQuantity,
    int RejectedQuantity,
    int RemainingQuantity,
    string? MachineCode,
    string? OperatorName,
    DateTime? StartedAt);

/// <summary>Operatörün anlık durumu; türler birbirine karıştırılmaz (mola ≠ arıza ≠ demet bekleme).</summary>
public enum OperatorState
{
    /// <summary>Makinede açık oturum yok.</summary>
    Offline,

    /// <summary>Oturum açık, operatör hazır ama elinde iş yok.</summary>
    Working,

    /// <summary>Operatör bir demet operasyonu üzerinde çalışıyor.</summary>
    Producing,
    WaitingForBundle,
    Paused,
    Break,
    MachineBreakdown,
}

/// <summary>Bir makinenin şu anki durumu: başındaki operatör ve süren işi.</summary>
/// <param name="OperatorState">Makinedeki operatörün durumu (oturum yoksa OFFLINE).</param>
public sealed record MachineActivityDto(
    int MachineId,
    string MachineCode,
    string MachineName,
    MachineStatus Status,
    string? OperationName,
    string? LineCode,
    string? OperatorName,
    OperatorState OperatorState,
    DateTime? SessionStartedAt,
    int? BundleId,
    string? BundleNo,
    DateTime? JobStartedAt,
    int? RemainingQuantity);

public enum OutputGroupBy
{
    Operator,
    Machine,
    Operation,
}

public sealed class OutputQuery
{
    /// <summary>Aralığın başı (UTC, dahil).</summary>
    public DateTime From { get; set; }

    /// <summary>Aralığın sonu (UTC, hariç).</summary>
    public DateTime To { get; set; }

    public OutputGroupBy GroupBy { get; set; } = OutputGroupBy.Operator;
}

/// <param name="Bundles">Bu aralıkta adet bildirilen farklı demet sayısı.</param>
public sealed record OutputRowDto(int Id, string Code, string Name, int Produced, int Rejected, int Bundles);

public sealed record OutputReportDto(
    DateTime From, DateTime To, OutputGroupBy GroupBy, int TotalProduced, int TotalRejected, IReadOnlyList<OutputRowDto> Rows);
