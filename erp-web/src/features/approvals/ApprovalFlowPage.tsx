import { ArrowDown, ArrowUp, Plus, Save, X } from 'lucide-react'
import { useState } from 'react'
import { fieldErrors } from '../../api/envelope'
import { ErrorAlert, TextField } from '../../components/FormControls'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDateTime } from '../../utils/format'
import { parseDecimal } from '../../utils/money'
import {
  useGetApprovalFlowOptionsQuery, useGetSalesOrderApprovalFlowQuery, useSaveSalesOrderApprovalFlowMutation,
} from './approvalFlowsApi'
import type { ApprovalFlow, ApprovalFlowOptions, ApproverType } from './approvalFlowsApi'

interface StepDraft {
  key: string
  name: string
  approverType: ApproverType
  roleId: number | null
  userIds: number[]
  minAmount: string
}

const newStep = (): StepDraft => ({
  key: crypto.randomUUID(), name: '', approverType: 'ROLE', roleId: null, userIds: [], minAmount: '',
})

function FlowEditor({ flow, options }: { flow: ApprovalFlow; options: ApprovalFlowOptions }) {
  const canManage = usePermission(Permissions.ApprovalFlowManage)
  const [save, { isLoading, error, isSuccess }] = useSaveSalesOrderApprovalFlowMutation()
  const [name, setName] = useState(flow.name)
  const [isActive, setIsActive] = useState(flow.isDefined ? flow.isActive : true)
  const [steps, setSteps] = useState<StepDraft[]>(() =>
    flow.steps.length > 0
      ? flow.steps.map((step) => ({
          key: String(step.id),
          name: step.name,
          approverType: step.approverType,
          roleId: step.roleId,
          userIds: step.users.map((user) => user.id),
          minAmount: step.minAmount === null ? '' : String(step.minAmount).replace('.', ','),
        }))
      : [newStep()],
  )

  const update = (key: string, change: Partial<StepDraft>) =>
    setSteps((current) => current.map((step) => (step.key === key ? { ...step, ...change } : step)))

  const move = (index: number, offset: number) =>
    setSteps((current) => {
      const next = [...current]
      const [item] = next.splice(index, 1)
      next.splice(index + offset, 0, item)
      return next
    })

  const handleSave = () =>
    void save({
      name,
      isActive,
      steps: steps.map((step) => ({
        name: step.name,
        approverType: step.approverType,
        roleId: step.approverType === 'ROLE' ? step.roleId : null,
        userIds: step.approverType === 'USERS' ? step.userIds : null,
        // Geçersiz eşik -1 gönderilir; sunucu hangi adımın hatalı olduğunu söyler.
        minAmount: step.minAmount.trim() === '' ? null : parseDecimal(step.minAmount.replace(/\./g, '')) ?? -1,
      })),
    })

  const userName = (id: number) => options.users.find((user) => user.id === id)?.fullName ?? `#${id}`

  return (
    <>
      <div className="toolbar">
        <span className="muted">
          {flow.isDefined
            ? `Son değişiklik: ${flow.updatedBy ?? '—'} · ${formatDateTime(flow.updatedAt)}`
            : 'Henüz akış tanımlanmadı; siparişleri "Sipariş onayla" yetkisi olan herkes tek adımda onaylıyor.'}
        </span>
        {canManage && (
          <div className="toolbar__actions">
            <button type="button" className="button button--primary" onClick={handleSave} disabled={isLoading}>
              <Save aria-hidden="true" />
              {isLoading ? 'Kaydediliyor…' : 'Kaydet'}
            </button>
          </div>
        )}
      </div>
      {isSuccess && <p className="alert alert--info" role="status">Onay akışı kaydedildi. Yeni gönderilen siparişler bu akışla onaylanır.</p>}
      <ErrorAlert error={error} fields={['name', 'steps']} />

      <section className="card approval-flow">
        <fieldset className="approval-flow__head" disabled={!canManage}>
          <TextField label="Akış adı" value={name} maxLength={100} onChange={(event) => setName(event.target.value)} errors={fieldErrors(error, 'name')} />
          <label className="checkbox">
            <input type="checkbox" checked={isActive} onChange={(event) => setIsActive(event.target.checked)} />
            Akış açık (kapalıysa siparişler akışsız, "Sipariş onayla" yetkisiyle onaylanır)
          </label>
        </fieldset>

        <div className="table-wrap recipe-table approval-flow__table">
          <table className="table table--compact">
            <colgroup>
              <col className="approval-flow__no" />
              <col className="approval-flow__name" />
              <col className="approval-flow__type" />
              <col />
              <col className="approval-flow__amount" />
              <col className="approval-flow__tools" />
            </colgroup>
            <thead>
              <tr>
                <th>Sıra</th>
                <th>Adım adı</th>
                <th>Onaylayan</th>
                <th>Rol / kişiler</th>
                <th className="table__number">Tutar eşiği (TRY)</th>
                <th aria-label="Adım işlemleri" />
              </tr>
            </thead>
            <tbody>
              {steps.map((step, index) => (
                <tr key={step.key}>
                  <td className="table__number">{index + 1}</td>
                  <td>
                    <input aria-label={`${index + 1}. adım adı`} value={step.name} maxLength={100} disabled={!canManage}
                      placeholder="Örn. Satış müdürü" onChange={(event) => update(step.key, { name: event.target.value })} />
                  </td>
                  <td>
                    <select aria-label={`${index + 1}. adım onaylayan türü`} value={step.approverType} disabled={!canManage}
                      onChange={(event) => update(step.key, { approverType: event.target.value as ApproverType })}>
                      <option value="ROLE">Roldeki herkes</option>
                      <option value="USERS">Seçili kişiler</option>
                    </select>
                  </td>
                  <td>
                    {step.approverType === 'ROLE' ? (
                      <select aria-label={`${index + 1}. adım rolü`} value={step.roleId ?? ''} disabled={!canManage}
                        onChange={(event) => update(step.key, { roleId: event.target.value ? Number(event.target.value) : null })}>
                        <option value="">Rol seçin…</option>
                        {options.roles.map((role) => <option key={role.id} value={role.id}>{role.name}</option>)}
                      </select>
                    ) : (
                      <div className="approval-flow__users">
                        {step.userIds.map((id) => (
                          <span key={id} className="chip">
                            {userName(id)}
                            {canManage && (
                              <button type="button" aria-label={`${userName(id)} kişisini çıkar`}
                                onClick={() => update(step.key, { userIds: step.userIds.filter((item) => item !== id) })}>
                                <X aria-hidden="true" />
                              </button>
                            )}
                          </span>
                        ))}
                        {canManage && (
                          <select aria-label={`${index + 1}. adıma kişi ekle`} value=""
                            onChange={(event) => event.target.value && update(step.key, { userIds: [...step.userIds, Number(event.target.value)] })}>
                            <option value="">Kişi ekle…</option>
                            {options.users.filter((user) => !step.userIds.includes(user.id)).map((user) => (
                              <option key={user.id} value={user.id}>{user.fullName} ({user.userName})</option>
                            ))}
                          </select>
                        )}
                      </div>
                    )}
                  </td>
                  <td>
                    <input className="approval-flow__amount-input" aria-label={`${index + 1}. adım tutar eşiği`} inputMode="decimal"
                      placeholder="Her siparişte" value={step.minAmount} disabled={!canManage}
                      onChange={(event) => update(step.key, { minAmount: event.target.value })} />
                  </td>
                  <td className="approval-flow__tools-cell">
                    {canManage && (
                      <>
                        <button type="button" className="icon-button" title="Yukarı taşı" aria-label="Yukarı taşı" disabled={index === 0} onClick={() => move(index, -1)}>
                          <ArrowUp aria-hidden="true" />
                        </button>
                        <button type="button" className="icon-button" title="Aşağı taşı" aria-label="Aşağı taşı" disabled={index === steps.length - 1} onClick={() => move(index, 1)}>
                          <ArrowDown aria-hidden="true" />
                        </button>
                        <button type="button" className="icon-button" title="Adımı sil" aria-label="Adımı sil" onClick={() => setSteps((current) => current.filter((item) => item.key !== step.key))}>
                          <X aria-hidden="true" />
                        </button>
                      </>
                    )}
                  </td>
                </tr>
              ))}
              {steps.length === 0 && (
                <tr><td colSpan={6} className="table__empty">Adım yok; siparişler akışsız onaylanır.</td></tr>
              )}
            </tbody>
          </table>
        </div>
        {canManage && (
          <div className="approval-flow__add">
            <button type="button" className="button" onClick={() => setSteps((current) => [...current, newStep()])}>
              <Plus aria-hidden="true" />
              Adım ekle
            </button>
          </div>
        )}
        <p className="muted form__note">
          Adımlar sırayla uygulanır: bir adımdaki rolden ya da kişilerden biri onaylayınca sipariş sıradaki adıma geçer.
          Tutar eşiği verilen adım yalnızca sipariş tutarı (TRY'ye çevrilmiş) eşiğe ulaşınca uygulanır. Akış değişikliği
          yalnızca bundan sonra onaya gönderilen siparişleri etkiler; onaya gönderen kişinin notu tüm onaylayıcılara görünür.
        </p>
      </section>
    </>
  )
}

/** Satış siparişi onay akışı (Yönetim): sıralı adımlar, rol ya da kişi ataması, tutar eşiği. */
export function ApprovalFlowPage() {
  const flow = useGetSalesOrderApprovalFlowQuery()
  const options = useGetApprovalFlowOptionsQuery()

  return (
    <section className="page">
      <h1 className="visually-hidden">Onay Akışları</h1>
      {(flow.isLoading || options.isLoading) && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={flow.error ?? options.error} />
      {flow.data && options.data && <FlowEditor key={flow.data.updatedAt ?? 'new'} flow={flow.data} options={options.data} />}
    </section>
  )
}
