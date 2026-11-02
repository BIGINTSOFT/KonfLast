using ERP.Boyahane.Application.Features.Batches;
using ERP.Boyahane.Application.Features.Orders;
using ERP.Boyahane.Domain.Entities;
using ERP.Boyahane.Domain.Enums;
using ERP.Boyahane.Domain.Rules;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.Finance;
using ERP.Core.Application.Features.MasterData;
using ERP.Core.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Boyahane.Application.Features.Quotations;

public sealed class BoyahaneQuotationQuery : PageRequest
{
    /// <summary>Teklif no, müşteri ya da aday adında aranır.</summary>
    public string? Search { get; set; }
}

/// <param name="CustomerName">Müşteri adı; müşteri yoksa aday adı.</param>
/// <param name="IsProspect">Kartı açılmamış aday müşteri.</param>
/// <param name="IsPastValidity">Gönderilmiş teklifin geçerlilik günü geçti; yanıt alınmadı.</param>
public sealed record BoyahaneQuotationListItemDto(
    int Id,
    string QuotationNo,
    DateOnly QuotationDate,
    DateOnly ValidUntil,
    string CustomerName,
    bool IsProspect,
    int LineCount,
    decimal TotalAmount,
    string Currency,
    QuotationStatus Status,
    bool IsPastValidity,
    int? OrderId,
    string? OrderNo);

/// <summary>Teklif satırı: girilen değerler (…Input) ve kayıt anında hesaplanan maliyet dökümü (birim başına, teklif para biriminde).</summary>
public sealed record BoyahaneQuotationLineDto(
    int Id,
    OrderProcessType ProcessType,
    int? FabricId,
    int? YarnId,
    string ItemCode,
    string ItemName,
    string? ColorName,
    string? ColorCode,
    int? ColorCardId,
    string? ColorCardCode,
    int? PatternId,
    string? PatternCode,
    int? ColorwayId,
    string? ColorwayCode,
    decimal Quantity,
    StockUnit Unit,
    int? RecipeTrialId,
    string? RecipeNo,
    int? MachineId,
    string? MachineCode,
    decimal? BatchKilogramsInput,
    decimal? LiquorRatioInput,
    decimal? ProcessMinutesInput,
    decimal? FirePercentInput,
    decimal? OverheadPercentInput,
    decimal? MarginPercentInput,
    decimal? UnitPriceInput,
    decimal BatchKilograms,
    decimal? LiquorRatio,
    decimal ProcessMinutes,
    ProcessMinutesSource MinutesSource,
    decimal? KilogramsPerMeter,
    decimal ChemicalCost,
    decimal PrintCost,
    decimal MachineCost,
    decimal LaborCost,
    decimal FirePercent,
    decimal FireCost,
    decimal OverheadPercent,
    decimal OverheadCost,
    decimal UnitCost,
    decimal? MarginPercent,
    decimal UnitPrice,
    decimal Amount,
    string? CostWarning,
    string? Notes);

/// <param name="IsPastValidity">Gönderilmiş teklifin geçerlilik günü geçti.</param>
/// <param name="TotalCost">Satırların birim maliyeti × miktar toplamı.</param>
/// <param name="MarginPercent">Toplam tutarın toplam maliyete göre kâr payı; maliyet sıfırsa boş.</param>
public sealed record BoyahaneQuotationDto(
    int Id,
    string QuotationNo,
    DateOnly QuotationDate,
    DateOnly ValidUntil,
    int? CustomerId,
    string? CustomerCode,
    string? CustomerName,
    string? ProspectName,
    string? CustomerAddress,
    string? CustomerTaxNumber,
    string? CustomerTaxOffice,
    int? PaymentTermDays,
    OrderOwnership Ownership,
    DateOnly? DueDate,
    string Currency,
    DocumentLanguage DocumentLanguage,
    string? Notes,
    QuotationStatus Status,
    bool IsPastValidity,
    DateTime? SentAt,
    DateTime? DecidedAt,
    string? DecidedBy,
    string? DecisionNote,
    int? OrderId,
    string? OrderNo,
    decimal TotalCost,
    decimal TotalAmount,
    decimal? MarginPercent,
    DateTime CreatedAt,
    string? CreatedBy,
    IReadOnlyList<BoyahaneQuotationLineDto> Lines);

/// <summary>Maliyete giren boya / kimyasal: reçeteden (boyamada %, g/L) ya da baskı pastasından (g/kg pasta).</summary>
/// <param name="KilogramsPerUnit">Satır biriminin (kg ya da m) başına gereken miktar (kg).</param>
/// <param name="CostPerKilogram">Lottan gelen kilogram maliyeti (teklif para biriminde); maliyetli lot yoksa boş.</param>
/// <param name="Cost">Birim başına maliyet.</param>
public sealed record BoyahaneQuotationChemicalCostDto(
    int DyeChemicalId,
    string Code,
    string Name,
    DyeChemicalKind Kind,
    bool Printing,
    decimal Amount,
    decimal KilogramsPerUnit,
    decimal? CostPerKilogram,
    decimal Cost);

/// <summary>Bir satırın hesaplanan maliyeti. Tutarlar teklif para biriminde ve satır biriminin başınadır.</summary>
/// <param name="RecipeFromColorCard">Reçete seçilmedi; renk kartının son onaylı reçetesi kullanıldı.</param>
/// <param name="MachineHourlyCost">Kullanılan makine saat maliyeti (teklif para biriminde).</param>
/// <param name="SuggestedUnitPrice">Maliyet × (1 + kâr payı); fiyat elle girilmişse karşılaştırma içindir.</param>
public sealed record BoyahaneQuotationLineCostDto(
    OrderProcessType ProcessType,
    string ItemCode,
    string ItemName,
    string? ColorName,
    decimal Quantity,
    StockUnit Unit,
    int? RecipeTrialId,
    string? RecipeNo,
    bool RecipeFromColorCard,
    int? MachineId,
    string? MachineCode,
    decimal BatchKilograms,
    decimal? LiquorRatio,
    decimal ProcessMinutes,
    ProcessMinutesSource MinutesSource,
    decimal? KilogramsPerMeter,
    decimal MachineHourlyCost,
    decimal LaborCostPerMinute,
    decimal ChemicalCost,
    decimal PrintCost,
    decimal MachineCost,
    decimal LaborCost,
    decimal FirePercent,
    decimal FireCost,
    decimal OverheadPercent,
    decimal OverheadCost,
    decimal UnitCost,
    decimal? MarginPercent,
    decimal SuggestedUnitPrice,
    decimal UnitPrice,
    bool PriceIsManual,
    decimal Amount,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<BoyahaneQuotationChemicalCostDto> Chemicals);

public sealed record BoyahaneQuotationCalculationDto(string Currency, decimal TotalCost, decimal TotalAmount, IReadOnlyList<BoyahaneQuotationLineCostDto> Lines);

/// <summary>Teklif satırı. Boş bırakılan değerler (parti, flotte, süre, oranlar, fiyat) parametrelerden ya da karttan hesaplanır.</summary>
/// <param name="RecipeTrialId">Boyamada maliyetin dayandığı onaylı reçete; boşsa renk kartının son reçetesi.</param>
/// <param name="MachineId">Saat maliyeti ve parti büyüklüğü için makine.</param>
/// <param name="BatchKilograms">Parti büyüklüğü (kg); boşsa makinenin kapasitesi, o da yoksa varsayılan.</param>
/// <param name="ProcessMinutes">Parti süresi (dakika); boşsa rotanın standart süresi, o da yoksa işlem türünün varsayılanı.</param>
/// <param name="UnitPrice">Elle fiyat; verilirse kâr payı fiyattan geri hesaplanır.</param>
public sealed record BoyahaneQuotationLineRequest(
    OrderProcessType ProcessType,
    decimal Quantity,
    StockUnit Unit,
    int? FabricId = null,
    int? YarnId = null,
    string? ColorName = null,
    string? ColorCode = null,
    int? ColorCardId = null,
    int? PatternId = null,
    int? ColorwayId = null,
    int? RecipeTrialId = null,
    int? MachineId = null,
    decimal? BatchKilograms = null,
    decimal? LiquorRatio = null,
    decimal? ProcessMinutes = null,
    decimal? FirePercent = null,
    decimal? OverheadPercent = null,
    decimal? MarginPercent = null,
    decimal? UnitPrice = null,
    string? Notes = null);

/// <param name="CustomerId">Kayıtlı müşteri; boşsa aday adı zorunludur.</param>
/// <param name="Currency">Boşsa müşterinin para birimi, müşteri yoksa TRY.</param>
/// <param name="DocumentLanguage">Boşsa müşterinin belge dili, müşteri yoksa Türkçe.</param>
public sealed record BoyahaneQuotationRequest(
    DateOnly QuotationDate,
    DateOnly ValidUntil,
    IReadOnlyList<BoyahaneQuotationLineRequest> Lines,
    int? CustomerId = null,
    string? ProspectName = null,
    OrderOwnership Ownership = OrderOwnership.Commission,
    DateOnly? DueDate = null,
    string? Currency = null,
    DocumentLanguage? DocumentLanguage = null,
    string? Notes = null);

