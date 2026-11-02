import { useState } from 'react'
import type { FormEvent } from 'react'
import { fieldErrors } from '../../api/envelope'
import { ErrorAlert, TextField } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { formatQuantity } from '../../utils/format'
import { useGetLookupQuery } from '../masterData/masterDataApi'
import { formatStockQuantity } from '../stock/stockPresentation'
import { DifferenceCell } from './CuttingBadges'
import { useCutCuttingPlanMutation } from './cuttingApi'
import type { CuttingPlan } from './cuttingApi'
import { fabricFor, formatMeters, parseDecimal } from './cuttingPresentation'

/**
 * Kesimi kaydet: gerçek kat ve beden adetleri girilir (varsayılan oran × gerçek kat). Kayıtla birlikte kesilen
 * adetlerden demetler açılır ve kullanılan kumaş plandaki toplardan düşülür; işlem geri alınamaz.
 */
export function CutDialog({ plan, onClose }: { plan: CuttingPlan; onClose: () => void }) {
  const [cut, { isLoading, error }] = useCutCuttingPlanMutation()
  const warehouses = useGetLookupQuery({ resource: 'warehouses', take: 100 }, { skip: !plan.hasRecipe })
  const [plies, setPlies] = useState(String(plan.plannedPlies))
  const actualPlies = Math.max(Number(plies) || 0, 0)
  const [quantities, setQuantities] = useState<Record<number, string>>({})
  const [bundleSize, setBundleSize] = useState<string | null>(null)
  const [manualFabric, setManualFabric] = useState(false)
  const [usages, setUsages] = useState<Record<number, string>>({})
  const [warehouseId, setWarehouseId] = useState<number | null>(null)

  // Elle değiştirilmeyen beden adedi gerçek kat sayısını izler.
  const quantityOf = (sizeId: number, ratio: number) => {
    const text = quantities[sizeId]
    if (text === undefined) {
      return ratio * actualPlies
    }
    const value = Number(text)
    return Number.isInteger(value) && value >= 0 ? value : 0
  }
  const total = plan.sizes.reduce((sum, size) => sum + quantityOf(size.sizeId, size.ratio), 0)
  // Varsayılan demet boyu: bir bedenin bir oran katmanı (kat sayısı kadar parça) bir demettir.
  const bundleSizeValue = bundleSize ?? String(actualPlies || 20)
  const fabricNeed = fabricFor(plan.markerLength, plan.endAllowance, actualPlies)
  const lotsInMeters = plan.lots.length > 0 && plan.lots.every((lot) => lot.unit === 'METER')
  // Metreyle izlenmeyen kumaşta (kg vb.) pastal boyundan hesap yapılamaz: miktar toptan toba girilir.
  const enterManually = manualFabric || !lotsInMeters

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault()
    void cut({
      id: plan.id,
      body: {
        rowVersion: plan.rowVersion,
        actualPlies,
        bundleSize: Number(bundleSizeValue) || 0,
        sizes: plan.sizes.map((size) => ({ sizeId: size.sizeId, quantity: quantityOf(size.sizeId, size.ratio) })),
        lotUsages: plan.lots.length > 0 && enterManually
          ? plan.lots.map((lot) => ({ lotId: lot.lotId, quantity: parseDecimal(usages[lot.lotId] ?? '') ?? 0 }))
          : null,
        warehouseId: plan.hasRecipe ? warehouseId : null,
      },
    }).then((result) => {
      if (!('error' in result)) {
        onClose()
      }
    })
  }

  return (
    <Modal title={`Kesimi kaydet: ${plan.planNo}`} onClose={onClose} variant="drawer">
      <form className="form" onSubmit={handleSubmit} noValidate>
        <p className="muted">
          Kayıtla birlikte kesilen adetlerden demetler açılır ve kullanılan kumaş stoktan düşülür. Kesim kaydı geri alınamaz.
        </p>

        <div className="form__grid">
          <TextField
            label="Gerçek kat sayısı"
            type="number"
            min={1}
            step={1}
            value={plies}
            onChange={(event) => setPlies(event.target.value)}
            errors={fieldErrors(error, 'actualPlies')}
            hint={`Planlanan: ${plan.plannedPlies}`}
            autoFocus
          />
          <TextField
            label="Demet başına adet"
            type="number"
            min={1}
            step={1}
            value={bundleSizeValue}
            onChange={(event) => setBundleSize(event.target.value)}
            errors={fieldErrors(error, 'bundleSize')}
            hint="Her bedenin adedi bu boyda demetlere bölünür."
          />
        </div>

        <div className="table-wrap">
          <table className="table table--compact">
            <thead>
              <tr>
                <th>Beden</th>
                <th className="table__number">Oran</th>
                <th className="table__number">Kesilecek kalan</th>
                <th className="table__number">Kesilen adet</th>
                <th>Fark</th>
              </tr>
            </thead>
            <tbody>
              {plan.sizes.map((size) => {
                const quantity = quantityOf(size.sizeId, size.ratio)
                return (
                  <tr key={size.sizeId}>
                    <td>{size.sizeName}</td>
                    <td className="table__number">{size.ratio}</td>
                    <td className="table__number">{formatQuantity(size.remaining)}</td>
                    <td className="table__number">
                      <input
                        type="number"
                        min={0}
                        step={1}
                        inputMode="numeric"
                        className="matrix-input"
                        aria-label={`${size.sizeName} kesilen adet`}
                        value={quantities[size.sizeId] ?? String(size.ratio * actualPlies)}
                        onChange={(event) => setQuantities((current) => ({ ...current, [size.sizeId]: event.target.value }))}
                      />
                    </td>
                    <td><DifferenceCell value={quantity - Math.max(size.remaining, 0)} /></td>
                  </tr>
                )
              })}
            </tbody>
            <tfoot>
              <tr>
                <th scope="row" colSpan={3}>Toplam</th>
                <td className="table__number"><strong>{formatQuantity(total)}</strong></td>
                <td />
              </tr>
            </tfoot>
          </table>
        </div>
        {fieldErrors(error, 'sizes')?.map((message) => <p key={message} className="field__error">{message}</p>)}

        <h3 className="section-title">Kumaş</h3>
        {plan.lots.length === 0 ? (
          <p className="muted">
            Plana top atanmamış: kumaş, öteki demetlerde olduğu gibi reçeteye göre depodaki en eski toplardan düşülür.
          </p>
        ) : (
          <>
            <p className="muted">
              Pastal ihtiyacı {formatMeters(fabricNeed)}.{' '}
              {enterManually
                ? 'Her toptan kullanılan miktarı girin.'
                : 'Bu miktar toplara serim sırasıyla dağıtılarak düşülür.'}
            </p>
            <label className="checkbox">
              <input type="checkbox" checked={enterManually} disabled={!lotsInMeters} onChange={(event) => setManualFabric(event.target.checked)} />
              Her toptan kullanılan miktarı gir
            </label>
            {enterManually && (
              <div className="table-wrap">
                <table className="table table--compact">
                  <thead>
                    <tr>
                      <th>Top</th>
                      <th className="table__number">Bakiye</th>
                      <th className="table__number">Kullanılan</th>
                    </tr>
                  </thead>
                  <tbody>
                    {plan.lots.map((lot) => (
                      <tr key={lot.lotId}>
                        <td><code>{lot.lotNo}</code> <span className="muted">{lot.stockItemCode}</span></td>
                        <td className="table__number">{formatStockQuantity(lot.balance, lot.unit)}</td>
                        <td className="table__number">
                          <input
                            inputMode="decimal"
                            className="matrix-input"
                            aria-label={`${lot.lotNo} kullanılan miktar`}
                            value={usages[lot.lotId] ?? ''}
                            onChange={(event) => setUsages((current) => ({ ...current, [lot.lotId]: event.target.value }))}
                          />
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
            {fieldErrors(error, 'lotUsages')?.map((message) => <p key={message} className="field__error">{message}</p>)}
          </>
        )}

        {plan.hasRecipe && (
          <label className="field">
            <span>Öteki reçete malzemesinin düşüleceği depo</span>
            <select value={warehouseId ?? ''} onChange={(event) => setWarehouseId(event.target.value ? Number(event.target.value) : null)}>
              <option value="">Siparişin malzeme deposu</option>
              {warehouses.data?.map((warehouse) => (
                <option key={warehouse.id} value={warehouse.id}>{warehouse.code} — {warehouse.name}</option>
              ))}
            </select>
            <small className="field__hint">Kumaş dışındaki reçete malzemesi (aksesuar vb.) demetlerle birlikte bu depodan düşülür.</small>
            {fieldErrors(error, 'warehouseId')?.map((message) => <small key={message} className="field__error">{message}</small>)}
          </label>
        )}

        <ErrorAlert error={error} fields={['actualPlies', 'bundleSize', 'sizes', 'lotUsages', 'warehouseId']} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading || actualPlies <= 0 || total <= 0}>
            {isLoading ? 'Kaydediliyor…' : 'Kesimi kaydet'}
          </button>
        </footer>
      </form>
    </Modal>
  )
}
