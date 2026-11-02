import { Printer } from 'lucide-react'
import { Link, useParams } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { formatDate, formatDateTime, formatQuantity, todayIsoDate } from '../../utils/format'
import { formatStockQuantity } from '../stock/stockPresentation'
import { useGetCuttingPlanQuery } from './cuttingApi'
import { cuttingPlanStatusPresentation, formatMeters, shadeText } from './cuttingPresentation'

/** Föyün baskı biçimi: siyah-beyaz, çerçeveli tablolar, A4 dikey. */
const sheetStyle = `
@page { size: A4 portrait; margin: 10mm; }
.cutting-sheet { max-width: 190mm; background: #fff; color: #000; font-size: 10pt; }
.cutting-sheet h2 { font-size: 14pt; margin: 0 0 2mm; }
.cutting-sheet h3 { font-size: 10.5pt; margin: 4mm 0 1.5mm; }
.cutting-sheet__head { display: grid; grid-template-columns: 1fr 1fr; gap: 1mm 6mm; margin-bottom: 3mm; }
.cutting-sheet table { width: 100%; border-collapse: collapse; }
.cutting-sheet th, .cutting-sheet td { border: 1px solid #000; padding: 1.2mm 1.6mm; text-align: left; }
.cutting-sheet td.num, .cutting-sheet th.num { text-align: right; }
.cutting-sheet td.blank { min-width: 18mm; }
.cutting-sheet__signatures { display: grid; grid-template-columns: repeat(4, 1fr); gap: 4mm; margin-top: 8mm; }
.cutting-sheet__signatures div { border-top: 1px solid #000; padding-top: 1mm; min-height: 16mm; }
`

/** Kesim föyü: kesimhanede pastal serimi ve kesim için yazdırılır; gerçek değerler elle doldurulup imzalanır. */
export function CuttingSheetPage() {
  const id = Number(useParams().id)
  const { data: plan, error, isLoading } = useGetCuttingPlanQuery(id)
  const isCut = plan?.status === 'CUT'

  return (
    <section className="page label-page">
      <style>{sheetStyle}</style>
      <div className="no-print">
        <p><Link to={`/cutting-plans/${id}`}>← Kesim planına dön</Link></p>
        <header className="page__header">
          <div>
            <h1>Kesim föyü</h1>
            <p className="muted">A4 dikey yazdırın. Boş alanlar kesimde elle doldurulur.</p>
          </div>
          <button type="button" className="button button--primary" disabled={!plan} onClick={() => window.print()}>
            <Printer aria-hidden="true" />
            Yazdır
          </button>
        </header>
        {isLoading && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={error} />
      </div>

      {plan && (
        <article className="cutting-sheet">
          <h2>Kesim föyü {plan.planNo}</h2>
          <div className="cutting-sheet__head">
            <div><strong>Üretim siparişi:</strong> {plan.orderNo}</div>
            <div><strong>Durum:</strong> {cuttingPlanStatusPresentation[plan.status].label}</div>
            <div><strong>Müşteri:</strong> {plan.customerName}</div>
            <div><strong>Tarih:</strong> {plan.cutAt ? formatDateTime(plan.cutAt) : formatDate(todayIsoDate())}</div>
            <div><strong>Ürün:</strong> {plan.productCode} {plan.productName}</div>
            <div><strong>Model:</strong> {plan.modelCode}</div>
            <div><strong>Renk:</strong> {plan.colorName}</div>
            <div><strong>Ton:</strong> {plan.shadeKey ?? '—'}</div>
          </div>

          <h3>Pastal</h3>
          <table>
            <thead>
              <tr>
                <th className="num">Pastal boyu</th>
                <th className="num">Kat başı pay</th>
                <th className="num">Planlanan kat</th>
                <th className="num">Katta parça</th>
                <th className="num">Planlanan adet</th>
                <th className="num">Kumaş ihtiyacı</th>
                <th className="num">Gerçek kat</th>
              </tr>
            </thead>
            <tbody>
              <tr>
                <td className="num">{formatMeters(plan.markerLength)}</td>
                <td className="num">{formatMeters(plan.endAllowance)}</td>
                <td className="num">{plan.plannedPlies}</td>
                <td className="num">{plan.piecesPerPly}</td>
                <td className="num">{formatQuantity(plan.plannedQuantity)}</td>
                <td className="num">{formatMeters(plan.plannedFabric)}</td>
                <td className="num blank">{isCut ? plan.actualPlies : ''}</td>
              </tr>
            </tbody>
          </table>

          <h3>Beden oranı</h3>
          <table>
            <thead>
              <tr>
                <th>Beden</th>
                <th className="num">Oran</th>
                <th className="num">Planlanan adet</th>
                <th className="num">Kesilen adet</th>
              </tr>
            </thead>
            <tbody>
              {plan.sizes.map((size) => (
                <tr key={size.sizeId}>
                  <td>{size.sizeName}</td>
                  <td className="num">{size.ratio}</td>
                  <td className="num">{formatQuantity(size.plannedQuantity)}</td>
                  <td className="num blank">{size.cutQuantity !== null ? formatQuantity(size.cutQuantity) : ''}</td>
                </tr>
              ))}
              <tr>
                <th scope="row">Toplam</th>
                <td className="num">{plan.piecesPerPly}</td>
                <td className="num">{formatQuantity(plan.plannedQuantity)}</td>
                <td className="num blank">{plan.cutQuantity !== null ? formatQuantity(plan.cutQuantity) : ''}</td>
              </tr>
            </tbody>
          </table>

          <h3>Kumaş topları (serim sırası)</h3>
          {plan.lots.length === 0 ? (
            <p>Top atanmamış.</p>
          ) : (
            <table>
              <thead>
                <tr>
                  <th className="num">Sıra</th>
                  <th>Top</th>
                  <th>Kumaş</th>
                  <th>Ton / parti</th>
                  <th className="num">En</th>
                  <th>Yer</th>
                  <th className="num">Bakiye</th>
                  <th className="num">Serilen kat</th>
                  <th className="num">Kullanılan</th>
                  <th className="num">Artan</th>
                </tr>
              </thead>
              <tbody>
                {plan.lots.map((lot) => (
                  <tr key={lot.id}>
                    <td className="num">{lot.sortOrder}</td>
                    <td>{lot.lotNo}</td>
                    <td>{lot.stockItemCode}</td>
                    <td>{shadeText(lot)}</td>
                    <td className="num">{lot.width ?? ''}</td>
                    <td>{lot.locations}</td>
                    <td className="num">{formatStockQuantity(lot.balance, lot.unit)}</td>
                    <td className="num blank" />
                    <td className="num blank">{lot.usedQuantity !== null ? formatStockQuantity(lot.usedQuantity, lot.unit) : ''}</td>
                    <td className="num blank" />
                  </tr>
                ))}
              </tbody>
            </table>
          )}

          {plan.notes && (
            <>
              <h3>Not</h3>
              <p className="preserve-lines">{plan.notes}</p>
            </>
          )}

          <div className="cutting-sheet__signatures">
            <div>Hazırlayan (planlama)</div>
            <div>Serimi yapan</div>
            <div>Kesen</div>
            <div>Kontrol</div>
          </div>
        </article>
      )}
    </section>
  )
}
