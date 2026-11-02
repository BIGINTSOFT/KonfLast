import { parseDecimal } from '../../utils/money'
import type { StockUnit } from '../stock/stockApi'
import type { QuotationLine, QuotationLineRequest, QuotationStatus } from './quotationsApi'

export const quotationStatusPresentation: Record<QuotationStatus, { label: string; icon: string; tone: string }> = {
  DRAFT: { label: 'Taslak', icon: '✎', tone: 'neutral' },
  SENT: { label: 'Gönderildi', icon: '→', tone: 'info' },
  ACCEPTED: { label: 'Kabul edildi', icon: '■', tone: 'ok' },
  REJECTED: { label: 'Reddedildi', icon: '✕', tone: 'danger' },
  EXPIRED: { label: 'Süresi doldu', icon: '○', tone: 'warning' },
}

export interface MaterialDraft {
  key: string
  stockItemId: number | null
  label: string
  unit: StockUnit | null
  quantityPerPiece: string
  /** Kayıttaki (ya da stok kartından gelen) birim maliyet. */
  sourceUnitCost: number
  /** Elle girilen birim maliyet; boşsa kaynaktaki maliyet. */
  unitCost: string
}

/** Teklif satırının düzenlenebilir hali (metin kutuları ve kaynak değerler). */
export interface LineDraft {
  productId: number | null
  productLabel: string | null
  description: string
  modelReference: string
  colors: string
  quantity: string
  /** Boşsa rotadaki dakika. */
  manualMinutes: string
  /** Boşsa rotadaki fason maliyeti. */
  manualSubcontractCost: string
  firePercent: string
  overheadPercent: string
  marginPercent: string
  /** Boşsa fiyat marjdan hesaplanır. */
  unitPrice: string
  /** null: malzemeler kayıtta ürünün reçetesinden yüklenecek. */
  materials: MaterialDraft[] | null
  routeMinutes: number | null
  routeSubcontractCost: number
  warnings: string[]
}

export interface CostBreakdown {
  materialCost: number
  laborMinutes: number
  laborCost: number
  energyCost: number
  subcontractCost: number
  directCost: number
  fireCost: number
  overheadCost: number
  unitCost: number
  marginPercent: number
  unitPrice: number
}

const round4 = (value: number) => Math.round(value * 10_000) / 10_000
const round2 = (value: number) => Math.round(value * 100) / 100

const text = (value: number | null) => (value === null ? '' : String(value).replace('.', ','))

/** Metin kutusundaki ondalık; boşsa null, geçersizse 0. */
function optional(textValue: string): number | null {
  return textValue.trim() === '' ? null : parseDecimal(textValue) ?? 0
}

/** Eksi değer de kabul eden ondalık (marj). */
function signed(textValue: string): number | null {
  const trimmed = textValue.trim()
  if (trimmed === '') {
    return null
  }
  const negative = trimmed.startsWith('-')
  const value = parseDecimal(negative ? trimmed.slice(1) : trimmed)
  return value === null ? 0 : negative ? -value : value
}

export function materialCostOf(material: MaterialDraft): number {
  const unitCost = optional(material.unitCost) ?? material.sourceUnitCost
  return round4((parseDecimal(material.quantityPerPiece) ?? 0) * unitCost)
}

/**
 * Parça başı maliyet ve fiyat; sunucudaki hesapla aynı formül ve yuvarlama (QuotationCost):
 * doğrudan = malzeme + dakika × (işçilik + enerji) + fason; fire = doğrudan × fire %; genel gider = (doğrudan + fire) ×
 * genel gider %; birim maliyet = doğrudan + fire + genel gider; fiyat = maliyet × (1 + marj %) ya da elle (marj geri hesaplanır).
 */
