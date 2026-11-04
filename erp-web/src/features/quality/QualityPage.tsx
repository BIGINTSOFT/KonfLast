import { Search } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { ErrorAlert } from '../../components/FormControls'
import { RecordLink } from '../../components/RecordLink'
import { GridHead } from '../../components/grid/GridHead'
import type { GridColumn } from '../../components/grid/GridHead'
import { useGrid } from '../../components/grid/useGrid'
import { Pagination } from '../../components/Pagination'
import { formatDateTime, formatQuantity, todayIsoDate } from '../../utils/format'
import { dayRangeToUtc, formatPercent } from '../productionTracking/trackingPresentation'
import { QualityResultBadge, ReworkStatusBadge } from './QualityBadges'
import { useGetDefectSummaryQuery, useGetInspectionsQuery, useGetReworkTasksQuery } from './qualityApi'
import { defectsText, qualityResultPresentation, reworkStatusPresentation } from './qualityPresentation'
import { ClearFiltersButton } from '../../components/grid/ClearFiltersButton'

const PAGE_SIZE = 20

const optionsOf = (presentation: Record<string, { label: string }>) =>
  Object.entries(presentation).map(([value, { label }]) => ({ value, label }))

const reworkColumns: GridColumn[] = [
  { key: 'id', label: 'Tamir', filter: 'number' },
  { key: 'bundleNo', label: 'Demet' },
  { key: 'orderNo', label: 'İş emri' },
  { key: 'operationName', label: 'Operasyon' },
  { key: 'quantity', label: 'Adet', numeric: true },
  { key: 'status', label: 'Durum', filter: optionsOf(reworkStatusPresentation) },
  { key: 'createdAt', label: 'Açılış', filter: false },
]

const inspectionColumns: GridColumn[] = [
  { key: 'inspectedAt', label: 'Zaman', filter: false },
  { key: 'bundleNo', label: 'Demet' },
  { key: 'operationName', label: 'Operasyon' },
  { key: 'result', label: 'Sonuç', filter: optionsOf(qualityResultPresentation) },
  { key: 'inspectedQuantity', label: 'Kontrol', numeric: true },
  { key: 'rejectedQuantity', label: 'Ret', numeric: true },
  { key: 'reworkQuantity', label: 'Tamir', numeric: true },
  { label: 'Hatalar' },
  { key: 'inspectedBy', label: 'Kontrol eden' },
]

