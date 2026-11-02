import { Printer } from 'lucide-react'
import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { formatDate, formatQuantity } from '../../utils/format'
import { Barcode, LabelOptionsToolbar, QrCode } from '../labels/LabelPrintPage'
import { labelFormats, readLabelOptions, storeLabelOptions } from '../labels/labelFormats'
import type { LabelOptions } from '../labels/labelFormats'
import { useGetFabricReceiptQuery } from './boyahaneReceiptsApi'

/** Kabul belgesindeki topların etiketleri. Barkod ve karekod sistem top numarasını taşır. */
export function FabricRollLabelPage() {
  const id = Number(useParams().id)
  const { data: receipt, error, isLoading } = useGetFabricReceiptQuery(id)
  const [options, setOptions] = useState<LabelOptions>(readLabelOptions)

  const change = (patch: Partial<LabelOptions>) => {
    const next = { ...options, ...patch }
    setOptions(next)
    storeLabelOptions(next)
  }

  const format = labelFormats[options.format]
  const labels = receipt?.lots.flatMap((lot) => lot.rolls.map((roll) => ({ lot, roll }))) ?? []

  return (
    <section className="page label-page">
      {/* Kağıt boyutu seçilen etikete göre ayarlanır; yalnızca yazdırırken geçerlidir. */}
      <style>{`@page { size: ${format.page}; margin: ${format.pageMargin}; }`}</style>

      <div className="no-print">
        <p><Link to={`/boyahane/receipts/${id}`}>← Kabul belgesine dön</Link></p>
        <header className="page__header">
          <div>
            <h1>Top etiketleri</h1>
            <p className="muted">
              Yazdırma penceresinde hedef yazıcıyı seçin; "kenar boşluğu yok" ve %100 ölçek kullanın. Barkod ve
              karekod top numarasını taşır.
            </p>
          </div>
          <button type="button" className="button button--primary" disabled={labels.length === 0} onClick={() => window.print()}>
            <Printer aria-hidden="true" />
            Yazdır ({labels.length})
          </button>
        </header>
        <LabelOptionsToolbar options={options} onChange={change} />
        {isLoading && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={error} />
      </div>

      <div
        className={`label-sheet label-sheet--${format.layout}`}
        style={{
          '--label-width': format.width,
          '--label-height': format.height,
          '--label-columns': format.columns,
        } as React.CSSProperties}
      >
        {receipt && labels.map(({ lot, roll }) => (
          <article key={roll.id} className="label">
            <header className="label__header">
              <span>{lot.fabricCode}</span>
              <span>{formatDate(receipt.receiptDate)}</span>
            </header>
            <div className="label__body">
              <div className="label__main">
                <div className="label__number">{roll.rollNo}</div>
                <div className="label__variant">{lot.fabricName}</div>
                <div className="label__quantity">{formatQuantity(roll.meters)} m · {formatQuantity(roll.kilograms)} kg</div>
              </div>
              {options.showQr && <QrCode text={roll.rollNo} />}
            </div>
            {options.showBarcode && <Barcode text={roll.rollNo} />}
            <footer className="label__footer">
              {receipt.customerName ?? 'İşletme malı'}
              {roll.customerRollNo && ` · Müşteri top no ${roll.customerRollNo}`}
            </footer>
          </article>
        ))}
      </div>
    </section>
  )
}