public sealed class BoyahaneQuotationRequestValidator : AbstractValidator<BoyahaneQuotationRequest>
{
    public const int MaxLines = 100;

    public BoyahaneQuotationRequestValidator()
    {
        RuleFor(x => x.QuotationDate).NotEmpty().WithMessage("Teklif tarihi zorunludur.");
        RuleFor(x => x.ValidUntil)
            .Must((request, until) => until >= request.QuotationDate).WithMessage("Geçerlilik tarihi teklif tarihinden önce olamaz.");
        RuleFor(x => x)
            .Must(x => x.CustomerId is not null || !string.IsNullOrWhiteSpace(x.ProspectName))
            .WithName("customerId").WithMessage("Müşteri seçin ya da aday müşterinin adını yazın.");
        RuleFor(x => x.ProspectName).OptionalText(200);
        RuleFor(x => x.Ownership).IsInEnum().WithMessage("Geçerli bir iş türü seçin.");
        RuleFor(x => x.DocumentLanguage).IsInEnum().WithMessage("Geçerli bir belge dili seçin.").When(x => x.DocumentLanguage is not null);
        RuleFor(x => x.Notes).OptionalText(1000);
        RuleFor(x => x.DueDate)
            .Must((request, due) => due >= request.QuotationDate).WithMessage("Termin teklif tarihinden önce olamaz.")
            .When(x => x.DueDate is not null);
        RuleFor(x => x.Currency)
            .Matches(@"^\s*[A-Za-z]{3}\s*$").WithMessage("Para birimi üç harfli ISO kodu olmalıdır (örn. TRY, EUR).")
            .When(x => !string.IsNullOrWhiteSpace(x.Currency));
        RuleFor(x => x.Lines)
            .NotEmpty().WithMessage("En az bir satır girilmelidir.")
            .Must(lines => lines is null || lines.Count <= MaxLines).WithMessage($"Bir teklifte en fazla {MaxLines} satır olabilir.")
            .Custom((lines, context) =>
            {
                if (lines is null)
                {
                    return;
                }

                for (var i = 0; i < lines.Count; i++)
                {
                    foreach (var problem in Problems(lines[i]))
                    {
                        context.AddFailure($"{i + 1}. satır: {problem}");
                    }
                }
            });
    }

    private static IEnumerable<string> Problems(BoyahaneQuotationLineRequest line)
    {
        if (!Enum.IsDefined(line.ProcessType))
        {
            yield return "Geçerli bir işlem türü seçin.";
        }

        if (line.ProcessType == OrderProcessType.YarnDyeing)
        {
            if (line.YarnId is null or <= 0)
            {
                yield return "İplik boyamada iplik kartı seçilmelidir.";
            }

            if (line.Unit != StockUnit.Kilogram)
            {
                yield return "İplik boyamada birim kilogram olmalıdır.";
            }
        }
        else
        {
            if (line.FabricId is null or <= 0)
            {
                yield return "Kumaş kartı seçilmelidir.";
            }

            if (line.Unit is not (StockUnit.Meter or StockUnit.Kilogram))
            {
                yield return "Birim metre ya da kilogram olmalıdır.";
            }
        }

        if (line.ColorCardId is not null && line.ProcessType is not (OrderProcessType.FabricDyeing or OrderProcessType.YarnDyeing))
        {
            yield return "Renk kartı yalnızca kumaş ve iplik boyama satırında seçilir.";
        }

        if (line.RecipeTrialId is not null && line.ProcessType is not (OrderProcessType.FabricDyeing or OrderProcessType.YarnDyeing))
        {
            yield return "Reçete yalnızca boyama satırında seçilir.";
        }

        if ((line.PatternId is not null || line.ColorwayId is not null) && line.ProcessType != OrderProcessType.Printing)
        {
            yield return "Desen ve renk yolu yalnızca baskı satırında seçilir.";
        }

        if (line.ColorwayId is not null && line.PatternId is null)
        {
            yield return "Renk yolu için önce desen seçilmelidir.";
        }

        if (line.ColorName?.Trim().Length > 100 || line.ColorCode?.Trim().Length > 50)
        {
            yield return "Renk adı en fazla 100, kodu en fazla 50 karakter olabilir.";
        }

        if (line.Quantity is <= 0 or > 99_999_999)
        {
            yield return "Miktar sıfırdan büyük olmalıdır.";
        }

        if (line.BatchKilograms is <= 0 or > 100_000)
        {
            yield return "Parti büyüklüğü 0 ile 100.000 kg arasında olmalıdır.";
        }

        if (line.LiquorRatio is < 0.1m or > 100)
        {
            yield return "Flotte oranı 0,1 ile 100 arasında olmalıdır.";
        }

        if (line.ProcessMinutes is <= 0 or > 10_000)
        {
            yield return "Parti süresi 0 ile 10.000 dakika arasında olmalıdır.";
        }

        if (line.FirePercent is < 0 or > 50)
        {
            yield return "Fire oranı 0 ile 50 arasında olmalıdır.";
        }

        if (line.OverheadPercent is < 0 or > 100)
        {
            yield return "Genel gider oranı 0 ile 100 arasında olmalıdır.";
        }

        if (line.MarginPercent is < -100 or > 1000)
        {
            yield return "Kâr payı -100 ile 1000 arasında olmalıdır.";
        }

        if (line.UnitPrice is < 0 or > 99_999_999)
        {
            yield return "Birim fiyat negatif olamaz.";
        }

        if (line.Notes?.Trim().Length > 500)
        {
            yield return "Satır notu en fazla 500 karakter olabilir.";
        }
    }
}

/// <param name="CustomerId">Aday müşteriye verilen teklifte, açılan müşteri kartı; kayıtlı müşterili teklifte boş bırakılır.</param>
/// <param name="DueDate">Siparişin termini; boşsa tekliftaki termin.</param>
public sealed record AcceptBoyahaneQuotationRequest(int? CustomerId = null, string? CustomerOrderNo = null, DateOnly? DueDate = null, string? Note = null);

public sealed class AcceptBoyahaneQuotationRequestValidator : AbstractValidator<AcceptBoyahaneQuotationRequest>
{
    public AcceptBoyahaneQuotationRequestValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0).WithMessage("Müşteri geçersiz.").When(x => x.CustomerId is not null);
        RuleFor(x => x.CustomerOrderNo).OptionalText(50);
        RuleFor(x => x.Note).OptionalText(500);
    }
}

/// <param name="Note">Ret ya da süre dolumu açıklaması.</param>
public sealed record BoyahaneQuotationDecisionRequest(string? Note = null);

public sealed class BoyahaneQuotationDecisionRequestValidator : AbstractValidator<BoyahaneQuotationDecisionRequest>
{
    public BoyahaneQuotationDecisionRequestValidator()
    {
        RuleFor(x => x.Note).OptionalText(500);
    }
}

/// <summary>Teklif satırında seçilebilecek reçete: müşterinin kabul ettiği lab denemesi.</summary>
public sealed record BoyahaneQuotationRecipeOptionDto(
    int TrialId,
    string RecipeNo,
    string? ColorCardCode,
    string? ColorName,
    string? ColorCode,
    string? ItemCode,
    string? CustomerName,
    DateOnly? DecisionOn);

public sealed class BoyahaneQuotationRecipeQuery
{
    public int? ColorCardId { get; set; }

    /// <summary>Talep no, renk, kart ya da müşteride aranır.</summary>
    public string? Search { get; set; }
}

public interface IBoyahaneQuotationService
{
    Task<PagedResult<BoyahaneQuotationListItemDto>> GetListAsync(BoyahaneQuotationQuery query, CancellationToken cancellationToken);
    Task<Result<BoyahaneQuotationDto>> GetAsync(int id, CancellationToken cancellationToken);
    Task<Result<BoyahaneQuotationCalculationDto>> CalculateAsync(BoyahaneQuotationRequest request, CancellationToken cancellationToken);
    Task<Result<BoyahaneQuotationDto>> CreateAsync(BoyahaneQuotationRequest request, CancellationToken cancellationToken);
    Task<Result<BoyahaneQuotationDto>> UpdateAsync(int id, BoyahaneQuotationRequest request, CancellationToken cancellationToken);
    Task<Result<BoyahaneQuotationDto>> RecalculateAsync(int id, CancellationToken cancellationToken);
    Task<Result<BoyahaneQuotationDto>> SendAsync(int id, CancellationToken cancellationToken);
    Task<Result<BoyahaneQuotationDto>> AcceptAsync(int id, AcceptBoyahaneQuotationRequest request, CancellationToken cancellationToken);
    Task<Result<BoyahaneQuotationDto>> RejectAsync(int id, BoyahaneQuotationDecisionRequest request, CancellationToken cancellationToken);
    Task<Result<BoyahaneQuotationDto>> ExpireAsync(int id, BoyahaneQuotationDecisionRequest request, CancellationToken cancellationToken);
    Task<Result<BoyahaneQuotationDto>> ReopenAsync(int id, CancellationToken cancellationToken);
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<BoyahaneQuotationRecipeOptionDto>> GetRecipeOptionsAsync(BoyahaneQuotationRecipeQuery query, CancellationToken cancellationToken);
}

