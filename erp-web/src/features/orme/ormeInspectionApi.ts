import { baseApi } from '../../api/baseApi'
import type { PagedResult } from '../../types/api'
import type { OrmeFabricRoll, OrmeFabricRollStatus } from './ormeKnitApi'

export interface OrmeRollDefect {
  id: number
  defectTypeId: number
  defectCode: string
  defectName: string
  atMeter: number
  lengthCm: number
  points: number
}

export interface OrmeRollInspection {
  roll: OrmeFabricRoll
  /** Kumaş kartındaki ham en ve gramaj; ölçümle karşılaştırılır. */
  cardWidthCm: number | null
  cardWeightGsm: number
  /** Bu topa uygulanan sınırlar (müşteriye özel ya da genel). */
  firstQualityMaxPoints: number
  secondQualityMaxPoints: number
  totalPoints: number | null
  calculatedStatus: OrmeFabricRollStatus | null
  inspectionNotes: string | null
  defects: OrmeRollDefect[]
}

export interface OrmeQualitySettings {
  firstQualityMaxPoints: number
  secondQualityMaxPoints: number
  /** Ölçümün karttan sapma sınırı; aşılırsa uyarı çıkar, kalite değişmez. */
  widthTolerancePercent: number
  weightTolerancePercent: number
}

const tags = ['OrmeKnitOrder'] as const

export const ormeInspectionApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getOrmeInspectionRolls: build.query<PagedResult<OrmeFabricRoll>, Record<string, unknown>>({
      query: (params) => ({ url: '/orme-inspection/rolls', params }),
      providesTags: [...tags],
    }),
    getOrmeRollInspection: build.query<OrmeRollInspection, number>({
      query: (id) => `/orme-inspection/rolls/${id}`,
      providesTags: [...tags],
    }),
    saveOrmeRollInspection: build.mutation<OrmeRollInspection, {
      id: number
      inspectedOn: string
      /** Boşsa makinede girilen metre, o da yoksa tahmin kullanılır. */
      meters: number | null
      widthCm: number
      weightGsm: number
      notes: string | null
      defects: { defectTypeId: number; atMeter: number; lengthCm: number }[]
    }>({
      query: ({ id, ...body }) => ({ url: `/orme-inspection/rolls/${id}`, method: 'PUT', body }),
      invalidatesTags: [...tags],
    }),
    overrideOrmeRollGrade: build.mutation<OrmeRollInspection, { id: number; status: OrmeFabricRollStatus; note: string | null }>({
      query: ({ id, ...body }) => ({ url: `/orme-inspection/rolls/${id}/override`, method: 'POST', body }),
      invalidatesTags: [...tags],
    }),
    getOrmeQualitySettings: build.query<OrmeQualitySettings, void>({
      query: () => '/orme-inspection/settings',
      providesTags: [...tags],
    }),
    saveOrmeQualitySettings: build.mutation<OrmeQualitySettings, OrmeQualitySettings>({
      query: (body) => ({ url: '/orme-inspection/settings', method: 'PUT', body }),
      invalidatesTags: [...tags],
    }),
  }),
})

export const {
  useGetOrmeInspectionRollsQuery,
  useGetOrmeRollInspectionQuery,
  useSaveOrmeRollInspectionMutation,
  useOverrideOrmeRollGradeMutation,
  useGetOrmeQualitySettingsQuery,
  useSaveOrmeQualitySettingsMutation,
} = ormeInspectionApi
