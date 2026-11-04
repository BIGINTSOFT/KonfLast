import { useState } from 'react'
import type { FormEvent } from 'react'
import { fieldErrors } from '../../api/envelope'
import { ErrorAlert } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { RecordLink } from '../../components/RecordLink'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDate, formatDateTimeShort } from '../../utils/format'
import {
  useCreateStockReservationMutation,
  useGetStockReservationsQuery,
  useReleaseStockReservationMutation,
} from './stockApi'
import { formatStockQuantity, parseQuantity } from './stockPresentation'

interface StockReservationDialogProps {
  warehouseId: number
  stockItemId: number
  onClose: () => void
}

/**
 * Bir kartın bir depodaki ayırmaları. Hazır mamulde serbest (müşterisi olmayan) stok, aynı ürün / renk / bedeni
 * isteyen onaylı bir sipariş kalemine buradan ayrılır; ayırma o siparişin sevkiyatında kullanılır.
 */
export function StockReservationDialog({ warehouseId, stockItemId, onClose }: StockReservationDialogProps) {
  const canReserve = usePermission(Permissions.StockReserve)
  const { data, error, isLoading } = useGetStockReservationsQuery({ warehouseId, stockItemId })
  const [create, createState] = useCreateStockReservationMutation()
  const [release, releaseState] = useReleaseStockReservationMutation()
  const [lineId, setLineId] = useState<number | null>(null)
  const [quantity, setQuantity] = useState('')

  const candidate = data?.candidates.find((item) => item.salesOrderLineId === lineId) ?? null
  const suggested = candidate && data ? Math.min(candidate.open, data.free) : 0
  const amount = quantity.trim() ? parseQuantity(quantity) : suggested

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    if (!candidate || !amount) {
      return
    }

    const result = await create({ warehouseId, stockItemId, salesOrderLineId: candidate.salesOrderLineId, quantity: amount })
    if (!('error' in result)) {
      setLineId(null)
      setQuantity('')
    }
  }

  return (
    <Modal title="Stok ayırmaları" onClose={onClose}>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {data && (
        <div className="form">
          <p>
            <code>{data.itemCode}</code> {data.itemName}
            <br />
            <span className="muted">
              {data.warehouseCode} · mevcut <strong>{formatStockQuantity(data.onHand, data.unit)}</strong> · ayrılan{' '}
              {formatStockQuantity(data.reserved, data.unit)} · serbest <strong>{formatStockQuantity(data.free, data.unit)}</strong>
            </span>
          </p>

          <h3 className="section-title">Müşteri siparişlerine ayrılan</h3>
          <div className="table-wrap">
            <table className="table table--compact">
              <thead>
                <tr>
                  <th>Sipariş</th>
                  <th>Müşteri</th>
                  <th>Termin</th>
                  <th>Kaynak</th>
                  <th className="table__number">Adet</th>
                  <th>Son değişiklik</th>
                  {canReserve && <th aria-label="İşlemler" />}
                </tr>
              </thead>
              <tbody>
                {data.salesReservations.map((reservation) => (
                  <tr key={reservation.id}>
                    <td>
                      <RecordLink to={`/sales-orders/${reservation.salesOrderId}`}>{reservation.orderNo}</RecordLink>
                      {reservation.customerOrderNo && <span className="muted"> · {reservation.customerOrderNo}</span>}
                    </td>
                    <td>{reservation.customerName}</td>
                    <td className="table__nowrap">{formatDate(reservation.dueDate)}</td>
                    <td className="table__nowrap">{reservation.fromProduction ? 'Siparişin üretimi' : 'Stoktan'}</td>
                    <td className="table__number table__nowrap">{formatStockQuantity(reservation.quantity, data.unit)}</td>
                    <td className="muted table__nowrap">{reservation.updatedBy} · {formatDateTimeShort(reservation.updatedAt)}</td>
                    {canReserve && (
                      <td className="table__actions">
                        <button
                          type="button"
                          className="button button--tiny"
                          disabled={releaseState.isLoading}
                          onClick={() => {
                            if (window.confirm(`${reservation.orderNo} siparişinin ayırması bırakılacak; mal serbest stoka döner. Devam edilsin mi?`)) {
                              void release(reservation.id)
                            }
                          }}
                        >
                          Bırak
                        </button>
                      </td>
                    )}
                  </tr>
                ))}
                {data.salesReservations.length === 0 && (
                  <tr><td colSpan={canReserve ? 7 : 6} className="table__empty">Bu depoda müşteri siparişine ayrılmış stok yok.</td></tr>
                )}
              </tbody>
            </table>
          </div>

          {data.materialReservations.length > 0 && (
            <>
              <h3 className="section-title">İş emirlerine ayrılan (malzeme)</h3>
              <ul className="plain-list">
                {data.materialReservations.map((reservation) => (
                  <li key={reservation.productionOrderId}>
                    <RecordLink to={`/production-orders/${reservation.productionOrderId}`}>{reservation.orderNo}</RecordLink>{' '}
                    · {formatStockQuantity(reservation.quantity, data.unit)}
                  </li>
                ))}
              </ul>
            </>
          )}
          <ErrorAlert error={releaseState.error} />

          {data.canReserve && canReserve && (
            <form onSubmit={(event) => void handleSubmit(event)} noValidate>
              <h3 className="section-title">Serbest stoktan siparişe ayır</h3>
              {data.candidates.length === 0 ? (
                <p className="muted">Bu ürün / renk / bedeni isteyen, açık adedi olan onaylı sipariş yok.</p>
              ) : (
                <div className="table-wrap">
                  <table className="table table--compact">
                    <thead>
                      <tr>
                        <th aria-label="Seçim" />
                        <th>Sipariş</th>
                        <th>Müşteri</th>
                        <th>Termin</th>
                        <th className="table__number">Sipariş</th>
                        <th className="table__number">Sevk edilen</th>
                        <th className="table__number">Ayrılan</th>
                        <th className="table__number">Açık</th>
                      </tr>
                    </thead>
                    <tbody>
                      {data.candidates.map((item) => (
                        <tr key={item.salesOrderLineId} onClick={() => setLineId(item.salesOrderLineId)}>
                          <td>
                            <input
                              type="radio"
                              name="salesOrderLine"
                              aria-label={`${item.orderNo} seç`}
                              checked={lineId === item.salesOrderLineId}
                              onChange={() => setLineId(item.salesOrderLineId)}
                            />
                          </td>
                          <td>
                            {item.orderNo}
                            {item.customerOrderNo && <span className="muted"> · {item.customerOrderNo}</span>}
                          </td>
                          <td>{item.customerName}</td>
                          <td>{formatDate(item.dueDate)}</td>
                          <td className="table__number">{formatStockQuantity(item.ordered, data.unit)}</td>
                          <td className="table__number">{formatStockQuantity(item.shipped, data.unit)}</td>
                          <td className="table__number">{formatStockQuantity(item.reserved, data.unit)}</td>
                          <td className="table__number"><strong>{formatStockQuantity(item.open, data.unit)}</strong></td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}

              {candidate && (
                <label className="field">
                  <span>Ayrılacak adet</span>
                  <input
                    inputMode="numeric"
                    value={quantity}
                    placeholder={String(suggested)}
                    onChange={(event) => setQuantity(event.target.value)}
                    aria-invalid={Boolean(fieldErrors(createState.error, 'quantity')?.length)}
                  />
                  <small className="field__hint">
                    En fazla {formatStockQuantity(suggested, data.unit)} (kalemin açık adedi ve serbest stok).
                  </small>
                  {fieldErrors(createState.error, 'quantity')?.map((message) => (
                    <small key={message} className="field__error">{message}</small>
                  ))}
                </label>
              )}
              <ErrorAlert error={createState.error} fields={['quantity']} />

              <footer className="form__actions">
                <button type="button" className="button" onClick={onClose}>Kapat</button>
                <button
                  type="submit"
                  className="button button--primary"
                  disabled={!candidate || !amount || amount <= 0 || data.free <= 0 || createState.isLoading}
                >
                  {createState.isLoading ? 'Ayrılıyor…' : 'Siparişe ayır'}
                </button>
              </footer>
            </form>
          )}

          {!(data.canReserve && canReserve) && (
            <footer className="form__actions">
              <button type="button" className="button" onClick={onClose}>Kapat</button>
            </footer>
          )}
        </div>
      )}
    </Modal>
  )
}
