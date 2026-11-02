import { ArrowDown, ArrowUp, CalendarRange, MoveRight, Table2 } from 'lucide-react'
import { Fragment, useState } from 'react'
import type { CSSProperties, FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { isRequestError } from '../../api/envelope'
import { ErrorAlert } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDate, formatDateTimeShort, formatQuantity } from '../../utils/format'
import { OVER_CAPACITY } from './boyahaneBatchesApi'
import { orderProcessTypeLabels } from './boyahaneOrdersApi'
import {
  useGetBoyahaneCapacityOptionsQuery,
  useGetBoyahaneCapacityPlanQuery,
  usePlanBoyahaneBatchMutation,
  useReorderBoyahaneMachineQueueMutation,
} from './boyahanePlanningApi'
import type { BoyahaneCapacityItem, BoyahaneCapacityMachine, BoyahaneCapacityPlan } from './boyahanePlanningApi'
import { dyeMachineStatusLabels, dyeMachineTypeLabels } from './boyahanePresentation'
import { processMinutesSourceLabels } from './boyahaneQuotationsApi'
import './boyahanePlanning.css'

type View = 'timeline' | 'table'

const DAY_MS = 24 * 60 * 60 * 1000
const weekday = new Intl.DateTimeFormat('tr-TR', { weekday: 'short' })
const dayMonth = new Intl.DateTimeFormat('tr-TR', { day: '2-digit', month: '2-digit' })

const percent = (value: number) => `%${formatQuantity(value)}`
const duration = (minutes: number) => (minutes >= 60 ? `${formatQuantity(Math.round((minutes / 60) * 10) / 10)} sa` : `${formatQuantity(Math.round(minutes))} dk`)

/** Taşınacak planlı parti. */
interface PlanTarget {
  batchId: number
  batchNo: string
  title: string
  plannedDate: string | null
  machineId: number
}

/**
 * Partiyi kuyrukta başka yere ya da başka makineye taşıma. Yalnızca partinin asıl operasyonunu (boyamada boyama) yapabilen etkin
 * makineler listelenir; kapasiteyi aşan makine ayrı yetki ve gerekçe ister.
 */
