import { baseApi } from '../../api/baseApi'
import type { PagedResult } from '../../types/api'
import type { DocumentLanguage } from '../../utils/documentText'
import type { DokumaOrderOwnership } from './dokumaOrdersApi'

export type DokumaQuotationStatus = 'DRAFT' | 'SENT' | 'ACCEPTED' | 'REJECTED' | 'EXPIRED'

export const dokumaQuotationStatusPresentation: Record<DokumaQuotationStatus, { label: string; tone: string }> = {
  DRAFT: { label: 'Taslak', tone: 'neutral' },
  SENT: { label: 'Gönderildi', tone: 'info' },
  ACCEPTED: { label: 'Kabul edildi', tone: 'ok' },
  REJECTED: { label: 'Reddedildi', tone: 'danger' },
  EXPIRED: { label: 'Süresi doldu', tone: 'warning' },
}

export interface DokumaQuotationListItem {
  id: number
  quotationNo: string
  quotationDate: string
  validUntil: string
  /** Müşteri adı ya da aday firma adı. */
  partyName: string
  isProspect: boolean
  ownership: DokumaOrderOwnership
  lineCount: number
  totalMeters: number
  totalAmount: number
  currency: string
  status: DokumaQuotationStatus
  orderId: number | null
  orderNo: string | null
}

export interface DokumaQuotationLineYarn {
  yarnId: number
  yarnCode: string
  yarnName: string
  yarnColor: string | null
  usedInWarp: boolean
  usedInWeft: boolean
  /** Bir metre kumaş için (kg, artikel firesi dahil). */
  kilogramsPerMeter: number
  /** Kilo fiyatı, teklifin para biriminde. */
  unitPrice: number
  /** Fasonda sıfır (iplik müşterinin). */
  costPerMeter: number
  /** Boşsa fiyat bulunamadı. */
  priceSource: string | null
}

/** Teklif satırı ve metre başına ön maliyet dökümü (teklifin para biriminde). */
export interface DokumaQuotationLine {
  id: number | null
  sequence: number
  articleId: number
  articleCode: string
  articleName: string
  rawWidthCm: number
  rawWeightGsm: number
  reedWidthCm: number
  warpCrimpPercent: number | null
  meters: number
  customerArticleCode: string | null
  loomId: number | null
  loomCode: string | null
  speedRpm: number
  efficiencyPercent: number
  picksPerCm: number
  metersPerHour: number
  warpKilogramsPerMeter: number
  weftKilogramsPerMeter: number
  yarnCost: number
  sizingCostPerWarpMeter: number
  warpingCostPerWarpMeter: number
  sizingCost: number
  warpingCost: number
  loomHourlyCost: number
  laborHourlyCost: number
  weavingHoursPerMeter: number
  weavingCost: number
  directCost: number
  wastePercent: number
  wasteCost: number
  overheadPercent: number
  overheadCost: number
  costPerMeter: number
  marginPercent: number
  calculatedPrice: number
  unitPrice: number
  amount: number
  actualMarginPercent: number | null
  notes: string | null
  yarns: DokumaQuotationLineYarn[]
  warnings: string[]
}

export interface DokumaQuotation {
  id: number
  quotationNo: string
  quotationDate: string
  validUntil: string
  customerId: number | null
  customerName: string | null
  prospectName: string | null
  contactName: string | null
  ownership: DokumaOrderOwnership
  currency: string
  documentLanguage: DocumentLanguage
  paymentTermDays: number | null
  deliveryTerms: string | null
  notes: string | null
  status: DokumaQuotationStatus
  sentAt: string | null
  decidedAt: string | null
  decidedBy: string | null
  rejectionReason: string | null
  orderId: number | null
  orderNo: string | null
  totalMeters: number
  totalCost: number
  totalAmount: number
  createdAt: string
  createdBy: string | null
  lines: DokumaQuotationLine[]
}

/** Kaydetmeden yapılan hesap. */
export interface DokumaQuotationCalculation {
  currency: string
  /** Maliyet parametrelerinin para birimi. */
  costCurrency: string
  /** Maliyet para biriminin 1 biriminin teklif para birimindeki karşılığı. */
  exchangeRate: number
  validUntil: string
  documentLanguage: DocumentLanguage
  totalMeters: number
  totalCost: number
  totalAmount: number
  lines: DokumaQuotationLine[]
}

/** Boş (null) hesap girdileri tezgâhtan, artikelden ve maliyet parametrelerinden alınır. */
export interface DokumaQuotationLineRequest {
  articleId: number
  meters: number
  loomId: number | null
  speedRpm: number | null
  efficiencyPercent: number | null
  picksPerCm: number | null
  loomHourlyCost: number | null
  laborHourlyCost: number | null
  warpingCostPerMeter: number | null
  sizingCostPerMeter: number | null
  wastePercent: number | null
  overheadPercent: number | null
  marginPercent: number | null
  unitPrice: number | null
  yarnPrices: { yarnId: number; unitPrice: number }[]
  customerArticleCode: string | null
  notes: string | null
}

export interface DokumaQuotationRequest {
  customerId: number | null
  prospectName: string | null
  quotationDate: string
  validUntil: string | null
  ownership: DokumaOrderOwnership
  currency: string | null
  documentLanguage: DocumentLanguage | null
  contactName: string | null
  paymentTermDays: number | null
  deliveryTerms: string | null
  notes: string | null
  lines: DokumaQuotationLineRequest[]
}

