import { Search } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { ErrorAlert } from '../../components/FormControls'
import { ClearFiltersButton } from '../../components/grid/ClearFiltersButton'
import { GridHead } from '../../components/grid/GridHead'
import type { GridColumn } from '../../components/grid/GridHead'
import { useGrid } from '../../components/grid/useGrid'
import { Modal } from '../../components/Modal'
import { Pagination } from '../../components/Pagination'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDateTime } from '../../utils/format'
import type { MasterRow } from '../masterData/masterDataApi'
import {
  dokumaPauseKindLabels,
  dokumaSessionEndReasonLabels,
  useCloseDokumaWorkSessionMutation,
  useGetDokumaLoomStopsQuery,
  useGetDokumaStopReasonOptionsQuery,
  useGetDokumaWorkSessionsQuery,
  useResetDokumaTabletDeviceMutation,
  useStartDokumaLoomStopMutation,
} from './dokumaFloorApi'
import { dokumaStopCategoryLabels, formatMinutes } from './dokumaPresentation'

const PAGE_SIZE = 20

/** Dokuma tabletinin cihaz işlemi: eşleştirilmiş cihazın bağını koparma. */
export function DokumaTabletRowActions({ row }: { row: MasterRow }) {
  const canManage = usePermission(Permissions.DokumaFloorManage)
  const [resetDevice, resetState] = useResetDokumaTabletDeviceMutation()
  const [confirming, setConfirming] = useState(false)

  if (!canManage || !row.isEnrolled) {
    return null
  }

  const confirmReset = async () => {
    if (!('error' in await resetDevice(row.id))) {
      setConfirming(false)
    }
  }

  return (
    <>
      <button type="button" className="button" onClick={() => setConfirming(true)}>Cihazı sıfırla</button>
      {confirming && (
        <Modal title="Cihazı sıfırla" onClose={() => setConfirming(false)}>
          <div className="form">
            <p><strong>{String(row.tabletCode)}</strong> ile eşleştirilmiş cihazın bağı koparılacak; tablet yeniden eşleştirilmeden çalışmaz.</p>
            <ErrorAlert error={resetState.error} />
            <footer className="form__actions">
              <button type="button" className="button" onClick={() => setConfirming(false)}>Vazgeç</button>
              <button type="button" className="button button--danger" disabled={resetState.isLoading} onClick={() => void confirmReset()}>Sıfırla</button>
            </footer>
          </div>
        </Modal>
      )}
    </>
  )
}

/** Ofisten tezgâh duruşu açma: tablet olmayan tezgâh ya da unutulan kayıt için. */
export function DokumaStartStopDialog({ loom, onClose }: { loom: { loomId: number; loomCode: string }; onClose: () => void }) {
  const { data, error, isLoading } = useGetDokumaStopReasonOptionsQuery()
  const [start, startState] = useStartDokumaLoomStopMutation()
  const [reasonId, setReasonId] = useState<number | null>(null)
  const [note, setNote] = useState('')
  // Ara nedeni operatörün oturumuna aittir, tezgâh duruşu açmaz.
  const reasons = data?.items.filter((reason) => reason.isActive && reason.category !== 'PAUSE') ?? []

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    if (reasonId === null) {
      return
    }
    const result = await start({ loomId: loom.loomId, reasonId, note: note.trim() || null })
    if (!('error' in result)) {
      onClose()
    }
  }

  return (
    <Modal title={`Duruş başlat: ${loom.loomCode}`} onClose={onClose}>
      <form className="form" onSubmit={handleSubmit}>
        {isLoading && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={error} />
        {data && reasons.length === 0 && <p className="muted">Etkin duruş nedeni yok. Tanımlar › Duruş Nedenleri ekranından ekleyin.</p>}
        <label className="field">
          <span>Neden</span>
          <select required value={reasonId ?? ''} onChange={(event) => setReasonId(Number(event.target.value) || null)}>
            <option value="">Seçin…</option>
            {reasons.map((reason) => (
              <option key={reason.id} value={reason.id}>{dokumaStopCategoryLabels[reason.category]} — {reason.name}</option>
            ))}
          </select>
        </label>
        <label className="field"><span>Not</span><input maxLength={500} value={note} onChange={(event) => setNote(event.target.value)} /></label>
        <ErrorAlert error={startState.error} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={startState.isLoading || reasonId === null}>Başlat</button>
        </footer>
      </form>
    </Modal>
  )
}

