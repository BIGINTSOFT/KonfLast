import { X } from 'lucide-react'
import { useEffect, useState } from 'react'
import { ErrorAlert } from '../../components/FormControls'
import { formatMoney, parseDecimal } from '../../utils/money'
import { useGetLookupQuery } from '../masterData/masterDataApi'
import type { FormExtraProps } from '../masterData/resources'
import type { StockUnit } from '../stock/stockApi'
import { stockUnitLabels } from '../stock/stockPresentation'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import {
  useGetMaterialOptionsQuery, useGetProductCostSummaryQuery, useGetProductMaterialsQuery, useSaveProductMaterialsMutation,
} from './costingApi'
import type { ProductMaterial } from './costingApi'

interface LineDraft {
  key: number
  stockItemId: number | null
  colorId: number | null
  sizeId: number | null
  quantityPerPiece: string
  wastePercent: string
}

interface MaterialOption {
  id: number
  code: string
  name: string
  unit: StockUnit
  unitCost: number | null
}

type Tab = 'general' | 'fabric' | 'trade' | 'recipe' | 'cost'

const tabLabels: Record<Tab, string> = {
  general: 'Genel',
  fabric: 'Kumaş ve etiket',
  trade: 'Ticari',
  recipe: 'Reçete',
  cost: 'Maliyet',
}

/** Standart alanların bulunduğu sekme; sunucu bir alanı reddedince o sekme açılır. */
const fieldTabs: Record<string, Tab> = {
  composition: 'fabric',
  fabricWeight: 'fabric',
  originCountryCode: 'fabric',
  careInstructions: 'fabric',
  gtipCode: 'trade',
  vatRate: 'trade',
  salesUnit: 'trade',
  listPrice: 'trade',
  currency: 'trade',
}

const formatSam = (minutes: number) => `${minutes.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 4 })} dk`

/** Etkin rota ve reçeteden hesaplanan parça başı standart maliyet (salt okunur). */
function CostSummary({ productId }: { productId: number }) {
  const { data, isLoading, error } = useGetProductCostSummaryQuery(productId)
  if (isLoading) {
    return <p role="status">Yükleniyor…</p>
  }
  if (!data) {
    return <ErrorAlert error={error} />
  }

  const sameCurrency = data.listPrice !== null && data.totalCost !== null && data.listPriceCurrency === data.currency
  const margin = sameCurrency && data.listPrice! > 0 ? ((data.listPrice! - data.totalCost!) / data.listPrice!) * 100 : null

  return (
    <>
      <p className="muted form__note">
        Parça başı standart maliyet; modelin etkin rotasından ve reçetenin genel satırlarından hesaplanır. Rota, reçete,
        malzeme maliyeti ya da ücret ayarı değişince güncellenir.
      </p>
      <dl className="status-list cost-list">
        <div>
          <dt>Etkin rota</dt>
          <dd>{data.routeVersion === null ? <span className="muted">Yok</span> : `v${data.routeVersion} · ${data.operationCount} adım`}</dd>
        </div>
        <div>
          <dt>Toplam standart süre (SAM)</dt>
          <dd>{data.routeVersion === null ? '—' : formatSam(data.totalSam)}</dd>
        </div>
        <div>
          <dt>İşçilik</dt>
          <dd>{data.laborCost === null ? '—' : formatMoney(data.laborCost, data.currency)}</dd>
        </div>
        <div>
          <dt>Malzeme ({data.materialLineCount} reçete satırı)</dt>
          <dd>{data.materialCost === null ? '—' : formatMoney(data.materialCost, data.currency)}</dd>
        </div>
        <div className="cost-list__total">
          <dt>Parça başı standart maliyet</dt>
          <dd>{data.totalCost === null ? '—' : formatMoney(data.totalCost, data.currency)}</dd>
        </div>
        <div>
          <dt>Liste fiyatı</dt>
          <dd>{data.listPrice === null ? '—' : formatMoney(data.listPrice, data.listPriceCurrency)}</dd>
        </div>
        {margin !== null && (
          <div>
            <dt>Brüt kâr marjı</dt>
            <dd>
              <span className={`badge badge--${margin >= 0 ? 'ok' : 'danger'}`}>
                %{margin.toLocaleString('tr-TR', { maximumFractionDigits: 1 })}
              </span>
            </dd>
          </div>
        )}
      </dl>
      {data.routeVersion === null && (
        <div className="alert alert--info">Modelin etkin operasyon rotası yok; işçilik maliyeti hesaplanamadı.</div>
      )}
      {data.laborUnpriced && (
        <div className="alert alert--info">Ücreti tanımlı olmayan operasyonlar var; işçilik eksik hesaplandı (Ücret ve Prim ayarları).</div>
      )}
      {data.materialUncosted && (
        <div className="alert alert--info">Birim maliyeti olmayan malzemeler var; malzeme maliyeti eksik hesaplandı.</div>
      )}
      {data.hasVariantMaterials && (
        <div className="alert alert--info">
          Renk ya da bedene özel reçete satırları bu özete dahil değildir: o renk-bedenlerde aynı malzemenin genel satırı yerine
          özel satır geçerlidir ve maliyetleri farklıdır (sipariş maliyetinde renk-beden bazında hesaplanır).
        </div>
      )}
    </>
  )
}

