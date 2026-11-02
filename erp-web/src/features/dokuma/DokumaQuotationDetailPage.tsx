import { Check, FileText, Pencil, RotateCcw, Send, Trash2, XCircle } from 'lucide-react'
import { Fragment, useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { documentLanguageLabels } from '../../utils/documentText'
import { formatDate, formatDateTime, formatQuantity, todayIsoDate } from '../../utils/format'
import { formatMoney, formatRate } from '../../utils/money'
import { LookupSelect } from '../masterData/LookupSelect'
import { QuotationLineBreakdown } from './DokumaQuotationBreakdown'
import { DokumaQuotationFormDialog } from './DokumaQuotationFormDialog'
import { dokumaOrderOwnershipLabels } from './dokumaOrdersApi'
import {
  dokumaQuotationStatusPresentation,
  useAcceptDokumaQuotationMutation,
  useDeleteDokumaQuotationMutation,
  useGetDokumaQuotationQuery,
  useRejectDokumaQuotationMutation,
  useReopenDokumaQuotationMutation,
  useSendDokumaQuotationMutation,
} from './dokumaQuotationsApi'
import type { DokumaQuotation } from './dokumaQuotationsApi'

/** Kabul: teklifin satırları ve fiyatlarıyla taslak sipariş açılır; aday firmaya verilen teklifte müşteri kartı seçilir. */
function AcceptDialog({ quotation, onClose }: { quotation: DokumaQuotation; onClose: () => void }) {
  const navigate = useNavigate()
  const [accept, { isLoading, error }] = useAcceptDokumaQuotationMutation()
  const [customerId, setCustomerId] = useState<number | null>(quotation.customerId)
  const [orderDate, setOrderDate] = useState(todayIsoDate())
  const [dueDate, setDueDate] = useState('')
  const [customerOrderNo, setCustomerOrderNo] = useState('')

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await accept({ id: quotation.id, customerId: quotation.customerId === null ? customerId : null, orderDate, dueDate: dueDate || null, customerOrderNo: customerOrderNo.trim() || null })
    if ('data' in result && result.data?.orderId) {
      navigate(`/dokuma/orders/${result.data.orderId}`)
    }
  }

  return (
    <Modal title={`Teklifi kabul et: ${quotation.quotationNo}`} onClose={onClose}>
      <form className="form" onSubmit={handleSubmit}>
        <p className="muted">Teklifin satırları, fiyatları, para birimi ve iş türüyle taslak sipariş açılır. Sipariş onayı sipariş ekranından yapılır.</p>
        {quotation.customerId === null && (
          <LookupSelect resource="dokuma-customers" label={`Müşteri kartı (${quotation.prospectName ?? 'aday firma'})`} required value={customerId} onChange={setCustomerId} />
        )}
        <div className="form__pair">
          <label className="field"><span>Sipariş tarihi</span><input type="date" required value={orderDate} onChange={(event) => setOrderDate(event.target.value)} /></label>
          <label className="field"><span>Termin</span><input type="date" value={dueDate} onChange={(event) => setDueDate(event.target.value)} /></label>
        </div>
        <label className="field"><span>Müşteri sipariş no</span><input maxLength={50} value={customerOrderNo} onChange={(event) => setCustomerOrderNo(event.target.value)} /></label>
        <ErrorAlert error={error} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading || (quotation.customerId === null && customerId === null)}>Kabul et, sipariş aç</button>
        </footer>
      </form>
    </Modal>
  )
}

function RejectDialog({ quotation, onClose }: { quotation: DokumaQuotation; onClose: () => void }) {
  const [reject, { isLoading, error }] = useRejectDokumaQuotationMutation()
  const [reason, setReason] = useState('')

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await reject({ id: quotation.id, reason: reason.trim() || null })
    if (!('error' in result)) {
      onClose()
    }
  }

  return (
    <Modal title={`Teklifi reddet: ${quotation.quotationNo}`} onClose={onClose}>
      <form className="form" onSubmit={handleSubmit}>
        <label className="field"><span>Ret nedeni</span><textarea rows={3} maxLength={500} value={reason} onChange={(event) => setReason(event.target.value)} /></label>
        <ErrorAlert error={error} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--danger-ghost" disabled={isLoading}>Reddet</button>
        </footer>
      </form>
    </Modal>
  )
}

