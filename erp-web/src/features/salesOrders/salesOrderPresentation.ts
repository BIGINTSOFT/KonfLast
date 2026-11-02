import type { SalesOrderLine, SalesOrderStatus } from './salesOrdersApi'

export const salesStatusPresentation: Record<SalesOrderStatus, { label: string; icon: string; tone: string }> = {
  DRAFT: { label: 'Taslak', icon: '✎', tone: 'neutral' },
  SUBMITTED: { label: 'Onay bekliyor', icon: '…', tone: 'warning' },
  APPROVED: { label: 'Onaylı', icon: '✓', tone: 'info' },
  CLOSED: { label: 'Kapatıldı', icon: '■', tone: 'ok' },
  CANCELLED: { label: 'İptal', icon: '✕', tone: 'danger' },
}

/** Siparişin bir ürünü: kalemleri (renk-beden) ve tutarı. */
export interface ProductSection {
  productId: number
  productCode: string
  productName: string
  /** Ürünün bütün kalemlerinde aynıysa o fiyat; fiyat yoksa ya da kalemler arasında farklıysa null. */
  unitPrice: number | null
  quantity: number
  shippedQuantity: number
  amount: number | null
  lines: SalesOrderLine[]
}

/** Kalemleri ürün bazında toplar; ürünler sunucunun sırasıyla (ürün koduna göre) gelir. */
export function groupByProduct(lines: readonly SalesOrderLine[]): ProductSection[] {
  const sections = new Map<number, ProductSection>()
  for (const line of lines) {
    const section = sections.get(line.productId)
    if (!section) {
      sections.set(line.productId, {
        productId: line.productId,
        productCode: line.productCode,
        productName: line.productName,
        unitPrice: line.unitPrice,
        quantity: line.quantity,
        shippedQuantity: line.shippedQuantity,
        amount: line.lineAmount,
        lines: [line],
      })
      continue
    }

    section.lines.push(line)
    section.quantity += line.quantity
    section.shippedQuantity += line.shippedQuantity
    section.unitPrice = section.unitPrice === line.unitPrice ? section.unitPrice : null
    section.amount = section.amount === null && line.lineAmount === null ? null : (section.amount ?? 0) + (line.lineAmount ?? 0)
  }

  return [...sections.values()]
}

/** Formdaki fiyat metni: boşsa null (fiyatsız), geçersizse undefined. Virgül de ondalık ayırıcıdır. */
export function parsePrice(text: string): number | null | undefined {
  const normalized = text.trim().replace(',', '.')
  if (normalized === '') {
    return null
  }

  return /^\d+(\.\d{1,4})?$/.test(normalized) ? Number(normalized) : undefined
}

/** Sipariş formunda bir satır: bir ürünün bir rengi; bedenler sütundur, satırın tek bir birim fiyatı vardır. */
export interface OrderRow {
  /** Satırın ekrandaki kimliği (sunucuya gönderilmez). */
  key: string
  productId: number | null
  colorId: number | null
  /** Müşterinin ürün (artikel) kodu; ürün kartından gelir, salt okunur. */
  articleCode: string | null
  unitPrice: string
  /** Kullanıcı fiyatı kendisi yazdı / sildi: önerilen fiyat artık üzerine yazılmaz. */
  priceTouched: boolean
  /** Beden kimliğine göre girilen adetler. */
  quantities: Record<number, string>
}

const priceText = (price: number | null) => (price === null ? '' : String(price).replace('.', ','))

/**
 * Kayıtlı kalemleri satırlara toplar: aynı ürün, renk ve birim fiyat bir satırdır. Bedenleri farklı fiyatlı
 * ürün-renk, fiyat başına ayrı satır olur (kayıtta fiyatlar korunur).
 */
