import { Plus, Printer } from 'lucide-react'
import { useState } from 'react'
import { ErrorAlert } from '../../components/FormControls'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { Barcode, QrCode } from '../labels/LabelPrintPage'
import { labelFormats, readLabelOptions, storeLabelOptions } from '../labels/labelFormats'
import type { LabelOptions } from '../labels/labelFormats'
import { useGetLookupQuery } from '../masterData/masterDataApi'
import { locationBarcode } from '../terminal/terminalApi'
import { BulkLocationDialog } from './BulkLocationDialog'
import { useGetLocationOptionsQuery } from './stockApi'

/**
 * Adres (raf) etiketleri. Barkod adresin kimliğini taşır: aynı adres kodu başka depoda da olabileceği için el
 * terminali adresi etiketinden kesin olarak tanır. Tarayıcının yazdırma penceresiyle basılır.
 */
export function LocationLabelPage() {
  const warehouses = useGetLookupQuery({ resource: 'warehouses', take: 100 })
  const [warehouseId, setWarehouseId] = useState<number | null>(null)
  const [zone, setZone] = useState('')
  const [options, setOptions] = useState<LabelOptions>(readLabelOptions)
  const [bulkOpen, setBulkOpen] = useState(false)
  // Toplu adres açma depo tanımı yetkisiyle yapılır.
  const canManage = usePermission(Permissions.WarehouseManage)
  const locations = useGetLocationOptionsQuery(warehouseId ?? 0, { skip: warehouseId === null })

  const change = (patch: Partial<LabelOptions>) => {
    const next = { ...options, ...patch }
    setOptions(next)
    storeLabelOptions(next)
  }

  const warehouse = warehouses.data?.find((item) => item.id === warehouseId)
  const prefix = zone.trim().toLocaleUpperCase('tr-TR')
  const labels = (locations.data ?? []).filter((location) => location.isActive && location.code.startsWith(prefix))
  const format = labelFormats[options.format]

  return (
    <section className="page label-page">
      <style>{`@page { size: ${format.page}; margin: ${format.pageMargin}; }`}</style>

      <div className="no-print">
        <header className="page__header">
          <div>
            <h1>Adres etiketleri</h1>
            <p className="muted">
              Rafa yapıştırılan etiket: el terminalinde adres bu barkoddan okutulur. Yazdırma penceresinde "kenar
              boşluğu yok" ve %100 ölçek kullanın.
            </p>
          </div>
          <div className="page__actions">
            {canManage && (
              <button
                type="button"
                className="button"
                disabled={warehouseId === null}
                title={warehouseId === null ? 'Önce depoyu seçin.' : undefined}
                onClick={() => setBulkOpen(true)}
              >
                <Plus aria-hidden="true" />
                Toplu adres aç
              </button>
            )}
            <button type="button" className="button button--primary" disabled={labels.length === 0} onClick={() => window.print()}>
              <Printer aria-hidden="true" />
              Yazdır ({labels.length})
            </button>
          </div>
        </header>

        <div className="toolbar toolbar--filters">
          <label>
            Depo
            <select value={warehouseId ?? ''} onChange={(event) => setWarehouseId(event.target.value ? Number(event.target.value) : null)}>
              <option value="">Seçin…</option>
              {warehouses.data?.map((item) => (
                <option key={item.id} value={item.id}>{item.code} — {item.name}</option>
              ))}
            </select>
          </label>
          <label>
            Adres kodu başlangıcı
            <input type="text" placeholder="Örn. A-01" value={zone} onChange={(event) => setZone(event.target.value)} />
          </label>
          <label>
            Etiket
            <select value={options.format} onChange={(event) => change({ format: event.target.value as LabelOptions['format'] })}>
              {Object.entries(labelFormats).map(([key, item]) => (
                <option key={key} value={key}>{item.title}</option>
              ))}
            </select>
          </label>
          <label className="checkbox">
            <input type="checkbox" checked={options.showBarcode} onChange={(event) => change({ showBarcode: event.target.checked })} />
            Barkod
          </label>
          <label className="checkbox">
            <input type="checkbox" checked={options.showQr} onChange={(event) => change({ showQr: event.target.checked })} />
            Karekod
          </label>
        </div>

        {locations.isFetching && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={warehouses.error ?? locations.error} />
        {warehouseId === null && <p className="muted">Etiketlerini basacağınız depoyu seçin.</p>}
      </div>

      {bulkOpen && warehouseId !== null && (
        <BulkLocationDialog
          warehouseId={warehouseId}
          warehouseLabel={warehouse ? `${warehouse.code} — ${warehouse.name}` : ''}
          onClose={() => setBulkOpen(false)}
        />
      )}

      <div
        className={`label-sheet label-sheet--${format.layout}`}
        style={{
          '--label-width': format.width,
          '--label-height': format.height,
          '--label-columns': format.columns,
        } as React.CSSProperties}
      >
        {labels.map((location) => (
          <article key={location.id} className="label">
            <header className="label__header">
              <span>{warehouse?.code}</span>
              <span>Adres</span>
            </header>
            <div className="label__body">
              <div className="label__main">
                <div className="label__number">{location.code}</div>
                <div className="label__variant">{location.description ?? warehouse?.name}</div>
              </div>
              {options.showQr && <QrCode text={locationBarcode(location.id)} />}
            </div>
            {options.showBarcode && <Barcode text={locationBarcode(location.id)} />}
          </article>
        ))}
      </div>
    </section>
  )
}
