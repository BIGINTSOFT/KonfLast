import { Printer } from 'lucide-react'
import { Link, useParams } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { formatDate, formatQuantity } from '../../utils/format'
import { unitLabel } from './dokumaPresentation'
import { sizingModeLabels, useGetDokumaBeamSizingDocumentQuery } from './dokumaWarpApi'

/**
 * Haşıl belgesi. Fasonda leventle birlikte haşılcıya gönderilir: levent, artikel, çözgü iplikleri ve reçete (bilgi amaçlı).
 * İçeride haşılda gerçekleşen alım, viskozite ve kullanılan kimyasallar da basılır. Antet alanı boş bırakılır.
 */
export function DokumaSizingDocumentPage() {
  const id = Number(useParams().id)
  const { data, isLoading, error } = useGetDokumaBeamSizingDocumentQuery(id)

  if (!data) {
    return (
      <section className="page">
        <p><Link to="/dokuma/beams">← Leventlere dön</Link></p>
        {isLoading && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={error} />
      </section>
    )
  }

  const commission = data.sizingMode === 'COMMISSION'
  const recipe = data.recipe

  return (
    <section className="page proforma-page">
      <div className="no-print proforma-page__bar">
        <Link to="/dokuma/beams">← Leventlere dön</Link>
        <span className="muted">Yazdır penceresinde hedef olarak &quot;PDF olarak kaydet&quot; seçilirse PDF dosyası oluşur.</span>
        <button type="button" className="button button--primary" onClick={() => window.print()}>
          <Printer aria-hidden="true" />
          Yazdır / PDF
        </button>
      </div>

      <article className="proforma">
        <div style={{ minHeight: '16mm' }}>
          <span className="muted no-print">Antet alanı: firma bilgisi için antetli kâğıda basın.</span>
        </div>
        <header className="proforma__head">
          <div>
            <h1>{commission ? 'Haşıl Gönderme Formu' : 'Haşıl Kaydı'}</h1>
            <div className="muted">{sizingModeLabels[data.sizingMode]}</div>
          </div>
          <dl className="proforma__meta">
            <div><dt>Levent no</dt><dd>{data.beamNo}</dd></div>
            <div><dt>Çözgü no</dt><dd>{data.warpNo}</dd></div>
            <div><dt>Sipariş no</dt><dd>{data.orderNo}</dd></div>
            {commission && <div><dt>Gönderim tarihi</dt><dd>{data.sentToSizingOn ? formatDate(data.sentToSizingOn) : '—'}</dd></div>}
            <div><dt>{commission ? 'Geri alım tarihi' : 'Haşıl tarihi'}</dt><dd>{data.sizedOn ? formatDate(data.sizedOn) : '—'}</dd></div>
          </dl>
        </header>

        {commission && (
          <div className="proforma__parties" style={{ gridTemplateColumns: '1fr' }}>
            <section className="proforma__party">
              <h3>Haşılcı</h3>
              <strong>{data.supplierName ?? '—'}</strong>
              {data.supplierCode && <div>Kod: {data.supplierCode}</div>}
              {data.supplierTaxNumber && <div>Vergi no: {data.supplierTaxNumber}</div>}
              {data.supplierPhone && <div>Telefon: {data.supplierPhone}</div>}
            </section>
          </div>
        )}

        <table className="proforma__table">
          <thead>
            <tr>
              <th>Artikel</th>
              <th>Müşteri</th>
              <th className="num">Levent metresi</th>
              <th className="num">Tel sayısı</th>
              <th className="num">Tahmini iplik (kg)</th>
            </tr>
          </thead>
          <tbody>
            <tr>
              <td>
                <strong>{data.articleCode}</strong> {data.articleName}
                {data.composition && <div className="muted">{data.composition}</div>}
              </td>
              <td>{data.customerName ?? 'Stoğa üretim'}</td>
              <td className="num">{formatQuantity(data.meters)}</td>
              <td className="num">{formatQuantity(data.ends)}</td>
              <td className="num">{data.estimatedYarnKilograms === null ? '—' : formatQuantity(data.estimatedYarnKilograms)}</td>
            </tr>
          </tbody>
        </table>

        <table className="proforma__table">
          <thead><tr><th>#</th><th>Çözgü ipliği</th><th>Renk</th><th className="num">Tel</th></tr></thead>
          <tbody>
            {data.yarns.map((yarn, index) => (
              <tr key={index}>
                <td>{index + 1}</td>
                <td><strong>{yarn.yarnCode}</strong> {yarn.yarnName}</td>
                <td>{yarn.yarnColor ?? ''}</td>
                <td className="num">{formatQuantity(yarn.ends)}</td>
              </tr>
            ))}
          </tbody>
        </table>

        <section className="proforma__notes">
          <h3>Haşıl reçetesi{commission && ' (bilgi amaçlı)'}</h3>
          {recipe ? (
            <>
              <p><strong>{recipe.code}</strong> {recipe.name}</p>
              <p>
                {[
                  recipe.concentrationPercent !== null && `Konsantrasyon: %${recipe.concentrationPercent}`,
                  recipe.bathTemperatureC !== null && `Banyo sıcaklığı: ${recipe.bathTemperatureC} °C`,
                  recipe.viscositySeconds !== null && `Viskozite: ${recipe.viscositySeconds} sn`,
                  recipe.machineSpeedMetersPerMinute !== null && `Makine hızı: ${recipe.machineSpeedMetersPerMinute} m/dk`,
                  recipe.targetPickUpPercent !== null && `Hedef alım: %${recipe.targetPickUpPercent}`,
                ].filter(Boolean).join(' · ') || 'Banyo değeri girilmemiş.'}
              </p>
              <table className="proforma__table">
                <thead><tr><th>#</th><th>Kimyasal</th><th className="num">Banyo oranı (%)</th>{!commission && <th className="num">Tahmini</th>}</tr></thead>
                <tbody>
                  {recipe.lines.map((line, index) => (
                    <tr key={line.materialId}>
                      <td>{index + 1}</td>
                      <td><strong>{line.materialCode}</strong> {line.materialName}</td>
                      <td className="num">{formatQuantity(line.bathPercent)}</td>
                      {!commission && <td className="num">{line.estimatedQuantity === null ? '—' : `${formatQuantity(line.estimatedQuantity)} ${unitLabel(line.unit)}`}</td>}
                    </tr>
                  ))}
                </tbody>
              </table>
              {recipe.notes && <p className="preserve-lines">{recipe.notes}</p>}
            </>
          ) : (
            <p>Reçete seçilmedi.</p>
          )}
        </section>

        {(data.pickUpPercent !== null || data.viscositySeconds !== null || data.chemicals.length > 0) && (
          <section className="proforma__notes">
            <h3>Gerçekleşen</h3>
            <p>
              {[
                data.pickUpPercent !== null && `Alım: %${data.pickUpPercent}`,
                data.viscositySeconds !== null && `Viskozite: ${data.viscositySeconds} sn`,
              ].filter(Boolean).join(' · ') || '—'}
            </p>
            {data.chemicals.length > 0 && (
              <table className="proforma__table">
                <thead><tr><th>Kimyasal</th><th className="num">Kullanılan</th></tr></thead>
                <tbody>
                  {data.chemicals.map((chemical) => (
                    <tr key={chemical.materialCode}>
                      <td><strong>{chemical.materialCode}</strong> {chemical.materialName}</td>
                      <td className="num">{formatQuantity(chemical.quantity)} {unitLabel(chemical.unit)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </section>
        )}

        {data.notes && (
          <section className="proforma__notes">
            <h3>Not</h3>
            <p className="preserve-lines">{data.notes}</p>
          </section>
        )}

        <footer className="proforma__foot">
          <div className="proforma__sign" style={{ gridTemplateColumns: 'repeat(2, 1fr)' }}>
            <div>Teslim eden</div>
            <div>Teslim alan</div>
          </div>
        </footer>
      </article>
    </section>
  )
}
