namespace ERP.Core.Domain.Authorization;

public sealed record PermissionDefinition(int Id, string Code, string Module, string Description);

/// <summary>
/// Çekirdek (tüm kurulumlarda olan) permission'ların kaynağı. Id'ler seed verisinde kullanıldığı için
/// SABİTTİR: yeni permission eklerken sıradaki Id verilir, mevcut Id'ler değiştirilmez.
/// </summary>
public static class Permissions
{
    public static class User
    {
        public const string View = "User.View";
        public const string Manage = "User.Manage";

        /// <summary>Kullanıcıya rol verme / rolünü değiştirme (kendine yönetici rolü verememesi için ayrıdır).</summary>
        public const string AssignRoles = "User.AssignRoles";

        public const string ResetPassword = "User.ResetPassword";
    }

    public static class Role
    {
        public const string View = "Role.View";
        public const string Manage = "Role.Manage";
    }

    public static class MasterData
    {
        public const string View = "MasterData.View";
        public const string Manage = "MasterData.Manage";
    }

    public static class AuditLog
    {
        public const string View = "AuditLog.View";
    }

    /// <summary>Fiyat listeleri, müşteri iskontoları, para birimleri ve döviz kurları.</summary>
    public static class Pricing
    {
        public const string View = "Pricing.View";
        public const string Manage = "Pricing.Manage";
    }

    public static class ExchangeRate
    {
        public const string Manage = "ExchangeRate.Manage";
    }

    public static class Integration
    {
        public const string View = "Integration.View";
        public const string Manage = "Integration.Manage";
    }

    /// <summary>Çekirdeğin yetkileri. Modüllerin yetkileri kendi kataloglarındadır (Id aralıkları ayrıdır).</summary>
    public static readonly IReadOnlyList<PermissionDefinition> All =
    [
        new(1, User.View, "User", "Kullanıcıları görüntüleme"),
        new(2, User.Manage, "User", "Kullanıcı oluşturma / güncelleme"),
        new(3, Role.View, "Role", "Rol ve yetkileri görüntüleme"),
        new(4, Role.Manage, "Role", "Rol yetkilerini düzenleme"),
        new(5, MasterData.View, "MasterData", "Ana verileri görüntüleme"),
        new(6, MasterData.Manage, "MasterData", "Ana verileri düzenleme"),
        new(28, AuditLog.View, "AuditLog", "İşlem kayıtlarını (audit log) görüntüleme"),
        new(39, Integration.View, "Integration", "Entegrasyon olaylarını ve teslimlerini görüntüleme"),
        new(40, Integration.Manage, "Integration", "Entegrasyon teslimlerini yeniden gönderme"),
        new(54, Pricing.View, "Pricing", "Fiyat listeleri, iskontolar, para birimleri ve döviz kurlarını görüntüleme"),
        new(55, Pricing.Manage, "Pricing", "Fiyat listesi, iskonto, para birimi ve döviz kuru düzenleme"),
        new(69, User.AssignRoles, "User", "Kullanıcıya rol verme / rolünü değiştirme"),
        new(70, User.ResetPassword, "User", "Kullanıcı parolasını sıfırlama"),
        new(87, ExchangeRate.Manage, "ExchangeRate", "Döviz kuru girme / silme"),
    ];
}
