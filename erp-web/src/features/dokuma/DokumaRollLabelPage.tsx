import { Printer } from 'lucide-react'
import { useState } from 'react'
import { Link, useParams, useSearchParams } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { documentTexts } from '../../utils/documentText'
import { formatDate, formatQuantity } from '../../utils/format'
import { Barcode, LabelOptionsToolbar, QrCode } from '../labels/LabelPrintPage'
import { labelFormats, readLabelOptions, storeLabelOptions } from '../labels/labelFormats'
import type { LabelOptions } from '../labels/labelFormats'
import { useGetDokumaBeamWeavingQuery } from './dokumaWeavingApi'

/** Leventten kesilen topların etiketleri (?roll= ile tek top). Barkod ve karekod top numarasını taşır. */
export function DokumaRollLabelPage() {
  const id = Number(useParams().id)
  const [search] = useSearchParams()
  const rollId = Number(search.get('roll')) || null
  const { data, error, isLoading } = useGetDokumaBeamWeavingQuery(id)
  const [options, setOptions] = useState<LabelOptions>(readLabelOptions)

  const change = (patch: Partial<LabelOptions>) => {
    const next = { ...options, ...patch }
    setOptions(next)
    storeLabelOptions(next)
  }

  const format = labelFormats[options.format]
  const rolls = data?.rolls.filter((roll) => rollId === null || roll.id === rollId) ?? []

  return (
    <section className="page label-page">
      {/* Kağıt boyutu seçilen etikete göre ayarlanır; yalnızca yazdırırken geçerlidir. */}
      <style>{`@page { size: ${format.page}; margin: ${format.pageMargin}; }`}</style>

      <div className="no-print">
        <p><Link to={`/dokuma/weaving/beams/${id}`}>← Levente dön</Link></p>
        <header className="page__header">
          <div>
            <h1>Top etiketleri</h1>
            <p className="muted">
              Yazdırma penceresinde hedef yazıcıyı seçin; "kenar boşluğu yok" ve %100 ölçek kullanın. Barkod ve karekod top
              numarasını taşır.
            </p>
          </div>
          <button type="button" className="button button--primary" disabled={rolls.length === 0} onClick={() => window.print()}>
            <Printer aria-hidden="true" />
            Yazdır ({rolls.length})
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
        {data && rolls.map((roll) => {
          // Etiket müşterinin belge dilinde; müşteri kodları yalnızca müşteri siparişinde gelir.
          const t = documentTexts(roll.documentLanguage)
          return (
            <article key={roll.id} className="label">
              <header className="label__header">
                <span>{roll.articleCode}</span>
                <span>{formatDate(roll.cutOn)}</span>
              </header>
              <div className="label__body">
                <div className="label__main">
                  <div className="label__number">{roll.rollNo}</div>
                  <div className="label__variant">{roll.articleName}</div>
                  {(roll.customerOrderNo || roll.customerArticleCode) && (
                    <div className="label__variant">
                      {roll.customerOrderNo && `${t('customerOrderNo')} ${roll.customerOrderNo}`}
                      {roll.customerOrderNo && roll.customerArticleCode && ' · '}
                      {roll.customerArticleCode && `${t('customerCode')} ${roll.customerArticleCode}`}
                    </div>
                  )}
                  <div className="label__quantity">
                    {formatQuantity(roll.meters)} m{roll.kilograms !== null && ` · ${formatQuantity(roll.kilograms)} kg`}
                  </div>
                </div>
                {options.showQr && <QrCode text={roll.rollNo} />}
              </div>
              {options.showBarcode && <Barcode text={roll.rollNo} />}
              <footer className="label__footer">
                {roll.customerName ?? t('ownStock')} · {roll.orderNo} · {roll.loomCode} · {roll.beamNo}
              </footer>
            </article>
          )
        })}
      </div>
    </section>
  )
}
