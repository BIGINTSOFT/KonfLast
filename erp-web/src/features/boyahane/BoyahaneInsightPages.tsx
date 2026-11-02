import { Search } from 'lucide-react'
import { Fragment, useState } from 'react'
import type { FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { ClearFiltersButton } from '../../components/grid/ClearFiltersButton'
import { GridHead } from '../../components/grid/GridHead'
import type { GridColumn } from '../../components/grid/GridHead'
import { useGrid } from '../../components/grid/useGrid'
import { Pagination } from '../../components/Pagination'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDate, formatDateTime, formatQuantity, todayIsoDate } from '../../utils/format'
import { batchStatusPresentation, batchStepStatusPresentation, colorCheckResultLabels, useSetBatchCardMutation } from './boyahaneBatchesApi'
import type { Batch } from './boyahaneBatchesApi'
import { BoyahaneCapacityPlanPage } from './BoyahaneCapacityPlanPage'
import { dyeMachineStatusLabels } from './boyahanePresentation'
import {
  useGetBoyahaneGoodsQuery,
  useGetBoyahaneIdleStockQuery,
  useGetBoyahaneOrderTrackingQuery,
  useGetBoyahaneReportQuery,
  useGetBoyahaneShipmentReportQuery,
  useGetBoyahaneTrackingQuery,
} from './boyahaneInsightsApi'
import type { IdleGoodsType, ShipmentTotals } from './boyahaneInsightsApi'
import { orderProcessTypeLabels, orderUnitLabels } from './boyahaneOrdersApi'
import { baseApi } from '../../api/baseApi'

const PAGE_SIZE = 20

function useSearch(setPage: (page: number) => void) {
  const [input, setInput] = useState('')
  const [search, setSearch] = useState('')
  const submit = (event: FormEvent) => {
    event.preventDefault()
    setPage(1)
    setSearch(input.trim())
  }
  return { input, setInput, search, submit }
}

/** Üretim takibi: başlamış bütün adımlar, en yeniden eskiye. */
export function BoyahaneTrackingPage() {
  const grid = useGrid()
  const { page, setPage } = grid
  const { input, setInput, search, submit } = useSearch(setPage)
  const { data, error, isLoading, isFetching } = useGetBoyahaneTrackingQuery({ page, pageSize: PAGE_SIZE, search: search || undefined, ...grid.params })

  const columns: GridColumn[] = [
    { key: 'batchNo', label: 'Parti' },
    { key: 'operationName', label: 'Operasyon' },
    { key: 'status', label: 'Durum', filter: Object.entries(batchStepStatusPresentation).map(([value, { label }]) => ({ value, label })) },
    { key: 'machineCode', label: 'Makine' },
    { key: 'startedBy', label: 'Başlayan' },
    { key: 'completedBy', label: 'Bitiren' },
    { key: 'startedAt', label: 'Başlangıç', filter: false },
    { key: 'completedAt', label: 'Bitiş', filter: false },
    { key: 'colorResult', label: 'Sonuç', filter: false },
  ]

  return (
    <section className="page">
      <h1 className="visually-hidden">Üretim Takibi</h1>
      <form className="toolbar" onSubmit={submit} role="search">
        <input type="search" placeholder="Parti, makine ya da personel" aria-label="Adım ara" value={input} onChange={(event) => setInput(event.target.value)} />
        <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
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
                {data.items.map((row) => {
                  const status = batchStepStatusPresentation[row.status]
                  return (
                    <tr key={row.id}>
                      <td><Link to={`/boyahane/batches/${row.batchId}`}><code>{row.batchNo}</code></Link></td>
                      <td>{row.operationName}</td>
                      <td><span className={`badge badge--${status.tone}`}>{status.label}</span></td>
                      <td>{row.machineCode ?? '—'}</td>
                      <td>{row.startedBy ?? '—'}</td>
                      <td>{row.completedBy ?? '—'}</td>
                      <td>{row.startedAt ? formatDateTime(row.startedAt) : '—'}</td>
                      <td>{row.completedAt ? formatDateTime(row.completedAt) : '—'}</td>
                      <td>{row.colorResult ? colorCheckResultLabels[row.colorResult] : '—'}</td>
                    </tr>
                  )
                })}
                {data.items.length === 0 && <tr><td colSpan={columns.length} className="table__empty">Başlamış adım yok.</td></tr>}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}
    </section>
  )
}

