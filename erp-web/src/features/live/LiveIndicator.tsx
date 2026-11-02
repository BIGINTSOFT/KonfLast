import { useAppSelector } from '../../store/hooks'
import { liveStatusPresentation } from './liveUpdates'

/** Canlı bağlantının durumu; yalnızca renkle değil, ikon ve metinle de belirtilir. */
export function LiveIndicator() {
  const status = useAppSelector((state) => state.live.status)
  const { label, icon, tone } = liveStatusPresentation[status]

  return (
    <span className={`badge badge--${tone}`} role="status" title="Üretim verisinin canlı güncelleme bağlantısı">
      <span aria-hidden="true">{icon}</span> {label}
    </span>
  )
}
