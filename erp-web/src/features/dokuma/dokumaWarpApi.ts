import { baseApi } from '../../api/baseApi'
import type { PagedResult } from '../../types/api'
import { formatDate } from '../../utils/format'
import type { DokumaOrderOwnership } from './dokumaOrdersApi'
import type { DokumaYarnTakeBack } from './dokumaYarnLotsApi'

export type WarpOrderStatus = 'OPEN' | 'COMPLETED' | 'CANCELLED'
export type SizingMode = 'NONE' | 'IN_HOUSE' | 'COMMISSION'
export type BeamStatus = 'WARPED' | 'AT_SIZING' | 'READY' | 'ON_LOOM' | 'FINISHED'
export type DrawingMethod = 'DRAWING' | 'TYING'

export const warpOrderStatusPresentation: Record<WarpOrderStatus, { label: string; tone: string }> = {
  OPEN: { label: 'Sürüyor', tone: 'info' },
  COMPLETED: { label: 'Bitti', tone: 'ok' },
  CANCELLED: { label: 'İptal', tone: 'danger' },
}

export const sizingModeLabels: Record<SizingMode, string> = {
  NONE: 'Haşılsız',
  IN_HOUSE: 'İçeride haşıl',
  COMMISSION: 'Fason haşıl',
}

export const beamStatusPresentation: Record<BeamStatus, { label: string; tone: string }> = {
  WARPED: { label: 'Çözgüden çıktı', tone: 'neutral' },
  AT_SIZING: { label: 'Haşılcıda', tone: 'warning' },
  READY: { label: 'Tezgâha hazır', tone: 'ok' },
  ON_LOOM: { label: 'Tezgâhta', tone: 'info' },
  FINISHED: { label: 'Bitti', tone: 'neutral' },
}

export const drawingMethodLabels: Record<DrawingMethod, string> = {
  DRAWING: 'Tahar',
  TYING: 'Düğüm',
}

export interface DokumaWarpOrderListItem {
  id: number
  warpNo: string
  plannedDate: string
  orderId: number
  orderNo: string
  customerName: string | null
  articleCode: string
  articleName: string
  plannedBeamCount: number
  metersPerBeam: number
  ends: number
  beamCount: number
  issuedKilograms: number
  status: WarpOrderStatus
}

export interface DokumaBeam {
  id: number
  beamNo: string
  warpOrderId: number
  warpNo: string
  orderNo: string
  customerName: string | null
  articleCode: string
  articleName: string
  meters: number
  ends: number
  sizingMode: SizingMode
  isSized: boolean
  sizingSupplierId: number | null
  sizingSupplierName: string | null
  sentToSizingOn: string | null
  sizedOn: string | null
  drawingMethod: DrawingMethod | null
  readyOn: string | null
  status: BeamStatus
  notes: string | null
  /** Bağlı olduğu ya da en son bağlandığı tezgâh. */
  loomCode: string | null
  /** Uygulanan haşıl reçetesi. */
  sizingRecipeId: number | null
  sizingRecipeCode: string | null
  /** Gerçekleşen alım (%). */
  sizingPickUpPercent: number | null
  /** Gerçekleşen viskozite (saniye). */
  sizingViscositySeconds: number | null
}

export type DokumaMaterialUnit = 'KILOGRAM' | 'PIECE' | 'LITER' | 'METER'

export interface DokumaSizingPlanLine {
  materialId: number
  materialCode: string
  materialName: string
  unit: DokumaMaterialUnit
  bathPercent: number
  /** Tahmini ipliği × hedef alım × banyo oranı; hesaplanamıyorsa boş. */
  estimatedQuantity: number | null
  availableQuantity: number
}

export interface DokumaSizingPlanRecipe {
  id: number
  code: string
  name: string
  isDefault: boolean
  concentrationPercent: number | null
  bathTemperatureC: number | null
  viscositySeconds: number | null
  machineSpeedMetersPerMinute: number | null
  targetPickUpPercent: number | null
  notes: string | null
  lines: DokumaSizingPlanLine[]
}

export interface DokumaBeamSizingPlan {
  beamId: number
  beamNo: string
  articleId: number
  articleCode: string
  articleName: string
  meters: number
  ends: number
  /** Leventteki çözgü ipliğinin tahmini ağırlığı (kg). */
  estimatedYarnKilograms: number | null
  currentRecipeId: number | null
  recipes: DokumaSizingPlanRecipe[]
}

export interface DokumaBeamSizingDocument {
  beamId: number
  beamNo: string
  warpNo: string
  orderNo: string
  customerName: string | null
  articleCode: string
  articleName: string
  composition: string | null
  meters: number
  ends: number
  estimatedYarnKilograms: number | null
  sizingMode: SizingMode
  status: BeamStatus
  supplierCode: string | null
  supplierName: string | null
  supplierTaxNumber: string | null
  supplierPhone: string | null
  sentToSizingOn: string | null
  sizedOn: string | null
  pickUpPercent: number | null
  viscositySeconds: number | null
  notes: string | null
  yarns: { yarnCode: string; yarnName: string; yarnColor: string | null; ends: number }[]
  recipe: DokumaSizingPlanRecipe | null
  chemicals: { materialCode: string; materialName: string; unit: DokumaMaterialUnit; bathPercent: number; quantity: number }[]
}

