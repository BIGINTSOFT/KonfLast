using System.Globalization;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.Finance;
using ERP.Core.Application.Features.MasterData;
using ERP.Core.Domain.Entities;
using ERP.Core.Domain.Enums;
using ERP.Orme.Application.Features.Materials;
using ERP.Orme.Application.Features.Orders;
using ERP.Orme.Domain.Entities;
using ERP.Orme.Domain.Enums;
using ERP.Orme.Domain.Rules;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Orme.Application.Features.Quotations;

public sealed class OrmeQuotationQuery : PageRequest
{
    /// <summary>Teklif no, müşteri ya da aday firma adında aranır.</summary>
    public string? Search { get; set; }
}

/// <param name="CustomerName">Müşteri kartı varsa adı; yoksa aday firma adı.</param>
/// <param name="IsProspect">Müşteri kartı olmayan aday firma.</param>
public sealed record OrmeQuotationListItemDto(
    int Id,
    string QuotationNo,
    DateOnly QuotationDate,
    DateOnly ValidUntil,
    string? CustomerName,
    bool IsProspect,
    int LineCount,
    decimal TotalKilograms,
    decimal TotalAmount,
    string Currency,
    QuotationStatus Status);

/// <param name="Percent">Kumaş ağırlığındaki oran (%).</param>
/// <param name="UnitPrice">Kilo fiyatı (teklifin para biriminde); bulunamadıysa boş.</param>
/// <param name="CostPerKilogram">Bu ipliğin kumaş kilosuna düşen fireli maliyeti.</param>
public sealed record OrmeQuotationLineYarnDto(
    int YarnId,
    string YarnCode,
    string YarnName,
    string? YarnColor,
    decimal Percent,
    decimal? UnitPrice,
    QuotationPriceSource PriceSource,
    string? PriceReference,
    decimal CostPerKilogram);

/// <summary>Teklif satırı ve kilo başına maliyet kırılımı.</summary>
/// <param name="YarnCostPerKilogram">Fireli iplik maliyeti; fasonda sıfır.</param>
/// <param name="KnittingCostPerKilogram">(Makine + işçilik) / kg/saat.</param>
/// <param name="OverheadPerKilogram">Doğrudan maliyet × genel gider.</param>
/// <param name="CostPerKilogram">Toplam maliyet.</param>
/// <param name="CalculatedPrice">Maliyet × (1 + kâr); fiyat elle değiştirilmediyse birim fiyata eşittir.</param>
/// <param name="CostAmount">Kilo × toplam maliyet.</param>
/// <param name="HasMissingCosts">İplik fiyatı ya da saatte örülen kilo eksik; maliyet eksik hesaplandı.</param>
/// <param name="OrderId">Teklif kabul edildiğinde açılan sipariş.</param>
public sealed record OrmeQuotationLineDto(
    int Id,
    int Sequence,
    int FabricId,
    string FabricCode,
    string FabricName,
    string? CustomerFabricCode,
    decimal Kilograms,
    bool IsCommission,
    int? MachineId,
    string? MachineCode,
    string? MachineName,
    decimal WastePercent,
    decimal? KilogramsPerHour,
    decimal MachineHourlyCost,
    decimal LaborHourlyCost,
    decimal OverheadPercent,
    decimal MarginPercent,
    decimal YarnCostPerKilogram,
    decimal KnittingCostPerKilogram,
    decimal OverheadPerKilogram,
    decimal CostPerKilogram,
    decimal CalculatedPrice,
    decimal UnitPrice,
    decimal Amount,
    decimal CostAmount,
    bool HasMissingCosts,
    string? Notes,
    int? OrderId,
    string? OrderNo,
    IReadOnlyList<OrmeQuotationLineYarnDto> Yarns);

/// <summary>Kabul edilen tekliften açılan sipariş.</summary>
public sealed record OrmeQuotationOrderDto(int Id, string OrderNo, OrderOwnership Ownership, OrderStatus Status);

/// <param name="DocumentLanguage">Teklifte seçilen belge dili; boşsa müşterinin dili.</param>
/// <param name="PrintLanguage">Basımda kullanılan dil.</param>
/// <param name="TotalCost">Satırların kilo × maliyet toplamı.</param>
/// <param name="MarginPercent">Toplam kâr / toplam maliyet; maliyet sıfırsa boş.</param>
public sealed record OrmeQuotationDto(
    int Id,
    string QuotationNo,
    DateOnly QuotationDate,
    DateOnly ValidUntil,
    int? CustomerId,
    string? CustomerName,
    string? ProspectName,
    string Currency,
    DocumentLanguage? DocumentLanguage,
    DocumentLanguage PrintLanguage,
    QuotationStatus Status,
    string? Notes,
    decimal TotalKilograms,
    decimal TotalAmount,
    decimal TotalCost,
    decimal ProfitAmount,
    decimal? MarginPercent,
    bool HasMissingCosts,
    DateTime CreatedAt,
    string? CreatedBy,
    IReadOnlyList<OrmeQuotationOrderDto> Orders,
    IReadOnlyList<OrmeQuotationLineDto> Lines);

/// <param name="CustomerId">Müşteri kartı; boşsa <paramref name="ProspectName"/> zorunludur.</param>
/// <param name="ProspectName">Kartı olmayan aday firma; müşteri seçilirse yok sayılır.</param>
/// <param name="Currency">Boşsa müşterinin para birimi, müşteri yoksa TRY. Satır varken değiştirilemez.</param>
/// <param name="DocumentLanguage">Boşsa müşterinin belge dili.</param>
public sealed record OrmeQuotationRequest(
    int? CustomerId,
    string? ProspectName,
    DateOnly QuotationDate,
    DateOnly ValidUntil,
    string? Currency = null,
    DocumentLanguage? DocumentLanguage = null,
    string? Notes = null);

public sealed class OrmeQuotationRequestValidator : AbstractValidator<OrmeQuotationRequest>
{
    public OrmeQuotationRequestValidator()
    {
        RuleFor(x => x.ProspectName)
            .NotEmpty().WithMessage("Müşteri seçin ya da aday firma adını yazın.")
            .When(x => x.CustomerId is null);
        RuleFor(x => x.ProspectName).OptionalText(200);
        RuleFor(x => x.QuotationDate).NotEmpty().WithMessage("Teklif tarihi zorunludur.");
        RuleFor(x => x.ValidUntil)
            .Must((request, until) => until >= request.QuotationDate).WithMessage("Geçerlilik tarihi teklif tarihinden önce olamaz.");
        RuleFor(x => x.Currency)
            .Matches(@"^\s*[A-Za-z]{3}\s*$").WithMessage("Para birimi üç harfli ISO kodu olmalıdır (örn. TRY, EUR).")
            .When(x => !string.IsNullOrWhiteSpace(x.Currency));
        RuleFor(x => x.DocumentLanguage).IsInEnum().WithMessage("Geçerli bir belge dili seçin.").When(x => x.DocumentLanguage is not null);
        RuleFor(x => x.Notes).OptionalText(1000);
    }
}

