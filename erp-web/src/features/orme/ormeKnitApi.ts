import { baseApi } from '../../api/baseApi'
import type { PagedResult } from '../../types/api'
import type { DocumentLanguage } from '../../utils/documentText'
import type { OrmeStopCategory } from './ormeFloorApi'
import type { OrmeOrderOwnership } from './ormeOrdersApi'

export type OrmeKnitOrderStatus = 'OPEN' | 'COMPLETED' | 'CANCELLED'
export type OrmeFabricRollStatus = 'PENDING_INSPECTION' | 'FIRST_QUALITY' | 'SECOND_QUALITY' | 'REJECTED'

export const ormeKnitOrderStatusPresentation: Record<OrmeKnitOrderStatus, { label: string; tone: string }> = {
  OPEN: { label: 'Açık', tone: 'info' },
  COMPLETED: { label: 'Bitti', tone: 'ok' },
  CANCELLED: { label: 'İptal', tone: 'danger' },
}

export const ormeRollStatusPresentation: Record<OrmeFabricRollStatus, { label: string; tone: string }> = {
  PENDING_INSPECTION: { label: 'Kontrol bekliyor', tone: 'warning' },
  FIRST_QUALITY: { label: '1. kalite', tone: 'ok' },
  SECOND_QUALITY: { label: '2. kalite', tone: 'info' },
  REJECTED: { label: 'Red', tone: 'danger' },
}

export interface OrmeKnitOrderListItem {
  id: number
  knitOrderNo: string
  plannedDate: string
  orderId: number
  orderNo: string
  customerName: string | null
  fabricCode: string
  fabricName: string
  bedType: string
  plannedKilograms: number
  knittedKilograms: number
  rollCount: number
  runningMachineCount: number
  issuedKilograms: number
  status: OrmeKnitOrderStatus
}

export interface OrmeKnitOrderYarn {
  yarnId: number
  yarnCode: string
  yarnName: string
  yarnColor: string | null
  percent: number
  /** Planlanan kilonun fireli ihtiyacı. */
  requiredKilograms: number
  /** Verilen − geri alınan. */
  issuedKilograms: number
}

export interface OrmeKnitOrderMachine {
  id: number
  machineId: number
  machineCode: string
  machineName: string
  diameterInch: number
  gauge: number
  assignedOn: string
  /** Boşsa emir bu makinede çalışıyor. */
  releasedOn: string | null
  /** Çap ya da fine kumaş kartıyla uyuşmuyorsa uyarı; engel değildir. */
  fitWarning: string | null
  knittedKilograms: number
  rollCount: number
}

export interface OrmeYarnIssue {
  id: number
  yarnLotId: number
  lotNo: string
  producerLotNo: string | null
  yarnCode: string
  yarnName: string
  issuedOn: string
  kilograms: number
}

export interface OrmeYarnTakeBack {
  id: number
  yarnLotId: number
  lotNo: string
  yarnCode: string
  yarnName: string
  takenOn: string
  kilograms: number
  notes: string | null
}

export interface OrmeKnitOrderLot {
  yarnLotId: number
  lotNo: string
  producerLotNo: string | null
  yarnCode: string
  yarnName: string
  issuedKilograms: number
  takenBackKilograms: number
  netKilograms: number
}

export interface OrmeFabricRoll {
  id: number
  rollNo: string
  knitOrderId: number
  knitOrderNo: string
  machineId: number
  machineCode: string
  orderId: number
  orderNo: string
  customerName: string | null
  fabricCode: string
  fabricName: string
  cutOn: string
  kilograms: number
  /** Girilmişse ölçülen metre. */
  meters: number | null
  /** Metre girilmediyse gramaj ve enden tahmin. */
  estimatedMeters: number | null
  status: OrmeFabricRollStatus
  notes: string | null
  /** Topu tabletten kaydeden personel; ofiste girilende boş. */
  cutByName: string | null
  inspectedOn: string | null
  /** Kontrolde puana esas metre. */
  inspectedMeters: number | null
  /** Kontrolde metre ölçülmedi, tahmin kullanıldı. */
  inspectedMetersEstimated: boolean
  inspectedWidthCm: number | null
  inspectedWeightGsm: number | null
  pointsPer100M2: number | null
  /** En ve gramajın karttan sapması; uyarıdır, kaliteyi değiştirmez. */
  specDeviation: string | null
  /** Dolu ise kalite yetkili kararıyla değişti. */
  overrideNote: string | null
}