export function rowsFromLines(lines: readonly SalesOrderLine[], newKey: () => string): OrderRow[] {
  const rows = new Map<string, OrderRow>()
  for (const line of lines) {
    const id = `${line.productId}:${line.colorId}:${line.unitPrice ?? ''}`
    const row = rows.get(id) ?? {
      key: newKey(),
      productId: line.productId,
      colorId: line.colorId,
      articleCode: line.customerArticleCode,
      unitPrice: priceText(line.unitPrice),
      priceTouched: true,
      quantities: {},
    }
    row.quantities[line.sizeId] = String(line.quantity)
    rows.set(id, row)
  }
  return [...rows.values()]
}

const hasQuantity = (row: OrderRow, sizeId: number) => Number(row.quantities[sizeId] || 0) !== 0

/**
 * Aynı ürün ve renk birden fazla satırda olabilir (ör. büyük bedenler farklı fiyatla); ancak bir beden yalnızca
 * bir satırda adet taşıyabilir. Satır anahtarına göre çakışan beden kimlikleri döner.
 */
export function sizeConflicts(rows: readonly OrderRow[]): Map<string, number[]> {
  const conflicts = new Map<string, number[]>()
  rows.forEach((row, index) => {
    if (row.productId === null || row.colorId === null) {
      return
    }
    for (const other of rows.slice(index + 1)) {
      if (other.productId !== row.productId || other.colorId !== row.colorId) {
        continue
      }
      const shared = Object.keys(row.quantities).map(Number).filter((sizeId) => hasQuantity(row, sizeId) && hasQuantity(other, sizeId))
      for (const target of [row, other]) {
        if (shared.length > 0) {
          conflicts.set(target.key, [...new Set([...(conflicts.get(target.key) ?? []), ...shared])])
        }
      }
    }
  })
  return conflicts
}

/**
 * Satırı, bedenlerin önerilen fiyatına göre böler: her farklı fiyat bir satır olur ve o fiyattaki bedenlerin
 * adetlerini taşır. Fiyatı bilinmeyen bedenler ilk satırda kalır.
 */
export function splitByPrice(row: OrderRow, priceBySize: ReadonlyMap<number, number>, newKey: () => string): OrderRow[] {
  const groups = new Map<number, number[]>()
  for (const [sizeId, price] of priceBySize) {
    groups.set(price, [...(groups.get(price) ?? []), sizeId])
  }
  if (groups.size < 2) {
    return [row]
  }

  const sizeIds = Object.keys(row.quantities).map(Number)
  const priced = new Set(priceBySize.keys())
  return [...groups.entries()].map(([price, ids], index) => {
    const own = index === 0 ? [...ids, ...sizeIds.filter((id) => !priced.has(id))] : ids
    return {
      ...row,
      key: index === 0 ? row.key : newKey(),
      unitPrice: priceText(price),
      priceTouched: false,
      quantities: Object.fromEntries(own.filter((id) => row.quantities[id] !== undefined).map((id) => [id, row.quantities[id]])),
    }
  })
}

/** Satırın bedenleri için önerilen fiyatlar (sunucudan gelen fiyat teklifine göre). */
export interface RowSuggestion {
  /** Bütün bedenlerde aynıysa önerilen fiyat; yoksa null. */
  price: number | null
  /** Beden başına önerilen fiyat (fiyatı bulunan bedenler). */
  priceBySize: Map<number, number>
  /** Bedenlere göre farklı fiyat var. */
  varies: boolean
  /** Fiyatın nereden geldiği (gösterim için). */
  hint: string | null
  /** Fiyat bulunamadıysa nedeni. */
  problem: string | null
}

export interface QuoteLike {
  productId: number
  colorId: number
  sizeId: number
  unitPrice: number | null
  discountPercent: number | null
  sourceName: string | null
  problem: string | null
}

export const quoteKey = (productId: number, colorId: number, sizeId: number) => `${productId}:${colorId}:${sizeId}`

/**
 * Satırın fiyat önerisi. Adet girilmiş bedenler varsa yalnızca onlara, yoksa tablodaki bütün bedenlere bakılır.
 */
