using ERP.Core.Application.Common;
using ERP.Konfeksiyon.Domain.Enums;
using FluentValidation;

namespace ERP.Konfeksiyon.Application.Features.ProductionLines;

public sealed record LineMachineDto(
    int MachineId,
    int Position,
    string MachineCode,
    string MachineName,
    string MachineTypeName,
    string? OperationName,
    MachineStatus Status,
    bool IsActive,
    DateTime AssignedAt);

/// <param name="IsCurrent">Bant şu an (sunucu saatiyle) bu planın zaman aralığında mı.</param>
/// <param name="RowVersion">Güncelleme ve iptalde geri gönderilir (eşzamanlılık kontrolü).</param>
public sealed record LinePlanDto(
    int Id,
    int ProductionLineId,
    string LineCode,
    string LineName,
    int ProductionOrderId,
    string OrderNo,
    string CustomerName,
    string ProductCode,
    string ProductName,
    string ModelCode,
    DateTime PlannedStart,
    DateTime PlannedEnd,
    int PlannedQuantity,
    LinePlanStatus Status,
    bool IsEditable,
    bool IsCurrent,
    string? Notes,
    string RowVersion);

/// <summary>Bantın tek ekranlık özeti: makineleri, şu anki planı ve sıradaki planlar.</summary>
public sealed record LineOverviewDto(
    int Id,
    string LineCode,
    string Name,
    string ProductionAreaName,
    string FactoryName,
    bool IsActive,
    IReadOnlyList<LineMachineDto> Machines,
    LinePlanDto? CurrentPlan,
    IReadOnlyList<LinePlanDto> UpcomingPlans);

/// <param name="Transfer">Makine başka bir banttaysa oradan çıkarılıp bu banta taşınsın mı.</param>
public sealed record AssignMachineRequest(int MachineId, bool Transfer = false);

/// <param name="MachineIds">Banttaki tüm makineler, istenen sırayla.</param>
public sealed record ReorderMachinesRequest(IReadOnlyList<int> MachineIds);

public sealed class LinePlanListQuery : PageRequest
{
    public int? ProductionLineId { get; set; }
    public int? ProductionOrderId { get; set; }
    public LinePlanStatus? Status { get; set; }

    /// <summary>Bu andan sonra biten planlar (UTC).</summary>
    public DateTime? From { get; set; }

    /// <summary>Bu andan önce başlayan planlar (UTC).</summary>
    public DateTime? To { get; set; }
}

/// <param name="ProductionLineId">Yalnızca oluşturmada kullanılır; planın bantı ve siparişi sonradan değişmez.</param>
/// <param name="PlannedStart">UTC.</param>
/// <param name="PlannedEnd">UTC; başlangıçtan sonra olmalıdır.</param>
/// <param name="RowVersion">Güncellemede zorunlu; oluşturmada gönderilmez.</param>
public sealed record LinePlanRequest(
    int ProductionLineId,
    int ProductionOrderId,
    DateTime PlannedStart,
    DateTime PlannedEnd,
    int PlannedQuantity,
    string? Notes,
    string? RowVersion = null);

public sealed record LinePlanVersionRequest(string RowVersion);

public sealed class AssignMachineRequestValidator : AbstractValidator<AssignMachineRequest>
{
    public AssignMachineRequestValidator()
    {
        RuleFor(x => x.MachineId).GreaterThan(0).WithMessage("Makine seçilmelidir.");
    }
}

public sealed class ReorderMachinesRequestValidator : AbstractValidator<ReorderMachinesRequest>
{
    public ReorderMachinesRequestValidator()
    {
        RuleFor(x => x.MachineIds)
            .NotNull().WithMessage("Makine listesi zorunludur.")
            .Must(ids => ids is null || ids.Distinct().Count() == ids.Count)
            .WithMessage("Aynı makine listede birden fazla kez yer alamaz.");
    }
}

public sealed class LinePlanRequestValidator : AbstractValidator<LinePlanRequest>
{
    public const int MaxPlannedQuantity = 10_000_000;
    public static readonly TimeSpan MaxDuration = TimeSpan.FromDays(366);

    public LinePlanRequestValidator()
    {
        RuleFor(x => x.ProductionLineId).GreaterThan(0).WithMessage("Bant seçilmelidir.");
        RuleFor(x => x.ProductionOrderId).GreaterThan(0).WithMessage("Sipariş seçilmelidir.");
        RuleFor(x => x.PlannedStart).NotEqual(default(DateTime)).WithMessage("Başlangıç zamanı zorunludur.");
        RuleFor(x => x.PlannedEnd)
            .NotEqual(default(DateTime)).WithMessage("Bitiş zamanı zorunludur.")
            .GreaterThan(x => x.PlannedStart).WithMessage("Bitiş zamanı başlangıçtan sonra olmalıdır.")
            .Must((request, end) => end - request.PlannedStart <= MaxDuration)
            .WithMessage("Bir plan en fazla bir yıl sürebilir.");
        RuleFor(x => x.PlannedQuantity)
            .InclusiveBetween(1, MaxPlannedQuantity)
            .WithMessage($"Planlanan adet 1 ile {MaxPlannedQuantity:N0} arasında olmalıdır.");
        RuleFor(x => x.Notes).MaximumLength(500).WithMessage("Not en fazla 500 karakter olabilir.");
    }
}

public sealed class LinePlanVersionRequestValidator : AbstractValidator<LinePlanVersionRequest>
{
    public LinePlanVersionRequestValidator()
    {
        RuleFor(x => x.RowVersion).NotEmpty().WithMessage("Kayıt sürümü (rowVersion) zorunludur.");
    }
}
