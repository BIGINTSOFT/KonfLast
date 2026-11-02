import { Calculator, Check, Pencil, Plus, Printer, RefreshCw, RotateCcw, Search, Send, Trash2, X, XCircle } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { ClearFiltersButton } from '../../components/grid/ClearFiltersButton'
import { GridHead } from '../../components/grid/GridHead'
import type { GridColumn } from '../../components/grid/GridHead'
import { useGrid } from '../../components/grid/useGrid'
import { Modal } from '../../components/Modal'
import { Pagination } from '../../components/Pagination'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { documentLanguageLabels, documentTexts } from '../../utils/documentText'
import type { DocumentLanguage } from '../../utils/documentText'
import { formatDate, formatQuantity, todayIsoDate } from '../../utils/format'
import { formatMoney, formatRate } from '../../utils/money'
import { useCurrencyOptions } from '../finance/currencyOptions'
import { LookupSelect } from '../masterData/LookupSelect'
import { ormeOrderOwnershipLabels, ormeOrderStatusPresentation } from './ormeOrdersApi'
import {
  ormeQuotationCost,
  ormeQuotationMarginFor,
  ormeQuotationPriceSourceLabels,
  ormeQuotationStatusPresentation,
  useAcceptOrmeQuotationMutation,
  useDeleteOrmeQuotationLineMutation,
  useDeleteOrmeQuotationMutation,
  useGetOrmeQuotationPrintQuery,
  useGetOrmeQuotationQuery,
  useGetOrmeQuotationsQuery,
  useLazyGetOrmeQuotationCostDefaultsQuery,
  useRejectOrmeQuotationMutation,
  useReopenOrmeQuotationMutation,
  useSaveOrmeQuotationLineMutation,
  useSaveOrmeQuotationMutation,
  useSendOrmeQuotationMutation,
} from './ormeQuotationsApi'
import type { OrmeQuotation, OrmeQuotationCostDefaults, OrmeQuotationLine, OrmeQuotationPriceSource } from './ormeQuotationsApi'

const PAGE_SIZE = 20

/** Varsayılan geçerlilik: teklif tarihinden 15 gün sonra. */
const VALIDITY_DAYS = 15

