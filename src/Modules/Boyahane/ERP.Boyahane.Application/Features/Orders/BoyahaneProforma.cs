using ERP.Boyahane.Domain.Entities;
using ERP.Boyahane.Domain.Enums;
using ERP.Boyahane.Domain.Rules;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERP.Boyahane.Application.Features.Orders;

/// <param name="UnitPrice">Fatura ölçüsünün birim fiyatı (<paramref name="PriceUnit"/>).</param>
/// <param name="Amount">Fatura ölçüsü sipariş birimiyle aynıysa miktar × fiyat; değilse tutar çıkış ölçüsünden çıkacağı için boş.</param>
public sealed record BoyahaneProformaLineDto(
    int Id,
    OrderProcessType ProcessType,
    bool IsYarn,
    string ItemCode,
    string ItemName,
    string? ColorName,
    string? ColorCode,
    decimal Quantity,
    StockUnit Unit,
    decimal? UnitPrice,
    StockUnit PriceUnit,
    decimal? Amount);

/// <summary>Proforma / sipariş teyidi: müşteriye gönderilen, fatura yerine geçmeyen belge.</summary>
/// <param name="VatRate">Müşterinin varsayılan KDV oranı; tanımlı değilse sıfır yazılır ve <paramref name="VatRateMissing"/> işaretlenir.</param>
/// <param name="HasUnestimatedLines">Fiyatı girilmemiş ya da fatura ölçüsü farklı olduğu için tutarı hesaplanamayan satır var.</param>
public sealed record BoyahaneProformaDto(
    int OrderId,
    string OrderNo,
    OrderStatus Status,
    DateOnly OrderDate,
    DateOnly? DueDate,
    string? CustomerOrderNo,
    string CustomerCode,
    string CustomerName,
    string? CustomerTaxNumber,
    string? CustomerTaxOffice,
    string? CustomerAddress,
    string Currency,
    decimal VatRate,
    bool VatRateMissing,
    int? PaymentTermDays,
    decimal Subtotal,
    decimal VatAmount,
    decimal Total,
    bool HasUnestimatedLines,
    string? Notes,
    DocumentLanguage DocumentLanguage,
    IReadOnlyList<BoyahaneProformaLineDto> Lines);

public interface IBoyahaneProformaService
{
    Task<Result<BoyahaneProformaDto>> GetAsync(int orderId, CancellationToken cancellationToken);
}

/// <summary>Taslak ya da onaylı müşterili siparişin proforma / teyit belgesi; kayıt tutulmaz, siparişten okunur.</summary>
public sealed class BoyahaneProformaService(IAppDbContext db) : IBoyahaneProformaService
{
    private const string InvalidState = "BOYAHANE_ORDER_INVALID_STATE";

    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Sipariş bulunamadı.", ErrorType.NotFound);

    public async Task<Result<BoyahaneProformaDto>> GetAsync(int orderId, CancellationToken cancellationToken)
    {
        var order = await db.Set<BoyahaneOrder>().AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.Lines).ThenInclude(l => l.Fabric)
            .Include(o => o.Lines).ThenInclude(l => l.Yarn)
            .SingleOrDefaultAsync(o => o.Id == orderId, cancellationToken);
        if (order is null)
        {
            return NotFound;
        }

        if (order.Status is not (OrderStatus.Draft or OrderStatus.Confirmed))
        {
            return new Error(InvalidState, "Proforma yalnızca taslak ya da onaylı sipariş için basılır.", ErrorType.Conflict);
        }

        if (order.Customer is not { } customer)
        {
            return new Error(InvalidState, "Stoğa üretim siparişinin müşterisi olmadığı için proforma basılamaz.", ErrorType.Conflict);
        }

        var address = await db.Set<BoyahaneCustomerAddress>().AsNoTracking()
            .Where(a => a.CustomerId == customer.Id && a.IsDefaultInvoice)
            .Select(a => a.AddressLine + " " + a.District + "/" + a.City)
            .FirstOrDefaultAsync(cancellationToken);

        var lines = order.Lines.OrderBy(l => l.Id).Select(l =>
        {
            var isYarn = l.ProcessType == OrderProcessType.YarnDyeing;
            var priceUnit = Billing.Resolve(l.BillingUnit, customer.BillingUnit, l.Unit, isYarn);
            return new BoyahaneProformaLineDto(
                l.Id,
                l.ProcessType,
                isYarn,
                l.Fabric?.FabricCode ?? l.Yarn?.YarnCode ?? "",
                l.Fabric?.Name ?? l.Yarn?.Name ?? "",
                l.ColorName,
                l.ColorCode,
                l.Quantity,
                l.Unit,
                l.UnitPrice,
                priceUnit,
                l.UnitPrice is { } price && priceUnit == l.Unit ? InvoiceAmounts.Line(l.Quantity, price) : null);
        }).ToList();

        var vatRate = customer.VatRate ?? 0;
        var amounts = InvoiceAmounts.Of(lines.Sum(l => l.Amount ?? 0), vatRate, null);
        return new BoyahaneProformaDto(
            order.Id,
            order.OrderNo,
            order.Status,
            order.OrderDate,
            order.DueDate,
            order.CustomerOrderNo,
            customer.CustomerCode,
            customer.Name,
            customer.TaxNumber,
            customer.TaxOffice,
            address,
            order.Currency,
            vatRate,
            customer.VatRate is null,
            customer.PaymentTermDays,
            amounts.Subtotal,
            amounts.Vat,
            amounts.Payable,
            lines.Any(l => l.Amount is null),
            order.Notes,
            customer.DocumentLanguage,
            lines);
    }
}
