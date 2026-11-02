import { useRef, useState } from 'react'
import type { FormEvent } from 'react'
import { fieldErrors } from '../../api/envelope'
import { ErrorAlert, TextField } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { formatQuantity } from '../../utils/format'
import { useGetLookupQuery } from '../masterData/masterDataApi'
import { useAssignCardMutation } from '../rfid/rfidApi'
import { useGetReceivableBundlesQuery, useReceiveFromProductionMutation } from '../stock/stockApi'
import { useGenerateBundlesMutation, useGetBundlesQuery } from './bundlesApi'

const DEFAULT_BUNDLE_SIZE = '20'

/** Hazır gelen depoların kodları; varsa varsayılan olarak seçilir. */
const RAW_MATERIAL_WAREHOUSE_CODE = 'HAMMADDE'
const FINISHED_GOODS_WAREHOUSE_CODE = 'MAMUL'

function WarehouseSelect({
  label, value, onChange, hint, errors,
}: { label: string; value: number | null; onChange: (id: number | null) => void; hint?: string; errors?: string[] }) {
  const warehouses = useGetLookupQuery({ resource: 'warehouses', take: 100 })
  return (
    <label className="field">
      <span>{label}</span>
      <select
        value={value ?? ''}
        onChange={(event) => onChange(event.target.value ? Number(event.target.value) : null)}
        aria-invalid={Boolean(errors?.length)}
      >
        <option value="">Seçin…</option>
        {warehouses.data?.map((warehouse) => (
          <option key={warehouse.id} value={warehouse.id}>{warehouse.code} — {warehouse.name}</option>
        ))}
      </select>
      {hint && !errors?.length && <small className="field__hint">{hint}</small>}
      {errors?.map((message) => (
        <small key={message} className="field__error">{message}</small>
      ))}
    </label>
  )
}

function useDefaultWarehouse(code: string): number | null {
  const warehouses = useGetLookupQuery({ resource: 'warehouses', take: 100 })
  return warehouses.data?.find((warehouse) => warehouse.code === code)?.id ?? null
}

/** Siparişin kalan adedini demet boyuna bölerek toplu demet oluşturur; reçeteli üründe malzeme deposu da seçilir. */
export function GenerateBundlesDialog({
  orderId, remaining, hasRecipe, onClose,
}: { orderId: number; remaining: number; hasRecipe: boolean; onClose: () => void }) {
  const [generate, { isLoading, error, data }] = useGenerateBundlesMutation()
  const [bundleSize, setBundleSize] = useState(DEFAULT_BUNDLE_SIZE)
  const defaultWarehouse = useDefaultWarehouse(RAW_MATERIAL_WAREHOUSE_CODE)
  const [chosenWarehouse, setChosenWarehouse] = useState<number | null>(null)
  const warehouseId = chosenWarehouse ?? defaultWarehouse

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault()
    void generate({
      orderId,
      bundleSize: Number(bundleSize || 0),
      warehouseId: hasRecipe && warehouseId !== null ? warehouseId : undefined,
    })
  }

  if (data) {
    return (
      <Modal title="Demetler oluşturuldu" onClose={onClose}>
        <div className="form">
          <p>
            <strong>{data.bundleCount}</strong> demet oluşturuldu (toplam {formatQuantity(data.totalQuantity)} adet):{' '}
            <code>{data.bundles[0].bundleNo}</code> – <code>{data.bundles[data.bundles.length - 1].bundleNo}</code>
          </p>
          {hasRecipe && <p className="muted">Reçetedeki malzemeler depodan düşüldü.</p>}
          <footer className="form__actions">
            <button type="button" className="button button--primary" onClick={onClose}>Tamam</button>
          </footer>
        </div>
      </Modal>
    )
  }

  return (
    <Modal title="Toplu demet oluştur" onClose={onClose}>
      <form className="form" onSubmit={handleSubmit} noValidate>
        <p className="muted">
          Demetlenmemiş {formatQuantity(remaining)} adet, her renk-beden için verilen demet boyuna bölünür;
          artan adet o kalemin son demetine kalır.
        </p>
        <TextField
          label="Demet başına adet"
          type="number"
          min={1}
          step={1}
          value={bundleSize}
          onChange={(event) => setBundleSize(event.target.value)}
          errors={fieldErrors(error, 'bundleSize')}
          autoFocus
        />
        {hasRecipe && (
          <WarehouseSelect
            label="Malzemenin düşüleceği depo"
            value={warehouseId}
            onChange={setChosenWarehouse}
            errors={fieldErrors(error, 'warehouseId')}
            hint="Reçetedeki malzemeler bu depodan düşülür; stok yetmezse demet oluşturulmaz."
          />
        )}
        <ErrorAlert error={error} fields={['bundleSize', 'warehouseId']} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading}>
            {isLoading ? 'Oluşturuluyor…' : 'Demetleri oluştur'}
          </button>
        </footer>
      </form>
    </Modal>
  )
}

/**
 * Siparişin kartsız demetlerine sırayla kart atar: kart okutulunca (okuyucu numarayı yazıp Enter gönderir)
 * atama yapılır ve sıradaki demete geçilir. Böylece demet demet sayfa değiştirmek gerekmez.
 */
