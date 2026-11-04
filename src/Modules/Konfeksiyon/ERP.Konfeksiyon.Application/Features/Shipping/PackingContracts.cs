using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Core.Domain.Enums;
using ERP.Konfeksiyon.Application.Features.Sales;
using ERP.Konfeksiyon.Application.Features.Warehousing;
using ERP.Konfeksiyon.Domain.Enums;
using FluentValidation;

namespace ERP.Konfeksiyon.Application.Features.Shipping;

// ───────── Asorti şablonları ─────────

/// <param name="CustomerName">Boşsa genel şablon.</param>
/// <param name="PiecesPerCarton">Bir koliye giren toplam adet (oranların toplamı).</param>
public sealed record AssortmentTemplateListItemDto(
    int Id,
    string Code,
    string Name,
    int? CustomerId,
    string? CustomerName,
    int SizeCount,
    int PiecesPerCarton,
    bool IsActive);

public sealed record AssortmentTemplateLineDto(int Id, int SizeId, string SizeCode, string SizeName, int SortOrder, int Ratio);

public sealed record AssortmentTemplateDto(
    int Id,
    string Code,
    string Name,
    int? CustomerId,
    string? CustomerCode,
    string? CustomerName,
    string? Notes,
    bool IsActive,
    int PiecesPerCarton,
    DateTime CreatedAt,
    string? CreatedBy,
    DateTime? UpdatedAt,
    string? UpdatedBy,
    IReadOnlyList<AssortmentTemplateLineDto> Lines);

/// <summary>Koli dağıtımında seçilecek şablon; dağılım "S:1 M:2 L:2" biçiminde gösterilir.</summary>
public sealed record AssortmentTemplateOptionDto(
    int Id, string Code, string Name, string? CustomerName, int PiecesPerCarton, string Distribution);

public sealed class AssortmentTemplateQuery : PageRequest
{
    /// <summary>Kod, ad ya da müşteri adı içinde aranır.</summary>
    public string? Search { get; set; }
    public int? CustomerId { get; set; }
}

public sealed record AssortmentTemplateLineRequest(int SizeId, int Ratio);

/// <param name="CustomerId">Boşsa genel şablon.</param>
public sealed record AssortmentTemplateRequest(
    string Code,
    string Name,
    int? CustomerId,
    string? Notes,
    IReadOnlyList<AssortmentTemplateLineRequest> Lines,
    bool IsActive = true);

public sealed class AssortmentTemplateRequestValidator : AbstractValidator<AssortmentTemplateRequest>
{
    public const int MaxLines = 50;
    public const int MaxRatio = 999;

    public AssortmentTemplateRequestValidator()
    {
        RuleFor(x => x.Code).ValidCode();
        RuleFor(x => x.Name).ValidName(200);
        RuleFor(x => x.Notes).OptionalText(500);

        RuleFor(x => x.Lines).NotNull().WithMessage("Beden listesi zorunludur.");
        When(x => x.Lines is not null, () =>
        {
            RuleFor(x => x.Lines)
                .Must(lines => lines.Count > 0).WithMessage("Şablonda en az bir beden olmalıdır.")
                .Must(lines => lines.Count <= MaxLines).WithMessage($"Bir şablonda en fazla {MaxLines} beden olabilir.")
                .Must(lines => lines.All(l => l.SizeId > 0)).WithMessage("Her satırda beden seçilmelidir.")
                .Must(lines => lines.All(l => l.Ratio is > 0 and <= MaxRatio))
                .WithMessage($"Koli içi adet 1 ile {MaxRatio} arasında bir tam sayı olmalıdır.")
                .Must(lines => lines.Select(l => l.SizeId).Distinct().Count() == lines.Count)
                .WithMessage("Aynı beden şablonda birden fazla kez yer alamaz.");
        });
    }
}

// ───────── Koliler (çeki listesi) ─────────

/// <param name="Quantity">Sevkiyattaki adet.</param>
/// <param name="PackedQuantity">Kolilere girmiş adet; sevk için <paramref name="Quantity"/> ile eşit olmalıdır.</param>
public sealed record ShipmentPackingItemDto(
    int StockItemId,
    string ItemCode,
    string? ProductCode,
    string? ProductName,
    string? ColorCode,
    string? ColorName,
    string? SizeCode,
    string? SizeName,
    int SizeOrder,
    StockUnit Unit,
    decimal Quantity,
    decimal PackedQuantity);

public sealed record ShipmentCartonLineDto(
    int Id, int StockItemId, string ItemCode, string? ColorName, string? SizeName, int SizeOrder, decimal Quantity);

/// <param name="Code">Etikette barkod olarak basılan koli kodu.</param>
/// <param name="AssortmentTemplateCode">Şablonla doldurulmuş kolide şablon; boşsa karışık koli.</param>
public sealed record ShipmentCartonDto(
    int Id,
    int CartonNo,
    string Code,
    int? AssortmentTemplateId,
    string? AssortmentTemplateCode,
    decimal? NetKg,
    decimal? GrossKg,
    DateTime? LoadedAt,
    string? LoadedBy,
    decimal Quantity,
    IReadOnlyList<ShipmentCartonLineDto> Lines);

