import { useState } from 'react'
import type { FormEvent } from 'react'
import { ErrorAlert } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { todayIsoDate } from '../../utils/format'
import { useCurrencyOptions } from '../finance/currencyOptions'
import { LookupSelect } from '../masterData/LookupSelect'
import { orderOwnershipLabels, orderProcessTypeLabels, useSaveBoyahaneOrderMutation } from './boyahaneOrdersApi'
import type { BillingUnit, BoyahaneOrder, OrderOwnership, OrderProcessType, OrderUnit } from './boyahaneOrdersApi'

interface LineDraft {
  key: number
  id: number | null
  processType: OrderProcessType
  fabricId: number | null
  yarnId: number | null
  colorName: string
  colorCode: string
  colorCardId: number | null
  patternId: number | null
  colorwayId: number | null
  quantity: string
  unit: OrderUnit
  billingUnit: BillingUnit | ''
  unitPrice: string
  notes: string
}

const emptyLine = (key: number): LineDraft => ({
  key, id: null, processType: 'FABRIC_DYEING', fabricId: null, yarnId: null, colorName: '', colorCode: '', colorCardId: null, patternId: null, colorwayId: null, quantity: '', unit: 'METER', billingUnit: '', unitPrice: '', notes: '',
})

/** Renk kartı (boyamada) ya da desen kartı (baskıda) seçiliyse ad ve kod karttan gelir; serbest metin girilmez. */
const fromArchive = (line: LineDraft) =>
  ((line.processType === 'FABRIC_DYEING' || line.processType === 'YARN_DYEING') && line.colorCardId !== null)
  || (line.processType === 'PRINTING' && line.patternId !== null)

