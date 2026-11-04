using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Core.Domain.Enums;
using ERP.Konfeksiyon.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ERP.Konfeksiyon.Application.Features.Warehousing;

/// <summary>Bir müşteri siparişine ayrılmış mamul.</summary>
/// <param name="FromProduction">Ayırma siparişin kendi üretiminin teslim alınmasıyla oluştu.</param>
public sealed record SalesReservationDto(
    int Id,
    int SalesOrderId,
    string OrderNo,
    string? CustomerOrderNo,
    string CustomerName,
    DateOnly DueDate,
    decimal Quantity,
    bool FromProduction,
    DateTime UpdatedAt,
    string UpdatedBy);

/// <summary>Bir iş emrine (reçete malzemesi) ayrılmış stok.</summary>
public sealed record MaterialReservationDto(int ProductionOrderId, string OrderNo, decimal Quantity);

/// <summary>Kartın ayrılabileceği onaylı sipariş kalemi.</summary>
/// <param name="Open">Kalemin ayrılabilecek açık adedi: sipariş − sevk edilen − bütün depolarda zaten ayrılan.</param>
public sealed record ReservationCandidateDto(
    int SalesOrderLineId,
    int SalesOrderId,
    string OrderNo,
    string? CustomerOrderNo,
    string CustomerName,
    DateOnly DueDate,
    int Ordered,
    decimal Shipped,
    decimal Reserved,
    decimal Open);

/// <param name="Free">Ayrılmamış (serbest) stok: mevcut − bütün ayırmalar.</param>
/// <param name="CanReserve">Kart hazır mamul; müşteri siparişine ayrılabilir.</param>
public sealed record StockReservationsDto(
    int WarehouseId,
    string WarehouseCode,
    int StockItemId,
    string ItemCode,
    string ItemName,
    StockUnit Unit,
    decimal OnHand,
    decimal Reserved,
    decimal Free,
    bool CanReserve,
    IReadOnlyList<SalesReservationDto> SalesReservations,
    IReadOnlyList<MaterialReservationDto> MaterialReservations,
    IReadOnlyList<ReservationCandidateDto> Candidates);

public sealed record CreateStockReservationRequest(int WarehouseId, int StockItemId, int SalesOrderLineId, decimal Quantity);

public sealed class CreateStockReservationRequestValidator : AbstractValidator<CreateStockReservationRequest>
{
    public CreateStockReservationRequestValidator()
    {
        RuleFor(x => x.WarehouseId).GreaterThan(0).WithMessage("Depo seçilmelidir.");
        RuleFor(x => x.StockItemId).GreaterThan(0).WithMessage("Stok kartı seçilmelidir.");
        RuleFor(x => x.SalesOrderLineId).GreaterThan(0).WithMessage("Sipariş kalemi seçilmelidir.");
        RuleFor(x => x.Quantity)
            .Must(q => q > 0 && decimal.Round(q) == q).WithMessage("Ayrılacak adet pozitif tam sayı olmalıdır.");
    }
}

public interface IStockReservationService
{
    Task<Result<StockReservationsDto>> GetAsync(int warehouseId, int stockItemId, CancellationToken cancellationToken);

    /// <summary>Serbest stoktan bir onaylı sipariş kalemine ayırır.</summary>
    Task<Result<StockReservationsDto>> CreateAsync(CreateStockReservationRequest request, CancellationToken cancellationToken);

    /// <summary>Bir sipariş ayırmasını bırakır; mal serbest stoka döner.</summary>
    Task<Result<StockReservationsDto>> ReleaseAsync(int reservationId, CancellationToken cancellationToken);
}

