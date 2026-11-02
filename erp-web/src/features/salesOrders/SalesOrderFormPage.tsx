import { CircleAlert, Plus, Save, Split, Trash2, X } from 'lucide-react'
import { useEffect, useId, useState } from 'react'
import type { FormEvent, ReactNode } from 'react'
import { Link, useNavigate, useParams, useSearchParams } from 'react-router-dom'
import { fieldErrors, isRequestError } from '../../api/envelope'
import { ErrorAlert, TextField } from '../../components/FormControls'
import { useDebounced } from '../../hooks/useDebounced'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDateTime, formatQuantity, todayIsoDate } from '../../utils/format'
import { useCurrencyOptions } from '../finance/currencyOptions'
import { LookupSelect } from '../masterData/LookupSelect'
import type { LookupItem } from '../masterData/masterDataApi'
import type { MatrixAxis } from '../productionOrders/orderPresentation'
import { buildQuantityMatrix } from '../productionOrders/orderPresentation'
import { useQuotePricesMutation } from '../pricing/pricingApi'
import { formatMoney } from '../purchasing/purchasingPresentation'
import { useFormDraft } from '../workspace/useFormDraft'
import {
  parsePrice, quoteKey, rowsFromLines, sizeConflicts, splitByPrice, suggestionFor,
} from './salesOrderPresentation'
import type { OrderRow, QuoteLike } from './salesOrderPresentation'
import {
  useGetCustomerDefaultsQuery, useGetSalesOrderQuery, useLazyGetCustomerDefaultsQuery, useSaveSalesOrderMutation,
} from './salesOrdersApi'
import type { SalesOrder } from './salesOrdersApi'

/** Formun tamamı; taslak olarak bu hâliyle saklanır. */
interface FormState {
  customerId: number | null
  customerBuyerId: number | null
  deliveryAddressId: number | null
  customerOrderNo: string
  orderDate: string
  dueDate: string
  currency: string
  notes: string
  /** Tablonun beden sütunları; bütün satırlar için ortaktır. */
  sizes: MatrixAxis[]
  rows: OrderRow[]
}

let lastRowKey = 0
const newRowKey = () => `row-${Date.now().toString(36)}-${(lastRowKey += 1)}`

const emptyRow = (from?: OrderRow): OrderRow => ({
  key: newRowKey(),
  // Yeni satır çoğunlukla aynı ürünün bir sonraki rengidir: ürün üstteki satırdan gelir.
  productId: from?.productId ?? null,
  colorId: null,
  articleCode: from?.articleCode ?? null,
  unitPrice: '',
  priceTouched: false,
  quantities: {},
})

