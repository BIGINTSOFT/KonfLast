import { Pencil, Save } from 'lucide-react'
import { useState } from 'react'
import { ErrorAlert } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { formatDateTime, formatQuantity } from '../../utils/format'
import { useGetProductionChangesQuery, useGetSalesOrderReservationsQuery, useSetSalesOrderReservationsMutation } from './salesOrdersApi'

/**
 * Siparişin karşılanması: kalem başına sevk edilen, depoda ayrılan hazır mamul, üretimdeki ve planlamayı bekleyen
 * adet. Onaylı siparişte ayırma buradan düzenlenir.
 */
export function CoverageSection({ salesOrderId, canManage }: { salesOrderId: number; canManage: boolean }) {
  const { data, error, isLoading } = useGetSalesOrderReservationsQuery(salesOrderId)
  const [save, saving] = useSetSalesOrderReservationsMutation()
  const [editing, setEditing] = useState(false)
  const [draft, setDraft] = useState<Record<number, string>>({})
  const [message, setMessage] = useState<string | null>(null)

  if (!data) {
    return isLoading ? <p role="status">Yükleniyor…</p> : <ErrorAlert error={error} />
  }

  const editable = data.canEdit && canManage
  const startEdit = () => {
    setDraft(Object.fromEntries(data.lines.map((line) => [line.lineId, String(line.reserved)])))
    setMessage(null)
    setEditing(true)
  }

  const handleSave = async () => {
    const lines = data.lines
      .filter((line) => line.hasStockItem && draft[line.lineId] !== undefined && Number(draft[line.lineId]) !== line.reserved)
      .map((line) => ({ lineId: line.lineId, quantity: Math.max(0, Math.floor(Number(draft[line.lineId]) || 0)) }))
    const result = await save({ id: salesOrderId, lines })
    if ('data' in result && result.data) {
      const short = lines.filter((request) => (result.data.lines.find((l) => l.lineId === request.lineId)?.reserved ?? 0) < request.quantity)
      setMessage(short.length > 0
        ? 'Bazı kalemlerde serbest stok istenen kadar değildi; ayrılabilen kadar ayrıldı, kalanı planlamaya düştü.'
        : 'Ayırma kaydedildi.')
      setEditing(false)
    }
  }

  return (
    <>
      <header className="section-title coverage__head">
        <h2>Karşılama</h2>
        <span className="muted coverage__hint">Stoktan ayrılmayan kısım Üretim Planı ekranında planlamaya düşer.</span>
        {editable && !editing && (
          <button type="button" className="button" onClick={startEdit}><Pencil aria-hidden="true" />Ayırmayı düzenle</button>
        )}
        {editing && (
          <>
            <button type="button" className="button" onClick={() => setEditing(false)}>Vazgeç</button>
            <button type="button" className="button button--primary" disabled={saving.isLoading} onClick={() => void handleSave()}>
              <Save aria-hidden="true" />Kaydet
            </button>
          </>
        )}
      </header>
      {message && <p className="alert alert--info" role="status">{message}</p>}
      <ErrorAlert error={saving.error} />
      <div className="table-wrap">
        <table className="table table--compact">
          <thead>
            <tr>
              <th>Ürün</th>
              <th>Renk</th>
              <th>Beden</th>
              <th className="table__number">Sipariş</th>
              <th className="table__number">Sevk edilen</th>
              <th className="table__number">Ayrılan</th>
              <th className="table__number">Üretimde</th>
              <th className="table__number">Planlanacak</th>
              <th className="table__number">Serbest stok</th>
            </tr>
          </thead>
          <tbody>
            {data.lines.map((line) => (
              <tr key={line.lineId}>
                <td><code>{line.productCode}</code></td>
                <td>{line.colorName}</td>
                <td>{line.sizeName}</td>
                <td className="table__number">{formatQuantity(line.ordered)}</td>
                <td className="table__number">{formatQuantity(line.shipped)}</td>
                <td className="table__number">
                  {editing && line.hasStockItem ? (
                    <input
                      className="matrix-input coverage__input"
                      type="number"
                      min={0}
                      step={1}
                      aria-label={`${line.productCode} ${line.colorName} ${line.sizeName} ayrılan adet`}
                      value={draft[line.lineId] ?? ''}
                      onChange={(event) => setDraft({ ...draft, [line.lineId]: event.target.value })}
                    />
                  ) : (
                    <>
                      {formatQuantity(line.reserved)}
                      {line.shippedFromReservation > 0 && (
                        <div className="muted coverage__sub">+{formatQuantity(line.shippedFromReservation)} ayırmadan sevk</div>
                      )}
                    </>
                  )}
                </td>
                <td className="table__number">{formatQuantity(line.inProduction)}</td>
                <td className="table__number">{line.toPlan > 0 ? <strong>{formatQuantity(line.toPlan)}</strong> : <span className="muted">—</span>}</td>
                <td className="table__number">
                  {line.hasStockItem ? formatQuantity(line.available) : <span className="muted" title="Bu ürün / renk / beden için mamul stok kartı yok">Kart yok</span>}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </>
  )
}

const actionLabels = { CREATED: 'ekledi', UPDATED: 'değiştirdi', DELETED: 'sildi' } as const

/** Üretim siparişinin sipariş adedinden farkı ve üretim kalemlerinde kimin neyi değiştirdiği. */
export function ProductionChangesDialog({ salesOrderId, productionOrderId, onClose }: {
  salesOrderId: number
  productionOrderId: number
  onClose: () => void
}) {
  const { data, error, isLoading } = useGetProductionChangesQuery(salesOrderId)
  const order = data?.find((item) => item.productionOrderId === productionOrderId)

  return (
    <Modal title={`Sipariş adedinden fark${order ? ` · ${order.orderNo}` : ''}`} onClose={onClose} variant="drawer">
      <div className="form">
        {isLoading && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={error} />
        {order && (
          <>
            <h3 className="section-title">Renk / beden farkları</h3>
            <div className="table-wrap">
              <table className="table table--compact">
                <thead>
                  <tr>
                    <th>Renk</th>
                    <th>Beden</th>
                    <th className="table__number">Sipariş</th>
                    <th className="table__number">Üretim</th>
                    <th className="table__number">Fark</th>
                  </tr>
                </thead>
                <tbody>
                  {order.items.map((item) => (
                    <tr key={`${item.colorName}-${item.sizeName}`} className={item.difference !== 0 ? 'coverage__diff' : undefined}>
                      <td>{item.colorName}</td>
                      <td>{item.sizeName}</td>
                      <td className="table__number">{formatQuantity(item.ordered)}</td>
                      <td className="table__number">{formatQuantity(item.produced)}</td>
                      <td className="table__number">{item.difference === 0 ? '—' : `${item.difference > 0 ? '+' : ''}${item.difference}`}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <h3 className="section-title">Üretim kalemlerindeki değişiklikler</h3>
            {order.changes.length === 0 ? (
              <p className="muted">Kayıt yok.</p>
            ) : (
              <ul className="approval-events">
                {order.changes.map((change, index) => (
                  <li key={`${change.at}-${index}`} className="coverage__change">
                    <strong>{change.userName}</strong> {change.colorName} / {change.sizeName} kalemini {actionLabels[change.action]}
                    {change.action === 'UPDATED' && `: ${change.oldQuantity ?? '?'} → ${change.newQuantity ?? '?'}`}
                    {change.action === 'CREATED' && `: ${change.newQuantity ?? '?'} adet`}
                    {change.action === 'DELETED' && ` (${change.oldQuantity ?? '?'} adet)`}
                    <div className="approval-events__time">{formatDateTime(change.at)}</div>
                  </li>
                ))}
              </ul>
            )}
            <p className="muted form__note">
              Sipariş adedinden fazla üretilen adet siparişe ayrılmaz; depoya girdikten sonra gerekirse Karşılama bölümünden
              elle ayrılır.
            </p>
          </>
        )}
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Kapat</button>
        </footer>
      </div>
    </Modal>
  )
}