/** Yeni sipariş ya da taslak siparişin düzenlenmesi. */
export function BoyahaneOrderFormDialog({ order, onClose, onSaved }: { order?: BoyahaneOrder; onClose: () => void; onSaved: (id: number) => void }) {
  const [save, { isLoading, error }] = useSaveBoyahaneOrderMutation()
  const [orderDate, setOrderDate] = useState(order?.orderDate ?? todayIsoDate())
  const [customerId, setCustomerId] = useState<number | null>(order?.customerId ?? null)
  const [ownership, setOwnership] = useState<OrderOwnership>(order?.ownership ?? 'COMMISSION')
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
      processType: line.processType,
      fabricId: line.fabricId,
      yarnId: line.yarnId,
      colorName: line.colorName ?? '',
      colorCode: line.colorCode ?? '',
      colorCardId: line.colorCardId,
      patternId: line.patternId,
      colorwayId: line.colorwayId,
      quantity: String(line.quantity),
      unit: line.unit,
      billingUnit: line.billingUnit ?? '',
      unitPrice: line.unitPrice === null ? '' : String(line.unitPrice),
      notes: line.notes ?? '',
    }))
    : [emptyLine(0)])

  const update = (key: number, change: Partial<LineDraft>) =>
    setLines(lines.map((line) => (line.key === key ? { ...line, ...change } : line)))

  const addLine = () => {
    setLines([...lines, emptyLine(nextKey)])
    setNextKey(nextKey - 1)
  }

  // İplik kilogram ile takip edilir; iş türü değişince seçim tutarlı kalır.
  const changeProcess = (line: LineDraft, processType: OrderProcessType) =>
    update(line.key, { processType, unit: processType === 'YARN_DYEING' ? 'KILOGRAM' : line.unit })

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
          processType: line.processType,
          fabricId: line.processType === 'YARN_DYEING' ? null : line.fabricId,
          yarnId: line.processType === 'YARN_DYEING' ? line.yarnId : null,
          colorName: line.colorName.trim() || null,
          colorCode: line.colorCode.trim() || null,
          colorCardId: line.processType === 'FABRIC_DYEING' || line.processType === 'YARN_DYEING' ? line.colorCardId : null,
          patternId: line.processType === 'PRINTING' ? line.patternId : null,
          colorwayId: line.processType === 'PRINTING' && line.patternId !== null ? line.colorwayId : null,
          quantity: Number(line.quantity),
          unit: line.unit,
          billingUnit: line.billingUnit || null,
          unitPrice: line.unitPrice.trim() === '' ? null : Number(line.unitPrice),
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
          resource="boyahane-customers"
          label={ownership === 'COMMISSION' ? 'Müşteri' : 'Müşteri (boşsa stoğa üretim)'}
          required={ownership === 'COMMISSION'}
          value={customerId}
          onChange={setCustomerId}
        />
        <div className="form__pair">
          <label className="field">
            <span>İş türü</span>
            <select value={ownership} onChange={(event) => setOwnership(event.target.value as OrderOwnership)}>
              {Object.entries(orderOwnershipLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
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
            <div className="form__pair">
              <label className="field">
                <span>İşlem türü</span>
                <select value={line.processType} onChange={(event) => changeProcess(line, event.target.value as OrderProcessType)}>
                  {Object.entries(orderProcessTypeLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
                </select>
              </label>
              {line.processType === 'YARN_DYEING' ? (
                <LookupSelect resource="boyahane-yarns" label="İplik kartı" required idSuffix={`y${line.key}`} value={line.yarnId} onChange={(value) => update(line.key, { yarnId: value })} />
              ) : (
                <LookupSelect resource="boyahane-fabrics" label="Kumaş kartı" required idSuffix={`f${line.key}`} value={line.fabricId} onChange={(value) => update(line.key, { fabricId: value })} />
              )}
            </div>
            {(line.processType === 'FABRIC_DYEING' || line.processType === 'YARN_DYEING') && (
              <LookupSelect
                resource="boyahane-color-cards"
                label="Renk kartı (isteğe bağlı)"
                idSuffix={`c${line.key}`}
                customerId={customerId}
                value={line.colorCardId}
                onChange={(value) => update(line.key, { colorCardId: value })}
              />
            )}
            {line.processType === 'PRINTING' && (
              <div className="form__pair">
                <LookupSelect
                  resource="boyahane-patterns"
                  label="Desen kartı (isteğe bağlı)"
                  idSuffix={`p${line.key}`}
                  customerId={customerId}
                  value={line.patternId}
                  onChange={(value) => update(line.key, { patternId: value, colorwayId: value === line.patternId ? line.colorwayId : null })}
                />
                {line.patternId !== null && (
                  <LookupSelect
                    resource={`boyahane-patterns/${line.patternId}/colorways`}
                    label="Renk yolu"
                    idSuffix={`w${line.key}`}
                    value={line.colorwayId}
                    onChange={(value) => update(line.key, { colorwayId: value })}
                  />
                )}
              </div>
            )}
            {fromArchive(line) ? (
              <p className="muted form__note">
                {line.processType === 'PRINTING' ? 'Desen adı ve kodu desen kartından (ve renk yolundan) gelir.' : 'Renk adı ve kodu renk kartından gelir.'}
              </p>
            ) : (
              <div className="form__pair">
                <label className="field">
                  <span>{line.processType === 'PRINTING' ? 'Desen' : 'Renk'}{line.processType === 'FINISHING' ? ' (isteğe bağlı)' : ''}</span>
                  <input maxLength={100} value={line.colorName} onChange={(event) => update(line.key, { colorName: event.target.value })} />
                </label>
                <label className="field">
                  <span>Müşteri renk / desen kodu</span>
                  <input maxLength={50} value={line.colorCode} onChange={(event) => update(line.key, { colorCode: event.target.value })} />
                </label>
              </div>
            )}
            <div className="form__pair">
              <label className="field">
                <span>Miktar</span>
                <input type="number" min="0" step="0.01" required value={line.quantity} onChange={(event) => update(line.key, { quantity: event.target.value })} />
              </label>
              <label className="field">
                <span>Birim</span>
                <select value={line.unit} disabled={line.processType === 'YARN_DYEING'} onChange={(event) => update(line.key, { unit: event.target.value as OrderUnit })}>
                  <option value="METER">Metre</option>
                  <option value="KILOGRAM">Kilogram</option>
                </select>
              </label>
            </div>
            <div className="form__pair">
              <label className="field">
                <span>Fatura ölçüsü</span>
                <select value={line.billingUnit} onChange={(event) => update(line.key, { billingUnit: event.target.value as BillingUnit | '' })}>
                  <option value="">Müşteri ayarı</option>
                  <option value="ORDER_UNIT">Sipariş birimi</option>
                  {line.processType !== 'YARN_DYEING' && <option value="METER">Metre</option>}
                  <option value="KILOGRAM">Kilo</option>
                </select>
              </label>
              <label className="field">
                <span>Birim fiyat (fatura ölçüsü)</span>
                <input type="number" min="0" step="0.0001" value={line.unitPrice} onChange={(event) => update(line.key, { unitPrice: event.target.value })} />
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
