import { X } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { todayIsoDate } from '../../utils/format'
import { LookupSelect } from '../masterData/LookupSelect'
import { useSaveFabricReceiptMutation } from './boyahaneReceiptsApi'
import type { FabricReceipt } from './boyahaneReceiptsApi'

interface RollDraft {
  key: number
  id: number | null
  rollNo: string | null
  locked: boolean
  customerRollNo: string
  meters: string
  kilograms: string
}

interface LotDraft {
  key: number
  id: number | null
  fabricId: number | null
  customerFabricCode: string
  declaredRollCount: string
  declaredMeters: string
  declaredKilograms: string
  rolls: RollDraft[]
}

const numberOrNull = (value: string) => (value.trim() === '' ? null : Number(value))
const text = (value: number | null) => (value === null ? '' : String(value))

const emptyRoll = (key: number): RollDraft => ({ key, id: null, rollNo: null, locked: false, customerRollNo: '', meters: '', kilograms: '' })

/** Düzenlemede yalnızca ölçülmüş değerler forma gelir; hesaplanan karşılık kayıtta yeniden hesaplanır. */
const lotsOf = (receipt: FabricReceipt): LotDraft[] => receipt.lots.map((lot) => ({
  key: lot.id,
  id: lot.id,
  fabricId: lot.fabricId,
  customerFabricCode: lot.customerFabricCode ?? '',
  declaredRollCount: text(lot.declaredRollCount),
  declaredMeters: text(lot.declaredMeters),
  declaredKilograms: text(lot.declaredKilograms),
  rolls: lot.rolls.map((roll) => ({
    key: roll.id,
    id: roll.id,
    rollNo: roll.rollNo,
    locked: roll.isLocked,
    customerRollNo: roll.customerRollNo ?? '',
    // Kilitli top değişmez; değerleri gösterim içindir, sunucu dikkate almaz.
    meters: roll.metersMeasured || roll.isLocked ? String(roll.meters) : '',
    kilograms: roll.kilogramsMeasured || roll.isLocked ? String(roll.kilograms) : '',
  })),
}))

/**
 * Kumaş kabul formu. Bir belgede birden çok kumaş; her kumaşın topları ayrı girilir. Düzenlemede var olan toplar
 * numaralarını korur, basılmış etiketler geçerli kalır; kontrol edilmiş ya da siparişe bağlanmış top değiştirilemez.
 */
