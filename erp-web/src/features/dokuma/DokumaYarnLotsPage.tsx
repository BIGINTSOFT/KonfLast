import { Plus, Search } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { ClearFiltersButton } from '../../components/grid/ClearFiltersButton'
import { GridHead } from '../../components/grid/GridHead'
import type { GridColumn } from '../../components/grid/GridHead'
import { useGrid } from '../../components/grid/useGrid'
import { Modal } from '../../components/Modal'
import { Pagination } from '../../components/Pagination'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDate, formatQuantity, todayIsoDate } from '../../utils/format'
import { LookupSelect } from '../masterData/LookupSelect'
import { useDeleteDokumaYarnLotMutation, useGetDokumaYarnLotsQuery, useSaveDokumaYarnLotMutation } from './dokumaYarnLotsApi'
import type { DokumaYarnLot } from './dokumaYarnLotsApi'

const PAGE_SIZE = 20

function YarnLotDialog({ lot, canManage, onClose }: { lot: DokumaYarnLot | null; canManage: boolean; onClose: () => void }) {
  const [save, { isLoading, error }] = useSaveDokumaYarnLotMutation()
  const [remove, removeState] = useDeleteDokumaYarnLotMutation()
  const [receiptDate, setReceiptDate] = useState(lot?.receiptDate ?? todayIsoDate())
  const [customerId, setCustomerId] = useState<number | null>(lot?.customerId ?? null)
  const [supplierId, setSupplierId] = useState<number | null>(lot?.supplierId ?? null)
  const [yarnId, setYarnId] = useState<number | null>(lot?.yarnId ?? null)
  const [despatchNo, setDespatchNo] = useState(lot?.despatchNo ?? '')
  const [producerLotNo, setProducerLotNo] = useState(lot?.producerLotNo ?? '')
  const [packageCount, setPackageCount] = useState(lot ? String(lot.packageCount) : '')
  const [kilograms, setKilograms] = useState(lot ? String(lot.kilograms) : '')
  const [notes, setNotes] = useState(lot?.notes ?? '')
  const [confirmDelete, setConfirmDelete] = useState(false)

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await save({
      id: lot?.id,
      body: {
        receiptDate,
        customerId,
        supplierId,
        yarnId: yarnId ?? 0,
        despatchNo: despatchNo.trim() || null,
        producerLotNo: producerLotNo.trim() || null,
        packageCount: Number(packageCount),
        kilograms: Number(kilograms),
        notes: notes.trim() || null,
      },
    })
    if (!('error' in result)) {
      onClose()
    }
  }

  const handleDelete = async () => {
    if (lot && !('error' in await remove(lot.id))) {
      onClose()
    }
  }

  return (
    <Modal title={lot ? `İplik girişi: ${lot.lotNo}` : 'Yeni iplik girişi'} onClose={onClose}>
      <form className="form" onSubmit={handleSubmit}>
        {lot && lot.issuedKilograms + lot.returnedKilograms > 0 && (
          <div className="alert alert--info">Bu lottan {formatQuantity(lot.issuedKilograms + lot.returnedKilograms)} kg çıktı: ipliği ve sahibi değiştirilemez, lot silinemez.</div>
        )}
        {lot?.purchaseOrderNo && (
          <p className="muted">Satın alma: <Link to={`/dokuma/purchase-orders/${lot.purchaseOrderId}`}>{lot.purchaseOrderNo}</Link> · iplik ve sahibi değiştirilemez; kilo değişince sipariş teslim miktarı güncellenir.</p>
        )}
        <fieldset disabled={!canManage} className="form">
          <div className="form__pair">
            <label className="field"><span>Giriş tarihi</span><input type="date" required value={receiptDate} onChange={(event) => setReceiptDate(event.target.value)} /></label>
            <label className="field"><span>İrsaliye no</span><input maxLength={50} value={despatchNo} onChange={(event) => setDespatchNo(event.target.value)} /></label>
          </div>
          <LookupSelect resource="dokuma-customers" label="Malın sahibi (müşteri)" value={customerId} onChange={setCustomerId} placeholder="Boş bırakılırsa işletme malı" />
          <LookupSelect resource="dokuma-suppliers" label="Tedarikçi" value={supplierId} onChange={setSupplierId} placeholder="İsteğe bağlı" />
          <LookupSelect resource="dokuma-yarns" label="İplik kartı" required value={yarnId} onChange={setYarnId} />
          <label className="field">
            <span>Üretici lot no</span>
            <input maxLength={50} value={producerLotNo} onChange={(event) => setProducerLotNo(event.target.value)} />
            <small className="field__hint">Farklı üretici lotları aynı çözgüde karışırsa renk farkı çıkabilir.</small>
          </label>
          <div className="form__pair">
            <label className="field"><span>Bobin sayısı</span><input type="number" min="1" step="1" required value={packageCount} onChange={(event) => setPackageCount(event.target.value)} /></label>
            <label className="field"><span>Kilo</span><input type="number" min="0" step="0.01" required value={kilograms} onChange={(event) => setKilograms(event.target.value)} /></label>
          </div>
          <label className="field"><span>Not</span><textarea rows={2} maxLength={1000} value={notes} onChange={(event) => setNotes(event.target.value)} /></label>
        </fieldset>
        <ErrorAlert error={error ?? removeState.error} />
        <footer className="form__actions">
          {canManage && lot && !confirmDelete && <button type="button" className="button button--danger-ghost" onClick={() => setConfirmDelete(true)}>Sil</button>}
          {canManage && lot && confirmDelete && <button type="button" className="button button--danger-ghost" disabled={removeState.isLoading} onClick={handleDelete}>Silmeyi onayla</button>}
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          {canManage && <button type="submit" className="button button--primary" disabled={isLoading}>Kaydet</button>}
        </footer>
      </form>
    </Modal>
  )
}

