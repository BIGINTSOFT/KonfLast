import { Pencil, Plus, Search } from 'lucide-react'
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
import { formatDate, formatRecipeAmount } from '../../utils/format'
import { LookupSelect } from '../masterData/LookupSelect'
import { ArchiveImagePanel } from './ArchiveImagePanel'
import {
  useDeleteBoyahaneColorCardImageMutation,
  useDeleteBoyahaneColorCardMutation,
  useGetBoyahaneColorCardImageQuery,
  useGetBoyahaneColorCardQuery,
  useGetBoyahaneColorCardRecipesQuery,
  useGetBoyahaneColorCardsQuery,
  useSaveBoyahaneColorCardImageMutation,
  useSaveBoyahaneColorCardMutation,
} from './boyahaneArchiveApi'
import type { BoyahaneColorCard } from './boyahaneArchiveApi'
import { dyeChemicalUnit } from './boyahaneLabApi'

const PAGE_SIZE = 20

const numberOrNull = (value: string) => (value.trim() === '' ? null : Number(value))

const toInput = (value: number | null | undefined) => (value === null || value === undefined ? '' : String(value))

/** CIELab değerleri: üçü birlikte girilir. */
export function formatLab(card: Pick<BoyahaneColorCard, 'labL' | 'labA' | 'labB'>): string {
  return card.labL === null || card.labA === null || card.labB === null ? '—' : `L ${card.labL} · a ${card.labA} · b ${card.labB}`
}

