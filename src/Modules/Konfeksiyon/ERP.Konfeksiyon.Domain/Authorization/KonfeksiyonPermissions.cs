using ERP.Core.Domain.Authorization;

namespace ERP.Konfeksiyon.Domain.Authorization;

/// <summary>
/// Konfeksiyon modülünün permission'ları. Id'ler seed verisinde kullanıldığı için SABİTTİR: yeni
/// permission eklerken sıradaki Id verilir (Konfeksiyon 1-999 aralığını kullanır; çekirdekle ortak
/// eski Id'ler korunmuştur), mevcut Id'ler değiştirilmez.
/// </summary>
public static class KonfeksiyonPermissions
{
    public static class ProductionOrder
    {
        public const string View = "ProductionOrder.View";
        public const string Create = "ProductionOrder.Create";
        public const string Update = "ProductionOrder.Update";

        /// <summary>Taslak üretim siparişini onaylayıp üretime açma.</summary>
        public const string Approve = "ProductionOrder.Approve";

        /// <summary>Siparişi iptal etme (üretimdeki sipariş dahil: yarı mamul ve malzeme etkilenir).</summary>
        public const string Cancel = "ProductionOrder.Cancel";

        public const string Complete = "ProductionOrder.Complete";
    }

    public static class ProductionLine
    {
        public const string View = "ProductionLine.View";
        public const string Update = "ProductionLine.Update";
    }

    public static class LinePlan
    {
        /// <summary>Bant planı oluşturma, değiştirme ve iptal etme (bant ve makine yerleşiminden ayrı).</summary>
        public const string Manage = "LinePlan.Manage";
    }

    public static class Bundle
    {
        public const string View = "Bundle.View";
        public const string Create = "Bundle.Create";
        public const string Start = "Bundle.Start";
        public const string Complete = "Bundle.Complete";

        /// <summary>Kart boşaltma: okutulan demet kartının demetle bağını kaldırma.</summary>
        public const string ReleaseCard = "Bundle.ReleaseCard";

        /// <summary>Demeti iptal etme: adet siparişte yeniden demetlenebilir, malzeme geri alınır.</summary>
        public const string Cancel = "Bundle.Cancel";
    }

    public static class CuttingPlan
    {
        public const string View = "CuttingPlan.View";

        /// <summary>Kesim planı (pastal) oluşturma / değiştirme / iptal ve kesim kaydı (demet açar, kumaş düşer).</summary>
        public const string Manage = "CuttingPlan.Manage";
    }

    public static class Production
    {
        public const string View = "Production.View";
        public const string Report = "Production.Report";

        /// <summary>
        /// Ofisten, tablet ve kart olmadan operasyon ilerletme: askıda kalan işin ya da arızalı tabletteki işin adedini
        /// operatör ve makine adına bildirme.
        /// </summary>
        public const string ManualReport = "Production.ManualReport";

        /// <summary>İşlemde kalmış operasyonu ofisten adet bildirmeden bırakma.</summary>
        public const string ReleaseJob = "Production.ReleaseJob";
    }

    public static class WorkSession
    {
        /// <summary>Operatörün çalışma oturumunu ofisten kapatma.</summary>
        public const string Close = "WorkSession.Close";
    }

    public static class Quality
    {
        public const string View = "Quality.View";
        public const string Create = "Quality.Create";

        /// <summary>Kalite bekletmesini kaldırma: demet üretime döner.</summary>
        public const string ReleaseHold = "Quality.ReleaseHold";

        /// <summary>Kontrolde parçayı hatalı (hurda) ayırma.</summary>
        public const string Reject = "Quality.Reject";

        public const string ManageDefectTypes = "Quality.ManageDefectTypes";
    }

    public static class Machine
    {
        public const string View = "Machine.View";
        /// <summary>Makinenin çalışma durumunu değiştirme (arıza dışında).</summary>
        public const string Update = "Machine.Update";

        /// <summary>Makine tanımı: oluşturma, düzenleme (demirbaş, tedarikçi), silme.</summary>
        public const string Manage = "Machine.Manage";

        /// <summary>Arızalı makineyi ofisten kullanıma açma (tablette kartla kapatmanın ofis karşılığı).</summary>
        public const string ResolveBreakdown = "Machine.ResolveBreakdown";
    }

