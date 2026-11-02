import { useState } from 'react'
import type { FormEvent } from 'react'
import { ErrorAlert, TextField } from '../../components/FormControls'
import { formatQuantity } from '../../utils/format'
import { LookupSelect } from '../masterData/LookupSelect'
import { formatPercent } from '../productionTracking/trackingPresentation'
import { parseQuantity } from '../stock/stockPresentation'
import { useLazyGetLineBalanceQuery } from './lineBalancingApi'
import type { BalanceOperation, LineBalance } from './lineBalancingApi'

const decimal = new Intl.NumberFormat('tr-TR', { maximumFractionDigits: 2 })

function Stat({ label, value, detail }: { label: string; value: string; detail?: string }) {
  return (
    <article className="card stat">
      <h2 className="stat__label">{label}</h2>
      <p className="stat__value">{value}</p>
      {detail && <p className="muted stat__detail">{detail}</p>}
    </article>
  )
}

/** Eksik / fazla durumu renge bırakılmaz: ikon ve metin de taşır. */
function Gap({ gap, unit }: { gap: number; unit: string }) {
  if (gap > 0) {
    return <span className="badge badge--danger"><span aria-hidden="true">▼</span> {gap} {unit} eksik</span>
  }
  if (gap < 0) {
    return <span className="badge badge--info"><span aria-hidden="true">▲</span> {-gap} {unit} fazla</span>
  }
  return <span className="badge badge--ok"><span aria-hidden="true">✓</span> Yeterli</span>
}

function OperationRow({ operation }: { operation: BalanceOperation }) {
  return (
    <tr>
      <td>
        <strong>{operation.sequenceNo}</strong> {operation.operationName}
        {operation.isBottleneck && (
          <div><span className="badge badge--warning"><span aria-hidden="true">!</span> Darboğaz</span></div>
        )}
        {operation.machineTypeName && <div className="muted">{operation.machineTypeName}</div>}
      </td>
      <td className="table__number">{decimal.format(operation.sam)}</td>
      <td className="table__number">{decimal.format(operation.workload)}</td>
      <td className="table__number"><strong>{operation.requiredStations}</strong></td>
      <td className="table__number">{formatPercent(operation.loadPercent)}</td>
      <td>
        {operation.availableMachines} makine <Gap gap={operation.machineGap} unit="makine" />
        <div className="muted">
          {operation.availableCapacityPerHour === null
            ? 'Kapasite sınırı yok'
            : `Kapasite ${formatQuantity(Math.floor(operation.availableCapacityPerHour))} adet / saat`}
        </div>
      </td>
      <td>
        {operation.qualifiedOperators} yetkin <Gap gap={operation.operatorGap} unit="operatör" />
        {operation.suggestedOperators.length > 0 && (
          <div className="muted">Öneri: {operation.suggestedOperators.map((o) => o.name).join(', ')}</div>
        )}
      </td>
      <td>
        {operation.suggestions.length === 0 ? (
          '—'
        ) : (
          <ul className="plain-list">
            {operation.suggestions.map((suggestion) => (
              <li key={suggestion}>{suggestion}</li>
            ))}
          </ul>
        )}
      </td>
    </tr>
  )
}

