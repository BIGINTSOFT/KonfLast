import { baseApi } from '../../api/baseApi'
import type { GridParams } from '../../components/grid/useGrid'
import type { PagedResult } from '../../types/api'

export type ProductionOrderStatus = 'DRAFT' | 'RELEASED' | 'IN_PRODUCTION' | 'COMPLETED' | 'CANCELLED'

export interface ProductionOrderListItem {
  id: number
  orderNo: string
  customerOrderNo: string | null
  customerId: number
  customerName: string
  productId: number
  productCode: string
  productName: string
  modelCode: string
  orderDate: string
  dueDate: string
  status: ProductionOrderStatus
  totalQuantity: number
}

export interface ProductionOrderItem {
  id: number
  colorId: number
  colorCode: string
  colorName: string
  sizeId: number
  sizeCode: string
  sizeName: string
  sizeSortOrder: number
  quantity: number
}

export interface ProductionOrder extends Omit<ProductionOrderListItem, 'totalQuantity'> {
  customerCode: string
  notes: string | null
  totalQuantity: number
  isEditable: boolean
  /** Sunucunun izin verdiği durum geçişleri; ekrandaki işlemler buna göre gösterilir. */
  allowedTransitions: ProductionOrderStatus[]
  rowVersion: string
  createdAt: string
  createdBy: string | null
  items: ProductionOrderItem[]
  /** Reçete malzemesinin ayrılacağı ve düşüleceği depo; boşsa tek hammadde deposu kullanılır. */
  materialWarehouseId: number | null
  /** Sipariş bir müşteri siparişinden açıldıysa o sipariş. */
  salesOrderId: number | null
  salesOrderNo: string | null
  operationSequence: OperationSequenceMode
  /** İlk demet açılana kadar operasyon sırası değiştirilebilir. */
  canChangeOperationSequence: boolean
}

/** Demet operasyonlarının sırası. ROUTE: bu özellikten önce açılmış demetler (rotadaki öncül tanımları). */
export type OperationSequenceMode = 'SEQUENTIAL' | 'FREE' | 'ROUTE'

export const operationSequenceLabels: Record<OperationSequenceMode, string> = {
  SEQUENTIAL: 'Sıralı',
  FREE: 'Sırasız',
  ROUTE: 'Rotaya göre',
}

export const operationSequenceHints: Record<OperationSequenceMode, string> = {
  SEQUENTIAL: 'Operasyonlar rotadaki sıra numarasıyla yapılır: 1 → 2 → 3 → 4.',
  FREE: 'Operasyonlar istenen sırayla yapılır.',
  ROUTE: 'Rotadaki öncül tanımlarına göre; paralel adımlar kendi aralarında serbest (eski demetler).',
}

export type ProductionOrderListParams = {
  page: number
  pageSize: number
  search?: string
  status?: ProductionOrderStatus
} & GridParams

export interface ProductionOrderRequest {
  customerId: number
  productId: number
  customerOrderNo: string | null
  orderDate: string
  dueDate: string
  notes: string | null
  items: { colorId: number; sizeId: number; quantity: number }[]
  rowVersion?: string
  materialWarehouseId?: number | null
  operationSequence?: OperationSequenceMode
}

/** Hedef durum → API işlemi. */
const transitionEndpoint: Partial<Record<ProductionOrderStatus, string>> = {
  RELEASED: 'release',
  DRAFT: 'revert-to-draft',
  COMPLETED: 'complete',
  CANCELLED: 'cancel',
}

export const productionOrdersApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getProductionOrders: build.query<PagedResult<ProductionOrderListItem>, ProductionOrderListParams>({
      query: (params) => ({ url: '/production-orders', params }),
      providesTags: ['ProductionOrder'],
    }),
    // 'Bundle' etiketi canlı üretim olaylarıyla tazelenir: taslak (düzenlenebilir) siparişte demet olmadığından
    // açık düzenleme formu bundan etkilenmez.
    getProductionOrder: build.query<ProductionOrder, number>({
      query: (id) => `/production-orders/${id}`,
      providesTags: ['ProductionOrder', 'Bundle'],
    }),
    createProductionOrder: build.mutation<ProductionOrder, ProductionOrderRequest>({
      query: (body) => ({ url: '/production-orders', method: 'POST', body }),
      invalidatesTags: ['ProductionOrder'],
    }),
    updateProductionOrder: build.mutation<ProductionOrder, { id: number; body: ProductionOrderRequest }>({
      query: ({ id, body }) => ({ url: `/production-orders/${id}`, method: 'PUT', body }),
      // Hata durumunda da (ör. sürüm çakışması) güncel sipariş yeniden yüklenir.
      invalidatesTags: ['ProductionOrder'],
    }),
    transitionProductionOrder: build.mutation<
      ProductionOrder,
      { id: number; target: ProductionOrderStatus; rowVersion: string }
    >({
      query: ({ id, target, rowVersion }) => ({
        url: `/production-orders/${id}/${transitionEndpoint[target]}`,
        method: 'POST',
        body: { rowVersion },
      }),
      // Sipariş iptali demetleri de iptal eder; onay / geri alma / tamamlama malzeme rezervasyonunu değiştirir.
      invalidatesTags: ['ProductionOrder', 'Bundle', 'Stock'],
    }),
    /** Operasyon sırasını (sıralı / sırasız) değiştirir; ilk demet açılana kadar. */
    changeOperationSequence: build.mutation<
      ProductionOrder,
      { id: number; operationSequence: OperationSequenceMode; rowVersion: string }
    >({
      query: ({ id, ...body }) => ({ url: `/production-orders/${id}/operation-sequence`, method: 'PUT', body }),
      invalidatesTags: ['ProductionOrder'],
    }),
    deleteProductionOrder: build.mutation<null, number>({
      query: (id) => ({ url: `/production-orders/${id}`, method: 'DELETE' }),
      invalidatesTags: ['ProductionOrder'],
    }),
  }),
})

export const {
  useChangeOperationSequenceMutation,
  useGetProductionOrdersQuery,
  useGetProductionOrderQuery,
  useCreateProductionOrderMutation,
  useUpdateProductionOrderMutation,
  useTransitionProductionOrderMutation,
  useDeleteProductionOrderMutation,
} = productionOrdersApi
