import { Search } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { ClearFiltersButton } from '../../components/grid/ClearFiltersButton'
import { GridHead } from '../../components/grid/GridHead'
import type { GridColumn } from '../../components/grid/GridHead'
import { useGrid } from '../../components/grid/useGrid'
import { Pagination } from '../../components/Pagination'
import { formatDateTime } from '../../utils/format'
import { labRequestStatusPresentation, useGetLabRequestsQuery } from './boyahaneLabApi'

const PAGE_SIZE = 20

/** Laboratuvar talepleri. Talep sipariş satırından açılır. */
export function LabRequestsPage() {
  const grid = useGrid()
  const { page, setPage } = grid
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const { data, error, isLoading, isFetching } = useGetLabRequestsQuery({ page, pageSize: PAGE_SIZE, search: search || undefined, ...grid.params })

  const columns: GridColumn[] = [
    { key: 'requestNo', label: 'Talep no' },
    { key: 'orderNo', label: 'Sipariş' },
    { key: 'customerName', label: 'Müşteri' },
    { key: 'itemCode', label: 'Kart' },
    { key: 'colorName', label: 'Renk' },
    { key: 'colorCode', label: 'Renk kodu' },
    { key: 'trialCount', label: 'Deneme', numeric: true },
    { key: 'approvedTrialCode', label: 'Reçete', filter: false },
    {
      key: 'status',
      label: 'Durum',
      filter: Object.entries(labRequestStatusPresentation).map(([value, { label }]) => ({ value, label })),
    },
    { key: 'createdAt', label: 'Açılış', filter: false },
  ]

  const handleSearch = (event: FormEvent) => {
    event.preventDefault()
    setPage(1)
    setSearch(searchInput.trim())
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">Laboratuvar</h1>
      <form className="toolbar" onSubmit={handleSearch} role="search">
        <input type="search" placeholder="Talep no, sipariş, renk ya da müşteri" aria-label="Talep ara" value={searchInput} onChange={(event) => setSearchInput(event.target.value)} />
        <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
        <ClearFiltersButton grid={grid} />
      </form>

      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />

      {data && (
        <>
          <div className="table-wrap" aria-busy={isFetching}>
            <table className="table">
              <GridHead columns={columns} grid={grid} />
              <tbody>
                {data.items.map((request) => {
                  const status = labRequestStatusPresentation[request.status]
                  return (
                    <tr key={request.id}>
                      <td><Link to={`/boyahane/lab/${request.id}`}><code>{request.requestNo}</code></Link></td>
                      <td><Link to={`/boyahane/orders/${request.orderId}`}>{request.orderNo}</Link></td>
                      <td>{request.customerName ?? <span className="muted">Stoğa üretim</span>}</td>
                      <td>{request.itemCode}</td>
                      <td>{request.colorName ?? '—'}</td>
                      <td>{request.colorCode ?? '—'}</td>
                      <td className="table__number">{request.trialCount}</td>
                      <td>{request.approvedTrialCode ?? '—'}</td>
                      <td><span className={`badge badge--${status.tone}`}>{status.label}</span></td>
                      <td>{formatDateTime(request.createdAt)}</td>
                    </tr>
                  )
                })}
                {data.items.length === 0 && <tr><td colSpan={columns.length} className="table__empty">Talep bulunamadı. Talep, sipariş satırındaki "Lab" düğmesiyle açılır.</td></tr>}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}
    </section>
  )
}
