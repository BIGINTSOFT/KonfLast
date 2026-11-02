import { ErrorAlert } from '../../components/FormControls'
import { formatQuantity } from '../../utils/format'
import { BundleStatusBadge } from '../bundles/BundleBadges'
import { useGetOrderProgressQuery } from './trackingApi'
import { percentOf } from './trackingPresentation'

/** Sipariş sayfasındaki üretim ilerlemesi: toplamlar, operasyon ve renk-beden bazında döküm. */
export function OrderProgressSection({ orderId }: { orderId: number }) {
  const { data: progress, error } = useGetOrderProgressQuery(orderId, { pollingInterval: 30_000 })

  if (error) {
    return <ErrorAlert error={error} />
  }

  // Demetlenmemiş siparişte gösterilecek ilerleme yoktur.
  if (!progress || progress.bundled === 0) {
    return null
  }

  return (
    <>
      <h2 className="section-title">Üretim ilerlemesi</h2>

      <div className="cards">
        <article className="card">
          <h2>Özet</h2>
          <label className="progress">
            <span>
              Biten: <strong>{formatQuantity(progress.finished)}</strong> / {formatQuantity(progress.ordered)} adet (%
              {progress.completionPercent})
            </span>
            <progress max={100} value={progress.completionPercent} />
          </label>
          <dl className="status-list">
            <div><dt>Demetlenen</dt><dd>{formatQuantity(progress.bundled)}</dd></div>
            <div><dt>Üretimde (açık demetlerde)</dt><dd>{formatQuantity(progress.inProcess)}</dd></div>
            <div><dt>Hatalı</dt><dd>{formatQuantity(progress.rejected)}</dd></div>
            <div><dt>Siparişe göre eksik</dt><dd>{formatQuantity(progress.shortage)}</dd></div>
            <div><dt>Sevk edilen</dt><dd>{formatQuantity(progress.shipped)}</dd></div>
          </dl>
          {progress.canComplete && (
            <p className="alert alert--info">
              Tüm demetler tamamlandı.{' '}
              {progress.shortage > 0
                ? `Sipariş adedine göre ${formatQuantity(progress.shortage)} adet eksik; eksik için yeni demet oluşturabilir ya da siparişi bu haliyle tamamlayabilirsiniz.`
                : 'Sipariş tamamlanabilir.'}
            </p>
          )}
        </article>
        <article className="card">
          <h2>Demetler</h2>
          <ul className="plain-list">
            {progress.bundleStatuses.map((row) => (
              <li key={row.status}>
                <BundleStatusBadge status={row.status} /> {row.bundleCount} demet · {formatQuantity(row.quantity)} adet
              </li>
            ))}
          </ul>
        </article>
      </div>

      <div className="table-wrap">
        <table className="table">
          <caption className="visually-hidden">Operasyon bazında ilerleme</caption>
          <thead>
            <tr>
              <th className="table__number">Sıra</th>
              <th>Operasyon</th>
              <th>İlerleme</th>
              <th className="table__number">Üretilen</th>
              <th className="table__number">Hatalı</th>
              <th className="table__number">Kalan</th>
              <th className="table__number">Bekleyen demet</th>
              <th className="table__number">İşlemde</th>
              <th className="table__number">Biten demet</th>
            </tr>
          </thead>
          <tbody>
            {progress.operations.map((operation) => {
              const done = percentOf(operation.produced + operation.rejected, operation.planned)
              return (
                <tr key={operation.sequenceNo}>
                  <td className="table__number"><strong>{operation.sequenceNo}</strong></td>
                  <td>{operation.operationName}</td>
                  <td>
                    <label className="progress progress--inline">
                      <progress max={100} value={done} aria-label={`${operation.operationName} ilerlemesi`} />
                      <span>%{done}</span>
                    </label>
                  </td>
                  <td className="table__number">{formatQuantity(operation.produced)}</td>
                  <td className="table__number">{formatQuantity(operation.rejected)}</td>
                  <td className="table__number">{formatQuantity(operation.remaining)}</td>
                  <td className="table__number">{operation.waitingBundles}</td>
                  <td className="table__number">{operation.inProgressBundles}</td>
                  <td className="table__number">
                    {operation.completedBundles} / {operation.totalBundles}
                  </td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>

      <div className="table-wrap">
        <table className="table table--compact">
          <caption className="visually-hidden">Renk ve beden bazında ilerleme</caption>
          <thead>
            <tr>
              <th>Renk</th>
              <th>Beden</th>
              <th className="table__number">Sipariş</th>
              <th className="table__number">Demetlenen</th>
              <th className="table__number">Biten</th>
              <th className="table__number">Hatalı</th>
            </tr>
          </thead>
          <tbody>
            {progress.items.map((item) => (
              <tr key={`${item.colorId}-${item.sizeId}`}>
                <td>{item.colorName}</td>
                <td>{item.sizeName}</td>
                <td className="table__number">{formatQuantity(item.ordered)}</td>
                <td className="table__number">{formatQuantity(item.bundled)}</td>
                <td className="table__number">{formatQuantity(item.finished)}</td>
                <td className="table__number">{formatQuantity(item.rejected)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </>
  )
}
