using ERP.Core.Application.Common;
using ERP.Konfeksiyon.Domain.Enums;
using FluentValidation;

namespace ERP.Konfeksiyon.Application.Features.ProductionOrders;

public sealed record ProductionOrderListItemDto(
    int Id,
    string OrderNo,
    string? CustomerOrderNo,
    int CustomerId,
    string CustomerName,
    int ProductId,
    string ProductCode,
    string ProductName,
    string ModelCode,
    DateOnly OrderDate,
    DateOnly DueDate,
    ProductionOrderStatus Status,
    int TotalQuantity,
    string? SalesOrderNo = null);

public sealed record ProductionOrderItemDto(
    int Id,
    int ColorId,
    string ColorCode,
    string ColorName,
    int SizeId,
    string SizeCode,
    string SizeName,
    int SizeSortOrder,
    int Quantity);

/// <param name="AllowedTransitions">Siparişin şu an geçebileceği durumlar; arayüz işlemleri buna göre sunar.</param>
/// <param name="RowVersion">Güncelleme ve durum işlemlerinde geri gönderilir (eşzamanlılık kontrolü).</param>
/// <param name="MaterialWarehouseId">Reçete malzemesinin ayrılacağı ve düşüleceği depo; boşsa tek hammadde deposu.</param>
/// <param name="SalesOrderId">Sipariş bir müşteri siparişinden açıldıysa o sipariş.</param>
public sealed record ProductionOrderDto(
    int Id,
    string OrderNo,
    string? CustomerOrderNo,
    int CustomerId,
    string CustomerCode,
    string CustomerName,
    int ProductId,
    string ProductCode,
    string ProductName,
    string ModelCode,
    DateOnly OrderDate,
    DateOnly DueDate,
    ProductionOrderStatus Status,
    string? Notes,
    int TotalQuantity,
    bool IsEditable,
    IReadOnlyList<ProductionOrderStatus> AllowedTransitions,
    string RowVersion,
    DateTime CreatedAt,
    string? CreatedBy,
    IReadOnlyList<ProductionOrderItemDto> Items,
    int? MaterialWarehouseId = null,
    int? SalesOrderId = null,
    string? SalesOrderNo = null,
    OperationSequenceMode OperationSequence = OperationSequenceMode.Sequential,
    bool CanChangeOperationSequence = false);

public sealed class ProductionOrderListQuery : PageRequest
{
    /// <summary>Sipariş no, müşteri sipariş no, ürün kodu veya müşteri adı içinde aranır.</summary>
    public string? Search { get; set; }
    public ProductionOrderStatus? Status { get; set; }
    public int? CustomerId { get; set; }

    /// <summary>Termin tarihi aralığı (dahil).</summary>
    public DateOnly? DueFrom { get; set; }
    public DateOnly? DueTo { get; set; }
}

public sealed record ProductionOrderItemRequest(int ColorId, int SizeId, int Quantity);

/// <param name="RowVersion">Güncellemede zorunlu; oluşturmada gönderilmez.</param>
/// <param name="MaterialWarehouseId">Reçete malzemesinin ayrılacağı ve düşüleceği depo (isteğe bağlı).</param>
public sealed record ProductionOrderRequest(
    int CustomerId,
    int ProductId,
    string? CustomerOrderNo,
    DateOnly OrderDate,
    DateOnly DueDate,
    string? Notes,
    IReadOnlyList<ProductionOrderItemRequest> Items,
    string? RowVersion = null,
    int? MaterialWarehouseId = null,
    OperationSequenceMode OperationSequence = OperationSequenceMode.Sequential);

/// <summary>Durum işlemlerinde (onayla, iptal ...) siparişin görülen sürümü gönderilir.</summary>
public sealed record ProductionOrderTransitionRequest(string RowVersion);

/// <summary>Sipariş onaylandıktan sonra, ilk demet açılana kadar operasyon sırası değiştirilebilir.</summary>
public sealed record OperationSequenceRequest(OperationSequenceMode OperationSequence, string RowVersion);

public sealed class ProductionOrderRequestValidator : AbstractValidator<ProductionOrderRequest>
{
    public const int MaxItemQuantity = 1_000_000;

    public ProductionOrderRequestValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0).WithMessage("Müşteri seçilmelidir.");
        RuleFor(x => x.ProductId).GreaterThan(0).WithMessage("Ürün seçilmelidir.");
        RuleFor(x => x.OperationSequence)
            .Must(mode => mode is OperationSequenceMode.Sequential or OperationSequenceMode.Free)
            .WithMessage("Operasyon sırası Sıralı ya da Sırasız olmalıdır.");
        RuleFor(x => x.CustomerOrderNo).MaximumLength(50).WithMessage("En fazla 50 karakter olabilir.");
        RuleFor(x => x.Notes).MaximumLength(1000).WithMessage("Not en fazla 1000 karakter olabilir.");
        RuleFor(x => x.OrderDate).NotEqual(default(DateOnly)).WithMessage("Sipariş tarihi zorunludur.");
        RuleFor(x => x.DueDate)
            .NotEqual(default(DateOnly)).WithMessage("Termin tarihi zorunludur.")
            .GreaterThanOrEqualTo(x => x.OrderDate).WithMessage("Termin tarihi sipariş tarihinden önce olamaz.");

        RuleFor(x => x.Items).NotNull().WithMessage("Kalem listesi zorunludur.");
        When(x => x.Items is not null, () =>
        {
            RuleFor(x => x.Items)
                .Must(items => items.All(i => i.ColorId > 0 && i.SizeId > 0))
                .WithMessage("Her kalemde renk ve beden seçilmelidir.")
                .Must(items => items.All(i => i.Quantity is >= 1 and <= MaxItemQuantity))
                .WithMessage($"Kalem adedi 1 ile {MaxItemQuantity:N0} arasında olmalıdır.")
                .Must(items => items.Select(i => (i.ColorId, i.SizeId)).Distinct().Count() == items.Count)
                .WithMessage("Aynı renk-beden birden fazla kez girilemez.");
        });
    }
}

public sealed class ProductionOrderTransitionRequestValidator : AbstractValidator<ProductionOrderTransitionRequest>
{
    public ProductionOrderTransitionRequestValidator()
    {
        RuleFor(x => x.RowVersion).NotEmpty().WithMessage("Sipariş sürümü (rowVersion) zorunludur.");
    }
}

public sealed class OperationSequenceRequestValidator : AbstractValidator<OperationSequenceRequest>
{
    public OperationSequenceRequestValidator()
    {
        RuleFor(x => x.OperationSequence)
            .Must(mode => mode is OperationSequenceMode.Sequential or OperationSequenceMode.Free)
            .WithMessage("Operasyon sırası Sıralı ya da Sırasız olmalıdır.");
        RuleFor(x => x.RowVersion).NotEmpty().WithMessage("Kayıt sürümü (rowVersion) zorunludur.");
    }
}
