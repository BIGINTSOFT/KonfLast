import type { BundleEvent, BundleEventType, BundleOperation, BundleOperationStatus, BundleStatus } from './bundlesApi'

interface Presentation {
  label: string
  icon: string
  tone: string
}

export const bundleStatusPresentation: Record<BundleStatus, Presentation> = {
  CREATED: { label: 'Oluşturuldu', icon: '✎', tone: 'neutral' },
  WAITING: { label: 'Bekliyor', icon: '◷', tone: 'neutral' },
  IN_PRODUCTION: { label: 'Üretimde', icon: '▶', tone: 'ok' },
  PARTIALLY_COMPLETED: { label: 'Kısmen tamamlandı', icon: '◐', tone: 'info' },
  COMPLETED: { label: 'Tamamlandı', icon: '■', tone: 'ok' },
  QUALITY_HOLD: { label: 'Kalite beklemede', icon: '!', tone: 'warning' },
  REWORK: { label: 'Tamirde', icon: '↺', tone: 'warning' },
  CANCELLED: { label: 'İptal', icon: '✕', tone: 'danger' },
  CLOSED: { label: 'Kapandı', icon: '▣', tone: 'neutral' },
}

export const operationStatusPresentation: Record<BundleOperationStatus, Presentation> = {
  WAITING: { label: 'Öncülü bekliyor', icon: '◷', tone: 'neutral' },
  READY: { label: 'Hazır', icon: '○', tone: 'info' },
  IN_PROGRESS: { label: 'İşlemde', icon: '▶', tone: 'ok' },
  PAUSED: { label: 'Duraklatıldı', icon: '‖', tone: 'warning' },
  COMPLETED: { label: 'Tamamlandı', icon: '✓', tone: 'ok' },
  QUALITY_HOLD: { label: 'Kalite beklemede', icon: '!', tone: 'warning' },
  CANCELLED: { label: 'İptal', icon: '✕', tone: 'danger' },
}

/** Adımları sunucunun hesapladığı aşamaya göre gruplar (akış görünümü için). */
export function groupOperationsByStage(operations: BundleOperation[]): BundleOperation[][] {
  const stages: BundleOperation[][] = []
  for (const operation of [...operations].sort((a, b) => a.sequenceNo - b.sequenceNo)) {
    ;(stages[operation.stage] ??= []).push(operation)
  }
  return stages.filter(Boolean)
}

export const eventTypeLabels: Record<BundleEventType, string> = {
  OPERATION_STARTED: 'Operasyon başlatıldı',
  OPERATION_RESUMED: 'Kalan adet için devam edildi',
  OPERATION_PAUSED: 'Operasyon bırakıldı (kalan var)',
  OPERATION_COMPLETED: 'Operasyon tamamlandı',
  QUANTITY_REPORTED: 'Üretim bildirildi',
  REJECTION_REPORTED: 'Hatalı adet bildirildi',
}

/** Olayın adet bilgisi; adet taşımayan olaylarda boş. */
export function eventQuantity(event: BundleEvent): number | null {
  if (event.eventType === 'QUANTITY_REPORTED') {
    return event.producedQuantity
  }
  return event.eventType === 'REJECTION_REPORTED' ? event.rejectedQuantity : null
}
