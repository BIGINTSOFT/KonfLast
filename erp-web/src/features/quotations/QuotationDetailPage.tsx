import { Plus, Printer } from 'lucide-react'
import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { documentLanguageLabels } from '../../utils/documentText'
import { formatDate, formatDateTime, formatQuantity } from '../../utils/format'
import { formatMoney, formatRate } from '../../utils/money'
import { QuotationAcceptDialog } from './QuotationAcceptDialog'
import { QuotationStatusBadge } from './QuotationBadges'
import { QuotationFormDialog } from './QuotationFormDialog'
import { QuotationLineDialog } from './QuotationLineDialog'
import { useActOnQuotationMutation, useDeleteQuotationMutation, useGetQuotationQuery, useSaveQuotationMutation } from './quotationsApi'
import type { Quotation, QuotationAction, QuotationLine } from './quotationsApi'
import { draftOf, formatMinutes, formatPercent, lineTitle, requestOf } from './quotationPresentation'
import type { LineDraft } from './quotationPresentation'

type Dialog =
  | { kind: 'header' }
  | { kind: 'line'; line?: QuotationLine }
  | { kind: 'accept' }
  | { kind: 'confirm'; action: QuotationAction | 'delete' }
  | { kind: 'removeLine'; line: QuotationLine }

const confirmTexts: Record<QuotationAction | 'delete', { title: string; text: string; button: string }> = {
  send: { title: 'Teklifi gönder', text: 'Teklif müşteriye gönderildi olarak işaretlenecek; içerik kilitlenir.', button: 'Gönderildi' },
  reject: { title: 'Teklif reddedildi', text: 'Teklif reddedildi olarak işaretlenecek.', button: 'Reddedildi' },
  expire: { title: 'Teklifin süresi doldu', text: 'Teklif süresi doldu olarak işaretlenecek.', button: 'Süresi doldu' },
  reopen: { title: 'Taslağa al', text: 'Teklif düzeltilmek üzere taslağa alınacak; satırlar yeniden düzenlenebilir.', button: 'Taslağa al' },
  delete: { title: 'Teklifi sil', text: 'Taslak teklif silinecek; numarası yeniden kullanılmaz.', button: 'Sil' },
}

/** Teklifin bütün satırlarıyla kayıt isteği; taslak, lineId'li satırın yerine (lineId boşsa sona) yazılır. */
function bodyWith(quotation: Quotation, lineId: number | null, draft: LineDraft | null) {
  const drafts = quotation.lines.map((line) => (line.id === lineId && draft ? draft : draftOf(line)))
  const lines = (lineId === null && draft ? [...drafts, draft] : drafts).map(requestOf)
  return {
    customerId: quotation.customerId,
    prospectName: quotation.prospectName,
    quotationDate: quotation.quotationDate,
    validUntil: quotation.validUntil,
    currency: quotation.currency,
    documentLanguage: quotation.documentLanguage,
    notes: quotation.notes,
    lines,
    rowVersion: quotation.rowVersion,
  }
}

