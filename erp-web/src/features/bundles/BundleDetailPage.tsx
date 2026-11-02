import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { RecordLink } from '../../components/RecordLink'
import { Modal } from '../../components/Modal'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDateTime, formatQuantity } from '../../utils/format'
import { BundleQualitySection } from '../quality/BundleQualitySection'
import { AssignCardDialog, ReleaseCardDialog } from '../rfid/CardDialogs'
import { BundleStatusBadge, OperationStatusBadge } from './BundleBadges'
import { eventQuantity, eventTypeLabels, groupOperationsByStage } from './bundlePresentation'
import { ManualReportDialog } from './ManualReportDialog'
import { operationSequenceHints, operationSequenceLabels } from '../productionOrders/productionOrdersApi'
import { useCancelBundleMutation, useGetBundleEventsQuery, useGetBundleQuery, useReleaseBundleOperationMutation } from './bundlesApi'
import type { BundleOperation } from './bundlesApi'

export function BundleDetailPage() {
  const id = Number(useParams().id)
  const canManage = usePermission(Permissions.BundleCreate)
  const canCancel = usePermission(Permissions.BundleCancel)
  const { data: bundle, isLoading, error } = useGetBundleQuery(id)
  const { data: events, error: eventsError } = useGetBundleEventsQuery(id)
  const [cancelBundle, cancelState] = useCancelBundleMutation()
  // Süren işi ofisten bırakma (tablet arızası, adet bildirmeden ayrılan operatör): ayrı yetki.
  const canRelease = usePermission(Permissions.ProductionReleaseJob)
  const [releaseOperation, releaseState] = useReleaseBundleOperationMutation()
  const [releasing, setReleasing] = useState<BundleOperation | null>(null)
  const [confirming, setConfirming] = useState(false)
  const [cardDialog, setCardDialog] = useState<'assign' | 'release' | null>(null)
  // Tablet ve kart olmadan elle bildirim (askıda kalan iş, arızalı tablet): ayrı yetki.
  const canManualReport = usePermission(Permissions.ProductionManualReport)
  const canReleaseCard = usePermission(Permissions.BundleReleaseCard)
  const [manualOperation, setManualOperation] = useState<BundleOperation | null>(null)

  if (isLoading) {
    return <section className="page"><p role="status">Yükleniyor…</p></section>
  }

  if (!bundle) {
    return (
      <section className="page">
        <ErrorAlert error={error} />
        <Link to="/bundles">← Demetlere dön</Link>
      </section>
    )
  }

  // Tamamlanmış, kapanmış, iptal edilmiş ya da kalite bekletmesindeki demet elle de ilerletilmez.
  const bundleOpen = !['COMPLETED', 'CLOSED', 'CANCELLED', 'QUALITY_HOLD'].includes(bundle.status)
  const manualAllowed = (operation: BundleOperation) =>
    bundleOpen && ['READY', 'PAUSED', 'IN_PROGRESS'].includes(operation.status)

  const runRelease = async () => {
    const operation = releasing
    setReleasing(null)
    if (operation) {
      await releaseOperation(operation.id)
    }
  }

  const runCancel = async () => {
    setConfirming(false)
    await cancelBundle({ id: bundle.id, rowVersion: bundle.rowVersion })
  }

  return (
    <section className="page">
      <p><Link to="/bundles">← Demetlere dön</Link></p>

      <header className="page__header">
        <div>
          <h1>
            Demet {bundle.bundleNo} <BundleStatusBadge status={bundle.status} />
            {bundle.atSubcontractor && <> <span className="badge badge--info"><span aria-hidden="true">→</span> Fasonda</span></>}
          </h1>
          <p className="muted">
            {bundle.createdBy ?? 'sistem'} tarafından {formatDateTime(bundle.createdAt)} tarihinde oluşturuldu.
          </p>
        </div>
        <div className="page__actions">
          {bundle.status !== 'CANCELLED' && (
            <Link to={`/bundles/labels?bundleId=${bundle.id}`} className="button">Etiket yazdır</Link>
          )}
          {canCancel && bundle.canCancel && (
            <button type="button" className="button" disabled={cancelState.isLoading} onClick={() => setConfirming(true)}>
              Demeti iptal et
            </button>
          )}
        </div>
      </header>

      <ErrorAlert error={cancelState.error} />

      <div className="cards">
        <article className="card">
          <h2>Demet</h2>
          <dl className="status-list">
            <div>
              <dt>Sipariş</dt>
              <dd><RecordLink to={`/production-orders/${bundle.productionOrderId}`}>{bundle.orderNo}</RecordLink></dd>
            </div>
            <div><dt>Müşteri</dt><dd>{bundle.customerName}</dd></div>
            <div><dt>Model</dt><dd>{bundle.modelCode} · rota v{bundle.routeVersion}</dd></div>
            <div><dt>Renk / beden</dt><dd>{bundle.colorName} / {bundle.sizeName}</dd></div>
            <div><dt>Adet</dt><dd>{formatQuantity(bundle.quantity)}</dd></div>
            <div><dt>Operasyon sırası</dt><dd title={operationSequenceHints[bundle.sequenceMode]}>{operationSequenceLabels[bundle.sequenceMode]}</dd></div>
          </dl>
        </article>
        <article className="card">
          <h2>RFID kartı</h2>
          {bundle.rfidUid ? (
            <p><code>{bundle.rfidUid}</code></p>
          ) : (
            <p className="muted">Kart atanmamış. Demet, kartı atanınca üretime hazır olur.</p>
          )}
          {canManage && bundle.canAssignCard && (
            <button type="button" className="button button--primary" onClick={() => setCardDialog('assign')}>
              Kart ata
            </button>
          )}
          {canReleaseCard && bundle.rfidUid && (
            <button type="button" className="button" onClick={() => setCardDialog('release')}>Kartı bırak</button>
          )}
        </article>
        <article className="card">
          <h2>Şu an nerede?</h2>
          {bundle.currentOperations.length > 0 ? (
            <ul className="plain-list">
              {bundle.currentOperations.map((name) => (
                <li key={name}><strong>{name}</strong></li>
              ))}
            </ul>
          ) : (
            <p className="muted">Demetin bekleyen ya da süren operasyonu yok.</p>
          )}
        </article>
      </div>

      <h2 className="section-title">Akış</h2>
      <ol className="route-flow" aria-label="Demet operasyon akışı">
        {groupOperationsByStage(bundle.operations).map((operations, index) => (
          <li key={operations[0].stage} className="route-flow__stage">
            <div className="route-flow__stage-title">
              {index + 1}. aşama{operations.length > 1 && ' · paralel'}
            </div>
            {operations.map((operation) => (
              <div key={operation.id} className="route-flow__step">
                <strong>{operation.sequenceNo}</strong> {operation.operationName}
                <div><OperationStatusBadge status={operation.status} /></div>
              </div>
            ))}
          </li>
        ))}
      </ol>

      <h2 className="section-title">Operasyonlar</h2>
      <ErrorAlert error={releaseState.error} />
      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              <th className="table__number">Sıra</th>
              <th>Operasyon</th>
              <th>Durum</th>
              <th className="table__number">Üretilen</th>
              <th className="table__number">Hatalı</th>
              <th className="table__number">Kalan</th>
              <th>Makine</th>
              <th>Operatör</th>
              <th>Başlangıç</th>
              <th>Bitiş</th>
            </tr>
          </thead>
          <tbody>
            {bundle.operations.map((operation) => (
              <tr key={operation.id}>
                <td className="table__number"><strong>{operation.sequenceNo}</strong></td>
                <td>
                  {operation.operationName}
                  {operation.predecessors.length > 0 && (
                    <div className="muted">önce: {operation.predecessors.join(', ')}</div>
                  )}
                </td>
                <td>
                  <OperationStatusBadge status={operation.status} />
                  {((canRelease && operation.status === 'IN_PROGRESS') || (canManualReport && manualAllowed(operation))) && (
                    <div className="table__actions table__actions--inline">
                      {canManualReport && manualAllowed(operation) && (
                        <button type="button" className="button" onClick={() => setManualOperation(operation)}>
                          Elle bildir
                        </button>
                      )}
                      {canRelease && operation.status === 'IN_PROGRESS' && (
                        <button type="button" className="button" disabled={releaseState.isLoading} onClick={() => setReleasing(operation)}>
                          Süren işi bırak
                        </button>
                      )}
                    </div>
                  )}
                </td>
                <td className="table__number">{formatQuantity(operation.producedQuantity)}</td>
                <td className="table__number">{formatQuantity(operation.rejectedQuantity)}</td>
                <td className="table__number">
                  {formatQuantity(operation.remainingQuantity)}
                  {operation.reworkQuantity > 0 && <div className="muted">{operation.reworkQuantity} tamir</div>}
                </td>
                <td>{operation.machineCode ?? '—'}</td>
                <td>{operation.operatorName ?? '—'}</td>
                <td>{formatDateTime(operation.startedAt)}</td>
                <td>{formatDateTime(operation.completedAt)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <BundleQualitySection bundleId={bundle.id} />

      <h2 className="section-title">Üretim hareketleri</h2>
      <ErrorAlert error={eventsError} />
      {events && events.length === 0 && <p className="muted">Bu demette henüz üretim hareketi yok.</p>}
      {events && events.length > 0 && (
        <div className="table-wrap">
          <table className="table table--compact">
            <thead>
              <tr>
                <th>Zaman</th>
                <th>Operasyon</th>
                <th>Olay</th>
                <th className="table__number">Adet</th>
                <th>Makine</th>
                <th>Operatör</th>
                <th>Kaynak</th>
              </tr>
            </thead>
            <tbody>
              {events.map((event) => (
                <tr key={event.id}>
                  <td>{formatDateTime(event.occurredAt)}</td>
                  <td><strong>{event.sequenceNo}</strong> {event.operationName}</td>
                  <td>{eventTypeLabels[event.eventType]}</td>
                  <td className="table__number">{eventQuantity(event) ?? '—'}</td>
                  <td>{event.machineCode}</td>
                  <td>{event.operatorName}</td>
                  <td>{event.enteredBy ? <span title="Ofisten, tablet ve kart olmadan girildi">Elle · {event.enteredBy}</span> : 'Tablet'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {cardDialog === 'assign' && (
        <AssignCardDialog
          owner="bundles"
          id={bundle.id}
          subject={`Demet ${bundle.bundleNo}`}
          onClose={() => setCardDialog(null)}
        />
      )}
      {cardDialog === 'release' && bundle.rfidUid && (
        <ReleaseCardDialog
          owner="bundles"
          id={bundle.id}
          subject={`Demet ${bundle.bundleNo}`}
          uid={bundle.rfidUid}
          onClose={() => setCardDialog(null)}
        />
      )}

      {manualOperation && (
        <ManualReportDialog
          operationId={manualOperation.id}
          bundleNo={bundle.bundleNo}
          onClose={() => setManualOperation(null)}
        />
      )}

      {releasing && (
        <Modal title="Süren işi bırak" onClose={() => setReleasing(null)}>
          <div className="form">
            <p>
              {releasing.sequenceNo} – {releasing.operationName} operasyonu
              {releasing.machineCode ? ` ${releasing.machineCode} makinesinde` : ''}
              {releasing.operatorName ? ` (${releasing.operatorName})` : ''} işlemde görünüyor. İş adet bildirilmeden
              bırakılacak: üretilen adet değişmez, kalan {formatQuantity(releasing.remainingQuantity)} adet uygun bir
              makinede sürdürülebilir; makine ve operatör serbest kalır.
            </p>
            <p className="muted">
              Yalnızca tablet arızalandığında, başka makineye bağlandığında ya da operatör adet bildirmeden
              ayrıldığında kullanın. Üretilmiş ama bildirilmemiş parçalar varsa iş sürdürülürken bildirilmelidir.
            </p>
            <footer className="form__actions">
              <button type="button" className="button" onClick={() => setReleasing(null)}>Vazgeç</button>
              <button type="button" className="button button--primary" onClick={() => void runRelease()}>İşi bırak</button>
            </footer>
          </div>
        </Modal>
      )}

      {confirming && (
        <Modal title="Demeti iptal et" onClose={() => setConfirming(false)}>
          <div className="form">
            <p>
              Demet {bundle.bundleNo} iptal edilecek. {formatQuantity(bundle.quantity)} adet siparişte yeniden
              demetlenebilir hale gelir; demet numarası yeniden kullanılmaz.
            </p>
            <footer className="form__actions">
              <button type="button" className="button" onClick={() => setConfirming(false)}>Vazgeç</button>
              <button type="button" className="button button--danger" onClick={() => void runCancel()}>İptal et</button>
            </footer>
          </div>
        </Modal>
      )}
    </section>
  )
}
