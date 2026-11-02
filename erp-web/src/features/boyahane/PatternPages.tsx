import { Pencil, Plus, Search, X } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { ClearFiltersButton } from '../../components/grid/ClearFiltersButton'
import { GridHead } from '../../components/grid/GridHead'
import type { GridColumn } from '../../components/grid/GridHead'
import { useGrid } from '../../components/grid/useGrid'
import { Pagination } from '../../components/Pagination'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatQuantity, formatRecipeAmount } from '../../utils/format'
import { LookupSelect } from '../masterData/LookupSelect'
import { ArchiveImagePanel } from './ArchiveImagePanel'
import {
  printTypeLabels,
  useDeleteBoyahanePatternColorwayMutation,
  useDeleteBoyahanePatternImageMutation,
  useDeleteBoyahanePatternMutation,
  useGetBoyahanePatternColorwaysQuery,
  useGetBoyahanePatternImageQuery,
  useGetBoyahanePatternQuery,
  useGetBoyahanePatternsQuery,
  useSaveBoyahanePatternColorwayMutation,
  useSaveBoyahanePatternImageMutation,
  useSaveBoyahanePatternMutation,
} from './boyahaneArchiveApi'
import type { BoyahaneColorway, BoyahanePattern, PrintType } from './boyahaneArchiveApi'

const PAGE_SIZE = 20

const numberOrNull = (value: string) => (value.trim() === '' ? null : Number(value))

const toInput = (value: number | null | undefined) => (value === null || value === undefined ? '' : String(value))

function formatRepeat(pattern: BoyahanePattern): string {
  return pattern.repeatWidthCm === null && pattern.repeatHeightCm === null
    ? '—'
    : `${pattern.repeatWidthCm === null ? '?' : formatQuantity(pattern.repeatWidthCm)} × ${pattern.repeatHeightCm === null ? '?' : formatQuantity(pattern.repeatHeightCm)} cm`
}

/** Yeni desen kartı ya da kartın düzenlenmesi. Görüntü ve renk yolları desenin sayfasından yönetilir. */
function PatternDialog({ pattern, onClose, onSaved, onDeleted }: {
  pattern: BoyahanePattern | null
  onClose: () => void
  onSaved?: (id: number) => void
  onDeleted?: () => void
}) {
  const [save, { isLoading, error }] = useSaveBoyahanePatternMutation()
  const [remove, removeState] = useDeleteBoyahanePatternMutation()
  const [code, setCode] = useState(pattern?.code ?? '')
  const [name, setName] = useState(pattern?.name ?? '')
  const [customerId, setCustomerId] = useState<number | null>(pattern?.customerId ?? null)
  const [printType, setPrintType] = useState<PrintType>(pattern?.printType ?? 'PIGMENT')
  const [repeatWidth, setRepeatWidth] = useState(toInput(pattern?.repeatWidthCm))
  const [repeatHeight, setRepeatHeight] = useState(toInput(pattern?.repeatHeightCm))
  const [screenCount, setScreenCount] = useState(toInput(pattern?.screenCount))
  const [notes, setNotes] = useState(pattern?.notes ?? '')
  const [isActive, setIsActive] = useState(pattern?.isActive ?? true)

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await save({
      id: pattern?.id,
      body: {
        code,
        name,
        customerId,
        printType,
        repeatWidthCm: numberOrNull(repeatWidth),
        repeatHeightCm: numberOrNull(repeatHeight),
        screenCount: numberOrNull(screenCount),
        notes: notes.trim() || null,
        isActive,
      },
    })
    if ('data' in result && result.data) {
      onSaved?.(result.data.id)
      onClose()
    }
  }

  const handleDelete = async () => {
    if (pattern && !('error' in await remove(pattern.id))) {
      onClose()
      onDeleted?.()
    }
  }

  return (
    <Modal title={pattern ? `Desen kartı: ${pattern.code}` : 'Yeni desen kartı'} onClose={onClose}>
      <form className="form" onSubmit={handleSubmit}>
        <div className="form__pair">
          <label className="field"><span>Kod</span><input required maxLength={30} value={code} onChange={(event) => setCode(event.target.value)} /></label>
          <label className="field"><span>Ad</span><input required maxLength={100} value={name} onChange={(event) => setName(event.target.value)} /></label>
        </div>
        <LookupSelect resource="boyahane-customers" label="Müşteri (boşsa genel desen)" value={customerId} onChange={setCustomerId} />
        <div className="form__pair">
          <label className="field">
            <span>Baskı türü</span>
            <select value={printType} onChange={(event) => setPrintType(event.target.value as PrintType)}>
              {Object.entries(printTypeLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
            </select>
          </label>
          <label className="field"><span>Şablon / silindir sayısı</span><input type="number" min="1" max="50" step="1" value={screenCount} onChange={(event) => setScreenCount(event.target.value)} /></label>
        </div>
        <div className="form__pair">
          <label className="field"><span>Rapor eni (cm)</span><input type="number" min="0" step="0.01" value={repeatWidth} onChange={(event) => setRepeatWidth(event.target.value)} /></label>
          <label className="field"><span>Rapor boyu (cm)</span><input type="number" min="0" step="0.01" value={repeatHeight} onChange={(event) => setRepeatHeight(event.target.value)} /></label>
        </div>
        <label className="field"><span>Not</span><textarea rows={2} maxLength={500} value={notes} onChange={(event) => setNotes(event.target.value)} /></label>
        <label className="checkbox"><input type="checkbox" checked={isActive} onChange={(event) => setIsActive(event.target.checked)} />Aktif</label>
        <ErrorAlert error={error ?? removeState.error} />
        <footer className="form__actions">
          {pattern && <button type="button" className="button button--danger-ghost" disabled={removeState.isLoading} onClick={handleDelete}>Sil</button>}
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading}>Kaydet</button>
        </footer>
      </form>
    </Modal>
  )
}

