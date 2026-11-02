export const ormeYarnCountSystemLabels: Record<string, string> = {
  NE: 'Ne (İngiliz)',
  NM: 'Nm (metrik)',
  DTEX: 'dtex',
  DENIER: 'Denye',
}

export const ormeOperatorRoleLabels: Record<string, string> = {
  KNITTER: 'Örme operatörü',
  SUPERVISOR: 'Vardiya amiri',
  MAINTENANCE: 'Bakım',
}

export const ormeStopCategoryLabels: Record<string, string> = {
  PAUSE: 'Ara (operatör)',
  STOP: 'Üretim duruşu',
  BREAKDOWN: 'Arıza',
  PLANNED: 'Planlı duruş',
}

/** Süreyi saat ve dakika olarak yazar. */
export function formatMinutes(minutes: number) {
  if (minutes < 60) {
    return `${minutes} dk`
  }
  const rest = minutes % 60
  return rest === 0 ? `${Math.floor(minutes / 60)} sa` : `${Math.floor(minutes / 60)} sa ${rest} dk`
}

export const knitBedTypeLabels: Record<string, string> = {
  SINGLE: 'Tek plaka',
  DOUBLE: 'Çift plaka',
}
