using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Core.Domain.Enums;
using ERP.Dokuma.Application.Features.Receipts;
using ERP.Dokuma.Domain.Entities;
using ERP.Dokuma.Domain.Enums;
using ERP.Dokuma.Domain.Rules;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Dokuma.Application.Features.Orders;

public sealed class DokumaOrderQuery : PageRequest
{
    /// <summary>Sipariş no, müşteri sipariş no ya da müşteri adında aranır.</summary>
    public string? Search { get; set; }
}

public sealed record DokumaOrderListItemDto(
    int Id,
    string OrderNo,
    DateOnly OrderDate,
    string? CustomerName,
    string? CustomerOrderNo,
    OrderOwnership Ownership,
    DateOnly? DueDate,
    int LineCount,
    decimal TotalMeters,
    decimal TotalAmount,
    string Currency,
    OrderStatus Status);

/// <param name="Amount">Metre × birim fiyat; fiyat girilmemişse boş.</param>
/// <param name="CustomerArticleCode">Müşterinin bu artikel için kullandığı kendi kodu.</param>
public sealed record DokumaOrderLineDto(
    int Id,
    int ArticleId,
    string ArticleCode,
    string ArticleName,
    decimal Meters,
    decimal? UnitPrice,
    decimal? Amount,
    string? Notes,
    string? CustomerArticleCode);

public sealed record DokumaOrderDto(
    int Id,
    string OrderNo,
    DateOnly OrderDate,
    int? CustomerId,
    string? CustomerName,
    string? CustomerOrderNo,
    OrderOwnership Ownership,
    DateOnly? DueDate,
    string Currency,
    string? Notes,
    OrderStatus Status,
    decimal TotalMeters,
    decimal TotalAmount,
    DateTime CreatedAt,
    string? CreatedBy,
    IReadOnlyList<DokumaOrderLineDto> Lines);

/// <param name="Id">Var olan satır güncelleniyorsa Id'si; yeni satırda boş.</param>
/// <param name="UnitPrice">Metre başına fiyat; henüz belli değilse boş.</param>
/// <param name="CustomerArticleCode">Müşterinin bu artikel için kullandığı kendi kodu; etikete ve sevk belgelerine basılır.</param>
public sealed record DokumaOrderLineRequest(
    int? Id,
    int ArticleId,
    decimal Meters,
    decimal? UnitPrice = null,
    string? Notes = null,
    string? CustomerArticleCode = null);

/// <param name="CustomerId">Fason işte zorunlu; kendi malında boşsa stoğa üretimdir.</param>
/// <param name="Currency">Boşsa müşterinin para birimi, müşteri yoksa TRY kullanılır.</param>
public sealed record DokumaOrderRequest(
    int? CustomerId,
    DateOnly OrderDate,
    IReadOnlyList<DokumaOrderLineRequest> Lines,
    OrderOwnership Ownership = OrderOwnership.Commission,
    string? CustomerOrderNo = null,
    DateOnly? DueDate = null,
    string? Currency = null,
    string? Notes = null);

public sealed class DokumaOrderRequestValidator : AbstractValidator<DokumaOrderRequest>
{
    public const int MaxLines = 100;

    public DokumaOrderRequestValidator()
    {
        RuleFor(x => x.CustomerId).NotNull().WithMessage("Fason siparişte müşteri seçilmelidir.").When(x => x.Ownership == OrderOwnership.Commission);
        RuleFor(x => x.OrderDate).NotEmpty().WithMessage("Sipariş tarihi zorunludur.");
        RuleFor(x => x.Ownership).IsInEnum().WithMessage("Geçerli bir iş türü seçin.");
        RuleFor(x => x.CustomerOrderNo).OptionalText(50);
        RuleFor(x => x.Notes).OptionalText(1000);
        RuleFor(x => x.DueDate)
            .Must((request, due) => due >= request.OrderDate).WithMessage("Termin sipariş tarihinden önce olamaz.")
            .When(x => x.DueDate is not null);
        RuleFor(x => x.Currency)
            .Matches(@"^\s*[A-Za-z]{3}\s*$").WithMessage("Para birimi üç harfli ISO kodu olmalıdır (örn. TRY, EUR).")
            .When(x => !string.IsNullOrWhiteSpace(x.Currency));
        RuleFor(x => x.Lines)
            .NotEmpty().WithMessage("En az bir satır girilmelidir.")
            .Must(lines => lines.Count <= MaxLines).WithMessage($"Bir siparişte en fazla {MaxLines} satır olabilir.")
            .Custom((lines, context) =>
            {
                if (lines is null)
                {
                    return;
                }

                for (var i = 0; i < lines.Count; i++)
                {
                    var line = lines[i];
                    if (line.ArticleId <= 0)
                    {
                        context.AddFailure($"{i + 1}. satır: artikel seçilmelidir.");
                    }

                    if (line.Meters is <= 0 or > 9_999_999)
                    {
                        context.AddFailure($"{i + 1}. satır: metre sıfırdan büyük olmalıdır.");
                    }

                    if (line.UnitPrice is < 0)
                    {
                        context.AddFailure($"{i + 1}. satır: birim fiyat negatif olamaz.");
                    }

                    if (line.Notes?.Trim().Length > 500)
                    {
                        context.AddFailure($"{i + 1}. satır: not en fazla 500 karakter olabilir.");
                    }

                    if (line.CustomerArticleCode?.Trim().Length > 50)
                    {
                        context.AddFailure($"{i + 1}. satır: müşteri artikel kodu en fazla 50 karakter olabilir.");
                    }
                }
            });
    }
}

