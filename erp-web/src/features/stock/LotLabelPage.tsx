import { Printer } from 'lucide-react'
import { useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { Pagination } from '../../components/Pagination'
import { formatDateTime } from '../../utils/format'
import { Barcode, LabelOptionsToolbar, QrCode } from '../labels/LabelPrintPage'
import { labelFormats, readLabelOptions, storeLabelOptions } from '../labels/labelFormats'
import type { LabelOptions } from '../labels/labelFormats'
import { useGetStockLotsQuery } from './stockApi'
import type { StockLotBalance } from './stockApi'
import { formatStockQuantity } from './stockPresentation'

const PAGE_SIZE = 100

function LotLabel({ lot, options }: { lot: StockLotBalance; options: LabelOptions }) {
  const details = [lot.shade && `Ton ${lot.shade}`, lot.width !== null && `En ${lot.width} cm`].filter(Boolean).join(' · ')

  return (
    <article className="label">
      <header className="label__header">
        <span>{lot.itemCode}</span>
        <span>{lot.batchNo ? `Parti ${lot.batchNo}` : formatDateTime(lot.receivedAt).split(" ")[0]}</span>
      </header>
      <div className="label__body">
        <div className="label__main">
          <div className="label__number">{lot.lotNo}</div>
          <div className="label__variant">{lot.itemName}</div>
          <div className="label__quantity">
            {formatStockQuantity(lot.initialQuantity, lot.unit)}
            {details && ` · ${details}`}
          </div>
        </div>
        {options.showQr && <QrCode text={lot.lotNo} />}
      </div>
      {options.showBarcode && <Barcode text={lot.lotNo} />}
      {lot.supplierRollNo && <footer className="label__footer">Tedarikçi top no {lot.supplierRollNo}</footer>}
    </article>
  )
}

/**
 * Top etiketleri. Tarayıcının yazdırma penceresiyle basılır; barkod ve karekod sistem top numarasını taşır.
 * Etikette topun giriş miktarı yazar (kalan miktar zamanla değişir).
 */
export function LotLabelPage() {
  const [searchParams] = useSearchParams()
  const number = (name: string) => Number(searchParams.get(name)) || undefined
  const [page, setPage] = useState(1)
  const [options, setOptions] = useState<LabelOptions>(readLabelOptions)

  const { data, error, isLoading } = useGetStockLotsQuery({
    page,
    pageSize: PAGE_SIZE,
    lotId: number('lotId'),
    warehouseId: number('warehouseId'),
    stockItemId: number('stockItemId'),
    purchaseOrderId: number('purchaseOrderId'),
    search: searchParams.get('search') || undefined,
  })

  const change = (patch: Partial<LabelOptions>) => {
    const next = { ...options, ...patch }
    setOptions(next)
    storeLabelOptions(next)
  }

  // İki adrese bölünmüş top tek etiket alır.
  const labels = [...new Map((data?.items ?? []).map((lot) => [lot.lotId, lot])).values()]
  const format = labelFormats[options.format]

  return (
    <section className="page label-page">
      {/* Kağıt boyutu seçilen etikete göre ayarlanır; yalnızca yazdırırken geçerlidir. */}
      <style>{`@page { size: ${format.page}; margin: ${format.pageMargin}; }`}</style>

      <div className="no-print">
        <p><Link to="/stock-lots">← Toplara dön</Link></p>
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
        {data && data.totalPages > 1 && (
          <>
            <p className="muted">Her sayfada {PAGE_SIZE} top basılır; sıradaki grup için sonraki sayfaya geçin.</p>
            <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
          </>
        )}
      </div>

      <div
        className={`label-sheet label-sheet--${format.layout}`}
        style={{
          '--label-width': format.width,
          '--label-height': format.height,
          '--label-columns': format.columns,
        } as React.CSSProperties}
      >
        {labels.map((lot) => (
          <LotLabel key={lot.lotId} lot={lot} options={options} />
        ))}
      </div>
    </section>
  )
}
