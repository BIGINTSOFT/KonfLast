import { baseApi } from '../../api/baseApi'
import type { PagedResult } from '../../types/api'
import type { DokumaMaterialUnit } from './dokumaWarpApi'

export type DokumaPurchaseStatus = 'DRAFT' | 'APPROVED' | 'PARTIALLY_RECEIVED' | 'RECEIVED' | 'CLOSED' | 'CANCELLED'
export type DokumaPurchaseItemKind = 'YARN' | 'MATERIAL'

export const dokumaPurchaseStatusPresentation: Record<DokumaPurchaseStatus, { label: string; tone: string }> = {
  DRAFT: { label: 'Taslak', tone: 'neutral' },
  APPROVED: { label: 'Onaylı', tone: 'info' },
  PARTIALLY_RECEIVED: { label: 'Kısmi teslim', tone: 'warning' },
  RECEIVED: { label: 'Teslim alındı', tone: 'ok' },
  CLOSED: { label: 'Kapatıldı', tone: 'neutral' },
  CANCELLED: { label: 'İptal', tone: 'danger' },
}

export const dokumaPurchaseItemLabels: Record<DokumaPurchaseItemKind, string> = {
  YARN: 'İplik',
  MATERIAL: 'Malzeme',
}

export interface DokumaPurchaseOrderListItem {
  id: number
  orderNo: string
  supplierId: number
  supplierName: string
  orderDate: string
  dueDate: string | null
  lineCount: number
  /** Teslimi tamamlanmamış satır sayısı. */
  openLineCount: number
  amount: number
  currency: string
  status: DokumaPurchaseStatus
}

export interface DokumaPurchaseOrderLine {
  id: number
  sequence: number
  kind: DokumaPurchaseItemKind
  yarnId: number | null
  materialId: number | null
  itemCode: string
  itemName: string
  /** İplikte renk. */
  itemDetail: string | null
  quantity: number
  unit: DokumaMaterialUnit
  unitPrice: number | null
  amount: number | null
  /** Satırın termini; boşsa siparişin termini. */
  dueDate: string | null
  receivedQuantity: number
  openQuantity: number
  notes: string | null
  lots: { id: number; lotNo: string; receiptDate: string; supplierLotNo: string | null; quantity: number }[]
}

export interface DokumaPurchaseOrder {
  id: number
  orderNo: string
  supplierId: number
  supplierCode: string
  supplierName: string
  supplierTaxNumber: string | null
  supplierPhone: string | null
  supplierEmail: string | null
  orderDate: string
  dueDate: string | null
  currency: string
  notes: string | null
  status: DokumaPurchaseStatus
  approvedAt: string | null
  approvedBy: string | null
  createdAt: string
  createdBy: string | null
  amount: number
  hasMissingPrices: boolean
  lines: DokumaPurchaseOrderLine[]
}

export interface DokumaPurchaseOrderRequest {
  supplierId: number
  orderDate: string
  dueDate: string | null
  currency: string | null
  notes: string | null
  lines: { yarnId: number | null; materialId: number | null; quantity: number; unitPrice: number | null; dueDate: string | null; notes: string | null }[]
}

export interface DokumaPurchaseReceiptRequest {
  receiptDate: string
  despatchNo: string | null
  lines: { lineId: number; quantity: number; lotNo: string | null; packageCount: number | null; notes: string | null }[]
}

export interface DokumaSupplySuggestion {
  kind: DokumaPurchaseItemKind
  itemId: number
  code: string
  name: string
  unit: DokumaMaterialUnit
  /** Malzemede kritik stok; iplikte onaylı kendi malı siparişlerin kalan fireli ihtiyacı. */
  requiredQuantity: number
  availableQuantity: number
  onOrderQuantity: number
  suggestedQuantity: number
  supplierId: number | null
  supplierName: string | null
  orderNos: string[]
}

export interface DokumaPurchaseDraft {
  id: number
  orderNo: string
  supplierName: string
  lineCount: number
}

// Teslim alma iplik ve malzeme lotu açar: iplik girişleriyle aynı etiket.
const purchaseTags = ['DokumaYarnLot'] as const

export const dokumaPurchasingApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getDokumaPurchaseOrders: build.query<PagedResult<DokumaPurchaseOrderListItem>, Record<string, unknown>>({
      query: (params) => ({ url: '/dokuma-purchase-orders', params }),
      providesTags: [...purchaseTags],
    }),
    getDokumaPurchaseOrder: build.query<DokumaPurchaseOrder, number>({
      query: (id) => `/dokuma-purchase-orders/${id}`,
      providesTags: [...purchaseTags],
    }),
    getDokumaSupplySuggestions: build.query<DokumaSupplySuggestion[], void>({
      query: () => '/dokuma-purchase-orders/suggestions',
      providesTags: [...purchaseTags, 'DokumaOrder', 'DokumaWarp', 'MasterData'],
    }),
    saveDokumaPurchaseOrder: build.mutation<DokumaPurchaseOrder, { id?: number; body: DokumaPurchaseOrderRequest }>({
      query: ({ id, body }) => ({ url: id === undefined ? '/dokuma-purchase-orders' : `/dokuma-purchase-orders/${id}`, method: id === undefined ? 'POST' : 'PUT', body }),
      invalidatesTags: [...purchaseTags],
    }),
    deleteDokumaPurchaseOrder: build.mutation<null, number>({
      query: (id) => ({ url: `/dokuma-purchase-orders/${id}`, method: 'DELETE' }),
      invalidatesTags: [...purchaseTags],
    }),
    changeDokumaPurchaseOrder: build.mutation<DokumaPurchaseOrder, { id: number; action: 'approve' | 'reopen' | 'cancel' | 'close' }>({
      query: ({ id, action }) => ({ url: `/dokuma-purchase-orders/${id}/${action}`, method: 'POST' }),
      invalidatesTags: [...purchaseTags],
    }),
    receiveDokumaPurchaseOrder: build.mutation<DokumaPurchaseOrder, { id: number; body: DokumaPurchaseReceiptRequest }>({
      query: ({ id, body }) => ({ url: `/dokuma-purchase-orders/${id}/receive`, method: 'POST', body }),
      invalidatesTags: [...purchaseTags, 'DokumaOrder'],
    }),
    createDokumaPurchaseDrafts: build.mutation<DokumaPurchaseDraft[], { orderDate: string | null; lines: { kind: DokumaPurchaseItemKind; itemId: number; quantity: number; supplierId: number }[] }>({
      query: (body) => ({ url: '/dokuma-purchase-orders/from-suggestions', method: 'POST', body }),
      invalidatesTags: [...purchaseTags],
    }),
  }),
})

export const {
  useGetDokumaPurchaseOrdersQuery,
  useGetDokumaPurchaseOrderQuery,
  useGetDokumaSupplySuggestionsQuery,
  useSaveDokumaPurchaseOrderMutation,
  useDeleteDokumaPurchaseOrderMutation,
  useChangeDokumaPurchaseOrderMutation,
  useReceiveDokumaPurchaseOrderMutation,
  useCreateDokumaPurchaseDraftsMutation,
} = dokumaPurchasingApi
