import { baseApi } from '../../api/baseApi'
import type { GridParams } from '../../components/grid/useGrid'
import type { PagedResult } from '../../types/api'
import type { StockItemType, StockUnit } from '../stock/stockApi'

export type PurchaseOrderStatus = 'DRAFT' | 'APPROVED' | 'PARTIALLY_RECEIVED' | 'RECEIVED' | 'CLOSED' | 'CANCELLED'

export interface PurchaseOrderListItem {
  id: number
  orderNo: string
  supplierId: number
  supplierName: string
  orderDate: string
  expectedDate: string | null
  status: PurchaseOrderStatus
  itemCount: number
  totalAmount: number | null
  currency: string
}

export interface PurchaseOrderItem {
  id: number
  stockItemId: number
  itemCode: string
  itemName: string
  unit: StockUnit
  quantity: number
  unitPrice: number | null
  receivedQuantity: number
  remainingQuantity: number
  lineAmount: number | null
  /** Top / parti takipli kart: mal kabulde toplara bölünebilir. */
  isLotTracked: boolean
  /** Kalem bir iş emrinin eksik malzemesi için açıldıysa o sipariş. */
  productionOrderId: number | null
  productionOrderNo: string | null
}

/** Tutarları, kalan miktarları ve yapılabilecek işlemleri sunucu belirler. */
export interface PurchaseOrder {
  id: number
  orderNo: string
  supplierId: number
  supplierCode: string
  supplierName: string
  orderDate: string
  expectedDate: string | null
  currency: string
  status: PurchaseOrderStatus
  notes: string | null
  totalAmount: number | null
  isEditable: boolean
  canReceive: boolean
  /** Kullanıcının yapabileceği durum geçişleri. */
  allowedTransitions: PurchaseOrderStatus[]
  rowVersion: string
  createdAt: string
  createdBy: string | null
  items: PurchaseOrderItem[]
  /** Sipariş para biriminin maliyet para birimine kuru; yerel para birimindeki siparişte boş. */
  exchangeRate: number | null
  /** Stok maliyetinin tutulduğu para birimi. */
  costCurrency: string
}

export interface PurchaseOrderRequest {
  supplierId: number
  orderDate: string
  expectedDate: string | null
  notes: string | null
  currency: string | null
  exchangeRate: number | null
  items: { stockItemId: number; quantity: number; unitPrice: number | null; productionOrderId: number | null }[]
  rowVersion?: string
}

export interface GoodsReceipt {
  id: number
  purchaseOrderId: number
  warehouseCode: string
  warehouseName: string
  documentNo: string | null
  note: string | null
  receivedAt: string
  receivedBy: string
  lines: { purchaseOrderItemId: number; itemCode: string; itemName: string; unit: StockUnit; quantity: number }[]
}

/** Mal kabulde gelen bir top / parti. */
export interface GoodsReceiptLot {
  quantity: number
  batchNo: string | null
  shade: string | null
  width: number | null
  supplierRollNo: string | null
}

export interface GoodsReceiptLine {
  purchaseOrderItemId: number
  quantity: number
  /** Takipli kartta gelen toplar; toplamı kalemin miktarına eşittir. Verilmezse kalem tek toptur. */
  lots?: GoodsReceiptLot[]
}

export interface GoodsReceiptRequest {
  /** İstemcide bir kez üretilir; istek yinelenirse stok ikinci kez artmaz. */
  requestId: string
  warehouseId: number
  lines: GoodsReceiptLine[]
  documentNo: string | null
  note: string | null
}

export type PurchaseOrderAction = 'approve' | 'revert-to-draft' | 'close' | 'cancel'

export interface PurchaseOrderListParams {
  page: number
  pageSize: number
  search?: string
  status?: PurchaseOrderStatus
  receivable?: boolean
}

