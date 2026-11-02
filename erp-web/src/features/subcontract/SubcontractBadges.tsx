import type { SubcontractSendStatus } from './subcontractApi'
import { subcontractSendStatusPresentation } from './subcontractPresentation'

/** Durum yalnızca renkle değil, ikon ve metinle de belirtilir. */
export function SubcontractSendStatusBadge({ status }: { status: SubcontractSendStatus }) {
  const { label, icon, tone } = subcontractSendStatusPresentation[status]
  return (
    <span className={`badge badge--${tone}`}>
      <span aria-hidden="true">{icon}</span> {label}
    </span>
  )
}

/** Hakedişin ödeme durumu. */
export function PaidBadge({ paid }: { paid: boolean }) {
  return paid
    ? <span className="badge badge--ok"><span aria-hidden="true">■</span> Ödendi</span>
    : <span className="badge badge--warning"><span aria-hidden="true">○</span> Ödenmedi</span>
}
