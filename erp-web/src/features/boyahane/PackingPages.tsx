import { CheckCheck, Pencil, Plus, Printer, RotateCcw, Search, Tag, XCircle } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { ClearFiltersButton } from '../../components/grid/ClearFiltersButton'
import { GridHead } from '../../components/grid/GridHead'
import type { GridColumn } from '../../components/grid/GridHead'
import { useGrid } from '../../components/grid/useGrid'
import { Modal } from '../../components/Modal'
import { Pagination } from '../../components/Pagination'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { documentTexts } from '../../utils/documentText'
import { formatDate, formatQuantity, todayIsoDate } from '../../utils/format'
import { Barcode, LabelOptionsToolbar, QrCode } from '../labels/LabelPrintPage'
import { LookupSelect } from '../masterData/LookupSelect'
import { labelFormats, readLabelOptions, storeLabelOptions } from '../labels/labelFormats'
import type { LabelOptions } from '../labels/labelFormats'
import {
  packingStatusPresentation,
  useChangePackingListMutation,
  useGetPackingCandidatesQuery,
  useGetPackingListQuery,
  useGetPackingListsQuery,
  useSavePackingListMutation,
  useSetPackingSaleCustomerMutation,
} from './boyahanePackingApi'
import type { PackingCandidate, PackingList } from './boyahanePackingApi'

const PAGE_SIZE = 20

