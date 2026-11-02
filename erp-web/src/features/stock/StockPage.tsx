import { ChevronDown, Search } from 'lucide-react'
import { Fragment, useState } from 'react'
import type { FormEvent } from 'react'
import { useSearchParams } from 'react-router-dom'
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
  // İşlem paneli açık olan satır (aynı anda tek satır).
  const [expanded, setExpanded] = useState<string | null>(null)

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

  const handleSearch = (event: FormEvent) => {
    event.preventDefault()
    setPage(1)
    setSearch(searchInput.trim())
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">Stok</h1>

      <form className="toolbar" onSubmit={handleSearch} role="search">
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
        <label className="checkbox">
          <input
            type="checkbox"
            checked={includeZero}
            onChange={(event) => {
              setPage(1)
              setIncludeZero(event.target.checked)
            }}
          />
          Bakiyesi sıfır olanları da göster
        </label>
        <label className="checkbox">
          <input
            type="checkbox"
            checked={byLocation}
            onChange={(event) => {
              // Sütunlar değişir: adres sütununa ait filtre / sıralama depo görünümünde geçersizdir.
              grid.clearFilters()
              setPage(1)
              setByLocation(event.target.checked)
            }}
          />
          Adres bazında göster
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
                      <tr className={open ? 'table__row--expanded' : undefined}>
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
                            className="button stock-row__toggle"
                            aria-expanded={open}
                            onClick={() => setExpanded(open ? null : key)}
                          >
                            İşlem
                            <ChevronDown aria-hidden="true" />
                          </button>
                        </td>
                      </tr>
                      {open && (
                        <tr className="table__detail-row">
                          <td colSpan={columns.length}>
                            <StockRowPanel
                              balance={balance}
                              canManage={canManage}
                              canAdjust={canAdjust}
                              onAction={(kind) => setDialog({ kind, balance })}
                            />
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
