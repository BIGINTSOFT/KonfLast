using ERP.Konfeksiyon.Application.Abstractions;
using System.ComponentModel.DataAnnotations;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Konfeksiyon.Application.Features.Costing;
using ERP.Core.Application.Features.Integrations;
using ERP.Konfeksiyon.Application.Features.Integrations;
using ERP.Core.Application.Features.MasterData;
using ERP.Konfeksiyon.Application.Features.Warehousing;
using ERP.Core.Domain.Entities;
using ERP.Konfeksiyon.Domain.Entities;
using ERP.Konfeksiyon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ERP.Konfeksiyon.Application.Features.ProductionOrders;

public sealed class ProductionOrderOptions
{
    public const string SectionName = "ProductionOrders";

    /// <summary>Sipariş numarası öneki: {önek}-{yıl}-{sıra}, örn. IE-2026-00125.</summary>
    [Required, RegularExpression("^[A-Z0-9]{1,10}$")]
    public string NumberPrefix { get; set; } = "IE";

    [Range(3, 9)]
    public int NumberDigits { get; set; } = 5;
}

public interface IProductionOrderService
{
    Task<PagedResult<ProductionOrderListItemDto>> GetListAsync(
        ProductionOrderListQuery query, CancellationToken cancellationToken);

    Task<Result<ProductionOrderDto>> GetAsync(int id, CancellationToken cancellationToken);
    Task<Result<ProductionOrderDto>> CreateAsync(ProductionOrderRequest request, CancellationToken cancellationToken);

    /// <summary>Müşteri siparişinden taslak üretim siparişi açar ve ona bağlar.</summary>
    Task<Result<ProductionOrderDto>> CreateForSalesOrderAsync(
        int salesOrderId, ProductionOrderRequest request, CancellationToken cancellationToken);

    Task<Result<ProductionOrderDto>> UpdateAsync(
        int id, ProductionOrderRequest request, CancellationToken cancellationToken);

    /// <summary>Siparişi hedef duruma geçirir (onayla, taslağa al, iptal et, tamamla).</summary>
    /// <summary>Operasyon sırasını (sıralı / sırasız) değiştirir; ilk demet açılana kadar.</summary>
    Task<Result<ProductionOrderDto>> ChangeOperationSequenceAsync(
        int id, OperationSequenceRequest request, CancellationToken cancellationToken);

    Task<Result<ProductionOrderDto>> TransitionAsync(
        int id, ProductionOrderStatus target, string rowVersion, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken);

    /// <summary>Banta planlanabilecek (onaylı veya üretimdeki) siparişler; seçim kutuları için.</summary>
    Task<IReadOnlyList<LookupDto>> LookupPlannableAsync(LookupQuery query, CancellationToken cancellationToken);
}

