import { Check, Plus, Printer, Trash2, Undo2 } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useParams } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDate, formatQuantity, todayIsoDate } from '../../utils/format'
import { DokumaTakeBacks } from './DokumaTakeBacks'
import { beamStatusPresentation, sizingText } from './dokumaWarpApi'
import {
  dokumaRollStatusPresentation,
  useAddDokumaRollMutation,
  useAddDokumaWeftIssueMutation,
  useFinishDokumaBeamMutation,
  useGetDokumaBeamWeavingQuery,
  useGetDokumaIssuableWeftLotsQuery,
  useRemoveDokumaRollMutation,
  useRemoveDokumaWeftIssueMutation,
  useUnmountDokumaBeamMutation,
} from './dokumaWeavingApi'
import type { DokumaBeamWeaving } from './dokumaWeavingApi'

/** Atkıya iplik verme: uygun lotlar listelenir, kilo girilir. */
function WeftIssueDialog({ data, onClose }: { data: DokumaBeamWeaving; onClose: () => void }) {
  const { data: lots, error, isLoading } = useGetDokumaIssuableWeftLotsQuery(data.beam.id)
  const [add, addState] = useAddDokumaWeftIssueMutation()
  const [lotId, setLotId] = useState<number | null>(null)
  const [kilograms, setKilograms] = useState('')
  const [issuedOn, setIssuedOn] = useState(todayIsoDate())
  const selected = lots?.find((lot) => lot.id === lotId)

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    if (lotId === null) {
      return
    }
    const result = await add({ id: data.beam.id, yarnLotId: lotId, kilograms: Number(kilograms), issuedOn })
    if (!('error' in result)) {
      onClose()
    }
  }

  return (
    <Modal title={`Atkıya iplik ver: ${data.beam.beamNo}`} onClose={onClose} variant="drawer">
      <form className="form" onSubmit={handleSubmit}>
        <p className="muted">Yalnızca artikelin atkı iplikleri ve malın sahibi siparişle aynı olan lotlar listelenir.</p>
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
                    <td className="table__number">{formatQuantity(lot.remainingKilograms)}</td>
                  </tr>
                ))}
                {lots.length === 0 && <tr><td colSpan={5} className="table__empty">Verilebilecek lot yok. Önce iplik girişi yapın.</td></tr>}
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

/** Top kesimi: metre, isteğe bağlı kilo. */
function RollDialog({ data, onClose }: { data: DokumaBeamWeaving; onClose: () => void }) {
  const [add, { isLoading, error }] = useAddDokumaRollMutation()
  const [cutOn, setCutOn] = useState(todayIsoDate())
  const [meters, setMeters] = useState('')
  const [kilograms, setKilograms] = useState('')
  const [notes, setNotes] = useState('')

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await add({ id: data.beam.id, cutOn, meters: Number(meters), kilograms: kilograms.trim() === '' ? null : Number(kilograms), notes: notes.trim() || null })
    if (!('error' in result)) {
      onClose()
    }
  }

  return (
    <Modal title={`Top kes: ${data.loomCode ?? ''}`} onClose={onClose}>
      <form className="form" onSubmit={handleSubmit}>
        <label className="field"><span>Kesim tarihi</span><input type="date" required value={cutOn} onChange={(event) => setCutOn(event.target.value)} /></label>
        <div className="form__pair">
          <label className="field"><span>Metre</span><input type="number" min="0" step="0.01" required value={meters} onChange={(event) => setMeters(event.target.value)} /></label>
          <label className="field"><span>Kilo (isteğe bağlı)</span><input type="number" min="0" step="0.01" value={kilograms} onChange={(event) => setKilograms(event.target.value)} /></label>
        </div>
        <label className="field"><span>Not</span><input maxLength={500} value={notes} onChange={(event) => setNotes(event.target.value)} /></label>
        <ErrorAlert error={error} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading}>Kaydet</button>
        </footer>
      </form>
    </Modal>
  )
}

