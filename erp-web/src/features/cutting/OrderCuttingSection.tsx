import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatQuantity } from '../../utils/format'
import { CuttingPlanStatusBadge } from './CuttingBadges'
import { CuttingPlanFormDialog } from './CuttingPlanFormDialog'
import { useGetOrderCuttingSummaryQuery } from './cuttingApi'
import type { CuttingSummaryItem } from './cuttingApi'
import { formatMeters } from './cuttingPresentation'

/** Renk satırı, beden sütunu: hücrede kesilecek kalan; eksi değer fazla kesimdir. */
function RemainingMatrix({ items }: { items: CuttingSummaryItem[] }) {
  const colors = [...new Map(items.map((item) => [item.colorId, item.colorName])).entries()]
  const sizes = [...new Map(items.map((item) => [item.sizeId, item.sizeName])).entries()]
  const cell = (colorId: number, sizeId: number) => items.find((item) => item.colorId === colorId && item.sizeId === sizeId)

  return (
    <div className="table-wrap">
      <table className="table table--compact">
        <thead>
          <tr>
            <th>Renk</th>
            {sizes.map(([sizeId, sizeName]) => <th key={sizeId} className="table__number">{sizeName}</th>)}
            <th className="table__number">Toplam</th>
          </tr>
        </thead>
        <tbody>
          {colors.map(([colorId, colorName]) => {
            const rowItems = items.filter((item) => item.colorId === colorId)
            const total = rowItems.reduce((sum, item) => sum + item.remaining, 0)
            return (
              <tr key={colorId}>
                <td>{colorName}</td>
                {sizes.map(([sizeId]) => {
                  const item = cell(colorId, sizeId)
                  if (!item) {
                    return <td key={sizeId} className="table__number muted">—</td>
                  }
                  return (
                    <td
                      key={sizeId}
                      className="table__number"
                      title={`Sipariş ${item.ordered} · kesilen ${item.cut} · plansız demet ${item.bundledOutside} · taslak planlarda ${item.plannedInDrafts}`}
                    >
                      {item.remaining < 0
                        ? <span className="badge badge--warning">{-item.remaining} fazla</span>
                        : formatQuantity(item.remaining)}
                      {item.plannedInDrafts > 0 && <div className="muted">planda {formatQuantity(item.plannedInDrafts)}</div>}
                    </td>
                  )
                })}
                <td className="table__number"><strong>{formatQuantity(total)}</strong></td>
              </tr>
            )
          })}
        </tbody>
      </table>
    </div>
  )
}

/**
 * Sipariş sayfasındaki kesim bölümü: renk-beden bazında kesilecek kalan (sipariş − kesilen − plansız demetlenen) ve
 * siparişin kesim planları. Yeni plan buradan açılır.
 */
export function OrderCuttingSection({ orderId }: { orderId: number }) {
  const canView = usePermission(Permissions.CuttingPlanView)
  const canManage = usePermission(Permissions.CuttingPlanManage)
  const navigate = useNavigate()
  const { data: summary, error } = useGetOrderCuttingSummaryQuery(orderId, { skip: !canView })
  const [creating, setCreating] = useState(false)

  if (!canView) {
    return null
  }

  const totalRemaining = summary?.items.reduce((sum, item) => sum + Math.max(item.remaining, 0), 0) ?? 0

  return (
    <>
      <header className="page__header section-title">
        <h2>Kesim</h2>
        <div className="page__actions">
          {summary && summary.plans.length > 0 && (
            <Link to={`/cutting-plans?orderId=${orderId}`} className="button">Kesim planları</Link>
          )}
          {canManage && summary?.canPlan && (
            <button
              type="button"
              className={totalRemaining > 0 ? 'button button--primary' : 'button'}
              onClick={() => setCreating(true)}
            >
              Yeni kesim planı
            </button>
          )}
        </div>
      </header>

      <ErrorAlert error={error} />
      {summary?.planBlockedReason && <p className="alert alert--info">{summary.planBlockedReason}</p>}
      {summary && !summary.planBlockedReason && summary.cutBlockedReason && (
        <p className="alert alert--info">{summary.cutBlockedReason}</p>
      )}

      {summary && summary.items.length > 0 && (
        <>
          <p className="muted">Kesilecek kalan: sipariş − kesilen − plan dışında demetlenen. Ayrıntı için hücrenin üzerine gelin.</p>
          <RemainingMatrix items={summary.items} />
        </>
      )}

      {summary && summary.plans.length > 0 && (
        <div className="table-wrap">
          <table className="table table--compact">
            <thead>
              <tr>
                <th>Plan no</th>
                <th>Renk</th>
                <th className="table__number">Pastal</th>
                <th className="table__number">Kat</th>
                <th className="table__number">Katta parça</th>
                <th className="table__number">Adet</th>
                <th className="table__number">Kumaş</th>
                <th>Durum</th>
              </tr>
            </thead>
            <tbody>
              {summary.plans.map((plan) => (
                <tr key={plan.id}>
                  <td><Link to={`/cutting-plans/${plan.id}`}><code>{plan.planNo}</code></Link></td>
                  <td>{plan.colorName}</td>
                  <td className="table__number">{formatMeters(plan.markerLength)}</td>
                  <td className="table__number">{plan.actualPlies ?? plan.plannedPlies}</td>
                  <td className="table__number">{plan.piecesPerPly}</td>
                  <td className="table__number">{formatQuantity(plan.cutQuantity ?? plan.plannedQuantity)}</td>
                  <td className="table__number">{formatMeters(plan.plannedFabric)}</td>
                  <td><CuttingPlanStatusBadge status={plan.status} /></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {creating && (
        <CuttingPlanFormDialog
          orderId={orderId}
          onClose={() => setCreating(false)}
          onSaved={(plan) => void navigate(`/cutting-plans/${plan.id}`)}
        />
      )}
    </>
  )
}
