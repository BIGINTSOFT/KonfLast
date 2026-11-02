import { CheckCheck, Pencil, Plus, Printer, RotateCcw, ScanBarcode, Search, XCircle } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent, ReactNode } from 'react'
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
import { documentTexts } from '../../utils/documentText'
import { formatDate, formatQuantity, todayIsoDate } from '../../utils/format'
import { formatMoney } from '../../utils/money'
import { Barcode } from '../labels/LabelPrintPage'
import { orderUnitLabels } from './boyahaneOrdersApi'
import {
  shippingStatusPresentation,
  useChangeDespatchMutation,
  useChangeInvoiceMutation,
  useGetDespatchCandidatesQuery,
  useGetDespatchesQuery,
  useGetDespatchQuery,
  useGetInvoiceCandidatesQuery,
  useGetInvoiceQuery,
  useGetInvoicesQuery,
  useSaveDespatchMutation,
  useSaveInvoiceMutation,
} from './boyahaneShippingApi'
import type { Despatch, Invoice, ShippingStatus } from './boyahaneShippingApi'

const PAGE_SIZE = 20

function StatusBadge({ status }: { status: ShippingStatus }) {
  const presentation = shippingStatusPresentation[status]
  return <span className={`badge badge--${presentation.tone}`}>{presentation.label}</span>
}

const statusColumn: GridColumn = {
  key: 'status',
  label: 'Durum',
  filter: Object.entries(shippingStatusPresentation).map(([value, { label }]) => ({ value, label })),
}

/** Belge seçimi: adaylar müşteri müşteri gruplanır, yalnızca bir müşteriden seçilebilir. */
function useCustomerSelection<T extends { id: number; customerId: number; customerName: string; selected: boolean }>(candidates: T[] | undefined) {
  const [changes, setChanges] = useState<Map<number, boolean>>(new Map())
  const isSelected = (candidate: T) => changes.get(candidate.id) ?? candidate.selected
  const selected = candidates?.filter(isSelected) ?? []
  const customers = [...new Set((candidates ?? []).map((candidate) => candidate.customerId))]
  const toggle = (id: number, value: boolean) => setChanges(new Map(changes).set(id, value))
  return { isSelected, selected, customers, toggle, activeCustomer: selected[0]?.customerId ?? null }
}

