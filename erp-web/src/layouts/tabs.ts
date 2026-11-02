import { navGroups } from './navigation'
import type { NavGroup } from './navigation'

/**
 * Çalışma alanındaki bir sekme. Sıradan sekme bir ekrana bağlı değildir: menüden seçilen ekran etkin sekmede
 * açılır (tarayıcı sekmesi gibi); yeni sekme yalnızca kullanıcı isteyince açılır. Ana sayfa ve özel sekmeler
 * ise kısayol sayfalarıdır: içlerinden açılan ekran bir çalışma sekmesinde açılır, kendileri yerinde kalır.
 */
export interface WorkTab {
  id: string
  /**
   * Sekmede açık olan adres (yol + sorgu); sekmeye dönülünce buraya gidilir. Ayrı pencere kendi içinde gezinir:
   * adres çubuğu her zaman şeritte seçili sekmenin adresidir.
   */
  path: string
  /**
   * Kullanıcının "Yeni sekme ekle" ile açtığı özel sekme: kendi kısayollarını tutan sayfa. Toplu kapatmalarda
   * kapanmaz, oturumlar arasında kalır.
   */
  custom?: boolean
  /** Özel sekmenin kısayolları: ekran adresleri, eklenme sırasıyla. */
  shortcuts?: string[]
  /** Sekme şeritten koparılıp ayrı pencere yapılmışsa penceresinin yeri; şeritteki sekmede yoktur. */
  floating?: FloatingWindow
  /** Kullanıcının verdiği ad; yoksa sekmedeki ekranın adı gösterilir. */
  name?: string
}

/** Pencerenin ekrandaki yeri (tarayıcı görünümüne göre, piksel). */
export interface WindowRect {
  x: number
  y: number
  width: number
  height: number
}

export interface FloatingWindow extends WindowRect {
  /** Simge durumunda: pencere gizlidir, alttaki pencere çubuğunda durur. */
  minimized?: boolean
  /** Üst üste binme sırası; en son öne alınan en büyüktür. */
  z: number
}

export interface TabState {
  tabs: WorkTab[]
  /** Şeritte seçili (çalışma alanında görünen) sekme; her zaman şeritteki bir sekmedir. */
  activeId: string
  /** Öndeki (en son tıklanan) ayrı pencere. */
  focusId?: string
}

/** Ana sayfa sekmesi hep ilk sıradadır; kapatılamaz, adı değiştirilemez ve başka ekran göstermez. */
export const HOME_TAB: WorkTab = { id: 'home', path: '/' }

export const INITIAL_TABS: TabState = { tabs: [HOME_TAB], activeId: HOME_TAB.id }

/** Açık sekmeler arka planda canlı kalır (veri yeniler); sayı sınırlıdır. */
export const MAX_TABS = 12

export const TAB_NAME_MAX_LENGTH = 30

/** Bir özel sekmedeki en fazla kısayol sayısı (ana sayfadaki sınırla aynı). */
export const TAB_SHORTCUTS_MAX = 24

/** Çalışma sekmesine hep yer kalsın diye özel sekme sayısı sınırlıdır. */
const MAX_CUSTOM_TABS = MAX_TABS - 2

/** Toplu kapatma kapsamı; "others", "left" ve "right" tıklanan sekmeye göredir. */
export type CloseScope = 'others' | 'left' | 'right' | 'all'

/** Menüde yer almayan ekranların sekme / başlık adı. */
const EXTRA_TITLES: Record<string, string> = {
  '/account/password': 'Parola değiştir',
}

function navItemOf(groups: NavGroup[], pathname: string) {
  let best: { path: string; label: string } | null = null
  for (const group of groups) {
    for (const item of group.items) {
      const matches = item.path === '/' ? pathname === '/' : pathname === item.path || pathname.startsWith(`${item.path}/`)
      if (matches && (best === null || item.path.length > best.path.length)) {
        best = item
      }
    }
  }

  return best
}