export function AssignCardsDialog({ orderId, onClose }: { orderId: number; onClose: () => void }) {
  // Atama yapıldıkça liste tazelenir; ilk sıradaki her zaman bir sonraki kartsız demettir.
  const { data, isLoading, error: listError } = useGetBundlesQuery({
    page: 1, pageSize: 100, status: 'CREATED', productionOrderId: orderId,
  })
  const [assign, { isLoading: assigning, error }] = useAssignCardMutation()
  const [uid, setUid] = useState('')
  const [assigned, setAssigned] = useState<{ bundleNo: string; uid: string }[]>([])
  const input = useRef<HTMLInputElement>(null)

  // En eski demet önce: liste yeniden eskiye gelir.
  const queue = [...(data?.items ?? [])].sort((a, b) => a.id - b.id)
  const current = queue[0]
  const remaining = data?.totalCount ?? 0

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    if (!current || !uid.trim()) {
      return
    }

    const result = await assign({ owner: 'bundles', id: current.id, uid: uid.trim() })
    if (!('error' in result)) {
      setAssigned((list) => [{ bundleNo: current.bundleNo, uid: uid.trim() }, ...list])
      setUid('')
    }
    input.current?.focus()
    input.current?.select()
  }

  return (
    <Modal title="Kartları sırayla ata" onClose={onClose}>
      <form className="form" onSubmit={(event) => void handleSubmit(event)} noValidate>
        {isLoading && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={listError} />

        {data && !current && (
          <p className="alert alert--info">Bu siparişte kart bekleyen demet kalmadı.</p>
        )}

        {current && (
          <>
            <dl className="status-list">
              <div><dt>Sıradaki demet</dt><dd><code>{current.bundleNo}</code></dd></div>
              <div><dt>Renk / beden</dt><dd>{current.colorName} / {current.sizeName}</dd></div>
              <div><dt>Adet</dt><dd>{formatQuantity(current.quantity)}</dd></div>
              <div><dt>Kart bekleyen</dt><dd>{remaining} demet</dd></div>
            </dl>
            <TextField
              ref={input}
              label="Kart numarası"
              value={uid}
              maxLength={10}
              onChange={(event) => setUid(event.target.value)}
              errors={fieldErrors(error, 'uid')}
              hint="Kartı okutun; atama yapılır ve sıradaki demete geçilir."
              autoFocus
              autoComplete="off"
            />
            <ErrorAlert error={error} fields={['uid']} />
          </>
        )}

        {assigned.length > 0 && (
          <p className="muted">
            Atanan: {assigned.slice(0, 5).map((item) => `${item.bundleNo} → ${item.uid}`).join(' · ')}
            {assigned.length > 5 && ` · +${assigned.length - 5}`}
          </p>
        )}

        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Kapat</button>
          {current && (
            <button type="submit" className="button button--primary" disabled={!uid.trim() || assigning}>
              {assigning ? 'Atanıyor…' : 'Kartı ata'}
            </button>
          )}
        </footer>
      </form>
    </Modal>
  )
}

/** Siparişin tamamlanmış demetlerini seçilen depoya topluca teslim alır. */
export function ReceiveBundlesDialog({ orderId, onClose }: { orderId: number; onClose: () => void }) {
  const { data, isLoading, error: listError } = useGetReceivableBundlesQuery({
    page: 1, pageSize: 100, productionOrderId: orderId,
  })
  const [receive] = useReceiveFromProductionMutation()
  const defaultWarehouse = useDefaultWarehouse(FINISHED_GOODS_WAREHOUSE_CODE)
  const [chosenWarehouse, setChosenWarehouse] = useState<number | null>(null)
  const warehouseId = chosenWarehouse ?? defaultWarehouse
  const [working, setWorking] = useState(false)
  const [done, setDone] = useState<{ count: number; quantity: number } | null>(null)
  const [failure, setFailure] = useState<unknown>(null)
  // Demet başına istek kimliği: yarıda kalan gönderim yeniden denenirse stok ikinci kez artmaz.
  const requestIds = useRef(new Map<number, string>())

  const bundles = data?.items ?? []
  const receivable = bundles.filter((bundle) => bundle.finishedQuantity > 0)

  const handleReceive = async () => {
    if (warehouseId === null) {
      return
    }

    setWorking(true)
    setFailure(null)
    let count = 0
    let quantity = 0
    for (const bundle of receivable) {
      const requestId = requestIds.current.get(bundle.bundleId) ?? crypto.randomUUID()
      requestIds.current.set(bundle.bundleId, requestId)
      const result = await receive({ requestId, bundleId: bundle.bundleId, warehouseId })
      if ('error' in result) {
        setFailure(result.error)
        break
      }
      count += 1
      quantity += bundle.finishedQuantity
    }
    setWorking(false)
    setDone({ count, quantity })
  }

  return (
    <Modal title="Tamamlanan demetleri depoya al" onClose={onClose}>
      <div className="form">
        {isLoading && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={listError} />

        {done && (
          <p className="alert alert--info">
            {done.count} demet teslim alındı: {formatQuantity(done.quantity)} adet mamul stoğuna girdi.
          </p>
        )}
        <ErrorAlert error={failure} />

        {data && !done && (
          receivable.length === 0 ? (
            <p className="alert alert--info">Bu siparişte depoya alınmayı bekleyen demet yok.</p>
          ) : (
            <>
              <p>
                <strong>{receivable.length}</strong> demet, toplam{' '}
                <strong>{formatQuantity(receivable.reduce((sum, bundle) => sum + bundle.finishedQuantity, 0))}</strong> sağlam
                adet depoya alınacak. Teslim alınan demet kapanır; artık ret ya da tamir girilemez.
              </p>
              <WarehouseSelect label="Depo" value={warehouseId} onChange={setChosenWarehouse} />
            </>
          )
        )}

        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>{done ? 'Kapat' : 'Vazgeç'}</button>
          {!done && receivable.length > 0 && (
            <button
              type="button"
              className="button button--primary"
              disabled={warehouseId === null || working}
              onClick={() => void handleReceive()}
            >
              {working ? 'Teslim alınıyor…' : 'Depoya al'}
            </button>
          )}
        </footer>
      </div>
    </Modal>
  )
}
