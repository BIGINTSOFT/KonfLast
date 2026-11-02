import { baseApi } from '../../api/baseApi'
import type { PagedResult } from '../../types/api'

export type OrmePurchaseStatus = 'DRAFT' | 'APPROVED' | 'PARTIALLY_RECEIVED' | 'RECEIVED' | 'CLOSED' | 'CANCELLED'

export const ormePurchaseStatusPresentation: Record<OrmePurchaseStatus, { label: string; tone: string }> = {
  DRAFT: { label: 'Taslak', tone: 'neutral' },
  APPROVED: { label: 'Onaylı', tone: 'info' },
  PARTIALLY_RECEIVED: { label: 'Kısmi teslim', tone: 'warning' },
  RECEIVED: { label: 'Teslim alındı', tone: 'ok' },
  CLOSED: { label: 'Kapatıldı', tone: 'neutral' },
  CANCELLED: { label: 'İptal', tone: 'danger' },
}

export interface OrmePurchaseOrderListItem {
  id: number
  orderNo: string
  supplierName: string
  orderDate: string
  /** Satırların en erken termini. */
  dueDate: string | null
  lineCount: number
  orderedKilograms: number
  receivedKilograms: number
  amount: number
  currency: string
  status: OrmePurchaseStatus
}

export interface OrmePurchaseLot {
  id: number
  lotNo: string
  receiptDate: string
  despatchNo: string | null
  producerLotNo: string | null
  packageCount: number
  kilograms: number
}

export interface OrmePurchaseLine {
  id: number
  yarnId: number
  yarnCode: string
  yarnName: string
  yarnColor: string | null
  kilograms: number
  unitPrice: number | null
  amount: number | null
  dueDate: string | null
  notes: string | null
  receivedKilograms: number
  /** Sipariş − teslim; fazla teslimde sıfır. */
  openKilograms: number
  lots: OrmePurchaseLot[]
}

export interface OrmePurchaseOrder {
  id: number
  orderNo: string
  supplierId: number
  supplierCode: string
  supplierName: string
  supplierTaxNumber: string | null
  supplierPhone: string | null
  supplierEmail: string | null
  orderDate: string
  currency: string
  notes: string | null
  status: OrmePurchaseStatus
  totalKilograms: number
  receivedKilograms: number
  amount: number
  hasUnpricedLines: boolean
  createdAt: string
  createdBy: string | null
  lines: OrmePurchaseLine[]
}

export interface OrmePurchaseOrderRequest {
  supplierId: number
  orderDate: string
  currency: string | null
  notes: string | null
  lines: { yarnId: number; kilograms: number; unitPrice: number | null; dueDate: string | null; notes: string | null }[]
}

export interface OrmePurchaseReceiptRequest {
  receiptDate: string
  despatchNo: string | null
  notes: string | null
  lines: { lineId: number; kilograms: number; packageCount: number; producerLotNo: string | null }[]
}

export interface OrmeYarnPlanOrder {
  orderId: number
  orderNo: string
  customerName: string | null
  dueDate: string | null
  fabricCodes: string
  /** Açık satırların fireli ihtiyacı. */
  requiredKilograms: number
  issuedKilograms: number
  remainingKilograms: number
  /** Termin sırasıyla eldeki ve yoldaki iplikten karşılanan. */
  coveredKilograms: number
}

export interface OrmeYarnPlanRow {
  yarnId: number
  yarnCode: string
  yarnName: string
  yarnColor: string | null
  /** Fasonda ipliğin sahibi müşteri; işletme malında boş. */
  ownerCustomerId: number | null
  ownerName: string | null
  /** İpliği müşteri getirir; satın alınmaz. */
  customerSupplied: boolean
  netKilograms: number
  requiredKilograms: number
  issuedKilograms: number
  remainingKilograms: number
  onHandKilograms: number
  /** Eldeki − kalan ihtiyaç; eksi olabilir. */
  freeKilograms: number
  openPurchaseKilograms: number
  draftPurchaseKilograms: number
  shortageKilograms: number
  /** Eksik − taslak siparişler; müşteri ipliğinde sıfır. */
  suggestedKilograms: number
  neededBy: string | null
  supplierId: number | null
  supplierName: string | null
  leadTimeDays: number | null
  /** İhtiyaç tarihi − teslim süresi. */
  orderBy: string | null
  orderOverdue: boolean
  orders: OrmeYarnPlanOrder[]
}

