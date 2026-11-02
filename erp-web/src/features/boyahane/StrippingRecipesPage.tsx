import { ArrowDown, ArrowUp, Plus, Search, X } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { ErrorAlert } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { Pagination } from '../../components/Pagination'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatRecipeAmount } from '../../utils/format'
import { LookupSelect } from '../masterData/LookupSelect'
import {
  recipeAmountUnitLabels,
  useDeleteBoyahaneStrippingRecipeMutation,
  useGetBoyahaneStrippingRecipesQuery,
  useSaveBoyahaneStrippingRecipeMutation,
} from './boyahaneArchiveApi'
import type { BoyahaneStrippingRecipe, BoyahaneStrippingRecipeLine, RecipeAmountUnit } from './boyahaneArchiveApi'

const PAGE_SIZE = 20

interface LineDraft {
  key: number
  dyeChemicalId: number | null
  amount: string
  unit: RecipeAmountUnit
  temperature: string
  duration: string
}

const numberOrNull = (value: string) => (value.trim() === '' ? null : Number(value))

/** Satırın kısa gösterimi: kimyasal, miktar, sıcaklık ve süre. */
export function formatStrippingLine(line: Pick<BoyahaneStrippingRecipeLine, 'code' | 'amount' | 'unit' | 'temperatureCelsius' | 'durationMinutes'>): string {
  const conditions = [
    line.temperatureCelsius !== null ? `${line.temperatureCelsius} °C` : null,
    line.durationMinutes !== null ? `${line.durationMinutes} dk` : null,
  ].filter(Boolean).join(', ')
  return `${line.code} ${formatRecipeAmount(line.amount)} ${recipeAmountUnitLabels[line.unit]}${conditions ? ` (${conditions})` : ''}`
}

