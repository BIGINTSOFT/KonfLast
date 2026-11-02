using P = ERP.Core.Domain.Authorization.Permissions;

namespace ERP.Core.Domain.Authorization;

/// <param name="ModuleKey">Rolün ait olduğu proje; null ise bütün projelerde geçerlidir.</param>
public sealed record RoleDefinition(int Id, string Code, string Name, string? ModuleKey = null);

/// <summary>Sistem rolleri. Id'ler seed verisinde kullanıldığı için sabittir.</summary>
public static class Roles
{
    public const string Admin = "ADMIN";
    public const string ProductionManager = "PRODUCTION_MANAGER";
    public const string ProductionPlanner = "PRODUCTION_PLANNER";
    public const string LineManager = "LINE_MANAGER";
    public const string Quality = "QUALITY";
    public const string Warehouse = "WAREHOUSE";
    public const string Maintenance = "MAINTENANCE";
    public const string Operator = "OPERATOR";
    public const string Management = "MANAGEMENT";
    public const string Purchasing = "PURCHASING";

    private const string KonfeksiyonKey = "Konfeksiyon";

    public static readonly IReadOnlyList<RoleDefinition> All =
    [
        new(1, Admin, "Sistem Yöneticisi"),
        new(2, ProductionManager, "Üretim Müdürü", KonfeksiyonKey),
        new(3, ProductionPlanner, "Üretim Planlama", KonfeksiyonKey),
        new(4, LineManager, "Bant Şefi", KonfeksiyonKey),
        new(5, Quality, "Kalite", KonfeksiyonKey),
        new(6, Warehouse, "Depo", KonfeksiyonKey),
        new(7, Maintenance, "Bakım", KonfeksiyonKey),
        new(8, Operator, "Operatör", KonfeksiyonKey),
        new(9, Management, "Üst Yönetim"),
        new(10, Purchasing, "Satın Alma", KonfeksiyonKey),
    ];

    /// <summary>
    /// Rollerin başlangıç yetkileri (ilk kurulum seed'i). Kurulumdan sonra yetkiler
    /// veritabanından yönetilir; bu liste sadece varsayılanı belirler.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> DefaultPermissions =
        new Dictionary<string, IReadOnlyList<string>>
        {
            [Admin] = P.All.Select(p => p.Code).ToList(),
            [ProductionManager] =
            [
                P.MasterData.View, P.MasterData.Manage, P.Pricing.View, P.Pricing.Manage, P.ExchangeRate.Manage,
            ],
            [ProductionPlanner] =
            [
                P.MasterData.View, P.Pricing.View, P.Pricing.Manage, P.ExchangeRate.Manage,
            ],
            [LineManager] = [],
            [Quality] = [],
            [Warehouse] = [],
            [Maintenance] = [],
            [Operator] = [],
            [Management] =
            [
                P.Pricing.View,
            ],
            [Purchasing] = [],
        };
}
