import { Search, Settings2, X } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { ClearFiltersButton } from '../../components/grid/ClearFiltersButton'
import { GridHead } from '../../components/grid/GridHead'
import type { GridColumn } from '../../components/grid/GridHead'
import { useGrid } from '../../components/grid/useGrid'
import { Modal } from '../../components/Modal'
import { Pagination } from '../../components/Pagination'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDate, formatQuantity, todayIsoDate } from '../../utils/format'
import { LookupSelect } from '../masterData/LookupSelect'
import {
  useGetOrmeInspectionRollsQuery,
  useGetOrmeQualitySettingsQuery,
  useGetOrmeRollInspectionQuery,
  useOverrideOrmeRollGradeMutation,
  useSaveOrmeQualitySettingsMutation,
  useSaveOrmeRollInspectionMutation,
} from './ormeInspectionApi'
import type { OrmeQualitySettings, OrmeRollInspection } from './ormeInspectionApi'
import { ormeRollStatusPresentation } from './ormeKnitApi'
import type { OrmeFabricRoll, OrmeFabricRollStatus } from './ormeKnitApi'

const PAGE_SIZE = 20

const gradeOptions: OrmeFabricRollStatus[] = ['FIRST_QUALITY', 'SECOND_QUALITY', 'REJECTED']

interface DefectDraft {
  key: number
  defectTypeId: number | null
  atMeter: string
  lengthCm: string
  /** Sunucunun verdiği puan; satır değişince silinir. */
  points: number | null
}

const draftsOf = (data: OrmeRollInspection): DefectDraft[] =>
  data.defects.map((defect) => ({
    key: defect.id,
    defectTypeId: defect.defectTypeId,
    atMeter: String(defect.atMeter),
    lengthCm: String(defect.lengthCm),
    points: defect.points,
  }))

/** Daha önce kontrol edildiyse ölçülen metre; tahmin yeniden hesaplansın diye forma taşınmaz. */
const measuredMeters = (roll: OrmeFabricRoll) => (roll.inspectedMeters !== null && !roll.inspectedMetersEstimated ? String(roll.inspectedMeters) : '')

