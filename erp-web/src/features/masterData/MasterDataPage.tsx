import { Check, Plus, Search, Trash2, Upload } from 'lucide-react'
import { useCallback, useRef, useState } from 'react'
import type { ChangeEvent, FormEvent, ReactNode } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { fieldErrors } from '../../api/envelope'
import { ErrorAlert, TextField } from '../../components/FormControls'
import { GridHead } from '../../components/grid/GridHead'
import type { GridColumn } from '../../components/grid/GridHead'
import { useGrid } from '../../components/grid/useGrid'
import { Modal } from '../../components/Modal'
import { Pagination } from '../../components/Pagination'
import { usePermission } from '../../hooks/usePermission'
import { useAppSelector } from '../../store/hooks'
import { ImportDialog } from './ImportDialog'
import { CurrencySelect } from '../finance/CurrencySelect'
import { LookupSelect } from './LookupSelect'
import { initialValues, toRequestBody } from './masterDataForm'
import { useDeleteMasterRecordMutation, useGetMasterListQuery, useSaveMasterRecordMutation } from './masterDataApi'
import type { MasterRow } from './masterDataApi'
import { cardScanHandlers } from '../rfid/cardScan'
import type { AfterSave, ColumnDef, FieldDef, ResourceConfig } from './resources'
import { ClearFiltersButton } from '../../components/grid/ClearFiltersButton'

const PAGE_SIZE = 20

/** "Makine Tipi" → "makine tipi"; kısaltmalar korunur: "RFID kartı" → "RFID kartı" (Türkçe küçültme "rfıd" yapar). */
function lowerCaseLabel(text: string): string {
  return text
    .split(' ')
    .map((word) => (word.length > 1 && word === word.toLocaleUpperCase('tr-TR') ? word : word.toLocaleLowerCase('tr-TR')))
    .join(' ')
}

/** Hücre içeriği; sayısal sütunlar Türkçe sayı biçimiyle yazılır. */
function cellContent(column: ColumnDef, row: MasterRow): ReactNode {
  if (column.render) {
    return column.render(row)
  }
  const value = row[column.key] as string | number | null | undefined
  if (value === null || value === undefined || value === '') {
    return '—'
  }
  if (typeof column.numeric === 'number' && typeof value === 'number') {
    return value.toLocaleString('tr-TR', {
      minimumFractionDigits: Math.min(column.numeric, 2),
      maximumFractionDigits: column.numeric,
    })
  }
  return typeof value === 'number' ? value.toLocaleString('tr-TR', { maximumFractionDigits: 2 }) : value
}

