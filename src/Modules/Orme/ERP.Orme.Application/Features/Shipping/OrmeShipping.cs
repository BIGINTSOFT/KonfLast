using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Domain.Enums;
using ERP.Orme.Application.Features.Accounts;
using ERP.Orme.Domain.Entities;
using ERP.Orme.Domain.Enums;
using ERP.Orme.Domain.Rules;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Orme.Application.Features.Shipping;

public sealed class OrmeShippingQuery : PageRequest
{
    /// <summary>Belge no ya da müşteri.</summary>
    public string? Search { get; set; }
}

public sealed class OrmeDespatchQuery : PageRequest
{
    /// <summary>İrsaliye no, müşteri ya da müşteri sipariş no.</summary>
    public string? Search { get; set; }

    /// <summary>İrsaliye tarihi en erken.</summary>
    public DateOnly? From { get; set; }

    /// <summary>İrsaliye tarihi en geç.</summary>
    public DateOnly? To { get; set; }
}

public sealed record OrmeDespatchListItemDto(
    int Id,
    string DespatchNo,
    string CustomerName,
    DateOnly DespatchDate,
    string? VehiclePlate,
    int PackingListCount,
    decimal TotalKilograms,
    DespatchStatus Status,
    string? InvoiceNo);

public sealed record OrmeDespatchListDto(int Id, string PackingNo, string OrderNo, int RollCount, decimal Kilograms);

/// <summary>İrsaliye satırı: sipariş satırı ve kalite bazında toplam.</summary>
/// <param name="MetersEstimated">Metrenin bir kısmı tahmindir.</param>
/// <param name="CustomerOrderNo">Müşterinin kendi sipariş numarası.</param>
/// <param name="CustomerFabricCode">Müşterinin sipariş satırındaki kendi kumaş kodu.</param>
public sealed record OrmeDespatchLineDto(
    string OrderNo, string FabricCode, string FabricName, FabricRollStatus Grade, int RollCount, decimal Kilograms, decimal Meters, bool MetersEstimated,
    string? CustomerOrderNo, string? CustomerFabricCode);

/// <summary>İrsaliyedeki top; yükleme kontrolünde okutulan numaralarla karşılaştırılır.</summary>
public sealed record OrmeDespatchRollDto(string RollNo, string PackingNo, string FabricCode, decimal Kilograms);

public sealed record OrmeDespatchDto(
    int Id,
    string DespatchNo,
    int CustomerId,
    string CustomerName,
    string? CustomerTaxNumber,
    DateOnly DespatchDate,
    string? VehiclePlate,
    string? DriverName,
    string? DeliveryAddress,
    string? Notes,
    DespatchStatus Status,
    int? InvoiceId,
    string? InvoiceNo,
    decimal TotalKilograms,
    int TotalRolls,
    IReadOnlyList<OrmeDespatchListDto> PackingLists,
    IReadOnlyList<OrmeDespatchLineDto> Lines,
    IReadOnlyList<OrmeDespatchRollDto> Rolls,
    DocumentLanguage DocumentLanguage);

public sealed record OrmeDespatchCandidateDto(int Id, string PackingNo, int CustomerId, string CustomerName, string OrderNo, int RollCount, decimal Kilograms, bool Selected);

/// <param name="DeliveryAddress">Boşsa müşterinin varsayılan teslimat adresi yazılır.</param>
public sealed record OrmeDespatchRequest(
    int CustomerId,
    DateOnly DespatchDate,
    IReadOnlyList<int> PackingListIds,
    string? VehiclePlate = null,
    string? DriverName = null,
    string? DeliveryAddress = null,
    string? Notes = null);

public sealed class OrmeDespatchRequestValidator : AbstractValidator<OrmeDespatchRequest>
{
    public OrmeDespatchRequestValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0).WithMessage("Müşteri seçilmelidir.");
        RuleFor(x => x.DespatchDate).NotEmpty().WithMessage("İrsaliye tarihi zorunludur.");
        RuleFor(x => x.PackingListIds).NotEmpty().WithMessage("En az bir çeki listesi seçilmelidir.");
        RuleFor(x => x.VehiclePlate).OptionalText(20);
        RuleFor(x => x.DriverName).OptionalText(100);
        RuleFor(x => x.DeliveryAddress).OptionalText(500);
        RuleFor(x => x.Notes).OptionalText(1000);
    }
}

public sealed record OrmeInvoiceListItemDto(
    int Id,
    string InvoiceNo,
    string CustomerName,
    DateOnly InvoiceDate,
    DateOnly? DueDate,
    string Currency,
    decimal Subtotal,
    InvoiceStatus Status);

