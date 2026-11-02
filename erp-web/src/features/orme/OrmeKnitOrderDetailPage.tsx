import { Check, Pencil, Plus, Printer, RotateCcw, Trash2, XCircle } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useParams } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDate, formatQuantity, todayIsoDate } from '../../utils/format'
import {
  ormeKnitOrderStatusPresentation,
  ormeRollStatusPresentation,
  useAddOrmeRollMutation,
  useAddOrmeYarnIssueMutation,
  useAddOrmeYarnTakeBackMutation,
  useAssignOrmeMachineMutation,
  useCancelOrmeKnitOrderMutation,
  useCompleteOrmeKnitOrderMutation,
  useGetOrmeIssuableLotsQuery,
  useGetOrmeKnitOrderQuery,
  useGetOrmeMachineOptionsQuery,
  useReleaseOrmeMachineMutation,
  useRemoveOrmeRollMutation,
  useRemoveOrmeYarnIssueMutation,
  useRemoveOrmeYarnTakeBackMutation,
  useReopenOrmeKnitOrderMutation,
  useUpdateOrmeKnitOrderMutation,
} from './ormeKnitApi'
import type { OrmeKnitOrder } from './ormeKnitApi'
import { ormeOrderOwnershipLabels } from './ormeOrdersApi'
import { knitBedTypeLabels } from './ormePresentation'

/** Emrin planı: tarih ve kilo. */
function PlanDialog({ knit, onClose }: { knit: OrmeKnitOrder; onClose: () => void }) {
  const [update, { isLoading, error }] = useUpdateOrmeKnitOrderMutation()
  const [plannedDate, setPlannedDate] = useState(knit.plannedDate)
  const [kilograms, setKilograms] = useState(String(knit.plannedKilograms))
  const [notes, setNotes] = useState(knit.notes ?? '')

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await update({ id: knit.id, plannedDate, plannedKilograms: Number(kilograms), notes: notes.trim() || null })
    if (!('error' in result)) {
      onClose()
    }
  }

  return (
    <Modal title={`Plan: ${knit.knitOrderNo}`} onClose={onClose}>
      <form className="form" onSubmit={handleSubmit}>
        <div className="form__pair">
          <label className="field"><span>Plan tarihi</span><input type="date" required value={plannedDate} onChange={(event) => setPlannedDate(event.target.value)} /></label>
          <label className="field"><span>Plan kilosu</span><input type="number" min="0" step="0.01" required value={kilograms} onChange={(event) => setKilograms(event.target.value)} /></label>
        </div>
        <label className="field"><span>Not</span><textarea rows={2} maxLength={1000} value={notes} onChange={(event) => setNotes(event.target.value)} /></label>
        <ErrorAlert error={error} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading}>Kaydet</button>
        </footer>
      </form>
    </Modal>
  )
}

