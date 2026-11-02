import { Plus, Search } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { fieldErrors } from '../../api/envelope'
import { ErrorAlert } from '../../components/FormControls'
import { GridHead } from '../../components/grid/GridHead'
import type { GridColumn } from '../../components/grid/GridHead'
import { useGrid } from '../../components/grid/useGrid'
import { Modal } from '../../components/Modal'
import { Pagination } from '../../components/Pagination'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDateTime } from '../../utils/format'
import { useGetLookupQuery } from '../masterData/masterDataApi'
import { useGetLocationOptionsQuery } from './stockApi'
import { stockCountStatusPresentation, useGetStockCountsQuery, useOpenStockCountMutation } from './stockCountsApi'
import { locationLabel } from './stockPresentation'
import { ClearFiltersButton } from '../../components/grid/ClearFiltersButton'
import { WarehouseFilter } from './WarehouseFilter'

const PAGE_SIZE = 25

/** Yeni sayım: depo ve (isteğe bağlı) tek adres seçilir; açılınca kapsamdaki stok hareketleri kilitlenir. */
function OpenCountDialog({ onClose }: { onClose: () => void }) {
  const navigate = useNavigate()
  const warehouses = useGetLookupQuery({ resource: 'warehouses', take: 100 })
  const [open, { isLoading, error }] = useOpenStockCountMutation()
  const [warehouseId, setWarehouseId] = useState<number | null>(null)
  const [locationId, setLocationId] = useState<number | null>(null)
  const [note, setNote] = useState('')
  const locations = useGetLocationOptionsQuery(warehouseId ?? 0, { skip: warehouseId === null })

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    if (warehouseId === null) {
      return
    }

    const result = await open({ warehouseId, locationId, note: note.trim() || null })
    if ('data' in result && result.data) {
      navigate(`/stock-counts/${result.data.id}`)
    }
  }

  return (
    <Modal title="Yeni sayım" onClose={onClose}>
      <form className="form" onSubmit={(event) => void handleSubmit(event)} noValidate>
        <label className="field">
          <span>Depo</span>
          <select
            value={warehouseId ?? ''}
            onChange={(event) => {
              setWarehouseId(event.target.value ? Number(event.target.value) : null)
              setLocationId(null)
            }}
            aria-invalid={Boolean(fieldErrors(error, 'warehouseId')?.length)}
          >
            <option value="">Seçin…</option>
            {warehouses.data?.map((warehouse) => (
              <option key={warehouse.id} value={warehouse.id}>{warehouse.code} — {warehouse.name}</option>
            ))}
          </select>
        </label>
        <label className="field">
          <span>Kapsam</span>
          <select
            value={locationId ?? ''}
            onChange={(event) => setLocationId(event.target.value ? Number(event.target.value) : null)}
            disabled={!locations.data}
          >
            <option value="">Deponun tamamı</option>
            {locations.data?.map((location) => (
              <option key={location.id} value={location.id}>Yalnızca {locationLabel(location)}</option>
            ))}
          </select>
          <small className="field__hint">
            Sayım açıkken kapsamdaki adreslerde giriş, çıkış, transfer ve üretime malzeme çıkışı yapılamaz.
          </small>
        </label>
        <label className="field">
          <span>Not (isteğe bağlı)</span>
          <textarea rows={2} maxLength={300} value={note} onChange={(event) => setNote(event.target.value)} />
        </label>

        <ErrorAlert error={error ?? warehouses.error} fields={['warehouseId']} />

        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading || warehouseId === null}>
            {isLoading ? 'Açılıyor…' : 'Sayımı aç'}
          </button>
        </footer>
      </form>
    </Modal>
  )
}

/** Sayım fişleri: açık, onaylanmış ve iptal edilmiş sayımlar. */
export function StockCountsPage() {
  const canManage = usePermission(Permissions.StockManage)
  const grid = useGrid()
  const { page, setPage } = grid
  const [warehouseId, setWarehouseId] = useState<number | ''>('')
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [opening, setOpening] = useState(false)

  const { data, error, isLoading, isFetching } = useGetStockCountsQuery({
    page,
    pageSize: PAGE_SIZE,
    warehouseId: warehouseId || undefined,
    search: search || undefined,
    ...grid.params,
  })

  const columns: GridColumn[] = [
    { key: 'countNo', label: 'Sayım no' },
    { key: 'warehouseCode', label: 'Depo' },
    { key: 'locationCode', label: 'Kapsam' },
    {
      key: 'status',
      label: 'Durum',
      filter: Object.entries(stockCountStatusPresentation).map(([value, { label }]) => ({ value, label })),
    },
    { key: 'lineCount', label: 'Satır', numeric: true },
    { key: 'countedLineCount', label: 'Sayılan', numeric: true },
    { key: 'differenceLineCount', label: 'Farklı', numeric: true },
    { key: 'createdAt', label: 'Açılış', filter: false },
    { key: 'createdBy', label: 'Açan' },
  ]

  const handleSearch = (event: FormEvent) => {
    event.preventDefault()
    setPage(1)
    setSearch(searchInput.trim())
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">Sayımlar</h1>

      <form className="toolbar" onSubmit={handleSearch} role="search">
        <input
          type="search"
          placeholder="Sayım no"
          aria-label="Sayım ara"
          value={searchInput}
          onChange={(event) => setSearchInput(event.target.value)}
        />
        <WarehouseFilter value={warehouseId} onChange={(value) => {
            setPage(1)
            setWarehouseId(value)
          }} />
        <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
        <ClearFiltersButton grid={grid} />
        <div className="toolbar__actions">
          {canManage && (
            <button type="button" className="button button--primary" onClick={() => setOpening(true)}>
              <Plus aria-hidden="true" />
              Yeni sayım
            </button>
          )}
        </div>
      </form>

      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />

      {data && (
        <>
          <div className="table-wrap" aria-busy={isFetching}>
            <table className="table">
              <GridHead columns={columns} grid={grid} />
              <tbody>
                {data.items.map((count) => {
                  const status = stockCountStatusPresentation[count.status]
                  return (
                    <tr key={count.id}>
                      <td><Link to={`/stock-counts/${count.id}`} className="table__open"><code>{count.countNo}</code></Link></td>
                      <td>{count.warehouseCode} — {count.warehouseName}</td>
                      <td>{count.locationCode ? <code>{count.locationCode}</code> : 'Deponun tamamı'}</td>
                      <td><span className={`badge badge--${status.tone}`}>{status.label}</span></td>
                      <td className="table__number">{count.lineCount}</td>
                      <td className="table__number">{count.countedLineCount}</td>
                      <td className="table__number">{count.differenceLineCount}</td>
                      <td>{formatDateTime(count.createdAt)}</td>
                      <td>{count.createdBy ?? '—'}</td>
                    </tr>
                  )
                })}
                {data.items.length === 0 && (
                  <tr>
                    <td colSpan={columns.length} className="table__empty">Sayım fişi bulunamadı.</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}

      {opening && <OpenCountDialog onClose={() => setOpening(false)} />}
    </section>
  )
}
