import { cuttingPlanStatusPresentation } from './cuttingPresentation'
import type { CuttingPlanStatus } from './cuttingApi'

/** Durum yalnızca renkle değil, ikon ve metinle de belirtilir. */
export function CuttingPlanStatusBadge({ status }: { status: CuttingPlanStatus }) {
  const { label, icon, tone } = cuttingPlanStatusPresentation[status]
  return (
    <span className={`badge badge--${tone}`}>
      <span aria-hidden="true">{icon}</span> {label}
    </span>
  )
}

/** Fark: artı fazla kesim (uyarı), eksi eksik kalan; sıfırsa tire. */
export function DifferenceCell({ value }: { value: number }) {
  if (value === 0) {
    return <span className="muted">—</span>
  }

  return value > 0
    ? <span className="badge badge--warning">+{value} fazla</span>
    : <span className="muted">{-value} eksik</span>
}
