using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Common;
using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Konfeksiyon.Application.Features.Sales;
using ERP.Konfeksiyon.Domain.Entities;
using ERP.Konfeksiyon.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERP.Konfeksiyon.Application.Features.Shipping;

public interface IShipmentPackingService
{
    /// <summary>Sevkiyatın kalemleri, kolilere girmiş adetleri ve kolileri.</summary>
    Task<Result<ShipmentPackingDto>> GetAsync(int shipmentId, CancellationToken cancellationToken);

    /// <summary>
    /// Koli dağıtımı (taslakta): mevcut koliler silinir; her ürün-renk için şablon oranlarıyla dolu koliler, kalan
    /// adetlerle renk başına bir karışık (artan) koli oluşturulur.
    /// </summary>
    Task<Result<ShipmentPackingDto>> DistributeAsync(int shipmentId, CartonDistributionRequest request, CancellationToken cancellationToken);

    /// <summary>Koli ekler (<paramref name="cartonId"/> boşsa, sona) ya da kolinin içeriğini ve ağırlığını değiştirir.</summary>
    Task<Result<ShipmentPackingDto>> SaveCartonAsync(
        int shipmentId, int? cartonId, ShipmentCartonRequest request, CancellationToken cancellationToken);

    /// <summary>Koliyi siler; sonraki kolilerin numarası bir azalır (etiketleri yeniden basılmalıdır).</summary>
    Task<Result<ShipmentPackingDto>> DeleteCartonAsync(int shipmentId, int cartonId, string rowVersion, CancellationToken cancellationToken);

    /// <summary>Bütün kolileri siler (çeki listesi kaldırılır).</summary>
    Task<Result<ShipmentPackingDto>> ClearAsync(int shipmentId, string rowVersion, CancellationToken cancellationToken);

    Task<Result<ShipmentPackingDto>> SetWeightsAsync(
        int shipmentId, ShipmentCartonWeightsRequest request, CancellationToken cancellationToken);

    /// <summary>Basılı belgelerin (çeki listesi, koli etiketi, irsaliye) verisi.</summary>
    Task<Result<ShipmentDocumentDto>> GetDocumentAsync(int shipmentId, CancellationToken cancellationToken);

    /// <summary>Terminalde seçilebilecek sevkiyatlar: kolisi olan taslak ya da sevk edilmiş sevkiyatlar, en yeniden.</summary>
    Task<IReadOnlyList<LoadableShipmentDto>> GetLoadableAsync(string? search, CancellationToken cancellationToken);

    Task<Result<ShipmentLoadingDto>> GetLoadingAsync(int shipmentId, CancellationToken cancellationToken);

    /// <summary>Sevkiyat numarası ya da koli kodu okutularak sevkiyatın yükleme durumunu bulur.</summary>
    Task<Result<ShipmentLoadingDto>> FindLoadingAsync(string code, CancellationToken cancellationToken);

    /// <summary>
    /// Okutulan koliyi yüklendi olarak işaretler. Aynı koli yeniden okutulursa ilk okutma korunur (hata değildir);
    /// başka sevkiyatın ya da tanınmayan koli reddedilir.
    /// </summary>
    Task<Result<CartonLoadResultDto>> LoadAsync(int shipmentId, string code, CancellationToken cancellationToken);
}

