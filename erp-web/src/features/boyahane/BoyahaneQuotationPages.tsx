import { Calculator, Check, FileText, Pencil, Plus, Printer, RefreshCw, RotateCcw, Search, Send, Trash2, XCircle } from 'lucide-react'
import { Fragment, useEffect, useState } from 'react'
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
import type { DocumentLanguage, DocumentTextKey } from '../../utils/documentText'
import { formatDate, formatDateTime, formatQuantity, todayIsoDate } from '../../utils/format'
import { formatMoney, formatRate } from '../../utils/money'
import { useCurrencyOptions } from '../finance/currencyOptions'
import { LookupSelect } from '../masterData/LookupSelect'
import { dyeChemicalUnit } from './boyahaneLabApi'
import { orderOwnershipLabels, orderProcessTypeLabels, orderUnitLabels } from './boyahaneOrdersApi'
import type { OrderOwnership, OrderProcessType, OrderUnit } from './boyahaneOrdersApi'
import {
  processMinutesSourceLabels,
  quotationLineToRequest,
  quotationStatusPresentation,
  useAcceptBoyahaneQuotationMutation,
  useCalculateBoyahaneQuotationMutation,
  useDeleteBoyahaneQuotationMutation,
  useExpireBoyahaneQuotationMutation,
  useGetBoyahaneQuotationQuery,
  useGetBoyahaneQuotationRecipeOptionsQuery,
  useGetBoyahaneQuotationsQuery,
  useRecalculateBoyahaneQuotationMutation,
  useRejectBoyahaneQuotationMutation,
  useReopenBoyahaneQuotationMutation,
  useSaveBoyahaneQuotationMutation,
  useSendBoyahaneQuotationMutation,
} from './boyahaneQuotationsApi'
import type {
  BoyahaneQuotation,
  BoyahaneQuotationLine,
  BoyahaneQuotationLineCost,
  BoyahaneQuotationLineRequest,
  BoyahaneQuotationRequest,
} from './boyahaneQuotationsApi'

const PAGE_SIZE = 20
const DEFAULT_VALIDITY_DAYS = 15

const processTextKeys: Record<OrderProcessType, DocumentTextKey> = {
  FABRIC_DYEING: 'fabricDyeing',
  YARN_DYEING: 'yarnDyeing',
  FINISHING: 'finishing',
  PRINTING: 'printing',
}

const usesRecipe = (process: OrderProcessType) => process === 'FABRIC_DYEING' || process === 'YARN_DYEING'
const percent = (value: number | null) => (value === null ? '—' : `%${formatQuantity(value)}`)
const optional = (text: string) => (text.trim() === '' ? null : Number(text.replace(',', '.')))
const text = (value: number | null) => (value === null ? '' : String(value))

