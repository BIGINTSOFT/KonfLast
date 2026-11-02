using ERP.Core.Domain.Authorization;

namespace ERP.Orme.Domain.Authorization;

/// <summary>
/// Örme modülünün permission'ları. Id'ler seed verisinde kullanıldığı için SABİTTİR; Örme 3001-3999
/// aralığını kullanır. Yeni permission eklerken sıradaki Id verilir, mevcut Id'ler değiştirilmez.
/// </summary>
public static class OrmePermissions
{
    public static class Module
    {
        public const string View = "Orme.View";
    }

    public static class Customer
    {
        public const string View = "Orme.Customer.View";
        public const string Manage = "Orme.Customer.Manage";
    }

    public static class Material
    {
        public const string View = "Orme.Material.View";
        public const string Manage = "Orme.Material.Manage";
    }

    public static class Machine
    {
        public const string View = "Orme.Machine.View";
        public const string Manage = "Orme.Machine.Manage";
    }

    public static class Order
    {
        public const string View = "Orme.Order.View";
        public const string Manage = "Orme.Order.Manage";
    }

    public static class Receipt
    {
        public const string View = "Orme.Receipt.View";
        public const string Manage = "Orme.Receipt.Manage";
    }

    public static class Knitting
    {
        public const string View = "Orme.Knitting.View";
        public const string Manage = "Orme.Knitting.Manage";
    }

    public static class Inspection
    {
        public const string View = "Orme.Inspection.View";
        public const string Manage = "Orme.Inspection.Manage";

        /// <summary>Hesaplanan kaliteyi gerekçeyle değiştirme.</summary>
        public const string Override = "Orme.Inspection.Override";
    }

    public static class Packing
    {
        public const string View = "Orme.Packing.View";
        public const string Manage = "Orme.Packing.Manage";
    }

    public static class Shipment
    {
        public const string View = "Orme.Shipment.View";
        public const string Manage = "Orme.Shipment.Manage";
    }

    public static class Invoice
    {
        public const string View = "Orme.Invoice.View";
        public const string Manage = "Orme.Invoice.Manage";
    }

    public static class Account
    {
        public const string View = "Orme.Account.View";
        public const string Manage = "Orme.Account.Manage";
    }

    public static class Floor
    {
        public const string View = "Orme.Floor.View";
        public const string Manage = "Orme.Floor.Manage";
    }

    public static class Report
    {
        public const string View = "Orme.Report.View";
    }

    /// <summary>Tedarikçi kartları, iplik satın alma siparişleri ve iplik ihtiyaç planı.</summary>
    public static class Purchase
    {
        public const string View = "Orme.Purchase.View";
        public const string Manage = "Orme.Purchase.Manage";
    }

    /// <summary>Makine kapasite planı: emirlerin makine kuyruğu ve sırası.</summary>
    public static class Planning
    {
        public const string View = "Orme.Planning.View";
        public const string Manage = "Orme.Planning.Manage";
    }

    /// <summary>Satış teklifleri, ön maliyet ve maliyet parametreleri.</summary>
    public static class Quotation
    {
        public const string View = "Orme.Quotation.View";
        public const string Manage = "Orme.Quotation.Manage";
    }

    public static readonly IReadOnlyList<PermissionDefinition> All =
    [
        new(3001, Module.View, "Orme", "Örme ekranlarını görüntüleme"),
        new(3002, Customer.View, "Orme", "Örme müşterilerini görüntüleme"),
        new(3003, Customer.Manage, "Orme", "Örme müşterilerini oluşturma ve düzenleme"),
        new(3004, Material.View, "Orme", "İplik ve kumaş kartlarını görüntüleme"),
        new(3005, Material.Manage, "Orme", "İplik ve kumaş kartlarını oluşturma ve düzenleme"),
        new(3006, Machine.View, "Orme", "Örme makinelerini görüntüleme"),
        new(3007, Machine.Manage, "Orme", "Örme makinelerini oluşturma ve düzenleme"),
        new(3008, Purchase.View, "Orme", "Tedarikçileri, iplik satın alma siparişlerini ve iplik ihtiyaç planını görüntüleme"),
        new(3009, Purchase.Manage, "Orme", "Tedarikçi kartları, iplik satın alma siparişi ve teslim alma"),
        new(3010, Order.View, "Orme", "Örme siparişlerini ve iplik ihtiyacını görüntüleme"),
        new(3011, Order.Manage, "Orme", "Örme siparişlerini oluşturma, onaylama ve iptal etme"),
        new(3012, Receipt.View, "Orme", "İplik girişlerini görüntüleme"),
        new(3013, Receipt.Manage, "Orme", "İplik girişi yapma, düzenleme ve silme"),
        new(3014, Knitting.View, "Orme", "Örme emirlerini, makine panosunu ve ham topları görüntüleme"),
        new(3015, Knitting.Manage, "Orme", "Örme emri açma, makine atama, iplik verme ve top kaydetme"),
        new(3016, Inspection.View, "Orme", "Ham kontrolü görüntüleme"),
        new(3017, Inspection.Manage, "Orme", "Ham kontrol yapma, hata türleri ve kalite sınırları"),
        new(3018, Inspection.Override, "Orme", "Hesaplanan kaliteyi gerekçeyle değiştirme"),
        new(3019, Packing.View, "Orme", "Çeki listelerini görüntüleme"),
        new(3020, Packing.Manage, "Orme", "Çeki listesi hazırlama, onaylama ve Boyahane'ye gönderme"),
        new(3021, Shipment.View, "Orme", "İrsaliyeleri görüntüleme"),
        new(3022, Shipment.Manage, "Orme", "İrsaliye hazırlama ve onaylama"),
        new(3023, Invoice.View, "Orme", "Faturaları görüntüleme"),
        new(3024, Invoice.Manage, "Orme", "Fatura hazırlama ve onaylama"),
        new(3025, Account.View, "Orme", "Cari hesapları, ekstreyi ve tahsilatları görüntüleme"),
        new(3026, Account.Manage, "Orme", "Tahsilat girişi, faturalara dağıtım ve iptal"),
        new(3027, Floor.View, "Orme", "Örme personelini, kartları, tabletleri ve oturumları görüntüleme"),
        new(3028, Floor.Manage, "Orme", "Personel, kart, tablet ve duruş nedeni tanımlama; ofisten duruş ve oturum işlemleri"),
        new(3029, Report.View, "Orme", "Makine verimi, sipariş takibi ve kalite raporları"),
        new(3030, Planning.View, "Orme", "Makine kapasite planını görüntüleme"),
        new(3031, Planning.Manage, "Orme", "Emirleri makine kuyruğuna alma, sırasını ve makinesini değiştirme"),
        new(3032, Quotation.View, "Orme", "Teklifleri, ön maliyeti ve maliyet parametrelerini görüntüleme"),
        new(3033, Quotation.Manage, "Orme", "Teklif hazırlama, gönderme, kabul / ret ve maliyet parametrelerini düzenleme"),
    ];
}
