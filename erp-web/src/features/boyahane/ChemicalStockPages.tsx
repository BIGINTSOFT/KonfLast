import { Plus, Search } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { baseApi } from '../../api/baseApi'
import { ErrorAlert } from '../../components/FormControls'
import { ClearFiltersButton } from '../../components/grid/ClearFiltersButton'
import { GridHead } from '../../components/grid/GridHead'
import type { GridColumn } from '../../components/grid/GridHead'
import { useGrid } from '../../components/grid/useGrid'
import { Modal } from '../../components/Modal'
import { Pagination } from '../../components/Pagination'
import { usePermission } from '../../hooks/usePermission'
import type { PagedResult } from '../../types/api'
import { Permissions } from '../../types/permissions'
import { formatDate, formatDateTime, formatQuantity, todayIsoDate } from '../../utils/format'
import { formatMoney } from '../../utils/money'
import { LookupSelect } from '../masterData/LookupSelect'

type MovementType = 'RECEIPT' | 'CONSUMPTION' | 'ADJUSTMENT'

const movementLabels: Record<MovementType, string> = { RECEIPT: 'Giriş', CONSUMPTION: 'Tüketim', ADJUSTMENT: 'Sayım düzeltmesi' }

interface StockRow {
  dyeChemicalId: number
  code: string
  name: string
  kind: 'DYE' | 'CHEMICAL'
  usableKilograms: number
  expiredKilograms: number
  lotCount: number
  nearestExpiry: string | null
}

interface ChemicalLot {
  id: number
  lotNo: string
  dyeChemicalId: number
  chemicalCode: string
  chemicalName: string
  supplierLotNo: string | null
  supplierId: number | null
  supplierName: string | null
  purchaseOrderNo: string | null
  receiptDate: string
  expiryDate: string | null
  receivedKilograms: number
  remainingKilograms: number
  unitCost: number | null
  currency: string
  notes: string | null
  isUsed: boolean
  locationCode: string | null
}

interface Movement {
  id: number
  lotNo: string
  chemicalCode: string
  chemicalName: string
  type: MovementType
  kilograms: number
  reference: string | null
  createdAt: string
  createdBy: string | null
}

interface LotRequest {
  locationId: number | null
  dyeChemicalId: number
  receiptDate: string
  kilograms: number
  supplierLotNo: string | null
  supplierId: number | null
  expiryDate: string | null
  unitCost: number | null
  currency: string | null
  notes: string | null
}

const stockApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getChemicalStock: build.query<StockRow[], void>({ query: () => '/boyahane-chemical-stock', providesTags: ['BoyahaneBatch'] }),
    getChemicalLots: build.query<PagedResult<ChemicalLot>, Record<string, unknown>>({
      query: (params) => ({ url: '/boyahane-chemical-stock/lots', params }),
      providesTags: ['BoyahaneBatch'],
    }),
    getChemicalMovements: build.query<PagedResult<Movement>, Record<string, unknown>>({
      query: (params) => ({ url: '/boyahane-chemical-stock/movements', params }),
      providesTags: ['BoyahaneBatch'],
    }),
    saveChemicalLot: build.mutation<ChemicalLot, { id?: number; body: LotRequest }>({
      query: ({ id, body }) => ({ url: id === undefined ? '/boyahane-chemical-stock/lots' : `/boyahane-chemical-stock/lots/${id}`, method: id === undefined ? 'POST' : 'PUT', body }),
      invalidatesTags: ['BoyahaneBatch'],
    }),
    deleteChemicalLot: build.mutation<null, number>({
      query: (id) => ({ url: `/boyahane-chemical-stock/lots/${id}`, method: 'DELETE' }),
      invalidatesTags: ['BoyahaneBatch'],
    }),
    adjustChemicalLot: build.mutation<ChemicalLot, { id: number; countedKilograms: number; note: string }>({
      query: ({ id, ...body }) => ({ url: `/boyahane-chemical-stock/lots/${id}/adjust`, method: 'POST', body }),
      invalidatesTags: ['BoyahaneBatch'],
    }),
  }),
})

const PAGE_SIZE = 20

