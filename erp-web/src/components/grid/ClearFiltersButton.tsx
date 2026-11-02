import { FilterX } from 'lucide-react'
import type { Grid } from './useGrid'

/** Tabloda sütun filtresi varsa gösterilir ve hepsini temizler. */
export function ClearFiltersButton({ grid }: { grid: Grid }) {
  if (!grid.hasFilters) {
    return null
  }
  return (
    <button type="button" className="button button--ghost" onClick={grid.clearFilters}>
      <FilterX aria-hidden="true" />
      Filtreleri temizle
    </button>
  )
}
