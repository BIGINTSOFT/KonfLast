import { baseApi } from '../../api/baseApi'
import type { PagedResult } from '../../types/api'
import type { BeamStatus } from './dokumaWarpApi'
import type { DokumaFabricRollStatus } from './dokumaWeavingApi'

export type DokumaStopCategory = 'PAUSE' | 'STOP' | 'BREAKDOWN' | 'PLANNED'
export type DokumaPauseKind = 'BREAK' | 'PAUSE' | 'WAITING_FOR_BEAM'
export type DokumaSessionEndReason = 'LOGOUT' | 'SHIFT_ENDED' | 'TAKEN_OVER' | 'MOVED_AWAY' | 'CLOSED_BY_OFFICE'

export const dokumaSessionEndReasonLabels: Record<DokumaSessionEndReason, string> = {
  LOGOUT: 'Çıkış',
  SHIFT_ENDED: 'Vardiya bitti',
  TAKEN_OVER: 'Devralındı',
  MOVED_AWAY: 'Başka tablete geçti',
  CLOSED_BY_OFFICE: 'Ofisten kapatıldı',
}

export const dokumaPauseKindLabels: Record<DokumaPauseKind, string> = {
  BREAK: 'Molada',
  PAUSE: 'Ara',
  WAITING_FOR_BEAM: 'Levent bekliyor',
}

export interface DokumaWorkSession {
  id: number
  tabletCode: string
  loomCode: string | null
  operatorCode: string
  operatorName: string
  startedAt: string
  endedAt: string | null
  endReason: DokumaSessionEndReason | null
  pauseKind: DokumaPauseKind | null
}

export interface DokumaLoomStop {
  id: number
  loomId: number
  loomCode: string
  beamNo: string | null
  category: DokumaStopCategory
  reasonName: string
  note: string | null
  startedAt: string
  endedAt: string | null
  startedBy: string
  endedBy: string | null
}

export interface DokumaStopReasonOption {
  id: number
  code: string
  name: string
  category: DokumaStopCategory
  isActive: boolean
}

export interface DokumaLoomReportRow {
  loomId: number
  loomCode: string
  loomName: string
  nominalSpeedRpm: number | null
  rollCount: number
  wovenMeters: number
  loadedDays: number
  stopMinutes: number
  breakdownMinutes: number
  plannedMinutes: number
  availableMinutes: number
  runningPercent: number | null
  efficiencyPercent: number | null
}

export interface DokumaOrderReportRow {
  orderLineId: number
  orderId: number
  orderNo: string
  customerName: string | null
  articleCode: string
  articleName: string
  dueDate: string | null
  orderedMeters: number
  beamMeters: number
  wovenMeters: number
  firstQualityMeters: number
  secondQualityMeters: number
  rejectedMeters: number
  packedMeters: number
  shippedMeters: number
  invoicedMeters: number
}

export interface DokumaQualityGroup {
  code: string
  name: string
  rollCount: number
  meters: number
  firstQualityMeters: number
  secondQualityMeters: number
  rejectedMeters: number
  averagePointsPer100M2: number | null
}

export interface DokumaQualityReport {
  total: DokumaQualityGroup
  byArticle: DokumaQualityGroup[]
  byLoom: DokumaQualityGroup[]
  defects: { code: string; name: string; count: number; points: number }[]
}

/** Günlük sevk satırı: bir günde bir müşteriye ya da Boyahane'ye giden. */
export interface DokumaShipmentRow {
  customerId: number | null
  /** Müşteri adı; Boyahane'ye gidende "Boyahane'ye". */
  name: string
  isBoyahane: boolean
  /** İrsaliye sayısı; Boyahane'ye gidende çeki listesi sayısı. */
  documentCount: number
  rollCount: number
  meters: number
  /** Tartılmış topların kilosu. */
  kilograms: number
  /** Sıfırdan büyükse kilo eksiktir. */
  unweighedRollCount: number
}

export interface DokumaShipmentReport {
  days: { day: string; rows: DokumaShipmentRow[]; total: DokumaShipmentRow }[]
  total: DokumaShipmentRow
}

