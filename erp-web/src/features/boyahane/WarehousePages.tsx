import { MoveRight, Printer, Search } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link } from 'react-router-dom'
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
import { formatDate, formatDateTime, formatQuantity, formatRecipeAmount } from '../../utils/format'
import { Barcode, QrCode } from '../labels/LabelPrintPage'
import { labelFormats, readLabelOptions, storeLabelOptions } from '../labels/labelFormats'
import type { LabelOptions } from '../labels/labelFormats'
import { LookupSelect } from '../masterData/LookupSelect'
import { useGetLookupQuery } from '../masterData/masterDataApi'

type GoodsType = 'FABRIC_ROLL' | 'YARN_LOT' | 'CHEMICAL_LOT' | 'PACKAGE'

const goodsTypeLabels: Record<GoodsType, string> = {
  FABRIC_ROLL: 'Ham kumaş topu',
  YARN_LOT: 'İplik lotu',
  CHEMICAL_LOT: 'Boya / kimyasal lotu',
  PACKAGE: 'Mamul paketi',
}

interface GoodsRow {
  id: number
  goodsNo: string
  cardCode: string
  cardName: string
  colorName: string | null
  ownerName: string | null
  orderNo: string | null
  kilograms: number
  meters: number | null
  condition: string | null
  expiryDate: string | null
  warehouseName: string | null
  locationCode: string | null
}

interface MovementRow {
  id: number
  goodsType: GoodsType
  goodsNo: string
  type: 'ADDRESS_CHANGE' | 'TRANSFER'
  fromWarehouse: string | null
  fromLocation: string | null
  toWarehouse: string
  toLocation: string
  note: string | null
  createdAt: string
  createdBy: string | null
}

const warehouseApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getBoyahaneWarehouseGoods: build.query<PagedResult<GoodsRow>, Record<string, unknown>>({
      query: (params) => ({ url: '/boyahane-warehousing/goods', params }),
      providesTags: ['BoyahaneBatch'],
    }),
    getBoyahaneLocationMovements: build.query<PagedResult<MovementRow>, Record<string, unknown>>({
      query: (params) => ({ url: '/boyahane-warehousing/movements', params }),
      providesTags: ['BoyahaneBatch'],
    }),
    getBoyahaneLocationOptions: build.query<{ id: number; code: string; description: string | null; isActive: boolean }[], number>({
      query: (warehouseId) => ({ url: '/boyahane-warehousing/locations', params: { warehouseId } }),
      providesTags: ['MasterData'],
    }),
    moveBoyahaneGoods: build.mutation<number, { type: GoodsType; ids: number[]; toLocationId: number; note: string | null }>({
      query: (body) => ({ url: '/boyahane-warehousing/move', method: 'POST', body }),
      invalidatesTags: ['BoyahaneBatch'],
    }),
  }),
})

const PAGE_SIZE = 50

function MoveDialog({ type, ids, onClose }: { type: GoodsType; ids: number[]; onClose: () => void }) {
  const [move, { isLoading, error }] = warehouseApi.useMoveBoyahaneGoodsMutation()
  const [toLocationId, setToLocationId] = useState<number | null>(null)
  const [note, setNote] = useState('')

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await move({ type, ids, toLocationId: toLocationId ?? 0, note: note.trim() || null })
    if (!('error' in result)) {
      onClose()
    }
  }

  return (
    <Modal title={`Taşı: ${ids.length} ${goodsTypeLabels[type].toLocaleLowerCase('tr-TR')}`} onClose={onClose}>
      <form className="form" onSubmit={handleSubmit}>
        <LookupSelect resource="boyahane-warehouse-locations" label="Hedef adres" required value={toLocationId} onChange={setToLocationId} />
        <label className="field"><span>Not</span><input maxLength={200} value={note} onChange={(event) => setNote(event.target.value)} /></label>
        <p className="muted">Hedef başka depodaysa transfer, aynı depodaysa adres değişimi olarak kaydedilir.</p>
        <ErrorAlert error={error} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading}>Taşı</button>
        </footer>
      </form>
    </Modal>
  )
}