/** Adresin ait olduğu ekran: menüdeki öğenin adresi (ayrıntı sayfaları listesine bağlanır); menü dışında adresin kendisi. */
export function tabKeyOf(pathname: string, groups: NavGroup[] = navGroups): string {
  return navItemOf(groups, pathname)?.path ?? pathname
}

export function tabTitle(key: string, groups: NavGroup[] = navGroups): string {
  return navItemOf(groups, key)?.label ?? EXTRA_TITLES[key] ?? 'Sayfa'
}

const pathnameOf = (path: string) => path.split('?')[0]

/** Sekmede açık olan ekran (simge ve ad için). */
export function tabScreen(tab: WorkTab): string {
  return tabKeyOf(pathnameOf(tab.path))
}

/** Sekmenin görünen adı: verilen ad, yoksa ekranın adı; adsız özel sekmede "Yeni sekme". */
export function tabLabel(tab: WorkTab): string {
  if (tab.name) {
    return tab.name
  }

  return tab.custom ? 'Yeni sekme' : tabTitle(tabScreen(tab))
}

/**
 * Özel sekmenin adresi. Ana sayfanın adresinden sorgusuyla ayrılır: menüden "Ana Sayfa"ya gidilince ana sayfa
 * sekmesine geçilir, sayfa yenilenince özel sekmede kalınır.
 */
export const customTabPath = (id: string) => `${HOME_TAB.path}?sekme=${encodeURIComponent(id)}`

export function newTabId(): string {
  return `t${Date.now().toString(36)}${Math.random().toString(36).slice(2, 8)}`
}

const isOrdinary = (tab: WorkTab) => tab.id !== HOME_TAB.id && !tab.custom
const isDocked = (tab: WorkTab) => !tab.floating

/** Şeritte seçili sekme. */
export function activeTab(state: TabState): WorkTab {
  return state.tabs.find((tab) => tab.id === state.activeId && isDocked(tab)) ?? HOME_TAB
}

const topZ = (state: TabState) => state.tabs.reduce((top, tab) => Math.max(top, tab.floating?.z ?? 0), 0)

/** Aynı sekmeler ve seçimle, verilen pencere önde (ya da hiçbiri önde değil). */
const focused = (tabs: WorkTab[], activeId: string, focusId?: string): TabState =>
  (focusId === undefined ? { tabs, activeId } : { tabs, activeId, focusId })

/** Şeritteki sekmeyi seçer. */
export function activateTab(state: TabState, id: string): TabState {
  return state.tabs.some((tab) => tab.id === id && isDocked(tab)) ? focused(state.tabs, id, state.focusId) : state
}

/** Sınır dolmuşken yeni sekme, kapatılabilecek sıradan bir sekme varsa açılabilir. */
export function canAddTab(state: TabState): boolean {
  return state.tabs.length < MAX_TABS || state.tabs.some(isOrdinary)
}

export function canAddCustomTab(state: TabState): boolean {
  return canAddTab(state) && state.tabs.filter((tab) => tab.custom).length < MAX_CUSTOM_TABS
}

/** Yeni özel sekme (isteğe bağlı ilk kısayollarıyla); açılamıyorsa durum değişmez. */
export function addCustomTab(state: TabState, id: string, shortcuts: readonly string[] = []): TabState {
  return canAddCustomTab(state)
    ? addTab(state, { id, path: customTabPath(id), custom: true, ...(shortcuts.length > 0 ? { shortcuts: shortcuts.slice(0, TAB_SHORTCUTS_MAX) } : {}) })
    : state
}

/**
 * Sona yeni sekme ekler ve etkin yapar. Sınır aşılırsa en eski sıradan sekme (tercihen etkin olmayan) kapanır;
 * kapatılacak sekme yoksa durum değişmez.
 */
