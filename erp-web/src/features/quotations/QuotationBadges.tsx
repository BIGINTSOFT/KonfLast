import type { QuotationStatus } from './quotationsApi'
import { quotationStatusPresentation } from './quotationPresentation'

/** Durum yalnızca renkle değil, ikon ve metinle de belirtilir. */
export function QuotationStatusBadge({ status }: { status: QuotationStatus }) {
  const { label, icon, tone } = quotationStatusPresentation[status]
  return (
    <span className={`badge badge--${tone}`}>
      <span aria-hidden="true">{icon}</span> {label}
    </span>
  )
}
