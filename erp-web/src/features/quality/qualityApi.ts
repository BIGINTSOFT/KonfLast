import { baseApi } from '../../api/baseApi'
import type { GridParams } from '../../components/grid/useGrid'
import type { PagedResult } from '../../types/api'
import type { BundleOperationStatus, BundleStatus } from '../bundles/bundlesApi'

export type QualityResult = 'PASSED' | 'REJECTED' | 'REWORK' | 'HOLD'

export type ReworkStatus = 'OPEN' | 'DONE' | 'APPROVED' | 'REJECTED'

export interface Inspection {
  id: number
  bundleId: number
  bundleNo: string
  orderNo: string
  bundleOperationId: number
  sequenceNo: number
  operationName: string
  result: QualityResult
  inspectedQuantity: number
  passedQuantity: number
  rejectedQuantity: number
  reworkQuantity: number
  placedOnHold: boolean
  reworkTaskId: number | null
  note: string | null
  inspectedAt: string
  inspectedBy: string
  defects: { defectTypeId: number; defectTypeName: string; quantity: number }[]
}

export interface ReworkTask {
  id: number
  qualityInspectionId: number
  bundleId: number
  bundleNo: string
  orderNo: string
  bundleOperationId: number
  sequenceNo: number
  operationName: string
  quantity: number
  status: ReworkStatus
  createdAt: string
  createdBy: string
  doneAt: string | null
  closedAt: string | null
  closedBy: string | null
}

export interface InspectableOperation {
  bundleOperationId: number
  sequenceNo: number
  operationName: string
  status: BundleOperationStatus
  /** Kontrol edilebilecek (operasyonun ürettiği) adet. */
  inspectableQuantity: number
  /** Sunucu hesaplar: ret / tamir için geri alınabilecek en fazla adet. */
  takeBackLimit: number
  pendingReworkQuantity: number
}

export interface BundleQuality {
  bundleId: number
  bundleNo: string
  status: BundleStatus
  onHold: boolean
  canHold: boolean
  operations: InspectableOperation[]
  inspections: Inspection[]
  reworkTasks: ReworkTask[]
}

export interface InspectionRequest {
  bundleOperationId: number
  inspectedQuantity: number
  rejectedQuantity: number
  reworkQuantity: number
  hold: boolean
  defects: { defectTypeId: number; quantity: number }[]
  note: string | null
  reworkTaskId: number | null
}

export interface DefectSummary {
  from: string
  to: string
  inspectionCount: number
  inspectedQuantity: number
  rejectedQuantity: number
  reworkQuantity: number
  defectRatePercent: number | null
  defects: { defectTypeId: number; code: string; name: string; quantity: number; inspectionCount: number }[]
}

// Kalite işlemleri demetin adetlerini ve durumunu değiştirir: demet ve takip verileri de tazelenir.
export const qualityApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getBundleQuality: build.query<BundleQuality, number>({
      query: (bundleId) => `/bundles/${bundleId}/quality`,
      providesTags: ['Quality', 'Bundle'],
    }),
    getInspections: build.query<
      PagedResult<Inspection>,
      { page: number; pageSize: number; search?: string; result?: QualityResult } & GridParams
    >({
      query: (params) => ({ url: '/quality/inspections', params }),
      providesTags: ['Quality'],
    }),
    getReworkTasks: build.query<
      PagedResult<ReworkTask>,
      { page: number; pageSize: number; pending?: boolean } & GridParams
    >({
      query: (params) => ({ url: '/quality/rework-tasks', params }),
      providesTags: ['Quality', 'Bundle'],
    }),
    getDefectSummary: build.query<DefectSummary, { from: string; to: string }>({
      query: (params) => ({ url: '/quality/defect-summary', params }),
      providesTags: ['Quality'],
    }),
    createInspection: build.mutation<Inspection, InspectionRequest>({
      query: (body) => ({ url: '/quality/inspections', method: 'POST', body }),
      invalidatesTags: ['Quality', 'Bundle', 'ProductionTracking'],
    }),
    releaseQualityHold: build.mutation<BundleQuality, number>({
      query: (bundleId) => ({ url: `/bundles/${bundleId}/quality/release-hold`, method: 'POST', body: {} }),
      invalidatesTags: ['Quality', 'Bundle', 'ProductionTracking'],
    }),
  }),
})

export const {
  useGetBundleQualityQuery,
  useGetInspectionsQuery,
  useGetReworkTasksQuery,
  useGetDefectSummaryQuery,
  useCreateInspectionMutation,
  useReleaseQualityHoldMutation,
} = qualityApi