/// <param name="NetKilograms">Siparişin bu iplikten fire hariç ihtiyacı.</param>
/// <param name="RequiredKilograms">Artikeldeki çözgü / atkı firesi eklenmiş ihtiyaç.</param>
/// <param name="IssuedKilograms">Bu siparişin çözgü emirlerine verilen iplik.</param>
/// <param name="AvailableKilograms">Malın sahibine ait lotların kalanı (çözgüye verilenler düşülmüş).</param>
/// <param name="OtherOrdersKilograms">Aynı sahibin diğer onaylı siparişlerinin kalan fireli ihtiyacı.</param>
/// <param name="FreeKilograms">Eldeki miktardan diğer siparişlere gereken düşüldükten sonra kalan (eksi olabilir).</param>
/// <param name="ShortageKilograms">Kalan ihtiyaç (fireli − verilen) serbest miktarı aşıyorsa eksik; aksi halde sıfır.</param>
public sealed record DokumaYarnRequirementDto(
    int YarnId,
    string YarnCode,
    string YarnName,
    string? YarnColor,
    bool UsedInWarp,
    bool UsedInWeft,
    decimal NetKilograms,
    decimal RequiredKilograms,
    decimal IssuedKilograms,
    decimal AvailableKilograms,
    decimal OtherOrdersKilograms,
    decimal FreeKilograms,
    decimal ShortageKilograms);

/// <param name="MissingArticles">İplik tanımı eksik olduğu için hesaba katılamayan artikeller.</param>
public sealed record DokumaOrderRequirementsDto(int OrderId, string? OwnerName, IReadOnlyList<DokumaYarnRequirementDto> Yarns, IReadOnlyList<string> MissingArticles);

/// <param name="IssuedKilograms">Siparişe verilen iplik (çözgü + atkı).</param>
/// <param name="TheoreticalKilograms">Dokunan metre için hesaplanan net iplik.</param>
/// <param name="WasteKilograms">Verilen − hesaplanan; dokuma sürerken yüksek görünür.</param>
/// <param name="WastePercent">Fire / verilen.</param>
public sealed record DokumaYarnWasteDto(int YarnId, string YarnCode, string YarnName, decimal IssuedKilograms, decimal TheoreticalKilograms, decimal WasteKilograms, decimal WastePercent);

/// <param name="TolerancePercent">Müşteri kartında kabul edilen iplik firesi; fasonda karşılaştırılır.</param>
/// <param name="TotalWastePercent">Sipariş toplamı fire oranı.</param>
public sealed record DokumaOrderWasteDto(int OrderId, decimal WovenMeters, bool AllBeamsFinished, decimal? TolerancePercent, decimal TotalWastePercent, bool ExceedsTolerance, IReadOnlyList<DokumaYarnWasteDto> Yarns);

/// <summary>Proforma / sipariş teyidi satırı.</summary>
/// <param name="Amount">Metre × birim fiyat; fiyat girilmemişse boş.</param>
public sealed record DokumaProformaLineDto(
    string ArticleCode,
    string ArticleName,
    string? CustomerArticleCode,
    decimal RawWidthCm,
    int RawWeightGsm,
    decimal Meters,
    decimal? UnitPrice,
    decimal? Amount,
    string? Notes);