/**
 * Makine planı (/boyahane/planning): ileriye dönük kapasite planı. Önceki tarih bazlı liste kapasite planının tablo görünümüne
 * taşındı; rota ve menü kaydı değişmesin diye bu ad korunur.
 */
export function BoyahanePlanningPage() {
  return <BoyahaneCapacityPlanPage />
}

/** Mal durumu: sahibine ve kartına göre malın bulunduğu aşama (kg). */
export function BoyahaneGoodsPage() {
  const { data, error, isLoading } = useGetBoyahaneGoodsQuery()

  return (
    <section className="page">
      <h1 className="visually-hidden">Mal Durumu</h1>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {data && (
        <div className="table-wrap">
          <table className="table">
            <thead>
              <tr>
                <th>Malın sahibi</th><th>Kart</th>
                <th className="table__number">Kontrolde</th><th className="table__number">Boşta</th><th className="table__number">Siparişte</th>
                <th className="table__number">Partide</th><th className="table__number">Tamamlandı</th><th className="table__number">Red</th>
              </tr>
            </thead>
            <tbody>
              {data.map((row) => (
                <tr key={`${row.ownerName}-${row.cardCode}-${row.isYarn}`}>
                  <td>{row.ownerName ?? <span className="muted">İşletme malı</span>}</td>
                  <td><code>{row.cardCode}</code> {row.cardName}</td>
                  {[row.pendingInspection, row.ready, row.assigned, row.inBatch, row.done, row.rejected].map((value, index) => (
                    <td key={index} className="table__number">{value > 0 ? formatQuantity(value) : '—'}</td>
                  ))}
                </tr>
              ))}
              {data.length === 0 && <tr><td colSpan={8} className="table__empty">Kayıtlı mal yok.</td></tr>}
            </tbody>
          </table>
        </div>
      )}
    </section>
  )
}

/** Sipariş takibi: onaylı siparişlerin satır bazında ilerlemesi, termine göre. */
export function BoyahaneOrderTrackingPage() {
  const grid = useGrid()
  const { page, setPage } = grid
  const { input, setInput, search, submit } = useSearch(setPage)
  const { data, error, isLoading, isFetching } = useGetBoyahaneOrderTrackingQuery({ page, pageSize: PAGE_SIZE, search: search || undefined, ...grid.params })
  const today = todayIsoDate()

  const columns: GridColumn[] = [
    { key: 'orderNo', label: 'Sipariş' },
    { key: 'customerName', label: 'Müşteri' },
    { key: 'dueDate', label: 'Termin', filter: false },
    { key: 'processType', label: 'İşlem', filter: Object.entries(orderProcessTypeLabels).map(([value, label]) => ({ value, label })) },
    { key: 'cardCode', label: 'Kart' },
    { key: 'colorName', label: 'Renk' },
    { key: 'quantity', label: 'Miktar', numeric: true },
    { key: 'assignedKilograms', label: 'Bağlı (kg)', numeric: true, filter: false },
    { key: 'inBatchKilograms', label: 'Partide (kg)', numeric: true, filter: false },
    { key: 'doneKilograms', label: 'Uygun (kg)', numeric: true, filter: false },
    { key: 'packedKilograms', label: 'Çekide (kg)', numeric: true, filter: false },
    { key: 'shippedKilograms', label: 'Sevk (kg)', numeric: true, filter: false },
    { label: 'Kalan', numeric: true, sortable: false },
    { label: 'Faturalanacak', numeric: true, sortable: false },
    { key: 'invoicedQuantity', label: 'Faturalanan', numeric: true, filter: false },
  ]

  return (
    <section className="page">
      <h1 className="visually-hidden">Sipariş Takibi</h1>
      <form className="toolbar" onSubmit={submit} role="search">
        <input type="search" placeholder="Sipariş no, müşteri sipariş no ya da müşteri" aria-label="Sipariş ara" value={input} onChange={(event) => setInput(event.target.value)} />
        <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
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
                {data.items.map((row) => (
                  <tr key={row.id}>
                    <td>
                      <Link to={`/boyahane/orders/${row.orderId}`}><code>{row.orderNo}</code></Link>
                      {row.customerOrderNo && <div className="muted">{row.customerOrderNo}</div>}
                    </td>
                    <td>{row.customerName ?? <span className="muted">Stoğa üretim</span>}</td>
                    <td>
                      {row.dueDate ? formatDate(row.dueDate) : '—'}
                      {row.dueDate && row.dueDate < today && <span className="badge badge--danger">Gecikmiş</span>}
                    </td>
                    <td>{orderProcessTypeLabels[row.processType]}</td>
                    <td>{row.cardCode}</td>
                    <td>{row.colorName ?? '—'}</td>
                    <td className="table__number">{formatQuantity(row.quantity)} {orderUnitLabels[row.unit]}</td>
                    <td className="table__number">{formatQuantity(row.assignedKilograms)}</td>
                    <td className="table__number">{formatQuantity(row.inBatchKilograms)}</td>
                    <td className="table__number">{formatQuantity(row.doneKilograms)}</td>
                    <td className="table__number">{formatQuantity(row.packedKilograms)}</td>
                    <td className="table__number">{formatQuantity(row.shippedKilograms)}</td>
                    <td className="table__number">
                      {row.remaining === null
                        ? <span className="muted" title="Sevk edilen paketlerden birinin metresi yok; metre kalanı hesaplanamaz">—</span>
                        : <>{formatQuantity(row.remaining)} {orderUnitLabels[row.unit]}</>}
                    </td>
                    <td className="table__number">{formatQuantity(row.billableQuantity)} {orderUnitLabels[row.billableUnit]}</td>
                    <td className="table__number">{formatQuantity(row.invoicedQuantity)} {orderUnitLabels[row.billableUnit]}</td>
                  </tr>
                ))}
                {data.items.length === 0 && <tr><td colSpan={columns.length} className="table__empty">Onaylı sipariş yok.</td></tr>}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}
    </section>
  )
}

const cardReleaseApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    findBatchByCard: build.query<Batch, string>({ query: (uid) => `/boyahane-batches/by-card/${uid}` }),
  }),
})

/** Kart boşaltma: okutulan RFID kartın partisi görülür, kart partiden çıkarılır. */
export function BoyahaneCardReleasePage() {
  const canManage = usePermission(Permissions.BoyahaneBatchManage)
  const [input, setInput] = useState('')
  const [uid, setUid] = useState<string | null>(null)
  const { data, error, isFetching } = cardReleaseApi.useFindBatchByCardQuery(uid ?? '', { skip: uid === null })
  const [setCard, state] = useSetBatchCardMutation()
  const [released, setReleased] = useState<string | null>(null)

  const submit = (event: FormEvent) => {
    event.preventDefault()
    setReleased(null)
    setUid(input.trim())
    setInput('')
  }

  const release = async () => {
    if (data && !('error' in await setCard({ id: data.id, uid: null }))) {
      setReleased(data.batchNo)
      setUid(null)
    }
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">Kart Boşaltma</h1>
      <form className="toolbar" onSubmit={submit}>
        <input aria-label="Kart numarası" placeholder="Kartı okutun" maxLength={10} autoFocus value={input} onChange={(event) => setInput(event.target.value)} />
        <button type="submit" className="button" disabled={input.trim() === ''}>Kartı bul</button>
      </form>
      {released && <div className="alert alert--info">Kart {released} partisinden çıkarıldı.</div>}
      {uid !== null && !isFetching && <ErrorAlert error={error ?? state.error} />}
      {uid !== null && data && (
        <article className="card">
          <h2><Link to={`/boyahane/batches/${data.id}`}>{data.batchNo}</Link> <span className={`badge badge--${batchStatusPresentation[data.status].tone}`}>{batchStatusPresentation[data.status].label}</span></h2>
          <p className="muted">{data.machineCode} · {formatQuantity(data.totalKilograms)} kg · {data.items.length} top / lot</p>
          {canManage && <button type="button" className="button button--primary" disabled={state.isLoading} onClick={release}>Kartı boşalt</button>}
        </article>
      )}
    </section>
  )
}

const iso = (date: Date) => date.toISOString().slice(0, 10)