export interface MarkDokumaBeamSizedRequest {
  id: number
  date: string
  sizingRecipeId: number | null
  pickUpPercent: number | null
  viscositySeconds: number | null
  chemicals: { materialId: number; quantity: number }[]
}

export interface DokumaWarpOrder {
  id: number
  warpNo: string
  plannedDate: string
  orderId: number
  orderNo: string
  ownership: DokumaOrderOwnership
  customerName: string | null
  orderLineId: number
  orderLineMeters: number
  articleCode: string
  articleName: string
  plannedBeamCount: number
  metersPerBeam: number
  ends: number
  status: WarpOrderStatus
  completedOn: string | null
  notes: string | null
  /** Planlanan levent metresi için çözgü iplikleri (fire dahil) ve verilen miktar. */
  yarns: { yarnId: number; yarnCode: string; yarnName: string; yarnColor: string | null; ends: number; requiredKilograms: number; issuedKilograms: number }[]
  issues: { id: number; yarnLotId: number; lotNo: string; producerLotNo: string | null; yarnCode: string; yarnName: string; issuedOn: string; kilograms: number }[]
  beams: DokumaBeam[]
  /** Birden çok üretici lotundan verilen iplikler (renk farkı riski). */
  mixedProducerLotYarns: string[]
  takeBacks: DokumaYarnTakeBack[]
}

export interface DokumaIssuableLot {
  id: number
  lotNo: string
  receiptDate: string
  yarnId: number
  yarnCode: string
  yarnName: string
  producerLotNo: string | null
  remainingKilograms: number
}

export interface DokumaBeamRequest {
  meters: number
  sizingMode: SizingMode
  ends: number | null
  notes: string | null
}

/** Leventin haşıl durumu: şekil, haşılcı ve tarih. */
export function sizingText(beam: DokumaBeam) {
  if (beam.sizingMode === 'NONE') {
    return sizingModeLabels.NONE
  }
  const recipe = beam.sizingRecipeCode ? ` · ${beam.sizingRecipeCode}` : ''
  if (beam.status === 'AT_SIZING') {
    return `${beam.sizingSupplierName ?? 'Haşılcı'} · ${beam.sentToSizingOn ? formatDate(beam.sentToSizingOn) : ''}${recipe}`
  }
  const pickUp = beam.sizingPickUpPercent !== null ? ` · alım %${beam.sizingPickUpPercent}` : ''
  return beam.isSized
    ? `${sizingModeLabels[beam.sizingMode]} · ${beam.sizedOn ? formatDate(beam.sizedOn) : ''}${recipe}${pickUp}`
    : `${sizingModeLabels[beam.sizingMode]} · bekliyor`
}

const warpTags = ['DokumaWarp', 'DokumaOrder', 'DokumaYarnLot'] as const

