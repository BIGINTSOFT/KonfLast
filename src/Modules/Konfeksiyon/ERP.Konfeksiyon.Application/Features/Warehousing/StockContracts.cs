using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Core.Domain.Enums;
using ERP.Konfeksiyon.Domain.Enums;
using FluentValidation;

namespace ERP.Konfeksiyon.Application.Features.Warehousing;

public sealed record WarehouseDto(int Id, string Code, string Name, WarehouseType Type, bool IsActive);

public sealed record WarehouseRequest(string Code, string Name, WarehouseType Type = WarehouseType.General, bool IsActive = true);

/// <param name="Code">Bölge, raf ve gözden oluşan adres kodu (örn. A-01-03).</param>
/// <param name="IsDefault">Deponun genel adresi: adres belirtilmeyen hareketler buraya yazılır.</param>
/// <param name="StockItemCount">Adreste şu an stoğu bulunan kart sayısı.</param>
public sealed record WarehouseLocationDto(
    int Id, int WarehouseId, string WarehouseCode, string WarehouseName, string Code, string Zone, string? Rack,
    string? Bin, string? Description, bool IsDefault, int StockItemCount, bool IsActive);

/// <param name="Zone">Bölge (zorunlu).</param>
/// <param name="Rack">Raf; verilmezse adres bölgenin tamamıdır.</param>
/// <param name="Bin">Göz; yalnızca raf verilmişse girilebilir.</param>
public sealed record WarehouseLocationRequest(
    int WarehouseId, string Zone, string? Rack = null, string? Bin = null, string? Description = null, bool IsActive = true);

/// <summary>Hareket formlarındaki adres seçimi için hafif kayıt.</summary>
public sealed record WarehouseLocationOptionDto(int Id, string Code, string? Description, bool IsDefault, bool IsActive)
{
    public const int MaxOptions = 1000;
}

/// <summary>Bir kartın bir adresteki bakiyesi.</summary>
public sealed record StockLocationBalanceDto(
    int WarehouseId,
    string WarehouseCode,
    string WarehouseName,
    int LocationId,
    string LocationCode,
    bool LocationIsDefault,
    int StockItemId,
    string ItemCode,
    string ItemName,
    StockItemType ItemType,
    StockUnit Unit,
    decimal Quantity,
    bool IsLotTracked);

/// <summary>Girişte açılacak topun özellikleri; hepsi isteğe bağlıdır.</summary>
/// <param name="BatchNo">Tedarikçinin parti / lot numarası.</param>
/// <param name="Shade">Renk tonu.</param>
/// <param name="Width">Kumaş eni (cm).</param>
/// <param name="SupplierRollNo">Tedarikçinin top numarası.</param>
public sealed record StockLotRequest(
    string? BatchNo = null, string? Shade = null, decimal? Width = null, string? SupplierRollNo = null);

/// <summary>Bir topun bir adresteki bakiyesi, topun özellikleriyle.</summary>
/// <param name="LotNo">Sistem top numarası; etikete basılır.</param>
/// <param name="InitialQuantity">Topun giriş miktarı.</param>
/// <param name="Quantity">Topun bu adreste kalan miktarı.</param>
public sealed record StockLotBalanceDto(
    int LotId,
    string LotNo,
    string? BatchNo,
    string? Shade,
    decimal? Width,
    string? SupplierRollNo,
    decimal InitialQuantity,
    DateTime ReceivedAt,
    int? PurchaseOrderId,
    string? PurchaseOrderNo,
    int WarehouseId,
    string WarehouseCode,
    string WarehouseName,
    int LocationId,
    string LocationCode,
    int StockItemId,
    string ItemCode,
    string ItemName,
    StockItemType ItemType,
    StockUnit Unit,
    decimal Quantity);

public sealed class StockLotBalanceQuery : PageRequest
{
    public int? WarehouseId { get; set; }
    public int? LocationId { get; set; }
    public int? StockItemId { get; set; }
    public int? LotId { get; set; }

    /// <summary>Bu satın alma siparişinden gelen toplar.</summary>
    public int? PurchaseOrderId { get; set; }