/** Boya ve kimyasal stoğu: kullanılabilir miktar, son kullanma tarihi geçen miktar ayrı. */
export function ChemicalStockPage() {
  const { data, error, isLoading } = stockApi.useGetChemicalStockQuery()
  return (
    <section className="page">
      <h1 className="visually-hidden">Boya ve Kimyasal Stoğu</h1>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {data && (
        <div className="table-wrap">
          <table className="table">
            <thead><tr><th>Kod</th><th>Ad</th><th>Tür</th><th className="table__number">Kullanılabilir (kg)</th><th className="table__number">Süresi geçen (kg)</th><th className="table__number">Lot</th><th>En yakın son kullanma</th></tr></thead>
            <tbody>
              {data.map((row) => (
                <tr key={row.dyeChemicalId}>
                  <td><code>{row.code}</code></td>
                  <td>{row.name}</td>
                  <td>{row.kind === 'DYE' ? 'Boya' : 'Kimyasal'}</td>
                  <td className="table__number">{row.usableKilograms > 0 ? formatQuantity(row.usableKilograms) : <span className="badge badge--warning">Stok yok</span>}</td>
                  <td className="table__number">{row.expiredKilograms > 0 ? <span className="badge badge--danger">{formatQuantity(row.expiredKilograms)}</span> : '—'}</td>
                  <td className="table__number">{row.lotCount}</td>
                  <td>{row.nearestExpiry ? formatDate(row.nearestExpiry) : '—'}</td>
                </tr>
              ))}
              {data.length === 0 && <tr><td colSpan={7} className="table__empty">Boya ya da kimyasal tanımlı değil.</td></tr>}
            </tbody>
          </table>
        </div>
      )}
    </section>
  )
}

