import { Banknote, Plus, Printer, Search, Trash2, XCircle } from 'lucide-react'
import { useMemo, useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useNavigate, useParams, useSearchParams } from 'react-router-dom'
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
import { formatDate, todayIsoDate } from '../../utils/format'
import { formatMoney } from '../../utils/money'
import { LookupSelect } from '../masterData/LookupSelect'

type PaymentMethod = 'CASH' | 'BANK_TRANSFER' | 'CHECK' | 'OPENING'
type PaymentStatus = 'ACTIVE' | 'CANCELLED'

const methodLabels: Record<PaymentMethod, string> = { CASH: 'Nakit', BANK_TRANSFER: 'Havale / EFT', CHECK: 'Çek', OPENING: 'Açılış bakiyesi' }

interface PaymentRow {
  id: number
  paymentNo: string
  customerName: string
  paymentDate: string
  method: PaymentMethod
  referenceNo: string | null
  dueDate: string | null
  amount: number
  allocatedAmount: number
  currency: string
  status: PaymentStatus
}

interface Payment extends PaymentRow {
  customerId: number
  unallocatedAmount: number
  notes: string | null
  cancelReason: string | null
  allocations: { id: number; invoiceId: number; invoiceNo: string; invoiceDate: string; dueDate: string | null; amount: number }[]
}

interface PaymentRequest {
  customerId: number
  paymentDate: string
  method: PaymentMethod
  amount: number
  currency: string | null
  referenceNo: string | null
  dueDate: string | null
  notes: string | null
}

interface Account {
  customerId: number
  customerCode: string
  customerName: string
  currency: string
  invoiced: number
  collected: number
  balance: number
  overdueAmount: number
  unallocatedAmount: number
  oldestOverdueDate: string | null
}

interface OpenInvoice {
  id: number
  invoiceNo: string
  customerId: number
  customerName: string
  invoiceDate: string
  dueDate: string | null
  currency: string
  payable: number
  paid: number
  open: number
  daysOverdue: number
}

interface Statement {
  customerId: number
  customerCode: string
  customerName: string
  customerTaxNumber: string | null
  customerTaxOffice: string | null
  currency: string
  currencies: string[]
  openingBalance: number
  totalDebit: number
  totalCredit: number
  closingBalance: number
  rows: { date: string; type: 'INVOICE' | 'PAYMENT'; documentId: number; documentNo: string; dueDate: string | null; description: string | null; debit: number; credit: number; balance: number }[]
}

interface OpeningBalance {
  id: number
  documentNo: string
  customerId: number
  customerName: string
  date: string
  dueDate: string | null
  currency: string
  debit: number
  credit: number
  allocatedAmount: number
  notes: string | null
}

interface OpeningBalanceRequest {
  customerId: number
  date: string
  amount: number
  isCredit: boolean
  currency: string | null
  dueDate: string | null
  notes: string | null
}

const accountApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getOrmePayments: build.query<PagedResult<PaymentRow>, Record<string, unknown>>({
      query: (params) => ({ url: '/orme-payments', params }),
      providesTags: ['OrmeShipping'],
    }),
    getOrmePayment: build.query<Payment, number>({
      query: (id) => `/orme-payments/${id}`,
      providesTags: ['OrmeShipping'],
    }),
    createOrmePayment: build.mutation<Payment, PaymentRequest>({
      query: (body) => ({ url: '/orme-payments', method: 'POST', body }),
      invalidatesTags: ['OrmeShipping'],
    }),
    cancelOrmePayment: build.mutation<Payment, { id: number; reason: string }>({
      query: ({ id, reason }) => ({ url: `/orme-payments/${id}/cancel`, method: 'POST', body: { reason } }),
      invalidatesTags: ['OrmeShipping'],
    }),
    allocateOrmePayment: build.mutation<Payment, { id: number; lines: { invoiceId: number; amount: number }[] }>({
      query: ({ id, lines }) => ({ url: `/orme-payments/${id}/allocations`, method: 'POST', body: { lines } }),
      invalidatesTags: ['OrmeShipping'],
    }),
    removeOrmeAllocation: build.mutation<Payment, { id: number; allocationId: number }>({
      query: ({ id, allocationId }) => ({ url: `/orme-payments/${id}/allocations/${allocationId}`, method: 'DELETE' }),
      invalidatesTags: ['OrmeShipping'],
    }),
    getOrmeOpeningBalances: build.query<OpeningBalance[], string | undefined>({
      query: (search) => ({ url: '/orme-accounts/openings', params: { search } }),
      providesTags: ['OrmeShipping'],
    }),
    createOrmeOpeningBalance: build.mutation<number, OpeningBalanceRequest>({
      query: (body) => ({ url: '/orme-accounts/openings', method: 'POST', body }),
      invalidatesTags: ['OrmeShipping'],
    }),
    deleteOrmeOpeningBalance: build.mutation<void, { isCredit: boolean; id: number }>({
      query: ({ isCredit, id }) => ({ url: `/orme-accounts/openings/${isCredit ? 'credit' : 'debit'}/${id}`, method: 'DELETE' }),
      invalidatesTags: ['OrmeShipping'],
    }),
    getOrmeAccounts: build.query<Account[], string | undefined>({
      query: (search) => ({ url: '/orme-accounts', params: { search } }),
      providesTags: ['OrmeShipping'],
    }),
    getOrmeOpenInvoices: build.query<OpenInvoice[], { customerId?: number; currency?: string; overdueOnly?: boolean }>({
      query: (params) => ({ url: '/orme-accounts/open-invoices', params }),
      providesTags: ['OrmeShipping'],
    }),
    getOrmeStatement: build.query<Statement, { customerId: number; currency?: string; from?: string; to?: string }>({
      query: ({ customerId, ...params }) => ({ url: `/orme-accounts/${customerId}/statement`, params }),
      providesTags: ['OrmeShipping'],
    }),
  }),
})

const PAGE_SIZE = 20

function PaymentDialog({ onClose }: { onClose: () => void }) {
  const navigate = useNavigate()
  const [create, { isLoading, error }] = accountApi.useCreateOrmePaymentMutation()
  const [customerId, setCustomerId] = useState<number | null>(null)
  const [paymentDate, setPaymentDate] = useState(todayIsoDate())
  const [method, setMethod] = useState<PaymentMethod>('BANK_TRANSFER')
  const [amount, setAmount] = useState('')
  const [currency, setCurrency] = useState('')
  const [referenceNo, setReferenceNo] = useState('')
  const [dueDate, setDueDate] = useState('')
  const [notes, setNotes] = useState('')
  const isCheck = method === 'CHECK'

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await create({
      customerId: customerId ?? 0,
      paymentDate,
      method,
      amount: Number(amount),
      currency: currency.trim() || null,
      referenceNo: referenceNo.trim() || null,
      dueDate: isCheck ? dueDate || null : null,
      notes: notes.trim() || null,
    })
    if ('data' in result && result.data) {
      navigate(`/orme/payments/${result.data.id}`)
    }
  }

  return (
    <Modal title="Yeni tahsilat" onClose={onClose}>
      <form className="form" onSubmit={handleSubmit}>
        <LookupSelect resource="orme-customers" label="Müşteri" required value={customerId} onChange={setCustomerId} />
        <div className="form__pair">
          <label className="field"><span>Tarih</span><input type="date" required value={paymentDate} onChange={(event) => setPaymentDate(event.target.value)} /></label>
          <label className="field">
            <span>Ödeme şekli</span>
            <select value={method} onChange={(event) => setMethod(event.target.value as PaymentMethod)}>
              {Object.entries(methodLabels).filter(([value]) => value !== 'OPENING').map(([value, label]) => <option key={value} value={value}>{label}</option>)}
            </select>
          </label>
        </div>
        <div className="form__pair">
          <label className="field"><span>Tutar</span><input type="number" min="0" step="0.01" required value={amount} onChange={(event) => setAmount(event.target.value)} /></label>
          <label className="field"><span>Para birimi</span><input maxLength={3} placeholder="Boşsa müşterinin" value={currency} onChange={(event) => setCurrency(event.target.value.toUpperCase())} /></label>
        </div>
        <div className="form__pair">
          <label className="field"><span>{isCheck ? 'Çek no' : 'Dekont no'}</span><input maxLength={50} required={isCheck} value={referenceNo} onChange={(event) => setReferenceNo(event.target.value)} /></label>
          {isCheck && <label className="field"><span>Çek vadesi</span><input type="date" required value={dueDate} onChange={(event) => setDueDate(event.target.value)} /></label>}
        </div>
        <label className="field"><span>Not</span><input maxLength={500} value={notes} onChange={(event) => setNotes(event.target.value)} /></label>
        <ErrorAlert error={error} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading}>Kaydet</button>
        </footer>
      </form>
    </Modal>
  )
}

