import type { CuttingPlanStatus } from './cuttingApi'

export const cuttingPlanStatusPresentation: Record<CuttingPlanStatus, { label: string; icon: string; tone: string }> = {
  DRAFT: { label: 'Taslak', icon: '✎', tone: 'neutral' },
  CUT: { label: 'Kesildi', icon: '■', tone: 'ok' },
  CANCELLED: { label: 'İptal', icon: '✕', tone: 'danger' },
}

/** Varsayılan kat başı uç payı (m); sunucudaki değerle aynıdır. */
export const DEFAULT_END_ALLOWANCE = 0.03

/** (Pastal boyu + kat başı pay) × kat; metre, 3 ondalık (sunucudaki hesapla aynı). */
export function fabricFor(markerLength: number, endAllowance: number, plies: number): number {
  return Math.round((markerLength + endAllowance) * plies * 1000) / 1000
}

/**
 * Kalan adetleri karşılayan en küçük kat sayısı: her beden için kalan ÷ oran yukarı yuvarlanır, en büyüğü alınır.
 * Kalanı ya da oranı olmayan beden hesaba girmez.
 */
export function suggestPlies(sizes: { ratio: number; remaining: number }[]): number {
  return sizes
    .filter((size) => size.ratio > 0 && size.remaining > 0)
    .reduce((max, size) => Math.max(max, Math.ceil(size.remaining / size.ratio)), 0)
}

const meters = new Intl.NumberFormat('tr-TR', { maximumFractionDigits: 3 })

/** "12,345 m" */
export function formatMeters(value: number): string {
  return `${meters.format(value)} m`
}

/** Formdaki ondalık metni sayıya çevirir (virgül de ayırıcıdır); geçersizse null. */
export function parseDecimal(text: string): number | null {
  const normalized = text.trim().replace(',', '.')
  return /^\d+(\.\d+)?$/.test(normalized) ? Number(normalized) : null
}

/** Topun ton / parti bilgisi: "ton B · parti 2231". */
export function shadeText(lot: { shade: string | null; batchNo: string | null }): string {
  return [lot.shade && `ton ${lot.shade}`, lot.batchNo && `parti ${lot.batchNo}`].filter(Boolean).join(' · ') || '—'
}
