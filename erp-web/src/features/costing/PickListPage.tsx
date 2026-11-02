import { Printer } from 'lucide-react'
import { Link, useParams, useSearchParams } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { useGetLookupQuery } from '../masterData/masterDataApi'
import { formatStockQuantity } from '../stock/stockPresentation'
import { useGetPickListQuery } from './costingApi'

/**
 * Toplama listesi: siparişin kalan malzeme ihtiyacı için hangi adresten, hangi toptan ne kadar alınacağı.
 * Sıra, demet açılırken yapılan otomatik stok çıkışıyla aynıdır (önce seçilen depo, sonra öteki hammadde depoları);
 * liste yazdırılıp depoda kullanılır.
 */
export function PickListPage() {
  const orderId = Number(useParams().id)
  // Tek hammadde deposu varsa sunucu onu kullanır; birden fazlaysa depo adresten ya da buradan seçilir.
  const [searchParams, setSearchParams] = useSearchParams()
  const warehouseId = Number(searchParams.get('warehouseId')) || undefined
  const warehouses = useGetLookupQuery({ resource: 'warehouses', take: 100 })
  const { data, error, isLoading } = useGetPickListQuery({ orderId, warehouseId }, { skip: !Number.isFinite(orderId) })

  const shortages = data?.items.filter((item) => item.shortage > 0).length ?? 0

  return (
    <section className="page">
      <p className="no-print"><Link to={`/production-orders/${orderId}`}>← Siparişe dön</Link></p>
      <header className="page__header">
        <div>
          <h1>Toplama listesi{data ? `: ${data.orderNo}` : ''}</h1>
          {data && (
            <p className="muted">
              <code>{data.productCode}</code> {data.productName} · Depo: {data.warehouseCode} — {data.warehouseName}
            </p>
          )}
        </div>
        <div className="page__actions">
          <select
            aria-label="Depo"
            value={warehouseId ?? data?.warehouseId ?? ''}
            onChange={(event) => setSearchParams(event.target.value ? { warehouseId: event.target.value } : {})}
          >
            <option value="">Depo seçin…</option>
            {warehouses.data?.map((warehouse) => (
              <option key={warehouse.id} value={warehouse.id}>{warehouse.code} — {warehouse.name}</option>
            ))}
          </select>
          <button type="button" className="button button--primary" disabled={!data || data.items.length === 0} onClick={() => window.print()}>
            <Printer aria-hidden="true" />
            Yazdır
          </button>
        </div>
      </header>

      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />

      {data && data.items.length === 0 && (
        <p className="muted">Toplanacak malzeme yok: siparişin kalan ihtiyacı bulunmuyor ya da ürünün reçetesi tanımlı değil.</p>
      )}

      {data && data.items.length > 0 && (
        <>
          {shortages > 0 && (
            <p className="alert alert--error" role="alert">
              {shortages} malzemede hammadde depolarındaki kullanılabilir stok ihtiyacı karşılamıyor; eksik miktar listede belirtilmiştir.
            </p>
          )}
          <div className="table-wrap">
            <table className="table table--compact">
              <thead>
                <tr>
                  <th>Malzeme</th>
                  <th>Depo</th>
                  <th>Adres</th>
                  <th>Top</th>
                  <th>Parti / ton</th>
                  <th className="table__number">Alınacak</th>
                  <th className="table__number">Adreste mevcut</th>
                  <th>Alındı</th>
                </tr>
              </thead>
              <tbody>
                {data.items.flatMap((item) => [
                  ...item.lines.map((line, index) => (
                    <tr key={`${item.stockItemId}-${line.warehouseId}-${line.locationCode}-${line.lotNo ?? ''}`}>
                      <td>
                        {index === 0 && (
                          <>
                            <code>{item.itemCode}</code> {item.itemName}
                            <div className="muted">İhtiyaç {formatStockQuantity(item.need, item.unit)}</div>
                          </>
                        )}
                      </td>
                      <td title={line.warehouseName}>
                        {line.warehouseCode}
                        {line.warehouseId !== data.warehouseId && <div className="muted">öteki hammadde deposu</div>}
                      </td>
                      <td><code>{line.locationCode}</code></td>
                      <td>{line.lotNo ? <code>{line.lotNo}</code> : '—'}</td>
                      <td>{[line.batchNo, line.shade && `ton ${line.shade}`].filter(Boolean).join(' · ') || '—'}</td>
                      <td className="table__number"><strong>{formatStockQuantity(line.quantity, item.unit)}</strong></td>
                      <td className="table__number">{formatStockQuantity(line.onHand, item.unit)}</td>
                      <td>☐</td>
                    </tr>
                  )),
                  ...(item.shortage > 0
                    ? [
                        <tr key={`${item.stockItemId}-shortage`}>
                          <td>
                            {item.lines.length === 0 && (
                              <>
                                <code>{item.itemCode}</code> {item.itemName}
                                <div className="muted">İhtiyaç {formatStockQuantity(item.need, item.unit)}</div>
                              </>
                            )}
                          </td>
                          <td colSpan={7}>
                            <span className="badge badge--danger">
                              <span aria-hidden="true">!</span> {formatStockQuantity(item.shortage, item.unit)} eksik
                            </span>
                          </td>
                        </tr>,
                      ]
                    : []),
                ])}
              </tbody>
            </table>
          </div>
        </>
      )}
    </section>
  )
}