/// <summary>
/// Depo stok ekranından ayırma: müşterisi olmayan (serbest) hazır mamul, onaylı bir müşteri siparişinin aynı ürün /
/// renk / beden kalemine ayrılır. Ayırma o siparişin sevkiyatında kullanılır; başka sipariş ve elle çıkışlar ayrılmış
/// stoğu kullanamaz.
/// </summary>
public sealed class StockReservationService(IKonfeksiyonDbContext db, IStockLedger stockLedger) : IStockReservationService
{
    public async Task<Result<StockReservationsDto>> GetAsync(int warehouseId, int stockItemId, CancellationToken cancellationToken)
    {
        var item = await db.StockItems.AsNoTracking().IgnoreQueryFilters()
            .Where(i => i.Id == stockItemId)
            .Select(i => new { i.Id, i.Code, i.Name, i.Unit, i.ItemType, i.ProductId, i.ColorId, i.SizeId })
            .SingleOrDefaultAsync(cancellationToken);
        var warehouse = await db.Warehouses.AsNoTracking().IgnoreQueryFilters()
            .Where(w => w.Id == warehouseId).Select(w => new { w.Id, w.Code }).SingleOrDefaultAsync(cancellationToken);
        if (item is null || warehouse is null)
        {
            return new Error(ErrorCodes.NotFound, "Stok kartı ya da depo bulunamadı.", ErrorType.NotFound);
        }

        var onHand = await db.Stocks.AsNoTracking().Where(s => s.WarehouseId == warehouseId && s.StockItemId == stockItemId)
            .SumAsync(s => (decimal?)s.Quantity, cancellationToken) ?? 0;
        var sales = await db.SalesOrderReservations.AsNoTracking()
            .Where(r => r.WarehouseId == warehouseId && r.StockItemId == stockItemId)
            .OrderBy(r => r.SalesOrder.DueDate).ThenBy(r => r.Id)
            .Select(r => new SalesReservationDto(
                r.Id, r.SalesOrderId, r.SalesOrder.OrderNo, r.SalesOrder.CustomerOrderNo, r.SalesOrder.Customer.Name, r.SalesOrder.DueDate,
                r.Quantity, r.FromProduction, r.UpdatedAt, r.UpdatedBy))
            .ToListAsync(cancellationToken);
        var materials = await db.StockReservations.AsNoTracking()
            .Where(r => r.WarehouseId == warehouseId && r.StockItemId == stockItemId)
            .Select(r => new MaterialReservationDto(r.ProductionOrderId, r.ProductionOrder.OrderNo, r.Quantity))
            .ToListAsync(cancellationToken);
        var reserved = sales.Sum(r => r.Quantity) + materials.Sum(r => r.Quantity);

        var canReserve = item.ItemType == StockItemType.FinishedGood && item.ProductId is not null && item.ColorId is not null && item.SizeId is not null;
        var candidates = new List<ReservationCandidateDto>();
        if (canReserve)
        {
            var lines = await db.SalesOrderLines.AsNoTracking()
                .Where(l => l.SalesOrder.Status == SalesOrderStatus.Approved
                    && l.ProductId == item.ProductId && l.ColorId == item.ColorId && l.SizeId == item.SizeId)
                .Select(l => new
                {
                    l.Id, l.SalesOrderId, l.SalesOrder.OrderNo, l.SalesOrder.CustomerOrderNo, CustomerName = l.SalesOrder.Customer.Name,
                    l.SalesOrder.DueDate, l.Quantity,
                    Reserved = db.SalesOrderReservations.Where(r => r.SalesOrderLineId == l.Id).Sum(r => (decimal?)r.Quantity) ?? 0,
                    Shipped = db.ShipmentItems
                        .Where(i => i.Shipment.SalesOrderId == l.SalesOrderId && i.Shipment.Status == ShipmentStatus.Shipped
                            && i.StockItem.ProductId == l.ProductId && i.StockItem.ColorId == l.ColorId && i.StockItem.SizeId == l.SizeId)
                        .Sum(i => (decimal?)i.Quantity) ?? 0,
                })
                .ToListAsync(cancellationToken);
            candidates = lines
                .Select(l => new ReservationCandidateDto(
                    l.Id, l.SalesOrderId, l.OrderNo, l.CustomerOrderNo, l.CustomerName, l.DueDate, l.Quantity, l.Shipped, l.Reserved,
                    Math.Max(l.Quantity - l.Shipped - l.Reserved, 0)))
                .Where(c => c.Open > 0)
                .OrderBy(c => c.DueDate).ThenBy(c => c.OrderNo, StringComparer.Ordinal)
                .ToList();
        }

        return new StockReservationsDto(
            warehouse.Id, warehouse.Code, item.Id, item.Code, item.Name, item.Unit, onHand, reserved, Math.Max(onHand - reserved, 0),
            canReserve, sales, materials, candidates);
    }

