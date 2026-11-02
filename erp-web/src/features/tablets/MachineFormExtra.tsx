import { useEffect, useState } from 'react'
import { CheckboxList, ErrorAlert } from '../../components/FormControls'
import { formatDate, todayIsoDate } from '../../utils/format'
import { MachineStatusBadge } from '../masterData/cells'
import { useGetLookupQuery } from '../masterData/masterDataApi'
import type { FormExtraProps } from '../masterData/resources'
import { mergeOptions } from '../workSessions/workSessionPresentation'
import { useGetMachineOperationsQuery, useSaveMachineOperationsMutation } from './tabletsApi'

type Tab = 'general' | 'technical' | 'maintenance' | 'operations'

const tabLabels: Record<Tab, string> = {
  general: 'Genel',
  technical: 'Teknik ve satın alma',
  maintenance: 'Bakım',
  operations: 'Operasyonlar',
}

/** Standart alanların bulunduğu sekme; sunucu bir alanı reddedince o sekme açılır. */
const fieldTabs: Record<string, Tab> = {
  brand: 'technical',
  modelNo: 'technical',
  serialNo: 'technical',
  manufactureYear: 'technical',
  assetNo: 'technical',
  purchaseDate: 'technical',
  supplierId: 'technical',
  warrantyEndDate: 'technical',
  maintenanceIntervalDays: 'maintenance',
  lastMaintenanceDate: 'maintenance',
  notes: 'maintenance',
}

/** Son bakım tarihine bakım aralığı eklenir (yyyy-aa-gg). */
function nextMaintenance(lastDate: unknown, intervalDays: unknown): string | null {
  const days = Number(intervalDays)
  if (typeof lastDate !== 'string' || lastDate === '' || !Number.isInteger(days) || days <= 0) {
    return null
  }
  const date = new Date(`${lastDate}T00:00:00Z`)
  date.setUTCDate(date.getUTCDate() + days)
  return date.toISOString().slice(0, 10)
}

/**
 * Makine formu: genel, teknik / satın alma ve bakım bilgileri ile makinenin yapabildiği ek operasyonlar. Ek
 * operasyonlar makine kaydının alanı değildir; makine kaydedildikten sonra (yeni kayıtta Id oluşunca) kendi ucuna yazılır.
 */
