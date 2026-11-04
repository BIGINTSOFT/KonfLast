import { useState } from 'react'
import { Modal } from './Modal'

interface ClippedTextProps {
  text: string | null | undefined
  /** Tıklanınca açılan pencerenin başlığı (ör. "Kontrol notu"). */
  title: string
}

/**
 * Tablo hücresindeki uzun metin: tek satırda kısaltılır ve tabloyu genişletmez. Fareyle üzerine gelince tamamı
 * ipucu olarak görünür; tıklanınca pencerede okunur. Kısa metin olduğu gibi gösterilir.
 */
export function ClippedText({ text, title }: ClippedTextProps) {
  const [open, setOpen] = useState(false)

  if (!text) {
    return <>—</>
  }

  return (
    <>
      <button type="button" className="clipped-text" title={text} onClick={() => setOpen(true)}>
        {text}
      </button>
      {open && (
        <Modal title={title} onClose={() => setOpen(false)}>
          <p className="clipped-text__full">{text}</p>
          <footer className="form__actions">
            <button type="button" className="button" onClick={() => setOpen(false)}>Kapat</button>
          </footer>
        </Modal>
      )}
    </>
  )
}