interface LineDraft {
  key: number
  dyeChemicalId: number | null
  gramsPerKilogram: string
}

interface ColorDraft {
  key: number
  position: string
  colorCardId: number | null
  name: string
  paste: string
  lines: LineDraft[]
}

let draftKey = 0
const nextKey = () => --draftKey

const emptyColor = (position: number): ColorDraft => ({
  key: nextKey(), position: String(position), colorCardId: null, name: '', paste: '', lines: [{ key: nextKey(), dyeChemicalId: null, gramsPerKilogram: '' }],
})

/**
 * Renk yolu ve baskı reçetesi: her renk bir şablon / silindirdir. Reçete kg pasta başına gram (g/kg) yazılır; metre başına
 * pasta, baskı partisinde gereken pastayı ve boya / kimyasal tüketimini verir.
 */
function ColorwayDialog({ patternId, colorway, onClose }: { patternId: number; colorway: BoyahaneColorway | null; onClose: () => void }) {
  const [save, { isLoading, error }] = useSaveBoyahanePatternColorwayMutation()
  const [remove, removeState] = useDeleteBoyahanePatternColorwayMutation()
  const [code, setCode] = useState(colorway?.code ?? '')
  const [name, setName] = useState(colorway?.name ?? '')
  const [isActive, setIsActive] = useState(colorway?.isActive ?? true)
  const [colors, setColors] = useState<ColorDraft[]>(colorway
    ? colorway.colors.map((color) => ({
      key: color.id,
      position: String(color.position),
      colorCardId: color.colorCardId,
      name: color.name,
      paste: toInput(color.pasteKilogramsPerMeter),
      lines: color.lines.map((line) => ({ key: line.id, dyeChemicalId: line.dyeChemicalId, gramsPerKilogram: String(line.gramsPerKilogram) })),
    }))
    : [emptyColor(1)])

  const updateColor = (key: number, change: Partial<ColorDraft>) =>
    setColors(colors.map((color) => (color.key === key ? { ...color, ...change } : color)))

  const updateLine = (color: ColorDraft, key: number, change: Partial<LineDraft>) =>
    updateColor(color.key, { lines: color.lines.map((line) => (line.key === key ? { ...line, ...change } : line)) })

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await save({
      patternId,
      colorwayId: colorway?.id,
      body: {
        code,
        name,
        isActive,
        colors: colors.map((color) => ({
          position: Number(color.position),
          colorCardId: color.colorCardId,
          name: color.name.trim() || null,
          pasteKilogramsPerMeter: numberOrNull(color.paste),
          lines: color.lines.filter((line) => line.dyeChemicalId !== null).map((line) => ({ dyeChemicalId: line.dyeChemicalId as number, gramsPerKilogram: Number(line.gramsPerKilogram) })),
        })),
      },
    })
    if (!('error' in result)) {
      onClose()
    }
  }

  const handleDelete = async () => {
    if (colorway && !('error' in await remove({ patternId, colorwayId: colorway.id }))) {
      onClose()
    }
  }

  return (
    <Modal title={colorway ? `Renk yolu: ${colorway.code}` : 'Yeni renk yolu'} onClose={onClose} variant="drawer">
      <form className="form" onSubmit={handleSubmit}>
        <div className="form__pair">
          <label className="field"><span>Kod</span><input required maxLength={30} value={code} onChange={(event) => setCode(event.target.value)} /></label>
          <label className="field"><span>Ad</span><input required maxLength={100} value={name} onChange={(event) => setName(event.target.value)} /></label>
        </div>
        <label className="checkbox"><input type="checkbox" checked={isActive} onChange={(event) => setIsActive(event.target.checked)} />Aktif</label>
        <p className="muted form__note">Reçete kg pasta başına gram (g/kg) yazılır. Metre başına pasta girilmeyen renk için baskıda tüketim hesaplanmaz.</p>

        {colors.map((color) => (
          <fieldset key={color.key} className="form__section">
            <legend>{color.position || '?'}. renk</legend>
            <div className="form__pair">
              <label className="field"><span>Sıra</span><input type="number" min="1" max="99" step="1" required value={color.position} onChange={(event) => updateColor(color.key, { position: event.target.value })} /></label>
              <LookupSelect resource="boyahane-color-cards" label="Renk kartı" idSuffix={`c${color.key}`} value={color.colorCardId} onChange={(value) => updateColor(color.key, { colorCardId: value })} />
            </div>
            <div className="form__pair">
              <label className="field"><span>Renk adı{color.colorCardId !== null ? ' (boşsa kartın adı)' : ''}</span><input maxLength={100} required={color.colorCardId === null} value={color.name} onChange={(event) => updateColor(color.key, { name: event.target.value })} /></label>
              <label className="field"><span>Pasta (kg/m)</span><input type="number" min="0" step="0.0001" value={color.paste} onChange={(event) => updateColor(color.key, { paste: event.target.value })} /></label>
            </div>
            <div className="table-wrap recipe-table">
              <table className="table table--compact">
                <thead><tr><th>Boya / pigment / kimyasal</th><th>g/kg pasta</th><th aria-label="Satırı kaldır" /></tr></thead>
                <tbody>
                  {color.lines.map((line) => (
                    <tr key={line.key}>
                      <td>
                        <LookupSelect resource="boyahane-dye-chemicals" label="Boya / kimyasal" hideLabel idSuffix={`l${line.key}`} value={line.dyeChemicalId} onChange={(value) => updateLine(color, line.key, { dyeChemicalId: value })} />
                      </td>
                      <td><input aria-label="g/kg pasta" type="number" min="0" max="1000" step="0.0001" required={line.dyeChemicalId !== null} value={line.gramsPerKilogram} onChange={(event) => updateLine(color, line.key, { gramsPerKilogram: event.target.value })} /></td>
                      <td className="recipe-table__remove-cell">
                        <button type="button" className="icon-button" aria-label="Satırı kaldır" title="Satırı kaldır" onClick={() => updateColor(color.key, { lines: color.lines.filter((row) => row.key !== line.key) })}>
                          <X aria-hidden="true" />
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <div className="toolbar">
              <button type="button" className="button" onClick={() => updateColor(color.key, { lines: [...color.lines, { key: nextKey(), dyeChemicalId: null, gramsPerKilogram: '' }] })}>Kalem ekle</button>
              {colors.length > 1 && <button type="button" className="button button--danger-ghost" onClick={() => setColors(colors.filter((row) => row.key !== color.key))}>Rengi kaldır</button>}
            </div>
          </fieldset>
        ))}
        <div>
          <button type="button" className="button" onClick={() => setColors([...colors, emptyColor(Math.max(0, ...colors.map((color) => Number(color.position) || 0)) + 1)])}>
            <Plus aria-hidden="true" />Renk ekle
          </button>
        </div>

        <ErrorAlert error={error ?? removeState.error} />
        <footer className="form__actions">
          {colorway && colorway.orderLineCount === 0 && <button type="button" className="button button--danger-ghost" disabled={removeState.isLoading} onClick={handleDelete}>Sil</button>}
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading}>Kaydet</button>
        </footer>
      </form>
    </Modal>
  )
}

/** Desen arşivi: desen kartları. Baskı siparişi satırında desen ve renk yolu seçilir. */
export function PatternsPage() {
  const canManage = usePermission(Permissions.BoyahaneLabManage)
  const navigate = useNavigate()
  const grid = useGrid()
  const { page, setPage } = grid
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [creating, setCreating] = useState(false)
  const { data, error, isLoading, isFetching } = useGetBoyahanePatternsQuery({ page, pageSize: PAGE_SIZE, search: search || undefined, ...grid.params })

  const columns: GridColumn[] = [
    { key: 'code', label: 'Kod' },
    { key: 'name', label: 'Ad' },
    { key: 'customerName', label: 'Müşteri' },
    { key: 'printType', label: 'Baskı türü', filter: Object.entries(printTypeLabels).map(([value, label]) => ({ value, label })) },
    { key: 'repeatWidthCm', label: 'Rapor', filter: false },
    { key: 'screenCount', label: 'Şablon', numeric: true, filter: false },
    { key: 'colorwayCount', label: 'Renk yolu', numeric: true, filter: false },
    { key: 'isActive', label: 'Durum', filter: [{ value: 'true', label: 'Aktif' }, { value: 'false', label: 'Pasif' }] },
  ]

  const handleSearch = (event: FormEvent) => {
    event.preventDefault()
    setPage(1)
    setSearch(searchInput.trim())
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">Desen Kartları</h1>
      <form className="toolbar" onSubmit={handleSearch} role="search">
        <input type="search" placeholder="Kod, ad ya da müşteri" aria-label="Desen ara" value={searchInput} onChange={(event) => setSearchInput(event.target.value)} />
        <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
        <ClearFiltersButton grid={grid} />
        <div className="toolbar__actions">
          {canManage && <button type="button" className="button button--primary" onClick={() => setCreating(true)}><Plus aria-hidden="true" />Yeni desen kartı</button>}
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
                {data.items.map((pattern) => (
                  <tr key={pattern.id}>
                    <td><Link to={`/boyahane/patterns/${pattern.id}`}><code>{pattern.code}</code></Link></td>
                    <td>{pattern.name}</td>
                    <td>{pattern.customerName ?? <span className="muted">Genel</span>}</td>
                    <td>{printTypeLabels[pattern.printType]}</td>
                    <td>{formatRepeat(pattern)}</td>
                    <td className="table__number">{pattern.screenCount ?? '—'}</td>
                    <td className="table__number">{pattern.colorwayCount}</td>
                    <td>{pattern.isActive ? <span className="badge badge--ok">Aktif</span> : <span className="badge badge--neutral">Pasif</span>}</td>
                  </tr>
                ))}
                {data.items.length === 0 && <tr><td colSpan={columns.length} className="table__empty">Desen kartı bulunamadı.</td></tr>}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}
      {creating && <PatternDialog pattern={null} onClose={() => setCreating(false)} onSaved={(id) => navigate(`/boyahane/patterns/${id}`)} />}
    </section>
  )
}

