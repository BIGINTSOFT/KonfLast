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
import { dokumaRollStatusPresentation, useGetDokumaRollsQuery } from './dokumaWeavingApi'

const PAGE_SIZE = 20

/** Tezgâhtan kesilen ham toplar. */
export function DokumaRollsPage() {
  const grid = useGrid()
  const { page, setPage } = grid
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const { data, error, isLoading, isFetching } = useGetDokumaRollsQuery({ page, pageSize: PAGE_SIZE, search: search || undefined, ...grid.params })

  const columns: GridColumn[] = [
    { key: 'rollNo', label: 'Top no' },
    { key: 'cutOn', label: 'Kesim', filter: false },
    { key: 'loomCode', label: 'Tezgâh' },
    { key: 'beamNo', label: 'Levent' },
    { key: 'orderNo', label: 'Sipariş' },
    { key: 'customerName', label: 'Müşteri' },
    { key: 'articleCode', label: 'Artikel' },
    { key: 'meters', label: 'Metre', numeric: true },
    { key: 'kilograms', label: 'Kilo', numeric: true },
    { key: 'cutByName', label: 'Kesen' },
    { key: 'status', label: 'Durum', filter: Object.entries(dokumaRollStatusPresentation).map(([value, { label }]) => ({ value, label })) },
  ]

  const handleSearch = (event: FormEvent) => {
    event.preventDefault()
    setPage(1)
    setSearch(searchInput.trim())
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">Ham Toplar</h1>
      <form className="toolbar" onSubmit={handleSearch} role="search">
        <input type="search" placeholder="Top no, levent, tezgâh, sipariş ya da artikel" aria-label="Top ara" value={searchInput} onChange={(event) => setSearchInput(event.target.value)} />
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
                {data.items.map((roll) => (
                  <tr key={roll.id}>
                    <td><code>{roll.rollNo}</code></td>
                    <td>{formatDate(roll.cutOn)}</td>
                    <td>{roll.loomCode}</td>
                    <td><Link to={`/dokuma/weaving/beams/${roll.beamId}`}>{roll.beamNo}</Link></td>
                    <td>{roll.orderNo}</td>
                    <td>{roll.customerName ?? <span className="muted">Stoğa üretim</span>}</td>
                    <td>{roll.articleCode} — {roll.articleName}</td>
                    <td className="table__number">{formatQuantity(roll.meters)}</td>
                    <td className="table__number">{roll.kilograms !== null ? formatQuantity(roll.kilograms) : '—'}</td>
                    <td>{roll.cutByName ?? <span className="muted">Ofis</span>}</td>
                    <td><span className={`badge badge--${dokumaRollStatusPresentation[roll.status].tone}`}>{dokumaRollStatusPresentation[roll.status].label}</span></td>
                  </tr>
                ))}
                {data.items.length === 0 && <tr><td colSpan={columns.length} className="table__empty">Top bulunamadı.</td></tr>}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}
    </section>
  )
}
