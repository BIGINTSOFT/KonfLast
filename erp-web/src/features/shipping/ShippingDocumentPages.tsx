import { Printer } from 'lucide-react'
import { Link, useParams } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { formatDate, formatDateTime, formatQuantity } from '../../utils/format'
import { Barcode, QrCode } from '../labels/LabelPrintPage'
import { stockUnitLabels } from '../stock/stockPresentation'
import { useGetShipmentDocumentQuery, useGetShipmentPickingQuery } from './shippingApi'

/** Belge sayfasının üst çubuğu (yazdırılmaz): geri dönüş ve yazdır düğmesi. */
function PrintBar({ id, title, ready }: { id: number; title: string; ready: boolean }) {
  return (
    <div className="no-print proforma-page__bar">
      <Link to={`/shipments/${id}`}>← Sevkiyata dön</Link>
      <span className="muted">{title} · &quot;PDF olarak kaydet&quot; ile PDF alınabilir.</span>
      <button type="button" className="button button--primary" disabled={!ready} onClick={() => window.print()}>
        <Printer aria-hidden="true" />
        Yazdır / PDF
      </button>
    </div>
  )
}

/**
 * Basılı toplama listesi: satırlar adres sırasıyladır (toplayıcının yürüyeceği sıra); her satırda alınacak adet ve
 * işaretleme kutusu vardır. El terminali kullanılmayan depoda kağıtla toplamak ya da kontrol için.
 */
export function ShipmentPickListPage() {
  const id = Number(useParams().id)
  const { data, error, isLoading } = useGetShipmentPickingQuery(id, { skip: !Number.isInteger(id) })
  return (
    <section className="page proforma-page label-page">
      <style>{'@page { size: A4 portrait; }'}</style>
      <PrintBar id={id} title="Toplama listesi" ready={Boolean(data)} />
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {data && (
        <article className="proforma">
          <header className="proforma__head">
            <div>
              <h1>Toplama Listesi</h1>
              <div className="muted">{data.shipmentNo} · {data.customerName}</div>
            </div>
            <div>
              <Barcode text={data.shipmentNo} />
              <dl className="proforma__meta">
                <div><dt>Sevkiyat</dt><dd>{data.shipmentNo}</dd></div>
                <div><dt>Sipariş</dt><dd>{data.orderNo ?? '—'}{data.customerOrderNo ? ` · ${data.customerOrderNo}` : ''}</dd></div>
                <div><dt>Depo</dt><dd>{data.warehouseCode}</dd></div>
                <div><dt>Sevk günü</dt><dd>{formatDate(data.shipDate)}</dd></div>
                <div><dt>Toplayan</dt><dd>{data.pickingBy ?? '—'}</dd></div>
              </dl>
            </div>
          </header>

          <table className="proforma__table">
            <thead>
              <tr>
                <th>Sıra</th>
                <th>Adres</th>
                <th>Ürün kodu</th>
                <th>Ürün</th>
                <th>Renk</th>
                <th>Beden</th>
                <th className="num">Alınacak</th>
                <th className="num">Alınan</th>
                <th>Onay</th>
              </tr>
            </thead>
            <tbody>
              {data.steps.map((step) => (
                <tr key={`${step.shipmentItemId}-${step.locationId}`}>
                  <td>{step.sequence}</td>
                  <td><strong>{step.locationCode}</strong></td>
                  <td>{step.itemCode}</td>
                  <td>{step.itemName}</td>
                  <td>{step.colorName ?? ''}</td>
                  <td>{step.sizeName ?? ''}</td>
                  <td className="num">{formatQuantity(step.quantity)}</td>
                  <td className="num">{step.pickedQuantity > 0 ? formatQuantity(step.pickedQuantity) : ''}</td>
                  <td className="picklist__check">{step.done ? '✓' : '☐'}</td>
                </tr>
              ))}
            </tbody>
            <tfoot>
              <tr>
                <th colSpan={6}>Toplam</th>
                <td className="num">{formatQuantity(data.steps.reduce((sum, step) => sum + step.quantity, 0))}</td>
                <td className="num">{formatQuantity(data.steps.reduce((sum, step) => sum + step.pickedQuantity, 0))}</td>
                <td />
              </tr>
            </tfoot>
          </table>

          {data.items.some((item) => item.shortage > 0) && (
            <section className="proforma__notes">
              <h3>Depoda bulunmayan</h3>
              <p>
                {data.items.filter((item) => item.shortage > 0)
                  .map((item) => `${item.itemCode}: ${formatQuantity(item.shortage)}`).join(' · ')}
              </p>
            </section>
          )}
          {data.notes && (
            <section className="proforma__notes">
              <h3>Not</h3>
              <p className="preserve-lines">{data.notes}</p>
            </section>
          )}
          <footer className="proforma__foot">
            <div>Stok, irsaliye kesilince toplanan adreslerden düşer.</div>
            <div className="proforma__sign">
              <div>Toplayan</div>
              <div>Kontrol eden</div>
            </div>
          </footer>
        </article>
      )}
    </section>
  )
}

