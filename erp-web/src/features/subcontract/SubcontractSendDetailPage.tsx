import { Printer } from 'lucide-react'
import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDate, formatDateTime, formatQuantity } from '../../utils/format'
import { formatRate } from '../../utils/money'
import { PaidBadge, SubcontractSendStatusBadge } from './SubcontractBadges'
import { SubcontractReturnDetailDialog } from './SubcontractReturnDetailDialog'
import { SubcontractReturnDialog } from './SubcontractReturnDialog'
import { useCancelSubcontractSendMutation, useGetSubcontractSendQuery } from './subcontractApi'
import { formatAmounts } from './subcontractPresentation'

type Dialog = { kind: 'return' } | { kind: 'cancel' } | { kind: 'view'; id: number }

export function SubcontractSendDetailPage() {
  const id = Number(useParams().id)
  const canManage = usePermission(Permissions.SubcontractManage)
  const { data: send, isLoading, error } = useGetSubcontractSendQuery(id)
  const [cancel, cancelState] = useCancelSubcontractSendMutation()
  const [dialog, setDialog] = useState<Dialog | null>(null)

  if (isLoading) {
    return <section className="page"><p role="status">Yükleniyor…</p></section>
  }

  if (!send) {
    return (
      <section className="page">
        <ErrorAlert error={error} />
        <Link to="/subcontract/sends">← Fason gönderimlere dön</Link>
      </section>
    )
  }

  const returnedGood = send.lines.reduce((sum, line) => sum + line.returnedGood, 0)
  const lost = send.lines.reduce((sum, line) => sum + line.returnedDefective + line.returnedMissing, 0)

  return (
    <section className="page">
      <p><Link to="/subcontract/sends">← Fason gönderimlere dön</Link></p>

      <header className="page__header">
        <div>
          <h1>{send.sendNo} <SubcontractSendStatusBadge status={send.status} /></h1>
          <p className="muted">{send.createdBy ?? 'sistem'} tarafından {formatDateTime(send.createdAt)} tarihinde kaydedildi.</p>
        </div>
        <div className="page__actions">
          {send.status !== 'CANCELLED' && (
            <Link to={`/subcontract/sends/${send.id}/print`} className="button">
              <Printer aria-hidden="true" />
              İrsaliye
            </Link>
          )}
          {canManage && send.canCancel && (
            <button type="button" className="button" onClick={() => setDialog({ kind: 'cancel' })}>İptal et</button>
          )}
          {canManage && send.canReturn && (
            <button type="button" className="button button--primary" onClick={() => setDialog({ kind: 'return' })}>Dönüş gir</button>
          )}
        </div>
      </header>

      <ErrorAlert error={cancelState.error} />
      {send.canReturn && (
        <p className="alert alert--info">
          Fasondaki demetler sahada (tablet ya da elle bildirim) işlenemez; dönüş girildikçe sahaya döner.
        </p>
      )}

      <div className="cards">
        <article className="card">
          <h2>Gönderim</h2>
          <dl className="status-list">
            <div><dt>Fasoncu</dt><dd><code>{send.supplierCode}</code> {send.supplierName}</dd></div>
            <div><dt>Operasyon</dt><dd><code>{send.operationCode}</code> {send.operationName}</dd></div>
            <div><dt>Tarih</dt><dd>{formatDate(send.sendDate)}</dd></div>
            <div><dt>Taşıyıcı / araç</dt><dd>{send.carrier ?? '—'}</dd></div>
          </dl>
        </article>
        <article className="card">
          <h2>Durum</h2>
          <dl className="status-list">
            <div><dt>Gönderilen</dt><dd>{formatQuantity(send.totalQuantity)}</dd></div>
            <div><dt>Sağlam dönen</dt><dd>{formatQuantity(returnedGood)}</dd></div>
            <div><dt>Hatalı / eksik</dt><dd>{formatQuantity(lost)}</dd></div>
            <div><dt>Fasonda</dt><dd><strong>{formatQuantity(send.outstanding)}</strong>{send.daysOutside !== null && ` · ${send.daysOutside} gündür dışarıda`}</dd></div>
          </dl>
        </article>
      </div>

      {send.notes && (
        <article className="card card--spaced">
          <h2>Not</h2>
          <p className="preserve-lines">{send.notes}</p>
        </article>
      )}

      <h2 className="section-title">Satırlar</h2>
      <div className="table-wrap">
        <table className="table table--compact">
          <thead>
            <tr>
              <th>Demet</th>
              <th>Sipariş</th>
              <th>Ürün</th>
              <th>Renk</th>
              <th>Beden</th>
              <th className="table__number">Gönderilen</th>
              <th className="table__number">Sağlam</th>
              <th className="table__number">Hatalı</th>
              <th className="table__number">Eksik</th>
              <th className="table__number">Fasonda</th>
              <th className="table__number">Fiyat</th>
            </tr>
          </thead>
          <tbody>
            {send.lines.map((line) => (
              <tr key={line.id}>
                <td>{line.bundleId ? <Link to={`/bundles/${line.bundleId}`}><code>{line.bundleNo}</code></Link> : <span className="muted">demetsiz</span>}</td>
                <td>{line.productionOrderId ? <Link to={`/production-orders/${line.productionOrderId}`}>{line.orderNo}</Link> : '—'}</td>
                <td><code>{line.productCode}</code> {line.modelCode}</td>
                <td>{line.colorName}</td>
                <td>{line.sizeName}</td>
                <td className="table__number">{formatQuantity(line.quantity)}</td>
                <td className="table__number">{formatQuantity(line.returnedGood)}</td>
                <td className="table__number">{formatQuantity(line.returnedDefective)}</td>
                <td className="table__number">{formatQuantity(line.returnedMissing)}</td>
                <td className="table__number">{line.outstanding > 0 ? <strong>{formatQuantity(line.outstanding)}</strong> : '—'}</td>
                <td className="table__number">{line.unitPrice === null ? <span className="muted">—</span> : formatRate(line.unitPrice, line.currency)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <h2 className="section-title">Dönüşler ({send.returns.length})</h2>
      {send.returns.length === 0 ? (
        <p className="muted">Henüz dönüş girilmedi.</p>
      ) : (
        <div className="table-wrap">
          <table className="table table--compact">
            <thead>
              <tr>
                <th>Dönüş no</th>
                <th>Tarih</th>
                <th>Fasoncu irsaliye</th>
                <th className="table__number">Sağlam</th>
                <th className="table__number">Hatalı</th>
                <th className="table__number">Eksik</th>
                <th className="table__number">Hakediş</th>
                <th>Ödeme</th>
              </tr>
            </thead>
            <tbody>
              {send.returns.map((ret) => (
                <tr key={ret.id}>
                  <td>
                    <button type="button" className="table__open" onClick={() => setDialog({ kind: 'view', id: ret.id })}>
                      <code>{ret.returnNo}</code>
                    </button>
                  </td>
                  <td>{formatDate(ret.returnDate)}</td>
                  <td>{ret.supplierDocumentNo ?? '—'}</td>
                  <td className="table__number">{formatQuantity(ret.goodQuantity)}</td>
                  <td className="table__number">{formatQuantity(ret.defectiveQuantity)}</td>
                  <td className="table__number">{formatQuantity(ret.missingQuantity)}</td>
                  <td className="table__number">{formatAmounts(ret.amounts)}</td>
                  <td><PaidBadge paid={ret.isPaid} /></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {dialog?.kind === 'return' && (
        <SubcontractReturnDialog send={send} onClose={() => setDialog(null)} onSaved={(ret) => setDialog({ kind: 'view', id: ret.id })} />
      )}
      {dialog?.kind === 'view' && <SubcontractReturnDetailDialog id={dialog.id} onClose={() => setDialog(null)} />}
      {dialog?.kind === 'cancel' && (
        <Modal title="Fason gönderimini iptal et" onClose={() => setDialog(null)}>
          <div className="form">
            <p>{send.sendNo} iptal edilecek; demetler sahaya döner. İptal edilen fiş yeniden açılamaz.</p>
            <footer className="form__actions">
              <button type="button" className="button" onClick={() => setDialog(null)}>Vazgeç</button>
              <button
                type="button"
                className="button button--danger"
                disabled={cancelState.isLoading}
                onClick={() => {
                  setDialog(null)
                  void cancel({ id: send.id, rowVersion: send.rowVersion })
                }}
              >
                İptal et
              </button>
            </footer>
          </div>
        </Modal>
      )}
    </section>
  )
}