function Result({ balance }: { balance: LineBalance }) {
  const scope = balance.lineCode ? `${balance.lineCode} · ${balance.lineName}` : 'Fabrikanın tüm aktif makineleri'
  return (
    <>
      <p className="muted">
        Model <strong>{balance.modelCode}</strong> {balance.modelName} · rota sürüm {balance.routeVersion} · {scope}
      </p>

      <p className={`alert alert--${balance.targetReachable ? 'info' : 'error'}`} role="status">
        {balance.targetReachable
          ? 'Eldeki makine ve yetkin operatörlerle hedef tutturulabilir.'
          : `Eldeki kaynaklarla hedef tutturulamıyor${
              balance.bottleneckOperationName ? `; hattı sınırlayan operasyon: ${balance.bottleneckOperationName}` : ''
            }.`}
      </p>

      <div className="cards cards--stats">
        <Stat
          label="Takt süresi"
          value={`${decimal.format(balance.taktMinutes)} dk`}
          detail={`Toplam SAM ${decimal.format(balance.totalSam)} dk / adet`}
        />
        <Stat
          label="Gereken istasyon"
          value={String(balance.requiredStations)}
          detail={`Teorik ${decimal.format(balance.theoreticalOperators)} operatör · denge verimi ${formatPercent(balance.balanceEfficiencyPercent)}`}
        />
        <Stat
          label="Eldeki makinelerle"
          value={balance.achievablePerHour === null ? '—' : `${formatQuantity(Math.floor(balance.achievablePerHour))} / saat`}
          detail={`Hedef ${decimal.format(balance.targetPerHour)} adet / saat`}
        />
        <Stat
          label="Operatörsüz istasyon"
          value={String(balance.unfilledStations)}
          detail={balance.unfilledStations === 0 ? 'Her istasyona yetkin operatör önerildi' : 'Yetkin operatör bulunamadı'}
        />
      </div>

      <div className="table-wrap">
        <table className="table">
          <caption className="visually-hidden">Operasyon bazında bant dengesi</caption>
          <thead>
            <tr>
              <th>Operasyon</th>
              <th className="table__number">SAM (dk)</th>
              <th className="table__number">İş yükü</th>
              <th className="table__number">Gereken istasyon</th>
              <th className="table__number">Doluluk</th>
              <th>Makine</th>
              <th>Operatör</th>
              <th>Öneriler</th>
            </tr>
          </thead>
          <tbody>
            {balance.operations.map((operation) => (
              <OperationRow key={operation.routeOperationId} operation={operation} />
            ))}
          </tbody>
        </table>
      </div>
      <p className="muted">
        Operatör önerisi yalnızca yetkinliklere bakar; operatörün o gün başka bir işte olup olmadığını dikkate almaz.
      </p>
    </>
  )
}

/**
 * Bant dengeleme: modelin rotası ve hedef adede göre gereken istasyonlar, eldeki makine ve operatörlerle
 * karşılaştırması. Hesap sunucudadır ve hiçbir şey kaydetmez.
 */
export function LineBalancingPage() {
  const [calculate, result] = useLazyGetLineBalanceQuery()

  const [modelId, setModelId] = useState<number | null>(null)
  const [lineId, setLineId] = useState<number | null>(null)
  const [target, setTarget] = useState('')
  const [efficiency, setEfficiency] = useState('100')

  const targetPerHour = parseQuantity(target)
  const efficiencyPercent = parseQuantity(efficiency)
  const ready = modelId !== null && targetPerHour !== null && efficiencyPercent !== null

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault()
    if (modelId !== null && targetPerHour !== null && efficiencyPercent !== null) {
      void calculate({ modelId, productionLineId: lineId ?? undefined, targetPerHour, efficiencyPercent })
    }
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">Bant Dengeleme</h1>

      <form className="form card" onSubmit={handleSubmit} noValidate>
        <div className="form__grid">
          <LookupSelect resource="models" label="Model" value={modelId} onChange={setModelId} required />
          <LookupSelect resource="production-lines" label="Bant (isteğe bağlı)" value={lineId} onChange={setLineId} />
          <TextField
            label="Hedef (adet / saat)"
            type="number"
            min={1}
            step="1"
            value={target}
            onChange={(event) => setTarget(event.target.value)}
            required
          />
          <TextField
            label="Beklenen verim (%)"
            type="number"
            min={1}
            max={200}
            step="1"
            value={efficiency}
            onChange={(event) => setEfficiency(event.target.value)}
            hint="%100: operatörler tam standart sürede (SAM) çalışır."
          />
        </div>
        <footer className="form__actions">
          <button type="submit" className="button button--primary" disabled={!ready || result.isFetching}>
            {result.isFetching ? 'Hesaplanıyor…' : 'Hesapla'}
          </button>
        </footer>
      </form>

      <ErrorAlert error={result.error} />
      {result.data && !result.error && <Result balance={result.data} />}
    </section>
  )
}
