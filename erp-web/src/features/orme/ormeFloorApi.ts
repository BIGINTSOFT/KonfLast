import { baseApi } from '../../api/baseApi'
import type { PagedResult } from '../../types/api'
import type { OrmeFabricRollStatus } from './ormeKnitApi'

export type OrmeStopCategory = 'PAUSE' | 'STOP' | 'BREAKDOWN' | 'PLANNED'
export type OrmePauseKind = 'BREAK' | 'PAUSE' | 'WAITING_FOR_ORDER'
export type OrmeSessionEndReason = 'LOGOUT' | 'SHIFT_ENDED' | 'TAKEN_OVER' | 'MOVED_AWAY' | 'CLOSED_BY_OFFICE'

export const ormeSessionEndReasonLabels: Record<OrmeSessionEndReason, string> = {
  LOGOUT: 'Çıkış',
  SHIFT_ENDED: 'Vardiya bitti',
  TAKEN_OVER: 'Devralındı',
  MOVED_AWAY: 'Başka tablete geçti',
  CLOSED_BY_OFFICE: 'Ofisten kapatıldı',
}

export const ormePauseKindLabels: Record<OrmePauseKind, string> = {
  BREAK: 'Molada',
  PAUSE: 'Ara',
  WAITING_FOR_ORDER: 'Emir bekliyor',
}

export interface OrmeWorkSession {
  id: number
  tabletCode: string
  machineCode: string | null
  operatorCode: string
  operatorName: string
  startedAt: string
  endedAt: string | null
  endReason: OrmeSessionEndReason | null
  pauseKind: OrmePauseKind | null
}

export interface OrmeMachineStop {
  id: number
  machineId: number
  machineCode: string
  knitOrderNo: string | null
  category: OrmeStopCategory
  reasonName: string
  note: string | null
  startedAt: string
  endedAt: string | null
  startedBy: string
  endedBy: string | null
}

export interface OrmeStopReasonOption {
  id: number
  code: string
  name: string
  category: OrmeStopCategory
  isActive: boolean
}

export interface OrmeMachineReportRow {
  machineId: number
  machineCode: string
  machineName: string
  rollCount: number
  knittedKilograms: number
  loadedDays: number
  stopMinutes: number
  breakdownMinutes: number
  plannedMinutes: number
  availableMinutes: number
  runningPercent: number | null
  efficiencyPercent: number | null
  /** Örülen kumaşlardan birinin kartında beklenen kg/saat yok. */
  missingExpectedRate: boolean
}

export interface OrmeOrderReportRow {
  orderLineId: number
  orderId: number
  orderNo: string
  customerName: string | null
  fabricCode: string
  fabricName: string
  dueDate: string | null
  orderedKilograms: number
  plannedKilograms: number
  knittedKilograms: number
  firstQualityKilograms: number
  secondQualityKilograms: number
  rejectedKilograms: number
  packedKilograms: number
  shippedKilograms: number
  invoicedKilograms: number
}

export interface OrmeQualityGroup {
  code: string
  name: string
  rollCount: number
  kilograms: number
  firstQualityKilograms: number
  secondQualityKilograms: number
  rejectedKilograms: number
  averagePointsPer100M2: number | null
  /** Puanı tahmini metreyle hesaplanan top. */
  estimatedMeterRollCount: number
  /** En ya da gramajı kumaş kartından sapan top. */
  deviationRollCount: number
}

export interface OrmeQualityReport {
  total: OrmeQualityGroup
  byFabric: OrmeQualityGroup[]
  byMachine: OrmeQualityGroup[]
  defects: { code: string; name: string; count: number; points: number }[]
}

export interface OrmeDailyShipmentRow {
  /** Boyahane satırında boş. */
  customerId: number | null
  customerName: string
  /** Boyahane'ye gönderilen çeki listeleri (irsaliyesiz). */
  toBoyahane: boolean
  /** Onaylı irsaliye sayısı; Boyahane satırında gönderilen çeki listesi sayısı. */
  despatchCount: number
  rollCount: number
  kilograms: number
}

export interface OrmeDailyShipmentDay {
  date: string
  rows: OrmeDailyShipmentRow[]
  despatchCount: number
  rollCount: number
  kilograms: number
}

export interface OrmeDailyShipmentReport {
  days: OrmeDailyShipmentDay[]
  despatchCount: number
  rollCount: number
  kilograms: number
}

export interface OrmeIdleYarnLot {
  id: number
  lotNo: string
  yarnCode: string
  yarnName: string
  yarnColor: string | null
  /** Malın sahibi; boşsa işletme malı. */
  customerName: string | null
  remainingKilograms: number
  /** Son verme, geri alma ya da iade; hareket yoksa giriş tarihi. */
  lastMovementOn: string
  idleDays: number
}

