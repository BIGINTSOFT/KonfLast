import { Pencil, Printer, XCircle } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useParams } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDate, formatDateTime, formatQuantity, formatRecipeAmount } from '../../utils/format'
import { formatMoney } from '../../utils/money'
import { cardScanHandlers } from '../rfid/cardScan'
import { BatchFormDialog } from './BatchFormDialog'
import { CompleteStepDialog, StartStepDialog } from './BatchStepDialogs'
import {
  batchStatusPresentation,
  batchStepLabel,
  batchStepStatusPresentation,
  colorCheckResultLabels,
  finalQualityResultLabels,
  useCancelBatchMutation,
  useGetBatchQuery,
  useSetBatchCardMutation,
  useSkipBatchStepMutation,
} from './boyahaneBatchesApi'
import type { BatchStep } from './boyahaneBatchesApi'
import { dyeChemicalUnit } from './boyahaneLabApi'
import { orderProcessTypeLabels } from './boyahaneOrdersApi'

/** Parti: kartı, adımları, mal listesi ve parti ölçeğinde reçete. Adımlar tablette ya da buradan elle ilerletilir. */
export function BatchDetailPage() {
  const id = Number(useParams().id)
  const canManage = usePermission(Permissions.BoyahaneBatchManage)
  const canFinal = usePermission(Permissions.BoyahaneBatchFinalQuality)
  const { data: batch, error, isLoading } = useGetBatchQuery(id)
  const [cancel, cancelState] = useCancelBatchMutation()
  const [setCard, cardState] = useSetBatchCardMutation()
  const [skip, skipState] = useSkipBatchStepMutation()
  const [editing, setEditing] = useState(false)
  const [starting, setStarting] = useState<BatchStep | null>(null)
  const [completing, setCompleting] = useState<BatchStep | null>(null)
  const [cardUid, setCardUid] = useState('')

  const status = batch && batchStatusPresentation[batch.status]
  const isOpen = batch?.status === 'PLANNED' || batch?.status === 'ON_MACHINE'

  const handleCard = async (event: FormEvent) => {
    event.preventDefault()
    if (!('error' in await setCard({ id, uid: cardUid.trim() }))) {
      setCardUid('')
    }
  }

  return (
    <section className="page">
      <p><Link to="/boyahane/batches">← Partilere dön</Link></p>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error ?? cancelState.error ?? skipState.error} />

      {batch && status && (
        <>
          <header className="page__header">
            <div>
              <h1>{batch.batchNo} <span className={`badge badge--${status.tone}`}>{status.label}</span></h1>
              <p className="muted">
                {orderProcessTypeLabels[batch.processType]}
                {batch.patternCode && ` · Desen ${batch.patternCode} — ${batch.patternName ?? ''}`}
                {batch.colorwayCode && ` · Renk yolu ${batch.colorwayCode} — ${batch.colorwayName ?? ''}`}
                {batch.routeName && ` · Rota: ${batch.routeName}`} · {batch.machineCode} — {batch.machineName} · Kapasite {formatQuantity(batch.machineCapacityKg)} kg
                {batch.plannedDate && ` · Plan ${formatDate(batch.plannedDate)}`}
                {batch.startedAt && ` · Başlangıç ${formatDateTime(batch.startedAt)}`}
                {batch.completedAt && ` · Bitiş ${formatDateTime(batch.completedAt)}`}
              </p>
              {batch.notes && <p>{batch.notes}</p>}
              {batch.reworkOfBatchId && (
                <p>Yeniden işlem: <Link to={`/boyahane/batches/${batch.reworkOfBatchId}`}>{batch.reworkOfBatchNo}</Link></p>
              )}
              {batch.capacityOverrideBy && (
                <div className="alert alert--info">Kapasite aşımı: {batch.capacityOverrideNote} · Onaylayan: {batch.capacityOverrideBy}</div>
              )}
            </div>
            <div className="page__actions">
              <Link className="button" to={`/boyahane/batches/${id}/card`}><Printer aria-hidden="true" />Kartı yazdır</Link>
              {canManage && batch.status === 'PLANNED' && <button type="button" className="button" onClick={() => setEditing(true)}><Pencil aria-hidden="true" />Düzenle</button>}
              {canManage && batch.status === 'PLANNED' && <button type="button" className="button button--danger-ghost" disabled={cancelState.isLoading} onClick={() => cancel(id)}><XCircle aria-hidden="true" />İptal et</button>}
            </div>
          </header>

          {isOpen && (
            <form className="toolbar" onSubmit={handleCard}>
              <span>RFID kart: {batch.cardUid ? <code>{batch.cardUid}</code> : <span className="muted">atanmadı</span>}</span>
              {canManage && (
                <>
                  <input aria-label="Kart numarası" placeholder="Kartı okutun" maxLength={10} autoComplete="off" value={cardUid} {...cardScanHandlers(setCardUid, { submitOnEnter: true })} />
                  <button type="submit" className="button" disabled={cardState.isLoading || cardUid.trim() === ''}>Kartı ata</button>
                  {batch.cardUid && <button type="button" className="button" disabled={cardState.isLoading} onClick={() => setCard({ id, uid: null })}>Kartı çıkar</button>}
                </>
              )}
            </form>
          )}
          <ErrorAlert error={cardState.error} />

          <h2 className="section-title">Adımlar</h2>
          <div className="table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <th>Sıra</th>
                  <th>Adım</th>
                  <th>Durum</th>
                  <th>Makine</th>
                  <th>Başlayan</th>
                  <th>Başlangıç</th>
                  <th>Bitiş</th>
                  <th>Sonuç / not</th>
                  <th aria-label="İşlemler" />
                </tr>
              </thead>
              <tbody>
                {batch.steps.map((step, index) => {
                  const stepStatus = batchStepStatusPresentation[step.status]
                  const isCurrent = isOpen && step.id === batch.currentStepId
                  return (
                    <tr key={step.id}>
                      <td>{index + 1}</td>
                      <td>
                        {batchStepLabel(step)}
                        {step.strippingLines && step.consumptions.length === 0 && (
                          <div className="muted">
                            {step.strippingRecipeName}: {step.strippingLines.map((line) => `${line.code} ${formatQuantity(line.requiredKilograms)} kg`).join(' · ')}
                          </div>
                        )}
                        {step.consumptions.length > 0 && (
                          <div className="muted">
                            {step.consumptions.map((line) => (
                              <div key={line.code}>
                                {line.code} {formatQuantity(line.actualKilograms)} kg
                                {line.lots.length > 0 && ` (${line.lots.map((lot) => `${lot.lotNo}: ${formatQuantity(lot.kilograms)}`).join(', ')})`}
                                {line.withoutLotKilograms > 0 && <span className="badge badge--warning">Lotsuz {formatQuantity(line.withoutLotKilograms)} kg</span>}
                              </div>
                            ))}
                          </div>
                        )}
                      </td>
                      <td><span className={`badge badge--${stepStatus.tone}`}>{stepStatus.label}</span></td>
                      <td>{step.machineCode ?? '—'}</td>
                      <td>{step.startedBy ?? '—'}</td>
                      <td>{step.startedAt ? formatDateTime(step.startedAt) : '—'}</td>
                      <td>{step.completedAt ? formatDateTime(step.completedAt) : '—'}</td>
                      <td>
                        {step.colorResult && colorCheckResultLabels[step.colorResult]}
                        {step.note && ` ${step.note}`}
                      </td>
                      <td>
                        {(step.behavior === 'FINAL_QUALITY' ? canFinal : canManage) && isCurrent && step.status === 'PENDING' && <button type="button" className="button" onClick={() => setStarting(step)}>Başlat</button>}
                        {(step.behavior === 'FINAL_QUALITY' ? canFinal : canManage) && isCurrent && step.status === 'IN_PROGRESS' && <button type="button" className="button button--primary" onClick={() => setCompleting(step)}>Bitir</button>}
                        {canManage && batch.status === 'PLANNED' && step.behavior !== 'DYEING' && step.behavior !== 'FINAL_QUALITY' && (
                          <button type="button" className="button" disabled={skipState.isLoading} onClick={() => skip({ batchId: id, stepId: step.id })}>
                            {step.status === 'SKIPPED' ? 'Geri al' : 'Kaldır'}
                          </button>
                        )}
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>

          {batch.recipeNo && batch.liquorRatio !== null && batch.liquorLiters !== null && (
            <>
              <h2 className="section-title">
                Reçete {batch.recipeNo} <small className="muted">
                  {formatQuantity(batch.totalKilograms)} kg · Flotte 1:{formatQuantity(batch.liquorRatio)} · {formatQuantity(batch.liquorLiters)} L
                </small>
              </h2>
              <div className="table-wrap">
                <table className="table">
                  <thead>
                    <tr>
                      <th>Kod</th>
                      <th>Ad</th>
                      <th className="table__number">Reçete</th>
                      <th className="table__number">Gereken (kg)</th>
                      <th className="table__number">Depo (kg)</th>
                    </tr>
                  </thead>
                  <tbody>
                    {batch.recipe.map((line) => (
                      <tr key={line.code}>
                        <td><code>{line.code}</code></td>
                        <td>{line.name}</td>
                        <td className="table__number">{formatRecipeAmount(line.amount)} {dyeChemicalUnit[line.kind]}</td>
                        <td className="table__number"><strong>{formatQuantity(line.requiredKilograms)}</strong></td>
                        <td className="table__number">
                          {formatQuantity(line.usableKilograms)}
                          {isOpen && line.usableKilograms < line.requiredKilograms && <span className="badge badge--warning">Yetersiz</span>}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </>
          )}

          {batch.printColors && batch.printColors.length > 0 && (
            <>
              <h2 className="section-title">
                Baskı reçetesi {batch.patternCode}/{batch.colorwayCode} <small className="muted">
                  {batch.totalMeters !== null ? `${formatQuantity(batch.totalMeters)} m` : 'Metre bilinmiyor'}
                </small>
              </h2>
              <div className="table-wrap">
                <table className="table">
                  <thead>
                    <tr>
                      <th>Sıra</th>
                      <th>Renk</th>
                      <th className="table__number">Pasta (kg/m)</th>
                      <th className="table__number">Pasta (kg)</th>
                      <th>Reçete (g/kg pasta → gereken kg)</th>
                    </tr>
                  </thead>
                  <tbody>
                    {batch.printColors.map((color) => (
                      <tr key={color.position}>
                        <td>{color.position}</td>
                        <td>{color.name}{color.colorCardCode && <span className="muted"> ({color.colorCardCode})</span>}</td>
                        <td className="table__number">{color.pasteKilogramsPerMeter !== null ? formatRecipeAmount(color.pasteKilogramsPerMeter) : '—'}</td>
                        <td className="table__number">{color.pasteKilograms !== null ? <strong>{formatQuantity(color.pasteKilograms)}</strong> : '—'}</td>
                        <td>{color.lines.map((line) => `${line.code} ${formatRecipeAmount(line.gramsPerKilogram)} → ${formatQuantity(line.requiredKilograms)} kg`).join(' · ') || '—'}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              {batch.printRecipe && batch.printRecipe.length > 0 && (
                <div className="table-wrap">
                  <table className="table">
                    <thead><tr><th>Kod</th><th>Ad</th><th className="table__number">Gereken (kg)</th><th className="table__number">Depo (kg)</th></tr></thead>
                    <tbody>
                      {batch.printRecipe.map((line) => (
                        <tr key={line.dyeChemicalId}>
                          <td><code>{line.code}</code></td>
                          <td>{line.name}</td>
                          <td className="table__number"><strong>{formatQuantity(line.requiredKilograms)}</strong></td>
                          <td className="table__number">
                            {formatQuantity(line.usableKilograms)}
                            {isOpen && line.usableKilograms < line.requiredKilograms && <span className="badge badge--warning">Yetersiz</span>}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </>
          )}

          {(batch.chemicalCosts.length > 0 || batch.hasUncostedConsumption) && (
            <p>
              Boya ve kimyasal maliyeti: <strong>{batch.chemicalCosts.map((cost) => formatMoney(cost.amount, cost.currency)).join(' + ') || '—'}</strong>
              {batch.hasUncostedConsumption && <span className="muted"> (lotsuz ya da maliyeti girilmemiş tüketim dahil değil)</span>}
            </p>
          )}

          <h2 className="section-title">Mal</h2>
          <div className="table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <th>Top / lot</th>
                  <th>Sipariş</th>
                  <th>Müşteri</th>
                  <th>Renk</th>
                  <th className="table__number">Kilo</th>
                  <th className="table__number">Metre</th>
                  <th>Final kalite</th>
                  <th className="table__number">Çıkış kilo</th>
                  <th className="table__number">Çıkış metre</th>
                  <th className="table__number">Fire</th>
                </tr>
              </thead>
              <tbody>
                {batch.items.map((item) => (
                  <tr key={item.id}>
                    <td><code>{item.itemNo}</code></td>
                    <td><Link to={`/boyahane/orders/${item.orderId}`}>{item.orderNo}</Link></td>
                    <td>{item.customerName ?? <span className="muted">Stoğa üretim</span>}</td>
                    <td>{item.colorName ?? '—'}</td>
                    <td className="table__number">{formatQuantity(item.kilograms)}</td>
                    <td className="table__number">{item.meters !== null ? formatQuantity(item.meters) : '—'}</td>
                    <td>{item.finalResult ? finalQualityResultLabels[item.finalResult] : '—'}{item.finalNote && ` ${item.finalNote}`}</td>
                    <td className="table__number">{item.finalKilograms !== null ? formatQuantity(item.finalKilograms) : '—'}</td>
                    <td className="table__number">{item.finalMeters !== null ? formatQuantity(item.finalMeters) : '—'}</td>
                    <td className="table__number">
                      {item.fireKilogramPercent !== null ? `%${formatQuantity(item.fireKilogramPercent)}` : '—'}
                      {item.overTolerance && <span className="badge badge--warning" title={`Kabul edilen fire: %${item.fireTolerancePercent}`}>Tolerans aşıldı</span>}
                    </td>
                  </tr>
                ))}
              </tbody>
              <tfoot>
                <tr>
                  <th colSpan={4} scope="row">Toplam ({batch.items.length})</th>
                  <td className="table__number">{formatQuantity(batch.totalKilograms)}</td>
                  <td className="table__number">{batch.totalMeters !== null ? formatQuantity(batch.totalMeters) : '—'}</td>
                  <td colSpan={4} />
                </tr>
              </tfoot>
            </table>
          </div>

          {editing && <BatchFormDialog batch={batch} onClose={() => setEditing(false)} />}
          {starting && <StartStepDialog batch={batch} step={starting} onClose={() => setStarting(null)} />}
          {completing && <CompleteStepDialog batch={batch} step={completing} onClose={() => setCompleting(null)} />}
        </>
      )}
    </section>
  )
}
