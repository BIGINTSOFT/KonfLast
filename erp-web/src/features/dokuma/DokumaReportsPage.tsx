import { Search } from 'lucide-react'
import { Fragment, useState } from 'react'
import type { FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { Pagination } from '../../components/Pagination'
import { formatDate, formatQuantity, todayIsoDate } from '../../utils/format'
import {
  useGetDokumaIdleStockQuery,
  useGetDokumaLoomReportQuery,
  useGetDokumaOrderReportQuery,
  useGetDokumaQualityReportQuery,
  useGetDokumaShipmentReportQuery,
} from './dokumaFloorApi'
import type { DokumaQualityGroup, DokumaShipmentRow } from './dokumaFloorApi'
import { formatMinutes } from './dokumaPresentation'
import { beamStatusPresentation } from './dokumaWarpApi'
import { dokumaRollStatusPresentation } from './dokumaWeavingApi'

type Tab = 'looms' | 'orders' | 'quality' | 'shipments' | 'idle'

const tabs: { key: Tab; label: string }[] = [
  { key: 'looms', label: 'Tezgâh verimi' },
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

function LoomReport() {
  const [from, setFrom] = useState(periodStart())
  const [to, setTo] = useState(todayIsoDate())
  const [dailyHours, setDailyHours] = useState(24)
  const valid = Boolean(from && to) && from <= to && dailyHours >= 1 && dailyHours <= 24
  const { data, error, isLoading, isFetching } = useGetDokumaLoomReportQuery({ from, to, dailyHours }, { skip: !valid })

  return (
    <>
      <Period from={from} to={to} onFrom={setFrom} onTo={setTo}>
        <label className="field">
          <span>Günlük saat</span>
          <input type="number" min="1" max="24" step="1" value={dailyHours} onChange={(event) => setDailyHours(Number(event.target.value))} />
        </label>
      </Period>
      <p className="muted">
        Kullanılabilir süre: leventli gün × günlük saat, planlı duruş düşülür. Çalışma oranı: üretim duruşu ve arıza dışında kalan pay.
        Verim: atılan atkı / anma devriyle atılabilecek atkı; tezgâh kartında devir yazılı değilse hesaplanmaz.
      </p>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {data && (
        <div className="table-wrap" aria-busy={isFetching}>
          <table className="table">
            <thead>
              <tr>
                <th>Tezgâh</th>
                <th className="table__number">Top</th>
                <th className="table__number">Dokunan (m)</th>
                <th className="table__number">Leventli gün</th>
                <th className="table__number">Duruş</th>
                <th className="table__number">Arıza</th>
                <th className="table__number">Planlı</th>
                <th className="table__number">Çalışma oranı</th>
                <th className="table__number">Verim</th>
              </tr>
            </thead>
            <tbody>
              {data.map((row) => (
                <tr key={row.loomId}>
                  <td><strong>{row.loomCode}</strong><div className="muted">{row.loomName}</div></td>
                  <td className="table__number">{row.rollCount}</td>
                  <td className="table__number">{formatQuantity(row.wovenMeters)}</td>
                  <td className="table__number">{row.loadedDays}</td>
                  <td className="table__number">{formatMinutes(row.stopMinutes)}</td>
                  <td className="table__number">{formatMinutes(row.breakdownMinutes)}</td>
                  <td className="table__number">{formatMinutes(row.plannedMinutes)}</td>
                  <td className="table__number">{percent(row.runningPercent)}</td>
                  <td className="table__number">{percent(row.efficiencyPercent)}</td>
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
  const { data, error, isLoading, isFetching } = useGetDokumaOrderReportQuery({ page, pageSize: PAGE_SIZE, search: search || undefined, openOnly })
  const today = todayIsoDate()

  const handleSearch = (event: FormEvent) => {
    event.preventDefault()
    setPage(1)
    setSearch(searchInput.trim())
  }

  return (
    <>
      <form className="toolbar" onSubmit={handleSearch} role="search">
        <input type="search" placeholder="Sipariş, müşteri ya da artikel" aria-label="Sipariş ara" value={searchInput} onChange={(event) => setSearchInput(event.target.value)} />
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
                  <th>Artikel</th>
                  <th>Termin</th>
                  <th className="table__number">Sipariş</th>
                  <th className="table__number">Çözgü</th>
                  <th className="table__number">Dokunan</th>
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
                  const late = row.dueDate !== null && row.dueDate < today && row.shippedMeters < row.orderedMeters
                  return (
                    <tr key={row.orderLineId}>
                      <td><Link to={`/dokuma/orders/${row.orderId}`}>{row.orderNo}</Link><div className="muted">{row.customerName ?? 'Stoğa üretim'}</div></td>
                      <td>{row.articleCode} — {row.articleName}</td>
                      <td>{row.dueDate ? formatDate(row.dueDate) : '—'}{late && <div><span className="badge badge--danger">Gecikti</span></div>}</td>
                      <td className="table__number">{formatQuantity(row.orderedMeters)}</td>
                      <td className="table__number">{formatQuantity(row.beamMeters)}</td>
                      <td className="table__number">{formatQuantity(row.wovenMeters)}</td>
                      <td className="table__number">{formatQuantity(row.firstQualityMeters)}</td>
                      <td className="table__number">{formatQuantity(row.secondQualityMeters)}</td>
                      <td className="table__number">{formatQuantity(row.rejectedMeters)}</td>
                      <td className="table__number">{formatQuantity(row.packedMeters)}</td>
                      <td className="table__number">{formatQuantity(row.shippedMeters)}</td>
                      <td className="table__number">{formatQuantity(row.invoicedMeters)}</td>
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

function QualityTable({ title, rows, total }: { title: string; rows: DokumaQualityGroup[]; total?: DokumaQualityGroup }) {
  const share = (part: number, whole: number) => (whole === 0 ? '—' : `%${formatQuantity(Math.round((part * 1000) / whole) / 10)}`)
  const line = (row: DokumaQualityGroup) => (
    <>
      <td className="table__number">{row.rollCount}</td>
      <td className="table__number">{formatQuantity(row.meters)}</td>
      <td className="table__number">{share(row.firstQualityMeters, row.meters)}</td>
      <td className="table__number">{share(row.secondQualityMeters, row.meters)}</td>
      <td className="table__number">{share(row.rejectedMeters, row.meters)}</td>
      <td className="table__number">{row.averagePointsPer100M2 === null ? '—' : formatQuantity(row.averagePointsPer100M2)}</td>
    </>
  )

  return (
    <div>
      <div className="section-title"><h2>{title}</h2></div>
      <div className="table-wrap">
        <table className="table table--compact">
          <thead>
            <tr>
              <th>{title === 'Tezgâha göre' ? 'Tezgâh' : 'Artikel'}</th>
              <th className="table__number">Top</th>
              <th className="table__number">Metre</th>
              <th className="table__number">1. kalite</th>
              <th className="table__number">2. kalite</th>
              <th className="table__number">Red</th>
              <th className="table__number">Puan / 100 m²</th>
            </tr>
          </thead>
          <tbody>
            {rows.map((row) => (
              <tr key={row.code}>
                <td>{row.code} — {row.name}</td>
                {line(row)}
              </tr>
            ))}
            {rows.length === 0 && <tr><td colSpan={7} className="table__empty">Bu dönemde kontrol yok.</td></tr>}
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
  const { data, error, isLoading } = useGetDokumaQualityReportQuery({ from, to }, { skip: !valid })

  return (
    <>
      <Period from={from} to={to} onFrom={setFrom} onTo={setTo} />
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {data && (
        <>
          <QualityTable title="Artikele göre" rows={data.byArticle} total={data.total} />
          <QualityTable title="Tezgâha göre" rows={data.byLoom} total={data.total} />
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

/** Kilo yalnızca tartılmış toplardan toplanır; eksikse işaretlenir. */
function ShipmentKilograms({ row }: { row: DokumaShipmentRow }) {
  if (row.rollCount === row.unweighedRollCount) {
    return <>—</>
  }
  return (
    <>
      {formatQuantity(row.kilograms)}
      {row.unweighedRollCount > 0 && <span className="muted" title={`${row.unweighedRollCount} top tartılmadı`}> *</span>}
    </>
  )
}

function ShipmentCells({ row }: { row: DokumaShipmentRow }) {
  return (
    <>
      <td className="table__number">{row.documentCount}</td>
      <td className="table__number">{row.rollCount}</td>
      <td className="table__number">{formatQuantity(row.meters)}</td>
      <td className="table__number"><ShipmentKilograms row={row} /></td>
    </>
  )
}

/** Günlük sevk: gün × müşteri; onaylı irsaliyeler ve Boyahane'ye gönderilen çeki listeleri. */
function ShipmentReport() {
  const [from, setFrom] = useState(periodStart())
  const [to, setTo] = useState(todayIsoDate())
  const valid = Boolean(from && to) && from <= to
  const { data, error, isLoading, isFetching } = useGetDokumaShipmentReportQuery({ from, to }, { skip: !valid })

  return (
    <>
      <Period from={from} to={to} onFrom={setFrom} onTo={setTo} />
      <p className="muted">
        Onaylı irsaliyeler sevk tarihine, Boyahane'ye gönderilen çeki listeleri gönderim tarihine göre sayılır. Belge: irsaliye
        sayısı, Boyahane satırında çeki listesi sayısı. Kilo yalnızca tartılmış toplardan toplanır; * işaretli satırda tartılmamış top vardır.
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
                <th className="table__number">Metre</th>
                <th className="table__number">Kilo</th>
              </tr>
            </thead>
            <tbody>
              {data.days.map((day) => (
                <Fragment key={day.day}>
                  {day.rows.map((row, index) => (
                    <tr key={`${day.day}-${row.customerId ?? 'boyahane'}`}>
                      <td>{index === 0 ? formatDate(day.day) : ''}</td>
                      <td>{row.isBoyahane ? <span className="badge badge--info">{row.name}</span> : row.name}</td>
                      <ShipmentCells row={row} />
                    </tr>
                  ))}
                  {day.rows.length > 1 && (
                    <tr>
                      <td />
                      <td><strong>Gün toplamı</strong></td>
                      <ShipmentCells row={day.total} />
                    </tr>
                  )}
                </Fragment>
              ))}
              {data.days.length === 0 && <tr><td colSpan={6} className="table__empty">Bu dönemde sevk yok.</td></tr>}
            </tbody>
            {data.days.length > 0 && (
              <tfoot>
                <tr>
                  <th colSpan={2} scope="row">Dönem toplamı</th>
                  <ShipmentCells row={data.total} />
                </tr>
              </tfoot>
            )}
          </table>
        </div>
      )}
    </>
  )
}

/** Hareketsiz stok: belirtilen gündür hareket görmeyen iplik lotları, ham toplar ve leventler. */
function IdleStockReport() {
  const [input, setInput] = useState('90')
  const [days, setDays] = useState(90)
  const { data, error, isLoading, isFetching } = useGetDokumaIdleStockQuery({ days })

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault()
    const value = Number(input)
    if (Number.isInteger(value) && value >= 1 && value <= 3650) {
      setDays(value)
    }
  }

  return (
    <>
      <form className="toolbar" onSubmit={handleSubmit}>
        <label className="field">
          <span>Hareketsiz gün</span>
          <input type="number" min="1" max="3650" step="1" required value={input} onChange={(event) => setInput(event.target.value)} />
        </label>
        <button type="submit" className="button"><Search aria-hidden="true" />Göster</button>
      </form>
      <p className="muted">
        İplik: kalanı olan ve son çıkışı (çözgü, atkı, iade, geri alım; hiç yoksa girişi) eşikten eski lotlar. Ham top: çeki
        listesine girmemiş ve kontrolü (kontrol edilmediyse kesimi) eşikten eski toplar. Levent: çözgüsü çekilmiş ya da tezgâha
        hazır bekleyenler.
      </p>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {data && (
        <div aria-busy={isFetching}>
          <div className="section-title"><h2>İplik lotları ({data.yarnLots.length})</h2></div>
          <div className="table-wrap">
            <table className="table table--compact">
              <thead>
                <tr>
                  <th>Lot</th>
                  <th>İplik</th>
                  <th>Malın sahibi</th>
                  <th>Giriş</th>
                  <th className="table__number">Kalan (kg)</th>
                  <th>Son hareket</th>
                  <th className="table__number">Gün</th>
                </tr>
              </thead>
              <tbody>
                {data.yarnLots.map((lot) => (
                  <tr key={lot.lotId}>
                    <td><code>{lot.lotNo}</code></td>
                    <td>{lot.yarnCode} — {lot.yarnName}</td>
                    <td>{lot.ownerName ?? <span className="muted">İşletme malı</span>}</td>
                    <td>{formatDate(lot.receiptDate)}</td>
                    <td className="table__number">{formatQuantity(lot.remainingKilograms)}</td>
                    <td>{formatDate(lot.lastMovementOn)}</td>
                    <td className="table__number">{lot.idleDays}</td>
                  </tr>
                ))}
                {data.yarnLots.length === 0 && <tr><td colSpan={7} className="table__empty">Hareketsiz iplik lotu yok.</td></tr>}
              </tbody>
              {data.yarnLots.length > 0 && (
                <tfoot>
                  <tr>
                    <th colSpan={4} scope="row">Toplam</th>
                    <td className="table__number">{formatQuantity(data.yarnLots.reduce((sum, lot) => sum + lot.remainingKilograms, 0))}</td>
                    <td colSpan={2} />
                  </tr>
                </tfoot>
              )}
            </table>
          </div>

          <div className="section-title"><h2>Ham toplar ({data.rolls.length})</h2></div>
          {data.rollsTruncated && <div className="alert alert--info">Liste uzun; en eski {data.rolls.length} top gösteriliyor.</div>}
          <div className="table-wrap">
            <table className="table table--compact">
              <thead>
                <tr>
                  <th>Top</th>
                  <th>Artikel</th>
                  <th>Sipariş</th>
                  <th>Durum</th>
                  <th className="table__number">Metre</th>
                  <th>Kesim / kontrol</th>
                  <th className="table__number">Gün</th>
                </tr>
              </thead>
              <tbody>
                {data.rolls.map((roll) => {
                  const status = dokumaRollStatusPresentation[roll.status]
                  return (
                    <tr key={roll.rollId}>
                      <td><code>{roll.rollNo}</code></td>
                      <td>{roll.articleCode} — {roll.articleName}</td>
                      <td>{roll.orderNo}<div className="muted">{roll.customerName ?? 'Stoğa üretim'}</div></td>
                      <td><span className={`badge badge--${status.tone}`}>{status.label}</span></td>
                      <td className="table__number">{formatQuantity(roll.meters)}</td>
                      <td>{formatDate(roll.since)}</td>
                      <td className="table__number">{roll.idleDays}</td>
                    </tr>
                  )
                })}
                {data.rolls.length === 0 && <tr><td colSpan={7} className="table__empty">Hareketsiz ham top yok.</td></tr>}
              </tbody>
              {data.rolls.length > 0 && (
                <tfoot>
                  <tr>
                    <th colSpan={4} scope="row">Toplam</th>
                    <td className="table__number">{formatQuantity(data.rolls.reduce((sum, roll) => sum + roll.meters, 0))}</td>
                    <td colSpan={2} />
                  </tr>
                </tfoot>
              )}
            </table>
          </div>

          <div className="section-title"><h2>Leventler ({data.beams.length})</h2></div>
          <div className="table-wrap">
            <table className="table table--compact">
              <thead>
                <tr>
                  <th>Levent</th>
                  <th>Çözgü emri</th>
                  <th>Sipariş</th>
                  <th>Artikel</th>
                  <th>Durum</th>
                  <th className="table__number">Metre</th>
                  <th>Bekleme başı</th>
                  <th className="table__number">Gün</th>
                </tr>
              </thead>
              <tbody>
                {data.beams.map((beam) => {
                  const status = beamStatusPresentation[beam.status]
                  return (
                    <tr key={beam.beamId}>
                      <td><Link to={`/dokuma/weaving/beams/${beam.beamId}`}><code>{beam.beamNo}</code></Link></td>
                      <td>{beam.warpNo}</td>
                      <td>{beam.orderNo}</td>
                      <td>{beam.articleCode}</td>
                      <td><span className={`badge badge--${status.tone}`}>{status.label}</span></td>
                      <td className="table__number">{formatQuantity(beam.meters)}</td>
                      <td>{formatDate(beam.since)}</td>
                      <td className="table__number">{beam.idleDays}</td>
                    </tr>
                  )
                })}
                {data.beams.length === 0 && <tr><td colSpan={8} className="table__empty">Bekleyen levent yok.</td></tr>}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </>
  )
}

/** Dokuma raporları: tezgâh verimi, sipariş takibi, kalite özeti, günlük sevk ve hareketsiz stok. */
export function DokumaReportsPage() {
  const [tab, setTab] = useState<Tab>('looms')

  return (
    <section className="page">
      <h1>Dokuma Raporları</h1>
      <div className="tabs" role="tablist" aria-label="Rapor">
        {tabs.map((item) => (
          <button key={item.key} type="button" role="tab" aria-selected={tab === item.key} className={`tabs__tab${tab === item.key ? ' tabs__tab--active' : ''}`} onClick={() => setTab(item.key)}>
            {item.label}
          </button>
        ))}
      </div>
      <div className="tabs__panel" role="tabpanel">
        {tab === 'looms' && <LoomReport />}
        {tab === 'orders' && <OrderReport />}
        {tab === 'quality' && <QualityReport />}
        {tab === 'shipments' && <ShipmentReport />}
        {tab === 'idle' && <IdleStockReport />}
      </div>
    </section>
  )
}
