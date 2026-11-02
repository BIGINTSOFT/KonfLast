using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Core.Domain.Enums;
using ERP.Konfeksiyon.Application.Features.Sales;
using ERP.Konfeksiyon.Domain.Enums;
using FluentValidation;

namespace ERP.Konfeksiyon.Application.Features.Quotations;

public static class QuotationErrors
{
    public const string InvalidState = "QUOTATION_INVALID_STATE";
    public const string CustomerRequired = "QUOTATION_CUSTOMER_REQUIRED";
}

// ---------------------------------------------------------------- Ön maliyet parametreleri

public sealed record CostSettingDto(
    decimal LaborCostPerMinute,
    decimal EnergyCostPerMinute,
    string Currency,
    decimal OverheadPercent,
    decimal DefaultFirePercent,
    decimal DefaultMarginPercent,
    DateTime? UpdatedAt,
    string? UpdatedBy);

public sealed record CostSettingRequest(
    decimal LaborCostPerMinute,
    decimal EnergyCostPerMinute,
    string? Currency,
    decimal OverheadPercent,
    decimal DefaultFirePercent,
    decimal DefaultMarginPercent);

public sealed class CostSettingRequestValidator : AbstractValidator<CostSettingRequest>
{
    public CostSettingRequestValidator()
    {
        RuleFor(x => x.LaborCostPerMinute)
            .InclusiveBetween(0, 100_000).WithMessage("Dakika işçilik maliyeti 0 ile 100.000 arasında olmalıdır.")
            .Must(v => decimal.Round(v, 4) == v).WithMessage("En fazla 4 ondalık basamak girilebilir.");
        RuleFor(x => x.EnergyCostPerMinute)
            .InclusiveBetween(0, 100_000).WithMessage("Dakika enerji maliyeti 0 ile 100.000 arasında olmalıdır.")
            .Must(v => decimal.Round(v, 4) == v).WithMessage("En fazla 4 ondalık basamak girilebilir.");
        RuleFor(x => x.Currency)
            .Matches("^[A-Za-z]{3}$").WithMessage("Para birimi üç harfli kod olmalıdır (örn. TRY, USD, EUR).")
            .When(x => !string.IsNullOrWhiteSpace(x.Currency));
        RuleFor(x => x.OverheadPercent).InclusiveBetween(0, 1000).WithMessage("Genel gider payı 0 ile 1000 arasında olmalıdır.");
        RuleFor(x => x.DefaultFirePercent).InclusiveBetween(0, 100).WithMessage("Fire 0 ile 100 arasında olmalıdır.");
        RuleFor(x => x.DefaultMarginPercent).InclusiveBetween(0, 1000).WithMessage("Kâr marjı 0 ile 1000 arasında olmalıdır.");
    }
}

// ---------------------------------------------------------------- Teklif

public sealed class QuotationListQuery : PageRequest
{
    /// <summary>Teklif no, müşteri / aday adı ya da not içinde aranır.</summary>
    public string? Search { get; set; }

    public QuotationStatus? Status { get; set; }
    public int? CustomerId { get; set; }
}

/// <param name="CustomerName">Müşteri kartının adı; aday müşteride adayın adı.</param>
/// <param name="IsPastValidity">Gönderilmiş teklifin geçerlilik tarihi geçti (henüz "süresi doldu" işaretlenmedi).</param>
public sealed record QuotationListItemDto(
    int Id,
    string QuotationNo,
    int? CustomerId,
    string CustomerName,
    bool IsProspect,
    DateOnly QuotationDate,
    DateOnly ValidUntil,
    string Currency,
    QuotationStatus Status,
    int LineCount,
    int TotalQuantity,
    decimal TotalAmount,
    bool IsPastValidity,
    int? SalesOrderId,
    string? SalesOrderNo);

/// <param name="UnitCost">Birim maliyet (teklifin para biriminde).</param>
/// <param name="UnitCostOverridden">Birim maliyet elle girildi; boş bırakılırsa stok kartından okunur.</param>
public sealed record QuotationMaterialDto(
    int Id,
    int? StockItemId,
    string? ItemCode,
    string Description,
    StockUnit? Unit,
    decimal QuantityPerPiece,
    decimal UnitCost,
    bool UnitCostOverridden,
    decimal Cost);

