import { Search, Settings2, X } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
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
  useGetDokumaInspectionRollsQuery,
  useGetDokumaQualitySettingsQuery,
  useGetDokumaRollInspectionQuery,
  useOverrideDokumaRollGradeMutation,
  useSaveDokumaQualitySettingsMutation,
  useSaveDokumaRollInspectionMutation,
} from './dokumaInspectionApi'
import type { DokumaQualitySettings, DokumaRollInspection } from './dokumaInspectionApi'
import { dokumaRollStatusPresentation } from './dokumaWeavingApi'
import type { DokumaFabricRoll, DokumaFabricRollStatus } from './dokumaWeavingApi'

const PAGE_SIZE = 20

const gradeOptions: DokumaFabricRollStatus[] = ['FIRST_QUALITY', 'SECOND_QUALITY', 'REJECTED']

interface DefectDraft {
  key: number
  defectTypeId: number | null
  atMeter: string
  lengthCm: string
  /** Sunucunun verdiği puan; satır değişince silinir. */
  points: number | null
}

const draftsOf = (data: DokumaRollInspection): DefectDraft[] =>
  data.defects.map((defect) => ({
    key: defect.id,
    defectTypeId: defect.defectTypeId,
    atMeter: String(defect.atMeter),
    lengthCm: String(defect.lengthCm),
    points: defect.points,
  }))

