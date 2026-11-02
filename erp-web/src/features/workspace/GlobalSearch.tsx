import { Search } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import type { KeyboardEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { useDebounced } from '../../hooks/useDebounced'
import { searchNav } from '../../layouts/navigation'
import type { NavGroup } from '../../layouts/navigation'
import { useActiveProject } from '../system/activeProject'
import { useSearchRecordsQuery } from './workspaceApi'

const MIN_LENGTH = 2
const MAX_SCREENS = 6

interface Result {
  key: string
  type: string
  title: string
  subtitle: string | null
  path: string
}

/**
 * Üst çubuktaki genel arama: ekran adları ve kayıtlar (sipariş no, demet no, kart no, personel, ürün …)
 * tek kutudan aranır. Ctrl+K ile odaklanır; ok tuşlarıyla gezilir, Enter ile açılır.
 */
export function GlobalSearch({ groups }: { groups: NavGroup[] }) {
  const navigate = useNavigate()
  const input = useRef<HTMLInputElement>(null)
  const container = useRef<HTMLDivElement>(null)
  const [query, setQuery] = useState('')
  const [open, setOpen] = useState(false)
  const [active, setActive] = useState(0)
  const term = useDebounced(query.trim())
  const searching = term.length >= MIN_LENGTH
  // Kayıt araması Konfeksiyon kayıtlarında yapılır; başka projede yalnızca ekran adları aranır.
  const konfeksiyon = useActiveProject().project === 'Konfeksiyon'
  const records = useSearchRecordsQuery(term, { skip: !searching || !konfeksiyon })

  useEffect(() => {
    const onKey = (event: globalThis.KeyboardEvent) => {
      if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') {
        event.preventDefault()
        input.current?.focus()
        input.current?.select()
      }
    }
    const onPointer = (event: PointerEvent) => {
      if (!container.current?.contains(event.target as Node)) {
        setOpen(false)
      }
    }

    document.addEventListener('keydown', onKey)
    document.addEventListener('pointerdown', onPointer)
    return () => {
      document.removeEventListener('keydown', onKey)
      document.removeEventListener('pointerdown', onPointer)
    }
  }, [])

  const screens: Result[] = query.trim() === ''
    ? []
    : searchNav(groups, query)
        .flatMap((group) => group.items.map((item) => ({
          key: `screen:${item.path}`, type: 'Ekran', title: item.label, subtitle: group.title ?? null, path: item.path,
        })))
        .slice(0, MAX_SCREENS)
  const hits: Result[] = searching
    ? (records.data ?? []).map((hit) => ({ key: `record:${hit.path}`, ...hit }))
    : []
  const results = [...screens, ...hits]
  const current = Math.min(active, Math.max(results.length - 1, 0))

  const go = (result: Result) => {
    navigate(result.path)
    setQuery('')
    setOpen(false)
    input.current?.blur()
  }

  const handleKey = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
      event.preventDefault()
      if (results.length > 0) {
        setActive((current + (event.key === 'ArrowDown' ? 1 : results.length - 1)) % results.length)
      }
    } else if (event.key === 'Enter' && results[current]) {
      event.preventDefault()
      go(results[current])
    } else if (event.key === 'Escape') {
      setOpen(false)
      input.current?.blur()
    }
  }

  const showPanel = open && query.trim() !== ''

  return (
    <div className="global-search" ref={container}>
      <Search className="global-search__icon" aria-hidden="true" />
      <input
        ref={input}
        type="search"
        placeholder="Ara: sipariş, demet, kart, personel, ekran…"
        aria-label="Genel arama"
        role="combobox"
        aria-expanded={showPanel}
        aria-controls="global-search-results"
        value={query}
        onChange={(event) => {
          setQuery(event.target.value)
          setActive(0)
          setOpen(true)
        }}
        onFocus={() => setOpen(true)}
        onKeyDown={handleKey}
        autoComplete="off"
      />
      {query === '' && <kbd aria-hidden="true">Ctrl K</kbd>}

      {showPanel && (
        <div className="global-search__panel" id="global-search-results" role="listbox">
          {results.map((result, index) => (
            <button
              key={result.key}
              type="button"
              role="option"
              aria-selected={index === current}
              className={`global-search__item${index === current ? ' global-search__item--active' : ''}`}
              onPointerEnter={() => setActive(index)}
              onClick={() => go(result)}
            >
              <span className="global-search__type">{result.type}</span>
              <span className="global-search__title">{result.title}</span>
              {result.subtitle && <span className="muted">{result.subtitle}</span>}
            </button>
          ))}
          {results.length === 0 && (
            <p className="global-search__empty" role="status">
              {records.isFetching || term !== query.trim()
                ? 'Aranıyor…'
                : searching ? 'Eşleşen kayıt ya da ekran yok.' : 'Kayıt aramak için en az 2 karakter yazın.'}
            </p>
          )}
        </div>
      )}
    </div>
  )
}
