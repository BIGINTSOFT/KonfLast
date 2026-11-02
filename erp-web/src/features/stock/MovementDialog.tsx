import { useState } from 'react'
import type { FormEvent } from 'react'
import { fieldErrors } from '../../api/envelope'
import { ErrorAlert, TextField } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { LookupSelect } from '../masterData/LookupSelect'
import { useGetLookupQuery } from '../masterData/masterDataApi'
import {
  useAdjustStockMutation,
  useGetLocationOptionsQuery,
  useIssueStockMutation,
  useReceiveStockMutation,
  useTransferStockMutation,
} from './stockApi'
import type { LotAttributes, StockBalance, StockLocationBalance, StockLotBalance } from './stockApi'
import { formatStockQuantity, locationLabel, parseQuantity, quantityStep, stockUnitLabels } from './stockPresentation'

export type MovementKind = 'receipt' | 'issue' | 'transfer' | 'relocation' | 'adjustment'

interface MovementDialogProps {
  kind: MovementKind
  /**
   * Çıkış, transfer, adres değişimi ve sayım bir bakiye satırından açılır; giriş için isteğe bağlıdır. Satır
   * adres bazındaysa işlem o adreste, top bazındaysa o topta yapılır; depo bazındaysa adres formda seçilir.
   */
  balance?: StockBalance | StockLocationBalance | StockLotBalance
  /** Bakiye satırı olmadan açılan girişte önceden doldurulacak alanlar (ör. siparişin eksik malzemesi). */
  preset?: { warehouseId: number | null; stockItemId: number; quantity?: number; note?: string }
  onClose: () => void
}

const titles: Record<MovementKind, string> = {
  receipt: 'Stok girişi',
  issue: 'Stok çıkışı',
  transfer: 'Depolar arası transfer',
  relocation: 'Adres değişimi',
  adjustment: 'Sayım düzeltmesi',
}

/**
 * Stok hareketi girişi. Miktarın geçerliliğini (birim, stok yeterliliği) sunucu denetler. İstek kimliği
 * pencere açılırken bir kez üretilir: gönderim yarıda kalıp yeniden denenirse stok ikinci kez değişmez.
 * Adres seçilmezse hareket deponun genel adresine yazılır. Takipli kartta giriş yeni bir top açar; diğer
 * hareketler Toplar ekranından, top seçilerek yapılır.
 */
