import type { ProductionRouteOperation, RouteStatus } from './productionRoutesApi'

export const routeStatusPresentation: Record<RouteStatus, { label: string; icon: string; tone: string }> = {
  DRAFT: { label: 'Taslak', icon: '✎', tone: 'neutral' },
  ACTIVE: { label: 'Etkin', icon: '✓', tone: 'ok' },
  ARCHIVED: { label: 'Arşiv', icon: '▣', tone: 'neutral' },
}

/** Adımları sunucunun hesapladığı aşamaya göre gruplar (akış görünümü için). */
export function groupByStage(operations: ProductionRouteOperation[]): ProductionRouteOperation[][] {
  const stages: ProductionRouteOperation[][] = []
  for (const operation of [...operations].sort((a, b) => a.sequenceNo - b.sequenceNo)) {
    ;(stages[operation.stage] ??= []).push(operation)
  }
  return stages.filter(Boolean)
}

/**
 * "20, 30" biçiminde yazılan sıra numaralarını sayı listesine çevirir. Boş metin boş listedir;
 * sayı olmayan bir parça varsa null döner (kullanıcıya yazım hatası gösterilir).
 */
export function parseSequenceList(text: string): number[] | null {
  const parts = text.split(/[\s,;]+/).filter((part) => part !== '')
  const numbers = parts.map(Number)
  return numbers.every((value) => Number.isInteger(value) && value > 0) ? [...new Set(numbers)] : null
}

const samFormat = new Intl.NumberFormat('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 4 })

export function formatSam(sam: number): string {
  return samFormat.format(sam)
}