/** Çeki listesi formu: aday mal sipariş sipariş gruplanır, yalnızca bir siparişten seçilebilir. */
function PackingListDialog({ list, onClose }: { list?: PackingList; onClose: () => void }) {
  const navigate = useNavigate()
  const { data: candidates, error: loadError, isLoading } = useGetPackingCandidatesQuery(list?.id)
  const [save, { isLoading: saving, error }] = useSavePackingListMutation()
  const [packingDate, setPackingDate] = useState(list?.packingDate ?? todayIsoDate())
  const [notes, setNotes] = useState(list?.notes ?? '')
  const [changes, setChanges] = useState<Map<number, boolean>>(new Map())
  const [boxes, setBoxes] = useState<Map<number, string>>(new Map())

  const isSelected = (candidate: PackingCandidate) => changes.get(candidate.batchItemId) ?? candidate.selected
  const selected = candidates?.filter(isSelected) ?? []
  const activeOrder = selected[0]?.orderId ?? null
  const orders = [...new Set((candidates ?? []).map((candidate) => candidate.orderId))]
  const boxOf = (candidate: PackingCandidate) => boxes.get(candidate.batchItemId) ?? String(candidate.boxCount)
  const [scan, setScan] = useState('')
  const [scanResult, setScanResult] = useState<{ ok: boolean; text: string } | null>(null)

  /**
   * Okutulan kod top / lot etiketindeki numarayla (top no, lot no) eşlenir; eşleşme yoksa parti kartındaki parti no
   * denenir ve partinin bu siparişteki bütün malı seçilir. Seçim tek siparişten olur.
   */
  const handleScan = () => {
    const code = scan.trim().toUpperCase()
    setScan('')
    if (!code || !candidates) {
      return
    }
    const byItem = candidates.filter((candidate) => candidate.itemNo.toUpperCase() === code)
    const matches = byItem.length > 0 ? byItem : candidates.filter((candidate) => candidate.batchNo.toUpperCase() === code)
    if (matches.length === 0) {
      setScanResult({ ok: false, text: `${code} listeye alınabilecek mallar arasında yok: final kalitede uygun değil ya da başka bir listede.` })
      return
    }
    const orderId = activeOrder ?? matches[0].orderId
    const usable = matches.filter((candidate) => candidate.orderId === orderId)
    if (usable.length === 0) {
      setScanResult({ ok: false, text: `${code} başka siparişin malı (${matches[0].orderNo}); çeki listesi tek siparişten oluşur.` })
      return
    }
    const fresh = usable.filter((candidate) => !isSelected(candidate))
    const next = new Map(changes)
    usable.forEach((candidate) => next.set(candidate.batchItemId, true))
    setChanges(next)
    setScanResult(fresh.length === 0
      ? { ok: true, text: `${code} zaten seçili.` }
      : { ok: true, text: `${code} seçildi (${fresh.length} paket, ${formatQuantity(fresh.reduce((sum, candidate) => sum + candidate.kilograms, 0))} kg).` })
  }

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await save({
      id: list?.id,
      body: {
        packingDate,
        notes: notes.trim() || null,
        items: selected.map((candidate) => ({ batchItemId: candidate.batchItemId, boxCount: candidate.isYarn ? Number(boxOf(candidate)) : 1 })),
      },
    })
    if ('data' in result && result.data) {
      if (list) {
        onClose()
      } else {
        navigate(`/boyahane/packing-lists/${result.data.id}`)
      }
    }
  }

  return (
    <Modal title={list ? `Çeki listesi: ${list.packingNo}` : 'Yeni çeki listesi'} onClose={onClose} variant="drawer">
      <form className="form" onSubmit={handleSubmit}>
        <div className="form__pair">
          <label className="field"><span>Tarih</span><input type="date" required value={packingDate} onChange={(event) => setPackingDate(event.target.value)} /></label>
          <label className="field"><span>Not</span><input maxLength={1000} value={notes} onChange={(event) => setNotes(event.target.value)} /></label>
        </div>
        <label className="field">
          <span>Barkod okut</span>
          <input
            autoFocus
            autoComplete="off"
            spellCheck={false}
            placeholder="Top no, lot no ya da parti no; Enter ile seçilir"
            value={scan}
            disabled={!candidates}
            onChange={(event) => setScan(event.target.value)}
            onKeyDown={(event) => {
              // Okuyucu Enter gönderir; form kaydedilmez, okutulan mal seçilir.
              if (event.key === 'Enter') {
                event.preventDefault()
                handleScan()
              }
            }}
          />
        </label>
        {scanResult && <div className={`alert alert--${scanResult.ok ? 'ok' : 'error'}`} role="status">{scanResult.text}</div>}
        {isLoading && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={loadError} />
        {candidates && candidates.length === 0 && <p className="muted">Paketlenecek mal yok. Mal önce final kalitede uygun verilmelidir.</p>}
        {orders.map((orderId) => {
          const items = (candidates ?? []).filter((candidate) => candidate.orderId === orderId)
          return (
            <fieldset key={orderId} className="form__section" disabled={activeOrder !== null && activeOrder !== orderId}>
              <legend>{items[0].orderNo} · {items[0].customerName ?? 'Stoğa üretim'}</legend>
              <div className="table-wrap batch-candidates">
                <table className="table">
                  <thead>
                    <tr><th aria-label="Seç" /><th>Mal</th><th>Parti</th><th>Kart</th><th className="table__number">Kilo</th><th className="table__number">Metre</th><th className="table__number">Koli</th></tr>
                  </thead>
                  <tbody>
                    {items.map((candidate) => (
                      <tr key={candidate.batchItemId}>
                        <td>
                          <input type="checkbox" aria-label={`${candidate.itemNo} seç`} checked={isSelected(candidate)}
                            onChange={(event) => setChanges(new Map(changes).set(candidate.batchItemId, event.target.checked))} />
                        </td>
                        <td><code>{candidate.itemNo}</code></td>
                        <td>{candidate.batchNo}</td>
                        <td>{candidate.cardCode}</td>
                        <td className="table__number">{formatQuantity(candidate.kilograms)}</td>
                        <td className="table__number">{candidate.meters !== null ? formatQuantity(candidate.meters) : '—'}</td>
                        <td className="table__number">
                          {candidate.isYarn
                            ? <input aria-label={`${candidate.itemNo} koli`} className="packing-box" type="number" min="1" step="1" required value={boxOf(candidate)} onChange={(event) => setBoxes(new Map(boxes).set(candidate.batchItemId, event.target.value))} />
                            : '1'}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </fieldset>
          )
        })}
        <p><strong>Seçilen: {selected.length} paket · {formatQuantity(selected.reduce((sum, candidate) => sum + candidate.kilograms, 0))} kg</strong></p>
        <ErrorAlert error={error} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={saving || selected.length === 0}>Kaydet</button>
        </footer>
      </form>
    </Modal>
  )
}

/** Çeki listeleri, en yeniden eskiye. */
export function PackingListsPage() {
  const canManage = usePermission(Permissions.BoyahanePackingManage)
  const grid = useGrid()
  const { page, setPage } = grid
  const [input, setInput] = useState('')
  const [search, setSearch] = useState('')
  const [creating, setCreating] = useState(false)
  const { data, error, isLoading, isFetching } = useGetPackingListsQuery({ page, pageSize: PAGE_SIZE, search: search || undefined, ...grid.params })

  const columns: GridColumn[] = [
    { key: 'packingNo', label: 'Çeki no' },
    { key: 'orderNo', label: 'Sipariş' },
    { key: 'customerName', label: 'Müşteri' },
    { key: 'packingDate', label: 'Tarih', filter: false },
    { key: 'packageCount', label: 'Paket', numeric: true },
    { key: 'totalKilograms', label: 'Kilo', numeric: true },
    { key: 'totalMeters', label: 'Metre', numeric: true },
    { key: 'status', label: 'Durum', filter: Object.entries(packingStatusPresentation).map(([value, { label }]) => ({ value, label })) },
  ]

  const handleSearch = (event: FormEvent) => {
    event.preventDefault()
    setPage(1)
    setSearch(input.trim())
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">Çeki Listeleri</h1>
      <form className="toolbar" onSubmit={handleSearch} role="search">
        <input type="search" placeholder="Çeki no, sipariş, müşteri sipariş no ya da müşteri" aria-label="Çeki listesi ara" value={input} onChange={(event) => setInput(event.target.value)} />
        <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
        <ClearFiltersButton grid={grid} />
        <div className="toolbar__actions">
          {canManage && <button type="button" className="button button--primary" onClick={() => setCreating(true)}><Plus aria-hidden="true" />Yeni çeki listesi</button>}
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
                {data.items.map((row) => {
                  const status = packingStatusPresentation[row.status]
                  return (
                    <tr key={row.id}>
                      <td><Link to={`/boyahane/packing-lists/${row.id}`}><code>{row.packingNo}</code></Link></td>
                      <td>{row.orderNo}</td>
                      <td>{row.customerName ?? <span className="muted">Stoğa üretim</span>}</td>
                      <td>{formatDate(row.packingDate)}</td>
                      <td className="table__number">{row.packageCount}</td>
                      <td className="table__number">{formatQuantity(row.totalKilograms)}</td>
                      <td className="table__number">{formatQuantity(row.totalMeters)}</td>
                      <td><span className={`badge badge--${status.tone}`}>{status.label}</span></td>
                    </tr>
                  )
                })}
                {data.items.length === 0 && <tr><td colSpan={columns.length} className="table__empty">Çeki listesi bulunamadı.</td></tr>}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}
      {creating && <PackingListDialog onClose={() => setCreating(false)} />}
    </section>
  )
}

/** Çeki listesi ayrıntısı: paketler, durum işlemleri, baskı ve etiket. */
/** Stoğa üretilen malın satıldığı müşteri: irsaliyeye bu müşteriyle girer. */
function SaleCustomerDialog({ list, onClose }: { list: PackingList; onClose: () => void }) {
  const [setCustomer, { isLoading, error }] = useSetPackingSaleCustomerMutation()
  const [customerId, setCustomerId] = useState<number | null>(list.saleCustomerId)

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    if (!('error' in await setCustomer({ id: list.id, customerId }))) {
      onClose()
    }
  }

  return (
    <Modal title="Satılan müşteri" onClose={onClose}>
      <form className="form" onSubmit={handleSubmit}>
        <p className="muted">Bu mal stoğa üretildi. Seçilen müşteri irsaliye ve faturada geçer.</p>
        <LookupSelect resource="boyahane-customers" label="Müşteri" value={customerId} onChange={setCustomerId} placeholder="Satılmadı" />
        <ErrorAlert error={error} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading}>Kaydet</button>
        </footer>
      </form>
    </Modal>
  )
}

