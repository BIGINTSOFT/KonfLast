using ERP.Konfeksiyon.Application.Abstractions;
using System.ComponentModel.DataAnnotations;
using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Konfeksiyon.Application.Features.Pricing;
using ERP.Core.Application.Features.Finance;
using ERP.Core.Application.Features.MasterData;
using ERP.Konfeksiyon.Application.Features.ProductionOrders;
using ERP.Core.Domain.Entities;
using ERP.Konfeksiyon.Domain.Entities;
using ERP.Core.Domain.Enums;
using ERP.Konfeksiyon.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using static ERP.Core.Application.Features.MasterData.MasterDataRules;

namespace ERP.Konfeksiyon.Application.Features.Sales;

public sealed class SalesOrderOptions
{
    public const string SectionName = "SalesOrders";

    /// <summary>Sipariş numarası öneki: {önek}-{yıl}-{sıra}, örn. MS-2026-00042.</summary>
    [Required, RegularExpression("^[A-Z0-9]{1,10}$")]
    public string NumberPrefix { get; set; } = "MS";

    [Range(3, 9)]
    public int NumberDigits { get; set; } = 5;
}

/// <param name="TotalAmount">Fiyatı girilmiş kalemlerin toplamı; hiçbirinde fiyat yoksa null.</param>
/// <param name="ShippedQuantity">Siparişe bağlı sevk edilmiş sevkiyatlardaki toplam adet.</param>
/// <param name="RemainingQuantity">
/// Sevk edilmeyi bekleyen adet: kalem başına sipariş − sevk (eksiye düşmez) toplamı; bir kalemdeki fazla sevk
/// başka kalemin eksiğini kapatmaz.
/// </param>
/// <param name="IsOverdue">Teslim tarihi geçmiş, kapanmamış ve sevk edilecek adedi kalmış sipariş.</param>
public sealed record SalesOrderListItemDto(
    int Id,
    string OrderNo,
    string? CustomerOrderNo,
    int CustomerId,
    string CustomerName,
    DateOnly OrderDate,
    DateOnly DueDate,
    SalesOrderStatus Status,
    int ProductCount,
    int TotalQuantity,
    decimal? TotalAmount,
    string Currency,
    int ShippedQuantity = 0,
    int RemainingQuantity = 0,
    bool IsOverdue = false);

/// <param name="LineAmount">Adet × birim fiyat; fiyat girilmemişse null.</param>
/// <param name="ShippedQuantity">Bu siparişe bağlı sevkiyatlarla müşteriye sevk edilmiş adet.</param>
/// <param name="ReservedQuantity">Depoda bu kaleme ayrılmış hazır mamul.</param>
/// <param name="InProductionQuantity">Siparişten açılmış (iptal edilmemiş) üretim siparişlerindeki adet.</param>
/// <param name="ToPlanQuantity">Ne stoktan ne üretimden karşılanan, planlamayı bekleyen adet.</param>
/// <param name="CustomerArticleCode">Müşterinin bu ürün için kullandığı kendi ürün (artikel) kodu; ürün kartından.</param>
public sealed record SalesOrderLineDto(
    int Id,
    int ProductId,
    string ProductCode,
    string ProductName,
    int ColorId,
    string ColorCode,
    string ColorName,
    int SizeId,
    string SizeCode,
    string SizeName,
    int SizeSortOrder,
    int Quantity,
    decimal? UnitPrice,
    decimal? LineAmount,
    int ShippedQuantity = 0,
    string? CustomerArticleCode = null,
    decimal VatRate = 0,
    decimal ReservedQuantity = 0,
    int InProductionQuantity = 0,
    int ToPlanQuantity = 0);

/// <summary>Müşteri siparişinden açılmış bir üretim siparişi.</summary>
/// <param name="OrderedQuantity">Müşteri siparişinde bu ürün için istenen adet (üretim adediyle karşılaştırmak için).</param>
public sealed record SalesOrderProductionDto(
    int Id, string OrderNo, int ProductId, string ProductCode, ProductionOrderStatus Status, int TotalQuantity,
    int OrderedQuantity = 0);

/// <param name="CanGenerateProduction">Onaylı ve planlamayı bekleyen (stoktan ya da üretimden karşılanmamış) adedi var.</param>
/// <param name="AllowedTransitions">Siparişin şu an geçebileceği durumlar; arayüz işlemleri buna göre sunar.</param>
/// <param name="RowVersion">Güncelleme ve durum işlemlerinde geri gönderilir (eşzamanlılık kontrolü).</param>
public sealed record SalesOrderDto(
    int Id,
    string OrderNo,
    string? CustomerOrderNo,
    int CustomerId,
    string CustomerCode,
    string CustomerName,
    DateOnly OrderDate,
    DateOnly DueDate,
    string Currency,
    SalesOrderStatus Status,
    string? Notes,
    int TotalQuantity,
    decimal? TotalAmount,
    bool IsEditable,
    bool CanGenerateProduction,
    IReadOnlyList<SalesOrderStatus> AllowedTransitions,
    DateTime? SubmittedAt,
    string? SubmittedBy,
    DateTime? DecidedAt,
    string? DecidedBy,
    string? DecisionNote,
    string RowVersion,
    DateTime CreatedAt,
    string? CreatedBy,
    IReadOnlyList<SalesOrderLineDto> Lines,
    IReadOnlyList<SalesOrderProductionDto> ProductionOrders,
    int ShippedQuantity = 0,
    int? CustomerBuyerId = null,
    string? CustomerBuyerName = null,
    int? DeliveryAddressId = null,
    string? DeliveryAddress = null,
    IReadOnlyList<SalesOrderApprovalStepDto>? ApprovalSteps = null,
    IReadOnlyList<SalesOrderApprovalEventDto>? ApprovalEvents = null,
    bool CanDecide = false,
    DateTime? ClosedAt = null,
    string? ClosedBy = null,
    string? CloseNote = null,
    bool ClosedAutomatically = false);

/// <summary>Son onaya gönderimin bir adımı.</summary>
public sealed record SalesOrderApprovalStepDto(
    int Sequence, string Name, string Approvers, ApprovalStepStatus Status, DateTime? DecidedAt, string? DecidedBy, string? Note);

/// <summary>Onay geçmişi kaydı: kim, hangi aşamada, ne yaptı, ne not yazdı.</summary>
public sealed record SalesOrderApprovalEventDto(
    int Round, ApprovalEventType EventType, string? StepName, string UserName, string? FullName, DateTime CreatedAt, string? Note);

public sealed record CustomerOrderAddressDto(int Id, string Title, string Text, bool IsDefaultDelivery);

public sealed record CustomerOrderBuyerDto(int Id, string Code, string Name);

public sealed record CustomerOrderSizeDto(int Id, string Code, string Name, int SortOrder);

/// <summary>Sipariş girişinde müşteri seçilince forma gelen bilgiler.</summary>
/// <param name="LastOrderSizes">Müşterinin son siparişindeki bedenler (forma sütun olarak gelir).</param>
public sealed record SalesOrderCustomerDefaultsDto(
    int CustomerId,
    string Currency,
    bool RequireUniqueCustomerOrderNo,
    IReadOnlyList<CustomerOrderAddressDto> Addresses,
    IReadOnlyList<CustomerOrderBuyerDto> Buyers,
    IReadOnlyList<CustomerOrderSizeDto> LastOrderSizes,
    string? LastOrderNo);

/// <summary>Belgede yer alan taraf: ad, vergi bilgisi, adres, iletişim.</summary>
public sealed record ProformaPartyDto(
    string Name, string? TaxOffice, string? TaxNumber, string? Address, string? Phone, string? Email);

/// <summary>Proforma: siparişin kendisi, satıcı (ilk aktif fabrika) ve müşteri bilgileri.</summary>
/// <param name="DocumentLanguage">Belgenin basılacağı dil (müşteri kartından).</param>
public sealed record SalesOrderProformaDto(
    SalesOrderDto Order,
    ProformaPartyDto? Seller,
    ProformaPartyDto Customer,
    string? InvoiceAddress,
    DateTime PrintedAt,
    DocumentLanguage DocumentLanguage = DocumentLanguage.Turkish);

