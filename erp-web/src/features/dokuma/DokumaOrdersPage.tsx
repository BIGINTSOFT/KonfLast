import { Plus, Search } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { ClearFiltersButton } from '../../components/grid/ClearFiltersButton'
import { GridHead } from '../../components/grid/GridHead'
import type { GridColumn } from '../../components/grid/GridHead'
import { useGrid } from '../../components/grid/useGrid'
import { Pagination } from '../../components/Pagination'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDate, formatQuantity } from '../../utils/format'
import { formatMoney } from '../../utils/money'
import { DokumaOrderFormDialog } from './DokumaOrderFormDialog'
import { dokumaOrderOwnershipLabels, dokumaOrderStatusPresentation, useGetDokumaOrdersQuery } from './dokumaOrdersApi'

const PAGE_SIZE = 20

/** Dokuma siparişleri, en yeniden eskiye. */
export function DokumaOrdersPage() {
  const navigate = useNavigate()
  const canManage = usePermission(Permissions.DokumaOrderManage)
  const grid = useGrid()
  const { page, setPage } = grid
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [creating, setCreating] = useState(false)

  const { data, error, isLoading, isFetching } = useGetDokumaOrdersQuery({
    page,
    pageSize: PAGE_SIZE,
    search: search || undefined,
    ...grid.params,
  })

  const columns: GridColumn[] = [
    { key: 'orderNo', label: 'Sipariş no' },
    { key: 'orderDate', label: 'Tarih', filter: false },
    { key: 'customerName', label: 'Müşteri' },
    { key: 'customerOrderNo', label: 'Müşteri sipariş no' },
    {
      key: 'ownership',
      label: 'İş türü',
      filter: Object.entries(dokumaOrderOwnershipLabels).map(([value, label]) => ({ value, label })),
    },
    { key: 'dueDate', label: 'Termin', filter: false },
    { key: 'lineCount', label: 'Satır', numeric: true },
    { key: 'totalMeters', label: 'Metre', numeric: true },
    { key: 'totalAmount', label: 'Tahmini tutar', numeric: true },
    {
      key: 'status',
      label: 'Durum',
      filter: Object.entries(dokumaOrderStatusPresentation).map(([value, { label }]) => ({ value, label })),
    },
  ]

  const handleSearch = (event: FormEvent) => {
    event.preventDefault()
    setPage(1)
    setSearch(searchInput.trim())
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">Siparişler</h1>

      <form className="toolbar" onSubmit={handleSearch} role="search">
        <input
          type="search"
          placeholder="Sipariş no ya da müşteri"
          aria-label="Sipariş ara"
          value={searchInput}
          onChange={(event) => setSearchInput(event.target.value)}
        />
        <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
        <ClearFiltersButton grid={grid} />
        <div className="toolbar__actions">
          {canManage && (
            <button type="button" className="button button--primary" onClick={() => setCreating(true)}>
              <Plus aria-hidden="true" />
              Yeni sipariş
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
                {data.items.map((order) => {
                  const status = dokumaOrderStatusPresentation[order.status]
                  return (
                    <tr key={order.id}>
                      <td><Link to={`/dokuma/orders/${order.id}`}><code>{order.orderNo}</code></Link></td>
                      <td>{formatDate(order.orderDate)}</td>
                      <td>{order.customerName ?? <span className="muted">Stoğa üretim</span>}</td>
                      <td>{order.customerOrderNo ?? '—'}</td>
                      <td>{dokumaOrderOwnershipLabels[order.ownership]}</td>
                      <td>{order.dueDate ? formatDate(order.dueDate) : '—'}</td>
                      <td className="table__number">{order.lineCount}</td>
                      <td className="table__number">{formatQuantity(order.totalMeters)}</td>
                      <td className="table__number">{order.totalAmount > 0 ? formatMoney(order.totalAmount, order.currency) : '—'}</td>
                      <td><span className={`badge badge--${status.tone}`}>{status.label}</span></td>
                    </tr>
                  )
                })}
                {data.items.length === 0 && (
                  <tr><td colSpan={columns.length} className="table__empty">Sipariş bulunamadı.</td></tr>
                )}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}

      {creating && <DokumaOrderFormDialog onClose={() => setCreating(false)} onSaved={(id) => navigate(`/dokuma/orders/${id}`)} />}
    </section>
  )
}
