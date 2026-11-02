using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Core.Application.Abstractions;
using ERP.Konfeksiyon.Application.Features.Sales;
using ERP.Konfeksiyon.Domain.Entities;
using ERP.Konfeksiyon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using KP = ERP.Konfeksiyon.Domain.Authorization.KonfeksiyonPermissions;
using P = ERP.Core.Domain.Authorization.Permissions;

namespace ERP.Konfeksiyon.Application.Features.Workspace;

/// <summary>Ana sayfadaki bir "yapılacak iş": bekleyen kayıt sayısı ve işin yapılacağı ekran.</summary>
/// <param name="Key">İşin sabit anahtarı (ör. bundles-without-card).</param>
/// <param name="Path">Web'de işin yapılacağı ekranın adresi.</param>
/// <param name="Items">
/// Onay bekleyen kayıtlar tek tek (en yeniden eskiye, en fazla <see cref="WorkspaceService.MaxTaskItems"/>); bildirimde
/// her biri kendi sayfasına götürür. Kayıt bazında gösterilmeyen işlerde boştur.
/// </param>
public sealed record WorkTaskDto(
    string Key, string Title, string Description, int Count, string Path, IReadOnlyList<WorkTaskItemDto>? Items = null);

/// <summary>Bekleyen tek bir kayıt (ör. onay bekleyen sipariş).</summary>
/// <param name="Path">Kaydın ayrıntı sayfası.</param>
/// <param name="At">Kaydın bu duruma geldiği an (onaya gönderilme / oluşturulma).</param>
public sealed record WorkTaskItemDto(string Title, string? Subtitle, string Path, DateTime? At, string? By);

/// <param name="Type">Kaydın türü (ör. "Üretim siparişi").</param>
public sealed record SearchHitDto(string Type, string Title, string? Subtitle, string Path);

/// <param name="Count">Tanımlı kayıt sayısı.</param>
/// <param name="Done">Adım tamamlanmış sayılır mı (en az bir kayıt var).</param>
/// <param name="Optional">Üretim akışı için zorunlu olmayan adım.</param>
public sealed record SetupStepDto(
    string Key, string Group, string Title, string Description, string Path, int Count, bool Done, bool Optional);

public sealed record SetupStatusDto(int RequiredSteps, int CompletedRequiredSteps, IReadOnlyList<SetupStepDto> Steps);

public interface IWorkspaceService
{
    /// <summary>Kullanıcının yetkili olduğu, şu an bekleyen işler (bekleyeni olmayanlar listelenmez).</summary>
    Task<IReadOnlyList<WorkTaskDto>> GetTasksAsync(CancellationToken cancellationToken);

    /// <summary>Sipariş, demet, satın alma, sevkiyat, personel, ürün, malzeme ve makinelerde arama.</summary>
    Task<IReadOnlyList<SearchHitDto>> SearchAsync(string? query, CancellationToken cancellationToken);