function InspectionForm({ data, canManage, canOverride, onClose }: { data: OrmeRollInspection; canManage: boolean; canOverride: boolean; onClose: () => void }) {
  const [save, saveState] = useSaveOrmeRollInspectionMutation()
  const [override, overrideState] = useOverrideOrmeRollGradeMutation()
  const { roll } = data
  const [inspectedOn, setInspectedOn] = useState(roll.inspectedOn ?? todayIsoDate())
  const [meters, setMeters] = useState(measuredMeters(roll))
  const [widthCm, setWidthCm] = useState(roll.inspectedWidthCm !== null ? String(roll.inspectedWidthCm) : '')
  const [weightGsm, setWeightGsm] = useState(roll.inspectedWeightGsm !== null ? String(roll.inspectedWeightGsm) : '')
  const [notes, setNotes] = useState(data.inspectionNotes ?? '')
  const [rows, setRows] = useState<DefectDraft[]>(() => draftsOf(data))
  const [nextKey, setNextKey] = useState(-1)
  const [grade, setGrade] = useState<OrmeFabricRollStatus>(roll.status === 'PENDING_INSPECTION' ? 'FIRST_QUALITY' : roll.status)
  const [overrideNote, setOverrideNote] = useState(roll.overrideNote ?? '')

  const update = (key: number, change: Partial<DefectDraft>) =>
    setRows(rows.map((row) => (row.key === key ? { ...row, ...change, points: null } : row)))

  const addRow = () => {
    setRows([...rows, { key: nextKey, defectTypeId: null, atMeter: '', lengthCm: '', points: null }])
    setNextKey(nextKey - 1)
  }

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await save({
      id: roll.id,
      inspectedOn,
      meters: meters ? Number(meters) : null,
      widthCm: Number(widthCm),
      weightGsm: Number(weightGsm),
      notes: notes.trim() || null,
      defects: rows.map((row) => ({ defectTypeId: row.defectTypeId ?? 0, atMeter: Number(row.atMeter), lengthCm: Number(row.lengthCm) })),
    })
    if ('data' in result && result.data) {
      // Kayıttan sonra form açık kalır: puan ve kalite sunucudan gelir.
      setRows(draftsOf(result.data))
      if (result.data.calculatedStatus) {
        setGrade(result.data.calculatedStatus)
      }
      setOverrideNote('')
    }
  }

  const status = ormeRollStatusPresentation[roll.status]
  const inspected = data.calculatedStatus !== null
  const differs = inspected && grade !== data.calculatedStatus

  return (
    <form className="form" onSubmit={handleSubmit}>
      <p className="muted">
        {roll.fabricCode} — {roll.fabricName} · {roll.orderNo} · {roll.customerName ?? 'Stoğa üretim'} · Makine {roll.machineCode} · {formatDate(roll.cutOn)}, {formatQuantity(roll.kilograms)} kg
        {roll.meters !== null && `, ${formatQuantity(roll.meters)} m`}.
        {' '}Kalite sınırı: 1. kalite ≤ {formatQuantity(data.firstQualityMaxPoints)}, 2. kalite ≤ {formatQuantity(data.secondQualityMaxPoints)} puan / 100 m².
      </p>

      {inspected && (
        <div className="alert alert--info" role="status">
          Toplam {data.totalPoints} puan, 100 m² başına {formatQuantity(roll.pointsPer100M2 ?? 0)} puan.
          {' '}Metre: {formatQuantity(roll.inspectedMeters ?? 0)}{roll.inspectedMetersEstimated && ' (tahmini)'}.
          {' '}Hesaplanan: {data.calculatedStatus && ormeRollStatusPresentation[data.calculatedStatus].label}.
          {' '}Karar: <span className={`badge badge--${status.tone}`}>{status.label}</span>
          {roll.overrideNote && ` (${roll.overrideNote})`}
        </div>
      )}
      {roll.specDeviation && <div className="alert alert--info">Kumaş kartından sapma: {roll.specDeviation}. Kaliteyi değiştirmez.</div>}

      <fieldset disabled={!canManage} className="form">
        <div className="form__pair">
          <label className="field"><span>Kontrol tarihi</span><input type="date" required value={inspectedOn} onChange={(event) => setInspectedOn(event.target.value)} /></label>
          <label className="field">
            <span>Ölçülen metre</span>
            <input type="number" min="0" step="0.01" value={meters} onChange={(event) => setMeters(event.target.value)} />
            <span className="field__hint">{roll.meters !== null ? `Boşsa makinede girilen ${formatQuantity(roll.meters)} m kullanılır.` : 'Boşsa kilo, gramaj ve enden tahmin edilir.'}</span>
          </label>
        </div>
        <div className="form__pair">
          <label className="field">
            <span>Ölçülen en (cm)</span>
            <input type="number" min="0" step="0.01" required value={widthCm} onChange={(event) => setWidthCm(event.target.value)} />
            <span className="field__hint">Kartta {data.cardWidthCm !== null ? `${formatQuantity(data.cardWidthCm)} cm` : 'yok'}</span>
          </label>
          <label className="field">
            <span>Ölçülen gramaj (g/m²)</span>
            <input type="number" min="0" step="0.01" required value={weightGsm} onChange={(event) => setWeightGsm(event.target.value)} />
            <span className="field__hint">Kartta {formatQuantity(data.cardWeightGsm)} g/m²</span>
          </label>
        </div>

        <div className="table-wrap recipe-table">
          <table className="table table--compact">
            <thead>
              <tr>
                <th style={{ width: '50%' }}>Hata türü</th>
                <th>Metre</th>
                <th>Uzunluk (cm)</th>
                <th className="table__number">Puan</th>
                <th aria-label="Satırı kaldır" />
              </tr>
            </thead>
            <tbody>
              {rows.map((row) => (
                <tr key={row.key}>
                  <td>
                    <LookupSelect resource="orme-defect-types" label="Hata türü" required hideLabel idSuffix={`defect-${row.key}`} value={row.defectTypeId}
                      onChange={(defectTypeId) => update(row.key, { defectTypeId })} />
                  </td>
                  <td><input aria-label="Metre" type="number" min="0" step="0.01" required value={row.atMeter} onChange={(event) => update(row.key, { atMeter: event.target.value })} /></td>
                  <td><input aria-label="Uzunluk" type="number" min="0" step="0.01" required value={row.lengthCm} onChange={(event) => update(row.key, { lengthCm: event.target.value })} /></td>
                  <td className="table__number">{row.points ?? '—'}</td>
                  <td className="recipe-table__remove-cell">
                    <button type="button" className="icon-button" aria-label="Satırı kaldır" title="Satırı kaldır" onClick={() => setRows(rows.filter((item) => item.key !== row.key))}>
                      <X aria-hidden="true" />
                    </button>
                  </td>
                </tr>
              ))}
              {rows.length === 0 && <tr><td colSpan={5} className="table__empty">Hata girilmedi. Hatasız top 0 puan alır.</td></tr>}
            </tbody>
          </table>
        </div>
        <div><button type="button" className="button" onClick={addRow}>Hata ekle</button></div>
        <p className="field__hint">Puan: 7,5 cm'ye kadar 1, 15 cm'ye kadar 2, 23 cm'ye kadar 3, daha uzunsa 4; delik her zaman 4. Bir metrede en fazla 4 puan sayılır.</p>
        <label className="field"><span>Kontrol notu</span><input maxLength={500} value={notes} onChange={(event) => setNotes(event.target.value)} /></label>
      </fieldset>

      <ErrorAlert error={saveState.error} />
      <footer className="form__actions">
        <button type="button" className="button" onClick={onClose}>Kapat</button>
        {canManage && <button type="submit" className="button button--primary" disabled={saveState.isLoading}>{inspected ? 'Yeniden kontrol et' : 'Kontrolü kaydet'}</button>}
      </footer>

      {canOverride && inspected && (
        <fieldset className="form__section">
          <legend>Yetkili kararı</legend>
          <div className="form__pair">
            <label className="field">
              <span>Kalite</span>
              <select value={grade} onChange={(event) => setGrade(event.target.value as OrmeFabricRollStatus)}>
                {gradeOptions.map((value) => <option key={value} value={value}>{ormeRollStatusPresentation[value].label}</option>)}
              </select>
            </label>
            <label className="field">
              <span>Gerekçe{differs ? '' : ' (hesaplanan kalitede gerekmez)'}</span>
              <input maxLength={500} required={differs} value={overrideNote} onChange={(event) => setOverrideNote(event.target.value)} />
            </label>
          </div>
          <ErrorAlert error={overrideState.error} />
          <div>
            <button type="button" className="button" disabled={overrideState.isLoading || (differs && overrideNote.trim() === '')}
              onClick={() => override({ id: roll.id, status: grade, note: overrideNote.trim() || null })}>Kararı kaydet</button>
          </div>
        </fieldset>
      )}
    </form>
  )
}

