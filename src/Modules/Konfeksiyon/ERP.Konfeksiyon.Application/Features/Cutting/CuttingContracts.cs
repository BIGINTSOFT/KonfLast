using ERP.Core.Application.Common;
using ERP.Core.Domain.Enums;
using ERP.Konfeksiyon.Domain.Enums;
using FluentValidation;

namespace ERP.Konfeksiyon.Application.Features.Cutting;

public sealed class CuttingPlanListQuery : PageRequest
{
    /// <summary>Plan no, sipariş no ya da ürün kodu içinde aranır.</summary>
    public string? Search { get; set; }

    public CuttingPlanStatus? Status { get; set; }
    public int? ProductionOrderId { get; set; }
}

/// <param name="PiecesPerPly">Bir kattan çıkan parça (oranların toplamı).</param>
/// <param name="PlannedQuantity">Oran × planlanan kat.</param>
/// <param name="CutQuantity">Kesimde çıkan toplam adet (kesilmemiş planda boş).</param>
/// <param name="PlannedFabric">(Pastal boyu + kat başı pay) × planlanan kat, metre.</param>
public sealed record CuttingPlanListItemDto(
    int Id,
    string PlanNo,
    int ProductionOrderId,
    string OrderNo,
    string ProductCode,
    string ModelCode,
    string ColorName,
    decimal MarkerLength,
    int PlannedPlies,
    int? ActualPlies,
    int PiecesPerPly,
    int PlannedQuantity,
    int? CutQuantity,
    decimal PlannedFabric,
    CuttingPlanStatus Status,
    DateTime CreatedAt,
    DateTime? CutAt);

/// <param name="Ordered">Siparişteki adet.</param>
/// <param name="Remaining">
/// Bu plan dışında kesilecek kalan: sipariş − kesilmiş planlar − plansız demetler (eksi olabilir: fazla kesim).
/// </param>
/// <param name="Difference">(Kesilen ya da planlanan) − kalan: artı fazla, eksi eksik.</param>
public sealed record CuttingPlanSizeDto(
    int SizeId,
    string SizeName,
    int Ratio,
    int PlannedQuantity,
    int Ordered,
    int Remaining,
    int Difference,
    int? CutQuantity);

/// <param name="Balance">Topun şu anki toplam bakiyesi (bütün depo ve adresler).</param>
/// <param name="Locations">Topun durduğu yerler, örn. "HAMMADDE / A-01-03".</param>
/// <param name="UsedQuantity">Kesimde bu toptan düşülen miktar (kesilmemiş planda boş).</param>
public sealed record CuttingPlanLotDto(
    int Id,
    int LotId,
    string LotNo,
    int StockItemId,
    string StockItemCode,
    string StockItemName,
    StockUnit Unit,
    string? BatchNo,
    string? Shade,
    decimal? Width,
    string? SupplierRollNo,
    decimal Balance,
    string Locations,
    int SortOrder,
    decimal? UsedQuantity);

public sealed record CuttingPlanBundleDto(int Id, string BundleNo, string SizeName, int Quantity, BundleStatus Status);

/// <param name="SuggestedPlies">Kalan adetleri bu oranla karşılayan en küçük kat sayısı.</param>
/// <param name="ActualFabric">Kesimde toplardan düşülen toplam (kartın biriminde); toplar atanmamışsa boş.</param>
/// <param name="ShadeKey">Toplardan çıkan ton (ton yoksa parti) bilgisi; atanmış top yoksa boş.</param>
/// <param name="CanCut">Sipariş şu an kesilebilir / demetlenebilir mi.</param>
/// <param name="Warnings">Fazla / eksik kesim, yetersiz kumaş gibi engellemeyen uyarılar.</param>
/// <param name="HasRecipe">Ürünün reçetesi var: kesimde kumaş dışındaki malzeme de demetlerle düşülür.</param>
public sealed record CuttingPlanDto(
    int Id,
    string PlanNo,
    int ProductionOrderId,
    string OrderNo,
    ProductionOrderStatus OrderStatus,
    string CustomerName,
    string ProductCode,
    string ProductName,
    string ModelCode,
    int ColorId,
    string ColorName,
    decimal MarkerLength,
    decimal EndAllowance,
    int PlannedPlies,
    int? ActualPlies,
    int PiecesPerPly,
    int PlannedQuantity,
    int? CutQuantity,
    decimal PlannedFabric,
    decimal? ActualFabric,
    int SuggestedPlies,
    string? ShadeKey,
    CuttingPlanStatus Status,
    string? Notes,
    bool CanEdit,
    bool CanCut,
    string? CutBlockedReason,
    IReadOnlyList<string> Warnings,
    DateTime CreatedAt,
    string? CreatedBy,
    DateTime? CutAt,
    string? CutBy,
    string RowVersion,
    IReadOnlyList<CuttingPlanSizeDto> Sizes,
    IReadOnlyList<CuttingPlanLotDto> Lots,
    IReadOnlyList<CuttingPlanBundleDto> Bundles,
    bool HasRecipe);

