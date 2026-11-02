import { baseApi } from '../../api/baseApi'
import type { GridParams } from '../../components/grid/useGrid'
import type { AuditAction, AuditLog, PagedResult } from '../../types/api'

export type AuditLogParams = {
  page: number
  pageSize: number
  entityName?: string
  entityId?: string
  userName?: string
  action?: AuditAction
  from?: string
  to?: string
} & GridParams

export const auditLogsApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getAuditLogs: build.query<PagedResult<AuditLog>, AuditLogParams>({
      query: (params) => ({ url: '/audit-logs', params }),
      // Kayıtlar sürekli eklenir; ekrana her gelişte güncel liste gösterilsin.
      keepUnusedDataFor: 0,
    }),
  }),
})

export const { useGetAuditLogsQuery } = auditLogsApi
