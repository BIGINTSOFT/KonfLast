using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Konfeksiyon.Domain.Enums;
using FluentValidation;

namespace ERP.Konfeksiyon.Application.Features.Quality;

public sealed record DefectTypeDto(int Id, string Code, string Name, bool IsActive);

public sealed record DefectTypeRequest(string Code, string Name, bool IsActive = true);

public sealed record InspectionDefectRequest(int DefectTypeId, int Quantity);

/// <param name="BundleOperationId">Çıktısı kontrol edilen demet operasyonu.</param>
/// <param name="InspectedQuantity">Kontrol edilen adet (operasyonun ürettiğinden fazla olamaz).</param>
/// <param name="RejectedQuantity">Iskartaya ayrılan adet.</param>
/// <param name="ReworkQuantity">Tamire (operasyona geri) gönderilen adet.</param>
/// <param name="Hold">Demet kalite bekletmesine alınsın mı (serbest bırakılana kadar operasyonları başlatılamaz).</param>
/// <param name="Defects">Bulunan hatalar; ret ya da tamir varsa en az bir hata türü zorunludur.</param>
/// <param name="ReworkTaskId">Tamir sonrası tekrar kontrolse kapatılacak tamir işi.</param>
public sealed record InspectionRequest(
    long BundleOperationId,
    int InspectedQuantity,
    int RejectedQuantity = 0,
    int ReworkQuantity = 0,
    bool Hold = false,
    IReadOnlyList<InspectionDefectRequest>? Defects = null,
    string? Note = null,
    long? ReworkTaskId = null);

public sealed record InspectionDefectDto(int DefectTypeId, string DefectTypeName, int Quantity);

public sealed record InspectionDto(
    long Id,
    int BundleId,
    string BundleNo,
    string OrderNo,
    long BundleOperationId,
    int SequenceNo,
    string OperationName,
    QualityResult Result,
    int InspectedQuantity,
    int PassedQuantity,
    int RejectedQuantity,
    int ReworkQuantity,
    bool PlacedOnHold,
    long? ReworkTaskId,
    string? Note,
    DateTime InspectedAt,
    string InspectedBy,
    IReadOnlyList<InspectionDefectDto> Defects);

public sealed record ReworkTaskDto(
    long Id,
    long QualityInspectionId,
    int BundleId,
    string BundleNo,
    string OrderNo,
    long BundleOperationId,
    int SequenceNo,
    string OperationName,
    int Quantity,
    ReworkStatus Status,
    DateTime CreatedAt,
    string CreatedBy,
    DateTime? DoneAt,
    DateTime? ClosedAt,
    string? ClosedBy);

/// <summary>Demetin kalite durumu: kontrol girilebilecek operasyonlar, geçmiş ve tamir işleri.</summary>
/// <param name="OnHold">Demet kalite bekletmesinde mi.</param>
/// <param name="CanHold">Demet şu an bekletmeye alınabilir mi.</param>
public sealed record BundleQualityDto(
    int BundleId,
    string BundleNo,
    BundleStatus Status,
    bool OnHold,
    bool CanHold,
    IReadOnlyList<InspectableOperationDto> Operations,
    IReadOnlyList<InspectionDto> Inspections,
    IReadOnlyList<ReworkTaskDto> ReworkTasks);

/// <param name="InspectableQuantity">Kontrol edilebilecek (operasyonun ürettiği) adet.</param>
/// <param name="TakeBackLimit">
/// Ret / tamir için geri alınabilecek en fazla adet: sonraki operasyonun işlediği parçalar geri alınamaz.
/// </param>
/// <param name="PendingReworkQuantity">Tamire gönderilmiş, henüz yeniden işlenmemiş adet.</param>
public sealed record InspectableOperationDto(
    long BundleOperationId,
    int SequenceNo,
    string OperationName,
    BundleOperationStatus Status,
    int InspectableQuantity,
    int TakeBackLimit,
    int PendingReworkQuantity);

