import { parseQuantity } from '../stock/stockPresentation'
import type { PurchaseOrderAction, PurchaseOrderStatus } from './purchasingApi'

/** Durum renge bırakılmaz: ikon ve metin de taşır. */
export const purchaseStatusPresentation: Record<PurchaseOrderStatus, { label: string; icon: string; tone: string }> = {
  DRAFT: { label: 'Taslak', icon: '✎', tone: 'neutral' },
  APPROVED: { label: 'Onaylı', icon: '✓', tone: 'info' },
  PARTIALLY_RECEIVED: { label: 'Kısmen teslim alındı', icon: '◐', tone: 'warning' },
  RECEIVED: { label: 'Teslim alındı', icon: '●', tone: 'ok' },
  CLOSED: { label: 'Eksik kapatıldı', icon: '■', tone: 'neutral' },
  CANCELLED: { label: 'İptal', icon: '✕', tone: 'danger' },
}

/** Sunucunun izin verdiği durum geçişi → kullanıcıya sunulan işlem. */
export const purchaseActions: Partial<Record<PurchaseOrderStatus, { action: PurchaseOrderAction; label: string; confirm?: string }>> = {
  APPROVED: { action: 'approve', label: 'Onayla' },
  DRAFT: { action: 'revert-to-draft', label: 'Taslağa al' },
  CLOSED: {
    action: 'close',
    label: 'Eksik kapat',
    confirm: 'Kalan miktar beklenmeden sipariş kapatılacak. Bu işlem geri alınamaz. Devam edilsin mi?',
  },
  CANCELLED: { action: 'cancel', label: 'İptal et', confirm: 'Sipariş iptal edilecek. Bu işlem geri alınamaz. Devam edilsin mi?' },
}

const amount = new Intl.NumberFormat('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })

/** "20.100,00 EUR"; tutar yoksa (fiyat girilmemiş) tire. */
export function formatMoney(value: number | null, currency: string): string {
  return value === null ? '—' : `${amount.format(value)} ${currency}`
}

export interface ItemRow {
  /** Satırın ekrandaki kimliği (sunucuya gönderilmez). */
  key: string
  stockItemId: number | null
  quantity: string
  unitPrice: string
  /** Kalemin açıldığı iş emri (eksik malzeme için); kayıtta korunur. */
  productionOrderId?: number | null
  productionOrderNo?: string | null
}

/**
 * Formdaki kalem satırlarını istek gövdesine çevirir. Tamamen boş satırlar atlanır; yarım doldurulmuş ya da
 * sayısı okunamayan satır varsa null döner. Miktar ve fiyatın geçerliliğini (birim, üst sınır) sunucu denetler.
 */
export function toItemsPayload(
  rows: ItemRow[],
): { stockItemId: number; quantity: number; unitPrice: number | null; productionOrderId: number | null }[] | null {
  const items = []
  for (const row of rows) {
    const empty = row.stockItemId === null && !row.quantity.trim() && !row.unitPrice.trim()
    if (empty) {
      continue
    }

    const quantity = parseQuantity(row.quantity)
    const unitPrice = row.unitPrice.trim() ? parseQuantity(row.unitPrice) : null
    if (row.stockItemId === null || quantity === null || (row.unitPrice.trim() && unitPrice === null)) {
      return null
    }

    items.push({ stockItemId: row.stockItemId, quantity, unitPrice, productionOrderId: row.productionOrderId ?? null })
  }

  return items
}

const unitPrice = new Intl.NumberFormat('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 4 })

/** Birim fiyat dört ondalığa kadar girilebilir: "0,0125 EUR". */
export function formatUnitPrice(value: number | null, currency: string): string {
  return value === null ? '—' : `${unitPrice.format(value)} ${currency}`
}
