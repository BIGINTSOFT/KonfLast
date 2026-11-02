import { baseApi } from '../../api/baseApi'
import type { PagedResult } from '../../types/api'
import type { DokumaFabricRoll, DokumaFabricRollStatus } from './dokumaWeavingApi'

export interface DokumaRollDefect {
  id: number
  defectTypeId: number
  defectCode: string
  defectName: string
  atMeter: number
  lengthCm: number
  points: number
}

export interface DokumaRollInspection {
  roll: DokumaFabricRoll
  /** Artikelin ham eni; forma önerilir. */
  rawWidthCm: number
  /** Bu topa uygulanan sınırlar (müşteriye özel ya da genel). */
  firstQualityMaxPoints: number
  secondQualityMaxPoints: number
  totalPoints: number | null
  calculatedStatus: DokumaFabricRollStatus | null
  overrideNote: string | null
  inspectionNotes: string | null
  defects: DokumaRollDefect[]
}

export interface DokumaQualitySettings {
  firstQualityMaxPoints: number
  secondQualityMaxPoints: number
}

const tags = ['DokumaWeaving'] as const

export const dokumaInspectionApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getDokumaInspectionRolls: build.query<PagedResult<DokumaFabricRoll>, Record<string, unknown>>({
      query: (params) => ({ url: '/dokuma-inspection/rolls', params }),
      providesTags: [...tags],
    }),
    getDokumaRollInspection: build.query<DokumaRollInspection, number>({
      query: (id) => `/dokuma-inspection/rolls/${id}`,
      providesTags: [...tags],
    }),
    saveDokumaRollInspection: build.mutation<DokumaRollInspection, {
      id: number
      inspectedOn: string
      meters: number
      widthCm: number
      notes: string | null
      defects: { defectTypeId: number; atMeter: number; lengthCm: number }[]
    }>({
      query: ({ id, ...body }) => ({ url: `/dokuma-inspection/rolls/${id}`, method: 'PUT', body }),
      invalidatesTags: [...tags],
    }),
    overrideDokumaRollGrade: build.mutation<DokumaRollInspection, { id: number; status: DokumaFabricRollStatus; note: string | null }>({
      query: ({ id, ...body }) => ({ url: `/dokuma-inspection/rolls/${id}/override`, method: 'POST', body }),
      invalidatesTags: [...tags],
    }),
    getDokumaQualitySettings: build.query<DokumaQualitySettings, void>({
      query: () => '/dokuma-inspection/settings',
      providesTags: [...tags],
    }),
    saveDokumaQualitySettings: build.mutation<DokumaQualitySettings, DokumaQualitySettings>({
      query: (body) => ({ url: '/dokuma-inspection/settings', method: 'PUT', body }),
      invalidatesTags: [...tags],
    }),
  }),
})

export const {
  useGetDokumaInspectionRollsQuery,
  useGetDokumaRollInspectionQuery,
  useSaveDokumaRollInspectionMutation,
  useOverrideDokumaRollGradeMutation,
  useGetDokumaQualitySettingsQuery,
  useSaveDokumaQualitySettingsMutation,
} = dokumaInspectionApi
