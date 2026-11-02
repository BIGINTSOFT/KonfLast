import { ChevronRight } from 'lucide-react'
import type { LucideIcon } from 'lucide-react'
import { useEffect, useLayoutEffect, useRef, useState } from 'react'
import type { KeyboardEvent } from 'react'
import { createPortal } from 'react-dom'

export interface ContextMenuItem {
  key: string
  label: string
  icon?: LucideIcon
  /** Alt menüsü olan öğede kullanılmaz. */
  onSelect?: () => void
  disabled?: boolean
  /** Öğenin üstüne ayırıcı çizgi koyar. */
  separated?: boolean
  /** Üzerine gelince yana açılan alt menü (tek düzey). */
  children?: ContextMenuItem[]
}

interface ContextMenuProps {
  /** Menünün açılacağı ekran konumu (sağ tıklanan nokta). */
  x: number
  y: number
  label: string
  items: ContextMenuItem[]
  onClose: () => void
}

const EDGE = 4
const itemClass = (item: ContextMenuItem) => (item.separated ? 'context-menu__item context-menu__item--separated' : 'context-menu__item')

/** Menünün (alt menü hariç) kendi öğeleri. */
const ownButtons = (menu: Element | null | undefined) =>
  [...(menu?.querySelectorAll<HTMLButtonElement>(':scope > button:not(:disabled), :scope > .context-menu__parent > button:not(:disabled)') ?? [])]

/**
 * Sağ tık menüsü. Tıklanan noktada açılır, ekrandan taşacaksa içeri kaydırılır; dışarı tıklanınca, Esc ile,
 * sayfa kayınca ya da pencere odağını yitirince kapanır. Ok tuşlarıyla gezilir; alt menü üzerine gelince ya da
 * sağ okla açılır.
 */
export function ContextMenu({ x, y, label, items, onClose }: ContextMenuProps) {
  const ref = useRef<HTMLDivElement>(null)
  const [position, setPosition] = useState({ left: x, top: y })
  const [openKey, setOpenKey] = useState<string | null>(null)

  useLayoutEffect(() => {
    const menu = ref.current
    if (!menu) {
      return
    }

    const { width, height } = menu.getBoundingClientRect()
    setPosition({
      left: Math.max(EDGE, Math.min(x, window.innerWidth - width - EDGE)),
      top: Math.max(EDGE, Math.min(y, window.innerHeight - height - EDGE)),
    })
    ownButtons(menu)[0]?.focus({ preventScroll: true })
  }, [x, y])

  useEffect(() => {
    const closeOutside = (event: Event) => {
      if (!ref.current?.contains(event.target as Node)) {
        onClose()
      }
    }

    document.addEventListener('pointerdown', closeOutside)
    document.addEventListener('scroll', onClose, true)
    window.addEventListener('resize', onClose)
    window.addEventListener('blur', onClose)
    return () => {
      document.removeEventListener('pointerdown', closeOutside)
      document.removeEventListener('scroll', onClose, true)
      window.removeEventListener('resize', onClose)
      window.removeEventListener('blur', onClose)
    }
  }, [onClose])

  const handleKey = (event: KeyboardEvent<HTMLDivElement>) => {
    const focusedButton = document.activeElement as HTMLButtonElement | null
    const menu = focusedButton?.closest('[role="menu"]')
    const inSubmenu = menu !== ref.current
    if (event.key === 'Escape' || event.key === 'Tab') {
      event.preventDefault()
      onClose()
    } else if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
      event.preventDefault()
      const buttons = ownButtons(menu)
      const index = focusedButton ? buttons.indexOf(focusedButton) : -1
      const step = event.key === 'ArrowDown' ? 1 : -1
      buttons[(index + step + buttons.length) % buttons.length]?.focus()
    } else if (event.key === 'ArrowRight' && focusedButton?.dataset.submenu) {
      event.preventDefault()
      setOpenKey(focusedButton.dataset.submenu)
    } else if (event.key === 'ArrowLeft' && inSubmenu) {
      event.preventDefault()
      menu?.parentElement?.querySelector<HTMLButtonElement>(':scope > button')?.focus()
      setOpenKey(null)
    }
  }

  const select = (item: ContextMenuItem) => {
    onClose()
    item.onSelect?.()
  }

  const content = (item: ContextMenuItem) => (
    <>
      <span className="context-menu__icon">{item.icon && <item.icon aria-hidden="true" />}</span>
      {item.label}
    </>
  )

  return createPortal(
    <div
      ref={ref}
      className="context-menu"
      role="menu"
      aria-label={label}
      style={position}
      onKeyDown={handleKey}
      onContextMenu={(event) => event.preventDefault()}
    >
      {items.map((item) =>
        item.children ? (
          <div
            key={item.key}
            className="context-menu__parent"
            onPointerEnter={() => !item.disabled && setOpenKey(item.key)}
            onPointerLeave={() => setOpenKey(null)}
          >
            <button
              type="button"
              role="menuitem"
              aria-haspopup="menu"
              aria-expanded={openKey === item.key}
              data-submenu={item.key}
              className={itemClass(item)}
              disabled={item.disabled}
              onClick={() => setOpenKey(item.key)}
            >
              {content(item)}
              <ChevronRight className="context-menu__chevron" aria-hidden="true" />
            </button>
            {openKey === item.key && (
              <div
                className="context-menu context-menu--sub"
                role="menu"
                aria-label={item.label}
                // Sağda / altta yer yoksa alt menü sola açılır ve yukarı kaydırılır; klavyeyle açıldıysa ilk öğe odaklanır.
                ref={(submenu) => {
                  if (!submenu) {
                    return
                  }

                  const bounds = submenu.getBoundingClientRect()
                  if (bounds.right > window.innerWidth - EDGE) {
                    submenu.classList.add('context-menu--sub-left')
                  }
                  if (bounds.bottom > window.innerHeight - EDGE) {
                    submenu.style.top = `${window.innerHeight - EDGE - bounds.bottom}px`
                  }
                  if (document.activeElement === submenu.previousElementSibling && submenu.matches(':not(:hover)')) {
                    ownButtons(submenu)[0]?.focus({ preventScroll: true })
                  }
                }}
              >
                {item.children.map((child) => (
                  <button
                    key={child.key}
                    type="button"
                    role="menuitem"
                    className={itemClass(child)}
                    disabled={child.disabled}
                    onClick={() => select(child)}
                  >
                    {content(child)}
                  </button>
                ))}
              </div>
            )}
          </div>
        ) : (
          <button
            key={item.key}
            type="button"
            role="menuitem"
            className={itemClass(item)}
            disabled={item.disabled}
            onClick={() => select(item)}
          >
            {content(item)}
          </button>
        ),
      )}
    </div>,
    document.body,
  )
}
