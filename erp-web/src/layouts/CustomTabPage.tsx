import { ExternalLink, StarOff } from 'lucide-react'
import { useState } from 'react'
import type { CSSProperties } from 'react'
import { Link } from 'react-router-dom'
import { ContextMenu } from '../components/ContextMenu'
import { moduleOf } from './navigation'
import type { NavGroup, NavItem } from './navigation'
import { moduleStyle, navIcon } from './navPresentation'
import { resolveShortcuts, tabLabel } from './tabs'
import type { WorkTab } from './tabs'
import { useWorkTabs } from './workTabs'

/**
 * Özel sekmenin sayfası: kullanıcının bu sekmeye eklediği kısayollar. Kısayola tıklanınca ekran bir çalışma
 * sekmesinde açılır, bu sayfa yerinde kalır (sekme koparılıp pencere yapılmışsa da). Kısayollar menüde ekrana
 * sağ tıklanarak eklenir.
 */
export function CustomTabPage({
  tab, groups, onRemoveShortcut,
}: {
  tab: WorkTab
  groups: NavGroup[]
  onRemoveShortcut: (item: NavItem) => void
}) {
  const workTabs = useWorkTabs()
  const [menu, setMenu] = useState<{ x: number; y: number; item: NavItem } | null>(null)
  // Artık görülemeyen (yetkisi kalkmış / kaldırılmış) ekranlar listelenmez.
  const items = resolveShortcuts(groups, tab.shortcuts ?? [])
  const moduleOfPath = (path: string) => moduleOf(groups.find((group) => group.items.some((item) => item.path === path)) ?? groups[0])

  return (
    <div className="page">
      <header className="page__header">
        <div>
          <h1>{tabLabel(tab)}</h1>
          <p className="muted">
            Bu sekme size özeldir; toplu kapatmalarda kapanmaz. Kısayol eklemek için menüde bir ekrana sağ tıklayıp
            Kısayol ekle'den bu sekmeyi seçin.
          </p>
        </div>
      </header>

      {items.length === 0 ? (
        <p className="muted">Bu sekmede henüz kısayol yok.</p>
      ) : (
        <nav className="shortcuts" aria-label={`${tabLabel(tab)} kısayolları`}>
          {items.map((item) => {
            const ShortcutIcon = navIcon(item.path)
            return (
              <Link
                key={item.path}
                to={item.path}
                className="shortcut"
                style={{ '--module': moduleStyle(moduleOfPath(item.path)).color } as CSSProperties}
                onContextMenu={(event) => {
                  event.preventDefault()
                  setMenu({ x: event.clientX, y: event.clientY, item })
                }}
              >
                <span className="shortcut__icon"><ShortcutIcon aria-hidden="true" /></span>
                <span className="shortcut__label">{item.label}</span>
              </Link>
            )
          })}
        </nav>
      )}

      {menu && (
        <ContextMenu
          x={menu.x}
          y={menu.y}
          label={menu.item.label}
          onClose={() => setMenu(null)}
          items={[
            {
              key: 'new-tab', label: 'Yeni sekmede aç', icon: ExternalLink,
              disabled: !workTabs.canOpenInNewTab, onSelect: () => workTabs.openInNewTab(menu.item.path),
            },
            { key: 'remove', label: 'Kısayolu kaldır', icon: StarOff, separated: true, onSelect: () => onRemoveShortcut(menu.item) },
          ]}
        />
      )}
    </div>
  )
}
