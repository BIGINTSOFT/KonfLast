import { CheckCheck, Pencil, Plus, Printer, RotateCcw, Search, XCircle } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { baseApi } from '../../api/baseApi'
import { ErrorAlert } from '../../components/FormControls'
import { ClearFiltersButton } from '../../components/grid/ClearFiltersButton'
import { GridHead } from '../../components/grid/GridHead'
import type { GridColumn } from '../../components/grid/GridHead'
import { useGrid } from '../../components/grid/useGrid'
import { Modal } from '../../components/Modal'
import { Pagination } from '../../components/Pagination'
import { usePermission } from '../../hooks/usePermission'
import type { PagedResult } from '../../types/api'
import { Permissions } from '../../types/permissions'
import { formatDate, formatQuantity, todayIsoDate } from '../../utils/format'
import { Barcode } from '../labels/LabelPrintPage'
import { LookupSelect } from '../masterData/LookupSelect'

type ReturnKind = 'CUSTOMER_RETURN' | 'SCRAP'
type ReturnStatus = 'DRAFT' | 'CONFIRMED' | 'CANCELLED'

const kindLabels: Record<ReturnKind, string> = { CUSTOMER_RETURN: 'Müşteriye iade', SCRAP: 'Hurda' }
const statusPresentation: Record<ReturnStatus, { label: string; tone: string }> = {
  DRAFT: { label: 'Taslak', tone: 'neutral' },
  CONFIRMED: { label: 'Onaylı', tone: 'ok' },
  CANCELLED: { label: 'İptal', tone: 'danger' },
}

interface ReturnRow {
  id: number
  returnNo: string
  kind: ReturnKind
  customerName: string | null
  returnDate: string
  itemCount: number
  kilograms: number
  status: ReturnStatus
}

interface GoodsReturn {
  id: number
  returnNo: string
  kind: ReturnKind
  customerId: number | null
  customerName: string | null
  customerTaxNumber: string | null
  returnDate: string
  vehiclePlate: string | null
  driverName: string | null
  deliveryAddress: string | null
  notes: string | null
  status: ReturnStatus
  kilograms: number
  meters: number
  items: { id: number; rollId: number | null; yarnLotId: number | null; itemNo: string; cardName: string; kilograms: number; meters: number | null; reason: string | null }[]
}

interface Candidate {
  rollId: number | null
  yarnLotId: number | null
  itemNo: string
  cardName: string
  ownerCustomerId: number | null
  ownerName: string | null
  stage: string
  kilograms: number
  meters: number | null
  reason: string | null
  selected: boolean
}

interface ReturnRequest {
  kind: ReturnKind
  returnDate: string
  customerId: number | null
  vehiclePlate: string | null
  driverName: string | null
  deliveryAddress: string | null
  notes: string | null
  items: { rollId: number | null; yarnLotId: number | null }[]
}

const returnApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getGoodsReturns: build.query<PagedResult<ReturnRow>, Record<string, unknown>>({
      query: (params) => ({ url: '/boyahane-goods-returns', params }),
      providesTags: ['BoyahaneBatch'],
    }),
    getGoodsReturn: build.query<GoodsReturn, number>({
      query: (id) => `/boyahane-goods-returns/${id}`,
      providesTags: ['BoyahaneBatch'],
    }),
    getGoodsReturnCandidates: build.query<Candidate[], number | undefined>({
      query: (returnId) => ({ url: '/boyahane-goods-returns/candidates', params: { returnId } }),
      providesTags: ['BoyahaneBatch'],
    }),
    saveGoodsReturn: build.mutation<GoodsReturn, { id?: number; body: ReturnRequest }>({
      query: ({ id, body }) => ({ url: id === undefined ? '/boyahane-goods-returns' : `/boyahane-goods-returns/${id}`, method: id === undefined ? 'POST' : 'PUT', body }),
      invalidatesTags: ['BoyahaneBatch'],
    }),
    changeGoodsReturn: build.mutation<GoodsReturn, { id: number; action: 'confirm' | 'reopen' | 'cancel' }>({
      query: ({ id, action }) => ({ url: `/boyahane-goods-returns/${id}/${action}`, method: 'POST' }),
      invalidatesTags: ['BoyahaneBatch'],
    }),
  }),
})

const keyOf = (candidate: Pick<Candidate, 'rollId' | 'yarnLotId'>) => (candidate.rollId !== null ? `r${candidate.rollId}` : `y${candidate.yarnLotId}`)

