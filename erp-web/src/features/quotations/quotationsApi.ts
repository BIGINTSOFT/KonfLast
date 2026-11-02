import { baseApi } from '../../api/baseApi'
import type { GridParams } from '../../components/grid/useGrid'
import type { PagedResult } from '../../types/api'
import type { DocumentLanguage } from '../../utils/documentText'
import type { ProformaParty } from '../salesOrders/salesOrdersApi'
import type { StockUnit } from '../stock/stockApi'

export type QuotationStatus = 'DRAFT' | 'SENT' | 'ACCEPTED' | 'REJECTED' | 'EXPIRED'

export interface CostSetting {
  laborCostPerMinute: number
  energyCostPerMinute: number
  currency: string
  overheadPercent: number
  defaultFirePercent: number
  defaultMarginPercent: number
  updatedAt: string | null
  updatedBy: string | null
}

export type CostSettingRequest = Omit<CostSetting, 'updatedAt' | 'updatedBy'>

export interface QuotationListItem {
  id: number
  quotationNo: string
  customerId: number | null
  /** Müşteri kartının adı; aday müşteride adayın adı. */
  customerName: string
  isProspect: boolean
  quotationDate: string
  validUntil: string
  currency: string
  status: QuotationStatus
  lineCount: number
  totalQuantity: number
  totalAmount: number
  /** Gönderilmiş teklifin geçerlilik tarihi geçti. */
  isPastValidity: boolean
  salesOrderId: number | null
  salesOrderNo: string | null
}

export interface QuotationMaterial {
  id: number
  stockItemId: number | null
  itemCode: string | null
  description: string
  unit: StockUnit | null
  /** Parça başı miktar, fire dahil. */
  quantityPerPiece: number
  /** Birim maliyet (teklifin para biriminde). */
  unitCost: number
  /** Birim maliyet elle girildi; boşsa stok kartından okunur. */
  unitCostOverridden: boolean
  cost: number
}

export interface QuotationLine {
  id: number
  lineNo: number
  productId: number | null
  productCode: string | null
  productName: string | null
  modelCode: string | null
  description: string | null
  modelReference: string | null
  colors: string | null
  quantity: number
  /** Elle girilen dakika; boşsa etkin rotanın fason olmayan adımlarının SAM toplamı. */
  manualMinutes: number | null
  /** Elle girilen fason maliyeti; boşsa rotadaki fason adımların fiyatları. */
  manualSubcontractCost: number | null
  priceOverridden: boolean
  firePercent: number
  overheadPercent: number
  marginPercent: number
  materialCost: number
  laborMinutes: number
  laborCost: number
  energyCost: number
  subcontractCost: number
  /** Etkin rotanın fason olmayan adımlarının SAM toplamı; rota yoksa boş. */
  routeMinutes: number | null
  /** Rotadaki fason adımların fiyatlarından gelen fason maliyeti. */
  routeSubcontractCost: number
  fireCost: number
  overheadCost: number
  unitCost: number
  unitPrice: number
  amount: number
  warnings: string[]
  materials: QuotationMaterial[]
}

export interface Quotation {
  id: number
  quotationNo: string
  customerId: number | null
  customerCode: string | null
  customerName: string | null
  prospectName: string | null
  quotationDate: string
  validUntil: string
  currency: string
  documentLanguage: DocumentLanguage
  status: QuotationStatus
  notes: string | null
  /** Kayıtta ayardan okunan dakika maliyetleri (teklifin para biriminde). */
  laborCostPerMinute: number
  energyCostPerMinute: number
  isEditable: boolean
  allowedTransitions: QuotationStatus[]
  isPastValidity: boolean
  salesOrderId: number | null
  salesOrderNo: string | null
  sentAt: string | null
  decidedAt: string | null
  decidedBy: string | null
  totalQuantity: number
  totalCost: number
  totalAmount: number
  rowVersion: string
  createdAt: string
  createdBy: string | null
  lines: QuotationLine[]
}

export interface QuotationMaterialRequest {
  stockItemId: number | null
  description: string | null
  quantityPerPiece: number
  /** Boşsa stok kartının maliyeti çevrilerek kullanılır. */
  unitCost: number | null
}

