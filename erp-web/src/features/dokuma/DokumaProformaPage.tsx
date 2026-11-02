import { Printer } from 'lucide-react'
import { Link, useParams } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { documentTexts } from '../../utils/documentText'
import { formatDate, formatQuantity } from '../../utils/format'
import { formatMoney } from '../../utils/money'
import { useGetDokumaProformaQuery } from './dokumaOrdersApi'

/**
 * Dokuma proforma / sipariş teyidi: taslak ve onaylı siparişin satırları, KDV'li tutarı ve imza alanları. Metinler müşterinin
 * belge dilindedir. KDV müşteri kartındaki varsayılan orandan gelir; oran yoksa %0 yazılır ve not düşülür. Firma kartı
 * olmadığı için üstte antet alanı boş bırakılır; antetli kâğıda basılır.
 */
export function DokumaProformaPage() {
  const id = Number(useParams().id)
  const { data, isLoading, error } = useGetDokumaProformaQuery(id)

  if (!data) {
    return (
      <section className="page">
        <p><Link to={`/dokuma/orders/${id}`}>← Siparişe dön</Link></p>
        {isLoading && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={error} />
      </section>
    )
  }

  const t = documentTexts(data.documentLanguage)
  const draft = data.status === 'DRAFT'
  const money = (value: number) => formatMoney(value, data.currency)

  return (
    <section className="page proforma-page">
      <div className="no-print proforma-page__bar">
        <Link to={`/dokuma/orders/${id}`}>← Siparişe dön</Link>
        <span className="muted">Yazdır penceresinde hedef olarak &quot;PDF olarak kaydet&quot; seçilirse PDF dosyası oluşur.</span>
        <button type="button" className="button button--primary" onClick={() => window.print()}>
          <Printer aria-hidden="true" />
          Yazdır / PDF
        </button>
      </div>
      {draft && <p className="alert alert--info no-print">Sipariş henüz taslak; belge üzerinde &quot;{t('draft')}&quot; ibaresi yer alır.</p>}

      <article lang={t.lang} className="proforma">
        {/* Antet alanı: firma bilgisi antetli kâğıttan gelir. */}
        <div style={{ minHeight: '16mm' }}>
          <span className="muted no-print">Antet alanı: firma bilgisi için antetli kâğıda basın.</span>
        </div>
        <header className="proforma__head">
          <div>
            <h1>{t('proforma')}</h1>
            <div className="muted">{t('orderConfirmation')}{draft && ` · ${t('draft')}`}</div>
          </div>
          <dl className="proforma__meta">
            <div><dt>{t('orderNo')}</dt><dd>{data.orderNo}</dd></div>
            <div><dt>{t('customerOrderNo')}</dt><dd>{data.customerOrderNo ?? '—'}</dd></div>
            <div><dt>{t('orderDate')}</dt><dd>{formatDate(data.orderDate)}</dd></div>
            <div><dt>{t('deliveryDate')}</dt><dd>{data.dueDate ? formatDate(data.dueDate) : '—'}</dd></div>
            <div><dt>{t('paymentTerms')}</dt><dd>{data.paymentTermDays === null ? '—' : `${data.paymentTermDays} ${t('days')}`}</dd></div>
          </dl>
        </header>

        <div className="proforma__parties" style={{ gridTemplateColumns: '1fr' }}>
          <section className="proforma__party">
            <h3>{t('customer')}</h3>
            {/* Stoğa üretim siparişinde müşteri yoktur; teyit yine basılabilir. */}
            <strong>{data.customerName ?? t('stockProduction')}</strong>
            {data.customerCode && <div>{t('customerCode')}: {data.customerCode}</div>}
            {data.customerAddress && <div>{t('address')}: {data.customerAddress}</div>}
            {(data.customerTaxOffice || data.customerTaxNumber) && (
              <div>{t('taxOffice')} / {t('taxNumber')}: {[data.customerTaxOffice, data.customerTaxNumber].filter(Boolean).join(' / ')}</div>
            )}
          </section>
        </div>

        <table className="proforma__table">
          <thead>
            <tr>
              <th>#</th>
              <th>{t('article')}</th>
              <th>{t('customerCode')}</th>
              <th className="num">{t('width')} (cm)</th>
              <th className="num">{t('weight')}</th>
              <th className="num">{t('meters')}</th>
              <th className="num">{t('unitPrice')}</th>
              <th className="num">{t('amount')}</th>
            </tr>
          </thead>
          <tbody>
            {data.lines.map((line, index) => (
              <tr key={index}>
                <td>{index + 1}</td>
                <td>
                  <strong>{line.articleCode}</strong> {line.articleName}
                  {line.notes && <div className="muted">{line.notes}</div>}
                </td>
                <td>{line.customerArticleCode ?? ''}</td>
                <td className="num">{formatQuantity(line.rawWidthCm)}</td>
                <td className="num">{line.rawWeightGsm}</td>
                <td className="num">{formatQuantity(line.meters)}</td>
                <td className="num">{line.unitPrice === null ? '—' : money(line.unitPrice)}</td>
                <td className="num">{line.amount === null ? '—' : money(line.amount)}</td>
              </tr>
            ))}
          </tbody>
          <tfoot>
            <tr>
              <th colSpan={5}>{t('total')}</th>
              <td className="num">{formatQuantity(data.totalMeters)}</td>
              <td colSpan={2} />
            </tr>
          </tfoot>
        </table>

        <div className="proforma__totals">
          {data.hasMissingPrices && <p className="muted">{t('unpricedNote')}</p>}
          {data.vatRate === null && <p className="muted">{t('vatRateMissing')}</p>}
          <dl>
            <div><dt>{t('subtotal')}</dt><dd>{money(data.subtotal)}</dd></div>
            <div><dt>{t('vat')} %{data.vatRate ?? 0}</dt><dd>{money(data.vatAmount)}</dd></div>
            <div className="proforma__grand"><dt>{t('grandTotal')}</dt><dd>{money(data.total)}</dd></div>
          </dl>
        </div>

        {data.notes && (
          <section className="proforma__notes">
            <h3>{t('notes')}</h3>
            <p className="preserve-lines">{data.notes}</p>
          </section>
        )}

        <section className="proforma__notes">
          <h3>{t('validity')}</h3>
          <p>{t('proformaValidity')}</p>
        </section>

        <footer className="proforma__foot">
          <div>{t('proformaNote')}</div>
          <div className="proforma__sign" style={{ gridTemplateColumns: 'repeat(3, 1fr)' }}>
            <div>{t('preparedBy')}</div>
            <div>{t('approvedBy')}</div>
            <div>{t('customerApproval')}</div>
          </div>
        </footer>
      </article>
    </section>
  )
}