/** Tahsilatlar, en yeniden eskiye. */
export function OrmePaymentsPage() {
  const canManage = usePermission(Permissions.OrmeAccountManage)
  const grid = useGrid()
  const { page, setPage } = grid
  const [input, setInput] = useState('')
  const [search, setSearch] = useState('')
  const [creating, setCreating] = useState(false)
  const { data, error, isLoading, isFetching } = accountApi.useGetOrmePaymentsQuery({ page, pageSize: PAGE_SIZE, search: search || undefined, ...grid.params })

  const columns: GridColumn[] = [
    { key: 'paymentNo', label: 'Tahsilat no' },
    { key: 'customerName', label: 'Müşteri' },
    { key: 'paymentDate', label: 'Tarih', filter: false },
    { key: 'method', label: 'Şekil', filter: Object.entries(methodLabels).map(([value, label]) => ({ value, label })) },
    { key: 'referenceNo', label: 'Belge no' },
    { key: 'dueDate', label: 'Çek vadesi', filter: false },
    { key: 'amount', label: 'Tutar', numeric: true, filter: false },
    { key: 'allocatedAmount', label: 'Dağıtılan', numeric: true, filter: false },
    { key: 'status', label: 'Durum', filter: [{ value: 'ACTIVE', label: 'Etkin' }, { value: 'CANCELLED', label: 'İptal' }] },
  ]

  const submit = (event: FormEvent) => {
    event.preventDefault()
    setPage(1)
    setSearch(input.trim())
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">Tahsilatlar</h1>
      <form className="toolbar" onSubmit={submit} role="search">
        <input type="search" placeholder="Tahsilat no, müşteri ya da belge no" aria-label="Tahsilat ara" value={input} onChange={(event) => setInput(event.target.value)} />
        <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
        <ClearFiltersButton grid={grid} />
        <div className="toolbar__actions">
          {canManage && <button type="button" className="button button--primary" onClick={() => setCreating(true)}><Plus aria-hidden="true" />Yeni tahsilat</button>}
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
                    <td><Link to={`/orme/payments/${row.id}`}><code>{row.paymentNo}</code></Link></td>
                    <td>{row.customerName}</td>
                    <td>{formatDate(row.paymentDate)}</td>
                    <td>{methodLabels[row.method]}</td>
                    <td>{row.referenceNo ?? '—'}</td>
                    <td>{row.dueDate ? formatDate(row.dueDate) : '—'}</td>
                    <td className="table__number">{formatMoney(row.amount, row.currency)}</td>
                    <td className="table__number">{formatMoney(row.allocatedAmount, row.currency)}</td>
                    <td>{row.status === 'ACTIVE' ? <span className="badge badge--ok">Etkin</span> : <span className="badge badge--danger">İptal</span>}</td>
                  </tr>
                ))}
                {data.items.length === 0 && <tr><td colSpan={columns.length} className="table__empty">Tahsilat bulunamadı.</td></tr>}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}
      {creating && <PaymentDialog onClose={() => setCreating(false)} />}
    </section>
  )
}