/** Raporlar: tüketim, renk tutturma, kalite, operasyon süreleri, personel ve makine duruşları. */
export function BoyahaneReportsPage() {
  const [from, setFrom] = useState(() => iso(new Date(Date.now() - 30 * 86_400_000)))
  const [to, setTo] = useState(todayIsoDate)
  const { data, error, isLoading } = useGetBoyahaneReportQuery({ from, to })
  const rate = data && data.colorCheckedBatches > 0 ? Math.round((data.colorFirstPassBatches / data.colorCheckedBatches) * 100) : null

  return (
    <section className="page">
      <h1 className="visually-hidden">Raporlar</h1>
      <div className="toolbar">
        <label className="field"><span>Başlangıç</span><input type="date" value={from} onChange={(event) => setFrom(event.target.value)} /></label>
        <label className="field"><span>Bitiş</span><input type="date" value={to} onChange={(event) => setTo(event.target.value)} /></label>
      </div>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {data && (
        <>
          <h2 className="section-title">Renk ve kalite</h2>
          <div className="table-wrap">
            <table className="table">
              <thead><tr><th className="table__number">Kontrol edilen parti</th><th className="table__number">İlk seferde tutan</th><th className="table__number">Oran</th><th className="table__number">İlave</th><th className="table__number">Yeniden işlem</th><th className="table__number">Söküm</th><th className="table__number">Söküm oranı</th><th className="table__number">Uygun top</th><th className="table__number">Red</th><th className="table__number">Yeniden işleme ayrılan</th></tr></thead>
              <tbody>
                <tr>
                  <td className="table__number">{data.colorCheckedBatches}</td>
                  <td className="table__number">{data.colorFirstPassBatches}</td>
                  <td className="table__number">{rate === null ? '—' : `%${rate}`}</td>
                  <td className="table__number">{data.additions}</td>
                  <td className="table__number">{data.redos}</td>
                  <td className="table__number">{data.strippings}</td>
                  <td className="table__number" title="Biten renk kontrollerinden söküm + yeniden boya sonucunun oranı">{data.strippingRatePercent === null ? '—' : `%${data.strippingRatePercent}`}</td>
                  <td className="table__number">{data.itemsApproved}</td>
                  <td className="table__number">{data.itemsRejected}</td>
                  <td className="table__number">{data.itemsReworked}</td>
                </tr>
              </tbody>
            </table>
          </div>

          <h2 className="section-title">Boya ve kimyasal tüketimi</h2>
          <div className="table-wrap">
            <table className="table">
              <thead><tr><th>Kod</th><th>Ad</th><th className="table__number">Hesaplanan (kg)</th><th className="table__number">Gerçek (kg)</th><th className="table__number">Fark (kg)</th><th className="table__number">Lotsuz (kg)</th></tr></thead>
              <tbody>
                {data.consumption.map((row) => (
                  <tr key={row.code}>
                    <td><code>{row.code}</code></td><td>{row.name}</td>
                    <td className="table__number">{formatQuantity(row.plannedKilograms)}</td>
                    <td className="table__number">{formatQuantity(row.actualKilograms)}</td>
                    <td className="table__number">{formatQuantity(row.actualKilograms - row.plannedKilograms)}</td>
                    <td className="table__number">{row.withoutLotKilograms > 0 ? formatQuantity(row.withoutLotKilograms) : '—'}</td>
                  </tr>
                ))}
                {data.consumption.length === 0 && <tr><td colSpan={6} className="table__empty">Kayıt yok.</td></tr>}
              </tbody>
            </table>
          </div>

          <h2 className="section-title">Fire</h2>
          <div className="table-wrap">
            <table className="table">
              <thead><tr><th>Malın sahibi</th><th>Kart</th><th className="table__number">Top / lot</th><th className="table__number">Giriş (kg)</th><th className="table__number">Çıkış (kg)</th><th className="table__number">Fire</th><th className="table__number">Kabul edilen</th></tr></thead>
              <tbody>
                {data.fire.map((row) => (
                  <tr key={`${row.ownerName}-${row.cardCode}`}>
                    <td>{row.ownerName ?? <span className="muted">İşletme malı</span>}</td>
                    <td>{row.cardCode}</td>
                    <td className="table__number">{row.items}</td>
                    <td className="table__number">{formatQuantity(row.inputKilograms)}</td>
                    <td className="table__number">{formatQuantity(row.finalKilograms)}</td>
                    <td className="table__number">
                      {row.firePercent === null ? '—' : `%${formatQuantity(row.firePercent)}`}
                      {row.overTolerance && <span className="badge badge--warning">Tolerans aşıldı</span>}
                    </td>
                    <td className="table__number">{row.tolerancePercent === null ? '—' : `%${formatQuantity(row.tolerancePercent)}`}</td>
                  </tr>
                ))}
                {data.fire.length === 0 && <tr><td colSpan={7} className="table__empty">Kayıt yok.</td></tr>}
              </tbody>
            </table>
          </div>

          <h2 className="section-title">Operasyon süreleri</h2>
          <div className="table-wrap">
            <table className="table">
              <thead><tr><th>Operasyon</th><th className="table__number">Adet</th><th className="table__number">Ortalama (dk)</th><th className="table__number">Standart (dk)</th></tr></thead>
              <tbody>
                {data.operations.map((row) => (
                  <tr key={row.name}>
                    <td>{row.name}</td>
                    <td className="table__number">{row.count}</td>
                    <td className="table__number">{formatQuantity(row.averageMinutes)}</td>
                    <td className="table__number">{row.standardMinutes === null ? '—' : formatQuantity(row.standardMinutes)}</td>
                  </tr>
                ))}
                {data.operations.length === 0 && <tr><td colSpan={4} className="table__empty">Kayıt yok.</td></tr>}
              </tbody>
            </table>
          </div>

          <h2 className="section-title">Operatörler</h2>
          <div className="table-wrap">
            <table className="table">
              <thead><tr><th>Operatör</th><th className="table__number">Biten adım</th><th className="table__number">Süre (dk)</th></tr></thead>
              <tbody>
                {data.operators.map((row) => (
                  <tr key={row.name}><td>{row.name}</td><td className="table__number">{row.steps}</td><td className="table__number">{formatQuantity(row.minutes)}</td></tr>
                ))}
                {data.operators.length === 0 && <tr><td colSpan={3} className="table__empty">Kayıt yok.</td></tr>}
              </tbody>
            </table>
          </div>

          <h2 className="section-title">Makine duruşları</h2>
          <div className="table-wrap">
            <table className="table">
              <thead><tr><th>Makine</th><th>Durum</th><th className="table__number">Adet</th><th className="table__number">Süre (dk)</th></tr></thead>
              <tbody>
                {data.downtime.map((row) => (
                  <tr key={`${row.machineCode}-${row.status}`}><td>{row.machineCode}</td><td>{dyeMachineStatusLabels[row.status]}</td><td className="table__number">{row.count}</td><td className="table__number">{formatQuantity(row.minutes)}</td></tr>
                ))}
                {data.downtime.length === 0 && <tr><td colSpan={4} className="table__empty">Kayıt yok.</td></tr>}
              </tbody>
            </table>
          </div>
        </>
      )}
      <DailyShipmentSection from={from} to={to} />
      <IdleStockSection />
    </section>
  )
}