/** İplik girişleri: kilo ve lot bazında, bobin sayısıyla. */
export function DokumaYarnLotsPage() {
  const canManage = usePermission(Permissions.DokumaReceiptManage)
  const grid = useGrid()
  const { page, setPage } = grid
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [editing, setEditing] = useState<DokumaYarnLot | null | undefined>(undefined)
  const { data, error, isLoading, isFetching } = useGetDokumaYarnLotsQuery({ page, pageSize: PAGE_SIZE, search: search || undefined, ...grid.params })

  const columns: GridColumn[] = [
    { key: 'lotNo', label: 'Lot no' },
    { key: 'receiptDate', label: 'Tarih', filter: false },
    { key: 'customerName', label: 'Malın sahibi' },
    { key: 'yarnCode', label: 'İplik' },
    { key: 'producerLotNo', label: 'Üretici lot no' },
    { key: 'supplierName', label: 'Tedarikçi' },
    { key: 'despatchNo', label: 'İrsaliye no' },
    { key: 'packageCount', label: 'Bobin', numeric: true },
    { key: 'kilograms', label: 'Kilo', numeric: true },
    { key: 'remainingKilograms', label: 'Kalan', numeric: true, filter: false, sortable: false },
  ]

  const handleSearch = (event: FormEvent) => {
    event.preventDefault()
    setPage(1)
    setSearch(searchInput.trim())
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">İplik Girişleri</h1>
      <form className="toolbar" onSubmit={handleSearch} role="search">
        <input type="search" placeholder="Lot no, irsaliye, iplik ya da müşteri" aria-label="İplik girişi ara" value={searchInput} onChange={(event) => setSearchInput(event.target.value)} />
        <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
        <ClearFiltersButton grid={grid} />
        <div className="toolbar__actions">
          {canManage && (
            <button type="button" className="button button--primary" onClick={() => setEditing(null)}>
              <Plus aria-hidden="true" />
              Yeni iplik girişi
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
                {data.items.map((lot) => (
                  <tr key={lot.id} className="table__row--link" onClick={() => setEditing(lot)}>
                    <td><code>{lot.lotNo}</code></td>
                    <td>{formatDate(lot.receiptDate)}</td>
                    <td>{lot.customerName ?? <span className="muted">İşletme malı</span>}</td>
                    <td>{lot.yarnCode} — {lot.yarnName}{lot.yarnColor && <span className="muted"> ({lot.yarnColor})</span>}</td>
                    <td>{lot.producerLotNo ?? '—'}</td>
                    <td>{lot.supplierName ?? '—'}</td>
                    <td>{lot.despatchNo ?? '—'}</td>
                    <td className="table__number">{formatQuantity(lot.packageCount)}</td>
                    <td className="table__number">{formatQuantity(lot.kilograms)}</td>
                    <td className="table__number">{formatQuantity(lot.remainingKilograms)}</td>
                  </tr>
                ))}
                {data.items.length === 0 && <tr><td colSpan={columns.length} className="table__empty">İplik girişi bulunamadı.</td></tr>}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}
      {editing !== undefined && <YarnLotDialog lot={editing} canManage={canManage} onClose={() => setEditing(undefined)} />}
    </section>
  )
}
