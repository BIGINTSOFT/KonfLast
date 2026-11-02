import { Save } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { ErrorAlert } from '../../components/FormControls'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDateTime } from '../../utils/format'
import { useCurrencyOptions } from '../finance/currencyOptions'
import { useGetDokumaCostSettingsQuery, useSaveDokumaCostSettingsMutation } from './dokumaQuotationsApi'
import type { DokumaCostSettings } from './dokumaQuotationsApi'

const generalFields = [
  { key: 'loomHourlyCost', label: 'Tezgâh saat maliyeti (enerji + amortisman)', step: '0.0001' },
  { key: 'laborHourlyCost', label: 'İşçilik (tezgâh saati başına)', step: '0.0001' },
  { key: 'warpingCostPerMeter', label: 'Çözgü maliyeti (metre çözgü)', step: '0.0001' },
  { key: 'sizingCostPerMeter', label: 'Haşıl maliyeti (metre çözgü)', step: '0.0001' },
  { key: 'overheadPercent', label: 'Genel gider (%)', step: '0.01' },
  { key: 'defaultWastePercent', label: 'Varsayılan fire (%)', step: '0.01' },
  { key: 'defaultMarginPercent', label: 'Varsayılan kâr marjı (%)', step: '0.01' },
  { key: 'defaultEfficiencyPercent', label: 'Varsayılan tezgâh verimi (%)', step: '0.1' },
  { key: 'quotationValidityDays', label: 'Teklif geçerlilik süresi (gün)', step: '1' },
] as const

type GeneralKey = (typeof generalFields)[number]['key']

const text = (value: number | null) => (value === null ? '' : String(value))
const optional = (value: string) => (value.trim() === '' ? null : Number(value))