function InspectionDialog({ roll, canManage, canOverride, onClose }: { roll: OrmeFabricRoll; canManage: boolean; canOverride: boolean; onClose: () => void }) {
  const { data, error, isLoading } = useGetOrmeRollInspectionQuery(roll.id)
  return (
    <Modal title={`Ham kontrol: ${roll.rollNo}`} onClose={onClose} variant="drawer">
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {data && <InspectionForm data={data} canManage={canManage} canOverride={canOverride} onClose={onClose} />}
    </Modal>
  )
}

function LimitsForm({ settings, onClose }: { settings: OrmeQualitySettings; onClose: () => void }) {
  const [save, { isLoading, error }] = useSaveOrmeQualitySettingsMutation()
  const [first, setFirst] = useState(String(settings.firstQualityMaxPoints))
  const [second, setSecond] = useState(String(settings.secondQualityMaxPoints))
  const [width, setWidth] = useState(String(settings.widthTolerancePercent))
  const [weight, setWeight] = useState(String(settings.weightTolerancePercent))

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await save({
      firstQualityMaxPoints: Number(first),
      secondQualityMaxPoints: Number(second),
      widthTolerancePercent: Number(width),
      weightTolerancePercent: Number(weight),
    })
    if (!('error' in result)) {
      onClose()
    }
  }

  return (
    <form className="form" onSubmit={handleSubmit}>
      <p className="muted">100 m² başına puan. Müşteri kartında müşteriye özel sınır verilebilir. Tolerans aşılırsa uyarı çıkar, kalite değişmez. Değişiklik yalnızca bundan sonraki kontrollere uygulanır.</p>
      <div className="form__pair">
        <label className="field"><span>1. kalite üst sınırı</span><input type="number" min="0" step="0.01" required value={first} onChange={(event) => setFirst(event.target.value)} /></label>
        <label className="field"><span>2. kalite üst sınırı</span><input type="number" min="0" step="0.01" required value={second} onChange={(event) => setSecond(event.target.value)} /></label>
      </div>
      <div className="form__pair">
        <label className="field"><span>En toleransı (%)</span><input type="number" min="0" max="100" step="0.01" required value={width} onChange={(event) => setWidth(event.target.value)} /></label>
        <label className="field"><span>Gramaj toleransı (%)</span><input type="number" min="0" max="100" step="0.01" required value={weight} onChange={(event) => setWeight(event.target.value)} /></label>
      </div>
      <ErrorAlert error={error} />
      <footer className="form__actions">
        <button type="button" className="button" onClick={onClose}>Vazgeç</button>
        <button type="submit" className="button button--primary" disabled={isLoading}>Kaydet</button>
      </footer>
    </form>
  )
}