function addDays(isoDate: string, days: number): string {
  const date = new Date(`${isoDate}T00:00:00`)
  date.setDate(date.getDate() + days)
  const pad = (value: number) => String(value).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`
}

/** Sayı kutusundaki metin; boş ya da geçersizse sıfır. */
const numberOf = (text: string) => {
  const value = Number(text.replace(',', '.'))
  return Number.isFinite(value) ? value : 0
}

const percent = (value: number | null) => (value === null ? '—' : `%${formatQuantity(value)}`)

/** Para birimi sütun başlığında yazılı kilo tutarı (2-4 ondalık). */
const perKg = (value: number) => formatRate(value, '').trim()

const partyName = (quotation: { customerName: string | null; prospectName: string | null }) =>
  quotation.customerName ?? quotation.prospectName ?? '—'

/* ───────── Teklif başlığı ───────── */

/** Yeni teklif ya da taslak teklifin başlığı. Satır varken para birimi değiştirilemez (maliyetler o para birimindedir). */
function OrmeQuotationFormDialog({ quotation, onClose, onSaved }: { quotation?: OrmeQuotation; onClose: () => void; onSaved: (id: number) => void }) {
  const [save, { isLoading, error }] = useSaveOrmeQuotationMutation()
  const today = todayIsoDate()
  const [customerId, setCustomerId] = useState<number | null>(quotation?.customerId ?? null)
  const [prospectName, setProspectName] = useState(quotation?.prospectName ?? '')
  const [quotationDate, setQuotationDate] = useState(quotation?.quotationDate ?? today)
  const [validUntil, setValidUntil] = useState(quotation?.validUntil ?? addDays(today, VALIDITY_DAYS))
  const [currency, setCurrency] = useState(quotation?.currency ?? '')
  const [language, setLanguage] = useState<DocumentLanguage | ''>(quotation?.documentLanguage ?? '')
  const [notes, setNotes] = useState(quotation?.notes ?? '')
  const currencies = useCurrencyOptions(quotation?.currency)
  const hasLines = (quotation?.lines.length ?? 0) > 0

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await save({
      id: quotation?.id,
      body: {
        customerId,
        prospectName: customerId === null ? prospectName.trim() || null : null,
        quotationDate,
        validUntil,
        currency: currency || null,
        documentLanguage: language || null,
        notes: notes.trim() || null,
      },
    })
    if ('data' in result && result.data) {
      onSaved(result.data.id)
    }
  }

  return (
    <Modal title={quotation ? `Teklif düzenle: ${quotation.quotationNo}` : 'Yeni teklif'} onClose={onClose}>
      <form className="form" onSubmit={handleSubmit}>
        <LookupSelect resource="orme-customers" label="Müşteri (boşsa aday firma)" value={customerId} onChange={setCustomerId} />
        {customerId === null && (
          <label className="field">
            <span>Aday firma adı</span>
            <input required maxLength={200} value={prospectName} onChange={(event) => setProspectName(event.target.value)} />
          </label>
        )}
        <div className="form__pair">
          <label className="field">
            <span>Teklif tarihi</span>
            <input type="date" required value={quotationDate} onChange={(event) => setQuotationDate(event.target.value)} />
          </label>
          <label className="field">
            <span>Geçerlilik tarihi</span>
            <input type="date" required min={quotationDate} value={validUntil} onChange={(event) => setValidUntil(event.target.value)} />
          </label>
        </div>
        <div className="form__pair">
          <label className="field">
            <span>Para birimi</span>
            <select value={currency} disabled={hasLines} onChange={(event) => setCurrency(event.target.value)}>
              {!quotation && <option value="">{customerId === null ? 'Varsayılan (TRY)' : 'Müşterinin para birimi'}</option>}
              {currencies.map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}
            </select>
            {hasLines && <span className="field__hint">Satır varken değiştirilemez.</span>}
          </label>
          <label className="field">
            <span>Belge dili</span>
            <select value={language} onChange={(event) => setLanguage(event.target.value as DocumentLanguage | '')}>
              <option value="">{customerId === null ? 'Türkçe (varsayılan)' : 'Müşterinin belge dili'}</option>
              {Object.entries(documentLanguageLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
            </select>
          </label>
        </div>
        <label className="field">
          <span>Not (belgede basılır)</span>
          <textarea rows={3} maxLength={1000} value={notes} onChange={(event) => setNotes(event.target.value)} />
        </label>
        <ErrorAlert error={error} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading}>Kaydet</button>
        </footer>
      </form>
    </Modal>
  )
}

/* ───────── Satır ve ön maliyet ───────── */

interface YarnDraft {
  yarnId: number
  yarnCode: string
  yarnName: string
  yarnColor: string | null
  percent: number
  price: string
  source: OrmeQuotationPriceSource
  reference: string | null
}

const priceText = (value: number | null) => (value === null ? '' : String(value))

/**
 * Teklif satırı ve ön maliyet. Kumaş ya da makine seçilince varsayılan girdiler (fire, kg/saat, saat maliyetleri, son
 * alış fiyatları) yüklenir; her girdi değiştirilebilir ve maliyet anlık hesaplanır. Fiyat elle girilirse kâr fiyattan,
 * kâr girilirse fiyat kârdan hesaplanır. Teklif taslak değilse ya da yetki yoksa pencere yalnız denemedir, kaydedilmez.
 */
function OrmeQuotationLineDialog({ quotation, line, readOnly, onClose }: {
  quotation: OrmeQuotation
  line?: OrmeQuotationLine
  readOnly: boolean
  onClose: () => void
}) {
  const [save, { isLoading, error }] = useSaveOrmeQuotationLineMutation()
  const [loadDefaults, defaultsState] = useLazyGetOrmeQuotationCostDefaultsQuery()
  const currency = quotation.currency
  const [fabricId, setFabricId] = useState<number | null>(line?.fabricId ?? null)
  const [machineId, setMachineId] = useState<number | null>(line?.machineId ?? null)
  const [kilograms, setKilograms] = useState(line ? String(line.kilograms) : '')
  const [isCommission, setIsCommission] = useState(line?.isCommission ?? false)
  const [waste, setWaste] = useState(line ? String(line.wastePercent) : '')
  const [kilogramsPerHour, setKilogramsPerHour] = useState(line === undefined || line.kilogramsPerHour === null ? '' : String(line.kilogramsPerHour))
  const [machineCost, setMachineCost] = useState(line ? String(line.machineHourlyCost) : '')
  const [laborCost, setLaborCost] = useState(line ? String(line.laborHourlyCost) : '')
  const [overhead, setOverhead] = useState(line ? String(line.overheadPercent) : '')
  const [margin, setMargin] = useState(line ? String(line.marginPercent) : '')
  // Fiyat elle girildiyse kâr fiyattan hesaplanır; aksi halde fiyat kârdan.
  const [manualPrice, setManualPrice] = useState<string | null>(line && line.unitPrice !== line.calculatedPrice ? String(line.unitPrice) : null)
  const [customerFabricCode, setCustomerFabricCode] = useState(line?.customerFabricCode ?? '')
  const [notes, setNotes] = useState(line?.notes ?? '')
  const [yarns, setYarns] = useState<YarnDraft[]>(line
    ? line.yarns.map((yarn) => ({
      yarnId: yarn.yarnId,
      yarnCode: yarn.yarnCode,
      yarnName: yarn.yarnName,
      yarnColor: yarn.yarnColor,
      percent: yarn.percent,
      price: priceText(yarn.unitPrice),
      source: yarn.priceSource,
      reference: yarn.priceReference,
    }))
    : [])
  const [defaults, setDefaults] = useState<OrmeQuotationCostDefaults | null>(null)

  const fetchDefaults = async (nextFabricId: number | null, nextMachineId: number | null, scope: 'all' | 'machine') => {
    if (nextFabricId === null) {
      return
    }
    const result = await loadDefaults({ fabricId: nextFabricId, machineId: nextMachineId, currency, date: quotation.quotationDate })
    const data = result.data
    if (!data) {
      return
    }
    setDefaults(data)
    setMachineCost(String(data.machineHourlyCost))
    if (scope === 'machine') {
      return
    }
    setWaste(String(data.wastePercent))
    setKilogramsPerHour(data.kilogramsPerHour === null ? '' : String(data.kilogramsPerHour))
    setLaborCost(String(data.laborHourlyCost))
    setOverhead(String(data.overheadPercent))
    setMargin(String(data.marginPercent))
    setManualPrice(null)
    setYarns(data.yarns.map((yarn) => ({
      yarnId: yarn.yarnId,
      yarnCode: yarn.yarnCode,
      yarnName: yarn.yarnName,
      yarnColor: yarn.yarnColor,
      percent: yarn.percent,
      price: priceText(yarn.unitPrice),
      source: yarn.priceSource,
      reference: yarn.priceReference,
    })))
    if (!data.hasRecipe) {
      setIsCommission(true)
    }
  }

  const cost = ormeQuotationCost({
    isCommission,
    yarns: yarns.map((yarn) => ({ percent: yarn.percent, unitPrice: yarn.price.trim() === '' ? null : numberOf(yarn.price) })),
    wastePercent: numberOf(waste),
    kilogramsPerHour: kilogramsPerHour.trim() === '' ? null : numberOf(kilogramsPerHour),
    machineHourlyCost: numberOf(machineCost),
    laborHourlyCost: numberOf(laborCost),
    overheadPercent: numberOf(overhead),
    marginPercent: numberOf(margin),
  })
  const unitPrice = manualPrice === null ? cost.calculatedPrice : numberOf(manualPrice)
  const effectiveMargin = manualPrice === null ? numberOf(margin) : ormeQuotationMarginFor(cost.totalCost, unitPrice)
  const kg = numberOf(kilograms)
  const amount = Math.round(kg * unitPrice * 100) / 100
  const costAmount = Math.round(kg * cost.totalCost * 100) / 100
  const wasteFactor = 1 + numberOf(waste) / 100
  const missingYarnPrice = !isCommission && (yarns.length === 0 || yarns.some((yarn) => yarn.price.trim() === ''))
  const missingSpeed = kilogramsPerHour.trim() === ''

  const updateYarn = (yarnId: number, price: string) =>
    setYarns(yarns.map((yarn) => (yarn.yarnId === yarnId ? { ...yarn, price, source: 'MANUAL', reference: null } : yarn)))

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    if (readOnly || fabricId === null) {
      return
    }
    const result = await save({
      id: quotation.id,
      lineId: line?.id,
      body: {
        fabricId,
        kilograms: kg,
        isCommission,
        machineId,
        wastePercent: numberOf(waste),
        kilogramsPerHour: kilogramsPerHour.trim() === '' ? null : numberOf(kilogramsPerHour),
        machineHourlyCost: numberOf(machineCost),
        laborHourlyCost: numberOf(laborCost),
        overheadPercent: numberOf(overhead),
        marginPercent: effectiveMargin ?? numberOf(margin),
        unitPrice: manualPrice === null ? null : numberOf(manualPrice),
        customerFabricCode: customerFabricCode.trim() || null,
        notes: notes.trim() || null,
        yarns: isCommission
          ? []
          : yarns.map((yarn) => ({
            yarnId: yarn.yarnId,
            unitPrice: yarn.price.trim() === '' ? null : numberOf(yarn.price),
            priceSource: yarn.source,
            priceReference: yarn.reference,
          })),
      },
    })
    if ('data' in result && result.data) {
      onClose()
    }
  }

  const title = line ? `${line.sequence}. satır: ${line.fabricCode}` : 'Yeni satır'

  return (
    <Modal title={readOnly ? `${title} (deneme)` : title} onClose={onClose} variant="drawer">
      <form className="form" onSubmit={handleSubmit}>
        {readOnly && (
          <p className="alert alert--info">
            Teklif taslak olmadığı ya da yetkiniz olmadığı için değişiklikler kaydedilmez; girdileri değiştirip sonucu deneyebilirsiniz.
          </p>
        )}
        <LookupSelect
          resource="orme-fabrics"
          label="Kumaş"
          required
          value={fabricId}
          onChange={(value) => {
            setFabricId(value)
            void fetchDefaults(value, machineId, 'all')
          }}
        />
        <div className="form__pair">
          <label className="field">
            <span>Kilo</span>
            <input type="number" min="0" step="0.01" required value={kilograms} onChange={(event) => setKilograms(event.target.value)} />
          </label>
          <LookupSelect
            resource="orme-machines"
            label="Makine (saat maliyeti için)"
            value={machineId}
            onChange={(value) => {
              setMachineId(value)
              void fetchDefaults(fabricId, value, 'machine')
            }}
          />
        </div>
        <label className="checkbox">
          <input
            type="checkbox"
            checked={isCommission}
            onChange={(event) => {
              setIsCommission(event.target.checked)
              if (!event.target.checked && yarns.length === 0) {
                void fetchDefaults(fabricId, machineId, 'all')
              }
            }}
          />
          <span>Fason: iplik müşteriden gelir, fiyat yalnızca örme ücretidir</span>
        </label>
        <div>
          <button type="button" className="button" disabled={fabricId === null || defaultsState.isFetching} onClick={() => void fetchDefaults(fabricId, machineId, 'all')}>
            <RefreshCw aria-hidden="true" />
            Güncel maliyetleri yükle
          </button>
        </div>
        <ErrorAlert error={defaultsState.error} />
        {defaults?.missingRate && (
          <p className="alert alert--info">Kur bulunamadığı için bazı tutarlar {currency} para birimine çevrilemedi; ilgili girdileri elle doldurun.</p>
        )}
        {defaults && !defaults.hasRecipe && (
          <p className="alert alert--info">Kumaş kartında iplik tanımlı değil; yalnız fason teklif verilebilir.</p>
        )}

        {!isCommission && (
          <fieldset className="form__section">
            <legend>İplik maliyeti ({currency}/kg)</legend>
            <div className="table-wrap">
              <table className="table table--compact">
                <thead>
                  <tr>
                    <th>İplik</th>
                    <th className="table__number">Oran</th>
                    <th>Fiyat kaynağı</th>
                    <th className="table__number">Kilo fiyatı</th>
                    <th className="table__number">Kumaş kg'ına (fireli)</th>
                  </tr>
                </thead>
                <tbody>
                  {yarns.map((yarn) => (
                    <tr key={yarn.yarnId}>
                      <td>{yarn.yarnCode} — {yarn.yarnName}{yarn.yarnColor && <span className="muted"> ({yarn.yarnColor})</span>}</td>
                      <td className="table__number">%{formatQuantity(yarn.percent)}</td>
                      <td>
                        {ormeQuotationPriceSourceLabels[yarn.source]}
                        {yarn.reference && <div className="muted">{yarn.reference}</div>}
                      </td>
                      <td className="table__number">
                        <input aria-label={`${yarn.yarnCode} kilo fiyatı`} className="matrix-input purchase-input" type="number" min="0" step="0.0001"
                          value={yarn.price} onChange={(event) => updateYarn(yarn.yarnId, event.target.value)} />
                      </td>
                      <td className="table__number">
                        {yarn.price.trim() === '' ? '—' : formatRate((yarn.percent / 100) * numberOf(yarn.price) * wasteFactor, currency)}
                      </td>
                    </tr>
                  ))}
                  {yarns.length === 0 && <tr><td colSpan={5} className="table__empty">Kumaş seçildiğinde reçetedeki iplikler gelir.</td></tr>}
                </tbody>
              </table>
            </div>
          </fieldset>
        )}

        <fieldset className="form__section">
          <legend>Maliyet girdileri</legend>
          <div className="form__pair">
            <label className="field">
              <span>İplik firesi (%)</span>
              <input type="number" min="0" max="100" step="0.01" required value={waste} onChange={(event) => setWaste(event.target.value)} />
            </label>
            <label className="field">
              <span>Saatte örülen (kg)</span>
              <input type="number" min="0" step="0.01" value={kilogramsPerHour} onChange={(event) => setKilogramsPerHour(event.target.value)} />
              {missingSpeed && <span className="field__hint">Kumaş kartında beklenen üretim yok; girilmezse örme maliyeti hesaplanmaz.</span>}
            </label>
          </div>
          <div className="form__pair">
            <label className="field">
              <span>Makine saat maliyeti ({currency})</span>
              <input type="number" min="0" step="0.0001" required value={machineCost} onChange={(event) => setMachineCost(event.target.value)} />
            </label>
            <label className="field">
              <span>İşçilik saat maliyeti ({currency})</span>
              <input type="number" min="0" step="0.0001" required value={laborCost} onChange={(event) => setLaborCost(event.target.value)} />
            </label>
          </div>
          <div className="form__pair">
            <label className="field">
              <span>Genel gider (%)</span>
              <input type="number" min="0" max="1000" step="0.01" required value={overhead} onChange={(event) => setOverhead(event.target.value)} />
            </label>
            <label className="field">
              <span>Kâr (maliyet üzerine %)</span>
              <input
                type="number"
                min="-100"
                step="0.01"
                required
                value={manualPrice === null ? margin : (effectiveMargin === null ? margin : String(effectiveMargin))}
                onChange={(event) => {
                  setMargin(event.target.value)
                  setManualPrice(null)
                }}
              />
            </label>
          </div>
          <div className="form__pair">
            <label className="field">
              <span>Kilo fiyatı ({currency})</span>
              <input
                type="number"
                min="0"
                step="0.0001"
                required
                value={manualPrice ?? String(cost.calculatedPrice)}
                onChange={(event) => setManualPrice(event.target.value)}
              />
              <span className="field__hint">{manualPrice === null ? 'Maliyet ve kârdan hesaplandı.' : 'Elle girildi; kâr fiyattan hesaplanır.'}</span>
            </label>
            {manualPrice !== null && (
              <div className="field">
                <span>&nbsp;</span>
                <button type="button" className="button" onClick={() => setManualPrice(null)}><Calculator aria-hidden="true" />Hesaplanan fiyata dön</button>
              </div>
            )}
          </div>
        </fieldset>

        <dl className="status-list cost-list">
          <div><dt>İplik (fireli)</dt><dd>{isCommission ? <span className="muted">Fason: 0</span> : formatRate(cost.yarnCost, currency)}</dd></div>
          <div><dt>Örme (makine + işçilik)</dt><dd>{missingSpeed ? <span className="muted">kg/saat yok</span> : formatRate(cost.knittingCost, currency)}</dd></div>
          <div><dt>Genel gider</dt><dd>{formatRate(cost.overhead, currency)}</dd></div>
          <div className="cost-list__total"><dt>Kilo maliyeti</dt><dd>{formatRate(cost.totalCost, currency)}</dd></div>
          <div><dt>Hesaplanan fiyat</dt><dd>{formatRate(cost.calculatedPrice, currency)}</dd></div>
          <div><dt>Teklif fiyatı / kâr</dt><dd>{formatRate(unitPrice, currency)} · {percent(effectiveMargin)}</dd></div>
          <div><dt>Satır tutarı / maliyeti</dt><dd>{formatMoney(amount, currency)} · {formatMoney(costAmount, currency)}</dd></div>
          <div className="cost-list__total"><dt>Satır kârı</dt><dd>{formatMoney(amount - costAmount, currency)}</dd></div>
        </dl>
        {(missingYarnPrice || missingSpeed) && (
          <p className="muted">Eksik girdi var: {[missingYarnPrice && 'iplik fiyatı', missingSpeed && 'saatte örülen kilo'].filter(Boolean).join(', ')}. Maliyet eksik hesaplanır.</p>
        )}

        <div className="form__pair">
          <label className="field">
            <span>Müşteri kumaş kodu</span>
            <input maxLength={50} value={customerFabricCode} onChange={(event) => setCustomerFabricCode(event.target.value)} />
          </label>
          <label className="field">
            <span>Satır notu (belgede basılır)</span>
            <input maxLength={500} value={notes} onChange={(event) => setNotes(event.target.value)} />
          </label>
        </div>

        <ErrorAlert error={error} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>{readOnly ? 'Kapat' : 'Vazgeç'}</button>
          {!readOnly && <button type="submit" className="button button--primary" disabled={isLoading || fabricId === null}>Kaydet</button>}
        </footer>
      </form>
    </Modal>
  )
}

/* ───────── Kabul ───────── */

/** Kabul: satırlardan taslak sipariş açılır; fason ve kendi malı satırlar ayrı siparişe düşer. */
function AcceptQuotationDialog({ quotation, onClose }: { quotation: OrmeQuotation; onClose: () => void }) {
  const [accept, { isLoading, error }] = useAcceptOrmeQuotationMutation()
  const [customerId, setCustomerId] = useState<number | null>(quotation.customerId)
  const [orderDate, setOrderDate] = useState(todayIsoDate())
  const [dueDate, setDueDate] = useState('')
  const [customerOrderNo, setCustomerOrderNo] = useState('')
  const mixed = quotation.lines.some((line) => line.isCommission) && quotation.lines.some((line) => !line.isCommission)

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await accept({
      id: quotation.id,
      body: {
        customerId: quotation.customerId === null ? customerId : null,
        orderDate,
        dueDate: dueDate || null,
        customerOrderNo: customerOrderNo.trim() || null,
      },
    })
    if ('data' in result && result.data) {
      onClose()
    }
  }

  return (
    <Modal title={`Teklifi kabul et: ${quotation.quotationNo}`} onClose={onClose}>
      <form className="form" onSubmit={handleSubmit}>
        <p className="muted">
          Teklif satırları, teklif fiyatlarıyla taslak örme siparişine aktarılır.
          {mixed && ' Fason ve kendi malı satırlar iki ayrı siparişe düşer.'}
        </p>
        {quotation.customerId === null && (
          <>
            <p className="alert alert--info">Teklif aday firmaya ({quotation.prospectName}) verilmiş; sipariş için müşteri kartını seçin. Kart yoksa önce Müşteriler ekranından açın.</p>
            <LookupSelect resource="orme-customers" label="Müşteri" required value={customerId} onChange={setCustomerId} />
          </>
        )}
        <div className="form__pair">
          <label className="field">
            <span>Sipariş tarihi</span>
            <input type="date" required value={orderDate} onChange={(event) => setOrderDate(event.target.value)} />
          </label>
          <label className="field">
            <span>Termin</span>
            <input type="date" min={orderDate} value={dueDate} onChange={(event) => setDueDate(event.target.value)} />
          </label>
        </div>
        <label className="field">
          <span>Müşteri sipariş no</span>
          <input maxLength={50} value={customerOrderNo} onChange={(event) => setCustomerOrderNo(event.target.value)} />
        </label>
        <ErrorAlert error={error} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading || (quotation.customerId === null && customerId === null)}>
            <Check aria-hidden="true" />
            Kabul et, sipariş aç
          </button>
        </footer>
      </form>
    </Modal>
  )
}

/* ───────── Liste ───────── */

/** Örme teklifleri, en yeniden eskiye. */
export function OrmeQuotationsPage() {
  const navigate = useNavigate()
  const canManage = usePermission(Permissions.OrmeQuotationManage)
  const grid = useGrid()
  const { page, setPage } = grid
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [creating, setCreating] = useState(false)

  const { data, error, isLoading, isFetching } = useGetOrmeQuotationsQuery({
    page,
    pageSize: PAGE_SIZE,
    search: search || undefined,
    ...grid.params,
  })

  const columns: GridColumn[] = [
    { key: 'quotationNo', label: 'Teklif no' },
    { key: 'quotationDate', label: 'Tarih', filter: false },
    { key: 'validUntil', label: 'Geçerlilik', filter: false },
    { key: 'customerName', label: 'Müşteri / aday firma' },
    { key: 'lineCount', label: 'Satır', numeric: true },
    { key: 'totalKilograms', label: 'Kilo', numeric: true },
    { key: 'totalAmount', label: 'Tutar', numeric: true },
    {
      key: 'status',
      label: 'Durum',
      filter: Object.entries(ormeQuotationStatusPresentation).map(([value, { label }]) => ({ value, label })),
    },
  ]

  const handleSearch = (event: FormEvent) => {
    event.preventDefault()
    setPage(1)
    setSearch(searchInput.trim())
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">Teklifler</h1>

      <form className="toolbar" onSubmit={handleSearch} role="search">
        <input
          type="search"
          placeholder="Teklif no ya da müşteri"
          aria-label="Teklif ara"
          value={searchInput}
          onChange={(event) => setSearchInput(event.target.value)}
        />
        <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
        <ClearFiltersButton grid={grid} />
        <div className="toolbar__actions">
          {canManage && (
            <button type="button" className="button button--primary" onClick={() => setCreating(true)}>
              <Plus aria-hidden="true" />
              Yeni teklif
            </button>
          )}
        </div>
      </form>

      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />

      {data && (
        <>
          <div className="table-wrap" aria-busy={isFetching}>
            <table className="table">
              <GridHead columns={columns} grid={grid} />
              <tbody>
                {data.items.map((quotation) => {
                  const status = ormeQuotationStatusPresentation[quotation.status]
                  return (
                    <tr key={quotation.id}>
                      <td><Link to={`/orme/quotations/${quotation.id}`}><code>{quotation.quotationNo}</code></Link></td>
                      <td>{formatDate(quotation.quotationDate)}</td>
                      <td>{formatDate(quotation.validUntil)}</td>
                      <td>
                        {quotation.customerName ?? '—'}
                        {quotation.isProspect && <span className="muted"> (aday)</span>}
                      </td>
                      <td className="table__number">{quotation.lineCount}</td>
                      <td className="table__number">{formatQuantity(quotation.totalKilograms)}</td>
                      <td className="table__number">{quotation.totalAmount > 0 ? formatMoney(quotation.totalAmount, quotation.currency) : '—'}</td>
                      <td><span className={`badge badge--${status.tone}`}>{status.label}</span></td>
                    </tr>
                  )
                })}
                {data.items.length === 0 && (
                  <tr><td colSpan={columns.length} className="table__empty">Teklif bulunamadı.</td></tr>
                )}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}

      {creating && <OrmeQuotationFormDialog onClose={() => setCreating(false)} onSaved={(id) => navigate(`/orme/quotations/${id}`)} />}
    </section>
  )
}

/* ───────── Ayrıntı ───────── */

/** Teklif: başlık, satırlar ve maliyet kırılımı, durum işlemleri. Satır penceresi maliyet denemesi için de açılır. */
export function OrmeQuotationDetailPage() {
  const id = Number(useParams().id)
  const navigate = useNavigate()
  const canManage = usePermission(Permissions.OrmeQuotationManage)
  const canOrder = usePermission(Permissions.OrmeOrderManage)
  const { data: quotation, error, isLoading } = useGetOrmeQuotationQuery(id)
  const [send, sendState] = useSendOrmeQuotationMutation()
  const [reopen, reopenState] = useReopenOrmeQuotationMutation()
  const [reject, rejectState] = useRejectOrmeQuotationMutation()
  const [remove, removeState] = useDeleteOrmeQuotationMutation()
  const [removeLine, removeLineState] = useDeleteOrmeQuotationLineMutation()
  const [editing, setEditing] = useState(false)
  const [accepting, setAccepting] = useState(false)
  const [lineDialog, setLineDialog] = useState<{ line?: OrmeQuotationLine } | null>(null)
  const [pending, setPending] = useState<'delete' | 'reject' | null>(null)
  const [pendingLine, setPendingLine] = useState<number | null>(null)

  const handleDelete = async () => {
    const result = await remove(id)
    if (!('error' in result)) {
      navigate('/orme/quotations')
    }
  }

  const status = quotation && ormeQuotationStatusPresentation[quotation.status]
  const draft = quotation?.status === 'DRAFT'
  const editable = canManage && draft

  return (
    <section className="page">
      <p><Link to="/orme/quotations">← Tekliflere dön</Link></p>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error ?? sendState.error ?? reopenState.error ?? rejectState.error ?? removeState.error ?? removeLineState.error} />
      {quotation && status && (
        <>
          <header className="page__header">
            <div>
              <h1>{quotation.quotationNo} <span className={`badge badge--${status.tone}`}>{status.label}</span></h1>
              <p className="muted">
                {formatDate(quotation.quotationDate)} · Geçerlilik {formatDate(quotation.validUntil)} · {partyName(quotation)}
                {quotation.customerId === null && ' (aday firma)'} · {quotation.currency} · Belge dili {documentLanguageLabels[quotation.printLanguage]}
              </p>
              {quotation.notes && <p className="preserve-lines">{quotation.notes}</p>}
            </div>
            <div className="page__actions">
              <Link className="button" to={`/orme/quotations/${id}/print`}><Printer aria-hidden="true" />Yazdır</Link>
              {editable && <button type="button" className="button" onClick={() => setEditing(true)}><Pencil aria-hidden="true" />Düzenle</button>}
              {editable && (
                <button type="button" className="button button--primary" disabled={sendState.isLoading || quotation.lines.length === 0} onClick={() => send(id)}>
                  <Send aria-hidden="true" />Gönderildi olarak işaretle
                </button>
              )}
              {editable && pending !== 'delete' && <button type="button" className="button button--danger-ghost" onClick={() => setPending('delete')}><Trash2 aria-hidden="true" />Sil</button>}
              {editable && pending === 'delete' && <button type="button" className="button button--danger-ghost" disabled={removeState.isLoading} onClick={handleDelete}>Silmeyi onayla</button>}
              {canManage && canOrder && quotation.status === 'SENT' && (
                <button type="button" className="button button--primary" onClick={() => setAccepting(true)}><Check aria-hidden="true" />Kabul et</button>
              )}
              {canManage && quotation.status === 'SENT' && pending !== 'reject' && (
                <button type="button" className="button button--danger-ghost" onClick={() => setPending('reject')}><XCircle aria-hidden="true" />Reddedildi</button>
              )}
              {canManage && quotation.status === 'SENT' && pending === 'reject' && (
                <button type="button" className="button button--danger-ghost" disabled={rejectState.isLoading} onClick={() => reject(id)}>Reddi onayla</button>
              )}
              {canManage && ['SENT', 'REJECTED', 'EXPIRED'].includes(quotation.status) && (
                <button type="button" className="button" disabled={reopenState.isLoading} onClick={() => reopen(id)}><RotateCcw aria-hidden="true" />Taslağa al</button>
              )}
            </div>
          </header>

          <dl className="stat-strip">
            <div><dt>Toplam kilo</dt><dd>{formatQuantity(quotation.totalKilograms)}</dd></div>
            <div><dt>Teklif tutarı</dt><dd>{formatMoney(quotation.totalAmount, quotation.currency)}</dd></div>
            <div><dt>Toplam maliyet</dt><dd>{formatMoney(quotation.totalCost, quotation.currency)}</dd></div>
            <div><dt>Kâr</dt><dd>{formatMoney(quotation.profitAmount, quotation.currency)}</dd></div>
            <div><dt>Kâr oranı (maliyet üzerine)</dt><dd>{percent(quotation.marginPercent)}</dd></div>
          </dl>

          {quotation.hasMissingCosts && (
            <p className="alert alert--info">Bazı satırlarda iplik fiyatı ya da saatte örülen kilo eksik; o satırların maliyeti eksik hesaplandı.</p>
          )}

          {quotation.orders.length > 0 && (
            <p>
              Tekliften açılan sipariş:{' '}
              {quotation.orders.map((order, index) => (
                <span key={order.id}>
                  {index > 0 && ', '}
                  <Link to={`/orme/orders/${order.id}`}><code>{order.orderNo}</code></Link>
                  <span className="muted"> ({ormeOrderOwnershipLabels[order.ownership]}, {ormeOrderStatusPresentation[order.status].label})</span>
                </span>
              ))}
            </p>
          )}
          {quotation.status === 'ACCEPTED' && quotation.orders.length === 0 && (
            <p className="muted">Tekliften açılan sipariş silinmiş.</p>
          )}

          <h2 className="section-title">Satırlar ve ön maliyet ({quotation.currency}/kg)</h2>
          <div className="table-wrap">
            <table className="table table--compact">
              <thead>
                <tr>
                  <th>#</th>
                  <th>Kumaş</th>
                  <th className="table__number">Kilo</th>
                  <th className="table__number">İplik</th>
                  <th className="table__number">Örme</th>
                  <th className="table__number">Genel gider</th>
                  <th className="table__number">Maliyet</th>
                  <th className="table__number">Kâr</th>
                  <th className="table__number">Fiyat</th>
                  <th className="table__number">Tutar</th>
                  <th aria-label="İşlem" />
                </tr>
              </thead>
              <tbody>
                {quotation.lines.map((line) => (
                  <tr key={line.id}>
                    <td>{line.sequence}</td>
                    <td>
                      {line.fabricCode} — {line.fabricName}
                      {line.isCommission && <> <span className="badge badge--neutral">Fason</span></>}
                      {line.hasMissingCosts && <> <span className="badge badge--warning">Eksik maliyet</span></>}
                      <div className="muted">
                        {[
                          line.machineCode && `Makine ${line.machineCode}`,
                          line.kilogramsPerHour !== null && `${formatQuantity(line.kilogramsPerHour)} kg/saat`,
                          !line.isCommission && `fire %${formatQuantity(line.wastePercent)}`,
                          line.customerFabricCode && `Müşteri kodu ${line.customerFabricCode}`,
                          line.orderNo && `Sipariş ${line.orderNo}`,
                        ].filter(Boolean).join(' · ')}
                      </div>
                    </td>
                    <td className="table__number">{formatQuantity(line.kilograms)}</td>
                    <td className="table__number">{perKg(line.yarnCostPerKilogram)}</td>
                    <td className="table__number">{perKg(line.knittingCostPerKilogram)}</td>
                    <td className="table__number">{perKg(line.overheadPerKilogram)}</td>
                    <td className="table__number">{perKg(line.costPerKilogram)}</td>
                    <td className="table__number">{percent(line.marginPercent)}</td>
                    <td className="table__number"><strong>{perKg(line.unitPrice)}</strong></td>
                    <td className="table__number">{formatMoney(line.amount, quotation.currency)}</td>
                    <td className="table__actions">
                      <button type="button" className="button" onClick={() => setLineDialog({ line })}>
                        {editable ? <><Pencil aria-hidden="true" />Düzenle</> : <><Calculator aria-hidden="true" />Maliyet</>}
                      </button>
                      {editable && pendingLine !== line.id && (
                        <button type="button" className="icon-button" aria-label="Satırı sil" title="Satırı sil" onClick={() => setPendingLine(line.id)}>
                          <X aria-hidden="true" />
                        </button>
                      )}
                      {editable && pendingLine === line.id && (
                        <button type="button" className="button button--danger-ghost" disabled={removeLineState.isLoading}
                          onClick={() => { void removeLine({ id, lineId: line.id }); setPendingLine(null) }}>
                          Silmeyi onayla
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
                {quotation.lines.length === 0 && (
                  <tr><td colSpan={11} className="table__empty">Satır yok. Kumaş ekleyerek ön maliyeti hesaplayın.</td></tr>
                )}
              </tbody>
              {quotation.lines.length > 0 && (
                <tfoot>
                  <tr>
                    <th colSpan={2} scope="row">Toplam</th>
                    <td className="table__number">{formatQuantity(quotation.totalKilograms)}</td>
                    <td colSpan={3} />
                    <td className="table__number">{formatMoney(quotation.totalCost, quotation.currency)}</td>
                    <td className="table__number">{percent(quotation.marginPercent)}</td>
                    <td />
                    <td className="table__number">{formatMoney(quotation.totalAmount, quotation.currency)}</td>
                    <td />
                  </tr>
                </tfoot>
              )}
            </table>
          </div>
          {editable && (
            <div>
              <button type="button" className="button" onClick={() => setLineDialog({})}><Plus aria-hidden="true" />Satır ekle</button>
            </div>
          )}
          <p className="muted">
            İplik: oran × kilo fiyatı × (1 + fire); fasonda sıfır. Örme: (makine + işçilik saat maliyeti) / saatte örülen kilo. Genel gider
            doğrudan maliyete, kâr toplam maliyete eklenir. Girdiler teklif hazırlanırken saklanır; iplik fiyatı ya da parametre sonradan değişse de
            teklif değişmez. Güncellemek için satırda &quot;Güncel maliyetleri yükle&quot; kullanılır.
          </p>

          {editing && <OrmeQuotationFormDialog quotation={quotation} onClose={() => setEditing(false)} onSaved={() => setEditing(false)} />}
          {accepting && <AcceptQuotationDialog quotation={quotation} onClose={() => setAccepting(false)} />}
          {lineDialog && (
            <OrmeQuotationLineDialog
              quotation={quotation}
              line={lineDialog.line}
              readOnly={!editable}
              onClose={() => setLineDialog(null)}
            />
          )}
        </>
      )}
    </section>
  )
}

/* ───────── Baskı ───────── */

/**
 * Müşteriye basılan teklif: maliyet bilgisi içermez. Metinler teklifte seçilen, yoksa müşterinin belge dilindedir. Örme'de
 * firma kartı olmadığı için üstte antet alanı boş bırakılır; antetli kâğıda basılır.
 */
export function OrmeQuotationPrintPage() {
  const id = Number(useParams().id)
  const { data, isLoading, error } = useGetOrmeQuotationPrintQuery(id)

  if (!data) {
    return (
      <section className="page">
        <p><Link to={`/orme/quotations/${id}`}>← Teklife dön</Link></p>
        {isLoading && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={error} />
      </section>
    )
  }

  const t = documentTexts(data.documentLanguage)
  const draft = data.status === 'DRAFT'

  return (
    <section className="page proforma-page">
      <div className="no-print proforma-page__bar">
        <Link to={`/orme/quotations/${id}`}>← Teklife dön</Link>
        <span className="muted">Yazdır penceresinde hedef olarak &quot;PDF olarak kaydet&quot; seçilirse PDF dosyası oluşur.</span>
        <button type="button" className="button button--primary" onClick={() => window.print()}>
          <Printer aria-hidden="true" />
          Yazdır / PDF
        </button>
      </div>
      {draft && <p className="alert alert--info no-print">Teklif henüz taslak; belge üzerinde &quot;{t('draft')}&quot; ibaresi yer alır.</p>}

      <article lang={t.lang} className="proforma">
        {/* Antet alanı: firma bilgisi antetli kâğıttan gelir. */}
        <div style={{ minHeight: '16mm' }}>
          <span className="muted no-print">Antet alanı: firma bilgisi için antetli kâğıda basın.</span>
        </div>
        <header className="proforma__head">
          <div>
            <h1>{t('ormeQuote')}</h1>
            {draft && <div className="muted">{t('draft')}</div>}
          </div>
          <dl className="proforma__meta">
            <div><dt>{t('ormeQuoteNo')}</dt><dd>{data.quotationNo}</dd></div>
            <div><dt>{t('ormeQuoteDate')}</dt><dd>{formatDate(data.quotationDate)}</dd></div>
            <div><dt>{t('ormeQuoteValidUntil')}</dt><dd>{formatDate(data.validUntil)}</dd></div>
            <div><dt>{t('currencyLabel')}</dt><dd>{data.currency}</dd></div>
            <div><dt>{t('paymentTerms')}</dt><dd>{data.paymentTermDays === null ? '—' : `${data.paymentTermDays} ${t('days')}`}</dd></div>
          </dl>
        </header>

        <div className="proforma__parties" style={{ gridTemplateColumns: '1fr' }}>
          <section className="proforma__party">
            <h3>{t('customer')}</h3>
            <strong>{data.partyName}</strong>
            {data.customerCode && <div>{t('customerCode')}: {data.customerCode}</div>}
            {data.customerAddress && <div>{data.customerAddress}</div>}
            {(data.customerTaxOffice || data.customerTaxNumber) && (
              <div>{t('taxOffice')} / {t('taxNumber')}: {[data.customerTaxOffice, data.customerTaxNumber].filter(Boolean).join(' / ')}</div>
            )}
          </section>
        </div>

        <table className="proforma__table">
          <thead>
            <tr>
              <th>#</th>
              <th>{t('fabric')}</th>
              <th>{t('customerFabricCode')}</th>
              <th className="num">{t('weight')}</th>
              <th className="num">{t('width')}</th>
              <th className="num">{t('ormeQuoteEstimatedKg')}</th>
              <th className="num">{t('ormeQuotePricePerKg')}</th>
              <th className="num">{t('amount')}</th>
            </tr>
          </thead>
          <tbody>
            {data.lines.map((line, index) => (
              <tr key={line.id}>
                <td>{index + 1}</td>
                <td>
                  <strong>{line.fabricCode}</strong> {line.fabricName}
                  {line.isCommission && ` (${t('ormeQuoteCommission')})`}
                  {(line.knitName || line.composition) && (
                    <div className="muted">
                      {[line.knitName && `${t('ormeQuoteKnit')}: ${line.knitName}`, line.composition && `${t('ormeQuoteComposition')}: ${line.composition}`].filter(Boolean).join(' · ')}
                    </div>
                  )}
                  {line.notes && <div className="muted">{line.notes}</div>}
                </td>
                <td>{line.customerFabricCode ?? ''}</td>
                <td className="num">{formatQuantity(line.rawWeightGsm)}</td>
                <td className="num">{line.rawWidthCm === null ? '—' : formatQuantity(line.rawWidthCm)}</td>
                <td className="num">{formatQuantity(line.kilograms)}</td>
                <td className="num">{formatRate(line.unitPrice, data.currency)}</td>
                <td className="num">{formatMoney(line.amount, data.currency)}</td>
              </tr>
            ))}
          </tbody>
          <tfoot>
            <tr>
              <th colSpan={5}>{t('total')}</th>
              <td className="num">{formatQuantity(data.totalKilograms)}</td>
              <td colSpan={2} />
            </tr>
          </tfoot>
        </table>

        <div className="proforma__totals">
          {data.vatRate === null && data.customerCode !== null && <p className="muted">{t('vatRateMissing')}</p>}
          <dl>
            <div><dt>{t('subtotal')}</dt><dd>{formatMoney(data.subtotal, data.currency)}</dd></div>
            <div><dt>{t('vat')} %{data.vatRate ?? 0}</dt><dd>{formatMoney(data.vatAmount, data.currency)}</dd></div>
            <div className="proforma__grand"><dt>{t('grandTotal')}</dt><dd>{formatMoney(data.total, data.currency)}</dd></div>
          </dl>
        </div>

        {data.notes && (
          <section className="proforma__notes">
            <h3>{t('notes')}</h3>
            <p className="preserve-lines">{data.notes}</p>
          </section>
        )}

        <section className="proforma__notes">
          <h3>{t('validity')}</h3>
          <p>{t('ormeQuoteValidityNote')} {formatDate(data.validUntil)}</p>
          {data.hasCommissionLines && <p>{t('ormeQuoteCommissionNote')}</p>}
          {data.secondQualityDiscountPercent !== null && (
            <p>{t('ormeQuoteSecondQuality')}: %{formatQuantity(data.secondQualityDiscountPercent)}</p>
          )}
        </section>

        <footer className="proforma__foot">
          <div>{t('ormeQuoteFootNote')}</div>
          <div className="proforma__sign" style={{ gridTemplateColumns: 'repeat(3, 1fr)' }}>
            <div>{t('preparedBy')}</div>
            <div>{t('approvedBy')}</div>
            <div>{t('customerApproval')}</div>
          </div>
        </footer>
      </article>
    </section>
  )
}
