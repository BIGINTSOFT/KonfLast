import { Boxes, Pencil, Plus, Scale, Trash2 } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { fieldErrors } from '../../api/envelope'
import { ErrorAlert } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { formatDateTime, formatQuantity } from '../../utils/format'
import { parseDecimal } from '../../utils/money'
import { parseQuantity } from '../stock/stockPresentation'
import { useGetAssortmentTemplateOptionsQuery } from './assortmentApi'
import {
  useClearShipmentCartonsMutation,
  useDeleteShipmentCartonMutation,
  useDistributeShipmentCartonsMutation,
  useGetShipmentPackingQuery,
  useSaveShipmentCartonMutation,
  useSetShipmentCartonWeightsMutation,
} from './shippingApi'
import type { ShipmentCarton, ShipmentCartonLine, ShipmentPacking, ShipmentPackingItem } from './shippingApi'

const kg = (value: number | null) => (value === null ? '—' : formatQuantity(value))
const kgText = (value: number | null) => (value === null ? '' : String(value).replace('.', ','))
const parseKg = (text: string) => (text.trim() === '' ? null : parseDecimal(text))

/** Kolinin içeriği kısaca: renk başına beden:adet. */
export function cartonSummary(lines: ShipmentCartonLine[]): string {
  const byColor = new Map<string, ShipmentCartonLine[]>()
  for (const line of lines) {
    const color = line.colorName ?? '—'
    byColor.set(color, [...(byColor.get(color) ?? []), line])
  }
  return [...byColor.entries()]
    .map(([color, items]) => `${color} ${[...items].sort((a, b) => a.sizeOrder - b.sizeOrder).map((line) => `${line.sizeName ?? line.itemCode}:${formatQuantity(line.quantity)}`).join(' ')}`)
    .join(' · ')
}

const variantText = (item: ShipmentPackingItem) =>
  [item.productCode, item.colorName, item.sizeName].filter(Boolean).join(' / ') || item.itemCode

/** Koli dağıtımı: şablon seçilir; mevcut koliler silinip yeniden oluşturulur. */
function DistributeDialog({ packing, onClose }: { packing: ShipmentPacking; onClose: () => void }) {
  const options = useGetAssortmentTemplateOptionsQuery({ customerId: packing.customerId })
  const [distribute, { isLoading, error }] = useDistributeShipmentCartonsMutation()
  const [templateId, setTemplateId] = useState<number | null>(null)
  const selected = options.data?.find((option) => option.id === templateId) ?? null

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    if (templateId === null) {
      return
    }
    if (packing.cartonCount > 0
      && !window.confirm(`Mevcut ${packing.cartonCount} koli (yükleme okutmalarıyla birlikte) silinip yeniden dağıtılacak. Devam edilsin mi?`)) {
      return
    }
    const result = await distribute({ id: packing.shipmentId, assortmentTemplateId: templateId, rowVersion: packing.rowVersion })
    if (!('error' in result)) {
      onClose()
    }
  }

  return (
    <Modal title="Koli dağıt" onClose={onClose}>
      <form className="form" onSubmit={(event) => void handleSubmit(event)}>
        <p className="muted form__note">
          Her ürün-renk için şablonun beden oranlarıyla dolu koliler oluşturulur; oranı tamamlamayan kalan adetler renk
          başına bir karışık koliye konur. Koliler tek renklidir.
        </p>
        {options.isLoading && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={options.error} />
        {options.data && options.data.length === 0 && (
          <p className="alert alert--info">Bu müşteri için tanımlı aktif asorti şablonu yok. Önce Asorti Şablonları ekranından şablon tanımlayın.</p>
        )}
        {options.data && options.data.length > 0 && (
          <label className="field">
            <span>Asorti şablonu</span>
            <select required value={templateId ?? ''} onChange={(event) => setTemplateId(event.target.value === '' ? null : Number(event.target.value))}>
              <option value="">Şablon seçin…</option>
              {options.data.map((option) => (
                <option key={option.id} value={option.id}>
                  {option.code} — {option.name} ({option.piecesPerCarton} adet{option.customerName ? '' : ', genel'})
                </option>
              ))}
            </select>
            {fieldErrors(error, 'assortmentTemplateId')?.map((message) => <small key={message} className="field__error">{message}</small>)}
          </label>
        )}
        {selected && <p className="muted">Dağılım: {selected.distribution}</p>}
        <ErrorAlert error={error} fields={['assortmentTemplateId']} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading || templateId === null}>
            {isLoading ? 'Dağıtılıyor…' : 'Dağıt'}
          </button>
        </footer>
      </form>
    </Modal>
  )
}

