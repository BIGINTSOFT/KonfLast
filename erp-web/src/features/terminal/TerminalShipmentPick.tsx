import { Check } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { isRequestError } from '../../api/envelope'
import { ErrorAlert } from '../../components/FormControls'
import { formatDate, formatQuantity } from '../../utils/format'
import {
  useCompletePickingMutation,
  useGetPickingQueueQuery,
  useGetShipmentPickingQuery,
  usePickShipmentItemMutation,
  useStartPickingMutation,
  useUndoShipmentPickMutation,
} from '../shipping/shippingApi'
import type { PickingStep } from '../shipping/shippingApi'
import { ScanInput } from './ScanInput'
import { locationBarcode } from './terminalApi'

const upper = (text: string) => text.trim().toLocaleUpperCase('en-US')

/**
 * Sevkiyat toplama: toplamaya verilmiş (siparişten aktarılmış ya da elle açılmış) bir sevkiyat seçilir, toplama
 * üstlenilir ve liste adres sırasıyla yürünür. Her satırda önce adres, sonra ürün okutulur ve adet onaylanır; onay
 * sunucuya yazılır. Stok burada düşmez, irsaliye kesilince düşer.
 */
export function TerminalShipmentPick() {
  const [shipmentId, setShipmentId] = useState<number | null>(null)
  return shipmentId === null
    ? <PickingQueue onOpen={setShipmentId} />
    : <PickingRun shipmentId={shipmentId} onExit={() => setShipmentId(null)} />
}

function PickingQueue({ onOpen }: { onOpen: (id: number) => void }) {
  const { data, error, isFetching } = useGetPickingQueueQuery({ status: 'PICKING' }, { pollingInterval: 30000 })
  const [start, startState] = useStartPickingMutation()
  const [problem, setProblem] = useState<string | null>(null)

  const open = async (id: number, takeOver = false) => {
    setProblem(null)
    const result = await start({ id, takeOver })
    if (!('error' in result)) {
      onOpen(id)
      return
    }

    // Başka biri topluyorsa devralma sorulur.
    if (!takeOver && isRequestError(result.error) && result.error.code === 'PICKING_CLAIMED') {
      if (window.confirm(`${result.error.message}\n\nDevralınsın mı?`)) {
        await open(id, true)
      }
    }
  }

  const handleScan = (code: string) => {
    const match = data?.find((item) => upper(item.shipmentNo) === upper(code))
    if (match) {
      void open(match.id)
    } else {
      setProblem(`${upper(code)} toplama bekleyen sevkiyatlar arasında yok.`)
    }
  }

  return (
    <>
      <ScanInput label="Sevkiyat numarasını okutun ya da listeden seçin" onScan={handleScan} busy={isFetching || startState.isLoading} />
      {problem && <p className="alert alert--error" role="alert">{problem}</p>}
      <ErrorAlert error={error} />
      {startState.isError && !(isRequestError(startState.error) && startState.error.code === 'PICKING_CLAIMED') && (
        <ErrorAlert error={startState.error} />
      )}
      {data && data.length === 0 && <p className="alert alert--info">Toplama bekleyen sevkiyat yok.</p>}
      <ul className="terminal-list">
        {data?.map((item) => (
          <li key={item.id}>
            <button type="button" className="terminal-list__row" onClick={() => void open(item.id)} disabled={startState.isLoading}>
              <span className="terminal-list__main">
                <code>{item.shipmentNo}</code> · {item.customerName}
                <br />
                <span className="muted">
                  {item.orderNo ?? 'Elle açılmış sevkiyat'} · {item.warehouseCode} · sevk {formatDate(item.shipDate)}
                  {item.pickingBy && ` · ${item.pickingBy} topluyor`}
                </span>
              </span>
              <strong>{formatQuantity(item.pickedQuantity)} / {formatQuantity(item.quantity)}</strong>
            </button>
          </li>
        ))}
      </ul>
    </>
  )
}

