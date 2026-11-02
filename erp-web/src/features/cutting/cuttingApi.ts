import { baseApi } from '../../api/baseApi'
import type { GridParams } from '../../components/grid/useGrid'
import type { PagedResult } from '../../types/api'
import type { BundleStatus } from '../bundles/bundlesApi'
import type { ProductionOrderStatus } from '../productionOrders/productionOrdersApi'
import type { StockUnit } from '../stock/stockApi'

export type CuttingPlanStatus = 'DRAFT' | 'CUT' | 'CANCELLED'

export interface CuttingPlanListItem {
  id: number
  planNo: string
  productionOrderId: number
  orderNo: string
  productCode: string
  modelCode: string
  colorName: string
  markerLength: number
  plannedPlies: number
  actualPlies: number | null
  /** Bir kattan çıkan parça (oranların toplamı). */
  piecesPerPly: number
  plannedQuantity: number
  cutQuantity: number | null
  /** (Pastal boyu + kat başı pay) × planlanan kat, metre. */
  plannedFabric: number
  status: CuttingPlanStatus
  createdAt: string
  cutAt: string | null
}

export interface CuttingPlanSize {
  sizeId: number
  sizeName: string
  ratio: number
  plannedQuantity: number
  ordered: number
  /** Bu plan dışında kesilecek kalan (eksi: fazla kesim). */
  remaining: number
  /** (Kesilen ya da planlanan) − kalan: artı fazla, eksi eksik. */
  difference: number
  cutQuantity: number | null
}

export interface CuttingPlanLot {
  id: number
  lotId: number
  lotNo: string
  stockItemId: number
  stockItemCode: string
  stockItemName: string
  unit: StockUnit
  batchNo: string | null
  shade: string | null
  width: number | null
  supplierRollNo: string | null
  /** Topun şu anki toplam bakiyesi. */
  balance: number
  locations: string
  sortOrder: number
  usedQuantity: number | null
}

export interface CuttingPlan {
  id: number
  planNo: string
  productionOrderId: number
  orderNo: string
  orderStatus: ProductionOrderStatus
  customerName: string
  productCode: string
  productName: string
  modelCode: string
  colorId: number
  colorName: string
  markerLength: number
  endAllowance: number
  plannedPlies: number
  actualPlies: number | null
  piecesPerPly: number
  plannedQuantity: number
  cutQuantity: number | null
  plannedFabric: number
  actualFabric: number | null
  suggestedPlies: number
  shadeKey: string | null
  status: CuttingPlanStatus
  notes: string | null
  canEdit: boolean
  canCut: boolean
  cutBlockedReason: string | null
  warnings: string[]
  createdAt: string
  createdBy: string | null
  cutAt: string | null
  cutBy: string | null
  rowVersion: string
  sizes: CuttingPlanSize[]
  lots: CuttingPlanLot[]
  bundles: { id: number; bundleNo: string; sizeName: string; quantity: number; status: BundleStatus }[]
  /** Kumaş dışında reçete malzemesi var: kesimde demetlerle birlikte düşülür. */
  hasRecipe: boolean
}

export interface CuttingSummaryItem {
  colorId: number
  colorName: string
  sizeId: number
  sizeName: string
  ordered: number
  /** Kesilmiş planlarda çıkan adet. */
  cut: number
  /** Plan dışında açılmış demetlerdeki adet. */
  bundledOutside: number
  /** Sipariş − kesilen − plansız demetlenen; eksi değer fazla kesimdir. */
  remaining: number
  plannedInDrafts: number
}

export interface OrderCuttingSummary {
  productionOrderId: number
  orderNo: string
  status: ProductionOrderStatus
  canPlan: boolean
  planBlockedReason: string | null
  canCut: boolean
  cutBlockedReason: string | null
  items: CuttingSummaryItem[]
  plans: CuttingPlanListItem[]
}

