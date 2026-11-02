import { baseApi } from '../../api/baseApi'
import type { PagedResult } from '../../types/api'
import type { DocumentLanguage } from '../../utils/documentText'

export type PackingListStatus = 'DRAFT' | 'CONFIRMED' | 'CANCELLED'

export const packingStatusPresentation: Record<PackingListStatus, { label: string; tone: string }> = {
  DRAFT: { label: 'Taslak', tone: 'neutral' },
  CONFIRMED: { label: 'Onaylı', tone: 'ok' },
  CANCELLED: { label: 'İptal', tone: 'danger' },
}

export interface PackingListRow {
  id: number
  packingNo: string
  orderNo: string
  customerName: string | null
  packingDate: string
  packageCount: number
  totalKilograms: number
  totalMeters: number
  status: PackingListStatus
}

export interface Package {
  id: number
  batchItemId: number
  packageNo: string
  itemNo: string
  batchId: number
  batchNo: string
  cardCode: string
  cardName: string
  colorName: string | null
  colorCode: string | null
  kilograms: number
  meters: number | null
  boxCount: number
  isYarn: boolean
  /** Müşterinin top ya da lot numarası. */
  customerItemNo: string | null
  /** Müşterinin kabul irsaliyesindeki kendi kumaş kodu. */
  customerFabricCode: string | null
}

export interface PackingList {
  id: number
  packingNo: string
  orderId: number
  orderNo: string
  customerOrderNo: string | null
  customerName: string | null
  packingDate: string
  notes: string | null
  status: PackingListStatus
  totalKilograms: number
  totalMeters: number
  totalBoxes: number
  packages: Package[]
  isStock: boolean
  saleCustomerId: number | null
  isDespatched: boolean
  documentLanguage: DocumentLanguage
}

export interface PackingCandidate {
  batchItemId: number
  orderId: number
  orderNo: string
  customerName: string | null
  itemNo: string
  batchNo: string
  cardCode: string
  colorName: string | null
  kilograms: number
  meters: number | null
  isYarn: boolean
  boxCount: number
  selected: boolean
}

export interface PackingListRequest {
  packingDate: string
  notes: string | null
  items: { batchItemId: number; boxCount: number }[]
}

export const boyahanePackingApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getPackingLists: build.query<PagedResult<PackingListRow>, Record<string, unknown>>({
      query: (params) => ({ url: '/boyahane-packing-lists', params }),
      providesTags: ['BoyahaneBatch'],
    }),
    getPackingList: build.query<PackingList, number>({
      query: (id) => `/boyahane-packing-lists/${id}`,
      providesTags: ['BoyahaneBatch'],
    }),
    getPackingCandidates: build.query<PackingCandidate[], number | undefined>({
      query: (packingListId) => ({ url: '/boyahane-packing-lists/candidates', params: packingListId === undefined ? {} : { packingListId } }),
      providesTags: ['BoyahaneBatch'],
    }),
    savePackingList: build.mutation<PackingList, { id?: number; body: PackingListRequest }>({
      query: ({ id, body }) => ({ url: id === undefined ? '/boyahane-packing-lists' : `/boyahane-packing-lists/${id}`, method: id === undefined ? 'POST' : 'PUT', body }),
      invalidatesTags: ['BoyahaneBatch'],
    }),
    setPackingSaleCustomer: build.mutation<PackingList, { id: number; customerId: number | null }>({
      query: ({ id, customerId }) => ({ url: `/boyahane-packing-lists/${id}/sale-customer`, method: 'PUT', body: { customerId } }),
      invalidatesTags: ['BoyahaneBatch'],
    }),
    changePackingList: build.mutation<PackingList, { id: number; action: 'confirm' | 'reopen' | 'cancel' }>({
      query: ({ id, action }) => ({ url: `/boyahane-packing-lists/${id}/${action}`, method: 'POST' }),
      invalidatesTags: ['BoyahaneBatch'],
    }),
  }),
})

export const {
  useSetPackingSaleCustomerMutation,
  useGetPackingListsQuery,
  useGetPackingListQuery,
  useGetPackingCandidatesQuery,
  useSavePackingListMutation,
  useChangePackingListMutation,
} = boyahanePackingApi