    public static class Operator
    {
        public const string View = "Operator.View";
        public const string Update = "Operator.Update";

        /// <summary>Saha görevi (operatör, bant şefi, bakım) ve ara / arıza hakları: tablet yetkilerini belirler.</summary>
        public const string AssignFloorRole = "Operator.AssignFloorRole";

        /// <summary>Personel kartını atama / geri alma: üretim ve prim kartın sahibine yazılır.</summary>
        public const string AssignCard = "Operator.AssignCard";

        /// <summary>Kişisel veri (fotoğraf) görüntüleme.</summary>
        public const string ViewPersonal = "Operator.ViewPersonal";
    }

    public static class Stock
    {
        public const string View = "Stock.View";
        public const string Manage = "Stock.Manage";

        /// <summary>Elle stok düzeltme ve sayım fişini kesinleştirme (fark stoğa işlenir).</summary>
        public const string Adjust = "Stock.Adjust";

        /// <summary>Stok ayırmayı elle değiştirme / bırakma (müşteri ve üretim siparişleri).</summary>
        public const string Reserve = "Stock.Reserve";

        /// <summary>Top / parti bilgisini (parti, ton, en) düzeltme.</summary>
        public const string EditLot = "Stock.EditLot";
    }

    public static class Warehouse
    {
        /// <summary>Depo ve depo adresi tanımlama / silme.</summary>
        public const string Manage = "Warehouse.Manage";
    }

    public static class Dashboard
    {
        public const string View = "Dashboard.View";
    }

    public static class Report
    {
        public const string View = "Report.View";

        /// <summary>Yalnızca depo raporlarını görüntüleme (bütün raporlar için <see cref="View"/> gerekir).</summary>
        public const string Warehouse = "Report.Warehouse";

        /// <summary>Operatör performansı ve çalışma süresi raporları.</summary>
        public const string Personnel = "Report.Personnel";

        /// <summary>Stok değeri, fark tutarı gibi parasal raporlar ve sütunlar.</summary>
        public const string Financial = "Report.Financial";

        /// <summary>Raporu dosya olarak dışa aktarma.</summary>
        public const string Export = "Report.Export";
    }

    public static class Tablet
    {
        public const string View = "Tablet.View";
        public const string Manage = "Tablet.Manage";
    }

    public static class RfidCard
    {
        public const string View = "RfidCard.View";
        public const string Manage = "RfidCard.Manage";
    }

    public static class ProductionRoute
    {
        public const string View = "ProductionRoute.View";
        public const string Manage = "ProductionRoute.Manage";

        /// <summary>Rotayı etkinleştirme: adımların SAM'i ücret ve verim hesabına girer.</summary>
        public const string Activate = "ProductionRoute.Activate";
    }

    public static class Purchase
    {
        public const string View = "Purchase.View";
        public const string Manage = "Purchase.Manage";

        /// <summary>Taslak satın alma siparişini onaylama.</summary>
        public const string Approve = "Purchase.Approve";

        /// <summary>Onaylı siparişin kalan miktarını kapatma.</summary>
        public const string Close = "Purchase.Close";

        public const string Cancel = "Purchase.Cancel";

        /// <summary>Satın alma birim fiyatlarını ve tutarlarını görme.</summary>
        public const string ViewPrices = "Purchase.ViewPrices";
    }

    public static class SalesOrder
    {
        public const string View = "SalesOrder.View";
        public const string Manage = "SalesOrder.Manage";

        /// <summary>Onaya gönderilmiş müşteri siparişini onaylama / reddetme.</summary>
        public const string Approve = "SalesOrder.Approve";

        /// <summary>
        /// Siparişi elle kapatma (kalan adet sevk edilmeyecekse). Sipariş normalde tamamı sevk edilince kendiliğinden
        /// kapanır; varsayılan olarak yalnızca sistem yöneticisindedir.
        /// </summary>
        public const string Close = "SalesOrder.Close";

        /// <summary>Siparişi iptal etme (onaylı sipariş dahil).</summary>
        public const string Cancel = "SalesOrder.Cancel";

        /// <summary>Satırda fiyat listesinin (iskonto sonrası) altında fiyat girme.</summary>
        public const string OverridePrice = "SalesOrder.OverridePrice";

