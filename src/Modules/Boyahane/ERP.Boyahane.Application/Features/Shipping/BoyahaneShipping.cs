using ERP.Boyahane.Application.Features.Accounts;
using ERP.Boyahane.Domain.Entities;
using ERP.Boyahane.Domain.Enums;
using ERP.Boyahane.Domain.Rules;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Boyahane.Application.Features.Shipping;

public sealed class BoyahaneShippingQuery : PageRequest
{
    /// <summary>Belge no, müşteri ya da müşteri sipariş no.</summary>
    public string? Search { get; set; }

    /// <summary>Belge tarihi aralığı (dahil); boşsa sınır yok.</summary>
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
}

public sealed record BoyahaneDespatchListItemDto(
    int Id,
    string DespatchNo,
    string CustomerName,
    DateOnly DespatchDate,
    string? VehiclePlate,
    int PackingListCount,
    decimal TotalKilograms,
    DespatchStatus Status,
    string? InvoiceNo);

public sealed record BoyahaneDespatchListDto(int Id, string PackingNo, string OrderNo, int PackageCount, decimal Kilograms, decimal Meters);

/// <summary>İrsaliye satırı: sipariş satırı bazında toplam.</summary>
/// <param name="CustomerFabricCode">Satırın toplarındaki müşteri kumaş kodları (birden çoksa virgülle).</param>
public sealed record BoyahaneDespatchLineDto(
    string OrderNo,
    string CardCode,
    string CardName,
    string? ColorName,
    int PackageCount,
    int BoxCount,
    decimal Kilograms,
    decimal Meters,
    string? CustomerOrderNo,
    string? CustomerFabricCode,
    string? ColorCode);

/// <summary>İrsaliyedeki paket; yükleme kontrolünde okutulan barkodla eşlenir.</summary>
public sealed record BoyahaneDespatchPackageDto(int Id, string PackageNo, string PackingNo, string ItemNo, string CardCode, decimal Kilograms, decimal? Meters);

/// <param name="DocumentLanguage">Baskının dili: müşterinin belge dili.</param>
public sealed record BoyahaneDespatchDto(
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
    decimal TotalMeters,
    int TotalPackages,
    IReadOnlyList<BoyahaneDespatchListDto> PackingLists,
    IReadOnlyList<BoyahaneDespatchLineDto> Lines,
    IReadOnlyList<BoyahaneDespatchPackageDto> Packages,
    DocumentLanguage DocumentLanguage);

public sealed record BoyahaneDespatchCandidateDto(int Id, string PackingNo, int CustomerId, string CustomerName, string OrderNo, int PackageCount, decimal Kilograms, decimal Meters, bool Selected);

/// <param name="DeliveryAddress">Boşsa müşterinin varsayılan teslimat adresi yazılır.</param>
public sealed record BoyahaneDespatchRequest(
    int CustomerId,
    DateOnly DespatchDate,
    IReadOnlyList<int> PackingListIds,
    string? VehiclePlate = null,
    string? DriverName = null,
    string? DeliveryAddress = null,
    string? Notes = null);

