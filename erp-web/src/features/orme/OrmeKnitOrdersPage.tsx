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
import { ormeKnitOrderStatusPresentation, useGetOrmeKnitOrdersQuery } from './ormeKnitApi'

const PAGE_SIZE = 20

/** Örme emirleri; yeni emir onaylı siparişin satırından açılır. */
export function OrmeKnitOrdersPage() {
  const grid = useGrid()
  const { page, setPage } = grid
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const { data, error, isLoading, isFetching } = useGetOrmeKnitOrdersQuery({ page, pageSize: PAGE_SIZE, search: search || undefined, ...grid.params })

  const columns: GridColumn[] = [
    { key: 'knitOrderNo', label: 'Emir no' },
    { key: 'plannedDate', label: 'Plan tarihi', filter: false },
    { key: 'orderNo', label: 'Sipariş' },
    { key: 'customerName', label: 'Müşteri' },
    { key: 'fabricCode', label: 'Kumaş' },
    { key: 'plannedKilograms', label: 'Plan (kg)', numeric: true },
    { key: 'knittedKilograms', label: 'Örülen (kg)', numeric: true },
    { key: 'rollCount', label: 'Top', numeric: true },
    { key: 'runningMachineCount', label: 'Makine', numeric: true },
    { key: 'issuedKilograms', label: 'Verilen iplik (kg)', numeric: true },
    {
      key: 'status',
      label: 'Durum',
      filter: Object.entries(ormeKnitOrderStatusPresentation).map(([value, { label }]) => ({ value, label })),
    },
  ]

  const handleSearch = (event: FormEvent) => {
    event.preventDefault()
    setPage(1)
    setSearch(searchInput.trim())
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">Örme Emirleri</h1>
      <form className="toolbar" onSubmit={handleSearch} role="search">
        <input type="search" placeholder="Emir no, sipariş, kumaş ya da müşteri" aria-label="Örme emri ara" value={searchInput} onChange={(event) => setSearchInput(event.target.value)} />
        <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
        <ClearFiltersButton grid={grid} />
        <p className="toolbar__actions muted">Yeni örme emri onaylı siparişin satırından açılır.</p>
      </form>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {data && (
        <>
          <div className="table-wrap" aria-busy={isFetching}>
            <table className="table">
              <GridHead columns={columns} grid={grid} />
              <tbody>
                {data.items.map((knit) => {
                  const status = ormeKnitOrderStatusPresentation[knit.status]
                  return (
                    <tr key={knit.id}>
                      <td><Link to={`/orme/knit-orders/${knit.id}`}><code>{knit.knitOrderNo}</code></Link></td>
                      <td>{formatDate(knit.plannedDate)}</td>
                      <td><Link to={`/orme/orders/${knit.orderId}`}>{knit.orderNo}</Link></td>
                      <td>{knit.customerName ?? <span className="muted">Stoğa üretim</span>}</td>
                      <td>{knit.fabricCode} — {knit.fabricName}</td>
                      <td className="table__number">{formatQuantity(knit.plannedKilograms)}</td>
                      <td className="table__number">{formatQuantity(knit.knittedKilograms)}</td>
                      <td className="table__number">{knit.rollCount}</td>
                      <td className="table__number">{knit.runningMachineCount}</td>
                      <td className="table__number">{formatQuantity(knit.issuedKilograms)}</td>
                      <td><span className={`badge badge--${status.tone}`}>{status.label}</span></td>
                    </tr>
                  )
                })}
                {data.items.length === 0 && <tr><td colSpan={columns.length} className="table__empty">Örme emri bulunamadı.</td></tr>}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}
    </section>
  )
}
