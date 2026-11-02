import { baseApi } from '../../api/baseApi'
import type { PagedResult } from '../../types/api'

export type YarnPackageType = 'BOBBIN' | 'HANK'

export const yarnPackageTypeLabels: Record<YarnPackageType, string> = { BOBBIN: 'Bobin', HANK: 'Çile' }

export type YarnLotStatus = 'APPROVED' | 'REJECTED'

export const yarnLotStatusPresentation: Record<YarnLotStatus, { label: string; tone: string }> = {
  APPROVED: { label: 'Uygun', tone: 'ok' },
  REJECTED: { label: 'Red', tone: 'danger' },
}

export interface YarnLot {
  id: number
  lotNo: string
  receiptDate: string
  customerId: number | null
  customerName: string | null
  yarnId: number
  yarnCode: string
  yarnName: string
  despatchNo: string | null
  customerLotNo: string | null
  packageType: YarnPackageType
  packageCount: number
  kilograms: number
  notes: string | null
  orderNo: string | null
  status: YarnLotStatus
  rejectionNote: string | null
  locationCode: string | null
}

export interface YarnLotRequest {
  locationId: number | null
  receiptDate: string
  customerId: number | null
  yarnId: number
  despatchNo: string | null
  customerLotNo: string | null
  packageType: YarnPackageType
  packageCount: number
  kilograms: number
  notes: string | null
}

export const boyahaneYarnLotsApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getYarnLots: build.query<PagedResult<YarnLot>, Record<string, unknown>>({
      query: (params) => ({ url: '/boyahane-yarn-lots', params }),
      providesTags: ['BoyahaneReceipt'],
    }),
    saveYarnLot: build.mutation<YarnLot, { id?: number; body: YarnLotRequest }>({
      query: ({ id, body }) => ({ url: id === undefined ? '/boyahane-yarn-lots' : `/boyahane-yarn-lots/${id}`, method: id === undefined ? 'POST' : 'PUT', body }),
      invalidatesTags: ['BoyahaneReceipt'],
    }),
    deleteYarnLot: build.mutation<null, number>({
      query: (id) => ({ url: `/boyahane-yarn-lots/${id}`, method: 'DELETE' }),
      invalidatesTags: ['BoyahaneReceipt'],
    }),
    rejectYarnLot: build.mutation<YarnLot, { id: number; note: string }>({
      query: ({ id, note }) => ({ url: `/boyahane-yarn-lots/${id}/reject`, method: 'POST', body: { note } }),
      invalidatesTags: ['BoyahaneReceipt', 'BoyahaneOrder', 'BoyahaneBatch'],
    }),
    approveYarnLot: build.mutation<YarnLot, number>({
      query: (id) => ({ url: `/boyahane-yarn-lots/${id}/approve`, method: 'POST' }),
      invalidatesTags: ['BoyahaneReceipt', 'BoyahaneOrder', 'BoyahaneBatch'],
    }),
  }),
})

export const {
  useGetYarnLotsQuery,
  useSaveYarnLotMutation,
  useDeleteYarnLotMutation,
  useRejectYarnLotMutation,
  useApproveYarnLotMutation,
} = boyahaneYarnLotsApi
