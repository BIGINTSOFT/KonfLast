import { useState } from 'react'
import type { FormEvent } from 'react'
import { fieldErrors } from '../../api/envelope'
import { ErrorAlert, TextField } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { formatQuantity } from '../../utils/format'
import { LookupSelect } from '../masterData/LookupSelect'
import { useGetManualReportOptionsQuery, useManualReportMutation } from './bundlesApi'

interface ManualReportDialogProps {
  operationId: number
  bundleNo: string
  onClose: () => void
}

const toQuantity = (text: string) => (text.trim() === '' ? 0 : Number(text))

/**
 * Ofisten elle üretim bildirimi: tablet arızalandığında ya da iş askıda kaldığında operasyon tablet ve kart
 * olmadan ilerletilir. İşlemdeki işte makine ve operatör o işinkidir; başlamamış / bekleyen işte seçilir.
 */
export function ManualReportDialog({ operationId, bundleNo, onClose }: ManualReportDialogProps) {
  const { data: options, error: optionsError } = useGetManualReportOptionsQuery(operationId)
  const [report, { isLoading, error }] = useManualReportMutation()
  // Çift tıklama / ağ tekrarı aynı kimlikle gelir ve ikinci kez işlenmez.
  const [requestId] = useState(() => crypto.randomUUID())
  const [produced, setProduced] = useState('')
  const [rejected, setRejected] = useState('')
  const [machineId, setMachineId] = useState<number | null>(null)
  const [operatorId, setOperatorId] = useState<number | null>(null)

  const title = options
    ? `Elle bildir: Demet ${bundleNo} · ${options.sequenceNo} ${options.operationName}`
    : `Elle bildir: Demet ${bundleNo}`

  if (!options) {
    return (
      <Modal title={title} onClose={onClose}>
        {optionsError ? <ErrorAlert error={optionsError} /> : <p role="status">Yükleniyor…</p>}
      </Modal>
    )
  }

  const total = toQuantity(produced) + toQuantity(rejected)
  const machineCount = options.machines.length
  const selectedMachine = options.inProgress ? options.machineId : (machineId ?? (machineCount === 1 ? options.machines[0].id : null))
  const valid = Number.isInteger(total) && total > 0 && (options.inProgress || (selectedMachine !== null && operatorId !== null))

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await report({
      operationId,
      requestId,
      producedQuantity: toQuantity(produced),
      rejectedQuantity: toQuantity(rejected),
      operatorId: options.inProgress ? null : operatorId,
      machineId: options.inProgress ? null : selectedMachine,
    })
    if (!('error' in result)) {
      onClose()
    }
  }

  return (
    <Modal title={title} onClose={onClose}>
      <form className="form" onSubmit={(event) => void handleSubmit(event)} noValidate>
        {options.inProgress ? (
          <p>
            İş <strong>{options.machineCode}</strong> makinesinde <strong>{options.operatorName}</strong> tarafından başlatılmış ve
            işlemde görünüyor. Bildirilen adet bu operatör ve makine adına yazılır; makine serbest kalır.
          </p>
        ) : (
          <p>İşi yapan operatörü ve makineyi seçin; operasyon tablet ve kart olmadan ilerletilir.</p>
        )}
        <p className="muted">Kalan adet: <strong>{formatQuantity(options.remainingQuantity)}</strong>. Kalan biterse operasyon tamamlanır, bitmezse bekletilir.</p>

        {!options.inProgress && (
          <div className="form__dates">
            <label className="field">
              <span>Makine</span>
              <select
                value={selectedMachine ?? ''}
                onChange={(event) => setMachineId(event.target.value ? Number(event.target.value) : null)}
                aria-invalid={Boolean(fieldErrors(error, 'machineId')?.length)}
              >
                <option value="">Seçin…</option>
                {options.machines.map((machine) => (
                  <option key={machine.id} value={machine.id}>{machine.machineCode} — {machine.machineName}</option>
                ))}
              </select>
              {machineCount === 0 && <small className="field__error">Bu operasyonu yapabilen aktif makine yok.</small>}
              {fieldErrors(error, 'machineId')?.map((message) => (
                <small key={message} className="field__error">{message}</small>
              ))}
            </label>
            <LookupSelect
              resource="operators"
              label="Operatör"
              value={operatorId}
              onChange={setOperatorId}
              required
              errors={fieldErrors(error, 'operatorId')}
            />
          </div>
        )}

        <div className="form__dates">
          <TextField
            label="Sağlam adet"
            type="number"
            inputMode="numeric"
            min={0}
            max={options.remainingQuantity}
            value={produced}
            onChange={(event) => setProduced(event.target.value)}
            errors={fieldErrors(error, 'producedQuantity')}
            autoFocus
          />
          <TextField
            label="Hatalı adet"
            type="number"
            inputMode="numeric"
            min={0}
            max={options.remainingQuantity}
            value={rejected}
            onChange={(event) => setRejected(event.target.value)}
            errors={fieldErrors(error, 'rejectedQuantity')}
          />
        </div>

        <ErrorAlert error={error} fields={['machineId', 'operatorId', 'producedQuantity', 'rejectedQuantity']} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={!valid || isLoading}>
            {isLoading ? 'Kaydediliyor…' : 'Bildir'}
          </button>
        </footer>
      </form>
    </Modal>
  )
}
