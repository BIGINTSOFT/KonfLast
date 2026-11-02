import { useState } from 'react'
import { Link } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatQuantity } from '../../utils/format'
import { AssignCardDialog } from '../rfid/CardDialogs'
import { BundleStatusBadge } from './BundleBadges'
import { useGetBundlesQuery, useGetOrderBundleSummaryQuery } from './bundlesApi'
import type { BundleListItem, BundleStatus, OrderBundleSummary } from './bundlesApi'
import { AssignCardsDialog, GenerateBundlesDialog, ReceiveBundlesDialog } from './OrderFlowDialogs'
import { orderFlowSteps } from './orderFlow'

const LIST_SIZE = 50

/** Kart atanamayan (kapanmış) demet durumları; son kararı sunucu verir. */
const CLOSED: BundleStatus[] = ['COMPLETED', 'CANCELLED', 'CLOSED']

type Dialog = 'generate' | 'cards' | 'receive'

function FlowSteps({ summary }: { summary: OrderBundleSummary }) {
  return (
    <ol className="flow">
      {orderFlowSteps(summary).map((step, index) => (
        <li key={step.key} className={`flow__step flow__step--${step.state}`}>
          <span className="flow__index" aria-hidden="true">{step.state === 'done' ? '✓' : index + 1}</span>
          <span className="flow__body">
            <strong>{step.title}</strong>
            <span className="muted">{step.detail}</span>
          </span>
        </li>
      ))}
    </ol>
  )
}

/**
 * Sipariş sayfasındaki üretim akışı: demetle → kart ata → üret → depoya al. Her adımın sayısı ve bir sonraki
 * işlem aynı ekrandadır; demet listesi de burada görünür.
 */
