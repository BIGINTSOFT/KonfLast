import { baseApi } from '../../api/baseApi'
import type { GridParams } from '../../components/grid/useGrid'
import type { PagedResult } from '../../types/api'

export type StockItemType = 'FABRIC' | 'ACCESSORY' | 'FINISHED_GOOD' | 'OTHER'
export type StockUnit = 'PIECE' | 'METER' | 'KILOGRAM' | 'ROLL' | 'BOX'
export type StockTransactionType =
  | 'RECEIPT'
  | 'ISSUE'
  | 'TRANSFER_OUT'
  | 'TRANSFER_IN'
  | 'ADJUSTMENT'
  | 'PURCHASE_RECEIPT'
  | 'SHIPMENT'
  | 'PRODUCTION_RECEIPT'
  | 'PRODUCTION_ISSUE'
  | 'PRODUCTION_RETURN'
  | 'RELOCATION_OUT'
  | 'RELOCATION_IN'

export interface StockBalance {
  warehouseId: number
  warehouseCode: string
  warehouseName: string
  stockItemId: number
  itemCode: string
  itemName: string
  itemType: StockItemType
  unit: StockUnit
  quantity: number
  /** Top / parti takipli kart: çıkış, transfer ve sayım top seçilerek yapılır. */
  isLotTracked: boolean
  /** Depodaki stoğun iş emirlerine ayrılmış kısmı (yalnızca depo bazındaki listede dolu). */
  reserved?: number
}

/** Girişte açılacak topun özellikleri; hepsi isteğe bağlıdır. */
export interface LotAttributes {
  batchNo?: string | null
  shade?: string | null
  /** Kumaş eni (cm). */
  width?: number | null
  supplierRollNo?: string | null
}

/** Bir topun bir adresteki bakiyesi, topun özellikleriyle. */
export interface StockLotBalance {
  lotId: number
  /** Sistem top numarası; etikete basılır. */
  lotNo: string
  batchNo: string | null
  shade: string | null
  width: number | null
  supplierRollNo: string | null
  initialQuantity: number
  receivedAt: string
  purchaseOrderId: number | null
  purchaseOrderNo: string | null
  warehouseId: number
  warehouseCode: string
  warehouseName: string
  locationId: number
  locationCode: string
  stockItemId: number
  itemCode: string
  itemName: string
  itemType: StockItemType
  unit: StockUnit
  /** Topun bu adreste kalan miktarı. */
  quantity: number
}

/** Bir kartın bir depo adresindeki bakiyesi. */
export interface StockLocationBalance extends StockBalance {
  locationId: number
  locationCode: string
  /** Deponun genel adresi: adres belirtilmeyen hareketler buraya yazılır. */
  locationIsDefault: boolean
}

export interface LocationOption {
  id: number
  code: string
  description: string | null
  isDefault: boolean
  isActive: boolean
}

export interface StockTransaction {
  id: number
  type: StockTransactionType
  warehouseId: number
  warehouseCode: string
  warehouseName: string
  locationId: number
  locationCode: string
  stockItemId: number
  itemCode: string
  itemName: string
  unit: StockUnit
  /** Bakiyedeki değişim: girişte artı, çıkışta eksi. */
  quantity: number
  /** Hareketten sonra kartın depodaki bakiyesi (tüm adresler). */
  balanceAfter: number
  /** Hareketten sonra kartın bu adresteki bakiyesi. */
  locationBalanceAfter: number
  counterWarehouseName: string | null
  counterLocationCode: string | null
  lotId: number | null
  lotNo: string | null
  /** Hareketten sonra topun bu adresteki bakiyesi; takipsiz kartta boş. */
  lotBalanceAfter: number | null
  bundleId: number | null
  bundleNo: string | null
  productionOrderId: number | null
  orderNo: string | null
  purchaseOrderId: number | null
  purchaseOrderNo: string | null
  shipmentId: number | null
  shipmentNo: string | null
  documentNo: string | null
  note: string | null
  occurredAt: string
  createdBy: string
  /** Hareket anındaki birim maliyet; bilinmiyorsa boş. */
  unitCost: number | null
}

export interface StockMovement {
  /** İstek daha önce işlenmişti; stok ikinci kez değişmedi. */
  duplicate: boolean
  transactions: StockTransaction[]
  /** Üretimden teslim almada mal bir müşteri siparişine ayrıldıysa o sipariş ve adet. */
  reservedForOrderNo: string | null
  reservedQuantity: number
}

