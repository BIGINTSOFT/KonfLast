import { useState } from 'react'
import type { FormEvent } from 'react'
import { fieldErrors } from '../../api/envelope'
import { ErrorAlert, TextField } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { todayIsoDate } from '../../utils/format'
import { LookupSelect } from '../masterData/LookupSelect'
import { useGetLookupQuery } from '../masterData/masterDataApi'
import { useSaveShipmentMutation } from './shippingApi'
import type { Shipment } from './shippingApi'
import { toShipmentItems } from './shippingPresentation'
import type { ShipmentRow } from './shippingPresentation'

interface ShipmentDialogProps {
  /** Düzenlenen taslak; yoksa yeni sevkiyat. */
  shipment?: Shipment
  onClose: () => void
  onSaved?: (shipment: Shipment) => void
}

/** Hazır gelen mamul deposunun kodu; varsa varsayılan olarak seçilir. */
const DEFAULT_WAREHOUSE_CODE = 'MAMUL'

const emptyRow = (): ShipmentRow => ({ key: crypto.randomUUID(), stockItemId: null, quantity: '' })

/**
 * Taslak sevkiyat: başlık ve mamul kalemleri. Taslak stoğu etkilemez; kuralları (yalnızca mamul, sipariş
 * uyumu, stok yeterliliği) sunucu denetler.
 */
