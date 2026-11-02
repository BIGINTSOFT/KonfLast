import { Plus, Search, Trash2 } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { fieldErrors } from '../../api/envelope'
import { ErrorAlert, TextField } from '../../components/FormControls'
import { GridHead } from '../../components/grid/GridHead'
import type { GridColumn } from '../../components/grid/GridHead'
import { useGrid } from '../../components/grid/useGrid'
import { Modal } from '../../components/Modal'
import { Pagination } from '../../components/Pagination'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDate, formatDateTime, todayIsoDate } from '../../utils/format'
import { parseDecimal } from '../../utils/money'
import { LookupSelect } from '../masterData/LookupSelect'
import { useDeleteCustomerDiscountMutation, useGetCustomerDiscountsQuery, useSaveCustomerDiscountMutation } from './pricingApi'
import type { CustomerDiscount } from './pricingApi'
import { ClearFiltersButton } from '../../components/grid/ClearFiltersButton'

const PAGE_SIZE = 25

const percentFormat = new Intl.NumberFormat('tr-TR', { maximumFractionDigits: 2 })

function DiscountDialog({ discount, onClose }: { discount?: CustomerDiscount; onClose: () => void }) {
  const canManage = usePermission(Permissions.PricingManage)
  const [save, { isLoading, error }] = useSaveCustomerDiscountMutation()
  const [remove, removing] = useDeleteCustomerDiscountMutation()
  const [customerId, setCustomerId] = useState<number | null>(discount?.customerId ?? null)
  const [productId, setProductId] = useState<number | null>(discount?.productId ?? null)
  const [percent, setPercent] = useState(discount ? String(discount.discountPercent).replace('.', ',') : '')
  const [validFrom, setValidFrom] = useState(discount?.validFrom ?? todayIsoDate())
  const [validTo, setValidTo] = useState(discount?.validTo ?? '')
  const [notes, setNotes] = useState(discount?.notes ?? '')

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await save({
      id: discount?.id,
      body: {
        customerId: customerId ?? 0,
        productId,
        // Geçersiz giriş 0 gönderilir; sunucu kuralı söyler.
        discountPercent: parseDecimal(percent) ?? 0,
        validFrom,
        validTo: validTo || null,
        notes: notes.trim() || null,
      },
    })
    if (!('error' in result)) {
      onClose()
    }
  }

  const handleDelete = async () => {
    if (discount && window.confirm('İskonto silinsin mi? Siparişlere yazılmış fiyatlar değişmez.')) {
      const result = await remove(discount.id)
      if (!('error' in result)) {
        onClose()
      }
    }
  }

  return (
    <Modal title={discount ? 'İskontoyu düzenle' : 'Yeni iskonto'} onClose={onClose}>
      <form className="form" onSubmit={(event) => void handleSubmit(event)} noValidate>
        <fieldset className="form__section" disabled={!canManage}>
          <legend>İskonto</legend>
          <LookupSelect resource="customers" label="Müşteri" value={customerId} onChange={setCustomerId} required errors={fieldErrors(error, 'customerId')} />
          <LookupSelect
            resource="products"
            label="Ürün"
            value={productId}
            onChange={setProductId}
            placeholder="— Tüm ürünler (genel iskonto) —"
            errors={fieldErrors(error, 'productId')}
          />
          <TextField
            label="İskonto (%)"
            inputMode="decimal"
            value={percent}
            onChange={(event) => setPercent(event.target.value)}
            errors={fieldErrors(error, 'discountPercent')}
            hint="Ürüne özel iskonto, o üründe genel iskontonun yerine geçer (üst üste uygulanmaz)."
          />
          <div className="form__pair">
            <TextField label="Başlangıç" type="date" value={validFrom} onChange={(event) => setValidFrom(event.target.value)} errors={fieldErrors(error, 'validFrom')} />
            <TextField label="Bitiş" type="date" min={validFrom} value={validTo} onChange={(event) => setValidTo(event.target.value)} errors={fieldErrors(error, 'validTo')} hint="Boşsa süresiz." />
          </div>
          <label className="field">
            <span>Not</span>
            <textarea rows={2} maxLength={500} value={notes} onChange={(event) => setNotes(event.target.value)} />
          </label>
        </fieldset>
        {discount?.updatedAt && (
          <p className="muted form__note">Son değişiklik: {discount.updatedBy ?? '—'} · {formatDateTime(discount.updatedAt)}</p>
        )}
        <ErrorAlert error={error ?? removing.error} fields={['customerId', 'productId', 'discountPercent', 'validFrom', 'validTo']} />
        <footer className="form__actions">
          {discount && canManage && (
            <button type="button" className="button button--danger-ghost form__actions-start" onClick={() => void handleDelete()}>
              <Trash2 aria-hidden="true" />
              Sil
            </button>
          )}
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          {canManage && (
            <button type="submit" className="button button--primary" disabled={isLoading}>
              {isLoading ? 'Kaydediliyor…' : 'Kaydet'}
            </button>
          )}
        </footer>
      </form>
    </Modal>
  )
}

