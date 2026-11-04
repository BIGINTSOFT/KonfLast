import { Link } from 'react-router-dom'
import { formatDateTimeShort } from '../../utils/format'
import { useGetStockTransactionsQuery } from './stockApi'
import type { StockBalance, StockLocationBalance } from './stockApi'
import { formatSignedQuantity, formatStockQuantity, transactionTypePresentation } from './stockPresentation'

const RECENT = 5

interface StockRowPanelProps {
  balance: StockBalance | StockLocationBalance
}

/**
 * Stok satırının altında açılan panel: kartın bu depodaki (adres satırında bu adresteki) son hareketleri.
 * Hareket işlemleri satırın işlem menüsündedir.
 */
export function StockRowPanel({ balance }: StockRowPanelProps) {
  const locationId = 'locationId' in balance ? balance.locationId : undefined
  const { data, isLoading } = useGetStockTransactionsQuery({
    page: 1,
    pageSize: RECENT,
    stockItemId: balance.stockItemId,
    warehouseId: balance.warehouseId,
    locationId,
  })
  const allMovements = `/stock-transactions?warehouseId=${balance.warehouseId}&search=${encodeURIComponent(balance.itemCode)}`

  return (
    <div className="stock-panel">
      <div className="stock-panel__history">
        <div className="stock-panel__heading">
          <strong>Son hareketler</strong>
          <Link to={allMovements}>Tüm hareketler</Link>
        </div>
        {isLoading && <p role="status" className="muted">Yükleniyor…</p>}
        {data && data.items.length === 0 && <p className="muted">Hareket yok.</p>}
        {data && data.items.length > 0 && (
          <table className="table table--compact">
            <thead>
              <tr>
                <th>Zaman</th>
                <th>Hareket</th>
                <th>Adres</th>
                <th className="table__number">Miktar</th>
                <th className="table__number">Sonraki bakiye</th>
                <th>Belge / ilgili</th>
                <th>Kullanıcı</th>
              </tr>
            </thead>
            <tbody>
              {data.items.map((transaction) => {
                const presentation = transactionTypePresentation[transaction.type]
                const related = transaction.purchaseOrderNo ?? transaction.orderNo ?? transaction.shipmentNo ?? transaction.bundleNo
                return (
                  <tr key={transaction.id}>
                    <td>{formatDateTimeShort(transaction.occurredAt)}</td>
                    <td><span className={`badge badge--${presentation.tone}`}>{presentation.label}</span></td>
                    <td>
                      <code>{transaction.locationCode}</code>
                      {transaction.counterWarehouseName && <span className="muted"> · {transaction.counterWarehouseName}</span>}
                    </td>
                    <td className="table__number">{formatSignedQuantity(transaction.quantity, transaction.unit)}</td>
                    <td className="table__number">
                      {formatStockQuantity(locationId === undefined ? transaction.balanceAfter : transaction.locationBalanceAfter, transaction.unit)}
                    </td>
                    <td>{[...new Set([transaction.documentNo, related].filter(Boolean))].join(' · ') || '—'}</td>
                    <td>{transaction.createdBy}</td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        )}
      </div>
    </div>
  )
}