export interface OrmeYarnPlan {
  today: string
  rows: OrmeYarnPlanRow[]
  /** İplik tanımı olmadığı için hesaba katılamayan kumaşlar. */
  missingFabrics: string[]
}

export interface OrmeCreatedPurchaseOrder {
  id: number
  orderNo: string
  supplierName: string
  lineCount: number
  kilograms: number
}

export interface OrmePurchaseFromPlanRequest {
  orderDate: string
  lines: { yarnId: number; supplierId: number; kilograms: number; dueDate: string | null }[]
}

export type OrmePurchaseAction = 'approve' | 'reopen' | 'cancel' | 'close'

const changed = ['OrmeYarnLot', 'OrmeOrder'] as const

/** Satın alma siparişleri iplik girişlerini değiştirdiği için 'OrmeYarnLot' etiketiyle tazelenir. */
export const ormePurchasingApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getOrmePurchaseOrders: build.query<PagedResult<OrmePurchaseOrderListItem>, Record<string, unknown>>({
      query: (params) => ({ url: '/orme-purchase-orders', params }),
      providesTags: ['OrmeYarnLot'],
    }),
    getOrmePurchaseOrder: build.query<OrmePurchaseOrder, number>({
      query: (id) => `/orme-purchase-orders/${id}`,
      providesTags: ['OrmeYarnLot'],
    }),
    saveOrmePurchaseOrder: build.mutation<OrmePurchaseOrder, { id?: number; body: OrmePurchaseOrderRequest }>({
      query: ({ id, body }) => ({ url: id === undefined ? '/orme-purchase-orders' : `/orme-purchase-orders/${id}`, method: id === undefined ? 'POST' : 'PUT', body }),
      invalidatesTags: changed,
    }),
    changeOrmePurchaseOrder: build.mutation<OrmePurchaseOrder, { id: number; action: OrmePurchaseAction }>({
      query: ({ id, action }) => ({ url: `/orme-purchase-orders/${id}/${action}`, method: 'POST' }),
      invalidatesTags: changed,
    }),
    deleteOrmePurchaseOrder: build.mutation<null, number>({
      query: (id) => ({ url: `/orme-purchase-orders/${id}`, method: 'DELETE' }),
      invalidatesTags: changed,
    }),
    receiveOrmePurchaseOrder: build.mutation<OrmePurchaseOrder, { id: number; body: OrmePurchaseReceiptRequest }>({
      query: ({ id, body }) => ({ url: `/orme-purchase-orders/${id}/receive`, method: 'POST', body }),
      invalidatesTags: changed,
    }),
    createOrmePurchaseOrdersFromPlan: build.mutation<OrmeCreatedPurchaseOrder[], OrmePurchaseFromPlanRequest>({
      query: (body) => ({ url: '/orme-purchase-orders/from-plan', method: 'POST', body }),
      invalidatesTags: changed,
    }),
    getOrmeYarnPlan: build.query<OrmeYarnPlan, void>({
      query: () => '/orme-yarn-plan',
      providesTags: ['OrmeOrder', 'OrmeYarnLot', 'OrmeKnitOrder', 'MasterData'],
    }),
  }),
})

export const {
  useGetOrmePurchaseOrdersQuery,
  useGetOrmePurchaseOrderQuery,
  useSaveOrmePurchaseOrderMutation,
  useChangeOrmePurchaseOrderMutation,
  useDeleteOrmePurchaseOrderMutation,
  useReceiveOrmePurchaseOrderMutation,
  useCreateOrmePurchaseOrdersFromPlanMutation,
  useGetOrmeYarnPlanQuery,
} = ormePurchasingApi
