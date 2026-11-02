import { ErrorAlert } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { formatDateTime } from '../../utils/format'
import { formatDuration, pauseLabel } from './workSessionPresentation'
import { useGetWorkSessionPausesQuery } from './workSessionsApi'
import type { WorkSession } from './workSessionsApi'

/** Oturumun duraklamaları: planlı mola, ara, makine arızası ve demet bekleme ayrı ayrı görünür. */
export function PausesDialog({ session, onClose }: { session: WorkSession; onClose: () => void }) {
  const { data, isLoading, error } = useGetWorkSessionPausesQuery(session.id)

  return (
    <Modal title={`Duraklamalar: ${session.operatorName} · ${session.machineCode}`} onClose={onClose}>
      <div className="form">
        {isLoading && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={error} />
        {data && data.length === 0 && <p className="muted">Bu oturumda duraklama yok.</p>}
        {data && data.length > 0 && (
          <div className="table-wrap">
            <table className="table table--compact">
              <thead>
                <tr>
                  <th>Tür</th>
                  <th>Başlangıç</th>
                  <th>Bitiş</th>
                  <th>Süre</th>
                  <th>Not</th>
                </tr>
              </thead>
              <tbody>
                {data.map((pause) => (
                  <tr key={pause.id}>
                    <td>{pauseLabel(pause)}</td>
                    <td>{formatDateTime(pause.startedAt)}</td>
                    <td>{pause.endedAt ? formatDateTime(pause.endedAt) : 'Sürüyor'}</td>
                    <td>{formatDuration(pause.durationMinutes)}</td>
                    <td>{pause.note ?? '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Kapat</button>
        </footer>
      </div>
    </Modal>
  )
}
