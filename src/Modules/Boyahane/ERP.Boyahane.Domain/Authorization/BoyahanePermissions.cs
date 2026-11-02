using ERP.Core.Domain.Authorization;

namespace ERP.Boyahane.Domain.Authorization;

/// <summary>
/// Boyahane modülünün permission'ları. Id'ler seed verisinde kullanıldığı için SABİTTİR; Boyahane 1001-1999
/// aralığını kullanır. Yeni permission eklerken sıradaki Id verilir, mevcut Id'ler değiştirilmez.
/// </summary>
public static class BoyahanePermissions
{
    public static class Module
    {
        public const string View = "Boyahane.View";
    }

    public static class Customer
    {
        public const string View = "Boyahane.Customer.View";
        public const string Manage = "Boyahane.Customer.Manage";
    }

    public static class Material
    {
        public const string View = "Boyahane.Material.View";
        public const string Manage = "Boyahane.Material.Manage";
    }

    public static class Receipt
    {
        public const string View = "Boyahane.Receipt.View";
        public const string Manage = "Boyahane.Receipt.Manage";
    }

    public static class Order
    {
        public const string View = "Boyahane.Order.View";
        public const string Manage = "Boyahane.Order.Manage";
    }

    public static class Operation
    {
        public const string View = "Boyahane.Operation.View";
        public const string Manage = "Boyahane.Operation.Manage";
    }

    public static class Packing
    {
        public const string View = "Boyahane.Packing.View";
        public const string Manage = "Boyahane.Packing.Manage";
    }

    public static class Shipment
    {
        public const string View = "Boyahane.Shipment.View";
        public const string Manage = "Boyahane.Shipment.Manage";
    }

    public static class Invoice
    {
        public const string View = "Boyahane.Invoice.View";
        public const string Manage = "Boyahane.Invoice.Manage";
    }

    public static class ChemicalStock
    {
        public const string View = "Boyahane.ChemicalStock.View";
        public const string Manage = "Boyahane.ChemicalStock.Manage";
    }

    public static class Purchase
    {
        public const string View = "Boyahane.Purchase.View";
        public const string Manage = "Boyahane.Purchase.Manage";
    }

    public static class Account
    {
        public const string View = "Boyahane.Account.View";
        public const string Manage = "Boyahane.Account.Manage";
    }

    public static class Report
    {
        public const string View = "Boyahane.Report.View";
    }

    public static class Floor
    {
        public const string View = "Boyahane.Floor.View";
        public const string Manage = "Boyahane.Floor.Manage";
    }

    public static class Batch
    {
        public const string View = "Boyahane.Batch.View";
        public const string Manage = "Boyahane.Batch.Manage";

        /// <summary>Makine kapasitesini aşan parti açmak; parti açmaktan ayrıdır.</summary>
        public const string CapacityOverride = "Boyahane.Batch.CapacityOverride";
        public const string FinalQuality = "Boyahane.Batch.FinalQuality";
        public const string FinalOverride = "Boyahane.Batch.FinalOverride";

        /// <summary>Arızalı makineyi ofisten açmak; makine durumunu değiştirmekten ayrıdır.</summary>
        public const string ResolveBreakdown = "Boyahane.Batch.ResolveBreakdown";
    }

    public static class Lab
    {
        public const string View = "Boyahane.Lab.View";
        public const string Manage = "Boyahane.Lab.Manage";

        /// <summary>Denemeyle ilgili müşteri kararını girmek ve önceki reçeteyi kullanmak; denemeyi yapmaktan ayrıdır.</summary>
        public const string CustomerDecision = "Boyahane.Lab.CustomerDecision";
    }

    public static class Inspection
    {
        public const string View = "Boyahane.Inspection.View";
        public const string Manage = "Boyahane.Inspection.Manage";

        /// <summary>Şartlı top için müşteri kararını girmek; kontrol yetkisinden ayrıdır.</summary>
        public const string CustomerDecision = "Boyahane.Inspection.CustomerDecision";
    }

    public static class Quotation
    {
        /// <summary>Teklifleri, maliyet dökümünü ve maliyet parametrelerini görmek.</summary>
        public const string View = "Boyahane.Quotation.View";

        /// <summary>Teklif hazırlamak, göndermek, kabul / ret kaydetmek ve maliyet parametrelerini değiştirmek.</summary>
        public const string Manage = "Boyahane.Quotation.Manage";
    }

