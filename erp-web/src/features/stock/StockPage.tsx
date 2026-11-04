import {
  ArrowDownToLine, ArrowLeftRight, ArrowUpFromLine, BookmarkCheck, ChevronDown, ClipboardCheck, History, Layers, ListOrdered, MapPin, Search,
} from 'lucide-react'
import { Fragment, useState } from 'react'
import type { FormEvent } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { ContextMenu } from '../../components/ContextMenu'
import type { ContextMenuItem } from '../../components/ContextMenu'
import { ErrorAlert } from '../../components/FormControls'
import { GridHead } from '../../components/grid/GridHead'
import type { GridColumn } from '../../components/grid/GridHead'
import { useGrid } from '../../components/grid/useGrid'
import { Pagination } from '../../components/Pagination'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { FinishedGoodCardDialog } from './FinishedGoodCardDialog'
import { MovementDialog } from './MovementDialog'
import type { MovementKind } from './MovementDialog'
import { StockReservationDialog } from './StockReservationDialog'
import { StockRowPanel } from './StockRowPanel'
import { useGetStockBalancesQuery, useGetStockLocationBalancesQuery } from './stockApi'
import type { StockBalance, StockLocationBalance } from './stockApi'
import { formatStockQuantity, stockItemTypeLabels } from './stockPresentation'
import { ClearFiltersButton } from '../../components/grid/ClearFiltersButton'
import { WarehouseFilter } from './WarehouseFilter'

const PAGE_SIZE = 25

type BalanceRow = StockBalance | StockLocationBalance

const locationOf = (balance: BalanceRow) => ('locationId' in balance ? balance : null)

const rowKey = (balance: BalanceRow) => `${balance.warehouseId}-${locationOf(balance)?.locationId ?? 0}-${balance.stockItemId}`

/**
 * Stok bakiyeleri: depo bazında (adreslerin toplamı) ya da adres bazında. Hareketler buradan girilir; bakiyeyi
 * sunucu tutar.
 */
