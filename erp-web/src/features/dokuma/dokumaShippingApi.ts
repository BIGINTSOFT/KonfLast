import { baseApi } from '../../api/baseApi'
import type { PagedResult } from '../../types/api'
import type { DocumentLanguage, DocumentTextKey } from '../../utils/documentText'
import type { DokumaFabricRollStatus } from './dokumaWeavingApi'

export type DokumaDocumentStatus = 'DRAFT' | 'CONFIRMED' | 'CANCELLED'

export const dokumaDocumentStatusPresentation: Record<DokumaDocumentStatus, { label: string; tone: string }> = {
  DRAFT: { label: 'Taslak', tone: 'neutral' },
  CONFIRMED: { label: 'Onaylı', tone: 'ok' },
  CANCELLED: { label: 'İptal', tone: 'danger' },
}

export const dokumaGradeLabels: Record<string, string> = {
  FIRST_QUALITY: '1. kalite',
  SECOND_QUALITY: '2. kalite',
}

/** Baskıda kalitenin belge dilindeki metni: `t(dokumaGradeTextKey(item.grade))`. */
export const dokumaGradeTextKey = (grade: DokumaFabricRollStatus): DocumentTextKey => (grade === 'SECOND_QUALITY' ? 'secondQuality' : 'firstQuality')

export interface DokumaPackingListRow {
  id: number
  packingNo: string
  orderNo: string
  customerOrderNo: string | null
  customerName: string | null
  packingDate: string
  rollCount: number
  totalMeters: number
  status: DokumaDocumentStatus
  despatchNo: string | null
  boyahaneReceiptNo: string | null
}

export interface DokumaPackingItem {
  id: number
  rollId: number
  rollNo: string
  beamNo: string
  loomCode: string
  articleCode: string
  articleName: string
  meters: number
  kilograms: number | null
  grade: DokumaFabricRollStatus
  /** Müşterinin artikel için kullandığı kendi kodu. */
  customerArticleCode: string | null
}

export interface DokumaPackingList {
  id: number
  packingNo: string
  orderId: number
  orderNo: string
  customerOrderNo: string | null
  customerName: string | null
  packingDate: string
  notes: string | null
  status: DokumaDocumentStatus
  totalMeters: number
  items: DokumaPackingItem[]
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
  /** Müşterinin belge dili; çıktı bu dilde basılır. */
  documentLanguage: DocumentLanguage
}

export interface DokumaPackingCandidate {
  rollId: number
  orderId: number
  orderNo: string
  customerName: string | null
  rollNo: string
  beamNo: string
  articleCode: string
  meters: number
  kilograms: number | null
  grade: DokumaFabricRollStatus
  selected: boolean
}

export interface DokumaDespatchRow {
  id: number
  despatchNo: string
  customerName: string
  despatchDate: string
  vehiclePlate: string | null
  packingListCount: number
  totalMeters: number
  status: DokumaDocumentStatus
  invoiceNo: string | null
}

export interface DokumaDespatchLine {
  orderNo: string
  articleCode: string
  articleName: string
  grade: DokumaFabricRollStatus
  rollCount: number
  meters: number
  /** Bütün toplar tartıldıysa toplam kilo. */
  kilograms: number | null
  customerOrderNo: string | null
  customerArticleCode: string | null
}

export interface DokumaDespatch {
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
  status: DokumaDocumentStatus
  invoiceId: number | null
  invoiceNo: string | null
  totalMeters: number
  totalRolls: number
  packingLists: { id: number; packingNo: string; orderNo: string; rollCount: number; meters: number }[]
  lines: DokumaDespatchLine[]
  customerCode: string
  documentLanguage: DocumentLanguage
  /** İrsaliyedeki topların numaraları (yükleme kontrolü). */
  rollNos: string[]
}

export interface DokumaDespatchCandidate {
  id: number
  packingNo: string
  customerId: number
  customerName: string
  orderNo: string
  rollCount: number
  meters: number
  selected: boolean
}

export interface DokumaDespatchRequest {
  customerId: number
  despatchDate: string
  packingListIds: number[]
  vehiclePlate: string | null
  driverName: string | null
  deliveryAddress: string | null
  notes: string | null
}

export interface DokumaInvoiceRow {
  id: number
  invoiceNo: string
  customerName: string
  invoiceDate: string
  dueDate: string | null
  currency: string
  subtotal: number
  status: DokumaDocumentStatus
}

