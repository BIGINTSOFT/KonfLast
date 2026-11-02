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
import { QuotationStatusBadge } from './QuotationBadges'
import { QuotationFormDialog } from './QuotationFormDialog'
import { useGetQuotationsQuery } from './quotationsApi'
import { quotationStatusPresentation } from './quotationPresentation'

const PAGE_SIZE = 25

const columns: GridColumn[] = [
  { key: 'quotationNo', label: 'Teklif no' },
  { key: 'customerName', label: 'Müşteri / aday' },
  { key: 'quotationDate', label: 'Tarih', filter: 'date' },
  { key: 'validUntil', label: 'Geçerlilik', filter: 'date' },
  { key: 'totalQuantity', label: 'Adet', numeric: true },
  { key: 'totalAmount', label: 'Tutar', numeric: true },
  {
    key: 'status',
    label: 'Durum',
    filter: Object.entries(quotationStatusPresentation).map(([value, { label }]) => ({ value, label })),
  },
  { key: 'salesOrderNo', label: 'Sipariş' },
]

/** Teklifler: ön maliyetli fiyat teklifleri, durumları ve kabulde açılan siparişler. */
export function QuotationsPage() {
  const canManage = usePermission(Permissions.QuotationManage)
  const navigate = useNavigate()
  const grid = useGrid()
  const { page, setPage } = grid
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [creating, setCreating] = useState(false)

  const { data, error, isLoading, isFetching } = useGetQuotationsQuery({
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
      <h1 className="visually-hidden">Teklifler</h1>

      <form className="toolbar" onSubmit={handleSearch} role="search">
        <input
          type="search"
          placeholder="Teklif no, müşteri ya da not"
          aria-label="Teklif ara"
          value={searchInput}
          onChange={(event) => setSearchInput(event.target.value)}
        />
        <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
        <ClearFiltersButton grid={grid} />
        <div className="toolbar__actions">
          {canManage && (
            <button type="button" className="button button--primary" onClick={() => setCreating(true)}>
              <Plus aria-hidden="true" />
              Yeni teklif
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
                {data.items.map((quotation) => (
                  <tr key={quotation.id}>
                    <td><Link to={`/quotations/${quotation.id}`}><code>{quotation.quotationNo}</code></Link></td>
                    <td>
                      {quotation.customerName}
                      {quotation.isProspect && <span className="muted"> (aday)</span>}
                    </td>
                    <td>{formatDate(quotation.quotationDate)}</td>
                    <td>
                      {formatDate(quotation.validUntil)}
                      {quotation.isPastValidity && <span className="badge badge--warning">Geçti</span>}
                    </td>
                    <td className="table__number">{formatQuantity(quotation.totalQuantity)}</td>
                    <td className="table__number">{formatMoney(quotation.totalAmount, quotation.currency)}</td>
                    <td><QuotationStatusBadge status={quotation.status} /></td>
                    <td>{quotation.salesOrderId ? <Link to={`/sales-orders/${quotation.salesOrderId}`}>{quotation.salesOrderNo}</Link> : '—'}</td>
                  </tr>
                ))}
                {data.items.length === 0 && (
                  <tr>
                    <td colSpan={columns.length} className="table__empty">Teklif bulunamadı.</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}

      {creating && (
        <QuotationFormDialog
          onClose={() => setCreating(false)}
          onSaved={(quotation) => void navigate(`/quotations/${quotation.id}`)}
        />
      )}
    </section>
  )
}