/// <summary>Proforma fatura ve sipariş teyidi çıktısı: müşteri, vergi bilgisi, satırlar ve KDV'li toplam.</summary>
/// <param name="CustomerAddress">Müşterinin varsayılan fatura adresi.</param>
/// <param name="VatRate">Müşteri kartındaki varsayılan KDV oranı; tanımlı değilse boş (KDV sıfır hesaplanır).</param>
/// <param name="HasMissingPrices">Fiyatı girilmemiş satır var; toplam eksik görünür.</param>
public sealed record DokumaProformaDto(
    int OrderId,
    string OrderNo,
    DateOnly OrderDate,
    DateOnly? DueDate,
    string? CustomerOrderNo,
    OrderStatus Status,
    string Currency,
    string? Notes,
    string? CustomerCode,
    string? CustomerName,
    string? CustomerTaxNumber,
    string? CustomerTaxOffice,
    string? CustomerAddress,
    int? PaymentTermDays,
    decimal? VatRate,
    decimal TotalMeters,
    decimal Subtotal,
    decimal VatAmount,
    decimal Total,
    bool HasMissingPrices,
    DocumentLanguage DocumentLanguage,
    IReadOnlyList<DokumaProformaLineDto> Lines);

public interface IDokumaOrderService
{
    Task<Result<DokumaProformaDto>> GetProformaAsync(int id, CancellationToken cancellationToken);
    Task<PagedResult<DokumaOrderListItemDto>> GetListAsync(DokumaOrderQuery query, CancellationToken cancellationToken);
    Task<Result<DokumaOrderDto>> GetAsync(int id, CancellationToken cancellationToken);
    Task<Result<DokumaOrderDto>> CreateAsync(DokumaOrderRequest request, CancellationToken cancellationToken);
    Task<Result<DokumaOrderDto>> UpdateAsync(int id, DokumaOrderRequest request, CancellationToken cancellationToken);
    Task<Result<DokumaOrderDto>> ConfirmAsync(int id, CancellationToken cancellationToken);
    Task<Result<DokumaOrderDto>> ReopenAsync(int id, CancellationToken cancellationToken);
    Task<Result<DokumaOrderDto>> CancelAsync(int id, CancellationToken cancellationToken);
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken);
    Task<Result<DokumaOrderRequirementsDto>> GetRequirementsAsync(int id, CancellationToken cancellationToken);
    Task<Result<DokumaOrderWasteDto>> GetWasteAsync(int id, CancellationToken cancellationToken);
}

public sealed class DokumaOrderService(IAppDbContext db, IDocumentNumberGenerator numbers) : IDokumaOrderService
{
    private const string OrderSeries = "DokumaOrder";
    private const string InvalidState = "DOKUMA_ORDER_INVALID_STATE";

    private const string InWarpMessage = "Çözgü emri açılmış sipariş taslağa alınamaz ve iptal edilemez; önce çözgü emirlerini iptal edin.";

    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Sipariş bulunamadı.", ErrorType.NotFound);

    private Task<bool> HasWarpOrdersAsync(int orderId, CancellationToken cancellationToken) =>
        db.Set<DokumaWarpOrder>().AnyAsync(w => w.OrderLine.OrderId == orderId && w.Status != WarpOrderStatus.Cancelled, cancellationToken);

    /// <summary>
    /// İptal edilmişler dahil çözgü emri açılmış satırlar. İptal edilen çözgü emri de satıra bağlı kalır: bu satır
    /// silinemez, artikeli değiştirilemez (metre ve fiyat düzeltilebilir).
    /// </summary>
    private async Task<HashSet<int>> LinesWithWarpOrdersAsync(int orderId, CancellationToken cancellationToken) =>
        (await db.Set<DokumaWarpOrder>()
            .Where(w => w.OrderLine.OrderId == orderId)
            .Select(w => w.OrderLineId)
            .Distinct()
            .ToListAsync(cancellationToken))
        .ToHashSet();