export interface DokumaInvoiceLine {
  id: number
  orderLineId: number
  orderNo: string
  grade: DokumaFabricRollStatus
  description: string
  quantity: number
  unitPrice: number | null
  discountPercent: number | null
  amount: number | null
  customerOrderNo: string | null
  customerArticleCode: string | null
}

export interface DokumaInvoice {
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
  status: DokumaDocumentStatus
  subtotal: number
  vatAmount: number
  withholdingAmount: number
  payableAmount: number
  paidAmount: number
  isOpening: boolean
  payments: { paymentId: number; paymentNo: string; paymentDate: string; method: 'CASH' | 'BANK_TRANSFER' | 'CHECK' | 'OPENING'; amount: number }[]
  despatchNos: string[]
  despatchIds: number[]
  lines: DokumaInvoiceLine[]
  documentLanguage: DocumentLanguage
}

export interface DokumaInvoiceCandidate {
  id: number
  despatchNo: string
  customerId: number
  customerName: string
  despatchDate: string
  meters: number
  selected: boolean
}

export interface DokumaInvoiceRequest {
  customerId: number
  invoiceDate: string
  despatchIds: number[]
  dueDate: string | null
  vatRate: number | null
  withholdingPercent: number | null
  notes: string | null
  prices: { orderLineId: number; grade: DokumaFabricRollStatus; unitPrice: number | null; discountPercent: number | null }[]
}

