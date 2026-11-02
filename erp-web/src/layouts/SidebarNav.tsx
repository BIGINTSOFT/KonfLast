import { ArrowRight, ChevronsLeft, ChevronsRight, ExternalLink, Search } from 'lucide-react'
import { useState } from 'react'
import type { CSSProperties, KeyboardEvent } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { ContextMenu } from '../components/ContextMenu'
import { navModules, searchNav } from './navigation'
import type { NavGroup, NavItem } from './navigation'
import { moduleStyle, navIcon } from './navPresentation'
import { tabKeyOf } from './tabs'
import { useWorkTabs } from './workTabs'

/**
 * Sol menü iki katmanlıdır: solda bütün modüllerin simgeli rayı, yanında seçili modülün ekranları. Rayda modüle
 * tıklanınca panel o modülü gösterir; seçim o anki ekrana bağlıdır, başka modülün ekranına geçilince panel o modüle
 * döner. Panel daraltılırsa yalnızca ray kalır; raydan modül seçilince ekranlar içeriğin üstünde açılır ve ekran seçilince,
 * fare menüden çıkınca ya da Esc ile kapanır. Arama bütün modüllerde
 * yapılır. Ekran tıklanınca etkin sekmede açılır; sağ tık menüsünden (ya da Ctrl / orta tuşla) yeni sekmede açılır ve
 * kısayollara eklenir. Modüle sağ tıklanınca modülün ekranları kısayol olarak eklenebilir.
 */
