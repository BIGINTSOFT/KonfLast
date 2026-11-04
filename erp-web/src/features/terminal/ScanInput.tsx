import { ScanBarcode } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import type { FormEvent } from 'react'
import { cardScanHandlers } from '../rfid/cardScan'
import { formatStockQuantity } from '../stock/stockPresentation'
import type { ScanBalance } from './terminalApi'

interface ScanInputProps {
  label: string
  onScan: (code: string) => void
  busy?: boolean
  /** Değeri değişince kutu yeniden odaklanır (bir adım bitip sıradaki okutma beklenirken). */
  focusToken?: unknown
  placeholder?: string
}

/**
 * Barkod kutusu. El terminalinin okuyucusu klavye gibi davranır: kodu yazar ve Enter gönderir. Kod elle de
 * yazılabilir. Okutmadan sonra kutu boşalır ve odakta kalır; sıradaki okutma için dokunmak gerekmez.
 */
export function ScanInput({ label, onScan, busy, focusToken, placeholder = 'Barkodu okutun ya da yazın' }: ScanInputProps) {
  const [value, setValue] = useState('')
  const input = useRef<HTMLInputElement>(null)

  useEffect(() => {
    input.current?.focus()
  }, [focusToken])

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault()
    const code = value.trim()
    if (code) {
      onScan(code)
      setValue('')
    }
    input.current?.focus()
  }

  return (
    <form className="scan" onSubmit={handleSubmit}>
      <label htmlFor="terminal-scan">{label}</label>
      <div className="scan__row">
        <input
          id="terminal-scan"
          ref={input}
          type="text"
          autoComplete="off"
          autoCapitalize="characters"
          spellCheck={false}
          enterKeyHint="go"
          placeholder={placeholder}
          value={value}
          {...cardScanHandlers(setValue, { submitOnEnter: true })}
        />
        <button type="submit" className="button" disabled={busy || value.trim() === ''}>
          <ScanBarcode aria-hidden="true" />
          {busy ? 'Aranıyor…' : 'Bul'}
        </button>
      </div>
    </form>
  )
}

/** Stok satırları: nerede, hangi kart / top, ne kadar. Seçilebilir verilirse satıra dokunularak seçilir. */
export function BalanceList({ rows, onPick }: { rows: ScanBalance[]; onPick?: (row: ScanBalance) => void }) {
  if (rows.length === 0) {
    return <p className="muted">Stok yok.</p>
  }

  return (
    <ul className="terminal-list">
      {rows.map((row) => {
        const content = (
          <>
            <span className="terminal-list__main">
              <code>{row.locationCode}</code> <span className="muted">{row.warehouseCode}</span>
              <br />
              <code>{row.itemCode}</code> {row.itemName}
              {row.lotNo && (
                <>
                  <br />
                  <code>{row.lotNo}</code>
                  {row.batchNo && <span className="muted"> · parti {row.batchNo}</span>}
                </>
              )}
            </span>
            <strong>{formatStockQuantity(row.quantity, row.unit)}</strong>
          </>
        )
        const key = `${row.locationId}-${row.stockItemId}-${row.lotId ?? 0}`
        return (
          <li key={key}>
            {onPick ? (
              <button type="button" className="terminal-list__row terminal-list__row--pick" onClick={() => onPick(row)}>
                {content}
              </button>
            ) : (
              <div className="terminal-list__row">{content}</div>
            )}
          </li>
        )
      })}
    </ul>
  )
}
