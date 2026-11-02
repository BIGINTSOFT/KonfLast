import { baseApi } from '../../api/baseApi'
import type { LookupItem } from '../masterData/masterDataApi'

export interface DokumaOperatorLooms {
  operatorId: number
  operatorName: string
  /** Boşsa tezgâh kısıtı yoktur. */
  looms: LookupItem[]
}

export interface DokumaOperatorPhoto {
  contentType: string
  dataBase64: string
  updatedAt: string
}

export const dokumaOperatorApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getDokumaOperatorLooms: build.query<DokumaOperatorLooms, number>({
      query: (operatorId) => `/dokuma-operators/${operatorId}/looms`,
      // Tezgâh adı değişirse liste de tazelenir.
      providesTags: ['OperatorQualification', 'MasterData'],
    }),
    saveDokumaOperatorLooms: build.mutation<DokumaOperatorLooms, { operatorId: number; loomIds: number[] }>({
      query: ({ operatorId, ...body }) => ({ url: `/dokuma-operators/${operatorId}/looms`, method: 'PUT', body }),
      invalidatesTags: ['OperatorQualification'],
    }),
    // Fotoğrafı olmayan operatör için sunucu NOT_FOUND döner.
    getDokumaOperatorPhoto: build.query<DokumaOperatorPhoto, number>({
      query: (operatorId) => `/dokuma-operators/${operatorId}/photo`,
      providesTags: ['OperatorPhoto'],
    }),
    saveDokumaOperatorPhoto: build.mutation<null, { operatorId: number; contentType: string; dataBase64: string }>({
      query: ({ operatorId, ...body }) => ({ url: `/dokuma-operators/${operatorId}/photo`, method: 'PUT', body }),
      invalidatesTags: ['OperatorPhoto'],
    }),
    deleteDokumaOperatorPhoto: build.mutation<null, number>({
      query: (operatorId) => ({ url: `/dokuma-operators/${operatorId}/photo`, method: 'DELETE' }),
      invalidatesTags: ['OperatorPhoto'],
    }),
  }),
})

export const {
  useGetDokumaOperatorLoomsQuery,
  useSaveDokumaOperatorLoomsMutation,
  useGetDokumaOperatorPhotoQuery,
  useSaveDokumaOperatorPhotoMutation,
  useDeleteDokumaOperatorPhotoMutation,
} = dokumaOperatorApi
