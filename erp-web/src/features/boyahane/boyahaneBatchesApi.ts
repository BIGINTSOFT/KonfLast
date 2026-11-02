import { baseApi } from '../../api/baseApi'
import type { PagedResult } from '../../types/api'
import type { RecipeAmountUnit } from './boyahaneArchiveApi'
import type { OperationBehavior } from './boyahaneInsightsApi'
import type { DyeChemicalKind } from './boyahaneLabApi'
import type { OrderProcessType } from './boyahaneOrdersApi'

export type BatchStatus = 'PLANNED' | 'ON_MACHINE' | 'COMPLETED' | 'CANCELLED'

export const batchStatusPresentation: Record<BatchStatus, { label: string; tone: string }> = {
  PLANNED: { label: 'Planlandı', tone: 'neutral' },
  ON_MACHINE: { label: 'İşlemde', tone: 'info' },
  COMPLETED: { label: 'Tamamlandı', tone: 'ok' },
  CANCELLED: { label: 'İptal', tone: 'danger' },
}

export const OVER_CAPACITY = 'BATCH_OVER_CAPACITY'

export type BatchStepStatus = 'PENDING' | 'IN_PROGRESS' | 'COMPLETED' | 'SKIPPED'
export type ColorCheckResult = 'APPROVED' | 'ADDITION' | 'REDO' | 'STRIPPING'
export type FinalQualityResult = 'APPROVED' | 'REJECTED' | 'REWORK'

export const batchStepStatusPresentation: Record<BatchStepStatus, { label: string; tone: string }> = {
  PENDING: { label: 'Bekliyor', tone: 'neutral' },
  IN_PROGRESS: { label: 'İşlemde', tone: 'info' },
  COMPLETED: { label: 'Bitti', tone: 'ok' },
  SKIPPED: { label: 'Kaldırıldı', tone: 'neutral' },
}

export const colorCheckResultLabels: Record<ColorCheckResult, string> = {
  APPROVED: 'Uygun',
  ADDITION: 'İlave',
  REDO: 'Yeniden işlem',
  STRIPPING: 'Söküm + yeniden boya',
}

export const finalQualityResultLabels: Record<FinalQualityResult, string> = {
  APPROVED: 'Uygun',
  REJECTED: 'Red',
  REWORK: 'Yeniden işlem',
}

/** Renk kontrolünün eklediği adımın etiketi; söküm sonucunda eklenen boyama "söküm sonrası" olarak gösterilir. */
const reworkStepLabels: Record<ColorCheckResult, string> = {
  APPROVED: 'uygun',
  ADDITION: 'ilave',
  REDO: 'yeniden işlem',
  STRIPPING: 'söküm sonrası',
}

export function batchStepLabel(step: { operationName: string; reworkKind: ColorCheckResult | null; strippingRecipeCode?: string | null }): string {
  if (step.strippingRecipeCode) {
    return `${step.operationName} (${step.strippingRecipeCode})`
  }

  return step.reworkKind ? `${step.operationName} (${reworkStepLabels[step.reworkKind]})` : step.operationName
}

export interface BatchConsumption {
  code: string
  name: string
  plannedKilograms: number
  actualKilograms: number
  withoutLotKilograms: number
  lots: { lotNo: string; supplierLotNo: string | null; kilograms: number }[]
}

export interface BatchStep {
  id: number
  sequence: number
  operationCode: string
  operationName: string
  behavior: OperationBehavior
  reworkKind: ColorCheckResult | null
  status: BatchStepStatus
  needsMachine: boolean
  machineCode: string | null
  startedBy: string | null
  completedBy: string | null
  startedAt: string | null
  completedAt: string | null
  colorResult: ColorCheckResult | null
  note: string | null
  consumptions: BatchConsumption[]
  /** Söküm adımında uygulanacak söküm reçetesi; diğer adımlarda boş. */
  strippingRecipeCode: string | null
  strippingRecipeName: string | null
  strippingLines: BatchStrippingLine[] | null
}

export interface BatchStrippingLine {
  dyeChemicalId: number
  code: string
  name: string
  amount: number
  unit: RecipeAmountUnit
  temperatureCelsius: number | null
  durationMinutes: number | null
  requiredKilograms: number
  usableKilograms: number
}

export interface BatchPrintColor {
  position: number
  name: string
  colorCardCode: string | null
  pasteKilogramsPerMeter: number | null
  /** Parti metresi × metre başına pasta; tahmin girilmemişse boş. */
  pasteKilograms: number | null
  lines: { dyeChemicalId: number; code: string; name: string; gramsPerKilogram: number; requiredKilograms: number }[]
}

export interface BatchPrintChemical {
  dyeChemicalId: number
  code: string
  name: string
  requiredKilograms: number
  usableKilograms: number
}

export interface CompleteStepBody {
  consumptions: { dyeChemicalId: number; actualKilograms: number }[] | null
  colorResult: ColorCheckResult | null
  /** "Söküm + yeniden boya" sonucunda zorunlu. */
  strippingRecipeId?: number | null
  itemResults: { itemId: number; result: FinalQualityResult; kilograms: number | null; meters: number | null }[] | null
  note: string | null
}

export interface BatchListItem {
  id: number
  batchNo: string
  machineCode: string
  plannedDate: string | null
  itemCount: number
  totalKilograms: number
  itemCode: string
  colorName: string | null
  processType: OrderProcessType
  status: BatchStatus
  currentStep: string | null
  createdAt: string
}

export interface BatchItem {
  id: number
  orderId: number
  orderNo: string
  customerName: string | null
  itemNo: string
  colorName: string | null
  kilograms: number
  meters: number | null
  finalResult: FinalQualityResult | null
  finalNote: string | null
  finalKilograms: number | null
  finalMeters: number | null
  fireKilogramPercent: number | null
  fireMeterPercent: number | null
  fireTolerancePercent: number | null
  overTolerance: boolean
}