/** Müşteri iskontoları: genel (tüm ürünler) ya da ürüne özel, geçerlilik tarihleriyle. */
export function CustomerDiscountsPage() {
  const canManage = usePermission(Permissions.PricingManage)
  const grid = useGrid()
  const { page, setPage } = grid
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [currentOnly, setCurrentOnly] = useState(true)
  // undefined: kapalı, null: yeni iskonto.
  const [open, setOpen] = useState<CustomerDiscount | null | undefined>(undefined)

  const { data, error, isLoading, isFetching } = useGetCustomerDiscountsQuery({
    page,
    pageSize: PAGE_SIZE,
    search: search || undefined,
    currentOnly: currentOnly || undefined,
    ...grid.params,
  })

  const columns: GridColumn[] = [
    { key: 'customerName', label: 'Müşteri' },
    { key: 'productName', label: 'Ürün' },
    { key: 'discountPercent', label: 'İskonto', numeric: true },
    { key: 'validFrom', label: 'Başlangıç', filter: 'date' },
    { key: 'validTo', label: 'Bitiş', filter: 'date' },
    { key: 'notes', label: 'Not' },
    { key: 'updatedBy', label: 'Güncelleyen' },
  ]

  const handleSearch = (event: FormEvent) => {
    event.preventDefault()
    setPage(1)
    setSearch(searchInput.trim())
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">Müşteri İskontoları</h1>

      <form className="toolbar" onSubmit={handleSearch} role="search">
        <input
          type="search"
          placeholder="Müşteri ya da ürün"
          aria-label="İskonto ara"
          value={searchInput}
          onChange={(event) => setSearchInput(event.target.value)}
        />
        <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
        <label className="checkbox">
          <input
            type="checkbox"
            checked={currentOnly}
            onChange={(event) => {
              setPage(1)
              setCurrentOnly(event.target.checked)
            }}
          />
          Yalnızca bugün geçerli olanlar
        </label>
        <ClearFiltersButton grid={grid} />
        <div className="toolbar__actions">
          {canManage && (
            <button type="button" className="button button--primary" onClick={() => setOpen(null)}>
              <Plus aria-hidden="true" />
              Yeni iskonto
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
                {data.items.map((discount) => (
                  <tr key={discount.id}>
                    <td>
                      <button type="button" className="table__open" onClick={() => setOpen(discount)}>
                        <code>{discount.customerCode}</code> {discount.customerName}
                      </button>
                    </td>
                    <td>
                      {discount.productId === null
                        ? <span className="badge badge--info">Genel iskonto</span>
                        : <><code>{discount.productCode}</code> {discount.productName}</>}
                    </td>
                    <td className="table__number">%{percentFormat.format(discount.discountPercent)}</td>
                    <td>{formatDate(discount.validFrom)}</td>
                    <td>{discount.validTo ? formatDate(discount.validTo) : <span className="muted">Süresiz</span>}</td>
                    <td className="muted">{discount.notes ?? ''}</td>
                    <td className="muted">{discount.updatedBy ?? '—'}</td>
                  </tr>
                ))}
                {data.items.length === 0 && (
                  <tr>
                    <td colSpan={columns.length} className="table__empty">İskonto bulunamadı.</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}

      {open !== undefined && <DiscountDialog discount={open ?? undefined} onClose={() => setOpen(undefined)} />}
    </section>
  )
}
