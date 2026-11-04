import { useEffect, useRef, useState } from 'react'
import type { FormEvent } from 'react'
import { ErrorAlert } from '../../components/FormControls'
import { useGetLookupQuery } from '../masterData/masterDataApi'
import { useGetPurchaseOrderQuery, useGetPurchaseOrdersQuery, useReceiveGoodsMutation } from '../purchasing/purchasingApi'
import type { PurchaseOrder, PurchaseOrderItem } from '../purchasing/purchasingApi'
import { formatStockQuantity, parseQuantity, quantityStep, stockUnitLabels } from '../stock/stockPresentation'

/** requestId satır seçilirken üretilir: "Teslim al" yinelenirse (ağ hatası) stok ikinci kez artmaz. */
interface Entry {
  item: PurchaseOrderItem
  requestId: string
}

function ReceiveSheet({ order, warehouseId, documentNo }: { order: PurchaseOrder; warehouseId: number; documentNo: string }) {
  const [receive, receiveState] = useReceiveGoodsMutation()
  const [entry, setEntry] = useState<Entry | null>(null)
  const [quantity, setQuantity] = useState('')
  const [batchNo, setBatchNo] = useState('')
  const [done, setDone] = useState<string | null>(null)
  const quantityInput = useRef<HTMLInputElement>(null)

  useEffect(() => {
    if (entry) {
      quantityInput.current?.focus()
      quantityInput.current?.select()
    }
  }, [entry])

  const open = (item: PurchaseOrderItem) => {
    receiveState.reset()
    setDone(null)
    setEntry({ item, requestId: crypto.randomUUID() })
    setQuantity(String(item.remainingQuantity))
    setBatchNo('')
  }

  const parsed = parseQuantity(quantity)
  const valid = parsed !== null && parsed > 0 && entry !== null && parsed <= entry.item.remainingQuantity

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    if (!entry || !valid) {
      return
    }

    const { item } = entry
    const result = await receive({
      id: order.id,
      body: {
        requestId: entry.requestId,
        warehouseId,
        documentNo: documentNo.trim() || null,
        note: null,
        lines: [{
          purchaseOrderItemId: item.id,
          quantity: parsed,
          // Takipli kartta gelen miktar tek top olarak açılır; birden fazla top için satır tekrar teslim alınır.
          lots: item.isLotTracked
            ? [{ quantity: parsed, batchNo: batchNo.trim() || null, shade: null, width: null, supplierRollNo: null }]
            : undefined,
        }],
      },
    })
    if (!('error' in result)) {
      setDone(`${item.itemCode}: ${formatStockQuantity(parsed, item.unit)} teslim alındı (deponun genel adresine).`)
      setEntry(null)
    }
  }

  const waiting = order.items.filter((item) => item.remainingQuantity > 0)

  return (
    <>
      {done && <p className="alert alert--ok" role="status">{done}</p>}
      <ErrorAlert error={receiveState.error} />

      {entry ? (
        <form className="terminal-card" onSubmit={(event) => void handleSubmit(event)} noValidate>
          <h2>Teslim alınan miktar</h2>
          <p>
            <code>{entry.item.itemCode}</code> {entry.item.itemName}
            <br />
            <span className="muted">Kalan {formatStockQuantity(entry.item.remainingQuantity, entry.item.unit)}</span>
          </p>
          <label className="field">
            <span>Miktar ({stockUnitLabels[entry.item.unit]})</span>
            <input
              ref={quantityInput}
              type="number"
              inputMode="decimal"
              min={0}
              max={entry.item.remainingQuantity}
              step={quantityStep(entry.item.unit)}
              value={quantity}
              aria-invalid={!valid}
              onChange={(event) => setQuantity(event.target.value)}
            />
          </label>
          {entry.item.isLotTracked && (
            <label className="field">
              <span>Parti no (isteğe bağlı)</span>
              <input type="text" maxLength={50} autoComplete="off" value={batchNo} onChange={(event) => setBatchNo(event.target.value)} />
              <small className="field__hint">Girilen miktar tek top olarak açılır. Her top için satırı ayrı ayrı teslim alın.</small>
            </label>
          )}
          <footer className="terminal-actions">
            <button type="button" className="button" onClick={() => setEntry(null)}>Vazgeç</button>
            <button type="submit" className="button button--primary" disabled={!valid || receiveState.isLoading}>
              {receiveState.isLoading ? 'Kaydediliyor…' : 'Teslim al'}
            </button>
          </footer>
        </form>
      ) : (
        <section className="terminal-card">
          <h2>Bekleyen kalemler</h2>
          {waiting.length === 0 ? (
            <p className="muted">Bu siparişin bütün kalemleri teslim alınmış.</p>
          ) : (
            <ul className="terminal-list">
              {waiting.map((item) => (
                <li key={item.id}>
                  <button type="button" className="terminal-list__row terminal-list__row--pick" onClick={() => open(item)}>
                    <span className="terminal-list__main">
                      <code>{item.itemCode}</code> {item.itemName}
                      {item.receivedQuantity > 0 && (
                        <span className="muted"><br />Alınan {formatStockQuantity(item.receivedQuantity, item.unit)}</span>
                      )}
                    </span>
                    <strong>{formatStockQuantity(item.remainingQuantity, item.unit)}</strong>
                  </button>
                </li>
              ))}
            </ul>
          )}
        </section>
      )}
    </>
  )
}

