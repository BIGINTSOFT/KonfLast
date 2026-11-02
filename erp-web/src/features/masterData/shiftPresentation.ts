export type BreakPolicy = 'AFTER_BUNDLE' | 'AFTER_QUANTITY' | 'AFTER_OPERATION' | 'IMMEDIATE'

/** İlk seçenek yeni mola planında varsayılandır. */
export const breakPolicyLabels: Record<BreakPolicy, string> = {
  AFTER_BUNDLE: 'Önce demeti bitirir',
  AFTER_QUANTITY: 'Yaptığı adedi bildirip çıkar',
  AFTER_OPERATION: 'Önce operasyonu bitirir',
  IMMEDIATE: 'İşi bırakıp hemen çıkar',
}

/** Sunucunun "08:00:00" biçimini "08:00" olarak gösterir. */
export function formatTime(value: string | null | undefined): string {
  return value ? value.slice(0, 5) : '—'
}

/** Dakikayı "10 sa", "45 dk", "1 sa 30 dk" biçiminde yazar. */
export function formatMinutes(minutes: number): string {
  const hours = Math.floor(minutes / 60)
  const rest = minutes % 60
  if (hours === 0) {
    return `${rest} dk`
  }
  return rest === 0 ? `${hours} sa` : `${hours} sa ${rest} dk`
}

export type StopReasonCategory = 'PAUSE' | 'BREAKDOWN'

/** Duruş nedeninin tablette hangi seçenek listesinde çıkacağı. */
export const stopReasonCategoryLabels: Record<StopReasonCategory, string> = {
  PAUSE: 'Ara nedeni',
  BREAKDOWN: 'Arıza nedeni',
}