export interface DokumaYarnReturn {
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

export interface DokumaOrderWaste {
  orderId: number
  wovenMeters: number
  allBeamsFinished: boolean
  /** Müşterinin kabul ettiği iplik firesi; fasonda karşılaştırılır. */
  tolerancePercent: number | null
  totalWastePercent: number
  exceedsTolerance: boolean
  yarns: { yarnId: number; yarnCode: string; yarnName: string; issuedKilograms: number; theoreticalKilograms: number; wasteKilograms: number; wastePercent: number }[]
}

type Action = 'confirm' | 'reopen' | 'cancel'

const tags = ['DokumaShipping', 'DokumaWeaving', 'DokumaOrder'] as const

export const dokumaShippingApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getDokumaPackingLists: build.query<PagedResult<DokumaPackingListRow>, Record<string, unknown>>({
      query: (params) => ({ url: '/dokuma-packing-lists', params }),
      providesTags: ['DokumaShipping'],
    }),
    getDokumaPackingList: build.query<DokumaPackingList, number>({
      query: (id) => `/dokuma-packing-lists/${id}`,
      providesTags: ['DokumaShipping'],
    }),
    getDokumaPackingCandidates: build.query<DokumaPackingCandidate[], number | undefined>({
      query: (packingListId) => ({ url: '/dokuma-packing-lists/candidates', params: packingListId === undefined ? {} : { packingListId } }),
      providesTags: ['DokumaShipping', 'DokumaWeaving'],
    }),
    saveDokumaPackingList: build.mutation<DokumaPackingList, { id?: number; body: { packingDate: string; notes: string | null; rollIds: number[] } }>({
      query: ({ id, body }) => ({ url: id === undefined ? '/dokuma-packing-lists' : `/dokuma-packing-lists/${id}`, method: id === undefined ? 'POST' : 'PUT', body }),
      invalidatesTags: [...tags],
    }),
    setDokumaPackingSaleCustomer: build.mutation<DokumaPackingList, { id: number; customerId: number | null }>({
      query: ({ id, customerId }) => ({ url: `/dokuma-packing-lists/${id}/sale-customer`, method: 'PUT', body: { customerId } }),
      invalidatesTags: [...tags],
    }),
    changeDokumaPackingList: build.mutation<DokumaPackingList, { id: number; action: Action }>({
      query: ({ id, action }) => ({ url: `/dokuma-packing-lists/${id}/${action}`, method: 'POST' }),
      invalidatesTags: [...tags],
    }),
    sendDokumaPackingToBoyahane: build.mutation<DokumaPackingList, { id: number; fabricId: number; customerId: number | null; date: string }>({
      query: ({ id, ...body }) => ({ url: `/dokuma-packing-lists/${id}/send-to-boyahane`, method: 'POST', body }),
      invalidatesTags: [...tags],
    }),
    getDokumaDespatches: build.query<PagedResult<DokumaDespatchRow>, Record<string, unknown>>({
      query: (params) => ({ url: '/dokuma-despatches', params }),
      providesTags: ['DokumaShipping'],
    }),
    getDokumaDespatch: build.query<DokumaDespatch, number>({
      query: (id) => `/dokuma-despatches/${id}`,
      providesTags: ['DokumaShipping'],
    }),
    getDokumaDespatchCandidates: build.query<DokumaDespatchCandidate[], number | undefined>({
      query: (despatchId) => ({ url: '/dokuma-despatches/candidates', params: despatchId === undefined ? {} : { despatchId } }),
      providesTags: ['DokumaShipping'],
    }),
    saveDokumaDespatch: build.mutation<DokumaDespatch, { id?: number; body: DokumaDespatchRequest }>({
      query: ({ id, body }) => ({ url: id === undefined ? '/dokuma-despatches' : `/dokuma-despatches/${id}`, method: id === undefined ? 'POST' : 'PUT', body }),
      invalidatesTags: ['DokumaShipping'],
    }),
    changeDokumaDespatch: build.mutation<DokumaDespatch, { id: number; action: Action }>({
      query: ({ id, action }) => ({ url: `/dokuma-despatches/${id}/${action}`, method: 'POST' }),
      invalidatesTags: ['DokumaShipping'],
    }),
    getDokumaInvoices: build.query<PagedResult<DokumaInvoiceRow>, Record<string, unknown>>({
      query: (params) => ({ url: '/dokuma-invoices', params }),
      providesTags: ['DokumaShipping'],
    }),
    getDokumaInvoice: build.query<DokumaInvoice, number>({
      query: (id) => `/dokuma-invoices/${id}`,
      providesTags: ['DokumaShipping'],
    }),
    getDokumaInvoiceCandidates: build.query<DokumaInvoiceCandidate[], number | undefined>({
      query: (invoiceId) => ({ url: '/dokuma-invoices/candidates', params: invoiceId === undefined ? {} : { invoiceId } }),
      providesTags: ['DokumaShipping'],
    }),
    saveDokumaInvoice: build.mutation<DokumaInvoice, { id?: number; body: DokumaInvoiceRequest }>({
      query: ({ id, body }) => ({ url: id === undefined ? '/dokuma-invoices' : `/dokuma-invoices/${id}`, method: id === undefined ? 'POST' : 'PUT', body }),
      invalidatesTags: ['DokumaShipping'],
    }),
    changeDokumaInvoice: build.mutation<DokumaInvoice, { id: number; action: Action }>({
      query: ({ id, action }) => ({ url: `/dokuma-invoices/${id}/${action}`, method: 'POST' }),
      invalidatesTags: ['DokumaShipping'],
    }),
    getDokumaYarnReturns: build.query<PagedResult<DokumaYarnReturn>, Record<string, unknown>>({
      query: (params) => ({ url: '/dokuma-yarn-returns', params }),
      providesTags: ['DokumaYarnLot'],
    }),
    createDokumaYarnReturn: build.mutation<DokumaYarnReturn, { yarnLotId: number; returnDate: string; kilograms: number; notes: string | null }>({
      query: (body) => ({ url: '/dokuma-yarn-returns', method: 'POST', body }),
      invalidatesTags: ['DokumaYarnLot'],
    }),
    deleteDokumaYarnReturn: build.mutation<null, number>({
      query: (id) => ({ url: `/dokuma-yarn-returns/${id}`, method: 'DELETE' }),
      invalidatesTags: ['DokumaYarnLot'],
    }),
    getDokumaOrderWaste: build.query<DokumaOrderWaste, number>({
      query: (id) => `/dokuma-orders/${id}/yarn-waste`,
      providesTags: ['DokumaOrder', 'DokumaWeaving', 'DokumaWarp'],
    }),
  }),
})

export const {
  useGetDokumaPackingListsQuery,
  useGetDokumaPackingListQuery,
  useGetDokumaPackingCandidatesQuery,
  useSaveDokumaPackingListMutation,
  useSetDokumaPackingSaleCustomerMutation,
  useChangeDokumaPackingListMutation,
  useSendDokumaPackingToBoyahaneMutation,
  useGetDokumaDespatchesQuery,
  useGetDokumaDespatchQuery,
  useGetDokumaDespatchCandidatesQuery,
  useSaveDokumaDespatchMutation,
  useChangeDokumaDespatchMutation,
  useGetDokumaInvoicesQuery,
  useGetDokumaInvoiceQuery,
  useGetDokumaInvoiceCandidatesQuery,
  useSaveDokumaInvoiceMutation,
  useChangeDokumaInvoiceMutation,
  useGetDokumaYarnReturnsQuery,
  useCreateDokumaYarnReturnMutation,
  useDeleteDokumaYarnReturnMutation,
  useGetDokumaOrderWasteQuery,
} = dokumaShippingApi
