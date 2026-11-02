import type { IntegrationDeliveryStatus, IntegrationEventType } from './integrationsApi'

interface Presentation {
  label: string
  icon: string
  tone: string
}

/** Durum renge bırakılmaz: ikon ve metin de taşır. */
export const deliveryStatusPresentation: Record<IntegrationDeliveryStatus, Presentation> = {
  PENDING: { label: 'Kuyrukta', icon: '…', tone: 'warning' },
  SUCCEEDED: { label: 'Ulaştı', icon: '✓', tone: 'ok' },
  FAILED: { label: 'Gönderilemedi', icon: '!', tone: 'danger' },
}

const NO_CHANNEL: Presentation = { label: 'Kanal yok', icon: '–', tone: 'neutral' }

/**
 * Bir olayın tüm kanallardaki teslimlerinin özeti. En çok dikkat isteyen durum öne çıkar: gönderilemeyen
 * varsa o, yoksa kuyruktaki, yoksa ulaşan. Hiç teslimi olmayan olay yalnızca kayıtlıdır (API'den okunabilir).
 */
export function messageState(counts: { pendingCount: number; succeededCount: number; failedCount: number }): Presentation {
  if (counts.failedCount > 0) {
    return deliveryStatusPresentation.FAILED
  }
  if (counts.pendingCount > 0) {
    return deliveryStatusPresentation.PENDING
  }
  return counts.succeededCount > 0 ? deliveryStatusPresentation.SUCCEEDED : NO_CHANNEL
}

/** Olay türünün okunur adı; sunucunun listesinde yoksa (eski bir tür) kodun kendisi. */
export function eventLabel(eventTypes: readonly IntegrationEventType[] | undefined, type: string): string {
  return eventTypes?.find((eventType) => eventType.type === type)?.label ?? type
}

const ENTITY_ROUTES: Record<string, string> = {
  Shipment: '/shipments',
  ProductionOrder: '/production-orders',
}

/** Olayın ait olduğu kaydın sayfası; kendi sayfası olmayan kayıtlarda (ör. mal kabul) null. */
export function entityLink(entityName: string, entityId: string): string | null {
  const route = ENTITY_ROUTES[entityName]
  return route ? `${route}/${encodeURIComponent(entityId)}` : null
}

/** Kanalın dinlediği olaylar; boş liste "tümü" demektir. */
export function channelEvents(eventTypes: readonly IntegrationEventType[] | undefined, channelTypes: readonly string[]): string {
  return channelTypes.length === 0 ? 'Tüm olaylar' : channelTypes.map((type) => eventLabel(eventTypes, type)).join(', ')
}
