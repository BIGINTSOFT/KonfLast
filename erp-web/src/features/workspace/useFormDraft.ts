import { useCallback, useEffect, useRef, useState } from 'react'
import { useAppSelector } from '../../store/hooks'
import { useDeleteFormDraftMutation, useGetFormDraftQuery, useSaveFormDraftMutation } from './draftsApi'

export interface StoredDraft<T> {
  state: T
  title: string | null
  updatedAt: string
}

const AUTOSAVE_DELAY_MS = 1500

function readLocal<T>(storageKey: string): StoredDraft<T> | null {
  try {
    const text = localStorage.getItem(storageKey)
    return text ? (JSON.parse(text) as StoredDraft<T>) : null
  } catch {
    return null
  }
}

function writeLocal(storageKey: string, draft: StoredDraft<unknown> | null) {
  try {
    if (draft) {
      localStorage.setItem(storageKey, JSON.stringify(draft))
    } else {
      localStorage.removeItem(storageKey)
    }
  } catch {
    // Tarayıcı deposu kapalıysa yalnızca sunucudaki taslak kullanılır.
  }
}

/**
 * Formun kaydedilmemiş hâlini kullanıcıya özel saklar: sunucuda (oturuma bağlı) ve bağlantı koparsa diye
 * tarayıcıda. Form açılınca bulunan taslak `pending` olarak döner; kullanıcı devam etmeye ya da silmeye karar
 * verene kadar otomatik kayıt yapılmaz (eski taslağın üzerine yazılmasın diye).
 *
 * @param enabled Otomatik kaydın başlayıp başlamayacağı (ör. müşteri seçildikten sonra).
 */
export function useFormDraft<T>(formKey: string, state: T, enabled: boolean, title: string | null) {
  const userName = useAppSelector((root) => root.auth.user?.userName ?? 'anonim')
  const storageKey = `erp.draft.${userName}.${formKey}`
  const server = useGetFormDraftQuery(formKey, { refetchOnMountOrArgChange: true })
  const [saveDraft] = useSaveFormDraftMutation()
  const [deleteDraft] = useDeleteFormDraftMutation()
  // undefined: açılıştaki taslak henüz aranıyor; null: taslak yok ya da kullanıcı karar verdi.
  const [pending, setPending] = useState<StoredDraft<T> | null | undefined>(undefined)
  const [savedAt, setSavedAt] = useState<string | null>(null)
  const [offline, setOffline] = useState(false)
  const lastSaved = useRef<string | null>(null)

  // Açılışta bir kez: sunucudaki ve tarayıcıdaki taslaktan yeni olanı. Sonraki otomatik kayıtlar öneri olmaz.
  if (pending === undefined && !server.isFetching) {
    const local = readLocal<T>(storageKey)
    const remote = server.data
      ? { state: JSON.parse(server.data.payload) as T, title: server.data.title, updatedAt: server.data.updatedAt }
      : null
    setPending([local, remote]
      .filter((draft): draft is StoredDraft<T> => draft !== null)
      .sort((a, b) => b.updatedAt.localeCompare(a.updatedAt))[0] ?? null)
  }

  // Taslak yoksa ya da karar verildiyse otomatik kayıt başlar.
  const ready = pending === null

  const serialized = JSON.stringify(state)
  useEffect(() => {
    if (!ready || !enabled || serialized === lastSaved.current) {
      return
    }
    const timer = setTimeout(() => {
      const updatedAt = new Date().toISOString()
      writeLocal(storageKey, { state: JSON.parse(serialized), title, updatedAt })
      lastSaved.current = serialized
      saveDraft({ formKey, payload: serialized, title })
        .unwrap()
        .then(() => {
          setSavedAt(updatedAt)
          setOffline(false)
        })
        .catch(() => setOffline(true))
    }, AUTOSAVE_DELAY_MS)
    return () => clearTimeout(timer)
  }, [ready, enabled, serialized, storageKey, formKey, title, saveDraft])

  /** Taslağı siler (form kaydedildi ya da kullanıcı vazgeçti). */
  const clear = useCallback(async () => {
    writeLocal(storageKey, null)
    lastSaved.current = null
    await deleteDraft(formKey).unwrap().catch(() => undefined)
  }, [storageKey, formKey, deleteDraft])

  return {
    /** Açılışta bulunan, kullanıcının kararını bekleyen taslak. */
    pending: pending ?? null,
    /** Bulunan taslakla devam edildi ya da reddedildi; otomatik kayıt başlar. */
    resolve: (keep: boolean) => {
      setPending(null)
      if (!keep) {
        void clear()
      }
    },
    clear,
    savedAt,
    offline,
  }
}