export interface SalesReservation {
  id: number
  salesOrderId: number
  orderNo: string
  customerOrderNo: string | null
  customerName: string
  dueDate: string
  quantity: number
  /** Siparişin kendi üretiminden teslim alınarak ayrıldı. */
  fromProduction: boolean
  updatedAt: string
  updatedBy: string
}

export interface ReservationCandidate {
  salesOrderLineId: number
  salesOrderId: number
  orderNo: string
  customerOrderNo: string | null
  customerName: string
  dueDate: string
  ordered: number
  shipped: number
  reserved: number
  /** Kalemin ayrılabilecek açık adedi. */
  open: number
}

export interface StockReservations {
  warehouseId: number
  warehouseCode: string
  stockItemId: number
  itemCode: string
  itemName: string
  unit: StockUnit
  onHand: number
  reserved: number
  /** Ayrılmamış stok. */
  free: number
  /** Kart hazır mamul; müşteri siparişine ayrılabilir. */
  canReserve: boolean
  salesReservations: SalesReservation[]
  materialReservations: { productionOrderId: number; orderNo: string; quantity: number }[]
  candidates: ReservationCandidate[]
}

export interface ReceivableBundle {
  bundleId: number
  bundleNo: string
  productionOrderId: number
  orderNo: string
  productCode: string
  productName: string
  colorName: string
  sizeName: string
  quantity: number
  /** Depoya girecek sağlam adet; sunucu hesaplar. */
  finishedQuantity: number
  completedAt: string | null
}

/** requestId istemcide bir kez üretilir; istek yinelenirse (ağ hatası) stok ikinci kez değişmez. */
interface MovementBase {
  requestId: string
  stockItemId: number
  note?: string | null
}

export interface ReceiptRequest extends MovementBase {
  warehouseId: number
  /** Verilmezse deponun genel adresi. */
  locationId?: number | null
  /** Takipli kartta açılacak topun özellikleri. */
  lot?: LotAttributes | null
  quantity: number
  documentNo?: string | null
}

export interface IssueRequest extends MovementBase {
  warehouseId: number
  locationId?: number | null
  /** Takipli kartta çıkılacak top. */
  lotId?: number | null
  quantity: number
  documentNo?: string | null
}

export interface TransferRequest extends MovementBase {
  fromWarehouseId: number
  /** İki depo aynıysa işlem depo içinde adres değişimidir. */
  toWarehouseId: number
  fromLocationId?: number | null
  toLocationId?: number | null
  lotId?: number | null
  quantity: number
}

export interface AdjustmentRequest extends MovementBase {
  warehouseId: number
  locationId?: number | null
  lotId?: number | null
  countedQuantity: number
  note: string
}

export interface BalanceParams {
  page: number
  pageSize: number
  warehouseId?: number
  itemType?: StockItemType
  search?: string
  includeZero?: boolean
}

export interface LotParams {
  page: number
  pageSize: number
  warehouseId?: number
  stockItemId?: number
  lotId?: number
  purchaseOrderId?: number
  search?: string
  includeZero?: boolean
}

export interface TransactionParams {
  page: number
  pageSize: number
  warehouseId?: number
  locationId?: number
  stockItemId?: number
  type?: StockTransactionType
  search?: string
  from?: string
  to?: string
}

