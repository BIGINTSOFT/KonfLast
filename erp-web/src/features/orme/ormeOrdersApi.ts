import { baseApi } from '../../api/baseApi'
import type { PagedResult } from '../../types/api'
import type { DocumentLanguage } from '../../utils/documentText'

export type OrmeOrderOwnership = 'COMMISSION' | 'OWN_GOODS'
export type OrmeOrderStatus = 'DRAFT' | 'CONFIRMED' | 'CANCELLED'

export const ormeOrderOwnershipLabels: Record<OrmeOrderOwnership, string> = {
  COMMISSION: 'Fason (müşteri ipliği)',
  OWN_GOODS: 'Kendi malı',
}

export const ormeOrderStatusPresentation: Record<OrmeOrderStatus, { label: string; tone: string }> = {
  DRAFT: { label: 'Taslak', tone: 'neutral' },
  CONFIRMED: { label: 'Onaylı', tone: 'ok' },
  CANCELLED: { label: 'İptal', tone: 'danger' },
}

export interface OrmeOrderListItem {
  id: number
  orderNo: string
  orderDate: string
  /** Boşsa stoğa üretimdir. */
  customerName: string | null
  customerOrderNo: string | null
  ownership: OrmeOrderOwnership
  dueDate: string | null
  lineCount: number
  totalKilograms: number
  totalAmount: number
  currency: string
  status: OrmeOrderStatus
}

export interface OrmeOrderLine {
  id: number
  fabricId: number
  fabricCode: string
  fabricName: string
  /** Müşterinin bu kumaşa verdiği kendi kodu; belgelerde basılır. */
  customerFabricCode: string | null
  kilograms: number
  /** Gramaj ve enden tahmini metre; en bilinmiyorsa boş. */
  estimatedMeters: number | null
  unitPrice: number | null
  amount: number | null
  /** İptal edilmemiş örme emirlerinin planı. */
  plannedKilograms: number
  /** Bu satırın emirlerinden çıkan topların toplamı. */
  knittedKilograms: number
  notes: string | null
}

export interface OrmeOrder {
  id: number
  orderNo: string
  orderDate: string
  customerId: number | null
  customerName: string | null
  customerOrderNo: string | null
  ownership: OrmeOrderOwnership
  dueDate: string | null
  currency: string
  notes: string | null
  status: OrmeOrderStatus
  totalKilograms: number
  totalAmount: number
  createdAt: string
  createdBy: string | null
  lines: OrmeOrderLine[]
}

export interface OrmeOrderRequest {
  customerId: number | null
  orderDate: string
  ownership: OrmeOrderOwnership
  customerOrderNo: string | null
  dueDate: string | null
  currency: string | null
  notes: string | null
  lines: { id: number | null; fabricId: number; kilograms: number; unitPrice: number | null; customerFabricCode: string | null; notes: string | null }[]
}

export interface OrmeProformaLine {
  id: number
  fabricCode: string
  fabricName: string
  customerFabricCode: string | null
  kilograms: number
  unitPrice: number | null
  /** Kilo × birim fiyat; fiyat girilmemişse boş. */
  amount: number | null
}

/** Proforma / sipariş teyidi; dil müşteri kartından gelir. */
export interface OrmeProforma {
  orderId: number
  orderNo: string
  status: OrmeOrderStatus
  orderDate: string
  dueDate: string | null
  customerOrderNo: string | null
  currency: string
  notes: string | null
  customerCode: string
  customerName: string
  customerTaxNumber: string | null
  customerTaxOffice: string | null
  /** Varsayılan fatura adresi. */
  customerAddress: string | null
  documentLanguage: DocumentLanguage
  paymentTermDays: number | null
  /** Müşterinin varsayılan KDV oranı; boşsa KDV sıfır hesaplanır. */
  vatRate: number | null
  totalKilograms: number
  subtotal: number
  vatAmount: number
  total: number
  /** Fiyatı girilmemiş satır var; tutara katılmaz. */
  hasUnpricedLines: boolean
  lines: OrmeProformaLine[]
}

