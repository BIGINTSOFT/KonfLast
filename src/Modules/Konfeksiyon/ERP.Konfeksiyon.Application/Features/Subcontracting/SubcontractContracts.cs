using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Konfeksiyon.Application.Features.Sales;
using ERP.Konfeksiyon.Domain.Enums;
using FluentValidation;

namespace ERP.Konfeksiyon.Application.Features.Subcontracting;

/// <summary>Fason işlemlerinin hata kodları.</summary>
public static class SubcontractErrors
{
    /// <summary>Demet fasoncuda; sahada (tablet / elle bildirim) işlenemez.</summary>
    public const string BundleAtSubcontractor = "BUNDLE_AT_SUBCONTRACTOR";

    public const string InvalidState = "SUBCONTRACT_INVALID_STATE";
    public const string QuantityInvalid = "SUBCONTRACT_QUANTITY_INVALID";
    public const string BundleNotSendable = "SUBCONTRACT_BUNDLE_NOT_SENDABLE";
    public const string PriceOverlap = "SUBCONTRACT_PRICE_EXISTS";
}

/// <summary>Bir para birimindeki tutar.</summary>
public sealed record CurrencyAmountDto(string Currency, decimal Amount);

// ---------------------------------------------------------------- Fason fiyatları

/// <param name="ProductId">Boşsa operasyonun bütün ürünleri için geçerli fiyat.</param>
/// <param name="IsCurrent">Bugün geçerli olan fiyat (aynı fasoncu, operasyon ve ürün için başlangıcı en yeni geçmiş kayıt).</param>
public sealed record SubcontractPriceDto(
    int Id,
    int SupplierId,
    string SupplierCode,
    string SupplierName,
    int OperationId,
    string OperationCode,
    string OperationName,
    int? ProductId,
    string? ProductCode,
    string? ProductName,
    decimal UnitPrice,
    string Currency,
    DateOnly ValidFrom,
    string? Notes,
    bool IsCurrent,
    DateTime? UpdatedAt,
    string? UpdatedBy);

public sealed class SubcontractPriceQuery : PageRequest
{
    /// <summary>Fasoncu, operasyon ya da ürün kodu / adı içinde aranır.</summary>
    public string? Search { get; set; }

    public int? SupplierId { get; set; }
    public int? OperationId { get; set; }

    /// <summary>Yalnızca bugün geçerli fiyatlar.</summary>
    public bool? CurrentOnly { get; set; }
}

/// <param name="Currency">ISO 4217; boşsa TRY.</param>
public sealed record SubcontractPriceRequest(
    int SupplierId,
    int OperationId,
    int? ProductId,
    decimal UnitPrice,
    string? Currency,
    DateOnly ValidFrom,
    string? Notes = null);

/// <summary>Bir gün için geçerli fason fiyatı; bulunamazsa fiyat boş.</summary>
public sealed record ResolvedSubcontractPriceDto(
    int SupplierId, int OperationId, int? ProductId, DateOnly Date, int? PriceId, decimal? UnitPrice, string? Currency,
    DateOnly? ValidFrom);

public sealed class SubcontractPriceRequestValidator : AbstractValidator<SubcontractPriceRequest>
{
    public SubcontractPriceRequestValidator()
    {
        RuleFor(x => x.SupplierId).GreaterThan(0).WithMessage("Fasoncu seçilmelidir.");
        RuleFor(x => x.OperationId).GreaterThan(0).WithMessage("Operasyon seçilmelidir.");
        RuleFor(x => x.UnitPrice)
            .InclusiveBetween(0, 1_000_000).WithMessage("Birim fiyat 0 ile 1.000.000 arasında olmalıdır.")
            .Must(value => decimal.Round(value, 4) == value).WithMessage("Birim fiyat en fazla 4 ondalık basamak içerebilir.");
        RuleFor(x => x.Currency)
            .Matches("^[A-Za-z]{3}$").WithMessage("Para birimi üç harfli kod olmalıdır (örn. TRY, USD, EUR).")
            .When(x => !string.IsNullOrWhiteSpace(x.Currency));
        RuleFor(x => x.ValidFrom).NotEqual(default(DateOnly)).WithMessage("Başlangıç tarihi zorunludur.");
        RuleFor(x => x.Notes).OptionalText(500);
    }
}

