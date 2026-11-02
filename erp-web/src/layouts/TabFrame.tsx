import { Minus, PanelTop, X } from 'lucide-react'
import type { LucideIcon } from 'lucide-react'
import { memo, useContext, useLayoutEffect, useMemo, useRef, useState } from 'react'
import type { PointerEvent as ReactPointerEvent, ReactNode } from 'react'
import { createPath, Routes, UNSAFE_NavigationContext as NavigationContext } from 'react-router-dom'
import type { To } from 'react-router-dom'
import type { WindowRect, WorkTab } from './tabs'
import { clampWindow, MIN_HEIGHT, MIN_WIDTH } from './windowGeometry'
import type { Point } from './windowGeometry'

/**
 * Bir sekmenin ekranı. Ekran sekmenin adresiyle çözülür ve sekme arka plandayken de açık kalır: filtreler,
 * sayfa ve yarım kalan formlar sekmeye dönülünce yerindedir. Adresi değişmedikçe yeniden çizilmez.
 */
const TabPane = memo(function TabPane({ path, routes }: { path: string; routes: ReactNode }) {
  return <Routes location={path}>{routes}</Routes>
})

/**
 * Ayrı pencerenin gezinmesi: penceredeki bağlantılar ve yönlendirmeler adres çubuğunu değil yalnızca pencerenin
 * adresini değiştirir (küçük bir geçmişle; "geri" pencerenin önceki ekranına döner). Şeritteki sekmede devre dışıdır.
 */
function WindowNavigation({
  detached, path, onNavigate, children,
}: {
  detached: boolean
  path: string
  onNavigate: (path: string) => void
  children: ReactNode
}) {
  const parent = useContext(NavigationContext)
  const history = useRef<string[]>([path])
  const navigate = useRef(onNavigate)
  useLayoutEffect(() => {
    navigate.current = onNavigate
  })

  const value = useMemo(() => {
    if (!detached) {
      return parent
    }

    const pathOf = (to: To) => (typeof to === 'string' ? to : createPath(to))
    return {
      ...parent,
      navigator: {
        ...parent.navigator,
        push: (to: To) => {
          history.current.push(pathOf(to))
          navigate.current(pathOf(to))
        },
        replace: (to: To) => {
          history.current[history.current.length - 1] = pathOf(to)
          navigate.current(pathOf(to))
        },
        go: (delta: number) => {
          if (delta < 0 && history.current.length > 1) {
            history.current.splice(Math.max(history.current.length + delta, 1))
            navigate.current(history.current[history.current.length - 1])
          }
        },
      },
    }
  }, [detached, parent])

  return <NavigationContext.Provider value={value}>{children}</NavigationContext.Provider>
}

interface TabFrameProps {
  tab: WorkTab
  /** Şeritte seçili sekme mi (ayrı pencerede anlamı yoktur). */
  active: boolean
  routes: ReactNode
  /**
   * Ekran yerine gösterilecek içerik (özel sekmenin kısayol sayfası). Bu içerikteki bağlantılar pencerede kalmaz:
   * ekran her zamanki gibi bir çalışma sekmesinde açılır.
   */
  children?: ReactNode
  title: string
  icon: LucideIcon
  /** Öndeki pencere mi. */
  focused: boolean
  zIndex: number
  onNavigate: (path: string) => void
  /** Pencere başlığından sürüklenirken imlecin yeri; sürükleme bitince null. */
  onDragOver: (point: Point | null) => void
  /** Sürükleme bitti: pencerenin yeni yeri ve bırakıldığı nokta (şeride bırakıldıysa sekmeye yerleşir). */
  onDrop: (rect: WindowRect, point: Point) => void
  onResize: (rect: WindowRect) => void
  onMinimize: () => void
  onDock: () => void
  onClose: () => void
}