/// <param name="UnitPrice">Kilo fiyatı (teklifin para biriminde); boşsa iplik maliyete sıfır katılır.</param>
/// <param name="PriceSource">Fiyatın kaynağı; ekranda değiştirilen fiyat elle girilmiş sayılır.</param>
/// <param name="PriceReference">Kaynak belge (sipariş / lot no ve orijinal fiyat).</param>
public sealed record OrmeQuotationYarnPriceRequest(int YarnId, decimal? UnitPrice, QuotationPriceSource PriceSource = QuotationPriceSource.Manual, string? PriceReference = null);

/// <summary>Teklif satırı: maliyet girdileri anlık görüntü olarak saklanır.</summary>
/// <param name="Yarns">Kumaş reçetesindeki ipliklerin fiyatları; oranlar sunucuda kumaş kartından alınır. Fasonda yok sayılır.</param>
/// <param name="KilogramsPerHour">Saatte örülen kilo; boşsa örme maliyeti hesaplanmaz.</param>
/// <param name="UnitPrice">Elle girilen kilo fiyatı; boşsa maliyet × (1 + kâr). Girilirse kâr fiyattan yeniden hesaplanır.</param>
public sealed record OrmeQuotationLineRequest(
    int FabricId,
    decimal Kilograms,
    bool IsCommission,
    decimal WastePercent,
    decimal? KilogramsPerHour,
    decimal MachineHourlyCost,
    decimal LaborHourlyCost,
    decimal OverheadPercent,
    decimal MarginPercent,
    IReadOnlyList<OrmeQuotationYarnPriceRequest> Yarns,
    int? MachineId = null,
    decimal? UnitPrice = null,
    string? CustomerFabricCode = null,
    string? Notes = null);

public sealed class OrmeQuotationLineRequestValidator : AbstractValidator<OrmeQuotationLineRequest>
{
    public OrmeQuotationLineRequestValidator()
    {
        RuleFor(x => x.FabricId).GreaterThan(0).WithMessage("Kumaş seçilmelidir.");
        RuleFor(x => x.Kilograms)
            .GreaterThan(0).WithMessage("Kilo sıfırdan büyük olmalıdır.")
            .LessThanOrEqualTo(9_999_999).WithMessage("Kilo çok büyük.")
            .PrecisionScale(12, 2, true).WithMessage("Kilo en fazla iki ondalık olabilir.");
        RuleFor(x => x.MachineId).GreaterThan(0).WithMessage("Geçerli bir makine seçin.").When(x => x.MachineId is not null);
        RuleFor(x => x.WastePercent)
            .InclusiveBetween(0, 100).WithMessage("Fire 0 ile 100 arasında olmalıdır.")
            .PrecisionScale(5, 2, true).WithMessage("Fire en fazla iki ondalık olabilir.");
        RuleFor(x => x.KilogramsPerHour)
            .GreaterThan(0).WithMessage("Saatte örülen kilo sıfırdan büyük olmalıdır.")
            .LessThanOrEqualTo(100_000).WithMessage("Saatte örülen kilo çok büyük.")
            .PrecisionScale(8, 2, true).WithMessage("Saatte örülen kilo en fazla iki ondalık olabilir.")
            .When(x => x.KilogramsPerHour is not null);
        RuleFor(x => x.MachineHourlyCost)
            .InclusiveBetween(0, 99_999_999).WithMessage("Makine saat maliyeti sıfır ya da büyük olmalıdır.")
            .PrecisionScale(18, 4, true).WithMessage("Makine saat maliyeti en fazla dört ondalık olabilir.");
        RuleFor(x => x.LaborHourlyCost)
            .InclusiveBetween(0, 99_999_999).WithMessage("İşçilik saat maliyeti sıfır ya da büyük olmalıdır.")
            .PrecisionScale(18, 4, true).WithMessage("İşçilik saat maliyeti en fazla dört ondalık olabilir.");
        RuleFor(x => x.OverheadPercent)
            .InclusiveBetween(0, 1000).WithMessage("Genel gider 0 ile 1000 arasında olmalıdır.")
            .PrecisionScale(7, 2, true).WithMessage("Genel gider en fazla iki ondalık olabilir.");
        RuleFor(x => x.MarginPercent)
            .InclusiveBetween(-100, 10_000).WithMessage("Kâr -100 ile 10.000 arasında olmalıdır.")
            .PrecisionScale(9, 2, true).WithMessage("Kâr en fazla iki ondalık olabilir.");
        RuleFor(x => x.UnitPrice)
            .InclusiveBetween(0, 99_999_999).WithMessage("Birim fiyat geçersiz.")
            .PrecisionScale(18, 4, true).WithMessage("Birim fiyat en fazla dört ondalık olabilir.")
            .When(x => x.UnitPrice is not null);
        RuleFor(x => x.CustomerFabricCode).OptionalText(50);
        RuleFor(x => x.Notes).OptionalText(500);
        RuleFor(x => x.Yarns)
            .NotNull().WithMessage("İplik fiyat listesi zorunludur.")
            .Must(list => list is null || list.Count <= SaveOrmeFabricYarnsRequestValidator.MaxLines).WithMessage("Çok fazla iplik satırı gönderildi.")
            .Must(list => list is null || list.Select(y => y.YarnId).Distinct().Count() == list.Count).WithMessage("Aynı iplik iki kez girilemez.");
        RuleForEach(x => x.Yarns).ChildRules(yarn =>
        {
            yarn.RuleFor(y => y.YarnId).GreaterThan(0).WithMessage("İplik seçilmelidir.");
            yarn.RuleFor(y => y.UnitPrice)
                .InclusiveBetween(0, 99_999_999).WithMessage("İplik fiyatı geçersiz.")
                .PrecisionScale(18, 4, true).WithMessage("İplik fiyatı en fazla dört ondalık olabilir.")
                .When(y => y.UnitPrice is not null);
            yarn.RuleFor(y => y.PriceSource).IsInEnum().WithMessage("Geçerli bir fiyat kaynağı seçin.");
            yarn.RuleFor(y => y.PriceReference).OptionalText(100);
        });
    }
}

/// <param name="CustomerId">Teklif aday firmaya verildiyse siparişin müşterisi; teklif bu müşteriye bağlanır.</param>
/// <param name="OrderDate">Boşsa bugün.</param>
public sealed record OrmeQuotationAcceptRequest(int? CustomerId = null, DateOnly? OrderDate = null, DateOnly? DueDate = null, string? CustomerOrderNo = null);