function PlanDialog({ target, onClose }: { target: PlanTarget; onClose: () => void }) {
  const canOverride = usePermission(Permissions.BoyahaneBatchCapacityOverride)
  const { data, error, isLoading } = useGetBoyahaneCapacityOptionsQuery(target.batchId)
  const [plan, planState] = usePlanBoyahaneBatchMutation()
  const [machineId, setMachineId] = useState(target.machineId)
  const [plannedDate, setPlannedDate] = useState(target.plannedDate ?? '')
  const [position, setPosition] = useState('')
  const [note, setNote] = useState('')
  const selected = data?.machines.find((machine) => machine.machineId === machineId)
  const overCapacity = (selected !== undefined && !selected.capacityOk) || (isRequestError(planState.error) && planState.error.code === OVER_CAPACITY)

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await plan({
      batchId: target.batchId,
      machineId,
      plannedDate: plannedDate || null,
      position: position.trim() === '' ? null : Number(position),
      capacityOverrideNote: note.trim() || null,
    })
    if (!('error' in result)) {
      onClose()
    }
  }

  return (
    <Modal title={`Makine planı: ${target.batchNo}`} onClose={onClose} variant="drawer">
      <form className="form" onSubmit={submit}>
        <p className="muted">
          {target.title}
          {data && <> · {formatQuantity(data.totalKilograms)} kg{data.mainOperation && ` · makinede yapılması gereken: ${data.mainOperation}`}</>}
        </p>
        {isLoading && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={error} />
        {data && (
          <div className="table-wrap">
            <table className="table table--compact">
              <thead>
                <tr>
                  <th aria-label="Seç" />
                  <th>Makine</th>
                  <th className="table__number">Kapasite (kg)</th>
                  <th className="table__number">Kuyruk</th>
                  <th>Boşalır</th>
                  <th>Uyarı</th>
                </tr>
              </thead>
              <tbody>
                {data.machines.map((option) => (
                  <tr key={option.machineId}>
                    <td><input type="radio" name="machine" aria-label={`${option.code} seç`} checked={machineId === option.machineId} onChange={() => setMachineId(option.machineId)} /></td>
                    <td>
                      <strong>{option.code}</strong> {option.name}
                      <div className="muted">{dyeMachineTypeLabels[option.machineType] ?? option.machineType}{option.current && ' · şu anki plan'}</div>
                    </td>
                    <td className="table__number">{formatQuantity(option.capacityKg)}</td>
                    <td className="table__number">{option.queueCount}</td>
                    <td>{option.queueEndsAt ? formatDateTimeShort(option.queueEndsAt) : <span className="muted">Boş</span>}</td>
                    <td>
                      {!option.capacityOk && <span className="badge badge--warning">Kapasite aşılır</span>}
                      {option.status !== 'AVAILABLE' && <> <span className="badge badge--warning">{dyeMachineStatusLabels[option.status]}</span></>}
                      {option.capacityOk && option.status === 'AVAILABLE' && <span className="muted">—</span>}
                    </td>
                  </tr>
                ))}
                {data.machines.length === 0 && <tr><td colSpan={6} className="table__empty">Bu partiyi işleyebilecek etkin makine yok.</td></tr>}
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
            <span>Plan tarihi (en erken başlangıç)</span>
            <input type="date" value={plannedDate} onChange={(event) => setPlannedDate(event.target.value)} />
          </label>
        </div>
        {overCapacity && (
          canOverride ? (
            <label className="field">
              <span>Kapasite aşım gerekçesi</span>
              <input maxLength={500} value={note} onChange={(event) => setNote(event.target.value)} />
            </label>
          ) : <p className="alert alert--info">Parti bu makinenin kapasitesini aşıyor; aşım için yetkili onayı gerekir.</p>
        )}
        <ErrorAlert error={planState.error} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={planState.isLoading}>Kaydet</button>
        </footer>
      </form>
    </Modal>
  )
}

function itemTitle(item: BoyahaneCapacityItem) {
  return [
    `${item.batchNo} · ${item.orderNos}${item.customerNames ? ` · ${item.customerNames}` : ''}`,
    `${orderProcessTypeLabels[item.processType]} · ${item.itemCode}${item.colorName ? ` · ${item.colorName}` : ''} · ${formatQuantity(item.totalKilograms)} kg`,
    `Süre: ${duration(item.durationMinutes)} (${processMinutesSourceLabels[item.durationSource]})${item.started ? `, kalan ${duration(item.remainingMinutes)}` : ''}`,
    `Başlangıç: ${formatDateTimeShort(item.estimatedStart)}`,
    `Tahmini bitiş: ${formatDateTimeShort(item.estimatedFinish)}`,
    `Termin: ${formatDate(item.dueDate)}${item.lateRisk ? ` (${item.lateDays} gün gecikme riski)` : ''}`,
    item.overCapacity ? `Kapasite aşımı${item.capacityOverrideBy ? ` (onaylayan ${item.capacityOverrideBy})` : ''}` : null,
    item.overrun ? 'Tahmini süre aşıldı' : null,
  ].filter(Boolean).join('\n')
}

function targetOf(item: BoyahaneCapacityItem, machineId: number): PlanTarget {
  return {
    batchId: item.batchId,
    batchNo: item.batchNo,
    title: `${orderProcessTypeLabels[item.processType]} · ${item.itemCode}${item.colorName ? ` · ${item.colorName}` : ''}`,
    plannedDate: item.plannedDate,
    machineId,
  }
}

/** Zaman çizelgesi: makine başına bir satır, gün sütunları CSS grid; çubuklar tahmini başlangıç ve bitişe göre konumlanır. */
function Timeline({ plan, canManage, onPlan }: { plan: BoyahaneCapacityPlan; canManage: boolean; onPlan: (target: PlanTarget) => void }) {
  const days = plan.days
  const start = new Date(`${plan.today}T00:00:00`)
  const dates = Array.from({ length: days }, (_, index) => new Date(start.getTime() + index * DAY_MS))
  const nowOffset = (Date.parse(plan.generatedAt) - start.getTime()) / DAY_MS
  const style = { '--boyahane-days': days } as CSSProperties
  const position = (from: number, to: number) => {
    const left = Math.max(0, from)
    const right = Math.min(days, to)
    return { left: `${(left / days) * 100}%`, width: `${((right - left) / days) * 100}%` }
  }

  return (
    <div className="boyahane-timeline">
      <div className="boyahane-timeline__row boyahane-timeline__head">
        <div className="boyahane-timeline__label">Makine</div>
        <div className="boyahane-timeline__track" style={style}>
          {dates.map((date, index) => (
            <div key={index} className={`boyahane-timeline__day${date.getDay() === 0 ? ' boyahane-timeline__day--weekend' : ''}${index === 0 ? ' boyahane-timeline__day--today' : ''}`} title={date.toLocaleDateString('tr-TR')}>
              {dayMonth.format(date)}
              <br />
              {weekday.format(date)}
            </div>
          ))}
        </div>
      </div>
      {plan.machines.map((machine) => (
        <div key={machine.machineId} className="boyahane-timeline__row">
          <div className="boyahane-timeline__label">
            <strong>{machine.machineCode}</strong> <span className="muted">{formatQuantity(machine.capacityKg)} kg</span>
            {machine.status !== 'AVAILABLE' && <> <span className="badge badge--warning">{dyeMachineStatusLabels[machine.status]}</span></>}
            <div className="boyahane-loads" title="Önümüzdeki 7 / 14 / 30 günün dolu payı">
              <span>7g {percent(machine.load7Percent)}</span>
              <span>14g {percent(machine.load14Percent)}</span>
              <span>30g {percent(machine.load30Percent)}</span>
            </div>
          </div>
          <div className="boyahane-timeline__track" style={style}>
            {dates.map((date, index) => <div key={index} className={`boyahane-timeline__day${date.getDay() === 0 ? ' boyahane-timeline__day--weekend' : ''}`} />)}
            {nowOffset >= 0 && nowOffset < days && <div className="boyahane-timeline__now" style={{ left: `${(nowOffset / days) * 100}%` }} aria-hidden="true" />}
            {machine.items.map((item) => {
              if (item.startOffsetDays >= days || item.endOffsetDays <= 0) {
                return null
              }
              const className = `boyahane-bar${item.started ? ' boyahane-bar--started' : ''}${item.overCapacity ? ' boyahane-bar--capacity' : ''}${item.lateRisk ? ' boyahane-bar--late' : ''}`
              const place = position(item.startOffsetDays, Math.max(item.endOffsetDays, item.startOffsetDays + 0.02))
              const label = `${item.batchNo} ${item.colorName ?? item.itemCode}`
              return canManage && !item.started
                ? <button key={item.batchId} type="button" className={className} style={place} title={itemTitle(item)} onClick={() => onPlan(targetOf(item, machine.machineId))}>{label}</button>
                : <div key={item.batchId} className={className} style={place} title={itemTitle(item)}>{label}</div>
            })}
          </div>
        </div>
      ))}
    </div>
  )
}

/** Tablo görünümü: makine başına kuyruk, sırayı değiştirme ve başka makineye taşıma. */
function QueueTable({ plan, canManage, onPlan }: { plan: BoyahaneCapacityPlan; canManage: boolean; onPlan: (target: PlanTarget) => void }) {
  const [reorder, reorderState] = useReorderBoyahaneMachineQueueMutation()

  const move = (machine: BoyahaneCapacityMachine, batchId: number, step: -1 | 1) => {
    const queued = machine.items.filter((item) => !item.started).map((item) => item.batchId)
    const index = queued.indexOf(batchId)
    const swap = index + step
    if (index < 0 || swap < 0 || swap >= queued.length) {
      return
    }
    const next = [...queued]
    next[index] = queued[swap]
    next[swap] = batchId
    reorder({ machineId: machine.machineId, batchIds: next })
  }

  return (
    <>
      <ErrorAlert error={reorderState.error} />
      <div className="table-wrap">
        <table className="table table--compact">
          <thead>
            <tr>
              <th>Sıra</th>
              <th>Parti</th>
              <th>Sipariş</th>
              <th>İşlem / kart</th>
              <th className="table__number">Kilo</th>
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
              const queued = machine.items.filter((item) => !item.started)
              return (
                <Fragment key={machine.machineId}>
                  <tr className="boyahane-machine-head">
                    <th colSpan={11} scope="rowgroup">
                      {machine.machineCode} <span className="muted">{machine.machineName} · {dyeMachineTypeLabels[machine.machineType] ?? machine.machineType}, {formatQuantity(machine.capacityKg)} kg{!machine.isActive && ' · pasif'}</span>
                      {machine.status !== 'AVAILABLE' && <> <span className="badge badge--warning">{dyeMachineStatusLabels[machine.status]}</span></>}
                      <span className="muted"> · Doluluk 7/14/30 gün: {percent(machine.load7Percent)} / {percent(machine.load14Percent)} / {percent(machine.load30Percent)}</span>
                      <span className="muted"> · Kuyruk: {duration(machine.queueMinutes)}{machine.queueEndsAt && `, boşalır ${formatDateTimeShort(machine.queueEndsAt)}`}</span>
                    </th>
                  </tr>
                  {machine.items.map((item) => {
                    const queueIndex = queued.findIndex((row) => row.batchId === item.batchId)
                    return (
                      <tr key={item.batchId}>
                        <td>{item.started ? <span className="badge badge--info">{item.running ? 'Makinede' : 'İşlemde'}</span> : item.sequence}</td>
                        <td>
                          <Link to={`/boyahane/batches/${item.batchId}`}><code>{item.batchNo}</code></Link>
                          {item.currentStep && <div className="muted">{item.currentStep}</div>}
                        </td>
                        <td>{item.orderNos}{item.customerNames && <div className="muted">{item.customerNames}</div>}</td>
                        <td>{orderProcessTypeLabels[item.processType]} · {item.itemCode}{item.colorName && <div className="muted">{item.colorName}</div>}</td>
                        <td className="table__number">
                          {formatQuantity(item.totalKilograms)}
                          {item.overCapacity && <div><span className="badge badge--warning" title={item.capacityOverrideBy ? `Onaylayan: ${item.capacityOverrideBy}` : 'Onaysız'}>Kapasite aşımı</span></div>}
                        </td>
                        <td className="table__number" title={processMinutesSourceLabels[item.durationSource]}>
                          {duration(item.started ? item.remainingMinutes : item.durationMinutes)}
                          {item.durationSource === 'DEFAULT' && <div className="muted">varsayılan</div>}
                          {item.started && <div className="muted">kalan</div>}
                        </td>
                        <td>{formatDateTimeShort(item.estimatedStart)}</td>
                        <td>{formatDateTimeShort(item.estimatedFinish)}{item.overrun && <div><span className="badge badge--warning">Süre aşıldı</span></div>}</td>
                        <td>{formatDate(item.dueDate)}</td>
                        <td>{item.lateRisk ? <span className="badge badge--danger">{item.lateDays} gün geç</span> : item.dueDate ? <span className="badge badge--ok">Zamanında</span> : '—'}</td>
                        <td className="table__actions">
                          {canManage && !item.started && (
                            <>
                              <button type="button" className="button button--icon" aria-label="Yukarı al" title="Yukarı al" disabled={reorderState.isLoading || queueIndex <= 0} onClick={() => move(machine, item.batchId, -1)}><ArrowUp aria-hidden="true" /></button>
                              <button type="button" className="button button--icon" aria-label="Aşağı al" title="Aşağı al" disabled={reorderState.isLoading || queueIndex >= queued.length - 1} onClick={() => move(machine, item.batchId, 1)}><ArrowDown aria-hidden="true" /></button>
                              <button type="button" className="button" onClick={() => onPlan(targetOf(item, machine.machineId))}><MoveRight aria-hidden="true" />Taşı</button>
                            </>
                          )}
                        </td>
                      </tr>
                    )
                  })}
                  {machine.items.length === 0 && <tr><td colSpan={11} className="table__empty">Kuyrukta parti yok.</td></tr>}
                </Fragment>
              )
            })}
            {plan.machines.length === 0 && <tr><td colSpan={11} className="table__empty">Etkin makine yok.</td></tr>}
          </tbody>
        </table>
      </div>
    </>
  )
}

