import { CheckCheck, Pencil, Plus, Printer, RotateCcw, ScanLine, Search, XCircle } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent, KeyboardEvent, ReactNode } from 'react'
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
import type { DocumentTextKey } from '../../utils/documentText'
import { formatDate, formatQuantity, todayIsoDate } from '../../utils/format'
import { formatMoney } from '../../utils/money'
import { Barcode } from '../labels/LabelPrintPage'
import {
  ormeDocumentStatusPresentation as shippingStatusPresentation,
  ormeMetersText,
  ormeGradeText,
  useChangeOrmeDespatchMutation as useChangeDespatchMutation,
  useChangeOrmeInvoiceMutation as useChangeInvoiceMutation,
  useGetOrmeDespatchCandidatesQuery as useGetDespatchCandidatesQuery,
  useGetOrmeDespatchesQuery as useGetDespatchesQuery,
  useGetOrmeDespatchQuery as useGetDespatchQuery,
  useGetOrmeInvoiceCandidatesQuery as useGetInvoiceCandidatesQuery,
  useGetOrmeInvoiceQuery as useGetInvoiceQuery,
  useGetOrmeInvoicesQuery as useGetInvoicesQuery,
  useSaveOrmeDespatchMutation as useSaveDespatchMutation,
  useSaveOrmeInvoiceMutation as useSaveInvoiceMutation,
} from './ormeShippingApi'
import type { OrmeDespatch as Despatch, OrmeDocumentStatus as ShippingStatus, OrmeInvoice as Invoice } from './ormeShippingApi'

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
        navigate(`/orme/despatches/${result.data.id}`)
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
                  <thead><tr><th aria-label="Seç" /><th>Çeki no</th><th>Sipariş</th><th className="table__number">Top</th><th className="table__number">Kilo</th></tr></thead>
                  <tbody>
                    {items.map((candidate) => (
                      <tr key={candidate.id}>
                        <td><input type="checkbox" aria-label={`${candidate.packingNo} seç`} checked={isSelected(candidate)} onChange={(event) => toggle(candidate.id, event.target.checked)} /></td>
                        <td><code>{candidate.packingNo}</code></td>
                        <td>{candidate.orderNo}</td>
                        <td className="table__number">{candidate.rollCount}</td>
                        <td className="table__number">{formatQuantity(candidate.kilograms)}</td>
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

/** Liste sayfası iskeleti: arama, isteğe bağlı ek süzgeçler, yeni kayıt düğmesi. */
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

export function OrmeDespatchesPage() {
  const canManage = usePermission(Permissions.OrmeShipmentManage)
  const grid = useGrid()
  const { page, setPage } = grid
  const [input, setInput] = useState('')
  const [search, setSearch] = useState('')
  const [creating, setCreating] = useState(false)
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
          <label className="field">
            <span>Başlangıç</span>
            <input type="date" value={from} onChange={(event) => { setPage(1); setFrom(event.target.value) }} />
          </label>
          <label className="field">
            <span>Bitiş</span>
            <input type="date" value={to} onChange={(event) => { setPage(1); setTo(event.target.value) }} />
          </label>
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
                    <td><Link to={`/orme/despatches/${row.id}`}><code>{row.despatchNo}</code></Link></td>
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

/**
 * Yükleme kontrolü: araca yüklenen topların barkodu okutulur; irsaliyenin okutulmamış topları ve irsaliyede olmayan
 * okutmalar gösterilir. Kayıt tutulmaz, pencere kapanınca sıfırlanır.
 */
function LoadingCheckDialog({ despatch, onClose }: { despatch: Despatch; onClose: () => void }) {
  const [input, setInput] = useState('')
  const [scanned, setScanned] = useState<string[]>([])
  const [message, setMessage] = useState<string | null>(null)
  const key = (rollNo: string) => rollNo.trim().toLocaleUpperCase('tr')
  const expected = new Map(despatch.rolls.map((roll) => [key(roll.rollNo), roll]))
  const scannedKeys = new Set(scanned.map(key))
  const loaded = despatch.rolls.filter((roll) => scannedKeys.has(key(roll.rollNo)))
  const missing = despatch.rolls.filter((roll) => !scannedKeys.has(key(roll.rollNo)))
  const extras = scanned.filter((rollNo) => !expected.has(key(rollNo)))

  const handleScan = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key !== 'Enter') {
      return
    }
    event.preventDefault()
    const rollNo = input.trim()
    setInput('')
    if (!rollNo) {
      return
    }
    if (scannedKeys.has(key(rollNo))) {
      setMessage(`${rollNo} zaten okutuldu.`)
      return
    }
    setScanned([...scanned, rollNo])
    setMessage(expected.has(key(rollNo)) ? null : `${rollNo} bu irsaliyede yok.`)
  }

  return (
    <Modal title={`Yükleme kontrolü: ${despatch.despatchNo}`} onClose={onClose} variant="drawer">
      <div className="form">
        <p className="muted">Yüklenen her topun barkodunu okutun. Sonuç kaydedilmez; pencere kapanınca sıfırlanır.</p>
        <label className="field">
          <span>Top okut</span>
          <input autoFocus autoComplete="off" placeholder="Barkodu okutun ya da top no yazıp Enter'a basın" value={input}
            onChange={(event) => setInput(event.target.value)} onKeyDown={handleScan} />
        </label>
        {message && <p role="status" className="field__error">{message}</p>}
        <p>
          <strong>Okutulan: {loaded.length} / {despatch.rolls.length} top · {formatQuantity(loaded.reduce((sum, roll) => sum + roll.kilograms, 0))} / {formatQuantity(despatch.totalKilograms)} kg</strong>
          {missing.length === 0 && extras.length === 0 && despatch.rolls.length > 0 && <> <span className="badge badge--ok">Tamam</span></>}
        </p>
        {extras.length > 0 && (
          <div className="alert alert--error">İrsaliyede olmayan okutma ({extras.length}): {extras.join(', ')}</div>
        )}
        <h3 className="section-title">Eksik ({missing.length})</h3>
        <div className="table-wrap batch-candidates">
          <table className="table table--compact">
            <thead><tr><th>Top</th><th>Çeki no</th><th>Kumaş</th><th className="table__number">Kilo</th></tr></thead>
            <tbody>
              {missing.map((roll) => (
                <tr key={roll.rollNo}>
                  <td><code>{roll.rollNo}</code></td>
                  <td>{roll.packingNo}</td>
                  <td>{roll.fabricCode}</td>
                  <td className="table__number">{formatQuantity(roll.kilograms)}</td>
                </tr>
              ))}
              {missing.length === 0 && <tr><td colSpan={4} className="table__empty">Eksik top yok.</td></tr>}
            </tbody>
          </table>
        </div>
        <h3 className="section-title">Okutulan ({loaded.length})</h3>
        <div className="table-wrap batch-candidates">
          <table className="table table--compact">
            <thead><tr><th>Top</th><th>Çeki no</th><th>Kumaş</th><th className="table__number">Kilo</th></tr></thead>
            <tbody>
              {loaded.map((roll) => (
                <tr key={roll.rollNo}>
                  <td><code>{roll.rollNo}</code></td>
                  <td>{roll.packingNo}</td>
                  <td>{roll.fabricCode}</td>
                  <td className="table__number">{formatQuantity(roll.kilograms)}</td>
                </tr>
              ))}
              {loaded.length === 0 && <tr><td colSpan={4} className="table__empty">Henüz top okutulmadı.</td></tr>}
            </tbody>
          </table>
        </div>
        <footer className="form__actions">
          <button type="button" className="button" disabled={scanned.length === 0} onClick={() => { setScanned([]); setMessage(null) }}>Sıfırla</button>
          <button type="button" className="button button--primary" onClick={onClose}>Kapat</button>
        </footer>
      </div>
    </Modal>
  )
}

export function OrmeDespatchDetailPage() {
  const id = Number(useParams().id)
  const canManage = usePermission(Permissions.OrmeShipmentManage)
  const { data: despatch, error, isLoading } = useGetDespatchQuery(id)
  const [change, changeState] = useChangeDespatchMutation()
  const [editing, setEditing] = useState(false)
  const [checking, setChecking] = useState(false)

  return (
    <section className="page">
      <p><Link to="/orme/despatches">← İrsaliyelere dön</Link></p>
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
              {despatch.invoiceId && <p>Fatura: <Link to={`/orme/invoices/${despatch.invoiceId}`}>{despatch.invoiceNo}</Link></p>}
              {despatch.notes && <p>{despatch.notes}</p>}
            </div>
            <div className="page__actions">
              <Link className="button" to={`/orme/despatches/${id}/print`}><Printer aria-hidden="true" />Yazdır</Link>
              {despatch.status !== 'CANCELLED' && despatch.rolls.length > 0 && <button type="button" className="button" onClick={() => setChecking(true)}><ScanLine aria-hidden="true" />Yükleme kontrolü</button>}
              {canManage && despatch.status === 'DRAFT' && <button type="button" className="button" onClick={() => setEditing(true)}><Pencil aria-hidden="true" />Düzenle</button>}
              {canManage && despatch.status === 'DRAFT' && <button type="button" className="button button--primary" disabled={changeState.isLoading} onClick={() => change({ id, action: 'confirm' })}><CheckCheck aria-hidden="true" />Onayla</button>}
              {canManage && despatch.status === 'CONFIRMED' && !despatch.invoiceId && <button type="button" className="button" disabled={changeState.isLoading} onClick={() => change({ id, action: 'reopen' })}><RotateCcw aria-hidden="true" />Taslağa al</button>}
              {canManage && despatch.status === 'DRAFT' && <button type="button" className="button button--danger-ghost" disabled={changeState.isLoading} onClick={() => change({ id, action: 'cancel' })}><XCircle aria-hidden="true" />İptal et</button>}
            </div>
          </header>
          <h2 className="section-title">Çeki listeleri</h2>
          <div className="table-wrap">
            <table className="table">
              <thead><tr><th>Çeki no</th><th>Sipariş</th><th className="table__number">Top</th><th className="table__number">Kilo</th></tr></thead>
              <tbody>
                {despatch.packingLists.map((list) => (
                  <tr key={list.id}>
                    <td><Link to={`/orme/packing-lists/${list.id}`}><code>{list.packingNo}</code></Link></td>
                    <td>{list.orderNo}</td>
                    <td className="table__number">{list.rollCount}</td>
                    <td className="table__number">{formatQuantity(list.kilograms)}</td>
                  </tr>
                ))}
              </tbody>
              <tfoot>
                <tr>
                  <th colSpan={2} scope="row">Toplam</th>
                  <td className="table__number">{despatch.totalRolls}</td>
                  <td className="table__number">{formatQuantity(despatch.totalKilograms)}</td>
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

/** Sipariş hücresi: bizim sipariş no, altında müşterinin sipariş no ve kumaş kodu. */
function CustomerRefs({ orderNo, customerOrderNo, customerFabricCode, t }: {
  orderNo: string
  customerOrderNo: string | null
  customerFabricCode: string | null
  t: (key: DocumentTextKey) => string
}) {
  return (
    <>
      {orderNo}
      {customerOrderNo && <div>{t('customerOrderNo')}: {customerOrderNo}</div>}
      {customerFabricCode && <div>{t('customerFabricCode')}: {customerFabricCode}</div>}
    </>
  )
}

/** Basılan irsaliye: metinler müşterinin belge dilinde. */
function DespatchSheet({ despatch }: { despatch: Despatch }) {
  const t = documentTexts(despatch.documentLanguage)
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
          <tr><th>{t('orderNo')}</th><th>{t('fabric')}</th><th>{t('quality')}</th><th>{t('rolls')}</th><th>{t('kilograms')}</th><th>{t('meters')}</th></tr>
        </thead>
        <tbody>
          {despatch.lines.map((line, index) => (
            <tr key={index}>
              <td><CustomerRefs orderNo={line.orderNo} customerOrderNo={line.customerOrderNo} customerFabricCode={line.customerFabricCode} t={t} /></td>
              <td>{line.fabricCode} {line.fabricName}</td>
              <td>{ormeGradeText(t, line.grade)}</td>
              <td>{line.rollCount}</td>
              <td>{formatQuantity(line.kilograms)}</td>
              <td>{ormeMetersText(line.meters, line.metersEstimated)}</td>
            </tr>
          ))}
        </tbody>
        <tfoot>
          <tr>
            <th colSpan={3}>{t('total')}</th>
            <th>{despatch.totalRolls}</th>
            <th>{formatQuantity(despatch.totalKilograms)}</th>
            <th />
          </tr>
        </tfoot>
      </table>
      {despatch.notes && <p>{despatch.notes}</p>}
      <p className="muted">{t('notOfficial')}</p>
    </article>
  )
}

export function OrmeDespatchPrintPage() {
  const id = Number(useParams().id)
  const { data: despatch, error, isLoading } = useGetDespatchQuery(id)
  return (
    <section className="page label-page">
      <style>{'@page { size: A4 portrait; margin: 12mm; }'}</style>
      <div className="no-print">
        <p><Link to={`/orme/despatches/${id}`}>← İrsaliyeye dön</Link></p>
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

function InvoiceDialog({ invoice, onClose }: { invoice?: Invoice; onClose: () => void }) {
  const navigate = useNavigate()
  const { data: candidates, error: loadError, isLoading } = useGetInvoiceCandidatesQuery(invoice?.id)
  const [save, { isLoading: saving, error }] = useSaveInvoiceMutation()
  const [invoiceDate, setInvoiceDate] = useState(invoice?.invoiceDate ?? todayIsoDate())
  const [dueDate, setDueDate] = useState(invoice?.dueDate ?? '')
  const [vatRate, setVatRate] = useState(invoice?.vatRate === null || invoice?.vatRate === undefined ? '' : String(invoice.vatRate))
  const [withholding, setWithholding] = useState(invoice?.withholdingPercent === null || invoice?.withholdingPercent === undefined ? '' : String(invoice.withholdingPercent))
  const [notes, setNotes] = useState(invoice?.notes ?? '')
  const [prices, setPrices] = useState<Record<string, { unitPrice?: string; discountPercent?: string }>>({})
  const keyOf = (line: { orderLineId: number; grade: string }) => `${line.orderLineId}:${line.grade}`
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
        prices: (invoice?.lines ?? []).filter((line) => prices[keyOf(line)]).map((line) => {
          const change = prices[keyOf(line)]
          const unitPrice = change.unitPrice ?? (line.unitPrice === null ? '' : String(line.unitPrice))
          const discount = change.discountPercent ?? (line.discountPercent === null ? '' : String(line.discountPercent))
          return {
            orderLineId: line.orderLineId,
            grade: line.grade,
            unitPrice: unitPrice.trim() === '' ? null : Number(unitPrice),
            discountPercent: discount.trim() === '' ? null : Number(discount),
          }
        }),
      },
    })
    if ('data' in result && result.data) {
      if (invoice) {
        onClose()
      } else {
        navigate(`/orme/invoices/${result.data.id}`)
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
                  <thead><tr><th aria-label="Seç" /><th>İrsaliye</th><th>Tarih</th><th className="table__number">Kilo</th></tr></thead>
                  <tbody>
                    {items.map((candidate) => (
                      <tr key={candidate.id}>
                        <td><input type="checkbox" aria-label={`${candidate.despatchNo} seç`} checked={isSelected(candidate)} onChange={(event) => toggle(candidate.id, event.target.checked)} /></td>
                        <td><code>{candidate.despatchNo}</code></td>
                        <td>{formatDate(candidate.despatchDate)}</td>
                        <td className="table__number">{formatQuantity(candidate.kilograms)}</td>
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
            <h3 className="section-title">Fiyat ve indirim</h3>
            <div className="table-wrap">
              <table className="table">
                <thead><tr><th>Satır</th><th className="table__number">Kilo</th><th className="table__number">Kilo fiyatı ({invoice.currency})</th><th className="table__number">İndirim (%)</th></tr></thead>
                <tbody>
                  {invoice.lines.map((line) => (
                    <tr key={line.id}>
                      <td>{line.description}</td>
                      <td className="table__number">{formatQuantity(line.quantity)}</td>
                      <td className="table__number">
                        <input aria-label={`${line.description} birim fiyat`} className="packing-box" type="number" min="0" step="0.0001" value={prices[keyOf(line)]?.unitPrice ?? (line.unitPrice === null ? '' : String(line.unitPrice))}
                          onChange={(event) => setPrices({ ...prices, [keyOf(line)]: { ...prices[keyOf(line)], unitPrice: event.target.value } })} />
                      </td>
                      <td className="table__number">
                        <input aria-label={`${line.description} indirim`} className="packing-box" type="number" min="0" max="100" step="0.01" value={prices[keyOf(line)]?.discountPercent ?? (line.discountPercent === null ? '' : String(line.discountPercent))}
                          onChange={(event) => setPrices({ ...prices, [keyOf(line)]: { ...prices[keyOf(line)], discountPercent: event.target.value } })} />
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

export function OrmeInvoicesPage() {
  const canManage = usePermission(Permissions.OrmeInvoiceManage)
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
    <ListShell title="Faturalar" placeholder="Fatura no ya da müşteri" canManage={canManage} addLabel="Yeni fatura" onAdd={() => setCreating(true)}
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
                    <td><Link to={`/orme/invoices/${row.id}`}><code>{row.invoiceNo}</code></Link></td>
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

export function OrmeInvoiceDetailPage() {
  const id = Number(useParams().id)
  const canManage = usePermission(Permissions.OrmeInvoiceManage)
  const canViewPayments = usePermission(Permissions.OrmeAccountView)
  const { data: invoice, error, isLoading } = useGetInvoiceQuery(id)
  const [change, changeState] = useChangeInvoiceMutation()
  const [editing, setEditing] = useState(false)

  return (
    <section className="page">
      <p><Link to="/orme/invoices">← Faturalara dön</Link></p>
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
              {!invoice.isOpening && <Link className="button" to={`/orme/invoices/${id}/print`}><Printer aria-hidden="true" />Yazdır</Link>}
              {canManage && invoice.status === 'DRAFT' && <button type="button" className="button" onClick={() => setEditing(true)}><Pencil aria-hidden="true" />Düzenle</button>}
              {canManage && invoice.status === 'DRAFT' && <button type="button" className="button button--primary" disabled={changeState.isLoading} onClick={() => change({ id, action: 'confirm' })}><CheckCheck aria-hidden="true" />Onayla</button>}
              {canManage && invoice.status === 'CONFIRMED' && !invoice.isOpening && <button type="button" className="button" disabled={changeState.isLoading} onClick={() => change({ id, action: 'reopen' })}><RotateCcw aria-hidden="true" />Taslağa al</button>}
              {canManage && invoice.status === 'DRAFT' && <button type="button" className="button button--danger-ghost" disabled={changeState.isLoading} onClick={() => change({ id, action: 'cancel' })}><XCircle aria-hidden="true" />İptal et</button>}
            </div>
          </header>
          <div className="table-wrap">
            <table className="table">
              <thead><tr><th>Sipariş</th><th>Açıklama</th><th className="table__number">Kilo</th><th className="table__number">Kilo fiyatı</th><th className="table__number">İndirim</th><th className="table__number">Tutar</th></tr></thead>
              <tbody>
                {invoice.lines.map((line) => (
                  <tr key={line.id}>
                    <td>{line.orderNo}</td>
                    <td>{line.description}</td>
                    <td className="table__number">{formatQuantity(line.quantity)}</td>
                    <td className="table__number">{line.unitPrice === null ? <span className="badge badge--warning">Girilmedi</span> : formatMoney(line.unitPrice, invoice.currency)}</td>
                    <td className="table__number">{line.discountPercent === null ? '—' : `%${line.discountPercent}`}</td>
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
                          <td>{canViewPayments ? <Link to={`/orme/payments/${payment.paymentId}`}><code>{payment.paymentNo}</code></Link> : <code>{payment.paymentNo}</code>}</td>
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

/** Basılan fatura: metinler müşterinin belge dilinde; açılış bakiyesi basılmaz. */
function InvoiceSheet({ invoice }: { invoice: Invoice }) {
  const t = documentTexts(invoice.documentLanguage)
  const total = (label: string, value: string, strong = false) => (
    <tr><th colSpan={5} scope="row">{label}</th><td>{strong ? <strong>{value}</strong> : value}</td></tr>
  )
  return (
    <article lang={t.lang} className="batch-card packing-sheet">
      <header className="batch-card__header">
        <div>
          <div className="batch-card__number">{t('invoice')} {invoice.invoiceNo}</div>
          <div>{invoice.customerName}</div>
          {invoice.customerTaxNumber && <div>{invoice.customerTaxOffice} · {t('taxNumber')} {invoice.customerTaxNumber}</div>}
          <div>{t('date')} {formatDate(invoice.invoiceDate)}{invoice.dueDate && ` · ${t('dueDate')} ${formatDate(invoice.dueDate)}`}</div>
          <div>{t('despatch')}: {invoice.despatchNos.join(', ')}</div>
        </div>
        <Barcode text={invoice.invoiceNo} />
      </header>
      <table className="batch-card__table">
        <thead>
          <tr><th>{t('orderNo')}</th><th>{t('description')}</th><th>{t('kilograms')}</th><th>{t('unitPrice')}</th><th>{t('discount')}</th><th>{t('amount')}</th></tr>
        </thead>
        <tbody>
          {invoice.lines.map((line) => (
            <tr key={line.id}>
              <td><CustomerRefs orderNo={line.orderNo} customerOrderNo={line.customerOrderNo} customerFabricCode={line.customerFabricCode} t={t} /></td>
              <td>{line.description}</td>
              <td>{formatQuantity(line.quantity)}</td>
              <td>{line.unitPrice === null ? '—' : formatMoney(line.unitPrice, invoice.currency)}</td>
              <td>{line.discountPercent === null ? '—' : `%${line.discountPercent}`}</td>
              <td>{line.amount === null ? '—' : formatMoney(line.amount, invoice.currency)}</td>
            </tr>
          ))}
        </tbody>
        <tfoot>
          {total(t('subtotal'), formatMoney(invoice.subtotal, invoice.currency))}
          {total(`${t('vat')}${invoice.vatRate === null ? '' : ` %${invoice.vatRate}`}`, formatMoney(invoice.vatAmount, invoice.currency))}
          {invoice.withholdingPercent !== null && total(`${t('withholding')} (%${invoice.withholdingPercent})`, `-${formatMoney(invoice.withholdingAmount, invoice.currency)}`)}
          {total(t('payable'), formatMoney(invoice.payableAmount, invoice.currency), true)}
        </tfoot>
      </table>
      {invoice.notes && <p>{invoice.notes}</p>}
      <p className="muted">{t('notOfficial')}</p>
    </article>
  )
}

export function OrmeInvoicePrintPage() {
  const id = Number(useParams().id)
  const { data: invoice, error, isLoading } = useGetInvoiceQuery(id)
  return (
    <section className="page label-page">
      <style>{'@page { size: A4 portrait; margin: 12mm; }'}</style>
      <div className="no-print">
        <p><Link to={`/orme/invoices/${id}`}>← Faturaya dön</Link></p>
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
