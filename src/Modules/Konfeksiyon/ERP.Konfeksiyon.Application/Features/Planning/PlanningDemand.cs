using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Konfeksiyon.Application.Features.ProductionOrders;
using ERP.Konfeksiyon.Application.Features.Sales;
using ERP.Konfeksiyon.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ERP.Konfeksiyon.Application.Features.Planning;

/// <summary>Planlamayı bekleyen bir renk / beden.</summary>
/// <param name="FromStock">Stoktan karşılanan (ayrılan + ayırmadan sevk edilen).</param>
public sealed record PlanningDemandItemDto(
    int ColorId, string ColorName, int SizeId, string SizeName, int SizeSortOrder,
    int Ordered, int FromStock, int InProduction, int ToPlan);

/// <summary>Onaylı bir müşteri siparişinin, üretime açılmayı bekleyen bir ürünü.</summary>
/// <param name="HasActiveRoute">Modelin aktif operasyon rotası var (yoksa üretim siparişi demetlenemez).</param>
public sealed record PlanningDemandDto(
    int SalesOrderId,
    string SalesOrderNo,
    string CustomerName,
    string? CustomerOrderNo,
    DateOnly DueDate,
    int DaysToDue,
    int ProductId,
    string ProductCode,
    string ProductName,
    string ModelCode,
    bool HasActiveRoute,
    int Ordered,
    int FromStock,
    int InProduction,
    int ToPlan,
    DateTime? ApprovedAt,
    IReadOnlyList<PlanningDemandItemDto> Items);

/// <param name="DueDate">Üretim siparişinin termini; boşsa müşteri siparişinin teslim tarihi.</param>
public sealed record CreateProductionFromDemandRequest(
    int SalesOrderId, int ProductId, IReadOnlyList<ProductionOrderItemRequest> Items, DateOnly? DueDate = null, string? Notes = null);

public sealed class CreateProductionFromDemandRequestValidator : AbstractValidator<CreateProductionFromDemandRequest>
{
    public CreateProductionFromDemandRequestValidator()
    {
        RuleFor(x => x.SalesOrderId).GreaterThan(0);
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("En az bir renk / beden adedi girilmelidir.")
            .Must(items => items.All(i => i.Quantity > 0)).WithMessage("Adetler sıfırdan büyük olmalıdır.")
            .Must(items => items.Select(i => (i.ColorId, i.SizeId)).Distinct().Count() == items.Count)
            .WithMessage("Aynı renk ve beden birden fazla kez girilemez.");
    }
}

