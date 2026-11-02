import { ArrowDown, ArrowUp, CalendarRange, MoveRight, Table2 } from 'lucide-react'
import { Fragment, useState } from 'react'
import type { CSSProperties, FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDate, formatDateTimeShort, formatQuantity } from '../../utils/format'
import {
  dokumaPlanBeamStatusLabels,
  useGetDokumaCapacityOptionsQuery,
  useGetDokumaCapacityPlanQuery,
  usePlanDokumaBeamMutation,
  useReorderDokumaLoomQueueMutation,
} from './dokumaPlanningApi'
import type { DokumaCapacityItem, DokumaCapacityLoom, DokumaCapacityPlan } from './dokumaPlanningApi'
import { dokumaStopCategoryLabels, loomTypeLabels, sheddingTypeLabels } from './dokumaPresentation'
import './dokumaPlanning.css'

type View = 'timeline' | 'table'

const DAY_MS = 24 * 60 * 60 * 1000
const weekday = new Intl.DateTimeFormat('tr-TR', { weekday: 'short' })
const dayMonth = new Intl.DateTimeFormat('tr-TR', { day: '2-digit', month: '2-digit' })

const percent = (value: number) => `%${formatQuantity(value)}`
const hours = (value: number | null) => (value === null ? '—' : `${formatQuantity(Math.round(value * 10) / 10)} sa`)

/** Planlanacak levent: kuyruktaki ya da planlanmamış. */
interface PlanTarget {
  beamId: number
  beamNo: string
  article: string
  loomId: number | null
}

/**
 * Leventi bir tezgâhın kuyruğuna alma ya da başka tezgâha taşıma. Bütün etkin tezgâhlar listelenir; ağızlık sistemi ya da tarak eni
 * artikele uymayan tezgâh seçilemez (levent bağlarken de aynı kural uygulanır).
 */
function PlanDialog({ target, onClose }: { target: PlanTarget; onClose: () => void }) {
  const { data, error, isLoading } = useGetDokumaCapacityOptionsQuery(target.beamId)
  const [plan, planState] = usePlanDokumaBeamMutation()
  const [loomId, setLoomId] = useState<number | null>(target.loomId)
  const [position, setPosition] = useState('')

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    if (loomId === null) {
      return
    }
    const result = await plan({ beamId: target.beamId, loomId, position: position.trim() === '' ? null : Number(position) })
    if (!('error' in result)) {
      onClose()
    }
  }

  const unplan = async () => {
    const result = await plan({ beamId: target.beamId, loomId: null })
    if (!('error' in result)) {
      onClose()
    }
  }

  return (
    <Modal title={`Tezgâh planı: ${target.beamNo}`} onClose={onClose} variant="drawer">
      <form className="form" onSubmit={submit}>
        <p className="muted">{target.article}. Süre bu leventin kalan kumaşının tezgâhın hızı ve verimiyle tahminidir.</p>
        {isLoading && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={error} />
        {data && (
          <div className="table-wrap">
            <table className="table table--compact">
              <thead>
                <tr>
                  <th aria-label="Seç" />
                  <th>Tezgâh</th>
                  <th className="table__number">Süre</th>
                  <th className="table__number">Kuyruk</th>
                  <th>Boşalır</th>
                  <th>Uyarı</th>
                </tr>
              </thead>
              <tbody>
                {data.map((option) => (
                  <tr key={option.loomId}>
                    <td>
                      <input
                        type="radio"
                        name="loom"
                        aria-label={`${option.code} seç`}
                        disabled={option.fitWarning !== null}
                        checked={loomId === option.loomId}
                        onChange={() => setLoomId(option.loomId)}
                      />
                    </td>
                    <td>
                      <strong>{option.code}</strong> {option.name}
                      <div className="muted">
                        {sheddingTypeLabels[option.sheddingType]}
                        {option.maxReedWidthCm !== null && `, en fazla ${formatQuantity(option.maxReedWidthCm)} cm`}
                        {option.nominalSpeedRpm !== null && `, ${option.nominalSpeedRpm} devir/dk`}
                        {option.current && ' · şu anki plan'}
                      </div>
                    </td>
                    <td className="table__number">{option.durationHours === null ? <span className="muted" title="Tezgâh kartında anma hızı yok">Hız yok</span> : hours(option.durationHours)}</td>
                    <td className="table__number">{option.queueCount}</td>
                    <td>{option.queueEndsAt ? formatDateTimeShort(option.queueEndsAt) : <span className="muted">Boş</span>}</td>
                    <td>
                      {option.fitWarning && <div className="dokuma-negative">Uygun değil: {option.fitWarning}</div>}
                      {option.openStopCategory && <span className="badge badge--warning">{dokumaStopCategoryLabels[option.openStopCategory]}</span>}
                      {!option.fitWarning && !option.openStopCategory && <span className="muted">—</span>}
                    </td>
                  </tr>
                ))}
                {data.length === 0 && <tr><td colSpan={6} className="table__empty">Etkin tezgâh yok.</td></tr>}
              </tbody>
            </table>
          </div>
        )}
        <label className="field">
          <span>Kuyruktaki sıra</span>
          <input type="number" min="1" step="1" placeholder="Boşsa sona eklenir" value={position} onChange={(event) => setPosition(event.target.value)} />
        </label>
        <ErrorAlert error={planState.error} />
        <footer className="form__actions">
          {target.loomId !== null && <button type="button" className="button button--danger-ghost" disabled={planState.isLoading} onClick={unplan}>Plandan çıkar</button>}
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={planState.isLoading || loomId === null}>Kaydet</button>
        </footer>
      </form>
    </Modal>
  )
}

