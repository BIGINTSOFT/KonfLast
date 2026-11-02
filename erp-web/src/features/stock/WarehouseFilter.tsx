import { useGetLookupQuery } from '../masterData/masterDataApi'

export type WarehouseFilterValue = number | ''

/** Liste ekranlarının depo süzgeci; boş değer tüm depolardır. */
export function WarehouseFilter({ value, onChange }: { value: WarehouseFilterValue; onChange: (value: WarehouseFilterValue) => void }) {
  const warehouses = useGetLookupQuery({ resource: 'warehouses', take: 100 })
  return (
    <select
      aria-label="Depo filtresi"
      value={value}
      onChange={(event) => onChange(event.target.value ? Number(event.target.value) : '')}
    >
      <option value="">Tüm depolar</option>
      {warehouses.data?.map((warehouse) => (
        <option key={warehouse.id} value={warehouse.id}>{warehouse.code} — {warehouse.name}</option>
      ))}
    </select>
  )
}
