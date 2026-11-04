import { Copy, FileText, Info } from 'lucide-react'
import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { RecordLink } from '../../components/RecordLink'
import { Modal } from '../../components/Modal'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDate, formatDateTime, formatQuantity } from '../../utils/format'
import { OrderStatusBadge } from '../productionOrders/OrderStatusBadge'
import { buildQuantityMatrix } from '../productionOrders/orderPresentation'
import { formatMoney } from '../purchasing/purchasingPresentation'
import { groupByProduct, shipmentState } from './salesOrderPresentation'
import type { ProductSection } from './salesOrderPresentation'
import { useActOnSalesOrderMutation, useDeleteSalesOrderMutation, useGetSalesOrderQuery } from './salesOrdersApi'
import type { SalesOrderAction } from './salesOrdersApi'
import { ApprovalPanel } from './ApprovalPanel'
import { CoverageSection, ProductionChangesDialog } from './CoverageSection'
import { SalesOrderStatusBadge } from './SalesOrdersPage'
import { QuantityMatrixTable } from '../productionOrders/QuantityMatrixTable'

/** Onay isteyen işlem: açıklaması ve (onay / rette) not kutusu. */
interface Pending {
  action: SalesOrderAction | 'delete'
  title: string
  message: string
  confirmLabel: string
  withNote?: boolean
  danger?: boolean
}

const dialogs: Record<'submit' | 'approve' | 'reject' | 'revise' | 'cancel' | 'close' | 'delete', Pending> = {
  submit: {
    action: 'submit', title: 'Onaya gönder', confirmLabel: 'Onaya gönder', withNote: true,
    message: 'Sipariş onay akışına girecek; içerik kilitlenir. Onaylayacak kişinin görmesini istediğiniz bir not yazabilirsiniz.',
  },
  approve: {
    action: 'approve', title: 'Siparişi onayla', confirmLabel: 'Onayla', withNote: true,
    message: 'Bu onay adımı tamamlanacak; başka adım varsa sipariş sıradaki onaylayıcıya geçer. Notunuz sonraki onaylayıcılara görünür.',
  },
  reject: {
    action: 'reject', title: 'Siparişi geri çevir', confirmLabel: 'Geri çevir', withNote: true, danger: true,
    message: 'Sipariş düzeltilmek üzere taslağa dönecek. Neyin düzeltilmesi gerektiğini not olarak yazın.',
  },
  revise: {
    action: 'revise', title: 'Siparişi revizyona al', confirmLabel: 'Revizyona al', withNote: true,
    message: 'Onaylı sipariş taslağa dönecek; düzeltip yeniden onaya gönderirsiniz. İş emri ya da sevkiyatı olan sipariş revize edilemez.',
  },
  cancel: {
    action: 'cancel', title: 'Siparişi iptal et', confirmLabel: 'İptal et', danger: true,
    message: 'Sipariş iptal edilecek. Bu işlem geri alınamaz.',
  },
  close: {
    action: 'close', title: 'Siparişi elle kapat', confirmLabel: 'Kapat', withNote: true,
    message: 'Sipariş, kalan adet sevk edilmeden kapatılacak. Tamamı sevk edilen sipariş zaten kendiliğinden kapanır; bu işlemi yalnızca kalan adet gönderilmeyecekse kullanın. Geri alınamaz.',
  },
  delete: {
    action: 'delete', title: 'Siparişi sil', confirmLabel: 'Sil', danger: true,
    message: 'Taslak sipariş silinecek. Sipariş numarası yeniden kullanılmaz.',
  },
}

