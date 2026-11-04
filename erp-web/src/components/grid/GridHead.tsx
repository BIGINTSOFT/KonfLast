import { useLayoutEffect, useRef } from 'react'
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

/**
 * Sütun genişliklerini tablo ilk kez veriyle dolunca sabitler. Tarayıcı genişlikleri o sayfadaki veriye göre
 * hesaplar; sabitlenmezse sayfa, filtre ya da sıralama değişince sütunlar kayar. Sabitlendikten sonra yalnızca
 * veri değişir. Sütunlar değişirse genişlikler yeniden ölçülür.
 */
function useStableColumnWidths(columns: GridColumn[]) {
  const theadRef = useRef<HTMLTableSectionElement>(null)
  const lockedFor = useRef<string | null>(null)
  const signature = columns.map((column, index) => column.key ?? `#${index}`).join('|')

  useLayoutEffect(() => {
    const thead = theadRef.current
    const table = thead?.closest('table')
    if (!thead || !table || lockedFor.current === signature) {
      return
    }

    const headers = Array.from(thead.rows[0]?.cells ?? [])
    if (lockedFor.current !== null) {
      table.style.tableLayout = ''
      table.style.width = ''
      headers.forEach((header) => (header.style.width = ''))
      lockedFor.current = null
    }

    // Boş tablo ("Kayıt bulunamadı") ya da görünmeyen tablo ölçülmez; veri gelince sabitlenir.
    const hasRows = Array.from(table.tBodies[0]?.rows ?? []).some((row) => !row.querySelector('.table__empty'))
    const tableWidth = table.getBoundingClientRect().width
    const available = table.parentElement?.clientWidth ?? tableWidth
    if (!hasRows || tableWidth === 0) {
      return
    }

    const widths = headers.map((header) => header.getBoundingClientRect().width)
    if (tableWidth > available + 1) {
      // Kaydırmalı geniş tablo: piksel olarak sabitlenir.
      table.style.width = `${tableWidth}px`
      headers.forEach((header, index) => (header.style.width = `${widths[index]}px`))
    } else {
      // Sığan tablo: oranlar sabitlenir, pencere boyu değişince tablo yine genişliğe uyar.
      headers.forEach((header, index) => (header.style.width = `${(widths[index] / tableWidth) * 100}%`))
    }
    table.style.tableLayout = 'fixed'
    lockedFor.current = signature
  })

  return theadRef
}

/** Liste tablosunun başlığı: sıralanabilir sütun başlıkları ve altında sütun filtreleri. */
export function GridHead({ columns, grid }: { columns: GridColumn[]; grid: Grid }) {
  const hasFilters = columns.some((column) => column.key && column.filter !== false)
  const theadRef = useStableColumnWidths(columns)

  return (
    <thead ref={theadRef}>
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
