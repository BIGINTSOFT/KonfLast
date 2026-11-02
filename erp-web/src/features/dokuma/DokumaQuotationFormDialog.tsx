import { Calculator } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { ErrorAlert } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { documentLanguageLabels } from '../../utils/documentText'
import type { DocumentLanguage } from '../../utils/documentText'
import { formatDate, formatQuantity, todayIsoDate } from '../../utils/format'
import { formatMoney, formatRate } from '../../utils/money'
import { useCurrencyOptions } from '../finance/currencyOptions'
import { LookupSelect } from '../masterData/LookupSelect'
import { QuotationLineBreakdown } from './DokumaQuotationBreakdown'
import { dokumaOrderOwnershipLabels } from './dokumaOrdersApi'
import type { DokumaOrderOwnership } from './dokumaOrdersApi'
import { useCalculateDokumaQuotationMutation, useSaveDokumaQuotationMutation } from './dokumaQuotationsApi'
import type { DokumaQuotation, DokumaQuotationCalculation, DokumaQuotationLine, DokumaQuotationRequest } from './dokumaQuotationsApi'

/** Hesap girdileri: boş bırakılan alan varsayılandan (tezgâh, artikel, maliyet parametreleri) alınır. */
const inputFields = [
  { key: 'speedRpm', label: 'Hız (devir/dk)', step: '1', from: (line: DokumaQuotationLine) => line.speedRpm },
  { key: 'efficiencyPercent', label: 'Verim (%)', step: '0.1', from: (line: DokumaQuotationLine) => line.efficiencyPercent },
  { key: 'picksPerCm', label: 'Atkı sıklığı (atkı/cm)', step: '0.1', from: (line: DokumaQuotationLine) => line.picksPerCm },
  { key: 'loomHourlyCost', label: 'Tezgâh saat maliyeti', step: '0.0001', from: (line: DokumaQuotationLine) => line.loomHourlyCost },
  { key: 'laborHourlyCost', label: 'İşçilik (tezgâh saati)', step: '0.0001', from: (line: DokumaQuotationLine) => line.laborHourlyCost },
  { key: 'warpingCostPerMeter', label: 'Çözgü (m çözgü)', step: '0.0001', from: (line: DokumaQuotationLine) => line.warpingCostPerWarpMeter },
  { key: 'sizingCostPerMeter', label: 'Haşıl (m çözgü)', step: '0.0001', from: (line: DokumaQuotationLine) => line.sizingCostPerWarpMeter },
  { key: 'wastePercent', label: 'Fire (%)', step: '0.01', from: (line: DokumaQuotationLine) => line.wastePercent },
  { key: 'overheadPercent', label: 'Genel gider (%)', step: '0.01', from: (line: DokumaQuotationLine) => line.overheadPercent },
  { key: 'marginPercent', label: 'Kâr marjı (%)', step: '0.01', from: (line: DokumaQuotationLine) => line.marginPercent },
] as const

type InputKey = (typeof inputFields)[number]['key']

interface LineDraft {
  key: number
  articleId: number | null
  meters: string
  loomId: number | null
  inputs: Record<InputKey, string>
  unitPrice: string
  /** İplik Id'sine göre elle girilen kilo fiyatı. */
  yarnPrices: Record<number, string>
  customerArticleCode: string
  notes: string
  showInputs: boolean
}

const emptyInputs = (): Record<InputKey, string> =>
  Object.fromEntries(inputFields.map((field) => [field.key, ''])) as Record<InputKey, string>

const emptyLine = (key: number): LineDraft => ({
  key, articleId: null, meters: '', loomId: null, inputs: emptyInputs(), unitPrice: '', yarnPrices: {}, customerArticleCode: '', notes: '', showInputs: false,
})

/** Kayıtlı satırın değerleri formda açıkça yazılı gelir; alan temizlenirse yeniden varsayılandan hesaplanır. */
const draftFrom = (line: DokumaQuotationLine): LineDraft => ({
  key: line.id ?? line.sequence,
  articleId: line.articleId,
  meters: String(line.meters),
  loomId: line.loomId,
  inputs: Object.fromEntries(inputFields.map((field) => [field.key, String(field.from(line))])) as Record<InputKey, string>,
  unitPrice: String(line.unitPrice),
  yarnPrices: Object.fromEntries(line.yarns.map((yarn) => [yarn.yarnId, String(yarn.unitPrice)])),
  customerArticleCode: line.customerArticleCode ?? '',
  notes: line.notes ?? '',
  showInputs: false,
})

const optional = (text: string) => (text.trim() === '' ? null : Number(text))