const addDays = (iso: string, days: number) => {
  const date = new Date(`${iso}T00:00:00`)
  date.setDate(date.getDate() + days)
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`
}

const daysBetween = (from: string, to: string) =>
  Math.round((new Date(`${to}T00:00:00`).getTime() - new Date(`${from}T00:00:00`).getTime()) / 86_400_000)

/** Kayıtlı siparişten form; kopyada tarih bugüne alınır (teslim süresi korunur), müşteri sipariş no boşalır. */
function stateOf(order: SalesOrder | undefined, asCopy = false): FormState {
  if (!order) {
    return {
      customerId: null, customerBuyerId: null, deliveryAddressId: null, customerOrderNo: '', orderDate: todayIsoDate(),
      dueDate: '', currency: 'TRY', notes: '', sizes: [], rows: [emptyRow()],
    }
  }

  const today = todayIsoDate()
  return {
    customerId: order.customerId,
    customerBuyerId: order.customerBuyerId,
    deliveryAddressId: order.deliveryAddressId,
    customerOrderNo: asCopy ? '' : order.customerOrderNo ?? '',
    orderDate: asCopy ? today : order.orderDate,
    dueDate: asCopy ? addDays(today, Math.max(0, daysBetween(order.orderDate, order.dueDate))) : order.dueDate,
    currency: order.currency,
    notes: order.notes ?? '',
    sizes: buildQuantityMatrix(order.lines).sizes,
    rows: order.lines.length > 0 ? rowsFromLines(order.lines, newRowKey) : [emptyRow()],
  }
}

function OrderForm({
  order, initial, formKey, title, intro, backTo, backLabel, notice, onConflict,
}: {
  order?: SalesOrder
  initial: FormState
  /** Taslağın anahtarı (yeni sipariş ya da düzenlenen siparişe özgü). */
  formKey: string
  title: string
  intro?: string
  backTo: string
  backLabel: string
  /** Başlığın altında gösterilecek uyarı (ör. eşzamanlı değişiklik). */
  notice?: ReactNode
  onConflict?: () => void
}) {
  const navigate = useNavigate()
  const formId = useId()
  const [save, { isLoading, error }] = useSaveSalesOrderMutation()
  const [form, setForm] = useState<FormState>(initial)
  const [initialText] = useState(() => JSON.stringify(initial))
  // Eksik satır uyarıları ilk kaydetme denemesinden sonra gösterilir.
  const [attempted, setAttempted] = useState(false)
  const canOverridePrice = usePermission(Permissions.SalesOrderOverridePrice)
  const { customerId, sizes, rows } = form

  const set = (change: Partial<FormState>) => setForm((current) => ({ ...current, ...change }))
  const setRows = (change: (rows: OrderRow[]) => OrderRow[]) => setForm((current) => ({ ...current, rows: change(current.rows) }))
  const update = (key: string, change: Partial<OrderRow>) =>
    setRows((current) => current.map((row) => (row.key === key ? { ...row, ...change } : row)))

  // Müşteriden gelen bilgiler: teslim adresleri, buyer'lar, son siparişin bedenleri.
  const defaults = useGetCustomerDefaultsQuery(customerId ?? 0, { skip: customerId === null })
  const [loadDefaults] = useLazyGetCustomerDefaultsQuery()
  // Taslak listesinde görünen ad.
  const [customerName, setCustomerName] = useState<string | null>(null)

  const changeCustomer = async (id: number | null) => {
    set({ customerId: id, customerBuyerId: null, deliveryAddressId: null })
    if (id === null) {
      return
    }
    const result = await loadDefaults(id).unwrap().catch(() => null)
    if (!result) {
      return
    }
    setForm((current) => ({
      ...current,
      currency: result.currency,
      deliveryAddressId: result.addresses.find((a) => a.isDefaultDelivery)?.id ?? result.addresses[0]?.id ?? null,
      // Bedenleri hatırla: tabloda beden yoksa müşterinin son siparişindeki bedenler gelir.
      sizes: current.sizes.length > 0 ? current.sizes : result.lastOrderSizes,
    }))
  }

  // Taslak: müşteri seçildikten sonra form değiştikçe kullanıcıya özel saklanır.
  const serialized = JSON.stringify(form)
  const draft = useFormDraft(formKey, form, customerId !== null && serialized !== initialText, customerName)

  // Fiyat önerisi: fiyat listesi → ürün liste fiyatı, kur ve müşteri iskontosu sunucuda uygulanır.
  const [quotePrices] = useQuotePricesMutation()
  const [quotes, setQuotes] = useState<Map<string, QuoteLike>>(new Map())
  const quoteItems = customerId === null
    ? []
    : rows
      .filter((row) => row.productId !== null && row.colorId !== null)
      .flatMap((row) => sizes.map((size) => ({ productId: row.productId!, colorId: row.colorId!, sizeId: size.id })))
  const quoteSignature = useDebounced(JSON.stringify([customerId, form.currency, form.orderDate, quoteItems]), 400)

  useEffect(() => {
    const [signatureCustomer, currency, date, items] = JSON.parse(quoteSignature) as
      [number | null, string, string, { productId: number; colorId: number; sizeId: number }[]]
    if (signatureCustomer === null || items.length === 0 || !date) {
      return
    }
    let cancelled = false
    void quotePrices({ customerId: signatureCustomer, currency, date, items }).unwrap().then((result) => {
      if (cancelled) {
        return
      }
      const next = new Map(result.map((quote) => [quoteKey(quote.productId, quote.colorId, quote.sizeId), quote]))
      setQuotes(next)
      // Fiyatı boş ve kullanıcının dokunmadığı satırlara önerilen fiyat yazılır.
      setForm((current) => ({
        ...current,
        rows: current.rows.map((row) => {
          if (row.priceTouched || row.unitPrice.trim() !== '') {
            return row
          }
          const suggestion = suggestionFor(row, current.sizes.map((size) => size.id), next)
          return suggestion?.price != null ? { ...row, unitPrice: String(suggestion.price).replace('.', ',') } : row
        }),
      }))
    }).catch(() => undefined)
    return () => {
      cancelled = true
    }
  }, [quoteSignature, quotePrices])

  const addSize = (item: LookupItem | null) => {
    if (item && !sizes.some((size) => size.id === item.id)) {
      set({ sizes: [...sizes, item] })
    }
  }

  const quantityOf = (row: OrderRow, sizeId: number) => Number(row.quantities[sizeId] || 0)
  const rowQuantity = (row: OrderRow) => sizes.reduce((sum, size) => sum + quantityOf(row, size.id), 0)
  const rowAmount = (row: OrderRow) => {
    const price = parsePrice(row.unitPrice)
    return typeof price === 'number' ? rowQuantity(row) * price : null
  }

  const conflicts = sizeConflicts(rows)
  // Hiç doldurulmamış satır yok sayılır; yarım kalan satır kaydı engeller.
  const started = (row: OrderRow) => row.productId !== null || row.colorId !== null || rowQuantity(row) !== 0
  const problemOf = (row: OrderRow): string | undefined => {
    if (!started(row)) {
      return undefined
    }
    if (row.productId === null) {
      return 'ürün seçilmedi'
    }
    if (row.colorId === null) {
      return 'renk seçilmedi'
    }
    if (parsePrice(row.unitPrice) === undefined) {
      return 'birim fiyat geçersiz (en fazla 4 ondalık)'
    }
    if (rowQuantity(row) === 0) {
      return sizes.length === 0 ? 'adet girmek için önce beden ekleyin' : 'adet girilmedi'
    }
    const shared = conflicts.get(row.key)
    if (shared) {
      const names = sizes.filter((size) => shared.includes(size.id)).map((size) => size.name).join(', ')
      return `aynı ürün, renk ve beden (${names}) başka bir satırda da var; bir beden tek satırda girilmelidir`
    }
    return undefined
  }

  const problems = rows.flatMap((row, index) => {
    const problem = problemOf(row)
    return problem ? [`${index + 1}. satır: ${problem}.`] : []
  })

  const filled = rows.filter(started)
  const totalQuantity = filled.reduce((sum, row) => sum + rowQuantity(row), 0)
  const priced = filled.map(rowAmount).filter((amount): amount is number => amount !== null)
  const totalAmount = priced.length > 0 ? priced.reduce((sum, amount) => sum + amount, 0) : null
  const currencyCode = form.currency || 'TRY'
  const currencies = useCurrencyOptions(currencyCode)
  const sizeIds = sizes.map((size) => size.id)

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    setAttempted(true)
    if (problems.length > 0) {
      return
    }

    // Yalnızca tabloda görünen bedenlerin adedi girilmiş hücreleri kalem olur.
    const lines = filled.flatMap((row) =>
      sizes
        .map((size) => ({
          productId: row.productId!,
          colorId: row.colorId!,
          sizeId: size.id,
          quantity: quantityOf(row, size.id),
          unitPrice: parsePrice(row.unitPrice) ?? null,
        }))
        .filter((line) => line.quantity !== 0),
    )

    const result = await save({
      id: order?.id,
      body: {
        customerId: customerId ?? 0,
        customerOrderNo: form.customerOrderNo.trim() || null,
        orderDate: form.orderDate,
        // Boş tarih için sunucunun "zorunludur" hatasını ilgili alana bağlaması sağlanır.
        dueDate: form.dueDate || '0001-01-01',
        notes: form.notes.trim() || null,
        currency: form.currency.trim() || null,
        lines,
        rowVersion: order?.rowVersion,
        customerBuyerId: form.customerBuyerId,
        deliveryAddressId: form.deliveryAddressId,
      },
    })

    if ('data' in result && result.data) {
      await draft.clear()
      void navigate(`/sales-orders/${result.data.id}`)
    } else if (isRequestError(result.error) && result.error.code === 'CONCURRENCY_CONFLICT') {
      onConflict?.()
    }
  }

  const discard = async () => {
    await draft.clear()
    void navigate(backTo)
  }

  return (
    <>
      <p><Link to={backTo}>← {backLabel}</Link></p>
      <header className="page__header">
        <div>
          <h1>{title}</h1>
          {intro && <p className="muted">{intro}</p>}
        </div>
        <div className="page__actions">
          <span className="muted order-form__draft" role="status">
            {draft.offline
              ? 'Sunucuya ulaşılamadı; taslak bu tarayıcıda saklandı.'
              : draft.savedAt && `Taslak kaydedildi · ${formatDateTime(draft.savedAt)}`}
          </span>
          <button type="button" className="button" onClick={() => void discard()}>Vazgeç</button>
          <button type="submit" form={formId} className="button button--primary" disabled={isLoading}>
            <Save aria-hidden="true" />
            {isLoading ? 'Kaydediliyor…' : 'Kaydet'}
          </button>
        </div>
      </header>
      {notice}
      {draft.pending && (
        <div className="alert alert--info order-form__restore" role="status">
          <span>
            Kaydedilmemiş bir taslağınız var ({formatDateTime(draft.pending.updatedAt)}). Kaldığınız yerden devam
            edebilirsiniz.
          </span>
          <button
            type="button"
            className="button button--primary"
            onClick={() => {
              setForm(draft.pending!.state)
              draft.resolve(true)
            }}
          >
            Taslakla devam et
          </button>
          <button type="button" className="button" onClick={() => draft.resolve(false)}>Taslağı sil</button>
        </div>
      )}

      <form id={formId} className="form order-form" onSubmit={(event) => void handleSubmit(event)} noValidate>
        <section className="card">
          <h2>Sipariş bilgileri</h2>
          <div className="order-form__head">
            <div className="order-form__wide">
              <LookupSelect
                resource="customers"
                label="Müşteri"
                value={customerId}
                onChange={(id) => void changeCustomer(id)}
                onSelectItem={(item) => setCustomerName(item?.name ?? null)}
                required
                errors={fieldErrors(error, 'customerId')}
              />
            </div>
            <label className="field">
              <span>Buyer</span>
              <select
                value={form.customerBuyerId ?? ''}
                disabled={!defaults.data || defaults.data.buyers.length === 0}
                onChange={(event) => set({ customerBuyerId: event.target.value ? Number(event.target.value) : null })}
              >
                <option value="">{defaults.data && defaults.data.buyers.length === 0 ? '— Tanımlı buyer yok —' : '— Seçilmedi —'}</option>
                {defaults.data?.buyers.map((buyer) => <option key={buyer.id} value={buyer.id}>{buyer.code} — {buyer.name}</option>)}
                {order?.customerBuyerId && !defaults.data?.buyers.some((b) => b.id === order.customerBuyerId) && (
                  <option value={order.customerBuyerId}>{order.customerBuyerName}</option>
                )}
              </select>
              {fieldErrors(error, 'customerBuyerId')?.map((message) => <small key={message} className="field__error">{message}</small>)}
            </label>
            <div className="order-form__wide">
              <label className="field">
                <span>Teslim adresi</span>
                <select
                  value={form.deliveryAddressId ?? ''}
                  disabled={!defaults.data}
                  onChange={(event) => set({ deliveryAddressId: event.target.value ? Number(event.target.value) : null })}
                >
                  <option value="">{defaults.data?.addresses.length === 0 ? '— Müşterinin adresi yok —' : '— Seçilmedi —'}</option>
                  {defaults.data?.addresses.map((address) => (
                    <option key={address.id} value={address.id}>
                      {address.title}: {address.text}{address.isDefaultDelivery ? ' (varsayılan)' : ''}
                    </option>
                  ))}
                </select>
                {fieldErrors(error, 'deliveryAddressId')?.map((message) => <small key={message} className="field__error">{message}</small>)}
              </label>
            </div>
            <TextField
              label="Müşteri sipariş no"
              value={form.customerOrderNo}
              maxLength={50}
              onChange={(event) => set({ customerOrderNo: event.target.value })}
              errors={fieldErrors(error, 'customerOrderNo')}
              hint={defaults.data?.requireUniqueCustomerOrderNo ? 'Bu müşteride numara tekil olmalıdır.' : undefined}
              autoComplete="off"
            />
            <TextField
              label="Sipariş tarihi"
              type="date"
              value={form.orderDate}
              onChange={(event) => set({ orderDate: event.target.value })}
              errors={fieldErrors(error, 'orderDate')}
            />
            <TextField
              label="Teslim tarihi"
              type="date"
              min={form.orderDate}
              value={form.dueDate}
              onChange={(event) => set({ dueDate: event.target.value })}
              errors={fieldErrors(error, 'dueDate')}
            />
            <label className="field">
              <span>Para birimi</span>
              <select value={currencyCode} onChange={(event) => set({ currency: event.target.value })}>
                {currencies.map((option) => (
                  <option key={option.value} value={option.value}>{option.value}</option>
                ))}
              </select>
              {fieldErrors(error, 'currency')?.map((message) => (
                <small key={message} className="field__error">{message}</small>
              ))}
            </label>
            <label className="field order-form__full">
              <span>Not</span>
              <textarea rows={1} maxLength={1000} value={form.notes} onChange={(event) => set({ notes: event.target.value })} />
              {fieldErrors(error, 'notes')?.map((message) => (
                <small key={message} className="field__error">{message}</small>
              ))}
            </label>
          </div>
        </section>

        <section className="card order-lines">
          <header className="order-lines__bar">
            <h2>Kalemler</h2>
            <span className="tabs__count">{filled.length}</span>
            {defaults.data?.lastOrderNo && sizes.length > 0 && (
              <span className="muted order-lines__note">Bedenler son siparişten ({defaults.data.lastOrderNo}) geldi.</span>
            )}
            <div className="order-lines__tools">
              <LookupSelect
                resource="sizes"
                label="Beden ekle"
                hideLabel
                placeholder="Beden ekle…"
                value={null}
                required
                onChange={() => undefined}
                onSelectItem={addSize}
              />
              <button type="button" className="button" onClick={() => setRows((current) => [...current, emptyRow(current.at(-1))])}>
                <Plus aria-hidden="true" />
                Satır ekle
              </button>
            </div>
          </header>

          <table className="table order-grid">
            <thead>
              <tr>
                <th className="order-grid__no">#</th>
                <th className="order-grid__product">Ürün</th>
                <th className="order-grid__article">Artikel</th>
                <th className="order-grid__color">Renk</th>
                {sizes.map((size) => (
                  <th key={size.id} className="order-grid__size">
                    <span className="matrix-editor__axis matrix-editor__axis--size">
                      {size.name}
                      <button
                        type="button"
                        className="matrix-editor__remove"
                        aria-label={`${size.name} bedenini kaldır`}
                        title="Bedeni kaldır"
                        onClick={() => set({ sizes: sizes.filter((item) => item.id !== size.id) })}
                      >
                        <X aria-hidden="true" />
                      </button>
                    </span>
                  </th>
                ))}
                {sizes.length === 0 && <th className="order-grid__hint">Adet girmek için sağ üstten beden ekleyin</th>}
                <th className="table__number order-grid__total">Adet</th>
                <th className="table__number order-grid__price">Birim fiyat</th>
                <th className="table__number order-grid__amount">Tutar ({currencyCode})</th>
                <th className="order-grid__remove" aria-label="Satır işlemleri" />
              </tr>
            </thead>
            <tbody>
              {rows.map((row, index) => {
                const flagged = attempted && problemOf(row) !== undefined
                const badPrice = parsePrice(row.unitPrice) === undefined
                const suggestion = suggestionFor(row, sizeIds, quotes)
                const conflicted = conflicts.get(row.key) ?? []
                return (
                  <tr key={row.key} className={flagged ? 'order-grid__row--invalid' : undefined}>
                    <td className="order-grid__no">{index + 1}</td>
                    <td>
                      <LookupSelect
                        resource="products"
                        idSuffix={row.key}
                        label={`${index + 1}. satır ürün`}
                        hideLabel
                        value={row.productId}
                        onChange={(productId) => update(row.key, { productId, unitPrice: row.priceTouched ? row.unitPrice : '' })}
                        onSelectItem={(item) => update(row.key, { articleCode: item?.detail ?? null })}
                        customerId={customerId}
                        required
                      />
                    </td>
                    <td className="order-grid__article" title="Müşterinin ürün kodu (ürün kartından)">
                      {row.articleCode ?? <span className="muted">—</span>}
                    </td>
                    <td>
                      <LookupSelect
                        resource="colors"
                        idSuffix={row.key}
                        label={`${index + 1}. satır renk`}
                        hideLabel
                        value={row.colorId}
                        onChange={(colorId) => update(row.key, { colorId })}
                        required
                      />
                    </td>
                    {sizes.map((size) => (
                      <td key={size.id} className="order-grid__size">
                        <input
                          type="number"
                          min={0}
                          step={1}
                          inputMode="numeric"
                          className={conflicted.includes(size.id) ? 'matrix-input matrix-input--conflict' : 'matrix-input'}
                          aria-label={`${index + 1}. satır ${size.name} adet`}
                          aria-invalid={conflicted.includes(size.id)}
                          value={row.quantities[size.id] ?? ''}
                          onChange={(event) => update(row.key, { quantities: { ...row.quantities, [size.id]: event.target.value } })}
                        />
                      </td>
                    ))}
                    {sizes.length === 0 && <td />}
                    <td className="table__number"><strong>{formatQuantity(rowQuantity(row))}</strong></td>
                    <td className="table__number">
                      <span className="order-grid__price-cell">
                        {suggestion?.varies && (
                          <button
                            type="button"
                            className="icon-button order-grid__split"
                            title="Fiyat listesinde bedenlere göre farklı fiyat var. Satırı fiyata göre böl."
                            aria-label={`${index + 1}. satırı bedenlerin fiyatına göre böl`}
                            onClick={() => setRows((current) => current.flatMap((item) =>
                              item.key === row.key ? splitByPrice(item, suggestion.priceBySize, newRowKey) : [item]))}
                          >
                            <Split aria-hidden="true" />
                          </button>
                        )}
                        {suggestion?.problem && !suggestion.varies && (
                          <span className="order-grid__warn" title={suggestion.problem}>
                            <CircleAlert aria-label={suggestion.problem} />
                          </span>
                        )}
                        <input
                          type="text"
                          inputMode="decimal"
                          className="matrix-input order-grid__price-input"
                          aria-label={`${index + 1}. satır birim fiyat`}
                          aria-invalid={badPrice}
                          title={suggestion?.hint ? `Önerilen: ${suggestion.hint}` : undefined}
                          placeholder={suggestion?.price != null ? String(suggestion.price).replace('.', ',') : undefined}
                          value={row.unitPrice}
                          onChange={(event) => update(row.key, { unitPrice: event.target.value, priceTouched: true })}
                          autoComplete="off"
                        />
                      </span>
                    </td>
                    <td className="table__number">{formatMoney(rowAmount(row), '').trim()}</td>
                    <td className="order-grid__remove">
                      <button
                        type="button"
                        className="matrix-editor__remove"
                        aria-label={`${index + 1}. satırı sil`}
                        title="Satırı sil"
                        onClick={() => setRows((current) => (current.length === 1 ? [emptyRow()] : current.filter((item) => item.key !== row.key)))}
                      >
                        <Trash2 aria-hidden="true" />
                      </button>
                    </td>
                  </tr>
                )
              })}
            </tbody>
            <tfoot>
              <tr>
                <th colSpan={4} scope="row">Toplam</th>
                {sizes.map((size) => (
                  <td key={size.id} className="table__number order-grid__size">
                    {formatQuantity(filled.reduce((sum, row) => sum + quantityOf(row, size.id), 0))}
                  </td>
                ))}
                {sizes.length === 0 && <td />}
                <td className="table__number">{formatQuantity(totalQuantity)}</td>
                <td />
                <td className="table__number">{formatMoney(totalAmount, '').trim()}</td>
                <td />
              </tr>
            </tfoot>
          </table>
          <p className="muted order-lines__footnote">
            Birim fiyat boşsa fiyat listesinden (yoksa ürünün liste fiyatından) önerilir; müşteri iskontosu ve kur
            uygulanmıştır. Aynı ürün ve renk, farklı bedenlerle ayrı satırlarda farklı fiyattan girilebilir.
            {!canOverridePrice && ' İskontolu liste fiyatının altında fiyat girme yetkiniz yok.'}
          </p>
        </section>

        {attempted && problems.length > 0 && (
          <div className="alert alert--error" role="alert">
            Sipariş kaydedilmedi; aşağıdaki satırları tamamlayın ya da silin:
            <ul>
              {problems.map((problem) => (
                <li key={problem}>{problem}</li>
              ))}
            </ul>
          </div>
        )}
        {fieldErrors(error, 'lines') && (
          <p className="alert alert--error" role="alert">{fieldErrors(error, 'lines')!.join(' ')}</p>
        )}
        <ErrorAlert
          error={error}
          fields={['customerId', 'customerBuyerId', 'deliveryAddressId', 'customerOrderNo', 'orderDate', 'dueDate', 'currency', 'lines', 'notes']}
        />
      </form>
    </>
  )
}

export function SalesOrderCreatePage() {
  // "Siparişi kopyala": ?copy={id} ile açılırsa form o siparişin kalemleriyle dolu gelir.
  const [searchParams] = useSearchParams()
  const copyId = Number(searchParams.get('copy') ?? 0)
  const source = useGetSalesOrderQuery(copyId, { skip: !copyId })

  if (copyId && !source.data) {
    return (
      <section className="page">
        {source.isLoading && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={source.error} />
      </section>
    )
  }

  return (
    <section className="page">
      <OrderForm
        key={copyId || 'new'}
        initial={stateOf(source.data, true)}
        formKey="sales-order:new"
        title={source.data ? `Yeni müşteri siparişi (${source.data.orderNo} kopyası)` : 'Yeni müşteri siparişi'}
        intro="Sipariş taslak olarak kaydedilir. Müşteri seçildikten sonra girişleriniz otomatik saklanır; sayfa yenilense de kaybolmaz."
        backTo="/sales-orders"
        backLabel="Müşteri siparişlerine dön"
      />
    </section>
  )
}

export function SalesOrderEditPage() {
  const id = Number(useParams().id)
  const { data: order, isLoading, error } = useGetSalesOrderQuery(id)
  const [conflicted, setConflicted] = useState(false)

  // key: başka bir kullanıcı kaydedip sürüm değişirse form güncel veriyle yeniden kurulur.
  if (order?.isEditable) {
    return (
      <section className="page">
        <OrderForm
          key={order.rowVersion}
          order={order}
          initial={stateOf(order)}
          formKey={`sales-order:${order.id}`}
          title={`Siparişi düzenle: ${order.orderNo}`}
          backTo={`/sales-orders/${order.id}`}
          backLabel="Siparişe dön"
          onConflict={() => setConflicted(true)}
          notice={conflicted && (
            <p className="alert alert--error" role="alert">
              Sipariş siz düzenlerken başka bir kullanıcı tarafından değiştirildi. Form güncel verilerle yenilendi;
              değişikliklerinizi kontrol ederek tekrar girin.
            </p>
          )}
        />
      </section>
    )
  }

  return (
    <section className="page">
      <p><Link to={`/sales-orders/${id}`}>← Siparişe dön</Link></p>
      <h1>Siparişi düzenle{order ? `: ${order.orderNo}` : ''}</h1>

      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {order && <p className="alert alert--info">Bu sipariş artık düzenlenemez; yalnızca taslak sipariş değiştirilebilir.</p>}
    </section>
  )
}