/** Faturalara dağıtım: açık faturalar en eski vadeden başlayarak hazır doldurulur. */
function AllocateDialog({ payment, onClose }: { payment: Payment; onClose: () => void }) {
  const { data: invoices, error: loadError, isLoading } = accountApi.useGetOrmeOpenInvoicesQuery({ customerId: payment.customerId, currency: payment.currency })
  const [allocate, state] = accountApi.useAllocateOrmePaymentMutation()
  const [edited, setEdited] = useState<Record<number, string>>({})

  const suggested = useMemo(() => {
    let left = payment.unallocatedAmount
    const result: Record<number, string> = {}
    for (const invoice of invoices ?? []) {
      const part = Math.min(left, invoice.open)
      result[invoice.id] = part > 0 ? part.toFixed(2) : ''
      left = Math.round((left - part) * 100) / 100
    }
    return result
  }, [invoices, payment.unallocatedAmount])

  const valueOf = (id: number) => edited[id] ?? suggested[id] ?? ''
  const total = (invoices ?? []).reduce((sum, invoice) => sum + (Number(valueOf(invoice.id)) || 0), 0)

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const lines = (invoices ?? [])
      .map((invoice) => ({ invoiceId: invoice.id, amount: Number(valueOf(invoice.id)) || 0 }))
      .filter((line) => line.amount > 0)
    const result = await allocate({ id: payment.id, lines })
    if (!('error' in result)) {
      onClose()
    }
  }

  return (
    <Modal title={`Faturalara dağıt: ${payment.paymentNo}`} onClose={onClose} variant="drawer">
      <form className="form" onSubmit={handleSubmit}>
        <p className="muted">Dağıtılacak: {formatMoney(payment.unallocatedAmount, payment.currency)} · Seçilen: {formatMoney(total, payment.currency)}</p>
        {isLoading && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={loadError ?? state.error} />
        <div className="table-wrap">
          <table className="table">
            <thead><tr><th>Fatura</th><th>Vade</th><th className="table__number">Kalan</th><th className="table__number">Dağıtılan</th></tr></thead>
            <tbody>
              {invoices?.map((invoice) => (
                <tr key={invoice.id}>
                  <td><code>{invoice.invoiceNo}</code></td>
                  <td>{invoice.dueDate ? formatDate(invoice.dueDate) : '—'}</td>
                  <td className="table__number">{formatMoney(invoice.open, invoice.currency)}</td>
                  <td className="table__number">
                    <input className="matrix-input purchase-input" aria-label={`${invoice.invoiceNo} dağıtılan`} type="number" min="0" step="0.01" value={valueOf(invoice.id)} onChange={(event) => setEdited({ ...edited, [invoice.id]: event.target.value })} />
                  </td>
                </tr>
              ))}
              {invoices?.length === 0 && <tr><td colSpan={4} className="table__empty">Bu müşterinin {payment.currency} cinsinden açık faturası yok.</td></tr>}
            </tbody>
          </table>
        </div>
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={state.isLoading || total <= 0}>Dağıt</button>
        </footer>
      </form>
    </Modal>
  )
}

function CancelPaymentDialog({ payment, onClose }: { payment: Payment; onClose: () => void }) {
  const [cancel, { isLoading, error }] = accountApi.useCancelOrmePaymentMutation()
  const [reason, setReason] = useState('')

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await cancel({ id: payment.id, reason: reason.trim() })
    if (!('error' in result)) {
      onClose()
    }
  }

  return (
    <Modal title={`Tahsilatı iptal et: ${payment.paymentNo}`} onClose={onClose}>
      <form className="form" onSubmit={handleSubmit}>
        <p className="muted">Faturalara dağıtılan tutarlar geri alınır, faturalar yeniden açık olur.</p>
        <label className="field"><span>Gerekçe</span><input required maxLength={500} value={reason} onChange={(event) => setReason(event.target.value)} /></label>
        <ErrorAlert error={error} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading}>İptal et</button>
        </footer>
      </form>
    </Modal>
  )
}

