using ERP.Core.Domain.Authorization;

namespace ERP.Dokuma.Domain.Authorization;

/// <summary>
/// Dokuma modülünün permission'ları. Id'ler seed verisinde kullanıldığı için SABİTTİR; Dokuma 2001-2999
/// aralığını kullanır. Yeni permission eklerken sıradaki Id verilir, mevcut Id'ler değiştirilmez.
/// </summary>
public static class DokumaPermissions
{
    public static class Module
    {
        public const string View = "Dokuma.View";
    }

    public static class Customer
    {
        public const string View = "Dokuma.Customer.View";
        public const string Manage = "Dokuma.Customer.Manage";
    }

    public static class Material
    {
        public const string View = "Dokuma.Material.View";
        public const string Manage = "Dokuma.Material.Manage";
    }

    public static class Loom
    {
        public const string View = "Dokuma.Loom.View";
        public const string Manage = "Dokuma.Loom.Manage";
    }

    public static class Purchase
    {
        public const string View = "Dokuma.Purchase.View";
        public const string Manage = "Dokuma.Purchase.Manage";
    }

    /// <summary>İplik ve malzeme satın alma siparişleri ile tedarik önerileri (tedarikçi kartları <see cref="Purchase"/> ile).</summary>
    public static class PurchaseOrder
    {
        public const string View = "Dokuma.PurchaseOrder.View";
        public const string Manage = "Dokuma.PurchaseOrder.Manage";
    }

    /// <summary>Malzeme stoğu: lotlar, çıkış, sayım ve hareketler (malzeme kartları ve haşıl reçeteleri <see cref="Material"/> ile).</summary>
    public static class Stock
    {
        public const string View = "Dokuma.Stock.View";
        public const string Manage = "Dokuma.Stock.Manage";
    }

    public static class Order
    {
        public const string View = "Dokuma.Order.View";
        public const string Manage = "Dokuma.Order.Manage";
    }

    public static class Receipt
    {
        public const string View = "Dokuma.Receipt.View";
        public const string Manage = "Dokuma.Receipt.Manage";
    }

    public static class Warp
    {
        public const string View = "Dokuma.Warp.View";
        public const string Manage = "Dokuma.Warp.Manage";
    }

    public static class Weaving
    {
        public const string View = "Dokuma.Weaving.View";
        public const string Manage = "Dokuma.Weaving.Manage";
    }

    public static class Inspection
    {
        public const string View = "Dokuma.Inspection.View";
        public const string Manage = "Dokuma.Inspection.Manage";

        /// <summary>Hesaplanan kaliteyi gerekçeyle değiştirmek; kontrol yapmaktan ayrıdır.</summary>
        public const string Override = "Dokuma.Inspection.Override";
    }

    public static class Floor
    {
        public const string View = "Dokuma.Floor.View";
        public const string Manage = "Dokuma.Floor.Manage";
    }

    public static class Report
    {
        public const string View = "Dokuma.Report.View";
    }

    public static class Packing
    {
        public const string View = "Dokuma.Packing.View";
        public const string Manage = "Dokuma.Packing.Manage";
    }

    public static class Shipment
    {
        public const string View = "Dokuma.Shipment.View";
        public const string Manage = "Dokuma.Shipment.Manage";
    }

    public static class Invoice
    {
        public const string View = "Dokuma.Invoice.View";
        public const string Manage = "Dokuma.Invoice.Manage";
    }

    public static class Account
    {
        public const string View = "Dokuma.Account.View";
        public const string Manage = "Dokuma.Account.Manage";
    }

    /// <summary>Teklifler ve ön maliyet parametreleri.</summary>
    public static class Quotation
    {
        public const string View = "Dokuma.Quotation.View";
        public const string Manage = "Dokuma.Quotation.Manage";
    }

    /// <summary>İleriye dönük tezgâh kapasite planı: levent kuyrukları.</summary>
    public static class Planning
    {
        public const string View = "Dokuma.Planning.View";
        public const string Manage = "Dokuma.Planning.Manage";
    }

