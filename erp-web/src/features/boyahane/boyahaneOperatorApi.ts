import { baseApi } from '../../api/baseApi'
import type { LookupItem } from '../masterData/masterDataApi'

export interface BoyahaneOperatorQualifications {
  operatorId: number
  operatorName: string
  operations: LookupItem[]
  /** Boşsa makine kısıtı yoktur. */
  machines: LookupItem[]
}

export interface BoyahaneOperatorPhoto {
  contentType: string
  dataBase64: string
  updatedAt: string
}

export const boyahaneOperatorApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getBoyahaneOperatorQualifications: build.query<BoyahaneOperatorQualifications, number>({
      query: (operatorId) => `/boyahane-operators/${operatorId}/qualifications`,
      // Operasyon / makine adı değişirse liste de tazelenir.
      providesTags: ['OperatorQualification', 'MasterData'],
    }),
    saveBoyahaneOperatorQualifications: build.mutation<
      BoyahaneOperatorQualifications,
      { operatorId: number; operationIds: number[]; machineIds: number[] }
    >({
      query: ({ operatorId, ...body }) => ({ url: `/boyahane-operators/${operatorId}/qualifications`, method: 'PUT', body }),
      invalidatesTags: ['OperatorQualification'],
    }),
    // Fotoğrafı olmayan operatör için sunucu NOT_FOUND döner.
    getBoyahaneOperatorPhoto: build.query<BoyahaneOperatorPhoto, number>({
      query: (operatorId) => `/boyahane-operators/${operatorId}/photo`,
      providesTags: ['OperatorPhoto'],
    }),
    saveBoyahaneOperatorPhoto: build.mutation<null, { operatorId: number; contentType: string; dataBase64: string }>({
      query: ({ operatorId, ...body }) => ({ url: `/boyahane-operators/${operatorId}/photo`, method: 'PUT', body }),
      invalidatesTags: ['OperatorPhoto'],
    }),
    deleteBoyahaneOperatorPhoto: build.mutation<null, number>({
      query: (operatorId) => ({ url: `/boyahane-operators/${operatorId}/photo`, method: 'DELETE' }),
      invalidatesTags: ['OperatorPhoto'],
    }),
  }),
})

export const {
  useGetBoyahaneOperatorQualificationsQuery,
  useSaveBoyahaneOperatorQualificationsMutation,
  useGetBoyahaneOperatorPhotoQuery,
  useSaveBoyahaneOperatorPhotoMutation,
  useDeleteBoyahaneOperatorPhotoMutation,
} = boyahaneOperatorApi