/// <param name="CustomerOrderNo">Müşterinin kendi sipariş numarası.</param>
/// <param name="CustomerFabricCode">Müşterinin sipariş satırındaki kendi kumaş kodu.</param>
public sealed record OrmeInvoiceLineDto(
    int Id, int OrderLineId, string OrderNo, FabricRollStatus Grade, string Description, decimal Quantity, decimal? UnitPrice, decimal? DiscountPercent, decimal? Amount,
    string? CustomerOrderNo, string? CustomerFabricCode);

public sealed record OrmeInvoicePaymentDto(int PaymentId, string PaymentNo, DateOnly PaymentDate, PaymentMethod Method, decimal Amount);

public sealed record OrmeInvoiceDto(
    int Id,
    string InvoiceNo,
    int CustomerId,
    string CustomerName,
    string? CustomerTaxNumber,
    string? CustomerTaxOffice,
    DateOnly InvoiceDate,
    DateOnly? DueDate,
    string Currency,
    decimal? VatRate,
    decimal? WithholdingPercent,
    string? Notes,
    InvoiceStatus Status,
    decimal Subtotal,
    decimal VatAmount,
    decimal WithholdingAmount,
    decimal PayableAmount,
    decimal PaidAmount,
    bool IsOpening,
    IReadOnlyList<string> DespatchNos,
    IReadOnlyList<int> DespatchIds,
    IReadOnlyList<OrmeInvoiceLineDto> Lines,
    IReadOnlyList<OrmeInvoicePaymentDto> Payments,
    DocumentLanguage DocumentLanguage);

public sealed record OrmeInvoiceCandidateDto(int Id, string DespatchNo, int CustomerId, string CustomerName, DateOnly DespatchDate, decimal Kilograms, bool Selected);

public sealed record OrmeInvoicePriceRequest(int OrderLineId, FabricRollStatus Grade, decimal? UnitPrice, decimal? DiscountPercent);

/// <param name="DueDate">Boşsa müşterinin vadesinden hesaplanır.</param>
/// <param name="VatRate">Boşsa müşterinin varsayılan oranı yazılır; o da yoksa onaydan önce girilmelidir.</param>
/// <param name="Prices">Taslakta değiştirilen fiyat ve indirimler; gönderilmeyen satır siparişteki fiyatı ve müşterinin indirimini alır.</param>
public sealed record OrmeInvoiceRequest(
    int CustomerId,
    DateOnly InvoiceDate,
    IReadOnlyList<int> DespatchIds,
    DateOnly? DueDate = null,
    decimal? VatRate = null,
    decimal? WithholdingPercent = null,
    string? Notes = null,
    IReadOnlyList<OrmeInvoicePriceRequest>? Prices = null);

public sealed class OrmeInvoiceRequestValidator : AbstractValidator<OrmeInvoiceRequest>
{
    public OrmeInvoiceRequestValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0).WithMessage("Müşteri seçilmelidir.");
        RuleFor(x => x.InvoiceDate).NotEmpty().WithMessage("Fatura tarihi zorunludur.");
        RuleFor(x => x.DespatchIds).NotEmpty().WithMessage("En az bir irsaliye seçilmelidir.");
        RuleFor(x => x.DueDate).Must((request, due) => due >= request.InvoiceDate).WithMessage("Vade fatura tarihinden önce olamaz.").When(x => x.DueDate is not null);
        RuleFor(x => x.VatRate).InclusiveBetween(0, 100).WithMessage("KDV oranı 0 ile 100 arasında olmalıdır.").When(x => x.VatRate is not null);
        RuleFor(x => x.WithholdingPercent).InclusiveBetween(0, 100).WithMessage("Tevkifat 0 ile 100 arasında olmalıdır.").When(x => x.WithholdingPercent is not null);
        RuleFor(x => x.Notes).OptionalText(1000);
        RuleForEach(x => x.Prices).ChildRules(p =>
        {
            p.RuleFor(l => l.UnitPrice).InclusiveBetween(0, 99_999_999).WithMessage("Birim fiyat geçersiz.").When(l => l.UnitPrice is not null);
            p.RuleFor(l => l.DiscountPercent).InclusiveBetween(0, 100).WithMessage("İndirim 0 ile 100 arasında olmalıdır.").When(l => l.DiscountPercent is not null);
        });
    }
}

