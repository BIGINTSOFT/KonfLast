import { Printer } from 'lucide-react'
import type { ReactNode } from 'react'
import { Link, useParams } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { formatDate, formatDateTime, formatQuantity } from '../../utils/format'
import { documentTexts } from '../../utils/documentText'
import { Barcode } from '../labels/LabelPrintPage'
import type { ProformaParty } from '../salesOrders/salesOrdersApi'
import { stockUnitLabels } from '../stock/stockPresentation'
import { useGetShipmentDocumentQuery } from './shippingApi'
import type { ShipmentDocument, ShipmentDocumentItem } from './shippingApi'

/** Bir koli ya da sevkiyat içinde ürün-renk satırı: bedenler ve adetleri. */
interface VariantGroup {
  key: string
  productCode: string
  productName: string | null
  articleCode: string | null
  colorName: string
  sizes: { name: string; order: number; quantity: number }[]
  quantity: number
}

/** Satırları ürün + renk bazında toplar; bedenler sıra numarasıyla dizilir. */
function groupVariants(lines: { stockItemId: number; quantity: number }[], items: Map<number, ShipmentDocumentItem>): VariantGroup[] {
  const groups = new Map<string, VariantGroup>()
  for (const line of lines) {
    const item = items.get(line.stockItemId)
    const productCode = item?.productCode ?? item?.itemCode ?? String(line.stockItemId)
    const colorName = item?.colorName ?? '—'
    const key = `${productCode}|${item?.colorCode ?? colorName}`
    const group: VariantGroup = groups.get(key) ?? {
      key, productCode, productName: item?.productName ?? null, articleCode: item?.customerArticleCode ?? null, colorName, sizes: [], quantity: 0,
    }
    group.sizes.push({ name: item?.sizeName ?? item?.sizeCode ?? '—', order: item?.sizeOrder ?? 0, quantity: line.quantity })
    group.quantity += line.quantity
    groups.set(key, group)
  }
  return [...groups.values()]
    .map((group) => ({ ...group, sizes: [...group.sizes].sort((a, b) => a.order - b.order) }))
    .sort((a, b) => a.productCode.localeCompare(b.productCode) || a.colorName.localeCompare(b.colorName))
}

const sizeText = (group: VariantGroup) => group.sizes.map((size) => `${size.name}: ${formatQuantity(size.quantity)}`).join('  ')

const itemMap = (document: ShipmentDocument) => new Map(document.items.map((item) => [item.stockItemId, item]))

const sum = (values: (number | null)[]) => values.reduce<number>((total, value) => total + (value ?? 0), 0)

function Party({ title, party, t }: { title: string; party: ProformaParty | null; t: ReturnType<typeof documentTexts> }) {
  return (
    <section className="proforma__party">
      <h3>{title}</h3>
      {party ? (
        <>
          <strong>{party.name}</strong>
          {party.address && <div>{party.address}</div>}
          {party.taxOffice && <div>{t('taxOffice')}: {party.taxOffice}</div>}
          {party.taxNumber && <div>{t('taxNumber')}: {party.taxNumber}</div>}
          {(party.phone || party.email) && <div>{[party.phone, party.email].filter(Boolean).join(' · ')}</div>}
        </>
      ) : (
        <div className="muted no-print">Fabrika tanımı yok; gönderen bilgisi Fabrikalar ekranından girilir.</div>
      )}
    </section>
  )
}

/** Belge sayfalarının ortak çerçevesi: geri dönüş, yazdır düğmesi, yükleme ve hata. */
function PrintFrame({ id, title, page, children }: { id: number; title: string; page: string; children: (document: ShipmentDocument) => ReactNode }) {
  const { data, error, isLoading } = useGetShipmentDocumentQuery(id, { skip: !Number.isInteger(id) })
  return (
    <section className="page proforma-page label-page">
      <style>{`@page { size: ${page}; }`}</style>
      <div className="no-print proforma-page__bar">
        <Link to={`/shipments/${id}`}>← Sevkiyata dön</Link>
        <span className="muted">{title} · Belge müşterinin belge dilinde basılır. &quot;PDF olarak kaydet&quot; ile PDF alınabilir.</span>
        <button type="button" className="button button--primary" disabled={!data} onClick={() => window.print()}>
          <Printer aria-hidden="true" />
          Yazdır / PDF
        </button>
      </div>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {data && data.status === 'DRAFT' && (
        <p className="alert alert--info no-print">Sevkiyat henüz taslak; sevk edilmeden önce koli ve adetler değişebilir.</p>
      )}
      {data && children(data)}
    </section>
  )
}