/// <summary>
/// Sevkiyatın kolileri (çeki listesi) ve kontrollü yükleme. Koliler yalnızca taslak sevkiyatta değiştirilir; her
/// değişiklik sevkiyatın sürümünü ilerletir (sevk etme ile çakışmaz). Yükleme okutması taslak ve sevk edilmiş
/// sevkiyatta yapılır ve sevkiyatın sürümünü değiştirmez.
/// </summary>
public sealed class ShipmentPackingService(
    IKonfeksiyonDbContext db,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IShipmentPackingService
{
    private const string SystemUser = "system";

    /// <summary>Bir sevkiyatta en fazla koli sayısı.</summary>
    public const int MaxCartons = 2000;

    private const string CartonNotFoundCode = "SHIPMENT_CARTON_NOT_FOUND";
    private const string CartonOtherShipmentCode = "SHIPMENT_CARTON_OTHER_SHIPMENT";
    private const string NotLoadableCode = "SHIPMENT_NOT_LOADABLE";
    private const string TooManyCartonsCode = "SHIPMENT_TOO_MANY_CARTONS";

    private static readonly Error NotFound = new(ErrorCodes.NotFound, "Sevkiyat bulunamadı.", ErrorType.NotFound);
    private static readonly Error CartonNotFound = new(ErrorCodes.NotFound, "Koli bulunamadı.", ErrorType.NotFound);

    private static readonly Error ConcurrencyConflict = new(
        ErrorCodes.ConcurrencyConflict,
        "Sevkiyat siz açtıktan sonra başka bir kullanıcı tarafından değiştirildi. Sayfayı yenileyip tekrar deneyin.",
        ErrorType.Conflict);

    private static readonly Error NotEditable = new(
        ErrorCodes.ShipmentNotEditable, "Koliler yalnızca taslak sevkiyatta değiştirilebilir.");

    public async Task<Result<ShipmentPackingDto>> GetAsync(int shipmentId, CancellationToken cancellationToken)
    {
        var shipment = await db.Shipments.AsNoTracking()
            .Where(s => s.Id == shipmentId)
            .Select(s => new { s.Id, s.ShipmentNo, s.CustomerId, s.Status, s.RowVersion })
            .SingleOrDefaultAsync(cancellationToken);
        if (shipment is null)
        {
            return NotFound;
        }

        var shipped = await db.ShipmentItems.AsNoTracking()
            .Where(i => i.ShipmentId == shipmentId)
            .GroupBy(i => i.StockItemId)
            .Select(g => new { StockItemId = g.Key, Quantity = g.Sum(i => i.Quantity) })
            .ToDictionaryAsync(g => g.StockItemId, g => g.Quantity, cancellationToken);

        var cartons = await db.ShipmentCartons.AsNoTracking().IgnoreQueryFilters()
            .Where(c => c.ShipmentId == shipmentId)
            .OrderBy(c => c.CartonNo)
            .Select(c => new
            {
                c.Id, c.CartonNo, c.AssortmentTemplateId,
                TemplateCode = c.AssortmentTemplate == null ? null : c.AssortmentTemplate.Code,
                c.NetKg, c.GrossKg, c.LoadedAt, c.LoadedBy,
            })
            .ToListAsync(cancellationToken);

        var lines = await db.ShipmentCartonLines.AsNoTracking().IgnoreQueryFilters()
            .Where(l => l.Carton.ShipmentId == shipmentId)
            .Select(l => new { l.Id, l.ShipmentCartonId, l.StockItemId, l.Quantity })
            .ToListAsync(cancellationToken);

        var packed = lines.GroupBy(l => l.StockItemId).ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity));
        var variants = await VariantsAsync(shipped.Keys.Union(packed.Keys).ToList(), cancellationToken);

        var items = variants.Values
            .OrderBy(v => v.ProductCode ?? v.Code, StringComparer.Ordinal)
            .ThenBy(v => v.ColorCode ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(v => v.SizeOrder)
            .ThenBy(v => v.Code, StringComparer.Ordinal)
            .Select(v => new ShipmentPackingItemDto(
                v.Id, v.Code, v.ProductCode, v.ProductName, v.ColorCode, v.ColorName, v.SizeCode, v.SizeName, v.SizeOrder, v.Unit,
                shipped.GetValueOrDefault(v.Id), packed.GetValueOrDefault(v.Id)))
            .ToList();

        var linesByCarton = lines.ToLookup(l => l.ShipmentCartonId);
        var cartonDtos = cartons
            .Select(c =>
            {
                var cartonLines = linesByCarton[c.Id]
                    .Select(l =>
                    {
                        var variant = variants[l.StockItemId];
                        return new ShipmentCartonLineDto(
                            l.Id, l.StockItemId, variant.Code, variant.ColorName, variant.SizeName, variant.SizeOrder, l.Quantity);
                    })
                    .OrderBy(l => l.ColorName ?? string.Empty, StringComparer.Ordinal)
                    .ThenBy(l => l.SizeOrder)
                    .ThenBy(l => l.ItemCode, StringComparer.Ordinal)
                    .ToList();
                return new ShipmentCartonDto(
                    c.Id, c.CartonNo, ShipmentCarton.CodeOf(shipment.ShipmentNo, c.CartonNo), c.AssortmentTemplateId, c.TemplateCode,
                    c.NetKg, c.GrossKg, c.LoadedAt, c.LoadedBy, cartonLines.Sum(l => l.Quantity), cartonLines);
            })
            .ToList();

        return new ShipmentPackingDto(
            shipment.Id,
            shipment.ShipmentNo,
            shipment.CustomerId,
            shipment.Status,
            shipment.Status == ShipmentStatus.Draft,
            RowVersionToken.From(shipment.RowVersion),
            cartons.Count,
            cartons.Count(c => c.LoadedAt != null),
            cartons.Count == 0 || CartonMatching.Matches(shipped, packed),
            items,
            cartonDtos);
    }

    public async Task<Result<ShipmentPackingDto>> DistributeAsync(
        int shipmentId, CartonDistributionRequest request, CancellationToken cancellationToken)
    {
        var loaded = await LoadEditableAsync(shipmentId, request.RowVersion, cancellationToken);
        if (!loaded.IsSuccess)
        {
            return loaded.Error!;
        }

        var shipment = loaded.Value;
        if (shipment.Items.Count == 0)
        {
            return new Error(ErrorCodes.ShipmentHasNoItems, "Kalemi olmayan sevkiyatta koli dağıtılamaz.");
        }

        var template = await db.AssortmentTemplates.AsNoTracking()
            .Where(t => t.Id == request.AssortmentTemplateId)
            .Select(t => new { t.Id, t.Code, t.CustomerId, t.IsActive, Lines = t.Lines.Select(l => new { l.SizeId, l.Ratio }).ToList() })
            .SingleOrDefaultAsync(cancellationToken);
        if (template is null)
        {
            return ReferenceValidation.InvalidReference("assortmentTemplateId", "Asorti şablonu bulunamadı.");
        }

        if (!template.IsActive)
        {
            return ReferenceValidation.InvalidReference("assortmentTemplateId", $"{template.Code} şablonu pasif durumda.");
        }

        if (template.CustomerId is { } owner && owner != shipment.CustomerId)
        {
            return ReferenceValidation.InvalidReference("assortmentTemplateId", $"{template.Code} şablonu başka bir müşteriye ait.");
        }

        if (template.Lines.Count == 0)
        {
            return ReferenceValidation.InvalidReference("assortmentTemplateId", $"{template.Code} şablonunda beden yok.");
        }

        var variants = await VariantsAsync(shipment.Items.Select(i => i.StockItemId).Distinct().ToList(), cancellationToken);
        var plan = CartonPlanner.Plan(
            shipment.Items.Select(i => new CartonPlanner.Item(
                i.StockItemId, variants[i.StockItemId].ProductId, variants[i.StockItemId].ColorId, variants[i.StockItemId].SizeId,
                variants[i.StockItemId].ProductCode ?? variants[i.StockItemId].Code, variants[i.StockItemId].ColorCode ?? string.Empty,
                i.Quantity)),
            template.Lines.ToDictionary(l => l.SizeId, l => l.Ratio));

        if (plan.Count > MaxCartons)
        {
            return new Error(
                TooManyCartonsCode,
                $"Dağıtım {plan.Count} koli üretiyor; bir sevkiyatta en fazla {MaxCartons} koli olabilir.",
                ErrorType.BusinessRule);
        }

        RemoveCartons(shipment.Cartons);
        var number = 0;
        foreach (var planned in plan)
        {
            var carton = new ShipmentCarton
            {
                ShipmentId = shipment.Id,
                AssortmentTemplateId = planned.FromTemplate ? template.Id : null,
            };
            carton.Renumber(++number);
            foreach (var (stockItemId, quantity) in planned.Lines)
            {
                carton.Lines.Add(new ShipmentCartonLine { StockItemId = stockItemId, Quantity = quantity });
            }

            db.ShipmentCartons.Add(carton);
        }

        return await SaveAndReloadAsync(shipment, cancellationToken);
    }

    public async Task<Result<ShipmentPackingDto>> SaveCartonAsync(
        int shipmentId, int? cartonId, ShipmentCartonRequest request, CancellationToken cancellationToken)
    {
        var loaded = await LoadEditableAsync(shipmentId, request.RowVersion, cancellationToken);
        if (!loaded.IsSuccess)
        {
            return loaded.Error!;
        }

        var shipment = loaded.Value;
        var carton = cartonId is { } id ? shipment.Cartons.SingleOrDefault(c => c.Id == id) : null;
        if (cartonId is not null && carton is null)
        {
            return CartonNotFound;
        }

        if (carton is null && shipment.Cartons.Count >= MaxCartons)
        {
            return new Error(TooManyCartonsCode, $"Bir sevkiyatta en fazla {MaxCartons} koli olabilir.", ErrorType.BusinessRule);
        }

        // Koliye yalnızca sevkiyatın kalemlerindeki mamuller girer.
        var allowed = shipment.Items.Select(i => i.StockItemId).ToHashSet();
        var variants = await VariantsAsync(request.Lines.Select(l => l.StockItemId).Distinct().ToList(), cancellationToken);
        foreach (var line in request.Lines)
        {
            if (!allowed.Contains(line.StockItemId) || !variants.TryGetValue(line.StockItemId, out var variant))
            {
                return LineError("Koliye yalnızca sevkiyat kalemlerindeki mamuller girebilir.");
            }

            if (variant.Unit == ERP.Core.Domain.Enums.StockUnit.Piece && line.Quantity != decimal.Truncate(line.Quantity))
            {
                return LineError($"{variant.Code}: adetle izlenen kartta miktar tam sayı olmalıdır.");
            }
        }

        if (carton is null)
        {
            carton = new ShipmentCarton { ShipmentId = shipment.Id };
            carton.Renumber(shipment.Cartons.Select(c => c.CartonNo).DefaultIfEmpty(0).Max() + 1);
            db.ShipmentCartons.Add(carton);
            foreach (var line in request.Lines)
            {
                carton.Lines.Add(new ShipmentCartonLine { StockItemId = line.StockItemId, Quantity = line.Quantity });
            }
        }
        else
        {
            var wanted = request.Lines.ToDictionary(l => l.StockItemId, l => l.Quantity);
            foreach (var line in carton.Lines.ToList())
            {
                if (wanted.Remove(line.StockItemId, out var quantity))
                {
                    line.Quantity = quantity;
                }
                else
                {
                    db.ShipmentCartonLines.Remove(line);
                }
            }

            foreach (var (stockItemId, quantity) in wanted)
            {
                db.ShipmentCartonLines.Add(new ShipmentCartonLine { ShipmentCartonId = carton.Id, StockItemId = stockItemId, Quantity = quantity });
            }
        }

        carton.NetKg = request.NetKg;
        carton.GrossKg = request.GrossKg;
        return await SaveAndReloadAsync(shipment, cancellationToken);

        static Error LineError(string message) => new(
            ErrorCodes.ValidationError, message, ErrorType.Validation, new Dictionary<string, string[]> { ["lines"] = [message] });
    }

    public async Task<Result<ShipmentPackingDto>> DeleteCartonAsync(
        int shipmentId, int cartonId, string rowVersion, CancellationToken cancellationToken)
    {
        var loaded = await LoadEditableAsync(shipmentId, rowVersion, cancellationToken);
        if (!loaded.IsSuccess)
        {
            return loaded.Error!;
        }

        var shipment = loaded.Value;
        var carton = shipment.Cartons.SingleOrDefault(c => c.Id == cartonId);
        if (carton is null)
        {
            return CartonNotFound;
        }

        var deletedNo = carton.CartonNo;
        RemoveCartons([carton]);
        db.Entry(shipment).State = EntityState.Modified;

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ConcurrencyConflict;
        }

        // Numaralar tek komutla kaydırılır (benzersiz indeks komut sonunda denetlenir). Numarası değişen kolinin
        // etiketi değiştiği için yükleme okutması da geçersiz olur.
        await db.ShipmentCartons
            .Where(c => c.ShipmentId == shipmentId && c.CartonNo > deletedNo)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(c => c.CartonNo, c => c.CartonNo - 1)
                    .SetProperty(c => c.LoadedAt, (DateTime?)null)
                    .SetProperty(c => c.LoadedBy, (string?)null),
                cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        db.Entry(shipment).State = EntityState.Detached;
        return await GetAsync(shipmentId, cancellationToken);
    }

    public async Task<Result<ShipmentPackingDto>> ClearAsync(int shipmentId, string rowVersion, CancellationToken cancellationToken)
    {
        var loaded = await LoadEditableAsync(shipmentId, rowVersion, cancellationToken);
        if (!loaded.IsSuccess)
        {
            return loaded.Error!;
        }

        RemoveCartons(loaded.Value.Cartons);
        return await SaveAndReloadAsync(loaded.Value, cancellationToken);
    }

    public async Task<Result<ShipmentPackingDto>> SetWeightsAsync(
        int shipmentId, ShipmentCartonWeightsRequest request, CancellationToken cancellationToken)
    {
        var loaded = await LoadEditableAsync(shipmentId, request.RowVersion, cancellationToken);
        if (!loaded.IsSuccess)
        {
            return loaded.Error!;
        }

        var ids = request.CartonIds.ToHashSet();
        var cartons = loaded.Value.Cartons.Where(c => ids.Contains(c.Id)).ToList();
        if (cartons.Count != ids.Count)
        {
            return CartonNotFound;
        }

        foreach (var carton in cartons)
        {
            carton.NetKg = request.NetKg;
            carton.GrossKg = request.GrossKg;
        }

        return await SaveAndReloadAsync(loaded.Value, cancellationToken);
    }

    public async Task<Result<ShipmentDocumentDto>> GetDocumentAsync(int shipmentId, CancellationToken cancellationToken)
    {
        // Silinmiş müşteri, sipariş ve kart da belgede adıyla yer alır.
        var header = await db.Shipments.AsNoTracking().IgnoreQueryFilters()
            .Where(s => s.Id == shipmentId && !s.IsDeleted)
            .Select(s => new
            {
                s.Id, s.ShipmentNo, s.DocumentNo, s.ShipDate, s.Status, s.ShippedAt, s.Carrier, s.Notes, s.CustomerId,
                s.Customer.CustomerCode, CustomerName = s.Customer.Name, s.Customer.TaxOffice, s.Customer.TaxNumber,
                s.Customer.Phone, s.Customer.Email, s.Customer.DocumentLanguage,
                SalesOrderNo = s.SalesOrder == null ? null : s.SalesOrder.OrderNo,
                CustomerOrderNo = s.SalesOrder == null ? null : s.SalesOrder.CustomerOrderNo,
                OrderDelivery = s.SalesOrder == null || s.SalesOrder.DeliveryAddress == null
                    ? null
                    : new AddressParts(
                        s.SalesOrder.DeliveryAddress.AddressLine, s.SalesOrder.DeliveryAddress.District,
                        s.SalesOrder.DeliveryAddress.City, s.SalesOrder.DeliveryAddress.CountryCode),
                ProductionOrderNo = s.ProductionOrder == null ? null : s.ProductionOrder.OrderNo,
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (header is null)
        {
            return NotFound;
        }

        var addresses = await db.CustomerAddresses.AsNoTracking()
            .Where(a => a.CustomerId == header.CustomerId && (a.IsDefaultInvoice || a.IsDefaultDelivery))
            .Select(a => new { a.IsDefaultInvoice, a.IsDefaultDelivery, Parts = new AddressParts(a.AddressLine, a.District, a.City, a.CountryCode) })
            .ToListAsync(cancellationToken);
        var invoiceAddress = addresses.FirstOrDefault(a => a.IsDefaultInvoice)?.Parts;
        var deliveryAddress = header.OrderDelivery ?? addresses.FirstOrDefault(a => a.IsDefaultDelivery)?.Parts;

        var factory = await db.Factories.AsNoTracking()
            .Where(f => f.IsActive)
            .OrderBy(f => f.Id)
            .Select(f => new { f.Name, Parts = new AddressParts(f.AddressLine, f.District, f.City, f.CountryCode), f.Phone, f.Email })
            .FirstOrDefaultAsync(cancellationToken);

        var quantities = await db.ShipmentItems.AsNoTracking().IgnoreQueryFilters()
            .Where(i => i.ShipmentId == shipmentId)
            .GroupBy(i => i.StockItemId)
            .Select(g => new { StockItemId = g.Key, Quantity = g.Sum(i => i.Quantity) })
            .ToDictionaryAsync(g => g.StockItemId, g => g.Quantity, cancellationToken);

        var cartons = await db.ShipmentCartons.AsNoTracking().IgnoreQueryFilters()
            .Where(c => c.ShipmentId == shipmentId)
            .OrderBy(c => c.CartonNo)
            .Select(c => new
            {
                c.CartonNo, c.NetKg, c.GrossKg,
                Lines = c.Lines.Select(l => new ShipmentDocumentCartonLineDto(l.StockItemId, l.Quantity)).ToList(),
            })
            .ToListAsync(cancellationToken);

        var variants = await VariantsAsync(
            quantities.Keys.Union(cartons.SelectMany(c => c.Lines).Select(l => l.StockItemId)).ToList(), cancellationToken);
        var items = variants.Values
            .OrderBy(v => v.ProductCode ?? v.Code, StringComparer.Ordinal)
            .ThenBy(v => v.ColorCode ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(v => v.SizeOrder)
            .ThenBy(v => v.Code, StringComparer.Ordinal)
            .Select(v => new ShipmentDocumentItemDto(
                v.Id, v.Code, v.ProductCode, v.ProductName, v.CustomerArticleCode, v.ColorCode, v.ColorName, v.SizeCode,
                v.SizeName, v.SizeOrder, v.Unit, quantities.GetValueOrDefault(v.Id)))
            .ToList();

        return new ShipmentDocumentDto(
            header.Id,
            header.ShipmentNo,
            header.DocumentNo,
            header.ShipDate,
            header.Status,
            header.ShippedAt,
            header.Carrier,
            header.Notes,
            header.CustomerCode,
            new ProformaPartyDto(header.CustomerName, header.TaxOffice, header.TaxNumber, null, header.Phone, header.Email),
            invoiceAddress?.Text,
            deliveryAddress?.Text,
            header.SalesOrderNo,
            header.CustomerOrderNo,
            header.ProductionOrderNo,
            factory is null ? null : new ProformaPartyDto(factory.Name, null, null, factory.Parts.Text, factory.Phone, factory.Email),
            header.DocumentLanguage,
            items,
            cartons
                .Select(c => new ShipmentDocumentCartonDto(
                    c.CartonNo, ShipmentCarton.CodeOf(header.ShipmentNo, c.CartonNo), c.NetKg, c.GrossKg, c.Lines))
                .ToList(),
            timeProvider.GetUtcNow().UtcDateTime);
    }

    public async Task<IReadOnlyList<LoadableShipmentDto>> GetLoadableAsync(string? search, CancellationToken cancellationToken)
    {
        var shipments = db.Shipments.AsNoTracking()
            .Where(s => s.Status != ShipmentStatus.Cancelled && s.Cartons.Any());
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            shipments = shipments.Where(s => s.ShipmentNo.Contains(term) || s.Customer.Name.Contains(term)
                || (s.DocumentNo != null && s.DocumentNo.Contains(term)));
        }

        // Yüklemesi bitmemiş sevkiyatlar önce gelir.
        return await shipments
            .OrderBy(s => s.Cartons.All(c => c.LoadedAt != null))
            .ThenByDescending(s => s.ShipDate)
            .ThenByDescending(s => s.Id)
            .Take(50)
            .Select(s => new LoadableShipmentDto(
                s.Id, s.ShipmentNo, s.Customer.Name, s.ShipDate, s.Status, s.Cartons.Count, s.Cartons.Count(c => c.LoadedAt != null)))
            .ToListAsync(cancellationToken);
    }

    public async Task<Result<ShipmentLoadingDto>> GetLoadingAsync(int shipmentId, CancellationToken cancellationToken)
    {
        var shipment = await db.Shipments.AsNoTracking().IgnoreQueryFilters()
            .Where(s => s.Id == shipmentId && !s.IsDeleted)
            .Select(s => new { s.Id, s.ShipmentNo, CustomerName = s.Customer.Name, s.Status, s.ShipDate })
            .SingleOrDefaultAsync(cancellationToken);
        if (shipment is null)
        {
            return NotFound;
        }

        var cartons = await db.ShipmentCartons.AsNoTracking().IgnoreQueryFilters()
            .Where(c => c.ShipmentId == shipmentId)
            .OrderBy(c => c.CartonNo)
            .Select(c => new
            {
                c.Id, c.CartonNo, c.LoadedAt, c.LoadedBy,
                Lines = c.Lines.Select(l => new
                {
                    l.Quantity,
                    ColorName = l.StockItem.Color == null ? null : l.StockItem.Color.Name,
                    SizeCode = l.StockItem.Size == null ? null : l.StockItem.Size.SizeCode,
                    SizeOrder = l.StockItem.Size == null ? 0 : l.StockItem.Size.SortOrder,
                }).ToList(),
            })
            .ToListAsync(cancellationToken);

        return new ShipmentLoadingDto(
            shipment.Id,
            shipment.ShipmentNo,
            shipment.CustomerName,
            shipment.Status,
            shipment.ShipDate,
            cartons.Count,
            cartons.Count(c => c.LoadedAt != null),
            cartons
                .Select(c => new ShipmentLoadingCartonDto(
                    c.Id,
                    c.CartonNo,
                    ShipmentCarton.CodeOf(shipment.ShipmentNo, c.CartonNo),
                    c.Lines.Sum(l => l.Quantity),
                    // Örn. "Lacivert S:1 M:2 L:2"; birden fazla renk varsa renkler ayrı ayrı yazılır.
                    string.Join(" · ", c.Lines
                        .GroupBy(l => l.ColorName ?? "—")
                        .OrderBy(g => g.Key, StringComparer.Ordinal)
                        .Select(g => $"{g.Key} " + string.Join(" ", g
                            .OrderBy(l => l.SizeOrder)
                            .Select(l => $"{l.SizeCode ?? "?"}:{l.Quantity:0.###}")))),
                    c.LoadedAt,
                    c.LoadedBy))
                .ToList());
    }

    public async Task<Result<ShipmentLoadingDto>> FindLoadingAsync(string code, CancellationToken cancellationToken)
    {
        var text = code.Trim().ToUpperInvariant();
        var shipmentId = await db.Shipments.AsNoTracking()
            .Where(s => s.ShipmentNo == text)
            .Select(s => (int?)s.Id)
            .SingleOrDefaultAsync(cancellationToken);

        // Sevkiyat numarası değilse koli kodu olabilir: koli kodu sevkiyat numarasını taşır.
        if (shipmentId is null && ShipmentCarton.TryParseCode(text, out var shipmentNo, out _))
        {
            shipmentId = await db.Shipments.AsNoTracking()
                .Where(s => s.ShipmentNo == shipmentNo)
                .Select(s => (int?)s.Id)
                .SingleOrDefaultAsync(cancellationToken);
        }

        if (shipmentId is not { } id)
        {
            return new Error(ErrorCodes.NotFound, $"{text} ile eşleşen sevkiyat ya da koli bulunamadı.", ErrorType.NotFound);
        }

        return await GetLoadingAsync(id, cancellationToken);
    }

    public async Task<Result<CartonLoadResultDto>> LoadAsync(int shipmentId, string code, CancellationToken cancellationToken)
    {
        var shipment = await db.Shipments.AsNoTracking()
            .Where(s => s.Id == shipmentId)
            .Select(s => new { s.ShipmentNo, s.Status })
            .SingleOrDefaultAsync(cancellationToken);
        if (shipment is null)
        {
            return NotFound;
        }

        if (shipment.Status == ShipmentStatus.Cancelled)
        {
            return new Error(NotLoadableCode, "İptal edilmiş sevkiyata koli yüklenemez.", ErrorType.Conflict);
        }

        var text = code.Trim().ToUpperInvariant();
        if (!ShipmentCarton.TryParseCode(text, out var shipmentNo, out var cartonNo))
        {
            return new Error(CartonNotFoundCode, $"{text} bir koli barkodu değil.", ErrorType.NotFound);
        }

        if (!string.Equals(shipmentNo, shipment.ShipmentNo, StringComparison.OrdinalIgnoreCase))
        {
            return new Error(
                CartonOtherShipmentCode,
                $"{text} başka bir sevkiyatın ({shipmentNo}) kolisi; bu araca yüklenmemeli.",
                ErrorType.Conflict);
        }

        var carton = await db.ShipmentCartons.SingleOrDefaultAsync(
            c => c.ShipmentId == shipmentId && c.CartonNo == cartonNo, cancellationToken);
        if (carton is null)
        {
            return new Error(
                CartonNotFoundCode,
                $"{text} bu sevkiyatın çeki listesinde yok (koliler yeniden dağıtılmış olabilir; etiketi kontrol edin).",
                ErrorType.NotFound);
        }

        var fresh = carton.MarkLoaded(timeProvider.GetUtcNow().UtcDateTime, currentUser.UserName ?? SystemUser);
        if (fresh)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        var loading = await GetLoadingAsync(shipmentId, cancellationToken);
        if (!loading.IsSuccess)
        {
            return loading.Error!;
        }

        return new CartonLoadResultDto(cartonNo, ShipmentCarton.CodeOf(shipment.ShipmentNo, cartonNo), !fresh, loading.Value);
    }

    /// <summary>Taslak sevkiyatı kalemleri ve kolileriyle (değiştirilmek üzere) yükler; sürümü ve durumu denetler.</summary>
    private async Task<Result<Shipment>> LoadEditableAsync(int shipmentId, string rowVersion, CancellationToken cancellationToken)
    {
        var shipment = await db.Shipments
            .Include(s => s.Items)
            .Include(s => s.Cartons).ThenInclude(c => c.Lines)
            .SingleOrDefaultAsync(s => s.Id == shipmentId, cancellationToken);
        if (shipment is null)
        {
            return NotFound;
        }

        if (RowVersionToken.Check(shipment.RowVersion, rowVersion, ConcurrencyConflict) is { } versionError)
        {
            return versionError;
        }

        if (!shipment.IsEditable)
        {
            return NotEditable;
        }

        return shipment;
    }

    /// <summary>Kolileri satırlarıyla siler (ilişkilerde zincirleme silme yoktur).</summary>
    private void RemoveCartons(IEnumerable<ShipmentCarton> cartons)
    {
        foreach (var carton in cartons.ToList())
        {
            db.ShipmentCartonLines.RemoveRange(carton.Lines);
            db.ShipmentCartons.Remove(carton);
        }
    }

    private async Task<Result<ShipmentPackingDto>> SaveAndReloadAsync(Shipment shipment, CancellationToken cancellationToken)
    {
        // Koli değişikliği sevkiyatın sürümünü ilerletir: açık ekrandaki "sevk et" eski çeki listesiyle çalışmaz.
        db.Entry(shipment).State = EntityState.Modified;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ConcurrencyConflict;
        }

        return await GetAsync(shipment.Id, cancellationToken);
    }

    private async Task<Dictionary<int, Variant>> VariantsAsync(IReadOnlyCollection<int> stockItemIds, CancellationToken cancellationToken) =>
        await db.StockItems.AsNoTracking().IgnoreQueryFilters()
            .Where(i => stockItemIds.Contains(i.Id))
            .Select(i => new Variant(
                i.Id,
                i.Code,
                i.Unit,
                i.ProductId,
                i.Product == null ? null : i.Product.ProductCode,
                i.Product == null ? null : i.Product.Name,
                i.Product == null ? null : i.Product.CustomerArticleCode,
                i.ColorId,
                i.Color == null ? null : i.Color.ColorCode,
                i.Color == null ? null : i.Color.Name,
                i.SizeId,
                i.Size == null ? null : i.Size.SizeCode,
                i.Size == null ? null : i.Size.Name,
                i.Size == null ? 0 : i.Size.SortOrder))
            .ToDictionaryAsync(v => v.Id, cancellationToken);

    /// <summary>Mamul kartının ürün / renk / beden bilgisi.</summary>
    private sealed record Variant(
        int Id,
        string Code,
        ERP.Core.Domain.Enums.StockUnit Unit,
        int? ProductId,
        string? ProductCode,
        string? ProductName,
        string? CustomerArticleCode,
        int? ColorId,
        string? ColorCode,
        string? ColorName,
        int? SizeId,
        string? SizeCode,
        string? SizeName,
        int SizeOrder);

    /// <summary>Adresin parçaları; belgede tek satır olarak yazılır.</summary>
    private sealed record AddressParts(string? Line, string? District, string? City, string? CountryCode)
    {
        public string? Text
        {
            get
            {
                var place = string.Join(" / ", new[] { District, City }.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p!.Trim()));
                var country = string.IsNullOrWhiteSpace(CountryCode) || CountryCode == "TR" ? null : CountryCode;
                var text = string.Join(", ", new[] { Line?.Trim(), place, country }.Where(p => !string.IsNullOrWhiteSpace(p)));
                return text.Length == 0 ? null : text;
            }
        }
    }
}