export function suggestionFor(
  row: OrderRow, sizeIds: readonly number[], quotes: ReadonlyMap<string, QuoteLike>,
): RowSuggestion | null {
  if (row.productId === null || row.colorId === null || sizeIds.length === 0) {
    return null
  }
  const entered = sizeIds.filter((sizeId) => hasQuantity(row, sizeId))
  const considered = entered.length > 0 ? entered : sizeIds
  const found = considered
    .map((sizeId) => quotes.get(quoteKey(row.productId!, row.colorId!, sizeId)))
    .filter((quote): quote is QuoteLike => quote !== undefined)
  if (found.length === 0) {
    return null
  }

  const priceBySize = new Map(found.filter((q) => q.unitPrice !== null).map((q) => [q.sizeId, q.unitPrice!]))
  const distinct = new Set(priceBySize.values())
  const first = found.find((q) => q.unitPrice !== null) ?? found[0]
  const discount = first.discountPercent ? ` · %${String(first.discountPercent).replace('.', ',')} iskonto` : ''
  return {
    price: distinct.size === 1 ? [...distinct][0] : null,
    priceBySize,
    varies: distinct.size > 1,
    hint: first.sourceName ? `${first.sourceName}${discount}` : null,
    problem: found.find((q) => q.problem)?.problem ?? null,
  }
}

/** Proformanın bir satırı: aynı ürün, renk ve fiyattaki bedenler tek satırda (beden dağılımıyla). */
export interface ProformaRow {
  productCode: string
  productName: string
  articleCode: string | null
  colorName: string
  /** "S 10 · M 20" biçiminde, beden sırasıyla. */
  sizes: string
  quantity: number
  unitPrice: number | null
  amount: number | null
  vatRate: number
}

export function proformaRows(lines: readonly SalesOrderLine[]): ProformaRow[] {
  const groups = new Map<string, SalesOrderLine[]>()
  for (const line of lines) {
    const key = `${line.productId}:${line.colorId}:${line.unitPrice ?? ''}`
    groups.set(key, [...(groups.get(key) ?? []), line])
  }
  return [...groups.values()].map((group) => {
    const [first] = group
    const sorted = [...group].sort((a, b) => a.sizeSortOrder - b.sizeSortOrder)
    const quantity = group.reduce((sum, line) => sum + line.quantity, 0)
    return {
      productCode: first.productCode,
      productName: first.productName,
      articleCode: first.customerArticleCode,
      colorName: first.colorName,
      sizes: sorted.map((line) => `${line.sizeName} ${line.quantity}`).join(' · '),
      quantity,
      unitPrice: first.unitPrice,
      amount: first.unitPrice === null ? null : Math.round(quantity * first.unitPrice * 100) / 100,
      vatRate: first.vatRate,
    }
  })
}

/** KDV oranına göre matrah ve KDV tutarı (fiyatlı kalemlerden). */
export function vatBreakdown(rows: readonly ProformaRow[]): { rate: number; base: number; vat: number }[] {
  const byRate = new Map<number, number>()
  for (const row of rows) {
    if (row.amount !== null) {
      byRate.set(row.vatRate, (byRate.get(row.vatRate) ?? 0) + row.amount)
    }
  }
  return [...byRate.entries()]
    .sort(([a], [b]) => a - b)
    .map(([rate, base]) => ({ rate, base, vat: Math.round(base * rate) / 100 }))
}

/** Sevk durumu: hiç sevk edilmedi, kısmen sevk edildi, tamamı sevk edildi. */
export function shipmentState(totalQuantity: number, shippedQuantity: number): { label: string; tone: string } | null {
  if (shippedQuantity <= 0) {
    return null
  }
  return shippedQuantity >= totalQuantity
    ? { label: 'Tamamı sevk edildi', tone: 'ok' }
    : { label: `Kısmi sevk · %${Math.floor((shippedQuantity / totalQuantity) * 100)}`, tone: 'warning' }
}
