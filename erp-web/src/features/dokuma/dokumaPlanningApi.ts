import { baseApi } from '../../api/baseApi'
import type { DokumaStopCategory } from './dokumaFloorApi'

export type DokumaPlanBeamStatus = 'WARPED' | 'AT_SIZING' | 'READY' | 'ON_LOOM' | 'FINISHED'
export type DokumaPlanSheddingType = 'CAM' | 'DOBBY' | 'JACQUARD'

export const dokumaPlanBeamStatusLabels: Record<DokumaPlanBeamStatus, string> = {
  WARPED: 'Çözgüden çıktı',
  AT_SIZING: 'Haşılda',
  READY: 'Tezgâha hazır',
  ON_LOOM: 'Tezgâhta',
  FINISHED: 'Bitti',
}

/** Tezgâh kuyruğundaki levent; saatler UTC, ofsetler bugünün başından gün. */
export interface DokumaCapacityItem {
  beamId: number
  beamNo: string
  beamStatus: DokumaPlanBeamStatus
  warpOrderId: number
  warpNo: string
  orderId: number
  orderNo: string
  customerName: string | null
  articleCode: string
  articleName: string
  running: boolean
  sequence: number | null
  beamMeters: number
  /** Levent metresi ÷ (1 + çözgü çekmesi). */
  expectedFabricMeters: number
  wovenMeters: number
  remainingMeters: number
  picksPerCm: number
  metersPerHour: number | null
  durationHours: number | null
  estimatedStart: string
  estimatedFinish: string | null
  estimatedFinishDate: string | null
  startOffsetDays: number
  endOffsetDays: number | null
  dueDate: string | null
  /** Levent henüz tezgâha hazır değil. */
  notReady: boolean
  lateRisk: boolean
  lateDays: number | null
  fitWarning: string | null
}

export interface DokumaCapacityLoom {
  loomId: number
  loomCode: string
  loomName: string
  loomType: string
  sheddingType: DokumaPlanSheddingType
  maxReedWidthCm: number | null
  nominalSpeedRpm: number | null
  efficiencyPercent: number
  isActive: boolean
  openStopCategory: DokumaStopCategory | null
  openStopReason: string | null
  queueHours: number
  queueEndsAt: string | null
  queueDays: number
  load7Percent: number
  load14Percent: number
  load30Percent: number
  lateRiskCount: number
  missingRateCount: number
  items: DokumaCapacityItem[]
}

export interface DokumaUnplannedBeam {
  beamId: number
  beamNo: string
  beamStatus: DokumaPlanBeamStatus
  warpOrderId: number
  warpNo: string
  orderId: number
  orderNo: string
  customerName: string | null
  articleCode: string
  articleName: string
  sheddingType: DokumaPlanSheddingType
  reedWidthCm: number
  picksPerCm: number
  readyOn: string | null
  dueDate: string | null
  expectedFabricMeters: number
  note: string | null
}

export interface DokumaCapacityPlan {
  generatedAt: string
  today: string
  days: number
  hoursPerDay: number
  hoursSource: string
  defaultEfficiencyPercent: number
  looms: DokumaCapacityLoom[]
  unplanned: DokumaUnplannedBeam[]
}

export interface DokumaCapacityLoomOption {
  loomId: number
  code: string
  name: string
  sheddingType: DokumaPlanSheddingType
  maxReedWidthCm: number | null
  nominalSpeedRpm: number | null
  /** Doluysa bu tezgâha planlanamaz. */
  fitWarning: string | null
  current: boolean
  queueCount: number
  queueEndsAt: string | null
  durationHours: number | null
  openStopCategory: DokumaStopCategory | null
}

export const dokumaPlanningApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getDokumaCapacityPlan: build.query<DokumaCapacityPlan, { days: number; hoursPerDay?: number }>({
      query: (params) => ({ url: '/dokuma-capacity-plan', params }),
      providesTags: ['DokumaWarp', 'DokumaWeaving', 'MasterData'],
    }),
    getDokumaCapacityOptions: build.query<DokumaCapacityLoomOption[], number>({
      query: (beamId) => `/dokuma-capacity-plan/beams/${beamId}/options`,
      providesTags: ['DokumaWarp', 'DokumaWeaving', 'MasterData'],
    }),
    planDokumaBeam: build.mutation<null, { beamId: number; loomId: number | null; position?: number | null }>({
      query: ({ beamId, ...body }) => ({ url: `/dokuma-capacity-plan/beams/${beamId}`, method: 'PUT', body }),
      invalidatesTags: ['DokumaWarp'],
    }),
    reorderDokumaLoomQueue: build.mutation<null, { loomId: number; beamIds: number[] }>({
      query: ({ loomId, beamIds }) => ({ url: `/dokuma-capacity-plan/looms/${loomId}/queue`, method: 'PUT', body: { beamIds } }),
      invalidatesTags: ['DokumaWarp'],
    }),
  }),
})

export const {
  useGetDokumaCapacityPlanQuery,
  useGetDokumaCapacityOptionsQuery,
  usePlanDokumaBeamMutation,
  useReorderDokumaLoomQueueMutation,
} = dokumaPlanningApi