/// <param name="RowVersion">Sevkiyatın sürümü; koli işlemlerinde geri gönderilir.</param>
/// <param name="MatchesItems">Koli yoksa ya da kolilerdeki adetler kalemlerle birebir tutuyorsa true.</param>
public sealed record ShipmentPackingDto(
    int ShipmentId,
    string ShipmentNo,
    int CustomerId,
    ShipmentStatus Status,
    bool IsEditable,
    string RowVersion,
    int CartonCount,
    int LoadedCount,
    bool MatchesItems,
    IReadOnlyList<ShipmentPackingItemDto> Items,
    IReadOnlyList<ShipmentCartonDto> Cartons);

/// <summary>Koli dağıtımı: mevcut koliler silinir, kalemler şablona göre yeniden kolilenir.</summary>
public sealed record CartonDistributionRequest(int AssortmentTemplateId, string RowVersion);

public sealed record ShipmentCartonLineRequest(int StockItemId, decimal Quantity);

public sealed record ShipmentCartonRequest(
    string RowVersion, IReadOnlyList<ShipmentCartonLineRequest> Lines, decimal? NetKg = null, decimal? GrossKg = null);

/// <summary>Seçilen kolilere aynı net / brüt ağırlığı yazar (boş değer ağırlığı siler).</summary>
public sealed record ShipmentCartonWeightsRequest(string RowVersion, IReadOnlyList<int> CartonIds, decimal? NetKg, decimal? GrossKg);

public sealed record ShipmentPackingVersionRequest(string RowVersion);

public sealed class CartonDistributionRequestValidator : AbstractValidator<CartonDistributionRequest>
{
    public CartonDistributionRequestValidator()
    {
        RuleFor(x => x.AssortmentTemplateId).GreaterThan(0).WithMessage("Asorti şablonu seçilmelidir.");
        RuleFor(x => x.RowVersion).NotEmpty().WithMessage("Sevkiyat sürümü (rowVersion) zorunludur.");
    }
}

public sealed class ShipmentCartonRequestValidator : AbstractValidator<ShipmentCartonRequest>
{
    public const int MaxLines = 200;

    public ShipmentCartonRequestValidator()
    {
        RuleFor(x => x.RowVersion).NotEmpty().WithMessage("Sevkiyat sürümü (rowVersion) zorunludur.");
        RuleFor(x => x).Custom((request, context) => CartonWeights.Validate(request.NetKg, request.GrossKg, context));

        RuleFor(x => x.Lines).NotNull().WithMessage("Koli içeriği zorunludur.");
        When(x => x.Lines is not null, () =>
        {
            RuleFor(x => x.Lines)
                .Must(lines => lines.Count > 0).WithMessage("Kolide en az bir kalem olmalıdır.")
                .Must(lines => lines.Count <= MaxLines).WithMessage($"Bir kolide en fazla {MaxLines} kalem olabilir.")
                .Must(lines => lines.All(l => l.StockItemId > 0)).WithMessage("Her satırda mamul seçilmelidir.")
                .Must(lines => lines.All(l =>
                    l.Quantity > 0 && l.Quantity <= StockRules.MaxQuantity && decimal.Round(l.Quantity, 3) == l.Quantity))
                .WithMessage("Adet sıfırdan büyük olmalı ve en fazla 3 ondalık basamak içermelidir.")
                .Must(lines => lines.Select(l => l.StockItemId).Distinct().Count() == lines.Count)
                .WithMessage("Aynı mamul kolide birden fazla satırda yer alamaz.");
        });
    }
}

public sealed class ShipmentCartonWeightsRequestValidator : AbstractValidator<ShipmentCartonWeightsRequest>
{
    public ShipmentCartonWeightsRequestValidator()
    {
        RuleFor(x => x.RowVersion).NotEmpty().WithMessage("Sevkiyat sürümü (rowVersion) zorunludur.");
        RuleFor(x => x.CartonIds).NotEmpty().WithMessage("En az bir koli seçilmelidir.");
        RuleFor(x => x).Custom((request, context) => CartonWeights.Validate(request.NetKg, request.GrossKg, context));
    }
}

public sealed class ShipmentPackingVersionRequestValidator : AbstractValidator<ShipmentPackingVersionRequest>
{
    public ShipmentPackingVersionRequestValidator()
    {
        RuleFor(x => x.RowVersion).NotEmpty().WithMessage("Sevkiyat sürümü (rowVersion) zorunludur.");
    }
}

/// <summary>Koli ağırlığı kuralları: negatif olamaz, brüt netten az olamaz.</summary>
public static class CartonWeights
{
    public const decimal MaxKg = 9_999m;

