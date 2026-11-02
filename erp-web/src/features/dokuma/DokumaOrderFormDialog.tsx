import { useState } from 'react'
import type { FormEvent } from 'react'
import { ErrorAlert } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { todayIsoDate } from '../../utils/format'
import { useCurrencyOptions } from '../finance/currencyOptions'
import { LookupSelect } from '../masterData/LookupSelect'
import { dokumaOrderOwnershipLabels, useSaveDokumaOrderMutation } from './dokumaOrdersApi'
import type { DokumaOrder, DokumaOrderOwnership } from './dokumaOrdersApi'

interface LineDraft {
  key: number
  id: number | null
  articleId: number | null
  meters: string
  unitPrice: string
  notes: string
  customerArticleCode: string
}

const emptyLine = (key: number): LineDraft => ({ key, id: null, articleId: null, meters: '', unitPrice: '', notes: '', customerArticleCode: '' })

/** Yeni sipariş ya da taslak siparişin düzenlenmesi. */
export function DokumaOrderFormDialog({ order, onClose, onSaved }: { order?: DokumaOrder; onClose: () => void; onSaved: (id: number) => void }) {
  const [save, { isLoading, error }] = useSaveDokumaOrderMutation()
  const [orderDate, setOrderDate] = useState(order?.orderDate ?? todayIsoDate())
  const [customerId, setCustomerId] = useState<number | null>(order?.customerId ?? null)
  const [ownership, setOwnership] = useState<DokumaOrderOwnership>(order?.ownership ?? 'COMMISSION')
  const [customerOrderNo, setCustomerOrderNo] = useState(order?.customerOrderNo ?? '')
  const [dueDate, setDueDate] = useState(order?.dueDate ?? '')
  const [currency, setCurrency] = useState(order?.currency ?? '')
  const [notes, setNotes] = useState(order?.notes ?? '')
  const currencies = useCurrencyOptions(order?.currency)
  const [nextKey, setNextKey] = useState(-1)
  const [lines, setLines] = useState<LineDraft[]>(order
    ? order.lines.map((line) => ({
      key: line.id,
      id: line.id,
      articleId: line.articleId,
      meters: String(line.meters),
      unitPrice: line.unitPrice === null ? '' : String(line.unitPrice),
      notes: line.notes ?? '',
      customerArticleCode: line.customerArticleCode ?? '',
    }))
    : [emptyLine(0)])

  const update = (key: number, change: Partial<LineDraft>) =>
    setLines(lines.map((line) => (line.key === key ? { ...line, ...change } : line)))

  const addLine = () => {
    setLines([...lines, emptyLine(nextKey)])
    setNextKey(nextKey - 1)
  }

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await save({
      id: order?.id,
      body: {
        customerId,
        orderDate,
        ownership,
        customerOrderNo: customerOrderNo.trim() || null,
        dueDate: dueDate || null,
        currency: currency || null,
        notes: notes.trim() || null,
        lines: lines.map((line) => ({
          id: line.id,
          articleId: line.articleId ?? 0,
          meters: Number(line.meters),
          unitPrice: line.unitPrice.trim() === '' ? null : Number(line.unitPrice),
          notes: line.notes.trim() || null,
          customerArticleCode: line.customerArticleCode.trim() || null,
        })),
      },
    })
    if ('data' in result && result.data) {
      onSaved(result.data.id)
    }
  }

  return (
    <Modal title={order ? `Sipariş düzenle: ${order.orderNo}` : 'Yeni sipariş'} onClose={onClose} variant="drawer">
      <form className="form" onSubmit={handleSubmit}>
        <LookupSelect
          resource="dokuma-customers"
          label={ownership === 'COMMISSION' ? 'Müşteri' : 'Müşteri (boşsa stoğa üretim)'}
          required={ownership === 'COMMISSION'}
          value={customerId}
          onChange={setCustomerId}
        />
        <div className="form__pair">
          <label className="field">
            <span>İş türü</span>
            <select value={ownership} onChange={(event) => setOwnership(event.target.value as DokumaOrderOwnership)}>
              {Object.entries(dokumaOrderOwnershipLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
            </select>
          </label>
          <label className="field">
            <span>Müşteri sipariş no</span>
            <input maxLength={50} value={customerOrderNo} onChange={(event) => setCustomerOrderNo(event.target.value)} />
          </label>
        </div>
        <div className="form__pair">
          <label className="field">
            <span>Sipariş tarihi</span>
            <input type="date" required value={orderDate} onChange={(event) => setOrderDate(event.target.value)} />
          </label>
          <label className="field">
            <span>Termin</span>
            <input type="date" value={dueDate} onChange={(event) => setDueDate(event.target.value)} />
          </label>
        </div>
        <label className="field">
          <span>Para birimi</span>
          <select value={currency} onChange={(event) => setCurrency(event.target.value)}>
            <option value="">{customerId === null ? 'Varsayılan (TRY)' : 'Müşterinin para birimi'}</option>
            {currencies.map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}
          </select>
        </label>
        <label className="field">
          <span>Not</span>
          <textarea rows={2} maxLength={1000} value={notes} onChange={(event) => setNotes(event.target.value)} />
        </label>

        {lines.map((line, index) => (
          <fieldset key={line.key} className="form__section">
            <legend>{index + 1}. satır</legend>
            <LookupSelect resource="dokuma-articles" label="Artikel" required idSuffix={`a${line.key}`} value={line.articleId} onChange={(value) => update(line.key, { articleId: value })} />
            <div className="form__pair">
              <label className="field">
                <span>Metre</span>
                <input type="number" min="0" step="0.01" required value={line.meters} onChange={(event) => update(line.key, { meters: event.target.value })} />
              </label>
              <label className="field">
                <span>Birim fiyat (metre)</span>
                <input type="number" min="0" step="0.0001" value={line.unitPrice} onChange={(event) => update(line.key, { unitPrice: event.target.value })} />
              </label>
            </div>
            <div className="form__pair">
              <label className="field">
                <span>Müşteri artikel kodu</span>
                <input
                  maxLength={50}
                  value={line.customerArticleCode}
                  title="Müşterinin bu kumaş için kullandığı kod; etikete ve sevk belgelerine basılır."
                  onChange={(event) => update(line.key, { customerArticleCode: event.target.value })}
                />
              </label>
              <label className="field">
                <span>Satır notu</span>
                <input maxLength={500} value={line.notes} onChange={(event) => update(line.key, { notes: event.target.value })} />
              </label>
            </div>
            {lines.length > 1 && (
              <div>
                <button type="button" className="button button--danger-ghost" onClick={() => setLines(lines.filter((item) => item.key !== line.key))}>Satırı kaldır</button>
              </div>
            )}
          </fieldset>
        ))}
        <div>
          <button type="button" className="button" onClick={addLine}>Satır ekle</button>
        </div>

        <ErrorAlert error={error} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading}>Kaydet</button>
        </footer>
      </form>
    </Modal>
  )
}
