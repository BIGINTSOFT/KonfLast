import { baseApi } from '../../api/baseApi'
import type { PagedResult } from '../../types/api'
import type { DokumaMaterialUnit } from './dokumaWarpApi'

export type DokumaMaterialType = 'SIZING_CHEMICAL' | 'SPARE_PART' | 'CONSUMABLE' | 'OTHER'
export type DokumaMaterialMovementType = 'RECEIPT' | 'ISSUE' | 'SIZING_CONSUMPTION' | 'ADJUSTMENT' | 'COUNT'

export const dokumaMaterialMovementLabels: Record<DokumaMaterialMovementType, string> = {
  RECEIPT: 'Giriş',
  ISSUE: 'Çıkış',
  SIZING_CONSUMPTION: 'Haşıl tüketimi',
  ADJUSTMENT: 'Lot düzeltmesi',
  COUNT: 'Sayım farkı',
}

export interface DokumaMaterialStockRow {
  materialId: number
  code: string
  name: string
  type: DokumaMaterialType
  unit: DokumaMaterialUnit
  minimumStock: number | null
  quantity: number
  /** Açık satın alma siparişlerinde (taslak dahil) teslim beklenen. */
  onOrderQuantity: number
  lotCount: number
  isCritical: boolean
  preferredSupplierName: string | null
  isActive: boolean
}

export interface DokumaMaterialLot {
  id: number
  lotNo: string
  materialId: number
  materialCode: string
  materialName: string
  unit: DokumaMaterialUnit
  receiptDate: string
  supplierId: number | null
  supplierName: string | null
  supplierLotNo: string | null
  despatchNo: string | null
  quantity: number
  remainingQuantity: number
  unitPrice: number | null
  currency: string
  purchaseOrderId: number | null
  purchaseOrderNo: string | null
  notes: string | null
  /** Çıkış ya da düzeltme yapıldı: malzemesi ve miktarı değişmez, silinmez. */
  isUsed: boolean
}

export interface DokumaMaterialLotRequest {
  materialId: number
  receiptDate: string
  quantity: number
  supplierId: number | null
  supplierLotNo: string | null
  despatchNo: string | null
  unitPrice: number | null
  currency: string | null
  notes: string | null
}

export interface DokumaMaterialMovement {
  id: number
  movementDate: string
  type: DokumaMaterialMovementType
  materialId: number
  materialCode: string
  materialName: string
  unit: DokumaMaterialUnit
  lotNo: string
  quantity: number
  loomCode: string | null
  department: string | null
  reason: string | null
  beamId: number | null
  beamNo: string | null
  reference: string | null
  createdAt: string
  createdBy: string | null
}

export interface DokumaSizingRecipeListItem {
  id: number
  code: string
  name: string
  articleId: number
  articleCode: string
  articleName: string
  isDefault: boolean
  concentrationPercent: number | null
  targetPickUpPercent: number | null
  lineCount: number
  isActive: boolean
}

export interface DokumaSizingRecipe {
  id: number
  code: string
  name: string
  articleId: number
  articleCode: string
  articleName: string
  isDefault: boolean
  concentrationPercent: number | null
  bathTemperatureC: number | null
  viscositySeconds: number | null
  machineSpeedMetersPerMinute: number | null
  targetPickUpPercent: number | null
  notes: string | null
  isActive: boolean
  lines: { id: number; sequence: number; materialId: number; materialCode: string; materialName: string; unit: DokumaMaterialUnit; bathPercent: number }[]
}

export interface DokumaSizingRecipeRequest {
  code: string
  name: string
  articleId: number
  isDefault: boolean
  concentrationPercent: number | null
  bathTemperatureC: number | null
  viscositySeconds: number | null
  machineSpeedMetersPerMinute: number | null
  targetPickUpPercent: number | null
  notes: string | null
  isActive: boolean
  lines: { materialId: number; bathPercent: number }[]
}

// Malzeme stoğu iplik girişleriyle aynı önbellek etiketini kullanır: satın alma teslimi ikisini birlikte değiştirir.
const stockTags = ['DokumaYarnLot'] as const

