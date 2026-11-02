import { Plus, RefreshCw, Trash2 } from 'lucide-react'
import { useState } from 'react'
import { ErrorAlert, TextField } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { formatMoney, formatRate } from '../../utils/money'
import { LookupSelect } from '../masterData/LookupSelect'
import type { LookupItem } from '../masterData/masterDataApi'
import { stockUnitLabels } from '../stock/stockPresentation'
import { usePreviewQuotationLineMutation } from './quotationsApi'
import type { Quotation, QuotationLine } from './quotationsApi'
import { computeLine, draftOf, formatMinutes, formatPercent, materialCostOf, requestOf } from './quotationPresentation'
import type { LineDraft, MaterialDraft } from './quotationPresentation'

const emptyDraft = (): LineDraft => ({
  productId: null,
  productLabel: null,
  description: '',
  modelReference: '',
  colors: '',
  quantity: '',
  manualMinutes: '',
  manualSubcontractCost: '',
  firePercent: '',
  overheadPercent: '',
  marginPercent: '',
  unitPrice: '',
  materials: [],
  routeMinutes: null,
  routeSubcontractCost: 0,
  warnings: [],
})

/**
 * Teklif satırı ve parça başı maliyeti. Değerler değiştikçe döküm anında yeniden hesaplanır ("ya olursa"); kaynak
 * değerler (reçete, stok maliyeti, rota dakikası, fason fiyatı) "Yeniden hesapla" ile sunucudan güncellenir. Boş
 * bırakılan alanlarda kaynak değer kullanılır.
 */