    public Task<PagedResult<DokumaOrderListItemDto>> GetListAsync(DokumaOrderQuery query, CancellationToken cancellationToken)
    {
        var orders = db.Set<DokumaOrder>().AsNoTracking();
        if (NullIfBlank(query.Search) is { } term)
        {
            orders = orders.Where(o => o.OrderNo.Contains(term)
                || (o.CustomerOrderNo != null && o.CustomerOrderNo.Contains(term))
                || (o.Customer != null && o.Customer.Name.Contains(term)));
        }

        return orders
            .OrderByDescending(o => o.OrderDate).ThenByDescending(o => o.Id)
            .Select(o => new DokumaOrderListItemDto(
                o.Id,
                o.OrderNo,
                o.OrderDate,
                o.Customer != null ? o.Customer.Name : null,
                o.CustomerOrderNo,
                o.Ownership,
                o.DueDate,
                o.Lines.Count,
                o.Lines.Sum(l => (decimal?)l.Meters) ?? 0,
                o.Lines.Sum(l => (decimal?)(l.Meters * (l.UnitPrice ?? 0))) ?? 0,
                o.Currency,
                o.Status))
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<Result<DokumaOrderDto>> GetAsync(int id, CancellationToken cancellationToken)
    {
        var order = await db.Set<DokumaOrder>().AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.Lines).ThenInclude(l => l.Article)
            .SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
        {
            return NotFound;
        }

        var lines = order.Lines.OrderBy(l => l.Id).Select(l => new DokumaOrderLineDto(
            l.Id,
            l.ArticleId,
            l.Article.ArticleCode,
            l.Article.Name,
            l.Meters,
            l.UnitPrice,
            l.UnitPrice is { } price ? Math.Round(l.Meters * price, 2) : null,
            l.Notes,
            l.CustomerArticleCode)).ToList();

        return new DokumaOrderDto(
            order.Id,
            order.OrderNo,
            order.OrderDate,
            order.CustomerId,
            order.Customer?.Name,
            order.CustomerOrderNo,
            order.Ownership,
            order.DueDate,
            order.Currency,
            order.Notes,
            order.Status,
            lines.Sum(l => l.Meters),
            lines.Sum(l => l.Amount ?? 0),
            order.CreatedAt,
            order.CreatedBy,
            lines);
    }

    /// <summary>
    /// Proforma / sipariş teyidi: taslak ve onaylı siparişte basılır. KDV müşteri kartındaki varsayılan orandan hesaplanır;
    /// oran yoksa sıfır alınır ve çıktıda not düşülür. Belge, müşterinin belge dilinde basılır.
    /// </summary>
    public async Task<Result<DokumaProformaDto>> GetProformaAsync(int id, CancellationToken cancellationToken)
    {
        var order = await db.Set<DokumaOrder>().AsNoTracking()
            .Include(o => o.Customer).ThenInclude(c => c!.Addresses)
            .Include(o => o.Lines).ThenInclude(l => l.Article)
            .SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
        {
            return NotFound;
        }

        if (order.Status is not (OrderStatus.Draft or OrderStatus.Confirmed))
        {
            return new Error(InvalidState, "Proforma yalnızca taslak ya da onaylı sipariş için basılır.", ErrorType.Conflict);
        }

        var lines = order.Lines.OrderBy(l => l.Id).Select(l => new DokumaProformaLineDto(
            l.Article.ArticleCode,
            l.Article.Name,
            l.CustomerArticleCode,
            l.Article.RawWidthCm,
            l.Article.RawWeightGsm,
            l.Meters,
            l.UnitPrice,
            l.UnitPrice is { } price ? Math.Round(l.Meters * price, 2) : null,
            l.Notes)).ToList();

        var customer = order.Customer;
        var address = customer?.Addresses.FirstOrDefault(a => a.IsDefaultInvoice) ?? customer?.Addresses.OrderBy(a => a.Id).FirstOrDefault();
        var subtotal = lines.Sum(l => l.Amount ?? 0);
        var vatRate = customer?.VatRate;
        var vat = Math.Round(subtotal * (vatRate ?? 0) / 100, 2);

        return new DokumaProformaDto(
            order.Id,
            order.OrderNo,
            order.OrderDate,
            order.DueDate,
            order.CustomerOrderNo,
            order.Status,
            order.Currency,
            order.Notes,
            customer?.CustomerCode,
            customer?.Name,
            customer?.TaxNumber,
            customer?.TaxOffice,
            address is null
                ? null
                : $"{address.AddressLine} {address.District}/{address.City}{(address.CountryCode == "TR" ? "" : " " + address.CountryCode)}",
            customer?.PaymentTermDays,
            vatRate,
            lines.Sum(l => l.Meters),
            subtotal,
            vat,
            subtotal + vat,
            lines.Any(l => l.UnitPrice is null),
            customer?.DocumentLanguage ?? DocumentLanguage.Turkish,
            lines);
    }

    public async Task<Result<DokumaOrderDto>> CreateAsync(DokumaOrderRequest request, CancellationToken cancellationToken)
    {
        if (await ValidateReferencesAsync(request, null, cancellationToken) is { } error)
        {
            return error;
        }

        var customerCurrency = await CustomerCurrencyAsync(request.CustomerId, cancellationToken);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var year = request.OrderDate.Year;
        var sequence = await numbers.NextAsync(OrderSeries, year, cancellationToken);

        var order = new DokumaOrder { OrderNo = $"DS-{year}-{sequence:D5}" };
        Apply(order, request, customerCurrency);
        foreach (var item in request.Lines)
        {
            var line = new DokumaOrderLine();
            ApplyLine(line, item);
            order.Lines.Add(line);
        }

        db.Set<DokumaOrder>().Add(order);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(order.Id, cancellationToken);
    }

