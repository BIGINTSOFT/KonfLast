import { useState } from 'react'
import type { FormEvent } from 'react'
import { fieldErrors } from '../../api/envelope'
import { ErrorAlert, TextField } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { useGetLookupQuery } from '../masterData/masterDataApi'
import { useCreateInspectionMutation } from './qualityApi'
import type { BundleQuality } from './qualityApi'
import { toQuantity } from './qualityPresentation'

interface InspectionDialogProps {
  quality: BundleQuality
  onClose: () => void
}

/**
 * Kalite kontrolü girişi. Adetlerin geçerliliğini (üretilenden fazla olmama, sonraki operasyonun işlediği
 * parçaların geri alınamaması) sunucu denetler; burada yalnızca sunucunun bildirdiği sınırlar gösterilir.
 */
export function InspectionDialog({ quality, onClose }: InspectionDialogProps) {
  const [createInspection, { isLoading, error }] = useCreateInspectionMutation()
  // Ret (ıskarta) kararı ayrı yetkidir; yetkisi olmayan kullanıcı yalnızca sağlam ve tamir adedi girer.
  const canReject = usePermission(Permissions.QualityReject)
  const defectTypes = useGetLookupQuery({ resource: 'defect-types', take: 100 })

  const [operationId, setOperationId] = useState(quality.operations[0]?.bundleOperationId ?? 0)
  const operation = quality.operations.find((o) => o.bundleOperationId === operationId)
  const [inspected, setInspected] = useState(String(operation?.inspectableQuantity ?? ''))
  const [rejected, setRejected] = useState('')
  const [rework, setRework] = useState('')
  const [hold, setHold] = useState(false)
  const [note, setNote] = useState('')
  const [defects, setDefects] = useState<Record<number, string>>({})
  const [reworkTaskId, setReworkTaskId] = useState<number | null>(null)

  // Bu operasyonun yeniden işlenmiş, tekrar kontrol bekleyen tamir işleri.
  const awaitingReinspection = quality.reworkTasks.filter(
    (task) => task.bundleOperationId === operationId && task.status === 'DONE',
  )
  const hasDefective = toQuantity(rejected) + toQuantity(rework) > 0

  const selectOperation = (id: number) => {
    setOperationId(id)
    setReworkTaskId(null)
    setInspected(String(quality.operations.find((o) => o.bundleOperationId === id)?.inspectableQuantity ?? ''))
  }

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await createInspection({
      bundleOperationId: operationId,
      inspectedQuantity: toQuantity(inspected),
      rejectedQuantity: toQuantity(rejected),
      reworkQuantity: toQuantity(rework),
      hold,
      defects: Object.entries(defects)
        .map(([id, quantity]) => ({ defectTypeId: Number(id), quantity: toQuantity(quantity) }))
        .filter((defect) => defect.quantity > 0),
      note: note.trim() || null,
      reworkTaskId,
    })
    if (!('error' in result)) {
      onClose()
    }
  }

  return (
    <Modal title={`Kalite kontrolü: Demet ${quality.bundleNo}`} onClose={onClose}>
      <form className="form" onSubmit={(event) => void handleSubmit(event)} noValidate>
        <label className="field">
          <span>Kontrol edilen operasyon</span>
          <select value={operationId} onChange={(event) => selectOperation(Number(event.target.value))}>
            {quality.operations.map((o) => (
              <option key={o.bundleOperationId} value={o.bundleOperationId}>
                {o.sequenceNo} · {o.operationName} ({o.inspectableQuantity} adet üretildi)
              </option>
            ))}
          </select>
          {operation && (
            <small className="field__hint">
              {operation.takeBackLimit > 0
                ? `Ret / tamir için en fazla ${operation.takeBackLimit} adet geri alınabilir.`
                : 'Bu operasyondan parça geri alınamaz (işlemde ya da sonraki operasyon parçaları işlemiş).'}
            </small>
          )}
        </label>

        {awaitingReinspection.length > 0 && (
          <label className="field">
            <span>Tamir sonrası tekrar kontrol</span>
            <select
              value={reworkTaskId ?? ''}
              onChange={(event) => setReworkTaskId(event.target.value ? Number(event.target.value) : null)}
            >
              <option value="">Hayır, olağan kontrol</option>
              {awaitingReinspection.map((task) => (
                <option key={task.id} value={task.id}>
                  Tamir #{task.id} ({task.quantity} adet) tekrar kontrol ediliyor
                </option>
              ))}
            </select>
            <small className="field__hint">Seçilirse kontrol, tamir işini onaylar ya da reddeder.</small>
          </label>
        )}

        <TextField
          label="Kontrol edilen adet"
          type="number"
          min={1}
          value={inspected}
          onChange={(event) => setInspected(event.target.value)}
          errors={fieldErrors(error, 'inspectedQuantity')}
        />
        <TextField
          label="Ret (ıskarta) adedi"
          type="number"
          min={0}
          value={canReject ? rejected : ''}
          disabled={!canReject}
          onChange={(event) => setRejected(event.target.value)}
          errors={fieldErrors(error, 'rejectedQuantity')}
          hint={canReject ? 'Parçalar operasyonun hatalı adedine geçer; sonraki operasyonlara gitmez.' : 'Ret kararı verme yetkiniz yok.'}
        />
        <TextField
          label="Tamir adedi"
          type="number"
          min={0}
          value={rework}
          onChange={(event) => setRework(event.target.value)}
          errors={fieldErrors(error, 'reworkQuantity')}
          hint="Parçalar bu operasyonda yeniden işlenmek üzere geri döner."
        />

        {hasDefective && (
          <fieldset className="checkbox-list">
            <legend>Hata türleri ve adetleri</legend>
            {defectTypes.data?.map((type) => (
              <label key={type.id} className="field">
                <span>{type.name}</span>
                <input
                  type="number"
                  min={0}
                  value={defects[type.id] ?? ''}
                  onChange={(event) => setDefects((current) => ({ ...current, [type.id]: event.target.value }))}
                />
              </label>
            ))}
            {fieldErrors(error, 'defects')?.map((message) => (
              <small key={message} className="field__error">{message}</small>
            ))}
          </fieldset>
        )}

        <label className="checkbox">
          <input type="checkbox" checked={hold} onChange={(event) => setHold(event.target.checked)} />
          Demeti kalite bekletmesine al (serbest bırakılana kadar hiçbir operasyonu başlatılamaz)
        </label>

        <label className="field">
          <span>Not (isteğe bağlı)</span>
          <textarea rows={2} maxLength={500} value={note} onChange={(event) => setNote(event.target.value)} />
        </label>

        <ErrorAlert error={error ?? defectTypes.error} fields={['inspectedQuantity', 'rejectedQuantity', 'reworkQuantity', 'defects']} />

        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading || !operation}>
            {isLoading ? 'Kaydediliyor…' : 'Kontrolü kaydet'}
          </button>
        </footer>
      </form>
    </Modal>
  )
}
