import { CheckCheck, Lock, PackageCheck, Pencil, Plus, RotateCcw, Search, ShoppingCart, X, XCircle } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
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
import { formatDate, formatQuantity, todayIsoDate } from '../../utils/format'
import { formatMoney } from '../../utils/money'
import { LookupSelect } from '../masterData/LookupSelect'

type PurchaseStatus = 'DRAFT' | 'CONFIRMED' | 'PARTIALLY_RECEIVED' | 'RECEIVED' | 'CLOSED' | 'CANCELLED'

const statusPresentation: Record<PurchaseStatus, { label: string; tone: string }> = {
  DRAFT: { label: 'Taslak', tone: 'neutral' },
  CONFIRMED: { label: 'Onaylı', tone: 'info' },
  PARTIALLY_RECEIVED: { label: 'Kısmi teslim', tone: 'warning' },
  RECEIVED: { label: 'Teslim alındı', tone: 'ok' },
  CLOSED: { label: 'Kapatıldı', tone: 'neutral' },
  CANCELLED: { label: 'İptal', tone: 'danger' },
}

interface PurchaseRow {
  id: number
  orderNo: string
  supplierName: string
  orderDate: string
  expectedDate: string | null
  lineCount: number
  orderedKilograms: number
  receivedKilograms: number
  amount: number
  currency: string
  status: PurchaseStatus
}

interface PurchaseLine {
  id: number
  dyeChemicalId: number
  code: string
  name: string
  kilograms: number
  unitPrice: number | null
  amount: number | null
  receivedKilograms: number
  openKilograms: number
  lots: { id: number; lotNo: string; supplierLotNo: string | null; receiptDate: string; expiryDate: string | null; kilograms: number; remainingKilograms: number }[]
}

interface PurchaseOrder {
  id: number
  orderNo: string
  supplierId: number
  supplierName: string
  orderDate: string
  expectedDate: string | null
  currency: string
  notes: string | null
  status: PurchaseStatus
  amount: number
  lines: PurchaseLine[]
}

interface PurchaseRequest {
  supplierId: number
  orderDate: string
  expectedDate: string | null
  currency: string | null
  notes: string | null
  lines: { dyeChemicalId: number; kilograms: number; unitPrice: number | null }[]
}

interface ReceiptRequest {
  receiptDate: string
  locationId: number | null
  lines: { lineId: number; kilograms: number; supplierLotNo: string | null; expiryDate: string | null }[]
}

interface Suggestion {
  dyeChemicalId: number
  code: string
  name: string
  kind: 'DYE' | 'CHEMICAL'
  minStockKilograms: number
  usableKilograms: number
  onOrderKilograms: number
  suggestedKilograms: number
}

const purchaseApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getBoyahanePurchaseOrders: build.query<PagedResult<PurchaseRow>, Record<string, unknown>>({
      query: (params) => ({ url: '/boyahane-purchase-orders', params }),
      providesTags: ['BoyahaneBatch'],
    }),
    getBoyahanePurchaseOrder: build.query<PurchaseOrder, number>({
      query: (id) => `/boyahane-purchase-orders/${id}`,
      providesTags: ['BoyahaneBatch'],
    }),
    getBoyahaneSupplySuggestions: build.query<Suggestion[], void>({
      query: () => '/boyahane-purchase-orders/suggestions',
      providesTags: ['BoyahaneBatch', 'MasterData'],
    }),
    saveBoyahanePurchaseOrder: build.mutation<PurchaseOrder, { id?: number; body: PurchaseRequest }>({
      query: ({ id, body }) => ({ url: id === undefined ? '/boyahane-purchase-orders' : `/boyahane-purchase-orders/${id}`, method: id === undefined ? 'POST' : 'PUT', body }),
      invalidatesTags: ['BoyahaneBatch'],
    }),
    changeBoyahanePurchaseOrder: build.mutation<PurchaseOrder, { id: number; action: 'confirm' | 'reopen' | 'cancel' | 'close' }>({
      query: ({ id, action }) => ({ url: `/boyahane-purchase-orders/${id}/${action}`, method: 'POST' }),
      invalidatesTags: ['BoyahaneBatch'],
    }),
    receiveBoyahanePurchaseOrder: build.mutation<PurchaseOrder, { id: number; body: ReceiptRequest }>({
      query: ({ id, body }) => ({ url: `/boyahane-purchase-orders/${id}/receive`, method: 'POST', body }),
      invalidatesTags: ['BoyahaneBatch'],
    }),
  }),
})

