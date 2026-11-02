import { Plus, Trash2 } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { ErrorAlert } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { formatDate, formatQuantity, todayIsoDate } from '../../utils/format'
import { useAddDokumaYarnTakeBackMutation, useRemoveDokumaYarnTakeBackMutation } from './dokumaYarnLotsApi'
import type { DokumaTakeBackSource, DokumaYarnTakeBack } from './dokumaYarnLotsApi'

interface IssueRow {
  yarnLotId: number
  lotNo: string
  yarnCode: string
  yarnName: string
  kilograms: number
}

/** Lot başına verilip henüz geri alınmamış miktar. */
function openLots(issues: IssueRow[], takeBacks: DokumaYarnTakeBack[]) {
  const lots = new Map<number, IssueRow & { open: number }>()
  for (const issue of issues) {
    const current = lots.get(issue.yarnLotId)
    lots.set(issue.yarnLotId, { ...issue, open: (current?.open ?? 0) + issue.kilograms })
  }
  for (const taken of takeBacks) {
    const current = lots.get(taken.yarnLotId)
    if (current) {
      current.open -= taken.kilograms
    }
  }
  return [...lots.values()].filter((lot) => lot.open > 0)
}

function TakeBackDialog({ source, lots, onClose }: { source: DokumaTakeBackSource; lots: (IssueRow & { open: number })[]; onClose: () => void }) {
  const [add, { isLoading, error }] = useAddDokumaYarnTakeBackMutation()
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
    const result = await add({ source, yarnLotId: lotId, kilograms: Number(kilograms), takenOn, notes: notes.trim() || null })
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
              <option key={lot.yarnLotId} value={lot.yarnLotId}>{lot.lotNo} · {lot.yarnCode} — {formatQuantity(lot.open)} kg verildi</option>
            ))}
          </select>
        </label>
        <div className="form__pair">
          <label className="field"><span>Kilo</span><input type="number" min="0" max={selected?.open} step="0.01" required value={kilograms} onChange={(event) => setKilograms(event.target.value)} /></label>
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

/** Üretimden artıp lota geri alınan iplikler. */
export function DokumaTakeBacks({ source, issues, takeBacks, canManage }: {
  source: DokumaTakeBackSource
  issues: IssueRow[]
  takeBacks: DokumaYarnTakeBack[]
  canManage: boolean
}) {
  const [remove, removeState] = useRemoveDokumaYarnTakeBackMutation()
  const [adding, setAdding] = useState(false)
  const lots = openLots(issues, takeBacks)

  return (
    <>
      <header className="page__header">
        <h2>Geri alınan iplik</h2>
        {canManage && lots.length > 0 && (
          <div className="page__actions">
            <button type="button" className="button" onClick={() => setAdding(true)}><Plus aria-hidden="true" />Artan ipliği geri al</button>
          </div>
        )}
      </header>
      <ErrorAlert error={removeState.error} />
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
            {takeBacks.map((taken) => (
              <tr key={taken.id}>
                <td>{formatDate(taken.takenOn)}</td>
                <td><code>{taken.lotNo}</code></td>
                <td>{taken.yarnCode} — {taken.yarnName}</td>
                <td className="table__number">{formatQuantity(taken.kilograms)}</td>
                <td>{taken.notes ?? '—'}</td>
                {canManage && (
                  <td className="table__actions">
                    <button type="button" className="button" disabled={removeState.isLoading} onClick={() => remove({ source, id: taken.id })}><Trash2 aria-hidden="true" />Sil</button>
                  </td>
                )}
              </tr>
            ))}
            {takeBacks.length === 0 && <tr><td colSpan={canManage ? 6 : 5} className="table__empty">Geri alınan iplik yok.</td></tr>}
          </tbody>
        </table>
      </div>
      {adding && <TakeBackDialog source={source} lots={lots} onClose={() => setAdding(false)} />}
    </>
  )
}
