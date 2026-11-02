import { useState } from 'react'
import type { FormEvent } from 'react'
import { ErrorAlert } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { todayIsoDate } from '../../utils/format'
import { useCurrencyOptions } from '../finance/currencyOptions'
import { LookupSelect } from '../masterData/LookupSelect'
import { ormeOrderOwnershipLabels, useSaveOrmeOrderMutation } from './ormeOrdersApi'
import type { OrmeOrder, OrmeOrderOwnership } from './ormeOrdersApi'

interface LineDraft {
  key: number
  id: number | null
  fabricId: number | null
  kilograms: string
  unitPrice: string
  customerFabricCode: string
  notes: string
}

const emptyLine = (key: number): LineDraft => ({ key, id: null, fabricId: null, kilograms: '', unitPrice: '', customerFabricCode: '', notes: '' })

/** Yeni sipariş ya da taslak siparişin düzenlenmesi. */
export function OrmeOrderFormDialog({ order, onClose, onSaved }: { order?: OrmeOrder; onClose: () => void; onSaved: (id: number) => void }) {
  const [save, { isLoading, error }] = useSaveOrmeOrderMutation()
  const [orderDate, setOrderDate] = useState(order?.orderDate ?? todayIsoDate())
  const [customerId, setCustomerId] = useState<number | null>(order?.customerId ?? null)
  const [ownership, setOwnership] = useState<OrmeOrderOwnership>(order?.ownership ?? 'COMMISSION')
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
      fabricId: line.fabricId,
      kilograms: String(line.kilograms),
      unitPrice: line.unitPrice === null ? '' : String(line.unitPrice),
      customerFabricCode: line.customerFabricCode ?? '',
      notes: line.notes ?? '',
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
          fabricId: line.fabricId ?? 0,
          kilograms: Number(line.kilograms),
          unitPrice: line.unitPrice.trim() === '' ? null : Number(line.unitPrice),
          customerFabricCode: line.customerFabricCode.trim() || null,
          notes: line.notes.trim() || null,
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
          resource="orme-customers"
          label={ownership === 'COMMISSION' ? 'Müşteri' : 'Müşteri (boşsa stoğa üretim)'}
          required={ownership === 'COMMISSION'}
          value={customerId}
          onChange={setCustomerId}
        />
        <div className="form__pair">
          <label className="field">
            <span>İş türü</span>
            <select value={ownership} onChange={(event) => setOwnership(event.target.value as OrmeOrderOwnership)}>
              {Object.entries(ormeOrderOwnershipLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
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
            <LookupSelect resource="orme-fabrics" label="Kumaş" required idSuffix={`a${line.key}`} value={line.fabricId} onChange={(value) => update(line.key, { fabricId: value })} />
            <div className="form__pair">
              <label className="field">
                <span>Kilo</span>
                <input type="number" min="0" step="0.01" required value={line.kilograms} onChange={(event) => update(line.key, { kilograms: event.target.value })} />
              </label>
              <label className="field">
                <span>Birim fiyat (kilo)</span>
                <input type="number" min="0" step="0.0001" value={line.unitPrice} onChange={(event) => update(line.key, { unitPrice: event.target.value })} />
              </label>
            </div>
            <div className="form__pair">
              <label className="field">
                <span>Müşteri kumaş kodu</span>
                <input maxLength={50} placeholder="Müşterinin kendi kodu" value={line.customerFabricCode} onChange={(event) => update(line.key, { customerFabricCode: event.target.value })} />
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
