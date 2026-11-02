import { useState } from 'react'
import type { FormEvent } from 'react'
import { fieldErrors } from '../../api/envelope'
import { ErrorAlert, TextField } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { todayIsoDate } from '../../utils/format'
import { documentLanguageLabels } from '../../utils/documentText'
import type { DocumentLanguage } from '../../utils/documentText'
import { LOCAL_CURRENCY, useCurrencyOptions } from '../finance/currencyOptions'
import { LookupSelect } from '../masterData/LookupSelect'
import { useSaveQuotationMutation } from './quotationsApi'
import type { Quotation } from './quotationsApi'
import { draftOf, requestOf } from './quotationPresentation'

/** Bugünden <paramref name="days"/> gün sonrası (yyyy-aa-gg). */
function daysFromToday(days: number): string {
  const date = new Date()
  date.setDate(date.getDate() + days)
  const pad = (value: number) => String(value).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`
}

/**
 * Teklif başlığı: müşteri (ya da aday müşteri adı), tarih, geçerlilik, para birimi, belge dili ve not. Yeni teklif
 * satırsız kaydedilir; satırlar teklif sayfasında maliyet dökümüyle eklenir.
 */
export function QuotationFormDialog({
  quotation, onClose, onSaved,
}: { quotation?: Quotation; onClose: () => void; onSaved?: (quotation: Quotation) => void }) {
  const [save, { isLoading, error }] = useSaveQuotationMutation()
  const [prospect, setProspect] = useState(quotation ? quotation.customerId === null : false)
  const [customerId, setCustomerId] = useState<number | null>(quotation?.customerId ?? null)
  const [prospectName, setProspectName] = useState(quotation?.prospectName ?? '')
  const [quotationDate, setQuotationDate] = useState(quotation?.quotationDate ?? todayIsoDate())
  const [validUntil, setValidUntil] = useState(quotation?.validUntil ?? daysFromToday(15))
  // Yeni teklifte boş bırakılırsa müşterinin para birimi ve belge dili kullanılır.
  const [currency, setCurrency] = useState(quotation?.currency ?? '')
  const [language, setLanguage] = useState<DocumentLanguage | ''>(quotation?.documentLanguage ?? '')
  const [notes, setNotes] = useState(quotation?.notes ?? '')
  const currencyOptions = useCurrencyOptions(quotation?.currency)

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await save({
      id: quotation?.id,
      body: {
        customerId: prospect ? null : customerId,
        prospectName: prospect ? prospectName.trim() || null : null,
        quotationDate,
        validUntil,
        currency: currency || null,
        documentLanguage: language || null,
        notes: notes.trim() || null,
        // Satırlar olduğu gibi korunur (maliyetler kayıtta yeniden hesaplanır).
        lines: (quotation?.lines ?? []).map((line) => requestOf(draftOf(line))),
        rowVersion: quotation?.rowVersion,
      },
    })
    if ('data' in result && result.data) {
      onSaved?.(result.data)
      onClose()
    }
  }

  return (
    <Modal title={quotation ? `Teklifi düzenle: ${quotation.quotationNo}` : 'Yeni teklif'} onClose={onClose}>
      <form className="form" onSubmit={(event) => void handleSubmit(event)} noValidate>
        <fieldset className="form__section">
          <legend>Müşteri</legend>
          <label className="checkbox">
            <input type="checkbox" checked={prospect} onChange={(event) => setProspect(event.target.checked)} />
            Müşteri kartı yok (aday müşteri)
          </label>
          {prospect ? (
            <TextField
              label="Aday müşteri adı"
              value={prospectName}
              maxLength={200}
              onChange={(event) => setProspectName(event.target.value)}
              errors={fieldErrors(error, 'customerId') ?? fieldErrors(error, 'prospectName')}
              hint="Kabulde siparişin açılacağı müşteri kartı seçilir."
            />
          ) : (
            <LookupSelect resource="customers" label="Müşteri" value={customerId} onChange={setCustomerId} required errors={fieldErrors(error, 'customerId')} />
          )}
        </fieldset>
        <fieldset className="form__section">
          <legend>Teklif</legend>
          <div className="form__pair">
            <TextField label="Teklif tarihi" type="date" value={quotationDate} onChange={(event) => setQuotationDate(event.target.value)} errors={fieldErrors(error, 'quotationDate')} />
            <TextField label="Geçerlilik" type="date" min={quotationDate} value={validUntil} onChange={(event) => setValidUntil(event.target.value)} errors={fieldErrors(error, 'validUntil')} />
          </div>
          <div className="form__pair">
            <label className="field">
              <span>Para birimi</span>
              <select value={currency} onChange={(event) => setCurrency(event.target.value)} aria-invalid={Boolean(fieldErrors(error, 'currency'))}>
                {!quotation && <option value="">Müşterinin para birimi ({prospect ? LOCAL_CURRENCY : 'kartından'})</option>}
                {currencyOptions.map((option) => (
                  <option key={option.value} value={option.value}>{option.label}</option>
                ))}
              </select>
              {quotation && <small className="field__hint">Değiştirilirse maliyetler yeni para birimine çevrilir.</small>}
              {fieldErrors(error, 'currency')?.map((message) => <small key={message} className="field__error">{message}</small>)}
            </label>
            <label className="field">
              <span>Belge dili</span>
              <select value={language} onChange={(event) => setLanguage(event.target.value as DocumentLanguage | '')}>
                <option value="">Müşteri kartındaki</option>
                {(Object.keys(documentLanguageLabels) as DocumentLanguage[]).map((key) => (
                  <option key={key} value={key}>{documentLanguageLabels[key]}</option>
                ))}
              </select>
            </label>
          </div>
          <label className="field">
            <span>Not (teklifte basılır: ödeme, teslim koşulları)</span>
            <textarea rows={3} maxLength={2000} value={notes} onChange={(event) => setNotes(event.target.value)} />
          </label>
        </fieldset>
        <ErrorAlert error={error} fields={['customerId', 'prospectName', 'quotationDate', 'validUntil', 'currency']} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading}>
            {isLoading ? 'Kaydediliyor…' : 'Kaydet'}
          </button>
        </footer>
      </form>
    </Modal>
  )
}