/// <param name="Cut">Kesilmiş planlarda bu renk-bedenden çıkan adet.</param>
/// <param name="BundledOutside">Plan dışında (elle / toplu) açılmış, iptal edilmemiş demetlerdeki adet.</param>
/// <param name="Remaining">Sipariş − kesilen − plansız demetlenen; eksi değer fazla kesimdir.</param>
/// <param name="PlannedInDrafts">Taslak planlarda bu renk-beden için planlanan adet (oran × kat).</param>
public sealed record CuttingSummaryItemDto(
    int ColorId,
    string ColorName,
    int SizeId,
    string SizeName,
    int Ordered,
    int Cut,
    int BundledOutside,
    int Remaining,
    int PlannedInDrafts);

/// <param name="CanPlan">Sipariş için kesim planı açılabilir mi (onaylı ya da üretimde).</param>
/// <param name="CanCut">Kesim kaydedilebilir mi (ayrıca modelin etkin rotası gerekir: kesim demet açar).</param>
public sealed record OrderCuttingSummaryDto(
    int ProductionOrderId,
    string OrderNo,
    ProductionOrderStatus Status,
    bool CanPlan,
    string? PlanBlockedReason,
    bool CanCut,
    string? CutBlockedReason,
    IReadOnlyList<CuttingSummaryItemDto> Items,
    IReadOnlyList<CuttingPlanListItemDto> Plans);

public sealed class CuttingLotQuery
{
    /// <summary>Top no, kumaş kodu / adı, parti ya da ton içinde aranır.</summary>
    public string? Search { get; set; }

    public int? StockItemId { get; set; }

    /// <summary>Verilirse siparişin reçetesindeki kumaşlar önce gelir.</summary>
    public int? ProductionOrderId { get; set; }

    public int Take { get; set; } = 100;
}

/// <param name="InRecipe">Kumaş siparişin ürün reçetesinde var.</param>
public sealed record CuttingLotOptionDto(
    int LotId,
    string LotNo,
    int StockItemId,
    string StockItemCode,
    string StockItemName,
    StockUnit Unit,
    string? BatchNo,
    string? Shade,
    decimal? Width,
    string? SupplierRollNo,
    decimal Balance,
    string Locations,
    bool InRecipe);

public sealed record CuttingPlanSizeRequest(int SizeId, int Ratio);

/// <param name="EndAllowance">Kat başı uç payı (m); boşsa 0,03.</param>
/// <param name="LotIds">Pastalda kullanılacak toplar, serim sırasıyla; hepsi aynı kumaş ve aynı tondan olmalıdır.</param>
/// <param name="RowVersion">Güncellemede zorunludur.</param>
public sealed record CuttingPlanRequest(
    int ProductionOrderId,
    int ColorId,
    decimal MarkerLength,
    decimal? EndAllowance,
    int PlannedPlies,
    IReadOnlyList<CuttingPlanSizeRequest> Sizes,
    IReadOnlyList<int>? LotIds = null,
    string? Notes = null,
    string? RowVersion = null);

public sealed record CuttingPlanVersionRequest(string RowVersion);

public sealed record CutSizeRequest(int SizeId, int Quantity);

public sealed record CutLotUsageRequest(int LotId, decimal Quantity);

/// <param name="Sizes">Beden bazında kesilen adet; gönderilmeyen beden oran × gerçek kat sayılır.</param>
/// <param name="BundleSize">Kesilen adetler bu boyda demetlere bölünür; artan adet son demete kalır.</param>
/// <param name="LotUsages">
/// Toplardan kullanılan miktar (kartın biriminde). Boşsa (pastal boyu + pay) × gerçek kat, toplara serim sırasıyla
/// dağıtılır (yalnızca metreyle izlenen kumaşta).
/// </param>
/// <param name="WarehouseId">Kumaş dışındaki reçete malzemesinin düşüleceği depo; boşsa siparişin / tek hammadde deposu.</param>
public sealed record CuttingPlanCutRequest(
    string RowVersion,
    int ActualPlies,
    int BundleSize,
    IReadOnlyList<CutSizeRequest>? Sizes = null,
    IReadOnlyList<CutLotUsageRequest>? LotUsages = null,
    int? WarehouseId = null);