/** Makine atama: kumaşla aynı plakadaki etkin makineler; çap ya da fine farkı uyarıdır, dolu makine seçilemez. */
function MachineDialog({ knit, onClose }: { knit: OrmeKnitOrder; onClose: () => void }) {
  const { data: options, error, isLoading } = useGetOrmeMachineOptionsQuery(knit.id)
  const [assign, assignState] = useAssignOrmeMachineMutation()
  const [machineId, setMachineId] = useState<number | null>(null)
  const [assignedOn, setAssignedOn] = useState(todayIsoDate())
  const selected = options?.find((option) => option.id === machineId)

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    if (machineId === null) {
      return
    }
    const result = await assign({ id: knit.id, machineId, assignedOn })
    if (!('error' in result)) {
      onClose()
    }
  }

  return (
    <Modal title={`Makine ata: ${knit.knitOrderNo}`} onClose={onClose} variant="drawer">
      <form className="form" onSubmit={handleSubmit}>
        <p className="muted">
          Kumaş: {knitBedTypeLabels[knit.bedType]}
          {knit.diameterInch !== null && `, ${knit.diameterInch}"`}
          {knit.gauge !== null && `, E${knit.gauge}`}. Yalnızca aynı plakadaki makineler listelenir.
        </p>
        {isLoading && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={error} />
        {options && (
          <div className="table-wrap">
            <table className="table table--compact">
              <thead>
                <tr>
                  <th aria-label="Seç" />
                  <th>Makine</th>
                  <th className="table__number">Çap</th>
                  <th className="table__number">Fine</th>
                  <th>Durum</th>
                </tr>
              </thead>
              <tbody>
                {options.map((option) => (
                  <tr key={option.id}>
                    <td>
                      <input type="radio" name="machine" aria-label={`${option.code} seç`} disabled={option.busyKnitOrderNo !== null}
                        checked={machineId === option.id} onChange={() => setMachineId(option.id)} />
                    </td>
                    <td><strong>{option.code}</strong> {option.name}</td>
                    <td className="table__number">{option.diameterInch}"</td>
                    <td className="table__number">E{option.gauge}</td>
                    <td>
                      {option.busyKnitOrderNo
                        ? <span className="muted">{option.busyKnitOrderNo} çalışıyor</span>
                        : option.fitWarning ? <span className="badge badge--warning">Uyuşmuyor</span> : 'Boş'}
                    </td>
                  </tr>
                ))}
                {options.length === 0 && <tr><td colSpan={5} className="table__empty">Aynı plakada atanabilecek makine yok.</td></tr>}
              </tbody>
            </table>
          </div>
        )}
        {selected?.fitWarning && <div className="alert alert--info">Kumaş kartından farklı: {selected.fitWarning}. Atama yapılabilir; kumaş özelliklerini kontrol edin.</div>}
        <label className="field"><span>Atama tarihi</span><input type="date" required value={assignedOn} onChange={(event) => setAssignedOn(event.target.value)} /></label>
        <ErrorAlert error={assignState.error} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={assignState.isLoading || machineId === null}>Ata</button>
        </footer>
      </form>
    </Modal>
  )
}

/** Emre iplik verme: uygun lotlar listelenir, kilo girilir. */
function IssueDialog({ knit, onClose }: { knit: OrmeKnitOrder; onClose: () => void }) {
  const { data: lots, error, isLoading } = useGetOrmeIssuableLotsQuery(knit.id)
  const [add, addState] = useAddOrmeYarnIssueMutation()
  const [lotId, setLotId] = useState<number | null>(null)
  const [kilograms, setKilograms] = useState('')
  const [issuedOn, setIssuedOn] = useState(todayIsoDate())
  const selected = lots?.find((lot) => lot.id === lotId)

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    if (lotId === null) {
      return
    }
    const result = await add({ id: knit.id, yarnLotId: lotId, kilograms: Number(kilograms), issuedOn })
    if (!('error' in result)) {
      onClose()
    }
  }

  return (
    <Modal title={`İplik ver: ${knit.knitOrderNo}`} onClose={onClose} variant="drawer">
      <form className="form" onSubmit={handleSubmit}>
        <p className="muted">
          Yalnızca kumaşın iplikleri ve malın sahibi siparişle aynı olan ({knit.ownership === 'COMMISSION' ? knit.customerName : 'işletme malı'}) lotlar listelenir.
        </p>
        {isLoading && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={error} />
        {lots && (
          <div className="table-wrap">
            <table className="table table--compact">
              <thead>
                <tr>
                  <th aria-label="Seç" />
                  <th>Lot no</th>
                  <th>İplik</th>
                  <th>Üretici lot no</th>
                  <th>Giriş</th>
                  <th className="table__number">Kalan (kg)</th>
                </tr>
              </thead>
              <tbody>
                {lots.map((lot) => (
                  <tr key={lot.id}>
                    <td>
                      <input type="radio" name="lot" aria-label={`${lot.lotNo} seç`} checked={lotId === lot.id}
                        onChange={() => { setLotId(lot.id); setKilograms(String(lot.remainingKilograms)) }} />
                    </td>
                    <td><code>{lot.lotNo}</code></td>
                    <td>{lot.yarnCode} — {lot.yarnName}</td>
                    <td>{lot.producerLotNo ?? '—'}</td>
                    <td>{formatDate(lot.receiptDate)}</td>
                    <td className="table__number">{formatQuantity(lot.remainingKilograms)}</td>
                  </tr>
                ))}
                {lots.length === 0 && <tr><td colSpan={6} className="table__empty">Verilebilecek lot yok. Önce iplik girişi yapın.</td></tr>}
              </tbody>
            </table>
          </div>
        )}
        <div className="form__pair">
          <label className="field">
            <span>Kilo</span>
            <input type="number" min="0" step="0.01" max={selected?.remainingKilograms} required disabled={lotId === null} value={kilograms} onChange={(event) => setKilograms(event.target.value)} />
          </label>
          <label className="field"><span>Çıkış tarihi</span><input type="date" required value={issuedOn} onChange={(event) => setIssuedOn(event.target.value)} /></label>
        </div>
        <ErrorAlert error={addState.error} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={addState.isLoading || lotId === null}>Ver</button>
        </footer>
      </form>
    </Modal>
  )
}

