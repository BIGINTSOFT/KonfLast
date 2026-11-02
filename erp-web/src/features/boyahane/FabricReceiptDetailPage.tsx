import { Pencil, Printer, Trash2 } from 'lucide-react'
import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDate, formatQuantity } from '../../utils/format'
import { fabricRollStatusPresentation, useDeleteFabricReceiptMutation, useGetFabricReceiptQuery } from './boyahaneReceiptsApi'
import type { FabricLot } from './boyahaneReceiptsApi'
import { FabricReceiptFormDialog } from './FabricReceiptFormDialog'

const sum = (values: number[]) => values.reduce((total, value) => total + value, 0)

/** Hesaplanan değer ölçülenden ayırt edilir: gerçek ölçüm girilene kadar yaklaşıktır. */
function Measure({ value, measured }: { value: number; measured: boolean }) {
  return measured ? <>{formatQuantity(value)}</> : <span className="muted" title="Kumaş kartından hesaplandı">≈ {formatQuantity(value)}</span>
}

function LotSection({ lot }: { lot: FabricLot }) {
  const meters = sum(lot.rolls.map((roll) => roll.meters))
  const kilograms = sum(lot.rolls.map((roll) => roll.kilograms))
  return (
    <section>
      <h2 className="section-title">{lot.fabricCode} — {lot.fabricName} <small className="muted">{lot.lotNo}</small></h2>
      {lot.customerFabricCode && <p className="muted">Müşteri kumaş kodu: {lot.customerFabricCode}</p>}
      {!lot.inspectionRequired && <p className="muted">Ham kontrol zorunlu değil (müşteri ayarı ya da açılış stoğu); toplar uygun girdi.</p>}
      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              <th>Top no</th>
              <th>Müşteri top no</th>
              <th className="table__number">Metre</th>
              <th className="table__number">Kilo</th>
              <th>Durum</th>
              <th>Adres</th>
            </tr>
          </thead>
          <tbody>
            {lot.rolls.map((roll) => {
              const status = fabricRollStatusPresentation[roll.status]
              return (
                <tr key={roll.id}>
                  <td><code>{roll.rollNo}</code></td>
                  <td>{roll.customerRollNo ?? '—'}</td>
                  <td className="table__number"><Measure value={roll.meters} measured={roll.metersMeasured} /></td>
                  <td className="table__number"><Measure value={roll.kilograms} measured={roll.kilogramsMeasured} /></td>
                  <td>
                    <span className={`badge badge--${status.tone}`}>{status.label}</span>
                    {roll.isConditionallyApproved && <> <span className="badge badge--info">Şartlı onaylı</span></>}
                  </td>
                  <td>{roll.locationCode ?? '—'}</td>
                </tr>
              )
            })}
          </tbody>
          <tfoot>
            <tr>
              <th scope="row">Toplam ({lot.rolls.length} top)</th>
              <td />
              <td className="table__number">{formatQuantity(meters)}</td>
              <td className="table__number">{formatQuantity(kilograms)}</td>
              <td />
            </tr>
            {(lot.declaredRollCount !== null || lot.declaredMeters !== null || lot.declaredKilograms !== null) && (
              <tr>
                <th scope="row">İrsaliye{lot.declaredRollCount !== null && ` (${lot.declaredRollCount} top)`}</th>
                <td />
                <td className="table__number">{lot.declaredMeters !== null ? formatQuantity(lot.declaredMeters) : '—'}</td>
                <td className="table__number">{lot.declaredKilograms !== null ? formatQuantity(lot.declaredKilograms) : '—'}</td>
                <td />
              </tr>
            )}
          </tfoot>
        </table>
      </div>
    </section>
  )
}

/** Kabul belgesi: kumaşlar, toplar ve irsaliyeyle karşılaştırma. */
export function FabricReceiptDetailPage() {
  const id = Number(useParams().id)
  const navigate = useNavigate()
  const canManage = usePermission(Permissions.BoyahaneReceiptManage)
  const { data: receipt, error, isLoading } = useGetFabricReceiptQuery(id)
  const [remove, { error: deleteError, isLoading: deleting }] = useDeleteFabricReceiptMutation()
  const [confirming, setConfirming] = useState(false)
  const [editing, setEditing] = useState(false)

  const handleDelete = async () => {
    const result = await remove(id)
    if (!('error' in result)) {
      navigate('/boyahane/receipts')
    }
  }

  return (
    <section className="page">
      <p><Link to="/boyahane/receipts">← Kabul belgelerine dön</Link></p>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error ?? deleteError} />

      {receipt && (
        <>
          <header className="page__header">
            <div>
              <h1>{receipt.receiptNo}</h1>
              <p className="muted">
                {formatDate(receipt.receiptDate)} · {receipt.customerName ?? 'İşletme malı'}
                {receipt.despatchNo && ` · İrsaliye ${receipt.despatchNo}`}
              </p>
              {receipt.notes && <p>{receipt.notes}</p>}
            </div>
            <div className="page__actions">
              <Link to={`/boyahane/receipts/${receipt.id}/labels`} className="button">
                <Printer aria-hidden="true" />
                Top etiketleri
              </Link>
              {canManage && (
                <button type="button" className="button" onClick={() => setEditing(true)}>
                  <Pencil aria-hidden="true" />
                  Düzenle
                </button>
              )}
              {canManage && receipt.canDelete && !confirming && (
                <button type="button" className="button button--danger-ghost" onClick={() => setConfirming(true)}>
                  <Trash2 aria-hidden="true" />
                  Sil
                </button>
              )}
              {confirming && (
                <button type="button" className="button button--danger-ghost" disabled={deleting} onClick={handleDelete}>
                  Silmeyi onayla
                </button>
              )}
            </div>
          </header>
          {receipt.lots.map((lot) => <LotSection key={lot.id} lot={lot} />)}
          {editing && <FabricReceiptFormDialog receipt={receipt} onClose={() => setEditing(false)} />}
        </>
      )}
    </section>
  )
}
