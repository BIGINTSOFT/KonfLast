import { baseApi } from '../../api/baseApi'
import type { GridParams } from '../../components/grid/useGrid'
import type { PagedResult } from '../../types/api'

export type IntegrationDeliveryStatus = 'PENDING' | 'SUCCEEDED' | 'FAILED'

export interface IntegrationMessageListItem {
  /** Sıra numarası: olaylar bununla sıralanır. */
  id: number
  eventId: string
  eventType: string
  entityName: string
  entityId: string
  reference: string | null
  occurredAt: string
  createdBy: string
  pendingCount: number
  succeededCount: number
  failedCount: number
}

export interface IntegrationDelivery {
  id: number
  channel: string
  channelName: string
  status: IntegrationDeliveryStatus
  attemptCount: number
  /** Yalnızca kuyruktaki teslimde doludur. */
  nextAttemptAt: string | null
  lastAttemptAt: string | null
  lastError: string | null
  deliveredAt: string | null
  /** Yeniden kuyruğa alınabilir mi; sunucu belirler. */
  canRetry: boolean
}

export interface IntegrationMessage extends Omit<IntegrationMessageListItem, 'pendingCount' | 'succeededCount' | 'failedCount'> {
  /** Olayın dış sistemlere giden verisi. */
  data: unknown
  deliveries: IntegrationDelivery[]
}

export interface IntegrationChannel {
  key: string
  name: string
  target: string | null
  /** Boşsa tüm olaylar. */
  eventTypes: string[]
  /** Yapılandırmadan kaldırılmış kanalın yalnızca eski teslimleri görünür. */
  isConfigured: boolean
  pendingCount: number
  succeededCount: number
  failedCount: number
}

export interface IntegrationEventType {
  type: string
  label: string
}

export interface IntegrationOverview {
  dispatchEnabled: boolean
  maxAttempts: number
  channels: IntegrationChannel[]
  eventTypes: IntegrationEventType[]
}

export type IntegrationMessageParams = {
  page: number
  pageSize: number
  search?: string
  eventType?: string
  deliveryStatus?: IntegrationDeliveryStatus
} & GridParams

// Teslimler arka planda ilerler; ekran her gelişte ve "Yenile" ile güncel durumu okur.
export const integrationsApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getIntegrationOverview: build.query<IntegrationOverview, void>({
      query: () => '/integrations/overview',
      providesTags: ['Integration'],
      keepUnusedDataFor: 0,
    }),
    getIntegrationMessages: build.query<PagedResult<IntegrationMessageListItem>, IntegrationMessageParams>({
      query: (params) => ({ url: '/integrations/messages', params }),
      providesTags: ['Integration'],
      keepUnusedDataFor: 0,
    }),
    getIntegrationMessage: build.query<IntegrationMessage, number>({
      query: (id) => `/integrations/messages/${id}`,
      providesTags: ['Integration'],
      keepUnusedDataFor: 0,
    }),
    retryIntegrationDelivery: build.mutation<IntegrationMessage, number>({
      query: (deliveryId) => ({ url: `/integrations/deliveries/${deliveryId}/retry`, method: 'POST' }),
      invalidatesTags: ['Integration'],
    }),
  }),
})

export const {
  useGetIntegrationOverviewQuery,
  useGetIntegrationMessagesQuery,
  useGetIntegrationMessageQuery,
  useRetryIntegrationDeliveryMutation,
} = integrationsApi
