import type { OrderBundleSummary } from './bundlesApi'

export type FlowState = 'done' | 'current' | 'pending'

export interface FlowStep {
  key: 'bundle' | 'card' | 'produce' | 'receive'
  title: string
  detail: string
  state: FlowState
}

/**
 * Siparişin üretim akışındaki dört adım ve durumları. Sunucunun verdiği sayılardan türetilir: bir adım,
 * yapılacak işi kalmadıysa "tamam", sırası geldiyse "sırada", önceki adım başlamadıysa "bekliyor"dur.
 */
export function orderFlowSteps(summary: OrderBundleSummary): FlowStep[] {
  const progress = summary.progress ?? { total: 0, withoutCard: 0, inProduction: 0, completed: 0, closed: 0 }
  const { total, withoutCard, inProduction, completed, closed } = progress
  const carded = total - withoutCard
  const finished = completed + closed

  const stateOf = (started: boolean, done: boolean): FlowState => (done ? 'done' : started ? 'current' : 'pending')
  const fullyBundled = summary.bundled > 0 && summary.remaining === 0

  return [
    {
      key: 'bundle',
      title: 'Demetle',
      detail: `${summary.bundled} / ${summary.ordered} adet demetlendi`,
      state: stateOf(summary.canCreateBundles || summary.bundled > 0, fullyBundled),
    },
    {
      key: 'card',
      title: 'Kart ata',
      detail: total === 0 ? 'Demet yok' : `${carded} / ${total} demete kart atandı`,
      state: stateOf(total > 0, total > 0 && withoutCard === 0),
    },
    {
      key: 'produce',
      title: 'Üret',
      detail: total === 0 ? 'Demet yok' : `${finished} / ${total} demet tamamlandı · ${inProduction} sahada`,
      state: stateOf(carded > 0, total > 0 && finished === total),
    },
    {
      key: 'receive',
      title: 'Depoya al',
      detail: total === 0 ? 'Demet yok' : `${closed} / ${total} demet depoda · ${completed} bekliyor`,
      state: stateOf(finished > 0, total > 0 && closed === total),
    },
  ]
}