    /// <summary>Top no, parti no, tedarikçi top no ya da kart kodu / adı içinde aranır.</summary>
    public string? Search { get; set; }

    /// <summary>true: tükenmiş toplar da listelenir.</summary>
    public bool IncludeZero { get; set; }
}

public sealed class StockLocationBalanceQuery : PageRequest
{
    public int? WarehouseId { get; set; }
    public int? LocationId { get; set; }
    public int? StockItemId { get; set; }
    public StockItemType? ItemType { get; set; }

    /// <summary>Kart kodu / adı ya da adres kodu içinde aranır.</summary>
    public string? Search { get; set; }

    /// <summary>true: bakiyesi sıfıra inmiş adresler de listelenir.</summary>
    public bool IncludeZero { get; set; }
}

/// <param name="IsSystemManaged">Mamul kartı: sistem açar, elle düzenlenemez.</param>
/// <param name="UnitCost">Ortalama birim maliyet; fiyatlı mal kabulde kendiliğinden güncellenir.</param>
/// <param name="IsLotTracked">Top / parti takipli: her giriş bir top açar, çıkışlar top seçilerek yapılır.</param>
public sealed record StockItemDto(
    int Id, string Code, string Name, StockItemType ItemType, StockUnit Unit, bool IsSystemManaged, bool IsActive,
    decimal? UnitCost = null, bool IsLotTracked = false, decimal? MinimumStock = null, decimal? ReorderQuantity = null,
    int? SupplierId = null, string? SupplierName = null);

/// <param name="ItemType">FABRIC, ACCESSORY ya da OTHER (mamul kartları sistem tarafından açılır).</param>
/// <param name="UnitCost">Birim maliyet (isteğe bağlı); fiyatlı mal kabul yapıldıkça ortalaması alınır.</param>
/// <param name="IsLotTracked">Top / parti takibi. Kumaş kartı her zaman takiplidir; hareket görmüş kartta değiştirilemez.</param>
/// <param name="MinimumStock">Asgari stok (tüm depoların toplamı); boşsa izlenmez.</param>
/// <param name="ReorderQuantity">Sipariş katı: tedarik önerisinde bundan azı önerilmez.</param>
/// <param name="SupplierId">Varsayılan tedarikçi.</param>
public sealed record StockItemRequest(
    string Code, string Name, StockItemType ItemType, StockUnit Unit, bool IsActive = true, decimal? UnitCost = null,
    bool IsLotTracked = false, decimal? MinimumStock = null, decimal? ReorderQuantity = null, int? SupplierId = null);

/// <param name="Reserved">Depodaki stoğun üretim siparişlerine ayrılmış kısmı; kullanılabilir stok = miktar − ayrılan.</param>
public sealed record StockBalanceDto(
    int WarehouseId,
    string WarehouseCode,
    string WarehouseName,
    int StockItemId,
    string ItemCode,
    string ItemName,
    StockItemType ItemType,
    StockUnit Unit,
    decimal Quantity,
    bool IsLotTracked,
    decimal Reserved = 0);

public sealed class StockBalanceQuery : PageRequest
{
    public int? WarehouseId { get; set; }
    public int? StockItemId { get; set; }
    public StockItemType? ItemType { get; set; }

    /// <summary>Kart kodu veya adı içinde aranır.</summary>
    public string? Search { get; set; }

    /// <summary>true: bakiyesi sıfıra inmiş kartlar da listelenir.</summary>
    public bool IncludeZero { get; set; }
}

