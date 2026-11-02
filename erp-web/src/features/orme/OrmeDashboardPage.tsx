import { Factory, PackageCheck, ScanSearch, TriangleAlert, Truck, Waves } from 'lucide-react'
import { Link } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { StatCard } from '../../components/StatCard'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDate, formatDateTime, formatQuantity } from '../../utils/format'
import { useLivePolling } from '../live/useLivePolling'
import { useGetOrmeDashboardQuery } from './ormeDashboardApi'
import { ormeStopCategoryLabels } from './ormePresentation'

/** Canlı pano: makine durumu, bugünkü üretim, bekleyen işler ve dikkat isteyen kayıtlar. Kartlar yetkiliyse ilgili ekrana götürür. */
export function OrmeDashboardPage() {
  const pollingInterval = useLivePolling()
  const { data, error, isLoading } = useGetOrmeDashboardQuery(undefined, { pollingInterval })
  const canKnit = usePermission(Permissions.OrmeKnittingView)
  const canInspect = usePermission(Permissions.OrmeInspectionView)
  const canPack = usePermission(Permissions.OrmePackingView)
  const canOrder = usePermission(Permissions.OrmeOrderView)

  return (
    <section className="page">
      <h1 className="visually-hidden">Canlı Pano</h1>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {data && (
        <>
          <div className="cards cards--stats cards--stats-compact">
            <StatCard label="Çalışan makine" icon={Factory} tone={data.breakdownMachines > 0 ? 'warning' : 'ok'} value={`${data.runningMachines} / ${data.machines}`}
              detail={`${data.idleMachines} boşta · ${data.stoppedMachines} duruşta · ${data.breakdownMachines} arızalı`} to={canKnit ? '/orme/machine-board' : undefined} />
            <StatCard label="Bugün örülen" icon={Waves} tone="info" value={`${formatQuantity(data.knittedTodayKilograms)} kg`}
              detail={`${data.rollsToday} top`} to={canKnit ? '/orme/rolls' : undefined} />
            <StatCard label="Açık örme emri" icon={PackageCheck} tone="info" value={String(data.openKnitOrders)} to={canKnit ? '/orme/knit-orders' : undefined} />
            <StatCard label="Kontrol bekleyen top" icon={ScanSearch} tone={data.pendingInspectionRolls > 0 ? 'warning' : 'ok'} value={String(data.pendingInspectionRolls)} to={canInspect ? '/orme/inspection' : undefined} />
            <StatCard label="Sevk bekleyen çeki" icon={Truck} tone="info" value={String(data.packingListsToShip)} to={canPack ? '/orme/packing-lists' : undefined} />
            <StatCard label="Geciken sipariş" icon={TriangleAlert} tone={data.lateOrderCount > 0 ? 'danger' : 'ok'} value={String(data.lateOrderCount)} to={canOrder ? '/orme/orders' : undefined} />
          </div>

          <h2 className="section-title">Duran makineler</h2>
          <div className="table-wrap">
            <table className="table table--compact">
              <thead><tr><th>Makine</th><th>Tür</th><th>Neden</th><th>Emir</th><th>Başlangıç</th></tr></thead>
              <tbody>
                {data.stops.map((stop) => (
                  <tr key={stop.machineCode}>
                    <td><code>{stop.machineCode}</code> {stop.machineName}</td>
                    <td><span className={`badge badge--${stop.category === 'BREAKDOWN' ? 'danger' : 'warning'}`}>{ormeStopCategoryLabels[stop.category]}</span></td>
                    <td>{stop.reasonName}</td>
                    <td>{stop.knitOrderNo ?? '—'}</td>
                    <td>{formatDateTime(stop.since)}</td>
                  </tr>
                ))}
                {data.stops.length === 0 && <tr><td colSpan={5} className="table__empty">Duran makine yok.</td></tr>}
              </tbody>
            </table>
          </div>

          <h2 className="section-title">Geciken siparişler</h2>
          <div className="table-wrap">
            <table className="table table--compact">
              <thead><tr><th>Sipariş</th><th>Müşteri</th><th>Termin</th><th className="table__number">Sipariş (kg)</th><th className="table__number">Sevk (kg)</th><th className="table__number">Kalan (kg)</th></tr></thead>
              <tbody>
                {data.lateOrders.map((order) => (
                  <tr key={order.orderId}>
                    <td>{canOrder ? <Link to={`/orme/orders/${order.orderId}`}><code>{order.orderNo}</code></Link> : <code>{order.orderNo}</code>}</td>
                    <td>{order.customerName ?? <span className="muted">Stoğa üretim</span>}</td>
                    <td>{formatDate(order.dueDate)}</td>
                    <td className="table__number">{formatQuantity(order.orderedKilograms)}</td>
                    <td className="table__number">{formatQuantity(order.shippedKilograms)}</td>
                    <td className="table__number">{formatQuantity(order.orderedKilograms - order.shippedKilograms)}</td>
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