export function MachineFormExtra({ row, values, fields, activeControl, disabled, errorFields, onAfterSaveChange }: FormExtraProps) {
  const [tab, setTab] = useState<Tab>('general')
  const errorKey = errorFields.join(',')
  const [seenErrors, setSeenErrors] = useState(errorKey)
  if (errorKey !== seenErrors) {
    setSeenErrors(errorKey)
    if (errorFields.length > 0) {
      setTab(errorFields.some((name) => !(name in fieldTabs)) ? 'general' : fieldTabs[errorFields[0]])
    }
  }

  const machineId = row?.id
  const skip = machineId === undefined
  const current = useGetMachineOperationsQuery(machineId ?? 0, { skip })
  const operations = useGetLookupQuery({ resource: 'operations', take: 100 })
  const [save] = useSaveMachineOperationsMutation()
  // Dokunulmadıkça sunucudaki seçim geçerlidir (null = değiştirilmedi).
  const [choice, setChoice] = useState<number[] | null>(null)

  const stored = current.data?.operations ?? []
  const selected = choice ?? stored.map((item) => item.id)
  const primaryId = typeof values.operationId === 'number' ? values.operationId : null

  useEffect(() => {
    onAfterSaveChange(async (id) => {
      if (choice === null) {
        return undefined
      }
      const result = await save({ machineId: id, operationIds: selected })
      if ('error' in result) {
        setTab('operations')
        return result.error
      }
      return undefined
    })

    return () => {
      onAfterSaveChange(null)
    }
  })

  const toggle = (id: number) =>
    setChoice(selected.includes(id) ? selected.filter((item) => item !== id) : [...selected, id])

  const nextDate = nextMaintenance(values.lastMaintenanceDate, values.maintenanceIntervalDays)
  const overdue = nextDate !== null && nextDate < todayIsoDate()
  const warrantyEnd = typeof values.warrantyEndDate === 'string' && values.warrantyEndDate !== '' ? values.warrantyEndDate : null
  const otherOperations = operations.data
    // Ana operasyon zaten yapılır; listede ayrıca gösterilmez.
    ? mergeOptions(operations.data, stored).filter((item) => item.id !== primaryId)
    : []

  return (
    <div className="machine-form">
      {row && (
        <dl className="stat-strip">
          <div><dt>Çalışma durumu</dt><dd><MachineStatusBadge status={String(row.status)} /></dd></div>
          <div><dt>Bant</dt><dd>{row.productionLineCode ? String(row.productionLineCode) : '—'}</dd></div>
          <div>
            <dt>Sonraki bakım</dt>
            <dd>
              {nextDate === null ? '—' : formatDate(nextDate)}
              {overdue && <span className="badge badge--danger">Gecikmiş</span>}
            </dd>
          </div>
          <div>
            <dt>Garanti</dt>
            <dd>
              {warrantyEnd === null ? '—' : formatDate(warrantyEnd)}
              {warrantyEnd !== null && warrantyEnd < todayIsoDate() && <span className="badge badge--neutral">Bitti</span>}
            </dd>
          </div>
        </dl>
      )}

      <div className="tabs" role="tablist" aria-label="Makine bilgileri">
        {(Object.keys(tabLabels) as Tab[]).map((key) => (
          <button
            key={key}
            type="button"
            role="tab"
            id={`machine-tab-${key}`}
            aria-selected={tab === key}
            aria-controls={`machine-panel-${key}`}
            className={`tabs__tab${tab === key ? ' tabs__tab--active' : ''}`}
            onClick={() => setTab(key)}
          >
            {tabLabels[key]}
            {key === 'operations' && selected.length > 0 && <span className="tabs__count">{selected.length}</span>}
          </button>
        ))}
      </div>

      {/* Sekmeler gizlenir, kaldırılmaz: diğer sekmedeki girişler korunur. */}
      <div className="tabs__panel" role="tabpanel" id="machine-panel-general" aria-labelledby="machine-tab-general" hidden={tab !== 'general'}>
        <div className="form__pair">
          {fields.machineCode}
          {fields.machineTypeId}
        </div>
        {fields.machineName}
        {fields.operationId}
        {activeControl}
      </div>

      <div className="tabs__panel" role="tabpanel" id="machine-panel-technical" aria-labelledby="machine-tab-technical" hidden={tab !== 'technical'}>
        <fieldset className="form__section">
          <legend>Teknik bilgiler</legend>
          <div className="form__pair">
            {fields.brand}
            {fields.modelNo}
          </div>
          <div className="form__pair">
            {fields.serialNo}
            {fields.manufactureYear}
          </div>
          {fields.assetNo}
        </fieldset>
        <fieldset className="form__section">
          <legend>Satın alma</legend>
          {fields.supplierId}
          <div className="form__pair">
            {fields.purchaseDate}
            {fields.warrantyEndDate}
          </div>
        </fieldset>
      </div>

      <div className="tabs__panel" role="tabpanel" id="machine-panel-maintenance" aria-labelledby="machine-tab-maintenance" hidden={tab !== 'maintenance'}>
        <div className="form__pair">
          {fields.maintenanceIntervalDays}
          {fields.lastMaintenanceDate}
        </div>
        <div className={overdue ? 'alert alert--error' : 'alert alert--info'}>
          {nextDate === null
            ? 'Sonraki bakım tarihi, bakım aralığı ve son bakım tarihi girilince hesaplanır.'
            : overdue
              ? `Periyodik bakım gecikmiş: ${formatDate(nextDate)} tarihinde yapılmalıydı.`
              : `Sonraki periyodik bakım: ${formatDate(nextDate)}.`}
        </div>
        {fields.notes}
      </div>

      <div className="tabs__panel" role="tabpanel" id="machine-panel-operations" aria-labelledby="machine-tab-operations" hidden={tab !== 'operations'}>
        <p className="muted form__note">
          Makinenin ana operasyonuna ek olarak yapabildiği operasyonlar. Tablette demet okutulduğunda yalnızca makinenin
          yapabildiği operasyonlar başlatılabilir.
        </p>
        {(current.isLoading || operations.isLoading) && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={current.error ?? operations.error} />
        {operations.data && (skip || current.data) && (
          <CheckboxList legend="Yapabildiği diğer operasyonlar">
            {otherOperations.map((item) => (
              <label key={item.id} className="checkbox">
                <input type="checkbox" disabled={disabled} checked={selected.includes(item.id)} onChange={() => toggle(item.id)} />
                {item.code} — {item.name}
              </label>
            ))}
          </CheckboxList>
        )}
      </div>
    </div>
  )
}
