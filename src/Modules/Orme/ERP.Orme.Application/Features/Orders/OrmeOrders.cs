using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Core.Domain.Enums;
using ERP.Orme.Application.Features.Receipts;
using ERP.Orme.Domain.Entities;
using ERP.Orme.Domain.Enums;
using ERP.Orme.Domain.Rules;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Orme.Application.Features.Orders;

public sealed class OrmeOrderQuery : PageRequest
{
    /// <summary>Sipariş no, müşteri sipariş no ya da müşteri adında aranır.</summary>
    public string? Search { get; set; }
}

public sealed record OrmeOrderListItemDto(
    int Id,
    string OrderNo,
    DateOnly OrderDate,
    string? CustomerName,
    string? CustomerOrderNo,
    OrderOwnership Ownership,
    DateOnly? DueDate,
    int LineCount,
    decimal TotalKilograms,
    decimal TotalAmount,
    string Currency,
    OrderStatus Status);

/// <param name="EstimatedMeters">Gramaj ve enden tahmini metre; en bilinmiyorsa boş.</param>
/// <param name="Amount">Kilo × birim fiyat; fiyat girilmemişse boş.</param>
/// <param name="PlannedKilograms">İptal edilmemiş örme emirlerinin planı.</param>
/// <param name="KnittedKilograms">Bu satırın emirlerinden çıkan topların toplamı.</param>
/// <param name="CustomerFabricCode">Müşterinin bu kumaşa verdiği kendi kodu.</param>
public sealed record OrmeOrderLineDto(
    int Id,
    int FabricId,
    string FabricCode,
    string FabricName,
    string? CustomerFabricCode,
    decimal Kilograms,
    decimal? EstimatedMeters,
    decimal? UnitPrice,
    decimal? Amount,
    decimal PlannedKilograms,
    decimal KnittedKilograms,
    string? Notes);

public sealed record OrmeOrderDto(
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
    decimal TotalKilograms,
    decimal TotalAmount,
    DateTime CreatedAt,
    string? CreatedBy,
    IReadOnlyList<OrmeOrderLineDto> Lines);

/// <param name="Id">Var olan satır güncelleniyorsa Id'si; yeni satırda boş.</param>
/// <param name="UnitPrice">Kilo başına fiyat; henüz belli değilse boş.</param>
/// <param name="CustomerFabricCode">Müşterinin bu kumaşa verdiği kendi kodu; belgelerde basılır.</param>
public sealed record OrmeOrderLineRequest(int? Id, int FabricId, decimal Kilograms, decimal? UnitPrice = null, string? Notes = null, string? CustomerFabricCode = null);

/// <param name="CustomerId">Fason işte zorunlu; kendi malında boşsa stoğa üretimdir.</param>
/// <param name="Currency">Boşsa müşterinin para birimi, müşteri yoksa TRY kullanılır.</param>
public sealed record OrmeOrderRequest(
    int? CustomerId,
    DateOnly OrderDate,
    IReadOnlyList<OrmeOrderLineRequest> Lines,
    OrderOwnership Ownership = OrderOwnership.Commission,
    string? CustomerOrderNo = null,
    DateOnly? DueDate = null,
    string? Currency = null,
    string? Notes = null);

public sealed class OrmeOrderRequestValidator : AbstractValidator<OrmeOrderRequest>
{
    public const int MaxLines = 100;

    public OrmeOrderRequestValidator()
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
                    if (line.FabricId <= 0)
                    {
                        context.AddFailure($"{i + 1}. satır: kumaş seçilmelidir.");
                    }

                    if (line.Kilograms is <= 0 or > 9_999_999)
                    {
                        context.AddFailure($"{i + 1}. satır: kilo sıfırdan büyük olmalıdır.");
                    }

                    if (line.UnitPrice is < 0)
                    {
                        context.AddFailure($"{i + 1}. satır: birim fiyat negatif olamaz.");
                    }

                    if (line.Notes?.Trim().Length > 500)
                    {
                        context.AddFailure($"{i + 1}. satır: not en fazla 500 karakter olabilir.");
                    }