/// <param name="Quantity">Bakiyedeki değişim: girişte artı, çıkışta eksi.</param>
/// <param name="BalanceAfter">Hareketten sonra kartın bu depodaki bakiyesi.</param>
/// <param name="LocationBalanceAfter">Hareketten sonra kartın bu adresteki bakiyesi.</param>
/// <param name="CounterLocationCode">Transferde ve adres değişiminde karşı adres.</param>
/// <param name="LotBalanceAfter">Hareketten sonra topun bu adresteki bakiyesi; takipsiz kartta boş.</param>
public sealed record StockTransactionDto(
    long Id,
    StockTransactionType Type,
    int WarehouseId,
    string WarehouseCode,
    string WarehouseName,
    int StockItemId,
    string ItemCode,
    string ItemName,
    StockUnit Unit,
    decimal Quantity,
    decimal BalanceAfter,
    string? CounterWarehouseName,
    int? BundleId,
    string? BundleNo,
    int? ProductionOrderId,
    string? OrderNo,
    string? DocumentNo,
    string? Note,
    DateTime OccurredAt,
    string CreatedBy,
    int? PurchaseOrderId = null,
    string? PurchaseOrderNo = null,
    int? ShipmentId = null,
    string? ShipmentNo = null,
    decimal? UnitCost = null,
    int LocationId = 0,
    string LocationCode = "",
    decimal LocationBalanceAfter = 0,
    string? CounterLocationCode = null,
    int? LotId = null,
    string? LotNo = null,
    decimal? LotBalanceAfter = null);

public sealed class StockTransactionQuery : PageRequest
{
    public int? WarehouseId { get; set; }
    public int? LocationId { get; set; }
    public int? StockItemId { get; set; }
    public int? LotId { get; set; }
    public StockTransactionType? Type { get; set; }
    public int? ProductionOrderId { get; set; }

    /// <summary>Kart kodu / adı, belge no, demet no ya da top no içinde aranır.</summary>
    public string? Search { get; set; }

    /// <summary>Hareket zamanı bu andan sonra (UTC).</summary>
    public DateTime? From { get; set; }

    /// <summary>Hareket zamanı bu andan önce (UTC).</summary>
    public DateTime? To { get; set; }
}

/// <param name="RequestId">İstemcinin ürettiği kimlik; aynı kimlikle yinelenen istek hareketi ikinci kez yazmaz.</param>
/// <param name="LocationId">Girişin yapılacağı adres; verilmezse deponun genel adresi.</param>
/// <param name="Lot">Takipli kartta açılacak topun özellikleri (verilmezse top özelliksiz açılır); takipsiz kartta gönderilmez.</param>
public sealed record StockReceiptRequest(
    Guid RequestId, int WarehouseId, int StockItemId, decimal Quantity, string? DocumentNo = null, string? Note = null,
    int? LocationId = null, StockLotRequest? Lot = null);

/// <param name="ProductionOrderId">Malzeme bir sipariş için çıkılıyorsa o sipariş.</param>
/// <param name="LocationId">Çıkışın yapılacağı adres; verilmezse topun bulunduğu adres, o da yoksa deponun genel adresi.</param>
/// <param name="LotId">Takipli kartta çıkılacak top (zorunlu); takipsiz kartta gönderilmez.</param>
public sealed record StockIssueRequest(
    Guid RequestId, int WarehouseId, int StockItemId, decimal Quantity, int? ProductionOrderId = null,
    string? DocumentNo = null, string? Note = null, int? LocationId = null, int? LotId = null);

/// <summary>
/// Depolar arası transfer ya da (iki depo aynıysa) depo içinde adres değişimi. Adres verilmeyen uçta deponun
/// genel adresi kullanılır; çıkış ve giriş adresi aynı olamaz. Takipli kartta taşınan top (LotId) zorunludur;
/// miktar topun tamamından azsa top iki adrese bölünür.
/// </summary>
public sealed record StockTransferRequest(
    Guid RequestId, int FromWarehouseId, int ToWarehouseId, int StockItemId, decimal Quantity, string? Note = null,
    int? FromLocationId = null, int? ToLocationId = null, int? LotId = null);

/// <param name="CountedQuantity">Adreste sayılan miktar; adresin bakiyesi bu değere getirilir.</param>
/// <param name="Note">Düzeltmenin nedeni (zorunlu).</param>
/// <param name="LocationId">Sayılan adres; verilmezse topun bulunduğu adres, o da yoksa deponun genel adresi.</param>
/// <param name="LotId">Takipli kartta sayılan top (zorunlu); sayılan miktar topun o adresteki miktarıdır.</param>
public sealed record StockAdjustmentRequest(
    Guid RequestId, int WarehouseId, int StockItemId, decimal CountedQuantity, string Note, int? LocationId = null,
    int? LotId = null);