public interface IPlanningDemandService
{
    /// <summary>
    /// Onaylı müşteri siparişlerinin, ne stoktan ne üretim siparişinden karşılanmış adedi (ürün başına, renk / beden
    /// dağılımıyla); termini en yakın önce.
    /// </summary>
    Task<IReadOnlyList<PlanningDemandDto>> GetDemandAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Planlamacı, siparişin bir ürünü için üretim siparişi açar (taslak); sipariş bundan sonra Üretim Siparişleri
    /// ekranında onaylanıp banta planlanır.
    /// </summary>
    Task<Result<ProductionOrderDto>> CreateProductionOrderAsync(
        CreateProductionFromDemandRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Satış → planlama → üretim geçişi: onaylanan sipariş doğrudan üretime gitmez; stoktan karşılanmayan kısmı burada
/// görülür ve üretim siparişine planlamacı tarafından dönüştürülür.
/// </summary>
public sealed class PlanningDemandService(
    IKonfeksiyonDbContext db, IProductionOrderService productionOrders, TimeProvider timeProvider) : IPlanningDemandService
{
    public async Task<IReadOnlyList<PlanningDemandDto>> GetDemandAsync(CancellationToken cancellationToken)
    {
        var lines = await db.SalesOrderLines.AsNoTracking()
            .Where(l => l.SalesOrder.Status == SalesOrderStatus.Approved)
            .Select(l => new
            {
                l.SalesOrderId, l.SalesOrder.OrderNo, CustomerName = l.SalesOrder.Customer.Name, l.SalesOrder.CustomerOrderNo,
                l.SalesOrder.DueDate, l.SalesOrder.DecidedAt,
                l.ProductId, l.Product.ProductCode, ProductName = l.Product.Name, l.Product.ModelId, l.Product.Model.ModelCode,
                l.ColorId, ColorName = l.Color.Name, l.SizeId, SizeName = l.Size.Name, l.Size.SortOrder,
                l.Quantity, l.ShippedFromReservation,
            })
            .ToListAsync(cancellationToken);
        if (lines.Count == 0)
        {
            return [];
        }

        var coverage = await SalesOrderCoverage.LoadAsync(db, lines.Select(l => l.SalesOrderId).Distinct().ToList(), cancellationToken);
        var modelIds = lines.Select(l => l.ModelId).Distinct().ToList();
        var routed = (await db.ProductionRoutes.AsNoTracking()
            .Where(r => modelIds.Contains(r.ModelId) && r.Status == RouteStatus.Active)
            .Select(r => r.ModelId)
            .ToListAsync(cancellationToken)).ToHashSet();
        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);

        return lines
            .GroupBy(l => (l.SalesOrderId, l.ProductId))
            .Select(group =>
            {
                var first = group.First();
                var items = group
                    .Select(l =>
                    {
                        var line = coverage.GetValueOrDefault((l.SalesOrderId, l.ProductId, l.ColorId, l.SizeId));
                        var fromStock = (int)(line.Reserved + l.ShippedFromReservation);
                        return new PlanningDemandItemDto(
                            l.ColorId, l.ColorName, l.SizeId, l.SizeName, l.SortOrder, l.Quantity, fromStock, line.InProduction,
                            SalesOrderCoverage.ToPlan(l.Quantity, line.Reserved, l.ShippedFromReservation, line.InProduction));
                    })
                    .OrderBy(i => i.ColorName).ThenBy(i => i.SizeSortOrder)
                    .ToList();
                return new PlanningDemandDto(
                    first.SalesOrderId, first.OrderNo, first.CustomerName, first.CustomerOrderNo, first.DueDate,
                    first.DueDate.DayNumber - today.DayNumber, first.ProductId, first.ProductCode, first.ProductName, first.ModelCode,
                    routed.Contains(first.ModelId), items.Sum(i => i.Ordered), items.Sum(i => i.FromStock),
                    items.Sum(i => i.InProduction), items.Sum(i => i.ToPlan), first.DecidedAt, items);
            })
            .Where(d => d.ToPlan > 0)
            .OrderBy(d => d.DueDate).ThenBy(d => d.SalesOrderNo).ThenBy(d => d.ProductCode)
            .ToList();
    }

    public async Task<Result<ProductionOrderDto>> CreateProductionOrderAsync(
        CreateProductionFromDemandRequest request, CancellationToken cancellationToken)
    {
        var order = await db.SalesOrders.AsNoTracking()
            .Include(o => o.Lines)
            .SingleOrDefaultAsync(o => o.Id == request.SalesOrderId, cancellationToken);
        if (order is null)
        {
            return new Error(ErrorCodes.NotFound, "Müşteri siparişi bulunamadı.", ErrorType.NotFound);
        }

        if (order.Status != SalesOrderStatus.Approved)
        {
            return new Error(
                ErrorCodes.InvalidStatusTransition, "Yalnızca onaylı müşteri siparişinden üretim siparişi açılabilir.");
        }

        // Üretim siparişi yalnızca müşteri siparişindeki renk / bedenler için açılır.
        var foreign = request.Items.FirstOrDefault(i => !order.Lines.Any(l =>
            l.ProductId == request.ProductId && l.ColorId == i.ColorId && l.SizeId == i.SizeId));
        if (foreign is not null)
        {
            return ReferenceValidation.InvalidReference("items", "Müşteri siparişinde bu ürünün olmayan bir renk / bedeni girildi.");
        }

        return await productionOrders.CreateForSalesOrderAsync(
            order.Id,
            new ProductionOrderRequest(
                order.CustomerId, request.ProductId, order.CustomerOrderNo,
                DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime), request.DueDate ?? order.DueDate,
                string.IsNullOrWhiteSpace(request.Notes) ? $"{order.OrderNo} numaralı müşteri siparişinden planlandı." : request.Notes.Trim(),
                request.Items),
            cancellationToken);
    }
}
