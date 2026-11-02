import { Plus, Search } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
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
import {
  useApproveYarnLotMutation,
  useDeleteYarnLotMutation,
  useGetYarnLotsQuery,
  useRejectYarnLotMutation,
  useSaveYarnLotMutation,
  yarnLotStatusPresentation,
  yarnPackageTypeLabels,
} from './boyahaneYarnLotsApi'
import type { YarnLot, YarnPackageType } from './boyahaneYarnLotsApi'

const PAGE_SIZE = 20

function YarnLotDialog({ lot, canManage, canInspect, onClose }: { lot: YarnLot | null; canManage: boolean; canInspect: boolean; onClose: () => void }) {
  const [save, { isLoading, error }] = useSaveYarnLotMutation()
  const [remove, removeState] = useDeleteYarnLotMutation()
  const [reject, rejectState] = useRejectYarnLotMutation()
  const [approve, approveState] = useApproveYarnLotMutation()
  const [rejectionNote, setRejectionNote] = useState('')
  const [receiptDate, setReceiptDate] = useState(lot?.receiptDate ?? todayIsoDate())
  const [customerId, setCustomerId] = useState<number | null>(lot?.customerId ?? null)
  const [yarnId, setYarnId] = useState<number | null>(lot?.yarnId ?? null)
  const [despatchNo, setDespatchNo] = useState(lot?.despatchNo ?? '')
  const [customerLotNo, setCustomerLotNo] = useState(lot?.customerLotNo ?? '')
  const [packageType, setPackageType] = useState<YarnPackageType>(lot?.packageType ?? 'BOBBIN')
  const [packageCount, setPackageCount] = useState(lot ? String(lot.packageCount) : '')
  const [kilograms, setKilograms] = useState(lot ? String(lot.kilograms) : '')
  const [notes, setNotes] = useState(lot?.notes ?? '')
  const [locationId, setLocationId] = useState<number | null>(null)
  const locked = lot?.orderNo != null

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await save({
      id: lot?.id,
      body: {
        receiptDate,
        customerId,
        yarnId: yarnId ?? 0,
        despatchNo: despatchNo.trim() || null,
        customerLotNo: customerLotNo.trim() || null,
        packageType,
        packageCount: Number(packageCount),
        kilograms: Number(kilograms),
        notes: notes.trim() || null,
        locationId,
      },
    })
    if (!('error' in result)) {
      onClose()
    }
  }

  const handleReject = async () => {
    if (lot && !('error' in await reject({ id: lot.id, note: rejectionNote.trim() }))) {
      onClose()
    }
  }

  const handleApprove = async () => {
    if (lot && !('error' in await approve(lot.id))) {
      onClose()
    }
  }

  const handleDelete = async () => {
    if (lot && !('error' in await remove(lot.id))) {
      onClose()
    }
  }

  return (
    <Modal title={lot ? `İplik lotu: ${lot.lotNo}` : 'Yeni iplik kabul'} onClose={onClose}>
      <form className="form" onSubmit={handleSubmit}>
        {locked && <div className="alert alert--info">Bu lot {lot?.orderNo} siparişine bağlı; düzenlemek için önce siparişten çıkarın.</div>}
        {lot?.status === 'REJECTED' && <div className="alert alert--info">Reddedildi: {lot.rejectionNote}</div>}
        <fieldset disabled={locked || !canManage} className="form">
          <div className="form__pair">
            <label className="field"><span>Kabul tarihi</span><input type="date" required value={receiptDate} onChange={(event) => setReceiptDate(event.target.value)} /></label>
            <label className="field"><span>İrsaliye no</span><input maxLength={50} value={despatchNo} onChange={(event) => setDespatchNo(event.target.value)} /></label>
          </div>
          <LookupSelect resource="boyahane-customers" label="Malın sahibi (müşteri)" value={customerId} onChange={setCustomerId} placeholder="Boş bırakılırsa işletme malı" />
          <LookupSelect resource="boyahane-yarns" label="İplik kartı" required value={yarnId} onChange={setYarnId} />
          {lot
            ? <p className="muted">Depo adresi: {lot.locationCode ?? '—'} (Adres Stoğu ekranından taşınır)</p>
            : <LookupSelect resource="boyahane-warehouse-locations" label="Depo adresi" value={locationId} onChange={setLocationId} placeholder="Boşsa ham deponun genel adresi" />}
          <label className="field"><span>Müşteri lot no</span><input maxLength={50} value={customerLotNo} onChange={(event) => setCustomerLotNo(event.target.value)} /></label>
          <div className="form__pair">
            <label className="field">
              <span>Ambalaj</span>
              <select value={packageType} onChange={(event) => setPackageType(event.target.value as YarnPackageType)}>
                {Object.entries(yarnPackageTypeLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
              </select>
            </label>
            <label className="field"><span>Adet</span><input type="number" min="1" step="1" required value={packageCount} onChange={(event) => setPackageCount(event.target.value)} /></label>
          </div>
          <label className="field"><span>Kilo</span><input type="number" min="0" step="0.01" required value={kilograms} onChange={(event) => setKilograms(event.target.value)} /></label>
          <label className="field"><span>Not</span><textarea rows={2} maxLength={1000} value={notes} onChange={(event) => setNotes(event.target.value)} /></label>
        </fieldset>
        {canInspect && lot?.status === 'APPROVED' && (
          <label className="field">
            <span>Red gerekçesi</span>
            <input maxLength={500} placeholder="Lot hatalıysa yazıp reddedin" value={rejectionNote} onChange={(event) => setRejectionNote(event.target.value)} />
          </label>
        )}
        <ErrorAlert error={error ?? removeState.error ?? rejectState.error ?? approveState.error} />
        <footer className="form__actions">
          {canInspect && lot?.status === 'APPROVED' && (
            <button type="button" className="button button--danger-ghost" disabled={rejectState.isLoading || rejectionNote.trim() === ''} onClick={handleReject}>Reddet</button>
          )}
          {canInspect && lot?.status === 'REJECTED' && (
            <button type="button" className="button" disabled={approveState.isLoading} onClick={handleApprove}>Uygun yap</button>
          )}
          {canManage && lot && !locked && <button type="button" className="button button--danger-ghost" disabled={removeState.isLoading} onClick={handleDelete}>Sil</button>}
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          {canManage && !locked && <button type="submit" className="button button--primary" disabled={isLoading}>Kaydet</button>}
        </footer>
      </form>
    </Modal>
  )
}

/** İplik girişleri: kilo ve lot bazında, bobin ya da çile sayısıyla. */
export function YarnLotsPage() {
  const canManage = usePermission(Permissions.BoyahaneReceiptManage)
  const canInspect = usePermission(Permissions.BoyahaneInspectionManage)
  const grid = useGrid()
  const { page, setPage } = grid
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [editing, setEditing] = useState<YarnLot | null | undefined>(undefined)

  const { data, error, isLoading, isFetching } = useGetYarnLotsQuery({ page, pageSize: PAGE_SIZE, search: search || undefined, ...grid.params })

  const columns: GridColumn[] = [
    { key: 'lotNo', label: 'Lot no' },
    { key: 'receiptDate', label: 'Tarih', filter: false },
    { key: 'customerName', label: 'Malın sahibi' },
    { key: 'yarnCode', label: 'İplik' },
    { key: 'customerLotNo', label: 'Müşteri lot no' },
    { key: 'packageCount', label: 'Adet', numeric: true },
    { key: 'kilograms', label: 'Kilo', numeric: true },
    { key: 'orderNo', label: 'Sipariş' },
    {
      key: 'status',
      label: 'Durum',
      filter: Object.entries(yarnLotStatusPresentation).map(([value, { label }]) => ({ value, label })),
    },
  ]

  const handleSearch = (event: FormEvent) => {
    event.preventDefault()
    setPage(1)
    setSearch(searchInput.trim())
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">İplik Kabul</h1>
      <form className="toolbar" onSubmit={handleSearch} role="search">
        <input type="search" placeholder="Lot no, irsaliye no ya da müşteri" aria-label="İplik lotu ara" value={searchInput} onChange={(event) => setSearchInput(event.target.value)} />
        <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
        <ClearFiltersButton grid={grid} />
        <div className="toolbar__actions">
          {canManage && (
            <button type="button" className="button button--primary" onClick={() => setEditing(null)}>
              <Plus aria-hidden="true" />
              Yeni iplik kabul
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
                  <tr key={lot.id} className={canManage || canInspect ? 'table__row--link' : undefined} onClick={() => (canManage || canInspect) && setEditing(lot)}>
                    <td><code>{lot.lotNo}</code></td>
                    <td>{formatDate(lot.receiptDate)}</td>
                    <td>{lot.customerName ?? <span className="muted">İşletme malı</span>}</td>
                    <td>{lot.yarnCode} — {lot.yarnName}</td>
                    <td>{lot.customerLotNo ?? '—'}</td>
                    <td className="table__number">{formatQuantity(lot.packageCount)} {yarnPackageTypeLabels[lot.packageType].toLowerCase()}</td>
                    <td className="table__number">{formatQuantity(lot.kilograms)}</td>
                    <td>{lot.orderNo ?? <span className="muted">Boşta</span>}</td>
                    <td><span className={`badge badge--${yarnLotStatusPresentation[lot.status].tone}`}>{yarnLotStatusPresentation[lot.status].label}</span></td>
                  </tr>
                ))}
                {data.items.length === 0 && <tr><td colSpan={columns.length} className="table__empty">İplik lotu bulunamadı.</td></tr>}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}

      {editing !== undefined && <YarnLotDialog lot={editing} canManage={canManage} canInspect={canInspect} onClose={() => setEditing(undefined)} />}
    </section>
  )
}