export function SidebarNav({
  groups,
  activeModule,
  collapsed,
  onCollapsedChange,
  onNavigate,
  onModuleMenu,
}: {
  groups: NavGroup[]
  activeModule: string
  collapsed: boolean
  onCollapsedChange: (collapsed: boolean) => void
  onNavigate: () => void
  onModuleMenu: (x: number, y: number, module: string) => void
}) {
  const navigate = useNavigate()
  // Etkin öğe, adrese en uzun yolu uyan öğedir: /bundles/card-release'te Demetler değil Kart Boşaltma seçilir;
  // ayrıntı sayfaları (ör. /bundles/12) yine listesinin öğesini seçer.
  const activePath = tabKeyOf(useLocation().pathname)
  const workTabs = useWorkTabs()
  const [query, setQuery] = useState('')
  const [menu, setMenu] = useState<{ x: number; y: number; item: NavItem } | null>(null)
  const [flyout, setFlyout] = useState(false)
  const floating = collapsed && flyout

  // Raydan seçilen modül, açık ekranın modülü değişene kadar geçerlidir.
  const [browsed, setBrowsed] = useState<{ module: string; from: string } | null>(null)
  if (browsed !== null && browsed.from !== activeModule) {
    setBrowsed(null)
  }
  const shownModule = browsed?.module ?? activeModule

  const allModules = navModules(groups)
  const searching = query.trim() !== ''
  const modules = searching ? navModules(searchNav(groups, query)) : allModules.filter((module) => module.title === shownModule)
  const firstMatch = searching ? modules[0]?.groups[0]?.items[0] : undefined

  const go = () => {
    setQuery('')
    setFlyout(false)
    onNavigate()
  }

  const openInNewTab = (item: NavItem) => {
    workTabs.openInNewTab(item.path)
    go()
  }

  const selectModule = (title: string) => {
    setQuery('')
    setBrowsed(title === activeModule ? null : { module: title, from: activeModule })
    if (collapsed) {
      setFlyout(true)
    }
  }

  const handleSearchKey = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key === 'Enter' && firstMatch) {
      event.preventDefault()
      workTabs.openScreen(firstMatch.path)
      go()
    } else if (event.key === 'Escape' && query !== '') {
      event.preventDefault()
      setQuery('')
    } else if (event.key === 'Escape' && floating) {
      setFlyout(false)
    }
  }

  // Ana sayfa hep kendi sekmesindedir; ayrı sekmede açılmaz, kısayolu olmaz.
  const menuItems = (item: NavItem) => [
    {
      key: 'new-tab', label: 'Yeni sekmede aç', icon: ExternalLink,
      disabled: !workTabs.canOpenInNewTab, onSelect: () => openInNewTab(item),
    },
    {
      key: 'open', label: 'Etkin sekmede aç', icon: ArrowRight,
      onSelect: () => {
        navigate(item.path)
        go()
      },
    },
    { ...workTabs.shortcutMenu([item]), separated: true },
  ]

  const shownStyle = moduleStyle(shownModule)
  const ShownIcon = shownStyle.icon

  return (
    <div
      className="navframe"
      onMouseLeave={() => setFlyout(false)}
      onKeyDown={(event) => {
        if (event.key === 'Escape' && floating) {
          setFlyout(false)
        }
      }}
    >
      <nav className="navrail" aria-label="Modüller">
        {allModules.map((module) => {
          const { icon: ModuleIcon, color } = moduleStyle(module.title)
          const selected = !searching && module.title === shownModule
          const classes = ['navrail__item']
          if (selected) classes.push('navrail__item--selected')
          if (module.title === activeModule) classes.push('navrail__item--current')
          return (
            <button
              key={module.title}
              type="button"
              className={classes.join(' ')}
              style={{ '--module': color } as CSSProperties}
              aria-pressed={selected}
              title={module.title}
              onClick={() => selectModule(module.title)}
              onContextMenu={(event) => {
                event.preventDefault()
                onModuleMenu(event.clientX, event.clientY, module.title)
              }}
            >
              <span className="navrail__icon"><ModuleIcon aria-hidden="true" /></span>
              <span className="navrail__label">{module.title}</span>
            </button>
          )
        })}
        <button
          type="button"
          className="navrail__collapse"
          onClick={() => {
            setFlyout(false)
            onCollapsedChange(!collapsed)
          }}
          aria-label={collapsed ? 'Menüyü genişlet' : 'Menüyü daralt'}
          title={collapsed ? 'Menüyü genişlet' : 'Menüyü daralt'}
        >
          {collapsed ? <ChevronsRight aria-hidden="true" /> : <ChevronsLeft aria-hidden="true" />}
        </button>
      </nav>

      <div className={floating ? 'navpanel navpanel--flyout' : 'navpanel'} hidden={collapsed && !flyout}>
        <div className="navpanel__head" style={{ '--module': shownStyle.color } as CSSProperties}>
          {searching ? (
            <span className="navpanel__title">Arama sonuçları</span>
          ) : (
            <span className="navpanel__title">
              <ShownIcon aria-hidden="true" />
              {shownModule}
            </span>
          )}
        </div>
        <div className="sidenav__search">
          <Search aria-hidden="true" />
          <input
            type="search"
            placeholder="Menüde ara"
            aria-label="Menüde ara"
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            onKeyDown={handleSearchKey}
            autoComplete="off"
          />
        </div>

        <nav aria-label="Modül menüsü" className={searching ? 'sidenav sidenav--searching' : 'sidenav'}>
          {modules.map((module) => (
            <section
              key={module.title}
              className="sidenav__module"
              aria-label={module.title}
              style={{ '--module': moduleStyle(module.title).color } as CSSProperties}
            >
              {searching && <h2 className="sidenav__module-title">{module.title}</h2>}
              {module.groups.map((group, index) => (
                <div key={group.title ?? `main-${index}`} className="sidenav__group">
                  {module.groups.length > 1 && group.title && <h3 className="sidenav__group-title">{group.title}</h3>}
                  {group.items.map((item) => {
                    const ItemIcon = navIcon(item.path)
                    const separate = item.path !== '/' && workTabs.canOpenInNewTab
                    return (
                    <div key={item.path} className="sidenav__item">
                    <Link
                      to={item.path}
                      aria-current={item.path === activePath ? 'page' : undefined}
                      className={`sidenav__link${item.path === activePath ? ' active' : ''}${item === firstMatch ? ' sidenav__link--first' : ''}`}
                      onClick={(event) => {
                        // Ekran yeni sekmede açılır (aynı adres açıksa o sekmeye geçilir); açık sekme ezilmez.
                        // Ctrl + tık her zaman yeni sekme açar.
                        event.preventDefault()
                        if ((event.ctrlKey || event.metaKey) && separate) {
                          openInNewTab(item)
                        } else {
                          workTabs.openScreen(item.path)
                          go()
                        }
                      }}
                      onAuxClick={(event) => {
                        if (event.button === 1 && separate) {
                          event.preventDefault()
                          openInNewTab(item)
                        }
                      }}
                      onContextMenu={(event) => {
                        if (item.path !== '/') {
                          event.preventDefault()
                          setMenu({ x: event.clientX, y: event.clientY, item })
                        }
                      }}
                    >
                      <ItemIcon aria-hidden="true" />
                      {item.label}
                    </Link>
                    </div>
                    )
                  })}
                </div>
              ))}
            </section>
          ))}
          {searching && modules.length === 0 && <p className="sidenav__empty" role="status">Eşleşen ekran yok.</p>}
        </nav>
      </div>

      {menu && (
        <ContextMenu x={menu.x} y={menu.y} label={menu.item.label} items={menuItems(menu.item)} onClose={() => setMenu(null)} />
      )}
    </div>
  )
}