export interface QuotationLineRequest {
  productId: number | null
  description: string | null
  modelReference: string | null
  colors: string | null
  quantity: number
  manualMinutes: number | null
  manualSubcontractCost: number | null
  firePercent: number | null
  overheadPercent: number | null
  marginPercent: number | null
  /** Elle birim fiyat; boşsa maliyet × (1 + marj). */
  unitPrice: number | null
  /** Boşsa (null) ürünün reçetesinden yüklenir. */
  materials: QuotationMaterialRequest[] | null
}

export interface QuotationRequest {
  customerId: number | null
  prospectName: string | null
  quotationDate: string
  validUntil: string
  currency: string | null
  documentLanguage: DocumentLanguage | null
  notes: string | null
  lines: QuotationLineRequest[]
  rowVersion?: string
}

export interface QuotationAcceptRequest {
  rowVersion: string
  customerId: number | null
  dueDate: string
  customerOrderNo: string | null
  lines: { quotationLineId: number; colorId: number; sizeId: number; quantity: number }[]
}

export interface QuotationDocument {
  quotation: Quotation
  seller: ProformaParty | null
  customer: ProformaParty
  customerAddress: string | null
  printedAt: string
}

export type QuotationAction = 'send' | 'reject' | 'expire' | 'reopen'

export type QuotationListParams = {
  page: number
  pageSize: number
  search?: string
  status?: QuotationStatus
  customerId?: number
} & GridParams

export const quotationsApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getCostSetting: build.query<CostSetting, void>({
      query: () => '/cost-settings',
      providesTags: ['Quotation'],
    }),
    saveCostSetting: build.mutation<CostSetting, CostSettingRequest>({
      query: (body) => ({ url: '/cost-settings', method: 'PUT', body }),
      invalidatesTags: ['Quotation'],
    }),
    getQuotations: build.query<PagedResult<QuotationListItem>, QuotationListParams>({
      query: (params) => ({ url: '/quotations', params }),
      providesTags: ['Quotation'],
    }),
    getQuotation: build.query<Quotation, number>({
      query: (id) => `/quotations/${id}`,
      providesTags: ['Quotation'],
    }),
    getQuotationDocument: build.query<QuotationDocument, number>({
      query: (id) => `/quotations/${id}/document`,
      providesTags: ['Quotation'],
    }),
    previewQuotationLine: build.mutation<QuotationLine, { currency: string | null; date: string | null; line: QuotationLineRequest }>({
      query: (body) => ({ url: '/quotations/line-preview', method: 'POST', body }),
    }),
    saveQuotation: build.mutation<Quotation, { id?: number; body: QuotationRequest }>({
      query: ({ id, body }) => ({ url: id ? `/quotations/${id}` : '/quotations', method: id ? 'PUT' : 'POST', body }),
      invalidatesTags: ['Quotation'],
    }),
    deleteQuotation: build.mutation<null, number>({
      query: (id) => ({ url: `/quotations/${id}`, method: 'DELETE' }),
      invalidatesTags: ['Quotation'],
    }),
    actOnQuotation: build.mutation<Quotation, { id: number; action: QuotationAction; rowVersion: string }>({
      query: ({ id, action, rowVersion }) => ({ url: `/quotations/${id}/${action}`, method: 'POST', body: { rowVersion } }),
      invalidatesTags: ['Quotation'],
    }),
    acceptQuotation: build.mutation<Quotation, { id: number; body: QuotationAcceptRequest }>({
      query: ({ id, body }) => ({ url: `/quotations/${id}/accept`, method: 'POST', body }),
      invalidatesTags: ['Quotation', 'SalesOrder'],
    }),
  }),
})

export const {
  useGetCostSettingQuery,
  useSaveCostSettingMutation,
  useGetQuotationsQuery,
  useGetQuotationQuery,
  useGetQuotationDocumentQuery,
  usePreviewQuotationLineMutation,
  useSaveQuotationMutation,
  useDeleteQuotationMutation,
  useActOnQuotationMutation,
  useAcceptQuotationMutation,
} = quotationsApi
