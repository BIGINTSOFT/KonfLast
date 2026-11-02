import { Check } from 'lucide-react'
import { useState } from 'react'
import { ErrorAlert } from '../../components/FormControls'
import { isRequestError } from '../../api/envelope'
import { formatDate, formatQuantity } from '../../utils/format'
import {
  useGetLoadableShipmentsQuery,
  useGetShipmentLoadingQuery,
  useLazyFindShipmentLoadingQuery,
  useLoadShipmentCartonMutation,
} from '../shipping/shippingApi'
import { shipmentStatusPresentation } from '../shipping/shippingPresentation'
import { ScanInput } from './ScanInput'

/**
 * Kısa sesli uyarı: okutma kabul edildiyse tiz tek ses, reddedildiyse pes çift ses. Ses kapalıysa ya da tarayıcı
 * desteklemiyorsa sessiz geçilir; ekrandaki metin her zaman gösterilir.
 */
function beep(ok: boolean) {
  try {
    const context = new AudioContext()
    const tones = ok ? [0] : [0, 0.22]
    for (const start of tones) {
      const oscillator = context.createOscillator()
      const gain = context.createGain()
      oscillator.frequency.value = ok ? 1200 : 300
      oscillator.type = ok ? 'sine' : 'square'
      gain.gain.value = 0.15
      oscillator.connect(gain)
      gain.connect(context.destination)
      oscillator.start(context.currentTime + start)
      oscillator.stop(context.currentTime + start + (ok ? 0.12 : 0.18))
    }
    window.setTimeout(() => void context.close(), 800)
  } catch {
    // Ses çıkarılamıyorsa yalnızca metin uyarısı kalır.
  }
}

type Feedback = { ok: boolean; text: string }

const messageOf = (error: unknown) => (isRequestError(error) ? error.message : null)

/**
 * Sevkiyat yükleme: sevkiyat seçilir (listeden ya da sevkiyat no / koli barkodu okutularak), sonra araca konan her
 * kolinin etiketi okutulur. Her okutma kolinin yüklendiğini kaydeder; aynı koli ikinci kez okutulursa uyarı verilir,
 * başka sevkiyatın kolisi reddedilir. Ekranda yüklenen / toplam ve eksik koliler görünür. Sevk etmeyi engellemez.
 */
