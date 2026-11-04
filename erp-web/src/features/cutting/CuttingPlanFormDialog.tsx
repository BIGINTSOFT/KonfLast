import { ArrowDown, ArrowUp, Search, X } from 'lucide-react'
import { useMemo, useState } from 'react'
import type { FormEvent } from 'react'
import { fieldErrors } from '../../api/envelope'
import { ErrorAlert, TextField } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { formatQuantity } from '../../utils/format'
import { useGetProductionOrderQuery } from '../productionOrders/productionOrdersApi'
import { formatStockQuantity } from '../stock/stockPresentation'
import { DifferenceCell } from './CuttingBadges'
import {
  useCreateCuttingPlanMutation,
  useGetCuttingLotOptionsQuery,
  useGetOrderCuttingSummaryQuery,
  useUpdateCuttingPlanMutation,
} from './cuttingApi'
import type { CuttingLotOption, CuttingPlan } from './cuttingApi'
import { DEFAULT_END_ALLOWANCE, fabricFor, formatMeters, parseDecimal, shadeText, suggestPlies } from './cuttingPresentation'

/** Seçili top (plan düzenlenirken plandaki toplar da bu biçime çevrilir). */
type SelectedLot = Pick<CuttingLotOption, 'lotId' | 'lotNo' | 'stockItemId' | 'stockItemCode' | 'unit' | 'shade' | 'batchNo' | 'balance' | 'locations'>

const normalize = (value: string | null) => (value ?? '').trim().toLocaleUpperCase('tr-TR')

/** Sunucudaki kuralın ön izlemesi: tonu olan toplarda ton, hiçbirinde yoksa parti karşılaştırılır. */
function shadeConflict(lots: SelectedLot[]): string | null {
  if (lots.length < 2) {
    return null
  }

  if (new Set(lots.map((lot) => lot.stockItemId)).size > 1) {
    return 'Bir pastaldaki toplar aynı kumaştan olmalıdır.'
  }

  const byShade = lots.some((lot) => normalize(lot.shade) !== '')
  const keys = new Set(lots.map((lot) => normalize(byShade ? lot.shade : lot.batchNo)))
  return keys.size > 1
    ? `Seçilen toplar farklı ${byShade ? 'tonlarda' : 'partilerde'}; bir pastalda tek ton kumaş kullanılır.`
    : null
}

/**
 * Kesim föyü oluşturma / düzenleme. Pastal boyu ve beden oranı girilir; kat sayısı kalan adetlerden önerilir,
 * kumaş ihtiyacı ve beden bazında planlanan / kalan karşılaştırması anında hesaplanır.
 */
