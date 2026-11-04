import { ClipboardCheck, Printer, Search, Send } from 'lucide-react'
import { useMemo, useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { fieldErrors } from '../../api/envelope'
import { ErrorAlert, TextField } from '../../components/FormControls'
import { GridHead } from '../../components/grid/GridHead'
import type { GridColumn } from '../../components/grid/GridHead'
import { useGrid } from '../../components/grid/useGrid'
import { Modal } from '../../components/Modal'
import { RecordLink } from '../../components/RecordLink'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDate, formatDateTimeShort, formatQuantity, todayIsoDate } from '../../utils/format'
import { ScanInput } from '../terminal/ScanInput'
import { formatStockQuantity, parseQuantity } from '../stock/stockPresentation'
import {
  useDispatchShipmentMutation,
  useGetPickingQueueQuery,
  useGetReadyOrderQuery,
  useGetReadyOrdersQuery,
  useGetShipmentQuery,
  useShipmentTransitionMutation,
  useTransferReadyOrderMutation,
} from './shippingApi'
import type { ReadyOrder, ShipmentStatus } from './shippingApi'
import { shipmentStatusPresentation } from './shippingPresentation'

function SearchBar({ placeholder, onSearch }: { placeholder: string; onSearch: (value: string) => void }) {
  const [value, setValue] = useState('')
  const handleSubmit = (event: FormEvent) => {
    event.preventDefault()
    onSearch(value.trim())
  }
  return (
    <form className="toolbar toolbar--compact" onSubmit={handleSubmit} role="search">
      <input type="search" placeholder={placeholder} aria-label={placeholder} value={value} onChange={(event) => setValue(event.target.value)} />
      <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
    </form>
  )
}

const progress = (picked: number, total: number) => (total > 0 ? Math.round((picked / total) * 100) : 0)

// ───────── Sevk bekleyen siparişler ─────────

/**
 * Sevke hazır siparişler: üretimden teslim alınarak ya da stoktan siparişe ayrılmış, henüz bir sevkiyata
 * aktarılmamış mal. Sevkiyata aktarma, depo bazında yeni bir sevkiyat açar ve depoya toplamaya verir.
 */
export function ReadyOrdersPage() {
  const canManage = usePermission(Permissions.ShipmentManage)
  const [search, setSearch] = useState('')
  const grid = useGrid()
  const { data, error, isLoading, isFetching } = useGetReadyOrdersQuery({ search: search || undefined })
  const [transferring, setTransferring] = useState<ReadyOrder | null>(null)

  const columns: GridColumn[] = [
    { label: 'Sipariş' },
    { label: 'Müşteri' },
    { label: 'Termin' },
    { label: 'Sipariş adedi', numeric: true },
    { label: 'Sevk edilen', numeric: true },
    { label: 'Depoda ayrılan', numeric: true },
    { label: 'Açık sevkiyatta', numeric: true },
    { label: 'Aktarılabilir', numeric: true },
    { label: 'Depo' },
    { label: '', ariaLabel: 'İşlemler' },
  ]

  return (
    <section className="page">
      <h1 className="visually-hidden">Sevk Bekleyen Siparişler</h1>
      <SearchBar placeholder="Sipariş no, müşteri veya müşteri sipariş no" onSearch={setSearch} />
      <p className="muted">
        Üretimden teslim alınan mal, iş emrinin müşteri siparişine kendiliğinden ayrılır; serbest stok Depo › Stok ekranından
        siparişe ayrılabilir. Ayrılan mal buradan sevkiyata aktarılır ve depoya toplamaya gider.
      </p>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {data && (
        <div className="table-wrap" aria-busy={isFetching}>
          <table className="table">
            <GridHead columns={columns} grid={grid} />
            <tbody>
              {data.map((order) => (
                <tr key={order.salesOrderId}>
                  <td>
                    <RecordLink to={`/sales-orders/${order.salesOrderId}`}>{order.orderNo}</RecordLink>
                    {order.customerOrderNo && <span className="muted"> · {order.customerOrderNo}</span>}
                  </td>
                  <td>{order.customerName}</td>
                  <td>{formatDate(order.dueDate)}</td>
                  <td className="table__number">{formatQuantity(order.ordered)}</td>
                  <td className="table__number">{formatQuantity(order.shipped)}</td>
                  <td className="table__number">{formatQuantity(order.reserved)}</td>
                  <td className="table__number">
                    {order.inShipments > 0 ? `${formatQuantity(order.inShipments)} (${order.openShipmentCount} sevkiyat)` : '—'}
                  </td>
                  <td className="table__number"><strong>{formatQuantity(order.transferable)}</strong></td>
                  <td>{order.warehouseCodes.join(', ')}</td>
                  <td className="table__actions">
                    {canManage && (
                      <button type="button" className="button button--tiny" onClick={() => setTransferring(order)}>
                        <Send aria-hidden="true" />Sevkiyata aktar
                      </button>
                    )}
                  </td>
                </tr>
              ))}
              {data.length === 0 && (
                <tr><td colSpan={columns.length} className="table__empty">Sevkiyata aktarılmayı bekleyen sipariş yok.</td></tr>
              )}
            </tbody>
          </table>
        </div>
      )}
      {transferring && <TransferDialog order={transferring} onClose={() => setTransferring(null)} />}
    </section>
  )
}

