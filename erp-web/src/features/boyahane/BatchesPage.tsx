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
import { BatchFormDialog } from './BatchFormDialog'
import { batchStatusPresentation, useGetBatchesQuery } from './boyahaneBatchesApi'
import { orderProcessTypeLabels } from './boyahaneOrdersApi'

const PAGE_SIZE = 20

/** Boya partileri, en yeniden eskiye. */
export function BatchesPage() {
  const canManage = usePermission(Permissions.BoyahaneBatchManage)
  const grid = useGrid()
  const { page, setPage } = grid
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [creating, setCreating] = useState(false)
  const { data, error, isLoading, isFetching } = useGetBatchesQuery({ page, pageSize: PAGE_SIZE, search: search || undefined, ...grid.params })

  const columns: GridColumn[] = [
    { key: 'batchNo', label: 'Parti no' },
    { key: 'processType', label: 'İşlem', filter: Object.entries(orderProcessTypeLabels).map(([value, label]) => ({ value, label })) },
    { key: 'machineCode', label: 'Makine' },
    { key: 'itemCode', label: 'Kart' },
    { key: 'colorName', label: 'Renk' },
    { key: 'plannedDate', label: 'Plan', filter: false },
    { key: 'itemCount', label: 'Mal', numeric: true },
    { key: 'totalKilograms', label: 'Kilo', numeric: true },
    {
      key: 'status',
      label: 'Durum',
      filter: Object.entries(batchStatusPresentation).map(([value, { label }]) => ({ value, label })),
    },
    { key: 'currentStep', label: 'Adım', filter: false },
  ]

  const handleSearch = (event: FormEvent) => {
    event.preventDefault()
    setPage(1)
    setSearch(searchInput.trim())
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">Partiler</h1>
      <form className="toolbar" onSubmit={handleSearch} role="search">
        <input type="search" placeholder="Parti no ya da makine" aria-label="Parti ara" value={searchInput} onChange={(event) => setSearchInput(event.target.value)} />
        <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
        <ClearFiltersButton grid={grid} />
        <div className="toolbar__actions">
          {canManage && (
            <button type="button" className="button button--primary" onClick={() => setCreating(true)}>
              <Plus aria-hidden="true" />
              Yeni parti
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
                {data.items.map((batch) => {
                  const status = batchStatusPresentation[batch.status]
                  return (
                    <tr key={batch.id}>
                      <td><Link to={`/boyahane/batches/${batch.id}`}><code>{batch.batchNo}</code></Link></td>
                      <td>{orderProcessTypeLabels[batch.processType]}</td>
                      <td>{batch.machineCode}</td>
                      <td>{batch.itemCode}</td>
                      <td>{batch.colorName ?? '—'}</td>
                      <td>{batch.plannedDate ? formatDate(batch.plannedDate) : '—'}</td>
                      <td className="table__number">{batch.itemCount}</td>
                      <td className="table__number">{formatQuantity(batch.totalKilograms)}</td>
                      <td><span className={`badge badge--${status.tone}`}>{status.label}</span></td>
                      <td>{batch.currentStep && batch.status !== 'CANCELLED' ? batch.currentStep : '—'}</td>
                    </tr>
                  )
                })}
                {data.items.length === 0 && <tr><td colSpan={columns.length} className="table__empty">Parti bulunamadı.</td></tr>}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}

      {creating && <BatchFormDialog onClose={() => setCreating(false)} />}
    </section>
  )
}
