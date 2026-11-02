using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.Finance;
using ERP.Core.Application.Features.MasterData;
using ERP.Core.Domain.Enums;
using ERP.Dokuma.Application.Features.Orders;
using ERP.Dokuma.Application.Features.Weaving;
using ERP.Dokuma.Domain.Entities;
using ERP.Dokuma.Domain.Enums;
using ERP.Dokuma.Domain.Rules;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Dokuma.Application.Features.Quotations;

public sealed class DokumaQuotationQuery : PageRequest
{
    /// <summary>Teklif no, müşteri ya da aday firma adında aranır.</summary>
    public string? Search { get; set; }
}

/// <param name="PartyName">Müşteri adı ya da aday firma adı.</param>
/// <param name="IsProspect">Müşteri kartı yok; aday firmaya verilmiş.</param>
/// <param name="OrderNo">Kabulde açılan sipariş.</param>
public sealed record DokumaQuotationListItemDto(
    int Id,
    string QuotationNo,
    DateOnly QuotationDate,
    DateOnly ValidUntil,
    string PartyName,
    bool IsProspect,
    OrderOwnership Ownership,
    int LineCount,
    decimal TotalMeters,
    decimal TotalAmount,
    string Currency,
    QuotationStatus Status,
    int? OrderId,
    string? OrderNo);

/// <param name="KilogramsPerMeter">Bir metre kumaş için bu iplikten gereken (kg, artikel firesi dahil).</param>
/// <param name="UnitPrice">Kilo fiyatı (teklifin para biriminde).</param>
/// <param name="CostPerMeter">Kilo × fiyat; fasonda fiyata girmez.</param>
/// <param name="PriceSource">Fiyatın kaynağı; boşsa fiyat bulunamamıştır.</param>
public sealed record DokumaQuotationLineYarnDto(
    int YarnId,
    string YarnCode,
    string YarnName,
    string? YarnColor,
    bool UsedInWarp,
    bool UsedInWeft,
    decimal KilogramsPerMeter,
    decimal UnitPrice,
    decimal CostPerMeter,
    string? PriceSource);

/// <summary>
/// Teklif satırı ve metre başına ön maliyet dökümü. Tutarlar teklifin para birimindedir. Doğrudan maliyet = iplik + haşıl +
/// çözgü + dokuma; fire doğrudan maliyete, genel gider fire eklenmiş maliyete uygulanır; kâr marjı toplam maliyete eklenir.
/// </summary>
/// <param name="Id">Kayıtlı satırda Id; hesaplama önizlemesinde boş.</param>
/// <param name="MetersPerHour">Tezgâhın saatte dokuduğu kumaş: devir/dk × 60 ÷ (atkı/cm × 100) × verim.</param>
/// <param name="SizingCostPerWarpMeter">Bir metre çözgünün haşıl maliyeti.</param>
/// <param name="WarpingCostPerWarpMeter">Bir metre çözgünün çözgü maliyeti.</param>
/// <param name="SizingCost">Metre kumaş başına haşıl: çözgü metresi × (1 + çözgü çekmesi).</param>
/// <param name="WarpingCost">Metre kumaş başına çözgü: çözgü metresi × (1 + çözgü çekmesi).</param>
/// <param name="WeavingHoursPerMeter">1 ÷ saatte dokunan metre.</param>
/// <param name="WeavingCost">Süre × (tezgâh + işçilik saat maliyeti).</param>
/// <param name="CostPerMeter">Doğrudan maliyet + fire + genel gider (kâr hariç).</param>
/// <param name="CalculatedPrice">Maliyet × (1 + kâr marjı).</param>
/// <param name="UnitPrice">Teklif edilen metre fiyatı.</param>
/// <param name="ActualMarginPercent">Teklif fiyatının maliyete göre kârı (%).</param>
/// <param name="Warnings">Hesapta varsayılan kullanılan ya da eksik bilgi.</param>
public sealed record DokumaQuotationLineDto(
    int? Id,
    int Sequence,
    int ArticleId,
    string ArticleCode,
    string ArticleName,
    decimal RawWidthCm,
    int RawWeightGsm,
    decimal ReedWidthCm,
    decimal? WarpCrimpPercent,
    decimal Meters,
    string? CustomerArticleCode,
    int? LoomId,
    string? LoomCode,
    decimal SpeedRpm,
    decimal EfficiencyPercent,
    decimal PicksPerCm,
    decimal MetersPerHour,
    decimal WarpKilogramsPerMeter,
    decimal WeftKilogramsPerMeter,
    decimal YarnCost,
    decimal SizingCostPerWarpMeter,
    decimal WarpingCostPerWarpMeter,
    decimal SizingCost,
    decimal WarpingCost,
    decimal LoomHourlyCost,
    decimal LaborHourlyCost,
    decimal WeavingHoursPerMeter,
    decimal WeavingCost,
    decimal DirectCost,
    decimal WastePercent,
    decimal WasteCost,
    decimal OverheadPercent,
    decimal OverheadCost,
    decimal CostPerMeter,
    decimal MarginPercent,
    decimal CalculatedPrice,
    decimal UnitPrice,
    decimal Amount,
    decimal? ActualMarginPercent,
    string? Notes,
    IReadOnlyList<DokumaQuotationLineYarnDto> Yarns,
    IReadOnlyList<string> Warnings);

public sealed record DokumaQuotationDto(
    int Id,
    string QuotationNo,
    DateOnly QuotationDate,
    DateOnly ValidUntil,
    int? CustomerId,
    string? CustomerName,
    string? ProspectName,
    string? ContactName,
    OrderOwnership Ownership,
    string Currency,
    DocumentLanguage DocumentLanguage,
    int? PaymentTermDays,
    string? DeliveryTerms,
    string? Notes,
    QuotationStatus Status,
    DateTime? SentAt,
    DateTime? DecidedAt,
    string? DecidedBy,
    string? RejectionReason,
    int? OrderId,
    string? OrderNo,
    decimal TotalMeters,
    decimal TotalCost,
    decimal TotalAmount,
    DateTime CreatedAt,
    string? CreatedBy,
    IReadOnlyList<DokumaQuotationLineDto> Lines);

/// <summary>Kaydetmeden hesaplanan teklif: formda girilen değerlerle ön maliyet dökümü.</summary>
/// <param name="CostCurrency">Maliyet parametrelerinin para birimi; teklifin para biriminden farklıysa kurla çevrilmiştir.</param>
/// <param name="ExchangeRate">Maliyet para biriminin 1 biriminin teklif para birimindeki karşılığı (döviz alış).</param>
public sealed record DokumaQuotationCalculationDto(
    string Currency,
    string CostCurrency,
    decimal ExchangeRate,
    DateOnly ValidUntil,
    DocumentLanguage DocumentLanguage,
    decimal TotalMeters,
    decimal TotalCost,
    decimal TotalAmount,
    IReadOnlyList<DokumaQuotationLineDto> Lines);

public sealed record DokumaQuotationPrintLineDto(
    string ArticleCode,
    string ArticleName,
    string? CustomerArticleCode,
    string? WeaveName,
    string? Composition,
    decimal RawWidthCm,
    int RawWeightGsm,
    decimal Meters,
    decimal UnitPrice,
    decimal Amount,
    string? Notes);

/// <summary>Müşteriye basılan teklif: maliyet dökümü basılmaz, yalnızca artikel, metre ve fiyat.</summary>
public sealed record DokumaQuotationPrintDto(
    int Id,
    string QuotationNo,
    DateOnly QuotationDate,
    DateOnly ValidUntil,
    QuotationStatus Status,
    string Currency,
    OrderOwnership Ownership,
    DocumentLanguage DocumentLanguage,
    string? CustomerCode,
    string PartyName,
    string? CustomerTaxNumber,
    string? CustomerTaxOffice,
    string? CustomerAddress,
    string? ContactName,
    int? PaymentTermDays,
    string? DeliveryTerms,
    string? Notes,
    decimal TotalMeters,
    decimal Total,
    IReadOnlyList<DokumaQuotationPrintLineDto> Lines);

