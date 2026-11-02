import { baseApi } from '../../api/baseApi'
import type { PagedResult } from '../../types/api'
import type { DocumentLanguage } from '../../utils/documentText'
import type { FabricRollStatus } from './boyahaneReceiptsApi'

export type OrderOwnership = 'COMMISSION' | 'OWN_GOODS'
export type OrderStatus = 'DRAFT' | 'CONFIRMED' | 'CANCELLED' | 'CLOSED'
export type OrderProcessType = 'FABRIC_DYEING' | 'YARN_DYEING' | 'FINISHING' | 'PRINTING'
export type OrderUnit = 'METER' | 'KILOGRAM'
export type BillingUnit = 'ORDER_UNIT' | 'METER' | 'KILOGRAM'

export const orderOwnershipLabels: Record<OrderOwnership, string> = {
  COMMISSION: 'Fason',
  OWN_GOODS: 'Kendi malı',
}

export const orderStatusPresentation: Record<OrderStatus, { label: string; tone: string }> = {
  DRAFT: { label: 'Taslak', tone: 'neutral' },
  CONFIRMED: { label: 'Onaylı', tone: 'ok' },
  CANCELLED: { label: 'İptal', tone: 'danger' },
  CLOSED: { label: 'Kapalı', tone: 'neutral' },
}

export const orderProcessTypeLabels: Record<OrderProcessType, string> = {
  FABRIC_DYEING: 'Kumaş boyama',
  YARN_DYEING: 'İplik boyama',
  FINISHING: 'Terbiye / apre',
  PRINTING: 'Baskı',
}

export const orderUnitLabels: Record<OrderUnit, string> = { METER: 'm', KILOGRAM: 'kg' }

export interface BoyahaneOrderListItem {
  id: number
  orderNo: string
  orderDate: string
  /** Boşsa stoğa üretimdir. */
  customerName: string | null
  customerOrderNo: string | null
  ownership: OrderOwnership
  dueDate: string | null
  lineCount: number
  assignedRollCount: number
  totalAmount: number
  currency: string
  status: OrderStatus
}

export interface BoyahaneOrderLine {
  id: number
  processType: OrderProcessType
  fabricId: number | null
  yarnId: number | null
  itemCode: string
  itemName: string
  colorName: string | null
  colorCode: string | null
  quantity: number
  unit: OrderUnit
  billingUnit: BillingUnit | null
  effectiveBillingUnit: OrderUnit
  unitPrice: number | null
  amount: number | null
  notes: string | null
  assignedRollCount: number
  assignedYarnLotCount: number
  assignedMeters: number
  assignedKilograms: number
  /** Bağlanan miktar siparişten toleransı aşan oranda farklıysa fark (%). */
  assignedDifferencePercent: number | null
  labRequestId: number | null
  labRequestNo: string | null
  labRequestStatus: 'OPEN' | 'APPROVED' | 'CANCELLED' | null
  /** Onaylanan denemenin harfi: satırın reçetesi. */
  recipeTrialCode: string | null
  colorCardId: number | null
  colorCardCode: string | null
  patternId: number | null
  patternCode: string | null
  colorwayId: number | null
  colorwayCode: string | null
}

export interface BoyahaneOrder {
  id: number
  orderNo: string
  orderDate: string
  customerId: number | null
  customerName: string | null
  customerOrderNo: string | null
  ownership: OrderOwnership
  dueDate: string | null
  currency: string
  notes: string | null
  status: OrderStatus
  totalAmount: number
  createdAt: string
  createdBy: string | null
  lines: BoyahaneOrderLine[]
  closedAt: string | null
  closedBy: string | null
  closeNote: string | null
}

export interface BoyahaneOrderRequest {
  customerId: number | null
  orderDate: string
  ownership: OrderOwnership
  customerOrderNo: string | null
  dueDate: string | null
  currency: string | null
  notes: string | null
  lines: {
    id: number | null
    processType: OrderProcessType
    fabricId: number | null
    yarnId: number | null
    colorName: string | null
    colorCode: string | null
    quantity: number
    unit: OrderUnit
    billingUnit: BillingUnit | null
    unitPrice: number | null
    notes: string | null
    /** Boyama satırında renk kartı; seçilirse renk adı ve kodu karttan gelir. */
    colorCardId: number | null
    /** Baskı satırında desen ve renk yolu; seçilirse desen adı ve kodu karttan gelir. */
    patternId: number | null
    colorwayId: number | null
  }[]
}

export interface AssignableRoll {
  id: number
  rollNo: string
  customerRollNo: string | null
  lotNo: string
  meters: number
  kilograms: number
  status: FabricRollStatus
  assigned: boolean
}

export interface AssignableYarnLot {
  id: number
  lotNo: string
  customerLotNo: string | null
  packageType: 'BOBBIN' | 'HANK'
  packageCount: number
  kilograms: number
  assigned: boolean
}

export interface BoyahaneProformaLine {
  id: number
  processType: OrderProcessType
  isYarn: boolean
  itemCode: string
  itemName: string
  colorName: string | null
  colorCode: string | null
  quantity: number
  unit: OrderUnit
  /** Fatura ölçüsünün (priceUnit) birim fiyatı. */
  unitPrice: number | null
  priceUnit: OrderUnit
  /** Fatura ölçüsü sipariş biriminden farklıysa boş. */
  amount: number | null
}

