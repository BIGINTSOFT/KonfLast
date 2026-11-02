import type { RouteStatus } from './productionRoutesApi'
import { routeStatusPresentation } from './routePresentation'

export function RouteStatusBadge({ status }: { status: RouteStatus }) {
  const { label, icon, tone } = routeStatusPresentation[status]
  return (
    <span className={`badge badge--${tone}`}>
      <span aria-hidden="true">{icon}</span> {label}
    </span>
  )
}