public sealed class OrmeQuotationAcceptRequestValidator : AbstractValidator<OrmeQuotationAcceptRequest>
{
    public OrmeQuotationAcceptRequestValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0).WithMessage("Geçerli bir müşteri seçin.").When(x => x.CustomerId is not null);
        RuleFor(x => x.CustomerOrderNo).OptionalText(50);
        RuleFor(x => x.DueDate)
            .Must((request, due) => request.OrderDate is null || due >= request.OrderDate).WithMessage("Termin sipariş tarihinden önce olamaz.")
            .When(x => x.DueDate is not null);
    }
}

public sealed class OrmeQuotationCostDefaultsQuery
{
    public int FabricId { get; set; }

    /// <summary>Saat maliyeti için makine; boşsa genel makine saat maliyeti.</summary>
    public int? MachineId { get; set; }

    /// <summary>Teklifin para birimi; boşsa TRY.</summary>
    public string? Currency { get; set; }

    /// <summary>Kur tarihi (teklif tarihi); boşsa bugün.</summary>
    public DateOnly? Date { get; set; }
}

/// <param name="UnitPrice">Teklifin para birimine çevrilmiş kilo fiyatı; fiyat ya da kur bulunamadıysa boş.</param>
/// <param name="OriginalPrice">Kaynak belgedeki fiyat, kendi para biriminde.</param>
public sealed record OrmeQuotationYarnCostDto(
    int YarnId,
    string YarnCode,
    string YarnName,
    string? YarnColor,
    decimal Percent,
    decimal? UnitPrice,
    QuotationPriceSource PriceSource,
    string? PriceReference,
    DateOnly? PriceDate,
    decimal? OriginalPrice,
    string? OriginalCurrency);

/// <summary>Teklif satırının varsayılan maliyet girdileri: kumaş kartı, parametreler ve son alış fiyatları.</summary>
/// <param name="HasRecipe">Kumaş kartında iplik satırı var; yoksa yalnız fason teklif verilebilir.</param>
/// <param name="WastePercent">Kumaş kartındaki fire, yoksa parametredeki varsayılan.</param>
/// <param name="KilogramsPerHour">Kumaş kartındaki beklenen üretim; boşsa elle girilmelidir.</param>
/// <param name="MissingRate">Kur bulunamadığı için bir ya da daha fazla tutar çevrilemedi.</param>
public sealed record OrmeQuotationCostDefaultsDto(
    int FabricId,
    string FabricCode,
    string FabricName,
    bool HasRecipe,
    decimal WastePercent,
    decimal? KilogramsPerHour,
    int? MachineId,
    decimal MachineHourlyCost,
    decimal LaborHourlyCost,
    decimal OverheadPercent,
    decimal MarginPercent,
    string Currency,
    bool MissingRate,
    IReadOnlyList<OrmeQuotationYarnCostDto> Yarns);

/// <param name="IsCommission">İplik müşteriden; fiyat yalnızca örme ücretidir.</param>
public sealed record OrmeQuotationPrintLineDto(
    int Id,
    string FabricCode,
    string FabricName,
    string? KnitName,
    string? Composition,
    int RawWeightGsm,
    decimal? RawWidthCm,
    string? CustomerFabricCode,
    decimal Kilograms,
    bool IsCommission,
    decimal UnitPrice,
    decimal Amount,
    string? Notes);

/// <summary>Müşteriye basılan teklif belgesi; maliyet bilgisi içermez.</summary>
/// <param name="PartyName">Müşteri adı ya da aday firma adı.</param>
/// <param name="VatRate">Müşterinin varsayılan KDV oranı; aday firmada ve tanımsızsa boş (KDV sıfır).</param>
/// <param name="SecondQualityDiscountPercent">Müşteri kartındaki 2. kalite indirimi; belgede not olarak basılır.</param>
public sealed record OrmeQuotationPrintDto(
    int Id,
    string QuotationNo,
    QuotationStatus Status,
    DateOnly QuotationDate,
    DateOnly ValidUntil,
    string Currency,
    string? Notes,
    string? CustomerCode,
    string PartyName,
    string? CustomerTaxNumber,
    string? CustomerTaxOffice,
    string? CustomerAddress,
    DocumentLanguage DocumentLanguage,
    int? PaymentTermDays,
    decimal? VatRate,
    decimal? SecondQualityDiscountPercent,
    decimal TotalKilograms,
    decimal Subtotal,
    decimal VatAmount,
    decimal Total,
    bool HasCommissionLines,
    IReadOnlyList<OrmeQuotationPrintLineDto> Lines);

/// <summary>İpliğin teklifte kullanılacak son alış fiyatı.</summary>
public sealed record OrmeYarnPriceInfo(decimal? UnitPrice, QuotationPriceSource Source, string? Reference, DateOnly? Date, decimal? OriginalPrice, string? OriginalCurrency);

/// <summary>
/// İpliklerin son alış fiyatı: son satın alma siparişi satırı ya da son teslim alınan lotun bağlı olduğu satın alma satırı;
/// tarihi en yeni olan kullanılır (aynı günde sipariş önceliklidir). Fiyat teklifin para birimine kurla çevrilir.
/// Lot kartında fiyat tutulmaz; lot fiyatı teslim alındığı satın alma satırından gelir.
/// </summary>
public static class OrmeYarnPrices
{
    /// <summary>Çevrimde kullanılan kur: TCMB döviz alış.</summary>
    public const ExchangeRateType ConversionRate = ExchangeRateType.ForexBuying;

    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    public static async Task<Dictionary<int, OrmeYarnPriceInfo>> LastAsync(
        IAppDbContext db, IExchangeRateService exchangeRates, IReadOnlyCollection<int> yarnIds, string currency, DateOnly date, CancellationToken cancellationToken)
    {
        if (yarnIds.Count == 0)
        {
            return [];
        }

        var orders = await db.Set<OrmePurchaseOrderLine>().AsNoTracking()
            .Where(l => yarnIds.Contains(l.YarnId) && l.UnitPrice != null && l.Order.Status != PurchaseOrderStatus.Cancelled)
            .Select(l => new Candidate(l.YarnId, l.Order.OrderDate, 0, l.Id, l.UnitPrice!.Value, l.Order.Currency, l.Order.OrderNo, QuotationPriceSource.PurchaseOrder))
            .ToListAsync(cancellationToken);
        var lots = await db.Set<OrmeYarnLot>().AsNoTracking()
            .Where(l => yarnIds.Contains(l.YarnId) && l.CustomerId == null && l.PurchaseOrderLineId != null && l.PurchaseOrderLine!.UnitPrice != null)
            .Select(l => new Candidate(l.YarnId, l.ReceiptDate, 1, l.Id, l.PurchaseOrderLine!.UnitPrice!.Value, l.PurchaseOrderLine.Order.Currency, l.LotNo, QuotationPriceSource.YarnLot))
            .ToListAsync(cancellationToken);

        var latest = orders.Concat(lots)
            .GroupBy(c => c.YarnId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(c => c.Date).ThenBy(c => c.Rank).ThenByDescending(c => c.Id).First());

        var rates = new Dictionary<string, decimal?>();
        var result = new Dictionary<int, OrmeYarnPriceInfo>();
        foreach (var (yarnId, candidate) in latest)
        {
            if (!rates.TryGetValue(candidate.Currency, out var rate))
            {
                rate = await exchangeRates.ConvertAsync(1m, candidate.Currency, currency, date, ConversionRate, cancellationToken);
                rates[candidate.Currency] = rate;
            }

            var original = $"{candidate.Price.ToString("0.####", Turkish)} {candidate.Currency}";
            result[yarnId] = rate is { } value
                ? new OrmeYarnPriceInfo(Math.Round(candidate.Price * value, 4, MidpointRounding.AwayFromZero), candidate.Source, $"{candidate.Reference} ({original})", candidate.Date, candidate.Price, candidate.Currency)
                : new OrmeYarnPriceInfo(null, QuotationPriceSource.None, $"{candidate.Reference} ({original}, kur bulunamadı)", candidate.Date, candidate.Price, candidate.Currency);
        }

        return result;
    }

