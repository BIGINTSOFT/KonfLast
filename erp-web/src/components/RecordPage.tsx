import { useState } from 'react'
import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'

export interface RecordFact {
  label: string
  value: ReactNode
  /** `danger`: dikkat isteyen değer (ör. geçmiş termin). */
  tone?: 'danger'
}

interface RecordHeaderProps {
  back: { to: string; label: string }
  /** Kayıt numarası / adı. */
  title: ReactNode
  /** Başlığın yanındaki durum etiketleri. */
  badges?: ReactNode
  /** Kaydın her sekmede görünen ana bilgileri. */
  facts: RecordFact[]
  /** Kaydın o anki durumunda yapılabilecek işlemler (sağ üst). */
  actions?: ReactNode
}

/**
 * Kayıt sayfasının üst bilgisi: numara ve durum, o an yapılabilecek işlemler ve ana bilgiler. Sayfa kaydırılınca
 * üstte kalır; hangi sekmede olunursa olunsun kaydın ne olduğu görünür.
 */
export function RecordHeader({ back, title, badges, facts, actions }: RecordHeaderProps) {
  return (
    <header className="record-header">
      <div className="record-header__top">
        <div className="record-header__title">
          <Link to={back.to} className="record-header__back">← {back.label}</Link>
          <h1>{title}</h1>
          {badges}
        </div>
        {actions && <div className="page__actions">{actions}</div>}
      </div>
      <dl className="record-header__facts">
        {facts.map((fact) => (
          <div key={fact.label} className={fact.tone === 'danger' ? 'record-header__fact record-header__fact--danger' : 'record-header__fact'}>
            <dt>{fact.label}</dt>
            <dd>{fact.value}</dd>
          </div>
        ))}
      </dl>
    </header>
  )
}

export interface RecordTab {
  key: string
  label: string
  /** Yetki yoksa ya da kayıt için anlamsızsa sekme gösterilmez. */
  visible?: boolean
  content: () => ReactNode
}

/**
 * Kayıt sayfasının sekmeleri: her sekmede tek iş. Yalnızca seçili sekmenin içeriği çizilir (veri de o an okunur);
 * seçim, çalışma sekmesi açık kaldıkça korunur.
 */
export function RecordTabs({ tabs, label }: { tabs: RecordTab[]; label: string }) {
  const visible = tabs.filter((tab) => tab.visible !== false)
  const [active, setActive] = useState(visible[0]?.key)
  const current = visible.find((tab) => tab.key === active) ?? visible[0]

  if (!current) {
    return null
  }

  return (
    <>
      <div className="tabs record-tabs" role="tablist" aria-label={label}>
        {visible.map((tab) => (
          <button
            key={tab.key}
            type="button"
            role="tab"
            id={`record-tab-${tab.key}`}
            aria-selected={tab.key === current.key}
            aria-controls={`record-panel-${tab.key}`}
            className={`tabs__tab${tab.key === current.key ? ' tabs__tab--active' : ''}`}
            onClick={() => setActive(tab.key)}
          >
            {tab.label}
          </button>
        ))}
      </div>
      <div className="record-panel" role="tabpanel" id={`record-panel-${current.key}`} aria-labelledby={`record-tab-${current.key}`}>
        {current.content()}
      </div>
    </>
  )
}