const PAGE_SIZE = 20

interface LineDraft {
  key: number
  dyeChemicalId: number | null
  kilograms: string
  unitPrice: string
}

/** Sipariş formu. Tedarik önerisinden açılınca satırlar önerilen miktarlarla gelir. */
function PurchaseOrderDialog({ order, initialLines, onClose }: { order?: PurchaseOrder; initialLines?: LineDraft[]; onClose: () => void }) {
  const navigate = useNavigate()
  const [save, { isLoading, error }] = purchaseApi.useSaveBoyahanePurchaseOrderMutation()
  const [supplierId, setSupplierId] = useState<number | null>(order?.supplierId ?? null)
  const [orderDate, setOrderDate] = useState(order?.orderDate ?? todayIsoDate())
  const [expectedDate, setExpectedDate] = useState(order?.expectedDate ?? '')
  const [currency, setCurrency] = useState(order?.currency ?? '')
  const [notes, setNotes] = useState(order?.notes ?? '')
  const [lines, setLines] = useState<LineDraft[]>(
    order?.lines.map((line) => ({ key: line.id, dyeChemicalId: line.dyeChemicalId, kilograms: String(line.kilograms), unitPrice: line.unitPrice === null ? '' : String(line.unitPrice) }))
      ?? initialLines
      ?? [{ key: 1, dyeChemicalId: null, kilograms: '', unitPrice: '' }],
  )
  const update = (key: number, patch: Partial<LineDraft>) => setLines(lines.map((line) => (line.key === key ? { ...line, ...patch } : line)))

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await save({
      id: order?.id,
      body: {
        supplierId: supplierId ?? 0,
        orderDate,
        expectedDate: expectedDate || null,
        currency: currency.trim() || null,
        notes: notes.trim() || null,
        lines: lines.map((line) => ({ dyeChemicalId: line.dyeChemicalId ?? 0, kilograms: Number(line.kilograms), unitPrice: line.unitPrice.trim() === '' ? null : Number(line.unitPrice) })),
      },
    })
    if ('data' in result && result.data) {
      if (order) {
        onClose()
      } else {
        navigate(`/boyahane/purchase-orders/${result.data.id}`)
      }
    }
  }

  return (
    <Modal title={order ? `Sipariş: ${order.orderNo}` : 'Yeni satın alma siparişi'} onClose={onClose} variant="drawer">
      <form className="form" onSubmit={handleSubmit}>
        <LookupSelect resource="boyahane-suppliers" label="Tedarikçi" required value={supplierId} onChange={setSupplierId} />
        <div className="form__pair">
          <label className="field"><span>Sipariş tarihi</span><input type="date" required value={orderDate} onChange={(event) => setOrderDate(event.target.value)} /></label>
          <label className="field"><span>Beklenen tarih</span><input type="date" value={expectedDate} onChange={(event) => setExpectedDate(event.target.value)} /></label>
        </div>
        <div className="form__pair">
          <label className="field"><span>Para birimi</span><input maxLength={3} placeholder="Boşsa tedarikçinin" value={currency} onChange={(event) => setCurrency(event.target.value.toUpperCase())} /></label>
          <label className="field"><span>Not</span><input maxLength={1000} value={notes} onChange={(event) => setNotes(event.target.value)} /></label>
        </div>
        <div className="table-wrap">
          <table className="table">
            <thead><tr><th>Boya / kimyasal</th><th className="table__number">Miktar (kg)</th><th className="table__number">Birim fiyat (kg)</th><th aria-label="Sil" /></tr></thead>
            <tbody>
              {lines.map((line) => (
                <tr key={line.key}>
                  <td>
                    <LookupSelect resource="boyahane-dye-chemicals" label="Boya / kimyasal" hideLabel idSuffix={`-${line.key}`} required value={line.dyeChemicalId} onChange={(value) => update(line.key, { dyeChemicalId: value })} />
                  </td>
                  <td className="table__number"><input aria-label="Miktar" className="matrix-input purchase-input" type="number" min="0" step="0.001" required value={line.kilograms} onChange={(event) => update(line.key, { kilograms: event.target.value })} /></td>
                  <td className="table__number"><input aria-label="Birim fiyat" className="matrix-input purchase-input" type="number" min="0" step="0.0001" value={line.unitPrice} onChange={(event) => update(line.key, { unitPrice: event.target.value })} /></td>
                  <td><button type="button" className="button" aria-label="Satırı sil" disabled={lines.length === 1} onClick={() => setLines(lines.filter((row) => row.key !== line.key))}><X aria-hidden="true" /></button></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <div><button type="button" className="button" onClick={() => setLines([...lines, { key: Date.now(), dyeChemicalId: null, kilograms: '', unitPrice: '' }])}><Plus aria-hidden="true" />Satır ekle</button></div>
        <ErrorAlert error={error} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading}>Kaydet</button>
        </footer>
      </form>
    </Modal>
  )
}

/** Satın alma siparişleri, en yeniden eskiye. */
export function PurchaseOrdersPage() {
  const canManage = usePermission(Permissions.BoyahanePurchaseManage)
  const grid = useGrid()
  const { page, setPage } = grid
  const [input, setInput] = useState('')
  const [search, setSearch] = useState('')
  const [creating, setCreating] = useState(false)
  const { data, error, isLoading, isFetching } = purchaseApi.useGetBoyahanePurchaseOrdersQuery({ page, pageSize: PAGE_SIZE, search: search || undefined, ...grid.params })
  const today = todayIsoDate()

  const columns: GridColumn[] = [
    { key: 'orderNo', label: 'Sipariş no' },
    { key: 'supplierName', label: 'Tedarikçi' },
    { key: 'orderDate', label: 'Tarih', filter: false },
    { key: 'expectedDate', label: 'Beklenen', filter: false },
    { key: 'orderedKilograms', label: 'Sipariş (kg)', numeric: true, filter: false },
    { key: 'receivedKilograms', label: 'Teslim (kg)', numeric: true, filter: false },
    { key: 'amount', label: 'Tutar', numeric: true, filter: false },
    { key: 'status', label: 'Durum', filter: Object.entries(statusPresentation).map(([value, { label }]) => ({ value, label })) },
  ]

  const submit = (event: FormEvent) => {
    event.preventDefault()
    setPage(1)
    setSearch(input.trim())
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">Satın Alma Siparişleri</h1>
      <form className="toolbar" onSubmit={submit} role="search">
        <input type="search" placeholder="Sipariş no ya da tedarikçi" aria-label="Sipariş ara" value={input} onChange={(event) => setInput(event.target.value)} />
        <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
        <ClearFiltersButton grid={grid} />
        <div className="toolbar__actions">
          {canManage && <button type="button" className="button button--primary" onClick={() => setCreating(true)}><Plus aria-hidden="true" />Yeni sipariş</button>}
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
                {data.items.map((row) => {
                  const status = statusPresentation[row.status]
                  const late = row.expectedDate && row.expectedDate < today && (row.status === 'CONFIRMED' || row.status === 'PARTIALLY_RECEIVED')
                  return (
                    <tr key={row.id}>
                      <td><Link to={`/boyahane/purchase-orders/${row.id}`}><code>{row.orderNo}</code></Link></td>
                      <td>{row.supplierName}</td>
                      <td>{formatDate(row.orderDate)}</td>
                      <td>{row.expectedDate ? formatDate(row.expectedDate) : '—'}{late && <span className="badge badge--danger">Gecikmiş</span>}</td>
                      <td className="table__number">{formatQuantity(row.orderedKilograms)}</td>
                      <td className="table__number">{formatQuantity(row.receivedKilograms)}</td>
                      <td className="table__number">{row.amount > 0 ? formatMoney(row.amount, row.currency) : '—'}</td>
                      <td><span className={`badge badge--${status.tone}`}>{status.label}</span></td>
                    </tr>
                  )
                })}
                {data.items.length === 0 && <tr><td colSpan={columns.length} className="table__empty">Sipariş bulunamadı.</td></tr>}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}
      {creating && <PurchaseOrderDialog onClose={() => setCreating(false)} />}
    </section>
  )
}