    private sealed record Candidate(int YarnId, DateOnly Date, int Rank, int Id, decimal Price, string Currency, string Reference, QuotationPriceSource Source);
}

public interface IOrmeQuotationService
{
    Task<PagedResult<OrmeQuotationListItemDto>> GetListAsync(OrmeQuotationQuery query, CancellationToken cancellationToken);
    Task<Result<OrmeQuotationDto>> GetAsync(int id, CancellationToken cancellationToken);
    Task<Result<OrmeQuotationDto>> CreateAsync(OrmeQuotationRequest request, CancellationToken cancellationToken);
    Task<Result<OrmeQuotationDto>> UpdateAsync(int id, OrmeQuotationRequest request, CancellationToken cancellationToken);
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken);
    Task<Result<OrmeQuotationDto>> AddLineAsync(int id, OrmeQuotationLineRequest request, CancellationToken cancellationToken);
    Task<Result<OrmeQuotationDto>> UpdateLineAsync(int id, int lineId, OrmeQuotationLineRequest request, CancellationToken cancellationToken);
    Task<Result<OrmeQuotationDto>> DeleteLineAsync(int id, int lineId, CancellationToken cancellationToken);
    Task<Result<OrmeQuotationDto>> SendAsync(int id, CancellationToken cancellationToken);
    Task<Result<OrmeQuotationDto>> ReopenAsync(int id, CancellationToken cancellationToken);
    Task<Result<OrmeQuotationDto>> RejectAsync(int id, CancellationToken cancellationToken);
    Task<Result<OrmeQuotationDto>> AcceptAsync(int id, OrmeQuotationAcceptRequest request, CancellationToken cancellationToken);
    Task<Result<OrmeQuotationPrintDto>> GetPrintAsync(int id, CancellationToken cancellationToken);
    Task<Result<OrmeQuotationCostDefaultsDto>> GetCostDefaultsAsync(OrmeQuotationCostDefaultsQuery query, CancellationToken cancellationToken);
}

