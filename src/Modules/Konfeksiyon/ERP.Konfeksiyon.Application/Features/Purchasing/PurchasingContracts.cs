using System.ComponentModel.DataAnnotations;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Konfeksiyon.Application.Features.Warehousing;
using ERP.Core.Domain.Enums;
using ERP.Konfeksiyon.Domain.Enums;
using FluentValidation;

namespace ERP.Konfeksiyon.Application.Features.Purchasing;

public sealed class PurchasingOptions
{
    public const string SectionName = "Purchasing";

    /// <summary>Sipariş numarası öneki: {önek}-{yıl}-{sıra}, örn. SA-2026-00012.</summary>
    [Required, RegularExpression("^[A-Z0-9]{1,10}$")]
    public string NumberPrefix { get; set; } = "SA";

    [Range(3, 9)]
    public int NumberDigits { get; set; } = 5;

    /// <summary>
    /// Mal kabulde sipariş edilen miktarın en çok yüzde kaç üzerine çıkılabileceği (kalem bazında). 0: sipariş
    /// miktarı aşılamaz.
    /// </summary>
    [Range(0, 100)]
    public decimal OverReceiptTolerancePercent { get; set; }
}

public sealed record SupplierDto(
    int Id, string SupplierCode, string Name, string? TaxNumber, string? Phone, string? Email, string? Address, bool IsActive);

public sealed record SupplierRequest(
    string SupplierCode, string Name, string? TaxNumber = null, string? Phone = null, string? Email = null,
    string? Address = null, bool IsActive = true);

public sealed record PurchaseOrderListItemDto(
    int Id,
    string OrderNo,
    int SupplierId,
    string SupplierName,
    DateOnly OrderDate,
    DateOnly? ExpectedDate,
    PurchaseOrderStatus Status,
    int ItemCount,
    decimal? TotalAmount,
    string Currency);

/// <param name="LineAmount">Miktar × birim fiyat; fiyat girilmemişse null.</param>
/// <param name="IsLotTracked">Top / parti takipli kart: mal kabulde toplara bölünebilir.</param>
public sealed record PurchaseOrderItemDto(
    int Id,
    int StockItemId,
    string ItemCode,
    string ItemName,
    StockUnit Unit,
    decimal Quantity,
    decimal? UnitPrice,
    decimal ReceivedQuantity,
    decimal RemainingQuantity,
    decimal? LineAmount,
    bool IsLotTracked = false,
    int? ProductionOrderId = null,
    string? ProductionOrderNo = null);

/// <param name="TotalAmount">Fiyatı girilmiş kalemlerin toplamı; hiçbirinde fiyat yoksa null.</param>
/// <param name="AllowedTransitions">Kullanıcının yapabileceği durum geçişleri (teslim durumları mal kabulle oluşur).</param>
/// <param name="RowVersion">Güncelleme ve durum işlemlerinde geri gönderilir (eşzamanlılık kontrolü).</param>
public sealed record PurchaseOrderDto(
    int Id,
    string OrderNo,
    int SupplierId,
    string SupplierCode,
    string SupplierName,
    DateOnly OrderDate,
    DateOnly? ExpectedDate,
    string Currency,
    PurchaseOrderStatus Status,
    string? Notes,
    decimal? TotalAmount,
    bool IsEditable,
    bool CanReceive,
    IReadOnlyList<PurchaseOrderStatus> AllowedTransitions,
    string RowVersion,
    DateTime CreatedAt,
    string? CreatedBy,
    IReadOnlyList<PurchaseOrderItemDto> Items,
    decimal? ExchangeRate = null,
    string CostCurrency = "TRY");

public sealed class PurchaseOrderListQuery : PageRequest
{
    /// <summary>Sipariş no ya da tedarikçi adı içinde aranır.</summary>
    public string? Search { get; set; }
    public PurchaseOrderStatus? Status { get; set; }
    public int? SupplierId { get; set; }

    /// <summary>true: yalnızca mal kabul bekleyen (onaylı ya da kısmen teslim alınmış) siparişler.</summary>
    public bool? Receivable { get; set; }
}

/// <param name="ProductionOrderId">Kalem bir üretim siparişinin eksik malzemesi için açılıyorsa o sipariş.</param>
public sealed record PurchaseOrderItemRequest(int StockItemId, decimal Quantity, decimal? UnitPrice = null, int? ProductionOrderId = null);