function InspectionForm({ data, canManage, canOverride, onClose }: { data: DokumaRollInspection; canManage: boolean; canOverride: boolean; onClose: () => void }) {
  const [save, saveState] = useSaveDokumaRollInspectionMutation()
  const [override, overrideState] = useOverrideDokumaRollGradeMutation()
  const { roll } = data
  const [inspectedOn, setInspectedOn] = useState(roll.inspectedOn ?? todayIsoDate())
  const [meters, setMeters] = useState(String(roll.inspectedMeters ?? roll.meters))
  const [widthCm, setWidthCm] = useState(String(roll.inspectedWidthCm ?? data.rawWidthCm))
  const [notes, setNotes] = useState(data.inspectionNotes ?? '')
  const [rows, setRows] = useState<DefectDraft[]>(() => draftsOf(data))
  const [nextKey, setNextKey] = useState(-1)
  const [grade, setGrade] = useState<DokumaFabricRollStatus>(roll.status === 'PENDING_INSPECTION' ? 'FIRST_QUALITY' : roll.status)
  const [overrideNote, setOverrideNote] = useState(data.overrideNote ?? '')

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
      meters: Number(meters),
      widthCm: Number(widthCm),
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

  const status = dokumaRollStatusPresentation[roll.status]
  const inspected = data.calculatedStatus !== null
  const differs = inspected && grade !== data.calculatedStatus

  return (
    <form className="form" onSubmit={handleSubmit}>
      <p className="muted">
        {roll.articleCode} — {roll.articleName} · {roll.orderNo} · {roll.customerName ?? 'Stoğa üretim'} · Tezgâh {roll.loomCode} · Kesim {formatDate(roll.cutOn)}, {formatQuantity(roll.meters)} m.
        {' '}Kalite sınırı: 1. kalite ≤ {formatQuantity(data.firstQualityMaxPoints)}, 2. kalite ≤ {formatQuantity(data.secondQualityMaxPoints)} puan / 100 m².
      </p>

      {inspected && (
        <div className="alert alert--info" role="status">
          Toplam {data.totalPoints} puan, 100 m² başına {formatQuantity(roll.pointsPer100M2 ?? 0)} puan.
          {' '}Hesaplanan: {data.calculatedStatus && dokumaRollStatusPresentation[data.calculatedStatus].label}.
          {' '}Karar: <span className={`badge badge--${status.tone}`}>{status.label}</span>
          {data.overrideNote && ` (${data.overrideNote})`}
        </div>
      )}

      <fieldset disabled={!canManage} className="form">
        <div className="form__pair">
          <label className="field"><span>Kontrol tarihi</span><input type="date" required value={inspectedOn} onChange={(event) => setInspectedOn(event.target.value)} /></label>
          <label className="field"><span>Ölçülen metre</span><input type="number" min="0" step="0.01" required value={meters} onChange={(event) => setMeters(event.target.value)} /></label>
          <label className="field"><span>Ölçülen en (cm)</span><input type="number" min="0" step="0.01" required value={widthCm} onChange={(event) => setWidthCm(event.target.value)} /></label>
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
                    <LookupSelect resource="dokuma-defect-types" label="Hata türü" required hideLabel idSuffix={`defect-${row.key}`} value={row.defectTypeId}
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
              <select value={grade} onChange={(event) => setGrade(event.target.value as DokumaFabricRollStatus)}>
                {gradeOptions.map((value) => <option key={value} value={value}>{dokumaRollStatusPresentation[value].label}</option>)}
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

function InspectionDialog({ roll, canManage, canOverride, onClose }: { roll: DokumaFabricRoll; canManage: boolean; canOverride: boolean; onClose: () => void }) {
  const { data, error, isLoading } = useGetDokumaRollInspectionQuery(roll.id)
  return (
    <Modal title={`Ham kontrol: ${roll.rollNo}`} onClose={onClose} variant="drawer">
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {data && <InspectionForm data={data} canManage={canManage} canOverride={canOverride} onClose={onClose} />}
    </Modal>
  )
}

function LimitsForm({ settings, onClose }: { settings: DokumaQualitySettings; onClose: () => void }) {
  const [save, { isLoading, error }] = useSaveDokumaQualitySettingsMutation()
  const [first, setFirst] = useState(String(settings.firstQualityMaxPoints))
  const [second, setSecond] = useState(String(settings.secondQualityMaxPoints))

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await save({ firstQualityMaxPoints: Number(first), secondQualityMaxPoints: Number(second) })
    if (!('error' in result)) {
      onClose()
    }
  }

  return (
    <form className="form" onSubmit={handleSubmit}>
      <p className="muted">100 m² başına puan. Müşteri kartında müşteriye özel sınır verilebilir. Değişiklik yalnızca bundan sonraki kontrollere uygulanır.</p>
      <div className="form__pair">
        <label className="field"><span>1. kalite üst sınırı</span><input type="number" min="0" step="0.01" required value={first} onChange={(event) => setFirst(event.target.value)} /></label>
        <label className="field"><span>2. kalite üst sınırı</span><input type="number" min="0" step="0.01" required value={second} onChange={(event) => setSecond(event.target.value)} /></label>
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
  const { data, error, isLoading } = useGetDokumaQualitySettingsQuery()
  return (
    <Modal title="Kalite sınırları" onClose={onClose}>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {data && <LimitsForm settings={data} onClose={onClose} />}
    </Modal>
  )
}

/** Ham kontrol: kesilen toplar, 4 puan kontrolü ve kalite kararı. */
export function DokumaInspectionPage() {
  const canManage = usePermission(Permissions.DokumaInspectionManage)
  const canOverride = usePermission(Permissions.DokumaInspectionOverride)
  const grid = useGrid()
  const { page, setPage } = grid
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [inspecting, setInspecting] = useState<DokumaFabricRoll | null>(null)
  const [limits, setLimits] = useState(false)
  const { data, error, isLoading, isFetching } = useGetDokumaInspectionRollsQuery({ page, pageSize: PAGE_SIZE, search: search || undefined, ...grid.params })

  const columns: GridColumn[] = [
    { key: 'rollNo', label: 'Top no' },
    { key: 'cutOn', label: 'Kesim', filter: false },
    { key: 'loomCode', label: 'Tezgâh' },
    { key: 'orderNo', label: 'Sipariş' },
    { key: 'customerName', label: 'Müşteri' },
    { key: 'articleCode', label: 'Artikel' },
    { key: 'meters', label: 'Kesim metre', numeric: true },
    { key: 'inspectedMeters', label: 'Ölçülen metre', numeric: true },
    { key: 'pointsPer100M2', label: 'Puan / 100 m²', numeric: true },
    { key: 'status', label: 'Kalite', filter: Object.entries(dokumaRollStatusPresentation).map(([value, { label }]) => ({ value, label })) },
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
        <input type="search" placeholder="Top no, levent, tezgâh, sipariş ya da artikel" aria-label="Top ara" value={searchInput} onChange={(event) => setSearchInput(event.target.value)} />
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
                  const status = dokumaRollStatusPresentation[roll.status]
                  return (
                    <tr key={roll.id}>
                      <td><code>{roll.rollNo}</code></td>
                      <td>{formatDate(roll.cutOn)}</td>
                      <td>{roll.loomCode}</td>
                      <td>{roll.orderNo}</td>
                      <td>{roll.customerName ?? <span className="muted">Stoğa üretim</span>}</td>
                      <td>{roll.articleCode} — {roll.articleName}</td>
                      <td className="table__number">{formatQuantity(roll.meters)}</td>
                      <td className="table__number">{roll.inspectedMeters !== null ? formatQuantity(roll.inspectedMeters) : '—'}</td>
                      <td className="table__number">{roll.pointsPer100M2 !== null ? formatQuantity(roll.pointsPer100M2) : '—'}</td>
                      <td>
                        <span className={`badge badge--${status.tone}`}>{status.label}</span>
                        {roll.overrideNote && <div className="muted">Yetkili kararı</div>}
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