function LimitsDialog({ onClose }: { onClose: () => void }) {
  const { data, error, isLoading } = useGetOrmeQualitySettingsQuery()
  return (
    <Modal title="Kalite sınırları" onClose={onClose}>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {data && <LimitsForm settings={data} onClose={onClose} />}
    </Modal>
  )
}

/** Ham kontrol: makineden çıkan toplar, 4 puan kontrolü, en ve gramaj ölçümü, kalite kararı. */
export function OrmeInspectionPage() {
  const canManage = usePermission(Permissions.OrmeInspectionManage)
  const canOverride = usePermission(Permissions.OrmeInspectionOverride)
  const canKnitView = usePermission(Permissions.OrmeKnittingView)
  const grid = useGrid()
  const { page, setPage } = grid
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [inspecting, setInspecting] = useState<OrmeFabricRoll | null>(null)
  const [limits, setLimits] = useState(false)
  const { data, error, isLoading, isFetching } = useGetOrmeInspectionRollsQuery({ page, pageSize: PAGE_SIZE, search: search || undefined, ...grid.params })

  const columns: GridColumn[] = [
    { key: 'rollNo', label: 'Top no' },
    { key: 'cutOn', label: 'Tarih', filter: false },
    { key: 'machineCode', label: 'Makine' },
    { key: 'knitOrderNo', label: 'Emir' },
    { key: 'customerName', label: 'Müşteri' },
    { key: 'fabricCode', label: 'Kumaş' },
    { key: 'kilograms', label: 'Kilo', numeric: true },
    { key: 'inspectedMeters', label: 'Kontrol metresi', numeric: true },
    { key: 'inspectedWidthCm', label: 'En (cm)', numeric: true },
    { key: 'inspectedWeightGsm', label: 'Gramaj', numeric: true },
    { key: 'pointsPer100M2', label: 'Puan / 100 m²', numeric: true },
    { key: 'status', label: 'Kalite', filter: Object.entries(ormeRollStatusPresentation).map(([value, { label }]) => ({ value, label })) },
    { label: '', ariaLabel: 'İşlem', className: 'table__actions' },
  ]

  const handleSearch = (event: FormEvent) => {
    event.preventDefault()
    setPage(1)
    setSearch(searchInput.trim())
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">Ham Kontrol</h1>
      <form className="toolbar" onSubmit={handleSearch} role="search">
        <input type="search" placeholder="Top no, emir, makine, sipariş ya da kumaş" aria-label="Top ara" value={searchInput} onChange={(event) => setSearchInput(event.target.value)} />
        <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
        <ClearFiltersButton grid={grid} />
        {canManage && (
          <div className="toolbar__actions">
            <button type="button" className="button" onClick={() => setLimits(true)}><Settings2 aria-hidden="true" />Kalite sınırları</button>
          </div>
        )}
      </form>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {data && (
        <>
          <div className="table-wrap" aria-busy={isFetching}>
            <table className="table">
              <GridHead columns={columns} grid={grid} />
              <tbody>
                {data.items.map((roll) => {
                  const status = ormeRollStatusPresentation[roll.status]
                  return (
                    <tr key={roll.id}>
                      <td><code>{roll.rollNo}</code></td>
                      <td>{formatDate(roll.cutOn)}</td>
                      <td>{roll.machineCode}</td>
                      <td>{canKnitView ? <Link to={`/orme/knit-orders/${roll.knitOrderId}`}><code>{roll.knitOrderNo}</code></Link> : <code>{roll.knitOrderNo}</code>}</td>
                      <td>{roll.customerName ?? <span className="muted">Stoğa üretim</span>}</td>
                      <td>{roll.fabricCode} — {roll.fabricName}</td>
                      <td className="table__number">{formatQuantity(roll.kilograms)}</td>
                      <td className="table__number">
                        {roll.inspectedMeters === null
                          ? '—'
                          : roll.inspectedMetersEstimated
                            ? <span className="muted" title="Metre ölçülmedi; kilo, gramaj ve enden tahmin">~{formatQuantity(roll.inspectedMeters)}</span>
                            : formatQuantity(roll.inspectedMeters)}
                      </td>
                      <td className="table__number">{roll.inspectedWidthCm !== null ? formatQuantity(roll.inspectedWidthCm) : '—'}</td>
                      <td className="table__number">{roll.inspectedWeightGsm !== null ? formatQuantity(roll.inspectedWeightGsm) : '—'}</td>
                      <td className="table__number">{roll.pointsPer100M2 !== null ? formatQuantity(roll.pointsPer100M2) : '—'}</td>
                      <td>
                        <span className={`badge badge--${status.tone}`}>{status.label}</span>
                        {roll.overrideNote && <div className="muted">Yetkili kararı</div>}
                        {roll.specDeviation && <div className="muted" title={roll.specDeviation}>Karttan sapma</div>}
                      </td>
                      <td className="table__actions">
                        <button type="button" className="button" onClick={() => setInspecting(roll)}>{roll.status === 'PENDING_INSPECTION' && canManage ? 'Kontrol et' : 'Aç'}</button>
                      </td>
                    </tr>
                  )
                })}
                {data.items.length === 0 && <tr><td colSpan={columns.length} className="table__empty">Top bulunamadı.</td></tr>}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}
      {inspecting && <InspectionDialog roll={inspecting} canManage={canManage} canOverride={canOverride} onClose={() => setInspecting(null)} />}
      {limits && <LimitsDialog onClose={() => setLimits(false)} />}
    </section>
  )
}
