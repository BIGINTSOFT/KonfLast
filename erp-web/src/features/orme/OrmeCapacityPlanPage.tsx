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
  useGetOrmeCapacityOptionsQuery,
  useGetOrmeCapacityPlanQuery,
  usePlanOrmeKnitOrderMutation,
  useReorderOrmeMachineQueueMutation,
} from './ormePlanningApi'
import type { OrmeCapacityItem, OrmeCapacityMachine, OrmeCapacityPlan } from './ormePlanningApi'
import { knitBedTypeLabels, ormeStopCategoryLabels } from './ormePresentation'
import './ormePlanning.css'

type View = 'timeline' | 'table'

const DAY_MS = 24 * 60 * 60 * 1000
const weekday = new Intl.DateTimeFormat('tr-TR', { weekday: 'short' })
const dayMonth = new Intl.DateTimeFormat('tr-TR', { day: '2-digit', month: '2-digit' })

const percent = (value: number) => `%${formatQuantity(value)}`
const hours = (value: number | null) => (value === null ? '—' : `${formatQuantity(Math.round(value * 10) / 10)} sa`)

/** Planlanacak emir: kuyruktaki ya da planlanmamış. */
interface PlanTarget {
  knitOrderId: number
  knitOrderNo: string
  fabric: string
  plannedDate: string
  machineId: number | null
}

/**
 * Emri bir makinenin kuyruğuna alma ya da başka makineye taşıma. Yalnızca aynı plakadaki makineler listelenir; çap ya da fine farkı
 * uyarıdır, engel değildir.
 */
function PlanDialog({ target, onClose }: { target: PlanTarget; onClose: () => void }) {
  const { data, error, isLoading } = useGetOrmeCapacityOptionsQuery(target.knitOrderId)
  const [plan, planState] = usePlanOrmeKnitOrderMutation()
  const [machineId, setMachineId] = useState<number | null>(target.machineId)
  const [plannedDate, setPlannedDate] = useState(target.plannedDate)
  const [position, setPosition] = useState('')

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    if (machineId === null) {
      return
    }
    const result = await plan({ knitOrderId: target.knitOrderId, machineId, plannedDate: plannedDate || null, position: position.trim() === '' ? null : Number(position) })
    if (!('error' in result)) {
      onClose()
    }
  }

  const unplan = async () => {
    const result = await plan({ knitOrderId: target.knitOrderId, machineId: null })
    if (!('error' in result)) {
      onClose()
    }
  }

  return (
    <Modal title={`Makine planı: ${target.knitOrderNo}`} onClose={onClose} variant="drawer">
      <form className="form" onSubmit={submit}>
        <p className="muted">{target.fabric}. Yalnızca kumaşla aynı plakadaki etkin makineler listelenir; çap ya da fine farkı uyarı olarak gösterilir.</p>
        {isLoading && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={error} />
        {data && (
          <div className="table-wrap">
            <table className="table table--compact">
              <thead>
                <tr>
                  <th aria-label="Seç" />
                  <th>Makine</th>
                  <th className="table__number">Kuyruk</th>
                  <th>Boşalır</th>
                  <th>Uyarı</th>
                </tr>
              </thead>
              <tbody>
                {data.map((option) => (
                  <tr key={option.machineId}>
                    <td><input type="radio" name="machine" aria-label={`${option.code} seç`} checked={machineId === option.machineId} onChange={() => setMachineId(option.machineId)} /></td>
                    <td>
                      <strong>{option.code}</strong> {option.name}
                      <div className="muted">{option.diameterInch}", E{option.gauge}{option.current && ' · şu anki plan'}</div>
                    </td>
                    <td className="table__number">{option.queueCount}</td>
                    <td>{option.queueEndsAt ? formatDateTimeShort(option.queueEndsAt) : <span className="muted">Boş</span>}</td>
                    <td>
                      {option.fitWarning && <div>Kumaş kartından farklı: {option.fitWarning}</div>}
                      {option.openStopCategory && <span className="badge badge--warning">{ormeStopCategoryLabels[option.openStopCategory]}</span>}
                      {!option.fitWarning && !option.openStopCategory && <span className="muted">—</span>}
                    </td>
                  </tr>
                ))}
                {data.length === 0 && <tr><td colSpan={5} className="table__empty">Aynı plakada etkin makine yok.</td></tr>}
              </tbody>
            </table>
          </div>
        )}
        <div className="form__pair">
          <label className="field">
            <span>Kuyruktaki sıra</span>
            <input type="number" min="1" step="1" placeholder="Boşsa sona eklenir" value={position} onChange={(event) => setPosition(event.target.value)} />
          </label>
          <label className="field">
            <span>En erken başlangıç (plan tarihi)</span>
            <input type="date" required value={plannedDate} onChange={(event) => setPlannedDate(event.target.value)} />
          </label>
        </div>
        <ErrorAlert error={planState.error} />
        <footer className="form__actions">
          {target.machineId !== null && <button type="button" className="button button--danger-ghost" disabled={planState.isLoading} onClick={unplan}>Plandan çıkar</button>}
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={planState.isLoading || machineId === null}>Kaydet</button>
        </footer>
      </form>
    </Modal>
  )
}