function LotDialog({ lot, onClose }: { lot: ChemicalLot | null; onClose: () => void }) {
  const [save, saveState] = stockApi.useSaveChemicalLotMutation()
  const [remove, removeState] = stockApi.useDeleteChemicalLotMutation()
  const [adjust, adjustState] = stockApi.useAdjustChemicalLotMutation()
  const [dyeChemicalId, setDyeChemicalId] = useState<number | null>(lot?.dyeChemicalId ?? null)
  const [receiptDate, setReceiptDate] = useState(lot?.receiptDate ?? todayIsoDate())
  const [kilograms, setKilograms] = useState(lot ? String(lot.receivedKilograms) : '')
  const [supplierLotNo, setSupplierLotNo] = useState(lot?.supplierLotNo ?? '')
  const [supplierId, setSupplierId] = useState<number | null>(lot?.supplierId ?? null)
  const [locationId, setLocationId] = useState<number | null>(null)
  const [expiryDate, setExpiryDate] = useState(lot?.expiryDate ?? '')
  const [unitCost, setUnitCost] = useState(lot?.unitCost === null || lot?.unitCost === undefined ? '' : String(lot.unitCost))
  const [currency, setCurrency] = useState(lot?.currency ?? 'TRY')
  const [notes, setNotes] = useState(lot?.notes ?? '')
  const [counted, setCounted] = useState('')
  const [adjustNote, setAdjustNote] = useState('')

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await save({
      id: lot?.id,
      body: {
        dyeChemicalId: dyeChemicalId ?? 0,
        receiptDate,
        kilograms: Number(kilograms),
        supplierLotNo: supplierLotNo.trim() || null,
        supplierId,
        locationId,
        expiryDate: expiryDate || null,
        unitCost: unitCost.trim() === '' ? null : Number(unitCost),
        currency: currency.trim() || null,
        notes: notes.trim() || null,
      },
    })
    if (!('error' in result)) {
      onClose()
    }
  }

  const handleAdjust = async () => {
    if (lot && !('error' in await adjust({ id: lot.id, countedKilograms: Number(counted), note: adjustNote.trim() }))) {
      onClose()
    }
  }

  const handleDelete = async () => {
    if (lot && !('error' in await remove(lot.id))) {
      onClose()
    }
  }

  return (
    <Modal title={lot ? `Lot: ${lot.lotNo}` : 'Yeni boya / kimyasal girişi'} onClose={onClose}>
      <form className="form" onSubmit={handleSubmit}>
        <LookupSelect resource="boyahane-dye-chemicals" label="Boya / kimyasal" required disabled={lot?.isUsed} value={dyeChemicalId} onChange={setDyeChemicalId} />
        <div className="form__pair">
          <label className="field"><span>Giriş tarihi</span><input type="date" required value={receiptDate} onChange={(event) => setReceiptDate(event.target.value)} /></label>
          <label className="field"><span>Son kullanma</span><input type="date" value={expiryDate} onChange={(event) => setExpiryDate(event.target.value)} /></label>
        </div>
        <div className="form__pair">
          <label className="field"><span>Miktar (kg)</span><input type="number" min="0" step="0.001" required disabled={lot?.isUsed} value={kilograms} onChange={(event) => setKilograms(event.target.value)} /></label>
          <label className="field"><span>Tedarikçi lot no</span><input maxLength={50} value={supplierLotNo} onChange={(event) => setSupplierLotNo(event.target.value)} /></label>
        </div>
        <LookupSelect resource="boyahane-suppliers" label="Tedarikçi" value={supplierId} onChange={setSupplierId} placeholder={lot?.supplierName ?? 'İsteğe bağlı'} />
        {lot?.purchaseOrderNo && <p className="muted">Satın alma siparişi: {lot.purchaseOrderNo}</p>}
        {lot
          ? <p className="muted">Depo adresi: {lot.locationCode ?? '—'} (Adres Stoğu ekranından taşınır)</p>
          : <LookupSelect resource="boyahane-warehouse-locations" label="Depo adresi" value={locationId} onChange={setLocationId} placeholder="Boşsa boya-kimyasal deposunun genel adresi" />}
        <div className="form__pair">
          <label className="field"><span>Birim maliyet (kg)</span><input type="number" min="0" step="0.0001" value={unitCost} onChange={(event) => setUnitCost(event.target.value)} /></label>
          <label className="field"><span>Para birimi</span><input maxLength={3} value={currency} onChange={(event) => setCurrency(event.target.value.toUpperCase())} /></label>
        </div>
        <label className="field"><span>Not</span><input maxLength={500} value={notes} onChange={(event) => setNotes(event.target.value)} /></label>
        <ErrorAlert error={saveState.error ?? removeState.error ?? adjustState.error} />
        <footer className="form__actions">
          {lot && !lot.isUsed && <button type="button" className="button button--danger-ghost" disabled={removeState.isLoading} onClick={handleDelete}>Sil</button>}
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={saveState.isLoading}>Kaydet</button>
        </footer>
      </form>
      {lot && (
        <div className="form form__section">
          <h3 className="section-title">Sayım düzeltmesi <small className="muted">Kalan {formatQuantity(lot.remainingKilograms)} kg</small></h3>
          <div className="form__pair">
            <label className="field"><span>Sayılan (kg)</span><input type="number" min="0" step="0.001" value={counted} onChange={(event) => setCounted(event.target.value)} /></label>
            <label className="field"><span>Gerekçe</span><input maxLength={200} value={adjustNote} onChange={(event) => setAdjustNote(event.target.value)} /></label>
          </div>
          <footer className="form__actions">
            <button type="button" className="button" disabled={adjustState.isLoading || counted.trim() === '' || adjustNote.trim() === ''} onClick={handleAdjust}>Düzelt</button>
          </footer>
        </div>
      )}
    </Modal>
  )
}

