import type { OperatorState, OutputGroupBy, PerformanceGroupBy } from './trackingApi'

/** Operatör durumları birbirine karıştırılmaz: mola, ara, arıza ve demet bekleme ayrı gösterilir. */
export const operatorStatePresentation: Record<OperatorState, { label: string; icon: string; tone: string }> = {
  OFFLINE: { label: 'Oturum yok', icon: '○', tone: 'neutral' },
  WORKING: { label: 'Hazır', icon: '●', tone: 'info' },
  PRODUCING: { label: 'Üretiyor', icon: '▶', tone: 'ok' },
  WAITING_FOR_BUNDLE: { label: 'Demet bekliyor', icon: '◷', tone: 'warning' },
  PAUSED: { label: 'Ara verdi', icon: '‖', tone: 'warning' },
  BREAK: { label: 'Molada', icon: '☕', tone: 'info' },
  MACHINE_BREAKDOWN: { label: 'Makine arızası', icon: '!', tone: 'danger' },
}

export const groupByLabels: Record<OutputGroupBy, string> = {
  OPERATOR: 'Operatör',
  MACHINE: 'Makine',
  OPERATION: 'Operasyon',
}

/**
 * Kullanıcının seçtiği yerel gün aralığını (iki tarih de dahil) API'nin beklediği UTC anlarına çevirir:
 * ilk günün yerel gece yarısı (dahil) → son günü izleyen günün yerel gece yarısı (hariç).
 * Tarihler geçersizse ya da bitiş başlangıçtan önceyse null döner.
 */
export function dayRangeToUtc(fromDate: string, toDate: string): { from: string; to: string } | null {
  const from = new Date(`${fromDate}T00:00:00`)
  const to = new Date(`${toDate}T00:00:00`)
  if (Number.isNaN(from.getTime()) || Number.isNaN(to.getTime()) || to < from) {
    return null
  }

  to.setDate(to.getDate() + 1)
  return { from: from.toISOString(), to: to.toISOString() }
}

/** Yüzdeyi 0-100 aralığına sıkıştırır (ilerleme çubuğu için). */
export function percentOf(part: number, whole: number): number {
  return whole <= 0 ? 0 : Math.max(0, Math.min(100, Math.round((part * 1000) / whole) / 10))
}

export const performanceGroupLabels: Record<PerformanceGroupBy, string> = {
  OPERATOR: 'Operatör',
  MACHINE: 'Makine',
  LINE: 'Bant',
  OPERATION: 'Operasyon (darboğaz)',
}

/** "%107,8"; değer yoksa tire. */
export function formatPercent(value: number | null): string {
  return value === null ? '—' : `%${value.toLocaleString('tr-TR', { maximumFractionDigits: 1 })}`
}

/**
 * Verimin gösterim tonu: hedefe ulaşan (≥ %100), yakın (≥ %85) ve altında kalan. Eşikler yalnızca
 * görsel vurgudur; ikon da değiştiği için anlam renge bağlı kalmaz.
 */
export function efficiencyTone(percent: number): { tone: string; icon: string } {
  if (percent >= 100) {
    return { tone: 'ok', icon: '▲' }
  }
  return percent >= 85 ? { tone: 'warning', icon: '●' } : { tone: 'danger', icon: '▼' }
}