/**
 * Mal kabul: mal kabul bekleyen bir satın alma siparişi seçilir, gelen kalemler tek tek teslim alınır. Stok
 * deponun genel adresine girer; rafa yerleştirme "Adres değiştir" ile yapılır.
 */
export function TerminalReceive() {
  const orders = useGetPurchaseOrdersQuery({ page: 1, pageSize: 50, receivable: true })
  const warehouses = useGetLookupQuery({ resource: 'warehouses', take: 100 })
  const [orderId, setOrderId] = useState<number | null>(null)
  const [warehouseId, setWarehouseId] = useState<number | null>(null)
  const [documentNo, setDocumentNo] = useState('')
  const order = useGetPurchaseOrderQuery(orderId ?? 0, { skip: orderId === null })

  if (orders.isLoading) {
    return <p role="status">Yükleniyor…</p>
  }

  if (orderId === null) {
    return (
      <>
        <ErrorAlert error={orders.error} />
        {orders.data?.items.length === 0 ? (
          <p className="alert alert--info">Mal kabul bekleyen (onaylı) satın alma siparişi yok.</p>
        ) : (
          <section className="terminal-card">
            <h2>Hangi sipariş?</h2>
            <ul className="terminal-list">
              {orders.data?.items.map((item) => (
                <li key={item.id}>
                  <button type="button" className="terminal-list__row terminal-list__row--pick" onClick={() => setOrderId(item.id)}>
                    <span className="terminal-list__main">
                      <code>{item.orderNo}</code>
                      <br />
                      {item.supplierName}
                    </span>
                    <span className="muted">{item.itemCount} kalem</span>
                  </button>
                </li>
              ))}
            </ul>
          </section>
        )}
      </>
    )
  }

  return (
    <>
      {order.isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={order.error ?? warehouses.error} />
      {order.data && (
        <>
          <dl className="terminal-facts">
            <dt>İş emri</dt>
            <dd><code>{order.data.orderNo}</code> · {order.data.supplierName}</dd>
          </dl>
          <label className="field">
            <span>Teslim alınan depo</span>
            <select value={warehouseId ?? ''} onChange={(event) => setWarehouseId(event.target.value ? Number(event.target.value) : null)}>
              <option value="">Seçin…</option>
              {warehouses.data?.map((warehouse) => (
                <option key={warehouse.id} value={warehouse.id}>{warehouse.code} — {warehouse.name}</option>
              ))}
            </select>
          </label>
          <label className="field">
            <span>İrsaliye no (isteğe bağlı)</span>
            <input type="text" maxLength={50} autoComplete="off" value={documentNo} onChange={(event) => setDocumentNo(event.target.value)} />
          </label>

          {!order.data.canReceive ? (
            <p className="alert alert--info">Bu siparişe artık mal kabul yapılamaz (tamamı teslim alınmış ya da kapatılmış).</p>
          ) : warehouseId === null ? (
            <p className="muted">Kalemleri teslim almak için depoyu seçin.</p>
          ) : (
            <ReceiveSheet key={order.data.id} order={order.data} warehouseId={warehouseId} documentNo={documentNo} />
          )}
        </>
      )}
      <p><button type="button" className="button button--ghost" onClick={() => setOrderId(null)}>Başka sipariş</button></p>
    </>
  )
}