    public async Task<Result<StockReservationsDto>> CreateAsync(CreateStockReservationRequest request, CancellationToken cancellationToken)
    {
        var item = await db.StockItems.AsNoTracking()
            .Where(i => i.Id == request.StockItemId)
            .Select(i => new { i.ItemType, i.ProductId, i.ColorId, i.SizeId })
            .SingleOrDefaultAsync(cancellationToken);
        if (item is null || item.ItemType != StockItemType.FinishedGood)
        {
            return ReferenceValidation.InvalidReference("stockItemId", "Müşteri siparişine yalnızca hazır mamul ayrılabilir.");
        }

        if (!await db.Warehouses.AnyAsync(w => w.Id == request.WarehouseId, cancellationToken))
        {
            return ReferenceValidation.InvalidReference("warehouseId", "Depo bulunamadı.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var line = await db.SalesOrderLines.AsNoTracking()
            .Where(l => l.Id == request.SalesOrderLineId)
            .Select(l => new { l.Id, l.SalesOrderId, l.SalesOrder.OrderNo, l.SalesOrder.Status, l.ProductId, l.ColorId, l.SizeId, l.Quantity })
            .SingleOrDefaultAsync(cancellationToken);
        if (line is null)
        {
            return ReferenceValidation.InvalidReference("salesOrderLineId", "Sipariş kalemi bulunamadı.");
        }

        if (line.Status != SalesOrderStatus.Approved)
        {
            return new Error(ErrorCodes.InvalidStatusTransition, $"{line.OrderNo} siparişi onaylı değil; ayırma yalnızca onaylı siparişe yapılır.");
        }

        if (line.ProductId != item.ProductId || line.ColorId != item.ColorId || line.SizeId != item.SizeId)
        {
            return ReferenceValidation.InvalidReference("salesOrderLineId", "Sipariş kalemi bu mamulün ürün / renk / bedeni değil.");
        }

        var shipped = await db.ShipmentItems
            .Where(i => i.Shipment.SalesOrderId == line.SalesOrderId && i.Shipment.Status == ShipmentStatus.Shipped
                && i.StockItem.ProductId == line.ProductId && i.StockItem.ColorId == line.ColorId && i.StockItem.SizeId == line.SizeId)
            .SumAsync(i => (decimal?)i.Quantity, cancellationToken) ?? 0;
        var reserved = await db.SalesOrderReservations.Where(r => r.SalesOrderLineId == line.Id)
            .SumAsync(r => (decimal?)r.Quantity, cancellationToken) ?? 0;
        var open = line.Quantity - shipped - reserved;
        if (request.Quantity > open)
        {
            var message = $"{line.OrderNo} siparişinin bu kalemine en fazla {Math.Max(open, 0):0.###} adet ayrılabilir "
                + $"(sipariş {line.Quantity}, sevk edilen {shipped:0.###}, zaten ayrılan {reserved:0.###}).";
            return new Error(ErrorCodes.InsufficientStock, message, ErrorType.BusinessRule,
                new Dictionary<string, string[]> { ["quantity"] = [message] });
        }

        var result = await stockLedger.ReserveForSalesLineAsync(
            line.Id, request.WarehouseId, request.StockItemId, request.Quantity, fromProduction: false, partial: false, cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error!;
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(request.WarehouseId, request.StockItemId, cancellationToken);
    }

    public async Task<Result<StockReservationsDto>> ReleaseAsync(int reservationId, CancellationToken cancellationToken)
    {
        var reservation = await db.SalesOrderReservations.SingleOrDefaultAsync(r => r.Id == reservationId, cancellationToken);
        if (reservation is null)
        {
            return new Error(ErrorCodes.NotFound, "Ayırma bulunamadı.", ErrorType.NotFound);
        }

        // Toplamadaki ya da irsaliye bekleyen sevkiyatın malı bırakılırsa sevk anında başka siparişe gitmiş olabilir.
        var inShipment = await db.ShipmentItems.AnyAsync(
            i => i.Shipment.SalesOrderId == reservation.SalesOrderId && i.Shipment.WarehouseId == reservation.WarehouseId
                && i.StockItemId == reservation.StockItemId
                && (i.Shipment.Status == ShipmentStatus.Picking || i.Shipment.Status == ShipmentStatus.Picked),
            cancellationToken);
        if (inShipment)
        {
            return new Error(
                ErrorCodes.InvalidStatusTransition,
                "Bu mal, siparişin toplamadaki ya da irsaliye bekleyen bir sevkiyatında; ayırma sevkiyat iptal edilmeden bırakılamaz.");
        }

        var (warehouseId, stockItemId) = (reservation.WarehouseId, reservation.StockItemId);
        db.SalesOrderReservations.Remove(reservation);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(warehouseId, stockItemId, cancellationToken);
    }
}
