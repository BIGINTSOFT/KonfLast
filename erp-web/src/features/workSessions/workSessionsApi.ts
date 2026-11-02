import { baseApi } from '../../api/baseApi'
import type { GridParams } from '../../components/grid/useGrid'
import type { PagedResult } from '../../types/api'
import type { LookupItem } from '../masterData/masterDataApi'

export type WorkSessionStatus = 'ACTIVE' | 'LOGGED_OUT' | 'BREAK' | 'PAUSED' | 'MACHINE_BREAKDOWN' | 'WAITING_FOR_BUNDLE'

export type WorkSessionEndReason =
  | 'LOGOUT'
  | 'TAKEN_OVER'
  | 'MOVED_TO_ANOTHER_MACHINE'
  | 'CLOSED_BY_SUPERVISOR'
  | 'SHIFT_ENDED'

export type WorkSessionPauseType = 'BREAK' | 'PAUSE' | 'MACHINE_BREAKDOWN' | 'WAITING_FOR_BUNDLE'


export interface WorkSessionPause {
  id: number
  type: WorkSessionPauseType
  /** Yalnızca operatörün kendi arasında dolu. */
  /** Arada seçilen nedenin adı (Duruş Nedenleri ana verisinden). */
  reasonName: string | null
  /** Yalnızca planlı molada dolu. */
  breakName: string | null
  note: string | null
  startedAt: string
  endedAt: string | null
  durationMinutes: number
}

export interface WorkSession {
  id: number
  operatorId: number
  operatorCode: string
  operatorName: string
  machineId: number
  machineCode: string
  tabletCode: string
  loginAt: string
  logoutAt: string | null
  /** Sunucu hesaplar; açık oturumda şu ana kadar geçen süre. */
  durationMinutes: number
  status: WorkSessionStatus
  endReason: WorkSessionEndReason | null
  closedBy: string | null
  /** Sunucu hesaplar: oturum açık mı (yetkili kullanıcı kapatabilir). */
  canClose: boolean
}

export type WorkSessionListParams = {
  page: number
  pageSize: number
  search?: string
  isOpen?: boolean
} & GridParams

export interface OperatorQualifications {
  operatorId: number
  operatorName: string
  operations: LookupItem[]
  /** Boşsa makine kısıtı yoktur. */
  machines: LookupItem[]
}

export interface OperatorPhoto {
  contentType: string
  dataBase64: string
  updatedAt: string
}

export const workSessionsApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getWorkSessions: build.query<PagedResult<WorkSession>, WorkSessionListParams>({
      query: (params) => ({ url: '/work-sessions', params }),
      providesTags: ['WorkSession'],
    }),
    getWorkSessionPauses: build.query<WorkSessionPause[], number>({
      query: (id) => `/work-sessions/${id}/pauses`,
      providesTags: ['WorkSession'],
    }),
    /** releaseWork: operatörün süren işi adet bildirilmeden bırakılır, sonra oturum kapatılır. */
    closeWorkSession: build.mutation<WorkSession, { id: number; releaseWork?: boolean }>({
      query: ({ id, releaseWork }) => ({
        url: `/work-sessions/${id}/close`, method: 'POST', body: {}, params: releaseWork ? { releaseWork: true } : undefined,
      }),
      invalidatesTags: ['WorkSession', 'Bundle', 'MachineStatus', 'ProductionTracking', 'LiveDashboard'],
    }),
    getOperatorQualifications: build.query<OperatorQualifications, number>({
      query: (operatorId) => `/operators/${operatorId}/qualifications`,
      // Operasyon / makine adı değişirse liste de tazelenir.
      providesTags: ['OperatorQualification', 'MasterData'],
    }),
    saveOperatorQualifications: build.mutation<
      OperatorQualifications,
      { operatorId: number; operationIds: number[]; machineIds: number[] }
    >({
      query: ({ operatorId, ...body }) => ({ url: `/operators/${operatorId}/qualifications`, method: 'PUT', body }),
      invalidatesTags: ['OperatorQualification'],
    }),
    // Fotoğrafı olmayan operatör için sunucu NOT_FOUND döner.
    getOperatorPhoto: build.query<OperatorPhoto, number>({
      query: (operatorId) => `/operators/${operatorId}/photo`,
      providesTags: ['OperatorPhoto'],
    }),
    saveOperatorPhoto: build.mutation<null, { operatorId: number; contentType: string; dataBase64: string }>({
      query: ({ operatorId, ...body }) => ({ url: `/operators/${operatorId}/photo`, method: 'PUT', body }),
      invalidatesTags: ['OperatorPhoto'],
    }),
    deleteOperatorPhoto: build.mutation<null, number>({
      query: (operatorId) => ({ url: `/operators/${operatorId}/photo`, method: 'DELETE' }),
      invalidatesTags: ['OperatorPhoto'],
    }),
  }),
})

export const {
  useGetWorkSessionsQuery,
  useGetWorkSessionPausesQuery,
  useCloseWorkSessionMutation,
  useGetOperatorQualificationsQuery,
  useSaveOperatorQualificationsMutation,
  useGetOperatorPhotoQuery,
  useSaveOperatorPhotoMutation,
  useDeleteOperatorPhotoMutation,
} = workSessionsApi
