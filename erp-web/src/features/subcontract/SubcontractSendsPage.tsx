import { Plus, Search } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { ClearFiltersButton } from '../../components/grid/ClearFiltersButton'
import { GridHead } from '../../components/grid/GridHead'
import type { GridColumn } from '../../components/grid/GridHead'
import { useGrid } from '../../components/grid/useGrid'
import { Pagination } from '../../components/Pagination'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDate, formatQuantity } from '../../utils/format'
import { SubcontractSendStatusBadge } from './SubcontractBadges'
import { SubcontractSendDialog } from './SubcontractSendDialog'
import { useGetSubcontractSendsQuery } from './subcontractApi'
import { daysSince, subcontractSendStatusPresentation } from './subcontractPresentation'

const PAGE_SIZE = 25

const columns: GridColumn[] = [
  { key: 'sendNo', label: 'Fiş no' },
  { key: 'supplierName', label: 'Fasoncu' },
  { key: 'sendDate', label: 'Tarih', filter: 'date' },
  { key: 'operationName', label: 'Operasyon' },
  { key: 'totalQuantity', label: 'Gönderilen', numeric: true },
  { key: 'returnedGood', label: 'Sağlam', numeric: true },
  { label: 'Hatalı / eksik', numeric: true },
  { key: 'outstanding', label: 'Dışarıda', numeric: true },
  { label: 'Gün', numeric: true },
  {
    key: 'status',
    label: 'Durum',
    filter: Object.entries(subcontractSendStatusPresentation).map(([value, { label }]) => ({ value, label })),
  },
]

/** Fason gönderim fişleri: fasoncuya gönderilen demetler / adetler ve dönüş durumu. */
export function SubcontractSendsPage() {
  const canManage = usePermission(Permissions.SubcontractManage)
  const navigate = useNavigate()
  const grid = useGrid()
  const { page, setPage } = grid
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [openOnly, setOpenOnly] = useState(true)
  const [creating, setCreating] = useState(false)

  const { data, error, isLoading, isFetching } = useGetSubcontractSendsQuery({
    page,
    pageSize: PAGE_SIZE,
    search: search || undefined,
    openOnly: openOnly || undefined,
    ...grid.params,
  })

  const handleSearch = (event: FormEvent) => {
    event.preventDefault()
    setPage(1)
    setSearch(searchInput.trim())
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">Fason Gönderimler</h1>

      <form className="toolbar" onSubmit={handleSearch} role="search">
        <input
          type="search"
          placeholder="Fiş no, fasoncu ya da operasyon"
          aria-label="Fason gönderim ara"
          value={searchInput}
          onChange={(event) => setSearchInput(event.target.value)}
        />
        <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
        <label className="checkbox">
          <input
            type="checkbox"
            checked={openOnly}
            onChange={(event) => {
              setPage(1)
              setOpenOnly(event.target.checked)
            }}
          />
          Yalnızca fasonda parçası olanlar
        </label>
        <ClearFiltersButton grid={grid} />
        <div className="toolbar__actions">
          {canManage && (
            <button type="button" className="button button--primary" onClick={() => setCreating(true)}>
              <Plus aria-hidden="true" />
              Yeni gönderim
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
                {data.items.map((send) => {
                  const open = send.status === 'OPEN' || send.status === 'PARTIALLY_RETURNED'
                  return (
                    <tr key={send.id}>
                      <td><Link to={`/subcontract/sends/${send.id}`}><code>{send.sendNo}</code></Link></td>
                      <td>{send.supplierName}</td>
                      <td>{formatDate(send.sendDate)}</td>
                      <td>{send.operationName}</td>
                      <td className="table__number">{formatQuantity(send.totalQuantity)}</td>
                      <td className="table__number">{formatQuantity(send.returnedGood)}</td>
                      <td className="table__number">{formatQuantity(send.returnedDefective + send.returnedMissing)}</td>
                      <td className="table__number">{open ? <strong>{formatQuantity(send.outstanding)}</strong> : '—'}</td>
                      <td className="table__number">{open ? daysSince(send.sendDate) : '—'}</td>
                      <td><SubcontractSendStatusBadge status={send.status} /></td>
                    </tr>
                  )
                })}
                {data.items.length === 0 && (
                  <tr>
                    <td colSpan={columns.length} className="table__empty">Fason gönderimi bulunamadı.</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}

      {creating && (
        <SubcontractSendDialog
          onClose={() => setCreating(false)}
          onSaved={(send) => void navigate(`/subcontract/sends/${send.id}`)}
        />
      )}
    </section>
  )
}
