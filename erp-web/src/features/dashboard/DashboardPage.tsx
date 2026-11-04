import { ChevronRight, Cog, PackageCheck, TriangleAlert, Users } from 'lucide-react'
import type { LucideIcon } from 'lucide-react'
import { Fragment, useState } from 'react'
import { Link } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { RecordLink } from '../../components/RecordLink'
import { formatQuantity } from '../../utils/format'
import { BundleStatusBadge } from '../bundles/BundleBadges'
import { useLivePolling } from '../live/useLivePolling'
import type { MachineActivity } from '../productionTracking/trackingApi'
import { efficiencyTone, formatPercent } from '../productionTracking/trackingPresentation'
import { formatDuration } from '../workSessions/workSessionPresentation'
import { useGetLiveDashboardQuery } from './dashboardApi'
import type { LiveDashboard, LiveLine } from './dashboardApi'
import { formatRate, machineGlance } from './dashboardPresentation'

function Efficiency({ value }: { value: number | null }) {
  if (value === null) {
    return <span className="muted">—</span>
  }

  return <span className={`badge badge--${efficiencyTone(value).tone}`}>{formatPercent(value)}</span>
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

/** Makinenin pano grubu: tek bakışta durumun tonundan; oturumu olmayan / çevrimdışı makine "boşta"dır. */
type MachineGroup = 'ok' | 'info' | 'warning' | 'danger' | 'idle'

const groupOf = (machine: MachineActivity): MachineGroup => {
  const tone = machineGlance(machine).tone
  return tone === 'ok' || tone === 'info' || tone === 'warning' || tone === 'danger' ? tone : 'idle'
}

const groupLabels: Record<MachineGroup, string> = {
  ok: 'üretiyor', info: 'hazır / molada', warning: 'bekliyor', danger: 'arızalı', idle: 'boşta',
}

/** Bant satırındaki makine özeti: yalnızca sıfır olmayan gruplar, renkli sayılarla. */
function MachineCounts({ machines }: { machines: MachineActivity[] }) {
  const counts = new Map<MachineGroup, number>()
  machines.forEach((machine) => counts.set(groupOf(machine), (counts.get(groupOf(machine)) ?? 0) + 1))
  const order: MachineGroup[] = ['ok', 'info', 'warning', 'danger', 'idle']

  return (
    <span className="machine-counts">
      {order.filter((group) => counts.has(group)).map((group) => (
        <span key={group} className={`machine-counts__item machine-counts__item--${group}`} title={groupLabels[group]}>
          {counts.get(group)} {groupLabels[group]}
        </span>
      ))}
    </span>
  )
}

function MachineRow({ machine }: { machine: MachineActivity }) {
  const glance = machineGlance(machine)
  return (
    <tr>
      <td><code>{machine.machineCode}</code> <span className="muted">{machine.operationName ?? machine.machineName}</span></td>
      <td><span className={`badge badge--${glance.tone}`}>{glance.label}</span></td>
      <td>{machine.operatorName ?? <span className="muted">—</span>}</td>
      <td>
        {machine.bundleId !== null ? (
          <>
            <RecordLink to={`/bundles/${machine.bundleId}`}>Demet {machine.bundleNo}</RecordLink>
            {machine.remainingQuantity !== null && <span className="muted"> · kalan {formatQuantity(machine.remainingQuantity)}</span>}
          </>
        ) : <span className="muted">—</span>}
      </td>
    </tr>
  )
}

/** Bantın makineleri: çalışanlar listelenir, boştakiler tek satırda toplanır (istenirse gösterilir). */
function LineMachines({ line }: { line: LiveLine }) {
  const [showIdle, setShowIdle] = useState(false)
  const active = line.machines.filter((machine) => groupOf(machine) !== 'idle')
  const idle = line.machines.filter((machine) => groupOf(machine) === 'idle')

  if (line.machines.length === 0) {
    return <p className="muted">Banta makine atanmamış.</p>
  }

  return (
    <table className="table table--compact line-machines">
      <thead>
        <tr>
          <th>Makine</th>
          <th>Durum</th>
          <th>Operatör</th>
          <th>İş</th>
        </tr>
      </thead>
      <tbody>
        {active.map((machine) => <MachineRow key={machine.machineId} machine={machine} />)}
        {active.length === 0 && (
          <tr><td colSpan={4} className="muted">Bu bantta şu an çalışan makine yok.</td></tr>
        )}
        {idle.length > 0 && (
          <tr>
            <td colSpan={4}>
              <button type="button" className="link-button" onClick={() => setShowIdle(!showIdle)}>
                {showIdle ? 'Boştaki makineleri gizle' : `${idle.length} makine boşta (oturum yok / çevrimdışı) — göster`}
              </button>
            </td>
          </tr>
        )}
        {showIdle && idle.map((machine) => <MachineRow key={machine.machineId} machine={machine} />)}
      </tbody>
    </table>
  )
}

/** Bantlar tek tabloda: her bant bir satır; satıra tıklayınca makineleri açılır. */
function LinesTable({ lines }: { lines: LiveLine[] }) {
  const [open, setOpen] = useState<number | null>(null)

  return (
    <div className="table-wrap">
      <table className="table lines-table">
        <thead>
          <tr>
            <th aria-label="Aç / kapat" />
            <th>Bant</th>
            <th>Yürüyen plan</th>
            <th>Üretilen / hedef</th>
            <th className="table__number">Gerçek / hedef hız</th>
            <th>Verim</th>
            <th className="table__number">Hatalı</th>
            <th className="table__number" title="Arıza · demet bekleme · mola">Kayıp (dk)</th>
            <th>Makineler</th>
          </tr>
        </thead>
        <tbody>
          {lines.map((line) => {
            const key = line.lineId ?? 0
            const expanded = open === key
            const ratio = line.targetQuantity ? Math.min(100, Math.round((line.produced / line.targetQuantity) * 100)) : null
            return (
              <Fragment key={key}>
                <tr className={expanded ? 'stock-row table__row--expanded' : 'stock-row'} onClick={() => setOpen(expanded ? null : key)}>
                  <td className="lines-table__toggle">
                    <ChevronRight aria-hidden="true" className={expanded ? 'lines-table__chevron lines-table__chevron--open' : 'lines-table__chevron'} />
                  </td>
                  <td>
                    {line.lineId === null ? (
                      <span className="muted">{line.name}</span>
                    ) : (
                      <Link to={`/production-lines/${line.lineId}`} onClick={(event) => event.stopPropagation()}>
                        <code>{line.lineCode}</code> {line.name}
                      </Link>
                    )}
                  </td>
                  <td>
                    {line.plan ? (
                      <>
                        <strong>{line.plan.modelCode}</strong>{' '}
                        <RecordLink to={`/production-orders/${line.plan.productionOrderId}`}>{line.plan.orderNo}</RecordLink>
                        <div className="muted">{formatQuantity(line.plan.producedQuantity)} / {formatQuantity(line.plan.plannedQuantity)} (%{line.plan.completionPercent})</div>
                      </>
                    ) : <span className="muted">—</span>}
                  </td>
                  <td>
                    <div className="lines-table__progress">
                      <span>{formatQuantity(line.produced)} / {line.targetQuantity === null ? '—' : formatQuantity(line.targetQuantity)}</span>
                      {ratio !== null && <progress max={100} value={ratio} aria-label="Hedefe göre" />}
                    </div>
                  </td>
                  <td className="table__number">{formatRate(line.producedPerHour)} · {formatRate(line.targetPerHour)}</td>
                  <td><Efficiency value={line.efficiencyPercent} /></td>
                  <td className="table__number">{formatQuantity(line.rejected)}</td>
                  <td className="table__number" title="Arıza · demet bekleme · mola">
                    {formatDuration(line.breakdownMinutes)} · {formatDuration(line.waitingMinutes)} · {formatDuration(line.breakMinutes)}
                  </td>
                  <td><MachineCounts machines={line.machines} /></td>
                </tr>
                {expanded && (
                  <tr className="table__detail-row">
                    <td colSpan={9}><LineMachines line={line} /></td>
                  </tr>
                )}
              </Fragment>
            )
          })}
        </tbody>
      </table>
    </div>
  )
}

/** Dikkat isteyenler: arızalı makineler, demet bekleyen ve ara veren operatörler. */
function Attention({ lines }: { lines: LiveLine[] }) {
  const items = lines
    .flatMap((line) => line.machines.map((machine) => ({ line, machine, group: groupOf(machine) })))
    .filter((item) => item.group === 'danger' || item.group === 'warning')
    .sort((a, b) => (a.group === b.group ? 0 : a.group === 'danger' ? -1 : 1))

  return (
    <article className="card dashboard-panel">
      <h2>Dikkat isteyenler</h2>
      {items.length === 0 ? (
        <p className="muted">Arızalı makine, demet bekleyen ya da ara veren operatör yok.</p>
      ) : (
        <table className="table table--compact">
          <tbody>
            {items.map(({ line, machine }) => (
              <tr key={machine.machineId}>
                <td><span className={`badge badge--${machineGlance(machine).tone}`}>{machineGlance(machine).label}</span></td>
                <td><code>{machine.machineCode}</code> <span className="muted">{machine.operationName ?? machine.machineName}</span></td>
                <td className="muted">{line.lineId === null ? 'Bantsız' : line.lineCode}</td>
                <td>{machine.operatorName ?? <span className="muted">—</span>}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </article>
  )
}

function FloorBundles({ dashboard }: { dashboard: LiveDashboard }) {
  return (
    <article className="card dashboard-panel">
      <h2>Sahadaki demetler</h2>
      {dashboard.bundleStatuses.length === 0 ? (
        <p className="muted">Sahada demet yok.</p>
      ) : (
        <table className="table table--compact">
          <tbody>
            {dashboard.bundleStatuses.map((row) => (
              <tr key={row.status}>
                <td><BundleStatusBadge status={row.status} /></td>
                <td className="table__number">{row.bundleCount} demet</td>
                <td className="table__number">{formatQuantity(row.quantity)} adet</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </article>
  )
}

/**
 * Canlı pano: üstte günün özeti, bantlar tek tabloda (makineler satır açılınca), dikkat isteyen makineler ve
 * sahadaki demetler, en altta ara stok. Veri SignalR bildirimiyle yenilenir; bağlantı yokken ekran düzenli
 * aralıkla kendini yeniler. Tüm rakamları sunucu hesaplar.
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

          <h2 className="section-title">Bantlar</h2>
          {data.lines.length === 0 ? (
            <p className="muted">Tanımlı bant ya da aktif makine yok.</p>
          ) : (
            <LinesTable lines={data.lines} />
          )}

          <div className="dashboard-panels">
            <Attention lines={data.lines} />
            <FloorBundles dashboard={data} />
          </div>

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
