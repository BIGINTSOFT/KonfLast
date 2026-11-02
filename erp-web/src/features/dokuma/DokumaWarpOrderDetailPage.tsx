import { Check, Pencil, Plus, Trash2, XCircle } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useParams } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDate, formatQuantity, todayIsoDate } from '../../utils/format'
import { DokumaBeamActions } from './DokumaBeamActions'
import { DokumaTakeBacks } from './DokumaTakeBacks'
import { dokumaOrderOwnershipLabels } from './dokumaOrdersApi'
import {
  beamStatusPresentation,
  drawingMethodLabels,
  sizingModeLabels,
  sizingText,
  useAddDokumaBeamMutation,
  useAddDokumaWarpIssueMutation,
  useCancelDokumaWarpOrderMutation,
  useCompleteDokumaWarpOrderMutation,
  useGetDokumaIssuableLotsQuery,
  useGetDokumaWarpOrderQuery,
  useRemoveDokumaBeamMutation,
  useRemoveDokumaWarpIssueMutation,
  useUpdateDokumaBeamMutation,
  useUpdateDokumaWarpOrderMutation,
  warpOrderStatusPresentation,
} from './dokumaWarpApi'
import type { DokumaBeam, DokumaWarpOrder, SizingMode } from './dokumaWarpApi'

