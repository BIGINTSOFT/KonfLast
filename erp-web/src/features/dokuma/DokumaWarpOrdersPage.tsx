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
import { formatDate, formatQuantity } from '../../utils/format'
import { useGetDokumaWarpOrdersQuery, warpOrderStatusPresentation } from './dokumaWarpApi'

const PAGE_SIZE = 20

/** Çözgü emirleri; yeni emir onaylı siparişin satırından açılır. */
export function DokumaWarpOrdersPage() {
  const grid = useGrid()
  const { page, setPage } = grid
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const { data, error, isLoading, isFetching } = useGetDokumaWarpOrdersQuery({ page, pageSize: PAGE_SIZE, search: search || undefined, ...grid.params })

  const columns: GridColumn[] = [
    { key: 'warpNo', label: 'Çözgü no' },
    { key: 'plannedDate', label: 'Plan tarihi', filter: false },
    { key: 'orderNo', label: 'Sipariş' },
    { key: 'customerName', label: 'Müşteri' },
    { key: 'articleCode', label: 'Artikel' },
    { key: 'plannedBeamCount', label: 'Plan levent', numeric: true },
    { key: 'metersPerBeam', label: 'Levent metresi', numeric: true },
    { key: 'ends', label: 'Tel', numeric: true },
    { key: 'beamCount', label: 'Çıkan levent', numeric: true },
    { key: 'issuedKilograms', label: 'Verilen iplik (kg)', numeric: true },
    {
      key: 'status',
      label: 'Durum',
      filter: Object.entries(warpOrderStatusPresentation).map(([value, { label }]) => ({ value, label })),
    },
  ]

  const handleSearch = (event: FormEvent) => {
    event.preventDefault()
    setPage(1)
    setSearch(searchInput.trim())
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">Çözgü Emirleri</h1>
      <form className="toolbar" onSubmit={handleSearch} role="search">
        <input type="search" placeholder="Çözgü no, sipariş, artikel ya da müşteri" aria-label="Çözgü emri ara" value={searchInput} onChange={(event) => setSearchInput(event.target.value)} />
        <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
        <ClearFiltersButton grid={grid} />
        <p className="toolbar__actions muted">Yeni çözgü emri onaylı siparişin satırından açılır.</p>
      </form>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {data && (
        <>
          <div className="table-wrap" aria-busy={isFetching}>
            <table className="table">
              <GridHead columns={columns} grid={grid} />
              <tbody>
                {data.items.map((warp) => {
                  const status = warpOrderStatusPresentation[warp.status]
                  return (
                    <tr key={warp.id}>
                      <td><Link to={`/dokuma/warp-orders/${warp.id}`}><code>{warp.warpNo}</code></Link></td>
                      <td>{formatDate(warp.plannedDate)}</td>
                      <td><Link to={`/dokuma/orders/${warp.orderId}`}>{warp.orderNo}</Link></td>
                      <td>{warp.customerName ?? <span className="muted">Stoğa üretim</span>}</td>
                      <td>{warp.articleCode} — {warp.articleName}</td>
                      <td className="table__number">{warp.plannedBeamCount}</td>
                      <td className="table__number">{formatQuantity(warp.metersPerBeam)}</td>
                      <td className="table__number">{formatQuantity(warp.ends)}</td>
                      <td className="table__number">{warp.beamCount}</td>
                      <td className="table__number">{formatQuantity(warp.issuedKilograms)}</td>
                      <td><span className={`badge badge--${status.tone}`}>{status.label}</span></td>
                    </tr>
                  )
                })}
                {data.items.length === 0 && <tr><td colSpan={columns.length} className="table__empty">Çözgü emri bulunamadı.</td></tr>}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}
    </section>
  )
}