public sealed class SalesOrderListQuery : PageRequest
{
    /// <summary>Sipariş no, müşteri sipariş no veya müşteri adı içinde aranır.</summary>
    public string? Search { get; set; }
    public SalesOrderStatus? Status { get; set; }
    public int? CustomerId { get; set; }

    /// <summary>Yalnızca teslim tarihi geçmiş, kapanmamış siparişler.</summary>
    public bool? OverdueOnly { get; set; }

    /// <summary>Yalnızca oturumdaki kullanıcının kararını bekleyen siparişler.</summary>
    public bool? AwaitingMyApproval { get; set; }
}

public sealed record SalesOrderLineRequest(int ProductId, int ColorId, int SizeId, int Quantity, decimal? UnitPrice = null);

/// <param name="Currency">ISO 4217 para birimi kodu; boşsa TRY.</param>
/// <param name="RowVersion">Güncellemede zorunlu; oluşturmada gönderilmez.</param>
/// <param name="CustomerBuyerId">Siparişi veren buyer; müşterinin buyer'ı olmalıdır.</param>
/// <param name="DeliveryAddressId">Teslim adresi; müşterinin adresi olmalıdır. Boşsa varsayılan teslimat adresi kullanılır.</param>
public sealed record SalesOrderRequest(
    int CustomerId,
    string? CustomerOrderNo,
    DateOnly OrderDate,
    DateOnly DueDate,
    string? Notes,
    IReadOnlyList<SalesOrderLineRequest> Lines,
    string? Currency = null,
    string? RowVersion = null,
    int? CustomerBuyerId = null,
    int? DeliveryAddressId = null);

/// <param name="Note">Onay ya da ret notu (isteğe bağlı).</param>
public sealed record SalesOrderActionRequest(string RowVersion, string? Note = null);

public sealed class SalesOrderRequestValidator : AbstractValidator<SalesOrderRequest>
{
    public const int MaxLines = 2000;
    public const decimal MaxUnitPrice = 100_000_000m;

    public SalesOrderRequestValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0).WithMessage("Müşteri seçilmelidir.");
        RuleFor(x => x.CustomerOrderNo).MaximumLength(50).WithMessage("En fazla 50 karakter olabilir.");
        RuleFor(x => x.Notes).MaximumLength(1000).WithMessage("Not en fazla 1000 karakter olabilir.");
        RuleFor(x => x.OrderDate).NotEqual(default(DateOnly)).WithMessage("Sipariş tarihi zorunludur.");
        RuleFor(x => x.DueDate)
            .NotEqual(default(DateOnly)).WithMessage("Teslim tarihi zorunludur.")
            .GreaterThanOrEqualTo(x => x.OrderDate).WithMessage("Teslim tarihi sipariş tarihinden önce olamaz.");
        RuleFor(x => x.Currency)
            .Matches("^[A-Za-z]{3}$").WithMessage("Para birimi üç harfli kod olmalıdır (örn. TRY, USD, EUR).")
            .When(x => !string.IsNullOrWhiteSpace(x.Currency));

        RuleFor(x => x.Lines).NotNull().WithMessage("Kalem listesi zorunludur.");
        When(x => x.Lines is not null, () =>
        {
            RuleFor(x => x.Lines)
                .Must(lines => lines.Count <= MaxLines).WithMessage($"Bir siparişte en fazla {MaxLines} kalem olabilir.")
                .Must(lines => lines.All(l => l.ProductId > 0 && l.ColorId > 0 && l.SizeId > 0))
                .WithMessage("Her kalemde ürün, renk ve beden seçilmelidir.")
                .Must(lines => lines.All(l => l.Quantity is >= 1 and <= ProductionOrderRequestValidator.MaxItemQuantity))
                .WithMessage($"Kalem adedi 1 ile {ProductionOrderRequestValidator.MaxItemQuantity:N0} arasında olmalıdır.")
                .Must(lines => lines.All(l =>
                    l.UnitPrice is null
                    || (l.UnitPrice >= 0 && l.UnitPrice <= MaxUnitPrice && decimal.Round(l.UnitPrice.Value, 4) == l.UnitPrice)))
                .WithMessage("Birim fiyat negatif olamaz ve en fazla 4 ondalık basamak içerebilir.")
                .Must(lines => lines.Select(l => (l.ProductId, l.ColorId, l.SizeId)).Distinct().Count() == lines.Count)
                .WithMessage("Aynı ürün, renk ve beden birden fazla kez girilemez.");
        });
    }
}

public sealed class SalesOrderActionRequestValidator : AbstractValidator<SalesOrderActionRequest>
{
    public SalesOrderActionRequestValidator()
    {
        RuleFor(x => x.RowVersion).NotEmpty().WithMessage("Sipariş sürümü (rowVersion) zorunludur.");
        RuleFor(x => x.Note).MaximumLength(500).WithMessage("Not en fazla 500 karakter olabilir.");
    }
}

public interface ISalesOrderService
{
    Task<PagedResult<SalesOrderListItemDto>> GetListAsync(SalesOrderListQuery query, CancellationToken cancellationToken);
    Task<Result<SalesOrderDto>> GetAsync(int id, CancellationToken cancellationToken);
    Task<Result<SalesOrderDto>> CreateAsync(SalesOrderRequest request, CancellationToken cancellationToken);
    Task<Result<SalesOrderDto>> UpdateAsync(int id, SalesOrderRequest request, CancellationToken cancellationToken);
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken);

    /// <summary>Taslağı onaya gönderir; içerik kilitlenir.</summary>
    Task<Result<SalesOrderDto>> SubmitAsync(int id, SalesOrderActionRequest request, CancellationToken cancellationToken);

    Task<Result<SalesOrderDto>> ApproveAsync(int id, SalesOrderActionRequest request, CancellationToken cancellationToken);

    /// <summary>Onaya gönderilmiş siparişi düzeltilmek üzere taslağa geri çevirir.</summary>
    Task<Result<SalesOrderDto>> RejectAsync(int id, SalesOrderActionRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Onaylı siparişi revizyon için taslağa alır. Geçerli üretim siparişi ya da sevkiyatı olan sipariş revize
    /// edilemez (önce onlar iptal edilmelidir).
    /// </summary>
    Task<Result<SalesOrderDto>> ReviseAsync(int id, SalesOrderActionRequest request, CancellationToken cancellationToken);

    Task<Result<SalesOrderDto>> CancelAsync(int id, SalesOrderActionRequest request, CancellationToken cancellationToken);
    Task<Result<SalesOrderDto>> CloseAsync(int id, SalesOrderActionRequest request, CancellationToken cancellationToken);

    /// <summary>Müşterinin varsayılan para birimi, teslim adresleri, aktif buyer'ları ve son siparişinin bedenleri.</summary>
    Task<Result<SalesOrderCustomerDefaultsDto>> GetCustomerDefaultsAsync(int customerId, CancellationToken cancellationToken);

    /// <summary>Proforma (sipariş teyidi) belgesi için sipariş, satıcı ve müşteri bilgileri.</summary>
    Task<Result<SalesOrderProformaDto>> GetProformaAsync(int id, CancellationToken cancellationToken);

}

