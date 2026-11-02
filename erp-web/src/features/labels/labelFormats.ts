export type LabelFormatKey = 'roll-100x50' | 'roll-80x50' | 'roll-60x40' | 'a4-3x8'

export interface LabelFormat {
  title: string
  /** `roll`: etiket yazıcısı, her etiket ayrı sayfa. `sheet`: A4 etiket kağıdı, ızgara. */
  layout: 'roll' | 'sheet'
  width: string
  height: string
  columns: number
  /** Yazdırma sayfasının boyutu (CSS @page size). */
  page: string
  pageMargin: string
}

export const labelFormats: Record<LabelFormatKey, LabelFormat> = {
  'roll-100x50': { title: 'Etiket yazıcısı 100 × 50 mm', layout: 'roll', width: '100mm', height: '50mm', columns: 1, page: '100mm 50mm', pageMargin: '0' },
  'roll-80x50': { title: 'Etiket yazıcısı 80 × 50 mm', layout: 'roll', width: '80mm', height: '50mm', columns: 1, page: '80mm 50mm', pageMargin: '0' },
  'roll-60x40': { title: 'Etiket yazıcısı 60 × 40 mm', layout: 'roll', width: '60mm', height: '40mm', columns: 1, page: '60mm 40mm', pageMargin: '0' },
  'a4-3x8': { title: 'A4 etiket kağıdı 3 × 8 (70 × 37 mm)', layout: 'sheet', width: '70mm', height: '37mm', columns: 3, page: 'A4', pageMargin: '0' },
}

export interface LabelOptions {
  format: LabelFormatKey
  showBarcode: boolean
  showQr: boolean
  showCard: boolean
}

const STORAGE_KEY = 'erp.labels.options'

export const defaultLabelOptions: LabelOptions = { format: 'roll-100x50', showBarcode: true, showQr: true, showCard: false }

/** Saklanan metinden seçenekleri okur; eksik ya da geçersiz alanlar varsayılana döner. */
export function parseLabelOptions(stored: string | null): LabelOptions {
  try {
    const value = JSON.parse(stored ?? 'null') as Partial<LabelOptions> | null
    if (typeof value !== 'object' || value === null) {
      return defaultLabelOptions
    }

    return {
      format: typeof value.format === 'string' && value.format in labelFormats ? value.format : defaultLabelOptions.format,
      showBarcode: typeof value.showBarcode === 'boolean' ? value.showBarcode : defaultLabelOptions.showBarcode,
      showQr: typeof value.showQr === 'boolean' ? value.showQr : defaultLabelOptions.showQr,
      showCard: typeof value.showCard === 'boolean' ? value.showCard : defaultLabelOptions.showCard,
    }
  } catch {
    return defaultLabelOptions
  }
}

/** Son kullanılan etiket seçenekleri bu tarayıcıda hatırlanır. */
export function readLabelOptions(): LabelOptions {
  try {
    return parseLabelOptions(localStorage.getItem(STORAGE_KEY))
  } catch {
    return defaultLabelOptions
  }
}

export function storeLabelOptions(options: LabelOptions) {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(options))
  } catch {
    // Depolama kapalıysa tercih yalnızca bu oturumda geçerli olur.
  }
}