// Üretimden teslim alma demeti kapatır: demet ve takip verileri de tazelenir.
export const stockApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getStockBalances: build.query<PagedResult<StockBalance>, BalanceParams & GridParams>({
      query: (params) => ({ url: '/stock', params }),
      providesTags: ['Stock'],
    }),
    getStockLocationBalances: build.query<PagedResult<StockLocationBalance>, BalanceParams & GridParams>({
      query: (params) => ({ url: '/stock/by-location', params }),
      providesTags: ['Stock'],
    }),
    getStockLots: build.query<PagedResult<StockLotBalance>, LotParams & GridParams>({
      query: (params) => ({ url: '/stock/lots', params }),
      providesTags: ['Stock'],
    }),
    /** Ürün / renk / beden için mamul kartını açar (varsa onu döner); açılış stoğu ve iadeler için. */
    openFinishedGoodCard: build.mutation<
      { id: number; code: string; name: string; isNew: boolean },
      { productId: number; colorId: number; sizeId: number }
    >({
      query: (body) => ({ url: '/stock/finished-good-cards', method: 'POST', body }),
      invalidatesTags: ['Stock', 'MasterData'],
    }),
    updateStockLot: build.mutation<null, { lotId: number; body: LotAttributes }>({
      query: ({ lotId, body }) => ({ url: `/stock/lots/${lotId}`, method: 'PUT', body }),
      invalidatesTags: ['Stock'],
    }),
    // Adres tanımları değişince (MasterData) seçim listesi de tazelenir.
    getLocationOptions: build.query<LocationOption[], number>({
      query: (warehouseId) => ({ url: '/stock/locations', params: { warehouseId } }),
      providesTags: ['Stock', 'MasterData'],
    }),
    // Toplu adres açma: adres listeleri (MasterData) ve seçim kutuları tazelenir.
    createLocationsBulk: build.mutation<
      { created: number; skipped: number; firstCode: string | null; lastCode: string | null },
      { warehouseId: number; zone: string; rackFrom: number; rackTo: number; binFrom: number | null; binTo: number | null; digits: number }
    >({
      query: (body) => ({ url: '/warehouse-locations/bulk', method: 'POST', body }),
      invalidatesTags: ['MasterData', 'Stock'],
    }),
    getStockTransactions: build.query<PagedResult<StockTransaction>, TransactionParams & GridParams>({
      query: (params) => ({ url: '/stock/transactions', params }),
      providesTags: ['Stock'],
    }),
    getReceivableBundles: build.query<
      PagedResult<ReceivableBundle>,
      { page: number; pageSize: number; search?: string; productionOrderId?: number } & GridParams
    >({
      query: (params) => ({ url: '/stock/receivable-bundles', params }),
      providesTags: ['Stock', 'Bundle'],
    }),
    receiveStock: build.mutation<StockMovement, ReceiptRequest>({
      query: (body) => ({ url: '/stock/receipts', method: 'POST', body }),
      invalidatesTags: ['Stock'],
    }),
    issueStock: build.mutation<StockMovement, IssueRequest>({
      query: (body) => ({ url: '/stock/issues', method: 'POST', body }),
      invalidatesTags: ['Stock'],
    }),
    transferStock: build.mutation<StockMovement, TransferRequest>({
      query: (body) => ({ url: '/stock/transfers', method: 'POST', body }),
      invalidatesTags: ['Stock'],
    }),
    adjustStock: build.mutation<StockMovement, AdjustmentRequest>({
      query: (body) => ({ url: '/stock/adjustments', method: 'POST', body }),
      invalidatesTags: ['Stock'],
    }),
    getStockReservations: build.query<StockReservations, { warehouseId: number; stockItemId: number }>({
      query: (params) => ({ url: '/stock/reservations', params }),
      providesTags: ['Stock', 'SalesOrder'],
    }),
    createStockReservation: build.mutation<
      StockReservations,
      { warehouseId: number; stockItemId: number; salesOrderLineId: number; quantity: number }
    >({
      query: (body) => ({ url: '/stock/reservations', method: 'POST', body }),
      invalidatesTags: ['Stock', 'SalesOrder', 'Shipment'],
    }),
    releaseStockReservation: build.mutation<StockReservations, number>({
      query: (id) => ({ url: `/stock/reservations/${id}`, method: 'DELETE' }),
      invalidatesTags: ['Stock', 'SalesOrder', 'Shipment'],
    }),
    receiveFromProduction: build.mutation<StockMovement, { requestId: string; bundleId: number; warehouseId: number }>({
      query: (body) => ({ url: '/stock/production-receipts', method: 'POST', body }),
      // Yeni mamul kartı açılmış olabilir: stok kartı listeleri de tazelenir.
      invalidatesTags: ['Stock', 'Bundle', 'ProductionTracking', 'MasterData'],
    }),
  }),
})

export const {
  useGetStockBalancesQuery,
  useGetStockLocationBalancesQuery,
  useGetLocationOptionsQuery,
  useCreateLocationsBulkMutation,
  useGetStockLotsQuery,
  useUpdateStockLotMutation,
  useGetStockTransactionsQuery,
  useGetReceivableBundlesQuery,
  useOpenFinishedGoodCardMutation,
  useReceiveStockMutation,
  useIssueStockMutation,
  useTransferStockMutation,
  useAdjustStockMutation,
  useReceiveFromProductionMutation,
  useGetStockReservationsQuery,
  useCreateStockReservationMutation,
  useReleaseStockReservationMutation,
} = stockApi