export function OrmePaymentDetailPage() {
  const id = Number(useParams().id)
  const canManage = usePermission(Permissions.OrmeAccountManage)
  const { data: payment, error, isLoading } = accountApi.useGetOrmePaymentQuery(id)
  const [removeAllocation, removeState] = accountApi.useRemoveOrmeAllocationMutation()
  const [allocating, setAllocating] = useState(false)
  const [cancelling, setCancelling] = useState(false)
  const active = payment?.status === 'ACTIVE'

  return (
    <section className="page">
      <p><Link to="/orme/payments">← Tahsilatlara dön</Link></p>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error ?? removeState.error} />
      {payment && (
        <>
          <header className="page__header">
            <div>
              <h1>{payment.paymentNo} {active ? <span className="badge badge--ok">Etkin</span> : <span className="badge badge--danger">İptal</span>}</h1>
              <p className="muted">
                <Link to={`/orme/accounts/${payment.customerId}?currency=${payment.currency}`}>{payment.customerName}</Link> · {formatDate(payment.paymentDate)} · {methodLabels[payment.method]}
                {payment.referenceNo && ` · ${payment.referenceNo}`}
                {payment.dueDate && ` · Vade ${formatDate(payment.dueDate)}`}
              </p>
              {payment.notes && <p>{payment.notes}</p>}
              {payment.cancelReason && <p>İptal gerekçesi: {payment.cancelReason}</p>}
            </div>
            {canManage && active && (
              <div className="page__actions">
                <button type="button" className="button button--primary" disabled={payment.unallocatedAmount <= 0} onClick={() => setAllocating(true)}><Banknote aria-hidden="true" />Faturalara dağıt</button>
                <button type="button" className="button button--danger-ghost" onClick={() => setCancelling(true)}><XCircle aria-hidden="true" />İptal et</button>
              </div>
            )}
          </header>
          <div className="cards cards--stats">
            <article className="card stat"><div className="stat__body"><h2 className="stat__label">Tutar</h2><p className="stat__value">{formatMoney(payment.amount, payment.currency)}</p></div></article>
            <article className="card stat"><div className="stat__body"><h2 className="stat__label">Dağıtılan</h2><p className="stat__value">{formatMoney(payment.allocatedAmount, payment.currency)}</p></div></article>
            <article className="card stat"><div className="stat__body"><h2 className="stat__label">Avans (dağıtılmamış)</h2><p className="stat__value">{formatMoney(payment.unallocatedAmount, payment.currency)}</p></div></article>
          </div>
          <div className="table-wrap">
            <table className="table">
              <thead><tr><th>Fatura</th><th>Fatura tarihi</th><th>Vade</th><th className="table__number">Dağıtılan</th><th aria-label="İşlem" /></tr></thead>
              <tbody>
                {payment.allocations.map((allocation) => (
                  <tr key={allocation.id}>
                    <td><Link to={`/orme/invoices/${allocation.invoiceId}`}><code>{allocation.invoiceNo}</code></Link></td>
                    <td>{formatDate(allocation.invoiceDate)}</td>
                    <td>{allocation.dueDate ? formatDate(allocation.dueDate) : '—'}</td>
                    <td className="table__number">{formatMoney(allocation.amount, payment.currency)}</td>
                    <td>{canManage && active && <button type="button" className="button" aria-label={`${allocation.invoiceNo} dağıtımını kaldır`} disabled={removeState.isLoading} onClick={() => removeAllocation({ id, allocationId: allocation.id })}><Trash2 aria-hidden="true" /></button>}</td>
                  </tr>
                ))}
                {payment.allocations.length === 0 && <tr><td colSpan={5} className="table__empty">Faturaya dağıtılmadı.</td></tr>}
              </tbody>
            </table>
          </div>
          {allocating && <AllocateDialog payment={payment} onClose={() => setAllocating(false)} />}
          {cancelling && <CancelPaymentDialog payment={payment} onClose={() => setCancelling(false)} />}
        </>
      )}
    </section>
  )
}

/** Müşteri başına, para birimi başına bakiye. */
export function OrmeAccountsPage() {
  const [input, setInput] = useState('')
  const [search, setSearch] = useState('')
  const { data, error, isLoading } = accountApi.useGetOrmeAccountsQuery(search || undefined)

  const submit = (event: FormEvent) => {
    event.preventDefault()
    setSearch(input.trim())
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">Cari Hesaplar</h1>
      <form className="toolbar" onSubmit={submit} role="search">
        <input type="search" placeholder="Müşteri kodu ya da adı" aria-label="Müşteri ara" value={input} onChange={(event) => setInput(event.target.value)} />
        <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
      </form>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {data && (
        <div className="table-wrap">
          <table className="table">
            <thead><tr><th>Kod</th><th>Müşteri</th><th className="table__number">Faturalanan</th><th className="table__number">Tahsil edilen</th><th className="table__number">Bakiye</th><th className="table__number">Vadesi geçen</th><th className="table__number">Avans</th><th>En eski vade</th></tr></thead>
            <tbody>
              {data.map((row) => (
                <tr key={`${row.customerId}-${row.currency}`}>
                  <td><code>{row.customerCode}</code></td>
                  <td><Link to={`/orme/accounts/${row.customerId}?currency=${row.currency}`}>{row.customerName}</Link></td>
                  <td className="table__number">{formatMoney(row.invoiced, row.currency)}</td>
                  <td className="table__number">{formatMoney(row.collected, row.currency)}</td>
                  <td className="table__number"><strong>{formatMoney(row.balance, row.currency)}</strong></td>
                  <td className="table__number">{row.overdueAmount > 0 ? <span className="badge badge--danger">{formatMoney(row.overdueAmount, row.currency)}</span> : '—'}</td>
                  <td className="table__number">{row.unallocatedAmount > 0 ? formatMoney(row.unallocatedAmount, row.currency) : '—'}</td>
                  <td>{row.oldestOverdueDate ? formatDate(row.oldestOverdueDate) : '—'}</td>
                </tr>
              ))}
              {data.length === 0 && <tr><td colSpan={8} className="table__empty">Onaylı fatura ya da tahsilat yok.</td></tr>}
            </tbody>
          </table>
        </div>
      )}
    </section>
  )
}

