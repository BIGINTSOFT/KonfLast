import { bundleStatusPresentation, operationStatusPresentation } from './bundlePresentation'
import type { BundleOperationStatus, BundleStatus } from './bundlesApi'

// Durum yalnızca renkle değil, ikon ve metinle de belirtilir.
export function BundleStatusBadge({ status }: { status: BundleStatus }) {
  const { label, icon, tone } = bundleStatusPresentation[status]
  return (
    <span className={`badge badge--${tone}`}>
      <span aria-hidden="true">{icon}</span> {label}
    </span>
  )
}

export function OperationStatusBadge({ status }: { status: BundleOperationStatus }) {
  const { label, icon, tone } = operationStatusPresentation[status]
  return (
    <span className={`badge badge--${tone}`}>
      <span aria-hidden="true">{icon}</span> {label}
    </span>
  )
}
