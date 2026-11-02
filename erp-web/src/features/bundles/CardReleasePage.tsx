import { useState } from 'react'
import { Link } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { RecordLink } from '../../components/RecordLink'
import { formatDateTime, formatQuantity } from '../../utils/format'
import { useLazyGetBundleCardQuery, useReleaseBundleCardMutation } from '../rfid/rfidApi'
import type { BundleCard, BundleCardRelease } from '../rfid/rfidApi'
import { ScanInput } from '../terminal/ScanInput'
import { BundleStatusBadge } from './BundleBadges'
import { useGetBundleQuery } from './bundlesApi'

interface ReleasedCard extends BundleCardRelease {
  releasedAt: string
}

/**
 * Kart boşaltma: yarım kalmış, okutulmamış ya da tamamlanmamış demetlerden toplanan kartlar okutulur ve demetle
 * bağı kaldırılır. Masaüstü okuyucu klavye gibi davranır; okutmadan sonra kutu odakta kalır, sıradaki kart
 * okutulabilir. Demet açık kalır; işine devam edilecekse sonradan yeni bir kart atanır.
 */
export function CardReleasePage() {
  const [lookup, lookupState] = useLazyGetBundleCardQuery()
  const [release, releaseState] = useReleaseBundleCardMutation()
  const [card, setCard] = useState<BundleCard | null>(null)
  const [instant, setInstant] = useState(false)
  const [released, setReleased] = useState<ReleasedCard[]>([])
  const [scanCount, setScanCount] = useState(0)

  const runRelease = async (uid: string) => {
    const result = await release(uid)
    if ('data' in result && result.data) {
      const entry = { ...result.data, releasedAt: new Date().toISOString() }
      setReleased((items) => [entry, ...items])
      setCard(null)
    }
    setScanCount((count) => count + 1)
  }

  const handleScan = async (uid: string) => {
    setCard(null)
    releaseState.reset()
    const result = await lookup(uid)
    setScanCount((count) => count + 1)
    if (!result.data) {
      return
    }

    if (instant && result.data.bundleId !== null) {
      await runRelease(result.data.uid)
    } else {
      setCard(result.data)
    }
  }

  const last = released[0]
  const busy = lookupState.isFetching || releaseState.isLoading

  return (
    <section className="page">
      <header className="page__header">
        <div>
          <h1>Kart Boşaltma</h1>
          <p className="muted">
            Yarım kalmış ya da tamamlanmamış demetlerden toplanan kartı okutun; kartın demetle bağı kaldırılır ve
            kart yeni bir demete atanabilir.
          </p>
        </div>
      </header>

      <div className="form">
        <ScanInput
          label="Demet kartını okutun"
          placeholder="Kartı okutun ya da kart numarasını yazın"
          onScan={(uid) => void handleScan(uid)}
          busy={busy}
          focusToken={scanCount}
        />
        <label className="checkbox">
          <input type="checkbox" checked={instant} onChange={(event) => setInstant(event.target.checked)} />
          Okutulan kartı onay sormadan boşalt (toplu boşaltma)
        </label>
      </div>

      <ErrorAlert error={lookupState.error} />
      <ErrorAlert error={releaseState.error} />

      {last && !card && !lookupState.error && !releaseState.error && (
        <div className="alert alert--ok" role="status">
          <code>{last.uid}</code> kartı boşaltıldı; Demet {last.bundleNo} ile bağı kaldırıldı.
        </div>
      )}

      {card && card.bundleId === null && (
        <div className="alert alert--info" role="status">
          <code>{card.uid}</code>{card.label ? ` (${card.label})` : ''} kartı zaten boş; bir demete bağlı değil.
        </div>
      )}

      {card && card.bundleId !== null && (
        <CardToRelease
          card={card}
          bundleId={card.bundleId}
          busy={busy}
          onRelease={() => void runRelease(card.uid)}
          onCancel={() => {
            setCard(null)
            setScanCount((count) => count + 1)
          }}
        />
      )}

      <h2 className="section-title">Bu oturumda boşaltılan kartlar</h2>
      {released.length === 0 ? (
        <p className="muted">Henüz kart boşaltılmadı.</p>
      ) : (
        <div className="table-wrap">
          <table className="table table--compact">
            <thead>
              <tr>
                <th>Zaman</th>
                <th>Kart</th>
                <th>Demet</th>
              </tr>
            </thead>
            <tbody>
              {released.map((item) => (
                <tr key={`${item.uid}-${item.releasedAt}`}>
                  <td>{formatDateTime(item.releasedAt)}</td>
                  <td><code>{item.uid}</code></td>
                  <td><RecordLink to={`/bundles/${item.bundleId}`}>{item.bundleNo}</RecordLink></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  )
}

interface CardToReleaseProps {
  card: BundleCard
  bundleId: number
  busy: boolean
  onRelease: () => void
  onCancel: () => void
}

/** Okutulan kartın bağlı olduğu demet: boşaltmadan önce ne durumda olduğu görülür. */
function CardToRelease({ card, bundleId, busy, onRelease, onCancel }: CardToReleaseProps) {
  const { data: bundle, error } = useGetBundleQuery(bundleId)

  if (!bundle) {
    return error ? <ErrorAlert error={error} /> : <p role="status">Yükleniyor…</p>
  }

  const completed = bundle.operations.filter((operation) => operation.status === 'COMPLETED').length
  const running = bundle.operations.filter((operation) => operation.status === 'IN_PROGRESS')
  const started = bundle.status !== 'CREATED' && bundle.status !== 'WAITING'

  return (
    <article className="card">
      <h2>
        Kart <code>{card.uid}</code>{card.label ? ` (${card.label})` : ''} → Demet{' '}
        <Link to={`/bundles/${bundle.id}`}>{bundle.bundleNo}</Link> <BundleStatusBadge status={bundle.status} />
      </h2>
      <dl className="status-list">
        <div>
          <dt>Sipariş</dt>
          <dd><RecordLink to={`/production-orders/${bundle.productionOrderId}`}>{bundle.orderNo}</RecordLink></dd>
        </div>
        <div><dt>Model</dt><dd>{bundle.modelCode}</dd></div>
        <div><dt>Renk / beden</dt><dd>{bundle.colorName} / {bundle.sizeName}</dd></div>
        <div><dt>Adet</dt><dd>{formatQuantity(bundle.quantity)}</dd></div>
        <div><dt>İlerleme</dt><dd>{completed} / {bundle.operations.length} operasyon tamam</dd></div>
        <div>
          <dt>Şu an nerede</dt>
          <dd>{bundle.currentOperations.length > 0 ? bundle.currentOperations.join(', ') : '—'}</dd>
        </div>
      </dl>

      {running.length > 0 && (
        <div className="alert alert--info" role="note">
          {running.map((operation) => `${operation.sequenceNo} – ${operation.operationName}${operation.machineCode ? ` (${operation.machineCode})` : ''}`).join(', ')}{' '}
          işlemde görünüyor. Kart boşaltılsa da süren iş tabletten bitirilebilir.
        </div>
      )}

      <p className="muted">
        {started
          ? 'Demet açık kalır ancak kartsız demet tablette okutulamaz; işine devam edilecekse demete yeni bir kart atayın.'
          : 'Demet henüz üretime girmemiş; kart boşaltılınca demet kartsız haline döner.'}
      </p>

      <footer className="form__actions">
        <button type="button" className="button" onClick={onCancel}>Vazgeç</button>
        <button type="button" className="button button--danger" disabled={busy} onClick={onRelease}>
          {busy ? 'Boşaltılıyor…' : 'Kartı boşalt'}
        </button>
      </footer>
    </article>
  )
}
