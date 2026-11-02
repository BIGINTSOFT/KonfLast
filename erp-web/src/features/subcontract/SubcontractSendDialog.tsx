import { Plus, Search, Trash2 } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { fieldErrors } from '../../api/envelope'
import { ErrorAlert, TextField } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { formatQuantity, todayIsoDate } from '../../utils/format'
import { formatRate } from '../../utils/money'
import { LookupSelect } from '../masterData/LookupSelect'
import type { LookupItem } from '../masterData/masterDataApi'
import {
  useCreateSubcontractSendMutation,
  useGetSubcontractCandidatesQuery,
  useGetSubcontractOperationOptionsQuery,
  useResolveSubcontractPriceQuery,
} from './subcontractApi'
import type { SubcontractSend } from './subcontractApi'
import { parseCount } from './subcontractPresentation'

interface ItemRow {
  key: string
  product: LookupItem | null
  colorId: number | null
  sizeId: number | null
  quantity: string
}

const OTHER = 'other'

/**
 * Yeni fason gönderimi: fasoncu, operasyon ve gönderilecek demetler (sıradaki işi bu operasyon olan açık demetler)
 * ya da demetsiz ürün / renk / beden adetleri. Kaydedilince demetler fasoncuda sayılır ve sahada işlenemez.
 */
