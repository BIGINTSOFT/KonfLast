import { useEffect, useRef, useState } from 'react'
import type { FormEvent } from 'react'
import { ErrorAlert } from '../../components/FormControls'
import type { StockUnit } from '../stock/stockApi'
import {
  useAddStockCountLineMutation, useGetStockCountQuery, useGetStockCountsQuery, useSaveStockCountLinesMutation,
} from '../stock/stockCountsApi'
import type { StockCount } from '../stock/stockCountsApi'
import { formatStockQuantity, parseQuantity, quantityStep, stockUnitLabels } from '../stock/stockPresentation'
import { ScanInput } from './ScanInput'
import { useScanner } from './terminalApi'

/** Miktarı girilmek üzere okutulmuş kart / top. lineId boşsa fişte yoktur; kayıtta yeni satır açılır. */
interface Entry {
  stockItemId: number
  itemCode: string
  itemName: string
  unit: StockUnit
  lotId: number | null
  lotNo: string | null
  locationId: number
  locationCode: string
  lineId: number | null
  previous: number | null
}

function CountSheet({ count, refreshing }: { count: StockCount; refreshing: boolean }) {
  const scanner = useScanner()
  const [saveLines, saveState] = useSaveStockCountLinesMutation()
  const [addLine, addState] = useAddStockCountLineMutation()
  const [shelf, setShelf] = useState<{ id: number; code: string } | null>(null)
  const [entry, setEntry] = useState<Entry | null>(null)
  const [quantity, setQuantity] = useState('')
  const [problem, setProblem] = useState<string | null>(null)
  const [done, setDone] = useState<string | null>(null)
  const [step, setStep] = useState(0)
  const quantityInput = useRef<HTMLInputElement>(null)

  // Tek adres sayılıyorsa raf okutmaya gerek yoktur.
  const location = count.locationId !== null && count.locationCode !== null
    ? { id: count.locationId, code: count.locationCode }
    : shelf

  // Kart okutulunca sıradaki iş miktarı yazmaktır.
  useEffect(() => {
    if (entry) {
      quantityInput.current?.focus()
      quantityInput.current?.select()
    }
  }, [entry])

  const handleScan = async (code: string) => {
    setProblem(null)
    setDone(null)
    saveState.reset()
    addState.reset()

    const result = await scanner.scan(code, count.warehouseId)
    if (!result) {
      return
    }

    if (result.kind === 'LOCATION' && result.location) {
      const scanned = result.location
      if (scanned.warehouseId !== count.warehouseId) {
        setProblem(`${scanned.code} bu sayımın deposunda değil (${scanned.warehouseCode}).`)
      } else if (count.locationId !== null && count.locationId !== scanned.id) {
        setProblem(`Bu fiş yalnızca ${count.locationCode} adresini sayıyor.`)
      } else {
        setShelf({ id: scanned.id, code: scanned.code })
        setEntry(null)
        setStep((current) => current + 1)
      }
      return
    }

    const item = result.item
    if (!item) {
      return
    }

    if (item.isLotTracked && !result.lot) {
      setProblem(`${item.code} top / parti takipli: kartı değil, topun etiketini okutun.`)
      return
    }

    const lotId = result.lot?.id ?? null
    const lines = count.lines.filter((line) => line.stockItemId === item.id && line.lotId === lotId)
    const line = location ? lines.find((candidate) => candidate.locationId === location.id) : lines.length === 1 ? lines[0] : undefined
    const place = location ?? (line ? { id: line.locationId, code: line.locationCode } : null)
    if (!place) {
      setProblem(
        lines.length === 0
          ? 'Bu stok fişte yok. Bulunduğu rafın adresini okutun, sonra yeniden okutun.'
          : 'Bu stok fişte birden fazla adreste görünüyor. Önce saydığınız rafın adresini okutun.',
      )
      return
    }

    setEntry({
      stockItemId: item.id,
      itemCode: item.code,
      itemName: item.name,
      unit: item.unit,
      lotId,
      lotNo: result.lot?.lotNo ?? null,
      locationId: place.id,
      locationCode: place.code,
      lineId: line?.id ?? null,
      previous: line?.countedQuantity ?? null,
    })
    setQuantity(line?.countedQuantity === null || line?.countedQuantity === undefined ? '' : String(line.countedQuantity))
  }

  const parsed = parseQuantity(quantity)
  const busy = saveState.isLoading || addState.isLoading || refreshing

  const handleSave = async (event: FormEvent) => {
    event.preventDefault()
    if (!entry || parsed === null) {
      return
    }

    const result = entry.lineId !== null
      ? await saveLines({ id: count.id, rowVersion: count.rowVersion, lines: [{ lineId: entry.lineId, countedQuantity: parsed }] })
      : await addLine({
          id: count.id,
          rowVersion: count.rowVersion,
          stockItemId: entry.stockItemId,
          countedQuantity: parsed,
          locationId: entry.locationId,
          lotId: entry.lotId,
        })
    if (!('error' in result)) {
      setDone(`${entry.lotNo ?? entry.itemCode} · ${entry.locationCode}: ${formatStockQuantity(parsed, entry.unit)} kaydedildi.`)
      setEntry(null)
      setQuantity('')
      setStep((current) => current + 1)
    }
  }

  const counted = count.lines.filter((line) => line.countedQuantity !== null).length
  const shelfLines = location ? count.lines.filter((line) => line.locationId === location.id) : []

  return (
    <>
      <dl className="terminal-facts">
        <dt>Fiş</dt>
        <dd><code>{count.countNo}</code> · {count.warehouseCode}</dd>
        <dt>Raf</dt>
        <dd>{location ? <code>{location.code}</code> : <span className="muted">Raf adresini okutun</span>}</dd>
        <dt>İlerleme</dt>
        <dd>{counted} / {count.lines.length} satır sayıldı</dd>
      </dl>

      <ScanInput
        label={location ? 'Top ya da stok kartı okutun (raf değiştirmek için adres)' : 'Saydığınız rafın adresini okutun'}
        onScan={(code) => void handleScan(code)}
        busy={scanner.busy}
        focusToken={step}
      />

      {done && <p className="alert alert--ok" role="status">{done}</p>}
      {problem && <p className="alert alert--error" role="alert">{problem}</p>}
      <ErrorAlert error={scanner.error ?? saveState.error ?? addState.error} />

      {entry && (
        <form className="terminal-card" onSubmit={(event) => void handleSave(event)} noValidate>
          <h2>Sayılan miktar</h2>
          <p>
            <code>{entry.itemCode}</code> {entry.itemName}
            {entry.lotNo && <><br /><code>{entry.lotNo}</code></>}
            <br />
            <span className="muted">Adres </span><code>{entry.locationCode}</code>
          </p>
          {entry.lineId === null && <p className="muted">Fişte yok: yeni satır olarak eklenecek.</p>}
          {entry.previous !== null && (
            <p className="muted">Daha önce {formatStockQuantity(entry.previous, entry.unit)} girilmiş; yeni değer onun yerine geçer.</p>
          )}
          <label className="field">
            <span>Miktar ({stockUnitLabels[entry.unit]})</span>
            <input
              ref={quantityInput}
              type="number"
              inputMode="decimal"
              min={0}
              step={quantityStep(entry.unit)}
              value={quantity}
              aria-invalid={quantity !== '' && parsed === null}
              onChange={(event) => setQuantity(event.target.value)}
            />
          </label>
          <footer className="terminal-actions">
            <button type="button" className="button" onClick={() => { setEntry(null); setStep((current) => current + 1) }}>
              Vazgeç
            </button>
            <button type="submit" className="button button--primary" disabled={parsed === null || busy}>
              {saveState.isLoading || addState.isLoading ? 'Kaydediliyor…' : 'Kaydet'}
            </button>
          </footer>
        </form>
      )}

      {location && !entry && (
        <section className="terminal-card">
          <h2>{location.code} rafındaki satırlar</h2>
          {shelfLines.length === 0 ? (
            <p className="muted">Fişte bu raf için satır yok; bulduğunuz stoğu okutarak ekleyebilirsiniz.</p>
          ) : (
            <ul className="terminal-list">
              {shelfLines.map((line) => (
                <li key={line.id}>
                  <div className="terminal-list__row">
                    <span className="terminal-list__main">
                      <code>{line.itemCode}</code> {line.itemName}
                      {line.lotNo && <><br /><code>{line.lotNo}</code></>}
                    </span>
                    {line.countedQuantity === null
                      ? <span className="muted">Sayılmadı</span>
                      : <strong>{formatStockQuantity(line.countedQuantity, line.unit)}</strong>}
                  </div>
                </li>
              ))}
            </ul>
          )}
        </section>
      )}
    </>
  )
}