export function addTab(state: TabState, tab: WorkTab): TabState {
  const tabs = [...state.tabs]
  if (tabs.length >= MAX_TABS) {
    const background = tabs.findIndex((item) => isOrdinary(item) && isDocked(item) && item.id !== state.activeId)
    const victim = background >= 0 ? background : tabs.findIndex(isOrdinary)
    if (victim < 0) {
      return state
    }

    tabs.splice(victim, 1)
  }

  return focused([...tabs, tab], tab.id, tabs.some((item) => item.id === state.focusId) ? state.focusId : undefined)
}

/**
 * Gidilen adresi sekmelere işler. Adres şeritte seçili sekmede açılır. Ana sayfa adresi ana sayfa sekmesine
 * geçirir. Kısayol sayfasından (ana sayfa ya da özel sekme) bir ekrana gidilirse: o ekran şeritteki bir sekmede
 * zaten açıksa oraya, değilse şeritteki ilk sıradan sekmeye geçilir; hiç yoksa yeni sekme açılır.
 */
export function navigateTabs(state: TabState, pathname: string, search = '', createId: () => string = newTabId): TabState {
  const path = `${pathname}${search}`
  const active = activeTab(state)
  if (active.path === path) {
    return state
  }

  if (pathname === '/') {
    return focused(state.tabs, HOME_TAB.id, state.focusId)
  }

  const show = (target: WorkTab): TabState =>
    focused(state.tabs.map((tab) => (tab === target ? { ...tab, path } : tab)), target.id, state.focusId)

  if (isOrdinary(active)) {
    return show(active)
  }

  const screen = tabKeyOf(pathname)
  const ordinary = state.tabs.filter((tab) => isOrdinary(tab) && isDocked(tab))
  const target = ordinary.find((tab) => tabScreen(tab) === screen) ?? ordinary[0]
  return target ? show(target) : addTab(state, { id: createId(), path })
}

/** Ayrı pencerenin kendi içindeki gezinmesi: yalnızca o pencerenin adresi değişir. */
export function navigateWindow(state: TabState, id: string, path: string): TabState {
  const tab = state.tabs.find((item) => item.id === id)
  return !tab?.floating || tab.path === path
    ? state
    : { ...state, tabs: state.tabs.map((item) => (item === tab ? { ...tab, path } : item)) }
}

/**
 * Sekmeyi ya da pencereyi kapatır (ana sayfa kapatılamaz). Kapanan, şeritte seçili sekmeyse yanındaki (sağdaki,
 * yoksa soldaki) seçilir.
 */
export function closeTab(state: TabState, id: string): TabState {
  const closing = state.tabs.find((tab) => tab.id === id)
  if (!closing || id === HOME_TAB.id) {
    return state
  }

  const tabs = state.tabs.filter((tab) => tab !== closing)
  const focusId = state.focusId === id ? undefined : state.focusId
  if (closing.floating || id !== state.activeId) {
    return focused(tabs, state.activeId, focusId)
  }

  const docked = state.tabs.filter(isDocked)
  const index = docked.indexOf(closing)
  const neighbour = docked[index + 1] ?? docked[index - 1] ?? HOME_TAB
  return focused(tabs, neighbour.id, focusId)
}

/**
 * Toplu kapatma: kapsamdaki sıradan sekmeler kapanır; ana sayfa ve özel sekmeler kalır. "Tümü" ayrı pencereleri
 * de kapatır; diğer kapsamlar şeritteki sıraya göredir. Seçili sekme kapanırsa tıklanan sekme (o da kapandıysa
 * ana sayfa) seçilir.
 */