export interface OrmeIdleRoll {
  id: number
  rollNo: string
  knitOrderId: number
  knitOrderNo: string
  orderNo: string
  customerName: string | null
  fabricCode: string
  fabricName: string
  kilograms: number
  status: OrmeFabricRollStatus
  /** Kontrol tarihi; kontrol edilmediyse kesim tarihi. */
  lastChangeOn: string
  idleDays: number
}

export interface OrmeIdleStockReport {
  days: number
  since: string
  yarnLots: OrmeIdleYarnLot[]
  yarnKilograms: number
  rolls: OrmeIdleRoll[]
  rollCount: number
  rollKilograms: number
  /** Top listesi en eski toplarla sınırlandı; sayı ve kilo bütün topları kapsar. */
  rollsTruncated: boolean
}

const floorTags = ['OrmeKnitOrder', 'OrmeReport'] as const

export const ormeFloorApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    resetOrmeTabletDevice: build.mutation<null, number>({
      query: (id) => ({ url: `/orme-tablets/${id}/reset-device`, method: 'POST' }),
      invalidatesTags: ['MasterData'],
    }),
    getOrmeWorkSessions: build.query<PagedResult<OrmeWorkSession>, Record<string, unknown>>({
      query: (params) => ({ url: '/orme-work-sessions', params }),
      providesTags: ['OrmeKnitOrder'],
    }),
    closeOrmeWorkSession: build.mutation<OrmeWorkSession, number>({
      query: (id) => ({ url: `/orme-work-sessions/${id}/close`, method: 'POST' }),
      invalidatesTags: ['OrmeKnitOrder'],
    }),
    getOrmeMachineStops: build.query<PagedResult<OrmeMachineStop>, Record<string, unknown>>({
      query: (params) => ({ url: '/orme-machine-stops', params }),
      providesTags: ['OrmeKnitOrder'],
    }),
    getOrmeStopReasonOptions: build.query<PagedResult<OrmeStopReasonOption>, void>({
      query: () => ({ url: '/orme-stop-reasons', params: { pageSize: 100 } }),
      providesTags: ['MasterData'],
    }),
    startOrmeMachineStop: build.mutation<null, { machineId: number; reasonId: number; note: string | null }>({
      query: ({ machineId, ...body }) => ({ url: `/orme-machine-stops/machines/${machineId}/start`, method: 'POST', body }),
      invalidatesTags: [...floorTags],
    }),
    endOrmeMachineStop: build.mutation<null, number>({
      query: (machineId) => ({ url: `/orme-machine-stops/machines/${machineId}/end`, method: 'POST' }),
      invalidatesTags: [...floorTags],
    }),
    getOrmeMachineReport: build.query<OrmeMachineReportRow[], { from: string; to: string; dailyHours: number }>({
      query: (params) => ({ url: '/orme-reports/machines', params }),
      providesTags: ['OrmeReport'],
    }),
    getOrmeOrderReport: build.query<PagedResult<OrmeOrderReportRow>, Record<string, unknown>>({
      query: (params) => ({ url: '/orme-reports/orders', params }),
      providesTags: ['OrmeReport'],
    }),
    getOrmeQualityReport: build.query<OrmeQualityReport, { from: string; to: string }>({
      query: (params) => ({ url: '/orme-reports/quality', params }),
      providesTags: ['OrmeReport'],
    }),
    getOrmeDailyShipmentReport: build.query<OrmeDailyShipmentReport, { from: string; to: string }>({
      query: (params) => ({ url: '/orme-reports/daily-shipments', params }),
      providesTags: ['OrmeReport', 'OrmeShipping'],
    }),
    getOrmeIdleStockReport: build.query<OrmeIdleStockReport, { days: number }>({
      query: (params) => ({ url: '/orme-reports/idle-stock', params }),
      providesTags: ['OrmeReport', 'OrmeShipping', 'OrmeKnitOrder', 'OrmeYarnLot'],
    }),
  }),
})

export const {
  useCloseOrmeWorkSessionMutation,
  useEndOrmeMachineStopMutation,
  useGetOrmeDailyShipmentReportQuery,
  useGetOrmeIdleStockReportQuery,
  useGetOrmeMachineReportQuery,
  useGetOrmeMachineStopsQuery,
  useGetOrmeOrderReportQuery,
  useGetOrmeQualityReportQuery,
  useGetOrmeStopReasonOptionsQuery,
  useGetOrmeWorkSessionsQuery,
  useResetOrmeTabletDeviceMutation,
  useStartOrmeMachineStopMutation,
} = ormeFloorApi