/// <param name="Currency">ISO 4217 para birimi kodu; boşsa TRY.</param>
/// <param name="ExchangeRate">Sipariş para biriminin maliyet para birimine kuru; dövizli siparişte onay için zorunludur.</param>
/// <param name="RowVersion">Güncellemede zorunlu; oluşturmada gönderilmez.</param>
public sealed record PurchaseOrderRequest(
    int SupplierId,
    DateOnly OrderDate,
    DateOnly? ExpectedDate,
    string? Notes,
    IReadOnlyList<PurchaseOrderItemRequest> Items,
    string? Currency = null,
    string? RowVersion = null,
    decimal? ExchangeRate = null);

public sealed record PurchaseOrderTransitionRequest(string RowVersion);

/// <summary>Mal kabulde gelen bir top / parti.</summary>
public sealed record GoodsReceiptLotRequest(
    decimal Quantity, string? BatchNo = null, string? Shade = null, decimal? Width = null, string? SupplierRollNo = null);

/// <param name="PurchaseOrderItemId">Teslim alınan sipariş kalemi.</param>
/// <param name="Lots">
/// Takipli kartta gelen toplar; miktarlarının toplamı kalemin miktarına eşit olmalıdır. Verilmezse kalemin
/// tamamı tek top olarak açılır. Takipsiz kartta gönderilmez.
/// </param>
public sealed record GoodsReceiptLineRequest(
    int PurchaseOrderItemId, decimal Quantity, IReadOnlyList<GoodsReceiptLotRequest>? Lots = null);

/// <param name="RequestId">İstemcinin ürettiği kimlik; aynı kimlikle yinelenen istek mal kabulü ikinci kez yazmaz.</param>
/// <param name="DocumentNo">Tedarikçinin irsaliye numarası.</param>
public sealed record GoodsReceiptRequest(
    Guid RequestId, int WarehouseId, IReadOnlyList<GoodsReceiptLineRequest> Lines, string? DocumentNo = null,
    string? Note = null);

public sealed record GoodsReceiptLineDto(int PurchaseOrderItemId, string ItemCode, string ItemName, StockUnit Unit, decimal Quantity);

public sealed record GoodsReceiptDto(
    long Id,
    int PurchaseOrderId,
    string WarehouseCode,
    string WarehouseName,
    string? DocumentNo,
    string? Note,
    DateTime ReceivedAt,
    string ReceivedBy,
    IReadOnlyList<GoodsReceiptLineDto> Lines);

/// <param name="Duplicate">İstek daha önce işlenmişti; stok ikinci kez değişmedi.</param>
public sealed record GoodsReceiptResultDto(bool Duplicate, GoodsReceiptDto Receipt, PurchaseOrderDto Order);

public sealed class SupplierRequestValidator : AbstractValidator<SupplierRequest>
{
    public SupplierRequestValidator()
    {
        RuleFor(x => x.SupplierCode).ValidCode();
        RuleFor(x => x.Name).ValidName(200);
        RuleFor(x => x.TaxNumber).OptionalText(20);
        RuleFor(x => x.Phone).OptionalText(30);
        RuleFor(x => x.Email).OptionalText(256);
        RuleFor(x => x.Email).EmailAddress().WithMessage("Geçerli bir e-posta adresi girin.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Address).OptionalText(500);
    }
}

public sealed class PurchaseOrderRequestValidator : AbstractValidator<PurchaseOrderRequest>
{
    public const int MaxItems = 200;
    public const decimal MaxUnitPrice = 100_000_000m;