public interface IOrmeShippingService
{
    Task<PagedResult<OrmeDespatchListItemDto>> GetDespatchesAsync(OrmeDespatchQuery query, CancellationToken cancellationToken);
    Task<Result<OrmeDespatchDto>> GetDespatchAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<OrmeDespatchCandidateDto>> GetDespatchCandidatesAsync(int? despatchId, CancellationToken cancellationToken);
    Task<Result<OrmeDespatchDto>> SaveDespatchAsync(int? id, OrmeDespatchRequest request, CancellationToken cancellationToken);
    Task<Result<OrmeDespatchDto>> ConfirmDespatchAsync(int id, CancellationToken cancellationToken);
    Task<Result<OrmeDespatchDto>> ReopenDespatchAsync(int id, CancellationToken cancellationToken);
    Task<Result<OrmeDespatchDto>> CancelDespatchAsync(int id, CancellationToken cancellationToken);
    Task<PagedResult<OrmeInvoiceListItemDto>> GetInvoicesAsync(OrmeShippingQuery query, CancellationToken cancellationToken);
    Task<Result<OrmeInvoiceDto>> GetInvoiceAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<OrmeInvoiceCandidateDto>> GetInvoiceCandidatesAsync(int? invoiceId, CancellationToken cancellationToken);
    Task<Result<OrmeInvoiceDto>> SaveInvoiceAsync(int? id, OrmeInvoiceRequest request, CancellationToken cancellationToken);
    Task<Result<OrmeInvoiceDto>> ConfirmInvoiceAsync(int id, CancellationToken cancellationToken);
    Task<Result<OrmeInvoiceDto>> ReopenInvoiceAsync(int id, CancellationToken cancellationToken);
    Task<Result<OrmeInvoiceDto>> CancelInvoiceAsync(int id, CancellationToken cancellationToken);
}

/// <summary>
/// İrsaliye: aynı müşterinin onaylı çeki listeleri. Fatura: aynı müşterinin onaylı irsaliyeleri, sipariş satırı ve kalite
/// bazında kilo; 2. kalitede müşteri kartındaki indirim gelir. Çıktılar e-belge entegrasyonuna kadar resmî belge değildir.
/// </summary>
public sealed class OrmeShippingService(IAppDbContext db, IDocumentNumberGenerator numbers, IAppLock appLock) : IOrmeShippingService
{
    private const string DespatchSeries = "OrmeDespatch";
    private const string InvoiceSeries = "OrmeInvoice";
    private const string InvalidState = "ORME_SHIPPING_INVALID_STATE";
    private const string InvalidItem = "ORME_SHIPPING_INVALID_ITEM";
    private const int MaxCandidates = 500;

    private static readonly Error DespatchNotFound = new(ErrorCodes.NotFound, "İrsaliye bulunamadı.", ErrorType.NotFound);
    private static readonly Error InvoiceNotFound = new(ErrorCodes.NotFound, "Fatura bulunamadı.", ErrorType.NotFound);

