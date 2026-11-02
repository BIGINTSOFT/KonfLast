import { baseApi } from '../../api/baseApi'
import type { PagedResult } from '../../types/api'
import type { CustomerDecisionChannel } from './boyahaneInspectionApi'

export type LabRequestStatus = 'OPEN' | 'APPROVED' | 'CANCELLED'
export type LabTrialDecision = 'PENDING' | 'ACCEPTED' | 'REJECTED'
export type DyeChemicalKind = 'DYE' | 'CHEMICAL'

export const labRequestStatusPresentation: Record<LabRequestStatus, { label: string; tone: string }> = {
  OPEN: { label: 'Açık', tone: 'warning' },
  APPROVED: { label: 'Onaylı', tone: 'ok' },
  CANCELLED: { label: 'İptal', tone: 'danger' },
}

export const labTrialDecisionPresentation: Record<LabTrialDecision, { label: string; tone: string }> = {
  PENDING: { label: 'Karar bekliyor', tone: 'warning' },
  ACCEPTED: { label: 'Reçete', tone: 'ok' },
  REJECTED: { label: 'Kabul edilmedi', tone: 'danger' },
}

/** Reçetede boya kumaş ağırlığına göre %, kimyasal g/L ile yazılır. */
export const dyeChemicalUnit: Record<DyeChemicalKind, string> = { DYE: '%', CHEMICAL: 'g/L' }

export interface LabRequestListItem {
  id: number
  requestNo: string
  orderNo: string
  orderId: number
  customerName: string | null
  itemCode: string
  colorName: string | null
  colorCode: string | null
  trialCount: number
  approvedTrialCode: string | null
  status: LabRequestStatus
  createdAt: string
}

export interface LabTrialItem {
  id: number
  dyeChemicalId: number
  code: string
  name: string
  kind: DyeChemicalKind
  amount: number
}

export interface LabTrial {
  id: number
  trialCode: string
  notes: string | null
  decision: LabTrialDecision
  decisionBy: string | null
  decisionChannel: CustomerDecisionChannel | null
  decisionOn: string | null
  decisionNote: string | null
  decisionRecordedBy: string | null
  reusedFromRequestNo: string | null
  items: LabTrialItem[]
}

export interface LabRequest {
  id: number
  requestNo: string
  orderId: number
  orderNo: string
  orderLineId: number
  customerName: string | null
  itemCode: string
  itemName: string
  colorName: string | null
  colorCode: string | null
  targetNote: string | null
  status: LabRequestStatus
  trials: LabTrial[]
  /** Sipariş satırında seçilen renk kartı; onaylanan reçete bu karta bağlanır. */
  colorCardId: number | null
  colorCardCode: string | null
}

export interface PreviousRecipe {
  trialId: number
  requestNo: string
  trialCode: string
  orderNo: string
  decisionOn: string | null
  items: LabTrialItem[]
}

export interface LabTrialRequest {
  notes: string | null
  items: { dyeChemicalId: number; amount: number }[]
}

export interface LabDecisionRequest {
  accepted: boolean
  decidedBy: string
  channel: CustomerDecisionChannel
  decidedOn: string
  note: string | null
}

const tags = ['BoyahaneLab', 'BoyahaneOrder'] as const

export const boyahaneLabApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getLabRequests: build.query<PagedResult<LabRequestListItem>, Record<string, unknown>>({
      query: (params) => ({ url: '/boyahane-lab-requests', params }),
      providesTags: ['BoyahaneLab'],
    }),
    getLabRequest: build.query<LabRequest, number>({
      query: (id) => `/boyahane-lab-requests/${id}`,
      providesTags: ['BoyahaneLab'],
    }),
    createLabRequest: build.mutation<LabRequest, { orderLineId: number; targetNote: string | null }>({
      query: (body) => ({ url: '/boyahane-lab-requests', method: 'POST', body }),
      invalidatesTags: [...tags],
    }),
    cancelLabRequest: build.mutation<LabRequest, number>({
      query: (id) => ({ url: `/boyahane-lab-requests/${id}/cancel`, method: 'POST' }),
      invalidatesTags: [...tags],
    }),
    reopenLabRequest: build.mutation<LabRequest, number>({
      query: (id) => ({ url: `/boyahane-lab-requests/${id}/reopen`, method: 'POST' }),
      invalidatesTags: [...tags],
    }),
    saveLabTrial: build.mutation<LabRequest, { id: number; trialId?: number; body: LabTrialRequest }>({
      query: ({ id, trialId, body }) => ({
        url: trialId === undefined ? `/boyahane-lab-requests/${id}/trials` : `/boyahane-lab-requests/${id}/trials/${trialId}`,
        method: trialId === undefined ? 'POST' : 'PUT',
        body,
      }),
      invalidatesTags: [...tags],
    }),
    deleteLabTrial: build.mutation<LabRequest, { id: number; trialId: number }>({
      query: ({ id, trialId }) => ({ url: `/boyahane-lab-requests/${id}/trials/${trialId}`, method: 'DELETE' }),
      invalidatesTags: [...tags],
    }),
    decideLabTrial: build.mutation<LabRequest, { id: number; trialId: number; body: LabDecisionRequest }>({
      query: ({ id, trialId, body }) => ({ url: `/boyahane-lab-requests/${id}/trials/${trialId}/decision`, method: 'POST', body }),
      invalidatesTags: [...tags],
    }),
    getPreviousRecipes: build.query<PreviousRecipe[], number>({
      query: (orderLineId) => ({ url: '/boyahane-lab-requests/previous-recipes', params: { orderLineId } }),
      providesTags: ['BoyahaneLab'],
    }),
    reuseLabRecipe: build.mutation<LabRequest, { orderLineId: number; trialId: number }>({
      query: (body) => ({ url: '/boyahane-lab-requests/reuse', method: 'POST', body }),
      invalidatesTags: [...tags],
    }),
  }),
})

export const {
  useGetLabRequestsQuery,
  useGetLabRequestQuery,
  useCreateLabRequestMutation,
  useCancelLabRequestMutation,
  useReopenLabRequestMutation,
  useSaveLabTrialMutation,
  useDeleteLabTrialMutation,
  useDecideLabTrialMutation,
  useGetPreviousRecipesQuery,
  useReuseLabRecipeMutation,
} = boyahaneLabApi
