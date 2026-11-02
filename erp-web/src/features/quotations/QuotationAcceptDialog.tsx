import { Plus, Trash2 } from 'lucide-react'
import { useState } from 'react'
import { fieldErrors } from '../../api/envelope'
import { ErrorAlert, TextField } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { formatQuantity, todayIsoDate } from '../../utils/format'
import { formatRate } from '../../utils/money'
import { LookupSelect } from '../masterData/LookupSelect'
import { useAcceptQuotationMutation } from './quotationsApi'
import type { Quotation } from './quotationsApi'
import { lineTitle } from './quotationPresentation'

interface BreakdownRow {
  key: string
  lineId: number
  colorId: number | null
  sizeId: number | null
  quantity: string
}

/**
 * Teklifin kabulü: ürünlü her satırın renk / beden kırılımı girilir (varsayılan: satır adedinde tek kırılım, renk ve
 * beden seçilir). Teklif fiyatlarıyla taslak müşteri siparişi açılır; sipariş ekranından onaya gönderilir.
 */
export function QuotationAcceptDialog({
  quotation, onClose, onAccepted,
}: { quotation: Quotation; onClose: () => void; onAccepted: (quotation: Quotation) => void }) {
  const [accept, { isLoading, error }] = useAcceptQuotationMutation()
  const productLines = quotation.lines.filter((line) => line.productId !== null)
  const [customerId, setCustomerId] = useState<number | null>(quotation.customerId)
  const [dueDate, setDueDate] = useState(todayIsoDate())
  const [customerOrderNo, setCustomerOrderNo] = useState('')
  const [rows, setRows] = useState<BreakdownRow[]>(() =>
    productLines.map((line) => ({ key: `r-${line.id}-0`, lineId: line.id, colorId: null, sizeId: null, quantity: String(line.quantity) })))

  const update = (key: string, patch: Partial<BreakdownRow>) =>
    setRows((current) => current.map((row) => (row.key === key ? { ...row, ...patch } : row)))

  const handleSubmit = async () => {
    const result = await accept({
      id: quotation.id,
      body: {
        rowVersion: quotation.rowVersion,
        customerId: quotation.customerId === null ? customerId : null,
        dueDate,
        customerOrderNo: customerOrderNo.trim() || null,
        lines: rows.map((row) => ({
          quotationLineId: row.lineId,
          colorId: row.colorId ?? 0,
          sizeId: row.sizeId ?? 0,
          quantity: Number(row.quantity) || 0,
        })),
      },
    })
    if ('data' in result && result.data) {
      onAccepted(result.data)
    }
  }

  const skipped = quotation.lines.length - productLines.length

  return (
    <Modal title={`Teklifi kabul et: ${quotation.quotationNo}`} onClose={onClose} variant="drawer">
      <div className="form">
        <p className="muted">
          Teklif fiyatlarıyla taslak müşteri siparişi açılır. Her ürün satırı için renk ve beden seçin; bir satır birden
          fazla renk / bedene bölünebilir.
        </p>
        {skipped > 0 && (
          <p className="alert alert--info">Ürün kartı olmayan {skipped} satır siparişe aktarılmaz; gerekirse ürün kartını açıp siparişe elle ekleyin.</p>
        )}
        <div className="form__grid">
          {quotation.customerId === null && (
            <LookupSelect
              resource="customers"
              label={`Müşteri (aday: ${quotation.prospectName ?? ''})`}
              value={customerId}
              onChange={setCustomerId}
              required
              errors={fieldErrors(error, 'customerId')}
            />
          )}
          <TextField label="Teslim tarihi" type="date" min={todayIsoDate()} value={dueDate} onChange={(event) => setDueDate(event.target.value)} errors={fieldErrors(error, 'dueDate')} />
          <TextField label="Müşteri sipariş no" value={customerOrderNo} maxLength={50} onChange={(event) => setCustomerOrderNo(event.target.value)} errors={fieldErrors(error, 'customerOrderNo')} />
        </div>

        <div className="table-wrap">
          <table className="table table--compact">
            <thead>
              <tr>
                <th>Satır</th>
                <th>Renk</th>
                <th>Beden</th>
                <th className="table__number">Adet</th>
                <th className="table__number">Birim fiyat</th>
                <th aria-label="İşlemler" />
              </tr>
            </thead>
            <tbody>
              {productLines.map((line) => {
                const own = rows.filter((row) => row.lineId === line.id)
                const total = own.reduce((sum, row) => sum + (Number(row.quantity) || 0), 0)
                return own.map((row, index) => (
                  <tr key={row.key}>
                    {index === 0 && (
                      <td rowSpan={own.length}>
                        <strong>{line.lineNo}.</strong> {lineTitle(line)}
                        {line.colors && <div className="muted">{line.colors}</div>}
                        <div className={total === line.quantity ? 'muted' : 'field__error'}>
                          Teklif {formatQuantity(line.quantity)} · girilen {formatQuantity(total)}
                        </div>
                        <button
                          type="button"
                          className="button button--inline"
                          onClick={() => setRows((current) => [...current, { key: `r-${line.id}-${Date.now()}`, lineId: line.id, colorId: null, sizeId: null, quantity: '' }])}
                        >
                          <Plus aria-hidden="true" />
                          Kırılım ekle
                        </button>
                      </td>
                    )}
                    <td>
                      <LookupSelect resource="colors" label="Renk" hideLabel idSuffix={row.key} value={row.colorId} onChange={(value) => update(row.key, { colorId: value })} required />
                    </td>
                    <td>
                      <LookupSelect resource="sizes" label="Beden" hideLabel idSuffix={row.key} value={row.sizeId} onChange={(value) => update(row.key, { sizeId: value })} required />
                    </td>
                    <td>
                      <input type="number" min={1} step={1} className="matrix-input" aria-label="Adet" value={row.quantity} onChange={(event) => update(row.key, { quantity: event.target.value })} />
                    </td>
                    <td className="table__number">{formatRate(line.unitPrice, quotation.currency)}</td>
                    <td className="table__actions">
                      {own.length > 1 && (
                        <button type="button" className="button button--ghost button--icon" aria-label="Kırılımı kaldır" onClick={() => setRows((current) => current.filter((other) => other.key !== row.key))}>
                          <Trash2 aria-hidden="true" />
                        </button>
                      )}
                    </td>
                  </tr>
                ))
              })}
            </tbody>
          </table>
        </div>

        <ErrorAlert error={error} fields={['customerId', 'dueDate', 'customerOrderNo']} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="button" className="button button--primary" disabled={isLoading || productLines.length === 0} onClick={() => void handleSubmit()}>
            {isLoading ? 'Sipariş açılıyor…' : 'Kabul et ve sipariş aç'}
          </button>
        </footer>
      </div>
    </Modal>
  )
}
