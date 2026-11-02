import { Plus, Printer } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useParams } from 'react-router-dom'
import { fieldErrors } from '../../api/envelope'
import { ErrorAlert, TextField } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDateTime } from '../../utils/format'
import { LookupSelect } from '../masterData/LookupSelect'
import { useGetLocationOptionsQuery, useGetStockLotsQuery } from './stockApi'
import {
  stockCountStatusPresentation,
  useAddStockCountLineMutation,
  useCancelStockCountMutation,
  useGetStockCountQuery,
  usePostStockCountMutation,
  useSaveStockCountLinesMutation,
} from './stockCountsApi'
import type { StockCount } from './stockCountsApi'
import { formatSignedQuantity, formatStockQuantity, locationLabel, parseQuantity, quantityStep, stockUnitLabels } from './stockPresentation'

/** Fişte olmayan, sayımda bulunan stok için satır ekler. Takipli kartta bulunan top seçilir. */
function AddLineDialog({ count, onClose }: { count: StockCount; onClose: () => void }) {
  const [addLine, { isLoading, error }] = useAddStockCountLineMutation()
  const [stockItemId, setStockItemId] = useState<number | null>(null)
  const [locationId, setLocationId] = useState<number | null>(count.locationId)
  const [lotId, setLotId] = useState<number | null>(null)
  const [quantity, setQuantity] = useState('')
  const locations = useGetLocationOptionsQuery(count.warehouseId, { skip: count.locationId !== null })
  // Kartın topları (tükenmişler dahil); takipsiz kartta liste boştur.
  const lots = useGetStockLotsQuery(
    { page: 1, pageSize: 100, stockItemId: stockItemId ?? 0, includeZero: true },
    { skip: stockItemId === null },
  )
  const lotOptions = [...new Map((lots.data?.items ?? []).map((lot) => [lot.lotId, lot])).values()]
  const amount = parseQuantity(quantity)

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    if (stockItemId === null || amount === null) {
      return
    }

    const result = await addLine({ id: count.id, rowVersion: count.rowVersion, stockItemId, countedQuantity: amount, locationId, lotId })
    if (!('error' in result)) {
      onClose()
    }
  }

  return (
    <Modal title="Sayımda bulunan stok" onClose={onClose}>
      <form className="form" onSubmit={(event) => void handleSubmit(event)} noValidate>
        <LookupSelect
          resource="stock-items"
          label="Stok kartı"
          value={stockItemId}
          onChange={(id) => {
            setStockItemId(id)
            setLotId(null)
          }}
          required
          errors={fieldErrors(error, 'stockItemId')}
        />
        {count.locationId === null && (
          <label className="field">
            <span>Adres</span>
            <select value={locationId ?? ''} onChange={(event) => setLocationId(event.target.value ? Number(event.target.value) : null)}>
              <option value="">Genel adres</option>
              {locations.data?.filter((location) => !location.isDefault).map((location) => (
                <option key={location.id} value={location.id}>{locationLabel(location)}</option>
              ))}
            </select>
          </label>
        )}
        {lotOptions.length > 0 && (
          <label className="field">
            <span>Top</span>
            <select
              value={lotId ?? ''}
              onChange={(event) => setLotId(event.target.value ? Number(event.target.value) : null)}
              aria-invalid={Boolean(fieldErrors(error, 'lotId')?.length)}
            >
              <option value="">Seçin…</option>
              {lotOptions.map((lot) => (
                <option key={lot.lotId} value={lot.lotId}>{lot.lotNo}{lot.batchNo ? ` — parti ${lot.batchNo}` : ''}</option>
              ))}
            </select>
            {fieldErrors(error, 'lotId')?.map((message) => (
              <small key={message} className="field__error">{message}</small>
            ))}
          </label>
        )}
        <TextField
          label="Sayılan miktar"
          type="number"
          min={0}
          step="0.001"
          value={quantity}
          onChange={(event) => setQuantity(event.target.value)}
          errors={fieldErrors(error, 'countedQuantity')}
          required
        />

        <ErrorAlert error={error} fields={['stockItemId', 'lotId', 'countedQuantity']} />

        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading || stockItemId === null || amount === null}>
            {isLoading ? 'Ekleniyor…' : 'Satır ekle'}
          </button>
        </footer>
      </form>
    </Modal>
  )
}