function RecordDialog({
  config,
  row,
  onClose,
  onDelete,
}: {
  config: ResourceConfig
  row?: MasterRow
  onClose: () => void
  /** Düzenlenen kayıt silinebiliyorsa formun alt çubuğunda Sil düğmesi gösterilir. */
  onDelete?: () => void
}) {
  const [save, { isLoading, error }] = useSaveMasterRecordMutation()
  const [values, setValues] = useState(() => initialValues(config.fields, row))
  const [isActive, setIsActive] = useState(row?.isActive ?? true)
  // Ek bölümün (ör. fotoğraf) kayıttan sonra yapacağı işlem ve o işlemin hatası.
  const afterSave = useRef<AfterSave | null>(null)
  const setAfterSave = useCallback((action: AfterSave | null) => {
    afterSave.current = action
  }, [])
  const [extraError, setExtraError] = useState<unknown>(null)
  const [applyingExtra, setApplyingExtra] = useState(false)
  // Yeni kayıt oluştu ama ek işlem başarısız olduysa, yeniden denemede kayıt ikinci kez oluşturulmaz.
  const [savedId, setSavedId] = useState(row?.id)
  const busy = isLoading || applyingExtra
  const FormExtra = config.formExtra
  const userPermissions = useAppSelector((state) => state.auth.user?.permissions)
  // Ayrı yetki isteyen alan, yetkisi olmayan kullanıcıda salt okunur kalır; değeri olduğu gibi geri gönderilir.
  const isLocked = (field: FieldDef) => field.permission !== undefined && !(userPermissions?.includes(field.permission) ?? false)
  const errorFields = config.fields.filter((field) => Boolean(fieldErrors(error, field.name)?.length)).map((field) => field.name)
  const hasFieldErrors = errorFields.length > 0

  const setValue = (name: string, value: string | number | boolean | null) =>
    setValues((current) => ({ ...current, [name]: value }))

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    setExtraError(null)
    const result = await save({
      resource: config.key,
      id: savedId,
      body: toRequestBody(config.fields, values, isActive),
    })
    if ('error' in result) {
      return
    }

    setSavedId(result.data.id)
    if (afterSave.current) {
      setApplyingExtra(true)
      const failure = await afterSave.current(result.data.id)
      setApplyingExtra(false)
      if (failure) {
        setExtraError(failure)
        return
      }
    }

    onClose()
  }

  const activeControl = (
    <label className="switch">
      <input type="checkbox" checked={isActive} onChange={(event) => setIsActive(event.target.checked)} />
      <span className="switch__track" aria-hidden="true" />
      Aktif
    </label>
  )

  // Ek bölümü olan kaynaklarda standart alanları ve Aktif anahtarını o bölüm yerleştirir.
  const withExtra = (fields: [string, ReactNode][]) =>
    FormExtra ? (
      <FormExtra
        row={row}
        values={values}
        fields={Object.fromEntries(fields)}
        activeControl={activeControl}
        disabled={busy}
        hasFieldErrors={hasFieldErrors}
        errorFields={errorFields}
        onAfterSaveChange={setAfterSave}
      />
    ) : (
      <>
        {fields.map(([, node]) => node)}
        {activeControl}
      </>
    )

  const renderField = (field: FieldDef, index: number): ReactNode => {
    const errors = fieldErrors(error, field.name)
    const value = values[field.name]
    const locked = isLocked(field)
    const hint = locked ? 'Bu alanı değiştirme yetkiniz yok.' : field.hint

  if (field.type === 'lookup') {
    return (
      <LookupSelect
        key={field.name}
        resource={field.lookup!}
        label={field.label}
        value={value as number | null}
        onChange={(next) => setValue(field.name, next)}
        required={field.required}
        disabled={locked}
        errors={errors}
      />
    )
  }

  if (field.type === 'currency') {
    return (
      <CurrencySelect
        key={field.name}
        label={field.label}
        value={value as string}
        onChange={(next) => setValue(field.name, next)}
        hint={hint}
        disabled={locked}
        errors={errors}
      />
    )
  }

  if (field.type === 'checkbox') {
    return (
      <div key={field.name} className="field">
        <label className="checkbox">
          <input
            type="checkbox"
            checked={value === true}
            disabled={locked}
            onChange={(event) => setValue(field.name, event.target.checked)}
          />
          {field.label}
        </label>
        {hint && <small className="field__hint">{hint}</small>}
      </div>
    )
  }

  if (field.type === 'select') {
    return (
      <label key={field.name} className="field">
        <span>{field.label}</span>
        <select
          value={value as string}
          disabled={locked}
          onChange={(event) => setValue(field.name, event.target.value)}
          aria-invalid={Boolean(errors?.length)}
        >
          {field.options?.map((option) => (
            <option key={option.value} value={option.value}>{option.label}</option>
          ))}
        </select>
        {hint && !errors?.length && <small className="field__hint">{hint}</small>}
        {errors?.map((message) => (
          <small key={message} className="field__error">{message}</small>
        ))}
      </label>
    )
  }

  if (field.type === 'textarea') {
    return (
      <label key={field.name} className="field">
        <span>{field.label}</span>
        <textarea
          rows={3}
          maxLength={field.maxLength}
          value={value as string}
          onChange={(event) => setValue(field.name, event.target.value)}
          aria-invalid={Boolean(errors?.length)}
        />
        {errors?.map((message) => (
          <small key={message} className="field__error">{message}</small>
        ))}
      </label>
    )
  }

  if (field.type === 'color') {
    return (
      <div key={field.name} className="field">
        <label htmlFor={`field-${field.name}`}>{field.label}</label>
        <div className="color-field">
          <input
            id={`field-${field.name}`}
            type="color"
            value={(value as string) || '#000000'}
            onChange={(event) => setValue(field.name, event.target.value)}
          />
          <code>{(value as string) || 'Seçilmedi'}</code>
          {value !== '' && (
            <button type="button" className="button" onClick={() => setValue(field.name, '')}>
              Temizle
            </button>
          )}
        </div>
        {errors?.map((message) => (
          <small key={message} className="field__error">{message}</small>
        ))}
      </div>
    )
  }

  return (
    <TextField
      key={field.name}
      label={field.label}
      type={field.type === 'number' ? 'number' : field.type === 'time' ? 'time' : field.type === 'date' ? 'date' : 'text'}
      required={field.type === 'time' && field.required}
      step={field.step}
      min={field.type === 'number' ? 0 : undefined}
      maxLength={field.maxLength}
      value={value as string}
      disabled={locked}
      {...(field.scan
        ? cardScanHandlers((next) => setValue(field.name, next))
        : { onChange: (event: ChangeEvent<HTMLInputElement>) => setValue(field.name, event.target.value) })}
      errors={errors}
      hint={hint}
      autoFocus={index === 0}
      autoComplete="off"
    />
  )
  }

  return (
    <Modal
      title={row ? `${config.singular} düzenle` : `Yeni ${lowerCaseLabel(config.singular)}`}
      onClose={onClose}
      variant="drawer"
    >
      <form className="form" onSubmit={(event) => void handleSubmit(event)} noValidate>
        {withExtra(config.fields.map((field, index) => [field.name, renderField(field, index)]))}

        <ErrorAlert error={error} fields={config.fields.map((field) => field.name)} />
        {Boolean(extraError) && (
          <>
            <div className="alert alert--error" role="alert">
              {config.singular} kaydedildi ancak ek bilgiler kaydedilemedi. Yeniden deneyebilirsiniz.
            </div>
            <ErrorAlert error={extraError} />
          </>
        )}

        <footer className="form__actions">
          {onDelete && (
            <button type="button" className="button button--danger-ghost form__actions-start" onClick={onDelete} disabled={busy}>
              <Trash2 aria-hidden="true" />
              Sil
            </button>
          )}
          <button type="button" className="button" onClick={onClose}>{extraError ? 'Kapat' : 'Vazgeç'}</button>
          <button type="submit" className="button button--primary" disabled={busy}>
            <Check aria-hidden="true" />
            {busy ? 'Kaydediliyor…' : 'Kaydet'}
          </button>
        </footer>
      </form>
    </Modal>
  )
}

