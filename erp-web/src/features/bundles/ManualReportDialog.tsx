import { useState } from 'react'
import type { FormEvent } from 'react'
import { fieldErrors } from '../../api/envelope'
import { ErrorAlert, TextField } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { formatQuantity } from '../../utils/format'
import { LookupSelect } from '../masterData/LookupSelect'
import { useGetManualReportOptionsQuery, useManualReportMutation } from './bundlesApi'
import type { ManualReportAttribution } from './bundlesApi'

interface ManualReportDialogProps {
  operationId: number
  bundleNo: string
  onClose: () => void
}

const toQuantity = (text: string) => (text.trim() === '' ? 0 : Number(text))

const attributions: { value: ManualReportAttribution; label: string; hint: string }[] = [
  { value: 'OPERATOR', label: 'Operatör adına', hint: 'Adet operatörün performansına ve primine sayılır.' },
  {
    value: 'OPERATOR_NOT_COUNTED',
    label: 'Operatör adına, performansa sayılmaz',
    hint: 'Operatörün adı kayıtta görünür; verim ve prim hesabına girmez.',
  },
  {
    value: 'NO_OPERATOR',
    label: 'Operatörsüz (ofis kapatması)',
    hint: 'Kimseye yazılmaz; yalnızca demet ve sipariş ilerler.',
  },
]

/**
 * Ofisten elle üretim bildirimi: tablet arızalandığında ya da iş askıda kaldığında operasyon tablet ve kart
 * olmadan ilerletilir. İşlemdeki işte makine ve operatör o işinkidir; başlamamış / bekleyen işte seçilir. Adedin
 * operatöre yazılıp performansına / primine sayılacağı ya da operatörsüz (ofis kapatması) girileceği seçilir.
 */
export function ManualReportDialog({ operationId, bundleNo, onClose }: ManualReportDialogProps) {
  const { data: options, error: optionsError } = useGetManualReportOptionsQuery(operationId)
  const [report, { isLoading, error }] = useManualReportMutation()
  // Çift tıklama / ağ tekrarı aynı kimlikle gelir ve ikinci kez işlenmez.
  const [requestId] = useState(() => crypto.randomUUID())
  // Boşsa kalan adedin tamamı sağlam sayılır (seçenekler gelince doldurulur).
  const [produced, setProduced] = useState<string | null>(null)
  const [rejected, setRejected] = useState('0')
  const [machineId, setMachineId] = useState<number | null>(null)
  const [operatorId, setOperatorId] = useState<number | null>(null)
  const [attribution, setAttribution] = useState<ManualReportAttribution>('OPERATOR')

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

  const remaining = options.remainingQuantity
  const producedText = produced ?? String(remaining)
  const producedCount = toQuantity(producedText)
  const rejectedCount = toQuantity(rejected)
  const total = producedCount + rejectedCount
  const left = remaining - total
  // Sağlam ve hatalının toplamı kalanı geçemez: biri artınca taşan kısım diğerinden düşülür (tablettekiyle aynı).
  const clamp = (value: number) => (Number.isFinite(value) ? Math.max(0, Math.min(remaining, Math.trunc(value))) : 0)
  const changeProduced = (text: string) => {
    setProduced(text)
    const next = clamp(toQuantity(text))
    if (next + rejectedCount > remaining) {
      setRejected(String(remaining - next))
    }
  }
  const changeRejected = (text: string) => {
    setRejected(text)
    const next = clamp(toQuantity(text))
    if (producedCount + next > remaining) {
      setProduced(String(remaining - next))
    }
  }
  const machineCount = options.machines.length
  const selectedMachine = options.inProgress ? options.machineId : (machineId ?? (machineCount === 1 ? options.machines[0].id : null))
  const withOperator = attribution !== 'NO_OPERATOR'
  const valid =
    Number.isInteger(total) && total > 0 && left >= 0
    && (options.inProgress || (selectedMachine !== null && (!withOperator || operatorId !== null)))

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await report({
      operationId,
      requestId,
      producedQuantity: producedCount,
      rejectedQuantity: rejectedCount,
      operatorId: options.inProgress || !withOperator ? null : operatorId,
      machineId: options.inProgress ? null : selectedMachine,
      attribution,
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
            işlemde görünüyor. Bildirilen adet bu makineye yazılır; operatöre yazılıp yazılmayacağını aşağıda seçin. Makine
            serbest kalır.
          </p>
        ) : (
          <p>İşin yapıldığı makineyi ve adedin kime yazılacağını seçin; operasyon tablet ve kart olmadan ilerletilir.</p>
        )}

        <fieldset className="attribution-options">
          <legend>Kimin adına</legend>
          {attributions.map((option) => (
            <label key={option.value} className={attribution === option.value ? 'attribution-option attribution-option--active' : 'attribution-option'}>
              <input
                type="radio"
                name="attribution"
                value={option.value}
                checked={attribution === option.value}
                onChange={() => setAttribution(option.value)}
              />
              <span>
                <strong>
                  {option.label}
                  {options.inProgress && option.value !== 'NO_OPERATOR' && options.operatorName ? ` · ${options.operatorName}` : ''}
                </strong>
                <small>{option.hint}</small>
              </span>
            </label>
          ))}
        </fieldset>
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
            {withOperator && (
              <LookupSelect
                resource="operators"
                label="Operatör"
                value={operatorId}
                onChange={setOperatorId}
                required
                errors={fieldErrors(error, 'operatorId')}
              />
            )}
          </div>
        )}

        <div className="form__dates">
          <TextField
            label="Sağlam adet"
            type="number"
            inputMode="numeric"
            min={0}
            max={options.remainingQuantity}
            value={producedText}
            onChange={(event) => changeProduced(event.target.value)}
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
            onChange={(event) => changeRejected(event.target.value)}
            errors={fieldErrors(error, 'rejectedQuantity')}
          />
        </div>

        <ErrorAlert error={error} fields={['machineId', 'operatorId', 'producedQuantity', 'rejectedQuantity']} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={!valid || isLoading}>
            {isLoading ? 'Kaydediliyor…' : left === 0 ? 'Bildir ve tamamla' : `Bildir (${formatQuantity(left)} adet kalır)`}
          </button>
        </footer>
      </form>
    </Modal>
  )
}