function itemTitle(item: OrmeCapacityItem) {
  return [
    `${item.knitOrderNo} · ${item.orderNo}${item.customerName ? ` · ${item.customerName}` : ''}`,
    `${item.fabricCode} — ${item.fabricName}`,
    `Makinede: ${formatQuantity(item.machineKilograms)} kg${item.runningMachineCount > 1 ? ` (${item.runningMachineCount} makinede)` : ''}`,
    `Başlangıç: ${formatDateTimeShort(item.estimatedStart)}`,
    item.estimatedFinish ? `Tahmini bitiş: ${formatDateTimeShort(item.estimatedFinish)}` : 'Kumaş kartında beklenen kg/saat yok; süre hesaplanamadı',
    `Termin: ${formatDate(item.dueDate)}${item.lateRisk ? ` (${item.lateDays} gün gecikme riski)` : ''}`,
    item.fitWarning ? `Kumaş kartından farklı: ${item.fitWarning}` : null,
  ].filter(Boolean).join('\n')
}

function targetOf(item: OrmeCapacityItem, machineId: number): PlanTarget {
  return { knitOrderId: item.knitOrderId, knitOrderNo: item.knitOrderNo, fabric: `${item.fabricCode} — ${item.fabricName}`, plannedDate: item.plannedDate, machineId }
}

/** Zaman çizelgesi: makine başına bir satır, gün sütunları CSS grid; çubuklar tahmini başlangıç ve bitişe göre konumlanır. */
function Timeline({ plan, canManage, onPlan }: { plan: OrmeCapacityPlan; canManage: boolean; onPlan: (target: PlanTarget) => void }) {
  const days = plan.days
  const start = new Date(`${plan.today}T00:00:00`)
  const dates = Array.from({ length: days }, (_, index) => new Date(start.getTime() + index * DAY_MS))
  const nowOffset = (Date.parse(plan.generatedAt) - start.getTime()) / DAY_MS
  const style = { '--orme-days': days } as CSSProperties
  const position = (from: number, to: number) => {
    const left = Math.max(0, from)
    const right = Math.min(days, to)
    return { left: `${(left / days) * 100}%`, width: `${((right - left) / days) * 100}%` }
  }

  return (
    <div className="orme-timeline">
      <div className="orme-timeline__row orme-timeline__head">
        <div className="orme-timeline__label">Makine</div>
        <div className="orme-timeline__track" style={style}>
          {dates.map((date, index) => (
            <div key={index} className={`orme-timeline__day${date.getDay() === 0 ? ' orme-timeline__day--weekend' : ''}${index === 0 ? ' orme-timeline__day--today' : ''}`} title={date.toLocaleDateString('tr-TR')}>
              {dayMonth.format(date)}
              <br />
              {weekday.format(date)}
            </div>
          ))}
        </div>
      </div>
      {plan.machines.map((machine) => (
        <div key={machine.machineId} className="orme-timeline__row">
          <div className="orme-timeline__label">
            <strong>{machine.machineCode}</strong> <span className="muted">{machine.diameterInch}", E{machine.gauge}</span>
            {machine.openStopCategory && <> <span className="badge badge--warning">{ormeStopCategoryLabels[machine.openStopCategory]}</span></>}
            <div className="orme-loads" title="Önümüzdeki 7 / 14 / 30 günün dolu payı">
              <span>7g {percent(machine.load7Percent)}</span>
              <span>14g {percent(machine.load14Percent)}</span>
              <span>30g {percent(machine.load30Percent)}</span>
            </div>
          </div>
          <div className="orme-timeline__track" style={style}>
            {dates.map((date, index) => <div key={index} className={`orme-timeline__day${date.getDay() === 0 ? ' orme-timeline__day--weekend' : ''}`} />)}
            {nowOffset >= 0 && nowOffset < days && <div className="orme-timeline__now" style={{ left: `${(nowOffset / days) * 100}%` }} aria-hidden="true" />}
            {machine.items.map((item) => {
              const end = item.endOffsetDays
              if (item.startOffsetDays >= days || (end !== null && end <= 0)) {
                return null
              }
              const className = `orme-bar${item.running ? ' orme-bar--running' : ''}${item.lateRisk ? ' orme-bar--late' : ''}${end === null ? ' orme-bar--missing' : ''}`
              const place = end === null ? { left: `${(Math.max(0, item.startOffsetDays) / days) * 100}%` } : position(item.startOffsetDays, end)
              const label = `${item.knitOrderNo} ${item.fabricCode}`
              return canManage && !item.running
                ? <button key={item.knitOrderId} type="button" className={className} style={place} title={itemTitle(item)} onClick={() => onPlan(targetOf(item, machine.machineId))}>{end === null ? '' : label}</button>
                : <div key={item.knitOrderId} className={className} style={place} title={itemTitle(item)}>{end === null ? '' : label}</div>
            })}
          </div>
        </div>
      ))}
    </div>
  )
}