/// <summary>Satırdaki ipliğin elle verilen kilo fiyatı (teklifin para biriminde).</summary>
public sealed record DokumaQuotationYarnPriceRequest(int YarnId, decimal UnitPrice);

/// <summary>Teklif satırı. Boş bırakılan hesap girdileri tezgâhtan, artikelden ve maliyet parametrelerinden alınır.</summary>
/// <param name="LoomId">Hız, verim ve tezgâh saat maliyetinin alınacağı tezgâh; boşsa uygun tezgâhların ortalama hızı.</param>
/// <param name="PicksPerCm">Boşsa artikelin atkı sıklığı.</param>
/// <param name="LoomHourlyCost">Teklifin para biriminde; boşsa tezgâha özel ya da genel değer.</param>
/// <param name="WarpingCostPerMeter">Bir metre çözgü için; boşsa parametreden.</param>
/// <param name="SizingCostPerMeter">Bir metre çözgü için; boşsa reçeteden ya da parametreden.</param>
/// <param name="UnitPrice">Teklif edilen metre fiyatı; boşsa hesaplanan fiyat (iki ondalığa yuvarlanmış).</param>
public sealed record DokumaQuotationLineRequest(
    int ArticleId,
    decimal Meters,
    int? LoomId = null,
    decimal? SpeedRpm = null,
    decimal? EfficiencyPercent = null,
    decimal? PicksPerCm = null,
    decimal? LoomHourlyCost = null,
    decimal? LaborHourlyCost = null,
    decimal? WarpingCostPerMeter = null,
    decimal? SizingCostPerMeter = null,
    decimal? WastePercent = null,
    decimal? OverheadPercent = null,
    decimal? MarginPercent = null,
    decimal? UnitPrice = null,
    IReadOnlyList<DokumaQuotationYarnPriceRequest>? YarnPrices = null,
    string? CustomerArticleCode = null,
    string? Notes = null);

/// <param name="CustomerId">Kayıtlı müşteri; boşsa <paramref name="ProspectName"/> zorunlu.</param>
/// <param name="ValidUntil">Boşsa teklif tarihi + parametredeki geçerlilik süresi.</param>
/// <param name="Currency">Boşsa müşterinin para birimi, müşteri yoksa TRY.</param>
/// <param name="DocumentLanguage">Boşsa müşterinin belge dili, müşteri yoksa Türkçe.</param>
/// <param name="PaymentTermDays">Boşsa müşteri kartındaki vade.</param>
public sealed record DokumaQuotationRequest(
    int? CustomerId,
    string? ProspectName,
    DateOnly QuotationDate,
    IReadOnlyList<DokumaQuotationLineRequest> Lines,
    OrderOwnership Ownership = OrderOwnership.OwnGoods,
    DateOnly? ValidUntil = null,
    string? Currency = null,
    DocumentLanguage? DocumentLanguage = null,
    string? ContactName = null,
    int? PaymentTermDays = null,
    string? DeliveryTerms = null,
    string? Notes = null);

/// <param name="CustomerId">Teklif aday firmaya verilmişse siparişin müşterisi (zorunlu).</param>
/// <param name="OrderDate">Boşsa bugün.</param>
public sealed record AcceptDokumaQuotationRequest(int? CustomerId = null, DateOnly? OrderDate = null, DateOnly? DueDate = null, string? CustomerOrderNo = null);

public sealed record RejectDokumaQuotationRequest(string? Reason = null);

public sealed class DokumaQuotationRequestValidator : AbstractValidator<DokumaQuotationRequest>
{
    public const int MaxLines = 100;

    public DokumaQuotationRequestValidator()
    {
        RuleFor(x => x.ProspectName)
            .NotEmpty().WithMessage("Müşteri seçin ya da aday firma adını yazın.")
            .When(x => x.CustomerId is null);
        RuleFor(x => x.ProspectName).OptionalText(200);
        RuleFor(x => x.ContactName).OptionalText(100);
        RuleFor(x => x.DeliveryTerms).OptionalText(200);
        RuleFor(x => x.Notes).OptionalText(1000);
        RuleFor(x => x.QuotationDate).NotEmpty().WithMessage("Teklif tarihi zorunludur.");
        RuleFor(x => x.Ownership).IsInEnum().WithMessage("Geçerli bir iş türü seçin.");
        RuleFor(x => x.DocumentLanguage).IsInEnum().WithMessage("Geçerli bir belge dili seçin.").When(x => x.DocumentLanguage is not null);
        RuleFor(x => x.ValidUntil)
            .Must((request, until) => until >= request.QuotationDate).WithMessage("Geçerlilik tarihi teklif tarihinden önce olamaz.")
            .When(x => x.ValidUntil is not null);
        RuleFor(x => x.PaymentTermDays).InclusiveBetween(0, 365).WithMessage("Vade 0 ile 365 gün arasında olmalıdır.").When(x => x.PaymentTermDays is not null);
        RuleFor(x => x.Currency)
            .Matches(@"^\s*[A-Za-z]{3}\s*$").WithMessage("Para birimi üç harfli ISO kodu olmalıdır (örn. TRY, EUR).")
            .When(x => !string.IsNullOrWhiteSpace(x.Currency));
        RuleFor(x => x.Lines)
            .NotEmpty().WithMessage("En az bir satır girilmelidir.")
            .Must(lines => lines.Count <= MaxLines).WithMessage($"Bir teklifte en fazla {MaxLines} satır olabilir.")
            .Custom((lines, context) =>
            {
                if (lines is null)
                {
                    return;
                }

                for (var i = 0; i < lines.Count; i++)
                {
                    var line = lines[i];
                    var no = i + 1;
                    if (line.ArticleId <= 0)
                    {
                        context.AddFailure($"{no}. satır: artikel seçilmelidir.");
                    }

                    if (line.Meters is <= 0 or > 9_999_999)
                    {
                        context.AddFailure($"{no}. satır: metre sıfırdan büyük olmalıdır.");
                    }

                    if (line.SpeedRpm is <= 0 or > 5000)
                    {
                        context.AddFailure($"{no}. satır: tezgâh hızı 0 ile 5000 devir/dk arasında olmalıdır.");
                    }

                    if (line.EfficiencyPercent is <= 0 or > 100)
                    {
                        context.AddFailure($"{no}. satır: verim 0'dan büyük, en fazla 100 olmalıdır.");
                    }

                    if (line.PicksPerCm is <= 0 or > 1000)
                    {
                        context.AddFailure($"{no}. satır: atkı sıklığı 0 ile 1000 arasında olmalıdır.");
                    }

                    if (line.LoomHourlyCost is < 0 || line.LaborHourlyCost is < 0 || line.WarpingCostPerMeter is < 0 || line.SizingCostPerMeter is < 0)
                    {
                        context.AddFailure($"{no}. satır: maliyetler negatif olamaz.");
                    }

                    if (line.WastePercent is < 0 or > 100)
                    {
                        context.AddFailure($"{no}. satır: fire 0 ile 100 arasında olmalıdır.");
                    }

                    if (line.OverheadPercent is < 0 or > 500 || line.MarginPercent is < 0 or > 500)
                    {
                        context.AddFailure($"{no}. satır: genel gider ve kâr marjı 0 ile 500 arasında olmalıdır.");
                    }

                    if (line.UnitPrice is < 0 or > 99_999_999)
                    {
                        context.AddFailure($"{no}. satır: teklif fiyatı geçersiz.");
                    }

                    if (line.YarnPrices?.Any(p => p.UnitPrice is < 0 or > 99_999_999) == true)
                    {
                        context.AddFailure($"{no}. satır: iplik fiyatı geçersiz.");
                    }

                    if (line.Notes?.Trim().Length > 500)
                    {
                        context.AddFailure($"{no}. satır: not en fazla 500 karakter olabilir.");
                    }

                    if (line.CustomerArticleCode?.Trim().Length > 50)
                    {
                        context.AddFailure($"{no}. satır: müşteri artikel kodu en fazla 50 karakter olabilir.");
                    }
                }
            });
    }
}