/** Yeni renk kartı ya da kartın düzenlenmesi. Görüntü kart kaydedildikten sonra kartın sayfasından yüklenir. */
export function ColorCardDialog({ card, onClose, onSaved, onDeleted }: {
  card: BoyahaneColorCard | null
  onClose: () => void
  onSaved?: (id: number) => void
  onDeleted?: () => void
}) {
  const [save, { isLoading, error }] = useSaveBoyahaneColorCardMutation()
  const [remove, removeState] = useDeleteBoyahaneColorCardMutation()
  const [code, setCode] = useState(card?.code ?? '')
  const [name, setName] = useState(card?.name ?? '')
  const [customerId, setCustomerId] = useState<number | null>(card?.customerId ?? null)
  const [customerColorCode, setCustomerColorCode] = useState(card?.customerColorCode ?? '')
  const [pantone, setPantone] = useState(card?.pantone ?? '')
  const [labL, setLabL] = useState(toInput(card?.labL))
  const [labA, setLabA] = useState(toInput(card?.labA))
  const [labB, setLabB] = useState(toInput(card?.labB))
  const [notes, setNotes] = useState(card?.notes ?? '')
  const [isActive, setIsActive] = useState(card?.isActive ?? true)

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await save({
      id: card?.id,
      body: {
        code,
        name,
        customerId,
        customerColorCode: customerColorCode.trim() || null,
        pantone: pantone.trim() || null,
        labL: numberOrNull(labL),
        labA: numberOrNull(labA),
        labB: numberOrNull(labB),
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
    if (card && !('error' in await remove(card.id))) {
      onClose()
      onDeleted?.()
    }
  }

  return (
    <Modal title={card ? `Renk kartı: ${card.code}` : 'Yeni renk kartı'} onClose={onClose}>
      <form className="form" onSubmit={handleSubmit}>
        <div className="form__pair">
          <label className="field"><span>Kod</span><input required maxLength={30} value={code} onChange={(event) => setCode(event.target.value)} /></label>
          <label className="field"><span>Ad</span><input required maxLength={100} value={name} onChange={(event) => setName(event.target.value)} /></label>
        </div>
        <LookupSelect resource="boyahane-customers" label="Müşteri (boşsa genel renk)" value={customerId} onChange={setCustomerId} />
        <div className="form__pair">
          <label className="field"><span>Müşteri renk kodu</span><input maxLength={50} value={customerColorCode} onChange={(event) => setCustomerColorCode(event.target.value)} /></label>
          <label className="field"><span>Pantone</span><input maxLength={50} placeholder="örn. 19-4052 TCX" value={pantone} onChange={(event) => setPantone(event.target.value)} /></label>
        </div>
        <div className="form__pair">
          <label className="field"><span>L</span><input type="number" min="0" max="100" step="0.001" value={labL} onChange={(event) => setLabL(event.target.value)} /></label>
          <label className="field"><span>a</span><input type="number" min="-128" max="128" step="0.001" value={labA} onChange={(event) => setLabA(event.target.value)} /></label>
          <label className="field"><span>b</span><input type="number" min="-128" max="128" step="0.001" value={labB} onChange={(event) => setLabB(event.target.value)} /></label>
        </div>
        <p className="muted form__note">Lab değerleri isteğe bağlıdır; girilecekse L, a ve b birlikte girilir.</p>
        <label className="field"><span>Not</span><textarea rows={2} maxLength={500} value={notes} onChange={(event) => setNotes(event.target.value)} /></label>
        <label className="checkbox"><input type="checkbox" checked={isActive} onChange={(event) => setIsActive(event.target.checked)} />Aktif</label>
        <ErrorAlert error={error ?? removeState.error} />
        <footer className="form__actions">
          {card && <button type="button" className="button button--danger-ghost" disabled={removeState.isLoading} onClick={handleDelete}>Sil</button>}
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading}>Kaydet</button>
        </footer>
      </form>
    </Modal>
  )
}

/** Renk arşivi: renk kartları. Sipariş satırında kart seçilince renk adı ve kodu karttan gelir. */
export function ColorCardsPage() {
  const canManage = usePermission(Permissions.BoyahaneLabManage)
  const navigate = useNavigate()
  const grid = useGrid()
  const { page, setPage } = grid
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [creating, setCreating] = useState(false)
  const { data, error, isLoading, isFetching } = useGetBoyahaneColorCardsQuery({ page, pageSize: PAGE_SIZE, search: search || undefined, ...grid.params })

  const columns: GridColumn[] = [
    { key: 'code', label: 'Kod' },
    { key: 'name', label: 'Ad' },
    { key: 'customerName', label: 'Müşteri' },
    { key: 'customerColorCode', label: 'Müşteri kodu' },
    { key: 'pantone', label: 'Pantone' },
    { key: 'labL', label: 'Lab', filter: false },
    { key: 'recipeCount', label: 'Reçete', numeric: true, filter: false },
    { key: 'isActive', label: 'Durum', filter: [{ value: 'true', label: 'Aktif' }, { value: 'false', label: 'Pasif' }] },
  ]

  const handleSearch = (event: FormEvent) => {
    event.preventDefault()
    setPage(1)
    setSearch(searchInput.trim())
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">Renk Kartları</h1>
      <form className="toolbar" onSubmit={handleSearch} role="search">
        <input type="search" placeholder="Kod, ad, müşteri kodu, Pantone ya da müşteri" aria-label="Renk kartı ara" value={searchInput} onChange={(event) => setSearchInput(event.target.value)} />
        <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
        <ClearFiltersButton grid={grid} />
        <div className="toolbar__actions">
          {canManage && <button type="button" className="button button--primary" onClick={() => setCreating(true)}><Plus aria-hidden="true" />Yeni renk kartı</button>}
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
                {data.items.map((card) => (
                  <tr key={card.id}>
                    <td><Link to={`/boyahane/color-cards/${card.id}`}><code>{card.code}</code></Link></td>
                    <td>{card.name}</td>
                    <td>{card.customerName ?? <span className="muted">Genel</span>}</td>
                    <td>{card.customerColorCode ?? '—'}</td>
                    <td>{card.pantone ?? '—'}</td>
                    <td>{formatLab(card)}</td>
                    <td className="table__number">{card.recipeCount}</td>
                    <td>{card.isActive ? <span className="badge badge--ok">Aktif</span> : <span className="badge badge--neutral">Pasif</span>}</td>
                  </tr>
                ))}
                {data.items.length === 0 && <tr><td colSpan={columns.length} className="table__empty">Renk kartı bulunamadı.</td></tr>}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}
      {creating && <ColorCardDialog card={null} onClose={() => setCreating(false)} onSaved={(id) => navigate(`/boyahane/color-cards/${id}`)} />}
    </section>
  )
}