function DeleteDialog({
  config,
  row,
  onClose,
  onDeleted,
}: {
  config: ResourceConfig
  row: MasterRow
  onClose: () => void
  onDeleted: () => void
}) {
  const [deleteRecord, { isLoading, error }] = useDeleteMasterRecordMutation()
  const firstColumn = config.columns[0].key

  const handleDelete = async () => {
    const result = await deleteRecord({ resource: config.key, id: row.id })
    if (!('error' in result)) {
      onDeleted()
    }
  }

  return (
    <Modal title={`${config.singular} sil`} onClose={onClose}>
      <div className="form">
        <p>
          <strong>{String(row[firstColumn])}</strong> silinecek. Geçmiş kayıtlarda görünmeye devam eder;
          kodu yeniden kullanılabilir.
        </p>
        <ErrorAlert error={error} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="button" className="button button--danger" disabled={isLoading} onClick={() => void handleDelete()}>
            {isLoading ? 'Siliniyor…' : 'Sil'}
          </button>
        </footer>
      </div>
    </Modal>
  )
}

// Silme onayı düzenleme formunun üstünde açılır; vazgeçilince form yerinde kalır.
type Dialog =
  | { kind: 'create' }
  | { kind: 'import' }
  | { kind: 'edit'; row: MasterRow; confirmDelete?: boolean }