/// <summary>
/// Satış teklifi ve ön maliyet. Taslak teklifin başlığı ve satırları düzenlenir; gönderilen teklif kabul ya da ret edilir,
/// geçerlilik tarihi geçince süresi dolmuş sayılır. Kabulde satırlardan taslak sipariş açılır: fason ve kendi malı satırlar
/// ayrı siparişe düşer (sipariş iş türü başlıktadır).
/// </summary>
public sealed class OrmeQuotationService(IAppDbContext db, IDocumentNumberGenerator numbers, IExchangeRateService exchangeRates, IAppLock appLock, TimeProvider timeProvider)
    : IOrmeQuotationService
{
    private const string Series = "OrmeQuotation";
    private const string InvalidState = "ORME_QUOTATION_INVALID_STATE";
    private const int MaxLines = 50;

    private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(10);
    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Teklif bulunamadı.", ErrorType.NotFound);
    private static readonly Error Busy = new(ErrorCodes.ResourceBusy, "Teklif üzerinde başka bir işlem sürüyor, biraz sonra yeniden deneyin.", ErrorType.Conflict);
    private static readonly Error LineNotFound = new(ErrorCodes.NotFound, "Teklif satırı bulunamadı.", ErrorType.NotFound);
    private static readonly Error NotDraft = new(InvalidState, "Yalnızca taslak teklif düzenlenebilir; gönderilmiş teklifi önce taslağa alın.", ErrorType.Conflict);

    private DateOnly Today => DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);

    /// <summary>Geçerlilik tarihi geçmiş gönderilmiş teklifleri "süresi doldu" yapar.</summary>
    private Task ExpireAsync(CancellationToken cancellationToken)
    {
        var today = Today;
        return db.Set<OrmeQuotation>()
            .Where(q => q.Status == QuotationStatus.Sent && q.ValidUntil < today)
            .ExecuteUpdateAsync(s => s.SetProperty(q => q.Status, QuotationStatus.Expired), cancellationToken);
    }

    public async Task<PagedResult<OrmeQuotationListItemDto>> GetListAsync(OrmeQuotationQuery query, CancellationToken cancellationToken)
    {
        await ExpireAsync(cancellationToken);

        var quotations = db.Set<OrmeQuotation>().AsNoTracking();
        if (NullIfBlank(query.Search) is { } term)
        {
            quotations = quotations.Where(q => q.QuotationNo.Contains(term)
                || (q.ProspectName != null && q.ProspectName.Contains(term))
                || (q.Customer != null && q.Customer.Name.Contains(term)));
        }

        return await quotations
            .OrderByDescending(q => q.QuotationDate).ThenByDescending(q => q.Id)
            .Select(q => new OrmeQuotationListItemDto(
                q.Id,
                q.QuotationNo,
                q.QuotationDate,
                q.ValidUntil,
                q.Customer != null ? q.Customer.Name : q.ProspectName,
                q.CustomerId == null,
                q.Lines.Count,
                q.Lines.Sum(l => (decimal?)l.Kilograms) ?? 0,
                q.Lines.Sum(l => (decimal?)(l.Kilograms * l.UnitPrice)) ?? 0,
                q.Currency,
                q.Status))
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<Result<OrmeQuotationDto>> GetAsync(int id, CancellationToken cancellationToken)
    {
        await ExpireAsync(cancellationToken);
        return await LoadAsync(id, cancellationToken);
    }

    public async Task<Result<OrmeQuotationDto>> CreateAsync(OrmeQuotationRequest request, CancellationToken cancellationToken)
    {
        if (await ValidateHeaderAsync(request, null, cancellationToken) is { } error)
        {
            return error;
        }

        var customerCurrency = await CustomerCurrencyAsync(request.CustomerId, cancellationToken);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var year = request.QuotationDate.Year;
        var sequence = await numbers.NextAsync(Series, year, cancellationToken);
        var quotation = new OrmeQuotation { QuotationNo = $"OTK-{year}-{sequence:D5}" };
        ApplyHeader(quotation, request, customerCurrency);
        db.Set<OrmeQuotation>().Add(quotation);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await LoadAsync(quotation.Id, cancellationToken);
    }

    public async Task<Result<OrmeQuotationDto>> UpdateAsync(int id, OrmeQuotationRequest request, CancellationToken cancellationToken)
    {
        var quotation = await db.Set<OrmeQuotation>().SingleOrDefaultAsync(q => q.Id == id, cancellationToken);
        if (quotation is null)
        {
            return NotFound;
        }

        if (quotation.Status != QuotationStatus.Draft)
        {
            return NotDraft;
        }

        if (await ValidateHeaderAsync(request, quotation, cancellationToken) is { } error)
        {
            return error;
        }

        var currency = NullIfBlank(request.Currency)?.ToUpperInvariant() ?? await CustomerCurrencyAsync(request.CustomerId, cancellationToken);
        if (currency != quotation.Currency && await db.Set<OrmeQuotationLine>().AnyAsync(l => l.QuotationId == id, cancellationToken))
        {
            return ReferenceValidation.InvalidReference(
                "currency", "Satırların maliyetleri teklifin para birimindedir; satır varken para birimi değiştirilemez. Önce satırları silin.");
        }

        ApplyHeader(quotation, request, currency);
        await db.SaveChangesAsync(cancellationToken);
        return await LoadAsync(id, cancellationToken);
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var quotation = await db.Set<OrmeQuotation>()
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

        db.Set<OrmeQuotationLineYarn>().RemoveRange(quotation.Lines.SelectMany(l => l.Yarns));
        db.Set<OrmeQuotationLine>().RemoveRange(quotation.Lines);
        db.Set<OrmeQuotation>().Remove(quotation);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<OrmeQuotationDto>> AddLineAsync(int id, OrmeQuotationLineRequest request, CancellationToken cancellationToken)
    {
        var quotation = await db.Set<OrmeQuotation>().Include(q => q.Lines).SingleOrDefaultAsync(q => q.Id == id, cancellationToken);
        if (quotation is null)
        {
            return NotFound;
        }

        if (quotation.Status != QuotationStatus.Draft)
        {
            return NotDraft;
        }

        if (quotation.Lines.Count >= MaxLines)
        {
            return new Error(InvalidState, $"Bir teklifte en fazla {MaxLines} satır olabilir.", ErrorType.Conflict);
        }

        var line = new OrmeQuotationLine { Sequence = quotation.Lines.Count == 0 ? 1 : quotation.Lines.Max(l => l.Sequence) + 1 };
        if (await ApplyLineAsync(line, request, isNew: true, cancellationToken) is { } error)
        {
            return error;
        }

        quotation.Lines.Add(line);
        await db.SaveChangesAsync(cancellationToken);
        return await LoadAsync(id, cancellationToken);
    }

    public async Task<Result<OrmeQuotationDto>> UpdateLineAsync(int id, int lineId, OrmeQuotationLineRequest request, CancellationToken cancellationToken)
    {
        var line = await db.Set<OrmeQuotationLine>()
            .Include(l => l.Quotation)
            .Include(l => l.Yarns)
            .SingleOrDefaultAsync(l => l.Id == lineId && l.QuotationId == id, cancellationToken);
        if (line is null)
        {
            return LineNotFound;
        }

        if (line.Quotation.Status != QuotationStatus.Draft)
        {
            return NotDraft;
        }

        if (await ApplyLineAsync(line, request, isNew: false, cancellationToken) is { } error)
        {
            return error;
        }

        await db.SaveChangesAsync(cancellationToken);
        return await LoadAsync(id, cancellationToken);
    }

    public async Task<Result<OrmeQuotationDto>> DeleteLineAsync(int id, int lineId, CancellationToken cancellationToken)
    {
        var line = await db.Set<OrmeQuotationLine>()
            .Include(l => l.Quotation)
            .Include(l => l.Yarns)
            .SingleOrDefaultAsync(l => l.Id == lineId && l.QuotationId == id, cancellationToken);
        if (line is null)
        {
            return LineNotFound;
        }

        if (line.Quotation.Status != QuotationStatus.Draft)
        {
            return NotDraft;
        }

        db.Set<OrmeQuotationLineYarn>().RemoveRange(line.Yarns);
        db.Set<OrmeQuotationLine>().Remove(line);
        await db.SaveChangesAsync(cancellationToken);
        return await LoadAsync(id, cancellationToken);
    }

    public async Task<Result<OrmeQuotationDto>> SendAsync(int id, CancellationToken cancellationToken)
    {
        var quotation = await db.Set<OrmeQuotation>().SingleOrDefaultAsync(q => q.Id == id, cancellationToken);
        if (quotation is null)
        {
            return NotFound;
        }

        if (quotation.Status != QuotationStatus.Draft)
        {
            return new Error(InvalidState, "Yalnızca taslak teklif gönderilebilir.", ErrorType.Conflict);
        }

        if (!await db.Set<OrmeQuotationLine>().AnyAsync(l => l.QuotationId == id, cancellationToken))
        {
            return new Error(InvalidState, "Satırı olmayan teklif gönderilemez.", ErrorType.Conflict);
        }

        if (quotation.ValidUntil < Today)
        {
            return ReferenceValidation.InvalidReference("validUntil", "Geçerlilik tarihi geçmiş; göndermeden önce tarihi güncelleyin.");
        }

        quotation.Status = QuotationStatus.Sent;
        await db.SaveChangesAsync(cancellationToken);
        return await LoadAsync(id, cancellationToken);
    }

    /// <summary>Gönderilmiş, reddedilmiş ya da süresi dolmuş teklifi düzeltmek için taslağa döndürür; numara korunur.</summary>
    public async Task<Result<OrmeQuotationDto>> ReopenAsync(int id, CancellationToken cancellationToken)
    {
        await ExpireAsync(cancellationToken);
        var quotation = await db.Set<OrmeQuotation>().SingleOrDefaultAsync(q => q.Id == id, cancellationToken);
        if (quotation is null)
        {
            return NotFound;
        }

        if (quotation.Status is not (QuotationStatus.Sent or QuotationStatus.Rejected or QuotationStatus.Expired))
        {
            return new Error(InvalidState, "Yalnızca gönderilmiş, reddedilmiş ya da süresi dolmuş teklif taslağa alınabilir.", ErrorType.Conflict);
        }

        quotation.Status = QuotationStatus.Draft;
        await db.SaveChangesAsync(cancellationToken);
        return await LoadAsync(id, cancellationToken);
    }

    public async Task<Result<OrmeQuotationDto>> RejectAsync(int id, CancellationToken cancellationToken)
    {
        await ExpireAsync(cancellationToken);
        var quotation = await db.Set<OrmeQuotation>().SingleOrDefaultAsync(q => q.Id == id, cancellationToken);
        if (quotation is null)
        {
            return NotFound;
        }

        if (quotation.Status is not (QuotationStatus.Sent or QuotationStatus.Expired))
        {
            return new Error(InvalidState, "Yalnızca gönderilmiş teklif reddedilebilir.", ErrorType.Conflict);
        }

        quotation.Status = QuotationStatus.Rejected;
        await db.SaveChangesAsync(cancellationToken);
        return await LoadAsync(id, cancellationToken);
    }

    /// <summary>
    /// Kabul: satırlardan taslak sipariş açılır (fason ve kendi malı satırlar ayrı siparişe), fiyatlar teklif fiyatıdır.
    /// Aday firmaya verilen teklifte müşteri seçilir ve teklif o müşteriye bağlanır.
    /// </summary>
    public async Task<Result<OrmeQuotationDto>> AcceptAsync(int id, OrmeQuotationAcceptRequest request, CancellationToken cancellationToken)
    {
        await ExpireAsync(cancellationToken);

        // İşlem ve kilit en başta alınır: aynı teklif iki kez kabul edilip iki kez sipariş açılmaz.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await appLock.TryAcquireAsync($"OrmeQuotation:{id}", LockTimeout, cancellationToken))
        {
            return Busy;
        }

        var quotation = await db.Set<OrmeQuotation>().Include(q => q.Lines).SingleOrDefaultAsync(q => q.Id == id, cancellationToken);
        if (quotation is null)
        {
            return NotFound;
        }

        if (quotation.Status != QuotationStatus.Sent)
        {
            return new Error(
                InvalidState,
                quotation.Status == QuotationStatus.Expired
                    ? "Süresi dolmuş teklif kabul edilemez; taslağa alıp geçerlilik tarihini güncelleyin ve yeniden gönderin."
                    : "Yalnızca gönderilmiş teklif kabul edilebilir.",
                ErrorType.Conflict);
        }

        if (quotation.Lines.Count == 0)
        {
            return new Error(InvalidState, "Satırı olmayan teklif kabul edilemez.", ErrorType.Conflict);
        }

        var customerId = quotation.CustomerId ?? request.CustomerId;
        if (customerId is null)
        {
            return ReferenceValidation.InvalidReference("customerId", "Teklif aday firmaya verilmiş; sipariş için müşteri kartını seçin.");
        }

        if (quotation.CustomerId is null
            && await ReferenceValidation.RequireSelectableAsync(db.Set<OrmeCustomer>(), customerId, null, "customerId", "Müşteri", cancellationToken) is { } customerError)
        {
            return customerError;
        }

        // Sipariş yalnız etkin kumaşla açılır.
        if (await ReferenceValidation.RequireSelectableAsync(
                db.Set<OrmeFabric>(), quotation.Lines.Select(l => l.FabricId).Distinct().ToList(), new HashSet<int>(), "lines", "Kumaş kartı", cancellationToken) is { } fabricError)
        {
            return fabricError;
        }

        var orderDate = request.OrderDate ?? Today;
        if (request.DueDate is { } due && due < orderDate)
        {
            return ReferenceValidation.InvalidReference("dueDate", "Termin sipariş tarihinden önce olamaz.");
        }

        var year = orderDate.Year;
        foreach (var group in quotation.Lines.GroupBy(l => l.IsCommission).OrderByDescending(g => g.Key))
        {
            var sequence = await numbers.NextAsync(OrmeOrderService.OrderSeries, year, cancellationToken);
            var order = new OrmeOrder
            {
                OrderNo = OrmeOrderService.OrderNoFor(year, sequence),
                OrderDate = orderDate,
                CustomerId = customerId,
                CustomerOrderNo = NullIfBlank(request.CustomerOrderNo),
                Ownership = group.Key ? OrderOwnership.Commission : OrderOwnership.OwnGoods,
                DueDate = request.DueDate,
                Currency = quotation.Currency,
                Notes = $"{quotation.QuotationNo} teklifinden oluşturuldu.",
                Status = OrderStatus.Draft,
            };

            foreach (var line in group.OrderBy(l => l.Sequence))
            {
                var orderLine = new OrmeOrderLine
                {
                    FabricId = line.FabricId,
                    Kilograms = line.Kilograms,
                    UnitPrice = line.UnitPrice,
                    CustomerFabricCode = line.CustomerFabricCode,
                    Notes = line.Notes,
                };
                order.Lines.Add(orderLine);
                line.OrderLine = orderLine;
            }

            db.Set<OrmeOrder>().Add(order);
        }

        quotation.CustomerId = customerId;
        quotation.Status = QuotationStatus.Accepted;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await LoadAsync(id, cancellationToken);
    }

    /// <summary>Müşteriye basılan teklif: dil tekliften, boşsa müşteri kartından; KDV müşterinin varsayılan oranıyla.</summary>
    public async Task<Result<OrmeQuotationPrintDto>> GetPrintAsync(int id, CancellationToken cancellationToken)
    {
        await ExpireAsync(cancellationToken);
        var quotation = await db.Set<OrmeQuotation>().AsNoTracking().IgnoreQueryFilters()
            .Include(q => q.Customer)
            .Include(q => q.Lines).ThenInclude(l => l.Fabric)
            .SingleOrDefaultAsync(q => q.Id == id, cancellationToken);
        if (quotation is null)
        {
            return NotFound;
        }

        var customer = quotation.Customer;
        var address = customer is null
            ? null
            : await db.Set<OrmeCustomerAddress>().AsNoTracking()
                .Where(a => a.CustomerId == customer.Id && a.IsDefaultInvoice)
                .Select(a => a.AddressLine + " " + a.District + "/" + a.City)
                .FirstOrDefaultAsync(cancellationToken);

        var lines = quotation.Lines.OrderBy(l => l.Sequence).Select(l => new OrmeQuotationPrintLineDto(
            l.Id,
            l.Fabric.FabricCode,
            l.Fabric.Name,
            l.Fabric.KnitName,
            l.Fabric.Composition,
            l.Fabric.RawWeightGsm,
            l.Fabric.RawWidthCm,
            l.CustomerFabricCode,
            l.Kilograms,
            l.IsCommission,
            l.UnitPrice,
            InvoiceAmounts.Line(l.Kilograms, l.UnitPrice),
            l.Notes)).ToList();
        var amounts = InvoiceAmounts.Of(lines.Sum(l => l.Amount), customer?.VatRate, null);

        return new OrmeQuotationPrintDto(
            quotation.Id,
            quotation.QuotationNo,
            quotation.Status,
            quotation.QuotationDate,
            quotation.ValidUntil,
            quotation.Currency,
            quotation.Notes,
            customer?.CustomerCode,
            customer?.Name ?? quotation.ProspectName ?? string.Empty,
            customer?.TaxNumber,
            customer?.TaxOffice,
            address,
            PrintLanguage(quotation),
            customer?.PaymentTermDays,
            customer?.VatRate,
            customer?.SecondQualityDiscountPercent,
            lines.Sum(l => l.Kilograms),
            amounts.Subtotal,
            amounts.Vat,
            amounts.Subtotal + amounts.Vat,
            lines.Any(l => l.IsCommission),
            lines);
    }

    /// <summary>
    /// Satır için varsayılan maliyet girdileri: kumaş kartındaki fire (yoksa parametre) ve kg/saat, makinenin (yoksa genel)
    /// saat maliyeti, işçilik, genel gider, kâr ve ipliklerin son alış fiyatı. Tutarlar teklifin para birimine çevrilir.
    /// </summary>
    public async Task<Result<OrmeQuotationCostDefaultsDto>> GetCostDefaultsAsync(OrmeQuotationCostDefaultsQuery query, CancellationToken cancellationToken)
    {
        var fabric = await db.Set<OrmeFabric>().AsNoTracking()
            .Include(f => f.Yarns).ThenInclude(y => y.Yarn)
            .SingleOrDefaultAsync(f => f.Id == query.FabricId, cancellationToken);
        if (fabric is null)
        {
            return new Error(ErrorCodes.NotFound, "Kumaş kartı bulunamadı.", ErrorType.NotFound);
        }

        var currency = NullIfBlank(query.Currency)?.ToUpperInvariant() ?? Currency.LocalCode;
        var date = query.Date ?? Today;
        var settings = await OrmeCostSettingsService.LoadAsync(db, cancellationToken);

        var machineCost = settings.MachineHourlyCost;
        if (query.MachineId is { } machineId)
        {
            var rate = await db.Set<OrmeMachineCostRate>().AsNoTracking()
                .Where(r => r.MachineId == machineId)
                .Select(r => (decimal?)r.HourlyCost)
                .SingleOrDefaultAsync(cancellationToken);
            machineCost = rate ?? machineCost;
        }

        // Parametreler yerel para birimindedir.
        var localRate = await exchangeRates.ConvertAsync(1m, Currency.LocalCode, currency, date, OrmeYarnPrices.ConversionRate, cancellationToken);
        var missingRate = localRate is null;
        var toCurrency = localRate ?? 0;

        var recipe = fabric.Yarns.OrderBy(y => y.Sequence).ToList();
        var prices = await OrmeYarnPrices.LastAsync(db, exchangeRates, recipe.Select(y => y.YarnId).ToList(), currency, date, cancellationToken);
        var yarns = recipe.Select(y =>
        {
            var price = prices.GetValueOrDefault(y.YarnId);
            if (price is { UnitPrice: null, OriginalPrice: not null })
            {
                missingRate = true;
            }

            return new OrmeQuotationYarnCostDto(
                y.YarnId, y.Yarn.YarnCode, y.Yarn.Name, y.Yarn.Color, y.Percent,
                price?.UnitPrice, price?.Source ?? QuotationPriceSource.None, price?.Reference, price?.Date, price?.OriginalPrice, price?.OriginalCurrency);
        }).ToList();

        return new OrmeQuotationCostDefaultsDto(
            fabric.Id,
            fabric.FabricCode,
            fabric.Name,
            recipe.Count > 0,
            fabric.WastePercent ?? settings.DefaultWastePercent,
            fabric.ExpectedKilogramsPerHour,
            query.MachineId,
            Round4(machineCost * toCurrency),
            Round4(settings.LaborHourlyCost * toCurrency),
            settings.OverheadPercent,
            settings.DefaultMarginPercent,
            currency,
            missingRate,
            yarns);
    }

    private async Task<Error?> ValidateHeaderAsync(OrmeQuotationRequest request, OrmeQuotation? existing, CancellationToken cancellationToken)
    {
        if (await ReferenceValidation.RequireSelectableAsync(
                db.Set<OrmeCustomer>(), request.CustomerId, existing?.CustomerId, "customerId", "Müşteri", cancellationToken) is { } customerError)
        {
            return customerError;
        }

        return await CurrencyRules.RequireActiveAsync(db, request.Currency, existing?.Currency, "currency", cancellationToken);
    }

    private async Task<string> CustomerCurrencyAsync(int? customerId, CancellationToken cancellationToken) =>
        customerId is { } id
            ? await db.Set<OrmeCustomer>().Where(c => c.Id == id).Select(c => c.Currency).SingleAsync(cancellationToken)
            : Currency.LocalCode;

    private static void ApplyHeader(OrmeQuotation quotation, OrmeQuotationRequest request, string defaultCurrency)
    {
        quotation.CustomerId = request.CustomerId;
        quotation.ProspectName = request.CustomerId is null ? NullIfBlank(request.ProspectName) : null;
        quotation.QuotationDate = request.QuotationDate;
        quotation.ValidUntil = request.ValidUntil;
        quotation.Currency = NullIfBlank(request.Currency)?.ToUpperInvariant() ?? defaultCurrency;
        quotation.DocumentLanguage = request.DocumentLanguage;
        quotation.Notes = NullIfBlank(request.Notes);
    }

    /// <summary>
    /// Satırı istekten doldurur: iplik oranları kumaş kartından, fiyatları istekten gelir. Fiyat girilmişse kâr fiyattan,
    /// girilmemişse fiyat kârdan hesaplanır.
    /// </summary>
    private async Task<Error?> ApplyLineAsync(OrmeQuotationLine line, OrmeQuotationLineRequest request, bool isNew, CancellationToken cancellationToken)
    {
        // Satırda zaten bulunan pasif kumaş ya da makine kalabilir; yeni seçim etkin olmalıdır.
        if (await ReferenceValidation.RequireSelectableAsync(
                db.Set<OrmeFabric>(), request.FabricId, isNew ? null : (int?)line.FabricId, "fabricId", "Kumaş kartı", cancellationToken) is { } fabricError)
        {
            return fabricError;
        }

        if (await ReferenceValidation.RequireSelectableAsync(
                db.Set<OrmeMachine>(), request.MachineId, isNew ? null : line.MachineId, "machineId", "Makine", cancellationToken) is { } machineError)
        {
            return machineError;
        }

        var recipe = await db.Set<OrmeFabricYarn>().AsNoTracking()
            .Where(y => y.FabricId == request.FabricId)
            .OrderBy(y => y.Sequence)
            .Select(y => new { y.YarnId, y.Percent })
            .ToListAsync(cancellationToken);
        if (!request.IsCommission && recipe.Count == 0)
        {
            return ReferenceValidation.InvalidReference(
                "fabricId", "Kumaş kartında iplik tanımlı değil; kendi malı teklifte iplik maliyeti hesaplanamaz. Önce kumaşın iplik bilgisini tamamlayın ya da fason teklif verin.");
        }

        line.FabricId = request.FabricId;
        line.Kilograms = request.Kilograms;
        line.IsCommission = request.IsCommission;
        line.MachineId = request.MachineId;
        line.WastePercent = request.WastePercent;
        line.KilogramsPerHour = request.KilogramsPerHour;
        line.MachineHourlyCost = request.MachineHourlyCost;
        line.LaborHourlyCost = request.LaborHourlyCost;
        line.OverheadPercent = request.OverheadPercent;
        line.CustomerFabricCode = NullIfBlank(request.CustomerFabricCode);
        line.Notes = NullIfBlank(request.Notes);

        // Fasonda iplik maliyete girmez; satır reçetesiz saklanır.
        db.Set<OrmeQuotationLineYarn>().RemoveRange(line.Yarns.ToList());
        line.Yarns.Clear();
        if (!request.IsCommission)
        {
            foreach (var item in recipe)
            {
                var price = request.Yarns.FirstOrDefault(y => y.YarnId == item.YarnId);
                line.Yarns.Add(new OrmeQuotationLineYarn
                {
                    YarnId = item.YarnId,
                    Percent = item.Percent,
                    UnitPrice = price?.UnitPrice,
                    PriceSource = price?.UnitPrice is null ? QuotationPriceSource.None : price.PriceSource,
                    PriceReference = price?.UnitPrice is null ? null : NullIfBlank(price.PriceReference),
                });
            }
        }

        var cost = Breakdown(line, request.MarginPercent);
        if (request.UnitPrice is { } manual)
        {
            line.UnitPrice = manual;
            line.MarginPercent = QuotationCost.MarginFor(cost.TotalCost, manual) ?? request.MarginPercent;
        }
        else
        {
            line.UnitPrice = cost.CalculatedPrice;
            line.MarginPercent = request.MarginPercent;
        }

        return null;
    }

    private static QuotationCostBreakdown Breakdown(OrmeQuotationLine line, decimal marginPercent) =>
        QuotationCost.Of(
            line.IsCommission,
            line.Yarns.Select(y => (y.Percent, y.UnitPrice)),
            line.WastePercent,
            line.KilogramsPerHour,
            line.MachineHourlyCost,
            line.LaborHourlyCost,
            line.OverheadPercent,
            marginPercent);

    private static DocumentLanguage PrintLanguage(OrmeQuotation quotation) =>
        quotation.DocumentLanguage ?? quotation.Customer?.DocumentLanguage ?? DocumentLanguage.Turkish;

    private static decimal Round4(decimal value) => Math.Round(value, 4, MidpointRounding.AwayFromZero);

    private static decimal Round2(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private async Task<Result<OrmeQuotationDto>> LoadAsync(int id, CancellationToken cancellationToken)
    {
        // Belge olarak sonradan pasife alınan müşteri, kumaş, makine ya da iplik de görünür.
        var quotation = await db.Set<OrmeQuotation>().AsNoTracking().IgnoreQueryFilters()
            .Include(q => q.Customer)
            .Include(q => q.Lines).ThenInclude(l => l.Fabric)
            .Include(q => q.Lines).ThenInclude(l => l.Machine)
            .Include(q => q.Lines).ThenInclude(l => l.Yarns).ThenInclude(y => y.Yarn)
            .Include(q => q.Lines).ThenInclude(l => l.OrderLine).ThenInclude(o => o!.Order)
            .SingleOrDefaultAsync(q => q.Id == id, cancellationToken);
        if (quotation is null)
        {
            return NotFound;
        }

        var lines = quotation.Lines.OrderBy(l => l.Sequence).Select(l =>
        {
            var cost = Breakdown(l, l.MarginPercent);
            var wasteFactor = 1 + l.WastePercent / 100m;
            var yarns = l.Yarns.OrderBy(y => y.Id).Select(y => new OrmeQuotationLineYarnDto(
                y.YarnId,
                y.Yarn.YarnCode,
                y.Yarn.Name,
                y.Yarn.Color,
                y.Percent,
                y.UnitPrice,
                y.PriceSource,
                y.PriceReference,
                Round4(y.Percent / 100m * (y.UnitPrice ?? 0) * wasteFactor))).ToList();
            var missing = l.KilogramsPerHour is null || (!l.IsCommission && (yarns.Count == 0 || yarns.Any(y => y.UnitPrice is null)));
            return new OrmeQuotationLineDto(
                l.Id,
                l.Sequence,
                l.FabricId,
                l.Fabric.FabricCode,
                l.Fabric.Name,
                l.CustomerFabricCode,
                l.Kilograms,
                l.IsCommission,
                l.MachineId,
                l.Machine?.Code,
                l.Machine?.Name,
                l.WastePercent,
                l.KilogramsPerHour,
                l.MachineHourlyCost,
                l.LaborHourlyCost,
                l.OverheadPercent,
                l.MarginPercent,
                cost.YarnCost,
                cost.KnittingCost,
                cost.Overhead,
                cost.TotalCost,
                cost.CalculatedPrice,
                l.UnitPrice,
                InvoiceAmounts.Line(l.Kilograms, l.UnitPrice),
                Round2(l.Kilograms * cost.TotalCost),
                missing,
                l.Notes,
                l.OrderLine?.OrderId,
                l.OrderLine?.Order.OrderNo,
                yarns);
        }).ToList();

        var orders = quotation.Lines
            .Where(l => l.OrderLine is not null)
            .Select(l => l.OrderLine!.Order)
            .DistinctBy(o => o.Id)
            .OrderBy(o => o.OrderNo)
            .Select(o => new OrmeQuotationOrderDto(o.Id, o.OrderNo, o.Ownership, o.Status))
            .ToList();

        var totalAmount = lines.Sum(l => l.Amount);
        var totalCost = lines.Sum(l => l.CostAmount);
        return new OrmeQuotationDto(
            quotation.Id,
            quotation.QuotationNo,
            quotation.QuotationDate,
            quotation.ValidUntil,
            quotation.CustomerId,
            quotation.Customer?.Name,
            quotation.ProspectName,
            quotation.Currency,
            quotation.DocumentLanguage,
            PrintLanguage(quotation),
            quotation.Status,
            quotation.Notes,
            lines.Sum(l => l.Kilograms),
            totalAmount,
            totalCost,
            totalAmount - totalCost,
            totalCost > 0 ? Round2((totalAmount - totalCost) / totalCost * 100m) : null,
            lines.Any(l => l.HasMissingCosts),
            quotation.CreatedAt,
            quotation.CreatedBy,
            orders,
            lines);
    }
}
