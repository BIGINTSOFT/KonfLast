import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDate, formatDateTime, formatQuantity } from '../../utils/format'
import { OrderBundlesSection } from '../bundles/OrderBundlesSection'
import { OrderCostSection, OrderMaterialsSection } from '../costing/OrderCostingSections'
import { OrderCuttingSection } from '../cutting/OrderCuttingSection'
import { useGetLookupQuery } from '../masterData/masterDataApi'
import { OrderProgressSection } from '../productionTracking/OrderProgressSection'
import { OrderStatusBadge } from './OrderStatusBadge'
import { buildQuantityMatrix, transitionActions } from './orderPresentation'
import {
  operationSequenceHints,
  operationSequenceLabels,
  useChangeOperationSequenceMutation,
  useDeleteProductionOrderMutation,
  useGetProductionOrderQuery,
  useTransitionProductionOrderMutation,
} from './productionOrdersApi'
import type { OperationSequenceMode, ProductionOrder, ProductionOrderStatus } from './productionOrdersApi'
import { QuantityMatrixTable } from './QuantityMatrixTable'

/**
 * Operasyon sırası. Onaylı siparişte ilk demet açılana kadar buradan değiştirilebilir (taslakta formdan);
 * demet açıldıktan sonra kilitlidir, açık demetler açıldıkları andaki sırayla yürür.
 */
function OperationSequenceValue({ order, canUpdate }: { order: ProductionOrder; canUpdate: boolean }) {
  const [change, { isLoading, error }] = useChangeOperationSequenceMutation()
  const editable = canUpdate && order.canChangeOperationSequence && !order.isEditable

  return (
    <>
      {editable ? (
        <select
          className="inline-select"
          aria-label="Operasyon sırası"
          value={order.operationSequence}
          disabled={isLoading}
          onChange={(event) => void change({
            id: order.id,
            operationSequence: event.target.value as OperationSequenceMode,
            rowVersion: order.rowVersion,
          })}
        >
          <option value="SEQUENTIAL">{operationSequenceLabels.SEQUENTIAL}</option>
          <option value="FREE">{operationSequenceLabels.FREE}</option>
        </select>
      ) : (
        <span title={operationSequenceHints[order.operationSequence]}>{operationSequenceLabels[order.operationSequence]}</span>
      )}
      <ErrorAlert error={error} />
    </>
  )
}

type Pending = { kind: 'transition'; target: ProductionOrderStatus; message: string } | { kind: 'delete' }

/** Siparişin malzeme deposunun adı; seçilmemişse tek hammadde deposunun kullanılacağı belirtilir. */
function WarehouseName({ id }: { id: number | null }) {
  const warehouses = useGetLookupQuery({ resource: 'warehouses', take: 100, includeId: id ?? undefined }, { skip: id === null })
  if (id === null) {
    return <span className="muted">Seçilmedi</span>
  }

  const warehouse = warehouses.data?.find((item) => item.id === id)
  return <>{warehouse ? `${warehouse.code} — ${warehouse.name}` : '…'}</>
}

function QuantityTable({ order }: { order: ProductionOrder }) {
  const matrix = buildQuantityMatrix(order.items)

  if (matrix.total === 0) {
    return <p className="muted">Bu siparişe henüz kalem girilmemiş.</p>
  }

  return <QuantityMatrixTable matrix={matrix} />
}

