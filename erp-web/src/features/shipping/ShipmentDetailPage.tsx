import { ClipboardCheck, Printer } from 'lucide-react'
import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { RecordLink } from '../../components/RecordLink'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDate, formatDateTime } from '../../utils/format'
import { formatStockQuantity } from '../stock/stockPresentation'
import { ShipmentCartonsSection } from './ShipmentCartonsSection'
import { ShipmentDialog } from './ShipmentDialog'
import {
  useCancelShipmentMutation,
  useDeleteShipmentMutation,
  useGetShipmentQuery,
  useShipmentTransitionMutation,
} from './shippingApi'
import { shipmentStatusPresentation } from './shippingPresentation'

/**
 * Sevkiyat: başlık, kalemler ve akış işlemleri. Taslak düzenlenir ve depoya toplamaya verilir; toplanan sevkiyatın
 * irsaliyesi, son adet kontrolünden sonra Sevkiyat menüsündeki "İrsaliye Bekleyenler"den kesilir.
 */
export function ShipmentDetailPage() {
  const shipmentId = Number(useParams().id)
  const navigate = useNavigate()
  const canManage = usePermission(Permissions.ShipmentManage)
  // İrsaliye kesmek (stoktan düşmek) ayrı yetkidir; iptal ve düzenleme sevkiyat yönetimiyle yapılır.
  const canShip = usePermission(Permissions.ShipmentShip)

  const { data: shipment, error, isLoading } = useGetShipmentQuery(shipmentId, { skip: !Number.isInteger(shipmentId) })
  const [transition, transitionState] = useShipmentTransitionMutation()
  const [cancel, cancelState] = useCancelShipmentMutation()
  const [remove, removeState] = useDeleteShipmentMutation()
  const [editing, setEditing] = useState(false)

  if (isLoading) {
    return <section className="page"><p role="status">Yükleniyor…</p></section>
  }

  if (error || !shipment) {
    return (
      <section className="page">
        <h1>Sevkiyat</h1>
        <ErrorAlert error={error} />
        <p><Link to="/shipments">Sevkiyat listesine dön</Link></p>
      </section>
    )
  }

  const status = shipmentStatusPresentation[shipment.status]
  const busy = transitionState.isLoading || cancelState.isLoading || removeState.isLoading
  const version = { id: shipment.id, rowVersion: shipment.rowVersion }
  const workflow = shipment.workflow
  const picking = shipment.status === 'PICKING' || shipment.status === 'PICKED'
  const showPicked = picking || shipment.status === 'SHIPPED'

  const cartons = shipment.cartons
  const unloaded = cartons ? cartons.cartonCount - cartons.loadedCount : 0

  const run = (action: 'send-to-picking' | 'return-to-draft' | 'reopen-picking', question: string) => {
    if (window.confirm(question)) {
      void transition({ ...version, action })
    }
  }

  const handleDelete = async () => {
    if (window.confirm('Taslak sevkiyat silinecek. Devam edilsin mi?')) {
      const result = await remove(shipment.id)
      if (!('error' in result)) {
        void navigate('/shipments')
      }
    }
  }

  return (
    <section className="page">
      <header className="page__header">
        <div>
          <h1>
            {shipment.shipmentNo}{' '}
            <span className={`badge badge--${status.tone}`}><span aria-hidden="true">{status.icon}</span> {status.label}</span>
          </h1>
          <p className="muted"><Link to="/shipments">Sevkiyatlar</Link></p>
        </div>
        <div className="page__actions">
          {shipment.status !== 'CANCELLED' && (
            <>
              {shipment.status !== 'DRAFT' && (
                <Link className="button" to={`/shipments/${shipment.id}/pick-list`}><Printer aria-hidden="true" />Toplama listesi</Link>
              )}
              <Link className="button" to={`/shipments/${shipment.id}/e-despatch`}><Printer aria-hidden="true" />e-İrsaliye</Link>
              <Link className="button" to={`/shipments/${shipment.id}/despatch-note`}><Printer aria-hidden="true" />Sevk belgesi</Link>
              {cartons !== null && cartons.cartonCount > 0 && (
                <>
                  <Link className="button" to={`/shipments/${shipment.id}/packing-list`}><Printer aria-hidden="true" />Çeki listesi</Link>
                  <Link className="button" to={`/shipments/${shipment.id}/carton-labels`}><Printer aria-hidden="true" />Koli etiketleri</Link>
                </>
              )}
            </>
          )}
          {canShip && shipment.canShip && (
            <Link className="button button--primary" to={`/despatch-queue/${shipment.id}`}>
              <ClipboardCheck aria-hidden="true" />Kontrol ve irsaliye
            </Link>
          )}
          {canManage && (
            <>
              {workflow?.canSendToPicking && (
                <button
                  type="button"
                  className="button button--primary"
                  disabled={busy}
                  onClick={() => run('send-to-picking', 'Sevkiyat depoya toplamaya verilecek; kalemler kilitlenir. Devam edilsin mi?')}
                >
                  Toplamaya gönder
                </button>
              )}
              {workflow?.canReturnToDraft && (
                <button
                  type="button"
                  className="button"
                  disabled={busy}
                  onClick={() => run('return-to-draft', 'Toplama geri alınacak ve sevkiyat yeniden düzenlenebilir olacak. Devam edilsin mi?')}
                >
                  Taslağa döndür
                </button>
              )}
              {workflow?.canReopenPicking && (
                <button
                  type="button"
                  className="button"
                  disabled={busy}
                  onClick={() => run('reopen-picking', 'Sevkiyat yeniden toplamaya açılacak; eksik biten kalemler ilk istenen adede döner. Devam edilsin mi?')}
                >
                  Yeniden toplamaya aç
                </button>
              )}
              {shipment.isEditable && (
                <button type="button" className="button" disabled={busy} onClick={() => setEditing(true)}>Düzenle</button>
              )}
              {shipment.canCancel && (
                <button
                  type="button"
                  className="button"
                  disabled={busy}
                  onClick={() => {
                    if (window.confirm('Sevkiyat iptal edilecek. Devam edilsin mi?')) {
                      void cancel(version)
                    }
                  }}
                >
                  İptal et
                </button>
              )}
              {shipment.isEditable && (
                <button type="button" className="button button--danger" disabled={busy} onClick={() => void handleDelete()}>
                  Sil
                </button>
              )}
            </>
          )}
        </div>
      </header>

      <ErrorAlert error={transitionState.error} />
      <ErrorAlert error={cancelState.error} />
      <ErrorAlert error={removeState.error} />
      {cartons !== null && cartons.cartonCount > 0 && !cartons.matchesItems && (shipment.isEditable || shipment.status === 'PICKED') && (
        <p className="alert alert--error" role="alert">
          Koliler sevkiyat kalemleriyle tutmuyor (kalemler koli dağıtımından ya da eksik toplamadan sonra değişmiş olabilir).
          İrsaliyeden önce aşağıdaki kolileri düzeltin ya da yeniden dağıtın.
        </p>
      )}

      <div className="cards">
        <article className="card">
          <h2>Sevkiyat</h2>
          <dl className="status-list">
            <div><dt>Müşteri</dt><dd>{shipment.customerCode} — {shipment.customerName}</dd></div>
            <div>
              <dt>Müşteri siparişi</dt>
              <dd>
                {shipment.salesOrderId === null ? '—' : (
                  <RecordLink to={`/sales-orders/${shipment.salesOrderId}`}>{shipment.salesOrderNo}</RecordLink>
                )}
              </dd>
            </div>
            <div><dt>Müşteri sipariş no</dt><dd>{shipment.customerOrderNo ?? '—'}</dd></div>
            <div>
              <dt>İş emri</dt>
              <dd>
                {shipment.productionOrderId === null ? '—' : (
                  <RecordLink to={`/production-orders/${shipment.productionOrderId}`}>{shipment.orderNo}</RecordLink>
                )}
              </dd>
            </div>
            <div><dt>Çıkış deposu</dt><dd>{shipment.warehouseCode} — {shipment.warehouseName}</dd></div>
            <div><dt>Sevk tarihi</dt><dd>{formatDate(shipment.shipDate)}</dd></div>
            <div><dt>İrsaliye no</dt><dd>{shipment.documentNo ?? '—'}</dd></div>
            <div>
              <dt>Taşıyıcı / araç</dt>
              <dd>{[shipment.carrier, workflow?.vehiclePlate, workflow?.driverName].filter(Boolean).join(' · ') || '—'}</dd>
            </div>
            {workflow && (picking || shipment.status === 'SHIPPED') && (
              <div>
                <dt>Toplayan</dt>
                <dd>
                  {workflow.pickedBy
                    ? `${workflow.pickedBy} · ${formatDateTime(workflow.pickedAt ?? '')}`
                    : workflow.pickingBy
                      ? `${workflow.pickingBy} topluyor (başlangıç ${formatDateTime(workflow.pickingStartedAt ?? '')})`
                      : 'Toplamaya başlanmadı'}
                </dd>
              </div>
            )}
            <div>
              <dt>İrsaliyeyi kesen</dt>
              <dd>{shipment.shippedAt ? `${shipment.shippedBy} · ${formatDateTime(shipment.shippedAt)}` : '—'}</dd>
            </div>
            {workflow?.despatchUuid && <div><dt>ETTN</dt><dd><code>{workflow.despatchUuid}</code></dd></div>}
            {cartons !== null && cartons.cartonCount > 0 && (
              <div>
                <dt>Yükleme</dt>
                <dd>
                  {cartons.loadedCount} / {cartons.cartonCount} koli yüklendi
                  {unloaded === 0
                    ? <> <span className="badge badge--ok">Tamam</span></>
                    : <> <span className="badge badge--warning">{unloaded} eksik</span></>}
                </dd>
              </div>
            )}
          </dl>
          {shipment.notes && <p className="preserve-lines">{shipment.notes}</p>}
        </article>
      </div>

      <h2 className="section-title">Kalemler</h2>
      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              <th>Mamul</th>
              <th className="table__number">Sevk adedi</th>
              {showPicked && <th className="table__number">Toplanan</th>}
              {shipment.status === 'SHIPPED' && <th className="table__number">Kontrol</th>}
              {shipment.isEditable && <th className="table__number">Depodaki stok</th>}
              {(shipment.isEditable || picking) && <th>Durum</th>}
            </tr>
          </thead>
          <tbody>
            {shipment.items.map((item) => {
              const enough = item.availableQuantity >= item.quantity
              return (
                <tr key={item.id}>
                  <td><code>{item.itemCode}</code> {item.itemName}</td>
                  <td className="table__number">
                    <strong>{formatStockQuantity(item.quantity, item.unit)}</strong>
                    {item.requestedQuantity !== null && (
                      <span className="muted"> / istenen {formatStockQuantity(item.requestedQuantity, item.unit)}</span>
                    )}
                  </td>
                  {showPicked && <td className="table__number">{formatStockQuantity(item.pickedQuantity, item.unit)}</td>}
                  {shipment.status === 'SHIPPED' && (
                    <td className="table__number">{item.checkedQuantity === null ? '—' : formatStockQuantity(item.checkedQuantity, item.unit)}</td>
                  )}
                  {shipment.isEditable && (
                    <td className="table__number">{formatStockQuantity(item.availableQuantity, item.unit)}</td>
                  )}
                  {shipment.isEditable && (
                    <td>
                      {enough ? (
                        <span className="badge badge--ok"><span aria-hidden="true">✓</span> Stok yeterli</span>
                      ) : (
                        <span className="badge badge--danger"><span aria-hidden="true">!</span> Stok yetersiz</span>
                      )}
                    </td>
                  )}
                  {picking && (
                    <td>
                      {item.pickedQuantity >= item.quantity ? (
                        <span className="badge badge--ok"><span aria-hidden="true">✓</span> Toplandı</span>
                      ) : item.pickedQuantity > 0 ? (
                        <span className="badge badge--warning">Kısmen toplandı</span>
                      ) : (
                        <span className="badge badge--neutral">Bekliyor</span>
                      )}
                    </td>
                  )}
                </tr>
              )
            })}
            {shipment.items.length === 0 && (
              <tr>
                <td colSpan={shipment.isEditable ? 4 : 2} className="table__empty">Kalem yok. Toplamaya göndermek için önce kalem ekleyin.</td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      <ShipmentCartonsSection shipmentId={shipment.id} canManage={canManage} />

      {editing && <ShipmentDialog shipment={shipment} onClose={() => setEditing(false)} />}
    </section>
  )
}
