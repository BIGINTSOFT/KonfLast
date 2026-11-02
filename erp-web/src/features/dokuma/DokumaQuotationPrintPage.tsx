import { Printer } from 'lucide-react'
import { Link, useParams } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { documentTexts } from '../../utils/documentText'
import { formatDate, formatQuantity } from '../../utils/format'
import { formatMoney, formatRate } from '../../utils/money'
import { useGetDokumaQuotationPrintQuery } from './dokumaQuotationsApi'

/**
 * Müşteriye basılan dokuma teklifi: artikel, metre ve metre fiyatı; maliyet dökümü basılmaz. Metinler teklifin belge dilindedir.
 * Proforma ile aynı düzen; firma bilgisi antetli kâğıttan gelir.
 */
export function DokumaQuotationPrintPage() {
  const id = Number(useParams().id)
  const { data, isLoading, error } = useGetDokumaQuotationPrintQuery(id)

  if (!data) {
    return (
      <section className="page">
        <p><Link to={`/dokuma/quotations/${id}`}>← Teklife dön</Link></p>
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
        <Link to={`/dokuma/quotations/${id}`}>← Teklife dön</Link>
        <span className="muted">Yazdır penceresinde hedef olarak &quot;PDF olarak kaydet&quot; seçilirse PDF dosyası oluşur.</span>
        <button type="button" className="button button--primary" onClick={() => window.print()}>
          <Printer aria-hidden="true" />
          Yazdır / PDF
        </button>
      </div>
      {draft && <p className="alert alert--info no-print">Teklif henüz taslak; belge üzerinde &quot;{t('draft')}&quot; ibaresi yer alır.</p>}

      <article lang={t.lang} className="proforma">
        {/* Antet alanı: firma bilgisi antetli kâğıttan gelir. */}
        <div style={{ minHeight: '16mm' }}>
          <span className="muted no-print">Antet alanı: firma bilgisi için antetli kâğıda basın.</span>
        </div>
        <header className="proforma__head">
          <div>
            <h1>{t('dokumaQuote')}</h1>
            {draft && <div className="muted">{t('draft')}</div>}
          </div>
          <dl className="proforma__meta">
            <div><dt>{t('dokumaQuoteNo')}</dt><dd>{data.quotationNo}</dd></div>
            <div><dt>{t('dokumaQuoteDate')}</dt><dd>{formatDate(data.quotationDate)}</dd></div>
            <div><dt>{t('dokumaQuoteValidUntil')}</dt><dd>{formatDate(data.validUntil)}</dd></div>
            <div><dt>{t('paymentTerms')}</dt><dd>{data.paymentTermDays === null ? '—' : `${data.paymentTermDays} ${t('days')}`}</dd></div>
            <div><dt>{t('dokumaQuoteDelivery')}</dt><dd>{data.deliveryTerms ?? '—'}</dd></div>
            <div><dt>{t('currencyLabel')}</dt><dd>{data.currency}</dd></div>
          </dl>
        </header>

        <div className="proforma__parties" style={{ gridTemplateColumns: '1fr' }}>
          <section className="proforma__party">
            <h3>{t('customer')}</h3>
            <strong>{data.partyName}</strong>
            {data.customerCode && <div>{t('customerCode')}: {data.customerCode}</div>}
            {data.customerAddress && <div>{t('address')}: {data.customerAddress}</div>}
            {(data.customerTaxOffice || data.customerTaxNumber) && (
              <div>{t('taxOffice')} / {t('taxNumber')}: {[data.customerTaxOffice, data.customerTaxNumber].filter(Boolean).join(' / ')}</div>
            )}
            {data.contactName && <div>{t('dokumaQuoteContact')}: {data.contactName}</div>}
          </section>
        </div>

        <p>{t('dokumaQuoteIntro')}</p>

        <table className="proforma__table">
          <thead>
            <tr>
              <th>#</th>
              <th>{t('article')}</th>
              <th>{t('customerCode')}</th>
              <th>{t('dokumaQuoteComposition')}</th>
              <th className="num">{t('width')} (cm)</th>
              <th className="num">{t('weight')}</th>
              <th className="num">{t('meters')}</th>
              <th className="num">{t('dokumaQuotePricePerMeter')}</th>
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
                <td>{[line.composition, line.weaveName].filter(Boolean).join(' / ')}</td>
                <td className="num">{formatQuantity(line.rawWidthCm)}</td>
                <td className="num">{line.rawWeightGsm}</td>
                <td className="num">{formatQuantity(line.meters)}</td>
                <td className="num">{formatRate(line.unitPrice, data.currency)}</td>
                <td className="num">{money(line.amount)}</td>
              </tr>
            ))}
          </tbody>
          <tfoot>
            <tr>
              <th colSpan={6}>{t('total')}</th>
              <td className="num">{formatQuantity(data.totalMeters)}</td>
              <td />
              <td className="num">{money(data.total)}</td>
            </tr>
          </tfoot>
        </table>

        <div className="proforma__totals">
          <p className="muted">{t('dokumaQuoteVatExcluded')}</p>
          <p className="muted">{data.ownership === 'COMMISSION' ? t('dokumaQuoteCommissionNote') : t('dokumaQuoteOwnGoodsNote')}</p>
        </div>

        {data.notes && (
          <section className="proforma__notes">
            <h3>{t('notes')}</h3>
            <p className="preserve-lines">{data.notes}</p>
          </section>
        )}

        <section className="proforma__notes">
          <h3>{t('validity')}</h3>
          <p>{t('dokumaQuoteValidityNote')}</p>
        </section>

        <footer className="proforma__foot">
          <div>{t('dokumaQuoteFootNote')}</div>
          <div className="proforma__sign" style={{ gridTemplateColumns: 'repeat(2, 1fr)' }}>
            <div>{t('preparedBy')}</div>
            <div>{t('customerApproval')}</div>
          </div>
        </footer>
      </article>
    </section>
  )
}