/**
 * Boyahane kapasite planı: her makinede işlemdeki ve planlı partiler, tahmini başlangıç ve bitiş, 7/14/30 günlük doluluk, kapasite
 * aşımı ve termini aşma riski. Parti süresi rotadaki operasyonların standart sürelerinden, yoksa işlem türünün varsayılanından gelir;
 * günlük çalışma saati vardiyalardan hesaplanır.
 */
export function BoyahaneCapacityPlanPage() {
  const canManage = usePermission(Permissions.BoyahaneBatchManage)
  const [days, setDays] = useState(14)
  const [hoursInput, setHoursInput] = useState('')
  const [view, setView] = useState<View>('timeline')
  const [target, setTarget] = useState<PlanTarget | null>(null)
  const hoursPerDay = hoursInput.trim() === '' ? undefined : Number(hoursInput)
  const validHours = hoursPerDay === undefined || (hoursPerDay >= 1 && hoursPerDay <= 24)
  const { data, error, isLoading, isFetching } = useGetBoyahaneCapacityPlanQuery({ days, ...(hoursPerDay === undefined ? {} : { hoursPerDay }) }, { skip: !validHours })
  const lateCount = data?.machines.reduce((sum, machine) => sum + machine.lateRiskCount, 0) ?? 0
  const overCapacityCount = data?.machines.reduce((sum, machine) => sum + machine.overCapacityCount, 0) ?? 0
  const defaultCount = data?.machines.reduce((sum, machine) => sum + machine.items.filter((item) => item.durationSource === 'DEFAULT').length, 0) ?? 0
  const batchCount = data?.machines.reduce((sum, machine) => sum + machine.items.length, 0) ?? 0

  return (
    <section className="page">
      <h1 className="visually-hidden">Makine Planı</h1>
      <div className="toolbar">
        <label className="field">
          <span>Görünen gün</span>
          <select value={days} onChange={(event) => setDays(Number(event.target.value))}>
            <option value={7}>7 gün</option>
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
            <div><dt>Parti</dt><dd>{batchCount}</dd></div>
            <div><dt>Termin riski</dt><dd>{lateCount > 0 ? <span className="boyahane-negative">{lateCount} parti</span> : 'Yok'}</dd></div>
            <div><dt>Kapasite aşımı</dt><dd>{overCapacityCount > 0 ? <span className="boyahane-negative">{overCapacityCount} parti</span> : 'Yok'}</dd></div>
            {defaultCount > 0 && <div><dt>Varsayılan süreli</dt><dd>{defaultCount} parti</dd></div>}
          </dl>
          <p className="muted">
            Parti süresi rotadaki operasyonlardan bu makinede yapılanların standart sürelerinin toplamıdır; standart süre yoksa Maliyet
            Parametrelerindeki işlem türü süresi kullanılır. İşlemdeki partide kalan süre, partinin başlangıcından geçen süre düşülerek bulunur.
            Süre günlük çalışma saatine göre takvime yayılır. Hesaplama: {formatDateTimeShort(data.generatedAt)}{isFetching && ' (yenileniyor)'}.
          </p>
          {view === 'timeline' && (
            <>
              <div className="boyahane-legend">
                <span><span className="boyahane-bar boyahane-bar--started" />İşlemde</span>
                <span><span className="boyahane-bar" />Planlı</span>
                <span><span className="boyahane-bar boyahane-bar--late" />Termin riski</span>
                <span><span className="boyahane-bar boyahane-bar--capacity" />Kapasite aşımı</span>
                {canManage && <span>Planlı partinin çubuğuna tıklayarak makinesini ya da sırasını değiştirin.</span>}
              </div>
              <Timeline plan={data} canManage={canManage} onPlan={setTarget} />
            </>
          )}
          {view === 'table' && <QueueTable plan={data} canManage={canManage} onPlan={setTarget} />}
        </>
      )}
      {target && <PlanDialog target={target} onClose={() => setTarget(null)} />}
    </section>
  )
}