/// <summary>Topun özelliklerini düzeltir (ton, en vb.); miktar ve yer yalnızca stok hareketiyle değişir.</summary>
public sealed record StockLotUpdateRequest(
    string? BatchNo = null, string? Shade = null, decimal? Width = null, string? SupplierRollNo = null);

/// <summary>Tamamlanmış demetin sağlam adedini depoya mamul olarak alır; adedi sunucu belirler.</summary>
public sealed record ProductionReceiptRequest(Guid RequestId, int BundleId, int WarehouseId);

/// <param name="Duplicate">İstek daha önce işlenmişti; hareketler ilk işlemin kayıtlarıdır.</param>
public sealed record StockMovementDto(bool Duplicate, IReadOnlyList<StockTransactionDto> Transactions);

/// <summary>Depoya teslim alınmayı bekleyen (tamamlanmış) demet.</summary>
/// <param name="FinishedQuantity">Demetten çıkan sağlam adet; depoya bu kadar girer.</param>
public sealed record ReceivableBundleDto(
    int BundleId,
    string BundleNo,
    int ProductionOrderId,
    string OrderNo,
    string ProductCode,
    string ProductName,
    string ColorName,
    string SizeName,
    int Quantity,
    int FinishedQuantity,
    DateTime? CompletedAt);

public sealed class ReceivableBundleQuery : PageRequest
{
    /// <summary>Demet no ya da sipariş no içinde aranır.</summary>
    public string? Search { get; set; }
    public int? ProductionOrderId { get; set; }
}

public static class StockRules
{
    public const decimal MaxQuantity = 100_000_000m;

    public static IRuleBuilderOptions<T, decimal> ValidStockQuantity<T>(this IRuleBuilder<T, decimal> rule) => rule
        .Must(quantity => quantity <= MaxQuantity).WithMessage($"Miktar en fazla {MaxQuantity:N0} olabilir.")
        .Must(quantity => decimal.Round(quantity, 3) == quantity).WithMessage("Miktar en fazla 3 ondalık basamak içerebilir.");
}

public sealed class WarehouseRequestValidator : AbstractValidator<WarehouseRequest>
{
    public WarehouseRequestValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName(100);
        RuleFor(x => x.Type).IsInEnum().WithMessage("Geçersiz depo türü.");
    }
}

public sealed class WarehouseLocationRequestValidator : AbstractValidator<WarehouseLocationRequest>
{
    public const int SegmentMaxLength = 10;

    public WarehouseLocationRequestValidator()
    {
        RuleFor(x => x.WarehouseId).GreaterThan(0).WithMessage("Depo seçilmelidir.");
        RuleFor(x => x.Zone).NotEmpty().WithMessage("Bölge zorunludur.");
        RuleFor(x => x.Zone).Must(BeSegment).WithMessage(SegmentMessage("Bölge")).When(x => !string.IsNullOrWhiteSpace(x.Zone));
        RuleFor(x => x.Rack).Must(BeSegment).WithMessage(SegmentMessage("Raf")).When(x => !string.IsNullOrWhiteSpace(x.Rack));
        RuleFor(x => x.Bin).Must(BeSegment).WithMessage(SegmentMessage("Göz")).When(x => !string.IsNullOrWhiteSpace(x.Bin));
        RuleFor(x => x.Bin)
            .Must(_ => false).WithMessage("Göz girmek için önce raf belirtilmelidir.")
            .When(x => !string.IsNullOrWhiteSpace(x.Bin) && string.IsNullOrWhiteSpace(x.Rack));
        RuleFor(x => x.Description).OptionalText(100);
    }

    // Tire, seviyeleri ayırdığı için seviyenin içinde kullanılamaz.
    private static bool BeSegment(string? value) =>
        value!.Trim() is { Length: <= SegmentMaxLength } segment && segment.All(char.IsAsciiLetterOrDigit);

