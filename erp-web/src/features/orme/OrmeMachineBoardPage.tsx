import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDate, formatDateTime, formatQuantity, todayIsoDate } from '../../utils/format'
import { useEndOrmeMachineStopMutation } from './ormeFloorApi'
import { OrmeStartStopDialog } from './OrmeFloorPages'
import { useAssignOrmeMachineMutation, useGetOrmeKnitOrdersQuery, useGetOrmeMachineBoardQuery } from './ormeKnitApi'
import type { OrmeMachineBoardItem } from './ormeKnitApi'
import { knitBedTypeLabels, ormeStopCategoryLabels } from './ormePresentation'

/** Boş makineye açık emir atama: yalnızca aynı plakadaki kumaşların emirleri listelenir. */
function AssignDialog({ machine, onClose }: { machine: OrmeMachineBoardItem; onClose: () => void }) {
  const { data, error, isLoading } = useGetOrmeKnitOrdersQuery({ 'filter[status]': 'OPEN', pageSize: 100 })
  const [assign, assignState] = useAssignOrmeMachineMutation()
  const [knitOrderId, setKnitOrderId] = useState<number | null>(null)
  const [assignedOn, setAssignedOn] = useState(todayIsoDate())
  const orders = data?.items.filter((knit) => knit.bedType === machine.bedType) ?? []

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    if (knitOrderId === null) {
      return
    }
    const result = await assign({ id: knitOrderId, machineId: machine.machineId, assignedOn })
    if (!('error' in result)) {
      onClose()
    }
  }

  return (
    <Modal title={`Emir ata: ${machine.machineCode}`} onClose={onClose} variant="drawer">
      <form className="form" onSubmit={handleSubmit}>
        <p className="muted">Makine: {knitBedTypeLabels[machine.bedType]}, {machine.diameterInch}", E{machine.gauge}. Çap ya da fine kumaştan farklıysa panoda uyarı görünür.</p>
        {isLoading && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={error} />
        {data && (
          <div className="table-wrap">
            <table className="table table--compact">
              <thead>
                <tr>
                  <th aria-label="Seç" />
                  <th>Emir no</th>
                  <th>Sipariş</th>
                  <th>Kumaş</th>
                  <th className="table__number">Plan (kg)</th>
                  <th className="table__number">Örülen (kg)</th>
                </tr>
              </thead>
              <tbody>
                {orders.map((knit) => (
                  <tr key={knit.id}>
                    <td><input type="radio" name="knitOrder" aria-label={`${knit.knitOrderNo} seç`} checked={knitOrderId === knit.id} onChange={() => setKnitOrderId(knit.id)} /></td>
                    <td><code>{knit.knitOrderNo}</code></td>
                    <td>{knit.orderNo}{knit.customerName && <div className="muted">{knit.customerName}</div>}</td>
                    <td>{knit.fabricCode} — {knit.fabricName}</td>
                    <td className="table__number">{formatQuantity(knit.plannedKilograms)}</td>
                    <td className="table__number">{formatQuantity(knit.knittedKilograms)}</td>
                  </tr>
                ))}
                {orders.length === 0 && <tr><td colSpan={6} className="table__empty">Aynı plakada açık emir yok.</td></tr>}
              </tbody>
            </table>
          </div>
        )}
        <label className="field"><span>Atama tarihi</span><input type="date" required value={assignedOn} onChange={(event) => setAssignedOn(event.target.value)} /></label>
        <ErrorAlert error={assignState.error} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={assignState.isLoading || knitOrderId === null}>Ata</button>
        </footer>
      </form>
    </Modal>
  )
}

