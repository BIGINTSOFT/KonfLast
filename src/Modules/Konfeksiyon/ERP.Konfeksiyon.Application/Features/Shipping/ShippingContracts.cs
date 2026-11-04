using System.ComponentModel.DataAnnotations;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Konfeksiyon.Application.Features.Warehousing;
using ERP.Core.Domain.Enums;
using ERP.Konfeksiyon.Domain.Enums;
using FluentValidation;

namespace ERP.Konfeksiyon.Application.Features.Shipping;

public sealed class ShippingOptions
{
    public const string SectionName = "Shipping";

    /// <summary>Sevkiyat numarası öneki: {önek}-{yıl}-{sıra}, örn. SV-2026-00031.</summary>
    [Required, RegularExpression("^[A-Z0-9]{1,10}$")]
    public string NumberPrefix { get; set; } = "SV";

    [Range(3, 9)]
    public int NumberDigits { get; set; } = 5;

    /// <summary>
    /// Siparişe bağlı sevkiyatta sipariş adedinin en çok yüzde kaç üzerine çıkılabileceği (renk-beden bazında).
    /// 0: sipariş adedi aşılamaz.
    /// </summary>
    [Range(0, 100)]
    public decimal OverShipTolerancePercent { get; set; }

    /// <summary>
    /// İrsaliye numarası öneki (e-İrsaliye: 3 karakter). Numara {önek}{yıl}{9 haneli sıra} biçimindedir,
    /// örn. IRS2026000000031; irsaliye kesilirken numara elle verilmemişse sistem verir.
    /// </summary>
    [Required, RegularExpression("^[A-Z0-9]{3}$")]
    public string DespatchPrefix { get; set; } = "IRS";

    /// <summary>İrsaliyede gönderen (firma) vergi dairesi ve vergi kimlik numarası; e-İrsaliye taslağında basılır.</summary>
    public string? SenderTaxOffice { get; set; }
    public string? SenderTaxNumber { get; set; }
}

/// <param name="CustomerOrderNo">Bağlı müşteri siparişindeki müşterinin kendi sipariş numarası.</param>
/// <param name="CartonCount">Koli sayısı (çeki listesi yoksa 0).</param>
public sealed record ShipmentListItemDto(
    int Id,
    string ShipmentNo,
    int CustomerId,
    string CustomerName,
    string WarehouseCode,
    string? OrderNo,
    DateOnly ShipDate,
    ShipmentStatus Status,
    string? DocumentNo,
    int ItemCount,
    decimal TotalQuantity,
    string? CustomerOrderNo,
    int CartonCount,
    decimal PickedQuantity = 0,
    string? PickingBy = null);

/// <param name="AvailableQuantity">Kartın sevkiyat deposundaki şu anki stoğu (taslakta yol gösterir).</param>
/// <param name="PickedQuantity">Depodan toplanan adet.</param>
/// <param name="RequestedQuantity">Toplama eksik bittiyse ilk istenen adet.</param>
/// <param name="CheckedQuantity">İrsaliye öncesi kontrolde sayılan adet.</param>
public sealed record ShipmentItemDto(
    int Id, int StockItemId, string ItemCode, string ItemName, StockUnit Unit, decimal Quantity, decimal AvailableQuantity,
    decimal PickedQuantity = 0, decimal? RequestedQuantity = null, decimal? CheckedQuantity = null);

/// <summary>Sevkiyatın toplama ve irsaliye durumu.</summary>
/// <param name="PickingBy">Toplamayı üstlenmiş kullanıcı.</param>
/// <param name="DespatchUuid">İrsaliyenin ETTN'si (irsaliye kesildikten sonra).</param>
public sealed record ShipmentWorkflowDto(
    bool CanSendToPicking,
    bool CanReturnToDraft,
    bool CanCompletePicking,
    bool CanReopenPicking,
    string? PickingBy,
    DateTime? PickingStartedAt,
    DateTime? PickedAt,
    string? PickedBy,
    string? VehiclePlate,
    string? DriverName,
    string? DriverIdentityNo,
    Guid? DespatchUuid);

/// <param name="RowVersion">Güncelleme ve durum işlemlerinde geri gönderilir (eşzamanlılık kontrolü).</param>
public sealed record ShipmentDto(
    int Id,
    string ShipmentNo,
    int CustomerId,
    string CustomerCode,
    string CustomerName,
    int WarehouseId,
    string WarehouseCode,
    string WarehouseName,
    int? ProductionOrderId,
    string? OrderNo,
    DateOnly ShipDate,
    string? DocumentNo,
    string? Carrier,
    string? Notes,
    ShipmentStatus Status,
    DateTime? ShippedAt,
    string? ShippedBy,
    decimal TotalQuantity,
    bool IsEditable,
    bool CanShip,
    bool CanCancel,
    string RowVersion,
    DateTime CreatedAt,
    string? CreatedBy,
    IReadOnlyList<ShipmentItemDto> Items,
    int? SalesOrderId = null,
    string? SalesOrderNo = null,
    string? CustomerOrderNo = null,
    ShipmentCartonSummaryDto? Cartons = null,
    ShipmentWorkflowDto? Workflow = null);

/// <summary>Sevkiyatın koli ve yükleme özeti (ayrıntıda uyarı ve sevk onayı için).</summary>
/// <param name="MatchesItems">Koli yoksa ya da kolilerdeki adetler kalemlerle birebir tutuyorsa true.</param>
public sealed record ShipmentCartonSummaryDto(int CartonCount, int LoadedCount, bool MatchesItems);

