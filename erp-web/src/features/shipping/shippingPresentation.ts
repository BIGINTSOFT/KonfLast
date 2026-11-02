import { parseQuantity } from '../stock/stockPresentation'
import type { ShipmentStatus } from './shippingApi'

/** Durum renge bırakılmaz: ikon ve metin de taşır. */
export const shipmentStatusPresentation: Record<ShipmentStatus, { label: string; icon: string; tone: string }> = {
  DRAFT: { label: 'Taslak', icon: '✎', tone: 'neutral' },
  SHIPPED: { label: 'Sevk edildi', icon: '✓', tone: 'ok' },
  CANCELLED: { label: 'İptal', icon: '✕', tone: 'danger' },
}

export interface ShipmentRow {
  /** Satırın ekrandaki kimliği (sunucuya gönderilmez). */
  key: string
  stockItemId: number | null
  quantity: string
}

/**
 * Formdaki kalem satırlarını istek gövdesine çevirir. Tamamen boş satırlar atlanır; yarım doldurulmuş ya da
 * sayısı okunamayan satır varsa null döner. Stok yeterliliğini ve kart türünü sunucu denetler.
 */
export function toShipmentItems(rows: ShipmentRow[]): { stockItemId: number; quantity: number }[] | null {
  const items = []
  for (const row of rows) {
    if (row.stockItemId === null && !row.quantity.trim()) {
      continue
    }

    const quantity = parseQuantity(row.quantity)
    if (row.stockItemId === null || quantity === null) {
      return null
    }

    items.push({ stockItemId: row.stockItemId, quantity })
  }

  return items
}
