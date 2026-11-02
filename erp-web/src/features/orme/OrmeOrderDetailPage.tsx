import { Check, Pencil, Printer, RotateCcw, Trash2, XCircle } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDate, formatQuantity, todayIsoDate } from '../../utils/format'
import { formatMoney } from '../../utils/money'
import { ormeKnitOrderStatusPresentation, useCreateOrmeKnitOrderMutation, useGetOrmeKnitOrdersQuery } from './ormeKnitApi'
import { OrmeOrderFormDialog } from './OrmeOrderFormDialog'
import {
  ormeOrderOwnershipLabels,
  ormeOrderStatusPresentation,
  useCancelOrmeOrderMutation,
  useConfirmOrmeOrderMutation,
  useDeleteOrmeOrderMutation,
  useGetOrmeOrderQuery,
  useGetOrmeOrderRequirementsQuery,
  useGetOrmeOrderWasteQuery,
  useReopenOrmeOrderMutation,
} from './ormeOrdersApi'
import type { OrmeOrderLine } from './ormeOrdersApi'

/** Sipariş satırından örme emri açar; plan kilosu satırda emri açılmamış kısım olarak önerilir. */
function OpenKnitOrderDialog({ line, onClose }: { line: OrmeOrderLine; onClose: () => void }) {
  const navigate = useNavigate()
  const [create, { isLoading, error }] = useCreateOrmeKnitOrderMutation()
  const [plannedDate, setPlannedDate] = useState(todayIsoDate())
  const [kilograms, setKilograms] = useState(String(Math.max(line.kilograms - line.plannedKilograms, 0) || line.kilograms))
  const [notes, setNotes] = useState('')

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await create({ orderLineId: line.id, plannedDate, plannedKilograms: Number(kilograms), notes: notes.trim() || null })
    if ('data' in result && result.data) {
      navigate(`/orme/knit-orders/${result.data.id}`)
    }
  }

  return (
    <Modal title={`Örme emri: ${line.fabricCode}`} onClose={onClose}>
      <form className="form" onSubmit={handleSubmit}>
        <p className="muted">Sipariş satırı {formatQuantity(line.kilograms)} kg; açılan emirlerin planı {formatQuantity(line.plannedKilograms)} kg.</p>
        <div className="form__pair">
          <label className="field"><span>Plan tarihi</span><input type="date" required value={plannedDate} onChange={(event) => setPlannedDate(event.target.value)} /></label>
          <label className="field"><span>Plan kilosu</span><input type="number" min="0" step="0.01" required value={kilograms} onChange={(event) => setKilograms(event.target.value)} /></label>
        </div>
        <label className="field"><span>Not</span><textarea rows={2} maxLength={1000} value={notes} onChange={(event) => setNotes(event.target.value)} /></label>
        <ErrorAlert error={error} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading}>Örme emri aç</button>
        </footer>
      </form>
    </Modal>
  )
}

