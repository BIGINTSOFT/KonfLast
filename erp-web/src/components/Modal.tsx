import { X } from 'lucide-react'
import { useEffect, useRef } from 'react'
import type { ReactNode } from 'react'
import { createPortal } from 'react-dom'

interface ModalProps {
  title: string
  onClose: () => void
  children: ReactNode
  /** `drawer`: ekranın sağından açılan, tam boy panel (uzun kayıt formları için). */
  variant?: 'dialog' | 'drawer'
}

/** Tarayıcının yerel <dialog> öğesi: odak tuzağı ve Esc ile kapanma hazır gelir. */
export function Modal({ title, onClose, children, variant = 'dialog' }: ModalProps) {
  const ref = useRef<HTMLDialogElement>(null)

  useEffect(() => {
    const dialog = ref.current
    if (dialog && !dialog.open) {
      dialog.showModal()
      // React'in autoFocus'u alanı pencere açılmadan odaklamaya çalışır ve boşa gider; tarayıcı da odağı ilk
      // düğmeye (Kapat) verir. Pencere açılınca ilk giriş alanı odaklanır (ör. kart okutulunca alana yazılsın).
      const field = dialog.querySelector<HTMLElement>(
        'input:not([type="hidden"]):not([type="checkbox"]):not([type="radio"]):not(:disabled), select:not(:disabled), textarea:not(:disabled)',
      )
      field?.focus()
    }
    return () => dialog?.close()
  }, [])

  // Belge köküne çizilir: tablo hücresi gibi bir yerden açılınca oranın stillerini (nowrap, hizalama) almaz.
  return createPortal(
    <dialog
      ref={ref}
      className={variant === 'drawer' ? 'modal modal--drawer' : 'modal'}
      aria-label={title}
      onCancel={(event) => {
        event.preventDefault()
        onClose()
      }}
    >
      <header className="modal__header">
        <h2>{title}</h2>
        <button type="button" className="button button--ghost button--icon" onClick={onClose} aria-label="Kapat">
          <X aria-hidden="true" />
        </button>
      </header>
      {/* Yalnız gövde kayar: başlık tam genişlikte sabit kalır, kaydırma çubuğu içeriği kaydırmaz. */}
      <div className="modal__body">{children}</div>
    </dialog>,
    document.body,
  )
}
