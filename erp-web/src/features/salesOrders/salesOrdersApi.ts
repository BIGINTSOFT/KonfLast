import { baseApi } from '../../api/baseApi'
import type { GridParams } from '../../components/grid/useGrid'
import type { PagedResult } from '../../types/api'
import type { DocumentLanguage } from '../../utils/documentText'
import type { ProductionOrderStatus } from '../productionOrders/productionOrdersApi'

export type SalesOrderStatus = 'DRAFT' | 'SUBMITTED' | 'APPROVED' | 'CLOSED' | 'CANCELLED'

export interface SalesOrderListItem {
  id: number
  orderNo: string
  customerOrderNo: string | null
  customerId: number
  customerName: string
  orderDate: string
  dueDate: string
  status: SalesOrderStatus
  productCount: number
  totalQuantity: number
  /** Fiyatı girilmiş kalemlerin toplamı; hiçbirinde fiyat yoksa boş. */
  totalAmount: number | null
  currency: string
  /** Siparişe bağlı sevk edilmiş adet. */
  shippedQuantity: number
  /** Sevk edilmeyi bekleyen adet. */
  remainingQuantity: number
  /** Teslim tarihi geçmiş, kapanmamış ve sevk edilecek adedi kalmış. */
  isOverdue: boolean
}

export interface SalesOrderLine {
  id: number
  productId: number
  productCode: string
  productName: string
  colorId: number
  colorCode: string
  colorName: string
  sizeId: number
  sizeCode: string
  sizeName: string
  sizeSortOrder: number
  quantity: number
  unitPrice: number | null
  lineAmount: number | null
  /** Bu siparişe bağlı sevkiyatlarla müşteriye sevk edilmiş adet. */
  shippedQuantity: number
  /** Müşterinin kendi ürün (artikel) kodu; ürün kartından. */
  customerArticleCode: string | null
  /** Ürünün KDV oranı (%). */
  vatRate: number
  /** Depoda bu kaleme ayrılmış hazır mamul. */
  reservedQuantity: number
  /** Siparişten açılmış üretim siparişlerindeki adet. */
  inProductionQuantity: number
  /** Ne stoktan ne üretimden karşılanmış; planlamayı bekleyen adet. */
  toPlanQuantity: number
}

export interface SalesOrderReservationLine {
  lineId: number
  productId: number
  productCode: string
  productName: string
  colorId: number
  colorName: string
  sizeId: number
  sizeName: string
  sizeSortOrder: number
  ordered: number
  shipped: number
  reserved: number
  shippedFromReservation: number
  inProduction: number
  toPlan: number
  /** Başka siparişlere ayrılmamış stok (bu kalemin ayırması dahil). */
  available: number
  hasStockItem: boolean
}

export interface SalesOrderReservations {
  salesOrderId: number
  orderNo: string
  canEdit: boolean
  lines: SalesOrderReservationLine[]
}

export interface ProductionOrderDifference {
  productionOrderId: number
  orderNo: string
  status: ProductionOrderStatus
  productCode: string
  items: { colorName: string; sizeName: string; sizeSortOrder: number; ordered: number; produced: number; difference: number }[]
  changes: {
    at: string
    userName: string
    action: 'CREATED' | 'UPDATED' | 'DELETED'
    colorName: string
    sizeName: string
    oldQuantity: number | null
    newQuantity: number | null
  }[]
}

/** Müşteri siparişinden açılmış bir üretim siparişi. */
export interface SalesOrderProduction {
  id: number
  orderNo: string
  productId: number
  productCode: string
  status: ProductionOrderStatus
  totalQuantity: number
  /** Müşteri siparişinde bu ürün için istenen adet. */
  orderedQuantity: number
}

export interface SalesOrder {
  id: number
  orderNo: string
  customerOrderNo: string | null
  customerId: number
  customerCode: string
  customerName: string
  orderDate: string
  dueDate: string
  currency: string
  status: SalesOrderStatus
  notes: string | null
  totalQuantity: number
  totalAmount: number | null
  isEditable: boolean
  /** Onaylı ve henüz üretim siparişi açılmamış ürünü var. */
  canGenerateProduction: boolean
  /** Sunucunun izin verdiği durum geçişleri; ekrandaki işlemler buna göre gösterilir. */
  allowedTransitions: SalesOrderStatus[]
  submittedAt: string | null
  submittedBy: string | null
  decidedAt: string | null
  decidedBy: string | null
  decisionNote: string | null
  rowVersion: string
  createdAt: string
  createdBy: string | null
  lines: SalesOrderLine[]
  productionOrders: SalesOrderProduction[]
  shippedQuantity: number
  customerBuyerId: number | null
  customerBuyerName: string | null
  deliveryAddressId: number | null
  deliveryAddress: string | null
  /** Son onaya gönderimin adımları (akışsız gönderimde boş). */
  approvalSteps: SalesOrderApprovalStep[] | null
  /** Onay geçmişi: gönderim, onay, ret, revizyon; notlarıyla. */
  approvalEvents: SalesOrderApprovalEvent[] | null
  /** Oturumdaki kullanıcı şu anki onay adımında karar verebilir. */
  canDecide: boolean
  closedAt: string | null
  closedBy: string | null
  closeNote: string | null
  /** Tamamı sevk edildiği için kendiliğinden kapandı. */
  closedAutomatically: boolean
}

export type ApprovalStepStatus = 'WAITING' | 'PENDING' | 'APPROVED' | 'REJECTED' | 'CANCELLED'
export type ApprovalEventType = 'SUBMITTED' | 'APPROVED' | 'REJECTED' | 'REVISED' | 'CANCELLED'