/** Çeki listesi (packing list): koli koli renk / beden dağılımı, adet ve ağırlık; altta toplamlar. */
export function ShipmentPackingListPage() {
  const id = Number(useParams().id)
  return (
    <PrintFrame id={id} title="Çeki listesi" page="A4 portrait">
      {(document) => {
        const t = documentTexts(document.documentLanguage)
        const items = itemMap(document)
        const cartons = document.cartons.map((carton) => ({ carton, groups: groupVariants(carton.lines, items) }))
        const totalPieces = sum(document.cartons.flatMap((carton) => carton.lines.map((line) => line.quantity)))
        const overall = groupVariants(document.cartons.flatMap((carton) => carton.lines), items)
        return (
          <article lang={t.lang} className="proforma">
            <header className="proforma__head">
              <div>
                <h1>{t('packingList')}</h1>
                <div className="muted">{document.shipmentNo}{document.status === 'DRAFT' ? ` · ${t('draft')}` : ''}</div>
              </div>
              <dl className="proforma__meta">
                <div><dt>{t('shipmentNo')}</dt><dd>{document.shipmentNo}</dd></div>
                <div><dt>{t('despatchNo')}</dt><dd>{document.documentNo ?? '—'}</dd></div>
                <div><dt>{t('shipDate')}</dt><dd>{formatDate(document.shipDate)}</dd></div>
                <div><dt>{t('orderNo')}</dt><dd>{document.salesOrderNo ?? document.productionOrderNo ?? '—'}</dd></div>
                <div><dt>{t('customerOrderNo')}</dt><dd>{document.customerOrderNo ?? '—'}</dd></div>
              </dl>
            </header>

            <div className="proforma__parties">
              <Party title={t('sender')} party={document.seller} t={t} />
              <Party title={t('consignee')} party={{ ...document.customer, address: document.invoiceAddress }} t={t} />
              <section className="proforma__party">
                <h3>{t('deliveryAddress')}</h3>
                <div>{document.deliveryAddress ?? '—'}</div>
              </section>
            </div>

            {document.cartons.length === 0 ? (
              <p className="alert alert--info">Bu sevkiyatta koli yok; önce sevkiyat ekranında kolileri oluşturun.</p>
            ) : (
              <table className="proforma__table">
                <thead>
                  <tr>
                    <th>{t('cartonNo')}</th>
                    <th>{t('product')}</th>
                    <th>{t('article')}</th>
                    <th>{t('color')}</th>
                    <th>{t('sizeBreakdown')}</th>
                    <th className="num">{t('pieces')}</th>
                    <th className="num">{t('netKilograms')}</th>
                    <th className="num">{t('grossKilograms')}</th>
                  </tr>
                </thead>
                <tbody>
                  {cartons.flatMap(({ carton, groups }) => groups.map((group, index) => (
                    <tr key={`${carton.cartonNo}-${group.key}`}>
                      {index === 0 && <td rowSpan={groups.length}><strong>{carton.cartonNo}</strong></td>}
                      <td><strong>{group.productCode}</strong> {group.productName}</td>
                      <td>{group.articleCode ?? ''}</td>
                      <td>{group.colorName}</td>
                      <td>{sizeText(group)}</td>
                      <td className="num">{formatQuantity(group.quantity)}</td>
                      {index === 0 && <td className="num" rowSpan={groups.length}>{carton.netKg === null ? '' : formatQuantity(carton.netKg)}</td>}
                      {index === 0 && <td className="num" rowSpan={groups.length}>{carton.grossKg === null ? '' : formatQuantity(carton.grossKg)}</td>}
                    </tr>
                  )))}
                </tbody>
                <tfoot>
                  <tr>
                    <th colSpan={5}>{t('total')} · {t('cartons')}: {document.cartons.length}</th>
                    <td className="num">{formatQuantity(totalPieces)}</td>
                    <td className="num">{formatQuantity(sum(document.cartons.map((carton) => carton.netKg)))}</td>
                    <td className="num">{formatQuantity(sum(document.cartons.map((carton) => carton.grossKg)))}</td>
                  </tr>
                </tfoot>
              </table>
            )}

            {overall.length > 0 && (
              <table className="proforma__table">
                <thead>
                  <tr>
                    <th>{t('product')}</th>
                    <th>{t('color')}</th>
                    <th>{t('sizeBreakdown')}</th>
                    <th className="num">{t('totalQuantity')}</th>
                  </tr>
                </thead>
                <tbody>
                  {overall.map((group) => (
                    <tr key={group.key}>
                      <td><strong>{group.productCode}</strong> {group.productName}</td>
                      <td>{group.colorName}</td>
                      <td>{sizeText(mergeSizes(group))}</td>
                      <td className="num">{formatQuantity(group.quantity)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}

            {document.notes && (
              <section className="proforma__notes">
                <h3>{t('notes')}</h3>
                <p className="preserve-lines">{document.notes}</p>
              </section>
            )}

            <footer className="proforma__foot">
              <div>{t('printedAt')}: {formatDateTime(document.printedAt)}</div>
              <div className="proforma__sign">
                <div>{t('preparedBy')}</div>
                <div>{t('approvedBy')}</div>
              </div>
            </footer>
          </article>
        )
      }}
    </PrintFrame>
  )
}

/** Birden fazla koliden gelen aynı bedenleri birleştirir (genel özet için). */
function mergeSizes(group: VariantGroup): VariantGroup {
  const sizes = new Map<string, { name: string; order: number; quantity: number }>()
  for (const size of group.sizes) {
    const current = sizes.get(size.name)
    sizes.set(size.name, current ? { ...current, quantity: current.quantity + size.quantity } : { ...size })
  }
  return { ...group, sizes: [...sizes.values()].sort((a, b) => a.order - b.order) }
}

/**
 * Koli etiketleri: koli başına bir A6 etiket. Barkod koli kodunu taşır ({sevkiyat no}-{koli no}); yükleme el
 * terminalinde bu barkod okutularak kontrol edilir.
 */
export function ShipmentCartonLabelsPage() {
  const id = Number(useParams().id)
  return (
    <PrintFrame id={id} title="Koli etiketleri (A6)" page="A6 portrait; margin: 0">
      {(document) => {
        const t = documentTexts(document.documentLanguage)
        const items = itemMap(document)
        const total = document.cartons.length
        if (total === 0) {
          return <p className="alert alert--info">Bu sevkiyatta koli yok; önce sevkiyat ekranında kolileri oluşturun.</p>
        }
        return (
          <div className="label-sheet">
            {document.cartons.map((carton) => {
              const groups = groupVariants(carton.lines, items)
              return (
                <article
                  key={carton.cartonNo}
                  className="batch-card"
                  style={{ width: '105mm', minHeight: '148mm', boxSizing: 'border-box', breakAfter: 'page', display: 'flex', flexDirection: 'column', gap: '2mm' }}
                >
                  <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '8pt' }}>
                    <span>{document.seller?.name ?? ''}</span>
                    <span>{formatDate(document.shipDate)}</span>
                  </div>
                  <div className="batch-card__number">{t('carton')} {carton.cartonNo} / {total}</div>
                  <div>
                    <strong>{document.customer.name}</strong>
                    {document.customerOrderNo && <div>{t('customerOrderNo')}: <strong>{document.customerOrderNo}</strong></div>}
                    {document.salesOrderNo && <div>{t('orderNo')}: {document.salesOrderNo}</div>}
                  </div>
                  {groups.map((group) => (
                    <div key={group.key}>
                      <div><strong>{group.productCode}</strong> {group.productName}</div>
                      {group.articleCode && <div>{t('article')}: <strong>{group.articleCode}</strong></div>}
                      <div>{t('color')}: <strong>{group.colorName}</strong></div>
                      <table className="batch-card__table">
                        <thead>
                          <tr>
                            {group.sizes.map((size) => <th key={size.name}>{size.name}</th>)}
                            <th>{t('total')}</th>
                          </tr>
                        </thead>
                        <tbody>
                          <tr>
                            {group.sizes.map((size) => <td key={size.name}>{formatQuantity(size.quantity)}</td>)}
                            <td><strong>{formatQuantity(group.quantity)}</strong></td>
                          </tr>
                        </tbody>
                      </table>
                    </div>
                  ))}
                  <div>
                    {t('pieces')}: <strong>{formatQuantity(sum(carton.lines.map((line) => line.quantity)))}</strong>
                    {carton.netKg !== null && <> · {t('netKilograms')}: {formatQuantity(carton.netKg)}</>}
                    {carton.grossKg !== null && <> · {t('grossKilograms')}: {formatQuantity(carton.grossKg)}</>}
                  </div>
                  <div style={{ marginTop: 'auto' }}>
                    <Barcode text={carton.code} />
                    <div style={{ textAlign: 'center', fontFamily: 'var(--font-mono)' }}>{carton.code}</div>
                  </div>
                </article>
              )
            })}
          </div>
        )
      }}
    </PrintFrame>
  )
}

/** Basılı irsaliye (resmî e-İrsaliye değildir): taraflar, teslim adresi, taşıyıcı, ürün / renk / beden adetleri. */
export function ShipmentDespatchNotePage() {
  const id = Number(useParams().id)
  return (
    <PrintFrame id={id} title="İrsaliye" page="A4 portrait">
      {(document) => {
        const t = documentTexts(document.documentLanguage)
        const items = itemMap(document)
        const groups = groupVariants(
          document.items.filter((item) => item.quantity > 0).map((item) => ({ stockItemId: item.stockItemId, quantity: item.quantity })),
          items,
        )
        // Birimler farklıysa (nadir) toplam satırı adet olarak yazılır; mamuller adetle izlenir.
        const units = [...new Set(document.items.map((item) => item.unit))]
        const unitLabel = units.length === 1 ? stockUnitLabels[units[0]] : ''
        const totalQuantity = sum(groups.map((group) => group.quantity))
        const gross = sum(document.cartons.map((carton) => carton.grossKg))
        return (
          <article lang={t.lang} className="proforma">
            <header className="proforma__head">
              <div>
                <h1>{t('despatch')}</h1>
                <div className="muted">{document.documentNo ?? document.shipmentNo}{document.status === 'DRAFT' ? ` · ${t('draft')}` : ''}</div>
              </div>
              <div>
                <Barcode text={document.documentNo ?? document.shipmentNo} />
                <dl className="proforma__meta">
                  <div><dt>{t('despatchNo')}</dt><dd>{document.documentNo ?? document.shipmentNo}</dd></div>
                  <div><dt>{t('date')}</dt><dd>{formatDate(document.shipDate)}</dd></div>
                  <div><dt>{t('orderNo')}</dt><dd>{document.salesOrderNo ?? document.productionOrderNo ?? '—'}</dd></div>
                  <div><dt>{t('customerOrderNo')}</dt><dd>{document.customerOrderNo ?? '—'}</dd></div>
                  <div><dt>{t('customerCode')}</dt><dd>{document.customerCode}</dd></div>
                </dl>
              </div>
            </header>

            <div className="proforma__parties">
              <Party title={t('sender')} party={document.seller} t={t} />
              <Party title={t('consignee')} party={{ ...document.customer, address: document.invoiceAddress }} t={t} />
              <section className="proforma__party">
                <h3>{t('deliveryAddress')}</h3>
                <div>{document.deliveryAddress ?? document.invoiceAddress ?? '—'}</div>
                <h3>{t('carrierVehicle')}</h3>
                <div>{document.carrier ?? '—'}</div>
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
                  <th className="num">{t('quantity')}</th>
                </tr>
              </thead>
              <tbody>
                {groups.map((group, index) => (
                  <tr key={group.key}>
                    <td>{index + 1}</td>
                    <td><strong>{group.productCode}</strong> {group.productName}</td>
                    <td>{group.articleCode ?? ''}</td>
                    <td>{group.colorName}</td>
                    <td>{sizeText(group)}</td>
                    <td className="num">{formatQuantity(group.quantity)} {unitLabel}</td>
                  </tr>
                ))}
              </tbody>
              <tfoot>
                <tr>
                  <th colSpan={5}>
                    {t('total')}
                    {document.cartons.length > 0 && ` · ${t('cartons')}: ${document.cartons.length}`}
                    {gross > 0 && ` · ${t('grossKilograms')}: ${formatQuantity(gross)}`}
                  </th>
                  <td className="num">{formatQuantity(totalQuantity)} {unitLabel}</td>
                </tr>
              </tfoot>
            </table>

            {document.notes && (
              <section className="proforma__notes">
                <h3>{t('notes')}</h3>
                <p className="preserve-lines">{document.notes}</p>
              </section>
            )}

            <footer className="proforma__foot">
              <div>{t('notOfficial')} {t('printedAt')}: {formatDateTime(document.printedAt)}</div>
              <div className="proforma__sign">
                <div>{t('deliveredBy')}</div>
                <div>{t('receivedBy')}</div>
              </div>
            </footer>
          </article>
        )
      }}
    </PrintFrame>
  )
}
