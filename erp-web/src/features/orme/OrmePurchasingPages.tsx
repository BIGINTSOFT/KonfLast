import { CheckCheck, ChevronDown, ChevronRight, Lock, PackageCheck, Pencil, Plus, Printer, RotateCcw, Search, ShoppingCart, Trash2, X, XCircle } from 'lucide-react'
import { Fragment, useMemo, useState } from 'react'
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
import './ormePlanning.css'
import {
  ormePurchaseStatusPresentation,
  useChangeOrmePurchaseOrderMutation,
  useCreateOrmePurchaseOrdersFromPlanMutation,
  useDeleteOrmePurchaseOrderMutation,
  useGetOrmePurchaseOrderQuery,
  useGetOrmePurchaseOrdersQuery,
  useGetOrmeYarnPlanQuery,
  useReceiveOrmePurchaseOrderMutation,
  useSaveOrmePurchaseOrderMutation,
} from './ormePurchasingApi'
import type { OrmeCreatedPurchaseOrder, OrmePurchaseOrder, OrmeYarnPlanRow } from './ormePurchasingApi'

const PAGE_SIZE = 20

const numberOrNull = (value: string) => (value.trim() === '' ? null : Number(value))

interface LineDraft {
  key: number
  yarnId: number | null
  kilograms: string
  unitPrice: string
  dueDate: string
  notes: string
}

const emptyLine = (key: number): LineDraft => ({ key, yarnId: null, kilograms: '', unitPrice: '', dueDate: '', notes: '' })

