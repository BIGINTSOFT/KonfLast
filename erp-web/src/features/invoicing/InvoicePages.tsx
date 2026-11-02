import { CheckCheck, Pencil, Plus, Printer, RotateCcw, Search, XCircle } from 'lucide-react'
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
import { formatDate, formatQuantity, todayIsoDate } from '../../utils/format'
import { formatMoney } from '../../utils/money'
import { documentTexts } from '../../utils/documentText'
import { Barcode } from '../labels/LabelPrintPage'
import {
  invoiceStatusPresentation,
  useChangeInvoiceMutation,
  useGetInvoiceCandidatesQuery,
  useGetInvoiceQuery,
  useGetInvoicesQuery,
  useSaveInvoiceMutation,
} from './invoicingApi'
import type { Invoice, InvoiceLine, InvoiceStatus } from './invoicingApi'

const PAGE_SIZE = 20

function StatusBadge({ status }: { status: InvoiceStatus }) {
  const presentation = invoiceStatusPresentation[status]
  return <span className={`badge badge--${presentation.tone}`}>{presentation.label}</span>
}

const lineKey = (line: Pick<InvoiceLine, 'stockItemId' | 'salesOrderLineId'>) => `${line.stockItemId}:${line.salesOrderLineId ?? ''}`
const text = (value: number | null) => (value === null ? '' : String(value))