function ProductTable({ product, currency }: { product: ProductSection; currency: string }) {
  const matrix = buildQuantityMatrix(product.lines)
  return (
    <>
      <header className="section-title order-product">
        <h3>
          <code>{product.productCode}</code> {product.productName}
          {product.lines[0]?.customerArticleCode && <span className="muted"> · Artikel {product.lines[0].customerArticleCode}</span>}
        </h3>
        <dl className="order-figures">
          <div><dt>Adet</dt><dd>{formatQuantity(product.quantity)}</dd></div>
          <div><dt>Sevk edilen</dt><dd>{formatQuantity(product.shippedQuantity)}</dd></div>
          <div><dt>Birim fiyat</dt><dd>{formatMoney(product.unitPrice, currency)}</dd></div>
          <div><dt>Tutar</dt><dd>{formatMoney(product.amount, currency)}</dd></div>
        </dl>
      </header>
      <QuantityMatrixTable matrix={matrix} />
    </>
  )
}

/** Müşteri siparişi: başlık, onay bilgisi, ürünler, açılmış iş emirleri ve durum işlemleri. */
export function SalesOrderDetailPage() {
  const id = Number(useParams().id)
  const navigate = useNavigate()
  const canManage = usePermission(Permissions.SalesOrderManage)
  const canApprove = usePermission(Permissions.SalesOrderApprove)
  const canClose = usePermission(Permissions.SalesOrderClose)
  const canCancel = usePermission(Permissions.SalesOrderCancel)
  // Sipariş girişi ve düzenleme fiyatları görmeyi de gerektirir; ayırma stok ayırma yetkisiyle yapılır.
  const canViewPrices = usePermission(Permissions.SalesOrderViewPrices)
  const canEditOrder = canManage && canViewPrices
  const canReserve = usePermission(Permissions.StockReserve)

  const { data: order, isLoading, error } = useGetSalesOrderQuery(id)
  const [act, actState] = useActOnSalesOrderMutation()
  const [deleteOrder, deleteState] = useDeleteSalesOrderMutation()
  const [pending, setPending] = useState<Pending | null>(null)
  const [note, setNote] = useState('')
  const [differenceFor, setDifferenceFor] = useState<number | null>(null)

  if (isLoading) {
    return <section className="page"><p role="status">Yükleniyor…</p></section>
  }

  if (!order) {
    return (
      <section className="page">
        <ErrorAlert error={error} />
        <Link to="/sales-orders">← Müşteri siparişlerine dön</Link>
      </section>
    )
  }

  const busy = actState.isLoading || deleteState.isLoading
  const can = (target: string) => order.allowedTransitions.includes(target as never)
  const open = (dialog: Pending) => {
    setNote('')
    setPending(dialog)
  }

  const run = async (action: SalesOrderAction) => {
    setPending(null)
    await act({ id: order.id, action, rowVersion: order.rowVersion, note: note.trim() || null })
  }

  const confirm = async () => {
    if (!pending) {
      return
    }

    if (pending.action !== 'delete') {
      await run(pending.action)
      return
    }

    const result = await deleteOrder(order.id)
    if (!('error' in result)) {
      void navigate('/sales-orders')
    } else {
      setPending(null)
    }
  }

  const products = groupByProduct(order.lines)
  const pendingStep = order.approvalSteps?.find((step) => step.status === 'PENDING')
  const shipping = shipmentState(order.totalQuantity, order.shippedQuantity)

  return (
    <section className="page">
      <p><Link to="/sales-orders">← Müşteri siparişlerine dön</Link></p>

      <header className="page__header">
        <div>
          <h1>
            {order.orderNo} <SalesOrderStatusBadge status={order.status} />
            {shipping && <span className={`badge badge--${shipping.tone}`}>{shipping.label}</span>}
          </h1>
          <p className="muted">
            {order.createdBy ?? 'sistem'} tarafından {formatDateTime(order.createdAt)} tarihinde oluşturuldu.
          </p>
        </div>

        <div className="page__actions">
          {canEditOrder && order.isEditable && <Link to={`/sales-orders/${order.id}/edit`} className="button">Düzenle</Link>}
          <Link to={`/sales-orders/${order.id}/proforma`} className="button"><FileText aria-hidden="true" />Proforma</Link>
          {canEditOrder && (
            <Link to={`/sales-orders/new?copy=${order.id}`} className="button" title="Bu siparişin kalemleriyle yeni taslak sipariş"><Copy aria-hidden="true" />Kopyala</Link>
          )}
          {canManage && order.status === 'DRAFT' && can('SUBMITTED') && (
            <button type="button" className="button button--primary" disabled={busy} onClick={() => open(dialogs.submit)}>
              Onaya gönder
            </button>
          )}
          {order.canDecide && (
            <>
              <button type="button" className="button button--primary" disabled={busy} onClick={() => open(dialogs.approve)}>
                Onayla
              </button>
              <button type="button" className="button" disabled={busy} onClick={() => open(dialogs.reject)}>Geri çevir</button>
            </>
          )}
          {canApprove && order.status === 'APPROVED' && can('DRAFT') && (
            <button type="button" className="button" disabled={busy} onClick={() => open(dialogs.revise)}>Revizyona al</button>
          )}
          {canClose && can('CLOSED') && (
            <button type="button" className="button" disabled={busy} onClick={() => open(dialogs.close)}>Elle kapat</button>
          )}
          {canCancel && can('CANCELLED') && (
            <button type="button" className="button" disabled={busy} onClick={() => open(dialogs.cancel)}>İptal et</button>
          )}
          {canManage && order.isEditable && (
            <button type="button" className="button" disabled={busy} onClick={() => open(dialogs.delete)}>Sil</button>
          )}
        </div>
      </header>

      <ErrorAlert error={actState.error ?? deleteState.error} />
      {order.status === 'CLOSED' && order.closedAt && (
        <p className="alert alert--info" role="status">
          {order.closedAutomatically
            ? `Sipariş tamamı sevk edildiği için ${formatDateTime(order.closedAt)} tarihinde kendiliğinden kapandı.`
            : `Sipariş ${order.closedBy ?? ''} tarafından ${formatDateTime(order.closedAt)} tarihinde elle kapatıldı.`}
          {order.closeNote && !order.closedAutomatically && ` Not: ${order.closeNote}`}
        </p>
      )}
      {order.status === 'SUBMITTED' && !order.canDecide && (
        <p className="alert alert--info">
          {pendingStep
            ? `Sipariş "${pendingStep.name}" adımında ${pendingStep.approvers} onayını bekliyor.`
            : 'Sipariş onay bekliyor. Onay, müşteri siparişi onaylama yetkisi olan bir kullanıcı tarafından verilir.'}
        </p>
      )}
      {order.status === 'DRAFT' && order.decisionNote && (
        <p className="alert alert--error" role="status">
          {order.decidedBy} siparişi geri çevirdi: {order.decisionNote}
        </p>
      )}

      <div className="order-summary">
        <article className="card">
          <h2>Sipariş</h2>
          <dl className="status-list">
            <div><dt>Müşteri</dt><dd>{order.customerName}</dd></div>
            {order.customerBuyerName && <div><dt>Buyer</dt><dd>{order.customerBuyerName}</dd></div>}
            <div><dt>Teslim adresi</dt><dd>{order.deliveryAddress ?? '—'}</dd></div>
            <div><dt>Müşteri sipariş no</dt><dd>{order.customerOrderNo ?? '—'}</dd></div>
            <div><dt>Sipariş tarihi</dt><dd>{formatDate(order.orderDate)}</dd></div>
            <div><dt>Teslim</dt><dd>{formatDate(order.dueDate)}</dd></div>
            <div><dt>Toplam adet</dt><dd>{formatQuantity(order.totalQuantity)}</dd></div>
            <div><dt>Sevk edilen</dt><dd>{formatQuantity(order.shippedQuantity)}</dd></div>
            <div><dt>Tutar</dt><dd>{formatMoney(order.totalAmount, order.currency)}</dd></div>
          </dl>
        </article>
        <ApprovalPanel order={order} />
      </div>

      {order.notes && (
        <article className="card card--spaced">
          <h2>Not</h2>
          <p className="preserve-lines">{order.notes}</p>
        </article>
      )}

      <h2 className="section-title">Ürünler</h2>
      {products.length === 0 && <p className="muted">Bu siparişe henüz kalem girilmemiş.</p>}
      {products.map((product) => (
        <ProductTable key={product.productId} product={product} currency={order.currency} />
      ))}

      {(order.status === 'APPROVED' || order.status === 'CLOSED') && (
        <CoverageSection salesOrderId={order.id} canManage={canReserve} />
      )}

      <h2 className="section-title">İş emirleri</h2>
      {order.productionOrders.length === 0 ? (
        <p className="muted">
          {order.status === 'APPROVED'
            ? 'Henüz iş emri açılmadı. Stoktan karşılanmayan adet Üretim Planı ekranında planlamaya düştü; iş emrini planlama açar.'
            : 'Sipariş onaylanınca stoktan karşılanmayan adet Üretim Planı ekranında planlamaya düşer.'}
        </p>
      ) : (
        <div className="table-wrap">
          <table className="table table--compact">
            <thead>
              <tr>
                <th>İş emri</th>
                <th>Ürün</th>
                <th className="table__number">Sipariş adedi</th>
                <th className="table__number">Üretim adedi</th>
                <th>Durum</th>
              </tr>
            </thead>
            <tbody>
              {order.productionOrders.map((production) => (
                <tr key={production.id}>
                  <td><RecordLink to={`/production-orders/${production.id}`}><code>{production.orderNo}</code></RecordLink></td>
                  <td><code>{production.productCode}</code></td>
                  <td className="table__number">{formatQuantity(production.orderedQuantity)}</td>
                  <td className="table__number">
                    {formatQuantity(production.totalQuantity)}
                    {production.status !== 'CANCELLED' && production.totalQuantity !== production.orderedQuantity && (
                      <div className="coverage__diff-note">
                        <span className="field__error">sipariş adedinden farklı</span>
                        <button
                          type="button"
                          className="icon-button"
                          title="Hangi renk / bedende ne kadar fark var, kim değiştirdi"
                          aria-label={`${production.orderNo} farklarını göster`}
                          onClick={() => setDifferenceFor(production.id)}
                        >
                          <Info aria-hidden="true" />
                        </button>
                      </div>
                    )}
                  </td>
                  <td><OrderStatusBadge status={production.status} /></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {differenceFor !== null && (
        <ProductionChangesDialog salesOrderId={order.id} productionOrderId={differenceFor} onClose={() => setDifferenceFor(null)} />
      )}

      {pending && (
        <Modal title={pending.title} onClose={() => setPending(null)}>
          <div className="form">
            <p>{pending.message}</p>
            {pending.action === 'close' && order.shippedQuantity < order.totalQuantity && (
              <p className="alert alert--error" role="alert">
                Sipariş {formatQuantity(order.totalQuantity)} adet, sevk edilen {formatQuantity(order.shippedQuantity)} adet.
                Kalan {formatQuantity(order.totalQuantity - order.shippedQuantity)} adet sevk edilmeden kapatılacak.
              </p>
            )}
            {pending.withNote && (
              <label className="field">
                <span>Not (isteğe bağlı)</span>
                <textarea rows={3} maxLength={500} value={note} onChange={(event) => setNote(event.target.value)} />
              </label>
            )}
            <footer className="form__actions">
              <button type="button" className="button" onClick={() => setPending(null)}>Vazgeç</button>
              <button
                type="button"
                className={pending.danger ? 'button button--danger' : 'button button--primary'}
                disabled={busy}
                onClick={() => void confirm()}
              >
                {pending.confirmLabel}
              </button>
            </footer>
          </div>
        </Modal>
      )}
    </section>
  )
}
