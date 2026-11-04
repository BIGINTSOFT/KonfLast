import { Plus, Search } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { GridHead } from '../../components/grid/GridHead'
import type { GridColumn } from '../../components/grid/GridHead'
import { useGrid } from '../../components/grid/useGrid'
import { Pagination } from '../../components/Pagination'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDate, formatQuantity } from '../../utils/format'
import { ShipmentDialog } from './ShipmentDialog'
import { useGetShipmentsQuery } from './shippingApi'
import { shipmentStatusPresentation } from './shippingPresentation'
import { ClearFiltersButton } from '../../components/grid/ClearFiltersButton'

const PAGE_SIZE = 20

/** Sevkiyat listesi; sevk etme ve iptal sevkiyat sayfasındadır. */
export function ShipmentsPage() {
  const canManage = usePermission(Permissions.ShipmentManage)
  const navigate = useNavigate()

  const grid = useGrid()
  const { page, setPage } = grid
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [from, setFrom] = useState('')
  const [to, setTo] = useState('')
  const [creating, setCreating] = useState(false)

  const { data, error, isLoading, isFetching } = useGetShipmentsQuery({
    page,
    pageSize: PAGE_SIZE,
    search: search || undefined,
    from: from || undefined,
    to: to || undefined,
    ...grid.params,
  })

  const columns: GridColumn[] = [
    { key: 'shipmentNo', label: 'Sevkiyat no' },
    { key: 'customerName', label: 'Müşteri' },
    { key: 'orderNo', label: 'İş emri' },
    { key: 'customerOrderNo', label: 'Müşteri sipariş no' },
    { key: 'warehouseCode', label: 'Depo' },
    { key: 'shipDate', label: 'Sevk tarihi', filter: 'date' },
    { key: 'documentNo', label: 'İrsaliye' },
    {
      key: 'status',
      label: 'Durum',
      filter: Object.entries(shipmentStatusPresentation).map(([value, { label }]) => ({ value, label })),
    },
    { key: 'itemCount', label: 'Kalem', numeric: true },
    { key: 'totalQuantity', label: 'Adet', numeric: true },
    { key: 'cartonCount', label: 'Koli', numeric: true },
  ]

  const handleSearch = (event: FormEvent) => {
    event.preventDefault()
    setPage(1)
    setSearch(searchInput.trim())
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">Sevkiyatlar</h1>

      <form className="toolbar" onSubmit={handleSearch} role="search">
        <input
          type="search"
          placeholder="Sevkiyat, irsaliye, müşteri, sipariş ya da müşteri sipariş no"
          aria-label="Sevkiyat ara"
          value={searchInput}
          onChange={(event) => setSearchInput(event.target.value)}
        />
        <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
        {/* Sevk tarihi aralığı: etiket ve iki tarih tek çerçevede; seçilince liste hemen süzülür. */}
        <div className="toolbar-range" role="group" aria-label="Sevk tarihi aralığı">
          <span className="toolbar-range__label">Sevk tarihi</span>
          <input
            type="date"
            aria-label="Sevk tarihi başlangıç"
            title="Başlangıç"
            value={from}
            max={to || undefined}
            onChange={(event) => {
              setPage(1)
              setFrom(event.target.value)
            }}
          />
          <span className="toolbar-range__separator" aria-hidden="true">–</span>
          <input
            type="date"
            aria-label="Sevk tarihi bitiş"
            title="Bitiş"
            value={to}
            min={from || undefined}
            onChange={(event) => {
              setPage(1)
              setTo(event.target.value)
            }}
          />
        </div>
        <ClearFiltersButton grid={grid} />
        <div className="toolbar__actions">
          {canManage && (
            <button type="button" className="button button--primary" onClick={() => setCreating(true)}>
              <Plus aria-hidden="true" />
              Yeni sevkiyat
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
                {data.items.map((shipment) => {
                  const presentation = shipmentStatusPresentation[shipment.status]
                  return (
                    <tr key={shipment.id}>
                      <td><Link to={`/shipments/${shipment.id}`}><code>{shipment.shipmentNo}</code></Link></td>
                      <td>{shipment.customerName}</td>
                      <td>{shipment.orderNo ?? '—'}</td>
                      <td>{shipment.customerOrderNo ?? '—'}</td>
                      <td>{shipment.warehouseCode}</td>
                      <td>{formatDate(shipment.shipDate)}</td>
                      <td>{shipment.documentNo ?? '—'}</td>
                      <td>
                        <span className={`badge badge--${presentation.tone}`}>
                          <span aria-hidden="true">{presentation.icon}</span> {presentation.label}
                        </span>
                      </td>
                      <td className="table__number">{shipment.itemCount}</td>
                      <td className="table__number">{formatQuantity(shipment.totalQuantity)}</td>
                      <td className="table__number">{shipment.cartonCount > 0 ? shipment.cartonCount : '—'}</td>
                    </tr>
                  )
                })}
                {data.items.length === 0 && (
                  <tr>
                    <td colSpan={columns.length} className="table__empty">Sevkiyat bulunamadı.</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}

      {creating && (
        <ShipmentDialog onClose={() => setCreating(false)} onSaved={(shipment) => void navigate(`/shipments/${shipment.id}`)} />
      )}
    </section>
  )
}