/** Tablo görünümü: makine başına kuyruk, sırayı değiştirme ve başka makineye taşıma. */
function QueueTable({ plan, canManage, onPlan }: { plan: OrmeCapacityPlan; canManage: boolean; onPlan: (target: PlanTarget) => void }) {
  const [reorder, reorderState] = useReorderOrmeMachineQueueMutation()

  const move = (machine: OrmeCapacityMachine, knitOrderId: number, step: -1 | 1) => {
    const queued = machine.items.filter((item) => !item.running).map((item) => item.knitOrderId)
    const index = queued.indexOf(knitOrderId)
    const swap = index + step
    if (index < 0 || swap < 0 || swap >= queued.length) {
      return
    }
    const next = [...queued]
    next[index] = queued[swap]
    next[swap] = knitOrderId
    reorder({ machineId: machine.machineId, knitOrderIds: next })
  }

  return (
    <>
      <ErrorAlert error={reorderState.error} />
      <div className="table-wrap">
        <table className="table table--compact">
          <thead>
            <tr>
              <th>Sıra</th>
              <th>Emir</th>
              <th>Sipariş</th>
              <th>Kumaş</th>
              <th className="table__number">Makinede (kg)</th>
              <th className="table__number">kg/saat</th>
              <th className="table__number">Süre</th>
              <th>Başlangıç</th>
              <th>Tahmini bitiş</th>
              <th>Termin</th>
              <th>Risk</th>
              <th aria-label="İşlem" />
            </tr>
          </thead>
          <tbody>
            {plan.machines.map((machine) => {
              const queued = machine.items.filter((item) => !item.running)
              return (
                <Fragment key={machine.machineId}>
                  <tr className="orme-machine-head">
                    <th colSpan={12} scope="rowgroup">
                      {machine.machineCode} <span className="muted">{machine.machineName} · {knitBedTypeLabels[machine.bedType]}, {machine.diameterInch}", E{machine.gauge}{!machine.isActive && ' · pasif'}</span>
                      {machine.openStopCategory && <> <span className="badge badge--warning">{ormeStopCategoryLabels[machine.openStopCategory]}{machine.openStopReason && `: ${machine.openStopReason}`}</span></>}
                      <span className="muted"> · Doluluk 7/14/30 gün: {percent(machine.load7Percent)} / {percent(machine.load14Percent)} / {percent(machine.load30Percent)}</span>
                      <span className="muted"> · Kuyruk: {hours(machine.queueHours)}{machine.queueEndsAt && `, boşalır ${formatDateTimeShort(machine.queueEndsAt)}`}</span>
                    </th>
                  </tr>
                  {machine.items.map((item) => {
                    const queueIndex = queued.findIndex((row) => row.knitOrderId === item.knitOrderId)
                    return (
                      <tr key={item.knitOrderId}>
                        <td>{item.running ? <span className="badge badge--info">Çalışıyor</span> : item.sequence}</td>
                        <td><Link to={`/orme/knit-orders/${item.knitOrderId}`}><code>{item.knitOrderNo}</code></Link></td>
                        <td><Link to={`/orme/orders/${item.orderId}`}>{item.orderNo}</Link>{item.customerName && <div className="muted">{item.customerName}</div>}</td>
                        <td>{item.fabricCode} — {item.fabricName}{item.fitWarning && <div className="muted">Kumaş kartından farklı: {item.fitWarning}</div>}</td>
                        <td className="table__number">
                          {formatQuantity(item.machineKilograms)}
                          {item.runningMachineCount > 1 && <div className="muted">{item.runningMachineCount} makinede</div>}
                        </td>
                        <td className="table__number">{item.kilogramsPerHour === null ? <span className="muted" title="Kumaş kartında beklenen kg/saat yok">Yok</span> : formatQuantity(item.kilogramsPerHour)}</td>
                        <td className="table__number">{hours(item.durationHours)}</td>
                        <td>{formatDateTimeShort(item.estimatedStart)}</td>
                        <td>{item.estimatedFinish ? formatDateTimeShort(item.estimatedFinish) : '—'}</td>
                        <td>{formatDate(item.dueDate)}</td>
                        <td>{item.lateRisk ? <span className="badge badge--danger">{item.lateDays} gün geç</span> : item.dueDate && item.estimatedFinish ? <span className="badge badge--ok">Zamanında</span> : '—'}</td>
                        <td className="table__actions">
                          {canManage && !item.running && (
                            <>
                              <button type="button" className="button button--icon" aria-label="Yukarı al" title="Yukarı al" disabled={reorderState.isLoading || queueIndex <= 0} onClick={() => move(machine, item.knitOrderId, -1)}><ArrowUp aria-hidden="true" /></button>
                              <button type="button" className="button button--icon" aria-label="Aşağı al" title="Aşağı al" disabled={reorderState.isLoading || queueIndex >= queued.length - 1} onClick={() => move(machine, item.knitOrderId, 1)}><ArrowDown aria-hidden="true" /></button>
                              <button type="button" className="button" onClick={() => onPlan(targetOf(item, machine.machineId))}><MoveRight aria-hidden="true" />Taşı</button>
                            </>
                          )}
                        </td>
                      </tr>
                    )
                  })}
                  {machine.items.length === 0 && <tr><td colSpan={12} className="table__empty">Kuyrukta emir yok.</td></tr>}
                </Fragment>
              )
            })}
            {plan.machines.length === 0 && <tr><td colSpan={12} className="table__empty">Etkin makine yok.</td></tr>}
          </tbody>
        </table>
      </div>
    </>
  )
}

