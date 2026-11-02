import { resources } from '../features/masterData/resources'
import type { ProjectKey } from '../features/system/systemApi'
import { Permissions } from '../types/permissions'

export interface NavItem {
  path: string
  label: string
  /** Tanımlıysa menü öğesi yalnızca bu yetkiye sahip kullanıcıya gösterilir. */
  permission?: string
}

export interface NavGroup {
  /** Başlıksız grup menünün en üstünde yer alır. */
  title?: string
  /**
   * Üst şeritteki modül sekmesi. Verilmezse grubun başlığıdır; başlıksız grup 'Genel' modülündedir.
   * Aynı modüldeki gruplar yan menüde alt bölüm olarak listelenir.
   */
  module?: string
  /**
   * Grubun ait olduğu proje (sektör modülü). Projeler menüsünden verisine kadar ayrıdır: yalnızca altyapı ekranları
   * (kullanıcı, kur, işlem kaydı ...) projesiz olur ve her projede görünür; verilirse grup yalnızca o proje
   * seçiliyken ve proje kurulumda devredeyken listelenir.
   */
  project?: ProjectKey
  items: NavItem[]
}

export interface NavModule {
  title: string
  groups: NavGroup[]
}

const DEFAULT_MODULE = "Genel"

export function moduleOf(group: NavGroup): string {
  return group.module ?? group.title ?? DEFAULT_MODULE
}

/** Grupları modüllere toplar; modüller ilk gruplarının sırasıyla gelir. */
export function navModules(groups: NavGroup[]): NavModule[] {
  const modules: NavModule[] = []
  for (const group of groups) {
    const title = moduleOf(group)
    const existing = modules.find((item) => item.title === title)
    if (existing) {
      existing.groups.push(group)
    } else {
      modules.push({ title, groups: [group] })
    }
  }
  return modules
}

/** Ana veri ekranlarının menü öğeleri, verilen sırayla. */
const resourceItems = (keys: string[]): NavItem[] =>
  keys.flatMap((key) => {
    const resource = resources.find((item) => item.key === key)
    return resource ? [{ path: `/${resource.key}`, label: resource.title, permission: resource.viewPermission }] : []
  })

