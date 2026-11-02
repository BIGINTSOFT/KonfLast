import { baseApi } from '../../api/baseApi'
import type { PagedResult } from '../../types/api'

export interface DokumaYarnLot {
  id: number
  lotNo: string
  receiptDate: string
  /** Malın sahibi; boşsa işletme malıdır. */
  customerId: number | null
  customerName: string | null
  supplierId: number | null
  supplierName: string | null
  yarnId: number
  yarnCode: string
  yarnName: string
  yarnColor: string | null
  despatchNo: string | null
  producerLotNo: string | null
  /** Bobin sayısı. */
  packageCount: number
  kilograms: number
  /** Çözgüye verilen. */
  issuedKilograms: number
  /** Müşteriye iade edilen. */
  returnedKilograms: number
  remainingKilograms: number
  notes: string | null
  /** Satın alma siparişinden teslim alındıysa sipariş. */
  purchaseOrderId: number | null
  purchaseOrderNo: string | null
}

export interface DokumaYarnLotRequest {
  receiptDate: string
  yarnId: number
  kilograms: number
  packageCount: number
  customerId: number | null
  supplierId: number | null
  despatchNo: string | null
  producerLotNo: string | null
  notes: string | null
}

/** Üretimden artıp lota geri alınan iplik. */
export interface DokumaYarnTakeBack {
  id: number
  yarnLotId: number
  lotNo: string
  yarnCode: string
  yarnName: string
  takenOn: string
  kilograms: number
  notes: string | null
}

export type DokumaTakeBackSource = { warpOrderId: number } | { beamId: number }

const takeBackUrl = (source: DokumaTakeBackSource) =>
  'warpOrderId' in source ? `/dokuma-warp-orders/${source.warpOrderId}/take-backs` : `/dokuma-weaving/beams/${source.beamId}/take-backs`

const takeBackTags = ['DokumaYarnLot', 'DokumaWarp', 'DokumaWeaving', 'DokumaOrder'] as const

export const dokumaYarnLotsApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    addDokumaYarnTakeBack: build.mutation<null, { source: DokumaTakeBackSource; yarnLotId: number; kilograms: number; takenOn: string; notes: string | null }>({
      query: ({ source, ...body }) => ({ url: takeBackUrl(source), method: 'POST', body }),
      invalidatesTags: [...takeBackTags],
    }),
    removeDokumaYarnTakeBack: build.mutation<null, { source: DokumaTakeBackSource; id: number }>({
      query: ({ source, id }) => ({ url: `${takeBackUrl(source)}/${id}`, method: 'DELETE' }),
      invalidatesTags: [...takeBackTags],
    }),
    getDokumaYarnLots: build.query<PagedResult<DokumaYarnLot>, Record<string, unknown>>({
      query: (params) => ({ url: '/dokuma-yarn-lots', params }),
      providesTags: ['DokumaYarnLot'],
    }),
    saveDokumaYarnLot: build.mutation<DokumaYarnLot, { id?: number; body: DokumaYarnLotRequest }>({
      query: ({ id, body }) => ({ url: id === undefined ? '/dokuma-yarn-lots' : `/dokuma-yarn-lots/${id}`, method: id === undefined ? 'POST' : 'PUT', body }),
      invalidatesTags: ['DokumaYarnLot'],
    }),
    deleteDokumaYarnLot: build.mutation<null, number>({
      query: (id) => ({ url: `/dokuma-yarn-lots/${id}`, method: 'DELETE' }),
      invalidatesTags: ['DokumaYarnLot'],
    }),
  }),
})

export const {
  useAddDokumaYarnTakeBackMutation,
  useRemoveDokumaYarnTakeBackMutation,
  useGetDokumaYarnLotsQuery,
  useSaveDokumaYarnLotMutation,
  useDeleteDokumaYarnLotMutation,
} = dokumaYarnLotsApi
