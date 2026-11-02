import type { Inspection, QualityResult, ReworkStatus } from './qualityApi'

interface Presentation {
  label: string
  icon: string
  tone: string
}

export const qualityResultPresentation: Record<QualityResult, Presentation> = {
  PASSED: { label: 'Geçti', icon: '✓', tone: 'ok' },
  REJECTED: { label: 'Ret', icon: '✕', tone: 'danger' },
  REWORK: { label: 'Tamir', icon: '↺', tone: 'warning' },
  HOLD: { label: 'Bekletme', icon: '!', tone: 'warning' },
}

export const reworkStatusPresentation: Record<ReworkStatus, Presentation> = {
  OPEN: { label: 'Tamir bekliyor', icon: '◷', tone: 'warning' },
  DONE: { label: 'Tekrar kontrol bekliyor', icon: '○', tone: 'info' },
  APPROVED: { label: 'Onaylandı', icon: '✓', tone: 'ok' },
  REJECTED: { label: 'Uygun bulunmadı', icon: '✕', tone: 'danger' },
}

/** Kontrolde bulunan hataların kısa dökümü: "Dikiş hatası ×4, Leke ×1". */
export function defectsText(inspection: Pick<Inspection, 'defects'>): string {
  return inspection.defects.length === 0
    ? '—'
    : inspection.defects.map((defect) => `${defect.defectTypeName} ×${defect.quantity}`).join(', ')
}

/** Form girdisini tam sayıya çevirir; boş ya da geçersiz değer 0 sayılır. */
export function toQuantity(value: string): number {
  const parsed = Number.parseInt(value, 10)
  return Number.isFinite(parsed) && parsed > 0 ? parsed : 0
}
