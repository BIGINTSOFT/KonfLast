import { Printer } from 'lucide-react'
import { useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import qrcode from 'qrcode-generator'
import { ErrorAlert } from '../../components/FormControls'
import { Pagination } from '../../components/Pagination'
import { formatQuantity } from '../../utils/format'
import { useGetBundleQuery, useGetBundlesQuery } from '../bundles/bundlesApi'
import { code128 } from './code128'
import { labelFormats, readLabelOptions, storeLabelOptions } from './labelFormats'
import type { LabelOptions } from './labelFormats'

const PAGE_SIZE = 100

interface LabelData {
  id: number
  bundleNo: string
  orderNo: string
  productCode: string
  modelCode: string
  colorName: string
  sizeName: string
  quantity: number
  rfidUid: string | null
}

export function Barcode({ text }: { text: string }) {
  const barcode = useMemo(() => code128(text), [text])
  if (!barcode) {
    return null
  }

  return (
    <svg
      className="label__barcode"
      viewBox={`0 0 ${barcode.width} 10`}
      preserveAspectRatio="none"
      role="img"
      aria-label={`Barkod ${text}`}
    >
      {barcode.bars.map((bar) => (
        <rect key={bar.x} x={bar.x} y={0} width={bar.width} height={10} />
      ))}
    </svg>
  )
}

export function QrCode({ text }: { text: string }) {
  const cells = useMemo(() => {
    const qr = qrcode(0, 'M')
    qr.addData(text)
    qr.make()
    const count = qr.getModuleCount()
    const dark: { x: number; y: number }[] = []
    for (let row = 0; row < count; row += 1) {
      for (let column = 0; column < count; column += 1) {
        if (qr.isDark(row, column)) {
          dark.push({ x: column, y: row })
        }
      }
    }
    return { count, dark }
  }, [text])

  return (
    <svg
      className="label__qr"
      viewBox={`-2 -2 ${cells.count + 4} ${cells.count + 4}`}
      shapeRendering="crispEdges"
      role="img"
      aria-label={`Karekod ${text}`}
    >
      {cells.dark.map((cell) => (
        <rect key={`${cell.x}:${cell.y}`} x={cell.x} y={cell.y} width={1} height={1} />
      ))}
    </svg>
  )
}

function Label({ data, options }: { data: LabelData; options: LabelOptions }) {
  return (
    <article className="label">
      <header className="label__header">
        <span>{data.orderNo}</span>
        <span>{data.modelCode} · {data.productCode}</span>
      </header>
      <div className="label__body">
        <div className="label__main">
          <div className="label__number">{data.bundleNo}</div>
          <div className="label__variant">{data.colorName} / {data.sizeName}</div>
          <div className="label__quantity">{formatQuantity(data.quantity)} adet</div>
        </div>
        {options.showQr && <QrCode text={data.bundleNo} />}
      </div>
      {options.showBarcode && <Barcode text={data.bundleNo} />}
      {options.showCard && data.rfidUid && <footer className="label__footer">Kart {data.rfidUid}</footer>}
    </article>
  )
}

/** Etiket biçimi ve barkod / karekod seçimi; ekrana özgü seçenekler alt öğe olarak eklenir. */
export function LabelOptionsToolbar({ options, onChange, children }: {
  options: LabelOptions
  onChange: (patch: Partial<LabelOptions>) => void
  children?: ReactNode
}) {
  return (
    <div className="toolbar toolbar--filters">
      <label>
        Etiket
        <select value={options.format} onChange={(event) => onChange({ format: event.target.value as LabelOptions['format'] })}>
          {Object.entries(labelFormats).map(([key, item]) => (
            <option key={key} value={key}>{item.title}</option>
          ))}
        </select>
      </label>
      <label className="checkbox">
        <input type="checkbox" checked={options.showBarcode} onChange={(event) => onChange({ showBarcode: event.target.checked })} />
        Barkod
      </label>
      <label className="checkbox">
        <input type="checkbox" checked={options.showQr} onChange={(event) => onChange({ showQr: event.target.checked })} />
        Karekod
      </label>
      {children}
    </div>
  )
}

/**
 * Demet etiketleri. Tarayıcının yazdırma penceresiyle basılır: etiket yazıcısında her etiket ayrı sayfadır,
 * A4 etiket kağıdında etiketler ızgaraya dizilir. Barkod ve karekod demet numarasını taşır.
 */
export function LabelPrintPage() {
  const [searchParams] = useSearchParams()
  const orderId = Number(searchParams.get('orderId')) || undefined
  const bundleId = Number(searchParams.get('bundleId')) || undefined
  const [page, setPage] = useState(1)
  const [options, setOptions] = useState<LabelOptions>(readLabelOptions)

  const list = useGetBundlesQuery(
    { page, pageSize: PAGE_SIZE, productionOrderId: orderId },
    { skip: orderId === undefined || bundleId !== undefined },
  )
  const single = useGetBundleQuery(bundleId ?? 0, { skip: bundleId === undefined })

  const change = (patch: Partial<LabelOptions>) => {
    const next = { ...options, ...patch }
    setOptions(next)
    storeLabelOptions(next)
  }

  const labels: LabelData[] = bundleId !== undefined
    ? (single.data ? [single.data] : [])
    // İptal edilmiş demete etiket basılmaz; en eski demet önce.
    : [...(list.data?.items ?? [])].filter((bundle) => bundle.status !== 'CANCELLED').sort((a, b) => a.id - b.id)

  const format = labelFormats[options.format]
  const back = orderId ? `/production-orders/${orderId}` : bundleId ? `/bundles/${bundleId}` : '/bundles'

  return (
    <section className="page label-page">
      {/* Kağıt boyutu seçilen etikete göre ayarlanır; yalnızca yazdırırken geçerlidir. */}
      <style>{`@page { size: ${format.page}; margin: ${format.pageMargin}; }`}</style>

      <div className="no-print">
        <p><Link to={back}>← Geri dön</Link></p>
        <header className="page__header">
          <div>
            <h1>Demet etiketleri</h1>
            <p className="muted">
              Yazdırma penceresinde hedef yazıcıyı seçin; "kenar boşluğu yok" ve %100 ölçek kullanın. Barkod ve
              karekod demet numarasını taşır.
            </p>
          </div>
          <button
            type="button"
            className="button button--primary"
            disabled={labels.length === 0}
            onClick={() => window.print()}
          >
            <Printer aria-hidden="true" />
            Yazdır ({labels.length})
          </button>
        </header>

        <LabelOptionsToolbar options={options} onChange={change}>
          <label className="checkbox">
            <input type="checkbox" checked={options.showCard} onChange={(event) => change({ showCard: event.target.checked })} />
            Kart numarası
          </label>
        </LabelOptionsToolbar>

        {(list.isLoading || single.isLoading) && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={list.error ?? single.error} />
        {orderId === undefined && bundleId === undefined && (
          <p className="alert alert--info">Etiket basmak için bir siparişin ya da demetin sayfasından gelin.</p>
        )}
        {list.data && list.data.totalPages > 1 && (
          <>
            <p className="muted">Her sayfada {PAGE_SIZE} demet basılır; sıradaki grup için sonraki sayfaya geçin.</p>
            <Pagination page={list.data.page} totalPages={list.data.totalPages} totalCount={list.data.totalCount} onPageChange={setPage} />
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
        {labels.map((label) => (
          <Label key={label.id} data={label} options={options} />
        ))}
      </div>
    </section>
  )
}