function stopDuration(startedAt: string, endedAt: string | null) {
  if (!endedAt) {
    return 'Sürüyor'
  }
  return formatMinutes(Math.round((new Date(endedAt).getTime() - new Date(startedAt).getTime()) / 60000))
}

/** Tezgâh duruşlarının geçmişi; kayıtlar silinmez, verim raporu buradan okunur. */
export function DokumaLoomStopsPage() {
  const grid = useGrid()
  const { page, setPage } = grid
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const { data, error, isLoading, isFetching } = useGetDokumaLoomStopsQuery({ page, pageSize: PAGE_SIZE, search: search || undefined, ...grid.params })

  const columns: GridColumn[] = [
    { key: 'loomCode', label: 'Tezgâh' },
    { key: 'beamNo', label: 'Levent' },
    { key: 'category', label: 'Tür', filter: Object.entries(dokumaStopCategoryLabels).filter(([value]) => value !== 'PAUSE').map(([value, label]) => ({ value, label })) },
    { key: 'reasonName', label: 'Neden' },
    { key: 'startedAt', label: 'Başlangıç', filter: false },
    { key: 'endedAt', label: 'Bitiş', filter: false },
    { label: 'Süre' },
    { key: 'startedBy', label: 'Bildiren' },
    { key: 'endedBy', label: 'Kapatan' },
  ]

  const handleSearch = (event: FormEvent) => {
    event.preventDefault()
    setPage(1)
    setSearch(searchInput.trim())
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">Tezgâh Duruşları</h1>
      <form className="toolbar" onSubmit={handleSearch} role="search">
        <input type="search" placeholder="Tezgâh, levent, neden ya da personel" aria-label="Duruş ara" value={searchInput} onChange={(event) => setSearchInput(event.target.value)} />
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
                {data.items.map((stop) => (
                  <tr key={stop.id}>
                    <td>{stop.loomCode}</td>
                    <td>{stop.beamNo ? <code>{stop.beamNo}</code> : '—'}</td>
                    <td>{dokumaStopCategoryLabels[stop.category]}</td>
                    <td>{stop.reasonName}{stop.note && <div className="muted">{stop.note}</div>}</td>
                    <td>{formatDateTime(stop.startedAt)}</td>
                    <td>{stop.endedAt ? formatDateTime(stop.endedAt) : '—'}</td>
                    <td>{stopDuration(stop.startedAt, stop.endedAt)}</td>
                    <td>{stop.startedBy}</td>
                    <td>{stop.endedBy ?? '—'}</td>
                  </tr>
                ))}
                {data.items.length === 0 && <tr><td colSpan={columns.length} className="table__empty">Duruş kaydı yok.</td></tr>}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}
    </section>
  )
}

/** Tablet oturumları: kim hangi tablette çalışıyor; açık oturum ofisten kapatılabilir. */
export function DokumaWorkSessionsPage() {
  const canManage = usePermission(Permissions.DokumaFloorManage)
  const grid = useGrid()
  const { page, setPage } = grid
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const { data, error, isLoading, isFetching } = useGetDokumaWorkSessionsQuery({ page, pageSize: PAGE_SIZE, search: search || undefined, ...grid.params })
  const [close, closeState] = useCloseDokumaWorkSessionMutation()

  const columns: GridColumn[] = [
    { key: 'operatorName', label: 'Personel' },
    { key: 'tabletCode', label: 'Tablet' },
    { key: 'loomCode', label: 'Tezgâh' },
    { key: 'startedAt', label: 'Başlangıç', filter: false },
    { key: 'endedAt', label: 'Bitiş', filter: false },
    { key: 'endReason', label: 'Durum', filter: false },
    { label: '', ariaLabel: 'İşlem', className: 'table__actions' },
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
        <input type="search" placeholder="Personel, tablet ya da tezgâh" aria-label="Oturum ara" value={searchInput} onChange={(event) => setSearchInput(event.target.value)} />
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
                    <td>{session.loomCode ?? <span className="muted">Salon tableti</span>}</td>
                    <td>{formatDateTime(session.startedAt)}</td>
                    <td>{session.endedAt ? formatDateTime(session.endedAt) : '—'}</td>
                    <td>{session.endReason
                        ? dokumaSessionEndReasonLabels[session.endReason]
                        : session.pauseKind ? dokumaPauseKindLabels[session.pauseKind] : 'Açık'}</td>
                    <td className="table__actions">
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