/** Adres stoğu: depoda duran mal. Makineye giren parti ve sevk edilen paket burada görünmez. */
export function WarehouseGoodsPage() {
  const canManage = usePermission(Permissions.BoyahaneChemicalStockManage)
  const grid = useGrid()
  const { page, setPage } = grid
  const warehouses = useGetLookupQuery({ resource: 'boyahane-warehouses', take: 100 })
  const [type, setType] = useState<GoodsType>('FABRIC_ROLL')
  const [warehouseId, setWarehouseId] = useState<number | null>(null)
  const [locationId, setLocationId] = useState<number | null>(null)
  const [input, setInput] = useState('')
  const [search, setSearch] = useState('')
  const [selected, setSelected] = useState<Set<number>>(new Set())
  const [moving, setMoving] = useState(false)
  const locations = warehouseApi.useGetBoyahaneLocationOptionsQuery(warehouseId ?? 0, { skip: warehouseId === null })
  const { data, error, isLoading, isFetching } = warehouseApi.useGetBoyahaneWarehouseGoodsQuery({
    type,
    warehouseId: warehouseId ?? undefined,
    locationId: locationId ?? undefined,
    search: search || undefined,
    page,
    pageSize: PAGE_SIZE,
    ...grid.params,
  })

  const columns: GridColumn[] = [
    ...(canManage ? [{ label: '', ariaLabel: 'Seç', filter: false as const }] : []),
    { key: 'goodsNo', label: 'Numara' },
    { key: 'cardName', label: 'Kart' },
    { key: 'ownerName', label: type === 'CHEMICAL_LOT' ? 'Tedarikçi' : 'Sahibi' },
    { key: 'orderNo', label: 'Sipariş' },
    { key: 'kilograms', label: 'Kilo', numeric: true, filter: false },
    { key: 'meters', label: 'Metre', numeric: true, filter: false },
    { key: 'condition', label: 'Durum', filter: false },
    { key: 'expiryDate', label: 'Son kullanma', filter: false },
    { key: 'warehouseName', label: 'Depo' },
    { key: 'locationCode', label: 'Adres' },
  ]

  const reset = () => {
    setPage(1)
    setSelected(new Set())
  }

  const toggle = (id: number, checked: boolean) => {
    const next = new Set(selected)
    if (checked) {
      next.add(id)
    } else {
      next.delete(id)
    }
    setSelected(next)
  }

  const submit = (event: FormEvent) => {
    event.preventDefault()
    reset()
    setSearch(input.trim())
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">Adres Stoğu</h1>
      <form className="toolbar" onSubmit={submit} role="search">
        <select aria-label="Mal türü" value={type} onChange={(event) => { reset(); setType(event.target.value as GoodsType) }}>
          {Object.entries(goodsTypeLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
        </select>
        <select aria-label="Depo" value={warehouseId ?? ''} onChange={(event) => { reset(); setLocationId(null); setWarehouseId(event.target.value ? Number(event.target.value) : null) }}>
          <option value="">Tüm depolar</option>
          {warehouses.data?.map((item) => <option key={item.id} value={item.id}>{item.code} — {item.name}</option>)}
        </select>
        <select aria-label="Adres" disabled={warehouseId === null} value={locationId ?? ''} onChange={(event) => { reset(); setLocationId(event.target.value ? Number(event.target.value) : null) }}>
          <option value="">Tüm adresler</option>
          {locations.data?.map((item) => <option key={item.id} value={item.id}>{item.code}</option>)}
        </select>
        <input type="search" placeholder="Numara, kart ya da sahibi" aria-label="Mal ara" value={input} onChange={(event) => setInput(event.target.value)} />
        <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
        <ClearFiltersButton grid={grid} />
        <div className="toolbar__actions">
          {canManage && <button type="button" className="button" disabled={!data?.items.length} onClick={() => setSelected(new Set(data?.items.map((row) => row.id)))}>Sayfayı seç</button>}
          {canManage && <button type="button" className="button button--primary" disabled={selected.size === 0} onClick={() => setMoving(true)}><MoveRight aria-hidden="true" />Taşı ({selected.size})</button>}
        </div>
      </form>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error ?? warehouses.error} />
      {data && (
        <>
          <div className="table-wrap" aria-busy={isFetching}>
            <table className="table">
              <GridHead columns={columns} grid={grid} />
              <tbody>
                {data.items.map((row) => (
                  <tr key={row.id}>
                    {canManage && <td><input type="checkbox" aria-label={`${row.goodsNo} seç`} checked={selected.has(row.id)} onChange={(event) => toggle(row.id, event.target.checked)} /></td>}
                    <td><code>{row.goodsNo}</code></td>
                    <td>{row.cardName}{row.colorName && ` · ${row.colorName}`}</td>
                    <td>{row.ownerName ?? '—'}</td>
                    <td>{row.orderNo ?? '—'}</td>
                    <td className="table__number">{formatQuantity(row.kilograms)}</td>
                    <td className="table__number">{row.meters === null ? '—' : formatQuantity(row.meters)}</td>
                    <td>{row.condition ?? '—'}</td>
                    <td>{row.expiryDate ? formatDate(row.expiryDate) : '—'}</td>
                    <td>{row.warehouseName ?? '—'}</td>
                    <td>{row.locationCode ? <code>{row.locationCode}</code> : '—'}</td>
                  </tr>
                ))}
                {data.items.length === 0 && <tr><td colSpan={columns.length} className="table__empty">Bu seçimde depoda mal yok.</td></tr>}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={(next) => { setSelected(new Set()); setPage(next) }} />
        </>
      )}
      {moving && <MoveDialog type={type} ids={[...selected]} onClose={() => { setMoving(false); setSelected(new Set()) }} />}
    </section>
  )
}

export function LocationMovementsPage() {
  const grid = useGrid()
  const { page, setPage } = grid
  const [input, setInput] = useState('')
  const [search, setSearch] = useState('')
  const { data, error, isLoading, isFetching } = warehouseApi.useGetBoyahaneLocationMovementsQuery({ page, pageSize: PAGE_SIZE, search: search || undefined, ...grid.params })

  const columns: GridColumn[] = [
    { key: 'createdAt', label: 'Tarih', filter: false },
    { key: 'goodsType', label: 'Mal', filter: Object.entries(goodsTypeLabels).map(([value, label]) => ({ value, label })) },
    { key: 'goodsNo', label: 'Numara' },
    { key: 'type', label: 'Hareket', filter: [{ value: 'ADDRESS_CHANGE', label: 'Adres değişimi' }, { value: 'TRANSFER', label: 'Transfer' }] },
    { key: 'fromLocation', label: 'Nereden', filter: false },
    { key: 'toLocation', label: 'Nereye', filter: false },
    { key: 'note', label: 'Not', filter: false },
    { key: 'createdBy', label: 'Kullanıcı' },
  ]

  const submit = (event: FormEvent) => {
    event.preventDefault()
    setPage(1)
    setSearch(input.trim())
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">Adres Hareketleri</h1>
      <form className="toolbar" onSubmit={submit} role="search">
        <input type="search" placeholder="Top, lot ya da paket no" aria-label="Hareket ara" value={input} onChange={(event) => setInput(event.target.value)} />
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
                    <td>{formatDateTime(row.createdAt)}</td>
                    <td>{goodsTypeLabels[row.goodsType]}</td>
                    <td><code>{row.goodsNo}</code></td>
                    <td>{row.type === 'TRANSFER' ? <span className="badge badge--info">Transfer</span> : 'Adres değişimi'}</td>
                    <td>{row.fromLocation ? `${row.fromWarehouse} / ${row.fromLocation}` : '—'}</td>
                    <td>{row.toWarehouse} / {row.toLocation}</td>
                    <td>{row.note ?? '—'}</td>
                    <td>{row.createdBy ?? '—'}</td>
                  </tr>
                ))}
                {data.items.length === 0 && <tr><td colSpan={columns.length} className="table__empty">Hareket yok.</td></tr>}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}
    </section>
  )
}

