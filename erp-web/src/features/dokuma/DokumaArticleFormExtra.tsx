import { X } from 'lucide-react'
import { useEffect, useState } from 'react'
import { ErrorAlert } from '../../components/FormControls'
import { LookupSelect } from '../masterData/LookupSelect'
import type { FormExtraProps } from '../masterData/resources'
import { useGetDokumaArticleYarnsQuery, useSaveDokumaArticleYarnsMutation } from './dokumaArticlesApi'
import type { ArticleYarnRole } from './dokumaArticlesApi'

type Tab = 'general' | 'yarns'

interface YarnDraft {
  key: number
  role: ArticleYarnRole
  yarnId: number | null
  ends: string
}

const sections: { role: ArticleYarnRole; title: string; endsLabel: string; addLabel: string }[] = [
  { role: 'WARP', title: 'Çözgü', endsLabel: 'Tel sayısı', addLabel: 'Çözgü ipliği ekle' },
  { role: 'WEFT', title: 'Atkı', endsLabel: 'Rapordaki atkı', addLabel: 'Atkı ipliği ekle' },
]

/**
 * Artikel formu. Çözgü ve atkı iplikleri kartın alanı değildir; kart kaydedildikten sonra (yeni kayıtta Id oluşunca)
 * kendi ucuna sırasıyla yazılır. Toplam tel sayısı çözgü satırlarının toplamıdır.
 */
export function DokumaArticleFormExtra({ row, fields, activeControl, disabled, onAfterSaveChange }: FormExtraProps) {
  const [tab, setTab] = useState<Tab>('general')
  const articleId = row?.id
  const stored = useGetDokumaArticleYarnsQuery(articleId ?? 0, { skip: articleId === undefined })
  const [saveYarns] = useSaveDokumaArticleYarnsMutation()
  // Düzenleme yapılmadıysa satırlar kayıtta yeniden gönderilmez.
  const [draft, setDraft] = useState<YarnDraft[] | null>(null)
  const [nextKey, setNextKey] = useState(-1)
  const lines: YarnDraft[] = draft
    ?? (stored.data?.yarns ?? []).map((yarn) => ({ key: yarn.id, role: yarn.role, yarnId: yarn.yarnId, ends: String(yarn.ends) }))

  useEffect(() => {
    onAfterSaveChange(async (id) => {
      if (draft === null) {
        return undefined
      }
      const result = await saveYarns({
        articleId: id,
        // Boş satırlar sunucuya gitmez; sunucu kalan satırları denetler.
        yarns: draft
          .filter((line) => line.yarnId !== null || line.ends.trim() !== '')
          .map((line) => ({ role: line.role, yarnId: line.yarnId ?? 0, ends: Number(line.ends) || 0 })),
      })
      return 'error' in result ? result.error : undefined
    })
    return () => onAfterSaveChange(null)
  })

  const update = (key: number, change: Partial<YarnDraft>) =>
    setDraft(lines.map((line) => (line.key === key ? { ...line, ...change } : line)))

  const add = (role: ArticleYarnRole) => {
    setDraft([...lines, { key: nextKey, role, yarnId: null, ends: '' }])
    setNextKey(nextKey - 1)
  }

  const totalEnds = lines.filter((line) => line.role === 'WARP').reduce((sum, line) => sum + (Number(line.ends) || 0), 0)

  return (
    <div className="customer-form">
      <div className="tabs" role="tablist" aria-label="Artikel bilgileri">
        {(['general', 'yarns'] as const).map((key) => (
          <button
            key={key}
            type="button"
            role="tab"
            id={`article-tab-${key}`}
            aria-selected={tab === key}
            aria-controls={`article-panel-${key}`}
            className={`tabs__tab${tab === key ? ' tabs__tab--active' : ''}`}
            onClick={() => setTab(key)}
          >
            {key === 'general' ? 'Genel' : 'İplikler'}
            {key === 'yarns' && lines.length > 0 && <span className="tabs__count">{lines.length}</span>}
          </button>
        ))}
      </div>

      {/* Sekmeler gizlenir, kaldırılmaz: diğer sekmedeki girişler korunur. */}
      <div className="tabs__panel" role="tabpanel" id="article-panel-general" aria-labelledby="article-tab-general" hidden={tab !== 'general'}>
        <div className="form__pair">
          {fields.articleCode}
          {fields.sheddingType}
        </div>
        {fields.name}
        <div className="form__pair">
          {fields.weaveName}
          {fields.composition}
        </div>
        <div className="form__pair">
          {fields.reedNumber}
          {fields.endsPerDent}
        </div>
        <div className="form__pair">
          {fields.reedWidthCm}
          {fields.weftDensityPerCm}
        </div>
        <div className="form__pair">
          {fields.rawWidthCm}
          {fields.rawWeightGsm}
        </div>
        <div className="form__pair">
          {fields.warpCrimpPercent}
          {fields.weftCrimpPercent}
        </div>
        <div className="form__pair">
          {fields.warpWastePercent}
          {fields.weftWastePercent}
        </div>
        {activeControl}
      </div>

      <div className="tabs__panel" role="tabpanel" id="article-panel-yarns" aria-labelledby="article-tab-yarns" hidden={tab !== 'yarns'}>
        {stored.isLoading && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={stored.error} />
        {sections.map((section) => {
          const sectionLines = lines.filter((line) => line.role === section.role)
          return (
            <fieldset key={section.role} className="form__section">
              <legend>
                {section.title}
                {section.role === 'WARP' && totalEnds > 0 && ` · toplam ${totalEnds.toLocaleString('tr-TR')} tel`}
              </legend>
              <div className="table-wrap recipe-table">
                <table className="table table--compact">
                  <thead>
                    <tr>
                      <th style={{ width: 56 }}>Sıra</th>
                      <th style={{ width: '60%' }}>İplik</th>
                      <th>{section.endsLabel}</th>
                      <th aria-label="Satırı kaldır" />
                    </tr>
                  </thead>
                  <tbody>
                    {sectionLines.map((line, index) => (
                      <tr key={line.key}>
                        <td>{index + 1}</td>
                        <td>
                          <LookupSelect
                            resource="dokuma-yarns"
                            label="İplik"
                            required
                            hideLabel
                            idSuffix={`article-yarn-${line.key}`}
                            value={line.yarnId}
                            disabled={disabled}
                            onChange={(yarnId) => update(line.key, { yarnId })}
                          />
                        </td>
                        <td>
                          <input aria-label={section.endsLabel} type="number" min={1} step={1} value={line.ends} disabled={disabled}
                            onChange={(event) => update(line.key, { ends: event.target.value })} />
                        </td>
                        <td className="recipe-table__remove-cell">
                          <button type="button" className="icon-button" aria-label="Satırı kaldır" title="Satırı kaldır" disabled={disabled}
                            onClick={() => setDraft(lines.filter((item) => item.key !== line.key))}>
                            <X aria-hidden="true" />
                          </button>
                        </td>
                      </tr>
                    ))}
                    {sectionLines.length === 0 && !stored.isLoading && (
                      <tr>
                        <td colSpan={4} className="table__empty">Kayıtlı iplik yok.</td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>
              <div>
                <button type="button" className="button" onClick={() => add(section.role)} disabled={disabled}>{section.addLabel}</button>
              </div>
            </fieldset>
          )
        })}
        <p className="field__hint">
          Çözgüde her ipliğin tel sayısını, atkıda desen raporundaki atkı sayısını girin (örn. 2 kırmızı, 1 mavi). Sıra, satırların sırasıdır.
        </p>
      </div>
    </div>
  )
}
