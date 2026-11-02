import { useState } from 'react'
import type { FormEvent } from 'react'
import { ErrorAlert } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { LookupSelect } from '../masterData/LookupSelect'
import {
  batchStepLabel,
  colorCheckResultLabels,
  finalQualityResultLabels,
  useCompleteBatchStepMutation,
  useStartBatchStepMutation,
} from './boyahaneBatchesApi'
import type { Batch, BatchStep, ColorCheckResult, FinalQualityResult } from './boyahaneBatchesApi'

/** Boyama ve söküm partinin planlandığı makinede yapılır. */
const onBatchMachine = (step: BatchStep) => step.behavior === 'DYEING' || step.behavior === 'STRIPPING'

interface PlannedLine {
  dyeChemicalId: number
  code: string
  name: string
  requiredKilograms: number
}

/**
 * Adımın hesaplanan tüketimi: boyamada parti reçetesi (ilavede boş), sökümde söküm reçetesi (aynı kimyasal birden çok
 * aşamadaysa toplanır), baskıda renk yolunun pasta reçetesi toplamı. Sunucu aynı planı hesaplar.
 */
function plannedLinesOf(batch: Batch, step: BatchStep): PlannedLine[] {
  if (step.behavior === 'DYEING') {
    return step.reworkKind === 'ADDITION' ? [] : batch.recipe
  }

  if (step.behavior === 'PRINTING') {
    return batch.printRecipe ?? []
  }

  if (step.behavior === 'STRIPPING') {
    const lines = new Map<number, PlannedLine>()
    for (const line of step.strippingLines ?? []) {
      const current = lines.get(line.dyeChemicalId)
      lines.set(line.dyeChemicalId, current
        ? { ...current, requiredKilograms: Math.round((current.requiredKilograms + line.requiredKilograms) * 1000) / 1000 }
        : { dyeChemicalId: line.dyeChemicalId, code: line.code, name: line.name, requiredKilograms: line.requiredKilograms })
    }

    return [...lines.values()]
  }

  return []
}

/** Ofisten elle bildirim: makine adımında makine seçilir, boşsa partinin makinesi kullanılır. */
export function StartStepDialog({ batch, step, onClose }: { batch: Batch; step: BatchStep; onClose: () => void }) {
  const [start, { isLoading, error }] = useStartBatchStepMutation()
  const [machineId, setMachineId] = useState<number | null>(onBatchMachine(step) ? batch.machineId : null)

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    if (!('error' in await start({ batchId: batch.id, stepId: step.id, machineId }))) {
      onClose()
    }
  }

  return (
    <Modal title={`${batchStepLabel(step)} başlat`} onClose={onClose}>
      <form className="form" onSubmit={handleSubmit}>
        {step.needsMachine
          ? <LookupSelect resource="boyahane-machines" label="Makine" disabled={onBatchMachine(step)} value={machineId} onChange={setMachineId} placeholder={`Boşsa ${batch.machineCode}`} />
          : <p className="muted">Bu adım makine istemez.</p>}
        <ErrorAlert error={error} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading}>Başlat</button>
        </footer>
      </form>
    </Modal>
  )
}

interface AddedLine {
  key: number
  dyeChemicalId: number | null
  actual: string
}

/**
 * Adımı bitirme: boyama, söküm ve baskıda tüketim; renk kontrolünde sonuç (sökümde söküm reçetesi); final kalitede mal
 * bazında sonuç.
 */
