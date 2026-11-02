import { Plus, Search, X } from 'lucide-react'
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
import { formatQuantity } from '../../utils/format'
import { LookupSelect } from '../masterData/LookupSelect'
import { useDeleteDokumaSizingRecipeMutation, useGetDokumaSizingRecipeQuery, useGetDokumaSizingRecipesQuery, useSaveDokumaSizingRecipeMutation } from './dokumaStockApi'
import type { DokumaSizingRecipe } from './dokumaStockApi'

const PAGE_SIZE = 20

interface LineDraft {
  key: number
  materialId: number | null
  bathPercent: string
}

const text = (value: number | null) => (value === null ? '' : String(value))
const optionalNumber = (value: string) => (value.trim() === '' ? null : Number(value))

function RecipeForm({ recipe, canManage, onClose }: { recipe: DokumaSizingRecipe | null; canManage: boolean; onClose: () => void }) {
  const [save, { isLoading, error }] = useSaveDokumaSizingRecipeMutation()
  const [remove, removeState] = useDeleteDokumaSizingRecipeMutation()
  const [code, setCode] = useState(recipe?.code ?? '')
  const [name, setName] = useState(recipe?.name ?? '')
  const [articleId, setArticleId] = useState<number | null>(recipe?.articleId ?? null)
  const [isDefault, setIsDefault] = useState(recipe?.isDefault ?? false)
  const [isActive, setIsActive] = useState(recipe?.isActive ?? true)
  const [concentration, setConcentration] = useState(text(recipe?.concentrationPercent ?? null))
  const [temperature, setTemperature] = useState(text(recipe?.bathTemperatureC ?? null))
  const [viscosity, setViscosity] = useState(text(recipe?.viscositySeconds ?? null))
  const [speed, setSpeed] = useState(text(recipe?.machineSpeedMetersPerMinute ?? null))
  const [pickUp, setPickUp] = useState(text(recipe?.targetPickUpPercent ?? null))
  const [notes, setNotes] = useState(recipe?.notes ?? '')
  const [lines, setLines] = useState<LineDraft[]>(
    recipe?.lines.map((line) => ({ key: line.id, materialId: line.materialId, bathPercent: String(line.bathPercent) })) ?? [{ key: 1, materialId: null, bathPercent: '' }],
  )
  const [confirmDelete, setConfirmDelete] = useState(false)
  const total = lines.reduce((sum, line) => sum + (Number(line.bathPercent) || 0), 0)
  const update = (key: number, patch: Partial<LineDraft>) => setLines(lines.map((line) => (line.key === key ? { ...line, ...patch } : line)))

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await save({
      id: recipe?.id,
      body: {
        code: code.trim(),
        name: name.trim(),
        articleId: articleId ?? 0,
        isDefault,
        isActive,
        concentrationPercent: optionalNumber(concentration),
        bathTemperatureC: optionalNumber(temperature),
        viscositySeconds: optionalNumber(viscosity),
        machineSpeedMetersPerMinute: optionalNumber(speed),
        targetPickUpPercent: optionalNumber(pickUp),
        notes: notes.trim() || null,
        lines: lines.filter((line) => line.materialId !== null).map((line) => ({ materialId: line.materialId ?? 0, bathPercent: Number(line.bathPercent) })),
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
    <Modal title={recipe ? `Haşıl reçetesi: ${recipe.code}` : 'Yeni haşıl reçetesi'} onClose={onClose} variant="drawer">
      <form className="form" onSubmit={handleSubmit}>
        <fieldset disabled={!canManage} className="form">
          <div className="form__pair">
            <label className="field"><span>Kod</span><input required maxLength={30} value={code} onChange={(event) => setCode(event.target.value)} /></label>
            <label className="field"><span>Ad</span><input required maxLength={200} value={name} onChange={(event) => setName(event.target.value)} /></label>
          </div>
          <LookupSelect resource="dokuma-articles" label="Artikel" required value={articleId} onChange={setArticleId} />
          <div className="form__pair">
            <label className="checkbox"><input type="checkbox" checked={isDefault} onChange={(event) => setIsDefault(event.target.checked)} />Artikelin varsayılan reçetesi</label>
            <label className="checkbox"><input type="checkbox" checked={isActive} onChange={(event) => setIsActive(event.target.checked)} />Etkin</label>
          </div>
          <div className="form__pair">
            <label className="field"><span>Konsantrasyon (%)</span><input type="number" min="0" max="100" step="0.01" value={concentration} onChange={(event) => setConcentration(event.target.value)} /></label>
            <label className="field"><span>Banyo sıcaklığı (°C)</span><input type="number" min="0" max="200" step="0.1" value={temperature} onChange={(event) => setTemperature(event.target.value)} /></label>
          </div>
          <div className="form__pair">
            <label className="field"><span>Viskozite (sn)</span><input type="number" min="0" step="0.1" value={viscosity} onChange={(event) => setViscosity(event.target.value)} /></label>
            <label className="field"><span>Makine hızı (m/dk)</span><input type="number" min="0" step="0.1" value={speed} onChange={(event) => setSpeed(event.target.value)} /></label>
          </div>
          <label className="field">
            <span>Hedef alım (%)</span>
            <input type="number" min="0" max="500" step="0.01" value={pickUp} onChange={(event) => setPickUp(event.target.value)} />
            <small className="field__hint">İpliğin ağırlığına göre aldığı banyo. Levent haşıllanırken kimyasal miktarı tahmini iplik × hedef alım × banyo oranı ile önerilir.</small>
          </label>
          <div className="table-wrap">
            <table className="table">
              <thead><tr><th>Kimyasal</th><th className="table__number">Banyo oranı (%)</th><th aria-label="Sil" /></tr></thead>
              <tbody>
                {lines.map((line) => (
                  <tr key={line.key}>
                    <td>
                      <LookupSelect resource="dokuma-materials" label="Kimyasal" hideLabel idSuffix={`-${line.key}`} required value={line.materialId} onChange={(value) => update(line.key, { materialId: value })} />
                    </td>
                    <td className="table__number">
                      <input aria-label="Banyo oranı" className="matrix-input purchase-input" type="number" min="0" max="100" step="0.001" required value={line.bathPercent} onChange={(event) => update(line.key, { bathPercent: event.target.value })} />
                    </td>
                    <td><button type="button" className="button" aria-label="Satırı sil" onClick={() => setLines(lines.filter((row) => row.key !== line.key))}><X aria-hidden="true" /></button></td>
                  </tr>
                ))}
                {lines.length === 0 && <tr><td colSpan={3} className="table__empty">Kimyasal girilmedi.</td></tr>}
              </tbody>
              <tfoot><tr><th scope="row">Toplam</th><td className="table__number">%{formatQuantity(total)}</td><td /></tr></tfoot>
            </table>
          </div>
          <div><button type="button" className="button" onClick={() => setLines([...lines, { key: Date.now(), materialId: null, bathPercent: '' }])}><Plus aria-hidden="true" />Kimyasal ekle</button></div>
          <label className="field"><span>Not</span><textarea rows={2} maxLength={500} value={notes} onChange={(event) => setNotes(event.target.value)} /></label>
        </fieldset>
        <ErrorAlert error={error ?? removeState.error} />
        <footer className="form__actions">
          {canManage && recipe && !confirmDelete && <button type="button" className="button button--danger-ghost" onClick={() => setConfirmDelete(true)}>Sil</button>}
          {canManage && recipe && confirmDelete && <button type="button" className="button button--danger-ghost" disabled={removeState.isLoading} onClick={handleDelete}>Silmeyi onayla</button>}
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          {canManage && <button type="submit" className="button button--primary" disabled={isLoading}>Kaydet</button>}
        </footer>
      </form>
    </Modal>
  )
}

/** Kayıtlı reçete, satırlarıyla birlikte yüklenince açılır. */
function RecipeLoader({ id, canManage, onClose }: { id: number; canManage: boolean; onClose: () => void }) {
  const { data, error } = useGetDokumaSizingRecipeQuery(id)
  if (!data) {
    return error ? <ErrorAlert error={error} /> : null
  }
  return <RecipeForm recipe={data} canManage={canManage} onClose={onClose} />
}

/** Haşıl reçeteleri: artikel başına bir ya da birkaç reçete, biri varsayılan. */
export function DokumaSizingRecipesPage() {
  const canManage = usePermission(Permissions.DokumaMaterialManage)
  const grid = useGrid()
  const { page, setPage } = grid
  const [input, setInput] = useState('')
  const [search, setSearch] = useState('')
  const [articleId, setArticleId] = useState<number | null>(null)
  const [editing, setEditing] = useState<number | null | undefined>(undefined)
  const { data, error, isLoading, isFetching } = useGetDokumaSizingRecipesQuery({ page, pageSize: PAGE_SIZE, search: search || undefined, articleId: articleId ?? undefined, ...grid.params })

  const columns: GridColumn[] = [
    { key: 'code', label: 'Kod' },
    { key: 'name', label: 'Ad' },
    { key: 'articleCode', label: 'Artikel' },
    { key: 'concentrationPercent', label: 'Konsantrasyon (%)', numeric: true, filter: false },
    { key: 'targetPickUpPercent', label: 'Hedef alım (%)', numeric: true, filter: false },
    { key: 'lineCount', label: 'Kimyasal', numeric: true, filter: false },
    { key: 'isDefault', label: 'Varsayılan', filter: false },
    { key: 'isActive', label: 'Durum', filter: false },
  ]

  const submit = (event: FormEvent) => {
    event.preventDefault()
    setPage(1)
    setSearch(input.trim())
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">Haşıl Reçeteleri</h1>
      <form className="toolbar" onSubmit={submit} role="search">
        <input type="search" placeholder="Reçete kodu, adı ya da artikel" aria-label="Reçete ara" value={input} onChange={(event) => setInput(event.target.value)} />
        <div style={{ minWidth: '16rem' }}>
          <LookupSelect resource="dokuma-articles" label="Artikel" hideLabel value={articleId} onChange={(value) => { setArticleId(value); setPage(1) }} placeholder="Bütün artikeller" />
        </div>
        <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
        <ClearFiltersButton grid={grid} />
        <div className="toolbar__actions">
          {canManage && <button type="button" className="button button--primary" onClick={() => setEditing(null)}><Plus aria-hidden="true" />Yeni reçete</button>}
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
                {data.items.map((row) => (
                  <tr key={row.id} className="table__row--link" onClick={() => setEditing(row.id)}>
                    <td><code>{row.code}</code></td>
                    <td>{row.name}</td>
                    <td>{row.articleCode} — {row.articleName}</td>
                    <td className="table__number">{row.concentrationPercent === null ? '—' : formatQuantity(row.concentrationPercent)}</td>
                    <td className="table__number">{row.targetPickUpPercent === null ? '—' : formatQuantity(row.targetPickUpPercent)}</td>
                    <td className="table__number">{row.lineCount}</td>
                    <td>{row.isDefault ? <span className="badge badge--info">Varsayılan</span> : ''}</td>
                    <td>{row.isActive ? <span className="badge badge--ok">Etkin</span> : <span className="badge badge--neutral">Pasif</span>}</td>
                  </tr>
                ))}
                {data.items.length === 0 && <tr><td colSpan={columns.length} className="table__empty">Haşıl reçetesi bulunamadı.</td></tr>}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}
      {editing === null && <RecipeForm recipe={null} canManage={canManage} onClose={() => setEditing(undefined)} />}
      {typeof editing === 'number' && <RecipeLoader id={editing} canManage={canManage} onClose={() => setEditing(undefined)} />}
    </section>
  )
}
