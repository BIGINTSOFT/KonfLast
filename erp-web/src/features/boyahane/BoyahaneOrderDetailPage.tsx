import { Archive, Check, FileText, Pencil, RotateCcw, Trash2, XCircle } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDate, formatDateTime, formatQuantity } from '../../utils/format'
import { formatMoney } from '../../utils/money'
import { BoyahaneOrderFormDialog } from './BoyahaneOrderFormDialog'
import {
  orderOwnershipLabels,
  orderProcessTypeLabels,
  orderStatusPresentation,
  orderUnitLabels,
  useAssignOrderLineRollsMutation,
  useAssignOrderLineYarnLotsMutation,
  useCancelBoyahaneOrderMutation,
  useCloseBoyahaneOrderMutation,
  useConfirmBoyahaneOrderMutation,
  useDeleteBoyahaneOrderMutation,
  useGetAssignableRollsQuery,
  useGetAssignableYarnLotsQuery,
  useGetBoyahaneOrderQuery,
  useReopenBoyahaneOrderMutation,
} from './boyahaneOrdersApi'
import type { BoyahaneOrderLine } from './boyahaneOrdersApi'
import { fabricRollStatusPresentation } from './boyahaneReceiptsApi'
import { OpenLabRequestDialog } from './OpenLabRequestDialog'

/** Satıra top bağlama: uygun toplar listelenir, seçim bütün olarak kaydedilir. */
function AssignRollsDialog({ orderId, line, onClose }: { orderId: number; line: BoyahaneOrderLine; onClose: () => void }) {
  const { data: rolls, error, isLoading } = useGetAssignableRollsQuery({ id: orderId, lineId: line.id })
  const [assign, { isLoading: saving, error: saveError }] = useAssignOrderLineRollsMutation()
  const [changes, setChanges] = useState<Map<number, boolean>>(new Map())

  const isSelected = (id: number, assigned: boolean) => changes.get(id) ?? assigned
  const selected = rolls?.filter((roll) => isSelected(roll.id, roll.assigned)) ?? []
  const toggle = (id: number, value: boolean) => setChanges(new Map(changes).set(id, value))

  const handleSave = async () => {
    const result = await assign({ id: orderId, lineId: line.id, rollIds: selected.map((roll) => roll.id) })
    if (!('error' in result)) {
      onClose()
    }
  }

  return (
    <Modal title={`Top bağla: ${line.itemCode}`} onClose={onClose} variant="drawer">
      <p className="muted">
        Yalnızca bu kumaş kartından, malın sahibi siparişle aynı olan, reddedilmemiş ve boştaki toplar listelenir.
        Sipariş miktarı: {formatQuantity(line.quantity)} {orderUnitLabels[line.unit]}.
      </p>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error ?? saveError} />
      {rolls && (
        <div className="table-wrap">
          <table className="table table--compact">
            <thead>
              <tr>
                <th aria-label="Seç" />
                <th>Top no</th>
                <th>Müşteri top no</th>
                <th className="table__number">Metre</th>
                <th className="table__number">Kilo</th>
                <th>Durum</th>
              </tr>
            </thead>
            <tbody>
              {rolls.map((roll) => {
                const status = fabricRollStatusPresentation[roll.status]
                return (
                  <tr key={roll.id}>
                    <td>
                      <input type="checkbox" aria-label={`${roll.rollNo} seç`} checked={isSelected(roll.id, roll.assigned)} onChange={(event) => toggle(roll.id, event.target.checked)} />
                    </td>
                    <td><code>{roll.rollNo}</code></td>
                    <td>{roll.customerRollNo ?? '—'}</td>
                    <td className="table__number">{formatQuantity(roll.meters)}</td>
                    <td className="table__number">{formatQuantity(roll.kilograms)}</td>
                    <td><span className={`badge badge--${status.tone}`}>{status.label}</span></td>
                  </tr>
                )
              })}
              {rolls.length === 0 && <tr><td colSpan={6} className="table__empty">Bağlanabilecek top yok.</td></tr>}
            </tbody>
            <tfoot>
              <tr>
                <th colSpan={3} scope="row">Seçilen {selected.length} top</th>
                <td className="table__number">{formatQuantity(selected.reduce((sum, roll) => sum + roll.meters, 0))}</td>
                <td className="table__number">{formatQuantity(selected.reduce((sum, roll) => sum + roll.kilograms, 0))}</td>
                <td />
              </tr>
            </tfoot>
          </table>
        </div>
      )}
      <footer className="form__actions">
        <button type="button" className="button" onClick={onClose}>Vazgeç</button>
        <button type="button" className="button button--primary" disabled={saving} onClick={handleSave}>Kaydet</button>
      </footer>
    </Modal>
  )
}

