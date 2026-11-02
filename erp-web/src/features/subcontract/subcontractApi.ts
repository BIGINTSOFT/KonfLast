import { baseApi } from '../../api/baseApi'
import type { GridParams } from '../../components/grid/useGrid'
import type { PagedResult } from '../../types/api'
import type { ProformaParty } from '../salesOrders/salesOrdersApi'

export type SubcontractSendStatus = 'OPEN' | 'PARTIALLY_RETURNED' | 'CLOSED' | 'CANCELLED'

export interface CurrencyAmount {
  currency: string
  amount: number
}

export interface SubcontractPrice {
  id: number
  supplierId: number
  supplierCode: string
  supplierName: string
  operationId: number
  operationCode: string
  operationName: string
  /** Boşsa operasyonun bütün ürünleri için. */
  productId: number | null
  productCode: string | null
  productName: string | null
  unitPrice: number
  currency: string
  validFrom: string
  notes: string | null
  /** Bugün geçerli olan fiyat. */
  isCurrent: boolean
  updatedAt: string | null
  updatedBy: string | null
}

export interface SubcontractPriceRequest {
  supplierId: number
  operationId: number
  productId: number | null
  unitPrice: number
  currency: string
  validFrom: string
  notes: string | null
}

export interface ResolvedSubcontractPrice {
  supplierId: number
  operationId: number
  productId: number | null
  date: string
  priceId: number | null
  unitPrice: number | null
  currency: string | null
  validFrom: string | null
}

export interface SubcontractOperationOption {
  operationId: number
  operationCode: string
  operationName: string
  defaultSupplierId: number | null
  defaultSupplierName: string | null
}

export interface SubcontractCandidate {
  bundleOperationId: number
  bundleId: number
  bundleNo: string
  productionOrderId: number
  orderNo: string
  productId: number
  productCode: string
  modelCode: string
  colorId: number
  colorName: string
  sizeId: number
  sizeName: string
  sequenceNo: number
  /** Operasyonda kalan (gönderilecek) adet. */
  quantity: number
  /** Rotada bu adım fason olarak işaretli. */
  isSubcontractedStep: boolean
  defaultSupplierId: number | null
}

export interface SubcontractSendListItem {
  id: number
  sendNo: string
  supplierId: number
  supplierName: string
  sendDate: string
  operationId: number
  operationCode: string
  operationName: string
  status: SubcontractSendStatus
  lineCount: number
  totalQuantity: number
  returnedGood: number
  returnedDefective: number
  returnedMissing: number
  /** Hâlâ fasoncuda olan adet. */
  outstanding: number
  createdBy: string | null
}

export interface SubcontractSendLine {
  id: number
  bundleId: number | null
  bundleNo: string | null
  bundleOperationId: number | null
  productionOrderId: number | null
  orderNo: string | null
  productId: number
  productCode: string
  productName: string
  modelCode: string | null
  colorId: number
  colorName: string
  sizeId: number
  sizeName: string
  sizeSortOrder: number
  quantity: number
  returnedGood: number
  returnedDefective: number
  returnedMissing: number
  outstanding: number
  unitPrice: number | null
  currency: string
}

export interface SubcontractReturnSummary {
  id: number
  returnNo: string
  returnDate: string
  supplierDocumentNo: string | null
  goodQuantity: number
  defectiveQuantity: number
  missingQuantity: number
  amounts: CurrencyAmount[]
  isPaid: boolean
  paidDate: string | null
  createdBy: string | null
}

export interface SubcontractSend {
  id: number
  sendNo: string
  supplierId: number
  supplierCode: string
  supplierName: string
  sendDate: string
  operationId: number
  operationCode: string
  operationName: string
  status: SubcontractSendStatus
  carrier: string | null
  notes: string | null
  canCancel: boolean
  canReturn: boolean
  totalQuantity: number
  outstanding: number
  /** Fiş açıksa gönderimden bugüne geçen gün. */
  daysOutside: number | null
  rowVersion: string
  createdAt: string
  createdBy: string | null
  lines: SubcontractSendLine[]
  returns: SubcontractReturnSummary[]
}

export interface SubcontractSendRequest {
  supplierId: number
  sendDate: string
  operationId: number
  carrier: string | null
  notes: string | null
  bundleOperationIds: number[]
  items: { productId: number; colorId: number; sizeId: number; quantity: number }[]
}