function GoodsReturnDialog({ entity, onClose }: { entity?: GoodsReturn; onClose: () => void }) {
  const navigate = useNavigate()
  const { data: candidates, error: loadError, isLoading } = returnApi.useGetGoodsReturnCandidatesQuery(entity?.id)
  const [save, { isLoading: saving, error }] = returnApi.useSaveGoodsReturnMutation()
  const [kind, setKind] = useState<ReturnKind>(entity?.kind ?? 'CUSTOMER_RETURN')
  const [customerId, setCustomerId] = useState<number | null>(entity?.customerId ?? null)
  const [returnDate, setReturnDate] = useState(entity?.returnDate ?? todayIsoDate())
  const [vehiclePlate, setVehiclePlate] = useState(entity?.vehiclePlate ?? '')
  const [driverName, setDriverName] = useState(entity?.driverName ?? '')
  const [notes, setNotes] = useState(entity?.notes ?? '')
  const [changes, setChanges] = useState<Map<string, boolean>>(new Map())
  const isReturn = kind === 'CUSTOMER_RETURN'
  // İadede yalnızca seçili müşterinin malı listelenir; hurdada bütün reddedilen mal.
  const shown = (candidates ?? []).filter((candidate) => !isReturn || candidate.ownerCustomerId === customerId)
  const isSelected = (candidate: Candidate) => changes.get(keyOf(candidate)) ?? candidate.selected
  const selected = shown.filter(isSelected)

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await save({
      id: entity?.id,
      body: {
        kind,
        returnDate,
        customerId: isReturn ? customerId : null,
        vehiclePlate: vehiclePlate.trim() || null,
        driverName: driverName.trim() || null,
        deliveryAddress: entity?.deliveryAddress ?? null,
        notes: notes.trim() || null,
        items: selected.map((candidate) => ({ rollId: candidate.rollId, yarnLotId: candidate.yarnLotId })),
      },
    })
    if ('data' in result && result.data) {
      if (entity) {
        onClose()
      } else {
        navigate(`/boyahane/goods-returns/${result.data.id}`)
      }
    }
  }

  return (
    <Modal title={entity ? `Belge: ${entity.returnNo}` : 'Yeni iade / hurda belgesi'} onClose={onClose} variant="drawer">
      <form className="form" onSubmit={handleSubmit}>
        <div className="form__pair">
          <label className="field">
            <span>Tür</span>
            <select value={kind} onChange={(event) => setKind(event.target.value as ReturnKind)}>
              {Object.entries(kindLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
            </select>
          </label>
          <label className="field"><span>Tarih</span><input type="date" required value={returnDate} onChange={(event) => setReturnDate(event.target.value)} /></label>
        </div>
        {isReturn && <LookupSelect resource="boyahane-customers" label="Müşteri" required value={customerId} onChange={setCustomerId} />}
        {isReturn && (
          <div className="form__pair">
            <label className="field"><span>Plaka</span><input maxLength={20} value={vehiclePlate} onChange={(event) => setVehiclePlate(event.target.value)} /></label>
            <label className="field"><span>Şoför</span><input maxLength={100} value={driverName} onChange={(event) => setDriverName(event.target.value)} /></label>
          </div>
        )}
        <label className="field"><span>{isReturn ? 'Not' : 'Hurda gerekçesi'}</span><input required={!isReturn} maxLength={1000} value={notes} onChange={(event) => setNotes(event.target.value)} /></label>
        {isLoading && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={loadError ?? error} />
        <div className="table-wrap">
          <table className="table">
            <thead><tr><th aria-label="Seç" /><th>Top / lot</th><th>Kart</th><th>Sahibi</th><th>Red yeri</th><th className="table__number">Kilo</th><th className="table__number">Metre</th><th>Gerekçe</th></tr></thead>
            <tbody>
              {shown.map((candidate) => (
                <tr key={keyOf(candidate)}>
                  <td><input type="checkbox" aria-label={`${candidate.itemNo} seç`} checked={isSelected(candidate)} onChange={(event) => setChanges(new Map(changes).set(keyOf(candidate), event.target.checked))} /></td>
                  <td><code>{candidate.itemNo}</code></td>
                  <td>{candidate.cardName}</td>
                  <td>{candidate.ownerName ?? 'İşletme malı'}</td>
                  <td>{candidate.stage}</td>
                  <td className="table__number">{formatQuantity(candidate.kilograms)}</td>
                  <td className="table__number">{candidate.meters === null ? '—' : formatQuantity(candidate.meters)}</td>
                  <td>{candidate.reason ?? '—'}</td>
                </tr>
              ))}
              {!isLoading && shown.length === 0 && <tr><td colSpan={8} className="table__empty">{isReturn && customerId === null ? 'Önce müşteri seçin.' : 'Reddedilmiş mal yok.'}</td></tr>}
            </tbody>
          </table>
        </div>
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={saving || selected.length === 0}>Kaydet ({selected.length})</button>
        </footer>
      </form>
    </Modal>
  )
}

export function GoodsReturnsPage() {
  const canManage = usePermission(Permissions.BoyahaneShipmentManage)
  const grid = useGrid()
  const { page, setPage } = grid
  const [input, setInput] = useState('')
  const [search, setSearch] = useState('')
  const [creating, setCreating] = useState(false)
  const { data, error, isLoading, isFetching } = returnApi.useGetGoodsReturnsQuery({ page, pageSize: 20, search: search || undefined, ...grid.params })

  const columns: GridColumn[] = [
    { key: 'returnNo', label: 'Belge no' },
    { key: 'kind', label: 'Tür', filter: Object.entries(kindLabels).map(([value, label]) => ({ value, label })) },
    { key: 'customerName', label: 'Müşteri' },
    { key: 'returnDate', label: 'Tarih', filter: false },
    { key: 'itemCount', label: 'Top / lot', numeric: true, filter: false },
    { key: 'kilograms', label: 'Kilo', numeric: true, filter: false },
    { key: 'status', label: 'Durum', filter: Object.entries(statusPresentation).map(([value, { label }]) => ({ value, label })) },
  ]

  const submit = (event: FormEvent) => {
    event.preventDefault()
    setPage(1)
    setSearch(input.trim())
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">İade ve Hurda</h1>
      <form className="toolbar" onSubmit={submit} role="search">
        <input type="search" placeholder="Belge no ya da müşteri" aria-label="Belge ara" value={input} onChange={(event) => setInput(event.target.value)} />
        <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
        <ClearFiltersButton grid={grid} />
        <div className="toolbar__actions">
          {canManage && <button type="button" className="button button--primary" onClick={() => setCreating(true)}><Plus aria-hidden="true" />Yeni belge</button>}
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
                {data.items.map((row) => (
                  <tr key={row.id}>
                    <td><Link to={`/boyahane/goods-returns/${row.id}`}><code>{row.returnNo}</code></Link></td>
                    <td>{kindLabels[row.kind]}</td>
                    <td>{row.customerName ?? '—'}</td>
                    <td>{formatDate(row.returnDate)}</td>
                    <td className="table__number">{row.itemCount}</td>
                    <td className="table__number">{formatQuantity(row.kilograms)}</td>
                    <td><span className={`badge badge--${statusPresentation[row.status].tone}`}>{statusPresentation[row.status].label}</span></td>
                  </tr>
                ))}
                {data.items.length === 0 && <tr><td colSpan={columns.length} className="table__empty">Belge yok.</td></tr>}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}
      {creating && <GoodsReturnDialog onClose={() => setCreating(false)} />}
    </section>
  )
}

export function GoodsReturnDetailPage() {
  const id = Number(useParams().id)
  const canManage = usePermission(Permissions.BoyahaneShipmentManage)
  const { data: entity, error, isLoading } = returnApi.useGetGoodsReturnQuery(id)
  const [change, changeState] = returnApi.useChangeGoodsReturnMutation()
  const [editing, setEditing] = useState(false)
  const status = entity && statusPresentation[entity.status]

  return (
    <section className="page">
      <p><Link to="/boyahane/goods-returns">← Belgelere dön</Link></p>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error ?? changeState.error} />
      {entity && status && (
        <>
          <header className="page__header">
            <div>
              <h1>{entity.returnNo} <span className={`badge badge--${status.tone}`}>{status.label}</span></h1>
              <p className="muted">{kindLabels[entity.kind]}{entity.customerName && ` · ${entity.customerName}`} · {formatDate(entity.returnDate)}{entity.vehiclePlate && ` · ${entity.vehiclePlate}`}</p>
              {entity.notes && <p>{entity.notes}</p>}
            </div>
            <div className="page__actions">
              <Link className="button" to={`/boyahane/goods-returns/${id}/print`}><Printer aria-hidden="true" />Yazdır</Link>
              {canManage && entity.status === 'DRAFT' && <button type="button" className="button" onClick={() => setEditing(true)}><Pencil aria-hidden="true" />Düzenle</button>}
              {canManage && entity.status === 'DRAFT' && <button type="button" className="button button--primary" disabled={changeState.isLoading} onClick={() => change({ id, action: 'confirm' })}><CheckCheck aria-hidden="true" />Onayla</button>}
              {canManage && entity.status === 'CONFIRMED' && <button type="button" className="button" disabled={changeState.isLoading} onClick={() => change({ id, action: 'reopen' })}><RotateCcw aria-hidden="true" />Taslağa al</button>}
              {canManage && entity.status === 'DRAFT' && <button type="button" className="button button--danger-ghost" disabled={changeState.isLoading} onClick={() => change({ id, action: 'cancel' })}><XCircle aria-hidden="true" />İptal et</button>}
            </div>
          </header>
          <div className="table-wrap">
            <table className="table">
              <thead><tr><th>Top / lot</th><th>Kart</th><th className="table__number">Kilo</th><th className="table__number">Metre</th><th>Gerekçe</th></tr></thead>
              <tbody>
                {entity.items.map((item) => (
                  <tr key={item.id}>
                    <td><code>{item.itemNo}</code></td>
                    <td>{item.cardName}</td>
                    <td className="table__number">{formatQuantity(item.kilograms)}</td>
                    <td className="table__number">{item.meters === null ? '—' : formatQuantity(item.meters)}</td>
                    <td>{item.reason ?? '—'}</td>
                  </tr>
                ))}
              </tbody>
              <tfoot><tr><th colSpan={2} scope="row">Toplam</th><td className="table__number">{formatQuantity(entity.kilograms)}</td><td className="table__number">{formatQuantity(entity.meters)}</td><td /></tr></tfoot>
            </table>
          </div>
          {editing && <GoodsReturnDialog entity={entity} onClose={() => setEditing(false)} />}
        </>
      )}
    </section>
  )
}