    private static string SegmentMessage(string label) =>
        $"{label} yalnızca harf (A-Z) ve rakam içerebilir, en fazla {SegmentMaxLength} karakter olabilir.";
}

public sealed class StockItemRequestValidator : AbstractValidator<StockItemRequest>
{
    public StockItemRequestValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName(200);
        RuleFor(x => x.ItemType)
            .IsInEnum().WithMessage("Geçersiz kart türü.")
            .NotEqual(StockItemType.FinishedGood)
            .WithMessage("Mamul kartları üretimden girişte sistem tarafından açılır; elle tanımlanamaz.");
        RuleFor(x => x.Unit).IsInEnum().WithMessage("Geçersiz birim.");
        RuleFor(x => x.UnitCost)
            .InclusiveBetween(0, 100_000_000).WithMessage("Birim maliyet negatif olamaz.")
            .PrecisionScale(18, 4, ignoreTrailingZeros: true).WithMessage("Birim maliyet en fazla 4 ondalık basamak içerebilir.");
        RuleFor(x => x.MinimumStock)
            .InclusiveBetween(0, StockRules.MaxQuantity).WithMessage("Asgari stok negatif olamaz.")
            .PrecisionScale(18, 3, ignoreTrailingZeros: true).WithMessage("Asgari stok en fazla 3 ondalık basamak içerebilir.");
        RuleFor(x => x.ReorderQuantity)
            .GreaterThan(0).WithMessage("Sipariş katı sıfırdan büyük olmalıdır.")
            .LessThanOrEqualTo(StockRules.MaxQuantity).WithMessage($"Sipariş katı en fazla {StockRules.MaxQuantity:N0} olabilir.")
            .PrecisionScale(18, 3, ignoreTrailingZeros: true).WithMessage("Sipariş katı en fazla 3 ondalık basamak içerebilir.");
    }
}

public static class StockLotRules
{
    public const decimal MaxWidth = 1000m;

    public static void ValidLot<T>(
        this AbstractValidator<T> validator, Func<T, string?> batchNo, Func<T, string?> shade, Func<T, decimal?> width,
        Func<T, string?> supplierRollNo)
    {
        validator.RuleFor(x => batchNo(x)).OptionalText(50).OverridePropertyName("batchNo");
        validator.RuleFor(x => shade(x)).OptionalText(30).OverridePropertyName("shade");
        validator.RuleFor(x => supplierRollNo(x)).OptionalText(50).OverridePropertyName("supplierRollNo");
        validator.RuleFor(x => width(x))
            .Must(value => value is null || (value > 0 && value <= MaxWidth && decimal.Round(value.Value, 2) == value))
            .WithMessage($"En sıfırdan büyük, en fazla {MaxWidth:0} cm olmalı ve en fazla 2 ondalık basamak içermelidir.")
            .OverridePropertyName("width");
    }
}

public sealed class StockLotRequestValidator : AbstractValidator<StockLotRequest>
{
    public StockLotRequestValidator() => this.ValidLot(x => x.BatchNo, x => x.Shade, x => x.Width, x => x.SupplierRollNo);
}

public sealed class StockLotUpdateRequestValidator : AbstractValidator<StockLotUpdateRequest>
{
    public StockLotUpdateRequestValidator() =>
        this.ValidLot(x => x.BatchNo, x => x.Shade, x => x.Width, x => x.SupplierRollNo);
}

public sealed class StockReceiptRequestValidator : AbstractValidator<StockReceiptRequest>
{
    public StockReceiptRequestValidator()
    {
        RuleFor(x => x.Lot!).SetValidator(new StockLotRequestValidator()).When(x => x.Lot is not null);
        RuleFor(x => x.RequestId).NotEmpty().WithMessage("İstek kimliği (requestId) zorunludur.");
        RuleFor(x => x.WarehouseId).GreaterThan(0).WithMessage("Depo seçilmelidir.");
        RuleFor(x => x.StockItemId).GreaterThan(0).WithMessage("Stok kartı seçilmelidir.");
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Miktar sıfırdan büyük olmalıdır.").ValidStockQuantity();
        RuleFor(x => x.DocumentNo).OptionalText(50);
        RuleFor(x => x.Note).OptionalText(300);
    }
}

