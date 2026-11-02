import { baseApi } from '../../api/baseApi'
import type { PagedResult } from '../../types/api'
import type { DocumentLanguage } from '../../utils/documentText'

export type InvoiceStatus = 'DRAFT' | 'CONFIRMED' | 'CANCELLED'

export const invoiceStatusPresentation: Record<InvoiceStatus, { label: string; tone: string }> = {
  DRAFT: { label: 'Taslak', tone: 'neutral' },
  CONFIRMED: { label: 'Onaylı', tone: 'ok' },
  CANCELLED: { label: 'İptal', tone: 'danger' },
}

export interface InvoiceRow {
  id: number
  invoiceNo: string
  customerName: string
  invoiceDate: string
  dueDate: string | null
  currency: string
  subtotal: number
  status: InvoiceStatus
}

export interface InvoiceLine {
  id: number
  stockItemId: number
  stockItemCode: string
  salesOrderLineId: number | null
  salesOrderNo: string | null
  description: string
  /** Adet. */
  quantity: number
  unitPrice: number | null
  discountPercent: number | null
  vatRate: number
  amount: number | null
  vatAmount: number | null
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
  withholdingPercent: number | null
  notes: string | null
  status: InvoiceStatus
  subtotal: number
  vatAmount: number
  withholdingAmount: number
  payableAmount: number
  paidAmount: number
  isOpening: boolean
  shipments: { id: number; shipmentNo: string; documentNo: string | null; shipDate: string }[]
  lines: InvoiceLine[]
  payments: { paymentId: number; paymentNo: string; paymentDate: string; method: 'CASH' | 'BANK_TRANSFER' | 'CHECK' | 'OPENING'; amount: number }[]
  /** Basılı faturanın dili (müşteri kartından). */
  documentLanguage: DocumentLanguage
}

export interface InvoiceCandidate {
  id: number
  shipmentNo: string
  /** Sevkiyattaki irsaliye numarası. */
  documentNo: string | null
  customerId: number
  customerName: string
  shipDate: string
  salesOrderNo: string | null
  quantity: number
  selected: boolean
}

export interface InvoicePriceRequest {
  stockItemId: number
  salesOrderLineId: number | null
  unitPrice: number | null
  discountPercent: number | null
  vatRate: number
}

export interface InvoiceRequest {
  customerId: number
  invoiceDate: string
  shipmentIds: number[]
  dueDate: string | null
  withholdingPercent: number | null
  notes: string | null
  prices: InvoicePriceRequest[]
}

const invoicingApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getSalesInvoices: build.query<PagedResult<InvoiceRow>, Record<string, unknown>>({
      query: (params) => ({ url: '/invoices', params }),
      providesTags: ['Invoice'],
    }),
    getSalesInvoice: build.query<Invoice, number>({
      query: (id) => `/invoices/${id}`,
      providesTags: ['Invoice'],
    }),
    getSalesInvoiceCandidates: build.query<InvoiceCandidate[], number | undefined>({
      query: (invoiceId) => ({ url: '/invoices/candidates', params: invoiceId === undefined ? {} : { invoiceId } }),
      providesTags: ['Invoice', 'Shipment'],
    }),
    saveSalesInvoice: build.mutation<Invoice, { id?: number; body: InvoiceRequest }>({
      query: ({ id, body }) => ({ url: id === undefined ? '/invoices' : `/invoices/${id}`, method: id === undefined ? 'POST' : 'PUT', body }),
      invalidatesTags: ['Invoice', 'Shipment'],
    }),
    changeSalesInvoice: build.mutation<Invoice, { id: number; action: 'confirm' | 'reopen' | 'cancel' }>({
      query: ({ id, action }) => ({ url: `/invoices/${id}/${action}`, method: 'POST' }),
      invalidatesTags: ['Invoice', 'Shipment'],
    }),
  }),
})

export const {
  useGetSalesInvoicesQuery: useGetInvoicesQuery,
  useGetSalesInvoiceQuery: useGetInvoiceQuery,
  useGetSalesInvoiceCandidatesQuery: useGetInvoiceCandidatesQuery,
  useSaveSalesInvoiceMutation: useSaveInvoiceMutation,
  useChangeSalesInvoiceMutation: useChangeInvoiceMutation,
} = invoicingApi
