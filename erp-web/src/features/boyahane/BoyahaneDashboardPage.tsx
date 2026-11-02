import { CheckCheck, CircleDashed, Factory, TriangleAlert } from 'lucide-react'
import type { LucideIcon } from 'lucide-react'
import { Link } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { formatDate, formatDateTime, formatQuantity } from '../../utils/format'
import { useLivePolling } from '../live/useLivePolling'
import { dyeMachineStatusLabels } from './boyahanePresentation'
import { useGetBoyahaneDashboardQuery } from './boyahaneInsightsApi'

function Stat({ label, value, icon: Icon, tone }: { label: string; value: number; icon: LucideIcon; tone: string }) {
  return (
    <article className={`card stat stat--${tone}`}>
      <span className="stat__icon"><Icon aria-hidden="true" /></span>
      <div className="stat__body">
        <h2 className="stat__label">{label}</h2>
        <p className="stat__value">{value}</p>
      </div>
    </article>
  )
}

const statusTone: Record<string, string> = { AVAILABLE: 'ok', BREAKDOWN: 'danger', MAINTENANCE: 'warning' }

/** Canlı pano: makinelerin durumu, üzerindeki parti ve sırada bekleyenler. */
export function BoyahaneDashboardPage() {
  const pollingInterval = useLivePolling()
  const { data, error, isLoading } = useGetBoyahaneDashboardQuery(undefined, { pollingInterval })

  return (
    <section className="page">
      <h1 className="visually-hidden">Canlı Pano</h1>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {data && (
        <>
          <div className="cards cards--stats">
            <Stat label="Planlanan parti" value={data.planned} icon={CircleDashed} tone="info" />
            <Stat label="İşlemdeki parti" value={data.inProgress} icon={Factory} tone="info" />
            <Stat label="Bugün tamamlanan" value={data.completedToday} icon={CheckCheck} tone="ok" />
            <Stat label="Arızalı makine" value={data.machinesDown} icon={TriangleAlert} tone={data.machinesDown > 0 ? 'danger' : 'ok'} />
          </div>

          <h2 className="section-title">Makineler</h2>
          <div className="table-wrap">
            <table className="table">
              <thead><tr><th>Makine</th><th>Durum</th><th>Parti</th><th>Operasyon</th><th>Operatör</th><th>Başlangıç</th></tr></thead>
              <tbody>
                {data.machines.map((machine) => (
                  <tr key={machine.id}>
                    <td><code>{machine.code}</code> {machine.name}</td>
                    <td><span className={`badge badge--${statusTone[machine.status]}`}>{machine.batchId && machine.status === 'AVAILABLE' ? 'Çalışıyor' : dyeMachineStatusLabels[machine.status]}</span></td>
                    <td>{machine.batchId ? <Link to={`/boyahane/batches/${machine.batchId}`}>{machine.batchNo}</Link> : '—'}</td>
                    <td>{machine.operationName ?? '—'}</td>
                    <td>{machine.operatorName ?? '—'}</td>
                    <td>{machine.startedAt ? formatDateTime(machine.startedAt) : '—'}</td>
                  </tr>
                ))}
                {data.machines.length === 0 && <tr><td colSpan={6} className="table__empty">Makine tanımlı değil.</td></tr>}
              </tbody>
            </table>
          </div>

          <h2 className="section-title">Sırada bekleyen partiler</h2>
          <div className="table-wrap">
            <table className="table">
              <thead><tr><th>Parti</th><th>Makine</th><th>Plan</th><th>Sıradaki operasyon</th><th className="table__number">Kilo</th></tr></thead>
              <tbody>
                {data.waiting.map((batch) => (
                  <tr key={batch.id}>
                    <td><Link to={`/boyahane/batches/${batch.id}`}>{batch.batchNo}</Link></td>
                    <td>{batch.machineCode}</td>
                    <td>{batch.plannedDate ? formatDate(batch.plannedDate) : '—'}</td>
                    <td>{batch.nextOperation ?? '—'}</td>
                    <td className="table__number">{formatQuantity(batch.kilograms)}</td>
                  </tr>
                ))}
                {data.waiting.length === 0 && <tr><td colSpan={5} className="table__empty">Bekleyen parti yok.</td></tr>}
              </tbody>
            </table>
          </div>
        </>
      )}
    </section>
  )
}