    public static void Validate<T>(decimal? net, decimal? gross, ValidationContext<T> context)
    {
        if (net is < 0m or > MaxKg || (net is { } n && decimal.Round(n, 3) != n))
        {
            context.AddFailure("netKg", $"Net ağırlık 0 ile {MaxKg:0} kg arasında, en fazla 3 ondalıklı olmalıdır.");
        }

        if (gross is < 0m or > MaxKg || (gross is { } g && decimal.Round(g, 3) != g))
        {
            context.AddFailure("grossKg", $"Brüt ağırlık 0 ile {MaxKg:0} kg arasında, en fazla 3 ondalıklı olmalıdır.");
        }

        if (net is { } netKg && gross is { } grossKg && grossKg < netKg)
        {
            context.AddFailure("grossKg", "Brüt ağırlık net ağırlıktan az olamaz.");
        }
    }
}

// ───────── Yükleme (el terminali) ─────────

/// <param name="Summary">Kolinin içeriği kısaca (renk ve beden dağılımı).</param>
public sealed record ShipmentLoadingCartonDto(
    int Id, int CartonNo, string Code, decimal Quantity, string Summary, DateTime? LoadedAt, string? LoadedBy);

public sealed record ShipmentLoadingDto(
    int ShipmentId,
    string ShipmentNo,
    string CustomerName,
    ShipmentStatus Status,
    DateOnly ShipDate,
    int CartonCount,
    int LoadedCount,
    IReadOnlyList<ShipmentLoadingCartonDto> Cartons);

/// <summary>Terminalde seçilebilecek sevkiyat: kolisi olan taslak ya da sevk edilmiş sevkiyatlar.</summary>
public sealed record LoadableShipmentDto(
    int Id, string ShipmentNo, string CustomerName, DateOnly ShipDate, ShipmentStatus Status, int CartonCount, int LoadedCount);

public sealed record CartonLoadRequest(string Code);

/// <param name="AlreadyLoaded">Koli daha önce okutulmuştu; yükleme bilgisi değişmedi.</param>
public sealed record CartonLoadResultDto(int CartonNo, string Code, bool AlreadyLoaded, ShipmentLoadingDto Shipment);

public sealed class CartonLoadRequestValidator : AbstractValidator<CartonLoadRequest>
{
    public CartonLoadRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage("Koli barkodu okutulmalıdır.").MaximumLength(60);
    }
}

// ───────── Basılı belgeler (çeki listesi, koli etiketi, irsaliye) ─────────

public sealed record ShipmentDocumentItemDto(
    int StockItemId,
    string ItemCode,
    string? ProductCode,
    string? ProductName,
    string? CustomerArticleCode,
    string? ColorCode,
    string? ColorName,
    string? SizeCode,
    string? SizeName,
    int SizeOrder,
    StockUnit Unit,
    decimal Quantity);

public sealed record ShipmentDocumentCartonLineDto(int StockItemId, decimal Quantity);

public sealed record ShipmentDocumentCartonDto(
    int CartonNo, string Code, decimal? NetKg, decimal? GrossKg, IReadOnlyList<ShipmentDocumentCartonLineDto> Lines);

/// <summary>
/// Sevkiyat belgelerinin ortak verisi: başlık, taraflar, kalemler (ürün / renk / beden) ve koliler. Belgeler müşterinin
/// belge dilinde basılır.
/// </summary>
/// <param name="DocumentNo">İrsaliye numarası; boşsa belgede sevkiyat numarası kullanılır.</param>
/// <param name="DeliveryAddress">Müşteri siparişinin teslim adresi, yoksa müşterinin varsayılan teslimat adresi.</param>
public sealed record ShipmentDocumentDto(
    int ShipmentId,
    string ShipmentNo,
    string? DocumentNo,
    DateOnly ShipDate,
    ShipmentStatus Status,
    DateTime? ShippedAt,
    string? Carrier,
    string? Notes,
    string CustomerCode,
    ProformaPartyDto Customer,
    string? InvoiceAddress,
    string? DeliveryAddress,
    string? SalesOrderNo,
    string? CustomerOrderNo,
    string? ProductionOrderNo,
    ProformaPartyDto? Seller,
    DocumentLanguage DocumentLanguage,
    IReadOnlyList<ShipmentDocumentItemDto> Items,
    IReadOnlyList<ShipmentDocumentCartonDto> Cartons,
    DateTime PrintedAt,
    ShipmentDespatchInfoDto? Despatch = null);

/// <summary>e-İrsaliye görünümü için ek bilgiler (taslak çıktı; GİB'e gönderim entegratör üzerinden yapılır).</summary>
/// <param name="DespatchUuid">ETTN; irsaliye kesilince verilir.</param>
/// <param name="ReceiverIsEDespatchUser">Alıcı e-İrsaliye mükellefi (posta kutusu etiketiyle).</param>
public sealed record ShipmentDespatchInfoDto(
    Guid? DespatchUuid,
    string? VehiclePlate,
    string? DriverName,
    string? DriverIdentityNo,
    bool ReceiverIsEDespatchUser,
    string? ReceiverAlias,
    string? CheckedBy);