export function closeTabs(state: TabState, scope: CloseScope, anchorId: string): TabState {
  const docked = state.tabs.filter(isDocked)
  const anchor = docked.findIndex((tab) => tab.id === anchorId)
  const closes = (tab: WorkTab) => {
    if (!isOrdinary(tab) || scope === 'all') {
      return isOrdinary(tab)
    }

    const index = docked.indexOf(tab)
    return index >= 0 && anchor >= 0 && (scope === 'others' ? index !== anchor : scope === 'left' ? index < anchor : index > anchor)
  }

  const tabs = state.tabs.filter((tab) => !closes(tab))
  if (tabs.length === state.tabs.length) {
    return state
  }

  const stays = (id: string | undefined) => tabs.some((tab) => tab.id === id)
  return focused(
    tabs,
    stays(state.activeId) ? state.activeId : stays(anchorId) && anchor >= 0 ? anchorId : HOME_TAB.id,
    stays(state.focusId) ? state.focusId : undefined,
  )
}

/** Şeritteki sekmeyi (ana sayfa dışında) koparıp verilen yerde ayrı pencere yapar; pencere öne gelir. */
export function detachTab(state: TabState, id: string, rect: WindowRect): TabState {
  const tab = state.tabs.find((item) => item.id === id)
  if (!tab || id === HOME_TAB.id || tab.floating) {
    return state
  }

  const docked = state.tabs.filter(isDocked)
  const index = docked.indexOf(tab)
  const neighbour = docked[index + 1] ?? docked[index - 1] ?? HOME_TAB
  return {
    tabs: state.tabs.map((item) => (item === tab ? { ...tab, floating: { ...rect, z: topZ(state) + 1 } } : item)),
    activeId: id === state.activeId ? neighbour.id : state.activeId,
    focusId: id,
  }
}

/** Pencereyi şeride geri yerleştirir (verilen sekmenin önüne, verilmezse sona) ve seçer. */
export function dockTab(state: TabState, id: string, beforeId?: string): TabState {
  const tab = state.tabs.find((item) => item.id === id)
  if (!tab?.floating) {
    return state
  }

  const { floating: _floating, ...docked } = tab
  const rest = state.tabs.filter((item) => item !== tab)
  // Ana sayfa hep ilk sekmedir.
  const before = beforeId === undefined || beforeId === HOME_TAB.id ? -1 : rest.findIndex((item) => item.id === beforeId)
  const at = before < 0 ? rest.length : before
  return { tabs: [...rest.slice(0, at), docked, ...rest.slice(at)], activeId: id }
}

/**
 * Şeritteki sekmeyi başka bir sekmenin önüne (verilmezse sona) taşır. Ana sayfa yerinden oynamaz ve önüne sekme
 * gelmez. Sıra değişmiyorsa durum aynı kalır.
 */
export function moveTab(state: TabState, id: string, beforeId?: string): TabState {
  const tab = state.tabs.find((item) => item.id === id)
  if (!tab || tab.floating || id === HOME_TAB.id || beforeId === id) {
    return state
  }

  const rest = state.tabs.filter((item) => item !== tab)
  const before = beforeId === undefined ? -1 : rest.findIndex((item) => item.id === beforeId)
  const at = before < 0 ? rest.length : Math.max(before, 1)
  const tabs = [...rest.slice(0, at), tab, ...rest.slice(at)]
  return tabs.every((item, index) => item === state.tabs[index]) ? state : { ...state, tabs }
}

/** Pencereyi öne alır (simge durumundaysa açar). Zaten öndeyse durum değişmez. */
export function focusWindow(state: TabState, id: string): TabState {
  const tab = state.tabs.find((item) => item.id === id)
  if (!tab?.floating) {
    return state
  }

  const top = topZ(state)
  if (state.focusId === id && !tab.floating.minimized && tab.floating.z === top) {
    return state
  }

  const { minimized: _minimized, ...rect } = tab.floating
  return focused(state.tabs.map((item) => (item === tab ? { ...tab, floating: { ...rect, z: top + 1 } } : item)), state.activeId, id)
}

/** Öndeki pencereyi geri plana düşürür (çalışma alanına tıklanınca). */
export function blurWindows(state: TabState): TabState {
  return state.focusId === undefined ? state : { tabs: state.tabs, activeId: state.activeId }
}

