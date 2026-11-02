import type { StockItemType, StockTransactionType, StockUnit } from './stockApi'

export const stockItemTypeLabels: Record<StockItemType, string> = {
  FABRIC: 'Kumaş',
  ACCESSORY: 'Aksesuar',
  FINISHED_GOOD: 'Mamul',
  OTHER: 'Diğer',
}

/** Elle tanımlanabilen kart türleri; mamul kartlarını sistem açar. */
export const manualStockItemTypes: StockItemType[] = ['FABRIC', 'ACCESSORY', 'OTHER']

export const stockUnitLabels: Record<StockUnit, string> = {
  PIECE: 'adet',
  METER: 'm',
  KILOGRAM: 'kg',
  ROLL: 'top',
  BOX: 'koli',
}

export const warehouseTypeLabels: Record<string, string> = {
  GENERAL: 'Genel',
  RAW_MATERIAL: 'Hammadde',
  FINISHED_GOODS: 'Mamul',
}

/** Hareketin yönü renge bırakılmaz: ikon ve metin de taşır. */
export const transactionTypePresentation: Record<StockTransactionType, { label: string; icon: string; tone: string }> = {
  RECEIPT: { label: 'Giriş', icon: '↓', tone: 'ok' },
  ISSUE: { label: 'Çıkış', icon: '↑', tone: 'warning' },
  TRANSFER_OUT: { label: 'Transfer çıkışı', icon: '→', tone: 'info' },
  TRANSFER_IN: { label: 'Transfer girişi', icon: '←', tone: 'info' },
  ADJUSTMENT: { label: 'Sayım düzeltmesi', icon: '≠', tone: 'neutral' },
  PURCHASE_RECEIPT: { label: 'Mal kabul', icon: '↓', tone: 'ok' },
  SHIPMENT: { label: 'Sevkiyat', icon: '↑', tone: 'info' },
  PRODUCTION_RECEIPT: { label: 'Üretimden giriş', icon: '↓', tone: 'ok' },
  PRODUCTION_ISSUE: { label: 'Üretime çıkış (reçete / kesim)', icon: '↑', tone: 'warning' },
  PRODUCTION_RETURN: { label: 'Üretimden iade (demet iptali)', icon: '↓', tone: 'neutral' },
  RELOCATION_OUT: { label: 'Adres değişimi (çıkış)', icon: '→', tone: 'neutral' },
  RELOCATION_IN: { label: 'Adres değişimi (giriş)', icon: '←', tone: 'neutral' },
}

/** Seçim listesindeki adres: "A-01-03 — Üst raf (pasif)". */
export function locationLabel(location: { code: string; description: string | null; isActive: boolean }): string {
  const description = location.description ? ` — ${location.description}` : ''
  return `${location.code}${description}${location.isActive ? '' : ' (pasif)'}`
}

const quantityFormat = new Intl.NumberFormat('tr-TR', { maximumFractionDigits: 3 })

/** "1.250,5 m" */
export function formatStockQuantity(quantity: number, unit: StockUnit): string {
  return `${quantityFormat.format(quantity)} ${stockUnitLabels[unit]}`
}

/** Hareket miktarı işaretiyle: "+20 m", "−2,25 m". */
export function formatSignedQuantity(quantity: number, unit: StockUnit): string {
  const sign = quantity > 0 ? '+' : quantity < 0 ? '−' : ''
  return `${sign}${formatStockQuantity(Math.abs(quantity), unit)}`
}

/**
 * Formdaki miktar metnini sayıya çevirir (virgül de ondalık ayırıcı sayılır). Geçersiz ya da boş girdi
 * null döner; geçerlilik kurallarını (pozitiflik, birim, stok yeterliliği) sunucu denetler.
 */
export function parseQuantity(text: string): number | null {
  const normalized = text.trim().replace(',', '.')
  if (!/^\d+(\.\d+)?$/.test(normalized)) {
    return null
  }

  return Number(normalized)
}

/** Adetle izlenen kartta miktar girdisi tam sayı adımıyla, diğerlerinde 3 ondalıkla alınır. */
export function quantityStep(unit: StockUnit | undefined): string {
  return unit === 'PIECE' ? '1' : '0.001'
}
