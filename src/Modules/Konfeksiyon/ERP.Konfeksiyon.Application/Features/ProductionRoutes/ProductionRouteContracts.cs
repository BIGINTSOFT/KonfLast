using ERP.Core.Application.Common;
using ERP.Konfeksiyon.Domain.Enums;
using FluentValidation;

namespace ERP.Konfeksiyon.Application.Features.ProductionRoutes;

public sealed record ProductionRouteListItemDto(
    int Id,
    int ModelId,
    string ModelCode,
    string ModelName,
    int Version,
    string? Name,
    RouteStatus Status,
    int OperationCount,
    decimal TotalSam);

/// <param name="Predecessors">Bu adım başlamadan önce tamamlanması gereken adımların sıra numaraları.</param>
/// <param name="Stage">
/// Akıştaki aşama (0'dan başlar). Aynı aşamadaki adımlar birbirini beklemeden paralel yürüyebilir.
/// </param>
public sealed record ProductionRouteOperationDto(
    int Id,
    int SequenceNo,
    int OperationId,
    string OperationCode,
    string OperationName,
    string? MachineTypeName,
    decimal Sam,
    IReadOnlyList<int> Predecessors,
    int Stage,
    bool IsSubcontracted = false,
    int? SubcontractorId = null,
    string? SubcontractorName = null);

public sealed record ProductionRouteDto(
    int Id,
    int ModelId,
    string ModelCode,
    string ModelName,
    int Version,
    string? Name,
    string? Description,
    RouteStatus Status,
    bool IsEditable,
    bool CanActivate,
    bool CanArchive,
    decimal TotalSam,
    string RowVersion,
    DateTime CreatedAt,
    string? CreatedBy,
    IReadOnlyList<ProductionRouteOperationDto> Operations);

public sealed class ProductionRouteListQuery : PageRequest
{
    /// <summary>Model kodu, model adı veya rota adı içinde aranır.</summary>
    public string? Search { get; set; }
    public int? ModelId { get; set; }
    public RouteStatus? Status { get; set; }
}

/// <param name="SequenceNo">Rota içindeki adım numarası (20, 30 ...); rota içinde benzersiz.</param>
/// <param name="Sam">Bu model için süre (dk/adet); boşsa operasyonun standart SAM'i kullanılır.</param>
/// <param name="Predecessors">Önce tamamlanması gereken adımların sıra numaraları; boşsa adım hemen başlayabilir.</param>
/// <param name="IsSubcontracted">Adım fasonda yapılır.</param>
/// <param name="SubcontractorId">Fason adımın varsayılan fasoncusu (tedarikçi); fason olmayan adımda yok sayılır.</param>
public sealed record RouteOperationRequest(
    int SequenceNo,
    int OperationId,
    decimal? Sam = null,
    IReadOnlyList<int>? Predecessors = null,
    bool IsSubcontracted = false,
    int? SubcontractorId = null);

/// <summary>Bir rota adımının fason ayarı; etkin rotada da değiştirilebilir (akışı ve SAM'i değiştirmez).</summary>
public sealed record RouteOperationSubcontractRequest(bool IsSubcontracted, int? SubcontractorId);

/// <param name="ModelId">Yalnızca oluşturmada kullanılır; rotanın modeli sonradan değişmez.</param>
/// <param name="RowVersion">Güncellemede zorunlu; oluşturmada gönderilmez.</param>
public sealed record ProductionRouteRequest(
    int ModelId,
    string? Name,
    string? Description,
    IReadOnlyList<RouteOperationRequest> Operations,
    string? RowVersion = null);

public sealed record RouteVersionRequest(string RowVersion);

public sealed class ProductionRouteRequestValidator : AbstractValidator<ProductionRouteRequest>
{
    public const int MaxOperations = 300;
    public const int MaxSequenceNo = 99_999;

    public ProductionRouteRequestValidator()
    {
        RuleFor(x => x.ModelId).GreaterThan(0).WithMessage("Model seçilmelidir.");
        RuleFor(x => x.Name).MaximumLength(100).WithMessage("Ad en fazla 100 karakter olabilir.");
        RuleFor(x => x.Description).MaximumLength(500).WithMessage("Açıklama en fazla 500 karakter olabilir.");

        RuleFor(x => x.Operations).NotNull().WithMessage("Operasyon listesi zorunludur.");
        When(x => x.Operations is not null, () =>
        {
            RuleFor(x => x.Operations)
                .Must(operations => operations.Count <= MaxOperations)
                .WithMessage($"Bir rotada en fazla {MaxOperations} operasyon olabilir.")
                .Must(operations => operations.All(o => o.SequenceNo is >= 1 and <= MaxSequenceNo))
                .WithMessage($"Sıra numarası 1 ile {MaxSequenceNo} arasında olmalıdır.")
                .Must(operations => operations.All(o => o.OperationId > 0))
                .WithMessage("Her adımda operasyon seçilmelidir.")
                .Must(operations => operations.All(o => o.Sam is null or (>= 0 and <= 9999)))
                .WithMessage("SAM 0 ile 9999 dakika arasında olmalıdır.")
                .Must(operations => operations.All(o => o.Sam is null || decimal.Round(o.Sam.Value, 4) == o.Sam.Value))
                .WithMessage("SAM en fazla 4 ondalık basamak içerebilir.");
        });
    }
}

public sealed class RouteVersionRequestValidator : AbstractValidator<RouteVersionRequest>
{
    public RouteVersionRequestValidator()
    {
        RuleFor(x => x.RowVersion).NotEmpty().WithMessage("Kayıt sürümü (rowVersion) zorunludur.");
    }
}
