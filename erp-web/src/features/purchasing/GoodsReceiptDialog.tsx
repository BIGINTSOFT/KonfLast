import { useState } from 'react'
import type { FormEvent } from 'react'
import { fieldErrors } from '../../api/envelope'
import { ErrorAlert, TextField } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { useGetLookupQuery } from '../masterData/masterDataApi'
import { formatStockQuantity, parseQuantity, quantityStep } from '../stock/stockPresentation'
import { useReceiveGoodsMutation } from './purchasingApi'
import type { GoodsReceiptLine, PurchaseOrder, PurchaseOrderItem } from './purchasingApi'

/** Formdaki bir top satırı (metin olarak). */
interface RollDraft {
  quantity: string
  batchNo: string
  shade: string
  width: string
  supplierRollNo: string
}

const emptyRoll: RollDraft = { quantity: '', batchNo: '', shade: '', width: '', supplierRollNo: '' }

const rollFields: { name: keyof RollDraft; label: string; numeric?: boolean }[] = [
  { name: 'quantity', label: 'Miktar', numeric: true },
  { name: 'batchNo', label: 'Parti no' },
  { name: 'shade', label: 'Ton' },
  { name: 'width', label: 'En (cm)', numeric: true },
  { name: 'supplierRollNo', label: 'Ted. top no' },
]

/** Kalemin gönderilecek satırı; boş bırakılan kalem null, okunamayan giriş 'invalid' döner. */
function toLine(item: PurchaseOrderItem, text: string, rolls: RollDraft[] | undefined): GoodsReceiptLine | 'invalid' | null {
  if (rolls && rolls.length > 0) {
    const lots = rolls.map((roll) => ({
      quantity: parseQuantity(roll.quantity),
      batchNo: roll.batchNo.trim() || null,
      shade: roll.shade.trim() || null,
      width: roll.width.trim() === '' ? null : parseQuantity(roll.width),
      supplierRollNo: roll.supplierRollNo.trim() || null,
      widthInvalid: roll.width.trim() !== '' && !((parseQuantity(roll.width) ?? 0) > 0),
    }))
    if (lots.some((lot) => !((lot.quantity ?? 0) > 0) || lot.widthInvalid)) {
      return 'invalid'
    }

    // Kesirli toplamlarda kayan nokta artığı kalmasın: stok 3 ondalık basamak tutar.
    const total = Math.round(lots.reduce((sum, lot) => sum + (lot.quantity ?? 0), 0) * 1000) / 1000
    return {
      purchaseOrderItemId: item.id,
      quantity: total,
      lots: lots.map(({ widthInvalid: _ignored, ...lot }) => ({ ...lot, quantity: lot.quantity ?? 0 })),
    }
  }

  if (text.trim() === '') {
    return null
  }

  const quantity = parseQuantity(text.trim())
  return (quantity ?? 0) > 0 ? { purchaseOrderItemId: item.id, quantity: quantity ?? 0 } : 'invalid'
}

/** Hazır gelen hammadde deposunun kodu; varsa varsayılan olarak seçilir. */
const DEFAULT_WAREHOUSE_CODE = 'HAMMADDE'

/**
 * Mal kabul: gelen miktarlar girilir, stok aynı işlemde artar. Kalan miktarı aşan giriş sunucuda reddedilir.
 * İstek kimliği pencere açılırken bir kez üretilir: gönderim yarıda kalıp yinelenirse stok ikinci kez artmaz.
 * Top / parti takipli kalem toplara bölünebilir; bölünmezse kalemin tamamı tek top olarak açılır.
 */
