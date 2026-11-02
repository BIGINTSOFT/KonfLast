import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDateTime } from '../../utils/format'
import { LookupSelect } from '../masterData/LookupSelect'
import {
  useChangeProductionRouteStatusMutation,
  useCopyProductionRouteMutation,
  useDeleteProductionRouteMutation,
  useGetProductionRouteQuery,
  useSetProductionRouteStepSubcontractMutation,
} from './productionRoutesApi'
import type { ProductionRoute, ProductionRouteOperation } from './productionRoutesApi'
import { formatSam, groupByStage } from './routePresentation'
import { RouteStatusBadge } from './RouteStatusBadge'

/** Akış görünümü: her sütun bir aşama; aynı sütundaki adımlar paralel yürür. */
function RouteFlow({ route }: { route: ProductionRoute }) {
  const stages = groupByStage(route.operations)

  return (
    <ol className="route-flow" aria-label="Operasyon akışı">
      {stages.map((operations, index) => (
        <li key={operations[0].stage} className="route-flow__stage">
          <div className="route-flow__stage-title">
            {index + 1}. aşama{operations.length > 1 && ' · paralel'}
          </div>
          {operations.map((operation) => (
            <div key={operation.id} className="route-flow__step">
              <strong>{operation.sequenceNo}</strong> {operation.operationName}
              <div className="muted">
                {operation.predecessors.length > 0 ? `← ${operation.predecessors.join(', ')}` : 'başlangıç'}
              </div>
            </div>
          ))}
        </li>
      ))}
    </ol>
  )
}

/**
 * Adımın fason ayarı. Akışı ve SAM'i değiştirmediği için rota kilitliyken (etkin / arşiv) de değiştirilebilir;
 * yeni sürüm gerekmez.
 */
function SubcontractStepDialog({
  route, step, onClose,
}: { route: ProductionRoute; step: ProductionRouteOperation; onClose: () => void }) {
  const [save, { isLoading, error }] = useSetProductionRouteStepSubcontractMutation()
  const [isSubcontracted, setIsSubcontracted] = useState(step.isSubcontracted)
  const [subcontractorId, setSubcontractorId] = useState<number | null>(step.subcontractorId)

  const handleSave = async () => {
    const result = await save({
      id: route.id,
      routeOperationId: step.id,
      isSubcontracted,
      subcontractorId: isSubcontracted ? subcontractorId : null,
    })
    if (!('error' in result)) {
      onClose()
    }
  }

  return (
    <Modal title={`Fason ayarı: ${step.sequenceNo} ${step.operationName}`} onClose={onClose}>
      <div className="form">
        <label className="checkbox">
          <input type="checkbox" checked={isSubcontracted} onChange={(event) => setIsSubcontracted(event.target.checked)} />
          Bu adım fasonda (dışarıda) yapılır
        </label>
        {isSubcontracted && (
          <LookupSelect
            resource="suppliers"
            label="Varsayılan fasoncu"
            placeholder="— Yok —"
            value={subcontractorId}
            onChange={setSubcontractorId}
          />
        )}
        <p className="muted">
          Fason adım teklif maliyetinde işçilik yerine fason fiyatıyla sayılır. Demetlerin bu adımı fason gönderim fişiyle
          gönderilir; dönüşte işlenen adet operatör ücretine girmez.
        </p>
        <ErrorAlert error={error} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="button" className="button button--primary" disabled={isLoading} onClick={() => void handleSave()}>
            Kaydet
          </button>
        </footer>
      </div>
    </Modal>
  )
}

type Pending = 'archive' | 'delete'