/** İplik satırına lot bağlama. */
function AssignYarnLotsDialog({ orderId, line, onClose }: { orderId: number; line: BoyahaneOrderLine; onClose: () => void }) {
  const { data: lots, error, isLoading } = useGetAssignableYarnLotsQuery({ id: orderId, lineId: line.id })
  const [assign, { isLoading: saving, error: saveError }] = useAssignOrderLineYarnLotsMutation()
  const [changes, setChanges] = useState<Map<number, boolean>>(new Map())

  const isSelected = (id: number, assigned: boolean) => changes.get(id) ?? assigned
  const selected = lots?.filter((lot) => isSelected(lot.id, lot.assigned)) ?? []

  const handleSave = async () => {
    const result = await assign({ id: orderId, lineId: line.id, lotIds: selected.map((lot) => lot.id) })
    if (!('error' in result)) {
      onClose()
    }
  }

  return (
    <Modal title={`Lot bağla: ${line.itemCode}`} onClose={onClose} variant="drawer">
      <p className="muted">
        Yalnızca bu iplik kartından, malın sahibi siparişle aynı olan ve boştaki lotlar listelenir.
        Sipariş miktarı: {formatQuantity(line.quantity)} kg.
      </p>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error ?? saveError} />
      {lots && (
        <div className="table-wrap">
          <table className="table table--compact">
            <thead>
              <tr>
                <th aria-label="Seç" />
                <th>Lot no</th>
                <th>Müşteri lot no</th>
                <th className="table__number">Adet</th>
                <th className="table__number">Kilo</th>
              </tr>
            </thead>
            <tbody>
              {lots.map((lot) => (
                <tr key={lot.id}>
                  <td>
                    <input type="checkbox" aria-label={`${lot.lotNo} seç`} checked={isSelected(lot.id, lot.assigned)}
                      onChange={(event) => setChanges(new Map(changes).set(lot.id, event.target.checked))} />
                  </td>
                  <td><code>{lot.lotNo}</code></td>
                  <td>{lot.customerLotNo ?? '—'}</td>
                  <td className="table__number">{formatQuantity(lot.packageCount)}</td>
                  <td className="table__number">{formatQuantity(lot.kilograms)}</td>
                </tr>
              ))}
              {lots.length === 0 && <tr><td colSpan={5} className="table__empty">Bağlanabilecek lot yok.</td></tr>}
            </tbody>
            <tfoot>
              <tr>
                <th colSpan={4} scope="row">Seçilen {selected.length} lot</th>
                <td className="table__number">{formatQuantity(selected.reduce((sum, lot) => sum + lot.kilograms, 0))}</td>
              </tr>
            </tfoot>
          </table>
        </div>
      )}
      <footer className="form__actions">
        <button type="button" className="button" onClick={onClose}>Vazgeç</button>
        <button type="button" className="button button--primary" disabled={saving} onClick={handleSave}>Kaydet</button>
      </footer>
    </Modal>
  )
}

/** Siparişi kapatma: isteğe bağlı not. Planlı ya da işlemdeki partisi olan sipariş kapatılamaz. */
function CloseOrderDialog({ orderId, orderNo, onClose }: { orderId: number; orderNo: string; onClose: () => void }) {
  const [close, { isLoading, error }] = useCloseBoyahaneOrderMutation()
  const [note, setNote] = useState('')

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    if (!('error' in await close({ id: orderId, note: note.trim() || null }))) {
      onClose()
    }
  }

  return (
    <Modal title={`Siparişi kapat: ${orderNo}`} onClose={onClose}>
      <form className="form" onSubmit={handleSubmit}>
        <p className="muted">
          Kapatılan sipariş salt okunur olur; yeni parti, laboratuvar talebi ve çeki listesi açılmaz. Partiye girmemiş top ve lotlar
          siparişten çözülür, açık laboratuvar talepleri iptal edilir.
        </p>
        <ErrorAlert error={error} />
        <label className="field">
          <span>Not</span>
          <input maxLength={500} value={note} onChange={(event) => setNote(event.target.value)} />
        </label>
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading}>Kapat</button>
        </footer>
      </form>
    </Modal>
  )
}

