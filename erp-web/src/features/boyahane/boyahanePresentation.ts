export const fabricTypeLabels: Record<string, string> = {
  KNIT: 'Örme',
  WOVEN: 'Dokuma',
}

/** Kumaş kartında seçilebilen birimler; sunucu da yalnızca bunları kabul eder. */
export const fabricUnitLabels: Record<string, string> = {
  METER: 'Metre',
  KILOGRAM: 'Kilogram',
}

export const yarnCountSystemLabels: Record<string, string> = {
  NE: 'Ne',
  NM: 'Nm',
  DTEX: 'dtex',
}

export const defectCategoryLabels: Record<string, string> = {
  APPEARANCE: 'Görünüm',
  STRUCTURE: 'Yapı',
  MEASURE: 'Ölçü',
}

export const dyeChemicalKindLabels: Record<string, string> = {
  DYE: 'Boya',
  CHEMICAL: 'Kimyasal',
}

export const dyeMachineTypeLabels: Record<string, string> = {
  JET: 'Jet',
  OVERFLOW: 'Overflow',
  JIGGER: 'Jigger',
  STENTER: 'Ram',
  YARN_VESSEL: 'İplik kazanı',
}

export const dyeMachineStatusLabels: Record<string, string> = {
  AVAILABLE: 'Müsait',
  BREAKDOWN: 'Arıza',
  MAINTENANCE: 'Bakımda',
}

export const boyahaneOperatorRoleLabels: Record<string, string> = {
  OPERATOR: 'Operatör',
  SUPERVISOR: 'Vardiya amiri',
  MAINTENANCE: 'Bakım',
}

export const boyahaneBreakPolicyLabels: Record<string, string> = {
  AFTER_STEP: 'Önce adımı bitirir',
  IMMEDIATE: 'İşi bırakıp hemen çıkar',
}

export const boyahaneCardTypeLabels: Record<string, string> = {
  OPERATOR: 'Personel',
  BATCH: 'Parti',
}

export const boyahaneStopReasonCategoryLabels: Record<string, string> = {
  PAUSE: 'Ara',
  BREAKDOWN: 'Arıza',
}

export const billingUnitLabels: Record<string, string> = {
  ORDER_UNIT: 'Sipariş birimi',
  METER: 'Metre',
  KILOGRAM: 'Kilo',
}
