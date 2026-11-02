using System.Globalization;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.Finance;
using ERP.Core.Application.Features.MasterData;
using ERP.Core.Domain.Enums;
using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Konfeksiyon.Application.Features.Pricing;
using ERP.Konfeksiyon.Application.Features.Sales;
using ERP.Konfeksiyon.Application.Features.Subcontracting;
using ERP.Konfeksiyon.Domain.Entities;
using ERP.Konfeksiyon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Konfeksiyon.Application.Features.Quotations;

public interface IQuotationService
{
    Task<PagedResult<QuotationListItemDto>> GetListAsync(QuotationListQuery query, CancellationToken cancellationToken);
    Task<Result<QuotationDto>> GetAsync(int id, CancellationToken cancellationToken);

    /// <summary>Basılı teklif için teklif, satıcı (fabrika) ve müşteri / aday bilgileri.</summary>
    Task<Result<QuotationDocumentDto>> GetDocumentAsync(int id, CancellationToken cancellationToken);

    /// <summary>Bir satırın maliyetini kaydetmeden hesaplar (yeni satır, "ya olursa" denemesi).</summary>
    Task<Result<QuotationLineDto>> PreviewLineAsync(QuotationLinePreviewRequest request, CancellationToken cancellationToken);

    Task<Result<QuotationDto>> CreateAsync(QuotationRequest request, CancellationToken cancellationToken);

    /// <summary>Taslak teklifi günceller; satırların maliyetleri güncel reçete, rota, fason fiyatı ve ayarlarla yeniden hesaplanır.</summary>
    Task<Result<QuotationDto>> UpdateAsync(int id, QuotationRequest request, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken);

    /// <summary>Teklifi müşteriye gönderildi olarak işaretler; içerik kilitlenir.</summary>
    Task<Result<QuotationDto>> SendAsync(int id, QuotationActionRequest request, CancellationToken cancellationToken);

    Task<Result<QuotationDto>> RejectAsync(int id, QuotationActionRequest request, CancellationToken cancellationToken);
    Task<Result<QuotationDto>> ExpireAsync(int id, QuotationActionRequest request, CancellationToken cancellationToken);

