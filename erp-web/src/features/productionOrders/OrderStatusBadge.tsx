import { statusPresentation } from './orderPresentation'
import type { ProductionOrderStatus } from './productionOrdersApi'

export function OrderStatusBadge({ status }: { status: ProductionOrderStatus }) {
  const { label, icon, tone } = statusPresentation[status]
  return (
    <span className={`badge badge--${tone}`}>
      <span aria-hidden="true">{icon}</span> {label}
    </span>
  )
}