/** Artan ipliğin geri alımı: verilenden düşer, lota döner. */
function TakeBackDialog({ knit, onClose }: { knit: OrmeKnitOrder; onClose: () => void }) {
  const lots = knit.lots.filter((lot) => lot.netKilograms > 0)
  const [add, { isLoading, error }] = useAddOrmeYarnTakeBackMutation()
  const [lotId, setLotId] = useState<number | null>(lots.length === 1 ? lots[0].yarnLotId : null)
  const [kilograms, setKilograms] = useState('')
  const [takenOn, setTakenOn] = useState(todayIsoDate())
  const [notes, setNotes] = useState('')
  const selected = lots.find((lot) => lot.yarnLotId === lotId)

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    if (lotId === null) {
      return
    }
    const result = await add({ id: knit.id, yarnLotId: lotId, kilograms: Number(kilograms), takenOn, notes: notes.trim() || null })
    if (!('error' in result)) {
      onClose()
    }
  }

  return (
    <Modal title="Artan ipliği geri al" onClose={onClose}>
      <form className="form" onSubmit={handleSubmit}>
        <p className="muted">Geri alınan kilo verilenden düşer ve lota döner; fire buna göre hesaplanır.</p>
        <label className="field">
          <span>Lot</span>
          <select required value={lotId ?? ''} onChange={(event) => setLotId(Number(event.target.value) || null)}>
            <option value="">Seçin…</option>
            {lots.map((lot) => (
              <option key={lot.yarnLotId} value={lot.yarnLotId}>{lot.lotNo} · {lot.yarnCode} — {formatQuantity(lot.netKilograms)} kg verildi</option>
            ))}
          </select>
        </label>
        <div className="form__pair">
          <label className="field"><span>Kilo</span><input type="number" min="0" max={selected?.netKilograms} step="0.01" required value={kilograms} onChange={(event) => setKilograms(event.target.value)} /></label>
          <label className="field"><span>Tarih</span><input type="date" required value={takenOn} onChange={(event) => setTakenOn(event.target.value)} /></label>
        </div>
        <label className="field"><span>Not</span><input maxLength={500} value={notes} onChange={(event) => setNotes(event.target.value)} /></label>
        <ErrorAlert error={error} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading || lotId === null}>Geri al</button>
        </footer>
      </form>
    </Modal>
  )
}

