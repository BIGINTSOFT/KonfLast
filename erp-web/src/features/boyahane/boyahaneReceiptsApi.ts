import { baseApi } from '../../api/baseApi'
import type { PagedResult } from '../../types/api'

export type FabricRollStatus = 'PENDING_INSPECTION' | 'APPROVED' | 'CONDITIONAL' | 'REJECTED'

export const fabricRollStatusPresentation: Record<FabricRollStatus, { label: string; tone: string }> = {
  PENDING_INSPECTION: { label: 'Kontrol bekliyor', tone: 'warning' },
  APPROVED: { label: 'Uygun', tone: 'ok' },
  CONDITIONAL: { label: 'Şartlı', tone: 'info' },
  REJECTED: { label: 'Red', tone: 'danger' },
}

export interface FabricReceiptListItem {
  id: number
  receiptNo: string
  receiptDate: string
  /** Malın sahibi; boşsa mal işletmenindir. */
  customerName: string | null
  despatchNo: string | null
  rollCount: number
  totalMeters: number
  totalKilograms: number
  pendingRollCount: number
  createdAt: string
  createdBy: string | null
}

export interface FabricRoll {
  id: number
  rollNo: string
  customerRollNo: string | null
  meters: number
  kilograms: number
  metersMeasured: boolean
  kilogramsMeasured: boolean
  isConditionallyApproved: boolean
  status: FabricRollStatus
  /** Kontrol edilmiş ya da siparişe bağlanmış top düzenlenemez. */
  isLocked: boolean
  locationCode: string | null
}

export interface FabricLot {
  id: number
  lotNo: string
  fabricId: number
  fabricCode: string
  fabricName: string
  customerFabricCode: string | null
  declaredRollCount: number | null
  declaredMeters: number | null
  declaredKilograms: number | null
  inspectionRequired: boolean
  rolls: FabricRoll[]
}

export interface FabricReceipt {
  id: number
  receiptNo: string
  receiptDate: string
  customerId: number | null
  customerName: string | null
  despatchNo: string | null
  notes: string | null
  canDelete: boolean
  createdAt: string
  createdBy: string | null
  lots: FabricLot[]
  isOpening: boolean
}

export interface FabricReceiptRequest {
  receiptDate: string
  customerId: number | null
  despatchNo: string | null
  notes: string | null
  locationId: number | null
  isOpening: boolean
  lots: {
    id: number | null
    fabricId: number
    customerFabricCode: string | null
    declaredRollCount: number | null
    declaredMeters: number | null
    declaredKilograms: number | null
    rolls: { id: number | null; customerRollNo: string | null; meters: number | null; kilograms: number | null }[]
  }[]
}

export const boyahaneReceiptsApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getFabricReceipts: build.query<PagedResult<FabricReceiptListItem>, Record<string, unknown>>({
      query: (params) => ({ url: '/boyahane-fabric-receipts', params }),
      providesTags: ['BoyahaneReceipt'],
    }),
    getFabricReceipt: build.query<FabricReceipt, number>({
      query: (id) => `/boyahane-fabric-receipts/${id}`,
      providesTags: ['BoyahaneReceipt'],
    }),
    saveFabricReceipt: build.mutation<FabricReceipt, { id?: number; body: FabricReceiptRequest }>({
      query: ({ id, body }) => ({ url: id === undefined ? '/boyahane-fabric-receipts' : `/boyahane-fabric-receipts/${id}`, method: id === undefined ? 'POST' : 'PUT', body }),
      invalidatesTags: ['BoyahaneReceipt'],
    }),
    deleteFabricReceipt: build.mutation<null, number>({
      query: (id) => ({ url: `/boyahane-fabric-receipts/${id}`, method: 'DELETE' }),
      invalidatesTags: ['BoyahaneReceipt'],
    }),
  }),
})

export const {
  useGetFabricReceiptsQuery,
  useGetFabricReceiptQuery,
  useSaveFabricReceiptMutation,
  useDeleteFabricReceiptMutation,
} = boyahaneReceiptsApi
