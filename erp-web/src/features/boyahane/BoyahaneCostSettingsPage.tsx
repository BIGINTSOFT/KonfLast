import { Save } from 'lucide-react'
import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { ErrorAlert } from '../../components/FormControls'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDateTime, formatQuantity } from '../../utils/format'
import { useCurrencyOptions } from '../finance/currencyOptions'
import { dyeMachineTypeLabels } from './boyahanePresentation'
import { chemicalCostMethodLabels, useGetBoyahaneCostSettingsQuery, useSaveBoyahaneCostSettingsMutation } from './boyahaneQuotationsApi'
import type { BoyahaneCostSettings, ChemicalCostMethod } from './boyahaneQuotationsApi'

type NumberField =
  | 'defaultMachineHourlyCost'
  | 'laborCostPerMinute'
  | 'defaultLiquorRatio'
  | 'defaultBatchKilograms'
  | 'overheadPercent'
  | 'firePercent'
  | 'marginPercent'
  | 'fabricDyeingMinutes'
  | 'yarnDyeingMinutes'
  | 'finishingMinutes'
  | 'printingMinutes'

const numberFields: NumberField[] = [
  'defaultMachineHourlyCost', 'laborCostPerMinute', 'defaultLiquorRatio', 'defaultBatchKilograms', 'overheadPercent', 'firePercent', 'marginPercent',
  'fabricDyeingMinutes', 'yarnDyeingMinutes', 'finishingMinutes', 'printingMinutes',
]

type Draft = Record<NumberField, string> & { currency: string; chemicalCostMethod: ChemicalCostMethod; rates: Record<number, string> }

function draftOf(settings: BoyahaneCostSettings): Draft {
  const values = Object.fromEntries(numberFields.map((field) => [field, String(settings[field])])) as Record<NumberField, string>
  return {
    ...values,
    currency: settings.currency,
    chemicalCostMethod: settings.chemicalCostMethod,
    rates: Object.fromEntries(settings.machines.map((machine) => [machine.machineId, machine.hourlyCost === null ? '' : String(machine.hourlyCost)])),
  }
}

const toNumber = (value: string) => Number(value.replace(',', '.'))

/**
 * Ön maliyet ve planlama parametreleri: makine saati (enerji + su) ve işçilik maliyeti, varsayılan flotte, parti büyüklüğü, fire,
 * genel gider ve kâr payı; işlem türlerinin varsayılan parti süresi (rotada standart süre yoksa teklif ve kapasite planı kullanır)
 * ve makineye özel saat maliyeti.
 */