/** Top kaydı: kilo zorunlu tartılır, metre isteğe bağlıdır; top numarası otomatik verilir. */
function RollDialog({ knit, onClose }: { knit: OrmeKnitOrder; onClose: () => void }) {
  const running = knit.machines.filter((machine) => machine.releasedOn === null)
  const [add, { isLoading, error }] = useAddOrmeRollMutation()
  const [machineId, setMachineId] = useState<number | null>(running.length === 1 ? running[0].machineId : null)
  const [cutOn, setCutOn] = useState(todayIsoDate())
  const [kilograms, setKilograms] = useState('')
  const [meters, setMeters] = useState('')
  const [notes, setNotes] = useState('')

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    if (machineId === null) {
      return
    }
    const result = await add({ id: knit.id, machineId, cutOn, kilograms: Number(kilograms), meters: meters ? Number(meters) : null, notes: notes.trim() || null })
    if (!('error' in result)) {
      onClose()
    }
  }

  return (
    <Modal title={`Top kaydet: ${knit.knitOrderNo}`} onClose={onClose}>
      <form className="form" onSubmit={handleSubmit}>
        <label className="field">
          <span>Makine</span>
          <select required value={machineId ?? ''} onChange={(event) => setMachineId(Number(event.target.value) || null)}>
            <option value="">Seçin…</option>
            {running.map((machine) => <option key={machine.id} value={machine.machineId}>{machine.machineCode} — {machine.machineName}</option>)}
          </select>
        </label>
        <div className="form__pair">
          <label className="field"><span>Tartılan kilo</span><input type="number" min="0" step="0.01" required value={kilograms} onChange={(event) => setKilograms(event.target.value)} /></label>
          <label className="field"><span>Metre (isteğe bağlı)</span><input type="number" min="0" step="0.01" value={meters} onChange={(event) => setMeters(event.target.value)} /></label>
        </div>
        <label className="field"><span>Tarih</span><input type="date" required value={cutOn} onChange={(event) => setCutOn(event.target.value)} /></label>
        <label className="field"><span>Not</span><input maxLength={500} value={notes} onChange={(event) => setNotes(event.target.value)} /></label>
        <ErrorAlert error={error} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading || machineId === null}>Kaydet</button>
        </footer>
      </form>
    </Modal>
  )
}