export function TerminalLoading() {
  const [shipmentId, setShipmentId] = useState<number | null>(null)
  const [search, setSearch] = useState('')
  const [feedback, setFeedback] = useState<Feedback | null>(null)
  const [step, setStep] = useState(0)
  const [showLoaded, setShowLoaded] = useState(false)

  const loadable = useGetLoadableShipmentsQuery({ search: search.trim() || undefined }, { skip: shipmentId !== null })
  const loading = useGetShipmentLoadingQuery(shipmentId ?? 0, { skip: shipmentId === null })
  const [find, findState] = useLazyFindShipmentLoadingQuery()
  const [scan, scanState] = useLoadShipmentCartonMutation()

  const report = (next: Feedback) => {
    setFeedback(next)
    beep(next.ok)
    setStep((current) => current + 1)
  }

  const handleScan = async (code: string) => {
    if (shipmentId === null) {
      // Sevkiyat numarası ya da sevkiyatın herhangi bir koli etiketi okutularak sevkiyat seçilir.
      try {
        const found = await find(code, false).unwrap()
        setShipmentId(found.shipmentId)
        setFeedback(null)
        setStep((current) => current + 1)
      } catch (error) {
        report({ ok: false, text: messageOf(error) ?? `${code} ile eşleşen sevkiyat bulunamadı.` })
      }
      return
    }

    try {
      const result = await scan({ id: shipmentId, code }).unwrap()
      report(result.alreadyLoaded
        ? { ok: false, text: `Koli ${result.cartonNo} zaten okutulmuştu (${result.code}).` }
        : { ok: true, text: `Koli ${result.cartonNo} yüklendi · ${result.shipment.loadedCount} / ${result.shipment.cartonCount}` })
    } catch (error) {
      report({ ok: false, text: messageOf(error) ?? 'Okutma kaydedilemedi.' })
    }
  }

  const data = loading.data
  const missing = data?.cartons.filter((carton) => carton.loadedAt === null) ?? []
  const shown = showLoaded ? (data?.cartons ?? []) : missing

  return (
    <>
      <ScanInput
        label={shipmentId === null ? 'Sevkiyat numarasını ya da bir koli etiketini okutun' : 'Araca konan kolinin etiketini okutun'}
        onScan={(code) => void handleScan(code)}
        busy={findState.isFetching || scanState.isLoading}
        focusToken={step}
      />

      {feedback && (
        <p className={feedback.ok ? 'alert alert--ok' : 'alert alert--error'} role={feedback.ok ? 'status' : 'alert'}>
          {feedback.text}
        </p>
      )}

      {shipmentId === null && (
        <>
          <label className="field">
            <span>Ya da listeden seçin</span>
            <input type="search" placeholder="Sevkiyat no, irsaliye no ya da müşteri" value={search} onChange={(event) => setSearch(event.target.value)} />
          </label>
          {loadable.isLoading && <p role="status">Yükleniyor…</p>}
          <ErrorAlert error={loadable.error} />
          {loadable.data && loadable.data.length === 0 && <p className="muted">Kolisi olan sevkiyat yok.</p>}
          <ul className="terminal-list">
            {loadable.data?.map((shipment) => (
              <li key={shipment.id}>
                <button
                  type="button"
                  className="terminal-list__row terminal-list__row--pick"
                  onClick={() => {
                    setShipmentId(shipment.id)
                    setFeedback(null)
                    setStep((current) => current + 1)
                  }}
                >
                  <span className="terminal-list__main">
                    <code>{shipment.shipmentNo}</code> {shipment.customerName}
                    <br />
                    <span className="muted">{formatDate(shipment.shipDate)} · {shipmentStatusPresentation[shipment.status].label}</span>
                  </span>
                  <strong>{shipment.loadedCount} / {shipment.cartonCount}</strong>
                </button>
              </li>
            ))}
          </ul>
        </>
      )}

      {shipmentId !== null && (
        <>
          {loading.isLoading && <p role="status">Yükleniyor…</p>}
          <ErrorAlert error={loading.error} />
          {data && (
            <>
              <dl className="terminal-facts">
                <dt>Sevkiyat</dt>
                <dd><code>{data.shipmentNo}</code> · {shipmentStatusPresentation[data.status].label}</dd>
                <dt>Müşteri</dt>
                <dd>{data.customerName}</dd>
                <dt>Yüklenen</dt>
                <dd><strong>{data.loadedCount} / {data.cartonCount}</strong> koli{missing.length > 0 ? ` · ${missing.length} eksik` : ''}</dd>
              </dl>

              {data.cartonCount > 0 && missing.length === 0 && (
                <p className="alert alert--ok" role="status">Bütün koliler yüklendi.</p>
              )}
              {data.cartonCount === 0 && <p className="alert alert--info">Bu sevkiyatın çeki listesi (kolisi) yok.</p>}

              <label className="checkbox">
                <input type="checkbox" checked={showLoaded} onChange={(event) => setShowLoaded(event.target.checked)} />
                Yüklenenleri de göster
              </label>

              <ul className="terminal-list">
                {shown.map((carton) => {
                  const done = carton.loadedAt !== null
                  return (
                    <li key={carton.id}>
                      <div className={done ? 'terminal-list__row terminal-list__row--done' : 'terminal-list__row'}>
                        <span className="terminal-list__main">
                          Koli <strong>{carton.cartonNo}</strong> <code>{carton.code}</code>
                          <br />
                          <span className="muted">{carton.summary}</span>
                        </span>
                        <strong>
                          {done && <Check aria-hidden="true" />}
                          {formatQuantity(carton.quantity)}
                        </strong>
                      </div>
                    </li>
                  )
                })}
              </ul>
            </>
          )}

          <p>
            <button
              type="button"
              className="button button--ghost"
              onClick={() => {
                setShipmentId(null)
                setFeedback(null)
                setStep((current) => current + 1)
              }}
            >
              Başka sevkiyat
            </button>
          </p>
        </>
      )}
    </>
  )
}