        /// <summary>Satış birim fiyatlarını ve tutarlarını görme.</summary>
        public const string ViewPrices = "SalesOrder.ViewPrices";
    }

    public static class ApprovalFlow
    {
        /// <summary>Onay akışlarını (tutar eşikleri, onaycılar) düzenleme.</summary>
        public const string Manage = "ApprovalFlow.Manage";
    }

    public static class Shipment
    {
        public const string View = "Shipment.View";
        public const string Manage = "Shipment.Manage";

        /// <summary>Sevkiyatı kesinleştirme: stoktan düşer, geri alınamaz.</summary>
        public const string Ship = "Shipment.Ship";
    }

    public static class Invoice
    {
        public const string View = "Invoice.View";

        /// <summary>Fatura hazırlama, onaylama, taslağa alma ve iptal.</summary>
        public const string Manage = "Invoice.Manage";
    }

    public static class Account
    {
        /// <summary>Cari hesaplar, ekstre, açık faturalar ve tahsilatlar.</summary>
        public const string View = "Account.View";

        /// <summary>Tahsilat girişi, faturalara dağıtım, iptal ve açılış bakiyesi.</summary>
        public const string Manage = "Account.Manage";
    }

    public static class Payroll
    {
        public const string View = "Payroll.View";
        public const string Manage = "Payroll.Manage";
    }

    public static class Costing
    {
        public const string View = "Costing.View";

        /// <summary>Stok kartı birim / ortalama maliyetini değiştirme.</summary>
        public const string Manage = "Costing.Manage";
    }

    public static class Quotation
    {
        /// <summary>Teklifleri, maliyet dökümünü ve ön maliyet parametrelerini görüntüleme.</summary>
        public const string View = "Quotation.View";

        /// <summary>Teklif hazırlama / gönderme / kabul (taslak müşteri siparişi açar) ve ön maliyet parametreleri.</summary>
        public const string Manage = "Quotation.Manage";
    }

    public static class Subcontract
    {
        /// <summary>Fason gönderim ve dönüşlerini, fason fiyatlarını ve hakedişleri görüntüleme.</summary>
        public const string View = "Subcontract.View";

        /// <summary>Fason gönderim / dönüş fişi, fason fiyatı ve hakediş ödeme işareti.</summary>
        public const string Manage = "Subcontract.Manage";
    }

    /// <summary>
    /// Tabletteki saha işlemleri. Web kullanıcısını değil, personelin görevine karşılık gelen rolü
    /// (operatör, bant şefi, bakım) yetkilendirir; personel bu işlemleri kartını okutarak yapar.
    /// </summary>
    public static class Floor
    {
        public const string Pause = "Floor.Pause";
        public const string ReportBreakdown = "Floor.ReportBreakdown";
        public const string ResolveBreakdown = "Floor.ResolveBreakdown";

        /// <summary>Tablette adet bildirirken hatalı adet girme.</summary>
        public const string ReportReject = "Floor.ReportReject";
    }

