import { baseApi } from '../../api/baseApi'
import type { GridParams } from '../../components/grid/useGrid'
import type { PagedResult } from '../../types/api'

/** Ana veri kayıtları ortak ekranda yapılandırmaya göre çizildiği için alanlar genel tutulur. */
export type MasterRow = { id: number; isActive: boolean } & Record<string, unknown>

export interface LookupItem {
  id: number
  code: string
  name: string
  /** Kodun ve adın yanında gösterilen ek bilgi (ör. modelin müşterisi). */
  detail?: string | null
}

export type MasterListParams = {
  resource: string
  page: number
  pageSize: number
  search?: string
} & GridParams

// Kaynaklar birbirine bağlıdır (müşteri adı model listesinde görünür); bu yüzden herhangi bir
// değişiklik tüm ana veri listelerini ve seçim kutularını tazeler.
export const masterDataApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getMasterList: build.query<PagedResult<MasterRow>, MasterListParams>({
      query: ({ resource, ...params }) => ({ url: `/${resource}`, params }),
      providesTags: ['MasterData'],
    }),
    getLookup: build.query<LookupItem[], { resource: string; search?: string; includeId?: number; take?: number; customerId?: number }>({
      query: ({ resource, ...params }) => ({ url: `/${resource}/lookup`, params }),
      providesTags: ['MasterData'],
    }),
    saveMasterRecord: build.mutation<MasterRow, { resource: string; id?: number; body: Record<string, unknown> }>({
      query: ({ resource, id, body }) => ({
        url: id === undefined ? `/${resource}` : `/${resource}/${id}`,
        method: id === undefined ? 'POST' : 'PUT',
        body,
      }),
      invalidatesTags: ['MasterData'],
    }),
    deleteMasterRecord: build.mutation<null, { resource: string; id: number }>({
      query: ({ resource, id }) => ({ url: `/${resource}/${id}`, method: 'DELETE' }),
      invalidatesTags: ['MasterData'],
    }),
  }),
})

export const {
  useGetMasterListQuery,
  useGetLookupQuery,
  useSaveMasterRecordMutation,
  useDeleteMasterRecordMutation,
} = masterDataApi
