import { useState } from 'react'
import { ErrorAlert } from '../../components/FormControls'
import { RecordLink } from '../../components/RecordLink'
import { Pagination } from '../../components/Pagination'
import { formatDateTime, formatQuantity, todayIsoDate } from '../../utils/format'
import { OperationStatusBadge } from '../bundles/BundleBadges'
import { useLivePolling } from '../live/useLivePolling'
import { MachineStatusBadge } from '../masterData/cells'
import {
  useGetMachineActivityQuery,
  useGetOutputQuery,
  useGetWipBundlesQuery,
  useGetWipQuery,
} from './trackingApi'
import type { OutputGroupBy, WipOperation } from './trackingApi'
import { dayRangeToUtc, groupByLabels, operatorStatePresentation } from './trackingPresentation'

const FALLBACK_REFRESH_MS = 30_000
const PAGE_SIZE = 20

function MachinesSection() {
  const pollingInterval = useLivePolling(FALLBACK_REFRESH_MS)
  const { data, error, isLoading } = useGetMachineActivityQuery(undefined, { pollingInterval })

  return (
    <>
      <h2 className="section-title">Makineler</h2>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {data && (
        <div className="table-wrap">
          <table className="table">
            <thead>
              <tr>
                <th>Makine</th>
                <th>Bant</th>
                <th>Operasyon</th>
                <th>Durum</th>
                <th>Operatör</th>
                <th>Operatör durumu</th>
                <th>Süren iş</th>
                <th className="table__number">Kalan</th>
                <th>İş başlangıcı</th>
              </tr>
            </thead>
            <tbody>
              {data.map((machine) => (
                <tr key={machine.machineId}>
                  <td><code>{machine.machineCode}</code> <span className="muted">{machine.machineName}</span></td>
                  <td>{machine.lineCode ?? '—'}</td>
                  <td>{machine.operationName ?? '—'}</td>
                  <td><MachineStatusBadge status={machine.status} /></td>
                  <td>{machine.operatorName ?? '—'}</td>
                  <td>
                    <span className={`badge badge--${operatorStatePresentation[machine.operatorState].tone}`}>
                      <span aria-hidden="true">{operatorStatePresentation[machine.operatorState].icon}</span>{' '}
                      {operatorStatePresentation[machine.operatorState].label}
                    </span>
                  </td>
                  <td>
                    {machine.bundleId ? (
                      <RecordLink to={`/bundles/${machine.bundleId}`}>Demet {machine.bundleNo}</RecordLink>
                    ) : (
                      '—'
                    )}
                  </td>
                  <td className="table__number">
                    {machine.remainingQuantity === null ? '—' : formatQuantity(machine.remainingQuantity)}
                  </td>
                  <td>{formatDateTime(machine.jobStartedAt)}</td>
                </tr>
              ))}
              {data.length === 0 && (
                <tr>
                  <td colSpan={9} className="table__empty">Aktif makine yok.</td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </>
  )
}

function WipBundles({ operation }: { operation: WipOperation }) {
  const pollingInterval = useLivePolling(FALLBACK_REFRESH_MS)
  const [page, setPage] = useState(1)
  const { data, error } = useGetWipBundlesQuery(
    { operationId: operation.operationId, page, pageSize: PAGE_SIZE },
    { pollingInterval },
  )

  return (
    <>
      <h3 className="section-title">{operation.operationName} operasyonundaki demetler</h3>
      <ErrorAlert error={error} />
      {data && (
        <>
          <div className="table-wrap">
            <table className="table table--compact">
              <thead>
                <tr>
                  <th>Demet</th>
                  <th>Sipariş</th>
                  <th>Model</th>
                  <th>Renk / beden</th>
                  <th>Durum</th>
                  <th className="table__number">Kalan</th>
                  <th>Makine</th>
                  <th>Operatör</th>
                </tr>
              </thead>
              <tbody>
                {data.items.map((bundle) => (
                  <tr key={bundle.bundleOperationId}>
                    <td><RecordLink to={`/bundles/${bundle.bundleId}`}><code>{bundle.bundleNo}</code></RecordLink></td>
                    <td><RecordLink to={`/production-orders/${bundle.productionOrderId}`}>{bundle.orderNo}</RecordLink></td>
                    <td>{bundle.modelCode}</td>
                    <td>{bundle.colorName} / {bundle.sizeName}</td>
                    <td><OperationStatusBadge status={bundle.status} /></td>
                    <td className="table__number">{formatQuantity(bundle.remainingQuantity)}</td>
                    <td>{bundle.machineCode ?? '—'}</td>
                    <td>{bundle.operatorName ?? '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}
    </>
  )
}

function WipSection() {
  const pollingInterval = useLivePolling(FALLBACK_REFRESH_MS)
  const { data, error, isLoading } = useGetWipQuery(undefined, { pollingInterval })
  const [selectedId, setSelectedId] = useState<number | null>(null)
  const selected = data?.find((operation) => operation.operationId === selectedId)

  return (
    <>
      <h2 className="section-title">Ara stok (operasyon önünde bekleyenler)</h2>
      <p className="muted">Sahadaki demetler. Demetleri görmek için operasyonu seçin.</p>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {data && (
        <div className="table-wrap">
          <table className="table">
            <thead>
              <tr>
                <th>Operasyon</th>
                <th className="table__number">Bekleyen demet</th>
                <th className="table__number">Bekleyen adet</th>
                <th className="table__number">İşlemdeki demet</th>
                <th className="table__number">İşlemdeki adet</th>
                <th className="table__number">Yarım kalan demet</th>
                <th className="table__number">Yarım kalan adet</th>
                <th aria-label="İşlemler" />
              </tr>
            </thead>
            <tbody>
              {data.map((operation) => (
                <tr key={operation.operationId} aria-selected={operation.operationId === selectedId}>
                  <td><code>{operation.operationCode}</code> {operation.operationName}</td>
                  <td className="table__number">{operation.readyBundles}</td>
                  <td className="table__number">{formatQuantity(operation.readyQuantity)}</td>
                  <td className="table__number">{operation.inProgressBundles}</td>
                  <td className="table__number">{formatQuantity(operation.inProgressQuantity)}</td>
                  <td className="table__number">{operation.pausedBundles}</td>
                  <td className="table__number">{formatQuantity(operation.pausedQuantity)}</td>
                  <td className="table__actions">
                    <button
                      type="button"
                      className="button"
                      aria-pressed={operation.operationId === selectedId}
                      onClick={() => setSelectedId(operation.operationId === selectedId ? null : operation.operationId)}
                    >
                      {operation.operationId === selectedId ? 'Gizle' : 'Demetler'}
                    </button>
                  </td>
                </tr>
              ))}
              {data.length === 0 && (
                <tr>
                  <td colSpan={8} className="table__empty">Sahada bekleyen ya da işlenen demet yok.</td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}
      {/* key: operasyon değişince sayfa numarası sıfırlansın. */}
      {selected && <WipBundles key={selected.operationId} operation={selected} />}
    </>
  )
}

function OutputSection() {
  const pollingInterval = useLivePolling(FALLBACK_REFRESH_MS)
  const [fromDate, setFromDate] = useState(todayIsoDate)
  const [toDate, setToDate] = useState(todayIsoDate)
  const [groupBy, setGroupBy] = useState<OutputGroupBy>('OPERATOR')
  const range = dayRangeToUtc(fromDate, toDate)

  const { data, error, isFetching } = useGetOutputQuery(
    range ? { ...range, groupBy } : { from: '', to: '', groupBy },
    { skip: !range, pollingInterval },
  )

  return (
    <>
      <h2 className="section-title">Üretim çıktısı</h2>
      <div className="toolbar toolbar--filters">
        <label>
          Başlangıç
          <input type="date" value={fromDate} max={toDate} onChange={(event) => setFromDate(event.target.value)} />
        </label>
        <label>
          Bitiş
          <input type="date" value={toDate} min={fromDate} onChange={(event) => setToDate(event.target.value)} />
        </label>
        <label>
          Gruplama
          <select value={groupBy} onChange={(event) => setGroupBy(event.target.value as OutputGroupBy)}>
            {Object.entries(groupByLabels).map(([value, label]) => (
              <option key={value} value={value}>{label}</option>
            ))}
          </select>
        </label>
      </div>

      {!range && <p className="alert alert--error" role="alert">Bitiş tarihi başlangıçtan önce olamaz.</p>}
      <ErrorAlert error={error} />

      {range && data && (
        <div className="table-wrap" aria-busy={isFetching}>
          <table className="table">
            <thead>
              <tr>
                <th>{groupByLabels[data.groupBy]}</th>
                <th className="table__number">Üretilen</th>
                <th className="table__number">Hatalı</th>
                <th className="table__number">Demet</th>
              </tr>
            </thead>
            <tbody>
              {data.rows.map((row) => (
                <tr key={row.id}>
                  <td><code>{row.code}</code> {row.name}</td>
                  <td className="table__number">{formatQuantity(row.produced)}</td>
                  <td className="table__number">{formatQuantity(row.rejected)}</td>
                  <td className="table__number">{row.bundles}</td>
                </tr>
              ))}
              {data.rows.length === 0 && (
                <tr>
                  <td colSpan={4} className="table__empty">Bu aralıkta üretim bildirimi yok.</td>
                </tr>
              )}
            </tbody>
            {data.rows.length > 0 && (
              <tfoot>
                <tr>
                  <th scope="row">Toplam</th>
                  <td className="table__number"><strong>{formatQuantity(data.totalProduced)}</strong></td>
                  <td className="table__number"><strong>{formatQuantity(data.totalRejected)}</strong></td>
                  <td />
                </tr>
              </tfoot>
            )}
          </table>
        </div>
      )}
    </>
  )
}

/** Üretimin anlık görünümü: makineler, ara stok ve üretim çıktısı. */
export function ProductionTrackingPage() {
  return (
    <section className="page">
      <h1 className="visually-hidden">Üretim Takibi</h1>
      <MachinesSection />
      <WipSection />
      <OutputSection />
    </section>
  )
}
