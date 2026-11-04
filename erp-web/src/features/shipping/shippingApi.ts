import { baseApi } from '../../api/baseApi'
import type { GridParams } from '../../components/grid/useGrid'
import type { PagedResult } from '../../types/api'
import type { DocumentLanguage } from '../../utils/documentText'
import type { ProformaParty } from '../salesOrders/salesOrdersApi'
import type { StockUnit } from '../stock/stockApi'

/** Taslak → toplamada → toplandı (irsaliye bekliyor) → sevk edildi; stok düşmeden iptal edilebilir. */
export type ShipmentStatus = 'DRAFT' | 'PICKING' | 'PICKED' | 'SHIPPED' | 'CANCELLED'

export interface ShipmentListItem {
  id: number
  shipmentNo: string
  customerId: number
  customerName: string
  warehouseCode: string
  orderNo: string | null
  shipDate: string
  status: ShipmentStatus
  documentNo: string | null
  itemCount: number
  totalQuantity: number
  /** Bağlı müşteri siparişindeki müşterinin kendi sipariş numarası. */
  customerOrderNo: string | null
  cartonCount: number
  pickedQuantity: number
  /** Toplamayı üstlenmiş kullanıcı. */
  pickingBy: string | null
}

export interface ShipmentItem {
  id: number
  stockItemId: number
  itemCode: string
  itemName: string
  unit: StockUnit
  quantity: number
  /** Kartın sevkiyat deposundaki şu anki stoğu. */
  availableQuantity: number
  /** Depodan toplanan adet. */
  pickedQuantity: number
  /** Toplama eksik bittiyse ilk istenen adet. */
  requestedQuantity: number | null
  /** İrsaliye öncesi kontrolde sayılan adet. */
  checkedQuantity: number | null
}

/** Toplama ve irsaliye durumu; yapılabilecek işlemleri sunucu belirler. */
export interface ShipmentWorkflow {
  canSendToPicking: boolean
  canReturnToDraft: boolean
  canCompletePicking: boolean
  canReopenPicking: boolean
  pickingBy: string | null
  pickingStartedAt: string | null
  pickedAt: string | null
  pickedBy: string | null
  vehiclePlate: string | null
  driverName: string | null
  driverIdentityNo: string | null
  /** ETTN; irsaliye kesilince verilir. */
  despatchUuid: string | null
}

/** Koli ve yükleme özeti; koli yoksa cartonCount 0, matchesItems true. */
export interface ShipmentCartonSummary {
  cartonCount: number
  loadedCount: number
  matchesItems: boolean
}

/** Yapılabilecek işlemleri (düzenle, sevk et, iptal) sunucu belirler. */
export interface Shipment {
  id: number
  shipmentNo: string
  customerId: number
  customerCode: string
  customerName: string
  warehouseId: number
  warehouseCode: string
  warehouseName: string
  productionOrderId: number | null
  orderNo: string | null
  shipDate: string
  documentNo: string | null
  carrier: string | null
  notes: string | null
  status: ShipmentStatus
  shippedAt: string | null
  shippedBy: string | null
  totalQuantity: number
  isEditable: boolean
  canShip: boolean
  canCancel: boolean
  rowVersion: string
  createdAt: string
  createdBy: string | null
  items: ShipmentItem[]
  /** Sevkiyatın karşıladığı müşteri siparişi (iş emrine bağlı sevkiyatta ondan gelir). */
  salesOrderId: number | null
  salesOrderNo: string | null
  customerOrderNo: string | null
  cartons: ShipmentCartonSummary | null
  workflow: ShipmentWorkflow | null
}

export interface ShipmentRequest {
  customerId: number
  warehouseId: number
  shipDate: string
  items: { stockItemId: number; quantity: number }[]
  productionOrderId: number | null
  salesOrderId: number | null
  documentNo: string | null
  carrier: string | null
  notes: string | null
  rowVersion?: string
}

export interface ShipmentListParams {
  page: number
  pageSize: number
  search?: string
  status?: ShipmentStatus
  /** Sevk tarihi aralığı (yyyy-MM-dd, iki uç dahil). */
  from?: string
  to?: string
}

// ───────── Koliler (çeki listesi) ─────────

