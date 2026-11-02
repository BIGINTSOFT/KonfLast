using System.ComponentModel.DataAnnotations;
using ERP.Core.Application.Common;
using ERP.Konfeksiyon.Domain.Enums;
using FluentValidation;

namespace ERP.Konfeksiyon.Application.Features.Bundles;

public sealed class BundleOptions
{
    public const string SectionName = "Bundles";

    /// <summary>Demet numarasının basamak sayısı (000154).</summary>
    [Range(4, 10)]
    public int NumberDigits { get; set; } = 6;

    /// <summary>Tek demete konabilecek en fazla parça.</summary>
    [Range(1, 100_000)]
    public int MaxBundleSize { get; set; } = 1000;

    /// <summary>Tek istekte oluşturulabilecek en fazla demet.</summary>
    [Range(1, 10_000)]
    public int MaxBundlesPerRequest { get; set; } = 1000;

    /// <summary>
    /// Sipariş kalemi adedinin yüzde kaç üzerinde demetlenebileceği (fire payı). 0: sipariş adedi aşılamaz.
    /// </summary>
    [Range(0, 100)]
    public decimal OverageTolerancePercent { get; set; }
}

public sealed record BundleListItemDto(
    int Id,
    string BundleNo,
    int ProductionOrderId,
    string OrderNo,
    string ProductCode,
    string ModelCode,
    string ColorName,
    string SizeName,
    int Quantity,
    BundleStatus Status,
    string? RfidUid,
    int CompletedOperations,
    int TotalOperations,
    DateTime CreatedAt);

/// <param name="Predecessors">Bu adımdan önce tamamlanması gereken adımların sıra numaraları.</param>
/// <param name="Stage">Akıştaki aşama (0'dan başlar); aynı aşamadaki adımlar paralel yürür.</param>
public sealed record BundleOperationDto(
    long Id,
    int SequenceNo,
    int OperationId,
    string OperationCode,
    string OperationName,
    decimal Sam,
    BundleOperationStatus Status,
    int PlannedQuantity,
    int ProducedQuantity,
    int RejectedQuantity,
    int RemainingQuantity,
    string? MachineCode,
    string? OperatorName,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    IReadOnlyList<int> Predecessors,
    int Stage,
    int ReworkQuantity = 0);

/// <param name="CurrentOperations">Demetin şu an bulunduğu yer: işlemdeki, yoksa başlatılabilir adımlar.</param>
/// <param name="RfidUid">Demete atanmış kartın UID'si (yoksa boş).</param>
/// <param name="CanAssignCard">Demete şu an kart atanabilir mi (açık demet ve kartı yok).</param>
/// <param name="RowVersion">Değiştiren işlemlerde geri gönderilir (eşzamanlılık kontrolü).</param>
public sealed record BundleDto(
    int Id,
    string BundleNo,
    int ProductionOrderId,
    string OrderNo,
    string CustomerName,
    string ProductCode,
    string ProductName,
    string ModelCode,
    int RouteVersion,
    int ColorId,
    string ColorName,
    int SizeId,
    string SizeName,
    int Quantity,
    BundleStatus Status,
    string? RfidUid,
    bool CanAssignCard,
    bool CanCancel,
    IReadOnlyList<string> CurrentOperations,
    string RowVersion,
    DateTime CreatedAt,
    string? CreatedBy,
    IReadOnlyList<BundleOperationDto> Operations,
    OperationSequenceMode SequenceMode = OperationSequenceMode.Sequential,
    bool AtSubcontractor = false);

public sealed class BundleListQuery : PageRequest
{
    /// <summary>Demet no veya sipariş no içinde aranır.</summary>
    public string? Search { get; set; }
    public int? ProductionOrderId { get; set; }
    public BundleStatus? Status { get; set; }
    public int? ColorId { get; set; }
    public int? SizeId { get; set; }
}

/// <param name="Allowed">Tolerans dahil demetlenebilecek en fazla adet.</param>
/// <param name="Bundled">İptal edilmemiş demetlerdeki toplam adet.</param>
public sealed record BundleSummaryItemDto(
    int ColorId, string ColorName, int SizeId, string SizeName, int Ordered, int Allowed, int Bundled, int Remaining,
    int BundleCount);

/// <summary>Siparişin (iptal edilmemiş) demetlerinin akıştaki dağılımı.</summary>
/// <param name="WithoutCard">Kartı atanmamış, üretime verilmemiş demetler.</param>
/// <param name="InProduction">Sahadaki demetler (bekleyen, işlemde, kısmen tamamlanmış, tamirde, kalite bekletmesinde).</param>
/// <param name="Completed">Tüm operasyonları bitmiş, depoya alınmayı bekleyen demetler.</param>
/// <param name="Closed">Depoya teslim alınmış demetler.</param>
public sealed record OrderBundleProgressDto(int Total, int WithoutCard, int InProduction, int Completed, int Closed);

