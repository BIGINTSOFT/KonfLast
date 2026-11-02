import { Plus, Search } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { GridHead } from '../../components/grid/GridHead'
import type { GridColumn } from '../../components/grid/GridHead'
import { useGrid } from '../../components/grid/useGrid'
import { Pagination } from '../../components/Pagination'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDate, formatQuantity } from '../../utils/format'
import { formatMoney } from '../purchasing/purchasingPresentation'
import { salesStatusPresentation } from './salesOrderPresentation'
import { useGetSalesOrdersQuery } from './salesOrdersApi'
import type { SalesOrderStatus } from './salesOrdersApi'
import { ClearFiltersButton } from '../../components/grid/ClearFiltersButton'

const PAGE_SIZE = 20

const columns: GridColumn[] = [
  { key: 'orderNo', label: 'Sipariş no' },
  { key: 'customerName', label: 'Müşteri' },
  { key: 'orderDate', label: 'Sipariş tarihi', filter: 'date' },
  { key: 'dueDate', label: 'Teslim', filter: 'date' },
  { key: 'productCount', label: 'Ürün', numeric: true },
  { key: 'totalQuantity', label: 'Adet', numeric: true },
  { key: 'shippedQuantity', label: 'Sevk edilen', numeric: true },
  { key: 'remainingQuantity', label: 'Kalan', numeric: true },
  { key: 'totalAmount', label: 'Tutar', numeric: true },
  {
    key: 'status',
    label: 'Durum',
    filter: Object.entries(salesStatusPresentation).map(([value, { label }]) => ({ value, label })),
  },
]

export function SalesOrderStatusBadge({ status }: { status: SalesOrderStatus }) {
  const { label, icon, tone } = salesStatusPresentation[status]
  return (
    <span className={`badge badge--${tone}`}>
      <span aria-hidden="true">{icon}</span> {label}
    </span>
  )
}

/** Müşteri siparişleri listesi; onay ve üretime aktarma sipariş sayfasındadır. */
export function SalesOrdersPage() {
  // Sipariş girişi fiyatları görmeyi de gerektirir.
  const canViewPrices = usePermission(Permissions.SalesOrderViewPrices)
  const canManage = usePermission(Permissions.SalesOrderManage) && canViewPrices

  // Ana sayfadaki "yapılacaklar"dan gelindiğinde liste o duruma süzülü açılır.
  const [searchParams] = useSearchParams()
  const urlStatus = searchParams.get('status') ?? ''
  const grid = useGrid(undefined, urlStatus in salesStatusPresentation ? { status: urlStatus } : undefined)
  const { page, setPage } = grid
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [overdueOnly, setOverdueOnly] = useState(searchParams.get('overdue') === 'true')
  const [awaitingMine, setAwaitingMine] = useState(searchParams.get('awaiting') === 'me')

  const { data, isLoading, isFetching, error } = useGetSalesOrdersQuery({
    page,
    pageSize: PAGE_SIZE,
    search: search || undefined,
    overdueOnly: overdueOnly || undefined,
    awaitingMyApproval: awaitingMine || undefined,
    ...grid.params,
  })

  const handleSearch = (event: FormEvent) => {
    event.preventDefault()
    setPage(1)
    setSearch(searchInput.trim())
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">Müşteri Siparişleri</h1>

      <form className="toolbar" onSubmit={handleSearch} role="search">
        <input
          type="search"
          placeholder="Sipariş no veya müşteri"
          aria-label="Sipariş ara"
          value={searchInput}
          onChange={(event) => setSearchInput(event.target.value)}
        />
        <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
        <label className="checkbox">
          <input
            type="checkbox"
            checked={overdueOnly}
            onChange={(event) => {
              setPage(1)
              setOverdueOnly(event.target.checked)
            }}
          />
          Yalnızca gecikenler
        </label>
        <label className="checkbox">
          <input
            type="checkbox"
            checked={awaitingMine}
            onChange={(event) => {
              setPage(1)
              setAwaitingMine(event.target.checked)
            }}
          />
          Onayımı bekleyenler
        </label>
        <ClearFiltersButton grid={grid} />
        <div className="toolbar__actions">
          {canManage && (
            <Link to="/sales-orders/new" className="button button--primary"><Plus aria-hidden="true" />Yeni sipariş</Link>
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
                {data.items.map((order) => (
                  <tr key={order.id} className={order.isOverdue ? 'table__row--overdue' : undefined}>
                    <td>
                      <Link to={`/sales-orders/${order.id}`}><code>{order.orderNo}</code></Link>
                      {order.customerOrderNo && <div className="muted">{order.customerOrderNo}</div>}
                    </td>
                    <td>{order.customerName}</td>
                    <td>{formatDate(order.orderDate)}</td>
                    <td>
                      {formatDate(order.dueDate)}
                      {order.isOverdue && <span className="badge badge--danger order-overdue" title="Teslim tarihi geçti, sevk edilecek adet var">Gecikti</span>}
                    </td>
                    <td className="table__number">{order.productCount}</td>
                    <td className="table__number">{formatQuantity(order.totalQuantity)}</td>
                    <td className="table__number">{formatQuantity(order.shippedQuantity)}</td>
                    <td className="table__number">{order.remainingQuantity > 0 ? formatQuantity(order.remainingQuantity) : <span className="muted">—</span>}</td>
                    <td className="table__number">{formatMoney(order.totalAmount, order.currency)}</td>
                    <td>
                      <SalesOrderStatusBadge status={order.status} />
                      {order.status === 'APPROVED' && order.shippedQuantity > 0 && (
                        <span className="badge badge--warning order-overdue">Kısmi sevk</span>
                      )}
                    </td>
                  </tr>
                ))}
                {data.items.length === 0 && (
                  <tr>
                    <td colSpan={columns.length} className="table__empty">Kayıt bulunamadı.</td>
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