/** Desen kartı: bilgileri, görüntüsü, renk yolları ve her rengin baskı reçetesi. */
export function PatternDetailPage() {
  const id = Number(useParams().id)
  const navigate = useNavigate()
  const canManage = usePermission(Permissions.BoyahaneLabManage)
  const { data: pattern, error, isLoading } = useGetBoyahanePatternQuery(id)
  const image = useGetBoyahanePatternImageQuery(id, { skip: !pattern?.hasImage })
  const colorways = useGetBoyahanePatternColorwaysQuery(id)
  const [saveImage] = useSaveBoyahanePatternImageMutation()
  const [removeImage] = useDeleteBoyahanePatternImageMutation()
  const [editing, setEditing] = useState(false)
  const [editingColorway, setEditingColorway] = useState<BoyahaneColorway | null | undefined>(undefined)

  return (
    <section className="page">
      <p><Link to="/boyahane/patterns">← Desen kartlarına dön</Link></p>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />

      {pattern && (
        <>
          <header className="page__header">
            <div>
              <h1>{pattern.code} — {pattern.name} {!pattern.isActive && <span className="badge badge--neutral">Pasif</span>}</h1>
              <p className="muted">
                {pattern.customerName ?? 'Genel desen'} · {printTypeLabels[pattern.printType]} · Rapor {formatRepeat(pattern)}
                {pattern.screenCount !== null && ` · ${pattern.screenCount} şablon / silindir`}
              </p>
              {pattern.notes && <p>{pattern.notes}</p>}
            </div>
            <div className="page__actions">
              {canManage && <button type="button" className="button" onClick={() => setEditing(true)}><Pencil aria-hidden="true" />Düzenle</button>}
              {canManage && <button type="button" className="button button--primary" onClick={() => setEditingColorway(null)}><Plus aria-hidden="true" />Yeni renk yolu</button>}
            </div>
          </header>

          <ArchiveImagePanel
            title="Desen görüntüsü"
            image={pattern.hasImage ? image : { isLoading: false }}
            canManage={canManage}
            onSave={(upload) => saveImage({ id, ...upload })}
            onRemove={() => removeImage(id)}
          />

          <h2 className="section-title">Renk yolları ve baskı reçeteleri</h2>
          <ErrorAlert error={colorways.error} />
          {colorways.data?.length === 0 && <p className="muted">Renk yolu tanımlanmadı. Baskı siparişi satırında renk yolu seçilebilmesi için en az bir renk yolu ekleyin.</p>}
          {colorways.data?.map((colorway) => (
            <div key={colorway.id} className="form__section">
              <div className="toolbar">
                <strong><code>{colorway.code}</code> {colorway.name}</strong>
                {!colorway.isActive && <span className="badge badge--neutral">Pasif</span>}
                <span className="muted">{colorway.orderLineCount} sipariş satırı</span>
                <div className="toolbar__actions">
                  {canManage && <button type="button" className="button" onClick={() => setEditingColorway(colorway)}><Pencil aria-hidden="true" />Düzenle</button>}
                </div>
              </div>
              <div className="table-wrap">
                <table className="table table--compact">
                  <thead>
                    <tr><th>Sıra</th><th>Renk</th><th>Renk kartı</th><th className="table__number">Pasta (kg/m)</th><th>Baskı reçetesi (g/kg pasta)</th></tr>
                  </thead>
                  <tbody>
                    {colorway.colors.map((color) => (
                      <tr key={color.id}>
                        <td>{color.position}</td>
                        <td>{color.name}</td>
                        <td>{color.colorCardId !== null ? <Link to={`/boyahane/color-cards/${color.colorCardId}`}><code>{color.colorCardCode}</code></Link> : '—'}</td>
                        <td className="table__number">{color.pasteKilogramsPerMeter !== null ? formatRecipeAmount(color.pasteKilogramsPerMeter) : '—'}</td>
                        <td>
                          {color.lines.length === 0
                            ? <span className="muted">Reçete girilmedi</span>
                            : color.lines.map((line) => `${line.code} ${formatRecipeAmount(line.gramsPerKilogram)}`).join(' · ')}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          ))}

          {editing && <PatternDialog pattern={pattern} onClose={() => setEditing(false)} onDeleted={() => navigate('/boyahane/patterns')} />}
          {editingColorway !== undefined && <ColorwayDialog patternId={id} colorway={editingColorway} onClose={() => setEditingColorway(undefined)} />}
        </>
      )}
    </section>
  )
}
