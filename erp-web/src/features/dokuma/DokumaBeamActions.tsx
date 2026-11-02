import { Plus, Printer, X } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { formatQuantity, todayIsoDate } from '../../utils/format'
import { LookupSelect } from '../masterData/LookupSelect'
import { unitLabel } from './dokumaPresentation'
import {
  drawingMethodLabels,
  useGetDokumaBeamSizingPlanQuery,
  useMarkDokumaBeamReadyMutation,
  useMarkDokumaBeamSizedMutation,
  useReceiveDokumaBeamFromSizingMutation,
  useSendDokumaBeamToSizingMutation,
  useUndoDokumaBeamReadyMutation,
} from './dokumaWarpApi'
import type { DokumaBeam, DokumaBeamSizingPlan, DokumaSizingPlanRecipe, DrawingMethod } from './dokumaWarpApi'

type Action = 'send' | 'receive' | 'sized' | 'ready'

const titles: Record<Action, string> = {
  send: 'Haşılcıya gönder',
  receive: 'Haşıldan geri al',
  sized: 'İçeride haşıllandı',
  ready: 'Tahar / düğüm',
}

const optionalNumber = (text: string) => (text.trim() === '' ? null : Number(text))

/** Seçilen reçetenin değerleri: konsantrasyon, sıcaklık, viskozite, hız ve hedef alım. */
function RecipeSummary({ recipe, plan }: { recipe: DokumaSizingPlanRecipe; plan: DokumaBeamSizingPlan }) {
  const parts = [
    recipe.concentrationPercent !== null && `Konsantrasyon %${recipe.concentrationPercent}`,
    recipe.bathTemperatureC !== null && `${recipe.bathTemperatureC} °C`,
    recipe.viscositySeconds !== null && `Viskozite ${recipe.viscositySeconds} sn`,
    recipe.machineSpeedMetersPerMinute !== null && `${recipe.machineSpeedMetersPerMinute} m/dk`,
    recipe.targetPickUpPercent !== null && `Hedef alım %${recipe.targetPickUpPercent}`,
  ].filter(Boolean)
  return (
    <p className="muted">
      {parts.join(' · ') || 'Reçetede banyo değeri girilmemiş.'}
      {plan.estimatedYarnKilograms !== null && ` · Tahmini çözgü ipliği ${formatQuantity(plan.estimatedYarnKilograms)} kg`}
    </p>
  )
}

/** Reçete seçimi: artikelin etkin reçeteleri, varsayılan önce gelir. */
function RecipeSelect({ plan, value, onChange }: { plan: DokumaBeamSizingPlan | undefined; value: number | null; onChange: (id: number | null) => void }) {
  return (
    <label className="field">
      <span>Haşıl reçetesi</span>
      <select value={value ?? ''} onChange={(event) => onChange(event.target.value === '' ? null : Number(event.target.value))} disabled={!plan}>
        <option value="">— Reçetesiz —</option>
        {plan?.recipes.map((recipe) => (
          <option key={recipe.id} value={recipe.id}>{recipe.code} — {recipe.name}{recipe.isDefault ? ' (varsayılan)' : ''}</option>
        ))}
      </select>
      {plan && plan.recipes.length === 0 && <small className="field__hint">{plan.articleCode} artikelinin haşıl reçetesi tanımlı değil.</small>}
    </label>
  )
}

interface ChemicalRow {
  key: number
  materialId: number | null
  /** Reçeteden gelen satırda kod, ad ve birim gösterilir; eklenen satırda malzeme seçilir. */
  label: string | null
  unit: string | null
  available: number | null
  quantity: string
}

/** Reçete satırlarından kimyasal tablosu: miktar tahminle dolar, düzeltilebilir. */
function chemicalRows(recipe: DokumaSizingPlanRecipe | undefined): ChemicalRow[] {
  return (recipe?.lines ?? []).map((line) => ({
    key: line.materialId,
    materialId: line.materialId,
    label: `${line.materialCode} — ${line.materialName} (%${line.bathPercent})`,
    unit: line.unit,
    available: line.availableQuantity,
    quantity: line.estimatedQuantity === null ? '' : String(line.estimatedQuantity),
  }))
}