/** Makine panosu: hangi makinede hangi emir var, ne kadar örüldü, ne kadar kaldı. */
export function OrmeMachineBoardPage() {
  const canManage = usePermission(Permissions.OrmeKnittingManage)
  const canManageFloor = usePermission(Permissions.OrmeFloorManage)
  const { data, error, isLoading } = useGetOrmeMachineBoardQuery()
  const [assigning, setAssigning] = useState<OrmeMachineBoardItem | null>(null)
  const [stopping, setStopping] = useState<OrmeMachineBoardItem | null>(null)
  const [endStop, endStopState] = useEndOrmeMachineStopMutation()

  return (
    <section className="page">
      <h1 className="visually-hidden">Makine Panosu</h1>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error ?? endStopState.error} />
      {data && (
        <div className="table-wrap">
          <table className="table">
            <thead>
              <tr>
                <th>Makine</th>
                <th>Durum</th>
                <th>Emir</th>
                <th>Sipariş</th>
                <th>Kumaş</th>
                <th>Atandı</th>
                <th className="table__number">Emir örülen / plan (kg)</th>
                <th className="table__number">Kalan (kg)</th>
                <th className="table__number">Bu makinede (kg)</th>
                <th className="table__number">Top</th>
                <th aria-label="İşlem" />
              </tr>
            </thead>
            <tbody>
              {data.map((machine) => (
                <tr key={machine.machineId}>
                  <td><strong>{machine.machineCode}</strong><div className="muted">{machine.machineName} · {knitBedTypeLabels[machine.bedType]}, {machine.diameterInch}", E{machine.gauge}</div></td>
                  <td>
                    {machine.openStopCategory
                      ? <>{ormeStopCategoryLabels[machine.openStopCategory]}<div className="muted">{machine.openStopReason} · {formatDateTime(machine.openStopSince)}</div></>
                      : machine.knitOrderId !== null ? 'Çalışıyor' : <span className="muted">Boşta</span>}
                  </td>
                  <td>{machine.knitOrderId !== null ? <Link to={`/orme/knit-orders/${machine.knitOrderId}`}><code>{machine.knitOrderNo}</code></Link> : <span className="muted">Boş</span>}</td>
                  <td>{machine.orderNo ?? '—'}{machine.customerName && <div className="muted">{machine.customerName}</div>}</td>
                  <td>
                    {machine.fabricCode ? `${machine.fabricCode} — ${machine.fabricName}` : '—'}
                    {machine.fitWarning && <div className="muted">Kumaş kartından farklı: {machine.fitWarning}</div>}
                  </td>
                  <td>{machine.assignedOn ? formatDate(machine.assignedOn) : '—'}</td>
                  <td className="table__number">{machine.knitOrderId !== null ? `${formatQuantity(machine.knittedKilograms ?? 0)} / ${formatQuantity(machine.plannedKilograms ?? 0)}` : '—'}</td>
                  <td className="table__number">{machine.remainingKilograms !== null ? formatQuantity(machine.remainingKilograms) : '—'}</td>
                  <td className="table__number">{machine.machineKilograms !== null ? formatQuantity(machine.machineKilograms) : '—'}</td>
                  <td className="table__number">{machine.knitOrderId !== null ? machine.rollCount : '—'}</td>
                  <td className="table__actions">
                    {canManageFloor && (machine.openStopCategory
                      ? <button type="button" className="button" disabled={endStopState.isLoading} onClick={() => endStop(machine.machineId)}>Duruşu bitir</button>
                      : <button type="button" className="button" onClick={() => setStopping(machine)}>Duruş başlat</button>)}
                    {machine.knitOrderId !== null
                      ? <Link className="button" to={`/orme/knit-orders/${machine.knitOrderId}`}>Aç</Link>
                      : canManage && <button type="button" className="button" onClick={() => setAssigning(machine)}>Emir ata</button>}
                  </td>
                </tr>
              ))}
              {data.length === 0 && <tr><td colSpan={11} className="table__empty">Etkin makine yok. Tanımlar › Örme Makineleri ekranından ekleyin.</td></tr>}
            </tbody>
          </table>
        </div>
      )}
      {assigning && <AssignDialog machine={assigning} onClose={() => setAssigning(null)} />}
      {stopping && <OrmeStartStopDialog machine={stopping} onClose={() => setStopping(null)} />}
    </section>
  )
}