/** Modüller tamamlandıkça buraya eklenir; henüz var olmayan ekranlar menüde yer almaz. */
export const navGroups: NavGroup[] = [
  {
    items: [{ path: '/', label: 'Ana Sayfa' }],
  },
  {
    module: 'Genel',
    project: 'Konfeksiyon',
    items: [
      { path: '/dashboard', label: 'Canlı Pano', permission: Permissions.DashboardView },
      { path: '/setup', label: 'Kurulum Rehberi', permission: Permissions.MasterDataView },
    ],
  },
  {
    title: 'Satış',
    project: 'Konfeksiyon',
    items: [
      { path: '/quotations', label: 'Teklifler', permission: Permissions.QuotationView },
      { path: '/sales-orders', label: 'Müşteri Siparişleri', permission: Permissions.SalesOrderView },
    ],
  },
  {
    title: 'Üretim',
    project: 'Konfeksiyon',
    items: [
      { path: '/production-orders', label: 'Üretim Siparişleri', permission: Permissions.ProductionOrderView },
      { path: '/cutting-plans', label: 'Kesim Planları', permission: Permissions.CuttingPlanView },
      { path: '/subcontract/sends', label: 'Fason Gönderimler', permission: Permissions.SubcontractView },
      { path: '/subcontract/returns', label: 'Fason Dönüşler', permission: Permissions.SubcontractView },
      { path: '/bundles', label: 'Demetler', permission: Permissions.BundleView },
      { path: '/production-tracking', label: 'Üretim Takibi', permission: Permissions.ProductionView },
      { path: '/quality', label: 'Kalite Kontrol', permission: Permissions.QualityView },
      { path: '/work-sessions', label: 'Çalışma Oturumları', permission: Permissions.OperatorView },
      { path: '/bundles/card-release', label: 'Kart Boşaltma', permission: Permissions.BundleReleaseCard },
    ],
  },
  {
    title: 'Planlama',
    project: 'Konfeksiyon',
    items: [
      { path: '/planning', label: 'Üretim Planı', permission: Permissions.ProductionLineView },
      { path: '/line-balancing', label: 'Bant Dengeleme', permission: Permissions.ProductionLineView },
      { path: '/production-routes', label: 'Operasyon Rotaları', permission: Permissions.ProductionRouteView },
    ],
  },
  {
    title: 'Raporlama',
    project: 'Konfeksiyon',
    items: [
      { path: '/performance', label: 'Performans', permission: Permissions.ProductionView },
      { path: '/payroll', label: 'Ücret ve Prim', permission: Permissions.PayrollView },
      { path: '/reports', label: 'Raporlar', permission: Permissions.ReportView },
    ],
  },
  {
    title: 'Satın Alma',
    project: 'Konfeksiyon',
    items: [
      { path: '/purchase-orders', label: 'Satın Alma Siparişleri', permission: Permissions.PurchaseView },
      { path: '/supply-suggestions', label: 'Tedarik Önerileri', permission: Permissions.PurchaseView },
      ...resourceItems(['suppliers']),
    ],
  },
  {
    title: 'Depo',
    project: 'Konfeksiyon',
    items: [
      { path: '/stock', label: 'Stok', permission: Permissions.StockView },
      { path: '/stock-lots', label: 'Toplar ve Partiler', permission: Permissions.StockView },
      { path: '/stock-transactions', label: 'Stok Hareketleri', permission: Permissions.StockView },
      { path: '/stock-counts', label: 'Sayımlar', permission: Permissions.StockView },
      { path: '/terminal', label: 'El Terminali', permission: Permissions.StockView },
      { path: '/warehouse-reports', label: 'Depo Raporları', permission: Permissions.ReportWarehouse },
      { path: '/production-receipts', label: 'Üretimden Teslim Alma', permission: Permissions.StockView },
      { path: '/shipments', label: 'Sevkiyatlar', permission: Permissions.ShipmentView },
      { path: '/assortment-templates', label: 'Asorti Şablonları', permission: Permissions.ShipmentView },
      ...resourceItems(['warehouses', 'warehouse-locations']),
      { path: '/location-labels', label: 'Adres Etiketleri', permission: Permissions.StockView },
      ...resourceItems(['stock-items']),
    ],
  },
  {
    module: 'Genel',
    project: 'Boyahane',
    items: [
      { path: '/boyahane/dashboard', label: 'Canlı Pano', permission: Permissions.BoyahaneBatchView },
      { path: '/boyahane/setup', label: 'Kurulum Rehberi', permission: Permissions.BoyahaneView },
    ],
  },
  {
    title: 'Satış',
    project: 'Boyahane',
    items: [
      { path: '/boyahane/quotations', label: 'Teklifler', permission: Permissions.BoyahaneQuotationView },
      { path: '/boyahane/orders', label: 'Siparişler', permission: Permissions.BoyahaneOrderView },
      { path: '/boyahane/order-tracking', label: 'Sipariş Takibi', permission: Permissions.BoyahaneOrderView },
    ],
  },
  {
    title: 'Mal Kabul',
    project: 'Boyahane',
    items: [
      { path: '/boyahane/receipts', label: 'Kumaş Kabul', permission: Permissions.BoyahaneReceiptView },
      { path: '/boyahane/inspection', label: 'Ham Kontrol', permission: Permissions.BoyahaneInspectionView },
      { path: '/boyahane/yarn-lots', label: 'İplik Kabul', permission: Permissions.BoyahaneReceiptView },
      { path: '/boyahane/goods', label: 'Mal Durumu', permission: Permissions.BoyahaneReceiptView },
    ],
  },
  {
    title: 'Laboratuvar',
    project: 'Boyahane',
    items: [
      { path: '/boyahane/lab', label: 'Renk Talepleri', permission: Permissions.BoyahaneLabView },
      { path: '/boyahane/recipes', label: 'Reçeteler', permission: Permissions.BoyahaneLabView },
    ],
  },
  {
    title: 'Planlama',
    project: 'Boyahane',
    items: [
      { path: '/boyahane/planning', label: 'Makine Planı', permission: Permissions.BoyahaneBatchView },
      { path: '/boyahane/routes', label: 'Operasyon Rotaları', permission: Permissions.BoyahaneOperationView },
    ],
  },
  {
    title: 'Üretim',
    project: 'Boyahane',
    items: [
      { path: '/boyahane/batches', label: 'Partiler', permission: Permissions.BoyahaneBatchView },
      { path: '/boyahane/tracking', label: 'Üretim Takibi', permission: Permissions.BoyahaneBatchView },
      { path: '/boyahane/work-sessions', label: 'Çalışma Oturumları', permission: Permissions.BoyahaneFloorView },
      { path: '/boyahane/card-release', label: 'Kart Boşaltma', permission: Permissions.BoyahaneBatchManage },
    ],
  },
  {
    title: 'Satın Alma',
    project: 'Boyahane',
    items: [
      { path: '/boyahane/purchase-orders', label: 'Satın Alma Siparişleri', permission: Permissions.BoyahanePurchaseView },
      { path: '/boyahane/supply-suggestions', label: 'Tedarik Önerileri', permission: Permissions.BoyahanePurchaseView },
      ...resourceItems(['boyahane-suppliers']),
    ],
  },
  {
    title: 'Depo',
    project: 'Boyahane',
    items: [
      { path: '/boyahane/chemical-stock', label: 'Boya ve Kimyasal Stoğu', permission: Permissions.BoyahaneChemicalStockView },
      { path: '/boyahane/chemical-lots', label: 'Boya Lotları', permission: Permissions.BoyahaneChemicalStockView },
      { path: '/boyahane/chemical-movements', label: 'Stok Hareketleri', permission: Permissions.BoyahaneChemicalStockView },
      { path: '/boyahane/warehouse-goods', label: 'Adres Stoğu', permission: Permissions.BoyahaneChemicalStockView },
      { path: '/boyahane/location-movements', label: 'Adres Hareketleri', permission: Permissions.BoyahaneChemicalStockView },
      ...resourceItems(['boyahane-warehouses', 'boyahane-warehouse-locations']),
      { path: '/boyahane/location-labels', label: 'Adres Etiketleri', permission: Permissions.BoyahaneChemicalStockView },
    ],
  },
  {
    title: 'Sevkiyat',
    project: 'Boyahane',
    items: [
      { path: '/boyahane/packing-lists', label: 'Çeki Listeleri', permission: Permissions.BoyahanePackingView },
      { path: '/boyahane/despatches', label: 'İrsaliyeler', permission: Permissions.BoyahaneShipmentView },
      { path: '/boyahane/goods-returns', label: 'İade ve Hurda', permission: Permissions.BoyahaneShipmentView },
    ],
  },
  {
    title: 'Raporlama',
    project: 'Boyahane',
    items: [{ path: '/boyahane/reports', label: 'Raporlar', permission: Permissions.BoyahaneReportView }],
  },
  {
    title: 'Faturalama',
    module: 'Finans',
    project: 'Boyahane',
    items: [{ path: '/boyahane/invoices', label: 'Faturalar', permission: Permissions.BoyahaneInvoiceView }],
  },
  {
    title: 'Cari',
    module: 'Finans',
    project: 'Boyahane',
    items: [
      { path: '/boyahane/payments', label: 'Tahsilatlar', permission: Permissions.BoyahaneAccountView },
      { path: '/boyahane/accounts', label: 'Cari Hesaplar', permission: Permissions.BoyahaneAccountView },
      { path: '/boyahane/open-invoices', label: 'Açık Faturalar', permission: Permissions.BoyahaneAccountView },
      { path: '/boyahane/opening-balances', label: 'Açılış Bakiyeleri', permission: Permissions.BoyahaneAccountView },
    ],
  },
  {
    title: 'Müşteri ve Malzeme',
    module: 'Tanımlar',
    project: 'Boyahane',
    items: resourceItems(['boyahane-customers', 'boyahane-fabrics', 'boyahane-yarns']),
  },
  {
    title: 'Operasyon ve Kalite',
    module: 'Tanımlar',
    project: 'Boyahane',
    items: resourceItems(['boyahane-operations', 'boyahane-dye-chemicals', 'boyahane-defect-types']),
  },
  {
    title: 'Maliyet',
    module: 'Tanımlar',
    project: 'Boyahane',
    items: [{ path: '/boyahane/cost-settings', label: 'Maliyet Parametreleri', permission: Permissions.BoyahaneQuotationView }],
  },
  {
    title: 'Renk ve Desen Arşivi',
    module: 'Tanımlar',
    project: 'Boyahane',
    items: [
      { path: '/boyahane/color-cards', label: 'Renk Kartları', permission: Permissions.BoyahaneLabView },
      { path: '/boyahane/patterns', label: 'Desen Kartları', permission: Permissions.BoyahaneLabView },
      { path: '/boyahane/stripping-recipes', label: 'Söküm Reçeteleri', permission: Permissions.BoyahaneLabView },
    ],
  },
  {
    title: 'Tesis ve Personel',
    module: 'Tanımlar',
    project: 'Boyahane',
    items: resourceItems(['boyahane-machines', 'boyahane-tablets', 'boyahane-rfid-cards', 'boyahane-operators', 'boyahane-stop-reasons', 'boyahane-shifts', 'boyahane-break-groups', 'boyahane-break-schedules']),
  },
  {
    module: 'Genel',
    project: 'Dokuma',
    items: [{ path: '/dokuma/dashboard', label: 'Canlı Pano', permission: Permissions.DokumaView }],
  },
  {
    title: 'Satış',
    project: 'Dokuma',
    items: [
      { path: '/dokuma/quotations', label: 'Teklifler', permission: Permissions.DokumaQuotationView },
      { path: '/dokuma/orders', label: 'Siparişler', permission: Permissions.DokumaOrderView },
    ],
  },
  {
    title: 'Mal Kabul',
    project: 'Dokuma',
    items: [
      { path: '/dokuma/yarn-lots', label: 'İplik Girişleri', permission: Permissions.DokumaReceiptView },
      { path: '/dokuma/yarn-returns', label: 'İplik İadeleri', permission: Permissions.DokumaReceiptView },
    ],
  },
  {
    title: 'Satın Alma',
    project: 'Dokuma',
    items: [
      { path: '/dokuma/purchase-orders', label: 'Satın Alma Siparişleri', permission: Permissions.DokumaPurchaseOrderView },
      { path: '/dokuma/supply-suggestions', label: 'Tedarik Önerileri', permission: Permissions.DokumaPurchaseOrderView },
    ],
  },
  {
    title: 'Depo',
    project: 'Dokuma',
    items: [
      { path: '/dokuma/material-stock', label: 'Malzeme Stoğu', permission: Permissions.DokumaStockView },
      { path: '/dokuma/material-movements', label: 'Malzeme Hareketleri', permission: Permissions.DokumaStockView },
    ],
  },
  {
    title: 'Üretim',
    project: 'Dokuma',
    items: [
      { path: '/dokuma/warp-orders', label: 'Çözgü Emirleri', permission: Permissions.DokumaWarpView },
      { path: '/dokuma/beams', label: 'Leventler', permission: Permissions.DokumaWarpView },
      { path: '/dokuma/weaving', label: 'Tezgâh Panosu', permission: Permissions.DokumaWeavingView },
      { path: '/dokuma/rolls', label: 'Ham Toplar', permission: Permissions.DokumaWeavingView },
      { path: '/dokuma/inspection', label: 'Ham Kontrol', permission: Permissions.DokumaInspectionView },
      { path: '/dokuma/loom-stops', label: 'Tezgâh Duruşları', permission: Permissions.DokumaWeavingView },
      { path: '/dokuma/work-sessions', label: 'Çalışma Oturumları', permission: Permissions.DokumaFloorView },
    ],
  },
  {
    title: 'Planlama',
    project: 'Dokuma',
    items: [{ path: '/dokuma/capacity-plan', label: 'Kapasite Planı', permission: Permissions.DokumaPlanningView }],
  },
  {
    title: 'Sevkiyat',
    module: 'Satış',
    project: 'Dokuma',
    items: [
      { path: '/dokuma/packing-lists', label: 'Çeki Listeleri', permission: Permissions.DokumaPackingView },
      { path: '/dokuma/despatches', label: 'İrsaliyeler', permission: Permissions.DokumaShipmentView },
    ],
  },
  {
    title: 'Fatura',
    module: 'Finans',
    project: 'Dokuma',
    items: [{ path: '/dokuma/invoices', label: 'Faturalar', permission: Permissions.DokumaInvoiceView }],
  },
  {
    title: 'Cari',
    module: 'Finans',
    project: 'Dokuma',
    items: [
      { path: '/dokuma/payments', label: 'Tahsilatlar', permission: Permissions.DokumaAccountView },
      { path: '/dokuma/accounts', label: 'Cari Hesaplar', permission: Permissions.DokumaAccountView },
      { path: '/dokuma/open-invoices', label: 'Açık Faturalar', permission: Permissions.DokumaAccountView },
      { path: '/dokuma/opening-balances', label: 'Açılış Bakiyeleri', permission: Permissions.DokumaAccountView },
    ],
  },
  {
    title: 'Raporlama',
    project: 'Dokuma',
    items: [{ path: '/dokuma/reports', label: 'Raporlar', permission: Permissions.DokumaReportView }],
  },
  {
    module: 'Genel',
    project: 'Orme',
    items: [{ path: '/orme/dashboard', label: 'Canlı Pano', permission: Permissions.OrmeView }],
  },
  {
    title: 'Satış',
    project: 'Orme',
    items: [
      { path: '/orme/quotations', label: 'Teklifler', permission: Permissions.OrmeQuotationView },
      { path: '/orme/orders', label: 'Siparişler', permission: Permissions.OrmeOrderView },
    ],
  },
  {
    title: 'Mal Kabul',
    project: 'Orme',
    items: [
      { path: '/orme/yarn-lots', label: 'İplik Girişleri', permission: Permissions.OrmeReceiptView },
      { path: '/orme/yarn-returns', label: 'İplik İadeleri', permission: Permissions.OrmeReceiptView },
    ],
  },
  {
    title: 'Üretim',
    project: 'Orme',
    items: [
      { path: '/orme/knit-orders', label: 'Örme Emirleri', permission: Permissions.OrmeKnittingView },
      { path: '/orme/machine-board', label: 'Makine Panosu', permission: Permissions.OrmeKnittingView },
      { path: '/orme/rolls', label: 'Ham Toplar', permission: Permissions.OrmeKnittingView },
      { path: '/orme/inspection', label: 'Ham Kontrol', permission: Permissions.OrmeInspectionView },
      { path: '/orme/machine-stops', label: 'Makine Duruşları', permission: Permissions.OrmeKnittingView },
      { path: '/orme/work-sessions', label: 'Çalışma Oturumları', permission: Permissions.OrmeFloorView },
    ],
  },
  {
    title: 'Planlama',
    project: 'Orme',
    items: [{ path: '/orme/capacity-plan', label: 'Kapasite Planı', permission: Permissions.OrmePlanningView }],
  },
  {
    title: 'Satın Alma',
    project: 'Orme',
    items: [
      { path: '/orme/purchase-orders', label: 'Satın Alma Siparişleri', permission: Permissions.OrmePurchaseView },
      { path: '/orme/yarn-plan', label: 'İplik İhtiyaç Planı', permission: Permissions.OrmePurchaseView },
    ],
  },
  {
    title: 'Sevkiyat',
    module: 'Satış',
    project: 'Orme',
    items: [
      { path: '/orme/packing-lists', label: 'Çeki Listeleri', permission: Permissions.OrmePackingView },
      { path: '/orme/despatches', label: 'İrsaliyeler', permission: Permissions.OrmeShipmentView },
    ],
  },
  {
    title: 'Fatura',
    module: 'Finans',
    project: 'Orme',
    items: [{ path: '/orme/invoices', label: 'Faturalar', permission: Permissions.OrmeInvoiceView }],
  },
  {
    title: 'Cari',
    module: 'Finans',
    project: 'Orme',
    items: [
      { path: '/orme/payments', label: 'Tahsilatlar', permission: Permissions.OrmeAccountView },
      { path: '/orme/accounts', label: 'Cari Hesaplar', permission: Permissions.OrmeAccountView },
      { path: '/orme/open-invoices', label: 'Açık Faturalar', permission: Permissions.OrmeAccountView },
      { path: '/orme/opening-balances', label: 'Açılış Bakiyeleri', permission: Permissions.OrmeAccountView },
    ],
  },
  {
    title: 'Raporlama',
    project: 'Orme',
    items: [{ path: '/orme/reports', label: 'Raporlar', permission: Permissions.OrmeReportView }],
  },
  {
    title: 'Müşteri ve Tedarikçi',
    module: 'Tanımlar',
    project: 'Orme',
    items: resourceItems(['orme-customers', 'orme-suppliers']),
  },
  {
    title: 'İplik ve Kumaş',
    module: 'Tanımlar',
    project: 'Orme',
    items: resourceItems(['orme-yarns', 'orme-fabrics', 'orme-defect-types']),
  },
  {
    title: 'Tesis',
    module: 'Tanımlar',
    project: 'Orme',
    items: resourceItems(['orme-machines', 'orme-tablets', 'orme-rfid-cards', 'orme-operators', 'orme-stop-reasons', 'orme-shifts', 'orme-break-groups', 'orme-break-schedules']),
  },
  {
    title: 'Maliyet',
    module: 'Tanımlar',
    project: 'Orme',
    items: [{ path: '/orme/cost-parameters', label: 'Maliyet Parametreleri', permission: Permissions.OrmeQuotationView }],
  },
  {
    title: 'Müşteri ve Tedarikçi',
    module: 'Tanımlar',
    project: 'Dokuma',
    items: resourceItems(['dokuma-customers', 'dokuma-suppliers']),
  },
  {
    title: 'İplik ve Artikel',
    module: 'Tanımlar',
    project: 'Dokuma',
    items: [
      ...resourceItems(['dokuma-yarns', 'dokuma-articles', 'dokuma-materials', 'dokuma-defect-types']),
      { path: '/dokuma/sizing-recipes', label: 'Haşıl Reçeteleri', permission: Permissions.DokumaMaterialView },
    ],
  },
  {
    title: 'Tesis',
    module: 'Tanımlar',
    project: 'Dokuma',
    items: resourceItems(['dokuma-looms', 'dokuma-tablets', 'dokuma-rfid-cards', 'dokuma-operators', 'dokuma-stop-reasons', 'dokuma-shifts', 'dokuma-break-groups', 'dokuma-break-schedules']),
  },
  {
    title: 'Maliyet',
    module: 'Tanımlar',
    project: 'Dokuma',
    items: [{ path: '/dokuma/cost-settings', label: 'Maliyet Parametreleri', permission: Permissions.DokumaQuotationView }],
  },
  {
    title: 'Finans',
    items: [
      { path: '/exchange-rates', label: 'Döviz Kurları', permission: Permissions.PricingView },
      ...resourceItems(['currencies']),
    ],
  },
  {
    title: 'Fatura',
    module: 'Finans',
    project: 'Konfeksiyon',
    items: [{ path: '/invoices', label: 'Faturalar', permission: Permissions.InvoiceView }],
  },
  {
    title: 'Cari',
    module: 'Finans',
    project: 'Konfeksiyon',
    items: [
      { path: '/payments', label: 'Tahsilatlar', permission: Permissions.AccountView },
      { path: '/accounts', label: 'Cari Hesaplar', permission: Permissions.AccountView },
      { path: '/open-invoices', label: 'Açık Faturalar', permission: Permissions.AccountView },
      { path: '/opening-balances', label: 'Açılış Bakiyeleri', permission: Permissions.AccountView },
    ],
  },
  {
    title: 'Fiyatlandırma',
    module: 'Finans',
    project: 'Konfeksiyon',
    items: [
      { path: '/price-lists', label: 'Fiyat Listeleri', permission: Permissions.PricingView },
      { path: '/customer-discounts', label: 'Müşteri İskontoları', permission: Permissions.PricingView },
    ],
  },
  {
    title: 'Müşteri ve Ürün',
    module: 'Tanımlar',
    project: 'Konfeksiyon',
    items: resourceItems(['customers', 'models', 'products', 'product-groups', 'colors', 'sizes']),
  },
  {
    title: 'Operasyon ve Kalite',
    module: 'Tanımlar',
    project: 'Konfeksiyon',
    items: resourceItems(['operations', 'defect-types']),
  },
  {
    title: 'Fason ve Maliyet',
    module: 'Tanımlar',
    project: 'Konfeksiyon',
    items: [
      { path: '/subcontract/prices', label: 'Fason Fiyatları', permission: Permissions.SubcontractView },
      { path: '/cost-settings', label: 'Ön Maliyet Parametreleri', permission: Permissions.QuotationView },
    ],
  },
  {
    title: 'Tesis ve Makineler',
    module: 'Tanımlar',
    project: 'Konfeksiyon',
    items: resourceItems([
      'factories', 'production-areas', 'production-lines', 'machine-types', 'machines', 'tablets', 'rfid-cards',
    ]),
  },
  {
    title: 'Personel ve Vardiya',
    module: 'Tanımlar',
    project: 'Konfeksiyon',
    items: resourceItems(['operators', 'shifts', 'break-groups', 'break-schedules', 'stop-reasons']),
  },
  {
    title: 'Yönetim',
    items: [
      { path: '/users', label: 'Kullanıcılar', permission: Permissions.UserView },
      { path: '/roles', label: 'Roller ve Yetkiler', permission: Permissions.RoleView },
      { path: '/audit-logs', label: 'İşlem Kayıtları', permission: Permissions.AuditLogView },
      { path: '/integrations', label: 'Entegrasyonlar', permission: Permissions.IntegrationView },
    ],
  },
  {
    title: 'Sipariş Onayı',
    module: 'Yönetim',
    project: 'Konfeksiyon',
    items: [{ path: '/approval-flows', label: 'Onay Akışları', permission: Permissions.RoleView }],
  },
]

