import { baseApi } from '../../api/baseApi'
import type { PagedResult } from '../../types/api'

export type DyeMachineStatus = 'AVAILABLE' | 'BREAKDOWN' | 'MAINTENANCE'
export type BoyahanePauseKind = 'BREAK' | 'PAUSE' | 'MACHINE_BREAKDOWN' | 'WAITING_FOR_BATCH'
export type BoyahaneSessionEndReason = 'LOGOUT' | 'SHIFT_ENDED' | 'TAKEN_OVER' | 'MOVED_AWAY' | 'CLOSED_BY_OFFICE'

export const sessionEndReasonLabels: Record<BoyahaneSessionEndReason, string> = {
  LOGOUT: 'Çıkış',
  SHIFT_ENDED: 'Vardiya bitti',
  TAKEN_OVER: 'Devralındı',
  MOVED_AWAY: 'Başka tablete geçti',
  CLOSED_BY_OFFICE: 'Ofisten kapatıldı',
}

export const pauseKindLabels: Record<BoyahanePauseKind, string> = {
  BREAK: 'Molada',
  PAUSE: 'Ara',
  MACHINE_BREAKDOWN: 'Arıza',
  WAITING_FOR_BATCH: 'Parti bekliyor',
}

export interface BoyahaneMachineStatus {
  machineId: number
  machineCode: string
  status: DyeMachineStatus
  history: { status: DyeMachineStatus; startedAt: string; endedAt: string | null; reason: string | null; changedBy: string }[]
}

export interface BoyahaneWorkSession {
  id: number
  tabletCode: string
  machineCode: string | null
  operatorCode: string
  operatorName: string
  startedAt: string
  endedAt: string | null
  endReason: BoyahaneSessionEndReason | null
  pauseKind: BoyahanePauseKind | null
  hasWorkInProgress: boolean
}

export const boyahaneFloorApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getBoyahaneMachineStatus: build.query<BoyahaneMachineStatus, number>({
      query: (id) => `/boyahane-machines/${id}/status`,
      providesTags: ['MasterData'],
    }),
    changeBoyahaneMachineStatus: build.mutation<BoyahaneMachineStatus, { machineId: number; status: DyeMachineStatus; reason: string | null }>({
      query: ({ machineId, status, reason }) => ({ url: `/boyahane-machines/${machineId}/status`, method: 'POST', body: { status, reason } }),
      invalidatesTags: ['MasterData', 'BoyahaneBatch'],
    }),
    getBoyahaneWorkSessions: build.query<PagedResult<BoyahaneWorkSession>, Record<string, unknown>>({
      query: (params) => ({ url: '/boyahane-work-sessions', params }),
      providesTags: ['BoyahaneBatch'],
    }),
    closeBoyahaneWorkSession: build.mutation<BoyahaneWorkSession, number>({
      query: (id) => ({ url: `/boyahane-work-sessions/${id}/close`, method: 'POST' }),
      invalidatesTags: ['BoyahaneBatch'],
    }),
  }),
})

export const {
  useGetBoyahaneMachineStatusQuery,
  useChangeBoyahaneMachineStatusMutation,
  useGetBoyahaneWorkSessionsQuery,
  useCloseBoyahaneWorkSessionMutation,
} = boyahaneFloorApi
