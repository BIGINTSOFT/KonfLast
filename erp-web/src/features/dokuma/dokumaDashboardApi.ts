import { baseApi } from '../../api/baseApi'
import type { DokumaStopCategory } from './dokumaFloorApi'

export interface DokumaDashboard {
  looms: number
  runningLooms: number
  stoppedLooms: number
  breakdownLooms: number
  idleLooms: number
  readyBeams: number
  wovenTodayMeters: number
  rollsToday: number
  pendingInspectionRolls: number
  packingListsToShip: number
  lateOrderCount: number
  stops: { loomCode: string; loomName: string; category: DokumaStopCategory; reasonName: string; since: string; beamNo: string | null }[]
  lateOrders: { orderId: number; orderNo: string; customerName: string | null; dueDate: string; orderedMeters: number; shippedMeters: number }[]
}

const dokumaDashboardApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getDokumaDashboard: build.query<DokumaDashboard, void>({
      query: () => '/dokuma-dashboard',
      providesTags: ['DokumaWeaving', 'DokumaOrder'],
    }),
  }),
})

export const { useGetDokumaDashboardQuery } = dokumaDashboardApi
