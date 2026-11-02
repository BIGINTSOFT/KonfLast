export type TargetGroup = 'UNISEX' | 'MEN' | 'WOMEN' | 'KIDS' | 'BABY'

export const targetGroupLabels: Record<TargetGroup, string> = {
  UNISEX: 'Unisex',
  MEN: 'Erkek',
  WOMEN: 'Kadın',
  KIDS: 'Çocuk',
  BABY: 'Bebek',
}

/** Liste fiyatında sık kullanılan para birimleri. */
export const currencyOptions = ['TRY', 'USD', 'EUR', 'GBP'].map((code) => ({ value: code, label: code }))

/** GTİP kodunu okunur biçimde yazar: 610510000011 → 6105.10.00.00.11 */
export function formatGtip(code: string | null | undefined): string {
  if (!code || code.length !== 12) {
    return code ?? '—'
  }
  return `${code.slice(0, 4)}.${code.slice(4, 6)}.${code.slice(6, 8)}.${code.slice(8, 10)}.${code.slice(10, 12)}`
}