/** Teslim alma: her satır depoya ayrı lot olarak girer. */
function ReceiveDialog({ order, onClose }: { order: PurchaseOrder; onClose: () => void }) {
  const [receive, { isLoading, error }] = purchaseApi.useReceiveBoyahanePurchaseOrderMutation()
  const [receiptDate, setReceiptDate] = useState(todayIsoDate())
  const [locationId, setLocationId] = useState<number | null>(null)
  const lines = order.lines.filter((line) => line.openKilograms > 0)
  const [rows, setRows] = useState<Record<number, { kilograms: string; supplierLotNo: string; expiryDate: string }>>(
    () => Object.fromEntries(lines.map((line) => [line.id, { kilograms: String(line.openKilograms), supplierLotNo: '', expiryDate: '' }])),
  )
  const update = (id: number, patch: Partial<{ kilograms: string; supplierLotNo: string; expiryDate: string }>) => setRows({ ...rows, [id]: { ...rows[id], ...patch } })

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await receive({
      id: order.id,
      body: {
        receiptDate,
        locationId,
        lines: lines
          .filter((line) => Number(rows[line.id].kilograms) > 0)
          .map((line) => ({
            lineId: line.id,
            kilograms: Number(rows[line.id].kilograms),
            supplierLotNo: rows[line.id].supplierLotNo.trim() || null,
            expiryDate: rows[line.id].expiryDate || null,
          })),
      },
    })
    if (!('error' in result)) {
      onClose()
    }
  }

  return (
    <Modal title={`Teslim al: ${order.orderNo}`} onClose={onClose} variant="drawer">
      <form className="form" onSubmit={handleSubmit}>
        <div className="form__pair">
          <label className="field"><span>Teslim tarihi</span><input type="date" required value={receiptDate} onChange={(event) => setReceiptDate(event.target.value)} /></label>
          <LookupSelect resource="boyahane-warehouse-locations" label="Depo adresi" value={locationId} onChange={setLocationId} placeholder="Boşsa boya-kimyasal genel adresi" />
        </div>
        <div className="table-wrap batch-final">
          <table className="table">
            <thead><tr><th>Boya / kimyasal</th><th className="table__number">Kalan</th><th>Gelen (kg)</th><th>Tedarikçi lot</th><th>Son kullanma</th></tr></thead>
            <tbody>
              {lines.map((line) => (
                <tr key={line.id}>
                  <td><code>{line.code}</code> {line.name}</td>
                  <td className="table__number">{formatQuantity(line.openKilograms)}</td>
                  <td><input className="matrix-input purchase-input" aria-label={`${line.code} gelen`} type="number" min="0" step="0.001" value={rows[line.id].kilograms} onChange={(event) => update(line.id, { kilograms: event.target.value })} /></td>
                  <td><input className="matrix-input purchase-input" aria-label={`${line.code} tedarikçi lot`} maxLength={50} value={rows[line.id].supplierLotNo} onChange={(event) => update(line.id, { supplierLotNo: event.target.value })} /></td>
                  <td><input aria-label={`${line.code} son kullanma`} className="matrix-input purchase-date" type="date" value={rows[line.id].expiryDate} onChange={(event) => update(line.id, { expiryDate: event.target.value })} /></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <p className="muted">Gelmeyen satırı boş ya da sıfır bırakın. Her satır depoya ayrı lot olarak girer; kalandan fazlası da kabul edilir.</p>
        <ErrorAlert error={error} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading}>Teslim al</button>
        </footer>
      </form>
    </Modal>
  )
}

export function PurchaseOrderDetailPage() {
  const id = Number(useParams().id)
  const canManage = usePermission(Permissions.BoyahanePurchaseManage)
  const { data: order, error, isLoading } = purchaseApi.useGetBoyahanePurchaseOrderQuery(id)
  const [change, changeState] = purchaseApi.useChangeBoyahanePurchaseOrderMutation()
  const [editing, setEditing] = useState(false)
  const [receiving, setReceiving] = useState(false)
  const status = order && statusPresentation[order.status]

  return (
    <section className="page">
      <p><Link to="/boyahane/purchase-orders">← Siparişlere dön</Link></p>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error ?? changeState.error} />
      {order && status && (
        <>
          <header className="page__header">
            <div>
              <h1>{order.orderNo} <span className={`badge badge--${status.tone}`}>{status.label}</span></h1>
              <p className="muted">{order.supplierName} · {formatDate(order.orderDate)}{order.expectedDate && ` · Beklenen ${formatDate(order.expectedDate)}`}</p>
              {order.notes && <p>{order.notes}</p>}
            </div>
            {canManage && (
              <div className="page__actions">
                {order.status === 'DRAFT' && <button type="button" className="button" onClick={() => setEditing(true)}><Pencil aria-hidden="true" />Düzenle</button>}
                {order.status === 'DRAFT' && <button type="button" className="button button--primary" disabled={changeState.isLoading} onClick={() => change({ id, action: 'confirm' })}><CheckCheck aria-hidden="true" />Onayla</button>}
                {(order.status === 'CONFIRMED' || order.status === 'PARTIALLY_RECEIVED') && <button type="button" className="button button--primary" onClick={() => setReceiving(true)}><PackageCheck aria-hidden="true" />Teslim al</button>}
                {order.status === 'CONFIRMED' && <button type="button" className="button" disabled={changeState.isLoading} onClick={() => change({ id, action: 'reopen' })}><RotateCcw aria-hidden="true" />Taslağa al</button>}
                {order.status === 'PARTIALLY_RECEIVED' && <button type="button" className="button" disabled={changeState.isLoading} onClick={() => change({ id, action: 'close' })}><Lock aria-hidden="true" />Kalanı kapat</button>}
                {(order.status === 'DRAFT' || order.status === 'CONFIRMED') && <button type="button" className="button button--danger-ghost" disabled={changeState.isLoading} onClick={() => change({ id, action: 'cancel' })}><XCircle aria-hidden="true" />İptal et</button>}
              </div>
            )}
          </header>
          <div className="table-wrap">
            <table className="table">
              <thead><tr><th>Kod</th><th>Ad</th><th className="table__number">Sipariş (kg)</th><th className="table__number">Teslim (kg)</th><th className="table__number">Kalan (kg)</th><th className="table__number">Birim fiyat</th><th className="table__number">Tutar</th><th>Lotlar</th></tr></thead>
              <tbody>
                {order.lines.map((line) => (
                  <tr key={line.id}>
                    <td><code>{line.code}</code></td>
                    <td>{line.name}</td>
                    <td className="table__number">{formatQuantity(line.kilograms)}</td>
                    <td className="table__number">{formatQuantity(line.receivedKilograms)}</td>
                    <td className="table__number">{formatQuantity(line.openKilograms)}</td>
                    <td className="table__number">{line.unitPrice === null ? '—' : formatMoney(line.unitPrice, order.currency)}</td>
                    <td className="table__number">{line.amount === null ? '—' : formatMoney(line.amount, order.currency)}</td>
                    <td>{line.lots.map((lot) => `${lot.lotNo} (${formatQuantity(lot.kilograms)} kg)`).join(', ') || '—'}</td>
                  </tr>
                ))}
              </tbody>
              <tfoot>
                <tr><th colSpan={6} scope="row">Toplam</th><td className="table__number">{order.amount > 0 ? formatMoney(order.amount, order.currency) : '—'}</td><td /></tr>
              </tfoot>
            </table>
          </div>
          {editing && <PurchaseOrderDialog order={order} onClose={() => setEditing(false)} />}
          {receiving && <ReceiveDialog order={order} onClose={() => setReceiving(false)} />}
        </>
      )}
    </section>
  )
}

