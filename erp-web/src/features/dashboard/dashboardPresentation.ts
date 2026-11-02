import { machineStatusPresentation } from '../masterData/machineStatus'
import type { MachineActivity } from '../productionTracking/trackingApi'
import { operatorStatePresentation } from '../productionTracking/trackingPresentation'

interface Presentation {
  label: string
  icon: string
  tone: string
}

/** Makine çalışamıyorsa (arıza, bakım, kapalı) operatörün durumu değil, makinenin durumu gösterilir. */
const machineFirst = new Set(['BREAKDOWN', 'MAINTENANCE', 'OFFLINE'])

/**
 * Pano satırında makinenin tek bakışta durumu: çalışamayan makinede makine durumu, aksi halde başındaki
 * operatörün durumu (üretiyor, demet bekliyor, molada…). Durumları sunucu belirler; burada yalnızca
 * hangisinin öne çıkacağı seçilir.
 */
export function machineGlance(machine: Pick<MachineActivity, 'status' | 'operatorState'>): Presentation {
  if (machineFirst.has(machine.status) && machine.operatorState !== 'MACHINE_BREAKDOWN') {
    return machineStatusPresentation[machine.status] ?? { label: machine.status, icon: '?', tone: 'neutral' }
  }

  return operatorStatePresentation[machine.operatorState]
}

/** "151 / saat"; değer yoksa tire. */
export function formatRate(value: number | null): string {
  return value === null ? '—' : `${value.toLocaleString('tr-TR', { maximumFractionDigits: 0 })} / saat`
}
