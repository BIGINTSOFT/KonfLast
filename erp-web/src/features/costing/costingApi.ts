import { baseApi } from '../../api/baseApi'
import type { StockItemType, StockUnit } from '../stock/stockApi'

export interface ProductMaterial {
  id: number
  stockItemId: number
  itemCode: string
  itemName: string
  itemType: StockItemType
  unit: StockUnit
  /** Satır yalnızca bu renkteki parçalar için geçerliyse renk; hepsi için boş. */
  colorId: number | null
  colorName: string | null
  sizeId: number | null
  sizeName: string | null
  /** Bir parça için gereken miktar, fire hariç. */
  quantityPerPiece: number
  wastePercent: number
  unitCost: number | null
  /** Fire dahil, bir parçaya düşen malzeme maliyeti. */
  costPerPiece: number | null
}

export interface ProductMaterials {
  productId: number
  productCode: string
  productName: string
  currency: string
  lines: ProductMaterial[]
}

export interface ProductMaterialRequest {
  stockItemId: number
  colorId: number | null
  sizeId: number | null
  quantityPerPiece: number
  wastePercent: number
}

export interface OrderMaterialLine {
  stockItemId: number
  itemCode: string
  itemName: string
  unit: StockUnit
  /** Siparişin tamamı için gereken miktar (fire dahil). */
  required: number
  /** Açılan demetler için depodan düşülmüş net miktar. */
  issued: number
  /** Henüz demetlenmemiş adet için gereken miktar. */
  remainingNeed: number
  /** Depodaki mevcut stok. */
  available: number
  /** Kalan ihtiyacın karşılanamayan kısmı (başka siparişlere ayrılmış stok sayılmaz). */
  shortage: number
  /** Depoda bu siparişe ayrılmış miktar. */
  reserved: number
  reservedByOthers: number
  /** Bu sipariş için açılmış satın alma siparişlerinde teslim alınmayı bekleyen miktar. */
  onOrder: number
  purchases: OrderMaterialPurchase[] | null
  /** Malzemenin bütün depolardaki stoğu. */
  stocks: { warehouseId: number; warehouseCode: string; warehouseName: string; onHand: number; reserved: number }[] | null
}

export interface OrderMaterialPurchase {
  purchaseOrderId: number
  orderNo: string
  status: string
  quantity: number
  remaining: number
  expectedDate: string | null
}

export interface PickListLine {
  locationCode: string
  lotNo: string | null
  batchNo: string | null
  shade: string | null
  /** Bu adresten (toptan) alınacak miktar. */
  quantity: number
  onHand: number
  /** Alınacağı depo: seçilen depoda yetmeyen kısım öteki hammadde depolarından gelir. */
  warehouseId: number
  warehouseCode: string
  warehouseName: string
}

export interface PickListItem {
  stockItemId: number
  itemCode: string
  itemName: string
  unit: StockUnit
  need: number
  shortage: number
  lines: PickListLine[]
}

export interface PickList {
  productionOrderId: number
  orderNo: string
  productCode: string
  productName: string
  warehouseId: number
  warehouseCode: string
  warehouseName: string
  items: PickListItem[]
}

export interface OrderMaterials {
  productionOrderId: number
  hasRecipe: boolean
  /** Stokların okunduğu depo; belirlenemediyse boş (stok tüm depoların toplamıdır). */
  warehouseId: number | null
  warehouseName: string | null
  lines: OrderMaterialLine[]
}

export interface OrderCostMaterial {
  stockItemId: number
  itemCode: string
  itemName: string
  unit: StockUnit
  quantity: number
  amount: number | null
}

export interface OrderCost {
  productionOrderId: number
  currency: string
  orderedQuantity: number
  bundledQuantity: number
  finishedQuantity: number
  materialCost: number
  laborCost: number
  totalCost: number
  costPerFinishedPiece: number | null
  standardMaterialCostPerPiece: number | null
  standardLaborCostPerPiece: number | null
  standardCostPerPiece: number | null
  hasUncostedMaterials: boolean
  hasUnpricedLabor: boolean
  materials: OrderCostMaterial[]
}

