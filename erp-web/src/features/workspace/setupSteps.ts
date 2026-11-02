import type { SetupStep } from './workspaceApi'

/** Adımları sunucudan geldiği sırayla gruplar. */
export function groupSetupSteps(steps: SetupStep[]): { group: string; steps: SetupStep[] }[] {
  const groups: { group: string; steps: SetupStep[] }[] = []
  for (const step of steps) {
    const last = groups[groups.length - 1]
    if (last?.group === step.group) {
      last.steps.push(step)
    } else {
      groups.push({ group: step.group, steps: [step] })
    }
  }
  return groups
}

export type SetupStateFilter = 'all' | 'missing' | 'done'

const normalize = (text: string) => text.toLocaleLowerCase('tr-TR')

/**
 * Adımları arama metnine ve durumuna göre süzer. Metin adımın başlığında, açıklamasında ya da grubunda
 * (Türkçe büyük / küçük harf ayrımı olmadan) aranır; birden fazla kelime verilirse hepsi geçmelidir.
 */
export function filterSetupSteps(steps: SetupStep[], search: string, state: SetupStateFilter): SetupStep[] {
  const words = normalize(search).split(/\s+/).filter(Boolean)
  return steps.filter((step) => {
    if (state === 'done' && !step.done) {
      return false
    }
    if (state === 'missing' && step.done) {
      return false
    }
    const text = normalize(`${step.title} ${step.description} ${step.group}`)
    return words.every((word) => text.includes(word))
  })
}