/// <summary>Teklif satırı ve parça başı maliyet dökümü (teklifin para biriminde).</summary>
/// <param name="ManualMinutes">Elle girilen dakika; boşsa etkin rotanın fason olmayan adımlarının SAM toplamı.</param>
/// <param name="ManualSubcontractCost">Elle girilen fason maliyeti; boşsa rotadaki fason adımların fiyatları.</param>
/// <param name="LaborMinutes">Hesapta kullanılan dakika.</param>
/// <param name="Warnings">Hesaptaki eksikler (rota yok, kur yok, maliyeti olmayan malzeme, fason fiyatı yok ...).</param>
/// <param name="RouteMinutes">Etkin rotanın fason olmayan adımlarının SAM toplamı (rota yoksa boş); elle dakika silinirse bu kullanılır.</param>
/// <param name="RouteSubcontractCost">Rotadaki fason adımların fiyatlarından gelen fason maliyeti.</param>
public sealed record QuotationLineDto(
    int Id,
    int LineNo,
    int? ProductId,
    string? ProductCode,
    string? ProductName,
    string? ModelCode,
    string? Description,
    string? ModelReference,
    string? Colors,
    int Quantity,
    decimal? ManualMinutes,
    decimal? ManualSubcontractCost,
    bool PriceOverridden,
    decimal FirePercent,
    decimal OverheadPercent,
    decimal MarginPercent,
    decimal MaterialCost,
    decimal LaborMinutes,
    decimal LaborCost,
    decimal EnergyCost,
    decimal SubcontractCost,
    decimal FireCost,
    decimal OverheadCost,
    decimal UnitCost,
    decimal UnitPrice,
    decimal Amount,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<QuotationMaterialDto> Materials,
    decimal? RouteMinutes = null,
    decimal RouteSubcontractCost = 0);

/// <param name="LaborCostPerMinute">Teklif kaydedilirken ayardan okunan dakika işçilik maliyeti (teklifin para biriminde).</param>
/// <param name="IsPastValidity">Gönderilmiş teklifin geçerlilik tarihi geçti.</param>
public sealed record QuotationDto(
    int Id,
    string QuotationNo,
    int? CustomerId,
    string? CustomerCode,
    string? CustomerName,
    string? ProspectName,
    DateOnly QuotationDate,
    DateOnly ValidUntil,
    string Currency,
    DocumentLanguage DocumentLanguage,
    QuotationStatus Status,
    string? Notes,
    decimal LaborCostPerMinute,
    decimal EnergyCostPerMinute,
    bool IsEditable,
    IReadOnlyList<QuotationStatus> AllowedTransitions,
    bool IsPastValidity,
    int? SalesOrderId,
    string? SalesOrderNo,
    DateTime? SentAt,
    DateTime? DecidedAt,
    string? DecidedBy,
    int TotalQuantity,
    decimal TotalCost,
    decimal TotalAmount,
    string RowVersion,
    DateTime CreatedAt,
    string? CreatedBy,
    IReadOnlyList<QuotationLineDto> Lines);

/// <param name="UnitCost">Birim maliyet (teklifin para biriminde); boşsa stok kartının maliyeti çevrilerek kullanılır.</param>
public sealed record QuotationMaterialRequest(int? StockItemId, string? Description, decimal QuantityPerPiece, decimal? UnitCost);

/// <param name="ManualMinutes">Dakika; boşsa etkin rotadan.</param>
/// <param name="ManualSubcontractCost">Fason maliyeti (parça başı); boşsa rotadaki fason adımların fiyatlarından.</param>
/// <param name="FirePercent">Boşsa ayardaki varsayılan.</param>
/// <param name="OverheadPercent">Boşsa ayardaki genel gider payı.</param>
/// <param name="MarginPercent">Boşsa ayardaki varsayılan; birim fiyat verilirse yok sayılır (fiyattan geri hesaplanır).</param>
/// <param name="UnitPrice">Elle birim fiyat; boşsa maliyet × (1 + marj).</param>
/// <param name="Materials">Boşsa (null) ürünün reçetesinden yüklenir; boş liste malzemesiz demektir.</param>
public sealed record QuotationLineRequest(
    int? ProductId,
    string? Description,
    string? ModelReference,
    string? Colors,
    int Quantity,
    decimal? ManualMinutes = null,
    decimal? ManualSubcontractCost = null,
    decimal? FirePercent = null,
    decimal? OverheadPercent = null,
    decimal? MarginPercent = null,
    decimal? UnitPrice = null,
    IReadOnlyList<QuotationMaterialRequest>? Materials = null);