export function BoyahaneCostSettingsPage() {
  const canManage = usePermission(Permissions.BoyahaneQuotationManage)
  const { data, error, isLoading } = useGetBoyahaneCostSettingsQuery()
  const [save, saveState] = useSaveBoyahaneCostSettingsMutation()
  const currencies = useCurrencyOptions(data?.currency)
  const [draft, setDraft] = useState<Draft | null>(null)
  const [saved, setSaved] = useState(false)

  useEffect(() => {
    if (data) {
      setDraft(draftOf(data))
    }
  }, [data])

  const set = (change: Partial<Draft>) => {
    setSaved(false)
    setDraft((current) => (current ? { ...current, ...change } : current))
  }

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    if (!draft || !data) {
      return
    }
    const numbers = Object.fromEntries(numberFields.map((field) => [field, toNumber(draft[field])])) as Record<NumberField, number>
    const result = await save({
      ...numbers,
      currency: draft.currency,
      chemicalCostMethod: draft.chemicalCostMethod,
      machineRates: data.machines.map((machine) => {
        const value = draft.rates[machine.machineId] ?? ''
        return { machineId: machine.machineId, hourlyCost: value.trim() === '' ? null : toNumber(value) }
      }),
    })
    setSaved(!('error' in result))
  }

  const field = (name: NumberField, label: string, step = '0.01', hint?: string) => (
    <label className="field">
      <span>{label}</span>
      <input type="number" min="0" step={step} required disabled={!canManage} value={draft?.[name] ?? ''} onChange={(event) => set({ [name]: event.target.value } as Partial<Draft>)} />
      {hint && <small className="muted">{hint}</small>}
    </label>
  )

  return (
    <section className="page">
      <h1 className="visually-hidden">Maliyet Parametreleri</h1>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {data && draft && (
        <form className="form" onSubmit={handleSubmit}>
          <p className="muted">
            {data.isDefault ? 'Parametreler henüz kaydedilmedi; gösterilen değerler varsayılandır.' : `Son değişiklik: ${data.updatedAt ? formatDateTime(data.updatedAt) : '—'}${data.updatedBy ? ` · ${data.updatedBy}` : ''}`}
            {' '}Teklif maliyeti kayıt anında hesaplanır; parametre değişikliği kayıtlı tekliflere ancak "Yeniden hesapla" ile yansır.
          </p>

          <fieldset className="form__section">
            <legend>Maliyet</legend>
            <div className="form__pair">
              <label className="field">
                <span>Para birimi</span>
                <select disabled={!canManage} value={draft.currency} onChange={(event) => set({ currency: event.target.value })}>
                  {!currencies.some((option) => option.value === draft.currency) && <option value={draft.currency}>{draft.currency}</option>}
                  {currencies.map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}
                </select>
              </label>
              {field('defaultMachineHourlyCost', 'Makine saat maliyeti (enerji + su)', '0.01', 'Makineye özel maliyet girilmemişse')}
              {field('laborCostPerMinute', 'İşçilik maliyeti (dakika)', '0.0001')}
            </div>
            <div className="form__pair">
              <label className="field">
                <span>Boya ve kimyasal maliyeti</span>
                <select disabled={!canManage} value={draft.chemicalCostMethod} onChange={(event) => set({ chemicalCostMethod: event.target.value as ChemicalCostMethod })}>
                  {Object.entries(chemicalCostMethodLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
                </select>
              </label>
              {field('defaultLiquorRatio', 'Varsayılan flotte oranı (1:x)', '0.1', 'Makinede ve satırda flotte yoksa')}
              {field('defaultBatchKilograms', 'Varsayılan parti büyüklüğü (kg)', '1', 'Makine seçilmemiş teklif satırında')}
            </div>
            <div className="form__pair">
              {field('firePercent', 'Fire (%)', '0.1')}
              {field('overheadPercent', 'Genel gider (%)', '0.1')}
              {field('marginPercent', 'Kâr payı (%)', '0.1')}
            </div>
          </fieldset>

          <fieldset className="form__section">
            <legend>Varsayılan parti süreleri (dakika)</legend>
            <p className="muted form__note">Rotadaki operasyonlarda standart süre yoksa teklif ve kapasite planı bu süreleri kullanır.</p>
            <div className="form__pair">
              {field('fabricDyeingMinutes', 'Kumaş boyama', '1')}
              {field('yarnDyeingMinutes', 'İplik boyama', '1')}
              {field('finishingMinutes', 'Terbiye / apre', '1')}
              {field('printingMinutes', 'Baskı', '1')}
            </div>
          </fieldset>

          <fieldset className="form__section">
            <legend>Makine saat maliyetleri ({draft.currency})</legend>
            <div className="table-wrap">
              <table className="table table--compact">
                <thead>
                  <tr><th>Makine</th><th>Tür</th><th className="table__number">Kapasite (kg)</th><th className="table__number">Saat maliyeti</th></tr>
                </thead>
                <tbody>
                  {data.machines.map((machine) => (
                    <tr key={machine.machineId}>
                      <td><strong>{machine.code}</strong> {machine.name}{!machine.isActive && <span className="muted"> (pasif)</span>}</td>
                      <td>{dyeMachineTypeLabels[machine.machineType] ?? machine.machineType}</td>
                      <td className="table__number">{formatQuantity(machine.capacityKg)}</td>
                      <td className="table__number">
                        <input
                          type="number"
                          min="0"
                          step="0.01"
                          aria-label={`${machine.code} saat maliyeti`}
                          placeholder={`Varsayılan (${draft.defaultMachineHourlyCost})`}
                          disabled={!canManage}
                          value={draft.rates[machine.machineId] ?? ''}
                          onChange={(event) => set({ rates: { ...draft.rates, [machine.machineId]: event.target.value } })}
                        />
                      </td>
                    </tr>
                  ))}
                  {data.machines.length === 0 && <tr><td colSpan={4} className="table__empty">Etkin makine yok.</td></tr>}
                </tbody>
              </table>
            </div>
          </fieldset>

          <ErrorAlert error={saveState.error} />
          {saved && <p className="alert alert--info">Parametreler kaydedildi.</p>}
          {canManage && (
            <footer className="form__actions">
              <button type="submit" className="button button--primary" disabled={saveState.isLoading}><Save aria-hidden="true" />Kaydet</button>
            </footer>
          )}
        </form>
      )}
    </section>
  )
}