                    if (line.CustomerFabricCode?.Trim().Length > 50)
                    {
                        context.AddFailure($"{i + 1}. satır: müşteri kumaş kodu en fazla 50 karakter olabilir.");
                    }
                }
            });
    }
}

/// <param name="NetKilograms">Siparişin bu iplikten fire hariç ihtiyacı.</param>
/// <param name="RequiredKilograms">Kumaş kartındaki fire eklenmiş ihtiyaç.</param>
/// <param name="IssuedKilograms">Bu siparişe verilen net iplik; kalan ihtiyaçtan düşer.</param>
/// <param name="AvailableKilograms">Malın sahibine ait lotların kalanı.</param>
/// <param name="OtherOrdersKilograms">Aynı sahibin diğer onaylı siparişlerinin henüz verilmemiş fireli ihtiyacı.</param>
/// <param name="FreeKilograms">Eldeki miktardan diğer siparişlere gereken düşüldükten sonra kalan (eksi olabilir).</param>
/// <param name="ShortageKilograms">İhtiyaç serbest miktarı aşıyorsa eksik; aksi halde sıfır.</param>
public sealed record OrmeYarnRequirementDto(
    int YarnId,
    string YarnCode,
    string YarnName,
    string? YarnColor,
    decimal NetKilograms,
    decimal RequiredKilograms,
    decimal IssuedKilograms,
    decimal AvailableKilograms,
    decimal OtherOrdersKilograms,
    decimal FreeKilograms,
    decimal ShortageKilograms);

/// <param name="MissingFabrics">İplik tanımı eksik olduğu için hesaba katılamayan kumaşlar.</param>
public sealed record OrmeOrderRequirementsDto(int OrderId, string? OwnerName, IReadOnlyList<OrmeYarnRequirementDto> Yarns, IReadOnlyList<string> MissingFabrics);

/// <param name="IssuedKilograms">Siparişe verilen net iplik.</param>
/// <param name="TheoreticalKilograms">Örülen kilonun gerektirdiği net iplik.</param>
public sealed record OrmeYarnWasteDto(int YarnId, string YarnCode, string YarnName, decimal IssuedKilograms, decimal TheoreticalKilograms, decimal WasteKilograms, decimal WastePercent);

/// <param name="AllKnitOrdersCompleted">Bütün emirler bittiyse fire kesindir; sürerken yüksek görünür.</param>
/// <param name="TolerancePercent">Müşteri kartında kabul edilen iplik firesi; fasonda karşılaştırılır.</param>
public sealed record OrmeOrderWasteDto(int OrderId, decimal KnittedKilograms, bool AllKnitOrdersCompleted, decimal? TolerancePercent, decimal TotalWastePercent, bool ExceedsTolerance, IReadOnlyList<OrmeYarnWasteDto> Yarns);

/// <param name="Amount">Kilo × birim fiyat; fiyat girilmemişse boş.</param>
public sealed record OrmeProformaLineDto(int Id, string FabricCode, string FabricName, string? CustomerFabricCode, decimal Kilograms, decimal? UnitPrice, decimal? Amount);

/// <summary>Proforma / sipariş teyidi: müşteriye gönderilen, siparişin satırlarını ve tutarını gösteren belge.</summary>
/// <param name="CustomerAddress">Müşterinin varsayılan fatura adresi.</param>
/// <param name="VatRate">Müşterinin varsayılan KDV oranı; boşsa KDV sıfır hesaplanır, belgede not düşülür.</param>
/// <param name="HasUnpricedLines">Fiyatı girilmemiş satır var; tutara katılmaz.</param>
public sealed record OrmeProformaDto(
    int OrderId,
    string OrderNo,
    OrderStatus Status,
    DateOnly OrderDate,
    DateOnly? DueDate,
    string? CustomerOrderNo,
    string Currency,
    string? Notes,
    string CustomerCode,
    string CustomerName,
    string? CustomerTaxNumber,
    string? CustomerTaxOffice,
    string? CustomerAddress,
    DocumentLanguage DocumentLanguage,
    int? PaymentTermDays,
    decimal? VatRate,
    decimal TotalKilograms,
    decimal Subtotal,
    decimal VatAmount,
    decimal Total,
    bool HasUnpricedLines,
    IReadOnlyList<OrmeProformaLineDto> Lines);

