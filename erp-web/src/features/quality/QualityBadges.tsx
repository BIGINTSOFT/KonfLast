import type { QualityResult, ReworkStatus } from './qualityApi'
import { qualityResultPresentation, reworkStatusPresentation } from './qualityPresentation'

// Durum yalnızca renkle değil, ikon ve metinle de belirtilir.
export function QualityResultBadge({ result }: { result: QualityResult }) {
  const { label, icon, tone } = qualityResultPresentation[result]
  return (
    <span className={`badge badge--${tone}`}>
      <span aria-hidden="true">{icon}</span> {label}
    </span>
  )
}

export function ReworkStatusBadge({ status }: { status: ReworkStatus }) {
  const { label, icon, tone } = reworkStatusPresentation[status]
  return (
    <span className={`badge badge--${tone}`}>
      <span aria-hidden="true">{icon}</span> {label}
    </span>
  )
}