    public Task<PagedResult<OrmeDespatchListItemDto>> GetDespatchesAsync(OrmeDespatchQuery query, CancellationToken cancellationToken)
    {
        var despatches = db.Set<OrmeDespatch>().AsNoTracking();
        if (NullIfBlank(query.Search) is { } term)
        {
            despatches = despatches.Where(d => d.DespatchNo.Contains(term)
                || d.Customer.Name.Contains(term)
                || d.PackingLists.Any(l => l.Order.CustomerOrderNo != null && l.Order.CustomerOrderNo.Contains(term)));
        }

        if (query.From is { } from)
        {
            despatches = despatches.Where(d => d.DespatchDate >= from);
        }

        if (query.To is { } to)
        {
            despatches = despatches.Where(d => d.DespatchDate <= to);
        }

        return despatches
            .OrderByDescending(d => d.Id)
            .Select(d => new OrmeDespatchListItemDto(
                d.Id,
                d.DespatchNo,
                d.Customer.Name,
                d.DespatchDate,
                d.VehiclePlate,
                d.PackingLists.Count,
                d.PackingLists.SelectMany(l => l.Items).Where(i => i.IsActive).Sum(i => (decimal?)i.Kilograms) ?? 0,
                d.Status,
                d.Invoice != null ? d.Invoice.InvoiceNo : null))
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<Result<OrmeDespatchDto>> GetDespatchAsync(int id, CancellationToken cancellationToken)
    {
        var d = await db.Set<OrmeDespatch>().AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Invoice)
            .Include(x => x.PackingLists).ThenInclude(l => l.Order)
            .Include(x => x.PackingLists).ThenInclude(l => l.Items).ThenInclude(i => i.Roll).ThenInclude(r => r.KnitOrder).ThenInclude(k => k.OrderLine).ThenInclude(o => o.Fabric)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (d is null)
        {
            return DespatchNotFound;
        }

        var items = d.PackingLists
            .SelectMany(l => l.Items.Where(i => i.IsActive).Select(i => new { l.Order.OrderNo, l.Order.CustomerOrderNo, l.PackingNo, Item = i, Line = i.Roll.KnitOrder.OrderLine }))
            .ToList();
        return new OrmeDespatchDto(
            d.Id,
            d.DespatchNo,
            d.CustomerId,
            d.Customer.Name,
            d.Customer.TaxNumber,
            d.DespatchDate,
            d.VehiclePlate,
            d.DriverName,
            d.DeliveryAddress,
            d.Notes,
            d.Status,
            d.InvoiceId,
            d.Invoice?.InvoiceNo,
            items.Sum(p => p.Item.Kilograms),
            items.Count,
            d.PackingLists.OrderBy(l => l.PackingNo).Select(l => new OrmeDespatchListDto(
                l.Id, l.PackingNo, l.Order.OrderNo, l.Items.Count(i => i.IsActive), l.Items.Where(i => i.IsActive).Sum(i => i.Kilograms))).ToList(),
            items
                .GroupBy(p => new { p.OrderNo, p.CustomerOrderNo, p.Line.Id, p.Line.Fabric.FabricCode, p.Line.Fabric.Name, p.Line.CustomerFabricCode, p.Item.Grade })
                .OrderBy(g => g.Key.OrderNo).ThenBy(g => g.Key.Id).ThenBy(g => g.Key.Grade)
                .Select(g => new OrmeDespatchLineDto(
                    g.Key.OrderNo, g.Key.FabricCode, g.Key.Name, g.Key.Grade, g.Count(), g.Sum(p => p.Item.Kilograms), g.Sum(p => p.Item.Meters), g.Any(p => p.Item.MetersEstimated),
                    g.Key.CustomerOrderNo, g.Key.CustomerFabricCode))
                .ToList(),
            items
                .OrderBy(p => p.PackingNo).ThenBy(p => p.Item.Roll.RollNo)
                .Select(p => new OrmeDespatchRollDto(p.Item.Roll.RollNo, p.PackingNo, p.Line.Fabric.FabricCode, p.Item.Kilograms))
                .ToList(),
            d.Customer.DocumentLanguage);
    }

    public async Task<IReadOnlyList<OrmeDespatchCandidateDto>> GetDespatchCandidatesAsync(int? despatchId, CancellationToken cancellationToken) =>
        await db.Set<OrmePackingList>().AsNoTracking()
            .Where(l => l.Status == PackingListStatus.Confirmed
                && l.BoyahaneReceiptNo == null
                && (l.Order.CustomerId != null || l.SaleCustomerId != null)
                && (l.DespatchId == null || l.DespatchId == despatchId))
            .OrderBy(l => l.Order.CustomerId ?? l.SaleCustomerId).ThenBy(l => l.Id)
            .Take(MaxCandidates)
            .Select(l => new OrmeDespatchCandidateDto(
                l.Id,
                l.PackingNo,
                (l.Order.CustomerId ?? l.SaleCustomerId)!.Value,
                l.Order.Customer != null ? l.Order.Customer.Name : l.SaleCustomer!.Name,
                l.Order.OrderNo,
                l.Items.Count(i => i.IsActive),
                l.Items.Where(i => i.IsActive).Sum(i => (decimal?)i.Kilograms) ?? 0,
                l.DespatchId != null))
            .ToListAsync(cancellationToken);

