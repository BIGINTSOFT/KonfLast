import { Search } from 'lucide-react'
import { useRef, useState } from 'react'
import type { FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { GridHead } from '../../components/grid/GridHead'
import type { GridColumn } from '../../components/grid/GridHead'
import { useGrid } from '../../components/grid/useGrid'
import { Pagination } from '../../components/Pagination'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDateTime, formatQuantity } from '../../utils/format'
import { useGetLookupQuery } from '../masterData/masterDataApi'
import { useGetReceivableBundlesQuery, useReceiveFromProductionMutation } from './stockApi'
import type { ReceivableBundle } from './stockApi'
import { ClearFiltersButton } from '../../components/grid/ClearFiltersButton'

const PAGE_SIZE = 25

/** Hazır gelen mamul deposunun kodu; varsa varsayılan olarak seçilir. */
const DEFAULT_WAREHOUSE_CODE = 'MAMUL'

/**
 * Tamamlanmış demetlerin depoya teslim alınması. Depoya girecek adedi (demetten çıkan sağlam adet) sunucu
 * belirler; teslim alınan demet kapanır ve artık tamire dönemez.
 */
export function ProductionReceiptPage() {
  const canManage = usePermission(Permissions.StockManage)
  const warehouses = useGetLookupQuery({ resource: 'warehouses', take: 100 })

  const grid = useGrid()
  const { page, setPage } = grid
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [selectedWarehouseId, setSelectedWarehouseId] = useState<number | null>(null)
  const [receivingId, setReceivingId] = useState<number | null>(null)
  const [lastReceived, setLastReceived] = useState<string | null>(null)

  // Demet başına istek kimliği: gönderim yarıda kalıp yeniden denenirse stok ikinci kez artmaz.
  const requestIds = useRef(new Map<number, string>())

  const { data, error, isLoading, isFetching } = useGetReceivableBundlesQuery({
    page,
    pageSize: PAGE_SIZE,
    search: search || undefined,
    ...grid.params,
  })
  const [receive, receiveState] = useReceiveFromProductionMutation()

  const columns: GridColumn[] = [
    { key: 'bundleNo', label: 'Demet' },
    { key: 'orderNo', label: 'İş emri' },
    { key: 'productCode', label: 'Ürün' },
    { key: 'colorName', label: 'Renk / beden' },
    { key: 'quantity', label: 'Demet adedi', numeric: true },
    { key: 'finishedQuantity', label: 'Sağlam adet', numeric: true },
    { key: 'completedAt', label: 'Tamamlanma', filter: false },
    ...(canManage ? [{ label: '', ariaLabel: 'İşlemler' }] : []),
  ]

  const warehouseId =
    selectedWarehouseId
    ?? warehouses.data?.find((warehouse) => warehouse.code === DEFAULT_WAREHOUSE_CODE)?.id
    ?? warehouses.data?.[0]?.id
    ?? null

  const handleSearch = (event: FormEvent) => {
    event.preventDefault()
    setPage(1)
    setSearch(searchInput.trim())
  }

  const handleReceive = async (bundle: ReceivableBundle) => {
    if (warehouseId === null) {
      return
    }

    let requestId = requestIds.current.get(bundle.bundleId)
    if (!requestId) {
      requestId = crypto.randomUUID()
      requestIds.current.set(bundle.bundleId, requestId)
    }

    setReceivingId(bundle.bundleId)
    setLastReceived(null)
    const result = await receive({ requestId, bundleId: bundle.bundleId, warehouseId })
    setReceivingId(null)
    if (!('error' in result)) {
      const reserved = result.data.reservedForOrderNo
        ? ` ${formatQuantity(result.data.reservedQuantity)} adedi ${result.data.reservedForOrderNo} siparişine ayrıldı; sevkiyata aktarılmayı bekliyor.`
        : ''
      setLastReceived(`${bundle.bundleNo} numaralı demet teslim alındı: ${formatQuantity(bundle.finishedQuantity)} adet.${reserved}`)
    }
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">Üretimden Teslim Alma</h1>

      <form className="toolbar" onSubmit={handleSearch} role="search">
        <input
          type="search"
          placeholder="Demet no veya sipariş no"
          aria-label="Demet ara"
          value={searchInput}
          onChange={(event) => setSearchInput(event.target.value)}
        />
        <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
        <ClearFiltersButton grid={grid} />
        {canManage && (
          <label>
            Teslim alınacak depo{' '}
            <select
              value={warehouseId ?? ''}
              onChange={(event) => setSelectedWarehouseId(event.target.value ? Number(event.target.value) : null)}
            >
              {warehouses.data?.map((warehouse) => (
                <option key={warehouse.id} value={warehouse.id}>{warehouse.code} — {warehouse.name}</option>
              ))}
            </select>
          </label>
        )}
      </form>

      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error ?? warehouses.error} />
      <ErrorAlert error={receiveState.error} />
      {lastReceived && <p className="alert alert--info" role="status">{lastReceived}</p>}

      {data && (
        <>
          <div className="table-wrap" aria-busy={isFetching}>
            <table className="table">
              <GridHead columns={columns} grid={grid} />
              <tbody>
                {data.items.map((bundle) => (
                  <tr key={bundle.bundleId}>
                    <td><Link to={`/bundles/${bundle.bundleId}`}><code>{bundle.bundleNo}</code></Link></td>
                    <td>{bundle.orderNo}</td>
                    <td><code>{bundle.productCode}</code> {bundle.productName}</td>
                    <td>{bundle.colorName} / {bundle.sizeName}</td>
                    <td className="table__number">{formatQuantity(bundle.quantity)}</td>
                    <td className="table__number"><strong>{formatQuantity(bundle.finishedQuantity)}</strong></td>
                    <td>{formatDateTime(bundle.completedAt)}</td>
                    {canManage && (
                      <td className="table__actions">
                        <button
                          type="button"
                          className="button button--primary"
                          disabled={warehouseId === null || receivingId !== null || bundle.finishedQuantity <= 0}
                          title={bundle.finishedQuantity <= 0 ? 'Demetten sağlam parça çıkmamış.' : undefined}
                          onClick={() => void handleReceive(bundle)}
                        >
                          {receivingId === bundle.bundleId ? 'Alınıyor…' : 'Teslim al'}
                        </button>
                      </td>
                    )}
                  </tr>
                ))}
                {data.items.length === 0 && (
                  <tr>
                    <td colSpan={canManage ? 8 : 7} className="table__empty">Teslim alınmayı bekleyen demet yok.</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}
    </section>
  )
}
