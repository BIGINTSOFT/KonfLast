import { Printer } from 'lucide-react'
import { Link, useParams } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { documentTexts } from '../../utils/documentText'
import { formatDate, formatDateTime, formatQuantity } from '../../utils/format'
import { formatMoney, formatRate } from '../../utils/money'
import type { ProformaParty } from '../salesOrders/salesOrdersApi'
import { useGetQuotationDocumentQuery } from './quotationsApi'

type Texts = ReturnType<typeof documentTexts>

function Party({ title, party, address, t }: { title: string; party: ProformaParty | null; address?: string | null; t: Texts }) {
  return (
    <section className="proforma__party">
      <h3>{title}</h3>
      {party ? (
        <>
          <strong>{party.name}</strong>
          {(address ?? party.address) && <div>{address ?? party.address}</div>}
          {(party.taxOffice || party.taxNumber) && (
            <div>{t('taxOffice')} / {t('taxNumber')}: {[party.taxOffice, party.taxNumber].filter(Boolean).join(' / ')}</div>
          )}
          {(party.phone || party.email) && <div>{[party.phone, party.email].filter(Boolean).join(' · ')}</div>}
        </>
      ) : (
        <div className="muted no-print">Fabrika tanımı yok; satıcı bilgisi Fabrikalar ekranından girilir.</div>
      )}
    </section>
  )
}

/**
 * Basılı teklif: satırlar, adet, birim fiyat ve tutar (maliyet dökümü basılmaz). Belge metinleri müşterinin belge
 * dilindedir; "PDF olarak kaydet" ile PDF alınabilir.
 */
export function QuotationPrintPage() {
  const id = Number(useParams().id)
  const { data, isLoading, error } = useGetQuotationDocumentQuery(id, { skip: !Number.isInteger(id) })

  if (!data) {
    return (
      <section className="page">
        {isLoading && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={error} />
      </section>
    )
  }

  const { quotation } = data
  const t = documentTexts(quotation.documentLanguage)
  const draft = quotation.status === 'DRAFT'

  return (
    <section className="page proforma-page">
      <div className="no-print proforma-page__bar">
        <Link to={`/quotations/${quotation.id}`}>← Teklife dön</Link>
        <span className="muted">Belge müşterinin belge dilinde basılır. Yazdır penceresinde &quot;PDF olarak kaydet&quot; ile PDF alınabilir.</span>
        <button type="button" className="button button--primary" onClick={() => window.print()}>
          <Printer aria-hidden="true" />
          Yazdır / PDF
        </button>
      </div>
      {draft && (
        <p className="alert alert--info no-print">Teklif henüz taslak; belge üzerinde &quot;{t('draft')}&quot; ibaresi yer alır.</p>
      )}

      <article lang={t.lang} className="proforma">
        <header className="proforma__head">
          <div>
            <h1>{t('konfQuote')}</h1>
            <div className="muted">{quotation.quotationNo}{draft ? ` · ${t('draft')}` : ''}</div>
          </div>
          <dl className="proforma__meta">
            <div><dt>{t('konfQuoteNo')}</dt><dd>{quotation.quotationNo}</dd></div>
            <div><dt>{t('date')}</dt><dd>{formatDate(quotation.quotationDate)}</dd></div>
            <div><dt>{t('konfQuoteValidUntil')}</dt><dd>{formatDate(quotation.validUntil)}</dd></div>
            <div><dt>{t('currencyLabel')}</dt><dd>{quotation.currency}</dd></div>
          </dl>
        </header>

        <div className="proforma__parties">
          <Party title={t('seller')} party={data.seller} t={t} />
          <Party title={t('customer')} party={data.customer} address={data.customerAddress} t={t} />
        </div>

        <table className="proforma__table">
          <thead>
            <tr>
              <th>{t('lineNo')}</th>
              <th>{t('product')}</th>
              <th>{t('konfQuoteModelReference')}</th>
              <th>{t('color')}</th>
              <th className="num">{t('pieces')}</th>
              <th className="num">{t('unitPrice')}</th>
              <th className="num">{t('amount')}</th>
            </tr>
          </thead>
          <tbody>
            {quotation.lines.map((line) => (
              <tr key={line.id}>
                <td>{line.lineNo}</td>
                <td>
                  {line.productCode && <strong>{line.productCode} </strong>}
                  {line.description ?? line.productName ?? ''}
                </td>
                <td>{line.modelReference ?? line.modelCode ?? ''}</td>
                <td>{line.colors ?? ''}</td>
                <td className="num">{formatQuantity(line.quantity)}</td>
                <td className="num">{formatRate(line.unitPrice, '').trim()}</td>
                <td className="num">{formatMoney(line.amount, '').trim()}</td>
              </tr>
            ))}
          </tbody>
          <tfoot>
            <tr>
              <th colSpan={4}>{t('totalQuantity')}</th>
              <td className="num">{formatQuantity(quotation.totalQuantity)}</td>
              <td colSpan={2} />
            </tr>
          </tfoot>
        </table>

        <div className="proforma__totals">
          <p className="muted">{t('konfQuoteVatNote')}</p>
          <dl>
            <div className="proforma__grand"><dt>{t('total')}</dt><dd>{formatMoney(quotation.totalAmount, quotation.currency)}</dd></div>
          </dl>
        </div>

        {quotation.notes && (
          <section className="proforma__notes">
            <h3>{t('notes')}</h3>
            <p className="preserve-lines">{quotation.notes}</p>
          </section>
        )}

        <footer className="proforma__foot">
          <div>{t('konfQuoteValidityNote')} {formatDate(quotation.validUntil)}. {t('printedAt')}: {formatDateTime(data.printedAt)}</div>
          <div className="proforma__sign">
            <div>{t('sellerApproval')}</div>
            <div>{t('customerApproval')}</div>
          </div>
        </footer>
      </article>
    </section>
  )
}