export function StockPage() {
  const canManage = usePermission(Permissions.StockManage)
  const canAdjust = usePermission(Permissions.StockAdjust)
  const canReserve = usePermission(Permissions.StockReserve)

  const grid = useGrid()
  const { page, setPage } = grid
  const [warehouseId, setWarehouseId] = useState<number | ''>('')
  // Genel aramadan gelindiğinde liste aranan stok kartına süzülü açılır (/stock?search=KOD).
  const [searchParams] = useSearchParams()
  const [searchInput, setSearchInput] = useState(() => searchParams.get('search') ?? '')
  const [search, setSearch] = useState(() => (searchParams.get('search') ?? '').trim())
  const [includeZero, setIncludeZero] = useState(false)
  const [byLocation, setByLocation] = useState(false)
  const [dialog, setDialog] = useState<{ kind: MovementKind; balance?: BalanceRow } | null>(null)
  const [openingCard, setOpeningCard] = useState(false)
  // Ayırmaları açık olan depo + kart (depo görünümündeki satırdan).
  const [reserving, setReserving] = useState<BalanceRow | null>(null)
  // Son hareketleri açık olan satır (aynı anda tek satır).
  const [expanded, setExpanded] = useState<string | null>(null)
  // Satırın işlem menüsü: sağ tıkla ya da satır sonundaki düğmeyle açılır.
  const [menu, setMenu] = useState<{ x: number; y: number; balance: BalanceRow } | null>(null)
  const navigate = useNavigate()

  const params = {
    page,
    pageSize: PAGE_SIZE,
    warehouseId: warehouseId || undefined,
    search: search || undefined,
    includeZero: includeZero || undefined,
    ...grid.params,
  }
  const warehouseBalances = useGetStockBalancesQuery(params, { skip: byLocation })
  const locationBalances = useGetStockLocationBalancesQuery(params, { skip: !byLocation })
  const { error, isLoading, isFetching } = byLocation ? locationBalances : warehouseBalances
  const data: { items: BalanceRow[]; page: number; totalPages: number; totalCount: number } | undefined = byLocation
    ? locationBalances.data
    : warehouseBalances.data

  const columns: GridColumn[] = [
    { key: 'itemCode', label: 'Stok kartı' },
    {
      key: 'itemType',
      label: 'Tür',
      filter: Object.entries(stockItemTypeLabels).map(([value, label]) => ({ value, label })),
    },
    { key: 'warehouseCode', label: 'Depo' },
    ...(byLocation ? [{ key: 'locationCode', label: 'Adres' }] : []),
    { key: 'quantity', label: 'Miktar', numeric: true },
    // Siparişlere ayrılan stok depo düzeyinde tutulur.
    ...(byLocation ? [] : [{ label: 'Ayrılan', numeric: true }, { label: 'Kullanılabilir', numeric: true }]),
    { label: '', ariaLabel: 'İşlemler' },
  ]

  const menuItems = (balance: BalanceRow): ContextMenuItem[] => {
    const key = rowKey(balance)
    const empty = balance.quantity <= 0
    const act = (kind: MovementKind) => () => setDialog({ kind, balance })
    const movements: ContextMenuItem[] = balance.isLotTracked
      ? [
          // Takipli kartta çıkış, transfer ve sayım top seçilerek yapılır.
          ...(canManage || canAdjust
            ? [{
                key: 'lots', label: 'Toplar (çıkış, transfer, sayım)', icon: Layers,
                onSelect: () => navigate(`/stock-lots?stockItemId=${balance.stockItemId}&warehouseId=${balance.warehouseId}`),
              }]
            : []),
        ]
      : [
          ...(canManage
            ? [
                { key: 'issue', label: 'Çıkış', icon: ArrowUpFromLine, disabled: empty, onSelect: act('issue') },
                { key: 'transfer', label: 'Transfer', icon: ArrowLeftRight, disabled: empty, onSelect: act('transfer') },
                { key: 'relocation', label: 'Adres değiştir', icon: MapPin, disabled: empty, onSelect: act('relocation') },
              ]
            : []),
          ...(canAdjust ? [{ key: 'adjustment', label: 'Sayım', icon: ClipboardCheck, onSelect: act('adjustment') }] : []),
        ]
    const actions = [
      ...(canManage ? [{ key: 'receipt', label: 'Giriş', icon: ArrowDownToLine, onSelect: act('receipt') }] : []),
      ...movements,
    ]

    // Ayırma depo düzeyindedir: adres görünümündeki satır da kendi deposunun ayırmalarını açar.
    const reservations: ContextMenuItem[] = [{
      key: 'reservations',
      label: balance.itemType === 'FINISHED_GOOD' && canReserve ? 'Ayırmalar / siparişe ayır' : 'Ayırmalar',
      icon: BookmarkCheck,
      separated: actions.length > 0,
      onSelect: () => setReserving(balance),
    }]

    return [
      ...actions,
      ...reservations,
      {
        key: 'recent', label: expanded === key ? 'Son hareketleri gizle' : 'Son hareketler', icon: History,
        separated: actions.length > 0, onSelect: () => setExpanded(expanded === key ? null : key),
      },
      {
        key: 'all', label: 'Tüm hareketler', icon: ListOrdered,
        onSelect: () => navigate(`/stock-transactions?warehouseId=${balance.warehouseId}&search=${encodeURIComponent(balance.itemCode)}`),
      },
    ]
  }

  const handleSearch = (event: FormEvent) => {
    event.preventDefault()
    setPage(1)
    setSearch(searchInput.trim())
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">Stok</h1>

      <form className="toolbar toolbar--compact" onSubmit={handleSearch} role="search">
        <input
          type="search"
          placeholder={byLocation ? 'Kart kodu, adı veya adres' : 'Kart kodu veya adı'}
          aria-label="Stok kartı ara"
          value={searchInput}
          onChange={(event) => setSearchInput(event.target.value)}
        />
        <WarehouseFilter value={warehouseId} onChange={(value) => {
            setPage(1)
            setWarehouseId(value)
          }} />
        <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
        <ClearFiltersButton grid={grid} />
        <div className="segmented" role="group" aria-label="Görünüm">
          {([[false, 'Depo', 'Depo bazında bakiye'], [true, 'Adres', 'Adres bazında bakiye']] as const).map(
            ([value, label, title]) => (
              <button
                key={label}
                type="button"
                title={title}
                className={byLocation === value ? 'segmented__item segmented__item--active' : 'segmented__item'}
                aria-pressed={byLocation === value}
                onClick={() => {
                  if (byLocation !== value) {
                    // Sütunlar değişir: adres sütununa ait filtre / sıralama depo görünümünde geçersizdir.
                    grid.clearFilters()
                    setPage(1)
                    setByLocation(value)
                  }
                }}
              >
                {label}
              </button>
            ),
          )}
        </div>
        <label className="checkbox" title="Bakiyesi sıfır olan kartları da listele">
          <input
            type="checkbox"
            checked={includeZero}
            onChange={(event) => {
              setPage(1)
              setIncludeZero(event.target.checked)
            }}
          />
          Sıfır bakiyeler
        </label>
        <div className="toolbar__actions">
          {canManage && (
            <>
              <button type="button" className="button" onClick={() => setOpeningCard(true)}>Mamul kartı aç</button>
              <button type="button" className="button button--primary" onClick={() => setDialog({ kind: 'receipt' })}>
                Stok girişi
              </button>
            </>
          )}
        </div>
      </form>

      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {openingCard && <FinishedGoodCardDialog onClose={() => setOpeningCard(false)} />}

      {data && (
        <>
          <div className="table-wrap" aria-busy={isFetching}>
            <table className="table">
              <GridHead columns={columns} grid={grid} />
              <tbody>
                {data.items.map((balance) => {
                  const key = rowKey(balance)
                  const open = expanded === key
                  return (
                    <Fragment key={key}>
                      <tr
                        className={open ? 'stock-row table__row--expanded' : 'stock-row'}
                        title="Son hareketler için tıklayın; işlemler için sağ tıklayın"
                        onClick={() => setExpanded(open ? null : key)}
                        onContextMenu={(event) => {
                          event.preventDefault()
                          setMenu({ x: event.clientX, y: event.clientY, balance })
                        }}
                      >
                        <td><code>{balance.itemCode}</code> {balance.itemName}</td>
                        <td>{stockItemTypeLabels[balance.itemType]}</td>
                        <td>{balance.warehouseCode} — {balance.warehouseName}</td>
                        {byLocation && (
                          <td>
                            <code>{locationOf(balance)?.locationCode}</code>
                          </td>
                        )}
                        <td className="table__number"><strong>{formatStockQuantity(balance.quantity, balance.unit)}</strong></td>
                        {!byLocation && (
                          <>
                            <td className="table__number">
                              {balance.reserved ? formatStockQuantity(balance.reserved, balance.unit) : '—'}
                            </td>
                            <td className="table__number">
                              {formatStockQuantity(Math.max(balance.quantity - (balance.reserved ?? 0), 0), balance.unit)}
                            </td>
                          </>
                        )}
                        <td className="table__actions">
                          <button
                            type="button"
                            className="button row-menu-button"
                            aria-label={`${balance.itemCode} işlemleri`}
                            aria-haspopup="menu"
                            aria-expanded={menu?.balance === balance}
                            onClick={(event) => {
                              event.stopPropagation()
                              const bounds = event.currentTarget.getBoundingClientRect()
                              setMenu({ x: bounds.left, y: bounds.bottom + 2, balance })
                            }}
                          >
                            İşlem
                            <ChevronDown aria-hidden="true" />
                          </button>
                        </td>
                      </tr>
                      {open && (
                        <tr className="table__detail-row">
                          <td colSpan={columns.length}>
                            <StockRowPanel balance={balance} />
                          </td>
                        </tr>
                      )}
                    </Fragment>
                  )
                })}
                {data.items.length === 0 && (
                  <tr>
                    <td colSpan={columns.length} className="table__empty">Stok kaydı bulunamadı.</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}

      {menu && (
        <ContextMenu
          x={menu.x}
          y={menu.y}
          label={`${menu.balance.itemCode} işlemleri`}
          items={menuItems(menu.balance)}
          onClose={() => setMenu(null)}
        />
      )}

      {reserving && (
        <StockReservationDialog
          warehouseId={reserving.warehouseId}
          stockItemId={reserving.stockItemId}
          onClose={() => setReserving(null)}
        />
      )}

      {dialog && (
        <MovementDialog
          // key: başka satır / işlem için açılınca form ve istek kimliği yenilensin.
          key={`${dialog.kind}-${dialog.balance?.warehouseId ?? 0}-${(dialog.balance && locationOf(dialog.balance)?.locationId) ?? 0}-${dialog.balance?.stockItemId ?? 0}`}
          kind={dialog.kind}
          balance={dialog.balance}
          onClose={() => setDialog(null)}
        />
      )}
    </section>
  )
}
