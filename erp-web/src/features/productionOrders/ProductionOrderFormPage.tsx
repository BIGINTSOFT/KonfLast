import { X } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { fieldErrors, isRequestError } from '../../api/envelope'
import { ErrorAlert, TextField } from '../../components/FormControls'
import { formatQuantity, todayIsoDate } from '../../utils/format'
import { LookupSelect } from '../masterData/LookupSelect'
import type { LookupItem } from '../masterData/masterDataApi'
import { buildQuantityMatrix } from './orderPresentation'
import type { MatrixAxis } from './orderPresentation'
import {
  operationSequenceHints,
  operationSequenceLabels,
  useCreateProductionOrderMutation,
  useGetProductionOrderQuery,
  useUpdateProductionOrderMutation,
} from './productionOrdersApi'
import type { OperationSequenceMode, ProductionOrder } from './productionOrdersApi'

const cellKey = (colorId: number, sizeId: number) => `${colorId}:${sizeId}`

/** Renk (satır) × beden (sütun) adet tablosu; satır ve sütunlar seçim kutularından eklenir. */
export function QuantityEditor({
  colors, sizes, quantities, onColorsChange, onSizesChange, onQuantityChange, errors, idSuffix,
}: {
  colors: MatrixAxis[]
  sizes: MatrixAxis[]
  quantities: Record<string, string>
  onColorsChange: (colors: MatrixAxis[]) => void
  onSizesChange: (sizes: MatrixAxis[]) => void
  onQuantityChange: (key: string, value: string) => void
  errors?: string[]
  /** Aynı ekranda birden fazla tablo varsa (ürün başına bir tablo) seçim kutularının kimliklerini ayırır. */
  idSuffix?: string
}) {
  const quantityOf = (colorId: number, sizeId: number) => Number(quantities[cellKey(colorId, sizeId)] || 0)
  const colorTotal = (colorId: number) => sizes.reduce((sum, size) => sum + quantityOf(colorId, size.id), 0)
  const sizeTotal = (sizeId: number) => colors.reduce((sum, color) => sum + quantityOf(color.id, sizeId), 0)
  const total = colors.reduce((sum, color) => sum + colorTotal(color.id), 0)

  // Listeden seçilen renk / beden doğrudan tabloya eklenir; kutu bir sonraki seçim için boş kalır.
  const addColor = (item: LookupItem | null) => {
    if (item && !colors.some((color) => color.id === item.id)) {
      onColorsChange([...colors, item])
    }
  }

  const addSize = (item: LookupItem | null) => {
    if (item && !sizes.some((size) => size.id === item.id)) {
      onSizesChange([...sizes, item])
    }
  }

  return (
    <fieldset className="matrix-editor">
      <legend>Renk – beden dağılımı</legend>

      <div className="matrix-editor__bar">
        <LookupSelect
          resource="colors"
          idSuffix={idSuffix}
          label="Renk ekle"
          value={null}
          required
          onChange={() => undefined}
          onSelectItem={addColor}
        />
        <LookupSelect
          resource="sizes"
          idSuffix={idSuffix}
          label="Beden ekle"
          value={null}
          required
          onChange={() => undefined}
          onSelectItem={addSize}
        />
      </div>

      {colors.length === 0 || sizes.length === 0 ? (
        <p className="matrix-editor__empty">Adet girmek için listeden en az bir renk ve bir beden seçin.</p>
      ) : (
        <div className="table-wrap">
          <table className="table table--matrix">
            <thead>
              <tr>
                <th>Renk</th>
                {sizes.map((size) => (
                  <th key={size.id}>
                    <span className="matrix-editor__axis matrix-editor__axis--size">
                      {size.name}
                      <button
                        type="button"
                        className="matrix-editor__remove"
                        aria-label={`${size.name} bedenini kaldır`}
                        title="Bedeni kaldır"
                        onClick={() => onSizesChange(sizes.filter((s) => s.id !== size.id))}
                      >
                        <X aria-hidden="true" />
                      </button>
                    </span>
                  </th>
                ))}
                <th className="table__number">Toplam</th>
              </tr>
            </thead>
            <tbody>
              {colors.map((color) => (
                <tr key={color.id}>
                  <th scope="row">
                    <span className="matrix-editor__axis">
                      {color.name}
                      <button
                        type="button"
                        className="matrix-editor__remove"
                        aria-label={`${color.name} rengini kaldır`}
                        title="Rengi kaldır"
                        onClick={() => onColorsChange(colors.filter((c) => c.id !== color.id))}
                      >
                        <X aria-hidden="true" />
                      </button>
                    </span>
                  </th>
                  {sizes.map((size) => (
                    <td key={size.id} className="table__number">
                      <input
                        type="number"
                        min={0}
                        step={1}
                        inputMode="numeric"
                        className="matrix-input"
                        aria-label={`${color.name} ${size.name} adet`}
                        value={quantities[cellKey(color.id, size.id)] ?? ''}
                        onChange={(event) => onQuantityChange(cellKey(color.id, size.id), event.target.value)}
                      />
                    </td>
                  ))}
                  <td className="table__number"><strong>{formatQuantity(colorTotal(color.id))}</strong></td>
                </tr>
              ))}
            </tbody>
            <tfoot>
              <tr>
                <th scope="row">Toplam</th>
                {sizes.map((size) => (
                  <td key={size.id} className="table__number">{formatQuantity(sizeTotal(size.id))}</td>
                ))}
                <td className="table__number"><strong>{formatQuantity(total)}</strong></td>
              </tr>
            </tfoot>
          </table>
        </div>
      )}

      {errors?.map((message) => (
        <small key={message} className="field__error">{message}</small>
      ))}
    </fieldset>
  )
}

