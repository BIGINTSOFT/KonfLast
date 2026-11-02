using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Application.Features.MasterData;
using ERP.Core.Domain.Enums;
using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Konfeksiyon.Application.Features.Pricing;
using ERP.Konfeksiyon.Domain.Entities;
using ERP.Konfeksiyon.Domain.Enums;
using ERP.Konfeksiyon.Domain.Rules;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Konfeksiyon.Application.Features.Invoicing;

public sealed class InvoiceQuery : PageRequest
{
    /// <summary>Fatura no ya da müşteri.</summary>
    public string? Search { get; set; }

    public InvoiceStatus? Status { get; set; }
}

public sealed record InvoiceListItemDto(
    int Id,
    string InvoiceNo,
    string CustomerName,
    DateOnly InvoiceDate,
    DateOnly? DueDate,
    string Currency,
    decimal Subtotal,
    InvoiceStatus Status);

public sealed record InvoiceLineDto(
    int Id,
    int StockItemId,
    string StockItemCode,
    int? SalesOrderLineId,
    string? SalesOrderNo,
    string Description,
    decimal Quantity,
    decimal? UnitPrice,
    decimal? DiscountPercent,
    decimal VatRate,
    decimal? Amount,
    decimal? VatAmount);

public sealed record InvoicePaymentDto(int PaymentId, string PaymentNo, DateOnly PaymentDate, PaymentMethod Method, decimal Amount);

public sealed record InvoiceShipmentDto(int Id, string ShipmentNo, string? DocumentNo, DateOnly ShipDate);

public sealed record InvoiceDto(
    int Id,
    string InvoiceNo,
    int CustomerId,
    string CustomerName,
    string? CustomerTaxNumber,
    string? CustomerTaxOffice,
    DateOnly InvoiceDate,
    DateOnly? DueDate,
    string Currency,
    decimal? WithholdingPercent,
    string? Notes,
    InvoiceStatus Status,
    decimal Subtotal,
    decimal VatAmount,
    decimal WithholdingAmount,
    decimal PayableAmount,
    decimal PaidAmount,
    bool IsOpening,
    IReadOnlyList<InvoiceShipmentDto> Shipments,
    IReadOnlyList<InvoiceLineDto> Lines,
    IReadOnlyList<InvoicePaymentDto> Payments,
    DocumentLanguage DocumentLanguage = DocumentLanguage.Turkish);

/// <param name="DocumentNo">Sevkiyattaki irsaliye numarası.</param>
public sealed record InvoiceCandidateDto(
    int Id,
    string ShipmentNo,
    string? DocumentNo,
    int CustomerId,
    string CustomerName,
    DateOnly ShipDate,
    string? SalesOrderNo,
    decimal Quantity,
    bool Selected);

/// <summary>Taslakta değiştirilen satır; satır, mamul kartı ve sipariş kalemiyle tanınır.</summary>
public sealed record InvoicePriceRequest(int StockItemId, int? SalesOrderLineId, decimal? UnitPrice, decimal? DiscountPercent, decimal VatRate);

/// <param name="DueDate">Boşsa müşterinin vadesinden hesaplanır.</param>
/// <param name="Prices">
/// Taslakta değiştirilen fiyat, iskonto ve KDV; gönderilmeyen satır önceki değerini, yeni satır sipariş kaleminin fiyatını
/// (yoksa fiyat listesini ve müşteri iskontosunu) ve ürünün KDV oranını alır.
/// </param>
public sealed record InvoiceRequest(
    int CustomerId,
    DateOnly InvoiceDate,
    IReadOnlyList<int> ShipmentIds,
    DateOnly? DueDate = null,
    decimal? WithholdingPercent = null,
    string? Notes = null,
    IReadOnlyList<InvoicePriceRequest>? Prices = null);