/** Müşteri ekstresi; ekranda ve A4 çıktıda aynı. */
export function OrmeAccountStatementPage() {
  const customerId = Number(useParams().customerId)
  const [params, setParams] = useSearchParams()
  const currency = params.get('currency') ?? undefined
  const [from, setFrom] = useState('')
  const [to, setTo] = useState('')
  const { data, error, isLoading } = accountApi.useGetOrmeStatementQuery({ customerId, currency, from: from || undefined, to: to || undefined })

  return (
    <section className="page label-page">
      <style>{'@page { size: A4 portrait; margin: 12mm; }'}</style>
      <div className="no-print">
        <p><Link to="/orme/accounts">← Cari hesaplara dön</Link></p>
        <div className="toolbar">
          <label className="field"><span>Başlangıç</span><input type="date" value={from} onChange={(event) => setFrom(event.target.value)} /></label>
          <label className="field"><span>Bitiş</span><input type="date" value={to} onChange={(event) => setTo(event.target.value)} /></label>
          {data && data.currencies.length > 1 && (
            <label className="field">
              <span>Para birimi</span>
              <select value={data.currency} onChange={(event) => setParams({ currency: event.target.value })}>
                {data.currencies.map((code) => <option key={code} value={code}>{code}</option>)}
              </select>
            </label>
          )}
          <div className="toolbar__actions">
            <button type="button" className="button button--primary" disabled={!data} onClick={() => window.print()}><Printer aria-hidden="true" />Yazdır</button>
          </div>
        </div>
        {isLoading && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={error} />
      </div>
      {data && (
        <article className="batch-card packing-sheet">
          <header className="batch-card__header">
            <div>
              <div className="batch-card__number">Cari Ekstre</div>
              <div>{data.customerCode} · {data.customerName}</div>
              {data.customerTaxNumber && <div>{data.customerTaxOffice} · VKN {data.customerTaxNumber}</div>}
              <div>{from || to ? `${from ? formatDate(from) : 'Başlangıç'} – ${to ? formatDate(to) : 'Bugün'}` : 'Tüm hareketler'} · {data.currency}</div>
            </div>
          </header>
          <table className="batch-card__table">
            <thead><tr><th>Tarih</th><th>Belge</th><th>Vade</th><th>Açıklama</th><th>Borç</th><th>Alacak</th><th>Bakiye</th></tr></thead>
            <tbody>
              <tr><td colSpan={6}>Devreden bakiye</td><td>{formatMoney(data.openingBalance, data.currency)}</td></tr>
              {data.rows.map((row) => (
                <tr key={`${row.type}-${row.documentId}`}>
                  <td>{formatDate(row.date)}</td>
                  <td><Link to={row.type === 'INVOICE' ? `/orme/invoices/${row.documentId}` : `/orme/payments/${row.documentId}`}>{row.documentNo}</Link></td>
                  <td>{row.dueDate ? formatDate(row.dueDate) : ''}</td>
                  <td>{row.description}</td>
                  <td>{row.debit > 0 ? formatMoney(row.debit, data.currency) : ''}</td>
                  <td>{row.credit > 0 ? formatMoney(row.credit, data.currency) : ''}</td>
                  <td>{formatMoney(row.balance, data.currency)}</td>
                </tr>
              ))}
            </tbody>
            <tfoot>
              <tr><th colSpan={4} scope="row">Toplam</th><td>{formatMoney(data.totalDebit, data.currency)}</td><td>{formatMoney(data.totalCredit, data.currency)}</td><td><strong>{formatMoney(data.closingBalance, data.currency)}</strong></td></tr>
            </tfoot>
          </table>
        </article>
      )}
    </section>
  )
}

const agingBuckets = [
  { label: '1-30 gün', min: 1, max: 30 },
  { label: '31-60 gün', min: 31, max: 60 },
  { label: '61-90 gün', min: 61, max: 90 },
  { label: '90+ gün', min: 91, max: Infinity },
]

/** Açık faturalar ve vade yaşlandırması. Vadesi girilmemiş fatura vadesi geçmiş sayılmaz. */
export function OrmeOpenInvoicesPage() {
  const [overdueOnly, setOverdueOnly] = useState(false)
  const { data, error, isLoading } = accountApi.useGetOrmeOpenInvoicesQuery({ overdueOnly })
  const currencies = [...new Set((data ?? []).map((row) => row.currency))].sort()
  const bucketTotal = (currency: string, min: number, max: number) =>
    (data ?? []).filter((row) => row.currency === currency && row.daysOverdue >= min && row.daysOverdue <= max).reduce((sum, row) => sum + row.open, 0)

  return (
    <section className="page">
      <h1 className="visually-hidden">Açık Faturalar</h1>
      <div className="toolbar">
        <label className="checkbox"><input type="checkbox" checked={overdueOnly} onChange={(event) => setOverdueOnly(event.target.checked)} /> Yalnız vadesi geçenler</label>
      </div>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {data && (
        <>
          {currencies.length > 0 && (
            <div className="table-wrap">
              <table className="table">
                <thead><tr><th>Para birimi</th><th className="table__number">Vadesi gelmemiş</th>{agingBuckets.map((bucket) => <th key={bucket.label} className="table__number">{bucket.label}</th>)}<th className="table__number">Toplam açık</th></tr></thead>
                <tbody>
                  {currencies.map((currency) => (
                    <tr key={currency}>
                      <td>{currency}</td>
                      <td className="table__number">{formatMoney(bucketTotal(currency, 0, 0), currency)}</td>
                      {agingBuckets.map((bucket) => <td key={bucket.label} className="table__number">{formatMoney(bucketTotal(currency, bucket.min, bucket.max), currency)}</td>)}
                      <td className="table__number"><strong>{formatMoney(bucketTotal(currency, 0, Infinity), currency)}</strong></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
          <div className="table-wrap">
            <table className="table">
              <thead><tr><th>Fatura</th><th>Müşteri</th><th>Tarih</th><th>Vade</th><th className="table__number">Gecikme (gün)</th><th className="table__number">Ödenecek</th><th className="table__number">Tahsil edilen</th><th className="table__number">Kalan</th></tr></thead>
              <tbody>
                {data.map((row) => (
                  <tr key={row.id}>
                    <td><Link to={`/orme/invoices/${row.id}`}><code>{row.invoiceNo}</code></Link></td>
                    <td><Link to={`/orme/accounts/${row.customerId}?currency=${row.currency}`}>{row.customerName}</Link></td>
                    <td>{formatDate(row.invoiceDate)}</td>
                    <td>{row.dueDate ? formatDate(row.dueDate) : '—'}</td>
                    <td className="table__number">{row.daysOverdue > 0 ? <span className="badge badge--danger">{row.daysOverdue}</span> : '—'}</td>
                    <td className="table__number">{formatMoney(row.payable, row.currency)}</td>
                    <td className="table__number">{formatMoney(row.paid, row.currency)}</td>
                    <td className="table__number"><strong>{formatMoney(row.open, row.currency)}</strong></td>
                  </tr>
                ))}
                {data.length === 0 && <tr><td colSpan={8} className="table__empty">Açık fatura yok.</td></tr>}
              </tbody>
            </table>
          </div>
        </>
      )}
    </section>
  )
}

function OpeningBalanceDialog({ onClose }: { onClose: () => void }) {
  const [create, { isLoading, error }] = accountApi.useCreateOrmeOpeningBalanceMutation()
  const [customerId, setCustomerId] = useState<number | null>(null)
  const [date, setDate] = useState(todayIsoDate())
  const [side, setSide] = useState<'debit' | 'credit'>('debit')
  const [amount, setAmount] = useState('')
  const [currency, setCurrency] = useState('')
  const [dueDate, setDueDate] = useState('')
  const [notes, setNotes] = useState('')

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await create({
      customerId: customerId ?? 0,
      date,
      amount: Number(amount),
      isCredit: side === 'credit',
      currency: currency.trim() || null,
      dueDate: side === 'debit' ? dueDate || null : null,
      notes: notes.trim() || null,
    })
    if (!('error' in result)) {
      onClose()
    }
  }

  return (
    <Modal title="Yeni açılış bakiyesi" onClose={onClose}>
      <form className="form" onSubmit={handleSubmit}>
        <LookupSelect resource="orme-customers" label="Müşteri" required value={customerId} onChange={setCustomerId} />
        <div className="form__pair">
          <label className="field">
            <span>Bakiye</span>
            <select value={side} onChange={(event) => setSide(event.target.value as 'debit' | 'credit')}>
              <option value="debit">Müşteri borçlu</option>
              <option value="credit">Müşteri alacaklı</option>
            </select>
          </label>
          <label className="field"><span>Tarih</span><input type="date" required value={date} onChange={(event) => setDate(event.target.value)} /></label>
        </div>
        <div className="form__pair">
          <label className="field"><span>Tutar</span><input type="number" min="0" step="0.01" required value={amount} onChange={(event) => setAmount(event.target.value)} /></label>
          <label className="field"><span>Para birimi</span><input maxLength={3} placeholder="Boşsa müşterinin" value={currency} onChange={(event) => setCurrency(event.target.value.toUpperCase())} /></label>
        </div>
        {side === 'debit' && <label className="field"><span>Vade</span><input type="date" value={dueDate} onChange={(event) => setDueDate(event.target.value)} /></label>}
        <label className="field"><span>Not</span><input maxLength={500} value={notes} onChange={(event) => setNotes(event.target.value)} /></label>
        <ErrorAlert error={error} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading}>Kaydet</button>
        </footer>
      </form>
    </Modal>
  )
}

/** Sisteme geçişteki müşteri bakiyeleri. Vadesi farklı borçlar ayrı satır girilebilir; tahsilatla eşleşen kayıt silinemez. */
export function OrmeOpeningBalancesPage() {
  const canManage = usePermission(Permissions.OrmeAccountManage)
  const [input, setInput] = useState('')
  const [search, setSearch] = useState('')
  const [creating, setCreating] = useState(false)
  const { data, error, isLoading } = accountApi.useGetOrmeOpeningBalancesQuery(search || undefined)
  const [remove, removeState] = accountApi.useDeleteOrmeOpeningBalanceMutation()

  const submit = (event: FormEvent) => {
    event.preventDefault()
    setSearch(input.trim())
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">Açılış Bakiyeleri</h1>
      <form className="toolbar" onSubmit={submit} role="search">
        <input type="search" placeholder="Müşteri kodu ya da adı" aria-label="Müşteri ara" value={input} onChange={(event) => setInput(event.target.value)} />
        <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
        <div className="toolbar__actions">
          {canManage && <button type="button" className="button button--primary" onClick={() => setCreating(true)}><Plus aria-hidden="true" />Yeni açılış bakiyesi</button>}
        </div>
      </form>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error ?? removeState.error} />
      {data && (
        <div className="table-wrap">
          <table className="table">
            <thead><tr><th>Belge</th><th>Müşteri</th><th>Tarih</th><th>Vade</th><th className="table__number">Borç</th><th className="table__number">Alacak</th><th className="table__number">Eşleşen</th><th>Not</th><th aria-label="İşlem" /></tr></thead>
            <tbody>
              {data.map((row) => (
                <tr key={row.documentNo}>
                  <td><code>{row.documentNo}</code></td>
                  <td><Link to={`/orme/accounts/${row.customerId}?currency=${row.currency}`}>{row.customerName}</Link></td>
                  <td>{formatDate(row.date)}</td>
                  <td>{row.dueDate ? formatDate(row.dueDate) : '—'}</td>
                  <td className="table__number">{row.debit > 0 ? formatMoney(row.debit, row.currency) : '—'}</td>
                  <td className="table__number">{row.credit > 0 ? formatMoney(row.credit, row.currency) : '—'}</td>
                  <td className="table__number">{formatMoney(row.allocatedAmount, row.currency)}</td>
                  <td>{row.notes ?? '—'}</td>
                  <td>{canManage && row.allocatedAmount === 0 && <button type="button" className="button" aria-label={`${row.documentNo} sil`} disabled={removeState.isLoading} onClick={() => remove({ isCredit: row.credit > 0, id: row.id })}><Trash2 aria-hidden="true" /></button>}</td>
                </tr>
              ))}
              {data.length === 0 && <tr><td colSpan={9} className="table__empty">Açılış bakiyesi girilmedi.</td></tr>}
            </tbody>
          </table>
        </div>
      )}
      {creating && <OpeningBalanceDialog onClose={() => setCreating(false)} />}
    </section>
  )
}
