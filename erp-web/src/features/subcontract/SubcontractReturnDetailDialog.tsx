import { useState } from 'react'
import { Link } from 'react-router-dom'
import { ErrorAlert, TextField } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDate, formatDateTime, formatQuantity, todayIsoDate } from '../../utils/format'
import { formatMoney, formatRate, parseDecimal } from '../../utils/money'
import { PaidBadge } from './SubcontractBadges'
import {
  useGetSubcontractReturnQuery,
  useSetSubcontractReturnPaymentMutation,
  useUpdateSubcontractReturnPricesMutation,
} from './subcontractApi'
import type { SubcontractReturn } from './subcontractApi'
import { formatAmounts } from './subcontractPresentation'

function PaymentForm({ ret }: { ret: SubcontractReturn }) {
  const [setPayment, { isLoading, error }] = useSetSubcontractReturnPaymentMutation()
  const [paidDate, setPaidDate] = useState(todayIsoDate())
  const [note, setNote] = useState('')

  if (ret.isPaid) {
    return (
      <div className="form">
        <p>
          <PaidBadge paid /> {formatDate(ret.paidDate)} · {ret.paidBy ?? '—'}
          {ret.paymentNote && <span className="muted"> · {ret.paymentNote}</span>}
        </p>
        <ErrorAlert error={error} />
        <div className="toolbar">
          <button
            type="button"
            className="button"
            disabled={isLoading}
            onClick={() => void setPayment({ id: ret.id, rowVersion: ret.rowVersion, paid: false, paidDate: null, note: null })}
          >
            Ödeme işaretini kaldır
          </button>
        </div>
      </div>
    )
  }

  return (
    <div className="form">
      <div className="form__pair">
        <TextField label="Ödeme tarihi" type="date" value={paidDate} onChange={(event) => setPaidDate(event.target.value)} />
        <TextField label="Not (dekont, açıklama)" value={note} maxLength={500} onChange={(event) => setNote(event.target.value)} />
      </div>
      <ErrorAlert error={error} />
      <div className="toolbar">
        <button
          type="button"
          className="button button--primary"
          disabled={isLoading}
          onClick={() => void setPayment({ id: ret.id, rowVersion: ret.rowVersion, paid: true, paidDate, note: note.trim() || null })}
        >
          Ödendi olarak işaretle
        </button>
      </div>
    </div>
  )
}

/** Dönüş fişi ve hakedişi: satırlar, fiyat düzeltme (ödenmemişse) ve ödendi işareti. */
export function SubcontractReturnDetailDialog({ id, onClose }: { id: number; onClose: () => void }) {
  const canManage = usePermission(Permissions.SubcontractManage)
  const { data: ret, error, isLoading } = useGetSubcontractReturnQuery(id)
  const [updatePrices, pricesState] = useUpdateSubcontractReturnPricesMutation()
  const [editing, setEditing] = useState<Record<number, string> | null>(null)

  const savePrices = async () => {
    if (!ret || !editing) {
      return
    }
    const result = await updatePrices({
      id: ret.id,
      rowVersion: ret.rowVersion,
      lines: ret.lines.map((line) => ({
        returnLineId: line.id,
        unitPrice: (editing[line.id] ?? '').trim() === '' ? null : parseDecimal(editing[line.id]),
      })),
    })
    if (!('error' in result)) {
      setEditing(null)
    }
  }

  return (
    <Modal title={ret ? `Fason dönüşü ${ret.returnNo}` : 'Fason dönüşü'} onClose={onClose} variant="drawer">
      <div className="form">
        {isLoading && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={error} />
        {ret && (
          <>
            <dl className="status-list">
              <div><dt>Fasoncu</dt><dd>{ret.supplierName}</dd></div>
              <div><dt>Gönderim</dt><dd><Link to={`/subcontract/sends/${ret.subcontractSendId}`}>{ret.sendNo}</Link> · {formatDate(ret.sendDate)}</dd></div>
              <div><dt>Operasyon</dt><dd>{ret.operationName}</dd></div>
              <div><dt>Dönüş tarihi</dt><dd>{formatDate(ret.returnDate)}</dd></div>
              <div><dt>Fasoncu irsaliye no</dt><dd>{ret.supplierDocumentNo ?? '—'}</dd></div>
              <div><dt>Hakediş</dt><dd><strong>{formatAmounts(ret.amounts)}</strong></dd></div>
              <div><dt>Kaydeden</dt><dd>{ret.createdBy ?? '—'} · {formatDateTime(ret.createdAt)}</dd></div>
            </dl>
            {ret.notes && <p className="preserve-lines">{ret.notes}</p>}

            <div className="table-wrap">
              <table className="table table--compact">
                <thead>
                  <tr>
                    <th>Demet / ürün</th>
                    <th>Renk / beden</th>
                    <th className="table__number">Sağlam</th>
                    <th className="table__number">Hatalı</th>
                    <th className="table__number">Eksik</th>
                    <th>Hata türü</th>
                    <th className="table__number">Birim fiyat</th>
                    <th className="table__number">Tutar</th>
                  </tr>
                </thead>
                <tbody>
                  {ret.lines.map((line) => (
                    <tr key={line.id}>
                      <td>
                        {line.bundleNo ? <code>{line.bundleNo}</code> : <span className="muted">demetsiz</span>}{' '}
                        <span className="muted">{line.productCode}</span>
                      </td>
                      <td>{line.colorName} / {line.sizeName}</td>
                      <td className="table__number">{formatQuantity(line.goodQuantity)}</td>
                      <td className="table__number">{formatQuantity(line.defectiveQuantity)}</td>
                      <td className="table__number">{formatQuantity(line.missingQuantity)}</td>
                      <td>{line.defectTypeName ?? '—'}</td>
                      <td className="table__number">
                        {editing ? (
                          <input
                            inputMode="decimal"
                            className="matrix-input"
                            aria-label={`${line.bundleNo ?? line.productCode} birim fiyat`}
                            placeholder={line.currency}
                            value={editing[line.id] ?? ''}
                            onChange={(event) => setEditing({ ...editing, [line.id]: event.target.value })}
                          />
                        ) : line.unitPrice === null ? <span className="badge badge--warning">Fiyat yok</span> : formatRate(line.unitPrice, line.currency)}
                      </td>
                      <td className="table__number">{formatMoney(line.amount, line.currency)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <ErrorAlert error={pricesState.error} />
            {canManage && !ret.isPaid && (
              <div className="toolbar">
                {editing ? (
                  <>
                    <button type="button" className="button" onClick={() => setEditing(null)}>Vazgeç</button>
                    <button type="button" className="button button--primary" disabled={pricesState.isLoading} onClick={() => void savePrices()}>
                      Fiyatları kaydet
                    </button>
                  </>
                ) : (
                  <button
                    type="button"
                    className="button"
                    onClick={() => setEditing(Object.fromEntries(ret.lines.map((line) => [line.id, line.unitPrice === null ? '' : String(line.unitPrice).replace('.', ',')])))}
                  >
                    Fiyatları düzelt
                  </button>
                )}
              </div>
            )}

            <h3 className="section-title">Ödeme</h3>
            {canManage ? <PaymentForm key={ret.rowVersion} ret={ret} /> : <p><PaidBadge paid={ret.isPaid} /> {ret.isPaid && formatDate(ret.paidDate)}</p>}
          </>
        )}
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Kapat</button>
        </footer>
      </div>
    </Modal>
  )
}