export interface ShipmentPackingItem {
  stockItemId: number
  itemCode: string
  productCode: string | null
  productName: string | null
  colorCode: string | null
  colorName: string | null
  sizeCode: string | null
  sizeName: string | null
  sizeOrder: number
  unit: StockUnit
  /** Sevkiyattaki adet. */
  quantity: number
  /** Kolilere girmiş adet; sevk için sevkiyattaki adede eşit olmalıdır. */
  packedQuantity: number
}

export interface ShipmentCartonLine {
  id: number
  stockItemId: number
  itemCode: string
  colorName: string | null
  sizeName: string | null
  sizeOrder: number
  quantity: number
}

export interface ShipmentCarton {
  id: number
  cartonNo: number
  /** Etiketteki barkod. */
  code: string
  assortmentTemplateId: number | null
  /** Boşsa karışık (artan ya da elle girilmiş) koli. */
  assortmentTemplateCode: string | null
  netKg: number | null
  grossKg: number | null
  loadedAt: string | null
  loadedBy: string | null
  quantity: number
  lines: ShipmentCartonLine[]
}

export interface ShipmentPacking {
  shipmentId: number
  shipmentNo: string
  customerId: number
  status: ShipmentStatus
  isEditable: boolean
  /** Sevkiyatın sürümü; koli işlemlerinde geri gönderilir. */
  rowVersion: string
  cartonCount: number
  loadedCount: number
  matchesItems: boolean
  items: ShipmentPackingItem[]
  cartons: ShipmentCarton[]
}

export interface ShipmentCartonRequest {
  rowVersion: string
  lines: { stockItemId: number; quantity: number }[]
  netKg: number | null
  grossKg: number | null
}

// ───────── Basılı belgeler ─────────

export interface ShipmentDocumentItem {
  stockItemId: number
  itemCode: string
  productCode: string | null
  productName: string | null
  customerArticleCode: string | null
  colorCode: string | null
  colorName: string | null
  sizeCode: string | null
  sizeName: string | null
  sizeOrder: number
  unit: StockUnit
  quantity: number
}

export interface ShipmentDocumentCarton {
  cartonNo: number
  code: string
  netKg: number | null
  grossKg: number | null
  lines: { stockItemId: number; quantity: number }[]
}

/** Çeki listesi, koli etiketi ve irsaliye çıktılarının ortak verisi; metinler müşterinin belge dilindedir. */
export interface ShipmentDocument {
  shipmentId: number
  shipmentNo: string
  documentNo: string | null
  shipDate: string
  status: ShipmentStatus
  shippedAt: string | null
  carrier: string | null
  notes: string | null
  customerCode: string
  customer: ProformaParty
  invoiceAddress: string | null
  deliveryAddress: string | null
  salesOrderNo: string | null
  customerOrderNo: string | null
  productionOrderNo: string | null
  /** Gönderen: ilk aktif fabrika; tanımlı değilse boş. */
  seller: ProformaParty | null
  documentLanguage: DocumentLanguage
  items: ShipmentDocumentItem[]
  cartons: ShipmentDocumentCarton[]
  printedAt: string
  despatch: {
    despatchUuid: string | null
    vehiclePlate: string | null
    driverName: string | null
    driverIdentityNo: string | null
    receiverIsEDespatchUser: boolean
    receiverAlias: string | null
    checkedBy: string | null
  } | null
}

// ───────── Toplama ─────────

export interface PickingQueueItem {
  id: number
  shipmentNo: string
  status: ShipmentStatus
  customerName: string
  warehouseCode: string
  orderNo: string | null
  customerOrderNo: string | null
  shipDate: string
  itemCount: number
  quantity: number
  pickedQuantity: number
  pickingBy: string | null
  pickingStartedAt: string | null
}

export interface PickingItem {
  shipmentItemId: number
  stockItemId: number
  itemCode: string
  itemName: string
  productCode: string | null
  colorName: string | null
  sizeName: string | null
  sizeOrder: number
  unit: StockUnit
  quantity: number
  pickedQuantity: number
  requestedQuantity: number | null
  /** Depoda bulunamayan (adreslere dağıtılamayan) adet. */
  shortage: number
}

