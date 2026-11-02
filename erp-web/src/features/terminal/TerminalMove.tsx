import { useState } from 'react'
import type { FormEvent } from 'react'
import { ErrorAlert } from '../../components/FormControls'
import { useTransferStockMutation } from '../stock/stockApi'
import { formatStockQuantity, parseQuantity, quantityStep, stockUnitLabels } from '../stock/stockPresentation'
import { BalanceList, ScanInput } from './ScanInput'
import { useScanner } from './terminalApi'
import type { ScanBalance, ScanLocation } from './terminalApi'

/** requestId kaynak seçilirken üretilir: "Taşı" yinelenirse (ağ hatası) stok ikinci kez taşınmaz. */
interface Source {
  row: ScanBalance
  requestId: string
}

/**
 * Adres değiştirme: taşınacak top / kart (ya da bulunduğu adres) okutulur, sonra hedef adres okutulur. Depo
 * içinde çalışır; depolar arası transfer Stok ekranından yapılır.
 */
export function TerminalMove() {
  const scanner = useScanner()
  const [transfer, transferState] = useTransferStockMutation()
  const [candidates, setCandidates] = useState<ScanBalance[]>([])
  const [source, setSource] = useState<Source | null>(null)
  const [target, setTarget] = useState<ScanLocation | null>(null)
  const [quantity, setQuantity] = useState('')
  const [problem, setProblem] = useState<string | null>(null)
  const [done, setDone] = useState<string | null>(null)
  const [step, setStep] = useState(0)

  const choose = (row: ScanBalance) => {
    setSource({ row, requestId: crypto.randomUUID() })
    setQuantity(String(row.quantity))
    setCandidates([])
    setTarget(null)
    setStep((current) => current + 1)
  }

  const reset = () => {
    setSource(null)
    setTarget(null)
    setCandidates([])
    setQuantity('')
    setStep((current) => current + 1)
  }

  const handleScan = async (code: string) => {
    setProblem(null)
    setDone(null)
    transferState.reset()

    if (!source) {
      const result = await scanner.scan(code)
      if (!result) {
        return
      }

      if (result.balances.length === 0) {
        setProblem(`${result.code}: stokta görünmüyor, taşınacak bir şey yok.`)
      } else if (result.balances.length === 1) {
        choose(result.balances[0])
      } else {
        setCandidates(result.balances)
      }
      return
    }

    const result = await scanner.scan(code, source.row.warehouseId)
    if (!result) {
      return
    }

    const location = result.location
    if (result.kind !== 'LOCATION' || !location) {
      setProblem('Hedef için bir adres okutun (adres etiketi ya da adres kodu).')
    } else if (location.warehouseId !== source.row.warehouseId) {
      setProblem(`${location.code} başka bir depoda (${location.warehouseCode}). Depolar arası transfer Stok ekranından yapılır.`)
    } else if (location.id === source.row.locationId) {
      setProblem(`Stok zaten ${location.code} adresinde.`)
    } else if (!location.isActive) {
      setProblem(`${location.code} adresi pasif; stok yerleştirilemez.`)
    } else {
      setTarget(location)
    }
  }

  const parsed = parseQuantity(quantity)
  const valid = parsed !== null && parsed > 0 && source !== null && parsed <= source.row.quantity

  const handleMove = async (event: FormEvent) => {
    event.preventDefault()
    if (!source || !target || !valid) {
      return
    }

    const { row } = source
    const result = await transfer({
      requestId: source.requestId,
      fromWarehouseId: row.warehouseId,
      toWarehouseId: row.warehouseId,
      fromLocationId: row.locationId,
      toLocationId: target.id,
      stockItemId: row.stockItemId,
      lotId: row.lotId,
      quantity: parsed,
    })
    if (!('error' in result)) {
      setDone(`${row.lotNo ?? row.itemCode}: ${formatStockQuantity(parsed, row.unit)} ${row.locationCode} → ${target.code} taşındı.`)
      reset()
    }
  }

  return (
    <>
      <ScanInput
        label={source ? 'Hedef adresi okutun' : 'Top, stok kartı ya da bulunduğu adresi okutun'}
        onScan={(code) => void handleScan(code)}
        busy={scanner.busy}
        focusToken={step}
      />

      {done && <p className="alert alert--ok" role="status">{done}</p>}
      {problem && <p className="alert alert--error" role="alert">{problem}</p>}
      <ErrorAlert error={scanner.error ?? transferState.error} />

      {candidates.length > 0 && (
        <section className="terminal-card">
          <h2>Hangisi taşınacak?</h2>
          <BalanceList rows={candidates} onPick={choose} />
        </section>
      )}

      {source && (
        <form className="terminal-card" onSubmit={(event) => void handleMove(event)} noValidate>
          <h2>Taşınacak</h2>
          <BalanceList rows={[source.row]} />
          <dl className="terminal-facts">
            <dt>Hedef</dt>
            <dd>{target ? <code>{target.code}</code> : <span className="muted">Hedef adresi okutun</span>}</dd>
          </dl>
          <label className="field">
            <span>Miktar ({stockUnitLabels[source.row.unit]})</span>
            <input
              type="number"
              inputMode="decimal"
              min={0}
              max={source.row.quantity}
              step={quantityStep(source.row.unit)}
              value={quantity}
              aria-invalid={!valid}
              onChange={(event) => setQuantity(event.target.value)}
            />
            <small className="field__hint">
              Tamamı: {formatStockQuantity(source.row.quantity, source.row.unit)}. Azı girilirse {source.row.lotNo ? 'top' : 'stok'} iki adrese bölünür.
            </small>
          </label>
          <footer className="terminal-actions">
            <button type="button" className="button" onClick={reset}>Vazgeç</button>
            <button type="submit" className="button button--primary" disabled={!target || !valid || transferState.isLoading}>
              {transferState.isLoading ? 'Taşınıyor…' : 'Taşı'}
            </button>
          </footer>
        </form>
      )}
    </>
  )
}