function ReworkSection() {
  const grid = useGrid()
  const { page, setPage } = grid
  const [pendingOnly, setPendingOnly] = useState(true)
  const { data, error } = useGetReworkTasksQuery(
    { page, pageSize: PAGE_SIZE, pending: pendingOnly ? true : undefined, ...grid.params },
    { pollingInterval: 60_000 },
  )

  return (
    <>
      <h2 className="section-title">Tamir işleri</h2>
      <div className="toolbar">
        <label className="checkbox">
          <input
            type="checkbox"
            checked={pendingOnly}
            onChange={(event) => {
              setPage(1)
              setPendingOnly(event.target.checked)
            }}
          />
          Yalnızca bekleyenler (tamir ya da tekrar kontrol bekleyen)
        </label>
        <ClearFiltersButton grid={grid} />
      </div>
      <ErrorAlert error={error} />
      {data && (
        <>
          <div className="table-wrap">
            <table className="table">
              <GridHead columns={reworkColumns} grid={grid} />
              <tbody>
                {data.items.map((task) => (
                  <tr key={task.id}>
                    <td>#{task.id}</td>
                    <td><RecordLink to={`/bundles/${task.bundleId}`}><code>{task.bundleNo}</code></RecordLink></td>
                    <td>{task.orderNo}</td>
                    <td><strong>{task.sequenceNo}</strong> {task.operationName}</td>
                    <td className="table__number">{formatQuantity(task.quantity)}</td>
                    <td><ReworkStatusBadge status={task.status} /></td>
                    <td>{formatDateTime(task.createdAt)} · {task.createdBy}</td>
                  </tr>
                ))}
                {data.items.length === 0 && (
                  <tr>
                    <td colSpan={7} className="table__empty">Tamir işi yok.</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}
    </>
  )
}

function DefectSection() {
  const [fromDate, setFromDate] = useState(todayIsoDate)
  const [toDate, setToDate] = useState(todayIsoDate)
  const range = dayRangeToUtc(fromDate, toDate)
  const { data, error } = useGetDefectSummaryQuery(range ?? { from: '', to: '' }, { skip: !range })

  return (
    <>
      <h2 className="section-title">Hata dağılımı</h2>
      <div className="toolbar toolbar--filters">
        <label>
          Başlangıç
          <input type="date" value={fromDate} max={toDate} onChange={(event) => setFromDate(event.target.value)} />
        </label>
        <label>
          Bitiş
          <input type="date" value={toDate} min={fromDate} onChange={(event) => setToDate(event.target.value)} />
        </label>
        {range && data && (
          <p className="toolbar__summary">
            {data.inspectionCount} kontrol · {formatQuantity(data.inspectedQuantity)} adet kontrol edildi ·{' '}
            {formatQuantity(data.rejectedQuantity)} ret · {formatQuantity(data.reworkQuantity)} tamir · hata oranı{' '}
            <strong>{formatPercent(data.defectRatePercent)}</strong>
          </p>
        )}
      </div>
      {!range && <p className="alert alert--error" role="alert">Bitiş tarihi başlangıçtan önce olamaz.</p>}
      <ErrorAlert error={error} />
      {range && data && (
        <>
          {data.defects.length > 0 && (
            <div className="table-wrap">
              <table className="table table--compact">
                <thead>
                  <tr>
                    <th>Hata türü</th>
                    <th className="table__number">Adet</th>
                    <th className="table__number">Kontrol sayısı</th>
                  </tr>
                </thead>
                <tbody>
                  {data.defects.map((defect) => (
                    <tr key={defect.defectTypeId}>
                      <td>{defect.name}</td>
                      <td className="table__number">{formatQuantity(defect.quantity)}</td>
                      <td className="table__number">{defect.inspectionCount}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </>
      )}
    </>
  )
}

function InspectionsSection() {
  const grid = useGrid()
  const { page, setPage } = grid
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const { data, error, isFetching } = useGetInspectionsQuery({
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

  return (
    <>
      <h2 className="section-title">Kalite kontrolleri</h2>
      <form className="toolbar" onSubmit={handleSearch} role="search">
        <input
          type="search"
          placeholder="Demet no veya sipariş no"
          aria-label="Kontrol ara"
          value={searchInput}
          onChange={(event) => setSearchInput(event.target.value)}
        />
        <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
        <ClearFiltersButton grid={grid} />
      </form>
      <ErrorAlert error={error} />
      {data && (
        <>
          <div className="table-wrap" aria-busy={isFetching}>
            <table className="table">
              <GridHead columns={inspectionColumns} grid={grid} />
              <tbody>
                {data.items.map((inspection) => (
                  <tr key={inspection.id}>
                    <td>{formatDateTime(inspection.inspectedAt)}</td>
                    <td>
                      <RecordLink to={`/bundles/${inspection.bundleId}`}><code>{inspection.bundleNo}</code></RecordLink>{' '}
                      <span className="muted">{inspection.orderNo}</span>
                    </td>
                    <td><strong>{inspection.sequenceNo}</strong> {inspection.operationName}</td>
                    <td><QualityResultBadge result={inspection.result} /></td>
                    <td className="table__number">{formatQuantity(inspection.inspectedQuantity)}</td>
                    <td className="table__number">{formatQuantity(inspection.rejectedQuantity)}</td>
                    <td className="table__number">{formatQuantity(inspection.reworkQuantity)}</td>
                    <td>{defectsText(inspection)}</td>
                    <td>{inspection.inspectedBy}</td>
                  </tr>
                ))}
                {data.items.length === 0 && (
                  <tr>
                    <td colSpan={9} className="table__empty">Kayıt bulunamadı.</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}
    </>
  )
}

/** Kalite: bekleyen tamir işleri, hata dağılımı ve kontrol geçmişi. Kontrol, demet sayfasından girilir. */
export function QualityPage() {
  return (
    <section className="page">
      <h1 className="visually-hidden">Kalite</h1>
      <ReworkSection />
      <DefectSection />
      <InspectionsSection />
    </section>
  )
}
