import { Printer } from 'lucide-react'
import { Link, useParams } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { formatDate, formatQuantity, formatRecipeAmount } from '../../utils/format'
import { Barcode } from '../labels/LabelPrintPage'
import { batchStepLabel, useGetBatchQuery } from './boyahaneBatchesApi'
import { dyeChemicalUnit } from './boyahaneLabApi'
import { orderProcessTypeLabels } from './boyahaneOrdersApi'

/** A5 parti kartı: partiyle birlikte gezer. Barkod parti numarasını taşır, tablette RFID kart yerine okutulabilir. */
export function BatchCardPage() {
  const id = Number(useParams().id)
  const { data: batch, error, isLoading } = useGetBatchQuery(id)

  return (
    <section className="page label-page">
      <style>{'@page { size: A5 portrait; margin: 8mm; }'}</style>
      <div className="no-print">
        <p><Link to={`/boyahane/batches/${id}`}>← Partiye dön</Link></p>
        <header className="page__header">
          <div>
            <h1>Parti kartı</h1>
            <p className="muted">A5 dikey yazdırın. Barkod parti numarasını taşır.</p>
          </div>
          <button type="button" className="button button--primary" disabled={!batch} onClick={() => window.print()}>
            <Printer aria-hidden="true" />
            Yazdır
          </button>
        </header>
        {isLoading && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={error} />
      </div>

      {batch && (
        <article className="batch-card">
          <header className="batch-card__header">
            <div>
              <div className="batch-card__number">{batch.batchNo}</div>
              <div>{orderProcessTypeLabels[batch.processType]} · {batch.items[0]?.colorName ?? '—'}</div>
              {batch.patternCode && (
                <div>Desen {batch.patternCode} — {batch.patternName}{batch.colorwayCode && ` · Renk yolu ${batch.colorwayCode} — ${batch.colorwayName ?? ''}`}</div>
              )}
              <div>{batch.machineCode} — {batch.machineName}{batch.plannedDate && ` · Plan ${formatDate(batch.plannedDate)}`}</div>
              <div>
                {formatQuantity(batch.totalKilograms)} kg
                {batch.totalMeters !== null && ` · ${formatQuantity(batch.totalMeters)} m`}
                {batch.liquorRatio !== null && ` · Flotte 1:${formatQuantity(batch.liquorRatio)}`}
              </div>
            </div>
            <Barcode text={batch.batchNo} />
          </header>

          {batch.recipe.length > 0 && (
            <table className="batch-card__table">
              <thead><tr><th>Reçete {batch.recipeNo}</th><th>Ölçü</th><th>Gereken (kg)</th></tr></thead>
              <tbody>
                {batch.recipe.map((line) => (
                  <tr key={line.code}>
                    <td>{line.code} {line.name}</td>
                    <td>{formatRecipeAmount(line.amount)} {dyeChemicalUnit[line.kind]}</td>
                    <td>{formatQuantity(line.requiredKilograms)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}

          {batch.printColors && batch.printColors.length > 0 && (
            <table className="batch-card__table">
              <thead><tr><th>Renk</th><th>Pasta (kg)</th><th>Baskı reçetesi (g/kg)</th></tr></thead>
              <tbody>
                {batch.printColors.map((color) => (
                  <tr key={color.position}>
                    <td>{color.position}. {color.name}{color.colorCardCode && ` (${color.colorCardCode})`}</td>
                    <td>{color.pasteKilograms !== null ? formatQuantity(color.pasteKilograms) : '—'}</td>
                    <td>{color.lines.map((line) => `${line.code} ${formatRecipeAmount(line.gramsPerKilogram)}`).join(' · ') || '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}

          <table className="batch-card__table">
            <thead><tr><th>Adım</th><th>Tarih / saat</th><th>İmza</th></tr></thead>
            <tbody>
              {batch.steps.filter((step) => step.status !== 'SKIPPED').map((step) => (
                <tr key={step.id}><td>{batchStepLabel(step)}</td><td /><td /></tr>
              ))}
            </tbody>
          </table>

          <table className="batch-card__table">
            <thead><tr><th>Top / lot</th><th>Sipariş</th><th>Müşteri</th><th>Kilo</th></tr></thead>
            <tbody>
              {batch.items.map((item) => (
                <tr key={item.id}>
                  <td>{item.itemNo}</td>
                  <td>{item.orderNo}</td>
                  <td>{item.customerName ?? 'Stoğa üretim'}</td>
                  <td>{formatQuantity(item.kilograms)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </article>
      )}
    </section>
  )
}
