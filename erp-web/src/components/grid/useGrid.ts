import { useCallback, useMemo, useState } from 'react'
import { useDebounced } from '../../hooks/useDebounced'

/** Liste uçlarının sütun filtresi ve sıralama parametreleri (sunucuda GridQuery). */
export type GridParams = { sort?: string } & { [key: `filter[${string}]`]: string }

export interface Grid {
  /** Sıralanan alan; azalan sırada başında "-" vardır. */
  sort: string | undefined
  /** Filtre kutularındaki değerler (yazarken anında). */
  filters: Record<string, string>
  /** Sunucuya gönderilecek parametreler (filtreler kısa bir beklemeyle). */
  params: GridParams
  page: number
  setPage: (page: number) => void
  /** Alanın sıralaması: artan → azalan → sırasız. */
  toggleSort: (field: string) => void
  setFilter: (field: string, value: string) => void
  clearFilters: () => void
  hasFilters: boolean
}

/**
 * Tablonun filtre, sıralama ve sayfa durumu. Filtre ya da sıralama değişince liste ilk sayfaya döner.
 * `initialSort` verilmezse sunucunun varsayılan sırası kullanılır; `initialFilters` ekran bir bağlantıdan
 * süzülü açılıyorsa (ör. ana sayfadaki yapılacaklar) ilk istekten itibaren uygulanır.
 */
export function useGrid(initialSort?: string, initialFilters?: Record<string, string>): Grid {
  const [sort, setSort] = useState(initialSort)
  const [filters, setFilters] = useState<Record<string, string>>(() => initialFilters ?? {})
  const debouncedFilters = useDebounced(filters)

  const params = useMemo(() => {
    const result: GridParams = {}
    if (sort) {
      result.sort = sort
    }
    for (const [field, value] of Object.entries(debouncedFilters)) {
      if (value.trim() !== '') {
        result[`filter[${field}]`] = value.trim()
      }
    }
    return result
  }, [sort, debouncedFilters])

  // Sayfa, hangi filtre / sıralama için seçildiğiyle birlikte saklanır; koşullar değişince 1'e döner.
  const key = JSON.stringify(params)
  const [pageState, setPageState] = useState({ key, page: 1 })
  const page = pageState.key === key ? pageState.page : 1
  const setPage = useCallback((next: number) => setPageState({ key, page: next }), [key])

  const toggleSort = useCallback((field: string) => {
    setSort((current) => {
      const next = current === field ? `-${field}` : current === `-${field}` ? initialSort : field
      // Varsayılan sıra bu alanın azalan sırasıysa döngü artan sıraya geçer.
      return next === current ? field : next
    })
  }, [initialSort])

  const setFilter = useCallback((field: string, value: string) => {
    setFilters((current) => ({ ...current, [field]: value }))
  }, [])

  const clearFilters = useCallback(() => setFilters({}), [])

  return {
    sort,
    filters,
    params,
    page,
    setPage,
    toggleSort,
    setFilter,
    clearFilters,
    hasFilters: Object.values(filters).some((value) => value.trim() !== ''),
  }
}