/** Leventi bitirme: üzerinde kalan çözgü (artık) metresi girilir, tezgâh boşalır. */
function FinishDialog({ data, onClose }: { data: DokumaBeamWeaving; onClose: () => void }) {
  const [finish, { isLoading, error }] = useFinishDokumaBeamMutation()
  const [date, setDate] = useState(todayIsoDate())
  const [leftover, setLeftover] = useState('0')

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await finish({ id: data.beam.id, date, leftoverMeters: Number(leftover) })
    if (!('error' in result)) {
      onClose()
    }
  }

  return (
    <Modal title={`Leventi bitir: ${data.beam.beamNo}`} onClose={onClose}>
      <form className="form" onSubmit={handleSubmit}>
        <p className="muted">Dokunan {formatQuantity(data.wovenMeters)} m, beklenen {formatQuantity(data.expectedFabricMeters)} m. Levent bitince {data.loomCode} tezgâhı boşalır.</p>
        <div className="form__pair">
          <label className="field"><span>Bitiş tarihi</span><input type="date" required value={date} onChange={(event) => setDate(event.target.value)} /></label>
          <label className="field"><span>Artık çözgü (m)</span><input type="number" min="0" step="0.01" required value={leftover} onChange={(event) => setLeftover(event.target.value)} /></label>
        </div>
        <ErrorAlert error={error} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading}>Bitir</button>
        </footer>
      </form>
    </Modal>
  )
}

