import { Filter } from 'lucide-react'
import { Fragment, useState } from 'react'
import type { FormEvent } from 'react'
import { ErrorAlert } from '../../components/FormControls'
import { GridHead } from '../../components/grid/GridHead'
import type { GridColumn } from '../../components/grid/GridHead'
import { useGrid } from '../../components/grid/useGrid'
import { Pagination } from '../../components/Pagination'
import type { AuditAction, AuditLog } from '../../types/api'
import { formatDateTime, localInputToIso } from '../../utils/format'
import { useGetAuditLogsQuery } from './auditLogsApi'
import type { AuditLogParams } from './auditLogsApi'
import { entityLabel, entityOptions } from './entityLabels'
import { ClearFiltersButton } from '../../components/grid/ClearFiltersButton'

const PAGE_SIZE = 25

const actionPresentation: Record<AuditAction, { label: string; tone: string }> = {
  CREATED: { label: 'Oluşturma', tone: 'ok' },
  UPDATED: { label: 'Güncelleme', tone: 'neutral' },
  DELETED: { label: 'Silme', tone: 'danger' },
}

type Filters = Pick<AuditLogParams, 'from' | 'to'>

// Kayıt türü tam eşleşmeyle süzülür ("Bundle" seçimi "BundleOperation" kayıtlarını getirmesin).
// Zaman sütunu sunucuda süzülemez; aralık araç çubuğundaki Başlangıç / Bitiş ile verilir.
const columns: GridColumn[] = [
  { key: 'createdAt', label: 'Zaman', filter: false },
  { key: 'userName', label: 'Kullanıcı' },
  {
    key: 'action',
    label: 'İşlem',
    filter: Object.entries(actionPresentation).map(([value, { label }]) => ({ value, label })),
  },
  {
    key: 'entityName',
    label: 'Kayıt türü',
    filter: entityOptions.map(([value, label]) => ({ value: `=${value}`, label })),
  },
  { key: 'entityId', label: 'Kayıt no' },
  { label: '', ariaLabel: 'Ayrıntı', className: 'table__toggle-col' },
]

function formatValue(value: unknown): string {
  if (value === null || value === undefined) {
    return '—'
  }
  return typeof value === 'object' ? JSON.stringify(value) : String(value)
}

function ChangeDetails({ log }: { log: AuditLog }) {
  const fields = [...new Set([...Object.keys(log.oldValues ?? {}), ...Object.keys(log.newValues ?? {})])]

  return (
    <div className="audit-details">
      <table className="table table--compact">
        <thead>
          <tr>
            <th>Alan</th>
            <th>Eski değer</th>
            <th>Yeni değer</th>
          </tr>
        </thead>
        <tbody>
          {fields.map((field) => (
            <tr key={field}>
              <td><code>{field}</code></td>
              <td>{formatValue(log.oldValues?.[field])}</td>
              <td>{formatValue(log.newValues?.[field])}</td>
            </tr>
          ))}
        </tbody>
      </table>
      <p className="muted">
        IP: {log.ipAddress ?? '—'} · Cihaz: {log.deviceInfo ?? '—'} · İzleme no: {log.correlationId ?? '—'}
      </p>
    </div>
  )
}

export function AuditLogsPage() {
  const grid = useGrid()
  const { page, setPage } = grid
  const [filters, setFilters] = useState<Filters>({})
  const [expandedId, setExpandedId] = useState<number | null>(null)

  const { data, isLoading, isFetching, error } = useGetAuditLogsQuery({
    page,
    pageSize: PAGE_SIZE,
    ...filters,
    ...grid.params,
  })

  const handleFilter = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const form = new FormData(event.currentTarget)
    setPage(1)
    setExpandedId(null)
    setFilters({
      from: localInputToIso(form.get('from') as string),
      to: localInputToIso(form.get('to') as string),
    })
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">İşlem Kayıtları</h1>

      <form className="toolbar toolbar--filters" onSubmit={handleFilter}>
        <label>
          Başlangıç
          <input name="from" type="datetime-local" />
        </label>
        <label>
          Bitiş
          <input name="to" type="datetime-local" />
        </label>
        <button type="submit" className="button button--primary"><Filter aria-hidden="true" />Filtrele</button>
        <ClearFiltersButton grid={grid} />
      </form>

      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />

      {data && (
        <>
          <div className="table-wrap" aria-busy={isFetching}>
            <table className="table">
              <GridHead columns={columns} grid={grid} />
              <tbody>
                {data.items.map((log) => {
                  const action = actionPresentation[log.action]
                  const expanded = expandedId === log.id
                  return (
                    <Fragment key={log.id}>
                      <tr
                        className={expanded ? 'table__row--link table__row--expanded' : 'table__row--link'}
                        onClick={() => setExpandedId(expanded ? null : log.id)}
                      >
                        <td>{formatDateTime(log.createdAt)}</td>
                        <td>{log.userName}</td>
                        <td><span className={`badge badge--${action.tone}`}>{action.label}</span></td>
                        <td>{entityLabel(log.entityName)}</td>
                        <td><code>{log.entityId}</code></td>
                        <td className="table__toggle-col">
                          <button
                            type="button"
                            className="table__toggle"
                            aria-expanded={expanded}
                            aria-label={expanded ? 'Ayrıntıyı gizle' : 'Ayrıntıyı göster'}
                            onClick={(event) => {
                              event.stopPropagation()
                              setExpandedId(expanded ? null : log.id)
                            }}
                          />
                        </td>
                      </tr>
                      {expanded && (
                        <tr className="table__detail-row">
                          <td colSpan={6}><ChangeDetails log={log} /></td>
                        </tr>
                      )}
                    </Fragment>
                  )
                })}
                {data.items.length === 0 && (
                  <tr>
                    <td colSpan={6} className="table__empty">Kayıt bulunamadı.</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
          <Pagination
            page={data.page}
            totalPages={data.totalPages}
            totalCount={data.totalCount}
            onPageChange={(next) => {
              setExpandedId(null)
              setPage(next)
            }}
          />
        </>
      )}
    </section>
  )
}
