import { baseApi } from '../../api/baseApi'
import type { OrmeStopCategory } from './ormeFloorApi'

export interface OrmeCapacityItem {
  knitOrderId: number
  knitOrderNo: string
  orderId: number
  orderNo: string
  customerName: string | null
  fabricCode: string
  fabricName: string
  /** Emir şu anda bu makinede çalışıyor (kuyruğun başı). */
  running: boolean
  /** Kuyruktaki sıra; çalışan emirde boş. */
  sequence: number | null
  runningMachineCount: number
  plannedKilograms: number
  knittedKilograms: number
  remainingKilograms: number
  /** Bu makinede örülmesi beklenen: kalan ÷ çalıştığı makine sayısı. */
  machineKilograms: number
  /** Kumaş kartındaki beklenen üretim; boşsa süre hesaplanamaz. */
  kilogramsPerHour: number | null
  durationHours: number | null
  estimatedStart: string
  estimatedFinish: string | null
  estimatedFinishDate: string | null
  /** Bugünün 00:00'ından itibaren gün. */
  startOffsetDays: number
  endOffsetDays: number | null
  plannedDate: string
  dueDate: string | null
  lateRisk: boolean
  lateDays: number | null
  fitWarning: string | null
}

export interface OrmeCapacityMachine {
  machineId: number
  machineCode: string
  machineName: string
  bedType: string
  diameterInch: number
  gauge: number
  isActive: boolean
  openStopCategory: OrmeStopCategory | null
  openStopReason: string | null
  queueHours: number
  queueEndsAt: string | null
  queueDays: number
  load7Percent: number
  load14Percent: number
  load30Percent: number
  lateRiskCount: number
  missingRateCount: number
  items: OrmeCapacityItem[]
}

export interface OrmeUnplannedKnitOrder {
  knitOrderId: number
  knitOrderNo: string
  orderId: number
  orderNo: string
  customerName: string | null
  fabricCode: string
  fabricName: string
  bedType: string
  diameterInch: number | null
  gauge: number | null
  plannedDate: string
  dueDate: string | null
  remainingKilograms: number
  kilogramsPerHour: number | null
  durationHours: number | null
  note: string | null
}

export interface OrmeCapacityPlan {
  generatedAt: string
  today: string
  days: number
  hoursPerDay: number
  hoursSource: string
  machines: OrmeCapacityMachine[]
  unplanned: OrmeUnplannedKnitOrder[]
}

export interface OrmeCapacityMachineOption {
  machineId: number
  code: string
  name: string
  diameterInch: number
  gauge: number
  fitWarning: string | null
  /** Emir şu anda bu makinenin kuyruğunda. */
  current: boolean
  queueCount: number
  queueEndsAt: string | null
  openStopCategory: OrmeStopCategory | null
}

export const ormePlanningApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getOrmeCapacityPlan: build.query<OrmeCapacityPlan, { days: number; hoursPerDay?: number }>({
      query: (params) => ({ url: '/orme-capacity-plan', params }),
      providesTags: ['OrmeKnitOrder', 'MasterData'],
    }),
    getOrmeCapacityOptions: build.query<OrmeCapacityMachineOption[], number>({
      query: (knitOrderId) => `/orme-capacity-plan/knit-orders/${knitOrderId}/options`,
      providesTags: ['OrmeKnitOrder', 'MasterData'],
    }),
    planOrmeKnitOrder: build.mutation<null, { knitOrderId: number; machineId: number | null; plannedDate?: string | null; position?: number | null }>({
      query: ({ knitOrderId, ...body }) => ({ url: `/orme-capacity-plan/knit-orders/${knitOrderId}`, method: 'PUT', body }),
      invalidatesTags: ['OrmeKnitOrder'],
    }),
    reorderOrmeMachineQueue: build.mutation<null, { machineId: number; knitOrderIds: number[] }>({
      query: ({ machineId, knitOrderIds }) => ({ url: `/orme-capacity-plan/machines/${machineId}/queue`, method: 'PUT', body: { knitOrderIds } }),
      invalidatesTags: ['OrmeKnitOrder'],
    }),
  }),
})

export const {
  useGetOrmeCapacityPlanQuery,
  useGetOrmeCapacityOptionsQuery,
  usePlanOrmeKnitOrderMutation,
  useReorderOrmeMachineQueueMutation,
} = ormePlanningApi