/// <summary>
/// Koli dağıtımının hesabı (veritabanından bağımsız): her ürün-renk için şablon oranlarıyla kaç dolu koli
/// çıkacağı ve kalan adetlerin karışık koliye konması.
/// </summary>
public static class CartonPlanner
{
    public sealed record Item(int StockItemId, int? ProductId, int? ColorId, int? SizeId, string ProductCode, string ColorCode, decimal Quantity);

    /// <param name="FromTemplate">Şablon oranlarıyla doldurulmuş koli; false: kalan adetlerin karışık kolisi.</param>
    public sealed record PlannedCarton(bool FromTemplate, IReadOnlyList<(int StockItemId, decimal Quantity)> Lines);

    /// <summary>
    /// Koliler ürün koduna, sonra renk koduna göre sıralanır; her ürün-renkte önce dolu koliler, sonra (kalan varsa)
    /// tek bir karışık koli gelir. Dolu koli sayısı, şablondaki her bedenin adedinin oranına bölümünün en küçüğüdür.
    /// </summary>
    public static IReadOnlyList<PlannedCarton> Plan(IEnumerable<Item> items, IReadOnlyDictionary<int, int> ratios)
    {
        var result = new List<PlannedCarton>();
        var groups = items
            .Where(i => i.Quantity > 0)
            .GroupBy(i => (i.ProductId, i.ColorId))
            .OrderBy(g => g.First().ProductCode, StringComparer.Ordinal)
            .ThenBy(g => g.First().ColorCode, StringComparer.Ordinal);

        foreach (var group in groups)
        {
            // Kalan adet, kalem bazında izlenir (bir beden birden fazla kartta olabilir).
            var remaining = group.ToDictionary(i => i.StockItemId, i => i.Quantity);
            var bySize = group
                .Where(i => i.SizeId is not null && ratios.ContainsKey(i.SizeId.Value))
                .GroupBy(i => i.SizeId!.Value)
                .ToDictionary(g => g.Key, g => g.Select(i => i.StockItemId).ToList());

            var full = ratios.All(r => bySize.ContainsKey(r.Key))
                ? ratios.Min(r => (int)decimal.Floor(bySize[r.Key].Sum(id => remaining[id]) / r.Value))
                : 0;

            for (var carton = 0; carton < full; carton++)
            {
                var lines = new List<(int StockItemId, decimal Quantity)>();
                foreach (var (sizeId, ratio) in ratios)
                {
                    decimal need = ratio;
                    foreach (var stockItemId in bySize[sizeId])
                    {
                        var take = Math.Min(need, remaining[stockItemId]);
                        if (take <= 0)
                        {
                            continue;
                        }

                        lines.Add((stockItemId, take));
                        remaining[stockItemId] -= take;
                        need -= take;
                        if (need == 0)
                        {
                            break;
                        }
                    }
                }

                result.Add(new PlannedCarton(true, Merge(lines)));
            }

            var leftover = remaining.Where(r => r.Value > 0).Select(r => (r.Key, r.Value)).ToList();
            if (leftover.Count > 0)
            {
                result.Add(new PlannedCarton(false, leftover));
            }
        }

        return result;
    }