/** Boyahane adres etiketi. Barkod adresin kimliğini taşır: aynı kod başka depoda da olabilir. */
function locationBarcode(id: number) {
  return `BADR${String(id).padStart(8, '0')}`
}

export function BoyahaneLocationLabelPage() {
  const warehouses = useGetLookupQuery({ resource: 'boyahane-warehouses', take: 100 })
  const [warehouseId, setWarehouseId] = useState<number | null>(null)
  const [zone, setZone] = useState('')
  const [options, setOptions] = useState<LabelOptions>(readLabelOptions)
  const locations = warehouseApi.useGetBoyahaneLocationOptionsQuery(warehouseId ?? 0, { skip: warehouseId === null })

  const change = (patch: Partial<LabelOptions>) => {
    const next = { ...options, ...patch }
    setOptions(next)
    storeLabelOptions(next)
  }

  const warehouse = warehouses.data?.find((item) => item.id === warehouseId)
  const prefix = zone.trim().toLocaleUpperCase('tr-TR')
  const labels = (locations.data ?? []).filter((location) => location.isActive && location.code.startsWith(prefix))
  const format = labelFormats[options.format]

  return (
    <section className="page label-page">
      <style>{`@page { size: ${format.page}; margin: ${format.pageMargin}; }`}</style>
      <div className="no-print">
        <header className="page__header">
          <div>
            <h1>Adres etiketleri</h1>
            <p className="muted">Rafa yapıştırılan etiket. Yazdırma penceresinde "kenar boşluğu yok" ve %100 ölçek kullanın. Adres açmak için <Link to="/boyahane-warehouse-locations">Depo Adresleri</Link>.</p>
          </div>
          <div className="page__actions">
            <button type="button" className="button button--primary" disabled={labels.length === 0} onClick={() => window.print()}><Printer aria-hidden="true" />Yazdır ({labels.length})</button>
          </div>
        </header>
        <div className="toolbar toolbar--filters">
          <label>
            Depo
            <select value={warehouseId ?? ''} onChange={(event) => setWarehouseId(event.target.value ? Number(event.target.value) : null)}>
              <option value="">Seçin…</option>
              {warehouses.data?.map((item) => <option key={item.id} value={item.id}>{item.code} — {item.name}</option>)}
            </select>
          </label>
          <label>
            Adres kodu başlangıcı
            <input type="text" placeholder="Örn. A-01" value={zone} onChange={(event) => setZone(event.target.value)} />
          </label>
          <label>
            Etiket
            <select value={options.format} onChange={(event) => change({ format: event.target.value as LabelOptions['format'] })}>
              {Object.entries(labelFormats).map(([key, item]) => <option key={key} value={key}>{item.title}</option>)}
            </select>
          </label>
          <label className="checkbox"><input type="checkbox" checked={options.showBarcode} onChange={(event) => change({ showBarcode: event.target.checked })} />Barkod</label>
          <label className="checkbox"><input type="checkbox" checked={options.showQr} onChange={(event) => change({ showQr: event.target.checked })} />Karekod</label>
        </div>
        {locations.isFetching && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={warehouses.error ?? locations.error} />
        {warehouseId === null && <p className="muted">Etiketlerini basacağınız depoyu seçin.</p>}
      </div>
      <div
        className={`label-sheet label-sheet--${format.layout}`}
        style={{ '--label-width': format.width, '--label-height': format.height, '--label-columns': format.columns } as React.CSSProperties}
      >
        {labels.map((location) => (
          <article key={location.id} className="label">
            <header className="label__header"><span>{warehouse?.code}</span><span>Adres</span></header>
            <div className="label__body">
              <div className="label__main">
                <div className="label__number">{location.code}</div>
                <div className="label__variant">{location.description ?? warehouse?.name}</div>
              </div>
              {options.showQr && <QrCode text={locationBarcode(location.id)} />}
            </div>
            {options.showBarcode && <Barcode text={locationBarcode(location.id)} />}
          </article>
        ))}
      </div>
    </section>
  )
}

interface RecipeRow {
  trialId: number
  requestId: number
  requestNo: string
  trialCode: string
  orderNo: string
  customerName: string | null
  cardCode: string | null
  cardName: string | null
  colorCode: string | null
  colorName: string | null
  decisionOn: string | null
  reusedFromRequestNo: string | null
  items: { code: string; name: string; kind: 'DYE' | 'CHEMICAL'; amount: number }[]
  /** Sipariş satırında renk kartı seçildiyse kartın kodu. */
  colorCardCode: string | null
}

const recipeApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getBoyahaneRecipes: build.query<PagedResult<RecipeRow>, Record<string, unknown>>({
      query: (params) => ({ url: '/boyahane-recipes', params }),
      providesTags: ['BoyahaneLab'],
    }),
  }),
})

