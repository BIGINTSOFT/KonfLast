import { baseApi } from '../../api/baseApi'
import type { LookupItem } from '../masterData/masterDataApi'

export interface OrmeOperatorMachines {
  operatorId: number
  operatorName: string
  /** Boşsa makine kısıtı yoktur. */
  machines: LookupItem[]
}

export interface OrmeOperatorPhoto {
  contentType: string
  dataBase64: string
  updatedAt: string
}

export const ormeOperatorApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getOrmeOperatorMachines: build.query<OrmeOperatorMachines, number>({
      query: (operatorId) => `/orme-operators/${operatorId}/machines`,
      // Makine adı değişirse liste de tazelenir.
      providesTags: ['OperatorQualification', 'MasterData'],
    }),
    saveOrmeOperatorMachines: build.mutation<OrmeOperatorMachines, { operatorId: number; machineIds: number[] }>({
      query: ({ operatorId, ...body }) => ({ url: `/orme-operators/${operatorId}/machines`, method: 'PUT', body }),
      invalidatesTags: ['OperatorQualification'],
    }),
    // Fotoğrafı olmayan operatör için sunucu NOT_FOUND döner.
    getOrmeOperatorPhoto: build.query<OrmeOperatorPhoto, number>({
      query: (operatorId) => `/orme-operators/${operatorId}/photo`,
      providesTags: ['OperatorPhoto'],
    }),
    saveOrmeOperatorPhoto: build.mutation<null, { operatorId: number; contentType: string; dataBase64: string }>({
      query: ({ operatorId, ...body }) => ({ url: `/orme-operators/${operatorId}/photo`, method: 'PUT', body }),
      invalidatesTags: ['OperatorPhoto'],
    }),
    deleteOrmeOperatorPhoto: build.mutation<null, number>({
      query: (operatorId) => ({ url: `/orme-operators/${operatorId}/photo`, method: 'DELETE' }),
      invalidatesTags: ['OperatorPhoto'],
    }),
  }),
})

export const {
  useGetOrmeOperatorMachinesQuery,
  useSaveOrmeOperatorMachinesMutation,
  useGetOrmeOperatorPhotoQuery,
  useSaveOrmeOperatorPhotoMutation,
  useDeleteOrmeOperatorPhotoMutation,
} = ormeOperatorApi