export function PackingListDetailPage() {
  const id = Number(useParams().id)
  const canManage = usePermission(Permissions.BoyahanePackingManage)
  const { data: list, error, isLoading } = useGetPackingListQuery(id)
  const [change, changeState] = useChangePackingListMutation()
  const [editing, setEditing] = useState(false)
  const [selling, setSelling] = useState(false)
  const status = list && packingStatusPresentation[list.status]

  return (
    <section className="page">
      <p><Link to="/boyahane/packing-lists">← Çeki listelerine dön</Link></p>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error ?? changeState.error} />
      {list && status && (
        <>
          <header className="page__header">
            <div>
              <h1>{list.packingNo} <span className={`badge badge--${status.tone}`}>{status.label}</span></h1>
              <p className="muted">
                <Link to={`/boyahane/orders/${list.orderId}`}>{list.orderNo}</Link>
                {list.customerOrderNo && ` (${list.customerOrderNo})`} · {list.customerName ?? 'Stoğa üretim'} · {formatDate(list.packingDate)}
              </p>
              {list.notes && <p>{list.notes}</p>}
            </div>
            <div className="page__actions">
              <Link className="button" to={`/boyahane/packing-lists/${id}/print`}><Printer aria-hidden="true" />Listeyi yazdır</Link>
              <Link className="button" to={`/boyahane/packing-lists/${id}/labels`}><Tag aria-hidden="true" />Etiketler</Link>
              {canManage && list.status === 'DRAFT' && <button type="button" className="button" onClick={() => setEditing(true)}><Pencil aria-hidden="true" />Düzenle</button>}
              {canManage && list.isStock && list.status !== 'CANCELLED' && !list.isDespatched && <button type="button" className="button" onClick={() => setSelling(true)}>Müşteri seç</button>}
              {canManage && list.status === 'DRAFT' && <button type="button" className="button button--primary" disabled={changeState.isLoading} onClick={() => change({ id, action: 'confirm' })}><CheckCheck aria-hidden="true" />Onayla</button>}
              {canManage && list.status === 'CONFIRMED' && <button type="button" className="button" disabled={changeState.isLoading} onClick={() => change({ id, action: 'reopen' })}><RotateCcw aria-hidden="true" />Taslağa al</button>}
              {canManage && list.status === 'DRAFT' && <button type="button" className="button button--danger-ghost" disabled={changeState.isLoading} onClick={() => change({ id, action: 'cancel' })}><XCircle aria-hidden="true" />İptal et</button>}
            </div>
          </header>
          <PackagesTable list={list} />
          {editing && <PackingListDialog list={list} onClose={() => setEditing(false)} />}
          {selling && <SaleCustomerDialog list={list} onClose={() => setSelling(false)} />}
        </>
      )}
    </section>
  )
}

