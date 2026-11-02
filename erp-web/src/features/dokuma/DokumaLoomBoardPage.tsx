import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDate, formatDateTime, formatQuantity, todayIsoDate } from '../../utils/format'
import { useEndDokumaLoomStopMutation } from './dokumaFloorApi'
import { DokumaStartStopDialog } from './DokumaFloorPages'
import { dokumaStopCategoryLabels, loomTypeLabels, sheddingTypeLabels } from './dokumaPresentation'
import { useGetDokumaBeamsQuery } from './dokumaWarpApi'
import { useGetDokumaLoomBoardQuery, useMountDokumaBeamMutation } from './dokumaWeavingApi'
import type { DokumaLoomBoardItem } from './dokumaWeavingApi'

/** Boş tezgâha tezgâha hazır levent bağlama. */
function MountDialog({ loom, onClose }: { loom: DokumaLoomBoardItem; onClose: () => void }) {
  const { data, error, isLoading } = useGetDokumaBeamsQuery({ 'filter[status]': 'READY', pageSize: 100 })
  const [mount, mountState] = useMountDokumaBeamMutation()
  const [beamId, setBeamId] = useState<number | null>(null)
  const [date, setDate] = useState(todayIsoDate())

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    if (beamId === null) {
      return
    }
    const result = await mount({ id: beamId, loomId: loom.loomId, date })
    if (!('error' in result)) {
      onClose()
    }
  }

  return (
    <Modal title={`Levent bağla: ${loom.loomCode}`} onClose={onClose} variant="drawer">
      <form className="form" onSubmit={handleSubmit}>
        <p className="muted">
          Tezgâh: {sheddingTypeLabels[loom.sheddingType]}
          {loom.maxReedWidthCm !== null && `, en geniş tarak eni ${formatQuantity(loom.maxReedWidthCm)} cm`}. Ağızlık sistemi ya da tarak eni uymayan levent bağlanmaz.
        </p>
        {isLoading && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={error} />
        {data && (
          <div className="table-wrap">
            <table className="table table--compact">
              <thead>
                <tr>
                  <th aria-label="Seç" />
                  <th>Levent no</th>
                  <th>Sipariş</th>
                  <th>Artikel</th>
                  <th className="table__number">Metre</th>
                </tr>
              </thead>
              <tbody>
                {data.items.map((beam) => (
                  <tr key={beam.id}>
                    <td><input type="radio" name="beam" aria-label={`${beam.beamNo} seç`} checked={beamId === beam.id} onChange={() => setBeamId(beam.id)} /></td>
                    <td><code>{beam.beamNo}</code></td>
                    <td>{beam.orderNo}{beam.customerName && <div className="muted">{beam.customerName}</div>}</td>
                    <td>{beam.articleCode} — {beam.articleName}</td>
                    <td className="table__number">{formatQuantity(beam.meters)}</td>
                  </tr>
                ))}
                {data.items.length === 0 && <tr><td colSpan={5} className="table__empty">Tezgâha hazır levent yok.</td></tr>}
              </tbody>
            </table>
          </div>
        )}
        <label className="field"><span>Bağlama tarihi</span><input type="date" required value={date} onChange={(event) => setDate(event.target.value)} /></label>
        <ErrorAlert error={mountState.error} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={mountState.isLoading || beamId === null}>Bağla</button>
        </footer>
      </form>
    </Modal>
  )
}

/** Tezgâh panosu: her tezgâhta hangi levent var, ne kadar dokundu, ne kadar kaldı. */
export function DokumaLoomBoardPage() {
  const canManage = usePermission(Permissions.DokumaWeavingManage)
  const { data, error, isLoading } = useGetDokumaLoomBoardQuery()
  const canManageFloor = usePermission(Permissions.DokumaFloorManage)
  const [mounting, setMounting] = useState<DokumaLoomBoardItem | null>(null)
  const [stopping, setStopping] = useState<DokumaLoomBoardItem | null>(null)
  const [endStop, endStopState] = useEndDokumaLoomStopMutation()
  const hasActions = canManage || canManageFloor

  return (
    <section className="page">
      <h1 className="visually-hidden">Tezgâh Panosu</h1>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error ?? endStopState.error} />
      {data && (
        <div className="table-wrap">
          <table className="table">
            <thead>
              <tr>
                <th>Tezgâh</th>
                <th>Durum</th>
                <th>Levent</th>
                <th>Sipariş</th>
                <th>Artikel</th>
                <th>Bağlandı</th>
                <th className="table__number">Dokunan / beklenen (m)</th>
                <th className="table__number">Kalan (m)</th>
                <th className="table__number">Top</th>
                {hasActions && <th aria-label="İşlem" />}
              </tr>
            </thead>
            <tbody>
              {data.map((loom) => {
                const remaining = loom.expectedFabricMeters !== null && loom.wovenMeters !== null ? loom.expectedFabricMeters - loom.wovenMeters : null
                return (
                  <tr key={loom.loomId}>
                    <td><strong>{loom.loomCode}</strong><div className="muted">{loom.loomName} · {loomTypeLabels[loom.loomType]}, {sheddingTypeLabels[loom.sheddingType]}</div></td>
                    <td>
                      {loom.openStopCategory
                        ? <>{dokumaStopCategoryLabels[loom.openStopCategory]}<div className="muted">{loom.openStopReason} · {formatDateTime(loom.openStopSince)}</div></>
                        : loom.beamId !== null ? 'Çalışıyor' : <span className="muted">Boşta</span>}
                    </td>
                    <td>{loom.beamId !== null ? <Link to={`/dokuma/weaving/beams/${loom.beamId}`}><code>{loom.beamNo}</code></Link> : <span className="muted">Boş</span>}</td>
                    <td>{loom.orderNo ?? '—'}{loom.customerName && <div className="muted">{loom.customerName}</div>}</td>
                    <td>{loom.articleCode ? `${loom.articleCode} — ${loom.articleName}` : '—'}</td>
                    <td>{loom.mountedOn ? formatDate(loom.mountedOn) : '—'}</td>
                    <td className="table__number">
                      {loom.beamId !== null ? `${formatQuantity(loom.wovenMeters ?? 0)} / ${formatQuantity(loom.expectedFabricMeters ?? 0)}` : '—'}
                    </td>
                    <td className="table__number">{remaining !== null ? formatQuantity(remaining) : '—'}</td>
                    <td className="table__number">{loom.beamId !== null ? loom.rollCount : '—'}</td>
                    {hasActions && (
                      <td className="table__actions">
                        {canManageFloor && (loom.openStopCategory
                          ? <button type="button" className="button" disabled={endStopState.isLoading} onClick={() => endStop(loom.loomId)}>Duruşu bitir</button>
                          : <button type="button" className="button" onClick={() => setStopping(loom)}>Duruş başlat</button>)}
                        {canManage && (loom.beamId === null
                          ? <button type="button" className="button" onClick={() => setMounting(loom)}>Levent bağla</button>
                          : <Link className="button" to={`/dokuma/weaving/beams/${loom.beamId}`}>Aç</Link>)}
                      </td>
                    )}
                  </tr>
                )
              })}
              {data.length === 0 && <tr><td colSpan={hasActions ? 10 : 9} className="table__empty">Etkin tezgâh yok. Tanımlar › Tezgâhlar ekranından ekleyin.</td></tr>}
            </tbody>
          </table>
        </div>
      )}
      {mounting && <MountDialog loom={mounting} onClose={() => setMounting(null)} />}
      {stopping && <DokumaStartStopDialog loom={stopping} onClose={() => setStopping(null)} />}
    </section>
  )
}
