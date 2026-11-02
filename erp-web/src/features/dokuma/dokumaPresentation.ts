export const dokumaYarnCountSystemLabels: Record<string, string> = {
  NE: 'Ne (İngiliz)',
  NM: 'Nm (metrik)',
  DTEX: 'dtex',
  DENIER: 'Denye',
}

export const loomTypeLabels: Record<string, string> = {
  AIR_JET: 'Hava jetli',
  RAPIER: 'Kancalı (rapier)',
  WATER_JET: 'Su jetli',
  PROJECTILE: 'Projektilli',
  SHUTTLE: 'Mekikli',
}

export const sheddingTypeLabels: Record<string, string> = {
  CAM: 'Eksantrik',
  DOBBY: 'Armür',
  JACQUARD: 'Jakar',
}

export const articleYarnRoleLabels: Record<string, string> = {
  WARP: 'Çözgü',
  WEFT: 'Atkı',
}

export const dokumaOperatorRoleLabels: Record<string, string> = {
  WEAVER: 'Dokumacı',
  SUPERVISOR: 'Vardiya amiri',
  MAINTENANCE: 'Bakım',
}

export const dokumaStopCategoryLabels: Record<string, string> = {
  PAUSE: 'Ara (operatör)',
  STOP: 'Üretim duruşu',
  BREAKDOWN: 'Arıza',
  PLANNED: 'Planlı duruş',
}

export const dokumaMaterialTypeLabels: Record<string, string> = {
  SIZING_CHEMICAL: 'Haşıl kimyasalı',
  SPARE_PART: 'Yedek parça',
  CONSUMABLE: 'Sarf malzemesi',
  OTHER: 'Diğer',
}

export const dokumaMaterialUnitLabels: Record<string, string> = {
  KILOGRAM: 'kg',
  PIECE: 'adet',
  LITER: 'lt',
  METER: 'm',
}

/** Birim kısaltması; bilinmeyen birimde boş. */
export function unitLabel(unit: string | null | undefined) {
  return unit ? dokumaMaterialUnitLabels[unit] ?? '' : ''
}

/** Süreyi saat ve dakika olarak yazar. */
export function formatMinutes(minutes: number) {
  if (minutes < 60) {
    return `${minutes} dk`
  }
  const rest = minutes % 60
  return rest === 0 ? `${Math.floor(minutes / 60)} sa` : `${Math.floor(minutes / 60)} sa ${rest} dk`
}