public sealed class InvoiceRequestValidator : AbstractValidator<InvoiceRequest>
{
    public InvoiceRequestValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0).WithMessage("Müşteri seçilmelidir.");
        RuleFor(x => x.InvoiceDate).NotEmpty().WithMessage("Fatura tarihi zorunludur.");
        RuleFor(x => x.ShipmentIds).NotEmpty().WithMessage("En az bir sevkiyat seçilmelidir.");
        RuleFor(x => x.DueDate).Must((request, due) => due >= request.InvoiceDate).WithMessage("Vade fatura tarihinden önce olamaz.").When(x => x.DueDate is not null);
        RuleFor(x => x.WithholdingPercent).InclusiveBetween(0, 100).WithMessage("Tevkifat 0 ile 100 arasında olmalıdır.").When(x => x.WithholdingPercent is not null);
        RuleFor(x => x.Notes).OptionalText(1000);
        RuleForEach(x => x.Prices).ChildRules(p =>
        {
            p.RuleFor(l => l.UnitPrice).InclusiveBetween(0, 99_999_999).WithMessage("Birim fiyat geçersiz.").When(l => l.UnitPrice is not null);
            p.RuleFor(l => l.DiscountPercent).InclusiveBetween(0, 100).WithMessage("İskonto 0 ile 100 arasında olmalıdır.").When(l => l.DiscountPercent is not null);
            p.RuleFor(l => l.VatRate).InclusiveBetween(0, 100).WithMessage("KDV oranı 0 ile 100 arasında olmalıdır.");
        });
    }
}

public interface IInvoiceService
{
    Task<PagedResult<InvoiceListItemDto>> GetListAsync(InvoiceQuery query, CancellationToken cancellationToken);
    Task<Result<InvoiceDto>> GetAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<InvoiceCandidateDto>> GetCandidatesAsync(int? invoiceId, CancellationToken cancellationToken);
    Task<Result<InvoiceDto>> SaveAsync(int? id, InvoiceRequest request, CancellationToken cancellationToken);
    Task<Result<InvoiceDto>> ConfirmAsync(int id, CancellationToken cancellationToken);
    Task<Result<InvoiceDto>> ReopenAsync(int id, CancellationToken cancellationToken);
    Task<Result<InvoiceDto>> CancelAsync(int id, CancellationToken cancellationToken);
}

/// <summary>
/// Fatura: aynı müşterinin sevk edilmiş sevkiyatları; satırlar mamul kartı ve sipariş kalemi bazında adettir. Fiyat
/// siparişten (iskonto sipariş fiyatına dahildir), siparişsiz sevkiyatta fiyat listesinden ve müşteri iskontosundan gelir.
/// Bir sevkiyat tek faturaya girer.
/// </summary>
public sealed class InvoiceService(IKonfeksiyonDbContext db, IDocumentNumberGenerator numbers, IAppLock appLock, IPricingService pricing) : IInvoiceService
{
    private const string Series = "SalesInvoice";
    private const string InvalidState = "INVOICE_INVALID_STATE";
    private const string InvalidItem = "INVOICE_INVALID_ITEM";
    private const int MaxCandidates = 500;

    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Fatura bulunamadı.", ErrorType.NotFound);

