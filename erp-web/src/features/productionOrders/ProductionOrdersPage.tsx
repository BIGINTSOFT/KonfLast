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
import { OrderStatusBadge } from './OrderStatusBadge'
import { statusPresentation } from './orderPresentation'
import { useGetProductionOrdersQuery } from './productionOrdersApi'
import { ClearFiltersButton } from '../../components/grid/ClearFiltersButton'

const PAGE_SIZE = 20

const columns: GridColumn[] = [
  { key: 'orderNo', label: 'İş emri no' },
  { key: 'customerName', label: 'Müşteri' },
  { key: 'productCode', label: 'Ürün' },
  { key: 'orderDate', label: 'İş emri tarihi', filter: 'date' },
  { key: 'dueDate', label: 'Termin', filter: 'date' },
  { key: 'totalQuantity', label: 'Adet', numeric: true },
  {
    key: 'status',
    label: 'Durum',
    filter: Object.entries(statusPresentation).map(([value, { label }]) => ({ value, label })),
  },
]

export function ProductionOrdersPage() {
  const canCreate = usePermission(Permissions.ProductionOrderCreate)

  // Ana sayfadaki "yapılacaklar"dan gelindiğinde liste o duruma süzülü açılır (durum sütununun süzgeci).
  const [searchParams] = useSearchParams()
  const urlStatus = searchParams.get('status') ?? ''
  const grid = useGrid(undefined, urlStatus in statusPresentation ? { status: urlStatus } : undefined)
  const { page, setPage } = grid
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')

  const { data, isLoading, isFetching, error } = useGetProductionOrdersQuery({
    page,
    pageSize: PAGE_SIZE,
    search: search || undefined,
    ...grid.params,
  })

  const handleSearch = (event: FormEvent) => {
    event.preventDefault()
    setPage(1)
    setSearch(searchInput.trim())
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">İş Emirleri</h1>

      <form className="toolbar" onSubmit={handleSearch} role="search">
        <input
          type="search"
          placeholder="İş emri no, müşteri, ürün kodu"
          aria-label="İş emri ara"
          value={searchInput}
          onChange={(event) => setSearchInput(event.target.value)}
        />
        <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
        <ClearFiltersButton grid={grid} />
        <div className="toolbar__actions">
          {canCreate && (
            <Link to="/production-orders/new" className="button button--primary"><Plus aria-hidden="true" />Yeni iş emri</Link>
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
                  <tr key={order.id}>
                    <td>
                      <Link to={`/production-orders/${order.id}`}><code>{order.orderNo}</code></Link>
                      {order.salesOrderNo === null && (
                        <span className="badge badge--neutral" title="Müşteri siparişine bağlı değil; fiyat ve termin sipariş üzerinden izlenmez">
                          Siparişsiz
                        </span>
                      )}
                      {order.customerOrderNo && <div className="muted">{order.customerOrderNo}</div>}
                    </td>
                    <td>{order.customerName}</td>
                    <td>
                      <code>{order.productCode}</code>
                      <div className="muted">{order.productName}</div>
                    </td>
                    <td>{formatDate(order.orderDate)}</td>
                    <td>{formatDate(order.dueDate)}</td>
                    <td className="table__number">{formatQuantity(order.totalQuantity)}</td>
                    <td><OrderStatusBadge status={order.status} /></td>
                  </tr>
                ))}
                {data.items.length === 0 && (
                  <tr>
                    <td colSpan={7} className="table__empty">Kayıt bulunamadı.</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
          <Pagination
            page={data.page}
            totalPages={data.totalPages}
            totalCount={data.totalCount}
            onPageChange={setPage}
          />
        </>
      )}
    </section>
  )
}