/** İplik satın alma siparişi formu. Termin boş bırakılırsa tedarikçinin teslim süresinden hesaplanır. */
function PurchaseOrderDialog({ order, onClose }: { order?: OrmePurchaseOrder; onClose: () => void }) {
  const navigate = useNavigate()
  const [save, { isLoading, error }] = useSaveOrmePurchaseOrderMutation()
  const [supplierId, setSupplierId] = useState<number | null>(order?.supplierId ?? null)
  const [orderDate, setOrderDate] = useState(order?.orderDate ?? todayIsoDate())
  const [currency, setCurrency] = useState(order?.currency ?? '')
  const [notes, setNotes] = useState(order?.notes ?? '')
  const [lines, setLines] = useState<LineDraft[]>(
    order?.lines.map((line) => ({
      key: line.id,
      yarnId: line.yarnId,
      kilograms: String(line.kilograms),
      unitPrice: line.unitPrice === null ? '' : String(line.unitPrice),
      dueDate: line.dueDate ?? '',
      notes: line.notes ?? '',
    })) ?? [emptyLine(1)],
  )
  const update = (key: number, patch: Partial<LineDraft>) => setLines(lines.map((line) => (line.key === key ? { ...line, ...patch } : line)))

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await save({
      id: order?.id,
      body: {
        supplierId: supplierId ?? 0,
        orderDate,
        currency: currency.trim() || null,
        notes: notes.trim() || null,
        lines: lines.map((line) => ({
          yarnId: line.yarnId ?? 0,
          kilograms: Number(line.kilograms),
          unitPrice: numberOrNull(line.unitPrice),
          dueDate: line.dueDate || null,
          notes: line.notes.trim() || null,
        })),
      },
    })
    if ('data' in result && result.data) {
      if (order) {
        onClose()
      } else {
        navigate(`/orme/purchase-orders/${result.data.id}`)
      }
    }
  }

  return (
    <Modal title={order ? `Satın alma siparişi: ${order.orderNo}` : 'Yeni iplik satın alma siparişi'} onClose={onClose} variant="drawer">
      <form className="form" onSubmit={handleSubmit}>
        <LookupSelect resource="orme-suppliers" label="Tedarikçi" required value={supplierId} onChange={setSupplierId} />
        <div className="form__pair">
          <label className="field"><span>Sipariş tarihi</span><input type="date" required value={orderDate} onChange={(event) => setOrderDate(event.target.value)} /></label>
          <label className="field"><span>Para birimi</span><input maxLength={3} placeholder="Boşsa tedarikçinin" value={currency} onChange={(event) => setCurrency(event.target.value.toUpperCase())} /></label>
        </div>
        <label className="field"><span>Not</span><textarea rows={2} maxLength={1000} value={notes} onChange={(event) => setNotes(event.target.value)} /></label>
        <div className="table-wrap">
          <table className="table table--compact">
            <thead>
              <tr>
                <th>İplik</th>
                <th className="table__number">Kilo</th>
                <th className="table__number">Birim fiyat (kg)</th>
                <th>Termin</th>
                <th aria-label="Sil" />
              </tr>
            </thead>
            <tbody>
              {lines.map((line) => (
                <tr key={line.key}>
                  <td><LookupSelect resource="orme-yarns" label="İplik" hideLabel idSuffix={`-${line.key}`} required value={line.yarnId} onChange={(value) => update(line.key, { yarnId: value })} /></td>
                  <td className="table__number"><input aria-label="Kilo" className="matrix-input purchase-input" type="number" min="0" step="0.01" required value={line.kilograms} onChange={(event) => update(line.key, { kilograms: event.target.value })} /></td>
                  <td className="table__number"><input aria-label="Birim fiyat" className="matrix-input purchase-input" type="number" min="0" step="0.0001" value={line.unitPrice} onChange={(event) => update(line.key, { unitPrice: event.target.value })} /></td>
                  <td><input aria-label="Termin" className="matrix-input purchase-date" type="date" value={line.dueDate} onChange={(event) => update(line.key, { dueDate: event.target.value })} /></td>
                  <td><button type="button" className="button" aria-label="Satırı sil" disabled={lines.length === 1} onClick={() => setLines(lines.filter((row) => row.key !== line.key))}><X aria-hidden="true" /></button></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <div><button type="button" className="button" onClick={() => setLines([...lines, emptyLine(Date.now())])}><Plus aria-hidden="true" />Satır ekle</button></div>
        <p className="muted">Termin boş bırakılırsa sipariş tarihine tedarikçinin teslim süresi eklenir. Yalnızca işletme malı iplik satın alınır.</p>
        <ErrorAlert error={error} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading}>Kaydet</button>
        </footer>
      </form>
    </Modal>
  )
}

/** İplik satın alma siparişleri, en yeniden eskiye. */
export function OrmePurchaseOrdersPage() {
  const canManage = usePermission(Permissions.OrmePurchaseManage)
  const grid = useGrid()
  const { page, setPage } = grid
  const [input, setInput] = useState('')
  const [search, setSearch] = useState('')
  const [creating, setCreating] = useState(false)
  const { data, error, isLoading, isFetching } = useGetOrmePurchaseOrdersQuery({ page, pageSize: PAGE_SIZE, search: search || undefined, ...grid.params })
  const today = todayIsoDate()

  const columns: GridColumn[] = [
    { key: 'orderNo', label: 'Sipariş no' },
    { key: 'supplierName', label: 'Tedarikçi' },
    { key: 'orderDate', label: 'Tarih', filter: false },
    { key: 'dueDate', label: 'Termin', filter: false, sortable: false },
    { key: 'lineCount', label: 'Satır', numeric: true, filter: false, sortable: false },
    { key: 'orderedKilograms', label: 'Sipariş (kg)', numeric: true, filter: false, sortable: false },
    { key: 'receivedKilograms', label: 'Teslim (kg)', numeric: true, filter: false, sortable: false },
    { key: 'amount', label: 'Tutar', numeric: true, filter: false, sortable: false },
    { key: 'status', label: 'Durum', filter: Object.entries(ormePurchaseStatusPresentation).map(([value, { label }]) => ({ value, label })) },
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
        <input type="search" placeholder="Sipariş no ya da tedarikçi" aria-label="Satın alma siparişi ara" value={input} onChange={(event) => setInput(event.target.value)} />
        <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
        <ClearFiltersButton grid={grid} />
        <div className="toolbar__actions">
          <Link className="button" to="/orme/yarn-plan">İplik ihtiyaç planı</Link>
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
                  const status = ormePurchaseStatusPresentation[row.status]
                  const late = row.dueDate !== null && row.dueDate < today && (row.status === 'APPROVED' || row.status === 'PARTIALLY_RECEIVED')
                  return (
                    <tr key={row.id}>
                      <td><Link to={`/orme/purchase-orders/${row.id}`}><code>{row.orderNo}</code></Link></td>
                      <td>{row.supplierName}</td>
                      <td>{formatDate(row.orderDate)}</td>
                      <td>{formatDate(row.dueDate)}{late && <> <span className="badge badge--danger">Gecikmiş</span></>}</td>
                      <td className="table__number">{row.lineCount}</td>
                      <td className="table__number">{formatQuantity(row.orderedKilograms)}</td>
                      <td className="table__number">{formatQuantity(row.receivedKilograms)}</td>
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
  key: number
  lineId: number
  kilograms: string
  packageCount: string
  producerLotNo: string
}

/** Teslim alma: her satır iplik girişi (lot) olarak girer. Aynı satırın farklı üretici lotları ayrı satır olarak eklenir. */
function ReceiveDialog({ order, onClose }: { order: OrmePurchaseOrder; onClose: () => void }) {
  const [receive, { isLoading, error }] = useReceiveOrmePurchaseOrderMutation()
  const [receiptDate, setReceiptDate] = useState(todayIsoDate())
  const [despatchNo, setDespatchNo] = useState('')
  const [notes, setNotes] = useState('')
  const lines = order.lines.filter((line) => line.openKilograms > 0)
  const [rows, setRows] = useState<ReceiptRow[]>(() => lines.map((line) => ({ key: line.id, lineId: line.id, kilograms: String(line.openKilograms), packageCount: '', producerLotNo: '' })))
  const update = (key: number, patch: Partial<ReceiptRow>) => setRows(rows.map((row) => (row.key === key ? { ...row, ...patch } : row)))
  const lineOf = (lineId: number) => order.lines.find((line) => line.id === lineId)
  const split = (row: ReceiptRow) => {
    const index = rows.findIndex((item) => item.key === row.key)
    const next = [...rows]
    next.splice(index + 1, 0, { key: Date.now(), lineId: row.lineId, kilograms: '', packageCount: '', producerLotNo: '' })
    setRows(next)
  }

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await receive({
      id: order.id,
      body: {
        receiptDate,
        despatchNo: despatchNo.trim() || null,
        notes: notes.trim() || null,
        lines: rows
          .filter((row) => Number(row.kilograms) > 0)
          .map((row) => ({ lineId: row.lineId, kilograms: Number(row.kilograms), packageCount: Number(row.packageCount), producerLotNo: row.producerLotNo.trim() || null })),
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
          <table className="table table--compact">
            <thead>
              <tr>
                <th>İplik</th>
                <th className="table__number">Kalan</th>
                <th>Gelen (kg)</th>
                <th>Bobin</th>
                <th>Üretici lot no</th>
                <th aria-label="İşlem" />
              </tr>
            </thead>
            <tbody>
              {rows.map((row) => {
                const line = lineOf(row.lineId)
                const first = rows.find((item) => item.lineId === row.lineId)?.key === row.key
                return (
                  <tr key={row.key}>
                    <td>{line && <><code>{line.yarnCode}</code> {line.yarnName}</>}</td>
                    <td className="table__number">{first && line ? formatQuantity(line.openKilograms) : ''}</td>
                    <td><input className="matrix-input purchase-input" aria-label="Gelen kilo" type="number" min="0" step="0.01" value={row.kilograms} onChange={(event) => update(row.key, { kilograms: event.target.value })} /></td>
                    <td><input className="matrix-input purchase-input" aria-label="Bobin sayısı" type="number" min="1" step="1" required={Number(row.kilograms) > 0} value={row.packageCount} onChange={(event) => update(row.key, { packageCount: event.target.value })} /></td>
                    <td><input className="matrix-input purchase-input" aria-label="Üretici lot no" maxLength={50} value={row.producerLotNo} onChange={(event) => update(row.key, { producerLotNo: event.target.value })} /></td>
                    <td className="table__actions">
                      {first
                        ? <button type="button" className="button" title="Aynı ipliğin başka üretici lotu" onClick={() => split(row)}><Plus aria-hidden="true" />Lot</button>
                        : <button type="button" className="button" aria-label="Satırı kaldır" onClick={() => setRows(rows.filter((item) => item.key !== row.key))}><X aria-hidden="true" /></button>}
                    </td>
                  </tr>
                )
              })}
              {rows.length === 0 && <tr><td colSpan={6} className="table__empty">Teslim beklenen satır yok.</td></tr>}
            </tbody>
          </table>
        </div>
        <label className="field"><span>Not</span><input maxLength={1000} placeholder="Boşsa sipariş numarası yazılır" value={notes} onChange={(event) => setNotes(event.target.value)} /></label>
        <p className="muted">Gelmeyen satırı boş ya da sıfır bırakın. Her satır iplik girişi (lot) olarak, işletme malı ve siparişin tedarikçisiyle girer; kalandan fazlası da kabul edilir.</p>
        <ErrorAlert error={error} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading}>Teslim al</button>
        </footer>
      </form>
    </Modal>
  )
}

export function OrmePurchaseOrderDetailPage() {
  const id = Number(useParams().id)
  const navigate = useNavigate()
  const canManage = usePermission(Permissions.OrmePurchaseManage)
  const { data: order, error, isLoading } = useGetOrmePurchaseOrderQuery(id)
  const [change, changeState] = useChangeOrmePurchaseOrderMutation()
  const [remove, removeState] = useDeleteOrmePurchaseOrderMutation()
  const [editing, setEditing] = useState(false)
  const [receiving, setReceiving] = useState(false)
  const [confirmDelete, setConfirmDelete] = useState(false)
  const status = order && ormePurchaseStatusPresentation[order.status]
  const busy = changeState.isLoading || removeState.isLoading

  const handleDelete = async () => {
    if (!('error' in await remove(id))) {
      navigate('/orme/purchase-orders')
    }
  }

  return (
    <section className="page">
      <p><Link to="/orme/purchase-orders">← Satın alma siparişlerine dön</Link></p>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error ?? changeState.error ?? removeState.error} />
      {order && status && (
        <>
          <header className="page__header">
            <div>
              <h1>{order.orderNo} <span className={`badge badge--${status.tone}`}>{status.label}</span></h1>
              <p className="muted">{order.supplierName} · {formatDate(order.orderDate)} · {order.currency}{order.createdBy && ` · ${order.createdBy}, ${formatDateTime(order.createdAt)}`}</p>
              {order.notes && <p className="preserve-lines">{order.notes}</p>}
            </div>
            <div className="page__actions">
              <Link className="button" to={`/orme/purchase-orders/${order.id}/print`}><Printer aria-hidden="true" />Yazdır</Link>
              {canManage && (
                <>
                  {order.status === 'DRAFT' && <button type="button" className="button" onClick={() => setEditing(true)}><Pencil aria-hidden="true" />Düzenle</button>}
                  {order.status === 'DRAFT' && <button type="button" className="button button--primary" disabled={busy} onClick={() => change({ id, action: 'approve' })}><CheckCheck aria-hidden="true" />Onayla</button>}
                  {(order.status === 'APPROVED' || order.status === 'PARTIALLY_RECEIVED') && <button type="button" className="button button--primary" onClick={() => setReceiving(true)}><PackageCheck aria-hidden="true" />Teslim al</button>}
                  {order.status === 'APPROVED' && <button type="button" className="button" disabled={busy} onClick={() => change({ id, action: 'reopen' })}><RotateCcw aria-hidden="true" />Taslağa al</button>}
                  {order.status === 'PARTIALLY_RECEIVED' && <button type="button" className="button" disabled={busy} onClick={() => change({ id, action: 'close' })}><Lock aria-hidden="true" />Kalanı kapat</button>}
                  {order.status === 'APPROVED' && <button type="button" className="button button--danger-ghost" disabled={busy} onClick={() => change({ id, action: 'cancel' })}><XCircle aria-hidden="true" />İptal et</button>}
                  {order.status === 'DRAFT' && !confirmDelete && <button type="button" className="button button--danger-ghost" onClick={() => setConfirmDelete(true)}><Trash2 aria-hidden="true" />Sil</button>}
                  {order.status === 'DRAFT' && confirmDelete && <button type="button" className="button button--danger-ghost" disabled={busy} onClick={handleDelete}>Silmeyi onayla</button>}
                </>
              )}
            </div>
          </header>
          <dl className="stat-strip">
            <div><dt>Sipariş</dt><dd>{formatQuantity(order.totalKilograms)} kg</dd></div>
            <div><dt>Teslim alınan</dt><dd>{formatQuantity(order.receivedKilograms)} kg</dd></div>
            <div><dt>Tutar</dt><dd>{order.amount > 0 ? formatMoney(order.amount, order.currency) : '—'}{order.hasUnpricedLines && <span className="muted"> (fiyatsız satır var)</span>}</dd></div>
          </dl>
          <div className="table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <th>İplik</th>
                  <th>Termin</th>
                  <th className="table__number">Sipariş (kg)</th>
                  <th className="table__number">Teslim (kg)</th>
                  <th className="table__number">Kalan (kg)</th>
                  <th className="table__number">Birim fiyat</th>
                  <th className="table__number">Tutar</th>
                  <th>Lotlar</th>
                </tr>
              </thead>
              <tbody>
                {order.lines.map((line) => (
                  <tr key={line.id}>
                    <td><code>{line.yarnCode}</code> {line.yarnName}{line.yarnColor && <span className="muted"> ({line.yarnColor})</span>}{line.notes && <div className="muted">{line.notes}</div>}</td>
                    <td>{formatDate(line.dueDate)}</td>
                    <td className="table__number">{formatQuantity(line.kilograms)}</td>
                    <td className="table__number">{formatQuantity(line.receivedKilograms)}</td>
                    <td className="table__number">{formatQuantity(line.openKilograms)}</td>
                    <td className="table__number">{line.unitPrice === null ? '—' : formatMoney(line.unitPrice, order.currency)}</td>
                    <td className="table__number">{line.amount === null ? '—' : formatMoney(line.amount, order.currency)}</td>
                    <td>
                      {line.lots.length === 0 && '—'}
                      {line.lots.map((lot) => (
                        <div key={lot.id}>
                          <code>{lot.lotNo}</code> {formatDate(lot.receiptDate)} · {formatQuantity(lot.kilograms)} kg
                          {lot.producerLotNo && <span className="muted"> · üretici lot {lot.producerLotNo}</span>}
                        </div>
                      ))}
                    </td>
                  </tr>
                ))}
              </tbody>
              <tfoot>
                <tr>
                  <th colSpan={2} scope="row">Toplam</th>
                  <td className="table__number">{formatQuantity(order.totalKilograms)}</td>
                  <td className="table__number">{formatQuantity(order.receivedKilograms)}</td>
                  <td colSpan={2} />
                  <td className="table__number">{order.amount > 0 ? formatMoney(order.amount, order.currency) : '—'}</td>
                  <td />
                </tr>
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

/**
 * Yazdırılabilir satın alma siparişi: tedarikçiye gönderilir. Örme'de firma kartı olmadığı için üstte antet alanı boş bırakılır;
 * antetli kâğıda basılır.
 */
export function OrmePurchaseOrderPrintPage() {
  const id = Number(useParams().id)
  const { data, isLoading, error } = useGetOrmePurchaseOrderQuery(id)

  if (!data) {
    return (
      <section className="page">
        <p><Link to={`/orme/purchase-orders/${id}`}>← Siparişe dön</Link></p>
        {isLoading && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={error} />
      </section>
    )
  }

  const draft = data.status === 'DRAFT'
  const cancelled = data.status === 'CANCELLED'

  return (
    <section className="page proforma-page">
      <div className="no-print proforma-page__bar">
        <Link to={`/orme/purchase-orders/${id}`}>← Siparişe dön</Link>
        <span className="muted">Yazdır penceresinde hedef olarak &quot;PDF olarak kaydet&quot; seçilirse PDF dosyası oluşur.</span>
        <button type="button" className="button button--primary" onClick={() => window.print()}>
          <Printer aria-hidden="true" />
          Yazdır / PDF
        </button>
      </div>
      {draft && <p className="alert alert--info no-print">Sipariş henüz taslak; belge üzerinde &quot;TASLAK&quot; ibaresi yer alır.</p>}

      <article className="proforma">
        <div style={{ minHeight: '16mm' }}>
          <span className="muted no-print">Antet alanı: firma bilgisi için antetli kâğıda basın.</span>
        </div>
        <header className="proforma__head">
          <div>
            <h1>SATIN ALMA SİPARİŞİ</h1>
            <div className="muted">İplik{draft && ' · TASLAK'}{cancelled && ' · İPTAL'}</div>
          </div>
          <dl className="proforma__meta">
            <div><dt>Sipariş no</dt><dd>{data.orderNo}</dd></div>
            <div><dt>Sipariş tarihi</dt><dd>{formatDate(data.orderDate)}</dd></div>
            <div><dt>Para birimi</dt><dd>{data.currency}</dd></div>
          </dl>
        </header>

        <div className="proforma__parties" style={{ gridTemplateColumns: '1fr' }}>
          <section className="proforma__party">
            <h3>Tedarikçi</h3>
            <strong>{data.supplierName}</strong>
            <div>Tedarikçi kodu: {data.supplierCode}</div>
            {data.supplierTaxNumber && <div>Vergi no: {data.supplierTaxNumber}</div>}
            {(data.supplierPhone || data.supplierEmail) && <div>{[data.supplierPhone, data.supplierEmail].filter(Boolean).join(' · ')}</div>}
          </section>
        </div>

        <table className="proforma__table">
          <thead>
            <tr>
              <th>#</th>
              <th>İplik</th>
              <th>Termin</th>
              <th className="num">Kilo</th>
              <th className="num">Birim fiyat</th>
              <th className="num">Tutar</th>
            </tr>
          </thead>
          <tbody>
            {data.lines.map((line, index) => (
              <tr key={line.id}>
                <td>{index + 1}</td>
                <td><strong>{line.yarnCode}</strong> {line.yarnName}{line.yarnColor && ` (${line.yarnColor})`}{line.notes && <div className="muted">{line.notes}</div>}</td>
                <td>{formatDate(line.dueDate)}</td>
                <td className="num">{formatQuantity(line.kilograms)}</td>
                <td className="num">{line.unitPrice === null ? '—' : formatMoney(line.unitPrice, data.currency)}</td>
                <td className="num">{line.amount === null ? '—' : formatMoney(line.amount, data.currency)}</td>
              </tr>
            ))}
          </tbody>
          <tfoot>
            <tr>
              <th colSpan={3}>Toplam</th>
              <td className="num">{formatQuantity(data.totalKilograms)}</td>
              <td />
              <td className="num">{formatMoney(data.amount, data.currency)}</td>
            </tr>
          </tfoot>
        </table>

        <div className="proforma__totals">
          {data.hasUnpricedLines && <p className="muted">Fiyatı belirtilmemiş satırlar tutara katılmamıştır.</p>}
          <p className="muted">Tutarlara KDV dahil değildir.</p>
        </div>

        {data.notes && (
          <section className="proforma__notes">
            <h3>Notlar</h3>
            <p className="preserve-lines">{data.notes}</p>
          </section>
        )}

        <section className="proforma__notes">
          <h3>Teslim koşulları</h3>
          <p>Teslimatta irsaliye numarası, iplik üreticisinin lot numarası ve bobin sayısı belirtilmelidir. Farklı üretici lotları ayrı etiketlenmelidir.</p>
        </section>

        <footer className="proforma__foot">
          <div className="proforma__sign" style={{ gridTemplateColumns: 'repeat(3, 1fr)' }}>
            <div>Hazırlayan</div>
            <div>Onaylayan</div>
            <div>Tedarikçi onayı</div>
          </div>
        </footer>
      </article>
    </section>
  )
}

interface PlanDraft {
  key: string
  yarnId: number
  yarnLabel: string
  supplierId: number | null
  kilograms: string
  dueDate: string
}

/** İhtiyaç planındaki seçili eksiklerden her tedarikçi için bir taslak sipariş. Tedarikçi satır satır değiştirilebilir. */
function CreateFromPlanDialog({ rows, onClose }: { rows: OrmeYarnPlanRow[]; onClose: () => void }) {
  const [create, { isLoading, error }] = useCreateOrmePurchaseOrdersFromPlanMutation()
  const [orderDate, setOrderDate] = useState(todayIsoDate())
  const [created, setCreated] = useState<OrmeCreatedPurchaseOrder[] | null>(null)
  const [drafts, setDrafts] = useState<PlanDraft[]>(() => rows.map((row) => ({
    key: `${row.yarnId}`,
    yarnId: row.yarnId,
    yarnLabel: `${row.yarnCode} — ${row.yarnName}${row.yarnColor ? ` (${row.yarnColor})` : ''}`,
    supplierId: row.supplierId,
    kilograms: String(row.suggestedKilograms),
    dueDate: row.neededBy ?? '',
  })))
  const update = (key: string, patch: Partial<PlanDraft>) => setDrafts(drafts.map((draft) => (draft.key === key ? { ...draft, ...patch } : draft)))

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await create({
      orderDate,
      lines: drafts
        .filter((draft) => Number(draft.kilograms) > 0)
        .map((draft) => ({ yarnId: draft.yarnId, supplierId: draft.supplierId ?? 0, kilograms: Number(draft.kilograms), dueDate: draft.dueDate || null })),
    })
    if ('data' in result && result.data) {
      setCreated(result.data)
    }
  }

  if (created) {
    return (
      <Modal title="Taslak siparişler oluşturuldu" onClose={onClose}>
        <div className="form">
          <p>Siparişler taslak olarak açıldı; fiyat ve terminleri kontrol edip onaylayın.</p>
          <ul>
            {created.map((order) => (
              <li key={order.id}><Link to={`/orme/purchase-orders/${order.id}`}><code>{order.orderNo}</code></Link> · {order.supplierName} · {order.lineCount} satır · {formatQuantity(order.kilograms)} kg</li>
            ))}
          </ul>
          <footer className="form__actions">
            <button type="button" className="button button--primary" onClick={onClose}>Kapat</button>
          </footer>
        </div>
      </Modal>
    )
  }

  return (
    <Modal title="Eksiklerden taslak satın alma siparişi" onClose={onClose} variant="drawer">
      <form className="form" onSubmit={handleSubmit}>
        <label className="field"><span>Sipariş tarihi</span><input type="date" required value={orderDate} onChange={(event) => setOrderDate(event.target.value)} /></label>
        <div className="table-wrap">
          <table className="table table--compact">
            <thead>
              <tr>
                <th>İplik</th>
                <th>Tedarikçi</th>
                <th className="table__number">Kilo</th>
                <th>Termin</th>
              </tr>
            </thead>
            <tbody>
              {drafts.map((draft) => (
                <tr key={draft.key}>
                  <td>{draft.yarnLabel}</td>
                  <td><LookupSelect resource="orme-suppliers" label="Tedarikçi" hideLabel idSuffix={`-plan-${draft.key}`} required value={draft.supplierId} onChange={(value) => update(draft.key, { supplierId: value })} /></td>
                  <td className="table__number"><input aria-label="Kilo" className="matrix-input purchase-input" type="number" min="0" step="0.01" value={draft.kilograms} onChange={(event) => update(draft.key, { kilograms: event.target.value })} /></td>
                  <td><input aria-label="Termin" className="matrix-input purchase-date" type="date" value={draft.dueDate} onChange={(event) => update(draft.key, { dueDate: event.target.value })} /></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <p className="muted">Her tedarikçi için bir taslak sipariş açılır. Termin ihtiyaç tarihidir; geçmişse ya da boşsa tedarikçinin teslim süresinden hesaplanır. Birim fiyat aynı tedarikçiden verilen son siparişten gelir.</p>
        <ErrorAlert error={error} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading}>Taslakları oluştur</button>
        </footer>
      </form>
    </Modal>
  )
}

type OwnerFilter = 'all' | 'own' | 'customer'

const rowKey = (row: OrmeYarnPlanRow) => `${row.yarnId}-${row.ownerCustomerId ?? 'own'}`

/**
 * İplik ihtiyaç planı: onaylı siparişlerin açık satırlarının iplik ihtiyacı, iplik ve malın sahibi başına. Eldeki stok ve yoldaki
 * satın alma düşülür; eksik, ihtiyaç tarihi ve sipariş verme tarihi gösterilir. Müşteri ipliği satın alınmaz, müşteriden beklenir.
 */
export function OrmeYarnPlanPage() {
  const canManage = usePermission(Permissions.OrmePurchaseManage)
  const { data, error, isLoading, isFetching } = useGetOrmeYarnPlanQuery()
  const [search, setSearch] = useState('')
  const [shortageOnly, setShortageOnly] = useState(false)
  const [owner, setOwner] = useState<OwnerFilter>('all')
  const [expanded, setExpanded] = useState<Set<string>>(new Set())
  const [selected, setSelected] = useState<Set<number>>(new Set())
  const [creating, setCreating] = useState(false)

  const rows = useMemo(() => {
    const term = search.trim().toLocaleLowerCase('tr')
    return (data?.rows ?? []).filter((row) =>
      (!shortageOnly || row.shortageKilograms > 0)
      && (owner === 'all' || (owner === 'own') === !row.customerSupplied)
      && (term === '' || `${row.yarnCode} ${row.yarnName} ${row.ownerName ?? ''} ${row.supplierName ?? ''}`.toLocaleLowerCase('tr').includes(term)))
  }, [data, search, shortageOnly, owner])

  const purchasable = (row: OrmeYarnPlanRow) => !row.customerSupplied && row.suggestedKilograms > 0
  const chosen = (data?.rows ?? []).filter((row) => purchasable(row) && selected.has(row.yarnId))
  const totals = rows.reduce((sum, row) => ({ remaining: sum.remaining + row.remainingKilograms, shortage: sum.shortage + row.shortageKilograms }), { remaining: 0, shortage: 0 })

  const toggle = <T,>(set: Set<T>, value: T, on: boolean) => {
    const next = new Set(set)
    if (on) {
      next.add(value)
    } else {
      next.delete(value)
    }
    return next
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">İplik İhtiyaç Planı</h1>
      <div className="toolbar">
        <input type="search" placeholder="İplik, müşteri ya da tedarikçi" aria-label="İplik ara" value={search} onChange={(event) => setSearch(event.target.value)} />
        <select aria-label="Malın sahibi" value={owner} onChange={(event) => setOwner(event.target.value as OwnerFilter)}>
          <option value="all">Tüm iplikler</option>
          <option value="own">İşletme malı</option>
          <option value="customer">Müşteri ipliği (fason)</option>
        </select>
        <label className="checkbox"><input type="checkbox" checked={shortageOnly} onChange={(event) => setShortageOnly(event.target.checked)} />Yalnız eksikler</label>
        <div className="toolbar__actions">
          <Link className="button" to="/orme/purchase-orders">Satın alma siparişleri</Link>
          {canManage && <button type="button" className="button button--primary" disabled={chosen.length === 0} onClick={() => setCreating(true)}><ShoppingCart aria-hidden="true" />Taslak sipariş oluştur ({chosen.length})</button>}
        </div>
      </div>
      <p className="muted">
        Onaylı siparişlerin örülmesi bitmemiş satırları sayılır. İhtiyaç: sipariş kilosu × iplik oranı, kumaş kartındaki fireyle; satıra verilen iplik düşülür.
        Eksik = kalan ihtiyaç − eldeki − yoldaki satın alma. İhtiyaç tarihi, termin sırasıyla stoktan karşılanamayan ilk siparişin terminidir;
        sipariş verme tarihi = ihtiyaç tarihi − tedarikçinin teslim süresi.
      </p>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {data && data.missingFabrics.length > 0 && (
        <p className="alert alert--info">Kumaş kartında iplik tanımı olmadığı için hesaba katılmayan kumaşlar: {data.missingFabrics.join(', ')}</p>
      )}
      {data && (
        <div className="table-wrap" aria-busy={isFetching}>
          <table className="table">
            <thead>
              <tr>
                <th aria-label="Seç" />
                <th aria-label="Aç" />
                <th>İplik</th>
                <th>Malın sahibi</th>
                <th className="table__number">İhtiyaç (fireli)</th>
                <th className="table__number">Verilen</th>
                <th className="table__number">Kalan ihtiyaç</th>
                <th className="table__number">Eldeki</th>
                <th className="table__number">Serbest</th>
                <th className="table__number">Yolda</th>
                <th className="table__number">Eksik</th>
                <th>İhtiyaç tarihi</th>
                <th>Tedarikçi</th>
                <th>Sipariş verme</th>
                <th className="table__number">Önerilen</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((row) => {
                const key = rowKey(row)
                const open = expanded.has(key)
                return (
                  <Fragment key={key}>
                    <tr>
                      <td>
                        {canManage && purchasable(row) && (
                          <input type="checkbox" aria-label={`${row.yarnCode} seç`} checked={selected.has(row.yarnId)} onChange={(event) => setSelected(toggle(selected, row.yarnId, event.target.checked))} />
                        )}
                      </td>
                      <td>
                        <button type="button" className="button button--ghost button--icon" aria-expanded={open} aria-label={open ? 'Siparişleri gizle' : 'Siparişleri göster'} onClick={() => setExpanded(toggle(expanded, key, !open))}>
                          {open ? <ChevronDown aria-hidden="true" /> : <ChevronRight aria-hidden="true" />}
                        </button>
                      </td>
                      <td><code>{row.yarnCode}</code> {row.yarnName}{row.yarnColor && <span className="muted"> ({row.yarnColor})</span>}</td>
                      <td>{row.customerSupplied ? <>{row.ownerName}<div className="muted">Müşteri ipliği</div></> : <span className="muted">İşletme malı</span>}</td>
                      <td className="table__number">{formatQuantity(row.requiredKilograms)}</td>
                      <td className="table__number">{formatQuantity(row.issuedKilograms)}</td>
                      <td className="table__number">{formatQuantity(row.remainingKilograms)}</td>
                      <td className="table__number">{formatQuantity(row.onHandKilograms)}</td>
                      <td className="table__number">{row.freeKilograms < 0 ? <span className="orme-negative">{formatQuantity(row.freeKilograms)}</span> : formatQuantity(row.freeKilograms)}</td>
                      <td className="table__number">
                        {row.customerSupplied ? '—' : formatQuantity(row.openPurchaseKilograms)}
                        {row.draftPurchaseKilograms > 0 && <div className="muted" title="Taslak satın alma siparişlerinde">taslak {formatQuantity(row.draftPurchaseKilograms)}</div>}
                      </td>
                      <td className="table__number">{row.shortageKilograms > 0 ? <span className="badge badge--danger">{formatQuantity(row.shortageKilograms)}</span> : <span className="muted">0</span>}</td>
                      <td>{formatDate(row.neededBy)}</td>
                      <td>
                        {row.customerSupplied
                          ? <span className="muted">Müşteriden beklenir</span>
                          : row.supplierName ?? <span className="muted">Tanımsız</span>}
                        {row.leadTimeDays !== null && <div className="muted">{row.leadTimeDays} gün teslim</div>}
                      </td>
                      <td>{row.orderBy ? <>{formatDate(row.orderBy)}{row.orderOverdue && <> <span className="badge badge--danger">Geçti</span></>}</> : '—'}</td>
                      <td className="table__number">{row.suggestedKilograms > 0 ? <strong>{formatQuantity(row.suggestedKilograms)}</strong> : '—'}</td>
                    </tr>
                    {open && (
                      <tr className="orme-subrow">
                        <td />
                        <td colSpan={14}>
                          <table className="table table--compact">
                            <thead>
                              <tr>
                                <th>Sipariş</th>
                                <th>Termin</th>
                                <th>Kumaş</th>
                                <th className="table__number">İhtiyaç</th>
                                <th className="table__number">Verilen</th>
                                <th className="table__number">Kalan</th>
                                <th className="table__number">Karşılanan</th>
                                <th>Durum</th>
                              </tr>
                            </thead>
                            <tbody>
                              {row.orders.map((order) => {
                                const short = order.remainingKilograms - order.coveredKilograms
                                return (
                                  <tr key={order.orderId}>
                                    <td><Link to={`/orme/orders/${order.orderId}`}><code>{order.orderNo}</code></Link>{order.customerName && <div className="muted">{order.customerName}</div>}</td>
                                    <td>{formatDate(order.dueDate)}</td>
                                    <td>{order.fabricCodes}</td>
                                    <td className="table__number">{formatQuantity(order.requiredKilograms)}</td>
                                    <td className="table__number">{formatQuantity(order.issuedKilograms)}</td>
                                    <td className="table__number">{formatQuantity(order.remainingKilograms)}</td>
                                    <td className="table__number">{formatQuantity(order.coveredKilograms)}</td>
                                    <td>
                                      {order.remainingKilograms <= 0
                                        ? <span className="badge badge--ok">Verildi</span>
                                        : short <= 0
                                          ? <span className="badge badge--ok">Karşılanıyor</span>
                                          : <span className="badge badge--danger">{formatQuantity(short)} kg eksik</span>}
                                    </td>
                                  </tr>
                                )
                              })}
                            </tbody>
                          </table>
                        </td>
                      </tr>
                    )}
                  </Fragment>
                )
              })}
              {rows.length === 0 && <tr><td colSpan={15} className="table__empty">{data.rows.length === 0 ? 'Açık siparişlerin iplik ihtiyacı yok.' : 'Filtreye uyan iplik yok.'}</td></tr>}
            </tbody>
            {rows.length > 0 && (
              <tfoot>
                <tr>
                  <th colSpan={6} scope="row">Toplam (listelenen)</th>
                  <td className="table__number">{formatQuantity(totals.remaining)}</td>
                  <td colSpan={3} />
                  <td className="table__number">{formatQuantity(totals.shortage)}</td>
                  <td colSpan={4} />
                </tr>
              </tfoot>
            )}
          </table>
        </div>
      )}
      {creating && <CreateFromPlanDialog rows={chosen} onClose={() => { setCreating(false); setSelected(new Set()) }} />}
    </section>
  )
}