/** Tedarik önerisi satırı: eksik = ihtiyaç + asgari stok − stok − yoldaki − taslaktaki. */
export interface SupplySuggestion {
  stockItemId: number
  itemCode: string
  itemName: string
  itemType: StockItemType
  unit: StockUnit
  /** Tüm depolardaki toplam stok. */
  onHand: number
  /** Onaylı ve üretimdeki siparişlerin henüz malzemesi çıkılmamış adetleri için ihtiyaç. */
  demand: number
  minimumStock: number
  /** Onaylı satın alma siparişlerinde teslim alınmamış miktar. */
  onOrder: number
  /** Taslak satın alma siparişlerindeki miktar. */
  inDraft: number
  shortage: number
  suggestedQuantity: number
  supplierId: number | null
  supplierName: string | null
  demands: { productionOrderId: number; orderNo: string; dueDate: string; quantity: number }[]
}

export interface SupplySuggestionParams {
  search?: string
  itemType?: StockItemType
  includeCovered?: boolean
}

export interface CreateSupplyOrdersRequest {
  lines: { stockItemId: number; quantity: number; supplierId: number | null }[]
  expectedDate: string | null
}

// Mal kabul stoğu da değiştirir: stok ekranları da tazelenir.
export const purchasingApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    // Öneri stoktan, açık siparişlerden, reçeteden ve satın alma siparişlerinden hesaplanır.
    getSupplySuggestions: build.query<SupplySuggestion[], SupplySuggestionParams>({
      query: (params) => ({ url: '/supply-suggestions', params }),
      providesTags: ['PurchaseOrder', 'Stock', 'ProductionOrder', 'Bundle', 'Material', 'MasterData'],
    }),
    createSupplyOrders: build.mutation<PurchaseOrder[], CreateSupplyOrdersRequest>({
      query: (body) => ({ url: '/supply-suggestions/orders', method: 'POST', body }),
      invalidatesTags: ['PurchaseOrder', 'Workspace'],
    }),
    getPurchaseOrders: build.query<PagedResult<PurchaseOrderListItem>, PurchaseOrderListParams & GridParams>({
      query: (params) => ({ url: '/purchase-orders', params }),
      providesTags: ['PurchaseOrder'],
    }),
    getPurchaseOrder: build.query<PurchaseOrder, number>({
      query: (id) => `/purchase-orders/${id}`,
      providesTags: ['PurchaseOrder'],
    }),
    getGoodsReceipts: build.query<GoodsReceipt[], number>({
      query: (id) => `/purchase-orders/${id}/receipts`,
      providesTags: ['PurchaseOrder'],
    }),
    savePurchaseOrder: build.mutation<PurchaseOrder, { id?: number; body: PurchaseOrderRequest }>({
      query: ({ id, body }) => ({
        url: id === undefined ? '/purchase-orders' : `/purchase-orders/${id}`,
        method: id === undefined ? 'POST' : 'PUT',
        body,
      }),
      invalidatesTags: ['PurchaseOrder'],
    }),
    transitionPurchaseOrder: build.mutation<PurchaseOrder, { id: number; action: PurchaseOrderAction; rowVersion: string }>({
      query: ({ id, action, rowVersion }) => ({ url: `/purchase-orders/${id}/${action}`, method: 'POST', body: { rowVersion } }),
      invalidatesTags: ['PurchaseOrder'],
    }),
    deletePurchaseOrder: build.mutation<null, number>({
      query: (id) => ({ url: `/purchase-orders/${id}`, method: 'DELETE' }),
      invalidatesTags: ['PurchaseOrder'],
    }),
    receiveGoods: build.mutation<{ duplicate: boolean; receipt: GoodsReceipt; order: PurchaseOrder }, { id: number; body: GoodsReceiptRequest }>({
      query: ({ id, body }) => ({ url: `/purchase-orders/${id}/receipts`, method: 'POST', body }),
      invalidatesTags: ['PurchaseOrder', 'Stock'],
    }),
  }),
})

export const {
  useGetSupplySuggestionsQuery,
  useCreateSupplyOrdersMutation,
  useGetPurchaseOrdersQuery,
  useGetPurchaseOrderQuery,
  useGetGoodsReceiptsQuery,
  useSavePurchaseOrderMutation,
  useTransitionPurchaseOrderMutation,
  useDeletePurchaseOrderMutation,
  useReceiveGoodsMutation,
} = purchasingApi
