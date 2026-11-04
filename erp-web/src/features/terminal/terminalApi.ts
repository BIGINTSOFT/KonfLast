import { baseApi } from '../../api/baseApi'
import type { PickList } from '../costing/costingApi'
import type { StockUnit } from '../stock/stockApi'

export type ScanKind = 'LOT' | 'LOCATION' | 'ITEM'

export interface ScanLocation {
  id: number
  warehouseId: number
  warehouseCode: string
  warehouseName: string
  code: string
  description: string | null
  isActive: boolean
}

/** Okutulan şeyle ilgili stok: bir kartın (takipli kartta bir topun) bir adresteki miktarı. */
export interface ScanBalance {
  warehouseId: number
  warehouseCode: string
  warehouseName: string
  locationId: number
  locationCode: string
  stockItemId: number
  itemCode: string
  itemName: string
  unit: StockUnit
  isLotTracked: boolean
  lotId: number | null
  lotNo: string | null
  batchNo: string | null
  quantity: number
}

export interface ScanResult {
  kind: ScanKind
  code: string
  location: ScanLocation | null
  item: { id: number; code: string; name: string; unit: StockUnit; isLotTracked: boolean } | null
  lot: {
    id: number
    lotNo: string
    batchNo: string | null
    shade: string | null
    width: number | null
    supplierRollNo: string | null
    initialQuantity: number
  } | null
  /** Topun / kartın bulunduğu yerler ya da adresin içindekiler. */
  balances: ScanBalance[]
}

/** Adres etiketindeki barkod; adres kodu depolar arasında yinelenebildiği için kimlikten üretilir. */
export function locationBarcode(id: number): string {
  return `ADR${String(id).padStart(8, '0')}`
}

export const terminalApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    scanStock: build.query<ScanResult, { code: string; warehouseId?: number }>({
      query: (params) => ({ url: '/stock/scan', params }),
      providesTags: ['Stock', 'MasterData'],
    }),
    // Toplama listesi sipariş numarasıyla bulunur; depo personelinin iş emri yetkisi gerekmez.
    getTerminalPickList: build.query<PickList, { orderNo: string; warehouseId?: number }>({
      query: (params) => ({ url: '/stock/pick-list', params }),
      providesTags: ['Stock', 'Material', 'Bundle'],
    }),
  }),
})

const { useLazyScanStockQuery } = terminalApi

export const { useLazyGetTerminalPickListQuery } = terminalApi

/** Barkod çözme: sonuç yoksa (tanınmayan kod, bağlantı hatası) null döner ve hata <c>error</c>'da durur. */
export function useScanner() {
  const [trigger, state] = useLazyScanStockQuery()

  const scan = async (code: string, warehouseId?: number): Promise<ScanResult | null> => {
    try {
      return await trigger({ code, warehouseId }, false).unwrap()
    } catch {
      return null
    }
  }

  return { scan, busy: state.isFetching, error: state.isError ? state.error : undefined }
}
