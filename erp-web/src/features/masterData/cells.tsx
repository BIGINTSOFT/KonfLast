import { machineStatusPresentation } from './machineStatus'

// Durum yalnızca renkle değil, ikon ve metinle de belirtilir.
export function MachineStatusBadge({ status }: { status: string }) {
  const { icon, label, tone } = machineStatusPresentation[status] ?? { icon: '?', label: status, tone: 'neutral' }
  return (
    <span className={`badge badge--${tone}`}>
      <span aria-hidden="true">{icon}</span> {label}
    </span>
  )
}

export function ColorSwatch({ hex }: { hex: string | null }) {
  if (!hex) {
    return <>—</>
  }
  return (
    <span className="swatch">
      <span className="swatch__chip" style={{ background: hex }} aria-hidden="true" />
      <code>{hex}</code>
    </span>
  )
}

export function OnlineBadge({ online }: { online: boolean }) {
  return online ? (
    <span className="badge badge--ok"><span aria-hidden="true">●</span> Çevrimiçi</span>
  ) : (
    <span className="badge badge--neutral"><span aria-hidden="true">○</span> Çevrimdışı</span>
  )
}

export function EnrolledBadge({ enrolled }: { enrolled: boolean }) {
  return enrolled ? (
    <span className="badge badge--info"><span aria-hidden="true">✓</span> Eşleştirildi</span>
  ) : (
    <span className="badge badge--warning"><span aria-hidden="true">!</span> Cihaz yok</span>
  )
}