export interface OrmeYarnRequirement {
  yarnId: number
  yarnCode: string
  yarnName: string
  yarnColor: string | null
  /** Fire hariç. */
  netKilograms: number
  /** Kumaştaki fire eklenmiş. */
  requiredKilograms: number
  /** Bu siparişe verilen net iplik. */
  issuedKilograms: number
  /** Malın sahibine ait lotların kalanı. */
  availableKilograms: number
  /** Aynı sahibin diğer onaylı siparişlerinin henüz verilmemiş fireli ihtiyacı. */
  otherOrdersKilograms: number
  /** Eldeki − diğer siparişler; eksi olabilir. */
  freeKilograms: number
  shortageKilograms: number
}

export interface OrmeOrderRequirements {
  orderId: number
  /** Fasonda ipliğin sahibi müşteri; kendi malında boş. */
  ownerName: string | null
  yarns: OrmeYarnRequirement[]
  missingFabrics: string[]
}

export interface OrmeYarnWaste {
  yarnId: number
  yarnCode: string
  yarnName: string
  issuedKilograms: number
  /** Örülen kilonun gerektirdiği net iplik. */
  theoreticalKilograms: number
  wasteKilograms: number
  wastePercent: number
}

export interface OrmeOrderWaste {
  orderId: number
  knittedKilograms: number
  /** Bütün emirler bittiyse fire kesindir. */
  allKnitOrdersCompleted: boolean
  tolerancePercent: number | null
  totalWastePercent: number
  exceedsTolerance: boolean
  yarns: OrmeYarnWaste[]
}

export const ormeOrdersApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getOrmeOrders: build.query<PagedResult<OrmeOrderListItem>, Record<string, unknown>>({
      query: (params) => ({ url: '/orme-orders', params }),
      providesTags: ['OrmeOrder'],
    }),
    getOrmeOrder: build.query<OrmeOrder, number>({
      query: (id) => `/orme-orders/${id}`,
      providesTags: ['OrmeOrder'],
    }),
    getOrmeOrderRequirements: build.query<OrmeOrderRequirements, number>({
      query: (id) => `/orme-orders/${id}/yarn-requirements`,
      providesTags: ['OrmeOrder', 'OrmeYarnLot'],
    }),
    getOrmeOrderProforma: build.query<OrmeProforma, number>({
      query: (id) => `/orme-orders/${id}/proforma`,
      providesTags: ['OrmeOrder', 'MasterData'],
    }),
    getOrmeOrderWaste: build.query<OrmeOrderWaste, number>({
      query: (id) => `/orme-orders/${id}/waste`,
      providesTags: ['OrmeOrder', 'OrmeKnitOrder'],
    }),
    saveOrmeOrder: build.mutation<OrmeOrder, { id?: number; body: OrmeOrderRequest }>({
      query: ({ id, body }) => ({ url: id === undefined ? '/orme-orders' : `/orme-orders/${id}`, method: id === undefined ? 'POST' : 'PUT', body }),
      invalidatesTags: ['OrmeOrder'],
    }),
    confirmOrmeOrder: build.mutation<OrmeOrder, number>({
      query: (id) => ({ url: `/orme-orders/${id}/confirm`, method: 'POST' }),
      invalidatesTags: ['OrmeOrder'],
    }),
    reopenOrmeOrder: build.mutation<OrmeOrder, number>({
      query: (id) => ({ url: `/orme-orders/${id}/reopen`, method: 'POST' }),
      invalidatesTags: ['OrmeOrder'],
    }),
    cancelOrmeOrder: build.mutation<OrmeOrder, number>({
      query: (id) => ({ url: `/orme-orders/${id}/cancel`, method: 'POST' }),
      invalidatesTags: ['OrmeOrder'],
    }),
    deleteOrmeOrder: build.mutation<null, number>({
      query: (id) => ({ url: `/orme-orders/${id}`, method: 'DELETE' }),
      invalidatesTags: ['OrmeOrder'],
    }),
  }),
})

export const {
  useGetOrmeOrdersQuery,
  useGetOrmeOrderQuery,
  useGetOrmeOrderRequirementsQuery,
  useGetOrmeOrderWasteQuery,
  useGetOrmeOrderProformaQuery,
  useSaveOrmeOrderMutation,
  useConfirmOrmeOrderMutation,
  useReopenOrmeOrderMutation,
  useCancelOrmeOrderMutation,
  useDeleteOrmeOrderMutation,
} = ormeOrdersApi
