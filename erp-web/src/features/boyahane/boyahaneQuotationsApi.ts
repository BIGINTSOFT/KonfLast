import { baseApi } from '../../api/baseApi'
import type { PagedResult } from '../../types/api'
import type { DocumentLanguage } from '../../utils/documentText'
import type { DyeChemicalKind } from './boyahaneLabApi'
import type { OrderOwnership, OrderProcessType, OrderUnit } from './boyahaneOrdersApi'

export type QuotationStatus = 'DRAFT' | 'SENT' | 'ACCEPTED' | 'REJECTED' | 'EXPIRED'
export type ProcessMinutesSource = 'MANUAL' | 'ROUTE' | 'DEFAULT'
export type ChemicalCostMethod = 'LAST_LOT' | 'WEIGHTED_AVERAGE'

export const quotationStatusPresentation: Record<QuotationStatus, { label: string; tone: string }> = {
  DRAFT: { label: 'Taslak', tone: 'neutral' },
  SENT: { label: 'Gönderildi', tone: 'info' },
  ACCEPTED: { label: 'Kabul edildi', tone: 'ok' },
  REJECTED: { label: 'Reddedildi', tone: 'danger' },
  EXPIRED: { label: 'Süresi doldu', tone: 'warning' },
}

export const processMinutesSourceLabels: Record<ProcessMinutesSource, string> = {
  MANUAL: 'Elle girildi',
  ROUTE: 'Rota standart süresi',
  DEFAULT: 'İşlem türü varsayılanı',
}

export const chemicalCostMethodLabels: Record<ChemicalCostMethod, string> = {
  LAST_LOT: 'Son lot maliyeti',
  WEIGHTED_AVERAGE: 'Eldeki lotların ağırlıklı ortalaması',
}

export interface BoyahaneQuotationListItem {
  id: number
  quotationNo: string
  quotationDate: string
  validUntil: string
  /** Müşteri adı; müşteri yoksa aday adı. */
  customerName: string
  isProspect: boolean
  lineCount: number
  totalAmount: number
  currency: string
  status: QuotationStatus
  /** Gönderilmiş teklifin geçerlilik günü geçti. */
  isPastValidity: boolean
  orderId: number | null
  orderNo: string | null
}

/** Teklif satırı: girilen değerler (…Input) ve kayıt anındaki maliyet dökümü (birim başına, teklif para biriminde). */
export interface BoyahaneQuotationLine {
  id: number
  processType: OrderProcessType
  fabricId: number | null
  yarnId: number | null
  itemCode: string
  itemName: string
  colorName: string | null
  colorCode: string | null
  colorCardId: number | null
  colorCardCode: string | null
  patternId: number | null
  patternCode: string | null
  colorwayId: number | null
  colorwayCode: string | null
  quantity: number
  unit: OrderUnit
  recipeTrialId: number | null
  recipeNo: string | null
  machineId: number | null
  machineCode: string | null
  batchKilogramsInput: number | null
  liquorRatioInput: number | null
  processMinutesInput: number | null
  firePercentInput: number | null
  overheadPercentInput: number | null
  marginPercentInput: number | null
  unitPriceInput: number | null
  batchKilograms: number
  liquorRatio: number | null
  processMinutes: number
  minutesSource: ProcessMinutesSource
  kilogramsPerMeter: number | null
  chemicalCost: number
  printCost: number
  machineCost: number
  laborCost: number
  firePercent: number
  fireCost: number
  overheadPercent: number
  overheadCost: number
  unitCost: number
  marginPercent: number | null
  unitPrice: number
  amount: number
  costWarning: string | null
  notes: string | null
}

export interface BoyahaneQuotation {
  id: number
  quotationNo: string
  quotationDate: string
  validUntil: string
  customerId: number | null
  customerCode: string | null
  customerName: string | null
  prospectName: string | null
  customerAddress: string | null
  customerTaxNumber: string | null
  customerTaxOffice: string | null
  paymentTermDays: number | null
  ownership: OrderOwnership
  dueDate: string | null
  currency: string
  documentLanguage: DocumentLanguage
  notes: string | null
  status: QuotationStatus
  isPastValidity: boolean
  sentAt: string | null
  decidedAt: string | null
  decidedBy: string | null
  decisionNote: string | null
  orderId: number | null
  orderNo: string | null
  totalCost: number
  totalAmount: number
  /** Toplam tutarın toplam maliyete göre kâr payı. */
  marginPercent: number | null
  createdAt: string
  createdBy: string | null
  lines: BoyahaneQuotationLine[]
}