/// <param name="CustomerId">Müşteri kartı; yoksa aday adı (ProspectName) girilir.</param>
/// <param name="DocumentLanguage">Boşsa müşteri kartının belge dili (aday müşteride Türkçe).</param>
/// <param name="RowVersion">Güncellemede zorunlu.</param>
public sealed record QuotationRequest(
    int? CustomerId,
    string? ProspectName,
    DateOnly QuotationDate,
    DateOnly ValidUntil,
    string? Currency,
    DocumentLanguage? DocumentLanguage,
    string? Notes,
    IReadOnlyList<QuotationLineRequest> Lines,
    string? RowVersion = null);

/// <summary>Kaydetmeden bir satırın maliyetini hesaplar (yeni satır ve "ya olursa" denemesi için).</summary>
public sealed record QuotationLinePreviewRequest(string? Currency, DateOnly? Date, QuotationLineRequest Line);

public sealed record QuotationActionRequest(string RowVersion);

/// <summary>Kabulde müşteri siparişine yazılacak renk / beden adedi.</summary>
public sealed record QuotationAcceptLineRequest(int QuotationLineId, int ColorId, int SizeId, int Quantity);

/// <param name="CustomerId">Teklif aday müşteriye verildiyse siparişin müşterisi (zorunlu); müşteri kartı varsa yok sayılır.</param>
/// <param name="Lines">Ürünlü teklif satırlarının renk / beden kırılımı; fiyat teklif satırının birim fiyatıdır.</param>
public sealed record QuotationAcceptRequest(
    string RowVersion,
    int? CustomerId,
    DateOnly DueDate,
    string? CustomerOrderNo,
    IReadOnlyList<QuotationAcceptLineRequest> Lines);

/// <summary>Basılı teklif: teklif, satıcı (fabrika) ve müşteri / aday.</summary>
public sealed record QuotationDocumentDto(
    QuotationDto Quotation, ProformaPartyDto? Seller, ProformaPartyDto Customer, string? CustomerAddress, DateTime PrintedAt);

public sealed class QuotationRequestValidator : AbstractValidator<QuotationRequest>
{
    public const int MaxLines = 200;
    public const int MaxMaterials = 100;

    public QuotationRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => x.CustomerId is > 0 || !string.IsNullOrWhiteSpace(x.ProspectName))
            .OverridePropertyName("customerId")
            .WithMessage("Müşteri seçin ya da aday müşterinin adını yazın.");
        RuleFor(x => x.ProspectName).OptionalText(200);
        RuleFor(x => x.QuotationDate).NotEqual(default(DateOnly)).WithMessage("Teklif tarihi zorunludur.");
        RuleFor(x => x.ValidUntil)
            .NotEqual(default(DateOnly)).WithMessage("Geçerlilik tarihi zorunludur.")
            .GreaterThanOrEqualTo(x => x.QuotationDate).WithMessage("Geçerlilik tarihi teklif tarihinden önce olamaz.");
        RuleFor(x => x.Currency)
            .Matches("^[A-Za-z]{3}$").WithMessage("Para birimi üç harfli kod olmalıdır (örn. TRY, USD, EUR).")
            .When(x => !string.IsNullOrWhiteSpace(x.Currency));
        RuleFor(x => x.Notes).OptionalText(2000);
        RuleFor(x => x.Lines).NotNull().WithMessage("Satır listesi zorunludur.")
            .Must(lines => lines is null || lines.Count <= MaxLines).WithMessage($"Bir teklifte en fazla {MaxLines} satır olabilir.");
        RuleForEach(x => x.Lines).SetValidator(new QuotationLineRequestValidator());
    }
}