export function QuotationLineDialog({
  quotation, line, onApply, onClose, saving, error,
}: {
  quotation: Quotation
  line?: QuotationLine
  onApply: (draft: LineDraft) => void
  onClose: () => void
  saving: boolean
  error: unknown
}) {
  const [draft, setDraft] = useState<LineDraft>(() => (line ? draftOf(line) : emptyDraft()))
  const [preview, previewState] = usePreviewQuotationLineMutation()
  const [materialToAdd, setMaterialToAdd] = useState<LookupItem | null>(null)
  const [freeMaterial, setFreeMaterial] = useState('')

  const update = (patch: Partial<LineDraft>) => setDraft((current) => ({ ...current, ...patch }))
  const updateMaterial = (key: string, patch: Partial<MaterialDraft>) =>
    setDraft((current) => ({
      ...current,
      materials: (current.materials ?? []).map((material) => (material.key === key ? { ...material, ...patch } : material)),
    }))

  /** Sunucuda hesaplatır; kaynak değerler (ve reçeteden yükleme istendiyse malzemeler) güncellenir, girilenler korunur. */
  const recompute = async (base: LineDraft, reloadMaterials: boolean) => {
    const request = requestOf(base)
    const result = await preview({
      currency: quotation.currency,
      date: quotation.quotationDate,
      line: { ...request, quantity: request.quantity > 0 ? request.quantity : 1, materials: reloadMaterials ? null : request.materials },
    })
    if ('data' in result && result.data) {
      const fresh = draftOf(result.data)
      setDraft({
        ...base,
        materials: fresh.materials,
        routeMinutes: fresh.routeMinutes,
        routeSubcontractCost: fresh.routeSubcontractCost,
        warnings: fresh.warnings,
        firePercent: base.firePercent || fresh.firePercent,
        overheadPercent: base.overheadPercent || fresh.overheadPercent,
        marginPercent: base.marginPercent || fresh.marginPercent,
      })
    }
  }

  const chooseProduct = (item: LookupItem | null) => {
    const next = { ...draft, productId: item?.id ?? null, productLabel: item ? `${item.code} — ${item.name}` : null }
    if (item) {
      // Ürün seçilince reçete, rota ve fason fiyatları yüklenir.
      void recompute(next, true)
    } else {
      setDraft({ ...next, routeMinutes: null, routeSubcontractCost: 0 })
    }
  }

  const addMaterial = () => {
    if (!materialToAdd && !freeMaterial.trim()) {
      return
    }
    const material: MaterialDraft = {
      key: `new-${Date.now()}`,
      stockItemId: materialToAdd?.id ?? null,
      label: materialToAdd ? `${materialToAdd.code} ${materialToAdd.name}` : freeMaterial.trim(),
      unit: null,
      quantityPerPiece: '1',
      sourceUnitCost: 0,
      unitCost: '',
    }
    const next = { ...draft, materials: [...(draft.materials ?? []), material] }
    setMaterialToAdd(null)
    setFreeMaterial('')
    if (material.stockItemId !== null) {
      // Stok kartının maliyeti sunucudan okunur.
      void recompute(next, false)
    } else {
      setDraft(next)
    }
  }

  const materials = draft.materials ?? []
  const materialCost = Math.round(materials.reduce((sum, material) => sum + materialCostOf(material), 0) * 10_000) / 10_000
  const cost = computeLine(draft, quotation.laborCostPerMinute, quotation.energyCostPerMinute, materialCost)
  const currency = quotation.currency
  const quantity = Number(draft.quantity) || 0

  return (
    <Modal title={line ? `Satır ${line.lineNo}: maliyet ve fiyat` : 'Yeni teklif satırı'} onClose={onClose} variant="drawer">
      <div className="form">
        <fieldset className="form__section">
          <legend>Ürün</legend>
          <div className="form__grid">
            <LookupSelect
              resource="products"
              label="Ürün (varsa)"
              value={draft.productId}
              onChange={() => undefined}
              onSelectItem={chooseProduct}
              customerId={quotation.customerId}
              placeholder="— Ürün kartı yok —"
            />
            <TextField label="Açıklama" value={draft.description} maxLength={200} onChange={(event) => update({ description: event.target.value })} hint={draft.productId ? 'Boşsa ürün adı basılır.' : 'Ürün kartı yoksa zorunlu.'} />
            <TextField label="Model / artikel referansı" value={draft.modelReference} maxLength={100} onChange={(event) => update({ modelReference: event.target.value })} />
            <TextField label="Renk(ler)" value={draft.colors} maxLength={200} onChange={(event) => update({ colors: event.target.value })} placeholder="örn. Siyah, Lacivert" />
            <TextField label="Adet" type="number" min={1} step={1} value={draft.quantity} onChange={(event) => update({ quantity: event.target.value })} />
          </div>
        </fieldset>

        <fieldset className="form__section">
          <legend>Malzeme (parça başı, fire dahil)</legend>
          <div className="table-wrap">
            <table className="table table--compact">
              <thead>
                <tr>
                  <th>Malzeme</th>
                  <th className="table__number">Miktar</th>
                  <th>Birim</th>
                  <th className="table__number">Birim maliyet ({currency})</th>
                  <th className="table__number">Maliyet</th>
                  <th aria-label="İşlemler" />
                </tr>
              </thead>
              <tbody>
                {materials.map((material) => (
                  <tr key={material.key}>
                    <td>{material.label}</td>
                    <td>
                      <input inputMode="decimal" className="matrix-input" aria-label={`${material.label} miktar`} value={material.quantityPerPiece} onChange={(event) => updateMaterial(material.key, { quantityPerPiece: event.target.value })} />
                    </td>
                    <td>{material.unit ? stockUnitLabels[material.unit] : '—'}</td>
                    <td>
                      <input
                        inputMode="decimal"
                        className="matrix-input"
                        aria-label={`${material.label} birim maliyet`}
                        placeholder={formatRate(material.sourceUnitCost, '').trim()}
                        value={material.unitCost}
                        onChange={(event) => updateMaterial(material.key, { unitCost: event.target.value })}
                      />
                    </td>
                    <td className="table__number">{formatRate(materialCostOf(material), '')}</td>
                    <td className="table__actions">
                      <button
                        type="button"
                        className="button button--ghost button--icon"
                        aria-label={`${material.label} kaldır`}
                        onClick={() => update({ materials: materials.filter((other) => other.key !== material.key) })}
                      >
                        <Trash2 aria-hidden="true" />
                      </button>
                    </td>
                  </tr>
                ))}
                {materials.length === 0 && (
                  <tr><td colSpan={6} className="table__empty">Malzeme yok.</td></tr>
                )}
              </tbody>
            </table>
          </div>
          <div className="matrix-adder">
            <LookupSelect resource="stock-items" label="Stok kartından ekle" value={materialToAdd?.id ?? null} onChange={() => undefined} onSelectItem={setMaterialToAdd} />
            <TextField label="ya da açıklama" value={freeMaterial} maxLength={200} onChange={(event) => setFreeMaterial(event.target.value)} />
            <button type="button" className="button" onClick={addMaterial} disabled={!materialToAdd && !freeMaterial.trim()}>
              <Plus aria-hidden="true" />
              Ekle
            </button>
          </div>
          <small className="field__hint">Birim maliyet boş bırakılırsa stok kartının maliyeti teklif para birimine çevrilerek kullanılır.</small>
        </fieldset>

        <fieldset className="form__section">
          <legend>İşçilik, fason ve yüzdeler</legend>
          <div className="form__grid">
            <TextField
              label="Dakika (SAM)"
              inputMode="decimal"
              value={draft.manualMinutes}
              placeholder={draft.routeMinutes === null ? 'rota yok' : `rota: ${formatMinutes(draft.routeMinutes)}`}
              onChange={(event) => update({ manualMinutes: event.target.value })}
              hint="Boşsa etkin rotanın fason olmayan adımlarının toplamı."
            />
            <TextField
              label={`Fason maliyeti (${currency})`}
              inputMode="decimal"
              value={draft.manualSubcontractCost}
              placeholder={`rota: ${formatRate(draft.routeSubcontractCost, '').trim()}`}
              onChange={(event) => update({ manualSubcontractCost: event.target.value })}
              hint="Boşsa rotadaki fason adımların fason fiyatları."
            />
            <TextField label="Fire (%)" inputMode="decimal" value={draft.firePercent} onChange={(event) => update({ firePercent: event.target.value })} />
            <TextField label="Genel gider (%)" inputMode="decimal" value={draft.overheadPercent} onChange={(event) => update({ overheadPercent: event.target.value })} />
            <TextField
              label="Kâr marjı (%)"
              inputMode="decimal"
              value={draft.unitPrice.trim() === '' ? draft.marginPercent : String(cost.marginPercent).replace('.', ',')}
              disabled={draft.unitPrice.trim() !== ''}
              onChange={(event) => update({ marginPercent: event.target.value })}
              hint={draft.unitPrice.trim() !== '' ? 'Fiyat elle girildi; marj fiyattan hesaplanır.' : undefined}
            />
            <TextField
              label={`Birim fiyat (${currency})`}
              inputMode="decimal"
              value={draft.unitPrice}
              placeholder={formatRate(cost.unitPrice, '').trim()}
              onChange={(event) => update({ unitPrice: event.target.value })}
              hint="Boşsa maliyet × (1 + marj)."
            />
          </div>
        </fieldset>

        <h3 className="section-title">Parça başı döküm</h3>
        <div className="table-wrap">
          <table className="table table--compact">
            <tbody>
              <tr><th scope="row">Malzeme</th><td className="table__number">{formatRate(cost.materialCost, currency)}</td></tr>
              <tr>
                <th scope="row">İşçilik ({formatMinutes(cost.laborMinutes)} × {formatRate(quotation.laborCostPerMinute, currency)})</th>
                <td className="table__number">{formatRate(cost.laborCost, currency)}</td>
              </tr>
              {quotation.energyCostPerMinute > 0 && (
                <tr><th scope="row">Enerji</th><td className="table__number">{formatRate(cost.energyCost, currency)}</td></tr>
              )}
              <tr><th scope="row">Fason</th><td className="table__number">{formatRate(cost.subcontractCost, currency)}</td></tr>
              <tr><th scope="row">Doğrudan maliyet</th><td className="table__number">{formatRate(cost.directCost, currency)}</td></tr>
              <tr><th scope="row">Fire</th><td className="table__number">{formatRate(cost.fireCost, currency)}</td></tr>
              <tr><th scope="row">Genel gider</th><td className="table__number">{formatRate(cost.overheadCost, currency)}</td></tr>
              <tr><th scope="row"><strong>Birim maliyet</strong></th><td className="table__number"><strong>{formatRate(cost.unitCost, currency)}</strong></td></tr>
              <tr><th scope="row">Kâr marjı</th><td className="table__number">{formatPercent(cost.marginPercent)}</td></tr>
              <tr><th scope="row"><strong>Birim fiyat</strong></th><td className="table__number"><strong>{formatRate(cost.unitPrice, currency)}</strong></td></tr>
              <tr><th scope="row">Satır tutarı ({quantity} adet)</th><td className="table__number">{formatMoney(cost.unitPrice * quantity, currency)}</td></tr>
            </tbody>
          </table>
        </div>

        {draft.warnings.length > 0 && (
          <div className="alert alert--info" role="status">
            <ul>{draft.warnings.map((warning) => <li key={warning}>{warning}</li>)}</ul>
          </div>
        )}
        <ErrorAlert error={previewState.error ?? error} />
        <footer className="form__actions">
          <button type="button" className="button form__actions-start" disabled={previewState.isLoading} onClick={() => void recompute(draft, false)}>
            <RefreshCw aria-hidden="true" />
            Yeniden hesapla
          </button>
          {draft.productId !== null && (
            <button type="button" className="button" disabled={previewState.isLoading} onClick={() => void recompute(draft, true)}>
              Reçeteden yükle
            </button>
          )}
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="button" className="button button--primary" disabled={saving} onClick={() => onApply(draft)}>
            {saving ? 'Kaydediliyor…' : 'Satırı kaydet'}
          </button>
        </footer>
      </div>
    </Modal>
  )
}
