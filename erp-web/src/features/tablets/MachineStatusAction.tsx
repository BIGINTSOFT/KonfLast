import { useState } from 'react'
import type { FormEvent } from 'react'
import { ErrorAlert, TextField } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDateTime } from '../../utils/format'
import { MachineStatusBadge } from '../masterData/cells'
import { machineStatusLabel } from '../masterData/machineStatus'
import type { MasterRow } from '../masterData/masterDataApi'
import { useChangeMachineStatusMutation, useGetMachineStatusQuery } from './tabletsApi'

function formatDuration(minutes: number): string {
  if (minutes < 60) {
    return `${Math.round(minutes)} dk`
  }
  return `${Math.floor(minutes / 60)} sa ${Math.round(minutes % 60)} dk`
}

function MachineStatusDialog({ machineId, onClose }: { machineId: number; onClose: () => void }) {
  const canUpdate = usePermission(Permissions.MachineUpdate)
  const canResolveBreakdown = usePermission(Permissions.MachineResolveBreakdown)
  const { data, isLoading, error } = useGetMachineStatusQuery(machineId)
  const [changeStatus, changeState] = useChangeMachineStatusMutation()
  const [target, setTarget] = useState('')
  const [reason, setReason] = useState('')

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await changeStatus({ machineId, status: target, reason: reason.trim() || null })
    if (!('error' in result)) {
      setTarget('')
      setReason('')
    }
  }

  // Arızayı tabletten yetkili kartı kapatır; makine durumunu değiştirme yetkisi olan kullanıcı buradan da kapatabilir.
  // Arızalı makineyi başka duruma almak (arızayı kapatmak) ayrı yetki ister.
  const isBreakdown = data?.status === 'BREAKDOWN'
  const canChange = canUpdate && (!isBreakdown || canResolveBreakdown)
  const resolveBreakdown = () => void changeStatus({ machineId, status: 'AVAILABLE', reason: null })

  return (
    <Modal title={data ? `Makine durumu: ${data.machineCode}` : 'Makine durumu'} onClose={onClose}>
      <div className="form">
        {isLoading && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={error} />

        {data && (
          <>
            <p>
              Şu anki durum: <MachineStatusBadge status={data.status} />
            </p>

            {data.status === 'BREAKDOWN' && (
              <div className="alert alert--error breakdown-notice" role="status">
                <span>
                  <strong>Makine arızalı</strong>
                  {data.statusReason ? ` · ${data.statusReason}` : ''}
                </span>
                {canChange && (
                  <button
                    type="button"
                    className="button button--primary"
                    disabled={changeState.isLoading}
                    onClick={resolveBreakdown}
                  >
                    {changeState.isLoading ? 'Kaydediliyor…' : 'Arızayı kapat'}
                  </button>
                )}
              </div>
            )}

            {canChange && (
              <form className="form" onSubmit={(event) => void handleSubmit(event)}>
                <label className="field">
                  <span>Yeni durum</span>
                  <select value={target} onChange={(event) => setTarget(event.target.value)} required>
                    <option value="">Seçin…</option>
                    {data.allowedTargets.map((status) => (
                      <option key={status} value={status}>{machineStatusLabel(status)}</option>
                    ))}
                  </select>
                </label>
                <TextField
                  label="Açıklama (isteğe bağlı)"
                  value={reason}
                  maxLength={300}
                  onChange={(event) => setReason(event.target.value)}
                  hint="Örn. arıza nedeni ya da yapılan bakım."
                />
                <ErrorAlert error={changeState.error} />
                <footer className="form__actions">
                  <button type="submit" className="button button--primary" disabled={!target || changeState.isLoading}>
                    {changeState.isLoading ? 'Kaydediliyor…' : 'Durumu değiştir'}
                  </button>
                </footer>
              </form>
            )}

            <h3 className="section-title">Durum geçmişi</h3>
            {data.history.length === 0 ? (
              <p className="muted">Henüz durum değişikliği kaydı yok.</p>
            ) : (
              <div className="table-wrap">
                <table className="table table--compact">
                  <thead>
                    <tr>
                      <th>Durum</th>
                      <th>Başlangıç</th>
                      <th>Süre</th>
                      <th>Bildiren</th>
                      <th>Açıklama</th>
                    </tr>
                  </thead>
                  <tbody>
                    {data.history.map((log) => (
                      <tr key={log.id}>
                        <td><MachineStatusBadge status={log.status} /></td>
                        <td>{formatDateTime(log.startedAt)}</td>
                        <td>
                          {formatDuration(log.durationMinutes)}
                          {log.endedAt === null && ' (sürüyor)'}
                        </td>
                        <td>
                          {log.changedBy}
                          {log.reportedFromTablet && <span className="muted"> · tablet</span>}
                        </td>
                        <td>{log.reason ?? '—'}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </>
        )}
      </div>
    </Modal>
  )
}

/** Makine satırındaki "Durum" işlemi: mevcut durum, durum değiştirme ve geçmiş. */
export function MachineStatusAction({ row }: { row: MasterRow }) {
  const [open, setOpen] = useState(false)

  return (
    <>
      <button type="button" className="button" onClick={() => setOpen(true)}>Durum</button>
      {open && <MachineStatusDialog machineId={row.id} onClose={() => setOpen(false)} />}
    </>
  )
}