/** Çözgü emrinin planı: tarih, levent sayısı ve metresi. */
function WarpPlanDialog({ warp, onClose }: { warp: DokumaWarpOrder; onClose: () => void }) {
  const [update, { isLoading, error }] = useUpdateDokumaWarpOrderMutation()
  const [plannedDate, setPlannedDate] = useState(warp.plannedDate)
  const [beamCount, setBeamCount] = useState(String(warp.plannedBeamCount))
  const [meters, setMeters] = useState(String(warp.metersPerBeam))
  const [notes, setNotes] = useState(warp.notes ?? '')

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await update({ id: warp.id, plannedDate, plannedBeamCount: Number(beamCount), metersPerBeam: Number(meters), notes: notes.trim() || null })
    if (!('error' in result)) {
      onClose()
    }
  }

  return (
    <Modal title={`Çözgü planı: ${warp.warpNo}`} onClose={onClose}>
      <form className="form" onSubmit={handleSubmit}>
        <label className="field"><span>Plan tarihi</span><input type="date" required value={plannedDate} onChange={(event) => setPlannedDate(event.target.value)} /></label>
        <div className="form__pair">
          <label className="field"><span>Levent sayısı</span><input type="number" min="1" step="1" required value={beamCount} onChange={(event) => setBeamCount(event.target.value)} /></label>
          <label className="field"><span>Levent metresi</span><input type="number" min="0" step="0.01" required value={meters} onChange={(event) => setMeters(event.target.value)} /></label>
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

/** Çözgüye iplik verme: uygun lotlar listelenir, kilo girilir. */
function IssueDialog({ warp, onClose }: { warp: DokumaWarpOrder; onClose: () => void }) {
  const { data: lots, error, isLoading } = useGetDokumaIssuableLotsQuery(warp.id)
  const [add, addState] = useAddDokumaWarpIssueMutation()
  const [lotId, setLotId] = useState<number | null>(null)
  const [kilograms, setKilograms] = useState('')
  const [issuedOn, setIssuedOn] = useState(todayIsoDate())
  const selected = lots?.find((lot) => lot.id === lotId)

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    if (lotId === null) {
      return
    }
    const result = await add({ id: warp.id, yarnLotId: lotId, kilograms: Number(kilograms), issuedOn })
    if (!('error' in result)) {
      onClose()
    }
  }

  return (
    <Modal title={`Çözgüye iplik ver: ${warp.warpNo}`} onClose={onClose} variant="drawer">
      <form className="form" onSubmit={handleSubmit}>
        <p className="muted">
          Yalnızca artikelin çözgü iplikleri ve malın sahibi siparişle aynı olan ({warp.customerName && warp.ownership === 'COMMISSION' ? warp.customerName : 'işletme malı'}) lotlar listelenir.
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

/** Levent ekleme ya da düzeltme. */
function BeamDialog({ warp, beam, onClose }: { warp: DokumaWarpOrder; beam: DokumaBeam | null; onClose: () => void }) {
  const [add, addState] = useAddDokumaBeamMutation()
  const [update, updateState] = useUpdateDokumaBeamMutation()
  const [meters, setMeters] = useState(beam ? String(beam.meters) : String(warp.metersPerBeam))
  const [ends, setEnds] = useState(beam ? String(beam.ends) : String(warp.ends))
  const [sizingMode, setSizingMode] = useState<SizingMode>(beam?.sizingMode ?? 'NONE')
  const [notes, setNotes] = useState(beam?.notes ?? '')

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const body = { meters: Number(meters), ends: Number(ends) || null, sizingMode, notes: notes.trim() || null }
    const result = beam ? await update({ id: warp.id, beamId: beam.id, body }) : await add({ id: warp.id, body })
    if (!('error' in result)) {
      onClose()
    }
  }

  return (
    <Modal title={beam ? `Levent: ${beam.beamNo}` : 'Levent ekle'} onClose={onClose}>
      <form className="form" onSubmit={handleSubmit}>
        <div className="form__pair">
          <label className="field"><span>Metre</span><input type="number" min="0" step="0.01" required value={meters} onChange={(event) => setMeters(event.target.value)} /></label>
          <label className="field"><span>Tel sayısı</span><input type="number" min="1" step="1" required value={ends} onChange={(event) => setEnds(event.target.value)} /></label>
        </div>
        <label className="field">
          <span>Haşıl</span>
          <select value={sizingMode} onChange={(event) => setSizingMode(event.target.value as SizingMode)}>
            {Object.entries(sizingModeLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
          </select>
        </label>
        <label className="field"><span>Not</span><input maxLength={500} value={notes} onChange={(event) => setNotes(event.target.value)} /></label>
        <ErrorAlert error={addState.error ?? updateState.error} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={addState.isLoading || updateState.isLoading}>Kaydet</button>
        </footer>
      </form>
    </Modal>
  )
}

/** Çözgü emri: verilen iplik, leventler, haşıl ve tahar / düğüm. */
export function DokumaWarpOrderDetailPage() {
  const id = Number(useParams().id)
  const canManage = usePermission(Permissions.DokumaWarpManage)
  const { data: warp, error, isLoading } = useGetDokumaWarpOrderQuery(id)
  const [complete, completeState] = useCompleteDokumaWarpOrderMutation()
  const [cancel, cancelState] = useCancelDokumaWarpOrderMutation()
  const [removeIssue, removeIssueState] = useRemoveDokumaWarpIssueMutation()
  const [removeBeam, removeBeamState] = useRemoveDokumaBeamMutation()
  const [dialog, setDialog] = useState<'plan' | 'issue' | null>(null)
  const [editingBeam, setEditingBeam] = useState<DokumaBeam | null | undefined>(undefined)
  const [confirmCancel, setConfirmCancel] = useState(false)

  const status = warp && warpOrderStatusPresentation[warp.status]
  const open = warp?.status === 'OPEN'
  const manage = canManage && open

  return (
    <section className="page">
      <p><Link to="/dokuma/warp-orders">← Çözgü emirlerine dön</Link></p>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error ?? completeState.error ?? cancelState.error ?? removeIssueState.error ?? removeBeamState.error} />
      {warp && status && (
        <>
          <header className="page__header">
            <div>
              <h1>{warp.warpNo} <span className={`badge badge--${status.tone}`}>{status.label}</span></h1>
              <p className="muted">
                <Link to={`/dokuma/orders/${warp.orderId}`}>{warp.orderNo}</Link> · {warp.customerName ?? 'Stoğa üretim'} · {dokumaOrderOwnershipLabels[warp.ownership]}
                {' · '}{warp.articleCode} — {warp.articleName} · Sipariş {formatQuantity(warp.orderLineMeters)} m
              </p>
              <p className="muted">
                Plan {formatDate(warp.plannedDate)} · {warp.plannedBeamCount} levent × {formatQuantity(warp.metersPerBeam)} m · {formatQuantity(warp.ends)} tel
                {warp.completedOn && ` · Bitiş ${formatDate(warp.completedOn)}`}
              </p>
              {warp.notes && <p>{warp.notes}</p>}
            </div>
            {manage && (
              <div className="page__actions">
                <button type="button" className="button" onClick={() => setDialog('plan')}><Pencil aria-hidden="true" />Planı düzenle</button>
                <button type="button" className="button button--primary" disabled={completeState.isLoading} onClick={() => complete(id)}><Check aria-hidden="true" />Çözgüyü bitir</button>
                {warp.beams.length === 0 && !confirmCancel && (
                  <button type="button" className="button button--danger-ghost" onClick={() => setConfirmCancel(true)}><XCircle aria-hidden="true" />İptal et</button>
                )}
                {confirmCancel && <button type="button" className="button button--danger-ghost" disabled={cancelState.isLoading} onClick={() => cancel(id)}>İptali onayla</button>}
              </div>
            )}
          </header>

          {warp.mixedProducerLotYarns.length > 0 && (
            <div className="alert alert--info">
              Birden çok üretici lotundan iplik verildi: {warp.mixedProducerLotYarns.join(', ')}. Aynı çözgüde farklı lot renk farkı yapabilir.
            </div>
          )}

          <h2>Çözgü iplikleri</h2>
          <div className="table-wrap">
            <table className="table table--compact">
              <thead>
                <tr>
                  <th>İplik</th>
                  <th className="table__number">Tel</th>
                  <th className="table__number">Gereken (kg, fireli)</th>
                  <th className="table__number">Verilen (kg)</th>
                </tr>
              </thead>
              <tbody>
                {warp.yarns.map((yarn) => (
                  <tr key={yarn.yarnId}>
                    <td>{yarn.yarnCode} — {yarn.yarnName}{yarn.yarnColor && <span className="muted"> ({yarn.yarnColor})</span>}</td>
                    <td className="table__number">{formatQuantity(yarn.ends)}</td>
                    <td className="table__number">{formatQuantity(yarn.requiredKilograms)}</td>
                    <td className="table__number">{formatQuantity(yarn.issuedKilograms)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <header className="page__header">
            <h2>Verilen iplik</h2>
            {manage && <div className="page__actions"><button type="button" className="button" onClick={() => setDialog('issue')}><Plus aria-hidden="true" />İplik ver</button></div>}
          </header>
          <div className="table-wrap">
            <table className="table table--compact">
              <thead>
                <tr>
                  <th>Tarih</th>
                  <th>Lot no</th>
                  <th>Üretici lot no</th>
                  <th>İplik</th>
                  <th className="table__number">Kilo</th>
                  {manage && <th aria-label="İşlem" />}
                </tr>
              </thead>
              <tbody>
                {warp.issues.map((issue) => (
                  <tr key={issue.id}>
                    <td>{formatDate(issue.issuedOn)}</td>
                    <td><code>{issue.lotNo}</code></td>
                    <td>{issue.producerLotNo ?? '—'}</td>
                    <td>{issue.yarnCode} — {issue.yarnName}</td>
                    <td className="table__number">{formatQuantity(issue.kilograms)}</td>
                    {manage && (
                      <td className="table__actions">
                        <button type="button" className="button" title="Çıkışı geri al; iplik lota döner" disabled={removeIssueState.isLoading}
                          onClick={() => removeIssue({ id, issueId: issue.id })}><Trash2 aria-hidden="true" />Geri al</button>
                      </td>
                    )}
                  </tr>
                ))}
                {warp.issues.length === 0 && <tr><td colSpan={manage ? 6 : 5} className="table__empty">Henüz iplik verilmedi.</td></tr>}
              </tbody>
            </table>
          </div>

          {canManage && warp.status === 'COMPLETED' && warp.issues.length > 0 && <DokumaTakeBacks source={{ warpOrderId: id }} issues={warp.issues} takeBacks={warp.takeBacks} canManage />}
          {!canManage && warp.takeBacks.length > 0 && <DokumaTakeBacks source={{ warpOrderId: id }} issues={warp.issues} takeBacks={warp.takeBacks} canManage={false} />}

          <header className="page__header">
            <h2>Leventler</h2>
            {manage && <div className="page__actions"><button type="button" className="button" onClick={() => setEditingBeam(null)}><Plus aria-hidden="true" />Levent ekle</button></div>}
          </header>
          <div className="table-wrap">
            <table className="table table--compact">
              <thead>
                <tr>
                  <th>Levent no</th>
                  <th className="table__number">Metre</th>
                  <th className="table__number">Tel</th>
                  <th>Haşıl</th>
                  <th>Hazırlık</th>
                  <th>Durum</th>
                  {canManage && <th aria-label="İşlem" />}
                </tr>
              </thead>
              <tbody>
                {warp.beams.map((beam) => {
                  const beamStatus = beamStatusPresentation[beam.status]
                  const editable = open && beam.status === 'WARPED' && !beam.isSized
                  return (
                    <tr key={beam.id}>
                      <td><code>{beam.beamNo}</code></td>
                      <td className="table__number">{formatQuantity(beam.meters)}</td>
                      <td className="table__number">{formatQuantity(beam.ends)}</td>
                      <td>{sizingText(beam)}</td>
                      <td>{beam.drawingMethod ? `${drawingMethodLabels[beam.drawingMethod]} · ${beam.readyOn ? formatDate(beam.readyOn) : ''}` : '—'}</td>
                      <td><span className={`badge badge--${beamStatus.tone}`}>{beamStatus.label}</span></td>
                      {canManage && (
                        <td className="table__actions">
                          {editable && <button type="button" className="button" onClick={() => setEditingBeam(beam)}>Düzelt</button>}
                          {editable && (
                            <button type="button" className="button" disabled={removeBeamState.isLoading} onClick={() => removeBeam({ id, beamId: beam.id })}>Sil</button>
                          )}
                          <DokumaBeamActions beam={beam} />
                        </td>
                      )}
                    </tr>
                  )
                })}
                {warp.beams.length === 0 && <tr><td colSpan={canManage ? 7 : 6} className="table__empty">Henüz levent kaydedilmedi.</td></tr>}
              </tbody>
            </table>
          </div>

          {dialog === 'plan' && <WarpPlanDialog warp={warp} onClose={() => setDialog(null)} />}
          {dialog === 'issue' && <IssueDialog warp={warp} onClose={() => setDialog(null)} />}
          {editingBeam !== undefined && <BeamDialog warp={warp} beam={editingBeam} onClose={() => setEditingBeam(undefined)} />}
        </>
      )}
    </section>
  )
}
