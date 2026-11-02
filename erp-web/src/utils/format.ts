const dateTime = new Intl.DateTimeFormat('tr-TR', { dateStyle: 'short', timeStyle: 'medium' })

/** API zamanları UTC'dir; kullanıcının yerel saatine çevrilerek gösterilir. */
export function formatDateTime(iso: string | null): string {
  return iso ? dateTime.format(new Date(iso)) : '—'
}

const dateTimeShort = new Intl.DateTimeFormat('tr-TR', { dateStyle: 'short', timeStyle: 'short' })

/** Saniyesiz tarih ve saat (plan saatleri gibi dakika hassasiyetindeki zamanlar için). */
export function formatDateTimeShort(iso: string | null): string {
  return iso ? dateTimeShort.format(new Date(iso)) : '—'
}

const dateOnly = new Intl.DateTimeFormat('tr-TR', { dateStyle: 'short' })

/** Saat içermeyen tarih (yyyy-aa-gg); saat dilimine göre kaymaması için yerel gün olarak okunur. */
export function formatDate(isoDate: string | null): string {
  return isoDate ? dateOnly.format(new Date(`${isoDate}T00:00:00`)) : '—'
}

/** Bugünün tarihi, date girdilerinin beklediği yyyy-aa-gg biçiminde (yerel saat). */
export function todayIsoDate(): string {
  const now = new Date()
  const pad = (value: number) => String(value).padStart(2, '0')
  return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`
}

const integer = new Intl.NumberFormat('tr-TR')

export function formatQuantity(value: number): string {
  return integer.format(value)
}

const precise = new Intl.NumberFormat('tr-TR', { maximumFractionDigits: 4 })

/** Reçete miktarı: dört ondalığa kadar. */
export function formatRecipeAmount(value: number): string {
  return precise.format(value)
}

/** UTC ISO zamanı, datetime-local girdisinin beklediği yerel yyyy-aa-ggTss:dd biçimine çevirir. */
export function isoToLocalInput(iso: string): string {
  const date = new Date(iso)
  const pad = (value: number) => String(value).padStart(2, '0')
  const day = `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`
  return `${day}T${pad(date.getHours())}:${pad(date.getMinutes())}`
}

/** datetime-local girdisini (yerel saat) API'nin beklediği UTC ISO biçimine çevirir. */
export function localInputToIso(value: string): string | undefined {
  return value ? new Date(value).toISOString() : undefined
}

/** Ad soyadın baş harfleri (avatar için): ilk ve son sözcükten birer harf. */
export function initials(fullName: string | null | undefined): string {
  const words = (fullName ?? '').trim().split(/\s+/).filter(Boolean)
  if (words.length === 0) {
    return '?'
  }

  const letters = words.length === 1 ? [words[0]] : [words[0], words[words.length - 1]]
  return letters.map((word) => word.charAt(0).toLocaleUpperCase('tr-TR')).join('')
}