export function computeLine(draft: LineDraft, laborRate: number, energyRate: number, materialCost: number): CostBreakdown {
  const laborMinutes = round4(optional(draft.manualMinutes) ?? draft.routeMinutes ?? 0)
  const laborCost = round4(laborMinutes * laborRate)
  const energyCost = round4(laborMinutes * energyRate)
  const subcontractCost = round4(optional(draft.manualSubcontractCost) ?? draft.routeSubcontractCost)
  const directCost = materialCost + laborCost + energyCost + subcontractCost
  const fireCost = round4(directCost * (optional(draft.firePercent) ?? 0) / 100)
  const overheadCost = round4((directCost + fireCost) * (optional(draft.overheadPercent) ?? 0) / 100)
  const unitCost = round4(directCost + fireCost + overheadCost)
  const price = optional(draft.unitPrice)
  const margin = signed(draft.marginPercent) ?? 0
  return price !== null
    ? {
        materialCost, laborMinutes, laborCost, energyCost, subcontractCost, directCost, fireCost, overheadCost, unitCost,
        marginPercent: unitCost > 0 ? round2((price / unitCost - 1) * 100) : 0,
        unitPrice: price,
      }
    : {
        materialCost, laborMinutes, laborCost, energyCost, subcontractCost, directCost, fireCost, overheadCost, unitCost,
        marginPercent: margin,
        unitPrice: round2(unitCost * (1 + margin / 100)),
      }
}

export function draftOf(line: QuotationLine): LineDraft {
  return {
    productId: line.productId,
    productLabel: line.productId === null ? null : `${line.productCode ?? ''} — ${line.productName ?? ''}`,
    description: line.description ?? '',
    modelReference: line.modelReference ?? '',
    colors: line.colors ?? '',
    quantity: String(line.quantity),
    manualMinutes: text(line.manualMinutes),
    manualSubcontractCost: text(line.manualSubcontractCost),
    firePercent: text(line.firePercent),
    overheadPercent: text(line.overheadPercent),
    marginPercent: text(line.marginPercent),
    unitPrice: line.priceOverridden ? text(line.unitPrice) : '',
    materials: line.materials.map((material, index) => ({
      key: `m-${material.id || index}-${index}`,
      stockItemId: material.stockItemId,
      label: material.description,
      unit: material.unit,
      quantityPerPiece: text(material.quantityPerPiece),
      sourceUnitCost: material.unitCost,
      unitCost: material.unitCostOverridden ? text(material.unitCost) : '',
    })),
    routeMinutes: line.routeMinutes,
    routeSubcontractCost: line.routeSubcontractCost,
    warnings: line.warnings,
  }
}

export function requestOf(draft: LineDraft): QuotationLineRequest {
  return {
    productId: draft.productId,
    description: draft.description.trim() || null,
    modelReference: draft.modelReference.trim() || null,
    colors: draft.colors.trim() || null,
    quantity: Number(draft.quantity) || 0,
    manualMinutes: optional(draft.manualMinutes),
    manualSubcontractCost: optional(draft.manualSubcontractCost),
    firePercent: optional(draft.firePercent),
    overheadPercent: optional(draft.overheadPercent),
    marginPercent: optional(draft.unitPrice) === null ? signed(draft.marginPercent) : null,
    unitPrice: optional(draft.unitPrice),
    materials: draft.materials === null
      ? null
      : draft.materials.map((material) => ({
          stockItemId: material.stockItemId,
          description: material.stockItemId === null ? material.label.trim() || null : null,
          quantityPerPiece: parseDecimal(material.quantityPerPiece) ?? 0,
          unitCost: optional(material.unitCost),
        })),
  }
}

/** Satırın listede görünen adı: ürün kodu / adı ya da açıklama. */
export function lineTitle(line: { productCode: string | null; productName: string | null; description: string | null }): string {
  return line.productCode ? `${line.productCode} ${line.productName ?? ''}`.trim() : line.description ?? '—'
}

const percent = new Intl.NumberFormat('tr-TR', { maximumFractionDigits: 2 })

export function formatPercent(value: number): string {
  return `%${percent.format(value)}`
}

const minutes = new Intl.NumberFormat('tr-TR', { maximumFractionDigits: 2 })

export function formatMinutes(value: number): string {
  return `${minutes.format(value)} dk`
}