function OrderForm({ order, onConflict }: { order?: ProductionOrder; onConflict?: () => void }) {
  const navigate = useNavigate()
  const [createOrder, createState] = useCreateProductionOrderMutation()
  const [updateOrder, updateState] = useUpdateProductionOrderMutation()
  const { isLoading, error } = order ? updateState : createState

  const existing = order ? buildQuantityMatrix(order.items) : undefined

  const [customerId, setCustomerId] = useState<number | null>(order?.customerId ?? null)
  const [productId, setProductId] = useState<number | null>(order?.productId ?? null)
  const [customerOrderNo, setCustomerOrderNo] = useState(order?.customerOrderNo ?? '')
  const [orderDate, setOrderDate] = useState(order?.orderDate ?? todayIsoDate())
  const [dueDate, setDueDate] = useState(order?.dueDate ?? '')
  const [notes, setNotes] = useState(order?.notes ?? '')
  const [materialWarehouseId, setMaterialWarehouseId] = useState<number | null>(order?.materialWarehouseId ?? null)
  const [operationSequence, setOperationSequence] = useState<OperationSequenceMode>(
    order?.operationSequence === 'FREE' ? 'FREE' : 'SEQUENTIAL',
  )
  const [colors, setColors] = useState<MatrixAxis[]>(existing?.colors ?? [])
  const [sizes, setSizes] = useState<MatrixAxis[]>(existing?.sizes ?? [])
  const [quantities, setQuantities] = useState<Record<string, string>>(() =>
    Object.fromEntries((order?.items ?? []).map((item) => [cellKey(item.colorId, item.sizeId), String(item.quantity)])),
  )

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()

    // Yalnızca tabloda görünen ve adedi girilmiş hücreler kalem olur.
    const items = colors.flatMap((color) =>
      sizes
        .map((size) => ({
          colorId: color.id,
          sizeId: size.id,
          quantity: Number(quantities[cellKey(color.id, size.id)] || 0),
        }))
        .filter((item) => item.quantity !== 0),
    )

    const body = {
      customerId: customerId ?? 0,
      productId: productId ?? 0,
      customerOrderNo: customerOrderNo.trim() || null,
      orderDate,
      // Boş tarih için sunucunun "zorunludur" hatasını ilgili alana bağlaması sağlanır.
      dueDate: dueDate || '0001-01-01',
      notes: notes.trim() || null,
      materialWarehouseId,
      operationSequence,
      items,
    }

    const result = order
      ? await updateOrder({ id: order.id, body: { ...body, rowVersion: order.rowVersion } })
      : await createOrder(body)

    if ('data' in result && result.data) {
      void navigate(`/production-orders/${result.data.id}`)
    } else if (isRequestError(result.error) && result.error.code === 'CONCURRENCY_CONFLICT') {
      onConflict?.()
    }
  }

  const backTo = order ? `/production-orders/${order.id}` : '/production-orders'
  // Müşteri siparişinden açılan üretim siparişi o siparişin ürününü karşılar; ürün ve müşteri değişmez.
  const linked = Boolean(order?.salesOrderId)

  return (
    <form className="form" onSubmit={(event) => void handleSubmit(event)} noValidate>
      <div className="form__grid">
        <LookupSelect
          resource="customers"
          label="Müşteri"
          value={customerId}
          onChange={setCustomerId}
          required
          disabled={linked}
          errors={fieldErrors(error, 'customerId')}
        />
        <LookupSelect
          resource="products"
          label="Ürün"
          value={productId}
          onChange={setProductId}
          required
          disabled={linked}
          errors={fieldErrors(error, 'productId')}
        />
        <TextField
          label="Müşteri sipariş no (isteğe bağlı)"
          value={customerOrderNo}
          maxLength={50}
          onChange={(event) => setCustomerOrderNo(event.target.value)}
          errors={fieldErrors(error, 'customerOrderNo')}
          autoComplete="off"
        />
        <div className="form__dates">
          <TextField
            label="Sipariş tarihi"
            type="date"
            value={orderDate}
            onChange={(event) => setOrderDate(event.target.value)}
            errors={fieldErrors(error, 'orderDate')}
          />
          <TextField
            label="Termin tarihi"
            type="date"
            value={dueDate}
            onChange={(event) => setDueDate(event.target.value)}
            errors={fieldErrors(error, 'dueDate')}
          />
        </div>
        <LookupSelect
          resource="warehouses"
          label="Malzeme deposu (isteğe bağlı)"
          value={materialWarehouseId}
          onChange={setMaterialWarehouseId}
          errors={fieldErrors(error, 'materialWarehouseId')}
        />
        <label className="field">
          <span>Operasyon sırası</span>
          <select
            value={operationSequence}
            onChange={(event) => setOperationSequence(event.target.value as OperationSequenceMode)}
          >
            <option value="SEQUENTIAL">{operationSequenceLabels.SEQUENTIAL}</option>
            <option value="FREE">{operationSequenceLabels.FREE}</option>
          </select>
          <small className="field__hint">
            {operationSequenceHints[operationSequence]} Başlanan operasyon tamamlanmadan demet başka operasyona geçemez.
          </small>
          {fieldErrors(error, 'operationSequence')?.map((message) => (
            <small key={message} className="field__error">{message}</small>
          ))}
        </label>
      </div>

      {linked && (
        <p className="alert alert--info">
          Bu üretim siparişi bir müşteri siparişinden açıldı: müşterisi ve ürünü değiştirilemez. Adet, müşteri
          siparişindeki adetten farklı girilirse müşteri siparişi ekranında uyarı olarak görünür.
        </p>
      )}

      <QuantityEditor
        colors={colors}
        sizes={sizes}
        quantities={quantities}
        onColorsChange={setColors}
        onSizesChange={setSizes}
        onQuantityChange={(key, value) => setQuantities((current) => ({ ...current, [key]: value }))}
        errors={fieldErrors(error, 'items')}
      />

      <label className="field">
        <span>Not (isteğe bağlı)</span>
        <textarea rows={3} maxLength={1000} value={notes} onChange={(event) => setNotes(event.target.value)} />
        {fieldErrors(error, 'notes')?.map((message) => (
          <small key={message} className="field__error">{message}</small>
        ))}
      </label>

      <ErrorAlert
        error={error}
        fields={['customerId', 'productId', 'customerOrderNo', 'orderDate', 'dueDate', 'materialWarehouseId', 'items', 'notes']}
      />

      <footer className="form__actions">
        <Link to={backTo} className="button">Vazgeç</Link>
        <button type="submit" className="button button--primary" disabled={isLoading}>
          {isLoading ? 'Kaydediliyor…' : 'Kaydet'}
        </button>
      </footer>
    </form>
  )
}