// ---------------------------------------------------------------- Gönderim

/// <summary>Bir rotada fason olarak işaretlenmiş operasyon ve varsayılan fasoncusu.</summary>
public sealed record SubcontractOperationOptionDto(
    int OperationId, string OperationCode, string OperationName, int? DefaultSupplierId, string? DefaultSupplierName);

public sealed class SubcontractCandidateQuery
{
    /// <summary>Fasona gönderilecek operasyon.</summary>
    public int OperationId { get; set; }

    /// <summary>Demet no, sipariş no, model ya da ürün kodu içinde aranır.</summary>
    public string? Search { get; set; }

    public int? ProductionOrderId { get; set; }

    public int Take { get; set; } = 200;
}

/// <summary>
/// Fasona gönderilebilecek demet: sıradaki (başlatılabilir) operasyonu seçilen operasyon olan, sahada açık ve
/// fasoncuda olmayan demet.
/// </summary>
/// <param name="Quantity">Operasyonda kalan (gönderilecek) adet.</param>
/// <param name="IsSubcontractedStep">Rotada bu adım fason olarak işaretli.</param>
public sealed record SubcontractCandidateDto(
    long BundleOperationId,
    int BundleId,
    string BundleNo,
    int ProductionOrderId,
    string OrderNo,
    int ProductId,
    string ProductCode,
    string ModelCode,
    int ColorId,
    string ColorName,
    int SizeId,
    string SizeName,
    int SequenceNo,
    int Quantity,
    bool IsSubcontractedStep,
    int? DefaultSupplierId);

public sealed class SubcontractSendListQuery : PageRequest
{
    /// <summary>Fiş no, fasoncu ya da operasyon içinde aranır.</summary>
    public string? Search { get; set; }

    public int? SupplierId { get; set; }
    public SubcontractSendStatus? Status { get; set; }

    /// <summary>Yalnızca fasoncuda parçası kalan (açık ya da kısmen dönmüş) fişler.</summary>
    public bool? OpenOnly { get; set; }
}

/// <param name="Outstanding">Hâlâ fasoncuda olan adet.</param>
public sealed record SubcontractSendListItemDto(
    int Id,
    string SendNo,
    int SupplierId,
    string SupplierName,
    DateOnly SendDate,
    int OperationId,
    string OperationCode,
    string OperationName,
    SubcontractSendStatus Status,
    int LineCount,
    int TotalQuantity,
    int ReturnedGood,
    int ReturnedDefective,
    int ReturnedMissing,
    int Outstanding,
    string? CreatedBy);

/// <param name="UnitPrice">Gönderim tarihindeki fason fiyatı (adet başı); fiyat tablosunda yoksa boş.</param>
public sealed record SubcontractSendLineDto(
    int Id,
    int? BundleId,
    string? BundleNo,
    long? BundleOperationId,
    int? ProductionOrderId,
    string? OrderNo,
    int ProductId,
    string ProductCode,
    string ProductName,
    string? ModelCode,
    int ColorId,
    string ColorName,
    int SizeId,
    string SizeName,
    int SizeSortOrder,
    int Quantity,
    int ReturnedGood,
    int ReturnedDefective,
    int ReturnedMissing,
    int Outstanding,
    decimal? UnitPrice,
    string Currency);

/// <summary>Gönderime karşı işlenmiş bir dönüş fişinin özeti.</summary>
public sealed record SubcontractReturnSummaryDto(
    int Id,
    string ReturnNo,
    DateOnly ReturnDate,
    string? SupplierDocumentNo,
    int GoodQuantity,
    int DefectiveQuantity,
    int MissingQuantity,
    IReadOnlyList<CurrencyAmountDto> Amounts,
    bool IsPaid,
    DateOnly? PaidDate,
    string? CreatedBy);