export interface DokumaQuotationPrintLine {
  articleCode: string
  articleName: string
  customerArticleCode: string | null
  weaveName: string | null
  composition: string | null
  rawWidthCm: number
  rawWeightGsm: number
  meters: number
  unitPrice: number
  amount: number
  notes: string | null
}

export interface DokumaQuotationPrint {
  id: number
  quotationNo: string
  quotationDate: string
  validUntil: string
  status: DokumaQuotationStatus
  currency: string
  ownership: DokumaOrderOwnership
  documentLanguage: DocumentLanguage
  customerCode: string | null
  partyName: string
  customerTaxNumber: string | null
  customerTaxOffice: string | null
  customerAddress: string | null
  contactName: string | null
  paymentTermDays: number | null
  deliveryTerms: string | null
  notes: string | null
  totalMeters: number
  total: number
  lines: DokumaQuotationPrintLine[]
}

export interface DokumaLoomCostRate {
  loomId: number
  loomCode: string
  loomName: string
  isActive: boolean
  nominalSpeedRpm: number | null
  /** Boşsa genel tezgâh saat maliyeti. */
  hourlyCost: number | null
  /** Boşsa genel verim. */
  efficiencyPercent: number | null
}

export interface DokumaCostSettings {
  currency: string
  loomHourlyCost: number
  laborHourlyCost: number
  warpingCostPerMeter: number
  sizingCostPerMeter: number
  useSizingRecipe: boolean
  overheadPercent: number
  defaultWastePercent: number
  defaultMarginPercent: number
  defaultEfficiencyPercent: number
  quotationValidityDays: number
  updatedAt: string | null
  updatedBy: string | null
  looms: DokumaLoomCostRate[]
}

export type DokumaCostSettingsRequest = Omit<DokumaCostSettings, 'updatedAt' | 'updatedBy' | 'looms'> & {
  looms: { loomId: number; hourlyCost: number | null; efficiencyPercent: number | null }[]
}

export const dokumaQuotationsApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getDokumaQuotations: build.query<PagedResult<DokumaQuotationListItem>, Record<string, unknown>>({
      query: (params) => ({ url: '/dokuma-quotations', params }),
      providesTags: ['DokumaOrder'],
    }),
    getDokumaQuotation: build.query<DokumaQuotation, number>({
      query: (id) => `/dokuma-quotations/${id}`,
      providesTags: ['DokumaOrder'],
    }),
    getDokumaQuotationPrint: build.query<DokumaQuotationPrint, number>({
      query: (id) => `/dokuma-quotations/${id}/print`,
      providesTags: ['DokumaOrder', 'MasterData'],
    }),
    calculateDokumaQuotation: build.mutation<DokumaQuotationCalculation, DokumaQuotationRequest>({
      query: (body) => ({ url: '/dokuma-quotations/calculate', method: 'POST', body }),
    }),
    saveDokumaQuotation: build.mutation<DokumaQuotation, { id?: number; body: DokumaQuotationRequest }>({
      query: ({ id, body }) => ({ url: id === undefined ? '/dokuma-quotations' : `/dokuma-quotations/${id}`, method: id === undefined ? 'POST' : 'PUT', body }),
      invalidatesTags: ['DokumaOrder'],
    }),
    deleteDokumaQuotation: build.mutation<null, number>({
      query: (id) => ({ url: `/dokuma-quotations/${id}`, method: 'DELETE' }),
      invalidatesTags: ['DokumaOrder'],
    }),
    sendDokumaQuotation: build.mutation<DokumaQuotation, number>({
      query: (id) => ({ url: `/dokuma-quotations/${id}/send`, method: 'POST' }),
      invalidatesTags: ['DokumaOrder'],
    }),
    reopenDokumaQuotation: build.mutation<DokumaQuotation, number>({
      query: (id) => ({ url: `/dokuma-quotations/${id}/reopen`, method: 'POST' }),
      invalidatesTags: ['DokumaOrder'],
    }),
    rejectDokumaQuotation: build.mutation<DokumaQuotation, { id: number; reason: string | null }>({
      query: ({ id, reason }) => ({ url: `/dokuma-quotations/${id}/reject`, method: 'POST', body: { reason } }),
      invalidatesTags: ['DokumaOrder'],
    }),
    acceptDokumaQuotation: build.mutation<DokumaQuotation, { id: number; customerId: number | null; orderDate: string | null; dueDate: string | null; customerOrderNo: string | null }>({
      query: ({ id, ...body }) => ({ url: `/dokuma-quotations/${id}/accept`, method: 'POST', body }),
      invalidatesTags: ['DokumaOrder'],
    }),
    getDokumaCostSettings: build.query<DokumaCostSettings, void>({
      query: () => '/dokuma-cost-settings',
      providesTags: ['MasterData'],
    }),
    saveDokumaCostSettings: build.mutation<DokumaCostSettings, DokumaCostSettingsRequest>({
      query: (body) => ({ url: '/dokuma-cost-settings', method: 'PUT', body }),
      invalidatesTags: ['MasterData', 'DokumaWeaving'],
    }),
  }),
})

export const {
  useGetDokumaQuotationsQuery,
  useGetDokumaQuotationQuery,
  useGetDokumaQuotationPrintQuery,
  useCalculateDokumaQuotationMutation,
  useSaveDokumaQuotationMutation,
  useDeleteDokumaQuotationMutation,
  useSendDokumaQuotationMutation,
  useReopenDokumaQuotationMutation,
  useRejectDokumaQuotationMutation,
  useAcceptDokumaQuotationMutation,
  useGetDokumaCostSettingsQuery,
  useSaveDokumaCostSettingsMutation,
} = dokumaQuotationsApi