/** Tüm ana veri kaynaklarının ortak liste + form ekranı; içerik `config` ile belirlenir. */
export function MasterDataPage({ config }: { config: ResourceConfig }) {
  const canManage = usePermission(config.managePermission)

  // Genel aramadan gelindiğinde liste aranan kayda süzülü açılır.
  const [searchParams] = useSearchParams()
  const initialSearch = searchParams.get('search') ?? ''

  const grid = useGrid()
  const { page, setPage } = grid
  const [searchInput, setSearchInput] = useState(initialSearch)
  const [search, setSearch] = useState(initialSearch)
  const [dialog, setDialog] = useState<Dialog | null>(null)

  const { data, isLoading, isFetching, error } = useGetMasterListQuery({
    resource: config.key,
    page,
    pageSize: PAGE_SIZE,
    search: search || undefined,
    ...grid.params,
  })

  const handleSearch = (event: FormEvent) => {
    event.preventDefault()
    setPage(1)
    setSearch(searchInput.trim())
  }

  const RowActions = config.rowActions
  const columnCount = config.columns.length + (RowActions ? 2 : 1)
  const gridColumns: GridColumn[] = [
    ...config.columns.map((column): GridColumn => ({
      key: column.key,
      label: column.label,
      numeric: column.numeric !== undefined,
      filter: column.filter
        ?? config.fields.find((field) => field.name === column.key && field.type === 'select')?.options,
    })),
    { key: 'isActive', label: 'Durum', filter: [{ value: 'true', label: 'Aktif' }, { value: 'false', label: 'Pasif' }] },
    ...(RowActions ? [{ label: '', ariaLabel: 'İşlemler' }] : []),
  ]
  const canEdit = (row: MasterRow) => canManage && !row.isSystemManaged
  const openEdit = (row: MasterRow) => {
    if (canEdit(row)) {
      setDialog({ kind: 'edit', row })
    }
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">{config.title}</h1>

      <form className="toolbar" onSubmit={handleSearch} role="search">
        <input
          type="search"
          placeholder="Kod veya ad"
          aria-label={`${config.title} içinde ara`}
          value={searchInput}
          onChange={(event) => setSearchInput(event.target.value)}
        />
        <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
        <ClearFiltersButton grid={grid} />
        <div className="toolbar__actions">
          {canManage && (
            <div className="page__actions">
              <button type="button" className="button" onClick={() => setDialog({ kind: 'import' })}>
                <Upload aria-hidden="true" />
                CSV içe aktar
              </button>
              <button type="button" className="button button--primary" onClick={() => setDialog({ kind: 'create' })}>
                <Plus aria-hidden="true" />
                Yeni {lowerCaseLabel(config.singular)}
              </button>
            </div>
          )}
        </div>
      </form>

      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />

      {data && (
        <>
          <div className="table-wrap" aria-busy={isFetching}>
            <table className="table">
              <GridHead columns={gridColumns} grid={grid} />
              <tbody>
                {data.items.map((row) => (
                  <tr
                    key={row.id}
                    className={canEdit(row) ? 'table__row--link' : undefined}
                    onClick={() => openEdit(row)}
                  >
                    {config.columns.map((column, index) => {
                      const content = cellContent(column, row)
                      if (index !== 0) {
                        return (
                          <td key={column.key} className={column.numeric !== undefined ? 'table__number' : undefined}>
                            {content}
                          </td>
                        )
                      }
                      // Kod sütunu klavyeyle de kaydı açar; ayrıntı sayfası olan kayıtlarda o sayfaya gider.
                      return (
                        <td key={column.key}>
                          {config.detailPath ? (
                            <Link to={config.detailPath(row)} onClick={(event) => event.stopPropagation()}>
                              <code>{content}</code>
                            </Link>
                          ) : canEdit(row) ? (
                            <button
                              type="button"
                              className="table__open"
                              onClick={(event) => {
                                event.stopPropagation()
                                openEdit(row)
                              }}
                            >
                              <code>{content}</code>
                            </button>
                          ) : (
                            <code>{content}</code>
                          )}
                        </td>
                      )
                    })}
                    <td>
                      <span className={`badge badge--${row.isActive ? 'ok' : 'neutral'}`}>
                        {row.isActive ? 'Aktif' : 'Pasif'}
                      </span>
                    </td>
                    {RowActions && (
                      <td className="table__actions" onClick={(event) => event.stopPropagation()}>
                        <RowActions row={row} />
                      </td>
                    )}
                  </tr>
                ))}
                {data.items.length === 0 && (
                  <tr>
                    <td colSpan={columnCount} className="table__empty">Kayıt bulunamadı.</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
          <Pagination
            page={data.page}
            totalPages={data.totalPages}
            totalCount={data.totalCount}
            onPageChange={setPage}
          />
        </>
      )}

      {dialog?.kind === 'create' && <RecordDialog config={config} onClose={() => setDialog(null)} />}
      {dialog?.kind === 'import' && <ImportDialog config={config} onClose={() => setDialog(null)} />}
      {dialog?.kind === 'edit' && (
        <RecordDialog
          config={config}
          row={dialog.row}
          onClose={() => setDialog(null)}
          onDelete={() => setDialog({ ...dialog, confirmDelete: true })}
        />
      )}
      {dialog?.kind === 'edit' && dialog.confirmDelete && (
        <DeleteDialog
          config={config}
          row={dialog.row}
          onClose={() => setDialog({ ...dialog, confirmDelete: false })}
          onDeleted={() => setDialog(null)}
        />
      )}
    </section>
  )
}