export function OrderBundlesSection({ orderId }: { orderId: number }) {
  const canView = usePermission(Permissions.BundleView)
  const canCreate = usePermission(Permissions.BundleCreate)
  const canReceive = usePermission(Permissions.StockManage)
  const { data: summary, error } = useGetOrderBundleSummaryQuery(orderId, { skip: !canView })
  const bundles = useGetBundlesQuery(
    { page: 1, pageSize: LIST_SIZE, productionOrderId: orderId },
    { skip: !canView || !summary || summary.bundled === 0 },
  )
  const [dialog, setDialog] = useState<Dialog | null>(null)
  const [assigning, setAssigning] = useState<BundleListItem | null>(null)

  if (!canView) {
    return null
  }

  const progress = summary?.progress
  const canGenerate = canCreate && summary?.canCreateBundles === true && summary.remaining > 0
  const cardsWaiting = progress?.withoutCard ?? 0
  const toReceive = progress?.completed ?? 0
  // Sıradaki iş öne çıkar: önce demetleme, sonra kart, sonra depoya alma.
  const next: Dialog | null = canGenerate ? 'generate' : canCreate && cardsWaiting > 0 ? 'cards' : canReceive && toReceive > 0 ? 'receive' : null
  const buttonClass = (kind: Dialog) => (next === kind ? 'button button--primary' : 'button')

  return (
    <>
      <header className="page__header section-title">
        <h2>Üretim akışı</h2>
        <div className="page__actions">
          {canGenerate && (
            <button type="button" className={buttonClass('generate')} onClick={() => setDialog('generate')}>
              Toplu demet oluştur
            </button>
          )}
          {canCreate && cardsWaiting > 0 && (
            <button type="button" className={buttonClass('cards')} onClick={() => setDialog('cards')}>
              Kartları ata ({cardsWaiting})
            </button>
          )}
          {summary && summary.bundled > 0 && (
            <Link to={`/bundles/labels?orderId=${orderId}`} className="button">Etiketleri yazdır</Link>
          )}
          {canReceive && toReceive > 0 && (
            <button type="button" className={buttonClass('receive')} onClick={() => setDialog('receive')}>
              Depoya al ({toReceive})
            </button>
          )}
        </div>
      </header>

      <ErrorAlert error={error ?? bundles.error} />
      {summary?.blockedReason && <p className="alert alert--info">{summary.blockedReason}</p>}
      {summary && <FlowSteps summary={summary} />}

      {summary && summary.items.length > 0 && (
        <div className="table-wrap">
          <table className="table table--compact">
            <thead>
              <tr>
                <th>Renk</th>
                <th>Beden</th>
                <th className="table__number">Sipariş</th>
                <th className="table__number">Demetlenen</th>
                <th className="table__number">Kalan</th>
                <th className="table__number">Demet sayısı</th>
              </tr>
            </thead>
            <tbody>
              {summary.items.map((item) => (
                <tr key={`${item.colorId}:${item.sizeId}`}>
                  <td>{item.colorName}</td>
                  <td>{item.sizeName}</td>
                  <td className="table__number">{formatQuantity(item.ordered)}</td>
                  <td className="table__number">{formatQuantity(item.bundled)}</td>
                  <td className="table__number">{formatQuantity(item.remaining)}</td>
                  <td className="table__number">{item.bundleCount}</td>
                </tr>
              ))}
            </tbody>
            <tfoot>
              <tr>
                <th scope="row" colSpan={2}>Toplam</th>
                <td className="table__number"><strong>{formatQuantity(summary.ordered)}</strong></td>
                <td className="table__number"><strong>{formatQuantity(summary.bundled)}</strong></td>
                <td className="table__number"><strong>{formatQuantity(summary.remaining)}</strong></td>
                <td />
              </tr>
            </tfoot>
          </table>
        </div>
      )}

      {bundles.data && bundles.data.items.length > 0 && (
        <>
          <header className="page__header section-title">
            <h2>Demetler</h2>
            {bundles.data.totalCount > bundles.data.items.length && (
              <Link to={`/bundles?orderId=${orderId}`} className="button">
                Tümünü gör ({bundles.data.totalCount})
              </Link>
            )}
          </header>
          <div className="table-wrap" aria-busy={bundles.isFetching}>
            <table className="table table--compact">
              <thead>
                <tr>
                  <th>Demet no</th>
                  <th>Renk</th>
                  <th>Beden</th>
                  <th className="table__number">Adet</th>
                  <th>Operasyon</th>
                  <th>Kart</th>
                  <th>Durum</th>
                  <th aria-label="İşlemler" />
                </tr>
              </thead>
              <tbody>
                {bundles.data.items.map((bundle) => (
                  <tr key={bundle.id}>
                    <td><Link to={`/bundles/${bundle.id}`}><code>{bundle.bundleNo}</code></Link></td>
                    <td>{bundle.colorName}</td>
                    <td>{bundle.sizeName}</td>
                    <td className="table__number">{formatQuantity(bundle.quantity)}</td>
                    <td>{bundle.completedOperations} / {bundle.totalOperations}</td>
                    <td>
                      {bundle.rfidUid ? (
                        <code>{bundle.rfidUid}</code>
                      ) : canCreate && !CLOSED.includes(bundle.status) ? (
                        <button type="button" className="button button--inline" onClick={() => setAssigning(bundle)}>Kart ata</button>
                      ) : (
                        '—'
                      )}
                    </td>
                    <td><BundleStatusBadge status={bundle.status} /></td>
                    <td className="table__actions">
                      {bundle.status !== 'CANCELLED' && (
                        <Link to={`/bundles/labels?bundleId=${bundle.id}`} className="button">Etiket</Link>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}

      {dialog === 'generate' && summary && (
        <GenerateBundlesDialog
          orderId={orderId}
          remaining={summary.remaining}
          hasRecipe={summary.hasRecipe}
          onClose={() => setDialog(null)}
        />
      )}
      {dialog === 'cards' && <AssignCardsDialog orderId={orderId} onClose={() => setDialog(null)} />}
      {dialog === 'receive' && <ReceiveBundlesDialog orderId={orderId} onClose={() => setDialog(null)} />}
      {assigning && (
        <AssignCardDialog
          owner="bundles"
          id={assigning.id}
          subject={`Demet ${assigning.bundleNo}`}
          onClose={() => setAssigning(null)}
        />
      )}
    </>
  )
}
