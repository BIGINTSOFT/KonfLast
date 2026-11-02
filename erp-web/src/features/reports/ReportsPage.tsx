import { Download } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { ErrorAlert } from '../../components/FormControls'
import { usePermission } from '../../hooks/usePermission'
import { downloadFile } from '../../services/download'
import { Permissions } from '../../types/permissions'
import { formatDateTime, todayIsoDate } from '../../utils/format'
import { dayRangeToUtc } from '../productionTracking/trackingPresentation'
import { formatReportCell, groupReports, isNumericColumn, reportFileName } from './reportPresentation'
import { reportExportPath, useGetReportCatalogQuery, useLazyRunReportQuery } from './reportsApi'
import type { ReportParams, ReportTable } from './reportsApi'

function ReportResult({ table }: { table: ReportTable }) {
  return (
    <>
      <p className="muted">
        {table.rows.length} satır · oluşturulma {formatDateTime(table.generatedAt)}
      </p>
      {table.truncated && (
        <p className="alert alert--info" role="status">
          Liste satır sınırına ulaştı; yalnızca ilk {table.rows.length} satır gösteriliyor. Aralığı daraltın ya da arama yapın.
        </p>
      )}
      <div className="table-wrap">
        <table className="table table--compact">
          <caption className="visually-hidden">{table.title}</caption>
          <thead>
            <tr>
              {table.columns.map((column) => (
                <th key={column.key} className={isNumericColumn(column.type) ? 'table__number' : undefined}>
                  {column.title}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {table.rows.map((row, rowIndex) => (
              // Rapor satırlarının kendi kimliği yoktur; liste yeniden sıralanmadığı için sıra numarası yeterlidir.
              <tr key={rowIndex}>
                {row.map((cell, index) => (
                  <td key={table.columns[index].key} className={isNumericColumn(table.columns[index].type) ? 'table__number' : undefined}>
                    {formatReportCell(cell, table.columns[index].type)}
                  </td>
                ))}
              </tr>
            ))}
            {table.rows.length === 0 && (
              <tr>
                <td colSpan={table.columns.length} className="table__empty">Bu ölçütlere uyan kayıt yok.</td>
              </tr>
            )}
          </tbody>
          {table.totals && (
            <tfoot>
              <tr>
                {table.totals.map((cell, index) => (
                  <td key={table.columns[index].key} className={isNumericColumn(table.columns[index].type) ? 'table__number' : undefined}>
                    <strong>{cell === null ? '' : formatReportCell(cell, table.columns[index].type)}</strong>
                  </td>
                ))}
              </tr>
            </tfoot>
          )}
        </table>
      </div>
    </>
  )
}

/**
 * Raporlar: rapor seçilir, ölçütler verilir, sonuç görüntülenir ya da CSV olarak indirilir. Grup verilirse
 * (ör. "Depo") yalnızca o gruptaki raporlar listelenir.
 */
export function ReportsPage({ group }: { group?: string }) {
  const { data: allReports, error: catalogError, isLoading } = useGetReportCatalogQuery()
  const catalog = group ? allReports?.filter((report) => report.group === group) : allReports
  const [run, result] = useLazyRunReportQuery()
  // Dışa aktarma (CSV) ayrı yetkidir; rapor ekranda yine görüntülenebilir.
  const canExport = usePermission(Permissions.ReportExport)

  const [selectedKey, setSelectedKey] = useState<string | null>(null)
  const [fromDate, setFromDate] = useState(todayIsoDate)
  const [toDate, setToDate] = useState(todayIsoDate)
  const [search, setSearch] = useState('')
  // Boşsa raporun varsayılan gün sayısı kullanılır.
  const [days, setDays] = useState('')
  const [shownKey, setShownKey] = useState<string | null>(null)
  const [downloading, setDownloading] = useState(false)
  const [downloadError, setDownloadError] = useState<unknown>(null)

  const definition = catalog?.find((report) => report.key === selectedKey) ?? catalog?.[0]
  const range = definition?.requiresDateRange ? dayRangeToUtc(fromDate, toDate) : null
  const daysValid = !definition?.daysLabel || days.trim() === '' || /^\d{1,4}$/.test(days.trim())
  const ready = definition !== undefined && (!definition.requiresDateRange || range !== null) && daysValid

  const params = (): ReportParams | null =>
    definition && ready
      ? {
        key: definition.key,
        ...(range ?? {}),
        search: definition.searchLabel ? search.trim() || undefined : undefined,
        days: definition.daysLabel ? days.trim() || undefined : undefined,
      }
      : null

  const handleRun = (event: FormEvent) => {
    event.preventDefault()
    const current = params()
    if (current) {
      setDownloadError(null)
      setShownKey(current.key)
      void run(current)
    }
  }

  const handleDownload = async () => {
    const current = params()
    if (!current || !definition) {
      return
    }

    setDownloading(true)
    setDownloadError(null)
    try {
      await downloadFile(
        reportExportPath(current),
        reportFileName(definition.key, definition.requiresDateRange ? fromDate : null, definition.requiresDateRange ? toDate : null),
      )
    } catch (error) {
      setDownloadError(error)
    } finally {
      setDownloading(false)
    }
  }

  // Başka bir rapor seçildiğinde önceki raporun sonucu gösterilmez.
  const table = result.data && result.data.key === shownKey && shownKey === definition?.key ? result.data : null

  return (
    <section className="page">
      <h1 className="visually-hidden">{group ? `${group} raporları` : 'Raporlar'}</h1>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={catalogError} />

      {catalog?.length === 0 && <p className="muted">Görüntüleyebileceğiniz rapor yok.</p>}

      {catalog && definition && (
        <>
          <form className="toolbar toolbar--filters" onSubmit={handleRun}>
            <label>
              Rapor
              <select value={definition.key} onChange={(event) => setSelectedKey(event.target.value)}>
                {groupReports(catalog, 'Üretim ve kalite').map((group) => (
                  <optgroup key={group.title} label={group.title}>
                    {group.reports.map((report) => (
                      <option key={report.key} value={report.key}>{report.title}</option>
                    ))}
                  </optgroup>
                ))}
              </select>
            </label>
            {definition.requiresDateRange && (
              <>
                <label>
                  Başlangıç
                  <input type="date" value={fromDate} max={toDate} onChange={(event) => setFromDate(event.target.value)} />
                </label>
                <label>
                  Bitiş
                  <input type="date" value={toDate} min={fromDate} onChange={(event) => setToDate(event.target.value)} />
                </label>
              </>
            )}
            {definition.daysLabel && (
              <label>
                {definition.daysLabel}
                <input
                  type="number"
                  min="1"
                  max="3650"
                  step="1"
                  className="packing-box"
                  placeholder={definition.defaultDays ? String(definition.defaultDays) : undefined}
                  value={days}
                  onChange={(event) => setDays(event.target.value)}
                />
              </label>
            )}
            {definition.searchLabel && (
              <label>
                {definition.searchLabel}
                <input type="search" value={search} onChange={(event) => setSearch(event.target.value)} />
              </label>
            )}
            <button type="submit" className="button button--primary" disabled={!ready || result.isFetching}>
              {result.isFetching ? 'Hazırlanıyor…' : 'Raporu çalıştır'}
            </button>
            {canExport && (
              <button type="button" className="button" disabled={!ready || downloading} onClick={() => void handleDownload()}>
                <Download aria-hidden="true" />
                {downloading ? 'İndiriliyor…' : 'CSV indir'}
              </button>
            )}
          </form>

          <p className="muted">
            {definition.description}
            {definition.requiresDateRange && ` Tarih aralığı en fazla ${definition.maxRangeDays} gün olabilir.`}
          </p>

          {definition.requiresDateRange && !range && (
            <p className="alert alert--error" role="alert">Bitiş tarihi başlangıçtan önce olamaz.</p>
          )}
          {shownKey === definition.key && <ErrorAlert error={result.error} />}
          <ErrorAlert error={downloadError} />

          {table && <ReportResult table={table} />}
        </>
      )}
    </section>
  )
}