function itemTitle(item: DokumaCapacityItem) {
  return [
    `${item.beamNo} · ${item.orderNo}${item.customerName ? ` · ${item.customerName}` : ''}`,
    `${item.articleCode} — ${item.articleName}`,
    `Kalan: ${formatQuantity(item.remainingMeters)} m (beklenen ${formatQuantity(item.expectedFabricMeters)} m, dokunan ${formatQuantity(item.wovenMeters)} m)`,
    item.notReady ? `Levent hazır değil: ${dokumaPlanBeamStatusLabels[item.beamStatus]}` : null,
    `Başlangıç: ${formatDateTimeShort(item.estimatedStart)}`,
    item.estimatedFinish ? `Tahmini bitiş: ${formatDateTimeShort(item.estimatedFinish)}` : 'Tezgâh hızı ya da atkı sıklığı yok; süre hesaplanamadı',
    `Termin: ${formatDate(item.dueDate)}${item.lateRisk ? ` (${item.lateDays} gün gecikme riski)` : ''}`,
    item.fitWarning ? `Uygun değil: ${item.fitWarning}` : null,
  ].filter(Boolean).join('\n')
}

function targetOf(item: DokumaCapacityItem, loomId: number): PlanTarget {
  return { beamId: item.beamId, beamNo: item.beamNo, article: `${item.articleCode} — ${item.articleName}`, loomId }
}

