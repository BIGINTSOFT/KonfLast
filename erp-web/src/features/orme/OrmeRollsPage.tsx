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
import { ormeRollStatusPresentation, useGetOrmeRollsQuery } from './ormeKnitApi'

const PAGE_SIZE = 20

/** Makineden çıkan ham toplar. */
export function OrmeRollsPage() {
  const grid = useGrid()
  const { page, setPage } = grid
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const { data, error, isLoading, isFetching } = useGetOrmeRollsQuery({ page, pageSize: PAGE_SIZE, search: search || undefined, ...grid.params })

  const columns: GridColumn[] = [
    { key: 'rollNo', label: 'Top no' },
    { key: 'cutOn', label: 'Tarih', filter: false },
    { key: 'machineCode', label: 'Makine' },
    { key: 'knitOrderNo', label: 'Emir' },
    { key: 'orderNo', label: 'Sipariş' },
    { key: 'customerName', label: 'Müşteri' },
    { key: 'fabricCode', label: 'Kumaş' },
    { key: 'kilograms', label: 'Kilo', numeric: true },
    { key: 'meters', label: 'Metre', numeric: true },
    { key: 'cutByName', label: 'Kaydeden' },
    { key: 'status', label: 'Durum', filter: Object.entries(ormeRollStatusPresentation).map(([value, { label }]) => ({ value, label })) },
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
        <input type="search" placeholder="Top no, emir, makine, sipariş ya da kumaş" aria-label="Top ara" value={searchInput} onChange={(event) => setSearchInput(event.target.value)} />
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
                    <td>{roll.machineCode}</td>
                    <td><Link to={`/orme/knit-orders/${roll.knitOrderId}`}><code>{roll.knitOrderNo}</code></Link></td>
                    <td><Link to={`/orme/orders/${roll.orderId}`}>{roll.orderNo}</Link></td>
                    <td>{roll.customerName ?? <span className="muted">Stoğa üretim</span>}</td>
                    <td>{roll.fabricCode} — {roll.fabricName}</td>
                    <td className="table__number">{formatQuantity(roll.kilograms)}</td>
                    <td className="table__number">
                      {roll.meters !== null
                        ? formatQuantity(roll.meters)
                        : roll.estimatedMeters !== null ? <span className="muted" title="Gramaj ve enden tahmin">~{formatQuantity(roll.estimatedMeters)}</span> : '—'}
                    </td>
                    <td>{roll.cutByName ?? <span className="muted">Ofis</span>}</td>
                    <td><span className={`badge badge--${ormeRollStatusPresentation[roll.status].tone}`}>{ormeRollStatusPresentation[roll.status].label}</span></td>
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