/** Toplama listesinin satırı: adres sırasıyla numaralanır, toplayıcı bu sırayla yürür. */
export interface PickingStep {
  sequence: number
  shipmentItemId: number
  stockItemId: number
  itemCode: string
  itemName: string
  colorName: string | null
  sizeName: string | null
  locationId: number
  locationCode: string
  /** Bu adresten alınacak toplam (alınmış + kalan). */
  quantity: number
  pickedQuantity: number
  done: boolean
}

export interface PickRecord {
  id: number
  itemCode: string
  locationCode: string
  quantity: number
  pickedAt: string
  pickedBy: string
}

export interface ShipmentPicking {
  id: number
  shipmentNo: string
  status: ShipmentStatus
  customerName: string
  warehouseId: number
  warehouseCode: string
  orderNo: string | null
  customerOrderNo: string | null
  shipDate: string
  notes: string | null
  pickingBy: string | null
  pickingStartedAt: string | null
  /** Toplamayı bu kullanıcı üstlenmiş. */
  isMine: boolean
  rowVersion: string
  items: PickingItem[]
  steps: PickingStep[]
  picks: PickRecord[]
}

// ───────── Sevke hazır siparişler ─────────

export interface ReadyOrder {
  salesOrderId: number
  orderNo: string
  customerOrderNo: string | null
  customerId: number
  customerName: string
  dueDate: string
  ordered: number
  shipped: number
  /** Depoda siparişe ayrılmış (stoktan ve üretimden). */
  reserved: number
  /** Açık sevkiyatlarda bulunan. */
  inShipments: number
  /** Sevkiyata aktarılabilecek: ayrılan − açık sevkiyattaki. */
  transferable: number
  openShipmentCount: number
  warehouseCodes: string[]
}

export interface ReadyOrderLine {
  warehouseId: number
  warehouseCode: string
  stockItemId: number
  itemCode: string
  productCode: string | null
  productName: string | null
  colorName: string | null
  sizeName: string | null
  sizeOrder: number
  ordered: number
  shipped: number
  reserved: number
  inShipments: number
  transferable: number
}

export interface ReadyOrderDetail {
  order: ReadyOrder
  lines: ReadyOrderLine[]
  shipments: { id: number; shipmentNo: string; status: ShipmentStatus; warehouseCode: string; quantity: number; pickedQuantity: number }[]
}

export interface ShipmentDispatchRequest {
  rowVersion: string
  checks: { shipmentItemId: number; quantity: number }[]
  documentNo: string | null
  carrier: string | null
  vehiclePlate: string | null
  driverName: string | null
  driverIdentityNo: string | null
}

// ───────── Yükleme (el terminali) ─────────

export interface ShipmentLoadingCarton {
  id: number
  cartonNo: number
  code: string
  quantity: number
  /** Kısa içerik, örn. "Lacivert S:1 M:2 L:2". */
  summary: string
  loadedAt: string | null
  loadedBy: string | null
}

export interface ShipmentLoading {
  shipmentId: number
  shipmentNo: string
  customerName: string
  status: ShipmentStatus
  shipDate: string
  cartonCount: number
  loadedCount: number
  cartons: ShipmentLoadingCarton[]
}

export interface LoadableShipment {
  id: number
  shipmentNo: string
  customerName: string
  shipDate: string
  status: ShipmentStatus
  cartonCount: number
  loadedCount: number
}

export interface CartonLoadResult {
  cartonNo: number
  code: string
  /** Koli daha önce okutulmuştu. */
  alreadyLoaded: boolean
  shipment: ShipmentLoading
}

