import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react'
import type { BaseQueryFn, FetchArgs } from '@reduxjs/toolkit/query/react'
import { Mutex } from 'async-mutex'
import { refreshSession } from '../services/session'
import type { RootState } from '../store'
import type { ApiEnvelope, RequestError } from '../types/api'
import { API_BASE_URL } from './config'
import { toRequestError } from './envelope'

const rawBaseQuery = fetchBaseQuery({
  baseUrl: API_BASE_URL,
  prepareHeaders: (headers, { getState }) => {
    const token = (getState() as RootState).auth.accessToken
    if (token) {
      headers.set('Authorization', `Bearer ${token}`)
    }
    headers.set('X-Correlation-Id', crypto.randomUUID().replaceAll('-', ''))
    return headers
  },
})

// Aynı anda 401 alan istekler tek bir token yenilemesini bekler.
const refreshMutex = new Mutex()

const isAuthEndpoint = (args: string | FetchArgs) => {
  const url = typeof args === 'string' ? args : args.url
  return url.startsWith('/auth/login') || url.startsWith('/auth/refresh')
}

/**
 * Yanıt zarfını açar (data → sonuç, error → RequestError) ve access token'ın süresi
 * dolduğunda oturumu bir kez yenileyip isteği tekrarlar.
 */
const baseQuery: BaseQueryFn<string | FetchArgs, unknown, RequestError> = async (args, api, extraOptions) => {
  await refreshMutex.waitForUnlock()
  let result = await rawBaseQuery(args, api, extraOptions)

  if (result.error?.status === 401 && !isAuthEndpoint(args)) {
    if (refreshMutex.isLocked()) {
      await refreshMutex.waitForUnlock()
    } else {
      await refreshMutex.runExclusive(() => refreshSession(api.dispatch))
    }

    if ((api.getState() as RootState).auth.accessToken) {
      result = await rawBaseQuery(args, api, extraOptions)
    }
  }

  if (result.error) {
    const body = 'data' in result.error ? result.error.data : undefined
    return { error: toRequestError(result.error.status, body) }
  }

  return { data: (result.data as ApiEnvelope<unknown>).data }
}

/** Önbellek etiketleri; sekme yenilemede bütün veriler bunlarla geçersiz kılınır. */
export const apiTags = ['Role', 'User', 'MasterData', 'ProductionOrder', 'ProductionRoute', 'ProductionLine', 'MachineStatus', 'MachineOperation', 'Bundle', 'WorkSession', 'OperatorQualification', 'OperatorPhoto', 'ProductionTracking', 'Quality', 'LiveDashboard', 'Stock', 'Planning', 'PurchaseOrder', 'Shipment', 'Subcontract', 'Quotation', 'CuttingPlan', 'Invoice', 'Integration', 'Payroll', 'Material', 'Workspace', 'CustomerAddress', 'CustomerBuyer', 'ExchangeRate', 'ApprovalFlow', 'PriceList', 'CustomerDiscount', 'Shortcut', 'StockCount', 'SalesOrder', 'BoyahaneReceipt', 'BoyahaneInspection', 'BoyahaneOrder', 'BoyahaneLab', 'BoyahaneBatch', 'DokumaOrder', 'DokumaYarnLot', 'DokumaWarp', 'DokumaWeaving', 'DokumaReport', 'OrmeOrder', 'OrmeYarnLot', 'OrmeKnitOrder', 'OrmeShipping', 'OrmeReport', 'DokumaShipping'] as const

export const baseApi = createApi({
  reducerPath: 'api',
  baseQuery,
  tagTypes: apiTags,
  // Ekran kurulurken verisi sunucudan yeniden istenir: sekme yenileme (F5) yalnızca o sekmenin ekranını yeniden
  // kurarak verisini tazeler. Açık sekmeler arka planda kurulu kaldığı için sekme değiştirmek istek atmaz.
  refetchOnMountOrArgChange: true,
  endpoints: () => ({}),
})
