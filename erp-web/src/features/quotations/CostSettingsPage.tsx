import { useState } from 'react'
import type { FormEvent } from 'react'
import { fieldErrors } from '../../api/envelope'
import { ErrorAlert, TextField } from '../../components/FormControls'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDateTime } from '../../utils/format'
import { parseDecimal } from '../../utils/money'
import { CurrencySelect } from '../finance/CurrencySelect'
import { useGetCostSettingQuery, useSaveCostSettingMutation } from './quotationsApi'
import type { CostSetting } from './quotationsApi'

const text = (value: number) => String(value).replace('.', ',')

function SettingsForm({ setting }: { setting: CostSetting }) {
  const canManage = usePermission(Permissions.QuotationManage)
  const [save, { isLoading, error, isSuccess }] = useSaveCostSettingMutation()
  const [labor, setLabor] = useState(text(setting.laborCostPerMinute))
  const [energy, setEnergy] = useState(text(setting.energyCostPerMinute))
  const [currency, setCurrency] = useState(setting.currency)
  const [overhead, setOverhead] = useState(text(setting.overheadPercent))
  const [fire, setFire] = useState(text(setting.defaultFirePercent))
  const [margin, setMargin] = useState(text(setting.defaultMarginPercent))

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    // Geçersiz giriş -1 gönderilir; sunucu kuralı söyler.
    await save({
      laborCostPerMinute: parseDecimal(labor) ?? -1,
      energyCostPerMinute: parseDecimal(energy || '0') ?? -1,
      currency,
      overheadPercent: parseDecimal(overhead || '0') ?? -1,
      defaultFirePercent: parseDecimal(fire || '0') ?? -1,
      defaultMarginPercent: parseDecimal(margin || '0') ?? -1,
    })
  }

  return (
    <form className="form" onSubmit={(event) => void handleSubmit(event)} noValidate>
      <fieldset className="form__section" disabled={!canManage}>
        <legend>Dakika maliyetleri</legend>
        <div className="form__grid">
          <TextField
            label="İşçilik maliyeti (dakika başı)"
            inputMode="decimal"
            value={labor}
            onChange={(event) => setLabor(event.target.value)}
            errors={fieldErrors(error, 'laborCostPerMinute')}
            hint="Ücret ve yan haklar dahil bir dakikalık dikim maliyeti; dakika = rotanın fason olmayan adımlarının SAM toplamı."
          />
          <TextField
            label="Enerji maliyeti (dakika başı)"
            inputMode="decimal"
            value={energy}
            onChange={(event) => setEnergy(event.target.value)}
            errors={fieldErrors(error, 'energyCostPerMinute')}
            hint="Kullanılmıyorsa 0."
          />
          <CurrencySelect label="Para birimi" value={currency} onChange={setCurrency} errors={fieldErrors(error, 'currency')} hint="Teklifte teklifin para birimine çevrilir." />
        </div>
      </fieldset>
      <fieldset className="form__section" disabled={!canManage}>
        <legend>Varsayılan yüzdeler</legend>
        <div className="form__grid">
          <TextField label="Genel gider (%)" inputMode="decimal" value={overhead} onChange={(event) => setOverhead(event.target.value)} errors={fieldErrors(error, 'overheadPercent')} hint="(Doğrudan maliyet + fire) üzerine." />
          <TextField label="Fire (%)" inputMode="decimal" value={fire} onChange={(event) => setFire(event.target.value)} errors={fieldErrors(error, 'defaultFirePercent')} hint="Doğrudan maliyet üzerine." />
          <TextField label="Kâr marjı (%)" inputMode="decimal" value={margin} onChange={(event) => setMargin(event.target.value)} errors={fieldErrors(error, 'defaultMarginPercent')} hint="Birim maliyet üzerine." />
        </div>
      </fieldset>
      <p className="muted form__note">
        Yeni teklif satırları bu değerlerle açılır; satırda yüzdeler değiştirilebilir. Dakika maliyetleri taslak teklif her
        kaydedildiğinde bu ayardan okunur.
        {setting.updatedAt && ` Son değişiklik: ${setting.updatedBy ?? '—'} · ${formatDateTime(setting.updatedAt)}.`}
      </p>
      <ErrorAlert error={error} fields={['laborCostPerMinute', 'energyCostPerMinute', 'currency', 'overheadPercent', 'defaultFirePercent', 'defaultMarginPercent']} />
      {isSuccess && <p className="alert alert--info" role="status">Kaydedildi.</p>}
      {canManage && (
        <footer className="form__actions">
          <button type="submit" className="button button--primary" disabled={isLoading}>
            {isLoading ? 'Kaydediliyor…' : 'Kaydet'}
          </button>
        </footer>
      )}
    </form>
  )
}

/** Ön maliyet parametreleri: teklif maliyet hesabının dakika maliyetleri ve varsayılan yüzdeleri. */
export function CostSettingsPage() {
  const { data, error, isLoading } = useGetCostSettingQuery()

  return (
    <section className="page">
      <h1 className="visually-hidden">Ön Maliyet Parametreleri</h1>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {data && (
        <div className="card card--spaced">
          <SettingsForm setting={data} />
        </div>
      )}
    </section>
  )
}