/**
 * Seçili projenin menüsü: çekirdek grupları ve yalnızca o projenin grupları. Proje seçili değilse (kurulumda
 * hiçbir modül devrede değil) yalnızca çekirdek kalır.
 */
export function projectNavGroups(groups: NavGroup[], project: ProjectKey | null): NavGroup[] {
  return groups.filter((group) => group.project === undefined || group.project === project)
}

/** Adres başka bir projenin ekranı mı (seçili projede açık kalmaması için)? Ayrıntı sayfaları listesine bağlanır. */
export function isForeignPath(groups: NavGroup[], pathname: string, project: ProjectKey): boolean {
  let best: { project?: ProjectKey; length: number } | null = null
  for (const group of groups) {
    for (const item of group.items) {
      const matches = item.path !== '/' && (pathname === item.path || pathname.startsWith(`${item.path}/`))
      if (matches && (best === null || item.path.length > best.length)) {
        best = { project: group.project, length: item.path.length }
      }
    }
  }
  return best?.project !== undefined && best.project !== project
}

function visibleNavItems(items: NavItem[], permissions: readonly string[]): NavItem[] {
  return items.filter((item) => !item.permission || permissions.includes(item.permission))
}

/** Kullanıcının görebileceği öğeleri bırakır; tamamen boşalan grupları atar. */
export function visibleNavGroups(groups: NavGroup[], permissions: readonly string[]): NavGroup[] {
  return groups
    .map((group) => ({ ...group, items: visibleNavItems(group.items, permissions) }))
    .filter((group) => group.items.length > 0)
}

