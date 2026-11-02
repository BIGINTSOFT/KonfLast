import { baseApi } from '../../api/baseApi'

export interface BalanceOperation {
  routeOperationId: number
  sequenceNo: number
  operationId: number
  operationCode: string
  operationName: string
  machineTypeName: string | null
  sam: number
  /** Hedef için gereken iş gücü (operatör cinsinden, kesirli). */
  workload: number
  requiredStations: number
  loadPercent: number | null
  availableMachines: number
  unavailableMachines: number
  spareMachines: number
  /** Eksik makine (artı) ya da fazla makine (eksi). */
  machineGap: number
  availableCapacityPerHour: number | null
  qualifiedOperators: number
  suggestedOperators: { id: number; code: string; name: string }[]
  operatorGap: number
  isBottleneck: boolean
  suggestions: string[]
}

/** Tüm değerleri ve önerileri sunucu hesaplar. */
export interface LineBalance {
  modelId: number
  modelCode: string
  modelName: string
  productionRouteId: number
  routeVersion: number
  productionLineId: number | null
  lineCode: string | null
  lineName: string | null
  targetPerHour: number
  efficiencyPercent: number
  taktMinutes: number
  totalSam: number
  theoreticalOperators: number
  requiredStations: number
  balanceEfficiencyPercent: number | null
  achievablePerHour: number | null
  targetReachable: boolean
  bottleneckOperationName: string | null
  unfilledStations: number
  operations: BalanceOperation[]
}

export interface LineBalanceParams {
  modelId: number
  productionLineId?: number
  targetPerHour: number
  efficiencyPercent: number
}

export const lineBalancingApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getLineBalance: build.query<LineBalance, LineBalanceParams>({
      query: (params) => ({ url: '/line-balancing', params }),
      // Hesap makine ve operatörlerin o anki durumuna dayanır; her çalıştırmada yeniden yapılır.
      keepUnusedDataFor: 0,
    }),
  }),
})

export const { useLazyGetLineBalanceQuery } = lineBalancingApi