public sealed class ProductionOrderService(
    IKonfeksiyonDbContext db,
    IDocumentNumberGenerator numberGenerator,
    IIntegrationOutbox outbox,
    IStockLedger stockLedger,
    ICostingService costing,
    TimeProvider timeProvider,
    IOptions<ProductionOrderOptions> options) : IProductionOrderService
{
    private static readonly Error NotFound = new(ErrorCodes.NotFound, "İş emri bulunamadı.", ErrorType.NotFound);

    private static readonly Error ConcurrencyConflict = new(
        ErrorCodes.ConcurrencyConflict,
        "Sipariş siz açtıktan sonra başka bir kullanıcı tarafından değiştirildi. Sayfayı yenileyip tekrar deneyin.",
        ErrorType.Conflict);

    private static readonly IReadOnlyDictionary<ProductionOrderStatus, string> StatusLabels =
        new Dictionary<ProductionOrderStatus, string>
        {
            [ProductionOrderStatus.Draft] = "Taslak",
            [ProductionOrderStatus.Released] = "Onaylı",
            [ProductionOrderStatus.InProduction] = "Üretimde",
            [ProductionOrderStatus.Completed] = "Tamamlandı",
            [ProductionOrderStatus.Cancelled] = "İptal",
        };

    private readonly ProductionOrderOptions _options = options.Value;

    public Task<PagedResult<ProductionOrderListItemDto>> GetListAsync(
        ProductionOrderListQuery query, CancellationToken cancellationToken)
    {
        var orders = db.ProductionOrders.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            orders = orders.Where(o =>
                o.OrderNo.Contains(search)
                || o.CustomerOrderNo!.Contains(search)
                || o.Product.ProductCode.Contains(search)
                || o.Customer.Name.Contains(search));
        }

        if (query.Status is { } status)
        {
            orders = orders.Where(o => o.Status == status);
        }

        if (query.CustomerId is { } customerId)
        {
            orders = orders.Where(o => o.CustomerId == customerId);
        }

        if (query.DueFrom is { } dueFrom)
        {
            orders = orders.Where(o => o.DueDate >= dueFrom);
        }

        if (query.DueTo is { } dueTo)
        {
            orders = orders.Where(o => o.DueDate <= dueTo);
        }

        return orders
            .OrderByDescending(o => o.Id)
            .Select(o => new ProductionOrderListItemDto(
                o.Id,
                o.OrderNo,
                o.CustomerOrderNo,
                o.CustomerId,
                o.Customer.Name,
                o.ProductId,
                o.Product.ProductCode,
                o.Product.Name,
                o.Product.Model.ModelCode,
                o.OrderDate,
                o.DueDate,
                o.Status,
                o.Items.Sum(i => (int?)i.Quantity) ?? 0,
                o.SalesOrder == null ? null : o.SalesOrder.OrderNo))
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<Result<ProductionOrderDto>> GetAsync(int id, CancellationToken cancellationToken)
    {
        var order = await db.ProductionOrders
            .AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.SalesOrder)
            .Include(o => o.Product).ThenInclude(p => p.Model)
            .Include(o => o.Items).ThenInclude(i => i.Color)
            .Include(o => o.Items).ThenInclude(i => i.Size)
            .SingleOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (order is null)
        {
            return NotFound;
        }

        return ToDto(order, await CanChangeOperationSequenceAsync(order, cancellationToken), await HasStartedBundlesAsync(order.Id, cancellationToken));
    }

    public Task<Result<ProductionOrderDto>> CreateAsync(
        ProductionOrderRequest request, CancellationToken cancellationToken) =>
        CreateAsync(request, null, cancellationToken);

    public Task<Result<ProductionOrderDto>> CreateForSalesOrderAsync(
        int salesOrderId, ProductionOrderRequest request, CancellationToken cancellationToken) =>
        CreateAsync(request, salesOrderId, cancellationToken);

    private async Task<Result<ProductionOrderDto>> CreateAsync(
        ProductionOrderRequest request, int? salesOrderId, CancellationToken cancellationToken)
    {
        if (await ValidateAsync(request, null, cancellationToken) is { } error)
        {
            return error;
        }

        var order = new ProductionOrder { SalesOrderId = salesOrderId };
        Apply(request, order);
        foreach (var item in request.Items)
        {
            order.Items.Add(new ProductionOrderItem
            {
                ColorId = item.ColorId, SizeId = item.SizeId, Quantity = item.Quantity,
            });
        }

        // Numara ve sipariş aynı transaction'da: kayıt başarısız olursa numara da geri alınır.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var year = timeProvider.GetLocalNow().Year;
        var sequence = await numberGenerator.NextAsync(DocumentSeries.ProductionOrder, year, cancellationToken);
        order.OrderNo = $"{_options.NumberPrefix}-{year}-{sequence.ToString().PadLeft(_options.NumberDigits, '0')}";

        db.ProductionOrders.Add(order);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await GetAsync(order.Id, cancellationToken);
    }

    public async Task<Result<ProductionOrderDto>> UpdateAsync(
        int id, ProductionOrderRequest request, CancellationToken cancellationToken)
    {
        var order = await db.ProductionOrders
            .Include(o => o.Items)
            .SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
        {
            return NotFound;
        }

        if (CheckVersion(order, request.RowVersion) is { } versionError)
        {
            return versionError;
        }

        if (!order.IsEditable)
        {
            return new Error(
                ErrorCodes.OrderNotEditable,
                $"{StatusLabels[order.Status]} durumundaki sipariş değiştirilemez. Değişiklik için taslağa alın.");
        }

        // Müşteri siparişinden açılan üretim siparişi o siparişin ürününü karşılar: ürünü ya da müşterisi değişirse
        // müşteri siparişi ürünü "aktarılmamış" sayar ve ikinci bir üretim siparişi açardı.
        if (order.SalesOrderId is not null && (request.ProductId != order.ProductId || request.CustomerId != order.CustomerId))
        {
            const string message =
                "Bu iş emri bir müşteri siparişinden açıldı; ürünü ve müşterisi değiştirilemez. "
                + "Farklı ürün için bu siparişi silip müşteri siparişini revize edin.";
            return new Error(
                ErrorCodes.OrderLinkedToSalesOrder, message, ErrorType.BusinessRule,
                new Dictionary<string, string[]>
                {
                    [request.ProductId != order.ProductId ? "productId" : "customerId"] = [message],
                });
        }

        if (await ValidateAsync(request, order, cancellationToken) is { } error)
        {
            return error;
        }

        Apply(request, order);
        SyncItems(order, request.Items);

        return await SaveAndReloadAsync(order, cancellationToken);
    }

    public async Task<Result<ProductionOrderDto>> TransitionAsync(
        int id, ProductionOrderStatus target, string rowVersion, CancellationToken cancellationToken)
    {
        var order = await db.ProductionOrders
            .Include(o => o.Items)
            .SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
        {
            return NotFound;
        }

        if (CheckVersion(order, rowVersion) is { } versionError)
        {
            return versionError;
        }

        if (!order.CanTransitionTo(target))
        {
            return new Error(
                ErrorCodes.InvalidStatusTransition,
                $"{StatusLabels[order.Status]} durumundaki sipariş '{StatusLabels[target]}' durumuna geçirilemez.");
        }

        if (target == ProductionOrderStatus.Released && order.Items.Count == 0)
        {
            return new Error(ErrorCodes.OrderHasNoItems, "Kalemi olmayan sipariş onaylanamaz.");
        }

        if (target == ProductionOrderStatus.Completed)
        {
            // Sipariş kendiliğinden tamamlanmaz (hatalı parçalar yüzünden eksik kalmış olabilir); kullanıcı
            // tamamlar. Üretime girmemiş demetler aşağıda iptal edilir, sahada işi süren demet varken kapatılamaz.
            var openBundles = await db.Bundles.CountAsync(
                b => b.ProductionOrderId == id
                    && b.Status != BundleStatus.Created
                    && b.Status != BundleStatus.Waiting
                    && b.Status != BundleStatus.Completed
                    && b.Status != BundleStatus.Closed
                    && b.Status != BundleStatus.Cancelled,
                cancellationToken);
            if (openBundles > 0)
            {
                return new Error(
                    ErrorCodes.OrderHasOpenBundles,
                    $"Üretimde {openBundles} demet var. Kalanını fire bildirin ya da bitirin, sonra siparişi tamamlayın.");
            }
        }

        var openPlans = await db.LineProductionPlans
            .Where(p => p.ProductionOrderId == id && p.Status == LinePlanStatus.Planned)
            .ToListAsync(cancellationToken);

        if (target == ProductionOrderStatus.Draft && openPlans.Count > 0)
        {
            return new Error(
                ErrorCodes.OrderHasLinePlans,
                "Sipariş bantlara planlanmış; taslağa almadan önce bant planlarını iptal edin.");
        }

        var bundles = target is ProductionOrderStatus.Draft or ProductionOrderStatus.Cancelled or ProductionOrderStatus.Completed
            ? await db.Bundles
                .Include(b => b.Operations)
                .Where(b => b.ProductionOrderId == id && b.Status != BundleStatus.Cancelled)
                .ToListAsync(cancellationToken)
            : [];

        if (target == ProductionOrderStatus.Draft && bundles.Count > 0)
        {
            return new Error(
                ErrorCodes.OrderHasBundles,
                "Sipariş için demet oluşturulmuş; taslağa almadan önce demetleri iptal edin.");
        }

        // İptal ve tamamlamada üretime girmemiş demetlerin malzemesi depoya döner; stok hareketi ile sipariş tek işlemde kaydedilir.
        var unstarted = target is ProductionOrderStatus.Cancelled or ProductionOrderStatus.Completed
            ? bundles.Where(b => b.CanCancel).ToList()
            : [];
        await using var transaction = (target == ProductionOrderStatus.Cancelled && bundles.Count > 0) || unstarted.Count > 0
            ? await db.Database.BeginTransactionAsync(cancellationToken)
            : null;

        if (target is ProductionOrderStatus.Cancelled or ProductionOrderStatus.Completed)
        {
            if (target == ProductionOrderStatus.Cancelled && bundles.Any(b => !b.CanCancel))
            {
                return new Error(
                    ErrorCodes.OrderHasBundles,
                    "Siparişin üretime girmiş demetleri var; iptal edilemez. Süren demetlerin kalanını fire bildirip siparişi tamamlayın.");
            }

            // İptal edilen siparişin bekleyen bant planları ve üretime girmemiş demetleri de
            // aynı kayıtla iptal edilir.
            foreach (var plan in openPlans)
            {
                plan.Cancel();
            }

            foreach (var bundle in unstarted)
            {
                bundle.Cancel();
            }

            var bundleIds = unstarted.Select(b => b.Id).ToList();
            var cardAssignments = await db.BundleCardAssignments
                .Where(a => bundleIds.Contains(a.BundleId) && a.ReleasedAt == null)
                .ToListAsync(cancellationToken);
            foreach (var assignment in cardAssignments)
            {
                assignment.Release(timeProvider.GetUtcNow().UtcDateTime);
            }

            if (transaction is not null
                && await stockLedger.PostProductionReturnAsync(bundleIds, cancellationToken) is { } stockError)
            {
                return stockError;
            }
        }

        order.TransitionTo(target);

        if (target == ProductionOrderStatus.Completed)
        {
            await EnqueueCompletedAsync(order, cancellationToken);
        }

        // Üretime kapalı siparişin ayrılmış malzemesi yeniden kullanılabilir olur.
        if (target is ProductionOrderStatus.Draft or ProductionOrderStatus.Completed or ProductionOrderStatus.Cancelled)
        {
            await stockLedger.ReleaseReservationsAsync(id, cancellationToken);
        }

        var result = await SaveAndReloadAsync(order, cancellationToken);
        if (transaction is not null && result.IsSuccess)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        // Onaylanan siparişin reçete malzemesi ayrılır. Ayırma yapılamazsa (reçete yok, depo belirsiz, stok yok)
        // onay yine geçerlidir; malzeme sipariş ekranından sonradan ayrılabilir.
        if (target == ProductionOrderStatus.Released && result.IsSuccess)
        {
            await costing.ReserveMaterialsAsync(id, null, cancellationToken);
        }

        return result;
    }

    public async Task<Result<ProductionOrderDto>> ChangeOperationSequenceAsync(
        int id, OperationSequenceRequest request, CancellationToken cancellationToken)
    {
        var order = await db.ProductionOrders.SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
        {
            return NotFound;
        }

        if (!await CanChangeOperationSequenceAsync(order, cancellationToken))
        {
            return new Error(
                ErrorCodes.OrderNotEditable,
                "Siparişin demetleri açılmış ya da sipariş kapanmış; operasyon sırası artık değiştirilemez.");
        }

        if (CheckVersion(order, request.RowVersion) is { } versionError)
        {
            return versionError;
        }

        order.OperationSequence = request.OperationSequence;
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

    /// <summary>
    /// Operasyon sırası, siparişin ilk (iptal edilmemiş) demeti açılana kadar değiştirilebilir: açık demetler
    /// açıldıkları andaki sırayla yürür.
    /// </summary>
    private async Task<bool> CanChangeOperationSequenceAsync(ProductionOrder order, CancellationToken cancellationToken) =>
        order.Status is ProductionOrderStatus.Draft or ProductionOrderStatus.Released or ProductionOrderStatus.InProduction
        && !await db.Bundles.AnyAsync(
            b => b.ProductionOrderId == order.Id && b.Status != BundleStatus.Cancelled, cancellationToken);

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var order = await db.ProductionOrders.SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
        {
            return NotFound;
        }

        if (!order.IsEditable)
        {
            return new Error(
                ErrorCodes.OrderNotEditable,
                $"{StatusLabels[order.Status]} durumundaki sipariş silinemez; bunun yerine iptal edin.");
        }

        // Soft delete: kayıt ve numarası korunur, numara yeniden kullanılmaz.
        db.ProductionOrders.Remove(order);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<IReadOnlyList<LookupDto>> LookupPlannableAsync(
        LookupQuery query, CancellationToken cancellationToken)
    {
        var orders = db.ProductionOrders
            .AsNoTracking()
            .Where(o => o.Status == ProductionOrderStatus.Released || o.Status == ProductionOrderStatus.InProduction);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            orders = orders.Where(o =>
                o.OrderNo.Contains(search) || o.Product.ProductCode.Contains(search) || o.Customer.Name.Contains(search));
        }

        var result = await orders
            .OrderByDescending(o => o.Id)
            .Take(query.NormalizedTake)
            .Select(ToLookup)
            .ToListAsync(cancellationToken);

        // Formda seçili olan sipariş, artık planlanabilir olmasa da listede kalır.
        if (query.IncludeId is { } includeId && result.All(item => item.Id != includeId))
        {
            result.InsertRange(
                0,
                await db.ProductionOrders.AsNoTracking().Where(o => o.Id == includeId).Select(ToLookup).ToListAsync(cancellationToken));
        }

        return result;
    }

    private static readonly System.Linq.Expressions.Expression<Func<ProductionOrder, LookupDto>> ToLookup = o => new LookupDto(
        o.Id, o.OrderNo, o.Customer.Name + " · " + o.Product.ProductCode);

    private async Task<Error?> ValidateAsync(
        ProductionOrderRequest request, ProductionOrder? existing, CancellationToken cancellationToken)
    {
        var referenceError =
            await ReferenceValidation.RequireSelectableAsync(
                db.Customers, request.CustomerId, existing?.CustomerId, "customerId", "Müşteri", cancellationToken)
            ?? await ReferenceValidation.RequireSelectableAsync(
                db.Products, request.ProductId, existing?.ProductId, "productId", "Ürün", cancellationToken)
            ?? await ReferenceValidation.RequireSelectableAsync(
                db.Warehouses, request.MaterialWarehouseId, existing?.MaterialWarehouseId, "materialWarehouseId", "Depo",
                cancellationToken)
            ?? await ReferenceValidation.RequireSelectableAsync(
                db.Colors,
                request.Items.Select(i => i.ColorId).Distinct().ToList(),
                existing?.Items.Select(i => i.ColorId).ToHashSet() ?? [],
                "items", "Renk", cancellationToken)
            ?? await ReferenceValidation.RequireSelectableAsync(
                db.Sizes,
                request.Items.Select(i => i.SizeId).Distinct().ToList(),
                existing?.Items.Select(i => i.SizeId).ToHashSet() ?? [],
                "items", "Beden", cancellationToken);
        if (referenceError is not null)
        {
            return referenceError;
        }

        // Model bir müşteriye özelse o modelin ürünü başka müşteriye üretilemez.
        var modelCustomerId = await db.Products
            .Where(p => p.Id == request.ProductId)
            .Select(p => p.Model.CustomerId)
            .SingleAsync(cancellationToken);

        if (modelCustomerId is not null && modelCustomerId != request.CustomerId)
        {
            const string message = "Seçilen ürünün modeli başka bir müşteriye ait.";
            return new Error(
                ErrorCodes.ProductCustomerMismatch,
                message,
                ErrorType.BusinessRule,
                new Dictionary<string, string[]> { ["productId"] = [message] });
        }

        return null;
    }

    private static void Apply(ProductionOrderRequest request, ProductionOrder order)
    {
        order.CustomerId = request.CustomerId;
        order.ProductId = request.ProductId;
        order.CustomerOrderNo = MasterDataRules.NullIfBlank(request.CustomerOrderNo);
        order.OrderDate = request.OrderDate;
        order.DueDate = request.DueDate;
        order.Notes = MasterDataRules.NullIfBlank(request.Notes);
        order.MaterialWarehouseId = request.MaterialWarehouseId;
        order.OperationSequence = request.OperationSequence;
    }

    /// <summary>Kalemleri istekle eşitler: kalan kalemin adedi güncellenir, eksilen silinir, yeni gelen eklenir.</summary>
    private void SyncItems(ProductionOrder order, IReadOnlyList<ProductionOrderItemRequest> requested)
    {
        var wanted = requested.ToDictionary(i => (i.ColorId, i.SizeId), i => i.Quantity);

        foreach (var item in order.Items.ToList())
        {
            if (wanted.Remove((item.ColorId, item.SizeId), out var quantity))
            {
                item.Quantity = quantity;
            }
            else
            {
                db.ProductionOrderItems.Remove(item);
            }
        }

        foreach (var ((colorId, sizeId), quantity) in wanted)
        {
            db.ProductionOrderItems.Add(new ProductionOrderItem
            {
                ProductionOrderId = order.Id, ColorId = colorId, SizeId = sizeId, Quantity = quantity,
            });
        }
    }

    private static Error? CheckVersion(ProductionOrder order, string? rowVersion) =>
        RowVersionToken.Check(order.RowVersion, rowVersion, ConcurrencyConflict);

    /// <summary>Tamamlanma olayını, durum değişikliğiyle aynı kayıtta dış sistemlere duyurulmak üzere yazar.</summary>
    private async Task EnqueueCompletedAsync(ProductionOrder order, CancellationToken cancellationToken)
    {
        // Silinmiş (soft delete) müşteri / ürün / renk / beden de koduyla yer alır.
        var header = await db.ProductionOrders
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(o => o.Id == order.Id)
            .Select(o => new
            {
                o.Customer.CustomerCode,
                CustomerName = o.Customer.Name,
                o.Customer.TaxNumber,
                o.Product.ProductCode,
                ProductName = o.Product.Name,
            })
            .SingleAsync(cancellationToken);
        var lines = await db.ProductionOrderItems
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(i => i.ProductionOrderId == order.Id)
            .OrderBy(i => i.Color.ColorCode)
            .ThenBy(i => i.Size.SortOrder)
            .Select(i => new ProductionOrderLinePayload(i.Color.ColorCode, i.Size.SizeCode, i.Quantity))
            .ToListAsync(cancellationToken);
        var receivedIntoStock = await db.StockTransactions
            .Where(t => t.ProductionOrderId == order.Id && t.Type == StockTransactionType.ProductionReceipt)
            .SumAsync(t => (decimal?)t.Quantity, cancellationToken) ?? 0;

        outbox.Enqueue(
            IntegrationEventTypes.ProductionOrderCompleted,
            nameof(ProductionOrder),
            order.Id,
            order.OrderNo,
            new ProductionOrderCompletedPayload(
                order.OrderNo,
                order.CustomerOrderNo,
                new PartyPayload(header.CustomerCode, header.CustomerName, header.TaxNumber),
                header.ProductCode,
                header.ProductName,
                order.OrderDate,
                order.DueDate,
                lines.Sum(l => l.Quantity),
                receivedIntoStock,
                lines));
    }

    private async Task<Result<ProductionOrderDto>> SaveAndReloadAsync(
        ProductionOrder order, CancellationToken cancellationToken)
    {
        // Yalnızca kalemler değişse bile başlık satırı güncellenir: rowversion ilerler ve
        // yükleme ile kayıt arasındaki eşzamanlı değişiklikleri veritabanı yakalar.
        db.Entry(order).State = EntityState.Modified;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ConcurrencyConflict;
        }

        return await GetAsync(order.Id, cancellationToken);
    }

    /// <summary>Üretime girmiş demeti olan sipariş iptal edilemez, tamamlanır.</summary>
    private Task<bool> HasStartedBundlesAsync(int orderId, CancellationToken cancellationToken) =>
        db.Bundles.AnyAsync(
            b => b.ProductionOrderId == orderId
                && b.Status != BundleStatus.Created
                && b.Status != BundleStatus.Waiting
                && b.Status != BundleStatus.Cancelled,
            cancellationToken);

    private static ProductionOrderDto ToDto(ProductionOrder order, bool canChangeOperationSequence, bool hasStartedBundles) => new(
        order.Id,
        order.OrderNo,
        order.CustomerOrderNo,
        order.CustomerId,
        order.Customer.CustomerCode,
        order.Customer.Name,
        order.ProductId,
        order.Product.ProductCode,
        order.Product.Name,
        order.Product.Model.ModelCode,
        order.OrderDate,
        order.DueDate,
        order.Status,
        order.Notes,
        order.Items.Sum(i => i.Quantity),
        order.IsEditable,
        // Üretimde durumuna yalnızca üretim akışı geçirir; kullanıcıya işlem olarak sunulmaz.
        order.AllowedTransitions
            .Where(s => s != ProductionOrderStatus.InProduction && !(s == ProductionOrderStatus.Cancelled && hasStartedBundles))
            .ToList(),
        RowVersionToken.From(order.RowVersion),
        order.CreatedAt,
        order.CreatedBy,
        order.Items
            .OrderBy(i => i.Color.ColorCode).ThenBy(i => i.Size.SortOrder).ThenBy(i => i.Size.SizeCode)
            .Select(i => new ProductionOrderItemDto(
                i.Id, i.ColorId, i.Color.ColorCode, i.Color.Name, i.SizeId, i.Size.SizeCode, i.Size.Name,
                i.Size.SortOrder, i.Quantity))
            .ToList(),
        order.MaterialWarehouseId,
        order.SalesOrderId,
        order.SalesOrder?.OrderNo,
        order.OperationSequence,
        canChangeOperationSequence);
}