    public static readonly IReadOnlyList<PermissionDefinition> All =
    [
        new(7, ProductionOrder.View, "ProductionOrder", "Üretim siparişlerini görüntüleme"),
        new(8, ProductionOrder.Create, "ProductionOrder", "Üretim siparişi oluşturma"),
        new(9, ProductionOrder.Update, "ProductionOrder", "Üretim siparişi güncelleme"),
        new(10, ProductionLine.View, "ProductionLine", "Bantları görüntüleme"),
        new(11, ProductionLine.Update, "ProductionLine", "Bant ve bant planı düzenleme"),
        new(12, Bundle.View, "Bundle", "Demetleri görüntüleme"),
        new(13, Bundle.Create, "Bundle", "Demet oluşturma"),
        new(14, Bundle.Start, "Bundle", "Demet operasyonu başlatma (kullanılmıyor: tablet yetkileri saha rollerinden yönetilir)"),
        new(15, Bundle.Complete, "Bundle", "Demet operasyonu tamamlama (kullanılmıyor: tablet yetkileri saha rollerinden yönetilir)"),
        new(16, Production.View, "Production", "Üretim hareketlerini görüntüleme"),
        new(17, Production.Report, "Production", "Üretim miktarı bildirme (kullanılmıyor: tablet yetkileri saha rollerinden yönetilir)"),
        new(18, Quality.View, "Quality", "Kalite kayıtlarını görüntüleme"),
        new(19, Quality.Create, "Quality", "Kalite kaydı oluşturma"),
        new(20, Machine.View, "Machine", "Makineleri görüntüleme"),
        new(21, Machine.Update, "Machine", "Makine güncelleme / durum değiştirme"),
        new(22, Operator.View, "Operator", "Operatörleri görüntüleme"),
        new(23, Operator.Update, "Operator", "Operatör güncelleme, oturum kapatma, süren işi bırakma"),
        new(24, Stock.View, "Stock", "Stok ve depo görüntüleme"),
        new(25, Stock.Manage, "Stock", "Stok hareketi oluşturma"),
        new(26, Dashboard.View, "Dashboard", "Canlı üretim ekranlarını görüntüleme"),
        new(27, Report.View, "Report", "Raporları görüntüleme"),
        new(29, ProductionRoute.View, "ProductionRoute", "Operasyon rotalarını görüntüleme"),
        new(30, ProductionRoute.Manage, "ProductionRoute", "Operasyon rotası oluşturma / etkinleştirme"),
        new(31, Tablet.View, "Tablet", "Tabletleri görüntüleme"),
        new(32, Tablet.Manage, "Tablet", "Tablet tanımlama / cihaz eşleştirme"),
        new(33, RfidCard.View, "RfidCard", "RFID kartlarını görüntüleme"),
        new(34, RfidCard.Manage, "RfidCard", "RFID kartı tanımlama"),
        new(35, Purchase.View, "Purchase", "Tedarikçi ve satın alma siparişlerini görüntüleme"),
        new(36, Purchase.Manage, "Purchase", "Tedarikçi ve satın alma siparişi oluşturma / onaylama"),
        new(37, Shipment.View, "Shipment", "Sevkiyatları görüntüleme"),
        new(38, Shipment.Manage, "Shipment", "Sevkiyat oluşturma / sevk etme"),
        new(41, Floor.Pause, "Floor", "Tablette kendi kartıyla ara verme / arayı onaylama"),
        new(42, Floor.ReportBreakdown, "Floor", "Tablette kendi kartıyla arıza bildirme"),
        new(43, Floor.ResolveBreakdown, "Floor", "Tablette kartını okutarak arızayı kapatma"),
        new(44, Payroll.View, "Payroll", "Parça başı ücret ve prim kazançlarını görüntüleme"),
        new(45, Payroll.Manage, "Payroll", "Ücret ve prim ayarlarını düzenleme"),
        new(46, Costing.View, "Costing", "Malzeme ve işçilik maliyetlerini görüntüleme"),
        new(47, SalesOrder.View, "SalesOrder", "Müşteri siparişlerini görüntüleme"),
        new(48, SalesOrder.Manage, "SalesOrder", "Müşteri siparişi oluşturma / onaya gönderme"),
        new(49, SalesOrder.Approve, "SalesOrder", "Müşteri siparişini onaylama / reddetme"),
        new(50, ProductionOrder.Approve, "ProductionOrder", "Üretim siparişini onaylayıp üretime açma"),
        new(51, Purchase.Approve, "Purchase", "Satın alma siparişini onaylama"),
        new(52, Report.Warehouse, "Report", "Yalnızca depo raporlarını görüntüleme"),
        new(53, SalesOrder.Close, "SalesOrder", "Müşteri siparişini elle kapatma (kalan sevk edilmeyecekse)"),
        new(56, Production.ManualReport, "Production", "Tabletsiz / kartsız elle üretim bildirimi (askıda kalan iş, arızalı tablet)"),
        new(57, Bundle.ReleaseCard, "Bundle", "Kart boşaltma: okutulan demet kartını demetten ayırma"),
        new(58, ProductionOrder.Cancel, "ProductionOrder", "Üretim siparişini iptal etme (üretimdeki dahil)"),
        new(59, ProductionOrder.Complete, "ProductionOrder", "Üretim siparişini tamamlandı olarak kapatma"),
        new(60, SalesOrder.Cancel, "SalesOrder", "Müşteri siparişini iptal etme (onaylı dahil)"),
        new(61, ProductionRoute.Activate, "ProductionRoute", "Operasyon rotasını etkinleştirme (SAM'i devreye alma)"),
        new(62, Stock.Adjust, "Stock", "Elle stok düzeltme ve sayım fişini kesinleştirme"),
        new(63, Costing.Manage, "Costing", "Stok kartı birim maliyetini değiştirme"),
        new(64, Operator.AssignFloorRole, "Operator", "Operatörün saha görevini ve ara / arıza haklarını değiştirme"),
        new(65, Operator.AssignCard, "Operator", "Personel kartını operatöre atama / geri alma"),
        new(66, Production.ReleaseJob, "Production", "Süren işi ofisten adet bildirmeden bırakma"),
        new(67, WorkSession.Close, "WorkSession", "Çalışma oturumunu ofisten kapatma"),
        new(68, Shipment.Ship, "Shipment", "Sevkiyatı kesinleştirme (stoktan düşer)"),
        new(71, Purchase.Close, "Purchase", "Satın alma siparişinin kalanını kapatma"),
        new(72, Purchase.Cancel, "Purchase", "Satın alma siparişini iptal etme"),
        new(73, Quality.ReleaseHold, "Quality", "Kalite bekletmesini kaldırma"),
        new(74, Quality.Reject, "Quality", "Kalite kontrolünde parçayı hatalı (hurda) ayırma"),
        new(75, Machine.Manage, "Machine", "Makine tanımı oluşturma / düzenleme / silme"),
        new(76, Machine.ResolveBreakdown, "Machine", "Arızalı makineyi ofisten kullanıma açma"),
        new(77, SalesOrder.OverridePrice, "SalesOrder", "Müşteri siparişinde fiyat listesinin altında fiyat girme"),
        new(78, Stock.Reserve, "Stock", "Stok ayırmayı elle değiştirme / bırakma"),
        new(79, Report.Personnel, "Report", "Operatör performansı ve çalışma süresi raporları"),
        new(80, Report.Financial, "Report", "Stok değeri ve tutar içeren raporlar / sütunlar"),
        new(81, Report.Export, "Report", "Raporu dosya olarak dışa aktarma"),
        new(82, Purchase.ViewPrices, "Purchase", "Satın alma fiyatlarını ve tutarlarını görme"),
        new(83, SalesOrder.ViewPrices, "SalesOrder", "Satış fiyatlarını ve tutarlarını görme"),
        new(84, ApprovalFlow.Manage, "ApprovalFlow", "Onay akışlarını düzenleme"),
        new(85, Bundle.Cancel, "Bundle", "Demeti iptal etme"),
        new(86, LinePlan.Manage, "LinePlan", "Bant planı oluşturma / değiştirme / iptal etme"),
        new(88, Stock.EditLot, "Stock", "Top / parti bilgisini düzeltme"),
        new(89, Warehouse.Manage, "Warehouse", "Depo ve depo adresi tanımlama"),
        new(90, Quality.ManageDefectTypes, "Quality", "Hata türü tanımlama"),
        new(91, Operator.ViewPersonal, "Operator", "Operatör fotoğrafını görme"),
        new(92, Floor.ReportReject, "Floor", "Tablette hatalı adet bildirme"),
        new(93, Invoice.View, "Invoice", "Satış faturalarını görüntüleme"),
        new(94, Invoice.Manage, "Invoice", "Satış faturası hazırlama, onaylama ve iptal"),
        new(95, Account.View, "Account", "Cari hesapları, ekstreyi ve tahsilatları görüntüleme"),
        new(96, Account.Manage, "Account", "Tahsilat girişi, faturalara dağıtım, iptal ve açılış bakiyesi"),
        new(97, CuttingPlan.View, "CuttingPlan", "Kesim planlarını görüntüleme"),
        new(98, CuttingPlan.Manage, "CuttingPlan", "Kesim planı oluşturma ve kesim kaydı"),
        new(99, Quotation.View, "Quotation", "Teklifleri ve ön maliyet dökümünü görüntüleme"),
        new(100, Quotation.Manage, "Quotation", "Teklif hazırlama, gönderme, kabul ve ön maliyet parametreleri"),
        new(101, Subcontract.View, "Subcontract", "Fason gönderim / dönüş, fason fiyatı ve hakedişleri görüntüleme"),
        new(102, Subcontract.Manage, "Subcontract", "Fason gönderim / dönüş fişi, fason fiyatı ve hakediş ödemesi"),
    ];
}
