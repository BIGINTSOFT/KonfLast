import type { LucideIcon } from 'lucide-react'
import { Link } from 'react-router-dom'

/** Pano göstergesi. `to` verilirse (kullanıcı o ekranı görebiliyorsa) kart ilgili ekrana götürür. */
export function StatCard({ label, value, detail, icon: Icon, tone, to }: { label: string; value: string; detail?: string; icon: LucideIcon; tone: string; to?: string }) {
  const body = (
    <>
      <span className="stat__icon"><Icon aria-hidden="true" /></span>
      <div className="stat__body">
        <h2 className="stat__label">{label}</h2>
        <p className="stat__value">{value}</p>
        {detail && <p className="muted stat__detail">{detail}</p>}
      </div>
    </>
  )
  return to
    ? <Link to={to} className={`card stat stat--link stat--${tone}`}>{body}</Link>
    : <article className={`card stat stat--${tone}`}>{body}</article>
}
