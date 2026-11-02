import { CheckCheck, Lock, PackageCheck, Pencil, Plus, Printer, RotateCcw, Search, ShoppingCart, Trash2, X, XCircle } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { ClearFiltersButton } from '../../components/grid/ClearFiltersButton'
import { GridHead } from '../../components/grid/GridHead'
import type { GridColumn } from '../../components/grid/GridHead'
import { useGrid } from '../../components/grid/useGrid'
import { Modal } from '../../components/Modal'
import { Pagination } from '../../components/Pagination'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDate, formatDateTime, formatQuantity, todayIsoDate } from '../../utils/format'
import { formatMoney } from '../../utils/money'
import { LookupSelect } from '../masterData/LookupSelect'
import { unitLabel } from './dokumaPresentation'
import {
  dokumaPurchaseItemLabels,
  dokumaPurchaseStatusPresentation,
  useChangeDokumaPurchaseOrderMutation,
  useCreateDokumaPurchaseDraftsMutation,
  useDeleteDokumaPurchaseOrderMutation,
  useGetDokumaPurchaseOrderQuery,
  useGetDokumaPurchaseOrdersQuery,
  useGetDokumaSupplySuggestionsQuery,
  useReceiveDokumaPurchaseOrderMutation,
  useSaveDokumaPurchaseOrderMutation,
} from './dokumaPurchasingApi'
import type { DokumaPurchaseDraft, DokumaPurchaseItemKind, DokumaPurchaseOrder } from './dokumaPurchasingApi'

const PAGE_SIZE = 20

interface LineDraft {
  key: number
  kind: DokumaPurchaseItemKind
  itemId: number | null
  /** Seçilen malzemenin birimi (seçimdeki ek bilgiden); iplikte kg. */
  unit: string
  quantity: string
  unitPrice: string
  dueDate: string
}

const emptyLine = (key: number): LineDraft => ({ key, kind: 'YARN', itemId: null, unit: 'kg', quantity: '', unitPrice: '', dueDate: '' })

