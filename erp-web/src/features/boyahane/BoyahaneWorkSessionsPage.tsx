import { Search } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { ErrorAlert } from '../../components/FormControls'
import { ClearFiltersButton } from '../../components/grid/ClearFiltersButton'
import { GridHead } from '../../components/grid/GridHead'
import type { GridColumn } from '../../components/grid/GridHead'
import { useGrid } from '../../components/grid/useGrid'
import { Pagination } from '../../components/Pagination'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDateTime } from '../../utils/format'
import { pauseKindLabels, sessionEndReasonLabels, useCloseBoyahaneWorkSessionMutation, useGetBoyahaneWorkSessionsQuery } from './boyahaneFloorApi'

const PAGE_SIZE = 20

/** Tablet oturumları. Süren adımı olan personelin oturumu buradan kapatılır; adım işlemde kalır, başkası bitirir. */
export function BoyahaneWorkSessionsPage() {
  const canManage = usePermission(Permissions.BoyahaneFloorManage)
  const grid = useGrid()
  const { page, setPage } = grid
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const { data, error, isLoading, isFetching } = useGetBoyahaneWorkSessionsQuery({ page, pageSize: PAGE_SIZE, search: search || undefined, ...grid.params })
  const [close, closeState] = useCloseBoyahaneWorkSessionMutation()

  const columns: GridColumn[] = [
    { key: 'operatorName', label: 'Operatör' },
    { key: 'tabletCode', label: 'Tablet' },
    { key: 'machineCode', label: 'Makine' },
    { key: 'startedAt', label: 'Başlangıç', filter: false },
    { key: 'endedAt', label: 'Bitiş', filter: false },
    { key: 'pauseKind', label: 'Durum', filter: false },
    { key: 'hasWorkInProgress', label: 'Süren adım', filter: false },
    { label: '', ariaLabel: 'İşlemler' },
  ]

  const handleSearch = (event: FormEvent) => {
    event.preventDefault()
    setPage(1)
    setSearch(searchInput.trim())
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">Çalışma Oturumları</h1>
      <form className="toolbar" onSubmit={handleSearch} role="search">
        <input type="search" placeholder="Operatör, tablet ya da makine" aria-label="Oturum ara" value={searchInput} onChange={(event) => setSearchInput(event.target.value)} />
        <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
        <ClearFiltersButton grid={grid} />
      </form>

      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error ?? closeState.error} />

      {data && (
        <>
          <div className="table-wrap" aria-busy={isFetching}>
            <table className="table">
              <GridHead columns={columns} grid={grid} />
              <tbody>
                {data.items.map((session) => (
                  <tr key={session.id}>
                    <td>{session.operatorName} <span className="muted">{session.operatorCode}</span></td>
                    <td>{session.tabletCode}</td>
                    <td>{session.machineCode ?? <span className="muted">Kontrol noktası</span>}</td>
                    <td>{formatDateTime(session.startedAt)}</td>
                    <td>{session.endedAt ? formatDateTime(session.endedAt) : '—'}</td>
                    <td>
                      {session.endReason
                        ? sessionEndReasonLabels[session.endReason]
                        : session.pauseKind ? pauseKindLabels[session.pauseKind] : 'Açık'}
                    </td>
                    <td>{session.hasWorkInProgress ? 'Var' : '—'}</td>
                    <td>
                      {canManage && !session.endedAt && (
                        <button type="button" className="button" disabled={closeState.isLoading} onClick={() => close(session.id)}>Oturumu kapat</button>
                      )}
                    </td>
                  </tr>
                ))}
                {data.items.length === 0 && <tr><td colSpan={columns.length} className="table__empty">Oturum bulunamadı.</td></tr>}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}
    </section>
  )
}
