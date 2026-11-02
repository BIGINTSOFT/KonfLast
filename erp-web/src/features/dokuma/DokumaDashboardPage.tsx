import { Factory, Grid3x3, PackageCheck, ScanSearch, TriangleAlert, Truck } from 'lucide-react'
import { Link } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { StatCard } from '../../components/StatCard'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDate, formatDateTime, formatQuantity } from '../../utils/format'
import { useLivePolling } from '../live/useLivePolling'
import { useGetDokumaDashboardQuery } from './dokumaDashboardApi'
import { dokumaStopCategoryLabels } from './dokumaPresentation'

/** Canlı pano: tezgâh durumu, bugünkü üretim, bekleyen işler ve dikkat isteyen kayıtlar. Kartlar yetkiliyse ilgili ekrana götürür. */
export function DokumaDashboardPage() {
  const pollingInterval = useLivePolling()
  const { data, error, isLoading } = useGetDokumaDashboardQuery(undefined, { pollingInterval })
  const canWeave = usePermission(Permissions.DokumaWeavingView)
  const canWarp = usePermission(Permissions.DokumaWarpView)
  const canInspect = usePermission(Permissions.DokumaInspectionView)
  const canPack = usePermission(Permissions.DokumaPackingView)
  const canOrder = usePermission(Permissions.DokumaOrderView)

  return (
    <section className="page">
      <h1 className="visually-hidden">Canlı Pano</h1>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {data && (
        <>
          <div className="cards cards--stats cards--stats-compact">
            <StatCard label="Çalışan tezgâh" icon={Factory} tone={data.breakdownLooms > 0 ? 'warning' : 'ok'} value={`${data.runningLooms} / ${data.looms}`}
              detail={`${data.idleLooms} boşta · ${data.stoppedLooms} duruşta · ${data.breakdownLooms} arızalı`} to={canWeave ? '/dokuma/weaving' : undefined} />
            <StatCard label="Bugün dokunan" icon={Grid3x3} tone="info" value={`${formatQuantity(data.wovenTodayMeters)} m`}
              detail={`${data.rollsToday} top`} to={canWeave ? '/dokuma/rolls' : undefined} />
            <StatCard label="Tezgâha hazır levent" icon={PackageCheck} tone="info" value={String(data.readyBeams)} to={canWarp ? '/dokuma/beams' : undefined} />
            <StatCard label="Kontrol bekleyen top" icon={ScanSearch} tone={data.pendingInspectionRolls > 0 ? 'warning' : 'ok'} value={String(data.pendingInspectionRolls)} to={canInspect ? '/dokuma/inspection' : undefined} />
            <StatCard label="Sevk bekleyen çeki" icon={Truck} tone="info" value={String(data.packingListsToShip)} to={canPack ? '/dokuma/packing-lists' : undefined} />
            <StatCard label="Geciken sipariş" icon={TriangleAlert} tone={data.lateOrderCount > 0 ? 'danger' : 'ok'} value={String(data.lateOrderCount)} to={canOrder ? '/dokuma/orders' : undefined} />
          </div>

          <h2 className="section-title">Duran tezgâhlar</h2>
          <div className="table-wrap">
            <table className="table table--compact">
              <thead><tr><th>Tezgâh</th><th>Tür</th><th>Neden</th><th>Levent</th><th>Başlangıç</th></tr></thead>
              <tbody>
                {data.stops.map((stop) => (
                  <tr key={stop.loomCode}>
                    <td><code>{stop.loomCode}</code> {stop.loomName}</td>
                    <td><span className={`badge badge--${stop.category === 'BREAKDOWN' ? 'danger' : 'warning'}`}>{dokumaStopCategoryLabels[stop.category]}</span></td>
                    <td>{stop.reasonName}</td>
                    <td>{stop.beamNo ?? '—'}</td>
                    <td>{formatDateTime(stop.since)}</td>
                  </tr>
                ))}
                {data.stops.length === 0 && <tr><td colSpan={5} className="table__empty">Duran tezgâh yok.</td></tr>}
              </tbody>
            </table>
          </div>

          <h2 className="section-title">Geciken siparişler</h2>
          <div className="table-wrap">
            <table className="table table--compact">
              <thead><tr><th>Sipariş</th><th>Müşteri</th><th>Termin</th><th className="table__number">Sipariş (m)</th><th className="table__number">Sevk (m)</th><th className="table__number">Kalan (m)</th></tr></thead>
              <tbody>
                {data.lateOrders.map((order) => (
                  <tr key={order.orderId}>
                    <td>{canOrder ? <Link to={`/dokuma/orders/${order.orderId}`}><code>{order.orderNo}</code></Link> : <code>{order.orderNo}</code>}</td>
                    <td>{order.customerName ?? <span className="muted">Stoğa üretim</span>}</td>
                    <td>{formatDate(order.dueDate)}</td>
                    <td className="table__number">{formatQuantity(order.orderedMeters)}</td>
                    <td className="table__number">{formatQuantity(order.shippedMeters)}</td>
                    <td className="table__number">{formatQuantity(order.orderedMeters - order.shippedMeters)}</td>
                  </tr>
                ))}
                {data.lateOrders.length === 0 && <tr><td colSpan={6} className="table__empty">Geciken sipariş yok.</td></tr>}
              </tbody>
            </table>
          </div>
          {data.lateOrderCount > data.lateOrders.length && <p className="muted">En eski {data.lateOrders.length} sipariş gösteriliyor; toplam {data.lateOrderCount}.</p>}
        </>
      )}
    </section>
  )
}