function addDays(iso: string, days: number) {
  const date = new Date(`${iso}T00:00:00`)
  date.setDate(date.getDate() + days)
  const pad = (value: number) => String(value).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`
}

/** Teklif listesi, en yeniden eskiye. */
export function BoyahaneQuotationsPage() {
  const navigate = useNavigate()
  const canManage = usePermission(Permissions.BoyahaneQuotationManage)
  const grid = useGrid()
  const { page, setPage } = grid
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [creating, setCreating] = useState(false)
  const { data, error, isLoading, isFetching } = useGetBoyahaneQuotationsQuery({ page, pageSize: PAGE_SIZE, search: search || undefined, ...grid.params })

  const columns: GridColumn[] = [
    { key: 'quotationNo', label: 'Teklif no' },
    { key: 'quotationDate', label: 'Tarih', filter: false },
    { key: 'validUntil', label: 'Geçerlilik', filter: false },
    { key: 'customerName', label: 'Müşteri / aday' },
    { key: 'lineCount', label: 'Satır', numeric: true },
    { key: 'totalAmount', label: 'Tutar', numeric: true },
    { key: 'status', label: 'Durum', filter: Object.entries(quotationStatusPresentation).map(([value, { label }]) => ({ value, label })) },
    { key: 'orderNo', label: 'Sipariş' },
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
        <input type="search" placeholder="Teklif no, müşteri ya da aday" aria-label="Teklif ara" value={searchInput} onChange={(event) => setSearchInput(event.target.value)} />
        <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
        <ClearFiltersButton grid={grid} />
        <div className="toolbar__actions">
          {canManage && <button type="button" className="button button--primary" onClick={() => setCreating(true)}><Plus aria-hidden="true" />Yeni teklif</button>}
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
                {data.items.map((row) => {
                  const status = quotationStatusPresentation[row.status]
                  return (
                    <tr key={row.id}>
                      <td><Link to={`/boyahane/quotations/${row.id}`}><code>{row.quotationNo}</code></Link></td>
                      <td>{formatDate(row.quotationDate)}</td>
                      <td>
                        {formatDate(row.validUntil)}
                        {row.isPastValidity && <> <span className="badge badge--warning">Geçti</span></>}
                      </td>
                      <td>{row.customerName}{row.isProspect && <span className="muted"> (aday)</span>}</td>
                      <td className="table__number">{row.lineCount}</td>
                      <td className="table__number">{formatMoney(row.totalAmount, row.currency)}</td>
                      <td><span className={`badge badge--${status.tone}`}>{status.label}</span></td>
                      <td>{row.orderId !== null ? <Link to={`/boyahane/orders/${row.orderId}`}>{row.orderNo}</Link> : '—'}</td>
                    </tr>
                  )
                })}
                {data.items.length === 0 && <tr><td colSpan={columns.length} className="table__empty">Teklif bulunamadı.</td></tr>}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}
      {creating && <BoyahaneQuotationFormDialog onClose={() => setCreating(false)} onSaved={(id) => navigate(`/boyahane/quotations/${id}`)} />}
    </section>
  )
}

interface LineDraft {
  key: number
  processType: OrderProcessType
  fabricId: number | null
  yarnId: number | null
  colorName: string
  colorCode: string
  colorCardId: number | null
  patternId: number | null
  colorwayId: number | null
  quantity: string
  unit: OrderUnit
  recipeTrialId: number | null
  machineId: number | null
  batchKilograms: string
  liquorRatio: string
  processMinutes: string
  firePercent: string
  overheadPercent: string
  marginPercent: string
  unitPrice: string
  notes: string
}

const emptyLine = (key: number): LineDraft => ({
  key, processType: 'FABRIC_DYEING', fabricId: null, yarnId: null, colorName: '', colorCode: '', colorCardId: null, patternId: null, colorwayId: null,
  quantity: '', unit: 'KILOGRAM', recipeTrialId: null, machineId: null, batchKilograms: '', liquorRatio: '', processMinutes: '',
  firePercent: '', overheadPercent: '', marginPercent: '', unitPrice: '', notes: '',
})

const draftOf = (line: BoyahaneQuotationLine): LineDraft => ({
  key: line.id,
  processType: line.processType,
  fabricId: line.fabricId,
  yarnId: line.yarnId,
  colorName: line.colorName ?? '',
  colorCode: line.colorCode ?? '',
  colorCardId: line.colorCardId,
  patternId: line.patternId,
  colorwayId: line.colorwayId,
  quantity: String(line.quantity),
  unit: line.unit,
  recipeTrialId: line.recipeTrialId,
  machineId: line.machineId,
  batchKilograms: text(line.batchKilogramsInput),
  liquorRatio: text(line.liquorRatioInput),
  processMinutes: text(line.processMinutesInput),
  firePercent: text(line.firePercentInput),
  overheadPercent: text(line.overheadPercentInput),
  marginPercent: text(line.marginPercentInput),
  unitPrice: text(line.unitPriceInput),
  notes: line.notes ?? '',
})

function lineRequest(line: LineDraft): BoyahaneQuotationLineRequest {
  const yarn = line.processType === 'YARN_DYEING'
  const recipe = usesRecipe(line.processType)
  return {
    processType: line.processType,
    quantity: Number(line.quantity.replace(',', '.')),
    unit: yarn ? 'KILOGRAM' : line.unit,
    fabricId: yarn ? null : line.fabricId,
    yarnId: yarn ? line.yarnId : null,
    colorName: line.colorName.trim() || null,
    colorCode: line.colorCode.trim() || null,
    colorCardId: recipe ? line.colorCardId : null,
    patternId: line.processType === 'PRINTING' ? line.patternId : null,
    colorwayId: line.processType === 'PRINTING' && line.patternId !== null ? line.colorwayId : null,
    recipeTrialId: recipe ? line.recipeTrialId : null,
    machineId: line.machineId,
    batchKilograms: optional(line.batchKilograms),
    liquorRatio: recipe ? optional(line.liquorRatio) : null,
    processMinutes: optional(line.processMinutes),
    firePercent: optional(line.firePercent),
    overheadPercent: optional(line.overheadPercent),
    marginPercent: optional(line.marginPercent),
    unitPrice: optional(line.unitPrice),
    notes: line.notes.trim() || null,
  }
}

/** Boyama satırının reçetesi: boşsa renk kartının son onaylı reçetesi kullanılır. */
function RecipeSelect({ colorCardId, value, onChange, idSuffix }: { colorCardId: number | null; value: number | null; onChange: (value: number | null) => void; idSuffix: string }) {
  const { data, isFetching } = useGetBoyahaneQuotationRecipeOptionsQuery(colorCardId === null ? {} : { colorCardId })
  const id = `quotation-recipe-${idSuffix}`
  const known = data?.some((option) => option.trialId === value) ?? false
  return (
    <label className="field" htmlFor={id}>
      <span>Reçete{isFetching ? ' (yükleniyor)' : ''}</span>
      <select id={id} value={value ?? ''} onChange={(event) => onChange(event.target.value === '' ? null : Number(event.target.value))}>
        <option value="">{colorCardId === null ? 'Seçilmedi' : 'Renk kartının son onaylı reçetesi'}</option>
        {value !== null && !known && <option value={value}>Seçili reçete</option>}
        {data?.map((option) => (
          <option key={option.trialId} value={option.trialId}>
            {option.recipeNo}{option.colorCardCode ? ` · ${option.colorCardCode}` : ''}{option.colorName ? ` · ${option.colorName}` : ''}{option.itemCode ? ` · ${option.itemCode}` : ''}{option.customerName ? ` · ${option.customerName}` : ''}
          </option>
        ))}
      </select>
    </label>
  )
}

/** Hesaplanan satır maliyetinin kısa özeti (formda "Hesapla" sonrası). */
function CostSummary({ cost, currency }: { cost: BoyahaneQuotationLineCost; currency: string }) {
  return (
    <div className="muted form__note">
      Birim maliyet {formatRate(cost.unitCost, currency)} / {orderUnitLabels[cost.unit]}
      {' '}(boya-kimyasal {formatRate(cost.chemicalCost, currency)}{cost.printCost > 0 && `, baskı ${formatRate(cost.printCost, currency)}`}, makine {formatRate(cost.machineCost, currency)}, işçilik {formatRate(cost.laborCost, currency)}, fire {formatRate(cost.fireCost, currency)}, genel gider {formatRate(cost.overheadCost, currency)})
      {' '}· Önerilen fiyat {formatRate(cost.suggestedUnitPrice, currency)}
      {' '}· Fiyat {formatRate(cost.unitPrice, currency)} (kâr {percent(cost.marginPercent)})
      {' '}· Süre {formatQuantity(cost.processMinutes)} dk ({processMinutesSourceLabels[cost.minutesSource]}), parti {formatQuantity(cost.batchKilograms)} kg
      {cost.recipeNo && ` · Reçete ${cost.recipeNo}${cost.recipeFromColorCard ? ' (renk kartından)' : ''}`}
      {cost.warnings.map((warning) => <div key={warning} className="alert alert--info">{warning}</div>)}
    </div>
  )
}

/** Yeni teklif ya da taslak teklifin düzenlenmesi. "Hesapla" kaydetmeden maliyeti ve önerilen fiyatı gösterir. */
export function BoyahaneQuotationFormDialog({ quotation, onClose, onSaved }: { quotation?: BoyahaneQuotation; onClose: () => void; onSaved: (id: number) => void }) {
  const [save, saveState] = useSaveBoyahaneQuotationMutation()
  const [calculate, calculation] = useCalculateBoyahaneQuotationMutation()
  const today = todayIsoDate()
  const [customerId, setCustomerId] = useState<number | null>(quotation?.customerId ?? null)
  const [prospectName, setProspectName] = useState(quotation?.prospectName ?? '')
  const [ownership, setOwnership] = useState<OrderOwnership>(quotation?.ownership ?? 'COMMISSION')
  const [quotationDate, setQuotationDate] = useState(quotation?.quotationDate ?? today)
  const [validUntil, setValidUntil] = useState(quotation?.validUntil ?? addDays(today, DEFAULT_VALIDITY_DAYS))
  const [dueDate, setDueDate] = useState(quotation?.dueDate ?? '')
  const [currency, setCurrency] = useState(quotation?.currency ?? '')
  const [language, setLanguage] = useState<DocumentLanguage | ''>(quotation?.documentLanguage ?? '')
  const [notes, setNotes] = useState(quotation?.notes ?? '')
  const currencies = useCurrencyOptions(quotation?.currency)
  const [nextKey, setNextKey] = useState(-1)
  const [lines, setLines] = useState<LineDraft[]>(quotation ? quotation.lines.map(draftOf) : [emptyLine(0)])

  const update = (key: number, change: Partial<LineDraft>) => setLines(lines.map((line) => (line.key === key ? { ...line, ...change } : line)))
  const addLine = () => {
    calculation.reset()
    setLines([...lines, emptyLine(nextKey)])
    setNextKey(nextKey - 1)
  }

  const body = (): BoyahaneQuotationRequest => ({
    quotationDate,
    validUntil,
    customerId,
    prospectName: customerId === null ? prospectName.trim() || null : null,
    ownership,
    dueDate: dueDate || null,
    currency: currency || null,
    documentLanguage: language || null,
    notes: notes.trim() || null,
    lines: lines.map(lineRequest),
  })

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await save({ id: quotation?.id, body: body() })
    if ('data' in result && result.data) {
      onSaved(result.data.id)
    }
  }

  const costs = calculation.data
  return (
    <Modal title={quotation ? `Teklif düzenle: ${quotation.quotationNo}` : 'Yeni teklif'} onClose={onClose} variant="drawer">
      <form className="form" onSubmit={handleSubmit}>
        <LookupSelect resource="boyahane-customers" label="Müşteri (boşsa aday müşteri)" value={customerId} onChange={setCustomerId} />
        {customerId === null && (
          <label className="field">
            <span>Aday müşteri adı</span>
            <input maxLength={200} required value={prospectName} onChange={(event) => setProspectName(event.target.value)} />
          </label>
        )}
        <div className="form__pair">
          <label className="field">
            <span>İş türü</span>
            <select value={ownership} onChange={(event) => setOwnership(event.target.value as OrderOwnership)}>
              {Object.entries(orderOwnershipLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
            </select>
          </label>
          <label className="field">
            <span>Belge dili</span>
            <select value={language} onChange={(event) => setLanguage(event.target.value as DocumentLanguage | '')}>
              <option value="">{customerId === null ? 'Türkçe (varsayılan)' : 'Müşteri ayarı'}</option>
              {Object.entries(documentLanguageLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
            </select>
          </label>
        </div>
        <div className="form__pair">
          <label className="field">
            <span>Teklif tarihi</span>
            <input type="date" required value={quotationDate} onChange={(event) => setQuotationDate(event.target.value)} />
          </label>
          <label className="field">
            <span>Geçerlilik tarihi</span>
            <input type="date" required min={quotationDate} value={validUntil} onChange={(event) => setValidUntil(event.target.value)} />
          </label>
          <label className="field">
            <span>İstenen termin</span>
            <input type="date" value={dueDate} onChange={(event) => setDueDate(event.target.value)} />
          </label>
        </div>
        <label className="field">
          <span>Para birimi</span>
          <select value={currency} onChange={(event) => setCurrency(event.target.value)}>
            <option value="">{customerId === null ? 'Varsayılan (TRY)' : 'Müşterinin para birimi'}</option>
            {currencies.map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}
          </select>
        </label>
        <label className="field">
          <span>Not</span>
          <textarea rows={2} maxLength={1000} value={notes} onChange={(event) => setNotes(event.target.value)} />
        </label>

        {lines.map((line, index) => {
          const recipe = usesRecipe(line.processType)
          const fromArchive = (recipe && line.colorCardId !== null) || (line.processType === 'PRINTING' && line.patternId !== null)
          const cost = costs?.lines[index]
          return (
            <fieldset key={line.key} className="form__section">
              <legend>{index + 1}. satır</legend>
              <div className="form__pair">
                <label className="field">
                  <span>İşlem türü</span>
                  <select value={line.processType} onChange={(event) => {
                    const processType = event.target.value as OrderProcessType
                    update(line.key, { processType, unit: processType === 'YARN_DYEING' ? 'KILOGRAM' : line.unit })
                  }}>
                    {Object.entries(orderProcessTypeLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
                  </select>
                </label>
                {line.processType === 'YARN_DYEING'
                  ? <LookupSelect resource="boyahane-yarns" label="İplik kartı" required idSuffix={`qy${line.key}`} value={line.yarnId} onChange={(value) => update(line.key, { yarnId: value })} />
                  : <LookupSelect resource="boyahane-fabrics" label="Kumaş kartı" required idSuffix={`qf${line.key}`} value={line.fabricId} onChange={(value) => update(line.key, { fabricId: value })} />}
              </div>
              {recipe && (
                <div className="form__pair">
                  <LookupSelect
                    resource="boyahane-color-cards"
                    label="Renk kartı (isteğe bağlı)"
                    idSuffix={`qc${line.key}`}
                    customerId={customerId}
                    value={line.colorCardId}
                    onChange={(value) => update(line.key, { colorCardId: value })}
                  />
                  <RecipeSelect colorCardId={line.colorCardId} value={line.recipeTrialId} idSuffix={String(line.key)} onChange={(value) => update(line.key, { recipeTrialId: value })} />
                </div>
              )}
              {line.processType === 'PRINTING' && (
                <div className="form__pair">
                  <LookupSelect
                    resource="boyahane-patterns"
                    label="Desen kartı (isteğe bağlı)"
                    idSuffix={`qp${line.key}`}
                    customerId={customerId}
                    value={line.patternId}
                    onChange={(value) => update(line.key, { patternId: value, colorwayId: value === line.patternId ? line.colorwayId : null })}
                  />
                  {line.patternId !== null && (
                    <LookupSelect
                      resource={`boyahane-patterns/${line.patternId}/colorways`}
                      label="Renk yolu (baskı reçetesi)"
                      idSuffix={`qw${line.key}`}
                      value={line.colorwayId}
                      onChange={(value) => update(line.key, { colorwayId: value })}
                    />
                  )}
                </div>
              )}
              {!fromArchive && (
                <div className="form__pair">
                  <label className="field">
                    <span>{line.processType === 'PRINTING' ? 'Desen' : 'Renk'} (isteğe bağlı)</span>
                    <input maxLength={100} value={line.colorName} onChange={(event) => update(line.key, { colorName: event.target.value })} />
                  </label>
                  <label className="field">
                    <span>Müşteri renk / desen kodu</span>
                    <input maxLength={50} value={line.colorCode} onChange={(event) => update(line.key, { colorCode: event.target.value })} />
                  </label>
                </div>
              )}
              <div className="form__pair">
                <label className="field">
                  <span>Miktar</span>
                  <input type="number" min="0" step="0.01" required value={line.quantity} onChange={(event) => update(line.key, { quantity: event.target.value })} />
                </label>
                <label className="field">
                  <span>Birim (fiyat bu birim içindir)</span>
                  <select value={line.unit} disabled={line.processType === 'YARN_DYEING'} onChange={(event) => update(line.key, { unit: event.target.value as OrderUnit })}>
                    <option value="KILOGRAM">Kilogram</option>
                    <option value="METER">Metre</option>
                  </select>
                </label>
                <label className="field">
                  <span>Birim fiyat (boşsa önerilen)</span>
                  <input type="number" min="0" step="0.0001" value={line.unitPrice} onChange={(event) => update(line.key, { unitPrice: event.target.value })} />
                </label>
              </div>
              <details>
                <summary>Maliyet ayarları (boş bırakılanlar parametrelerden)</summary>
                <div className="form__pair">
                  <LookupSelect resource="boyahane-machines" label="Makine (saat maliyeti, parti)" idSuffix={`qm${line.key}`} value={line.machineId} onChange={(value) => update(line.key, { machineId: value })} />
                  <label className="field">
                    <span>Parti büyüklüğü (kg)</span>
                    <input type="number" min="0" step="0.01" placeholder="Makine kapasitesi" value={line.batchKilograms} onChange={(event) => update(line.key, { batchKilograms: event.target.value })} />
                  </label>
                  {recipe && (
                    <label className="field">
                      <span>Flotte oranı (1:x)</span>
                      <input type="number" min="0.1" max="100" step="0.1" placeholder="Makine / varsayılan" value={line.liquorRatio} onChange={(event) => update(line.key, { liquorRatio: event.target.value })} />
                    </label>
                  )}
                  <label className="field">
                    <span>Parti süresi (dk)</span>
                    <input type="number" min="1" step="1" placeholder="Rota / varsayılan" value={line.processMinutes} onChange={(event) => update(line.key, { processMinutes: event.target.value })} />
                  </label>
                </div>
                <div className="form__pair">
                  <label className="field">
                    <span>Fire (%)</span>
                    <input type="number" min="0" max="50" step="0.1" value={line.firePercent} onChange={(event) => update(line.key, { firePercent: event.target.value })} />
                  </label>
                  <label className="field">
                    <span>Genel gider (%)</span>
                    <input type="number" min="0" max="100" step="0.1" value={line.overheadPercent} onChange={(event) => update(line.key, { overheadPercent: event.target.value })} />
                  </label>
                  <label className="field">
                    <span>Kâr payı (%)</span>
                    <input type="number" min="-100" max="1000" step="0.1" value={line.marginPercent} onChange={(event) => update(line.key, { marginPercent: event.target.value })} />
                  </label>
                  <label className="field">
                    <span>Satır notu</span>
                    <input maxLength={500} value={line.notes} onChange={(event) => update(line.key, { notes: event.target.value })} />
                  </label>
                </div>
              </details>
              {cost && costs && <CostSummary cost={cost} currency={costs.currency} />}
              {lines.length > 1 && (
                <div>
                  <button type="button" className="button button--danger-ghost" onClick={() => {
                    calculation.reset()
                    setLines(lines.filter((item) => item.key !== line.key))
                  }}>Satırı kaldır</button>
                </div>
              )}
            </fieldset>
          )
        })}
        <div>
          <button type="button" className="button" onClick={addLine}>Satır ekle</button>
        </div>

        {costs && (
          <p className="muted">Toplam maliyet {formatMoney(costs.totalCost, costs.currency)} · Toplam tutar {formatMoney(costs.totalAmount, costs.currency)}</p>
        )}
        <ErrorAlert error={saveState.error ?? calculation.error} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="button" className="button" disabled={calculation.isLoading} onClick={() => calculate(body())}><Calculator aria-hidden="true" />Hesapla</button>
          <button type="submit" className="button button--primary" disabled={saveState.isLoading}>Kaydet</button>
        </footer>
      </form>
    </Modal>
  )
}

/** Satırın maliyet dökümü: kayıtlı değerler ve güncel lot fiyatlarıyla boya-kimyasal ayrıntısı. */
function LineCostDialog({ quotation, line, onClose }: { quotation: BoyahaneQuotation; line: BoyahaneQuotationLine; onClose: () => void }) {
  const [calculate, { data, error, isLoading }] = useCalculateBoyahaneQuotationMutation()
  const currency = quotation.currency

  useEffect(() => {
    calculate({
      quotationDate: quotation.quotationDate,
      validUntil: quotation.validUntil,
      customerId: quotation.customerId,
      prospectName: quotation.prospectName,
      ownership: quotation.ownership,
      dueDate: null,
      currency: quotation.currency,
      documentLanguage: quotation.documentLanguage,
      notes: null,
      lines: [quotationLineToRequest(line)],
    })
  }, [calculate, quotation, line])

  const current = data?.lines[0]
  const rows: [string, number][] = [
    ['Boya ve kimyasal', line.chemicalCost],
    ['Baskı pastası', line.printCost],
    ['Makine (enerji, su)', line.machineCost],
    ['İşçilik', line.laborCost],
    [`Fire (%${formatQuantity(line.firePercent)})`, line.fireCost],
    [`Genel gider (%${formatQuantity(line.overheadPercent)})`, line.overheadCost],
  ]

  return (
    <Modal title={`Maliyet dökümü: ${line.itemCode}${line.colorName ? ` · ${line.colorName}` : ''}`} onClose={onClose} variant="drawer">
      <p className="muted">
        Tutarlar {orderUnitLabels[line.unit]} başına, {currency}. Parti {formatQuantity(line.batchKilograms)} kg
        {line.liquorRatio !== null && `, flotte 1:${formatQuantity(line.liquorRatio)}`}, süre {formatQuantity(line.processMinutes)} dk ({processMinutesSourceLabels[line.minutesSource]})
        {line.machineCode && `, makine ${line.machineCode}`}
        {line.kilogramsPerMeter !== null && `, 1 m = ${formatQuantity(line.kilogramsPerMeter)} kg`}
        {line.recipeNo && `, reçete ${line.recipeNo}`}.
      </p>
      <div className="table-wrap">
        <table className="table table--compact">
          <thead><tr><th>Kalem</th><th className="table__number">Kayıtlı</th><th className="table__number">Güncel</th></tr></thead>
          <tbody>
            {rows.map(([label, value], index) => {
              const now = current ? [current.chemicalCost, current.printCost, current.machineCost, current.laborCost, current.fireCost, current.overheadCost][index] : null
              return (
                <tr key={label}>
                  <td>{label}</td>
                  <td className="table__number">{formatRate(value, currency)}</td>
                  <td className="table__number">{now === null ? '—' : formatRate(now, currency)}</td>
                </tr>
              )
            })}
          </tbody>
          <tfoot>
            <tr><th scope="row">Birim maliyet</th><td className="table__number">{formatRate(line.unitCost, currency)}</td><td className="table__number">{current ? formatRate(current.unitCost, currency) : '—'}</td></tr>
            <tr><th scope="row">Birim fiyat</th><td className="table__number">{formatRate(line.unitPrice, currency)}</td><td className="table__number">{current ? formatRate(current.unitPrice, currency) : '—'}</td></tr>
            <tr><th scope="row">Kâr payı</th><td className="table__number">{percent(line.marginPercent)}</td><td className="table__number">{current ? percent(current.marginPercent) : '—'}</td></tr>
          </tfoot>
        </table>
      </div>
      {line.costWarning && <p className="alert alert--info">{line.costWarning}</p>}

      <h3 className="section-title">Boya ve kimyasal (güncel lot maliyetiyle)</h3>
      {isLoading && <p role="status">Hesaplanıyor…</p>}
      <ErrorAlert error={error} />
      {current && (
        <div className="table-wrap">
          <table className="table table--compact">
            <thead>
              <tr>
                <th>Kod</th><th>Ad</th><th className="table__number">Miktar</th>
                <th className="table__number">kg / {orderUnitLabels[line.unit]}</th>
                <th className="table__number">kg maliyeti</th><th className="table__number">Maliyet</th>
              </tr>
            </thead>
            <tbody>
              {current.chemicals.map((chemical) => (
                <tr key={`${chemical.printing ? 'p' : 'r'}${chemical.dyeChemicalId}`}>
                  <td><code>{chemical.code}</code></td>
                  <td>{chemical.name}{chemical.printing && <span className="muted"> (baskı)</span>}</td>
                  <td className="table__number">{formatQuantity(chemical.amount)} {chemical.printing ? 'g/kg' : dyeChemicalUnit[chemical.kind]}</td>
                  <td className="table__number">{chemical.kilogramsPerUnit.toLocaleString('tr-TR', { maximumFractionDigits: 4 })}</td>
                  <td className="table__number">{chemical.costPerKilogram === null ? <span className="muted">Lot maliyeti yok</span> : formatRate(chemical.costPerKilogram, currency)}</td>
                  <td className="table__number">{formatRate(chemical.cost, currency)}</td>
                </tr>
              ))}
              {current.chemicals.length === 0 && <tr><td colSpan={6} className="table__empty">Reçete ya da baskı reçetesi yok.</td></tr>}
            </tbody>
          </table>
        </div>
      )}
      {current && current.warnings.length > 0 && <ul className="muted">{current.warnings.map((warning) => <li key={warning}>{warning}</li>)}</ul>}
      <footer className="form__actions">
        <button type="button" className="button" onClick={onClose}>Kapat</button>
      </footer>
    </Modal>
  )
}

/** Kabul: taslak sipariş açılır. Aday müşteriye verilmiş teklifte müşteri kartı seçilmelidir. */
function AcceptDialog({ quotation, onClose }: { quotation: BoyahaneQuotation; onClose: () => void }) {
  const navigate = useNavigate()
  const [accept, { isLoading, error }] = useAcceptBoyahaneQuotationMutation()
  const [customerId, setCustomerId] = useState<number | null>(quotation.customerId)
  const [customerOrderNo, setCustomerOrderNo] = useState('')
  const [dueDate, setDueDate] = useState(quotation.dueDate ?? '')
  const [note, setNote] = useState('')

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await accept({ id: quotation.id, customerId: quotation.customerId === null ? customerId : null, customerOrderNo: customerOrderNo.trim() || null, dueDate: dueDate || null, note: note.trim() || null })
    if ('data' in result && result.data?.orderId) {
      navigate(`/boyahane/orders/${result.data.orderId}`)
    }
  }

  return (
    <Modal title={`Teklifi kabul et: ${quotation.quotationNo}`} onClose={onClose}>
      <form className="form" onSubmit={handleSubmit}>
        <p className="muted">Teklifin satırları ve fiyatlarıyla taslak sipariş açılır; fatura ölçüsü satırın birimi olarak yazılır. Sipariş onayı ayrıca verilir.</p>
        {quotation.customerId === null && (
          <LookupSelect resource="boyahane-customers" label={`Müşteri kartı (aday: ${quotation.prospectName ?? ''})`} required={quotation.ownership === 'COMMISSION'} value={customerId} onChange={setCustomerId} />
        )}
        <div className="form__pair">
          <label className="field">
            <span>Müşteri sipariş no</span>
            <input maxLength={50} value={customerOrderNo} onChange={(event) => setCustomerOrderNo(event.target.value)} />
          </label>
          <label className="field">
            <span>Termin</span>
            <input type="date" value={dueDate} onChange={(event) => setDueDate(event.target.value)} />
          </label>
        </div>
        <label className="field">
          <span>Not</span>
          <input maxLength={500} value={note} onChange={(event) => setNote(event.target.value)} />
        </label>
        <ErrorAlert error={error} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading}>Kabul et ve sipariş aç</button>
        </footer>
      </form>
    </Modal>
  )
}

/** Ret ya da süre dolumu: isteğe bağlı açıklama. */
function DecisionDialog({ quotation, kind, onClose }: { quotation: BoyahaneQuotation; kind: 'reject' | 'expire'; onClose: () => void }) {
  const [reject, rejectState] = useRejectBoyahaneQuotationMutation()
  const [expire, expireState] = useExpireBoyahaneQuotationMutation()
  const [note, setNote] = useState('')
  const state = kind === 'reject' ? rejectState : expireState

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const action = kind === 'reject' ? reject : expire
    if (!('error' in await action({ id: quotation.id, note: note.trim() || null }))) {
      onClose()
    }
  }

  return (
    <Modal title={`${kind === 'reject' ? 'Reddedildi' : 'Süresi doldu'}: ${quotation.quotationNo}`} onClose={onClose}>
      <form className="form" onSubmit={handleSubmit}>
        <label className="field">
          <span>{kind === 'reject' ? 'Ret nedeni' : 'Açıklama'}</span>
          <input maxLength={500} value={note} onChange={(event) => setNote(event.target.value)} />
        </label>
        <ErrorAlert error={state.error} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={state.isLoading}>Kaydet</button>
        </footer>
      </form>
    </Modal>
  )
}

/** Teklif: başlık, satırlar ve maliyet dökümü, durum işlemleri. */
export function BoyahaneQuotationDetailPage() {
  const id = Number(useParams().id)
  const navigate = useNavigate()
  const canManage = usePermission(Permissions.BoyahaneQuotationManage)
  const canOrder = usePermission(Permissions.BoyahaneOrderManage)
  const { data: quotation, error, isLoading } = useGetBoyahaneQuotationQuery(id)
  const [send, sendState] = useSendBoyahaneQuotationMutation()
  const [reopen, reopenState] = useReopenBoyahaneQuotationMutation()
  const [recalculate, recalculateState] = useRecalculateBoyahaneQuotationMutation()
  const [remove, removeState] = useDeleteBoyahaneQuotationMutation()
  const [editing, setEditing] = useState(false)
  const [accepting, setAccepting] = useState(false)
  const [deciding, setDeciding] = useState<'reject' | 'expire' | null>(null)
  const [costLine, setCostLine] = useState<BoyahaneQuotationLine | null>(null)
  const [confirmDelete, setConfirmDelete] = useState(false)

  const handleDelete = async () => {
    if (!('error' in await remove(id))) {
      navigate('/boyahane/quotations')
    }
  }

  const status = quotation && quotationStatusPresentation[quotation.status]
  const draft = quotation?.status === 'DRAFT'
  const sent = quotation?.status === 'SENT'

  return (
    <section className="page">
      <p><Link to="/boyahane/quotations">← Tekliflere dön</Link></p>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error ?? sendState.error ?? reopenState.error ?? recalculateState.error ?? removeState.error} />
      {quotation && status && (
        <>
          <header className="page__header">
            <div>
              <h1>{quotation.quotationNo} <span className={`badge badge--${status.tone}`}>{status.label}</span>{quotation.isPastValidity && <> <span className="badge badge--warning">Geçerlilik geçti</span></>}</h1>
              <p className="muted">
                {formatDate(quotation.quotationDate)} · Geçerlilik {formatDate(quotation.validUntil)} · {quotation.customerName ?? `${quotation.prospectName ?? ''} (aday)`} · {orderOwnershipLabels[quotation.ownership]}
                {quotation.dueDate && ` · İstenen termin ${formatDate(quotation.dueDate)}`} · {documentLanguageLabels[quotation.documentLanguage]}
              </p>
              {quotation.notes && <p>{quotation.notes}</p>}
              {quotation.decidedAt && (
                <p className="muted">
                  {status.label}: {formatDateTime(quotation.decidedAt)}{quotation.decidedBy && ` · ${quotation.decidedBy}`}{quotation.decisionNote && ` · ${quotation.decisionNote}`}
                  {quotation.orderId !== null && <> · Sipariş <Link to={`/boyahane/orders/${quotation.orderId}`}>{quotation.orderNo}</Link></>}
                </p>
              )}
            </div>
            <div className="page__actions">
              <Link className="button" to={`/boyahane/quotations/${quotation.id}/print`}><Printer aria-hidden="true" />Yazdır</Link>
              {canManage && draft && <button type="button" className="button" onClick={() => setEditing(true)}><Pencil aria-hidden="true" />Düzenle</button>}
              {canManage && draft && <button type="button" className="button" disabled={recalculateState.isLoading} onClick={() => recalculate(id)}><RefreshCw aria-hidden="true" />Yeniden hesapla</button>}
              {canManage && draft && <button type="button" className="button button--primary" disabled={sendState.isLoading} onClick={() => send(id)}><Send aria-hidden="true" />Gönderildi</button>}
              {canManage && canOrder && sent && <button type="button" className="button button--primary" onClick={() => setAccepting(true)}><Check aria-hidden="true" />Kabul</button>}
              {canManage && sent && <button type="button" className="button" onClick={() => setDeciding('reject')}><XCircle aria-hidden="true" />Ret</button>}
              {canManage && sent && <button type="button" className="button" onClick={() => setDeciding('expire')}><FileText aria-hidden="true" />Süresi doldu</button>}
              {canManage && (sent || quotation.status === 'REJECTED' || quotation.status === 'EXPIRED') && (
                <button type="button" className="button" disabled={reopenState.isLoading} onClick={() => reopen(id)}><RotateCcw aria-hidden="true" />Taslağa al</button>
              )}
              {canManage && draft && !confirmDelete && <button type="button" className="button button--danger-ghost" onClick={() => setConfirmDelete(true)}><Trash2 aria-hidden="true" />Sil</button>}
              {confirmDelete && <button type="button" className="button button--danger-ghost" disabled={removeState.isLoading} onClick={handleDelete}>Silmeyi onayla</button>}
            </div>
          </header>

          <dl className="stat-strip">
            <div><dt>Toplam tutar</dt><dd>{formatMoney(quotation.totalAmount, quotation.currency)}</dd></div>
            <div><dt>Toplam maliyet</dt><dd>{formatMoney(quotation.totalCost, quotation.currency)}</dd></div>
            <div><dt>Kâr payı</dt><dd>{percent(quotation.marginPercent)}</dd></div>
          </dl>

          <div className="table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <th>İşlem</th>
                  <th>Kart</th>
                  <th>Renk / desen</th>
                  <th className="table__number">Miktar</th>
                  <th className="table__number" title="Boya-kimyasal + baskı">Malzeme</th>
                  <th className="table__number" title="Makine + işçilik">Süre maliyeti</th>
                  <th className="table__number" title="Fire + genel gider">Ek</th>
                  <th className="table__number">Birim maliyet</th>
                  <th className="table__number">Kâr</th>
                  <th className="table__number">Birim fiyat</th>
                  <th className="table__number">Tutar</th>
                  <th aria-label="İşlem" />
                </tr>
              </thead>
              <tbody>
                {quotation.lines.map((line) => (
                  <Fragment key={line.id}>
                    <tr>
                      <td>{orderProcessTypeLabels[line.processType]}</td>
                      <td>{line.itemCode} — {line.itemName}{line.notes && <div className="muted">{line.notes}</div>}</td>
                      <td>
                        {line.colorName ?? '—'}{line.colorCode && <span className="muted"> ({line.colorCode})</span>}
                        {line.recipeNo && <div className="muted">Reçete {line.recipeNo}</div>}
                      </td>
                      <td className="table__number">{formatQuantity(line.quantity)} {orderUnitLabels[line.unit]}</td>
                      <td className="table__number">{formatRate(line.chemicalCost + line.printCost, quotation.currency)}</td>
                      <td className="table__number">
                        {formatRate(line.machineCost + line.laborCost, quotation.currency)}
                        <div className="muted">{formatQuantity(line.processMinutes)} dk / {formatQuantity(line.batchKilograms)} kg</div>
                      </td>
                      <td className="table__number">{formatRate(line.fireCost + line.overheadCost, quotation.currency)}</td>
                      <td className="table__number">{formatRate(line.unitCost, quotation.currency)}</td>
                      <td className="table__number">{percent(line.marginPercent)}</td>
                      <td className="table__number">
                        {formatRate(line.unitPrice, quotation.currency)} / {orderUnitLabels[line.unit]}
                        {line.unitPriceInput !== null && <div className="muted">elle</div>}
                      </td>
                      <td className="table__number">{formatMoney(line.amount, quotation.currency)}</td>
                      <td><button type="button" className="button button--tiny" onClick={() => setCostLine(line)}>Döküm</button></td>
                    </tr>
                    {line.costWarning && (
                      <tr><td colSpan={12} className="muted">{line.costWarning}</td></tr>
                    )}
                  </Fragment>
                ))}
              </tbody>
              <tfoot>
                <tr>
                  <th colSpan={10} scope="row">Toplam</th>
                  <td className="table__number">{formatMoney(quotation.totalAmount, quotation.currency)}</td>
                  <td />
                </tr>
              </tfoot>
            </table>
          </div>

          {editing && <BoyahaneQuotationFormDialog quotation={quotation} onClose={() => setEditing(false)} onSaved={() => setEditing(false)} />}
          {accepting && <AcceptDialog quotation={quotation} onClose={() => setAccepting(false)} />}
          {deciding && <DecisionDialog quotation={quotation} kind={deciding} onClose={() => setDeciding(null)} />}
          {costLine && <LineCostDialog quotation={quotation} line={costLine} onClose={() => setCostLine(null)} />}
        </>
      )}
    </section>
  )
}

/**
 * Teklif baskısı: müşterinin belge dilinde, maliyet bilgisi olmadan. Firma bilgisi antetli kâğıttan gelir; üstte antet alanı
 * boş bırakılır.
 */
export function BoyahaneQuotationPrintPage() {
  const id = Number(useParams().id)
  const { data, isLoading, error } = useGetBoyahaneQuotationQuery(id)

  if (!data) {
    return (
      <section className="page">
        <p><Link to={`/boyahane/quotations/${id}`}>← Teklife dön</Link></p>
        {isLoading && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={error} />
      </section>
    )
  }

  const t = documentTexts(data.documentLanguage)
  const draft = data.status === 'DRAFT'
  const hasColorCode = data.lines.some((line) => line.colorCode)

  return (
    <section className="page proforma-page">
      <div className="no-print proforma-page__bar">
        <Link to={`/boyahane/quotations/${id}`}>← Teklife dön</Link>
        <span className="muted">Yazdır penceresinde hedef olarak &quot;PDF olarak kaydet&quot; seçilirse PDF dosyası oluşur.</span>
        <button type="button" className="button button--primary" onClick={() => window.print()}><Printer aria-hidden="true" />Yazdır / PDF</button>
      </div>
      {draft && <p className="alert alert--info no-print">Teklif henüz taslak; belge üzerinde &quot;{t('draft')}&quot; ibaresi yer alır.</p>}

      <article lang={t.lang} className="proforma">
        <div style={{ minHeight: '16mm' }}>
          <span className="muted no-print">Antet alanı: firma bilgisi için antetli kâğıda basın.</span>
        </div>
        <header className="proforma__head">
          <div>
            <h1>{t('boyahaneQuote')}</h1>
            {draft && <div className="muted">{t('draft')}</div>}
          </div>
          <dl className="proforma__meta">
            <div><dt>{t('boyahaneQuoteNo')}</dt><dd>{data.quotationNo}</dd></div>
            <div><dt>{t('boyahaneQuoteDate')}</dt><dd>{formatDate(data.quotationDate)}</dd></div>
            <div><dt>{t('boyahaneQuoteValidUntil')}</dt><dd>{formatDate(data.validUntil)}</dd></div>
            {data.dueDate && <div><dt>{t('boyahaneQuoteRequestedDelivery')}</dt><dd>{formatDate(data.dueDate)}</dd></div>}
            <div><dt>{t('paymentTerms')}</dt><dd>{data.paymentTermDays === null ? '—' : `${data.paymentTermDays} ${t('days')}`}</dd></div>
            <div><dt>{t('currencyLabel')}</dt><dd>{data.currency}</dd></div>
          </dl>
        </header>

        <div className="proforma__parties" style={{ gridTemplateColumns: '1fr' }}>
          <section className="proforma__party">
            <h3>{t('customer')}</h3>
            <strong>{data.customerName ?? data.prospectName}</strong>
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
              <th>{t('process')}</th>
              <th>{t('fabric')} / {t('yarn')}</th>
              <th>{t('color')}</th>
              {hasColorCode && <th>{t('colorCode')}</th>}
              <th className="num">{t('quantity')}</th>
              <th className="num">{t('unitPrice')}</th>
              <th className="num">{t('amount')}</th>
            </tr>
          </thead>
          <tbody>
            {data.lines.map((line, index) => (
              <tr key={line.id}>
                <td>{index + 1}</td>
                <td>{t(processTextKeys[line.processType])}</td>
                <td><strong>{line.itemCode}</strong> {line.itemName}</td>
                <td>{line.colorName ?? '—'}</td>
                {hasColorCode && <td>{line.colorCode ?? ''}</td>}
                <td className="num">{formatQuantity(line.quantity)} {orderUnitLabels[line.unit]}</td>
                <td className="num">{formatRate(line.unitPrice, data.currency)} / {orderUnitLabels[line.unit]}</td>
                <td className="num">{formatMoney(line.amount, data.currency)}</td>
              </tr>
            ))}
          </tbody>
        </table>

        <div className="proforma__totals">
          <dl>
            <div className="proforma__grand"><dt>{t('boyahaneQuoteEstimatedTotal')}</dt><dd>{formatMoney(data.totalAmount, data.currency)}</dd></div>
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
          <p>{t('boyahaneQuoteValidityNote')}</p>
          {data.ownership === 'COMMISSION' && <p>{t('boyahaneQuoteCommissionNote')}</p>}
          <p>{t('boyahaneQuoteQuantityNote')}</p>
        </section>

        <footer className="proforma__foot">
          <div>{t('boyahaneQuoteFootNote')}</div>
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