const toDraft = (line: ProductMaterial, key: number): LineDraft => ({
  key,
  stockItemId: line.stockItemId,
  colorId: line.colorId,
  sizeId: line.sizeId,
  quantityPerPiece: String(line.quantityPerPiece).replace('.', ','),
  wastePercent: String(line.wastePercent).replace('.', ','),
})

/**
 * Ürün formu: genel bilgiler ve reçete (bir parça için gereken malzemeler). Reçete ürün kaydının alanı değildir;
 * ürün kaydedildikten sonra (yeni kayıtta Id oluşunca) kendi ucuna yazılır.
 */
export function ProductFormExtra({ row, fields, activeControl, disabled, errorFields, onAfterSaveChange }: FormExtraProps) {
  const [tab, setTab] = useState<Tab>('general')
  const canSeeCost = usePermission(Permissions.CostingView)
  // Standart alanlardan biri reddedilince o alanın sekmesi açılır.
  const errorKey = errorFields.join(',')
  const [seenErrors, setSeenErrors] = useState(errorKey)
  if (errorKey !== seenErrors) {
    setSeenErrors(errorKey)
    if (errorFields.length > 0) {
      setTab(errorFields.some((name) => !(name in fieldTabs)) ? 'general' : fieldTabs[errorFields[0]])
    }
  }

  const productId = row?.id
  const current = useGetProductMaterialsQuery(productId ?? 0, { skip: productId === undefined })
  // Birim ve birim maliyet seçilen malzemeyle birlikte gösterilir; mamul kartları reçeteye girmez.
  // Stok ekranlarını görme yetkisi olmayan kullanıcı da reçete tanımlayabilsin diye liste reçete ucundan gelir.
  const stockItems = useGetMaterialOptionsQuery()
  const colors = useGetLookupQuery({ resource: 'colors', take: 100 })
  const sizes = useGetLookupQuery({ resource: 'sizes', take: 100 })
  const [save] = useSaveProductMaterialsMutation()
  // Dokunulmadıkça sunucudaki reçete geçerlidir (null = değiştirilmedi).
  const [draft, setDraft] = useState<LineDraft[] | null>(null)
  const [nextKey, setNextKey] = useState(1000)

  const stored = current.data?.lines ?? []
  const lines = draft ?? stored.map(toDraft)
  const currency = current.data?.currency ?? 'TRY'

  useEffect(() => {
    onAfterSaveChange(async (id) => {
      if (draft === null) {
        return undefined
      }
      const result = await save({
        productId: id,
        // Malzemesi seçilmemiş satır gönderilmez; eksik / geçersiz sayıyı sunucu reddeder.
        lines: draft
          .filter((line) => line.stockItemId !== null)
          .map((line) => ({
            stockItemId: line.stockItemId!,
            colorId: line.colorId,
            sizeId: line.sizeId,
            quantityPerPiece: parseDecimal(line.quantityPerPiece) ?? 0,
            wastePercent: line.wastePercent.trim() === '' ? 0 : (parseDecimal(line.wastePercent) ?? -1),
          })),
      })
      if ('error' in result) {
        setTab('recipe')
        return result.error
      }
      return undefined
    })

    return () => {
      onAfterSaveChange(null)
    }
  })

  // Reçetedeki pasif / liste sınırı dışındaki malzemeler de seçeneklerde kalır.
  const options: MaterialOption[] = [
    ...(stockItems.data ?? [])
      .filter((item) => item.isActive || stored.some((line) => line.stockItemId === item.id))
      .map((item) => ({ id: item.id, code: item.code, name: item.name, unit: item.unit, unitCost: item.unitCost })),
    ...stored
      .filter((line) => !(stockItems.data ?? []).some((item) => item.id === line.stockItemId))
      .map((line) => ({ id: line.stockItemId, code: line.itemCode, name: line.itemName, unit: line.unit, unitCost: line.unitCost })),
  ]
  const optionOf = (id: number | null) => options.find((option) => option.id === id)
  const idOf = (value: string) => (value === '' ? null : Number(value))

  const costOf = (line: LineDraft) => {
    const option = optionOf(line.stockItemId)
    const quantity = parseDecimal(line.quantityPerPiece)
    const waste = line.wastePercent.trim() === '' ? 0 : parseDecimal(line.wastePercent)
    if (!option || option.unitCost === null || quantity === null || waste === null) {
      return null
    }
    return quantity * (1 + waste / 100) * option.unitCost
  }
  const costs = lines.map(costOf)
  const totalCost = costs.reduce<number>((sum, cost) => sum + (cost ?? 0), 0)
  const hasUnpriced = lines.some((line, index) => line.stockItemId !== null && costs[index] === null)

  const change = (key: number, patch: Partial<LineDraft>) =>
    setDraft(lines.map((line) => (line.key === key ? { ...line, ...patch } : line)))

  const add = () => {
    setDraft([...lines, { key: nextKey, stockItemId: null, colorId: null, sizeId: null, quantityPerPiece: '', wastePercent: '0' }])
    setNextKey(nextKey + 1)
  }

  return (
    <div className="product-form">
      <div className="tabs" role="tablist" aria-label="Ürün bilgileri">
        {(Object.keys(tabLabels) as Tab[])
          // Maliyet özeti kaydedilmiş ürün ve maliyet görme yetkisi ister.
          .filter((key) => key !== 'cost' || (canSeeCost && productId !== undefined))
          .map((key) => (
          <button
            key={key}
            type="button"
            role="tab"
            id={`product-tab-${key}`}
            aria-selected={tab === key}
            aria-controls={`product-panel-${key}`}
            className={`tabs__tab${tab === key ? ' tabs__tab--active' : ''}`}
            onClick={() => setTab(key)}
          >
            {tabLabels[key]}
            {key === 'recipe' && lines.length > 0 && <span className="tabs__count">{lines.length}</span>}
          </button>
        ))}
      </div>

      {/* Sekmeler gizlenir, kaldırılmaz: diğer sekmedeki girişler korunur. */}
      <div className="tabs__panel" role="tabpanel" id="product-panel-general" aria-labelledby="product-tab-general" hidden={tab !== 'general'}>
        <div className="form__pair">
          {fields.productCode}
          {fields.modelId}
        </div>
        {fields.name}
        {fields.description}
        <fieldset className="form__section">
          <legend>Sınıflandırma</legend>
          <div className="form__pair">
            {fields.productGroupId}
            {fields.targetGroup}
          </div>
          <div className="form__pair">
            {fields.season}
            {fields.customerArticleCode}
          </div>
        </fieldset>
        {activeControl}
      </div>

      <div className="tabs__panel" role="tabpanel" id="product-panel-fabric" aria-labelledby="product-tab-fabric" hidden={tab !== 'fabric'}>
        <p className="muted form__note">Bakım etiketi ve müşteri teknik föyünde kullanılan bilgiler.</p>
        {fields.composition}
        <div className="form__pair">
          {fields.fabricWeight}
          {fields.originCountryCode}
        </div>
        {fields.careInstructions}
      </div>

      <div className="tabs__panel" role="tabpanel" id="product-panel-trade" aria-labelledby="product-tab-trade" hidden={tab !== 'trade'}>
        <fieldset className="form__section">
          <legend>Fatura ve e-belge</legend>
          <div className="form__pair">
            {fields.gtipCode}
            {fields.vatRate}
          </div>
          {fields.salesUnit}
        </fieldset>
        <fieldset className="form__section">
          <legend>Fiyat</legend>
          <div className="form__pair">
            {fields.listPrice}
            {fields.currency}
          </div>
        </fieldset>
      </div>

      <div className="tabs__panel" role="tabpanel" id="product-panel-recipe" aria-labelledby="product-tab-recipe" hidden={tab !== 'recipe'}>
        <p className="muted form__note">
          Bir parça için gereken malzemeler. Demet açılırken bu miktarlar (fire dahil) hammadde deposundan düşülür; stok
          yetmezse demet açılmaz. Renk ya da beden seçilen satır yalnızca o parçalar için geçerlidir ve aynı malzemenin
          genel satırının yerine geçer.
        </p>
        {(current.isLoading || stockItems.isLoading) && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={current.error ?? stockItems.error} />

        <div className="table-wrap recipe-table">
          <table className="table table--compact">
            <colgroup>
              <col className="recipe-table__material" />
              <col className="recipe-table__variant" />
              <col className="recipe-table__variant" />
              <col className="recipe-table__amount" />
              <col className="recipe-table__waste" />
              <col className="recipe-table__cost" />
              <col className="recipe-table__remove" />
            </colgroup>
            <thead>
              <tr>
                <th>Malzeme</th>
                <th>Renk</th>
                <th>Beden</th>
                <th className="table__number">Miktar / parça</th>
                <th className="table__number">Fire %</th>
                <th className="table__number">Maliyet</th>
                <th aria-label="Satırı kaldır" />
              </tr>
            </thead>
            <tbody>
              {lines.map((line, index) => {
                const option = optionOf(line.stockItemId)
                return (
                  <tr key={line.key}>
                    <td>
                      <select
                        aria-label="Malzeme"
                        disabled={disabled}
                        value={line.stockItemId ?? ''}
                        title={option ? `${option.code} — ${option.name}` : undefined}
                        onChange={(event) => change(line.key, { stockItemId: idOf(event.target.value) })}
                      >
                        <option value="">Malzeme seçin…</option>
                        {options.map((item) => (
                          <option key={item.id} value={item.id}>{item.code} — {item.name}</option>
                        ))}
                      </select>
                    </td>
                    <td>
                      <select
                        aria-label="Renk"
                        disabled={disabled}
                        value={line.colorId ?? ''}
                        onChange={(event) => change(line.key, { colorId: idOf(event.target.value) })}
                      >
                        <option value="">Tümü</option>
                        {colors.data?.map((color) => <option key={color.id} value={color.id}>{color.name}</option>)}
                      </select>
                    </td>
                    <td>
                      <select
                        aria-label="Beden"
                        disabled={disabled}
                        value={line.sizeId ?? ''}
                        onChange={(event) => change(line.key, { sizeId: idOf(event.target.value) })}
                      >
                        <option value="">Tümü</option>
                        {sizes.data?.map((size) => <option key={size.id} value={size.id}>{size.name}</option>)}
                      </select>
                    </td>
                    <td>
                      <span className="input-unit">
                        <input
                          aria-label="Parça başı miktar"
                          inputMode="decimal"
                          disabled={disabled}
                          value={line.quantityPerPiece}
                          onChange={(event) => change(line.key, { quantityPerPiece: event.target.value })}
                        />
                        <span className="input-unit__suffix">{option ? stockUnitLabels[option.unit] : ''}</span>
                      </span>
                    </td>
                    <td>
                      <input
                        className="recipe-table__number-input"
                        aria-label="Fire yüzdesi"
                        inputMode="decimal"
                        disabled={disabled}
                        value={line.wastePercent}
                        onChange={(event) => change(line.key, { wastePercent: event.target.value })}
                      />
                    </td>
                    <td className="table__number">
                      {costs[index] === null ? <span className="muted">—</span> : formatMoney(costs[index], currency)}
                    </td>
                    <td className="recipe-table__remove-cell">
                      <button
                        type="button"
                        className="icon-button"
                        aria-label="Satırı kaldır"
                        title="Satırı kaldır"
                        disabled={disabled}
                        onClick={() => setDraft(lines.filter((item) => item.key !== line.key))}
                      >
                        <X aria-hidden="true" />
                      </button>
                    </td>
                  </tr>
                )
              })}
              {lines.length === 0 && (
                <tr>
                  <td colSpan={7} className="table__empty">Reçete boş. Reçetesi olmayan üründe demet açılırken stok düşülmez.</td>
                </tr>
              )}
            </tbody>
            {lines.length > 0 && (
              <tfoot>
                <tr>
                  <th scope="row" colSpan={5}>
                    Parça başı malzeme maliyeti
                    {hasUnpriced && <span className="muted recipe-table__note"> (birim maliyeti olmayan malzemeler hariç)</span>}
                  </th>
                  <td className="table__number">{formatMoney(totalCost, currency)}</td>
                  <td />
                </tr>
              </tfoot>
            )}
          </table>
        </div>
        <div>
          <button type="button" className="button" disabled={disabled} onClick={add}>Malzeme ekle</button>
        </div>
      </div>

      {canSeeCost && productId !== undefined && (
        <div className="tabs__panel" role="tabpanel" id="product-panel-cost" aria-labelledby="product-tab-cost" hidden={tab !== 'cost'}>
          {tab === 'cost' && <CostSummary productId={productId} />}
        </div>
      )}
    </div>
  )
}