export interface BoyahaneQuotationLineRequest {
  processType: OrderProcessType
  quantity: number
  unit: OrderUnit
  fabricId: number | null
  yarnId: number | null
  colorName: string | null
  colorCode: string | null
  colorCardId: number | null
  patternId: number | null
  colorwayId: number | null
  /** Boşsa renk kartının son onaylı reçetesi. */
  recipeTrialId: number | null
  machineId: number | null
  batchKilograms: number | null
  liquorRatio: number | null
  processMinutes: number | null
  firePercent: number | null
  overheadPercent: number | null
  marginPercent: number | null
  /** Elle fiyat; boşsa maliyet × (1 + kâr payı). */
  unitPrice: number | null
  notes: string | null
}

export interface BoyahaneQuotationRequest {
  quotationDate: string
  validUntil: string
  customerId: number | null
  prospectName: string | null
  ownership: OrderOwnership
  dueDate: string | null
  currency: string | null
  documentLanguage: DocumentLanguage | null
  notes: string | null
  lines: BoyahaneQuotationLineRequest[]
}

export interface BoyahaneQuotationChemicalCost {
  dyeChemicalId: number
  code: string
  name: string
  kind: DyeChemicalKind
  /** Baskı pastasından gelen satır (miktar g/kg pasta); değilse reçeteden (boya %, kimyasal g/L). */
  printing: boolean
  amount: number
  kilogramsPerUnit: number
  costPerKilogram: number | null
  cost: number
}

export interface BoyahaneQuotationLineCost {
  processType: OrderProcessType
  itemCode: string
  itemName: string
  colorName: string | null
  quantity: number
  unit: OrderUnit
  recipeTrialId: number | null
  recipeNo: string | null
  recipeFromColorCard: boolean
  machineId: number | null
  machineCode: string | null
  batchKilograms: number
  liquorRatio: number | null
  processMinutes: number
  minutesSource: ProcessMinutesSource
  kilogramsPerMeter: number | null
  machineHourlyCost: number
  laborCostPerMinute: number
  chemicalCost: number
  printCost: number
  machineCost: number
  laborCost: number
  firePercent: number
  fireCost: number
  overheadPercent: number
  overheadCost: number
  unitCost: number
  marginPercent: number | null
  suggestedUnitPrice: number
  unitPrice: number
  priceIsManual: boolean
  amount: number
  warnings: string[]
  chemicals: BoyahaneQuotationChemicalCost[]
}

export interface BoyahaneQuotationCalculation {
  currency: string
  totalCost: number
  totalAmount: number
  lines: BoyahaneQuotationLineCost[]
}

export interface BoyahaneQuotationRecipeOption {
  trialId: number
  recipeNo: string
  colorCardCode: string | null
  colorName: string | null
  colorCode: string | null
  itemCode: string | null
  customerName: string | null
  decisionOn: string | null
}

export interface BoyahaneMachineCostRate {
  machineId: number
  code: string
  name: string
  machineType: string
  capacityKg: number
  isActive: boolean
  /** Boşsa varsayılan saat maliyeti kullanılır. */
  hourlyCost: number | null
}

export interface BoyahaneCostSettings {
  currency: string
  defaultMachineHourlyCost: number
  laborCostPerMinute: number
  defaultLiquorRatio: number
  defaultBatchKilograms: number
  overheadPercent: number
  firePercent: number
  marginPercent: number
  chemicalCostMethod: ChemicalCostMethod
  fabricDyeingMinutes: number
  yarnDyeingMinutes: number
  finishingMinutes: number
  printingMinutes: number
  /** Parametreler henüz kaydedilmedi; gösterilen değerler varsayılandır. */
  isDefault: boolean
  updatedAt: string | null
  updatedBy: string | null
  machines: BoyahaneMachineCostRate[]
}

export type SaveBoyahaneCostSettingsRequest = Omit<BoyahaneCostSettings, 'isDefault' | 'updatedAt' | 'updatedBy' | 'machines'> & {
  machineRates: { machineId: number; hourlyCost: number | null }[]
}

// Etiket listesi ortak dosyada olduğu için teklifler sipariş etiketini kullanır: kabul edilen teklif sipariş açar.
const quotationTag = 'BoyahaneOrder' as const

