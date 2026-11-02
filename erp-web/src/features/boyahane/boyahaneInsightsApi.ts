import { baseApi } from '../../api/baseApi'
import type { PagedResult } from '../../types/api'
import type { BatchStepStatus, ColorCheckResult } from './boyahaneBatchesApi'
import type { DyeMachineStatus } from './boyahaneFloorApi'
import type { OrderProcessType, OrderUnit } from './boyahaneOrdersApi'

export type OperationBehavior = 'STANDARD' | 'DYEING' | 'COLOR_CHECK' | 'FINAL_QUALITY' | 'STRIPPING' | 'PRINTING'

export const operationBehaviorLabels: Record<string, string> = {
  STANDARD: 'Başlat / bitir',
  DYEING: 'Boyama (reçete ve tüketim)',
  COLOR_CHECK: 'Renk kontrolü (uygun, ilave, yeniden işlem, söküm)',
  FINAL_QUALITY: 'Final kalite (top bazında sonuç)',
  STRIPPING: 'Söküm (söküm reçetesi ve tüketim; renk kontrolünden eklenir)',
  PRINTING: 'Baskı (desen baskı reçetesi ve pasta tüketimi)',
}

export interface BoyahaneRouteStep {
  sequence: number
  operationId: number
  operationCode: string
  operationName: string
  behavior: OperationBehavior
  needsMachine: boolean
}

export interface BoyahaneRoute {
  id: number
  code: string
  name: string
  processType: OrderProcessType
  isDefault: boolean
  isActive: boolean
  steps: BoyahaneRouteStep[]
}

export interface BoyahaneRouteRequest {
  code: string
  name: string
  processType: OrderProcessType
  operationIds: number[]
  isDefault: boolean
  isActive: boolean
}

export interface BoyahaneDashboard {
  planned: number
  inProgress: number
  completedToday: number
  machinesDown: number
  machines: {
    id: number
    code: string
    name: string
    status: DyeMachineStatus
    batchId: number | null
    batchNo: string | null
    operationName: string | null
    operatorName: string | null
    startedAt: string | null
  }[]
  waiting: { id: number; batchNo: string; machineCode: string; plannedDate: string | null; kilograms: number; nextOperation: string | null }[]
}

export interface BoyahaneSetup {
  customers: number
  fabrics: number
  yarns: number
  dyeChemicals: number
  defectTypes: number
  operations: number
  routes: number
  machines: number
  machinesWithoutOperation: number
  tablets: number
  rfidCards: number
  operators: number
  stopReasons: number
}

export interface BoyahaneTrackingRow {
  id: number
  batchId: number
  batchNo: string
  operationName: string
  status: BatchStepStatus
  machineCode: string | null
  startedBy: string | null
  completedBy: string | null
  startedAt: string | null
  completedAt: string | null
  colorResult: ColorCheckResult | null
}

export interface BoyahaneGoodsStatus {
  ownerName: string | null
  cardCode: string
  cardName: string
  isYarn: boolean
  pendingInspection: number
  ready: number
  assigned: number
  inBatch: number
  done: number
  rejected: number
}

export interface BoyahaneOrderTracking {
  id: number
  orderId: number
  orderNo: string
  customerName: string | null
  dueDate: string | null
  processType: OrderProcessType
  cardCode: string
  colorName: string | null
  quantity: number
  unit: OrderUnit
  assignedKilograms: number
  inBatchKilograms: number
  doneKilograms: number
  packedKilograms: number
  shippedKilograms: number
  invoicedQuantity: number
  billableUnit: OrderUnit
  billableQuantity: number
  customerOrderNo: string | null
  shippedMeters: number
  hasUnmeasuredShipment: boolean
  /** Sipariş biriminde kalan (sipariş − onaylı sevk); metre satırında sevk metresi bilinmiyorsa boş. */
  remaining: number | null
}

export interface ShipmentTotals {
  despatchCount: number
  packageCount: number
  kilograms: number
  meters: number
}

export interface BoyahaneShipmentReport {
  from: string
  to: string
  days: { day: string; customers: { customerId: number; customerName: string; totals: ShipmentTotals }[]; total: ShipmentTotals }[]
  total: ShipmentTotals
}

export type IdleGoodsType = 'FABRIC_ROLL' | 'YARN_LOT' | 'CHEMICAL_LOT' | 'PACKAGE'

export interface BoyahaneIdleStock {
  days: number
  goods: {
    goodsType: IdleGoodsType
    goodsNo: string
    cardCode: string
    cardName: string
    ownerName: string | null
    warehouseName: string | null
    locationCode: string | null
    kilograms: number
    meters: number | null
    lastMovementAt: string
    idleDays: number
  }[]
  chemicals: {
    lotNo: string
    code: string
    name: string
    supplierName: string | null
    remainingKilograms: number
    warehouseName: string | null
    locationCode: string | null
    lastMovementAt: string
    idleDays: number
  }[]
  truncated: boolean
}