/** Zaman çizelgesi: tezgâh başına bir satır, gün sütunları CSS grid; çubuklar tahmini başlangıç ve bitişe göre konumlanır. */
function Timeline({ plan, canManage, onPlan }: { plan: DokumaCapacityPlan; canManage: boolean; onPlan: (target: PlanTarget) => void }) {
  const days = plan.days
  const start = new Date(`${plan.today}T00:00:00`)
  const dates = Array.from({ length: days }, (_, index) => new Date(start.getTime() + index * DAY_MS))
  const nowOffset = (Date.parse(plan.generatedAt) - start.getTime()) / DAY_MS
  const style = { '--dokuma-days': days } as CSSProperties
  const position = (from: number, to: number) => {
    const left = Math.max(0, from)
    const right = Math.min(days, to)
    return { left: `${(left / days) * 100}%`, width: `${((right - left) / days) * 100}%` }
  }

  return (
    <div className="dokuma-timeline">
      <div className="dokuma-timeline__row dokuma-timeline__head">
        <div className="dokuma-timeline__label">Tezgâh</div>
        <div className="dokuma-timeline__track" style={style}>
          {dates.map((date, index) => (
            <div key={index} className={`dokuma-timeline__day${date.getDay() === 0 ? ' dokuma-timeline__day--weekend' : ''}${index === 0 ? ' dokuma-timeline__day--today' : ''}`} title={date.toLocaleDateString('tr-TR')}>
              {dayMonth.format(date)}
              <br />
              {weekday.format(date)}
            </div>
          ))}
        </div>
      </div>
      {plan.looms.map((loom) => (
        <div key={loom.loomId} className="dokuma-timeline__row">
          <div className="dokuma-timeline__label">
            <strong>{loom.loomCode}</strong> <span className="muted">{sheddingTypeLabels[loom.sheddingType]}{loom.nominalSpeedRpm !== null && `, ${loom.nominalSpeedRpm} dv/dk`}</span>
            {loom.openStopCategory && <> <span className="badge badge--warning">{dokumaStopCategoryLabels[loom.openStopCategory]}</span></>}
            <div className="dokuma-loads" title="Önümüzdeki 7 / 14 / 30 günün dolu payı">
              <span>7g {percent(loom.load7Percent)}</span>
              <span>14g {percent(loom.load14Percent)}</span>
              <span>30g {percent(loom.load30Percent)}</span>
            </div>
          </div>
          <div className="dokuma-timeline__track" style={style}>
            {dates.map((date, index) => <div key={index} className={`dokuma-timeline__day${date.getDay() === 0 ? ' dokuma-timeline__day--weekend' : ''}`} />)}
            {nowOffset >= 0 && nowOffset < days && <div className="dokuma-timeline__now" style={{ left: `${(nowOffset / days) * 100}%` }} aria-hidden="true" />}
            {loom.items.map((item) => {
              const end = item.endOffsetDays
              if (item.startOffsetDays >= days || (end !== null && end <= 0)) {
                return null
              }
              const className = `dokuma-bar${item.running ? ' dokuma-bar--running' : ''}${item.notReady ? ' dokuma-bar--not-ready' : ''}${item.lateRisk ? ' dokuma-bar--late' : ''}${end === null ? ' dokuma-bar--missing' : ''}`
              const place = end === null ? { left: `${(Math.max(0, item.startOffsetDays) / days) * 100}%` } : position(item.startOffsetDays, end)
              const label = `${item.beamNo} ${item.articleCode}`
              return canManage && !item.running
                ? <button key={item.beamId} type="button" className={className} style={place} title={itemTitle(item)} onClick={() => onPlan(targetOf(item, loom.loomId))}>{end === null ? '' : label}</button>
                : <div key={item.beamId} className={className} style={place} title={itemTitle(item)}>{end === null ? '' : label}</div>
            })}
          </div>
        </div>
      ))}
    </div>
  )
}