/** Teklif: başlık, satırlar, satır başına ön maliyet dökümü ve durum işlemleri. */
export function DokumaQuotationDetailPage() {
  const id = Number(useParams().id)
  const navigate = useNavigate()
  const canManage = usePermission(Permissions.DokumaQuotationManage)
  const { data: quotation, error, isLoading } = useGetDokumaQuotationQuery(id)
  const [send, sendState] = useSendDokumaQuotationMutation()
  const [reopen, reopenState] = useReopenDokumaQuotationMutation()
  const [remove, removeState] = useDeleteDokumaQuotationMutation()
  const [editing, setEditing] = useState(false)
  const [dialog, setDialog] = useState<'accept' | 'reject' | null>(null)
  const [pendingDelete, setPendingDelete] = useState(false)
  const [expanded, setExpanded] = useState<number | null>(null)

  const handleDelete = async () => {
    const result = await remove(id)
    if (!('error' in result)) {
      navigate('/dokuma/quotations')
    }
  }

  const status = quotation && dokumaQuotationStatusPresentation[quotation.status]
  const money = (value: number) => (quotation ? formatMoney(value, quotation.currency) : '')
  const rate = (value: number) => (quotation ? formatRate(value, quotation.currency) : '')

  return (
    <section className="page">
      <p><Link to="/dokuma/quotations">← Tekliflere dön</Link></p>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error ?? sendState.error ?? reopenState.error ?? removeState.error} />
      {quotation && status && (
        <>
          <header className="page__header">
            <div>
              <h1>{quotation.quotationNo} <span className={`badge badge--${status.tone}`}>{status.label}</span></h1>
              <p className="muted">
                {formatDate(quotation.quotationDate)} · {quotation.customerName ?? `${quotation.prospectName} (aday)`} · {dokumaOrderOwnershipLabels[quotation.ownership]}
                {` · Geçerlilik ${formatDate(quotation.validUntil)}`}
                {quotation.contactName && ` · İlgili ${quotation.contactName}`}
                {` · ${quotation.currency}, ${documentLanguageLabels[quotation.documentLanguage]}`}
              </p>
              <p className="muted">
                {quotation.paymentTermDays !== null && `Vade ${quotation.paymentTermDays} gün`}
                {quotation.deliveryTerms && `${quotation.paymentTermDays !== null ? ' · ' : ''}Teslim: ${quotation.deliveryTerms}`}
                {quotation.sentAt && ` · Gönderildi ${formatDateTime(quotation.sentAt)}`}
                {quotation.decidedAt && ` · Karar ${formatDateTime(quotation.decidedAt)}${quotation.decidedBy ? ` (${quotation.decidedBy})` : ''}`}
              </p>
              {quotation.rejectionReason && <p>Ret nedeni: {quotation.rejectionReason}</p>}
              {quotation.orderId && <p>Açılan sipariş: <Link to={`/dokuma/orders/${quotation.orderId}`}>{quotation.orderNo}</Link></p>}
              {quotation.notes && <p className="preserve-lines">{quotation.notes}</p>}
            </div>
            <div className="page__actions">
              <Link className="button" to={`/dokuma/quotations/${id}/print`}><FileText aria-hidden="true" />Yazdır</Link>
              {canManage && quotation.status === 'DRAFT' && (
                <>
                  <button type="button" className="button" onClick={() => setEditing(true)}><Pencil aria-hidden="true" />Düzenle</button>
                  <button type="button" className="button button--primary" disabled={sendState.isLoading} onClick={() => send(id)}><Send aria-hidden="true" />Gönderildi</button>
                  {!pendingDelete && <button type="button" className="button button--danger-ghost" onClick={() => setPendingDelete(true)}><Trash2 aria-hidden="true" />Sil</button>}
                  {pendingDelete && <button type="button" className="button button--danger-ghost" disabled={removeState.isLoading} onClick={handleDelete}>Silmeyi onayla</button>}
                </>
              )}
              {canManage && quotation.status === 'SENT' && (
                <button type="button" className="button button--primary" onClick={() => setDialog('accept')}><Check aria-hidden="true" />Kabul</button>
              )}
              {canManage && (quotation.status === 'SENT' || quotation.status === 'EXPIRED') && (
                <button type="button" className="button button--danger-ghost" onClick={() => setDialog('reject')}><XCircle aria-hidden="true" />Ret</button>
              )}
              {canManage && (quotation.status === 'SENT' || quotation.status === 'REJECTED' || quotation.status === 'EXPIRED') && (
                <button type="button" className="button" disabled={reopenState.isLoading} onClick={() => reopen(id)}><RotateCcw aria-hidden="true" />Taslağa al</button>
              )}
            </div>
          </header>

          <dl className="stat-strip">
            <div><dt>Toplam metre</dt><dd>{formatQuantity(quotation.totalMeters)}</dd></div>
            <div><dt>Toplam maliyet</dt><dd>{money(quotation.totalCost)}</dd></div>
            <div><dt>Teklif tutarı</dt><dd>{money(quotation.totalAmount)}</dd></div>
            <div>
              <dt>Kâr</dt>
              <dd>
                {money(quotation.totalAmount - quotation.totalCost)}
                {quotation.totalCost > 0 && <span className="muted"> (%{formatQuantity(Math.round(((quotation.totalAmount - quotation.totalCost) / quotation.totalCost) * 1000) / 10)})</span>}
              </dd>
            </div>
          </dl>

          <div className="table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <th>Artikel</th>
                  <th>Tezgâh</th>
                  <th className="table__number">Metre</th>
                  <th className="table__number">Maliyet (m)</th>
                  <th className="table__number">Hesaplanan (m)</th>
                  <th className="table__number">Teklif fiyatı (m)</th>
                  <th className="table__number">Kâr</th>
                  <th className="table__number">Tutar</th>
                  <th aria-label="Döküm" />
                </tr>
              </thead>
              <tbody>
                {quotation.lines.map((line) => {
                  const lineKey = line.id ?? line.sequence
                  const open = expanded === lineKey
                  return (
                    <Fragment key={lineKey}>
                      <tr>
                        <td>
                          {line.articleCode} — {line.articleName}
                          {line.customerArticleCode && <div className="muted">Müşteri kodu {line.customerArticleCode}</div>}
                          {line.notes && <div className="muted">{line.notes}</div>}
                        </td>
                        <td>{line.loomCode ?? '—'}</td>
                        <td className="table__number">{formatQuantity(line.meters)}</td>
                        <td className="table__number">{rate(line.costPerMeter)}</td>
                        <td className="table__number">{rate(line.calculatedPrice)}</td>
                        <td className="table__number"><strong>{rate(line.unitPrice)}</strong></td>
                        <td className="table__number">{line.actualMarginPercent === null ? '—' : `%${formatQuantity(line.actualMarginPercent)}`}</td>
                        <td className="table__number">{money(line.amount)}</td>
                        <td className="table__actions">
                          <button type="button" className="button" aria-expanded={open} onClick={() => setExpanded(open ? null : lineKey)}>{open ? 'Gizle' : 'Döküm'}</button>
                        </td>
                      </tr>
                      {open && (
                        <tr>
                          <td colSpan={9}>
                            <QuotationLineBreakdown line={line} currency={quotation.currency} ownership={quotation.ownership} />
                          </td>
                        </tr>
                      )}
                    </Fragment>
                  )
                })}
              </tbody>
              <tfoot>
                <tr>
                  <th scope="row" colSpan={2}>Toplam</th>
                  <td className="table__number">{formatQuantity(quotation.totalMeters)}</td>
                  <td colSpan={4} />
                  <td className="table__number">{money(quotation.totalAmount)}</td>
                  <td />
                </tr>
              </tfoot>
            </table>
          </div>
          <p className="muted">
            Maliyet dökümü teklif kaydedilirken hesaplanır ve saklanır; maliyet parametreleri ya da iplik fiyatları sonradan değişse de
            bu teklif değişmez. Güncel değerlerle yeniden hesaplamak için taslak teklifi düzenleyip ilgili alanları temizleyin.
          </p>
        </>
      )}

      {editing && quotation && <DokumaQuotationFormDialog quotation={quotation} onClose={() => setEditing(false)} onSaved={() => setEditing(false)} />}
      {dialog === 'accept' && quotation && <AcceptDialog quotation={quotation} onClose={() => setDialog(null)} />}
      {dialog === 'reject' && quotation && <RejectDialog quotation={quotation} onClose={() => setDialog(null)} />}
    </section>
  )
}
