import { Plus, Search, Trash2, X } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { fieldErrors } from '../../api/envelope'
import { ErrorAlert, TextField } from '../../components/FormControls'
import { ClearFiltersButton } from '../../components/grid/ClearFiltersButton'
import { GridHead } from '../../components/grid/GridHead'
import type { GridColumn } from '../../components/grid/GridHead'
import { useGrid } from '../../components/grid/useGrid'
import { Modal } from '../../components/Modal'
import { Pagination } from '../../components/Pagination'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDateTime } from '../../utils/format'
import { LookupSelect } from '../masterData/LookupSelect'
import { useGetLookupQuery } from '../masterData/masterDataApi'
import {
  useDeleteAssortmentTemplateMutation,
  useGetAssortmentTemplateQuery,
  useGetAssortmentTemplatesQuery,
  useSaveAssortmentTemplateMutation,
} from './assortmentApi'
import type { AssortmentTemplate } from './assortmentApi'

const PAGE_SIZE = 25

interface LineRow {
  key: string
  sizeId: number | null
  ratio: string
}

const emptyRow = (): LineRow => ({ key: crypto.randomUUID(), sizeId: null, ratio: '' })

const ratioOf = (text: string) => {
  const value = Number(text.trim())
  return Number.isInteger(value) && value > 0 && value <= 999 ? value : null
}

