import { ArrowDown, ArrowUp, Plus, X } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { ErrorAlert } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { LookupSelect } from '../masterData/LookupSelect'
import { useDeleteBoyahaneRouteMutation, useGetBoyahaneRoutesQuery, useSaveBoyahaneRouteMutation } from './boyahaneInsightsApi'
import type { BoyahaneRoute } from './boyahaneInsightsApi'
import { orderProcessTypeLabels } from './boyahaneOrdersApi'
import type { OrderProcessType } from './boyahaneOrdersApi'

interface StepDraft {
  key: number
  operationId: number | null
}

function RouteDialog({ route, onClose }: { route: BoyahaneRoute | null; onClose: () => void }) {
  const [save, { isLoading, error }] = useSaveBoyahaneRouteMutation()
  const [remove, removeState] = useDeleteBoyahaneRouteMutation()
  const [code, setCode] = useState(route?.code ?? '')
  const [name, setName] = useState(route?.name ?? '')
  const [processType, setProcessType] = useState<OrderProcessType>(route?.processType ?? 'FABRIC_DYEING')
  const [isDefault, setIsDefault] = useState(route?.isDefault ?? false)
  const [isActive, setIsActive] = useState(route?.isActive ?? true)
  const [steps, setSteps] = useState<StepDraft[]>(route?.steps.map((step) => ({ key: step.sequence, operationId: step.operationId })) ?? [{ key: 1, operationId: null }])

  const move = (index: number, delta: number) => {
    const next = [...steps]
    const [item] = next.splice(index, 1)
    next.splice(index + delta, 0, item)
    setSteps(next)
  }

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await save({
      id: route?.id,
      body: { code, name, processType, isDefault, isActive, operationIds: steps.flatMap((step) => (step.operationId === null ? [] : [step.operationId])) },
    })
    if (!('error' in result)) {
      onClose()
    }
  }

  const handleDelete = async () => {
    if (route && !('error' in await remove(route.id))) {
      onClose()
    }
  }

  return (
    <Modal title={route ? `Rota: ${route.name}` : 'Yeni rota'} onClose={onClose} variant="drawer">
      <form className="form" onSubmit={handleSubmit}>
        <div className="form__pair">
          <label className="field"><span>Kod</span><input required maxLength={30} value={code} onChange={(event) => setCode(event.target.value)} /></label>
          <label className="field"><span>Ad</span><input required maxLength={100} value={name} onChange={(event) => setName(event.target.value)} /></label>
        </div>
        <label className="field">
          <span>İşlem türü</span>
          <select value={processType} onChange={(event) => setProcessType(event.target.value as OrderProcessType)}>
            {Object.entries(orderProcessTypeLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
          </select>
        </label>
        <label className="checkbox"><input type="checkbox" checked={isDefault} onChange={(event) => setIsDefault(event.target.checked)} />Bu işlem türünün varsayılan rotası</label>
        <label className="checkbox"><input type="checkbox" checked={isActive} onChange={(event) => setIsActive(event.target.checked)} />Aktif</label>

        <h3 className="section-title">Adımlar</h3>
        <div className="table-wrap">
          <table className="table">
            <thead><tr><th>Sıra</th><th>Operasyon</th><th aria-label="İşlemler" /></tr></thead>
            <tbody>
              {steps.map((step, index) => (
                <tr key={step.key}>
                  <td>{index + 1}</td>
                  <td>
                    <LookupSelect resource="boyahane-operations" label="Operasyon" hideLabel idSuffix={`-${step.key}`} required value={step.operationId}
                      onChange={(value) => setSteps(steps.map((row) => (row.key === step.key ? { ...row, operationId: value } : row)))} />
                  </td>
                  <td>
                    <button type="button" className="button" aria-label="Yukarı" disabled={index === 0} onClick={() => move(index, -1)}><ArrowUp aria-hidden="true" /></button>
                    <button type="button" className="button" aria-label="Aşağı" disabled={index === steps.length - 1} onClick={() => move(index, 1)}><ArrowDown aria-hidden="true" /></button>
                    <button type="button" className="button" aria-label="Sil" disabled={steps.length === 1} onClick={() => setSteps(steps.filter((row) => row.key !== step.key))}><X aria-hidden="true" /></button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <div><button type="button" className="button" onClick={() => setSteps([...steps, { key: Date.now(), operationId: null }])}><Plus aria-hidden="true" />Adım ekle</button></div>
        <p className="muted">Rota değişikliği açılmış partileri etkilemez; yeni partiler bu adımlarla açılır.</p>

        <ErrorAlert error={error ?? removeState.error} />
        <footer className="form__actions">
          {route && !route.isDefault && <button type="button" className="button button--danger-ghost" disabled={removeState.isLoading} onClick={handleDelete}>Sil</button>}
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading}>Kaydet</button>
        </footer>
      </form>
    </Modal>
  )
}

/** Operasyon rotaları: her işlem türünün adımları. Varsayılan rota partiye kendiliğinden gelir. */
export function BoyahaneRoutesPage() {
  const canManage = usePermission(Permissions.BoyahaneOperationManage)
  const { data, error, isLoading } = useGetBoyahaneRoutesQuery()
  const [editing, setEditing] = useState<BoyahaneRoute | null | undefined>(undefined)

  return (
    <section className="page">
      <h1 className="visually-hidden">Operasyon Rotaları</h1>
      <div className="toolbar">
        <div className="toolbar__actions">
          {canManage && <button type="button" className="button button--primary" onClick={() => setEditing(null)}><Plus aria-hidden="true" />Yeni rota</button>}
        </div>
      </div>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {data && (
        <div className="table-wrap">
          <table className="table">
            <thead><tr><th>Kod</th><th>Ad</th><th>İşlem türü</th><th>Adımlar</th><th>Durum</th></tr></thead>
            <tbody>
              {data.map((route) => (
                <tr key={route.id} className={canManage ? 'table__row--link' : undefined} onClick={() => canManage && setEditing(route)}>
                  <td><code>{route.code}</code></td>
                  <td>{route.name}</td>
                  <td>{orderProcessTypeLabels[route.processType]}</td>
                  <td>{route.steps.map((step) => step.operationName).join(' → ')}</td>
                  <td>
                    {route.isDefault && <span className="badge badge--info">Varsayılan</span>}
                    {!route.isActive && <span className="badge badge--neutral">Pasif</span>}
                  </td>
                </tr>
              ))}
              {data.length === 0 && <tr><td colSpan={5} className="table__empty">Rota bulunamadı.</td></tr>}
            </tbody>
          </table>
        </div>
      )}
      {editing !== undefined && <RouteDialog route={editing} onClose={() => setEditing(undefined)} />}
    </section>
  )
}
