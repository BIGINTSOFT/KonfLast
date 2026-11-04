import { baseApi } from '../../api/baseApi'
import type { GridParams } from '../../components/grid/useGrid'
import type { PagedResult } from '../../types/api'
import type { OperationSequenceMode } from '../productionOrders/productionOrdersApi'

export type BundleStatus =
  | 'CREATED'
  | 'WAITING'
  | 'IN_PRODUCTION'
  | 'PARTIALLY_COMPLETED'
  | 'COMPLETED'
  | 'QUALITY_HOLD'
  | 'REWORK'
  | 'CANCELLED'
  | 'CLOSED'

export type BundleOperationStatus =
  | 'WAITING'
  | 'READY'
  | 'IN_PROGRESS'
  | 'PAUSED'
  | 'COMPLETED'
  | 'QUALITY_HOLD'
  | 'CANCELLED'

export interface BundleListItem {
  id: number
  bundleNo: string
  productionOrderId: number
  orderNo: string
  productCode: string
  modelCode: string
  colorName: string
  sizeName: string
  quantity: number
  status: BundleStatus
  rfidUid: string | null
  completedOperations: number
  totalOperations: number
  createdAt: string
}

export interface BundleOperation {
  id: number
  sequenceNo: number
  operationId: number
  operationCode: string
  operationName: string
  sam: number
  status: BundleOperationStatus
  plannedQuantity: number
  producedQuantity: number
  rejectedQuantity: number
  remainingQuantity: number
  machineCode: string | null
  operatorName: string | null
  startedAt: string | null
  completedAt: string | null
  predecessors: number[]
  /** Akıştaki aşama (0'dan başlar); sunucu hesaplar. */
  stage: number
  /** Kalitenin tamire gönderdiği, henüz yeniden işlenmemiş adet (kalan adedin içindedir). */
  reworkQuantity: number
}

export interface Bundle {
  id: number
  bundleNo: string
  productionOrderId: number
  orderNo: string
  customerName: string
  productCode: string
  productName: string
  modelCode: string
  routeVersion: number
  colorName: string
  sizeName: string
  quantity: number
  status: BundleStatus
  /** Demete atanmış RFID kartı. */
  rfidUid: string | null
  /** Sunucu hesaplar: demete şu an kart atanabilir mi. */
  canAssignCard: boolean
  canCancel: boolean
  /** Demetin şu an bulunduğu operasyon(lar). */
  currentOperations: string[]
  rowVersion: string
  createdAt: string
  createdBy: string | null
  operations: BundleOperation[]
  /** Operasyonların sırası; demet açılırken siparişten kopyalanır. */
  sequenceMode: OperationSequenceMode
  /** Demet fasoncuda: dönüş işlenene kadar sahada işlenemez. */
  atSubcontractor?: boolean
}

export type BundleEventType =
  | 'OPERATION_STARTED'
  | 'OPERATION_RESUMED'
  | 'OPERATION_PAUSED'
  | 'OPERATION_COMPLETED'
  | 'QUANTITY_REPORTED'
  | 'REJECTION_REPORTED'

/** Üretim olayı; kayıtlar değiştirilemez. */
export interface BundleEvent {
  id: number
  eventType: BundleEventType
  sequenceNo: number
  operationName: string
  machineCode: string
  operatorName: string
  producedQuantity: number
  rejectedQuantity: number
  occurredAt: string
  /** Ofisten elle girildiyse kaydı giren kullanıcı; tabletten gelen olaylarda boş. */
  enteredBy: string | null
  /** Adet operatörün performansına ve primine sayılır mı (elle bildirimde seçilir). */
  countsForPerformance: boolean
}

/** Elle bildirim penceresi: operasyonun durumu ve operasyonu yapabilen makineler. */
export interface ManualReportOptions {
  bundleOperationId: number
  sequenceNo: number
  operationName: string
  status: BundleOperationStatus
  remainingQuantity: number
  /** Operasyon bir makinede işlemde (askıda); makine ve operatör o işinkidir, değiştirilemez. */
  inProgress: boolean
  machineId: number | null
  machineCode: string | null
  operatorId: number | null
  operatorName: string | null
  machines: { id: number; machineCode: string; machineName: string; status: string }[]
}

export interface ManualReportRequest {
  operationId: number
  requestId: string
  producedQuantity: number
  rejectedQuantity: number
  operatorId: number | null
  machineId: number | null
  attribution: ManualReportAttribution
}