function PackagesTable({ list }: { list: PackingList }) {
  return (
    <div className="table-wrap">
      <table className="table">
        <thead>
          <tr><th>Paket no</th><th>Top / lot</th><th>Parti</th><th>Kart</th><th>Renk</th><th className="table__number">Koli</th><th className="table__number">Kilo</th><th className="table__number">Metre</th></tr>
        </thead>
        <tbody>
          {list.packages.map((item) => (
            <tr key={item.id}>
              <td><code>{item.packageNo}</code></td>
              <td>{item.itemNo}</td>
              <td>{item.batchNo}</td>
              <td>{item.cardCode}</td>
              <td>{item.colorName ?? '—'}{item.colorCode && ` (${item.colorCode})`}</td>
              <td className="table__number">{item.boxCount}</td>
              <td className="table__number">{formatQuantity(item.kilograms)}</td>
              <td className="table__number">{item.meters !== null ? formatQuantity(item.meters) : '—'}</td>
            </tr>
          ))}
        </tbody>
        <tfoot>
          <tr>
            <th colSpan={5} scope="row">Toplam ({list.packages.length} paket)</th>
            <td className="table__number">{list.totalBoxes}</td>
            <td className="table__number">{formatQuantity(list.totalKilograms)}</td>
            <td className="table__number">{formatQuantity(list.totalMeters)}</td>
          </tr>
        </tfoot>
      </table>
    </div>
  )
}