function StrippingRecipeDialog({ recipe, onClose }: { recipe: BoyahaneStrippingRecipe | null; onClose: () => void }) {
  const [save, { isLoading, error }] = useSaveBoyahaneStrippingRecipeMutation()
  const [remove, removeState] = useDeleteBoyahaneStrippingRecipeMutation()
  const [code, setCode] = useState(recipe?.code ?? '')
  const [name, setName] = useState(recipe?.name ?? '')
  const [notes, setNotes] = useState(recipe?.notes ?? '')
  const [isActive, setIsActive] = useState(recipe?.isActive ?? true)
  const [nextKey, setNextKey] = useState(-1)
  const [lines, setLines] = useState<LineDraft[]>(recipe
    ? recipe.lines.map((line) => ({
      key: line.id,
      dyeChemicalId: line.dyeChemicalId,
      amount: String(line.amount),
      unit: line.unit,
      temperature: line.temperatureCelsius === null ? '' : String(line.temperatureCelsius),
      duration: line.durationMinutes === null ? '' : String(line.durationMinutes),
    }))
    : [{ key: 0, dyeChemicalId: null, amount: '', unit: 'GRAMS_PER_LITER', temperature: '', duration: '' }])

  const update = (key: number, change: Partial<LineDraft>) =>
    setLines(lines.map((line) => (line.key === key ? { ...line, ...change } : line)))

  const move = (index: number, delta: number) => {
    const next = [...lines]
    const [item] = next.splice(index, 1)
    next.splice(index + delta, 0, item)
    setLines(next)
  }

  const addLine = () => {
    setLines([...lines, { key: nextKey, dyeChemicalId: null, amount: '', unit: 'GRAMS_PER_LITER', temperature: '', duration: '' }])
    setNextKey(nextKey - 1)
  }

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await save({
      id: recipe?.id,
      body: {
        code,
        name,
        notes: notes.trim() || null,
        isActive,
        lines: lines.map((line) => ({
          dyeChemicalId: line.dyeChemicalId ?? 0,
          amount: Number(line.amount),
          unit: line.unit,
          temperatureCelsius: numberOrNull(line.temperature),
          durationMinutes: numberOrNull(line.duration),
        })),
      },
    })
    if (!('error' in result)) {
      onClose()
    }
  }

  const handleDelete = async () => {
    if (recipe && !('error' in await remove(recipe.id))) {
      onClose()
    }
  }

  return (
    <Modal title={recipe ? `Söküm reçetesi: ${recipe.code}` : 'Yeni söküm reçetesi'} onClose={onClose} variant="drawer">
      <form className="form" onSubmit={handleSubmit}>
        <div className="form__pair">
          <label className="field"><span>Kod</span><input required maxLength={30} value={code} onChange={(event) => setCode(event.target.value)} /></label>
          <label className="field"><span>Ad</span><input required maxLength={100} value={name} onChange={(event) => setName(event.target.value)} /></label>
        </div>
        <p className="muted form__note">Miktar flotte suyuna göre g/L ya da mal ağırlığına göre % yazılır; satırlar bu sırayla uygulanır.</p>
        <div className="table-wrap recipe-table">
          <table className="table table--compact">
            <thead>
              <tr><th>Sıra</th><th>Kimyasal</th><th>Miktar</th><th>Ölçü</th><th>Sıcaklık (°C)</th><th>Süre (dk)</th><th aria-label="İşlemler" /></tr>
            </thead>
            <tbody>
              {lines.map((line, index) => (
                <tr key={line.key}>
                  <td>{index + 1}</td>
                  <td>
                    <LookupSelect resource="boyahane-dye-chemicals" label="Kimyasal" hideLabel idSuffix={`s${line.key}`} required value={line.dyeChemicalId} onChange={(value) => update(line.key, { dyeChemicalId: value })} />
                  </td>
                  <td><input aria-label="Miktar" type="number" min="0" step="0.0001" required value={line.amount} onChange={(event) => update(line.key, { amount: event.target.value })} /></td>
                  <td>
                    <select aria-label="Ölçü" value={line.unit} onChange={(event) => update(line.key, { unit: event.target.value as RecipeAmountUnit })}>
                      {Object.entries(recipeAmountUnitLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
                    </select>
                  </td>
                  <td><input aria-label="Sıcaklık" type="number" min="0" max="150" step="1" value={line.temperature} onChange={(event) => update(line.key, { temperature: event.target.value })} /></td>
                  <td><input aria-label="Süre" type="number" min="1" max="1440" step="1" value={line.duration} onChange={(event) => update(line.key, { duration: event.target.value })} /></td>
                  <td>
                    <button type="button" className="icon-button" aria-label="Yukarı" disabled={index === 0} onClick={() => move(index, -1)}><ArrowUp aria-hidden="true" /></button>
                    <button type="button" className="icon-button" aria-label="Aşağı" disabled={index === lines.length - 1} onClick={() => move(index, 1)}><ArrowDown aria-hidden="true" /></button>
                    <button type="button" className="icon-button" aria-label="Satırı kaldır" disabled={lines.length === 1} onClick={() => setLines(lines.filter((row) => row.key !== line.key))}><X aria-hidden="true" /></button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <div><button type="button" className="button" onClick={addLine}><Plus aria-hidden="true" />Satır ekle</button></div>
        <label className="field"><span>Not</span><textarea rows={2} maxLength={500} value={notes} onChange={(event) => setNotes(event.target.value)} /></label>
        <label className="checkbox"><input type="checkbox" checked={isActive} onChange={(event) => setIsActive(event.target.checked)} />Aktif</label>
        <p className="muted">Reçete değişikliği yalnızca bundan sonra bitirilen söküm adımlarının tüketimini etkiler.</p>
        <ErrorAlert error={error ?? removeState.error} />
        <footer className="form__actions">
          {recipe && <button type="button" className="button button--danger-ghost" disabled={removeState.isLoading} onClick={handleDelete}>Sil</button>}
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading}>Kaydet</button>
        </footer>
      </form>
    </Modal>
  )
}

/** Söküm reçeteleri. Renk kontrolünde "söküm + yeniden boya" seçilince reçete eklenen söküm adımına bağlanır. */
export function StrippingRecipesPage() {
  const canManage = usePermission(Permissions.BoyahaneLabManage)
  const [page, setPage] = useState(1)
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [editing, setEditing] = useState<BoyahaneStrippingRecipe | null | undefined>(undefined)
  const { data, error, isLoading, isFetching } = useGetBoyahaneStrippingRecipesQuery({ page, pageSize: PAGE_SIZE, search: search || undefined })

  const handleSearch = (event: FormEvent) => {
    event.preventDefault()
    setPage(1)
    setSearch(searchInput.trim())
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">Söküm Reçeteleri</h1>
      <form className="toolbar" onSubmit={handleSearch} role="search">
        <input type="search" placeholder="Kod ya da ad" aria-label="Söküm reçetesi ara" value={searchInput} onChange={(event) => setSearchInput(event.target.value)} />
        <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
        <div className="toolbar__actions">
          {canManage && <button type="button" className="button button--primary" onClick={() => setEditing(null)}><Plus aria-hidden="true" />Yeni söküm reçetesi</button>}
        </div>
      </form>

      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />

      {data && (
        <>
          <div className="table-wrap" aria-busy={isFetching}>
            <table className="table">
              <thead><tr><th>Kod</th><th>Ad</th><th>Satırlar</th><th className="table__number">Kullanım</th><th>Durum</th></tr></thead>
              <tbody>
                {data.items.map((recipe) => (
                  <tr key={recipe.id} className={canManage ? 'table__row--link' : undefined} onClick={() => canManage && setEditing(recipe)}>
                    <td><code>{recipe.code}</code></td>
                    <td>{recipe.name}{recipe.notes && <div className="muted">{recipe.notes}</div>}</td>
                    <td>{recipe.lines.map(formatStrippingLine).join(' → ')}</td>
                    <td className="table__number">{recipe.usageCount}</td>
                    <td>{recipe.isActive ? <span className="badge badge--ok">Aktif</span> : <span className="badge badge--neutral">Pasif</span>}</td>
                  </tr>
                ))}
                {data.items.length === 0 && <tr><td colSpan={5} className="table__empty">Söküm reçetesi bulunamadı.</td></tr>}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}
      {editing !== undefined && <StrippingRecipeDialog recipe={editing} onClose={() => setEditing(undefined)} />}
    </section>
  )
}