export interface OrmeKnitOrder {
  id: number
  knitOrderNo: string
  orderId: number
  orderNo: string
  orderLineId: number
  customerName: string | null
  ownership: OrmeOrderOwnership
  orderLineKilograms: number
  fabricCode: string
  fabricName: string
  bedType: string
  diameterInch: number | null
  gauge: number | null
  plannedDate: string
  plannedKilograms: number
  knittedKilograms: number
  remainingKilograms: number
  completedOn: string | null
  notes: string | null
  status: OrmeKnitOrderStatus
  yarns: OrmeKnitOrderYarn[]
  machines: OrmeKnitOrderMachine[]
  issues: OrmeYarnIssue[]
  takeBacks: OrmeYarnTakeBack[]
  lots: OrmeKnitOrderLot[]
  /** Aynı ipliğin birden çok üretici lotundan verildiği iplikler. */
  mixedProducerLotYarns: string[]
  rolls: OrmeFabricRoll[]
  /** Müşterili siparişte müşterinin sipariş no'su; stoğa üretimde boş. */
  customerOrderNo: string | null
  /** Müşterinin bu kumaşa verdiği kendi kodu. */
  customerFabricCode: string | null
  /** Etiket dili: siparişin müşterisinden, stoğa üretimde Türkçe. */
  documentLanguage: DocumentLanguage
}

export interface OrmeMachineOption {
  id: number
  code: string
  name: string
  diameterInch: number
  gauge: number
  /** Makinede çalışan başka emir; doluysa atanamaz. */
  busyKnitOrderNo: string | null
  fitWarning: string | null
}

export interface OrmeIssuableLot {
  id: number
  lotNo: string
  receiptDate: string
  yarnId: number
  yarnCode: string
  yarnName: string
  producerLotNo: string | null
  remainingKilograms: number
}

export interface OrmeMachineBoardItem {
  machineId: number
  machineCode: string
  machineName: string
  bedType: string
  diameterInch: number
  gauge: number
  knitOrderId: number | null
  knitOrderNo: string | null
  orderNo: string | null
  customerName: string | null
  fabricCode: string | null
  fabricName: string | null
  assignedOn: string | null
  plannedKilograms: number | null
  /** Emrin tüm makinelerde örülen toplamı. */
  knittedKilograms: number | null
  /** Emrin bu makinede örülen kısmı. */
  machineKilograms: number | null
  remainingKilograms: number | null
  rollCount: number
  fitWarning: string | null
  /** Makine duruyorsa duruşun türü. */
  openStopCategory: OrmeStopCategory | null
  openStopReason: string | null
  openStopSince: string | null
}

const changed = ['OrmeKnitOrder', 'OrmeOrder', 'OrmeYarnLot'] as const