/** A4 çeki listesi. */
export function PackingListPrintPage() {
  const id = Number(useParams().id)
  const { data: list, error, isLoading } = useGetPackingListQuery(id)

  return (
    <section className="page label-page">
      <style>{'@page { size: A4 portrait; margin: 12mm; }'}</style>
      <div className="no-print">
        <p><Link to={`/boyahane/packing-lists/${id}`}>← Çeki listesine dön</Link></p>
        <header className="page__header">
          <div><h1>Çeki listesi</h1></div>
          <button type="button" className="button button--primary" disabled={!list} onClick={() => window.print()}><Printer aria-hidden="true" />Yazdır</button>
        </header>
        {isLoading && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={error} />
      </div>
      {list && <PackingSheet list={list} />}
    </section>
  )
}

/** Basılan çeki listesi; sabit metinler müşterinin belge dilindedir. */
function PackingSheet({ list }: { list: PackingList }) {
  const t = documentTexts(list.documentLanguage)
  // Müşteri kodu sütunları yalnızca en az bir pakette değer varsa basılır.
  const hasCustomerItemNo = list.packages.some((item) => item.customerItemNo)
  const hasCustomerFabricCode = list.packages.some((item) => item.customerFabricCode)
  const hasColorCode = list.packages.some((item) => item.colorCode)
  const leading = 4 + (hasCustomerItemNo ? 1 : 0) + (hasCustomerFabricCode ? 1 : 0) + (hasColorCode ? 1 : 0)

  return (
    <article lang={t.lang} className="batch-card packing-sheet">
      <header className="batch-card__header">
        <div>
          <div className="batch-card__number">{t('packingList')} {list.packingNo}</div>
          <div>{list.customerName ?? t('stockProduction')}</div>
          <div>{t('orderNo')} {list.orderNo}{list.customerOrderNo && ` · ${t('customerOrderNo')} ${list.customerOrderNo}`}</div>
          <div>{t('date')} {formatDate(list.packingDate)}</div>
        </div>
        <Barcode text={list.packingNo} />
      </header>
      <table className="batch-card__table">
        <thead>
          <tr>
            <th>{t('packageNo')}</th>
            <th>{t('rollOrLot')}</th>
            {hasCustomerItemNo && <th>{t('customerItemNo')}</th>}
            <th>{list.packages.some((item) => item.isYarn) ? t('yarn') : t('fabric')}</th>
            {hasCustomerFabricCode && <th>{t('customerFabricCode')}</th>}
            <th>{t('color')}</th>
            {hasColorCode && <th>{t('colorCode')}</th>}
            <th>{t('carton')}</th>
            <th>{t('kilograms')}</th>
            <th>{t('meters')}</th>
          </tr>
        </thead>
        <tbody>
          {list.packages.map((item) => (
            <tr key={item.id}>
              <td>{item.packageNo}</td>
              <td>{item.itemNo}</td>
              {hasCustomerItemNo && <td>{item.customerItemNo ?? '—'}</td>}
              <td>{item.cardCode}</td>
              {hasCustomerFabricCode && <td>{item.customerFabricCode ?? '—'}</td>}
              <td>{item.colorName ?? '—'}</td>
              {hasColorCode && <td>{item.colorCode ?? '—'}</td>}
              <td>{item.boxCount}</td>
              <td>{formatQuantity(item.kilograms)}</td>
              <td>{item.meters !== null ? formatQuantity(item.meters) : '—'}</td>
            </tr>
          ))}
        </tbody>
        <tfoot>
          <tr>
            <th colSpan={leading}>{t('total')} · {t('packages')} {list.packages.length}</th>
            <th>{list.totalBoxes}</th>
            <th>{formatQuantity(list.totalKilograms)}</th>
            <th>{formatQuantity(list.totalMeters)}</th>
          </tr>
        </tfoot>
      </table>
      {list.notes && <p>{t('notes')}: {list.notes}</p>}
    </article>
  )
}

/** Paket etiketleri: barkod ve karekod paket numarasını taşır. */
export function PackingLabelPage() {
  const id = Number(useParams().id)
  const { data: list, error, isLoading } = useGetPackingListQuery(id)
  const [options, setOptions] = useState<LabelOptions>(readLabelOptions)
  const change = (patch: Partial<LabelOptions>) => {
    const next = { ...options, ...patch }
    setOptions(next)
    storeLabelOptions(next)
  }
  const format = labelFormats[options.format]
  const packages = list?.packages ?? []
  const t = documentTexts(list?.documentLanguage)

  return (
    <section className="page label-page">
      <style>{`@page { size: ${format.page}; margin: ${format.pageMargin}; }`}</style>
      <div className="no-print">
        <p><Link to={`/boyahane/packing-lists/${id}`}>← Çeki listesine dön</Link></p>
        <header className="page__header">
          <div>
            <h1>Paket etiketleri</h1>
            <p className="muted">Yazdırma penceresinde hedef yazıcıyı seçin; "kenar boşluğu yok" ve %100 ölçek kullanın.</p>
          </div>
          <button type="button" className="button button--primary" disabled={packages.length === 0} onClick={() => window.print()}><Printer aria-hidden="true" />Yazdır ({packages.length})</button>
        </header>
        <LabelOptionsToolbar options={options} onChange={change} />
        {isLoading && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={error} />
      </div>
      <div
        className={`label-sheet label-sheet--${format.layout}`}
        style={{ '--label-width': format.width, '--label-height': format.height, '--label-columns': format.columns } as React.CSSProperties}
      >
        {list && packages.map((item) => (
          <article key={item.id} className="label">
            <header className="label__header">
              <span>{list.customerName ?? t('stockProduction')}</span>
              <span>{list.customerOrderNo ? `${t('customerOrderNo')} ${list.customerOrderNo}` : list.orderNo}</span>
            </header>
            <div className="label__body">
              <div className="label__main">
                <div className="label__number">{item.packageNo}</div>
                <div className="label__variant">
                  {item.customerFabricCode ? `${item.customerFabricCode} (${item.cardCode})` : item.cardCode} · {item.colorName ?? '—'}{item.colorCode && ` ${item.colorCode}`}
                </div>
                <div className="label__quantity">
                  {item.meters !== null && `${formatQuantity(item.meters)} m · `}{formatQuantity(item.kilograms)} kg
                  {item.isYarn && ` · ${item.boxCount} ${t('carton')}`}
                </div>
              </div>
              {options.showQr && <QrCode text={item.packageNo} />}
            </div>
            {options.showBarcode && <Barcode text={item.packageNo} />}
            <footer className="label__footer">
              {item.itemNo}{item.customerItemNo && ` / ${item.customerItemNo}`} · {t('batch')} {item.batchNo}{list.customerOrderNo && ` · ${list.orderNo}`}
            </footer>
          </article>
        ))}
      </div>
    </section>
  )
}
