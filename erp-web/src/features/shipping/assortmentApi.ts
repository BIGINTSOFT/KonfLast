import { baseApi } from '../../api/baseApi'
import type { GridParams } from '../../components/grid/useGrid'
import type { PagedResult } from '../../types/api'

export interface AssortmentTemplateListItem {
  id: number
  code: string
  name: string
  customerId: number | null
  /** Boşsa genel şablon. */
  customerName: string | null
  sizeCount: number
  /** Bir koliye giren toplam adet. */
  piecesPerCarton: number
  isActive: boolean
}

export interface AssortmentTemplateLine {
  id: number
  sizeId: number
  sizeCode: string
  sizeName: string
  sortOrder: number
  ratio: number
}

export interface AssortmentTemplate {
  id: number
  code: string
  name: string
  customerId: number | null
  customerCode: string | null
  customerName: string | null
  notes: string | null
  isActive: boolean
  piecesPerCarton: number
  createdAt: string
  createdBy: string | null
  updatedAt: string | null
  updatedBy: string | null
  lines: AssortmentTemplateLine[]
}

export interface AssortmentTemplateRequest {
  code: string
  name: string
  customerId: number | null
  notes: string | null
  isActive: boolean
  lines: { sizeId: number; ratio: number }[]
}

/** Koli dağıtımında seçilecek şablon; dağılım "S:1 M:2 L:2" biçimindedir. */
export interface AssortmentTemplateOption {
  id: number
  code: string
  name: string
  customerName: string | null
  piecesPerCarton: number
  distribution: string
}

interface ListParams {
  page: number
  pageSize: number
  search?: string
  customerId?: number
}

// Şablonlar tanım verisi gibi davranır: MasterData etiketiyle tazelenir.
export const assortmentApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getAssortmentTemplates: build.query<PagedResult<AssortmentTemplateListItem>, ListParams & GridParams>({
      query: (params) => ({ url: '/assortment-templates', params }),
      providesTags: ['MasterData'],
    }),
    getAssortmentTemplate: build.query<AssortmentTemplate, number>({
      query: (id) => `/assortment-templates/${id}`,
      providesTags: ['MasterData'],
    }),
    getAssortmentTemplateOptions: build.query<AssortmentTemplateOption[], { customerId?: number }>({
      query: (params) => ({ url: '/assortment-templates/options', params }),
      providesTags: ['MasterData'],
    }),
    saveAssortmentTemplate: build.mutation<AssortmentTemplate, { id?: number; body: AssortmentTemplateRequest }>({
      query: ({ id, body }) => ({
        url: id ? `/assortment-templates/${id}` : '/assortment-templates',
        method: id ? 'PUT' : 'POST',
        body,
      }),
      invalidatesTags: ['MasterData'],
    }),
    deleteAssortmentTemplate: build.mutation<void, number>({
      query: (id) => ({ url: `/assortment-templates/${id}`, method: 'DELETE' }),
      invalidatesTags: ['MasterData'],
    }),
  }),
})

export const {
  useGetAssortmentTemplatesQuery,
  useGetAssortmentTemplateQuery,
  useGetAssortmentTemplateOptionsQuery,
  useSaveAssortmentTemplateMutation,
  useDeleteAssortmentTemplateMutation,
} = assortmentApi