export function ProductionRouteDetailPage() {
  const id = Number(useParams().id)
  const navigate = useNavigate()
  const canManage = usePermission(Permissions.ProductionRouteManage)
  // Rotayı etkinleştirmek (üretimde geçerli sürüm yapmak) ayrı yetkidir.
  const canActivate = usePermission(Permissions.ProductionRouteActivate)

  const { data: route, isLoading, error } = useGetProductionRouteQuery(id)
  const [changeStatus, statusState] = useChangeProductionRouteStatusMutation()
  const [copyRoute, copyState] = useCopyProductionRouteMutation()
  const [deleteRoute, deleteState] = useDeleteProductionRouteMutation()
  const [pending, setPending] = useState<Pending | null>(null)
  const [subcontractStep, setSubcontractStep] = useState<ProductionRouteOperation | null>(null)

  if (isLoading) {
    return <section className="page"><p role="status">Yükleniyor…</p></section>
  }

  if (!route) {
    return (
      <section className="page">
        <ErrorAlert error={error} />
        <Link to="/production-routes">← Rotalara dön</Link>
      </section>
    )
  }

  const busy = statusState.isLoading || copyState.isLoading || deleteState.isLoading

  const runCopy = async () => {
    const result = await copyRoute(route.id)
    if ('data' in result && result.data) {
      void navigate(`/production-routes/${result.data.id}/edit`)
    }
  }

  const confirmPending = async () => {
    if (pending === 'archive') {
      setPending(null)
      await changeStatus({ id: route.id, action: 'archive', rowVersion: route.rowVersion })
      return
    }

    const result = await deleteRoute(route.id)
    if ('error' in result) {
      setPending(null)
    } else {
      void navigate('/production-routes')
    }
  }

  return (
    <section className="page">
      <p><Link to="/production-routes">← Rotalara dön</Link></p>

      <header className="page__header">
        <div>
          <h1>
            {route.modelCode} · v{route.version} <RouteStatusBadge status={route.status} />
          </h1>
          <p className="muted">
            {route.modelName}
            {route.name && ` · ${route.name}`} · {route.createdBy ?? 'sistem'}, {formatDateTime(route.createdAt)}
          </p>
        </div>

        {(canManage || canActivate) && (
          <div className="page__actions">
            {canManage && route.isEditable && <Link to={`/production-routes/${route.id}/edit`} className="button">Düzenle</Link>}
            {canActivate && route.canActivate && (
              <button
                type="button"
                className="button button--primary"
                disabled={busy}
                onClick={() => void changeStatus({ id: route.id, action: 'activate', rowVersion: route.rowVersion })}
              >
                Etkinleştir
              </button>
            )}
            {canManage && (
              <button type="button" className="button" disabled={busy} onClick={() => void runCopy()}>
                Yeni sürüm olarak kopyala
              </button>
            )}
            {canManage && route.canArchive && (
              <button type="button" className="button" disabled={busy} onClick={() => setPending('archive')}>
                Arşivle
              </button>
            )}
            {canManage && route.isEditable && (
              <button type="button" className="button" disabled={busy} onClick={() => setPending('delete')}>
                Sil
              </button>
            )}
          </div>
        )}
      </header>

      <ErrorAlert error={statusState.error ?? copyState.error ?? deleteState.error} />

      {!route.isEditable && canManage && (
        <p className="alert alert--info">
          Bu rota kilitli. Değişiklik için yeni sürüm olarak kopyalayın; yeni sürüm etkinleştirildiğinde
          bu sürüm arşive alınır.
        </p>
      )}

      {route.description && <p className="preserve-lines">{route.description}</p>}

      {route.operations.length === 0 ? (
        <p className="muted">Bu rotaya henüz operasyon eklenmemiş.</p>
      ) : (
        <>
          <h2 className="section-title">Akış</h2>
          <RouteFlow route={route} />

          <h2 className="section-title">Adımlar</h2>
          <div className="table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <th className="table__number">Sıra</th>
                  <th>Operasyon</th>
                  <th>Makine tipi</th>
                  <th className="table__number">SAM (dk)</th>
                  <th>Önce tamamlanmalı</th>
                  <th>Fason</th>
                  {canManage && <th aria-label="İşlemler" />}
                </tr>
              </thead>
              <tbody>
                {route.operations.map((operation) => (
                  <tr key={operation.id}>
                    <td className="table__number"><strong>{operation.sequenceNo}</strong></td>
                    <td>
                      {operation.operationName} <span className="muted">({operation.operationCode})</span>
                    </td>
                    <td>{operation.machineTypeName ?? '—'}</td>
                    <td className="table__number">{formatSam(operation.sam)}</td>
                    <td>{operation.predecessors.length > 0 ? operation.predecessors.join(', ') : '—'}</td>
                    <td>
                      {operation.isSubcontracted
                        ? <span className="badge badge--info">Fason{operation.subcontractorName ? ` · ${operation.subcontractorName}` : ''}</span>
                        : <span className="muted">—</span>}
                    </td>
                    {canManage && (
                      <td className="table__actions">
                        <button type="button" className="button button--inline" onClick={() => setSubcontractStep(operation)}>
                          Fason ayarı
                        </button>
                      </td>
                    )}
                  </tr>
                ))}
              </tbody>
              <tfoot>
                <tr>
                  <th scope="row" colSpan={3}>Toplam</th>
                  <td className="table__number"><strong>{formatSam(route.totalSam)}</strong></td>
                  <td colSpan={canManage ? 3 : 2} />
                </tr>
              </tfoot>
            </table>
          </div>
        </>
      )}

      {subcontractStep && (
        <SubcontractStepDialog route={route} step={subcontractStep} onClose={() => setSubcontractStep(null)} />
      )}

      {pending && (
        <Modal title={pending === 'delete' ? 'Rotayı sil' : 'Rotayı arşivle'} onClose={() => setPending(null)}>
          <div className="form">
            <p>
              {pending === 'delete'
                ? `${route.modelCode} v${route.version} taslağı silinecek. Sürüm numarası yeniden kullanılmaz.`
                : `${route.modelCode} modelinin etkin rotası kalmayacak; yeni demetler için rota bulunamaz.`}
            </p>
            <footer className="form__actions">
              <button type="button" className="button" onClick={() => setPending(null)}>Vazgeç</button>
              <button type="button" className="button button--danger" disabled={busy} onClick={() => void confirmPending()}>
                {pending === 'delete' ? 'Sil' : 'Arşivle'}
              </button>
            </footer>
          </div>
        </Modal>
      )}
    </section>
  )
}