    /// <summary>İlk kurulum adımları ve hangilerinin tamamlandığı.</summary>
    Task<SetupStatusDto> GetSetupStatusAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Web'in ana sayfası, genel araması ve kurulum rehberi için okuma sorguları. Her sonuç yalnızca kullanıcının
/// görme yetkisi olan modüllerden gelir.
/// </summary>
public sealed class WorkspaceService(
    IKonfeksiyonDbContext db, ICurrentUser currentUser, IPermissionService permissions, TimeProvider timeProvider) : IWorkspaceService
{
    private const int HitsPerType = 5;

    /// <summary>Bir işte tek tek listelenen en fazla kayıt; fazlası için işin liste ekranına gidilir.</summary>
    public const int MaxTaskItems = 8;
    private const int MinQueryLength = 2;

    public async Task<IReadOnlyList<WorkTaskDto>> GetTasksAsync(CancellationToken cancellationToken)
    {
        var granted = await permissions.GetPermissionsAsync(currentUser.Roles, cancellationToken);
        var tasks = new List<WorkTaskDto>();

        void Add(string key, string title, string description, int count, string path, IReadOnlyList<WorkTaskItemDto>? items = null)
        {
            if (count > 0)
            {
                tasks.Add(new WorkTaskDto(key, title, description, count, path, items));
            }
        }

        // Onay akışında sıra kullanıcıya (rolüne ya da kendisine) gelmiş siparişler.
        if (granted.Contains(KP.SalesOrder.View) && currentUser.UserId is { } approverId)
        {
            var roleIds = await db.UserRoles.Where(r => r.UserId == approverId).Select(r => r.RoleId).ToListAsync(cancellationToken);
            var submitted = db.SalesOrders.AsNoTracking()
                .AwaitingDecisionOf(db, approverId, roleIds, granted.Contains(KP.SalesOrder.Approve));
            Add(
                "sales-orders-submitted", "Onayınızı bekleyen müşteri siparişleri", "Onay sırası size gelmiş siparişler.",
                await submitted.CountAsync(cancellationToken),
                "/sales-orders?awaiting=me",
                await submitted
                    .OrderByDescending(o => o.SubmittedAt)
                    .Take(MaxTaskItems)
                    .Select(o => new WorkTaskItemDto(
                        o.OrderNo + " onay bekliyor", o.Customer.Name, "/sales-orders/" + o.Id, o.SubmittedAt, o.SubmittedBy))
                    .ToListAsync(cancellationToken));
        }

        if (granted.Contains(KP.ProductionOrder.View))
        {
            var drafts = db.ProductionOrders.AsNoTracking().Where(o => o.Status == ProductionOrderStatus.Draft);
            Add(
                "orders-draft", "Onay bekleyen üretim siparişleri", "Taslak siparişler onaylanınca demetlenebilir.",
                await drafts.CountAsync(cancellationToken),
                "/production-orders?status=DRAFT",
                // Tek tek bildirim, kararı verecek kişiye (onay yetkisi olana) gösterilir.
                granted.Contains(KP.ProductionOrder.Approve)
                    ? await drafts
                        .OrderByDescending(o => o.Id)
                        .Take(MaxTaskItems)
                        .Select(o => new WorkTaskItemDto(
                            o.OrderNo + " onay bekliyor", o.Customer.Name + " · " + o.Product.ProductCode,
                            "/production-orders/" + o.Id, o.CreatedAt, o.CreatedBy))
                        .ToListAsync(cancellationToken)
                    : null);

            var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
            Add(
                "orders-overdue", "Termini geçmiş siparişler", "Termin tarihi geçtiği halde tamamlanmamış siparişler.",
                await db.ProductionOrders.CountAsync(
                    o => o.DueDate < today
                        && (o.Status == ProductionOrderStatus.Released || o.Status == ProductionOrderStatus.InProduction),
                    cancellationToken),
                "/production-orders");
        }

        if (granted.Contains(KP.Bundle.Create))
        {
            Add(
                "orders-to-bundle", "Demetlenecek siparişler", "Onaylı olup demetlenmemiş adedi kalan siparişler.",
                await db.ProductionOrders.CountAsync(
                    o => (o.Status == ProductionOrderStatus.Released || o.Status == ProductionOrderStatus.InProduction)
                        && o.Items.Sum(i => i.Quantity)
                        > (db.Bundles
                            .Where(b => b.ProductionOrderId == o.Id && b.Status != BundleStatus.Cancelled)
                            .Sum(b => (int?)b.Quantity) ?? 0),
                    cancellationToken),
                "/production-orders?status=RELEASED");
        }

        if (granted.Contains(KP.Bundle.View))
        {
            Add(
                "bundles-without-card", "Kart bekleyen demetler", "Kartı atanmamış demet üretime verilemez.",
                await db.Bundles.CountAsync(b => b.Status == BundleStatus.Created, cancellationToken),
                "/bundles?status=CREATED");
            Add(
                "bundles-rework-no-card", "Tamirde kartsız demetler", "Tamir tablette işlenemez; yeni kart atayın.",
                await db.Bundles.CountAsync(
                    b => b.Status == BundleStatus.Rework && !db.BundleCardAssignments.Any(a => a.BundleId == b.Id && a.ReleasedAt == null),
                    cancellationToken),
                "/bundles?status=REWORK");
        }

        if (granted.Contains(KP.Quality.View))
        {
            Add(
                "bundles-quality-hold", "Kalite bekletmesindeki demetler", "Kalite serbest bırakana kadar ilerlemez.",
                await db.Bundles.CountAsync(b => b.Status == BundleStatus.QualityHold, cancellationToken),
                "/quality");
        }

        if (granted.Contains(KP.Stock.View))
        {
            Add(
                "bundles-to-receive", "Depoya alınacak demetler", "Tüm operasyonları bitmiş, mamul deposuna alınmayı bekliyor.",
                await db.Bundles.CountAsync(b => b.Status == BundleStatus.Completed, cancellationToken),
                "/production-receipts");
        }

        if (granted.Contains(KP.Purchase.View))
        {
            var purchaseDrafts = db.PurchaseOrders.AsNoTracking().Where(o => o.Status == PurchaseOrderStatus.Draft);
            Add(
                "purchases-draft", "Onay bekleyen satın alma siparişleri", "Taslak siparişe mal kabul yapılamaz.",
                await purchaseDrafts.CountAsync(cancellationToken),
                "/purchase-orders?status=DRAFT",
                granted.Contains(KP.Purchase.Approve)
                    ? await purchaseDrafts
                        .OrderByDescending(o => o.Id)
                        .Take(MaxTaskItems)
                        .Select(o => new WorkTaskItemDto(
                            o.OrderNo + " onay bekliyor", o.Supplier.Name, "/purchase-orders/" + o.Id, o.CreatedAt, o.CreatedBy))
                        .ToListAsync(cancellationToken)
                    : null);
            Add(
                "purchases-to-receive", "Mal kabul bekleyen siparişler", "Onaylı, henüz tamamı teslim alınmamış siparişler.",
                await db.PurchaseOrders.CountAsync(
                    o => o.Status == PurchaseOrderStatus.Approved || o.Status == PurchaseOrderStatus.PartiallyReceived,
                    cancellationToken),
                "/purchase-orders?receivable=true");
        }

        if (granted.Contains(KP.Shipment.View))
        {
            Add(
                "shipments-draft", "Sevk bekleyen sevkiyatlar", "Taslak sevkiyat stoğu etkilemez; sevk edilince düşer.",
                await db.Shipments.CountAsync(s => s.Status == ShipmentStatus.Draft, cancellationToken),
                "/shipments");
        }

        if (granted.Contains(KP.Machine.View))
        {
            Add(
                "machines-down", "Arızalı / bakımdaki makineler", "Bu makinelerde üretim başlatılamaz.",
                await db.Machines.CountAsync(
                    m => m.Status == MachineStatus.Breakdown || m.Status == MachineStatus.Maintenance, cancellationToken),
                "/machines");
        }

        return tasks;
    }

    public async Task<IReadOnlyList<SearchHitDto>> SearchAsync(string? query, CancellationToken cancellationToken)
    {
        var term = query?.Trim() ?? "";
        if (term.Length < MinQueryLength)
        {
            return [];
        }

        var granted = await permissions.GetPermissionsAsync(currentUser.Roles, cancellationToken);
        var hits = new List<SearchHitDto>();

        if (granted.Contains(KP.ProductionOrder.View))
        {
            hits.AddRange(await db.ProductionOrders
                .AsNoTracking()
                .Where(o => o.OrderNo.Contains(term)
                    || (o.CustomerOrderNo != null && o.CustomerOrderNo.Contains(term))
                    || o.Customer.Name.Contains(term))
                .OrderByDescending(o => o.Id)
                .Take(HitsPerType)
                .Select(o => new SearchHitDto(
                    "Üretim siparişi", o.OrderNo, o.Customer.Name + " · " + o.Product.Name, "/production-orders/" + o.Id))
                .ToListAsync(cancellationToken));
        }

        if (granted.Contains(KP.Bundle.View))
        {
            hits.AddRange(await db.Bundles
                .AsNoTracking()
                .Where(b => b.BundleNo.Contains(term)
                    || db.BundleCardAssignments.Any(a => a.BundleId == b.Id && a.ReleasedAt == null && a.RfidCard.Uid.Contains(term)))
                .OrderByDescending(b => b.Id)
                .Take(HitsPerType)
                .Select(b => new SearchHitDto(
                    "Demet", b.BundleNo,
                    b.ProductionOrder.OrderNo + " · " + b.Color.Name + " / " + b.Size.Name + " · " + b.Quantity + " adet",
                    "/bundles/" + b.Id))
                .ToListAsync(cancellationToken));
        }

        if (granted.Contains(KP.Purchase.View))
        {
            hits.AddRange(await db.PurchaseOrders
                .AsNoTracking()
                .Where(o => o.OrderNo.Contains(term) || o.Supplier.Name.Contains(term))
                .OrderByDescending(o => o.Id)
                .Take(HitsPerType)
                .Select(o => new SearchHitDto("Satın alma siparişi", o.OrderNo, o.Supplier.Name, "/purchase-orders/" + o.Id))
                .ToListAsync(cancellationToken));
        }

        if (granted.Contains(KP.Shipment.View))
        {
            hits.AddRange(await db.Shipments
                .AsNoTracking()
                .Where(s => s.ShipmentNo.Contains(term) || s.Customer.Name.Contains(term))
                .OrderByDescending(s => s.Id)
                .Take(HitsPerType)
                .Select(s => new SearchHitDto("Sevkiyat", s.ShipmentNo, s.Customer.Name, "/shipments/" + s.Id))
                .ToListAsync(cancellationToken));
        }

        if (granted.Contains(KP.Operator.View))
        {
            hits.AddRange(await db.Operators
                .AsNoTracking()
                .Where(o => o.OperatorCode.Contains(term) || (o.FirstName + " " + o.LastName).Contains(term))
                .OrderBy(o => o.OperatorCode)
                .Take(HitsPerType)
                .Select(o => new SearchHitDto(
                    "Personel", o.FirstName + " " + o.LastName, o.OperatorCode, "/operators?search=" + o.OperatorCode))
                .ToListAsync(cancellationToken));
        }

        if (granted.Contains(P.MasterData.View))
        {
            hits.AddRange(await db.Products
                .AsNoTracking()
                .Where(p => p.ProductCode.Contains(term) || p.Name.Contains(term))
                .OrderBy(p => p.ProductCode)
                .Take(HitsPerType)
                .Select(p => new SearchHitDto("Ürün", p.ProductCode, p.Name, "/products?search=" + p.ProductCode))
                .ToListAsync(cancellationToken));
        }

        if (granted.Contains(KP.Stock.View))
        {
            hits.AddRange(await db.StockItems
                .AsNoTracking()
                .Where(i => i.Code.Contains(term) || i.Name.Contains(term))
                .OrderBy(i => i.Code)
                .Take(HitsPerType)
                .Select(i => new SearchHitDto("Stok kartı", i.Code, i.Name, "/stock?search=" + i.Code))
                .ToListAsync(cancellationToken));
        }

        if (granted.Contains(KP.Machine.View))
        {
            hits.AddRange(await db.Machines
                .AsNoTracking()
                .Where(m => m.MachineCode.Contains(term) || m.MachineName.Contains(term))
                .OrderBy(m => m.MachineCode)
                .Take(HitsPerType)
                .Select(m => new SearchHitDto("Makine", m.MachineCode, m.MachineName, "/machines?search=" + m.MachineCode))
                .ToListAsync(cancellationToken));
        }

        return hits;
    }

    public async Task<SetupStatusDto> GetSetupStatusAsync(CancellationToken cancellationToken)
    {
        var payroll = await db.PayrollSettings
            .AsNoTracking()
            .Where(s => s.Id == PayrollSetting.SingletonId)
            .Select(s => s.MinuteRate)
            .SingleOrDefaultAsync(cancellationToken);
        var pricedOperations = await db.Operations.CountAsync(o => o.PieceRate != null, cancellationToken);

        (string Key, string Group, string Title, string Description, string Path, int Count, bool Optional)[] steps =
        [
            ("customers", "Ürün tanımları", "Müşteriler", "Sipariş veren firmalar.", "/customers",
                await db.Customers.CountAsync(cancellationToken), false),
            ("models", "Ürün tanımları", "Modeller", "Üretilen stiller; operasyon rotası modele bağlanır.", "/models",
                await db.Models.CountAsync(cancellationToken), false),
            ("products", "Ürün tanımları", "Ürünler", "Siparişe ve demete konu olan ürünler.", "/products",
                await db.Products.CountAsync(cancellationToken), false),
            ("colors", "Ürün tanımları", "Renkler", "Sipariş kalemlerinin renkleri.", "/colors",
                await db.Colors.CountAsync(cancellationToken), false),
            ("sizes", "Ürün tanımları", "Bedenler", "Sipariş kalemlerinin bedenleri.", "/sizes",
                await db.Sizes.CountAsync(cancellationToken), false),
            ("machine-types", "Operasyon ve makineler", "Makine tipleri", "Düz dikiş, overlok gibi makine sınıfları.",
                "/machine-types", await db.MachineTypes.CountAsync(cancellationToken), false),
            ("operations", "Operasyon ve makineler", "Operasyonlar", "Dikim operasyonları ve standart süreleri (SAM).",
                "/operations", await db.Operations.CountAsync(cancellationToken), false),
            ("routes", "Operasyon ve makineler", "Etkin operasyon rotası",
                "Modelin operasyon sırası; etkin rotası olmayan modele demet açılamaz.", "/production-routes",
                await db.ProductionRoutes.CountAsync(r => r.Status == RouteStatus.Active, cancellationToken), false),
            ("machines", "Operasyon ve makineler", "Makineler", "Makineler ve yapabildikleri operasyonlar.", "/machines",
                await db.Machines.CountAsync(cancellationToken), false),
            ("tablets", "Operasyon ve makineler", "Eşleştirilmiş tablet",
                "Makine başındaki tablet; tablet uygulamasının kurulum ekranına tablet kodu girilir.", "/tablets",
                await db.Tablets.CountAsync(t => t.DeviceKeyHash != null, cancellationToken), false),
            ("operators", "Personel ve kartlar", "Kartı atanmış personel",
                "Personel kartı ve yetkinlikleri Operatörler ekranında kaydın içinde tanımlanır.", "/operators",
                await db.OperatorCards.Where(c => c.RevokedAt == null).Select(c => c.OperatorId).Distinct().CountAsync(cancellationToken),
                false),
            ("bundle-cards", "Personel ve kartlar", "Demet kartları", "Demetlere atanacak RFID kartları.", "/rfid-cards",
                await db.RfidCards.CountAsync(c => c.CardType == RfidCardType.Bundle, cancellationToken), false),
            ("shifts", "Personel ve kartlar", "Vardiyalar", "Vardiya ve mola tanımları; verim hesabında molalar düşülür.",
                "/shifts", await db.Shifts.CountAsync(cancellationToken), true),
            ("payroll", "Personel ve kartlar", "Ücret ve prim ayarları",
                "Dakika ücreti ya da operasyon birim ücretleri; tanımlanmazsa kazanç hesaplanmaz.", "/payroll",
                (payroll > 0 ? 1 : 0) + pricedOperations, true),
            ("stock-items", "Malzeme ve satın alma", "Stok kartları", "Kumaş ve aksesuar gibi malzemeler.", "/stock-items",
                await db.StockItems.CountAsync(i => i.ItemType != StockItemType.FinishedGood, cancellationToken), true),
            ("suppliers", "Malzeme ve satın alma", "Tedarikçiler", "Malzeme satın alınan firmalar.", "/suppliers",
                await db.Suppliers.CountAsync(cancellationToken), true),
            ("recipes", "Malzeme ve satın alma", "Ürün reçeteleri",
                "Ürünün malzeme listesi; demet açılırken stoktan düşülür ve maliyete girer.", "/products",
                await db.ProductMaterials.Select(m => m.ProductId).Distinct().CountAsync(cancellationToken), true),
            ("lines", "Planlama", "Bantlar", "Üretim bantları; planlama ve bant dengeleme için.", "/production-lines",
                await db.ProductionLines.CountAsync(cancellationToken), true),
            ("orders", "İlk üretim", "Üretim siparişi", "İlk siparişi açıp onaylayın; ardından demet oluşturun.",
                "/production-orders", await db.ProductionOrders.CountAsync(cancellationToken), false),
        ];

        var result = steps
            .Select(s => new SetupStepDto(s.Key, s.Group, s.Title, s.Description, s.Path, s.Count, s.Count > 0, s.Optional))
            .ToList();
        return new SetupStatusDto(
            result.Count(s => !s.Optional), result.Count(s => !s.Optional && s.Done), result);
    }
}