/** Koli ekleme / düzenleme: sevkiyat kalemlerinden bu koliye girecek adetler ve ağırlık. */
function CartonDialog({ packing, carton, onClose }: { packing: ShipmentPacking; carton?: ShipmentCarton; onClose: () => void }) {
  const [save, { isLoading, error }] = useSaveShipmentCartonMutation()
  const current = new Map((carton?.lines ?? []).map((line) => [line.stockItemId, line.quantity]))
  const [quantities, setQuantities] = useState<Record<number, string>>(() =>
    Object.fromEntries([...current.entries()].map(([id, quantity]) => [id, String(quantity)])),
  )
  const [netKg, setNetKg] = useState(kgText(carton?.netKg ?? null))
  const [grossKg, setGrossKg] = useState(kgText(carton?.grossKg ?? null))
  const [attempted, setAttempted] = useState(false)

  const entries = packing.items.map((item) => ({ item, text: quantities[item.stockItemId] ?? '' }))
  const filled = entries.filter((entry) => entry.text.trim() !== '' && entry.text.trim() !== '0')
  const invalid = filled.some((entry) => parseQuantity(entry.text) === null)
    || (netKg.trim() !== '' && parseKg(netKg) === null) || (grossKg.trim() !== '' && parseKg(grossKg) === null)

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    setAttempted(true)
    if (invalid || filled.length === 0) {
      return
    }
    const result = await save({
      id: packing.shipmentId,
      cartonId: carton?.id,
      body: {
        rowVersion: packing.rowVersion,
        lines: filled.map((entry) => ({ stockItemId: entry.item.stockItemId, quantity: parseQuantity(entry.text)! })),
        netKg: parseKg(netKg),
        grossKg: parseKg(grossKg),
      },
    })
    if (!('error' in result)) {
      onClose()
    }
  }

  return (
    <Modal title={carton ? `Koli ${carton.cartonNo}` : 'Yeni koli'} onClose={onClose} variant="drawer">
      <form className="form" onSubmit={(event) => void handleSubmit(event)} noValidate>
        <div className="table-wrap">
          <table className="table table--compact">
            <thead>
              <tr>
                <th>Mamul</th>
                <th className="table__number">Sevkiyatta</th>
                <th className="table__number">Kolisiz kalan</th>
                <th className="table__number">Bu kolide</th>
              </tr>
            </thead>
            <tbody>
              {entries.map(({ item, text }) => {
                // Bu kolinin şu anki adedi kolilere girmiş adetten düşülür: kalan, bu koliye eklenebilecek adettir.
                const open = item.quantity - item.packedQuantity + (current.get(item.stockItemId) ?? 0)
                return (
                  <tr key={item.stockItemId}>
                    <td><code>{item.itemCode}</code> <span className="muted">{variantText(item)}</span></td>
                    <td className="table__number">{formatQuantity(item.quantity)}</td>
                    <td className="table__number">{formatQuantity(open)}</td>
                    <td className="table__number">
                      <input
                        className="packing-box"
                        aria-label={`${item.itemCode} adet`}
                        inputMode="decimal"
                        value={text}
                        onChange={(event) => setQuantities({ ...quantities, [item.stockItemId]: event.target.value })}
                      />
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
        <div className="form__pair">
          <label className="field">
            <span>Net kg</span>
            <input inputMode="decimal" value={netKg} onChange={(event) => setNetKg(event.target.value)} />
            {fieldErrors(error, 'netKg')?.map((message) => <small key={message} className="field__error">{message}</small>)}
          </label>
          <label className="field">
            <span>Brüt kg</span>
            <input inputMode="decimal" value={grossKg} onChange={(event) => setGrossKg(event.target.value)} />
            {fieldErrors(error, 'grossKg')?.map((message) => <small key={message} className="field__error">{message}</small>)}
          </label>
        </div>
        {attempted && filled.length === 0 && <small className="field__error">Koliye en az bir mamul girin.</small>}
        {attempted && invalid && <small className="field__error">Adet ve ağırlıklar sayı olmalıdır (örn. 12 ya da 8,5).</small>}
        {fieldErrors(error, 'lines')?.map((message) => <small key={message} className="field__error">{message}</small>)}
        <ErrorAlert error={error} fields={['lines', 'netKg', 'grossKg']} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading}>{isLoading ? 'Kaydediliyor…' : 'Kaydet'}</button>
        </footer>
      </form>
    </Modal>
  )
}

type WeightScope = 'all' | 'template' | 'mixed' | 'empty'

const weightScopes: Record<WeightScope, string> = {
  all: 'Bütün koliler',
  template: 'Şablonlu (dolu) koliler',
  mixed: 'Karışık koliler',
  empty: 'Ağırlığı girilmemiş koliler',
}

/** Toplu ağırlık: aynı şablonla dolan koliler genellikle aynı ağırlıktadır. */
function WeightsDialog({ packing, onClose }: { packing: ShipmentPacking; onClose: () => void }) {
  const [setWeights, { isLoading, error }] = useSetShipmentCartonWeightsMutation()
  const [scope, setScope] = useState<WeightScope>('template')
  const [netKg, setNetKg] = useState('')
  const [grossKg, setGrossKg] = useState('')
  const targets = packing.cartons.filter((carton) =>
    scope === 'all' ? true
      : scope === 'template' ? carton.assortmentTemplateId !== null
        : scope === 'mixed' ? carton.assortmentTemplateId === null
          : carton.netKg === null && carton.grossKg === null)
  const invalid = (netKg.trim() !== '' && parseKg(netKg) === null) || (grossKg.trim() !== '' && parseKg(grossKg) === null)

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    if (invalid || targets.length === 0) {
      return
    }
    const result = await setWeights({
      id: packing.shipmentId,
      rowVersion: packing.rowVersion,
      cartonIds: targets.map((carton) => carton.id),
      netKg: parseKg(netKg),
      grossKg: parseKg(grossKg),
    })
    if (!('error' in result)) {
      onClose()
    }
  }

  return (
    <Modal title="Koli ağırlığı" onClose={onClose}>
      <form className="form" onSubmit={(event) => void handleSubmit(event)} noValidate>
        <label className="field">
          <span>Uygulanacak koliler</span>
          <select value={scope} onChange={(event) => setScope(event.target.value as WeightScope)}>
            {(Object.keys(weightScopes) as WeightScope[]).map((key) => <option key={key} value={key}>{weightScopes[key]}</option>)}
          </select>
        </label>
        <p className="muted">{targets.length} koliye uygulanır. Boş bırakılan ağırlık silinir.</p>
        <div className="form__pair">
          <label className="field">
            <span>Net kg</span>
            <input inputMode="decimal" value={netKg} onChange={(event) => setNetKg(event.target.value)} />
            {fieldErrors(error, 'netKg')?.map((message) => <small key={message} className="field__error">{message}</small>)}
          </label>
          <label className="field">
            <span>Brüt kg</span>
            <input inputMode="decimal" value={grossKg} onChange={(event) => setGrossKg(event.target.value)} />
            {fieldErrors(error, 'grossKg')?.map((message) => <small key={message} className="field__error">{message}</small>)}
          </label>
        </div>
        {invalid && <small className="field__error">Ağırlık sayı olmalıdır (örn. 8,5).</small>}
        <ErrorAlert error={error} fields={['netKg', 'grossKg']} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading || invalid || targets.length === 0}>Uygula</button>
        </footer>
      </form>
    </Modal>
  )
}