function DespatchDialog({ despatch, onClose }: { despatch?: Despatch; onClose: () => void }) {
  const navigate = useNavigate()
  const { data: candidates, error: loadError, isLoading } = useGetDespatchCandidatesQuery(despatch?.id)
  const [save, { isLoading: saving, error }] = useSaveDespatchMutation()
  const [despatchDate, setDespatchDate] = useState(despatch?.despatchDate ?? todayIsoDate())
  const [vehiclePlate, setVehiclePlate] = useState(despatch?.vehiclePlate ?? '')
  const [driverName, setDriverName] = useState(despatch?.driverName ?? '')
  const [deliveryAddress, setDeliveryAddress] = useState(despatch?.deliveryAddress ?? '')
  const [notes, setNotes] = useState(despatch?.notes ?? '')
  const { isSelected, selected, customers, toggle, activeCustomer } = useCustomerSelection(candidates)

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await save({
      id: despatch?.id,
      body: {
        customerId: activeCustomer ?? 0,
        despatchDate,
        packingListIds: selected.map((candidate) => candidate.id),
        vehiclePlate: vehiclePlate.trim() || null,
        driverName: driverName.trim() || null,
        deliveryAddress: deliveryAddress.trim() || null,
        notes: notes.trim() || null,
      },
    })
    if ('data' in result && result.data) {
      if (despatch) {
        onClose()
      } else {
        navigate(`/boyahane/despatches/${result.data.id}`)
      }
    }
  }

  return (
    <Modal title={despatch ? `İrsaliye: ${despatch.despatchNo}` : 'Yeni irsaliye'} onClose={onClose} variant="drawer">
      <form className="form" onSubmit={handleSubmit}>
        <div className="form__pair">
          <label className="field"><span>Tarih</span><input type="date" required value={despatchDate} onChange={(event) => setDespatchDate(event.target.value)} /></label>
          <label className="field"><span>Plaka</span><input maxLength={20} value={vehiclePlate} onChange={(event) => setVehiclePlate(event.target.value)} /></label>
        </div>
        <label className="field"><span>Şoför</span><input maxLength={100} value={driverName} onChange={(event) => setDriverName(event.target.value)} /></label>
        <label className="field"><span>Teslimat adresi</span><input maxLength={500} placeholder="Boşsa müşterinin teslimat adresi" value={deliveryAddress} onChange={(event) => setDeliveryAddress(event.target.value)} /></label>
        <label className="field"><span>Not</span><input maxLength={1000} value={notes} onChange={(event) => setNotes(event.target.value)} /></label>
        {isLoading && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={loadError} />
        {candidates && candidates.length === 0 && <p className="muted">Sevk edilecek onaylı çeki listesi yok.</p>}
        {customers.map((customerId) => {
          const items = (candidates ?? []).filter((candidate) => candidate.customerId === customerId)
          return (
            <fieldset key={customerId} className="form__section" disabled={activeCustomer !== null && activeCustomer !== customerId}>
              <legend>{items[0].customerName}</legend>
              <div className="table-wrap batch-candidates">
                <table className="table">
                  <thead><tr><th aria-label="Seç" /><th>Çeki no</th><th>Sipariş</th><th className="table__number">Paket</th><th className="table__number">Kilo</th><th className="table__number">Metre</th></tr></thead>
                  <tbody>
                    {items.map((candidate) => (
                      <tr key={candidate.id}>
                        <td><input type="checkbox" aria-label={`${candidate.packingNo} seç`} checked={isSelected(candidate)} onChange={(event) => toggle(candidate.id, event.target.checked)} /></td>
                        <td><code>{candidate.packingNo}</code></td>
                        <td>{candidate.orderNo}</td>
                        <td className="table__number">{candidate.packageCount}</td>
                        <td className="table__number">{formatQuantity(candidate.kilograms)}</td>
                        <td className="table__number">{formatQuantity(candidate.meters)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </fieldset>
          )
        })}
        <ErrorAlert error={error} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={saving || selected.length === 0}>Kaydet</button>
        </footer>
      </form>
    </Modal>
  )
}

/** Liste sayfası iskeleti: arama, isteğe bağlı süzgeçler, yeni kayıt düğmesi. */
function ListShell({ title, placeholder, canManage, addLabel, onAdd, search, filters, children }: {
  title: string
  placeholder: string
  canManage: boolean
  addLabel: string
  onAdd: () => void
  search: { input: string; setInput: (value: string) => void; submit: (event: FormEvent) => void; clear: ReactNode }
  filters?: ReactNode
  children: ReactNode
}) {
  return (
    <section className="page">
      <h1 className="visually-hidden">{title}</h1>
      <form className="toolbar" onSubmit={search.submit} role="search">
        <input type="search" placeholder={placeholder} aria-label={`${title} ara`} value={search.input} onChange={(event) => search.setInput(event.target.value)} />
        {filters}
        <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
        {search.clear}
        <div className="toolbar__actions">
          {canManage && <button type="button" className="button button--primary" onClick={onAdd}><Plus aria-hidden="true" />{addLabel}</button>}
        </div>
      </form>
      {children}
    </section>
  )
}

export function DespatchesPage() {
  const canManage = usePermission(Permissions.BoyahaneShipmentManage)
  const grid = useGrid()
  const { page, setPage } = grid
  const [input, setInput] = useState('')
  const [search, setSearch] = useState('')
  const [creating, setCreating] = useState(false)
  // Tarih aralığı seçildiği anda uygulanır; arama metni Ara ile.
  const [from, setFrom] = useState('')
  const [to, setTo] = useState('')
  const { data, error, isLoading, isFetching } = useGetDespatchesQuery({
    page,
    pageSize: PAGE_SIZE,
    search: search || undefined,
    from: from || undefined,
    to: to || undefined,
    ...grid.params,
  })
  const columns: GridColumn[] = [
    { key: 'despatchNo', label: 'İrsaliye no' },
    { key: 'customerName', label: 'Müşteri' },
    { key: 'despatchDate', label: 'Tarih', filter: false },
    { key: 'vehiclePlate', label: 'Plaka' },
    { key: 'packingListCount', label: 'Çeki', numeric: true },
    { key: 'totalKilograms', label: 'Kilo', numeric: true },
    { key: 'invoiceNo', label: 'Fatura' },
    statusColumn,
  ]
  const submit = (event: FormEvent) => {
    event.preventDefault()
    setPage(1)
    setSearch(input.trim())
  }

  return (
    <ListShell title="İrsaliyeler" placeholder="İrsaliye no, müşteri ya da müşteri sipariş no" canManage={canManage} addLabel="Yeni irsaliye" onAdd={() => setCreating(true)}
      search={{ input, setInput, submit, clear: <ClearFiltersButton grid={grid} /> }}
      filters={(
        <>
          <input type="date" aria-label="Başlangıç tarihi" title="Başlangıç tarihi" value={from} onChange={(event) => { setPage(1); setFrom(event.target.value) }} />
          <input type="date" aria-label="Bitiş tarihi" title="Bitiş tarihi" value={to} onChange={(event) => { setPage(1); setTo(event.target.value) }} />
        </>
      )}>
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
                    <td><Link to={`/boyahane/despatches/${row.id}`}><code>{row.despatchNo}</code></Link></td>
                    <td>{row.customerName}</td>
                    <td>{formatDate(row.despatchDate)}</td>
                    <td>{row.vehiclePlate ?? '—'}</td>
                    <td className="table__number">{row.packingListCount}</td>
                    <td className="table__number">{formatQuantity(row.totalKilograms)}</td>
                    <td>{row.invoiceNo ?? '—'}</td>
                    <td><StatusBadge status={row.status} /></td>
                  </tr>
                ))}
                {data.items.length === 0 && <tr><td colSpan={columns.length} className="table__empty">İrsaliye bulunamadı.</td></tr>}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}
      {creating && <DespatchDialog onClose={() => setCreating(false)} />}
    </ListShell>
  )
}

