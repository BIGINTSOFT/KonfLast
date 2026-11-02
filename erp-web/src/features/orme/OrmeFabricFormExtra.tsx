import { X } from 'lucide-react'
import { useEffect, useState } from 'react'
import { ErrorAlert } from '../../components/FormControls'
import { LookupSelect } from '../masterData/LookupSelect'
import type { FormExtraProps } from '../masterData/resources'
import { useGetOrmeFabricYarnsQuery, useSaveOrmeFabricYarnsMutation } from './ormeFabricsApi'

type Tab = 'general' | 'yarns'

interface YarnDraft {
  key: number
  yarnId: number | null
  percent: string
  stitchLengthMm: string
}

/**
 * Kumaş formu. İplikler kartın alanı değildir; kart kaydedildikten sonra (yeni kayıtta Id oluşunca) kendi ucuna
 * sırasıyla yazılır. Oranların toplamı 100 olmalıdır.
 */
export function OrmeFabricFormExtra({ row, fields, activeControl, disabled, onAfterSaveChange }: FormExtraProps) {
  const [tab, setTab] = useState<Tab>('general')
  const fabricId = row?.id
  const stored = useGetOrmeFabricYarnsQuery(fabricId ?? 0, { skip: fabricId === undefined })
  const [saveYarns] = useSaveOrmeFabricYarnsMutation()
  // Düzenleme yapılmadıysa satırlar kayıtta yeniden gönderilmez.
  const [draft, setDraft] = useState<YarnDraft[] | null>(null)
  const [nextKey, setNextKey] = useState(-1)
  const lines: YarnDraft[] = draft
    ?? (stored.data?.yarns ?? []).map((yarn) => ({ key: yarn.id, yarnId: yarn.yarnId, percent: String(yarn.percent), stitchLengthMm: yarn.stitchLengthMm === null ? '' : String(yarn.stitchLengthMm) }))

  useEffect(() => {
    onAfterSaveChange(async (id) => {
      if (draft === null) {
        return undefined
      }
      const result = await saveYarns({
        fabricId: id,
        // Boş satırlar sunucuya gitmez; sunucu kalan satırları denetler.
        yarns: draft
          .filter((line) => line.yarnId !== null || line.percent.trim() !== '')
          .map((line) => ({ yarnId: line.yarnId ?? 0, percent: Number(line.percent) || 0, stitchLengthMm: line.stitchLengthMm.trim() === '' ? null : Number(line.stitchLengthMm) })),
      })
      return 'error' in result ? result.error : undefined
    })
    return () => onAfterSaveChange(null)
  })

  const update = (key: number, change: Partial<YarnDraft>) =>
    setDraft(lines.map((line) => (line.key === key ? { ...line, ...change } : line)))

  const add = () => {
    setDraft([...lines, { key: nextKey, yarnId: null, percent: '', stitchLengthMm: '' }])
    setNextKey(nextKey - 1)
  }

  const total = lines.reduce((sum, line) => sum + (Number(line.percent) || 0), 0)

  return (
    <div className="customer-form">
      <div className="tabs" role="tablist" aria-label="Kumaş bilgileri">
        {(['general', 'yarns'] as const).map((key) => (
          <button
            key={key}
            type="button"
            role="tab"
            id={`fabric-tab-${key}`}
            aria-selected={tab === key}
            aria-controls={`fabric-panel-${key}`}
            className={`tabs__tab${tab === key ? ' tabs__tab--active' : ''}`}
            onClick={() => setTab(key)}
          >
            {key === 'general' ? 'Genel' : 'İplikler'}
            {key === 'yarns' && lines.length > 0 && <span className="tabs__count">{lines.length}</span>}
          </button>
        ))}
      </div>

      {/* Sekmeler gizlenir, kaldırılmaz: diğer sekmedeki girişler korunur. */}
      <div className="tabs__panel" role="tabpanel" id="fabric-panel-general" aria-labelledby="fabric-tab-general" hidden={tab !== 'general'}>
        <div className="form__pair">
          {fields.fabricCode}
          {fields.bedType}
        </div>
        {fields.name}
        <div className="form__pair">
          {fields.knitName}
          {fields.composition}
        </div>
        <div className="form__pair">
          {fields.diameterInch}
          {fields.gauge}
        </div>
        <div className="form__pair">
          {fields.rawWidthCm}
          {fields.rawWeightGsm}
        </div>
        <div className="form__pair">
          {fields.wastePercent}
          {fields.expectedKilogramsPerHour}
        </div>
        {activeControl}
      </div>

      <div className="tabs__panel" role="tabpanel" id="fabric-panel-yarns" aria-labelledby="fabric-tab-yarns" hidden={tab !== 'yarns'}>
        {stored.isLoading && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={stored.error} />
        <fieldset className="form__section">
          <legend>İplikler{lines.length > 0 && ` · toplam %${total.toLocaleString('tr-TR')}`}</legend>
          <div className="table-wrap recipe-table">
            <table className="table table--compact">
              <thead>
                <tr>
                  <th style={{ width: 56 }}>Sıra</th>
                  <th style={{ width: '55%' }}>İplik</th>
                  <th>Oran (%)</th>
                  <th>İlmek uzunluğu (mm)</th>
                  <th aria-label="Satırı kaldır" />
                </tr>
              </thead>
              <tbody>
                {lines.map((line, index) => (
                  <tr key={line.key}>
                    <td>{index + 1}</td>
                    <td>
                      <LookupSelect
                        resource="orme-yarns"
                        label="İplik"
                        required
                        hideLabel
                        idSuffix={`fabric-yarn-${line.key}`}
                        value={line.yarnId}
                        disabled={disabled}
                        onChange={(yarnId) => update(line.key, { yarnId })}
                      />
                    </td>
                    <td>
                      <input aria-label="Oran" type="number" min={0} max={100} step="0.01" value={line.percent} disabled={disabled}
                        onChange={(event) => update(line.key, { percent: event.target.value })} />
                    </td>
                    <td>
                      <input aria-label="İlmek uzunluğu" type="number" min={0} step="0.01" value={line.stitchLengthMm} disabled={disabled}
                        onChange={(event) => update(line.key, { stitchLengthMm: event.target.value })} />
                    </td>
                    <td className="recipe-table__remove-cell">
                      <button type="button" className="icon-button" aria-label="Satırı kaldır" title="Satırı kaldır" disabled={disabled}
                        onClick={() => setDraft(lines.filter((item) => item.key !== line.key))}>
                        <X aria-hidden="true" />
                      </button>
                    </td>
                  </tr>
                ))}
                {lines.length === 0 && !stored.isLoading && (
                  <tr>
                    <td colSpan={5} className="table__empty">Kayıtlı iplik yok.</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
          <div>
            <button type="button" className="button" onClick={add} disabled={disabled}>İplik ekle</button>
          </div>
        </fieldset>
        <p className="field__hint">Oran, ipliğin kumaş ağırlığındaki payıdır (örn. pamuk 95, likra 5); toplam 100 olmalıdır. İplik ihtiyacı bu orandan hesaplanır.</p>
      </div>
    </div>
  )
}
