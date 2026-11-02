import { Fragment, useState } from 'react'
import { Link } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { filterSetupSteps, groupSetupSteps } from './setupSteps'
import type { SetupStateFilter } from './setupSteps'
import { useGetSetupStatusQuery } from './workspaceApi'
import type { SetupStep } from './workspaceApi'

function StepBadge({ step }: { step: SetupStep }) {
  if (step.done) {
    return <span className="badge badge--ok"><span aria-hidden="true">✓</span> Tamam · {step.count}</span>
  }
  return step.optional
    ? <span className="badge badge--neutral"><span aria-hidden="true">○</span> İsteğe bağlı</span>
    : <span className="badge badge--warning"><span aria-hidden="true">!</span> Eksik</span>
}

/**
 * İlk kurulum rehberi: boş bir sistemde üretime başlamak için neyin, hangi sırayla tanımlanacağını ve hangi
 * adımların tamamlandığını gösterir. Tanım ekranlarında "CSV içe aktar" ile toplu veri alınabilir.
 */
export function SetupGuidePage() {
  const { data, isLoading, error } = useGetSetupStatusQuery()
  const [search, setSearch] = useState('')
  const [state, setState] = useState<SetupStateFilter>('all')
  const steps = data ? filterSetupSteps(data.steps, search, state) : []

  return (
    <section className="page">
      <h1 className="visually-hidden">Kurulum Rehberi</h1>

      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />

      {data && (
        <>
          <p className="alert alert--info">
            Zorunlu adımların <strong>{data.completedRequiredSteps} / {data.requiredSteps}</strong> tanesi tamamlandı.
            {data.completedRequiredSteps === data.requiredSteps && ' Sistem üretime hazır.'}
          </p>
          <div className="progress"><progress max={data.requiredSteps} value={data.completedRequiredSteps} /></div>

          <div className="toolbar" role="search">
            <input
              type="search"
              placeholder="Adım, ekran ya da açıklama ara"
              aria-label="Kurulum adımı ara"
              value={search}
              onChange={(event) => setSearch(event.target.value)}
            />
            <select aria-label="Durum" value={state} onChange={(event) => setState(event.target.value as SetupStateFilter)}>
              <option value="all">Tüm adımlar</option>
              <option value="missing">Tamamlanmamış</option>
              <option value="done">Tamamlanmış</option>
            </select>
          </div>

          {steps.length === 0 && <p className="muted">Aramaya uyan adım yok.</p>}
          {groupSetupSteps(steps).map(({ group, steps: groupSteps }) => (
            <Fragment key={group}>
              <h2 className="section-title">{group}</h2>
              <div className="table-wrap">
                <table className="table table--compact">
                  <tbody>
                    {groupSteps.map((step) => (
                      <tr key={step.key}>
                        <td className="setup__title"><Link to={step.path}>{step.title}</Link></td>
                        <td className="muted">{step.description}</td>
                        <td className="setup__state"><StepBadge step={step} /></td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </Fragment>
          ))}
        </>
      )}
    </section>
  )
}