export function DespatchDetailPage() {
  const id = Number(useParams().id)
  const canManage = usePermission(Permissions.BoyahaneShipmentManage)
  const { data: despatch, error, isLoading } = useGetDespatchQuery(id)
  const [change, changeState] = useChangeDespatchMutation()
  const [editing, setEditing] = useState(false)
  const [checking, setChecking] = useState(false)

  return (
    <section className="page">
      <p><Link to="/boyahane/despatches">← İrsaliyelere dön</Link></p>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error ?? changeState.error} />
      {despatch && (
        <>
          <header className="page__header">
            <div>
              <h1>{despatch.despatchNo} <StatusBadge status={despatch.status} /></h1>
              <p className="muted">
                {despatch.customerName} · {formatDate(despatch.despatchDate)}
                {despatch.vehiclePlate && ` · ${despatch.vehiclePlate}`}
                {despatch.driverName && ` · ${despatch.driverName}`}
              </p>
              {despatch.deliveryAddress && <p>{despatch.deliveryAddress}</p>}
              {despatch.invoiceId && <p>Fatura: <Link to={`/boyahane/invoices/${despatch.invoiceId}`}>{despatch.invoiceNo}</Link></p>}
              {despatch.notes && <p>{despatch.notes}</p>}
            </div>
            <div className="page__actions">
              <Link className="button" to={`/boyahane/despatches/${id}/print`}><Printer aria-hidden="true" />Yazdır</Link>
              {despatch.status !== 'CANCELLED' && despatch.packages.length > 0 && (
                <button type="button" className="button" onClick={() => setChecking(true)}><ScanBarcode aria-hidden="true" />Yükleme kontrolü</button>
              )}
              {canManage && despatch.status === 'DRAFT' && <button type="button" className="button" onClick={() => setEditing(true)}><Pencil aria-hidden="true" />Düzenle</button>}
              {canManage && despatch.status === 'DRAFT' && <button type="button" className="button button--primary" disabled={changeState.isLoading} onClick={() => change({ id, action: 'confirm' })}><CheckCheck aria-hidden="true" />Onayla</button>}
              {canManage && despatch.status === 'CONFIRMED' && !despatch.invoiceId && <button type="button" className="button" disabled={changeState.isLoading} onClick={() => change({ id, action: 'reopen' })}><RotateCcw aria-hidden="true" />Taslağa al</button>}
              {canManage && despatch.status === 'DRAFT' && <button type="button" className="button button--danger-ghost" disabled={changeState.isLoading} onClick={() => change({ id, action: 'cancel' })}><XCircle aria-hidden="true" />İptal et</button>}
            </div>
          </header>
          <h2 className="section-title">Çeki listeleri</h2>
          <div className="table-wrap">
            <table className="table">
              <thead><tr><th>Çeki no</th><th>Sipariş</th><th className="table__number">Paket</th><th className="table__number">Kilo</th><th className="table__number">Metre</th></tr></thead>
              <tbody>
                {despatch.packingLists.map((list) => (
                  <tr key={list.id}>
                    <td><Link to={`/boyahane/packing-lists/${list.id}`}><code>{list.packingNo}</code></Link></td>
                    <td>{list.orderNo}</td>
                    <td className="table__number">{list.packageCount}</td>
                    <td className="table__number">{formatQuantity(list.kilograms)}</td>
                    <td className="table__number">{formatQuantity(list.meters)}</td>
                  </tr>
                ))}
              </tbody>
              <tfoot>
                <tr>
                  <th colSpan={2} scope="row">Toplam</th>
                  <td className="table__number">{despatch.totalPackages}</td>
                  <td className="table__number">{formatQuantity(despatch.totalKilograms)}</td>
                  <td className="table__number">{formatQuantity(despatch.totalMeters)}</td>
                </tr>
              </tfoot>
            </table>
          </div>
          {editing && <DespatchDialog despatch={despatch} onClose={() => setEditing(false)} />}
          {checking && <LoadingCheckDialog despatch={despatch} onClose={() => setChecking(false)} />}
        </>
      )}
    </section>
  )
}

/**
 * Yükleme kontrolü: araca yüklenen paketlerin etiket barkodu okutulur; irsaliyedeki paketlerle karşılaştırılır.
 * Kayıt tutulmaz, pencere kapanınca okutmalar silinir.
 */
