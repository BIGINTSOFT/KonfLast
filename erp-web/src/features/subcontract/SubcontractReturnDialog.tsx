import { useState } from 'react'
import { fieldErrors } from '../../api/envelope'
import { ErrorAlert, TextField } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { formatQuantity, todayIsoDate } from '../../utils/format'
import { formatMoney, parseDecimal } from '../../utils/money'
import { LookupSelect } from '../masterData/LookupSelect'
import { useCreateSubcontractReturnMutation } from './subcontractApi'
import type { SubcontractReturn, SubcontractSend } from './subcontractApi'
import { parseCount } from './subcontractPresentation'

interface Row {
  good: string
  defective: string
  missing: string
  defectTypeId: number | null
  price: string
}

const priceText = (value: number | null) => (value === null ? '' : String(value).replace('.', ','))

/**
 * Fason dönüşü: satır başına sağlam, hatalı ve eksik adet (gönderilen = sağlam + hatalı + eksik). Sağlam adet demetin
 * operasyonuna üretilmiş olarak işlenir; hakediş = sağlam × fiyat. Kısmi dönüş yapılabilir.
 */
export function SubcontractReturnDialog({
  send, onClose, onSaved,
}: { send: SubcontractSend; onClose: () => void; onSaved: (ret: SubcontractReturn) => void }) {
  const [create, { isLoading, error }] = useCreateSubcontractReturnMutation()
  const openLines = send.lines.filter((line) => line.outstanding > 0)
  const [returnDate, setReturnDate] = useState(todayIsoDate())
  const [documentNo, setDocumentNo] = useState('')
  const [notes, setNotes] = useState('')
  const [rows, setRows] = useState<Record<number, Row>>(() =>
    Object.fromEntries(openLines.map((line) => [line.id, { good: '', defective: '', missing: '', defectTypeId: null, price: priceText(line.unitPrice) }])))

  const update = (id: number, patch: Partial<Row>) => setRows((current) => ({ ...current, [id]: { ...current[id], ...patch } }))

  /** Satırın tamamını sağlam döndü olarak doldurur. */
  const fillAll = () =>
    setRows((current) => Object.fromEntries(openLines.map((line) => [line.id, { ...current[line.id], good: String(line.outstanding), defective: '', missing: '' }])))

  const totalOf = (row: Row) => parseCount(row.good) + parseCount(row.defective) + parseCount(row.missing)
  const overflow = openLines.filter((line) => totalOf(rows[line.id]) > line.outstanding)

  const amounts = new Map<string, number>()
  for (const line of openLines) {
    const row = rows[line.id]
    const price = parseDecimal(row.price)
    if (price !== null && parseCount(row.good) > 0) {
      amounts.set(line.currency, (amounts.get(line.currency) ?? 0) + Math.round(parseCount(row.good) * price * 100) / 100)
    }
  }

  const handleSubmit = async () => {
    const result = await create({
      subcontractSendId: send.id,
      rowVersion: send.rowVersion,
      returnDate,
      supplierDocumentNo: documentNo.trim() || null,
      notes: notes.trim() || null,
      lines: openLines
        .filter((line) => totalOf(rows[line.id]) > 0)
        .map((line) => {
          const row = rows[line.id]
          return {
            sendLineId: line.id,
            goodQuantity: parseCount(row.good),
            defectiveQuantity: parseCount(row.defective),
            missingQuantity: parseCount(row.missing),
            defectTypeId: parseCount(row.defective) > 0 ? row.defectTypeId : null,
            unitPrice: row.price.trim() === '' ? null : parseDecimal(row.price),
          }
        }),
    })
    if ('data' in result && result.data) {
      onSaved(result.data)
    }
  }

  return (
    <Modal title={`Fason dönüşü: ${send.sendNo}`} onClose={onClose} variant="drawer">
      <div className="form">
        <p className="muted">
          {send.supplierName} · {send.operationName}. Sağlam adet demetin operasyonuna üretilmiş olarak işlenir; hatalı ve
          eksik adet ıskarta sayılır. Satırın tamamı dönünce demet sahaya döner.
        </p>
        <div className="form__grid">
          <TextField label="Dönüş tarihi" type="date" min={send.sendDate} value={returnDate} onChange={(event) => setReturnDate(event.target.value)} errors={fieldErrors(error, 'returnDate')} />
          <TextField label="Fasoncu irsaliye no" value={documentNo} maxLength={50} onChange={(event) => setDocumentNo(event.target.value)} />
        </div>

        <div className="toolbar">
          <button type="button" className="button" onClick={fillAll}>Tamamı sağlam döndü</button>
        </div>
        <div className="table-wrap">
          <table className="table table--compact">
            <thead>
              <tr>
                <th>Demet / ürün</th>
                <th>Renk / beden</th>
                <th className="table__number">Fasonda</th>
                <th className="table__number">Sağlam</th>
                <th className="table__number">Hatalı</th>
                <th className="table__number">Eksik</th>
                <th>Hata türü</th>
                <th className="table__number">Birim fiyat</th>
              </tr>
            </thead>
            <tbody>
              {openLines.map((line) => {
                const row = rows[line.id]
                const invalid = totalOf(row) > line.outstanding
                return (
                  <tr key={line.id}>
                    <td>
                      {line.bundleNo ? <code>{line.bundleNo}</code> : <span className="muted">demetsiz</span>}{' '}
                      <span className="muted">{line.productCode}</span>
                    </td>
                    <td>{line.colorName} / {line.sizeName}</td>
                    <td className="table__number">{formatQuantity(line.outstanding)}</td>
                    <td>
                      <input type="number" min={0} step={1} className="matrix-input" aria-label={`${line.bundleNo ?? line.productCode} sağlam`} aria-invalid={invalid} value={row.good} onChange={(event) => update(line.id, { good: event.target.value })} />
                    </td>
                    <td>
                      <input type="number" min={0} step={1} className="matrix-input" aria-label={`${line.bundleNo ?? line.productCode} hatalı`} aria-invalid={invalid} value={row.defective} onChange={(event) => update(line.id, { defective: event.target.value })} />
                    </td>
                    <td>
                      <input type="number" min={0} step={1} className="matrix-input" aria-label={`${line.bundleNo ?? line.productCode} eksik`} aria-invalid={invalid} value={row.missing} onChange={(event) => update(line.id, { missing: event.target.value })} />
                    </td>
                    <td>
                      {parseCount(row.defective) > 0 ? (
                        <LookupSelect resource="defect-types" label="Hata türü" hideLabel idSuffix={String(line.id)} value={row.defectTypeId} onChange={(value) => update(line.id, { defectTypeId: value })} />
                      ) : <span className="muted">—</span>}
                    </td>
                    <td>
                      <input inputMode="decimal" className="matrix-input" aria-label={`${line.bundleNo ?? line.productCode} birim fiyat (${line.currency})`} placeholder={line.currency} value={row.price} onChange={(event) => update(line.id, { price: event.target.value })} />
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
        {overflow.length > 0 && (
          <p className="alert alert--error" role="alert">Dönen adet fasonda kalan adedi aşamaz (gönderilen = sağlam + hatalı + eksik).</p>
        )}

        <label className="field">
          <span>Not</span>
          <textarea rows={2} maxLength={1000} value={notes} onChange={(event) => setNotes(event.target.value)} />
        </label>

        <ErrorAlert error={error} fields={['returnDate']} />
        <footer className="form__actions">
          <span className="muted form__actions-start">
            Hakediş: {amounts.size === 0 ? '—' : [...amounts.entries()].map(([currency, amount]) => formatMoney(amount, currency)).join(' · ')}
          </span>
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="button" className="button button--primary" disabled={isLoading || overflow.length > 0} onClick={() => void handleSubmit()}>
            {isLoading ? 'Kaydediliyor…' : 'Dönüşü kaydet'}
          </button>
        </footer>
      </div>
    </Modal>
  )
}
