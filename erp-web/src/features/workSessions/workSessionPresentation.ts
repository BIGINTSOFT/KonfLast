import type { LookupItem } from '../masterData/masterDataApi'
import type {
  WorkSessionEndReason,
  WorkSessionPause,
  WorkSessionPauseType,
  WorkSessionStatus,
} from './workSessionsApi'

export const sessionStatusPresentation: Record<WorkSessionStatus, { label: string; icon: string; tone: string }> = {
  ACTIVE: { label: 'Açık', icon: '▶', tone: 'ok' },
  LOGGED_OUT: { label: 'Kapandı', icon: '■', tone: 'neutral' },
  BREAK: { label: 'Molada', icon: '☕', tone: 'info' },
  PAUSED: { label: 'Duraklatıldı', icon: '‖', tone: 'warning' },
  MACHINE_BREAKDOWN: { label: 'Makine arızası', icon: '!', tone: 'danger' },
  WAITING_FOR_BUNDLE: { label: 'Demet bekliyor', icon: '◷', tone: 'warning' },
}

export const endReasonLabels: Record<WorkSessionEndReason, string> = {
  LOGOUT: 'Operatör çıkış yaptı',
  TAKEN_OVER: 'Makineyi başka operatör devraldı',
  MOVED_TO_ANOTHER_MACHINE: 'Operatör başka makineye geçti',
  CLOSED_BY_SUPERVISOR: 'Yönetici kapattı',
  SHIFT_ENDED: 'Vardiya bitti (sistem kapattı)',
}

export const pauseTypeLabels: Record<WorkSessionPauseType, string> = {
  BREAK: 'Planlı mola',
  PAUSE: 'Ara',
  MACHINE_BREAKDOWN: 'Makine arızası',
  WAITING_FOR_BUNDLE: 'Demet bekleme',
}


/** Duraklamanın türü ve ayrıntısı: "Planlı mola · Çay molası", "Ara · Tuvalet". */
export function pauseLabel(pause: Pick<WorkSessionPause, 'type' | 'reasonName' | 'breakName'>): string {
  const detail = pause.type === 'BREAK' ? pause.breakName : pause.reasonName
  return detail ? `${pauseTypeLabels[pause.type]} · ${detail}` : pauseTypeLabels[pause.type]
}

/** Dakikayı "3 sa 25 dk" biçiminde yazar. */
export function formatDuration(minutes: number): string {
  const total = Math.max(0, Math.round(minutes))
  return total < 60 ? `${total} dk` : `${Math.floor(total / 60)} sa ${total % 60} dk`
}

/**
 * Seçim listesi: seçilebilir (aktif) kayıtlar ile hâlihazırda seçili olanların birleşimi. Seçili bir kayıt
 * sonradan pasife alınmış ya da liste sınırının dışında kalmış olsa da görünür ve kaydederken kaybolmaz.
 */
export function mergeOptions(available: LookupItem[], selected: LookupItem[]): LookupItem[] {
  const known = new Set(available.map((item) => item.id))
  return [...available, ...selected.filter((item) => !known.has(item.id))].sort((a, b) =>
    a.code.localeCompare(b.code, 'tr'),
  )
}

/** İki kart numarası aynı kartı mı gösteriyor? Baştaki sıfırlar ve boşluklar önemsenmez ("12629368" = "0012629368"). */
export function sameCardNumber(a: string, b: string): boolean {
  // Sunucudaki kural: harfler büyük harfe çevrilir, yalnız rakamdan oluşan numarada baştaki sıfırlar önemsizdir.
  const normalize = (value: string) => {
    const code = value.trim().toUpperCase()
    return /^\d+$/.test(code) ? code.replace(/^0+(?=\d)/, '') : code
  }
  return normalize(a) === normalize(b)
}

/** Ad soyadın baş harfleri (en fazla iki): "Utku Can Uzun" → "UU". */
export function initialsOf(fullName: string): string {
  const words = fullName.trim().split(/\s+/).filter(Boolean)
  if (words.length === 0) {
    return ''
  }

  const letters = words.length === 1 ? [words[0][0]] : [words[0][0], words[words.length - 1][0]]
  return letters.join('').toLocaleUpperCase('tr-TR')
}

export type StaffRole = 'OPERATOR' | 'LINE_CHIEF' | 'MAINTENANCE'

/** Personelin sahadaki görevi; tabletteki yetkilerini belirler. */
export const staffRoleLabels: Record<StaffRole, string> = {
  OPERATOR: 'Operatör',
  LINE_CHIEF: 'Bant Şefi',
  MAINTENANCE: 'Bakım Operatörü',
}
