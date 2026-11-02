import type { MasterRow } from './masterDataApi'
import type { FieldDef } from './resources'

export type FormValues = Record<string, string | number | boolean | null>

/** Alanın boş (yeni kayıt) form değeri. */
export function emptyValue(field: FieldDef): string | number | boolean | null {
  if (field.defaultValue !== undefined) {
    return field.defaultValue
  }
  switch (field.type) {
    case 'lookup':
      return null
    case 'checkbox':
      return false
    case 'select':
      return field.options?.[0]?.value ?? ''
    case 'currency':
      return 'TRY'
    default:
      return ''
  }
}

export function initialValues(fields: FieldDef[], row?: MasterRow): FormValues {
  const values: FormValues = {}
  for (const field of fields) {
    if (!row) {
      values[field.name] = emptyValue(field)
      continue
    }
    const current = row?.[field.name]
    if (field.type === 'lookup') {
      values[field.name] = typeof current === 'number' ? current : null
    } else if (field.type === 'checkbox') {
      values[field.name] = current === true
    } else if (field.type === 'time') {
      // Sunucu "08:00:00" döner; saat girdisi "08:00" bekler.
      values[field.name] = typeof current === 'string' ? current.slice(0, 5) : ''
    } else if (field.type === 'select' || field.type === 'currency') {
      values[field.name] = typeof current === 'string' ? current : emptyValue(field)
    } else {
      values[field.name] = current === null || current === undefined ? '' : String(current)
    }
  }
  return values
}

/** Form değerlerini API'nin beklediği tiplere çevirir. */
export function toRequestBody(fields: FieldDef[], values: FormValues, isActive: boolean): Record<string, unknown> {
  const body: Record<string, unknown> = { isActive }
  for (const field of fields) {
    const value = values[field.name]
    if (field.type === 'number') {
      body[field.name] = field.nullable && value === '' ? null : Number(value || 0)
    } else if (field.type === 'checkbox') {
      body[field.name] = value === true
    } else if (field.type === 'time') {
      body[field.name] = typeof value === 'string' && value.length === 5 ? `${value}:00` : value || null
    } else if (field.type === 'lookup') {
      // Zorunlu seçim boşsa 0 gönderilir; sunucu "seçilmelidir" hatasını ilgili alana bağlar.
      body[field.name] = value ?? (field.required ? 0 : null)
    } else {
      body[field.name] = value === '' ? null : value
    }
  }
  return body
}
