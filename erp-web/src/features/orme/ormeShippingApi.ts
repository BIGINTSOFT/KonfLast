import { baseApi } from '../../api/baseApi'
import type { PagedResult } from '../../types/api'
import type { DocumentLanguage, DocumentTextKey } from '../../utils/documentText'
import { formatQuantity } from '../../utils/format'
import type { OrmeFabricRollStatus } from './ormeKnitApi'

/** Tahmini metre ~ ile gösterilir. */
export const ormeMetersText = (meters: number, estimated: boolean) => `${estimated ? '~' : ''}${formatQuantity(meters)}`

export type OrmeDocumentStatus = 'DRAFT' | 'CONFIRMED' | 'CANCELLED'

export const ormeDocumentStatusPresentation: Record<OrmeDocumentStatus, { label: string; tone: string }> = {
  DRAFT: { label: 'Taslak', tone: 'neutral' },
  CONFIRMED: { label: 'Onaylı', tone: 'ok' },
  CANCELLED: { label: 'İptal', tone: 'danger' },
}

export const ormeGradeLabels: Record<string, string> = {
  FIRST_QUALITY: '1. kalite',
  SECOND_QUALITY: '2. kalite',
}

/** Baskıda kalite: belge dilinde. */
export const ormeGradeText = (t: (key: DocumentTextKey) => string, grade: string) =>
  grade === 'SECOND_QUALITY' ? t('secondQuality') : t('firstQuality')

export interface OrmePackingListRow {
  id: number
  packingNo: string
  orderNo: string
  customerName: string | null
  packingDate: string
  rollCount: number
  totalKilograms: number
  status: OrmeDocumentStatus
  despatchNo: string | null
  boyahaneReceiptNo: string | null
}

export interface OrmePackingItem {
  id: number
  rollId: number
  rollNo: string
  knitOrderNo: string
  machineCode: string
  fabricCode: string
  fabricName: string
  /** Müşterinin sipariş satırındaki kendi kumaş kodu. */
  customerFabricCode: string | null
  /** Makinede tartılan kilo. */
  kilograms: number
  meters: number
  /** Metre ölçülmedi, tahmindir. */
  metersEstimated: boolean
  grade: OrmeFabricRollStatus
}

export interface OrmePackingList {
  id: number
  packingNo: string
  orderId: number
  orderNo: string
  customerOrderNo: string | null
  customerName: string | null
  packingDate: string
  notes: string | null
  status: OrmeDocumentStatus
  totalKilograms: number
  items: OrmePackingItem[]
  /** Stoğa üretim: irsaliye için listede müşteri seçilir. */
  isStock: boolean
  /** Fason: Boyahane'ye gönderirken Boyahane müşterisi zorunlu. */
  isCommission: boolean
  saleCustomerId: number | null
  despatchId: number | null
  despatchNo: string | null
  boyahaneReceiptNo: string | null
  boyahaneSentOn: string | null
  /** Boyahane modülü bu kurulumda yüklü. */
  canSendToBoyahane: boolean
  /** Baskı dili: siparişin ya da satılan müşterinin kartından. */
  documentLanguage: DocumentLanguage
}

export interface OrmePackingCandidate {
  rollId: number
  orderId: number
  orderNo: string
  customerName: string | null
  rollNo: string
  knitOrderNo: string
  fabricCode: string
  /** Makinede tartılan kilo. */
  kilograms: number
  meters: number
  /** Metre ölçülmedi, tahmindir. */
  metersEstimated: boolean
  grade: OrmeFabricRollStatus
  selected: boolean
}

export interface OrmeDespatchRow {
  id: number
  despatchNo: string
  customerName: string
  despatchDate: string
  vehiclePlate: string | null
  packingListCount: number
  totalKilograms: number
  status: OrmeDocumentStatus
  invoiceNo: string | null
}

export interface OrmeDespatch {
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
  status: OrmeDocumentStatus
  invoiceId: number | null
  invoiceNo: string | null
  totalKilograms: number
  totalRolls: number
  packingLists: { id: number; packingNo: string; orderNo: string; rollCount: number; kilograms: number }[]
  lines: {
    orderNo: string
    fabricCode: string
    fabricName: string
    grade: OrmeFabricRollStatus
    rollCount: number
    kilograms: number
    meters: number
    metersEstimated: boolean
    customerOrderNo: string | null
    customerFabricCode: string | null
  }[]
  /** İrsaliyedeki toplar; yükleme kontrolünde kullanılır. */
  rolls: { rollNo: string; packingNo: string; fabricCode: string; kilograms: number }[]
  documentLanguage: DocumentLanguage
}