export interface DokumaIdleStock {
  days: number
  cutoff: string
  yarnLots: { lotId: number; lotNo: string; yarnCode: string; yarnName: string; ownerName: string | null; receiptDate: string; remainingKilograms: number; lastMovementOn: string; idleDays: number }[]
  rolls: { rollId: number; rollNo: string; articleCode: string; articleName: string; orderNo: string; customerName: string | null; meters: number; status: DokumaFabricRollStatus; since: string; idleDays: number }[]
  beams: { beamId: number; beamNo: string; warpNo: string; orderNo: string; articleCode: string; meters: number; status: BeamStatus; since: string; idleDays: number }[]
  /** Top listesi üst sınırda kesildi. */
  rollsTruncated: boolean
}

const floorTags = ['DokumaWeaving', 'DokumaReport'] as const

export const dokumaFloorApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    resetDokumaTabletDevice: build.mutation<null, number>({
      query: (id) => ({ url: `/dokuma-tablets/${id}/reset-device`, method: 'POST' }),
      invalidatesTags: ['MasterData'],
    }),
    getDokumaWorkSessions: build.query<PagedResult<DokumaWorkSession>, Record<string, unknown>>({
      query: (params) => ({ url: '/dokuma-work-sessions', params }),
      providesTags: ['DokumaWeaving'],
    }),
    closeDokumaWorkSession: build.mutation<DokumaWorkSession, number>({
      query: (id) => ({ url: `/dokuma-work-sessions/${id}/close`, method: 'POST' }),
      invalidatesTags: ['DokumaWeaving'],
    }),
    getDokumaLoomStops: build.query<PagedResult<DokumaLoomStop>, Record<string, unknown>>({
      query: (params) => ({ url: '/dokuma-loom-stops', params }),
      providesTags: ['DokumaWeaving'],
    }),
    getDokumaStopReasonOptions: build.query<PagedResult<DokumaStopReasonOption>, void>({
      query: () => ({ url: '/dokuma-stop-reasons', params: { pageSize: 100 } }),
      providesTags: ['MasterData'],
    }),
    startDokumaLoomStop: build.mutation<null, { loomId: number; reasonId: number; note: string | null }>({
      query: ({ loomId, ...body }) => ({ url: `/dokuma-loom-stops/looms/${loomId}/start`, method: 'POST', body }),
      invalidatesTags: [...floorTags],
    }),
    endDokumaLoomStop: build.mutation<null, number>({
      query: (loomId) => ({ url: `/dokuma-loom-stops/looms/${loomId}/end`, method: 'POST' }),
      invalidatesTags: [...floorTags],
    }),
    getDokumaLoomReport: build.query<DokumaLoomReportRow[], { from: string; to: string; dailyHours: number }>({
      query: (params) => ({ url: '/dokuma-reports/looms', params }),
      providesTags: ['DokumaReport'],
    }),
    getDokumaOrderReport: build.query<PagedResult<DokumaOrderReportRow>, Record<string, unknown>>({
      query: (params) => ({ url: '/dokuma-reports/orders', params }),
      providesTags: ['DokumaReport'],
    }),
    getDokumaQualityReport: build.query<DokumaQualityReport, { from: string; to: string }>({
      query: (params) => ({ url: '/dokuma-reports/quality', params }),
      providesTags: ['DokumaReport'],
    }),
    getDokumaShipmentReport: build.query<DokumaShipmentReport, { from: string; to: string }>({
      query: (params) => ({ url: '/dokuma-reports/shipments', params }),
      providesTags: ['DokumaReport', 'DokumaShipping'],
    }),
    getDokumaIdleStock: build.query<DokumaIdleStock, { days: number }>({
      query: (params) => ({ url: '/dokuma-reports/idle-stock', params }),
      providesTags: ['DokumaReport', 'DokumaShipping', 'DokumaWeaving', 'DokumaWarp', 'DokumaYarnLot'],
    }),
  }),
})

export const {
  useCloseDokumaWorkSessionMutation,
  useEndDokumaLoomStopMutation,
  useGetDokumaLoomReportQuery,
  useGetDokumaLoomStopsQuery,
  useGetDokumaOrderReportQuery,
  useGetDokumaIdleStockQuery,
  useGetDokumaQualityReportQuery,
  useGetDokumaShipmentReportQuery,
  useGetDokumaStopReasonOptionsQuery,
  useGetDokumaWorkSessionsQuery,
  useResetDokumaTabletDeviceMutation,
  useStartDokumaLoomStopMutation,
} = dokumaFloorApi
