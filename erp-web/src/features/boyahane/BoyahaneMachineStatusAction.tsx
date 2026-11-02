import { useState } from 'react'
import type { FormEvent } from 'react'
import { ErrorAlert } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDateTime } from '../../utils/format'
import type { MasterRow } from '../masterData/masterDataApi'
import { dyeMachineStatusLabels } from './boyahanePresentation'
import { useChangeBoyahaneMachineStatusMutation, useGetBoyahaneMachineStatusQuery } from './boyahaneFloorApi'
import type { DyeMachineStatus } from './boyahaneFloorApi'

function StatusDialog({ machineId, onClose }: { machineId: number; onClose: () => void }) {
  const canManage = usePermission(Permissions.BoyahaneBatchManage)
  const canResolve = usePermission(Permissions.BoyahaneBatchResolveBreakdown)
  const { data, error, isLoading } = useGetBoyahaneMachineStatusQuery(machineId)
  const [change, changeState] = useChangeBoyahaneMachineStatusMutation()
  const [target, setTarget] = useState<DyeMachineStatus | ''>('')
  const [reason, setReason] = useState('')

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    if (target && !('error' in await change({ machineId, status: target, reason: reason.trim() || null }))) {
      setTarget('')
      setReason('')
    }
  }

  return (
    <Modal title={`Makine durumu: ${data?.machineCode ?? ''}`} onClose={onClose}>
      <div className="form">
        {isLoading && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={error ?? changeState.error} />
        {data && (
          <>
            <p>Şu anki durum: <strong>{dyeMachineStatusLabels[data.status]}</strong></p>
            {canManage && (data.status !== 'BREAKDOWN' || canResolve) && (
              <form className="form" onSubmit={handleSubmit}>
                <div className="form__pair">
                  <label className="field">
                    <span>Yeni durum</span>
                    <select required value={target} onChange={(event) => setTarget(event.target.value as DyeMachineStatus)}>
                      <option value="">Seçin</option>
                      {Object.entries(dyeMachineStatusLabels).filter(([value]) => value !== data.status).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
                    </select>
                  </label>
                  <label className="field">
                    <span>Neden</span>
                    <input maxLength={500} value={reason} onChange={(event) => setReason(event.target.value)} />
                  </label>
                </div>
                <footer className="form__actions">
                  <button type="submit" className="button button--primary" disabled={changeState.isLoading}>Kaydet</button>
                </footer>
              </form>
            )}
            <div className="table-wrap">
              <table className="table">
                <thead><tr><th>Durum</th><th>Başlangıç</th><th>Bitiş</th><th>Neden</th><th>Bildiren</th></tr></thead>
                <tbody>
                  {data.history.map((log) => (
                    <tr key={`${log.startedAt}-${log.status}`}>
                      <td>{dyeMachineStatusLabels[log.status]}</td>
                      <td>{formatDateTime(log.startedAt)}</td>
                      <td>{log.endedAt ? formatDateTime(log.endedAt) : '—'}</td>
                      <td>{log.reason ?? '—'}</td>
                      <td>{log.changedBy}</td>
                    </tr>
                  ))}
                  {data.history.length === 0 && <tr><td colSpan={5} className="table__empty">Kayıt yok.</td></tr>}
                </tbody>
              </table>
            </div>
          </>
        )}
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Kapat</button>
        </footer>
      </div>
    </Modal>
  )
}

/** Makine satırındaki durum işlemi: arıza, bakım ve geçmişi. */
export function BoyahaneMachineStatusAction({ row }: { row: MasterRow }) {
  const [open, setOpen] = useState(false)
  return (
    <>
      <button type="button" className="button" onClick={() => setOpen(true)}>Durum</button>
      {open && <StatusDialog machineId={row.id} onClose={() => setOpen(false)} />}
    </>
  )
}