export interface SalesOrderApprovalStep {
  sequence: number
  name: string
  approvers: string
  status: ApprovalStepStatus
  decidedAt: string | null
  decidedBy: string | null
  note: string | null
}

export interface SalesOrderApprovalEvent {
  round: number
  eventType: ApprovalEventType
  stepName: string | null
  userName: string
  fullName: string | null
  createdAt: string
  note: string | null
}

export interface SalesOrderRequest {
  customerId: number
  customerOrderNo: string | null
  orderDate: string
  dueDate: string
  notes: string | null
  currency: string | null
  lines: { productId: number; colorId: number; sizeId: number; quantity: number; unitPrice: number | null }[]
  rowVersion?: string
  customerBuyerId: number | null
  deliveryAddressId: number | null
}

/** Durum işlemleri; "revise" onaylı siparişi taslağa alır. Üretime geçiş Üretim Planı ekranından yapılır. */
export type SalesOrderAction = 'submit' | 'approve' | 'reject' | 'revise' | 'cancel' | 'close'

export type SalesOrderListParams = {
  page: number
  pageSize: number
  search?: string
  status?: SalesOrderStatus
  overdueOnly?: boolean
  awaitingMyApproval?: boolean
} & GridParams

export interface ProformaParty {
  name: string
  taxOffice: string | null
  taxNumber: string | null
  address: string | null
  phone: string | null
  email: string | null
}

export interface SalesOrderProforma {
  order: SalesOrder
  /** Satıcı: ilk aktif fabrika; tanımlı değilse boş. */
  seller: ProformaParty | null
  customer: ProformaParty
  invoiceAddress: string | null
  printedAt: string
  /** Belgenin basılacağı dil (müşteri kartından). */
  documentLanguage: DocumentLanguage
}

export interface SalesOrderCustomerDefaults {
  customerId: number
  currency: string
  requireUniqueCustomerOrderNo: boolean
  addresses: { id: number; title: string; text: string; isDefaultDelivery: boolean }[]
  buyers: { id: number; code: string; name: string }[]
  lastOrderSizes: { id: number; code: string; name: string; sortOrder: number }[]
  lastOrderNo: string | null
}

// Üretime aktarma üretim siparişi açar; onay ana sayfadaki bekleyen işleri değiştirir.
export const salesOrdersApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getSalesOrders: build.query<PagedResult<SalesOrderListItem>, SalesOrderListParams>({
      query: (params) => ({ url: '/sales-orders', params }),
      providesTags: ['SalesOrder'],
    }),
    getSalesOrder: build.query<SalesOrder, number>({
      query: (id) => `/sales-orders/${id}`,
      providesTags: ['SalesOrder', 'ProductionOrder'],
    }),
    saveSalesOrder: build.mutation<SalesOrder, { id?: number; body: SalesOrderRequest }>({
      query: ({ id, body }) => ({
        url: id === undefined ? '/sales-orders' : `/sales-orders/${id}`,
        method: id === undefined ? 'POST' : 'PUT',
        body,
      }),
      invalidatesTags: ['SalesOrder'],
    }),
    actOnSalesOrder: build.mutation<SalesOrder, { id: number; action: SalesOrderAction; rowVersion: string; note?: string | null }>({
      query: ({ id, action, rowVersion, note }) => ({
        url: `/sales-orders/${id}/${action}`,
        method: 'POST',
        body: { rowVersion, note: note ?? null },
      }),
      invalidatesTags: ['SalesOrder', 'ProductionOrder', 'Workspace'],
    }),
    getSalesOrderProforma: build.query<SalesOrderProforma, number>({
      query: (id) => `/sales-orders/${id}/proforma`,
      providesTags: ['SalesOrder'],
    }),
    getSalesOrderReservations: build.query<SalesOrderReservations, number>({
      query: (id) => `/sales-orders/${id}/reservations`,
      providesTags: ['SalesOrder', 'Stock', 'ProductionOrder'],
    }),
    setSalesOrderReservations: build.mutation<SalesOrderReservations, { id: number; lines: { lineId: number; quantity: number }[] }>({
      query: ({ id, lines }) => ({ url: `/sales-orders/${id}/reservations`, method: 'PUT', body: { lines } }),
      invalidatesTags: ['SalesOrder', 'Stock', 'Planning'],
    }),
    getProductionChanges: build.query<ProductionOrderDifference[], number>({
      query: (id) => `/sales-orders/${id}/production-changes`,
      providesTags: ['SalesOrder', 'ProductionOrder'],
    }),
    getCustomerDefaults: build.query<SalesOrderCustomerDefaults, number>({
      query: (customerId) => `/sales-orders/customer-defaults/${customerId}`,
      providesTags: ['CustomerAddress', 'CustomerBuyer', 'MasterData'],
    }),
    deleteSalesOrder: build.mutation<null, number>({
      query: (id) => ({ url: `/sales-orders/${id}`, method: 'DELETE' }),
      invalidatesTags: ['SalesOrder'],
    }),
  }),
})

export const {
  useGetSalesOrdersQuery,
  useGetSalesOrderQuery,
  useSaveSalesOrderMutation,
  useActOnSalesOrderMutation,
  useDeleteSalesOrderMutation,
  useGetCustomerDefaultsQuery,
  useGetSalesOrderProformaQuery,
  useGetSalesOrderReservationsQuery,
  useSetSalesOrderReservationsMutation,
  useGetProductionChangesQuery,
  useLazyGetCustomerDefaultsQuery,
} = salesOrdersApi
