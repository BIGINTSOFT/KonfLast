import type { PlanLoadStatus, PlanningRisk } from './planningApi'

interface Presentation {
  label: string
  icon: string
  tone: string
}

/** Risk ve yük durumu renge bırakılmaz: ikon ve metin de taşır. */
export const riskPresentation: Record<PlanningRisk, Presentation> = {
  LATE: { label: 'Gecikmiş', icon: '!', tone: 'danger' },
  PLAN_AFTER_DUE_DATE: { label: 'Plan terminden sonra bitiyor', icon: '▼', tone: 'danger' },
  UNPLANNED: { label: 'Planlanmamış adet var', icon: '◷', tone: 'warning' },
  ON_TRACK: { label: 'Yolunda', icon: '✓', tone: 'ok' },
}

export const loadStatusPresentation: Record<PlanLoadStatus, Presentation> = {
  OK: { label: 'Sığıyor', icon: '✓', tone: 'ok' },
  TIGHT: { label: 'Sıkışık', icon: '●', tone: 'warning' },
  OVERLOADED: { label: 'Sığmıyor', icon: '▼', tone: 'danger' },
  NO_CAPACITY: { label: 'Bantta makine eksik', icon: '!', tone: 'danger' },
  NO_ROUTE: { label: 'Rota yok', icon: '?', tone: 'neutral' },
}

/** "Bugün", "3 gün kaldı", "2 gün gecikti". */
export function dueText(daysToDue: number): string {
  if (daysToDue === 0) {
    return 'Bugün'
  }
  return daysToDue > 0 ? `${daysToDue} gün kaldı` : `${-daysToDue} gün gecikti`
}

const hours = new Intl.NumberFormat('tr-TR', { maximumFractionDigits: 1 })

/** "12,5 sa"; değer yoksa tire. */
export function formatHours(value: number | null): string {
  return value === null ? '—' : `${hours.format(value)} sa`
}

/** Bugünden başlayıp verilen gün sayısı kadar ileriyi kapsayan yerel gün aralığı (yyyy-aa-gg). */
export function horizonDates(today: string, days: number): { fromDate: string; toDate: string } {
  const end = new Date(`${today}T00:00:00`)
  end.setDate(end.getDate() + days - 1)
  const pad = (value: number) => String(value).padStart(2, '0')
  return { fromDate: today, toDate: `${end.getFullYear()}-${pad(end.getMonth() + 1)}-${pad(end.getDate())}` }
}