/** Boya ve kimyasal lotları, en yeniden eskiye. */
export function ChemicalLotsPage() {
  const canManage = usePermission(Permissions.BoyahaneChemicalStockManage)
  const grid = useGrid()
  const { page, setPage } = grid
  const [input, setInput] = useState('')
  const [search, setSearch] = useState('')
  const [editing, setEditing] = useState<ChemicalLot | null | undefined>(undefined)
  const { data, error, isLoading, isFetching } = stockApi.useGetChemicalLotsQuery({ page, pageSize: PAGE_SIZE, search: search || undefined, ...grid.params })
  const today = todayIsoDate()

  const columns: GridColumn[] = [
    { key: 'lotNo', label: 'Lot no' },
    { key: 'chemicalCode', label: 'Kod' },
    { key: 'chemicalName', label: 'Ad' },
    { key: 'supplierName', label: 'Tedarikçi' },
    { key: 'supplierLotNo', label: 'Tedarikçi lot' },
    { key: 'locationCode', label: 'Adres' },
    { key: 'receiptDate', label: 'Giriş', filter: false },
    { key: 'expiryDate', label: 'Son kullanma', filter: false },
    { key: 'receivedKilograms', label: 'Giren (kg)', numeric: true },
    { key: 'remainingKilograms', label: 'Kalan (kg)', numeric: true },
    { key: 'unitCost', label: 'Birim maliyet', numeric: true, filter: false },
  ]

  const submit = (event: FormEvent) => {
    event.preventDefault()
    setPage(1)
    setSearch(input.trim())
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">Boya Lotları</h1>
      <form className="toolbar" onSubmit={submit} role="search">
        <input type="search" placeholder="Lot, tedarikçi ya da boya" aria-label="Lot ara" value={input} onChange={(event) => setInput(event.target.value)} />
        <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
        <ClearFiltersButton grid={grid} />
        <div className="toolbar__actions">
          {canManage && <button type="button" className="button button--primary" onClick={() => setEditing(null)}><Plus aria-hidden="true" />Yeni giriş</button>}
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
                  <tr key={lot.id} className={canManage ? 'table__row--link' : undefined} onClick={() => canManage && setEditing(lot)}>
                    <td><code>{lot.lotNo}</code></td>
                    <td>{lot.chemicalCode}</td>
                    <td>{lot.chemicalName}</td>
                    <td>{lot.supplierName ?? '—'}</td>
                    <td>{lot.supplierLotNo ?? '—'}</td>
                    <td>{lot.locationCode ?? '—'}</td>
                    <td>{formatDate(lot.receiptDate)}</td>
                    <td>
                      {lot.expiryDate ? formatDate(lot.expiryDate) : '—'}
                      {lot.expiryDate && lot.expiryDate < today && lot.remainingKilograms > 0 && <span className="badge badge--danger">Süresi geçti</span>}
                    </td>
                    <td className="table__number">{formatQuantity(lot.receivedKilograms)}</td>
                    <td className="table__number">{formatQuantity(lot.remainingKilograms)}</td>
                    <td className="table__number">{lot.unitCost === null ? '—' : formatMoney(lot.unitCost, lot.currency)}</td>
                  </tr>
                ))}
                {data.items.length === 0 && <tr><td colSpan={columns.length} className="table__empty">Lot bulunamadı.</td></tr>}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}
      {editing !== undefined && <LotDialog lot={editing} onClose={() => setEditing(undefined)} />}
    </section>
  )
}

/** Lot hareketleri, en yeniden eskiye. */
export function ChemicalMovementsPage() {
  const grid = useGrid()
  const { page, setPage } = grid
  const [input, setInput] = useState('')
  const [search, setSearch] = useState('')
  const { data, error, isLoading, isFetching } = stockApi.useGetChemicalMovementsQuery({ page, pageSize: PAGE_SIZE, search: search || undefined, ...grid.params })

  const columns: GridColumn[] = [
    { key: 'createdAt', label: 'Tarih', filter: false },
    { key: 'lotNo', label: 'Lot' },
    { key: 'chemicalCode', label: 'Kod' },
    { key: 'chemicalName', label: 'Ad' },
    { key: 'type', label: 'Hareket', filter: Object.entries(movementLabels).map(([value, label]) => ({ value, label })) },
    { key: 'kilograms', label: 'Kilo', numeric: true },
    { key: 'reference', label: 'Belge / gerekçe' },
    { key: 'createdBy', label: 'Yapan' },
  ]

  const submit = (event: FormEvent) => {
    event.preventDefault()
    setPage(1)
    setSearch(input.trim())
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">Stok Hareketleri</h1>
      <form className="toolbar" onSubmit={submit} role="search">
        <input type="search" placeholder="Lot, boya ya da parti no" aria-label="Hareket ara" value={input} onChange={(event) => setInput(event.target.value)} />
        <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
        <ClearFiltersButton grid={grid} />
      </form>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {data && (
        <>
          <div className="table-wrap" aria-busy={isFetching}>
            <table className="table">
              <GridHead columns={columns} grid={grid} />
              <tbody>
                {data.items.map((row) => (
                  <tr key={row.id}>
                    <td>{formatDateTime(row.createdAt)}</td>
                    <td><code>{row.lotNo}</code></td>
                    <td>{row.chemicalCode}</td>
                    <td>{row.chemicalName}</td>
                    <td>{movementLabels[row.type]}</td>
                    <td className="table__number">{formatQuantity(row.kilograms)}</td>
                    <td>{row.reference ?? '—'}</td>
                    <td>{row.createdBy ?? '—'}</td>
                  </tr>
                ))}
                {data.items.length === 0 && <tr><td colSpan={columns.length} className="table__empty">Hareket yok.</td></tr>}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}
    </section>
  )
}