    private static IReadOnlyList<(int StockItemId, decimal Quantity)> Merge(IEnumerable<(int StockItemId, decimal Quantity)> lines) =>
        lines.GroupBy(l => l.StockItemId).Select(g => (g.Key, g.Sum(l => l.Quantity))).ToList();
}

/// <summary>Kolilerdeki adetlerin sevkiyat kalemleriyle karşılaştırılması (sevk öncesi denetim ve ekrandaki uyarı).</summary>
public static class CartonMatching
{
    public const string MismatchCode = "SHIPMENT_CARTONS_MISMATCH";

    /// <summary>Her mamul kartı için kalem adedi ile kolilerdeki toplam adet eşit mi (kolide fazladan kart da olmamalı).</summary>
    public static bool Matches(IReadOnlyDictionary<int, decimal> shipped, IReadOnlyDictionary<int, decimal> packed) =>
        shipped.Keys.Union(packed.Keys).All(id => shipped.GetValueOrDefault(id) == packed.GetValueOrDefault(id));

    /// <summary>Ayrıntı sayfası için özet; kalemleri yüklenmiş sevkiyattan hesaplanır.</summary>
    public static async Task<ShipmentCartonSummaryDto> SummaryAsync(
        IKonfeksiyonDbContext db, Shipment shipment, CancellationToken cancellationToken)
    {
        var cartons = db.ShipmentCartons.AsNoTracking().IgnoreQueryFilters().Where(c => c.ShipmentId == shipment.Id);
        var count = await cartons.CountAsync(cancellationToken);
        if (count == 0)
        {
            return new ShipmentCartonSummaryDto(0, 0, true);
        }

        var loaded = await cartons.CountAsync(c => c.LoadedAt != null, cancellationToken);
        var packed = await PackedAsync(db, shipment.Id, cancellationToken);
        return new ShipmentCartonSummaryDto(count, loaded, Matches(Shipped(shipment), packed));
    }

