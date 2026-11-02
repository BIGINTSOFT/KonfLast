import { useId, useLayoutEffect, useRef, useState } from 'react'
import type { CSSProperties, KeyboardEvent } from 'react'
import { useDebounced } from '../../hooks/useDebounced'
import { useGetLookupQuery } from './masterDataApi'
import type { LookupItem } from './masterDataApi'

interface LookupSelectProps {
  resource: string
  label: string
  value: number | null
  onChange: (value: number | null) => void
  /** Seçilen kaydın kod ve adına da ihtiyaç duyan ekranlar için. */
  onSelectItem?: (item: LookupItem | null) => void
  required?: boolean
  disabled?: boolean
  errors?: string[]
  /** Aynı kaynağın seçimi ekranda birden fazla kez yer alıyorsa (satır satır) öğe kimliklerini ayırır. */
  idSuffix?: string
  /** Etiket ekranda gösterilmez (tablo hücresi gibi dar yerlerde); ekran okuyucular yine okur. */
  hideLabel?: boolean
  placeholder?: string
  /** Müşteriye göre süzen kaynaklarda (ürünler) yalnızca bu müşteriye ait / genel kayıtlar listelenir. */
  customerId?: number | null
}

const itemLabel = (item: LookupItem) => `${item.code} — ${item.name}${item.detail ? ` · ${item.detail}` : ''}`

const LIST_MAX_HEIGHT = 264

/**
 * Liste ekrana göre sabit konumlanır: pencere (modal) gövdesi ya da tablo kaydırma alanı onu kırpmaz. Altta yer
 * yoksa yukarı açılır.
 */
function listPosition(anchor: HTMLElement): CSSProperties {
  const rect = anchor.getBoundingClientRect()
  const below = window.innerHeight - rect.bottom - 8
  const above = rect.top - 8
  const upward = below < 160 && above > below
  const maxHeight = Math.min(LIST_MAX_HEIGHT, upward ? above : below)
  return {
    position: 'fixed',
    left: rect.left,
    width: rect.width,
    right: 'auto',
    maxHeight: Math.max(maxHeight, 96),
    ...(upward ? { top: 'auto', bottom: window.innerHeight - rect.top + 2 } : { top: rect.bottom + 2, bottom: 'auto' }),
  }
}

/**
 * Başka bir ana veri kaydını seçmek için aranabilir seçim kutusu. Kutuya yazılan metin sunucuda aranır
 * (liste sınırlı sayıda gelir); ok tuşları ve Enter ile seçilir, Esc ile kapanır. Seçili değer her zaman
 * listede kalır.
 */