export const ormeKnitApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getOrmeKnitOrders: build.query<PagedResult<OrmeKnitOrderListItem>, Record<string, unknown>>({
      query: (params) => ({ url: '/orme-knit-orders', params }),
      providesTags: ['OrmeKnitOrder'],
    }),
    getOrmeKnitOrder: build.query<OrmeKnitOrder, number>({
      query: (id) => `/orme-knit-orders/${id}`,
      providesTags: ['OrmeKnitOrder'],
    }),
    createOrmeKnitOrder: build.mutation<OrmeKnitOrder, { orderLineId: number; plannedDate: string; plannedKilograms: number; notes: string | null }>({
      query: (body) => ({ url: '/orme-knit-orders', method: 'POST', body }),
      invalidatesTags: changed,
    }),
    updateOrmeKnitOrder: build.mutation<OrmeKnitOrder, { id: number; plannedDate: string; plannedKilograms: number; notes: string | null }>({
      query: ({ id, ...body }) => ({ url: `/orme-knit-orders/${id}`, method: 'PUT', body }),
      invalidatesTags: changed,
    }),
    completeOrmeKnitOrder: build.mutation<OrmeKnitOrder, number>({
      query: (id) => ({ url: `/orme-knit-orders/${id}/complete`, method: 'POST' }),
      invalidatesTags: changed,
    }),
    reopenOrmeKnitOrder: build.mutation<OrmeKnitOrder, number>({
      query: (id) => ({ url: `/orme-knit-orders/${id}/reopen`, method: 'POST' }),
      invalidatesTags: changed,
    }),
    cancelOrmeKnitOrder: build.mutation<OrmeKnitOrder, number>({
      query: (id) => ({ url: `/orme-knit-orders/${id}/cancel`, method: 'POST' }),
      invalidatesTags: changed,
    }),
    getOrmeMachineOptions: build.query<OrmeMachineOption[], number>({
      query: (id) => `/orme-knit-orders/${id}/machine-options`,
      providesTags: ['OrmeKnitOrder'],
    }),
    assignOrmeMachine: build.mutation<OrmeKnitOrder, { id: number; machineId: number; assignedOn: string }>({
      query: ({ id, ...body }) => ({ url: `/orme-knit-orders/${id}/machines`, method: 'POST', body }),
      invalidatesTags: changed,
    }),
    releaseOrmeMachine: build.mutation<OrmeKnitOrder, { id: number; assignmentId: number }>({
      query: ({ id, assignmentId }) => ({ url: `/orme-knit-orders/${id}/machines/${assignmentId}`, method: 'DELETE' }),
      invalidatesTags: changed,
    }),
    getOrmeIssuableLots: build.query<OrmeIssuableLot[], number>({
      query: (id) => `/orme-knit-orders/${id}/issuable-lots`,
      providesTags: ['OrmeKnitOrder', 'OrmeYarnLot'],
    }),
    addOrmeYarnIssue: build.mutation<OrmeKnitOrder, { id: number; yarnLotId: number; kilograms: number; issuedOn: string }>({
      query: ({ id, ...body }) => ({ url: `/orme-knit-orders/${id}/issues`, method: 'POST', body }),
      invalidatesTags: changed,
    }),
    removeOrmeYarnIssue: build.mutation<OrmeKnitOrder, { id: number; issueId: number }>({
      query: ({ id, issueId }) => ({ url: `/orme-knit-orders/${id}/issues/${issueId}`, method: 'DELETE' }),
      invalidatesTags: changed,
    }),
    addOrmeYarnTakeBack: build.mutation<OrmeKnitOrder, { id: number; yarnLotId: number; kilograms: number; takenOn: string; notes: string | null }>({
      query: ({ id, ...body }) => ({ url: `/orme-knit-orders/${id}/take-backs`, method: 'POST', body }),
      invalidatesTags: changed,
    }),
    removeOrmeYarnTakeBack: build.mutation<OrmeKnitOrder, { id: number; takeBackId: number }>({
      query: ({ id, takeBackId }) => ({ url: `/orme-knit-orders/${id}/take-backs/${takeBackId}`, method: 'DELETE' }),
      invalidatesTags: changed,
    }),
    addOrmeRoll: build.mutation<OrmeKnitOrder, { id: number; machineId: number; cutOn: string; kilograms: number; meters: number | null; notes: string | null }>({
      query: ({ id, ...body }) => ({ url: `/orme-knit-orders/${id}/rolls`, method: 'POST', body }),
      invalidatesTags: changed,
    }),
    removeOrmeRoll: build.mutation<OrmeKnitOrder, { id: number; rollId: number }>({
      query: ({ id, rollId }) => ({ url: `/orme-knit-orders/${id}/rolls/${rollId}`, method: 'DELETE' }),
      invalidatesTags: changed,
    }),
    getOrmeRolls: build.query<PagedResult<OrmeFabricRoll>, Record<string, unknown>>({
      query: (params) => ({ url: '/orme-rolls', params }),
      providesTags: ['OrmeKnitOrder'],
    }),
    getOrmeMachineBoard: build.query<OrmeMachineBoardItem[], void>({
      query: () => '/orme-machine-board',
      providesTags: ['OrmeKnitOrder'],
    }),
  }),
})

export const {
  useGetOrmeKnitOrdersQuery,
  useGetOrmeKnitOrderQuery,
  useCreateOrmeKnitOrderMutation,
  useUpdateOrmeKnitOrderMutation,
  useCompleteOrmeKnitOrderMutation,
  useReopenOrmeKnitOrderMutation,
  useCancelOrmeKnitOrderMutation,
  useGetOrmeMachineOptionsQuery,
  useAssignOrmeMachineMutation,
  useReleaseOrmeMachineMutation,
  useGetOrmeIssuableLotsQuery,
  useAddOrmeYarnIssueMutation,
  useRemoveOrmeYarnIssueMutation,
  useAddOrmeYarnTakeBackMutation,
  useRemoveOrmeYarnTakeBackMutation,
  useAddOrmeRollMutation,
  useRemoveOrmeRollMutation,
  useGetOrmeRollsQuery,
  useGetOrmeMachineBoardQuery,
} = ormeKnitApi
