import { Search } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { ClearFiltersButton } from '../../components/grid/ClearFiltersButton'
import { GridHead } from '../../components/grid/GridHead'
import type { GridColumn } from '../../components/grid/GridHead'
import { useGrid } from '../../components/grid/useGrid'
import { Pagination } from '../../components/Pagination'
import { formatDate, formatQuantity } from '../../utils/format'
import { LookupSelect } from '../masterData/LookupSelect'
import { PaidBadge } from './SubcontractBadges'
import { SubcontractReturnDetailDialog } from './SubcontractReturnDetailDialog'
import { useGetSubcontractReturnsQuery, useGetSubcontractStatementQuery } from './subcontractApi'
import { formatAmounts } from './subcontractPresentation'

const PAGE_SIZE = 25

const columns: GridColumn[] = [
  { key: 'returnNo', label: 'Dönüş no' },
  { key: 'returnDate', label: 'Tarih', filter: 'date' },
  { key: 'supplierName', label: 'Fasoncu' },
  { key: 'sendNo', label: 'Gönderim' },
  { key: 'operationName', label: 'Operasyon' },
  { key: 'goodQuantity', label: 'Sağlam', numeric: true },
  { label: 'Hatalı / eksik', numeric: true },
  { label: 'Hakediş', numeric: true },
  { label: 'Ödeme' },
]

/** İlk gün: içinde bulunulan ayın başı (yyyy-aa-gg). */
function monthStart(): string {
  const now = new Date()
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-01`
}

/**
 * Fason dönüşleri ve hakediş: fasoncu ve dönem bazında dönen adetler, tutarlar (sağlam × fiyat) ve ödendi işareti.
 * Üstte seçili dönemin fasoncu bazında toplamları (ödenen / ödenmeyen) yer alır.
 */
export function SubcontractReturnsPage() {
  const grid = useGrid()
  const { page, setPage } = grid
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [supplierId, setSupplierId] = useState<number | null>(null)
  const [from, setFrom] = useState(monthStart())
  const [to, setTo] = useState('')
  const [paid, setPaid] = useState<'all' | 'paid' | 'unpaid'>('all')
  const [openId, setOpenId] = useState<number | null>(null)

  const filters = { supplierId: supplierId ?? undefined, from: from || undefined, to: to || undefined }
  const { data, error, isLoading, isFetching } = useGetSubcontractReturnsQuery({
    page,
    pageSize: PAGE_SIZE,
    search: search || undefined,
    paid: paid === 'all' ? undefined : paid === 'paid',
    ...filters,
    ...grid.params,
  })
  const statement = useGetSubcontractStatementQuery(filters)

  const handleSearch = (event: FormEvent) => {
    event.preventDefault()
    setPage(1)
    setSearch(searchInput.trim())
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">Fason Dönüşler ve Hakediş</h1>

      <form className="toolbar" onSubmit={handleSearch} role="search">
        <input
          type="search"
          placeholder="Dönüş / gönderim no, fasoncu, irsaliye"
          aria-label="Fason dönüşü ara"
          value={searchInput}
          onChange={(event) => setSearchInput(event.target.value)}
        />
        <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
        <div style={{ minWidth: '16rem' }}>
          <LookupSelect resource="suppliers" label="Fasoncu" hideLabel placeholder="— Tüm fasoncular —" value={supplierId} onChange={(value) => { setPage(1); setSupplierId(value) }} />
        </div>
        <label className="checkbox">
          <span>Başlangıç</span>
          <input type="date" value={from} onChange={(event) => { setPage(1); setFrom(event.target.value) }} aria-label="Dönem başı" />
        </label>
        <label className="checkbox">
          <span>Bitiş</span>
          <input type="date" value={to} min={from || undefined} onChange={(event) => { setPage(1); setTo(event.target.value) }} aria-label="Dönem sonu" />
        </label>
        <select value={paid} aria-label="Ödeme durumu" onChange={(event) => { setPage(1); setPaid(event.target.value as typeof paid) }}>
          <option value="all">Tümü</option>
          <option value="unpaid">Ödenmemiş</option>
          <option value="paid">Ödenmiş</option>
        </select>
        <ClearFiltersButton grid={grid} />
      </form>

      <ErrorAlert error={statement.error} />
      {statement.data && statement.data.suppliers.length > 0 && (
        <>
          <h2 className="section-title">Hakediş özeti</h2>
          <div className="table-wrap">
            <table className="table table--compact">
              <thead>
                <tr>
                  <th>Fasoncu</th>
                  <th className="table__number">Dönüş</th>
                  <th className="table__number">Sağlam adet</th>
                  <th className="table__number">Hakediş</th>
                  <th className="table__number">Ödenen</th>
                  <th className="table__number">Ödenmeyen</th>
                </tr>
              </thead>
              <tbody>
                {statement.data.suppliers.map((row) => (
                  <tr key={row.supplierId}>
                    <td>{row.supplierName}</td>
                    <td className="table__number">{row.returnCount}</td>
                    <td className="table__number">{formatQuantity(row.goodQuantity)}</td>
                    <td className="table__number">{formatAmounts(row.total)}</td>
                    <td className="table__number">{formatAmounts(row.paid)}</td>
                    <td className="table__number"><strong>{formatAmounts(row.unpaid)}</strong></td>
                  </tr>
                ))}
              </tbody>
              <tfoot>
                <tr>
                  <th colSpan={3}>Toplam</th>
                  <td className="table__number">{formatAmounts(statement.data.total)}</td>
                  <td className="table__number">{formatAmounts(statement.data.paid)}</td>
                  <td className="table__number"><strong>{formatAmounts(statement.data.unpaid)}</strong></td>
                </tr>
              </tfoot>
            </table>
          </div>
          <h2 className="section-title">Dönüş fişleri</h2>
        </>
      )}

      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />

      {data && (
        <>
          <div className="table-wrap" aria-busy={isFetching}>
            <table className="table">
              <GridHead columns={columns} grid={grid} />
              <tbody>
                {data.items.map((ret) => (
                  <tr key={ret.id}>
                    <td>
                      <button type="button" className="table__open" onClick={() => setOpenId(ret.id)}>
                        <code>{ret.returnNo}</code>
                      </button>
                    </td>
                    <td>{formatDate(ret.returnDate)}</td>
                    <td>{ret.supplierName}</td>
                    <td><Link to={`/subcontract/sends/${ret.subcontractSendId}`}>{ret.sendNo}</Link></td>
                    <td>{ret.operationName}</td>
                    <td className="table__number">{formatQuantity(ret.goodQuantity)}</td>
                    <td className="table__number">{formatQuantity(ret.defectiveQuantity + ret.missingQuantity)}</td>
                    <td className="table__number">
                      {formatAmounts(ret.amounts)}
                      {ret.hasUnpricedLines && <span className="badge badge--warning" title="Fiyatı olmayan satır var">Fiyat eksik</span>}
                    </td>
                    <td><PaidBadge paid={ret.isPaid} />{ret.paidDate && <span className="muted"> {formatDate(ret.paidDate)}</span>}</td>
                  </tr>
                ))}
                {data.items.length === 0 && (
                  <tr>
                    <td colSpan={columns.length} className="table__empty">Bu dönemde fason dönüşü yok.</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}

      {openId !== null && <SubcontractReturnDetailDialog id={openId} onClose={() => setOpenId(null)} />}
    </section>
  )
}