public interface IOrmeOrderService
{
    Task<PagedResult<OrmeOrderListItemDto>> GetListAsync(OrmeOrderQuery query, CancellationToken cancellationToken);
    Task<Result<OrmeOrderDto>> GetAsync(int id, CancellationToken cancellationToken);
    Task<Result<OrmeOrderDto>> CreateAsync(OrmeOrderRequest request, CancellationToken cancellationToken);
    Task<Result<OrmeOrderDto>> UpdateAsync(int id, OrmeOrderRequest request, CancellationToken cancellationToken);
    Task<Result<OrmeOrderDto>> ConfirmAsync(int id, CancellationToken cancellationToken);
    Task<Result<OrmeOrderDto>> ReopenAsync(int id, CancellationToken cancellationToken);
    Task<Result<OrmeOrderDto>> CancelAsync(int id, CancellationToken cancellationToken);
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken);
    Task<Result<OrmeOrderRequirementsDto>> GetRequirementsAsync(int id, CancellationToken cancellationToken);
    Task<Result<OrmeOrderWasteDto>> GetWasteAsync(int id, CancellationToken cancellationToken);
    Task<Result<OrmeProformaDto>> GetProformaAsync(int id, CancellationToken cancellationToken);
}

public sealed class OrmeOrderService(IAppDbContext db, IDocumentNumberGenerator numbers) : IOrmeOrderService
{
    /// <summary>Sipariş numara serisi; tekliften açılan sipariş de aynı seriden numara alır.</summary>
    public const string OrderSeries = "OrmeOrder";
    private const string InvalidState = "ORME_ORDER_INVALID_STATE";

    /// <summary>Sipariş numarası: OS-yıl-sıra.</summary>
    public static string OrderNoFor(int year, int sequence) => $"OS-{year}-{sequence:D5}";

    private const string InKnittingMessage = "Örme emri açılmış sipariş taslağa alınamaz ve iptal edilemez; önce örme emirlerini iptal edin.";

    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Sipariş bulunamadı.", ErrorType.NotFound);

    private Task<bool> HasKnitOrdersAsync(int orderId, CancellationToken cancellationToken) =>
        db.Set<OrmeKnitOrder>().AnyAsync(k => k.OrderLine.OrderId == orderId && k.Status != KnitOrderStatus.Cancelled, cancellationToken);

    public Task<PagedResult<OrmeOrderListItemDto>> GetListAsync(OrmeOrderQuery query, CancellationToken cancellationToken)
    {
        var orders = db.Set<OrmeOrder>().AsNoTracking();
        if (NullIfBlank(query.Search) is { } term)
        {
            orders = orders.Where(o => o.OrderNo.Contains(term)
                || (o.CustomerOrderNo != null && o.CustomerOrderNo.Contains(term))
                || (o.Customer != null && o.Customer.Name.Contains(term)));
        }

        return orders
            .OrderByDescending(o => o.OrderDate).ThenByDescending(o => o.Id)
            .Select(o => new OrmeOrderListItemDto(
                o.Id,
                o.OrderNo,
                o.OrderDate,
                o.Customer != null ? o.Customer.Name : null,
                o.CustomerOrderNo,
                o.Ownership,
                o.DueDate,
                o.Lines.Count,
                o.Lines.Sum(l => (decimal?)l.Kilograms) ?? 0,
                o.Lines.Sum(l => (decimal?)(l.Kilograms * (l.UnitPrice ?? 0))) ?? 0,
                o.Currency,
                o.Status))
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<Result<OrmeOrderDto>> GetAsync(int id, CancellationToken cancellationToken)
    {
        var order = await db.Set<OrmeOrder>().AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.Lines).ThenInclude(l => l.Fabric)
            .SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
        {
            return NotFound;
        }

        var knitting = await db.Set<OrmeKnitOrder>().AsNoTracking()
            .Where(k => k.OrderLine.OrderId == id && k.Status != KnitOrderStatus.Cancelled)
            .GroupBy(k => k.OrderLineId)
            .Select(g => new { LineId = g.Key, Planned = g.Sum(k => k.PlannedKilograms) })
            .ToDictionaryAsync(g => g.LineId, g => g.Planned, cancellationToken);
        var knittedByLine = await db.Set<OrmeFabricRoll>().AsNoTracking()
            .Where(r => r.KnitOrder.OrderLine.OrderId == id)
            .GroupBy(r => r.KnitOrder.OrderLineId)
            .Select(g => new { LineId = g.Key, Kilograms = g.Sum(r => r.Kilograms) })
            .ToDictionaryAsync(g => g.LineId, g => g.Kilograms, cancellationToken);

        var lines = order.Lines.OrderBy(l => l.Id).Select(l => new OrmeOrderLineDto(
            l.Id,
            l.FabricId,
            l.Fabric.FabricCode,
            l.Fabric.Name,
            l.CustomerFabricCode,
            l.Kilograms,
            YarnRequirement.EstimatedMeters(l.Kilograms, l.Fabric.RawWeightGsm, l.Fabric.RawWidthCm),
            l.UnitPrice,
            l.UnitPrice is { } price ? Math.Round(l.Kilograms * price, 2) : null,
            knitting.GetValueOrDefault(l.Id),
            knittedByLine.GetValueOrDefault(l.Id),
            l.Notes)).ToList();

        return new OrmeOrderDto(
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
            lines.Sum(l => l.Kilograms),
            lines.Sum(l => l.Amount ?? 0),
            order.CreatedAt,
            order.CreatedBy,
            lines);
    }