/** Elle bildirilen adedin kime yazılacağı. */
export type ManualReportAttribution = 'OPERATOR' | 'OPERATOR_NOT_COUNTED' | 'NO_OPERATOR'

export interface BundleSummaryItem {
  colorId: number
  colorName: string
  sizeId: number
  sizeName: string
  ordered: number
  allowed: number
  bundled: number
  remaining: number
  bundleCount: number
}

export interface OrderBundleSummary {
  productionOrderId: number
  orderNo: string
  canCreateBundles: boolean
  blockedReason: string | null
  ordered: number
  bundled: number
  remaining: number
  items: BundleSummaryItem[]
  /** İptal edilmemiş demetlerin akıştaki dağılımı. */
  progress: { total: number; withoutCard: number; inProduction: number; completed: number; closed: number } | null
  /** Ürünün reçetesi var: demet açılırken malzeme depodan düşülür. */
  hasRecipe: boolean
}

export type BundleListParams = {
  page: number
  pageSize: number
  search?: string
  status?: BundleStatus
  productionOrderId?: number
} & GridParams

export const bundlesApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getBundles: build.query<PagedResult<BundleListItem>, BundleListParams>({
      query: (params) => ({ url: '/bundles', params }),
      providesTags: ['Bundle'],
    }),
    getBundle: build.query<Bundle, number>({
      query: (id) => `/bundles/${id}`,
      providesTags: ['Bundle'],
    }),
    getBundleEvents: build.query<BundleEvent[], number>({
      query: (id) => `/bundles/${id}/events`,
      providesTags: ['Bundle'],
    }),
    getOrderBundleSummary: build.query<OrderBundleSummary, number>({
      query: (orderId) => `/production-orders/${orderId}/bundle-summary`,
      // Sipariş onaylanınca / iptal edilince demetlenebilirlik de değişir.
      providesTags: ['Bundle', 'ProductionOrder'],
    }),
    generateBundles: build.mutation<
      { bundleCount: number; totalQuantity: number; bundles: BundleListItem[] },
      { orderId: number; bundleSize: number; warehouseId?: number }
    >({
      query: ({ orderId, ...body }) => ({
        url: `/production-orders/${orderId}/bundles/generate`,
        method: 'POST',
        body,
      }),
      // Reçeteli üründe demet açmak stoğu da düşer.
      invalidatesTags: ['Bundle', 'Stock'],
    }),
    cancelBundle: build.mutation<Bundle, { id: number; rowVersion: string }>({
      query: ({ id, rowVersion }) => ({ url: `/bundles/${id}/cancel`, method: 'POST', body: { rowVersion } }),
      invalidatesTags: ['Bundle', 'Stock'],
    }),
    /** Ofisten: işlemde kalmış operasyonu adet bildirmeden bırakır; operasyon kalan adediyle bekler. */
    releaseBundleOperation: build.mutation<{ bundleId: number; bundleOperationId: number; remainingQuantity: number }, number>({
      query: (operationId) => ({ url: `/bundle-operations/${operationId}/release`, method: 'POST' }),
      invalidatesTags: ['Bundle', 'WorkSession', 'MachineStatus', 'ProductionTracking', 'LiveDashboard'],
    }),
    getManualReportOptions: build.query<ManualReportOptions, number>({
      query: (operationId) => `/bundle-operations/${operationId}/manual-report`,
      keepUnusedDataFor: 0,
    }),
    /** Ofisten, tablet ve kart olmadan adet bildirir (askıda kalan iş, arızalı tablet). */
    manualReport: build.mutation<unknown, ManualReportRequest>({
      query: ({ operationId, ...body }) => ({ url: `/bundle-operations/${operationId}/manual-report`, method: 'POST', body }),
      invalidatesTags: ['Bundle', 'ProductionOrder', 'WorkSession', 'MachineStatus', 'ProductionTracking', 'LiveDashboard'],
    }),
  }),
})

export const {
  useGetBundlesQuery,
  useGetBundleQuery,
  useGetBundleEventsQuery,
  useGetOrderBundleSummaryQuery,
  useGenerateBundlesMutation,
  useCancelBundleMutation,
  useReleaseBundleOperationMutation,
  useGetManualReportOptionsQuery,
  useManualReportMutation,
} = bundlesApi