/** Pencereyi simge durumuna küçültür. */
export function minimizeWindow(state: TabState, id: string): TabState {
  const tab = state.tabs.find((item) => item.id === id)
  if (!tab?.floating || tab.floating.minimized) {
    return state
  }

  const tabs = state.tabs.map((item) => (item === tab ? { ...tab, floating: { ...tab.floating!, minimized: true } } : item))
  return focused(tabs, state.activeId, state.focusId === id ? undefined : state.focusId)
}

/** Pencerenin yerini / boyutunu günceller. */
export function moveWindow(state: TabState, id: string, rect: WindowRect): TabState {
  return {
    ...state,
    tabs: state.tabs.map((tab) => (tab.id === id && tab.floating ? { ...tab, floating: { ...tab.floating, ...rect } } : tab)),
  }
}

/** Sekmeye ad verir; boş ad, ekranın adına geri döndürür. */
export function renameTab(state: TabState, id: string, name: string): TabState {
  const trimmed = name.trim().slice(0, TAB_NAME_MAX_LENGTH)
  return {
    ...state,
    tabs: state.tabs.map((tab) => {
      if (tab.id !== id || id === HOME_TAB.id) {
        return tab
      }

      const { name: _previous, ...rest } = tab
      return trimmed ? { ...rest, name: trimmed } : rest
    }),
  }
}

/** Özel sekmeye kısayol ekler; zaten olanlar ve sınırı aşanlar atlanır. Eklenen adresler `added` olarak döner. */
export function addTabShortcuts(state: TabState, id: string, paths: readonly string[]): { state: TabState; added: string[] } {
  const tab = state.tabs.find((item) => item.id === id && item.custom)
  const current = tab?.shortcuts ?? []
  const added = [...new Set(paths)]
    .filter((path) => path !== HOME_TAB.path && !current.includes(path))
    .slice(0, Math.max(TAB_SHORTCUTS_MAX - current.length, 0))
  if (!tab || added.length === 0) {
    return { state, added: [] }
  }

  return {
    state: { ...state, tabs: state.tabs.map((item) => (item === tab ? { ...tab, shortcuts: [...current, ...added] } : item)) },
    added,
  }
}

export function removeTabShortcut(state: TabState, id: string, path: string): TabState {
  return {
    ...state,
    tabs: state.tabs.map((tab) =>
      tab.id === id && tab.shortcuts?.includes(path) ? { ...tab, shortcuts: tab.shortcuts.filter((item) => item !== path) } : tab),
  }
}

function parseTabList(value: unknown): WorkTab[] {
  if (!Array.isArray(value)) {
    return []
  }

  const seen = new Set<string>([HOME_TAB.id])
  const tabs: WorkTab[] = []
  for (const item of value as Partial<WorkTab>[]) {
    if (typeof item !== 'object' || item === null || typeof item.id !== 'string' || typeof item.path !== 'string'
      || !item.path.startsWith('/') || seen.has(item.id)) {
      continue
    }

    seen.add(item.id)
    const rect = item.floating as Partial<FloatingWindow> | undefined
    const floats = typeof rect === 'object' && rect !== null
      && [rect.x, rect.y, rect.width, rect.height, rect.z].every((value) => typeof value === 'number' && Number.isFinite(value))
    const shortcuts = Array.isArray(item.shortcuts)
      ? [...new Set(item.shortcuts.filter((path): path is string => typeof path === 'string' && path.startsWith('/') && path !== '/'))]
        .slice(0, TAB_SHORTCUTS_MAX)
      : []
    tabs.push({
      id: item.id,
      // Özel sekme bir kısayol sayfasıdır; başka ekran göstermez.
      path: item.custom === true ? customTabPath(item.id) : item.path,
      ...(item.custom === true ? { custom: true } : {}),
      ...(item.custom === true && shortcuts.length > 0 ? { shortcuts } : {}),
      ...(typeof item.name === 'string' && item.name.trim() ? { name: item.name.trim().slice(0, TAB_NAME_MAX_LENGTH) } : {}),
      ...(floats
        ? {
            floating: {
              x: rect.x!, y: rect.y!, width: rect.width!, height: rect.height!, z: rect.z!,
              ...(rect.minimized === true ? { minimized: true } : {}),
            },
          }
        : {}),
    })
  }

  return tabs.slice(0, MAX_TABS - 1)
}

