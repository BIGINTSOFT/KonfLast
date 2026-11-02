import { Printer } from 'lucide-react'
import { Link, useParams } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { documentTexts } from '../../utils/documentText'
import { formatDate, formatDateTime, formatQuantity } from '../../utils/format'
import { formatMoney, formatRate } from '../../utils/money'
import { proformaRows, vatBreakdown } from './salesOrderPresentation'
import { useGetSalesOrderProformaQuery } from './salesOrdersApi'
import type { ProformaParty } from './salesOrdersApi'

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
 * Proforma fatura / sipariş teyidi: müşteriye gönderilen, siparişin kalemlerini ve tutarını gösteren belge. Belge
 * metinleri müşterinin belge dilindedir. Tarayıcının yazdır penceresinden kâğıda ya da "PDF olarak kaydet" ile PDF'e alınır.
 */
export function SalesOrderProformaPage() {
  const id = Number(useParams().id)
  const { data, isLoading, error } = useGetSalesOrderProformaQuery(id)

  if (!data) {
    return (
      <section className="page">
        {isLoading && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={error} />
      </section>
    )
  }

  const t = documentTexts(data.documentLanguage)
  const { order } = data
  const rows = proformaRows(order.lines)
  const vat = vatBreakdown(rows)
  const subtotal = vat.reduce((sum, item) => sum + item.base, 0)
  const vatTotal = vat.reduce((sum, item) => sum + item.vat, 0)
  const unpriced = rows.some((row) => row.unitPrice === null)
  const draft = order.status === 'DRAFT'

  return (
    <section className="page proforma-page">
      <div className="no-print proforma-page__bar">
        <Link to={`/sales-orders/${order.id}`}>← Siparişe dön</Link>
        <span className="muted">Yazdır penceresinde hedef olarak &quot;PDF olarak kaydet&quot; seçilirse PDF dosyası oluşur.</span>
        <button type="button" className="button button--primary" onClick={() => window.print()}>
          <Printer aria-hidden="true" />
          Yazdır / PDF
        </button>
      </div>
      {draft && (
        <p className="alert alert--info no-print">Sipariş henüz taslak; belge üzerinde &quot;{t('draft')}&quot; ibaresi yer alır.</p>
      )}

      <article lang={t.lang} className="proforma">
        <header className="proforma__head">
          <div>
            <h1>{t('proforma')}</h1>
            <div className="muted">{t('orderConfirmation')}{draft ? ` · ${t('draft')}` : ''}</div>
          </div>
          <dl className="proforma__meta">
            <div><dt>{t('orderNo')}</dt><dd>{order.orderNo}</dd></div>
            <div><dt>{t('customerOrderNo')}</dt><dd>{order.customerOrderNo ?? '—'}</dd></div>
            <div><dt>{t('orderDate')}</dt><dd>{formatDate(order.orderDate)}</dd></div>
            <div><dt>{t('deliveryDate')}</dt><dd>{formatDate(order.dueDate)}</dd></div>
            <div><dt>{t('currencyLabel')}</dt><dd>{order.currency}</dd></div>
          </dl>
        </header>

        <div className="proforma__parties">
          <Party title={t('seller')} party={data.seller} t={t} />
          <Party title={t('consignee')} party={data.customer} address={data.invoiceAddress} t={t} />
          <section className="proforma__party">
            <h3>{t('deliveryAddress')}</h3>
            <div>{order.deliveryAddress ?? '—'}</div>
            {order.customerBuyerName && <div>Buyer: {order.customerBuyerName}</div>}
          </section>
        </div>

        <table className="proforma__table">
          <thead>
            <tr>
              <th>{t('lineNo')}</th>
              <th>{t('product')}</th>
              <th>{t('article')}</th>
              <th>{t('color')}</th>
              <th>{t('sizeBreakdown')}</th>
              <th className="num">{t('pieces')}</th>
              <th className="num">{t('unitPrice')}</th>
              <th className="num">{t('vat')}</th>
              <th className="num">{t('amount')}</th>
            </tr>
          </thead>
          <tbody>
            {rows.map((row, index) => (
              <tr key={`${row.productCode}-${row.colorName}-${row.unitPrice}`}>
                <td>{index + 1}</td>
                <td><strong>{row.productCode}</strong> {row.productName}</td>
                <td>{row.articleCode ?? ''}</td>
                <td>{row.colorName}</td>
                <td>{row.sizes}</td>
                <td className="num">{formatQuantity(row.quantity)}</td>
                <td className="num">{formatRate(row.unitPrice, '').trim() || '—'}</td>
                <td className="num">%{row.vatRate}</td>
                <td className="num">{formatMoney(row.amount, '').trim() || '—'}</td>
              </tr>
            ))}
          </tbody>
          <tfoot>
            <tr>
              <th colSpan={5}>{t('totalQuantity')}</th>
              <td className="num">{formatQuantity(order.totalQuantity)}</td>
              <td colSpan={3} />
            </tr>
          </tfoot>
        </table>

        <div className="proforma__totals">
          {unpriced && <p className="muted">{t('unpricedNote')}</p>}
          <dl>
            <div><dt>{t('subtotal')}</dt><dd>{formatMoney(subtotal, order.currency)}</dd></div>
            {vat.map((item) => (
              <div key={item.rate}><dt>{t('vat')} %{item.rate} ({t('vatBase')} {formatMoney(item.base, '').trim()})</dt><dd>{formatMoney(item.vat, order.currency)}</dd></div>
            ))}
            <div className="proforma__grand"><dt>{t('grandTotal')}</dt><dd>{formatMoney(subtotal + vatTotal, order.currency)}</dd></div>
          </dl>
        </div>

        {order.notes && (
          <section className="proforma__notes">
            <h3>{t('notes')}</h3>
            <p className="preserve-lines">{order.notes}</p>
          </section>
        )}

        <footer className="proforma__foot">
          <div>{t('proformaNote')} {t('printedAt')}: {formatDateTime(data.printedAt)}</div>
          <div className="proforma__sign">
            <div>{t('sellerApproval')}</div>
            <div>{t('buyerApproval')}</div>
          </div>
        </footer>
      </article>
    </section>
  )
}