/** Tezgâhtaki levent: atkı ipliği, kesilen toplar, bitirme. */
export function DokumaBeamWeavingPage() {
  const id = Number(useParams().id)
  const canManage = usePermission(Permissions.DokumaWeavingManage)
  const { data, error, isLoading } = useGetDokumaBeamWeavingQuery(id)
  const [unmount, unmountState] = useUnmountDokumaBeamMutation()
  const [removeIssue, removeIssueState] = useRemoveDokumaWeftIssueMutation()
  const [removeRoll, removeRollState] = useRemoveDokumaRollMutation()
  const [dialog, setDialog] = useState<'weft' | 'roll' | 'finish' | null>(null)

  const onLoom = data?.beam.status === 'ON_LOOM'
  const manage = canManage && onLoom
  const status = data && beamStatusPresentation[data.beam.status]

  return (
    <section className="page">
      <p><Link to="/dokuma/weaving">← Tezgâh panosuna dön</Link></p>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error ?? unmountState.error ?? removeIssueState.error ?? removeRollState.error} />
      {data && status && (
        <>
          <header className="page__header">
            <div>
              <h1>{data.beam.beamNo} <span className={`badge badge--${status.tone}`}>{status.label}</span></h1>
              <p className="muted">
                {data.loomCode ? `${data.loomCode} — ${data.loomName}` : 'Tezgâh yok'}
                {data.mountedOn && ` · Bağlandı ${formatDate(data.mountedOn)}`}
                {data.finishedOn && ` · Bitti ${formatDate(data.finishedOn)}, artık ${formatQuantity(data.leftoverMeters ?? 0)} m`}
              </p>
              <p className="muted">
                <Link to={`/dokuma/warp-orders/${data.beam.warpOrderId}`}>{data.beam.warpNo}</Link> · {data.beam.orderNo} · {data.beam.customerName ?? 'Stoğa üretim'}
                {' · '}{data.beam.articleCode} — {data.beam.articleName} · {formatQuantity(data.beam.meters)} m çözgü, {formatQuantity(data.beam.ends)} tel · {sizingText(data.beam)}
              </p>
              <p>Dokunan <strong>{formatQuantity(data.wovenMeters)} m</strong> / beklenen {formatQuantity(data.expectedFabricMeters)} m</p>
            </div>
            {manage && (
              <div className="page__actions">
                <button type="button" className="button" onClick={() => setDialog('roll')}><Plus aria-hidden="true" />Top kes</button>
                {data.rolls.length === 0 && data.weftIssues.length === 0 && (
                  <button type="button" className="button" disabled={unmountState.isLoading} onClick={() => unmount(id)}><Undo2 aria-hidden="true" />Tezgâhtan geri al</button>
                )}
                <button type="button" className="button button--primary" onClick={() => setDialog('finish')}><Check aria-hidden="true" />Leventi bitir</button>
              </div>
            )}
          </header>

          <header className="page__header">
            <h2>Atkı ipliği</h2>
            {manage && <div className="page__actions"><button type="button" className="button" onClick={() => setDialog('weft')}><Plus aria-hidden="true" />İplik ver</button></div>}
          </header>
          <div className="table-wrap">
            <table className="table table--compact">
              <thead>
                <tr>
                  <th>İplik</th>
                  <th className="table__number">Gereken (kg, fireli)</th>
                  <th className="table__number">Verilen (kg)</th>
                </tr>
              </thead>
              <tbody>
                {data.weftYarns.map((yarn) => (
                  <tr key={yarn.yarnId}>
                    <td>{yarn.yarnCode} — {yarn.yarnName}{yarn.yarnColor && <span className="muted"> ({yarn.yarnColor})</span>}</td>
                    <td className="table__number">{formatQuantity(yarn.requiredKilograms)}</td>
                    <td className="table__number">{formatQuantity(yarn.issuedKilograms)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          {data.weftIssues.length > 0 && (
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
                  {data.weftIssues.map((issue) => (
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
                </tbody>
              </table>
            </div>
          )}

          {data.weftIssues.length > 0 && (
            <DokumaTakeBacks
              source={{ beamId: id }}
              issues={data.weftIssues}
              takeBacks={data.takeBacks}
              canManage={canManage && (data.beam.status === 'ON_LOOM' || data.beam.status === 'FINISHED')}
            />
          )}

          <header className="page__header">
            <h2>Toplar</h2>
            {data.rolls.length > 0 && (
              <div className="page__actions">
                <Link className="button" to={`/dokuma/weaving/beams/${id}/labels`}><Printer aria-hidden="true" />Etiketler</Link>
              </div>
            )}
          </header>
          <div className="table-wrap">
            <table className="table table--compact">
              <thead>
                <tr>
                  <th>Top no</th>
                  <th>Tezgâh</th>
                  <th>Kesim</th>
                  <th className="table__number">Metre</th>
                  <th className="table__number">Kilo</th>
                  <th>Durum</th>
                  <th>Not</th>
                  {manage && <th aria-label="İşlem" />}
                </tr>
              </thead>
              <tbody>
                {data.rolls.map((roll) => (
                  <tr key={roll.id}>
                    <td><code>{roll.rollNo}</code></td>
                    <td>{roll.loomCode}</td>
                    <td>{formatDate(roll.cutOn)}</td>
                    <td className="table__number">{formatQuantity(roll.meters)}</td>
                    <td className="table__number">{roll.kilograms !== null ? formatQuantity(roll.kilograms) : '—'}</td>
                    <td><span className={`badge badge--${dokumaRollStatusPresentation[roll.status].tone}`}>{dokumaRollStatusPresentation[roll.status].label}</span></td>
                    <td>{roll.notes ?? '—'}</td>
                    {manage && (
                      <td className="table__actions">
                        <Link className="button" to={`/dokuma/weaving/beams/${id}/labels?roll=${roll.id}`}><Printer aria-hidden="true" />Etiket</Link>
                        <button type="button" className="button" disabled={removeRollState.isLoading} onClick={() => removeRoll({ id, rollId: roll.id })}>Sil</button>
                      </td>
                    )}
                  </tr>
                ))}
                {data.rolls.length === 0 && <tr><td colSpan={manage ? 8 : 7} className="table__empty">Henüz top kesilmedi.</td></tr>}
              </tbody>
            </table>
          </div>

          {dialog === 'weft' && <WeftIssueDialog data={data} onClose={() => setDialog(null)} />}
          {dialog === 'roll' && <RollDialog data={data} onClose={() => setDialog(null)} />}
          {dialog === 'finish' && <FinishDialog data={data} onClose={() => setDialog(null)} />}
        </>
      )}
    </section>
  )
}