/// <summary>
/// Fiyat teklifi ve ön maliyet. Birim maliyet = boya-kimyasal (reçete, flotte ve lot maliyetinden) + baskı pastası (renk yolu
/// reçetesinden) + makine saati (enerji, su) + işçilik; üzerine fire ve genel gider eklenir. Fiyat = maliyet × (1 + kâr payı).
/// Süre, rotadaki operasyonların standart sürelerinden, yoksa işlem türünün varsayılanından gelir; parti süresi parti kilosuna
/// bölünerek kilo başına dağıtılır. Metre satırında kilo, kumaş kartının ham en ve gramajından çevrilir.
/// </summary>
public sealed class BoyahaneQuotationService(
    IAppDbContext db,
    IDocumentNumberGenerator numbers,
    ICurrentUser currentUser,
    IExchangeRateService exchangeRates,
    IBoyahaneOrderService orders,
    IAppLock appLock,
    TimeProvider timeProvider) : IBoyahaneQuotationService
{
    private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(10);
    private static readonly Error Busy = new(ErrorCodes.ResourceBusy, "Teklif üzerinde başka bir işlem sürüyor, biraz sonra yeniden deneyin.", ErrorType.Conflict);

    private const string Series = "BoyahaneQuotation";
    private const string InvalidState = "BOYAHANE_QUOTATION_INVALID_STATE";
    private const int MaxRecipeOptions = 50;

    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Teklif bulunamadı.", ErrorType.NotFound);

    private DateOnly Today => DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);

    public Task<PagedResult<BoyahaneQuotationListItemDto>> GetListAsync(BoyahaneQuotationQuery query, CancellationToken cancellationToken)
    {
        var today = Today;
        var quotations = db.Set<BoyahaneQuotation>().AsNoTracking();
        if (NullIfBlank(query.Search) is { } term)
        {
            quotations = quotations.Where(q => q.QuotationNo.Contains(term)
                || (q.ProspectName != null && q.ProspectName.Contains(term))
                || (q.Customer != null && q.Customer.Name.Contains(term)));
        }

        return quotations
            .OrderByDescending(q => q.QuotationDate).ThenByDescending(q => q.Id)
            .Select(q => new BoyahaneQuotationListItemDto(
                q.Id,
                q.QuotationNo,
                q.QuotationDate,
                q.ValidUntil,
                q.Customer != null ? q.Customer.Name : q.ProspectName ?? "",
                q.CustomerId == null,
                q.Lines.Count,
                q.Lines.Sum(l => Math.Round(l.Quantity * l.UnitPrice, 2)),
                q.Currency,
                q.Status,
                q.Status == QuotationStatus.Sent && q.ValidUntil < today,
                q.OrderId,
                q.Order != null ? q.Order.OrderNo : null))
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<Result<BoyahaneQuotationDto>> GetAsync(int id, CancellationToken cancellationToken)
    {
        var quotation = await db.Set<BoyahaneQuotation>().AsNoTracking()
            .Include(q => q.Customer)
            .Include(q => q.Order)
            .Include(q => q.Lines).ThenInclude(l => l.Fabric)
            .Include(q => q.Lines).ThenInclude(l => l.Yarn)
            .Include(q => q.Lines).ThenInclude(l => l.ColorCard)
            .Include(q => q.Lines).ThenInclude(l => l.Pattern)
            .Include(q => q.Lines).ThenInclude(l => l.Colorway)
            .Include(q => q.Lines).ThenInclude(l => l.Machine)
            .Include(q => q.Lines).ThenInclude(l => l.RecipeTrial).ThenInclude(t => t!.Request)
            .SingleOrDefaultAsync(q => q.Id == id, cancellationToken);
        if (quotation is null)
        {
            return NotFound;
        }

        var customer = quotation.Customer;
        var address = customer is null
            ? null
            : await db.Set<BoyahaneCustomerAddress>().AsNoTracking()
                .Where(a => a.CustomerId == customer.Id && a.IsDefaultInvoice)
                .Select(a => a.AddressLine + " " + a.District + "/" + a.City)
                .FirstOrDefaultAsync(cancellationToken);

        var lines = quotation.Lines.OrderBy(l => l.Id).Select(l => new BoyahaneQuotationLineDto(
            l.Id,
            l.ProcessType,
            l.FabricId,
            l.YarnId,
            l.Fabric?.FabricCode ?? l.Yarn?.YarnCode ?? "",
            l.Fabric?.Name ?? l.Yarn?.Name ?? "",
            l.ColorName,
            l.ColorCode,
            l.ColorCardId,
            l.ColorCard?.Code,
            l.PatternId,
            l.Pattern?.Code,
            l.ColorwayId,
            l.Colorway?.Code,
            l.Quantity,
            l.Unit,
            l.RecipeTrialId,
            l.RecipeTrial is null ? null : $"{l.RecipeTrial.Request.RequestNo}/{l.RecipeTrial.TrialCode}",
            l.MachineId,
            l.Machine?.Code,
            l.BatchKilogramsInput,
            l.LiquorRatioInput,
            l.ProcessMinutesInput,
            l.FirePercentInput,
            l.OverheadPercentInput,
            l.MarginPercentInput,
            l.UnitPriceInput,
            l.BatchKilograms,
            l.LiquorRatio,
            l.ProcessMinutes,
            l.MinutesSource,
            l.KilogramsPerMeter,
            l.ChemicalCost,
            l.PrintCost,
            l.MachineCost,
            l.LaborCost,
            l.FirePercent,
            l.FireCost,
            l.OverheadPercent,
            l.OverheadCost,
            l.UnitCost,
            l.MarginPercent,
            l.UnitPrice,
            InvoiceAmounts.Line(l.Quantity, l.UnitPrice),
            l.CostWarning,
            l.Notes)).ToList();

        var totalAmount = lines.Sum(l => l.Amount);
        var totalCost = Math.Round(lines.Sum(l => l.Quantity * l.UnitCost), 2);
        return new BoyahaneQuotationDto(
            quotation.Id,
            quotation.QuotationNo,
            quotation.QuotationDate,
            quotation.ValidUntil,
            quotation.CustomerId,
            customer?.CustomerCode,
            customer?.Name,
            quotation.ProspectName,
            address,
            customer?.TaxNumber,
            customer?.TaxOffice,
            customer?.PaymentTermDays,
            quotation.Ownership,
            quotation.DueDate,
            quotation.Currency,
            quotation.DocumentLanguage,
            quotation.Notes,
            quotation.Status,
            quotation.Status == QuotationStatus.Sent && quotation.ValidUntil < Today,
            quotation.SentAt,
            quotation.DecidedAt,
            quotation.DecidedBy,
            quotation.DecisionNote,
            quotation.OrderId,
            quotation.Order?.OrderNo,
            totalCost,
            totalAmount,
            totalCost > 0 ? Math.Round((totalAmount / totalCost - 1) * 100, 2) : null,
            quotation.CreatedAt,
            quotation.CreatedBy,
            lines);
    }

    public async Task<Result<BoyahaneQuotationCalculationDto>> CalculateAsync(BoyahaneQuotationRequest request, CancellationToken cancellationToken)
    {
        if (await ValidateReferencesAsync(request, null, cancellationToken) is { } error)
        {
            return error;
        }

        var currency = await ResolveCurrencyAsync(request, cancellationToken);
        var lines = await CalculateLinesAsync(request, currency, cancellationToken);
        return new BoyahaneQuotationCalculationDto(
            currency,
            Math.Round(lines.Sum(l => l.Quantity * l.UnitCost), 2),
            lines.Sum(l => l.Amount),
            lines);
    }

    public async Task<Result<BoyahaneQuotationDto>> CreateAsync(BoyahaneQuotationRequest request, CancellationToken cancellationToken)
    {
        if (await ValidateReferencesAsync(request, null, cancellationToken) is { } error)
        {
            return error;
        }

        var currency = await ResolveCurrencyAsync(request, cancellationToken);
        var language = await ResolveLanguageAsync(request, cancellationToken);
        var costs = await CalculateLinesAsync(request, currency, cancellationToken);
        var archive = await LoadArchiveAsync(request.Lines, cancellationToken);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var year = request.QuotationDate.Year;
        var sequence = await numbers.NextAsync(Series, year, cancellationToken);
        var quotation = new BoyahaneQuotation { QuotationNo = $"BT-{year}-{sequence:D5}" };
        Apply(quotation, request, currency, language);
        ReplaceLines(quotation, request, costs, archive);
        db.Set<BoyahaneQuotation>().Add(quotation);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(quotation.Id, cancellationToken);
    }

    public async Task<Result<BoyahaneQuotationDto>> UpdateAsync(int id, BoyahaneQuotationRequest request, CancellationToken cancellationToken)
    {
        var quotation = await db.Set<BoyahaneQuotation>().Include(q => q.Lines).SingleOrDefaultAsync(q => q.Id == id, cancellationToken);
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

        var currency = await ResolveCurrencyAsync(request, cancellationToken);
        var language = await ResolveLanguageAsync(request, cancellationToken);
        var costs = await CalculateLinesAsync(request, currency, cancellationToken);
        var archive = await LoadArchiveAsync(request.Lines, cancellationToken);

        Apply(quotation, request, currency, language);
        db.Set<BoyahaneQuotationLine>().RemoveRange(quotation.Lines);
        quotation.Lines.Clear();
        ReplaceLines(quotation, request, costs, archive);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    /// <summary>Taslağın maliyetini güncel lot fiyatları, kur ve parametrelerle yeniden hesaplar; girilen değerler korunur.</summary>
    public async Task<Result<BoyahaneQuotationDto>> RecalculateAsync(int id, CancellationToken cancellationToken)
    {
        var quotation = await db.Set<BoyahaneQuotation>().AsNoTracking().Include(q => q.Lines).SingleOrDefaultAsync(q => q.Id == id, cancellationToken);
        if (quotation is null)
        {
            return NotFound;
        }

        return await UpdateAsync(id, ToRequest(quotation), cancellationToken);
    }

    public async Task<Result<BoyahaneQuotationDto>> SendAsync(int id, CancellationToken cancellationToken)
    {
        var quotation = await db.Set<BoyahaneQuotation>().SingleOrDefaultAsync(q => q.Id == id, cancellationToken);
        if (quotation is null)
        {
            return NotFound;
        }

        if (quotation.Status != QuotationStatus.Draft)
        {
            return new Error(InvalidState, "Yalnızca taslak teklif gönderilebilir.", ErrorType.Conflict);
        }

        if (quotation.ValidUntil < Today)
        {
            return new Error(InvalidState, "Geçerlilik tarihi geçmiş teklif gönderilemez; tarihi düzenleyin.", ErrorType.Conflict);
        }

        quotation.Status = QuotationStatus.Sent;
        quotation.SentAt = timeProvider.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    /// <summary>
    /// Gönderilmiş teklifi kabul eder ve satırlarıyla, fiyatlarıyla taslak sipariş açar. Fiyat satırın biriminde olduğu için
    /// siparişte fatura ölçüsü sipariş birimi yazılır. Aday müşteriye verilmiş teklifte önce müşteri kartı açılıp seçilmelidir.
    /// </summary>
    public async Task<Result<BoyahaneQuotationDto>> AcceptAsync(int id, AcceptBoyahaneQuotationRequest request, CancellationToken cancellationToken)
    {
        BoyahaneOrderRequest orderRequest;
        int? previousCustomerId;
        await using (var transaction = await db.Database.BeginTransactionAsync(cancellationToken))
        {
            // Kilit ve önce 'kabul edildi' işaretlemek, aynı teklifin iki kez kabul edilip iki sipariş açılmasını önler.
            if (!await appLock.TryAcquireAsync($"BoyahaneQuotation:{id}", LockTimeout, cancellationToken))
            {
                return Busy;
            }

            var quotation = await db.Set<BoyahaneQuotation>().Include(q => q.Lines).SingleOrDefaultAsync(q => q.Id == id, cancellationToken);
            if (quotation is null)
            {
                return NotFound;
            }

            if (quotation.Status != QuotationStatus.Sent)
            {
                return new Error(InvalidState, "Yalnızca gönderilmiş teklif kabul edilebilir.", ErrorType.Conflict);
            }

            if (quotation.ValidUntil < Today)
            {
                return new Error(InvalidState, "Teklifin geçerliliği dolmuş; süresi doldu olarak işaretleyin ya da taslağa alıp tarihini uzatın.", ErrorType.Conflict);
            }

            if (quotation.CustomerId is not null && request.CustomerId is not null && request.CustomerId != quotation.CustomerId)
            {
                return ReferenceValidation.InvalidReference("customerId", "Teklifin müşterisi değiştirilemez.");
            }

            var customerId = quotation.CustomerId ?? request.CustomerId;
            if (customerId is null && quotation.Ownership == OrderOwnership.Commission)
            {
                return ReferenceValidation.InvalidReference("customerId", "Aday müşteriye verilen teklif için önce müşteri kartını açıp seçin.");
            }

            if (await ReferenceValidation.RequireSelectableAsync(
                    db.Set<BoyahaneCustomer>(), customerId, quotation.CustomerId, "customerId", "Müşteri", cancellationToken) is { } customerError)
            {
                return customerError;
            }

            var today = Today;
            var dueDate = request.DueDate ?? quotation.DueDate;
            orderRequest = new BoyahaneOrderRequest(
                customerId,
                today,
                quotation.Lines.OrderBy(l => l.Id).Select(l => new BoyahaneOrderLineRequest(
                    null,
                    l.ProcessType,
                    l.Quantity,
                    l.Unit,
                    l.FabricId,
                    l.YarnId,
                    l.ColorName,
                    l.ColorCode,
                    l.UnitPrice,
                    l.Notes,
                    BillingUnit.OrderUnit,
                    l.ColorCardId,
                    l.PatternId,
                    l.ColorwayId)).ToList(),
                quotation.Ownership,
                NullIfBlank(request.CustomerOrderNo),
                dueDate is { } due && due >= today ? (DateOnly?)due : null,
                quotation.Currency,
                Truncate($"{quotation.QuotationNo} numaralı tekliften.{(quotation.Notes is null ? "" : " " + quotation.Notes)}", 1000));

            previousCustomerId = quotation.CustomerId;
            quotation.CustomerId = customerId;
            quotation.Status = QuotationStatus.Accepted;
            quotation.DecidedAt = timeProvider.GetUtcNow().UtcDateTime;
            quotation.DecidedBy = currentUser.UserName;
            quotation.DecisionNote = NullIfBlank(request.Note);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        // Sipariş kendi işleminde açılır; açılamazsa teklif gönderildi durumuna döner.
        var order = await orders.CreateAsync(orderRequest, cancellationToken);
        var accepted = await db.Set<BoyahaneQuotation>().SingleAsync(q => q.Id == id, cancellationToken);
        if (!order.IsSuccess)
        {
            accepted.Status = QuotationStatus.Sent;
            accepted.CustomerId = previousCustomerId;
            accepted.DecidedAt = null;
            accepted.DecidedBy = null;
            accepted.DecisionNote = null;
            await db.SaveChangesAsync(cancellationToken);
            return order.Error!;
        }

        accepted.OrderId = order.Value.Id;
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public Task<Result<BoyahaneQuotationDto>> RejectAsync(int id, BoyahaneQuotationDecisionRequest request, CancellationToken cancellationToken) =>
        DecideAsync(id, QuotationStatus.Rejected, request.Note, cancellationToken);

    public Task<Result<BoyahaneQuotationDto>> ExpireAsync(int id, BoyahaneQuotationDecisionRequest request, CancellationToken cancellationToken) =>
        DecideAsync(id, QuotationStatus.Expired, request.Note, cancellationToken);

    private async Task<Result<BoyahaneQuotationDto>> DecideAsync(int id, QuotationStatus status, string? note, CancellationToken cancellationToken)
    {
        var quotation = await db.Set<BoyahaneQuotation>().SingleOrDefaultAsync(q => q.Id == id, cancellationToken);
        if (quotation is null)
        {
            return NotFound;
        }

        if (quotation.Status != QuotationStatus.Sent)
        {
            return new Error(InvalidState, "Yalnızca gönderilmiş teklif reddedilebilir ya da süresi doldu olarak işaretlenebilir.", ErrorType.Conflict);
        }

        quotation.Status = status;
        quotation.DecidedAt = timeProvider.GetUtcNow().UtcDateTime;
        quotation.DecidedBy = currentUser.UserName;
        quotation.DecisionNote = NullIfBlank(note);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    /// <summary>Gönderilmiş, reddedilmiş ya da süresi dolmuş teklifi düzeltmek için taslağa alır; numara korunur.</summary>
    public async Task<Result<BoyahaneQuotationDto>> ReopenAsync(int id, CancellationToken cancellationToken)
    {
        var quotation = await db.Set<BoyahaneQuotation>().SingleOrDefaultAsync(q => q.Id == id, cancellationToken);
        if (quotation is null)
        {
            return NotFound;
        }

        if (quotation.Status is not (QuotationStatus.Sent or QuotationStatus.Rejected or QuotationStatus.Expired))
        {
            return new Error(InvalidState, "Kabul edilmiş ya da zaten taslak olan teklif taslağa alınamaz.", ErrorType.Conflict);
        }

        quotation.Status = QuotationStatus.Draft;
        quotation.SentAt = null;
        quotation.DecidedAt = null;
        quotation.DecidedBy = null;
        quotation.DecisionNote = null;
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var quotation = await db.Set<BoyahaneQuotation>().Include(q => q.Lines).SingleOrDefaultAsync(q => q.Id == id, cancellationToken);
        if (quotation is null)
        {
            return NotFound;
        }

        if (quotation.Status != QuotationStatus.Draft)
        {
            return new Error(InvalidState, "Yalnızca taslak teklif silinebilir.", ErrorType.Conflict);
        }

        db.Set<BoyahaneQuotationLine>().RemoveRange(quotation.Lines);
        db.Set<BoyahaneQuotation>().Remove(quotation);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<IReadOnlyList<BoyahaneQuotationRecipeOptionDto>> GetRecipeOptionsAsync(BoyahaneQuotationRecipeQuery query, CancellationToken cancellationToken)
    {
        var trials = db.Set<BoyahaneLabTrial>().AsNoTracking()
            .Where(t => t.Decision == LabTrialDecision.Accepted && t.Request.Status != LabRequestStatus.Cancelled);
        if (query.ColorCardId is { } cardId)
        {
            trials = trials.Where(t => t.Request.OrderLine.ColorCardId == cardId);
        }

        if (NullIfBlank(query.Search) is { } term)
        {
            trials = trials.Where(t => t.Request.RequestNo.Contains(term)
                || (t.Request.OrderLine.ColorName != null && t.Request.OrderLine.ColorName.Contains(term))
                || (t.Request.OrderLine.ColorCode != null && t.Request.OrderLine.ColorCode.Contains(term))
                || (t.Request.OrderLine.ColorCard != null && t.Request.OrderLine.ColorCard.Code.Contains(term))
                || (t.Request.OrderLine.Order.Customer != null && t.Request.OrderLine.Order.Customer.Name.Contains(term)));
        }

        return await trials
            .OrderByDescending(t => t.DecisionOn).ThenByDescending(t => t.Id)
            .Take(MaxRecipeOptions)
            .Select(t => new BoyahaneQuotationRecipeOptionDto(
                t.Id,
                t.Request.RequestNo + "/" + t.TrialCode,
                t.Request.OrderLine.ColorCard != null ? t.Request.OrderLine.ColorCard.Code : null,
                t.Request.OrderLine.ColorName,
                t.Request.OrderLine.ColorCode,
                t.Request.OrderLine.Fabric != null ? t.Request.OrderLine.Fabric.FabricCode : t.Request.OrderLine.Yarn != null ? t.Request.OrderLine.Yarn.YarnCode : null,
                t.Request.OrderLine.Order.Customer != null ? t.Request.OrderLine.Order.Customer.Name : null,
                t.DecisionOn))
            .ToListAsync(cancellationToken);
    }

    // ----- Maliyet hesabı -----

    private sealed record FabricInfo(string Code, string Name, decimal RawWidthCm, int RawWeightGsm);

    private sealed record MachineInfo(string Code, decimal CapacityKg, decimal? DefaultLiquorRatio);

    private sealed record ChemicalInfo(int Id, string Code, string Name, DyeChemicalKind Kind);

    private sealed record TrialInfo(string RecipeNo, IReadOnlyList<(ChemicalInfo Chemical, decimal Amount)> Items);

    private sealed record PrintColorInfo(string Name, decimal? PasteKilogramsPerMeter, IReadOnlyList<(ChemicalInfo Chemical, decimal GramsPerKilogram)> Lines);

    /// <summary>Satırların maliyeti; referanslar önceden doğrulanmış olmalıdır.</summary>
    private async Task<List<BoyahaneQuotationLineCostDto>> CalculateLinesAsync(BoyahaneQuotationRequest request, string currency, CancellationToken cancellationToken)
    {
        var settings = await BoyahaneCostSettingsService.LoadAsync(db, cancellationToken);
        var lines = request.Lines;

        var fabricIds = lines.Where(l => l.ProcessType != OrderProcessType.YarnDyeing && l.FabricId is not null).Select(l => l.FabricId!.Value).Distinct().ToList();
        var fabrics = await db.Set<BoyahaneFabric>().AsNoTracking()
            .Where(f => fabricIds.Contains(f.Id))
            .ToDictionaryAsync(f => f.Id, f => new FabricInfo(f.FabricCode, f.Name, f.RawWidthCm, f.RawWeightGsm), cancellationToken);
        var yarnIds = lines.Where(l => l.ProcessType == OrderProcessType.YarnDyeing && l.YarnId is not null).Select(l => l.YarnId!.Value).Distinct().ToList();
        var yarns = await db.Set<BoyahaneYarn>().AsNoTracking()
            .Where(y => yarnIds.Contains(y.Id))
            .ToDictionaryAsync(y => y.Id, y => (y.YarnCode, y.Name), cancellationToken);

        var machineIds = lines.Where(l => l.MachineId is not null).Select(l => l.MachineId!.Value).Distinct().ToList();
        var machines = await db.Set<BoyahaneMachine>().AsNoTracking()
            .Where(m => machineIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, m => new MachineInfo(m.Code, m.CapacityKg, m.DefaultLiquorRatio), cancellationToken);
        var machineOperations = (await db.Set<BoyahaneMachineOperation>().AsNoTracking()
                .Where(o => machineIds.Contains(o.MachineId))
                .Select(o => new { o.MachineId, o.OperationId })
                .ToListAsync(cancellationToken))
            .GroupBy(o => o.MachineId)
            .ToDictionary(g => g.Key, g => (IReadOnlySet<int>)g.Select(o => o.OperationId).ToHashSet());
        var machineRates = await db.Set<BoyahaneMachineCostRate>().AsNoTracking()
            .Where(r => machineIds.Contains(r.MachineId))
            .ToDictionaryAsync(r => r.MachineId, r => r.HourlyCost, cancellationToken);

        // İşlem türlerinin varsayılan rotası; standart süre buradan.
        var routeSteps = (await db.Set<BoyahaneRouteStep>().AsNoTracking()
                .Where(s => s.Route.IsActive && s.Route.IsDefault && !s.Route.IsDeleted)
                .Select(s => new { s.Route.ProcessType, s.OperationId, s.Operation.NeedsMachine, s.Operation.StandardMinutes })
                .ToListAsync(cancellationToken))
            .GroupBy(s => s.ProcessType)
            .ToDictionary(g => g.Key, g => g.Select(s => (s.OperationId, s.NeedsMachine, s.StandardMinutes)).ToList());

        // Reçete: seçilen deneme, yoksa renk kartının son onaylı reçetesi.
        var cardIds = lines.Where(l => UsesRecipe(l.ProcessType) && l.RecipeTrialId is null && l.ColorCardId is not null)
            .Select(l => l.ColorCardId!.Value).Distinct().ToList();
        var cardRecipes = (await db.Set<BoyahaneLabTrial>().AsNoTracking()
                .Where(t => t.Decision == LabTrialDecision.Accepted && t.Request.Status == LabRequestStatus.Approved
                    && t.Request.OrderLine.ColorCardId != null && cardIds.Contains(t.Request.OrderLine.ColorCardId.Value))
                .Select(t => new { t.Id, CardId = t.Request.OrderLine.ColorCardId!.Value, t.DecisionOn })
                .ToListAsync(cancellationToken))
            .GroupBy(t => t.CardId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(t => t.DecisionOn).ThenByDescending(t => t.Id).First().Id);
        var trialIds = lines.Where(l => UsesRecipe(l.ProcessType) && l.RecipeTrialId is not null).Select(l => l.RecipeTrialId!.Value)
            .Concat(cardRecipes.Values).Distinct().ToList();
        var trials = (await db.Set<BoyahaneLabTrial>().AsNoTracking()
                .Where(t => trialIds.Contains(t.Id))
                .Select(t => new
                {
                    t.Id,
                    RecipeNo = t.Request.RequestNo + "/" + t.TrialCode,
                    Items = t.Items.OrderBy(i => i.Id).Select(i => new { i.DyeChemicalId, i.DyeChemical.Code, i.DyeChemical.Name, i.DyeChemical.Kind, i.Amount }).ToList(),
                })
                .ToListAsync(cancellationToken))
            .ToDictionary(t => t.Id, t => new TrialInfo(t.RecipeNo, t.Items.Select(i => (new ChemicalInfo(i.DyeChemicalId, i.Code, i.Name, i.Kind), i.Amount)).ToList()));

        // Baskı: renk yolunun renkleri ve pasta reçeteleri.
        var colorwayIds = lines.Where(l => l.ProcessType == OrderProcessType.Printing && l.ColorwayId is not null).Select(l => l.ColorwayId!.Value).Distinct().ToList();
        var printColors = (await db.Set<BoyahaneColorwayColor>().AsNoTracking()
                .Where(c => colorwayIds.Contains(c.ColorwayId))
                .OrderBy(c => c.Position)
                .Select(c => new
                {
                    c.ColorwayId,
                    c.Name,
                    c.PasteKilogramsPerMeter,
                    Lines = c.Lines.OrderBy(l => l.Id).Select(l => new { l.DyeChemicalId, l.DyeChemical.Code, l.DyeChemical.Name, l.DyeChemical.Kind, l.GramsPerKilogram }).ToList(),
                })
                .ToListAsync(cancellationToken))
            .GroupBy(c => c.ColorwayId)
            .ToDictionary(g => g.Key, g => g.Select(c => new PrintColorInfo(
                c.Name, c.PasteKilogramsPerMeter, c.Lines.Select(l => (new ChemicalInfo(l.DyeChemicalId, l.Code, l.Name, l.Kind), l.GramsPerKilogram)).ToList())).ToList());

        // Boya ve kimyasalın kilogram maliyeti (teklif para biriminde).
        var chemicalIds = trials.Values.SelectMany(t => t.Items.Select(i => i.Chemical.Id))
            .Concat(printColors.Values.SelectMany(c => c.SelectMany(x => x.Lines.Select(l => l.Chemical.Id))))
            .Distinct().ToList();
        var lots = await db.Set<BoyahaneChemicalLot>().AsNoTracking()
            .Where(l => chemicalIds.Contains(l.DyeChemicalId) && l.UnitCost != null)
            .Select(l => new { l.Id, l.DyeChemicalId, l.ReceiptDate, l.RemainingKilograms, UnitCost = l.UnitCost!.Value, l.Currency })
            .ToListAsync(cancellationToken);

        var factors = new Dictionary<string, decimal?>(StringComparer.OrdinalIgnoreCase);
        foreach (var code in lots.Select(l => l.Currency).Append(settings.Currency).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            factors[code] = await exchangeRates.ConvertAsync(1m, code, currency, request.QuotationDate, ExchangeRateType.ForexSelling, cancellationToken);
        }

        var chemicalCosts = new Dictionary<int, decimal?>();
        foreach (var group in lots.GroupBy(l => l.DyeChemicalId))
        {
            var held = group.Where(l => l.RemainingKilograms > 0 && factors[l.Currency] is not null).ToList();
            if (settings.ChemicalCostMethod == ChemicalCostMethod.WeightedAverage && held.Count > 0)
            {
                chemicalCosts[group.Key] = held.Sum(l => l.RemainingKilograms * l.UnitCost * factors[l.Currency]!.Value) / held.Sum(l => l.RemainingKilograms);
                continue;
            }

            var last = group.OrderByDescending(l => l.ReceiptDate).ThenByDescending(l => l.Id).First();
            chemicalCosts[group.Key] = factors[last.Currency] is { } factor ? last.UnitCost * factor : null;
        }

        var missingRate = lots.Select(l => l.Currency).Distinct(StringComparer.OrdinalIgnoreCase).Where(c => factors[c] is null).ToList();
        var settingsFactor = factors[settings.Currency];

        return lines.Select(item =>
        {
            var warnings = new List<string>();
            var yarn = item.ProcessType == OrderProcessType.YarnDyeing;
            var fabric = !yarn && item.FabricId is { } fabricId ? fabrics.GetValueOrDefault(fabricId) : null;
            var yarnInfo = yarn && item.YarnId is { } yarnId && yarns.TryGetValue(yarnId, out var y) ? y : default;
            decimal? fabricKilogramsPerMeter = fabric is not null && fabric.RawWidthCm > 0 && fabric.RawWeightGsm > 0
                ? fabric.RawWidthCm * fabric.RawWeightGsm / 100_000m
                : null;
            decimal? kilogramsPerUnit = item.Unit == StockUnit.Kilogram ? 1 : fabricKilogramsPerMeter;
            if (kilogramsPerUnit is null)
            {
                warnings.Add("Kumaş kartında ham en / gramaj yok; metre başına kilo hesaplanamadığı için reçete, makine ve işçilik maliyeti eklenmedi.");
            }

            var machine = item.MachineId is { } machineId ? machines.GetValueOrDefault(machineId) : null;
            var batchKilograms = item.BatchKilograms ?? machine?.CapacityKg ?? settings.DefaultBatchKilograms;

            // Boya ve kimyasal: reçete parti kilosu ve flotteyle ölçeklenir, kiloya bölünür.
            var chemicals = new List<BoyahaneQuotationChemicalCostDto>();
            var usesRecipe = UsesRecipe(item.ProcessType);
            int? trialId = null;
            if (usesRecipe)
            {
                trialId = item.RecipeTrialId;
                if (trialId is null && item.ColorCardId is { } card && cardRecipes.TryGetValue(card, out var cardTrial))
                {
                    trialId = cardTrial;
                }
            }

            var trial = trialId is { } tid ? trials.GetValueOrDefault(tid) : null;
            decimal? liquorRatio = usesRecipe ? item.LiquorRatio ?? machine?.DefaultLiquorRatio ?? settings.DefaultLiquorRatio : null;
            decimal chemicalCost = 0;
            if (usesRecipe && trial is null)
            {
                warnings.Add("Reçete seçilmedi ve renk kartının onaylı reçetesi yok; boya ve kimyasal maliyeti eklenmedi.");
            }

            foreach (var (chemical, amount) in trial?.Items ?? [])
            {
                var perKilogram = RecipeScaling.RequiredKilograms(chemical.Kind, amount, batchKilograms, liquorRatio!.Value) / batchKilograms;
                var perUnit = perKilogram * (kilogramsPerUnit ?? 0);
                var costPerKilogram = chemicalCosts.GetValueOrDefault(chemical.Id);
                if (costPerKilogram is null)
                {
                    warnings.Add($"{chemical.Code}: maliyeti girilmiş lot yok.");
                }

                var cost = perUnit * (costPerKilogram ?? 0);
                chemicalCost += cost;
                chemicals.Add(new BoyahaneQuotationChemicalCostDto(
                    chemical.Id, chemical.Code, chemical.Name, chemical.Kind, false, amount, Round4(perUnit), costPerKilogram is { } c ? (decimal?)Round4(c) : null, Round4(cost)));
            }

            // Baskı pastası: metre başına pasta × pasta reçetesi (g/kg); kilo satırında kumaşın metre kilosuna bölünür.
            decimal printCost = 0;
            if (item.ProcessType == OrderProcessType.Printing)
            {
                decimal? metersPerUnit = null;
                if (item.Unit == StockUnit.Meter)
                {
                    metersPerUnit = 1;
                }
                else if (fabricKilogramsPerMeter is { } kgm)
                {
                    metersPerUnit = 1 / kgm;
                }
                var colors = item.ColorwayId is { } colorwayId ? printColors.GetValueOrDefault(colorwayId) : null;
                if (colors is null)
                {
                    warnings.Add("Renk yolu seçilmedi ya da renk yolunda renk yok; baskı pastası maliyeti eklenmedi.");
                }
                else if (metersPerUnit is null)
                {
                    warnings.Add("Kumaş kartında ham en / gramaj yok; kilo başına baskı pastası hesaplanamadı.");
                }
                else
                {
                    foreach (var color in colors.Where(c => c.PasteKilogramsPerMeter is null))
                    {
                        warnings.Add($"{color.Name}: metre başına pasta girilmemiş.");
                    }

                    var usage = colors.Where(c => c.PasteKilogramsPerMeter is not null)
                        .SelectMany(c => c.Lines.Select(l => (l.Chemical, l.GramsPerKilogram,
                            KilogramsPerMeter: RecipeScaling.PasteChemicalKilograms(RecipeScaling.PasteKilograms(1000, c.PasteKilogramsPerMeter!.Value), l.GramsPerKilogram) / 1000)))
                        .GroupBy(x => x.Chemical.Id);
                    foreach (var group in usage)
                    {
                        var chemical = group.First().Chemical;
                        var perUnit = group.Sum(x => x.KilogramsPerMeter) * metersPerUnit.Value;
                        var costPerKilogram = chemicalCosts.GetValueOrDefault(chemical.Id);
                        if (costPerKilogram is null)
                        {
                            warnings.Add($"{chemical.Code}: maliyeti girilmiş lot yok.");
                        }

                        var cost = perUnit * (costPerKilogram ?? 0);
                        printCost += cost;
                        chemicals.Add(new BoyahaneQuotationChemicalCostDto(
                            chemical.Id, chemical.Code, chemical.Name, chemical.Kind, true, group.Sum(x => x.GramsPerKilogram), Round4(perUnit),
                            costPerKilogram is { } c ? (decimal?)Round4(c) : null, Round4(cost)));
                    }
                }
            }

            // Süre: elle, rotanın standart süresi (makinede yapılan adımlar) ya da işlem türünün varsayılanı.
            decimal minutes;
            ProcessMinutesSource source;
            var operations = item.MachineId is { } mid && machineOperations.TryGetValue(mid, out var ops) && ops.Count > 0 ? ops : null;
            if (item.ProcessMinutes is { } manual)
            {
                (minutes, source) = (manual, ProcessMinutesSource.Manual);
            }
            else if (routeSteps.TryGetValue(item.ProcessType, out var steps) && BoyahaneMachineFit.StandardMinutes(steps, operations) is { } standard)
            {
                (minutes, source) = (standard, ProcessMinutesSource.Route);
            }
            else
            {
                (minutes, source) = (settings.DefaultMinutes(item.ProcessType), ProcessMinutesSource.Default);
            }

            if (settingsFactor is null)
            {
                warnings.Add($"{settings.Currency} → {currency} kuru bulunamadı; makine ve işçilik maliyeti eklenmedi.");
            }

            var factor = settingsFactor ?? 0;
            var hourlyCost = (item.MachineId is { } rateMachine && machineRates.TryGetValue(rateMachine, out var rate) ? rate : settings.DefaultMachineHourlyCost) * factor;
            var laborPerMinute = settings.LaborCostPerMinute * factor;
            var machineCost = minutes / 60 * hourlyCost / batchKilograms * (kilogramsPerUnit ?? 0);
            var laborCost = minutes * laborPerMinute / batchKilograms * (kilogramsPerUnit ?? 0);

            var direct = chemicalCost + printCost + machineCost + laborCost;
            var firePercent = item.FirePercent ?? settings.FirePercent;
            var fireCost = direct * firePercent / 100;
            var overheadPercent = item.OverheadPercent ?? settings.OverheadPercent;
            var overheadCost = (direct + fireCost) * overheadPercent / 100;
            var unitCost = Round4(direct + fireCost + overheadCost);
            var margin = item.MarginPercent ?? settings.MarginPercent;
            var suggested = Round4(unitCost * (1 + margin / 100));
            var price = item.UnitPrice is { } manualPrice ? Round4(manualPrice) : suggested;
            decimal? marginPercent = margin;
            if (item.UnitPrice is not null)
            {
                marginPercent = unitCost > 0 ? Math.Clamp(Math.Round((price / unitCost - 1) * 100, 2), -9_999_999m, 9_999_999m) : null;
            }

            if (missingRate.Count > 0 && chemicals.Count > 0)
            {
                warnings.Add($"Kuru bulunamayan lot para birimi: {string.Join(", ", missingRate)}.");
            }

            return new BoyahaneQuotationLineCostDto(
                item.ProcessType,
                fabric?.Code ?? yarnInfo.YarnCode ?? "",
                fabric?.Name ?? yarnInfo.Name ?? "",
                NullIfBlank(item.ColorName),
                item.Quantity,
                item.Unit,
                trialId,
                trial?.RecipeNo,
                usesRecipe && item.RecipeTrialId is null && trial is not null,
                item.MachineId,
                machine?.Code,
                batchKilograms,
                liquorRatio,
                minutes,
                source,
                item.Unit == StockUnit.Meter ? (fabricKilogramsPerMeter is { } k ? (decimal?)Round4(k) : null) : null,
                Round4(hourlyCost),
                Round4(laborPerMinute),
                Round4(chemicalCost),
                Round4(printCost),
                Round4(machineCost),
                Round4(laborCost),
                firePercent,
                Round4(fireCost),
                overheadPercent,
                Round4(overheadCost),
                unitCost,
                marginPercent,
                suggested,
                price,
                item.UnitPrice is not null,
                InvoiceAmounts.Line(item.Quantity, price),
                warnings.Distinct().ToList(),
                chemicals);
        }).ToList();
    }

    private static bool UsesRecipe(OrderProcessType process) =>
        process is OrderProcessType.FabricDyeing or OrderProcessType.YarnDyeing;

    private static decimal Round4(decimal value) => Math.Round(value, 4, MidpointRounding.AwayFromZero);

    // ----- Kayıt -----

    private async Task<string> ResolveCurrencyAsync(BoyahaneQuotationRequest request, CancellationToken cancellationToken)
    {
        if (NullIfBlank(request.Currency) is { } code)
        {
            return code.ToUpperInvariant();
        }

        return request.CustomerId is { } id
            ? await db.Set<BoyahaneCustomer>().Where(c => c.Id == id).Select(c => c.Currency).SingleAsync(cancellationToken)
            : "TRY";
    }

    private async Task<DocumentLanguage> ResolveLanguageAsync(BoyahaneQuotationRequest request, CancellationToken cancellationToken)
    {
        if (request.DocumentLanguage is { } language)
        {
            return language;
        }

        return request.CustomerId is { } id
            ? await db.Set<BoyahaneCustomer>().Where(c => c.Id == id).Select(c => c.DocumentLanguage).SingleAsync(cancellationToken)
            : DocumentLanguage.Turkish;
    }

    private static void Apply(BoyahaneQuotation quotation, BoyahaneQuotationRequest request, string currency, DocumentLanguage language)
    {
        quotation.QuotationDate = request.QuotationDate;
        quotation.ValidUntil = request.ValidUntil;
        quotation.CustomerId = request.CustomerId;
        quotation.ProspectName = request.CustomerId is null ? NullIfBlank(request.ProspectName) : null;
        quotation.Ownership = request.Ownership;
        quotation.DueDate = request.DueDate;
        quotation.Currency = currency;
        quotation.DocumentLanguage = language;
        quotation.Notes = NullIfBlank(request.Notes);
    }

    private static void ReplaceLines(BoyahaneQuotation quotation, BoyahaneQuotationRequest request, IReadOnlyList<BoyahaneQuotationLineCostDto> costs, ArchiveInfo archive)
    {
        for (var i = 0; i < request.Lines.Count; i++)
        {
            var item = request.Lines[i];
            var cost = costs[i];
            var yarn = item.ProcessType == OrderProcessType.YarnDyeing;
            var printing = item.ProcessType == OrderProcessType.Printing;
            var cardId = UsesRecipe(item.ProcessType) ? item.ColorCardId : null;
            var patternId = printing ? item.PatternId : null;
            var colorwayId = printing && patternId is not null ? item.ColorwayId : null;

            var line = new BoyahaneQuotationLine
            {
                ProcessType = item.ProcessType,
                FabricId = yarn ? null : item.FabricId,
                YarnId = yarn ? item.YarnId : null,
                ColorCardId = cardId,
                PatternId = patternId,
                ColorwayId = colorwayId,
                Quantity = item.Quantity,
                Unit = item.Unit,
                RecipeTrialId = cost.RecipeTrialId,
                MachineId = item.MachineId,
                BatchKilogramsInput = item.BatchKilograms,
                LiquorRatioInput = UsesRecipe(item.ProcessType) ? item.LiquorRatio : null,
                ProcessMinutesInput = item.ProcessMinutes,
                FirePercentInput = item.FirePercent,
                OverheadPercentInput = item.OverheadPercent,
                MarginPercentInput = item.MarginPercent,
                UnitPriceInput = item.UnitPrice,
                BatchKilograms = cost.BatchKilograms,
                LiquorRatio = cost.LiquorRatio,
                ProcessMinutes = cost.ProcessMinutes,
                MinutesSource = cost.MinutesSource,
                KilogramsPerMeter = cost.KilogramsPerMeter,
                ChemicalCost = cost.ChemicalCost,
                PrintCost = cost.PrintCost,
                MachineCost = cost.MachineCost,
                LaborCost = cost.LaborCost,
                FirePercent = cost.FirePercent,
                FireCost = cost.FireCost,
                OverheadPercent = cost.OverheadPercent,
                OverheadCost = cost.OverheadCost,
                UnitCost = cost.UnitCost,
                MarginPercent = cost.MarginPercent,
                UnitPrice = cost.UnitPrice,
                CostWarning = cost.Warnings.Count == 0 ? null : Truncate(string.Join(" ", cost.Warnings), 500),
                Notes = NullIfBlank(item.Notes),
            };

            // Renk kartı ya da desen seçildiyse ad ve kod karttan gelir (sipariş satırıyla aynı kural).
            if (cardId is { } card && archive.Cards.TryGetValue(card, out var cardInfo))
            {
                line.ColorName = Truncate(cardInfo.Name, 100);
                line.ColorCode = Truncate(cardInfo.ColorCode, 50);
            }
            else if (patternId is { } pattern && archive.Patterns.TryGetValue(pattern, out var patternInfo))
            {
                var colorway = colorwayId is { } cw ? archive.Colorways.GetValueOrDefault(cw) : null;
                line.ColorName = Truncate(colorway is null ? patternInfo.Name : $"{patternInfo.Name} / {colorway.Name}", 100);
                line.ColorCode = Truncate(colorway is null ? patternInfo.ColorCode : $"{patternInfo.ColorCode}/{colorway.Code}", 50);
            }
            else
            {
                line.ColorName = NullIfBlank(item.ColorName);
                line.ColorCode = NullIfBlank(item.ColorCode);
            }

            quotation.Lines.Add(line);
        }
    }

    private static BoyahaneQuotationRequest ToRequest(BoyahaneQuotation quotation) => new(
        quotation.QuotationDate,
        quotation.ValidUntil,
        quotation.Lines.OrderBy(l => l.Id).Select(l => new BoyahaneQuotationLineRequest(
            l.ProcessType,
            l.Quantity,
            l.Unit,
            l.FabricId,
            l.YarnId,
            l.ColorName,
            l.ColorCode,
            l.ColorCardId,
            l.PatternId,
            l.ColorwayId,
            l.RecipeTrialId,
            l.MachineId,
            l.BatchKilogramsInput,
            l.LiquorRatioInput,
            l.ProcessMinutesInput,
            l.FirePercentInput,
            l.OverheadPercentInput,
            l.MarginPercentInput,
            l.UnitPriceInput,
            l.Notes)).ToList(),
        quotation.CustomerId,
        quotation.ProspectName,
        quotation.Ownership,
        quotation.DueDate,
        quotation.Currency,
        quotation.DocumentLanguage,
        quotation.Notes);

    private sealed record CardInfo(string Code, string Name, string ColorCode, int? CustomerId, bool IsActive);

    private sealed record ColorwayInfo(int PatternId, string Code, string Name, bool IsActive);

    private sealed record ArchiveInfo(
        IReadOnlyDictionary<int, CardInfo> Cards,
        IReadOnlyDictionary<int, CardInfo> Patterns,
        IReadOnlyDictionary<int, ColorwayInfo> Colorways);

    private async Task<ArchiveInfo> LoadArchiveAsync(IReadOnlyList<BoyahaneQuotationLineRequest> lines, CancellationToken cancellationToken)
    {
        var cardIds = lines.Where(l => l.ColorCardId is not null).Select(l => l.ColorCardId!.Value).Distinct().ToList();
        var patternIds = lines.Where(l => l.PatternId is not null).Select(l => l.PatternId!.Value).Distinct().ToList();
        var colorwayIds = lines.Where(l => l.ColorwayId is not null).Select(l => l.ColorwayId!.Value).Distinct().ToList();

        var cards = await db.Set<BoyahaneColorCard>().AsNoTracking()
            .Where(c => cardIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => new CardInfo(c.Code, c.Name, c.CustomerColorCode ?? c.Code, c.CustomerId, c.IsActive), cancellationToken);
        var patterns = await db.Set<BoyahanePattern>().AsNoTracking()
            .Where(p => patternIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => new CardInfo(p.Code, p.Name, p.Code, p.CustomerId, p.IsActive), cancellationToken);
        var colorways = await db.Set<BoyahanePatternColorway>().AsNoTracking()
            .Where(c => colorwayIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => new ColorwayInfo(c.PatternId, c.Code, c.Name, c.IsActive), cancellationToken);
        return new ArchiveInfo(cards, patterns, colorways);
    }

    /// <summary>
    /// Müşteri, para birimi, kumaş / iplik kartı ve makine seçilebilir olmalı; renk kartı ve desen genel ya da teklifin müşterisine
    /// ait olmalı (aday müşteride yalnızca genel kartlar); renk yolu desene ait olmalı; reçete müşterinin kabul ettiği deneme olmalı.
    /// Teklifte zaten seçili olan kayıt pasif olsa da kabul edilir.
    /// </summary>
    private async Task<Error?> ValidateReferencesAsync(BoyahaneQuotationRequest request, BoyahaneQuotation? existing, CancellationToken cancellationToken)
    {
        if (await ReferenceValidation.RequireSelectableAsync(
                db.Set<BoyahaneCustomer>(), request.CustomerId, existing?.CustomerId, "customerId", "Müşteri", cancellationToken) is { } customerError)
        {
            return customerError;
        }

        if (await CurrencyRules.RequireActiveAsync(db, request.Currency, existing?.Currency, "currency", cancellationToken) is { } currencyError)
        {
            return currencyError;
        }

        IEnumerable<BoyahaneQuotationLine> current = existing?.Lines ?? Enumerable.Empty<BoyahaneQuotationLine>();
        var fabricIds = request.Lines.Where(l => l.ProcessType != OrderProcessType.YarnDyeing && l.FabricId is not null).Select(l => l.FabricId!.Value).Distinct().ToList();
        if (await ReferenceValidation.RequireSelectableAsync(
                db.Set<BoyahaneFabric>(), fabricIds, current.Where(l => l.FabricId is not null).Select(l => l.FabricId!.Value).ToHashSet(), "lines", "Kumaş kartı", cancellationToken) is { } fabricError)
        {
            return fabricError;
        }

        var yarnIds = request.Lines.Where(l => l.ProcessType == OrderProcessType.YarnDyeing && l.YarnId is not null).Select(l => l.YarnId!.Value).Distinct().ToList();
        if (await ReferenceValidation.RequireSelectableAsync(
                db.Set<BoyahaneYarn>(), yarnIds, current.Where(l => l.YarnId is not null).Select(l => l.YarnId!.Value).ToHashSet(), "lines", "İplik kartı", cancellationToken) is { } yarnError)
        {
            return yarnError;
        }

        var machineIds = request.Lines.Where(l => l.MachineId is not null).Select(l => l.MachineId!.Value).Distinct().ToList();
        if (await ReferenceValidation.RequireSelectableAsync(
                db.Set<BoyahaneMachine>(), machineIds, current.Where(l => l.MachineId is not null).Select(l => l.MachineId!.Value).ToHashSet(), "lines", "Makine", cancellationToken) is { } machineError)
        {
            return machineError;
        }

        var archive = await LoadArchiveAsync(request.Lines, cancellationToken);
        var currentCards = current.Where(l => l.ColorCardId is not null).Select(l => l.ColorCardId!.Value).ToHashSet();
        var currentPatterns = current.Where(l => l.PatternId is not null).Select(l => l.PatternId!.Value).ToHashSet();
        var currentColorways = current.Where(l => l.ColorwayId is not null).Select(l => l.ColorwayId!.Value).ToHashSet();
        foreach (var item in request.Lines)
        {
            if (item.ColorCardId is { } cardId)
            {
                if (!archive.Cards.TryGetValue(cardId, out var card))
                {
                    return ReferenceValidation.InvalidReference("lines", "Renk kartı bulunamadı.");
                }

                if (!currentCards.Contains(cardId) && !card.IsActive)
                {
                    return ReferenceValidation.InvalidReference("lines", $"{card.Code} renk kartı pasif durumda.");
                }

                if (card.CustomerId is not null && card.CustomerId != request.CustomerId)
                {
                    return ReferenceValidation.InvalidReference("lines", $"{card.Code} renk kartı başka bir müşteriye ait.");
                }
            }

            if (item.PatternId is { } patternId)
            {
                if (!archive.Patterns.TryGetValue(patternId, out var pattern))
                {
                    return ReferenceValidation.InvalidReference("lines", "Desen kartı bulunamadı.");
                }

                if (!currentPatterns.Contains(patternId) && !pattern.IsActive)
                {
                    return ReferenceValidation.InvalidReference("lines", $"{pattern.Code} desen kartı pasif durumda.");
                }

                if (pattern.CustomerId is not null && pattern.CustomerId != request.CustomerId)
                {
                    return ReferenceValidation.InvalidReference("lines", $"{pattern.Code} desen kartı başka bir müşteriye ait.");
                }
            }

            if (item.ColorwayId is { } colorwayId)
            {
                if (!archive.Colorways.TryGetValue(colorwayId, out var colorway) || colorway.PatternId != item.PatternId)
                {
                    return ReferenceValidation.InvalidReference("lines", "Renk yolu seçilen desene ait değil.");
                }

                if (!currentColorways.Contains(colorwayId) && !colorway.IsActive)
                {
                    return ReferenceValidation.InvalidReference("lines", $"{colorway.Code} renk yolu pasif durumda.");
                }
            }
        }

        var trialIds = request.Lines.Where(l => l.RecipeTrialId is not null).Select(l => l.RecipeTrialId!.Value).Distinct().ToList();
        if (trialIds.Count > 0)
        {
            var accepted = await db.Set<BoyahaneLabTrial>().AsNoTracking()
                .CountAsync(t => trialIds.Contains(t.Id) && t.Decision == LabTrialDecision.Accepted, cancellationToken);
            if (accepted != trialIds.Count)
            {
                return ReferenceValidation.InvalidReference("lines", "Seçilen reçete bulunamadı ya da müşterinin kabul ettiği bir deneme değil.");
            }
        }

        return null;
    }

    private static string Truncate(string value, int maxLength) => value.Length <= maxLength ? value : value[..maxLength];
}
