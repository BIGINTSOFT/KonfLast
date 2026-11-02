import type { ProductionOrderItem, ProductionOrderStatus } from './productionOrdersApi'

export const statusPresentation: Record<ProductionOrderStatus, { label: string; icon: string; tone: string }> = {
  DRAFT: { label: 'Taslak', icon: '✎', tone: 'neutral' },
  RELEASED: { label: 'Onaylı', icon: '✓', tone: 'info' },
  IN_PRODUCTION: { label: 'Üretimde', icon: '▶', tone: 'ok' },
  COMPLETED: { label: 'Tamamlandı', icon: '■', tone: 'ok' },
  CANCELLED: { label: 'İptal', icon: '✕', tone: 'danger' },
}

/** Bir duruma geçiş için ekranda gösterilen işlem. */
export const transitionActions: Partial<Record<ProductionOrderStatus, { label: string; confirm?: string }>> = {
  RELEASED: { label: 'Onayla' },
  DRAFT: { label: 'Taslağa al' },
  COMPLETED: { label: 'Tamamla', confirm: 'Sipariş kapatılacak; üretime girmemiş demetler iptal edilip malzemeleri depoya dönecek. Bu işlem geri alınamaz.' },
  CANCELLED: { label: 'İptal et', confirm: 'Sipariş iptal edilecek. Bu işlem geri alınamaz.' },
}

export interface MatrixAxis {
  id: number
  code: string
  name: string
}

export interface QuantityMatrix {
  colors: MatrixAxis[]
  sizes: MatrixAxis[]
  quantity: (colorId: number, sizeId: number) => number
  colorTotal: (colorId: number) => number
  sizeTotal: (sizeId: number) => number
  total: number
}

/**
 * Kalem listesini renk (satır) × beden (sütun) tablosuna çevirir. Kalemler sunucudan renk koduna
 * ve beden sırasına göre sıralı gelir; satır ve sütunlar ilk görülme sırasını korur.
 */
export function buildQuantityMatrix(items: ProductionOrderItem[]): QuantityMatrix {
  const colors = new Map<number, MatrixAxis>()
  const sizes = new Map<number, MatrixAxis & { sortOrder: number }>()
  const cells = new Map<string, number>()

  for (const item of items) {
    colors.set(item.colorId, { id: item.colorId, code: item.colorCode, name: item.colorName })
    sizes.set(item.sizeId, { id: item.sizeId, code: item.sizeCode, name: item.sizeName, sortOrder: item.sizeSortOrder })
    cells.set(`${item.colorId}:${item.sizeId}`, item.quantity)
  }

  const sum = (matches: (item: ProductionOrderItem) => boolean) =>
    items.filter(matches).reduce((total, item) => total + item.quantity, 0)

  return {
    colors: [...colors.values()],
    sizes: [...sizes.values()].sort((a, b) => a.sortOrder - b.sortOrder || a.code.localeCompare(b.code)),
    quantity: (colorId, sizeId) => cells.get(`${colorId}:${sizeId}`) ?? 0,
    colorTotal: (colorId) => sum((item) => item.colorId === colorId),
    sizeTotal: (sizeId) => sum((item) => item.sizeId === sizeId),
    total: sum(() => true),
  }
}