export interface NavTrail {
  /** Üst şeritteki modül. */
  module: string
  /** Menü grubunun başlığı; başlıksız gruptaki ekranlarda yoktur. */
  group?: string
  label: string
}

/**
 * Adresin ait olduğu menü öğesi (üst çubuktaki konum bilgisi için). Ayrıntı sayfaları (ör. /shipments/12)
 * listesinin öğesine bağlanır; birden fazla öğe uyuyorsa en uzun yol kazanır.
 */
export function findNavTrail(groups: NavGroup[], pathname: string): NavTrail | null {
  let best: { trail: NavTrail; length: number } | null = null

  for (const group of groups) {
    for (const item of group.items) {
      const matches = item.path === '/' ? pathname === '/' : pathname === item.path || pathname.startsWith(`${item.path}/`)
      if (matches && (best === null || item.path.length > best.length)) {
        best = { trail: { module: moduleOf(group), group: group.title, label: item.label }, length: item.path.length }
      }
    }
  }

  return best?.trail ?? null
}

/** Aramada büyük/küçük harf ve Türkçe karakter farkı gözetilmez ("uretim" → "Üretim", "isle" → "İşle"). */
export function normalizeSearch(text: string): string {
  return text
    .toLocaleLowerCase('tr-TR')
    .replace(/ı/g, 'i')
    .replace(/ş/g, 's')
    .replace(/ğ/g, 'g')
    .replace(/ü/g, 'u')
    .replace(/ö/g, 'o')
    .replace(/ç/g, 'c')
    .trim()
}

/**
 * Menü araması: yazılan her sözcük ekranın adında ya da grubunun adında geçmelidir. Grup adı tümüyle
 * eşleşiyorsa (ör. "depo") o grubun bütün ekranları gelir. Eşleşmesi olmayan gruplar atılır.
 */
export function searchNav(groups: NavGroup[], query: string): NavGroup[] {
  const words = normalizeSearch(query).split(/\s+/).filter(Boolean)
  if (words.length === 0) {
    return groups
  }

  return groups
    .map((group) => {
      const groupText = normalizeSearch(group.title ?? '')
      return {
        ...group,
        items: group.items.filter((item) => {
          const text = `${normalizeSearch(item.label)} ${groupText}`
          return words.every((word) => text.includes(word))
        }),
      }
    })
    .filter((group) => group.items.length > 0)
}
