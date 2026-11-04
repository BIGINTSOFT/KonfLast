import { useState } from 'react'
import { Link } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDate, formatQuantity } from '../../utils/format'
import { formatMoney, formatRate } from '../../utils/money'
import { useGetLookupQuery } from '../masterData/masterDataApi'
import { PurchaseOrderDialog } from '../purchasing/PurchaseOrderDialog'
import type { PurchaseOrder } from '../purchasing/purchasingApi'
import { MovementDialog } from '../stock/MovementDialog'
import { formatStockQuantity } from '../stock/stockPresentation'
import {
  useGetOrderCostQuery,
  useGetOrderMaterialsQuery,
  useReleaseOrderMaterialsMutation,
  useReserveOrderMaterialsMutation,
} from './costingApi'
import type { OrderMaterialLine } from './costingApi'

/**
 * Sipariş sayfasındaki malzeme ihtiyacı: reçeteye göre gereken, düşülen, kalan ihtiyaç, siparişe ayrılan ve
 * depodaki stok. Malzeme sipariş onaylanınca kendiliğinden ayrılır; buradan yenilenebilir ya da bırakılabilir.
 * Stoğu yetmeyen malzeme için buradan stok girişi yapılabilir ya da eksik miktarla satın alma siparişi açılabilir.
 */