public sealed class InspectionListQuery : PageRequest
{
    /// <summary>Demet no ya da sipariş no içinde aranır.</summary>
    public string? Search { get; set; }
    public int? BundleId { get; set; }
    public QualityResult? Result { get; set; }

    /// <summary>Kontrol zamanı bu andan sonra (UTC).</summary>
    public DateTime? From { get; set; }

    /// <summary>Kontrol zamanı bu andan önce (UTC).</summary>
    public DateTime? To { get; set; }
}

public sealed class ReworkTaskListQuery : PageRequest
{
    public ReworkStatus? Status { get; set; }

    /// <summary>true: yalnızca kapanmamış (açık ya da tekrar kontrol bekleyen) işler.</summary>
    public bool? Pending { get; set; }
    public int? BundleId { get; set; }
}

public sealed class DefectSummaryQuery
{
    /// <summary>Aralığın başı (UTC, dahil).</summary>
    public DateTime From { get; set; }

    /// <summary>Aralığın sonu (UTC, hariç).</summary>
    public DateTime To { get; set; }
}

public sealed record DefectSummaryRowDto(int DefectTypeId, string Code, string Name, int Quantity, int InspectionCount);

/// <param name="DefectRatePercent">(Ret + tamir) ÷ kontrol edilen adet.</param>
public sealed record DefectSummaryDto(
    DateTime From,
    DateTime To,
    int InspectionCount,
    int InspectedQuantity,
    int RejectedQuantity,
    int ReworkQuantity,
    double? DefectRatePercent,
    IReadOnlyList<DefectSummaryRowDto> Defects);

public sealed class DefectTypeRequestValidator : AbstractValidator<DefectTypeRequest>
{
    public DefectTypeRequestValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName(100);
    }
}

public sealed class InspectionRequestValidator : AbstractValidator<InspectionRequest>
{
    public const int MaxDefectLines = 20;

    public InspectionRequestValidator()
    {
        RuleFor(x => x.BundleOperationId).GreaterThan(0).WithMessage("Kontrol edilen operasyon seçilmelidir.");
        RuleFor(x => x.InspectedQuantity).GreaterThan(0).WithMessage("Kontrol edilen adet 1 veya daha fazla olmalıdır.");
        RuleFor(x => x.RejectedQuantity).GreaterThanOrEqualTo(0).WithMessage("Ret adedi negatif olamaz.");
        RuleFor(x => x.ReworkQuantity).GreaterThanOrEqualTo(0).WithMessage("Tamir adedi negatif olamaz.");
        RuleFor(x => x.RejectedQuantity)
            .Must((request, rejected) => rejected + request.ReworkQuantity <= request.InspectedQuantity)
            .WithMessage("Ret ve tamir adetlerinin toplamı kontrol edilen adedi aşamaz.");
        RuleFor(x => x.Note).MaximumLength(500).WithMessage("Not en fazla 500 karakter olabilir.");

        RuleFor(x => x.Defects)
            .Must(defects => defects is { Count: > 0 })
            .WithMessage("Ret ya da tamir varsa en az bir hata türü belirtilmelidir.")
            .When(x => x.RejectedQuantity + x.ReworkQuantity > 0);
        RuleFor(x => x.Defects)
            .Must(defects => defects is null || defects.Count <= MaxDefectLines)
            .WithMessage($"En fazla {MaxDefectLines} hata satırı girilebilir.")
            .Must(defects => defects is null || defects.All(d => d.DefectTypeId > 0 && d.Quantity > 0))
            .WithMessage("Her hata satırında tür ve 1 veya daha fazla adet belirtilmelidir.")
            .Must(defects => defects is null || defects.Select(d => d.DefectTypeId).Distinct().Count() == defects.Count)
            .WithMessage("Aynı hata türü birden fazla satırda belirtilemez.");
    }
}
