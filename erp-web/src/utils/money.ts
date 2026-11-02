const amount = new Intl.NumberFormat('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
const rate = new Intl.NumberFormat('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 4 })

/** Tutar: "1.250,50 TRY". Değer yoksa "—". */
export function formatMoney(value: number | null | undefined, currency: string): string {
  return value === null || value === undefined ? '—' : `${amount.format(value)} ${currency}`
}

/** Birim fiyat / ücret: en az 2, en fazla 4 ondalık ("0,125 TRY"). */
export function formatRate(value: number | null | undefined, currency: string): string {
  return value === null || value === undefined ? '—' : `${rate.format(value)} ${currency}`
}

/**
 * Formdaki sayı metnini sayıya çevirir (virgül de ondalık ayırıcı sayılır). Boş ya da geçersiz girdi null
 * döner; aralık ve basamak kurallarını sunucu denetler.
 */
export function parseDecimal(text: string): number | null {
  const normalized = text.trim().replace(',', '.')
  return /^\d+(\.\d+)?$/.test(normalized) ? Number(normalized) : null
}