    /// <summary>Gönderilmiş, reddedilmiş ya da süresi dolmuş teklifi düzeltmek için taslağa alır.</summary>
    Task<Result<QuotationDto>> ReopenAsync(int id, QuotationActionRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Kabul: verilen renk / beden adetleri ve teklif fiyatlarıyla taslak müşteri siparişi açılır, teklif kabul edildi
    /// olur. Sipariş kurallarına (müşteri, fiyat yetkisi, fiyat listesi altı) uymayan kabul reddedilir.
    /// </summary>
    Task<Result<QuotationDto>> AcceptAsync(int id, QuotationAcceptRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Teklifler ve ön maliyet. Satır başına parça maliyeti:
/// malzeme (reçetenin genel satırları × stok kartı birim maliyeti, elle değiştirilebilir) + işçilik (dakika × dakika
/// maliyeti; dakika etkin rotanın fason olmayan adımlarının SAM toplamı ya da elle) + enerji (dakika × enerji maliyeti)
/// + fason (rotadaki fason adımların fason fiyatları ya da elle) = doğrudan maliyet; fire = doğrudan × fire %;
/// genel gider = (doğrudan + fire) × genel gider %; birim maliyet = doğrudan + fire + genel gider; birim fiyat =
/// birim maliyet × (1 + marj %) ya da elle (marj fiyattan geri hesaplanır). Tutarlar teklifin para birimine
/// (fiyat çevrimindeki kurla) çevrilir.
/// </summary>
public sealed class QuotationService(
    IKonfeksiyonDbContext db,
    IDocumentNumberGenerator numbers,
    ICurrentUser currentUser,
    IExchangeRateService exchangeRates,
    ISalesOrderService salesOrders,
    TimeProvider timeProvider) : IQuotationService
{
    private const string Series = "Quotation";
    private const string NumberPrefix = "TK";
    private const string SystemUser = "system";
    private const char WarningSeparator = '\n';

    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Teklif bulunamadı.", ErrorType.NotFound);

    private static readonly Error ConcurrencyConflict = new(
        ErrorCodes.ConcurrencyConflict,
        "Teklif siz açtıktan sonra değişti. Sayfayı yenileyip tekrar deneyin.",
        ErrorType.Conflict);

    private static readonly IReadOnlyDictionary<QuotationStatus, string> StatusLabels = new Dictionary<QuotationStatus, string>
    {
        [QuotationStatus.Draft] = "Taslak",
        [QuotationStatus.Sent] = "Gönderildi",
        [QuotationStatus.Accepted] = "Kabul edildi",
        [QuotationStatus.Rejected] = "Reddedildi",
        [QuotationStatus.Expired] = "Süresi doldu",
    };

    private DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;

    private DateOnly Today() => DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);

    private string UserName => currentUser.UserName ?? SystemUser;

    // ------------------------------------------------------------------ Okuma

    public Task<PagedResult<QuotationListItemDto>> GetListAsync(QuotationListQuery query, CancellationToken cancellationToken)
    {
        var today = Today();
        // Sonradan silinen müşteri de adıyla görünür; teklifin kendisi silinmemiş olmalıdır.
        var quotations = db.Quotations.AsNoTracking().IgnoreQueryFilters().Where(q => !q.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            quotations = quotations.Where(q => q.QuotationNo.Contains(search)
                || (q.Customer != null && q.Customer.Name.Contains(search))
                || (q.ProspectName != null && q.ProspectName.Contains(search))
                || (q.Notes != null && q.Notes.Contains(search)));
        }

        if (query.Status is { } status)
        {
            quotations = quotations.Where(q => q.Status == status);
        }

        if (query.CustomerId is { } customerId)
        {
            quotations = quotations.Where(q => q.CustomerId == customerId);
        }

        return quotations
            .OrderByDescending(q => q.QuotationDate).ThenByDescending(q => q.Id)
            .Select(q => new QuotationListItemDto(
                q.Id,
                q.QuotationNo,
                q.CustomerId,
                q.Customer != null ? q.Customer.Name : q.ProspectName ?? "",
                q.CustomerId == null,
                q.QuotationDate,
                q.ValidUntil,
                q.Currency,
                q.Status,
                q.Lines.Count,
                q.Lines.Sum(l => (int?)l.Quantity) ?? 0,
                q.Lines.Sum(l => (decimal?)(l.Quantity * l.UnitPrice)) ?? 0,
                q.Status == QuotationStatus.Sent && q.ValidUntil < today,
                q.SalesOrderId,
                q.SalesOrder != null ? q.SalesOrder.OrderNo : null))
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<Result<QuotationDto>> GetAsync(int id, CancellationToken cancellationToken)
    {
        var quotation = await db.Quotations
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(q => q.Customer)
            .Include(q => q.SalesOrder)
            .Include(q => q.Lines).ThenInclude(l => l.Product).ThenInclude(p => p!.Model)
            .Include(q => q.Lines).ThenInclude(l => l.Materials).ThenInclude(m => m.StockItem)
            .SingleOrDefaultAsync(q => q.Id == id && !q.IsDeleted, cancellationToken);
        return quotation is null ? NotFound : ToDto(quotation);
    }

    public async Task<Result<QuotationDocumentDto>> GetDocumentAsync(int id, CancellationToken cancellationToken)
    {
        var quotation = await GetAsync(id, cancellationToken);
        if (!quotation.IsSuccess)
        {
            return quotation.Error!;
        }

        var factory = await db.Factories.AsNoTracking()
            .Where(f => f.IsActive)
            .OrderBy(f => f.Id)
            .Select(f => new { f.Name, f.AddressLine, f.District, f.City, f.Phone, f.Email })
            .FirstOrDefaultAsync(cancellationToken);
        var seller = factory is null
            ? null
            : new ProformaPartyDto(
                factory.Name, null, null, AddressText(factory.AddressLine, factory.District, factory.City), factory.Phone, factory.Email);

        var value = quotation.Value;
        ProformaPartyDto customer;
        string? address = null;
        if (value.CustomerId is { } customerId)
        {
            customer = await db.Customers.AsNoTracking().IgnoreQueryFilters()
                .Where(c => c.Id == customerId)
                .Select(c => new ProformaPartyDto(c.Name, c.TaxOffice, c.TaxNumber, null, c.Phone, c.Email))
                .SingleAsync(cancellationToken);
            var invoice = await db.CustomerAddresses.AsNoTracking()
                .Where(a => a.CustomerId == customerId && a.IsDefaultInvoice)
                .Select(a => new { a.AddressLine, a.District, a.City })
                .FirstOrDefaultAsync(cancellationToken);
            address = invoice is null ? null : AddressText(invoice.AddressLine, invoice.District, invoice.City);
        }
        else
        {
            customer = new ProformaPartyDto(value.ProspectName ?? "", null, null, null, null, null);
        }

        return new QuotationDocumentDto(value, seller, customer, address, UtcNow);
    }

    // ------------------------------------------------------------------ Maliyet

    public async Task<Result<QuotationLineDto>> PreviewLineAsync(QuotationLinePreviewRequest request, CancellationToken cancellationToken)
    {
        var currency = string.IsNullOrWhiteSpace(request.Currency)
            ? ERP.Core.Domain.Entities.Currency.LocalCode
            : request.Currency.Trim().ToUpperInvariant();
        var context = await CostContext.CreateAsync(db, exchangeRates, currency, request.Date ?? Today(), cancellationToken);
        var line = new QuotationLine { LineNo = 1 };
        if (await ComputeLineAsync(line, request.Line, context, cancellationToken) is { } error)
        {
            return error;
        }

        var products = await LoadProductInfoAsync(
            line.ProductId is { } productId ? new[] { productId } : Array.Empty<int>(), cancellationToken);
        var items = await LoadStockItemCodesAsync(line.Materials.Where(m => m.StockItemId is not null).Select(m => m.StockItemId!.Value).ToList(), cancellationToken);
        return LineDto(line, products, items);
    }

    /// <summary>Teklifin dakika maliyetleri ve para birimi çevrimleri; bir kayıt / önizleme boyunca önbelleklenir.</summary>
    private sealed class CostContext
    {
        private readonly Dictionary<string, decimal?> _rates = new(StringComparer.Ordinal);
        private readonly IExchangeRateService _exchangeRates;

        private CostContext(IExchangeRateService exchangeRates, string currency, DateOnly date, CostSetting setting)
        {
            _exchangeRates = exchangeRates;
            Currency = currency;
            Date = date;
            Setting = setting;
        }

        public string Currency { get; }
        public DateOnly Date { get; }
        public CostSetting Setting { get; }
        public decimal LaborRate { get; private set; }
        public decimal EnergyRate { get; private set; }

        /// <summary>Dakika maliyetlerinin teklif para birimine çevrilemediği durumda uyarı.</summary>
        public string? RateWarning { get; private set; }

        public static async Task<CostContext> CreateAsync(
            IKonfeksiyonDbContext db, IExchangeRateService exchangeRates, string currency, DateOnly date,
            CancellationToken cancellationToken)
        {
            var setting = await CostSettingService.LoadAsync(db, cancellationToken);
            var context = new CostContext(exchangeRates, currency, date, setting);
            var rate = await context.RateAsync(setting.Currency, cancellationToken);
            if (rate is null)
            {
                context.RateWarning =
                    $"{setting.Currency} → {currency} kuru bulunamadı; işçilik ve enerji sıfır hesaplandı (Döviz Kurları ekranından girin).";
            }

            context.LaborRate = decimal.Round(setting.LaborCostPerMinute * (rate ?? 0), 4);
            context.EnergyRate = decimal.Round(setting.EnergyCostPerMinute * (rate ?? 0), 4);
            return context;
        }

        /// <summary>1 <paramref name="from"/> = X teklif para birimi; kur yoksa null.</summary>
        public async Task<decimal?> RateAsync(string from, CancellationToken cancellationToken)
        {
            if (string.Equals(from, Currency, StringComparison.OrdinalIgnoreCase))
            {
                return 1m;
            }

            if (!_rates.TryGetValue(from, out var rate))
            {
                rate = await _exchangeRates.ConvertAsync(1m, from, Currency, Date, PricingService.ConversionRate, cancellationToken);
                _rates[from] = rate;
            }

            return rate;
        }
    }

    /// <summary>
    /// Satırın girdilerini isteğe göre yazar, kaynakları (reçete, stok maliyeti, rota, fason fiyatı) okuyup maliyet
    /// dökümünü hesaplar. Eksikler satırın uyarılarına yazılır; yalnızca geçersiz referans hata döner.
    /// </summary>
    private async Task<Error?> ComputeLineAsync(
        QuotationLine line, QuotationLineRequest request, CostContext context, CancellationToken cancellationToken)
    {
        var warnings = new List<string>();
        if (context.RateWarning is not null)
        {
            warnings.Add(context.RateWarning);
        }

        int? modelId = null;
        if (request.ProductId is { } productId)
        {
            modelId = await db.Products.AsNoTracking()
                .Where(p => p.Id == productId)
                .Select(p => (int?)p.ModelId)
                .SingleOrDefaultAsync(cancellationToken);
            if (modelId is null)
            {
                return ReferenceValidation.InvalidReference("lines", "Satırdaki ürün bulunamadı.");
            }
        }

        line.ProductId = request.ProductId;
        line.Description = NullIfBlank(request.Description);
        line.ModelReference = NullIfBlank(request.ModelReference);
        line.Colors = NullIfBlank(request.Colors);
        line.Quantity = request.Quantity;
        line.ManualMinutes = request.ManualMinutes;
        line.ManualSubcontractCost = request.ManualSubcontractCost;
        line.FirePercent = request.FirePercent ?? context.Setting.DefaultFirePercent;
        line.OverheadPercent = request.OverheadPercent ?? context.Setting.OverheadPercent;
        line.MarginPercent = request.MarginPercent ?? context.Setting.DefaultMarginPercent;

        // Malzemeler: istekte yoksa ürünün reçetesinden (genel satırlar, fire dahil miktar).
        List<QuotationMaterialRequest> materials;
        if (request.Materials is null && request.ProductId is { } bomProductId)
        {
            var bom = await db.ProductMaterials.AsNoTracking().Where(m => m.ProductId == bomProductId).ToListAsync(cancellationToken);
            var general = MaterialRequirement.GeneralLines(bom);
            materials = general
                .Select(l => new QuotationMaterialRequest(l.StockItemId, null, decimal.Round(l.GrossQuantityPerPiece, 4), null))
                .ToList();
            if (general.Count == 0)
            {
                warnings.Add("Ürünün reçetesi yok; malzemeleri elle ekleyin.");
            }
            else if (bom.Any(l => l.Specificity > 0))
            {
                warnings.Add("Renk / bedene özel reçete satırları var; genel reçete kullanıldı.");
            }
        }
        else
        {
            materials = (request.Materials ?? []).ToList();
        }

        if (await ApplyMaterialsAsync(line, materials, context, warnings, cancellationToken) is { } materialError)
        {
            return materialError;
        }

        // Rota: fason olmayan adımların SAM toplamı işçilik dakikasıdır; fason adımlar fason fiyatıyla sayılır.
        decimal? routeMinutes = null;
        decimal routeSubcontract = 0;
        if (modelId is { } model)
        {
            var steps = await db.ProductionRouteOperations.AsNoTracking()
                .Where(o => o.ProductionRoute.ModelId == model && o.ProductionRoute.Status == RouteStatus.Active)
                .Select(o => new { o.Sam, o.IsSubcontracted, o.SubcontractorId, o.OperationId, OperationName = o.Operation.Name })
                .ToListAsync(cancellationToken);
            if (steps.Count == 0)
            {
                if (request.ManualMinutes is null)
                {
                    warnings.Add("Ürünün modelinin etkin rotası yok; dakikayı elle girin.");
                }
            }
            else
            {
                routeMinutes = steps.Where(s => !s.IsSubcontracted).Sum(s => s.Sam);
                // Rota kaynaklı fason maliyeti elle değer girilse de saklanır (ekranda "rotaya dön" için); eksik fiyat
                // uyarısı yalnızca rota değeri kullanılıyorsa gösterilir.
                var subcontractWarnings = new List<string>();
                foreach (var step in steps.Where(s => s.IsSubcontracted))
                {
                    routeSubcontract += await SubcontractCostAsync(
                        step.OperationId, step.OperationName, step.SubcontractorId, request.ProductId, context, subcontractWarnings,
                        cancellationToken);
                }

                if (request.ManualSubcontractCost is null)
                {
                    warnings.AddRange(subcontractWarnings);
                }
            }
        }
        else if (request.ManualMinutes is null)
        {
            warnings.Add("Dakika girilmedi; işçilik sıfır hesaplandı.");
        }

        line.RouteMinutes = routeMinutes is null ? null : decimal.Round(routeMinutes.Value, 4);
        line.RouteSubcontractCost = decimal.Round(routeSubcontract, 4);
        line.LaborMinutes = decimal.Round(request.ManualMinutes ?? routeMinutes ?? 0, 4);
        QuotationCost.Apply(line, context.LaborRate, context.EnergyRate, decimal.Round(routeSubcontract, 4), request.UnitPrice);
        line.Warnings = warnings.Count == 0 ? null : Truncate(string.Join(WarningSeparator, warnings.Distinct()), 2000);
        return null;
    }

    private async Task<Error?> ApplyMaterialsAsync(
        QuotationLine line, IReadOnlyList<QuotationMaterialRequest> materials, CostContext context, List<string> warnings,
        CancellationToken cancellationToken)
    {
        var itemIds = materials.Where(m => m.StockItemId is not null).Select(m => m.StockItemId!.Value).Distinct().ToList();
        var items = await db.StockItems.AsNoTracking().IgnoreQueryFilters()
            .Where(i => itemIds.Contains(i.Id))
            .Select(i => new { i.Id, i.Code, i.Name, i.Unit, i.UnitCost, i.IsDeleted })
            .ToDictionaryAsync(i => i.Id, cancellationToken);
        if (itemIds.Any(id => !items.ContainsKey(id)))
        {
            return ReferenceValidation.InvalidReference("lines", "Satırdaki malzemelerden biri bulunamadı.");
        }

        // Stok kartı maliyetleri sistem para birimindedir.
        var localRate = itemIds.Count == 0 ? 1m : await context.RateAsync(ERP.Core.Domain.Entities.Currency.LocalCode, cancellationToken);
        if (localRate is null && materials.Any(m => m.UnitCost is null && m.StockItemId is not null))
        {
            warnings.Add(
                $"{ERP.Core.Domain.Entities.Currency.LocalCode} → {context.Currency} kuru bulunamadı; stok maliyetleri sıfır hesaplandı.");
        }

        line.Materials.Clear();
        var order = 0;
        foreach (var material in materials)
        {
            var item = material.StockItemId is { } id ? items[id] : null;
            var description = NullIfBlank(material.Description) ?? (item is null ? "?" : $"{item.Code} {item.Name}");
            decimal unitCost;
            if (material.UnitCost is { } given)
            {
                unitCost = given;
            }
            else if (item?.UnitCost is { } stockCost)
            {
                unitCost = decimal.Round(stockCost * (localRate ?? 0), 4);
            }
            else
            {
                unitCost = 0;
                warnings.Add(item is null
                    ? $"{description}: birim maliyet girilmedi."
                    : $"{item.Code}: stok kartında birim maliyet yok; maliyeti elle girin.");
            }

            line.Materials.Add(new QuotationLineMaterial
            {
                SortOrder = ++order,
                StockItemId = material.StockItemId,
                Description = Truncate(description, 200),
                Unit = item?.Unit,
                QuantityPerPiece = material.QuantityPerPiece,
                UnitCost = unitCost,
                UnitCostOverridden = material.UnitCost is not null,
            });
        }

        return null;
    }

    /// <summary>
    /// Fason adımın parça başı maliyeti (teklif para biriminde): varsayılan fasoncunun teklif günündeki fiyatı; varsayılan
    /// fasoncu yoksa fiyatı olan fasoncuların en düşüğü.
    /// </summary>
    private async Task<decimal> SubcontractCostAsync(
        int operationId, string operationName, int? subcontractorId, int? productId, CostContext context, List<string> warnings,
        CancellationToken cancellationToken)
    {
        List<SubcontractPrice> candidates;
        if (subcontractorId is { } supplierId)
        {
            var resolved = await SubcontractPriceService.ResolveManyAsync(
                db, [new SubcontractPriceKey(supplierId, operationId, productId)], context.Date, cancellationToken);
            candidates = resolved.Values.ToList();
        }
        else
        {
            var supplierIds = await db.SubcontractPrices.AsNoTracking()
                .Where(p => p.OperationId == operationId && p.ValidFrom <= context.Date
                    && (p.ProductId == null || p.ProductId == productId))
                .Select(p => p.SupplierId)
                .Distinct()
                .ToListAsync(cancellationToken);
            var resolved = await SubcontractPriceService.ResolveManyAsync(
                db, supplierIds.Select(s => new SubcontractPriceKey(s, operationId, productId)).ToList(), context.Date,
                cancellationToken);
            candidates = resolved.Values.ToList();
        }

        decimal? best = null;
        foreach (var candidate in candidates)
        {
            var rate = await context.RateAsync(candidate.Currency, cancellationToken);
            if (rate is null)
            {
                warnings.Add($"{candidate.Currency} → {context.Currency} kuru bulunamadı; {operationName} fason fiyatı sayılmadı.");
                continue;
            }

            var converted = decimal.Round(candidate.UnitPrice * rate.Value, 4);
            best = best is null ? converted : Math.Min(best.Value, converted);
        }

        if (best is null)
        {
            warnings.Add($"{operationName}: fason adımın fiyatı yok (Fason Fiyatları); fason maliyeti eksik.");
            return 0;
        }

        if (subcontractorId is null)
        {
            warnings.Add($"{operationName}: varsayılan fasoncu yok; en düşük fason fiyatı alındı.");
        }

        return best.Value;
    }

    // ------------------------------------------------------------------ Yazma

    public Task<Result<QuotationDto>> CreateAsync(QuotationRequest request, CancellationToken cancellationToken) =>
        SaveAsync(null, request, cancellationToken);

    public Task<Result<QuotationDto>> UpdateAsync(int id, QuotationRequest request, CancellationToken cancellationToken) =>
        SaveAsync(id, request, cancellationToken);

    private async Task<Result<QuotationDto>> SaveAsync(int? id, QuotationRequest request, CancellationToken cancellationToken)
    {
        Quotation? quotation = null;
        if (id is not null)
        {
            quotation = await db.Quotations
                .Include(q => q.Lines).ThenInclude(l => l.Materials)
                .SingleOrDefaultAsync(q => q.Id == id, cancellationToken);
            if (quotation is null)
            {
                return NotFound;
            }

            if (RowVersionToken.Check(quotation.RowVersion, request.RowVersion, ConcurrencyConflict) is { } versionError)
            {
                return versionError;
            }

            if (!quotation.IsEditable)
            {
                return NotEditable(quotation);
            }
        }

        var customerId = request.CustomerId is > 0 ? request.CustomerId : null;
        if (await ReferenceValidation.RequireSelectableAsync(
                db.Customers, customerId, quotation?.CustomerId, "customerId", "Müşteri", cancellationToken) is { } customerError)
        {
            return customerError;
        }

        var customer = customerId is null
            ? null
            : await db.Customers.AsNoTracking()
                .Where(c => c.Id == customerId)
                .Select(c => new { c.Currency, c.DocumentLanguage })
                .SingleAsync(cancellationToken);
        var currency = NullIfBlank(request.Currency)?.ToUpperInvariant()
            ?? customer?.Currency
            ?? ERP.Core.Domain.Entities.Currency.LocalCode;
        if (await CurrencyRules.RequireActiveAsync(db, currency, quotation?.Currency, "currency", cancellationToken) is { } currencyError)
        {
            return currencyError;
        }

        var context = await CostContext.CreateAsync(db, exchangeRates, currency, request.QuotationDate, cancellationToken);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (quotation is null)
        {
            var year = request.QuotationDate.Year;
            var sequence = await numbers.NextAsync(Series, year, cancellationToken);
            quotation = new Quotation
            {
                QuotationNo = $"{NumberPrefix}-{year}-{sequence.ToString(CultureInfo.InvariantCulture).PadLeft(5, '0')}",
            };
            db.Quotations.Add(quotation);
        }
        else
        {
            // Satırlar yeniden yazılır (teklif taslakken satır kimliğine bağlı kayıt yoktur).
            foreach (var old in quotation.Lines.ToList())
            {
                db.QuotationLineMaterials.RemoveRange(old.Materials);
                db.QuotationLines.Remove(old);
                quotation.Lines.Remove(old);
            }

            db.Entry(quotation).State = EntityState.Modified;
        }

        quotation.CustomerId = customerId;
        quotation.ProspectName = customerId is null ? NullIfBlank(request.ProspectName) : null;
        quotation.QuotationDate = request.QuotationDate;
        quotation.ValidUntil = request.ValidUntil;
        quotation.Currency = currency;
        quotation.DocumentLanguage = request.DocumentLanguage ?? customer?.DocumentLanguage ?? DocumentLanguage.Turkish;
        quotation.Notes = NullIfBlank(request.Notes);
        quotation.LaborCostPerMinute = context.LaborRate;
        quotation.EnergyCostPerMinute = context.EnergyRate;

        var lineNo = 0;
        foreach (var lineRequest in request.Lines)
        {
            var line = new QuotationLine { LineNo = ++lineNo };
            if (await ComputeLineAsync(line, lineRequest, context, cancellationToken) is { } lineError)
            {
                return lineError;
            }

            quotation.Lines.Add(line);
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ConcurrencyConflict;
        }

        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(quotation.Id, cancellationToken);
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var quotation = await db.Quotations.SingleOrDefaultAsync(q => q.Id == id, cancellationToken);
        if (quotation is null)
        {
            return NotFound;
        }

        if (!quotation.IsEditable)
        {
            return NotEditable(quotation);
        }

        // Soft delete: numara korunur, yeniden kullanılmaz.
        db.Quotations.Remove(quotation);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public Task<Result<QuotationDto>> SendAsync(int id, QuotationActionRequest request, CancellationToken cancellationToken) =>
        ChangeAsync(id, request.RowVersion, QuotationStatus.Sent, quotation =>
        {
            if (quotation.Lines.Count == 0)
            {
                return new Error(QuotationErrors.InvalidState, "Satırı olmayan teklif gönderilemez.", ErrorType.BusinessRule);
            }

            quotation.Send(UtcNow);
            return null;
        }, cancellationToken);

    public Task<Result<QuotationDto>> RejectAsync(int id, QuotationActionRequest request, CancellationToken cancellationToken) =>
        ChangeAsync(id, request.RowVersion, QuotationStatus.Rejected, quotation =>
        {
            quotation.Reject(UtcNow, UserName);
            return null;
        }, cancellationToken);

    public Task<Result<QuotationDto>> ExpireAsync(int id, QuotationActionRequest request, CancellationToken cancellationToken) =>
        ChangeAsync(id, request.RowVersion, QuotationStatus.Expired, quotation =>
        {
            quotation.Expire(UtcNow, UserName);
            return null;
        }, cancellationToken);

    public Task<Result<QuotationDto>> ReopenAsync(int id, QuotationActionRequest request, CancellationToken cancellationToken) =>
        ChangeAsync(id, request.RowVersion, QuotationStatus.Draft, quotation =>
        {
            quotation.Reopen();
            return null;
        }, cancellationToken);

    private async Task<Result<QuotationDto>> ChangeAsync(
        int id, string rowVersion, QuotationStatus target, Func<Quotation, Error?> apply, CancellationToken cancellationToken)
    {
        var quotation = await db.Quotations.Include(q => q.Lines).SingleOrDefaultAsync(q => q.Id == id, cancellationToken);
        if (quotation is null)
        {
            return NotFound;
        }

        if (RowVersionToken.Check(quotation.RowVersion, rowVersion, ConcurrencyConflict) is { } versionError)
        {
            return versionError;
        }

        if (!quotation.CanTransitionTo(target))
        {
            return new Error(
                ErrorCodes.InvalidStatusTransition,
                $"{StatusLabels[quotation.Status]} durumundaki teklif {StatusLabels[target].ToLowerInvariant()} durumuna geçemez.",
                ErrorType.Conflict);
        }

        if (apply(quotation) is { } error)
        {
            return error;
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ConcurrencyConflict;
        }

        return await GetAsync(id, cancellationToken);
    }

    public async Task<Result<QuotationDto>> AcceptAsync(int id, QuotationAcceptRequest request, CancellationToken cancellationToken)
    {
        // Ön denetim izlenmeyen kopya üzerinde yapılır: sipariş servisi kendi kaydını yaparken teklifte yarım kalmış
        // değişiklik bulunmasın.
        var snapshot = await db.Quotations.AsNoTracking()
            .Include(q => q.Lines)
            .SingleOrDefaultAsync(q => q.Id == id, cancellationToken);
        if (snapshot is null)
        {
            return NotFound;
        }

        if (RowVersionToken.Check(snapshot.RowVersion, request.RowVersion, ConcurrencyConflict) is { } versionError)
        {
            return versionError;
        }

        if (!snapshot.CanTransitionTo(QuotationStatus.Accepted))
        {
            return new Error(
                ErrorCodes.InvalidStatusTransition,
                $"{StatusLabels[snapshot.Status]} durumundaki teklif kabul edilemez; önce müşteriye gönderilmiş olmalıdır.",
                ErrorType.Conflict);
        }

        var customerId = snapshot.CustomerId ?? (request.CustomerId is > 0 ? request.CustomerId : null);
        if (customerId is null)
        {
            const string message = "Teklif aday müşteriye verildi; siparişin açılacağı müşteri kartını seçin.";
            return new Error(
                QuotationErrors.CustomerRequired, message, ErrorType.Validation,
                new Dictionary<string, string[]> { ["customerId"] = [message] });
        }

        if (request.DueDate < Today())
        {
            const string message = "Teslim tarihi bugünden önce olamaz.";
            return new Error(
                ErrorCodes.ValidationError, message, ErrorType.Validation, new Dictionary<string, string[]> { ["dueDate"] = [message] });
        }

        var productLines = snapshot.Lines.Where(l => l.ProductId is not null).ToDictionary(l => l.Id);
        if (productLines.Count == 0)
        {
            return new Error(
                QuotationErrors.InvalidState,
                "Teklifte ürün kartı seçilmiş satır yok; siparişe aktarılacak ürün bulunmuyor. Ürün kartını açıp teklifi düzeltin.",
                ErrorType.BusinessRule);
        }

        if (request.Lines.Any(l => !productLines.ContainsKey(l.QuotationLineId)))
        {
            return ReferenceValidation.InvalidReference("lines", "Satırlardan biri bu teklifin ürünlü satırı değil.");
        }

        var missing = productLines.Values.Where(l => request.Lines.All(r => r.QuotationLineId != l.Id)).Select(l => l.LineNo).ToList();
        if (missing.Count > 0)
        {
            var message = $"Şu satırlar için renk / beden adedi girilmedi: {string.Join(", ", missing)}.";
            return new Error(
                ErrorCodes.ValidationError, message, ErrorType.Validation, new Dictionary<string, string[]> { ["lines"] = [message] });
        }

        // Aynı ürün, renk ve beden siparişte bir kez yer alır: farklı teklif satırlarından gelen aynı kırılım birleşmez.
        var orderLines = request.Lines
            .Select(l => new SalesOrderLineRequest(
                productLines[l.QuotationLineId].ProductId!.Value, l.ColorId, l.SizeId, l.Quantity, productLines[l.QuotationLineId].UnitPrice))
            .ToList();
        if (orderLines.Select(l => (l.ProductId, l.ColorId, l.SizeId)).Distinct().Count() != orderLines.Count)
        {
            const string message = "Aynı ürün için aynı renk ve beden birden fazla satırda girildi; siparişte bir kez yer alabilir.";
            return new Error(
                ErrorCodes.ValidationError, message, ErrorType.Validation, new Dictionary<string, string[]> { ["lines"] = [message] });
        }

        var created = await salesOrders.CreateAsync(
            new SalesOrderRequest(
                customerId.Value,
                NullIfBlank(request.CustomerOrderNo),
                Today(),
                request.DueDate,
                $"{snapshot.QuotationNo} numaralı tekliften açıldı.",
                orderLines,
                snapshot.Currency),
            cancellationToken);
        if (!created.IsSuccess)
        {
            return created.Error!;
        }

        var quotation = await db.Quotations.SingleAsync(q => q.Id == id, cancellationToken);
        Error? failure = null;
        if (!quotation.RowVersion.SequenceEqual(snapshot.RowVersion) || !quotation.CanTransitionTo(QuotationStatus.Accepted))
        {
            failure = ConcurrencyConflict;
        }
        else
        {
            quotation.CustomerId ??= customerId;
            quotation.Accept(created.Value.Id, UtcNow, UserName);
            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                failure = ConcurrencyConflict;
            }
        }

        if (failure is not null)
        {
            // Teklif başka bir işlemle değişti: açılan taslak sipariş geri alınır (taslak sipariş silinebilir).
            db.Entry(quotation).State = EntityState.Detached;
            await salesOrders.DeleteAsync(created.Value.Id, cancellationToken);
            return failure;
        }

        return await GetAsync(id, cancellationToken);
    }

    // ------------------------------------------------------------------ Dönüşüm

    private QuotationDto ToDto(Quotation quotation)
    {
        var lines = quotation.Lines
            .OrderBy(l => l.LineNo)
            .Select(l => new QuotationLineDto(
                l.Id,
                l.LineNo,
                l.ProductId,
                l.Product?.ProductCode,
                l.Product?.Name,
                l.Product?.Model?.ModelCode,
                l.Description,
                l.ModelReference,
                l.Colors,
                l.Quantity,
                l.ManualMinutes,
                l.ManualSubcontractCost,
                l.PriceOverridden,
                l.FirePercent,
                l.OverheadPercent,
                l.MarginPercent,
                l.MaterialCost,
                l.LaborMinutes,
                l.LaborCost,
                l.EnergyCost,
                l.SubcontractCost,
                l.FireCost,
                l.OverheadCost,
                l.UnitCost,
                l.UnitPrice,
                l.Amount,
                WarningsOf(l.Warnings),
                l.Materials
                    .OrderBy(m => m.SortOrder)
                    .Select(m => new QuotationMaterialDto(
                        m.Id, m.StockItemId, m.StockItem?.Code, m.Description, m.Unit, m.QuantityPerPiece, m.UnitCost,
                        m.UnitCostOverridden, m.Cost))
                    .ToList(),
                l.RouteMinutes,
                l.RouteSubcontractCost))
            .ToList();

        return new QuotationDto(
            quotation.Id,
            quotation.QuotationNo,
            quotation.CustomerId,
            quotation.Customer?.CustomerCode,
            quotation.Customer?.Name,
            quotation.ProspectName,
            quotation.QuotationDate,
            quotation.ValidUntil,
            quotation.Currency,
            quotation.DocumentLanguage,
            quotation.Status,
            quotation.Notes,
            quotation.LaborCostPerMinute,
            quotation.EnergyCostPerMinute,
            quotation.IsEditable,
            quotation.AllowedTransitions,
            quotation.Status == QuotationStatus.Sent && quotation.ValidUntil < Today(),
            quotation.SalesOrderId,
            quotation.SalesOrder?.OrderNo,
            quotation.SentAt,
            quotation.DecidedAt,
            quotation.DecidedBy,
            lines.Sum(l => l.Quantity),
            decimal.Round(lines.Sum(l => l.Quantity * l.UnitCost), 2),
            lines.Sum(l => l.Amount),
            RowVersionToken.From(quotation.RowVersion),
            quotation.CreatedAt,
            quotation.CreatedBy,
            lines);
    }

    private sealed record ProductInfo(string Code, string Name, string? ModelCode);

    private async Task<Dictionary<int, ProductInfo>> LoadProductInfoAsync(IReadOnlyList<int> productIds, CancellationToken cancellationToken) =>
        productIds.Count == 0
            ? []
            : await db.Products.AsNoTracking().IgnoreQueryFilters()
                .Where(p => productIds.Contains(p.Id))
                .Select(p => new { p.Id, p.ProductCode, p.Name, p.Model.ModelCode })
                .ToDictionaryAsync(p => p.Id, p => new ProductInfo(p.ProductCode, p.Name, p.ModelCode), cancellationToken);

    private async Task<Dictionary<int, string>> LoadStockItemCodesAsync(IReadOnlyList<int> itemIds, CancellationToken cancellationToken) =>
        itemIds.Count == 0
            ? []
            : await db.StockItems.AsNoTracking().IgnoreQueryFilters()
                .Where(i => itemIds.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id, i => i.Code, cancellationToken);

    /// <summary>Kaydedilmemiş (önizleme) satırın gösterimi.</summary>
    private static QuotationLineDto LineDto(
        QuotationLine line, IReadOnlyDictionary<int, ProductInfo> products, IReadOnlyDictionary<int, string> itemCodes)
    {
        var product = line.ProductId is { } productId ? products.GetValueOrDefault(productId) : null;
        return new QuotationLineDto(
            0,
            line.LineNo,
            line.ProductId,
            product?.Code,
            product?.Name,
            product?.ModelCode,
            line.Description,
            line.ModelReference,
            line.Colors,
            line.Quantity,
            line.ManualMinutes,
            line.ManualSubcontractCost,
            line.PriceOverridden,
            line.FirePercent,
            line.OverheadPercent,
            line.MarginPercent,
            line.MaterialCost,
            line.LaborMinutes,
            line.LaborCost,
            line.EnergyCost,
            line.SubcontractCost,
            line.FireCost,
            line.OverheadCost,
            line.UnitCost,
            line.UnitPrice,
            line.Amount,
            WarningsOf(line.Warnings),
            line.Materials
                .OrderBy(m => m.SortOrder)
                .Select(m => new QuotationMaterialDto(
                    0, m.StockItemId, m.StockItemId is { } id ? itemCodes.GetValueOrDefault(id) : null, m.Description, m.Unit,
                    m.QuantityPerPiece, m.UnitCost, m.UnitCostOverridden, m.Cost))
                .ToList(),
            line.RouteMinutes,
            line.RouteSubcontractCost);
    }

    private static IReadOnlyList<string> WarningsOf(string? warnings) =>
        string.IsNullOrEmpty(warnings) ? [] : warnings.Split(WarningSeparator, StringSplitOptions.RemoveEmptyEntries);

    private static string Truncate(string value, int length) => value.Length <= length ? value : value[..length];

    private static Error NotEditable(Quotation quotation) => new(
        QuotationErrors.InvalidState,
        $"{StatusLabels[quotation.Status]} durumundaki teklif değiştirilemez; önce taslağa alın.",
        ErrorType.Conflict);

    private static string? AddressText(string? line, string? district, string? city)
    {
        var parts = new[] { line, string.Join(" / ", new[] { district, city }.Where(p => !string.IsNullOrWhiteSpace(p))) }
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();
        return parts.Count == 0 ? null : string.Join(", ", parts);
    }
}

/// <summary>Teklif satırının parça başı maliyet ve fiyat hesabı (web'deki önizleme aynı formülü kullanır).</summary>
public static class QuotationCost
{
    /// <summary>
    /// Malzeme, dakika ve yüzdeler yazılmış satırın dökümünü hesaplar. <paramref name="unitPrice"/> verilirse fiyat
    /// odur ve marj fiyattan geri hesaplanır; verilmezse fiyat = birim maliyet × (1 + marj %).
    /// </summary>
    public static void Apply(QuotationLine line, decimal laborRate, decimal energyRate, decimal routeSubcontractCost, decimal? unitPrice)
    {
        line.MaterialCost = Round4(line.Materials.Sum(m => m.Cost));
        line.LaborCost = Round4(line.LaborMinutes * laborRate);
        line.EnergyCost = Round4(line.LaborMinutes * energyRate);
        line.SubcontractCost = Round4(line.ManualSubcontractCost ?? routeSubcontractCost);

        var direct = line.MaterialCost + line.LaborCost + line.EnergyCost + line.SubcontractCost;
        line.FireCost = Round4(direct * line.FirePercent / 100);
        line.OverheadCost = Round4((direct + line.FireCost) * line.OverheadPercent / 100);
        line.UnitCost = Round4(direct + line.FireCost + line.OverheadCost);

        if (unitPrice is { } price)
        {
            line.PriceOverridden = true;
            line.UnitPrice = price;
            line.MarginPercent = line.UnitCost > 0 ? Math.Clamp(decimal.Round((price / line.UnitCost - 1) * 100, 2), -100, 9_999_999) : 0;
        }
        else
        {
            line.PriceOverridden = false;
            line.UnitPrice = decimal.Round(line.UnitCost * (1 + line.MarginPercent / 100), 2);
        }
    }

    private static decimal Round4(decimal value) => decimal.Round(value, 4);
}