    public static readonly IReadOnlyList<PermissionDefinition> All =
    [
        new(2001, Module.View, "Dokuma", "Dokuma ekranlarını görüntüleme"),
        new(2002, Customer.View, "Dokuma", "Dokuma müşterilerini görüntüleme"),
        new(2003, Customer.Manage, "Dokuma", "Dokuma müşterilerini oluşturma ve düzenleme"),
        new(2004, Material.View, "Dokuma", "İplik ve artikel kartlarını görüntüleme"),
        new(2005, Material.Manage, "Dokuma", "İplik ve artikel kartlarını oluşturma ve düzenleme"),
        new(2006, Loom.View, "Dokuma", "Tezgâh kartlarını görüntüleme"),
        new(2007, Loom.Manage, "Dokuma", "Tezgâh kartlarını oluşturma ve düzenleme"),
        new(2008, Purchase.View, "Dokuma", "Tedarikçileri görüntüleme"),
        new(2009, Purchase.Manage, "Dokuma", "Tedarikçi kartlarını oluşturma ve düzenleme"),
        new(2010, Order.View, "Dokuma", "Dokuma siparişlerini ve iplik ihtiyacını görüntüleme"),
        new(2011, Order.Manage, "Dokuma", "Dokuma siparişlerini oluşturma, onaylama ve iptal etme"),
        new(2012, Receipt.View, "Dokuma", "İplik girişlerini görüntüleme"),
        new(2013, Receipt.Manage, "Dokuma", "İplik girişi yapma, düzenleme ve silme"),
        new(2014, Warp.View, "Dokuma", "Çözgü emirlerini ve leventleri görüntüleme"),
        new(2015, Warp.Manage, "Dokuma", "Çözgü emri, çözgüye iplik verme, levent, haşıl ve tahar / düğüm"),
        new(2016, Weaving.View, "Dokuma", "Tezgâh panosunu, dokunan leventleri ve topları görüntüleme"),
        new(2017, Weaving.Manage, "Dokuma", "Leventi tezgâha bağlama ve bitirme, atkıya iplik verme, top kesme"),
        new(2018, Inspection.View, "Dokuma", "Ham kontrolü görüntüleme"),
        new(2019, Inspection.Manage, "Dokuma", "Ham kontrol yapma, hata türleri ve kalite sınırları"),
        new(2020, Inspection.Override, "Dokuma", "Hesaplanan kaliteyi gerekçeyle değiştirme"),
        new(2021, Packing.View, "Dokuma", "Çeki listelerini görüntüleme"),
        new(2022, Packing.Manage, "Dokuma", "Çeki listesi hazırlama, onaylama ve Boyahane'ye gönderme"),
        new(2023, Shipment.View, "Dokuma", "İrsaliyeleri görüntüleme"),
        new(2024, Shipment.Manage, "Dokuma", "İrsaliye hazırlama ve onaylama"),
        new(2025, Invoice.View, "Dokuma", "Faturaları görüntüleme"),
        new(2026, Invoice.Manage, "Dokuma", "Fatura hazırlama ve onaylama"),
        new(2027, Account.View, "Dokuma", "Cari hesapları, ekstreyi ve tahsilatları görüntüleme"),
        new(2028, Account.Manage, "Dokuma", "Tahsilat girişi, faturalara dağıtım ve iptal"),
        new(2029, Floor.View, "Dokuma", "Dokuma personelini, kartları, tabletleri ve oturumları görüntüleme"),
        new(2030, Floor.Manage, "Dokuma", "Personel, kart, tablet ve duruş nedeni tanımlama; ofisten duruş ve oturum işlemleri"),
        new(2031, Report.View, "Dokuma", "Tezgâh verimi, sipariş takibi ve kalite raporları"),
        new(2032, PurchaseOrder.View, "Dokuma", "Satın alma siparişlerini ve tedarik önerilerini görüntüleme"),
        new(2033, PurchaseOrder.Manage, "Dokuma", "Satın alma siparişi hazırlama, onaylama, teslim alma ve kapatma"),
        new(2034, Stock.View, "Dokuma", "Malzeme stoğunu, lotlarını ve hareketlerini görüntüleme"),
        new(2035, Stock.Manage, "Dokuma", "Malzeme girişi, tezgâha / bölüme çıkış, sayım ve düzeltme"),
        new(2036, Quotation.View, "Dokuma", "Teklifleri, ön maliyet dökümünü ve maliyet parametrelerini görüntüleme"),
        new(2037, Quotation.Manage, "Dokuma", "Teklif hazırlama, gönderme, kabul / ret ve maliyet parametrelerini düzenleme"),
        new(2038, Planning.View, "Dokuma", "Tezgâh kapasite planını görüntüleme"),
        new(2039, Planning.Manage, "Dokuma", "Leventleri tezgâh kuyruğuna alma, sıralama ve başka tezgâha taşıma"),
    ];
}