    public Task<PagedResult<InvoiceListItemDto>> GetListAsync(InvoiceQuery query, CancellationToken cancellationToken)
    {
        var invoices = db.Invoices.AsNoTracking().Where(i => i.OpeningAmount == null);
        if (NullIfBlank(query.Search) is { } term)
        {
            invoices = invoices.Where(i => i.InvoiceNo.Contains(term) || i.Customer.Name.Contains(term));
        }

        if (query.Status is { } status)
        {
            invoices = invoices.Where(i => i.Status == status);
        }

        return invoices
            .OrderByDescending(i => i.Id)
            .Select(i => new InvoiceListItemDto(
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

    public async Task<Result<InvoiceDto>> GetAsync(int id, CancellationToken cancellationToken)
    {
        var invoice = await db.Invoices.AsNoTracking()
            .Include(i => i.Customer)
            .Include(i => i.Shipments)
            .Include(i => i.Lines).ThenInclude(l => l.StockItem)
            .Include(i => i.Lines).ThenInclude(l => l.SalesOrderLine).ThenInclude(o => o!.SalesOrder)
            .SingleOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (invoice is null)
        {
            return NotFound;
        }

        var lines = invoice.Lines.OrderBy(l => l.Id).Select(l =>
        {
            decimal? amount = l.UnitPrice is { } price ? InvoiceAmounts.Line(l.Quantity, price, l.DiscountPercent) : null;
            return new InvoiceLineDto(
                l.Id, l.StockItemId, l.StockItem.Code, l.SalesOrderLineId, l.SalesOrderLine?.SalesOrder.OrderNo, l.Description,
                l.Quantity, l.UnitPrice, l.DiscountPercent, l.VatRate, amount,
                amount is { } value ? InvoiceAmounts.LineVat(value, l.VatRate) : null);
        }).ToList();
        var amounts = invoice.OpeningAmount is { } opening
            ? InvoiceAmounts.Opening(opening)
            : InvoiceAmounts.Of(lines.Select(l => (l.Amount ?? 0, l.VatRate)), invoice.WithholdingPercent);
        var payments = await db.PaymentAllocations.AsNoTracking()
            .Where(a => a.InvoiceId == id)
            .OrderBy(a => a.Id)
            .Select(a => new InvoicePaymentDto(a.PaymentId, a.Payment.PaymentNo, a.Payment.PaymentDate, a.Payment.Method, a.Amount))
            .ToListAsync(cancellationToken);
        return new InvoiceDto(
            invoice.Id,
            invoice.InvoiceNo,
            invoice.CustomerId,
            invoice.Customer.Name,
            invoice.Customer.TaxNumber,
            invoice.Customer.TaxOffice,
            invoice.InvoiceDate,
            invoice.DueDate,
            invoice.Currency,
            invoice.WithholdingPercent,
            invoice.Notes,
            invoice.Status,
            amounts.Subtotal,
            amounts.Vat,
            amounts.Withholding,
            amounts.Payable,
            payments.Sum(p => p.Amount),
            invoice.OpeningAmount is not null,
            invoice.Shipments.OrderBy(s => s.ShipmentNo).Select(s => new InvoiceShipmentDto(s.Id, s.ShipmentNo, s.DocumentNo, s.ShipDate)).ToList(),
            lines,
            payments,
            invoice.Customer.DocumentLanguage);
    }

    public async Task<IReadOnlyList<InvoiceCandidateDto>> GetCandidatesAsync(int? invoiceId, CancellationToken cancellationToken) =>
        await db.Shipments.AsNoTracking()
            .Where(s => s.Status == ShipmentStatus.Shipped && (s.InvoiceId == null || s.InvoiceId == invoiceId))
            .OrderBy(s => s.CustomerId).ThenBy(s => s.Id)
            .Take(MaxCandidates)
            .Select(s => new InvoiceCandidateDto(
                s.Id,
                s.ShipmentNo,
                s.DocumentNo,
                s.CustomerId,
                s.Customer.Name,
                s.ShipDate,
                s.SalesOrder != null ? s.SalesOrder.OrderNo : null,
                s.Items.Sum(i => (decimal?)i.Quantity) ?? 0,
                s.InvoiceId != null))
            .ToListAsync(cancellationToken);

    public async Task<Result<InvoiceDto>> SaveAsync(int? id, InvoiceRequest request, CancellationToken cancellationToken)
    {
        Invoice? invoice = null;
        if (id is not null)
        {
            invoice = await db.Invoices.Include(i => i.Lines).Include(i => i.Shipments).SingleOrDefaultAsync(i => i.Id == id, cancellationToken);
            if (invoice is null)
            {
                return NotFound;
            }

            if (invoice.Status != InvoiceStatus.Draft)
            {
                return new Error(InvalidState, "Yalnızca taslak fatura düzenlenebilir.", ErrorType.Conflict);
            }
        }

        var ids = request.ShipmentIds.Distinct().ToList();
        var shipments = await db.Shipments.Include(s => s.SalesOrder).Where(s => ids.Contains(s.Id)).ToListAsync(cancellationToken);
        if (shipments.Count != ids.Count)
        {
            return new Error(InvalidItem, "Seçilen sevkiyatlardan biri bulunamadı.", ErrorType.Conflict);
        }

        foreach (var shipment in shipments)
        {
            var problem = shipment.Status != ShipmentStatus.Shipped ? "sevk edilmemiş"
                : shipment.CustomerId != request.CustomerId ? "başka müşterinin"
                : shipment.InvoiceId is not null && shipment.InvoiceId != id ? "başka bir faturada"
                : null;
            if (problem is not null)
            {
                return new Error(InvalidItem, $"{shipment.ShipmentNo} faturaya alınamaz: {problem}.", ErrorType.Conflict);
            }
        }

        var customer = await db.Customers.AsNoTracking().SingleOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken);
        if (customer is null)
        {
            return new Error(ErrorCodes.NotFound, "Müşteri bulunamadı.", ErrorType.NotFound);
        }

        // Siparişsiz sevkiyat müşterinin para birimiyle faturalanır.
        var currencies = shipments.Select(s => s.SalesOrder?.Currency ?? customer.Currency).Distinct().ToList();
        if (currencies.Count > 1)
        {
            return new Error(InvalidItem, "Bir faturadaki sevkiyatların para birimi aynı olmalıdır.", ErrorType.Conflict);
        }

        var currency = currencies[0];
        var items = await db.ShipmentItems.AsNoTracking()
            .Where(i => ids.Contains(i.ShipmentId))
            .Select(i => new
            {
                i.StockItemId,
                i.Quantity,
                i.Shipment.SalesOrderId,
                StockItemName = i.StockItem.Name,
                i.StockItem.ProductId,
                i.StockItem.ColorId,
                i.StockItem.SizeId,
                ProductText = i.StockItem.Product != null ? i.StockItem.Product.ProductCode + " " + i.StockItem.Product.Name : null,
                ColorName = i.StockItem.Color != null ? i.StockItem.Color.Name : null,
                SizeName = i.StockItem.Size != null ? i.StockItem.Size.Name : null,
                VatRate = i.StockItem.Product != null ? i.StockItem.Product.VatRate : (decimal?)null,
            })
            .ToListAsync(cancellationToken);
        var orderIds = items.Where(i => i.SalesOrderId != null).Select(i => i.SalesOrderId!.Value).Distinct().ToList();
        var orderLines = await db.SalesOrderLines.AsNoTracking()
            .Where(l => orderIds.Contains(l.SalesOrderId))
            .OrderBy(l => l.Id)
            .Select(l => new { l.Id, l.SalesOrderId, l.ProductId, l.ColorId, l.SizeId, l.UnitPrice })
            .ToListAsync(cancellationToken);

        // Sevk edilen mamul, siparişte aynı ürün, renk ve bedenin kalemine bağlanır.
        var rows = items.Select(i => new
        {
            Item = i,
            OrderLine = i.SalesOrderId is { } orderId
                ? orderLines.FirstOrDefault(l => l.SalesOrderId == orderId && l.ProductId == i.ProductId && l.ColorId == i.ColorId && l.SizeId == i.SizeId)
                : null,
        }).ToList();
        var groups = rows.GroupBy(r => (r.Item.StockItemId, SalesOrderLineId: r.OrderLine?.Id)).OrderBy(g => g.Key.StockItemId).ThenBy(g => g.Key.SalesOrderLineId).ToList();

        // Sipariş fiyatı olmayan satırlar fiyat listesinden ve müşteri iskontosundan önerilir.
        var quoteItems = groups
            .Where(g => g.First().OrderLine?.UnitPrice is null && g.First().Item is { ProductId: not null, ColorId: not null, SizeId: not null })
            .Select(g => g.First().Item)
            .Select(i => new PriceQuoteItemRequest(i.ProductId!.Value, i.ColorId!.Value, i.SizeId!.Value))
            .Distinct()
            .ToList();
        var quotes = quoteItems.Count == 0
            ? []
            : await pricing.QuoteAsync(new PriceQuoteRequest(request.CustomerId, currency, request.InvoiceDate, quoteItems), cancellationToken);

        var changes = (request.Prices ?? []).GroupBy(p => (p.StockItemId, p.SalesOrderLineId)).ToDictionary(g => g.Key, g => g.Last());
        var previous = invoice?.Lines.ToDictionary(l => (l.StockItemId, l.SalesOrderLineId)) ?? [];

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (invoice is null)
        {
            var year = request.InvoiceDate.Year;
            var sequence = await numbers.NextAsync(Series, year, cancellationToken);
            invoice = new Invoice { InvoiceNo = $"FT-{year}-{sequence:D5}" };
            db.Invoices.Add(invoice);
        }
        else
        {
            db.InvoiceLines.RemoveRange(invoice.Lines);
            foreach (var removed in invoice.Shipments.Where(s => !ids.Contains(s.Id)).ToList())
            {
                invoice.Shipments.Remove(removed);
            }
        }

        invoice.CustomerId = request.CustomerId;
        invoice.InvoiceDate = request.InvoiceDate;
        invoice.DueDate = request.DueDate ?? (customer.PaymentTermDays is { } days ? request.InvoiceDate.AddDays(days) : null);
        invoice.Currency = currency;
        invoice.WithholdingPercent = request.WithholdingPercent;
        invoice.Notes = NullIfBlank(request.Notes);
        foreach (var shipment in shipments.Where(s => !invoice.Shipments.Contains(s)))
        {
            invoice.Shipments.Add(shipment);
        }

        foreach (var group in groups)
        {
            var first = group.First();
            var key = group.Key;
            var item = first.Item;
            var description = item.ProductText is null
                ? item.StockItemName
                : string.Join(" / ", new[] { item.ProductText, item.ColorName, item.SizeName }.Where(x => x is not null));
            var line = new InvoiceLine
            {
                StockItemId = key.StockItemId,
                SalesOrderLineId = key.SalesOrderLineId,
                Description = description.Length > 300 ? description[..300] : description,
                Quantity = group.Sum(r => r.Item.Quantity),
            };
            if (changes.TryGetValue(key, out var change))
            {
                (line.UnitPrice, line.DiscountPercent, line.VatRate) = (change.UnitPrice, change.DiscountPercent, change.VatRate);
            }
            else if (previous.TryGetValue(key, out var kept))
            {
                (line.UnitPrice, line.DiscountPercent, line.VatRate) = (kept.UnitPrice, kept.DiscountPercent, kept.VatRate);
            }
            else
            {
                line.VatRate = item.VatRate ?? 0;
                if (first.OrderLine?.UnitPrice is { } orderPrice)
                {
                    line.UnitPrice = orderPrice;
                }
                else if (quotes.FirstOrDefault(q => q.ProductId == item.ProductId && q.ColorId == item.ColorId && q.SizeId == item.SizeId) is { ListPrice: not null } quote)
                {
                    line.UnitPrice = quote.ListPrice;
                    line.DiscountPercent = quote.DiscountPercent;
                }
            }

            invoice.Lines.Add(line);
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(invoice.Id, cancellationToken);
    }

    public async Task<Result<InvoiceDto>> ConfirmAsync(int id, CancellationToken cancellationToken)
    {
        var invoice = await db.Invoices.Include(i => i.Lines).SingleOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (invoice is null)
        {
            return NotFound;
        }

        if (invoice.Status != InvoiceStatus.Draft)
        {
            return new Error(InvalidState, "Yalnızca taslak fatura onaylanabilir.", ErrorType.Conflict);
        }

        if (invoice.Lines.Count == 0 || invoice.Lines.Any(l => l.UnitPrice is null))
        {
            return new Error(InvalidState, "Onay için bütün satırların birim fiyatı girilmelidir.", ErrorType.Conflict);
        }

        invoice.Status = InvoiceStatus.Confirmed;
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<Result<InvoiceDto>> ReopenAsync(int id, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await appLock.TryAcquireAsync(CustomerAccountService.AccountLock, CustomerAccountService.LockTimeout, cancellationToken))
        {
            return new Error(ErrorCodes.ResourceBusy, "Cari işlemi sürüyor, biraz sonra yeniden deneyin.", ErrorType.Conflict);
        }

        var invoice = await db.Invoices.SingleOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (invoice is null)
        {
            return NotFound;
        }

        if (invoice.Status != InvoiceStatus.Confirmed)
        {
            return new Error(InvalidState, "Yalnızca onaylı fatura taslağa alınabilir.", ErrorType.Conflict);
        }

        if (invoice.OpeningAmount is not null)
        {
            return new Error(InvalidState, "Açılış bakiyesi taslağa alınamaz; Açılış Bakiyeleri ekranından silin.", ErrorType.Conflict);
        }

        if (await db.PaymentAllocations.AnyAsync(a => a.InvoiceId == id, cancellationToken))
        {
            return new Error(InvalidState, "Tahsilat dağıtılmış fatura taslağa alınamaz; önce dağıtımı kaldırın.", ErrorType.Conflict);
        }

        invoice.Status = InvoiceStatus.Draft;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<Result<InvoiceDto>> CancelAsync(int id, CancellationToken cancellationToken)
    {
        var invoice = await db.Invoices.Include(i => i.Shipments).SingleOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (invoice is null)
        {
            return NotFound;
        }

        if (invoice.Status != InvoiceStatus.Draft)
        {
            return new Error(InvalidState, "Yalnızca taslak fatura iptal edilebilir.", ErrorType.Conflict);
        }

        invoice.Status = InvoiceStatus.Cancelled;
        invoice.Shipments.Clear();
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }
}