export const dokumaStockApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getDokumaMaterialStock: build.query<DokumaMaterialStockRow[], { search?: string; criticalOnly?: boolean; includeInactive?: boolean }>({
      query: (params) => ({ url: '/dokuma-material-stock', params }),
      providesTags: [...stockTags, 'MasterData'],
    }),
    getDokumaMaterialLots: build.query<PagedResult<DokumaMaterialLot>, Record<string, unknown>>({
      query: (params) => ({ url: '/dokuma-material-stock/lots', params }),
      providesTags: [...stockTags],
    }),
    getDokumaMaterialMovements: build.query<PagedResult<DokumaMaterialMovement>, Record<string, unknown>>({
      query: (params) => ({ url: '/dokuma-material-stock/movements', params }),
      providesTags: [...stockTags],
    }),
    saveDokumaMaterialLot: build.mutation<DokumaMaterialLot, { id?: number; body: DokumaMaterialLotRequest }>({
      query: ({ id, body }) => ({ url: id === undefined ? '/dokuma-material-stock/lots' : `/dokuma-material-stock/lots/${id}`, method: id === undefined ? 'POST' : 'PUT', body }),
      invalidatesTags: [...stockTags],
    }),
    deleteDokumaMaterialLot: build.mutation<null, number>({
      query: (id) => ({ url: `/dokuma-material-stock/lots/${id}`, method: 'DELETE' }),
      invalidatesTags: [...stockTags],
    }),
    adjustDokumaMaterialLot: build.mutation<DokumaMaterialLot, { id: number; countedQuantity: number; reason: string; date: string | null }>({
      query: ({ id, ...body }) => ({ url: `/dokuma-material-stock/lots/${id}/adjust`, method: 'POST', body }),
      invalidatesTags: [...stockTags],
    }),
    issueDokumaMaterial: build.mutation<null, { materialId: number; quantity: number; date: string; reason: string; loomId: number | null; department: string | null }>({
      query: (body) => ({ url: '/dokuma-material-stock/issues', method: 'POST', body }),
      invalidatesTags: [...stockTags],
    }),
    countDokumaMaterial: build.mutation<null, { materialId: number; countedQuantity: number; date: string; reason: string | null }>({
      query: (body) => ({ url: '/dokuma-material-stock/counts', method: 'POST', body }),
      invalidatesTags: [...stockTags],
    }),
    getDokumaSizingRecipes: build.query<PagedResult<DokumaSizingRecipeListItem>, Record<string, unknown>>({
      query: (params) => ({ url: '/dokuma-sizing-recipes', params }),
      providesTags: ['MasterData'],
    }),
    getDokumaSizingRecipe: build.query<DokumaSizingRecipe, number>({
      query: (id) => `/dokuma-sizing-recipes/${id}`,
      providesTags: ['MasterData'],
    }),
    saveDokumaSizingRecipe: build.mutation<DokumaSizingRecipe, { id?: number; body: DokumaSizingRecipeRequest }>({
      query: ({ id, body }) => ({ url: id === undefined ? '/dokuma-sizing-recipes' : `/dokuma-sizing-recipes/${id}`, method: id === undefined ? 'POST' : 'PUT', body }),
      invalidatesTags: ['MasterData'],
    }),
    deleteDokumaSizingRecipe: build.mutation<null, number>({
      query: (id) => ({ url: `/dokuma-sizing-recipes/${id}`, method: 'DELETE' }),
      invalidatesTags: ['MasterData'],
    }),
  }),
})

export const {
  useGetDokumaMaterialStockQuery,
  useGetDokumaMaterialLotsQuery,
  useGetDokumaMaterialMovementsQuery,
  useSaveDokumaMaterialLotMutation,
  useDeleteDokumaMaterialLotMutation,
  useAdjustDokumaMaterialLotMutation,
  useIssueDokumaMaterialMutation,
  useCountDokumaMaterialMutation,
  useGetDokumaSizingRecipesQuery,
  useGetDokumaSizingRecipeQuery,
  useSaveDokumaSizingRecipeMutation,
  useDeleteDokumaSizingRecipeMutation,
} = dokumaStockApi