export function FabricReceiptFormDialog({ receipt, onClose }: { receipt?: FabricReceipt; onClose: () => void }) {
  const navigate = useNavigate()
  const [save, { isLoading, error }] = useSaveFabricReceiptMutation()
  const [receiptDate, setReceiptDate] = useState(receipt?.receiptDate ?? todayIsoDate())
  const [customerId, setCustomerId] = useState<number | null>(receipt?.customerId ?? null)
  const [despatchNo, setDespatchNo] = useState(receipt?.despatchNo ?? '')
  const [notes, setNotes] = useState(receipt?.notes ?? '')
  const [locationId, setLocationId] = useState<number | null>(null)
  const [isOpening, setIsOpening] = useState(receipt?.isOpening ?? false)
  // Yeni satır anahtarları negatiftir; var olan kayıtların kimlikleriyle çakışmaz.
  const [nextKey, setNextKey] = useState(-2)
  const [lots, setLots] = useState<LotDraft[]>(receipt ? lotsOf(receipt) : [
    { key: 0, id: null, fabricId: null, customerFabricCode: '', declaredRollCount: '', declaredMeters: '', declaredKilograms: '', rolls: [emptyRoll(-1)] },
  ])
  const hasLockedRoll = lots.some((lot) => lot.rolls.some((roll) => roll.locked))

  const takeKeys = (count: number) => {
    const first = nextKey
    setNextKey(first - count)
    return first
  }

  const updateLot = (key: number, change: Partial<LotDraft>) =>
    setLots(lots.map((lot) => (lot.key === key ? { ...lot, ...change } : lot)))

  const addLot = () => {
    const first = takeKeys(2)
    setLots([...lots, {
      key: first, id: null, fabricId: null, customerFabricCode: '', declaredRollCount: '', declaredMeters: '', declaredKilograms: '', rolls: [emptyRoll(first - 1)],
    }])
  }

  const addRolls = (lot: LotDraft, count: number) => {
    const first = takeKeys(count)
    updateLot(lot.key, { rolls: [...lot.rolls, ...Array.from({ length: count }, (_, index) => emptyRoll(first - index))] })
  }

  const updateRoll = (lot: LotDraft, key: number, change: Partial<RollDraft>) =>
    updateLot(lot.key, { rolls: lot.rolls.map((roll) => (roll.key === key ? { ...roll, ...change } : roll)) })

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await save({
      id: receipt?.id,
      body: {
        receiptDate,
        customerId,
        despatchNo: despatchNo.trim() || null,
        notes: notes.trim() || null,
        locationId,
        isOpening,
        lots: lots.map((lot) => ({
          id: lot.id,
          fabricId: lot.fabricId ?? 0,
          customerFabricCode: lot.customerFabricCode.trim() || null,
          declaredRollCount: numberOrNull(lot.declaredRollCount),
          declaredMeters: numberOrNull(lot.declaredMeters),
          declaredKilograms: numberOrNull(lot.declaredKilograms),
          rolls: lot.rolls.map((roll) => ({
            id: roll.id,
            customerRollNo: roll.customerRollNo.trim() || null,
            meters: numberOrNull(roll.meters),
            kilograms: numberOrNull(roll.kilograms),
          })),
        })),
      },
    })
    if ('data' in result && result.data) {
      if (receipt) {
        onClose()
      } else {
        navigate(`/boyahane/receipts/${result.data.id}`)
      }
    }
  }

  return (
    <Modal title={receipt ? `Kabul düzenle: ${receipt.receiptNo}` : 'Yeni kumaş kabul'} onClose={onClose} variant="drawer">
      <form className="form" onSubmit={handleSubmit}>
        <div className="form__pair">
          <label className="field">
            <span>Kabul tarihi</span>
            <input type="date" required value={receiptDate} onChange={(event) => setReceiptDate(event.target.value)} />
          </label>
          <label className="field">
            <span>İrsaliye no</span>
            <input value={despatchNo} maxLength={50} onChange={(event) => setDespatchNo(event.target.value)} />
          </label>
        </div>
        <LookupSelect resource="boyahane-warehouse-locations" label="Depo adresi (yeni toplar için)" value={locationId} onChange={setLocationId} placeholder="Boşsa ham deponun genel adresi" />
        <label className="checkbox"><input type="checkbox" checked={isOpening} disabled={hasLockedRoll} onChange={(event) => setIsOpening(event.target.checked)} />Açılış stoğu (sisteme geçiş; toplar ham kontrole girmez)</label>
        <LookupSelect
          resource="boyahane-customers"
          label="Malın sahibi (müşteri)"
          value={customerId}
          disabled={hasLockedRoll}
          onChange={setCustomerId}
          placeholder="Boş bırakılırsa işletme malı"
        />
        {hasLockedRoll && (
          <p className="muted form__note">
            Ham kontrolü yapılmış ya da siparişe bağlanmış top olduğu için malın sahibi ve açılış stoğu değiştirilemez.
          </p>
        )}
        <label className="field">
          <span>Not</span>
          <textarea rows={2} value={notes} maxLength={1000} onChange={(event) => setNotes(event.target.value)} />
        </label>

        {lots.map((lot, index) => {
          const lotLocked = lot.rolls.some((roll) => roll.locked)
          return (
            <fieldset key={lot.key} className="form__section">
              <legend>{index + 1}. kumaş</legend>
              <div className="form__pair">
                <LookupSelect
                  resource="boyahane-fabrics"
                  label="Kumaş kartı"
                  required
                  disabled={lotLocked}
                  idSuffix={String(lot.key)}
                  value={lot.fabricId}
                  onChange={(value) => updateLot(lot.key, { fabricId: value })}
                />
                <label className="field">
                  <span>Müşterinin kumaş kodu</span>
                  <input value={lot.customerFabricCode} maxLength={100} onChange={(event) => updateLot(lot.key, { customerFabricCode: event.target.value })} />
                </label>
              </div>
              <div className="form__pair">
                <label className="field">
                  <span>İrsaliyedeki top</span>
                  <input type="number" min="0" step="1" value={lot.declaredRollCount} onChange={(event) => updateLot(lot.key, { declaredRollCount: event.target.value })} />
                </label>
                <label className="field">
                  <span>İrsaliyedeki metre</span>
                  <input type="number" min="0" step="0.01" value={lot.declaredMeters} onChange={(event) => updateLot(lot.key, { declaredMeters: event.target.value })} />
                </label>
                <label className="field">
                  <span>İrsaliyedeki kilo</span>
                  <input type="number" min="0" step="0.01" value={lot.declaredKilograms} onChange={(event) => updateLot(lot.key, { declaredKilograms: event.target.value })} />
                </label>
              </div>
              <p className="muted form__note">Metre ya da kilodan birini girmeniz yeterlidir; diğeri kumaş kartından hesaplanır.</p>
              <div className="table-wrap recipe-table">
                <table className="table table--compact">
                  <thead>
                    <tr>
                      <th>Top no</th>
                      <th>Müşteri top no</th>
                      <th>Metre</th>
                      <th>Kilo</th>
                      <th aria-label="Satırı kaldır" />
                    </tr>
                  </thead>
                  <tbody>
                    {lot.rolls.map((roll, rollIndex) => (
                      <tr key={roll.key}>
                        <td>{roll.rollNo ? <code>{roll.rollNo}</code> : <span className="muted">Yeni {rollIndex + 1}</span>}</td>
                        <td>
                          <input aria-label="Müşteri top no" value={roll.customerRollNo} maxLength={50} disabled={roll.locked}
                            onChange={(event) => updateRoll(lot, roll.key, { customerRollNo: event.target.value })} />
                        </td>
                        <td>
                          <input aria-label="Metre" type="number" min="0" step="0.01" value={roll.meters} disabled={roll.locked}
                            placeholder={roll.locked ? 'Kilitli' : undefined}
                            onChange={(event) => updateRoll(lot, roll.key, { meters: event.target.value })} />
                        </td>
                        <td>
                          <input aria-label="Kilo" type="number" min="0" step="0.01" value={roll.kilograms} disabled={roll.locked}
                            placeholder={roll.locked ? 'Kilitli' : undefined}
                            onChange={(event) => updateRoll(lot, roll.key, { kilograms: event.target.value })} />
                        </td>
                        <td className="recipe-table__remove-cell">
                          <button type="button" className="icon-button" aria-label="Satırı kaldır" title="Satırı kaldır" disabled={lot.rolls.length === 1 || roll.locked}
                            onClick={() => updateLot(lot.key, { rolls: lot.rolls.filter((item) => item.key !== roll.key) })}>
                            <X aria-hidden="true" />
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              <div className="page__actions">
                <button type="button" className="button" onClick={() => addRolls(lot, 1)}>Top ekle</button>
                <button type="button" className="button" onClick={() => addRolls(lot, 10)}>10 top ekle</button>
                {lots.length > 1 && !lotLocked && (
                  <button type="button" className="button button--danger-ghost" onClick={() => setLots(lots.filter((item) => item.key !== lot.key))}>
                    Kumaşı kaldır
                  </button>
                )}
              </div>
            </fieldset>
          )
        })}
        <div>
          <button type="button" className="button" onClick={addLot}>Kumaş ekle</button>
        </div>

        <ErrorAlert error={error} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading}>Kaydet</button>
        </footer>
      </form>
    </Modal>
  )
}
