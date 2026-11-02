import { baseApi } from '../../api/baseApi'
import type { PagedResult } from '../../types/api'
import type { BundleOperationStatus, BundleStatus } from '../bundles/bundlesApi'
import type { ProductionOrderStatus } from '../productionOrders/productionOrdersApi'

export interface OrderOperationProgress {
  sequenceNo: number
  operationId: number
  operationCode: string
  operationName: string
  planned: number
  produced: number
  rejected: number
  remaining: number
  waitingBundles: number
  inProgressBundles: number
  completedBundles: number
  totalBundles: number
}

export interface OrderItemProgress {
  colorId: number
  colorName: string
  sizeId: number
  sizeName: string
  ordered: number
  bundled: number
  finished: number
  rejected: number
}

/** Tüm değerleri sunucu hesaplar. */
export interface OrderProgress {
  productionOrderId: number
  orderNo: string
  status: ProductionOrderStatus
  ordered: number
  bundled: number
  finished: number
  rejected: number
  inProcess: number
  shortage: number
  completionPercent: number
  openBundleCount: number
  canComplete: boolean
  /** Bu siparişe bağlı, sevk edilmiş sevkiyatlardaki toplam adet. */
  shipped: number
  bundleStatuses: { status: BundleStatus; bundleCount: number; quantity: number }[]
  operations: OrderOperationProgress[]
  items: OrderItemProgress[]
}

export interface WipOperation {
  operationId: number
  operationCode: string
  operationName: string
  readyBundles: number
  readyQuantity: number
  inProgressBundles: number
  inProgressQuantity: number
  pausedBundles: number
  pausedQuantity: number
}

export interface WipBundle {
  bundleOperationId: number
  bundleId: number
  bundleNo: string
  productionOrderId: number
  orderNo: string
  modelCode: string
  colorName: string
  sizeName: string
  status: BundleOperationStatus
  plannedQuantity: number
  producedQuantity: number
  rejectedQuantity: number
  remainingQuantity: number
  machineCode: string | null
  operatorName: string | null
  startedAt: string | null
}

export type OperatorState =
  | 'OFFLINE'
  | 'WORKING'
  | 'PRODUCING'
  | 'WAITING_FOR_BUNDLE'
  | 'PAUSED'
  | 'BREAK'
  | 'MACHINE_BREAKDOWN'

export interface MachineActivity {
  machineId: number
  machineCode: string
  machineName: string
  status: string
  operationName: string | null
  lineCode: string | null
  operatorName: string | null
  operatorState: OperatorState
  sessionStartedAt: string | null
  bundleId: number | null
  bundleNo: string | null
  jobStartedAt: string | null
  remainingQuantity: number | null
}

export type OutputGroupBy = 'OPERATOR' | 'MACHINE' | 'OPERATION'

export interface OutputReport {
  from: string
  to: string
  groupBy: OutputGroupBy
  totalProduced: number
  totalRejected: number
  rows: { id: number; code: string; name: string; produced: number; rejected: number; bundles: number }[]
}

export type PerformanceGroupBy = 'OPERATOR' | 'MACHINE' | 'LINE' | 'OPERATION'

/** Süreler dakikadır; tüm değerleri sunucu hesaplar. */
export interface PerformanceRow {
  id: number
  code: string
  name: string
  presenceMinutes: number
  breakMinutes: number
  pauseMinutes: number
  breakdownMinutes: number
  waitingMinutes: number
  netWorkingMinutes: number
  activeMinutes: number
  produced: number
  rejected: number
  earnedMinutes: number
  /** %100 verimde aynı sürede üretilmesi gereken adet. */
  targetQuantity: number | null
  efficiencyPercent: number | null
  utilizationPercent: number | null
  rejectRatePercent: number | null
  producedPerHour: number | null
  targetPerHour: number | null
}

export interface PerformanceReport {
  from: string
  to: string
  groupBy: PerformanceGroupBy
  total: PerformanceRow
  rows: PerformanceRow[]
}

// Üretim tabletlerden ilerler; bu ekranlar veriyi düzenli aralıkla yeniden çeker (canlı akış ayrı modülde).
export const trackingApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getOrderProgress: build.query<OrderProgress, number>({
      query: (orderId) => `/production-orders/${orderId}/progress`,
      providesTags: ['ProductionTracking', 'Bundle', 'ProductionOrder'],
    }),
    getWip: build.query<WipOperation[], void>({
      query: () => '/production-tracking/wip',
      providesTags: ['ProductionTracking'],
    }),
    getWipBundles: build.query<PagedResult<WipBundle>, { operationId: number; page: number; pageSize: number }>({
      query: (params) => ({ url: '/production-tracking/wip/bundles', params }),
      providesTags: ['ProductionTracking'],
    }),
    getMachineActivity: build.query<MachineActivity[], void>({
      query: () => '/production-tracking/machines',
      providesTags: ['ProductionTracking', 'MachineStatus'],
    }),
    getOutput: build.query<OutputReport, { from: string; to: string; groupBy: OutputGroupBy }>({
      query: (params) => ({ url: '/production-tracking/output', params }),
      providesTags: ['ProductionTracking'],
    }),
    getPerformance: build.query<PerformanceReport, { from: string; to: string; groupBy: PerformanceGroupBy }>({
      query: (params) => ({ url: '/production-tracking/performance', params }),
      providesTags: ['ProductionTracking', 'WorkSession'],
    }),
  }),
})

export const {
  useGetOrderProgressQuery,
  useGetWipQuery,
  useGetWipBundlesQuery,
  useGetMachineActivityQuery,
  useGetOutputQuery,
  useGetPerformanceQuery,
} = trackingApi