    public static readonly IReadOnlyList<PermissionDefinition> All =
    [
        new(1001, Module.View, "Boyahane", "Boyahane ekranlarını görüntüleme"),
        new(1002, Customer.View, "Boyahane", "Boyahane müşterilerini görüntüleme"),
        new(1003, Customer.Manage, "Boyahane", "Boyahane müşterilerini oluşturma ve düzenleme"),
        new(1004, Material.View, "Boyahane", "Kumaş ve iplik kartlarını görüntüleme"),
        new(1005, Material.Manage, "Boyahane", "Kumaş ve iplik kartlarını oluşturma ve düzenleme"),
        new(1006, Receipt.View, "Boyahane", "Kumaş kabul belgelerini görüntüleme"),
        new(1007, Receipt.Manage, "Boyahane", "Kumaş kabul belgelerini oluşturma ve silme"),
        new(1008, Inspection.View, "Boyahane", "Ham kontrolü görüntüleme"),
        new(1009, Inspection.Manage, "Boyahane", "Ham kontrol yapma, hata türlerini yönetme"),
        new(1010, Inspection.CustomerDecision, "Boyahane", "Şartlı top için müşteri kararını girme"),
        new(1011, Order.View, "Boyahane", "Boyahane siparişlerini görüntüleme"),
        new(1012, Order.Manage, "Boyahane", "Boyahane siparişlerini oluşturma, onaylama ve iptal etme"),
        new(1013, Lab.View, "Boyahane", "Laboratuvarı görüntüleme"),
        new(1014, Lab.Manage, "Boyahane", "Laboratuvar talebi ve denemeleri, boya / kimyasal kartları"),
        new(1015, Lab.CustomerDecision, "Boyahane", "Lab denemesi için müşteri kararını girme, önceki reçeteyi kullanma"),
        new(1016, Batch.View, "Boyahane", "Partileri ve makineleri görüntüleme"),
        new(1017, Batch.Manage, "Boyahane", "Parti açma ve yürütme, makine kartları"),
        new(1018, Batch.CapacityOverride, "Boyahane", "Makine kapasitesini aşan parti açma"),
        new(1019, Floor.View, "Boyahane", "Personeli ve tabletleri görüntüleme"),
        new(1020, Floor.Manage, "Boyahane", "Personel ve tablet tanımları, cihaz sıfırlama"),
        new(1021, Batch.ResolveBreakdown, "Boyahane", "Makine arızasını ofisten kapatma"),
        new(1022, Operation.View, "Boyahane", "Operasyonları ve rotaları görüntüleme"),
        new(1023, Operation.Manage, "Boyahane", "Operasyon ve rota tanımları"),
        new(1024, Report.View, "Boyahane", "Boyahane raporları"),
        new(1025, Packing.View, "Boyahane", "Çeki listelerini görüntüleme"),
        new(1026, Packing.Manage, "Boyahane", "Çeki listesi hazırlama ve onaylama"),
        new(1027, Shipment.View, "Boyahane", "İrsaliyeleri görüntüleme"),
        new(1028, Shipment.Manage, "Boyahane", "İrsaliye hazırlama ve onaylama"),
        new(1029, Invoice.View, "Boyahane", "Faturaları görüntüleme"),
        new(1030, Invoice.Manage, "Boyahane", "Fatura hazırlama ve onaylama"),
        new(1031, ChemicalStock.View, "Boyahane", "Depoyu, adresleri ve boya-kimyasal stoğunu görüntüleme"),
        new(1032, ChemicalStock.Manage, "Boyahane", "Depo ve adres tanımları, mal taşıma, boya-kimyasal girişi ve sayım düzeltmesi"),
        new(1033, Purchase.View, "Boyahane", "Satın alma siparişlerini ve tedarikçileri görüntüleme"),
        new(1034, Purchase.Manage, "Boyahane", "Satın alma siparişi, teslim alma, tedarikçi kartları"),
        new(1035, Account.View, "Boyahane", "Cari hesapları, ekstreyi ve tahsilatları görüntüleme"),
        new(1036, Account.Manage, "Boyahane", "Tahsilat girişi, faturalara dağıtım ve iptal"),
        new(1037, Batch.FinalQuality, "Boyahane", "Final kalite kararını ofisten girme"),
        new(1038, Batch.FinalOverride, "Boyahane", "Çıkış ölçüsü sınırını gerekçeyle aşma"),
        new(1039, Quotation.View, "Boyahane", "Fiyat tekliflerini, ön maliyeti ve maliyet parametrelerini görüntüleme"),
        new(1040, Quotation.Manage, "Boyahane", "Teklif hazırlama, gönderme, kabul / ret ve maliyet parametreleri"),
    ];
}
