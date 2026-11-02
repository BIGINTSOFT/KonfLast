import type { ReactNode } from 'react'
import type { Grid } from './useGrid'

export interface GridOption {
  value: string
  label: string
}

export interface GridColumn {
  /** Liste satırındaki (DTO) alan adı; sıralama ve filtre bununla sunucuya gider. Yoksa sütun yalnızca başlıktır. */
  key?: string
  label: ReactNode
  /** Sayısal sütun: sağa yaslanır, filtresi tam sayı / ondalık eşitliğidir. */
  numeric?: boolean
  /** Varsayılan: alan adı varsa sıralanabilir. */
  sortable?: boolean
  /**
   * Filtre türü. Varsayılan: alan adı varsa metin (sayısal sütunda sayı) kutusu. Seçenek listesi verilirse açılır
   * liste olur; `false` filtreyi kapatır (ör. sunucunun süzemediği zaman damgaları).
   */
  filter?: 'text' | 'number' | 'date' | GridOption[] | false
  className?: string
  /** Başlığı görünmeyen sütunlar (ör. işlem düğmeleri) için ekran okuyucu adı. */
  ariaLabel?: string
}

/** Liste tablosunun başlığı: sıralanabilir sütun başlıkları ve altında sütun filtreleri. */
export function GridHead({ columns, grid }: { columns: GridColumn[]; grid: Grid }) {
  const hasFilters = columns.some((column) => column.key && column.filter !== false)

  return (
    <thead>
      <tr>
        {columns.map((column, index) => {
          const className = [column.numeric ? 'table__number' : '', column.className ?? ''].join(' ').trim() || undefined
          const sortable = column.key !== undefined && column.sortable !== false
          if (!sortable) {
            return (
              <th key={column.key ?? index} className={className} aria-label={column.ariaLabel}>
                {column.label}
              </th>
            )
          }
          const direction = grid.sort === column.key ? 'ascending' : grid.sort === `-${column.key}` ? 'descending' : undefined
          return (
            <th key={column.key} className={className} aria-sort={direction ?? 'none'}>
              <button
                type="button"
                className={direction ? `table__sort table__sort--${direction}` : 'table__sort'}
                onClick={() => grid.toggleSort(column.key!)}
                title="Sıralamak için tıklayın"
              >
                {column.label}
              </button>
            </th>
          )
        })}
      </tr>
      {hasFilters && (
        <tr className="table__filters">
          {columns.map((column, index) => (
            <td key={column.key ?? index} className={column.numeric ? 'table__number' : undefined}>
              {column.key && column.filter !== false && <FilterControl column={column} grid={grid} />}
            </td>
          ))}
        </tr>
      )}
    </thead>
  )
}

function FilterControl({ column, grid }: { column: GridColumn; grid: Grid }) {
  const key = column.key!
  const value = grid.filters[key] ?? ''
  const label = `${typeof column.label === 'string' ? column.label : key} süzgeci`
  const onChange = (next: string) => grid.setFilter(key, next)

  if (Array.isArray(column.filter)) {
    return (
      <select aria-label={label} value={value} onChange={(event) => onChange(event.target.value)}>
        <option value="">Tümü</option>
        {column.filter.map((option) => (
          <option key={option.value} value={option.value}>{option.label}</option>
        ))}
      </select>
    )
  }

  const type = column.filter || (column.numeric ? 'number' : 'text')
  return (
    <input
      type={type === 'text' ? 'search' : type}
      inputMode={type === 'number' ? 'decimal' : undefined}
      aria-label={label}
      value={value}
      onChange={(event) => onChange(event.target.value)}
      autoComplete="off"
    />
  )
}