export interface CuttingLotOption {
  lotId: number
  lotNo: string
  stockItemId: number
  stockItemCode: string
  stockItemName: string
  unit: StockUnit
  batchNo: string | null
  shade: string | null
  width: number | null
  supplierRollNo: string | null
  balance: number
  locations: string
  /** Kumaş siparişin ürün reçetesinde var. */
  inRecipe: boolean
}

export interface CuttingPlanRequest {
  productionOrderId: number
  colorId: number
  markerLength: number
  endAllowance: number | null
  plannedPlies: number
  sizes: { sizeId: number; ratio: number }[]
  lotIds: number[]
  notes: string | null
  rowVersion?: string
}

export interface CuttingPlanCutRequest {
  rowVersion: string
  actualPlies: number
  bundleSize: number
  sizes: { sizeId: number; quantity: number }[]
  /** Boşsa (pastal boyu + pay) × gerçek kat toplara serim sırasıyla dağıtılır (metreyle izlenen kumaşta). */
  lotUsages: { lotId: number; quantity: number }[] | null
  warehouseId: number | null
}

export type CuttingPlanListParams = {
  page: number
  pageSize: number
  search?: string
  productionOrderId?: number
} & GridParams

export const cuttingApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getCuttingPlans: build.query<PagedResult<CuttingPlanListItem>, CuttingPlanListParams>({
      query: (params) => ({ url: '/cutting-plans', params }),
      providesTags: ['CuttingPlan'],
    }),
    getCuttingPlan: build.query<CuttingPlan, number>({
      query: (id) => `/cutting-plans/${id}`,
      // Kalan adetler plan dışındaki demetlerden de etkilenir.
      providesTags: ['CuttingPlan', 'Bundle'],
    }),
    getOrderCuttingSummary: build.query<OrderCuttingSummary, number>({
      query: (orderId) => `/production-orders/${orderId}/cutting-summary`,
      providesTags: ['CuttingPlan', 'Bundle', 'ProductionOrder'],
    }),
    getCuttingLotOptions: build.query<CuttingLotOption[], { search?: string; stockItemId?: number; productionOrderId?: number }>({
      query: (params) => ({ url: '/cutting-plans/lot-options', params: { ...params, take: 100 } }),
      providesTags: ['Stock'],
      keepUnusedDataFor: 0,
    }),
    createCuttingPlan: build.mutation<CuttingPlan, CuttingPlanRequest>({
      query: (body) => ({ url: '/cutting-plans', method: 'POST', body }),
      invalidatesTags: ['CuttingPlan'],
    }),
    updateCuttingPlan: build.mutation<CuttingPlan, { id: number; body: CuttingPlanRequest }>({
      query: ({ id, body }) => ({ url: `/cutting-plans/${id}`, method: 'PUT', body }),
      invalidatesTags: ['CuttingPlan'],
    }),
    cancelCuttingPlan: build.mutation<CuttingPlan, { id: number; rowVersion: string }>({
      query: ({ id, rowVersion }) => ({ url: `/cutting-plans/${id}/cancel`, method: 'POST', body: { rowVersion } }),
      invalidatesTags: ['CuttingPlan'],
    }),
    /** Kesimi kaydeder: demetler açılır, kumaş ve reçete malzemesi stoktan düşer. */
    cutCuttingPlan: build.mutation<CuttingPlan, { id: number; body: CuttingPlanCutRequest }>({
      query: ({ id, body }) => ({ url: `/cutting-plans/${id}/cut`, method: 'POST', body }),
      invalidatesTags: ['CuttingPlan', 'Bundle', 'Stock', 'ProductionOrder'],
    }),
  }),
})

export const {
  useGetCuttingPlansQuery,
  useGetCuttingPlanQuery,
  useGetOrderCuttingSummaryQuery,
  useGetCuttingLotOptionsQuery,
  useCreateCuttingPlanMutation,
  useUpdateCuttingPlanMutation,
  useCancelCuttingPlanMutation,
  useCutCuttingPlanMutation,
} = cuttingApi
