import { Check, Circle, CircleDot, Send, Undo2, X } from 'lucide-react'
import type { ReactNode } from 'react'
import { formatDateTime } from '../../utils/format'
import type { ApprovalEventType, ApprovalStepStatus, SalesOrder } from './salesOrdersApi'

const stepPresentation: Record<ApprovalStepStatus, { label: string; icon: ReactNode; tone: string }> = {
  WAITING: { label: 'Sırada', icon: <Circle aria-hidden="true" />, tone: 'muted' },
  PENDING: { label: 'Kararı bekleniyor', icon: <CircleDot aria-hidden="true" />, tone: 'pending' },
  APPROVED: { label: 'Onayladı', icon: <Check aria-hidden="true" />, tone: 'ok' },
  REJECTED: { label: 'Geri çevirdi', icon: <X aria-hidden="true" />, tone: 'danger' },
  CANCELLED: { label: 'Gerek kalmadı', icon: <Circle aria-hidden="true" />, tone: 'muted' },
}

const eventLabels: Record<ApprovalEventType, string> = {
  SUBMITTED: 'onaya gönderdi',
  APPROVED: 'onayladı',
  REJECTED: 'geri çevirdi',
  REVISED: 'revizyona aldı',
  CANCELLED: 'iptal etti',
}

const eventIcons: Record<ApprovalEventType, ReactNode> = {
  SUBMITTED: <Send aria-hidden="true" />,
  APPROVED: <Check aria-hidden="true" />,
  REJECTED: <X aria-hidden="true" />,
  REVISED: <Undo2 aria-hidden="true" />,
  CANCELLED: <X aria-hidden="true" />,
}

/**
 * Siparişin onay bilgisi (dar panel): son gönderimin adımları ve kimin hangi aşamada ne yaptığı, notlarıyla.
 * Akışsız gönderilmiş eski siparişlerde yalnızca gönderen / karar veren gösterilir.
 */
export function ApprovalPanel({ order }: { order: SalesOrder }) {
  const steps = order.approvalSteps ?? []
  const events = [...(order.approvalEvents ?? [])].reverse()

  return (
    <aside className="card approval-panel" aria-label="Onay bilgisi">
      <h2>Onay</h2>

      {steps.length > 0 && (
        <ol className="approval-steps">
          {steps.map((step) => {
            const presentation = stepPresentation[step.status]
            return (
              <li key={step.sequence} className={`approval-steps__item approval-steps__item--${presentation.tone}`}>
                <span className="approval-steps__icon" title={presentation.label}>{presentation.icon}</span>
                <div>
                  <div className="approval-steps__name">{step.name}</div>
                  <div className="approval-steps__who">
                    {step.decidedBy
                      ? `${step.decidedBy} · ${formatDateTime(step.decidedAt)}`
                      : `${step.approvers}${step.status === 'PENDING' ? ' · bekleniyor' : ''}`}
                  </div>
                </div>
              </li>
            )
          })}
        </ol>
      )}

      {steps.length === 0 && order.submittedAt && events.length === 0 && (
        <dl className="approval-legacy">
          <div><dt>Gönderen</dt><dd>{order.submittedBy} · {formatDateTime(order.submittedAt)}</dd></div>
          {order.decidedAt && <div><dt>Karar</dt><dd>{order.decidedBy} · {formatDateTime(order.decidedAt)}</dd></div>}
          {order.decisionNote && <div><dt>Not</dt><dd>{order.decisionNote}</dd></div>}
        </dl>
      )}

      {events.length > 0 && (
        <>
          <h3 className="approval-panel__subtitle">Geçmiş ve notlar</h3>
          <ul className="approval-events">
            {events.map((event) => (
              <li key={`${event.createdAt}-${event.eventType}-${event.userName}`} className="approval-events__item">
                <span className={`approval-events__icon approval-events__icon--${event.eventType.toLowerCase()}`}>{eventIcons[event.eventType]}</span>
                <div>
                  <div className="approval-events__head">
                    <strong>{event.fullName ?? event.userName}</strong> {eventLabels[event.eventType]}
                    {event.stepName && <span className="muted"> · {event.stepName}</span>}
                  </div>
                  <div className="approval-events__time">{formatDateTime(event.createdAt)}{event.round > 1 ? ` · ${event.round}. gönderim` : ''}</div>
                  {event.note && <p className="approval-events__note">{event.note}</p>}
                </div>
              </li>
            ))}
          </ul>
        </>
      )}

      {!order.submittedAt && events.length === 0 && <p className="muted approval-panel__empty">Henüz onaya gönderilmedi.</p>}
    </aside>
  )
}