export function MovementDialog({ kind, balance, preset, onClose }: MovementDialogProps) {
  const [requestId] = useState(() => crypto.randomUUID())
  const warehouses = useGetLookupQuery({ resource: 'warehouses', take: 100 })

  const [receive, receiveState] = useReceiveStockMutation()
  const [issue, issueState] = useIssueStockMutation()
  const [transfer, transferState] = useTransferStockMutation()
  const [adjust, adjustState] = useAdjustStockMutation()
  const state = {
    receipt: receiveState,
    issue: issueState,
    transfer: transferState,
    relocation: transferState,
    adjustment: adjustState,
  }[kind]

  const fixedLocation = balance && 'locationId' in balance ? balance : null
  const fixedLot = balance && 'lotId' in balance ? balance : null
  // Kart formda seçildiyse takipli olup olmadığı bilinmez; top alanları isteğe bağlı gösterilir.
  const showLotFields = kind === 'receipt' && (!balance || ('isLotTracked' in balance && balance.isLotTracked))
  const moves = kind === 'transfer' || kind === 'relocation'

  const [warehouseId, setWarehouseId] = useState<number | null>(balance?.warehouseId ?? preset?.warehouseId ?? null)
  const [locationId, setLocationId] = useState<number | null>(fixedLocation?.locationId ?? null)
  const [toWarehouseId, setToWarehouseId] = useState<number | null>(null)
  const [toLocationId, setToLocationId] = useState<number | null>(null)
  const [stockItemId, setStockItemId] = useState<number | null>(balance?.stockItemId ?? preset?.stockItemId ?? null)
  const [quantity, setQuantity] = useState(preset?.quantity ? String(preset.quantity) : '')
  const [documentNo, setDocumentNo] = useState('')
  const [note, setNote] = useState(preset?.note ?? '')
  const [batchNo, setBatchNo] = useState('')
  const [shade, setShade] = useState('')
  const [width, setWidth] = useState('')
  const [supplierRollNo, setSupplierRollNo] = useState('')

  // Adres değişiminde hedef depo, çıkış deposunun kendisidir.
  const targetWarehouseId = kind === 'relocation' ? warehouseId : toWarehouseId
  const locations = useGetLocationOptionsQuery(warehouseId ?? 0, { skip: warehouseId === null || fixedLocation !== null })
  const targetLocations = useGetLocationOptionsQuery(targetWarehouseId ?? 0, { skip: !moves || targetWarehouseId === null })

  const amount = parseQuantity(quantity)
  const quantityField = kind === 'adjustment' ? 'countedQuantity' : 'quantity'
  const sourceLocationField = moves ? 'fromLocationId' : 'locationId'
  const ready =
    warehouseId !== null &&
    stockItemId !== null &&
    amount !== null &&
    (kind !== 'transfer' || toWarehouseId !== null) &&
    (kind !== 'relocation' || toLocationId !== null)

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    if (warehouseId === null || stockItemId === null || amount === null) {
      return
    }

    const common = { requestId, stockItemId, note: note.trim() || null }
    const lotId = fixedLot?.lotId ?? null
    const lot: LotAttributes | null =
      showLotFields && (batchNo.trim() || shade.trim() || width.trim() || supplierRollNo.trim())
        ? {
            batchNo: batchNo.trim() || null,
            shade: shade.trim() || null,
            width: parseQuantity(width),
            supplierRollNo: supplierRollNo.trim() || null,
          }
        : null
    const result =
      kind === 'receipt'
        ? await receive({ ...common, warehouseId, locationId, lot, quantity: amount, documentNo: documentNo.trim() || null })
        : kind === 'issue'
          ? await issue({ ...common, warehouseId, locationId, lotId, quantity: amount, documentNo: documentNo.trim() || null })
          : kind === 'adjustment'
            ? await adjust({ ...common, warehouseId, locationId, lotId, countedQuantity: amount, note: note.trim() })
            : await transfer({
                ...common,
                fromWarehouseId: warehouseId,
                toWarehouseId: targetWarehouseId ?? 0,
                fromLocationId: locationId,
                toLocationId,
                lotId,
                quantity: amount,
              })

    if (!('error' in result)) {
      onClose()
    }
  }

  const errorsOf = (field: string) =>
    fieldErrors(state.error, field)?.map((message) => (
      <small key={message} className="field__error">{message}</small>
    ))

  const warehouseSelect = (
    label: string,
    value: number | null,
    onChange: (id: number | null) => void,
    field: string,
    excludeId?: number | null,
  ) => (
    <label className="field">
      <span>{label}</span>
      <select
        value={value ?? ''}
        onChange={(event) => onChange(event.target.value ? Number(event.target.value) : null)}
        aria-invalid={Boolean(fieldErrors(state.error, field)?.length)}
      >
        <option value="">Seçin…</option>
        {warehouses.data
          ?.filter((warehouse) => warehouse.id !== excludeId)
          .map((warehouse) => (
            <option key={warehouse.id} value={warehouse.id}>
              {warehouse.code} — {warehouse.name}
            </option>
          ))}
      </select>
      {errorsOf(field)}
    </label>
  )

  /** Stok giren adres aktif olmalıdır; çıkışta pasif adres de seçilebilir (boşaltılabilsin). */
  const locationSelect = (
    label: string,
    options: typeof locations.data,
    value: number | null,
    onChange: (id: number | null) => void,
    field: string,
    inbound: boolean,
    excludeId?: number | null,
  ) => (
    <label className="field">
      <span>{label}</span>
      <select
        value={value ?? ''}
        onChange={(event) => onChange(event.target.value ? Number(event.target.value) : null)}
        aria-invalid={Boolean(fieldErrors(state.error, field)?.length)}
        disabled={!options}
      >
        <option value="">{kind === 'relocation' && inbound ? 'Seçin…' : 'Genel adres'}</option>
        {options
          ?.filter((location) => location.id !== excludeId && (!inbound || location.isActive))
          .filter((location) => !location.isDefault || (kind === 'relocation' && inbound))
          .map((location) => (
            <option key={location.id} value={location.id}>
              {locationLabel(location)}
            </option>
          ))}
      </select>
      {errorsOf(field)}
    </label>
  )

  return (
    <Modal title={titles[kind]} onClose={onClose}>
      <form className="form" onSubmit={(event) => void handleSubmit(event)} noValidate>
        {balance ? (
          <p>
            <code>{balance.itemCode}</code> {balance.itemName}
            <br />
            <span className="muted">
              {balance.warehouseCode} — {balance.warehouseName}
              {fixedLocation && <> · adres <code>{fixedLocation.locationCode}</code></>}
              {fixedLot && <> · top <code>{fixedLot.lotNo}</code></>} · mevcut{' '}
              <strong>{formatStockQuantity(balance.quantity, balance.unit)}</strong>
            </span>
          </p>
        ) : (
          <>
            <LookupSelect
              resource="stock-items"
              label="Stok kartı"
              value={stockItemId}
              onChange={setStockItemId}
              required
              errors={fieldErrors(state.error, 'stockItemId')}
            />
            {warehouseSelect(
              'Depo',
              warehouseId,
              (id) => {
                setWarehouseId(id)
                setLocationId(null)
              },
              'warehouseId',
            )}
          </>
        )}

        {!fixedLocation &&
          locationSelect(
            moves ? 'Çıkış adresi' : 'Adres',
            locations.data,
            locationId,
            setLocationId,
            sourceLocationField,
            kind === 'receipt',
          )}

        {kind === 'transfer' &&
          warehouseSelect(
            'Aktarılacak depo',
            toWarehouseId,
            (id) => {
              setToWarehouseId(id)
              setToLocationId(null)
            },
            'toWarehouseId',
            warehouseId,
          )}

        {moves &&
          locationSelect(
            kind === 'relocation' ? 'Yeni adres' : 'Giriş adresi',
            targetLocations.data,
            toLocationId,
            setToLocationId,
            'toLocationId',
            true,
            kind === 'relocation' ? (locationId ?? locations.data?.find((location) => location.isDefault)?.id) : null,
          )}

        <TextField
          label={kind === 'adjustment' ? 'Sayılan miktar' : 'Miktar'}
          type="number"
          min={0}
          step={quantityStep(balance?.unit)}
          value={quantity}
          onChange={(event) => setQuantity(event.target.value)}
          errors={fieldErrors(state.error, quantityField)}
          hint={
            kind === 'adjustment'
              ? fixedLot
                ? 'Topun ölçülen gerçek miktarı; top bu değere getirilir, fark hareket olarak yazılır.'
                : 'Adreste sayılan gerçek miktar; adresin bakiyesi bu değere getirilir, fark hareket olarak yazılır.'
              : balance
                ? `Birim: ${stockUnitLabels[balance.unit]}`
                : 'Birim stok kartında tanımlıdır; adetle izlenen kartta tam sayı girin.'
          }
          required
        />

        {showLotFields && (
          <fieldset className="form__section">
            <legend>Top / parti bilgisi{balance ? '' : ' (takipli kartlarda)'}</legend>
            <div className="form__pair">
              <TextField
                label="Parti no"
                maxLength={50}
                value={batchNo}
                onChange={(event) => setBatchNo(event.target.value)}
                errors={fieldErrors(state.error, 'lot.batchNo')}
              />
              <TextField
                label="Tedarikçi top no"
                maxLength={50}
                value={supplierRollNo}
                onChange={(event) => setSupplierRollNo(event.target.value)}
                errors={fieldErrors(state.error, 'lot.supplierRollNo')}
              />
            </div>
            <div className="form__pair">
              <TextField
                label="Ton"
                maxLength={30}
                value={shade}
                onChange={(event) => setShade(event.target.value)}
                errors={fieldErrors(state.error, 'lot.shade')}
              />
              <TextField
                label="En (cm)"
                type="number"
                min={0}
                step="0.01"
                value={width}
                onChange={(event) => setWidth(event.target.value)}
                errors={fieldErrors(state.error, 'lot.width')}
              />
            </div>
            <small className="field__hint">Her giriş ayrı bir top açar; top numarasını sistem verir.</small>
          </fieldset>
        )}

        {(kind === 'receipt' || kind === 'issue') && (
          <TextField
            label="Belge no (isteğe bağlı)"
            maxLength={50}
            value={documentNo}
            onChange={(event) => setDocumentNo(event.target.value)}
            errors={fieldErrors(state.error, 'documentNo')}
            hint="İrsaliye, fiş vb."
          />
        )}

        <label className="field">
          <span>{kind === 'adjustment' ? 'Düzeltmenin nedeni' : 'Not (isteğe bağlı)'}</span>
          <textarea rows={2} maxLength={300} value={note} onChange={(event) => setNote(event.target.value)} />
          {errorsOf('note')}
        </label>

        <ErrorAlert
          error={state.error ?? warehouses.error ?? locations.error ?? targetLocations.error}
          fields={[
            'stockItemId', 'warehouseId', 'toWarehouseId', sourceLocationField, 'toLocationId', quantityField, 'documentNo',
            'note', 'lot.batchNo', 'lot.supplierRollNo', 'lot.shade', 'lot.width',
          ]}
        />

        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={state.isLoading || !ready}>
            {state.isLoading ? 'Kaydediliyor…' : 'Kaydet'}
          </button>
        </footer>
      </form>
    </Modal>
  )
}
