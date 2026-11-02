import { formatQuantity, formatRecipeAmount } from '../../utils/format'
import { formatRate } from '../../utils/money'
import type { DokumaOrderOwnership } from './dokumaOrdersApi'
import type { DokumaQuotationLine } from './dokumaQuotationsApi'

const percent = (value: number) => `%${formatQuantity(value)}`

/**
 * Teklif satırının metre başına ön maliyet dökümü: hesap girdileri, iplikler ve maliyet kalemleri. Hem kayıtlı teklifte hem formdaki
 * hesaplama önizlemesinde kullanılır. Tutarlar teklifin para birimindedir.
 */
export function QuotationLineBreakdown({ line, currency, ownership }: { line: DokumaQuotationLine; currency: string; ownership: DokumaOrderOwnership }) {
  const money = (value: number) => formatRate(value, currency)
  const crimp = line.warpCrimpPercent ?? 0

  return (
    <div className="form__pair" style={{ alignItems: 'start' }}>
      <div>
        <table className="table table--compact">
          <caption className="muted" style={{ textAlign: 'left' }}>Dokuma hızı</caption>
          <tbody>
            <tr><th scope="row">Tezgâh</th><td>{line.loomCode ?? <span className="muted">Seçilmedi</span>}</td></tr>
            <tr><th scope="row">Hız</th><td className="table__number">{formatQuantity(line.speedRpm)} devir/dk</td></tr>
            <tr><th scope="row">Verim</th><td className="table__number">{percent(line.efficiencyPercent)}</td></tr>
            <tr><th scope="row">Atkı sıklığı</th><td className="table__number">{formatQuantity(line.picksPerCm)} atkı/cm</td></tr>
            <tr><th scope="row">Saatte dokunan</th><td className="table__number">{formatQuantity(line.metersPerHour)} m</td></tr>
            <tr><th scope="row">Metre başına süre</th><td className="table__number">{formatRecipeAmount(line.weavingHoursPerMeter * 60)} dk</td></tr>
          </tbody>
        </table>
        <table className="table table--compact">
          <caption className="muted" style={{ textAlign: 'left' }}>
            İplik (metre başına, artikel firesi dahil){ownership === 'COMMISSION' && ' — fason: iplik müşterinin, fiyata girmez'}
          </caption>
          <thead>
            <tr>
              <th>İplik</th>
              <th>Yer</th>
              <th className="table__number">kg/m</th>
              <th className="table__number">Fiyat (kg)</th>
              <th className="table__number">Tutar</th>
            </tr>
          </thead>
          <tbody>
            {line.yarns.map((yarn) => (
              <tr key={yarn.yarnId}>
                <td>
                  {yarn.yarnCode} <span className="muted">{yarn.yarnName}{yarn.yarnColor && ` · ${yarn.yarnColor}`}</span>
                  <div className="muted">{yarn.priceSource ?? 'Fiyat yok'}</div>
                </td>
                <td>{yarn.usedInWarp && yarn.usedInWeft ? 'Çözgü, atkı' : yarn.usedInWarp ? 'Çözgü' : 'Atkı'}</td>
                <td className="table__number">{formatRecipeAmount(yarn.kilogramsPerMeter)}</td>
                <td className="table__number">{money(yarn.unitPrice)}</td>
                <td className="table__number">{money(yarn.costPerMeter)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <table className="table table--compact">
        <caption className="muted" style={{ textAlign: 'left' }}>Metre kumaş başına maliyet</caption>
        <tbody>
          <tr><th scope="row">İplik</th><td className="table__number">{money(line.yarnCost)}</td></tr>
          <tr>
            <th scope="row">Haşıl <div className="muted">{money(line.sizingCostPerWarpMeter)} / m çözgü × {formatQuantity(1 + crimp / 100)}</div></th>
            <td className="table__number">{money(line.sizingCost)}</td>
          </tr>
          <tr>
            <th scope="row">Çözgü <div className="muted">{money(line.warpingCostPerWarpMeter)} / m çözgü × {formatQuantity(1 + crimp / 100)}</div></th>
            <td className="table__number">{money(line.warpingCost)}</td>
          </tr>
          <tr>
            <th scope="row">Dokuma <div className="muted">{formatRecipeAmount(line.weavingHoursPerMeter)} sa × ({money(line.loomHourlyCost)} tezgâh + {money(line.laborHourlyCost)} işçilik)</div></th>
            <td className="table__number">{money(line.weavingCost)}</td>
          </tr>
          <tr><th scope="row">Doğrudan maliyet</th><td className="table__number"><strong>{money(line.directCost)}</strong></td></tr>
          <tr><th scope="row">Fire {percent(line.wastePercent)}</th><td className="table__number">{money(line.wasteCost)}</td></tr>
          <tr><th scope="row">Genel gider {percent(line.overheadPercent)}</th><td className="table__number">{money(line.overheadCost)}</td></tr>
          <tr><th scope="row">Maliyet</th><td className="table__number"><strong>{money(line.costPerMeter)}</strong></td></tr>
          <tr><th scope="row">Kâr marjı {percent(line.marginPercent)}</th><td className="table__number">{money(line.calculatedPrice - line.costPerMeter)}</td></tr>
          <tr><th scope="row">Hesaplanan fiyat</th><td className="table__number">{money(line.calculatedPrice)}</td></tr>
          <tr>
            <th scope="row">Teklif fiyatı {line.actualMarginPercent !== null && <span className="muted">(kâr {percent(line.actualMarginPercent)})</span>}</th>
            <td className="table__number"><strong>{money(line.unitPrice)}</strong></td>
          </tr>
        </tbody>
      </table>
    </div>
  )
}