/** Tablo görünümü: tezgâh başına kuyruk, sırayı değiştirme ve başka tezgâha taşıma. */
function QueueTable({ plan, canManage, onPlan }: { plan: DokumaCapacityPlan; canManage: boolean; onPlan: (target: PlanTarget) => void }) {
  const [reorder, reorderState] = useReorderDokumaLoomQueueMutation()

  const move = (loom: DokumaCapacityLoom, beamId: number, step: -1 | 1) => {
    const queued = loom.items.filter((item) => !item.running).map((item) => item.beamId)
    const index = queued.indexOf(beamId)
    const swap = index + step
    if (index < 0 || swap < 0 || swap >= queued.length) {
      return
    }
    const next = [...queued]
    next[index] = queued[swap]
    next[swap] = beamId
    reorder({ loomId: loom.loomId, beamIds: next })
  }

  return (
    <>
      <ErrorAlert error={reorderState.error} />
      <div className="table-wrap">
        <table className="table table--compact">
          <thead>
            <tr>
              <th>Sıra</th>
              <th>Levent</th>
              <th>Sipariş</th>
              <th>Artikel</th>
              <th className="table__number">Kalan (m)</th>
              <th className="table__number">m/saat</th>
              <th className="table__number">Süre</th>
              <th>Başlangıç</th>
              <th>Tahmini bitiş</th>
              <th>Termin</th>
              <th>Risk</th>
              <th aria-label="İşlem" />
            </tr>
          </thead>
          <tbody>
            {plan.looms.map((loom) => {
              const queued = loom.items.filter((item) => !item.running)
              return (
                <Fragment key={loom.loomId}>
                  <tr className="dokuma-loom-head">
                    <th colSpan={12} scope="rowgroup">
                      {loom.loomCode}{' '}
                      <span className="muted">
                        {loom.loomName} · {loomTypeLabels[loom.loomType] ?? loom.loomType}, {sheddingTypeLabels[loom.sheddingType]}
                        {loom.maxReedWidthCm !== null && `, en fazla ${formatQuantity(loom.maxReedWidthCm)} cm`}
                        {`, ${loom.nominalSpeedRpm === null ? 'hız yok' : `${loom.nominalSpeedRpm} devir/dk`}, verim ${percent(loom.efficiencyPercent)}`}
                        {!loom.isActive && ' · pasif'}
                      </span>
                      {loom.openStopCategory && <> <span className="badge badge--warning">{dokumaStopCategoryLabels[loom.openStopCategory]}{loom.openStopReason && `: ${loom.openStopReason}`}</span></>}
                      <span className="muted"> · Doluluk 7/14/30 gün: {percent(loom.load7Percent)} / {percent(loom.load14Percent)} / {percent(loom.load30Percent)}</span>
                      <span className="muted"> · Kuyruk: {hours(loom.queueHours)}{loom.queueEndsAt && `, boşalır ${formatDateTimeShort(loom.queueEndsAt)}`}</span>
                    </th>
                  </tr>
                  {loom.items.map((item) => {
                    const queueIndex = queued.findIndex((row) => row.beamId === item.beamId)
                    return (
                      <tr key={item.beamId}>
                        <td>{item.running ? <span className="badge badge--info">Tezgâhta</span> : item.sequence}</td>
                        <td>
                          {item.running ? <Link to={`/dokuma/weaving/beams/${item.beamId}`}><code>{item.beamNo}</code></Link> : <code>{item.beamNo}</code>}
                          <div className="muted"><Link to={`/dokuma/warp-orders/${item.warpOrderId}`}>{item.warpNo}</Link>{item.notReady && ` · ${dokumaPlanBeamStatusLabels[item.beamStatus]}`}</div>
                        </td>
                        <td><Link to={`/dokuma/orders/${item.orderId}`}>{item.orderNo}</Link>{item.customerName && <div className="muted">{item.customerName}</div>}</td>
                        <td>{item.articleCode} — {item.articleName}{item.fitWarning && <div className="dokuma-negative">Uygun değil: {item.fitWarning}</div>}</td>
                        <td className="table__number">
                          {formatQuantity(item.remainingMeters)}
                          {item.wovenMeters > 0 && <div className="muted">dokunan {formatQuantity(item.wovenMeters)}</div>}
                        </td>
                        <td className="table__number">{item.metersPerHour === null ? <span className="muted" title="Tezgâh kartında anma hızı yok">Yok</span> : formatQuantity(item.metersPerHour)}</td>
                        <td className="table__number">{hours(item.durationHours)}</td>
                        <td>{formatDateTimeShort(item.estimatedStart)}</td>
                        <td>{item.estimatedFinish ? formatDateTimeShort(item.estimatedFinish) : '—'}</td>
                        <td>{formatDate(item.dueDate)}</td>
                        <td>{item.lateRisk ? <span className="badge badge--danger">{item.lateDays} gün geç</span> : item.dueDate && item.estimatedFinish ? <span className="badge badge--ok">Zamanında</span> : '—'}</td>
                        <td className="table__actions">
                          {canManage && !item.running && (
                            <>
                              <button type="button" className="button button--icon" aria-label="Yukarı al" title="Yukarı al" disabled={reorderState.isLoading || queueIndex <= 0} onClick={() => move(loom, item.beamId, -1)}><ArrowUp aria-hidden="true" /></button>
                              <button type="button" className="button button--icon" aria-label="Aşağı al" title="Aşağı al" disabled={reorderState.isLoading || queueIndex >= queued.length - 1} onClick={() => move(loom, item.beamId, 1)}><ArrowDown aria-hidden="true" /></button>
                              <button type="button" className="button" onClick={() => onPlan(targetOf(item, loom.loomId))}><MoveRight aria-hidden="true" />Taşı</button>
                            </>
                          )}
                        </td>
                      </tr>
                    )
                  })}
                  {loom.items.length === 0 && <tr><td colSpan={12} className="table__empty">Tezgâh boş, kuyrukta levent yok.</td></tr>}
                </Fragment>
              )
            })}
            {plan.looms.length === 0 && <tr><td colSpan={12} className="table__empty">Etkin tezgâh yok.</td></tr>}
          </tbody>
        </table>
      </div>
    </>
  )
}