export interface SubcontractSendDocument {
  send: SubcontractSend
  sender: ProformaParty | null
  receiver: ProformaParty
  printedAt: string
}

export interface SubcontractReturnListItem {
  id: number
  returnNo: string
  returnDate: string
  subcontractSendId: number
  sendNo: string
  supplierId: number
  supplierName: string
  operationName: string
  supplierDocumentNo: string | null
  goodQuantity: number
  defectiveQuantity: number
  missingQuantity: number
  /** Fiyatı olmayan satır var; tutar eksik olabilir. */
  hasUnpricedLines: boolean
  isPaid: boolean
  paidDate: string | null
  amounts: CurrencyAmount[]
}

export interface SubcontractReturnLine {
  id: number
  sendLineId: number
  bundleNo: string | null
  orderNo: string | null
  productCode: string
  productName: string
  colorName: string
  sizeName: string
  sentQuantity: number
  goodQuantity: number
  defectiveQuantity: number
  missingQuantity: number
  defectTypeId: number | null
  defectTypeName: string | null
  unitPrice: number | null
  currency: string
  amount: number
}

export interface SubcontractReturn {
  id: number
  returnNo: string
  returnDate: string
  subcontractSendId: number
  sendNo: string
  sendDate: string
  supplierId: number
  supplierName: string
  operationId: number
  operationName: string
  supplierDocumentNo: string | null
  notes: string | null
  isPaid: boolean
  paidDate: string | null
  paidBy: string | null
  paymentNote: string | null
  rowVersion: string
  createdAt: string
  createdBy: string | null
  amounts: CurrencyAmount[]
  lines: SubcontractReturnLine[]
}

export interface SubcontractReturnRequest {
  subcontractSendId: number
  rowVersion: string
  returnDate: string
  supplierDocumentNo: string | null
  notes: string | null
  lines: {
    sendLineId: number
    goodQuantity: number
    defectiveQuantity: number
    missingQuantity: number
    defectTypeId: number | null
    unitPrice: number | null
  }[]
}

export interface SubcontractStatementSupplier {
  supplierId: number
  supplierName: string
  returnCount: number
  goodQuantity: number
  total: CurrencyAmount[]
  paid: CurrencyAmount[]
  unpaid: CurrencyAmount[]
}

export interface SubcontractStatement {
  suppliers: SubcontractStatementSupplier[]
  total: CurrencyAmount[]
  paid: CurrencyAmount[]
  unpaid: CurrencyAmount[]
}

export type SubcontractPriceListParams = {
  page: number
  pageSize: number
  search?: string
  supplierId?: number
  operationId?: number
  currentOnly?: boolean
} & GridParams

export type SubcontractSendListParams = {
  page: number
  pageSize: number
  search?: string
  supplierId?: number
  openOnly?: boolean
} & GridParams

export type SubcontractReturnListParams = {
  page: number
  pageSize: number
  search?: string
  supplierId?: number
  subcontractSendId?: number
  from?: string
  to?: string
  paid?: boolean
} & GridParams

