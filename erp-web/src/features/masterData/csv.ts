import type { FieldDef } from './resources'

/** Başlık ve hücre karşılaştırması: büyük/küçük harf, Türkçe karakter ve boşluk farkı gözetilmez. */
export function normalizeHeader(text: string): string {
  return text
    .toLocaleLowerCase('tr-TR')
    .replace(/ı/g, 'i').replace(/ş/g, 's').replace(/ğ/g, 'g').replace(/ü/g, 'u').replace(/ö/g, 'o').replace(/ç/g, 'c')
    .replace(/\(.*?\)/g, '')
    .replace(/[^a-z0-9]/g, '')
}

/** Ayırıcıyı ilk satırdan anlar: Türkçe Excel noktalı virgül, diğerleri virgül ya da sekme kullanır. */
export function detectDelimiter(firstLine: string): string {
  const counts = [';', ',', '\t'].map((delimiter) => ({ delimiter, count: firstLine.split(delimiter).length }))
  return counts.sort((a, b) => b.count - a.count)[0].delimiter
}

/**
 * CSV metnini satır ve hücrelere ayırır. Çift tırnaklı hücreler (içinde ayırıcı, satır sonu ya da "" ile
 * kaçırılmış tırnak olabilir) desteklenir; tümü boş olan satırlar atılır.
 */
export function parseCsv(text: string, delimiter?: string): string[][] {
  const content = text.replace(/^﻿/, '')
  const separator = delimiter ?? detectDelimiter(content.split(/\r?\n/, 1)[0] ?? '')
  const rows: string[][] = []
  let row: string[] = []
  let cell = ''
  let quoted = false

  for (let index = 0; index < content.length; index += 1) {
    const character = content[index]
    if (quoted) {
      if (character === '"' && content[index + 1] === '"') {
        cell += '"'
        index += 1
      } else if (character === '"') {
        quoted = false
      } else {
        cell += character
      }
    } else if (character === '"' && cell === '') {
      quoted = true
    } else if (character === separator) {
      row.push(cell)
      cell = ''
    } else if (character === '\n' || character === '\r') {
      if (character === '\r' && content[index + 1] === '\n') {
        index += 1
      }
      row.push(cell)
      rows.push(row)
      row = []
      cell = ''
    } else {
      cell += character
    }
  }

  if (cell !== '' || row.length > 0) {
    row.push(cell)
    rows.push(row)
  }

  return rows.map((cells) => cells.map((value) => value.trim())).filter((cells) => cells.some((value) => value !== ''))
}

/** Dosya baytlarını metne çevirir: UTF-8 değilse (Excel'in Türkçe ANSI kaydı) Windows-1254 olarak okunur. */
export function decodeCsv(bytes: ArrayBuffer): string {
  try {
    return new TextDecoder('utf-8', { fatal: true }).decode(bytes)
  } catch {
    return new TextDecoder('windows-1254').decode(bytes)
  }
}

export const ACTIVE_COLUMN = 'Aktif'

/** Şablonun başlık satırı: alan etiketleri ve Aktif sütunu. */
export function templateHeaders(fields: FieldDef[]): string[] {
  return [...fields.map((field) => field.label), ACTIVE_COLUMN]
}

/** Şablon dosyası: Excel'in doğrudan açabildiği noktalı virgüllü, BOM'lu UTF-8 metin. */
export function templateCsv(fields: FieldDef[]): string {
  return `﻿${templateHeaders(fields).map(quoteCell).join(';')}\r\n`
}

function quoteCell(value: string): string {
  return /[;"\r\n]/.test(value) ? `"${value.replace(/"/g, '""')}"` : value
}

export interface ColumnMapping {
  /** Dosyadaki sütun sırası → alan (eşleşmeyen sütun null). */
  fields: (FieldDef | 'isActive' | null)[]
  unknownColumns: string[]
  missingRequired: string[]
}

/** Başlıkları alanlarla eşleştirir; hem alan etiketi ("Müşteri kodu") hem alan adı ("customerCode") kabul edilir. */
export function mapColumns(headers: string[], fields: FieldDef[]): ColumnMapping {
  const byKey = new Map<string, FieldDef>()
  for (const field of fields) {
    byKey.set(normalizeHeader(field.label), field)
    byKey.set(normalizeHeader(field.name), field)
  }

  const mapped = headers.map((header) => {
    const key = normalizeHeader(header)
    return key === normalizeHeader(ACTIVE_COLUMN) || key === 'isactive' ? 'isActive' as const : byKey.get(key) ?? null
  })

  const used = new Set(mapped.filter((field): field is FieldDef => field !== null && field !== 'isActive').map((field) => field.name))
  return {
    fields: mapped,
    unknownColumns: headers.filter((_, index) => mapped[index] === null),
    missingRequired: fields.filter((field) => field.required && !used.has(field.name)).map((field) => field.label),
  }
}

const TRUE_VALUES = new Set(['evet', 'e', 'true', '1', 'x', 'var', 'aktif', 'yes'])
const FALSE_VALUES = new Set(['hayir', 'h', 'false', '0', 'yok', 'pasif', 'no'])

/** "Evet / Hayır / 1 / 0 / Aktif / Pasif" gibi değerleri boolean'a çevirir; boş hücre hayırdır, tanınmayan değer null. */
export function parseBoolean(text: string): boolean | null {
  if (text.trim() === '') {
    return false
  }

  const key = normalizeHeader(text)
  return TRUE_VALUES.has(key) ? true : FALSE_VALUES.has(key) ? false : null
}

export type CellValue = string | number | boolean | null

export interface CellResult {
  value?: CellValue
  /** Başka bir kaydın koduyla verilen referans; içe aktarma sırasında Id'ye çevrilir. */
  lookupCode?: string
  error?: string
}

/** Bir hücreyi alanın tipine göre form değerine çevirir (referanslar hariç; onlar kodla döner). */
export function convertCell(field: FieldDef, text: string): CellResult {
  const raw = text.trim()

  switch (field.type) {
    case 'number': {
      if (raw === '') {
        return { value: '' }
      }
      const normalized = raw.replace(/\s/g, '').replace(',', '.')
      return /^-?\d+(\.\d+)?$/.test(normalized) ? { value: normalized } : { error: `${field.label}: "${raw}" sayı değil.` }
    }
    case 'checkbox': {
      const parsed = parseBoolean(raw)
      return parsed === null ? { error: `${field.label}: "${raw}" Evet / Hayır olmalı.` } : { value: parsed }
    }
    case 'select': {
      if (raw === '') {
        return { value: field.options?.[0]?.value ?? '' }
      }
      const key = normalizeHeader(raw)
      const option = field.options?.find((item) => normalizeHeader(item.value) === key || normalizeHeader(item.label) === key)
      return option
        ? { value: option.value }
        : { error: `${field.label}: "${raw}" geçerli değil (${field.options?.map((item) => item.label).join(', ')}).` }
    }
    case 'time': {
      const match = /^(\d{1,2})[:.](\d{2})(?::\d{2})?$/.exec(raw)
      if (raw === '') {
        return { value: '' }
      }
      return match ? { value: `${match[1].padStart(2, '0')}:${match[2]}` } : { error: `${field.label}: "${raw}" saat değil (örn. 08:00).` }
    }
    case 'lookup':
      return raw === '' ? { value: null } : { lookupCode: raw }
    default:
      return { value: raw }
  }
}
