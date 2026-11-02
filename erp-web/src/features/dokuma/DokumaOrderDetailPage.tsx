import { Check, FileText, Pencil, RotateCcw, Trash2, XCircle } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDate, formatQuantity, todayIsoDate } from '../../utils/format'
import { formatMoney } from '../../utils/money'
import { DokumaOrderFormDialog } from './DokumaOrderFormDialog'
import { useGetDokumaOrderWasteQuery } from './dokumaShippingApi'
import { useCreateDokumaWarpOrderMutation } from './dokumaWarpApi'
import type { DokumaOrderLine } from './dokumaOrdersApi'
import {
  dokumaOrderOwnershipLabels,
  dokumaOrderStatusPresentation,
  useCancelDokumaOrderMutation,
  useConfirmDokumaOrderMutation,
  useDeleteDokumaOrderMutation,
  useGetDokumaOrderQuery,
  useGetDokumaOrderRequirementsQuery,
  useReopenDokumaOrderMutation,
} from './dokumaOrdersApi'

/** Sipariş satırından çözgü emri açar; levent sayısı satır metresine göre önerilir. */
function OpenWarpDialog({ line, onClose }: { line: DokumaOrderLine; onClose: () => void }) {
  const navigate = useNavigate()
  const [create, { isLoading, error }] = useCreateDokumaWarpOrderMutation()
  const [plannedDate, setPlannedDate] = useState(todayIsoDate())
  const [beamCount, setBeamCount] = useState('1')
  const [meters, setMeters] = useState(String(line.meters))
  const [notes, setNotes] = useState('')

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await create({ orderLineId: line.id, plannedDate, plannedBeamCount: Number(beamCount), metersPerBeam: Number(meters), notes: notes.trim() || null })
    if ('data' in result && result.data) {
      navigate(`/dokuma/warp-orders/${result.data.id}`)
    }
  }

  return (
    <Modal title={`Çözgü emri: ${line.articleCode}`} onClose={onClose}>
      <form className="form" onSubmit={handleSubmit}>
        <p className="muted">Sipariş satırı {formatQuantity(line.meters)} m. Levent metresi iplik boyudur; çekme ve fire payını ekleyin.</p>
        <label className="field"><span>Plan tarihi</span><input type="date" required value={plannedDate} onChange={(event) => setPlannedDate(event.target.value)} /></label>
        <div className="form__pair">
          <label className="field"><span>Levent sayısı</span><input type="number" min="1" step="1" required value={beamCount} onChange={(event) => setBeamCount(event.target.value)} /></label>
          <label className="field"><span>Levent metresi</span><input type="number" min="0" step="0.01" required value={meters} onChange={(event) => setMeters(event.target.value)} /></label>
        </div>
        <label className="field"><span>Not</span><textarea rows={2} maxLength={1000} value={notes} onChange={(event) => setNotes(event.target.value)} /></label>
        <ErrorAlert error={error} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading}>Çözgü emri aç</button>
        </footer>
      </form>
    </Modal>
  )
}

const yarnPlace = (warp: boolean, weft: boolean) => (warp && weft ? 'Çözgü, atkı' : warp ? 'Çözgü' : 'Atkı')

