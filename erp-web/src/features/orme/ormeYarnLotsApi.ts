import { baseApi } from '../../api/baseApi'
import type { PagedResult } from '../../types/api'

export interface OrmeYarnLot {
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
  remainingKilograms: number
  notes: string | null
  /** Lot bir satın alma siparişinden teslim alındıysa sipariş; ipliği, sahibi ve tedarikçisi değiştirilemez. */
  purchaseOrderId: number | null
  purchaseOrderNo: string | null
}

export interface OrmeYarnLotRequest {
  receiptDate: string
  customerId: number | null
  supplierId: number | null
  yarnId: number
  despatchNo: string | null
  producerLotNo: string | null
  packageCount: number
  kilograms: number
  notes: string | null
}

export const ormeYarnLotsApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getOrmeYarnLots: build.query<PagedResult<OrmeYarnLot>, Record<string, unknown>>({
      query: (params) => ({ url: '/orme-yarn-lots', params }),
      providesTags: ['OrmeYarnLot'],
    }),
    saveOrmeYarnLot: build.mutation<OrmeYarnLot, { id?: number; body: OrmeYarnLotRequest }>({
      query: ({ id, body }) => ({ url: id === undefined ? '/orme-yarn-lots' : `/orme-yarn-lots/${id}`, method: id === undefined ? 'POST' : 'PUT', body }),
      invalidatesTags: ['OrmeYarnLot'],
    }),
    deleteOrmeYarnLot: build.mutation<null, number>({
      query: (id) => ({ url: `/orme-yarn-lots/${id}`, method: 'DELETE' }),
      invalidatesTags: ['OrmeYarnLot'],
    }),
  }),
})

export const { useGetOrmeYarnLotsQuery, useSaveOrmeYarnLotMutation, useDeleteOrmeYarnLotMutation } = ormeYarnLotsApi