export function ProductionOrderDetailPage() {
  const id = Number(useParams().id)
  const navigate = useNavigate()
  const canUpdate = usePermission(Permissions.ProductionOrderUpdate)
  // Onay (üretime açma) ayrı bir yetkidir; siparişi hazırlayan onaylayamayabilir.
  const canApprove = usePermission(Permissions.ProductionOrderApprove)
  // İptal ve tamamlama da ayrı yetkilerdir; taslağa alma ve silme düzenleme yetkisiyle yapılır.
  const canCancel = usePermission(Permissions.ProductionOrderCancel)
  const canComplete = usePermission(Permissions.ProductionOrderComplete)
  const canTransition = (target: ProductionOrderStatus) =>
    target === 'RELEASED' ? canApprove
      : target === 'CANCELLED' ? canCancel
        : target === 'COMPLETED' ? canComplete
          : canUpdate

  const { data: order, isLoading, error } = useGetProductionOrderQuery(id)
  const [transition, transitionState] = useTransitionProductionOrderMutation()
  const [deleteOrder, deleteState] = useDeleteProductionOrderMutation()
  const [pending, setPending] = useState<Pending | null>(null)

  if (isLoading) {
    return <section className="page"><p role="status">Yükleniyor…</p></section>
  }

  if (!order) {
    return (
      <section className="page">
        <ErrorAlert error={error} />
        <Link to="/production-orders">← Siparişlere dön</Link>
      </section>
    )
  }

  const runTransition = async (target: ProductionOrderStatus) => {
    setPending(null)
    await transition({ id: order.id, target, rowVersion: order.rowVersion })
  }

  const runDelete = async () => {
    const result = await deleteOrder(order.id)
    if (!('error' in result)) {
      void navigate('/production-orders')
    } else {
      setPending(null)
    }
  }

  const busy = transitionState.isLoading || deleteState.isLoading

  return (
    <section className="page">
      <p><Link to="/production-orders">← Siparişlere dön</Link></p>

      <header className="page__header">
        <div>
          <h1>
            {order.orderNo} <OrderStatusBadge status={order.status} />
          </h1>
          <p className="muted">
            {order.createdBy ?? 'sistem'} tarafından {formatDateTime(order.createdAt)} tarihinde oluşturuldu.
          </p>
        </div>

        {(canUpdate || canApprove || canCancel || canComplete) && (
          <div className="page__actions">
            {canUpdate && order.isEditable && (
              <Link to={`/production-orders/${order.id}/edit`} className="button">Düzenle</Link>
            )}
            {order.allowedTransitions.map((target) => {
              const action = transitionActions[target]
              if (!action || !canTransition(target)) {
                return null
              }
              return (
                <button
                  key={target}
                  type="button"
                  className={target === 'RELEASED' ? 'button button--primary' : 'button'}
                  disabled={busy}
                  onClick={() =>
                    action.confirm
                      ? setPending({ kind: 'transition', target, message: action.confirm })
                      : void runTransition(target)
                  }
                >
                  {action.label}
                </button>
              )
            })}
            {canUpdate && order.isEditable && (
              <button type="button" className="button" disabled={busy} onClick={() => setPending({ kind: 'delete' })}>
                Sil
              </button>
            )}
          </div>
        )}
      </header>

      <ErrorAlert error={transitionState.error ?? deleteState.error} />

      <div className="cards">
        <article className="card">
          <h2>Sipariş</h2>
          <dl className="status-list">
            <div><dt>Müşteri</dt><dd>{order.customerName}</dd></div>
            <div>
              <dt>Müşteri siparişi</dt>
              <dd>
                {order.salesOrderId !== null
                  ? <Link to={`/sales-orders/${order.salesOrderId}`}><code>{order.salesOrderNo}</code></Link>
                  : '—'}
              </dd>
            </div>
            <div><dt>Müşteri sipariş no</dt><dd>{order.customerOrderNo ?? '—'}</dd></div>
            <div><dt>Sipariş tarihi</dt><dd>{formatDate(order.orderDate)}</dd></div>
            <div><dt>Termin</dt><dd>{formatDate(order.dueDate)}</dd></div>
            <div><dt>Malzeme deposu</dt><dd><WarehouseName id={order.materialWarehouseId} /></dd></div>
            <div>
              <dt>Operasyon sırası</dt>
              <dd><OperationSequenceValue order={order} canUpdate={canUpdate} /></dd>
            </div>
          </dl>
        </article>
        <article className="card">
          <h2>Ürün</h2>
          <dl className="status-list">
            <div><dt>Ürün</dt><dd>{order.productCode}</dd></div>
            <div><dt>Ad</dt><dd>{order.productName}</dd></div>
            <div><dt>Model</dt><dd>{order.modelCode}</dd></div>
            <div><dt>Toplam adet</dt><dd>{formatQuantity(order.totalQuantity)}</dd></div>
          </dl>
        </article>
      </div>

      {order.notes && (
        <article className="card card--spaced">
          <h2>Not</h2>
          <p className="preserve-lines">{order.notes}</p>
        </article>
      )}

      <h2 className="section-title">Renk – beden dağılımı</h2>
      <QuantityTable order={order} />

      <OrderCuttingSection orderId={order.id} />

      <OrderBundlesSection orderId={order.id} />

      <OrderProgressSection orderId={order.id} />

      <OrderMaterialsSection orderId={order.id} orderNo={order.orderNo} />

      <OrderCostSection orderId={order.id} />

      {pending && (
        <Modal title={pending.kind === 'delete' ? 'Siparişi sil' : 'Emin misiniz?'} onClose={() => setPending(null)}>
          <div className="form">
            <p>
              {pending.kind === 'delete'
                ? `${order.orderNo} silinecek. Sipariş numarası yeniden kullanılmaz.`
                : pending.message}
            </p>
            <footer className="form__actions">
              <button type="button" className="button" onClick={() => setPending(null)}>Vazgeç</button>
              <button
                type="button"
                className="button button--danger"
                disabled={busy}
                onClick={() => void (pending.kind === 'delete' ? runDelete() : runTransition(pending.target))}
              >
                {pending.kind === 'delete' ? 'Sil' : 'Evet, devam et'}
              </button>
            </footer>
          </div>
        </Modal>
      )}
    </section>
  )
}