// Sevk etme stoğu düşürür ve siparişin sevk edilen adedini değiştirir. Koli işlemleri sevkiyatın sürümünü
// ilerlettiği için sevkiyat etiketi de geçersiz kılınır.
export const shippingApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getShipments: build.query<PagedResult<ShipmentListItem>, ShipmentListParams & GridParams>({
      query: (params) => ({ url: '/shipments', params }),
      providesTags: ['Shipment'],
    }),
    getShipment: build.query<Shipment, number>({
      query: (id) => `/shipments/${id}`,
      // Kalemlerdeki mevcut stok, stok hareketleriyle değişir.
      providesTags: ['Shipment', 'Stock'],
    }),
    saveShipment: build.mutation<Shipment, { id?: number; body: ShipmentRequest }>({
      query: ({ id, body }) => ({
        url: id === undefined ? '/shipments' : `/shipments/${id}`,
        method: id === undefined ? 'POST' : 'PUT',
        body,
      }),
      invalidatesTags: ['Shipment'],
    }),
    // İrsaliye kesme stoğu düşürür ve siparişin sevk edilen adedini değiştirir.
    dispatchShipment: build.mutation<Shipment, { id: number; body: ShipmentDispatchRequest }>({
      query: ({ id, body }) => ({ url: `/shipments/${id}/dispatch`, method: 'POST', body }),
      invalidatesTags: ['Shipment', 'Stock', 'ProductionTracking', 'SalesOrder'],
    }),
    getPickingQueue: build.query<PickingQueueItem[], { search?: string; status?: ShipmentStatus }>({
      query: (params) => ({ url: '/shipments/picking-queue', params }),
      providesTags: ['Shipment'],
    }),
    getShipmentPicking: build.query<ShipmentPicking, number>({
      query: (id) => `/shipments/${id}/picking`,
      providesTags: ['Shipment', 'Stock'],
    }),
    shipmentTransition: build.mutation<ShipmentPicking, { id: number; action: 'send-to-picking' | 'return-to-draft' | 'reopen-picking'; rowVersion: string }>({
      query: ({ id, action, rowVersion }) => ({ url: `/shipments/${id}/${action}`, method: 'POST', body: { rowVersion } }),
      invalidatesTags: ['Shipment'],
    }),
    startPicking: build.mutation<ShipmentPicking, { id: number; takeOver?: boolean }>({
      query: ({ id, takeOver }) => ({ url: `/shipments/${id}/picking/start`, method: 'POST', body: { takeOver: takeOver ?? false } }),
      invalidatesTags: ['Shipment'],
    }),
    pickShipmentItem: build.mutation<
      ShipmentPicking,
      { id: number; requestId: string; shipmentItemId: number; locationId: number; quantity: number }
    >({
      query: ({ id, ...body }) => ({ url: `/shipments/${id}/picks`, method: 'POST', body }),
      invalidatesTags: ['Shipment'],
    }),
    undoShipmentPick: build.mutation<ShipmentPicking, { id: number; pickId: number }>({
      query: ({ id, pickId }) => ({ url: `/shipments/${id}/picks/${pickId}`, method: 'DELETE' }),
      invalidatesTags: ['Shipment'],
    }),
    completePicking: build.mutation<ShipmentPicking, number>({
      query: (id) => ({ url: `/shipments/${id}/picking/complete`, method: 'POST' }),
      invalidatesTags: ['Shipment'],
    }),
    getReadyOrders: build.query<ReadyOrder[], { search?: string }>({
      query: (params) => ({ url: '/shipping/ready-orders', params }),
      providesTags: ['Shipment', 'Stock', 'SalesOrder'],
    }),
    getReadyOrder: build.query<ReadyOrderDetail, number>({
      query: (id) => `/shipping/ready-orders/${id}`,
      providesTags: ['Shipment', 'Stock', 'SalesOrder'],
    }),
    transferReadyOrder: build.mutation<
      number,
      { salesOrderId: number; warehouseId: number; shipDate: string; lines: { stockItemId: number; quantity: number }[]; carrier: string | null; notes: string | null }
    >({
      query: ({ salesOrderId, ...body }) => ({ url: `/shipping/ready-orders/${salesOrderId}/transfer`, method: 'POST', body }),
      invalidatesTags: ['Shipment'],
    }),
    cancelShipment: build.mutation<Shipment, { id: number; rowVersion: string }>({
      query: ({ id, rowVersion }) => ({ url: `/shipments/${id}/cancel`, method: 'POST', body: { rowVersion } }),
      invalidatesTags: ['Shipment'],
    }),
    deleteShipment: build.mutation<null, number>({
      query: (id) => ({ url: `/shipments/${id}`, method: 'DELETE' }),
      invalidatesTags: ['Shipment'],
    }),
    getShipmentPacking: build.query<ShipmentPacking, number>({
      query: (id) => `/shipments/${id}/cartons`,
      providesTags: ['Shipment'],
    }),
    distributeShipmentCartons: build.mutation<ShipmentPacking, { id: number; assortmentTemplateId: number; rowVersion: string }>({
      query: ({ id, ...body }) => ({ url: `/shipments/${id}/cartons/distribute`, method: 'POST', body }),
      invalidatesTags: ['Shipment'],
    }),
    saveShipmentCarton: build.mutation<ShipmentPacking, { id: number; cartonId?: number; body: ShipmentCartonRequest }>({
      query: ({ id, cartonId, body }) => ({
        url: cartonId === undefined ? `/shipments/${id}/cartons` : `/shipments/${id}/cartons/${cartonId}`,
        method: cartonId === undefined ? 'POST' : 'PUT',
        body,
      }),
      invalidatesTags: ['Shipment'],
    }),
    deleteShipmentCarton: build.mutation<ShipmentPacking, { id: number; cartonId: number; rowVersion: string }>({
      query: ({ id, cartonId, rowVersion }) => ({ url: `/shipments/${id}/cartons/${cartonId}`, method: 'DELETE', params: { rowVersion } }),
      invalidatesTags: ['Shipment'],
    }),
    clearShipmentCartons: build.mutation<ShipmentPacking, { id: number; rowVersion: string }>({
      query: ({ id, rowVersion }) => ({ url: `/shipments/${id}/cartons/clear`, method: 'POST', body: { rowVersion } }),
      invalidatesTags: ['Shipment'],
    }),
    setShipmentCartonWeights: build.mutation<
      ShipmentPacking,
      { id: number; rowVersion: string; cartonIds: number[]; netKg: number | null; grossKg: number | null }
    >({
      query: ({ id, ...body }) => ({ url: `/shipments/${id}/cartons/weights`, method: 'POST', body }),
      invalidatesTags: ['Shipment'],
    }),
    getShipmentDocument: build.query<ShipmentDocument, number>({
      query: (id) => `/shipments/${id}/document`,
      providesTags: ['Shipment'],
    }),
    getLoadableShipments: build.query<LoadableShipment[], { search?: string }>({
      query: (params) => ({ url: '/shipments/loading/lookup', params }),
      providesTags: ['Shipment'],
    }),
    findShipmentLoading: build.query<ShipmentLoading, string>({
      query: (code) => ({ url: '/shipments/loading/find', params: { code } }),
      keepUnusedDataFor: 0,
    }),
    getShipmentLoading: build.query<ShipmentLoading, number>({
      query: (id) => `/shipments/${id}/loading`,
      providesTags: ['Shipment'],
    }),
    // Yükleme okutması sevkiyatın sürümünü değiştirmez; ayrıntıdaki ilerleme tazelensin diye etiket geçersiz kılınır.
    loadShipmentCarton: build.mutation<CartonLoadResult, { id: number; code: string }>({
      query: ({ id, code }) => ({ url: `/shipments/${id}/loading/scan`, method: 'POST', body: { code } }),
      invalidatesTags: ['Shipment'],
    }),
  }),
})

export const {
  useGetShipmentsQuery,
  useGetShipmentQuery,
  useSaveShipmentMutation,
  useDispatchShipmentMutation,
  useGetPickingQueueQuery,
  useGetShipmentPickingQuery,
  useLazyGetShipmentPickingQuery,
  useShipmentTransitionMutation,
  useStartPickingMutation,
  usePickShipmentItemMutation,
  useUndoShipmentPickMutation,
  useCompletePickingMutation,
  useGetReadyOrdersQuery,
  useGetReadyOrderQuery,
  useTransferReadyOrderMutation,
  useCancelShipmentMutation,
  useDeleteShipmentMutation,
  useGetShipmentPackingQuery,
  useDistributeShipmentCartonsMutation,
  useSaveShipmentCartonMutation,
  useDeleteShipmentCartonMutation,
  useClearShipmentCartonsMutation,
  useSetShipmentCartonWeightsMutation,
  useGetShipmentDocumentQuery,
  useGetLoadableShipmentsQuery,
  useLazyFindShipmentLoadingQuery,
  useGetShipmentLoadingQuery,
  useLoadShipmentCartonMutation,
} = shippingApi
