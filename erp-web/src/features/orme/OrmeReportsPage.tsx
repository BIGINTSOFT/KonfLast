import { Search } from 'lucide-react'
import { Fragment, useState } from 'react'
import type { FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { Pagination } from '../../components/Pagination'
import { formatDate, formatQuantity, todayIsoDate } from '../../utils/format'
import {
  useGetOrmeDailyShipmentReportQuery,
  useGetOrmeIdleStockReportQuery,
  useGetOrmeMachineReportQuery,
  useGetOrmeOrderReportQuery,
  useGetOrmeQualityReportQuery,
} from './ormeFloorApi'
import { ormeRollStatusPresentation } from './ormeKnitApi'
import type { OrmeQualityGroup } from './ormeFloorApi'
import { formatMinutes } from './ormePresentation'

type Tab = 'machines' | 'orders' | 'quality' | 'shipments' | 'idle'

const tabs: { key: Tab; label: string }[] = [
  { key: 'machines', label: 'Makine verimi' },
  { key: 'orders', label: 'Sipariş takibi' },
  { key: 'quality', label: 'Kalite özeti' },
  { key: 'shipments', label: 'Günlük sevk' },
  { key: 'idle', label: 'Hareketsiz stok' },
]

const PAGE_SIZE = 20

const percent = (value: number | null) => (value === null ? '—' : `%${formatQuantity(value)}`)

/** Son 30 gün; dönem seçicilerinin başlangıcı. Ay başında ekran boş açılmasın. */
function periodStart() {
  const start = new Date(`${todayIsoDate()}T00:00:00`)
  start.setDate(start.getDate() - 30)
  const pad = (value: number) => String(value).padStart(2, '0')
  return `${start.getFullYear()}-${pad(start.getMonth() + 1)}-${pad(start.getDate())}`
}

function Period({ from, to, onFrom, onTo, children }: { from: string; to: string; onFrom: (value: string) => void; onTo: (value: string) => void; children?: React.ReactNode }) {
  return (
    <div className="toolbar">
      <label className="field"><span>Başlangıç</span><input type="date" required value={from} onChange={(event) => onFrom(event.target.value)} /></label>
      <label className="field"><span>Bitiş</span><input type="date" required value={to} onChange={(event) => onTo(event.target.value)} /></label>
      {children}
    </div>
  )
}

function MachineReport() {
  const [from, setFrom] = useState(periodStart())
  const [to, setTo] = useState(todayIsoDate())
  const [dailyHours, setDailyHours] = useState(24)
  const valid = Boolean(from && to) && from <= to && dailyHours >= 1 && dailyHours <= 24
  const { data, error, isLoading, isFetching } = useGetOrmeMachineReportQuery({ from, to, dailyHours }, { skip: !valid })

  return (
    <>
      <Period from={from} to={to} onFrom={setFrom} onTo={setTo}>
        <label className="field">
          <span>Günlük saat</span>
          <input type="number" min="1" max="24" step="1" value={dailyHours} onChange={(event) => setDailyHours(Number(event.target.value))} />
        </label>
      </Period>
      <p className="muted">
        Kullanılabilir süre: emirli gün × günlük saat, planlı duruş düşülür; duruşlar yalnızca emirli günlerde sayılır. Çalışma oranı: üretim duruşu ve arıza dışında kalan pay.
        Verim: örülen kilonun kumaş kartındaki beklenen kg/saate göre gerektirdiği süre / kullanılabilir süre; örülen kumaşlardan birinin kartında hız yoksa hesaplanmaz.
      </p>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {data && (
        <div className="table-wrap" aria-busy={isFetching}>
          <table className="table">
            <thead>
              <tr>
                <th>Makine</th>
                <th className="table__number">Top</th>
                <th className="table__number">Örülen (kg)</th>
                <th className="table__number">Emirli gün</th>
                <th className="table__number">Duruş</th>
                <th className="table__number">Arıza</th>
                <th className="table__number">Planlı</th>
                <th className="table__number">Çalışma oranı</th>
                <th className="table__number">Verim</th>
              </tr>
            </thead>
            <tbody>
              {data.map((row) => (
                <tr key={row.machineId}>
                  <td><strong>{row.machineCode}</strong><div className="muted">{row.machineName}</div></td>
                  <td className="table__number">{row.rollCount}</td>
                  <td className="table__number">{formatQuantity(row.knittedKilograms)}</td>
                  <td className="table__number">{row.loadedDays}</td>
                  <td className="table__number">{formatMinutes(row.stopMinutes)}</td>
                  <td className="table__number">{formatMinutes(row.breakdownMinutes)}</td>
                  <td className="table__number">{formatMinutes(row.plannedMinutes)}</td>
                  <td className="table__number">{percent(row.runningPercent)}</td>
                  <td className="table__number">{row.missingExpectedRate ? <span className="muted" title="Örülen kumaşlardan birinin kartında beklenen kg/saat yok">Kartta hız yok</span> : percent(row.efficiencyPercent)}</td>
                </tr>
              ))}
              {data.length === 0 && <tr><td colSpan={9} className="table__empty">Bu dönemde veri yok.</td></tr>}
            </tbody>
          </table>
        </div>
      )}
    </>
  )
}

function OrderReport() {
  const [page, setPage] = useState(1)
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [openOnly, setOpenOnly] = useState(true)
  const { data, error, isLoading, isFetching } = useGetOrmeOrderReportQuery({ page, pageSize: PAGE_SIZE, search: search || undefined, openOnly })
  const today = todayIsoDate()

  const handleSearch = (event: FormEvent) => {
    event.preventDefault()
    setPage(1)
    setSearch(searchInput.trim())
  }

  return (
    <>
      <form className="toolbar" onSubmit={handleSearch} role="search">
        <input type="search" placeholder="Sipariş, müşteri ya da kumaş" aria-label="Sipariş ara" value={searchInput} onChange={(event) => setSearchInput(event.target.value)} />
        <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
        <label className="checkbox">
          <input type="checkbox" checked={openOnly} onChange={(event) => { setPage(1); setOpenOnly(event.target.checked) }} />
          Yalnız sevki tamamlanmamış
        </label>
      </form>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {data && (
        <>
          <div className="table-wrap" aria-busy={isFetching}>
            <table className="table">
              <thead>
                <tr>
                  <th>Sipariş</th>
                  <th>Kumaş</th>
                  <th>Termin</th>
                  <th className="table__number">Sipariş</th>
                  <th className="table__number">Emir planı</th>
                  <th className="table__number">Örülen</th>
                  <th className="table__number">1. kalite</th>
                  <th className="table__number">2. kalite</th>
                  <th className="table__number">Red</th>
                  <th className="table__number">Çeki</th>
                  <th className="table__number">Sevk</th>
                  <th className="table__number">Fatura</th>
                </tr>
              </thead>
              <tbody>
                {data.items.map((row) => {
                  const late = row.dueDate !== null && row.dueDate < today && row.shippedKilograms < row.orderedKilograms
                  return (
                    <tr key={row.orderLineId}>
                      <td><Link to={`/orme/orders/${row.orderId}`}>{row.orderNo}</Link><div className="muted">{row.customerName ?? 'Stoğa üretim'}</div></td>
                      <td>{row.fabricCode} — {row.fabricName}</td>
                      <td>{row.dueDate ? formatDate(row.dueDate) : '—'}{late && <div><span className="badge badge--danger">Gecikti</span></div>}</td>
                      <td className="table__number">{formatQuantity(row.orderedKilograms)}</td>
                      <td className="table__number">{formatQuantity(row.plannedKilograms)}</td>
                      <td className="table__number">{formatQuantity(row.knittedKilograms)}</td>
                      <td className="table__number">{formatQuantity(row.firstQualityKilograms)}</td>
                      <td className="table__number">{formatQuantity(row.secondQualityKilograms)}</td>
                      <td className="table__number">{formatQuantity(row.rejectedKilograms)}</td>
                      <td className="table__number">{formatQuantity(row.packedKilograms)}</td>
                      <td className="table__number">{formatQuantity(row.shippedKilograms)}</td>
                      <td className="table__number">{formatQuantity(row.invoicedKilograms)}</td>
                    </tr>
                  )
                })}
                {data.items.length === 0 && <tr><td colSpan={12} className="table__empty">Sipariş bulunamadı.</td></tr>}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}
    </>
  )
}

function QualityTable({ title, rows, total }: { title: string; rows: OrmeQualityGroup[]; total?: OrmeQualityGroup }) {
  const share = (part: number, whole: number) => (whole === 0 ? '—' : `%${formatQuantity(Math.round((part * 1000) / whole) / 10)}`)
  const line = (row: OrmeQualityGroup) => (
    <>
      <td className="table__number">{row.rollCount}</td>
      <td className="table__number">{formatQuantity(row.kilograms)}</td>
      <td className="table__number">{share(row.firstQualityKilograms, row.kilograms)}</td>
      <td className="table__number">{share(row.secondQualityKilograms, row.kilograms)}</td>
      <td className="table__number">{share(row.rejectedKilograms, row.kilograms)}</td>
      <td className="table__number">{row.averagePointsPer100M2 === null ? '—' : formatQuantity(row.averagePointsPer100M2)}</td>
      <td className="table__number">{row.estimatedMeterRollCount}</td>
      <td className="table__number">{row.deviationRollCount}</td>
    </>
  )

  return (
    <div>
      <div className="section-title"><h2>{title}</h2></div>
      <div className="table-wrap">
        <table className="table table--compact">
          <thead>
            <tr>
              <th>{title === 'Makineye göre' ? 'Makine' : 'Kumaş'}</th>
              <th className="table__number">Top</th>
              <th className="table__number">Kilo</th>
              <th className="table__number">1. kalite</th>
              <th className="table__number">2. kalite</th>
              <th className="table__number">Red</th>
              <th className="table__number">Puan / 100 m²</th>
              <th className="table__number" title="Puanı tahmini metreyle hesaplanan top">Tahmini metre</th>
              <th className="table__number" title="En ya da gramajı kumaş kartından sapan top">Karttan sapma</th>
            </tr>
          </thead>
          <tbody>
            {rows.map((row) => (
              <tr key={row.code}>
                <td>{row.code} — {row.name}</td>
                {line(row)}
              </tr>
            ))}
            {rows.length === 0 && <tr><td colSpan={9} className="table__empty">Bu dönemde kontrol yok.</td></tr>}
            {total && rows.length > 0 && (
              <tr>
                <td><strong>Toplam</strong></td>
                {line(total)}
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  )
}

function QualityReport() {
  const [from, setFrom] = useState(periodStart())
  const [to, setTo] = useState(todayIsoDate())
  const valid = Boolean(from && to) && from <= to
  const { data, error, isLoading } = useGetOrmeQualityReportQuery({ from, to }, { skip: !valid })

  return (
    <>
      <Period from={from} to={to} onFrom={setFrom} onTo={setTo} />
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {data && (
        <>
          <QualityTable title="Kumaşa göre" rows={data.byFabric} total={data.total} />
          <QualityTable title="Makineye göre" rows={data.byMachine} total={data.total} />
          <div>
          <div className="section-title"><h2>Hata türleri</h2></div>
          <div className="table-wrap">
            <table className="table table--compact">
              <thead>
                <tr>
                  <th>Hata</th>
                  <th className="table__number">Adet</th>
                  <th className="table__number">Puan</th>
                </tr>
              </thead>
              <tbody>
                {data.defects.map((defect) => (
                  <tr key={defect.code}>
                    <td>{defect.code} — {defect.name}</td>
                    <td className="table__number">{defect.count}</td>
                    <td className="table__number">{defect.points}</td>
                  </tr>
                ))}
                {data.defects.length === 0 && <tr><td colSpan={3} className="table__empty">Bu dönemde hata kaydı yok.</td></tr>}
              </tbody>
            </table>
          </div>
          </div>
        </>
      )}
    </>
  )
}

/** Günlük sevk: gün ve müşteri bazında onaylı irsaliyeler; Boyahane'ye gönderilen çeki listeleri ayrı satırda. */
function ShipmentReport() {
  const [from, setFrom] = useState(periodStart())
  const [to, setTo] = useState(todayIsoDate())
  const valid = Boolean(from && to) && from <= to
  const { data, error, isLoading, isFetching } = useGetOrmeDailyShipmentReportQuery({ from, to }, { skip: !valid })

  return (
    <>
      <Period from={from} to={to} onFrom={setFrom} onTo={setTo} />
      <p className="muted">
        Onaylı irsaliyeler irsaliye tarihine göre sayılır. Boyahane'ye gönderilen çeki listeleri gönderim tarihinde &quot;Boyahane'ye&quot; satırında
        yer alır; o satırda belge sayısı çeki listesi sayısıdır.
      </p>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {data && (
        <div className="table-wrap" aria-busy={isFetching}>
          <table className="table table--compact">
            <thead>
              <tr>
                <th>Tarih</th>
                <th>Müşteri</th>
                <th className="table__number">Belge</th>
                <th className="table__number">Top</th>
                <th className="table__number">Kilo</th>
              </tr>
            </thead>
            <tbody>
              {data.days.map((day) => (
                <Fragment key={day.date}>
                  {day.rows.map((row, index) => (
                    <tr key={`${row.customerId ?? 'boyahane'}`}>
                      <td>{index === 0 ? formatDate(day.date) : ''}</td>
                      <td>{row.toBoyahane ? <span className="muted">{row.customerName}</span> : row.customerName}</td>
                      <td className="table__number">{row.despatchCount}</td>
                      <td className="table__number">{row.rollCount}</td>
                      <td className="table__number">{formatQuantity(row.kilograms)}</td>
                    </tr>
                  ))}
                  <tr>
                    <td />
                    <th scope="row">Gün toplamı</th>
                    <th className="table__number">{day.despatchCount}</th>
                    <th className="table__number">{day.rollCount}</th>
                    <th className="table__number">{formatQuantity(day.kilograms)}</th>
                  </tr>
                </Fragment>
              ))}
              {data.days.length === 0 && <tr><td colSpan={5} className="table__empty">Bu dönemde sevk yok.</td></tr>}
            </tbody>
            {data.days.length > 0 && (
              <tfoot>
                <tr>
                  <th colSpan={2} scope="row">Genel toplam</th>
                  <th className="table__number">{data.despatchCount}</th>
                  <th className="table__number">{data.rollCount}</th>
                  <th className="table__number">{formatQuantity(data.kilograms)}</th>
                </tr>
              </tfoot>
            )}
          </table>
        </div>
      )}
    </>
  )
}

/** Hareketsiz stok: belirtilen gündür hareketi olmayan iplik lotları ve sevk edilmemiş ham toplar. */
function IdleStockReport() {
  const [days, setDays] = useState(90)
  const valid = days >= 1 && days <= 3650
  const { data, error, isLoading, isFetching } = useGetOrmeIdleStockReportQuery({ days }, { skip: !valid })

  return (
    <>
      <div className="toolbar">
        <label className="field">
          <span>Hareketsiz gün</span>
          <input type="number" min="1" max="3650" step="1" value={days} onChange={(event) => setDays(Number(event.target.value))} />
        </label>
      </div>
      <p className="muted">
        İplik: kalanı olan, son {valid ? days : '—'} gündür üretime verilmemiş, geri alınmamış ve iade edilmemiş lotlar; hiç hareketi olmayan lot giriş
        tarihinden sayılır. Ham top: hiçbir çeki listesinde olmayan (sevk edilmemiş), kontrol ya da kesim tarihi eski toplar.
      </p>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {data && (
        <>
          <div>
            <div className="section-title"><h2>İplik lotları · {data.yarnLots.length} lot · {formatQuantity(data.yarnKilograms)} kg</h2></div>
            <div className="table-wrap" aria-busy={isFetching}>
              <table className="table table--compact">
                <thead>
                  <tr>
                    <th>Lot</th>
                    <th>İplik</th>
                    <th>Sahibi</th>
                    <th className="table__number">Kalan (kg)</th>
                    <th>Son hareket</th>
                    <th className="table__number">Gün</th>
                  </tr>
                </thead>
                <tbody>
                  {data.yarnLots.map((lot) => (
                    <tr key={lot.id}>
                      <td><code>{lot.lotNo}</code></td>
                      <td>{lot.yarnCode} — {lot.yarnName}{lot.yarnColor && <span className="muted"> ({lot.yarnColor})</span>}</td>
                      <td>{lot.customerName ?? <span className="muted">İşletme malı</span>}</td>
                      <td className="table__number">{formatQuantity(lot.remainingKilograms)}</td>
                      <td>{formatDate(lot.lastMovementOn)}</td>
                      <td className="table__number">{lot.idleDays}</td>
                    </tr>
                  ))}
                  {data.yarnLots.length === 0 && <tr><td colSpan={6} className="table__empty">Hareketsiz iplik lotu yok.</td></tr>}
                </tbody>
              </table>
            </div>
          </div>
          <div>
            <div className="section-title"><h2>Ham toplar · {data.rollCount} top · {formatQuantity(data.rollKilograms)} kg</h2></div>
            {data.rollsTruncated && <p className="muted">Liste en eski {data.rolls.length} topu gösteriyor; sayı ve kilo bütün topları kapsar.</p>}
            <div className="table-wrap" aria-busy={isFetching}>
              <table className="table table--compact">
                <thead>
                  <tr>
                    <th>Top</th>
                    <th>Emir / sipariş</th>
                    <th>Kumaş</th>
                    <th className="table__number">Kilo</th>
                    <th>Durum</th>
                    <th>Son değişiklik</th>
                    <th className="table__number">Gün</th>
                  </tr>
                </thead>
                <tbody>
                  {data.rolls.map((roll) => {
                    const status = ormeRollStatusPresentation[roll.status]
                    return (
                      <tr key={roll.id}>
                        <td><code>{roll.rollNo}</code></td>
                        <td>
                          <Link to={`/orme/knit-orders/${roll.knitOrderId}`}>{roll.knitOrderNo}</Link> · {roll.orderNo}
                          <div className="muted">{roll.customerName ?? 'Stoğa üretim'}</div>
                        </td>
                        <td>{roll.fabricCode} — {roll.fabricName}</td>
                        <td className="table__number">{formatQuantity(roll.kilograms)}</td>
                        <td><span className={`badge badge--${status.tone}`}>{status.label}</span></td>
                        <td>{formatDate(roll.lastChangeOn)}</td>
                        <td className="table__number">{roll.idleDays}</td>
                      </tr>
                    )
                  })}
                  {data.rolls.length === 0 && <tr><td colSpan={7} className="table__empty">Hareketsiz ham top yok.</td></tr>}
                </tbody>
              </table>
            </div>
          </div>
        </>
      )}
    </>
  )
}

/** Örme raporları: makine verimi, sipariş takibi, kalite özeti, günlük sevk ve hareketsiz stok. */
export function OrmeReportsPage() {
  const [tab, setTab] = useState<Tab>('machines')

  return (
    <section className="page">
      <h1>Örme Raporları</h1>
      <div className="tabs" role="tablist" aria-label="Rapor">
        {tabs.map((item) => (
          <button key={item.key} type="button" role="tab" aria-selected={tab === item.key} className={`tabs__tab${tab === item.key ? ' tabs__tab--active' : ''}`} onClick={() => setTab(item.key)}>
            {item.label}
          </button>
        ))}
      </div>
      <div className="tabs__panel" role="tabpanel">
        {tab === 'machines' && <MachineReport />}
        {tab === 'orders' && <OrderReport />}
        {tab === 'quality' && <QualityReport />}
        {tab === 'shipments' && <ShipmentReport />}
        {tab === 'idle' && <IdleStockReport />}
      </div>
    </section>
  )
}