export function ProductionOrderCreatePage() {
  return (
    <section className="page">
      <p><Link to="/production-orders">← Siparişlere dön</Link></p>
      <h1>Yeni üretim siparişi</h1>
      <p className="muted">Sipariş taslak olarak kaydedilir; numarası kayıt sırasında sistem tarafından verilir.</p>
      <div className="card card--spaced"><OrderForm /></div>
    </section>
  )
}

export function ProductionOrderEditPage() {
  const id = Number(useParams().id)
  const { data: order, isLoading, error } = useGetProductionOrderQuery(id)
  const [conflicted, setConflicted] = useState(false)

  return (
    <section className="page">
      <p><Link to={`/production-orders/${id}`}>← Siparişe dön</Link></p>
      <h1>Siparişi düzenle{order ? `: ${order.orderNo}` : ''}</h1>

      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {conflicted && (
        <p className="alert alert--error" role="alert">
          Sipariş siz düzenlerken başka bir kullanıcı tarafından değiştirildi. Form güncel verilerle
          yenilendi; değişikliklerinizi kontrol ederek tekrar girin.
        </p>
      )}

      {order && !order.isEditable && (
        <p className="alert alert--info">Bu sipariş artık düzenlenemez. Değişiklik için önce taslağa alın.</p>
      )}
      {/* key: başka bir kullanıcı kaydedip sürüm değişirse form güncel veriyle yeniden kurulur. */}
      {order?.isEditable && (
        <div className="card card--spaced"><OrderForm key={order.rowVersion} order={order} onConflict={() => setConflicted(true)} /></div>
      )}
    </section>
  )
}
