import { CheckCheck, Droplets, Pencil, Plus, Printer, RotateCcw, Search, XCircle } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent, KeyboardEvent } from 'react'
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
import { Barcode } from '../labels/LabelPrintPage'
import { LookupSelect } from '../masterData/LookupSelect'
import {
  ormeDocumentStatusPresentation,
  ormeMetersText,
  ormeGradeLabels,
  ormeGradeText,
  useChangeOrmePackingListMutation,
  useGetOrmePackingCandidatesQuery,
  useGetOrmePackingListQuery,
  useGetOrmePackingListsQuery,
  useSaveOrmePackingListMutation,
  useSendOrmePackingToBoyahaneMutation,
  useSetOrmePackingSaleCustomerMutation,
} from './ormeShippingApi'
import type { OrmePackingCandidate, OrmePackingList } from './ormeShippingApi'

const PAGE_SIZE = 20

/** Çeki listesi formu: aday toplar sipariş sipariş gruplanır, yalnızca bir siparişten seçilebilir. */
function PackingListDialog({ list, onClose }: { list?: OrmePackingList; onClose: () => void }) {
  const navigate = useNavigate()
  const { data: candidates, error: loadError, isLoading } = useGetOrmePackingCandidatesQuery(list?.id)
  const [save, { isLoading: saving, error }] = useSaveOrmePackingListMutation()
  const [packingDate, setPackingDate] = useState(list?.packingDate ?? todayIsoDate())
  const [notes, setNotes] = useState(list?.notes ?? '')
  const [changes, setChanges] = useState<Map<number, boolean>>(new Map())

  const [scan, setScan] = useState('')
  const [scanMessage, setScanMessage] = useState<{ ok: boolean; text: string } | null>(null)

  const isSelected = (candidate: OrmePackingCandidate) => changes.get(candidate.rollId) ?? candidate.selected
  const selected = candidates?.filter(isSelected) ?? []
  const activeOrder = selected[0]?.orderId ?? null
  const orders = [...new Set((candidates ?? []).map((candidate) => candidate.orderId))]

  // Okutulan top numarası aday toplardan bulunur ve seçilir; Enter formu kaydetmez.
  const handleScan = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key !== 'Enter') {
      return
    }
    event.preventDefault()
    const rollNo = scan.trim()
    if (!rollNo) {
      return
    }
    const candidate = candidates?.find((item) => item.rollNo.toLocaleUpperCase('tr') === rollNo.toLocaleUpperCase('tr'))
    if (!candidate) {
      setScanMessage({ ok: false, text: `${rollNo} listeye alınabilecek toplar arasında yok (kontrol edilmemiş, red, başka listede ya da siparişi onaylı değil).` })
    } else if (activeOrder !== null && activeOrder !== candidate.orderId) {
      setScanMessage({ ok: false, text: `${candidate.rollNo} başka siparişin topu (${candidate.orderNo}); bir listede tek sipariş olur.` })
    } else if (isSelected(candidate)) {
      setScanMessage({ ok: true, text: `${candidate.rollNo} zaten seçili.` })
    } else {
      setChanges(new Map(changes).set(candidate.rollId, true))
      setScanMessage({ ok: true, text: `${candidate.rollNo} seçildi · ${formatQuantity(candidate.kilograms)} kg` })
    }
    setScan('')
  }

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await save({
      id: list?.id,
      body: { packingDate, notes: notes.trim() || null, rollIds: selected.map((candidate) => candidate.rollId) },
    })
    if ('data' in result && result.data) {
      if (list) {
        onClose()
      } else {
        navigate(`/orme/packing-lists/${result.data.id}`)
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
          <span>Top okut</span>
          <input autoFocus autoComplete="off" placeholder="Barkodu okutun ya da top no yazıp Enter'a basın" value={scan}
            disabled={!candidates} onChange={(event) => setScan(event.target.value)} onKeyDown={handleScan} />
        </label>
        {scanMessage && <p role="status" className={scanMessage.ok ? 'muted' : 'field__error'}>{scanMessage.text}</p>}
        {isLoading && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={loadError} />
        {candidates && candidates.length === 0 && <p className="muted">Listeye alınacak top yok. Top önce ham kontrolde 1. ya da 2. kalite olmalıdır.</p>}
        {orders.map((orderId) => {
          const items = (candidates ?? []).filter((candidate) => candidate.orderId === orderId)
          return (
            <fieldset key={orderId} className="form__section" disabled={activeOrder !== null && activeOrder !== orderId}>
              <legend>{items[0].orderNo} · {items[0].customerName ?? 'Stoğa üretim'}</legend>
              <div className="table-wrap batch-candidates">
                <table className="table">
                  <thead>
                    <tr><th aria-label="Seç" /><th>Top</th><th>Emir</th><th>Kumaş</th><th>Kalite</th><th className="table__number">Kilo</th><th className="table__number">Metre</th></tr>
                  </thead>
                  <tbody>
                    {items.map((candidate) => (
                      <tr key={candidate.rollId}>
                        <td>
                          <input type="checkbox" aria-label={`${candidate.rollNo} seç`} checked={isSelected(candidate)}
                            onChange={(event) => setChanges(new Map(changes).set(candidate.rollId, event.target.checked))} />
                        </td>
                        <td><code>{candidate.rollNo}</code></td>
                        <td>{candidate.knitOrderNo}</td>
                        <td>{candidate.fabricCode}</td>
                        <td>{ormeGradeLabels[candidate.grade]}</td>
                        <td className="table__number">{formatQuantity(candidate.kilograms)}</td>
                        <td className="table__number">{ormeMetersText(candidate.meters, candidate.metersEstimated)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </fieldset>
          )
        })}
        <p><strong>Seçilen: {selected.length} top · {formatQuantity(selected.reduce((sum, candidate) => sum + candidate.kilograms, 0))} kg</strong></p>
        <ErrorAlert error={error} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={saving || selected.length === 0}>Kaydet</button>
        </footer>
      </form>
    </Modal>
  )
}

export function OrmePackingListsPage() {
  const canManage = usePermission(Permissions.OrmePackingManage)
  const grid = useGrid()
  const { page, setPage } = grid
  const [input, setInput] = useState('')
  const [search, setSearch] = useState('')
  const [creating, setCreating] = useState(false)
  const { data, error, isLoading, isFetching } = useGetOrmePackingListsQuery({ page, pageSize: PAGE_SIZE, search: search || undefined, ...grid.params })

  const columns: GridColumn[] = [
    { key: 'packingNo', label: 'Çeki no' },
    { key: 'orderNo', label: 'Sipariş' },
    { key: 'customerName', label: 'Müşteri' },
    { key: 'packingDate', label: 'Tarih', filter: false },
    { key: 'rollCount', label: 'Top', numeric: true },
    { key: 'totalKilograms', label: 'Kilo', numeric: true },
    { key: 'despatchNo', label: 'Gittiği yer', filter: false, sortable: false },
    { key: 'status', label: 'Durum', filter: Object.entries(ormeDocumentStatusPresentation).map(([value, { label }]) => ({ value, label })) },
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
                  const status = ormeDocumentStatusPresentation[row.status]
                  return (
                    <tr key={row.id}>
                      <td><Link to={`/orme/packing-lists/${row.id}`}><code>{row.packingNo}</code></Link></td>
                      <td>{row.orderNo}</td>
                      <td>{row.customerName ?? <span className="muted">Stoğa üretim</span>}</td>
                      <td>{formatDate(row.packingDate)}</td>
                      <td className="table__number">{row.rollCount}</td>
                      <td className="table__number">{formatQuantity(row.totalKilograms)}</td>
                      <td>{row.despatchNo ?? (row.boyahaneReceiptNo ? `Boyahane ${row.boyahaneReceiptNo}` : '—')}</td>
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

/** Stoğa üretilen malın satıldığı müşteri: irsaliyeye bu müşteriyle girer. */
function SaleCustomerDialog({ list, onClose }: { list: OrmePackingList; onClose: () => void }) {
  const [setCustomer, { isLoading, error }] = useSetOrmePackingSaleCustomerMutation()
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
        <LookupSelect resource="orme-customers" label="Müşteri" value={customerId} onChange={setCustomerId} placeholder="Satılmadı" />
        <ErrorAlert error={error} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading}>Kaydet</button>
        </footer>
      </form>
    </Modal>
  )
}

/** Boyahane'ye gönderim: müşteriler ve kumaş kartları modüller arasında ortak olmadığı için burada seçilir. */
function SendToBoyahaneDialog({ list, onClose }: { list: OrmePackingList; onClose: () => void }) {
  const [send, { isLoading, error }] = useSendOrmePackingToBoyahaneMutation()
  const [fabricId, setFabricId] = useState<number | null>(null)
  const [customerId, setCustomerId] = useState<number | null>(null)
  const [date, setDate] = useState(todayIsoDate())

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await send({ id: list.id, fabricId: fabricId ?? 0, customerId: list.isCommission ? customerId : null, date })
    if (!('error' in result)) {
      onClose()
    }
  }

  return (
    <Modal title={`Boyahane'ye gönder: ${list.packingNo}`} onClose={onClose}>
      <form className="form" onSubmit={handleSubmit}>
        <p className="muted">
          {list.items.length} top, {formatQuantity(list.totalKilograms)} kg Boyahane'de kumaş kabul belgesi olarak açılır. Tahmini metreler gönderilmez, Boyahane kilodan hesaplar.
          {list.isCommission ? ' Fason sipariş: malın sahibi olan Boyahane müşterisini seçin.' : ' Kendi malı: Boyahane\'de işletme malı olarak girer.'}
          {' '}Gönderilen liste geri alınmaz ve müşteriye irsaliye edilemez.
        </p>
        <LookupSelect resource="boyahane-fabrics" label="Boyahane kumaş kartı" required value={fabricId} onChange={setFabricId} />
        {list.isCommission && <LookupSelect resource="boyahane-customers" label="Boyahane müşterisi" required value={customerId} onChange={setCustomerId} />}
        <label className="field"><span>Tarih</span><input type="date" required value={date} onChange={(event) => setDate(event.target.value)} /></label>
        <ErrorAlert error={error} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading || fabricId === null || (list.isCommission && customerId === null)}>Gönder</button>
        </footer>
      </form>
    </Modal>
  )
}

/** Çeki listesi ayrıntısı: toplar, durum işlemleri, baskı, Boyahane'ye gönderim. */
export function OrmePackingListDetailPage() {
  const id = Number(useParams().id)
  const canManage = usePermission(Permissions.OrmePackingManage)
  const { data: list, error, isLoading } = useGetOrmePackingListQuery(id)
  const [change, changeState] = useChangeOrmePackingListMutation()
  const [dialog, setDialog] = useState<'edit' | 'sell' | 'boyahane' | null>(null)
  const status = list && ormeDocumentStatusPresentation[list.status]
  const shipped = list !== undefined && (list.despatchId !== null || list.boyahaneReceiptNo !== null)

  return (
    <section className="page">
      <p><Link to="/orme/packing-lists">← Çeki listelerine dön</Link></p>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error ?? changeState.error} />
      {list && status && (
        <>
          <header className="page__header">
            <div>
              <h1>{list.packingNo} <span className={`badge badge--${status.tone}`}>{status.label}</span></h1>
              <p className="muted">
                <Link to={`/orme/orders/${list.orderId}`}>{list.orderNo}</Link> · {list.customerName ?? 'Stoğa üretim'} · {formatDate(list.packingDate)}
              </p>
              {list.despatchId !== null && <p>İrsaliye: <Link to={`/orme/despatches/${list.despatchId}`}>{list.despatchNo}</Link></p>}
              {list.boyahaneReceiptNo && <p>Boyahane'ye gönderildi: {list.boyahaneReceiptNo}{list.boyahaneSentOn && ` · ${formatDate(list.boyahaneSentOn)}`}</p>}
              {list.notes && <p>{list.notes}</p>}
            </div>
            <div className="page__actions">
              <Link className="button" to={`/orme/packing-lists/${id}/print`}><Printer aria-hidden="true" />Listeyi yazdır</Link>
              {canManage && list.status === 'DRAFT' && <button type="button" className="button" onClick={() => setDialog('edit')}><Pencil aria-hidden="true" />Düzenle</button>}
              {canManage && list.isStock && list.status !== 'CANCELLED' && !shipped && <button type="button" className="button" onClick={() => setDialog('sell')}>Müşteri seç</button>}
              {canManage && list.status === 'DRAFT' && <button type="button" className="button button--primary" disabled={changeState.isLoading} onClick={() => change({ id, action: 'confirm' })}><CheckCheck aria-hidden="true" />Onayla</button>}
              {canManage && list.canSendToBoyahane && list.status === 'CONFIRMED' && !shipped && <button type="button" className="button" onClick={() => setDialog('boyahane')}><Droplets aria-hidden="true" />Boyahane'ye gönder</button>}
              {canManage && list.status === 'CONFIRMED' && !shipped && <button type="button" className="button" disabled={changeState.isLoading} onClick={() => change({ id, action: 'reopen' })}><RotateCcw aria-hidden="true" />Taslağa al</button>}
              {canManage && list.status === 'DRAFT' && <button type="button" className="button button--danger-ghost" disabled={changeState.isLoading} onClick={() => change({ id, action: 'cancel' })}><XCircle aria-hidden="true" />İptal et</button>}
            </div>
          </header>
          <div className="table-wrap">
            <table className="table">
              <thead>
                <tr><th>Top</th><th>Emir</th><th>Makine</th><th>Kumaş</th><th>Kalite</th><th className="table__number">Kilo</th><th className="table__number">Metre</th></tr>
              </thead>
              <tbody>
                {list.items.map((item) => (
                  <tr key={item.id}>
                    <td><code>{item.rollNo}</code></td>
                    <td>{item.knitOrderNo}</td>
                    <td>{item.machineCode}</td>
                    <td>{item.fabricCode} — {item.fabricName}{item.customerFabricCode && <div className="muted">Müşteri kodu: {item.customerFabricCode}</div>}</td>
                    <td>{ormeGradeLabels[item.grade]}</td>
                    <td className="table__number">{formatQuantity(item.kilograms)}</td>
                    <td className="table__number">{ormeMetersText(item.meters, item.metersEstimated)}</td>
                  </tr>
                ))}
              </tbody>
              <tfoot>
                <tr>
                  <th colSpan={5} scope="row">Toplam ({list.items.length} top)</th>
                  <td className="table__number">{formatQuantity(list.totalKilograms)}</td>
                  <td />
                </tr>
              </tfoot>
            </table>
          </div>
          {dialog === 'edit' && <PackingListDialog list={list} onClose={() => setDialog(null)} />}
          {dialog === 'sell' && <SaleCustomerDialog list={list} onClose={() => setDialog(null)} />}
          {dialog === 'boyahane' && <SendToBoyahaneDialog list={list} onClose={() => setDialog(null)} />}
        </>
      )}
    </section>
  )
}

/** Basılan çeki listesi: metinler müşterinin belge dilinde; müşteri kumaş kodu varsa sütun eklenir. */
function PackingSheet({ list }: { list: OrmePackingList }) {
  const t = documentTexts(list.documentLanguage)
  const withCustomerCode = list.items.some((item) => item.customerFabricCode)
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
            <th>{t('rollNo')}</th>
            <th>{t('fabric')}</th>
            {withCustomerCode && <th>{t('customerFabricCode')}</th>}
            <th>{t('quality')}</th>
            <th>{t('kilograms')}</th>
            <th>{t('meters')}</th>
          </tr>
        </thead>
        <tbody>
          {list.items.map((item) => (
            <tr key={item.id}>
              <td>{item.rollNo}</td>
              <td>{item.fabricCode} {item.fabricName}</td>
              {withCustomerCode && <td>{item.customerFabricCode ?? ''}</td>}
              <td>{ormeGradeText(t, item.grade)}</td>
              <td>{formatQuantity(item.kilograms)}</td>
              <td>{ormeMetersText(item.meters, item.metersEstimated)}</td>
            </tr>
          ))}
        </tbody>
        <tfoot>
          <tr>
            <th colSpan={withCustomerCode ? 4 : 3}>{t('total')} · {t('rolls')}: {list.items.length}</th>
            <th>{formatQuantity(list.totalKilograms)}</th>
            <th />
          </tr>
        </tfoot>
      </table>
      {list.notes && <p>{list.notes}</p>}
    </article>
  )
}

/** A4 çeki listesi. */
export function OrmePackingListPrintPage() {
  const id = Number(useParams().id)
  const { data: list, error, isLoading } = useGetOrmePackingListQuery(id)

  return (
    <section className="page label-page">
      <style>{'@page { size: A4 portrait; margin: 12mm; }'}</style>
      <div className="no-print">
        <p><Link to={`/orme/packing-lists/${id}`}>← Çeki listesine dön</Link></p>
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