function ShipmentCells({ totals }: { totals: ShipmentTotals }) {
  return (
    <>
      <td className="table__number">{totals.despatchCount}</td>
      <td className="table__number">{totals.packageCount}</td>
      <td className="table__number">{formatQuantity(totals.kilograms)}</td>
      <td className="table__number">{totals.meters > 0 ? formatQuantity(totals.meters) : '—'}</td>
    </>
  )
}

/** Günlük sevk: onaylı irsaliyeler gün ve müşteri bazında; gün ara toplamı ve genel toplam. */
function DailyShipmentSection({ from, to }: { from: string; to: string }) {
  const { data, error, isLoading } = useGetBoyahaneShipmentReportQuery({ from, to })

  return (
    <>
      <h2 className="section-title">Günlük sevk</h2>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {data && (
        <div className="table-wrap">
          <table className="table">
            <thead><tr><th>Gün</th><th>Müşteri</th><th className="table__number">İrsaliye</th><th className="table__number">Paket</th><th className="table__number">Kilo</th><th className="table__number">Metre</th></tr></thead>
            <tbody>
              {data.days.map((day) => (
                <Fragment key={day.day}>
                  {day.customers.map((row, index) => (
                    <tr key={row.customerId}>
                      <td>{index === 0 ? formatDate(day.day) : ''}</td>
                      <td>{row.customerName}</td>
                      <ShipmentCells totals={row.totals} />
                    </tr>
                  ))}
                  <tr>
                    <th scope="row" colSpan={2}>{formatDate(day.day)} toplamı</th>
                    <ShipmentCells totals={day.total} />
                  </tr>
                </Fragment>
              ))}
              {data.days.length === 0 && <tr><td colSpan={6} className="table__empty">Onaylı irsaliye yok.</td></tr>}
            </tbody>
            {data.days.length > 0 && (
              <tfoot>
                <tr><th scope="row" colSpan={2}>Genel toplam</th><ShipmentCells totals={data.total} /></tr>
              </tfoot>
            )}
          </table>
        </div>
      )}
    </>
  )
}

