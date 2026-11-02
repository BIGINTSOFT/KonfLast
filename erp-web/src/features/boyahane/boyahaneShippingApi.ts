import { baseApi } from '../../api/baseApi'
import type { PagedResult } from '../../types/api'
import type { DocumentLanguage } from '../../utils/documentText'
import type { OrderUnit } from './boyahaneOrdersApi'

export type ShippingStatus = 'DRAFT' | 'CONFIRMED' | 'CANCELLED'

export const shippingStatusPresentation: Record<ShippingStatus, { label: string; tone: string }> = {
  DRAFT: { label: 'Taslak', tone: 'neutral' },
  CONFIRMED: { label: 'Onaylı', tone: 'ok' },
  CANCELLED: { label: 'İptal', tone: 'danger' },
}

export interface DespatchRow {
  id: number
  despatchNo: string
  customerName: string
  despatchDate: string
  vehiclePlate: string | null
  packingListCount: number
  totalKilograms: number
  status: ShippingStatus
  invoiceNo: string | null
}

export interface Despatch {
  id: number
  despatchNo: string
  customerId: number
  customerName: string
  customerTaxNumber: string | null
  despatchDate: string
  vehiclePlate: string | null
  driverName: string | null
  deliveryAddress: string | null
  notes: string | null
  status: ShippingStatus
  invoiceId: number | null
  invoiceNo: string | null
  totalKilograms: number
  totalMeters: number
  totalPackages: number
  packingLists: { id: number; packingNo: string; orderNo: string; packageCount: number; kilograms: number; meters: number }[]
  lines: DespatchLine[]
  /** Yükleme kontrolünde okutulan barkodla eşlenen paketler. */
  packages: DespatchPackage[]
  documentLanguage: DocumentLanguage
}

export interface DespatchLine {
  orderNo: string
  cardCode: string
  cardName: string
  colorName: string | null
  packageCount: number
  boxCount: number
  kilograms: number
  meters: number
  customerOrderNo: string | null
  customerFabricCode: string | null
  colorCode: string | null
}

export interface DespatchPackage {
  id: number
  packageNo: string
  packingNo: string
  itemNo: string
  cardCode: string
  kilograms: number
  meters: number | null
}

export interface DespatchCandidate {
  id: number
  packingNo: string
  customerId: number
  customerName: string
  orderNo: string
  packageCount: number
  kilograms: number
  meters: number
  selected: boolean
}

export interface DespatchRequest {
  customerId: number
  despatchDate: string
  packingListIds: number[]
  vehiclePlate: string | null
  driverName: string | null
  deliveryAddress: string | null
  notes: string | null
}

export interface InvoiceRow {
  id: number
  invoiceNo: string
  customerName: string
  invoiceDate: string
  dueDate: string | null
  currency: string
  subtotal: number
  status: ShippingStatus
}

export interface InvoiceLine {
  id: number
  orderLineId: number
  orderNo: string
  description: string
  quantity: number
  unit: OrderUnit
  unitPrice: number | null
  amount: number | null
  customerOrderNo: string | null
}

export interface Invoice {
  id: number
  invoiceNo: string
  customerId: number
  customerName: string
  customerTaxNumber: string | null
  customerTaxOffice: string | null
  invoiceDate: string
  dueDate: string | null
  currency: string
  vatRate: number | null
  withholdingPercent: number | null
  notes: string | null
  status: ShippingStatus
  subtotal: number
  vatAmount: number
  withholdingAmount: number
  payableAmount: number
  paidAmount: number
  isOpening: boolean
  payments: { paymentId: number; paymentNo: string; paymentDate: string; method: 'CASH' | 'BANK_TRANSFER' | 'CHECK' | 'OPENING'; amount: number }[]
  despatchNos: string[]
  despatchIds: number[]
  lines: InvoiceLine[]
  documentLanguage: DocumentLanguage
}

export interface InvoiceCandidate {
  id: number
  despatchNo: string
  customerId: number
  customerName: string
  despatchDate: string
  kilograms: number
  meters: number
  selected: boolean
}

export interface InvoiceRequest {
  customerId: number
  invoiceDate: string
  despatchIds: number[]
  dueDate: string | null
  vatRate: number | null
  withholdingPercent: number | null
  notes: string | null
  prices: { orderLineId: number; unitPrice: number | null }[]
}

type Action = 'confirm' | 'reopen' | 'cancel'

export const boyahaneShippingApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getDespatches: build.query<PagedResult<DespatchRow>, Record<string, unknown>>({
      query: (params) => ({ url: '/boyahane-despatches', params }),
      providesTags: ['BoyahaneBatch'],
    }),
    getDespatch: build.query<Despatch, number>({
      query: (id) => `/boyahane-despatches/${id}`,
      providesTags: ['BoyahaneBatch'],
    }),
    getDespatchCandidates: build.query<DespatchCandidate[], number | undefined>({
      query: (despatchId) => ({ url: '/boyahane-despatches/candidates', params: despatchId === undefined ? {} : { despatchId } }),
      providesTags: ['BoyahaneBatch'],
    }),
    saveDespatch: build.mutation<Despatch, { id?: number; body: DespatchRequest }>({
      query: ({ id, body }) => ({ url: id === undefined ? '/boyahane-despatches' : `/boyahane-despatches/${id}`, method: id === undefined ? 'POST' : 'PUT', body }),
      invalidatesTags: ['BoyahaneBatch'],
    }),
    changeDespatch: build.mutation<Despatch, { id: number; action: Action }>({
      query: ({ id, action }) => ({ url: `/boyahane-despatches/${id}/${action}`, method: 'POST' }),
      invalidatesTags: ['BoyahaneBatch'],
    }),
    getInvoices: build.query<PagedResult<InvoiceRow>, Record<string, unknown>>({
      query: (params) => ({ url: '/boyahane-invoices', params }),
      providesTags: ['BoyahaneBatch'],
    }),
    getInvoice: build.query<Invoice, number>({
      query: (id) => `/boyahane-invoices/${id}`,
      providesTags: ['BoyahaneBatch'],
    }),
    getInvoiceCandidates: build.query<InvoiceCandidate[], number | undefined>({
      query: (invoiceId) => ({ url: '/boyahane-invoices/candidates', params: invoiceId === undefined ? {} : { invoiceId } }),
      providesTags: ['BoyahaneBatch'],
    }),
    saveInvoice: build.mutation<Invoice, { id?: number; body: InvoiceRequest }>({
      query: ({ id, body }) => ({ url: id === undefined ? '/boyahane-invoices' : `/boyahane-invoices/${id}`, method: id === undefined ? 'POST' : 'PUT', body }),
      invalidatesTags: ['BoyahaneBatch'],
    }),
    changeInvoice: build.mutation<Invoice, { id: number; action: Action }>({
      query: ({ id, action }) => ({ url: `/boyahane-invoices/${id}/${action}`, method: 'POST' }),
      invalidatesTags: ['BoyahaneBatch'],
    }),
  }),
})

export const {
  useGetDespatchesQuery,
  useGetDespatchQuery,
  useGetDespatchCandidatesQuery,
  useSaveDespatchMutation,
  useChangeDespatchMutation,
  useGetInvoicesQuery,
  useGetInvoiceQuery,
  useGetInvoiceCandidatesQuery,
  useSaveInvoiceMutation,
  useChangeInvoiceMutation,
} = boyahaneShippingApi