public sealed class BoyahaneDespatchRequestValidator : AbstractValidator<BoyahaneDespatchRequest>
{
    public BoyahaneDespatchRequestValidator()
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

public sealed record BoyahaneInvoiceListItemDto(
    int Id,
    string InvoiceNo,
    string CustomerName,
    DateOnly InvoiceDate,
    DateOnly? DueDate,
    string Currency,
    decimal Subtotal,
    InvoiceStatus Status);

public sealed record BoyahaneInvoiceLineDto(int Id, int OrderLineId, string OrderNo, string Description, decimal Quantity, StockUnit Unit, decimal? UnitPrice, decimal? Amount, string? CustomerOrderNo);

/// <param name="DocumentLanguage">Baskının dili: müşterinin belge dili.</param>
public sealed record BoyahaneInvoiceDto(
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
    IReadOnlyList<BoyahaneInvoiceLineDto> Lines,
    IReadOnlyList<BoyahaneInvoicePaymentDto> Payments,
    DocumentLanguage DocumentLanguage);

public sealed record BoyahaneInvoicePaymentDto(int PaymentId, string PaymentNo, DateOnly PaymentDate, PaymentMethod Method, decimal Amount);

public sealed record BoyahaneInvoiceCandidateDto(int Id, string DespatchNo, int CustomerId, string CustomerName, DateOnly DespatchDate, decimal Kilograms, decimal Meters, bool Selected);

public sealed record BoyahaneInvoicePriceRequest(int OrderLineId, decimal? UnitPrice);

/// <param name="DueDate">Boşsa müşterinin vadesinden hesaplanır.</param>
/// <param name="VatRate">Boşsa müşterinin varsayılan oranı yazılır; o da yoksa onaydan önce girilmelidir.</param>
/// <param name="Prices">Taslakta değiştirilen birim fiyatlar; gönderilmeyen satır siparişteki fiyatı alır.</param>
public sealed record BoyahaneInvoiceRequest(
    int CustomerId,
    DateOnly InvoiceDate,
    IReadOnlyList<int> DespatchIds,
    DateOnly? DueDate = null,
    decimal? VatRate = null,
    decimal? WithholdingPercent = null,
    string? Notes = null,
    IReadOnlyList<BoyahaneInvoicePriceRequest>? Prices = null);

public sealed class BoyahaneInvoiceRequestValidator : AbstractValidator<BoyahaneInvoiceRequest>
{
    public BoyahaneInvoiceRequestValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0).WithMessage("Müşteri seçilmelidir.");
        RuleFor(x => x.InvoiceDate).NotEmpty().WithMessage("Fatura tarihi zorunludur.");
        RuleFor(x => x.DespatchIds).NotEmpty().WithMessage("En az bir irsaliye seçilmelidir.");
        RuleFor(x => x.DueDate).Must((request, due) => due >= request.InvoiceDate).WithMessage("Vade fatura tarihinden önce olamaz.").When(x => x.DueDate is not null);
        RuleFor(x => x.VatRate).InclusiveBetween(0, 100).WithMessage("KDV oranı 0 ile 100 arasında olmalıdır.").When(x => x.VatRate is not null);
        RuleFor(x => x.WithholdingPercent).InclusiveBetween(0, 100).WithMessage("Tevkifat 0 ile 100 arasında olmalıdır.").When(x => x.WithholdingPercent is not null);
        RuleFor(x => x.Notes).OptionalText(1000);
        RuleForEach(x => x.Prices).ChildRules(p => p.RuleFor(l => l.UnitPrice).InclusiveBetween(0, 99_999_999).WithMessage("Birim fiyat geçersiz.").When(l => l.UnitPrice is not null));
    }
}

public interface IBoyahaneShippingService
{
    Task<PagedResult<BoyahaneDespatchListItemDto>> GetDespatchesAsync(BoyahaneShippingQuery query, CancellationToken cancellationToken);
    Task<Result<BoyahaneDespatchDto>> GetDespatchAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<BoyahaneDespatchCandidateDto>> GetDespatchCandidatesAsync(int? despatchId, CancellationToken cancellationToken);
    Task<Result<BoyahaneDespatchDto>> SaveDespatchAsync(int? id, BoyahaneDespatchRequest request, CancellationToken cancellationToken);
    Task<Result<BoyahaneDespatchDto>> ConfirmDespatchAsync(int id, CancellationToken cancellationToken);
    Task<Result<BoyahaneDespatchDto>> ReopenDespatchAsync(int id, CancellationToken cancellationToken);
    Task<Result<BoyahaneDespatchDto>> CancelDespatchAsync(int id, CancellationToken cancellationToken);
    Task<PagedResult<BoyahaneInvoiceListItemDto>> GetInvoicesAsync(BoyahaneShippingQuery query, CancellationToken cancellationToken);
    Task<Result<BoyahaneInvoiceDto>> GetInvoiceAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<BoyahaneInvoiceCandidateDto>> GetInvoiceCandidatesAsync(int? invoiceId, CancellationToken cancellationToken);
    Task<Result<BoyahaneInvoiceDto>> SaveInvoiceAsync(int? id, BoyahaneInvoiceRequest request, CancellationToken cancellationToken);
    Task<Result<BoyahaneInvoiceDto>> ConfirmInvoiceAsync(int id, CancellationToken cancellationToken);
    Task<Result<BoyahaneInvoiceDto>> ReopenInvoiceAsync(int id, CancellationToken cancellationToken);
    Task<Result<BoyahaneInvoiceDto>> CancelInvoiceAsync(int id, CancellationToken cancellationToken);
}

