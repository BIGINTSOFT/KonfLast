import { Plus, Search } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { ClearFiltersButton } from '../../components/grid/ClearFiltersButton'
import { GridHead } from '../../components/grid/GridHead'
import type { GridColumn } from '../../components/grid/GridHead'
import { useGrid } from '../../components/grid/useGrid'
import { Pagination } from '../../components/Pagination'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDate, formatQuantity } from '../../utils/format'
import { useGetFabricReceiptsQuery } from './boyahaneReceiptsApi'
import { FabricReceiptFormDialog } from './FabricReceiptFormDialog'

const PAGE_SIZE = 20

/** Kumaş kabul belgeleri, en yeniden eskiye. */
export function FabricReceiptsPage() {
  const canManage = usePermission(Permissions.BoyahaneReceiptManage)
  const grid = useGrid()
  const { page, setPage } = grid
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [creating, setCreating] = useState(false)

  const { data, error, isLoading, isFetching } = useGetFabricReceiptsQuery({
    page,
    pageSize: PAGE_SIZE,
    search: search || undefined,
    ...grid.params,
  })

  const columns: GridColumn[] = [
    { key: 'receiptNo', label: 'Belge no' },
    { key: 'receiptDate', label: 'Tarih', filter: false },
    { key: 'customerName', label: 'Malın sahibi' },
    { key: 'despatchNo', label: 'İrsaliye no' },
    { key: 'rollCount', label: 'Top', numeric: true },
    { key: 'totalMeters', label: 'Metre', numeric: true },
    { key: 'totalKilograms', label: 'Kilo', numeric: true },
    { key: 'pendingRollCount', label: 'Kontrol bekleyen', numeric: true },
  ]

  const handleSearch = (event: FormEvent) => {
    event.preventDefault()
    setPage(1)
    setSearch(searchInput.trim())
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">Kumaş Kabul</h1>

      <form className="toolbar" onSubmit={handleSearch} role="search">
        <input
          type="search"
          placeholder="Belge no, irsaliye no ya da müşteri"
          aria-label="Kabul belgesi ara"
          value={searchInput}
          onChange={(event) => setSearchInput(event.target.value)}
        />
        <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
        <ClearFiltersButton grid={grid} />
        <div className="toolbar__actions">
          {canManage && (
            <button type="button" className="button button--primary" onClick={() => setCreating(true)}>
              <Plus aria-hidden="true" />
              Yeni kabul
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
                {data.items.map((receipt) => (
                  <tr key={receipt.id}>
                    <td><Link to={`/boyahane/receipts/${receipt.id}`}><code>{receipt.receiptNo}</code></Link></td>
                    <td>{formatDate(receipt.receiptDate)}</td>
                    <td>{receipt.customerName ?? <span className="muted">İşletme malı</span>}</td>
                    <td>{receipt.despatchNo ?? '—'}</td>
                    <td className="table__number">{formatQuantity(receipt.rollCount)}</td>
                    <td className="table__number">{formatQuantity(receipt.totalMeters)}</td>
                    <td className="table__number">{formatQuantity(receipt.totalKilograms)}</td>
                    <td className="table__number">{formatQuantity(receipt.pendingRollCount)}</td>
                  </tr>
                ))}
                {data.items.length === 0 && (
                  <tr>
                    <td colSpan={columns.length} className="table__empty">Kabul belgesi bulunamadı.</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}

      {creating && <FabricReceiptFormDialog onClose={() => setCreating(false)} />}
    </section>
  )
}
