import { Trash2, X } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { fieldErrors } from '../../api/envelope'
import { ErrorAlert, TextField } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDateTime, todayIsoDate } from '../../utils/format'
import { parseDecimal } from '../../utils/money'
import { CurrencySelect } from '../finance/CurrencySelect'
import { LookupSelect } from '../masterData/LookupSelect'
import { useGetLookupQuery } from '../masterData/masterDataApi'
import { useDeletePriceListMutation, useGetPriceListQuery, useSavePriceListMutation } from './pricingApi'
import type { PriceList } from './pricingApi'

interface ItemRow {
  key: string
  productId: number | null
  colorId: number | null
  sizeId: number | null
  unitPrice: string
}

const emptyRow = (): ItemRow => ({ key: crypto.randomUUID(), productId: null, colorId: null, sizeId: null, unitPrice: '' })

const toRows = (list: PriceList): ItemRow[] =>
  list.items.map((item) => ({
    key: String(item.id),
    productId: item.productId,
    colorId: item.colorId,
    sizeId: item.sizeId,
    unitPrice: String(item.unitPrice).replace('.', ','),
  }))

function PriceListForm({ list, onClose }: { list?: PriceList; onClose: () => void }) {
  const canManage = usePermission(Permissions.PricingManage)
  const [save, { isLoading, error }] = useSavePriceListMutation()
  const [remove, removing] = useDeletePriceListMutation()
  const colors = useGetLookupQuery({ resource: 'colors', take: 200 })
  const sizes = useGetLookupQuery({ resource: 'sizes', take: 200 })

  const [code, setCode] = useState(list?.code ?? '')
  const [name, setName] = useState(list?.name ?? '')
  const [customerId, setCustomerId] = useState<number | null>(list?.customerId ?? null)
  const [currency, setCurrency] = useState(list?.currency ?? 'TRY')
  const [validFrom, setValidFrom] = useState(list?.validFrom ?? todayIsoDate())
  const [validTo, setValidTo] = useState(list?.validTo ?? '')
  const [notes, setNotes] = useState(list?.notes ?? '')
  const [isActive, setIsActive] = useState(list?.isActive ?? true)
  const [rows, setRows] = useState<ItemRow[]>(() => (list && list.items.length > 0 ? toRows(list) : [emptyRow()]))
  const [attempted, setAttempted] = useState(false)

  const update = (key: string, change: Partial<ItemRow>) =>
    setRows((current) => current.map((row) => (row.key === key ? { ...row, ...change } : row)))

  const started = rows.filter((row) => row.productId !== null || row.unitPrice.trim() !== '')
  const invalid = started.filter((row) => row.productId === null || parseDecimal(row.unitPrice) === null)

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    setAttempted(true)
    if (invalid.length > 0) {
      return
    }
    const result = await save({
      id: list?.id,
      body: {
        code,
        name,
        customerId,
        currency,
        validFrom,
        validTo: validTo || null,
        notes: notes.trim() || null,
        isActive,
        items: started.map((row) => ({
          productId: row.productId!,
          colorId: row.colorId,
          sizeId: row.sizeId,
          unitPrice: parseDecimal(row.unitPrice)!,
        })),
      },
    })
    if (!('error' in result)) {
      onClose()
    }
  }

  const handleDelete = async () => {
    if (list && window.confirm(`${list.code} fiyat listesi silinsin mi? Siparişlere yazılmış fiyatlar değişmez.`)) {
      const result = await remove(list.id)
      if (!('error' in result)) {
        onClose()
      }
    }
  }

  const idOf = (value: string) => (value === '' ? null : Number(value))

  return (
    <form className="form price-list-form" onSubmit={(event) => void handleSubmit(event)} noValidate>
      <fieldset className="form__section" disabled={!canManage}>
        <legend>Liste</legend>
        <div className="form__pair">
          <TextField label="Kod" value={code} maxLength={30} required onChange={(event) => setCode(event.target.value.toUpperCase())} errors={fieldErrors(error, 'code')} />
          <TextField label="Ad" value={name} maxLength={200} required onChange={(event) => setName(event.target.value)} errors={fieldErrors(error, 'name')} />
        </div>
        <div className="form__pair">
          <LookupSelect
            resource="customers"
            label="Müşteri"
            value={customerId}
            onChange={setCustomerId}
            placeholder="— Genel liste (tüm müşteriler) —"
            errors={fieldErrors(error, 'customerId')}
          />
          <CurrencySelect label="Para birimi" value={currency} onChange={setCurrency} errors={fieldErrors(error, 'currency')} />
        </div>
        <div className="form__pair">
          <TextField label="Geçerlilik başlangıcı" type="date" value={validFrom} required onChange={(event) => setValidFrom(event.target.value)} errors={fieldErrors(error, 'validFrom')} />
          <TextField
            label="Geçerlilik bitişi"
            type="date"
            min={validFrom}
            value={validTo}
            onChange={(event) => setValidTo(event.target.value)}
            errors={fieldErrors(error, 'validTo')}
            hint="Boşsa süresiz."
          />
        </div>
        <label className="field">
          <span>Not</span>
          <textarea rows={1} maxLength={500} value={notes} onChange={(event) => setNotes(event.target.value)} />
        </label>
        <label className="checkbox">
          <input type="checkbox" checked={isActive} onChange={(event) => setIsActive(event.target.checked)} />
          Aktif
        </label>
      </fieldset>

      <fieldset className="form__section" disabled={!canManage}>
        <legend>Fiyatlar ({started.length})</legend>
        <p className="muted form__note">
          Renk ya da beden boş bırakılırsa fiyat ürünün tüm renk / bedenleri için geçerlidir. Belirli bir beden (ör. XL)
          için ayrı satır girilirse o beden bu fiyattan satılır.
        </p>
        <div className="table-wrap recipe-table price-table">
          <table className="table table--compact">
            <colgroup>
              <col />
              <col className="price-table__variant" />
              <col className="price-table__variant" />
              <col className="price-table__price" />
              <col className="recipe-table__remove" />
            </colgroup>
            <thead>
              <tr>
                <th>Ürün</th>
                <th>Renk</th>
                <th>Beden</th>
                <th className="table__number">Birim fiyat ({currency})</th>
                <th aria-label="Satırı kaldır" />
              </tr>
            </thead>
            <tbody>
              {rows.map((row, index) => {
                const rowInvalid = attempted && invalid.includes(row)
                return (
                  <tr key={row.key} className={rowInvalid ? 'price-table__row--invalid' : undefined}>
                    <td>
                      <LookupSelect
                        resource="products"
                        idSuffix={row.key}
                        label={`${index + 1}. ürün`}
                        hideLabel
                        value={row.productId}
                        onChange={(productId) => update(row.key, { productId })}
                        placeholder="Ürün seçin…"
                        customerId={customerId}
                      />
                    </td>
                    <td>
                      <select aria-label="Renk" value={row.colorId ?? ''} onChange={(event) => update(row.key, { colorId: idOf(event.target.value) })}>
                        <option value="">Tümü</option>
                        {colors.data?.map((color) => <option key={color.id} value={color.id}>{color.name}</option>)}
                      </select>
                    </td>
                    <td>
                      <select aria-label="Beden" value={row.sizeId ?? ''} onChange={(event) => update(row.key, { sizeId: idOf(event.target.value) })}>
                        <option value="">Tümü</option>
                        {sizes.data?.map((size) => <option key={size.id} value={size.id}>{size.name}</option>)}
                      </select>
                    </td>
                    <td>
                      <input
                        className="price-table__input"
                        aria-label="Birim fiyat"
                        inputMode="decimal"
                        value={row.unitPrice}
                        onChange={(event) => update(row.key, { unitPrice: event.target.value })}
                      />
                    </td>
                    <td className="recipe-table__remove-cell">
                      <button
                        type="button"
                        className="icon-button"
                        aria-label="Satırı kaldır"
                        title="Satırı kaldır"
                        onClick={() => setRows((current) => current.filter((item) => item.key !== row.key))}
                      >
                        <X aria-hidden="true" />
                      </button>
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
        <div>
          <button type="button" className="button" onClick={() => setRows((current) => [...current, emptyRow()])}>Satır ekle</button>
        </div>
        {attempted && invalid.length > 0 && (
          <small className="field__error">Her satırda ürün ve geçerli bir fiyat (örn. 125,50) girilmelidir.</small>
        )}
        {fieldErrors(error, 'items')?.map((message) => <small key={message} className="field__error">{message}</small>)}
      </fieldset>

      {list && (
        <p className="muted form__note">
          Oluşturan: {list.createdBy ?? '—'} · {formatDateTime(list.createdAt)}
          {list.updatedAt && ` · Son değişiklik: ${list.updatedBy ?? '—'} · ${formatDateTime(list.updatedAt)}`}
        </p>
      )}

      <ErrorAlert error={error ?? removing.error} fields={['code', 'name', 'customerId', 'currency', 'validFrom', 'validTo', 'items']} />

      <footer className="form__actions">
        {list && canManage && (
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
  )
}

/** Fiyat listesi kaydı: yeni liste ya da var olanın düzenlenmesi (sağdan açılan panel). */
export function PriceListDialog({ id, onClose }: { id?: number; onClose: () => void }) {
  const { data, isLoading, error } = useGetPriceListQuery(id ?? 0, { skip: id === undefined })
  return (
    <Modal title={id === undefined ? 'Yeni fiyat listesi' : `Fiyat listesi: ${data?.code ?? ''}`} onClose={onClose} variant="drawer">
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {(id === undefined || data) && <PriceListForm list={data} onClose={onClose} />}
    </Modal>
  )
}