/** Sipariş: başlık, satırlar, bağlı mal ve durum işlemleri. */
export function BoyahaneOrderDetailPage() {
  const id = Number(useParams().id)
  const navigate = useNavigate()
  const canManage = usePermission(Permissions.BoyahaneOrderManage)
  const { data: order, error, isLoading } = useGetBoyahaneOrderQuery(id)
  const [confirm, confirmState] = useConfirmBoyahaneOrderMutation()
  const [cancel, cancelState] = useCancelBoyahaneOrderMutation()
  const [reopen, reopenState] = useReopenBoyahaneOrderMutation()
  const [remove, removeState] = useDeleteBoyahaneOrderMutation()
  const [editing, setEditing] = useState(false)
  const [assigning, setAssigning] = useState<BoyahaneOrderLine | null>(null)
  const [labLine, setLabLine] = useState<BoyahaneOrderLine | null>(null)
  const canManageLab = usePermission(Permissions.BoyahaneLabManage)
  const canDecideLab = usePermission(Permissions.BoyahaneLabCustomerDecision)
  const canOpenLab = canManageLab || canDecideLab
  const [pending, setPending] = useState<'cancel' | 'delete' | null>(null)
  const [closing, setClosing] = useState(false)

  const handleDelete = async () => {
    const result = await remove(id)
    if (!('error' in result)) {
      navigate('/boyahane/orders')
    }
  }

  const status = order && orderStatusPresentation[order.status]
  const draft = order?.status === 'DRAFT'
  // İptal edilen ve kapatılan sipariş salt okunurdur.
  const open = order?.status === 'DRAFT' || order?.status === 'CONFIRMED'

  return (
    <section className="page">
      <p><Link to="/boyahane/orders">← Siparişlere dön</Link></p>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error ?? confirmState.error ?? cancelState.error ?? removeState.error ?? reopenState.error} />

      {order && status && (
        <>
          <header className="page__header">
            <div>
              <h1>{order.orderNo} <span className={`badge badge--${status.tone}`}>{status.label}</span></h1>
              <p className="muted">
                {formatDate(order.orderDate)} · {order.customerName ?? 'Stoğa üretim'} · {orderOwnershipLabels[order.ownership]}
                {order.customerOrderNo && ` · Müşteri sipariş no ${order.customerOrderNo}`}
                {order.dueDate && ` · Termin ${formatDate(order.dueDate)}`}
              </p>
              {order.notes && <p>{order.notes}</p>}
              {order.status === 'CLOSED' && order.closedAt && (
                <p className="muted">
                  Kapatıldı: {formatDateTime(order.closedAt)}{order.closedBy && ` · ${order.closedBy}`}{order.closeNote && ` · ${order.closeNote}`}
                </p>
              )}
            </div>
            {(canManage || (open && order.customerId !== null)) && (
              <div className="page__actions">
                {open && order.customerId !== null && (
                  <Link className="button" to={`/boyahane/orders/${order.id}/proforma`}><FileText aria-hidden="true" />Proforma / Teyit</Link>
                )}
                {canManage && draft && <button type="button" className="button" onClick={() => setEditing(true)}><Pencil aria-hidden="true" />Düzenle</button>}
                {canManage && draft && <button type="button" className="button button--primary" disabled={confirmState.isLoading} onClick={() => confirm(id)}><Check aria-hidden="true" />Onayla</button>}
                {canManage && draft && pending !== 'delete' && <button type="button" className="button button--danger-ghost" onClick={() => setPending('delete')}><Trash2 aria-hidden="true" />Sil</button>}
                {pending === 'delete' && <button type="button" className="button button--danger-ghost" disabled={removeState.isLoading} onClick={handleDelete}>Silmeyi onayla</button>}
                {canManage && order.status === 'CONFIRMED' && <button type="button" className="button" onClick={() => setClosing(true)}><Archive aria-hidden="true" />Kapat</button>}
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
                  <th>İşlem</th>
                  <th>Kart</th>
                  <th>Renk / desen</th>
                  <th className="table__number">Miktar</th>
                  <th className="table__number">Birim fiyat (fatura ölçüsü)</th>
                  <th className="table__number" title="Fatura ölçüsü sipariş biriminden farklıysa tutar faturada çıkış ölçüsünden hesaplanır">Tahmini tutar</th>
                  <th className="table__number">Bağlı</th>
                  <th className="table__number">Bağlı miktar</th>
                  <th>Reçete</th>
                  <th aria-label="İşlem" />
                </tr>
              </thead>
              <tbody>
                {order.lines.map((line) => (
                  <tr key={line.id}>
                    <td>{orderProcessTypeLabels[line.processType]}</td>
                    <td>{line.itemCode} — {line.itemName}{line.notes && <div className="muted">{line.notes}</div>}</td>
                    <td>
                      {line.colorName ?? '—'}{line.colorCode && <span className="muted"> ({line.colorCode})</span>}
                      {line.colorCardId !== null && <div className="muted">Renk kartı: <Link to={`/boyahane/color-cards/${line.colorCardId}`}>{line.colorCardCode}</Link></div>}
                      {line.patternId !== null && (
                        <div className="muted">
                          Desen: <Link to={`/boyahane/patterns/${line.patternId}`}>{line.patternCode}</Link>{line.colorwayCode && ` · Renk yolu ${line.colorwayCode}`}
                        </div>
                      )}
                    </td>
                    <td className="table__number">
                      {formatQuantity(line.quantity)} {orderUnitLabels[line.unit]}
                      <div className="muted">Fatura: {orderUnitLabels[line.effectiveBillingUnit]}</div>
                    </td>
                    <td className="table__number">{line.unitPrice !== null ? formatMoney(line.unitPrice, order.currency) : '—'}</td>
                    <td className="table__number">{line.amount !== null ? formatMoney(line.amount, order.currency) : '—'}</td>
                    <td className="table__number">{line.fabricId !== null ? `${line.assignedRollCount} top` : `${line.assignedYarnLotCount} lot`}</td>
                    <td className="table__number">
                      {formatQuantity(line.unit === 'METER' ? line.assignedMeters : line.assignedKilograms)} {orderUnitLabels[line.unit]}
                      {line.assignedDifferencePercent !== null && (
                        <div>
                          <span className="badge badge--warning" title="Bağlanan miktar sipariş miktarından %10'dan fazla farklı">
                            {line.assignedDifferencePercent > 0 ? '+' : ''}{line.assignedDifferencePercent}%
                          </span>
                        </div>
                      )}
                    </td>
                    <td>
                      {line.labRequestId !== null ? (
                        <Link to={`/boyahane/lab/${line.labRequestId}`}>
                          {line.recipeTrialCode ? `Onaylı: ${line.recipeTrialCode}` : 'Laboratuvarda'}
                        </Link>
                      ) : (line.processType === 'FABRIC_DYEING' || line.processType === 'YARN_DYEING') && open && canOpenLab ? (
                        <button type="button" className="button button--tiny" onClick={() => setLabLine(line)}>Lab</button>
                      ) : '—'}
                    </td>
                    <td>
                      {canManage && open && (
                        <button type="button" className="button button--tiny" onClick={() => setAssigning(line)}>{line.fabricId !== null ? 'Top bağla' : 'Lot bağla'}</button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
              <tfoot>
                <tr>
                  <th colSpan={5} scope="row">Toplam</th>
                  <td className="table__number">{order.totalAmount > 0 ? formatMoney(order.totalAmount, order.currency) : '—'}</td>
                  <td colSpan={4} />
                </tr>
              </tfoot>
            </table>
          </div>

          {editing && <BoyahaneOrderFormDialog order={order} onClose={() => setEditing(false)} onSaved={() => setEditing(false)} />}
          {closing && <CloseOrderDialog orderId={order.id} orderNo={order.orderNo} onClose={() => setClosing(false)} />}
          {labLine && <OpenLabRequestDialog line={labLine} onClose={() => setLabLine(null)} />}
          {assigning && assigning.fabricId !== null && <AssignRollsDialog orderId={order.id} line={assigning} onClose={() => setAssigning(null)} />}
          {assigning && assigning.yarnId !== null && <AssignYarnLotsDialog orderId={order.id} line={assigning} onClose={() => setAssigning(null)} />}
        </>
      )}
    </section>
  )
}
