import { Plus, Search } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { ErrorAlert } from '../../components/FormControls'
import { GridHead } from '../../components/grid/GridHead'
import type { GridColumn } from '../../components/grid/GridHead'
import { useGrid } from '../../components/grid/useGrid'
import { Pagination } from '../../components/Pagination'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDate } from '../../utils/format'
import { PriceListDialog } from './PriceListDialog'
import { useGetPriceListsQuery } from './pricingApi'
import { ClearFiltersButton } from '../../components/grid/ClearFiltersButton'

const PAGE_SIZE = 25

/**
 * Satış fiyat listeleri. Siparişte fiyat önerilirken müşteriye özel liste genel listeden önce gelir; ikisi de
 * yoksa ürünün liste fiyatı kullanılır.
 */
export function PriceListsPage() {
  const canManage = usePermission(Permissions.PricingManage)
  const grid = useGrid()
  const { page, setPage } = grid
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [currentOnly, setCurrentOnly] = useState(false)
  // undefined: kapalı, null: yeni liste, sayı: açılan liste.
  const [open, setOpen] = useState<number | null | undefined>(undefined)

  const { data, error, isLoading, isFetching } = useGetPriceListsQuery({
    page,
    pageSize: PAGE_SIZE,
    search: search || undefined,
    currentOnly: currentOnly || undefined,
    ...grid.params,
  })

  const columns: GridColumn[] = [
    { key: 'code', label: 'Kod' },
    { key: 'name', label: 'Ad' },
    { key: 'customerName', label: 'Müşteri' },
    { key: 'currency', label: 'Para birimi' },
    { key: 'validFrom', label: 'Başlangıç', filter: 'date' },
    { key: 'validTo', label: 'Bitiş', filter: 'date' },
    { key: 'itemCount', label: 'Fiyat', numeric: true, filter: false },
    { key: 'isActive', label: 'Durum', filter: [{ value: 'true', label: 'Aktif' }, { value: 'false', label: 'Pasif' }] },
  ]

  const handleSearch = (event: FormEvent) => {
    event.preventDefault()
    setPage(1)
    setSearch(searchInput.trim())
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">Fiyat Listeleri</h1>

      <form className="toolbar" onSubmit={handleSearch} role="search">
        <input
          type="search"
          placeholder="Kod, ad ya da müşteri"
          aria-label="Fiyat listesi ara"
          value={searchInput}
          onChange={(event) => setSearchInput(event.target.value)}
        />
        <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
        <label className="checkbox">
          <input
            type="checkbox"
            checked={currentOnly}
            onChange={(event) => {
              setPage(1)
              setCurrentOnly(event.target.checked)
            }}
          />
          Yalnızca bugün geçerli olanlar
        </label>
        <ClearFiltersButton grid={grid} />
        <div className="toolbar__actions">
          {canManage && (
            <button type="button" className="button button--primary" onClick={() => setOpen(null)}>
              <Plus aria-hidden="true" />
              Yeni fiyat listesi
            </button>
          )}
        </div>
      </form>

      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />

      {data && (
        <>
          <div className="table-wrap" aria-busy={isFetching}>
            <table className="table">
              <GridHead columns={columns} grid={grid} />
              <tbody>
                {data.items.map((list) => (
                  <tr key={list.id}>
                    <td>
                      <button type="button" className="table__open" onClick={() => setOpen(list.id)}>
                        <code>{list.code}</code>
                      </button>
                    </td>
                    <td>{list.name}</td>
                    <td>{list.customerName ?? <span className="muted">Genel liste</span>}</td>
                    <td>{list.currency}</td>
                    <td>{formatDate(list.validFrom)}</td>
                    <td>{list.validTo ? formatDate(list.validTo) : <span className="muted">Süresiz</span>}</td>
                    <td className="table__number">{list.itemCount}</td>
                    <td>
                      {!list.isActive ? (
                        <span className="badge badge--neutral">Pasif</span>
                      ) : list.isCurrent ? (
                        <span className="badge badge--ok">Geçerli</span>
                      ) : (
                        <span className="badge badge--warning">Tarih dışı</span>
                      )}
                    </td>
                  </tr>
                ))}
                {data.items.length === 0 && (
                  <tr>
                    <td colSpan={columns.length} className="table__empty">Fiyat listesi bulunamadı.</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}

      {open !== undefined && <PriceListDialog id={open ?? undefined} onClose={() => setOpen(undefined)} />}
    </section>
  )
}