function SettingsForm({ settings, canManage }: { settings: DokumaCostSettings; canManage: boolean }) {
  const [save, { isLoading, error, isSuccess }] = useSaveDokumaCostSettingsMutation()
  const currencies = useCurrencyOptions(settings.currency)
  const [currency, setCurrency] = useState(settings.currency)
  const [useSizingRecipe, setUseSizingRecipe] = useState(settings.useSizingRecipe)
  const [values, setValues] = useState<Record<GeneralKey, string>>(
    Object.fromEntries(generalFields.map((field) => [field.key, String(settings[field.key])])) as Record<GeneralKey, string>,
  )
  const [looms, setLooms] = useState(settings.looms.map((loom) => ({ ...loom, hourly: text(loom.hourlyCost), efficiency: text(loom.efficiencyPercent) })))

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    await save({
      currency,
      useSizingRecipe,
      loomHourlyCost: Number(values.loomHourlyCost),
      laborHourlyCost: Number(values.laborHourlyCost),
      warpingCostPerMeter: Number(values.warpingCostPerMeter),
      sizingCostPerMeter: Number(values.sizingCostPerMeter),
      overheadPercent: Number(values.overheadPercent),
      defaultWastePercent: Number(values.defaultWastePercent),
      defaultMarginPercent: Number(values.defaultMarginPercent),
      defaultEfficiencyPercent: Number(values.defaultEfficiencyPercent),
      quotationValidityDays: Number(values.quotationValidityDays),
      looms: looms.map((loom) => ({ loomId: loom.loomId, hourlyCost: optional(loom.hourly), efficiencyPercent: optional(loom.efficiency) })),
    })
  }

  return (
    <form className="form" onSubmit={handleSubmit}>
      <fieldset className="form__section" disabled={!canManage}>
        <legend>Genel parametreler</legend>
        <div className="form__pair" style={{ gridTemplateColumns: 'repeat(auto-fill, minmax(220px, 1fr))' }}>
          <label className="field">
            <span>Tutarların para birimi</span>
            <select value={currency} onChange={(event) => setCurrency(event.target.value)}>
              {currencies.map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}
            </select>
          </label>
          {generalFields.map((field) => (
            <label key={field.key} className="field">
              <span>{field.label}</span>
              <input
                type="number"
                min="0"
                step={field.step}
                required
                value={values[field.key]}
                onChange={(event) => setValues({ ...values, [field.key]: event.target.value })}
              />
            </label>
          ))}
        </div>
        <label className="checkbox">
          <input type="checkbox" checked={useSizingRecipe} onChange={(event) => setUseSizingRecipe(event.target.checked)} />
          {' '}Haşıl maliyetini artikelin varsayılan haşıl reçetesinden hesapla (kimyasal × son lot fiyatı); reçete ya da fiyat yoksa yukarıdaki sabit maliyet
        </label>
      </fieldset>

      <fieldset className="form__section" disabled={!canManage}>
        <legend>Tezgâha özel değerler</legend>
        <p className="muted">Boş bırakılan alan genel değerden alınır. Verim kapasite planında da kullanılır; hız tezgâh kartındaki anma hızıdır.</p>
        <div className="table-wrap">
          <table className="table table--compact">
            <thead>
              <tr>
                <th>Tezgâh</th>
                <th className="table__number">Anma hızı (devir/dk)</th>
                <th className="table__number">Saat maliyeti</th>
                <th className="table__number">Verim (%)</th>
              </tr>
            </thead>
            <tbody>
              {looms.map((loom, index) => (
                <tr key={loom.loomId}>
                  <td><strong>{loom.loomCode}</strong> {loom.loomName}{!loom.isActive && <span className="muted"> (pasif)</span>}</td>
                  <td className="table__number">{loom.nominalSpeedRpm ?? <span className="muted" title="Tezgâh kartında anma hızı yok">Yok</span>}</td>
                  <td className="table__number">
                    <input
                      type="number"
                      min="0"
                      step="0.0001"
                      aria-label={`${loom.loomCode} saat maliyeti`}
                      placeholder={values.loomHourlyCost}
                      value={loom.hourly}
                      onChange={(event) => setLooms(looms.map((item, i) => (i === index ? { ...item, hourly: event.target.value } : item)))}
                    />
                  </td>
                  <td className="table__number">
                    <input
                      type="number"
                      min="0"
                      max="100"
                      step="0.1"
                      aria-label={`${loom.loomCode} verim`}
                      placeholder={values.defaultEfficiencyPercent}
                      value={loom.efficiency}
                      onChange={(event) => setLooms(looms.map((item, i) => (i === index ? { ...item, efficiency: event.target.value } : item)))}
                    />
                  </td>
                </tr>
              ))}
              {looms.length === 0 && <tr><td colSpan={4} className="table__empty">Etkin tezgâh yok.</td></tr>}
            </tbody>
          </table>
        </div>
      </fieldset>

      <ErrorAlert error={error} />
      {isSuccess && <p className="alert alert--info" role="status">Parametreler kaydedildi.</p>}
      {canManage && (
        <footer className="form__actions">
          <button type="submit" className="button button--primary" disabled={isLoading}><Save aria-hidden="true" />Kaydet</button>
        </footer>
      )}
    </form>
  )
}

/**
 * Ön maliyet parametreleri: tezgâh ve işçilik saat maliyeti, çözgü ve haşıl metre maliyeti, genel gider, varsayılan fire, kâr marjı ve
 * verim; tezgâha özel saat maliyeti ve verim. Değişiklik yalnızca bundan sonra hesaplanan tekliflere uygulanır.
 */
export function DokumaCostSettingsPage() {
  const canManage = usePermission(Permissions.DokumaQuotationManage)
  const { data, error, isLoading } = useGetDokumaCostSettingsQuery()

  return (
    <section className="page">
      <h1 className="visually-hidden">Maliyet Parametreleri</h1>
      <p className="muted">
        Teklif ön maliyetinde kullanılır: metre maliyeti = iplik + haşıl + çözgü + dokuma süresi × (tezgâh + işçilik saat maliyeti); fire ve genel
        gider eklenir, kâr marjıyla fiyat bulunur. Kayıtlı tekliflerin dökümü değişmez.
        {data?.updatedAt && ` Son değişiklik: ${formatDateTime(data.updatedAt)}${data.updatedBy ? ` (${data.updatedBy})` : ''}.`}
      </p>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {data && <SettingsForm settings={data} canManage={canManage} />}
    </section>
  )
}