/** Siparişin bir depodaki ayrılmış malını sevkiyata aktarma penceresi: adetler varsayılan olarak aktarılabilir kadardır. */
function TransferDialog({ order, onClose }: { order: ReadyOrder; onClose: () => void }) {
  const navigate = useNavigate()
  const { data, error, isLoading } = useGetReadyOrderQuery(order.salesOrderId)
  const [transfer, state] = useTransferReadyOrderMutation()
  const warehouses = useMemo(() => {
    const seen = new Map<number, string>()
    for (const line of data?.lines ?? []) {
      if (line.transferable > 0) {
        seen.set(line.warehouseId, line.warehouseCode)
      }
    }
    return [...seen.entries()]
  }, [data])
  const [warehouseId, setWarehouseId] = useState<number | null>(null)
  const selectedWarehouse = warehouseId ?? warehouses[0]?.[0] ?? null
  const [shipDate, setShipDate] = useState(todayIsoDate())
  const [carrier, setCarrier] = useState('')
  const [notes, setNotes] = useState('')
  const [quantities, setQuantities] = useState<Record<number, string>>({})

  const lines = (data?.lines ?? []).filter((line) => line.warehouseId === selectedWarehouse && line.transferable > 0)
  const requested = lines.map((line) => ({
    line,
    quantity: quantities[line.stockItemId] === undefined ? line.transferable : parseQuantity(quantities[line.stockItemId]),
  }))
  const invalid = requested.some((entry) => entry.quantity === null || entry.quantity < 0 || entry.quantity > entry.line.transferable)
  const total = requested.reduce((sum, entry) => sum + (entry.quantity ?? 0), 0)

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    if (selectedWarehouse === null || invalid || total <= 0) {
      return
    }

    const result = await transfer({
      salesOrderId: order.salesOrderId,
      warehouseId: selectedWarehouse,
      shipDate,
      carrier: carrier.trim() || null,
      notes: notes.trim() || null,
      lines: requested.filter((entry) => (entry.quantity ?? 0) > 0).map((entry) => ({ stockItemId: entry.line.stockItemId, quantity: entry.quantity ?? 0 })),
    })
    if (!('error' in result)) {
      onClose()
      void navigate(`/shipments/${result.data}`)
    }
  }

  return (
    <Modal title={`Sevkiyata aktar · ${order.orderNo}`} onClose={onClose}>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {data && (
        <form className="form" onSubmit={(event) => void handleSubmit(event)} noValidate>
          <p className="muted">
            {order.customerName}{order.customerOrderNo && ` · müşteri sipariş no ${order.customerOrderNo}`} · termin {formatDate(order.dueDate)}
          </p>
          <div className="form__grid">
            <label className="field">
              <span>Çıkış deposu</span>
              <select value={selectedWarehouse ?? ''} onChange={(event) => setWarehouseId(Number(event.target.value))}>
                {warehouses.map(([id, code]) => <option key={id} value={id}>{code}</option>)}
              </select>
            </label>
            <TextField label="Sevk tarihi" type="date" value={shipDate} onChange={(event) => setShipDate(event.target.value)}
              errors={fieldErrors(state.error, 'shipDate')} />
            <TextField label="Taşıyıcı (isteğe bağlı)" value={carrier} maxLength={150} onChange={(event) => setCarrier(event.target.value)} />
          </div>

          <div className="table-wrap">
            <table className="table table--compact">
              <thead>
                <tr>
                  <th>Mamul</th>
                  <th>Renk</th>
                  <th>Beden</th>
                  <th className="table__number">Sipariş</th>
                  <th className="table__number">Sevk edilen</th>
                  <th className="table__number">Ayrılan</th>
                  <th className="table__number">Açık sevkiyatta</th>
                  <th className="table__number">Aktarılacak</th>
                </tr>
              </thead>
              <tbody>
                {requested.map(({ line, quantity }) => (
                  <tr key={line.stockItemId}>
                    <td><code>{line.productCode ?? line.itemCode}</code> {line.productName}</td>
                    <td>{line.colorName ?? '—'}</td>
                    <td>{line.sizeName ?? '—'}</td>
                    <td className="table__number">{formatQuantity(line.ordered)}</td>
                    <td className="table__number">{formatQuantity(line.shipped)}</td>
                    <td className="table__number">{formatQuantity(line.reserved)}</td>
                    <td className="table__number">{formatQuantity(line.inShipments)}</td>
                    <td className="table__number">
                      <input
                        className="matrix-input"
                        inputMode="numeric"
                        aria-label={`${line.itemCode} aktarılacak adet`}
                        aria-invalid={quantity === null || quantity > line.transferable}
                        value={quantities[line.stockItemId] ?? String(line.transferable)}
                        onChange={(event) => setQuantities((current) => ({ ...current, [line.stockItemId]: event.target.value }))}
                      />
                    </td>
                  </tr>
                ))}
                {requested.length === 0 && <tr><td colSpan={8} className="table__empty">Bu depoda aktarılacak mal yok.</td></tr>}
              </tbody>
              <tfoot>
                <tr><th colSpan={7}>Toplam</th><td className="table__number"><strong>{formatQuantity(total)}</strong></td></tr>
              </tfoot>
            </table>
          </div>

          {data.shipments.length > 0 && (
            <p className="muted">
              Siparişin açık sevkiyatları:{' '}
              {data.shipments.map((shipment, index) => (
                <span key={shipment.id}>
                  {index > 0 && ', '}
                  <Link to={`/shipments/${shipment.id}`}>{shipment.shipmentNo}</Link> ({shipmentStatusPresentation[shipment.status].label},{' '}
                  {formatQuantity(shipment.quantity)} adet)
                </span>
              ))}
            </p>
          )}

          <label className="field">
            <span>Not (isteğe bağlı)</span>
            <textarea rows={2} maxLength={1000} value={notes} onChange={(event) => setNotes(event.target.value)} />
          </label>
          {invalid && <p className="alert alert--error" role="alert">Aktarılacak adet sıfır ile aktarılabilir adet arasında olmalıdır.</p>}
          <ErrorAlert error={state.error} fields={['shipDate']} />

          <footer className="form__actions">
            <button type="button" className="button" onClick={onClose}>Vazgeç</button>
            <button type="submit" className="button button--primary" disabled={state.isLoading || invalid || total <= 0 || selectedWarehouse === null}>
              {state.isLoading ? 'Aktarılıyor…' : 'Aktar ve toplamaya gönder'}
            </button>
          </footer>
        </form>
      )}
    </Modal>
  )
}

