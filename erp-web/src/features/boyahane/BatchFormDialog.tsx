import { useState } from 'react'
import type { FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { isRequestError } from '../../api/envelope'
import { ErrorAlert } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { formatQuantity } from '../../utils/format'
import { LookupSelect } from '../masterData/LookupSelect'
import { OVER_CAPACITY, useGetBatchCandidatesQuery, useSaveBatchMutation } from './boyahaneBatchesApi'
import { useGetBoyahaneRoutesQuery } from './boyahaneInsightsApi'
import type { Batch, BatchCandidate } from './boyahaneBatchesApi'

const idOf = (candidate: BatchCandidate) => (candidate.rollId !== null ? `r${candidate.rollId}` : `l${candidate.yarnLotId}`)

/**
 * Parti formu. Aday mal kart ve reçeteye göre gruplanır; yalnızca bir gruptan seçilebilir. Kuralların asıl denetimi
 * sunucudadır; gruplama seçimi kolaylaştırır.
 */
export function BatchFormDialog({ batch, onClose }: { batch?: Batch; onClose: () => void }) {
  const navigate = useNavigate()
  const { data: candidates, error: loadError, isLoading } = useGetBatchCandidatesQuery(batch?.id)
  const [save, { isLoading: saving, error }] = useSaveBatchMutation()
  const [machineId, setMachineId] = useState<number | null>(batch?.machineId ?? null)
  const [routeId, setRouteId] = useState<number | null>(batch?.routeId ?? null)
  const { data: routes } = useGetBoyahaneRoutesQuery()
  const [liquorRatio, setLiquorRatio] = useState(batch?.liquorRatio != null ? String(batch.liquorRatio) : '')
  const [plannedDate, setPlannedDate] = useState(batch?.plannedDate ?? '')
  const [notes, setNotes] = useState(batch?.notes ?? '')
  const [overrideNote, setOverrideNote] = useState(batch?.capacityOverrideNote ?? '')
  const [changes, setChanges] = useState<Map<string, boolean>>(new Map())

  const isSelected = (candidate: BatchCandidate) => changes.get(idOf(candidate)) ?? candidate.selected
  const selected = candidates?.filter(isSelected) ?? []
  const activeGroup = selected[0]?.groupKey ?? null
  const usesRecipe = selected[0]?.usesRecipe ?? true
  const processRoutes = routes?.filter((route) => route.isActive && route.processType === selected[0]?.processType) ?? []
  const groups = [...new Set((candidates ?? []).map((candidate) => candidate.groupKey))]
  const overCapacity = isRequestError(error) && error.code === OVER_CAPACITY

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await save({
      id: batch?.id,
      body: {
        machineId: machineId ?? 0,
        rollIds: selected.filter((candidate) => candidate.rollId !== null).map((candidate) => candidate.rollId as number),
        yarnLotIds: selected.filter((candidate) => candidate.yarnLotId !== null).map((candidate) => candidate.yarnLotId as number),
        liquorRatio: !usesRecipe || liquorRatio.trim() === '' ? null : Number(liquorRatio),
        routeId: processRoutes.some((route) => route.id === routeId) ? routeId : null,
        plannedDate: plannedDate || null,
        notes: notes.trim() || null,
        capacityOverrideNote: overrideNote.trim() || null,
      },
    })
    if ('data' in result && result.data) {
      if (batch) {
        onClose()
      } else {
        navigate(`/boyahane/batches/${result.data.id}`)
      }
    }
  }

  return (
    <Modal title={batch ? `Parti düzenle: ${batch.batchNo}` : 'Yeni parti'} onClose={onClose} variant="drawer">
      <form className="form" onSubmit={handleSubmit}>
        <LookupSelect resource="boyahane-machines" label="Makine" required value={machineId} onChange={setMachineId} />
        <div className="form__pair">
          {usesRecipe && (
            <label className="field">
              <span>Flotte oranı (1:…)</span>
              <input type="number" min="0" step="0.1" placeholder="Boşsa makinenin oranı" value={liquorRatio} onChange={(event) => setLiquorRatio(event.target.value)} />
            </label>
          )}
          <label className="field">
            <span>Planlanan tarih</span>
            <input type="date" value={plannedDate} onChange={(event) => setPlannedDate(event.target.value)} />
          </label>
        </div>
        {processRoutes.length > 1 && (
          <label className="field">
            <span>Rota</span>
            <select value={routeId ?? ''} onChange={(event) => setRouteId(event.target.value === '' ? null : Number(event.target.value))}>
              <option value="">Varsayılan rota</option>
              {processRoutes.map((route) => <option key={route.id} value={route.id}>{route.name}</option>)}
            </select>
          </label>
        )}
        <label className="field">
          <span>Not</span>
          <textarea rows={2} maxLength={1000} value={notes} onChange={(event) => setNotes(event.target.value)} />
        </label>

        {isLoading && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={loadError} />
        {candidates && candidates.length === 0 && (
          <p className="muted">Partiye alınabilecek mal yok. Mal uygun, sipariş onaylı ve boyama satırının reçetesi onaylı olmalıdır.</p>
        )}
        {groups.map((group) => {
          const items = (candidates ?? []).filter((candidate) => candidate.groupKey === group)
          const disabled = activeGroup !== null && activeGroup !== group
          return (
            <fieldset key={group} className="form__section" disabled={disabled}>
              <legend>{items[0].groupTitle}</legend>
              <div className="table-wrap batch-candidates">
                <table className="table">
                  <thead>
                    <tr>
                      <th aria-label="Seç" />
                      <th>Mal</th>
                      <th>Sipariş</th>
                      <th>Müşteri</th>
                      <th className="table__number">Kilo</th>
                      <th className="table__number">Metre</th>
                    </tr>
                  </thead>
                  <tbody>
                    {items.map((candidate) => (
                      <tr key={idOf(candidate)}>
                        <td>
                          <input type="checkbox" aria-label={`${candidate.itemNo} seç`} checked={isSelected(candidate)}
                            onChange={(event) => setChanges(new Map(changes).set(idOf(candidate), event.target.checked))} />
                        </td>
                        <td><code>{candidate.itemNo}</code></td>
                        <td>{candidate.orderNo}</td>
                        <td>{candidate.customerName ?? <span className="muted">Stoğa üretim</span>}</td>
                        <td className="table__number">{formatQuantity(candidate.kilograms)}</td>
                        <td className="table__number">{candidate.meters !== null ? formatQuantity(candidate.meters) : '—'}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </fieldset>
          )
        })}
        <p><strong>Seçilen: {selected.length} · {formatQuantity(selected.reduce((sum, candidate) => sum + candidate.kilograms, 0))} kg</strong></p>

        <ErrorAlert error={error} />
        {(overCapacity || overrideNote !== '') && (
          <label className="field">
            <span>Kapasite aşım gerekçesi</span>
            <input maxLength={500} value={overrideNote} onChange={(event) => setOverrideNote(event.target.value)} />
          </label>
        )}
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={saving}>Kaydet</button>
        </footer>
      </form>
    </Modal>
  )
}
