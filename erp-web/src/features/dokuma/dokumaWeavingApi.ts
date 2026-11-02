import { baseApi } from '../../api/baseApi'
import type { PagedResult } from '../../types/api'
import type { DocumentLanguage } from '../../utils/documentText'
import type { DokumaOrderOwnership } from './dokumaOrdersApi'
import type { DokumaStopCategory } from './dokumaFloorApi'
import type { DokumaBeam, DokumaIssuableLot } from './dokumaWarpApi'
import type { DokumaYarnTakeBack } from './dokumaYarnLotsApi'

export type DokumaFabricRollStatus = 'PENDING_INSPECTION' | 'FIRST_QUALITY' | 'SECOND_QUALITY' | 'REJECTED'

export const dokumaRollStatusPresentation: Record<DokumaFabricRollStatus, { label: string; tone: string }> = {
  PENDING_INSPECTION: { label: 'Kontrol bekliyor', tone: 'warning' },
  FIRST_QUALITY: { label: '1. kalite', tone: 'ok' },
  SECOND_QUALITY: { label: '2. kalite', tone: 'info' },
  REJECTED: { label: 'Red', tone: 'danger' },
}

export interface DokumaLoomBoardItem {
  loomId: number
  loomCode: string
  loomName: string
  loomType: string
  sheddingType: string
  maxReedWidthCm: number | null
  beamId: number | null
  beamNo: string | null
  orderNo: string | null
  customerName: string | null
  articleCode: string | null
  articleName: string | null
  mountedOn: string | null
  beamMeters: number | null
  /** Levent metresi / (1 + çözgü çekmesi). */
  expectedFabricMeters: number | null
  wovenMeters: number | null
  rollCount: number
  /** Tezgâh duruyorsa duruşun türü. */
  openStopCategory: DokumaStopCategory | null
  openStopReason: string | null
  openStopSince: string | null
}

export interface DokumaFabricRoll {
  /** Topu tabletten kesen personel. */
  cutByName: string | null
  id: number
  rollNo: string
  beamId: number
  beamNo: string
  loomCode: string
  orderNo: string
  customerName: string | null
  articleCode: string
  articleName: string
  cutOn: string
  meters: number
  kilograms: number | null
  status: DokumaFabricRollStatus
  notes: string | null
  inspectedOn: string | null
  inspectedMeters: number | null
  inspectedWidthCm: number | null
  pointsPer100M2: number | null
  /** Doluysa kalite yetkili kararıyla değiştirilmiştir. */
  overrideNote: string | null
  /** Müşteri siparişinde müşterinin sipariş numarası (etikete basılır). */
  customerOrderNo: string | null
  /** Müşterinin artikel için kullandığı kendi kodu (etikete basılır). */
  customerArticleCode: string | null
  /** Etiketin dili: müşterinin belge dili. */
  documentLanguage: DocumentLanguage
}

export interface DokumaBeamWeaving {
  beam: DokumaBeam
  loomId: number | null
  loomCode: string | null
  loomName: string | null
  mountedOn: string | null
  finishedOn: string | null
  leftoverMeters: number | null
  ownership: DokumaOrderOwnership
  expectedFabricMeters: number
  wovenMeters: number
  /** Beklenen kumaş için atkı iplikleri (fire dahil) ve verilen. */
  weftYarns: { yarnId: number; yarnCode: string; yarnName: string; yarnColor: string | null; requiredKilograms: number; issuedKilograms: number }[]
  weftIssues: { id: number; yarnLotId: number; lotNo: string; producerLotNo: string | null; yarnCode: string; yarnName: string; issuedOn: string; kilograms: number }[]
  rolls: DokumaFabricRoll[]
  takeBacks: DokumaYarnTakeBack[]
}

const weavingTags = ['DokumaWeaving', 'DokumaWarp', 'DokumaOrder', 'DokumaYarnLot'] as const

export const dokumaWeavingApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getDokumaLoomBoard: build.query<DokumaLoomBoardItem[], void>({
      query: () => '/dokuma-weaving/board',
      providesTags: ['DokumaWeaving'],
    }),
    getDokumaBeamWeaving: build.query<DokumaBeamWeaving, number>({
      query: (id) => `/dokuma-weaving/beams/${id}`,
      providesTags: ['DokumaWeaving'],
    }),
    getDokumaIssuableWeftLots: build.query<DokumaIssuableLot[], number>({
      query: (id) => `/dokuma-weaving/beams/${id}/issuable-weft-lots`,
      providesTags: ['DokumaWeaving', 'DokumaYarnLot'],
    }),
    mountDokumaBeam: build.mutation<DokumaBeamWeaving, { id: number; loomId: number; date: string }>({
      query: ({ id, ...body }) => ({ url: `/dokuma-weaving/beams/${id}/mount`, method: 'POST', body }),
      invalidatesTags: [...weavingTags],
    }),
    unmountDokumaBeam: build.mutation<DokumaBeamWeaving, number>({
      query: (id) => ({ url: `/dokuma-weaving/beams/${id}/unmount`, method: 'POST' }),
      invalidatesTags: [...weavingTags],
    }),
    finishDokumaBeam: build.mutation<DokumaBeamWeaving, { id: number; date: string; leftoverMeters: number }>({
      query: ({ id, ...body }) => ({ url: `/dokuma-weaving/beams/${id}/finish`, method: 'POST', body }),
      invalidatesTags: [...weavingTags],
    }),
    addDokumaWeftIssue: build.mutation<DokumaBeamWeaving, { id: number; yarnLotId: number; kilograms: number; issuedOn: string }>({
      query: ({ id, ...body }) => ({ url: `/dokuma-weaving/beams/${id}/weft-issues`, method: 'POST', body }),
      invalidatesTags: [...weavingTags],
    }),
    removeDokumaWeftIssue: build.mutation<DokumaBeamWeaving, { id: number; issueId: number }>({
      query: ({ id, issueId }) => ({ url: `/dokuma-weaving/beams/${id}/weft-issues/${issueId}`, method: 'DELETE' }),
      invalidatesTags: [...weavingTags],
    }),
    addDokumaRoll: build.mutation<DokumaBeamWeaving, { id: number; cutOn: string; meters: number; kilograms: number | null; notes: string | null }>({
      query: ({ id, ...body }) => ({ url: `/dokuma-weaving/beams/${id}/rolls`, method: 'POST', body }),
      invalidatesTags: [...weavingTags],
    }),
    removeDokumaRoll: build.mutation<DokumaBeamWeaving, { id: number; rollId: number }>({
      query: ({ id, rollId }) => ({ url: `/dokuma-weaving/beams/${id}/rolls/${rollId}`, method: 'DELETE' }),
      invalidatesTags: [...weavingTags],
    }),
    getDokumaRolls: build.query<PagedResult<DokumaFabricRoll>, Record<string, unknown>>({
      query: (params) => ({ url: '/dokuma-weaving/rolls', params }),
      providesTags: ['DokumaWeaving'],
    }),
  }),
})

export const {
  useGetDokumaLoomBoardQuery,
  useGetDokumaBeamWeavingQuery,
  useGetDokumaIssuableWeftLotsQuery,
  useMountDokumaBeamMutation,
  useUnmountDokumaBeamMutation,
  useFinishDokumaBeamMutation,
  useAddDokumaWeftIssueMutation,
  useRemoveDokumaWeftIssueMutation,
  useAddDokumaRollMutation,
  useRemoveDokumaRollMutation,
  useGetDokumaRollsQuery,
} = dokumaWeavingApi