/** Renk kartı: bilgileri, numune görüntüsü ve karta bağlı onaylı lab reçeteleri. */
export function ColorCardDetailPage() {
  const id = Number(useParams().id)
  const navigate = useNavigate()
  const canManage = usePermission(Permissions.BoyahaneLabManage)
  const { data: card, error, isLoading } = useGetBoyahaneColorCardQuery(id)
  const image = useGetBoyahaneColorCardImageQuery(id, { skip: !card?.hasImage })
  const recipes = useGetBoyahaneColorCardRecipesQuery(id)
  const [saveImage] = useSaveBoyahaneColorCardImageMutation()
  const [removeImage] = useDeleteBoyahaneColorCardImageMutation()
  const [editing, setEditing] = useState(false)

  return (
    <section className="page">
      <p><Link to="/boyahane/color-cards">← Renk kartlarına dön</Link></p>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />

      {card && (
        <>
          <header className="page__header">
            <div>
              <h1>{card.code} — {card.name} {!card.isActive && <span className="badge badge--neutral">Pasif</span>}</h1>
              <p className="muted">
                {card.customerName ?? 'Genel renk'}
                {card.customerColorCode && ` · Müşteri kodu ${card.customerColorCode}`}
                {card.pantone && ` · Pantone ${card.pantone}`}
                {` · Lab: ${formatLab(card)}`}
              </p>
              {card.notes && <p>{card.notes}</p>}
            </div>
            <div className="page__actions">
              {canManage && <button type="button" className="button" onClick={() => setEditing(true)}><Pencil aria-hidden="true" />Düzenle</button>}
            </div>
          </header>

          <ArchiveImagePanel
            title="Numune görüntüsü"
            image={card.hasImage ? image : { isLoading: false }}
            canManage={canManage}
            onSave={(upload) => saveImage({ id, ...upload })}
            onRemove={() => removeImage(id)}
          />

          <h2 className="section-title">Reçeteler</h2>
          <p className="muted">Sipariş satırında bu renk kartı seçilmiş ve onaylanmış lab reçeteleri.</p>
          <ErrorAlert error={recipes.error} />
          {recipes.data && (
            <div className="table-wrap">
              <table className="table">
                <thead>
                  <tr><th>Reçete no</th><th>Müşteri</th><th>Kumaş / iplik</th><th>Sipariş</th><th>Onay tarihi</th><th>Reçete</th></tr>
                </thead>
                <tbody>
                  {recipes.data.map((recipe) => (
                    <tr key={recipe.trialId}>
                      <td><Link to={`/boyahane/lab/${recipe.requestId}`}><code>{recipe.requestNo}/{recipe.trialCode}</code></Link></td>
                      <td>{recipe.customerName ?? <span className="muted">Stoğa üretim</span>}</td>
                      <td>{recipe.itemCode ? `${recipe.itemCode} — ${recipe.itemName ?? ''}` : '—'}</td>
                      <td><Link to={`/boyahane/orders/${recipe.orderId}`}>{recipe.orderNo}</Link></td>
                      <td>{recipe.decisionOn ? formatDate(recipe.decisionOn) : '—'}</td>
                      <td className="muted">{recipe.items.map((item) => `${item.code} ${formatRecipeAmount(item.amount)} ${dyeChemicalUnit[item.kind]}`).join(' · ')}</td>
                    </tr>
                  ))}
                  {recipes.data.length === 0 && <tr><td colSpan={6} className="table__empty">Bu karta bağlı onaylı reçete yok.</td></tr>}
                </tbody>
              </table>
            </div>
          )}

          {editing && <ColorCardDialog card={card} onClose={() => setEditing(false)} onDeleted={() => navigate('/boyahane/color-cards')} />}
        </>
      )}
    </section>
  )
}