/** Ürünün parça başı standart maliyeti: etkin rotadan işçilik, reçetenin genel satırlarından malzeme. */
export interface ProductCostSummary {
  productId: number
  currency: string
  routeVersion: number | null
  operationCount: number
  totalSam: number
  laborCost: number | null
  laborUnpriced: boolean
  materialLineCount: number
  materialCost: number | null
  materialUncosted: boolean
  hasVariantMaterials: boolean
  totalCost: number | null
  listPrice: number | null
  listPriceCurrency: string
}

export const costingApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    /** Reçeteye eklenebilecek malzemeler (mamul olmayan stok kartları). */
    getMaterialOptions: build.query<
      { id: number; code: string; name: string; unit: StockUnit; unitCost: number | null; isActive: boolean }[],
      void
    >({
      query: () => '/product-material-options',
      providesTags: ['MasterData', 'Stock'],
    }),
    getProductMaterials: build.query<ProductMaterials, number>({
      query: (productId) => `/products/${productId}/materials`,
      // Malzemenin adı / maliyeti değişince reçete de tazelenir.
      providesTags: ['Material', 'MasterData'],
    }),
    saveProductMaterials: build.mutation<ProductMaterials, { productId: number; lines: ProductMaterialRequest[] }>({
      query: ({ productId, lines }) => ({ url: `/products/${productId}/materials`, method: 'PUT', body: { lines } }),
      invalidatesTags: ['Material'],
    }),
    getProductCostSummary: build.query<ProductCostSummary, number>({
      query: (productId) => `/products/${productId}/cost-summary`,
      providesTags: ['Material', 'MasterData', 'ProductionRoute', 'Payroll'],
    }),
    getOrderMaterials: build.query<OrderMaterials, { orderId: number; warehouseId?: number }>({
      query: ({ orderId, warehouseId }) => ({
        url: `/production-orders/${orderId}/materials`,
        params: warehouseId === undefined ? undefined : { warehouseId },
      }),
      // Sipariş onaylanınca malzemesi kendiliğinden ayrılır: sipariş değişince de tazelenir.
      providesTags: ['Material', 'Bundle', 'Stock', 'ProductionOrder', 'PurchaseOrder'],
    }),
    reserveOrderMaterials: build.mutation<OrderMaterials, { orderId: number; warehouseId?: number }>({
      query: ({ orderId, warehouseId }) => ({
        url: `/production-orders/${orderId}/materials/reserve`,
        method: 'POST',
        params: warehouseId === undefined ? undefined : { warehouseId },
      }),
      // İlk seçilen depo siparişin malzeme deposu olur: sipariş de tazelenir.
      invalidatesTags: ['Stock', 'ProductionOrder'],
    }),
    releaseOrderMaterials: build.mutation<OrderMaterials, { orderId: number; warehouseId?: number }>({
      query: ({ orderId, warehouseId }) => ({
        url: `/production-orders/${orderId}/materials/release`,
        method: 'POST',
        params: warehouseId === undefined ? undefined : { warehouseId },
      }),
      invalidatesTags: ['Stock'],
    }),
    getPickList: build.query<PickList, { orderId: number; warehouseId?: number }>({
      query: ({ orderId, warehouseId }) => ({
        url: `/production-orders/${orderId}/pick-list`,
        params: warehouseId === undefined ? undefined : { warehouseId },
      }),
      providesTags: ['Material', 'Bundle', 'Stock', 'ProductionOrder'],
    }),
    getOrderCost: build.query<OrderCost, number>({
      query: (orderId) => `/production-orders/${orderId}/cost`,
      providesTags: ['Material', 'Bundle', 'Stock', 'ProductionTracking', 'Payroll'],
    }),
  }),
})

export const {
  useGetProductCostSummaryQuery,
  useGetMaterialOptionsQuery,
  useGetProductMaterialsQuery,
  useSaveProductMaterialsMutation,
  useGetOrderMaterialsQuery,
  useReserveOrderMaterialsMutation,
  useReleaseOrderMaterialsMutation,
  useGetPickListQuery,
  useGetOrderCostQuery,
} = costingApi
