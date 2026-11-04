import { Factory } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { fieldErrors } from '../../api/envelope'
import { ErrorAlert, TextField } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { RecordLink } from '../../components/RecordLink'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDate, formatQuantity } from '../../utils/format'
import { useCreateProductionFromDemandMutation, useGetPlanningDemandQuery } from './planningApi'
import type { PlanningDemand } from './planningApi'
import { dueText } from './planningPresentation'

/** Planlanacak adetten iş emri: renk / beden adetleri (planlanacak kadar önerilir), termin ve not. */
function CreateProductionDialog({ demand, onClose, onCreated }: {
  demand: PlanningDemand
  onClose: () => void
  onCreated: (order: { id: number; orderNo: string }) => void
}) {
  const [create, { isLoading, error }] = useCreateProductionFromDemandMutation()
  const [quantities, setQuantities] = useState<Record<string, string>>(() =>
    Object.fromEntries(demand.items.map((item) => [`${item.colorId}:${item.sizeId}`, item.toPlan > 0 ? String(item.toPlan) : ''])))
  const [dueDate, setDueDate] = useState(demand.dueDate)
  const [notes, setNotes] = useState('')

  const items = demand.items
    .map((item) => ({ colorId: item.colorId, sizeId: item.sizeId, quantity: Number(quantities[`${item.colorId}:${item.sizeId}`] || 0) }))
    .filter((item) => item.quantity > 0)
  const total = items.reduce((sum, item) => sum + item.quantity, 0)

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await create({
      salesOrderId: demand.salesOrderId, productId: demand.productId, dueDate, notes: notes.trim() || null, items,
    })
    if ('data' in result && result.data) {
      onCreated(result.data)
    }
  }

  return (
    <Modal title={`İş emri aç · ${demand.salesOrderNo} · ${demand.productCode}`} onClose={onClose}>
      <form className="form" onSubmit={(event) => void handleSubmit(event)} noValidate>
        <p className="muted">
          Adetler, stoktan ve açık iş emirlerinden karşılanmayan kısım kadar önerildi. Fire payı için artırabilirsiniz.
          İş emri taslak açılır; İş Emirleri ekranında onaylanıp banta planlanır.
        </p>
        <div className="table-wrap">
          <table className="table table--compact demand-items">
            <thead>
              <tr>
                <th>Renk</th>
                <th>Beden</th>
                <th className="table__number">Sipariş</th>
                <th className="table__number">Stoktan</th>
                <th className="table__number">Üretimde</th>
                <th className="table__number">Üretilecek</th>
              </tr>
            </thead>
            <tbody>
              {demand.items.map((item) => {
                const key = `${item.colorId}:${item.sizeId}`
                return (
                  <tr key={key}>
                    <td>{item.colorName}</td>
                    <td>{item.sizeName}</td>
                    <td className="table__number">{formatQuantity(item.ordered)}</td>
                    <td className="table__number">{formatQuantity(item.fromStock)}</td>
                    <td className="table__number">{formatQuantity(item.inProduction)}</td>
                    <td className="table__number">
                      <input
                        className="matrix-input"
                        type="number"
                        min={0}
                        step={1}
                        aria-label={`${item.colorName} ${item.sizeName} üretilecek adet`}
                        value={quantities[key]}
                        onChange={(event) => setQuantities({ ...quantities, [key]: event.target.value })}
                      />
                    </td>
                  </tr>
                )
              })}
            </tbody>
            <tfoot>
              <tr>
                <th colSpan={5} scope="row">Toplam</th>
                <td className="table__number">{formatQuantity(total)}</td>
              </tr>
            </tfoot>
          </table>
        </div>
        <div className="form__pair">
          <TextField label="Üretim termini" type="date" value={dueDate} onChange={(event) => setDueDate(event.target.value)}
            errors={fieldErrors(error, 'dueDate')} hint="Varsayılan: müşteri siparişinin teslim tarihi." />
          <TextField label="Not" value={notes} maxLength={500} onChange={(event) => setNotes(event.target.value)} />
        </div>
        <ErrorAlert error={error} fields={['dueDate', 'items']} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading || total === 0}>
            {isLoading ? 'Açılıyor…' : 'İş emri aç'}
          </button>
        </footer>
      </form>
    </Modal>
  )
}