public sealed class QuotationLineRequestValidator : AbstractValidator<QuotationLineRequest>
{
    public QuotationLineRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => x.ProductId is > 0 || !string.IsNullOrWhiteSpace(x.Description))
            .OverridePropertyName("lines")
            .WithMessage("Her satırda ürün seçilmeli ya da açıklama yazılmalıdır.");
        RuleFor(x => x.Description).OptionalText(200);
        RuleFor(x => x.ModelReference).OptionalText(100);
        RuleFor(x => x.Colors).OptionalText(200);
        RuleFor(x => x.Quantity).InclusiveBetween(1, 10_000_000).WithMessage("Adet 1 ile 10.000.000 arasında olmalıdır.");
        RuleFor(x => x.ManualMinutes)
            .Must(v => v is null || (v >= 0 && v <= 9999 && decimal.Round(v.Value, 4) == v))
            .WithMessage("Dakika 0 ile 9999 arasında, en fazla 4 ondalık olmalıdır.");
        RuleFor(x => x.ManualSubcontractCost)
            .Must(v => v is null || (v >= 0 && v <= 1_000_000 && decimal.Round(v.Value, 4) == v))
            .WithMessage("Fason maliyeti negatif olamaz, en fazla 4 ondalık olmalıdır.");
        RuleFor(x => x.FirePercent).InclusiveBetween(0, 100).When(x => x.FirePercent is not null)
            .WithMessage("Fire 0 ile 100 arasında olmalıdır.");
        RuleFor(x => x.OverheadPercent).InclusiveBetween(0, 1000).When(x => x.OverheadPercent is not null)
            .WithMessage("Genel gider payı 0 ile 1000 arasında olmalıdır.");
        RuleFor(x => x.MarginPercent).InclusiveBetween(-100, 1000).When(x => x.MarginPercent is not null)
            .WithMessage("Kâr marjı -100 ile 1000 arasında olmalıdır.");
        RuleFor(x => x.UnitPrice)
            .Must(v => v is null || (v >= 0 && v <= 100_000_000 && decimal.Round(v.Value, 4) == v))
            .WithMessage("Birim fiyat negatif olamaz, en fazla 4 ondalık olmalıdır.");
        RuleFor(x => x.Materials)
            .Must(m => m is null || m.Count <= QuotationRequestValidator.MaxMaterials)
            .WithMessage($"Bir satırda en fazla {QuotationRequestValidator.MaxMaterials} malzeme olabilir.");
        RuleForEach(x => x.Materials).ChildRules(material =>
        {
            material.RuleFor(m => m)
                .Must(m => m.StockItemId is > 0 || !string.IsNullOrWhiteSpace(m.Description))
                .OverridePropertyName("materials")
                .WithMessage("Malzeme seçilmeli ya da açıklaması yazılmalıdır.");
            material.RuleFor(m => m.Description).OptionalText(200);
            material.RuleFor(m => m.QuantityPerPiece)
                .Must(v => v >= 0 && v <= 100_000 && decimal.Round(v, 4) == v)
                .WithMessage("Parça başı miktar negatif olamaz, en fazla 4 ondalık olmalıdır.");
            material.RuleFor(m => m.UnitCost)
                .Must(v => v is null || (v >= 0 && v <= 10_000_000 && decimal.Round(v.Value, 4) == v))
                .WithMessage("Birim maliyet negatif olamaz, en fazla 4 ondalık olmalıdır.");
        });
    }
}

public sealed class QuotationLinePreviewRequestValidator : AbstractValidator<QuotationLinePreviewRequest>
{
    public QuotationLinePreviewRequestValidator()
    {
        RuleFor(x => x.Line).NotNull().WithMessage("Satır gönderilmelidir.").SetValidator(new QuotationLineRequestValidator());
        RuleFor(x => x.Currency)
            .Matches("^[A-Za-z]{3}$").WithMessage("Para birimi üç harfli kod olmalıdır (örn. TRY, USD, EUR).")
            .When(x => !string.IsNullOrWhiteSpace(x.Currency));
    }
}

public sealed class QuotationActionRequestValidator : AbstractValidator<QuotationActionRequest>
{
    public QuotationActionRequestValidator()
    {
        RuleFor(x => x.RowVersion).NotEmpty().WithMessage("Kayıt sürümü (rowVersion) zorunludur.");
    }
}

public sealed class QuotationAcceptRequestValidator : AbstractValidator<QuotationAcceptRequest>
{
    public QuotationAcceptRequestValidator()
    {
        RuleFor(x => x.RowVersion).NotEmpty().WithMessage("Kayıt sürümü (rowVersion) zorunludur.");
        RuleFor(x => x.DueDate).NotEqual(default(DateOnly)).WithMessage("Teslim tarihi zorunludur.");
        RuleFor(x => x.CustomerOrderNo).OptionalText(50);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Siparişe aktarılacak en az bir renk / beden satırı girilmelidir.");
        RuleFor(x => x.Lines)
            .Must(lines => lines is null || lines.Select(l => (l.QuotationLineId, l.ColorId, l.SizeId)).Distinct().Count() == lines.Count)
            .WithMessage("Bir satır için aynı renk ve beden birden fazla kez girilemez.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ColorId).GreaterThan(0).WithMessage("Renk seçilmelidir.");
            line.RuleFor(l => l.SizeId).GreaterThan(0).WithMessage("Beden seçilmelidir.");
            line.RuleFor(l => l.Quantity).InclusiveBetween(1, 10_000_000).WithMessage("Adet 1 ile 10.000.000 arasında olmalıdır.");
        });
    }
}