/// <param name="CanCreateBundles">Sipariş demetlenebilir durumda mı (onaylı / üretimde ve modelin etkin rotası var).</param>
/// <param name="BlockedReason">Demetlenemiyorsa nedeni.</param>
/// <param name="HasRecipe">Ürünün reçetesi var: demet açılırken malzeme depodan düşülür.</param>
public sealed record OrderBundleSummaryDto(
    int ProductionOrderId,
    string OrderNo,
    bool CanCreateBundles,
    string? BlockedReason,
    int Ordered,
    int Bundled,
    int Remaining,
    IReadOnlyList<BundleSummaryItemDto> Items,
    OrderBundleProgressDto? Progress = null,
    bool HasRecipe = false);

/// <param name="WarehouseId">Reçete malzemesinin düşüleceği depo; boşsa tek hammadde deposu kullanılır.</param>
public sealed record CreateBundleRequest(int ProductionOrderId, int ColorId, int SizeId, int Quantity, int? WarehouseId = null);

public sealed record GenerateBundleItem(int ColorId, int SizeId);

/// <param name="BundleSize">Bir demete konacak parça adedi; artan adet son demete kalır.</param>
/// <param name="Items">Yalnızca bu renk-bedenler demetlensin; boşsa siparişin tüm kalemleri.</param>
/// <param name="WarehouseId">Reçete malzemesinin düşüleceği depo; boşsa tek hammadde deposu kullanılır.</param>
public sealed record GenerateBundlesRequest(
    int BundleSize, IReadOnlyList<GenerateBundleItem>? Items = null, int? WarehouseId = null);

public sealed record GenerateBundlesResultDto(int BundleCount, int TotalQuantity, IReadOnlyList<BundleListItemDto> Bundles);

public sealed record BundleVersionRequest(string RowVersion);

/// <summary>Kesimde bir renk-bedenden çıkan adet.</summary>
public sealed record CuttingBundleItem(int ColorId, int SizeId, int Quantity);

/// <summary>Kesim planının kesiminden demet açma (servisler arası; API isteği değildir).</summary>
/// <param name="BundleSize">Bir demete konacak parça adedi; artan adet son demete kalır.</param>
/// <param name="SkipMaterialItemIds">Reçeteden düşülmeyecek kartlar: plana atanmış toplardan ayrıca düşülen kumaş.</param>
/// <param name="WarehouseId">Öteki reçete malzemesinin düşüleceği depo; boşsa siparişin / tek hammadde deposu.</param>
public sealed record CuttingBundleRequest(
    int ProductionOrderId,
    int CuttingPlanId,
    int BundleSize,
    IReadOnlyList<CuttingBundleItem> Items,
    IReadOnlyCollection<int> SkipMaterialItemIds,
    int? WarehouseId);

public sealed class CreateBundleRequestValidator : AbstractValidator<CreateBundleRequest>
{
    public CreateBundleRequestValidator()
    {
        RuleFor(x => x.ProductionOrderId).GreaterThan(0).WithMessage("Sipariş seçilmelidir.");
        RuleFor(x => x.ColorId).GreaterThan(0).WithMessage("Renk seçilmelidir.");
        RuleFor(x => x.SizeId).GreaterThan(0).WithMessage("Beden seçilmelidir.");
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Adet 1 veya daha fazla olmalıdır.");
    }
}

public sealed class GenerateBundlesRequestValidator : AbstractValidator<GenerateBundlesRequest>
{
    public GenerateBundlesRequestValidator()
    {
        RuleFor(x => x.BundleSize).GreaterThan(0).WithMessage("Demet adedi 1 veya daha fazla olmalıdır.");
        RuleFor(x => x.Items)
            .Must(items => items is null || items.All(i => i.ColorId > 0 && i.SizeId > 0))
            .WithMessage("Her kalemde renk ve beden belirtilmelidir.")
            .Must(items => items is null || items.Distinct().Count() == items.Count)
            .WithMessage("Aynı renk-beden birden fazla kez belirtilemez.");
    }
}

public sealed class BundleVersionRequestValidator : AbstractValidator<BundleVersionRequest>
{
    public BundleVersionRequestValidator()
    {
        RuleFor(x => x.RowVersion).NotEmpty().WithMessage("Kayıt sürümü (rowVersion) zorunludur.");
    }
}