    /// <summary>Koli varsa ve adetler tutmuyorsa sevki engelleyen hata; koli yoksa ya da tutuyorsa null.</summary>
    public static async Task<Error?> CheckAsync(IKonfeksiyonDbContext db, Shipment shipment, CancellationToken cancellationToken)
    {
        if (!await db.ShipmentCartons.AnyAsync(c => c.ShipmentId == shipment.Id, cancellationToken))
        {
            return null;
        }

        var shipped = Shipped(shipment);
        var packed = await PackedAsync(db, shipment.Id, cancellationToken);
        if (Matches(shipped, packed))
        {
            return null;
        }

        var ids = shipped.Keys.Union(packed.Keys).ToList();
        var codes = await db.StockItems.AsNoTracking().IgnoreQueryFilters()
            .Where(i => ids.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, i => i.Code, cancellationToken);
        var problems = ids
            .Where(id => shipped.GetValueOrDefault(id) != packed.GetValueOrDefault(id))
            .OrderBy(id => codes.GetValueOrDefault(id, string.Empty), StringComparer.Ordinal)
            .Select(id =>
                $"{codes.GetValueOrDefault(id, id.ToString())}: sevkiyatta {shipped.GetValueOrDefault(id):0.###}, kolilerde {packed.GetValueOrDefault(id):0.###}.")
            .ToArray();

        return new Error(
            MismatchCode,
            "Koli içerikleri sevkiyat kalemleriyle tutmuyor. Sevk etmeden önce çeki listesini düzeltin ya da kolileri yeniden dağıtın.",
            ErrorType.BusinessRule,
            new Dictionary<string, string[]> { ["cartons"] = problems });
    }

    private static Dictionary<int, decimal> Shipped(Shipment shipment) =>
        shipment.Items.GroupBy(i => i.StockItemId).ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

    private static Task<Dictionary<int, decimal>> PackedAsync(IKonfeksiyonDbContext db, int shipmentId, CancellationToken cancellationToken) =>
        db.ShipmentCartonLines.AsNoTracking().IgnoreQueryFilters()
            .Where(l => l.Carton.ShipmentId == shipmentId)
            .GroupBy(l => l.StockItemId)
            .Select(g => new { StockItemId = g.Key, Quantity = g.Sum(l => l.Quantity) })
            .ToDictionaryAsync(g => g.StockItemId, g => g.Quantity, cancellationToken);
}
