import { Printer } from 'lucide-react'
import { Link, useParams } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { formatDate, formatDateTime, formatQuantity } from '../../utils/format'
import { documentTexts } from '../../utils/documentText'
import { Barcode } from '../labels/LabelPrintPage'
import type { ProformaParty } from '../salesOrders/salesOrdersApi'
import { useGetSubcontractSendDocumentQuery } from './subcontractApi'
import type { SubcontractSendLine } from './subcontractApi'

// Fasoncu belgeleri Türkçe basılır.
const t = documentTexts('TURKISH')

function Party({ title, party }: { title: string; party: ProformaParty | null }) {
  return (
    <section className="proforma__party">
      <h3>{title}</h3>
      {party ? (
        <>
          <strong>{party.name}</strong>
          {party.address && <div>{party.address}</div>}
          {party.taxNumber && <div>{t('taxNumber')}: {party.taxNumber}</div>}
          {(party.phone || party.email) && <div>{[party.phone, party.email].filter(Boolean).join(' · ')}</div>}
        </>
      ) : (
        <div className="muted no-print">Fabrika tanımı yok; gönderen bilgisi Fabrikalar ekranından girilir.</div>
      )}
    </section>
  )
}

interface Group {
  key: string
  productCode: string
  productName: string
  colorName: string
  bundles: string[]
  sizes: { name: string; order: number; quantity: number }[]
  quantity: number
}

/** Satırları ürün + renk bazında toplar; bedenler sıra numarasıyla, demet numaraları listelenir. */
function groupLines(lines: SubcontractSendLine[]): Group[] {
  const groups = new Map<string, Group>()
  for (const line of lines) {
    const key = `${line.productCode}|${line.colorName}`
    const group = groups.get(key) ?? {
      key, productCode: line.productCode, productName: line.productName, colorName: line.colorName, bundles: [], sizes: [], quantity: 0,
    }
    const size = group.sizes.find((item) => item.name === line.sizeName)
    if (size) {
      size.quantity += line.quantity
    } else {
      group.sizes.push({ name: line.sizeName, order: line.sizeSortOrder, quantity: line.quantity })
    }
    if (line.bundleNo) {
      group.bundles.push(line.bundleNo)
    }
    group.quantity += line.quantity
    groups.set(key, group)
  }
  return [...groups.values()].map((group) => ({ ...group, sizes: [...group.sizes].sort((a, b) => a.order - b.order) }))
}

/** Basılı fason gönderim irsaliyesi (resmî e-İrsaliye değildir); fasoncuya Türkçe basılır. */
export function SubcontractSendPrintPage() {
  const id = Number(useParams().id)
  const { data, error, isLoading } = useGetSubcontractSendDocumentQuery(id, { skip: !Number.isInteger(id) })

  return (
    <section className="page proforma-page">
      <style>{'@page { size: A4 portrait; }'}</style>
      <div className="no-print proforma-page__bar">
        <Link to={`/subcontract/sends/${id}`}>← Gönderime dön</Link>
        <span className="muted">Fason gönderim irsaliyesi · &quot;PDF olarak kaydet&quot; ile PDF alınabilir.</span>
        <button type="button" className="button button--primary" disabled={!data} onClick={() => window.print()}>
          <Printer aria-hidden="true" />
          Yazdır / PDF
        </button>
      </div>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {data && (() => {
        const { send } = data
        const groups = groupLines(send.lines)
        return (
          <article lang={t.lang} className="proforma">
            <header className="proforma__head">
              <div>
                <h1>{t('despatch')}</h1>
                <div className="muted">{t('konfSubcontractSend')} · {send.operationName}{send.status === 'CANCELLED' ? ' · İPTAL' : ''}</div>
              </div>
              <div>
                <Barcode text={send.sendNo} />
                <dl className="proforma__meta">
                  <div><dt>{t('despatchNo')}</dt><dd>{send.sendNo}</dd></div>
                  <div><dt>{t('date')}</dt><dd>{formatDate(send.sendDate)}</dd></div>
                  <div><dt>{t('process')}</dt><dd>{send.operationCode} — {send.operationName}</dd></div>
                </dl>
              </div>
            </header>

            <div className="proforma__parties">
              <Party title={t('sender')} party={data.sender} />
              <Party title={t('consignee')} party={data.receiver} />
              <section className="proforma__party">
                <h3>{t('carrierVehicle')}</h3>
                <div>{send.carrier ?? '—'}</div>
              </section>
            </div>

            <table className="proforma__table">
              <thead>
                <tr>
                  <th>{t('lineNo')}</th>
                  <th>{t('product')}</th>
                  <th>{t('color')}</th>
                  <th>{t('sizeBreakdown')}</th>
                  <th>{t('konfSubcontractBundles')}</th>
                  <th className="num">{t('pieces')}</th>
                </tr>
              </thead>
              <tbody>
                {groups.map((group, index) => (
                  <tr key={group.key}>
                    <td>{index + 1}</td>
                    <td><strong>{group.productCode}</strong> {group.productName}</td>
                    <td>{group.colorName}</td>
                    <td>{group.sizes.map((size) => `${size.name}: ${formatQuantity(size.quantity)}`).join('  ')}</td>
                    <td>{group.bundles.length > 0 ? group.bundles.join(', ') : '—'}</td>
                    <td className="num">{formatQuantity(group.quantity)}</td>
                  </tr>
                ))}
              </tbody>
              <tfoot>
                <tr>
                  <th colSpan={5}>{t('total')}</th>
                  <td className="num">{formatQuantity(send.totalQuantity)}</td>
                </tr>
              </tfoot>
            </table>

            {send.notes && (
              <section className="proforma__notes">
                <h3>{t('notes')}</h3>
                <p className="preserve-lines">{send.notes}</p>
              </section>
            )}

            <footer className="proforma__foot">
              <div>{t('konfSubcontractReturnNote')} {t('notOfficial')} {t('printedAt')}: {formatDateTime(data.printedAt)}</div>
              <div className="proforma__sign">
                <div>{t('deliveredBy')}</div>
                <div>{t('receivedBy')}</div>
              </div>
            </footer>
          </article>
        )
      })()}
    </section>
  )
}
