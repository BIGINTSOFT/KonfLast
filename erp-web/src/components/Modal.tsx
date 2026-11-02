import { X } from 'lucide-react'
import { useEffect, useRef } from 'react'
import type { ReactNode } from 'react'

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
    }
    return () => dialog?.close()
  }, [])

  return (
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
      {children}
    </dialog>
  )
}
