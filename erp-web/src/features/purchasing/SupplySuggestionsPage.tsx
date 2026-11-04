import { Search, ShoppingCart } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDate } from '../../utils/format'
import { useGetLookupQuery } from '../masterData/masterDataApi'
import type { StockItemType } from '../stock/stockApi'
import {
  formatStockQuantity, manualStockItemTypes, parseQuantity, quantityStep, stockItemTypeLabels, stockUnitLabels,
} from '../stock/stockPresentation'
import { useCreateSupplyOrdersMutation, useGetSupplySuggestionsQuery } from './purchasingApi'
import type { PurchaseOrder, SupplySuggestion } from './purchasingApi'

/** Kullanıcının bir satırda yaptığı değişiklik; yoksa önerilen miktar ve kartın tedarikçisi geçerlidir. */
interface RowEdit {
  quantity?: string
  supplierId?: number | null
}

/**
 * Tedarik önerileri: açık iş emirlerinin ihtiyacı ve asgari stok için eksik kalan malzemeler. Seçilen
 * satırlar tedarikçi başına bir taslak satın alma siparişine dönüştürülür; onay satın alma ekranında verilir.
 */
export function SupplySuggestionsPage() {
  const canManage = usePermission(Permissions.PurchaseManage)
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [itemType, setItemType] = useState<StockItemType | ''>('')
  const [includeCovered, setIncludeCovered] = useState(false)
  const [selected, setSelected] = useState<Set<number>>(new Set())
  const [edits, setEdits] = useState<Record<number, RowEdit>>({})
  const [created, setCreated] = useState<PurchaseOrder[] | null>(null)

  const { data, error, isLoading, isFetching } = useGetSupplySuggestionsQuery({
    search: search || undefined,
    itemType: itemType || undefined,
    includeCovered: includeCovered || undefined,
  })
  const suppliers = useGetLookupQuery({ resource: 'suppliers', take: 100 })
  const [createOrders, createState] = useCreateSupplyOrdersMutation()

  const rows = (data ?? []).map((line) => {
    const edit = edits[line.stockItemId] ?? {}
    const quantityText = edit.quantity ?? (line.suggestedQuantity > 0 ? String(line.suggestedQuantity) : '')
    const quantity = parseQuantity(quantityText)
    return {
      line,
      quantityText,
      quantity: quantity !== null && quantity > 0 ? quantity : null,
      supplierId: edit.supplierId !== undefined ? edit.supplierId : line.supplierId,
      checked: selected.has(line.stockItemId),
    }
  })
  const chosen = rows.filter((row) => row.checked)
  const incomplete = chosen.some((row) => row.quantity === null || row.supplierId === null)
  const selectable = rows.filter((row) => row.line.shortage > 0)
  const allSelected = selectable.length > 0 && selectable.every((row) => row.checked)
  const supplierCount = new Set(chosen.map((row) => row.supplierId)).size

  const edit = (line: SupplySuggestion, change: RowEdit) =>
    setEdits((current) => ({ ...current, [line.stockItemId]: { ...current[line.stockItemId], ...change } }))

  const toggle = (stockItemId: number, checked: boolean) =>
    setSelected((current) => {
      const next = new Set(current)
      if (checked) {
        next.add(stockItemId)
      } else {
        next.delete(stockItemId)
      }
      return next
    })

  const handleSearch = (event: FormEvent) => {
    event.preventDefault()
    setSearch(searchInput.trim())
  }

  const handleCreate = async () => {
    if (chosen.length === 0 || incomplete) {
      return
    }

    const result = await createOrders({
      lines: chosen.map((row) => ({ stockItemId: row.line.stockItemId, quantity: row.quantity!, supplierId: row.supplierId })),
      expectedDate: null,
    })
    if (!('error' in result)) {
      setCreated(result.data)
      setSelected(new Set())
      setEdits({})
    }
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">Tedarik Önerileri</h1>

      <form className="toolbar" onSubmit={handleSearch} role="search">
        <input
          type="search"
          placeholder="Stok kartı kodu veya adı"
          aria-label="Stok kartı ara"
          value={searchInput}
          onChange={(event) => setSearchInput(event.target.value)}
        />
        <select aria-label="Tür filtresi" value={itemType} onChange={(event) => setItemType(event.target.value as StockItemType | '')}>
          <option value="">Tüm türler</option>
          {manualStockItemTypes.map((type) => (
            <option key={type} value={type}>{stockItemTypeLabels[type]}</option>
          ))}
        </select>
        <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
        <label className="checkbox">
          <input type="checkbox" checked={includeCovered} onChange={(event) => setIncludeCovered(event.target.checked)} />
          Eksiği olmayanları da göster
        </label>
        <div className="toolbar__actions">
          {canManage && (
            <button
              type="button"
              className="button button--primary"
              disabled={chosen.length === 0 || incomplete || createState.isLoading}
              title={incomplete ? 'Seçili her satırda miktar ve tedarikçi olmalıdır.' : undefined}
              onClick={() => void handleCreate()}
            >
              <ShoppingCart aria-hidden="true" />
              {createState.isLoading
                ? 'Oluşturuluyor…'
                : chosen.length === 0
                  ? 'Taslak sipariş oluştur'
                  : `Taslak sipariş oluştur (${chosen.length} satır, ${supplierCount} tedarikçi)`}
            </button>
          )}
        </div>
      </form>

      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error ?? createState.error} />

      {data && (
        <>
          <div className="table-wrap" aria-busy={isFetching}>
            <table className="table table--compact supply-table">
              <thead>
                <tr>
                  {canManage && (
                    <th>
                      <input
                        type="checkbox"
                        aria-label="Eksiği olan tüm satırları seç"
                        checked={allSelected}
                        disabled={selectable.length === 0}
                        onChange={(event) =>
                          setSelected(event.target.checked ? new Set(selectable.map((row) => row.line.stockItemId)) : new Set())
                        }
                      />
                    </th>
                  )}
                  <th>Stok kartı</th>
                  <th>Tür</th>
                  <th className="table__number">Stokta</th>
                  <th className="table__number">Sipariş ihtiyacı</th>
                  <th className="table__number">Asgari stok</th>
                  <th className="table__number">Yolda</th>
                  <th className="table__number">Taslakta</th>
                  <th className="table__number">Eksik</th>
                  {canManage && <th className="table__number">Sipariş miktarı</th>}
                  <th>Tedarikçi</th>
                </tr>
              </thead>
              <tbody>
                {rows.map(({ line, quantityText, quantity, supplierId, checked }) => (
                  <tr key={line.stockItemId}>
                    {canManage && (
                      <td>
                        <input
                          type="checkbox"
                          aria-label={`${line.itemCode} satırını seç`}
                          checked={checked}
                          onChange={(event) => toggle(line.stockItemId, event.target.checked)}
                        />
                      </td>
                    )}
                    <td><code>{line.itemCode}</code> {line.itemName}</td>
                    <td>{stockItemTypeLabels[line.itemType]}</td>
                    <td className="table__number">{formatStockQuantity(line.onHand, line.unit)}</td>
                    <td className="table__number">
                      {line.demand > 0 ? formatStockQuantity(line.demand, line.unit) : '—'}
                      {line.demands.length > 0 && (
                        <div className="muted supply-demands">
                          {line.demands.map((demand) => (
                            <Link
                              key={demand.productionOrderId}
                              to={`/production-orders/${demand.productionOrderId}`}
                              title={`Termin ${formatDate(demand.dueDate)} · ${formatStockQuantity(demand.quantity, line.unit)}`}
                            >
                              {demand.orderNo}
                            </Link>
                          ))}
                        </div>
                      )}
                    </td>
                    <td className="table__number">{line.minimumStock > 0 ? formatStockQuantity(line.minimumStock, line.unit) : '—'}</td>
                    <td className="table__number">{line.onOrder > 0 ? formatStockQuantity(line.onOrder, line.unit) : '—'}</td>
                    <td className="table__number">{line.inDraft > 0 ? formatStockQuantity(line.inDraft, line.unit) : '—'}</td>
                    <td className="table__number">
                      {line.shortage > 0 ? <strong>{formatStockQuantity(line.shortage, line.unit)}</strong> : <span className="muted">Yok</span>}
                    </td>
                    {canManage && (
                      <td className="table__number">
                        <input
                          type="number"
                          min={0}
                          step={quantityStep(line.unit)}
                          className="matrix-input"
                          aria-label={`${line.itemCode} sipariş miktarı (${stockUnitLabels[line.unit]})`}
                          aria-invalid={checked && quantity === null}
                          value={quantityText}
                          onChange={(event) => edit(line, { quantity: event.target.value })}
                        />
                      </td>
                    )}
                    <td>
                      {canManage ? (
                        <select
                          aria-label={`${line.itemCode} tedarikçisi`}
                          aria-invalid={checked && supplierId === null}
                          value={supplierId ?? ''}
                          onChange={(event) => edit(line, { supplierId: event.target.value ? Number(event.target.value) : null })}
                        >
                          <option value="">Seçin…</option>
                          {/* Kartın tedarikçisi pasifse ya da ilk yüz kayıtta değilse yine de görünür. */}
                          {line.supplierId !== null && !suppliers.data?.some((supplier) => supplier.id === line.supplierId) && (
                            <option value={line.supplierId}>{line.supplierName}</option>
                          )}
                          {suppliers.data?.map((supplier) => (
                            <option key={supplier.id} value={supplier.id}>{supplier.name}</option>
                          ))}
                        </select>
                      ) : (
                        line.supplierName ?? '—'
                      )}
                    </td>
                  </tr>
                ))}
                {rows.length === 0 && (
                  <tr>
                    <td colSpan={canManage ? 11 : 9} className="table__empty">
                      {includeCovered ? 'İhtiyacı ya da asgari stoğu tanımlı kart yok.' : 'Eksik malzeme yok.'}
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
          <p className="muted">
            Eksik = sipariş ihtiyacı + asgari stok − stokta − yolda − taslakta. İhtiyaç, onaylı ve üretimdeki siparişlerin henüz
            demeti açılmamış adetleri içindir. Asgari stok, sipariş katı ve varsayılan tedarikçi stok kartında tanımlanır.
          </p>
        </>
      )}

      {created && (
        <Modal title="Taslak siparişler oluşturuldu" onClose={() => setCreated(null)}>
          <p>
            {created.length} taslak satın alma siparişi açıldı. Fiyat ve teslim tarihini girip onayladığınızda mal kabul
            yapılabilir.
          </p>
          <ul>
            {created.map((order) => (
              <li key={order.id}>
                <Link to={`/purchase-orders/${order.id}`} onClick={() => setCreated(null)}><code>{order.orderNo}</code></Link>
                {' — '}{order.supplierName} ({order.items.length} kalem)
              </li>
            ))}
          </ul>
          <footer className="form__actions">
            <button type="button" className="button button--primary" onClick={() => setCreated(null)}>Tamam</button>
          </footer>
        </Modal>
      )}
    </section>
  )
}