public sealed class ShipmentListQuery : PageRequest
{
    /// <summary>Sevkiyat no, irsaliye no, müşteri adı, sipariş no ya da müşteri sipariş no içinde aranır.</summary>
    public string? Search { get; set; }
    public ShipmentStatus? Status { get; set; }
    public int? CustomerId { get; set; }
    public int? ProductionOrderId { get; set; }
    public int? SalesOrderId { get; set; }

    /// <summary>Sevk tarihi bu günden önce olmayanlar (dahil).</summary>
    public DateOnly? From { get; set; }

    /// <summary>Sevk tarihi bu günden sonra olmayanlar (dahil).</summary>
    public DateOnly? To { get; set; }
}

public sealed record ShipmentItemRequest(int StockItemId, decimal Quantity);

/// <param name="ProductionOrderId">Sevkiyat bir üretim siparişine aitse; kalemler o siparişin ürünü olmalıdır.</param>
/// <param name="RowVersion">Güncellemede zorunlu; oluşturmada gönderilmez.</param>
public sealed record ShipmentRequest(
    int CustomerId,
    int WarehouseId,
    DateOnly ShipDate,
    IReadOnlyList<ShipmentItemRequest> Items,
    int? ProductionOrderId = null,
    string? DocumentNo = null,
    string? Carrier = null,
    string? Notes = null,
    string? RowVersion = null,
    int? SalesOrderId = null);

public sealed record ShipmentTransitionRequest(string RowVersion);

/// <summary>Son kontrolde bir kalem için sayılan adet.</summary>
public sealed record ShipmentCheckLine(int ShipmentItemId, decimal Quantity);

/// <summary>
/// İrsaliye kesme: toplanmış sevkiyatın kalemleri son kez sayılır; sayılan adetler toplananla birebir tutmalıdır.
/// </summary>
/// <param name="DocumentNo">İrsaliye numarası; boşsa sistem verir.</param>
public sealed record ShipmentDispatchRequest(
    string RowVersion,
    IReadOnlyList<ShipmentCheckLine> Checks,
    string? DocumentNo = null,
    string? Carrier = null,
    string? VehiclePlate = null,
    string? DriverName = null,
    string? DriverIdentityNo = null);

public sealed class ShipmentDispatchRequestValidator : AbstractValidator<ShipmentDispatchRequest>
{
    public ShipmentDispatchRequestValidator()
    {
        RuleFor(x => x.RowVersion).NotEmpty().WithMessage("Sevkiyat sürümü (rowVersion) zorunludur.");
        RuleFor(x => x.Checks).NotNull().WithMessage("Kontrol adetleri zorunludur.");
        RuleForEach(x => x.Checks)
            .Must(c => c.Quantity >= 0 && c.Quantity <= StockRules.MaxQuantity && decimal.Round(c.Quantity, 3) == c.Quantity)
            .WithMessage("Kontrol adedi sıfır ya da pozitif olmalı ve en fazla 3 ondalık basamak içermelidir.");
        RuleFor(x => x.DocumentNo).OptionalText(50);
        RuleFor(x => x.Carrier).OptionalText(150);
        RuleFor(x => x.VehiclePlate).OptionalText(20);
        RuleFor(x => x.DriverName).OptionalText(100);
        RuleFor(x => x.DriverIdentityNo)
            .Matches("^[1-9][0-9]{10}$").When(x => !string.IsNullOrWhiteSpace(x.DriverIdentityNo))
            .WithMessage("Şoför T.C. kimlik numarası 11 haneli olmalıdır.");
    }
}

public sealed class ShipmentRequestValidator : AbstractValidator<ShipmentRequest>
{
    public const int MaxItems = 200;

    public ShipmentRequestValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0).WithMessage("Müşteri seçilmelidir.");
        RuleFor(x => x.WarehouseId).GreaterThan(0).WithMessage("Depo seçilmelidir.");
        RuleFor(x => x.ShipDate).NotEqual(default(DateOnly)).WithMessage("Sevk tarihi zorunludur.");
        RuleFor(x => x.DocumentNo).OptionalText(50);
        RuleFor(x => x.Carrier).OptionalText(150);
        RuleFor(x => x.Notes).MaximumLength(1000).WithMessage("Not en fazla 1000 karakter olabilir.");

        RuleFor(x => x.Items).NotNull().WithMessage("Kalem listesi zorunludur.");
        When(x => x.Items is not null, () =>
        {
            RuleFor(x => x.Items)
                .Must(items => items.Count <= MaxItems).WithMessage($"Bir sevkiyatta en fazla {MaxItems} kalem olabilir.")
                .Must(items => items.All(i => i.StockItemId > 0)).WithMessage("Her kalemde mamul kartı seçilmelidir.")
                .Must(items => items.All(i =>
                    i.Quantity > 0 && i.Quantity <= StockRules.MaxQuantity && decimal.Round(i.Quantity, 3) == i.Quantity))
                .WithMessage("Kalem miktarı sıfırdan büyük olmalı ve en fazla 3 ondalık basamak içermelidir.")
                .Must(items => items.Select(i => i.StockItemId).Distinct().Count() == items.Count)
                .WithMessage("Aynı mamul kartı birden fazla kalemde yer alamaz.");
        });
    }
}

public sealed class ShipmentTransitionRequestValidator : AbstractValidator<ShipmentTransitionRequest>
{
    public ShipmentTransitionRequestValidator()
    {
        RuleFor(x => x.RowVersion).NotEmpty().WithMessage("Sevkiyat sürümü (rowVersion) zorunludur.");
    }
}
