import { Search } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { GridHead } from '../../components/grid/GridHead'
import type { GridColumn } from '../../components/grid/GridHead'
import { useGrid } from '../../components/grid/useGrid'
import { Pagination } from '../../components/Pagination'
import { formatDateTime } from '../../utils/format'
import { dayRangeToUtc } from '../productionTracking/trackingPresentation'
import { useGetStockTransactionsQuery } from './stockApi'
import type { StockTransaction } from './stockApi'
import { formatSignedQuantity, formatStockQuantity, transactionTypePresentation } from './stockPresentation'
import { ClearFiltersButton } from '../../components/grid/ClearFiltersButton'
import { WarehouseFilter } from './WarehouseFilter'

const PAGE_SIZE = 25

function Reference({ transaction }: { transaction: StockTransaction }) {
  const parts = [
    transaction.counterWarehouseName && `Karşı depo: ${transaction.counterWarehouseName}`,
    transaction.counterLocationCode && `Karşı adres: ${transaction.counterLocationCode}`,
    transaction.documentNo && `Belge: ${transaction.documentNo}`,
    transaction.note,
  ].filter(Boolean)

  return (
    <>
      {transaction.bundleId !== null && (
        <div><Link to={`/bundles/${transaction.bundleId}`}>Demet {transaction.bundleNo}</Link></div>
      )}
      {transaction.lotId !== null && (
        <div>
          <Link to={`/stock-lots?lotId=${transaction.lotId}`}>Top {transaction.lotNo}</Link>
          {transaction.lotBalanceAfter !== null && (
            <span className="muted"> · kalan {formatStockQuantity(transaction.lotBalanceAfter, transaction.unit)}</span>
          )}
        </div>
      )}
      {transaction.orderNo && <div>Sipariş {transaction.orderNo}</div>}
      {transaction.shipmentId !== null && (
        <div><Link to={`/shipments/${transaction.shipmentId}`}>Sevkiyat {transaction.shipmentNo}</Link></div>
      )}
      {transaction.purchaseOrderId !== null && (
        <div><Link to={`/purchase-orders/${transaction.purchaseOrderId}`}>Satın alma {transaction.purchaseOrderNo}</Link></div>
      )}
      {parts.length > 0 && <div className="muted">{parts.join(' · ')}</div>}
      {transaction.bundleId === null && transaction.lotId === null && !transaction.orderNo && transaction.purchaseOrderId === null && transaction.shipmentId === null && parts.length === 0 && '—'}
    </>
  )
}

/** Stok hareketleri: değiştirilemez kayıtlar, en yeniden eskiye. */
export function StockTransactionsPage() {

  const grid = useGrid()
  const { page, setPage } = grid
  // Stok ekranındaki satır panelinden gelindiğinde liste depo ve karta süzülü açılır (?warehouseId=…&search=KOD).
  const [searchParams] = useSearchParams()
  const [warehouseId, setWarehouseId] = useState<number | ''>(() => Number(searchParams.get('warehouseId')) || '')
  const [searchInput, setSearchInput] = useState(() => searchParams.get('search') ?? '')
  const [search, setSearch] = useState(() => (searchParams.get('search') ?? '').trim())
  const [fromDate, setFromDate] = useState('')
  const [toDate, setToDate] = useState('')

  // Tarih filtresi isteğe bağlıdır; yalnızca iki tarih de verilince uygulanır.
  const range = fromDate && toDate ? dayRangeToUtc(fromDate, toDate) : null
  const invalidRange = Boolean(fromDate && toDate && !range)

  const { data, error, isLoading, isFetching } = useGetStockTransactionsQuery(
    {
      page,
      pageSize: PAGE_SIZE,
      warehouseId: warehouseId || undefined,
      search: search || undefined,
      ...(range ?? {}),
      ...grid.params,
    },
    { skip: invalidRange },
  )

  // İlgili kayıt birden çok alanı gösterir; araması üstteki serbest metin kutusundadır.
  const columns: GridColumn[] = [
    { key: 'occurredAt', label: 'Zaman', filter: false },
    {
      key: 'type',
      label: 'Tür',
      filter: Object.entries(transactionTypePresentation).map(([value, { label }]) => ({ value, label })),
    },
    { key: 'itemCode', label: 'Stok kartı' },
    { key: 'warehouseCode', label: 'Depo' },
    { key: 'locationCode', label: 'Adres' },
    { key: 'quantity', label: 'Miktar', numeric: true },
    { key: 'balanceAfter', label: 'Depo bakiyesi', numeric: true },
    { key: 'locationBalanceAfter', label: 'Adres bakiyesi', numeric: true },
    { label: 'İlgili kayıt' },
    { key: 'createdBy', label: 'Yapan' },
  ]

  const handleSearch = (event: FormEvent) => {
    event.preventDefault()
    setPage(1)
    setSearch(searchInput.trim())
  }

  const resetPage = <T,>(setter: (value: T) => void) => (value: T) => {
    setPage(1)
    setter(value)
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">Stok Hareketleri</h1>

      <form className="toolbar" onSubmit={handleSearch} role="search">
        <input
          type="search"
          placeholder="Kart, belge, demet veya top no"
          aria-label="Hareket ara"
          value={searchInput}
          onChange={(event) => setSearchInput(event.target.value)}
        />
        <WarehouseFilter value={warehouseId} onChange={resetPage(setWarehouseId)} />
        <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
        <ClearFiltersButton grid={grid} />
      </form>
      <div className="toolbar toolbar--filters">
        <label>
          Başlangıç
          <input type="date" value={fromDate} onChange={(event) => resetPage(setFromDate)(event.target.value)} />
        </label>
        <label>
          Bitiş
          <input type="date" value={toDate} onChange={(event) => resetPage(setToDate)(event.target.value)} />
        </label>
      </div>

      {invalidRange && <p className="alert alert--error" role="alert">Bitiş tarihi başlangıçtan önce olamaz.</p>}
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />

      {data && !invalidRange && (
        <>
          <div className="table-wrap" aria-busy={isFetching}>
            <table className="table">
              <GridHead columns={columns} grid={grid} />
              <tbody>
                {data.items.map((transaction) => {
                  const presentation = transactionTypePresentation[transaction.type]
                  return (
                    <tr key={transaction.id}>
                      <td>{formatDateTime(transaction.occurredAt)}</td>
                      <td>
                        <span className={`badge badge--${presentation.tone}`}>
                          <span aria-hidden="true">{presentation.icon}</span> {presentation.label}
                        </span>
                      </td>
                      <td><code>{transaction.itemCode}</code> {transaction.itemName}</td>
                      <td>{transaction.warehouseCode}</td>
                      <td><code>{transaction.locationCode}</code></td>
                      <td className="table__number">
                        <strong>{formatSignedQuantity(transaction.quantity, transaction.unit)}</strong>
                      </td>
                      <td className="table__number">{formatStockQuantity(transaction.balanceAfter, transaction.unit)}</td>
                      <td className="table__number">
                        {formatStockQuantity(transaction.locationBalanceAfter, transaction.unit)}
                      </td>
                      <td><Reference transaction={transaction} /></td>
                      <td>{transaction.createdBy}</td>
                    </tr>
                  )
                })}
                {data.items.length === 0 && (
                  <tr>
                    <td colSpan={columns.length} className="table__empty">Hareket bulunamadı.</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}
    </section>
  )
}
