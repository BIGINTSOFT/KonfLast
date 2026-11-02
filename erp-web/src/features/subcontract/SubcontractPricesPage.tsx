import { Plus, Search, Trash2 } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { fieldErrors } from '../../api/envelope'
import { ErrorAlert, TextField } from '../../components/FormControls'
import { ClearFiltersButton } from '../../components/grid/ClearFiltersButton'
import { GridHead } from '../../components/grid/GridHead'
import type { GridColumn } from '../../components/grid/GridHead'
import { useGrid } from '../../components/grid/useGrid'
import { Modal } from '../../components/Modal'
import { Pagination } from '../../components/Pagination'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDate, formatDateTime, todayIsoDate } from '../../utils/format'
import { formatRate, parseDecimal } from '../../utils/money'
import { CurrencySelect } from '../finance/CurrencySelect'
import { LOCAL_CURRENCY } from '../finance/currencyOptions'
import { LookupSelect } from '../masterData/LookupSelect'
import { useDeleteSubcontractPriceMutation, useGetSubcontractPricesQuery, useSaveSubcontractPriceMutation } from './subcontractApi'
import type { SubcontractPrice } from './subcontractApi'

const PAGE_SIZE = 25

function PriceDialog({ price, onClose }: { price?: SubcontractPrice; onClose: () => void }) {
  const canManage = usePermission(Permissions.SubcontractManage)
  const [save, { isLoading, error }] = useSaveSubcontractPriceMutation()
  const [remove, removing] = useDeleteSubcontractPriceMutation()
  const [supplierId, setSupplierId] = useState<number | null>(price?.supplierId ?? null)
  const [operationId, setOperationId] = useState<number | null>(price?.operationId ?? null)
  const [productId, setProductId] = useState<number | null>(price?.productId ?? null)
  const [unitPrice, setUnitPrice] = useState(price ? String(price.unitPrice).replace('.', ',') : '')
  const [currency, setCurrency] = useState(price?.currency ?? LOCAL_CURRENCY)
  const [validFrom, setValidFrom] = useState(price?.validFrom ?? todayIsoDate())
  const [notes, setNotes] = useState(price?.notes ?? '')

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await save({
      id: price?.id,
      body: {
        supplierId: supplierId ?? 0,
        operationId: operationId ?? 0,
        productId,
        // Geçersiz giriş -1 gönderilir; sunucu kuralı söyler.
        unitPrice: parseDecimal(unitPrice) ?? -1,
        currency,
        validFrom,
        notes: notes.trim() || null,
      },
    })
    if (!('error' in result)) {
      onClose()
    }
  }

  const handleDelete = async () => {
    if (price && window.confirm('Fason fiyatı silinsin mi? Daha önce kesilmiş fişlerin fiyatı değişmez.')) {
      const result = await remove(price.id)
      if (!('error' in result)) {
        onClose()
      }
    }
  }

  return (
    <Modal title={price ? 'Fason fiyatını düzenle' : 'Yeni fason fiyatı'} onClose={onClose}>
      <form className="form" onSubmit={(event) => void handleSubmit(event)} noValidate>
        <fieldset className="form__section" disabled={!canManage}>
          <legend>Fiyat</legend>
          <LookupSelect resource="suppliers" label="Fasoncu" value={supplierId} onChange={setSupplierId} required errors={fieldErrors(error, 'supplierId')} />
          <LookupSelect resource="operations" label="Operasyon" value={operationId} onChange={setOperationId} required errors={fieldErrors(error, 'operationId')} />
          <LookupSelect
            resource="products"
            label="Ürün"
            value={productId}
            onChange={setProductId}
            placeholder="— Tüm ürünler —"
            errors={fieldErrors(error, 'productId')}
          />
          <div className="form__pair">
            <TextField
              label="Adet başı fiyat"
              inputMode="decimal"
              value={unitPrice}
              onChange={(event) => setUnitPrice(event.target.value)}
              errors={fieldErrors(error, 'unitPrice')}
            />
            <CurrencySelect label="Para birimi" value={currency} onChange={setCurrency} errors={fieldErrors(error, 'currency')} />
          </div>
          <TextField
            label="Geçerlilik başlangıcı"
            type="date"
            value={validFrom}
            onChange={(event) => setValidFrom(event.target.value)}
            errors={fieldErrors(error, 'validFrom')}
            hint="Yeni fiyat, başlangıcından itibaren eskisinin yerine geçer. Ürüne özel fiyat genel fiyatın önündedir."
          />
          <label className="field">
            <span>Not</span>
            <textarea rows={2} maxLength={500} value={notes} onChange={(event) => setNotes(event.target.value)} />
          </label>
        </fieldset>
        {price?.updatedAt && (
          <p className="muted form__note">Son değişiklik: {price.updatedBy ?? '—'} · {formatDateTime(price.updatedAt)}</p>
        )}
        <ErrorAlert error={error ?? removing.error} fields={['supplierId', 'operationId', 'productId', 'unitPrice', 'currency', 'validFrom']} />
        <footer className="form__actions">
          {price && canManage && (
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

const columns: GridColumn[] = [
  { key: 'supplierName', label: 'Fasoncu' },
  { key: 'operationName', label: 'Operasyon' },
  { key: 'productCode', label: 'Ürün' },
  { key: 'unitPrice', label: 'Adet fiyatı', numeric: true },
  { key: 'validFrom', label: 'Başlangıç', filter: 'date' },
  { label: 'Durum' },
  { key: 'notes', label: 'Not' },
]

/** Fason fiyatları: fasoncu + operasyon (+ ürün) için adet başı fiyat, başlangıç tarihiyle. */
export function SubcontractPricesPage() {
  const canManage = usePermission(Permissions.SubcontractManage)
  const grid = useGrid()
  const { page, setPage } = grid
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [currentOnly, setCurrentOnly] = useState(true)
  // undefined: kapalı, null: yeni fiyat.
  const [open, setOpen] = useState<SubcontractPrice | null | undefined>(undefined)

  const { data, error, isLoading, isFetching } = useGetSubcontractPricesQuery({
    page,
    pageSize: PAGE_SIZE,
    search: search || undefined,
    currentOnly: currentOnly || undefined,
    ...grid.params,
  })

  const handleSearch = (event: FormEvent) => {
    event.preventDefault()
    setPage(1)
    setSearch(searchInput.trim())
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">Fason Fiyatları</h1>

      <form className="toolbar" onSubmit={handleSearch} role="search">
        <input
          type="search"
          placeholder="Fasoncu, operasyon ya da ürün"
          aria-label="Fason fiyatı ara"
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
              Yeni fiyat
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
                {data.items.map((price) => (
                  <tr key={price.id}>
                    <td>
                      <button type="button" className="table__open" onClick={() => setOpen(price)}>
                        <code>{price.supplierCode}</code> {price.supplierName}
                      </button>
                    </td>
                    <td><code>{price.operationCode}</code> {price.operationName}</td>
                    <td>
                      {price.productId === null
                        ? <span className="badge badge--info">Tüm ürünler</span>
                        : <><code>{price.productCode}</code> {price.productName}</>}
                    </td>
                    <td className="table__number">{formatRate(price.unitPrice, price.currency)}</td>
                    <td>{formatDate(price.validFrom)}</td>
                    <td>{price.isCurrent ? <span className="badge badge--ok">Geçerli</span> : <span className="muted">Geçmiş / ileri tarihli</span>}</td>
                    <td className="muted">{price.notes ?? ''}</td>
                  </tr>
                ))}
                {data.items.length === 0 && (
                  <tr>
                    <td colSpan={columns.length} className="table__empty">Fason fiyatı bulunamadı.</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}

      {open !== undefined && <PriceDialog price={open ?? undefined} onClose={() => setOpen(undefined)} />}
    </section>
  )
}