function BeamActionDialog({ beam, action, onClose }: { beam: DokumaBeam; action: Action; onClose: () => void }) {
  const [send, sendState] = useSendDokumaBeamToSizingMutation()
  const [receive, receiveState] = useReceiveDokumaBeamFromSizingMutation()
  const [markSized, sizedState] = useMarkDokumaBeamSizedMutation()
  const [markReady, readyState] = useMarkDokumaBeamReadyMutation()
  const usesRecipe = action === 'send' || action === 'sized'
  const { data: plan, error: planError } = useGetDokumaBeamSizingPlanQuery(beam.id, { skip: !usesRecipe })
  const [date, setDate] = useState(todayIsoDate())
  const [supplierId, setSupplierId] = useState<number | null>(beam.sizingSupplierId)
  const [method, setMethod] = useState<DrawingMethod>('DRAWING')
  const [recipeId, setRecipeId] = useState<number | null | undefined>(undefined)
  const [pickUp, setPickUp] = useState('')
  const [viscosity, setViscosity] = useState('')
  const [chemicals, setChemicals] = useState<ChemicalRow[] | null>(null)
  const busy = sendState.isLoading || receiveState.isLoading || sizedState.isLoading || readyState.isLoading

  // Reçete, plan gelince leventin reçetesi ya da artikelin varsayılanıyla başlar.
  const selectedId = recipeId === undefined ? plan?.currentRecipeId ?? null : recipeId
  const recipe = plan?.recipes.find((item) => item.id === selectedId)
  const rows = chemicals ?? chemicalRows(recipe)

  const chooseRecipe = (id: number | null) => {
    setRecipeId(id)
    setChemicals(chemicalRows(plan?.recipes.find((item) => item.id === id)))
  }
  const updateRow = (key: number, patch: Partial<ChemicalRow>) => setChemicals(rows.map((row) => (row.key === key ? { ...row, ...patch } : row)))

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = action === 'send'
      ? await send({ id: beam.id, supplierId: supplierId ?? 0, date, sizingRecipeId: selectedId })
      : action === 'receive'
        ? await receive({ id: beam.id, date, pickUpPercent: optionalNumber(pickUp), viscositySeconds: optionalNumber(viscosity) })
        : action === 'sized'
          ? await markSized({
            id: beam.id,
            date,
            sizingRecipeId: selectedId,
            pickUpPercent: optionalNumber(pickUp),
            viscositySeconds: optionalNumber(viscosity),
            chemicals: rows
              .filter((row) => row.materialId !== null && Number(row.quantity) > 0)
              .map((row) => ({ materialId: row.materialId ?? 0, quantity: Number(row.quantity) })),
          })
          : await markReady({ id: beam.id, drawingMethod: method, date })
    if (!('error' in result)) {
      onClose()
    }
  }

  return (
    <Modal title={`${titles[action]}: ${beam.beamNo}`} onClose={onClose} variant={action === 'sized' ? 'drawer' : 'dialog'}>
      <form className="form" onSubmit={handleSubmit}>
        {action === 'send' && <LookupSelect resource="dokuma-suppliers" label="Haşılcı" required value={supplierId} onChange={setSupplierId} />}
        {action === 'ready' && (
          <label className="field">
            <span>Hazırlık</span>
            <select value={method} onChange={(event) => setMethod(event.target.value as DrawingMethod)}>
              {Object.entries(drawingMethodLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
            </select>
          </label>
        )}
        <label className="field">
          <span>Tarih</span>
          <input type="date" required value={date} onChange={(event) => setDate(event.target.value)} />
        </label>
        {usesRecipe && (
          <>
            <RecipeSelect plan={plan} value={selectedId} onChange={chooseRecipe} />
            {plan && recipe && <RecipeSummary recipe={recipe} plan={plan} />}
            {action === 'send' && <p className="muted">Fasonda reçete bilgi amaçlıdır; haşıl belgesine basılır, stoktan düşüm yapılmaz.</p>}
          </>
        )}
        {(action === 'sized' || action === 'receive') && (
          <div className="form__pair">
            <label className="field">
              <span>Gerçekleşen alım (%)</span>
              <input type="number" min="0" max="500" step="0.01" value={pickUp} onChange={(event) => setPickUp(event.target.value)} placeholder={action === 'receive' ? 'İsteğe bağlı' : undefined} />
            </label>
            <label className="field">
              <span>Viskozite (sn)</span>
              <input type="number" min="0" step="0.1" value={viscosity} onChange={(event) => setViscosity(event.target.value)} placeholder={action === 'receive' ? 'İsteğe bağlı' : undefined} />
            </label>
          </div>
        )}
        {action === 'sized' && (
          <>
            <div className="table-wrap">
              <table className="table">
                <thead><tr><th>Kimyasal</th><th className="table__number">Stok</th><th className="table__number">Kullanılan</th><th aria-label="Sil" /></tr></thead>
                <tbody>
                  {rows.map((row) => (
                    <tr key={row.key}>
                      <td>
                        {row.label ?? (
                          <LookupSelect resource="dokuma-materials" label="Kimyasal" hideLabel idSuffix={`-${row.key}`} required value={row.materialId}
                            onChange={(value) => updateRow(row.key, { materialId: value })} />
                        )}
                      </td>
                      <td className="table__number">{row.available === null ? '—' : `${formatQuantity(row.available)} ${unitLabel(row.unit)}`}</td>
                      <td className="table__number">
                        <input aria-label="Kullanılan miktar" className="matrix-input purchase-input" type="number" min="0" step="0.001" value={row.quantity} onChange={(event) => updateRow(row.key, { quantity: event.target.value })} />
                      </td>
                      <td><button type="button" className="button" aria-label="Satırı sil" onClick={() => setChemicals(rows.filter((item) => item.key !== row.key))}><X aria-hidden="true" /></button></td>
                    </tr>
                  ))}
                  {rows.length === 0 && <tr><td colSpan={4} className="table__empty">Kimyasal tüketimi girilmeyecek.</td></tr>}
                </tbody>
              </table>
            </div>
            <div>
              <button type="button" className="button" onClick={() => setChemicals([...rows, { key: Date.now(), materialId: null, label: null, unit: null, available: null, quantity: '' }])}>
                <Plus aria-hidden="true" />Kimyasal ekle
              </button>
            </div>
            <p className="muted">Miktarlar reçeteden (tahmini iplik × hedef alım × banyo oranı) gelir; gerçekte kullanılanı girin. Girilen miktar malzeme stoğundan en eski lottan başlayarak düşülür; boş ya da sıfır satır düşülmez.</p>
          </>
        )}
        <ErrorAlert error={planError ?? sendState.error ?? receiveState.error ?? sizedState.error ?? readyState.error} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={busy}>Kaydet</button>
        </footer>
      </form>
    </Modal>
  )
}

/** Leventin durumuna göre haşıl ve tahar / düğüm işlemleri. */
export function DokumaBeamActions({ beam }: { beam: DokumaBeam }) {
  const [action, setAction] = useState<Action | null>(null)
  const [undo, undoState] = useUndoDokumaBeamReadyMutation()
  const warped = beam.status === 'WARPED'
  const needsSizing = beam.sizingMode !== 'NONE' && !beam.isSized
  const hasDocument = (beam.sizingMode === 'COMMISSION' && (beam.status === 'AT_SIZING' || beam.isSized)) || (beam.sizingMode === 'IN_HOUSE' && beam.isSized)

  return (
    <>
      {warped && beam.sizingMode === 'COMMISSION' && !beam.isSized && (
        <button type="button" className="button" onClick={() => setAction('send')}>Haşılcıya gönder</button>
      )}
      {beam.status === 'AT_SIZING' && <button type="button" className="button" onClick={() => setAction('receive')}>Geri al</button>}
      {warped && beam.sizingMode === 'IN_HOUSE' && !beam.isSized && (
        <button type="button" className="button" onClick={() => setAction('sized')}>Haşıllandı</button>
      )}
      {hasDocument && (
        <Link className="button" to={`/dokuma/beams/${beam.id}/sizing-document`} title="Haşıl belgesi"><Printer aria-hidden="true" />Belge</Link>
      )}
      {warped && !needsSizing && <button type="button" className="button" onClick={() => setAction('ready')}>Tahar / düğüm</button>}
      {beam.status === 'READY' && (
        <button type="button" className="button" disabled={undoState.isLoading} onClick={() => undo(beam.id)}>Hazırı geri al</button>
      )}
      <ErrorAlert error={undoState.error} />
      {action && <BeamActionDialog beam={beam} action={action} onClose={() => setAction(null)} />}
    </>
  )
}