export interface OrmeDespatchCandidate {
  id: number
  packingNo: string
  customerId: number
  customerName: string
  orderNo: string
  rollCount: number
  kilograms: number
  selected: boolean
}

export interface OrmeDespatchRequest {
  customerId: number
  despatchDate: string
  packingListIds: number[]
  vehiclePlate: string | null
  driverName: string | null
  deliveryAddress: string | null
  notes: string | null
}

export interface OrmeInvoiceRow {
  id: number
  invoiceNo: string
  customerName: string
  invoiceDate: string
  dueDate: string | null
  currency: string
  subtotal: number
  status: OrmeDocumentStatus
}

export interface OrmeInvoiceLine {
  id: number
  orderLineId: number
  orderNo: string
  grade: OrmeFabricRollStatus
  description: string
  quantity: number
  unitPrice: number | null
  discountPercent: number | null
  amount: number | null
  customerOrderNo: string | null
  customerFabricCode: string | null
}

export interface OrmeInvoice {
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
  status: OrmeDocumentStatus
  subtotal: number
  vatAmount: number
  withholdingAmount: number
  payableAmount: number
  paidAmount: number
  isOpening: boolean
  payments: { paymentId: number; paymentNo: string; paymentDate: string; method: 'CASH' | 'BANK_TRANSFER' | 'CHECK' | 'OPENING'; amount: number }[]
  despatchNos: string[]
  despatchIds: number[]
  lines: OrmeInvoiceLine[]
  documentLanguage: DocumentLanguage
}

export interface OrmeInvoiceCandidate {
  id: number
  despatchNo: string
  customerId: number
  customerName: string
  despatchDate: string
  kilograms: number
  selected: boolean
}

export interface OrmeInvoiceRequest {
  customerId: number
  invoiceDate: string
  despatchIds: number[]
  dueDate: string | null
  vatRate: number | null
  withholdingPercent: number | null
  notes: string | null
  prices: { orderLineId: number; grade: OrmeFabricRollStatus; unitPrice: number | null; discountPercent: number | null }[]
}

export interface OrmeYarnReturn {
  id: number
  returnNo: string
  returnDate: string
  yarnLotId: number
  lotNo: string
  producerLotNo: string | null
  customerName: string
  yarnCode: string
  yarnName: string
  kilograms: number
  notes: string | null
}

type Action = 'confirm' | 'reopen' | 'cancel'

const tags = ['OrmeShipping', 'OrmeKnitOrder', 'OrmeOrder'] as const

