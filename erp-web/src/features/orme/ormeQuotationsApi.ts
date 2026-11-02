import { baseApi } from '../../api/baseApi'
import type { PagedResult } from '../../types/api'
import type { DocumentLanguage } from '../../utils/documentText'
import type { OrmeOrderOwnership, OrmeOrderStatus } from './ormeOrdersApi'

export type OrmeQuotationStatus = 'DRAFT' | 'SENT' | 'ACCEPTED' | 'REJECTED' | 'EXPIRED'
export type OrmeQuotationPriceSource = 'NONE' | 'PURCHASE_ORDER' | 'YARN_LOT' | 'MANUAL'

export const ormeQuotationStatusPresentation: Record<OrmeQuotationStatus, { label: string; tone: string }> = {
  DRAFT: { label: 'Taslak', tone: 'neutral' },
  SENT: { label: 'Gönderildi', tone: 'info' },
  ACCEPTED: { label: 'Kabul edildi', tone: 'ok' },
  REJECTED: { label: 'Reddedildi', tone: 'danger' },
  EXPIRED: { label: 'Süresi doldu', tone: 'warning' },
}

export const ormeQuotationPriceSourceLabels: Record<OrmeQuotationPriceSource, string> = {
  NONE: 'Fiyat yok',
  PURCHASE_ORDER: 'Son satın alma',
  YARN_LOT: 'Son lot',
  MANUAL: 'Elle',
}

export interface OrmeQuotationListItem {
  id: number
  quotationNo: string
  quotationDate: string
  validUntil: string
  /** Müşteri adı ya da aday firma adı. */
  customerName: string | null
  isProspect: boolean
  lineCount: number
  totalKilograms: number
  totalAmount: number
  currency: string
  status: OrmeQuotationStatus
}

export interface OrmeQuotationLineYarn {
  yarnId: number
  yarnCode: string
  yarnName: string
  yarnColor: string | null
  percent: number
  unitPrice: number | null
  priceSource: OrmeQuotationPriceSource
  priceReference: string | null
  /** Bu ipliğin kumaş kilosuna düşen fireli maliyeti. */
  costPerKilogram: number
}

export interface OrmeQuotationLine {
  id: number
  sequence: number
  fabricId: number
  fabricCode: string
  fabricName: string
  customerFabricCode: string | null
  kilograms: number
  /** Fason: iplik müşteriden, fiyat yalnızca örme ücreti. */
  isCommission: boolean
  machineId: number | null
  machineCode: string | null
  machineName: string | null
  wastePercent: number
  kilogramsPerHour: number | null
  machineHourlyCost: number
  laborHourlyCost: number
  overheadPercent: number
  marginPercent: number
  yarnCostPerKilogram: number
  knittingCostPerKilogram: number
  overheadPerKilogram: number
  costPerKilogram: number
  calculatedPrice: number
  unitPrice: number
  amount: number
  costAmount: number
  /** İplik fiyatı ya da saatte örülen kilo eksik. */
  hasMissingCosts: boolean
  notes: string | null
  orderId: number | null
  orderNo: string | null
  yarns: OrmeQuotationLineYarn[]
}

export interface OrmeQuotationOrder {
  id: number
  orderNo: string
  ownership: OrmeOrderOwnership
  status: OrmeOrderStatus
}

export interface OrmeQuotation {
  id: number
  quotationNo: string
  quotationDate: string
  validUntil: string
  customerId: number | null
  customerName: string | null
  prospectName: string | null
  currency: string
  /** Teklifte seçilen dil; boşsa müşterinin dili. */
  documentLanguage: DocumentLanguage | null
  printLanguage: DocumentLanguage
  status: OrmeQuotationStatus
  notes: string | null
  totalKilograms: number
  totalAmount: number
  totalCost: number
  profitAmount: number
  marginPercent: number | null
  hasMissingCosts: boolean
  createdAt: string
  createdBy: string | null
  orders: OrmeQuotationOrder[]
  lines: OrmeQuotationLine[]
}

export interface OrmeQuotationRequest {
  customerId: number | null
  prospectName: string | null
  quotationDate: string
  validUntil: string
  currency: string | null
  documentLanguage: DocumentLanguage | null
  notes: string | null
}

export interface OrmeQuotationYarnPriceRequest {
  yarnId: number
  unitPrice: number | null
  priceSource: OrmeQuotationPriceSource
  priceReference: string | null
}

export interface OrmeQuotationLineRequest {
  fabricId: number
  kilograms: number
  isCommission: boolean
  machineId: number | null
  wastePercent: number
  kilogramsPerHour: number | null
  machineHourlyCost: number
  laborHourlyCost: number
  overheadPercent: number
  marginPercent: number
  /** Elle girilen fiyat; boşsa maliyet × (1 + kâr). */
  unitPrice: number | null
  customerFabricCode: string | null
  notes: string | null
  yarns: OrmeQuotationYarnPriceRequest[]
}

