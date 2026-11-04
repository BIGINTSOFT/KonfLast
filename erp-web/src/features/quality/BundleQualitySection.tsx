import { useState } from 'react'
import { ClippedText } from '../../components/ClippedText'
import { ErrorAlert } from '../../components/FormControls'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDateTime, formatQuantity } from '../../utils/format'
import { InspectionDialog } from './InspectionDialog'
import { QualityResultBadge, ReworkStatusBadge } from './QualityBadges'
import { useGetBundleQualityQuery, useReleaseQualityHoldMutation } from './qualityApi'
import { defectsText } from './qualityPresentation'

/** Demet sayfasındaki kalite bölümü: kontrol girişi, bekletmenin kaldırılması, kontrol ve tamir geçmişi. */
export function BundleQualitySection({ bundleId }: { bundleId: number }) {
  const canInspect = usePermission(Permissions.QualityCreate)
  const canReleaseHold = usePermission(Permissions.QualityReleaseHold)
  const { data: quality, error } = useGetBundleQualityQuery(bundleId)
  const [releaseHold, releaseState] = useReleaseQualityHoldMutation()
  const [inspecting, setInspecting] = useState(false)

  if (error) {
    return <ErrorAlert error={error} />
  }

  if (!quality) {
    return null
  }

  const nothingYet = quality.operations.length === 0 && quality.inspections.length === 0

  return (
    <>
      {/* Başlık şeridi: işlem düğmesi sağda; diğer bölümlerle aynı görünüm. */}
      <div className="section-bar">
        <h2 className="section-title">Kalite</h2>
        {!nothingYet && canInspect && quality.operations.length > 0 && (
          <button type="button" className="button button--primary button--inline" onClick={() => setInspecting(true)}>
            Kalite kontrolü gir
          </button>
        )}
      </div>

      {quality.onHold && (
        <div className="alert alert--error alert--action" role="status">
          <span>
            <strong>Demet kalite bekletmesinde.</strong> Serbest bırakılana kadar hiçbir operasyonu başlatılamaz.
          </span>
          {canReleaseHold && (
            <button
              type="button"
              className="button"
              disabled={releaseState.isLoading}
              onClick={() => void releaseHold(bundleId)}
            >
              {releaseState.isLoading ? 'Kaldırılıyor…' : 'Bekletmeyi kaldır'}
            </button>
          )}
        </div>
      )}
      <ErrorAlert error={releaseState.error} />

      {nothingYet && (
        <p className="muted">Henüz üretilmiş parça yok; üretim bildirildikten sonra kalite kontrolü girilebilir.</p>
      )}

      {quality.reworkTasks.length > 0 && (
        <div className="table-wrap">
          <table className="table table--compact">
            <caption className="visually-hidden">Tamir işleri</caption>
            <thead>
              <tr>
                <th>Tamir</th>
                <th>Operasyon</th>
                <th className="table__number">Adet</th>
                <th>Durum</th>
                <th>Açılış</th>
                <th>Yeniden işlendi</th>
                <th>Kapanış</th>
              </tr>
            </thead>
            <tbody>
              {quality.reworkTasks.map((task) => (
                <tr key={task.id}>
                  <td>#{task.id}</td>
                  <td><strong>{task.sequenceNo}</strong> {task.operationName}</td>
                  <td className="table__number">{formatQuantity(task.quantity)}</td>
                  <td><ReworkStatusBadge status={task.status} /></td>
                  <td>{formatDateTime(task.createdAt)} · {task.createdBy}</td>
                  <td>{formatDateTime(task.doneAt)}</td>
                  <td>{task.closedAt ? `${formatDateTime(task.closedAt)} · ${task.closedBy}` : '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {quality.inspections.length > 0 && (
        <div className="table-wrap">
          <table className="table table--compact">
            <caption className="visually-hidden">Kalite kontrolleri</caption>
            <thead>
              <tr>
                <th>Zaman</th>
                <th>Operasyon</th>
                <th>Sonuç</th>
                <th className="table__number">Kontrol</th>
                <th className="table__number">Geçen</th>
                <th className="table__number">Ret</th>
                <th className="table__number">Tamir</th>
                <th>Hatalar</th>
                <th>Kontrol eden</th>
                <th>Not</th>
              </tr>
            </thead>
            <tbody>
              {quality.inspections.map((inspection) => (
                <tr key={inspection.id}>
                  <td className="table__nowrap">{formatDateTime(inspection.inspectedAt)}</td>
                  <td>
                    <strong>{inspection.sequenceNo}</strong> {inspection.operationName}
                    {inspection.reworkTaskId && <div className="muted">Tamir #{inspection.reworkTaskId} tekrar kontrolü</div>}
                  </td>
                  <td><QualityResultBadge result={inspection.result} /></td>
                  <td className="table__number">{formatQuantity(inspection.inspectedQuantity)}</td>
                  <td className="table__number">{formatQuantity(inspection.passedQuantity)}</td>
                  <td className="table__number">{formatQuantity(inspection.rejectedQuantity)}</td>
                  <td className="table__number">{formatQuantity(inspection.reworkQuantity)}</td>
                  <td>{defectsText(inspection)}</td>
                  <td className="table__nowrap">{inspection.inspectedBy}</td>
                  {/* Uzun not tabloyu genişletmez: kısaltılır, tıklanınca tamamı okunur. */}
                  <td><ClippedText text={inspection.note} title="Kontrol notu" /></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {inspecting && <InspectionDialog quality={quality} onClose={() => setInspecting(false)} />}
    </>
  )
}
