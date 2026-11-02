import { Plus, Search } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { GridHead } from '../../components/grid/GridHead'
import type { GridColumn } from '../../components/grid/GridHead'
import { useGrid } from '../../components/grid/useGrid'
import { Pagination } from '../../components/Pagination'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { useGetProductionRoutesQuery } from './productionRoutesApi'
import { formatSam, routeStatusPresentation } from './routePresentation'
import { RouteStatusBadge } from './RouteStatusBadge'
import { ClearFiltersButton } from '../../components/grid/ClearFiltersButton'

const PAGE_SIZE = 20

const columns: GridColumn[] = [
  { key: 'modelCode', label: 'Model' },
  { key: 'version', label: 'Sürüm', filter: 'number' },
  { key: 'name', label: 'Rota adı' },
  { key: 'operationCount', label: 'Adım', numeric: true },
  { key: 'totalSam', label: 'Toplam SAM (dk)', numeric: true },
  {
    key: 'status',
    label: 'Durum',
    filter: Object.entries(routeStatusPresentation).map(([value, { label }]) => ({ value, label })),
  },
]

export function ProductionRoutesPage() {
  const canManage = usePermission(Permissions.ProductionRouteManage)

  const grid = useGrid()
  const { page, setPage } = grid
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')

  const { data, isLoading, isFetching, error } = useGetProductionRoutesQuery({
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
      <h1 className="visually-hidden">Operasyon Rotaları</h1>

      <form className="toolbar" onSubmit={handleSearch} role="search">
        <input
          type="search"
          placeholder="Model kodu, model adı veya rota adı"
          aria-label="Rota ara"
          value={searchInput}
          onChange={(event) => setSearchInput(event.target.value)}
        />
        <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
        <ClearFiltersButton grid={grid} />
        <div className="toolbar__actions">
          {canManage && <Link to="/production-routes/new" className="button button--primary"><Plus aria-hidden="true" />Yeni rota</Link>}
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
                {data.items.map((route) => (
                  <tr key={route.id}>
                    <td>
                      <Link to={`/production-routes/${route.id}`}><code>{route.modelCode}</code></Link>
                      <div className="muted">{route.modelName}</div>
                    </td>
                    <td>v{route.version}</td>
                    <td>{route.name ?? '—'}</td>
                    <td className="table__number">{route.operationCount}</td>
                    <td className="table__number">{formatSam(route.totalSam)}</td>
                    <td><RouteStatusBadge status={route.status} /></td>
                  </tr>
                ))}
                {data.items.length === 0 && (
                  <tr>
                    <td colSpan={6} className="table__empty">Kayıt bulunamadı.</td>
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
