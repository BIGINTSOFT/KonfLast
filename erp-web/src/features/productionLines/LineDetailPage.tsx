import { ArrowDown, ArrowUp } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useParams } from 'react-router-dom'
import { fieldErrors, isRequestError } from '../../api/envelope'
import { ErrorAlert, TextField } from '../../components/FormControls'
import { RecordLink } from '../../components/RecordLink'
import { Modal } from '../../components/Modal'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDateTime, formatQuantity, isoToLocalInput, localInputToIso } from '../../utils/format'
import { MachineStatusBadge } from '../masterData/cells'
import { LookupSelect } from '../masterData/LookupSelect'
import {
  useAssignMachineMutation,
  useCancelLinePlanMutation,
  useGetLineOverviewQuery,
  useReorderMachinesMutation,
  useSaveLinePlanMutation,
  useUnassignMachineMutation,
} from './productionLinesApi'
import type { LineOverview, LinePlan } from './productionLinesApi'

function MachinesSection({ line, canUpdate }: { line: LineOverview; canUpdate: boolean }) {
  const [assign, assignState] = useAssignMachineMutation()
  const [unassign, unassignState] = useUnassignMachineMutation()
  const [reorder, reorderState] = useReorderMachinesMutation()
  const [machineId, setMachineId] = useState<number | null>(null)

  const busy = assignState.isLoading || unassignState.isLoading || reorderState.isLoading
  // Makine başka banttaysa sunucu bunu bildirir; kullanıcı onaylarsa taşınır.
  const onAnotherLine = isRequestError(assignState.error) && assignState.error.code === 'MACHINE_ALREADY_ASSIGNED'

  const handleAssign = async (transfer: boolean) => {
    if (machineId === null) {
      return
    }
    const result = await assign({ lineId: line.id, machineId, transfer })
    if (!('error' in result)) {
      setMachineId(null)
    }
  }

  const move = (index: number, direction: -1 | 1) => {
    const ids = line.machines.map((machine) => machine.machineId)
    const target = index + direction
    ;[ids[index], ids[target]] = [ids[target], ids[index]]
    void reorder({ lineId: line.id, machineIds: ids })
  }

  return (
    <>
      <h2 className="section-title">Makineler</h2>

      {canUpdate && (
        <div className="matrix-adder matrix-adder--narrow">
          <LookupSelect
            resource="machines"
            label="Banta makine ekle"
            value={machineId}
            onChange={(id) => {
              setMachineId(id)
              assignState.reset()
            }}
          />
          <button type="button" className="button" disabled={machineId === null || busy} onClick={() => void handleAssign(false)}>
            Ekle
          </button>
        </div>
      )}

      <ErrorAlert error={assignState.error ?? unassignState.error ?? reorderState.error} />
      {onAnotherLine && (
        <button type="button" className="button" disabled={busy} onClick={() => void handleAssign(true)}>
          Bu banta taşı
        </button>
      )}

      {line.machines.length === 0 ? (
        <p className="muted">Bu bantta henüz makine yok.</p>
      ) : (
        <div className="table-wrap">
          <table className="table">
            <thead>
              <tr>
                <th className="table__number">Sıra</th>
                <th>Makine</th>
                <th>Tip</th>
                <th>Operasyon</th>
                <th>Çalışma durumu</th>
                {canUpdate && <th aria-label="İşlemler" />}
              </tr>
            </thead>
            <tbody>
              {line.machines.map((machine, index) => (
                <tr key={machine.machineId}>
                  <td className="table__number">{machine.position}</td>
                  <td>
                    <code>{machine.machineCode}</code>
                    <div className="muted">{machine.machineName}</div>
                  </td>
                  <td>{machine.machineTypeName}</td>
                  <td>{machine.operationName ?? '—'}</td>
                  <td><MachineStatusBadge status={machine.status} /></td>
                  {canUpdate && (
                    <td className="table__actions">
                      <button
                        type="button"
                        className="button"
                        aria-label={`${machine.machineCode} yukarı taşı`}
                        disabled={busy || index === 0}
                        onClick={() => move(index, -1)}
                      >
                        <ArrowUp aria-hidden="true" />
                      </button>
                      <button
                        type="button"
                        className="button"
                        aria-label={`${machine.machineCode} aşağı taşı`}
                        disabled={busy || index === line.machines.length - 1}
                        onClick={() => move(index, 1)}
                      >
                        <ArrowDown aria-hidden="true" />
                      </button>
                      <button
                        type="button"
                        className="button"
                        disabled={busy}
                        onClick={() => void unassign({ lineId: line.id, machineId: machine.machineId })}
                      >
                        Çıkar
                      </button>
                    </td>
                  )}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </>
  )
}

function PlanDialog({ lineId, plan, onClose }: { lineId: number; plan?: LinePlan; onClose: () => void }) {
  const [save, { isLoading, error }] = useSaveLinePlanMutation()
  const [orderId, setOrderId] = useState<number | null>(plan?.productionOrderId ?? null)
  const [start, setStart] = useState(plan ? isoToLocalInput(plan.plannedStart) : '')
  const [end, setEnd] = useState(plan ? isoToLocalInput(plan.plannedEnd) : '')
  const [quantity, setQuantity] = useState(plan ? String(plan.plannedQuantity) : '')
  const [notes, setNotes] = useState(plan?.notes ?? '')

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await save({
      id: plan?.id,
      body: {
        productionLineId: lineId,
        productionOrderId: orderId ?? 0,
        // Boş zaman için sunucunun "zorunludur" hatasını ilgili alana bağlaması sağlanır.
        plannedStart: localInputToIso(start) ?? '0001-01-01T00:00:00Z',
        plannedEnd: localInputToIso(end) ?? '0001-01-01T00:00:00Z',
        plannedQuantity: Number(quantity || 0),
        notes: notes.trim() || null,
        rowVersion: plan?.rowVersion,
      },
    })
    if (!('error' in result)) {
      onClose()
    }
  }

  return (
    <Modal title={plan ? `Planı düzenle: ${plan.orderNo}` : 'Banta plan ekle'} onClose={onClose}>
      <form className="form" onSubmit={(event) => void handleSubmit(event)} noValidate>
        <LookupSelect
          resource="production-orders"
          label="Sipariş (onaylı veya üretimde)"
          value={orderId}
          onChange={setOrderId}
          required
          disabled={Boolean(plan)}
          errors={fieldErrors(error, 'productionOrderId')}
        />
        <div className="form__dates">
          <TextField
            label="Başlangıç"
            type="datetime-local"
            value={start}
            onChange={(event) => setStart(event.target.value)}
            errors={fieldErrors(error, 'plannedStart')}
          />
          <TextField
            label="Bitiş"
            type="datetime-local"
            value={end}
            onChange={(event) => setEnd(event.target.value)}
            errors={fieldErrors(error, 'plannedEnd')}
          />
        </div>
        <TextField
          label="Planlanan adet"
          type="number"
          min={1}
          step={1}
          value={quantity}
          onChange={(event) => setQuantity(event.target.value)}
          errors={fieldErrors(error, 'plannedQuantity')}
        />
        <TextField
          label="Not (isteğe bağlı)"
          value={notes}
          maxLength={500}
          onChange={(event) => setNotes(event.target.value)}
          errors={fieldErrors(error, 'notes')}
        />
        <ErrorAlert error={error} fields={['productionOrderId', 'plannedStart', 'plannedEnd', 'plannedQuantity', 'notes']} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading}>
            {isLoading ? 'Kaydediliyor…' : 'Kaydet'}
          </button>
        </footer>
      </form>
    </Modal>
  )
}

type PlanDialogState = { kind: 'create' } | { kind: 'edit'; plan: LinePlan }

function PlansSection({ line, canUpdate }: { line: LineOverview; canUpdate: boolean }) {
  const [cancelPlan, cancelState] = useCancelLinePlanMutation()
  const [dialog, setDialog] = useState<PlanDialogState | null>(null)
  const plans = [...(line.currentPlan ? [line.currentPlan] : []), ...line.upcomingPlans]

  return (
    <>
      <header className="page__header section-title">
        <h2>Üretim planı</h2>
        {canUpdate && (
          <button type="button" className="button button--primary" onClick={() => setDialog({ kind: 'create' })}>
            Plan ekle
          </button>
        )}
      </header>

      <ErrorAlert error={cancelState.error} />

      {plans.length === 0 ? (
        <p className="muted">Bu bant için şu an veya ileri tarihli plan yok.</p>
      ) : (
        <div className="table-wrap">
          <table className="table">
            <thead>
              <tr>
                <th>Sipariş</th>
                <th>Ürün</th>
                <th>Başlangıç</th>
                <th>Bitiş</th>
                <th className="table__number">Adet</th>
                <th>Durum</th>
                {canUpdate && <th aria-label="İşlemler" />}
              </tr>
            </thead>
            <tbody>
              {plans.map((plan) => (
                <tr key={plan.id}>
                  <td>
                    <RecordLink to={`/production-orders/${plan.productionOrderId}`}><code>{plan.orderNo}</code></RecordLink>
                    <div className="muted">{plan.customerName}</div>
                  </td>
                  <td>
                    <code>{plan.productCode}</code>
                    <div className="muted">{plan.productName}</div>
                  </td>
                  <td>{formatDateTime(plan.plannedStart)}</td>
                  <td>{formatDateTime(plan.plannedEnd)}</td>
                  <td className="table__number">{formatQuantity(plan.plannedQuantity)}</td>
                  <td>
                    {plan.isCurrent ? (
                      <span className="badge badge--ok"><span aria-hidden="true">▶</span> Şu an üretimde</span>
                    ) : (
                      <span className="badge badge--neutral"><span aria-hidden="true">◷</span> Sırada</span>
                    )}
                  </td>
                  {canUpdate && (
                    <td className="table__actions">
                      <button type="button" className="button" onClick={() => setDialog({ kind: 'edit', plan })}>
                        Düzenle
                      </button>
                      <button
                        type="button"
                        className="button"
                        disabled={cancelState.isLoading}
                        onClick={() => void cancelPlan({ id: plan.id, rowVersion: plan.rowVersion })}
                      >
                        İptal et
                      </button>
                    </td>
                  )}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {dialog?.kind === 'create' && <PlanDialog lineId={line.id} onClose={() => setDialog(null)} />}
      {dialog?.kind === 'edit' && <PlanDialog lineId={line.id} plan={dialog.plan} onClose={() => setDialog(null)} />}
    </>
  )
}

export function LineDetailPage() {
  const id = Number(useParams().id)
  const canUpdate = usePermission(Permissions.ProductionLineUpdate)
  // Bant planı girmek ayrı yetkidir; makine ataması bant düzenleme yetkisiyle yapılır.
  const canPlan = usePermission(Permissions.LinePlanManage)
  const { data: line, isLoading, error } = useGetLineOverviewQuery(id)

  if (isLoading) {
    return <section className="page"><p role="status">Yükleniyor…</p></section>
  }

  if (!line) {
    return (
      <section className="page">
        <ErrorAlert error={error} />
        <Link to="/production-lines">← Bantlara dön</Link>
      </section>
    )
  }

  return (
    <section className="page">
      <p><Link to="/production-lines">← Bantlara dön</Link></p>
      <h1>
        {line.name}{' '}
        <span className={`badge badge--${line.isActive ? 'ok' : 'neutral'}`}>{line.isActive ? 'Aktif' : 'Pasif'}</span>
      </h1>
      <p className="muted">
        <code>{line.lineCode}</code> · {line.factoryName} / {line.productionAreaName}
      </p>

      <div className="cards">
        <article className="card">
          <h2>Şu an üretilen</h2>
          {line.currentPlan ? (
            <dl className="status-list">
              <div><dt>Sipariş</dt><dd>{line.currentPlan.orderNo}</dd></div>
              <div><dt>Model</dt><dd>{line.currentPlan.modelCode}</dd></div>
              <div><dt>Müşteri</dt><dd>{line.currentPlan.customerName}</dd></div>
              <div><dt>Plan</dt><dd>{formatQuantity(line.currentPlan.plannedQuantity)} adet</dd></div>
            </dl>
          ) : (
            <p className="muted">Bu saat için plan yok.</p>
          )}
        </article>
        <article className="card">
          <h2>Bant</h2>
          <dl className="status-list">
            <div><dt>Makine sayısı</dt><dd>{line.machines.length}</dd></div>
            <div><dt>Sıradaki plan</dt><dd>{line.upcomingPlans.length}</dd></div>
          </dl>
        </article>
      </div>

      <PlansSection line={line} canUpdate={canPlan} />
      <MachinesSection line={line} canUpdate={canUpdate} />
    </section>
  )
}
