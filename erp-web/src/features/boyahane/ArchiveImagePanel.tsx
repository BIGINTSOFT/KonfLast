import { ImagePlus, Trash2 } from 'lucide-react'
import { useState } from 'react'
import type { ChangeEvent } from 'react'
import { isRequestError } from '../../api/envelope'
import { ErrorAlert } from '../../components/FormControls'
import { archiveImageUri, prepareArchiveImage } from './boyahaneArchiveApi'
import type { BoyahaneArchiveImage, BoyahaneArchiveImageUpload } from './boyahaneArchiveApi'

interface ArchiveImagePanelProps {
  title: string
  /** Sunucudaki görüntü sorgusu; görüntü yoksa sunucu NOT_FOUND döner, bu boş durumdur. */
  image: { data?: BoyahaneArchiveImage; error?: unknown; isLoading: boolean }
  canManage: boolean
  onSave: (upload: BoyahaneArchiveImageUpload) => Promise<unknown>
  onRemove: () => Promise<unknown>
}

/** Renk numunesi ya da desen görüntüsü: gösterir, yükler (JPEG / PNG, sunucuda en fazla 2 MB), kaldırır. */
export function ArchiveImagePanel({ title, image, canManage, onSave, onRemove }: ArchiveImagePanelProps) {
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<unknown>(null)
  const [fileError, setFileError] = useState<string | null>(null)
  const isEmpty = isRequestError(image.error) && image.error.status === 404
  const shown = isEmpty ? undefined : image.data

  const run = async (action: () => Promise<unknown>) => {
    setBusy(true)
    setError(null)
    const result = await action()
    setBusy(false)
    if (result && typeof result === 'object' && 'error' in result) {
      setError(result.error)
    }
  }

  const handleFile = async (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0]
    event.target.value = ''
    if (!file) {
      return
    }

    let upload: BoyahaneArchiveImageUpload
    try {
      upload = await prepareArchiveImage(file)
      setFileError(null)
    } catch {
      setFileError('Dosya okunamadı. JPEG ya da PNG bir görüntü seçin.')
      return
    }

    await run(() => onSave(upload))
  }

  return (
    <div className="form__section">
      <h2 className="section-title">{title}</h2>
      {image.isLoading && <p role="status">Yükleniyor…</p>}
      {shown
        ? <img src={archiveImageUri(shown)} alt={title} style={{ display: 'block', maxWidth: '100%', maxHeight: 280, objectFit: 'contain', border: '1px solid var(--border, #d0d5dd)' }} />
        : !image.isLoading && <p className="muted">Görüntü yüklenmedi.</p>}
      {canManage && (
        <div className="toolbar">
          <label className="button">
            <ImagePlus aria-hidden="true" />
            {shown ? 'Görüntüyü değiştir' : 'Görüntü yükle'}
            <input type="file" accept="image/jpeg,image/png" className="visually-hidden" disabled={busy} onChange={(event) => void handleFile(event)} />
          </label>
          {shown && (
            <button type="button" className="button button--danger-ghost" disabled={busy} onClick={() => void run(onRemove)}>
              <Trash2 aria-hidden="true" />Görüntüyü kaldır
            </button>
          )}
        </div>
      )}
      {!isEmpty && <ErrorAlert error={image.error} />}
      <ErrorAlert error={error} />
      {fileError && <div className="alert alert--error" role="alert">{fileError}</div>}
    </div>
  )
}
