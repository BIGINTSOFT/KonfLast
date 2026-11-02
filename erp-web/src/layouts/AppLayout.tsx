import {
  ArrowLeftToLine, ArrowRightToLine, Bookmark, Check, ChevronDown, ChevronRight, KeyRound, LogOut, Menu, Moon, Pencil,
  PictureInPicture2, Plus, RotateCw, Star, Sun, X,
} from 'lucide-react'
import { createElement, useCallback, useEffect, useLayoutEffect, useMemo, useRef, useState } from 'react'
import type { CSSProperties, PointerEvent as ReactPointerEvent, ReactNode } from 'react'
import { Link, NavLink, useLocation, useNavigate } from 'react-router-dom'
import { BrandMark } from '../components/BrandMark'
import { ContextMenu } from '../components/ContextMenu'
import type { ContextMenuItem } from '../components/ContextMenu'
import { useLogoutMutation } from '../features/auth/authApi'
import { LiveIndicator } from '../features/live/LiveIndicator'
import { GlobalSearch } from '../features/workspace/GlobalSearch'
import { NotificationBell } from '../features/workspace/NotificationBell'
import { useShortcutActions } from '../features/workspace/useShortcutActions'
import { useLiveUpdates } from '../features/live/useLiveUpdates'
import { useActiveProject } from '../features/system/activeProject'
import { projectLabels } from '../features/system/systemApi'
import type { ProjectKey } from '../features/system/systemApi'
import { useTheme } from '../features/theme/theme'
import { useAppSelector } from '../store/hooks'
import { initials } from '../utils/format'
import { CustomTabPage } from './CustomTabPage'
import { findNavTrail, isForeignPath, navGroups, navModules, projectNavGroups, visibleNavGroups } from './navigation'
import type { NavItem } from './navigation'
import { moduleStyle, navIcon } from './navPresentation'
import { SidebarNav } from './SidebarNav'
import { TabFrame } from './TabFrame'
import {
  activateTab, activeTab, addCustomTab, addTab, addTabShortcuts, blurWindows, canAddCustomTab, canAddTab, closeTab, closeTabs,
  closeWindows, detachTab, dockTab, focusWindow, openScreen, HOME_TAB, INITIAL_TABS, MAX_TABS, minimizeWindow, moveTab, moveWindow, navigateTabs, navigateWindow,
  newTabId, parseTabState, removeTabShortcut, renameTab, TAB_NAME_MAX_LENGTH, TAB_SHORTCUTS_MAX, tabKeyOf, tabLabel, tabScreen,
  tabTitle,
} from './tabs'
import type { CloseScope, TabState, WindowRect, WorkTab } from './tabs'
import { clampWindow } from './windowGeometry'
import type { Point } from './windowGeometry'
import { WorkTabsContext } from './workTabs'
import type { WorkTabsApi } from './workTabs'

/** Bu tarayıcı sekmesinde açık olan çalışma sekmeleri (sayfa yenilense de kalır). */
const TABS_STORAGE_KEY = 'erp.tabs'
/** Kullanıcının özel sekmeleri: oturum kapansa da kalır. */
const customTabsKey = (owner: string) => `erp.tabs.custom.${owner}`
const NOTICE_MS = 5000

function readStoredTabs(owner: string): TabState {
  try {
    return parseTabState(sessionStorage.getItem(TABS_STORAGE_KEY), localStorage.getItem(customTabsKey(owner)), owner)
  } catch {
    return INITIAL_TABS
  }
}

/** Koparılan sekmenin penceresi: imlecin altında, görünüme sığacak boyutta. */
function windowAt(point: Point): WindowRect {
  const width = Math.min(980, window.innerWidth - 120)
  const height = Math.min(620, window.innerHeight - 180)
  // Yeni pencere bütünüyle görünür açılır (düğmeleri ekranın dışında kalmaz).
  return clampWindow({
    x: Math.max(8, Math.min(point.x - 160, window.innerWidth - width - 8)),
    y: Math.max(8, Math.min(point.y - 14, window.innerHeight - height - 8)),
    width,
    height,
  })
}

const tabIcon = (tab: WorkTab) => (tab.custom ? Bookmark : navIcon(tabScreen(tab)))