const idleGoodsTypeLabels: Record<IdleGoodsType, string> = {
  FABRIC_ROLL: 'Top',
  YARN_LOT: 'İplik lotu',
  CHEMICAL_LOT: 'Boya / kimyasal lotu',
  PACKAGE: 'Paket',
}

/** Hareketsiz stok: verilen gün sayısıdır adresi değişmeyen mal ve hareket görmeyen boya / kimyasal lotları. */
function IdleStockSection() {
  const [input, setInput] = useState('90')
  const [days, setDays] = useState(90)
  const { data, error, isLoading, isFetching } = useGetBoyahaneIdleStockQuery({ days })

  const submit = (event: FormEvent) => {
    event.preventDefault()
    const value = Math.trunc(Number(input))
    if (value >= 1) {
      setDays(value)
    }
  }

  return (
    <>
      <h2 className="section-title">Hareketsiz stok</h2>
      <form className="toolbar" onSubmit={submit}>
        <label className="field"><span>Hareketsiz gün</span><input type="number" min="1" max="3650" step="1" required value={input} onChange={(event) => setInput(event.target.value)} /></label>
        <button type="submit" className="button">Listele</button>
      </form>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {data && (
        <div aria-busy={isFetching}>
          {data.truncated && <div className="alert alert--info">Liste uzun; en uzun bekleyen kayıtlar gösteriliyor.</div>}
          <h3 className="section-title">Depo adreslerindeki mal</h3>
          <div className="table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <th>Tür</th><th>No</th><th>Kart</th><th>Malın sahibi</th><th>Depo / adres</th>
                  <th className="table__number">Kilo</th><th className="table__number">Metre</th><th>Son hareket</th><th className="table__number">Bekleme (gün)</th>
                </tr>
              </thead>
              <tbody>
                {data.goods.map((row) => (
                  <tr key={`${row.goodsType}-${row.goodsNo}`}>
                    <td>{idleGoodsTypeLabels[row.goodsType]}</td>
                    <td><code>{row.goodsNo}</code></td>
                    <td><code>{row.cardCode}</code> {row.cardName}</td>
                    <td>{row.ownerName ?? <span className="muted">İşletme malı</span>}</td>
                    <td>{row.warehouseName ?? '—'}{row.locationCode && ` · ${row.locationCode}`}</td>
                    <td className="table__number">{formatQuantity(row.kilograms)}</td>
                    <td className="table__number">{row.meters !== null ? formatQuantity(row.meters) : '—'}</td>
                    <td>{formatDateTime(row.lastMovementAt)}</td>
                    <td className="table__number">{row.idleDays}</td>
                  </tr>
                ))}
                {data.goods.length === 0 && <tr><td colSpan={9} className="table__empty">{data.days} gündür hareketsiz mal yok.</td></tr>}
              </tbody>
            </table>
          </div>

          <h3 className="section-title">Boya ve kimyasal lotları</h3>
          <div className="table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <th>Lot</th><th>Malzeme</th><th>Tedarikçi</th><th>Depo / adres</th>
                  <th className="table__number">Kalan (kg)</th><th>Son hareket</th><th className="table__number">Bekleme (gün)</th>
                </tr>
              </thead>
              <tbody>
                {data.chemicals.map((row) => (
                  <tr key={row.lotNo}>
                    <td><code>{row.lotNo}</code></td>
                    <td><code>{row.code}</code> {row.name}</td>
                    <td>{row.supplierName ?? '—'}</td>
                    <td>{row.warehouseName ?? '—'}{row.locationCode && ` · ${row.locationCode}`}</td>
                    <td className="table__number">{formatQuantity(row.remainingKilograms)}</td>
                    <td>{formatDateTime(row.lastMovementAt)}</td>
                    <td className="table__number">{row.idleDays}</td>
                  </tr>
                ))}
                {data.chemicals.length === 0 && <tr><td colSpan={7} className="table__empty">{data.days} gündür hareketsiz lot yok.</td></tr>}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </>
  )
}
