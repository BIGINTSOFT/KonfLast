import { useState } from 'react'
import type { FormEvent } from 'react'
import { fieldErrors } from '../../api/envelope'
import { ErrorAlert, TextField } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { useCreateLocationsBulkMutation } from './stockApi'

const MAX_LOCATIONS = 2000

const pad = (value: number, digits: number) => String(value).padStart(digits, '0')

/** Formdaki sayı metni: boşsa null, geçersizse undefined. */
function parseNumber(text: string): number | null | undefined {
  const trimmed = text.trim()
  if (trimmed === '') {
    return null
  }

  return /^\d{1,4}$/.test(trimmed) ? Number(trimmed) : undefined
}

/**
 * Toplu adres açma: bir bölgede raf (ve isteğe bağlı göz) aralığı. Depoda zaten var olan adresler atlanır;
 * önizleme ilk ve son adresi, kaç adres açılacağını gösterir.
 */
export function BulkLocationDialog({ warehouseId, warehouseLabel, onClose }: { warehouseId: number; warehouseLabel: string; onClose: () => void }) {
  const [create, { isLoading, error, data }] = useCreateLocationsBulkMutation()
  const [zone, setZone] = useState('')
  const [rackFrom, setRackFrom] = useState('1')
  const [rackTo, setRackTo] = useState('')
  const [binFrom, setBinFrom] = useState('')
  const [binTo, setBinTo] = useState('')
  const [digits, setDigits] = useState(2)

  const racks = [parseNumber(rackFrom), parseNumber(rackTo)]
  const bins = [parseNumber(binFrom), parseNumber(binTo)]
  const hasBins = bins[0] !== null || bins[1] !== null
  const [rf, rt, bf, bt] = [...racks, ...bins]
  const valid =
    zone.trim() !== '' && typeof rf === 'number' && typeof rt === 'number' && rt >= rf
    && (!hasBins || (typeof bf === 'number' && typeof bt === 'number' && bt >= bf))
  const count = valid ? (rt - rf + 1) * (hasBins ? (bt as number) - (bf as number) + 1 : 1) : 0
  const code = (rack: number, bin: number | null) =>
    [zone.trim().toLocaleUpperCase('en-US'), pad(rack, digits), bin === null ? null : pad(bin, digits)].filter(Boolean).join('-')

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    if (!valid || count > MAX_LOCATIONS) {
      return
    }

    await create({
      warehouseId,
      zone: zone.trim(),
      rackFrom: rf,
      rackTo: rt,
      binFrom: hasBins ? (bf as number) : null,
      binTo: hasBins ? (bt as number) : null,
      digits,
    })
  }

  if (data) {
    return (
      <Modal title="Adresler açıldı" onClose={onClose}>
        <div className="form">
          <p>
            {data.created} adres açıldı
            {data.firstCode && <> (<code>{data.firstCode}</code> … <code>{data.lastCode}</code>)</>}
            {data.skipped > 0 && `; ${data.skipped} adres zaten vardı, atlandı`}.
          </p>
          <footer className="form__actions">
            <button type="button" className="button button--primary" onClick={onClose}>Tamam</button>
          </footer>
        </div>
      </Modal>
    )
  }

  return (
    <Modal title={`Toplu adres aç: ${warehouseLabel}`} onClose={onClose}>
      <form className="form" onSubmit={(event) => void handleSubmit(event)} noValidate>
        <div className="form__grid">
          <TextField
            label="Bölge"
            value={zone}
            maxLength={10}
            onChange={(event) => setZone(event.target.value)}
            errors={fieldErrors(error, 'zone')}
            hint="Harf ve rakam; örn. A."
            autoComplete="off"
            required
          />
          <label className="field">
            <span>Basamak sayısı</span>
            <select value={digits} onChange={(event) => setDigits(Number(event.target.value))}>
              {[1, 2, 3, 4].map((value) => (
                <option key={value} value={value}>{value} ({pad(1, value)})</option>
              ))}
            </select>
          </label>
          <TextField label="İlk raf" inputMode="numeric" value={rackFrom} onChange={(event) => setRackFrom(event.target.value)} errors={fieldErrors(error, 'rackFrom')} required />
          <TextField label="Son raf" inputMode="numeric" value={rackTo} onChange={(event) => setRackTo(event.target.value)} errors={fieldErrors(error, 'rackTo')} required />
          <TextField label="İlk göz (isteğe bağlı)" inputMode="numeric" value={binFrom} onChange={(event) => setBinFrom(event.target.value)} errors={fieldErrors(error, 'binFrom')} />
          <TextField label="Son göz (isteğe bağlı)" inputMode="numeric" value={binTo} onChange={(event) => setBinTo(event.target.value)} errors={fieldErrors(error, 'binTo')} />
        </div>

        {valid ? (
          <p className={count > MAX_LOCATIONS ? 'alert alert--error' : 'muted'}>
            {count} adres: <code>{code(rf, hasBins ? (bf as number) : null)}</code> … <code>{code(rt, hasBins ? (bt as number) : null)}</code>
            {count > MAX_LOCATIONS && ` — tek seferde en fazla ${MAX_LOCATIONS} adres açılabilir.`}
          </p>
        ) : (
          <p className="muted">Bölgeyi ve raf aralığını girin; göz aralığı boşsa adresler raf düzeyinde açılır.</p>
        )}

        <ErrorAlert error={error} fields={['zone', 'rackFrom', 'rackTo', 'binFrom', 'binTo']} />

        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading || !valid || count > MAX_LOCATIONS}>
            {isLoading ? 'Açılıyor…' : 'Adresleri aç'}
          </button>
        </footer>
      </form>
    </Modal>
  )
}