    public async Task<Result<OrmeDespatchDto>> SaveDespatchAsync(int? id, OrmeDespatchRequest request, CancellationToken cancellationToken)
    {
        OrmeDespatch? despatch = null;
        if (id is not null)
        {
            despatch = await db.Set<OrmeDespatch>().Include(d => d.PackingLists).SingleOrDefaultAsync(d => d.Id == id, cancellationToken);
            if (despatch is null)
            {
                return DespatchNotFound;
            }

            if (despatch.Status != DespatchStatus.Draft)
            {
                return new Error(InvalidState, "Yalnızca taslak irsaliye düzenlenebilir.", ErrorType.Conflict);
            }
        }

        var ids = request.PackingListIds.Distinct().ToList();
        var lists = await db.Set<OrmePackingList>().Include(l => l.Order).Where(l => ids.Contains(l.Id)).ToListAsync(cancellationToken);
        if (lists.Count != ids.Count)
        {
            return new Error(InvalidItem, "Seçilen çeki listelerinden biri bulunamadı.", ErrorType.Conflict);
        }

        foreach (var list in lists)
        {
            var problem = list.Status != PackingListStatus.Confirmed ? "onaylı değil"
                : list.BoyahaneReceiptNo is not null ? "Boyahane'ye gönderilmiş"
                : (list.Order.CustomerId ?? list.SaleCustomerId) is null ? "stoğa üretim; önce çeki listesinde müşteri seçin"
                : (list.Order.CustomerId ?? list.SaleCustomerId) != request.CustomerId ? "başka müşterinin"
                : list.DespatchId is not null && list.DespatchId != id ? "başka bir irsaliyede"
                : null;
            if (problem is not null)
            {
                return new Error(InvalidItem, $"{list.PackingNo} irsaliyeye alınamaz: {problem}.", ErrorType.Conflict);
            }
        }

        var address = NullIfBlank(request.DeliveryAddress) ?? await db.Set<OrmeCustomerAddress>().AsNoTracking()
            .Where(a => a.CustomerId == request.CustomerId && a.IsDefaultDelivery)
            .Select(a => a.AddressLine + " " + a.District + "/" + a.City)
            .FirstOrDefaultAsync(cancellationToken);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (despatch is null)
        {
            // Numara yılı belge tarihinden alınır: yıl dönümünde önceki yıla kesilen irsaliye o yılın serisine girer.
            var year = request.DespatchDate.Year;
            var sequence = await numbers.NextAsync(DespatchSeries, year, cancellationToken);
            despatch = new OrmeDespatch { DespatchNo = $"OIR-{year}-{sequence:D5}" };
            db.Set<OrmeDespatch>().Add(despatch);
        }

        foreach (var removed in despatch.PackingLists.Where(l => !ids.Contains(l.Id)).ToList())
        {
            despatch.PackingLists.Remove(removed);
        }

        despatch.CustomerId = request.CustomerId;
        despatch.DespatchDate = request.DespatchDate;
        despatch.VehiclePlate = NullIfBlank(request.VehiclePlate);
        despatch.DriverName = NullIfBlank(request.DriverName);
        despatch.DeliveryAddress = address;
        despatch.Notes = NullIfBlank(request.Notes);
        foreach (var list in lists.Where(l => !despatch.PackingLists.Contains(l)))
        {
            despatch.PackingLists.Add(list);
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetDespatchAsync(despatch.Id, cancellationToken);
    }

    public async Task<Result<OrmeDespatchDto>> ConfirmDespatchAsync(int id, CancellationToken cancellationToken)
    {
        var despatch = await db.Set<OrmeDespatch>().SingleOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (despatch is null)
        {
            return DespatchNotFound;
        }

        if (despatch.Status != DespatchStatus.Draft)
        {
            return new Error(InvalidState, "Yalnızca taslak irsaliye onaylanabilir.", ErrorType.Conflict);
        }

        despatch.Status = DespatchStatus.Confirmed;
        await db.SaveChangesAsync(cancellationToken);
        return await GetDespatchAsync(id, cancellationToken);
    }

    public async Task<Result<OrmeDespatchDto>> ReopenDespatchAsync(int id, CancellationToken cancellationToken)
    {
        var despatch = await db.Set<OrmeDespatch>().SingleOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (despatch is null)
        {
            return DespatchNotFound;
        }

        if (despatch.Status != DespatchStatus.Confirmed)
        {
            return new Error(InvalidState, "Yalnızca onaylı irsaliye taslağa alınabilir.", ErrorType.Conflict);
        }

        if (despatch.InvoiceId is not null)
        {
            return new Error(InvalidState, "Faturaya girmiş irsaliye taslağa alınamaz; önce faturadan çıkarın.", ErrorType.Conflict);
        }

        despatch.Status = DespatchStatus.Draft;
        await db.SaveChangesAsync(cancellationToken);
        return await GetDespatchAsync(id, cancellationToken);
    }

    public async Task<Result<OrmeDespatchDto>> CancelDespatchAsync(int id, CancellationToken cancellationToken)
    {
        var despatch = await db.Set<OrmeDespatch>().Include(d => d.PackingLists).SingleOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (despatch is null)
        {
            return DespatchNotFound;
        }

        if (despatch.Status != DespatchStatus.Draft)
        {
            return new Error(InvalidState, "Yalnızca taslak irsaliye iptal edilebilir.", ErrorType.Conflict);
        }

        despatch.Status = DespatchStatus.Cancelled;
        despatch.PackingLists.Clear();
        await db.SaveChangesAsync(cancellationToken);
        return await GetDespatchAsync(id, cancellationToken);
    }

    public Task<PagedResult<OrmeInvoiceListItemDto>> GetInvoicesAsync(OrmeShippingQuery query, CancellationToken cancellationToken)
    {
        var invoices = db.Set<OrmeInvoice>().AsNoTracking().Where(i => i.OpeningAmount == null);
        if (NullIfBlank(query.Search) is { } term)
        {
            invoices = invoices.Where(i => i.InvoiceNo.Contains(term) || i.Customer.Name.Contains(term));
        }

        return invoices
            .OrderByDescending(i => i.Id)
            .Select(i => new OrmeInvoiceListItemDto(
                i.Id,
                i.InvoiceNo,
                i.Customer.Name,
                i.InvoiceDate,
                i.DueDate,
                i.Currency,
                i.Lines.Sum(l => (decimal?)Math.Round(l.Quantity * (l.UnitPrice ?? 0) * (1 - (l.DiscountPercent ?? 0) / 100), 2)) ?? 0,
                i.Status))
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<Result<OrmeInvoiceDto>> GetInvoiceAsync(int id, CancellationToken cancellationToken)
    {
        var invoice = await db.Set<OrmeInvoice>().AsNoTracking()
            .Include(i => i.Customer)
            .Include(i => i.Despatches)
            .Include(i => i.Lines).ThenInclude(l => l.OrderLine).ThenInclude(o => o.Order)
            .SingleOrDefaultAsync(i => i.Id == id, cancellationToken);        if (invoice is null)
        {
            return InvoiceNotFound;
        }

        var lines = invoice.Lines.OrderBy(l => l.Id).Select(l => new OrmeInvoiceLineDto(
            l.Id, l.OrderLineId, l.OrderLine.Order.OrderNo, l.Grade, l.Description, l.Quantity, l.UnitPrice, l.DiscountPercent,
            l.UnitPrice is { } price ? InvoiceAmounts.Line(l.Quantity, price, l.DiscountPercent) : null,
            l.OrderLine.Order.CustomerOrderNo, l.OrderLine.CustomerFabricCode)).ToList();
        var amounts = InvoiceAmounts.Of(invoice.OpeningAmount ?? lines.Sum(l => l.Amount ?? 0), invoice.VatRate, invoice.WithholdingPercent);
        var payments = await db.Set<OrmePaymentAllocation>().AsNoTracking()
            .Where(a => a.InvoiceId == id)
            .OrderBy(a => a.Id)
            .Select(a => new OrmeInvoicePaymentDto(a.PaymentId, a.Payment.PaymentNo, a.Payment.PaymentDate, a.Payment.Method, a.Amount))
            .ToListAsync(cancellationToken);
        return new OrmeInvoiceDto(
            invoice.Id,
            invoice.InvoiceNo,
            invoice.CustomerId,
            invoice.Customer.Name,
            invoice.Customer.TaxNumber,
            invoice.Customer.TaxOffice,
            invoice.InvoiceDate,
            invoice.DueDate,
            invoice.Currency,
            invoice.VatRate,
            invoice.WithholdingPercent,
            invoice.Notes,
            invoice.Status,
            amounts.Subtotal,
            amounts.Vat,
            amounts.Withholding,
            amounts.Payable,
            payments.Sum(p => p.Amount),
            invoice.OpeningAmount is not null,
            invoice.Despatches.OrderBy(d => d.DespatchNo).Select(d => d.DespatchNo).ToList(),
            invoice.Despatches.OrderBy(d => d.DespatchNo).Select(d => d.Id).ToList(),
            lines,
            payments,
            invoice.Customer.DocumentLanguage);
    }

    public async Task<IReadOnlyList<OrmeInvoiceCandidateDto>> GetInvoiceCandidatesAsync(int? invoiceId, CancellationToken cancellationToken) =>
        await db.Set<OrmeDespatch>().AsNoTracking()
            .Where(d => d.Status == DespatchStatus.Confirmed && (d.InvoiceId == null || d.InvoiceId == invoiceId))
            .OrderBy(d => d.CustomerId).ThenBy(d => d.Id)
            .Take(MaxCandidates)
            .Select(d => new OrmeInvoiceCandidateDto(
                d.Id,
                d.DespatchNo,
                d.CustomerId,
                d.Customer.Name,
                d.DespatchDate,
                d.PackingLists.SelectMany(l => l.Items).Where(i => i.IsActive).Sum(i => (decimal?)i.Kilograms) ?? 0,
                d.InvoiceId != null))
            .ToListAsync(cancellationToken);

    public async Task<Result<OrmeInvoiceDto>> SaveInvoiceAsync(int? id, OrmeInvoiceRequest request, CancellationToken cancellationToken)
    {
        OrmeInvoice? invoice = null;
        if (id is not null)
        {
            invoice = await db.Set<OrmeInvoice>().Include(i => i.Lines).Include(i => i.Despatches).SingleOrDefaultAsync(i => i.Id == id, cancellationToken);
            if (invoice is null)
            {
                return InvoiceNotFound;
            }

            if (invoice.Status != InvoiceStatus.Draft)
            {
                return new Error(InvalidState, "Yalnızca taslak fatura düzenlenebilir.", ErrorType.Conflict);
            }
        }

        var ids = request.DespatchIds.Distinct().ToList();
        var despatches = await db.Set<OrmeDespatch>().Where(d => ids.Contains(d.Id)).ToListAsync(cancellationToken);
        if (despatches.Count != ids.Count)
        {
            return new Error(InvalidItem, "Seçilen irsaliyelerden biri bulunamadı.", ErrorType.Conflict);
        }

        foreach (var despatch in despatches)
        {
            var problem = despatch.Status != DespatchStatus.Confirmed ? "onaylı değil"
                : despatch.CustomerId != request.CustomerId ? "başka müşterinin"
                : despatch.InvoiceId is not null && despatch.InvoiceId != id ? "başka bir faturada"
                : null;
            if (problem is not null)
            {
                return new Error(InvalidItem, $"{despatch.DespatchNo} faturaya alınamaz: {problem}.", ErrorType.Conflict);
            }
        }

        var customer = await db.Set<OrmeCustomer>().AsNoTracking().SingleAsync(c => c.Id == request.CustomerId, cancellationToken);
        var items = await db.Set<OrmePackingItem>().AsNoTracking()
            .Where(p => p.IsActive && p.PackingList.DespatchId != null && ids.Contains(p.PackingList.DespatchId.Value))
            .Select(p => new
            {
                p.Kilograms,
                p.Grade,
                LineId = p.Roll.KnitOrder.OrderLineId,
                p.Roll.KnitOrder.OrderLine.UnitPrice,
                p.Roll.KnitOrder.OrderLine.Order.Currency,
                p.Roll.KnitOrder.OrderLine.Fabric.FabricCode,
                FabricName = p.Roll.KnitOrder.OrderLine.Fabric.Name,
            })
            .ToListAsync(cancellationToken);
        var currencies = items.Select(p => p.Currency).Distinct().ToList();
        if (currencies.Count > 1)
        {
            return new Error(InvalidItem, "Bir faturadaki siparişlerin para birimi aynı olmalıdır.", ErrorType.Conflict);
        }

        var prices = (request.Prices ?? []).GroupBy(p => (p.OrderLineId, p.Grade)).ToDictionary(g => g.Key, g => g.Last());
        var previous = invoice?.Lines.ToDictionary(l => (l.OrderLineId, l.Grade)) ?? [];

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (invoice is null)
        {
            // Numara yılı fatura tarihinden alınır.
            var year = request.InvoiceDate.Year;
            var sequence = await numbers.NextAsync(InvoiceSeries, year, cancellationToken);
            invoice = new OrmeInvoice { InvoiceNo = $"OFT-{year}-{sequence:D5}" };
            db.Set<OrmeInvoice>().Add(invoice);
        }
        else
        {
            db.Set<OrmeInvoiceLine>().RemoveRange(invoice.Lines);
            foreach (var removed in invoice.Despatches.Where(d => !ids.Contains(d.Id)).ToList())
            {
                invoice.Despatches.Remove(removed);
            }
        }

        invoice.CustomerId = request.CustomerId;
        invoice.InvoiceDate = request.InvoiceDate;
        invoice.DueDate = request.DueDate ?? (customer.PaymentTermDays is { } days ? request.InvoiceDate.AddDays(days) : null);
        invoice.Currency = currencies.FirstOrDefault() ?? customer.Currency;
        invoice.VatRate = request.VatRate ?? customer.VatRate;
        invoice.WithholdingPercent = request.WithholdingPercent;
        invoice.Notes = NullIfBlank(request.Notes);
        foreach (var despatch in despatches.Where(d => !invoice.Despatches.Contains(d)))
        {
            invoice.Despatches.Add(despatch);
        }

        foreach (var group in items.GroupBy(p => (p.LineId, p.Grade)).OrderBy(g => g.Key.LineId).ThenBy(g => g.Key.Grade))
        {
            var first = group.First();
            var key = group.Key;
            var secondQuality = key.Grade == FabricRollStatus.SecondQuality;
            invoice.Lines.Add(new OrmeInvoiceLine
            {
                OrderLineId = key.LineId,
                Grade = key.Grade,
                Description = $"{first.FabricCode} {first.FabricName}{(secondQuality ? " (2. kalite)" : "")}",
                Quantity = group.Sum(p => p.Kilograms),
                UnitPrice = prices.TryGetValue(key, out var price) ? price.UnitPrice : previous.TryGetValue(key, out var kept) ? kept.UnitPrice : first.UnitPrice,
                DiscountPercent = prices.TryGetValue(key, out var change) ? change.DiscountPercent
                    : previous.TryGetValue(key, out var old) ? old.DiscountPercent
                    : secondQuality ? customer.SecondQualityDiscountPercent : null,
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetInvoiceAsync(invoice.Id, cancellationToken);
    }

    public async Task<Result<OrmeInvoiceDto>> ConfirmInvoiceAsync(int id, CancellationToken cancellationToken)
    {
        var invoice = await db.Set<OrmeInvoice>().Include(i => i.Lines).SingleOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (invoice is null)
        {
            return InvoiceNotFound;
        }

        if (invoice.Status != InvoiceStatus.Draft)
        {
            return new Error(InvalidState, "Yalnızca taslak fatura onaylanabilir.", ErrorType.Conflict);
        }

        if (invoice.VatRate is null)
        {
            return new Error(InvalidState, "Onay için KDV oranı girilmelidir.", ErrorType.Conflict);
        }

        if (invoice.Lines.Count == 0 || invoice.Lines.Any(l => l.UnitPrice is null))
        {
            return new Error(InvalidState, "Onay için bütün satırların birim fiyatı girilmelidir.", ErrorType.Conflict);
        }

        invoice.Status = InvoiceStatus.Confirmed;
        await db.SaveChangesAsync(cancellationToken);
        return await GetInvoiceAsync(id, cancellationToken);
    }

    public async Task<Result<OrmeInvoiceDto>> ReopenInvoiceAsync(int id, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await appLock.TryAcquireAsync(OrmeAccountService.AccountLock, OrmeAccountService.LockTimeout, cancellationToken))
        {
            return new Error(ErrorCodes.ResourceBusy, "Cari işlemi sürüyor, biraz sonra yeniden deneyin.", ErrorType.Conflict);
        }

        var invoice = await db.Set<OrmeInvoice>().SingleOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (invoice is null)
        {
            return InvoiceNotFound;
        }

        if (invoice.Status != InvoiceStatus.Confirmed)
        {
            return new Error(InvalidState, "Yalnızca onaylı fatura taslağa alınabilir.", ErrorType.Conflict);
        }

        if (invoice.OpeningAmount is not null)
        {
            return new Error(InvalidState, "Açılış bakiyesi taslağa alınamaz; Açılış Bakiyeleri ekranından silin.", ErrorType.Conflict);
        }

        if (await db.Set<OrmePaymentAllocation>().AnyAsync(a => a.InvoiceId == id, cancellationToken))
        {
            return new Error(InvalidState, "Tahsilat dağıtılmış fatura taslağa alınamaz; önce dağıtımı kaldırın.", ErrorType.Conflict);
        }

        invoice.Status = InvoiceStatus.Draft;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetInvoiceAsync(id, cancellationToken);
    }

    public async Task<Result<OrmeInvoiceDto>> CancelInvoiceAsync(int id, CancellationToken cancellationToken)
    {
        var invoice = await db.Set<OrmeInvoice>().Include(i => i.Despatches).SingleOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (invoice is null)
        {
            return InvoiceNotFound;
        }

        if (invoice.Status != InvoiceStatus.Draft)
        {
            return new Error(InvalidState, "Yalnızca taslak fatura iptal edilebilir.", ErrorType.Conflict);
        }

        invoice.Status = InvoiceStatus.Cancelled;
        invoice.Despatches.Clear();
        await db.SaveChangesAsync(cancellationToken);
        return await GetInvoiceAsync(id, cancellationToken);
    }
}