/**
 * Yeni teklif ya da taslak teklifin düzenlenmesi. "Hesapla" kaydetmeden ön maliyeti gösterir; boş bırakılan hesap girdilerinin yerine
 * hesapta kullanılan varsayılan değer soluk yazılır. Artikel ya da tezgâh değişince satırın hesap girdileri ve iplik fiyatları
 * varsayılana döner.
 */
export function DokumaQuotationFormDialog({ quotation, onClose, onSaved }: { quotation?: DokumaQuotation; onClose: () => void; onSaved: (id: number) => void }) {
  const [save, saveState] = useSaveDokumaQuotationMutation()
  const [calculate, calculateState] = useCalculateDokumaQuotationMutation()
  const [customerId, setCustomerId] = useState<number | null>(quotation?.customerId ?? null)
  const [prospectName, setProspectName] = useState(quotation?.prospectName ?? '')
  const [ownership, setOwnership] = useState<DokumaOrderOwnership>(quotation?.ownership ?? 'OWN_GOODS')
  const [quotationDate, setQuotationDate] = useState(quotation?.quotationDate ?? todayIsoDate())
  const [validUntil, setValidUntil] = useState(quotation?.validUntil ?? '')
  const [currency, setCurrency] = useState(quotation?.currency ?? '')
  const [language, setLanguage] = useState<DocumentLanguage | ''>(quotation?.documentLanguage ?? '')
  const [contactName, setContactName] = useState(quotation?.contactName ?? '')
  const [paymentTermDays, setPaymentTermDays] = useState(quotation?.paymentTermDays === null || quotation?.paymentTermDays === undefined ? '' : String(quotation.paymentTermDays))
  const [deliveryTerms, setDeliveryTerms] = useState(quotation?.deliveryTerms ?? '')
  const [notes, setNotes] = useState(quotation?.notes ?? '')
  const currencies = useCurrencyOptions(quotation?.currency)
  const [nextKey, setNextKey] = useState(-1)
  const [lines, setLines] = useState<LineDraft[]>(quotation ? quotation.lines.map(draftFrom) : [emptyLine(0)])
  const [preview, setPreview] = useState<DokumaQuotationCalculation | null>(null)
  const [stale, setStale] = useState(false)

  const changed = () => setStale(true)

  const update = (key: number, change: Partial<LineDraft>) => {
    setLines(lines.map((line) => (line.key === key ? { ...line, ...change } : line)))
    changed()
  }

  const updateInput = (key: number, field: InputKey, value: string) => {
    setLines(lines.map((line) => (line.key === key ? { ...line, inputs: { ...line.inputs, [field]: value } } : line)))
    changed()
  }

  const addLine = () => {
    setLines([...lines, emptyLine(nextKey)])
    setNextKey(nextKey - 1)
    changed()
  }

  const body = (): DokumaQuotationRequest => ({
    customerId,
    prospectName: customerId === null ? prospectName.trim() || null : null,
    quotationDate,
    validUntil: validUntil || null,
    ownership,
    currency: currency || null,
    documentLanguage: language || null,
    contactName: contactName.trim() || null,
    paymentTermDays: optional(paymentTermDays),
    deliveryTerms: deliveryTerms.trim() || null,
    notes: notes.trim() || null,
    lines: lines.map((line) => ({
      articleId: line.articleId ?? 0,
      meters: Number(line.meters),
      loomId: line.loomId,
      speedRpm: optional(line.inputs.speedRpm),
      efficiencyPercent: optional(line.inputs.efficiencyPercent),
      picksPerCm: optional(line.inputs.picksPerCm),
      loomHourlyCost: optional(line.inputs.loomHourlyCost),
      laborHourlyCost: optional(line.inputs.laborHourlyCost),
      warpingCostPerMeter: optional(line.inputs.warpingCostPerMeter),
      sizingCostPerMeter: optional(line.inputs.sizingCostPerMeter),
      wastePercent: optional(line.inputs.wastePercent),
      overheadPercent: optional(line.inputs.overheadPercent),
      marginPercent: optional(line.inputs.marginPercent),
      unitPrice: optional(line.unitPrice),
      yarnPrices: Object.entries(line.yarnPrices)
        .filter(([, price]) => price.trim() !== '')
        .map(([yarnId, price]) => ({ yarnId: Number(yarnId), unitPrice: Number(price) })),
      customerArticleCode: line.customerArticleCode.trim() || null,
      notes: line.notes.trim() || null,
    })),
  })

  const handleCalculate = async () => {
    const result = await calculate(body())
    if ('data' in result && result.data) {
      setPreview(result.data)
      setStale(false)
    }
  }

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await save({ id: quotation?.id, body: body() })
    if ('data' in result && result.data) {
      onSaved(result.data.id)
    }
  }

  // Önizleme yalnızca güncelse yer tutucu olarak kullanılır; satır sırası istekteki sırayla aynıdır.
  const previewLine = (index: number) => (preview && !stale ? preview.lines[index] ?? null : null)
  const shownCurrency = preview?.currency ?? (currency || quotation?.currency || '')

  return (
    <Modal title={quotation ? `Teklif düzenle: ${quotation.quotationNo}` : 'Yeni teklif'} onClose={onClose} variant="drawer">
      <form className="form" onSubmit={handleSubmit}>
        <LookupSelect
          resource="dokuma-customers"
          label="Müşteri (kartı yoksa boş bırakıp aday firma adını yazın)"
          value={customerId}
          onChange={(value) => { setCustomerId(value); changed() }}
        />
        {customerId === null && (
          <label className="field">
            <span>Aday firma</span>
            <input required maxLength={200} value={prospectName} onChange={(event) => setProspectName(event.target.value)} />
          </label>
        )}
        <div className="form__pair">
          <label className="field">
            <span>İş türü</span>
            <select value={ownership} onChange={(event) => { setOwnership(event.target.value as DokumaOrderOwnership); changed() }}>
              {Object.entries(dokumaOrderOwnershipLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
            </select>
          </label>
          <label className="field">
            <span>İlgili kişi</span>
            <input maxLength={100} value={contactName} onChange={(event) => setContactName(event.target.value)} />
          </label>
        </div>
        <div className="form__pair">
          <label className="field">
            <span>Teklif tarihi</span>
            <input type="date" required value={quotationDate} onChange={(event) => { setQuotationDate(event.target.value); changed() }} />
          </label>
          <label className="field">
            <span>Geçerlilik tarihi</span>
            <input type="date" value={validUntil} title={preview ? `Boşsa ${formatDate(preview.validUntil)}` : 'Boşsa parametredeki geçerlilik süresi'} onChange={(event) => setValidUntil(event.target.value)} />
          </label>
        </div>
        <div className="form__pair">
          <label className="field">
            <span>Para birimi</span>
            <select value={currency} onChange={(event) => { setCurrency(event.target.value); changed() }}>
              <option value="">{customerId === null ? 'Varsayılan (TRY)' : 'Müşterinin para birimi'}</option>
              {currencies.map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}
            </select>
          </label>
          <label className="field">
            <span>Belge dili</span>
            <select value={language} onChange={(event) => setLanguage(event.target.value as DocumentLanguage | '')}>
              <option value="">{customerId === null ? 'Varsayılan (Türkçe)' : 'Müşterinin belge dili'}</option>
              {Object.entries(documentLanguageLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
            </select>
          </label>
        </div>
        <div className="form__pair">
          <label className="field">
            <span>Ödeme vadesi (gün)</span>
            <input type="number" min="0" max="365" step="1" placeholder={customerId === null ? '' : 'Müşteri kartından'} value={paymentTermDays} onChange={(event) => setPaymentTermDays(event.target.value)} />
          </label>
          <label className="field">
            <span>Teslim süresi</span>
            <input maxLength={200} placeholder="Örn. siparişten itibaren 4 hafta" value={deliveryTerms} onChange={(event) => setDeliveryTerms(event.target.value)} />
          </label>
        </div>
        <label className="field">
          <span>Not (teklife basılır)</span>
          <textarea rows={2} maxLength={1000} value={notes} onChange={(event) => setNotes(event.target.value)} />
        </label>

        {lines.map((line, index) => {
          const calculated = previewLine(index)
          return (
            <fieldset key={line.key} className="form__section">
              <legend>{index + 1}. satır</legend>
              <LookupSelect
                resource="dokuma-articles"
                label="Artikel"
                required
                idSuffix={`a${line.key}`}
                value={line.articleId}
                onChange={(value) => update(line.key, { articleId: value, inputs: emptyInputs(), yarnPrices: {} })}
              />
              <div className="form__pair">
                <label className="field">
                  <span>Metre</span>
                  <input type="number" min="0" step="0.01" required value={line.meters} onChange={(event) => update(line.key, { meters: event.target.value })} />
                </label>
                <LookupSelect
                  resource="dokuma-looms"
                  label="Tezgâh (hız, verim ve saat maliyeti)"
                  idSuffix={`l${line.key}`}
                  value={line.loomId}
                  onChange={(value) => update(line.key, {
                    loomId: value,
                    inputs: { ...line.inputs, speedRpm: '', efficiencyPercent: '', loomHourlyCost: '' },
                  })}
                />
              </div>
              <div className="form__pair">
                <label className="field">
                  <span>Teklif fiyatı (metre)</span>
                  <input
                    type="number"
                    min="0"
                    step="0.0001"
                    placeholder={calculated ? `Hesaplanan: ${formatRate(calculated.calculatedPrice, shownCurrency)}` : 'Boşsa hesaplanan fiyat'}
                    value={line.unitPrice}
                    onChange={(event) => update(line.key, { unitPrice: event.target.value })}
                  />
                </label>
                <label className="field">
                  <span>Müşteri artikel kodu</span>
                  <input maxLength={50} value={line.customerArticleCode} onChange={(event) => update(line.key, { customerArticleCode: event.target.value })} />
                </label>
              </div>
              <label className="field">
                <span>Satır notu (teklife basılır)</span>
                <input maxLength={500} value={line.notes} onChange={(event) => update(line.key, { notes: event.target.value })} />
              </label>

              <div>
                <button type="button" className="button" aria-expanded={line.showInputs} onClick={() => setLines(lines.map((item) => (item.key === line.key ? { ...item, showInputs: !item.showInputs } : item)))}>
                  {line.showInputs ? 'Hesap girdilerini gizle' : 'Hesap girdileri ve iplik fiyatları'}
                </button>
              </div>
              {line.showInputs && (
                <>
                  <p className="muted">Boş alan varsayılandan alınır; yer tutucuda son hesaplamada kullanılan değer görünür. Tutarlar teklifin para birimindedir.</p>
                  <div className="form__pair" style={{ gridTemplateColumns: 'repeat(auto-fill, minmax(150px, 1fr))' }}>
                    {inputFields.map((field) => (
                      <label key={field.key} className="field">
                        <span>{field.label}</span>
                        <input
                          type="number"
                          min="0"
                          step={field.step}
                          placeholder={calculated ? formatQuantity(field.from(calculated)) : 'Varsayılan'}
                          value={line.inputs[field.key]}
                          onChange={(event) => updateInput(line.key, field.key, event.target.value)}
                        />
                      </label>
                    ))}
                  </div>
                  {calculated && calculated.yarns.length > 0 && (
                    <div className="form__pair" style={{ gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))' }}>
                      {calculated.yarns.map((yarn) => (
                        <label key={yarn.yarnId} className="field">
                          <span>{yarn.yarnCode} kilo fiyatı</span>
                          <input
                            type="number"
                            min="0"
                            step="0.0001"
                            placeholder={formatQuantity(yarn.unitPrice)}
                            title={yarn.priceSource ?? 'Alış fiyatı bulunamadı'}
                            value={line.yarnPrices[yarn.yarnId] ?? ''}
                            onChange={(event) => update(line.key, { yarnPrices: { ...line.yarnPrices, [yarn.yarnId]: event.target.value } })}
                          />
                        </label>
                      ))}
                    </div>
                  )}
                  {!calculated && <p className="muted">İplik fiyatlarını görmek için hesaplayın.</p>}
                </>
              )}

              {calculated && (
                <>
                  {calculated.warnings.map((warning) => <p key={warning} className="alert alert--info">{warning}</p>)}
                  <QuotationLineBreakdown line={calculated} currency={shownCurrency} ownership={ownership} />
                </>
              )}

              {lines.length > 1 && (
                <div>
                  <button type="button" className="button button--danger-ghost" onClick={() => { setLines(lines.filter((item) => item.key !== line.key)); changed() }}>Satırı kaldır</button>
                </div>
              )}
            </fieldset>
          )
        })}
        <div>
          <button type="button" className="button" onClick={addLine}>Satır ekle</button>
        </div>

        {preview && (
          <p className={stale ? 'alert alert--info' : 'muted'}>
            {stale
              ? 'Form değişti; güncel döküm için yeniden hesaplayın.'
              : `Toplam ${formatQuantity(preview.totalMeters)} m · maliyet ${formatMoney(preview.totalCost, preview.currency)} · teklif ${formatMoney(preview.totalAmount, preview.currency)}`
                + (preview.costCurrency !== preview.currency ? ` · maliyet parametreleri ${preview.costCurrency}, kur ${formatQuantity(preview.exchangeRate)}` : '')}
          </p>
        )}

        <ErrorAlert error={calculateState.error ?? saveState.error} />
        <footer className="form__actions">
          <button type="button" className="button" disabled={calculateState.isLoading} onClick={handleCalculate}>
            <Calculator aria-hidden="true" />
            Hesapla
          </button>
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={saveState.isLoading}>Kaydet</button>
        </footer>
      </form>
    </Modal>
  )
}
