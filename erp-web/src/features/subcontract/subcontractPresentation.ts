import { formatMoney } from '../../utils/money'
import type { CurrencyAmount, SubcontractSendStatus } from './subcontractApi'

export const subcontractSendStatusPresentation: Record<SubcontractSendStatus, { label: string; icon: string; tone: string }> = {
  OPEN: { label: 'Fasonda', icon: '→', tone: 'info' },
  PARTIALLY_RETURNED: { label: 'Kısmen döndü', icon: '◐', tone: 'warning' },
  CLOSED: { label: 'Kapandı', icon: '■', tone: 'ok' },
  CANCELLED: { label: 'İptal', icon: '✕', tone: 'danger' },
}

/** Para birimi başına tutarlar: "1.250,00 TRY · 300,00 USD"; hiç yoksa tire. */
export function formatAmounts(amounts: CurrencyAmount[]): string {
  return amounts.length === 0 ? '—' : amounts.map((item) => formatMoney(item.amount, item.currency)).join(' · ')
}

/** Verilen günden bugüne geçen gün (yerel gün). */
export function daysSince(isoDate: string): number {
  const start = new Date(`${isoDate}T00:00:00`)
  const now = new Date()
  const today = new Date(now.getFullYear(), now.getMonth(), now.getDate())
  return Math.max(0, Math.round((today.getTime() - start.getTime()) / 86_400_000))
}

/** Sayı kutusundaki tam sayı; boş ya da geçersizse 0. */
export function parseCount(text: string): number {
  const value = Number(text)
  return Number.isInteger(value) && value > 0 ? value : 0
}
