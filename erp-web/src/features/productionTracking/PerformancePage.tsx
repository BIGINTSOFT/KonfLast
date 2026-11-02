import { useState } from 'react'
import { ErrorAlert } from '../../components/FormControls'
import { formatQuantity, todayIsoDate } from '../../utils/format'
import { formatDuration } from '../workSessions/workSessionPresentation'
import { useGetPerformanceQuery } from './trackingApi'
import type { PerformanceGroupBy, PerformanceRow } from './trackingApi'
import { dayRangeToUtc, efficiencyTone, formatPercent, performanceGroupLabels } from './trackingPresentation'

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

function Row({ row, timeBaseIsActive, total = false }: { row: PerformanceRow; timeBaseIsActive: boolean; total?: boolean }) {
  const Cell = total ? 'th' : 'td'
  return (
    <tr>
      <Cell scope={total ? 'row' : undefined}>
        {total ? 'Toplam' : <><code>{row.code}</code> {row.name}</>}
      </Cell>
      {!timeBaseIsActive && (
        <>
          <td className="table__number">{formatDuration(row.presenceMinutes)}</td>
          <td className="table__number">{formatDuration(row.breakMinutes)}</td>
          <td className="table__number">{formatDuration(row.pauseMinutes)}</td>
          <td className="table__number">{formatDuration(row.breakdownMinutes)}</td>
          <td className="table__number">{formatDuration(row.waitingMinutes)}</td>
          <td className="table__number">{formatDuration(row.netWorkingMinutes)}</td>
        </>
      )}
      <td className="table__number">{formatDuration(row.activeMinutes)}</td>
      <td className="table__number">{formatQuantity(row.produced)}</td>
      <td className="table__number">{row.targetQuantity === null ? '—' : formatQuantity(row.targetQuantity)}</td>
      <td className="table__number">{formatQuantity(row.rejected)}</td>
      <td><Efficiency value={row.efficiencyPercent} /></td>
      {!timeBaseIsActive && <td className="table__number">{formatPercent(row.utilizationPercent)}</td>}
    </tr>
  )
}

/** Operatör, makine, bant ve operasyon bazında süre, üretim ve verim. Tüm değerleri sunucu hesaplar. */
export function PerformancePage() {
  const [fromDate, setFromDate] = useState(todayIsoDate)
  const [toDate, setToDate] = useState(todayIsoDate)
  const [groupBy, setGroupBy] = useState<PerformanceGroupBy>('OPERATOR')
  const range = dayRangeToUtc(fromDate, toDate)

  const { data, error, isLoading, isFetching } = useGetPerformanceQuery(
    range ? { ...range, groupBy } : { from: '', to: '', groupBy },
    { skip: !range, pollingInterval: 60_000 },
  )

  const timeBaseIsActive = data?.groupBy === 'OPERATION'

  return (
    <section className="page">
      <h1 className="visually-hidden">Performans</h1>

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
          <select value={groupBy} onChange={(event) => setGroupBy(event.target.value as PerformanceGroupBy)}>
            {Object.entries(performanceGroupLabels).map(([value, label]) => (
              <option key={value} value={value}>{label}</option>
            ))}
          </select>
        </label>
      </div>

      {!range && <p className="alert alert--error" role="alert">Bitiş tarihi başlangıçtan önce olamaz.</p>}
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />

      {range && data && (
        <>
          {timeBaseIsActive && (
            <p className="alert alert--info">
              Operasyon karşılaştırmasında (darboğaz analizi) süre, operasyonun fiilen işlemde olduğu süredir; verimi
              en düşük operasyon hattı yavaşlatan operasyondur.
            </p>
          )}
          <div className="table-wrap" aria-busy={isFetching}>
            <table className="table">
              <thead>
                <tr>
                  <th>{performanceGroupLabels[data.groupBy]}</th>
                  {!timeBaseIsActive && (
                    <>
                      <th className="table__number">Mevcudiyet</th>
                      <th className="table__number">Planlı mola</th>
                      <th className="table__number">Ara</th>
                      <th className="table__number">Arıza</th>
                      <th className="table__number">Demet bekleme</th>
                      <th className="table__number">Net çalışma</th>
                    </>
                  )}
                  <th className="table__number">Aktif üretim</th>
                  <th className="table__number">Üretilen</th>
                  <th className="table__number">Hedef</th>
                  <th className="table__number">Hatalı</th>
                  <th>Verim</th>
                  {!timeBaseIsActive && <th className="table__number">Kullanım</th>}
                </tr>
              </thead>
              <tbody>
                {data.rows.map((row) => (
                  <Row key={row.id} row={row} timeBaseIsActive={timeBaseIsActive} />
                ))}
                {data.rows.length === 0 && (
                  <tr>
                    <td colSpan={timeBaseIsActive ? 6 : 13} className="table__empty">
                      Bu aralıkta oturum ya da üretim kaydı yok.
                    </td>
                  </tr>
                )}
              </tbody>
              {data.rows.length > 0 && (
                <tfoot>
                  <Row row={data.total} timeBaseIsActive={timeBaseIsActive} total />
                </tfoot>
              )}
            </table>
          </div>
        </>
      )}
    </section>
  )
}