export function GoodsReceiptDialog({ order, onClose }: { order: PurchaseOrder; onClose: () => void }) {
  const [requestId] = useState(() => crypto.randomUUID())
  const warehouses = useGetLookupQuery({ resource: 'warehouses', take: 100 })
  const [receive, { isLoading, error }] = useReceiveGoodsMutation()

  const pending = order.items.filter((item) => item.remainingQuantity > 0)
  const [selectedWarehouseId, setSelectedWarehouseId] = useState<number | null>(null)
  const [quantities, setQuantities] = useState<Record<number, string>>({})
  const [rolls, setRolls] = useState<Record<number, RollDraft[]>>({})
  const [documentNo, setDocumentNo] = useState('')
  const [note, setNote] = useState('')

  const warehouseId =
    selectedWarehouseId
    ?? warehouses.data?.find((warehouse) => warehouse.code === DEFAULT_WAREHOUSE_CODE)?.id
    ?? warehouses.data?.[0]?.id
    ?? null

  // Boş bırakılan kalem teslim alınmaz; okunamayan giriş gönderimi engeller.
  const entries = pending.map((item) => toLine(item, quantities[item.id] ?? '', rolls[item.id]))
  const lines = entries.filter((entry): entry is GoodsReceiptLine => entry !== null && entry !== 'invalid')
  const invalid = entries.includes('invalid')

  const changeRoll = (itemId: number, index: number, patch: Partial<RollDraft>) =>
    setRolls((current) => ({
      ...current,
      [itemId]: (current[itemId] ?? []).map((roll, i) => (i === index ? { ...roll, ...patch } : roll)),
    }))

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    if (warehouseId === null || invalid || lines.length === 0) {
      return
    }

    const result = await receive({
      id: order.id,
      body: { requestId, warehouseId, lines, documentNo: documentNo.trim() || null, note: note.trim() || null },
    })
    if (!('error' in result)) {
      onClose()
    }
  }

  return (
    <Modal title={`Mal kabul: ${order.orderNo}`} onClose={onClose}>
      <form className="form" onSubmit={(event) => void handleSubmit(event)} noValidate>
        <p className="muted">{order.supplierName}</p>

        <label className="field">
          <span>Teslim alınacak depo</span>
          <select
            value={warehouseId ?? ''}
            onChange={(event) => setSelectedWarehouseId(event.target.value ? Number(event.target.value) : null)}
            aria-invalid={Boolean(fieldErrors(error, 'warehouseId')?.length)}
          >
            {warehouses.data?.map((warehouse) => (
              <option key={warehouse.id} value={warehouse.id}>{warehouse.code} — {warehouse.name}</option>
            ))}
          </select>
          {fieldErrors(error, 'warehouseId')?.map((message) => (
            <small key={message} className="field__error">{message}</small>
          ))}
        </label>

        <fieldset className="checkbox-list">
          <legend>Gelen miktarlar</legend>
          {pending.map((item) => {
            const itemRolls = rolls[item.id] ?? []
            const line = entries[pending.indexOf(item)]
            const remaining = `Kalan ${formatStockQuantity(item.remainingQuantity, item.unit)} · sipariş ${formatStockQuantity(item.quantity, item.unit)}`
            return (
              <div key={item.id}>
                {itemRolls.length === 0 ? (
                  <TextField
                    label={`${item.itemCode} — ${item.itemName}`}
                    type="number"
                    min={0}
                    step={quantityStep(item.unit)}
                    value={quantities[item.id] ?? ''}
                    onChange={(event) => setQuantities((current) => ({ ...current, [item.id]: event.target.value }))}
                    hint={remaining}
                  />
                ) : (
                  <div className="field">
                    <span>{item.itemCode} — {item.itemName}</span>
                    <div className="lot-rows">
                      <div className="lot-rows__row lot-rows__head" aria-hidden="true">
                        {rollFields.map((field) => <span key={field.name}>{field.label}</span>)}
                        <span />
                      </div>
                      {itemRolls.map((roll, index) => (
                        // Satırların sırası sabittir; aradan silinince kalanlar kayar.
                        <div className="lot-rows__row" key={index}>
                          {rollFields.map((field) => (
                            <input
                              key={field.name}
                              type={field.numeric ? 'number' : 'text'}
                              min={field.numeric ? 0 : undefined}
                              step={field.name === 'quantity' ? quantityStep(item.unit) : field.numeric ? '0.01' : undefined}
                              maxLength={field.numeric ? undefined : field.name === 'shade' ? 30 : 50}
                              aria-label={`${index + 1}. top ${field.label}`}
                              value={roll[field.name]}
                              onChange={(event) => changeRoll(item.id, index, { [field.name]: event.target.value })}
                            />
                          ))}
                          <button
                            type="button"
                            className="button button--tiny"
                            aria-label={`${index + 1}. topu sil`}
                            onClick={() => setRolls((current) => ({ ...current, [item.id]: itemRolls.filter((_, i) => i !== index) }))}
                          >
                            Sil
                          </button>
                        </div>
                      ))}
                    </div>
                    <small className="field__hint">
                      {itemRolls.length} top{line && line !== 'invalid' ? ` · toplam ${formatStockQuantity(line.quantity, item.unit)}` : ''} · {remaining}
                    </small>
                  </div>
                )}
                {item.isLotTracked && (
                  <p>
                    <button
                      type="button"
                      className="button button--tiny"
                      onClick={() => setRolls((current) => ({ ...current, [item.id]: [...itemRolls, emptyRoll] }))}
                    >
                      {itemRolls.length === 0 ? 'Toplara böl' : 'Top ekle'}
                    </button>
                  </p>
                )}
              </div>
            )
          })}
          <p>
            <button
              type="button"
              className="button"
              onClick={() => setQuantities(Object.fromEntries(pending.map((item) => [item.id, String(item.remainingQuantity)])))}
            >
              Kalanın tamamını doldur
            </button>
          </p>
          {invalid && <small className="field__error">Miktarlar (ve girildiyse en) sıfırdan büyük sayılar olmalıdır.</small>}
          {fieldErrors(error, 'lines')?.map((message) => (
            <small key={message} className="field__error">{message}</small>
          ))}
          <small className="field__hint">
            Gelmeyen kalemi boş bırakın; eksik gelen miktar sonraki mal kabulde alınabilir. Top takipli kalemde her
            top ayrı satıra girilebilir; bölünmeyen kalem tek top olarak açılır.
          </small>
        </fieldset>

        <TextField
          label="İrsaliye no (isteğe bağlı)"
          maxLength={50}
          value={documentNo}
          onChange={(event) => setDocumentNo(event.target.value)}
          errors={fieldErrors(error, 'documentNo')}
        />
        <label className="field">
          <span>Not (isteğe bağlı)</span>
          <textarea rows={2} maxLength={300} value={note} onChange={(event) => setNote(event.target.value)} />
        </label>

        <ErrorAlert error={error ?? warehouses.error} fields={['warehouseId', 'lines', 'documentNo']} />

        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button
            type="submit"
            className="button button--primary"
            disabled={isLoading || warehouseId === null || invalid || lines.length === 0}
          >
            {isLoading ? 'Kaydediliyor…' : 'Teslim al'}
          </button>
        </footer>
      </form>
    </Modal>
  )
}
