import { baseApi } from '../../api/baseApi'
import type { PagedResult } from '../../types/api'
import type { DocumentLanguage } from '../../utils/documentText'

export type DokumaOrderOwnership = 'COMMISSION' | 'OWN_GOODS'
export type DokumaOrderStatus = 'DRAFT' | 'CONFIRMED' | 'CANCELLED'

export const dokumaOrderOwnershipLabels: Record<DokumaOrderOwnership, string> = {
  COMMISSION: 'Fason (müşteri ipliği)',
  OWN_GOODS: 'Kendi malı',
}

export const dokumaOrderStatusPresentation: Record<DokumaOrderStatus, { label: string; tone: string }> = {
  DRAFT: { label: 'Taslak', tone: 'neutral' },
  CONFIRMED: { label: 'Onaylı', tone: 'ok' },
  CANCELLED: { label: 'İptal', tone: 'danger' },
}

export interface DokumaOrderListItem {
  id: number
  orderNo: string
  orderDate: string
  /** Boşsa stoğa üretimdir. */
  customerName: string | null
  customerOrderNo: string | null
  ownership: DokumaOrderOwnership
  dueDate: string | null
  lineCount: number
  totalMeters: number
  totalAmount: number
  currency: string
  status: DokumaOrderStatus
}

export interface DokumaOrderLine {
  id: number
  articleId: number
  articleCode: string
  articleName: string
  meters: number
  unitPrice: number | null
  amount: number | null
  notes: string | null
  /** Müşterinin bu artikel için kullandığı kendi kodu. */
  customerArticleCode: string | null
}

export interface DokumaOrder {
  id: number
  orderNo: string
  orderDate: string
  customerId: number | null
  customerName: string | null
  customerOrderNo: string | null
  ownership: DokumaOrderOwnership
  dueDate: string | null
  currency: string
  notes: string | null
  status: DokumaOrderStatus
  totalMeters: number
  totalAmount: number
  createdAt: string
  createdBy: string | null
  lines: DokumaOrderLine[]
}

export interface DokumaOrderRequest {
  customerId: number | null
  orderDate: string
  ownership: DokumaOrderOwnership
  customerOrderNo: string | null
  dueDate: string | null
  currency: string | null
  notes: string | null
  lines: { id: number | null; articleId: number; meters: number; unitPrice: number | null; notes: string | null; customerArticleCode: string | null }[]
}

export interface DokumaYarnRequirement {
  yarnId: number
  yarnCode: string
  yarnName: string
  yarnColor: string | null
  usedInWarp: boolean
  usedInWeft: boolean
  /** Fire hariç. */
  netKilograms: number
  /** Artikeldeki fire eklenmiş. */
  requiredKilograms: number
  /** Bu siparişin çözgü emirlerine verilen. */
  issuedKilograms: number
  availableKilograms: number
  /** Aynı sahibin diğer onaylı siparişlerinin fireli ihtiyacı. */
  otherOrdersKilograms: number
  /** Eldeki − diğer siparişler; eksi olabilir. */
  freeKilograms: number
  shortageKilograms: number
}

export interface DokumaOrderRequirements {
  orderId: number
  /** Fasonda ipliğin sahibi müşteri; kendi malında boş. */
  ownerName: string | null
  yarns: DokumaYarnRequirement[]
  missingArticles: string[]
}

export interface DokumaProformaLine {
  articleCode: string
  articleName: string
  customerArticleCode: string | null
  rawWidthCm: number
  rawWeightGsm: number
  meters: number
  unitPrice: number | null
  amount: number | null
  notes: string | null
}

/** Proforma / sipariş teyidi çıktısı. */
export interface DokumaProforma {
  orderId: number
  orderNo: string
  orderDate: string
  dueDate: string | null
  customerOrderNo: string | null
  status: DokumaOrderStatus
  currency: string
  notes: string | null
  customerCode: string | null
  customerName: string | null
  customerTaxNumber: string | null
  customerTaxOffice: string | null
  /** Varsayılan fatura adresi. */
  customerAddress: string | null
  paymentTermDays: number | null
  /** Müşteri kartındaki varsayılan KDV; boşsa KDV sıfır hesaplanır. */
  vatRate: number | null
  totalMeters: number
  subtotal: number
  vatAmount: number
  total: number
  /** Fiyatı girilmemiş satır var. */
  hasMissingPrices: boolean
  documentLanguage: DocumentLanguage
  lines: DokumaProformaLine[]
}

export const dokumaOrdersApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getDokumaOrders: build.query<PagedResult<DokumaOrderListItem>, Record<string, unknown>>({
      query: (params) => ({ url: '/dokuma-orders', params }),
      providesTags: ['DokumaOrder'],
    }),
    getDokumaOrder: build.query<DokumaOrder, number>({
      query: (id) => `/dokuma-orders/${id}`,
      providesTags: ['DokumaOrder'],
    }),
    getDokumaProforma: build.query<DokumaProforma, number>({
      query: (id) => `/dokuma-orders/${id}/proforma`,
      providesTags: ['DokumaOrder', 'MasterData'],
    }),
    getDokumaOrderRequirements: build.query<DokumaOrderRequirements, number>({
      query: (id) => `/dokuma-orders/${id}/yarn-requirements`,
      providesTags: ['DokumaOrder', 'DokumaYarnLot'],
    }),
    saveDokumaOrder: build.mutation<DokumaOrder, { id?: number; body: DokumaOrderRequest }>({
      query: ({ id, body }) => ({ url: id === undefined ? '/dokuma-orders' : `/dokuma-orders/${id}`, method: id === undefined ? 'POST' : 'PUT', body }),
      invalidatesTags: ['DokumaOrder'],
    }),
    confirmDokumaOrder: build.mutation<DokumaOrder, number>({
      query: (id) => ({ url: `/dokuma-orders/${id}/confirm`, method: 'POST' }),
      invalidatesTags: ['DokumaOrder'],
    }),
    reopenDokumaOrder: build.mutation<DokumaOrder, number>({
      query: (id) => ({ url: `/dokuma-orders/${id}/reopen`, method: 'POST' }),
      invalidatesTags: ['DokumaOrder'],
    }),
    cancelDokumaOrder: build.mutation<DokumaOrder, number>({
      query: (id) => ({ url: `/dokuma-orders/${id}/cancel`, method: 'POST' }),
      invalidatesTags: ['DokumaOrder'],
    }),
    deleteDokumaOrder: build.mutation<null, number>({
      query: (id) => ({ url: `/dokuma-orders/${id}`, method: 'DELETE' }),
      invalidatesTags: ['DokumaOrder'],
    }),
  }),
})

export const {
  useGetDokumaOrdersQuery,
  useGetDokumaOrderQuery,
  useGetDokumaOrderRequirementsQuery,
  useGetDokumaProformaQuery,
  useSaveDokumaOrderMutation,
  useConfirmDokumaOrderMutation,
  useReopenDokumaOrderMutation,
  useCancelDokumaOrderMutation,
  useDeleteDokumaOrderMutation,
} = dokumaOrdersApi