public sealed class CuttingPlanRequestValidator : AbstractValidator<CuttingPlanRequest>
{
    public CuttingPlanRequestValidator()
    {
        RuleFor(x => x.ProductionOrderId).GreaterThan(0).WithMessage("Üretim siparişi seçilmelidir.");
        RuleFor(x => x.ColorId).GreaterThan(0).WithMessage("Renk seçilmelidir.");
        RuleFor(x => x.MarkerLength).GreaterThan(0).WithMessage("Pastal boyu 0 ile 100 m arasında olmalıdır.").LessThanOrEqualTo(100).WithMessage("Pastal boyu 0 ile 100 m arasında olmalıdır.");
        RuleFor(x => x.EndAllowance)
            .InclusiveBetween(0, 1).WithMessage("Kat başı pay 0 ile 1 m arasında olmalıdır.")
            .When(x => x.EndAllowance is not null);
        RuleFor(x => x.PlannedPlies).InclusiveBetween(1, 1000).WithMessage("Kat sayısı 1 ile 1000 arasında olmalıdır.");
        RuleFor(x => x.Sizes).NotEmpty().WithMessage("En az bir beden için oran girilmelidir.");
        RuleFor(x => x.Sizes)
            .Must(sizes => sizes is null || sizes.Select(s => s.SizeId).Distinct().Count() == sizes.Count)
            .WithMessage("Aynı beden birden fazla kez girilemez.");
        RuleForEach(x => x.Sizes).ChildRules(size =>
        {
            size.RuleFor(s => s.SizeId).GreaterThan(0).WithMessage("Beden seçilmelidir.");
            size.RuleFor(s => s.Ratio).InclusiveBetween(1, 100).WithMessage("Oran 1 ile 100 arasında olmalıdır.");
        });
        RuleFor(x => x.LotIds)
            .Must(ids => ids is null || ids.Distinct().Count() == ids.Count)
            .WithMessage("Aynı top birden fazla kez seçilemez.");
        RuleFor(x => x.Notes).MaximumLength(500).WithMessage("Not en fazla 500 karakter olabilir.");
    }
}

public sealed class CuttingPlanVersionRequestValidator : AbstractValidator<CuttingPlanVersionRequest>
{
    public CuttingPlanVersionRequestValidator()
    {
        RuleFor(x => x.RowVersion).NotEmpty().WithMessage("Kayıt sürümü (rowVersion) zorunludur.");
    }
}

public sealed class CuttingPlanCutRequestValidator : AbstractValidator<CuttingPlanCutRequest>
{
    public CuttingPlanCutRequestValidator()
    {
        RuleFor(x => x.RowVersion).NotEmpty().WithMessage("Kayıt sürümü (rowVersion) zorunludur.");
        RuleFor(x => x.ActualPlies).InclusiveBetween(1, 1000).WithMessage("Gerçek kat sayısı 1 ile 1000 arasında olmalıdır.");
        RuleFor(x => x.BundleSize).GreaterThan(0).WithMessage("Demet adedi 1 veya daha fazla olmalıdır.");
        RuleFor(x => x.Sizes)
            .Must(sizes => sizes is null || sizes.Select(s => s.SizeId).Distinct().Count() == sizes.Count)
            .WithMessage("Aynı beden birden fazla kez girilemez.");
        RuleForEach(x => x.Sizes).ChildRules(size =>
            size.RuleFor(s => s.Quantity).InclusiveBetween(0, 100_000).WithMessage("Kesilen adet 0 ile 100.000 arasında olmalıdır."));
        RuleFor(x => x.LotUsages)
            .Must(lots => lots is null || lots.Select(l => l.LotId).Distinct().Count() == lots.Count)
            .WithMessage("Aynı top birden fazla kez girilemez.");
        RuleForEach(x => x.LotUsages).ChildRules(lot =>
            lot.RuleFor(l => l.Quantity).InclusiveBetween(0, 100_000).WithMessage("Kullanılan miktar 0 ile 100.000 arasında olmalıdır."));
    }
}
