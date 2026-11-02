import { useState } from 'react'
import { Link } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { RecordLink } from '../../components/RecordLink'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDate, formatDateTime, formatQuantity, todayIsoDate } from '../../utils/format'
import { dayRangeToUtc, formatPercent } from '../productionTracking/trackingPresentation'
import { DemandSection } from './DemandSection'
import { SuggestionDialog } from './SuggestionDialog'
import { useGetLineLoadQuery, useGetPlanningOrdersQuery } from './planningApi'
import type { LineLoad, PlanningOrder } from './planningApi'
import { dueText, formatHours, horizonDates, loadStatusPresentation, riskPresentation } from './planningPresentation'

const HORIZON_DAYS = 14

function OrdersSection({ canPlan }: { canPlan: boolean }) {
  const { data, error, isLoading } = useGetPlanningOrdersQuery()
  const [suggestingFor, setSuggestingFor] = useState<PlanningOrder | null>(null)

  return (
    <>
      <h2 className="section-title">Açık siparişler</h2>
      <p className="muted">Onaylı ve üretimdeki siparişler; en riskli ve termini en yakın olan üsttedir.</p>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {data && (
        <div className="table-wrap">
          <table className="table">
            <thead>
              <tr>
                <th>Sipariş</th>
                <th>Model</th>
                <th>Termin</th>
                <th>Durum</th>
                <th className="table__number">Sipariş</th>
                <th className="table__number">Biten</th>
                <th className="table__number">Kalan</th>
                <th className="table__number">Planlanmamış</th>
                <th className="table__number">Kalan iş</th>
                <th aria-label="İşlemler" />
              </tr>
            </thead>
            <tbody>
              {data.map((order) => {
                const risk = riskPresentation[order.risk]
                return (
                  <tr key={order.id}>
                    <td>
                      <RecordLink to={`/production-orders/${order.id}`}>{order.orderNo}</RecordLink>
                      <div className="muted">{order.customerName}</div>
                    </td>
                    <td>{order.modelCode} <span className="muted">{order.productName}</span></td>
                    <td>
                      {formatDate(order.dueDate)}
                      <div className="muted">{dueText(order.daysToDue)}</div>
                    </td>
                    <td>
                      <span className={`badge badge--${risk.tone}`}><span aria-hidden="true">{risk.icon}</span> {risk.label}</span>
                      {order.lastPlannedEnd && <div className="muted">Son plan bitişi {formatDateTime(order.lastPlannedEnd)}</div>}
                    </td>
                    <td className="table__number">{formatQuantity(order.ordered)}</td>
                    <td className="table__number">{formatQuantity(order.finished)}</td>
                    <td className="table__number">{formatQuantity(order.remaining)}</td>
                    <td className="table__number"><strong>{formatQuantity(order.unplanned)}</strong></td>
                    <td className="table__number">{order.hasRoute ? formatHours(order.remainingWorkHours) : 'Rota yok'}</td>
                    <td className="table__actions">
                      <button
                        type="button"
                        className="button"
                        disabled={order.unplanned <= 0 || !order.hasRoute}
                        title={
                          !order.hasRoute
                            ? 'Modelin aktif rotası yok.'
                            : order.unplanned <= 0 ? 'Siparişin tamamı planlanmış.' : undefined
                        }
                        onClick={() => setSuggestingFor(order)}
                      >
                        Bant öner
                      </button>
                    </td>
                  </tr>
                )
              })}
              {data.length === 0 && (
                <tr>
                  <td colSpan={10} className="table__empty">Açık sipariş yok.</td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}
      {suggestingFor && (
        <SuggestionDialog
          key={suggestingFor.id}
          order={suggestingFor}
          canPlan={canPlan}
          onClose={() => setSuggestingFor(null)}
        />
      )}
    </>
  )
}

function LineLoadCard({ line }: { line: LineLoad }) {
  return (
    <article className="card line-card">
      <header className="line-card__header">
        <h2><Link to={`/production-lines/${line.lineId}`}>{line.lineCode} · {line.name}</Link></h2>
        <span className="muted">{line.machines} makine</span>
      </header>
      <label className="progress">
        <span>
          Doluluk <strong>{formatPercent(line.utilizationPercent)}</strong> · {formatHours(line.bookedHours)} planlı /{' '}
          {formatHours(line.workingHours)} çalışma · {formatHours(line.freeHours)} boş
        </span>
        <progress max={100} value={Math.min(line.utilizationPercent ?? 0, 100)} />
      </label>

      {line.plans.length === 0 ? (
        <p className="muted">Bu aralıkta plan yok.</p>
      ) : (
        <ul className="plain-list line-card__machines">
          {line.plans.map((plan) => {
            const status = loadStatusPresentation[plan.status]
            return (
              <li key={plan.planId}>
                <span>
                  <RecordLink to={`/production-orders/${plan.productionOrderId}`}>{plan.orderNo}</RecordLink>{' '}
                  <span className="muted">{plan.modelCode} · {formatQuantity(plan.plannedQuantity)} adet</span>
                </span>
                <span className={`badge badge--${status.tone}`}><span aria-hidden="true">{status.icon}</span> {status.label}</span>
                <span className="muted line-card__job">
                  {formatDateTime(plan.plannedStart)} – {formatDateTime(plan.plannedEnd)}
                  {plan.requiredHours !== null &&
                    ` · gereken ${formatHours(plan.requiredHours)} / mevcut ${formatHours(plan.availableHours)}`}
                  {plan.status === 'NO_CAPACITY' && plan.bottleneckOperationName && ` · eksik: ${plan.bottleneckOperationName}`}
                  {plan.endsAfterDueDate && ` · termin ${formatDate(plan.dueDate)} aşılıyor`}
                </span>
              </li>
            )
          })}
        </ul>
      )}
    </article>
  )
}

function LineLoadSection() {
  const [{ fromDate, toDate }, setDates] = useState(() => horizonDates(todayIsoDate(), HORIZON_DAYS))
  const range = dayRangeToUtc(fromDate, toDate)
  const { data, error, isLoading } = useGetLineLoadQuery(range ?? { from: '', to: '' }, { skip: !range })

  return (
    <>
      <h2 className="section-title">Bant yükü</h2>
      <div className="toolbar toolbar--filters">
        <label>
          Başlangıç
          <input type="date" value={fromDate} max={toDate} onChange={(event) => setDates({ fromDate: event.target.value, toDate })} />
        </label>
        <label>
          Bitiş
          <input type="date" value={toDate} min={fromDate} onChange={(event) => setDates({ fromDate, toDate: event.target.value })} />
        </label>
      </div>
      {!range && <p className="alert alert--error" role="alert">Bitiş tarihi başlangıçtan önce olamaz.</p>}
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />

      {range && data && (
        <>
          {data.calendarAssumed && (
            <p className="alert alert--info">
              Vardiya tanımlı değil; çalışma süresi gün boyu çalışma varsayımıyla hesaplandı. Gerçekçi sonuç için
              vardiyaları tanımlayın.
            </p>
          )}
          {data.lines.length === 0 ? (
            <p className="muted">Aktif bant yok.</p>
          ) : (
            <div className="cards cards--lines">
              {data.lines.map((line) => (
                <LineLoadCard key={line.lineId} line={line} />
              ))}
            </div>
          )}
        </>
      )}
    </>
  )
}

/**
 * Planlama: açık siparişlerin termin riski, bantların doluluğu ve planların pencerelerine sığıp sığmadığı.
 * Süre ve risk hesapları sunucudadır; plan, öneriden tek tıkla ya da bant sayfasından oluşturulur.
 */
export function PlanningPage() {
  const canPlan = usePermission(Permissions.LinePlanManage)

  return (
    <section className="page">
      <h1 className="visually-hidden">Üretim Planı</h1>
      <DemandSection />
      <OrdersSection canPlan={canPlan} />
      <LineLoadSection />
    </section>
  )
}