const timeOf = (iso: string | null) =>
  iso ? new Date(iso).toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit', second: '2-digit' }) : '—'

/**
 * e-İrsaliye taslağı: Gelir İdaresi Başkanlığının e-İrsaliye standart görünümündeki yerleşimle (gönderen, SAYIN alıcı,
 * özelleştirme / senaryo / tip / numara / tarih tablosu, ETTN, taşıyıcı ve mal hizmet tablosu). Resmî belge değildir:
 * belge, entegratör üzerinden GİB'e gönderildiğinde resmîleşir.
 */
export function ShipmentEDespatchPage() {
  const id = Number(useParams().id)
  const { data, error, isLoading } = useGetShipmentDocumentQuery(id, { skip: !Number.isInteger(id) })
  const despatch = data?.despatch
  const seller = data?.seller
  const issued = data?.status === 'SHIPPED'
  const lines = (data?.items ?? []).filter((item) => item.quantity > 0)
  const total = lines.reduce((sum, item) => sum + item.quantity, 0)
  const qrText = data
    ? JSON.stringify({
        ettn: despatch?.despatchUuid ?? '',
        no: data.documentNo ?? data.shipmentNo,
        tarih: data.shippedAt ?? data.shipDate,
        vkntckn: seller?.taxNumber ?? '',
        avkntckn: data.customer.taxNumber ?? '',
        senaryo: 'TEMELIRSALIYE',
        tip: 'SEVK',
      })
    : ''

  return (
    <section className="page proforma-page label-page">
      <style>{'@page { size: A4 portrait; margin: 10mm; }'}</style>
      <PrintBar id={id} title="e-İrsaliye (taslak)" ready={Boolean(data)} />
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {data && !issued && (
        <p className="alert alert--info no-print">İrsaliye henüz kesilmedi; numara, ETTN ve tarih irsaliye kesilince oluşur.</p>
      )}
      {data && (
        <article className="edespatch" lang="tr">
          <div className="edespatch__watermark" aria-hidden="true">TASLAK</div>

          <header className="edespatch__top">
            <section className="edespatch__party edespatch__party--sender">
              <strong>{seller?.name ?? 'Gönderen tanımlı değil'}</strong>
              <div>{seller?.address ?? ''}</div>
              <div>Tel: {seller?.phone ?? ''}</div>
              <div>E-Posta: {seller?.email ?? ''}</div>
              <div>Vergi Dairesi: {seller?.taxOffice ?? ''}</div>
              <div>VKN: {seller?.taxNumber ?? ''}</div>
            </section>
            <section className="edespatch__title">
              <div className="edespatch__emblem" aria-hidden="true">e</div>
              <h1>e-İrsaliye</h1>
            </section>
            <section className="edespatch__qr">
              <QrCode text={qrText} />
            </section>
          </header>

          <div className="edespatch__middle">
            <section className="edespatch__party edespatch__party--receiver">
              <div className="edespatch__sayin">SAYIN</div>
              <strong>{data.customer.name}</strong>
              <div>{data.invoiceAddress ?? ''}</div>
              <div>E-Posta: {data.customer.email ?? ''}</div>
              <div>Tel: {data.customer.phone ?? ''}</div>
              <div>Vergi Dairesi: {data.customer.taxOffice ?? ''}</div>
              <div>{(data.customer.taxNumber ?? '').length === 11 ? 'TCKN' : 'VKN'}: {data.customer.taxNumber ?? ''}</div>
              {despatch?.receiverIsEDespatchUser && despatch.receiverAlias && <div>Etiket: {despatch.receiverAlias}</div>}
            </section>
            <table className="edespatch__info">
              <tbody>
                <tr><th>Özelleştirme No:</th><td>TR1.2</td></tr>
                <tr><th>Senaryo:</th><td>TEMELIRSALIYE</td></tr>
                <tr><th>İrsaliye Tipi:</th><td>SEVK</td></tr>
                <tr><th>İrsaliye No:</th><td>{issued ? data.documentNo : '—'}</td></tr>
                <tr><th>İrsaliye Tarihi:</th><td>{issued ? formatDate(data.shippedAt?.slice(0, 10) ?? data.shipDate) : '—'}</td></tr>
                <tr><th>İrsaliye Zamanı:</th><td>{issued ? timeOf(data.shippedAt) : '—'}</td></tr>
                <tr><th>Fiili Sevk Tarihi:</th><td>{formatDate(data.shipDate)}</td></tr>
                <tr><th>Fiili Sevk Zamanı:</th><td>{issued ? timeOf(data.shippedAt) : '—'}</td></tr>
                {data.customerOrderNo && <tr><th>Sipariş No:</th><td>{data.customerOrderNo}</td></tr>}
              </tbody>
            </table>
          </div>

          <div className="edespatch__ettn"><strong>ETTN:</strong> {despatch?.despatchUuid ?? '—'}</div>

          <div className="edespatch__delivery">
            <section>
              <h3>Teslimat Adresi</h3>
              <div>{data.deliveryAddress ?? data.invoiceAddress ?? '—'}</div>
            </section>
            <section>
              <h3>Taşıyıcı / Sürücü Bilgileri</h3>
              <div>Taşıyıcı: {data.carrier ?? '—'}</div>
              <div>Araç Plaka No: {despatch?.vehiclePlate ?? '—'}</div>
              <div>Sürücü Adı Soyadı: {despatch?.driverName ?? '—'}</div>
              <div>Sürücü TCKN: {despatch?.driverIdentityNo ?? '—'}</div>
            </section>
          </div>

          <table className="edespatch__lines">
            <thead>
              <tr>
                <th>Sıra No</th>
                <th>Mal Hizmet Kodu</th>
                <th>Mal Hizmet</th>
                <th className="num">Miktar</th>
                <th>Birim</th>
              </tr>
            </thead>
            <tbody>
              {lines.map((item, index) => (
                <tr key={item.stockItemId}>
                  <td className="num">{index + 1}</td>
                  <td>{item.itemCode}</td>
                  <td>
                    {item.productName ?? item.productCode ?? item.itemCode}
                    {(item.colorName || item.sizeName) && ` · ${[item.colorName, item.sizeName].filter(Boolean).join(' / ')}`}
                    {item.customerArticleCode && ` · Artikel ${item.customerArticleCode}`}
                  </td>
                  <td className="num">{formatQuantity(item.quantity)}</td>
                  <td>{stockUnitLabels[item.unit]}</td>
                </tr>
              ))}
            </tbody>
            <tfoot>
              <tr>
                <th colSpan={3}>Toplam Miktar{data.cartons.length > 0 ? ` · ${data.cartons.length} koli` : ''}</th>
                <td className="num">{formatQuantity(total)}</td>
                <td />
              </tr>
            </tfoot>
          </table>

          <section className="edespatch__notes">
            <strong>Not:</strong>{' '}
            {[
              data.salesOrderNo && `Sipariş ${data.salesOrderNo}`,
              `Sevkiyat ${data.shipmentNo}`,
              data.notes,
            ].filter(Boolean).join(' · ')}
          </section>

          <footer className="edespatch__foot">
            <div className="edespatch__sign"><span>Teslim Eden</span><span>{despatch?.checkedBy ?? ''}</span></div>
            <div className="edespatch__sign"><span>Teslim Alan</span><span /></div>
          </footer>
          <div className="edespatch__disclaimer">
            Bu çıktı taslaktır, resmî e-İrsaliye değildir. Belge entegratör aracılığıyla GİB&apos;e iletildiğinde geçerlilik kazanır.
            Basım: {formatDateTime(data.printedAt)}
          </div>
        </article>
      )}
    </section>
  )
}