/** Satın alma siparişi formu: satır iplik (kg) ya da malzeme (kartındaki birim) olur. */
function PurchaseOrderDialog({ order, onClose }: { order?: DokumaPurchaseOrder; onClose: () => void }) {
  const navigate = useNavigate()
  const [save, { isLoading, error }] = useSaveDokumaPurchaseOrderMutation()
  const [supplierId, setSupplierId] = useState<number | null>(order?.supplierId ?? null)
  const [orderDate, setOrderDate] = useState(order?.orderDate ?? todayIsoDate())
  const [dueDate, setDueDate] = useState(order?.dueDate ?? '')
  const [currency, setCurrency] = useState(order?.currency ?? '')
  const [notes, setNotes] = useState(order?.notes ?? '')
  const [lines, setLines] = useState<LineDraft[]>(
    order?.lines.map((line) => ({
      key: line.id,
      kind: line.kind,
      itemId: line.yarnId ?? line.materialId,
      unit: unitLabel(line.unit),
      quantity: String(line.quantity),
      unitPrice: line.unitPrice === null ? '' : String(line.unitPrice),
      dueDate: line.dueDate && line.dueDate !== order?.dueDate ? line.dueDate : '',
    })) ?? [emptyLine(1)],
  )
  const update = (key: number, patch: Partial<LineDraft>) => setLines((current) => current.map((line) => (line.key === key ? { ...line, ...patch } : line)))

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await save({
      id: order?.id,
      body: {
        supplierId: supplierId ?? 0,
        orderDate,
        dueDate: dueDate || null,
        currency: currency.trim() || null,
        notes: notes.trim() || null,
        lines: lines.map((line) => ({
          yarnId: line.kind === 'YARN' ? line.itemId : null,
          materialId: line.kind === 'MATERIAL' ? line.itemId : null,
          quantity: Number(line.quantity),
          unitPrice: line.unitPrice.trim() === '' ? null : Number(line.unitPrice),
          dueDate: line.dueDate || null,
          notes: null,
        })),
      },
    })
    if ('data' in result && result.data) {
      if (order) {
        onClose()
      } else {
        navigate(`/dokuma/purchase-orders/${result.data.id}`)
      }
    }
  }

  return (
    <Modal title={order ? `Satın alma: ${order.orderNo}` : 'Yeni satın alma siparişi'} onClose={onClose} variant="drawer">
      <form className="form" onSubmit={handleSubmit}>
        <LookupSelect resource="dokuma-suppliers" label="Tedarikçi" required value={supplierId} onChange={setSupplierId} />
        <div className="form__pair">
          <label className="field"><span>Sipariş tarihi</span><input type="date" required value={orderDate} onChange={(event) => setOrderDate(event.target.value)} /></label>
          <label className="field"><span>Termin</span><input type="date" value={dueDate} onChange={(event) => setDueDate(event.target.value)} /><small className="field__hint">Boşsa tedarikçinin teslim süresinden hesaplanır.</small></label>
        </div>
        <div className="form__pair">
          <label className="field"><span>Para birimi</span><input maxLength={3} placeholder="Boşsa tedarikçinin" value={currency} onChange={(event) => setCurrency(event.target.value.toUpperCase())} /></label>
          <label className="field"><span>Not</span><input maxLength={1000} value={notes} onChange={(event) => setNotes(event.target.value)} /></label>
        </div>
        <div className="table-wrap">
          <table className="table">
            <thead>
              <tr><th>Tür</th><th>Kalem</th><th className="table__number">Miktar</th><th>Birim</th><th className="table__number">Birim fiyat</th><th>Satır termini</th><th aria-label="Sil" /></tr>
            </thead>
            <tbody>
              {lines.map((line) => (
                <tr key={line.key}>
                  <td>
                    <select aria-label="Kalem türü" value={line.kind} onChange={(event) => {
                      const kind = event.target.value as DokumaPurchaseItemKind
                      update(line.key, { kind, itemId: null, unit: kind === 'YARN' ? 'kg' : '' })
                    }}>
                      {Object.entries(dokumaPurchaseItemLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
                    </select>
                  </td>
                  <td style={{ minWidth: '16rem' }}>
                    <LookupSelect
                      key={line.kind}
                      resource={line.kind === 'YARN' ? 'dokuma-yarns' : 'dokuma-materials'}
                      label={line.kind === 'YARN' ? 'İplik' : 'Malzeme'}
                      hideLabel
                      idSuffix={`-${line.key}`}
                      required
                      value={line.itemId}
                      onChange={(value) => update(line.key, { itemId: value })}
                      onSelectItem={(item) => { if (line.kind === 'MATERIAL') update(line.key, { unit: item?.detail ?? '' }) }}
                    />
                  </td>
                  <td className="table__number"><input aria-label="Miktar" className="matrix-input purchase-input" type="number" min="0" step={line.kind === 'YARN' ? '0.01' : '0.001'} required value={line.quantity} onChange={(event) => update(line.key, { quantity: event.target.value })} /></td>
                  <td>{line.unit || '—'}</td>
                  <td className="table__number"><input aria-label="Birim fiyat" className="matrix-input purchase-input" type="number" min="0" step="0.0001" value={line.unitPrice} onChange={(event) => update(line.key, { unitPrice: event.target.value })} /></td>
                  <td><input aria-label="Satır termini" className="matrix-input purchase-date" type="date" value={line.dueDate} onChange={(event) => update(line.key, { dueDate: event.target.value })} /></td>
                  <td><button type="button" className="button" aria-label="Satırı sil" disabled={lines.length === 1} onClick={() => setLines(lines.filter((row) => row.key !== line.key))}><X aria-hidden="true" /></button></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <div><button type="button" className="button" onClick={() => setLines([...lines, emptyLine(Date.now())])}><Plus aria-hidden="true" />Satır ekle</button></div>
        <ErrorAlert error={error} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading}>Kaydet</button>
        </footer>
      </form>
    </Modal>
  )
}

/** İplik ve malzeme satın alma siparişleri, en yeniden eskiye. */
export function DokumaPurchaseOrdersPage() {
  const canManage = usePermission(Permissions.DokumaPurchaseOrderManage)
  const grid = useGrid()
  const { page, setPage } = grid
  const [input, setInput] = useState('')
  const [search, setSearch] = useState('')
  const [creating, setCreating] = useState(false)
  const { data, error, isLoading, isFetching } = useGetDokumaPurchaseOrdersQuery({ page, pageSize: PAGE_SIZE, search: search || undefined, ...grid.params })
  const today = todayIsoDate()

  const columns: GridColumn[] = [
    { key: 'orderNo', label: 'Sipariş no' },
    { key: 'supplierName', label: 'Tedarikçi' },
    { key: 'orderDate', label: 'Tarih', filter: false },
    { key: 'dueDate', label: 'Termin', filter: false },
    { key: 'lineCount', label: 'Satır', numeric: true, filter: false },
    { key: 'openLineCount', label: 'Açık satır', numeric: true, filter: false },
    { key: 'amount', label: 'Tutar', numeric: true, filter: false },
    { key: 'status', label: 'Durum', filter: Object.entries(dokumaPurchaseStatusPresentation).map(([value, { label }]) => ({ value, label })) },
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
                  const status = dokumaPurchaseStatusPresentation[row.status]
                  const late = row.dueDate && row.dueDate < today && (row.status === 'APPROVED' || row.status === 'PARTIALLY_RECEIVED')
                  return (
                    <tr key={row.id}>
                      <td><Link to={`/dokuma/purchase-orders/${row.id}`}><code>{row.orderNo}</code></Link></td>
                      <td>{row.supplierName}</td>
                      <td>{formatDate(row.orderDate)}</td>
                      <td>{row.dueDate ? formatDate(row.dueDate) : '—'}{late && <> <span className="badge badge--danger">Gecikmiş</span></>}</td>
                      <td className="table__number">{row.lineCount}</td>
                      <td className="table__number">{row.openLineCount}</td>
                      <td className="table__number">{row.amount > 0 ? formatMoney(row.amount, row.currency) : '—'}</td>
                      <td><span className={`badge badge--${status.tone}`}>{status.label}</span></td>
                    </tr>
                  )
                })}
                {data.items.length === 0 && <tr><td colSpan={columns.length} className="table__empty">Satın alma siparişi bulunamadı.</td></tr>}
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

interface ReceiptRow {
  quantity: string
  lotNo: string
  packageCount: string
}

/** Teslim alma: iplik satırı işletme malı iplik lotu, malzeme satırı malzeme lotu olarak girer. */
function ReceiveDialog({ order, onClose }: { order: DokumaPurchaseOrder; onClose: () => void }) {
  const [receive, { isLoading, error }] = useReceiveDokumaPurchaseOrderMutation()
  const [receiptDate, setReceiptDate] = useState(todayIsoDate())
  const [despatchNo, setDespatchNo] = useState('')
  const lines = order.lines.filter((line) => line.openQuantity > 0)
  const [rows, setRows] = useState<Record<number, ReceiptRow>>(
    () => Object.fromEntries(lines.map((line) => [line.id, { quantity: String(line.openQuantity), lotNo: '', packageCount: '' }])),
  )
  const update = (id: number, patch: Partial<ReceiptRow>) => setRows({ ...rows, [id]: { ...rows[id], ...patch } })

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await receive({
      id: order.id,
      body: {
        receiptDate,
        despatchNo: despatchNo.trim() || null,
        lines: lines
          .filter((line) => Number(rows[line.id].quantity) > 0)
          .map((line) => ({
            lineId: line.id,
            quantity: Number(rows[line.id].quantity),
            lotNo: rows[line.id].lotNo.trim() || null,
            packageCount: line.kind === 'YARN' ? Number(rows[line.id].packageCount) || null : null,
            notes: null,
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
          <label className="field"><span>İrsaliye no</span><input maxLength={50} value={despatchNo} onChange={(event) => setDespatchNo(event.target.value)} /></label>
        </div>
        <div className="table-wrap">
          <table className="table">
            <thead><tr><th>Kalem</th><th className="table__number">Kalan</th><th>Gelen</th><th>Lot no</th><th>Bobin</th></tr></thead>
            <tbody>
              {lines.map((line) => (
                <tr key={line.id}>
                  <td><span className="muted">{dokumaPurchaseItemLabels[line.kind]}</span> <code>{line.itemCode}</code> {line.itemName}</td>
                  <td className="table__number">{formatQuantity(line.openQuantity)} {unitLabel(line.unit)}</td>
                  <td><input className="matrix-input purchase-input" aria-label={`${line.itemCode} gelen`} type="number" min="0" step={line.kind === 'YARN' ? '0.01' : '0.001'} value={rows[line.id].quantity} onChange={(event) => update(line.id, { quantity: event.target.value })} /></td>
                  <td><input className="matrix-input purchase-input" aria-label={`${line.itemCode} lot no`} maxLength={50} placeholder={line.kind === 'YARN' ? 'Üretici lot' : 'Tedarikçi lot'} value={rows[line.id].lotNo} onChange={(event) => update(line.id, { lotNo: event.target.value })} /></td>
                  <td>
                    {line.kind === 'YARN'
                      ? <input className="matrix-input purchase-input" aria-label={`${line.itemCode} bobin`} type="number" min="1" step="1" required={Number(rows[line.id].quantity) > 0} value={rows[line.id].packageCount} onChange={(event) => update(line.id, { packageCount: event.target.value })} />
                      : '—'}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <p className="muted">Gelmeyen satırı boş ya da sıfır bırakın. Her satır ayrı lot olarak girer: iplik İplik Girişleri'ne işletme malı olarak, malzeme Malzeme Stoğu'na. Kalandan fazlası da kabul edilir.</p>
        <ErrorAlert error={error} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading}>Teslim al</button>
        </footer>
      </form>
    </Modal>
  )
}

export function DokumaPurchaseOrderDetailPage() {
  const id = Number(useParams().id)
  const navigate = useNavigate()
  const canManage = usePermission(Permissions.DokumaPurchaseOrderManage)
  const { data: order, error, isLoading } = useGetDokumaPurchaseOrderQuery(id)
  const [change, changeState] = useChangeDokumaPurchaseOrderMutation()
  const [remove, removeState] = useDeleteDokumaPurchaseOrderMutation()
  const [editing, setEditing] = useState(false)
  const [receiving, setReceiving] = useState(false)
  const [confirmDelete, setConfirmDelete] = useState(false)
  const status = order && dokumaPurchaseStatusPresentation[order.status]
  const busy = changeState.isLoading || removeState.isLoading
  const received = order?.lines.some((line) => line.receivedQuantity > 0) ?? false

  const handleDelete = async () => {
    if (!('error' in await remove(id))) {
      navigate('/dokuma/purchase-orders')
    }
  }

  return (
    <section className="page">
      <p><Link to="/dokuma/purchase-orders">← Siparişlere dön</Link></p>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error ?? changeState.error ?? removeState.error} />
      {order && status && (
        <>
          <header className="page__header">
            <div>
              <h1>{order.orderNo} <span className={`badge badge--${status.tone}`}>{status.label}</span></h1>
              <p className="muted">
                {order.supplierName} · {formatDate(order.orderDate)}{order.dueDate && ` · Termin ${formatDate(order.dueDate)}`}
                {order.approvedAt && ` · Onay: ${order.approvedBy ?? ''} ${formatDateTime(order.approvedAt)}`}
              </p>
              {order.notes && <p>{order.notes}</p>}
            </div>
            <div className="page__actions">
              <Link className="button" to={`/dokuma/purchase-orders/${order.id}/print`}><Printer aria-hidden="true" />Yazdır</Link>
              {canManage && (
                <>
                  {order.status === 'DRAFT' && <button type="button" className="button" onClick={() => setEditing(true)}><Pencil aria-hidden="true" />Düzenle</button>}
                  {order.status === 'DRAFT' && <button type="button" className="button button--primary" disabled={busy} onClick={() => change({ id, action: 'approve' })}><CheckCheck aria-hidden="true" />Onayla</button>}
                  {(order.status === 'APPROVED' || order.status === 'PARTIALLY_RECEIVED') && <button type="button" className="button button--primary" onClick={() => setReceiving(true)}><PackageCheck aria-hidden="true" />Teslim al</button>}
                  {order.status === 'APPROVED' && !received && <button type="button" className="button" disabled={busy} onClick={() => change({ id, action: 'reopen' })}><RotateCcw aria-hidden="true" />Taslağa al</button>}
                  {order.status === 'PARTIALLY_RECEIVED' && <button type="button" className="button" disabled={busy} onClick={() => change({ id, action: 'close' })}><Lock aria-hidden="true" />Kalanı kapat</button>}
                  {order.status === 'APPROVED' && !received && <button type="button" className="button button--danger-ghost" disabled={busy} onClick={() => change({ id, action: 'cancel' })}><XCircle aria-hidden="true" />İptal et</button>}
                  {order.status === 'DRAFT' && !confirmDelete && <button type="button" className="button button--danger-ghost" onClick={() => setConfirmDelete(true)}><Trash2 aria-hidden="true" />Sil</button>}
                  {order.status === 'DRAFT' && confirmDelete && <button type="button" className="button button--danger-ghost" disabled={busy} onClick={handleDelete}>Silmeyi onayla</button>}
                </>
              )}
            </div>
          </header>
          <div className="table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <th>#</th><th>Tür</th><th>Kalem</th><th className="table__number">Sipariş</th><th className="table__number">Teslim</th><th className="table__number">Kalan</th>
                  <th className="table__number">Birim fiyat</th><th className="table__number">Tutar</th><th>Termin</th><th>Lotlar</th>
                </tr>
              </thead>
              <tbody>
                {order.lines.map((line) => {
                  const unit = unitLabel(line.unit)
                  return (
                    <tr key={line.id}>
                      <td>{line.sequence}</td>
                      <td>{dokumaPurchaseItemLabels[line.kind]}</td>
                      <td><code>{line.itemCode}</code> {line.itemName}{line.itemDetail && <span className="muted"> ({line.itemDetail})</span>}</td>
                      <td className="table__number">{formatQuantity(line.quantity)} {unit}</td>
                      <td className="table__number">{formatQuantity(line.receivedQuantity)} {unit}</td>
                      <td className="table__number">{formatQuantity(line.openQuantity)} {unit}</td>
                      <td className="table__number">{line.unitPrice === null ? '—' : formatMoney(line.unitPrice, order.currency)}</td>
                      <td className="table__number">{line.amount === null ? '—' : formatMoney(line.amount, order.currency)}</td>
                      <td>{line.dueDate ? formatDate(line.dueDate) : '—'}</td>
                      <td>{line.lots.map((lot) => `${lot.lotNo} (${formatQuantity(lot.quantity)} ${unit})`).join(', ') || '—'}</td>
                    </tr>
                  )
                })}
              </tbody>
              <tfoot>
                <tr><th colSpan={7} scope="row">Toplam</th><td className="table__number">{order.amount > 0 ? formatMoney(order.amount, order.currency) : '—'}</td><td colSpan={2} /></tr>
              </tfoot>
            </table>
          </div>
          {order.hasMissingPrices && <p className="muted">Fiyatı girilmemiş satır var; toplam eksik görünür.</p>}
          {editing && <PurchaseOrderDialog order={order} onClose={() => setEditing(false)} />}
          {receiving && <ReceiveDialog order={order} onClose={() => setReceiving(false)} />}
        </>
      )}
    </section>
  )
}

interface SuggestionEdit {
  quantity: string
  supplierId: number | null
}

/**
 * Tedarik önerileri: kritik stok altındaki malzemeler ve onaylı kendi malı siparişlerin iplik eksikleri; açık siparişlerde
 * (taslak dahil) bekleyen düşülür. Seçilenlerden tedarikçi başına taslak sipariş açılır.
 */
export function DokumaSupplySuggestionsPage() {
  const canManage = usePermission(Permissions.DokumaPurchaseOrderManage)
  const { data, error, isLoading, isFetching } = useGetDokumaSupplySuggestionsQuery()
  const [create, createState] = useCreateDokumaPurchaseDraftsMutation()
  const [selected, setSelected] = useState<Set<string>>(new Set())
  const [edits, setEdits] = useState<Record<string, SuggestionEdit>>({})
  const [created, setCreated] = useState<DokumaPurchaseDraft[] | null>(null)
  const rows = data ?? []
  const keyOf = (row: { kind: string; itemId: number }) => `${row.kind}-${row.itemId}`
  const editOf = (row: (typeof rows)[number]): SuggestionEdit => edits[keyOf(row)] ?? { quantity: String(row.suggestedQuantity), supplierId: row.supplierId }
  const chosen = rows.filter((row) => selected.has(keyOf(row)))
  const missingSupplier = chosen.some((row) => editOf(row).supplierId === null)

  const toggle = (key: string, checked: boolean) => {
    const next = new Set(selected)
    if (checked) {
      next.add(key)
    } else {
      next.delete(key)
    }
    setSelected(next)
  }

  const handleCreate = async () => {
    const result = await create({
      orderDate: null,
      lines: chosen.map((row) => ({ kind: row.kind, itemId: row.itemId, quantity: Number(editOf(row).quantity), supplierId: editOf(row).supplierId ?? 0 })),
    })
    if ('data' in result && result.data) {
      setCreated(result.data)
      setSelected(new Set())
      setEdits({})
    }
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">Tedarik Önerileri</h1>
      <div className="toolbar">
        <span className="muted">Malzemede kritik stok, malzeme kartında; iplikte ihtiyaç, onaylı kendi malı siparişlerden hesaplanır. Fason siparişin ipliği müşteriden gelir.</span>
        <div className="toolbar__actions">
          {canManage && (
            <button type="button" className="button button--primary" disabled={chosen.length === 0 || missingSupplier || createState.isLoading} onClick={handleCreate}>
              <ShoppingCart aria-hidden="true" />Taslak sipariş oluştur ({chosen.length})
            </button>
          )}
        </div>
      </div>
      {missingSupplier && <p className="alert alert--info">Seçilen her öneri için tedarikçi seçin; siparişler tedarikçi başına açılır.</p>}
      {created && (
        <div className="alert alert--info">
          Taslak siparişler oluşturuldu:{' '}
          {created.map((order, index) => (
            <span key={order.id}>{index > 0 && ', '}<Link to={`/dokuma/purchase-orders/${order.id}`}>{order.orderNo}</Link> ({order.supplierName}, {order.lineCount} satır)</span>
          ))}
        </div>
      )}
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error ?? createState.error} />
      {data && (
        <div className="table-wrap" aria-busy={isFetching}>
          <table className="table">
            <thead>
              <tr>
                <th aria-label="Seç" /><th>Tür</th><th>Kod</th><th>Ad</th><th className="table__number">İhtiyaç / kritik</th><th className="table__number">Eldeki</th>
                <th className="table__number">Siparişte</th><th className="table__number">Önerilen</th><th>Tedarikçi</th><th>Siparişler</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((row) => {
                const key = keyOf(row)
                const edit = editOf(row)
                const unit = unitLabel(row.unit)
                return (
                  <tr key={key}>
                    <td><input type="checkbox" aria-label={`${row.code} seç`} disabled={!canManage} checked={selected.has(key)} onChange={(event) => toggle(key, event.target.checked)} /></td>
                    <td>{dokumaPurchaseItemLabels[row.kind]}</td>
                    <td><code>{row.code}</code></td>
                    <td>{row.name}</td>
                    <td className="table__number">{formatQuantity(row.requiredQuantity)} {unit}</td>
                    <td className="table__number">{formatQuantity(row.availableQuantity)} {unit}</td>
                    <td className="table__number">{row.onOrderQuantity > 0 ? `${formatQuantity(row.onOrderQuantity)} ${unit}` : '—'}</td>
                    <td className="table__number">
                      {canManage
                        ? <input aria-label={`${row.code} önerilen`} className="matrix-input purchase-input" type="number" min="0" step="0.001" value={edit.quantity} onChange={(event) => setEdits({ ...edits, [key]: { ...edit, quantity: event.target.value } })} />
                        : <strong>{formatQuantity(row.suggestedQuantity)}</strong>}
                      {' '}{unit}
                    </td>
                    <td style={{ minWidth: '14rem' }}>
                      {canManage
                        ? <LookupSelect resource="dokuma-suppliers" label="Tedarikçi" hideLabel idSuffix={`-${key}`} value={edit.supplierId} onChange={(value) => setEdits({ ...edits, [key]: { ...edit, supplierId: value } })} placeholder="Tedarikçi seçin" />
                        : row.supplierName ?? '—'}
                    </td>
                    <td>{row.orderNos.join(', ') || '—'}</td>
                  </tr>
                )
              })}
              {rows.length === 0 && <tr><td colSpan={10} className="table__empty">Kritik stok altında malzeme ya da eksik iplik yok.</td></tr>}
            </tbody>
          </table>
        </div>
      )}
    </section>
  )
}

/** Yazdırılabilir satın alma sipariş formu. Antet alanı boş bırakılır; antetli kâğıda basılır. */
export function DokumaPurchaseOrderPrintPage() {
  const id = Number(useParams().id)
  const { data, isLoading, error } = useGetDokumaPurchaseOrderQuery(id)

  if (!data) {
    return (
      <section className="page">
        <p><Link to={`/dokuma/purchase-orders/${id}`}>← Siparişe dön</Link></p>
        {isLoading && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={error} />
      </section>
    )
  }

  const money = (value: number) => formatMoney(value, data.currency)
  const draft = data.status === 'DRAFT'

  return (
    <section className="page proforma-page">
      <div className="no-print proforma-page__bar">
        <Link to={`/dokuma/purchase-orders/${id}`}>← Siparişe dön</Link>
        <span className="muted">Yazdır penceresinde hedef olarak &quot;PDF olarak kaydet&quot; seçilirse PDF dosyası oluşur.</span>
        <button type="button" className="button button--primary" onClick={() => window.print()}>
          <Printer aria-hidden="true" />
          Yazdır / PDF
        </button>
      </div>
      {draft && <p className="alert alert--info no-print">Sipariş henüz onaylanmadı; belge üzerinde &quot;Taslak&quot; ibaresi yer alır.</p>}

      <article className="proforma">
        <div style={{ minHeight: '16mm' }}>
          <span className="muted no-print">Antet alanı: firma bilgisi için antetli kâğıda basın.</span>
        </div>
        <header className="proforma__head">
          <div>
            <h1>Satın Alma Siparişi</h1>
            {draft && <div className="muted">Taslak</div>}
          </div>
          <dl className="proforma__meta">
            <div><dt>Sipariş no</dt><dd>{data.orderNo}</dd></div>
            <div><dt>Sipariş tarihi</dt><dd>{formatDate(data.orderDate)}</dd></div>
            <div><dt>Termin</dt><dd>{data.dueDate ? formatDate(data.dueDate) : '—'}</dd></div>
            <div><dt>Para birimi</dt><dd>{data.currency}</dd></div>
          </dl>
        </header>

        <div className="proforma__parties" style={{ gridTemplateColumns: '1fr' }}>
          <section className="proforma__party">
            <h3>Tedarikçi</h3>
            <strong>{data.supplierName}</strong>
            <div>Kod: {data.supplierCode}</div>
            {data.supplierTaxNumber && <div>Vergi no: {data.supplierTaxNumber}</div>}
            {data.supplierPhone && <div>Telefon: {data.supplierPhone}</div>}
            {data.supplierEmail && <div>E-posta: {data.supplierEmail}</div>}
          </section>
        </div>

        <table className="proforma__table">
          <thead>
            <tr>
              <th>#</th>
              <th>Kalem</th>
              <th>Termin</th>
              <th className="num">Miktar</th>
              <th>Birim</th>
              <th className="num">Birim fiyat</th>
              <th className="num">Tutar</th>
            </tr>
          </thead>
          <tbody>
            {data.lines.map((line) => (
              <tr key={line.id}>
                <td>{line.sequence}</td>
                <td>
                  <strong>{line.itemCode}</strong> {line.itemName}
                  {line.itemDetail && <span className="muted"> ({line.itemDetail})</span>}
                  {line.notes && <div className="muted">{line.notes}</div>}
                </td>
                <td>{line.dueDate ? formatDate(line.dueDate) : '—'}</td>
                <td className="num">{formatQuantity(line.quantity)}</td>
                <td>{unitLabel(line.unit)}</td>
                <td className="num">{line.unitPrice === null ? '—' : money(line.unitPrice)}</td>
                <td className="num">{line.amount === null ? '—' : money(line.amount)}</td>
              </tr>
            ))}
          </tbody>
        </table>

        <div className="proforma__totals">
          {data.hasMissingPrices && <p className="muted">Fiyatı girilmemiş satırlar toplama dahil değildir.</p>}
          <dl>
            <div className="proforma__grand"><dt>Toplam (KDV hariç)</dt><dd>{money(data.amount)}</dd></div>
          </dl>
        </div>

        {data.notes && (
          <section className="proforma__notes">
            <h3>Not</h3>
            <p className="preserve-lines">{data.notes}</p>
          </section>
        )}

        <footer className="proforma__foot">
          <div>Lütfen sipariş numarasını irsaliye ve faturada belirtiniz.</div>
          <div className="proforma__sign" style={{ gridTemplateColumns: 'repeat(3, 1fr)' }}>
            <div>Hazırlayan{data.createdBy && `: ${data.createdBy}`}</div>
            <div>Onaylayan{data.approvedBy && `: ${data.approvedBy}`}</div>
            <div>Tedarikçi onayı</div>
          </div>
        </footer>
      </article>
    </section>
  )
}
