import { baseApi } from '../../api/baseApi'

export type LinePlanStatus = 'PLANNED' | 'COMPLETED' | 'CANCELLED'

export interface LineMachine {
  machineId: number
  position: number
  machineCode: string
  machineName: string
  machineTypeName: string
  operationName: string | null
  status: string
  isActive: boolean
  assignedAt: string
}

export interface LinePlan {
  id: number
  productionLineId: number
  lineCode: string
  lineName: string
  productionOrderId: number
  orderNo: string
  customerName: string
  productCode: string
  productName: string
  modelCode: string
  plannedStart: string
  plannedEnd: string
  plannedQuantity: number
  status: LinePlanStatus
  isEditable: boolean
  /** Bant şu an bu planı mı üretiyor (sunucu saatiyle). */
  isCurrent: boolean
  notes: string | null
  rowVersion: string
}

export interface LineOverview {
  id: number
  lineCode: string
  name: string
  productionAreaName: string
  factoryName: string
  isActive: boolean
  machines: LineMachine[]
  currentPlan: LinePlan | null
  upcomingPlans: LinePlan[]
}

export interface LinePlanRequest {
  productionLineId: number
  productionOrderId: number
  plannedStart: string
  plannedEnd: string
  plannedQuantity: number
  notes: string | null
  rowVersion?: string
}

// Makine ataması, makine listesindeki "Bant" sütununu ve bant listesindeki makine sayısını da değiştirir.
const lineTags = ['ProductionLine', 'MasterData'] as const

export const productionLinesApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getLineOverview: build.query<LineOverview, number>({
      query: (lineId) => `/production-lines/${lineId}/overview`,
      providesTags: ['ProductionLine'],
    }),
    assignMachine: build.mutation<LineOverview, { lineId: number; machineId: number; transfer?: boolean }>({
      query: ({ lineId, ...body }) => ({ url: `/production-lines/${lineId}/machines`, method: 'POST', body }),
      invalidatesTags: [...lineTags],
    }),
    unassignMachine: build.mutation<LineOverview, { lineId: number; machineId: number }>({
      query: ({ lineId, machineId }) => ({ url: `/production-lines/${lineId}/machines/${machineId}`, method: 'DELETE' }),
      invalidatesTags: [...lineTags],
    }),
    reorderMachines: build.mutation<LineOverview, { lineId: number; machineIds: number[] }>({
      query: ({ lineId, machineIds }) => ({
        url: `/production-lines/${lineId}/machines/order`,
        method: 'PUT',
        body: { machineIds },
      }),
      invalidatesTags: ['ProductionLine'],
    }),
    saveLinePlan: build.mutation<LinePlan, { id?: number; body: LinePlanRequest }>({
      query: ({ id, body }) => ({
        url: id === undefined ? '/line-plans' : `/line-plans/${id}`,
        method: id === undefined ? 'POST' : 'PUT',
        body,
      }),
      invalidatesTags: ['ProductionLine'],
    }),
    cancelLinePlan: build.mutation<LinePlan, { id: number; rowVersion: string }>({
      query: ({ id, rowVersion }) => ({ url: `/line-plans/${id}/cancel`, method: 'POST', body: { rowVersion } }),
      invalidatesTags: ['ProductionLine'],
    }),
  }),
})

export const {
  useGetLineOverviewQuery,
  useAssignMachineMutation,
  useUnassignMachineMutation,
  useReorderMachinesMutation,
  useSaveLinePlanMutation,
  useCancelLinePlanMutation,
} = productionLinesApi