    public PurchaseOrderRequestValidator()
    {
        RuleFor(x => x.SupplierId).GreaterThan(0).WithMessage("Tedarikçi seçilmelidir.");
        RuleFor(x => x.OrderDate).NotEqual(default(DateOnly)).WithMessage("Sipariş tarihi zorunludur.");
        RuleFor(x => x.ExpectedDate)
            .Must((request, expected) => expected is null || expected >= request.OrderDate)
            .WithMessage("Beklenen teslim tarihi sipariş tarihinden önce olamaz.");
        RuleFor(x => x.Notes).MaximumLength(1000).WithMessage("Not en fazla 1000 karakter olabilir.");
        RuleFor(x => x.Currency)
            .Matches("^[A-Za-z]{3}$").WithMessage("Para birimi üç harfli kod olmalıdır (örn. TRY, USD, EUR).")
            .When(x => !string.IsNullOrWhiteSpace(x.Currency));
        RuleFor(x => x.ExchangeRate)
            .Must(rate => rate is null || (rate > 0 && rate <= 1_000_000m && decimal.Round(rate.Value, 6) == rate))
            .WithMessage("Kur sıfırdan büyük olmalı ve en fazla 6 ondalık basamak içermelidir.");

        RuleFor(x => x.Items).NotNull().WithMessage("Kalem listesi zorunludur.");
        When(x => x.Items is not null, () =>
        {
            RuleFor(x => x.Items)
                .Must(items => items.Count <= MaxItems).WithMessage($"Bir siparişte en fazla {MaxItems} kalem olabilir.")
                .Must(items => items.All(i => i.StockItemId > 0)).WithMessage("Her kalemde stok kartı seçilmelidir.")
                .Must(items => items.All(i =>
                    i.Quantity > 0 && i.Quantity <= StockRules.MaxQuantity && decimal.Round(i.Quantity, 3) == i.Quantity))
                .WithMessage("Kalem miktarı sıfırdan büyük olmalı ve en fazla 3 ondalık basamak içermelidir.")
                .Must(items => items.All(i =>
                    i.UnitPrice is null
                    || (i.UnitPrice >= 0 && i.UnitPrice <= MaxUnitPrice && decimal.Round(i.UnitPrice.Value, 4) == i.UnitPrice)))
                .WithMessage("Birim fiyat negatif olamaz ve en fazla 4 ondalık basamak içerebilir.")
                .Must(items => items.Select(i => i.StockItemId).Distinct().Count() == items.Count)
                .WithMessage("Aynı stok kartı birden fazla kalemde yer alamaz.");
        });
    }
}

public sealed class PurchaseOrderTransitionRequestValidator : AbstractValidator<PurchaseOrderTransitionRequest>
{
    public PurchaseOrderTransitionRequestValidator()
    {
        RuleFor(x => x.RowVersion).NotEmpty().WithMessage("Sipariş sürümü (rowVersion) zorunludur.");
    }
}

public sealed class GoodsReceiptRequestValidator : AbstractValidator<GoodsReceiptRequest>
{
    public const int MaxLotsPerLine = 200;

    public GoodsReceiptRequestValidator()
    {
        RuleFor(x => x.RequestId).NotEmpty().WithMessage("İstek kimliği (requestId) zorunludur.");
        RuleFor(x => x.WarehouseId).GreaterThan(0).WithMessage("Depo seçilmelidir.");
        RuleFor(x => x.DocumentNo).OptionalText(50);
        RuleFor(x => x.Note).OptionalText(300);

        RuleFor(x => x.Lines)
            .Must(lines => lines is { Count: > 0 }).WithMessage("En az bir kalem teslim alınmalıdır.");
        When(x => x.Lines is { Count: > 0 }, () =>
        {
            RuleFor(x => x.Lines)
                .Must(lines => lines.All(l => l.PurchaseOrderItemId > 0)).WithMessage("Her satırda sipariş kalemi belirtilmelidir.")
                .Must(lines => lines.All(l =>
                    l.Quantity > 0 && l.Quantity <= StockRules.MaxQuantity && decimal.Round(l.Quantity, 3) == l.Quantity))
                .WithMessage("Teslim alınan miktar sıfırdan büyük olmalı ve en fazla 3 ondalık basamak içermelidir.")
                .Must(lines => lines.Select(l => l.PurchaseOrderItemId).Distinct().Count() == lines.Count)
                .WithMessage("Aynı sipariş kalemi birden fazla satırda yer alamaz.")
                .Must(lines => lines.All(l => l.Lots is null || l.Lots.Count <= MaxLotsPerLine))
                .WithMessage($"Bir kalem en fazla {MaxLotsPerLine} topa bölünebilir.")
                .Must(lines => lines.All(l => l.Lots is not { Count: > 0 } || l.Lots.All(lot =>
                    lot.Quantity > 0 && decimal.Round(lot.Quantity, 3) == lot.Quantity)))
                .WithMessage("Top miktarı sıfırdan büyük olmalı ve en fazla 3 ondalık basamak içermelidir.")
                .Must(lines => lines.All(l => l.Lots is not { Count: > 0 } || l.Lots.Sum(lot => lot.Quantity) == l.Quantity))
                .WithMessage("Topların miktar toplamı kalemin teslim alınan miktarına eşit olmalıdır.");
            RuleForEach(x => x.Lines).ChildRules(line => line.RuleForEach(l => l.Lots).ChildRules(lot =>
                lot.ValidLot(l => l.BatchNo, l => l.Shade, l => l.Width, l => l.SupplierRollNo)));
        });
    }
}