function PickingRun({ shipmentId, onExit }: { shipmentId: number; onExit: () => void }) {
  const { data, error, isLoading } = useGetShipmentPickingQuery(shipmentId)
  const [pick, pickState] = usePickShipmentItemMutation()
  const [undo, undoState] = useUndoShipmentPickMutation()
  const [complete, completeState] = useCompletePickingMutation()
  const [selected, setSelected] = useState<number | null>(null)
  const [atLocation, setAtLocation] = useState<number | null>(null)
  const [itemScanned, setItemScanned] = useState(false)
  const [quantity, setQuantity] = useState('')
  const [problem, setProblem] = useState<string | null>(null)
  const [step, setStep] = useState(0)
  const [requestId, setRequestId] = useState(() => crypto.randomUUID())

  if (isLoading) {
    return <p role="status">Yükleniyor…</p>
  }

  if (error || !data) {
    return <ErrorAlert error={error} />
  }

  const steps = data.steps
  const current: PickingStep | undefined = steps.find((entry) => entry.sequence === selected && !entry.done) ?? steps.find((entry) => !entry.done)
  const remaining = current ? current.quantity - current.pickedQuantity : 0
  const doneCount = steps.filter((entry) => entry.done).length
  const shortage = data.items.filter((item) => item.pickedQuantity < item.quantity)
  const mine = data.isMine && data.status === 'PICKING'

  const reset = () => {
    setAtLocation(null)
    setItemScanned(false)
    setQuantity('')
    setStep((value) => value + 1)
  }

  const handleScan = (code: string) => {
    setProblem(null)
    if (!current) {
      return
    }

    const text = upper(code)
    if (atLocation !== current.locationId) {
      if (text === upper(current.locationCode) || text === locationBarcode(current.locationId)) {
        setAtLocation(current.locationId)
        setStep((value) => value + 1)
      } else {
        setProblem(`Sıradaki adres ${current.locationCode}. Okutulan (${text}) bu adres değil.`)
      }
      return
    }

    if (text === upper(current.itemCode)) {
      setItemScanned(true)
      setQuantity(String(remaining))
    } else {
      setProblem(`${text} bu satırın ürünü değil; ${current.itemCode} alınmalı. Yanlış ürün olabilir.`)
    }
  }

  const handleConfirm = async (event: FormEvent) => {
    event.preventDefault()
    const amount = Number(quantity.replace(',', '.'))
    if (!current || !Number.isFinite(amount) || amount <= 0) {
      return
    }

    const result = await pick({ id: shipmentId, requestId, shipmentItemId: current.shipmentItemId, locationId: current.locationId, quantity: amount })
    if (!('error' in result)) {
      setRequestId(crypto.randomUUID())
      setSelected(null)
      reset()
    }
  }

  const handleComplete = async () => {
    const warning = shortage.length > 0
      ? `Eksik toplanan kalemler var:\n${shortage.map((item) => `${item.itemCode}: ${formatQuantity(item.pickedQuantity)} / ${formatQuantity(item.quantity)}`).join('\n')}\n\nEksik kalemler toplanan adede iner, hiç toplanmayanlar sevkiyattan düşer.\n\n`
      : ''
    if (window.confirm(`${warning}Toplama bitirilecek ve sevkiyat irsaliye kontrolüne gidecek. Devam edilsin mi?`)) {
      const result = await complete(shipmentId)
      if (!('error' in result)) {
        onExit()
      }
    }
  }

  return (
    <>
      <dl className="terminal-facts">
        <dt>Sevkiyat</dt>
        <dd><code>{data.shipmentNo}</code> · {data.customerName}</dd>
        <dt>Sipariş</dt>
        <dd>{data.orderNo ?? 'Elle açılmış sevkiyat'}{data.customerOrderNo && ` · ${data.customerOrderNo}`}</dd>
        <dt>İlerleme</dt>
        <dd>{doneCount} / {steps.length} satır</dd>
      </dl>

      {!mine && (
        <p className="alert alert--error" role="alert">
          {data.status === 'PICKING' ? `Bu sevkiyatı ${data.pickingBy ?? 'başka biri'} topluyor; listeye dönüp devralın.` : 'Bu sevkiyatın toplaması bitmiş.'}
        </p>
      )}

      {mine && current && (
        <section className="terminal-card">
          <h2>Sıra {current.sequence}: adres <code>{current.locationCode}</code></h2>
          <p>
            <code>{current.itemCode}</code> {current.itemName}
            {(current.colorName || current.sizeName) && <span className="muted"> · {[current.colorName, current.sizeName].filter(Boolean).join(' / ')}</span>}
            <br />
            Alınacak <strong>{formatQuantity(remaining)}</strong>
            {current.pickedQuantity > 0 && <span className="muted"> (alınan {formatQuantity(current.pickedQuantity)})</span>}
          </p>
          {!itemScanned && (
            <ScanInput
              label={atLocation === current.locationId ? 'Ürünün barkodunu okutun' : 'Adres etiketini okutun'}
              onScan={handleScan}
              busy={pickState.isLoading}
              focusToken={step}
            />
          )}
          {itemScanned && (
            <form className="scan" onSubmit={(event) => void handleConfirm(event)}>
              <label htmlFor="pick-quantity">Aldığınız adet</label>
              <div className="scan__row">
                <input id="pick-quantity" inputMode="numeric" autoFocus value={quantity} onChange={(event) => setQuantity(event.target.value)} />
                <button type="submit" className="button button--primary" disabled={pickState.isLoading}>Onayla</button>
              </div>
            </form>
          )}
          {problem && <p className="alert alert--error" role="alert">{problem}</p>}
          <ErrorAlert error={pickState.error} fields={[]} />
          <p>
            <button type="button" className="button button--ghost" onClick={() => {
              const next = steps.find((entry) => !entry.done && entry.sequence > current.sequence) ?? steps.find((entry) => !entry.done && entry.sequence !== current.sequence)
              setSelected(next?.sequence ?? null)
              setProblem(null)
              reset()
            }}>
              Bu satırı sonra al
            </button>
          </p>
        </section>
      )}

      {mine && !current && <p className="alert alert--info">Listedeki bütün satırlar toplandı. Toplamayı bitirin.</p>}

      <ul className="terminal-list">
        {steps.map((entry) => (
          <li key={`${entry.shipmentItemId}-${entry.locationId}`}>
            <button
              type="button"
              className={entry.done ? 'terminal-list__row terminal-list__row--pick terminal-list__row--done' : 'terminal-list__row terminal-list__row--pick'}
              aria-current={current?.sequence === entry.sequence ? 'step' : undefined}
              disabled={entry.done || !mine}
              onClick={() => { setSelected(entry.sequence); setProblem(null); reset() }}
            >
              <span className="terminal-list__main">
                {entry.sequence}. <code>{entry.locationCode}</code> · <code>{entry.itemCode}</code>
                {(entry.colorName || entry.sizeName) && <span className="muted"> · {[entry.colorName, entry.sizeName].filter(Boolean).join(' / ')}</span>}
              </span>
              <strong>
                {entry.done && <Check aria-hidden="true" />}
                {formatQuantity(entry.pickedQuantity)} / {formatQuantity(entry.quantity)}
              </strong>
            </button>
          </li>
        ))}
      </ul>

      {data.items.some((item) => item.shortage > 0) && (
        <p className="alert alert--error">
          Depoda bulunamayan: {data.items.filter((item) => item.shortage > 0).map((item) => `${item.itemCode} ${formatQuantity(item.shortage)}`).join(' · ')}
        </p>
      )}

      {mine && data.picks.length > 0 && (
        <details>
          <summary>Yapılan toplamalar ({data.picks.length})</summary>
          <ul className="terminal-list">
            {[...data.picks].reverse().map((record) => (
              <li key={record.id} className="terminal-list__row">
                <span className="terminal-list__main"><code>{record.locationCode}</code> · <code>{record.itemCode}</code> · {formatQuantity(record.quantity)}</span>
                <button type="button" className="button button--tiny" disabled={undoState.isLoading}
                  onClick={() => { if (window.confirm('Bu toplama geri alınsın mı?')) { void undo({ id: shipmentId, pickId: record.id }) } }}>
                  Geri al
                </button>
              </li>
            ))}
          </ul>
        </details>
      )}
      <ErrorAlert error={undoState.error} />
      <ErrorAlert error={completeState.error} />

      <p className="terminal-actions">
        <button type="button" className="button" onClick={onExit}>Başka sevkiyat</button>
        {mine && (
          <button type="button" className="button button--primary" disabled={completeState.isLoading || data.picks.length === 0} onClick={() => void handleComplete()}>
            Toplamayı bitir
          </button>
        )}
      </p>
    </>
  )
}