/// <param name="DaysOutside">Fiş açıksa gönderimden bugüne geçen gün; kapalıysa boş.</param>
public sealed record SubcontractSendDto(
    int Id,
    string SendNo,
    int SupplierId,
    string SupplierCode,
    string SupplierName,
    DateOnly SendDate,
    int OperationId,
    string OperationCode,
    string OperationName,
    SubcontractSendStatus Status,
    string? Carrier,
    string? Notes,
    bool CanCancel,
    bool CanReturn,
    int TotalQuantity,
    int Outstanding,
    int? DaysOutside,
    string RowVersion,
    DateTime CreatedAt,
    string? CreatedBy,
    IReadOnlyList<SubcontractSendLineDto> Lines,
    IReadOnlyList<SubcontractReturnSummaryDto> Returns);

/// <summary>Demetsiz gönderim satırı: ürün / renk / beden adedi.</summary>
public sealed record SubcontractSendItemRequest(int ProductId, int ColorId, int SizeId, int Quantity);

/// <param name="BundleOperationIds">Gönderilen demetlerin bu operasyondaki adımları; demetin kalan adedi gönderilir.</param>
/// <param name="Items">Demetsiz satırlar (ürün / renk / beden adedi); üretim akışını etkilemez.</param>
public sealed record SubcontractSendRequest(
    int SupplierId,
    DateOnly SendDate,
    int OperationId,
    string? Carrier,
    string? Notes,
    IReadOnlyList<long>? BundleOperationIds,
    IReadOnlyList<SubcontractSendItemRequest>? Items);

public sealed record SubcontractVersionRequest(string RowVersion);

/// <summary>Basılı gönderim irsaliyesi: fiş, gönderen (fabrika) ve alıcı (fasoncu).</summary>
public sealed record SubcontractSendDocumentDto(
    SubcontractSendDto Send, ProformaPartyDto? Sender, ProformaPartyDto Receiver, DateTime PrintedAt);

public sealed class SubcontractSendRequestValidator : AbstractValidator<SubcontractSendRequest>
{
    public const int MaxLines = 500;

    public SubcontractSendRequestValidator()
    {
        RuleFor(x => x.SupplierId).GreaterThan(0).WithMessage("Fasoncu seçilmelidir.");
        RuleFor(x => x.OperationId).GreaterThan(0).WithMessage("Operasyon seçilmelidir.");
        RuleFor(x => x.SendDate).NotEqual(default(DateOnly)).WithMessage("Gönderim tarihi zorunludur.");
        RuleFor(x => x.Carrier).OptionalText(200);
        RuleFor(x => x.Notes).OptionalText(1000);
        RuleFor(x => x)
            .Must(x => (x.BundleOperationIds?.Count ?? 0) + (x.Items?.Count ?? 0) > 0)
            .OverridePropertyName("lines")
            .WithMessage("En az bir demet ya da ürün satırı seçilmelidir.");
        RuleFor(x => x)
            .Must(x => (x.BundleOperationIds?.Count ?? 0) + (x.Items?.Count ?? 0) <= MaxLines)
            .OverridePropertyName("lines")
            .WithMessage($"Bir fişte en fazla {MaxLines} satır olabilir.");
        RuleFor(x => x.BundleOperationIds)
            .Must(ids => ids is null || ids.Distinct().Count() == ids.Count)
            .WithMessage("Aynı demet birden fazla kez seçilemez.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).GreaterThan(0).WithMessage("Ürün seçilmelidir.");
            item.RuleFor(i => i.ColorId).GreaterThan(0).WithMessage("Renk seçilmelidir.");
            item.RuleFor(i => i.SizeId).GreaterThan(0).WithMessage("Beden seçilmelidir.");
            item.RuleFor(i => i.Quantity).InclusiveBetween(1, 1_000_000).WithMessage("Adet 1 ile 1.000.000 arasında olmalıdır.");
        });
    }
}