/**
 * Sayım: açık bir sayım fişine raf raf miktar girilir. Beklenen miktar gösterilmez (kör sayım); farklar ve
 * onay masaüstündeki Sayımlar ekranındadır.
 */
export function TerminalCount() {
  const open = useGetStockCountsQuery({ page: 1, pageSize: 50, status: 'OPEN' })
  const [selectedId, setSelectedId] = useState<number | null>(null)
  const counts = open.data?.items ?? []
  // Tek açık fiş varsa seçmeye gerek yoktur.
  const countId = selectedId ?? (counts.length === 1 ? counts[0].id : null)
  const count = useGetStockCountQuery(countId ?? 0, { skip: countId === null })

  if (open.isLoading) {
    return <p role="status">Yükleniyor…</p>
  }

  if (countId === null) {
    return (
      <>
        <ErrorAlert error={open.error} />
        {counts.length === 0 ? (
          <p className="alert alert--info">Açık sayım fişi yok. Sayım, masaüstündeki Sayımlar ekranından açılır.</p>
        ) : (
          <section className="terminal-card">
            <h2>Hangi sayım?</h2>
            <ul className="terminal-list">
              {counts.map((item) => (
                <li key={item.id}>
                  <button type="button" className="terminal-list__row terminal-list__row--pick" onClick={() => setSelectedId(item.id)}>
                    <span className="terminal-list__main">
                      <code>{item.countNo}</code>
                      <br />
                      {item.warehouseCode} — {item.locationCode ? `yalnızca ${item.locationCode}` : 'deponun tamamı'}
                    </span>
                    <span className="muted">{item.countedLineCount} / {item.lineCount}</span>
                  </button>
                </li>
              ))}
            </ul>
          </section>
        )}
      </>
    )
  }

  return (
    <>
      {count.isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={count.error} />
      {count.data && !count.data.isOpen && (
        <p className="alert alert--info">Bu fiş kapanmış ({count.data.countNo}); artık miktar girilemez.</p>
      )}
      {count.data?.isOpen && <CountSheet key={count.data.id} count={count.data} refreshing={count.isFetching} />}
      {counts.length > 1 && (
        <p><button type="button" className="button button--ghost" onClick={() => setSelectedId(null)}>Başka sayım seç</button></p>
      )}
    </>
  )
}
