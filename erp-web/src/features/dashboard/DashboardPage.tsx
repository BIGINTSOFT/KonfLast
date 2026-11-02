import { Cog, PackageCheck, TriangleAlert, Users } from 'lucide-react'
import type { LucideIcon } from 'lucide-react'
import { Link } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { RecordLink } from '../../components/RecordLink'
import { formatQuantity } from '../../utils/format'
import { BundleStatusBadge } from '../bundles/BundleBadges'
import { useLivePolling } from '../live/useLivePolling'
import { efficiencyTone, formatPercent } from '../productionTracking/trackingPresentation'
import { formatDuration } from '../workSessions/workSessionPresentation'
import { useGetLiveDashboardQuery } from './dashboardApi'
import type { LiveDashboard, LiveLine } from './dashboardApi'
import { formatRate, machineGlance } from './dashboardPresentation'

function Efficiency({ value }: { value: number | null }) {
  if (value === null) {
    return <span className="muted">—</span>
  }

  const tone = efficiencyTone(value)
  return (
    <span className={`badge badge--${tone.tone}`}>
      <span aria-hidden="true">{tone.icon}</span> {formatPercent(value)}
    </span>
  )
}

type StatTone = 'info' | 'ok' | 'warning' | 'danger'

function Stat({ label, value, detail, icon: Icon, tone }: { label: string; value: string; detail?: string; icon: LucideIcon; tone: StatTone }) {
  return (
    <article className={`card stat stat--${tone}`}>
      <span className="stat__icon"><Icon aria-hidden="true" /></span>
      <div className="stat__body">
        <h2 className="stat__label">{label}</h2>
        <p className="stat__value">{value}</p>
        {detail && <p className="muted stat__detail">{detail}</p>}
      </div>
    </article>
  )
}

function Summary({ dashboard }: { dashboard: LiveDashboard }) {
  const { summary } = dashboard
  return (
    <div className="cards cards--stats">
      <Stat
        label="Bugün üretilen"
        icon={PackageCheck}
        tone="info"
        value={formatQuantity(summary.produced)}
        detail={`Hedef ${summary.targetQuantity === null ? '—' : formatQuantity(summary.targetQuantity)} · verim ${formatPercent(summary.efficiencyPercent)}`}
      />
      <Stat
        label="Çalışan makine"
        icon={Cog}
        tone={summary.breakdownMachines > 0 ? 'warning' : 'ok'}
        value={`${summary.runningMachines} / ${summary.machines}`}
        detail={summary.breakdownMachines > 0 ? `${summary.breakdownMachines} makine arızalı` : 'Arızalı makine yok'}
      />
      <Stat
        label="Operatör"
        icon={Users}
        tone="info"
        value={`${summary.producing} / ${summary.operatorsPresent}`}
        detail={`üretiyor / mevcut · ${summary.waitingForBundle} demet bekliyor · ${summary.onBreak} molada`}
      />
      <Stat
        label="Bugün hatalı"
        icon={TriangleAlert}
        tone={summary.rejected > 0 ? 'danger' : 'ok'}
        value={formatQuantity(summary.rejected)}
        detail={`${summary.pendingReworkTasks} tamir işi bekliyor`}
      />
    </div>
  )
}