public sealed class SubcontractVersionRequestValidator : AbstractValidator<SubcontractVersionRequest>
{
    public SubcontractVersionRequestValidator()
    {
        RuleFor(x => x.RowVersion).NotEmpty().WithMessage("Kayıt sürümü (rowVersion) zorunludur.");
    }
}

// ---------------------------------------------------------------- Dönüş ve hakediş

/// <param name="UnitPrice">Hakediş birim fiyatı; boşsa gönderim satırının fiyatı.</param>
public sealed record SubcontractReturnLineRequest(
    int SendLineId, int GoodQuantity, int DefectiveQuantity, int MissingQuantity, int? DefectTypeId = null,
    decimal? UnitPrice = null);

/// <param name="RowVersion">Gönderim fişinin sürümü.</param>
/// <param name="SupplierDocumentNo">Fasoncunun irsaliye numarası.</param>
public sealed record SubcontractReturnRequest(
    int SubcontractSendId,
    string RowVersion,
    DateOnly ReturnDate,
    string? SupplierDocumentNo,
    string? Notes,
    IReadOnlyList<SubcontractReturnLineRequest> Lines);

public sealed class SubcontractReturnListQuery : PageRequest
{
    /// <summary>Dönüş no, gönderim no, fasoncu ya da fasoncu irsaliye no içinde aranır.</summary>
    public string? Search { get; set; }

    public int? SupplierId { get; set; }
    public int? SubcontractSendId { get; set; }

    /// <summary>Dönüş günü aralığı (dahil).</summary>
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }

    /// <summary>true: yalnızca ödenmiş, false: yalnızca ödenmemiş hakedişler.</summary>
    public bool? Paid { get; set; }
}

/// <param name="Amounts">Hakediş tutarı (sağlam × fiyat), para birimi başına.</param>
/// <param name="HasUnpricedLines">Fiyatı olmayan satır var; tutar eksik olabilir.</param>
public sealed record SubcontractReturnListItemDto(
    int Id,
    string ReturnNo,
    DateOnly ReturnDate,
    int SubcontractSendId,
    string SendNo,
    int SupplierId,
    string SupplierName,
    string OperationName,
    string? SupplierDocumentNo,
    int GoodQuantity,
    int DefectiveQuantity,
    int MissingQuantity,
    bool HasUnpricedLines,
    bool IsPaid,
    DateOnly? PaidDate,
    IReadOnlyList<CurrencyAmountDto> Amounts);

public sealed record SubcontractReturnLineDto(
    int Id,
    int SendLineId,
    string? BundleNo,
    string? OrderNo,
    string ProductCode,
    string ProductName,
    string ColorName,
    string SizeName,
    int SentQuantity,
    int GoodQuantity,
    int DefectiveQuantity,
    int MissingQuantity,
    int? DefectTypeId,
    string? DefectTypeName,
    decimal? UnitPrice,
    string Currency,
    decimal Amount);

public sealed record SubcontractReturnDto(
    int Id,
    string ReturnNo,
    DateOnly ReturnDate,
    int SubcontractSendId,
    string SendNo,
    DateOnly SendDate,
    int SupplierId,
    string SupplierName,
    int OperationId,
    string OperationName,
    string? SupplierDocumentNo,
    string? Notes,
    bool IsPaid,
    DateOnly? PaidDate,
    string? PaidBy,
    string? PaymentNote,
    string RowVersion,
    DateTime CreatedAt,
    string? CreatedBy,
    IReadOnlyList<CurrencyAmountDto> Amounts,
    IReadOnlyList<SubcontractReturnLineDto> Lines);

public sealed record SubcontractReturnPriceRequest(int ReturnLineId, decimal? UnitPrice);

/// <summary>Ödenmemiş hakedişin satır fiyatlarını düzeltir.</summary>
public sealed record SubcontractReturnPricesRequest(string RowVersion, IReadOnlyList<SubcontractReturnPriceRequest> Lines);