/** Örme emri: makineler, çıkan toplar, verilen ve geri alınan iplik. */
export function OrmeKnitOrderDetailPage() {
  const id = Number(useParams().id)
  const canManage = usePermission(Permissions.OrmeKnittingManage)
  const { data: knit, error, isLoading } = useGetOrmeKnitOrderQuery(id)
  const [complete, completeState] = useCompleteOrmeKnitOrderMutation()
  const [reopen, reopenState] = useReopenOrmeKnitOrderMutation()
  const [cancel, cancelState] = useCancelOrmeKnitOrderMutation()
  const [release, releaseState] = useReleaseOrmeMachineMutation()
  const [removeIssue, removeIssueState] = useRemoveOrmeYarnIssueMutation()
  const [removeTakeBack, removeTakeBackState] = useRemoveOrmeYarnTakeBackMutation()
  const [removeRoll, removeRollState] = useRemoveOrmeRollMutation()
  const [dialog, setDialog] = useState<'plan' | 'machine' | 'issue' | 'takeBack' | 'roll' | null>(null)
  const [confirmCancel, setConfirmCancel] = useState(false)

  const status = knit && ormeKnitOrderStatusPresentation[knit.status]
  const open = knit?.status === 'OPEN'
  const manage = canManage && open
  const hasRunning = knit?.machines.some((machine) => machine.releasedOn === null) ?? false
  const canTakeBack = canManage && knit?.status !== 'CANCELLED' && knit?.lots.some((lot) => lot.netKilograms > 0)
  const actionError = completeState.error ?? reopenState.error ?? cancelState.error ?? releaseState.error ?? removeIssueState.error ?? removeTakeBackState.error ?? removeRollState.error

  return (
    <section className="page">
      <p><Link to="/orme/knit-orders">← Örme emirlerine dön</Link></p>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error ?? actionError} />
      {knit && status && (
        <>
          <header className="page__header">
            <div>
              <h1>{knit.knitOrderNo} <span className={`badge badge--${status.tone}`}>{status.label}</span></h1>
              <p className="muted">
                <Link to={`/orme/orders/${knit.orderId}`}>{knit.orderNo}</Link> · {knit.customerName ?? 'Stoğa üretim'} · {ormeOrderOwnershipLabels[knit.ownership]}
                {' · '}{knit.fabricCode} — {knit.fabricName} · Sipariş {formatQuantity(knit.orderLineKilograms)} kg
              </p>
              <p className="muted">
                Plan {formatDate(knit.plannedDate)} · {formatQuantity(knit.plannedKilograms)} kg · Örülen {formatQuantity(knit.knittedKilograms)} kg · Kalan {formatQuantity(knit.remainingKilograms)} kg
                {knit.completedOn && ` · Bitiş ${formatDate(knit.completedOn)}`}
              </p>
              {knit.notes && <p>{knit.notes}</p>}
            </div>
            {canManage && (
              <div className="page__actions">
                {open && <button type="button" className="button" onClick={() => setDialog('plan')}><Pencil aria-hidden="true" />Planı düzenle</button>}
                {open && <button type="button" className="button button--primary" disabled={completeState.isLoading} onClick={() => complete(id)}><Check aria-hidden="true" />Emri bitir</button>}
                {knit.status === 'COMPLETED' && <button type="button" className="button" disabled={reopenState.isLoading} onClick={() => reopen(id)}><RotateCcw aria-hidden="true" />Yeniden aç</button>}
                {open && knit.rolls.length === 0 && !confirmCancel && (
                  <button type="button" className="button button--danger-ghost" onClick={() => setConfirmCancel(true)}><XCircle aria-hidden="true" />İptal et</button>
                )}
                {open && confirmCancel && <button type="button" className="button button--danger-ghost" disabled={cancelState.isLoading} onClick={() => cancel(id)}>İptali onayla</button>}
              </div>
            )}
          </header>

          {knit.mixedProducerLotYarns.length > 0 && (
            <div className="alert alert--info">
              Birden çok üretici lotundan iplik verildi: {knit.mixedProducerLotYarns.join(', ')}. Aynı emirde farklı lot renk farkı yapabilir.
            </div>
          )}

          <header className="page__header section-title">
            <h2 className="section-title">Makineler</h2>
            {manage && <div className="page__actions"><button type="button" className="button" onClick={() => setDialog('machine')}><Plus aria-hidden="true" />Makine ata</button></div>}
          </header>
          <div className="table-wrap">
            <table className="table table--compact">
              <thead>
                <tr>
                  <th>Makine</th>
                  <th className="table__number">Çap</th>
                  <th className="table__number">Fine</th>
                  <th>Atandı</th>
                  <th>Alındı</th>
                  <th className="table__number">Örülen (kg)</th>
                  <th className="table__number">Top</th>
                  {manage && <th aria-label="İşlem" />}
                </tr>
              </thead>
              <tbody>
                {knit.machines.map((machine) => (
                  <tr key={machine.id}>
                    <td>
                      <strong>{machine.machineCode}</strong> {machine.machineName}
                      {machine.fitWarning && <div className="muted">Kumaş kartından farklı: {machine.fitWarning}</div>}
                    </td>
                    <td className="table__number">{machine.diameterInch}"</td>
                    <td className="table__number">E{machine.gauge}</td>
                    <td>{formatDate(machine.assignedOn)}</td>
                    <td>{machine.releasedOn ? formatDate(machine.releasedOn) : <span className="badge badge--info">Çalışıyor</span>}</td>
                    <td className="table__number">{formatQuantity(machine.knittedKilograms)}</td>
                    <td className="table__number">{machine.rollCount}</td>
                    {manage && (
                      <td className="table__actions">
                        {machine.releasedOn === null && (
                          <button type="button" className="button" disabled={releaseState.isLoading} onClick={() => release({ id, assignmentId: machine.id })}>Makineden al</button>
                        )}
                      </td>
                    )}
                  </tr>
                ))}
                {knit.machines.length === 0 && <tr><td colSpan={manage ? 8 : 7} className="table__empty">Henüz makine atanmadı.</td></tr>}
              </tbody>
            </table>
          </div>

          <header className="page__header section-title">
            <h2 className="section-title">Toplar</h2>
            <div className="page__actions">
              {knit.rolls.length > 0 && <Link className="button" to={`/orme/knit-orders/${id}/labels`}><Printer aria-hidden="true" />Etiketleri yazdır</Link>}
              {manage && <button type="button" className="button" disabled={!hasRunning} title={hasRunning ? undefined : 'Önce makine atayın'} onClick={() => setDialog('roll')}><Plus aria-hidden="true" />Top kaydet</button>}
            </div>
          </header>
          <div className="table-wrap">
            <table className="table table--compact">
              <thead>
                <tr>
                  <th>Top no</th>
                  <th>Tarih</th>
                  <th>Makine</th>
                  <th className="table__number">Kilo</th>
                  <th className="table__number">Metre</th>
                  <th>Durum</th>
                  <th>Not</th>
                  <th aria-label="İşlem" />
                </tr>
              </thead>
              <tbody>
                {knit.rolls.map((roll) => {
                  const rollStatus = ormeRollStatusPresentation[roll.status]
                  return (
                    <tr key={roll.id}>
                      <td><code>{roll.rollNo}</code></td>
                      <td>{formatDate(roll.cutOn)}</td>
                      <td>{roll.machineCode}</td>
                      <td className="table__number">{formatQuantity(roll.kilograms)}</td>
                      <td className="table__number">
                        {roll.meters !== null
                          ? formatQuantity(roll.meters)
                          : roll.estimatedMeters !== null ? <span className="muted" title="Gramaj ve enden tahmin">~{formatQuantity(roll.estimatedMeters)}</span> : '—'}
                      </td>
                      <td><span className={`badge badge--${rollStatus.tone}`}>{rollStatus.label}</span></td>
                      <td>{roll.notes ?? '—'}</td>
                      <td className="table__actions">
                        <Link className="button" to={`/orme/knit-orders/${id}/labels?roll=${roll.id}`}>Etiket</Link>
                        {manage && roll.status === 'PENDING_INSPECTION' && (
                          <button type="button" className="button" disabled={removeRollState.isLoading} onClick={() => removeRoll({ id, rollId: roll.id })}>Sil</button>
                        )}
                      </td>
                    </tr>
                  )
                })}
                {knit.rolls.length === 0 && <tr><td colSpan={8} className="table__empty">Henüz top kaydedilmedi.</td></tr>}
              </tbody>
            </table>
          </div>

          <h2 className="section-title">İplik ihtiyacı</h2>
          <div className="table-wrap">
            <table className="table table--compact">
              <thead>
                <tr>
                  <th>İplik</th>
                  <th className="table__number">Oran (%)</th>
                  <th className="table__number">Gereken (kg, fireli)</th>
                  <th className="table__number">Verilen (kg)</th>
                </tr>
              </thead>
              <tbody>
                {knit.yarns.map((yarn) => (
                  <tr key={yarn.yarnId}>
                    <td>{yarn.yarnCode} — {yarn.yarnName}{yarn.yarnColor && <span className="muted"> ({yarn.yarnColor})</span>}</td>
                    <td className="table__number">{formatQuantity(yarn.percent)}</td>
                    <td className="table__number">{formatQuantity(yarn.requiredKilograms)}</td>
                    <td className="table__number">{formatQuantity(yarn.issuedKilograms)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <header className="page__header section-title">
            <div>
              <h2 className="section-title">Bu emirde kullanılan lotlar</h2>
              <p className="muted">Toplar emre bağlıdır; bir topta hangi lotun olabileceği bu listeden izlenir.</p>
            </div>
            <div className="page__actions">
              {manage && <button type="button" className="button" onClick={() => setDialog('issue')}><Plus aria-hidden="true" />İplik ver</button>}
              {canTakeBack && <button type="button" className="button" onClick={() => setDialog('takeBack')}><RotateCcw aria-hidden="true" />Artan ipliği geri al</button>}
            </div>
          </header>
          <div className="table-wrap">
            <table className="table table--compact">
              <thead>
                <tr>
                  <th>Lot no</th>
                  <th>Üretici lot no</th>
                  <th>İplik</th>
                  <th className="table__number">Verilen (kg)</th>
                  <th className="table__number">Geri alınan (kg)</th>
                  <th className="table__number">Net (kg)</th>
                </tr>
              </thead>
              <tbody>
                {knit.lots.map((lot) => (
                  <tr key={lot.yarnLotId}>
                    <td><code>{lot.lotNo}</code></td>
                    <td>{lot.producerLotNo ?? '—'}</td>
                    <td>{lot.yarnCode} — {lot.yarnName}</td>
                    <td className="table__number">{formatQuantity(lot.issuedKilograms)}</td>
                    <td className="table__number">{formatQuantity(lot.takenBackKilograms)}</td>
                    <td className="table__number">{formatQuantity(lot.netKilograms)}</td>
                  </tr>
                ))}
                {knit.lots.length === 0 && <tr><td colSpan={6} className="table__empty">Henüz iplik verilmedi.</td></tr>}
              </tbody>
            </table>
          </div>

          {knit.issues.length > 0 && (
            <>
              <h2 className="section-title">İplik çıkışları</h2>
              <div className="table-wrap">
                <table className="table table--compact">
                  <thead>
                    <tr>
                      <th>Tarih</th>
                      <th>Lot no</th>
                      <th>İplik</th>
                      <th className="table__number">Kilo</th>
                      {manage && <th aria-label="İşlem" />}
                    </tr>
                  </thead>
                  <tbody>
                    {knit.issues.map((issue) => (
                      <tr key={issue.id}>
                        <td>{formatDate(issue.issuedOn)}</td>
                        <td><code>{issue.lotNo}</code></td>
                        <td>{issue.yarnCode} — {issue.yarnName}</td>
                        <td className="table__number">{formatQuantity(issue.kilograms)}</td>
                        {manage && (
                          <td className="table__actions">
                            <button type="button" className="button" title="Yanlış girilen çıkışı siler; iplik lota döner" disabled={removeIssueState.isLoading}
                              onClick={() => removeIssue({ id, issueId: issue.id })}><Trash2 aria-hidden="true" />Sil</button>
                          </td>
                        )}
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </>
          )}

          {knit.takeBacks.length > 0 && (
            <>
              <h2 className="section-title">Geri alınan iplik</h2>
              <div className="table-wrap">
                <table className="table table--compact">
                  <thead>
                    <tr>
                      <th>Tarih</th>
                      <th>Lot no</th>
                      <th>İplik</th>
                      <th className="table__number">Kilo</th>
                      <th>Not</th>
                      {canManage && <th aria-label="İşlem" />}
                    </tr>
                  </thead>
                  <tbody>
                    {knit.takeBacks.map((taken) => (
                      <tr key={taken.id}>
                        <td>{formatDate(taken.takenOn)}</td>
                        <td><code>{taken.lotNo}</code></td>
                        <td>{taken.yarnCode} — {taken.yarnName}</td>
                        <td className="table__number">{formatQuantity(taken.kilograms)}</td>
                        <td>{taken.notes ?? '—'}</td>
                        {canManage && (
                          <td className="table__actions">
                            <button type="button" className="button" disabled={removeTakeBackState.isLoading} onClick={() => removeTakeBack({ id, takeBackId: taken.id })}><Trash2 aria-hidden="true" />Sil</button>
                          </td>
                        )}
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </>
          )}

          {dialog === 'plan' && <PlanDialog knit={knit} onClose={() => setDialog(null)} />}
          {dialog === 'machine' && <MachineDialog knit={knit} onClose={() => setDialog(null)} />}
          {dialog === 'issue' && <IssueDialog knit={knit} onClose={() => setDialog(null)} />}
          {dialog === 'takeBack' && <TakeBackDialog knit={knit} onClose={() => setDialog(null)} />}
          {dialog === 'roll' && <RollDialog knit={knit} onClose={() => setDialog(null)} />}
        </>
      )}
    </section>
  )
}
