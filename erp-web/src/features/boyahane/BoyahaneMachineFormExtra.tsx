import { useEffect, useState } from 'react'
import { useGetMasterListQuery } from '../masterData/masterDataApi'
import type { FormExtraProps } from '../masterData/resources'
import { useGetBoyahaneMachineOperationsQuery, useSetBoyahaneMachineOperationsMutation } from './boyahaneInsightsApi'

const MAX_OPERATIONS = 100

/** Makine formu: makinenin yapabildiği operasyonlar makine kaydedildikten sonra yazılır. */
export function BoyahaneMachineFormExtra({ row, fields, activeControl, disabled, onAfterSaveChange }: FormExtraProps) {
  const { data: operations } = useGetMasterListQuery({ resource: 'boyahane-operations', page: 1, pageSize: MAX_OPERATIONS })
  const { data: current } = useGetBoyahaneMachineOperationsQuery(row?.id ?? 0, { skip: !row })
  const [setOperations] = useSetBoyahaneMachineOperationsMutation()
  const [changes, setChanges] = useState<Map<number, boolean>>(new Map())
  const machineOperations = operations?.items.filter((operation) => operation.needsMachine && (operation.isActive || current?.includes(operation.id))) ?? []
  const isSelected = (id: number) => changes.get(id) ?? current?.includes(id) ?? false

  useEffect(() => {
    onAfterSaveChange(async (id: number) => {
      const operationIds = machineOperations.filter((operation) => isSelected(operation.id)).map((operation) => operation.id)
      const result = await setOperations({ machineId: id, operationIds })
      return 'error' in result ? result.error : undefined
    })
    return () => onAfterSaveChange(null)
    // Seçim değiştikçe kayıt sonrası iş güncellenir.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [changes, current, operations])

  return (
    <>
      {Object.values(fields)}
      <fieldset className="form__section" disabled={disabled}>
        <legend>Bu makinede yapılan operasyonlar</legend>
        {machineOperations.map((operation) => (
          <label key={operation.id} className="checkbox">
            <input type="checkbox" checked={isSelected(operation.id)} onChange={(event) => setChanges(new Map(changes).set(operation.id, event.target.checked))} />
            {String(operation.name)}
          </label>
        ))}
        {operations && machineOperations.length === 0 && <p className="muted">Makinede yapılan operasyon tanımlı değil.</p>}
      </fieldset>
      {activeControl}
    </>
  )
}
