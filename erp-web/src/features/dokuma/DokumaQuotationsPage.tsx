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
import { DokumaQuotationFormDialog } from './DokumaQuotationFormDialog'
import { dokumaOrderOwnershipLabels } from './dokumaOrdersApi'
import { dokumaQuotationStatusPresentation, useGetDokumaQuotationsQuery } from './dokumaQuotationsApi'

const PAGE_SIZE = 20

/** Dokuma teklifleri, en yeniden eskiye. Geçerlilik tarihi geçen gönderilmiş teklif "süresi doldu" görünür. */
export function DokumaQuotationsPage() {
  const navigate = useNavigate()
  const canManage = usePermission(Permissions.DokumaQuotationManage)
  const grid = useGrid()
  const { page, setPage } = grid
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [creating, setCreating] = useState(false)

  const { data, error, isLoading, isFetching } = useGetDokumaQuotationsQuery({
    page,
    pageSize: PAGE_SIZE,
    search: search || undefined,
    ...grid.params,
  })

  const columns: GridColumn[] = [
    { key: 'quotationNo', label: 'Teklif no' },
    { key: 'quotationDate', label: 'Tarih', filter: false },
    { key: 'validUntil', label: 'Geçerlilik', filter: false },
    { key: 'partyName', label: 'Müşteri / aday' },
    {
      key: 'ownership',
      label: 'İş türü',
      filter: Object.entries(dokumaOrderOwnershipLabels).map(([value, label]) => ({ value, label })),
    },
    { key: 'lineCount', label: 'Satır', numeric: true },
    { key: 'totalMeters', label: 'Metre', numeric: true },
    { key: 'totalAmount', label: 'Tutar', numeric: true },
    {
      key: 'status',
      label: 'Durum',
      filter: Object.entries(dokumaQuotationStatusPresentation).map(([value, { label }]) => ({ value, label })),
    },
    { key: 'orderNo', label: 'Sipariş' },
  ]

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
          placeholder="Teklif no, müşteri ya da aday firma"
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
                {data.items.map((quotation) => {
                  const status = dokumaQuotationStatusPresentation[quotation.status]
                  return (
                    <tr key={quotation.id}>
                      <td><Link to={`/dokuma/quotations/${quotation.id}`}><code>{quotation.quotationNo}</code></Link></td>
                      <td>{formatDate(quotation.quotationDate)}</td>
                      <td>{formatDate(quotation.validUntil)}</td>
                      <td>{quotation.partyName}{quotation.isProspect && <span className="muted"> (aday)</span>}</td>
                      <td>{dokumaOrderOwnershipLabels[quotation.ownership]}</td>
                      <td className="table__number">{quotation.lineCount}</td>
                      <td className="table__number">{formatQuantity(quotation.totalMeters)}</td>
                      <td className="table__number">{formatMoney(quotation.totalAmount, quotation.currency)}</td>
                      <td><span className={`badge badge--${status.tone}`}>{status.label}</span></td>
                      <td>{quotation.orderId ? <Link to={`/dokuma/orders/${quotation.orderId}`}>{quotation.orderNo}</Link> : '—'}</td>
                    </tr>
                  )
                })}
                {data.items.length === 0 && (
                  <tr><td colSpan={columns.length} className="table__empty">Teklif bulunamadı.</td></tr>
                )}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}

      {creating && <DokumaQuotationFormDialog onClose={() => setCreating(false)} onSaved={(id) => navigate(`/dokuma/quotations/${id}`)} />}
    </section>
  )
}
