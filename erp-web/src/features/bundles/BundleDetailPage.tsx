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
import { eventQuantity, eventTypeLabels, groupOperationsByStage, operationStatusPresentation } from './bundlePresentation'
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
  const rowActions = (operation: BundleOperation) => ({
    manual: canManualReport && manualAllowed(operation),
    release: canRelease && operation.status === 'IN_PROGRESS',
  })
  const hasRowActions = bundle.operations.some((operation) => {
    const actions = rowActions(operation)
    return actions.manual || actions.release
  })
  // Sıralı demette öncül her zaman bir önceki adımdır; yalnızca sırasız / rotalı demette gösterilir.
  const showPredecessors = bundle.sequenceMode !== 'SEQUENTIAL'

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

      {/* Demetin özeti tek şeritte: kart ve "şu an nerede" ayrı büyük kartlarda yer kaplamaz. */}
      <dl className="bundle-summary">
        <div>
          <dt>İş emri</dt>
          <dd><RecordLink to={`/production-orders/${bundle.productionOrderId}`}>{bundle.orderNo}</RecordLink></dd>
        </div>
        <div><dt>Müşteri</dt><dd>{bundle.customerName}</dd></div>
        <div><dt>Model</dt><dd>{bundle.modelCode} <span className="muted">· rota v{bundle.routeVersion}</span></dd></div>
        <div><dt>Renk / beden</dt><dd>{bundle.colorName} / {bundle.sizeName}</dd></div>
        <div><dt>Adet</dt><dd>{formatQuantity(bundle.quantity)}</dd></div>
        <div>
          <dt>Operasyon sırası</dt>
          <dd title={operationSequenceHints[bundle.sequenceMode]}>{operationSequenceLabels[bundle.sequenceMode]}</dd>
        </div>
        <div className="bundle-summary__wide">
          <dt>Şu an nerede?</dt>
          <dd>
            {bundle.currentOperations.length > 0
              ? <strong>{bundle.currentOperations.join(', ')}</strong>
              : <span className="muted">Bekleyen ya da süren operasyon yok</span>}
          </dd>
        </div>
        <div className="bundle-summary__wide">
          <dt>RFID kartı</dt>
          <dd className="bundle-summary__card">
            {bundle.rfidUid ? <code>{bundle.rfidUid}</code> : <span className="muted">Atanmamış</span>}
            {canManage && bundle.canAssignCard && (
              <button type="button" className="button button--inline button--primary" onClick={() => setCardDialog('assign')}>
                Kart ata
              </button>
            )}
            {canReleaseCard && bundle.rfidUid && (
              <button type="button" className="button button--inline" onClick={() => setCardDialog('release')}>Kartı bırak</button>
            )}
          </dd>
        </div>
      </dl>
      {!bundle.rfidUid && bundle.canAssignCard && (
        <p className="muted">Demet, kartı atanınca üretime hazır olur.</p>
      )}

      <h2 className="section-title">
        Akış <span className="muted">· {bundle.operations.filter((operation) => operation.status === 'COMPLETED').length} / {bundle.operations.length} tamamlandı</span>
      </h2>
      {/* Adımlar sırayla tek akışta; paralel aşama tek grupta. Renk durumu, ikon ve metin de söyler. */}
      <ol className="route-steps" aria-label="Demet operasyon akışı">
        {groupOperationsByStage(bundle.operations).map((operations) => (
          <li
            key={operations[0].stage}
            className={operations.length > 1 ? 'route-steps__stage route-steps__stage--parallel' : 'route-steps__stage'}
            title={operations.length > 1 ? 'Paralel aşama: adımlar aynı anda yapılabilir.' : undefined}
          >
            {operations.map((operation) => {
              const { label, icon, tone } = operationStatusPresentation[operation.status]
              return (
                <span key={operation.id} className={`route-steps__step route-steps__step--${tone}${operation.status === 'COMPLETED' ? ' route-steps__step--done' : ''}`} title={label}>
                  <span aria-hidden="true">{icon}</span>
                  <strong>{operation.sequenceNo}</strong> {operation.operationName}
                  <span className="visually-hidden"> ({label})</span>
                </span>
              )
            })}
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
              {hasRowActions && <th aria-label="İşlemler" />}
            </tr>
          </thead>
          <tbody>
            {bundle.operations.map((operation) => {
              const actions = rowActions(operation)
              // Sıradaki (hazır) ya da süren operasyon vurgulanır.
              const current = ['READY', 'IN_PROGRESS', 'PAUSED'].includes(operation.status)
              return (
              <tr key={operation.id} className={current ? 'table__row--current' : undefined}>
                <td className="table__number"><strong>{operation.sequenceNo}</strong></td>
                <td>
                  {operation.operationName}
                  {showPredecessors && operation.predecessors.length > 0 && (
                    <div className="muted">önce: {operation.predecessors.join(', ')}</div>
                  )}
                </td>
                <td><OperationStatusBadge status={operation.status} /></td>
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
                {hasRowActions && (
                  <td className="table__actions">
                    {actions.manual && (
                      <button type="button" className="button" onClick={() => setManualOperation(operation)}>
                        Elle bildir
                      </button>
                    )}
                    {actions.release && (
                      <button type="button" className="button" disabled={releaseState.isLoading} onClick={() => setReleasing(operation)}>
                        Süren işi bırak
                      </button>
                    )}
                  </td>
                )}
              </tr>
              )
            })}
          </tbody>
        </table>
      </div>

      <BundleQualitySection bundleId={bundle.id} />

      <h2 className="section-title">Üretim hareketleri</h2>
      <ErrorAlert error={eventsError} />
      {events && events.length === 0 && <p className="muted">Bu demette henüz üretim hareketi yok.</p>}
      {events && events.length > 0 && (
        // Hareketler uzar: tablo sabit yükseklikte kendi içinde kayar, başlık satırı üstte kalır.
        <div className="table-wrap table-wrap--scroll">
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
                  <td>
                    {event.operatorName}
                    {event.enteredBy && !event.countsForPerformance && event.operatorName !== 'Ofis kapatması' && (
                      <span className="muted" title="Bu adet operatörün performansına ve primine sayılmaz"> · performansa sayılmaz</span>
                    )}
                  </td>
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
