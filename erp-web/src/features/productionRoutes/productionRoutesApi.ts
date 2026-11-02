import { baseApi } from '../../api/baseApi'
import type { GridParams } from '../../components/grid/useGrid'
import type { PagedResult } from '../../types/api'

export type RouteStatus = 'DRAFT' | 'ACTIVE' | 'ARCHIVED'

export interface ProductionRouteListItem {
  id: number
  modelId: number
  modelCode: string
  modelName: string
  version: number
  name: string | null
  status: RouteStatus
  operationCount: number
  totalSam: number
}

export interface ProductionRouteOperation {
  id: number
  sequenceNo: number
  operationId: number
  operationCode: string
  operationName: string
  machineTypeName: string | null
  sam: number
  /** Önce tamamlanması gereken adımların sıra numaraları. */
  predecessors: number[]
  /** Akıştaki aşama (0'dan başlar); aynı aşamadaki adımlar paralel yürür. Sunucu hesaplar. */
  stage: number
  /** Adım fasonda (dışarıda) yapılır. */
  isSubcontracted: boolean
  /** Fason adımın varsayılan fasoncusu. */
  subcontractorId: number | null
  subcontractorName: string | null
}

export interface ProductionRoute {
  id: number
  modelId: number
  modelCode: string
  modelName: string
  version: number
  name: string | null
  description: string | null
  status: RouteStatus
  isEditable: boolean
  canActivate: boolean
  canArchive: boolean
  totalSam: number
  rowVersion: string
  createdAt: string
  createdBy: string | null
  operations: ProductionRouteOperation[]
}

export type ProductionRouteListParams = {
  page: number
  pageSize: number
  search?: string
  status?: RouteStatus
} & GridParams

export interface ProductionRouteRequest {
  modelId: number
  name: string | null
  description: string | null
  operations: {
    sequenceNo: number
    operationId: number
    sam: number | null
    predecessors: number[]
    isSubcontracted: boolean
    subcontractorId: number | null
  }[]
  rowVersion?: string
}

export const productionRoutesApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getProductionRoutes: build.query<PagedResult<ProductionRouteListItem>, ProductionRouteListParams>({
      query: (params) => ({ url: '/production-routes', params }),
      providesTags: ['ProductionRoute'],
    }),
    getProductionRoute: build.query<ProductionRoute, number>({
      query: (id) => `/production-routes/${id}`,
      providesTags: ['ProductionRoute'],
    }),
    createProductionRoute: build.mutation<ProductionRoute, ProductionRouteRequest>({
      query: (body) => ({ url: '/production-routes', method: 'POST', body }),
      invalidatesTags: ['ProductionRoute'],
    }),
    updateProductionRoute: build.mutation<ProductionRoute, { id: number; body: ProductionRouteRequest }>({
      query: ({ id, body }) => ({ url: `/production-routes/${id}`, method: 'PUT', body }),
      invalidatesTags: ['ProductionRoute'],
    }),
    changeProductionRouteStatus: build.mutation<
      ProductionRoute,
      { id: number; action: 'activate' | 'archive'; rowVersion: string }
    >({
      query: ({ id, action, rowVersion }) => ({
        url: `/production-routes/${id}/${action}`,
        method: 'POST',
        body: { rowVersion },
      }),
      // Etkin rota, siparişin demetlenebilirliğini ve planlamadaki "rotası var" bilgisini de değiştirir.
      invalidatesTags: ['ProductionRoute', 'Bundle', 'Planning'],
    }),
    copyProductionRoute: build.mutation<ProductionRoute, number>({
      query: (id) => ({ url: `/production-routes/${id}/copy`, method: 'POST' }),
      invalidatesTags: ['ProductionRoute'],
    }),
    deleteProductionRoute: build.mutation<null, number>({
      query: (id) => ({ url: `/production-routes/${id}`, method: 'DELETE' }),
      invalidatesTags: ['ProductionRoute'],
    }),
    /** Adımın fason ayarı; akışı ve SAM'i değiştirmediği için etkin rotada da yapılır. */
    setProductionRouteStepSubcontract: build.mutation<
      ProductionRoute,
      { id: number; routeOperationId: number; isSubcontracted: boolean; subcontractorId: number | null }
    >({
      query: ({ id, routeOperationId, ...body }) => ({
        url: `/production-routes/${id}/operations/${routeOperationId}/subcontract`,
        method: 'PUT',
        body,
      }),
      invalidatesTags: ['ProductionRoute', 'Subcontract'],
    }),
  }),
})

export const {
  useGetProductionRoutesQuery,
  useGetProductionRouteQuery,
  useCreateProductionRouteMutation,
  useUpdateProductionRouteMutation,
  useChangeProductionRouteStatusMutation,
  useCopyProductionRouteMutation,
  useDeleteProductionRouteMutation,
  useSetProductionRouteStepSubcontractMutation,
} = productionRoutesApi