/** Proforma / sipariş teyidi baskısının verisi. */
export interface BoyahaneProforma {
  orderId: number
  orderNo: string
  status: OrderStatus
  orderDate: string
  dueDate: string | null
  customerOrderNo: string | null
  customerCode: string
  customerName: string
  customerTaxNumber: string | null
  customerTaxOffice: string | null
  customerAddress: string | null
  currency: string
  vatRate: number
  vatRateMissing: boolean
  paymentTermDays: number | null
  subtotal: number
  vatAmount: number
  total: number
  hasUnestimatedLines: boolean
  notes: string | null
  documentLanguage: DocumentLanguage
  lines: BoyahaneProformaLine[]
}

const orderTags = ['BoyahaneOrder', 'BoyahaneInspection', 'BoyahaneReceipt'] as const

export const boyahaneOrdersApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getBoyahaneOrders: build.query<PagedResult<BoyahaneOrderListItem>, Record<string, unknown>>({
      query: (params) => ({ url: '/boyahane-orders', params }),
      providesTags: ['BoyahaneOrder'],
    }),
    getBoyahaneOrder: build.query<BoyahaneOrder, number>({
      query: (id) => `/boyahane-orders/${id}`,
      providesTags: ['BoyahaneOrder'],
    }),
    getBoyahaneProforma: build.query<BoyahaneProforma, number>({
      query: (id) => `/boyahane-orders/${id}/proforma`,
      providesTags: ['BoyahaneOrder', 'MasterData'],
    }),
    saveBoyahaneOrder: build.mutation<BoyahaneOrder, { id?: number; body: BoyahaneOrderRequest }>({
      query: ({ id, body }) => ({ url: id === undefined ? '/boyahane-orders' : `/boyahane-orders/${id}`, method: id === undefined ? 'POST' : 'PUT', body }),
      invalidatesTags: ['BoyahaneOrder'],
    }),
    confirmBoyahaneOrder: build.mutation<BoyahaneOrder, number>({
      query: (id) => ({ url: `/boyahane-orders/${id}/confirm`, method: 'POST' }),
      invalidatesTags: ['BoyahaneOrder'],
    }),
    reopenBoyahaneOrder: build.mutation<BoyahaneOrder, number>({
      query: (id) => ({ url: `/boyahane-orders/${id}/reopen`, method: 'POST' }),
      invalidatesTags: ['BoyahaneOrder'],
    }),
    getAssignableYarnLots: build.query<AssignableYarnLot[], { id: number; lineId: number }>({
      query: ({ id, lineId }) => `/boyahane-orders/${id}/lines/${lineId}/assignable-yarn-lots`,
      providesTags: ['BoyahaneOrder'],
    }),
    assignOrderLineYarnLots: build.mutation<BoyahaneOrder, { id: number; lineId: number; lotIds: number[] }>({
      query: ({ id, lineId, lotIds }) => ({ url: `/boyahane-orders/${id}/lines/${lineId}/yarn-lots`, method: 'PUT', body: { lotIds } }),
      invalidatesTags: [...orderTags],
    }),
    cancelBoyahaneOrder: build.mutation<BoyahaneOrder, number>({
      query: (id) => ({ url: `/boyahane-orders/${id}/cancel`, method: 'POST' }),
      invalidatesTags: [...orderTags],
    }),
    closeBoyahaneOrder: build.mutation<BoyahaneOrder, { id: number; note: string | null }>({
      query: ({ id, note }) => ({ url: `/boyahane-orders/${id}/close`, method: 'POST', body: { note } }),
      invalidatesTags: [...orderTags, 'BoyahaneLab', 'BoyahaneBatch'],
    }),
    deleteBoyahaneOrder: build.mutation<null, number>({
      query: (id) => ({ url: `/boyahane-orders/${id}`, method: 'DELETE' }),
      invalidatesTags: [...orderTags],
    }),
    getAssignableRolls: build.query<AssignableRoll[], { id: number; lineId: number }>({
      query: ({ id, lineId }) => `/boyahane-orders/${id}/lines/${lineId}/assignable-rolls`,
      providesTags: ['BoyahaneOrder'],
    }),
    assignOrderLineRolls: build.mutation<BoyahaneOrder, { id: number; lineId: number; rollIds: number[] }>({
      query: ({ id, lineId, rollIds }) => ({ url: `/boyahane-orders/${id}/lines/${lineId}/rolls`, method: 'PUT', body: { rollIds } }),
      invalidatesTags: [...orderTags],
    }),
  }),
})

export const {
  useGetBoyahaneOrdersQuery,
  useGetBoyahaneOrderQuery,
  useGetBoyahaneProformaQuery,
  useSaveBoyahaneOrderMutation,
  useConfirmBoyahaneOrderMutation,
  useCancelBoyahaneOrderMutation,
  useCloseBoyahaneOrderMutation,
  useDeleteBoyahaneOrderMutation,
  useGetAssignableRollsQuery,
  useAssignOrderLineRollsMutation,
  useReopenBoyahaneOrderMutation,
  useGetAssignableYarnLotsQuery,
  useAssignOrderLineYarnLotsMutation,
} = boyahaneOrdersApi