export function SubcontractSendDialog({ onClose, onSaved }: { onClose: () => void; onSaved: (send: SubcontractSend) => void }) {
  const [create, { isLoading, error }] = useCreateSubcontractSendMutation()
  const { data: operationOptions } = useGetSubcontractOperationOptionsQuery()
  const [operationChoice, setOperationChoice] = useState('')
  const [otherOperationId, setOtherOperationId] = useState<number | null>(null)
  const [supplierId, setSupplierId] = useState<number | null>(null)
  const [sendDate, setSendDate] = useState(todayIsoDate())
  const [carrier, setCarrier] = useState('')
  const [notes, setNotes] = useState('')
  const [tab, setTab] = useState<'bundles' | 'items'>('bundles')
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [selected, setSelected] = useState<Set<number>>(new Set())
  const [items, setItems] = useState<ItemRow[]>([])

  const operationId = operationChoice === OTHER ? otherOperationId : operationChoice ? Number(operationChoice) : null
  const candidates = useGetSubcontractCandidatesQuery(
    { operationId: operationId ?? 0, search: search || undefined },
    { skip: operationId === null },
  )
  const price = useResolveSubcontractPriceQuery(
    { supplierId: supplierId ?? 0, operationId: operationId ?? 0, date: sendDate },
    { skip: supplierId === null || operationId === null || !sendDate },
  )

  const chooseOperation = (value: string) => {
    setOperationChoice(value)
    setSelected(new Set())
    const option = operationOptions?.find((item) => String(item.operationId) === value)
    if (option?.defaultSupplierId && supplierId === null) {
      setSupplierId(option.defaultSupplierId)
    }
  }

  const rows = candidates.data ?? []
  const selectedRows = rows.filter((row) => selected.has(row.bundleOperationId))
  const allSelected = rows.length > 0 && rows.every((row) => selected.has(row.bundleOperationId))

  const toggle = (id: number) =>
    setSelected((current) => {
      const next = new Set(current)
      if (next.has(id)) {
        next.delete(id)
      } else {
        next.add(id)
      }
      return next
    })

  const updateItem = (key: string, patch: Partial<ItemRow>) =>
    setItems((current) => current.map((row) => (row.key === key ? { ...row, ...patch } : row)))

  const handleSearch = (event: FormEvent) => {
    event.preventDefault()
    setSearch(searchInput.trim())
  }

  const handleSubmit = async () => {
    const result = await create({
      supplierId: supplierId ?? 0,
      operationId: operationId ?? 0,
      sendDate,
      carrier: carrier.trim() || null,
      notes: notes.trim() || null,
      bundleOperationIds: [...selected],
      items: items.map((row) => ({
        productId: row.product?.id ?? 0,
        colorId: row.colorId ?? 0,
        sizeId: row.sizeId ?? 0,
        quantity: parseCount(row.quantity),
      })),
    })
    if ('data' in result && result.data) {
      onSaved(result.data)
    }
  }

  const totalPieces = selectedRows.reduce((sum, row) => sum + row.quantity, 0)
    + items.reduce((sum, row) => sum + parseCount(row.quantity), 0)

  return (
    <Modal title="Yeni fason gönderimi" onClose={onClose} variant="drawer">
      <div className="form">
        <div className="form__grid">
          <label className="field">
            <span>Operasyon</span>
            <select value={operationChoice} onChange={(event) => chooseOperation(event.target.value)} aria-invalid={Boolean(fieldErrors(error, 'operationId'))}>
              <option value="">Seçin…</option>
              {(operationOptions ?? []).map((option) => (
                <option key={option.operationId} value={option.operationId}>
                  {option.operationCode} — {option.operationName}
                </option>
              ))}
              <option value={OTHER}>Başka bir operasyon…</option>
            </select>
            <small className="field__hint">Listede rotalarda fason işaretli operasyonlar var.</small>
          </label>
          {operationChoice === OTHER && (
            <LookupSelect
              resource="operations"
              label="Operasyon (tümü)"
              value={otherOperationId}
              onChange={(value) => {
                setOtherOperationId(value)
                setSelected(new Set())
              }}
              required
            />
          )}
          <LookupSelect resource="suppliers" label="Fasoncu" value={supplierId} onChange={setSupplierId} required errors={fieldErrors(error, 'supplierId')} />
          <TextField label="Gönderim tarihi" type="date" value={sendDate} onChange={(event) => setSendDate(event.target.value)} errors={fieldErrors(error, 'sendDate')} />
          <TextField label="Taşıyıcı / araç" value={carrier} maxLength={200} onChange={(event) => setCarrier(event.target.value)} />
        </div>
        {price.data && (
          <p className="muted">
            {price.data.unitPrice === null
              ? 'Bu fasoncu ve operasyon için fason fiyatı tanımlı değil; hakediş fiyatı dönüşte girilebilir.'
              : `Fason fiyatı: ${formatRate(price.data.unitPrice, price.data.currency ?? '')} / adet (ürüne özel fiyat varsa o geçerlidir).`}
          </p>
        )}

        <div className="tabs" role="tablist">
          <button type="button" role="tab" aria-selected={tab === 'bundles'} className={`tabs__tab${tab === 'bundles' ? ' tabs__tab--active' : ''}`} onClick={() => setTab('bundles')}>
            Demetler ({selected.size})
          </button>
          <button type="button" role="tab" aria-selected={tab === 'items'} className={`tabs__tab${tab === 'items' ? ' tabs__tab--active' : ''}`} onClick={() => setTab('items')}>
            Demetsiz satırlar ({items.length})
          </button>
        </div>

        {tab === 'bundles' && (
          <div className="tabs__panel">
            {operationId === null ? (
              <p className="muted">Önce operasyonu seçin; sıradaki işi bu operasyon olan açık demetler listelenir.</p>
            ) : (
              <>
                <form className="toolbar" onSubmit={handleSearch} role="search">
                  <input
                    type="search"
                    placeholder="Demet no, sipariş, model ya da ürün"
                    aria-label="Demet ara"
                    value={searchInput}
                    onChange={(event) => setSearchInput(event.target.value)}
                  />
                  <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
                </form>
                <ErrorAlert error={candidates.error} />
                <div className="table-wrap" aria-busy={candidates.isFetching}>
                  <table className="table table--compact">
                    <thead>
                      <tr>
                        <th>
                          <input
                            type="checkbox"
                            aria-label="Tümünü seç"
                            checked={allSelected}
                            onChange={() => setSelected(allSelected ? new Set() : new Set(rows.map((row) => row.bundleOperationId)))}
                          />
                        </th>
                        <th>Demet</th>
                        <th>Sipariş</th>
                        <th>Model / ürün</th>
                        <th>Renk</th>
                        <th>Beden</th>
                        <th className="table__number">Adım</th>
                        <th className="table__number">Adet</th>
                      </tr>
                    </thead>
                    <tbody>
                      {rows.map((row) => (
                        <tr key={row.bundleOperationId}>
                          <td>
                            <input
                              type="checkbox"
                              aria-label={`${row.bundleNo} seç`}
                              checked={selected.has(row.bundleOperationId)}
                              onChange={() => toggle(row.bundleOperationId)}
                            />
                          </td>
                          <td><code>{row.bundleNo}</code></td>
                          <td>{row.orderNo}</td>
                          <td>{row.modelCode} <span className="muted">{row.productCode}</span></td>
                          <td>{row.colorName}</td>
                          <td>{row.sizeName}</td>
                          <td className="table__number">
                            {row.sequenceNo}
                            {!row.isSubcontractedStep && <span className="muted" title="Rotada fason işaretli değil"> *</span>}
                          </td>
                          <td className="table__number">{formatQuantity(row.quantity)}</td>
                        </tr>
                      ))}
                      {rows.length === 0 && !candidates.isFetching && (
                        <tr><td colSpan={8} className="table__empty">Bu operasyon için gönderilebilecek demet yok.</td></tr>
                      )}
                    </tbody>
                  </table>
                </div>
                <small className="field__hint">
                  Demetin bu operasyonda kalan adedi gönderilir. * Rotada fason işaretli olmayan adım (yine de gönderilebilir).
                </small>
              </>
            )}
          </div>
        )}

        {tab === 'items' && (
          <div className="tabs__panel">
            <p className="muted">Demetle izlenmeyen işler için (ör. ütü, yıkama) ürün / renk / beden adedi girilir; üretim akışını etkilemez.</p>
            {items.length > 0 && (
              <div className="table-wrap">
                <table className="table table--compact">
                  <thead>
                    <tr>
                      <th>Ürün</th>
                      <th>Renk</th>
                      <th>Beden</th>
                      <th className="table__number">Adet</th>
                      <th aria-label="İşlemler" />
                    </tr>
                  </thead>
                  <tbody>
                    {items.map((row) => (
                      <tr key={row.key}>
                        <td>
                          <LookupSelect resource="products" label="Ürün" hideLabel idSuffix={row.key} value={row.product?.id ?? null} onChange={() => undefined} onSelectItem={(item) => updateItem(row.key, { product: item })} required />
                        </td>
                        <td>
                          <LookupSelect resource="colors" label="Renk" hideLabel idSuffix={row.key} value={row.colorId} onChange={(value) => updateItem(row.key, { colorId: value })} required />
                        </td>
                        <td>
                          <LookupSelect resource="sizes" label="Beden" hideLabel idSuffix={row.key} value={row.sizeId} onChange={(value) => updateItem(row.key, { sizeId: value })} required />
                        </td>
                        <td>
                          <input type="number" min={1} step={1} className="matrix-input" aria-label="Adet" value={row.quantity} onChange={(event) => updateItem(row.key, { quantity: event.target.value })} />
                        </td>
                        <td className="table__actions">
                          <button type="button" className="button button--ghost button--icon" aria-label="Satırı kaldır" onClick={() => setItems((current) => current.filter((other) => other.key !== row.key))}>
                            <Trash2 aria-hidden="true" />
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
            <button
              type="button"
              className="button"
              onClick={() => setItems((current) => [...current, { key: `item-${Date.now()}`, product: null, colorId: null, sizeId: null, quantity: '' }])}
            >
              <Plus aria-hidden="true" />
              Satır ekle
            </button>
          </div>
        )}

        <label className="field">
          <span>Not</span>
          <textarea rows={2} maxLength={1000} value={notes} onChange={(event) => setNotes(event.target.value)} />
        </label>

        <ErrorAlert error={error} fields={['supplierId', 'sendDate']} />
        <footer className="form__actions">
          <span className="muted form__actions-start">Toplam {formatQuantity(totalPieces)} adet</span>
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="button" className="button button--primary" disabled={isLoading || operationId === null} onClick={() => void handleSubmit()}>
            {isLoading ? 'Kaydediliyor…' : 'Gönderimi kaydet'}
          </button>
        </footer>
      </div>
    </Modal>
  )
}
