import { baseApi } from '../../api/baseApi'
import type { LookupItem } from '../masterData/masterDataApi'

export interface MachineStatusLog {
  id: number
  status: string
  startedAt: string
  endedAt: string | null
  durationMinutes: number
  reason: string | null
  changedBy: string
  reportedFromTablet: boolean
}

/** Makinenin ana operasyonuna ek olarak yapabildiği operasyonlar. */
export interface MachineOperations {
  machineId: number
  operations: LookupItem[]
}

export interface MachineStatusInfo {
  machineId: number
  machineCode: string
  machineName: string
  status: string
  /** Sunucunun izin verdiği hedef durumlar; seçim kutusu buna göre dolar. */
  allowedTargets: string[]
  /** Şu anki durum için kaydedilmiş neden (ör. arıza nedeni). */
  statusReason?: string | null
  history: MachineStatusLog[]
}

export const tabletsApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    resetTabletDevice: build.mutation<null, number>({
      query: (tabletId) => ({ url: `/tablets/${tabletId}/reset-device`, method: 'POST' }),
      invalidatesTags: ['MasterData'],
    }),
    getMachineStatus: build.query<MachineStatusInfo, number>({
      query: (machineId) => `/machines/${machineId}/status`,
      providesTags: ['MachineStatus'],
    }),
    getMachineOperations: build.query<MachineOperations, number>({
      query: (machineId) => `/machines/${machineId}/operations`,
      // Operasyon adı değişirse liste de tazelenir.
      providesTags: ['MachineOperation', 'MasterData'],
    }),
    saveMachineOperations: build.mutation<MachineOperations, { machineId: number; operationIds: number[] }>({
      query: ({ machineId, operationIds }) => ({
        url: `/machines/${machineId}/operations`,
        method: 'PUT',
        body: { operationIds },
      }),
      invalidatesTags: ['MachineOperation'],
    }),
    changeMachineStatus: build.mutation<MachineStatusInfo, { machineId: number; status: string; reason: string | null }>({
      query: ({ machineId, ...body }) => ({ url: `/machines/${machineId}/status`, method: 'POST', body }),
      // Makine durumu makine listesinde ve bant ekranında da görünür.
      invalidatesTags: ['MachineStatus', 'MasterData', 'ProductionLine'],
    }),
  }),
})

export const {
  useResetTabletDeviceMutation,
  useGetMachineStatusQuery,
  useChangeMachineStatusMutation,
  useGetMachineOperationsQuery,
  useSaveMachineOperationsMutation,
} = tabletsApi