/** Sipariş: başlık, satırlar, iplik ihtiyacı ve durum işlemleri. */
export function DokumaOrderDetailPage() {
  const id = Number(useParams().id)
  const navigate = useNavigate()
  const canManage = usePermission(Permissions.DokumaOrderManage)
  const { data: order, error, isLoading } = useGetDokumaOrderQuery(id)
  const requirements = useGetDokumaOrderRequirementsQuery(id)
  const waste = useGetDokumaOrderWasteQuery(id)
  const [confirm, confirmState] = useConfirmDokumaOrderMutation()
  const [cancel, cancelState] = useCancelDokumaOrderMutation()
  const [reopen, reopenState] = useReopenDokumaOrderMutation()
  const [remove, removeState] = useDeleteDokumaOrderMutation()
  const [editing, setEditing] = useState(false)
  const [warpLine, setWarpLine] = useState<DokumaOrderLine | null>(null)
  const canWarp = usePermission(Permissions.DokumaWarpManage)
  const [pending, setPending] = useState<'cancel' | 'delete' | null>(null)

  const handleDelete = async () => {
    const result = await remove(id)
    if (!('error' in result)) {
      navigate('/dokuma/orders')
    }
  }

  const status = order && dokumaOrderStatusPresentation[order.status]
  const draft = order?.status === 'DRAFT'
  const needs = requirements.data

  return (
    <section className="page">
      <p><Link to="/dokuma/orders">← Siparişlere dön</Link></p>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error ?? confirmState.error ?? cancelState.error ?? removeState.error ?? reopenState.error} />
      {order && status && (
        <>
          <header className="page__header">
            <div>
              <h1>{order.orderNo} <span className={`badge badge--${status.tone}`}>{status.label}</span></h1>
              <p className="muted">
                {formatDate(order.orderDate)} · {order.customerName ?? 'Stoğa üretim'} · {dokumaOrderOwnershipLabels[order.ownership]}
                {order.customerOrderNo && ` · Müşteri sipariş no ${order.customerOrderNo}`}
                {order.dueDate && ` · Termin ${formatDate(order.dueDate)}`}
              </p>
              {order.notes && <p>{order.notes}</p>}
            </div>
            {(canManage || order.status !== 'CANCELLED') && (
              <div className="page__actions">
                {order.status !== 'CANCELLED' && (
                  <Link className="button" to={`/dokuma/orders/${id}/proforma`}><FileText aria-hidden="true" />Proforma / Teyit</Link>
                )}
                {canManage && draft && <button type="button" className="button" onClick={() => setEditing(true)}><Pencil aria-hidden="true" />Düzenle</button>}
                {canManage && draft && <button type="button" className="button button--primary" disabled={confirmState.isLoading} onClick={() => confirm(id)}><Check aria-hidden="true" />Onayla</button>}
                {canManage && draft && pending !== 'delete' && <button type="button" className="button button--danger-ghost" onClick={() => setPending('delete')}><Trash2 aria-hidden="true" />Sil</button>}
                {pending === 'delete' && <button type="button" className="button button--danger-ghost" disabled={removeState.isLoading} onClick={handleDelete}>Silmeyi onayla</button>}
                {canManage && order.status === 'CONFIRMED' && <button type="button" className="button" disabled={reopenState.isLoading} onClick={() => reopen(id)}><RotateCcw aria-hidden="true" />Taslağa geri al</button>}
                {canManage && order.status === 'CONFIRMED' && pending !== 'cancel' && <button type="button" className="button button--danger-ghost" onClick={() => setPending('cancel')}><XCircle aria-hidden="true" />İptal et</button>}
                {pending === 'cancel' && order.status === 'CONFIRMED' && <button type="button" className="button button--danger-ghost" disabled={cancelState.isLoading} onClick={() => cancel(id)}>İptali onayla</button>}
              </div>
            )}
          </header>

          <div className="table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <th>Artikel</th>
                  <th>Müşteri kodu</th>
                  <th className="table__number">Metre</th>
                  <th className="table__number">Birim fiyat (metre)</th>
                  <th className="table__number">Tahmini tutar</th>
                  {canWarp && order.status === 'CONFIRMED' && <th aria-label="İşlem" />}
                </tr>
              </thead>
              <tbody>
                {order.lines.map((line) => (
                  <tr key={line.id}>
                    <td>{line.articleCode} — {line.articleName}{line.notes && <div className="muted">{line.notes}</div>}</td>
                    <td>{line.customerArticleCode ?? '—'}</td>
                    <td className="table__number">{formatQuantity(line.meters)}</td>
                    <td className="table__number">{line.unitPrice !== null ? formatMoney(line.unitPrice, order.currency) : '—'}</td>
                    <td className="table__number">{line.amount !== null ? formatMoney(line.amount, order.currency) : '—'}</td>
                    {canWarp && order.status === 'CONFIRMED' && (
                      <td className="table__actions"><button type="button" className="button" onClick={() => setWarpLine(line)}>Çözgü emri aç</button></td>
                    )}
                  </tr>
                ))}
              </tbody>
              <tfoot>
                <tr>
                  <th scope="row" colSpan={2}>Toplam</th>
                  <td className="table__number">{formatQuantity(order.totalMeters)}</td>
                  <td />
                  <td className="table__number">{order.totalAmount > 0 ? formatMoney(order.totalAmount, order.currency) : '—'}</td>
                  {canWarp && order.status === 'CONFIRMED' && <td />}
                </tr>
              </tfoot>
            </table>
          </div>

          <h2>İplik ihtiyacı</h2>
          <p className="muted">
            Artikel kartındaki çözgü ve atkı bilgisinden hesaplanır; gereken miktara artikeldeki fire eklenir. Eldeki miktar{' '}
            {order.ownership === 'COMMISSION' ? `${needs?.ownerName ?? 'müşteri'} adına girilen` : 'işletme malı'} iplik girişlerinin
            toplamıdır. Aynı sahibin diğer onaylı siparişlerine gereken iplik bilgi olarak düşülür; kesin düşüm çözgüye iplik verilirken yapılır.
          </p>
          <ErrorAlert error={requirements.error} />
          {needs && needs.missingArticles.length > 0 && (
            <div className="alert alert--info">İplik tanımı eksik olduğu için hesaba katılmayan artikel: {needs.missingArticles.join(', ')}</div>
          )}
          {needs && (
            <div className="table-wrap">
              <table className="table table--compact">
                <thead>
                  <tr>
                    <th>İplik</th>
                    <th>Kullanım</th>
                    <th className="table__number">Net (kg)</th>
                    <th className="table__number">Fireli gereken (kg)</th>
                    <th className="table__number">Verilen (kg)</th>
                    <th className="table__number">Eldeki (kg)</th>
                    <th className="table__number">Diğer siparişlere (kg)</th>
                    <th className="table__number">Serbest (kg)</th>
                    <th className="table__number">Eksik (kg)</th>
                  </tr>
                </thead>
                <tbody>
                  {needs.yarns.map((yarn) => (
                    <tr key={yarn.yarnId}>
                      <td>{yarn.yarnCode} — {yarn.yarnName}{yarn.yarnColor && <span className="muted"> ({yarn.yarnColor})</span>}</td>
                      <td>{yarnPlace(yarn.usedInWarp, yarn.usedInWeft)}</td>
                      <td className="table__number">{formatQuantity(yarn.netKilograms)}</td>
                      <td className="table__number">{formatQuantity(yarn.requiredKilograms)}</td>
                      <td className="table__number">{yarn.issuedKilograms > 0 ? formatQuantity(yarn.issuedKilograms) : '—'}</td>
                      <td className="table__number">{formatQuantity(yarn.availableKilograms)}</td>
                      <td className="table__number">{yarn.otherOrdersKilograms > 0 ? formatQuantity(yarn.otherOrdersKilograms) : '—'}</td>
                      <td className="table__number">{formatQuantity(yarn.freeKilograms)}</td>
                      <td className="table__number">
                        {yarn.shortageKilograms > 0
                          ? <span className="badge badge--warning">{formatQuantity(yarn.shortageKilograms)}</span>
                          : '—'}
                      </td>
                    </tr>
                  ))}
                  {needs.yarns.length === 0 && <tr><td colSpan={9} className="table__empty">Hesaplanacak iplik yok.</td></tr>}
                </tbody>
              </table>
            </div>
          )}

          {waste.data && waste.data.yarns.some((yarn) => yarn.issuedKilograms > 0) && (
            <>
              <h2>Gerçek fire</h2>
              <p className="muted">
                Verilen iplik ile dokunan {formatQuantity(waste.data.wovenMeters)} m için hesaplanan net iplik karşılaştırılır.
                {!waste.data.allBeamsFinished && ' Dokuma sürdüğü için oran şimdilik yüksek görünür; leventler bitince kesinleşir.'}
                {' '}Verilip artan iplik de fire sayılır.
              </p>
              {waste.data.exceedsTolerance && waste.data.tolerancePercent !== null && (
                <div className="alert alert--info">Toplam fire %{formatQuantity(waste.data.totalWastePercent)}, müşterinin kabul ettiği %{formatQuantity(waste.data.tolerancePercent)} sınırını aşıyor.</div>
              )}
              <div className="table-wrap">
                <table className="table table--compact">
                  <thead>
                    <tr>
                      <th>İplik</th>
                      <th className="table__number">Verilen (kg)</th>
                      <th className="table__number">Net hesaplanan (kg)</th>
                      <th className="table__number">Fire (kg)</th>
                      <th className="table__number">Fire (%)</th>
                    </tr>
                  </thead>
                  <tbody>
                    {waste.data.yarns.map((yarn) => (
                      <tr key={yarn.yarnId}>
                        <td>{yarn.yarnCode} — {yarn.yarnName}</td>
                        <td className="table__number">{formatQuantity(yarn.issuedKilograms)}</td>
                        <td className="table__number">{formatQuantity(yarn.theoreticalKilograms)}</td>
                        <td className="table__number">{formatQuantity(yarn.wasteKilograms)}</td>
                        <td className="table__number">{formatQuantity(yarn.wastePercent)}</td>
                      </tr>
                    ))}
                  </tbody>
                  <tfoot>
                    <tr>
                      <th colSpan={4} scope="row">Toplam fire oranı{waste.data.tolerancePercent !== null && ` (kabul edilen %${formatQuantity(waste.data.tolerancePercent)})`}</th>
                      <td className="table__number">{formatQuantity(waste.data.totalWastePercent)}</td>
                    </tr>
                  </tfoot>
                </table>
              </div>
            </>
          )}

          {warpLine && <OpenWarpDialog line={warpLine} onClose={() => setWarpLine(null)} />}
          {editing && <DokumaOrderFormDialog order={order} onClose={() => setEditing(false)} onSaved={() => setEditing(false)} />}
        </>
      )}
    </section>
  )
}