/**
 * Tezgâh kapasite planı: her tezgâhta bağlı levent ve kuyruğa alınmış leventler, tahmini başlangıç ve bitiş, 7/14/30 günlük doluluk ve
 * termini aşma riski. Süre = kalan kumaş ÷ (devir/dk × 60 ÷ (atkı/cm × 100) × verim); günlük çalışma saati vardiyalardan gelir.
 */
export function DokumaCapacityPlanPage() {
  const canManage = usePermission(Permissions.DokumaPlanningManage)
  const [days, setDays] = useState(30)
  const [hoursInput, setHoursInput] = useState('')
  const [view, setView] = useState<View>('timeline')
  const [target, setTarget] = useState<PlanTarget | null>(null)
  const hoursPerDay = hoursInput.trim() === '' ? undefined : Number(hoursInput)
  const validHours = hoursPerDay === undefined || (hoursPerDay >= 1 && hoursPerDay <= 24)
  const { data, error, isLoading, isFetching } = useGetDokumaCapacityPlanQuery({ days, ...(hoursPerDay === undefined ? {} : { hoursPerDay }) }, { skip: !validHours })
  const lateCount = data?.looms.reduce((sum, loom) => sum + loom.lateRiskCount, 0) ?? 0
  const missingCount = data?.looms.reduce((sum, loom) => sum + loom.missingRateCount, 0) ?? 0
  const readyUnplanned = data?.unplanned.filter((beam) => beam.beamStatus === 'READY').length ?? 0

  return (
    <section className="page">
      <h1 className="visually-hidden">Kapasite Planı</h1>
      <div className="toolbar">
        <label className="field">
          <span>Görünen gün</span>
          <select value={days} onChange={(event) => setDays(Number(event.target.value))}>
            <option value={14}>14 gün</option>
            <option value={30}>30 gün</option>
            <option value={60}>60 gün</option>
          </select>
        </label>
        <label className="field">
          <span>Günlük çalışma (saat)</span>
          <input type="number" min="1" max="24" step="0.5" placeholder={data ? String(data.hoursPerDay) : 'Vardiyalardan'} value={hoursInput} onChange={(event) => setHoursInput(event.target.value)} />
        </label>
        <div className="toolbar__actions">
          <div className="tabs" role="tablist" aria-label="Görünüm">
            <button type="button" role="tab" aria-selected={view === 'timeline'} className={`tabs__tab${view === 'timeline' ? ' tabs__tab--active' : ''}`} onClick={() => setView('timeline')}><CalendarRange aria-hidden="true" />Zaman çizelgesi</button>
            <button type="button" role="tab" aria-selected={view === 'table'} className={`tabs__tab${view === 'table' ? ' tabs__tab--active' : ''}`} onClick={() => setView('table')}><Table2 aria-hidden="true" />Tablo</button>
          </div>
        </div>
      </div>
      {!validHours && <p className="alert alert--error">Günlük çalışma 1 ile 24 saat arasında olmalıdır.</p>}
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {data && (
        <>
          <dl className="stat-strip">
            <div><dt>Günlük çalışma</dt><dd>{formatQuantity(data.hoursPerDay)} sa <span className="muted">({data.hoursSource})</span></dd></div>
            <div><dt>Genel verim</dt><dd>{percent(data.defaultEfficiencyPercent)}</dd></div>
            <div><dt>Tezgâh</dt><dd>{data.looms.length}</dd></div>
            <div><dt>Termin riski</dt><dd>{lateCount > 0 ? <span className="dokuma-negative">{lateCount} levent</span> : 'Yok'}</dd></div>
            <div><dt>Planlanmamış levent</dt><dd>{data.unplanned.length}{readyUnplanned > 0 && <span className="muted"> ({readyUnplanned} hazır)</span>}</dd></div>
            {missingCount > 0 && <div><dt>Hızı tanımsız</dt><dd>{missingCount} levent</dd></div>}
          </dl>
          <p className="muted">
            Süre = kalan kumaş ÷ saatte dokunan metre (devir/dk × 60 ÷ (atkı/cm × 100) × verim). Kalan kumaş, levent metresinin çözgü çekmesi
            düşülmüş hâlinden kesilen topların çıkarılmasıyla bulunur. Verim tezgâha özel değerden ya da maliyet parametrelerindeki genel verimden gelir;
            dokuma saati günlük çalışma saatine göre takvime yayılır. Kuyruk sırası ve tezgâh değişikliği yalnızca plandır; leventi fiilen bağlamak
            tezgâh panosundan yapılır, bağlanan levent kuyruktan çıkar. Hesaplama: {formatDateTimeShort(data.generatedAt)}{isFetching && ' (yenileniyor)'}.
          </p>
          {view === 'timeline' && (
            <>
              <div className="dokuma-legend">
                <span><span className="dokuma-bar dokuma-bar--running" />Tezgâhta</span>
                <span><span className="dokuma-bar" />Kuyrukta</span>
                <span><span className="dokuma-bar dokuma-bar--not-ready" />Hazır değil</span>
                <span><span className="dokuma-bar dokuma-bar--late" />Termin riski</span>
                <span><span className="dokuma-bar dokuma-bar--missing" />Hızı tanımsız</span>
                {canManage && <span>Kuyruktaki çubuğa tıklayarak tezgâhını ya da sırasını değiştirin.</span>}
              </div>
              <Timeline plan={data} canManage={canManage} onPlan={setTarget} />
            </>
          )}
          {view === 'table' && <QueueTable plan={data} canManage={canManage} onPlan={setTarget} />}

          <h2>Planlanmamış leventler</h2>
          <div className="table-wrap">
            <table className="table table--compact">
              <thead>
                <tr>
                  <th>Levent</th>
                  <th>Durum</th>
                  <th>Sipariş</th>
                  <th>Artikel</th>
                  <th>Ağızlık / tarak eni</th>
                  <th>Hazır</th>
                  <th>Termin</th>
                  <th className="table__number">Beklenen kumaş (m)</th>
                  <th aria-label="İşlem" />
                </tr>
              </thead>
              <tbody>
                {data.unplanned.map((beam) => (
                  <tr key={beam.beamId}>
                    <td><code>{beam.beamNo}</code><div className="muted"><Link to={`/dokuma/warp-orders/${beam.warpOrderId}`}>{beam.warpNo}</Link></div></td>
                    <td>{beam.beamStatus === 'READY' ? <span className="badge badge--ok">{dokumaPlanBeamStatusLabels[beam.beamStatus]}</span> : dokumaPlanBeamStatusLabels[beam.beamStatus]}</td>
                    <td><Link to={`/dokuma/orders/${beam.orderId}`}>{beam.orderNo}</Link>{beam.customerName && <div className="muted">{beam.customerName}</div>}</td>
                    <td>{beam.articleCode} — {beam.articleName}{beam.note && <div className="muted">{beam.note}</div>}</td>
                    <td>{sheddingTypeLabels[beam.sheddingType]}, {formatQuantity(beam.reedWidthCm)} cm<div className="muted">{formatQuantity(beam.picksPerCm)} atkı/cm</div></td>
                    <td>{formatDate(beam.readyOn)}</td>
                    <td>{formatDate(beam.dueDate)}</td>
                    <td className="table__number">{formatQuantity(beam.expectedFabricMeters)}</td>
                    <td className="table__actions">
                      {canManage && (
                        <button
                          type="button"
                          className="button"
                          onClick={() => setTarget({ beamId: beam.beamId, beamNo: beam.beamNo, article: `${beam.articleCode} — ${beam.articleName}`, loomId: null })}
                        >
                          Tezgâha planla
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
                {data.unplanned.length === 0 && <tr><td colSpan={9} className="table__empty">Bütün leventler bir tezgâhta ya da kuyrukta.</td></tr>}
              </tbody>
            </table>
          </div>
        </>
      )}
      {target && <PlanDialog target={target} onClose={() => setTarget(null)} />}
    </section>
  )
}