export function GoodsReturnPrintPage() {
  const id = Number(useParams().id)
  const { data: entity, error, isLoading } = returnApi.useGetGoodsReturnQuery(id)
  return (
    <section className="page label-page">
      <style>{'@page { size: A4 portrait; margin: 12mm; }'}</style>
      <div className="no-print">
        <p><Link to={`/boyahane/goods-returns/${id}`}>← Belgeye dön</Link></p>
        <header className="page__header">
          <div><h1>{entity ? kindLabels[entity.kind] : 'Belge'}</h1><p className="muted">Bu çıktı resmî e-İrsaliye değildir.</p></div>
          <button type="button" className="button button--primary" disabled={!entity} onClick={() => window.print()}><Printer aria-hidden="true" />Yazdır</button>
        </header>
        {isLoading && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={error} />
      </div>
      {entity && (
        <article className="batch-card packing-sheet">
          <header className="batch-card__header">
            <div>
              <div className="batch-card__number">{kindLabels[entity.kind]} {entity.returnNo}</div>
              {entity.customerName && <div>{entity.customerName}{entity.customerTaxNumber && ` · VKN ${entity.customerTaxNumber}`}</div>}
              <div>Tarih {formatDate(entity.returnDate)}</div>
              {entity.deliveryAddress && <div>{entity.deliveryAddress}</div>}
              {(entity.vehiclePlate || entity.driverName) && <div>{entity.vehiclePlate} {entity.driverName}</div>}
            </div>
            <Barcode text={entity.returnNo} />
          </header>
          <table className="batch-card__table">
            <thead><tr><th>Top / lot</th><th>Kart</th><th>Kilo</th><th>Metre</th><th>Gerekçe</th></tr></thead>
            <tbody>
              {entity.items.map((item) => (
                <tr key={item.id}>
                  <td>{item.itemNo}</td>
                  <td>{item.cardName}</td>
                  <td>{formatQuantity(item.kilograms)}</td>
                  <td>{item.meters === null ? '' : formatQuantity(item.meters)}</td>
                  <td>{item.reason}</td>
                </tr>
              ))}
            </tbody>
            <tfoot><tr><th colSpan={2} scope="row">Toplam</th><td>{formatQuantity(entity.kilograms)}</td><td>{formatQuantity(entity.meters)}</td><td /></tr></tfoot>
          </table>
          {entity.notes && <p>{entity.notes}</p>}
        </article>
      )}
    </section>
  )
}
