import { Search } from 'lucide-react'
import { useMemo, useState } from 'react'

export interface CheckListItem {
  id: number
  code: string
  name: string
}

interface SearchableCheckListProps {
  /** Ekran okuyucu ve sayaç için listenin adı (ör. "Operasyon"). */
  label: string
  items: CheckListItem[]
  selected: number[]
  onChange: (ids: number[]) => void
  disabled?: boolean
}

const normalize = (text: string) => text.toLocaleLowerCase('tr-TR')

/**
 * Uzun seçim listesi: kod / ada göre arama, yalnız seçilenleri gösterme ve görünenleri topluca seçme / kaldırma.
 * Liste kendi içinde kayar; form aşağı doğru uzamaz.
 */
export function SearchableCheckList({ label, items, selected, onChange, disabled }: SearchableCheckListProps) {
  const [search, setSearch] = useState('')
  const [onlySelected, setOnlySelected] = useState(false)

  const visible = useMemo(() => {
    const term = normalize(search.trim())
    return items.filter(
      (item) =>
        (!onlySelected || selected.includes(item.id))
        && (term === '' || normalize(`${item.code} ${item.name}`).includes(term)),
    )
  }, [items, search, onlySelected, selected])

  const visibleIds = visible.map((item) => item.id)
  const allVisibleSelected = visible.length > 0 && visibleIds.every((id) => selected.includes(id))

  const toggle = (id: number) => onChange(selected.includes(id) ? selected.filter((x) => x !== id) : [...selected, id])
  const selectVisible = () => onChange([...new Set([...selected, ...visibleIds])])
  const clearVisible = () => onChange(selected.filter((id) => !visibleIds.includes(id)))

  return (
    <div className="check-list">
      <div className="check-list__toolbar">
        <label className="check-list__search">
          <Search aria-hidden="true" />
          <input
            type="search"
            placeholder="Kod veya ad ile ara"
            aria-label={`${label} ara`}
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            autoComplete="off"
          />
        </label>
        <label className="checkbox">
          <input type="checkbox" checked={onlySelected} onChange={(event) => setOnlySelected(event.target.checked)} />
          Yalnız seçililer <span className="muted">({selected.length} / {items.length})</span>
        </label>
        <button
          type="button"
          className="button check-list__bulk"
          disabled={disabled || visible.length === 0}
          onClick={allVisibleSelected ? clearVisible : selectVisible}
          title="Yalnızca aramayla görünen kayıtlara uygulanır."
        >
          {allVisibleSelected ? 'Tümünü kaldır' : 'Tümünü seç'}
        </button>
      </div>

      <div className="check-list__items" role="group" aria-label={label}>
        {visible.map((item) => (
          <label key={item.id} className="checkbox check-list__item" title={`${item.code} — ${item.name}`}>
            <input type="checkbox" disabled={disabled} checked={selected.includes(item.id)} onChange={() => toggle(item.id)} />
            <code>{item.code}</code>
            <span className="check-list__name">{item.name}</span>
          </label>
        ))}
        {visible.length === 0 && (
          <p className="muted check-list__empty">{search || onlySelected ? 'Aramaya uyan kayıt yok.' : 'Kayıt yok.'}</p>
        )}
      </div>
    </div>
  )
}
