import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDate, formatDateTime } from '../../utils/format'
import { formatStockQuantity } from '../stock/stockPresentation'
import { GoodsReceiptDialog } from './GoodsReceiptDialog'
import { PurchaseOrderDialog } from './PurchaseOrderDialog'
import {
  useDeletePurchaseOrderMutation,
  useGetGoodsReceiptsQuery,
  useGetPurchaseOrderQuery,
  useTransitionPurchaseOrderMutation,
} from './purchasingApi'
import { formatMoney, formatUnitPrice, purchaseActions, purchaseStatusPresentation } from './purchasingPresentation'

/** Satın alma siparişi: başlık, kalemler, durum işlemleri, mal kabul ve mal kabul geçmişi. */
export function PurchaseOrderDetailPage() {
  const orderId = Number(useParams().id)
  const navigate = useNavigate()
  const canManage = usePermission(Permissions.PurchaseManage)
  const canReceive = usePermission(Permissions.StockManage)
  // Onay ayrı bir yetkidir; siparişi hazırlayan onaylayamayabilir.
  const canApprove = usePermission(Permissions.PurchaseApprove)
  const canClose = usePermission(Permissions.PurchaseClose)
  const canCancel = usePermission(Permissions.PurchaseCancel)
  // Fiyatları görmeyen kullanıcıda tutarlar boş gelir; düzenleme de fiyat görme yetkisi ister.
  const canViewPrices = usePermission(Permissions.PurchaseViewPrices)
  const canTransition = (target: string) =>
    target === 'APPROVED' ? canApprove
      : target === 'CLOSED' ? canClose
        : target === 'CANCELLED' ? canCancel
          : canManage

  const { data: order, error, isLoading } = useGetPurchaseOrderQuery(orderId, { skip: !Number.isInteger(orderId) })
  const receipts = useGetGoodsReceiptsQuery(orderId, { skip: !order })
  const [transition, transitionState] = useTransitionPurchaseOrderMutation()
  const [remove, removeState] = useDeletePurchaseOrderMutation()
  const [dialog, setDialog] = useState<'edit' | 'receive' | null>(null)

  if (isLoading) {
    return <section className="page"><p role="status">Yükleniyor…</p></section>
  }

  if (error || !order) {
    return (
      <section className="page">
        <h1>Satın alma siparişi</h1>
        <ErrorAlert error={error} />
        <p><Link to="/purchase-orders">Sipariş listesine dön</Link></p>
      </section>
    )
  }

  const status = purchaseStatusPresentation[order.status]
  const busy = transitionState.isLoading || removeState.isLoading

  const handleDelete = async () => {
    if (window.confirm('Taslak sipariş silinecek. Devam edilsin mi?')) {
      const result = await remove(order.id)
      if (!('error' in result)) {
        void navigate('/purchase-orders')
      }
    }
  }

  return (
    <section className="page">
      <header className="page__header">
        <div>
          <h1>
            {order.orderNo}{' '}
            <span className={`badge badge--${status.tone}`}><span aria-hidden="true">{status.icon}</span> {status.label}</span>
          </h1>
          <p className="muted"><Link to="/purchase-orders">Satın alma siparişleri</Link></p>
        </div>
        <div className="page__actions">
          {canReceive && order.canReceive && (
            <button type="button" className="button button--primary" disabled={busy} onClick={() => setDialog('receive')}>
              Mal kabul
            </button>
          )}
          {canManage && canViewPrices && order.isEditable && (
            <button type="button" className="button" disabled={busy} onClick={() => setDialog('edit')}>Düzenle</button>
          )}
          {order.allowedTransitions.map((target) => {
            const action = purchaseActions[target]
            return (
              action && canTransition(target) && (
                <button
                  key={target}
                  type="button"
                  className="button"
                  disabled={busy}
                  onClick={() => {
                    if (!action.confirm || window.confirm(action.confirm)) {
                      void transition({ id: order.id, action: action.action, rowVersion: order.rowVersion })
                    }
                  }}
                >
                  {action.label}
                </button>
              )
            )
          })}
          {canManage && order.isEditable && (
            <button type="button" className="button button--danger" disabled={busy} onClick={() => void handleDelete()}>
              Sil
            </button>
          )}
        </div>
      </header>

      <ErrorAlert error={transitionState.error} />
      <ErrorAlert error={removeState.error} />

      <div className="cards">
        <article className="card">
          <h2>Sipariş</h2>
          <dl className="status-list">
            <div><dt>Tedarikçi</dt><dd>{order.supplierCode} — {order.supplierName}</dd></div>
            <div><dt>Sipariş tarihi</dt><dd>{formatDate(order.orderDate)}</dd></div>
            <div><dt>Beklenen teslim</dt><dd>{formatDate(order.expectedDate)}</dd></div>
            <div><dt>Toplam tutar</dt><dd>{formatMoney(order.totalAmount, order.currency)}</dd></div>
            {order.currency !== order.costCurrency && (
              <div>
                <dt>Kur</dt>
                <dd>{!canViewPrices ? '—' : order.exchangeRate === null ? 'Girilmedi (onay için gerekli)' : `1 ${order.currency} = ${order.exchangeRate} ${order.costCurrency}`}</dd>
              </div>
            )}
            <div><dt>Oluşturan</dt><dd>{order.createdBy ?? '—'} · {formatDateTime(order.createdAt)}</dd></div>
          </dl>
          {order.notes && <p className="preserve-lines">{order.notes}</p>}
        </article>
      </div>

      <h2 className="section-title">Kalemler</h2>
      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              <th>Stok kartı</th>
              <th className="table__number">Sipariş</th>
              <th className="table__number">Teslim alınan</th>
              <th className="table__number">Kalan</th>
              <th className="table__number">Birim fiyat</th>
              <th className="table__number">Tutar</th>
            </tr>
          </thead>
          <tbody>
            {order.items.map((item) => (
              <tr key={item.id}>
                <td>
                  <code>{item.itemCode}</code> {item.itemName}
                  {item.productionOrderId && (
                    <div className="purchase-items__link">
                      <Link to={`/production-orders/${item.productionOrderId}`}>{item.productionOrderNo}</Link> eksik malzemesi
                    </div>
                  )}
                </td>
                <td className="table__number">{formatStockQuantity(item.quantity, item.unit)}</td>
                <td className="table__number">{formatStockQuantity(item.receivedQuantity, item.unit)}</td>
                <td className="table__number"><strong>{formatStockQuantity(item.remainingQuantity, item.unit)}</strong></td>
                <td className="table__number">{formatUnitPrice(item.unitPrice, order.currency)}</td>
                <td className="table__number">{formatMoney(item.lineAmount, order.currency)}</td>
              </tr>
            ))}
            {order.items.length === 0 && (
              <tr>
                <td colSpan={6} className="table__empty">Kalem yok. Onaylamak için önce kalem ekleyin.</td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      <h2 className="section-title">Mal kabuller</h2>
      <ErrorAlert error={receipts.error} />
      {receipts.data && receipts.data.length === 0 && <p className="muted">Henüz mal kabul yapılmadı.</p>}
      {receipts.data && receipts.data.length > 0 && (
        <div className="table-wrap">
          <table className="table table--compact">
            <thead>
              <tr>
                <th>Zaman</th>
                <th>Depo</th>
                <th>İrsaliye</th>
                <th>Teslim alınanlar</th>
                <th>Teslim alan</th>
                <th>Not</th>
              </tr>
            </thead>
            <tbody>
              {receipts.data.map((receipt) => (
                <tr key={receipt.id}>
                  <td>{formatDateTime(receipt.receivedAt)}</td>
                  <td>{receipt.warehouseCode} — {receipt.warehouseName}</td>
                  <td>{receipt.documentNo ?? '—'}</td>
                  <td>
                    <ul className="plain-list">
                      {receipt.lines.map((line) => (
                        <li key={line.purchaseOrderItemId}>
                          <code>{line.itemCode}</code> {formatStockQuantity(line.quantity, line.unit)}
                        </li>
                      ))}
                    </ul>
                  </td>
                  <td>{receipt.receivedBy}</td>
                  <td>{receipt.note ?? '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {dialog === 'edit' && <PurchaseOrderDialog order={order} onClose={() => setDialog(null)} />}
      {dialog === 'receive' && <GoodsReceiptDialog order={order} onClose={() => setDialog(null)} />}
    </section>
  )
}
