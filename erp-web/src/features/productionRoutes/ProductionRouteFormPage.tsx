import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { fieldErrors, isRequestError } from '../../api/envelope'
import { ErrorAlert, TextField } from '../../components/FormControls'
import { LookupSelect } from '../masterData/LookupSelect'
import type { LookupItem } from '../masterData/masterDataApi'
import {
  useCreateProductionRouteMutation,
  useGetProductionRouteQuery,
  useUpdateProductionRouteMutation,
} from './productionRoutesApi'
import type { ProductionRoute } from './productionRoutesApi'
import { parseSequenceList } from './routePresentation'

interface StepRow {
  /** Satırın arayüzdeki kimliği (sıra numarası düzenlenebildiği için ayrı tutulur). */
  key: string
  sequenceNo: string
  operationId: number
  operationLabel: string
  sam: string
  predecessors: string
  /** Adım fasonda yapılır. */
  isSubcontracted: boolean
  subcontractorId: number | null
}

const SEQUENCE_STEP = 10

function rowsOf(route?: ProductionRoute): StepRow[] {
  return (route?.operations ?? []).map((operation) => ({
    key: `existing-${operation.id}`,
    sequenceNo: String(operation.sequenceNo),
    operationId: operation.operationId,
    operationLabel: `${operation.operationCode} — ${operation.operationName}`,
    sam: String(operation.sam),
    predecessors: operation.predecessors.join(', '),
    isSubcontracted: operation.isSubcontracted,
    subcontractorId: operation.subcontractorId,
  }))
}

