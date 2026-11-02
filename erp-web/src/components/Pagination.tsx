import { ChevronLeft, ChevronRight } from 'lucide-react'

interface PaginationProps {
  page: number
  totalPages: number
  totalCount: number
  onPageChange: (page: number) => void
}

export function Pagination({ page, totalPages, totalCount, onPageChange }: PaginationProps) {
  return (
    <nav className="pagination" aria-label="Sayfalama">
      <span className="muted">{totalCount} kayıt</span>
      <button type="button" className="button" disabled={page <= 1} onClick={() => onPageChange(page - 1)}>
        <ChevronLeft aria-hidden="true" />
        Önceki
      </button>
      <span>
        Sayfa {totalPages === 0 ? 0 : page} / {totalPages}
      </span>
      <button type="button" className="button" disabled={page >= totalPages} onClick={() => onPageChange(page + 1)}>
        Sonraki
        <ChevronRight aria-hidden="true" />
      </button>
    </nav>
  )
}