    public async Task<Result<DokumaOrderDto>> UpdateAsync(int id, DokumaOrderRequest request, CancellationToken cancellationToken)
    {
        var order = await db.Set<DokumaOrder>().Include(o => o.Lines).SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
        {
            return NotFound;
        }

        if (order.Status != OrderStatus.Draft)
        {
            return new Error(InvalidState, "Yalnızca taslak sipariş düzenlenebilir.", ErrorType.Conflict);
        }

        var foreign = request.Lines.FirstOrDefault(l => l.Id is { } lineId && order.Lines.All(e => e.Id != lineId));
        if (foreign is not null)
        {
            return ReferenceValidation.InvalidReference("lines", "Satırlardan biri bu siparişe ait değil.");
        }

        if (await ValidateReferencesAsync(request, order, cancellationToken) is { } error)
        {
            return error;
        }

        // Çözgü emri (iptal edilmiş olsa da) satıra bağlıdır: satır silinemez, artikeli değişemez.
        var warped = await LinesWithWarpOrdersAsync(id, cancellationToken);
        if (warped.Count > 0)
        {
            if (order.Lines.Any(l => warped.Contains(l.Id) && request.Lines.All(r => r.Id != l.Id)))
            {
                return new Error(InvalidState, "Çözgü emri açılmış satır siparişten çıkarılamaz (iptal edilmiş çözgü emri de satıra bağlıdır).", ErrorType.Conflict);
            }

            if (request.Lines.Any(r => r.Id is { } lineId && warped.Contains(lineId) && order.Lines.Single(l => l.Id == lineId).ArticleId != r.ArticleId))
            {
                return new Error(InvalidState, "Çözgü emri açılmış satırın artikeli değiştirilemez (iptal edilmiş çözgü emri de satıra bağlıdır); yalnızca metre, fiyat ve not düzeltilebilir.", ErrorType.Conflict);
            }
        }

        Apply(order, request, await CustomerCurrencyAsync(request.CustomerId, cancellationToken));

        db.Set<DokumaOrderLine>().RemoveRange(order.Lines.Where(l => request.Lines.All(r => r.Id != l.Id)).ToList());
        foreach (var item in request.Lines)
        {
            if (item.Id is { } lineId)
            {
                ApplyLine(order.Lines.Single(l => l.Id == lineId), item);
            }
            else
            {
                var line = new DokumaOrderLine();
                ApplyLine(line, item);
                order.Lines.Add(line);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<Result<DokumaOrderDto>> ConfirmAsync(int id, CancellationToken cancellationToken)
    {
        var order = await db.Set<DokumaOrder>().SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
        {
            return NotFound;
        }

        if (order.Status != OrderStatus.Draft)
        {
            return new Error(InvalidState, "Yalnızca taslak sipariş onaylanabilir.", ErrorType.Conflict);
        }

        // İplik ihtiyacı hesaplanabilsin diye satırdaki her artikelin çözgü ve atkı ipliği tanımlı olmalıdır.
        var incomplete = await db.Set<DokumaOrderLine>()
            .Where(l => l.OrderId == id
                && (!l.Article.Yarns.Any(y => y.Role == ArticleYarnRole.Warp) || !l.Article.Yarns.Any(y => y.Role == ArticleYarnRole.Weft)))
            .Select(l => l.Article.ArticleCode)
            .Distinct()
            .ToListAsync(cancellationToken);
        if (incomplete.Count > 0)
        {
            return new Error(
                InvalidState,
                $"Artikel kartında çözgü ya da atkı ipliği tanımlı değil: {string.Join(", ", incomplete)}. Önce artikelin iplik bilgisini tamamlayın.",
                ErrorType.Conflict);
        }

        order.Status = OrderStatus.Confirmed;
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    /// <summary>Onaylı siparişi düzeltmek için taslağa döndürür; numara korunur.</summary>
    public async Task<Result<DokumaOrderDto>> ReopenAsync(int id, CancellationToken cancellationToken)
    {
        var order = await db.Set<DokumaOrder>().SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
        {
            return NotFound;
        }

        if (order.Status != OrderStatus.Confirmed)
        {
            return new Error(InvalidState, "Yalnızca onaylı sipariş taslağa alınabilir.", ErrorType.Conflict);
        }

        if (await HasWarpOrdersAsync(id, cancellationToken))
        {
            return new Error(InvalidState, InWarpMessage, ErrorType.Conflict);
        }

        order.Status = OrderStatus.Draft;
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<Result<DokumaOrderDto>> CancelAsync(int id, CancellationToken cancellationToken)
    {
        var order = await db.Set<DokumaOrder>().SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
        {
            return NotFound;
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            return new Error(InvalidState, "Sipariş zaten iptal edilmiş.", ErrorType.Conflict);
        }

        if (order.Status == OrderStatus.Draft)
        {
            return new Error(InvalidState, "Taslak sipariş iptal edilmez, silinir.", ErrorType.Conflict);
        }

        if (await HasWarpOrdersAsync(id, cancellationToken))
        {
            return new Error(InvalidState, InWarpMessage, ErrorType.Conflict);
        }

        order.Status = OrderStatus.Cancelled;
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var order = await db.Set<DokumaOrder>().Include(o => o.Lines).SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
        {
            return NotFound;
        }

        if (order.Status != OrderStatus.Draft)
        {
            return new Error(InvalidState, "Yalnızca taslak sipariş silinebilir; onaylanmış sipariş iptal edilir.", ErrorType.Conflict);
        }

        // Taslağa alınmış siparişte iptal edilmiş çözgü emirleri kalabilir; bunlar satıra bağlı olduğundan sipariş silinemez.
        if (await db.Set<DokumaWarpOrder>().AnyAsync(w => w.OrderLine.OrderId == id, cancellationToken))
        {
            return new Error(InvalidState, "Çözgü emri açılmış sipariş silinemez (iptal edilmiş çözgü emri de siparişe bağlıdır); siparişi onaylayıp iptal edin.", ErrorType.Conflict);
        }

        db.Set<DokumaOrderLine>().RemoveRange(order.Lines);
        db.Set<DokumaOrder>().Remove(order);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    /// <summary>
    /// Siparişin iplik ihtiyacı: artikellerin çözgü ve atkı tanımından, net ve artikeldeki fireyle. Eldeki miktar malın
    /// sahibine ait iplik girişlerinin toplamıdır (fasonda müşterinin, kendi malında işletmenin). Aynı sahibin diğer onaylı
    /// siparişlerinin ihtiyacı bilgi olarak düşülür; kesin düşüm çözgüye iplik verilirken lottan yapılır.
    /// </summary>
    public async Task<Result<DokumaOrderRequirementsDto>> GetRequirementsAsync(int id, CancellationToken cancellationToken)
    {
        var order = await db.Set<DokumaOrder>().AsNoTracking()
            .Include(o => o.Customer)
            .SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
        {
            return NotFound;
        }

        var lines = await LinesWithYarns(db.Set<DokumaOrderLine>().Where(l => l.OrderId == id)).ToListAsync(cancellationToken);
        var missing = new List<string>();
        var needs = YarnNeeds(lines.Select(l => (l, l.Meters)), missing);

        // Malın sahibi: fasonda müşteri, kendi malında işletme (boş).
        var owner = order.Ownership == OrderOwnership.Commission ? order.CustomerId : null;
        var yarnIds = needs.Keys.ToList();
        // Eldeki: lotların kalanı (girişten çözgüye ve atkıya verilenler düşülmüş).
        var lots = db.Set<DokumaYarnLot>().AsNoTracking().Where(l => yarnIds.Contains(l.YarnId) && l.CustomerId == owner);
        var received = await lots.GroupBy(l => l.YarnId)
            .Select(g => new { YarnId = g.Key, Kilograms = g.Sum(l => l.Kilograms) })
            .ToDictionaryAsync(g => g.YarnId, g => g.Kilograms, cancellationToken);
        var consumed = await DokumaYarnStock.ConsumedByYarnAsync(db, yarnIds, owner, cancellationToken);

        var otherLines = await LinesWithYarns(db.Set<DokumaOrderLine>().Where(l =>
                l.OrderId != id
                && l.Order.Status == OrderStatus.Confirmed
                && (owner == null
                    ? l.Order.Ownership == OrderOwnership.OwnGoods
                    : l.Order.Ownership == OrderOwnership.Commission && l.Order.CustomerId == owner)
                && l.Article.Yarns.Any(y => yarnIds.Contains(y.YarnId))))
            .ToListAsync(cancellationToken);

        // Siparişe verilen iplik ihtiyacından düşer: kalan ihtiyaç sipariş başına, sıfırın altına inmeden hesaplanır.
        var orderIds = otherLines.Select(l => l.OrderId).Append(id).Distinct().ToList();
        var issued = await DokumaYarnStock.IssuedByOrderAsync(db, orderIds, yarnIds, cancellationToken);
        var others = otherLines
            .GroupBy(l => l.OrderId)
            .SelectMany(g => YarnNeeds(g.Select(l => (l, l.Meters)), []).Values.Select(n => (n.Yarn.Id, Remaining: Math.Max(0, n.WithWaste - issued.GetValueOrDefault((g.Key, n.Yarn.Id))))))
            .GroupBy(x => x.Id)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Remaining));

        var yarns = needs.Values
            .OrderBy(n => n.Yarn.YarnCode)
            .Select(n =>
            {
                var required = Math.Round(n.WithWaste, 2);
                var given = issued.GetValueOrDefault((id, n.Yarn.Id));
                var remaining = Math.Max(0, required - given);
                var have = received.GetValueOrDefault(n.Yarn.Id) - consumed.GetValueOrDefault(n.Yarn.Id);
                var other = Math.Round(others.GetValueOrDefault(n.Yarn.Id), 2);
                var free = have - other;
                return new DokumaYarnRequirementDto(
                    n.Yarn.Id, n.Yarn.YarnCode, n.Yarn.Name, n.Yarn.Color, n.Warp, n.Weft,
                    Math.Round(n.Net, 2), required, given, have, other, free, Math.Max(0, remaining - Math.Max(free, 0)));
            })
            .ToList();

        return new DokumaOrderRequirementsDto(
            order.Id,
            order.Ownership == OrderOwnership.Commission ? order.Customer?.Name : null,
            yarns,
            missing.Distinct().ToList());
    }

    /// <summary>
    /// Gerçek fire: siparişe verilen iplik ile dokunan metre için hesaplanan net iplik arasındaki fark. Müşteri kartındaki
    /// kabul edilen fire ile karşılaştırılır, engel olmaz.
    /// </summary>
    public async Task<Result<DokumaOrderWasteDto>> GetWasteAsync(int id, CancellationToken cancellationToken)
    {
        var order = await db.Set<DokumaOrder>().AsNoTracking().Include(o => o.Customer).SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
        {
            return NotFound;
        }

        var lines = await LinesWithYarns(db.Set<DokumaOrderLine>().Where(l => l.OrderId == id)).ToListAsync(cancellationToken);
        var lineIds = lines.Select(l => l.Id).ToList();
        var woven = await db.Set<DokumaFabricRoll>().AsNoTracking()
            .Where(r => lineIds.Contains(r.Beam.WarpOrder.OrderLineId))
            .GroupBy(r => r.Beam.WarpOrder.OrderLineId)
            .Select(g => new { LineId = g.Key, Meters = g.Sum(r => r.Meters) })
            .ToDictionaryAsync(g => g.LineId, g => g.Meters, cancellationToken);
        var allFinished = !await db.Set<DokumaBeam>().AnyAsync(b => lineIds.Contains(b.WarpOrder.OrderLineId) && b.Status != BeamStatus.Finished, cancellationToken)
            && await db.Set<DokumaBeam>().AnyAsync(b => lineIds.Contains(b.WarpOrder.OrderLineId), cancellationToken);

        var theoretical = YarnNeeds(lines.Select(l => (l, woven.GetValueOrDefault(l.Id))), []);
        var yarnIds = theoretical.Keys.ToList();
        var issued = await DokumaYarnStock.IssuedByOrderAsync(db, [id], yarnIds, cancellationToken);

        var yarns = theoretical.Values.OrderBy(n => n.Yarn.YarnCode).Select(n =>
        {
            var given = issued.GetValueOrDefault((id, n.Yarn.Id));
            var net = Math.Round(n.Net, 2);
            var waste = given - net;
            return new DokumaYarnWasteDto(n.Yarn.Id, n.Yarn.YarnCode, n.Yarn.Name, given, net, waste, given > 0 ? Math.Round(waste / given * 100, 2) : 0);
        }).ToList();

        var totalIssued = yarns.Sum(y => y.IssuedKilograms);
        var totalPercent = totalIssued > 0 ? Math.Round(yarns.Sum(y => y.WasteKilograms) / totalIssued * 100, 2) : 0;
        var tolerance = order.Ownership == OrderOwnership.Commission ? order.Customer?.FireTolerancePercent : null;

        return new DokumaOrderWasteDto(id, woven.Values.Sum(), allFinished, tolerance, totalPercent, tolerance is { } limit && totalPercent > limit, yarns);
    }

    private static IQueryable<DokumaOrderLine> LinesWithYarns(IQueryable<DokumaOrderLine> lines) =>
        lines.AsNoTracking().Include(l => l.Article).ThenInclude(a => a.Yarns).ThenInclude(y => y.Yarn);

    private sealed class YarnNeed(DokumaYarn yarn)
    {
        public DokumaYarn Yarn { get; } = yarn;
        public bool Warp { get; set; }
        public bool Weft { get; set; }
        public decimal Net { get; set; }
        public decimal WithWaste { get; set; }
    }

    /// <summary>Satırların iplik başına net ve fireli ihtiyacı; iplik tanımı eksik artikel <paramref name="missing"/> listesine yazılır.</summary>
    private static Dictionary<int, YarnNeed> YarnNeeds(IEnumerable<(DokumaOrderLine Line, decimal Meters)> lines, List<string> missing)
    {
        var needs = new Dictionary<int, YarnNeed>();
        foreach (var (line, meters) in lines)
        {
            var article = line.Article;
            var warp = article.Yarns.Where(y => y.Role == ArticleYarnRole.Warp).ToList();
            var weft = article.Yarns.Where(y => y.Role == ArticleYarnRole.Weft).ToList();
            if (warp.Count == 0 || weft.Count == 0)
            {
                missing.Add(article.ArticleCode);
                continue;
            }

            var weftTotal = weft.Sum(y => y.Ends);
            foreach (var item in warp)
            {
                var tex = YarnRequirement.Tex(item.Yarn.CountValue, item.Yarn.CountSystem, item.Yarn.Plies);
                var net = YarnRequirement.WarpKilogramsPerMeter(item.Ends, article.WarpCrimpPercent, tex) * meters;
                Add(item.Yarn, warp: true, net, article.WarpWastePercent);
            }

            foreach (var item in weft)
            {
                var tex = YarnRequirement.Tex(item.Yarn.CountValue, item.Yarn.CountSystem, item.Yarn.Plies);
                var share = (decimal)item.Ends / weftTotal;
                var net = YarnRequirement.WeftKilogramsPerMeter(article.WeftDensityPerCm, article.ReedWidthCm, article.WeftCrimpPercent, tex, share) * meters;
                Add(item.Yarn, warp: false, net, article.WeftWastePercent);
            }
        }

        return needs;

        void Add(DokumaYarn yarn, bool warp, decimal net, decimal? wastePercent)
        {
            if (!needs.TryGetValue(yarn.Id, out var need))
            {
                need = new YarnNeed(yarn);
                needs[yarn.Id] = need;
            }

            need.Warp |= warp;
            need.Weft |= !warp;
            need.Net += net;
            need.WithWaste += net * (1 + (wastePercent ?? 0) / 100m);
        }
    }

    /// <summary>Müşteri varsa onun para birimi; stoğa üretimde yerel para.</summary>
    private async Task<string> CustomerCurrencyAsync(int? customerId, CancellationToken cancellationToken) =>
        customerId is { } id
            ? await db.Set<DokumaCustomer>().Where(c => c.Id == id).Select(c => c.Currency).SingleAsync(cancellationToken)
            : "TRY";

    private async Task<Error?> ValidateReferencesAsync(DokumaOrderRequest request, DokumaOrder? existing, CancellationToken cancellationToken)
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

        // Siparişte zaten bulunan pasif artikel kalabilir; yeni seçilen artikel etkin olmalıdır.
        return await ReferenceValidation.RequireSelectableAsync(
            db.Set<DokumaArticle>(),
            request.Lines.Select(l => l.ArticleId).Distinct().ToList(),
            existing?.Lines.Select(l => l.ArticleId).ToHashSet() ?? [],
            "lines",
            "Artikel",
            cancellationToken);
    }

    private static void Apply(DokumaOrder order, DokumaOrderRequest request, string customerCurrency)
    {
        order.OrderDate = request.OrderDate;
        order.CustomerId = request.CustomerId;
        order.CustomerOrderNo = NullIfBlank(request.CustomerOrderNo);
        order.Ownership = request.Ownership;
        order.DueDate = request.DueDate;
        order.Currency = NullIfBlank(request.Currency)?.ToUpperInvariant() ?? customerCurrency;
        order.Notes = NullIfBlank(request.Notes);
    }

    private static void ApplyLine(DokumaOrderLine line, DokumaOrderLineRequest item)
    {
        line.ArticleId = item.ArticleId;
        line.Meters = item.Meters;
        line.UnitPrice = item.UnitPrice;
        line.Notes = NullIfBlank(item.Notes);
        line.CustomerArticleCode = NullIfBlank(item.CustomerArticleCode);
    }
}