export const ormeShippingApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getOrmePackingLists: build.query<PagedResult<OrmePackingListRow>, Record<string, unknown>>({
      query: (params) => ({ url: '/orme-packing-lists', params }),
      providesTags: ['OrmeShipping'],
    }),
    getOrmePackingList: build.query<OrmePackingList, number>({
      query: (id) => `/orme-packing-lists/${id}`,
      providesTags: ['OrmeShipping'],
    }),
    getOrmePackingCandidates: build.query<OrmePackingCandidate[], number | undefined>({
      query: (packingListId) => ({ url: '/orme-packing-lists/candidates', params: packingListId === undefined ? {} : { packingListId } }),
      providesTags: ['OrmeShipping', 'OrmeKnitOrder'],
    }),
    saveOrmePackingList: build.mutation<OrmePackingList, { id?: number; body: { packingDate: string; notes: string | null; rollIds: number[] } }>({
      query: ({ id, body }) => ({ url: id === undefined ? '/orme-packing-lists' : `/orme-packing-lists/${id}`, method: id === undefined ? 'POST' : 'PUT', body }),
      invalidatesTags: [...tags],
    }),
    setOrmePackingSaleCustomer: build.mutation<OrmePackingList, { id: number; customerId: number | null }>({
      query: ({ id, customerId }) => ({ url: `/orme-packing-lists/${id}/sale-customer`, method: 'PUT', body: { customerId } }),
      invalidatesTags: [...tags],
    }),
    changeOrmePackingList: build.mutation<OrmePackingList, { id: number; action: Action }>({
      query: ({ id, action }) => ({ url: `/orme-packing-lists/${id}/${action}`, method: 'POST' }),
      invalidatesTags: [...tags],
    }),
    sendOrmePackingToBoyahane: build.mutation<OrmePackingList, { id: number; fabricId: number; customerId: number | null; date: string }>({
      query: ({ id, ...body }) => ({ url: `/orme-packing-lists/${id}/send-to-boyahane`, method: 'POST', body }),
      invalidatesTags: [...tags],
    }),
    getOrmeDespatches: build.query<PagedResult<OrmeDespatchRow>, Record<string, unknown>>({
      query: (params) => ({ url: '/orme-despatches', params }),
      providesTags: ['OrmeShipping'],
    }),
    getOrmeDespatch: build.query<OrmeDespatch, number>({
      query: (id) => `/orme-despatches/${id}`,
      providesTags: ['OrmeShipping'],
    }),
    getOrmeDespatchCandidates: build.query<OrmeDespatchCandidate[], number | undefined>({
      query: (despatchId) => ({ url: '/orme-despatches/candidates', params: despatchId === undefined ? {} : { despatchId } }),
      providesTags: ['OrmeShipping'],
    }),
    saveOrmeDespatch: build.mutation<OrmeDespatch, { id?: number; body: OrmeDespatchRequest }>({
      query: ({ id, body }) => ({ url: id === undefined ? '/orme-despatches' : `/orme-despatches/${id}`, method: id === undefined ? 'POST' : 'PUT', body }),
      invalidatesTags: ['OrmeShipping'],
    }),
    changeOrmeDespatch: build.mutation<OrmeDespatch, { id: number; action: Action }>({
      query: ({ id, action }) => ({ url: `/orme-despatches/${id}/${action}`, method: 'POST' }),
      invalidatesTags: ['OrmeShipping'],
    }),
    getOrmeInvoices: build.query<PagedResult<OrmeInvoiceRow>, Record<string, unknown>>({
      query: (params) => ({ url: '/orme-invoices', params }),
      providesTags: ['OrmeShipping'],
    }),
    getOrmeInvoice: build.query<OrmeInvoice, number>({
      query: (id) => `/orme-invoices/${id}`,
      providesTags: ['OrmeShipping'],
    }),
    getOrmeInvoiceCandidates: build.query<OrmeInvoiceCandidate[], number | undefined>({
      query: (invoiceId) => ({ url: '/orme-invoices/candidates', params: invoiceId === undefined ? {} : { invoiceId } }),
      providesTags: ['OrmeShipping'],
    }),
    saveOrmeInvoice: build.mutation<OrmeInvoice, { id?: number; body: OrmeInvoiceRequest }>({
      query: ({ id, body }) => ({ url: id === undefined ? '/orme-invoices' : `/orme-invoices/${id}`, method: id === undefined ? 'POST' : 'PUT', body }),
      invalidatesTags: ['OrmeShipping'],
    }),
    changeOrmeInvoice: build.mutation<OrmeInvoice, { id: number; action: Action }>({
      query: ({ id, action }) => ({ url: `/orme-invoices/${id}/${action}`, method: 'POST' }),
      invalidatesTags: ['OrmeShipping'],
    }),
    getOrmeYarnReturns: build.query<PagedResult<OrmeYarnReturn>, Record<string, unknown>>({
      query: (params) => ({ url: '/orme-yarn-returns', params }),
      providesTags: ['OrmeYarnLot'],
    }),
    createOrmeYarnReturn: build.mutation<OrmeYarnReturn, { yarnLotId: number; returnDate: string; kilograms: number; notes: string | null }>({
      query: (body) => ({ url: '/orme-yarn-returns', method: 'POST', body }),
      invalidatesTags: ['OrmeYarnLot'],
    }),
    deleteOrmeYarnReturn: build.mutation<null, number>({
      query: (id) => ({ url: `/orme-yarn-returns/${id}`, method: 'DELETE' }),
      invalidatesTags: ['OrmeYarnLot'],
    }),
  }),
})

export const {
  useGetOrmePackingListsQuery,
  useGetOrmePackingListQuery,
  useGetOrmePackingCandidatesQuery,
  useSaveOrmePackingListMutation,
  useSetOrmePackingSaleCustomerMutation,
  useChangeOrmePackingListMutation,
  useSendOrmePackingToBoyahaneMutation,
  useGetOrmeDespatchesQuery,
  useGetOrmeDespatchQuery,
  useGetOrmeDespatchCandidatesQuery,
  useSaveOrmeDespatchMutation,
  useChangeOrmeDespatchMutation,
  useGetOrmeInvoicesQuery,
  useGetOrmeInvoiceQuery,
  useGetOrmeInvoiceCandidatesQuery,
  useSaveOrmeInvoiceMutation,
  useChangeOrmeInvoiceMutation,
  useGetOrmeYarnReturnsQuery,
  useCreateOrmeYarnReturnMutation,
  useDeleteOrmeYarnReturnMutation,
} = ormeShippingApi