export function OrderMaterialsSection({ orderId, orderNo }: { orderId: number; orderNo?: string }) {
  // Tek hammadde deposu varsa sunucu onu kullanır; birden fazlaysa depo burada seçilir.
  const [warehouseId, setWarehouseId] = useState<number | undefined>(undefined)
  const { data, error } = useGetOrderMaterialsQuery({ orderId, warehouseId })
  const warehouses = useGetLookupQuery({ resource: 'warehouses', take: 100 })
  const canSeeStock = usePermission(Permissions.StockView)
  const canReserve = usePermission(Permissions.StockReserve)
  const canReceive = usePermission(Permissions.StockManage)
  // Satın alma siparişi açmak fiyatları görmeyi de gerektirir.
  const canViewPurchasePrices = usePermission(Permissions.PurchaseViewPrices)
  const canPurchase = usePermission(Permissions.PurchaseManage) && canViewPurchasePrices
  /** Eksik malzeme için açılan pencere: stok girişi (tek malzeme) ya da satın alma siparişi (bir ya da tüm eksikler). */
  const [receiving, setReceiving] = useState<OrderMaterialLine | null>(null)
  const [purchasing, setPurchasing] = useState<OrderMaterialLine[] | null>(null)
  const [purchased, setPurchased] = useState<PurchaseOrder | null>(null)
  const [reserve, reserveState] = useReserveOrderMaterialsMutation()
  const [release, releaseState] = useReleaseOrderMaterialsMutation()
  const busy = reserveState.isLoading || releaseState.isLoading

  if (!data && !error) {
    return null
  }

  const short = data?.lines.filter((line) => line.shortage > 0) ?? []
  const shortages = short.length
  const source = orderNo ? `${orderNo} iş emrinin eksik malzemesi` : 'İş emrinin eksik malzemesi'
  const hasReservation = data?.lines.some((line) => line.reserved > 0) ?? false
  const hasNeed = data?.lines.some((line) => line.remainingNeed > 0) ?? false
  const warehouseKnown = data?.warehouseId != null
  const pickListQuery = warehouseId === undefined ? '' : `?warehouseId=${warehouseId}`

  return (
    <>
      <header className="page__header section-title">
        <h2>Malzeme ihtiyacı</h2>
        {data && data.lines.length > 0 && (
          <div className="page__actions">
            {(warehouseId !== undefined || !warehouseKnown) && (
              <select
                aria-label="Malzeme deposu"
                value={warehouseId ?? ''}
                onChange={(event) => setWarehouseId(event.target.value ? Number(event.target.value) : undefined)}
              >
                <option value="">Depo seçin…</option>
                {warehouses.data?.map((warehouse) => (
                  <option key={warehouse.id} value={warehouse.id}>{warehouse.code} — {warehouse.name}</option>
                ))}
              </select>
            )}
            {canReserve && hasNeed && (
              <button
                type="button"
                className="button"
                disabled={busy || !warehouseKnown}
                title={warehouseKnown ? undefined : 'Önce malzemenin ayrılacağı depoyu seçin.'}
                onClick={() => void reserve({ orderId, warehouseId })}
              >
                {hasReservation ? 'Rezervi yenile' : 'Malzemeyi ayır'}
              </button>
            )}
            {canReserve && hasReservation && (
              <button type="button" className="button" disabled={busy} onClick={() => void release({ orderId, warehouseId })}>
                Rezervi bırak
              </button>
            )}
            {hasNeed && warehouseKnown && (
              <Link to={`/production-orders/${orderId}/pick-list${pickListQuery}`} className="button">Toplama listesi</Link>
            )}
            {canSeeStock && <Link to="/stock" className="button">Stoğu aç</Link>}
          </div>
        )}
      </header>
      <ErrorAlert error={error ?? reserveState.error ?? releaseState.error} />

      {data && !data.hasRecipe && data.lines.length === 0 && (
        <p className="muted">
          Bu ürünün reçetesi yok; demet açılırken stok düşülmez. Reçete, Ürünler ekranında ürün kaydının içinde tanımlanır.
        </p>
      )}

      {data && data.lines.length > 0 && (
        <>
          {shortages > 0 && (
            <div className="alert alert--error alert--with-action" role="alert">
              <span>
                {shortages} malzemede kalan ihtiyaç için stok yetersiz; eksik tamamlanmadan o demetler açılamaz.
                {(canReceive || canPurchase) && ' Eksik satırdan stok girişi yapabilir ya da satın alma siparişi açabilirsiniz.'}
              </span>
              {canPurchase && shortages > 1 && (
                <button type="button" className="button" onClick={() => setPurchasing(short)}>
                  Eksiklerin tümü için satın alma siparişi
                </button>
              )}
            </div>
          )}
          {purchased && (
            <p className="alert alert--info" role="status">
              <Link to={`/purchase-orders/${purchased.id}`}>{purchased.orderNo}</Link> numaralı satın alma siparişi taslak
              olarak oluşturuldu. Onaylanıp mal kabulü yapılınca stok artar ve eksik kapanır.
            </p>
          )}
          <p className="muted">
            Stok: {data.warehouseName ?? 'tüm depoların toplamı (birden fazla hammadde deposu var; malzeme ayırmak ve toplama listesi için depo seçin)'}.
          </p>
          <div className="table-wrap">
            <table className="table table--compact">
              <thead>
                <tr>
                  <th>Malzeme</th>
                  <th className="table__number">Sipariş için gereken</th>
                  <th className="table__number">Düşülen</th>
                  <th className="table__number">Kalan ihtiyaç</th>
                  <th className="table__number">Siparişe ayrılan</th>
                  <th className="table__number">Depoda</th>
                  <th>Durum</th>
                  <th aria-label="İşlemler" />
                </tr>
              </thead>
              <tbody>
                {data.lines.map((line) => {
                  const covered = line.shortage > 0 && line.onOrder >= line.shortage
                  const otherStocks = (line.stocks ?? []).filter((stock) => stock.warehouseId !== data.warehouseId)
                  return (
                    <tr key={line.stockItemId}>
                      <td><code>{line.itemCode}</code> {line.itemName}</td>
                      <td className="table__number">{formatStockQuantity(line.required, line.unit)}</td>
                      <td className="table__number">{formatStockQuantity(line.issued, line.unit)}</td>
                      <td className="table__number">{formatStockQuantity(line.remainingNeed, line.unit)}</td>
                      <td className="table__number">{formatStockQuantity(line.reserved, line.unit)}</td>
                      <td className="table__number">
                        {formatStockQuantity(line.available, line.unit)}
                        {line.reservedByOthers > 0 && (
                          <div className="muted">{formatStockQuantity(line.reservedByOthers, line.unit)} başka siparişlere ayrılı</div>
                        )}
                        {/* Malzemenin öteki depolardaki stoğu: seçili depoda yetmiyorsa nereden getirileceği görülür. */}
                        {data.warehouseId != null && otherStocks.length > 0 && (
                          <ul className="material-stocks" aria-label="Diğer depolardaki stok">
                            {otherStocks.map((stock) => (
                              <li key={stock.warehouseId} title={stock.warehouseName}>
                                {stock.warehouseCode}: {formatStockQuantity(stock.onHand, line.unit)}
                                {stock.reserved > 0 && ` (${formatQuantity(stock.reserved)} ayrılı)`}
                              </li>
                            ))}
                          </ul>
                        )}
                        {data.warehouseId == null && (line.stocks ?? []).length > 0 && (
                          <ul className="material-stocks" aria-label="Depolardaki stok">
                            {line.stocks!.map((stock) => (
                              <li key={stock.warehouseId} title={stock.warehouseName}>
                                {stock.warehouseCode}: {formatStockQuantity(stock.onHand, line.unit)}
                              </li>
                            ))}
                          </ul>
                        )}
                      </td>
                      <td>
                        {line.shortage > 0 ? (
                          <span className={covered ? 'badge badge--info' : 'badge badge--danger'}>
                            <span aria-hidden="true">{covered ? '…' : '!'}</span> {formatStockQuantity(line.shortage, line.unit)} eksik
                          </span>
                        ) : line.remainingNeed === 0 ? (
                          <span className="badge badge--neutral"><span aria-hidden="true">✓</span> Tamamı düşüldü</span>
                        ) : (
                          <span className="badge badge--ok"><span aria-hidden="true">✓</span> Yeterli</span>
                        )}
                        {(line.purchases ?? []).map((purchase) => (
                          <div key={purchase.purchaseOrderId} className="material-purchase">
                            Siparişte: <Link to={`/purchase-orders/${purchase.purchaseOrderId}`}>{purchase.orderNo}</Link>
                            {' · '}{formatStockQuantity(purchase.remaining, line.unit)}
                            {purchase.expectedDate && ` · beklenen ${formatDate(purchase.expectedDate)}`}
                            {purchase.status === 'DRAFT' && <span className="muted"> (taslak)</span>}
                          </div>
                        ))}
                      </td>
                      <td className="table__actions material-actions">
                        {line.shortage > 0 && canReceive && (
                          <button type="button" className="button" onClick={() => setReceiving(line)}>Stok girişi</button>
                        )}
                        {line.shortage > 0 && canPurchase && (
                          <button
                            type="button"
                            className="button"
                            title={covered ? 'Eksiğin tamamı için açık satın alma siparişi var.' : undefined}
                            onClick={() => setPurchasing([line])}
                          >
                            {covered || line.onOrder > 0 ? 'Ek satın alma' : 'Satın alma siparişi'}
                          </button>
                        )}
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
        </>
      )}

      {receiving && data && (
        <MovementDialog
          kind="receipt"
          preset={{ warehouseId: data.warehouseId, stockItemId: receiving.stockItemId, quantity: receiving.shortage, note: source }}
          onClose={() => setReceiving(null)}
        />
      )}
      {purchasing && (
        <PurchaseOrderDialog
          initialItems={purchasing.map((line) => ({
            stockItemId: line.stockItemId,
            // Açık siparişte olan kısım yeniden istenmez.
            quantity: Math.max(line.shortage - line.onOrder, 0) || line.shortage,
            productionOrderId: orderId,
            productionOrderNo: orderNo,
          }))}
          initialNotes={source}
          onClose={() => setPurchasing(null)}
          onSaved={setPurchased}
        />
      )}
    </>
  )
}

/** Sipariş sayfasındaki maliyet özeti; yalnızca maliyet görme yetkisi olan kullanıcıya gösterilir. */
export function OrderCostSection({ orderId }: { orderId: number }) {
  const canView = usePermission(Permissions.CostingView)
  const { data, error } = useGetOrderCostQuery(orderId, { skip: !canView })

  if (!canView || (!data && !error)) {
    return null
  }

  return (
    <>
      <h2 className="section-title">Maliyet</h2>
      <ErrorAlert error={error} />
      {data && (
        <>
          {(data.hasUncostedMaterials || data.hasUnpricedLabor) && (
            <p className="alert alert--info">
              {data.hasUncostedMaterials && 'Birim maliyeti bilinmeyen malzeme var; malzeme maliyeti eksik hesaplandı. '}
              {data.hasUnpricedLabor && 'Ücreti tanımlı olmayan operasyon var; işçilik maliyeti eksik hesaplandı.'}
            </p>
          )}
          <div className="cards">
            <article className="card">
              <h3>Gerçekleşen</h3>
              <dl className="status-list">
                <div><dt>Malzeme</dt><dd>{formatMoney(data.materialCost, data.currency)}</dd></div>
                <div><dt>İşçilik (parça başı)</dt><dd>{formatMoney(data.laborCost, data.currency)}</dd></div>
                <div><dt>Toplam</dt><dd><strong>{formatMoney(data.totalCost, data.currency)}</strong></dd></div>
                <div>
                  <dt>Depoya giren adet</dt>
                  <dd>{formatQuantity(data.finishedQuantity)} / {formatQuantity(data.orderedQuantity)}</dd>
                </div>
                <div>
                  <dt>Depoya giren parça başına</dt>
                  <dd>{formatRate(data.costPerFinishedPiece, data.currency)}</dd>
                </div>
              </dl>
            </article>
            <article className="card">
              <h3>Standart (parça başına)</h3>
              <dl className="status-list">
                <div><dt>Malzeme (reçete × ortalama maliyet)</dt><dd>{formatRate(data.standardMaterialCostPerPiece, data.currency)}</dd></div>
                <div><dt>İşçilik (rota × birim ücret)</dt><dd>{formatRate(data.standardLaborCostPerPiece, data.currency)}</dd></div>
                <div><dt>Toplam</dt><dd><strong>{formatRate(data.standardCostPerPiece, data.currency)}</strong></dd></div>
                <div>
                  <dt>Sipariş için tahmini</dt>
                  <dd>
                    {data.standardCostPerPiece === null
                      ? '—'
                      : formatMoney(data.standardCostPerPiece * data.orderedQuantity, data.currency)}
                  </dd>
                </div>
              </dl>
            </article>
          </div>
          <p className="muted">
            Gerçekleşen malzeme maliyeti, demetler açılırken düşülen miktarın o andaki ortalama maliyetidir; işçilik, üretilen
            sağlam adet ile demette sabitlenen birim ücretin çarpımıdır. Verim primi dahil değildir.
          </p>
        </>
      )}
    </>
  )
}