/** Fişin satırları ve işlemleri. Sunucudaki fiş değişince (key) girdiler kayıtlı değerlerden yeniden başlar. */
function CountSheet({ count }: { count: StockCount }) {
  const canManage = usePermission(Permissions.StockManage)
  const editable = canManage && count.isOpen
  // Sayımı onaylamak stok farkını deftere yazar; stok düzeltme yetkisi ister.
  const canAdjust = usePermission(Permissions.StockAdjust)
  const [saveLines, saveState] = useSaveStockCountLinesMutation()
  const [postCount, postState] = usePostStockCountMutation()
  const [cancelCount, cancelState] = useCancelStockCountMutation()
  const busy = saveState.isLoading || postState.isLoading || cancelState.isLoading

  const [entries, setEntries] = useState<Record<number, string>>(() =>
    Object.fromEntries(count.lines.map((line) => [line.id, line.countedQuantity === null ? '' : String(line.countedQuantity)])),
  )
  const [blind, setBlind] = useState(false)
  const [dialog, setDialog] = useState<'add' | 'post' | 'cancel' | null>(null)

  const parsed = count.lines.map((line) => {
    const text = (entries[line.id] ?? '').trim()
    return { line, text, value: text === '' ? null : parseQuantity(text) }
  })
  const invalid = parsed.some((entry) => entry.text !== '' && entry.value === null)
  const dirty = parsed.some((entry) => entry.value !== entry.line.countedQuantity)
  const countedLines = parsed.filter((entry) => entry.value !== null)
  const differences = countedLines.filter((entry) => entry.value !== entry.line.expectedQuantity)
  const status = stockCountStatusPresentation[count.status]

  const handleSave = async (event: FormEvent) => {
    event.preventDefault()
    if (!invalid) {
      await saveLines({
        id: count.id,
        rowVersion: count.rowVersion,
        lines: parsed.map((entry) => ({ lineId: entry.line.id, countedQuantity: entry.value })),
      })
    }
  }

  const confirm = async (action: 'post' | 'cancel') => {
    const run = action === 'post' ? postCount : cancelCount
    const result = await run({ id: count.id, rowVersion: count.rowVersion })
    if (!('error' in result)) {
      setDialog(null)
    }
  }

  return (
    <>
      <header className="page__header">
        <div>
          <h1>Sayım {count.countNo} <span className={`badge badge--${status.tone}`}>{status.label}</span></h1>
          <p className="muted">
            {count.warehouseCode} — {count.warehouseName} · {count.locationCode ? `yalnızca ${count.locationCode} adresi` : 'deponun tamamı'}
            {' · '}açan {count.createdBy ?? '—'}, {formatDateTime(count.createdAt)}
            {count.postedAt && <> · onaylayan {count.postedBy}, {formatDateTime(count.postedAt)}</>}
            {count.note && <> · {count.note}</>}
          </p>
        </div>
        <div className="page__actions">
          <label className="checkbox">
            <input type="checkbox" checked={blind} onChange={(event) => setBlind(event.target.checked)} />
            Beklenen miktarı gizle
          </label>
          <button type="button" className="button" onClick={() => window.print()}>
            <Printer aria-hidden="true" />
            Yazdır
          </button>
          {editable && (
            <>
              <button type="button" className="button" disabled={busy || dirty} onClick={() => setDialog('add')} title={dirty ? 'Önce girdiğiniz miktarları kaydedin.' : undefined}>
                <Plus aria-hidden="true" />
                Satır ekle
              </button>
              <button type="submit" form="stock-count-form" className="button" disabled={busy || invalid || !dirty}>
                {saveState.isLoading ? 'Kaydediliyor…' : 'Kaydet'}
              </button>
              {canAdjust && (
                <button
                  type="button"
                  className="button button--primary"
                  disabled={busy || dirty || countedLines.length === 0}
                  title={dirty ? 'Önce girdiğiniz miktarları kaydedin.' : undefined}
                  onClick={() => setDialog('post')}
                >
                  Onayla
                </button>
              )}
              <button type="button" className="button button--danger-ghost" disabled={busy} onClick={() => setDialog('cancel')}>
                İptal et
              </button>
            </>
          )}
        </div>
      </header>

      {count.isOpen && (
        <p className="alert alert--info no-print">
          Sayım sürüyor: kapsamdaki adreslerde stok hareketi yapılamaz. Sayılan miktarları girip kaydedin; onayladığınızda
          farklar stoğa işlenir. Miktarı boş bırakılan satır sayılmamış kabul edilir ve stoğu değiştirmez.
        </p>
      )}
      {invalid && <p className="alert alert--error" role="alert">Miktarlar sıfır ya da pozitif sayılar olmalıdır.</p>}
      <ErrorAlert error={saveState.error ?? postState.error ?? cancelState.error} />

      <form id="stock-count-form" onSubmit={(event) => void handleSave(event)} noValidate>
      <div className="table-wrap">
        <table className="table table--compact">
          <thead>
            <tr>
              <th>Adres</th>
              <th>Stok kartı</th>
              <th>Top</th>
              {!blind && <th className="table__number">Beklenen</th>}
              <th className="table__number">Sayılan</th>
              {!blind && <th className="table__number">Fark</th>}
            </tr>
          </thead>
          <tbody>
            {parsed.map(({ line, value }) => {
              const difference = value === null ? null : Math.round((value - line.expectedQuantity) * 1000) / 1000
              return (
                <tr key={line.id}>
                  <td><code>{line.locationCode}</code></td>
                  <td><code>{line.itemCode}</code> {line.itemName}</td>
                  <td>
                    {line.lotNo ? <code>{line.lotNo}</code> : '—'}
                    {line.batchNo && <span className="muted"> · {line.batchNo}</span>}
                  </td>
                  {!blind && <td className="table__number">{formatStockQuantity(line.expectedQuantity, line.unit)}</td>}
                  <td className="table__number">
                    {editable ? (
                      <input
                        type="number"
                        min={0}
                        step={quantityStep(line.unit)}
                        className="matrix-input"
                        aria-label={`${line.itemCode} ${line.locationCode} sayılan miktar (${stockUnitLabels[line.unit]})`}
                        value={entries[line.id] ?? ''}
                        onChange={(event) => setEntries((current) => ({ ...current, [line.id]: event.target.value }))}
                      />
                    ) : line.countedQuantity === null ? (
                      <span className="muted">Sayılmadı</span>
                    ) : (
                      formatStockQuantity(line.countedQuantity, line.unit)
                    )}
                  </td>
                  {!blind && (
                    <td className="table__number">
                      {difference === null ? '—' : difference === 0 ? (
                        <span className="muted">Fark yok</span>
                      ) : (
                        <strong>{formatSignedQuantity(difference, line.unit)}</strong>
                      )}
                    </td>
                  )}
                </tr>
              )
            })}
            {count.lines.length === 0 && (
              <tr>
                <td colSpan={blind ? 4 : 6} className="table__empty">
                  Kapsamda kayıtlı stok yok. Sayımda bulunan stok için satır ekleyin.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
      </form>
      <p className="muted">
        {count.lines.length} satır · {countedLines.length} sayıldı · {differences.length} satırda fark var
      </p>

      {dialog === 'add' && <AddLineDialog count={count} onClose={() => setDialog(null)} />}
      {(dialog === 'post' || dialog === 'cancel') && (
        <Modal title={dialog === 'post' ? 'Sayımı onayla' : 'Sayımı iptal et'} onClose={() => setDialog(null)}>
          <div className="form">
            <p>
              {dialog === 'post'
                ? `${countedLines.length} satır sayıldı; ${differences.length} satırdaki fark stok düzeltmesi olarak işlenecek. Sayılmayan ${count.lines.length - countedLines.length} satır değişmeyecek. Bu işlem geri alınamaz.`
                : 'Fiş iptal edilecek: girilen miktarlar stoğa işlenmez ve adreslerdeki kilit kalkar.'}
            </p>
            <ErrorAlert error={postState.error ?? cancelState.error} />
            <footer className="form__actions">
              <button type="button" className="button" onClick={() => setDialog(null)}>Vazgeç</button>
              <button
                type="button"
                className={dialog === 'post' ? 'button button--primary' : 'button button--danger'}
                disabled={busy}
                onClick={() => void confirm(dialog)}
              >
                {dialog === 'post' ? 'Onayla ve stoğu düzelt' : 'Sayımı iptal et'}
              </button>
            </footer>
          </div>
        </Modal>
      )}
    </>
  )
}

/** Sayım fişi: sayılan miktarlar girilir, kaydedilir ve onaylanır. Fiş yazdırılıp depoda doldurulabilir. */
export function StockCountDetailPage() {
  const id = Number(useParams().id)
  const { data, error, isLoading } = useGetStockCountQuery(id, { skip: !Number.isFinite(id) })

  return (
    <section className="page">
      <p className="no-print"><Link to="/stock-counts">← Sayımlara dön</Link></p>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {/* key: fiş sunucuda değişince (kayıt, satır ekleme) girdiler kayıtlı değerlerle yeniden kurulur. */}
      {data && <CountSheet key={data.rowVersion} count={data} />}
    </section>
  )
}