/** Yeni fatura: sevk edilmiş sevkiyatlar müşteri müşteri gruplanır, yalnızca bir müşteriden seçilebilir. Düzenlemede fiyat, iskonto ve KDV değiştirilir. */
function InvoiceDialog({ invoice, onClose }: { invoice?: Invoice; onClose: () => void }) {
  const navigate = useNavigate()
  const { data: candidates, error: loadError, isLoading } = useGetInvoiceCandidatesQuery(invoice?.id)
  const [save, { isLoading: saving, error }] = useSaveInvoiceMutation()
  const [invoiceDate, setInvoiceDate] = useState(invoice?.invoiceDate ?? todayIsoDate())
  const [dueDate, setDueDate] = useState(invoice?.dueDate ?? '')
  const [withholding, setWithholding] = useState(text(invoice?.withholdingPercent ?? null))
  const [notes, setNotes] = useState(invoice?.notes ?? '')
  const [prices, setPrices] = useState<Record<string, { unitPrice?: string; discountPercent?: string; vatRate?: string }>>({})
  const [changes, setChanges] = useState<Map<number, boolean>>(new Map())
  const isSelected = (candidate: { id: number; selected: boolean }) => changes.get(candidate.id) ?? candidate.selected
  const selected = candidates?.filter(isSelected) ?? []
  const customers = [...new Set((candidates ?? []).map((candidate) => candidate.customerId))]
  const activeCustomer = selected[0]?.customerId ?? null
  const edit = (line: InvoiceLine, field: 'unitPrice' | 'discountPercent' | 'vatRate', value: string) =>
    setPrices({ ...prices, [lineKey(line)]: { ...prices[lineKey(line)], [field]: value } })
  const valueOf = (line: InvoiceLine, field: 'unitPrice' | 'discountPercent' | 'vatRate') => prices[lineKey(line)]?.[field] ?? text(line[field])

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await save({
      id: invoice?.id,
      body: {
        customerId: activeCustomer ?? 0,
        invoiceDate,
        shipmentIds: selected.map((candidate) => candidate.id),
        dueDate: dueDate || null,
        withholdingPercent: withholding.trim() === '' ? null : Number(withholding),
        notes: notes.trim() || null,
        prices: (invoice?.lines ?? []).filter((line) => prices[lineKey(line)]).map((line) => {
          const unitPrice = valueOf(line, 'unitPrice')
          const discount = valueOf(line, 'discountPercent')
          return {
            stockItemId: line.stockItemId,
            salesOrderLineId: line.salesOrderLineId,
            unitPrice: unitPrice.trim() === '' ? null : Number(unitPrice),
            discountPercent: discount.trim() === '' ? null : Number(discount),
            vatRate: Number(valueOf(line, 'vatRate') || 0),
          }
        }),
      },
    })
    if ('data' in result && result.data) {
      if (invoice) {
        onClose()
      } else {
        navigate(`/invoices/${result.data.id}`)
      }
    }
  }

  return (
    <Modal title={invoice ? `Fatura: ${invoice.invoiceNo}` : 'Yeni fatura'} onClose={onClose} variant="drawer">
      <form className="form" onSubmit={handleSubmit}>
        <div className="form__pair">
          <label className="field"><span>Fatura tarihi</span><input type="date" required value={invoiceDate} onChange={(event) => setInvoiceDate(event.target.value)} /></label>
          <label className="field"><span>Vade</span><input type="date" value={dueDate} placeholder="Boşsa müşteri vadesi" onChange={(event) => setDueDate(event.target.value)} /></label>
        </div>
        <div className="form__pair">
          <label className="field"><span>Tevkifat (KDV'nin %'si)</span><input type="number" min="0" max="100" step="0.01" placeholder="5/10 için 50; yoksa boş" value={withholding} onChange={(event) => setWithholding(event.target.value)} /></label>
          <label className="field"><span>Not</span><input maxLength={1000} value={notes} onChange={(event) => setNotes(event.target.value)} /></label>
        </div>
        {isLoading && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={loadError} />
        {candidates && candidates.length === 0 && <p className="muted">Faturalanacak sevkiyat yok: yalnızca sevk edilmiş ve faturalanmamış sevkiyatlar listelenir.</p>}
        {customers.map((customerId) => {
          const items = (candidates ?? []).filter((candidate) => candidate.customerId === customerId)
          return (
            <fieldset key={customerId} className="form__section" disabled={activeCustomer !== null && activeCustomer !== customerId}>
              <legend>{items[0].customerName}</legend>
              <div className="table-wrap batch-candidates">
                <table className="table">
                  <thead><tr><th aria-label="Seç" /><th>Sevkiyat</th><th>İrsaliye no</th><th>Sipariş</th><th>Tarih</th><th className="table__number">Adet</th></tr></thead>
                  <tbody>
                    {items.map((candidate) => (
                      <tr key={candidate.id}>
                        <td><input type="checkbox" aria-label={`${candidate.shipmentNo} seç`} checked={isSelected(candidate)} onChange={(event) => setChanges(new Map(changes).set(candidate.id, event.target.checked))} /></td>
                        <td><code>{candidate.shipmentNo}</code></td>
                        <td>{candidate.documentNo ?? '—'}</td>
                        <td>{candidate.salesOrderNo ?? '—'}</td>
                        <td>{formatDate(candidate.shipDate)}</td>
                        <td className="table__number">{formatQuantity(candidate.quantity)}</td>
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
            <h3 className="section-title">Fiyat, iskonto ve KDV</h3>
            <div className="table-wrap">
              <table className="table">
                <thead><tr><th>Satır</th><th className="table__number">Adet</th><th className="table__number">Birim fiyat ({invoice.currency})</th><th className="table__number">İskonto (%)</th><th className="table__number">KDV (%)</th></tr></thead>
                <tbody>
                  {invoice.lines.map((line) => (
                    <tr key={line.id}>
                      <td>{line.description}{line.salesOrderNo && <span className="muted"> · {line.salesOrderNo}</span>}</td>
                      <td className="table__number">{formatQuantity(line.quantity)}</td>
                      <td className="table__number">
                        <input aria-label={`${line.description} birim fiyat`} className="packing-box" type="number" min="0" step="0.0001" value={valueOf(line, 'unitPrice')} onChange={(event) => edit(line, 'unitPrice', event.target.value)} />
                      </td>
                      <td className="table__number">
                        <input aria-label={`${line.description} iskonto`} className="packing-box" type="number" min="0" max="100" step="0.01" value={valueOf(line, 'discountPercent')} onChange={(event) => edit(line, 'discountPercent', event.target.value)} />
                      </td>
                      <td className="table__number">
                        <input aria-label={`${line.description} KDV`} className="packing-box" type="number" min="0" max="100" step="0.01" required value={valueOf(line, 'vatRate')} onChange={(event) => edit(line, 'vatRate', event.target.value)} />
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

export function SalesInvoicesPage() {
  const canManage = usePermission(Permissions.InvoiceManage)
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
    { key: 'status', label: 'Durum', filter: Object.entries(invoiceStatusPresentation).map(([value, { label }]) => ({ value, label })) },
  ]
  const submit = (event: FormEvent) => {
    event.preventDefault()
    setPage(1)
    setSearch(input.trim())
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">Faturalar</h1>
      <form className="toolbar" onSubmit={submit} role="search">
        <input type="search" placeholder="Fatura no ya da müşteri" aria-label="Faturalar ara" value={input} onChange={(event) => setInput(event.target.value)} />
        <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
        <ClearFiltersButton grid={grid} />
        <div className="toolbar__actions">
          {canManage && <button type="button" className="button button--primary" onClick={() => setCreating(true)}><Plus aria-hidden="true" />Yeni fatura</button>}
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
                {data.items.map((row) => (
                  <tr key={row.id}>
                    <td><Link to={`/invoices/${row.id}`}><code>{row.invoiceNo}</code></Link></td>
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
    </section>
  )
}

const LINE_COLUMNS = 7

function InvoiceTotals({ invoice }: { invoice: Invoice }) {
  const span = LINE_COLUMNS - 1
  if (invoice.isOpening) {
    return <tr><th colSpan={span} scope="row">Açılış borcu</th><td className="table__number"><strong>{formatMoney(invoice.payableAmount, invoice.currency)}</strong></td></tr>
  }

  return (
    <>
      <tr><th colSpan={span} scope="row">Matrah</th><td className="table__number">{formatMoney(invoice.subtotal, invoice.currency)}</td></tr>
      <tr><th colSpan={span} scope="row">KDV</th><td className="table__number">{formatMoney(invoice.vatAmount, invoice.currency)}</td></tr>
      {invoice.withholdingPercent !== null && (
        <tr><th colSpan={span} scope="row">Tevkifat (KDV'nin %{invoice.withholdingPercent}'si)</th><td className="table__number">-{formatMoney(invoice.withholdingAmount, invoice.currency)}</td></tr>
      )}
      <tr><th colSpan={span} scope="row">Ödenecek</th><td className="table__number"><strong>{formatMoney(invoice.payableAmount, invoice.currency)}</strong></td></tr>
    </>
  )
}

function LineCells({ line, currency }: { line: InvoiceLine; currency: string }) {
  return (
    <>
      <td>{line.salesOrderNo ?? '—'}</td>
      <td>{line.description}</td>
      <td className="table__number">{formatQuantity(line.quantity)}</td>
      <td className="table__number">{line.unitPrice === null ? <span className="badge badge--warning">Girilmedi</span> : formatMoney(line.unitPrice, currency)}</td>
      <td className="table__number">{line.discountPercent === null ? '—' : `%${line.discountPercent}`}</td>
      <td className="table__number">%{line.vatRate}</td>
      <td className="table__number">{line.amount === null ? '—' : formatMoney(line.amount, currency)}</td>
    </>
  )
}

const lineHead = <thead><tr><th>Sipariş</th><th>Açıklama</th><th className="table__number">Adet</th><th className="table__number">Birim fiyat</th><th className="table__number">İskonto</th><th className="table__number">KDV</th><th className="table__number">Tutar</th></tr></thead>

const shipmentText = (invoice: Invoice) =>
  invoice.shipments.map((shipment) => (shipment.documentNo ? `${shipment.shipmentNo} (${shipment.documentNo})` : shipment.shipmentNo)).join(', ')

export function SalesInvoiceDetailPage() {
  const id = Number(useParams().id)
  const canManage = usePermission(Permissions.InvoiceManage)
  const canViewPayments = usePermission(Permissions.AccountView)
  const canViewShipments = usePermission(Permissions.ShipmentView)
  const { data: invoice, error, isLoading } = useGetInvoiceQuery(id)
  const [change, changeState] = useChangeInvoiceMutation()
  const [editing, setEditing] = useState(false)

  return (
    <section className="page">
      <p><Link to="/invoices">← Faturalara dön</Link></p>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error ?? changeState.error} />
      {invoice && (
        <>
          <header className="page__header">
            <div>
              <h1>{invoice.invoiceNo} <StatusBadge status={invoice.status} /></h1>
              <p className="muted">
                {invoice.customerName} · {formatDate(invoice.invoiceDate)}
                {invoice.dueDate && ` · Vade ${formatDate(invoice.dueDate)}`}
                {!invoice.isOpening && <> · Sevkiyat: {invoice.shipments.length === 0 ? '—' : invoice.shipments.map((shipment, index) => (
                  <span key={shipment.id}>
                    {index > 0 && ', '}
                    {canViewShipments ? <Link to={`/shipments/${shipment.id}`}>{shipment.shipmentNo}</Link> : shipment.shipmentNo}
                    {shipment.documentNo && ` (${shipment.documentNo})`}
                  </span>
                ))}</>}
              </p>
              {invoice.notes && <p>{invoice.notes}</p>}
            </div>
            <div className="page__actions">
              {!invoice.isOpening && <Link className="button" to={`/invoices/${id}/print`}><Printer aria-hidden="true" />Yazdır</Link>}
              {canManage && invoice.status === 'DRAFT' && <button type="button" className="button" onClick={() => setEditing(true)}><Pencil aria-hidden="true" />Düzenle</button>}
              {canManage && invoice.status === 'DRAFT' && <button type="button" className="button button--primary" disabled={changeState.isLoading} onClick={() => change({ id, action: 'confirm' })}><CheckCheck aria-hidden="true" />Onayla</button>}
              {canManage && invoice.status === 'CONFIRMED' && !invoice.isOpening && <button type="button" className="button" disabled={changeState.isLoading} onClick={() => change({ id, action: 'reopen' })}><RotateCcw aria-hidden="true" />Taslağa al</button>}
              {canManage && invoice.status === 'DRAFT' && <button type="button" className="button button--danger-ghost" disabled={changeState.isLoading} onClick={() => change({ id, action: 'cancel' })}><XCircle aria-hidden="true" />İptal et</button>}
            </div>
          </header>
          <div className="table-wrap">
            <table className="table">
              {lineHead}
              <tbody>
                {invoice.lines.map((line) => <tr key={line.id}><LineCells line={line} currency={invoice.currency} /></tr>)}
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
                          <td>{canViewPayments ? <Link to={`/payments/${payment.paymentId}`}><code>{payment.paymentNo}</code></Link> : <code>{payment.paymentNo}</code>}</td>
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

/** Basılı faturanın satır başlığı ve toplamları: müşterinin belge dilinde. */
function PrintLineHead({ t }: { t: ReturnType<typeof documentTexts> }) {
  return (
    <thead>
      <tr>
        <th>{t('order')}</th>
        <th>{t('description')}</th>
        <th className="table__number">{t('pieces')}</th>
        <th className="table__number">{t('unitPrice')}</th>
        <th className="table__number">{t('discount')}</th>
        <th className="table__number">{t('vat')}</th>
        <th className="table__number">{t('amount')}</th>
      </tr>
    </thead>
  )
}

function PrintTotals({ invoice, t }: { invoice: Invoice; t: ReturnType<typeof documentTexts> }) {
  const span = LINE_COLUMNS - 1
  const english = invoice.documentLanguage === 'ENGLISH'
  return (
    <>
      <tr><th colSpan={span} scope="row">{t('subtotal')}</th><td className="table__number">{formatMoney(invoice.subtotal, invoice.currency)}</td></tr>
      <tr><th colSpan={span} scope="row">{t('vat')}</th><td className="table__number">{formatMoney(invoice.vatAmount, invoice.currency)}</td></tr>
      {invoice.withholdingPercent !== null && (
        <tr>
          <th colSpan={span} scope="row">
            {t('withholding')} ({english ? `${invoice.withholdingPercent}% of VAT` : `KDV'nin %${invoice.withholdingPercent}'si`})
          </th>
          <td className="table__number">-{formatMoney(invoice.withholdingAmount, invoice.currency)}</td>
        </tr>
      )}
      <tr><th colSpan={span} scope="row">{t('payable')}</th><td className="table__number"><strong>{formatMoney(invoice.payableAmount, invoice.currency)}</strong></td></tr>
    </>
  )
}

export function SalesInvoicePrintPage() {
  const id = Number(useParams().id)
  const { data: invoice, error, isLoading } = useGetInvoiceQuery(id)
  const t = documentTexts(invoice?.documentLanguage)
  return (
    <section className="page label-page">
      <style>{'@page { size: A4 portrait; margin: 12mm; }'}</style>
      <div className="no-print">
        <p><Link to={`/invoices/${id}`}>← Faturaya dön</Link></p>
        <header className="page__header">
          <div><h1>Fatura</h1><p className="muted">Bu çıktı resmî e-Fatura değildir. Belge müşterinin belge dilinde basılır.</p></div>
          <button type="button" className="button button--primary" disabled={!invoice} onClick={() => window.print()}><Printer aria-hidden="true" />Yazdır</button>
        </header>
        {isLoading && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={error} />
      </div>
      {invoice && (
        <article lang={t.lang} className="batch-card packing-sheet">
          <header className="batch-card__header">
            <div>
              <div className="batch-card__number">{t('invoice')} {invoice.invoiceNo}</div>
              <div>{invoice.customerName}</div>
              {invoice.customerTaxNumber && (
                <div>{invoice.customerTaxOffice && `${t('taxOffice')}: ${invoice.customerTaxOffice} · `}{t('taxNumber')}: {invoice.customerTaxNumber}</div>
              )}
              <div>{t('date')}: {formatDate(invoice.invoiceDate)}{invoice.dueDate && ` · ${t('dueDate')}: ${formatDate(invoice.dueDate)}`}</div>
              <div>{t('shipments')}: {shipmentText(invoice)}</div>
            </div>
            <Barcode text={invoice.invoiceNo} />
          </header>
          <table className="batch-card__table">
            <PrintLineHead t={t} />
            <tbody>
              {invoice.lines.map((line) => <tr key={line.id}><LineCells line={line} currency={invoice.currency} /></tr>)}
            </tbody>
            <tfoot><PrintTotals invoice={invoice} t={t} /></tfoot>
          </table>
          {invoice.notes && <p>{invoice.notes}</p>}
          <p className="muted">{t('notOfficial')}</p>
        </article>
      )}
    </section>
  )
}