function LineCard({ line }: { line: LiveLine }) {
  const title = line.lineId === null ? line.name : `${line.lineCode} · ${line.name}`

  return (
    <article className="card line-card">
      <header className="line-card__header">
        <h2>{line.lineId === null ? title : <Link to={`/production-lines/${line.lineId}`}>{title}</Link>}</h2>
        <Efficiency value={line.efficiencyPercent} />
      </header>

      {line.plan ? (
        <label className="progress">
          <span>
            Model <strong>{line.plan.modelCode}</strong> ·{' '}
            <RecordLink to={`/production-orders/${line.plan.productionOrderId}`}>{line.plan.orderNo}</RecordLink>
            <br />
            Plan {formatQuantity(line.plan.plannedQuantity)} · banttan çıkan{' '}
            <strong>{formatQuantity(line.plan.producedQuantity)}</strong> (%{line.plan.completionPercent})
          </span>
          <progress max={100} value={line.plan.completionPercent} />
        </label>
      ) : (
        line.lineId !== null && <p className="muted">Şu an yürüyen plan yok.</p>
      )}

      <dl className="status-list">
        <div>
          <dt>Bugün üretilen / hedef</dt>
          <dd>
            {formatQuantity(line.produced)} / {line.targetQuantity === null ? '—' : formatQuantity(line.targetQuantity)}
          </dd>
        </div>
        <div>
          <dt>Gerçek / hedef hız</dt>
          <dd>{formatRate(line.producedPerHour)} · {formatRate(line.targetPerHour)}</dd>
        </div>
        <div>
          <dt>Hatalı</dt>
          <dd>{formatQuantity(line.rejected)}</dd>
        </div>
        <div>
          <dt>Arıza · demet bekleme · mola</dt>
          <dd>
            {formatDuration(line.breakdownMinutes)} · {formatDuration(line.waitingMinutes)} ·{' '}
            {formatDuration(line.breakMinutes)}
          </dd>
        </div>
      </dl>

      {line.machines.length === 0 ? (
        <p className="muted">Banta makine atanmamış.</p>
      ) : (
        <ul className="plain-list line-card__machines">
          {line.machines.map((machine) => {
            const glance = machineGlance(machine)
            return (
              <li key={machine.machineId}>
                <span>
                  <code>{machine.machineCode}</code>{' '}
                  <span className="muted">{machine.operationName ?? machine.machineName}</span>
                </span>
                <span className={`badge badge--${glance.tone}`}>
                  <span aria-hidden="true">{glance.icon}</span> {glance.label}
                </span>
                <span className="muted line-card__job">
                  {machine.operatorName ?? 'Operatör yok'}
                  {machine.bundleId !== null && (
                    <>
                      {' · '}
                      <RecordLink to={`/bundles/${machine.bundleId}`}>Demet {machine.bundleNo}</RecordLink>
                      {machine.remainingQuantity !== null && ` (kalan ${formatQuantity(machine.remainingQuantity)})`}
                    </>
                  )}
                </span>
              </li>
            )
          })}
        </ul>
      )}
    </article>
  )
}

/**
 * Canlı pano: bantlar, makineler, bugünkü üretim / hedef ve ara stok. Veri SignalR bildirimiyle yenilenir;
 * bağlantı yokken ekran düzenli aralıkla kendini yeniler. Tüm rakamları sunucu hesaplar.
 */
export function DashboardPage() {
  const pollingInterval = useLivePolling(15_000)
  const { data, error, isLoading } = useGetLiveDashboardQuery(undefined, { pollingInterval })

  return (
    <section className="page">
      <h1 className="visually-hidden">Canlı Pano</h1>

      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />

      {data && (
        <>
          <Summary dashboard={data} />

          <h2 className="section-title">Sahadaki demetler</h2>
          {data.bundleStatuses.length === 0 ? (
            <p className="muted">Sahada demet yok.</p>
          ) : (
            <ul className="chips chips--plain">
              {data.bundleStatuses.map((row) => (
                <li key={row.status}>
                  <BundleStatusBadge status={row.status} /> {row.bundleCount} demet · {formatQuantity(row.quantity)} adet
                </li>
              ))}
            </ul>
          )}

          <h2 className="section-title">Bantlar</h2>
          {data.lines.length === 0 ? (
            <p className="muted">Tanımlı bant ya da aktif makine yok.</p>
          ) : (
            <div className="cards cards--lines">
              {data.lines.map((line) => (
                <LineCard key={line.lineId ?? 0} line={line} />
              ))}
            </div>
          )}

          <h2 className="section-title">Ara stok (operasyon önünde bekleyenler)</h2>
          <div className="table-wrap">
            <table className="table table--compact">
              <thead>
                <tr>
                  <th>Operasyon</th>
                  <th className="table__number">Bekleyen demet</th>
                  <th className="table__number">Bekleyen adet</th>
                  <th className="table__number">İşlemdeki adet</th>
                  <th className="table__number">Yarım kalan adet</th>
                </tr>
              </thead>
              <tbody>
                {data.wip.map((operation) => (
                  <tr key={operation.operationId}>
                    <td><code>{operation.operationCode}</code> {operation.operationName}</td>
                    <td className="table__number">{operation.readyBundles}</td>
                    <td className="table__number">{formatQuantity(operation.readyQuantity)}</td>
                    <td className="table__number">{formatQuantity(operation.inProgressQuantity)}</td>
                    <td className="table__number">{formatQuantity(operation.pausedQuantity)}</td>
                  </tr>
                ))}
                {data.wip.length === 0 && (
                  <tr>
                    <td colSpan={5} className="table__empty">Sahada bekleyen ya da işlenen demet yok.</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </>
      )}
    </section>
  )
}
