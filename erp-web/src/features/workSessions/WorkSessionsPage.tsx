import { Search } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { isRequestError } from '../../api/envelope'
import { ErrorAlert } from '../../components/FormControls'
import { GridHead } from '../../components/grid/GridHead'
import type { GridColumn } from '../../components/grid/GridHead'
import { useGrid } from '../../components/grid/useGrid'
import { Modal } from '../../components/Modal'
import { Pagination } from '../../components/Pagination'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDateTime } from '../../utils/format'
import { PausesDialog } from './PausesDialog'
import { endReasonLabels, formatDuration, sessionStatusPresentation } from './workSessionPresentation'
import { useCloseWorkSessionMutation, useGetWorkSessionsQuery } from './workSessionsApi'
import type { WorkSession } from './workSessionsApi'
import { ClearFiltersButton } from '../../components/grid/ClearFiltersButton'

const PAGE_SIZE = 25

// Süre sayfalamadan sonra bellekte hesaplanır; sunucu bu sütunu süzemez / sıralayamaz.
const columns: GridColumn[] = [
  { key: 'operatorName', label: 'Operatör' },
  { key: 'machineCode', label: 'Makine' },
  { key: 'tabletCode', label: 'Tablet' },
  { key: 'loginAt', label: 'Giriş', filter: false },
  { key: 'logoutAt', label: 'Çıkış', filter: false },
  { label: 'Süre' },
  {
    key: 'status',
    label: 'Durum',
    filter: Object.entries(sessionStatusPresentation).map(([value, { label }]) => ({ value, label })),
  },
  { label: '', ariaLabel: 'İşlemler' },
]

function CloseDialog({ session, onClose }: { session: WorkSession; onClose: () => void }) {
  const [closeSession, { isLoading, error }] = useCloseWorkSessionMutation()
  // Süren iş nedeniyle kapatılamadıysa, işi bırakarak kapatma seçeneği sunulur.
  const blockedByWork = isRequestError(error) && error.code === 'WORK_IN_PROGRESS'

  const handleClose = async (releaseWork = false) => {
    const result = await closeSession({ id: session.id, releaseWork })
    if (!('error' in result)) {
      onClose()
    }
  }

  return (
    <Modal title="Oturumu kapat" onClose={onClose}>
      <div className="form">
        <p>
          <strong>{session.operatorName}</strong> adlı operatörün <strong>{session.machineCode}</strong> makinesindeki
          oturumu kapatılacak.
        </p>
        <ErrorAlert error={error} />
        {blockedByWork && (
          <p className="alert alert--info">
            Operatör adet bildirmeden ayrıldıysa işi bırakarak kapatabilirsiniz: üretilen adet değişmez, kalan adet
            demet yeniden okutulunca sürdürülür. Yapılmış ama bildirilmemiş parçalar o sırada bildirilmelidir.
          </p>
        )}
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          {blockedByWork && (
            <button type="button" className="button" disabled={isLoading} onClick={() => void handleClose(true)}>
              İşi bırak ve kapat
            </button>
          )}
          <button type="button" className="button button--danger" disabled={isLoading} onClick={() => void handleClose()}>
            {isLoading ? 'Kapatılıyor…' : 'Oturumu kapat'}
          </button>
        </footer>
      </div>
    </Modal>
  )
}

/** Operatörlerin makine başındaki çalışma oturumları: kim, hangi makinede, ne zaman çalıştı. */
export function WorkSessionsPage() {
  const canClose = usePermission(Permissions.WorkSessionClose)
  const grid = useGrid()
  const { page, setPage } = grid
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [openFilter, setOpenFilter] = useState<'' | 'true' | 'false'>('true')
  const [closing, setClosing] = useState<WorkSession | null>(null)
  const [pausesOf, setPausesOf] = useState<WorkSession | null>(null)

  const { data, isLoading, isFetching, error } = useGetWorkSessionsQuery({
    page,
    pageSize: PAGE_SIZE,
    search: search || undefined,
    isOpen: openFilter === '' ? undefined : openFilter === 'true',
    ...grid.params,
  })

  const handleSearch = (event: FormEvent) => {
    event.preventDefault()
    setPage(1)
    setSearch(searchInput.trim())
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">Çalışma Oturumları</h1>

      <form className="toolbar" onSubmit={handleSearch} role="search">
        <input
          type="search"
          placeholder="Operatör adı / sicili veya makine kodu"
          aria-label="Oturum ara"
          value={searchInput}
          onChange={(event) => setSearchInput(event.target.value)}
        />
        <select
          aria-label="Oturum durumu"
          value={openFilter}
          onChange={(event) => {
            setPage(1)
            setOpenFilter(event.target.value as '' | 'true' | 'false')
          }}
        >
          <option value="true">Açık oturumlar</option>
          <option value="false">Kapanmış oturumlar</option>
          <option value="">Tümü</option>
        </select>
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
                {data.items.map((session) => {
                  const status = sessionStatusPresentation[session.status]
                  return (
                    <tr key={session.id}>
                      <td>
                        {session.operatorName} <span className="muted">({session.operatorCode})</span>
                      </td>
                      <td><code>{session.machineCode}</code></td>
                      <td>{session.tabletCode}</td>
                      <td>{formatDateTime(session.loginAt)}</td>
                      <td>{formatDateTime(session.logoutAt)}</td>
                      <td>{formatDuration(session.durationMinutes)}</td>
                      <td>
                        <span className={`badge badge--${status.tone}`}>
                          <span aria-hidden="true">{status.icon}</span> {status.label}
                        </span>
                        {session.endReason && (
                          <div className="muted">
                            {endReasonLabels[session.endReason]}
                            {session.endReason === 'CLOSED_BY_SUPERVISOR' && session.closedBy ? ` (${session.closedBy})` : ''}
                          </div>
                        )}
                      </td>
                      <td className="table__actions">
                        <button type="button" className="button" onClick={() => setPausesOf(session)}>
                          Duraklamalar
                        </button>
                        {canClose && session.canClose && (
                          <button type="button" className="button" onClick={() => setClosing(session)}>
                            Oturumu kapat
                          </button>
                        )}
                      </td>
                    </tr>
                  )
                })}
                {data.items.length === 0 && (
                  <tr>
                    <td colSpan={8} className="table__empty">Kayıt bulunamadı.</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}

      {closing && <CloseDialog session={closing} onClose={() => setClosing(null)} />}
      {pausesOf && <PausesDialog session={pausesOf} onClose={() => setPausesOf(null)} />}
    </section>
  )
}
