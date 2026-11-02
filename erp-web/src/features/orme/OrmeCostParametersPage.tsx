import { useState } from 'react'
import type { FormEvent } from 'react'
import { ErrorAlert } from '../../components/FormControls'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { useGetOrmeCostSettingsQuery, useSaveOrmeCostSettingsMutation } from './ormeQuotationsApi'
import type { OrmeCostSettings } from './ormeQuotationsApi'

const text = (value: number | null) => (value === null ? '' : String(value))

function CostParametersForm({ settings, canManage }: { settings: OrmeCostSettings; canManage: boolean }) {
  const [save, { isLoading, error, isSuccess }] = useSaveOrmeCostSettingsMutation()
  const [machineCost, setMachineCost] = useState(text(settings.machineHourlyCost))
  const [laborCost, setLaborCost] = useState(text(settings.laborHourlyCost))
  const [overhead, setOverhead] = useState(text(settings.overheadPercent))
  const [waste, setWaste] = useState(text(settings.defaultWastePercent))
  const [margin, setMargin] = useState(text(settings.defaultMarginPercent))
  const [rates, setRates] = useState<Record<number, string>>(
    Object.fromEntries(settings.machines.map((machine) => [machine.machineId, text(machine.hourlyCost)])),
  )

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    await save({
      machineHourlyCost: Number(machineCost),
      laborHourlyCost: Number(laborCost),
      overheadPercent: Number(overhead),
      defaultWastePercent: Number(waste),
      defaultMarginPercent: Number(margin),
      machines: settings.machines
        .filter((machine) => (rates[machine.machineId] ?? '').trim() !== '')
        .map((machine) => ({ machineId: machine.machineId, hourlyCost: Number(rates[machine.machineId]) })),
    })
  }

  const currency = settings.currency

  return (
    <form className="form" onSubmit={handleSubmit}>
      <fieldset className="form__section" disabled={!canManage}>
        <legend>Genel parametreler</legend>
        <div className="form__pair">
          <label className="field">
            <span>Makine saat maliyeti ({currency})</span>
            <input type="number" min="0" step="0.0001" required value={machineCost} onChange={(event) => setMachineCost(event.target.value)} />
            <span className="field__hint">Amortisman, enerji, bakım; makineye özel tutar aşağıda girilebilir.</span>
          </label>
          <label className="field">
            <span>İşçilik (makine saati başına, {currency})</span>
            <input type="number" min="0" step="0.0001" required value={laborCost} onChange={(event) => setLaborCost(event.target.value)} />
          </label>
        </div>
        <div className="form__pair">
          <label className="field">
            <span>Genel gider (%)</span>
            <input type="number" min="0" max="1000" step="0.01" required value={overhead} onChange={(event) => setOverhead(event.target.value)} />
            <span className="field__hint">Doğrudan maliyetin (iplik + örme) üzerine eklenir.</span>
          </label>
          <label className="field">
            <span>Varsayılan fire (%)</span>
            <input type="number" min="0" max="100" step="0.01" required value={waste} onChange={(event) => setWaste(event.target.value)} />
            <span className="field__hint">Kumaş kartında fire girilmemişse kullanılır.</span>
          </label>
        </div>
        <div className="form__pair">
          <label className="field">
            <span>Varsayılan kâr (%)</span>
            <input type="number" min="0" max="1000" step="0.01" required value={margin} onChange={(event) => setMargin(event.target.value)} />
            <span className="field__hint">Toplam maliyetin üzerine eklenir; teklif satırında değiştirilebilir.</span>
          </label>
        </div>
      </fieldset>

      <fieldset className="form__section" disabled={!canManage}>
        <legend>Makineye özel saat maliyeti ({currency})</legend>
        <p className="muted">Boş bırakılan makinede genel makine saat maliyeti kullanılır.</p>
        <div className="table-wrap">
          <table className="table table--compact">
            <thead>
              <tr>
                <th>Makine</th>
                <th className="table__number">Saat maliyeti</th>
              </tr>
            </thead>
            <tbody>
              {settings.machines.map((machine) => (
                <tr key={machine.machineId}>
                  <td>
                    {machine.machineCode} — {machine.machineName}
                    {!machine.isActive && <span className="muted"> (pasif)</span>}
                  </td>
                  <td className="table__number">
                    <input
                      aria-label={`${machine.machineCode} saat maliyeti`}
                      className="matrix-input purchase-input"
                      type="number"
                      min="0"
                      step="0.0001"
                      placeholder={text(Number(machineCost))}
                      value={rates[machine.machineId] ?? ''}
                      onChange={(event) => setRates({ ...rates, [machine.machineId]: event.target.value })}
                    />
                  </td>
                </tr>
              ))}
              {settings.machines.length === 0 && <tr><td colSpan={2} className="table__empty">Makine tanımlı değil.</td></tr>}
            </tbody>
          </table>
        </div>
      </fieldset>

      <ErrorAlert error={error} />
      {isSuccess && <p className="alert alert--info" role="status">Parametreler kaydedildi. Yeni teklif satırlarına uygulanır; mevcut teklifler değişmez.</p>}
      {canManage && (
        <footer className="form__actions">
          <button type="submit" className="button button--primary" disabled={isLoading}>Kaydet</button>
        </footer>
      )}
    </form>
  )
}

/** Ön maliyet parametreleri: teklif satırlarına varsayılan olarak gelir; teklifte satır bazında değiştirilebilir. */
export function OrmeCostParametersPage() {
  const canManage = usePermission(Permissions.OrmeQuotationManage)
  const { data, error, isLoading } = useGetOrmeCostSettingsQuery()

  return (
    <section className="page">
      <h1 className="visually-hidden">Maliyet Parametreleri</h1>
      <p className="muted">
        Örme ön maliyeti: iplik (oran × son alış fiyatı × (1 + fire)) + örme ((makine + işçilik saat maliyeti) / kumaş kartındaki kg/saat),
        üzerine genel gider; fiyat maliyet × (1 + kâr). Saatlik tutarlar yerel para biriminde girilir, teklifte teklifin para birimine çevrilir.
      </p>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {data && <CostParametersForm settings={data} canManage={canManage} />}
    </section>
  )
}