/**
 * Saklanan sekmeleri okur. Oturum kaydı (bu tarayıcı sekmesinde açık olanlar) varsa o geçerlidir; yoksa
 * kullanıcının kalıcı özel sekmeleriyle başlanır. Bozuk ya da başka kullanıcıya ait veri yok sayılır.
 */
export function parseTabState(session: string | null, custom: string | null, owner: string): TabState {
  const read = (text: string | null): unknown => {
    try {
      return JSON.parse(text ?? 'null')
    } catch {
      return null
    }
  }

  const stored = read(session) as { owner?: unknown; tabs?: unknown; activeId?: unknown } | null
  if (typeof stored === 'object' && stored !== null && !Array.isArray(stored) && stored.owner === owner) {
    const tabs = [HOME_TAB, ...parseTabList(stored.tabs)]
    const has = (id: unknown, floating: boolean) => tabs.some((tab) => tab.id === id && Boolean(tab.floating) === floating)
    return focused(
      tabs,
      has(stored.activeId, false) ? (stored.activeId as string) : HOME_TAB.id,
      has((stored as { focusId?: unknown }).focusId, true) ? ((stored as { focusId: string }).focusId) : undefined,
    )
  }

  return { tabs: [HOME_TAB, ...parseTabList(read(custom)).filter((tab) => tab.custom)], activeId: HOME_TAB.id }
}

/** Kullanıcının kısayol adreslerini görebildiği menü ekranlarına çevirir; tanınmayan / yetkisiz adres atlanır. */
export function resolveShortcuts(groups: NavGroup[], paths: readonly string[]) {
  const items = groups.flatMap((group) => group.items)
  return paths.flatMap((path) => {
    const item = items.find((candidate) => candidate.path === path)
    return item ? [item] : []
  })
}

/**
 * Menüden ekran açma: ekran şeritteki bir sekmede aynı adresle zaten açıksa o sekmeye geçilir; değilse yeni sekme
 * açılır (açık sekmenin içeriği ezilmez). Ana sayfa kendi sekmesinde açılır.
 */
export function openScreen(state: TabState, path: string, createId: () => string = newTabId): TabState {
  if (path === '/') {
    return focused(state.tabs, HOME_TAB.id, state.focusId)
  }

  const existing = state.tabs.find((tab) => isOrdinary(tab) && isDocked(tab) && tab.path === path)
  return existing ? focused(state.tabs, existing.id, state.focusId) : addTab(state, { id: createId(), path })
}

/**
 * Simge durumundaki pencereleri toplu kapatır: kapsam, alttaki pencere şeridindeki sıraya göredir (tıklanan
 * pencereye göre diğerleri / soldakiler / sağdakiler / tümü). Şeritteki sekmeler etkilenmez.
 */
export function closeWindows(state: TabState, scope: CloseScope, anchorId: string): TabState {
  const minimized = state.tabs.filter((tab) => tab.floating?.minimized)
  const anchor = minimized.findIndex((tab) => tab.id === anchorId)
  const closes = (tab: WorkTab) => {
    const index = minimized.indexOf(tab)
    if (index < 0) {
      return false
    }

    return scope === 'all' || (anchor >= 0 && (scope === 'others' ? index !== anchor : scope === 'left' ? index < anchor : index > anchor))
  }

  const tabs = state.tabs.filter((tab) => !closes(tab))
  return tabs.length === state.tabs.length
    ? state
    : focused(tabs, state.activeId, tabs.some((tab) => tab.id === state.focusId) ? state.focusId : undefined)
}
