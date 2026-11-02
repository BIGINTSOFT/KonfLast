import { Plus, Search } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { GridHead } from '../../components/grid/GridHead'
import type { GridColumn } from '../../components/grid/GridHead'
import { useGrid } from '../../components/grid/useGrid'
import { Pagination } from '../../components/Pagination'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDate } from '../../utils/format'
import { PurchaseOrderDialog } from './PurchaseOrderDialog'
import { useGetPurchaseOrdersQuery } from './purchasingApi'
import { formatMoney, purchaseStatusPresentation } from './purchasingPresentation'
import { ClearFiltersButton } from '../../components/grid/ClearFiltersButton'

const PAGE_SIZE = 20

/** Satın alma siparişleri listesi; ayrıntı, onay ve mal kabul sipariş sayfasındadır. */
export function PurchaseOrdersPage() {
  // Sipariş girişi fiyatları görmeyi de gerektirir.
  const canViewPrices = usePermission(Permissions.PurchaseViewPrices)
  const canManage = usePermission(Permissions.PurchaseManage) && canViewPrices
  const navigate = useNavigate()

  // Ana sayfadaki "yapılacaklar"dan gelindiğinde liste o duruma süzülü açılır.
  const [searchParams] = useSearchParams()

  const grid = useGrid(undefined, searchParams.get('status') === 'DRAFT' ? { status: 'DRAFT' } : undefined)
  const { page, setPage } = grid
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [receivableOnly, setReceivableOnly] = useState(searchParams.get('receivable') === 'true')
  const [creating, setCreating] = useState(false)

  const { data, error, isLoading, isFetching } = useGetPurchaseOrdersQuery({
    page,
    pageSize: PAGE_SIZE,
    search: search || undefined,
    receivable: receivableOnly || undefined,
    ...grid.params,
  })

  const columns: GridColumn[] = [
    { key: 'orderNo', label: 'Sipariş no' },
    { key: 'supplierName', label: 'Tedarikçi' },
    { key: 'orderDate', label: 'Sipariş tarihi', filter: 'date' },
    { key: 'expectedDate', label: 'Beklenen teslim', filter: 'date' },
    {
      key: 'status',
      label: 'Durum',
      filter: Object.entries(purchaseStatusPresentation).map(([value, { label }]) => ({ value, label })),
    },
    { key: 'itemCount', label: 'Kalem', numeric: true },
    { key: 'totalAmount', label: 'Tutar', numeric: true },
  ]

  const handleSearch = (event: FormEvent) => {
    event.preventDefault()
    setPage(1)
    setSearch(searchInput.trim())
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">Satın Alma Siparişleri</h1>

      <form className="toolbar" onSubmit={handleSearch} role="search">
        <input
          type="search"
          placeholder="Sipariş no veya tedarikçi"
          aria-label="Sipariş ara"
          value={searchInput}
          onChange={(event) => setSearchInput(event.target.value)}
        />
        <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
        <ClearFiltersButton grid={grid} />
        <label className="checkbox">
          <input
            type="checkbox"
            checked={receivableOnly}
            onChange={(event) => {
              setPage(1)
              setReceivableOnly(event.target.checked)
            }}
          />
          Yalnızca mal kabul bekleyenler
        </label>
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
                  const presentation = purchaseStatusPresentation[order.status]
                  return (
                    <tr key={order.id}>
                      <td><Link to={`/purchase-orders/${order.id}`}><code>{order.orderNo}</code></Link></td>
                      <td>{order.supplierName}</td>
                      <td>{formatDate(order.orderDate)}</td>
                      <td>{formatDate(order.expectedDate)}</td>
                      <td>
                        <span className={`badge badge--${presentation.tone}`}>
                          <span aria-hidden="true">{presentation.icon}</span> {presentation.label}
                        </span>
                      </td>
                      <td className="table__number">{order.itemCount}</td>
                      <td className="table__number">{formatMoney(order.totalAmount, order.currency)}</td>
                    </tr>
                  )
                })}
                {data.items.length === 0 && (
                  <tr>
                    <td colSpan={7} className="table__empty">Sipariş bulunamadı.</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}

      {creating && (
        <PurchaseOrderDialog onClose={() => setCreating(false)} onSaved={(order) => void navigate(`/purchase-orders/${order.id}`)} />
      )}
    </section>
  )
}