function LoadingCheckDialog({ despatch, onClose }: { despatch: Despatch; onClose: () => void }) {
  const [scan, setScan] = useState('')
  const [scanned, setScanned] = useState<string[]>([])
  const [last, setLast] = useState<{ ok: boolean; text: string } | null>(null)
  const expected = new Map(despatch.packages.map((item) => [item.packageNo.toUpperCase(), item]))
  const scannedSet = new Set(scanned)
  const loaded = despatch.packages.filter((item) => scannedSet.has(item.packageNo.toUpperCase()))
  const missing = despatch.packages.filter((item) => !scannedSet.has(item.packageNo.toUpperCase()))
  const extra = scanned.filter((code) => !expected.has(code))

  const handleScan = (event: FormEvent) => {
    event.preventDefault()
    const code = scan.trim().toUpperCase()
    setScan('')
    if (!code) {
      return
    }
    if (scannedSet.has(code)) {
      setLast({ ok: false, text: `${code} daha önce okutuldu.` })
      return
    }
    setScanned([...scanned, code])
    const item = expected.get(code)
    setLast(item
      ? { ok: true, text: `${code} yüklendi (${item.packingNo} · ${formatQuantity(item.kilograms)} kg).` }
      : { ok: false, text: `${code} bu irsaliyede yok; araçtan indirin.` })
  }

  return (
    <Modal title={`Yükleme kontrolü: ${despatch.despatchNo}`} onClose={onClose} variant="drawer">
      <form className="form" onSubmit={handleScan}>
        <label className="field">
          <span>Paket barkodu</span>
          <input autoFocus autoComplete="off" spellCheck={false} placeholder="Etiketi okutun ya da paket no yazın" value={scan} onChange={(event) => setScan(event.target.value)} />
        </label>
        {last && <div className={`alert alert--${last.ok ? 'ok' : 'error'}`} role="status">{last.text}</div>}
        <p>
          <strong>Okutulan {loaded.length} / {despatch.packages.length}</strong>
          {' · '}Eksik {missing.length}
          {' · '}Fazla {extra.length}
          {missing.length === 0 && extra.length === 0 && <span className="badge badge--ok">Yükleme tam</span>}
        </p>
        {extra.length > 0 && (
          <>
            <h3 className="section-title">Fazla okutulan</h3>
            <div className="table-wrap">
              <table className="table table--compact">
                <thead><tr><th>Barkod</th></tr></thead>
                <tbody>{extra.map((code) => <tr key={code}><td><code>{code}</code> <span className="badge badge--danger">İrsaliyede yok</span></td></tr>)}</tbody>
              </table>
            </div>
          </>
        )}
        <h3 className="section-title">Eksik paketler</h3>
        <div className="table-wrap batch-candidates">
          <table className="table table--compact">
            <thead><tr><th>Paket no</th><th>Çeki</th><th>Top / lot</th><th>Kart</th><th className="table__number">Kilo</th></tr></thead>
            <tbody>
              {missing.map((item) => (
                <tr key={item.id}>
                  <td><code>{item.packageNo}</code></td>
                  <td>{item.packingNo}</td>
                  <td>{item.itemNo}</td>
                  <td>{item.cardCode}</td>
                  <td className="table__number">{formatQuantity(item.kilograms)}</td>
                </tr>
              ))}
              {missing.length === 0 && <tr><td colSpan={5} className="table__empty">Eksik paket yok.</td></tr>}
            </tbody>
          </table>
        </div>
        <footer className="form__actions">
          <button type="button" className="button" disabled={scanned.length === 0} onClick={() => { setScanned([]); setLast(null) }}>Sıfırla</button>
          <button type="button" className="button button--primary" onClick={onClose}>Kapat</button>
        </footer>
      </form>
    </Modal>
  )
}

