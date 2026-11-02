import { baseApi } from '../../api/baseApi'
import type { OrmeStopCategory } from './ormeFloorApi'

export interface OrmeDashboard {
  machines: number
  runningMachines: number
  stoppedMachines: number
  breakdownMachines: number
  idleMachines: number
  openKnitOrders: number
  knittedTodayKilograms: number
  rollsToday: number
  pendingInspectionRolls: number
  packingListsToShip: number
  lateOrderCount: number
  stops: { machineCode: string; machineName: string; category: OrmeStopCategory; reasonName: string; since: string; knitOrderNo: string | null }[]
  lateOrders: { orderId: number; orderNo: string; customerName: string | null; dueDate: string; orderedKilograms: number; shippedKilograms: number }[]
}

const ormeDashboardApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getOrmeDashboard: build.query<OrmeDashboard, void>({
      query: () => '/orme-dashboard',
      providesTags: ['OrmeKnitOrder', 'OrmeOrder'],
    }),
  }),
})

export const { useGetOrmeDashboardQuery } = ormeDashboardApi