/**
 * Kapasite planı: her makinede çalışan emir ve kuyruğa alınmış açık emirler, tahmini başlangıç ve bitiş, 7/14/30 günlük doluluk
 * ve termini aşma riski. Süre = makinede örülecek kilo ÷ kumaş kartındaki beklenen kg/saat; günlük çalışma saati vardiyalardan gelir.
 */
export function OrmeCapacityPlanPage() {
  const canManage = usePermission(Permissions.OrmePlanningManage)
  const [days, setDays] = useState(30)
  const [hoursInput, setHoursInput] = useState('')
  const [view, setView] = useState<View>('timeline')
  const [target, setTarget] = useState<PlanTarget | null>(null)
  const hoursPerDay = hoursInput.trim() === '' ? undefined : Number(hoursInput)
  const validHours = hoursPerDay === undefined || (hoursPerDay >= 1 && hoursPerDay <= 24)
  const { data, error, isLoading, isFetching } = useGetOrmeCapacityPlanQuery({ days, ...(hoursPerDay === undefined ? {} : { hoursPerDay }) }, { skip: !validHours })
  const lateCount = data?.machines.reduce((sum, machine) => sum + machine.lateRiskCount, 0) ?? 0
  const missingCount = data?.machines.reduce((sum, machine) => sum + machine.missingRateCount, 0) ?? 0

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
            <div><dt>Makine</dt><dd>{data.machines.length}</dd></div>
            <div><dt>Termin riski</dt><dd>{lateCount > 0 ? <span className="orme-negative">{lateCount} emir</span> : 'Yok'}</dd></div>
            <div><dt>Planlanmamış emir</dt><dd>{data.unplanned.length}</dd></div>
            {missingCount > 0 && <div><dt>Hızı tanımsız</dt><dd>{missingCount} emir</dd></div>}
          </dl>
          <p className="muted">
            Süre = makinede örülecek kilo ÷ kumaş kartındaki beklenen kg/saat; birden çok makinede çalışan emrin kalanı makinelere eşit bölünür.
            Üretim saati günlük çalışma saatine göre takvime yayılır. Kuyruk sırası ve makine değişikliği yalnızca plandır; makineye fiili atama
            makine panosundan yapılır. Hesaplama: {formatDateTimeShort(data.generatedAt)}{isFetching && ' (yenileniyor)'}.
          </p>
          {view === 'timeline' && (
            <>
              <div className="orme-legend">
                <span><span className="orme-bar orme-bar--running" />Çalışan</span>
                <span><span className="orme-bar" />Kuyrukta</span>
                <span><span className="orme-bar orme-bar--late" />Termin riski</span>
                <span><span className="orme-bar orme-bar--missing" />Hızı tanımsız</span>
                {canManage && <span>Kuyruktaki çubuğa tıklayarak makinesini ya da sırasını değiştirin.</span>}
              </div>
              <Timeline plan={data} canManage={canManage} onPlan={setTarget} />
            </>
          )}
          {view === 'table' && <QueueTable plan={data} canManage={canManage} onPlan={setTarget} />}

          <h2>Planlanmamış emirler</h2>
          <div className="table-wrap">
            <table className="table table--compact">
              <thead>
                <tr>
                  <th>Emir</th>
                  <th>Sipariş</th>
                  <th>Kumaş</th>
                  <th>Plaka</th>
                  <th>Plan tarihi</th>
                  <th>Termin</th>
                  <th className="table__number">Kalan (kg)</th>
                  <th className="table__number">Süre</th>
                  <th aria-label="İşlem" />
                </tr>
              </thead>
              <tbody>
                {data.unplanned.map((knit) => (
                  <tr key={knit.knitOrderId}>
                    <td><Link to={`/orme/knit-orders/${knit.knitOrderId}`}><code>{knit.knitOrderNo}</code></Link></td>
                    <td><Link to={`/orme/orders/${knit.orderId}`}>{knit.orderNo}</Link>{knit.customerName && <div className="muted">{knit.customerName}</div>}</td>
                    <td>{knit.fabricCode} — {knit.fabricName}{knit.note && <div className="muted">{knit.note}</div>}</td>
                    <td>{knitBedTypeLabels[knit.bedType]}{(knit.diameterInch !== null || knit.gauge !== null) && <div className="muted">{knit.diameterInch !== null && `${knit.diameterInch}"`}{knit.gauge !== null && ` E${knit.gauge}`}</div>}</td>
                    <td>{formatDate(knit.plannedDate)}</td>
                    <td>{formatDate(knit.dueDate)}</td>
                    <td className="table__number">{formatQuantity(knit.remainingKilograms)}</td>
                    <td className="table__number">{knit.durationHours === null ? <span className="muted" title="Kumaş kartında beklenen kg/saat yok">Hız yok</span> : hours(knit.durationHours)}</td>
                    <td className="table__actions">
                      {canManage && (
                        <button
                          type="button"
                          className="button"
                          onClick={() => setTarget({ knitOrderId: knit.knitOrderId, knitOrderNo: knit.knitOrderNo, fabric: `${knit.fabricCode} — ${knit.fabricName}`, plannedDate: knit.plannedDate, machineId: null })}
                        >
                          Makineye planla
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
                {data.unplanned.length === 0 && <tr><td colSpan={9} className="table__empty">Bütün açık emirler bir makinede ya da kuyrukta.</td></tr>}
              </tbody>
            </table>
          </div>
        </>
      )}
      {target && <PlanDialog target={target} onClose={() => setTarget(null)} />}
    </section>
  )
}