/// <summary>
/// Müşteri siparişleri. Durum kuralları <see cref="SalesOrder"/> içindedir; onay ve ret ayrı bir yetkiyle yapılır
/// (API katmanında denetlenir). Üretim, siparişten açılan üretim siparişleri üzerinden yürür.
/// </summary>
public sealed class SalesOrderService(
    IKonfeksiyonDbContext db,
    IDocumentNumberGenerator numberGenerator,
    ICurrentUser currentUser,
    IPermissionService permissions,
    IExchangeRateService exchangeRates,
    ISalesOrderReservationService reservations,
    IPricingService pricing,
    TimeProvider timeProvider,
    IOptions<SalesOrderOptions> options) : ISalesOrderService
{
    private static readonly Error CannotSeePrices = new(
        ErrorCodes.Forbidden,
        "Müşteri siparişini düzenlemek için fiyatları görme yetkisi gerekir (Satış fiyatlarını görme).",
        ErrorType.Forbidden);

    private async Task<bool> HasPermissionAsync(string permission, CancellationToken cancellationToken) =>
        (await permissions.GetPermissionsAsync(currentUser.Roles, cancellationToken)).Contains(permission);

    private const string DefaultCurrency = "TRY";
    private const string SystemUser = "system";

    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Müşteri siparişi bulunamadı.", ErrorType.NotFound);

    private static readonly Error ConcurrencyConflict = new(
        ErrorCodes.ConcurrencyConflict,
        "Sipariş siz açtıktan sonra başka bir kullanıcı tarafından değiştirildi. Sayfayı yenileyip tekrar deneyin.",
        ErrorType.Conflict);

    private static readonly IReadOnlyDictionary<SalesOrderStatus, string> StatusLabels =
        new Dictionary<SalesOrderStatus, string>
        {
            [SalesOrderStatus.Draft] = "Taslak",
            [SalesOrderStatus.Submitted] = "Onay bekliyor",
            [SalesOrderStatus.Approved] = "Onaylı",
            [SalesOrderStatus.Closed] = "Kapatıldı",
            [SalesOrderStatus.Cancelled] = "İptal",
        };

    /// <summary>Kapanmamış (teslimi beklenen) sipariş durumları.</summary>
    private static readonly SalesOrderStatus[] OpenStatuses =
        [SalesOrderStatus.Draft, SalesOrderStatus.Submitted, SalesOrderStatus.Approved];

    private readonly SalesOrderOptions _options = options.Value;

    public async Task<PagedResult<SalesOrderListItemDto>> GetListAsync(SalesOrderListQuery query, CancellationToken cancellationToken)
    {
        var orders = db.SalesOrders.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            orders = orders.Where(o =>
                o.OrderNo.Contains(search) || o.CustomerOrderNo!.Contains(search) || o.Customer.Name.Contains(search));
        }

        if (query.Status is { } status)
        {
            orders = orders.Where(o => o.Status == status);
        }

        if (query.CustomerId is { } customerId)
        {
            orders = orders.Where(o => o.CustomerId == customerId);
        }

        if (query.AwaitingMyApproval == true)
        {
            orders = currentUser.UserId is { } userId
                ? orders.AwaitingDecisionOf(db, userId, await CurrentRoleIdsAsync(cancellationToken),
                    (await permissions.GetPermissionsAsync(currentUser.Roles, cancellationToken)).Contains(ERP.Konfeksiyon.Domain.Authorization.KonfeksiyonPermissions.SalesOrder.Approve))
                : orders.Where(_ => false);
        }

        var today = Today();
        if (query.OverdueOnly == true)
        {
            orders = orders.Where(o => o.DueDate < today && OpenStatuses.Contains(o.Status));
        }

        // Sevk edilen adet sayfalı listede sunucuda hesaplanır (istemci yalnızca bir sayfayı görür).
        var page = await orders
            .OrderByDescending(o => o.Id)
            .Select(o => new
            {
                Order = o,
                Total = o.Lines.Sum(l => (int?)l.Quantity) ?? 0,
                Shipped = (int)(db.ShipmentItems
                    .Where(i => i.Shipment.SalesOrderId == o.Id && i.Shipment.Status == ShipmentStatus.Shipped)
                    .Sum(i => (decimal?)i.Quantity) ?? 0),
            })
            .Select(x => new SalesOrderListItemDto(
                x.Order.Id,
                x.Order.OrderNo,
                x.Order.CustomerOrderNo,
                x.Order.CustomerId,
                x.Order.Customer.Name,
                x.Order.OrderDate,
                x.Order.DueDate,
                x.Order.Status,
                x.Order.Lines.Select(l => l.ProductId).Distinct().Count(),
                x.Total,
                x.Order.Lines.Sum(l => l.Quantity * l.UnitPrice),
                x.Order.Currency,
                x.Shipped))
            .ToPagedResultAsync(query, cancellationToken);

        // Kalan adet kalem bazında: bir kalemdeki fazla sevk, başka kalemin eksiğini kapatmaz.
        var remaining = await RemainingByOrderAsync(page.Items.Select(o => o.Id).ToList(), cancellationToken);
        page = page with
        {
            Items = page.Items
                .Select(o =>
                {
                    var left = remaining.GetValueOrDefault(o.Id);
                    return o with
                    {
                        RemainingQuantity = left,
                        IsOverdue = o.DueDate < today && OpenStatuses.Contains(o.Status) && left > 0,
                    };
                })
                .ToList(),
        };

        return await HasPermissionAsync(ERP.Konfeksiyon.Domain.Authorization.KonfeksiyonPermissions.SalesOrder.ViewPrices, cancellationToken)
            ? page
            : page with { Items = page.Items.Select(o => o with { TotalAmount = null }).ToList() };
    }

    public async Task<Result<SalesOrderProformaDto>> GetProformaAsync(int id, CancellationToken cancellationToken)
    {
        var order = await GetAsync(id, cancellationToken);
        if (!order.IsSuccess)
        {
            return order.Error!;
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
        var customer = await db.Customers.AsNoTracking().IgnoreQueryFilters()
            .Where(c => c.Id == order.Value.CustomerId)
            .Select(c => new ProformaPartyDto(c.Name, c.TaxOffice, c.TaxNumber, null, c.Phone, c.Email))
            .SingleAsync(cancellationToken);
        var invoice = await db.CustomerAddresses.AsNoTracking()
            .Where(a => a.CustomerId == order.Value.CustomerId && a.IsDefaultInvoice)
            .Select(a => new { a.AddressLine, a.District, a.City })
            .FirstOrDefaultAsync(cancellationToken);
        var invoiceAddress = invoice is null ? null : AddressText(invoice.AddressLine, invoice.District, invoice.City);
        var language = await db.Customers.AsNoTracking().IgnoreQueryFilters()
            .Where(c => c.Id == order.Value.CustomerId)
            .Select(c => c.DocumentLanguage)
            .SingleAsync(cancellationToken);

        return new SalesOrderProformaDto(
            order.Value, seller, customer, invoiceAddress, timeProvider.GetUtcNow().UtcDateTime, language);
    }

    public async Task<Result<SalesOrderCustomerDefaultsDto>> GetCustomerDefaultsAsync(
        int customerId, CancellationToken cancellationToken)
    {
        var customer = await db.Customers.AsNoTracking()
            .Where(c => c.Id == customerId)
            .Select(c => new { c.Currency, c.RequireUniqueCustomerOrderNo })
            .SingleOrDefaultAsync(cancellationToken);
        if (customer is null)
        {
            return new Error(ErrorCodes.NotFound, "Müşteri bulunamadı.", ErrorType.NotFound);
        }

        var addresses = await db.CustomerAddresses.AsNoTracking()
            .Where(a => a.CustomerId == customerId)
            .OrderByDescending(a => a.IsDefaultDelivery).ThenBy(a => a.Title)
            .Select(a => new { a.Id, a.Title, a.AddressLine, a.District, a.City, a.IsDefaultDelivery })
            .ToListAsync(cancellationToken);
        var addressDtos = addresses
            .Select(a => new CustomerOrderAddressDto(a.Id, a.Title, AddressText(a.AddressLine, a.District, a.City) ?? string.Empty, a.IsDefaultDelivery))
            .ToList();
        var buyers = await db.CustomerBuyers.AsNoTracking()
            .Where(b => b.CustomerId == customerId && b.IsActive)
            .OrderBy(b => b.Code)
            .Select(b => new CustomerOrderBuyerDto(b.Id, b.Code, b.Name))
            .ToListAsync(cancellationToken);

        // Son sipariş: iptal edilmemiş, en yeni.
        var last = await db.SalesOrders.AsNoTracking()
            .Where(o => o.CustomerId == customerId && o.Status != SalesOrderStatus.Cancelled)
            .OrderByDescending(o => o.OrderDate).ThenByDescending(o => o.Id)
            .Select(o => new { o.Id, o.OrderNo })
            .FirstOrDefaultAsync(cancellationToken);
        var sizes = last is null
            ? []
            : (await db.SalesOrderLines.AsNoTracking().IgnoreQueryFilters()
                .Where(l => l.SalesOrderId == last.Id && !l.Size.IsDeleted && l.Size.IsActive)
                .Select(l => new { l.SizeId, l.Size.SizeCode, l.Size.Name, l.Size.SortOrder })
                .Distinct()
                .ToListAsync(cancellationToken))
                .OrderBy(s => s.SortOrder).ThenBy(s => s.SizeCode, StringComparer.Ordinal)
                .Select(s => new CustomerOrderSizeDto(s.SizeId, s.SizeCode, s.Name, s.SortOrder))
                .ToList();

        return new SalesOrderCustomerDefaultsDto(
            customerId, customer.Currency, customer.RequireUniqueCustomerOrderNo, addressDtos, buyers, sizes, last?.OrderNo);
    }

    public async Task<Result<SalesOrderDto>> GetAsync(int id, CancellationToken cancellationToken)
    {
        var order = await LoadAsync(id, cancellationToken);
        if (!order.IsSuccess || await HasPermissionAsync(ERP.Konfeksiyon.Domain.Authorization.KonfeksiyonPermissions.SalesOrder.ViewPrices, cancellationToken))
        {
            return order;
        }

        // Fiyat görme yetkisi yoksa adet ve sevk durumu görünür; fiyat ve tutarlar boş döner.
        return order.Value with
        {
            TotalAmount = null,
            Lines = order.Value.Lines.Select(l => l with { UnitPrice = null, LineAmount = null }).ToList(),
        };
    }

    private async Task<Result<SalesOrderDto>> LoadAsync(int id, CancellationToken cancellationToken)
    {
        // Sonradan silinen müşteri / ürün / renk / beden de adıyla görünür; sipariş kendisi silinmemiş olmalıdır.
        var order = await db.SalesOrders
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(o => o.Customer)
            .Include(o => o.CustomerBuyer)
            .Include(o => o.DeliveryAddress)
            .Include(o => o.Lines).ThenInclude(l => l.Product)
            .Include(o => o.Lines).ThenInclude(l => l.Color)
            .Include(o => o.Lines).ThenInclude(l => l.Size)
            .SingleOrDefaultAsync(o => o.Id == id && !o.IsDeleted, cancellationToken);
        if (order is null)
        {
            return NotFound;
        }

        var production = await db.ProductionOrders
            .AsNoTracking()
            .Where(p => p.SalesOrderId == id)
            .OrderBy(p => p.Id)
            .Select(p => new SalesOrderProductionDto(
                p.Id, p.OrderNo, p.ProductId, p.Product.ProductCode, p.Status, p.Items.Sum(i => (int?)i.Quantity) ?? 0))
            .ToListAsync(cancellationToken);

        var shipped = await ShippedAsync(id, cancellationToken);

        var coverage = await SalesOrderCoverage.LoadAsync(db, [id], cancellationToken);
        var lines = order.Lines
            .OrderBy(l => l.Product.ProductCode, StringComparer.Ordinal)
            .ThenBy(l => l.Color.ColorCode, StringComparer.Ordinal)
            .ThenBy(l => l.Size.SortOrder).ThenBy(l => l.Size.SizeCode, StringComparer.Ordinal)
            .Select(l => new SalesOrderLineDto(
                l.Id, l.ProductId, l.Product.ProductCode, l.Product.Name, l.ColorId, l.Color.ColorCode, l.Color.Name, l.SizeId,
                l.Size.SizeCode, l.Size.Name, l.Size.SortOrder, l.Quantity, l.UnitPrice,
                l.UnitPrice is null ? null : decimal.Round(l.Quantity * l.UnitPrice.Value, 2),
                (int)shipped.GetValueOrDefault((l.ProductId, l.ColorId, l.SizeId)),
                l.Product.CustomerArticleCode,
                l.Product.VatRate,
                coverage.GetValueOrDefault((id, l.ProductId, l.ColorId, l.SizeId)).Reserved,
                coverage.GetValueOrDefault((id, l.ProductId, l.ColorId, l.SizeId)).InProduction,
                SalesOrderCoverage.ToPlan(
                    l.Quantity, coverage.GetValueOrDefault((id, l.ProductId, l.ColorId, l.SizeId)).Reserved, l.ShippedFromReservation,
                    coverage.GetValueOrDefault((id, l.ProductId, l.ColorId, l.SizeId)).InProduction)))
            .ToList();
        production = production
            .Select(p => p with { OrderedQuantity = lines.Where(l => l.ProductId == p.ProductId).Sum(l => l.Quantity) })
            .ToList();

        var steps = await db.SalesOrderApprovalSteps.AsNoTracking()
            .Where(s => s.SalesOrderId == id && s.Round == order.ApprovalRound)
            .OrderBy(s => s.Sequence)
            .Select(s => new SalesOrderApprovalStepDto(s.Sequence, s.Name, s.ApproverNames, s.Status, s.DecidedAt, s.DecidedBy, s.Note))
            .ToListAsync(cancellationToken);
        var events = await db.SalesOrderApprovalEvents.AsNoTracking()
            .Where(e => e.SalesOrderId == id)
            .OrderBy(e => e.CreatedAt).ThenBy(e => e.Id)
            .Select(e => new SalesOrderApprovalEventDto(e.Round, e.EventType, e.StepName, e.UserName, e.FullName, e.CreatedAt, e.Note))
            .ToListAsync(cancellationToken);
        var canDecide = order.Status == SalesOrderStatus.Submitted
            && (await PendingStepForCurrentUserAsync(id, cancellationToken)).Error is null;

        return new SalesOrderDto(
            order.Id,
            order.OrderNo,
            order.CustomerOrderNo,
            order.CustomerId,
            order.Customer.CustomerCode,
            order.Customer.Name,
            order.OrderDate,
            order.DueDate,
            order.Currency,
            order.Status,
            order.Notes,
            lines.Sum(l => l.Quantity),
            lines.Any(l => l.LineAmount is not null) ? lines.Sum(l => l.LineAmount ?? 0) : null,
            order.IsEditable,
            order.CanGenerateProduction && order.Status == SalesOrderStatus.Approved && lines.Any(l => l.ToPlanQuantity > 0),
            order.AllowedTransitions,
            order.SubmittedAt,
            order.SubmittedBy,
            order.DecidedAt,
            order.DecidedBy,
            order.DecisionNote,
            RowVersionToken.From(order.RowVersion),
            order.CreatedAt,
            order.CreatedBy,
            lines,
            production,
            lines.Sum(l => l.ShippedQuantity),
            order.CustomerBuyerId,
            order.CustomerBuyer?.Name,
            order.DeliveryAddressId,
            order.DeliveryAddress is { } address
                ? $"{address.Title}: {AddressText(address.AddressLine, address.District, address.City)}"
                : null,
            steps,
            events,
            canDecide,
            order.ClosedAt,
            order.ClosedBy,
            order.CloseNote,
            order.ClosedAutomatically);
    }

    public async Task<Result<SalesOrderDto>> CreateAsync(SalesOrderRequest request, CancellationToken cancellationToken)
    {
        if (await ValidateAsync(request, null, cancellationToken) is { } error)
        {
            return error;
        }

        var order = new SalesOrder();
        Apply(request, order);
        order.DeliveryAddressId = await DeliveryAddressOfAsync(request, cancellationToken);
        foreach (var line in request.Lines)
        {
            order.Lines.Add(new SalesOrderLine
            {
                ProductId = line.ProductId, ColorId = line.ColorId, SizeId = line.SizeId, Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
            });
        }

        // Numara ve sipariş aynı transaction'da: kayıt başarısız olursa numara da geri alınır.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var year = timeProvider.GetLocalNow().Year;
        var sequence = await numberGenerator.NextAsync(DocumentSeries.SalesOrder, year, cancellationToken);
        order.OrderNo = $"{_options.NumberPrefix}-{year}-{sequence.ToString().PadLeft(_options.NumberDigits, '0')}";

        db.SalesOrders.Add(order);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await GetAsync(order.Id, cancellationToken);
    }

    public async Task<Result<SalesOrderDto>> UpdateAsync(int id, SalesOrderRequest request, CancellationToken cancellationToken)
    {
        var order = await db.SalesOrders.Include(o => o.Lines).SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
        {
            return NotFound;
        }

        if (RowVersionToken.Check(order.RowVersion, request.RowVersion, ConcurrencyConflict) is { } versionError)
        {
            return versionError;
        }

        if (!order.IsEditable)
        {
            return new Error(
                ErrorCodes.OrderNotEditable,
                $"{StatusLabels[order.Status]} durumundaki sipariş değiştirilemez; yalnızca taslak düzenlenebilir.");
        }

        if (await ValidateAsync(request, order, cancellationToken) is { } error)
        {
            return error;
        }

        Apply(request, order);
        order.DeliveryAddressId = await DeliveryAddressOfAsync(request, cancellationToken);
        SyncLines(order, request.Lines);
        return await SaveAndReloadAsync(order, cancellationToken);
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var order = await db.SalesOrders.SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
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
        db.SalesOrders.Remove(order);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    /// <summary>
    /// Taslağı onaya gönderir. Satış siparişi onay akışı tanımlı ve aktifse akışın adımları (tutar eşiğine göre
    /// süzülerek) bu gönderime kopyalanır; ilk adım kararı bekler.
    /// </summary>
    public async Task<Result<SalesOrderDto>> SubmitAsync(int id, SalesOrderActionRequest request, CancellationToken cancellationToken)
    {
        var steps = await PlanApprovalStepsAsync(id, cancellationToken);
        var actor = await ActorAsync(cancellationToken);
        return await ChangeAsync(id, request, SalesOrderStatus.Submitted, fromSubmitted: false, (order, now, user) =>
        {
            if (order.Lines.Count == 0)
            {
                return new Error(ErrorCodes.OrderHasNoItems, "Kalemi olmayan sipariş onaya gönderilemez.");
            }

            order.Submit(now, user);
            for (var i = 0; i < steps.Count; i++)
            {
                steps[i].SalesOrderId = order.Id;
                steps[i].Round = order.ApprovalRound;
                steps[i].Status = i == 0 ? ApprovalStepStatus.Pending : ApprovalStepStatus.Waiting;
                db.SalesOrderApprovalSteps.Add(steps[i]);
            }

            AddEvent(order, ApprovalEventType.Submitted, null, actor, now, request.Note);
            return null;
        }, cancellationToken);
    }

    /// <summary>
    /// Kararı bekleyen adımı onaylar; son adımsa sipariş onaylanır, değilse sıradaki adıma geçer. Akışsız gönderilmiş
    /// siparişi müşteri siparişi onaylama yetkisi olan herkes onaylar.
    /// </summary>
    public async Task<Result<SalesOrderDto>> ApproveAsync(int id, SalesOrderActionRequest request, CancellationToken cancellationToken)
    {
        var (pending, error) = await PendingStepForCurrentUserAsync(id, cancellationToken);
        if (error is not null)
        {
            return error;
        }

        var actor = await ActorAsync(cancellationToken);
        var approved = await ChangeAsync(id, request, SalesOrderStatus.Approved, fromSubmitted: true, (order, now, user) =>
        {
            if (pending is null)
            {
                order.Approve(now, user, request.Note);
                AddEvent(order, ApprovalEventType.Approved, null, actor, now, request.Note);
                return null;
            }

            Decide(pending, ApprovalStepStatus.Approved, now, user, request.Note);
            AddEvent(order, ApprovalEventType.Approved, pending.Name, actor, now, request.Note);
            var next = db.SalesOrderApprovalSteps.Local
                .Where(s => s.SalesOrderId == order.Id && s.Round == order.ApprovalRound && s.Status == ApprovalStepStatus.Waiting)
                .OrderBy(s => s.Sequence)
                .FirstOrDefault();
            if (next is not null)
            {
                next.Status = ApprovalStepStatus.Pending;
            }
            else
            {
                order.Approve(now, user, request.Note);
            }

            return null;
        }, cancellationToken);

        // Son onayla sipariş onaylandıysa depodaki hazır mamul siparişe ayrılır; eksik kısım planlamaya düşer.
        if (approved.IsSuccess && approved.Value.Status == SalesOrderStatus.Approved)
        {
            await reservations.ReserveRemainingAsync(id, cancellationToken);
            return await GetAsync(id, cancellationToken);
        }

        return approved;
    }

    /// <summary>Kararı bekleyen adımda siparişi geri çevirir: sipariş taslağa döner, kalan adımlar kapanır.</summary>
    public async Task<Result<SalesOrderDto>> RejectAsync(int id, SalesOrderActionRequest request, CancellationToken cancellationToken)
    {
        var (pending, error) = await PendingStepForCurrentUserAsync(id, cancellationToken);
        if (error is not null)
        {
            return error;
        }

        var actor = await ActorAsync(cancellationToken);
        return await ChangeAsync(id, request, SalesOrderStatus.Draft, fromSubmitted: true, (order, now, user) =>
        {
            order.Reject(now, user, request.Note);
            if (pending is not null)
            {
                Decide(pending, ApprovalStepStatus.Rejected, now, user, request.Note);
                CloseOpenSteps(order);
            }

            AddEvent(order, ApprovalEventType.Rejected, pending?.Name, actor, now, request.Note);
            return null;
        }, cancellationToken);
    }

    public async Task<Result<SalesOrderDto>> ReviseAsync(int id, SalesOrderActionRequest request, CancellationToken cancellationToken)
    {
        if (await OpenProductionAsync(id, includeCompleted: true, cancellationToken) is { Count: > 0 } active)
        {
            return new Error(
                ErrorCodes.SalesOrderHasProduction,
                $"Bu siparişten açılmış iş emirleri var ({string.Join(", ", active)}). Revizyon için önce onlar "
                + "silinmeli ya da iptal edilmelidir.");
        }

        if (await ShipmentsAsync(id, onlyShipped: false, cancellationToken) is { Count: > 0 } shipments)
        {
            return new Error(
                ErrorCodes.SalesOrderHasShipments,
                $"Bu siparişe bağlı sevkiyatlar var ({string.Join(", ", shipments)}); sipariş revizyona alınamaz.");
        }

        var order = await db.SalesOrders.AsNoTracking().SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is not null && order.Status != SalesOrderStatus.Approved)
        {
            return new Error(
                ErrorCodes.InvalidStatusTransition,
                $"{StatusLabels[order.Status]} durumundaki sipariş revizyona alınamaz; yalnızca onaylı sipariş revize edilir.");
        }

        var actor = await ActorAsync(cancellationToken);
        var changed = await ChangeAsync(id, request, SalesOrderStatus.Draft, fromSubmitted: false, (changing, now, user) =>
        {
            changing.Revise(now, user, request.Note);
            AddEvent(changing, ApprovalEventType.Revised, null, actor, now, request.Note);
            return null;
        }, cancellationToken);
        if (changed.IsSuccess)
        {
            // Ayırma bırakılır: stok başka siparişlerce kullanılabilir.
            await reservations.ReleaseAsync(id, cancellationToken);
            return await GetAsync(id, cancellationToken);
        }

        return changed;
    }

    public async Task<Result<SalesOrderDto>> CancelAsync(int id, SalesOrderActionRequest request, CancellationToken cancellationToken)
    {
        if (await ShipmentsAsync(id, onlyShipped: true, cancellationToken) is { Count: > 0 } shipped)
        {
            return new Error(
                ErrorCodes.SalesOrderHasShipments,
                $"Bu siparişten sevkiyat yapılmış ({string.Join(", ", shipped)}); sipariş iptal edilemez, kapatılabilir.");
        }

        if (await OpenProductionAsync(id, includeCompleted: true, cancellationToken) is { Count: > 0 } active)
        {
            return new Error(
                ErrorCodes.SalesOrderHasProduction,
                $"Bu siparişten açılmış iş emirleri var ({string.Join(", ", active)}). Önce onlar iptal edilmelidir.");
        }

        await LoadRoundStepsAsync(id, cancellationToken);
        var actor = await ActorAsync(cancellationToken);
        var changed = await ChangeAsync(id, request, SalesOrderStatus.Cancelled, fromSubmitted: false, (order, now, _) =>
        {
            var wasSubmitted = order.Status == SalesOrderStatus.Submitted;
            order.Cancel();
            CloseOpenSteps(order);
            if (wasSubmitted)
            {
                AddEvent(order, ApprovalEventType.Cancelled, null, actor, now, request.Note);
            }
            return null;
        }, cancellationToken);
        if (changed.IsSuccess)
        {
            // Ayırma bırakılır: stok başka siparişlerce kullanılabilir.
            await reservations.ReleaseAsync(id, cancellationToken);
            return await GetAsync(id, cancellationToken);
        }

        return changed;
    }

    public async Task<Result<SalesOrderDto>> CloseAsync(int id, SalesOrderActionRequest request, CancellationToken cancellationToken)
    {
        if (await OpenProductionAsync(id, includeCompleted: false, cancellationToken) is { Count: > 0 } open)
        {
            return new Error(
                ErrorCodes.SalesOrderHasProduction,
                $"Üretimi süren siparişler var ({string.Join(", ", open)}). Sipariş, iş emirleri tamamlanınca kapatılabilir.");
        }

        // Kapatma "teslim edildi" demektir: hiç üretimi tamamlanmamış ve hiç sevk edilmemiş sipariş iptal edilir.
        var completed = await db.ProductionOrders
            .AnyAsync(p => p.SalesOrderId == id && p.Status == ProductionOrderStatus.Completed, cancellationToken);
        if (!completed && (await ShipmentsAsync(id, onlyShipped: true, cancellationToken)).Count == 0)
        {
            return new Error(
                ErrorCodes.SalesOrderNothingDelivered,
                "Siparişin tamamlanmış üretimi ya da sevkiyatı yok; kapatılamaz. Sipariş karşılanmayacaksa iptal edin.");
        }

        var changed = await ChangeAsync(id, request, SalesOrderStatus.Closed, fromSubmitted: false, (order, now, user) =>
        {
            order.Close(now, user, request.Note, automatic: false);
            return null;
        }, cancellationToken);
        if (changed.IsSuccess)
        {
            // Ayırma bırakılır: stok başka siparişlerce kullanılabilir.
            await reservations.ReleaseAsync(id, cancellationToken);
            return await GetAsync(id, cancellationToken);
        }

        return changed;
    }

    /// <summary>Bu müşteri siparişinden açılmış, hâlâ geçerli üretim siparişlerinin numaraları.</summary>
    private async Task<List<string>> OpenProductionAsync(int id, bool includeCompleted, CancellationToken cancellationToken) =>
        await db.ProductionOrders
            .AsNoTracking()
            .Where(p => p.SalesOrderId == id && p.Status != ProductionOrderStatus.Cancelled
                && (includeCompleted || p.Status != ProductionOrderStatus.Completed))
            .OrderBy(p => p.Id)
            .Select(p => p.OrderNo)
            .ToListAsync(cancellationToken);

    /// <summary>Bu müşteri siparişine bağlı (iptal edilmemiş) sevkiyatların numaraları.</summary>
    private async Task<List<string>> ShipmentsAsync(int id, bool onlyShipped, CancellationToken cancellationToken) =>
        await db.Shipments
            .AsNoTracking()
            .Where(s => s.SalesOrderId == id
                && (onlyShipped ? s.Status == ShipmentStatus.Shipped : s.Status != ShipmentStatus.Cancelled))
            .OrderBy(s => s.Id)
            .Select(s => s.ShipmentNo)
            .ToListAsync(cancellationToken);

    /// <summary>Siparişe bağlı sevk edilmiş sevkiyatların ürün / renk / beden bazında toplam adetleri.</summary>
    private async Task<Dictionary<(int ProductId, int ColorId, int SizeId), decimal>> ShippedAsync(
        int id, CancellationToken cancellationToken) =>
        (await db.ShipmentItems
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(i => i.Shipment.SalesOrderId == id && i.Shipment.Status == ShipmentStatus.Shipped && !i.Shipment.IsDeleted)
            .GroupBy(i => new { i.StockItem.ProductId, i.StockItem.ColorId, i.StockItem.SizeId })
            .Select(g => new { g.Key.ProductId, g.Key.ColorId, g.Key.SizeId, Quantity = g.Sum(i => i.Quantity) })
            .ToListAsync(cancellationToken))
        .Where(g => g.ProductId is not null && g.ColorId is not null && g.SizeId is not null)
        .ToDictionary(g => (g.ProductId!.Value, g.ColorId!.Value, g.SizeId!.Value), g => g.Quantity);

    /// <summary>
    /// Siparişlerin sevk edilmeyi bekleyen adedi: kalem başına (ürün / renk / beden) sipariş − sevk, eksiye düşmeden
    /// toplanır. Sevk, <see cref="ShippedAsync"/> ile aynı kuralla (sevk edilmiş sevkiyatlar) kaleme eşlenir.
    /// </summary>
    private async Task<Dictionary<int, int>> RemainingByOrderAsync(List<int> orderIds, CancellationToken cancellationToken)
    {
        if (orderIds.Count == 0)
        {
            return new Dictionary<int, int>();
        }

        var lines = await db.SalesOrderLines
            .AsNoTracking()
            .Where(l => orderIds.Contains(l.SalesOrderId))
            .Select(l => new { l.SalesOrderId, l.ProductId, l.ColorId, l.SizeId, l.Quantity })
            .ToListAsync(cancellationToken);

        var shipped = (await db.ShipmentItems
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(i => i.Shipment.SalesOrderId != null && orderIds.Contains(i.Shipment.SalesOrderId.Value)
                    && i.Shipment.Status == ShipmentStatus.Shipped && !i.Shipment.IsDeleted)
                .GroupBy(i => new { i.Shipment.SalesOrderId, i.StockItem.ProductId, i.StockItem.ColorId, i.StockItem.SizeId })
                .Select(g => new
                {
                    g.Key.SalesOrderId, g.Key.ProductId, g.Key.ColorId, g.Key.SizeId, Quantity = g.Sum(i => i.Quantity),
                })
                .ToListAsync(cancellationToken))
            .ToDictionary(g => (g.SalesOrderId!.Value, g.ProductId, g.ColorId, g.SizeId), g => g.Quantity);

        return lines
            .GroupBy(l => l.SalesOrderId)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(l =>
                {
                    var sent = shipped.GetValueOrDefault((l.SalesOrderId, (int?)l.ProductId, (int?)l.ColorId, (int?)l.SizeId));
                    return sent >= l.Quantity ? 0 : l.Quantity - (int)sent;
                }));
    }

    /// <summary>
    /// Gönderimde uygulanacak onay adımları: aktif akışın adımlarından, tutar eşiği sipariş tutarının (yerel para
    /// birimine çevrilmiş) üstünde kalanlar çıkarılır. Akış yoksa ya da kapalıysa liste boştur (akışsız onay).
    /// </summary>
    private async Task<List<SalesOrderApprovalStep>> PlanApprovalStepsAsync(int id, CancellationToken cancellationToken)
    {
        var flow = await db.ApprovalFlows.AsNoTracking()
            .Include(f => f.Steps).ThenInclude(s => s.Role)
            .Include(f => f.Steps).ThenInclude(s => s.Users).ThenInclude(u => u.User)
            .SingleOrDefaultAsync(f => f.DocumentType == ApprovalDocumentType.SalesOrder && f.IsActive, cancellationToken);
        if (flow is null || flow.Steps.Count == 0)
        {
            return [];
        }

        decimal? amount = null;
        if (flow.Steps.Any(s => s.MinAmount is not null))
        {
            var order = await db.SalesOrders.AsNoTracking()
                .Where(o => o.Id == id)
                .Select(o => new { o.Currency, o.OrderDate, Total = o.Lines.Sum(l => l.Quantity * l.UnitPrice) })
                .SingleOrDefaultAsync(cancellationToken);
            if (order?.Total is { } total)
            {
                amount = await exchangeRates.ConvertAsync(
                    total, order.Currency, Currency.LocalCode, order.OrderDate, ExchangeRateType.ForexBuying, cancellationToken);
            }
        }

        // Tutar bilinmiyorsa (fiyatsız sipariş ya da kur yok) eşikli adımlar da uygulanır: onay atlanmaz.
        return flow.Steps
            .Where(s => s.MinAmount is null || amount is null || amount >= s.MinAmount)
            .OrderBy(s => s.Sequence)
            .Select((s, index) => new SalesOrderApprovalStep
            {
                Sequence = index + 1,
                Name = s.Name,
                ApproverType = s.ApproverType,
                RoleId = s.RoleId,
                ApproverUserIds = s.ApproverType == ApproverType.Users
                    ? "," + string.Join(",", s.Users.Select(u => u.UserId).Order()) + ","
                    : null,
                ApproverNames = s.ApproverType == ApproverType.Role
                    ? s.Role?.Name ?? "—"
                    : string.Join(", ", s.Users.Select(u => u.User.FullName).Order(StringComparer.CurrentCulture)),
            })
            .ToList();
    }

    /// <summary>Siparişin son gönderimine ait adımlar (izlenir; işlem içinde değiştirilir).</summary>
    private async Task<List<SalesOrderApprovalStep>> LoadRoundStepsAsync(int id, CancellationToken cancellationToken)
    {
        var round = await db.SalesOrders.Where(o => o.Id == id).Select(o => (int?)o.ApprovalRound).SingleOrDefaultAsync(cancellationToken);
        return round is null
            ? []
            : await db.SalesOrderApprovalSteps.Where(s => s.SalesOrderId == id && s.Round == round).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Kararı bekleyen adım ve oturumdaki kullanıcının o adımda karar verip veremeyeceği. Adımsız (akışsız) gönderimde
    /// adım boştur ve müşteri siparişi onaylama yetkisi aranır.
    /// </summary>
    private async Task<(SalesOrderApprovalStep? Pending, Error? Error)> PendingStepForCurrentUserAsync(
        int id, CancellationToken cancellationToken)
    {
        var steps = await LoadRoundStepsAsync(id, cancellationToken);
        var pending = steps.FirstOrDefault(s => s.Status == ApprovalStepStatus.Pending);
        if (pending is null)
        {
            if (steps.Count == 0 && !(await permissions.GetPermissionsAsync(currentUser.Roles, cancellationToken))
                .Contains(ERP.Konfeksiyon.Domain.Authorization.KonfeksiyonPermissions.SalesOrder.Approve))
            {
                return (null, NotYourApproval("Bu siparişi onaylama yetkiniz yok."));
            }

            return (null, null);
        }

        var roleIds = await CurrentRoleIdsAsync(cancellationToken);
        return currentUser.UserId is { } userId && pending.CanBeDecidedBy(userId, roleIds)
            ? (pending, null)
            : (null, NotYourApproval($"Sipariş \"{pending.Name}\" adımında {pending.ApproverNames} onayını bekliyor."));
    }

    private static Error NotYourApproval(string message) => new(ErrorCodes.NotYourApproval, message, ErrorType.Forbidden);

    private async Task<List<int>> CurrentRoleIdsAsync(CancellationToken cancellationToken) =>
        currentUser.UserId is { } userId
            ? await db.UserRoles.Where(r => r.UserId == userId).Select(r => r.RoleId).ToListAsync(cancellationToken)
            : [];

    private async Task<(string UserName, string? FullName)> ActorAsync(CancellationToken cancellationToken)
    {
        var fullName = currentUser.UserId is { } userId
            ? await db.Users.Where(u => u.Id == userId).Select(u => u.FullName).SingleOrDefaultAsync(cancellationToken)
            : null;
        return (currentUser.UserName ?? SystemUser, fullName);
    }

    private void AddEvent(
        SalesOrder order, ApprovalEventType type, string? stepName, (string UserName, string? FullName) actor, DateTime now, string? note) =>
        db.SalesOrderApprovalEvents.Add(new SalesOrderApprovalEvent
        {
            SalesOrderId = order.Id,
            Round = order.ApprovalRound,
            EventType = type,
            StepName = stepName,
            UserName = actor.UserName,
            FullName = actor.FullName,
            CreatedAt = now,
            Note = NullIfBlank(note),
        });

    private static void Decide(SalesOrderApprovalStep step, ApprovalStepStatus status, DateTime now, string user, string? note)
    {
        step.Status = status;
        step.DecidedAt = now;
        step.DecidedBy = user;
        step.Note = NullIfBlank(note);
    }

    /// <summary>Turun karar verilmemiş adımlarını kapatır (ret, revizyon, iptal).</summary>
    private void CloseOpenSteps(SalesOrder order)
    {
        foreach (var step in db.SalesOrderApprovalSteps.Local.Where(s =>
            s.SalesOrderId == order.Id && s.Round == order.ApprovalRound
            && s.Status is ApprovalStepStatus.Pending or ApprovalStepStatus.Waiting))
        {
            step.Status = ApprovalStepStatus.Cancelled;
        }
    }

    /// <summary>Adres metni: açık adres, ilçe / il; boş parçalar atlanır.</summary>
    private static string? AddressText(string? line, string? district, string? city)
    {
        var place = string.Join(" / ", new[] { district, city }.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p!.Trim()));
        var text = string.Join(", ", new[] { line?.Trim(), place }.Where(p => !string.IsNullOrWhiteSpace(p)));
        return text.Length == 0 ? null : text;
    }

    private DateOnly Today() => DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);

    /// <summary>Durum işlemlerinin ortak gövdesi: sürüm ve geçiş denetimi, işlem, kayıt.</summary>
    private async Task<Result<SalesOrderDto>> ChangeAsync(
        int id, SalesOrderActionRequest request, SalesOrderStatus target, bool fromSubmitted,
        Func<SalesOrder, DateTime, string, Error?> apply, CancellationToken cancellationToken)
    {
        var order = await db.SalesOrders.Include(o => o.Lines).SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
        {
            return NotFound;
        }

        if (RowVersionToken.Check(order.RowVersion, request.RowVersion, ConcurrencyConflict) is { } versionError)
        {
            return versionError;
        }

        // Ret, taslağa dönüşün yalnızca onay bekleyen siparişten yapılan biçimidir.
        if (!order.CanTransitionTo(target) || (fromSubmitted && order.Status != SalesOrderStatus.Submitted))
        {
            return new Error(
                ErrorCodes.InvalidStatusTransition,
                $"{StatusLabels[order.Status]} durumundaki sipariş '{StatusLabels[target]}' durumuna geçirilemez.");
        }

        if (apply(order, timeProvider.GetUtcNow().UtcDateTime, currentUser.UserName ?? SystemUser) is { } error)
        {
            return error;
        }

        return await SaveAndReloadAsync(order, cancellationToken);
    }

    private async Task<Error?> ValidateAsync(SalesOrderRequest request, SalesOrder? existing, CancellationToken cancellationToken)
    {
        // Fiyatları göremeyen kullanıcının formu fiyatları boş gönderir; kaydedilirse mevcut fiyatlar silinirdi.
        if (!await HasPermissionAsync(ERP.Konfeksiyon.Domain.Authorization.KonfeksiyonPermissions.SalesOrder.ViewPrices, cancellationToken))
        {
            return CannotSeePrices;
        }

        if (await CheckPriceFloorAsync(request, existing, cancellationToken) is { } priceError)
        {
            return priceError;
        }

        var productIds = request.Lines.Select(l => l.ProductId).Distinct().ToList();
        var referenceError =
            await ReferenceValidation.RequireSelectableAsync(
                db.Customers, request.CustomerId, existing?.CustomerId, "customerId", "Müşteri", cancellationToken)
            ?? await ReferenceValidation.RequireSelectableAsync(
                db.Products, productIds, existing?.Lines.Select(l => l.ProductId).ToHashSet() ?? [], "lines", "Ürün",
                cancellationToken)
            ?? await ReferenceValidation.RequireSelectableAsync(
                db.Colors, request.Lines.Select(l => l.ColorId).Distinct().ToList(),
                existing?.Lines.Select(l => l.ColorId).ToHashSet() ?? [], "lines", "Renk", cancellationToken)
            ?? await ReferenceValidation.RequireSelectableAsync(
                db.Sizes, request.Lines.Select(l => l.SizeId).Distinct().ToList(),
                existing?.Lines.Select(l => l.SizeId).ToHashSet() ?? [], "lines", "Beden", cancellationToken);
        if (referenceError is not null)
        {
            return referenceError;
        }

        // Model bir müşteriye özelse o modelin ürünü başka müşteriye satılamaz.
        var foreign = await db.Products
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(p => productIds.Contains(p.Id) && p.Model.CustomerId != null && p.Model.CustomerId != request.CustomerId)
            .Select(p => p.ProductCode)
            .FirstOrDefaultAsync(cancellationToken);
        if (foreign is not null)
        {
            var message = $"{foreign}: ürünün modeli başka bir müşteriye ait.";
            return new Error(
                ErrorCodes.ProductCustomerMismatch, message, ErrorType.BusinessRule,
                new Dictionary<string, string[]> { ["lines"] = [message] });
        }

        if (request.CustomerBuyerId is { } buyerId)
        {
            var buyer = await db.CustomerBuyers.AsNoTracking()
                .Where(b => b.Id == buyerId && b.CustomerId == request.CustomerId)
                .Select(b => new { b.IsActive })
                .SingleOrDefaultAsync(cancellationToken);
            if (buyer is null)
            {
                return ReferenceValidation.InvalidReference("customerBuyerId", "Seçilen buyer bu müşteriye ait değil.");
            }

            if (!buyer.IsActive && existing?.CustomerBuyerId != buyerId)
            {
                return ReferenceValidation.InvalidReference("customerBuyerId", "Seçilen buyer pasif.");
            }
        }

        if (request.DeliveryAddressId is { } addressId
            && !await db.CustomerAddresses.AnyAsync(a => a.Id == addressId && a.CustomerId == request.CustomerId, cancellationToken))
        {
            return ReferenceValidation.InvalidReference("deliveryAddressId", "Seçilen teslim adresi bu müşteriye ait değil.");
        }

        return await CurrencyRules.RequireActiveAsync(db, request.Currency, existing?.Currency, "currency", cancellationToken)
            ?? await CheckCustomerOrderNoAsync(request, existing?.Id, cancellationToken);
    }

    /// <summary>
    /// Müşteride "sipariş no tekil" seçeneği açıksa aynı müşteri sipariş numarası iptal edilmemiş başka bir siparişte
    /// kullanılamaz.
    /// </summary>
    private async Task<Error?> CheckCustomerOrderNoAsync(SalesOrderRequest request, int? orderId, CancellationToken cancellationToken)
    {
        var customerOrderNo = NullIfBlank(request.CustomerOrderNo);
        if (customerOrderNo is null
            || !await db.Customers.AnyAsync(c => c.Id == request.CustomerId && c.RequireUniqueCustomerOrderNo, cancellationToken))
        {
            return null;
        }

        var duplicate = await db.SalesOrders.AsNoTracking()
            .Where(o => o.CustomerId == request.CustomerId && o.CustomerOrderNo == customerOrderNo
                && o.Status != SalesOrderStatus.Cancelled && o.Id != orderId)
            .Select(o => o.OrderNo)
            .FirstOrDefaultAsync(cancellationToken);
        if (duplicate is null)
        {
            return null;
        }

        var message = $"Bu müşteri sipariş numarası {duplicate} numaralı siparişte kullanılmış; müşteri için sipariş numarası tekil olmalıdır.";
        return new Error(
            ErrorCodes.DuplicateCustomerOrderNo, message, ErrorType.Conflict,
            new Dictionary<string, string[]> { ["customerOrderNo"] = [message] });
    }

    /// <summary>Teslim adresi seçilmemişse müşterinin varsayılan teslimat adresi kullanılır.</summary>
    private async Task<int?> DeliveryAddressOfAsync(SalesOrderRequest request, CancellationToken cancellationToken) =>
        request.DeliveryAddressId
        ?? await db.CustomerAddresses.AsNoTracking()
            .Where(a => a.CustomerId == request.CustomerId && a.IsDefaultDelivery)
            .Select(a => (int?)a.Id)
            .FirstOrDefaultAsync(cancellationToken);

    private static void Apply(SalesOrderRequest request, SalesOrder order)
    {
        order.CustomerId = request.CustomerId;
        order.CustomerOrderNo = NullIfBlank(request.CustomerOrderNo);
        order.OrderDate = request.OrderDate;
        order.DueDate = request.DueDate;
        order.Currency = string.IsNullOrWhiteSpace(request.Currency) ? DefaultCurrency : request.Currency.Trim().ToUpperInvariant();
        order.Notes = NullIfBlank(request.Notes);
        order.CustomerBuyerId = request.CustomerBuyerId;
    }

    /// <summary>Kalemleri istekle eşitler: kalan kalem güncellenir, eksilen silinir, yeni gelen eklenir.</summary>
    /// <summary>
    /// Fiyat listesinin (iskonto sonrası) altında fiyat girmek ayrı yetki ister. Yalnızca yeni girilen ya da değişen
    /// fiyatlar denetlenir: liste sonradan artsa da kayıtlı satırın fiyatı kendiliğinden hatalı sayılmaz.
    /// </summary>
    private async Task<Error?> CheckPriceFloorAsync(
        SalesOrderRequest request, SalesOrder? existing, CancellationToken cancellationToken)
    {
        var previous = existing?.Lines.ToDictionary(l => (l.ProductId, l.ColorId, l.SizeId), l => l.UnitPrice) ?? [];
        var changed = request.Lines
            .Where(l => l.UnitPrice is not null
                && !(previous.TryGetValue((l.ProductId, l.ColorId, l.SizeId), out var before) && before == l.UnitPrice))
            .ToList();
        if (changed.Count == 0
            || await HasPermissionAsync(ERP.Konfeksiyon.Domain.Authorization.KonfeksiyonPermissions.SalesOrder.OverridePrice, cancellationToken))
        {
            return null;
        }

        var quotes = await pricing.QuoteAsync(
            new PriceQuoteRequest(
                request.CustomerId, request.Currency, request.OrderDate,
                changed.Select(l => new PriceQuoteItemRequest(l.ProductId, l.ColorId, l.SizeId)).ToList()),
            cancellationToken);
        var floor = quotes
            .Where(q => q.UnitPrice is not null)
            .GroupBy(q => (q.ProductId, q.ColorId, q.SizeId))
            .ToDictionary(g => g.Key, g => g.First().UnitPrice!.Value);
        var below = changed.FirstOrDefault(l =>
            floor.TryGetValue((l.ProductId, l.ColorId, l.SizeId), out var minimum) && l.UnitPrice < minimum);
        if (below is null)
        {
            return null;
        }

        var code = await db.Products.IgnoreQueryFilters().Where(p => p.Id == below.ProductId)
            .Select(p => p.ProductCode).SingleOrDefaultAsync(cancellationToken);
        var message =
            $"{code}: girilen fiyat ({below.UnitPrice}) fiyat listesinin altında " +
            $"({floor[(below.ProductId, below.ColorId, below.SizeId)]}). Liste altı fiyat ayrı yetki ister.";
        return new Error(
            ErrorCodes.Forbidden, message, ErrorType.Forbidden, new Dictionary<string, string[]> { ["lines"] = [message] });
    }

    private void SyncLines(SalesOrder order, IReadOnlyList<SalesOrderLineRequest> requested)
    {
        var wanted = requested.ToDictionary(l => (l.ProductId, l.ColorId, l.SizeId));

        foreach (var line in order.Lines.ToList())
        {
            if (wanted.Remove((line.ProductId, line.ColorId, line.SizeId), out var request))
            {
                line.Quantity = request.Quantity;
                line.UnitPrice = request.UnitPrice;
            }
            else
            {
                db.SalesOrderLines.Remove(line);
            }
        }

        foreach (var request in wanted.Values)
        {
            db.SalesOrderLines.Add(new SalesOrderLine
            {
                SalesOrderId = order.Id, ProductId = request.ProductId, ColorId = request.ColorId, SizeId = request.SizeId,
                Quantity = request.Quantity, UnitPrice = request.UnitPrice,
            });
        }
    }

    private async Task<Result<SalesOrderDto>> SaveAndReloadAsync(SalesOrder order, CancellationToken cancellationToken)
    {
        // Yalnızca kalemler değişse bile başlık satırı güncellenir: rowversion ilerler.
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
}
