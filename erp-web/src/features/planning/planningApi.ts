import { baseApi } from '../../api/baseApi'
import type { ProductionOrderStatus } from '../productionOrders/productionOrdersApi'

export type PlanningRisk = 'LATE' | 'PLAN_AFTER_DUE_DATE' | 'UNPLANNED' | 'ON_TRACK'
export type PlanLoadStatus = 'OK' | 'TIGHT' | 'OVERLOADED' | 'NO_CAPACITY' | 'NO_ROUTE'

/** Tüm değerleri ve risk değerlendirmesini sunucu hesaplar. */
export interface PlanningOrder {
  id: number
  orderNo: string
  customerName: string
  modelCode: string
  productName: string
  status: ProductionOrderStatus
  dueDate: string
  daysToDue: number
  ordered: number
  finished: number
  remaining: number
  planned: number
  /** Henüz banta planlanabilecek adet. */
  unplanned: number
  hasRoute: boolean
  remainingWorkHours: number | null
  lastPlannedEnd: string | null
  risk: PlanningRisk
}

export interface PlanLoad {
  planId: number
  productionOrderId: number
  orderNo: string
  modelCode: string
  plannedQuantity: number
  plannedStart: string
  plannedEnd: string
  ratePerHour: number | null
  bottleneckOperationName: string | null
  requiredHours: number | null
  availableHours: number
  loadPercent: number | null
  status: PlanLoadStatus
  dueDate: string
  endsAfterDueDate: boolean
}

export interface LineLoad {
  lineId: number
  lineCode: string
  name: string
  machines: number
  workingHours: number
  bookedHours: number
  freeHours: number
  utilizationPercent: number | null
  plans: PlanLoad[]
}

export interface LineLoadReport {
  from: string
  to: string
  efficiencyPercent: number
  /** Vardiya tanımlı olmadığı için gün boyu çalışma varsayıldı. */
  calendarAssumed: boolean
  lines: LineLoad[]
}

export interface LineSuggestion {
  lineId: number
  lineCode: string
  name: string
  feasible: boolean
  reason: string | null
  ratePerHour: number | null
  bottleneckOperationName: string | null
  requiredHours: number | null
  suggestedStart: string | null
  suggestedEnd: string | null
  meetsDueDate: boolean | null
}

export interface PlanSuggestion {
  productionOrderId: number
  orderNo: string
  modelCode: string
  dueDate: string
  quantity: number
  unplannedQuantity: number
  efficiencyPercent: number
  calendarAssumed: boolean
  lines: LineSuggestion[]
}

export interface PlanningDemandItem {
  colorId: number
  colorName: string
  sizeId: number
  sizeName: string
  sizeSortOrder: number
  ordered: number
  /** Stoktan karşılanan: ayrılan + ayırmadan sevk edilen. */
  fromStock: number
  inProduction: number
  toPlan: number
}

/** Onaylı müşteri siparişinin, üretime açılmayı bekleyen bir ürünü. */
export interface PlanningDemand {
  salesOrderId: number
  salesOrderNo: string
  customerName: string
  customerOrderNo: string | null
  dueDate: string
  daysToDue: number
  productId: number
  productCode: string
  productName: string
  modelCode: string
  hasActiveRoute: boolean
  ordered: number
  fromStock: number
  inProduction: number
  toPlan: number
  approvedAt: string | null
  items: PlanningDemandItem[]
}

export interface CreateProductionFromDemandRequest {
  salesOrderId: number
  productId: number
  dueDate: string | null
  notes: string | null
  items: { colorId: number; sizeId: number; quantity: number }[]
}

// Bant planı kaydedilince ('ProductionLine') ya da canlı plan bildirimi gelince ('Planning') yenilenir.
export const planningApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getPlanningOrders: build.query<PlanningOrder[], void>({
      query: () => '/planning/orders',
      providesTags: ['Planning', 'ProductionLine', 'ProductionOrder'],
    }),
    getLineLoad: build.query<LineLoadReport, { from: string; to: string }>({
      query: (params) => ({ url: '/planning/line-load', params }),
      providesTags: ['Planning', 'ProductionLine'],
    }),
    getPlanSuggestion: build.query<PlanSuggestion, { productionOrderId: number; quantity?: number }>({
      query: (params) => ({ url: '/planning/suggestions', params }),
      providesTags: ['Planning', 'ProductionLine'],
      // Öneri "şu an"a göredir; pencere her açıldığında yeniden hesaplanır.
      keepUnusedDataFor: 0,
    }),
    getPlanningDemand: build.query<PlanningDemand[], void>({
      query: () => '/planning/demand',
      providesTags: ['Planning', 'SalesOrder', 'ProductionOrder', 'Stock'],
    }),
    createProductionFromDemand: build.mutation<{ id: number; orderNo: string }, CreateProductionFromDemandRequest>({
      query: (body) => ({ url: '/planning/demand/production-orders', method: 'POST', body }),
      invalidatesTags: ['Planning', 'SalesOrder', 'ProductionOrder', 'Workspace'],
    }),
  }),
})

export const {
  useGetPlanningOrdersQuery,
  useGetLineLoadQuery,
  useGetPlanSuggestionQuery,
  useGetPlanningDemandQuery,
  useCreateProductionFromDemandMutation,
} = planningApi