export const subcontractApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getSubcontractPrices: build.query<PagedResult<SubcontractPrice>, SubcontractPriceListParams>({
      query: (params) => ({ url: '/subcontract-prices', params }),
      providesTags: ['Subcontract'],
    }),
    saveSubcontractPrice: build.mutation<SubcontractPrice, { id?: number; body: SubcontractPriceRequest }>({
      query: ({ id, body }) => ({ url: id ? `/subcontract-prices/${id}` : '/subcontract-prices', method: id ? 'PUT' : 'POST', body }),
      invalidatesTags: ['Subcontract'],
    }),
    deleteSubcontractPrice: build.mutation<null, number>({
      query: (id) => ({ url: `/subcontract-prices/${id}`, method: 'DELETE' }),
      invalidatesTags: ['Subcontract'],
    }),
    resolveSubcontractPrice: build.query<ResolvedSubcontractPrice, { supplierId: number; operationId: number; productId?: number; date: string }>({
      query: (params) => ({ url: '/subcontract-prices/resolve', params }),
      providesTags: ['Subcontract'],
    }),
    getSubcontractOperationOptions: build.query<SubcontractOperationOption[], void>({
      query: () => '/subcontract-sends/operation-options',
      providesTags: ['Subcontract', 'ProductionRoute'],
    }),
    getSubcontractCandidates: build.query<SubcontractCandidate[], { operationId: number; search?: string; productionOrderId?: number }>({
      query: (params) => ({ url: '/subcontract-sends/candidates', params: { ...params, take: 300 } }),
      providesTags: ['Subcontract', 'Bundle'],
      keepUnusedDataFor: 0,
    }),
    getSubcontractSends: build.query<PagedResult<SubcontractSendListItem>, SubcontractSendListParams>({
      query: (params) => ({ url: '/subcontract-sends', params }),
      providesTags: ['Subcontract'],
    }),
    getSubcontractSend: build.query<SubcontractSend, number>({
      query: (id) => `/subcontract-sends/${id}`,
      providesTags: ['Subcontract'],
    }),
    getSubcontractSendDocument: build.query<SubcontractSendDocument, number>({
      query: (id) => `/subcontract-sends/${id}/document`,
      providesTags: ['Subcontract'],
    }),
    createSubcontractSend: build.mutation<SubcontractSend, SubcontractSendRequest>({
      query: (body) => ({ url: '/subcontract-sends', method: 'POST', body }),
      // Gönderilen demetler sahadan çıkar; demet, ara stok ve sipariş durumları değişir.
      invalidatesTags: ['Subcontract', 'Bundle', 'ProductionTracking', 'ProductionOrder'],
    }),
    cancelSubcontractSend: build.mutation<SubcontractSend, { id: number; rowVersion: string }>({
      query: ({ id, rowVersion }) => ({ url: `/subcontract-sends/${id}/cancel`, method: 'POST', body: { rowVersion } }),
      invalidatesTags: ['Subcontract', 'Bundle', 'ProductionTracking'],
    }),
    getSubcontractReturns: build.query<PagedResult<SubcontractReturnListItem>, SubcontractReturnListParams>({
      query: (params) => ({ url: '/subcontract-returns', params }),
      providesTags: ['Subcontract'],
    }),
    getSubcontractStatement: build.query<SubcontractStatement, { supplierId?: number; from?: string; to?: string }>({
      query: (params) => ({ url: '/subcontract-returns/statement', params }),
      providesTags: ['Subcontract'],
    }),
    getSubcontractReturn: build.query<SubcontractReturn, number>({
      query: (id) => `/subcontract-returns/${id}`,
      providesTags: ['Subcontract'],
    }),
    createSubcontractReturn: build.mutation<SubcontractReturn, SubcontractReturnRequest>({
      query: (body) => ({ url: '/subcontract-returns', method: 'POST', body }),
      // Dönen adetler demet operasyonuna işlenir: ilerleme, ara stok ve demet durumları değişir.
      invalidatesTags: ['Subcontract', 'Bundle', 'ProductionTracking', 'ProductionOrder', 'Quality'],
    }),
    updateSubcontractReturnPrices: build.mutation<
      SubcontractReturn,
      { id: number; rowVersion: string; lines: { returnLineId: number; unitPrice: number | null }[] }
    >({
      query: ({ id, ...body }) => ({ url: `/subcontract-returns/${id}/prices`, method: 'PUT', body }),
      invalidatesTags: ['Subcontract'],
    }),
    setSubcontractReturnPayment: build.mutation<
      SubcontractReturn,
      { id: number; rowVersion: string; paid: boolean; paidDate: string | null; note: string | null }
    >({
      query: ({ id, ...body }) => ({ url: `/subcontract-returns/${id}/payment`, method: 'POST', body }),
      invalidatesTags: ['Subcontract'],
    }),
  }),
})

export const {
  useGetSubcontractPricesQuery,
  useSaveSubcontractPriceMutation,
  useDeleteSubcontractPriceMutation,
  useResolveSubcontractPriceQuery,
  useGetSubcontractOperationOptionsQuery,
  useGetSubcontractCandidatesQuery,
  useGetSubcontractSendsQuery,
  useGetSubcontractSendQuery,
  useGetSubcontractSendDocumentQuery,
  useCreateSubcontractSendMutation,
  useCancelSubcontractSendMutation,
  useGetSubcontractReturnsQuery,
  useGetSubcontractStatementQuery,
  useGetSubcontractReturnQuery,
  useCreateSubcontractReturnMutation,
  useUpdateSubcontractReturnPricesMutation,
  useSetSubcontractReturnPaymentMutation,
} = subcontractApi