/// <param name="Paid">true: ödendi olarak işaretle; false: işareti kaldır.</param>
/// <param name="PaidDate">Ödeme günü; boşsa bugün.</param>
public sealed record SubcontractPaymentRequest(string RowVersion, bool Paid, DateOnly? PaidDate = null, string? Note = null);

/// <summary>Hakediş özeti: fasoncu başına dönüş sayısı, sağlam adet ve tutarlar (toplam / ödenen / ödenmeyen).</summary>
public sealed record SubcontractStatementSupplierDto(
    int SupplierId,
    string SupplierName,
    int ReturnCount,
    int GoodQuantity,
    IReadOnlyList<CurrencyAmountDto> Total,
    IReadOnlyList<CurrencyAmountDto> Paid,
    IReadOnlyList<CurrencyAmountDto> Unpaid);

public sealed record SubcontractStatementDto(
    IReadOnlyList<SubcontractStatementSupplierDto> Suppliers,
    IReadOnlyList<CurrencyAmountDto> Total,
    IReadOnlyList<CurrencyAmountDto> Paid,
    IReadOnlyList<CurrencyAmountDto> Unpaid);

public sealed class SubcontractReturnRequestValidator : AbstractValidator<SubcontractReturnRequest>
{
    public SubcontractReturnRequestValidator()
    {
        RuleFor(x => x.SubcontractSendId).GreaterThan(0).WithMessage("Gönderim fişi seçilmelidir.");
        RuleFor(x => x.RowVersion).NotEmpty().WithMessage("Kayıt sürümü (rowVersion) zorunludur.");
        RuleFor(x => x.ReturnDate).NotEqual(default(DateOnly)).WithMessage("Dönüş tarihi zorunludur.");
        RuleFor(x => x.SupplierDocumentNo).OptionalText(50);
        RuleFor(x => x.Notes).OptionalText(1000);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("En az bir satırda dönen adet girilmelidir.");
        RuleFor(x => x.Lines)
            .Must(lines => lines is null || lines.Select(l => l.SendLineId).Distinct().Count() == lines.Count)
            .WithMessage("Aynı satır birden fazla kez girilemez.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.GoodQuantity).GreaterThanOrEqualTo(0).WithMessage("Sağlam adet negatif olamaz.");
            line.RuleFor(l => l.DefectiveQuantity).GreaterThanOrEqualTo(0).WithMessage("Hatalı adet negatif olamaz.");
            line.RuleFor(l => l.MissingQuantity).GreaterThanOrEqualTo(0).WithMessage("Eksik adet negatif olamaz.");
            line.RuleFor(l => l.UnitPrice)
                .Must(p => p is null || (p >= 0 && p <= 1_000_000 && decimal.Round(p.Value, 4) == p))
                .WithMessage("Birim fiyat negatif olamaz ve en fazla 4 ondalık basamak içerebilir.");
        });
    }
}

public sealed class SubcontractReturnPricesRequestValidator : AbstractValidator<SubcontractReturnPricesRequest>
{
    public SubcontractReturnPricesRequestValidator()
    {
        RuleFor(x => x.RowVersion).NotEmpty().WithMessage("Kayıt sürümü (rowVersion) zorunludur.");
        RuleFor(x => x.Lines).NotNull().WithMessage("Satırlar gönderilmelidir.");
        RuleForEach(x => x.Lines).ChildRules(line =>
            line.RuleFor(l => l.UnitPrice)
                .Must(p => p is null || (p >= 0 && p <= 1_000_000 && decimal.Round(p.Value, 4) == p))
                .WithMessage("Birim fiyat negatif olamaz ve en fazla 4 ondalık basamak içerebilir."));
    }
}

public sealed class SubcontractPaymentRequestValidator : AbstractValidator<SubcontractPaymentRequest>
{
    public SubcontractPaymentRequestValidator()
    {
        RuleFor(x => x.RowVersion).NotEmpty().WithMessage("Kayıt sürümü (rowVersion) zorunludur.");
        RuleFor(x => x.Note).OptionalText(500);
    }
}

public sealed class SubcontractStatementQuery
{
    public int? SupplierId { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
}
