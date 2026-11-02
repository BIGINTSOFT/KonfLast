import { Check } from 'lucide-react'
import { useState } from 'react'
import { fieldErrors } from '../../api/envelope'
import { ErrorAlert } from '../../components/FormControls'
import type { PickList, PickListLine } from '../costing/costingApi'
import { useGetLookupQuery } from '../masterData/masterDataApi'
import { formatStockQuantity } from '../stock/stockPresentation'
import { ScanInput } from './ScanInput'
import { useLazyGetTerminalPickListQuery } from './terminalApi'

const lineKey = (stockItemId: number, line: PickListLine) => `${stockItemId}|${line.warehouseId}|${line.locationCode}|${line.lotNo ?? ''}`

/**
 * Toplama: sipariş numarası okutulur, kalan ihtiyaç için hangi adresten hangi toptan ne kadar alınacağı listelenir.
 * Alınan top (ya da kart) okutuldukça satır işaretlenir. Stok bu ekranda düşmez: malzeme, demet açılırken aynı
 * sırayla stoktan çıkılır; ekran yanlış topun alınmasını önlemek içindir.
 */
export function TerminalPick() {
  const warehouses = useGetLookupQuery({ resource: 'warehouses', take: 100 })
  const [load, state] = useLazyGetTerminalPickListQuery()
  const [warehouseId, setWarehouseId] = useState<number | null>(null)
  const [list, setList] = useState<PickList | null>(null)
  const [picked, setPicked] = useState<Set<string>>(new Set())
  const [problem, setProblem] = useState<string | null>(null)
  const [step, setStep] = useState(0)

  const lines = (list?.items ?? []).flatMap((item) =>
    item.lines.map((line) => ({ item, line, key: lineKey(item.stockItemId, line) })),
  )

  const toggle = (key: string, on?: boolean) =>
    setPicked((current) => {
      const next = new Set(current)
      if (on ?? !next.has(key)) {
        next.add(key)
      } else {
        next.delete(key)
      }
      return next
    })

  const handleScan = async (code: string) => {
    setProblem(null)
    const text = code.toLocaleUpperCase('en-US')

    if (!list) {
      try {
        setList(await load({ orderNo: code, warehouseId: warehouseId ?? undefined }, false).unwrap())
        setPicked(new Set())
        setStep((current) => current + 1)
      } catch {
        // Hata (bulunamadı, depo belirsiz) aşağıda gösterilir.
      }
      return
    }

    // Top etiketi tek bir satırı gösterir; kart kodu, o kartın sıradaki toplanmamış satırını.
    const match =
      lines.find((entry) => entry.line.lotNo === text)
      ?? lines.find((entry) => entry.line.lotNo === null && entry.item.itemCode.toLocaleUpperCase('en-US') === text && !picked.has(entry.key))
    if (match) {
      toggle(match.key, true)
    } else if (lines.some((entry) => entry.item.itemCode.toLocaleUpperCase('en-US') === text)) {
      setProblem(`${text} top takipli ya da hepsi toplanmış: listedeki topun etiketini okutun.`)
    } else {
      setProblem(`${text} bu siparişin toplama listesinde yok. Yanlış top alınmış olabilir.`)
    }
  }

  return (
    <>
      {!list && (
        <label className="field">
          <span>Depo (isteğe bağlı)</span>
          <select value={warehouseId ?? ''} onChange={(event) => setWarehouseId(event.target.value ? Number(event.target.value) : null)}>
            <option value="">Siparişin malzeme deposu (yoksa tek hammadde deposu)</option>
            {warehouses.data?.map((warehouse) => (
              <option key={warehouse.id} value={warehouse.id}>{warehouse.code} — {warehouse.name}</option>
            ))}
          </select>
          {fieldErrors(state.isError ? state.error : undefined, 'warehouseId')?.map((message) => (
            <small key={message} className="field__error">{message}</small>
          ))}
        </label>
      )}

      <ScanInput
        label={list ? 'Aldığınız topu (ya da kartı) okutun' : 'Üretim siparişi numarasını okutun ya da yazın'}
        onScan={(code) => void handleScan(code)}
        busy={state.isFetching}
        focusToken={step}
      />

      {problem && <p className="alert alert--error" role="alert">{problem}</p>}
      {!list && <ErrorAlert error={state.isError ? state.error : undefined} fields={['warehouseId']} />}

      {list && (
        <>
          <dl className="terminal-facts">
            <dt>Sipariş</dt>
            <dd><code>{list.orderNo}</code> · {list.productCode}</dd>
            <dt>Depo</dt>
            <dd>{list.warehouseCode}</dd>
            <dt>İlerleme</dt>
            <dd>{lines.filter((entry) => picked.has(entry.key)).length} / {lines.length} satır toplandı</dd>
          </dl>

          {list.items.length === 0 && <p className="alert alert--info">Bu sipariş için toplanacak malzeme kalmamış.</p>}
          {list.items.map((item) => (
            <section key={item.stockItemId} className="terminal-card">
              <h2><code>{item.itemCode}</code> {item.itemName}</h2>
              <p className="muted">
                İhtiyaç {formatStockQuantity(item.need, item.unit)}
                {item.shortage > 0 && <strong> · {formatStockQuantity(item.shortage, item.unit)} eksik (hammadde depolarında yok)</strong>}
              </p>
              <ul className="terminal-list">
                {item.lines.map((line) => {
                  const key = lineKey(item.stockItemId, line)
                  const done = picked.has(key)
                  return (
                    <li key={key}>
                      <button
                        type="button"
                        className={done ? 'terminal-list__row terminal-list__row--pick terminal-list__row--done' : 'terminal-list__row terminal-list__row--pick'}
                        aria-pressed={done}
                        onClick={() => toggle(key)}
                      >
                        <span className="terminal-list__main">
                          {line.warehouseId !== list.warehouseId && <strong>{line.warehouseCode} · </strong>}
                          <code>{line.locationCode}</code>
                          {line.lotNo && <> · <code>{line.lotNo}</code></>}
                          {line.batchNo && <span className="muted"> · parti {line.batchNo}</span>}
                          {line.shade && <span className="muted"> · ton {line.shade}</span>}
                        </span>
                        <strong>
                          {done && <Check aria-hidden="true" />}
                          {formatStockQuantity(line.quantity, item.unit)}
                        </strong>
                      </button>
                    </li>
                  )
                })}
              </ul>
            </section>
          ))}

          <p className="muted">
            Stok bu ekranda düşmez; malzeme, demet açılırken bu listedeki sırayla stoktan çıkılır.
          </p>
          <p>
            <button type="button" className="button button--ghost" onClick={() => { setList(null); setProblem(null); setStep((current) => current + 1) }}>
              Başka sipariş
            </button>
          </p>
        </>
      )}
    </>
  )
}