public sealed class AcceptDokumaQuotationRequestValidator : AbstractValidator<AcceptDokumaQuotationRequest>
{
    public AcceptDokumaQuotationRequestValidator()
    {
        RuleFor(x => x.CustomerOrderNo).OptionalText(50);
        RuleFor(x => x.DueDate)
            .Must((request, due) => request.OrderDate is null || due >= request.OrderDate).WithMessage("Termin sipariş tarihinden önce olamaz.")
            .When(x => x.DueDate is not null);
    }
}

public sealed class RejectDokumaQuotationRequestValidator : AbstractValidator<RejectDokumaQuotationRequest>
{
    public RejectDokumaQuotationRequestValidator()
    {
        RuleFor(x => x.Reason).OptionalText(500);
    }
}

public interface IDokumaQuotationService
{
    Task<PagedResult<DokumaQuotationListItemDto>> GetListAsync(DokumaQuotationQuery query, CancellationToken cancellationToken);
    Task<Result<DokumaQuotationDto>> GetAsync(int id, CancellationToken cancellationToken);
    Task<Result<DokumaQuotationPrintDto>> GetPrintAsync(int id, CancellationToken cancellationToken);
    Task<Result<DokumaQuotationCalculationDto>> CalculateAsync(DokumaQuotationRequest request, CancellationToken cancellationToken);
    Task<Result<DokumaQuotationDto>> CreateAsync(DokumaQuotationRequest request, CancellationToken cancellationToken);
    Task<Result<DokumaQuotationDto>> UpdateAsync(int id, DokumaQuotationRequest request, CancellationToken cancellationToken);
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken);
    Task<Result<DokumaQuotationDto>> SendAsync(int id, CancellationToken cancellationToken);
    Task<Result<DokumaQuotationDto>> ReopenAsync(int id, CancellationToken cancellationToken);
    Task<Result<DokumaQuotationDto>> RejectAsync(int id, RejectDokumaQuotationRequest request, CancellationToken cancellationToken);
    Task<Result<DokumaQuotationDto>> AcceptAsync(int id, AcceptDokumaQuotationRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Dokuma teklifi ve ön maliyet. Metre başına maliyet: çözgü ve atkı ipliği (artikel konstrüksiyonundan, sipariş iplik ihtiyacıyla aynı
/// formül, artikel firesi dahil) × son alış fiyatı; haşıl ve çözgü (çözgü metresi başına, çözgü çekmesiyle kumaş metresine çevrilir);
/// dokuma süresi (1 ÷ (devir/dk × 60 ÷ (atkı/cm × 100) × verim)) × (tezgâh + işçilik saat maliyeti). Doğrudan maliyete fire, sonra genel
/// gider eklenir; kâr marjıyla fiyat bulunur. Fasonda iplik müşterinin olduğu için iplik maliyeti fiyata girmez. Maliyet parametreleri
/// ve iplik / kimyasal fiyatları teklifin para birimine teklif tarihindeki döviz alış kuruyla çevrilir.
/// Durum: taslak → gönderildi → kabul (taslak sipariş açılır) / ret; geçerlilik tarihi geçen gönderilmiş teklif "süresi doldu" olur.
/// </summary>
public sealed class DokumaQuotationService(
    IAppDbContext db,
    IDocumentNumberGenerator numbers,
    IExchangeRateService exchangeRates,
    IDokumaOrderService orders,
    ICurrentUser currentUser,
    IAppLock appLock,
    TimeProvider timeProvider) : IDokumaQuotationService
{
    private const string QuotationSeries = "DokumaQuotation";
    private const string InvalidState = "DOKUMA_QUOTATION_INVALID_STATE";
    private const string MissingRate = "DOKUMA_QUOTATION_MISSING_RATE";
    private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(10);

    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Teklif bulunamadı.", ErrorType.NotFound);
    private static readonly Error Busy = new("DOKUMA_BUSY", "Aynı anda başka bir işlem yapılıyor; tekrar deneyin.", ErrorType.Conflict);

    private DateOnly Today() => DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);

    public async Task<PagedResult<DokumaQuotationListItemDto>> GetListAsync(DokumaQuotationQuery query, CancellationToken cancellationToken)
    {
        await ExpireOverdueAsync(cancellationToken);
        var quotations = db.Set<DokumaQuotation>().AsNoTracking();
        if (NullIfBlank(query.Search) is { } term)
        {
            quotations = quotations.Where(q => q.QuotationNo.Contains(term)
                || (q.ProspectName != null && q.ProspectName.Contains(term))
                || (q.Customer != null && q.Customer.Name.Contains(term)));
        }

        return await quotations
            .OrderByDescending(q => q.QuotationDate).ThenByDescending(q => q.Id)
            .Select(q => new DokumaQuotationListItemDto(
                q.Id,
                q.QuotationNo,
                q.QuotationDate,
                q.ValidUntil,
                q.Customer != null ? q.Customer.Name : q.ProspectName ?? "",
                q.CustomerId == null,
                q.Ownership,
                q.Lines.Count,
                q.Lines.Sum(l => (decimal?)l.Meters) ?? 0,
                q.Lines.Sum(l => (decimal?)(l.Meters * l.UnitPrice)) ?? 0,
                q.Currency,
                q.Status,
                q.OrderId,
                q.Order != null ? q.Order.OrderNo : null))
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<Result<DokumaQuotationDto>> GetAsync(int id, CancellationToken cancellationToken)
    {
        await ExpireOverdueAsync(cancellationToken);
        var quotation = await db.Set<DokumaQuotation>().AsNoTracking()
            .Include(q => q.Customer)
            .Include(q => q.Order)
            .Include(q => q.Lines).ThenInclude(l => l.Article)
            .Include(q => q.Lines).ThenInclude(l => l.Loom)
            .Include(q => q.Lines).ThenInclude(l => l.Yarns).ThenInclude(y => y.Yarn)
            .SingleOrDefaultAsync(q => q.Id == id, cancellationToken);
        if (quotation is null)
        {
            return NotFound;
        }

        var lines = quotation.Lines.OrderBy(l => l.Sequence)
            .Select(l => ToLineDto(l, l.Article, l.Loom?.Code, l.Yarns.ToDictionary(y => y.YarnId, y => y.Yarn), quotation.Ownership, []))
            .ToList();

        return new DokumaQuotationDto(
            quotation.Id,
            quotation.QuotationNo,
            quotation.QuotationDate,
            quotation.ValidUntil,
            quotation.CustomerId,
            quotation.Customer?.Name,
            quotation.ProspectName,
            quotation.ContactName,
            quotation.Ownership,
            quotation.Currency,
            quotation.DocumentLanguage,
            quotation.PaymentTermDays,
            quotation.DeliveryTerms,
            quotation.Notes,
            quotation.Status,
            quotation.SentAt,
            quotation.DecidedAt,
            quotation.DecidedBy,
            quotation.RejectionReason,
            quotation.OrderId,
            quotation.Order?.OrderNo,
            lines.Sum(l => l.Meters),
            Math.Round(lines.Sum(l => l.Meters * l.CostPerMeter), 2),
            lines.Sum(l => l.Amount),
            quotation.CreatedAt,
            quotation.CreatedBy,
            lines);
    }

    /// <summary>Basılan teklif: müşteri (ya da aday firma), artikel, metre ve fiyat. Maliyet dökümü basılmaz.</summary>
    public async Task<Result<DokumaQuotationPrintDto>> GetPrintAsync(int id, CancellationToken cancellationToken)
    {
        var quotation = await db.Set<DokumaQuotation>().AsNoTracking()
            .Include(q => q.Customer).ThenInclude(c => c!.Addresses)
            .Include(q => q.Lines).ThenInclude(l => l.Article)
            .SingleOrDefaultAsync(q => q.Id == id, cancellationToken);
        if (quotation is null)
        {
            return NotFound;
        }

        var lines = quotation.Lines.OrderBy(l => l.Sequence).Select(l => new DokumaQuotationPrintLineDto(
            l.Article.ArticleCode,
            l.Article.Name,
            l.CustomerArticleCode,
            l.Article.WeaveName,
            l.Article.Composition,
            l.Article.RawWidthCm,
            l.Article.RawWeightGsm,
            l.Meters,
            l.UnitPrice,
            Math.Round(l.Meters * l.UnitPrice, 2),
            l.Notes)).ToList();

        var customer = quotation.Customer;
        var address = customer?.Addresses.FirstOrDefault(a => a.IsDefaultInvoice) ?? customer?.Addresses.OrderBy(a => a.Id).FirstOrDefault();
        return new DokumaQuotationPrintDto(
            quotation.Id,
            quotation.QuotationNo,
            quotation.QuotationDate,
            quotation.ValidUntil,
            quotation.Status,
            quotation.Currency,
            quotation.Ownership,
            quotation.DocumentLanguage,
            customer?.CustomerCode,
            customer?.Name ?? quotation.ProspectName ?? "",
            customer?.TaxNumber,
            customer?.TaxOffice,
            address is null
                ? null
                : $"{address.AddressLine} {address.District}/{address.City}{(address.CountryCode == "TR" ? "" : " " + address.CountryCode)}",
            quotation.ContactName,
            quotation.PaymentTermDays,
            quotation.DeliveryTerms,
            quotation.Notes,
            lines.Sum(l => l.Meters),
            lines.Sum(l => l.Amount),
            lines);
    }

    public async Task<Result<DokumaQuotationCalculationDto>> CalculateAsync(DokumaQuotationRequest request, CancellationToken cancellationToken)
    {
        if (await ValidateReferencesAsync(request, null, cancellationToken) is { } error)
        {
            return error;
        }

        var calculated = await CalculateCoreAsync(request, cancellationToken);
        if (!calculated.IsSuccess)
        {
            return calculated.Error!;
        }

        var calc = calculated.Value;
        var lines = calc.Lines
            .Select(l => ToLineDto(l.Line, calc.Articles[l.Line.ArticleId], l.LoomCode, calc.Yarns, request.Ownership, l.Warnings))
            .ToList();
        return new DokumaQuotationCalculationDto(
            calc.Currency,
            calc.CostCurrency,
            calc.CostRate,
            calc.ValidUntil,
            calc.DocumentLanguage,
            lines.Sum(l => l.Meters),
            Math.Round(lines.Sum(l => l.Meters * l.CostPerMeter), 2),
            lines.Sum(l => l.Amount),
            lines);
    }

    public async Task<Result<DokumaQuotationDto>> CreateAsync(DokumaQuotationRequest request, CancellationToken cancellationToken)
    {
        if (await ValidateReferencesAsync(request, null, cancellationToken) is { } error)
        {
            return error;
        }

        var calculated = await CalculateCoreAsync(request, cancellationToken);
        if (!calculated.IsSuccess)
        {
            return calculated.Error!;
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var year = request.QuotationDate.Year;
        var sequence = await numbers.NextAsync(QuotationSeries, year, cancellationToken);
        var quotation = new DokumaQuotation { QuotationNo = $"DTK-{year}-{sequence:D5}" };
        Apply(quotation, request, calculated.Value);
        foreach (var line in calculated.Value.Lines)
        {
            quotation.Lines.Add(line.Line);
        }

        db.Set<DokumaQuotation>().Add(quotation);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(quotation.Id, cancellationToken);
    }

    public async Task<Result<DokumaQuotationDto>> UpdateAsync(int id, DokumaQuotationRequest request, CancellationToken cancellationToken)
    {
        var quotation = await db.Set<DokumaQuotation>()
            .Include(q => q.Lines).ThenInclude(l => l.Yarns)
            .SingleOrDefaultAsync(q => q.Id == id, cancellationToken);
        if (quotation is null)
        {
            return NotFound;
        }

        if (quotation.Status != QuotationStatus.Draft)
        {
            return new Error(InvalidState, "Yalnızca taslak teklif düzenlenebilir; gönderilmiş teklifi önce taslağa alın.", ErrorType.Conflict);
        }

        if (await ValidateReferencesAsync(request, quotation, cancellationToken) is { } error)
        {
            return error;
        }

        var calculated = await CalculateCoreAsync(request, cancellationToken);
        if (!calculated.IsSuccess)
        {
            return calculated.Error!;
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // Satır sırası benzersiz olduğu için eski satırlar önce silinir; satırlar her kayıtta yeniden hesaplanır.
        foreach (var line in quotation.Lines.ToList())
        {
            db.Set<DokumaQuotationLineYarn>().RemoveRange(line.Yarns);
            db.Set<DokumaQuotationLine>().Remove(line);
        }

        await db.SaveChangesAsync(cancellationToken);

        Apply(quotation, request, calculated.Value);
        foreach (var line in calculated.Value.Lines)
        {
            quotation.Lines.Add(line.Line);
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var quotation = await db.Set<DokumaQuotation>()
            .Include(q => q.Lines).ThenInclude(l => l.Yarns)
            .SingleOrDefaultAsync(q => q.Id == id, cancellationToken);
        if (quotation is null)
        {
            return NotFound;
        }

        if (quotation.Status != QuotationStatus.Draft)
        {
            return new Error(InvalidState, "Yalnızca taslak teklif silinebilir.", ErrorType.Conflict);
        }

        foreach (var line in quotation.Lines)
        {
            db.Set<DokumaQuotationLineYarn>().RemoveRange(line.Yarns);
        }

        db.Set<DokumaQuotationLine>().RemoveRange(quotation.Lines);
        db.Set<DokumaQuotation>().Remove(quotation);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    /// <summary>Taslağı müşteriye gönderilmiş olarak işaretler; geçerlilik tarihi geçmiş teklif gönderilmez.</summary>
    public async Task<Result<DokumaQuotationDto>> SendAsync(int id, CancellationToken cancellationToken)
    {
        var quotation = await db.Set<DokumaQuotation>().SingleOrDefaultAsync(q => q.Id == id, cancellationToken);
        if (quotation is null)
        {
            return NotFound;
        }

        if (quotation.Status != QuotationStatus.Draft)
        {
            return new Error(InvalidState, "Yalnızca taslak teklif gönderilir.", ErrorType.Conflict);
        }

        if (quotation.ValidUntil < Today())
        {
            return new Error(InvalidState, "Teklifin geçerlilik tarihi geçmiş; tarihi güncelleyip gönderin.", ErrorType.Conflict);
        }

        quotation.Status = QuotationStatus.Sent;
        quotation.SentAt = timeProvider.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    /// <summary>Gönderilmiş, reddedilmiş ya da süresi dolmuş teklifi düzeltmek için taslağa alır; numara korunur.</summary>
    public async Task<Result<DokumaQuotationDto>> ReopenAsync(int id, CancellationToken cancellationToken)
    {
        var quotation = await db.Set<DokumaQuotation>().SingleOrDefaultAsync(q => q.Id == id, cancellationToken);
        if (quotation is null)
        {
            return NotFound;
        }

        if (quotation.Status is not (QuotationStatus.Sent or QuotationStatus.Rejected or QuotationStatus.Expired))
        {
            return new Error(InvalidState, "Yalnızca gönderilmiş, reddedilmiş ya da süresi dolmuş teklif taslağa alınır.", ErrorType.Conflict);
        }

        quotation.Status = QuotationStatus.Draft;
        quotation.SentAt = null;
        quotation.DecidedAt = null;
        quotation.DecidedBy = null;
        quotation.RejectionReason = null;
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<Result<DokumaQuotationDto>> RejectAsync(int id, RejectDokumaQuotationRequest request, CancellationToken cancellationToken)
    {
        var quotation = await db.Set<DokumaQuotation>().SingleOrDefaultAsync(q => q.Id == id, cancellationToken);
        if (quotation is null)
        {
            return NotFound;
        }

        if (quotation.Status is not (QuotationStatus.Sent or QuotationStatus.Expired))
        {
            return new Error(InvalidState, "Yalnızca gönderilmiş ya da süresi dolmuş teklif reddedilir.", ErrorType.Conflict);
        }

        quotation.Status = QuotationStatus.Rejected;
        quotation.RejectionReason = NullIfBlank(request.Reason);
        quotation.DecidedAt = timeProvider.GetUtcNow().UtcDateTime;
        quotation.DecidedBy = currentUser.UserName;
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    /// <summary>
    /// Kabul: teklif kabul edildi olarak işaretlenir ve teklifin satırları, fiyatları, para birimi ve iş türüyle taslak sipariş açılır.
    /// Aday firmaya verilen teklifte müşteri kartı seçilir ve teklife yazılır. Sipariş açılamazsa teklif gönderildi durumuna döner.
    /// </summary>
    public async Task<Result<DokumaQuotationDto>> AcceptAsync(int id, AcceptDokumaQuotationRequest request, CancellationToken cancellationToken)
    {
        await ExpireOverdueAsync(cancellationToken);

        DokumaOrderRequest orderRequest;
        int? previousCustomerId;
        await using (var transaction = await db.Database.BeginTransactionAsync(cancellationToken))
        {
            // Kilit aynı teklifin iki kez kabul edilip iki sipariş açılmasını önler.
            if (!await appLock.TryAcquireAsync($"DokumaQuotation:{id}", LockTimeout, cancellationToken))
            {
                return Busy;
            }

            var quotation = await db.Set<DokumaQuotation>().Include(q => q.Lines).SingleOrDefaultAsync(q => q.Id == id, cancellationToken);
            if (quotation is null)
            {
                return NotFound;
            }

            if (quotation.Status != QuotationStatus.Sent)
            {
                return new Error(
                    InvalidState,
                    quotation.Status == QuotationStatus.Expired
                        ? "Teklifin süresi dolmuş; taslağa alıp geçerlilik tarihini güncelleyin ve yeniden gönderin."
                        : "Yalnızca gönderilmiş teklif kabul edilir.",
                    ErrorType.Conflict);
            }

            previousCustomerId = quotation.CustomerId;
            var customerId = quotation.CustomerId ?? request.CustomerId;
            if (customerId is null)
            {
                return ReferenceValidation.InvalidReference("customerId", "Teklif aday firmaya verilmiş; sipariş için müşteri kartı seçin.");
            }

            if (quotation.CustomerId is null)
            {
                if (await ReferenceValidation.RequireSelectableAsync(db.Set<DokumaCustomer>(), customerId, null, "customerId", "Müşteri", cancellationToken) is { } customerError)
                {
                    return customerError;
                }

                quotation.CustomerId = customerId;
            }

            var orderDate = request.OrderDate ?? Today();
            if (request.DueDate is { } due && due < orderDate)
            {
                return ReferenceValidation.InvalidReference("dueDate", "Termin sipariş tarihinden önce olamaz.");
            }

            var notes = $"{quotation.QuotationNo} numaralı tekliften açıldı.{(quotation.Notes is null ? "" : " " + quotation.Notes)}";
            orderRequest = new DokumaOrderRequest(
                customerId,
                orderDate,
                quotation.Lines.OrderBy(l => l.Sequence)
                    .Select(l => new DokumaOrderLineRequest(null, l.ArticleId, l.Meters, l.UnitPrice, l.Notes, l.CustomerArticleCode))
                    .ToList(),
                quotation.Ownership,
                NullIfBlank(request.CustomerOrderNo),
                request.DueDate,
                quotation.Currency,
                notes.Length > 1000 ? notes[..1000] : notes);

            quotation.Status = QuotationStatus.Accepted;
            quotation.DecidedAt = timeProvider.GetUtcNow().UtcDateTime;
            quotation.DecidedBy = currentUser.UserName;
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        // Sipariş kendi işleminde açılır (numara ve doğrulama sipariş servisinde).
        var created = await orders.CreateAsync(orderRequest, cancellationToken);
        var accepted = await db.Set<DokumaQuotation>().SingleAsync(q => q.Id == id, cancellationToken);
        if (!created.IsSuccess)
        {
            accepted.Status = QuotationStatus.Sent;
            accepted.DecidedAt = null;
            accepted.DecidedBy = null;
            accepted.CustomerId = previousCustomerId;
            await db.SaveChangesAsync(cancellationToken);
            return created.Error!;
        }

        accepted.OrderId = created.Value.Id;
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    /// <summary>Geçerlilik tarihi geçmiş gönderilmiş teklifler "süresi doldu" olur.</summary>
    private async Task ExpireOverdueAsync(CancellationToken cancellationToken)
    {
        var today = Today();
        var overdue = await db.Set<DokumaQuotation>()
            .Where(q => q.Status == QuotationStatus.Sent && q.ValidUntil < today)
            .ToListAsync(cancellationToken);
        if (overdue.Count == 0)
        {
            return;
        }

        foreach (var quotation in overdue)
        {
            quotation.Status = QuotationStatus.Expired;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Error?> ValidateReferencesAsync(DokumaQuotationRequest request, DokumaQuotation? existing, CancellationToken cancellationToken)
    {
        if (await ReferenceValidation.RequireSelectableAsync(
                db.Set<DokumaCustomer>(), request.CustomerId, existing?.CustomerId, "customerId", "Müşteri", cancellationToken) is { } customerError)
        {
            return customerError;
        }

        if (await CurrencyRules.RequireActiveAsync(db, request.Currency, existing?.Currency, "currency", cancellationToken) is { } currencyError)
        {
            return currencyError;
        }

        if (await ReferenceValidation.RequireSelectableAsync(
                db.Set<DokumaArticle>(),
                request.Lines.Select(l => l.ArticleId).Distinct().ToList(),
                existing?.Lines.Select(l => l.ArticleId).ToHashSet() ?? [],
                "lines",
                "Artikel",
                cancellationToken) is { } articleError)
        {
            return articleError;
        }

        var loomIds = request.Lines.Where(l => l.LoomId is not null).Select(l => l.LoomId!.Value).Distinct().ToList();
        return loomIds.Count == 0
            ? null
            : await ReferenceValidation.RequireSelectableAsync(
                db.Set<DokumaLoom>(),
                loomIds,
                existing?.Lines.Where(l => l.LoomId is not null).Select(l => l.LoomId!.Value).ToHashSet() ?? [],
                "lines",
                "Tezgâh",
                cancellationToken);
    }

    private static void Apply(DokumaQuotation quotation, DokumaQuotationRequest request, Calculation calc)
    {
        quotation.QuotationDate = request.QuotationDate;
        quotation.ValidUntil = calc.ValidUntil;
        quotation.CustomerId = request.CustomerId;
        quotation.ProspectName = request.CustomerId is null ? NullIfBlank(request.ProspectName) : null;
        quotation.ContactName = NullIfBlank(request.ContactName);
        quotation.Ownership = request.Ownership;
        quotation.Currency = calc.Currency;
        quotation.DocumentLanguage = calc.DocumentLanguage;
        quotation.PaymentTermDays = calc.PaymentTermDays;
        quotation.DeliveryTerms = NullIfBlank(request.DeliveryTerms);
        quotation.Notes = NullIfBlank(request.Notes);
    }

    private sealed record PriceRow(int ItemId, decimal Price, string Currency, DateOnly Date, int Id, string? Reference);

    private sealed record CalculatedLine(DokumaQuotationLine Line, string? LoomCode, IReadOnlyList<string> Warnings);

    /// <param name="CostRate">Maliyet para biriminin 1 biriminin teklif para birimindeki karşılığı.</param>
    private sealed record Calculation(
        string Currency,
        string CostCurrency,
        decimal CostRate,
        DateOnly ValidUntil,
        DocumentLanguage DocumentLanguage,
        int? PaymentTermDays,
        IReadOnlyDictionary<int, DokumaArticle> Articles,
        IReadOnlyDictionary<int, DokumaYarn> Yarns,
        IReadOnlyList<CalculatedLine> Lines);

    private sealed class YarnAccumulator(DokumaYarn yarn)
    {
        public DokumaYarn Yarn { get; } = yarn;
        public bool Warp { get; set; }
        public bool Weft { get; set; }
        public decimal Kilograms { get; set; }
    }

    /// <summary>
    /// Satırların ön maliyetini hesaplar. Satır girdisi boşsa: hız tezgâhtan (tezgâh seçilmediyse artikeli dokuyabilen etkin tezgâhların
    /// ortalaması), verim ve tezgâh saat maliyeti tezgâha özel değerden ya da genel parametreden, sıklık artikelden, iplik fiyatı son
    /// onaylı satın alma siparişinden, haşıl reçeteden ya da parametreden alınır.
    /// </summary>
    private async Task<Result<Calculation>> CalculateCoreAsync(DokumaQuotationRequest request, CancellationToken cancellationToken)
    {
        var customer = request.CustomerId is { } customerId
            ? await db.Set<DokumaCustomer>().AsNoTracking()
                .Where(c => c.Id == customerId)
                .Select(c => new { c.Currency, c.DocumentLanguage, c.PaymentTermDays })
                .SingleOrDefaultAsync(cancellationToken)
            : null;
        var currency = NullIfBlank(request.Currency)?.ToUpperInvariant() ?? customer?.Currency ?? "TRY";
        var settings = await DokumaCostSettingsService.LoadAsync(db, cancellationToken);
        var loomRates = await DokumaCostSettingsService.LoomRatesAsync(db, cancellationToken);
        var date = request.QuotationDate;

        var articleIds = request.Lines.Select(l => l.ArticleId).Distinct().ToList();
        var articles = await db.Set<DokumaArticle>().AsNoTracking()
            .Include(a => a.Yarns).ThenInclude(y => y.Yarn)
            .Where(a => articleIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, cancellationToken);
        if (articleIds.Any(id => !articles.ContainsKey(id)))
        {
            return ReferenceValidation.InvalidReference("lines", "Artikel bulunamadı.");
        }

        var requestedLooms = request.Lines.Where(l => l.LoomId is not null).Select(l => l.LoomId!.Value).Distinct().ToList();
        var looms = await db.Set<DokumaLoom>().AsNoTracking()
            .Where(l => l.IsActive || requestedLooms.Contains(l.Id))
            .ToListAsync(cancellationToken);

        // Son iplik alış fiyatı: onaylanmış (taslak ve iptal hariç) satın alma siparişlerinden, sipariş tarihine göre en yenisi.
        var yarns = articles.Values.SelectMany(a => a.Yarns).Select(y => y.Yarn).DistinctBy(y => y.Id).ToDictionary(y => y.Id);
        var yarnIds = yarns.Keys.ToList();
        var yarnPurchases = await db.Set<DokumaPurchaseOrderLine>().AsNoTracking()
            .Where(l => l.YarnId != null && yarnIds.Contains(l.YarnId.Value) && l.UnitPrice != null
                && l.Order.Status != PurchaseOrderStatus.Draft && l.Order.Status != PurchaseOrderStatus.Cancelled)
            .Select(l => new PriceRow(l.YarnId!.Value, l.UnitPrice!.Value, l.Order.Currency, l.Order.OrderDate, l.Id, l.Order.OrderNo))
            .ToListAsync(cancellationToken);
        var lastYarnPrice = yarnPurchases.GroupBy(p => p.ItemId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(p => p.Date).ThenByDescending(p => p.Id).First());

        // Haşıl reçetesi: artikelin etkin varsayılan reçetesi; kimyasal fiyatı son fiyatlı malzeme lotundan.
        List<DokumaSizingRecipe> recipes = settings.UseSizingRecipe
            ? await db.Set<DokumaSizingRecipe>().AsNoTracking()
                .Include(r => r.Lines).ThenInclude(l => l.Material)
                .Where(r => articleIds.Contains(r.ArticleId) && r.IsDefault && r.IsActive)
                .ToListAsync(cancellationToken)
            : [];
        var recipeByArticle = recipes.GroupBy(r => r.ArticleId).ToDictionary(g => g.Key, g => g.First());
        var materialIds = recipes.SelectMany(r => r.Lines).Select(l => l.MaterialId).Distinct().ToList();
        List<PriceRow> materialLots = materialIds.Count == 0
            ? []
            : await db.Set<DokumaMaterialLot>().AsNoTracking()
                .Where(l => materialIds.Contains(l.MaterialId) && l.UnitPrice != null)
                .Select(l => new PriceRow(l.MaterialId, l.UnitPrice!.Value, l.Currency, l.ReceiptDate, l.Id, l.LotNo))
                .ToListAsync(cancellationToken);
        var lastMaterialPrice = materialLots.GroupBy(p => p.ItemId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(p => p.Date).ThenByDescending(p => p.Id).First());

        // Kurlar: maliyet parametrelerinin, iplik ve kimyasal fiyatlarının para biriminden teklifin para birimine (döviz alış).
        var factors = new Dictionary<string, decimal?>(StringComparer.OrdinalIgnoreCase);
        foreach (var code in lastYarnPrice.Values.Select(p => p.Currency)
                     .Concat(lastMaterialPrice.Values.Select(p => p.Currency))
                     .Append(settings.Currency)
                     .Select(c => c.Trim().ToUpperInvariant())
                     .Distinct())
        {
            factors[code] = await exchangeRates.ConvertAsync(1m, code, currency, date, ExchangeRateType.ForexBuying, cancellationToken);
        }

        if (factors[settings.Currency.Trim()] is not { } costRate)
        {
            return new Error(
                MissingRate,
                $"{settings.Currency} → {currency} kuru bulunamadı; maliyet parametreleri teklifin para birimine çevrilemiyor. Döviz kurlarını girin.",
                ErrorType.BusinessRule);
        }

        decimal? ToQuotationCurrency(decimal amount, string from) => factors.GetValueOrDefault(from.Trim()) is { } factor ? amount * factor : null;

        var calculatedLines = new List<CalculatedLine>();
        for (var index = 0; index < request.Lines.Count; index++)
        {
            var item = request.Lines[index];
            var no = index + 1;
            var article = articles[item.ArticleId];
            var warnings = new List<string>();

            var warp = article.Yarns.Where(y => y.Role == ArticleYarnRole.Warp).ToList();
            var weft = article.Yarns.Where(y => y.Role == ArticleYarnRole.Weft).ToList();
            var weftTotal = weft.Sum(y => y.Ends);
            if (warp.Count == 0 || weft.Count == 0 || weftTotal <= 0)
            {
                return new Error(
                    InvalidState,
                    $"{no}. satır: {article.ArticleCode} artikelinin kartında çözgü ya da atkı ipliği tanımlı değil. Önce artikelin iplik bilgisini tamamlayın.",
                    ErrorType.Conflict);
            }

            var picks = item.PicksPerCm ?? article.WeftDensityPerCm;
            var loom = item.LoomId is { } loomId ? looms.SingleOrDefault(l => l.Id == loomId) : null;
            var rate = loom is null ? null : loomRates.GetValueOrDefault(loom.Id);
            if (loom is not null && !Fits(loom, article))
            {
                warnings.Add($"{loom.Code} tezgâhı bu artikeli dokuyamaz (ağızlık sistemi ya da tarak eni uygun değil).");
            }

            var speed = item.SpeedRpm ?? loom?.NominalSpeedRpm;
            if (speed is null)
            {
                var fitting = looms.Where(l => l.IsActive && l.NominalSpeedRpm > 0 && Fits(l, article)).ToList();
                if (fitting.Count > 0)
                {
                    speed = Math.Round((decimal)fitting.Average(l => l.NominalSpeedRpm!.Value), 0);
                    warnings.Add($"Tezgâh seçilmedi; artikeli dokuyabilen {fitting.Count} tezgâhın ortalama hızı kullanıldı.");
                }
            }

            if (speed is not > 0)
            {
                return ReferenceValidation.InvalidReference("lines", $"{no}. satır: tezgâh hızı bilinmiyor; hızı tanımlı bir tezgâh seçin ya da hızı girin.");
            }

            var efficiency = item.EfficiencyPercent ?? rate?.EfficiencyPercent ?? settings.DefaultEfficiencyPercent;
            if (WeavingRate.HoursPerMeter(speed, picks, efficiency) is not { } hoursPerMeter)
            {
                return ReferenceValidation.InvalidReference("lines", $"{no}. satır: atkı sıklığı ve verim sıfırdan büyük olmalıdır.");
            }

            // İplik: sipariş iplik ihtiyacıyla aynı formül; artikeldeki çözgü / atkı firesi eklenir.
            var accumulators = new Dictionary<int, YarnAccumulator>();
            void Add(DokumaYarn yarn, bool isWarp, decimal kilograms)
            {
                if (!accumulators.TryGetValue(yarn.Id, out var accumulator))
                {
                    accumulator = new YarnAccumulator(yarn);
                    accumulators[yarn.Id] = accumulator;
                }

                accumulator.Warp |= isWarp;
                accumulator.Weft |= !isWarp;
                accumulator.Kilograms += kilograms;
            }

            decimal warpKilograms = 0, weftKilograms = 0, warpNetPerWarpMeter = 0;
            foreach (var yarnLine in warp)
            {
                var tex = YarnRequirement.Tex(yarnLine.Yarn.CountValue, yarnLine.Yarn.CountSystem, yarnLine.Yarn.Plies);
                var kilograms = YarnRequirement.WarpKilogramsPerMeter(yarnLine.Ends, article.WarpCrimpPercent, tex) * (1 + (article.WarpWastePercent ?? 0) / 100m);
                warpNetPerWarpMeter += YarnRequirement.WarpKilogramsPerMeter(yarnLine.Ends, null, tex);
                warpKilograms += kilograms;
                Add(yarnLine.Yarn, true, kilograms);
            }

            foreach (var yarnLine in weft)
            {
                var tex = YarnRequirement.Tex(yarnLine.Yarn.CountValue, yarnLine.Yarn.CountSystem, yarnLine.Yarn.Plies);
                var share = (decimal)yarnLine.Ends / weftTotal;
                var kilograms = YarnRequirement.WeftKilogramsPerMeter(picks, article.ReedWidthCm, article.WeftCrimpPercent, tex, share)
                    * (1 + (article.WeftWastePercent ?? 0) / 100m);
                weftKilograms += kilograms;
                Add(yarnLine.Yarn, false, kilograms);
            }

            var priceOverrides = (item.YarnPrices ?? []).GroupBy(p => p.YarnId).ToDictionary(g => g.Key, g => g.Last().UnitPrice);
            var line = new DokumaQuotationLine
            {
                Sequence = no,
                ArticleId = article.Id,
                Meters = item.Meters,
                CustomerArticleCode = NullIfBlank(item.CustomerArticleCode),
                Notes = NullIfBlank(item.Notes),
                LoomId = loom?.Id,
                SpeedRpm = speed.Value,
                EfficiencyPercent = efficiency,
                PicksPerCm = picks,
                WarpKilogramsPerMeter = Round6(warpKilograms),
                WeftKilogramsPerMeter = Round6(weftKilograms),
            };

            decimal yarnCost = 0;
            foreach (var accumulator in accumulators.Values.OrderBy(a => a.Yarn.YarnCode))
            {
                decimal price;
                string? source;
                if (priceOverrides.TryGetValue(accumulator.Yarn.Id, out var given))
                {
                    price = given;
                    source = "Elle girildi";
                }
                else if (lastYarnPrice.TryGetValue(accumulator.Yarn.Id, out var purchase) && ToQuotationCurrency(purchase.Price, purchase.Currency) is { } converted)
                {
                    price = Round4(converted);
                    var foreign = !string.Equals(purchase.Currency.Trim(), currency, StringComparison.OrdinalIgnoreCase);
                    source = $"Son alış {purchase.Reference} ({purchase.Date:dd.MM.yyyy}){(foreign ? $", {purchase.Price:0.####} {purchase.Currency}" : "")}";
                }
                else
                {
                    price = 0;
                    source = null;
                    warnings.Add(lastYarnPrice.ContainsKey(accumulator.Yarn.Id)
                        ? $"{accumulator.Yarn.YarnCode} ipliğinin alış para biriminden kur bulunamadı; fiyatı girin."
                        : $"{accumulator.Yarn.YarnCode} ipliğinin alış fiyatı yok; fiyatı girin.");
                }

                var kilograms = Round6(accumulator.Kilograms);
                yarnCost += kilograms * price;
                line.Yarns.Add(new DokumaQuotationLineYarn
                {
                    YarnId = accumulator.Yarn.Id,
                    UsedInWarp = accumulator.Warp,
                    UsedInWeft = accumulator.Weft,
                    KilogramsPerMeter = kilograms,
                    UnitPrice = price,
                    PriceSource = source is { Length: > 100 } ? source[..100] : source,
                });
            }

            if (request.Ownership == OrderOwnership.Commission)
            {
                // Fasonda iplik müşterinindir; kilo ve fiyat bilgi olarak gösterilir, maliyete girmez.
                yarnCost = 0;
            }

            // Haşıl: elle girilen, reçeteden hesaplanan ya da parametre (bir metre çözgü için).
            decimal sizingPerWarpMeter;
            if (item.SizingCostPerMeter is { } givenSizing)
            {
                sizingPerWarpMeter = givenSizing;
            }
            else if (settings.UseSizingRecipe && RecipeCost(recipeByArticle.GetValueOrDefault(article.Id), warpNetPerWarpMeter, lastMaterialPrice, ToQuotationCurrency, warnings) is { } recipeCost)
            {
                sizingPerWarpMeter = recipeCost;
            }
            else
            {
                sizingPerWarpMeter = settings.SizingCostPerMeter * costRate;
            }

            var crimpFactor = 1 + (article.WarpCrimpPercent ?? 0) / 100m;
            var warpingPerWarpMeter = item.WarpingCostPerMeter ?? settings.WarpingCostPerMeter * costRate;
            var loomHourly = item.LoomHourlyCost ?? (rate?.HourlyCost ?? settings.LoomHourlyCost) * costRate;
            var laborHourly = item.LaborHourlyCost ?? settings.LaborHourlyCost * costRate;

            line.YarnCost = Round4(yarnCost);
            line.SizingCostPerWarpMeter = Round4(sizingPerWarpMeter);
            line.WarpingCostPerWarpMeter = Round4(warpingPerWarpMeter);
            line.SizingCost = Round4(line.SizingCostPerWarpMeter * crimpFactor);
            line.WarpingCost = Round4(line.WarpingCostPerWarpMeter * crimpFactor);
            line.LoomHourlyCost = Round4(loomHourly);
            line.LaborHourlyCost = Round4(laborHourly);
            line.WeavingHoursPerMeter = Round6(hoursPerMeter);
            line.WeavingCost = Round4(line.WeavingHoursPerMeter * (line.LoomHourlyCost + line.LaborHourlyCost));
            line.WastePercent = item.WastePercent ?? settings.DefaultWastePercent;
            line.OverheadPercent = item.OverheadPercent ?? settings.OverheadPercent;
            line.MarginPercent = item.MarginPercent ?? settings.DefaultMarginPercent;

            var breakdown = Breakdown(line);
            line.CostPerMeter = Round4(breakdown.Cost);
            line.CalculatedPrice = Round4(line.CostPerMeter * (1 + line.MarginPercent / 100m));
            line.UnitPrice = item.UnitPrice ?? Math.Round(line.CalculatedPrice, 2, MidpointRounding.AwayFromZero);

            calculatedLines.Add(new CalculatedLine(line, loom?.Code, warnings));
        }

        return new Calculation(
            currency,
            settings.Currency,
            Round6(costRate),
            request.ValidUntil ?? request.QuotationDate.AddDays(settings.QuotationValidityDays),
            request.DocumentLanguage ?? customer?.DocumentLanguage ?? DocumentLanguage.Turkish,
            request.PaymentTermDays ?? customer?.PaymentTermDays,
            articles,
            yarns,
            calculatedLines);
    }

    /// <summary>
    /// Reçeteden bir metre çözgünün haşıl kimyasalı maliyeti: çözgü ipliği (kg/m) × hedef alım × banyo oranı × kimyasalın son lot fiyatı.
    /// Reçete, hedef alım ya da bir kimyasalın kilogram fiyatı yoksa boş döner (parametredeki sabit maliyet kullanılır).
    /// </summary>
    private static decimal? RecipeCost(
        DokumaSizingRecipe? recipe,
        decimal warpKilogramsPerWarpMeter,
        IReadOnlyDictionary<int, PriceRow> prices,
        Func<decimal, string, decimal?> convert,
        List<string> warnings)
    {
        if (recipe is null || recipe.TargetPickUpPercent is not { } pickUp || recipe.Lines.Count == 0)
        {
            warnings.Add("Artikelin hedef alımı girilmiş varsayılan haşıl reçetesi yok; parametredeki haşıl maliyeti kullanıldı.");
            return null;
        }

        decimal total = 0;
        foreach (var chemical in recipe.Lines)
        {
            if (chemical.Material.Unit != MaterialUnit.Kilogram
                || !prices.TryGetValue(chemical.MaterialId, out var lot)
                || convert(lot.Price, lot.Currency) is not { } price)
            {
                warnings.Add($"{recipe.Code} reçetesindeki {chemical.Material.Code} için kilogram fiyatı bulunamadı; parametredeki haşıl maliyeti kullanıldı.");
                return null;
            }

            total += warpKilogramsPerWarpMeter * pickUp / 100m * chemical.BathPercent / 100m * price;
        }

        return total;
    }

    /// <summary>Artikel tezgâhta dokunabilir mi: ağızlık sistemi ve tarak eni (levent bağlamadaki kuralla aynı).</summary>
    private static bool Fits(DokumaLoom loom, DokumaArticle article) =>
        DokumaWeavingService.CanWeave(loom.SheddingType, article.SheddingType)
        && (loom.MaxReedWidthCm is not { } max || article.ReedWidthCm <= max);

    private readonly record struct CostBreakdown(decimal Direct, decimal Waste, decimal Overhead, decimal Cost);

    private static CostBreakdown Breakdown(DokumaQuotationLine line)
    {
        var direct = line.YarnCost + line.SizingCost + line.WarpingCost + line.WeavingCost;
        var waste = direct * line.WastePercent / 100m;
        var overhead = (direct + waste) * line.OverheadPercent / 100m;
        return new CostBreakdown(direct, waste, overhead, direct + waste + overhead);
    }

    private static DokumaQuotationLineDto ToLineDto(
        DokumaQuotationLine line,
        DokumaArticle article,
        string? loomCode,
        IReadOnlyDictionary<int, DokumaYarn> yarns,
        OrderOwnership ownership,
        IReadOnlyList<string> warnings)
    {
        var breakdown = Breakdown(line);
        var amount = Math.Round(line.Meters * line.UnitPrice, 2);
        return new DokumaQuotationLineDto(
            line.Id == 0 ? null : line.Id,
            line.Sequence,
            line.ArticleId,
            article.ArticleCode,
            article.Name,
            article.RawWidthCm,
            article.RawWeightGsm,
            article.ReedWidthCm,
            article.WarpCrimpPercent,
            line.Meters,
            line.CustomerArticleCode,
            line.LoomId,
            loomCode,
            line.SpeedRpm,
            line.EfficiencyPercent,
            line.PicksPerCm,
            line.WeavingHoursPerMeter > 0 ? Math.Round(1m / line.WeavingHoursPerMeter, 2) : 0,
            line.WarpKilogramsPerMeter,
            line.WeftKilogramsPerMeter,
            line.YarnCost,
            line.SizingCostPerWarpMeter,
            line.WarpingCostPerWarpMeter,
            line.SizingCost,
            line.WarpingCost,
            line.LoomHourlyCost,
            line.LaborHourlyCost,
            line.WeavingHoursPerMeter,
            line.WeavingCost,
            Round4(breakdown.Direct),
            line.WastePercent,
            Round4(breakdown.Waste),
            line.OverheadPercent,
            Round4(breakdown.Overhead),
            line.CostPerMeter,
            line.MarginPercent,
            line.CalculatedPrice,
            line.UnitPrice,
            amount,
            line.CostPerMeter > 0 ? Math.Round((line.UnitPrice - line.CostPerMeter) / line.CostPerMeter * 100m, 2) : null,
            line.Notes,
            line.Yarns.OrderBy(y => yarns[y.YarnId].YarnCode).Select(y =>
            {
                var yarn = yarns[y.YarnId];
                return new DokumaQuotationLineYarnDto(
                    y.YarnId,
                    yarn.YarnCode,
                    yarn.Name,
                    yarn.Color,
                    y.UsedInWarp,
                    y.UsedInWeft,
                    y.KilogramsPerMeter,
                    y.UnitPrice,
                    ownership == OrderOwnership.Commission ? 0 : Round4(y.KilogramsPerMeter * y.UnitPrice),
                    y.PriceSource);
            }).ToList(),
            warnings);
    }

    private static decimal Round4(decimal value) => Math.Round(value, 4, MidpointRounding.AwayFromZero);

    private static decimal Round6(decimal value) => Math.Round(value, 6, MidpointRounding.AwayFromZero);
}