/** Tedarik önerileri: kritik stok altına düşen boya ve kimyasallar; yoldaki sipariş düşülür. */
export function SupplySuggestionsPage() {
  const canManage = usePermission(Permissions.BoyahanePurchaseManage)
  const { data, error, isLoading } = purchaseApi.useGetBoyahaneSupplySuggestionsQuery()
  const [selected, setSelected] = useState<Set<number>>(new Set())
  const [creating, setCreating] = useState(false)
  const lines = (data ?? []).filter((row) => selected.has(row.dyeChemicalId))
    .map((row) => ({ key: row.dyeChemicalId, dyeChemicalId: row.dyeChemicalId, kilograms: String(row.suggestedKilograms), unitPrice: '' }))

  const toggle = (id: number, checked: boolean) => {
    const next = new Set(selected)
    if (checked) {
      next.add(id)
    } else {
      next.delete(id)
    }
    setSelected(next)
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">Tedarik Önerileri</h1>
      <div className="toolbar">
        <span className="muted">Kritik stok, boya ve kimyasal kartında tanımlanır.</span>
        <div className="toolbar__actions">
          {canManage && <button type="button" className="button button--primary" disabled={lines.length === 0} onClick={() => setCreating(true)}><ShoppingCart aria-hidden="true" />Sipariş oluştur ({lines.length})</button>}
        </div>
      </div>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {data && (
        <div className="table-wrap">
          <table className="table">
            <thead><tr><th aria-label="Seç" /><th>Kod</th><th>Ad</th><th className="table__number">Kritik stok</th><th className="table__number">Kullanılabilir</th><th className="table__number">Yolda</th><th className="table__number">Önerilen (kg)</th></tr></thead>
            <tbody>
              {data.map((row) => (
                <tr key={row.dyeChemicalId}>
                  <td><input type="checkbox" aria-label={`${row.code} seç`} checked={selected.has(row.dyeChemicalId)} onChange={(event) => toggle(row.dyeChemicalId, event.target.checked)} /></td>
                  <td><code>{row.code}</code></td>
                  <td>{row.name}</td>
                  <td className="table__number">{formatQuantity(row.minStockKilograms)}</td>
                  <td className="table__number">{formatQuantity(row.usableKilograms)}</td>
                  <td className="table__number">{formatQuantity(row.onOrderKilograms)}</td>
                  <td className="table__number"><strong>{formatQuantity(row.suggestedKilograms)}</strong></td>
                </tr>
              ))}
              {data.length === 0 && <tr><td colSpan={7} className="table__empty">Kritik stok altında boya ya da kimyasal yok.</td></tr>}
            </tbody>
          </table>
        </div>
      )}
      {creating && <PurchaseOrderDialog initialLines={lines} onClose={() => setCreating(false)} />}
    </section>
  )
}
