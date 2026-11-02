import { baseApi } from '../../api/baseApi'
import type { PagedResult } from '../../types/api'
import type { FabricRollStatus } from './boyahaneReceiptsApi'

export type CustomerDecisionChannel = 'EMAIL' | 'PHONE' | 'WRITTEN' | 'IN_PERSON'

export const customerDecisionChannelLabels: Record<CustomerDecisionChannel, string> = {
  EMAIL: 'E-posta',
  PHONE: 'Telefon',
  WRITTEN: 'Yazılı',
  IN_PERSON: 'Yüz yüze',
}

export interface FabricRollListItem {
  id: number
  rollNo: string
  customerRollNo: string | null
  lotNo: string
  receiptId: number
  receiptDate: string
  customerName: string | null
  fabricCode: string
  fabricName: string
  meters: number
  kilograms: number
  defectCount: number
  isConditionallyApproved: boolean
  status: FabricRollStatus
}

export interface FabricRollDefect {
  id: number
  defectTypeId: number
  defectCode: string
  defectName: string
  quantity: number
  atMeter: number | null
  note: string | null
}

export interface FabricRollDetail {
  id: number
  rollNo: string
  customerRollNo: string | null
  lotId: number
  lotNo: string
  receiptId: number
  customerName: string | null
  fabricCode: string
  fabricName: string
  rawWidthCm: number
  rawWeightGsm: number
  receivedMeters: number
  receivedKilograms: number
  inspectedMeters: number | null
  inspectedKilograms: number | null
  metersMeasured: boolean
  kilogramsMeasured: boolean
  inspectedWidthCm: number | null
  inspectedWeightGsm: number | null
  inspectedAt: string | null
  inspectedBy: string | null
  inspectionNote: string | null
  status: FabricRollStatus
  isConditionallyApproved: boolean
  customerDecisionAccepted: boolean | null
  customerDecisionBy: string | null
  customerDecisionChannel: CustomerDecisionChannel | null
  customerDecisionOn: string | null
  customerDecisionNote: string | null
  customerDecisionRecordedBy: string | null
  pendingRollsInLot: number
  canInspect: boolean
  defects: FabricRollDefect[]
}

export interface InspectionRequest {
  decision: FabricRollStatus
  inspectedMeters: number | null
  inspectedKilograms: number | null
  inspectedWidthCm: number | null
  inspectedWeightGsm: number | null
  note: string | null
  defects: { defectTypeId: number; quantity: number; atMeter: number | null; note: string | null }[]
}

export interface CustomerDecisionRequest {
  accepted: boolean
  decidedBy: string
  channel: CustomerDecisionChannel
  decidedOn: string
  note: string | null
}

const invalidates = ['BoyahaneInspection', 'BoyahaneReceipt'] as const

export const boyahaneInspectionApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getFabricRolls: build.query<PagedResult<FabricRollListItem>, Record<string, unknown>>({
      query: (params) => ({ url: '/boyahane-fabric-rolls', params }),
      providesTags: ['BoyahaneInspection'],
    }),
    getFabricRoll: build.query<FabricRollDetail, number>({
      query: (id) => `/boyahane-fabric-rolls/${id}`,
      providesTags: ['BoyahaneInspection'],
    }),
    findFabricRoll: build.mutation<FabricRollDetail, string>({
      query: (rollNo) => `/boyahane-fabric-rolls/by-no/${encodeURIComponent(rollNo)}`,
    }),
    inspectFabricRoll: build.mutation<FabricRollDetail, { id: number; body: InspectionRequest }>({
      query: ({ id, body }) => ({ url: `/boyahane-fabric-rolls/${id}/inspection`, method: 'POST', body }),
      invalidatesTags: [...invalidates],
    }),
    approveRemainingRolls: build.mutation<{ approvedCount: number }, number>({
      query: (lotId) => ({ url: '/boyahane-fabric-rolls/approve-remaining', method: 'POST', body: { lotId } }),
      invalidatesTags: [...invalidates],
    }),
    recordRollCustomerDecision: build.mutation<FabricRollDetail, { id: number; body: CustomerDecisionRequest }>({
      query: ({ id, body }) => ({ url: `/boyahane-fabric-rolls/${id}/customer-decision`, method: 'POST', body }),
      invalidatesTags: [...invalidates],
    }),
  }),
})

export const {
  useGetFabricRollsQuery,
  useGetFabricRollQuery,
  useFindFabricRollMutation,
  useInspectFabricRollMutation,
  useApproveRemainingRollsMutation,
  useRecordRollCustomerDecisionMutation,
} = boyahaneInspectionApi
