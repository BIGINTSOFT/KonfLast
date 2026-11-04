import { Printer } from 'lucide-react'
import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDateTime, formatQuantity } from '../../utils/format'
import { BundleStatusBadge } from '../bundles/BundleBadges'
import { formatStockQuantity } from '../stock/stockPresentation'
import { CutDialog } from './CutDialog'
import { CuttingPlanStatusBadge, DifferenceCell } from './CuttingBadges'
import { CuttingPlanFormDialog } from './CuttingPlanFormDialog'
import { useCancelCuttingPlanMutation, useGetCuttingPlanQuery } from './cuttingApi'
import { formatMeters, shadeText } from './cuttingPresentation'

type Dialog = 'edit' | 'cut' | 'cancel'

export function CuttingPlanDetailPage() {
  const id = Number(useParams().id)
  const canManage = usePermission(Permissions.CuttingPlanManage)
  const { data: plan, isLoading, error } = useGetCuttingPlanQuery(id)
  const [cancel, cancelState] = useCancelCuttingPlanMutation()
  const [dialog, setDialog] = useState<Dialog | null>(null)

  if (isLoading) {
    return <section className="page"><p role="status">Yükleniyor…</p></section>
  }

  if (!plan) {
    return (
      <section className="page">
        <ErrorAlert error={error} />
        <Link to="/cutting-plans">← Kesim föylerine dön</Link>
      </section>
    )
  }

  const isCut = plan.status === 'CUT'

  return (
    <section className="page">
      <p><Link to="/cutting-plans">← Kesim föylerine dön</Link></p>

      <header className="page__header">
        <div>
          <h1>{plan.planNo} <CuttingPlanStatusBadge status={plan.status} /></h1>
          <p className="muted">
            {plan.createdBy ?? 'sistem'} tarafından {formatDateTime(plan.createdAt)} tarihinde oluşturuldu.
            {plan.cutAt && ` Kesim ${formatDateTime(plan.cutAt)} tarihinde ${plan.cutBy ?? 'sistem'} tarafından kaydedildi.`}
          </p>
        </div>
        <div className="page__actions">
          {plan.status !== 'CANCELLED' && (
            <Link to={`/cutting-plans/${plan.id}/sheet`} className="button">
              <Printer aria-hidden="true" />
              Föyü yazdır
            </Link>
          )}
          {canManage && plan.canEdit && (
            <>
              <button type="button" className="button" onClick={() => setDialog('edit')}>Düzenle</button>
              <button type="button" className="button" onClick={() => setDialog('cancel')}>İptal et</button>
              <button type="button" className="button button--primary" disabled={!plan.canCut} onClick={() => setDialog('cut')}>
                Kesimi kaydet
              </button>
            </>
          )}
        </div>
      </header>

      <ErrorAlert error={cancelState.error} />
      {plan.cutBlockedReason && <p className="alert alert--info">{plan.cutBlockedReason}</p>}
      {plan.warnings.length > 0 && (
        <div className="alert alert--info" role="status">
          <strong>Uyarılar</strong>
          <ul>
            {plan.warnings.map((warning) => <li key={warning}>{warning}</li>)}
          </ul>
        </div>
      )}

      <div className="cards">
        <article className="card">
          <h2>İş emri</h2>
          <dl className="status-list">
            <div><dt>İş emri</dt><dd><Link to={`/production-orders/${plan.productionOrderId}`}>{plan.orderNo}</Link></dd></div>
            <div><dt>Müşteri</dt><dd>{plan.customerName}</dd></div>
            <div><dt>Ürün</dt><dd><code>{plan.productCode}</code> {plan.productName}</dd></div>
            <div><dt>Model</dt><dd>{plan.modelCode}</dd></div>
            <div><dt>Renk</dt><dd>{plan.colorName}</dd></div>
          </dl>
        </article>
        <article className="card">
          <h2>Pastal</h2>
          <dl className="status-list">
            <div><dt>Pastal boyu</dt><dd>{formatMeters(plan.markerLength)}</dd></div>
            <div><dt>Kat başı pay</dt><dd>{formatMeters(plan.endAllowance)}</dd></div>
            <div>
              <dt>Kat sayısı</dt>
              <dd>
                {isCut ? `${plan.actualPlies} (planlanan ${plan.plannedPlies})` : plan.plannedPlies}
                {plan.canEdit && plan.suggestedPlies > 0 && plan.suggestedPlies !== plan.plannedPlies && (
                  <span className="muted"> · önerilen {plan.suggestedPlies}</span>
                )}
              </dd>
            </div>
            <div><dt>Katta parça</dt><dd>{plan.piecesPerPly}</dd></div>
            <div>
              <dt>Adet</dt>
              <dd>{isCut ? `${formatQuantity(plan.cutQuantity ?? 0)} kesildi (planlanan ${formatQuantity(plan.plannedQuantity)})` : formatQuantity(plan.plannedQuantity)}</dd>
            </div>
            <div>
              <dt>Kumaş</dt>
              <dd>
                {formatMeters(plan.plannedFabric)} planlanan
                {plan.actualFabric !== null && plan.lots.length > 0 && ` · ${formatStockQuantity(plan.actualFabric, plan.lots[0].unit)} düşüldü`}
              </dd>
            </div>
            <div><dt>Ton</dt><dd>{plan.shadeKey ?? '—'}</dd></div>
          </dl>
        </article>
      </div>

      {plan.notes && (
        <article className="card card--spaced">
          <h2>Not</h2>
          <p className="preserve-lines">{plan.notes}</p>
        </article>
      )}

      <h2 className="section-title">Beden oranı</h2>
      <div className="table-wrap">
        <table className="table table--compact">
          <thead>
            <tr>
              <th>Beden</th>
              <th className="table__number">Oran</th>
              <th className="table__number">Sipariş</th>
              <th className="table__number">Kesilecek kalan</th>
              <th className="table__number">Planlanan</th>
              {isCut && <th className="table__number">Kesilen</th>}
              <th>Fark</th>
            </tr>
          </thead>
          <tbody>
            {plan.sizes.map((size) => (
              <tr key={size.sizeId}>
                <td>{size.sizeName}</td>
                <td className="table__number">{size.ratio}</td>
                <td className="table__number">{formatQuantity(size.ordered)}</td>
                <td className="table__number">{formatQuantity(size.remaining)}</td>
                <td className="table__number">{formatQuantity(size.plannedQuantity)}</td>
                {isCut && <td className="table__number">{formatQuantity(size.cutQuantity ?? 0)}</td>}
                <td><DifferenceCell value={size.difference} /></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <h2 className="section-title">Kumaş topları</h2>
      {plan.lots.length === 0 ? (
        <p className="muted">Top atanmamış: kumaş kesimde reçeteye göre düşülür.</p>
      ) : (
        <div className="table-wrap">
          <table className="table table--compact">
            <thead>
              <tr>
                <th>Sıra</th>
                <th>Top</th>
                <th>Kumaş</th>
                <th>Ton / parti</th>
                <th className="table__number">En (cm)</th>
                <th>Tedarikçi top no</th>
                <th className="table__number">Bakiye</th>
                <th>Yer</th>
                {isCut && <th className="table__number">Kullanılan</th>}
              </tr>
            </thead>
            <tbody>
              {plan.lots.map((lot) => (
                <tr key={lot.id}>
                  <td>{lot.sortOrder}</td>
                  <td><code>{lot.lotNo}</code></td>
                  <td><code>{lot.stockItemCode}</code> {lot.stockItemName}</td>
                  <td>{shadeText(lot)}</td>
                  <td className="table__number">{lot.width ?? '—'}</td>
                  <td>{lot.supplierRollNo ?? '—'}</td>
                  <td className="table__number">{formatStockQuantity(lot.balance, lot.unit)}</td>
                  <td>{lot.locations || '—'}</td>
                  {isCut && <td className="table__number">{lot.usedQuantity !== null ? formatStockQuantity(lot.usedQuantity, lot.unit) : '—'}</td>}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {plan.bundles.length > 0 && (
        <>
          <header className="page__header section-title">
            <h2>Açılan demetler ({plan.bundles.length})</h2>
            <Link to={`/bundles/labels?orderId=${plan.productionOrderId}`} className="button">Etiketleri yazdır</Link>
          </header>
          <div className="table-wrap">
            <table className="table table--compact">
              <thead>
                <tr>
                  <th>Demet no</th>
                  <th>Beden</th>
                  <th className="table__number">Adet</th>
                  <th>Durum</th>
                </tr>
              </thead>
              <tbody>
                {plan.bundles.map((bundle) => (
                  <tr key={bundle.id}>
                    <td><Link to={`/bundles/${bundle.id}`}><code>{bundle.bundleNo}</code></Link></td>
                    <td>{bundle.sizeName}</td>
                    <td className="table__number">{formatQuantity(bundle.quantity)}</td>
                    <td><BundleStatusBadge status={bundle.status} /></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}

      {dialog === 'edit' && (
        <CuttingPlanFormDialog orderId={plan.productionOrderId} plan={plan} onClose={() => setDialog(null)} />
      )}
      {dialog === 'cut' && <CutDialog plan={plan} onClose={() => setDialog(null)} />}
      {dialog === 'cancel' && (
        <Modal title="Kesim föyünü iptal et" onClose={() => setDialog(null)}>
          <div className="form">
            <p>{plan.planNo} iptal edilecek. İptal edilen plan yeniden açılamaz; numarası yeniden kullanılmaz.</p>
            <footer className="form__actions">
              <button type="button" className="button" onClick={() => setDialog(null)}>Vazgeç</button>
              <button
                type="button"
                className="button button--danger"
                disabled={cancelState.isLoading}
                onClick={() => {
                  setDialog(null)
                  void cancel({ id: plan.id, rowVersion: plan.rowVersion })
                }}
              >
                İptal et
              </button>
            </footer>
          </div>
        </Modal>
      )}
    </section>
  )
}
