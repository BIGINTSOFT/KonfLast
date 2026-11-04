import { Check, Pencil, Plus, RotateCcw, Trash2, XCircle, X } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDate, formatRecipeAmount, todayIsoDate } from '../../utils/format'
import { LookupSelect } from '../masterData/LookupSelect'
import { customerDecisionChannelLabels } from './boyahaneInspectionApi'
import type { CustomerDecisionChannel } from './boyahaneInspectionApi'
import {
  dyeChemicalUnit,
  labRequestStatusPresentation,
  labTrialDecisionPresentation,
  useCancelLabRequestMutation,
  useDecideLabTrialMutation,
  useDeleteLabTrialMutation,
  useGetLabRequestQuery,
  useReopenLabRequestMutation,
  useSaveLabTrialMutation,
} from './boyahaneLabApi'
import type { LabRequest, LabTrial } from './boyahaneLabApi'

interface ItemDraft {
  key: number
  dyeChemicalId: number | null
  amount: string
}

/** Yeni deneme ya da karar verilmemiş denemenin düzenlenmesi. Birim (%, g/L) kartın türünden gelir. */
function TrialDialog({ request, trial, onClose }: { request: LabRequest; trial?: LabTrial; onClose: () => void }) {
  const [save, { isLoading, error }] = useSaveLabTrialMutation()
  const [notes, setNotes] = useState(trial?.notes ?? '')
  const [nextKey, setNextKey] = useState(-1)
  const [items, setItems] = useState<ItemDraft[]>(trial
    ? trial.items.map((item) => ({ key: item.id, dyeChemicalId: item.dyeChemicalId, amount: String(item.amount) }))
    : [{ key: 0, dyeChemicalId: null, amount: '' }])

  const update = (key: number, change: Partial<ItemDraft>) =>
    setItems(items.map((item) => (item.key === key ? { ...item, ...change } : item)))

  const addItem = () => {
    setItems([...items, { key: nextKey, dyeChemicalId: null, amount: '' }])
    setNextKey(nextKey - 1)
  }

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await save({
      id: request.id,
      trialId: trial?.id,
      body: {
        notes: notes.trim() || null,
        items: items.map((item) => ({ dyeChemicalId: item.dyeChemicalId ?? 0, amount: Number(item.amount) })),
      },
    })
    if (!('error' in result)) {
      onClose()
    }
  }

  return (
    <Modal title={trial ? `Deneme ${trial.trialCode}` : 'Yeni deneme'} onClose={onClose} variant="drawer">
      <form className="form" onSubmit={handleSubmit}>
        <p className="muted form__note">Boyalar kumaş ağırlığına göre %, kimyasallar g/L olarak girilir.</p>
        <div className="table-wrap recipe-table">
          <table className="table table--compact">
            <colgroup>
              <col />
              <col className="defect-table__number" />
              <col className="recipe-table__remove" />
            </colgroup>
            <thead>
              <tr>
                <th>Boya / kimyasal</th>
                <th>Miktar</th>
                <th aria-label="Satırı kaldır" />
              </tr>
            </thead>
            <tbody>
              {items.map((item) => (
                <tr key={item.key}>
                  <td>
                    <LookupSelect resource="boyahane-dye-chemicals" label="Boya / kimyasal" hideLabel idSuffix={String(item.key)} value={item.dyeChemicalId} onChange={(value) => update(item.key, { dyeChemicalId: value })} />
                  </td>
                  <td><input aria-label="Miktar" type="number" min="0" step="0.0001" required value={item.amount} onChange={(event) => update(item.key, { amount: event.target.value })} /></td>
                  <td className="recipe-table__remove-cell">
                    <button type="button" className="icon-button" aria-label="Satırı kaldır" title="Satırı kaldır" disabled={items.length === 1} onClick={() => setItems(items.filter((row) => row.key !== item.key))}>
                      <X aria-hidden="true" />
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <div><button type="button" className="button" onClick={addItem}>Kalem ekle</button></div>
        <label className="field"><span>Not</span><textarea rows={2} maxLength={500} value={notes} onChange={(event) => setNotes(event.target.value)} /></label>
        <ErrorAlert error={error} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading}>Kaydet</button>
        </footer>
      </form>
    </Modal>
  )
}

/** Müşterinin kararı: kim, ne zaman, hangi yolla. */
function DecisionDialog({ request, trial, onClose }: { request: LabRequest; trial: LabTrial; onClose: () => void }) {
  const [decide, { isLoading, error }] = useDecideLabTrialMutation()
  const [decidedBy, setDecidedBy] = useState('')
  const [channel, setChannel] = useState<CustomerDecisionChannel>('EMAIL')
  const [decidedOn, setDecidedOn] = useState(todayIsoDate())
  const [note, setNote] = useState('')

  const submit = async (accepted: boolean) => {
    const result = await decide({ id: request.id, trialId: trial.id, body: { accepted, decidedBy, channel, decidedOn, note: note.trim() || null } })
    if (!('error' in result)) {
      onClose()
    }
  }

  return (
    <Modal title={`Deneme ${trial.trialCode}: ${request.customerName ? 'müşteri kararı' : 'yetkili kararı'}`} onClose={onClose}>
      <div className="form">
        <div className="form__pair">
          <label className="field"><span>Kararı veren</span><input maxLength={100} value={decidedBy} onChange={(event) => setDecidedBy(event.target.value)} /></label>
          <label className="field"><span>Tarih</span><input type="date" value={decidedOn} onChange={(event) => setDecidedOn(event.target.value)} /></label>
        </div>
        <label className="field">
          <span>İletişim yolu</span>
          <select value={channel} onChange={(event) => setChannel(event.target.value as CustomerDecisionChannel)}>
            {Object.entries(customerDecisionChannelLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
          </select>
        </label>
        <label className="field"><span>Not</span><input maxLength={500} value={note} onChange={(event) => setNote(event.target.value)} /></label>
        <ErrorAlert error={error} />
        <footer className="form__actions">
          <button type="button" className="button button--danger-ghost" disabled={isLoading} onClick={() => submit(false)}>Kabul etmedi</button>
          <button type="button" className="button button--primary" disabled={isLoading} onClick={() => submit(true)}>Kabul etti</button>
        </footer>
      </div>
    </Modal>
  )
}

/** Laboratuvar talebi: hedef renk, denemeler, kararlar ve reçete. */
export function LabRequestDetailPage() {
  const id = Number(useParams().id)
  const navigate = useNavigate()
  const canManage = usePermission(Permissions.BoyahaneLabManage)
  const canDecide = usePermission(Permissions.BoyahaneLabCustomerDecision)
  const { data: request, error, isLoading } = useGetLabRequestQuery(id)
  const [cancel, cancelState] = useCancelLabRequestMutation()
  const [reopen, reopenState] = useReopenLabRequestMutation()
  const [removeTrial, removeState] = useDeleteLabTrialMutation()
  const [trialDialog, setTrialDialog] = useState<LabTrial | null | undefined>(undefined)
  const [deciding, setDeciding] = useState<LabTrial | null>(null)

  const status = request && labRequestStatusPresentation[request.status]
  const open = request?.status === 'OPEN'

  return (
    <section className="page">
      <p><Link to="/boyahane/lab">← Taleplere dön</Link></p>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error ?? cancelState.error ?? reopenState.error ?? removeState.error} />

      {request && status && (
        <>
          <header className="page__header">
            <div>
              <h1>{request.requestNo} <span className={`badge badge--${status.tone}`}>{status.label}</span></h1>
              <p className="muted">
                <Link to={`/boyahane/orders/${request.orderId}`}>{request.orderNo}</Link> · {request.customerName ?? 'Stoğa üretim'} · {request.itemCode} — {request.itemName}
              </p>
              <p>
                <strong>{request.colorName}</strong>
                {request.colorCode && <span className="muted"> ({request.colorCode})</span>}
                {request.colorCardId !== null && <span className="muted"> · Renk kartı <Link to={`/boyahane/color-cards/${request.colorCardId}`}>{request.colorCardCode}</Link></span>}
                {request.targetNote && <span className="muted"> · {request.targetNote}</span>}
              </p>
            </div>
            <div className="page__actions">
              {open && canManage && <button type="button" className="button button--primary" onClick={() => setTrialDialog(null)}><Plus aria-hidden="true" />Yeni deneme</button>}
              {open && canManage && <button type="button" className="button button--danger-ghost" disabled={cancelState.isLoading} onClick={() => cancel(id)}><XCircle aria-hidden="true" />Talebi iptal et</button>}
              {request.status === 'APPROVED' && canDecide && <button type="button" className="button" disabled={reopenState.isLoading} onClick={() => reopen(id)}><RotateCcw aria-hidden="true" />Onayı geri al</button>}
            </div>
          </header>

          {request.trials.length === 0 && <p className="muted">Henüz deneme yok.</p>}
          {request.trials.map((trial) => {
            const decision = labTrialDecisionPresentation[trial.decision]
            const pending = trial.decision === 'PENDING' && open
            return (
              <section key={trial.id}>
                {/* Deneme başlığı ve işlemleri tek çubukta: düğmeler tablonun altında kopuk kalmaz. */}
                <div className="section-bar trial-bar">
                  <h2 className="section-title">
                    Deneme {trial.trialCode} <span className={`badge badge--${decision.tone}`}>{decision.label}</span>
                  </h2>
                  {pending && (
                    <div className="trial-bar__actions">
                      {canManage && <button type="button" className="button button--inline button--danger-ghost" disabled={removeState.isLoading} onClick={() => removeTrial({ id, trialId: trial.id })}><Trash2 aria-hidden="true" />Sil</button>}
                      {canManage && <button type="button" className="button button--inline" onClick={() => setTrialDialog(trial)}><Pencil aria-hidden="true" />Düzenle</button>}
                      {canDecide && <button type="button" className="button button--inline button--primary" onClick={() => setDeciding(trial)}><Check aria-hidden="true" />Karar gir</button>}
                    </div>
                  )}
                </div>
                {trial.notes && <p className="muted">{trial.notes}</p>}
                {trial.reusedFromRequestNo && <p className="muted">Önceki reçeteden kopyalandı: {trial.reusedFromRequestNo}</p>}
                {trial.decisionBy && (
                  <p className="muted">
                    {trial.decisionBy}
                    {trial.decisionChannel && ` · ${customerDecisionChannelLabels[trial.decisionChannel]}`}
                    {trial.decisionOn && ` · ${formatDate(trial.decisionOn)}`}
                    {trial.decisionNote && ` · ${trial.decisionNote}`}
                    {trial.decisionRecordedBy && ` · Kaydeden: ${trial.decisionRecordedBy}`}
                  </p>
                )}
                <div className="table-wrap">
                  <table className="table">
                    <thead>
                      <tr>
                        <th>Kod</th>
                        <th>Ad</th>
                        <th className="table__number">Miktar</th>
                      </tr>
                    </thead>
                    <tbody>
                      {trial.items.map((item) => (
                        <tr key={item.id}>
                          <td><code>{item.code}</code></td>
                          <td>{item.name}</td>
                          <td className="table__number">{formatRecipeAmount(item.amount)} {dyeChemicalUnit[item.kind]}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </section>
            )
          })}

          {trialDialog !== undefined && <TrialDialog request={request} trial={trialDialog ?? undefined} onClose={() => setTrialDialog(undefined)} />}
          {deciding && <DecisionDialog request={request} trial={deciding} onClose={() => setDeciding(null)} />}
          {request.status === 'CANCELLED' && (
            <p><button type="button" className="button" onClick={() => navigate(`/boyahane/orders/${request.orderId}`)}>Siparişe dön</button></p>
          )}
        </>
      )}
    </section>
  )
}