export function CuttingPlanFormDialog({
  orderId, plan, onClose, onSaved,
}: { orderId: number; plan?: CuttingPlan; onClose: () => void; onSaved?: (plan: CuttingPlan) => void }) {
  const order = useGetProductionOrderQuery(orderId)
  const summary = useGetOrderCuttingSummaryQuery(orderId)
  const [create, createState] = useCreateCuttingPlanMutation()
  const [update, updateState] = useUpdateCuttingPlanMutation()
  const saving = createState.isLoading || updateState.isLoading
  const error = createState.error ?? updateState.error

  const colors = useMemo(() => {
    const seen = new Map<number, string>()
    for (const item of order.data?.items ?? []) {
      seen.set(item.colorId, item.colorName)
    }
    return [...seen.entries()].map(([id, name]) => ({ id, name }))
  }, [order.data])

  const [chosenColor, setChosenColor] = useState<number | null>(plan?.colorId ?? null)
  const colorId = chosenColor ?? colors[0]?.id ?? null
  const [markerLength, setMarkerLength] = useState(plan ? String(plan.markerLength) : '')
  const [endAllowance, setEndAllowance] = useState(String(plan?.endAllowance ?? DEFAULT_END_ALLOWANCE))
  const [plies, setPlies] = useState(plan ? String(plan.plannedPlies) : '')
  const [pliesTouched, setPliesTouched] = useState(Boolean(plan))
  const [ratios, setRatios] = useState<Record<number, string>>(
    () => Object.fromEntries((plan?.sizes ?? []).map((size) => [size.sizeId, String(size.ratio)])),
  )
  const [lots, setLots] = useState<SelectedLot[]>(() => plan?.lots.map((lot) => ({ ...lot })) ?? [])
  const [notes, setNotes] = useState(plan?.notes ?? '')
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const options = useGetCuttingLotOptionsQuery({
    productionOrderId: orderId,
    search: search || undefined,
    stockItemId: lots[0]?.stockItemId,
  })

  // Seçili rengin bedenleri, sipariş sırasıyla; kalan adet kesim özetinden gelir.
  const rows = useMemo(() => (order.data?.items ?? [])
    .filter((item) => item.colorId === colorId)
    .sort((a, b) => a.sizeSortOrder - b.sizeSortOrder || a.sizeCode.localeCompare(b.sizeCode, 'tr'))
    .map((item) => {
      const state = summary.data?.items.find((entry) => entry.colorId === item.colorId && entry.sizeId === item.sizeId)
      return { sizeId: item.sizeId, sizeName: item.sizeName, ordered: item.quantity, remaining: state?.remaining ?? item.quantity }
    }), [order.data, summary.data, colorId])

  const ratioOf = (sizeId: number) => {
    const value = Number(ratios[sizeId] ?? '')
    return Number.isInteger(value) && value > 0 ? value : 0
  }
  const suggested = suggestPlies(rows.map((row) => ({ ratio: ratioOf(row.sizeId), remaining: row.remaining })))
  // Kat sayısı elle değiştirilmedikçe öneriyi izler.
  const plyCount = pliesTouched ? Number(plies) || 0 : suggested
  const marker = parseDecimal(markerLength) ?? 0
  const allowance = parseDecimal(endAllowance) ?? 0
  const piecesPerPly = rows.reduce((sum, row) => sum + ratioOf(row.sizeId), 0)
  const fabricNeed = fabricFor(marker, allowance, plyCount)
  const lotTotal = lots.reduce((sum, lot) => sum + lot.balance, 0)
  const lotsInMeters = lots.length > 0 && lots.every((lot) => lot.unit === 'METER')
  const conflict = shadeConflict(lots)

  const addLot = (option: CuttingLotOption) => setLots((current) => (current.some((lot) => lot.lotId === option.lotId) ? current : [...current, option]))
  const moveLot = (index: number, offset: number) => setLots((current) => {
    const next = [...current]
    const [moved] = next.splice(index, 1)
    next.splice(index + offset, 0, moved)
    return next
  })

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    if (colorId === null) {
      return
    }

    const body = {
      productionOrderId: orderId,
      colorId,
      markerLength: marker,
      endAllowance: parseDecimal(endAllowance),
      plannedPlies: plyCount,
      sizes: rows.filter((row) => ratioOf(row.sizeId) > 0).map((row) => ({ sizeId: row.sizeId, ratio: ratioOf(row.sizeId) })),
      lotIds: lots.map((lot) => lot.lotId),
      notes: notes.trim() || null,
      rowVersion: plan?.rowVersion,
    }
    const result = plan ? await update({ id: plan.id, body }) : await create(body)
    if (!('error' in result) && result.data) {
      onSaved?.(result.data)
      onClose()
    }
  }

  const fields = ['colorId', 'markerLength', 'endAllowance', 'plannedPlies', 'sizes', 'lotIds', 'notes', 'rowVersion']

  return (
    <Modal title={plan ? `Kesim föyü ${plan.planNo}` : 'Yeni kesim föyü'} onClose={onClose} variant="drawer">
      <form className="form" onSubmit={(event) => void handleSubmit(event)} noValidate>
        {(order.isLoading || summary.isLoading) && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={order.error ?? summary.error} />
        {order.data && (
          <p className="muted">
            {order.data.orderNo} · <code>{order.data.productCode}</code> {order.data.productName} · {order.data.modelCode}
          </p>
        )}

        <div className="form__grid">
          <label className="field">
            <span>Renk</span>
            <select
              value={colorId ?? ''}
              onChange={(event) => {
                setChosenColor(Number(event.target.value))
                setRatios({})
              }}
              aria-invalid={Boolean(fieldErrors(error, 'colorId')?.length)}
            >
              {colors.map((color) => <option key={color.id} value={color.id}>{color.name}</option>)}
            </select>
            {fieldErrors(error, 'colorId')?.map((message) => <small key={message} className="field__error">{message}</small>)}
          </label>
          <TextField
            label="Pastal boyu (m)"
            inputMode="decimal"
            value={markerLength}
            onChange={(event) => setMarkerLength(event.target.value)}
            errors={fieldErrors(error, 'markerLength')}
            autoFocus
          />
          <TextField
            label="Kat başı pay (m)"
            inputMode="decimal"
            value={endAllowance}
            onChange={(event) => setEndAllowance(event.target.value)}
            errors={fieldErrors(error, 'endAllowance')}
            hint="Her katın uç payı / firesi."
          />
          <TextField
            label="Kat sayısı"
            type="number"
            min={1}
            step={1}
            value={pliesTouched ? plies : String(suggested || '')}
            onChange={(event) => {
              setPliesTouched(true)
              setPlies(event.target.value)
            }}
            errors={fieldErrors(error, 'plannedPlies')}
            hint={pliesTouched ? `Önerilen: ${suggested}` : 'Kalan adetlerden önerildi.'}
          />
        </div>

        {pliesTouched && suggested > 0 && plyCount !== suggested && (
          <p>
            <button type="button" className="button button--inline" onClick={() => setPliesTouched(false)}>
              Önerilen kat sayısını kullan ({suggested})
            </button>
          </p>
        )}

        <div className="table-wrap">
          <table className="table table--compact">
            <thead>
              <tr>
                <th>Beden</th>
                <th className="table__number">Sipariş</th>
                <th className="table__number">Kesilecek kalan</th>
                <th className="table__number">Oran</th>
                <th className="table__number">Planlanan</th>
                <th>Fark</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((row) => {
                const planned = ratioOf(row.sizeId) * plyCount
                return (
                  <tr key={row.sizeId}>
                    <td>{row.sizeName}</td>
                    <td className="table__number">{formatQuantity(row.ordered)}</td>
                    <td className="table__number">{formatQuantity(row.remaining)}</td>
                    <td className="table__number">
                      <input
                        type="number"
                        min={0}
                        step={1}
                        inputMode="numeric"
                        className="matrix-input"
                        aria-label={`${row.sizeName} oranı`}
                        value={ratios[row.sizeId] ?? ''}
                        onChange={(event) => setRatios((current) => ({ ...current, [row.sizeId]: event.target.value }))}
                      />
                    </td>
                    <td className="table__number">{ratioOf(row.sizeId) > 0 ? formatQuantity(planned) : '—'}</td>
                    <td>{ratioOf(row.sizeId) > 0 ? <DifferenceCell value={planned - Math.max(row.remaining, 0)} /> : '—'}</td>
                  </tr>
                )
              })}
              {rows.length === 0 && (
                <tr><td colSpan={6} className="table__empty">Bu renkte sipariş kalemi yok.</td></tr>
              )}
            </tbody>
            <tfoot>
              <tr>
                <th scope="row" colSpan={3}>Katta parça / toplam</th>
                <td className="table__number"><strong>{piecesPerPly}</strong></td>
                <td className="table__number"><strong>{formatQuantity(piecesPerPly * plyCount)}</strong></td>
                <td />
              </tr>
            </tfoot>
          </table>
        </div>
        {fieldErrors(error, 'sizes')?.map((message) => <p key={message} className="field__error">{message}</p>)}

        <dl className="status-list">
          <div><dt>Kumaş ihtiyacı</dt><dd><strong>{formatMeters(fabricNeed)}</strong> ({formatMeters(marker + allowance)} × {plyCount} kat)</dd></div>
          {lots.length > 0 && (
            <div>
              <dt>Atanan toplarda</dt>
              <dd>
                {lotsInMeters ? formatMeters(lotTotal) : formatStockQuantity(lotTotal, lots[0].unit)}
                {lotsInMeters && lotTotal < fabricNeed && <span className="badge badge--warning"> {formatMeters(fabricNeed - lotTotal)} eksik</span>}
              </dd>
            </div>
          )}
        </dl>

        <h3 className="section-title">Kumaş topları</h3>
        <p className="muted">Bir pastalda tek ton kullanılır. Toplar serim sırasıyla listelenir; kesimde kumaş bu sırayla düşülür.</p>
        {lots.length > 0 && (
          <div className="table-wrap">
            <table className="table table--compact">
              <thead>
                <tr>
                  <th>Sıra</th>
                  <th>Top</th>
                  <th>Kumaş</th>
                  <th>Ton / parti</th>
                  <th className="table__number">Bakiye</th>
                  <th aria-label="İşlemler" />
                </tr>
              </thead>
              <tbody>
                {lots.map((lot, index) => (
                  <tr key={lot.lotId}>
                    <td>{index + 1}</td>
                    <td><code>{lot.lotNo}</code></td>
                    <td><code>{lot.stockItemCode}</code></td>
                    <td>{shadeText(lot)}</td>
                    <td className="table__number">{formatStockQuantity(lot.balance, lot.unit)}</td>
                    <td className="table__actions">
                      <button type="button" className="button button--ghost button--icon" aria-label="Yukarı" disabled={index === 0} onClick={() => moveLot(index, -1)}>
                        <ArrowUp aria-hidden="true" />
                      </button>
                      <button type="button" className="button button--ghost button--icon" aria-label="Aşağı" disabled={index === lots.length - 1} onClick={() => moveLot(index, 1)}>
                        <ArrowDown aria-hidden="true" />
                      </button>
                      <button type="button" className="button button--ghost button--icon" aria-label="Çıkar" onClick={() => setLots((current) => current.filter((item) => item.lotId !== lot.lotId))}>
                        <X aria-hidden="true" />
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
        {conflict && <p className="alert alert--error" role="alert">{conflict}</p>}
        {fieldErrors(error, 'lotIds')?.map((message) => <p key={message} className="field__error">{message}</p>)}

        <div className="toolbar" role="search">
          <input
            type="search"
            placeholder="Top no, kumaş, parti veya ton"
            aria-label="Top ara"
            value={searchInput}
            onChange={(event) => setSearchInput(event.target.value)}
            onKeyDown={(event) => {
              if (event.key === 'Enter') {
                event.preventDefault()
                setSearch(searchInput.trim())
              }
            }}
          />
          <button type="button" className="button" onClick={() => setSearch(searchInput.trim())}><Search aria-hidden="true" />Ara</button>
        </div>
        <ErrorAlert error={options.error} />
        {options.data && (
          <div className="table-wrap">
            <table className="table table--compact">
              <thead>
                <tr>
                  <th>Top</th>
                  <th>Kumaş</th>
                  <th>Ton / parti</th>
                  <th className="table__number">En (cm)</th>
                  <th className="table__number">Bakiye</th>
                  <th>Yer</th>
                  <th aria-label="İşlemler" />
                </tr>
              </thead>
              <tbody>
                {options.data.filter((option) => !lots.some((lot) => lot.lotId === option.lotId)).slice(0, 30).map((option) => (
                  <tr key={option.lotId}>
                    <td><code>{option.lotNo}</code></td>
                    <td>
                      <code>{option.stockItemCode}</code> {option.stockItemName}
                      {option.inRecipe && <span className="muted"> · reçetede</span>}
                    </td>
                    <td>{shadeText(option)}</td>
                    <td className="table__number">{option.width ?? '—'}</td>
                    <td className="table__number">{formatStockQuantity(option.balance, option.unit)}</td>
                    <td>{option.locations}</td>
                    <td className="table__actions">
                      <button type="button" className="button button--inline" onClick={() => addLot(option)}>Ekle</button>
                    </td>
                  </tr>
                ))}
                {options.data.length === 0 && (
                  <tr><td colSpan={7} className="table__empty">Stokta uygun kumaş topu bulunamadı.</td></tr>
                )}
              </tbody>
            </table>
          </div>
        )}

        <label className="field">
          <span>Not</span>
          <textarea rows={2} maxLength={500} value={notes} onChange={(event) => setNotes(event.target.value)} />
        </label>

        <ErrorAlert error={error} fields={fields} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button
            type="submit"
            className="button button--primary"
            disabled={saving || colorId === null || piecesPerPly === 0 || plyCount <= 0 || marker <= 0 || conflict !== null}
          >
            {saving ? 'Kaydediliyor…' : 'Kaydet'}
          </button>
        </footer>
      </form>
    </Modal>
  )
}