function AssortmentTemplateForm({ template, onClose }: { template?: AssortmentTemplate; onClose: () => void }) {
  const canManage = usePermission(Permissions.ShipmentManage)
  const [save, { isLoading, error }] = useSaveAssortmentTemplateMutation()
  const [remove, removing] = useDeleteAssortmentTemplateMutation()
  const sizes = useGetLookupQuery({ resource: 'sizes', take: 200 })

  const [code, setCode] = useState(template?.code ?? '')
  const [name, setName] = useState(template?.name ?? '')
  const [customerId, setCustomerId] = useState<number | null>(template?.customerId ?? null)
  const [notes, setNotes] = useState(template?.notes ?? '')
  const [isActive, setIsActive] = useState(template?.isActive ?? true)
  const [rows, setRows] = useState<LineRow[]>(() =>
    template && template.lines.length > 0
      ? template.lines.map((line) => ({ key: String(line.id), sizeId: line.sizeId, ratio: String(line.ratio) }))
      : [emptyRow(), emptyRow(), emptyRow()],
  )
  const [attempted, setAttempted] = useState(false)

  const update = (key: string, change: Partial<LineRow>) =>
    setRows((current) => current.map((row) => (row.key === key ? { ...row, ...change } : row)))

  const started = rows.filter((row) => row.sizeId !== null || row.ratio.trim() !== '')
  const invalid = started.filter((row) => row.sizeId === null || ratioOf(row.ratio) === null)
  const pieces = started.reduce((sum, row) => sum + (ratioOf(row.ratio) ?? 0), 0)

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    setAttempted(true)
    if (invalid.length > 0) {
      return
    }
    const result = await save({
      id: template?.id,
      body: {
        code,
        name,
        customerId,
        notes: notes.trim() || null,
        isActive,
        lines: started.map((row) => ({ sizeId: row.sizeId!, ratio: ratioOf(row.ratio)! })),
      },
    })
    if (!('error' in result)) {
      onClose()
    }
  }

  const handleDelete = async () => {
    if (template && window.confirm(`${template.code} asorti şablonu silinsin mi? Dağıtılmış koliler değişmez.`)) {
      const result = await remove(template.id)
      if (!('error' in result)) {
        onClose()
      }
    }
  }

  return (
    <form className="form" onSubmit={(event) => void handleSubmit(event)} noValidate>
      <fieldset className="form__section" disabled={!canManage}>
        <legend>Şablon</legend>
        <div className="form__pair">
          <TextField label="Kod" value={code} maxLength={30} required onChange={(event) => setCode(event.target.value.toUpperCase())} errors={fieldErrors(error, 'code')} />
          <TextField label="Ad" value={name} maxLength={200} required onChange={(event) => setName(event.target.value)} errors={fieldErrors(error, 'name')} />
        </div>
        <LookupSelect
          resource="customers"
          label="Müşteri"
          value={customerId}
          onChange={setCustomerId}
          placeholder="— Genel şablon (tüm müşteriler) —"
          errors={fieldErrors(error, 'customerId')}
        />
        <label className="field">
          <span>Not</span>
          <textarea rows={1} maxLength={500} value={notes} onChange={(event) => setNotes(event.target.value)} />
        </label>
        <label className="checkbox">
          <input type="checkbox" checked={isActive} onChange={(event) => setIsActive(event.target.checked)} />
          Aktif
        </label>
      </fieldset>

      <fieldset className="form__section" disabled={!canManage}>
        <legend>Beden dağılımı · koli başına {pieces} adet</legend>
        <p className="muted form__note">
          Her koli tek renktir. Koli dağıtımında her ürün-renk için bu oranlarla dolu koliler oluşturulur; oranı
          tamamlamayan kalan adetler renk başına bir karışık koliye konur.
        </p>
        <div className="table-wrap recipe-table">
          <table className="table table--compact">
            <colgroup>
              <col />
              <col />
              <col className="recipe-table__remove" />
            </colgroup>
            <thead>
              <tr>
                <th>Beden</th>
                <th className="table__number">Koli içi adet</th>
                <th aria-label="Satırı kaldır" />
              </tr>
            </thead>
            <tbody>
              {rows.map((row) => {
                const rowInvalid = attempted && invalid.includes(row)
                return (
                  <tr key={row.key} className={rowInvalid ? 'price-table__row--invalid' : undefined}>
                    <td>
                      <select
                        aria-label="Beden"
                        value={row.sizeId ?? ''}
                        onChange={(event) => update(row.key, { sizeId: event.target.value === '' ? null : Number(event.target.value) })}
                      >
                        <option value="">Beden seçin…</option>
                        {sizes.data?.map((size) => <option key={size.id} value={size.id}>{size.code} — {size.name}</option>)}
                        {row.sizeId !== null && !sizes.data?.some((size) => size.id === row.sizeId) && (
                          <option value={row.sizeId}>{template?.lines.find((line) => line.sizeId === row.sizeId)?.sizeName ?? row.sizeId}</option>
                        )}
                      </select>
                    </td>
                    <td className="table__number">
                      <input
                        className="packing-box"
                        aria-label="Koli içi adet"
                        type="number"
                        min="1"
                        max="999"
                        step="1"
                        value={row.ratio}
                        onChange={(event) => update(row.key, { ratio: event.target.value })}
                      />
                    </td>
                    <td className="recipe-table__remove-cell">
                      <button
                        type="button"
                        className="icon-button"
                        aria-label="Satırı kaldır"
                        title="Satırı kaldır"
                        onClick={() => setRows((current) => current.filter((item) => item.key !== row.key))}
                      >
                        <X aria-hidden="true" />
                      </button>
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
        <div>
          <button type="button" className="button" onClick={() => setRows((current) => [...current, emptyRow()])}>Beden ekle</button>
        </div>
        {attempted && invalid.length > 0 && (
          <small className="field__error">Her satırda beden ve 1 ile 999 arasında bir adet girilmelidir.</small>
        )}
        {fieldErrors(error, 'lines')?.map((message) => <small key={message} className="field__error">{message}</small>)}
      </fieldset>

      {template && (
        <p className="muted form__note">
          Oluşturan: {template.createdBy ?? '—'} · {formatDateTime(template.createdAt)}
          {template.updatedAt && ` · Son değişiklik: ${template.updatedBy ?? '—'} · ${formatDateTime(template.updatedAt)}`}
        </p>
      )}

      <ErrorAlert error={error ?? removing.error} fields={['code', 'name', 'customerId', 'lines']} />

      <footer className="form__actions">
        {template && canManage && (
          <button type="button" className="button button--danger-ghost form__actions-start" onClick={() => void handleDelete()}>
            <Trash2 aria-hidden="true" />
            Sil
          </button>
        )}
        <button type="button" className="button" onClick={onClose}>Vazgeç</button>
        {canManage && (
          <button type="submit" className="button button--primary" disabled={isLoading}>
            {isLoading ? 'Kaydediliyor…' : 'Kaydet'}
          </button>
        )}
      </footer>
    </form>
  )
}

/** Asorti şablonu kaydı: yeni şablon ya da var olanın düzenlenmesi. */
function AssortmentTemplateDialog({ id, onClose }: { id?: number; onClose: () => void }) {
  const { data, isLoading, error } = useGetAssortmentTemplateQuery(id ?? 0, { skip: id === undefined })
  return (
    <Modal title={id === undefined ? 'Yeni asorti şablonu' : `Asorti şablonu: ${data?.code ?? ''}`} onClose={onClose}>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {(id === undefined || data) && <AssortmentTemplateForm template={data} onClose={onClose} />}
    </Modal>
  )
}

/**
 * Asorti şablonları: kolinin beden dağılımı (ör. S:1 M:2 L:2 XL:1). Müşteriye özel ya da genel tanımlanır;
 * sevkiyatta "Koli dağıt" ile seçilir.
 */
export function AssortmentTemplatesPage() {
  const canManage = usePermission(Permissions.ShipmentManage)
  const grid = useGrid()
  const { page, setPage } = grid
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  // undefined: kapalı, null: yeni şablon, sayı: açılan şablon.
  const [open, setOpen] = useState<number | null | undefined>(undefined)

  const { data, error, isLoading, isFetching } = useGetAssortmentTemplatesQuery({
    page,
    pageSize: PAGE_SIZE,
    search: search || undefined,
    ...grid.params,
  })

  const columns: GridColumn[] = [
    { key: 'code', label: 'Kod' },
    { key: 'name', label: 'Ad' },
    { key: 'customerName', label: 'Müşteri' },
    { key: 'sizeCount', label: 'Beden', numeric: true, filter: false },
    { key: 'piecesPerCarton', label: 'Koli adedi', numeric: true },
    { key: 'isActive', label: 'Durum', filter: [{ value: 'true', label: 'Aktif' }, { value: 'false', label: 'Pasif' }] },
  ]

  const handleSearch = (event: FormEvent) => {
    event.preventDefault()
    setPage(1)
    setSearch(searchInput.trim())
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">Asorti Şablonları</h1>

      <form className="toolbar" onSubmit={handleSearch} role="search">
        <input
          type="search"
          placeholder="Kod, ad ya da müşteri"
          aria-label="Asorti şablonu ara"
          value={searchInput}
          onChange={(event) => setSearchInput(event.target.value)}
        />
        <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
        <ClearFiltersButton grid={grid} />
        <div className="toolbar__actions">
          {canManage && (
            <button type="button" className="button button--primary" onClick={() => setOpen(null)}>
              <Plus aria-hidden="true" />
              Yeni şablon
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
                {data.items.map((template) => (
                  <tr key={template.id}>
                    <td>
                      <button type="button" className="table__open" onClick={() => setOpen(template.id)}>
                        <code>{template.code}</code>
                      </button>
                    </td>
                    <td>{template.name}</td>
                    <td>{template.customerName ?? <span className="muted">Genel</span>}</td>
                    <td className="table__number">{template.sizeCount}</td>
                    <td className="table__number">{template.piecesPerCarton}</td>
                    <td>
                      {template.isActive
                        ? <span className="badge badge--ok">Aktif</span>
                        : <span className="badge badge--neutral">Pasif</span>}
                    </td>
                  </tr>
                ))}
                {data.items.length === 0 && (
                  <tr>
                    <td colSpan={columns.length} className="table__empty">Asorti şablonu bulunamadı.</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}

      {open !== undefined && <AssortmentTemplateDialog id={open ?? undefined} onClose={() => setOpen(undefined)} />}
    </section>
  )
}