export const dokumaWarpApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getDokumaWarpOrders: build.query<PagedResult<DokumaWarpOrderListItem>, Record<string, unknown>>({
      query: (params) => ({ url: '/dokuma-warp-orders', params }),
      providesTags: ['DokumaWarp'],
    }),
    getDokumaWarpOrder: build.query<DokumaWarpOrder, number>({
      query: (id) => `/dokuma-warp-orders/${id}`,
      providesTags: ['DokumaWarp'],
    }),
    getDokumaIssuableLots: build.query<DokumaIssuableLot[], number>({
      query: (id) => `/dokuma-warp-orders/${id}/issuable-lots`,
      providesTags: ['DokumaWarp', 'DokumaYarnLot'],
    }),
    createDokumaWarpOrder: build.mutation<DokumaWarpOrder, { orderLineId: number; plannedDate: string; plannedBeamCount: number; metersPerBeam: number; notes: string | null }>({
      query: (body) => ({ url: '/dokuma-warp-orders', method: 'POST', body }),
      invalidatesTags: [...warpTags],
    }),
    updateDokumaWarpOrder: build.mutation<DokumaWarpOrder, { id: number; plannedDate: string; plannedBeamCount: number; metersPerBeam: number; notes: string | null }>({
      query: ({ id, ...body }) => ({ url: `/dokuma-warp-orders/${id}`, method: 'PUT', body }),
      invalidatesTags: [...warpTags],
    }),
    completeDokumaWarpOrder: build.mutation<DokumaWarpOrder, number>({
      query: (id) => ({ url: `/dokuma-warp-orders/${id}/complete`, method: 'POST' }),
      invalidatesTags: [...warpTags],
    }),
    cancelDokumaWarpOrder: build.mutation<DokumaWarpOrder, number>({
      query: (id) => ({ url: `/dokuma-warp-orders/${id}/cancel`, method: 'POST' }),
      invalidatesTags: [...warpTags],
    }),
    addDokumaWarpIssue: build.mutation<DokumaWarpOrder, { id: number; yarnLotId: number; kilograms: number; issuedOn: string }>({
      query: ({ id, ...body }) => ({ url: `/dokuma-warp-orders/${id}/issues`, method: 'POST', body }),
      invalidatesTags: [...warpTags],
    }),
    removeDokumaWarpIssue: build.mutation<DokumaWarpOrder, { id: number; issueId: number }>({
      query: ({ id, issueId }) => ({ url: `/dokuma-warp-orders/${id}/issues/${issueId}`, method: 'DELETE' }),
      invalidatesTags: [...warpTags],
    }),
    addDokumaBeam: build.mutation<DokumaWarpOrder, { id: number; body: DokumaBeamRequest }>({
      query: ({ id, body }) => ({ url: `/dokuma-warp-orders/${id}/beams`, method: 'POST', body }),
      invalidatesTags: [...warpTags],
    }),
    updateDokumaBeam: build.mutation<DokumaWarpOrder, { id: number; beamId: number; body: DokumaBeamRequest }>({
      query: ({ id, beamId, body }) => ({ url: `/dokuma-warp-orders/${id}/beams/${beamId}`, method: 'PUT', body }),
      invalidatesTags: [...warpTags],
    }),
    removeDokumaBeam: build.mutation<DokumaWarpOrder, { id: number; beamId: number }>({
      query: ({ id, beamId }) => ({ url: `/dokuma-warp-orders/${id}/beams/${beamId}`, method: 'DELETE' }),
      invalidatesTags: [...warpTags],
    }),
    getDokumaBeams: build.query<PagedResult<DokumaBeam>, Record<string, unknown>>({
      query: (params) => ({ url: '/dokuma-beams', params }),
      providesTags: ['DokumaWarp'],
    }),
    getDokumaBeamSizingPlan: build.query<DokumaBeamSizingPlan, number>({
      query: (id) => `/dokuma-beams/${id}/sizing-plan`,
      providesTags: ['DokumaWarp', 'DokumaYarnLot', 'MasterData'],
    }),
    getDokumaBeamSizingDocument: build.query<DokumaBeamSizingDocument, number>({
      query: (id) => `/dokuma-beams/${id}/sizing-document`,
      providesTags: ['DokumaWarp', 'MasterData'],
    }),
    sendDokumaBeamToSizing: build.mutation<DokumaBeam, { id: number; supplierId: number; date: string; sizingRecipeId: number | null }>({
      query: ({ id, ...body }) => ({ url: `/dokuma-beams/${id}/send-to-sizing`, method: 'POST', body }),
      invalidatesTags: ['DokumaWarp'],
    }),
    receiveDokumaBeamFromSizing: build.mutation<DokumaBeam, { id: number; date: string; pickUpPercent: number | null; viscositySeconds: number | null }>({
      query: ({ id, ...body }) => ({ url: `/dokuma-beams/${id}/receive-from-sizing`, method: 'POST', body }),
      invalidatesTags: ['DokumaWarp'],
    }),
    markDokumaBeamSized: build.mutation<DokumaBeam, MarkDokumaBeamSizedRequest>({
      query: ({ id, ...body }) => ({ url: `/dokuma-beams/${id}/mark-sized`, method: 'POST', body }),
      // Kimyasal tüketimi malzeme stoğunu değiştirir.
      invalidatesTags: ['DokumaWarp', 'DokumaYarnLot'],
    }),
    markDokumaBeamReady: build.mutation<DokumaBeam, { id: number; drawingMethod: DrawingMethod; date: string }>({
      query: ({ id, ...body }) => ({ url: `/dokuma-beams/${id}/ready`, method: 'POST', body }),
      invalidatesTags: ['DokumaWarp'],
    }),
    undoDokumaBeamReady: build.mutation<DokumaBeam, number>({
      query: (id) => ({ url: `/dokuma-beams/${id}/undo-ready`, method: 'POST' }),
      invalidatesTags: ['DokumaWarp'],
    }),
  }),
})

export const {
  useGetDokumaWarpOrdersQuery,
  useGetDokumaWarpOrderQuery,
  useGetDokumaIssuableLotsQuery,
  useCreateDokumaWarpOrderMutation,
  useUpdateDokumaWarpOrderMutation,
  useCompleteDokumaWarpOrderMutation,
  useCancelDokumaWarpOrderMutation,
  useAddDokumaWarpIssueMutation,
  useRemoveDokumaWarpIssueMutation,
  useAddDokumaBeamMutation,
  useUpdateDokumaBeamMutation,
  useRemoveDokumaBeamMutation,
  useGetDokumaBeamsQuery,
  useGetDokumaBeamSizingPlanQuery,
  useGetDokumaBeamSizingDocumentQuery,
  useSendDokumaBeamToSizingMutation,
  useReceiveDokumaBeamFromSizingMutation,
  useMarkDokumaBeamSizedMutation,
  useMarkDokumaBeamReadyMutation,
  useUndoDokumaBeamReadyMutation,
} = dokumaWarpApi