export interface BatchRecipeLine {
  dyeChemicalId: number
  code: string
  name: string
  kind: DyeChemicalKind
  amount: number
  requiredKilograms: number
  usableKilograms: number
}

export interface Batch {
  id: number
  batchNo: string
  machineId: number
  machineCode: string
  machineName: string
  machineCapacityKg: number
  processType: OrderProcessType
  liquorRatio: number | null
  liquorLiters: number | null
  plannedDate: string | null
  notes: string | null
  status: BatchStatus
  capacityOverrideBy: string | null
  capacityOverrideNote: string | null
  startedAt: string | null
  completedAt: string | null
  routeId: number | null
  routeName: string | null
  recipeNo: string | null
  totalKilograms: number
  totalMeters: number | null
  cardUid: string | null
  reworkOfBatchId: number | null
  reworkOfBatchNo: string | null
  currentStepId: number | null
  items: BatchItem[]
  recipe: BatchRecipeLine[]
  steps: BatchStep[]
  chemicalCosts: { currency: string; amount: number }[]
  hasUncostedConsumption: boolean
  /** Baskı partisinde sipariş satırının deseni ve renk yolu. */
  patternCode: string | null
  patternName: string | null
  colorwayCode: string | null
  colorwayName: string | null
  printColors: BatchPrintColor[] | null
  printRecipe: BatchPrintChemical[] | null
}

export interface BatchCandidate {
  rollId: number | null
  yarnLotId: number | null
  itemNo: string
  groupKey: string
  groupTitle: string
  orderId: number
  orderNo: string
  customerName: string | null
  colorName: string | null
  kilograms: number
  meters: number | null
  usesRecipe: boolean
  processType: OrderProcessType
  selected: boolean
}

export interface BatchRequest {
  machineId: number
  rollIds: number[]
  yarnLotIds: number[]
  liquorRatio: number | null
  routeId: number | null
  plannedDate: string | null
  notes: string | null
  capacityOverrideNote: string | null
}

const tags = ['BoyahaneBatch', 'BoyahaneOrder', 'BoyahaneInspection', 'BoyahaneLab'] as const

export const boyahaneBatchesApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getBatches: build.query<PagedResult<BatchListItem>, Record<string, unknown>>({
      query: (params) => ({ url: '/boyahane-batches', params }),
      providesTags: ['BoyahaneBatch'],
    }),
    /** Makine planı: planlanan bütün partiler, sayfasız. */
    getBatchPlan: build.query<BatchListItem[], void>({
      query: () => '/boyahane-batches/plan',
      providesTags: ['BoyahaneBatch'],
    }),
    getBatch: build.query<Batch, number>({
      query: (id) => `/boyahane-batches/${id}`,
      providesTags: ['BoyahaneBatch'],
    }),
    getBatchCandidates: build.query<BatchCandidate[], number | undefined>({
      query: (batchId) => ({ url: '/boyahane-batches/candidates', params: batchId === undefined ? {} : { batchId } }),
      providesTags: ['BoyahaneBatch', 'BoyahaneOrder', 'BoyahaneInspection', 'BoyahaneLab'],
    }),
    saveBatch: build.mutation<Batch, { id?: number; body: BatchRequest }>({
      query: ({ id, body }) => ({ url: id === undefined ? '/boyahane-batches' : `/boyahane-batches/${id}`, method: id === undefined ? 'POST' : 'PUT', body }),
      invalidatesTags: [...tags],
    }),
    setBatchCard: build.mutation<Batch, { id: number; uid: string | null }>({
      query: ({ id, uid }) => ({ url: `/boyahane-batches/${id}/card`, method: 'PUT', body: { uid } }),
      invalidatesTags: ['BoyahaneBatch'],
    }),
    startBatchStep: build.mutation<Batch, { batchId: number; stepId: number; machineId: number | null }>({
      query: ({ batchId, stepId, machineId }) => ({ url: `/boyahane-batches/${batchId}/steps/${stepId}/start`, method: 'POST', body: { machineId } }),
      invalidatesTags: ['BoyahaneBatch'],
    }),
    completeBatchStep: build.mutation<Batch, { batchId: number; stepId: number; body: CompleteStepBody }>({
      query: ({ batchId, stepId, body }) => ({ url: `/boyahane-batches/${batchId}/steps/${stepId}/complete`, method: 'POST', body }),
      invalidatesTags: ['BoyahaneBatch'],
    }),
    skipBatchStep: build.mutation<Batch, { batchId: number; stepId: number }>({
      query: ({ batchId, stepId }) => ({ url: `/boyahane-batches/${batchId}/steps/${stepId}/skip`, method: 'POST' }),
      invalidatesTags: ['BoyahaneBatch'],
    }),
    resetBoyahaneTabletDevice: build.mutation<null, number>({
      query: (id) => ({ url: `/boyahane-tablets/${id}/reset-device`, method: 'POST' }),
      invalidatesTags: ['MasterData'],
    }),
    cancelBatch: build.mutation<Batch, number>({
      query: (id) => ({ url: `/boyahane-batches/${id}/cancel`, method: 'POST' }),
      invalidatesTags: [...tags],
    }),
  }),
})

export const {
  useGetBatchesQuery,
  useGetBatchPlanQuery,
  useGetBatchQuery,
  useGetBatchCandidatesQuery,
  useSaveBatchMutation,
  useSetBatchCardMutation,
  useStartBatchStepMutation,
  useCompleteBatchStepMutation,
  useSkipBatchStepMutation,
  useResetBoyahaneTabletDeviceMutation,
  useCancelBatchMutation,
} = boyahaneBatchesApi