/**
 * Planlanacak müşteri siparişleri: onaylı siparişlerin stoktan ya da iş emrinden karşılanmamış adedi. Satış
 * siparişi doğrudan üretime gitmez; planlamacı buradan iş emri açar.
 */
export function DemandSection() {
  const canCreate = usePermission(Permissions.ProductionOrderCreate)
  const { data, error, isLoading } = useGetPlanningDemandQuery()
  const [creating, setCreating] = useState<PlanningDemand | null>(null)
  const [created, setCreated] = useState<{ id: number; orderNo: string } | null>(null)

  return (
    <>
      <h2 className="section-title">Planlanacak müşteri siparişleri</h2>
      <p className="muted">
        Onaylanan siparişlerin stoktan ayrılamayan ve henüz iş emrine dönüşmemiş adedi. İş emri açılınca
        sipariş buradan düşer ve İş Emirleri ekranına geçer.
      </p>
      {created && (
        <p className="alert alert--info" role="status">
          <RecordLink to={`/production-orders/${created.id}`}>{created.orderNo}</RecordLink> numaralı taslak iş emri açıldı.
        </p>
      )}
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {data && (
        <div className="table-wrap">
          <table className="table">
            <thead>
              <tr>
                <th>Müşteri siparişi</th>
                <th>Ürün</th>
                <th>Teslim</th>
                <th>Renk / beden</th>
                <th className="table__number">Sipariş</th>
                <th className="table__number">Stoktan</th>
                <th className="table__number">Üretimde</th>
                <th className="table__number">Planlanacak</th>
                <th aria-label="İşlemler" />
              </tr>
            </thead>
            <tbody>
              {data.map((demand) => (
                <tr key={`${demand.salesOrderId}-${demand.productId}`} className={demand.daysToDue < 0 ? 'table__row--overdue' : undefined}>
                  <td>
                    <RecordLink to={`/sales-orders/${demand.salesOrderId}`}>{demand.salesOrderNo}</RecordLink>
                    <div className="muted">{demand.customerName}{demand.customerOrderNo ? ` · ${demand.customerOrderNo}` : ''}</div>
                  </td>
                  <td>
                    <code>{demand.productCode}</code> {demand.productName}
                    {!demand.hasActiveRoute && <div><span className="badge badge--warning">Modelin aktif rotası yok</span></div>}
                  </td>
                  <td>
                    {formatDate(demand.dueDate)}
                    <div className="muted">{dueText(demand.daysToDue)}</div>
                  </td>
                  <td className="demand-sizes">
                    {demand.items.filter((item) => item.toPlan > 0).map((item) => `${item.colorName} ${item.sizeName}: ${item.toPlan}`).join(' · ')}
                  </td>
                  <td className="table__number">{formatQuantity(demand.ordered)}</td>
                  <td className="table__number">{formatQuantity(demand.fromStock)}</td>
                  <td className="table__number">{formatQuantity(demand.inProduction)}</td>
                  <td className="table__number"><strong>{formatQuantity(demand.toPlan)}</strong></td>
                  <td className="table__actions">
                    {canCreate && (
                      <button type="button" className="button" onClick={() => setCreating(demand)}>
                        <Factory aria-hidden="true" />
                        İş emri aç
                      </button>
                    )}
                  </td>
                </tr>
              ))}
              {data.length === 0 && (
                <tr><td colSpan={9} className="table__empty">Planlanacak müşteri siparişi yok.</td></tr>
              )}
            </tbody>
          </table>
        </div>
      )}
      {creating && (
        <CreateProductionDialog
          demand={creating}
          onClose={() => setCreating(null)}
          onCreated={(order) => {
            setCreating(null)
            setCreated(order)
          }}
        />
      )}
    </>
  )
}