export const boyahaneQuotationsApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getBoyahaneQuotations: build.query<PagedResult<BoyahaneQuotationListItem>, Record<string, unknown>>({
      query: (params) => ({ url: '/boyahane-quotations', params }),
      providesTags: [quotationTag],
    }),
    getBoyahaneQuotation: build.query<BoyahaneQuotation, number>({
      query: (id) => `/boyahane-quotations/${id}`,
      providesTags: [quotationTag, 'MasterData'],
    }),
    calculateBoyahaneQuotation: build.mutation<BoyahaneQuotationCalculation, BoyahaneQuotationRequest>({
      query: (body) => ({ url: '/boyahane-quotations/calculate', method: 'POST', body }),
    }),
    getBoyahaneQuotationRecipeOptions: build.query<BoyahaneQuotationRecipeOption[], { colorCardId?: number; search?: string }>({
      query: (params) => ({ url: '/boyahane-quotations/recipe-options', params }),
      providesTags: ['BoyahaneLab'],
    }),
    saveBoyahaneQuotation: build.mutation<BoyahaneQuotation, { id?: number; body: BoyahaneQuotationRequest }>({
      query: ({ id, body }) => ({ url: id === undefined ? '/boyahane-quotations' : `/boyahane-quotations/${id}`, method: id === undefined ? 'POST' : 'PUT', body }),
      invalidatesTags: [quotationTag],
    }),
    recalculateBoyahaneQuotation: build.mutation<BoyahaneQuotation, number>({
      query: (id) => ({ url: `/boyahane-quotations/${id}/recalculate`, method: 'POST' }),
      invalidatesTags: [quotationTag],
    }),
    sendBoyahaneQuotation: build.mutation<BoyahaneQuotation, number>({
      query: (id) => ({ url: `/boyahane-quotations/${id}/send`, method: 'POST' }),
      invalidatesTags: [quotationTag],
    }),
    acceptBoyahaneQuotation: build.mutation<BoyahaneQuotation, { id: number; customerId: number | null; customerOrderNo: string | null; dueDate: string | null; note: string | null }>({
      query: ({ id, ...body }) => ({ url: `/boyahane-quotations/${id}/accept`, method: 'POST', body }),
      invalidatesTags: [quotationTag],
    }),
    rejectBoyahaneQuotation: build.mutation<BoyahaneQuotation, { id: number; note: string | null }>({
      query: ({ id, note }) => ({ url: `/boyahane-quotations/${id}/reject`, method: 'POST', body: { note } }),
      invalidatesTags: [quotationTag],
    }),
    expireBoyahaneQuotation: build.mutation<BoyahaneQuotation, { id: number; note: string | null }>({
      query: ({ id, note }) => ({ url: `/boyahane-quotations/${id}/expire`, method: 'POST', body: { note } }),
      invalidatesTags: [quotationTag],
    }),
    reopenBoyahaneQuotation: build.mutation<BoyahaneQuotation, number>({
      query: (id) => ({ url: `/boyahane-quotations/${id}/reopen`, method: 'POST' }),
      invalidatesTags: [quotationTag],
    }),
    deleteBoyahaneQuotation: build.mutation<null, number>({
      query: (id) => ({ url: `/boyahane-quotations/${id}`, method: 'DELETE' }),
      invalidatesTags: [quotationTag],
    }),
    getBoyahaneCostSettings: build.query<BoyahaneCostSettings, void>({
      query: () => '/boyahane-cost-settings',
      providesTags: ['MasterData'],
    }),
    saveBoyahaneCostSettings: build.mutation<BoyahaneCostSettings, SaveBoyahaneCostSettingsRequest>({
      query: (body) => ({ url: '/boyahane-cost-settings', method: 'PUT', body }),
      invalidatesTags: ['MasterData', 'BoyahaneBatch'],
    }),
  }),
})

export const {
  useGetBoyahaneQuotationsQuery,
  useGetBoyahaneQuotationQuery,
  useCalculateBoyahaneQuotationMutation,
  useGetBoyahaneQuotationRecipeOptionsQuery,
  useSaveBoyahaneQuotationMutation,
  useRecalculateBoyahaneQuotationMutation,
  useSendBoyahaneQuotationMutation,
  useAcceptBoyahaneQuotationMutation,
  useRejectBoyahaneQuotationMutation,
  useExpireBoyahaneQuotationMutation,
  useReopenBoyahaneQuotationMutation,
  useDeleteBoyahaneQuotationMutation,
  useGetBoyahaneCostSettingsQuery,
  useSaveBoyahaneCostSettingsMutation,
} = boyahaneQuotationsApi

/** Satırdaki girilen değerleri istek biçimine çevirir (yeniden hesap ve form için). */
export function quotationLineToRequest(line: BoyahaneQuotationLine): BoyahaneQuotationLineRequest {
  return {
    processType: line.processType,
    quantity: line.quantity,
    unit: line.unit,
    fabricId: line.fabricId,
    yarnId: line.yarnId,
    colorName: line.colorName,
    colorCode: line.colorCode,
    colorCardId: line.colorCardId,
    patternId: line.patternId,
    colorwayId: line.colorwayId,
    recipeTrialId: line.recipeTrialId,
    machineId: line.machineId,
    batchKilograms: line.batchKilogramsInput,
    liquorRatio: line.liquorRatioInput,
    processMinutes: line.processMinutesInput,
    firePercent: line.firePercentInput,
    overheadPercent: line.overheadPercentInput,
    marginPercent: line.marginPercentInput,
    unitPrice: line.unitPriceInput,
    notes: line.notes,
  }
}