export function CompleteStepDialog({ batch, step, onClose }: { batch: Batch; step: BatchStep; onClose: () => void }) {
  const [complete, { isLoading, error }] = useCompleteBatchStepMutation()
  const isAddition = step.reworkKind === 'ADDITION'
  const recordsConsumption = step.behavior === 'DYEING' || step.behavior === 'STRIPPING' || step.behavior === 'PRINTING'
  const [planned] = useState(() => plannedLinesOf(batch, step))
  const [actuals, setActuals] = useState<Record<number, string>>(() => Object.fromEntries(planned.map((line) => [line.dyeChemicalId, String(line.requiredKilograms)])))
  const [added, setAdded] = useState<AddedLine[]>(isAddition ? [{ key: 1, dyeChemicalId: null, actual: '' }] : [])
  const [colorResult, setColorResult] = useState<ColorCheckResult | null>(null)
  const [strippingRecipeId, setStrippingRecipeId] = useState<number | null>(null)
  const [results, setResults] = useState<Record<number, FinalQualityResult>>({})
  const [measures, setMeasures] = useState<Record<number, { kilograms: string; meters: string }>>({})
  const [note, setNote] = useState('')
  const canOverride = usePermission(Permissions.BoyahaneBatchFinalOverride)
  const [overrides, setOverrides] = useState<Record<number, string>>({})
  const finalItems = batch.items.filter((item) => item.finalResult === null)
  // Çıkış ölçüsü giriş ölçüsüyle hazır gelir; farklıysa düzeltilir.
  const measureOf = (item: Batch['items'][number]) => measures[item.id] ?? { kilograms: String(item.kilograms), meters: item.meters === null ? '' : String(item.meters) }
  const setMeasure = (item: Batch['items'][number], patch: Partial<{ kilograms: string; meters: string }>) =>
    setMeasures({ ...measures, [item.id]: { ...measureOf(item), ...patch } })

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await complete({
      batchId: batch.id,
      stepId: step.id,
      body: {
        consumptions: recordsConsumption
          ? [
              ...planned.map((line) => ({ dyeChemicalId: line.dyeChemicalId, actualKilograms: Number(actuals[line.dyeChemicalId]) })),
              ...added.filter((line) => line.dyeChemicalId !== null).map((line) => ({ dyeChemicalId: line.dyeChemicalId as number, actualKilograms: Number(line.actual) })),
            ]
          : null,
        colorResult,
        strippingRecipeId: colorResult === 'STRIPPING' ? strippingRecipeId : null,
        itemResults: step.behavior === 'FINAL_QUALITY' ? finalItems.map((item) => ({
          itemId: item.id,
          result: results[item.id],
          kilograms: results[item.id] === 'APPROVED' ? Number(measureOf(item).kilograms) : null,
          meters: results[item.id] === 'APPROVED' && item.meters !== null ? Number(measureOf(item).meters) : null,
          overrideNote: overrides[item.id]?.trim() || null,
        })) : null,
        note: note.trim() || null,
      },
    })
    if (!('error' in result)) {
      onClose()
    }
  }

  const invalid = (step.behavior === 'COLOR_CHECK' && (colorResult === null || (colorResult === 'STRIPPING' && strippingRecipeId === null)))
    || (step.behavior === 'FINAL_QUALITY' && finalItems.some((item) => !results[item.id]))

  return (
    <Modal title={`${batchStepLabel(step)} bitir`} onClose={onClose} variant={step.behavior === 'FINAL_QUALITY' ? 'drawer' : undefined}>
      <form className="form" onSubmit={handleSubmit}>
        {step.behavior === 'STRIPPING' && step.strippingRecipeCode && (
          <p className="muted">
            Söküm reçetesi: <code>{step.strippingRecipeCode}</code> {step.strippingRecipeName}
            {(step.strippingLines ?? []).some((line) => line.temperatureCelsius !== null || line.durationMinutes !== null) && (
              <> · {(step.strippingLines ?? []).map((line) => [line.code, line.temperatureCelsius !== null ? `${line.temperatureCelsius} °C` : null, line.durationMinutes !== null ? `${line.durationMinutes} dk` : null].filter(Boolean).join(' ')).join(' → ')}</>
            )}
          </p>
        )}
        {step.behavior === 'PRINTING' && planned.length === 0 && (
          <p className="muted">Renk yolunda pasta tahmini ya da baskı reçetesi yok; kullanılan boya ve kimyasalı satır ekleyerek girin.</p>
        )}
        {recordsConsumption && (
          <div className="table-wrap">
            <table className="table">
              <thead>
                <tr><th>Boya / kimyasal</th><th className="table__number">Hesaplanan (kg)</th><th className="table__number">Gerçek (kg)</th></tr>
              </thead>
              <tbody>
                {planned.map((line) => (
                  <tr key={line.dyeChemicalId}>
                    <td><code>{line.code}</code> {line.name}</td>
                    <td className="table__number">{line.requiredKilograms}</td>
                    <td className="table__number">
                      <input aria-label={`${line.code} gerçek`} type="number" min="0" step="0.001" required value={actuals[line.dyeChemicalId] ?? ''}
                        onChange={(event) => setActuals({ ...actuals, [line.dyeChemicalId]: event.target.value })} />
                    </td>
                  </tr>
                ))}
                {added.map((line) => (
                  <tr key={line.key}>
                    <td>
                      <LookupSelect resource="boyahane-dye-chemicals" label="Boya / kimyasal" hideLabel idSuffix={`-${line.key}`} required value={line.dyeChemicalId}
                        onChange={(value) => setAdded(added.map((row) => (row.key === line.key ? { ...row, dyeChemicalId: value } : row)))} />
                    </td>
                    <td className="table__number">—</td>
                    <td className="table__number">
                      <input aria-label="Gerçek miktar" type="number" min="0" step="0.001" required value={line.actual}
                        onChange={(event) => setAdded(added.map((row) => (row.key === line.key ? { ...row, actual: event.target.value } : row)))} />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
        {recordsConsumption && (
          <div>
            <button type="button" className="button" onClick={() => setAdded([...added, { key: Date.now(), dyeChemicalId: null, actual: '' }])}>Satır ekle</button>
          </div>
        )}

        {step.behavior === 'COLOR_CHECK' && (
          <label className="field">
            <span>Sonuç</span>
            <select required value={colorResult ?? ''} onChange={(event) => setColorResult(event.target.value as ColorCheckResult)}>
              <option value="">Seçin</option>
              {Object.entries(colorCheckResultLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
            </select>
          </label>
        )}
        {step.behavior === 'COLOR_CHECK' && colorResult === 'STRIPPING' && (
          <>
            <LookupSelect resource="boyahane-stripping-recipes" label="Söküm reçetesi" required value={strippingRecipeId} onChange={setStrippingRecipeId} />
            <p className="muted">Araya söküm adımı, ardından reçetenin baştan uygulandığı boyama ve yeni bir renk kontrolü eklenir.</p>
          </>
        )}

        {step.behavior === 'FINAL_QUALITY' && (
          <div className="table-wrap batch-final">
            <table className="table">
              <thead><tr><th>Top / lot</th><th>Müşteri</th><th className="table__number">Giriş kilo</th><th>Sonuç</th><th>Çıkış kilo</th><th>Çıkış metre</th>{canOverride && <th>Sınır aşım gerekçesi</th>}</tr></thead>
              <tbody>
                {finalItems.map((item) => (
                  <tr key={item.id}>
                    <td><code>{item.itemNo}</code></td>
                    <td>{item.customerName ?? <span className="muted">Stoğa üretim</span>}</td>
                    <td className="table__number">{item.kilograms}</td>
                    <td>
                      <select aria-label={`${item.itemNo} sonuç`} required value={results[item.id] ?? ''} onChange={(event) => setResults({ ...results, [item.id]: event.target.value as FinalQualityResult })}>
                        <option value="">Seçin</option>
                        {Object.entries(finalQualityResultLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
                      </select>
                    </td>
                    <td>
                      {results[item.id] === 'APPROVED' && (
                        <input aria-label={`${item.itemNo} çıkış kilo`} type="number" min="0" step="0.01" required value={measureOf(item).kilograms} onChange={(event) => setMeasure(item, { kilograms: event.target.value })} />
                      )}
                    </td>
                    <td>
                      {results[item.id] === 'APPROVED' && item.meters !== null && (
                        <input aria-label={`${item.itemNo} çıkış metre`} type="number" min="0" step="0.01" required value={measureOf(item).meters} onChange={(event) => setMeasure(item, { meters: event.target.value })} />
                      )}
                    </td>
                    {canOverride && (
                      <td>
                        {results[item.id] === 'APPROVED' && (
                          <input aria-label={`${item.itemNo} sınır aşım gerekçesi`} maxLength={500} value={overrides[item.id] ?? ''} onChange={(event) => setOverrides({ ...overrides, [item.id]: event.target.value })} />
                        )}
                      </td>
                    )}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        <label className="field">
          <span>Not</span>
          <input maxLength={500} value={note} onChange={(event) => setNote(event.target.value)} />
        </label>
        <ErrorAlert error={error} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading || invalid}>Bitir</button>
        </footer>
      </form>
    </Modal>
  )
}
