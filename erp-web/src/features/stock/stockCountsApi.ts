import { baseApi } from '../../api/baseApi'
import type { GridParams } from '../../components/grid/useGrid'
import type { PagedResult } from '../../types/api'
import type { StockUnit } from './stockApi'

export type StockCountStatus = 'OPEN' | 'POSTED' | 'CANCELLED'

export const stockCountStatusPresentation: Record<StockCountStatus, { label: string; tone: string }> = {
  OPEN: { label: 'Sayım sürüyor', tone: 'warning' },
  POSTED: { label: 'Onaylandı', tone: 'ok' },
  CANCELLED: { label: 'İptal', tone: 'neutral' },
}

export interface StockCountListItem {
  id: number
  countNo: string
  warehouseId: number
  warehouseCode: string
  warehouseName: string
  /** Yalnızca bir adres sayılıyorsa; boşsa deponun tamamı. */
  locationCode: string | null
  status: StockCountStatus
  lineCount: number
  countedLineCount: number
  differenceLineCount: number
  createdAt: string
  createdBy: string | null
  postedAt: string | null
}

export interface StockCountLine {
  id: number
  locationId: number
  locationCode: string
  stockItemId: number
  itemCode: string
  itemName: string
  unit: StockUnit
  lotId: number | null
  lotNo: string | null
  batchNo: string | null
  expectedQuantity: number
  /** Sayılan miktar; henüz sayılmadıysa boş. */
  countedQuantity: number | null
  difference: number | null
}

export interface StockCount {
  id: number
  countNo: string
  warehouseId: number
  warehouseCode: string
  warehouseName: string
  locationId: number | null
  locationCode: string | null
  status: StockCountStatus
  note: string | null
  isOpen: boolean
  /** Kayıt, onay ve iptalde geri gönderilir. */
  rowVersion: string
  createdAt: string
  createdBy: string | null
  postedAt: string | null
  postedBy: string | null
  lines: StockCountLine[]
}

export interface StockCountListParams {
  page: number
  pageSize: number
  warehouseId?: number
  status?: StockCountStatus
  search?: string
}

// Onay stoğu değiştirir; açık sayım stok hareketlerini kilitler: stok listeleri de tazelenir.
export const stockCountsApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getStockCounts: build.query<PagedResult<StockCountListItem>, StockCountListParams & GridParams>({
      query: (params) => ({ url: '/stock-counts', params }),
      providesTags: ['StockCount'],
    }),
    getStockCount: build.query<StockCount, number>({
      query: (id) => `/stock-counts/${id}`,
      providesTags: ['StockCount'],
    }),
    openStockCount: build.mutation<StockCount, { warehouseId: number; locationId: number | null; note: string | null }>({
      query: (body) => ({ url: '/stock-counts', method: 'POST', body }),
      invalidatesTags: ['StockCount'],
    }),
    saveStockCountLines: build.mutation<
      StockCount,
      { id: number; rowVersion: string; lines: { lineId: number; countedQuantity: number | null }[] }
    >({
      query: ({ id, ...body }) => ({ url: `/stock-counts/${id}/lines`, method: 'PUT', body }),
      invalidatesTags: ['StockCount'],
    }),
    addStockCountLine: build.mutation<
      StockCount,
      { id: number; rowVersion: string; stockItemId: number; countedQuantity: number; locationId: number | null; lotId: number | null }
    >({
      query: ({ id, ...body }) => ({ url: `/stock-counts/${id}/lines`, method: 'POST', body }),
      invalidatesTags: ['StockCount'],
    }),
    postStockCount: build.mutation<StockCount, { id: number; rowVersion: string }>({
      query: ({ id, rowVersion }) => ({ url: `/stock-counts/${id}/post`, method: 'POST', body: { rowVersion } }),
      invalidatesTags: ['StockCount', 'Stock'],
    }),
    cancelStockCount: build.mutation<StockCount, { id: number; rowVersion: string }>({
      query: ({ id, rowVersion }) => ({ url: `/stock-counts/${id}/cancel`, method: 'POST', body: { rowVersion } }),
      invalidatesTags: ['StockCount'],
    }),
  }),
})

export const {
  useGetStockCountsQuery,
  useGetStockCountQuery,
  useOpenStockCountMutation,
  useSaveStockCountLinesMutation,
  useAddStockCountLineMutation,
  usePostStockCountMutation,
  useCancelStockCountMutation,
} = stockCountsApi