export interface BoyahaneReport {
  from: string
  to: string
  colorCheckedBatches: number
  colorFirstPassBatches: number
  additions: number
  redos: number
  itemsApproved: number
  itemsRejected: number
  itemsReworked: number
  consumption: { code: string; name: string; plannedKilograms: number; actualKilograms: number; withoutLotKilograms: number }[]
  fire: { ownerName: string | null; cardCode: string; items: number; inputKilograms: number; finalKilograms: number; firePercent: number | null; tolerancePercent: number | null; overTolerance: boolean }[]
  operations: { name: string; count: number; averageMinutes: number; standardMinutes: number | null }[]
  operators: { name: string; steps: number; minutes: number }[]
  downtime: { machineCode: string; status: DyeMachineStatus; count: number; minutes: number }[]
  /** Dönemde biten söküm adımı sayısı. */
  strippings: number
  /** Dönemde biten renk kontrollerinden "söküm + yeniden boya" sonucunun yüzdesi; kontrol yoksa boş. */
  strippingRatePercent: number | null
}

export const boyahaneInsightsApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getBoyahaneRoutes: build.query<BoyahaneRoute[], void>({
      query: () => '/boyahane-routes',
      providesTags: ['BoyahaneBatch'],
    }),
    saveBoyahaneRoute: build.mutation<BoyahaneRoute, { id?: number; body: BoyahaneRouteRequest }>({
      query: ({ id, body }) => ({ url: id === undefined ? '/boyahane-routes' : `/boyahane-routes/${id}`, method: id === undefined ? 'POST' : 'PUT', body }),
      invalidatesTags: ['BoyahaneBatch'],
    }),
    deleteBoyahaneRoute: build.mutation<null, number>({
      query: (id) => ({ url: `/boyahane-routes/${id}`, method: 'DELETE' }),
      invalidatesTags: ['BoyahaneBatch'],
    }),
    getBoyahaneMachineOperations: build.query<number[], number>({
      query: (id) => `/boyahane-machines/${id}/operations`,
      providesTags: ['MasterData'],
    }),
    setBoyahaneMachineOperations: build.mutation<number[], { machineId: number; operationIds: number[] }>({
      query: ({ machineId, operationIds }) => ({ url: `/boyahane-machines/${machineId}/operations`, method: 'PUT', body: { operationIds } }),
      invalidatesTags: ['MasterData'],
    }),
    getBoyahaneDashboard: build.query<BoyahaneDashboard, void>({
      query: () => '/boyahane-insights/dashboard',
      providesTags: ['BoyahaneBatch'],
    }),
    getBoyahaneSetup: build.query<BoyahaneSetup, void>({
      query: () => '/boyahane-insights/setup',
      providesTags: ['MasterData', 'BoyahaneBatch'],
    }),
    getBoyahaneTracking: build.query<PagedResult<BoyahaneTrackingRow>, Record<string, unknown>>({
      query: (params) => ({ url: '/boyahane-insights/tracking', params }),
      providesTags: ['BoyahaneBatch'],
    }),
    getBoyahaneGoods: build.query<BoyahaneGoodsStatus[], void>({
      query: () => '/boyahane-insights/goods',
      providesTags: ['BoyahaneBatch', 'BoyahaneReceipt', 'BoyahaneOrder', 'BoyahaneInspection'],
    }),
    getBoyahaneOrderTracking: build.query<PagedResult<BoyahaneOrderTracking>, Record<string, unknown>>({
      query: (params) => ({ url: '/boyahane-insights/orders', params }),
      providesTags: ['BoyahaneBatch', 'BoyahaneOrder'],
    }),
    getBoyahaneReport: build.query<BoyahaneReport, { from?: string; to?: string }>({
      query: (params) => ({ url: '/boyahane-insights/report', params }),
      providesTags: ['BoyahaneBatch'],
    }),
    getBoyahaneShipmentReport: build.query<BoyahaneShipmentReport, { from?: string; to?: string }>({
      query: (params) => ({ url: '/boyahane-insights/report/shipments', params }),
      providesTags: ['BoyahaneBatch'],
    }),
    getBoyahaneIdleStock: build.query<BoyahaneIdleStock, { days: number }>({
      query: (params) => ({ url: '/boyahane-insights/report/idle-stock', params }),
      providesTags: ['BoyahaneBatch', 'BoyahaneReceipt'],
    }),
  }),
})

export const {
  useGetBoyahaneRoutesQuery,
  useSaveBoyahaneRouteMutation,
  useDeleteBoyahaneRouteMutation,
  useGetBoyahaneMachineOperationsQuery,
  useSetBoyahaneMachineOperationsMutation,
  useGetBoyahaneDashboardQuery,
  useGetBoyahaneSetupQuery,
  useGetBoyahaneTrackingQuery,
  useGetBoyahaneGoodsQuery,
  useGetBoyahaneOrderTrackingQuery,
  useGetBoyahaneReportQuery,
  useGetBoyahaneShipmentReportQuery,
  useGetBoyahaneIdleStockQuery,
} = boyahaneInsightsApi