/** Sipariş: başlık, satırlar, iplik ihtiyacı ve durum işlemleri. */
export function OrmeOrderDetailPage() {
  const id = Number(useParams().id)
  const navigate = useNavigate()
  const canManage = usePermission(Permissions.OrmeOrderManage)
  const canKnitView = usePermission(Permissions.OrmeKnittingView)
  const canKnit = usePermission(Permissions.OrmeKnittingManage)
  const { data: order, error, isLoading } = useGetOrmeOrderQuery(id)
  const requirements = useGetOrmeOrderRequirementsQuery(id)
  const waste = useGetOrmeOrderWasteQuery(id)
  const [confirm, confirmState] = useConfirmOrmeOrderMutation()
  const [cancel, cancelState] = useCancelOrmeOrderMutation()
  const [reopen, reopenState] = useReopenOrmeOrderMutation()
  const [remove, removeState] = useDeleteOrmeOrderMutation()
  const [editing, setEditing] = useState(false)
  const [pending, setPending] = useState<'cancel' | 'delete' | null>(null)
  const [knitLine, setKnitLine] = useState<OrmeOrderLine | null>(null)
  const knitOrders = useGetOrmeKnitOrdersQuery({ search: order?.orderNo, pageSize: 100 }, { skip: !order || !canKnitView })

  const handleDelete = async () => {
    const result = await remove(id)
    if (!('error' in result)) {
      navigate('/orme/orders')
    }
  }

  const status = order && ormeOrderStatusPresentation[order.status]
  const draft = order?.status === 'DRAFT'
  // Proforma yalnız müşterili, taslak ya da onaylı siparişte; görme yetkisi yeterlidir.
  const canProforma = order !== undefined && order.customerId !== null && order.status !== 'CANCELLED'
  const needs = requirements.data

  return (
    <section className="page">
      <p><Link to="/orme/orders">← Siparişlere dön</Link></p>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error ?? confirmState.error ?? cancelState.error ?? removeState.error ?? reopenState.error} />
      {order && status && (
        <>
          <header className="page__header">
            <div>
              <h1>{order.orderNo} <span className={`badge badge--${status.tone}`}>{status.label}</span></h1>
              <p className="muted">
                {formatDate(order.orderDate)} · {order.customerName ?? 'Stoğa üretim'} · {ormeOrderOwnershipLabels[order.ownership]}
                {order.customerOrderNo && ` · Müşteri sipariş no ${order.customerOrderNo}`}
                {order.dueDate && ` · Termin ${formatDate(order.dueDate)}`}
              </p>
              {order.notes && <p>{order.notes}</p>}
            </div>
            {canProforma && !canManage && (
              <div className="page__actions">
                <Link className="button" to={`/orme/orders/${id}/proforma`}><Printer aria-hidden="true" />Proforma / Teyit</Link>
              </div>
            )}
            {canManage && (
              <div className="page__actions">
                {canProforma && <Link className="button" to={`/orme/orders/${id}/proforma`}><Printer aria-hidden="true" />Proforma / Teyit</Link>}
                {draft && <button type="button" className="button" onClick={() => setEditing(true)}><Pencil aria-hidden="true" />Düzenle</button>}
                {draft && <button type="button" className="button button--primary" disabled={confirmState.isLoading} onClick={() => confirm(id)}><Check aria-hidden="true" />Onayla</button>}
                {draft && pending !== 'delete' && <button type="button" className="button button--danger-ghost" onClick={() => setPending('delete')}><Trash2 aria-hidden="true" />Sil</button>}
                {pending === 'delete' && <button type="button" className="button button--danger-ghost" disabled={removeState.isLoading} onClick={handleDelete}>Silmeyi onayla</button>}
                {order.status === 'CONFIRMED' && <button type="button" className="button" disabled={reopenState.isLoading} onClick={() => reopen(id)}><RotateCcw aria-hidden="true" />Taslağa geri al</button>}
                {order.status === 'CONFIRMED' && pending !== 'cancel' && <button type="button" className="button button--danger-ghost" onClick={() => setPending('cancel')}><XCircle aria-hidden="true" />İptal et</button>}
                {pending === 'cancel' && order.status === 'CONFIRMED' && <button type="button" className="button button--danger-ghost" disabled={cancelState.isLoading} onClick={() => cancel(id)}>İptali onayla</button>}
              </div>
            )}
          </header>

          <div className="table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <th>Kumaş</th>
                  <th className="table__number">Kilo</th>
                  <th className="table__number">Tahmini metre</th>
                  <th className="table__number">Birim fiyat (kilo)</th>
                  <th className="table__number">Tahmini tutar</th>
                  <th className="table__number">Emir planı (kg)</th>
                  <th className="table__number">Örülen (kg)</th>
                  {canKnit && order.status === 'CONFIRMED' && <th aria-label="İşlem" />}
                </tr>
              </thead>
              <tbody>
                {order.lines.map((line) => (
                  <tr key={line.id}>
                    <td>
                      {line.fabricCode} — {line.fabricName}
                      {line.customerFabricCode && <div className="muted">Müşteri kodu: {line.customerFabricCode}</div>}
                      {line.notes && <div className="muted">{line.notes}</div>}
                    </td>
                    <td className="table__number">{formatQuantity(line.kilograms)}</td>
                    <td className="table__number">{line.estimatedMeters !== null ? formatQuantity(line.estimatedMeters) : '—'}</td>
                    <td className="table__number">{line.unitPrice !== null ? formatMoney(line.unitPrice, order.currency) : '—'}</td>
                    <td className="table__number">{line.amount !== null ? formatMoney(line.amount, order.currency) : '—'}</td>
                    <td className="table__number">{formatQuantity(line.plannedKilograms)}</td>
                    <td className="table__number">{formatQuantity(line.knittedKilograms)}</td>
                    {canKnit && order.status === 'CONFIRMED' && (
                      <td className="table__actions"><button type="button" className="button" onClick={() => setKnitLine(line)}>Örme emri aç</button></td>
                    )}
                  </tr>
                ))}
              </tbody>
              <tfoot>
                <tr>
                  <th scope="row">Toplam</th>
                  <td className="table__number">{formatQuantity(order.totalKilograms)}</td>
                  <td />
                  <td />
                  <td className="table__number">{order.totalAmount > 0 ? formatMoney(order.totalAmount, order.currency) : '—'}</td>
                  <td />
                  <td />
                  {canKnit && order.status === 'CONFIRMED' && <td />}
                </tr>
              </tfoot>
            </table>
          </div>

          {canKnitView && knitOrders.data && knitOrders.data.items.length > 0 && (
            <>
              <h2 className="section-title">Örme emirleri</h2>
              <div className="table-wrap">
                <table className="table table--compact">
                  <thead>
                    <tr>
                      <th>Emir no</th>
                      <th>Plan tarihi</th>
                      <th>Kumaş</th>
                      <th className="table__number">Plan (kg)</th>
                      <th className="table__number">Örülen (kg)</th>
                      <th className="table__number">Top</th>
                      <th>Durum</th>
                    </tr>
                  </thead>
                  <tbody>
                    {knitOrders.data.items.map((knit) => (
                      <tr key={knit.id}>
                        <td><Link to={`/orme/knit-orders/${knit.id}`}><code>{knit.knitOrderNo}</code></Link></td>
                        <td>{formatDate(knit.plannedDate)}</td>
                        <td>{knit.fabricCode} — {knit.fabricName}</td>
                        <td className="table__number">{formatQuantity(knit.plannedKilograms)}</td>
                        <td className="table__number">{formatQuantity(knit.knittedKilograms)}</td>
                        <td className="table__number">{knit.rollCount}</td>
                        <td><span className={`badge badge--${ormeKnitOrderStatusPresentation[knit.status].tone}`}>{ormeKnitOrderStatusPresentation[knit.status].label}</span></td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </>
          )}

          <h2 className="section-title">İplik ihtiyacı</h2>
          <p className="muted">
            Kumaş kartındaki iplik oranlarından hesaplanır; gereken miktara kumaştaki fire eklenir. Eldeki miktar{' '}
            {order.ownership === 'COMMISSION' ? `${needs?.ownerName ?? 'müşteri'} adına girilen` : 'işletme malı'} lotların
            kalanıdır. Siparişe verilen iplik ihtiyaçtan düşer; aynı sahibin diğer onaylı siparişlerinin kalan ihtiyacı bilgi olarak düşülür.
          </p>
          <ErrorAlert error={requirements.error} />
          {needs && needs.missingFabrics.length > 0 && (
            <div className="alert alert--info">İplik tanımı eksik olduğu için hesaba katılmayan kumaş: {needs.missingFabrics.join(', ')}</div>
          )}
          {needs && (
            <div className="table-wrap">
              <table className="table table--compact">
                <thead>
                  <tr>
                    <th>İplik</th>
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
                      <td className="table__number">{formatQuantity(yarn.netKilograms)}</td>
                      <td className="table__number">{formatQuantity(yarn.requiredKilograms)}</td>
                      <td className="table__number">{formatQuantity(yarn.issuedKilograms)}</td>
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
                  {needs.yarns.length === 0 && <tr><td colSpan={8} className="table__empty">Hesaplanacak iplik yok.</td></tr>}
                </tbody>
              </table>
            </div>
          )}

          {waste.data && waste.data.yarns.some((yarn) => yarn.issuedKilograms > 0) && (
            <>
              <h2 className="section-title">Gerçekleşen iplik firesi</h2>
              <p className="muted">
                Verilen iplik ile örülen {formatQuantity(waste.data.knittedKilograms)} kg için hesaplanan net iplik karşılaştırılır.
                {!waste.data.allKnitOrdersCompleted && ' Örme sürdüğü için oran şimdilik yüksek görünür; emirler bitince kesinleşir.'}
              </p>
              {waste.data.exceedsTolerance && waste.data.tolerancePercent !== null && (
                <div className="alert alert--info">Toplam fire %{formatQuantity(waste.data.totalWastePercent)}, müşterinin kabul ettiği %{formatQuantity(waste.data.tolerancePercent)} oranını aşıyor.</div>
              )}
              <div className="table-wrap">
                <table className="table table--compact">
                  <thead>
                    <tr>
                      <th>İplik</th>
                      <th className="table__number">Verilen (kg)</th>
                      <th className="table__number">Örülene gereken (kg)</th>
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

          {knitLine && <OpenKnitOrderDialog line={knitLine} onClose={() => setKnitLine(null)} />}
          {editing && <OrmeOrderFormDialog order={order} onClose={() => setEditing(false)} onSaved={() => setEditing(false)} />}
        </>
      )}
    </section>
  )
}