/** Reçeteler: müşterinin kabul ettiği lab denemeleri. Boya % (kumaş ağırlığına göre), kimyasal g/L. */
export function RecipesPage() {
  const grid = useGrid()
  const { page, setPage } = grid
  const [input, setInput] = useState('')
  const [search, setSearch] = useState('')
  const [opened, setOpened] = useState<RecipeRow | null>(null)
  const { data, error, isLoading, isFetching } = recipeApi.useGetBoyahaneRecipesQuery({ page, pageSize: 20, search: search || undefined, ...grid.params })

  const columns: GridColumn[] = [
    { key: 'requestNo', label: 'Talep' },
    { key: 'colorName', label: 'Renk' },
    { key: 'cardName', label: 'Kart' },
    { key: 'customerName', label: 'Müşteri' },
    { key: 'orderNo', label: 'Sipariş' },
    { key: 'decisionOn', label: 'Onay tarihi', filter: false },
    { key: 'reusedFromRequestNo', label: 'Kaynak', filter: false },
  ]

  const submit = (event: FormEvent) => {
    event.preventDefault()
    setPage(1)
    setSearch(input.trim())
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">Reçeteler</h1>
      <form className="toolbar" onSubmit={submit} role="search">
        <input type="search" placeholder="Talep no, müşteri, kart ya da renk" aria-label="Reçete ara" value={input} onChange={(event) => setInput(event.target.value)} />
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
                  <tr key={row.trialId} className="table__row--link" onClick={() => setOpened(row)}>
                    <td><code>{row.requestNo}</code> / {row.trialCode}</td>
                    <td>{row.colorCode && <code>{row.colorCode}</code>} {row.colorName ?? '—'}{row.colorCardCode && <span className="muted"> · Kart {row.colorCardCode}</span>}</td>
                    <td>{row.cardName ?? '—'}</td>
                    <td>{row.customerName ?? 'İşletme malı'}</td>
                    <td>{row.orderNo}</td>
                    <td>{row.decisionOn ? formatDate(row.decisionOn) : '—'}</td>
                    <td>{row.reusedFromRequestNo ?? 'Laboratuvar'}</td>
                  </tr>
                ))}
                {data.items.length === 0 && <tr><td colSpan={columns.length} className="table__empty">Onaylı reçete yok.</td></tr>}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}
      {opened && (
        <Modal title={`Reçete: ${opened.requestNo} / ${opened.trialCode}`} onClose={() => setOpened(null)}>
          <p className="muted">{opened.cardName} · {opened.colorName ?? '—'} · {opened.customerName ?? 'İşletme malı'} · <Link to={`/boyahane/lab/${opened.requestId}`}>Talebe git</Link></p>
          <div className="table-wrap">
            <table className="table">
              <thead><tr><th>Kod</th><th>Ad</th><th className="table__number">Miktar</th></tr></thead>
              <tbody>
                {opened.items.map((item) => (
                  <tr key={item.code}>
                    <td><code>{item.code}</code></td>
                    <td>{item.name}</td>
                    <td className="table__number">{formatRecipeAmount(item.amount)} {item.kind === 'DYE' ? '%' : 'g/L'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </Modal>
      )}
    </section>
  )
}