type Dialog = { kind: 'distribute' } | { kind: 'carton'; carton?: ShipmentCarton } | { kind: 'weights' }

/**
 * Sevkiyatın kolileri (çeki listesi): dağıtım, elle düzeltme, ağırlık ve yükleme durumu. Koliler yalnızca taslakta
 * değişir; koli varsa sevk anında kolilerdeki adetler kalemlerle birebir tutmalıdır.
 */
export function ShipmentCartonsSection({ shipmentId, canManage }: { shipmentId: number; canManage: boolean }) {
  const { data: packing, error, isLoading } = useGetShipmentPackingQuery(shipmentId)
  const [remove, removeState] = useDeleteShipmentCartonMutation()
  const [clear, clearState] = useClearShipmentCartonsMutation()
  const [dialog, setDialog] = useState<Dialog | null>(null)

  if (isLoading) {
    return <p role="status">Yükleniyor…</p>
  }

  if (!packing) {
    return <ErrorAlert error={error} />
  }

  const editable = canManage && packing.isEditable
  const busy = removeState.isLoading || clearState.isLoading
  const differences = packing.items.filter((item) => item.quantity !== item.packedQuantity)
  const totals = packing.cartons.reduce(
    (sum, carton) => ({
      quantity: sum.quantity + carton.quantity,
      net: sum.net + (carton.netKg ?? 0),
      gross: sum.gross + (carton.grossKg ?? 0),
    }),
    { quantity: 0, net: 0, gross: 0 },
  )

  const handleDelete = (carton: ShipmentCarton) => {
    if (window.confirm(`Koli ${carton.cartonNo} silinecek. Sonraki kolilerin numarası bir azalır; etiketlerini yeniden basın. Devam edilsin mi?`)) {
      void remove({ id: shipmentId, cartonId: carton.id, rowVersion: packing.rowVersion })
    }
  }

  const handleClear = () => {
    if (window.confirm(`${packing.cartonCount} koli silinecek (çeki listesi kaldırılır). Devam edilsin mi?`)) {
      void clear({ id: shipmentId, rowVersion: packing.rowVersion })
    }
  }

  return (
    <>
      <h2 className="section-title section-title--with-action">
        <span>
          Koliler ({packing.cartonCount})
          {packing.cartonCount > 0 && <span className="muted"> · Yüklenen {packing.loadedCount} / {packing.cartonCount}</span>}
        </span>
        {editable && (
          <span className="page__actions">
            <button type="button" className="button" disabled={busy} onClick={() => setDialog({ kind: 'distribute' })}>
              <Boxes aria-hidden="true" />Koli dağıt
            </button>
            <button type="button" className="button" disabled={busy} onClick={() => setDialog({ kind: 'carton' })}>
              <Plus aria-hidden="true" />Koli ekle
            </button>
            {packing.cartonCount > 0 && (
              <>
                <button type="button" className="button" disabled={busy} onClick={() => setDialog({ kind: 'weights' })}>
                  <Scale aria-hidden="true" />Ağırlık
                </button>
                <button type="button" className="button button--danger-ghost" disabled={busy} onClick={handleClear}>
                  <Trash2 aria-hidden="true" />Kolileri sil
                </button>
              </>
            )}
          </span>
        )}
      </h2>

      <ErrorAlert error={removeState.error ?? clearState.error} />

      {packing.cartonCount > 0 && !packing.matchesItems && (
        <div className="alert alert--error" role="alert">
          Koli içerikleri sevkiyat kalemleriyle tutmuyor; bu haliyle sevk edilemez. Kolileri düzeltin ya da yeniden dağıtın.
          <ul>
            {differences.map((item) => (
              <li key={item.stockItemId}>
                <code>{item.itemCode}</code> {variantText(item)}: sevkiyatta {formatQuantity(item.quantity)}, kolilerde {formatQuantity(item.packedQuantity)}
              </li>
            ))}
          </ul>
        </div>
      )}

      {packing.cartonCount === 0 ? (
        <p className="muted">
          Çeki listesi yok. {editable ? 'Asorti şablonuyla "Koli dağıt" ya da "Koli ekle" ile koli oluşturun.' : ''}
        </p>
      ) : (
        <div className="table-wrap">
          <table className="table table--compact">
            <thead>
              <tr>
                <th className="table__number">Koli</th>
                <th>Koli kodu</th>
                <th>Şablon</th>
                <th>İçerik</th>
                <th className="table__number">Adet</th>
                <th className="table__number">Net kg</th>
                <th className="table__number">Brüt kg</th>
                <th>Yükleme</th>
                {editable && <th aria-label="İşlemler" />}
              </tr>
            </thead>
            <tbody>
              {packing.cartons.map((carton) => (
                <tr key={carton.id}>
                  <td className="table__number">{carton.cartonNo}</td>
                  <td><code>{carton.code}</code></td>
                  <td>{carton.assortmentTemplateCode ?? <span className="muted">Karışık</span>}</td>
                  <td>{cartonSummary(carton.lines)}</td>
                  <td className="table__number">{formatQuantity(carton.quantity)}</td>
                  <td className="table__number">{kg(carton.netKg)}</td>
                  <td className="table__number">{kg(carton.grossKg)}</td>
                  <td>
                    {carton.loadedAt
                      ? <span className="badge badge--ok" title={`${carton.loadedBy ?? ''} · ${formatDateTime(carton.loadedAt)}`}>Yüklendi</span>
                      : <span className="muted">—</span>}
                  </td>
                  {editable && (
                    <td className="table__actions">
                      <button type="button" className="icon-button" aria-label={`Koli ${carton.cartonNo} düzenle`} title="Düzenle" disabled={busy} onClick={() => setDialog({ kind: 'carton', carton })}>
                        <Pencil aria-hidden="true" />
                      </button>
                      <button type="button" className="icon-button" aria-label={`Koli ${carton.cartonNo} sil`} title="Sil" disabled={busy} onClick={() => handleDelete(carton)}>
                        <Trash2 aria-hidden="true" />
                      </button>
                    </td>
                  )}
                </tr>
              ))}
            </tbody>
            <tfoot>
              <tr>
                <th colSpan={4} scope="row">Toplam</th>
                <td className="table__number"><strong>{formatQuantity(totals.quantity)}</strong></td>
                <td className="table__number">{formatQuantity(totals.net)}</td>
                <td className="table__number">{formatQuantity(totals.gross)}</td>
                <td colSpan={editable ? 2 : 1} />
              </tr>
            </tfoot>
          </table>
        </div>
      )}

      {dialog?.kind === 'distribute' && <DistributeDialog packing={packing} onClose={() => setDialog(null)} />}
      {dialog?.kind === 'carton' && <CartonDialog packing={packing} carton={dialog.carton} onClose={() => setDialog(null)} />}
      {dialog?.kind === 'weights' && <WeightsDialog packing={packing} onClose={() => setDialog(null)} />}
    </>
  )
}
