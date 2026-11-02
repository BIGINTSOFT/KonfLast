import { useState } from 'react'
import { ErrorAlert } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { formatDate, formatDateTimeShort, formatQuantity } from '../../utils/format'
import { useSaveLinePlanMutation } from '../productionLines/productionLinesApi'
import { useGetPlanSuggestionQuery } from './planningApi'
import type { LineSuggestion, PlanningOrder } from './planningApi'
import { formatHours } from './planningPresentation'

interface SuggestionDialogProps {
  order: PlanningOrder
  canPlan: boolean
  onClose: () => void
}

/**
 * Siparişin planlanmamış adedi için bant önerileri. Süre, başlangıç ve bitişi sunucu hesaplar; "Planla"
 * öneriyi olduğu gibi bant planı olarak kaydeder (çakışma ve adet kurallarını yine sunucu denetler).
 */
export function SuggestionDialog({ order, canPlan, onClose }: SuggestionDialogProps) {
  const { data, error, isLoading } = useGetPlanSuggestionQuery({ productionOrderId: order.id })
  const [savePlan, saveState] = useSaveLinePlanMutation()
  const [savingLineId, setSavingLineId] = useState<number | null>(null)
  const feasibleCount = data?.lines.filter((line) => line.feasible).length ?? 0

  const handlePlan = async (line: LineSuggestion) => {
    if (!data || !line.suggestedStart || !line.suggestedEnd) {
      return
    }

    setSavingLineId(line.lineId)
    const result = await savePlan({
      body: {
        productionLineId: line.lineId,
        productionOrderId: order.id,
        plannedStart: line.suggestedStart,
        plannedEnd: line.suggestedEnd,
        plannedQuantity: data.quantity,
        notes: null,
      },
    })
    setSavingLineId(null)
    if (!('error' in result)) {
      onClose()
    }
  }

  return (
    <Modal title={`Plan önerisi: ${order.orderNo}`} onClose={onClose}>
      {isLoading && <p role="status">Hesaplanıyor…</p>}
      <ErrorAlert error={error} />
      <ErrorAlert error={saveState.error} />

      {data && (
        <div className="plan-suggestions">
          <dl className="plan-suggestions__facts">
            <div><dt>Model</dt><dd><code>{data.modelCode}</code></dd></div>
            <div><dt>Planlanacak</dt><dd>{formatQuantity(data.quantity)} adet</dd></div>
            <div><dt>Termin</dt><dd>{formatDate(data.dueDate)}</dd></div>
            <div><dt>Verim</dt><dd>%{formatQuantity(data.efficiencyPercent)}</dd></div>
            <div><dt>Uygun bant</dt><dd>{feasibleCount} / {data.lines.length}</dd></div>
          </dl>
          {data.calendarAssumed && (
            <p className="alert alert--info">Vardiya tanımlı değil; süreler gün boyu çalışma varsayımıyla hesaplandı.</p>
          )}

          <div className="table-wrap">
            <table className="table table--compact">
              <colgroup>
                <col className="plan-suggestions__line" />
                <col className="plan-suggestions__rate" />
                <col className="plan-suggestions__hours" />
                <col className="plan-suggestions__time" />
                <col className="plan-suggestions__time" />
                <col className="plan-suggestions__due" />
                {canPlan && <col className="plan-suggestions__action" />}
              </colgroup>
              <thead>
                <tr>
                  <th>Bant</th>
                  <th className="table__number">Hız / darboğaz</th>
                  <th className="table__number">Süre</th>
                  <th>Başlangıç</th>
                  <th>Bitiş</th>
                  <th>Termin</th>
                  {canPlan && <th aria-label="İşlemler" />}
                </tr>
              </thead>
              <tbody>
                {data.lines.map((line) => (
                  <tr key={line.lineId} className={line.feasible ? undefined : 'plan-suggestions__row--off'}>
                    <td>
                      <code>{line.lineCode}</code>
                      <div className="muted">{line.name}</div>
                    </td>
                    {line.feasible ? (
                      <>
                        <td className="table__number">
                          {line.ratePerHour === null ? '—' : `${formatQuantity(Math.floor(line.ratePerHour))} adet/sa`}
                          {line.bottleneckOperationName && (
                            <div className="muted" title="Darboğaz operasyon">{line.bottleneckOperationName}</div>
                          )}
                        </td>
                        <td className="table__number">{formatHours(line.requiredHours)}</td>
                        <td>{formatDateTimeShort(line.suggestedStart)}</td>
                        <td>{formatDateTimeShort(line.suggestedEnd)}</td>
                        <td>
                          {line.meetsDueDate ? (
                            <span className="badge badge--ok">Yetişiyor</span>
                          ) : (
                            <span className="badge badge--danger">Yetişmiyor</span>
                          )}
                        </td>
                        {canPlan && (
                          <td className="table__actions">
                            <button
                              type="button"
                              className="button button--primary"
                              disabled={savingLineId !== null}
                              onClick={() => void handlePlan(line)}
                            >
                              {savingLineId === line.lineId ? 'Kaydediliyor…' : 'Planla'}
                            </button>
                          </td>
                        )}
                      </>
                    ) : (
                      <td colSpan={canPlan ? 6 : 5}>
                        <span className="badge badge--neutral">Uygun değil</span>
                        <span className="plan-suggestions__reason">{line.reason}</span>
                      </td>
                    )}
                  </tr>
                ))}
                {data.lines.length === 0 && (
                  <tr>
                    <td colSpan={canPlan ? 7 : 6} className="table__empty">Aktif bant yok.</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
          <p className="field__hint">
            Öneri, bandın mevcut planlarıyla çakışmayan en erken zamanı gösterir; hız, darboğaz operasyona göre
            hesaplanır. Planı sonradan bant sayfasından düzenleyebilir ya da iptal edebilirsiniz.
          </p>
        </div>
      )}
    </Modal>
  )
}
