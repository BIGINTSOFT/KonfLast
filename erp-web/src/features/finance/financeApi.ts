import { baseApi } from '../../api/baseApi'

export type ExchangeRateSource = 'TCMB' | 'MANUAL'

export interface ExchangeRateRow {
  currencyId: number
  code: string
  name: string
  fetchFromTcmb: boolean
  id: number | null
  /** Gösterilen kurun tarihi; istenen günde kur yoksa önceki son kurun tarihi. */
  rateDate: string | null
  forexBuying: number | null
  forexSelling: number | null
  banknoteBuying: number | null
  banknoteSelling: number | null
  source: ExchangeRateSource | null
  isCarriedOver: boolean
  updatedAt: string | null
  updatedBy: string | null
}

export interface ExchangeRateDay {
  date: string
  localCurrency: string
  rates: ExchangeRateRow[]
}

export interface ExchangeRateHistoryItem {
  id: number
  rateDate: string
  forexBuying: number
  forexSelling: number
  banknoteBuying: number | null
  banknoteSelling: number | null
  source: ExchangeRateSource
}

export interface ExchangeRateEntry {
  currencyId: number
  forexBuying: number
  forexSelling: number
  banknoteBuying: number | null
  banknoteSelling: number | null
}

export interface FetchExchangeRatesResult {
  bulletinDate: string | null
  imported: number
  skippedManual: string[]
  notListed: string[]
}

export const financeApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getExchangeRates: build.query<ExchangeRateDay, string>({
      query: (date) => ({ url: '/exchange-rates', params: { date } }),
      providesTags: ['ExchangeRate'],
    }),
    getExchangeRateHistory: build.query<ExchangeRateHistoryItem[], { currencyId: number; from: string; to: string }>({
      query: (params) => ({ url: '/exchange-rates/history', params }),
      providesTags: ['ExchangeRate'],
    }),
    saveExchangeRates: build.mutation<ExchangeRateDay, { date: string; rates: ExchangeRateEntry[] }>({
      query: (body) => ({ url: '/exchange-rates', method: 'PUT', body }),
      invalidatesTags: ['ExchangeRate'],
    }),
    fetchExchangeRates: build.mutation<FetchExchangeRatesResult, { date: string | null; overwriteManual: boolean }>({
      query: (body) => ({ url: '/exchange-rates/fetch', method: 'POST', body }),
      invalidatesTags: ['ExchangeRate'],
    }),
    deleteExchangeRate: build.mutation<void, number>({
      query: (id) => ({ url: `/exchange-rates/${id}`, method: 'DELETE' }),
      invalidatesTags: ['ExchangeRate'],
    }),
  }),
})

export const {
  useGetExchangeRatesQuery,
  useGetExchangeRateHistoryQuery,
  useSaveExchangeRatesMutation,
  useFetchExchangeRatesMutation,
  useDeleteExchangeRateMutation,
} = financeApi
