import { baseApi } from '../../api/baseApi'

export interface BonusTier {
  /** Kademenin başladığı günlük verim (%). */
  minEfficiencyPercent: number
  /** Parça başı kazanca eklenecek prim oranı (%). */
  bonusPercent: number
}

export interface PayrollSettings {
  /** Bir standart dakikanın ücreti; birim ücreti girilmemiş operasyonlarda SAM ile çarpılır. */
  minuteRate: number
  currency: string
  bonusTiers: BonusTier[]
  updatedAt: string | null
  updatedBy: string | null
}

export interface OperatorEarnings {
  operatorId: number
  operatorCode: string
  operatorName: string
  daysWorked: number
  produced: number
  earnedMinutes: number
  efficiencyPercent: number | null
  pieceEarnings: number
  bonusAmount: number
  totalEarnings: number
  /** Ücreti tanımlı olmayan operasyonlarda üretilen adet (kazanca girmez). */
  unpricedQuantity: number
}

export interface PayrollReport {
  from: string
  to: string
  currency: string
  minuteRate: number
  total: OperatorEarnings
  rows: OperatorEarnings[]
}

export interface EarningsDay {
  day: string
  produced: number
  efficiencyPercent: number | null
  pieceEarnings: number
  bonusPercent: number
  bonusAmount: number
  totalEarnings: number
}

export interface EarningsOperation {
  operationId: number
  operationCode: string
  operationName: string
  produced: number
  unitRate: number | null
  amount: number
}

export interface OperatorEarningsDetail {
  from: string
  to: string
  currency: string
  summary: OperatorEarnings
  days: EarningsDay[]
  operations: EarningsOperation[]
}

interface Range {
  from: string
  to: string
}

export const payrollApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getPayrollSettings: build.query<PayrollSettings, void>({
      query: () => '/payroll/settings',
      providesTags: ['Payroll'],
    }),
    savePayrollSettings: build.mutation<PayrollSettings, { minuteRate: number; currency: string; bonusTiers: BonusTier[] }>({
      query: (body) => ({ url: '/payroll/settings', method: 'PUT', body }),
      invalidatesTags: ['Payroll'],
    }),
    getEarnings: build.query<PayrollReport, Range>({
      query: (params) => ({ url: '/payroll/earnings', params }),
      // Üretim bildirildikçe kazanç da değişir.
      providesTags: ['Payroll', 'ProductionTracking'],
    }),
    getOperatorEarnings: build.query<OperatorEarningsDetail, Range & { operatorId: number }>({
      query: ({ operatorId, ...params }) => ({ url: `/payroll/earnings/${operatorId}`, params }),
      providesTags: ['Payroll', 'ProductionTracking'],
    }),
  }),
})

export const {
  useGetPayrollSettingsQuery,
  useSavePayrollSettingsMutation,
  useGetEarningsQuery,
  useGetOperatorEarningsQuery,
} = payrollApi