export function LookupSelect({
  resource, label, value, onChange, onSelectItem, required, disabled, errors, idSuffix, hideLabel, placeholder, customerId,
}: LookupSelectProps) {
  const [open, setOpen] = useState(false)
  // Kullanıcı yazmaya başlayınca metin aramadır; yazmıyorsa kutuda seçili kaydın adı görünür.
  const [query, setQuery] = useState<string | null>(null)
  const [active, setActive] = useState(0)
  // Arama sonucu seçili kaydı içermese de kutuda adı görünsün diye son bilinen kayıt saklanır.
  const [selected, setSelected] = useState<LookupItem | null>(null)
  const listId = useId()
  const anchorRef = useRef<HTMLDivElement>(null)
  const [position, setPosition] = useState<CSSProperties | undefined>(undefined)
  const debouncedSearch = useDebounced((query ?? '').trim())
  const { data: items, isFetching } = useGetLookupQuery({
    resource,
    search: debouncedSearch || undefined,
    includeId: value ?? undefined,
    customerId: customerId ?? undefined,
  })

  const inputId = `lookup-${resource}${idSuffix ? `-${idSuffix}` : ''}`
  const found = items?.find((item) => item.id === value)
  if (found && found !== selected) {
    setSelected(found)
  } else if (value === null && selected !== null) {
    setSelected(null)
  }

  // Zorunlu olmayan alanda ilk seçenek değeri boşaltır.
  const options: (LookupItem | null)[] = [...(required ? [] : [null]), ...(items ?? [])]
  const current = Math.min(active, Math.max(options.length - 1, 0))

  const close = () => {
    setOpen(false)
    setQuery(null)
  }

  const choose = (item: LookupItem | null) => {
    setSelected(item)
    onChange(item?.id ?? null)
    onSelectItem?.(item)
    close()
  }

  const openList = () => {
    if (disabled) {
      return
    }
    setOpen(true)
    const index = options.findIndex((item) => (item?.id ?? null) === value)
    setActive(index < 0 ? 0 : index)
  }

  const handleKey = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
      event.preventDefault()
      if (!open) {
        openList()
        return
      }
      const step = event.key === 'ArrowDown' ? 1 : -1
      setActive((current + step + options.length) % Math.max(options.length, 1))
    } else if (event.key === 'Enter' && open) {
      event.preventDefault()
      if (options.length > 0) {
        choose(options[current])
      }
    } else if (event.key === 'Escape' && open) {
      event.preventDefault()
      close()
    }
  }

  // Açıkken liste, kutunun ekrandaki yerine göre konumlanır; kaydırma ve yeniden boyutlandırmada izler.
  useLayoutEffect(() => {
    if (!open) {
      return
    }
    const update = () => anchorRef.current && setPosition(listPosition(anchorRef.current))
    update()
    window.addEventListener('scroll', update, true)
    window.addEventListener('resize', update)
    return () => {
      window.removeEventListener('scroll', update, true)
      window.removeEventListener('resize', update)
    }
  }, [open])

  const optionId = (index: number) => `${listId}-${index}`
  const invalid = Boolean(errors?.length)

  return (
    <div className="field">
      <label htmlFor={inputId} className={hideLabel ? 'visually-hidden' : undefined}>{label}</label>
      <div className="combobox" ref={anchorRef}>
        <input
          id={inputId}
          type="text"
          role="combobox"
          aria-expanded={open}
          aria-controls={listId}
          aria-autocomplete="list"
          aria-activedescendant={open && options.length > 0 ? optionId(current) : undefined}
          aria-invalid={invalid}
          aria-busy={isFetching}
          autoComplete="off"
          placeholder={placeholder ?? (required ? 'Seçin…' : '— Yok —')}
          value={query ?? (selected ? itemLabel(selected) : '')}
          disabled={disabled}
          onFocus={(event) => {
            event.target.select()
            openList()
          }}
          onClick={() => !open && openList()}
          onBlur={close}
          onChange={(event) => {
            setQuery(event.target.value)
            setActive(0)
            setOpen(true)
          }}
          onKeyDown={handleKey}
        />
        {open && (
          <ul className="combobox__list" id={listId} role="listbox" aria-label={label} style={position}>
            {options.map((item, index) => (
              <li
                key={item?.id ?? 'none'}
                id={optionId(index)}
                role="option"
                aria-selected={(item?.id ?? null) === value}
                className={index === current ? 'combobox__option combobox__option--active' : 'combobox__option'}
                // Seçim, kutu odağı kaybetmeden önce yapılır.
                onMouseDown={(event) => {
                  event.preventDefault()
                  choose(item)
                }}
                onMouseEnter={() => setActive(index)}
              >
                {item ? (
                  <>
                    <span className="combobox__code">{item.code}</span>
                    <span>{item.name}</span>
                    {item.detail && <span className="combobox__detail">{item.detail}</span>}
                  </>
                ) : (
                  <span className="muted">— Yok —</span>
                )}
              </li>
            ))}
            {options.length === 0 && (
              <li className="combobox__empty" role="presentation">
                {isFetching ? 'Aranıyor…' : 'Eşleşen kayıt yok.'}
              </li>
            )}
          </ul>
        )}
      </div>
      {errors?.map((message) => (
        <small key={message} className="field__error">{message}</small>
      ))}
    </div>
  )
}
