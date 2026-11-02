import { Plus, X } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { fieldErrors } from '../../api/envelope'
import { ErrorAlert, TextField } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { todayIsoDate } from '../../utils/format'
import { LookupSelect } from '../masterData/LookupSelect'
import { useSavePurchaseOrderMutation } from './purchasingApi'
import type { PurchaseOrder } from './purchasingApi'
import { formatMoney, toItemsPayload } from './purchasingPresentation'
import { parseQuantity } from '../stock/stockPresentation'
import type { ItemRow } from './purchasingPresentation'

interface PurchaseOrderDialogProps {
  /** Düzenlenen taslak; yoksa yeni sipariş. */
  order?: PurchaseOrder
  /** Yeni siparişte önceden doldurulacak kalemler ve not (ör. bir üretim siparişinin eksik malzemeleri). */
  initialItems?: { stockItemId: number; quantity: number; productionOrderId?: number; productionOrderNo?: string }[]
  initialNotes?: string
  onClose: () => void
  onSaved?: (order: PurchaseOrder) => void
}

const emptyRow = (): ItemRow => ({ key: crypto.randomUUID(), stockItemId: null, quantity: '', unitPrice: '' })

/** Taslak satın alma siparişi: başlık ve kalemler. Kuralları (birim, yinelenen kart, tarih) sunucu denetler. */
export function PurchaseOrderDialog({ order, initialItems, initialNotes, onClose, onSaved }: PurchaseOrderDialogProps) {
  const [save, { isLoading, error }] = useSavePurchaseOrderMutation()

  const [supplierId, setSupplierId] = useState<number | null>(order?.supplierId ?? null)
  const [orderDate, setOrderDate] = useState(order?.orderDate ?? todayIsoDate())
  const [expectedDate, setExpectedDate] = useState(order?.expectedDate ?? '')
  const [currency, setCurrency] = useState(order?.currency ?? 'TRY')
  // Dövizli alışta kur: alış fiyatı bu kurla çevrilip stok maliyetine işlenir; onay için zorunludur.
  const costCurrency = order?.costCurrency ?? 'TRY'
  const [exchangeRate, setExchangeRate] = useState(order?.exchangeRate ? String(order.exchangeRate) : '')
  const foreign = currency.trim() !== '' && currency.trim() !== costCurrency
  const rate = Number(exchangeRate.replace(',', '.'))
  const [notes, setNotes] = useState(order?.notes ?? initialNotes ?? '')
  const [rows, setRows] = useState<ItemRow[]>(() =>
    order && order.items.length > 0
      ? order.items.map((item) => ({
          key: String(item.id),
          stockItemId: item.stockItemId,
          quantity: String(item.quantity),
          unitPrice: item.unitPrice === null ? '' : String(item.unitPrice),
          productionOrderId: item.productionOrderId,
          productionOrderNo: item.productionOrderNo,
        }))
      : initialItems && initialItems.length > 0
        ? initialItems.map((item) => ({
          ...emptyRow(),
          stockItemId: item.stockItemId,
          quantity: String(item.quantity),
          productionOrderId: item.productionOrderId ?? null,
          productionOrderNo: item.productionOrderNo ?? null,
        }))
        : [emptyRow()],
  )

  const items = toItemsPayload(rows)
  const updateRow = (key: string, change: Partial<ItemRow>) =>
    setRows((current) => current.map((row) => (row.key === key ? { ...row, ...change } : row)))

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    if (supplierId === null || items === null) {
      return
    }

    const result = await save({
      id: order?.id,
      body: {
        supplierId,
        orderDate,
        expectedDate: expectedDate || null,
        notes: notes.trim() || null,
        currency: currency.trim() || null,
        exchangeRate: foreign && exchangeRate.trim() !== '' && Number.isFinite(rate) ? rate : null,
        items,
        rowVersion: order?.rowVersion,
      },
    })
    if (!('error' in result)) {
      onSaved?.(result.data)
      onClose()
    }
  }

  return (
    <Modal title={order ? `Siparişi düzenle: ${order.orderNo}` : 'Yeni satın alma siparişi'} onClose={onClose}>
      <form className="form purchase-order-form" onSubmit={(event) => void handleSubmit(event)} noValidate>
        <div className="form__grid">
          <LookupSelect
            resource="suppliers"
            label="Tedarikçi"
            value={supplierId}
            onChange={setSupplierId}
            required
            errors={fieldErrors(error, 'supplierId')}
          />
          <TextField
            label="Sipariş tarihi"
            type="date"
            value={orderDate}
            onChange={(event) => setOrderDate(event.target.value)}
            errors={fieldErrors(error, 'orderDate')}
            required
          />
          <TextField
            label="Beklenen teslim tarihi (isteğe bağlı)"
            type="date"
            min={orderDate}
            value={expectedDate}
            onChange={(event) => setExpectedDate(event.target.value)}
            errors={fieldErrors(error, 'expectedDate')}
          />
          <TextField
            label="Para birimi"
            maxLength={3}
            value={currency}
            onChange={(event) => setCurrency(event.target.value.toUpperCase())}
            errors={fieldErrors(error, 'currency')}
            hint="Üç harfli kod: TRY, USD, EUR…"
          />
          {foreign && (
            <TextField
              label={`Kur (1 ${currency.trim()} = ? ${costCurrency})`}
              inputMode="decimal"
              value={exchangeRate}
              onChange={(event) => setExchangeRate(event.target.value)}
              errors={fieldErrors(error, 'exchangeRate')}
              hint="Onay için zorunlu; alış fiyatı bu kurla stok maliyetine işlenir."
              autoComplete="off"
            />
          )}
        </div>

        <fieldset className="form__section">
          <legend>Kalemler</legend>
          <div className="table-wrap recipe-table purchase-items">
            <table className="table table--compact">
              <colgroup>
                <col className="purchase-items__no" />
                <col />
                <col className="purchase-items__number" />
                <col className="purchase-items__number" />
                <col className="purchase-items__amount" />
                <col className="recipe-table__remove" />
              </colgroup>
              <thead>
                <tr>
                  <th>#</th>
                  <th>Stok kartı</th>
                  <th className="table__number">Miktar</th>
                  <th className="table__number">Birim fiyat</th>
                  <th className="table__number">Tutar</th>
                  <th aria-label="Satırı kaldır" />
                </tr>
              </thead>
              <tbody>
                {rows.map((row, index) => {
                  const quantity = parseQuantity(row.quantity)
                  const price = row.unitPrice.trim() ? parseQuantity(row.unitPrice) : null
                  return (
                    <tr key={row.key}>
                      <td className="purchase-items__index">{index + 1}</td>
                      <td>
                        <LookupSelect
                          resource="stock-items"
                          idSuffix={row.key}
                          label={`${index + 1}. stok kartı`}
                          hideLabel
                          placeholder="Stok kartı seçin…"
                          value={row.stockItemId}
                          onChange={(stockItemId) => updateRow(row.key, { stockItemId })}
                        />
                        {row.productionOrderId && (
                          <div className="purchase-items__link">
                            {row.productionOrderNo ?? 'Üretim siparişi'} eksik malzemesi
                            <button type="button" className="link-button" onClick={() => updateRow(row.key, { productionOrderId: null, productionOrderNo: null })}>
                              bağı kaldır
                            </button>
                          </div>
                        )}
                      </td>
                      <td>
                        <input
                          className="purchase-items__input"
                          aria-label={`${index + 1}. kalem miktar`}
                          inputMode="decimal"
                          value={row.quantity}
                          onChange={(event) => updateRow(row.key, { quantity: event.target.value })}
                        />
                      </td>
                      <td>
                        <input
                          className="purchase-items__input"
                          aria-label={`${index + 1}. kalem birim fiyat`}
                          inputMode="decimal"
                          placeholder="İsteğe bağlı"
                          value={row.unitPrice}
                          onChange={(event) => updateRow(row.key, { unitPrice: event.target.value })}
                        />
                      </td>
                      <td className="table__number">
                        {quantity !== null && price !== null ? formatMoney(quantity * price, currency.trim() || 'TRY') : '—'}
                      </td>
                      <td className="recipe-table__remove-cell">
                        <button
                          type="button"
                          className="icon-button"
                          aria-label={`${index + 1}. kalemi kaldır`}
                          title="Kalemi kaldır"
                          onClick={() => setRows((current) => current.filter((r) => r.key !== row.key))}
                        >
                          <X aria-hidden="true" />
                        </button>
                      </td>
                    </tr>
                  )
                })}
                {rows.length === 0 && (
                  <tr><td colSpan={6} className="table__empty">Kalem yok.</td></tr>
                )}
              </tbody>
            </table>
          </div>
          <div>
            <button type="button" className="button" onClick={() => setRows((current) => [...current, emptyRow()])}>
              <Plus aria-hidden="true" />
              Kalem ekle
            </button>
          </div>
          {items === null && (
            <small className="field__error">Her kalemde stok kartı ve geçerli bir miktar girilmelidir.</small>
          )}
          {fieldErrors(error, 'items')?.map((message) => (
            <small key={message} className="field__error">{message}</small>
          ))}
          <small className="field__hint">Miktar, stok kartının biriminde girilir; adetle izlenen kartta tam sayı olmalıdır.</small>
        </fieldset>

        <label className="field">
          <span>Not (isteğe bağlı)</span>
          <textarea rows={2} maxLength={1000} value={notes} onChange={(event) => setNotes(event.target.value)} />
        </label>

        <ErrorAlert error={error} fields={['supplierId', 'orderDate', 'expectedDate', 'currency', 'exchangeRate', 'items']} />

        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading || supplierId === null || items === null}>
            {isLoading ? 'Kaydediliyor…' : 'Kaydet'}
          </button>
        </footer>
      </form>
    </Modal>
  )
}