export function QuotationDetailPage() {
  const id = Number(useParams().id)
  const navigate = useNavigate()
  const canManage = usePermission(Permissions.QuotationManage)
  const { data: quotation, isLoading, error } = useGetQuotationQuery(id)
  const [save, saveState] = useSaveQuotationMutation()
  const [act, actState] = useActOnQuotationMutation()
  const [remove, removeState] = useDeleteQuotationMutation()
  const [dialog, setDialog] = useState<Dialog | null>(null)

  if (isLoading) {
    return <section className="page"><p role="status">Yükleniyor…</p></section>
  }

  if (!quotation) {
    return (
      <section className="page">
        <ErrorAlert error={error} />
        <Link to="/quotations">← Tekliflere dön</Link>
      </section>
    )
  }

  const editable = canManage && quotation.isEditable
  const can = (action: QuotationAction) =>
    canManage && quotation.allowedTransitions.includes(
      ({ send: 'SENT', reject: 'REJECTED', expire: 'EXPIRED', reopen: 'DRAFT' } as const)[action])
  const canAccept = canManage && quotation.allowedTransitions.includes('ACCEPTED')
  const currency = quotation.currency

  const saveLine = async (lineId: number | null, draft: LineDraft) => {
    const result = await save({ id: quotation.id, body: bodyWith(quotation, lineId, draft) })
    if (!('error' in result)) {
      setDialog(null)
    }
  }

  const removeLine = async (line: QuotationLine) => {
    const body = bodyWith(quotation, null, null)
    const result = await save({
      id: quotation.id,
      body: { ...body, lines: quotation.lines.filter((other) => other.id !== line.id).map((other) => requestOf(draftOf(other))) },
    })
    if (!('error' in result)) {
      setDialog(null)
    }
  }

  const confirm = async (action: QuotationAction | 'delete') => {
    setDialog(null)
    if (action === 'delete') {
      const result = await remove(quotation.id)
      if (!('error' in result)) {
        void navigate('/quotations')
      }
      return
    }
    await act({ id: quotation.id, action, rowVersion: quotation.rowVersion })
  }

  return (
    <section className="page">
      <p><Link to="/quotations">← Tekliflere dön</Link></p>

      <header className="page__header">
        <div>
          <h1>{quotation.quotationNo} <QuotationStatusBadge status={quotation.status} /></h1>
          <p className="muted">
            {quotation.createdBy ?? 'sistem'} tarafından {formatDateTime(quotation.createdAt)} tarihinde oluşturuldu.
            {quotation.sentAt && ` Gönderildi: ${formatDateTime(quotation.sentAt)}.`}
            {quotation.decidedAt && ` Karar: ${formatDateTime(quotation.decidedAt)} · ${quotation.decidedBy ?? ''}.`}
          </p>
        </div>
        <div className="page__actions">
          <Link to={`/quotations/${quotation.id}/print`} className="button">
            <Printer aria-hidden="true" />
            Yazdır
          </Link>
          {editable && <button type="button" className="button" onClick={() => setDialog({ kind: 'header' })}>Başlığı düzenle</button>}
          {editable && <button type="button" className="button" onClick={() => setDialog({ kind: 'confirm', action: 'delete' })}>Sil</button>}
          {can('reopen') && <button type="button" className="button" onClick={() => setDialog({ kind: 'confirm', action: 'reopen' })}>Taslağa al</button>}
          {can('expire') && <button type="button" className="button" onClick={() => setDialog({ kind: 'confirm', action: 'expire' })}>Süresi doldu</button>}
          {can('reject') && <button type="button" className="button" onClick={() => setDialog({ kind: 'confirm', action: 'reject' })}>Reddedildi</button>}
          {can('send') && (
            <button type="button" className="button button--primary" disabled={quotation.lines.length === 0} onClick={() => setDialog({ kind: 'confirm', action: 'send' })}>
              Gönderildi
            </button>
          )}
          {canAccept && <button type="button" className="button button--primary" onClick={() => setDialog({ kind: 'accept' })}>Kabul et</button>}
        </div>
      </header>

      <ErrorAlert error={actState.error ?? removeState.error ?? (dialog === null ? saveState.error : undefined)} />
      {quotation.isPastValidity && (
        <p className="alert alert--info">Geçerlilik tarihi ({formatDate(quotation.validUntil)}) geçti; müşteri yanıt vermediyse &quot;Süresi doldu&quot; olarak işaretleyin.</p>
      )}
      {quotation.salesOrderId && (
        <p className="alert alert--info">
          Kabulde açılan taslak sipariş: <Link to={`/sales-orders/${quotation.salesOrderId}`}>{quotation.salesOrderNo}</Link>
        </p>
      )}

      <div className="cards">
        <article className="card">
          <h2>Müşteri</h2>
          <dl className="status-list">
            <div>
              <dt>{quotation.customerId ? 'Müşteri' : 'Aday müşteri'}</dt>
              <dd>{quotation.customerId ? <><code>{quotation.customerCode}</code> {quotation.customerName}</> : quotation.prospectName}</dd>
            </div>
            <div><dt>Belge dili</dt><dd>{documentLanguageLabels[quotation.documentLanguage]}</dd></div>
            <div><dt>Teklif tarihi</dt><dd>{formatDate(quotation.quotationDate)}</dd></div>
            <div><dt>Geçerlilik</dt><dd>{formatDate(quotation.validUntil)}</dd></div>
          </dl>
        </article>
        <article className="card">
          <h2>Toplam</h2>
          <dl className="status-list">
            <div><dt>Adet</dt><dd>{formatQuantity(quotation.totalQuantity)}</dd></div>
            <div><dt>Maliyet</dt><dd>{formatMoney(quotation.totalCost, currency)}</dd></div>
            <div><dt>Teklif tutarı</dt><dd><strong>{formatMoney(quotation.totalAmount, currency)}</strong></dd></div>
            <div>
              <dt>Brüt kâr</dt>
              <dd>
                {formatMoney(quotation.totalAmount - quotation.totalCost, currency)}
                {quotation.totalCost > 0 && ` (${formatPercent(Math.round(((quotation.totalAmount / quotation.totalCost) - 1) * 10_000) / 100)})`}
              </dd>
            </div>
            <div>
              <dt>Dakika maliyeti</dt>
              <dd>
                {formatRate(quotation.laborCostPerMinute, currency)} işçilik
                {quotation.energyCostPerMinute > 0 && ` · ${formatRate(quotation.energyCostPerMinute, currency)} enerji`}
              </dd>
            </div>
          </dl>
        </article>
      </div>

      {quotation.notes && (
        <article className="card card--spaced">
          <h2>Not</h2>
          <p className="preserve-lines">{quotation.notes}</p>
        </article>
      )}

      <header className="page__header section-title">
        <h2>Satırlar ve maliyet dökümü (parça başı, {currency})</h2>
        {editable && (
          <button type="button" className="button" onClick={() => setDialog({ kind: 'line' })}>
            <Plus aria-hidden="true" />
            Satır ekle
          </button>
        )}
      </header>
      {quotation.lines.length === 0 ? (
        <p className="muted">Henüz satır yok.</p>
      ) : (
        <div className="table-wrap">
          <table className="table table--compact">
            <thead>
              <tr>
                <th>#</th>
                <th>Ürün / açıklama</th>
                <th className="table__number">Adet</th>
                <th className="table__number">Malzeme</th>
                <th className="table__number">İşçilik</th>
                <th className="table__number">Fason</th>
                <th className="table__number">Fire + GG</th>
                <th className="table__number">Birim maliyet</th>
                <th className="table__number">Marj</th>
                <th className="table__number">Birim fiyat</th>
                <th className="table__number">Tutar</th>
                {editable && <th aria-label="İşlemler" />}
              </tr>
            </thead>
            <tbody>
              {quotation.lines.map((line) => (
                <tr key={line.id}>
                  <td>{line.lineNo}</td>
                  <td>
                    {editable ? (
                      <button type="button" className="table__open" onClick={() => setDialog({ kind: 'line', line })}>{lineTitle(line)}</button>
                    ) : lineTitle(line)}
                    {(line.modelReference || line.colors) && (
                      <div className="muted">{[line.modelReference, line.colors].filter(Boolean).join(' · ')}</div>
                    )}
                    {line.warnings.length > 0 && (
                      <span className="badge badge--warning" title={line.warnings.join('\n')}>{line.warnings.length} uyarı</span>
                    )}
                  </td>
                  <td className="table__number">{formatQuantity(line.quantity)}</td>
                  <td className="table__number">{formatRate(line.materialCost, '')}</td>
                  <td className="table__number" title={formatMinutes(line.laborMinutes)}>{formatRate(line.laborCost + line.energyCost, '')}</td>
                  <td className="table__number">{formatRate(line.subcontractCost, '')}</td>
                  <td className="table__number">{formatRate(line.fireCost + line.overheadCost, '')}</td>
                  <td className="table__number"><strong>{formatRate(line.unitCost, '')}</strong></td>
                  <td className="table__number">{formatPercent(line.marginPercent)}</td>
                  <td className="table__number"><strong>{formatRate(line.unitPrice, '')}</strong>{line.priceOverridden && <span className="muted" title="Fiyat elle girildi"> *</span>}</td>
                  <td className="table__number">{formatMoney(line.amount, currency)}</td>
                  {editable && (
                    <td className="table__actions">
                      <button type="button" className="button button--inline" onClick={() => setDialog({ kind: 'removeLine', line })}>Kaldır</button>
                    </td>
                  )}
                </tr>
              ))}
            </tbody>
            <tfoot>
              <tr>
                <th colSpan={2}>Toplam</th>
                <td className="table__number">{formatQuantity(quotation.totalQuantity)}</td>
                <td colSpan={7} />
                <td className="table__number"><strong>{formatMoney(quotation.totalAmount, currency)}</strong></td>
                {editable && <td />}
              </tr>
            </tfoot>
          </table>
        </div>
      )}

      {dialog?.kind === 'header' && <QuotationFormDialog quotation={quotation} onClose={() => setDialog(null)} />}
      {dialog?.kind === 'line' && (
        <QuotationLineDialog
          quotation={quotation}
          line={dialog.line}
          saving={saveState.isLoading}
          error={saveState.error}
          onClose={() => setDialog(null)}
          onApply={(draft) => void saveLine(dialog.line?.id ?? null, draft)}
        />
      )}
      {dialog?.kind === 'accept' && (
        <QuotationAcceptDialog quotation={quotation} onClose={() => setDialog(null)} onAccepted={() => setDialog(null)} />
      )}
      {dialog?.kind === 'removeLine' && (
        <Modal title="Satırı kaldır" onClose={() => setDialog(null)}>
          <div className="form">
            <p>{dialog.line.lineNo}. satır ({lineTitle(dialog.line)}) tekliften kaldırılacak.</p>
            <footer className="form__actions">
              <button type="button" className="button" onClick={() => setDialog(null)}>Vazgeç</button>
              <button type="button" className="button button--danger" disabled={saveState.isLoading} onClick={() => void removeLine(dialog.line)}>Kaldır</button>
            </footer>
          </div>
        </Modal>
      )}
      {dialog?.kind === 'confirm' && (
        <Modal title={confirmTexts[dialog.action].title} onClose={() => setDialog(null)}>
          <div className="form">
            <p>{confirmTexts[dialog.action].text}</p>
            <footer className="form__actions">
              <button type="button" className="button" onClick={() => setDialog(null)}>Vazgeç</button>
              <button
                type="button"
                className={dialog.action === 'delete' ? 'button button--danger' : 'button button--primary'}
                disabled={actState.isLoading || removeState.isLoading}
                onClick={() => void confirm(dialog.action)}
              >
                {confirmTexts[dialog.action].button}
              </button>
            </footer>
          </div>
        </Modal>
      )}
    </section>
  )
}