// ───────── Toplama işleri (depo) ─────────

/**
 * Depodaki toplama işleri: toplamaya verilmiş sevkiyatlar ve ilerlemeleri. Toplama el terminalinde yapılır;
 * buradan kağıt toplama listesi basılabilir.
 */
export function PickingQueuePage() {
  const [status, setStatus] = useState<ShipmentStatus>('PICKING')
  const [search, setSearch] = useState('')
  const grid = useGrid()
  const { data, error, isLoading, isFetching } = useGetPickingQueueQuery({ status, search: search || undefined })

  const columns: GridColumn[] = [
    { label: 'Sevkiyat' },
    { label: 'Müşteri' },
    { label: 'Sipariş' },
    { label: 'Sevk günü' },
    { label: 'Depo' },
    { label: 'Kalem', numeric: true },
    { label: 'İlerleme', numeric: true },
    { label: 'Toplayan' },
    { label: '', ariaLabel: 'İşlemler' },
  ]

  return (
    <section className="page">
      <h1 className="visually-hidden">Toplama İşleri</h1>
      <div className="toolbar toolbar--compact">
        <div className="segmented" role="group" aria-label="Durum">
          {(['PICKING', 'PICKED'] as const).map((value) => (
            <button
              key={value}
              type="button"
              className={status === value ? 'segmented__item segmented__item--active' : 'segmented__item'}
              aria-pressed={status === value}
              onClick={() => setStatus(value)}
            >
              {value === 'PICKING' ? 'Toplanacaklar' : 'Toplananlar (irsaliye bekliyor)'}
            </button>
          ))}
        </div>
        <SearchBar placeholder="Sevkiyat no, müşteri veya sipariş no" onSearch={setSearch} />
      </div>
      <p className="muted">
        Toplama el terminalinde yapılır (El Terminali › Sevkiyat toplama): liste adres sırasıyladır, adres ve ürün okutularak
        satır satır onaylanır. Stok, irsaliye kesilince düşer.
      </p>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {data && (
        <div className="table-wrap" aria-busy={isFetching}>
          <table className="table">
            <GridHead columns={columns} grid={grid} />
            <tbody>
              {data.map((item) => (
                <tr key={item.id}>
                  <td><Link to={`/shipments/${item.id}`}>{item.shipmentNo}</Link></td>
                  <td>{item.customerName}</td>
                  <td>{item.orderNo ?? '—'}{item.customerOrderNo && <span className="muted"> · {item.customerOrderNo}</span>}</td>
                  <td>{formatDate(item.shipDate)}</td>
                  <td>{item.warehouseCode}</td>
                  <td className="table__number">{item.itemCount}</td>
                  <td className="table__number">
                    {formatQuantity(item.pickedQuantity)} / {formatQuantity(item.quantity)}{' '}
                    <span className="muted">(%{progress(item.pickedQuantity, item.quantity)})</span>
                  </td>
                  <td>
                    {item.pickingBy
                      ? <>{item.pickingBy} <span className="muted">· {formatDateTimeShort(item.pickingStartedAt)}</span></>
                      : <span className="muted">Başlanmadı</span>}
                  </td>
                  <td className="table__actions">
                    <Link className="button button--tiny" to={`/shipments/${item.id}/pick-list`}><Printer aria-hidden="true" />Liste</Link>
                    {status === 'PICKED' && (
                      <Link className="button button--tiny" to={`/despatch-queue/${item.id}`}><ClipboardCheck aria-hidden="true" />Kontrol</Link>
                    )}
                  </td>
                </tr>
              ))}
              {data.length === 0 && (
                <tr><td colSpan={columns.length} className="table__empty">
                  {status === 'PICKING' ? 'Toplanacak sevkiyat yok.' : 'İrsaliye bekleyen sevkiyat yok.'}
                </td></tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </section>
  )
}

// ───────── İrsaliye bekleyenler (sevkiyat) ─────────

/** Toplaması bitmiş, son adet kontrolü ve irsaliye bekleyen sevkiyatlar. */
export function DespatchQueuePage() {
  const [search, setSearch] = useState('')
  const grid = useGrid()
  const { data, error, isLoading, isFetching } = useGetPickingQueueQuery({ status: 'PICKED', search: search || undefined })

  const columns: GridColumn[] = [
    { label: 'Sevkiyat' },
    { label: 'Müşteri' },
    { label: 'Sipariş' },
    { label: 'Sevk günü' },
    { label: 'Depo' },
    { label: 'Kalem', numeric: true },
    { label: 'Toplanan adet', numeric: true },
    { label: 'Toplayan' },
    { label: '', ariaLabel: 'İşlemler' },
  ]

  return (
    <section className="page">
      <h1 className="visually-hidden">İrsaliye Bekleyenler</h1>
      <SearchBar placeholder="Sevkiyat no, müşteri veya sipariş no" onSearch={setSearch} />
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {data && (
        <div className="table-wrap" aria-busy={isFetching}>
          <table className="table">
            <GridHead columns={columns} grid={grid} />
            <tbody>
              {data.map((item) => (
                <tr key={item.id}>
                  <td><Link to={`/shipments/${item.id}`}>{item.shipmentNo}</Link></td>
                  <td>{item.customerName}</td>
                  <td>{item.orderNo ?? '—'}{item.customerOrderNo && <span className="muted"> · {item.customerOrderNo}</span>}</td>
                  <td>{formatDate(item.shipDate)}</td>
                  <td>{item.warehouseCode}</td>
                  <td className="table__number">{item.itemCount}</td>
                  <td className="table__number">{formatQuantity(item.quantity)}</td>
                  <td>{item.pickingBy ?? '—'}</td>
                  <td className="table__actions">
                    <Link className="button button--tiny button--primary" to={`/despatch-queue/${item.id}`}>
                      <ClipboardCheck aria-hidden="true" />Kontrol ve irsaliye
                    </Link>
                  </td>
                </tr>
              ))}
              {data.length === 0 && (
                <tr><td colSpan={columns.length} className="table__empty">İrsaliye bekleyen sevkiyat yok.</td></tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </section>
  )
}

/**
 * Son kontrol ve irsaliye: toplanan kalemler sayılır (ürün barkodu okutuldukça ya da elle), sayım toplananla birebir
 * tutunca irsaliye kesilir. Fark varsa sevkiyat yeniden toplamaya açılır.
 */
export function DespatchCheckPage() {
  const shipmentId = Number(useParams().id)
  const navigate = useNavigate()
  const canShip = usePermission(Permissions.ShipmentShip)
  const canManage = usePermission(Permissions.ShipmentManage)
  const { data: shipment, error, isLoading } = useGetShipmentQuery(shipmentId, { skip: !Number.isInteger(shipmentId) })
  const [dispatch, state] = useDispatchShipmentMutation()
  const [transition, transitionState] = useShipmentTransitionMutation()
  const [counts, setCounts] = useState<Record<number, string>>({})
  const [problem, setProblem] = useState<string | null>(null)
  const [scanStep, setScanStep] = useState(0)
  const [documentNo, setDocumentNo] = useState('')
  const [carrier, setCarrier] = useState<string | null>(null)
  const [vehiclePlate, setVehiclePlate] = useState('')
  const [driverName, setDriverName] = useState('')
  const [driverIdentityNo, setDriverIdentityNo] = useState('')

  if (isLoading) {
    return <section className="page"><p role="status">Yükleniyor…</p></section>
  }

  if (error || !shipment) {
    return <section className="page"><h1>İrsaliye</h1><ErrorAlert error={error} /></section>
  }

  const counted = (itemId: number) => {
    const text = counts[itemId]
    return text === undefined || text.trim() === '' ? null : parseQuantity(text)
  }
  const rows = shipment.items.map((item) => ({ item, count: counted(item.id) }))
  const allMatch = rows.length > 0 && rows.every((row) => row.count === row.item.quantity)
  const cartons = shipment.cartons

  const handleScan = (code: string) => {
    setProblem(null)
    const text = code.toLocaleUpperCase('en-US')
    const item = shipment.items.find((entry) => entry.itemCode.toLocaleUpperCase('en-US') === text)
    if (!item) {
      setProblem(`${text} bu sevkiyatta yok. Yanlış ürün toplanmış olabilir.`)
      return
    }

    setCounts((current) => ({ ...current, [item.id]: String((counted(item.id) ?? 0) + 1) }))
    setScanStep((current) => current + 1)
  }

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    if (!allMatch) {
      return
    }

    if (!window.confirm('İrsaliye kesilecek: kalemler stoktan düşer ve sevkiyat kesinleşir. Bu işlem geri alınamaz. Devam edilsin mi?')) {
      return
    }

    const result = await dispatch({
      id: shipment.id,
      body: {
        rowVersion: shipment.rowVersion,
        checks: rows.map((row) => ({ shipmentItemId: row.item.id, quantity: row.count ?? 0 })),
        documentNo: documentNo.trim() || null,
        carrier: (carrier ?? shipment.carrier ?? '').trim() || null,
        vehiclePlate: vehiclePlate.trim() || null,
        driverName: driverName.trim() || null,
        driverIdentityNo: driverIdentityNo.trim() || null,
      },
    })
    if (!('error' in result)) {
      void navigate(`/shipments/${shipment.id}/e-despatch`)
    }
  }

  const status = shipmentStatusPresentation[shipment.status]
  const checkErrors = fieldErrors(state.error, 'checks')

  return (
    <section className="page">
      <header className="page__header">
        <div>
          <h1>
            Kontrol ve irsaliye · {shipment.shipmentNo}{' '}
            <span className={`badge badge--${status.tone}`}><span aria-hidden="true">{status.icon}</span> {status.label}</span>
          </h1>
          <p className="muted">
            <Link to="/despatch-queue">İrsaliye bekleyenler</Link> · {shipment.customerName}
            {shipment.salesOrderNo && ` · ${shipment.salesOrderNo}`}{shipment.customerOrderNo && ` · ${shipment.customerOrderNo}`}
            {' '}· {shipment.warehouseCode} · toplayan {shipment.workflow?.pickedBy ?? '—'}
          </p>
        </div>
        <div className="page__actions">
          <Link className="button" to={`/shipments/${shipment.id}`}>Sevkiyat ayrıntısı</Link>
          {canManage && shipment.workflow?.canReopenPicking && (
            <button
              type="button"
              className="button"
              disabled={transitionState.isLoading}
              onClick={() => {
                if (window.confirm('Sevkiyat yeniden toplamaya açılacak. Devam edilsin mi?')) {
                  void transition({ id: shipment.id, rowVersion: shipment.rowVersion, action: 'reopen-picking' })
                }
              }}
            >
              Yeniden toplamaya aç
            </button>
          )}
        </div>
      </header>

      {shipment.status !== 'PICKED' && (
        <p className="alert alert--info">
          {shipment.status === 'SHIPPED'
            ? <>İrsaliye kesildi: <Link to={`/shipments/${shipment.id}/e-despatch`}>{shipment.documentNo}</Link>.</>
            : 'Bu sevkiyatın toplaması bitmedi; irsaliye, toplama bittikten sonra kesilir.'}
        </p>
      )}
      <ErrorAlert error={transitionState.error} />

      {shipment.status === 'PICKED' && (
        <form className="form" onSubmit={(event) => void handleSubmit(event)} noValidate>
          <ScanInput label="Saydığınız ürünün barkodunu okutun (her okutma bir adet)" onScan={handleScan} focusToken={scanStep} />
          {problem && <p className="alert alert--error" role="alert">{problem}</p>}

          <div className="table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <th>Mamul</th>
                  <th className="table__number">Toplanan</th>
                  <th className="table__number">Sayılan</th>
                  <th>Durum</th>
                </tr>
              </thead>
              <tbody>
                {rows.map(({ item, count }) => {
                  const difference = count === null ? null : count - item.quantity
                  return (
                    <tr key={item.id}>
                      <td><code>{item.itemCode}</code> {item.itemName}</td>
                      <td className="table__number"><strong>{formatStockQuantity(item.quantity, item.unit)}</strong></td>
                      <td className="table__number">
                        <input
                          className="matrix-input"
                          inputMode="numeric"
                          aria-label={`${item.itemCode} sayılan adet`}
                          aria-invalid={difference !== null && difference !== 0}
                          value={counts[item.id] ?? ''}
                          onChange={(event) => setCounts((current) => ({ ...current, [item.id]: event.target.value }))}
                        />
                      </td>
                      <td>
                        {difference === null ? (
                          <span className="badge badge--neutral">Sayılmadı</span>
                        ) : difference === 0 ? (
                          <span className="badge badge--ok"><span aria-hidden="true">✓</span> Tutuyor</span>
                        ) : (
                          <span className="badge badge--danger">{difference > 0 ? `${formatQuantity(difference)} fazla` : `${formatQuantity(-difference)} eksik`}</span>
                        )}
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
          {checkErrors && <ul className="alert alert--error">{checkErrors.map((message) => <li key={message}>{message}</li>)}</ul>}
          {cartons !== null && cartons.cartonCount > 0 && !cartons.matchesItems && (
            <p className="alert alert--error" role="alert">
              Koliler toplanan adetlerle tutmuyor; irsaliyeden önce <Link to={`/shipments/${shipment.id}`}>sevkiyat ayrıntısında</Link> kolileri düzeltin.
            </p>
          )}

          <fieldset className="form__section">
            <legend>İrsaliye ve taşıyıcı</legend>
            <div className="form__grid">
              <TextField label="İrsaliye no" value={documentNo} maxLength={50} placeholder={shipment.documentNo ?? 'Boş bırakılırsa sistem verir'}
                onChange={(event) => setDocumentNo(event.target.value)} errors={fieldErrors(state.error, 'documentNo')} />
              <TextField label="Taşıyıcı firma" value={carrier ?? shipment.carrier ?? ''} maxLength={150}
                onChange={(event) => setCarrier(event.target.value)} errors={fieldErrors(state.error, 'carrier')} />
              <TextField label="Araç plakası" value={vehiclePlate} maxLength={20}
                onChange={(event) => setVehiclePlate(event.target.value)} errors={fieldErrors(state.error, 'vehiclePlate')} />
            </div>
            <div className="form__grid">
              <TextField label="Şoför adı soyadı" value={driverName} maxLength={100}
                onChange={(event) => setDriverName(event.target.value)} errors={fieldErrors(state.error, 'driverName')} />
              <TextField label="Şoför T.C. kimlik no" value={driverIdentityNo} maxLength={11} inputMode="numeric"
                onChange={(event) => setDriverIdentityNo(event.target.value)} errors={fieldErrors(state.error, 'driverIdentityNo')} />
            </div>
          </fieldset>
          <ErrorAlert error={state.error} fields={['checks', 'documentNo', 'carrier', 'vehiclePlate', 'driverName', 'driverIdentityNo']} />

          <footer className="form__actions">
            <span className="muted">
              {allMatch ? 'Bütün kalemler tutuyor.' : `${rows.filter((row) => row.count === row.item.quantity).length} / ${rows.length} kalem tutuyor.`}
            </span>
            {canShip && (
              <button type="submit" className="button button--primary" disabled={!allMatch || state.isLoading}>
                {state.isLoading ? 'İrsaliye kesiliyor…' : 'İrsaliye kes'}
              </button>
            )}
          </footer>
        </form>
      )}
    </section>
  )
}