/**
 * Uygulama kabuğu: üst çubuk, modül şeridi, yan menü ve sekmeli çalışma alanı. Seçilen ekran etkin sekmede
 * açılır; yeni sekme sağ tık menülerinden açılır. Sekme şeritten koparılıp ayrı pencere yapılabilir (sürükleyip
 * şeride bırakılınca geri yerleşir, simge durumuna küçültülebilir). Kullanıcının eklediği özel sekmeler kendi
 * kısayollarını tutar ve toplu kapatmada kalır. Açık sekmeler oturum boyunca (sayfa yenilense de), özel sekmeler
 * oturumlar arasında hatırlanır.
 */
export function AppLayout({ routes }: { routes: ReactNode }) {
  const user = useAppSelector((state) => state.auth.user)
  const [logout, { isLoading }] = useLogoutMutation()
  const { pathname, search } = useLocation()
  const navigate = useNavigate()
  const [theme, toggleTheme] = useTheme()
  const [navOpen, setNavOpen] = useState(false)
  // Daraltılmış menüde yalnızca modül rayı görünür; tercih tarayıcıda saklanır.
  const [navCollapsed, setNavCollapsed] = useState(() => {
    try {
      return localStorage.getItem('erp.navCollapsed') === '1'
    } catch {
      return false
    }
  })
  const changeNavCollapsed = (value: boolean) => {
    setNavCollapsed(value)
    try {
      localStorage.setItem('erp.navCollapsed', value ? '1' : '0')
    } catch {
      // Depolama kapalıysa tercih yalnızca bu oturumda geçerlidir.
    }
  }
  const userMenu = useRef<HTMLDetailsElement>(null)
  const tabMenu = useRef<HTMLDetailsElement>(null)
  const owner = user?.userName ?? ''
  const [storedTabs, setStoredTabs] = useState(() => readStoredTabs(owner))
  // Oturum boyunca tek canlı bağlantı; açık olan ekran hangisiyse onun verisi yenilenir.
  useLiveUpdates()

  // Sekme değiştirilirken adres bir an daha eski ekrandadır; geçiş bitene dek adres sekmeye işlenmez.
  const location = `${pathname}${search}`
  const [switching, setSwitching] = useState<string | null>(null)
  if (switching !== null && switching !== location) {
    setSwitching(null)
  }

  // Gidilen adres etkin sekmeye işlenir (sekme değiştirme dışındaki her gezinme).
  const tabState = useMemo(
    () => (switching === location ? storedTabs : navigateTabs(storedTabs, pathname, search)),
    [storedTabs, pathname, search, switching, location],
  )
  if (tabState !== storedTabs) {
    setStoredTabs(tabState)
  }

  const { tabs, activeId } = tabState

  // Sekme yenileme: yalnızca o sekmenin (ya da öndeki pencerenin) ekranı yeniden kurulur; sorgular kurulurken
  // sunucudan yeniden istendiği için (refetchOnMountOrArgChange) yalnızca o ekranın verisi tazelenir. Tarayıcı
  // sayfası ve diğer sekmeler yerinde kalır.
  const [reloads, setReloads] = useState<Record<string, number>>({})
  const reloadTab = useCallback((tabId: string) => {
    setReloads((current) => ({ ...current, [tabId]: (current[tabId] ?? 0) + 1 }))
  }, [])

  useEffect(() => {
    const onKey = (event: KeyboardEvent) => {
      // F5 / Ctrl+R tarayıcıyı değil etkin sekmeyi yeniler; Ctrl+Shift+R ve Ctrl+F5 tam yenileme olarak kalır.
      const reload = (event.key === 'F5' && !event.ctrlKey) || ((event.ctrlKey || event.metaKey) && !event.shiftKey && event.key.toLowerCase() === 'r')
      if (reload) {
        event.preventDefault()
        event.stopPropagation()
        reloadTab(tabState.focusId ?? activeId)
      }
    }
    // Yakalama aşamasında: ekranlardaki tuş işleyicileri olayı durdursa da tarayıcı sayfası yenilenmez.
    window.addEventListener('keydown', onKey, true)
    return () => window.removeEventListener('keydown', onKey, true)
  }, [activeId, tabState.focusId, reloadTab])
  const canAdd = canAddTab(tabState)

  useEffect(() => {
    try {
      sessionStorage.setItem(TABS_STORAGE_KEY, JSON.stringify({ owner, ...tabState }))
      localStorage.setItem(customTabsKey(owner), JSON.stringify(tabState.tabs.filter((tab) => tab.custom)))
    } catch {
      // Depolama kapalıysa sekmeler yalnızca bu sayfa açıkken hatırlanır.
    }
  }, [tabState, owner])

  /** Sekme durumunu uygular; etkin sekme değiştiyse onun adresine gidilir. */
  const apply = useCallback((next: TabState) => {
    setStoredTabs(next)
    const target = activeTab(next).path
    if (target !== location) {
      setSwitching(location)
      navigate(target)
    }
  }, [location, navigate])

  const [notice, setNotice] = useState<string | null>(null)
  const notify = useCallback((message: string) => setNotice(message), [])
  useEffect(() => {
    if (notice === null) {
      return
    }

    const timer = window.setTimeout(() => setNotice(null), NOTICE_MS)
    return () => window.clearTimeout(timer)
  }, [notice])

  const shortcuts = useShortcutActions(notify)
  /** Sağ tık menüsü: bir sekmede, sekme şeridinin boş yerinde ya da bir modülde. */
  const [menu, setMenu] = useState<{ x: number; y: number; tabId?: string; module?: string; windowId?: string } | null>(null)
  /** Adı sekmenin üzerinde, yerinde düzenlenen sekme. */
  const [renaming, setRenaming] = useState<{ id: string; name: string } | null>(null)
  const startRename = (tab: WorkTab) => setRenaming({ id: tab.id, name: tabLabel(tab) })
  const canAddCustom = canAddCustomTab(tabState)

  const closeTabMenu = () => {
    if (tabMenu.current) {
      tabMenu.current.open = false
    }
  }

  const openCustomTab = (paths: string[] = []) => apply(addCustomTab(tabState, newTabId(), paths))

  const addToTab = (tab: WorkTab, items: NavItem[]) => {
    const { state: next, added } = addTabShortcuts(tabState, tab.id, items.map((item) => item.path))
    setStoredTabs(next)
    if (added.length === 0) {
      notify(`"${tabLabel(tab)}" sekmesine en fazla ${TAB_SHORTCUTS_MAX} kısayol eklenebilir.`)
    } else {
      notify(added.length === 1
        ? `"${items.find((item) => item.path === added[0])?.label}" kısayolu "${tabLabel(tab)}" sekmesine eklendi.`
        : `${added.length} kısayol "${tabLabel(tab)}" sekmesine eklendi.`)
    }
  }

  const removeFromTab = (tab: WorkTab, item: NavItem) => {
    setStoredTabs(removeTabShortcut(tabState, tab.id, item.path))
    notify(`"${item.label}" kısayolu "${tabLabel(tab)}" sekmesinden kaldırıldı.`)
  }

  /**
   * "Kısayol ekle" öğesi: alt menüde ana sayfa ve özel sekmeler. Tek ekranda, kısayolu zaten olan sekme işaretli
   * gelir ve seçilince kısayol kaldırılır; birden fazla ekran (modül) topluca eklenir.
   */
  const shortcutMenu = (items: NavItem[]): ContextMenuItem => {
    const screens = items.filter((item) => item.path !== HOME_TAB.path)
    const single = screens.length === 1 ? screens[0] : null
    const target = (key: string, label: string, has: (path: string) => boolean, add: () => void, remove: (item: NavItem) => void, ready = true): ContextMenuItem => {
      const all = screens.every((item) => has(item.path))
      return {
        key, label, icon: all ? Check : undefined, disabled: !ready || (single === null && all),
        onSelect: () => (single !== null && all ? remove(single) : add()),
      }
    }

    return {
      key: 'shortcut', label: 'Kısayol ekle', icon: Star, disabled: screens.length === 0,
      children: [
        target('home', 'Ana Sayfa', shortcuts.has, () => void shortcuts.add(screens), (item) => void shortcuts.remove(item), shortcuts.ready),
        ...tabs.filter((tab) => tab.custom).map((tab) =>
          target(tab.id, tabLabel(tab), (path) => tab.shortcuts?.includes(path) ?? false, () => addToTab(tab, screens), (item) => removeFromTab(tab, item))),
        {
          key: 'new', label: 'Yeni sekmeye ekle', icon: Plus, separated: true, disabled: !canAddCustom,
          onSelect: () => openCustomTab(screens.map((item) => item.path)),
        },
      ],
    }
  }

  const workTabs: WorkTabsApi = {
    openInNewTab: (path) => apply(addTab(tabState, { id: newTabId(), path })),
    openScreen: (path) => apply(openScreen(tabState, path)),
    canOpenInNewTab: canAdd,
    shortcutMenu,
    notify,
  }

  // ── Ayrı pencereler ──
  const strip = useRef<HTMLDivElement>(null)
  /** Sekme şeritten aşağı sürüklenirken açılacak pencerenin taslağı. */
  const [tearing, setTearing] = useState<WindowRect | null>(null)
  /** Pencere sürüklenirken şeridin üzerinde mi (bırakılırsa sekmeye yerleşir). */
  const [dockHover, setDockHover] = useState(false)
  const latest = useRef({ tabState, apply })
  useLayoutEffect(() => {
    latest.current = { tabState, apply }
  })

  const overStrip = (point: Point) => {
    const bounds = strip.current?.getBoundingClientRect()
    return bounds !== undefined && point.y >= bounds.top - 8 && point.y <= bounds.bottom + 8 && point.x >= bounds.left && point.x <= bounds.right
  }

  /** Bırakılan pencere şeridin üzerindeyse, altındaki sekmenin önüne (yoksa sona) yerleşir; değilse yeri güncellenir. */
  const dropWindow = (id: string, rect: WindowRect, point: Point) => {
    setDockHover(false)
    if (!overStrip(point)) {
      setStoredTabs(moveWindow(tabState, id, rect))
      return
    }

    const before = [...(strip.current?.querySelectorAll<HTMLElement>('[data-tab-id]') ?? [])].find((element) => {
      const bounds = element.getBoundingClientRect()
      return point.x < bounds.left + bounds.width / 2
    })
    apply(dockTab(tabState, id, before?.dataset.tabId))
  }

  /** Şeritte sürüklenmekte olan sekme. */
  const [draggingTab, setDraggingTab] = useState<string | null>(null)

  /**
   * Sekme (ana sayfa dışında) şeritte yana sürüklenince sıradaki yeri değişir; aşağı sürüklenip bırakılınca ayrı
   * pencere olur.
   */
  const startTabDrag = (event: ReactPointerEvent<HTMLElement>, tab: WorkTab) => {
    if (event.button !== 0 || tab.id === HOME_TAB.id || (event.target as Element).closest('.tabstrip__close, input')) {
      return
    }

    const origin = { x: event.clientX, y: event.clientY }
    const bottom = strip.current?.getBoundingClientRect().bottom ?? 0
    const torn = (moved: PointerEvent) => moved.clientY > bottom + 28
    let dragging = false

    const move = (moved: PointerEvent) => {
      // Küçük el oynaması tıklama sayılır.
      dragging ||= Math.abs(moved.clientX - origin.x) > 6 || Math.abs(moved.clientY - origin.y) > 6
      if (!dragging) {
        return
      }

      setDraggingTab(tab.id)
      setTearing(torn(moved) ? windowAt({ x: moved.clientX, y: moved.clientY }) : null)
      if (!torn(moved)) {
        // İmlecin solunda kaldığı ilk sekmenin önüne geçer; hiçbiri yoksa sona.
        const before = [...(strip.current?.querySelectorAll<HTMLElement>('[data-tab-id]') ?? [])].find((element) => {
          const bounds = element.getBoundingClientRect()
          return element.dataset.tabId !== tab.id && moved.clientX < bounds.left + bounds.width / 2
        })
        setStoredTabs((current) => moveTab(current, tab.id, before?.dataset.tabId))
      }
    }

    const end = (ended: PointerEvent) => {
      document.removeEventListener('pointermove', move)
      document.removeEventListener('pointerup', end)
      document.removeEventListener('pointercancel', end)
      setDraggingTab(null)
      setTearing(null)
      if (dragging && ended.type === 'pointerup' && torn(ended)) {
        latest.current.apply(detachTab(latest.current.tabState, tab.id, windowAt({ x: ended.clientX, y: ended.clientY })))
      }
    }

    document.addEventListener('pointermove', move)
    document.addEventListener('pointerup', end)
    document.addEventListener('pointercancel', end)
  }

  // Tıklanan pencere öne gelir; çalışma alanının başka bir yerine tıklanınca pencereler geri plana düşer.
  useEffect(() => {
    const focus = (event: PointerEvent) => {
      const target = event.target instanceof Element ? event.target : null
      const id = target?.closest('[data-window-id]')?.getAttribute('data-window-id')
      if (id) {
        setStoredTabs((current) => focusWindow(current, id))
      } else if (target?.closest('.shell') && !target.closest('.windowbar')) {
        setStoredTabs(blurWindows)
      }
    }

    document.addEventListener('pointerdown', focus, true)
    return () => document.removeEventListener('pointerdown', focus, true)
  }, [])

  const docked = tabs.filter((tab) => !tab.floating)
  const windows = tabs.filter((tab) => tab.floating)
  // Üst üste binme sırası: en son öne alınan en üstte (menülerin ve iletişim kutularının altında kalır).
  const stacking = [...windows].sort((a, b) => a.floating!.z - b.floating!.z).map((tab) => tab.id)

  const tabMenuItems = (tabId?: string): ContextMenuItem[] => {
    const anchor = tabId ?? activeId
    const bulk = (scope: CloseScope, label: string, icon?: ContextMenuItem['icon']): ContextMenuItem => {
      const next = closeTabs(tabState, scope, anchor)
      return { key: scope, label, icon, disabled: next === tabState, onSelect: () => apply(next) }
    }
    const newTab: ContextMenuItem = { key: 'new', label: 'Yeni sekme ekle', icon: Plus, disabled: !canAddCustom, onSelect: () => openCustomTab() }
    const tab = tabs.find((item) => item.id === tabId)
    if (!tab) {
      return [newTab, { ...bulk('all', 'Tümünü kapat'), separated: true }]
    }

    const fixed = tab.id === HOME_TAB.id
    return [
      { key: 'reload', label: 'Yenile (F5)', icon: RotateCw, onSelect: () => reloadTab(tab.id) },
      { key: 'rename', label: 'Yeniden adlandır', icon: Pencil, disabled: fixed, onSelect: () => startRename(tab) },
      {
        key: 'detach', label: 'Pencere olarak ayır', icon: PictureInPicture2, disabled: fixed,
        onSelect: () => apply(detachTab(tabState, tab.id, windowAt({ x: 460 + windows.length * 28, y: 150 + windows.length * 28 }))),
      },
      { key: 'close', label: 'Kapat', icon: X, disabled: fixed, separated: true, onSelect: () => apply(closeTab(tabState, tab.id)) },
      bulk('others', 'Diğerlerini kapat'),
      bulk('left', 'Soldakileri kapat', ArrowLeftToLine),
      bulk('right', 'Sağdakileri kapat', ArrowRightToLine),
      bulk('all', 'Tümünü kapat'),
      { ...newTab, separated: true },
    ]
  }

  /** Alttaki pencere şeridinde (simge durumundaki pencereler) sağ tık menüsü: sekmelerdekiyle aynı toplu kapatma. */
  const windowMenuItems = (windowId: string): ContextMenuItem[] => {
    const bulk = (scope: CloseScope, label: string, icon?: ContextMenuItem['icon']): ContextMenuItem => {
      const next = closeWindows(tabState, scope, windowId)
      return { key: scope, label, icon, disabled: next === tabState, onSelect: () => apply(next) }
    }
    return [
      { key: 'open', label: 'Aç', onSelect: () => setStoredTabs(focusWindow(tabState, windowId)) },
      { key: 'reload', label: 'Yenile', icon: RotateCw, onSelect: () => reloadTab(windowId) },
      { key: 'dock', label: 'Şeride sekme olarak al', onSelect: () => apply(dockTab(tabState, windowId)) },
      { key: 'close', label: 'Kapat', icon: X, separated: true, onSelect: () => apply(closeTab(tabState, windowId)) },
      bulk('others', 'Diğerlerini kapat'),
      bulk('left', 'Soldakileri kapat', ArrowLeftToLine),
      bulk('right', 'Sağdakileri kapat', ArrowRightToLine),
      bulk('all', 'Tümünü kapat'),
    ]
  }

  /** Yazılan adı kaydeder. Boş ya da ekranın kendi adıyla aynı ad, sekmeyi yeniden ekranın adına bağlar. */
  const commitRename = () => {
    const tab = renaming && tabs.find((item) => item.id === renaming.id)
    if (renaming && tab) {
      const name = renaming.name.trim()
      setStoredTabs(renameTab(tabState, tab.id, name === tabLabel({ ...tab, name: undefined }) ? '' : name))
    }
    setRenaming(null)
  }

  // Açılır menüler (hesap, sekmeler) dışarı tıklanınca ya da Esc ile kapanır.
  useEffect(() => {
    const close = (event: Event) => {
      for (const menu of [userMenu.current, tabMenu.current]) {
        if (menu?.open && (event instanceof KeyboardEvent ? event.key === 'Escape' : !menu.contains(event.target as Node))) {
          menu.open = false
        }
      }
    }

    document.addEventListener('pointerdown', close)
    document.addEventListener('keydown', close)
    return () => {
      document.removeEventListener('pointerdown', close)
      document.removeEventListener('keydown', close)
    }
  }, [])

  // Menü: çekirdek ekranlar ve üst çubukta seçili projenin (Konfeksiyon, Boyahane, Dokuma) ekranları.
  const { project, projects, select: selectProject } = useActiveProject()
  const groups = visibleNavGroups(projectNavGroups(navGroups, project), user?.permissions ?? [])
  const changeProject = (next: ProjectKey) => {
    if (next !== project) {
      // Önceki projenin ekranları açık kalmaz; verisi yeni projeye taşınmasın.
      selectProject(next)
      apply(closeTabs(tabState, 'all', HOME_TAB.id))
    }
  }
  // Menü ve başlık etkin sekmenin adresinden okunur: menüden ekran açılınca sekme hemen değişir, adres ise yönlendirici
  // geçişi bitince (ilk açılışta ekranın kodu yüklenince) gelir. Adresten okunsaydı menü o arada eski modüle dönüp
  // titrerdi.
  const screenPath = activeTab(tabState).path.split('?')[0]
  // Oturumdan geri gelen ya da adresten açılan başka proje ekranı kapatılır.
  const foreignTab = project ? tabs.find((tab) => isForeignPath(navGroups, tab.path.split('?')[0], project)) : undefined
  useEffect(() => {
    if (foreignTab) {
      apply(closeTab(tabState, foreignTab.id))
    }
  }, [foreignTab, tabState, apply])
  const trail = findNavTrail(navGroups, screenPath)
  const title = trail?.label ?? tabTitle(tabKeyOf(screenPath))
  // Menü dışındaki ekranlar (ör. parola değiştirme) Genel modülünde sayılır.
  const activeModule = trail?.module ?? 'Genel'
  const modules = navModules(groups)
  const ActiveModuleIcon = moduleStyle(activeModule).icon

  const closeUserMenu = () => {
    if (userMenu.current) {
      userMenu.current.open = false
    }
  }

  const moduleMenuItems = (title: string): ContextMenuItem[] => {
    const items = modules.find((module) => module.title === title)?.groups.flatMap((group) => group.items) ?? []
    return [{ ...shortcutMenu(items), label: 'Ekranlarını kısayol ekle' }]
  }
  return (
    <WorkTabsContext.Provider value={workTabs}>
    <div
      className={['shell', navOpen ? 'shell--nav-open' : '', navCollapsed ? 'shell--rail' : ''].filter(Boolean).join(' ')}
      style={{ '--module': moduleStyle(activeModule).color } as CSSProperties}
    >
      <header className="appbar">
        <div className="appbar__top">
          <button
            type="button"
            className="appbar__icon appbar__menu"
            aria-label="Menüyü aç"
            aria-controls="app-sidebar"
            aria-expanded={navOpen}
            onClick={() => setNavOpen(true)}
          >
            <Menu aria-hidden="true" />
          </button>
          <Link to="/" className="appbar__brand">
            <BrandMark />
            <span>Tekstil ERP</span>
          </Link>

          <GlobalSearch groups={groups} />

          <div className="appbar__tools">
            <LiveIndicator />
            <NotificationBell />
            <button
              type="button"
              className="appbar__icon"
              onClick={toggleTheme}
              aria-label={theme === 'dark' ? 'Açık temaya geç' : 'Koyu temaya geç'}
              title={theme === 'dark' ? 'Açık tema' : 'Koyu tema'}
            >
              {theme === 'dark' ? <Sun aria-hidden="true" /> : <Moon aria-hidden="true" />}
            </button>

            <details className="user-menu" ref={userMenu}>
              <summary aria-label={`Hesap menüsü: ${user?.fullName ?? ''}`}>
                <span className="avatar" aria-hidden="true">{initials(user?.fullName)}</span>
                <span className="appbar__user">
                  <strong>{user?.fullName}</strong>
                  <span>{user?.roles.join(', ')}</span>
                </span>
                <ChevronDown className="user-menu__chevron" aria-hidden="true" />
              </summary>
              <div className="user-menu__panel">
                {projects.length > 1 && (
                  <div className="user-menu__section" role="group" aria-label="Proje">
                    {projects.map((key) => (
                      <button
                        key={key}
                        type="button"
                        className="user-menu__item"
                        aria-pressed={key === project}
                        onClick={() => {
                          closeUserMenu()
                          changeProject(key)
                        }}
                      >
                        {key === project ? <Check aria-hidden="true" /> : <span className="user-menu__spacer" aria-hidden="true" />}
                        {projectLabels[key]}
                      </button>
                    ))}
                  </div>
                )}
                <NavLink to="/account/password" className="user-menu__item" onClick={closeUserMenu}>
                  <KeyRound aria-hidden="true" />
                  Parola değiştir
                </NavLink>
                <button
                  type="button"
                  className="user-menu__item user-menu__item--danger"
                  onClick={() => void logout()}
                  disabled={isLoading}
                >
                  <LogOut aria-hidden="true" />
                  Çıkış yap
                </button>
              </div>
            </details>
          </div>
        </div>

      </header>

      <aside className="sidepanel" id="app-sidebar">
        <SidebarNav
          groups={groups}
          activeModule={activeModule}
          collapsed={navCollapsed}
          onCollapsedChange={changeNavCollapsed}
          onNavigate={() => setNavOpen(false)}
          onModuleMenu={(x, y, module) => setMenu({ x, y, module })}
        />
      </aside>
      <button
        type="button"
        className="shell__backdrop"
        aria-label="Menüyü kapat"
        tabIndex={-1}
        onClick={() => setNavOpen(false)}
      />

      <main className="shell__main">
        <div
          ref={strip}
          className={dockHover ? 'tabstrip tabstrip--drop' : 'tabstrip'}
          onContextMenu={(event) => {
            event.preventDefault()
            setMenu({ x: event.clientX, y: event.clientY })
          }}
        >
          <div className="tabstrip__tabs" role="tablist" aria-label="Açık sekmeler">
            {docked.map((tab) => {
              const active = tab.id === activeId
              const closable = tab.id !== HOME_TAB.id
              const label = tabLabel(tab)
              return (
                <div
                  key={tab.id}
                  data-tab-id={tab.id}
                  className={`tabstrip__tab${active ? ' tabstrip__tab--active' : ''}${tab.custom ? ' tabstrip__tab--custom' : ''}${draggingTab === tab.id ? ' tabstrip__tab--dragging' : ''}`}
                  // Yana sürüklenen sekme sıra değiştirir, aşağı sürüklenen ayrı pencere olur; orta tuş kapatır, çift tıklama ad verir.
                  onPointerDown={(event) => startTabDrag(event, tab)}
                  onAuxClick={(event) => {
                    if (event.button === 1 && closable) {
                      event.preventDefault()
                      apply(closeTab(tabState, tab.id))
                    }
                  }}
                  onDoubleClick={() => closable && renaming?.id !== tab.id && startRename(tab)}
                  onContextMenu={(event) => {
                    event.preventDefault()
                    event.stopPropagation()
                    setMenu({ x: event.clientX, y: event.clientY, tabId: tab.id })
                  }}
                >
                  {renaming?.id === tab.id ? (
                    // Ad sekmenin üzerinde düzenlenir: Enter ya da başka yere tıklamak kaydeder, Esc vazgeçer.
                    <span className="tabstrip__link tabstrip__link--editing">
                      {createElement(tabIcon(tab), { 'aria-hidden': true })}
                      <input
                        className="tabstrip__rename"
                        aria-label="Sekme adı"
                        value={renaming.name}
                        maxLength={TAB_NAME_MAX_LENGTH}
                        autoComplete="off"
                        autoFocus
                        onFocus={(event) => event.target.select()}
                        onChange={(event) => setRenaming({ id: tab.id, name: event.target.value })}
                        onBlur={commitRename}
                        onKeyDown={(event) => {
                          if (event.key === 'Enter') {
                            event.preventDefault()
                            commitRename()
                          } else if (event.key === 'Escape') {
                            event.preventDefault()
                            setRenaming(null)
                          }
                        }}
                      />
                    </span>
                  ) : (
                    <button
                      type="button"
                      role="tab"
                      aria-selected={active}
                      className="tabstrip__link"
                      title={tab.custom ? `${label} (özel sekme)` : label}
                      onClick={() => apply(activateTab(tabState, tab.id))}
                    >
                      {createElement(tabIcon(tab), { 'aria-hidden': true })}
                      <span>{label}</span>
                    </button>
                  )}
                  {closable && (
                    <button
                      type="button"
                      className="tabstrip__close"
                      aria-label={`${label} sekmesini kapat`}
                      onClick={() => apply(closeTab(tabState, tab.id))}
                    >
                      <X aria-hidden="true" />
                    </button>
                  )}
                </div>
              )
            })}
          </div>
          <details className="user-menu tabstrip__menu" ref={tabMenu}>
            <summary aria-label="Sekme işlemleri" title="Sekme işlemleri">
              <ChevronDown aria-hidden="true" />
            </summary>
            <div className="user-menu__panel">
              {tabMenuItems().map((item) => (
                <button
                  key={item.key}
                  type="button"
                  className="user-menu__item"
                  disabled={item.disabled}
                  onClick={() => {
                    closeTabMenu()
                    item.onSelect?.()
                  }}
                >
                  {item.label}
                </button>
              ))}
            </div>
          </details>
        </div>

        <div className="tabpanes">
          {tabs.map((tab) => (
            <TabFrame
              key={`${tab.id}:${reloads[tab.id] ?? 0}`}
              tab={tab}
              active={tab.id === activeId}
              routes={routes}
              title={tabLabel(tab)}
              icon={tabIcon(tab)}
              focused={tab.id === tabState.focusId}
              zIndex={20 + stacking.indexOf(tab.id)}
              onNavigate={(path) => setStoredTabs((current) => navigateWindow(current, tab.id, path))}
              onDragOver={(point) => setDockHover(point !== null && overStrip(point))}
              onDrop={(rect, point) => dropWindow(tab.id, rect, point)}
              onResize={(rect) => setStoredTabs(moveWindow(tabState, tab.id, rect))}
              onMinimize={() => setStoredTabs(minimizeWindow(tabState, tab.id))}
              onDock={() => apply(dockTab(tabState, tab.id))}
              onClose={() => apply(closeTab(tabState, tab.id))}
            >
              {tab.custom ? <CustomTabPage tab={tab} groups={groups} onRemoveShortcut={(item) => removeFromTab(tab, item)} /> : undefined}
            </TabFrame>
          ))}
        </div>
        {tearing && <div className="window-ghost" style={{ left: tearing.x, top: tearing.y, width: tearing.width, height: tearing.height }} />}

        {windows.some((tab) => tab.floating?.minimized) && (
          <div className="windowbar" role="toolbar" aria-label="Simge durumundaki pencereler">
            {windows.filter((tab) => tab.floating?.minimized).map((tab) => (
              <div
                key={tab.id}
                className="windowbar__item"
                onContextMenu={(event) => {
                  event.preventDefault()
                  setMenu({ x: event.clientX, y: event.clientY, windowId: tab.id })
                }}
              >
                <button
                  type="button"
                  className="windowbar__open"
                  title={`${tabLabel(tab)} penceresini aç`}
                  onClick={() => setStoredTabs(focusWindow(tabState, tab.id))}
                >
                  {createElement(tabIcon(tab), { 'aria-hidden': true })}
                  <span>{tabLabel(tab)}</span>
                </button>
                <button
                  type="button"
                  className="windowbar__close"
                  aria-label={`${tabLabel(tab)} penceresini kapat`}
                  onClick={() => apply(closeTab(tabState, tab.id))}
                >
                  <X aria-hidden="true" />
                </button>
              </div>
            ))}
          </div>
        )}

        <footer className="statusbar">
          <span className="breadcrumb__module">
            <ActiveModuleIcon aria-hidden="true" />
            {activeModule}
          </span>
          {trail?.group && trail.group !== activeModule && (
            <>
              <ChevronRight className="breadcrumb__separator" aria-hidden="true" />
              <span>{trail.group}</span>
            </>
          )}
          <ChevronRight className="breadcrumb__separator" aria-hidden="true" />
          <span className="breadcrumb__current">
            {trail && createElement(navIcon(screenPath), { 'aria-hidden': true })}
            {title}
          </span>
          <span className="statusbar__spacer" />
          {notice && <span className="statusbar__notice" role="status">{notice}</span>}
          <span>Açık sekme: {tabs.length} / {MAX_TABS}</span>
          <span>Kullanıcı: {user?.userName}</span>
        </footer>
      </main>

      {menu && (
        <ContextMenu
          x={menu.x}
          y={menu.y}
          label={menu.module ?? (menu.windowId ? 'Pencere işlemleri' : 'Sekme işlemleri')}
          items={menu.module ? moduleMenuItems(menu.module) : menu.windowId ? windowMenuItems(menu.windowId) : tabMenuItems(menu.tabId)}
          onClose={() => setMenu(null)}
        />
      )}

    </div>
    </WorkTabsContext.Provider>
  )
}
