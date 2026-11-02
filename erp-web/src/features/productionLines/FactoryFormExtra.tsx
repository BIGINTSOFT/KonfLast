import { useState } from 'react'
import type { FormExtraProps } from '../masterData/resources'

type Tab = 'general' | 'address'

const tabs: { key: Tab; label: string }[] = [
  { key: 'general', label: 'Genel' },
  { key: 'address', label: 'Adres ve iletişim' },
]

const addressFields = ['phone', 'email', 'countryCode', 'city', 'district', 'postalCode', 'addressLine']

/** Fabrika formu: genel bilgiler, adres ve iletişim; düzenlemede fabrikanın yapısı (alan, bant, makine) özetlenir. */
export function FactoryFormExtra({ row, fields, activeControl, errorFields }: FormExtraProps) {
  const [tab, setTab] = useState<Tab>('general')
  // Sunucu bir alanı reddedince o alanın sekmesi açılır.
  const errorKey = errorFields.join(',')
  const [seenErrors, setSeenErrors] = useState(errorKey)
  if (errorKey !== seenErrors) {
    setSeenErrors(errorKey)
    if (errorFields.length > 0) {
      setTab(errorFields.some((name) => !addressFields.includes(name)) ? 'general' : 'address')
    }
  }

  return (
    <div className="factory-form">
      {row && (
        <dl className="stat-strip">
          <div><dt>Üretim alanı</dt><dd>{Number(row.areaCount ?? 0)}</dd></div>
          <div><dt>Bant</dt><dd>{Number(row.lineCount ?? 0)}</dd></div>
          <div><dt>Bantlardaki makine</dt><dd>{Number(row.machineCount ?? 0)}</dd></div>
          <div><dt>Kapalı alan</dt><dd>{row.indoorArea ? `${Number(row.indoorArea).toLocaleString('tr-TR')} m²` : '—'}</dd></div>
        </dl>
      )}

      <div className="tabs" role="tablist" aria-label="Fabrika bilgileri">
        {tabs.map((item) => (
          <button
            key={item.key}
            type="button"
            role="tab"
            id={`factory-tab-${item.key}`}
            aria-selected={tab === item.key}
            aria-controls={`factory-panel-${item.key}`}
            className={`tabs__tab${tab === item.key ? ' tabs__tab--active' : ''}`}
            onClick={() => setTab(item.key)}
          >
            {item.label}
          </button>
        ))}
      </div>

      {/* Sekmeler gizlenir, kaldırılmaz: diğer sekmedeki girişler korunur. */}
      <div className="tabs__panel" role="tabpanel" id="factory-panel-general" aria-labelledby="factory-tab-general" hidden={tab !== 'general'}>
        <div className="form__pair">
          {fields.code}
          {fields.name}
        </div>
        <div className="form__pair">
          {fields.managerName}
          {fields.indoorArea}
        </div>
        {fields.sgkWorkplaceNo}
        {activeControl}
      </div>

      <div className="tabs__panel" role="tabpanel" id="factory-panel-address" aria-labelledby="factory-tab-address" hidden={tab !== 'address'}>
        <fieldset className="form__section">
          <legend>Adres</legend>
          <div className="form__pair">
            {fields.countryCode}
            {fields.city}
          </div>
          <div className="form__pair">
            {fields.district}
            {fields.postalCode}
          </div>
          {fields.addressLine}
        </fieldset>
        <fieldset className="form__section">
          <legend>İletişim</legend>
          <div className="form__pair">
            {fields.phone}
            {fields.email}
          </div>
        </fieldset>
      </div>
    </div>
  )
}
