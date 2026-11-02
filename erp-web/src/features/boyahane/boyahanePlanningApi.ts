import { baseApi } from '../../api/baseApi'
import type { OrderProcessType } from './boyahaneOrdersApi'
import type { ProcessMinutesSource } from './boyahaneQuotationsApi'

export type DyeMachineStatus = 'AVAILABLE' | 'BREAKDOWN' | 'MAINTENANCE'

/** Makine kuyruğundaki parti ve tahmini zamanları; saatler UTC, ofsetler bugünün başından gün. */
export interface BoyahaneCapacityItem {
  batchId: number
  batchNo: string
  /** Makineye alınmış (işlemde); kuyruğun başındadır, taşınamaz. */
  started: boolean
  /** Şu anda bu makinede süren adımı var. */
  running: boolean
  sequence: number | null
  processType: OrderProcessType
  itemCode: string
  colorName: string | null
  customerNames: string | null
  orderNos: string
  totalKilograms: number
  overCapacity: boolean
  capacityOverrideBy: string | null
  currentStep: string | null
  durationMinutes: number
  remainingMinutes: number
  durationSource: ProcessMinutesSource
  /** İşlemdeki parti tahmini süreyi aştı. */
  overrun: boolean
  estimatedStart: string
  estimatedFinish: string
  estimatedFinishDate: string
  startOffsetDays: number
  endOffsetDays: number
  plannedDate: string | null
  dueDate: string | null
  lateRisk: boolean
  lateDays: number | null
}

export interface BoyahaneCapacityMachine {
  machineId: number
  machineCode: string
  machineName: string
  machineType: string
  capacityKg: number
  status: DyeMachineStatus
  isActive: boolean
  queueMinutes: number
  queueEndsAt: string | null
  queueDays: number
  load7Percent: number
  load14Percent: number
  load30Percent: number
  lateRiskCount: number
  overCapacityCount: number
  items: BoyahaneCapacityItem[]
}

export interface BoyahaneCapacityPlan {
  generatedAt: string
  today: string
  days: number
  hoursPerDay: number
  hoursSource: string
  machines: BoyahaneCapacityMachine[]
}

export interface BoyahaneCapacityMachineOption {
  machineId: number
  code: string
  name: string
  machineType: string
  capacityKg: number
  status: DyeMachineStatus
  current: boolean
  capacityOk: boolean
  queueCount: number
  queueEndsAt: string | null
}

export interface BoyahaneCapacityOptions {
  batchId: number
  batchNo: string
  totalKilograms: number
  /** Makinenin yapabilmesi gereken operasyon. */
  mainOperation: string | null
  machines: BoyahaneCapacityMachineOption[]
}

export interface BoyahanePlanBatchBody {
  batchId: number
  machineId: number
  /** Plan tarihi (en erken başlangıç); boşsa kaldırılır. */
  plannedDate: string | null
  /** Kuyruktaki yeri; boşsa sona eklenir. */
  position: number | null
  capacityOverrideNote: string | null
}

export const boyahanePlanningApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getBoyahaneCapacityPlan: build.query<BoyahaneCapacityPlan, { days: number; hoursPerDay?: number }>({
      query: (params) => ({ url: '/boyahane-capacity-plan', params }),
      providesTags: ['BoyahaneBatch', 'MasterData'],
    }),
    getBoyahaneCapacityOptions: build.query<BoyahaneCapacityOptions, number>({
      query: (batchId) => `/boyahane-capacity-plan/batches/${batchId}/options`,
      providesTags: ['BoyahaneBatch', 'MasterData'],
    }),
    planBoyahaneBatch: build.mutation<null, BoyahanePlanBatchBody>({
      query: ({ batchId, ...body }) => ({ url: `/boyahane-capacity-plan/batches/${batchId}`, method: 'PUT', body }),
      invalidatesTags: ['BoyahaneBatch'],
    }),
    reorderBoyahaneMachineQueue: build.mutation<null, { machineId: number; batchIds: number[] }>({
      query: ({ machineId, batchIds }) => ({ url: `/boyahane-capacity-plan/machines/${machineId}/queue`, method: 'PUT', body: { batchIds } }),
      invalidatesTags: ['BoyahaneBatch'],
    }),
  }),
})

export const {
  useGetBoyahaneCapacityPlanQuery,
  useGetBoyahaneCapacityOptionsQuery,
  usePlanBoyahaneBatchMutation,
  useReorderBoyahaneMachineQueueMutation,
} = boyahanePlanningApi
