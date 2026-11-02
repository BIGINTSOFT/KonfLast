import { baseApi } from '../../api/baseApi'
import type { BundleStatus } from '../bundles/bundlesApi'
import type { MachineActivity, WipOperation } from '../productionTracking/trackingApi'

export interface LiveSummary {
  machines: number
  runningMachines: number
  breakdownMachines: number
  operatorsPresent: number
  producing: number
  waitingForBundle: number
  onBreak: number
  produced: number
  rejected: number
  targetQuantity: number | null
  efficiencyPercent: number | null
  pendingReworkTasks: number
}

export interface LiveLinePlan {
  planId: number
  productionOrderId: number
  orderNo: string
  modelCode: string
  plannedQuantity: number
  /** Bu siparişten banttan çıkan (son operasyondan geçen) sağlam adet. */
  producedQuantity: number
  completionPercent: number
  plannedEnd: string
}

/** Adet ve süreler bugüne aittir; süreler dakikadır. Tüm değerleri sunucu hesaplar. */
export interface LiveLine {
  /** Banta atanmamış makineler grubunda null. */
  lineId: number | null
  lineCode: string
  name: string
  plan: LiveLinePlan | null
  produced: number
  rejected: number
  targetQuantity: number | null
  efficiencyPercent: number | null
  producedPerHour: number | null
  targetPerHour: number | null
  breakdownMinutes: number
  waitingMinutes: number
  breakMinutes: number
  machines: MachineActivity[]
}

export interface LiveDashboard {
  generatedAt: string
  dayStart: string
  summary: LiveSummary
  lines: LiveLine[]
  bundleStatuses: { status: BundleStatus; bundleCount: number; quantity: number }[]
  wip: WipOperation[]
}

// 'LiveDashboard' etiketi SignalR bildirimiyle geçersiz kılınır (bkz. features/live).
export const dashboardApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getLiveDashboard: build.query<LiveDashboard, void>({
      query: () => '/dashboard/live',
      providesTags: ['LiveDashboard'],
    }),
  }),
})

export const { useGetLiveDashboardQuery } = dashboardApi