public sealed class StockIssueRequestValidator : AbstractValidator<StockIssueRequest>
{
    public StockIssueRequestValidator()
    {
        RuleFor(x => x.RequestId).NotEmpty().WithMessage("İstek kimliği (requestId) zorunludur.");
        RuleFor(x => x.WarehouseId).GreaterThan(0).WithMessage("Depo seçilmelidir.");
        RuleFor(x => x.StockItemId).GreaterThan(0).WithMessage("Stok kartı seçilmelidir.");
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Miktar sıfırdan büyük olmalıdır.").ValidStockQuantity();
        RuleFor(x => x.DocumentNo).OptionalText(50);
        RuleFor(x => x.Note).OptionalText(300);
    }
}

public sealed class StockTransferRequestValidator : AbstractValidator<StockTransferRequest>
{
    public StockTransferRequestValidator()
    {
        RuleFor(x => x.RequestId).NotEmpty().WithMessage("İstek kimliği (requestId) zorunludur.");
        RuleFor(x => x.FromWarehouseId).GreaterThan(0).WithMessage("Çıkış deposu seçilmelidir.");
        RuleFor(x => x.ToWarehouseId).GreaterThan(0).WithMessage("Giriş deposu seçilmelidir.");
        RuleFor(x => x.StockItemId).GreaterThan(0).WithMessage("Stok kartı seçilmelidir.");
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Miktar sıfırdan büyük olmalıdır.").ValidStockQuantity();
        RuleFor(x => x.Note).OptionalText(300);
    }
}

public sealed class StockAdjustmentRequestValidator : AbstractValidator<StockAdjustmentRequest>
{
    public StockAdjustmentRequestValidator()
    {
        RuleFor(x => x.RequestId).NotEmpty().WithMessage("İstek kimliği (requestId) zorunludur.");
        RuleFor(x => x.WarehouseId).GreaterThan(0).WithMessage("Depo seçilmelidir.");
        RuleFor(x => x.StockItemId).GreaterThan(0).WithMessage("Stok kartı seçilmelidir.");
        RuleFor(x => x.CountedQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Sayılan miktar negatif olamaz.").ValidStockQuantity();
        RuleFor(x => x.Note)
            .NotEmpty().WithMessage("Sayım düzeltmesinin nedeni yazılmalıdır.")
            .MaximumLength(300).WithMessage("Not en fazla 300 karakter olabilir.");
    }
}

public sealed class ProductionReceiptRequestValidator : AbstractValidator<ProductionReceiptRequest>
{
    public ProductionReceiptRequestValidator()
    {
        RuleFor(x => x.RequestId).NotEmpty().WithMessage("İstek kimliği (requestId) zorunludur.");
        RuleFor(x => x.BundleId).GreaterThan(0).WithMessage("Demet seçilmelidir.");
        RuleFor(x => x.WarehouseId).GreaterThan(0).WithMessage("Depo seçilmelidir.");
    }
}

/// <summary>
/// Üretimden gelmemiş mamul (açılış stoğu, iade, dışarıdan alınan mamul) için mamul kartı açma isteği. Kart
/// açıldıktan sonra miktar olağan stok girişiyle girilir.
/// </summary>
public sealed record FinishedGoodCardRequest(int ProductId, int ColorId, int SizeId);

/// <param name="IsNew">Kart bu istekle açıldıysa true; zaten varsa false.</param>
public sealed record FinishedGoodCardDto(int Id, string Code, string Name, bool IsNew);

public sealed class FinishedGoodCardRequestValidator : AbstractValidator<FinishedGoodCardRequest>
{
    public FinishedGoodCardRequestValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0).WithMessage("Ürün seçilmelidir.");
        RuleFor(x => x.ColorId).GreaterThan(0).WithMessage("Renk seçilmelidir.");
        RuleFor(x => x.SizeId).GreaterThan(0).WithMessage("Beden seçilmelidir.");
    }
}