export function ShipmentDialog({ shipment, onClose, onSaved }: ShipmentDialogProps) {
  const [save, { isLoading, error }] = useSaveShipmentMutation()
  const warehouses = useGetLookupQuery({ resource: 'warehouses', take: 100, includeId: shipment?.warehouseId })

  const [customerId, setCustomerId] = useState<number | null>(shipment?.customerId ?? null)
  const [selectedWarehouseId, setSelectedWarehouseId] = useState<number | null>(shipment?.warehouseId ?? null)
  const [orderId, setOrderId] = useState<number | null>(shipment?.productionOrderId ?? null)
  // Üretim siparişine bağlı sevkiyatta müşteri siparişi ondan gelir; doğrudan seçilen sipariş birden fazla ürün taşıyabilir.
  const [salesOrderId, setSalesOrderId] = useState<number | null>(
    shipment && shipment.productionOrderId === null ? shipment.salesOrderId : null)
  const [shipDate, setShipDate] = useState(shipment?.shipDate ?? todayIsoDate())
  const [documentNo, setDocumentNo] = useState(shipment?.documentNo ?? '')
  const [carrier, setCarrier] = useState(shipment?.carrier ?? '')
  const [notes, setNotes] = useState(shipment?.notes ?? '')
  const [rows, setRows] = useState<ShipmentRow[]>(() =>
    shipment && shipment.items.length > 0
      ? shipment.items.map((item) => ({ key: String(item.id), stockItemId: item.stockItemId, quantity: String(item.quantity) }))
      : [emptyRow()],
  )

  const warehouseId =
    selectedWarehouseId
    ?? warehouses.data?.find((warehouse) => warehouse.code === DEFAULT_WAREHOUSE_CODE)?.id
    ?? warehouses.data?.[0]?.id
    ?? null
  const items = toShipmentItems(rows)
  const updateRow = (key: string, change: Partial<ShipmentRow>) =>
    setRows((current) => current.map((row) => (row.key === key ? { ...row, ...change } : row)))

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    if (customerId === null || warehouseId === null || items === null) {
      return
    }

    const result = await save({
      id: shipment?.id,
      body: {
        customerId,
        warehouseId,
        shipDate,
        items,
        productionOrderId: orderId,
        salesOrderId: orderId === null ? salesOrderId : null,
        documentNo: documentNo.trim() || null,
        carrier: carrier.trim() || null,
        notes: notes.trim() || null,
        rowVersion: shipment?.rowVersion,
      },
    })
    if (!('error' in result)) {
      onSaved?.(result.data)
      onClose()
    }
  }

  return (
    <Modal title={shipment ? `Sevkiyatı düzenle: ${shipment.shipmentNo}` : 'Yeni sevkiyat'} onClose={onClose}>
      <form className="form" onSubmit={(event) => void handleSubmit(event)} noValidate>
        <div className="form__grid">
          <LookupSelect
            resource="customers"
            label="Müşteri"
            value={customerId}
            onChange={setCustomerId}
            required
            errors={fieldErrors(error, 'customerId')}
          />
          <LookupSelect
            resource="shipments/sales-orders"
            label="Müşteri siparişi (isteğe bağlı)"
            value={orderId === null ? salesOrderId : null}
            onChange={setSalesOrderId}
            disabled={orderId !== null}
            errors={fieldErrors(error, 'salesOrderId')}
          />
          <LookupSelect
            resource="shipments/orders"
            label="Üretim siparişi (isteğe bağlı)"
            value={orderId}
            onChange={setOrderId}
            errors={fieldErrors(error, 'productionOrderId')}
          />
          <label className="field">
            <span>Çıkış deposu</span>
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
          <TextField
            label="Sevk tarihi"
            type="date"
            value={shipDate}
            onChange={(event) => setShipDate(event.target.value)}
            errors={fieldErrors(error, 'shipDate')}
            required
          />
          <TextField
            label="İrsaliye no (isteğe bağlı)"
            maxLength={50}
            value={documentNo}
            onChange={(event) => setDocumentNo(event.target.value)}
            errors={fieldErrors(error, 'documentNo')}
          />
          <TextField
            label="Taşıyıcı / plaka (isteğe bağlı)"
            maxLength={150}
            value={carrier}
            onChange={(event) => setCarrier(event.target.value)}
            errors={fieldErrors(error, 'carrier')}
          />
        </div>

        <fieldset className="checkbox-list">
          <legend>Sevk edilecek mamuller</legend>
          {rows.map((row, index) => (
            <div key={row.key} className="form__grid">
              <LookupSelect
                resource="stock-items"
                idSuffix={row.key}
                label={`${index + 1}. mamul kartı`}
                value={row.stockItemId}
                onChange={(stockItemId) => updateRow(row.key, { stockItemId })}
              />
              <TextField
                label="Adet"
                type="number"
                min={1}
                step="1"
                value={row.quantity}
                onChange={(event) => updateRow(row.key, { quantity: event.target.value })}
              />
              <div className="field">
                <button
                  type="button"
                  className="button"
                  onClick={() => setRows((current) => current.filter((r) => r.key !== row.key))}
                  aria-label={`${index + 1}. kalemi kaldır`}
                >
                  Kalemi kaldır
                </button>
              </div>
            </div>
          ))}
          <p>
            <button type="button" className="button" onClick={() => setRows((current) => [...current, emptyRow()])}>
              Kalem ekle
            </button>
          </p>
          {items === null && <small className="field__error">Her kalemde mamul kartı ve geçerli bir adet girilmelidir.</small>}
          {fieldErrors(error, 'items')?.map((message) => (
            <small key={message} className="field__error">{message}</small>
          ))}
          <small className="field__hint">
            Yalnızca mamul kartları sevk edilebilir. Sipariş seçildiyse mamuller o siparişin ürünü olmalıdır.
          </small>
        </fieldset>

        <label className="field">
          <span>Not (isteğe bağlı)</span>
          <textarea rows={2} maxLength={1000} value={notes} onChange={(event) => setNotes(event.target.value)} />
        </label>

        <ErrorAlert
          error={error ?? warehouses.error}
          fields={['customerId', 'productionOrderId', 'salesOrderId', 'warehouseId', 'shipDate', 'documentNo', 'carrier', 'items']}
        />

        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button
            type="submit"
            className="button button--primary"
            disabled={isLoading || customerId === null || warehouseId === null || items === null}
          >
            {isLoading ? 'Kaydediliyor…' : 'Taslağı kaydet'}
          </button>
        </footer>
      </form>
    </Modal>
  )
}