export interface OrmeQuotationAcceptRequest {
  customerId: number | null
  orderDate: string | null
  dueDate: string | null
  customerOrderNo: string | null
}

export interface OrmeQuotationYarnCost {
  yarnId: number
  yarnCode: string
  yarnName: string
  yarnColor: string | null
  percent: number
  /** Teklifin para birimine çevrilmiş fiyat; bulunamadıysa boş. */
  unitPrice: number | null
  priceSource: OrmeQuotationPriceSource
  priceReference: string | null
  priceDate: string | null
  originalPrice: number | null
  originalCurrency: string | null
}

export interface OrmeQuotationCostDefaults {
  fabricId: number
  fabricCode: string
  fabricName: string
  hasRecipe: boolean
  wastePercent: number
  kilogramsPerHour: number | null
  machineId: number | null
  machineHourlyCost: number
  laborHourlyCost: number
  overheadPercent: number
  marginPercent: number
  currency: string
  /** Kur bulunamadığı için bir tutar çevrilemedi. */
  missingRate: boolean
  yarns: OrmeQuotationYarnCost[]
}

export interface OrmeQuotationPrintLine {
  id: number
  fabricCode: string
  fabricName: string
  knitName: string | null
  composition: string | null
  rawWeightGsm: number
  rawWidthCm: number | null
  customerFabricCode: string | null
  kilograms: number
  isCommission: boolean
  unitPrice: number
  amount: number
  notes: string | null
}

export interface OrmeQuotationPrint {
  id: number
  quotationNo: string
  status: OrmeQuotationStatus
  quotationDate: string
  validUntil: string
  currency: string
  notes: string | null
  customerCode: string | null
  partyName: string
  customerTaxNumber: string | null
  customerTaxOffice: string | null
  customerAddress: string | null
  documentLanguage: DocumentLanguage
  paymentTermDays: number | null
  vatRate: number | null
  secondQualityDiscountPercent: number | null
  totalKilograms: number
  subtotal: number
  vatAmount: number
  total: number
  hasCommissionLines: boolean
  lines: OrmeQuotationPrintLine[]
}

export interface OrmeMachineCostRate {
  machineId: number
  machineCode: string
  machineName: string
  isActive: boolean
  /** Boşsa genel makine saat maliyeti. */
  hourlyCost: number | null
}

export interface OrmeCostSettings {
  machineHourlyCost: number
  laborHourlyCost: number
  overheadPercent: number
  defaultWastePercent: number
  defaultMarginPercent: number
  /** Saatlik tutarların para birimi (yerel para). */
  currency: string
  machines: OrmeMachineCostRate[]
}

export interface OrmeCostSettingsRequest {
  machineHourlyCost: number
  laborHourlyCost: number
  overheadPercent: number
  defaultWastePercent: number
  defaultMarginPercent: number
  machines: { machineId: number; hourlyCost: number }[]
}

export interface OrmeQuotationCostInput {
  isCommission: boolean
  yarns: { percent: number; unitPrice: number | null }[]
  wastePercent: number
  kilogramsPerHour: number | null
  machineHourlyCost: number
  laborHourlyCost: number
  overheadPercent: number
  marginPercent: number
}

export interface OrmeQuotationCostBreakdown {
  yarnCost: number
  knittingCost: number
  overhead: number
  totalCost: number
  calculatedPrice: number
}

const round = (value: number, digits: number) => {
  const factor = 10 ** digits
  return Math.round(value * factor) / factor
}

/**
 * Ön maliyet: sunucudaki hesabın (QuotationCost) aynısı; satır penceresinde girdiler değiştikçe anlık gösterilir.
 * İplik: Σ (oran × fiyat) × (1 + fire); örme: (makine + işçilik) / kg/saat; genel gider doğrudan maliyete, kâr toplam maliyete eklenir.
 */
export function ormeQuotationCost(input: OrmeQuotationCostInput): OrmeQuotationCostBreakdown {
  const yarn = input.isCommission
    ? 0
    : input.yarns.reduce((sum, item) => sum + (item.percent / 100) * (item.unitPrice ?? 0), 0) * (1 + input.wastePercent / 100)
  const knitting = input.kilogramsPerHour !== null && input.kilogramsPerHour > 0
    ? (input.machineHourlyCost + input.laborHourlyCost) / input.kilogramsPerHour
    : 0
  const overhead = (yarn + knitting) * input.overheadPercent / 100
  const total = yarn + knitting + overhead
  return {
    yarnCost: round(yarn, 4),
    knittingCost: round(knitting, 4),
    overhead: round(overhead, 4),
    totalCost: round(total, 4),
    calculatedPrice: Math.max(0, round(total * (1 + input.marginPercent / 100), 4)),
  }
}

