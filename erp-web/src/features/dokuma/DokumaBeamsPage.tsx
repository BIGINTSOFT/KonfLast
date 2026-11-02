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
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDate, formatQuantity } from '../../utils/format'
import { DokumaBeamActions } from './DokumaBeamActions'
import { beamStatusPresentation, drawingMethodLabels, sizingModeLabels, sizingText, useGetDokumaBeamsQuery } from './dokumaWarpApi'

const PAGE_SIZE = 20

/** Bütün leventler: haşıl ve tahar / düğümle tezgâha hazırlanır. */
export function DokumaBeamsPage() {
  const canManage = usePermission(Permissions.DokumaWarpManage)
  const grid = useGrid()
  const { page, setPage } = grid
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const { data, error, isLoading, isFetching } = useGetDokumaBeamsQuery({ page, pageSize: PAGE_SIZE, search: search || undefined, ...grid.params })

  const columns: GridColumn[] = [
    { key: 'beamNo', label: 'Levent no' },
    { key: 'warpNo', label: 'Çözgü' },
    { key: 'orderNo', label: 'Sipariş' },
    { key: 'articleCode', label: 'Artikel' },
    { key: 'loomCode', label: 'Tezgâh' },
    { key: 'meters', label: 'Metre', numeric: true },
    { key: 'ends', label: 'Tel', numeric: true },
    { key: 'sizingMode', label: 'Haşıl', filter: Object.entries(sizingModeLabels).map(([value, label]) => ({ value, label })) },
    { key: 'drawingMethod', label: 'Hazırlık', filter: false },
    { key: 'status', label: 'Durum', filter: Object.entries(beamStatusPresentation).map(([value, { label }]) => ({ value, label })) },
    ...(canManage ? [{ label: '', ariaLabel: 'İşlem', className: 'table__actions' }] : []),
  ]

  const handleSearch = (event: FormEvent) => {
    event.preventDefault()
    setPage(1)
    setSearch(searchInput.trim())
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">Leventler</h1>
      <form className="toolbar" onSubmit={handleSearch} role="search">
        <input type="search" placeholder="Levent no, çözgü, sipariş ya da artikel" aria-label="Levent ara" value={searchInput} onChange={(event) => setSearchInput(event.target.value)} />
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
                {data.items.map((beam) => {
                  const status = beamStatusPresentation[beam.status]
                  return (
                    <tr key={beam.id}>
                      <td>
                        {beam.status === 'ON_LOOM' || beam.status === 'FINISHED'
                          ? <Link to={`/dokuma/weaving/beams/${beam.id}`}><code>{beam.beamNo}</code></Link>
                          : <code>{beam.beamNo}</code>}
                      </td>
                      <td><Link to={`/dokuma/warp-orders/${beam.warpOrderId}`}>{beam.warpNo}</Link></td>
                      <td>{beam.orderNo}</td>
                      <td>{beam.articleCode} — {beam.articleName}</td>
                      <td>{beam.loomCode ?? '—'}</td>
                      <td className="table__number">{formatQuantity(beam.meters)}</td>
                      <td className="table__number">{formatQuantity(beam.ends)}</td>
                      <td>{sizingText(beam)}</td>
                      <td>{beam.drawingMethod ? `${drawingMethodLabels[beam.drawingMethod]} · ${beam.readyOn ? formatDate(beam.readyOn) : ''}` : '—'}</td>
                      <td><span className={`badge badge--${status.tone}`}>{status.label}</span></td>
                      {canManage && <td className="table__actions"><DokumaBeamActions beam={beam} /></td>}
                    </tr>
                  )
                })}
                {data.items.length === 0 && <tr><td colSpan={columns.length} className="table__empty">Levent bulunamadı.</td></tr>}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}
    </section>
  )
}