    public async Task<Result<OrmeOrderDto>> CreateAsync(OrmeOrderRequest request, CancellationToken cancellationToken)
    {
        if (await ValidateReferencesAsync(request, null, cancellationToken) is { } error)
        {
            return error;
        }

        var customerCurrency = await CustomerCurrencyAsync(request.CustomerId, cancellationToken);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var year = request.OrderDate.Year;
        var sequence = await numbers.NextAsync(OrderSeries, year, cancellationToken);

        var order = new OrmeOrder { OrderNo = OrderNoFor(year, sequence) };
        Apply(order, request, customerCurrency);
        foreach (var item in request.Lines)
        {
            var line = new OrmeOrderLine();
            ApplyLine(line, item);
            order.Lines.Add(line);
        }

        db.Set<OrmeOrder>().Add(order);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(order.Id, cancellationToken);
    }

    public async Task<Result<OrmeOrderDto>> UpdateAsync(int id, OrmeOrderRequest request, CancellationToken cancellationToken)
    {
        var order = await db.Set<OrmeOrder>().Include(o => o.Lines).SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
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

        // İptal edilmiş emrin kaydı satıra bağlı kalır; bu yüzden emir görmüş satır silinemez.
        var removedLineIds = order.Lines.Where(l => request.Lines.All(r => r.Id != l.Id)).Select(l => l.Id).ToList();
        if (await db.Set<OrmeKnitOrder>().AnyAsync(k => removedLineIds.Contains(k.OrderLineId), cancellationToken))
        {
            return new Error(InvalidState, "Örme emri kaydı olan sipariş satırı silinemez.", ErrorType.Conflict);
        }

        Apply(order, request, await CustomerCurrencyAsync(request.CustomerId, cancellationToken));

        db.Set<OrmeOrderLine>().RemoveRange(order.Lines.Where(l => request.Lines.All(r => r.Id != l.Id)).ToList());
        foreach (var item in request.Lines)
        {
            if (item.Id is { } lineId)
            {
                ApplyLine(order.Lines.Single(l => l.Id == lineId), item);
            }
            else
            {
                var line = new OrmeOrderLine();
                ApplyLine(line, item);
                order.Lines.Add(line);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<Result<OrmeOrderDto>> ConfirmAsync(int id, CancellationToken cancellationToken)
    {
        var order = await db.Set<OrmeOrder>().SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
        {
            return NotFound;
        }

        if (order.Status != OrderStatus.Draft)
        {
            return new Error(InvalidState, "Yalnızca taslak sipariş onaylanabilir.", ErrorType.Conflict);
        }

        // İplik ihtiyacı hesaplanabilsin diye satırdaki her kumaşın ipliği tanımlı olmalıdır.
        var incomplete = await db.Set<OrmeOrderLine>()
            .Where(l => l.OrderId == id && !l.Fabric.Yarns.Any())
            .Select(l => l.Fabric.FabricCode)
            .Distinct()
            .ToListAsync(cancellationToken);
        if (incomplete.Count > 0)
        {
            return new Error(InvalidState, $"Kumaş kartında iplik tanımlı değil: {string.Join(", ", incomplete)}. Önce kumaşın iplik bilgisini tamamlayın.", ErrorType.Conflict);
        }

        order.Status = OrderStatus.Confirmed;
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    /// <summary>Onaylı siparişi düzeltmek için taslağa döndürür; numara korunur.</summary>
    public async Task<Result<OrmeOrderDto>> ReopenAsync(int id, CancellationToken cancellationToken)
    {
        var order = await db.Set<OrmeOrder>().SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
        {
            return NotFound;
        }

        if (order.Status != OrderStatus.Confirmed)
        {
            return new Error(InvalidState, "Yalnızca onaylı sipariş taslağa alınabilir.", ErrorType.Conflict);
        }

        if (await HasKnitOrdersAsync(id, cancellationToken))
        {
            return new Error(InvalidState, InKnittingMessage, ErrorType.Conflict);
        }

        order.Status = OrderStatus.Draft;
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<Result<OrmeOrderDto>> CancelAsync(int id, CancellationToken cancellationToken)
    {
        var order = await db.Set<OrmeOrder>().SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
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

        if (await HasKnitOrdersAsync(id, cancellationToken))
        {
            return new Error(InvalidState, InKnittingMessage, ErrorType.Conflict);
        }

        order.Status = OrderStatus.Cancelled;
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var order = await db.Set<OrmeOrder>().Include(o => o.Lines).SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
        {
            return NotFound;
        }

        if (order.Status != OrderStatus.Draft)
        {
            return new Error(InvalidState, "Yalnızca taslak sipariş silinebilir; onaylanmış sipariş iptal edilir.", ErrorType.Conflict);
        }

        if (await db.Set<OrmeKnitOrder>().AnyAsync(k => k.OrderLine.OrderId == id, cancellationToken))
        {
            return new Error(InvalidState, "Örme emri kaydı olan sipariş silinemez.", ErrorType.Conflict);
        }

        db.Set<OrmeOrderLine>().RemoveRange(order.Lines);
        db.Set<OrmeOrder>().Remove(order);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    /// <summary>
    /// Siparişin iplik ihtiyacı: kumaş kilosu × iplik oranı, net ve kumaştaki fireyle. Eldeki miktar malın sahibine ait lotların
    /// kalanıdır (fasonda müşterinin, kendi malında işletmenin). Siparişe verilen iplik ihtiyaçtan düşer; aynı sahibin diğer onaylı
    /// siparişlerinin henüz verilmemiş ihtiyacı bilgi olarak düşülür.
    /// </summary>
    public async Task<Result<OrmeOrderRequirementsDto>> GetRequirementsAsync(int id, CancellationToken cancellationToken)
    {
        var order = await db.Set<OrmeOrder>().AsNoTracking()
            .Include(o => o.Customer)
            .SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
        {
            return NotFound;
        }

        var lines = await LinesWithYarns(db.Set<OrmeOrderLine>().Where(l => l.OrderId == id)).ToListAsync(cancellationToken);
        var missing = new List<string>();
        var needs = YarnNeeds(lines.Select(l => (l, l.Kilograms)), missing);

        // Malın sahibi: fasonda müşteri, kendi malında işletme (boş).
        var owner = order.Ownership == OrderOwnership.Commission ? order.CustomerId : null;
        var yarnIds = needs.Keys.ToList();
        var received = await db.Set<OrmeYarnLot>().AsNoTracking()
            .Where(l => yarnIds.Contains(l.YarnId) && l.CustomerId == owner)
            .GroupBy(l => l.YarnId)
            .Select(g => new { YarnId = g.Key, Kilograms = g.Sum(l => l.Kilograms) })
            .ToDictionaryAsync(g => g.YarnId, g => g.Kilograms, cancellationToken);

        var consumed = await OrmeYarnStock.ConsumedByYarnAsync(db, yarnIds, owner, cancellationToken);

        var otherLines = await LinesWithYarns(db.Set<OrmeOrderLine>().Where(l =>
                l.OrderId != id
                && l.Order.Status == OrderStatus.Confirmed
                && (owner == null
                    ? l.Order.Ownership == OrderOwnership.OwnGoods
                    : l.Order.Ownership == OrderOwnership.Commission && l.Order.CustomerId == owner)
                && l.Fabric.Yarns.Any(y => yarnIds.Contains(y.YarnId))))
            .ToListAsync(cancellationToken);

        // Siparişe verilen iplik ihtiyacından düşer: kalan ihtiyaç sipariş başına, sıfırın altına inmeden hesaplanır.
        var orderIds = otherLines.Select(l => l.OrderId).Append(id).Distinct().ToList();
        var issued = await OrmeYarnStock.IssuedByOrderAsync(db, orderIds, yarnIds, cancellationToken);
        var others = otherLines
            .GroupBy(l => l.OrderId)
            .SelectMany(g => YarnNeeds(g.Select(l => (l, l.Kilograms)), []).Values.Select(n => (n.Yarn.Id, Remaining: Math.Max(0, n.WithWaste - issued.GetValueOrDefault((g.Key, n.Yarn.Id))))))
            .GroupBy(x => x.Id)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Remaining));

        var yarns = needs.Values
            .OrderBy(n => n.Yarn.YarnCode)
            .Select(n =>
            {
                var required = Math.Round(n.WithWaste, 2, MidpointRounding.AwayFromZero);
                var given = issued.GetValueOrDefault((id, n.Yarn.Id));
                var remaining = Math.Max(0, required - given);
                var have = received.GetValueOrDefault(n.Yarn.Id) - consumed.GetValueOrDefault(n.Yarn.Id);
                var other = Math.Round(others.GetValueOrDefault(n.Yarn.Id), 2, MidpointRounding.AwayFromZero);
                var free = have - other;
                return new OrmeYarnRequirementDto(
                    n.Yarn.Id, n.Yarn.YarnCode, n.Yarn.Name, n.Yarn.Color,
                    Math.Round(n.Net, 2, MidpointRounding.AwayFromZero), required, given, have, other, free, Math.Max(0, remaining - Math.Max(free, 0)));
            })
            .ToList();

        return new OrmeOrderRequirementsDto(
            order.Id,
            order.Ownership == OrderOwnership.Commission ? order.Customer?.Name : null,
            yarns,
            missing.Distinct().ToList());
    }

    /// <summary>Gerçek fire: siparişe verilen net iplik ile örülen kilonun gerektirdiği net iplik karşılaştırılır.</summary>
    public async Task<Result<OrmeOrderWasteDto>> GetWasteAsync(int id, CancellationToken cancellationToken)
    {
        var order = await db.Set<OrmeOrder>().AsNoTracking().Include(o => o.Customer).SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
        {
            return NotFound;
        }

        var lines = await LinesWithYarns(db.Set<OrmeOrderLine>().Where(l => l.OrderId == id)).ToListAsync(cancellationToken);
        var lineIds = lines.Select(l => l.Id).ToList();
        var knitted = await db.Set<OrmeFabricRoll>().AsNoTracking()
            .Where(r => lineIds.Contains(r.KnitOrder.OrderLineId))
            .GroupBy(r => r.KnitOrder.OrderLineId)
            .Select(g => new { LineId = g.Key, Kilograms = g.Sum(r => r.Kilograms) })
            .ToDictionaryAsync(g => g.LineId, g => g.Kilograms, cancellationToken);
        var live = db.Set<OrmeKnitOrder>().Where(k => lineIds.Contains(k.OrderLineId) && k.Status != KnitOrderStatus.Cancelled);
        var allCompleted = await live.AnyAsync(cancellationToken) && !await live.AnyAsync(k => k.Status != KnitOrderStatus.Completed, cancellationToken);

        var theoretical = YarnNeeds(lines.Select(l => (l, knitted.GetValueOrDefault(l.Id))), []);

        // Verilen ipliğin tamamı sayılır: reçeteden sonradan çıkarılan ipliğe verilen kilo da fireye girer, kendi satırında görünür.
        var issued = (await OrmeYarnStock.IssuedByOrderAsync(db, [id], null, cancellationToken))
            .ToDictionary(x => x.Key.YarnId, x => x.Value);
        var removedIds = issued.Keys.Where(y => !theoretical.ContainsKey(y)).ToList();
        var removed = removedIds.Count == 0
            ? new List<OrmeYarn>()
            : await db.Set<OrmeYarn>().AsNoTracking().IgnoreQueryFilters().Where(y => removedIds.Contains(y.Id)).ToListAsync(cancellationToken);

        var yarns = theoretical.Values.Select(n => (n.Yarn, n.Net))
            .Concat(removed.Select(y => (Yarn: y, Net: 0m)))
            .OrderBy(n => n.Yarn.YarnCode)
            .Select(n =>
            {
                var given = issued.GetValueOrDefault(n.Yarn.Id);
                var net = Math.Round(n.Net, 2, MidpointRounding.AwayFromZero);
                var waste = given - net;
                return new OrmeYarnWasteDto(n.Yarn.Id, n.Yarn.YarnCode, n.Yarn.Name, given, net, waste, given > 0 ? Math.Round(waste / given * 100, 2, MidpointRounding.AwayFromZero) : 0);
            }).ToList();

        var totalIssued = yarns.Sum(y => y.IssuedKilograms);
        var totalPercent = totalIssued > 0 ? Math.Round(yarns.Sum(y => y.WasteKilograms) / totalIssued * 100, 2, MidpointRounding.AwayFromZero) : 0;
        var tolerance = order.Ownership == OrderOwnership.Commission ? order.Customer?.FireTolerancePercent : null;

        return new OrmeOrderWasteDto(id, knitted.Values.Sum(), allCompleted, tolerance, totalPercent, tolerance is { } limit && totalPercent > limit, yarns);
    }

    /// <summary>
    /// Proforma / sipariş teyidi. Yalnız müşterili, taslak ya da onaylı siparişte; KDV müşterinin varsayılan oranıyla,
    /// oran yoksa sıfır hesaplanır. Dil müşteri kartından gelir.
    /// </summary>
    public async Task<Result<OrmeProformaDto>> GetProformaAsync(int id, CancellationToken cancellationToken)
    {
        var order = await db.Set<OrmeOrder>().AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.Lines).ThenInclude(l => l.Fabric)
            .SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
        {
            return NotFound;
        }

        if (order.Customer is null)
        {
            return new Error(InvalidState, "Stoğa üretim siparişinin proforması olmaz; önce müşteri seçin.", ErrorType.Conflict);
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            return new Error(InvalidState, "İptal edilmiş siparişin proforması basılmaz.", ErrorType.Conflict);
        }

        var address = await db.Set<OrmeCustomerAddress>().AsNoTracking()
            .Where(a => a.CustomerId == order.Customer.Id && a.IsDefaultInvoice)
            .Select(a => a.AddressLine + " " + a.District + "/" + a.City)
            .FirstOrDefaultAsync(cancellationToken);

        var lines = order.Lines.OrderBy(l => l.Id).Select(l => new OrmeProformaLineDto(
            l.Id,
            l.Fabric.FabricCode,
            l.Fabric.Name,
            l.CustomerFabricCode,
            l.Kilograms,
            l.UnitPrice,
            l.UnitPrice is { } price ? InvoiceAmounts.Line(l.Kilograms, price) : null)).ToList();
        var amounts = InvoiceAmounts.Of(lines.Sum(l => l.Amount ?? 0), order.Customer.VatRate, null);
        var customer = order.Customer;

        return new OrmeProformaDto(
            order.Id,
            order.OrderNo,
            order.Status,
            order.OrderDate,
            order.DueDate,
            order.CustomerOrderNo,
            order.Currency,
            order.Notes,
            customer.CustomerCode,
            customer.Name,
            customer.TaxNumber,
            customer.TaxOffice,
            address,
            customer.DocumentLanguage,
            customer.PaymentTermDays,
            customer.VatRate,
            lines.Sum(l => l.Kilograms),
            amounts.Subtotal,
            amounts.Vat,
            amounts.Subtotal + amounts.Vat,
            lines.Any(l => l.UnitPrice is null),
            lines);
    }

    private static IQueryable<OrmeOrderLine> LinesWithYarns(IQueryable<OrmeOrderLine> lines) =>
        lines.AsNoTracking().Include(l => l.Fabric).ThenInclude(a => a.Yarns).ThenInclude(y => y.Yarn);

    private sealed class YarnNeed(OrmeYarn yarn)
    {
        public OrmeYarn Yarn { get; } = yarn;
        public decimal Net { get; set; }
        public decimal WithWaste { get; set; }
    }

    /// <summary>Satırların iplik başına net ve fireli ihtiyacı; iplik tanımı eksik kumaş <paramref name="missing"/> listesine yazılır.</summary>
    private static Dictionary<int, YarnNeed> YarnNeeds(IEnumerable<(OrmeOrderLine Line, decimal Kilograms)> lines, List<string> missing)
    {
        var needs = new Dictionary<int, YarnNeed>();
        foreach (var (line, kilograms) in lines)
        {
            var fabric = line.Fabric;
            if (fabric.Yarns.Count == 0)
            {
                missing.Add(fabric.FabricCode);
                continue;
            }

            foreach (var item in fabric.Yarns)
            {
                if (!needs.TryGetValue(item.YarnId, out var need))
                {
                    need = new YarnNeed(item.Yarn);
                    needs[item.YarnId] = need;
                }

                var net = YarnRequirement.NetKilograms(kilograms, item.Percent);
                need.Net += net;
                need.WithWaste += YarnRequirement.WithWaste(net, fabric.WastePercent);
            }
        }

        return needs;
    }

    /// <summary>Müşteri varsa onun para birimi; stoğa üretimde yerel para.</summary>
    private async Task<string> CustomerCurrencyAsync(int? customerId, CancellationToken cancellationToken) =>
        customerId is { } id
            ? await db.Set<OrmeCustomer>().Where(c => c.Id == id).Select(c => c.Currency).SingleAsync(cancellationToken)
            : "TRY";

    private async Task<Error?> ValidateReferencesAsync(OrmeOrderRequest request, OrmeOrder? existing, CancellationToken cancellationToken)
    {
        if (await ReferenceValidation.RequireSelectableAsync(
                db.Set<OrmeCustomer>(), request.CustomerId, existing?.CustomerId, "customerId", "Müşteri", cancellationToken) is { } customerError)
        {
            return customerError;
        }

        if (await CurrencyRules.RequireActiveAsync(db, request.Currency, existing?.Currency, "currency", cancellationToken) is { } currencyError)
        {
            return currencyError;
        }

        // Siparişte zaten bulunan pasif kumaş kalabilir; yeni seçilen kumaş etkin olmalıdır.
        return await ReferenceValidation.RequireSelectableAsync(
            db.Set<OrmeFabric>(),
            request.Lines.Select(l => l.FabricId).Distinct().ToList(),
            existing?.Lines.Select(l => l.FabricId).ToHashSet() ?? [],
            "lines",
            "Kumaş kartı",
            cancellationToken);
    }

    private static void Apply(OrmeOrder order, OrmeOrderRequest request, string customerCurrency)
    {
        order.OrderDate = request.OrderDate;
        order.CustomerId = request.CustomerId;
        order.CustomerOrderNo = NullIfBlank(request.CustomerOrderNo);
        order.Ownership = request.Ownership;
        order.DueDate = request.DueDate;
        order.Currency = NullIfBlank(request.Currency)?.ToUpperInvariant() ?? customerCurrency;
        order.Notes = NullIfBlank(request.Notes);
    }

    private static void ApplyLine(OrmeOrderLine line, OrmeOrderLineRequest item)
    {
        line.FabricId = item.FabricId;
        line.Kilograms = item.Kilograms;
        line.UnitPrice = item.UnitPrice;
        line.CustomerFabricCode = NullIfBlank(item.CustomerFabricCode);
        line.Notes = NullIfBlank(item.Notes);
    }
}