export function DespatchPrintPage() {
  const id = Number(useParams().id)
  const { data: despatch, error, isLoading } = useGetDespatchQuery(id)
  return (
    <section className="page label-page">
      <style>{'@page { size: A4 portrait; margin: 12mm; }'}</style>
      <div className="no-print">
        <p><Link to={`/boyahane/despatches/${id}`}>← İrsaliyeye dön</Link></p>
        <header className="page__header">
          <div><h1>İrsaliye</h1><p className="muted">Bu çıktı resmî e-İrsaliye değildir.</p></div>
          <button type="button" className="button button--primary" disabled={!despatch} onClick={() => window.print()}><Printer aria-hidden="true" />Yazdır</button>
        </header>
        {isLoading && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={error} />
      </div>
      {despatch && <DespatchSheet despatch={despatch} />}
    </section>
  )
}

/** Basılan irsaliye; sabit metinler müşterinin belge dilindedir. Müşteri kodları satırda varsa basılır. */
function DespatchSheet({ despatch }: { despatch: Despatch }) {
  const t = documentTexts(despatch.documentLanguage)
  const hasCustomerOrderNo = despatch.lines.some((line) => line.customerOrderNo)
  const hasCustomerFabricCode = despatch.lines.some((line) => line.customerFabricCode)
  const hasColorCode = despatch.lines.some((line) => line.colorCode)
  const leading = 3 + (hasCustomerOrderNo ? 1 : 0) + (hasCustomerFabricCode ? 1 : 0) + (hasColorCode ? 1 : 0)

  return (
    <article lang={t.lang} className="batch-card packing-sheet">
      <header className="batch-card__header">
        <div>
          <div className="batch-card__number">{t('despatch')} {despatch.despatchNo}</div>
          <div>{despatch.customerName}{despatch.customerTaxNumber && ` · ${t('taxNumber')} ${despatch.customerTaxNumber}`}</div>
          <div>
            {t('date')} {formatDate(despatch.despatchDate)}
            {despatch.vehiclePlate && ` · ${t('vehicle')} ${despatch.vehiclePlate}`}
            {despatch.driverName && ` · ${t('driver')} ${despatch.driverName}`}
          </div>
          {despatch.deliveryAddress && <div>{t('deliveryAddress')}: {despatch.deliveryAddress}</div>}
        </div>
        <Barcode text={despatch.despatchNo} />
      </header>
      <table className="batch-card__table">
        <thead>
          <tr>
            <th>{t('orderNo')}</th>
            {hasCustomerOrderNo && <th>{t('customerOrderNo')}</th>}
            <th>{t('article')}</th>
            {hasCustomerFabricCode && <th>{t('customerFabricCode')}</th>}
            <th>{t('color')}</th>
            {hasColorCode && <th>{t('colorCode')}</th>}
            <th>{t('package')}</th>
            <th>{t('carton')}</th>
            <th>{t('kilograms')}</th>
            <th>{t('meters')}</th>
          </tr>
        </thead>
        <tbody>
          {despatch.lines.map((line, index) => (
            <tr key={index}>
              <td>{line.orderNo}</td>
              {hasCustomerOrderNo && <td>{line.customerOrderNo ?? '—'}</td>}
              <td>{line.cardCode} {line.cardName}</td>
              {hasCustomerFabricCode && <td>{line.customerFabricCode ?? '—'}</td>}
              <td>{line.colorName ?? '—'}</td>
              {hasColorCode && <td>{line.colorCode ?? '—'}</td>}
              <td>{line.packageCount}</td>
              <td>{line.boxCount}</td>
              <td>{formatQuantity(line.kilograms)}</td>
              <td>{line.meters > 0 ? formatQuantity(line.meters) : '—'}</td>
            </tr>
          ))}
        </tbody>
        <tfoot>
          <tr>
            <th colSpan={leading}>{t('total')}</th>
            <th>{despatch.totalPackages}</th>
            <th>{despatch.lines.reduce((sum, line) => sum + line.boxCount, 0)}</th>
            <th>{formatQuantity(despatch.totalKilograms)}</th>
            <th>{formatQuantity(despatch.totalMeters)}</th>
          </tr>
        </tfoot>
      </table>
      {despatch.notes && <p>{t('notes')}: {despatch.notes}</p>}
      <p>{t('notOfficial')}</p>
    </article>
  )
}

function InvoiceDialog({ invoice, onClose }: { invoice?: Invoice; onClose: () => void }) {
  const navigate = useNavigate()
  const { data: candidates, error: loadError, isLoading } = useGetInvoiceCandidatesQuery(invoice?.id)
  const [save, { isLoading: saving, error }] = useSaveInvoiceMutation()
  const [invoiceDate, setInvoiceDate] = useState(invoice?.invoiceDate ?? todayIsoDate())
  const [dueDate, setDueDate] = useState(invoice?.dueDate ?? '')
  const [vatRate, setVatRate] = useState(invoice?.vatRate === null || invoice?.vatRate === undefined ? '' : String(invoice.vatRate))
  const [withholding, setWithholding] = useState(invoice?.withholdingPercent === null || invoice?.withholdingPercent === undefined ? '' : String(invoice.withholdingPercent))
  const [notes, setNotes] = useState(invoice?.notes ?? '')
  const [prices, setPrices] = useState<Record<number, string>>({})
  const { isSelected, selected, customers, toggle, activeCustomer } = useCustomerSelection(candidates)

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await save({
      id: invoice?.id,
      body: {
        customerId: activeCustomer ?? 0,
        invoiceDate,
        despatchIds: selected.map((candidate) => candidate.id),
        dueDate: dueDate || null,
        vatRate: vatRate.trim() === '' ? null : Number(vatRate),
        withholdingPercent: withholding.trim() === '' ? null : Number(withholding),
        notes: notes.trim() || null,
        prices: Object.entries(prices).map(([orderLineId, value]) => ({ orderLineId: Number(orderLineId), unitPrice: value.trim() === '' ? null : Number(value) })),
      },
    })
    if ('data' in result && result.data) {
      if (invoice) {
        onClose()
      } else {
        navigate(`/boyahane/invoices/${result.data.id}`)
      }
    }
  }

  return (
    <Modal title={invoice ? `Fatura: ${invoice.invoiceNo}` : 'Yeni fatura'} onClose={onClose} variant="drawer">
      <form className="form" onSubmit={handleSubmit}>
        <div className="form__pair">
          <label className="field"><span>Fatura tarihi</span><input type="date" required value={invoiceDate} onChange={(event) => setInvoiceDate(event.target.value)} /></label>
          <label className="field"><span>Vade</span><input type="date" value={dueDate} onChange={(event) => setDueDate(event.target.value)} /></label>
        </div>
        <div className="form__pair">
          <label className="field"><span>KDV oranı (%)</span><input type="number" min="0" max="100" step="0.01" placeholder="Boşsa müşteri oranı" value={vatRate} onChange={(event) => setVatRate(event.target.value)} /></label>
          <label className="field"><span>Tevkifat (KDV'nin %'si)</span><input type="number" min="0" max="100" step="0.01" placeholder="5/10 için 50; yoksa boş" value={withholding} onChange={(event) => setWithholding(event.target.value)} /></label>
        </div>
        <label className="field"><span>Not</span><input maxLength={1000} value={notes} onChange={(event) => setNotes(event.target.value)} /></label>
        {isLoading && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={loadError} />
        {candidates && candidates.length === 0 && <p className="muted">Faturalanacak onaylı irsaliye yok.</p>}
        {customers.map((customerId) => {
          const items = (candidates ?? []).filter((candidate) => candidate.customerId === customerId)
          return (
            <fieldset key={customerId} className="form__section" disabled={activeCustomer !== null && activeCustomer !== customerId}>
              <legend>{items[0].customerName}</legend>
              <div className="table-wrap batch-candidates">
                <table className="table">
                  <thead><tr><th aria-label="Seç" /><th>İrsaliye</th><th>Tarih</th><th className="table__number">Kilo</th><th className="table__number">Metre</th></tr></thead>
                  <tbody>
                    {items.map((candidate) => (
                      <tr key={candidate.id}>
                        <td><input type="checkbox" aria-label={`${candidate.despatchNo} seç`} checked={isSelected(candidate)} onChange={(event) => toggle(candidate.id, event.target.checked)} /></td>
                        <td><code>{candidate.despatchNo}</code></td>
                        <td>{formatDate(candidate.despatchDate)}</td>
                        <td className="table__number">{formatQuantity(candidate.kilograms)}</td>
                        <td className="table__number">{formatQuantity(candidate.meters)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </fieldset>
          )
        })}
        {invoice && invoice.lines.length > 0 && (
          <>
            <h3 className="section-title">Birim fiyatlar</h3>
            <div className="table-wrap">
              <table className="table">
                <thead><tr><th>Satır</th><th className="table__number">Miktar</th><th className="table__number">Birim fiyat ({invoice.currency})</th></tr></thead>
                <tbody>
                  {invoice.lines.map((line) => (
                    <tr key={line.id}>
                      <td>{line.description}</td>
                      <td className="table__number">{formatQuantity(line.quantity)} {orderUnitLabels[line.unit]}</td>
                      <td className="table__number">
                        <input aria-label={`${line.description} birim fiyat`} className="packing-box" type="number" min="0" step="0.0001" value={prices[line.orderLineId] ?? (line.unitPrice === null ? '' : String(line.unitPrice))}
                          onChange={(event) => setPrices({ ...prices, [line.orderLineId]: event.target.value })} />
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </>
        )}
        <ErrorAlert error={error} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={saving || selected.length === 0}>Kaydet</button>
        </footer>
      </form>
    </Modal>
  )
}

export function InvoicesPage() {
  const canManage = usePermission(Permissions.BoyahaneInvoiceManage)
  const grid = useGrid()
  const { page, setPage } = grid
  const [input, setInput] = useState('')
  const [search, setSearch] = useState('')
  const [creating, setCreating] = useState(false)
  const { data, error, isLoading, isFetching } = useGetInvoicesQuery({ page, pageSize: PAGE_SIZE, search: search || undefined, ...grid.params })
  const columns: GridColumn[] = [
    { key: 'invoiceNo', label: 'Fatura no' },
    { key: 'customerName', label: 'Müşteri' },
    { key: 'invoiceDate', label: 'Tarih', filter: false },
    { key: 'dueDate', label: 'Vade', filter: false },
    { key: 'subtotal', label: 'Matrah', numeric: true },
    statusColumn,
  ]
  const submit = (event: FormEvent) => {
    event.preventDefault()
    setPage(1)
    setSearch(input.trim())
  }

  return (
    <ListShell title="Faturalar" placeholder="Fatura no, müşteri ya da müşteri sipariş no" canManage={canManage} addLabel="Yeni fatura" onAdd={() => setCreating(true)}
      search={{ input, setInput, submit, clear: <ClearFiltersButton grid={grid} /> }}>
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
                    <td><Link to={`/boyahane/invoices/${row.id}`}><code>{row.invoiceNo}</code></Link></td>
                    <td>{row.customerName}</td>
                    <td>{formatDate(row.invoiceDate)}</td>
                    <td>{row.dueDate ? formatDate(row.dueDate) : '—'}</td>
                    <td className="table__number">{formatMoney(row.subtotal, row.currency)}</td>
                    <td><StatusBadge status={row.status} /></td>
                  </tr>
                ))}
                {data.items.length === 0 && <tr><td colSpan={columns.length} className="table__empty">Fatura bulunamadı.</td></tr>}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}
      {creating && <InvoiceDialog onClose={() => setCreating(false)} />}
    </ListShell>
  )
}

function InvoiceTotals({ invoice }: { invoice: Invoice }) {
  if (invoice.isOpening) {
    return <tr><th colSpan={5} scope="row">Açılış borcu</th><td className="table__number"><strong>{formatMoney(invoice.payableAmount, invoice.currency)}</strong></td></tr>
  }

  return (
    <>
      <tr><th colSpan={5} scope="row">Matrah</th><td className="table__number">{formatMoney(invoice.subtotal, invoice.currency)}</td></tr>
      <tr><th colSpan={5} scope="row">KDV {invoice.vatRate === null ? '(oran girilmedi)' : `%${invoice.vatRate}`}</th><td className="table__number">{formatMoney(invoice.vatAmount, invoice.currency)}</td></tr>
      {invoice.withholdingPercent !== null && (
        <tr><th colSpan={5} scope="row">Tevkifat (KDV'nin %{invoice.withholdingPercent}'si)</th><td className="table__number">-{formatMoney(invoice.withholdingAmount, invoice.currency)}</td></tr>
      )}
      <tr><th colSpan={5} scope="row">Ödenecek</th><td className="table__number"><strong>{formatMoney(invoice.payableAmount, invoice.currency)}</strong></td></tr>
    </>
  )
}

export function InvoiceDetailPage() {
  const id = Number(useParams().id)
  const canManage = usePermission(Permissions.BoyahaneInvoiceManage)
  const canViewPayments = usePermission(Permissions.BoyahaneAccountView)
  const { data: invoice, error, isLoading } = useGetInvoiceQuery(id)
  const [change, changeState] = useChangeInvoiceMutation()
  const [editing, setEditing] = useState(false)

  return (
    <section className="page">
      <p><Link to="/boyahane/invoices">← Faturalara dön</Link></p>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error ?? changeState.error} />
      {invoice && (
        <>
          <header className="page__header">
            <div>
              <h1>{invoice.invoiceNo} <StatusBadge status={invoice.status} /></h1>
              <p className="muted">
                {invoice.customerName} · {formatDate(invoice.invoiceDate)}
                {invoice.dueDate && ` · Vade ${formatDate(invoice.dueDate)}`} · İrsaliye: {invoice.despatchNos.join(', ') || '—'}
              </p>
              {invoice.notes && <p>{invoice.notes}</p>}
            </div>
            <div className="page__actions">
              {!invoice.isOpening && <Link className="button" to={`/boyahane/invoices/${id}/print`}><Printer aria-hidden="true" />Yazdır</Link>}
              {canManage && invoice.status === 'DRAFT' && <button type="button" className="button" onClick={() => setEditing(true)}><Pencil aria-hidden="true" />Düzenle</button>}
              {canManage && invoice.status === 'DRAFT' && <button type="button" className="button button--primary" disabled={changeState.isLoading} onClick={() => change({ id, action: 'confirm' })}><CheckCheck aria-hidden="true" />Onayla</button>}
              {canManage && invoice.status === 'CONFIRMED' && !invoice.isOpening && <button type="button" className="button" disabled={changeState.isLoading} onClick={() => change({ id, action: 'reopen' })}><RotateCcw aria-hidden="true" />Taslağa al</button>}
              {canManage && invoice.status === 'DRAFT' && <button type="button" className="button button--danger-ghost" disabled={changeState.isLoading} onClick={() => change({ id, action: 'cancel' })}><XCircle aria-hidden="true" />İptal et</button>}
            </div>
          </header>
          <div className="table-wrap">
            <table className="table">
              <thead><tr><th>Sipariş</th><th>Açıklama</th><th className="table__number">Miktar</th><th>Birim</th><th className="table__number">Birim fiyat</th><th className="table__number">Tutar</th></tr></thead>
              <tbody>
                {invoice.lines.map((line) => (
                  <tr key={line.id}>
                    <td>{line.orderNo}{line.customerOrderNo && <div className="muted">{line.customerOrderNo}</div>}</td>
                    <td>{line.description}</td>
                    <td className="table__number">{formatQuantity(line.quantity)}</td>
                    <td>{orderUnitLabels[line.unit]}</td>
                    <td className="table__number">{line.unitPrice === null ? <span className="badge badge--warning">Girilmedi</span> : formatMoney(line.unitPrice, invoice.currency)}</td>
                    <td className="table__number">{line.amount === null ? '—' : formatMoney(line.amount, invoice.currency)}</td>
                  </tr>
                ))}
              </tbody>
              <tfoot><InvoiceTotals invoice={invoice} /></tfoot>
            </table>
          </div>
          {invoice.status === 'CONFIRMED' && (
            <>
              <h2 className="section-title">Tahsilat: {formatMoney(invoice.paidAmount, invoice.currency)} · Kalan: {formatMoney(invoice.payableAmount - invoice.paidAmount, invoice.currency)}</h2>
              {invoice.payments.length > 0 && (
                <div className="table-wrap">
                  <table className="table">
                    <thead><tr><th>Tahsilat</th><th>Tarih</th><th className="table__number">Tutar</th></tr></thead>
                    <tbody>
                      {invoice.payments.map((payment) => (
                        <tr key={payment.paymentId}>
                          <td>{canViewPayments ? <Link to={`/boyahane/payments/${payment.paymentId}`}><code>{payment.paymentNo}</code></Link> : <code>{payment.paymentNo}</code>}</td>
                          <td>{formatDate(payment.paymentDate)}</td>
                          <td className="table__number">{formatMoney(payment.amount, invoice.currency)}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </>
          )}
          {editing && <InvoiceDialog invoice={invoice} onClose={() => setEditing(false)} />}
        </>
      )}
    </section>
  )
}

export function InvoicePrintPage() {
  const id = Number(useParams().id)
  const { data: invoice, error, isLoading } = useGetInvoiceQuery(id)
  return (
    <section className="page label-page">
      <style>{'@page { size: A4 portrait; margin: 12mm; }'}</style>
      <div className="no-print">
        <p><Link to={`/boyahane/invoices/${id}`}>← Faturaya dön</Link></p>
        <header className="page__header">
          <div><h1>Fatura</h1><p className="muted">Bu çıktı resmî e-Fatura değildir.</p></div>
          <button type="button" className="button button--primary" disabled={!invoice} onClick={() => window.print()}><Printer aria-hidden="true" />Yazdır</button>
        </header>
        {isLoading && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={error} />
      </div>
      {invoice && <InvoiceSheet invoice={invoice} />}
    </section>
  )
}

/** Basılan fatura; sabit metinler müşterinin belge dilindedir. Satırda müşteri sipariş no basılır. */
function InvoiceSheet({ invoice }: { invoice: Invoice }) {
  const t = documentTexts(invoice.documentLanguage)
  const hasCustomerOrderNo = invoice.lines.some((line) => line.customerOrderNo)
  const span = 5 + (hasCustomerOrderNo ? 1 : 0)

  return (
    <article lang={t.lang} className="batch-card packing-sheet">
      <header className="batch-card__header">
        <div>
          <div className="batch-card__number">{t('invoice')} {invoice.invoiceNo}</div>
          <div>{invoice.customerName}</div>
          {invoice.customerTaxNumber && <div>{invoice.customerTaxOffice && `${t('taxOffice')} ${invoice.customerTaxOffice} · `}{t('taxNumber')} {invoice.customerTaxNumber}</div>}
          <div>{t('date')} {formatDate(invoice.invoiceDate)}{invoice.dueDate && ` · ${t('dueDate')} ${formatDate(invoice.dueDate)}`}</div>
          <div>{t('despatch')}: {invoice.despatchNos.join(', ')}</div>
        </div>
        <Barcode text={invoice.invoiceNo} />
      </header>
      <table className="batch-card__table">
        <thead>
          <tr>
            <th>{t('orderNo')}</th>
            {hasCustomerOrderNo && <th>{t('customerOrderNo')}</th>}
            <th>{t('description')}</th>
            <th>{t('quantity')}</th>
            <th>{t('unit')}</th>
            <th>{t('unitPrice')}</th>
            <th>{t('amount')}</th>
          </tr>
        </thead>
        <tbody>
          {invoice.lines.map((line) => (
            <tr key={line.id}>
              <td>{line.orderNo}</td>
              {hasCustomerOrderNo && <td>{line.customerOrderNo ?? '—'}</td>}
              <td>{line.description}</td>
              <td>{formatQuantity(line.quantity)}</td>
              <td>{orderUnitLabels[line.unit]}</td>
              <td>{line.unitPrice === null ? '—' : formatMoney(line.unitPrice, invoice.currency)}</td>
              <td>{line.amount === null ? '—' : formatMoney(line.amount, invoice.currency)}</td>
            </tr>
          ))}
        </tbody>
        <tfoot>
          <tr><th colSpan={span} scope="row">{t('subtotal')}</th><td>{formatMoney(invoice.subtotal, invoice.currency)}</td></tr>
          <tr><th colSpan={span} scope="row">{t('vat')} {invoice.vatRate === null ? '' : `%${invoice.vatRate}`}</th><td>{formatMoney(invoice.vatAmount, invoice.currency)}</td></tr>
          {invoice.withholdingPercent !== null && (
            <tr><th colSpan={span} scope="row">{t('withholding')} (%{invoice.withholdingPercent})</th><td>-{formatMoney(invoice.withholdingAmount, invoice.currency)}</td></tr>
          )}
          <tr><th colSpan={span} scope="row">{t('payable')}</th><td><strong>{formatMoney(invoice.payableAmount, invoice.currency)}</strong></td></tr>
        </tfoot>
      </table>
      {invoice.notes && <p>{t('notes')}: {invoice.notes}</p>}
      <p>{t('notOfficial')}</p>
    </article>
  )
}