/** Elle girilen fiyattan kâr oranı; maliyet sıfırsa hesaplanamaz. */
export function ormeQuotationMarginFor(totalCost: number, unitPrice: number): number | null {
  return totalCost > 0 ? round((unitPrice / totalCost - 1) * 100, 2) : null
}

const tags = ['OrmeOrder'] as const

export const ormeQuotationsApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getOrmeQuotations: build.query<PagedResult<OrmeQuotationListItem>, Record<string, unknown>>({
      query: (params) => ({ url: '/orme-quotations', params }),
      providesTags: [...tags],
    }),
    getOrmeQuotation: build.query<OrmeQuotation, number>({
      query: (id) => `/orme-quotations/${id}`,
      providesTags: [...tags],
    }),
    getOrmeQuotationPrint: build.query<OrmeQuotationPrint, number>({
      query: (id) => `/orme-quotations/${id}/print`,
      providesTags: [...tags, 'MasterData'],
    }),
    getOrmeQuotationCostDefaults: build.query<OrmeQuotationCostDefaults, { fabricId: number; machineId: number | null; currency: string; date: string }>({
      query: ({ fabricId, machineId, currency, date }) => ({
        url: '/orme-quotations/cost-defaults',
        params: { fabricId, currency, date, ...(machineId === null ? {} : { machineId }) },
      }),
      providesTags: [...tags, 'MasterData'],
    }),
    saveOrmeQuotation: build.mutation<OrmeQuotation, { id?: number; body: OrmeQuotationRequest }>({
      query: ({ id, body }) => ({ url: id === undefined ? '/orme-quotations' : `/orme-quotations/${id}`, method: id === undefined ? 'POST' : 'PUT', body }),
      invalidatesTags: [...tags],
    }),
    deleteOrmeQuotation: build.mutation<null, number>({
      query: (id) => ({ url: `/orme-quotations/${id}`, method: 'DELETE' }),
      invalidatesTags: [...tags],
    }),
    saveOrmeQuotationLine: build.mutation<OrmeQuotation, { id: number; lineId?: number; body: OrmeQuotationLineRequest }>({
      query: ({ id, lineId, body }) => ({
        url: lineId === undefined ? `/orme-quotations/${id}/lines` : `/orme-quotations/${id}/lines/${lineId}`,
        method: lineId === undefined ? 'POST' : 'PUT',
        body,
      }),
      invalidatesTags: [...tags],
    }),
    deleteOrmeQuotationLine: build.mutation<OrmeQuotation, { id: number; lineId: number }>({
      query: ({ id, lineId }) => ({ url: `/orme-quotations/${id}/lines/${lineId}`, method: 'DELETE' }),
      invalidatesTags: [...tags],
    }),
    sendOrmeQuotation: build.mutation<OrmeQuotation, number>({
      query: (id) => ({ url: `/orme-quotations/${id}/send`, method: 'POST' }),
      invalidatesTags: [...tags],
    }),
    reopenOrmeQuotation: build.mutation<OrmeQuotation, number>({
      query: (id) => ({ url: `/orme-quotations/${id}/reopen`, method: 'POST' }),
      invalidatesTags: [...tags],
    }),
    rejectOrmeQuotation: build.mutation<OrmeQuotation, number>({
      query: (id) => ({ url: `/orme-quotations/${id}/reject`, method: 'POST' }),
      invalidatesTags: [...tags],
    }),
    acceptOrmeQuotation: build.mutation<OrmeQuotation, { id: number; body: OrmeQuotationAcceptRequest }>({
      query: ({ id, body }) => ({ url: `/orme-quotations/${id}/accept`, method: 'POST', body }),
      invalidatesTags: [...tags],
    }),
    getOrmeCostSettings: build.query<OrmeCostSettings, void>({
      query: () => '/orme-cost-settings',
      providesTags: [...tags, 'MasterData'],
    }),
    saveOrmeCostSettings: build.mutation<OrmeCostSettings, OrmeCostSettingsRequest>({
      query: (body) => ({ url: '/orme-cost-settings', method: 'PUT', body }),
      invalidatesTags: [...tags],
    }),
  }),
})

export const {
  useGetOrmeQuotationsQuery,
  useGetOrmeQuotationQuery,
  useGetOrmeQuotationPrintQuery,
  useLazyGetOrmeQuotationCostDefaultsQuery,
  useSaveOrmeQuotationMutation,
  useDeleteOrmeQuotationMutation,
  useSaveOrmeQuotationLineMutation,
  useDeleteOrmeQuotationLineMutation,
  useSendOrmeQuotationMutation,
  useReopenOrmeQuotationMutation,
  useRejectOrmeQuotationMutation,
  useAcceptOrmeQuotationMutation,
  useGetOrmeCostSettingsQuery,
  useSaveOrmeCostSettingsMutation,
} = ormeQuotationsApi