function StepsEditor({
  rows, onChange, errors,
}: {
  rows: StepRow[]
  onChange: (rows: StepRow[]) => void
  errors?: string[]
}) {
  const [operationToAdd, setOperationToAdd] = useState<LookupItem | null>(null)

  const update = (key: string, patch: Partial<StepRow>) =>
    onChange(rows.map((row) => (row.key === key ? { ...row, ...patch } : row)))

  const addStep = () => {
    if (!operationToAdd) {
      return
    }

    // Yeni adım varsayılan olarak son adımdan sonra gelir (sıralı akış); istenirse değiştirilir.
    const lastSequence = Math.max(0, ...rows.map((row) => Number(row.sequenceNo) || 0))
    onChange([
      ...rows,
      {
        key: `new-${Date.now()}`,
        sequenceNo: String(lastSequence + SEQUENCE_STEP),
        operationId: operationToAdd.id,
        operationLabel: `${operationToAdd.code} — ${operationToAdd.name}`,
        sam: '',
        predecessors: lastSequence > 0 ? String(lastSequence) : '',
        isSubcontracted: false,
        subcontractorId: null,
      },
    ])
    setOperationToAdd(null)
  }

  return (
    <fieldset className="checkbox-list">
      <legend>Adımlar</legend>

      <div className="matrix-adder">
        <LookupSelect
          resource="operations"
          label="Operasyon ekle"
          value={operationToAdd?.id ?? null}
          onChange={() => undefined}
          onSelectItem={setOperationToAdd}
        />
        <button type="button" className="button" onClick={addStep} disabled={!operationToAdd}>Adımı ekle</button>
      </div>

      {rows.length === 0 ? (
        <p className="muted">Henüz adım yok. Yukarıdan operasyon seçip ekleyin.</p>
      ) : (
        <div className="table-wrap">
          <table className="table">
            <thead>
              <tr>
                <th>Sıra no</th>
                <th>Operasyon</th>
                <th>SAM (dk)</th>
                <th>Önce tamamlanmalı (sıra no)</th>
                <th>Fason</th>
                <th aria-label="İşlemler" />
              </tr>
            </thead>
            <tbody>
              {rows.map((row) => {
                const invalidPredecessors = parseSequenceList(row.predecessors) === null
                return (
                  <tr key={row.key}>
                    <td>
                      <input
                        type="number"
                        min={1}
                        step={1}
                        className="matrix-input"
                        aria-label="Sıra no"
                        value={row.sequenceNo}
                        onChange={(event) => update(row.key, { sequenceNo: event.target.value })}
                      />
                    </td>
                    <td>{row.operationLabel}</td>
                    <td>
                      <input
                        type="number"
                        min={0}
                        step="0.0001"
                        className="matrix-input"
                        aria-label={`${row.operationLabel} SAM`}
                        placeholder="standart"
                        value={row.sam}
                        onChange={(event) => update(row.key, { sam: event.target.value })}
                      />
                    </td>
                    <td>
                      <input
                        className="matrix-input matrix-input--wide"
                        aria-label={`${row.operationLabel} öncülleri`}
                        aria-invalid={invalidPredecessors}
                        placeholder="örn. 20, 30 — boşsa hemen başlar"
                        value={row.predecessors}
                        onChange={(event) => update(row.key, { predecessors: event.target.value })}
                      />
                      {invalidPredecessors && (
                        <small className="field__error">Sıra numaralarını virgülle ayırarak yazın.</small>
                      )}
                    </td>
                    <td>
                      <label className="checkbox">
                        <input
                          type="checkbox"
                          checked={row.isSubcontracted}
                          onChange={(event) => update(row.key, {
                            isSubcontracted: event.target.checked,
                            subcontractorId: event.target.checked ? row.subcontractorId : null,
                          })}
                        />
                        Fasonda
                      </label>
                      {row.isSubcontracted && (
                        <LookupSelect
                          resource="suppliers"
                          label={`${row.operationLabel} fasoncusu`}
                          hideLabel
                          idSuffix={row.key}
                          placeholder="— Varsayılan fasoncu yok —"
                          value={row.subcontractorId}
                          onChange={(value) => update(row.key, { subcontractorId: value })}
                        />
                      )}
                    </td>
                    <td className="table__actions">
                      <button
                        type="button"
                        className="button"
                        onClick={() => onChange(rows.filter((other) => other.key !== row.key))}
                      >
                        Kaldır
                      </button>
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      )}

      <small className="field__hint">
        Aynı adıma bağlı birden fazla adım paralel yürür. SAM boş bırakılırsa operasyonun standart süresi alınır.
        Fason adımlar maliyet ve teklifte işçilik yerine fason fiyatıyla sayılır; demetleri fason gönderim fişiyle gönderilir.
      </small>
      {errors?.map((message) => (
        <small key={message} className="field__error">{message}</small>
      ))}
    </fieldset>
  )
}

function RouteForm({ route, onConflict }: { route?: ProductionRoute; onConflict?: () => void }) {
  const navigate = useNavigate()
  const [createRoute, createState] = useCreateProductionRouteMutation()
  const [updateRoute, updateState] = useUpdateProductionRouteMutation()
  const { isLoading, error } = route ? updateState : createState

  const [modelId, setModelId] = useState<number | null>(route?.modelId ?? null)
  const [name, setName] = useState(route?.name ?? '')
  const [description, setDescription] = useState(route?.description ?? '')
  const [rows, setRows] = useState<StepRow[]>(() => rowsOf(route))

  const hasTypingError = rows.some((row) => parseSequenceList(row.predecessors) === null)

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    if (hasTypingError) {
      return
    }

    const body = {
      modelId: modelId ?? 0,
      name: name.trim() || null,
      description: description.trim() || null,
      operations: rows.map((row) => ({
        sequenceNo: Number(row.sequenceNo || 0),
        operationId: row.operationId,
        sam: row.sam === '' ? null : Number(row.sam),
        predecessors: parseSequenceList(row.predecessors) ?? [],
        isSubcontracted: row.isSubcontracted,
        subcontractorId: row.isSubcontracted ? row.subcontractorId : null,
      })),
    }

    const result = route
      ? await updateRoute({ id: route.id, body: { ...body, rowVersion: route.rowVersion } })
      : await createRoute(body)

    if ('data' in result && result.data) {
      void navigate(`/production-routes/${result.data.id}`)
    } else if (isRequestError(result.error) && result.error.code === 'CONCURRENCY_CONFLICT') {
      onConflict?.()
    }
  }

  return (
    <form className="form" onSubmit={(event) => void handleSubmit(event)} noValidate>
      <div className="form__grid">
        {route ? (
          <div className="field">
            <span>Model</span>
            <div>
              <code>{route.modelCode}</code> — {route.modelName} · v{route.version}
            </div>
          </div>
        ) : (
          <LookupSelect
            resource="models"
            label="Model"
            value={modelId}
            onChange={setModelId}
            required
            errors={fieldErrors(error, 'modelId')}
          />
        )}
        <TextField
          label="Rota adı (isteğe bağlı)"
          value={name}
          maxLength={100}
          onChange={(event) => setName(event.target.value)}
          errors={fieldErrors(error, 'name')}
          autoComplete="off"
        />
      </div>

      <StepsEditor rows={rows} onChange={setRows} errors={fieldErrors(error, 'operations')} />

      <label className="field">
        <span>Açıklama (isteğe bağlı)</span>
        <textarea rows={2} maxLength={500} value={description} onChange={(event) => setDescription(event.target.value)} />
      </label>

      <ErrorAlert error={error} fields={['modelId', 'name', 'operations']} />

      <footer className="form__actions">
        <Link to={route ? `/production-routes/${route.id}` : '/production-routes'} className="button">Vazgeç</Link>
        <button type="submit" className="button button--primary" disabled={isLoading || hasTypingError}>
          {isLoading ? 'Kaydediliyor…' : 'Kaydet'}
        </button>
      </footer>
    </form>
  )
}

export function ProductionRouteCreatePage() {
  return (
    <section className="page">
      <p><Link to="/production-routes">← Rotalara dön</Link></p>
      <h1>Yeni operasyon rotası</h1>
      <p className="muted">
        Rota taslak olarak kaydedilir ve modelin sıradaki sürümü olur. Üretimde kullanılması için etkinleştirilmelidir.
      </p>
      <div className="card card--spaced"><RouteForm /></div>
    </section>
  )
}

export function ProductionRouteEditPage() {
  const id = Number(useParams().id)
  const { data: route, isLoading, error } = useGetProductionRouteQuery(id)
  const [conflicted, setConflicted] = useState(false)

  return (
    <section className="page">
      <p><Link to={`/production-routes/${id}`}>← Rotaya dön</Link></p>
      <h1>Rotayı düzenle{route ? `: ${route.modelCode} v${route.version}` : ''}</h1>

      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {conflicted && (
        <p className="alert alert--error" role="alert">
          Rota siz düzenlerken başka bir kullanıcı tarafından değiştirildi. Form güncel verilerle yenilendi;
          değişikliklerinizi kontrol ederek tekrar girin.
        </p>
      )}

      {route && !route.isEditable && (
        <p className="alert alert--info">
          Bu rota kilitli. Değişiklik için rota sayfasından yeni sürüm olarak kopyalayın.
        </p>
      )}
      {/* key: başka bir kullanıcı kaydedip sürüm değişirse form güncel veriyle yeniden kurulur. */}
      {route?.isEditable && (
        <div className="card card--spaced">
          <RouteForm key={route.rowVersion} route={route} onConflict={() => setConflicted(true)} />
        </div>
      )}
    </section>
  )
}