public sealed class BoyahaneShippingService(IAppDbContext db, IDocumentNumberGenerator numbers, IAppLock appLock, TimeProvider timeProvider) : IBoyahaneShippingService
{
    private const string DespatchSeries = "BoyahaneDespatch";
    private const string InvoiceSeries = "BoyahaneInvoice";
    private const string InvalidState = "SHIPPING_INVALID_STATE";
    private const string InvalidItem = "SHIPPING_INVALID_ITEM";
    private const int MaxCandidates = 500;

    private static readonly Error DespatchNotFound = new(ErrorCodes.NotFound, "İrsaliye bulunamadı.", ErrorType.NotFound);
    private static readonly Error InvoiceNotFound = new(ErrorCodes.NotFound, "Fatura bulunamadı.", ErrorType.NotFound);

    private int Year => timeProvider.GetUtcNow().Year;

    public Task<PagedResult<BoyahaneDespatchListItemDto>> GetDespatchesAsync(BoyahaneShippingQuery query, CancellationToken cancellationToken)
    {
        var despatches = db.Set<BoyahaneDespatch>().AsNoTracking();
        if (NullIfBlank(query.Search) is { } term)
        {
            despatches = despatches.Where(d => d.DespatchNo.Contains(term) || d.Customer.Name.Contains(term)
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
            .Select(d => new BoyahaneDespatchListItemDto(
                d.Id,
                d.DespatchNo,
                d.Customer.Name,
                d.DespatchDate,
                d.VehiclePlate,
                d.PackingLists.Count,
                d.PackingLists.SelectMany(l => l.Items).Sum(i => i.Kilograms),
                d.Status,
                d.Invoice != null ? d.Invoice.InvoiceNo : null))
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<Result<BoyahaneDespatchDto>> GetDespatchAsync(int id, CancellationToken cancellationToken)
    {
        var d = await db.Set<BoyahaneDespatch>().AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Invoice)
            .Include(x => x.PackingLists).ThenInclude(l => l.Order)
            .Include(x => x.PackingLists).ThenInclude(l => l.Items).ThenInclude(i => i.BatchItem).ThenInclude(b => b.OrderLine).ThenInclude(o => o.Fabric)
            .Include(x => x.PackingLists).ThenInclude(l => l.Items).ThenInclude(i => i.BatchItem).ThenInclude(b => b.OrderLine).ThenInclude(o => o.Yarn)
            .Include(x => x.PackingLists).ThenInclude(l => l.Items).ThenInclude(i => i.BatchItem).ThenInclude(b => b.Roll!).ThenInclude(r => r.Lot)
            .Include(x => x.PackingLists).ThenInclude(l => l.Items).ThenInclude(i => i.BatchItem).ThenInclude(b => b.YarnLot)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (d is null)
        {
            return DespatchNotFound;
        }

        var packages = d.PackingLists.SelectMany(l => l.Items.Select(i => new { l.Order.OrderNo, l.Order.CustomerOrderNo, l.PackingNo, Item = i, Line = i.BatchItem.OrderLine })).ToList();
        return new BoyahaneDespatchDto(
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
            packages.Sum(p => p.Item.Kilograms),
            packages.Sum(p => p.Item.Meters ?? 0),
            packages.Count,
            d.PackingLists.OrderBy(l => l.PackingNo).Select(l => new BoyahaneDespatchListDto(l.Id, l.PackingNo, l.Order.OrderNo, l.Items.Count, l.Items.Sum(i => i.Kilograms), l.Items.Sum(i => i.Meters ?? 0))).ToList(),
            packages
                .GroupBy(p => new { p.OrderNo, p.CustomerOrderNo, p.Line.Id, Code = p.Line.Fabric?.FabricCode ?? p.Line.Yarn?.YarnCode ?? "", Name = p.Line.Fabric?.Name ?? p.Line.Yarn?.Name ?? "", p.Line.ColorName, p.Line.ColorCode })
                .OrderBy(g => g.Key.OrderNo).ThenBy(g => g.Key.Id)
                .Select(g => new BoyahaneDespatchLineDto(
                    g.Key.OrderNo, g.Key.Code, g.Key.Name, g.Key.ColorName, g.Count(), g.Sum(p => p.Item.BoxCount), g.Sum(p => p.Item.Kilograms), g.Sum(p => p.Item.Meters ?? 0),
                    g.Key.CustomerOrderNo,
                    JoinDistinct(g.Select(p => p.Item.BatchItem.Roll?.Lot.CustomerFabricCode)),
                    g.Key.ColorCode))
                .ToList(),
            packages
                .OrderBy(p => p.Item.PackageNo)
                .Select(p => new BoyahaneDespatchPackageDto(
                    p.Item.Id, p.Item.PackageNo, p.PackingNo,
                    p.Item.BatchItem.Roll?.RollNo ?? p.Item.BatchItem.YarnLot?.LotNo ?? "",
                    p.Line.Fabric?.FabricCode ?? p.Line.Yarn?.YarnCode ?? "",
                    p.Item.Kilograms, p.Item.Meters))
                .ToList(),
            d.Customer.DocumentLanguage);
    }

    /// <summary>Boş olmayan farklı değerler virgülle; hiçbiri yoksa boş.</summary>
    private static string? JoinDistinct(IEnumerable<string?> values)
    {
        var distinct = values.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v!).Distinct().ToList();
        return distinct.Count == 0 ? null : string.Join(", ", distinct);
    }

    public async Task<IReadOnlyList<BoyahaneDespatchCandidateDto>> GetDespatchCandidatesAsync(int? despatchId, CancellationToken cancellationToken) =>
        await db.Set<BoyahanePackingList>().AsNoTracking()
            .Where(l => l.Status == PackingListStatus.Confirmed && (l.Order.CustomerId != null || l.SaleCustomerId != null) && (l.DespatchId == null || l.DespatchId == despatchId))
            .OrderBy(l => l.Order.CustomerId ?? l.SaleCustomerId).ThenBy(l => l.Id)
            .Take(MaxCandidates)
            .Select(l => new BoyahaneDespatchCandidateDto(
                l.Id,
                l.PackingNo,
                (l.Order.CustomerId ?? l.SaleCustomerId)!.Value,
                l.Order.Customer != null ? l.Order.Customer.Name : l.SaleCustomer!.Name,
                l.Order.OrderNo,
                l.Items.Count,
                l.Items.Sum(i => i.Kilograms),
                l.Items.Sum(i => i.Meters ?? 0),
                l.DespatchId != null))
            .ToListAsync(cancellationToken);

    public async Task<Result<BoyahaneDespatchDto>> SaveDespatchAsync(int? id, BoyahaneDespatchRequest request, CancellationToken cancellationToken)
    {
        BoyahaneDespatch? despatch = null;
        if (id is not null)
        {
            despatch = await db.Set<BoyahaneDespatch>().Include(d => d.PackingLists).SingleOrDefaultAsync(d => d.Id == id, cancellationToken);
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
        var lists = await db.Set<BoyahanePackingList>().Include(l => l.Order).Where(l => ids.Contains(l.Id)).ToListAsync(cancellationToken);
        if (lists.Count != ids.Count)
        {
            return new Error(InvalidItem, "Seçilen çeki listelerinden biri bulunamadı.", ErrorType.Conflict);
        }

        foreach (var list in lists)
        {
            var problem = list.Status != PackingListStatus.Confirmed ? "onaylı değil"
                : (list.Order.CustomerId ?? list.SaleCustomerId) is null ? "stoğa üretim; önce çeki listesinde müşteri seçin"
                : (list.Order.CustomerId ?? list.SaleCustomerId) != request.CustomerId ? "başka müşterinin"
                : list.DespatchId is not null && list.DespatchId != id ? "başka bir irsaliyede"
                : null;
            if (problem is not null)
            {
                return new Error(InvalidItem, $"{list.PackingNo} irsaliyeye alınamaz: {problem}.", ErrorType.Conflict);
            }
        }

        var address = NullIfBlank(request.DeliveryAddress) ?? await db.Set<BoyahaneCustomerAddress>().AsNoTracking()
            .Where(a => a.CustomerId == request.CustomerId && a.IsDefaultDelivery)
            .Select(a => a.AddressLine + " " + a.District + "/" + a.City)
            .FirstOrDefaultAsync(cancellationToken);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (despatch is null)
        {
            var sequence = await numbers.NextAsync(DespatchSeries, Year, cancellationToken);
            despatch = new BoyahaneDespatch { DespatchNo = $"IR-{Year}-{sequence:D5}" };
            db.Set<BoyahaneDespatch>().Add(despatch);
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

    public async Task<Result<BoyahaneDespatchDto>> ConfirmDespatchAsync(int id, CancellationToken cancellationToken)
    {
        var despatch = await db.Set<BoyahaneDespatch>().SingleOrDefaultAsync(d => d.Id == id, cancellationToken);
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

    public async Task<Result<BoyahaneDespatchDto>> ReopenDespatchAsync(int id, CancellationToken cancellationToken)
    {
        var despatch = await db.Set<BoyahaneDespatch>().SingleOrDefaultAsync(d => d.Id == id, cancellationToken);
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

    public async Task<Result<BoyahaneDespatchDto>> CancelDespatchAsync(int id, CancellationToken cancellationToken)
    {
        var despatch = await db.Set<BoyahaneDespatch>().Include(d => d.PackingLists).SingleOrDefaultAsync(d => d.Id == id, cancellationToken);
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

    public Task<PagedResult<BoyahaneInvoiceListItemDto>> GetInvoicesAsync(BoyahaneShippingQuery query, CancellationToken cancellationToken)
    {
        var invoices = db.Set<BoyahaneInvoice>().AsNoTracking().Where(i => i.OpeningAmount == null);
        if (NullIfBlank(query.Search) is { } term)
        {
            invoices = invoices.Where(i => i.InvoiceNo.Contains(term) || i.Customer.Name.Contains(term)
                || i.Lines.Any(l => l.OrderLine.Order.CustomerOrderNo != null && l.OrderLine.Order.CustomerOrderNo.Contains(term)));
        }

        if (query.From is { } from)
        {
            invoices = invoices.Where(i => i.InvoiceDate >= from);
        }

        if (query.To is { } to)
        {
            invoices = invoices.Where(i => i.InvoiceDate <= to);
        }

        return invoices
            .OrderByDescending(i => i.Id)
            .Select(i => new BoyahaneInvoiceListItemDto(
                i.Id,
                i.InvoiceNo,
                i.Customer.Name,
                i.InvoiceDate,
                i.DueDate,
                i.Currency,
                i.Lines.Sum(l => Math.Round(l.Quantity * (l.UnitPrice ?? 0), 2)),
                i.Status))
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<Result<BoyahaneInvoiceDto>> GetInvoiceAsync(int id, CancellationToken cancellationToken)
    {
        var invoice = await db.Set<BoyahaneInvoice>().AsNoTracking()
            .Include(i => i.Customer)
            .Include(i => i.Despatches)
            .Include(i => i.Lines).ThenInclude(l => l.OrderLine).ThenInclude(o => o.Order)
            .SingleOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (invoice is null)
        {
            return InvoiceNotFound;
        }

        var lines = invoice.Lines.OrderBy(l => l.Id).Select(l => new BoyahaneInvoiceLineDto(
            l.Id, l.OrderLineId, l.OrderLine.Order.OrderNo, l.Description, l.Quantity, l.Unit, l.UnitPrice,
            l.UnitPrice is { } price ? InvoiceAmounts.Line(l.Quantity, price) : null, l.OrderLine.Order.CustomerOrderNo)).ToList();
        var amounts = InvoiceAmounts.Of(invoice.OpeningAmount ?? lines.Sum(l => l.Amount ?? 0), invoice.VatRate, invoice.WithholdingPercent);
        var payments = await db.Set<BoyahanePaymentAllocation>().AsNoTracking()
            .Where(a => a.InvoiceId == id)
            .OrderBy(a => a.Id)
            .Select(a => new BoyahaneInvoicePaymentDto(a.PaymentId, a.Payment.PaymentNo, a.Payment.PaymentDate, a.Payment.Method, a.Amount))
            .ToListAsync(cancellationToken);
        return new BoyahaneInvoiceDto(
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

    public async Task<IReadOnlyList<BoyahaneInvoiceCandidateDto>> GetInvoiceCandidatesAsync(int? invoiceId, CancellationToken cancellationToken) =>
        await db.Set<BoyahaneDespatch>().AsNoTracking()
            .Where(d => d.Status == DespatchStatus.Confirmed && (d.InvoiceId == null || d.InvoiceId == invoiceId))
            .OrderBy(d => d.CustomerId).ThenBy(d => d.Id)
            .Take(MaxCandidates)
            .Select(d => new BoyahaneInvoiceCandidateDto(
                d.Id,
                d.DespatchNo,
                d.CustomerId,
                d.Customer.Name,
                d.DespatchDate,
                d.PackingLists.SelectMany(l => l.Items).Sum(i => i.Kilograms),
                d.PackingLists.SelectMany(l => l.Items).Sum(i => i.Meters ?? 0),
                d.InvoiceId != null))
            .ToListAsync(cancellationToken);

    public async Task<Result<BoyahaneInvoiceDto>> SaveInvoiceAsync(int? id, BoyahaneInvoiceRequest request, CancellationToken cancellationToken)
    {
        BoyahaneInvoice? invoice = null;
        if (id is not null)
        {
            invoice = await db.Set<BoyahaneInvoice>().Include(i => i.Lines).Include(i => i.Despatches).SingleOrDefaultAsync(i => i.Id == id, cancellationToken);
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
        var despatches = await db.Set<BoyahaneDespatch>().Where(d => ids.Contains(d.Id)).ToListAsync(cancellationToken);
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

        var customer = await db.Set<BoyahaneCustomer>().AsNoTracking().SingleAsync(c => c.Id == request.CustomerId, cancellationToken);
        var packages = await db.Set<BoyahanePackingItem>().AsNoTracking()
            .Where(p => p.PackingList.DespatchId != null && ids.Contains(p.PackingList.DespatchId.Value))
            .Select(p => new
            {
                p.Kilograms,
                p.Meters,
                LineId = p.BatchItem.OrderLineId,
                p.BatchItem.OrderLine.ProcessType,
                p.BatchItem.OrderLine.Unit,
                p.BatchItem.OrderLine.BillingUnit,
                p.BatchItem.OrderLine.UnitPrice,
                p.BatchItem.OrderLine.ColorName,
                p.BatchItem.OrderLine.Order.Currency,
                CardName = p.BatchItem.OrderLine.Fabric != null ? p.BatchItem.OrderLine.Fabric.Name : p.BatchItem.OrderLine.Yarn != null ? p.BatchItem.OrderLine.Yarn.Name : "",
            })
            .ToListAsync(cancellationToken);
        var currencies = packages.Select(p => p.Currency).Distinct().ToList();
        if (currencies.Count > 1)
        {
            return new Error(InvalidItem, "Bir faturadaki siparişlerin para birimi aynı olmalıdır.", ErrorType.Conflict);
        }

        var prices = (request.Prices ?? []).GroupBy(p => p.OrderLineId).ToDictionary(g => g.Key, g => g.Last().UnitPrice);
        var previous = invoice?.Lines.ToDictionary(l => l.OrderLineId, l => l.UnitPrice) ?? [];

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (invoice is null)
        {
            var sequence = await numbers.NextAsync(InvoiceSeries, Year, cancellationToken);
            invoice = new BoyahaneInvoice { InvoiceNo = $"FT-{Year}-{sequence:D5}" };
            db.Set<BoyahaneInvoice>().Add(invoice);
        }
        else
        {
            db.Set<BoyahaneInvoiceLine>().RemoveRange(invoice.Lines);
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

        foreach (var group in packages.GroupBy(p => p.LineId).OrderBy(g => g.Key))
        {
            var first = group.First();
            var unit = Billing.Resolve(first.BillingUnit, customer.BillingUnit, first.Unit, first.ProcessType == OrderProcessType.YarnDyeing);
            invoice.Lines.Add(new BoyahaneInvoiceLine
            {
                OrderLineId = group.Key,
                Description = string.IsNullOrEmpty(first.ColorName) ? first.CardName : $"{first.CardName} - {first.ColorName}",
                Quantity = unit == StockUnit.Meter ? group.Sum(p => p.Meters ?? 0) : group.Sum(p => p.Kilograms),
                Unit = unit,
                UnitPrice = prices.TryGetValue(group.Key, out var price) ? price : previous.TryGetValue(group.Key, out var kept) ? kept : first.UnitPrice,
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetInvoiceAsync(invoice.Id, cancellationToken);
    }

    public async Task<Result<BoyahaneInvoiceDto>> ConfirmInvoiceAsync(int id, CancellationToken cancellationToken)
    {
        var invoice = await db.Set<BoyahaneInvoice>().Include(i => i.Lines).SingleOrDefaultAsync(i => i.Id == id, cancellationToken);
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

    public async Task<Result<BoyahaneInvoiceDto>> ReopenInvoiceAsync(int id, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await appLock.TryAcquireAsync(BoyahaneAccountService.AccountLock, BoyahaneAccountService.LockTimeout, cancellationToken))
        {
            return new Error(ErrorCodes.ResourceBusy, "Cari işlemi sürüyor, biraz sonra yeniden deneyin.", ErrorType.Conflict);
        }

        var invoice = await db.Set<BoyahaneInvoice>().SingleOrDefaultAsync(i => i.Id == id, cancellationToken);
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

        if (await db.Set<BoyahanePaymentAllocation>().AnyAsync(a => a.InvoiceId == id, cancellationToken))
        {
            return new Error(InvalidState, "Tahsilat dağıtılmış fatura taslağa alınamaz; önce dağıtımı kaldırın.", ErrorType.Conflict);
        }

        invoice.Status = InvoiceStatus.Draft;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetInvoiceAsync(id, cancellationToken);
    }

    public async Task<Result<BoyahaneInvoiceDto>> CancelInvoiceAsync(int id, CancellationToken cancellationToken)
    {
        var invoice = await db.Set<BoyahaneInvoice>().Include(i => i.Despatches).SingleOrDefaultAsync(i => i.Id == id, cancellationToken);
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