/**
 * Bir sekmenin çerçevesi: şeritteyken çalışma alanını doldurur; koparılınca başlık çubuklu, taşınabilir ve
 * boyutlandırılabilir bir penceredir. İki durumda da aynı ekran örneği kullanılır: koparmak ya da geri
 * yerleştirmek ekrandaki yarım işi (filtre, form) sıfırlamaz.
 */
export function TabFrame({
  tab, active, routes, children, title, icon: Icon, focused, zIndex, onNavigate, onDragOver, onDrop, onResize, onMinimize, onDock, onClose,
}: TabFrameProps) {
  const floating = tab.floating
  // Sürükleme / boyutlandırma sırasında pencerenin geçici yeri; bırakılınca kalıcı duruma yazılır.
  const [draft, setDraft] = useState<WindowRect | null>(null)
  const rect = floating ? clampWindow(draft ?? floating) : null

  const startDrag = (event: ReactPointerEvent<HTMLElement>, mode: 'move' | 'resize') => {
    if (!rect || event.button !== 0 || (mode === 'move' && (event.target as Element).closest('button'))) {
      return
    }

    event.preventDefault()
    const handle = event.currentTarget
    const origin = { x: event.clientX, y: event.clientY }
    let latest = rect
    handle.setPointerCapture(event.pointerId)

    const move = (moved: PointerEvent) => {
      const dx = moved.clientX - origin.x
      const dy = moved.clientY - origin.y
      latest = clampWindow(mode === 'move'
        ? { ...rect, x: rect.x + dx, y: rect.y + dy }
        : { ...rect, width: Math.max(MIN_WIDTH, rect.width + dx), height: Math.max(MIN_HEIGHT, rect.height + dy) })
      setDraft(latest)
      if (mode === 'move') {
        onDragOver({ x: moved.clientX, y: moved.clientY })
      }
    }

    const end = (ended: PointerEvent) => {
      handle.removeEventListener('pointermove', move)
      handle.removeEventListener('pointerup', end)
      handle.removeEventListener('pointercancel', end)
      setDraft(null)
      if (mode === 'resize') {
        onResize(latest)
      } else {
        onDragOver(null)
        onDrop(latest, { x: ended.clientX, y: ended.clientY })
      }
    }

    handle.addEventListener('pointermove', move)
    handle.addEventListener('pointerup', end)
    handle.addEventListener('pointercancel', end)
  }

  return (
    <section
      className={rect ? `window${focused ? ' window--focused' : ''}${draft ? ' window--dragging' : ''}` : 'tabframe'}
      role={rect ? 'dialog' : 'tabpanel'}
      aria-label={title}
      data-window-id={rect ? tab.id : undefined}
      hidden={floating ? floating.minimized === true : !active}
      style={rect ? { left: rect.x, top: rect.y, width: rect.width, height: rect.height, zIndex } : undefined}
    >
      {rect && (
        <header key="bar" className="window__bar" onPointerDown={(event) => startDrag(event, 'move')} onDoubleClick={onDock}>
          <Icon aria-hidden="true" />
          <span className="window__title">{title}</span>
          <button type="button" className="window__button" aria-label="Simge durumuna küçült" title="Simge durumuna küçült" onClick={onMinimize}>
            <Minus aria-hidden="true" />
          </button>
          <button type="button" className="window__button" aria-label="Sekmeye yerleştir" title="Sekmeye yerleştir" onClick={onDock}>
            <PanelTop aria-hidden="true" />
          </button>
          <button type="button" className="window__button window__button--close" aria-label="Pencereyi kapat" title="Kapat" onClick={onClose}>
            <X aria-hidden="true" />
          </button>
        </header>
      )}
      <div key="body" className={rect ? 'window__body' : undefined}>
        {children ?? (
          <WindowNavigation detached={rect !== null} path={tab.path} onNavigate={onNavigate}>
            <TabPane path={tab.path} routes={routes} />
          </WindowNavigation>
        )}
      </div>
      {rect && <span key="grip" className="window__grip" aria-hidden="true" onPointerDown={(event) => startDrag(event, 'resize')} />}
    </section>
  )
}
